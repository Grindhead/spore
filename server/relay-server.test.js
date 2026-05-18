'use strict';

/**
 * Server-side security unit tests.
 * Run with: npm test  (uses Node.js built-in test runner, Node 18+)
 */

const { describe, it, before, beforeEach } = require('node:test');
const assert = require('node:assert/strict');

const {
    RateLimiter,
    validateCode,
    validateClanName,
    validateServerName,
    validateClanTag,
    validateBase64,
    validatePlayerId,
    validateRelayTarget,
    validateLobbyId,
    normalizeChatMessage,
    validateChatMessage,
    filterProfanity,
    validateAuthToken,
    extractQueryParam,
} = require('./security');

// ---- RateLimiter ----

describe('RateLimiter', () => {
    it('allows events within limit', () => {
        const rl = new RateLimiter({ windowMs: 1000, max: 3 });
        assert.ok(rl.check('a'));
        assert.ok(rl.check('a'));
        assert.ok(rl.check('a'));
    });

    it('blocks when limit is reached', () => {
        const rl = new RateLimiter({ windowMs: 1000, max: 2 });
        rl.check('b'); rl.check('b');
        assert.equal(rl.check('b'), false);
    });

    it('different keys are tracked independently', () => {
        const rl = new RateLimiter({ windowMs: 1000, max: 1 });
        rl.check('x');
        assert.equal(rl.check('x'), false);
        assert.ok(rl.check('y'));
    });

    it('window resets after elapsed time', async () => {
        const rl = new RateLimiter({ windowMs: 50, max: 1 });
        rl.check('c');
        assert.equal(rl.check('c'), false);
        await new Promise(r => setTimeout(r, 60));
        assert.ok(rl.check('c'));
    });

    it('cleanup removes stale buckets', async () => {
        const rl = new RateLimiter({ windowMs: 50, max: 5 });
        rl.check('d');
        assert.equal(rl.size, 1);
        await new Promise(r => setTimeout(r, 60));
        rl.cleanup();
        assert.equal(rl.size, 0);
    });
});

// ---- validateCode ----

describe('validateCode', () => {
    it('accepts a valid 6-char code', () => {
        assert.ok(validateCode('ABCD12'));
        assert.ok(validateCode('ZZZZZZ'));
        assert.ok(validateCode('222222'));
    });

    it('rejects codes that are too short', () => {
        assert.equal(validateCode('ABC'), false);
    });

    it('rejects codes that are too long', () => {
        assert.equal(validateCode('ABCDE12'), false);
    });

    it('rejects lowercase letters', () => {
        assert.equal(validateCode('abcd12'), false);
    });

    it('rejects special characters', () => {
        assert.equal(validateCode('AB;D12'), false);
        assert.equal(validateCode('../etc'), false);
        assert.equal(validateCode('AB\0D12'), false);
    });

    it('rejects null and non-string types', () => {
        assert.equal(validateCode(null), false);
        assert.equal(validateCode(123456), false);
        assert.equal(validateCode(undefined), false);
    });
});

// ---- clan validators ----

describe('validateClanName', () => {
    it('accepts names in the allowed length range', () => {
        assert.ok(validateClanName('Tide Hunters'));
        assert.ok(validateClanName('  Spore Pack  '));
    });

    it('rejects names that are too short or too long', () => {
        assert.equal(validateClanName('ab'), false);
        assert.equal(validateClanName('x'.repeat(25)), false);
    });

    it('rejects non-string values', () => {
        assert.equal(validateClanName(null), false);
        assert.equal(validateClanName(42), false);
    });
});

describe('validateServerName', () => {
    it('accepts display-safe server names', () => {
        assert.ok(validateServerName('Tide Pool'));
        assert.ok(validateServerName('  Open Water  '));
    });

    it('rejects server names outside the allowed length', () => {
        assert.equal(validateServerName('ab'), false);
        assert.equal(validateServerName('x'.repeat(33)), false);
    });
});

describe('validateClanTag', () => {
    it('accepts uppercase alphanumeric tags between two and five chars', () => {
        assert.ok(validateClanTag('AB'));
        assert.ok(validateClanTag('A12Z'));
        assert.ok(validateClanTag('abc'));
    });

    it('rejects invalid tags', () => {
        assert.equal(validateClanTag('A'), false);
        assert.equal(validateClanTag('ABCDEF'), false);
        assert.equal(validateClanTag('A-B'), false);
        assert.equal(validateClanTag(null), false);
    });
});

// ---- validateBase64 ----

