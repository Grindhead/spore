'use strict';

/**
 * Server-side player progress storage.
 *
 * Identity model
 *   - A player identity is minted by the server (`register`) as a random UUID plus a
 *     random 256-bit secret. The client receives `<playerId>.<secret>` once and presents
 *     it as a bearer credential on every progress request.
 *   - Only a SHA-256 hash of the secret is stored. The player id used for storage is
 *     always the one proven by the credential; caller-supplied headers or URL ids are
 *     never consulted.
 *   - Storage directories are keyed by SHA-256(playerId), so normalisation cannot merge
 *     two identities and there is no shared "unknown player" bucket.
 *
 * Slot model
 *   - Each slot is its own file. A deleted slot leaves a tombstone so a stale copy
 *     cannot reappear; a later save to the same slot replaces the tombstone.
 *   - All writes go to a temporary file and are renamed into place.
 */

const crypto = require('crypto');
const fs = require('fs');
const path = require('path');

const SLOT_RE = /^[a-z0-9_-]{1,64}$/;
const PLAYER_ID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/;
const SECRET_RE = /^[A-Za-z0-9_-]{43}$/; // 32 random bytes, base64url
const DATA_SUFFIX = '.json';
const TOMBSTONE_SUFFIX = '.deleted.json';

function sha256Hex(value) {
    return crypto.createHash('sha256').update(value, 'utf8').digest('hex');
}

/** Lower-cases and validates a slot name. Returns null when the name is unusable. */
function normalizeSlot(slot) {
    if (typeof slot !== 'string') return null;
    const normalized = slot.trim().toLowerCase();
    return SLOT_RE.test(normalized) ? normalized : null;
}

/** Collision-resistant storage key for a verified player id. */
function storageKey(playerId) {
    return sha256Hex(`player:${playerId}`);
}

function writeFileAtomic(file, text) {
    fs.mkdirSync(path.dirname(file), { recursive: true });
    const temporary = `${file}.${crypto.randomUUID()}.tmp`;
    try {
        fs.writeFileSync(temporary, text, 'utf8');
        fs.renameSync(temporary, file);
    } catch (err) {
        try { fs.rmSync(temporary, { force: true }); } catch { /* best effort */ }
        throw err;
    }
}

function readJsonOrNull(file) {
    try {
        return JSON.parse(fs.readFileSync(file, 'utf8'));
    } catch (err) {
        if (err && err.code === 'ENOENT') return null;
        throw err;
    }
}

class ProgressStore {
    /**
     * @param {string} rootDir     Base directory (AUTOSAVE_DIR)
     * @param {object} [opts]
     * @param {number} [opts.maxSlots]  Maximum live slots per player
     */
    constructor(rootDir, { maxSlots = 16 } = {}) {
        this.rootDir = rootDir;
        this.maxSlots = maxSlots;
    }

    playerDir(playerId) {
        return path.join(this.rootDir, 'players', storageKey(playerId));
    }

    slotPaths(playerId, slot) {
        const dir = path.join(this.playerDir(playerId), 'slots');
        return {
            data: path.join(dir, slot + DATA_SUFFIX),
            tombstone: path.join(dir, slot + TOMBSTONE_SUFFIX),
        };
    }

    /** Mints a new player identity and returns its one-time credential. */
    register() {
        const playerId = crypto.randomUUID();
        const secret = crypto.randomBytes(32).toString('base64url');
        const credentialFile = path.join(this.playerDir(playerId), 'credential.json');
        writeFileAtomic(credentialFile, JSON.stringify({
            secretSha256: sha256Hex(secret),
            createdAt: new Date().toISOString(),
        }));
        return { playerId, credential: `${playerId}.${secret}` };
    }

    /** Returns the verified player id for a credential, or null. Never throws. */
    authenticate(credential) {
        if (typeof credential !== 'string') return null;
        const dot = credential.indexOf('.');
        if (dot <= 0) return null;
        const playerId = credential.slice(0, dot);
        const secret = credential.slice(dot + 1);
        if (!PLAYER_ID_RE.test(playerId) || !SECRET_RE.test(secret)) return null;

        let stored;
        try {
            stored = readJsonOrNull(path.join(this.playerDir(playerId), 'credential.json'));
        } catch {
            return null;
        }
        if (!stored || typeof stored.secretSha256 !== 'string') return null;

        const expected = Buffer.from(stored.secretSha256, 'hex');
        const actual = Buffer.from(sha256Hex(secret), 'hex');
        if (expected.length !== actual.length) return null;
        return crypto.timingSafeEqual(expected, actual) ? playerId : null;
    }

    /**
     * Resolves a slot's state, using server timestamps to settle a data file and a
     * tombstone that both exist (possible after an interrupted write or delete).
     */
    readSlot(playerId, slot) {
        const paths = this.slotPaths(playerId, slot);
        const data = readJsonOrNull(paths.data);
        const tombstone = readJsonOrNull(paths.tombstone);

        if (data && tombstone) {
            const savedAt = Date.parse(data.savedAt);
            const deletedAt = Date.parse(tombstone.deletedAt);
            if (!(savedAt > deletedAt)) return { status: 'deleted', deletedAt: tombstone.deletedAt };
            return { status: 'ok', data };
        }
        if (data) return { status: 'ok', data };
        if (tombstone) return { status: 'deleted', deletedAt: tombstone.deletedAt };
        return { status: 'missing' };
    }

    listSlots(playerId) {
        const dir = path.join(this.playerDir(playerId), 'slots');
        let names;
        try {
            names = fs.readdirSync(dir);
        } catch (err) {
            if (err && err.code === 'ENOENT') return [];
            throw err;
        }

        const slots = new Set();
        for (const name of names) {
            if (name.endsWith(TOMBSTONE_SUFFIX)) slots.add(name.slice(0, -TOMBSTONE_SUFFIX.length));
            else if (name.endsWith(DATA_SUFFIX)) slots.add(name.slice(0, -DATA_SUFFIX.length));
        }

        const result = [];
        for (const slot of [...slots].sort()) {
            if (!SLOT_RE.test(slot)) continue;
            const state = this.readSlot(playerId, slot);
            if (state.status === 'ok') result.push({ slot, savedAt: state.data.savedAt });
            else if (state.status === 'deleted') result.push({ slot, deletedAt: state.deletedAt });
        }
        return result;
    }

    /**
     * Stores `save` in `slot`. Returns false when the player is at the slot limit.
     */
    writeSlot(playerId, slot, save) {
        const paths = this.slotPaths(playerId, slot);
        if (this.readSlot(playerId, slot).status !== 'ok') {
            const live = this.listSlots(playerId).filter(entry => entry.savedAt).length;
            if (live >= this.maxSlots) return false;
        }

        const envelope = { slot, savedAt: new Date().toISOString(), save };
        writeFileAtomic(paths.data, JSON.stringify(envelope));
        fs.rmSync(paths.tombstone, { force: true });
        return true;
    }

    deleteSlot(playerId, slot) {
        const paths = this.slotPaths(playerId, slot);
        const deletedAt = new Date().toISOString();
        writeFileAtomic(paths.tombstone, JSON.stringify({ slot, deletedAt }));
        fs.rmSync(paths.data, { force: true });
        return deletedAt;
    }
}

module.exports = {
    ProgressStore,
    normalizeSlot,
    storageKey,
};
