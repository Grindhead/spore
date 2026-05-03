'use strict';

/**
 * RoobsWorld Multiplayer Relay Server — hardened edition
 *
 * Pure packet relay: no game logic runs here.
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
 *     { type: "create" }
 *     { type: "join",    code: "ABCD12" }
 *     { type: "join_lobby", lobbyId: "cell" }
 *     { type: "relay",   to: <playerId|null>,  data: "<base64 ≤16 KB>" }
 *     { type: "leave" }
 *     { type: "pong" }    ← keepalive response
 *
 *   Server → Client:
 *     { type: "created",       code, playerId }
 *     { type: "joined",        code, playerId, hostPlayerId }
 *     { type: "player_joined", playerId }
 *     { type: "player_left",   playerId, wasHost }
 *     { type: "host_migrated", newHostId }
 *     { type: "player_lobby",  playerId, lobbyId }
 *     { type: "data",          from, data }
 *     { type: "ping" }
 *     { type: "error",         reason: "not_found"|"full"|"already_in_session"|"invalid_code" }
 */

const http = require('http');
const { WebSocketServer } = require('ws');
const {
    RateLimiter,
    validateCode,
    validateClanName,
    validateClanTag,
    validateBase64,
    validateRelayTarget,
    validateLobbyId,
    validateAuthToken,
    extractQueryParam,
} = require('./security');

// ---- configuration (all overridable via environment variables) ----

const CFG = {
    PORT:               parseInt(process.env.PORT               ?? '7788'),
    MAX_PLAYERS:        parseInt(process.env.MAX_PLAYERS        ?? '4'),
    GAME_TOKEN:              process.env.GAME_TOKEN             ?? '',   // empty = dev mode (open)
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
    MAX_CONNS_PER_IP:   parseInt(process.env.MAX_CONNS_PER_IP   ?? '4'),    // 4 simultaneous per IP
    MSG_RATE_WINDOW_MS: parseInt(process.env.MSG_RATE_WINDOW    ?? '1000'), // 1-second window
    MSG_RATE_MAX:       parseInt(process.env.MSG_RATE_MAX       ?? '120'),  // gameplay input + voice chunks
    JOIN_RATE_WINDOW_MS:parseInt(process.env.JOIN_RATE_WINDOW   ?? '60000'),
    JOIN_RATE_MAX:      parseInt(process.env.JOIN_RATE_MAX      ?? '10'),   // 10 join attempts/min per IP
};

// ---- rate limiters ----

const connRateLimiter = new RateLimiter({ windowMs: CFG.CONN_RATE_WINDOW_MS, max: CFG.CONN_RATE_MAX });
const msgRateLimiter  = new RateLimiter({ windowMs: CFG.MSG_RATE_WINDOW_MS,  max: CFG.MSG_RATE_MAX  });
const joinRateLimiter = new RateLimiter({ windowMs: CFG.JOIN_RATE_WINDOW_MS, max: CFG.JOIN_RATE_MAX });

// ---- session store ----

/** @type {Map<string, Session>} */
const sessions = new Map();

/** @type {Map<string, Set<import('ws')>>} */
const connsByIp = new Map(); // ip → Set<ws>

const CODE_CHARS = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';

class Session {
    constructor(code) {
        this.code      = code;
        /** @type {Map<number, import('ws')>} */
        this.players   = new Map(); // playerId → ws
        this.lobbies   = new Map(); // lobbyId -> Set<playerId>
        this.clans     = new Map(); // clanId -> clan
        this.playerClans = new Map(); // playerId -> clanId
        this.ready     = new Map(); // playerId -> bool
        this.nextId    = 1;         // 0 = host (creator)
        this.nextClanId = 1;
        this.createdAt = Date.now();
    }

    get hostId() {
        let min = Infinity;
        for (const id of this.players.keys()) if (id < min) min = id;
        return min === Infinity ? -1 : min;
    }

    get size() { return this.players.size; }

    get isExpired() { return Date.now() - this.createdAt > CFG.SESSION_TTL_MS; }

    add(ws) {
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
            if (client.readyState === 1) client.send(text);
        }
    }

    broadcastToLobby(lobbyId, excludeId, msg) {
        const lobby = this.lobbies.get(lobbyId);
        if (!lobby) return;

        const text = JSON.stringify(msg);
        for (const id of lobby) {
            if (id === excludeId) continue;
            const client = this.players.get(id);
            if (client?.readyState === 1) client.send(text);
        }
    }

    isPlayerInLobby(playerId, lobbyId) {
        return this.lobbies.get(lobbyId)?.has(playerId) ?? false;
    }

    sendTo(toId, msg) {
        const client = this.players.get(toId);
        if (client?.readyState === 1) client.send(JSON.stringify(msg));
    }

    /** Forcibly close all sockets (called on TTL expiry). */
    destroy(reason) {
        const msg = JSON.stringify({ type: 'error', reason });
        for (const client of this.players.values()) {
            if (client.readyState === 1) client.send(msg);
            client.terminate();
        }
        this.players.clear();
        this.lobbies.clear();
    }
}

