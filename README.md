# Checkpoint

**English** · [Español](README.es.md)

One game at a time. A native, translucent Windows desktop widget for the games you want to finish, with cover art, goals and progress shared with Checkpoint friends.

> **IMPORTANT — SMARTSCREEN:** GitHub EXE/MSI downloads have no digital signature or publisher certificate. Their unsigned status and lack of reputation can trigger “Windows protected your PC”. **I have no certificate because it costs money; I will not buy one until Checkpoint income covers at least its cost. Until then, these downloads will remain without a certificate.** Signing does not guarantee that warnings immediately disappear. [Read the notice and requirements](docs/en/DOWNLOAD-NOTES.md).

![Checkpoint cover grid](docs/screenshots/widget-grid-wide-dark-en.png)

## Screenshots

Captures use real WPF controls rendered during native tests. Games, users and friend progress are examples; the private cover fixture is a solid-color image. See the [gallery](docs/en/SCREENSHOTS.md) for both languages.

| Account | Friends' progress |
| --- | --- |
| ![Username and password](docs/screenshots/widget-friends-login-en.png) | ![Example friend progress](docs/screenshots/widget-friends-progress-en.png) |

## Download and run

Current version: **0.6.13**. Build outputs in `dist`:

- `Checkpoint-0.6.13-win-x64.zip`: portable edition. Extract the ZIP and open `Checkpoint/Checkpoint.exe`. All files stay inside the `Checkpoint` folder.
- `Checkpoint-0.6.13-win-x64.msi`: per-user installer with Start menu shortcut and Windows uninstall support.
- `.sha256` files: integrity checksums.

