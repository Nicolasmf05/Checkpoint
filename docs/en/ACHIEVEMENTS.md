# Game detection and achievements

Checkpoint for Windows checks running processes every 10 seconds while open, including in the tray. Detecting a saved library game opens an independent pending-achievement window without taking focus. No administrator privileges are required. Closing it suppresses reopening for that running session; launching the game again can open a new window. Disable this in **Settings → Detect games and open achievements automatically**.

## Direct access and descriptions

On any game card, select **View achievements** or **Achievements · completed / total**. The action is available in list, compact and cover-grid layouts in Windows and the browser. Miniature keeps its name/status-only list.

The panel emphasizes the completion count, progress bar and pending achievements. Choose **Show description** on a card to read how to earn the achievement; **Hide description** collapses it again. Long descriptions wrap to the available width. Missing provider descriptions are indicated explicitly.

Secret achievements conceal both names and descriptions until you enable the reveal option. Revealing names does not automatically expand descriptions. API-provided achievement titles and descriptions retain the provider's language.

Windows card/panel counters reflect visible Checkpoint goals (Steam, RetroAchievements and personal goals), including local completion overrides. The browser displays official Steam progress. Story/task progress remains separate, and friend publications retain official progress.

## Steam

Connect Steam in Settings and save a game with its Steam ID. Checkpoint reads installed Steam library folders and recognizes executables inside that game's folder. If those folders are inaccessible or the executable is elsewhere, set **Executable to detect** in its editor. Steam Family games may require manual addition when absent from the API. Your Steam game details and achievements must be accessible.

The window queries on opening and every 60 seconds while open. The Steam service caches user progress for up to 15 minutes, so official unlocks may appear later. Connection failures preserve previous data.

## RetroAchievements

1. Open **Settings → Configure RetroAchievements**, enter your username and your personal **Web API key** from your RetroAchievements account settings. Do not use your password. The key is encrypted with Windows DPAPI for your Windows user and excluded from backups, GitHub and friend publications.
2. In the game editor, enter its **RetroAchievements game ID**, the number on its game page. Steam IDs are different.
3. Enter the executable, such as `retroarch.exe`, and game-specific window title text. The emulator executable alone cannot distinguish its different games. If it exposes no useful title, open achievements manually from the editor.
4. Optionally enable **Count Hardcore achievements only**. Otherwise, completion in either mode counts.

Requests go directly to the [official RetroAchievements API](https://api-docs.retroachievements.org/v1/get-game-info-and-user-progress.html), at least 60 seconds apart per game. Checkpoint does not scan/hash ROM files or alter emulator settings. Official unlocks require the emulator's own compatible RetroAchievements connection. Provider achievement names/descriptions retain the provider's language; controls and notices use the language selected in Checkpoint.

## Your achievement list

- **Show pending only** starts enabled.
- **Add manual achievement** creates a personal name/description goal.
- **Completed in Checkpoint** marks or unmarks any goal. Manual choices survive synchronization.
- **Use API status** removes a manual override from an official achievement.
- **Remove from my list** hides an official achievement or deletes a manual goal. **Restore removed achievements** restores hidden official achievements.

Personal edits never unlock or remove Steam/RetroAchievements achievements. Existing Steam counters and friend publications retain official progress; personal goals, overrides and credentials are not published. Backups retain goals, overrides, hidden entries and game IDs, but exclude credentials.

## Browser and limitations

Process detection and this window belong to Windows. GitHub Pages cannot inspect PC programs. Web JSON import/export preserves these desktop fields; its achievement panel still displays Steam data. Detection covers saved games, rather than adding every open program. Protected processes or changed executable names/window titles may require an adjusted association.

RetroAchievements integration is tested with simulated HTTP responses and fixture keys. A real personal API key has not been tested. No new Supabase schema or secret deployment is required.
