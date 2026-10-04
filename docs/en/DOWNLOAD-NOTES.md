# Checkpoint: read this before running

## IMPORTANT: WINDOWS SMARTSCREEN

**The GitHub EXE and MSI are NOT digitally signed and have no publisher certificate. Because of this and their insufficient established reputation, Windows SmartScreen may display “Windows protected your PC” or “Unknown publisher”.**

**The developer has no code-signing certificate because obtaining one for these downloads costs money. A certificate will not be purchased until project income covers at least its cost. Until then, GitHub downloads will remain without that certificate.**

SmartScreen evaluates reputation: even a valid signature does not guarantee the warning immediately disappears. This explanation concerns an unrecognized app/publisher warning; a named virus detection is a different case that needs investigation. Do not disable Defender or add exclusions to run the app.

Microsoft Store MSIX is an alternative: Microsoft signs approved apps without buying your own certificate. Preparation is partly implemented, but the app is not yet published in Store.

## Requirements to use it

- A Windows x64 PC. Windows 11 is the tested platform. Windows 10 is unvalidated; Windows 7/8/8.1 are unsupported. ARM64 requires its own package and hardware validation.
- **Microsoft Edge WebView2 Evergreen Runtime is required from version 0.7.0.** Windows 11 includes it; if missing, [download it from Microsoft](https://developer.microsoft.com/microsoft-edge/webview2/). It is shared and updated independently. The ZIP/MSI includes .NET but does not bundle or silently install WebView2. Offline use requires the runtime to be present first.
- A user-writable folder and disk space for the app, library, covers and backups.
- **No .NET, Node.js, Python, Visual Studio, Supabase installation or server is needed. The runtime is included. No administrator privileges are requested.**
- **No account or Internet connection is needed to organize games manually, use local covers or import/export backups.** Internet is only needed for cover downloads and Steam/friends features; Steam and Checkpoint accounts are separate and optional. Steam sign-in requires a browser.

Portable: extract the ZIP and run `Checkpoint.exe` inside `Checkpoint`. Keep the files together. MSI installs for the current user. Both editions include this notice and its Spanish translation.

## Window modes

Choose Full window, Small window or Miniature in the header or Settings → Window mode. Full window fills the work area (taskbar stays available) at 100% opacity. Returning to Small window restores its saved size, position and opacity. Miniature keeps only names/states and its own dimensions; its right-click menu offers all three modes. Mode is saved. The opacity slider configures Small window/Miniature; it never makes Full window translucent.

## Miniature view

Choose **Settings → Collection view → Miniature**, or cycle with F6. Shows only **game name and state** for My list, without covers, row buttons or progress bars. Header, navigation, filters and footer are hidden. The window can shrink to 240 × 90 Windows logical units. Drag the top edge and resize from the bottom-right corner. **F6 or right-click → Exit miniature view** returns to the normal view. Both sizes are saved independently; progress/files are unchanged. No covers are loaded while active.

In Miniature, **right-click a game** to select pending, playing, paused, story finished or abandoned. The menu checks the current state and also offers Exit miniature view and Settings. Changes persist and follow the normal progress/sharing rules: tracked games are visible to friends unless marked private. The list still shows only name and state.

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

There is no measured minimum RAM/CPU on older hardware. No arbitrary minimum is promised: usage depends on library size, window size and images. The CSS interface uses WebView2 browser processes in addition to .NET. Lower RAM usage than the previous WPF interface is not claimed. Covers and backups can increase disk use; users control their data.

Source, documentation and downloads: [Nicolasmf05/Checkpoint](https://github.com/Nicolasmf05/Checkpoint). See [Windows security](https://github.com/Nicolasmf05/Checkpoint/blob/main/docs/en/WINDOWS-SECURITY.md) and [Microsoft SmartScreen reputation guidance](https://learn.microsoft.com/windows/apps/package-and-deploy/smartscreen-reputation).

**Consistent language:** notices and their buttons follow the language selected in Settings, including Miniature and editing windows. Windows system dialogs follow the operating system language.

**Complete friend code:** Friends → Account displays and copies `checkpoint-` followed by 12 characters. Enter this format in Checkpoint 0.6.16 or later; earlier codes still work.

## Achievements and detection

Detection works only in Windows while Checkpoint is open. Steam uses its existing connection; optional RetroAchievements requires a username, personal Web API key in Settings and a game ID. The key is encrypted locally. Manual achievements work offline and never unlock official achievements. [Guide](ACHIEVEMENTS.md).

## Creator and license

Checkpoint originally created by **Nicolasmf05** — https://github.com/Nicolasmf05/Checkpoint. New distributions from the license-change revision use Checkpoint Attribution License 1.0; retain the accompanying LICENSE and creator credit when redistributing. Original v0.8.5 and earlier packages retain their included MIT license.

## Uninstall

MSI: close Checkpoint, then use Start → Checkpoint → Uninstall Checkpoint, or the uninstall shortcut inside the installed folder. The shortcut follows the Windows language (Spanish or English) and opens the Windows Installer confirmation. Windows Settings → Apps → Checkpoint → Uninstall also works. The app and installed shortcuts are removed; library and settings in `%LOCALAPPDATA%\Checkpoint` remain.

Portable: disable Start with Windows if enabled, close Checkpoint and remove its extracted folder. The portable ZIP does not install MSI shortcuts.

## Empty Miniature lists

When the default My list has no visible games, Miniature uses Library automatically and labels its source **Library**. Imported games therefore remain visible even before you add them to My list. This does not track, publish or change the privacy of any game. Empty custom lists stay empty with a localized explanation and a Library button that exits Miniature into the normal Library view.

## Background achievement review

Settings → Review all achievements → Update all achievements returns to the collection and continues in the background. The progress indicator has Stop review; completed results remain saved. Up to three queries run at once, with starts paced to respect Steam limits. Windows updates only the changed game in SQLite. You can browse and edit your games during the review. Closing the Windows app or reloading/closing the browser stops the remaining work. The browser reviews Steam; Windows also reviews linked RetroAchievements games.

## Navigation in one window

Checkpoint uses the same window or browser tab for settings, game/list sheets, achievements, reviews and notices. Back returns one level; Back to collection returns to your current collection. Opening a related page preserves the parent form. Leaving an unsaved form cancels its pending changes; use Save to keep them. Miniature expands temporarily to make full sheets readable and restores its size on return. Native file pickers and Steam authorization remain system/external interfaces.


## Minimizing on Windows

The − button minimizes to the taskbar by default. Settings → Hide in the system tray when minimized enables hiding instead. Restore it by double-clicking the tray icon or using your configured global shortcut. Hide in the system tray when closed is independent; the X still exits completely by default. Escape and the explicit show/hide actions keep their configured purpose. Browser windows use the browser’s own minimize controls.
