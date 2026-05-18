'use strict';

/**
 * RoobsWorld Multiplayer Relay Server — hardened edition
 *
 * Session relay plus server-owned session setup.
 * The relay owns shared lobby/session metadata such as the creature map seed.
 * The host client has authority over food, organisms, and events.
 *
 * Security measures:
 *   - GAME_TOKEN auth   : clients must send ?token=<GAME_TOKEN>; reject without it
 *   - Connection rate   : max CONN_RATE_MAX new connections per IP per minute
 *   - Per-IP cap        : max MAX_CONNS_PER_IP simultaneous sockets per IP
 *   - Message rate      : max MSG_RATE_MAX messages per client per second window
 *   - Join rate         : separate limiter prevents brute-forcing session codes
 *   - Payload size      : ws maxPayload hard cap + base64 field validation
 *   - Input validation  : all fields validated before use (allowlist approach)
 *   - Idle timeout      : clients inactive for IDLE_MS are disconnected
 *   - Session TTL       : sessions older than SESSION_TTL_MS are destroyed
 *   - No info leaks     : errors never reveal internal state or stack traces
 *
 * Protocol (all messages are JSON text frames):
 *
 *   Client → Server:
 *     { type: "create", serverName: "Tide Pool" }
 *     { type: "join",    code: "ABCD12" }
 *     { type: "list_public_lobbies" }
 *     { type: "join_lobby", lobbyId: "cell" }
 *     { type: "chat",    message: "Hello tide pool" }
 *     { type: "relay",   to: <playerId|null>,  data: "<base64 ≤16 KB>" }
 *     { type: "leave" }
 *     { type: "pong" }    ← keepalive response
 *
 *   Server → Client:
 *     { type: "created",       code, playerId }
 *     { type: "joined",        code, playerId, hostPlayerId }
 *     { type: "creature_map",  map } // compact server-generated map definition
 *     { type: "public_lobbies", lobbies: "ABCD12,1,0,Tide%20Pool;EFGH34,2,0,Open%20Water" } // max 0 = unlimited
 *     { type: "player_joined", playerId }
 *     { type: "player_left",   playerId, wasHost }
 *     { type: "host_migrated", newHostId }
 *     { type: "player_lobby",  playerId, lobbyId }
 *     { type: "chat",          from, message } // server-filtered global session chat
 *     { type: "data",          from, data }
 *     { type: "ping" }
 *     { type: "error",         reason: "not_found"|"full"|"already_in_session"|"invalid_code" }
 */

const http = require('http');
const fs = require('fs');
const path = require('path');
const { WebSocketServer } = require('ws');
const {
    RateLimiter,
    validateCode,
    validateClanName,
    validateServerName,
    validateClanTag,
    validateBase64,
    validateRelayTarget,
    validateLobbyId,
    normalizeChatMessage,
    validateChatMessage,
    filterProfanity,
    validateAuthToken,
    extractQueryParam,
} = require('./security');

// ---- configuration (all overridable via environment variables) ----

