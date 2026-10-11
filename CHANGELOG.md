# Changelog

**English** · [Español](CHANGELOG.es.md)

## 0.8.30

- The game sheet now uses a centered, bounded content axis with consistent spacing and alignment. Artwork, stats, services and metadata keep balanced proportions as the window resizes; Miniature is unchanged.

## 0.8.29

- The game sheet uses the host window width and reflows artwork, progress and services to match available space. Forms and metadata adapt without changing Miniature mode.

## 0.8.28

- The Windows and browser apps now share the public website and game-sheet identity: Bagel Fat One headings, Inconsolata controls and copy, consistent panels, focus and all 22 existing palettes.
- Library, list, compact and grid layouts, forms, settings, account, menus, game details and Miniature use one shared style layer without changing collection data or virtualized row geometry.
- Current screenshots are captured from the real interfaces with local cover fixtures and bilingual example collections. Legacy WPF evidence is identified as historical.

## 0.8.27

- Individual, automatic and full achievement updates share result application. Concurrent reads of the same game share one request; individual updates no longer disappear during another review. The one-minute throttle applies only to successful automatic refreshes.
- Bounded concurrency, one retry for transient read failures and cancellation of pending requests on expired sessions, rate limits or service-wide failures. Per-game privacy failures do not stop other games.
- Responses are validated before replacing achievements; account or game changes during requests cannot overwrite current data. Manual goals, notes, privacy and partial progress are retained.
- The web saves results in batches and every two seconds, flushing on completion or cancellation. Supabase coalesces concurrent reads, caches progress for one minute and does not cache invalid definitions.

## 0.8.26

- Game details now follow the Figma design on Windows and the web, including cover art, playtime, privacy, achievement progress, synchronization, lists and game metadata. Tasks and notes save automatically.
- The top navigation retains Back, the game details title and Back to collection. The dark header contains the game title and actions; View achievements and Edit game share the same style, and the repeated Game details label is removed from the content.
- Fonts and icons are bundled locally; the sheet adapts to narrow windows and the web version caches its assets for offline use.

## 0.8.25

- Library’s selection bar now includes Add to My list for multiple selected games. The action appears when any selected game is not tracked yet and preserves privacy, other lists and progress; games already in My list are retained.
- Pressing the active Library or My list section button returns to its initial screen, clearing filters and selection. My list returns to all tracked games. Pressing Friends again returns from a friend or subsection to the main friends menu.
- Steam achievement updates now show the localized failure reason instead of only a generic message or error count. Background review identifies the first failing game, and expired web Steam sessions ask to link Steam again. Saved progress is retained on failure.

## 0.8.24

- Right-click a Library game to add it directly to My list. The action appears only when the selection includes games that are not tracked yet, works with multiple selected games and preserves privacy, other list memberships and progress.

## 0.8.23

- Library retains every game; My list remains a separate selection. Returning to Friends opens its main menu.
- Shared achievement groups let Checkpoint friends track a game together. Published manual achievements include descriptions and completion, with a clear Checkpoint label; manual achievements can be deleted separately from provider achievements.
- Windows Miniature always uses minimum opacity and stays on top, restoring normal preferences when leaving. Full window can be exited directly and switching to Miniature restores a small window.
- Game sheets use one outer scrollbar, show playtime in a clock card and provide a file picker for game executables. Credits recognize Yus.
- Rendering and cover work are reduced when the window is hidden or in Miniature. Collection ordering supports prioritizing games.
- Source formatting and fixed formatter versions make collaboration consistent while retaining the collaborator’s CSS changes.
- Friends’ achievements have a dedicated panel per game with larger completed counts, pending counts and a progress bar. Missing published data is explicit; story and tasks remain secondary. No additional private data is shared.

## 0.8.22

- Empty lists cannot open their details from collections or list management. Moving or removing the last member closes the sheet; adding a member enables it again. Search and status filters do not affect this rule.

## 0.8.21

