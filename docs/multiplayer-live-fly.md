# Multiplayer Relay on Fly.io

## What this deploys

`server/` is a long-running Node WebSocket relay for RoobsWorld multiplayer. Fly.io runs it as a normal app process, with HTTP health checks at `/healthz` and WebSocket traffic on the same public hostname.

## Files

- `server/Dockerfile` builds the relay container.
- `server/fly.toml` configures the Europe official relay.
- `server/fly-us.toml` configures the US East official relay.
- `server/fly-ap.toml` configures the Tokyo mirror for the Asia Pacific official relay.
- `server/fly-sg.toml` configures the Singapore mirror for the Asia Pacific official relay.
- `server/fly-sa.toml` configures the South America official relay in Sao Paulo.
- `server/fly-au.toml` configures the Oceania official relay in Sydney.
- `server/fly-af.toml` configures the Africa official relay in Johannesburg.
- `RoobsWorld/Assets/Resources/multiplayer-relay.json` controls the Unity client relay list.

For China-adjacent coverage on Fly, use the Tokyo and Singapore mirrors under the single `Official Asia Pacific` label. For a true mainland China relay, use `docs/multiplayer-live-china.md`; Fly.io does not currently provide a mainland China region.

## First deployment

From `server/`:

```powershell
fly auth login
fly apps create <unique-app-name>
```

Then edit the matching `server/fly*.toml` app name if needed:

```toml
app = "spore"
```

Set a relay token. This is a lightweight gate for the relay, not a true secret once shipped in a game client.

```powershell
fly secrets set GAME_TOKEN="<random-long-token>"
fly deploy
fly scale count 1 -a spore --yes
```

## Official servers

The shipped client shows six official server choices. Asia Pacific is one displayed choice with Tokyo and Singapore configured as redundant relay URLs:

```text
Official Europe       wss://spore.fly.dev
Official US East      wss://spore-us.fly.dev
Official Asia Pacific wss://spore-ap.fly.dev, wss://spore-sg.fly.dev
Official South America wss://spore-sa.fly.dev
Official Oceania      wss://spore-au.fly.dev
Official Africa       wss://spore-af.fly.dev
```

Mainland China is not a Fly.io region. Tokyo and Singapore are mirrors under the official Asia Pacific label. Host a true China official relay on a mainland China provider only after the required domain, filing, and `wss://` endpoint are ready.

Deploy each official relay as its own Fly app so rooms stay region-local:

```powershell
fly apps create spore
fly apps create spore-us
fly apps create spore-ap
fly apps create spore-sg
fly apps create spore-sa
fly apps create spore-au
fly apps create spore-af

fly secrets set GAME_TOKEN="<same-token-as-client>" -a spore
fly secrets set GAME_TOKEN="<same-token-as-client>" -a spore-us
fly secrets set GAME_TOKEN="<same-token-as-client>" -a spore-ap
fly secrets set GAME_TOKEN="<same-token-as-client>" -a spore-sg
fly secrets set GAME_TOKEN="<same-token-as-client>" -a spore-sa
fly secrets set GAME_TOKEN="<same-token-as-client>" -a spore-au
fly secrets set GAME_TOKEN="<same-token-as-client>" -a spore-af

fly deploy -c fly.toml
fly deploy -c fly-us.toml
fly deploy -c fly-ap.toml
fly deploy -c fly-sg.toml
fly deploy -c fly-sa.toml
fly deploy -c fly-au.toml
fly deploy -c fly-af.toml

fly scale count 1 -a spore --yes
fly scale count 1 -a spore-us --yes
fly scale count 1 -a spore-ap --yes
fly scale count 1 -a spore-sg --yes
fly scale count 1 -a spore-sa --yes
fly scale count 1 -a spore-au --yes
fly scale count 1 -a spore-af --yes
```

Each official app seeds a server-owned `PUBLIC` lobby through `OFFICIAL_LOBBIES` in its `fly*.toml`. That lobby is advertised even at `0` players, never expires by session TTL, and remains available after the last connected player leaves. The first player to join an empty official lobby becomes the host for that live session.

Before shipping, confirm all seven relay health endpoints:

```powershell
curl https://spore.fly.dev/healthz
curl https://spore-us.fly.dev/healthz
curl https://spore-ap.fly.dev/healthz
curl https://spore-sg.fly.dev/healthz
curl https://spore-sa.fly.dev/healthz
curl https://spore-au.fly.dev/healthz
curl https://spore-af.fly.dev/healthz
```

