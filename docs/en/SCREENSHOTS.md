# Checkpoint screenshots

**English** · [Español](../SCREENSHOTS.md)

Current captures use actual WebView2 HTML/CSS through `CapturePreviewAsync` in isolated package tests. Older WPF captures below are archived examples. Games/users/progress are fixtures, HTTP replies are simulated, and private cover artwork uses a solid-color image. This is not real-account signup evidence.

## 0.7.4 — eight themes

Live palettes from the actual packaged application. Settings → Theme previews immediately; Save keeps the choice and Cancel restores the previous one.

| Dark | Light |
| --- | --- |
| ![Dark](../screenshots/css-theme-dark-en.png) | ![Light](../screenshots/css-theme-light-en.png) |

| Midnight | Ocean |
| --- | --- |
| ![Midnight](../screenshots/css-theme-midnight-en.png) | ![Ocean](../screenshots/css-theme-ocean-en.png) |

| Forest | Plum |
| --- | --- |
| ![Forest](../screenshots/css-theme-forest-en.png) | ![Plum](../screenshots/css-theme-plum-en.png) |

| Amber | High contrast |
| --- | --- |
| ![Amber](../screenshots/css-theme-amber-en.png) | ![High contrast](../screenshots/css-theme-contrast-en.png) |

![Theme selector](../screenshots/css-theme-settings-en.png)

## 0.7.3 — visible keyboard shortcuts

The bottom strip shows useful key gestures; F1 opens the complete guide. Miniature offers F1 and a context-menu entry.

![English keyboard shortcuts guide](../screenshots/css-shortcuts-en.png)

## 0.7.1 — full window

![Full window, 100% opacity](../screenshots/css-full-window-en.png)

## 0.7.0 — current CSS interface

![English widget](../screenshots/css-widget-en.png)

| Friends login | Miniature |
| --- | --- |
| ![Friends](../screenshots/css-friends-login-en.png) | ![Miniature](../screenshots/css-miniature-en.png) |

![Light compact view](../screenshots/css-compact-light-en.png)

![Settings](../screenshots/css-settings-en.png)

![English app notice](../screenshots/css-notice-en.png)

Spanish captures: [widget](../screenshots/css-widget-es.png), [editor](../screenshots/css-editor-es.png), [Miniature](../screenshots/css-miniature-es.png).

## Archived WPF interface (0.6.x)

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

## Miniature — 0.6.4

Only name and state. Enable from Settings or F6; right-click to exit.

![Miniature](../screenshots/widget-miniature-en.png)

## Miniature game menu — 0.6.11

The menu includes states, editing, search, adding and window settings; rows still show only name and state.

![State menu](../screenshots/miniature-state-menu-en.png)

## Keyboard in Miniature — 0.6.6

The outline marks the focused row. Up/Down and Home/End select games; Enter/Space open the menu. Capture uses Spanish.

![Keyboard focus](../screenshots/widget-miniature-keyboard.png)


## Add from empty Miniature — 0.6.11

Right-click the background → Add game, including with no rows. This native capture uses Spanish.

![Empty Miniature menu, Spanish capture](../screenshots/miniature-empty-menu.png)


## Page navigation — 0.6.12

PageUp/PageDown adapt the jump to Miniature height. Native capture uses Spanish and a large sample collection.

![Miniature page navigation, Spanish capture](../screenshots/widget-miniature-pages.png)


## Larger text — 0.6.13

Miniature with text size 18 and adapted rows. This native capture uses Spanish.

![Miniature with larger text, Spanish capture](../screenshots/widget-miniature-large-text.png)

## Notices in English

![Notice with English text and button](../screenshots/dialog-notice-en.png)

## Complete friend code

![Account with the complete Checkpoint friend code](../screenshots/widget-friends-account-en.png)
