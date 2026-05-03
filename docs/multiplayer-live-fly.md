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
app = "<unique-app-name>"
```

Set a relay token. This is a lightweight gate for the relay, not a true secret once shipped in a game client.

```powershell
fly secrets set GAME_TOKEN="<random-long-token>"
fly deploy
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
curl https://<unique-app-name>.fly.dev/healthz
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
RoobsWorld.exe -relayUrl wss://<unique-app-name>.fly.dev -gameToken <same-token-as-GAME_TOKEN>
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
