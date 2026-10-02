# Changelog

**English** · [Español](CHANGELOG.es.md)

## Unreleased

- Microsoft Store preparation: unsigned MSIX packaging with explicit preview/Store identities, reproducible icons, SDK validation and payload/architecture verification.
- English/Spanish Store submission and migration guidance; CI builds a packaging preview. Store certification and installed testing remain pending.

## 0.6.1 — 2026-10-02

- Portable ZIP extracts into one `Checkpoint` folder, containing the executable, runtime, configuration and licenses.
- Package validation checks the single-folder layout and runs the app from that folder. MSI harvesting uses the same bundle.
- Windows security troubleshooting distinguishes reputation/unsigned-publisher warnings from antivirus detections. Packages remain unsigned.

## 0.6.0 — 2026-10-02

- Supabase Steam Edge Function: verified OpenID, durable private session hashes, atomic polling, nonce replay protection, shared limits and caches.
- Default Supabase Steam endpoint; English/Spanish achievements with separate caches.
- Atomic DPAPI persistence and native HTTP checks. Live Steam validation requires the server secret and personal sign-in.

## 0.5.0 — 2026-10-02

- English/Spanish interface, saved Settings → Language selector and immediate updates to navigation, dialogs, labels, tooltips, accessible names and built-in errors.
- User content and API wire values remain unchanged. Tests verify switching, persistence and publication privacy.
- English documentation alongside Spanish counterparts; screenshots in both languages.
- Steam service landing, privacy and callback pages display English and Spanish; known server errors are translated by the native client.
- Version-specific build folders allow packaging while an older portable edition stays open. Package verification accepts informational versions with Git hashes.
- Source exports exclude Git metadata explicitly; private key/session extensions are ignored.

## 0.4.0 — 2026-10-02

- Supabase friends: independent Checkpoint username/password accounts without real email/confirmation, friend codes, accepted/declined/canceled requests, unfriend and blocking.
- Selected game progress and private custom covers. Notes, task names and individual achievements excluded.
- Account-bound durable outbox, idempotent retries, revision conflicts and withdrawals; signing out preserves publications.
- DPAPI sessions and refresh rotation; optional manual story progress independent of tasks/achievements.
- Public configuration included; privileged keys excluded. Avatar UI, recovery and private-library cloud restore pending. Client uses simulated HTTP tests; backend permissions tested live. Steam Edge Functions migration pending.

## 0.3.0 — 2026-10-02

- Complete `.checkpoint` backups with custom covers; legacy JSON support; atomic exports, validated image imports and preservation of existing games.
- Undo ↶/Ctrl+Z and per-game recovery in Settings, retaining the last 20 deletions across restart with full notes/tasks/progress/state/favorites/order/covers.
- SQLite recovery migration; archive path/reference/size/type/duplicate validation.
- 76 core, 36 native and 15 service checks; isolated native fixtures reject existing libraries/sessions.

## 0.2.0 — 2026-10-02

- Adaptive virtualized cover grid; drag ordering with indicator/edge scroll and Alt+arrow alternative; favorite groups and filtered-out order preserved.
- Three views in Settings/F6/bottom toggle; 96-image memory cache, deduplicated downloads, 10-minute failure delay.
- Extracted-ZIP checks: 24 native assertions, 1,003-game fixture; 37 core and 15 service checks. Central versions/source checksums.
- Startup shortcut preserved on upgrades, removed on complete uninstall and restored when configured but missing.

## 0.1.0 — 2026-10-02

First local version: translucent Windows widget, SQLite, covers, tasks/goals, Steam OpenID server, tray and distribution packages.