// ---- helpers ----

function genCode() {
    for (let attempt = 0; attempt < 100; attempt++) {
        const code = Array.from({ length: 6 }, () =>
            CODE_CHARS[Math.floor(Math.random() * CODE_CHARS.length)]
        ).join('');
        if (!sessions.has(code)) return code;
    }
    throw new Error('Failed to generate unique session code'); // extremely unlikely
}

function log(level, ...parts) {
    const ts = new Date().toISOString();
    // eslint-disable-next-line no-console
    console[level === 'error' ? 'error' : level === 'warn' ? 'warn' : 'log'](
        `[${ts}] [relay] [${level}]`, ...parts
    );
}

function send(ws, obj) {
    if (ws.readyState === 1) ws.send(JSON.stringify(obj));
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

// ---- WebSocket server ----

const server = http.createServer((req, res) => {
    if (req.url === '/healthz' || req.url === '/') {
        const body = JSON.stringify({
            ok: true,
            sessions: sessions.size,
            clients: wss.clients.size,
        });
        res.writeHead(200, {
            'content-type': 'application/json',
            'cache-control': 'no-store',
        });
        res.end(body);
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
    server.listen(CFG.PORT, () => {
        log('info', `RoobsWorld relay server listening on ws://0.0.0.0:${CFG.PORT}${devMode ? ' (dev mode - no token required)' : ''}`);
    });
}

// ---- keepalive + housekeeping ----

function startHousekeeping() {
    if (housekeepingTimer) return;
    housekeepingTimer = setInterval(() => {
    const ping = JSON.stringify({ type: 'ping' });
    const now  = Date.now();

    for (const ws of wss.clients) {
        if (ws.readyState !== 1) continue;

        // Idle timeout
        if (now - ws._lastActivity > CFG.IDLE_MS) {
            log('info', `idle timeout ip=${ws._ip}`);
            ws.terminate();
            continue;
        }

        ws.send(ping);
    }

    // Expired sessions
    for (const [code, session] of sessions) {
        if (session.isExpired) {
            log('info', `session TTL expired code=${code}`);
            session.destroy('session_expired');
            sessions.delete(code);
        }
    }

    // Rate limiter memory
    connRateLimiter.cleanup();
    msgRateLimiter.cleanup();
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
    if (connCountForIp(ip) >= CFG.MAX_CONNS_PER_IP) {
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

            // ---- create session ----
            case 'create': {
                if (ws._session) {
                    return send(ws, { type: 'error', reason: 'already_in_session' });
                }
                const code    = genCode();
                const session = new Session(code);
                sessions.set(code, session);
                const playerId = session.add(ws);
                log('info', `created code=${code} ip=${ip}`);
                send(ws, { type: 'created', code, playerId });
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
                if (session.size >= CFG.MAX_PLAYERS) {
                    return send(ws, { type: 'error', reason: 'full' });
                }
                const playerId = session.add(ws);
                log('info', `joined code=${msg.code} player=${playerId} ip=${ip}`);
                send(ws, { type: 'joined', code: msg.code, playerId, hostPlayerId: session.hostId });
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
                    send(ws, {
                        type: 'clan_created',
                        clanId: clan.clanId,
                        name: clan.name,
                        tag: clan.tag,
                        leaderPlayerId: clan.leaderPlayerId,
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

            // ---- create clan ----
            case 'create_clan': {
                const session = ws._session;
                if (!session) return;
                if (!validateClanName(msg.name) || !validateClanTag(msg.tag)) {
                    return send(ws, { type: 'error', reason: 'invalid_clan' });
                }
                const clan = session.createClan(ws._playerId, msg.name, msg.tag);
                if (!clan) {
                    return send(ws, { type: 'error', reason: 'already_in_clan' });
                }
                const payload = {
                    type: 'clan_created',
                    clanId: clan.clanId,
                    name: clan.name,
                    tag: clan.tag,
                    leaderPlayerId: clan.leaderPlayerId,
                };
                log('info', `clan created code=${session.code} clan=${clan.tag} player=${ws._playerId}`);
                session.broadcast(-1, payload);
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
                session.broadcast(-1, { type: 'game_started' });
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
        sessions.delete(session.code);
        log('info', `destroyed code=${session.code}`);
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
module.exports = { sessions, Session, genCode, CFG, server, wss, startRelayServer, startHousekeeping };