describe('validateBase64', () => {
    it('accepts valid base64 strings', () => {
        const b64 = Buffer.from('hello world').toString('base64');
        assert.ok(validateBase64(b64));
    });

    it('accepts empty string', () => {
        assert.ok(validateBase64(''));
    });

    it('rejects strings with invalid characters', () => {
        assert.equal(validateBase64('not!base64~'), false);
    });

    it('rejects strings exceeding byte limit', () => {
        // Generate a string whose encoded size would exceed limit
        const huge = 'A'.repeat(30_000); // ~22 500 decoded bytes > 16 384
        assert.equal(validateBase64(huge, 16_384), false);
    });

    it('accepts strings just within byte limit', () => {
        const data = Buffer.alloc(100).toString('base64');
        assert.ok(validateBase64(data, 200));
    });

    it('rejects non-string types', () => {
        assert.equal(validateBase64(null), false);
        assert.equal(validateBase64(42), false);
        assert.equal(validateBase64({ data: 'abc' }), false);
    });
});

// ---- validatePlayerId ----

describe('validatePlayerId', () => {
    it('accepts any non-negative integer id', () => {
        assert.ok(validatePlayerId(0));
        assert.ok(validatePlayerId(1));
        assert.ok(validatePlayerId(3));
        assert.ok(validatePlayerId(4));
        assert.ok(validatePlayerId(100));
    });

    it('rejects negative ids', () => {
        assert.equal(validatePlayerId(-1), false);
    });

    it('rejects non-integer numbers', () => {
        assert.equal(validatePlayerId(1.5), false);
    });

    it('rejects non-number types', () => {
        assert.equal(validatePlayerId('1'), false);
        assert.equal(validatePlayerId(null), false);
    });
});

// ---- validateRelayTarget ----

describe('validateRelayTarget', () => {
    it('accepts null (broadcast)', () => {
        assert.ok(validateRelayTarget(null));
        assert.ok(validateRelayTarget(undefined));
    });

    it('accepts valid player IDs', () => {
        assert.ok(validateRelayTarget(0));
        assert.ok(validateRelayTarget(3));
        assert.ok(validateRelayTarget(100));
    });

    it('rejects invalid player IDs', () => {
        assert.equal(validateRelayTarget(-1), false);
    });
});

// ---- validateLobbyId ----

describe('validateLobbyId', () => {
    it('accepts compact lobby ids', () => {
        assert.ok(validateLobbyId('session'));
        assert.ok(validateLobbyId('cell-0_1'));
        assert.ok(validateLobbyId('A'.repeat(24)));
    });

    it('rejects unsafe or oversized lobby ids', () => {
        assert.equal(validateLobbyId(''), false);
        assert.equal(validateLobbyId('has space'), false);
        assert.equal(validateLobbyId('../session'), false);
        assert.equal(validateLobbyId('A'.repeat(25)), false);
        assert.equal(validateLobbyId(null), false);
    });
});

// ---- chat validation and profanity filter ----

describe('chat validation and filtering', () => {
    it('normalizes chat whitespace before broadcast', () => {
        assert.equal(normalizeChatMessage('  hello   tide\npool  '), 'hello tide pool');
    });

    it('accepts non-empty chat within the configured length', () => {
        assert.ok(validateChatMessage('hello tide pool', 32));
        assert.ok(validateChatMessage('  hello  ', 32));
    });

    it('rejects empty, oversized, and control-character chat', () => {
        assert.equal(validateChatMessage('   ', 32), false);
        assert.equal(validateChatMessage('x'.repeat(33), 32), false);
        assert.equal(validateChatMessage('hello\u0001there', 32), false);
    });

    it('masks default profanity on whole-word boundaries', () => {
        assert.equal(filterProfanity('that is crap'), 'that is ****');
        assert.equal(filterProfanity('grass class pass'), 'grass class pass');
    });

    it('allows server operators to append custom filtered words', () => {
        assert.equal(filterProfanity('bad kelp energy', 'kelp'), 'bad **** energy');
    });
});

// ---- Session lobbies ----

describe('Session lobbies', () => {
    it('adds new players to the default session lobby', () => {
        const { Session } = require('./relay-server');
        const session = new Session('ABC123');
        const ws = { readyState: 1, send() {} };

        const playerId = session.add(ws);

        assert.equal(playerId, 0);
        assert.equal(ws._lobbyId, 'session');
        assert.ok(session.isPlayerInLobby(playerId, 'session'));
    });

    it('moves players between bandwidth lobbies', () => {
        const { Session } = require('./relay-server');
        const session = new Session('ABC123');
        const ws = { readyState: 1, send() {} };
        const playerId = session.add(ws);

        assert.ok(session.moveToLobby(playerId, 'cell-0_1'));

        assert.equal(ws._lobbyId, 'cell-0_1');
        assert.equal(session.isPlayerInLobby(playerId, 'session'), false);
        assert.ok(session.isPlayerInLobby(playerId, 'cell-0_1'));
    });
});

// ---- Creature map generation ----

