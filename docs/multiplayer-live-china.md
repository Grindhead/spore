# Mainland China Multiplayer Relay

## Decision

Fly.io does not currently have a mainland China region. For now, use Tokyo and Singapore as mirrors under the single official Asia Pacific label for China-adjacent coverage:

```text
Official Asia Pacific wss://spore-ap.fly.dev  (Tokyo mirror)
Official Asia Pacific wss://spore-sg.fly.dev  (Singapore mirror)
```

Use a mainland China cloud VM only if we later need a true mainland-hosted relay, such as Alibaba Cloud ECS or Tencent Cloud CVM/Lighthouse, and run the existing `server/` Docker image there.

Mainland China hosting has compliance requirements before public launch:

- A public mainland China web service normally needs an ICP filing or ICP license.
- The filing is tied to the domain and the mainland cloud/access provider.
- Do not add the China relay to the shipped client until the domain resolves, TLS works, and the ICP/app requirements are handled.

## Target Endpoint

Use a dedicated China hostname:

```text
wss://spore-cn.<your-domain>
```

The server should advertise one permanent official lobby:

```text
OFFICIAL_LOBBIES=PUBLIC:Official China
```

## VM Environment

Set these environment variables on the China VM:

```text
NODE_ENV=production
PORT=7788
MAX_PLAYERS=4
GAME_TOKEN=<same-token-as-client>
OFFICIAL_LOBBIES=PUBLIC:Official China
```

## Container Run

From the repo's `server/` folder, build and run the relay:

```bash
docker build -t roobs-world-relay .
docker run -d \
  --name roobs-world-relay \
  --restart unless-stopped \
  -e NODE_ENV=production \
  -e PORT=7788 \
  -e MAX_PLAYERS=4 \
  -e GAME_TOKEN="<same-token-as-client>" \
  -e OFFICIAL_LOBBIES="PUBLIC:Official China" \
  -p 127.0.0.1:7788:7788 \
  roobs-world-relay
```

Put Nginx, Caddy, or the cloud provider's load balancer in front of the relay to terminate TLS on `443` and proxy WebSocket traffic to `127.0.0.1:7788`.

## Nginx WebSocket Proxy

```nginx
server {
    listen 443 ssl http2;
    server_name spore-cn.<your-domain>;

    ssl_certificate /etc/letsencrypt/live/spore-cn.<your-domain>/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/spore-cn.<your-domain>/privkey.pem;

    location / {
        proxy_pass http://127.0.0.1:7788;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_read_timeout 120s;
    }
}
```

## Client Config

After the China endpoint is live and reachable, add it to `RoobsWorld/Assets/Resources/multiplayer-relay.json`:

```json
{
  "name": "Official China",
  "region": "Mainland China",
  "relayUrl": "wss://spore-cn.<your-domain>",
  "gameToken": "<same-token-as-GAME_TOKEN>"
}
```

## Smoke Test

1. Confirm the health endpoint:

   ```bash
   curl https://spore-cn.<your-domain>/healthz
   ```

   Expected:

   ```json
   {"ok":true,"sessions":1,"clients":0}
   ```

2. Open multiplayer in the client.
3. Confirm `Official China` appears with a `PUBLIC` lobby, even at `0` players.
4. Join from two clients and start the game.
