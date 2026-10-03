# Changelog

**English** · [Español](CHANGELOG.es.md)

## 0.7.6 — 2026-10-03

- Tracked games visible to accepted friends by default, with private-from-first-save, later withdrawal and a separate Private games view.
- Multiple local lists with non-duplicating memberships, creation, rename, removal and default-private additions.
- Preserve prior explicit privacy decisions and privacy/list data in backups; pending public operations are reconciled before private withdrawals.
- Available in Windows and the browser, fully localized in English and Spanish.

## 0.7.5 — 2026-10-03

- Configurable local, Miniature, ordering and global Windows shortcuts, with conflict validation, persistence, default reset and updated hints.
- A usable browser application on GitHub Pages, with local storage, JSON backups, Steam and Checkpoint friends.
- Exact-origin Steam CORS preserves authentication; web sessions stay out of backups.

## 0.7.4 — 2026-10-03

- Eight themes: Dark, Light, Midnight, Ocean, Forest, Plum, Amber and High contrast.
- Theme selector with live preview throughout the CSS interface; Save persists the choice and Cancel restores the original appearance.
- Preserve legacy light/dark preferences, validate unknown identifiers and keep opacity/window mode independent.
- Fourteen core and fourteen actual CSS theme checks; screenshot gallery included.

## 0.7.3 — 2026-10-03

- Visible shortcut strip, keyboard badges in menus, tooltips and accessible key gestures.
- Keyboard shortcuts guide via F1 or its button, fully localized; Miniature offers F1 and a context-menu entry without adding list controls.
- Modal help preserves focus and closes with Escape; an occupied global shortcut is explained. Seven new actual CSS checks.

## 0.7.2 — 2026-10-03

- Automatically sync the Steam library, playtime and tracked achievements on each launch and at the configured interval (30 minutes by default).
- Settings explain automatic startup and periodic sync. Offline errors preserve saved progress; overlapping requests are skipped.

## Hosted Steam service — 2026-10-03

- Request family licenses and merge recently played games with owned games; deduplicate AppIDs and preserve total playtime.
- Borrowed visible games can sync the linked player's achievements. Private-library restrictions remain enforced.
- Replace old persistent library caches; existing desktop versions use the server update without reinstalling.
- 36 HTTP checks pass across both service implementations; real family-account coverage remains pending.

## 0.7.1 — 2026-10-03

- Three explicit window modes in the header, Settings and context menu: Full window, Small window and Miniature.
- Full window fills the current monitor work area with 100% opacity, while preserving small-window size, position and translucency for returning.
- Saved mode, compatibility with older preferences and actual CSS transition/settings tests.

## 0.7.0 — 2026-10-03

- Visible interface migrated to local HTML/CSS/JavaScript in transparent WebView2: collection, Miniature, friends, forms and app notices.
- Native C# retains SQLite, Steam, Supabase, tray, backups and existing form validation; data formats remain compatible.
- Virtualized rows, keyboard menus/order, consistent Spanish/English, dark/light themes and local cover endpoint.
- **New requirement: Microsoft Edge WebView2 Evergreen Runtime.** .NET remains bundled; WebView2 is shared and installed separately if missing.
- Real WebView2 package tests, frontend model tests and updated current screenshots/documentation. Unsigned-download warning remains applicable.

## 0.6.16 — 2026-10-03

- Friend codes display and copy the full `checkpoint-` name with their original 12 characters.
- Friend input accepts 23 characters; labels and notices use the complete name in Spanish and English.
- Previously copied codes still resolve the same account through a service compatibility boundary. Accounts, friendships and publications are unchanged.
- Updated documentation and screenshots; format, compatibility and actual Account window tests in both languages.

## 0.6.15 — 2026-10-03

- App-owned notices use buttons in Checkpoint's language, independently of Windows.
- External technical errors use translated explanations; known validation messages retain their translations.
- Translated recovered-task counters, untitled publications, language choices and accessible names.
- Saved language loads before startup notices and applies to thread culture.
- Automatic catalog checks and window and error tests in both languages.

## 0.6.14 — 2026-10-03

- Exit miniature view restores the previous normal layout: list, compact or cover grid. Settings keeps that layout when entering Miniature or saving while it is active.
- Both background/game exit menus persist the restored choice. F6 keeps its four-view cycle; Search still opens the normal list.

## 0.6.13 — 2026-10-03

- Settings adds Miniature text size, 12–20 with default 12, saved independently of other views. Names/states and row height grow together.
- Page navigation measures larger rows and reduces the jump accordingly. No new runtime dependency.

## 0.6.12 — 2026-10-03

- Miniature supports PageUp/PageDown with a jump based on visible height and actual row size; keeps keyboard focus and clamps to first/last game.
- Search closes the context menu before focusing the visible search field.
- Regression coverage confirms unfocused reading position persists through refresh without losing virtualization.

## 0.6.11 — 2026-10-03

- Miniature background/game menus include Add game with its Ctrl+N hint, including when My list is empty.
- Uses the normal editor without leaving Miniature; saving adds the row, canceling leaves no data. Empty view keeps its minimal layout.

## 0.6.10 — 2026-10-03

- Miniature game menus add Edit game; F2 opens the selected game editor. Notes/tasks and normal editor actions are available without changing view or adding row controls.
- Closing the editor restores focus by game identity when the game remains visible; untracking it clears selection.

## 0.6.9 — 2026-10-03

- Ctrl+F opens visible collection search from Miniature or Friends; it selects the current query instead of focusing a hidden field.
- Miniature background/game menus add Search games with its Ctrl+F hint. Searching leaves Miniature for the normal list and preserves its saved dimensions.

## 0.6.8 — 2026-10-02

- Miniature menus expose checked Always on top and Lock position and size actions; changes apply immediately and persist.
- Background and game menus share the same window actions. A locked drag strip uses a normal cursor; names/state rows remain unchanged.

## 0.6.7 — 2026-10-02

- Miniature retains selected game and keyboard focus across refreshes and ordering changes, including virtualized rows. Refresh without list focus does not acquire it; removed games clear selection.
- Switching views and showing the widget clamp its bounds to the current monitor work area.

## 0.6.6 — 2026-10-02

- Miniature keyboard navigation with Up/Down/Home/End and Enter/Space state-menu access.
- Visible keyboard focus outline and focus restoration after menu closure/state changes; normal views retain their styles and shortcuts.

## 0.6.5 — 2026-10-02

- Miniature game context menu: five localized states with the current one checked; persist changes without leaving the view.
- Context menu retains Settings/Exit; keyboard-focusable rows expose accessible names with game/state.
- Native tests open real menus, save/reopen story completion and verify English labels.

## 0.6.4 — 2026-10-02

- Miniature view: only names/states, no covers or surrounding controls; independent saved dimensions, drag strip and resize grip.
- Settings/F6 activation and right-click/F6 exit; English/Spanish states and menu, verified theme contrast and native captures.

## 0.6.3 — 2026-10-02

- Optional saved lightweight mode: no collection/friend cover rendering or new image requests; clears decoded cache without deleting library/cover files or changing sharing.
- Immediate restoration when disabled; Spanish/English setting and cancellation behavior covered by native tests.

## 0.6.2 — 2026-10-02

- Decoded cover cache capped at an estimated 8 MiB/32 entries; recycled cards release images and reload correctly.
- Skip periodic friend-progress refresh while hidden/minimized, preserving publication retries.
- Keep only English/Spanish framework resources and include bilingual first-run notices in ZIP/MSI.
- Prominent SmartScreen/certificate funding policy and no-runtime/no-admin/offline manual-library requirements.

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
