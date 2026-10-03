# CSS interface

Checkpoint 0.7.0 renders its collection, Miniature, friends, forms and notices with local HTML/CSS/JavaScript in Microsoft Edge WebView2. No website is hosted and no React, npm build or remote interface is used.

## Where to change it

- `src/Checkpoint.App/Web/app.css`: colors, translucency, layouts, typography, responsive rules and forms.
- `Web/index.html`: local document and Content Security Policy.
- `Web/app.js`: HTML elements, virtualized rows, menus, keyboard, focus and explicit native commands.
- `Web/ui-model.mjs`: virtualization and navigation calculations; Node tests in `tests/web`.
- `WebSurface.cs`: transparent composition control, local document, message validation and form schema.
- `WebInterface.cs`: collection snapshots and commands using existing C# controllers.

Edit the files and run `./scripts/Build.ps1 -Installer`. CSS/JS is copied into the package's `Web` directory. Tests execute the actual packaged WebView2 application with fresh data through `Verify-WebInterface.ps1`. WebView2 Evergreen Runtime must be installed; `Ensure-WebViewRuntime.ps1` reports availability. Only GitHub Actions can explicitly request its automatic CI installation.

## Native responsibilities

C#/.NET retains SQLite storage, validation, Steam, Supabase, encrypted sessions, image decoding, backups, tray, shortcuts and the native window. Existing WPF form controllers remain hidden and emit a JSON presentation schema; their validation and button handlers are reused. XAML is therefore still present, but it does not style the production interface. Native file pickers follow Windows appearance. If WebView2 cannot initialize, a native bootstrap notice offers Microsoft's runtime download.

The browser accepts explicit commands rather than arbitrary native objects. Remote navigation, frames, downloads and permissions are blocked. CSP disallows network calls from JavaScript. Covers use an app-owned image origin and existing game IDs; arbitrary filesystem paths cannot be requested. Titles and notes use DOM text/value properties. Password fields are not serialized back to JavaScript snapshots; passwords are present in the field while typed and sent to the native login controller. Steam keys and saved session tokens stay native.

## Updating and resource use

The SQLite library and preferences keep their existing location and format. No account migration or server deployment is required. WebView2 stores its browser profile in `webview-profile` beside the library; it is excluded from backups and source releases.

WebView2 is an additional shared runtime dependency. The .NET runtime remains bundled; WebView2 is not bundled with GitHub ZIP/MSI downloads. Offline manual use works once it is installed. See [Microsoft's distribution guidance](https://learn.microsoft.com/microsoft-edge/webview2/concepts/distribution).

Visible rows are virtualized; unchanged collection snapshots are not repeatedly sent. Lightweight mode and Miniature avoid covers. Browser processes add memory overhead; this change does not promise lower RAM usage than WPF. Minimum hardware, physical pointer/keyboard use, DPI changes, screen readers and installed-package upgrades still need validation.