Download the portable ZIP or MSI from [GitHub Releases](https://github.com/Nicolasmf05/Checkpoint/releases/tag/v0.6.13), or build from source below. The **Build and verify** GitHub Actions workflow also produces artifacts after a successful run. Packages include the .NET runtime; end users need no development tools. Windows 11 x64 is the target. Windows 10 and ARM64 hardware have not been validated; ARM64 packaging is supported. The MSI and EXE are unsigned; Windows may warn or block them. See [Windows security messages](docs/en/WINDOWS-SECURITY.md). No private certificates are included.


Microsoft Store packaging is being prepared separately: [MSIX build and submission guide](docs/en/MICROSOFT-STORE.md). The unsigned MSIX preview is for developer validation and does not remove warnings from the current GitHub downloads.

## Requirements to use it

Windows x64 (Windows 11 tested), a user-writable folder and disk space. **No .NET/development-tool installation, administrator privileges, account or Internet connection is needed for the manual library.** Steam, online covers and friends are optional. Minimum RAM/CPU on older hardware is not yet measured. [Requirements and resource usage](docs/en/DOWNLOAD-NOTES.md).

## Features

- **Quick state changes in Miniature:** right-click a game to select its state. The current state is checked; changes persist without leaving the names/states-only list.
- **Miniature window actions:** right-click the background or a game to toggle Always on top or Lock position and size. Checked values reflect saved settings.
- **Search from any view:** Ctrl+F opens collection search and selects its current query. From Miniature it returns to the normal list, preserving the small size for next time; from Friends it returns to the collection. Miniature menus also offer Search games.
- **Edit from Miniature:** right-click a game → Edit game, or F2 on the selected game. Opens its normal editor without leaving Miniature; closing returns focus if it remains in My list.
- **Add from Miniature:** right-click the background or a game → Add game, including when My list is empty. Opens the normal editor without leaving Miniature; Ctrl+N remains available.
- **Page navigation in Miniature:** PageUp/PageDown move by a visible page, adapting to window height and keeping the active row focused.
- **Miniature text size:** Settings → Miniature text size, 12–20 (default 12). Names, states and row height grow together; other views keep their text size.
- **Miniature keyboard controls:** Up/Down select a game, Home/End jump to the first/last, Enter/Space open its state menu. A focus outline marks the active row; focus returns after closing the menu or changing state.
- **Miniature view:** only game names and states in a small translucent window. Settings → Collection view → Miniature, or cycle with F6. Right-click → Exit miniature view. Resize it independently from the normal widget; drag the top edge. Shows My list with no filters or cover downloads.
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
- Checkpoint accounts, requests, accepted friendships, blocks and explicitly selected publications through Supabase.

**Story completion and all achievements are independent.** Sync never decides you finished a story and preserves previous progress if Steam fails.

Miniature retains selection/focus during updates; changing view or showing the widget keeps it within the current screen work area.

## Friends

Open Friends and create an account: username of 3–24 ASCII letters, digits or underscores, password of at least eight characters. Usernames are case-insensitive. No real email or confirmation is requested. Keep your password safe: recovery is unavailable.

Copy your code from Account. Another user sends a request with that code. After acceptance, each can view games the other selects in Sharing: title, platform, cover, state, goal and counters. Notes, task names and individual achievement details stay private.

Consent and pending changes persist per account. Offline changes retry when connected; a withdrawal takes effect after server acknowledgment, so the old publication can remain visible until then. Signing out does not withdraw games. Cross-device conflicts require an explicit choice to publish your local version. Friends refresh every 60 seconds while the tab is open. The private library stays local; full cloud restore is unavailable.

Public Supabase configuration is included. Friends use Checkpoint accounts independently of Steam. See [setup](docs/en/SUPABASE.md), [friends scope](docs/en/FRIENDS-PLAN.md) and [privacy](docs/en/PRIVACY.md).

## Controls and data

`Ctrl+Alt+C` show/hide; `Ctrl+N` add; `Ctrl+F` search; `F6` cycle views; `Ctrl+Z` restore last deletion when not editing text; `Escape` hide. Use the tray if the global shortcut is occupied.

Drag `⠿` above/below another game or focus the handle and press `Alt+↑` / `Alt+↓`. Favorites stay above other games; reorder within each group. Filtered-out games retain relative order. Grid columns adapt to width.

Data lives in `%LOCALAPPDATA%\Checkpoint`. Uninstalling preserves the library. Steam/Checkpoint tokens use Windows DPAPI for the current user and are excluded from backups.

## Backups and recovery

Settings → Backups → Export creates a `.checkpoint` archive of the current collection and custom covers. Import adds games/images, preserving existing games matched by local ID or Steam ID. Steam covers download again online. Legacy JSON omits images.

Limits: 10,000 games, 8 MB/image, 25 MB collection data, 200 MB extracted. PNG headers are validated before decoding (4,096 pixels/side, 12 million pixels). References are never extraction paths. Failures preserve existing data.

The ↶ button and Settings → View deleted games restore the last 20 deletions with notes, tasks, progress, favorites, order and covers. Existing library data wins on conflicts. Backups exclude deletion history, sessions and sharing consent.

Version 0.3 introduced SQLite schema 2. Version 0.5 opens older libraries; use 0.3+ after migration. Legacy JSON remains compatible; complete backups require 0.3+.

## Steam service

The local library works without Steam. The Supabase Steam service is deployed and configured. The distribution includes its public endpoint and the Checkpoint public project configuration; end users do not need API keys or development tools. Press Link Steam and finish sign-in on Steam; imports appear in Library. Friends remain independent Checkpoint accounts. See [Steam setup](docs/en/STEAM-SERVICE.md). Open the app, select Link Steam and finish sign-in on the official Steam page. Game details must be visible to import the library and achievements.

Keep the Steam API key in a server environment secret, never the app or GitHub. Users sign in on Steam through OpenID; private game details remain inaccessible. See [Steam setup and limits](docs/en/STEAM-SERVICE.md). Advanced connection supports a local development server.

## Build and test

Requirements: Windows, PowerShell 7, .NET SDK 10, Node.js 22+. Scripts use `.tools/dotnet` if available; that directory is not published. The icon is included (`scripts/New-Icon.ps1` regenerates it).

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
# Always use a new isolated data folder:
dotnet run --project src/Checkpoint.App -c Release --no-build -- --data-dir "$PWD/.qa/example" --demo --diagnostics --smoke-test "$PWD/.qa/render"
```

Native tests reject existing libraries/sessions/publications and exercise real controls with simulated Steam/social responses. Demo adds examples only to an empty library. Build extracts and tests its self-contained ZIP, including language switching and virtualized rendering of 1,003 games. Reports and captures: `.qa/package-…/render`.

Repeat using `./scripts/Verify-Package.ps1 -ZipPath ./dist/Checkpoint-0.6.13-win-x64.zip`. ARM64 execution needs Windows ARM64 hardware. [Validation](docs/en/VALIDATION.md) lists remaining real-account/Storage and clean-machine MSI checks.

## GitHub

MIT license, source, docs and build workflow are included. Set repository variable `CHECKPOINT_SERVICE_URL` for a hosted Steam service. The workflow uploads artifacts but does not publish a release automatically. Never upload `.tools`, `.qa`, `dist`, `.env`, databases, tokens or private keys.

`scripts/Export-Source.ps1` creates `dist/Checkpoint-source-0.6.13.zip` using ripgrep and `.gitignore`. Versions come from `Directory.Build.props`. Built-in translations live in `src/Checkpoint.Core/Localization/en.json`, XAML keys in `src/Checkpoint.App/LocalizationKeys.json`. User content and API wire values are not translated.

See [roadmap](docs/en/ROADMAP.md) and [changelog](CHANGELOG.md). Notifications, monthly statistics and private-library cloud sync are future work.

Checkpoint is independent of Valve. Games, artwork and trademarks belong to their owners. [License](LICENSE) · [Third-party notices](THIRD-PARTY-NOTICES.md) · [Privacy](docs/en/PRIVACY.md).
