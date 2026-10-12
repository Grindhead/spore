'use strict';

/**
 * Integration tests against the real HTTP + WebSocket relay.
 * Run with: npm test
 */

const { describe, it, before, after } = require('node:test');
const assert = require('node:assert/strict');
const { once } = require('node:events');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { WebSocket } = require('ws');

const relay = require('./relay-server');
const { ProgressStore, storageKey } = require('./progress-store');

const GAME_TOKEN = 'shared-game-token-for-tests';
let baseHttp;
let baseWs;
let autosaveDir;
const saved = {};

before(async () => {
    for (const key of ['GAME_TOKEN', 'AUTOSAVE_DIR', 'MSG_RATE_MAX', 'MSG_IP_RATE_MAX', 'OFFICIAL_LOBBIES'])
        saved[key] = relay.CFG[key];
    autosaveDir = fs.mkdtempSync(path.join(os.tmpdir(), 'spore-progress-'));
    relay.CFG.AUTOSAVE_DIR = autosaveDir;
    relay.CFG.GAME_TOKEN = GAME_TOKEN;
    relay.CFG.OFFICIAL_LOBBIES = 'PUBLIC:Official Test';
    relay.server.listen(0, '127.0.0.1');
    await once(relay.server, 'listening');
    const { port } = relay.server.address();
    baseHttp = `http://127.0.0.1:${port}`;
    baseWs = `ws://127.0.0.1:${port}/?token=${GAME_TOKEN}`;
});

after(async () => {
    for (const client of relay.wss.clients) client.terminate();
    await new Promise(resolve => relay.wss.close(() => relay.server.close(resolve)));
    Object.assign(relay.CFG, saved);
    relay.sessions.clear();
    fs.rmSync(autosaveDir, { recursive: true, force: true });
});

function request(urlPath, { token, method = 'GET', body, headers = {} } = {}) {
    return fetch(baseHttp + urlPath, {
        method,
        headers: { ...(token ? { authorization: `Bearer ${token}` } : {}), ...headers },
        body: body === undefined ? undefined : (typeof body === 'string' ? body : JSON.stringify(body)),
    });
}

async function register() {
    const res = await request('/api/progress/register', { token: GAME_TOKEN, method: 'POST' });
    assert.equal(res.status, 200);
    return res.json();
}

async function openSocket() {
    const socket = new WebSocket(baseWs);
    await once(socket, 'open');
    return socket;
}

function nextMessage(socket, type) {
    return new Promise((resolve, reject) => {
        const onMessage = (raw) => {
            const msg = JSON.parse(raw);
            if (type && msg.type !== type) return;
            socket.off('message', onMessage);
            socket.off('close', onClose);
            resolve(msg);
        };
        const onClose = (code) => {
            socket.off('message', onMessage);
            reject(new Error(`socket closed (${code}) before ${type}`));
        };
        socket.on('message', onMessage);
        socket.on('close', onClose);
    });
}

async function closeSocket(socket) {
    if (socket.readyState === WebSocket.CLOSED) return;
    const closed = once(socket, 'close');
    socket.close();
    await closed;
}

describe('malformed relay input', () => {
    it('drops null and non-object JSON messages and keeps serving the connection', async () => {
        const socket = await openSocket();
        for (const raw of ['null', '[]', 'true', '42', '"text"', '{', '{"type":null}'])
            socket.send(raw);

        const reply = nextMessage(socket, 'public_lobbies');
        socket.send(JSON.stringify({ type: 'list_public_lobbies' }));
        assert.equal((await reply).type, 'public_lobbies');

        // Gameplay still works after the bad input: create a session and relay to a peer.
        const created = nextMessage(socket, 'created');
        socket.send(JSON.stringify({ type: 'create', serverName: 'Tide Pool' }));
        const { code } = await created;

        const peer = await openSocket();
        peer.send('null');
        const joined = nextMessage(peer, 'joined');
        peer.send(JSON.stringify({ type: 'join', code }));
        await joined;

        const data = nextMessage(peer, 'data');
        socket.send('null');
        socket.send(JSON.stringify({ type: 'relay', to: null, data: Buffer.from('hi').toString('base64') }));
        assert.equal((await data).data, Buffer.from('hi').toString('base64'));

        await closeSocket(peer);
        await closeSocket(socket);
    });

    it('rejects malformed percent-encoding in progress URLs with 400 and the process keeps serving', async () => {
        const { credential } = await register();
        for (const bad of ['/api/progress/slots/%E0%A4%A', '/api/progress/slots/%', '/api/progress/autosave/%E0%A4%A']) {
            const res = await request(bad, { token: credential });
            assert.ok(res.status === 400 || res.status === 410, `${bad} -> ${res.status}`);
            assert.equal((await res.json()).ok, false);
        }

        const health = await request('/healthz');
        assert.equal(health.status, 200);

        const write = await request('/api/progress/slots/autosave', {
            token: credential, method: 'PUT', body: { slot: 'autosave', save: { DNAPoints: 1 } },
        });
        assert.equal(write.status, 200);
    });

    it('rejects oversized, non-JSON and non-object save payloads', async () => {
        const { credential } = await register();
        for (const body of ['{', 'null', '[]', '{"slot":"a"}', JSON.stringify({ slot: 'other', save: {} })]) {
            const res = await request('/api/progress/slots/a', { token: credential, method: 'PUT', body });
            assert.equal(res.status, 400, body);
        }
        const original = relay.CFG.MAX_AUTOSAVE_BYTES;
        relay.CFG.MAX_AUTOSAVE_BYTES = 64;
        try {
            const res = await request('/api/progress/slots/a', {
                token: credential, method: 'PUT', body: { save: { blob: 'x'.repeat(200) } },
            });
            assert.equal(res.status, 413);
        } finally {
            relay.CFG.MAX_AUTOSAVE_BYTES = original;
        }
    });
});

