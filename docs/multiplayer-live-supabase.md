# Multiplayer Live Deployment Notes

## Short answer

The current multiplayer stack cannot be deployed directly as a Supabase Edge Function because it is a long-running Node WebSocket relay (`server/relay-server.js`). Supabase can still be part of the live multiplayer setup in either of these ways:

1. Use Supabase Realtime Broadcast/Presence as the relay layer and replace `WebSocketNetworkTransport` with a Supabase Realtime transport.
2. Keep the existing Node relay and host it on a WebSocket-capable service, while using Supabase for account auth, matchmaking records, telemetry, or moderation.

Option 2 is the fastest route to getting the current game mode live because the Unity client already speaks the relay protocol. The current deployment target for that route is Fly.io; see `docs/multiplayer-live-fly.md`.

## Current relay contract

The live relay expects:

- A WebSocket endpoint, for example `wss://relay.example.com`.
- Optional `GAME_TOKEN` auth, appended by Unity as `?token=...`.
- `PORT` supplied by the hosting platform.
- HTTP health checks at `/healthz`.

The Unity lobby currently defaults to `ws://localhost:7788` in:

- `RoobsWorld/Assets/Scripts/Multiplayer/MultiplayerLobbyUI.cs`
- `RoobsWorld/Assets/Scripts/Multiplayer/WebSocketNetworkTransport.cs`

## Recommended first live path

1. Deploy `server/` to Fly.io.
2. Set `GAME_TOKEN` to a generated random value.
3. Confirm `https://<host>/healthz` returns JSON with `ok: true`.
4. Configure the Unity build to use `wss://<host>` and the same `GAME_TOKEN`.
5. Run a two-client smoke test: host creates a session, second client joins by code, ready states sync, host starts game.

## Supabase-backed path

To make Supabase itself the multiplayer transport, build a new `INetworkTransport` implementation over Supabase Realtime:

- Use one Realtime channel per session code.
- Use Broadcast events for `create`, `join`, `player_joined`, `player_left`, `ready`, `start_game`, and `data`.
- Use Presence to track connected players and host migration.
- Use Postgres tables for durable session metadata if we want reconnect/replay or moderation.

This is more work than deploying the current relay because Unity needs a Supabase Realtime client/protocol layer. The upside is less custom server infrastructure.

## Supabase tables for matchmaking, if we keep the relay

If we keep the Node relay, Supabase can still hold non-authoritative metadata:

```sql
create table public.multiplayer_sessions (
  code text primary key check (code ~ '^[A-Z0-9]{6}$'),
  relay_url text not null,
  host_player_id integer not null default 0,
  player_count integer not null default 1 check (player_count between 0 and 4),
  created_at timestamptz not null default now(),
  expires_at timestamptz not null default now() + interval '2 hours'
);

alter table public.multiplayer_sessions enable row level security;

create policy "sessions are readable"
on public.multiplayer_sessions
for select
using (expires_at > now());
```

Keep authoritative room membership in the relay unless/until the Unity client is moved to Supabase Realtime.
