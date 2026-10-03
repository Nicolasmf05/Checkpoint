# Validation

## 0.7.4 — eight themes

417 checks passed: 144 core, 18 Node service, 18 Edge, 175 native controller, 4 interface model and 58 actual CSS checks. The 28 new checks cover legacy preferences, unknown identifiers, SQLite persistence for each theme, localized names, eight distinct rendered backgrounds, preview in main/settings, cancel/save and themes in Miniature/notices. Friends also retains the selected palette. Captures use the extracted ZIP with isolated data. No accessibility certification is claimed; screen readers, physical keyboard and older hardware remain unverified.

## 0.7.3 — visible keyboard shortcuts

389 checks passed: 130 core, 18 Node service, 18 Edge, 175 native controller, 4 interface model and 44 actual CSS checks. Seven new checks cover visible and accessible gestures, Spanish/English help, blocking underlying commands, Escape without hiding the window, Miniature context-menu help and restoration of its selected row focus. Captures come from the extracted ZIP with isolated data. Occupied global gestures are explained; tests do not close another user instance. Physical keyboard and screen-reader validation remain pending.

## 0.7.2 — automatic synchronization

382 checks passed: 130 core, 18 Node service, 18 Edge, 175 native controller, 4 interface model and 37 actual CSS checks. Startup and the timer now use the same library and achievements sync as Refresh. Existing tests cover preservation of manual states, deduplicated imports, Steam errors and localized interfaces. Startup with a real Steam account remains unverified; service tests use simulated responses.

## 0.7.1 — window modes

375 checks passed: 130 core, 15 service, 14 Edge, 175 native controllers, 4 frontend model and 37 actual CSS checks. New coverage verifies work-area dimensions and opaque CSS in Full window, SQLite mode persistence without overwriting small bounds/opacity, return to Small window, Miniature context transitions and localized Settings. Older settings default to Small window. Full-window screenshots use actual WebView2. Physical multi-monitor/DPI, installed upgrades and real-account limitations remain pending.

## 0.7.0 — CSS interface

366 checks passed: 128 core, 15 service Node, 14 Supabase Edge, 175 native controller regression, 4 frontend model and 30 actual WebView2 checks. The extracted ZIP runs both native and CSS test suites with fresh isolated data. CSS checks exercise local cover loading, game/notes SQLite persistence, input acknowledgement, password preservation without snapshot disclosure, Spanish/English, settings, friends login, Miniature menus/focus/editing, 1,004-game virtualization, grid, light compact view, keyboard ordering, safe text rendering and blocked remote navigation. Screenshots are captured from WebView2 rather than legacy WPF controls.

The legacy WPF checks validate existing controllers and simulated Steam/Supabase behavior; they do not prove the new HTML interactions by themselves. Live Steam accounts, two real Checkpoint accounts, installed MSI/MSIX upgrades, physical drag/keyboard, multi-monitor DPI, screen readers and low-end hardware remain pending. WebView2 Evergreen Runtime is an additional requirement; no memory reduction or signing reputation is claimed.

## 0.6.16 — complete friend-code name

332 checks passed: 128 core, 15 Node, 14 Edge and 175 WPF. Tests cover complete presentation, case/whitespace normalization, length and character validation, existing-identity lookup with both formats, Spanish/English Account windows and the 23-character input. Stored service codes and historical schema identifiers remain compatible with released clients. HTTP responses are simulated and actual windows use isolated data; real accounts and installation remain pending.

## 0.6.15 — consistent language

316 checks passed: 118 core, 15 Node, 14 Edge and 169 WPF. Coverage checks literal messages and XAML resources, unknown system and Steam errors, thread culture, notices and language choices in actual Spanish and English windows. Windows system dialogs follow the operating system language. Tests use isolated data; real accounts, installation, physical keyboard and screen readers remain pending.

## 0.6.14 — restore the normal layout

