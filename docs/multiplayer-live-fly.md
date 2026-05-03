# Multiplayer Relay on Fly.io

## What this deploys

`server/` is a long-running Node WebSocket relay for RoobsWorld multiplayer. Fly.io runs it as a normal app process, with HTTP health checks at `/healthz` and WebSocket traffic on the same public hostname.

## Files

- `server/Dockerfile` builds the relay container.
- `server/fly.toml` configures the Fly app, London primary region, one always-on machine, and `/healthz` checks.
- `RoobsWorld/Assets/Resources/multiplayer-relay.json` controls the Unity client relay endpoint.

## First deployment

From `server/`:

```powershell
fly auth login
fly apps create <unique-app-name>
```

Then edit `server/fly.toml`:

```toml
app = "spore"
```

Set a relay token. This is a lightweight gate for the relay, not a true secret once shipped in a game client.

```powershell
fly secrets set GAME_TOKEN="<random-long-token>"
fly deploy
fly scale count 1 -a spore --yes
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
{"ok":true,"sessions":0,"clients":0}
```

## Unity build config

Before building the game, update:

`RoobsWorld/Assets/Resources/multiplayer-relay.json`

```json
{
  "relayUrl": "wss://<unique-app-name>.fly.dev",
  "gameToken": "<same-token-as-GAME_TOKEN>"
}
```

For standalone desktop smoke tests, you can also override without editing the resource file:

```powershell
RoobsWorld.exe -relayUrl wss://spore.fly.dev -gameToken <same-token-as-GAME_TOKEN>
```

## Smoke test

1. Launch one client and host a multiplayer session.
2. Confirm a six-character session code appears.
3. Launch a second client and join that code.
4. Toggle ready on the joining client.
5. Start the game from the host.

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
