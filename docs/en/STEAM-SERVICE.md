# Steam service

**English** · [Español](../STEAM-SERVICE.md)

Version 0.6 source includes a dependency-free Supabase Edge Function. Node remains available for local development; no `npm install` is needed.

## Automatic desktop synchronization

When Steam is linked, Checkpoint checks the library and playtime on every launch and every 30 minutes by default. Settings offers 15, 30, 60 or 120 minutes. No Refresh click is required. New games appear in Library; adding them to My list remains manual. Each cycle updates achievements for up to 20 tracked games, starting with the least recently synced. Manual states, notes and story progress are preserved. Offline failures keep saved data and retry at the next cycle. Automatic cycles never overlap. An individual refresh may join a running achievement request.

## Request and error handling

From 0.8.27, reviews and automatic updates use up to three concurrent reads, with starts spaced by 750 ms. Active reads for the same game, account and language share one request. Reads have one retry for network failures, timeouts or HTTP 502/504; linking operations, authentication failures and rate limits are not retried. Rate limits stop the batch and impose a cooldown. Cancelling one consumer does not interrupt another consumer of the same request.

Service-wide failures stop pending requests; per-game failures allow the review to continue. Incomplete responses or duplicate identifiers cannot replace progress. Account changes, deleted games or edited identifiers cannot receive stale results. Notes, manual achievements, personal overrides and privacy are retained. Web persistence batches eight changes or two seconds and flushes on completion or cancellation; Windows saves each existing game individually.

- **Expired session:** link Steam again; signing into Checkpoint does not renew Steam authentication.
- **Privacy:** check Game details visibility on the linked account. A public profile alone is insufficient.
- **Unavailable service or rate limit:** keep your progress and retry later. Removing games or manual achievements is unnecessary.
- **Invalid response:** progress and synchronization timestamps remain unchanged; retry when Steam responds correctly.

Hosted progress caching lasts one minute; Refresh within that minute may return the same information. Valid definitions are reused for 24 hours and failures are never stored as successful results. Server request coalescing is local to each Edge worker, not shared across workers.

## Supabase deployment

1. Apply `supabase/migrations/202610020002_checkpoint_steam.sql` after the social migration. Run `supabase/tests/steam.sql`; fixtures roll back.
2. Deploy `supabase/functions/checkpoint-steam/index.ts` as `checkpoint-steam`. Only the server uses automatic `SUPABASE_URL` and `SUPABASE_SERVICE_ROLE_KEY` variables. Never distribute privileged keys.
3. Obtain a key from [Steam](https://steamcommunity.com/dev/apikey), accepting Valve's terms personally. Save it as `STEAM_WEB_API_KEY` in Supabase → Edge Functions → Secrets. Never paste it in chat/source.
4. Disable **Verify JWT with legacy secret** for this function only. Public login/callback routes verify OpenID directly with Steam; library/achievement routes require their own random, expiring session token. Social Auth/RLS remain independent.
5. Verify `/health` reports `steamConfigured: true`, press Link Steam in the app and finish sign-in yourself on Steam. Review Game details visibility if Steam denies access.
6. The default build endpoint is `https://fumdnvvvoiwoiziwtmsu.supabase.co/functions/v1/checkpoint-steam/`. Fork operators must supply their own public project/service configuration.

Flows, session/nonce hashes, limits and caches persist in a private schema, accessible only through a service-role RPC. Polling atomically consumes the flow and creates a session; losing a successful polling response requires relinking. Flows expire after 10 minutes, sessions after seven days; expired rows are purged during later requests. Unlink revokes the session and removes its game cache. Library cache lasts 15 minutes and achievement progress one minute; language-specific public definitions 24 hours. Shared limits: 30 links/minute, 2,000 requests/minute, 90 requests/session/minute and 90,000 upstream calls/day. Caller-supplied IP headers are not trusted. Review capacity before broad distribution. Hosting logs/backups follow Supabase retention. The browser callback is readable bilingual text because Supabase rewrites HTML responses.

Run `node --test supabase/tests/steam.test.mjs` for simulated HTTP/security checks. Live Steam validation requires the server secret and a person completing sign-in.

## Local development

1. Obtain a key from [Steam](https://steamcommunity.com/dev/apikey). Never put it in chat/source.
2. Copy `server/.env.example` to `server/.env`; set `STEAM_API_KEY`.
3. Keep `PUBLIC_URL=http://127.0.0.1:34871`, `HOST=127.0.0.1`, `PORT=34871`.
4. Run `npm start` from `server`.
5. In Settings → Advanced connection enter `http://127.0.0.1:34871`, then Link Steam.
6. Complete sign-in yourself on `steamcommunity.com`; the app receives the session/library.

Keep the server running. Imports go to Library; enable Show in My list from each card.

## Alternative Node hosting

1. Host Node/the Dockerfile behind HTTPS.
2. Set the exact external HTTPS origin in `PUBLIC_URL`, `HOST=0.0.0.0` and provider port.
3. Keep `STEAM_API_KEY` in provider secrets, never app workflow variables or URLs.
4. Set `OPERATOR_NAME`, `HOSTING_COUNTRY`, `PRIVACY_CONTACT`; `/privacy` identifies the real operator. Review provider logs.
5. Preserve `/v1/auth/callback` path/query. Do not add cookies or log Authorization/full auth URLs.
6. Verify `/health`, `/privacy` and real sign-in before distribution.
7. Build with `./scripts/Build.ps1 -Installer -ServiceUrl https://your-service.example`.

Uses `api.steampowered.com` and `x-webapi-key`. The partner host does not accept an ordinary key.

## Node limits

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

## Steam Families import

The service requests family licenses alongside owned games and merges Steam's recently played games as a supplemental source. Duplicate AppIDs keep the largest reported total playtime. Import is limited to games Steam exposes for the linked user's visible profile; this is not a complete enumeration of unplayed family-group games. No other family member's credentials or progress is imported.

Achievements are requested for the linked SteamID, including borrowed games present in the combined visible library. Private profiles remain restricted. If the recent-games endpoint fails, the owned library is retained. The hosted library cache lasts 15 minutes and achievement progress one minute. The hosted service replaces old owned-only cache entries automatically; use the widget's Update button and open Library. Existing desktop releases work without reinstalling.

The `include_family_licenses` flag is treated as a compatibility hint; Valve may ignore it or restrict returned data. The recent-games route is documented by [Valve](https://partner.steamgames.com/doc/webapi/IPlayerService). Family-account validation remains pending; passing simulated HTTP tests is not proof that every shared game is available for every profile.
