'use strict';

/**
 * Security utilities for the RoobsWorld relay server.
 * All functions are pure / stateless (except RateLimiter) to aid unit testing.
 */

// ---- rate limiter (sliding window) ----

/**
 * Tracks event counts per key within a rolling time window.
 * Safe to call synchronously from the message handler.
 */
class RateLimiter {
    /**
     * @param {object} opts
     * @param {number} opts.windowMs   Rolling window in ms (default 60 000)
     * @param {number} opts.max        Max events per window (default 30)
     */
    constructor({ windowMs = 60_000, max = 30 } = {}) {
        this.windowMs = windowMs;
        this.max = max;
        /** @type {Map<string, { times: number[], start: number }>} */
        this._buckets = new Map();
    }

    /**
     * Returns true if the event is allowed; false if the key is rate-limited.
     * Automatically advances the window.
     * @param {string} key  Typically an IP address or connection id
     */
    check(key) {
        const now = Date.now();
        let bucket = this._buckets.get(key);
        if (!bucket) {
            bucket = { times: [], start: 0 };
            this._buckets.set(key, bucket);
        }

        // Drop timestamps outside the window
        while (bucket.start < bucket.times.length && now - bucket.times[bucket.start] >= this.windowMs) {
            bucket.start++;
        }

        const count = bucket.times.length - bucket.start;
        if (count >= this.max) return false;

        if (bucket.start > 32 && bucket.start * 2 > bucket.times.length) {
            bucket.times = bucket.times.slice(bucket.start);
            bucket.start = 0;
        }

        bucket.times.push(now);
        return true;
    }

    /**
     * Remove stale buckets to prevent memory growth.
     * Call periodically (e.g. every minute).
     */
    cleanup() {
        const cutoff = Date.now() - this.windowMs;
        for (const [key, bucket] of this._buckets) {
            if (bucket.times.length === bucket.start || bucket.times[bucket.times.length - 1] < cutoff)
                this._buckets.delete(key);
        }
    }

    /** Number of tracked keys (for monitoring). */
    get size() { return this._buckets.size; }
}

// ---- input validators ----

const CODE_RE   = /^[A-Z0-9]{6}$/;
const CLAN_TAG_RE = /^[A-Z0-9]{2,5}$/;
const LOBBY_ID_RE = /^[A-Za-z0-9_-]{1,24}$/;
// Base64 characters + padding
const BASE64_RE = /^[A-Za-z0-9+/]*={0,2}$/;
const CHAT_CONTROL_CHARS_RE = /[\u0000-\u0008\u000B\u000C\u000E-\u001F\u007F]/;
const DEFAULT_PROFANITY_WORDS = Object.freeze([
    'asshole',
    'bastard',
    'bitch',
    'bullshit',
    'crap',
    'cunt',
    'damn',
    'dick',
    'fuck',
    'fucker',
    'fucking',
    'motherfucker',
    'piss',
    'shit',
]);

/**
 * Returns true for a valid 6-char uppercase alphanumeric session code.
 * Rejects anything else (SQL injection, path traversal, etc. are impossible
 * with this allowlist).
 */
function validateCode(code) {
    return typeof code === 'string' && CODE_RE.test(code);
}

/**
 * Returns true for a display-safe clan name. Names are stored as entered after trim.
 */
function validateClanName(name) {
    if (typeof name !== 'string') return false;
    const trimmed = name.trim();
    return trimmed.length >= 3 && trimmed.length <= 24;
}

function validateServerName(name) {
    if (typeof name !== 'string') return false;
    const trimmed = name.trim();
    return trimmed.length >= 3 && trimmed.length <= 32;
}

/**
 * Returns true for a 2-5 char uppercase alphanumeric clan tag.
 */
function validateClanTag(tag) {
    return typeof tag === 'string' && CLAN_TAG_RE.test(tag.trim().toUpperCase());
}

/**
 * Returns true for a valid base64 string within the given byte limit.
 * A base64 string of N raw bytes is at most ceil(N/3)*4 characters long.
 *
 * @param {*}      data      Value to test
 * @param {number} maxBytes  Maximum decoded byte count (default 16 384 = 16 KB)
 */
function validateBase64(data, maxBytes = 16_384) {
    if (typeof data !== 'string') return false;
    // Each 4 base64 chars encodes 3 bytes; reject anything clearly too large.
    const maxChars = Math.ceil(maxBytes * 4 / 3) + 4;
    if (data.length > maxChars) return false;
    return BASE64_RE.test(data);
}