- Optional No goal setting; completed story and achievement objectives disappear from cards and details without deleting their saved selection. Reopening progress restores them. Unknown or empty achievements do not count as completion.

## 0.8.20

- Visible status filters in the library and collections, also available in list details. Combine them with title search; changing list filters clears selections to keep batch actions safe.

## 0.8.19

- An accepted, saved IGDB cover can become the default for other users of the same game when the Steam cover is missing. It applies without another confirmation, including in Library, while retaining personal covers and rejected-image exclusions. Supabase stores only text and IGDB IDs; images remain local.

## 0.8.18

- Game sheets put Play, View achievements and Edit game at the top. Play, save and accept actions have stronger contrast and larger targets; web game sheets can also open locally installed Steam games. Manual games emphasize achievements without offering an invalid Steam launch.

## 0.8.17

- Steam descriptions missing from the game schema now fall back to matching player-achievement text. Old cached results are bypassed without revoking sessions. Hidden achievements remain protected until revealed; when Steam omits a hidden description from both responses, the UI explains the provider limitation.

## 0.8.16

- Minimize keeps Checkpoint on the Windows taskbar by default. Settings offers an independent option to hide in the tray when minimizing; closing to the tray remains a separate choice. The global show/hide shortcut restores a minimized window.

## 0.8.15

- Checkpoint screens share one window/browser surface, including settings, game/list sheets, achievements, notices and review tools. Back and Back to collection show a clear screen path. Nested forms preserve their parent; Miniature temporarily expands for full sheets and restores its dimensions. Background review controls remain visible on pages.

## 0.8.14

- Achievement review runs in the background in Windows and the browser, with progress and Stop review. Up to three requests overlap, with paced starts to respect Steam limits. Windows persists only the updated game; completed results and local edits survive stopping the job.

## 0.8.13 — Imported games in Miniature

Miniature falls back to Library when the default My list is empty, so fresh Steam imports remain visible. A source label identifies the fallback without changing tracking or friend privacy. Empty custom lists show a localized message and a Library action instead of a blank viewport.

## 0.8.12 — Uninstall shortcuts

The MSI adds Windows Installer uninstall shortcuts to the Start menu and installed folder. Spanish Windows receives Spanish shortcut names; other Windows languages receive English names. The normal uninstall confirmation remains visible, and user library/settings are preserved. Portable removal is documented separately.

## 0.8.11 — Exit on close by default

The X button now exits Windows Checkpoint completely for new settings. Hiding in the tray remains optional in Settings; existing saved choices are preserved. README screenshots now show simple example collections with real Steam covers.

## 0.8.10 — Text-only cloud storage

Supabase rejects new file uploads, including privileged Storage writes. Existing files are preserved. Windows shares only game metadata and progress; local covers and legacy image paths are excluded from publication. Migration and rollback tests document the policy.

## 0.8.9

- Settings now review achievements across the entire Library, including untracked and private games, with sequential navigation and an explicit update of every linked game. Local achievement changes are preserved.
- A missing-cover wizard checks Steam first and searches IGDB one game at a time: Accept, Next cover or Next game. Accepting saves immediately; dismissed images are excluded from later suggestions.
- Both reviews work in Windows and the web. Windows updates Steam and RetroAchievements; the web updates Steam and displays imported RetroAchievements and manual goals.

## 0.8.8 — 2026-10-04

- Save creates a typed list and opens it, including when starting in Library; creation no longer depends on the separate Create list button.
- Game context menus contain only game actions, with the three newest destination lists and a full list chooser.
- Multi-selection in Library and normal lists: move, add another membership, remove from a list, and change privacy. Membership removal retains the library game, notes and achievements.
- Full list sheets with counts, progress, private members, search, pages of 50 and individual or batch actions. All controls are localized in Spanish and English.

## Web — 2026-10-04

- The main web address opens a bilingual presentation. Open Checkpoint enters the existing app; app.html provides direct access. Returning to the overview retains same-tab sessions and local games. Installed PWAs open the app directly.

## 0.8.7 — 2026-10-04

