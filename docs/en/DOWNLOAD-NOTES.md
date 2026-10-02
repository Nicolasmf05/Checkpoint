# Checkpoint: read this before running

## IMPORTANT: WINDOWS SMARTSCREEN

**The GitHub EXE and MSI are NOT digitally signed and have no publisher certificate. Because of this and their insufficient established reputation, Windows SmartScreen may display “Windows protected your PC” or “Unknown publisher”.**

**The developer has no code-signing certificate because obtaining one for these downloads costs money. A certificate will not be purchased until project income covers at least its cost. Until then, GitHub downloads will remain without that certificate.**

SmartScreen evaluates reputation: even a valid signature does not guarantee the warning immediately disappears. This explanation concerns an unrecognized app/publisher warning; a named virus detection is a different case that needs investigation. Do not disable Defender or add exclusions to run the app.

Microsoft Store MSIX is an alternative: Microsoft signs approved apps without buying your own certificate. Preparation is partly implemented, but the app is not yet published in Store.

## Requirements to use it

- A Windows x64 PC. Windows 11 is the tested platform. Windows 10 is unvalidated; Windows 7/8/8.1 are unsupported. ARM64 requires its own package and hardware validation.
- A user-writable folder and disk space for the app, library, covers and backups.
- **No .NET, Node.js, Python, Visual Studio, Supabase installation or server is needed. The runtime is included. No administrator privileges are requested.**
- **No account or Internet connection is needed to organize games manually, use local covers or import/export backups.** Internet is only needed for cover downloads and Steam/friends features; Steam and Checkpoint accounts are separate and optional. Steam sign-in requires a browser.

Portable: extract the ZIP and run `Checkpoint.exe` inside `Checkpoint`. Keep the files together. MSI installs for the current user. Both editions include this notice and its Spanish translation.

## Miniature view

Choose **Settings → Collection view → Miniature**, or cycle with F6. Shows only **game name and state** for My list, without covers, row buttons or progress bars. Header, navigation, filters and footer are hidden. The window can shrink to 240 × 90 Windows logical units. Drag the top edge and resize from the bottom-right corner. **F6 or right-click → Exit miniature view** returns to the normal view. Both sizes are saved independently; progress/files are unchanged. No covers are loaded while active.

## Optional lightweight mode

Enable **Settings → Lightweight mode (no covers)**. Collection and friend covers are hidden, new image downloads are skipped and the decoded image cache is cleared. Games, goals, progress, saved cover files and publications are preserved; you can turn it off at any time. The choice is saved. An already-started download may finish. Steam, synchronization and pending publication delivery remain enabled.

## Resource usage and limits

Lists are virtualized; recycled cover cards release image references and the decoded image cache has an estimated 8 MiB budget with at most 32 entries. That budget is not the application's total memory. Unused framework language resources are omitted. Friend progress does not refresh periodically while the widget is hidden or minimized; pending publications can still be delivered.

There is no measured minimum RAM/CPU on older hardware. No arbitrary minimum is promised: usage depends on library size, window size and images. Covers and backups can increase disk use; users control their data.

Source, documentation and downloads: [Nicolasmf05/Checkpoint](https://github.com/Nicolasmf05/Checkpoint). See [Windows security](https://github.com/Nicolasmf05/Checkpoint/blob/main/docs/en/WINDOWS-SECURITY.md) and [Microsoft SmartScreen reputation guidance](https://learn.microsoft.com/windows/apps/package-and-deploy/smartscreen-reputation).