## GitHub Actions deployment

The repository includes `.github/workflows/fly-relay.yml`.

It runs on pushes to `main` or `master` when files under `server/` change, and it can also be started manually from the GitHub Actions tab. The workflow:

1. Installs Node 20 dependencies in `server/`.
2. Runs `npm test`.
3. Deploys `server/` to Fly with `flyctl deploy --remote-only`.

After creating the Fly app, create a deploy token:

```powershell
fly tokens create deploy -x 999999h
```

In GitHub, add this repository secret:

```text
FLY_API_TOKEN=<the full token output, including the FlyV1 prefix>
```

Keep `GAME_TOKEN` as a Fly secret, not a GitHub secret, unless a later workflow needs to rotate it:

```powershell
fly secrets set GAME_TOKEN="<random-long-token>"
```

Check the health endpoint:

```powershell
fly status
curl https://spore.fly.dev/healthz
```

Expected response:

```json
{"ok":true,"sessions":1,"clients":0}
```

## Unity build config

Before building the game, update:

`RoobsWorld/Assets/Resources/multiplayer-relay.json`

```json
{
  "officialServers": [
    {
      "name": "Official Europe",
      "region": "London",
      "relayUrl": "wss://spore.fly.dev",
      "gameToken": "<same-token-as-GAME_TOKEN>"
    }
  ]
}
```

For standalone desktop smoke tests, you can also override without editing the resource file:

```powershell
RoobsWorld.exe -relayUrl wss://spore.fly.dev -gameToken <same-token-as-GAME_TOKEN>
```

## Community-hosted servers

Players can run the same `server/` relay on any WebSocket-capable host. The game lobby accepts a custom `ws://` or `wss://` URL, then lists lobbies from that community relay. Private community relays can include the token in the shared URL query string.

For a simple self-hosted relay:

```powershell
cd server
npm ci
$env:GAME_TOKEN="<optional-server-token>"
$env:PORT="7788"
npm start
```

The host should publish the WebSocket endpoint to players, for example:

```text
wss://my-roobsworld-relay.example.com
wss://my-roobsworld-relay.example.com?token=my-server-token
```

## Smoke test

1. Launch one client and open multiplayer.
2. Confirm the selected official relay shows its `PUBLIC` lobby, even if it has `0` players.
3. Join the official lobby from the first client.
4. Launch a second client and join the same official lobby.
5. Toggle ready on the joining client.
6. Start the game from the host.

If the first client can host but the second cannot join, check Fly logs:

```powershell
fly logs
```

Look for `unauthorized`, `join rate exceeded`, or `not_found` messages.

## Scaling note

Do not scale this relay to multiple machines yet. Sessions are stored in memory, so every player in a room must connect to the same relay instance. The current `fly.toml` keeps one machine running.

Fly may create two machines on the first deploy for redundancy. If that happens, run:

```powershell
fly scale count 1 -a spore --yes
```

## Autoscaling plan

Fly supports two useful autoscaling modes:

- Proxy autostop/autostart starts and stops existing Machines based on request/connection load.
- Metrics-based autoscaling can create/delete or start/stop Machines based on metrics.

The relay is not ready for horizontal autoscaling yet because room state lives in process memory:

- Session codes are stored in `sessions`, a local `Map`.
- Room membership is stored on each WebSocket process.
- A second Machine would have a different empty `sessions` map.

The safe current setup is:

- Keep exactly one Machine running.
- Use the `[http_service.concurrency]` limits in `server/fly.toml` as the first alert/capacity threshold.
- Scale vertically if needed:

```powershell
fly scale vm shared-cpu-2x -a spore
```

To enable true usage-based horizontal autoscaling, first move room/session coordination out of memory. Recommended options:

1. Add Redis/Upstash for session lookup, membership, and relay pub/sub.
2. Split matchmaking from relay traffic: keep room creation in shared storage and route each room to a specific relay instance.
3. Replace the custom relay with a managed realtime backend that supports channels/presence.

After that, create a machine pool and let Fly start/stop Machines:

```powershell
fly scale count 3 -a spore
```

Then switch:

```toml
auto_stop_machines = "suspend"
auto_start_machines = true
min_machines_running = 1
```
