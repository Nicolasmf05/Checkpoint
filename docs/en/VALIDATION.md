# Validation of 0.5.0

**English** · [Español](../VALIDATION.md)

Prepared on October 2, 2026. Release compilation, model/storage tests, Node service tests and native WPF tests run through `scripts/Build.ps1`.

Verified totals: **108 core checks, 15 Node tests and 64 native checks**. The Node privacy-page check verifies both language sections.

- Core tests cover story/achievement independence, Steam deduplication, SQLite transactions/migrations, preferences/order, complete/legacy backups, malformed archives, size/path/PNG limits, recovery, account-bound outbox, idempotence/conflicts, safe publication fields, username mapping and refresh rotation. English/Spanish labels and unchanged wire/user data are checked.
- The 15 Node checks use simulated Steam replies: OpenID, nonce/callback, polling secrets, revocation, cache, privacy, authorization and rate limits.
- Native tests exercise real WPF search/filters, grid columns, persistent ordering, drag authorization, Unicode editor/tasks, cancel/save, hidden achievements, preferences, deletion/undo/recovery and cover backups. Social HTTP is simulated; tests cover account form, DPAPI, requests, sharing, authenticated private PNG rendering, blocking, withdrawal and account isolation.
- Language tests switch from Settings, verify persistence and immediate UI updates, open English editor/settings and friend progress, preserve user notes/publication payloads, and switch back to Spanish.
- A 1,003-game grid creates fewer than 30 realized rows before/after scrolling. Captures use `RenderTargetBitmap` from the app.
- Tests run against the self-contained app extracted from the distribution ZIP; MSI is compiled with WiX 5.0.2 and checksums are generated.

Supabase real backend passed 23 social SQL + 10 Storage/request-limit + 7 anonymous HTTP permission checks. Synthetic users/assets were rolled back. Auth settings confirmed enabled signup and disabled email confirmation. This does not replace real two-account signup or live Storage upload/download testing.

The Steam server is not publicly deployed; the distribution URL is empty. No real Steam account/key was used. Native captures/events do not replace physical drag/keyboard, system file-picker or full accessibility testing. The unsigned MSI has not been installed/uninstalled on this PC.

Before public release, verify:

1. Clean per-user install, Start menu launch without external runtime, upgrade and uninstall preserving library/removing startup shortcut.
2. International names, edits/tasks/custom covers/backups and both languages across restart.
3. Movement/resize/translucency on multiple backgrounds, monitors and DPI settings.
4. Real Steam linking/import/achievements and connection failures.
5. Tray, global shortcut, startup and resume from sleep.
6. Two real Checkpoint accounts, requests/sharing/private covers, conflict/disconnect retry, withdrawals and blocking.
7. Artifact signing, operator/contact/hosting identity and retention procedures.

Transparency is alpha, without blur. SQLite 0.1/0.2 libraries migrate to schema 2; older app versions reject migrated databases. Compatible JSON remains importable; complete backups require 0.3+.
