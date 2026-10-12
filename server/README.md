
## Progress identity and slot contract

Progress endpoints no longer trust `x-player-id`, a URL identity, or the shared `GAME_TOKEN`. Configure `PROGRESS_PLAYER_TOKENS` as a server-only JSON mapping from trusted player IDs to unique per-player bearer credentials of at least 32 characters. Provision these through a trusted authenticated service; do not distribute the mapping or reuse the shared game token. Missing/invalid mappings fail closed. The deployment’s actual identity provider still needs to be integrated with the Unity client.

Authenticated requests use `/api/progress/autosave/slots/0` through `/slots/9` for POST, GET and DELETE; POST JSON must contain the matching `slot`. GET `/api/progress/autosave/slots` lists saved slots. Storage keys derive from the verified identity, and writes rename a temporary file atomically. The original base endpoint maps only to slot 0; identity-specific lookups allow only the authenticated player. Legacy single-file saves are not silently reassigned or deleted.

`npm test` includes real HTTP/WebSocket tests for malformed input, same-IP message budgets, spoofed identities, shared-token denial, slot isolation and deletion. Unity client integration, cloud/local freshness, asynchronous loading and hosted acceptance remain unverified. Message quotas are per connection; existing connection/join/chat IP abuse limits remain separate.