const CFG = {
    PORT:               parseInt(process.env.PORT               ?? '7788'),
    MAX_PLAYERS:        parseInt(process.env.MAX_PLAYERS        ?? '0'), // 0 or less = unlimited
    GAME_TOKEN:              process.env.GAME_TOKEN             ?? '',   // empty = dev mode (open)
    OFFICIAL_LOBBIES:        process.env.OFFICIAL_LOBBIES       ?? 'PUBLIC:Official Lobby',
    // Payload
    MAX_PAYLOAD_BYTES:  parseInt(process.env.MAX_PAYLOAD_BYTES  ?? String(64 * 1024)),  // 64 KB ws frame
    MAX_RELAY_BYTES:    parseInt(process.env.MAX_RELAY_BYTES    ?? String(16 * 1024)),  // 16 KB relay data
    // Timeouts
    PING_MS:            parseInt(process.env.PING_MS            ?? '30000'),
    IDLE_MS:            parseInt(process.env.IDLE_MS            ?? '120000'),  // 2 min idle → kick
    SESSION_TTL_MS:     parseInt(process.env.SESSION_TTL_MS     ?? String(2 * 60 * 60 * 1000)), // 2 hr
    // Rate limits
    CONN_RATE_WINDOW_MS:parseInt(process.env.CONN_RATE_WINDOW   ?? '60000'),
    CONN_RATE_MAX:      parseInt(process.env.CONN_RATE_MAX      ?? '20'),   // 20 new conn/min per IP
    MAX_CONNS_PER_IP:   parseInt(process.env.MAX_CONNS_PER_IP   ?? '0'),    // 0 or less = unlimited
    MSG_RATE_WINDOW_MS: parseInt(process.env.MSG_RATE_WINDOW    ?? '1000'), // 1-second window
    MSG_RATE_MAX:       parseInt(process.env.MSG_RATE_MAX       ?? '120'),  // gameplay input + voice chunks
    CHAT_RATE_WINDOW_MS:parseInt(process.env.CHAT_RATE_WINDOW   ?? '5000'),
    CHAT_RATE_MAX:      parseInt(process.env.CHAT_RATE_MAX      ?? '5'),
    JOIN_RATE_WINDOW_MS:parseInt(process.env.JOIN_RATE_WINDOW   ?? '60000'),
    JOIN_RATE_MAX:      parseInt(process.env.JOIN_RATE_MAX      ?? '10'),   // 10 join attempts/min per IP
    MAX_CHAT_CHARS:     parseInt(process.env.MAX_CHAT_CHARS     ?? '160'),
    CHAT_PROFANITY_WORDS:     process.env.CHAT_PROFANITY_WORDS  ?? '',
    MAX_AUTOSAVE_BYTES: parseInt(process.env.MAX_AUTOSAVE_BYTES ?? String(256 * 1024)),
    AUTOSAVE_DIR:             process.env.AUTOSAVE_DIR          ?? path.join(process.cwd(), 'player-progress'),
    CREATURE_MAP_SEED:         process.env.CREATURE_MAP_SEED    ?? '',
    CREATURE_MAP_RADIUS: parseFloat(process.env.CREATURE_MAP_RADIUS ?? '48'),
    CREATURE_MAP_RESOLUTION: parseInt(process.env.CREATURE_MAP_RESOLUTION ?? '72'),
    CREATURE_MAP_HEIGHT: parseFloat(process.env.CREATURE_MAP_HEIGHT ?? '8'),
    CREATURE_MAP_NOISE_SCALE: parseFloat(process.env.CREATURE_MAP_NOISE_SCALE ?? '0.055'),
    CREATURE_MAP_LANDMARKS: parseInt(process.env.CREATURE_MAP_LANDMARKS ?? '7'),
};

// ---- rate limiters ----

const connRateLimiter = new RateLimiter({ windowMs: CFG.CONN_RATE_WINDOW_MS, max: CFG.CONN_RATE_MAX });
const msgRateLimiter  = new RateLimiter({ windowMs: CFG.MSG_RATE_WINDOW_MS,  max: CFG.MSG_RATE_MAX  });
const chatRateLimiter = new RateLimiter({ windowMs: CFG.CHAT_RATE_WINDOW_MS, max: CFG.CHAT_RATE_MAX });
const joinRateLimiter = new RateLimiter({ windowMs: CFG.JOIN_RATE_WINDOW_MS, max: CFG.JOIN_RATE_MAX });

// ---- session store ----

/** @type {Map<string, Session>} */
const sessions = new Map();

/** @type {Map<string, Set<import('ws')>>} */
const connsByIp = new Map(); // ip → Set<ws>

const CODE_CHARS = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
const OPEN = 1;
const PING_TEXT = JSON.stringify({ type: 'ping' });
const CREATURE_MAP_MODE = 'creature';
const CREATURE_MAP_VERSION = 1;

function hasPlayerLimit() {
    return Number.isInteger(CFG.MAX_PLAYERS) && CFG.MAX_PLAYERS > 0;
}

function hasConnectionLimitPerIp() {
    return Number.isInteger(CFG.MAX_CONNS_PER_IP) && CFG.MAX_CONNS_PER_IP > 0;
}

class Session {
    constructor(code, serverName = 'Public Server', options = {}) {
        this.code      = code;
        this.serverName = normalizeServerName(serverName);
        this.official  = Boolean(options.official);
        /** @type {Map<number, import('ws')>} */
        this.players   = new Map(); // playerId → ws
        this.lobbies   = new Map(); // lobbyId -> Set<playerId>
        this.clans     = new Map(); // clanId -> clan
        this.playerClans = new Map(); // playerId -> clanId
        this.ready     = new Map(); // playerId -> bool
        this.nextId    = 1;         // 0 = host (creator)
        this.nextClanId = 1;
        this.createdAt = Date.now();
        this.creatureMap = generateCreatureMap();
    }

    get hostId() {
        let min = Infinity;
        for (const id of this.players.keys()) if (id < min) min = id;
        return min === Infinity ? -1 : min;
    }

    get size() { return this.players.size; }

    get isExpired() { return !this.official && Date.now() - this.createdAt > CFG.SESSION_TTL_MS; }

    add(ws) {
        if (this.official && this.size === 0)
            this.creatureMap = generateCreatureMap();

        const id = this.size === 0 ? 0 : this.nextId++;
        this.players.set(id, ws);
        this.ready.set(id, id === 0);
        ws._session  = this;
        ws._playerId = id;
        ws._lobbyId  = 'session';
        this.moveToLobby(id, ws._lobbyId);
        return id;
    }

