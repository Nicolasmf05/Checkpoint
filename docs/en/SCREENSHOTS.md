# Checkpoint screenshots

**English** · [Español](../SCREENSHOTS.md)

Captured from actual WPF controls through `RenderTargetBitmap` during isolated native package tests. Games/users/progress are fixtures, HTTP replies are simulated, and private cover artwork uses a solid-color image. This is not real-account signup evidence.

## English

![Cover grid](../screenshots/widget-grid-wide-dark-en.png)

| Account | Friend progress |
| --- | --- |
| ![Account form](../screenshots/widget-friends-login-en.png) | ![Friend progress](../screenshots/widget-friends-progress-en.png) |

![English game editor](../screenshots/dialog-editor-en.png)

![English settings](../screenshots/dialog-settings-en.png)

## Spanish

![Cover grid in Spanish](../screenshots/widget-grid-wide-dark.png)

| List | Compact |
| --- | --- |
| ![Dark list](../screenshots/widget-dark.png) | ![Light compact view](../screenshots/widget-compact-light.png) |

| Account | Friends |
| --- | --- |
| ![Spanish account form](../screenshots/widget-friends-login.png) | ![Spanish friend progress](../screenshots/widget-friends-progress.png) |

![Spanish editor](../screenshots/dialog-editor.png)

![Spanish settings](../screenshots/dialog-settings.png)

Regenerate with `./scripts/Build.ps1 -Installer` on Windows. Output: `.qa/package-…/render`. Publish selected PNGs only, excluding libraries, sessions and local logs. See [validation](VALIDATION.md).

## Lightweight mode — 0.6.3

Covers are hidden while games and progress remain. This test capture uses Spanish; the setting is also available in English.

![Lightweight mode, Spanish capture](../screenshots/widget-lightweight.png)