describe('per-connection message quota', () => {
    it('lets several same-IP clients each use their full budget, and limits only the abusive one', async () => {
        relay.CFG.MSG_RATE_MAX = 40;
        relay.CFG.MSG_IP_RATE_MAX = 1000;
        try {
            // New limiters read CFG at connection time.
            const sockets = [await openSocket(), await openSocket(), await openSocket()];
            for (let i = 0; i < 35; i++)
                for (const socket of sockets) socket.send(JSON.stringify({ type: 'pong' }));

            for (const socket of sockets) {
                const reply = nextMessage(socket, 'public_lobbies');
                socket.send(JSON.stringify({ type: 'list_public_lobbies' }));
                await reply;
                assert.equal(socket.readyState, WebSocket.OPEN);
            }

            const closed = once(sockets[0], 'close');
            for (let i = 0; i < 20; i++) sockets[0].send(JSON.stringify({ type: 'pong' }));
            const [code] = await closed;
            assert.equal(code, 1008);

            for (const socket of sockets.slice(1)) {
                assert.equal(socket.readyState, WebSocket.OPEN);
                const reply = nextMessage(socket, 'public_lobbies');
                socket.send(JSON.stringify({ type: 'list_public_lobbies' }));
                await reply;
                await closeSocket(socket);
            }
        } finally {
            relay.CFG.MSG_RATE_MAX = saved.MSG_RATE_MAX;
            relay.CFG.MSG_IP_RATE_MAX = saved.MSG_IP_RATE_MAX;
        }
    });
});

describe('player-bound progress authorization', () => {
    it('refuses the shared game token, forged headers and forged credentials', async () => {
        const alice = await register();
        const bob = await register();
        assert.notEqual(alice.playerId, bob.playerId);

        // Shared game token is not a player credential.
        assert.equal((await request('/api/progress/slots', { token: GAME_TOKEN })).status, 401);
        assert.equal((await request('/api/progress/slots/autosave', {
            token: GAME_TOKEN, method: 'PUT', body: { save: { x: 1 } },
        })).status, 401);

        // Registration itself requires the game token.
        assert.equal((await request('/api/progress/register', { method: 'POST' })).status, 401);

        // Alice writes; header naming bob is ignored.
        assert.equal((await request('/api/progress/slots/autosave', {
            token: alice.credential, method: 'PUT', headers: { 'x-player-id': bob.playerId },
            body: { slot: 'autosave', save: { owner: 'alice' } },
        })).status, 200);

        // Bob cannot see it.
        assert.equal((await request('/api/progress/slots/autosave', { token: bob.credential })).status, 404);

        // Forged credential: bob's id with alice's secret, or a truncated secret.
        const aliceSecret = alice.credential.split('.')[1];
        for (const forged of [`${bob.playerId}.${aliceSecret}`, alice.credential.slice(0, -1), alice.playerId, '']) {
            assert.equal((await request('/api/progress/slots/autosave', { token: forged })).status, 401);
        }

        // Retired caller-chosen-identity routes are gone.
        assert.equal((await request(`/api/progress/autosave/${alice.playerId}`, { token: GAME_TOKEN })).status, 410);
        assert.equal((await request('/api/progress/autosave', {
            token: GAME_TOKEN, method: 'POST', headers: { 'x-player-id': alice.playerId }, body: { save: {} },
        })).status, 410);

        const own = await (await request('/api/progress/slots/autosave', { token: alice.credential })).json();
        assert.equal(own.data.save.owner, 'alice');
    });

    it('derives collision-resistant storage keys and stores only a secret hash', async () => {
        assert.notEqual(storageKey('a_b'), storageKey('a.b'));
        assert.notEqual(storageKey('unknown-player'), storageKey('unknown_player'));
        const { playerId, credential } = await register();
        const credFile = path.join(autosaveDir, 'players', storageKey(playerId), 'credential.json');
        const stored = fs.readFileSync(credFile, 'utf8');
        assert.ok(!stored.includes(credential.split('.')[1]));
    });
});