/**
 * Returns true for a valid relay-assigned player ID.
 */
function validatePlayerId(id) {
    return Number.isInteger(id) && id >= 0;
}

/**
 * Returns true when `to` is either null/undefined (broadcast) or a valid player ID.
 */
function validateRelayTarget(to) {
    return to == null || validatePlayerId(to);
}

/**
 * Returns true for a compact relay lobby identifier.
 * Lobbies are bandwidth buckets inside a session, so keep ids small and
 * display-agnostic: letters, numbers, underscore, hyphen.
 */
function validateLobbyId(lobbyId) {
    return typeof lobbyId === 'string' && LOBBY_ID_RE.test(lobbyId);
}

function normalizeChatMessage(message) {
    if (typeof message !== 'string') return '';
    return message.replace(/\s+/g, ' ').trim();
}

function validateChatMessage(message, maxChars = 160) {
    if (typeof message !== 'string') return false;
    if (CHAT_CONTROL_CHARS_RE.test(message)) return false;

    const normalized = normalizeChatMessage(message);
    return normalized.length > 0 && normalized.length <= maxChars;
}

function escapeRegExp(value) {
    return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

function parseProfanityWords(rawWords) {
    const words = new Set(DEFAULT_PROFANITY_WORDS);
    if (typeof rawWords === 'string' && rawWords.trim().length > 0) {
        for (const word of rawWords.split(',')) {
            const normalized = word.trim().toLowerCase();
            if (normalized.length > 0 && normalized.length <= 64)
                words.add(normalized);
        }
    }
    return [...words].sort((a, b) => b.length - a.length);
}

function maskProfanityWord(word) {
    return '*'.repeat(Math.max(3, word.length));
}

function filterProfanity(message, rawWords = '') {
    if (typeof message !== 'string') return '';

    let filtered = message;
    const words = Array.isArray(rawWords)
        ? rawWords.map(word => String(word).trim().toLowerCase()).filter(Boolean)
        : parseProfanityWords(rawWords);

    for (const word of words) {
        const pattern = new RegExp(`(^|[^A-Za-z0-9])(${escapeRegExp(word)})(?=$|[^A-Za-z0-9])`, 'gi');
        filtered = filtered.replace(pattern, (_match, prefix, profaneWord) =>
            `${prefix}${maskProfanityWord(profaneWord)}`);
    }

    return filtered;
}

// ---- auth ----

/**
 * Validates the game token from the URL query string against the server secret.
 * Returns true when:
 *  - GAME_TOKEN env var is not set (dev/local mode — open access)
 *  - The provided token matches GAME_TOKEN (constant-time comparison)
 *
 * @param {string|null} clientToken  Token extracted from ?token= query param
 * @param {string|null} serverToken  Expected token (typically process.env.GAME_TOKEN)
 */
function validateAuthToken(clientToken, serverToken) {
    if (!serverToken) return true; // dev mode: no token required
    if (typeof clientToken !== 'string' || clientToken.length === 0) return false;
    // Constant-time comparison to prevent timing attacks
    if (clientToken.length !== serverToken.length) return false;
    let diff = 0;
    for (let i = 0; i < serverToken.length; i++) {
        diff |= clientToken.charCodeAt(i) ^ serverToken.charCodeAt(i);
    }
    return diff === 0;
}

/**
 * Extracts a query parameter value from a raw request URL string.
 * e.g. extractQueryParam("/?token=abc123", "token") → "abc123"
 */
function extractQueryParam(url, param) {
    if (!url) return null;
    try {
        // URL constructor requires an origin; use a dummy one.
        const u = new URL(url, 'ws://localhost');
        return u.searchParams.get(param);
    } catch {
        return null;
    }
}

module.exports = {
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
    parseProfanityWords,
    validateAuthToken,
    extractQueryParam,
};

function progressIdentity(token, configuredPlayers) {
    if (typeof token !== 'string' || token.length < 32 || !configuredPlayers || typeof configuredPlayers !== 'object' || Array.isArray(configuredPlayers)) return null;
    const entries = Object.entries(configuredPlayers);
    const matching = entries.filter(([id, expected]) => /^[A-Za-z0-9_-]{1,128}$/.test(id) && typeof expected === 'string' && expected.length >= 32 && validateAuthToken(token, expected));
    return matching.length === 1 ? matching[0][0] : null;
}
module.exports.progressIdentity = progressIdentity;
