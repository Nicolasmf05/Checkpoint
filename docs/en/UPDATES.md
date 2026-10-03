# Windows updates

Starting in **0.8.1**, open **Settings → Updates**. Checkpoint checks public `Nicolasmf05/Checkpoint` releases on launch, at most once every 24 hours. Disable automatic checks or choose **Check for updates** at any time. Published previews with numeric tags are included because Checkpoint currently distributes pre-1.0 releases as previews. No GitHub account/token is needed, and your library, sessions and Steam data are not sent.

A newer compatible release enables **Download and install**. Download size and MSI SHA-256 must match both the `.sha256` file and GitHub's digest. Only official repository URLs and HTTPS GitHub asset-server redirects are accepted. Incomplete/tampered downloads cannot be installed. The helper checks the hash again before running the installer.

Before installation, Checkpoint exports a `.checkpoint` backup into `updates` under your data folder. It closes the app, waits for it to exit, runs the per-user MSI and reopens the app with the same data folder. The library/settings are preserved. Failure/cancellation attempts to reopen the previous app; you can also open it manually. Installing requires clicking the button: opening the app does not silently download/install anything.

**Portable:** this updater installs the newer app in `%LOCALAPPDATA%\Programs\Checkpoint`, with a Start menu shortcut. **It does not replace the original portable ZIP/executable.** Use the Start menu app afterwards; the old portable remains an older version. The original data folder, including a custom `--data-dir`, is passed when restarting.

Windows installer status and a local result determine success. Code 3010 means Windows may need a reboot; the app never automatically restarts the PC. Installers remain unsigned: SHA-256 checks integrity, but does not replace a certificate or remove SmartScreen warnings. Windows security configuration stays unchanged.

Downloads/backups use disk space. Old MSI files and backups inside `updates` can be removed manually after confirming the new app works. Do not delete `checkpoint.db`. Encrypted sessions stay local and are excluded from backups. The result appears on reopening; if the helper could not launch/restart the app, open Checkpoint from the Start menu.

GitHub rate limits and Internet access apply; failure does not prevent using the installed app. The updater uses Windows PowerShell included with Windows; managed policies blocking that process/MSI require manual installation from [Releases](https://github.com/Nicolasmf05/Checkpoint/releases). It never changes PowerShell policies or disables SmartScreen. The web receives GitHub Pages deployments on reload and does not use MSI updates.

Tests cover version/architecture selection, origins, hashes, simulated downloads and a helper rejecting a wrong hash before installation. Tests do not perform a real MSI upgrade of the user's personal installation.

Downloads preserve the Windows Internet zone marker. The download folder must support that metadata (normal Windows NTFS); otherwise install through the browser. Start menu and enabled Windows startup shortcuts also retain a custom data directory.
