# Checkpoint in your browser

**English** · [Español](../WEB.md)

[**Open the web app**](https://nicolasmf05.github.io/Checkpoint/). No Checkpoint or .NET installation is required. Use a modern browser with JavaScript and IndexedDB enabled.

Create and edit games, state and manual story progress, private notes, tasks, favorites, local covers, list/compact/grid layouts, eight themes, Spanish or English and [configurable shortcuts](SHORTCUTS.md). The interface automatically fills the available browser viewport and adapts when resized; no full/small/Miniature window presets are offered. Layout selection only changes game presentation.

Link Steam in Settings using the official Steam login page. Library and achievements refresh on launch, reconnection and every 15/30/60/120 minutes while the tab is visible. Relevant Steam data must be public. Family games returned through licenses or recent activity are included; the entire family catalog is not guaranteed. Steam never completes the story or overwrites manual state and notes.

Use **Sign in** at the top of the page with the same Checkpoint username and password as Windows. The button opens sign-in for an existing account. Choose **Create account** to register; **I already have an account** returns to sign-in. Once signed in, **My account** shows your friend code and **Sign out**.

Friends uses your Checkpoint username/password account, full checkpoint-… codes, requests and progress published from Checkpoint. Tracked games are visible to accepted friends by default; private games and untracked imports are excluded. [Lists and privacy](LISTS.md). Title, platform, state and progress totals are published; notes, task titles and custom covers remain private. Signing out does not withdraw publications: use Stop sharing. Cross-device conflicts require review before publishing again.

## Data and backups

Your private library, settings and pending publications live in this browser and origin. There is no automatic private library synchronization between Windows and the web or between browsers. Clearing site data deletes local games. **Export JSON backups regularly.** Import supports Checkpoint JSON and skips duplicates; desktop .checkpoint ZIP packages are not supported. JSON backups may contain private notes and tasks. Web embedded covers are included; packaged Windows covers must be selected again.

Steam and Checkpoint sessions use this tab's sessionStorage; snapshots and backups exclude tokens. Closing the tab may require signing in again. The library works without an account. After a completed first visit, public app files are cached for offline opening; Steam and friends need a connection. There is no app telemetry. GitHub serves the page and Supabase handles authenticated requests. [Privacy](PRIVACY.md).

The web has no tray, Windows startup, global shortcuts, desktop window transparency or native Steam executable launch. Use the Windows download for these features.

## Development and deployment

```powershell
node scripts/build-web.mjs
node --test tests/web/*.test.mjs web/test/*.test.mjs supabase/tests/steam.test.mjs
npm --prefix web ci --ignore-scripts
cd web
npx playwright install chromium
cd ..
node web/test/browser.mjs
```

GitHub Actions builds only allowlisted static files in dist/web and deploys them through Pages. Steam function CORS permits https://nicolasmf05.github.io while session and privacy checks remain active. The Supabase publishable key is intentionally public; the private Steam key remains in server secrets. Never publish private keys or session tokens.

Browser tests use simulated services and fictional accounts. Production CORS is also checked, including the unauthenticated library's 401 response. Specific borrowed games depend on what Steam returns for the linked account.
