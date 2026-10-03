# Privacy

**English** · [Español](../PRIVACY.md)

Checkpoint stores titles, states, notes, tasks, settings, covers and latest Steam progress locally. No advertising or telemetry. Language is a local preference; it does not translate or upload user content.

Signing in enables friends through Supabase in Ireland. Registration uses username/password, with no real email or confirmation. Auth internally uses `username@accounts.checkpoint.invalid`. Account ID, display name, friend code, requests, friendships, blocks and publications are stored by the service. Passwords travel over HTTPS to Auth, are not stored by the app and are excluded from backups. Recovery is unavailable.

Only games selected in Friends → Sharing are published: title, platform, state, goal, manual story percentage, task/achievement counters, cover reference and finish date. Notes, task labels and individual achievements remain on the PC. Display names are also visible to pending-request participants. Shared games require accepted friendship.

Consent and pending operations persist locally per account. Withdrawal clears published content after acknowledgment; offline withdrawal can leave it visible until sync. A revision tombstone prevents stale clients republishing silently. Signing out preserves publications and queued operations.

Custom shared covers use a private bucket. Friends read only images referenced by authorized data. Removing/blocking revokes server access; polling clears the view on revoked access or failed reads. Private images stay in memory without disk cache. This cannot erase screenshots/downloads from other clients, orphan Storage images, provider backups or logs. Full account/file deletion requires operator management.

Automatic covers contact Steam's CDN with the connection address and app ID. Custom images are copied locally. The last 20 deleted games persist in recovery history until displaced by newer deletions; deleting games does not immediately remove cached images.

Steam linking/sync queries the authenticated ID, visible library, playtime and achievements, without passwords. Sessions last up to seven days in memory, user cache 15 minutes, non-identifying achievement definitions 24 hours. Unlinking revokes the session and removes user cache in that instance.

The operator must configure name, contact and hosting country before distribution; `/privacy` publishes them. Providers may retain connection logs under their own policies.

Local data survives unlink/uninstall. Export from Settings; to remove it, close the app and delete `%LOCALAPPDATA%\Checkpoint`. Exports contain notes and should be shared intentionally.

Steam/Checkpoint sessions use Windows DPAPI for the current user. Complete `.checkpoint` backups contain active games and custom covers, excluding sessions, sharing consent, social queue and recovery history. JSON omits images. Neither backup format provides its own encryption. Checkpoint is independent of Valve.

In the 0.6 Supabase Steam deployment, Steam IDs, session/nonce hashes, library and achievement cache persist in a private database schema in Ireland. Flows expire after 10 minutes, sessions after seven days, user cache after 15 minutes and public definitions after 24 hours. Expired rows are purged during later requests. Unlink revokes the session and clears its game cache. Passwords and plaintext session tokens are never stored in this schema; the operator key stays in server secrets. Supabase logs/backups follow provider retention. The preceding in-memory description applies to the alternative Node service.

The local interface uses WebView2 and stores its browser profile in `webview-profile` beside the library. The interface does not load remote web pages; Steam/Supabase calls are made by native C#. The shared Microsoft runtime updates independently under Microsoft’s settings and terms.

## Web version

The web stores your private library, settings and publication queue in browser IndexedDB, without private cross-device synchronization. Sessions use this tab’s sessionStorage and are excluded from exports; they do not use DPAPI. Clearing site data deletes the local library: keep JSON backups. Offline caches contain public app files, not private service responses. GitHub Pages serves public files and Supabase handles authentication and social data. [Details and limits](WEB.md).