    remove(ws) {
        const playerId = ws._playerId;
        this.players.delete(playerId);
        this.ready.delete(playerId);
        this.removeFromLobby(playerId, ws._lobbyId);
        const clanId = this.playerClans.get(playerId);
        if (clanId == null) return;

        const clan = this.clans.get(clanId);
        clan?.members.delete(playerId);
        this.playerClans.delete(playerId);

        if (!clan || clan.members.size === 0 || clan.leaderPlayerId === playerId) {
            this.clans.delete(clanId);
            for (const [memberId, memberClanId] of this.playerClans) {
                if (memberClanId === clanId) this.playerClans.delete(memberId);
            }
        }
    }

    createClan(leaderPlayerId, name, tag) {
        if (this.playerClans.has(leaderPlayerId)) return null;
        const normalizedName = name.trim();
        const normalizedTag = tag.trim().toUpperCase();
        for (const clan of this.clans.values()) {
            if (clan.tag === normalizedTag) return null;
        }

        const clan = {
            clanId: this.nextClanId++,
            name: normalizedName,
            tag: normalizedTag,
            leaderPlayerId,
            members: new Set([leaderPlayerId]),
        };
        this.clans.set(clan.clanId, clan);
        this.playerClans.set(leaderPlayerId, clan.clanId);
        return clan;
    }

    setReady(playerId, isReady) {
        if (!this.players.has(playerId)) return false;
        this.ready.set(playerId, Boolean(isReady));
        return true;
    }

    moveToLobby(playerId, lobbyId) {
        if (!this.players.has(playerId) || !validateLobbyId(lobbyId)) return false;
        const client = this.players.get(playerId);
        this.removeFromLobby(playerId, client?._lobbyId);

        let lobby = this.lobbies.get(lobbyId);
        if (!lobby) {
            lobby = new Set();
            this.lobbies.set(lobbyId, lobby);
        }

        lobby.add(playerId);
        if (client) client._lobbyId = lobbyId;
        return true;
    }

    removeFromLobby(playerId, lobbyId) {
        if (!lobbyId) return;
        const lobby = this.lobbies.get(lobbyId);
        if (!lobby) return;
        lobby.delete(playerId);
        if (lobby.size === 0) this.lobbies.delete(lobbyId);
    }

    allReady() {
        if (this.players.size === 0) return false;
        for (const playerId of this.players.keys()) {
            if (!this.ready.get(playerId)) return false;
        }
        return true;
    }

    broadcast(excludeId, msg) {
        const text = JSON.stringify(msg);
        for (const [id, client] of this.players) {
            if (id === excludeId) continue;
            if (client.readyState === OPEN) client.send(text);
        }
    }

    broadcastToLobby(lobbyId, excludeId, msg) {
        const lobby = this.lobbies.get(lobbyId);
        if (!lobby) return;

        const text = JSON.stringify(msg);
        for (const id of lobby) {
            if (id === excludeId) continue;
            const client = this.players.get(id);
            if (client?.readyState === OPEN) client.send(text);
        }
    }

    isPlayerInLobby(playerId, lobbyId) {
        return this.lobbies.get(lobbyId)?.has(playerId) ?? false;
    }

    sendTo(toId, msg) {
        const client = this.players.get(toId);
        if (client?.readyState === OPEN) client.send(JSON.stringify(msg));
    }

    /** Forcibly close all sockets (called on TTL expiry). */
    destroy(reason) {
        const msg = JSON.stringify({ type: 'error', reason });
        for (const client of this.players.values()) {
            if (client.readyState === OPEN) client.send(msg);
            client.terminate();
        }
        this.players.clear();
        this.lobbies.clear();
    }
}

// ---- helpers ----

function genCode() {
    for (let attempt = 0; attempt < 100; attempt++) {
        let code = '';
        for (let i = 0; i < 6; i++) {
            code += CODE_CHARS[Math.floor(Math.random() * CODE_CHARS.length)];
        }
        if (!sessions.has(code)) return code;
    }
    throw new Error('Failed to generate unique session code'); // extremely unlikely
}

function normalizeCreatureMapSeed(seed) {
    const parsed = Number(seed);
    if (!Number.isFinite(parsed)) return 1;

    const integer = Math.trunc(Math.abs(parsed));
    const normalized = integer % 2147483647;
    return normalized === 0 ? 1 : normalized;
}

function generateCreatureMapSeed() {
    if (CFG.CREATURE_MAP_SEED !== '') {
        return normalizeCreatureMapSeed(CFG.CREATURE_MAP_SEED);
    }

    return Math.floor(Math.random() * 2147483646) + 1;
}

