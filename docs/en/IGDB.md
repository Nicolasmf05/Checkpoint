# IGDB covers

**English** · [Español](../IGDB.md)

When the Steam cover for a listed game fails, Checkpoint searches IGDB by title and proposes its closest match. Manually added games without Steam IDs are supported too. Existing custom covers are preserved.

## Choosing a cover

The preview shows the image, matched name and available year. Check the match: similar names do not guarantee the correct edition.

- **Use this cover** stores a local PNG copy. In the game editor, press **Save** to apply an accepted image.
- **Do not use this cover**, or dismissing the proposal, remembers its image ID for that game. It will not be offered again, even if you cancel the editor or restart.
- **Edit game → Search IGDB for another cover** makes a fresh provider request and excludes all declined images. You can change the title before searching. No alternative results leave the current cover intact.

An answered proposal or empty search is not repeated automatically for the same title. Renaming allows another search while retaining declined IDs. Temporary failures are not recorded as rejections. Automatic requests wait while dialogs are open; Windows also waits for Checkpoint to be active. Lightweight and Miniature modes do not request covers. The limit is 10 automatic searches per launch/tab and 200 declined images per game; further searches stop at the latter limit to preserve every rejection.

Accepted images and rejection metadata survive compatible Checkpoint backups/imports. They are excluded from friends' progress publications. Browser storage belongs to that browser and site: clearing it removes these choices unless you export a backup. Windows and web do not automatically synchronize these choices.

## Configuring your own distribution

1. Register a confidential application in [Twitch Developers](https://dev.twitch.tv/console/apps). Follow the [official IGDB documentation](https://api-docs.igdb.com/#account-creation) for a Client ID and Client Secret.
2. Store `IGDB_CLIENT_ID` and `IGDB_CLIENT_SECRET` as Supabase secrets. Never put them in source, public files, browser configuration or repositories.
3. Apply the [Supabase migrations](SUPABASE.md), including the Steam service state table used as a private server cache.
4. Deploy `supabase/functions/checkpoint-covers/index.ts` as `checkpoint-covers`, with `verify_jwt = false` as configured in `supabase/config.toml`. This endpoint only provides public cover metadata and does not require a user session. Social and Steam services retain their own authentication.
5. Configure your project's URL and public key in your distribution. Adjust the service's explicit CORS origin list for another website.

Users of the official distribution need no Twitch account, personal API key or Checkpoint login for these proposals. Service credentials belong to its operator. IGDB describes free access as non-commercial; check its terms before commercializing another distribution.

## Data and limits

The searched title is sent to Supabase and IGDB, including titles private to friends. Notes, passwords, Steam sessions and progress are not sent. Rejected IDs are sent to Supabase to filter suggestions; the reply contains the name, year and public image identifier. The server caches results for 7 days and applies per-IP limits through a hashed identifier. Manual searches bypass the result cache. Images use an IGDB-only proxy, are validated and converted into local PNGs up to 160 × 240 pixels. Downloads are limited to 2 MB; arbitrary URLs are not accepted.

The function applies local frequency limits; multiple provider instances can share IGDB's quota. Provider failures or rate limits leave your existing cover intact. Image source: **IGDB**; Checkpoint's code license grants no rights over third-party cover artwork.