300 checks passed: 108 core, 15 Node, 14 Edge and 163 native WPF. Six new checks exercise list, compact and grid through real Settings, entering Miniature, another save, SQLite settings reload and background/game exit menus. Persisted flags and restored templates are checked. Existing tests retain F6 cycling, Search, dimensions and text. Installed packages, physical keyboard and real accounts remain pending.

## 0.6.13 — configurable Miniature text

294 checks passed: 108 core, 15 Node, 14 Edge and 157 native WPF. Five new checks save size 18 through Settings and reload SQLite, verify larger names/states and row height, cancel without changing the saved value, verify the English label and reduce page jumps for large text. Native large-text screenshot included. Real dialogs use isolated data; physical keyboard, screen reader, installed packages and real accounts remain pending.

## 0.6.12 — page navigation

289 checks passed: 108 core, 15 Node, 14 Edge and 152 native WPF. Five new checks cover PageDown at the final boundary, page advance with focus, PageUp/first boundary, larger jumps in a taller viewport and reading-offset retention after an unfocused refresh. The collection has 1,003 games with fewer than 30 realized rows. The no-focus check permits focus on the outer window without acquiring list focus. Search now closes its menu first; its existing focus test passes. Native page-navigation screenshot included. Physical keyboard, installed packages and real accounts remain pending.

## 0.6.11 — adding from empty Miniature

284 checks passed: 108 core, 15 Node, 14 Edge and 147 native WPF. Four new checks cover Add game in empty Miniature, cancellation with no data on disk, saved notes/new row without covers or changing view, and English labels/Ctrl+N in both menus. The real background menu is opened and native controls captured using isolated data. Physical keyboard/drag, installed packages and real accounts remain pending.

## 0.6.10 — editing from Miniature

280 checks passed: 108 core, 15 Node, 14 Edge and 143 native WPF. Five new checks exercise the game-menu editor, persisted notes and same-game focus, F2/cancellation, untracking with cleared selection and English label/shortcut. Real dialogs and controls use isolated data; this does not replace physical keyboard testing. Native menus captured in both languages. Installed packages and real accounts remain pending.

## 0.6.9 — visible search

275 checks passed: 108 core, 15 Node, 14 Edge and 138 native WPF. Five new checks cover current-query selection, visible collection search from Friends, saved normal view from the Miniature menu without losing its dimensions, search focus after closing the game menu and the English label/shortcut hint. Tests exercise the same method used by Ctrl+F; physical modifier-key input is not simulated. Native menu screenshots updated in both languages. Physical keyboard, installed packages and real accounts remain pending.

## 0.6.8 — Miniature window actions

270 checks passed: 108 core, 15 Node, 14 Edge and 133 native WPF. Seven new checks cover checked values, application/persistence of Always on top from background and game menus, resize locking, drag cursor, unlocking and immediate English translation. Native menus are captured in both languages. Physical drag gestures, installed package upgrades and real accounts remain pending.

## 0.6.7 — stable selection and window bounds

263 checks passed: 108 core, 15 Node, 14 Edge and 126 native WPF. Tests retain selection/focus through refresh and reordering, avoid acquiring absent keyboard focus, clear removed selections and restore a distant virtualized row among 1,003 games with fewer than 30 realized rows. Bounds tests expand from the screen corner, enter Miniature from a partly offscreen position and show an offscreen widget. Restored dimensions are capped by the available work area, including small desktops. These tests use the current monitor; physical multi-monitor/DPI transitions, installed packages and real accounts remain pending.

## 0.6.6 — miniature keyboard controls

255 checks passed: 108 core, 15 Node, 14 Edge and 118 native WPF. Routed key tests exercise End/Up/Home, first-row boundaries, Enter/Space menus, focus restoration on menu closure and state change, and an End jump to a distant virtualized row among 1,003 games with fewer than 30 realized rows. Container focus waits for deferred layout; the test waits for that focus change. Native focus screenshot included. These automated key events do not replace physical keyboard/screen-reader validation. Installed/real-account limitations remain pending.

