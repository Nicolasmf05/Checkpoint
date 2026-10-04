# Checkpoint

**English** · [Español](README.es.md)

A translucent Windows desktop widget for the games you want to finish, with cover art, goals and progress shared with Checkpoint friends.

**[Checkpoint overview](https://nicolasmf05.github.io/Checkpoint/)** · [Open the web app](https://nicolasmf05.github.io/Checkpoint/app.html) · [Web guide](docs/en/WEB.md) · [Configure shortcuts](docs/en/SHORTCUTS.md). The web library lives in this browser; use JSON to transfer your Windows games.

> **IMPORTANT — SMARTSCREEN:** GitHub EXE/MSI downloads have no digital signature or publisher certificate. Their unsigned status and lack of reputation can trigger “Windows protected your PC”. **I have no certificate because it costs money; I will not buy one until Checkpoint income covers at least its cost. Until then, these downloads will remain without a certificate.** Signing does not guarantee that warnings immediately disappear. [Read the notice and requirements](docs/en/DOWNLOAD-NOTES.md).

![Checkpoint CSS interface](docs/screenshots/css-widget-en.png)


Settings → Updates checks new releases on launch and downloads/installs a verified MSI with a pre-update backup and preserved data. [Details and portable behavior](docs/en/UPDATES.md).

Settings also includes **Review all achievements** and **Find missing covers**. [Review achievements and missing covers](docs/en/LIBRARY-REVIEW.md).

## Game detection and personal achievements

The Windows edition opens pending achievements when a saved game is detected. Steam, RetroAchievements and manual goals support adding, removing and personal completion. RetroAchievements requires your username and personal Web API key in Settings, plus a game ID and emulator association. [Setup, privacy and limitations](docs/en/ACHIEVEMENTS.md).

## 22 themes

Settings → Theme includes Cyber Purple, Electric Blue, Neon Lime, Black + Red, Black + Orange, Synthwave, Blue + White, Purple + Dark, Emerald + Neutral, Black + White + Accent, Navy + Cyan, Coral/Pink + Cream, Orange + Charcoal and Indigo + Soft Gray, alongside the original eight palettes. Theme previews apply throughout Windows and the browser. [Palette guide](docs/en/THEMES.md).

## Full game details

Click a game card, title or cover to open its complete sheet, including status, goal, achievements, playtime, privacy, lists, tasks and notes. Miniature rows open the same sheet without changing window mode. Editing and achievements remain available as explicit actions.

## Missing game covers

When a listed game has no working Steam cover, Checkpoint proposes the closest IGDB match for your approval. Declined images are remembered; Edit game → Search IGDB for another cover requests a fresh alternative. Accepted images are stored locally. Available in Windows and the browser. [Setup and behavior](docs/en/IGDB.md).

## Screenshots

Current captures come from the actual local HTML/CSS interface in WebView2, using isolated test data. Game titles and progress are examples; the green cover is a local fixture. [Current gallery and archived screenshots](docs/en/SCREENSHOTS.md).

| Friends login | Miniature |
| --- | --- |
| ![Username and password](docs/screenshots/css-friends-login-en.png) | ![Names and states](docs/screenshots/css-miniature-en.png) |

## Download and run

Current version: **0.8.10**. Build outputs in `dist`:

- `Checkpoint-0.8.10-win-x64.zip`: portable edition. Extract the ZIP and open `Checkpoint/Checkpoint.exe`. All files stay inside the `Checkpoint` folder.
- `Checkpoint-0.8.10-win-x64.msi`: per-user installer with Start menu shortcut and Windows uninstall support.
- `.sha256` files: integrity checksums.

Download the portable ZIP or MSI from [GitHub Releases](https://github.com/Nicolasmf05/Checkpoint/releases/tag/v0.8.10), or build from source below. The **Build and verify** GitHub Actions workflow also produces artifacts after a successful run. Packages include the .NET runtime; end users need no development tools. Windows 11 x64 is the target. Windows 10 and ARM64 hardware have not been validated; ARM64 packaging is supported. The MSI and EXE are unsigned; Windows may warn or block them. See [Windows security messages](docs/en/WINDOWS-SECURITY.md). No private certificates are included.


Microsoft Store packaging is being prepared separately: [MSIX build and submission guide](docs/en/MICROSOFT-STORE.md). The unsigned MSIX preview is for developer validation and does not remove warnings from the current GitHub downloads.

With Steam linked, the library, playtime and tracked achievements sync automatically on each launch and every 30 minutes by default. Change the interval in Settings to 15, 30, 60 or 120 minutes. New games appear in Library; manual states and notes are preserved.


## Requirements to use it

Windows x64 (Windows 11 tested), **Microsoft Edge WebView2 Evergreen Runtime**, a user-writable folder and disk space. WebView2 is shared with other applications and included in Windows 11; if missing, [install it from Microsoft](https://developer.microsoft.com/microsoft-edge/webview2/). **No .NET/development-tool installation, administrator privileges, account or Internet connection is needed for the manual library.** Steam, online covers and friends are optional. Minimum RAM/CPU on older hardware is not yet measured. [Requirements and resource usage](docs/en/DOWNLOAD-NOTES.md).

## Interface technology

The visible collection, Miniature, friends, editors, settings and app notices use **local HTML, CSS and JavaScript in WebView2**. Style them in `src/Checkpoint.App/Web/app.css`. C#/.NET handles SQLite, Steam, Supabase, the tray and native window. WPF remains the window shell and existing form controllers; Windows file pickers and the missing-runtime notice remain native. No JavaScript framework or remote interface is required. [Architecture and development](docs/en/CSS-INTERFACE.md).

## Themes

Choose **Settings → Theme**: Dark, Light, Midnight, Ocean, Forest, Plum, Amber or High contrast. Colors preview immediately across the main window, Miniature, friends, dialogs and keyboard help. **Save** keeps your choice; **Cancel** restores the previous appearance. Existing light/dark preferences retain their appearance. Opacity and window mode remain independent. Native Windows file pickers use the system appearance.

## Window modes

Use the header selector or Settings → Window mode: **Full window**, **Small window**, **Miniature**. Full window fills the current monitor work area, leaving the taskbar available, and always uses **100% opacity**. Small window restores its saved size and chosen opacity. Miniature keeps the names/states-only list and its own saved dimensions; right-click to choose any window mode. The mode persists across restarts. Collection layouts (list, compact, grid) remain separate.

## Features

- **Quick state changes in Miniature:** right-click a game to select its state. The current state is checked; changes persist without leaving the names/states-only list.
- **Miniature window actions:** right-click the background or a game to toggle Always on top or Lock position and size. Checked values reflect saved settings.
- **Search from any view:** Ctrl+F opens collection search and selects its current query. From Miniature it returns to the normal list, preserving the small size for next time; from Friends it returns to the collection. Miniature menus also offer Search games.
- **Edit from Miniature:** right-click a game → Edit game, or F2 on the selected game. Opens its normal editor without leaving Miniature; closing returns focus if it remains in My list.
- **Add from Miniature:** right-click the background or a game → Add game, including when My list is empty. Opens the normal editor without leaving Miniature; Ctrl+N remains available.
- **Page navigation in Miniature:** PageUp/PageDown move by a visible page, adapting to window height and keeping the active row focused.
- **Miniature text size:** Settings → Miniature text size, 12–20 (default 12). Names, states and row height grow together; other views keep their text size.
- **Return from Miniature:** the visible **Exit miniature view** button above the list (or right-click → Exit miniature view) restores the previous list, compact or grid layout. Saving Settings while Miniature is active preserves that choice. F6 keeps cycling all four views; Search opens the normal list.
- **Miniature keyboard controls:** Up/Down select a game, Home/End jump to the first/last, Enter/Space open its state menu. A focus outline marks the active row; focus returns after closing the menu or changing state.
- **Miniature view:** a name/status list in a small translucent window, with a visible exit button. Settings → Collection view → Miniature, or cycle with F6. Use the visible **Exit miniature view** button above the list. Resize it independently from the normal widget; drag the top edge. Shows My list with no filters or cover downloads.
- **Optional lightweight mode:** Settings → Lightweight mode (no covers). Hides collection/friend covers, skips new image downloads and clears the decoded cover cache. Progress and saved images are preserved.
- Frameless, movable, resizable widget; saved bounds; background opacity 35–100%; readable text/covers. Alpha translucency persists across focus changes, without Acrylic blur.
- Dark/light themes, list/compact/cover-grid views, always-on-top and position locking.
- **English and Spanish**, switchable immediately in Settings → Language. Choice persists; user titles, notes and custom goals remain unchanged.
- My list and Library, search/status filters, virtualized rows for large collections.
- Steam covers and local caching, custom images, favorites and persistent drag/keyboard ordering.
- Pending, playing, paused, story finished and abandoned states.
- Story, all-achievements and custom goals; notes/checklists; optional manual story percentage separate from task/achievement counters.
- Steam import and achievements through the included server; secret achievements hidden by default. Manual sync or every 15/30/60/120 minutes, up to 20 tracked games per cycle, least recently updated first.
- Tray, optional Windows startup and keyboard shortcuts.
- SQLite library, complete backups with covers, non-destructive imports and legacy JSON support.
- Restore the last 20 deleted games with their data/covers after restarting.
- Checkpoint accounts, requests, accepted friendships, blocks and default-visible games and per-game privacy through Supabase.

**Story completion and all achievements are independent.** Sync never decides you finished a story and preserves previous progress if Steam fails.

Miniature retains selection/focus during updates; changing view or showing the widget keeps it within the current screen work area.

## Lists and privacy

**My list is visible to accepted Checkpoint friends by default when signed in.** Imported Steam games remain in Library until tracked. In the game editor, enable **Private to my friends** before the first save, or right-click a saved game to make it private later. Private games appear in the separate **Private games** view and stay out of the normal/custom lists.

Use the list selector and **Manage lists** to create, rename or remove up to 30 custom lists. Select memberships in the game editor: a game can belong to several lists without duplicate records. Removing a list preserves its games. Enable **Create new games as private** to change the default for future additions. Previous explicit withdrawals are preserved as private. List names and membership remain local; friends see visible game progress, without your grouping. [Guide](docs/en/LISTS.md).

## Friends

Open Friends and create an account: username of 3–24 ASCII letters, digits or underscores, password of at least eight characters. Usernames are case-insensitive. No real email or confirmation is requested. Keep your password safe: recovery is unavailable.

Copy your code from Account. Another user sends a request with that code. After acceptance, each can view the other’s tracked games unless marked private: title, platform, cover, state, goal and counters. Notes, task names and individual achievement details stay private.

Pending changes persist per account; per-game privacy persists in the library. Offline changes retry when connected; a withdrawal takes effect after server acknowledgment, so the old publication can remain visible until then. Signing out does not withdraw games. Cross-device conflicts require an explicit choice to publish your local version. Friends refresh every 60 seconds while the tab is open. The private library stays local; full cloud restore is unavailable.

Public Supabase configuration is included. Friends use Checkpoint accounts independently of Steam. See [setup](docs/en/SUPABASE.md), [friends scope](docs/en/FRIENDS-PLAN.md) and [privacy](docs/en/PRIVACY.md).

## Controls and data

**Keyboard shortcuts are visible at the bottom of the window.** Click **Keyboard shortcuts · F1** or press **F1** for the complete localized guide. In Miniature, use F1 or the right-click menu; the list remains names and states only. Escape closes help and returns focus.

`Ctrl+Alt+C` show/hide; `Ctrl+N` add; `Ctrl+F` search; `F6` cycle views; `Ctrl+Z` restore last deletion when not editing text; `Escape` hide. Use the tray if the global shortcut is occupied.

Drag `⠿` above/below another game or focus the handle and press `Alt+↑` / `Alt+↓`. Favorites stay above other games; reorder within each group. Filtered-out games retain relative order. Grid columns adapt to width.

Data lives in `%LOCALAPPDATA%\Checkpoint`. Uninstalling preserves the library. Steam/Checkpoint tokens use Windows DPAPI for the current user and are excluded from backups.

## Backups and recovery

Settings → Backups → Export creates a `.checkpoint` archive of the current collection and custom covers. Import adds games/images, preserving existing games matched by local ID or Steam ID. Steam covers download again online. Legacy JSON omits images.

Limits: 10,000 games, 8 MB/image, 25 MB collection data, 200 MB extracted. PNG headers are validated before decoding (4,096 pixels/side, 12 million pixels). References are never extraction paths. Failures preserve existing data.

The ↶ button and Settings → View deleted games restore the last 20 deletions with notes, tasks, progress, favorites, order and covers. Existing library data wins on conflicts. Backups include game privacy, memberships and empty list names; they exclude deletion history, sessions and the account publication queue.

Version 0.3 introduced SQLite schema 2. Version 0.5 opens older libraries; use 0.3+ after migration. Legacy JSON remains compatible; complete backups require 0.3+.

## Steam service

The local library works without Steam. The Supabase Steam service is deployed and configured. The distribution includes its public endpoint and the Checkpoint public project configuration; end users do not need API keys or development tools. Press Link Steam and finish sign-in on Steam; imports appear in Library. Friends remain independent Checkpoint accounts. See [Steam setup](docs/en/STEAM-SERVICE.md). Open the app, select Link Steam and finish sign-in on the official Steam page. Game details must be visible to import the library and achievements.

Keep the Steam API key in a server environment secret, never the app or GitHub. Users sign in on Steam through OpenID; private game details remain inaccessible. See [Steam setup and limits](docs/en/STEAM-SERVICE.md). Advanced connection supports a local development server.

## Build and test

Requirements: Windows, PowerShell 7, .NET SDK 10, Node.js 22+ and WebView2 Evergreen Runtime. Scripts use `.tools/dotnet` if available; that directory is not published. The icon is included (`scripts/New-Icon.ps1` regenerates it).

```powershell
dotnet restore Checkpoint.slnx --configfile NuGet.config
dotnet build Checkpoint.slnx -c Release --no-restore
dotnet run --project src/Checkpoint.App -c Release --no-build
dotnet tool install wix --version 5.0.2 --tool-path .tools/wix --configfile NuGet.config
./scripts/Build.ps1 -Installer
# With your hosted Steam service:
./scripts/Build.ps1 -Installer -ServiceUrl https://your-service.example
```

`service-config.json` contains only a public URL; `supabase-config.json` contains public project configuration and its publishable key, never privileged keys. MSI installs into `%LOCALAPPDATA%\Programs\Checkpoint` without enabling Windows startup automatically.

```powershell
dotnet run --project tests/Checkpoint.Tests -c Release
node --test server/test/service.test.mjs
node --test tests/web/ui-model.test.mjs
# Always use a new isolated data folder:
dotnet run --project src/Checkpoint.App -c Release --no-build -- --data-dir "$PWD/.qa/example" --demo --diagnostics --smoke-test "$PWD/.qa/render"
```

Native tests reject existing libraries/sessions/publications and exercise real controls with simulated Steam/social responses. Demo adds examples only to an empty library. Build extracts and tests its self-contained ZIP, including language switching and virtualized rendering of 1,003 games. The CSS suite separately launches the actual HTML interface with 1,004 games, forms, menus and language changes. Reports and captures: `.qa/package-…/render` and `.qa/web-package-…/render`.

Repeat using `./scripts/Verify-Package.ps1 -ZipPath ./dist/Checkpoint-0.7.1-win-x64.zip`. ARM64 execution needs Windows ARM64 hardware. [Validation](docs/en/VALIDATION.md) lists remaining real-account/Storage and clean-machine MSI checks.

## License and creator credit

**Checkpoint originally created by Nicolasmf05.** Current source is licensed under [Checkpoint Attribution License 1.0](LICENSE): redistributed and publicly hosted derivatives must retain an accessible creator credit and original project link. Use and commercial distribution remain permitted subject to these terms. [Attribution requirements](ATTRIBUTION.md). Original v0.8.5 and earlier downloads retain their included MIT license.

## Complete releases

Only the newest complete stable release is public. Earlier releases are retained as drafts, with their assets preserved. Each release includes portable ZIP, MSI, source ZIP and all three SHA-256 files. [Publishing policy and command](docs/en/RELEASES.md).

## GitHub

The Checkpoint Attribution License 1.0, source, docs and build workflow are included. Set repository variable `CHECKPOINT_SERVICE_URL` for a hosted Steam service. The workflow uploads artifacts but does not publish a release automatically. Never upload `.tools`, `.qa`, `dist`, `.env`, databases, tokens or private keys.

`scripts/Export-Source.ps1` creates `dist/Checkpoint-source-0.8.10.zip` using ripgrep and `.gitignore`. Versions come from `Directory.Build.props`. Built-in translations live in `src/Checkpoint.Core/Localization/en.json`, XAML keys in `src/Checkpoint.App/LocalizationKeys.json`. User content and API wire values are not translated.

See [roadmap](docs/en/ROADMAP.md) and [changelog](CHANGELOG.md). Notifications, monthly statistics and private-library cloud sync are future work.

Checkpoint is independent of Valve. Games, artwork and trademarks belong to their owners. [License](LICENSE) · [Third-party notices](THIRD-PARTY-NOTICES.md) · [Privacy](docs/en/PRIVACY.md).

Checkpoint notices, menus and windows follow the language selected in Settings. External technical errors appear as translated explanations. Game titles, notes, tasks and player names keep their original content. System dialogs, including file pickers and SmartScreen, follow the Windows language.

Your complete friend code appears and copies from Friends → Account: `checkpoint-` followed by 12 characters. Use Checkpoint 0.7.1 or later to enter this format; codes copied from earlier versions still identify the same account.