function seededRandom(seed) {
    let state = normalizeCreatureMapSeed(seed) >>> 0;
    return () => {
        state += 0x6D2B79F5;
        let t = state;
        t = Math.imul(t ^ (t >>> 15), t | 1);
        t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
        return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
}

function roundNumber(value, places = 3) {
    const multiplier = Math.pow(10, places);
    return Math.round(value * multiplier) / multiplier;
}

function range(rng, min, max, places = 3) {
    return roundNumber(min + (max - min) * rng(), places);
}

function clampNumber(value, fallback, min, max) {
    const parsed = Number(value);
    if (!Number.isFinite(parsed)) return fallback;
    return Math.min(max, Math.max(min, parsed));
}

function landmarkScaleForType(type, rng) {
    switch (type) {
        case 'HighGround':
            return {
                x: range(rng, 6.2, 9.4),
                y: range(rng, 0.7, 1.15),
                z: range(rng, 4.8, 7.6),
            };
        case 'NarrowPassage':
            return {
                x: range(rng, 1.7, 2.8),
                y: range(rng, 1.6, 2.4),
                z: range(rng, 7.2, 10.5),
            };
        case 'HealingZone':
            return {
                x: range(rng, 4.2, 6.1),
                y: range(rng, 0.1, 0.18),
                z: range(rng, 4.2, 6.1),
            };
        default:
            return {
                x: range(rng, 3.2, 5.8),
                y: range(rng, 1.1, 1.9),
                z: range(rng, 2.6, 5.2),
            };
    }
}

function generateCreatureMap(seed = generateCreatureMapSeed()) {
    const normalizedSeed = normalizeCreatureMapSeed(seed);
    const rng = seededRandom(normalizedSeed);
    const radius = clampNumber(CFG.CREATURE_MAP_RADIUS, 48, 20, 120);
    const islandResolution = Math.trunc(clampNumber(CFG.CREATURE_MAP_RESOLUTION, 72, 16, 160));
    const islandHeight = clampNumber(CFG.CREATURE_MAP_HEIGHT, 8, 1, 32);
    const islandNoiseScale = clampNumber(CFG.CREATURE_MAP_NOISE_SCALE, 0.055, 0.005, 0.25);
    const landmarkCount = Math.trunc(clampNumber(CFG.CREATURE_MAP_LANDMARKS, 7, 3, 16));
    const landmarkPattern = ['HealingZone', 'Cover', 'Cover', 'HighGround', 'NarrowPassage', 'Cover', 'HighGround'];
    const landmarks = [];

    for (let i = 0; i < landmarkCount; i++) {
        const type = landmarkPattern[i % landmarkPattern.length];
        const angle = rng() * Math.PI * 2;
        const distance = range(rng, radius * 0.12, radius * 0.72);
        const scale = landmarkScaleForType(type, rng);
        landmarks.push({
            type,
            x: roundNumber(Math.cos(angle) * distance),
            z: roundNumber(Math.sin(angle) * distance),
            sx: scale.x,
            sy: scale.y,
            sz: scale.z,
        });
    }

    return {
        mode: CREATURE_MAP_MODE,
        version: CREATURE_MAP_VERSION,
        seed: normalizedSeed,
        arenaRadius: roundNumber(radius),
        islandResolution,
        islandHeight: roundNumber(islandHeight),
        islandNoiseScale: roundNumber(islandNoiseScale, 5),
        landmarks,
    };
}

function formatMapNumber(value, places = 3) {
    return Number(value).toFixed(places).replace(/\.?0+$/, '');
}

function encodeCreatureMap(map) {
    if (!map || map.mode !== CREATURE_MAP_MODE) return '';

    const header = [
        map.mode,
        map.version,
        map.seed,
        formatMapNumber(map.arenaRadius),
        map.islandResolution,
        formatMapNumber(map.islandHeight),
        formatMapNumber(map.islandNoiseScale, 5),
    ].join(',');

    const landmarks = (map.landmarks ?? []).map(landmark => [
        landmark.type,
        formatMapNumber(landmark.x),
        formatMapNumber(landmark.z),
        formatMapNumber(landmark.sx),
        formatMapNumber(landmark.sy),
        formatMapNumber(landmark.sz),
    ].join(','));

    return [header, ...landmarks].join(';');
}

function log(level, ...parts) {
    const ts = new Date().toISOString();
    // eslint-disable-next-line no-console
    console[level === 'error' ? 'error' : level === 'warn' ? 'warn' : 'log'](
        `[${ts}] [relay] [${level}]`, ...parts
    );
}

function send(ws, obj) {
    if (ws.readyState === OPEN) ws.send(JSON.stringify(obj));
}

function sendCreatureMap(ws, session) {
    if (!session?.creatureMap) return;
    send(ws, { type: 'creature_map', map: encodeCreatureMap(session.creatureMap) });
}

function closeUnauthorized(ws) {
    // WebSocket 1008 = Policy Violation
    ws.close(1008, 'Unauthorized');
}

function trackConn(ip, ws) {
    let set = connsByIp.get(ip);
    if (!set) { set = new Set(); connsByIp.set(ip, set); }
    set.add(ws);
}

function untrackConn(ip, ws) {
    const set = connsByIp.get(ip);
    if (!set) return;
    set.delete(ws);
    if (set.size === 0) connsByIp.delete(ip);
}

function connCountForIp(ip) {
    return connsByIp.get(ip)?.size ?? 0;
}

function normalizeServerName(name) {
    return validateServerName(name) ? name.trim() : 'Public Server';
}

function parseOfficialLobbySpecs(raw = CFG.OFFICIAL_LOBBIES) {
    if (typeof raw !== 'string' || raw.trim().length === 0) return [];

    return raw
        .split(';')
        .map(part => part.trim())
        .filter(Boolean)
        .map(part => {
            const separator = part.indexOf(':');
            const code = (separator >= 0 ? part.slice(0, separator) : part).trim().toUpperCase();
            const serverName = separator >= 0 ? part.slice(separator + 1).trim() : 'Official Lobby';
            return { code, serverName };
        })
        .filter(spec => validateCode(spec.code));
}

function ensureOfficialSessions(specs = parseOfficialLobbySpecs()) {
    for (const spec of specs) {
        let session = sessions.get(spec.code);
        if (session) {
            session.official = true;
            session.serverName = normalizeServerName(spec.serverName);
            continue;
        }

        session = new Session(spec.code, spec.serverName, { official: true });
        sessions.set(session.code, session);
    }
}

function getPublicLobbies() {
    ensureOfficialSessions();

    const lobbies = [];
    for (const session of sessions.values()) {
        if (session.isExpired || (!session.official && session.size <= 0)) continue;
        lobbies.push({
            code: session.code,
            serverName: session.serverName,
            players: session.size,
            maxPlayers: CFG.MAX_PLAYERS,
        });
    }

    lobbies.sort((a, b) => a.code.localeCompare(b.code));
    return lobbies;
}

function encodePublicLobbies(lobbies = getPublicLobbies()) {
    return lobbies
        .map(lobby => `${lobby.code},${lobby.players},${lobby.maxPlayers},${encodeURIComponent(lobby.serverName)}`)
        .join(';');
}

function sanitizePlayerKey(value) {
    if (typeof value !== 'string') return null;
    const trimmed = value.trim();
    if (!trimmed) return null;
    const safe = trimmed.replace(/[^A-Za-z0-9_.-]/g, '_');
    return safe.length > 0 ? safe.slice(0, 128) : null;
}

function sendJson(res, statusCode, body) {
    res.writeHead(statusCode, {
        'content-type': 'application/json',
        'cache-control': 'no-store',
    });
    res.end(JSON.stringify(body));
}

function handleAutosaveRequest(req, res) {
    if (req.method !== 'POST') {
        return sendJson(res, 405, { ok: false, reason: 'method_not_allowed' });
    }

    const authHeader = req.headers['authorization'];
    const token = typeof authHeader === 'string' && authHeader.startsWith('Bearer ')
        ? authHeader.slice('Bearer '.length).trim()
        : '';
    if (!validateAuthToken(token, CFG.GAME_TOKEN || null)) {
        return sendJson(res, 401, { ok: false, reason: 'unauthorized' });
    }

    const playerHeader = req.headers['x-player-id'];
    const playerId = sanitizePlayerKey(Array.isArray(playerHeader) ? playerHeader[0] : playerHeader);
    if (!playerId) {
        return sendJson(res, 400, { ok: false, reason: 'missing_player_id' });
    }

    let body = '';
    req.on('data', (chunk) => {
        body += chunk;
        if (body.length > CFG.MAX_AUTOSAVE_BYTES) {
            req.destroy();
        }
    });

    req.on('end', () => {
        try {
            const parsed = JSON.parse(body);
            if (!parsed || typeof parsed !== 'object') {
                return sendJson(res, 400, { ok: false, reason: 'invalid_payload' });
            }

            fs.mkdirSync(CFG.AUTOSAVE_DIR, { recursive: true });
            const outputPath = path.join(CFG.AUTOSAVE_DIR, `${playerId}.json`);
            fs.writeFileSync(outputPath, JSON.stringify(parsed, null, 2), 'utf8');
            return sendJson(res, 200, { ok: true });
        } catch {
            return sendJson(res, 400, { ok: false, reason: 'invalid_json' });
        }
    });

    req.on('error', () => {
        sendJson(res, 500, { ok: false, reason: 'request_error' });
    });
}

function handleAutosaveLookupRequest(req, res) {
    if (req.method !== 'GET') {
        return sendJson(res, 405, { ok: false, reason: 'method_not_allowed' });
    }

    const authHeader = req.headers['authorization'];
    const token = typeof authHeader === 'string' && authHeader.startsWith('Bearer ')
        ? authHeader.slice('Bearer '.length).trim()
        : '';
    if (!validateAuthToken(token, CFG.GAME_TOKEN || null)) {
        return sendJson(res, 401, { ok: false, reason: 'unauthorized' });
    }

    const match = /^\/api\/progress\/autosave\/([^/?#]+)$/.exec(req.url || '');
    const playerId = sanitizePlayerKey(match ? decodeURIComponent(match[1]) : '');
    if (!playerId) {
        return sendJson(res, 400, { ok: false, reason: 'missing_player_id' });
    }

    try {
        const inputPath = path.join(CFG.AUTOSAVE_DIR, `${playerId}.json`);
        if (!fs.existsSync(inputPath)) {
            return sendJson(res, 404, { ok: false, reason: 'not_found' });
        }

        const json = fs.readFileSync(inputPath, 'utf8');
        const parsed = JSON.parse(json);
        return sendJson(res, 200, { ok: true, data: parsed });
    } catch {
        return sendJson(res, 500, { ok: false, reason: 'read_failed' });
    }
}

// ---- WebSocket server ----

const server = http.createServer((req, res) => {
    if (req.url === '/api/progress/autosave') {
        handleAutosaveRequest(req, res);
        return;
    }
    if ((req.url || '').startsWith('/api/progress/autosave/')) {
        handleAutosaveLookupRequest(req, res);
        return;
    }

    if (req.url === '/healthz' || req.url === '/') {
        const body = {
            ok: true,
            sessions: sessions.size,
            clients: wss.clients.size,
        };
        sendJson(res, 200, body);
        return;
    }

    res.writeHead(404, { 'content-type': 'text/plain' });
    res.end('Not found');
});

const wss = new WebSocketServer({
    server,
    maxPayload: CFG.MAX_PAYLOAD_BYTES,
});

const devMode = !CFG.GAME_TOKEN;
let housekeepingTimer = null;

function startRelayServer() {
    if (server.listening) return;
    ensureOfficialSessions();
    server.listen(CFG.PORT, () => {
        log('info', `RoobsWorld relay server listening on ws://0.0.0.0:${CFG.PORT}${devMode ? ' (dev mode - no token required)' : ''}`);
    });
}

// ---- keepalive + housekeeping ----

function startHousekeeping() {
    if (housekeepingTimer) return;
    housekeepingTimer = setInterval(() => {
    const now  = Date.now();

    for (const ws of wss.clients) {
        if (ws.readyState !== OPEN) continue;

        // Idle timeout
        if (now - ws._lastActivity > CFG.IDLE_MS) {
            log('info', `idle timeout ip=${ws._ip}`);
            ws.terminate();
            continue;
        }

        ws.send(PING_TEXT);
    }

    // Expired sessions
    for (const [code, session] of sessions) {
        if (session.isExpired) {
            log('info', `session TTL expired code=${code}`);
            session.destroy('session_expired');
            sessions.delete(code);
        }
    }
    ensureOfficialSessions();

    // Rate limiter memory
    connRateLimiter.cleanup();
    msgRateLimiter.cleanup();
    chatRateLimiter.cleanup();
    joinRateLimiter.cleanup();
    }, CFG.PING_MS);
}

// ---- connection handler ----

wss.on('connection', (ws, req) => {
    const ip = req.socket.remoteAddress ?? 'unknown';

    // ---- auth token ----
    const clientToken = extractQueryParam(req.url, 'token');
    if (!validateAuthToken(clientToken, CFG.GAME_TOKEN || null)) {
        log('warn', `rejected unauthorized ip=${ip}`);
        closeUnauthorized(ws);
        return;
    }

    // ---- connection rate ----
    if (!connRateLimiter.check(ip)) {
        log('warn', `connection rate exceeded ip=${ip}`);
        ws.close(1008, 'Rate limited');
        return;
    }

    // ---- per-IP connection cap ----
    if (hasConnectionLimitPerIp() && connCountForIp(ip) >= CFG.MAX_CONNS_PER_IP) {
        log('warn', `connection cap exceeded ip=${ip}`);
        ws.close(1008, 'Too many connections');
        return;
    }

    // ---- init socket state ----
    ws._ip           = ip;
    ws._session      = null;
    ws._playerId     = -1;
    ws._lobbyId      = null;
    ws._lastActivity = Date.now();

    trackConn(ip, ws);
    log('info', `connect ip=${ip} total=${wss.clients.size}`);

    // ---- message handler ----
    ws.on('message', (raw) => {
        ws._lastActivity = Date.now();

        // Per-client message rate limit
        if (!msgRateLimiter.check(ip)) {
            log('warn', `message rate exceeded ip=${ip}`);
            ws.close(1008, 'Rate limited');
            return;
        }

        let msg;
        try { msg = JSON.parse(raw); } catch { return; } // drop malformed JSON silently

        // type must be a non-empty string from a known set
        if (typeof msg.type !== 'string') return;

        switch (msg.type) {

            // ---- public lobby discovery ----
            case 'list_public_lobbies':
                send(ws, { type: 'public_lobbies', lobbies: encodePublicLobbies() });
                break;

            // ---- create session ----
            case 'create': {
                if (ws._session) {
                    return send(ws, { type: 'error', reason: 'already_in_session' });
                }
                const code    = genCode();
                const session = new Session(code, msg.serverName);
                sessions.set(code, session);
                const playerId = session.add(ws);
                log('info', `created code=${code} name=${session.serverName} ip=${ip}`);
                send(ws, { type: 'created', code, playerId, serverName: session.serverName });
                sendCreatureMap(ws, session);
                break;
            }

            // ---- join session ----
            case 'join': {
                if (ws._session) {
                    return send(ws, { type: 'error', reason: 'already_in_session' });
                }
                if (!validateCode(msg.code)) {
                    return send(ws, { type: 'error', reason: 'invalid_code' });
                }
                // Rate-limit join attempts to prevent code brute-forcing
                if (!joinRateLimiter.check(ip)) {
                    log('warn', `join rate exceeded ip=${ip}`);
                    return send(ws, { type: 'error', reason: 'rate_limited' });
                }
                const session = sessions.get(msg.code);
                if (!session) {
                    return send(ws, { type: 'error', reason: 'not_found' });
                }
                if (hasPlayerLimit() && session.size >= CFG.MAX_PLAYERS) {
                    return send(ws, { type: 'error', reason: 'full' });
                }
                const playerId = session.add(ws);
                log('info', `joined code=${msg.code} player=${playerId} ip=${ip}`);
                send(ws, { type: 'joined', code: msg.code, playerId, hostPlayerId: session.hostId });
                sendCreatureMap(ws, session);
                for (const [existingId] of session.players) {
                    if (existingId === playerId) continue;
                    send(ws, { type: 'player_joined', playerId: existingId });
                    send(ws, {
                        type: 'player_ready',
                        playerId: existingId,
                        isReady: Boolean(session.ready.get(existingId)),
                    });
                }
                session.broadcast(playerId, { type: 'player_joined', playerId });
                session.broadcast(-1, { type: 'player_ready', playerId, isReady: false });
                for (const clan of session.clans.values()) {
                    const clanPayload = {
                        type: 'clan_created',
                        clanId: clan.clanId,
                        name: clan.name,
                        tag: clan.tag,
                        leaderPlayerId: clan.leaderPlayerId,
                    };
                    send(ws, clanPayload);
                    send(ws, {
                        ...clanPayload,
                        type: 'party_created',
                    });
                }
                for (const [existingId, client] of session.players) {
                    send(ws, {
                        type: 'player_lobby',
                        playerId: existingId,
                        lobbyId: client._lobbyId ?? 'session',
                    });
                }
                break;
            }

            // ---- bandwidth lobby ----
            case 'join_lobby': {
                const session = ws._session;
                if (!session) return;
                if (!validateLobbyId(msg.lobbyId)) {
                    return send(ws, { type: 'error', reason: 'invalid_lobby' });
                }

                session.moveToLobby(ws._playerId, msg.lobbyId);
                session.broadcast(-1, {
                    type: 'player_lobby',
                    playerId: ws._playerId,
                    lobbyId: ws._lobbyId,
                });
                log('info', `lobby changed code=${session.code} player=${ws._playerId} lobby=${ws._lobbyId}`);
                break;
            }

            // ---- create clan / party ----
            case 'create_clan':
            case 'create_party': {
                const session = ws._session;
                const isPartyRequest = msg.type === 'create_party';
                if (!session) return;
                if (!validateClanName(msg.name) || !validateClanTag(msg.tag)) {
                    return send(ws, { type: 'error', reason: isPartyRequest ? 'invalid_party' : 'invalid_clan' });
                }
                const clan = session.createClan(ws._playerId, msg.name, msg.tag);
                if (!clan) {
                    return send(ws, { type: 'error', reason: isPartyRequest ? 'already_in_party' : 'already_in_clan' });
                }
                const clanPayload = {
                    type: 'clan_created',
                    clanId: clan.clanId,
                    name: clan.name,
                    tag: clan.tag,
                    leaderPlayerId: clan.leaderPlayerId,
                };
                const partyPayload = {
                    ...clanPayload,
                    type: 'party_created',
                };
                log('info', `${isPartyRequest ? 'party' : 'clan'} created code=${session.code} clan=${clan.tag} player=${ws._playerId}`);
                session.broadcast(-1, clanPayload);
                session.broadcast(-1, partyPayload);
                break;
            }

            // ---- lobby readiness ----
            case 'ready': {
                const session = ws._session;
                if (!session || typeof msg.isReady !== 'boolean') return;
                session.setReady(ws._playerId, msg.isReady);
                session.broadcast(-1, {
                    type: 'player_ready',
                    playerId: ws._playerId,
                    isReady: msg.isReady,
                });
                break;
            }

            // ---- host-authoritative game start ----
            case 'start_game': {
                const session = ws._session;
                if (!session) return;
                if (ws._playerId !== session.hostId) {
                    return send(ws, { type: 'error', reason: 'not_host' });
                }
                if (!session.allReady()) {
                    return send(ws, { type: 'error', reason: 'not_ready' });
                }
                log('info', `game started code=${session.code} host=${ws._playerId}`);
                session.broadcast(-1, { type: 'creature_map', map: encodeCreatureMap(session.creatureMap) });
                session.broadcast(-1, { type: 'game_started' });
                break;
            }

            // ---- server-filtered global chat ----
            case 'chat': {
                const session = ws._session;
                if (!session) {
                    return send(ws, { type: 'error', reason: 'not_in_session' });
                }

                const chatKey = `${ip}:${session.code}:${ws._playerId}`;
                if (!chatRateLimiter.check(chatKey)) {
                    log('warn', `chat rate exceeded code=${session.code} player=${ws._playerId} ip=${ip}`);
                    return send(ws, { type: 'error', reason: 'chat_rate_limited' });
                }

                if (!validateChatMessage(msg.message, CFG.MAX_CHAT_CHARS)) {
                    return send(ws, { type: 'error', reason: 'invalid_chat' });
                }

                const message = filterProfanity(
                    normalizeChatMessage(msg.message),
                    CFG.CHAT_PROFANITY_WORDS
                );
                session.broadcast(-1, {
                    type: 'chat',
                    from: ws._playerId,
                    message,
                });
                break;
            }

            // ---- relay packet ----
            case 'relay': {
                const session = ws._session;
                if (!session) return;

                if (!validateBase64(msg.data, CFG.MAX_RELAY_BYTES)) return;
                if (!validateRelayTarget(msg.to)) return;

                const from = ws._playerId;
                if (msg.to != null) {
                    if (!session.isPlayerInLobby(msg.to, ws._lobbyId)) return;
                    session.sendTo(msg.to, { type: 'data', from, data: msg.data });
                } else {
                    session.broadcastToLobby(ws._lobbyId, from, { type: 'data', from, data: msg.data });
                }
                break;
            }

            // ---- leave ----
            case 'leave':
                handleLeave(ws);
                break;

            case 'pong':
                break; // keepalive — already updated _lastActivity above

            // drop unknown types silently (no info leak about valid types)
        }
    });

    ws.on('close', () => {
        log('info', `close ip=${ip}`);
        untrackConn(ip, ws);
        handleLeave(ws);
    });

    ws.on('error', (err) => {
        log('error', `socket error ip=${ip} msg=${err.message}`);
    });
});

// ---- leave / disconnect ----

function handleLeave(ws) {
    const session = ws._session;
    if (!session) return;

    const playerId = ws._playerId;
    const wasHost  = playerId === session.hostId;
    session.remove(ws);
    ws._session = null;
    log('info', `left code=${session.code} player=${playerId} wasHost=${wasHost}`);

    if (session.size === 0) {
        if (!session.official) {
            sessions.delete(session.code);
            log('info', `destroyed code=${session.code}`);
        }
        return;
    }

    session.broadcast(-1, { type: 'player_left', playerId, wasHost });

    if (wasHost) {
        const newHostId = session.hostId;
        log('info', `migrated code=${session.code} newHost=${newHostId}`);
        session.broadcast(-1, { type: 'host_migrated', newHostId });
    }
}

// ---- graceful shutdown ----

function shutdown(signal) {
    log('info', `${signal} — shutting down`);
    if (housekeepingTimer) clearInterval(housekeepingTimer);
    for (const session of sessions.values()) session.destroy('server_shutdown');
    wss.close(() => server.close(() => process.exit(0)));
    setTimeout(() => process.exit(1), 5000); // force-exit if close hangs
}

if (require.main === module) {
    startRelayServer();
    startHousekeeping();
    process.on('SIGTERM', () => shutdown('SIGTERM'));
    process.on('SIGINT',  () => shutdown('SIGINT'));
}

// Export for testing
module.exports = {
    sessions,
    Session,
    genCode,
    CFG,
    server,
    wss,
    startRelayServer,
    startHousekeeping,
    handleLeave,
    parseOfficialLobbySpecs,
    ensureOfficialSessions,
    getPublicLobbies,
    encodePublicLobbies,
    generateCreatureMap,
    encodeCreatureMap,
    normalizeCreatureMapSeed,
};