## 0.6.5 — miniature state menu

247 local checks passed: 108 core, 15 Node, 14 Edge and 110 native WPF. Tests open real row context menus, check all five states/current selection, persist story completion without leaving Miniature, reopen a story and clear its date, and verify English menu labels. Native menu captures are included in both languages. Physical keyboard/drag, installed-package and real-account limitations remain pending.

## 0.6.4 — miniature view

243 local checks passed: 108 core, 15 Node, 14 Edge and 106 native WPF. Miniature tests cover actual Settings activation/persistence, separate dimensions and resizing, name/state-only templates, hidden chrome, no images/actions/progress bars, readable theme colors, immediate English labels, context-menu exit and view cycling. Spanish/English captures are rendered from native controls. ZIP/MSI and an SDK-validated unsigned MSIX preview were generated. Physical keyboard/drag, installed updates/uninstall and real-account limitations remain pending.

## 0.6.3 — lightweight mode

233 checks passed: 108 core, 15 Node, 14 Edge and 96 native WPF. The actual Settings checkbox is tested for save, persistence, cancel and disabling; collection images remain hidden with an empty decoded cache. Simulated private Storage requests prove that friend progress remains visible without cover downloads in lightweight mode and that covers return when disabled. English localization is verified. ZIP/MSI and an SDK-validated unsigned MSIX preview were generated. Installed-package and real-account limitations remain as described below.

## 0.6.2 — lower resource usage

222 checks passed: 108 core, 15 Node, 14 Edge and 85 native WPF. Native checks include cover release/reload, a 40-image cache stress case and a 1,003-game grid with offscreen reference checks. Unsigned ZIP/MSI and the MSIX packaging preview were generated. The preview matches all 283 application files. Compared with 0.6.1, extracted files fell from 173.72 to 158.00 MiB and ZIP size from 73.85 to 68.44 MiB. Both packages include first-run notices in English/Spanish. No minimum RAM/CPU benchmark on older hardware, installed MSI/MSIX test or real-account validation is claimed.

## MSIX preparation before 0.6.2

MakeAppx schema/content validation passed for the unsigned x64 preview. Unpack verification matched all 485 application files by SHA-256 and checked icon sizes, languages, manifest and PE architecture. Five invalid identity/version input cases were rejected. The extracted MSIX payload passed 79 native WPF assertions using an isolated data folder and the real Windows profile for DPAPI. This does not test installed MSIX behavior, upgrades, uninstall, startup or Store certification. No package was installed or submitted to Store.

## 0.6.1 packaging

The portable archive contains one top-level `Checkpoint` folder. Verification enforces that layout and launches the extracted application from it. The EXE/MSI remain unsigned; the reported SmartScreen block requires a trusted signing/distribution route. No suitable code-signing certificate was found in the current-user certificate store. Functional tests do not validate SmartScreen reputation.

## 0.6.0 Steam integration

Local distribution checks passed: 108 core, 15 legacy Node, 14 Supabase Edge HTTP/security and 79 native WPF assertions (216 total). Native checks include Supabase URL validation, bound authorization, DPAPI restoration, malformed-session preservation, language parameters and unlinking. ZIP and MSI were generated; the MSI remains unsigned and installation/uninstallation is untested.

The `checkpoint-steam` function and private-state migration were deployed to the project. Twenty PostgreSQL assertions passed with rolled-back fixtures: anonymous/authenticated permissions, atomic flow consumption, nonce replay, expiry, limits and revocation. HTTP Steam state permission checks use only the public publishable key. The legacy JWT gateway setting is disabled for this function only, following approval. Live health reports Steam configured; unauthenticated library/achievements return 401, invalid callbacks return 400, login start/pending polling return 200 and a wrong polling secret returns 400. The operator key stays in Supabase secrets. Real Steam account sign-in, upstream key validity and actual library/achievement imports still require a person completing the link.

## 0.5.0

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