describe('cloud save slots', () => {
    it('keeps named slots independent, tombstones deletes, and lists both states', async () => {
        const { credential } = await register();
        const put = (slot, save) => request(`/api/progress/slots/${slot}`, {
            token: credential, method: 'PUT', body: { slot, save },
        });
        assert.equal((await put('autosave', { n: 1 })).status, 200);
        assert.equal((await put('slot-2', { n: 2 })).status, 200);

        const a = await (await request('/api/progress/slots/autosave', { token: credential })).json();
        const b = await (await request('/api/progress/slots/slot-2', { token: credential })).json();
        assert.equal(a.data.save.n, 1);
        assert.equal(b.data.save.n, 2);
        assert.ok(a.data.savedAt);

        const del = await request('/api/progress/slots/autosave', { token: credential, method: 'DELETE' });
        assert.equal(del.status, 200);
        const gone = await request('/api/progress/slots/autosave', { token: credential });
        assert.equal(gone.status, 404);
        assert.equal((await gone.json()).reason, 'deleted');
        assert.equal((await request('/api/progress/slots/slot-2', { token: credential })).status, 200);

        const list = await (await request('/api/progress/slots', { token: credential })).json();
        assert.deepEqual(list.slots.map(s => [s.slot, Boolean(s.savedAt), Boolean(s.deletedAt)]), [
            ['autosave', false, true],
            ['slot-2', true, false],
        ]);

        // Saving again after delete revives the slot with the new data only.
        assert.equal((await put('autosave', { n: 3 })).status, 200);
        const revived = await (await request('/api/progress/slots/autosave', { token: credential })).json();
        assert.equal(revived.data.save.n, 3);
    });

    it('enforces the slot limit and rejects unsafe slot names', async () => {
        const { credential } = await register();
        assert.equal((await request('/api/progress/slots/..%2Fescape', { token: credential })).status, 400);
        assert.equal((await request(`/api/progress/slots/${'x'.repeat(65)}`, { token: credential })).status, 400);

        const original = relay.CFG.MAX_SAVE_SLOTS;
        relay.CFG.MAX_SAVE_SLOTS = 2;
        try {
            for (const slot of ['a', 'b'])
                assert.equal((await request(`/api/progress/slots/${slot}`, {
                    token: credential, method: 'PUT', body: { save: {} },
                })).status, 200);
            assert.equal((await request('/api/progress/slots/c', {
                token: credential, method: 'PUT', body: { save: {} },
            })).status, 409);
            // Overwriting an existing slot is still allowed at the limit.
            assert.equal((await request('/api/progress/slots/a', {
                token: credential, method: 'PUT', body: { save: { v: 2 } },
            })).status, 200);
        } finally {
            relay.CFG.MAX_SAVE_SLOTS = original;
        }
    });
});

describe('ProgressStore', () => {
    it('treats a tombstone newer than leftover data as deleted', () => {
        const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'spore-store-'));
        try {
            const store = new ProgressStore(dir);
            const { playerId } = store.register();
            store.writeSlot(playerId, 'a', { v: 1 });
            const paths = store.slotPaths(playerId, 'a');
            const leftover = fs.readFileSync(paths.data, 'utf8');
            store.deleteSlot(playerId, 'a');
            // Simulate a crash that left the old data file next to the tombstone.
            fs.writeFileSync(paths.data, leftover);
            assert.equal(store.readSlot(playerId, 'a').status, 'deleted');
            // Leftover temp files from an interrupted write are ignored.
            fs.writeFileSync(paths.data + '.abc.tmp', '{');
            assert.deepEqual(store.listSlots(playerId).map(s => s.slot), ['a']);
        } finally {
            fs.rmSync(dir, { recursive: true, force: true });
        }
    });
});
