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

In Miniature, **right-click a game** to select pending, playing, paused, story finished or abandoned. The menu checks the current state and also offers Exit miniature view and Settings. Changes persist and follow the normal progress/sharing rules: changing a state does not share a new game. The list still shows only name and state.

Miniature also supports keyboard controls: **Up/Down** select games, **Home/End** jump to the first/last and **Enter/Space** open the active game menu. A focus outline marks the active row. Closing the menu or changing state returns focus to that game. Other views retain their existing shortcuts.

Updates preserve the selected game and restore its keyboard focus only if the list had focus. Removing it from My list clears selection. Changing view or showing the widget keeps the window within the current monitor work area.

**Right-click the background or a game → Always on top / Lock position and size.** Checked options reflect your saved settings; changes apply immediately and persist. Unlock from the same menu to move or resize again. The drag strip uses a normal cursor while locked. The list keeps only names and states.

**Ctrl+F or right-click → Search games** opens the normal collection view and focuses search. It saves that view while preserving Miniature dimensions for your next visit. From Friends, Ctrl+F also returns to the visible collection search. Any existing query is selected so you can replace it by typing; its text is preserved until you change it. Miniature itself keeps the names/states-only list.

**Right-click a game → Edit game, or F2 on the selected game** opens its normal editor while keeping Miniature active. Edit notes, tasks and other normal editor fields. Save applies changes; Cancel preserves the saved data. Closing restores game focus if it remains visible. Unchecking Show in My list removes its Miniature row without deleting the library entry. Rows still show only name and state.

**Right-click the background or a game → Add game**, also available with an empty list. The normal editor opens while Miniature stays active. Save creates the game; Cancel leaves no entry. Games marked Show in My list appear as name/state rows. Ctrl+N remains available. No extra empty-state controls are added.

**PageUp/PageDown** navigate Miniature by a visible page, based on the current viewport and row height. Resizing the window adjusts the jump. Navigation stops at the first/last game and keeps the active row focused; large lists stay virtualized. Updates without list focus retain the existing reading offset. Search closes the menu before focusing the normal search field.

**Settings → Miniature text size** offers values from 12 to 20, default 12. Save applies and stores the choice; Cancel preserves the saved value. Names, states and row height scale together. PageUp/PageDown adapt to the larger rows. Other views retain their text size. Widen Miniature if long names are cut off; their tooltip keeps the full title. No additional installation is needed.

**Right-click → Exit miniature view** returns to the list, compact or cover-grid layout used before entering Miniature. It saves the restored view and uses its independently saved normal size, limited by available screen space. Saving Settings while Miniature is active preserves the remembered layout. **F6** keeps the list → compact → grid → Miniature → list cycle; **Search games / Ctrl+F** opens the normal list with its search field.

## Optional lightweight mode

Enable **Settings → Lightweight mode (no covers)**. Collection and friend covers are hidden, new image downloads are skipped and the decoded image cache is cleared. Games, goals, progress, saved cover files and publications are preserved; you can turn it off at any time. The choice is saved. An already-started download may finish. Steam, synchronization and pending publication delivery remain enabled.

## Resource usage and limits

Lists are virtualized; recycled cover cards release image references and the decoded image cache has an estimated 8 MiB budget with at most 32 entries. That budget is not the application's total memory. Unused framework language resources are omitted. Friend progress does not refresh periodically while the widget is hidden or minimized; pending publications can still be delivered.

There is no measured minimum RAM/CPU on older hardware. No arbitrary minimum is promised: usage depends on library size, window size and images. Covers and backups can increase disk use; users control their data.

Source, documentation and downloads: [Nicolasmf05/Checkpoint](https://github.com/Nicolasmf05/Checkpoint). See [Windows security](https://github.com/Nicolasmf05/Checkpoint/blob/main/docs/en/WINDOWS-SECURITY.md) and [Microsoft SmartScreen reputation guidance](https://learn.microsoft.com/windows/apps/package-and-deploy/smartscreen-reputation).

**Consistent language:** notices and their buttons follow the language selected in Settings, including Miniature and editing windows. Windows system dialogs follow the operating system language.