describe('Creature map generation', () => {
    it('generates deterministic maps for the same seed', () => {
        const { generateCreatureMap, encodeCreatureMap } = require('./relay-server');

        const a = generateCreatureMap(12345);
        const b = generateCreatureMap(12345);

        assert.deepEqual(a, b);
        assert.equal(encodeCreatureMap(a), encodeCreatureMap(b));
        assert.equal(a.mode, 'creature');
        assert.ok(a.landmarks.length > 0);
    });

    it('creates sessions with a server-owned creature map', () => {
        const { Session, encodeCreatureMap } = require('./relay-server');
        const session = new Session('MAP123');

        assert.equal(session.creatureMap.mode, 'creature');
        assert.ok(session.creatureMap.seed > 0);
        assert.match(encodeCreatureMap(session.creatureMap), /^creature,1,\d+,/);
    });
});

// ---- Public lobby discovery ----

describe('Public lobby discovery', () => {
    it('lists joinable sessions with player counts', () => {
        const { Session, sessions, CFG, getPublicLobbies, encodePublicLobbies } = require('./relay-server');
        const previousOfficialLobbies = CFG.OFFICIAL_LOBBIES;
        CFG.OFFICIAL_LOBBIES = '';
        sessions.clear();

        const session = new Session('ABC123', 'Tide Pool');
        session.add({ readyState: 1, send() {} });
        sessions.set(session.code, session);

        assert.deepEqual(getPublicLobbies(), [
            { code: 'ABC123', serverName: 'Tide Pool', players: 1, maxPlayers: 0 },
        ]);
        assert.equal(encodePublicLobbies(), 'ABC123,1,0,Tide%20Pool');

        sessions.clear();
        CFG.OFFICIAL_LOBBIES = previousOfficialLobbies;
    });

    it('uses maxPlayers zero to indicate unlimited sessions', () => {
        const { Session, sessions, CFG, getPublicLobbies } = require('./relay-server');
        const previousOfficialLobbies = CFG.OFFICIAL_LOBBIES;
        CFG.OFFICIAL_LOBBIES = '';
        sessions.clear();

        const session = new Session('OPEN12');
        for (let i = 0; i < 8; i++)
            session.add({ readyState: 1, send() {} });
        sessions.set(session.code, session);

        assert.deepEqual(getPublicLobbies(), [
            { code: 'OPEN12', serverName: 'Public Server', players: 8, maxPlayers: CFG.MAX_PLAYERS },
        ]);

        sessions.clear();
        CFG.OFFICIAL_LOBBIES = previousOfficialLobbies;
    });

    it('lists official server-owned lobbies even when empty', () => {
        const { sessions, CFG, getPublicLobbies, encodePublicLobbies } = require('./relay-server');
        const previousOfficialLobbies = CFG.OFFICIAL_LOBBIES;
        CFG.OFFICIAL_LOBBIES = 'PUBLIC:Official Europe';
        sessions.clear();

        assert.deepEqual(getPublicLobbies(), [
            { code: 'PUBLIC', serverName: 'Official Europe', players: 0, maxPlayers: CFG.MAX_PLAYERS },
        ]);
        assert.equal(encodePublicLobbies(), 'PUBLIC,0,0,Official%20Europe');

        sessions.clear();
        CFG.OFFICIAL_LOBBIES = previousOfficialLobbies;
    });

    it('keeps official sessions after the last player leaves', () => {
        const { sessions, Session, handleLeave } = require('./relay-server');
        sessions.clear();
        const session = new Session('PUBLIC', 'Official Europe', { official: true });
        const ws = { readyState: 1, send() {} };
        session.add(ws);
        sessions.set(session.code, session);

        handleLeave(ws);

        assert.equal(session.size, 0);
        assert.equal(sessions.get('PUBLIC'), session);
        assert.equal(session.official, true);

        sessions.clear();
    });
});

// ---- validateAuthToken ----

describe('validateAuthToken', () => {
    it('allows any token when server secret is empty (dev mode)', () => {
        assert.ok(validateAuthToken('anything', null));
        assert.ok(validateAuthToken('anything', ''));
        assert.ok(validateAuthToken(null, null));
    });

    it('allows matching token', () => {
        assert.ok(validateAuthToken('secret123', 'secret123'));
    });

    it('rejects wrong token', () => {
        assert.equal(validateAuthToken('wrong', 'secret123'), false);
    });

    it('rejects empty client token when server secret is set', () => {
        assert.equal(validateAuthToken('', 'secret123'), false);
        assert.equal(validateAuthToken(null, 'secret123'), false);
    });

    it('rejects tokens of different length (timing safe)', () => {
        assert.equal(validateAuthToken('short', 'secret123'), false);
    });
});

// ---- extractQueryParam ----

describe('extractQueryParam', () => {
    it('extracts token from query string', () => {
        assert.equal(extractQueryParam('/?token=abc123', 'token'), 'abc123');
    });

    it('returns null when param is absent', () => {
        assert.equal(extractQueryParam('/', 'token'), null);
    });

    it('returns null for null url', () => {
        assert.equal(extractQueryParam(null, 'token'), null);
    });

    it('handles multiple query params', () => {
        assert.equal(extractQueryParam('/?foo=bar&token=xyz&baz=1', 'token'), 'xyz');
    });
});