- A simpler desktop appearance: square controls, restrained borders, neutral default colors and smaller section headings throughout Windows and the browser. All 22 themes remain available.
- Removed the slogan and promotional headings from the interface, shortcuts and README.
- Complete stable release with portable, MSI, source and SHA-256 files; older releases retained as drafts.

## 0.8.6 — 2026-10-04

- Fourteen new palettes, bringing the total to 22 themes: Cyber Purple, Electric Blue, Neon Lime, Black + Red, Black + Orange, Synthwave, Blue + White, Purple + Dark, Emerald + Neutral, Black + White + Accent, Navy + Cyan, Coral/Pink + Cream, Orange + Charcoal and Indigo + Soft Gray.
- Complete Spanish/English theme names, instant previews, persistence and light/dark handling in Windows and the browser. Readable derived text tones retain the requested primary colors.
- New packages include Checkpoint Attribution License 1.0 and creator-credit documents. Original v0.8.5 and earlier releases retain MIT.

## 0.8.5 — 2026-10-04

- Click any game card, title, cover or Miniature row to open its full game sheet; buttons retain their direct actions.
- Status, goal, achievement overview, playtime, privacy, lists, tasks, notes, dates and provider IDs, with dedicated Edit game and View achievements actions.
- Localized Windows/browser sheets, keyboard access and regression checks.

## 0.8.4 — 2026-10-04

- IGDB cover suggestions when Steam artwork is unavailable, with closest-title matching and explicit preview approval in Windows and the browser.
- Declined images persist per game; the game editor requests fresh alternatives while excluding rejected IDs. Accepted images are stored locally and retained by compatible backups.
- Server-only Twitch credentials, restricted image proxy, bounded requests and cached metadata. Bilingual controls, screenshots and regression tests.

## 0.8.3 — 2026-10-03

- Visible, localized **Exit miniature view** button above the game list: one click restores and saves the previous normal layout.
- The control reserves its own space without covering names or states; it disappears in normal views.
- English/Spanish real WebView2 tests verify visibility, placement and restoration. Updated screenshots and documentation.

## 0.8.2 — 2026-10-03

- Direct achievement buttons and counters on game cards in Windows and the browser; compact and cover-grid layouts accommodate the new action.
- Prominent completion summaries, progress bars and achievement cards with larger titles.
- Per-achievement Show/Hide description actions, with secret names and descriptions protected until explicitly revealed.
- Spanish/English controls, screenshots and regression checks for descriptions, secrets and direct navigation; Miniature retains its name/status-only list.

## 0.8.1 — 2026-10-03

- Optional daily startup update checks and a bilingual Settings → Updates window.
- Official GitHub release/version/architecture selection, bounded HTTPS downloads and size/SHA-256 verification against both asset digest and checksum file.
- Explicit download/install action creates a library backup, closes the app, rechecks the hash and upgrades with the per-user MSI before restarting with the same data folder.
- Portable users migrate to the Start menu installation; original portable files are preserved.
- Tests use simulated installer downloads and verify helper integrity rejection without installing into the user's personal environment.

## Web sign-in fix — 2026-10-03

- The visible Sign in action opens an existing-account form with username/password only.
- Registration requires choosing Create account; switching back clears the password and registration-only fields disappear.
- Spanish/English browser regression checks verify both modes and password authentication.

## 0.8.0 — 2026-10-03

- Windows game detection opens an independent pending-achievement window once per game process session; default enabled and configurable.
- Steam installation folders and optional executable/window-title associations for emulators.
- Direct RetroAchievements game progress, personal DPAPI-encrypted credentials and Hardcore mode.
- Add/remove personal achievements and completion overrides, restore hidden official entries, retain manual choices across provider refreshes.
- Refresh open detected windows every 60 seconds; Steam's existing 15-minute server cache still applies.
- Private manual goals and credentials stay out of friend publications. Desktop backups and web normalization retain the new library fields.
- Spanish/English controls and setup documentation. RetroAchievements tested with simulated HTTP; real personal credentials remain user setup.

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
