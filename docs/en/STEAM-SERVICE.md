# Steam service

**English** · [Español](../STEAM-SERVICE.md)

Node uses built-in modules; no `npm install`. Steam hosting on Supabase Edge Functions is pending.

## Local development

1. Obtain a key from [Steam](https://steamcommunity.com/dev/apikey). Never put it in chat/source.
2. Copy `server/.env.example` to `server/.env`; set `STEAM_API_KEY`.
3. Keep `PUBLIC_URL=http://127.0.0.1:34871`, `HOST=127.0.0.1`, `PORT=34871`.
4. Run `npm start` from `server`.
5. In Settings → Advanced connection enter `http://127.0.0.1:34871`, then Link Steam.
6. Complete sign-in yourself on `steamcommunity.com`; the app receives the session/library.

Keep the server running. Imports go to Library; enable Show in My list from each card.

## Distribution

1. Host Node/the Dockerfile behind HTTPS.
2. Set the exact external HTTPS origin in `PUBLIC_URL`, `HOST=0.0.0.0` and provider port.
3. Keep `STEAM_API_KEY` in provider secrets, never app workflow variables or URLs.
4. Set `OPERATOR_NAME`, `HOSTING_COUNTRY`, `PRIVACY_CONTACT`; `/privacy` identifies the real operator. Review provider logs.
5. Preserve `/v1/auth/callback` path/query. Do not add cookies or log Authorization/full auth URLs.
6. Verify `/health`, `/privacy` and real sign-in before distribution.
7. Build with `./scripts/Build.ps1 -Installer -ServiceUrl https://your-service.example`.

Uses `api.steampowered.com` and `x-webapi-key`. The partner host does not accept an ordinary key.

## Limits

- OpenID verifies provider, origin, callback, identity, signed fields, nonce and expiry directly with Steam.
- Sessions bind to authenticated SteamID; clients cannot substitute another user. A polling secret protects callback-based session acquisition.
- Login flows expire in 10 minutes, client waits five; sessions last seven days and revoke on unlink.
- In-memory sessions/cache require relinking after restart. Use one instance; scaling needs shared sessions/cache/limits such as Redis.
- User cache 15 minutes, definitions 24 hours; refreshing the app does not force Steam updates.
- 90 queries/session/minute, 10 links/IP/minute, 90,000 Steam calls/day/process.
- Proxy IP limits see the proxy; do not blindly trust `X-Forwarded-For`. Configure proxy and shared egress limits.
- Only authenticated users' visible libraries are queried; private data produces an error, not fabricated empty results.
- No telemetry, push notifications, achievement modification or password storage.

Eligibility/availability depend on Valve. [Web API](https://partner.steamgames.com/doc/webapi_overview) · [Keys](https://partner.steamgames.com/doc/webapi_overview/auth) · [OpenID](https://partner.steamgames.com/doc/features/auth) · [Terms](https://steamcommunity.com/dev/apiterms).
