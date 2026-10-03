# Microsoft Store preparation

**English** · [Español](../MICROSOFT-STORE.md)

Checkpoint now has a repeatable MSIX packaging path. It is not yet published or certified by Microsoft Store. GitHub EXE/MSI downloads remain unsigned.

## What is ready

- Windows 11 desktop manifest for the native app with a WPF shell and WebView2 CSS interface, including English/Spanish and `runFullTrust`.
- Store, Start menu and tile icons derived from the existing Checkpoint icon. Regenerate with `scripts/New-StoreAssets.ps1`.
- Self-contained .NET runtime, Steam/Supabase public configuration and licenses inside the package.
- MakeAppx schema/content validation, SHA-256 checksum and unpack verification of every application file, icon dimensions and native architecture.
- GitHub Actions builds an explicitly named **preview-unsigned** package. This artifact is for packaging validation, not end-user installation.

## Build a local packaging preview

Install the Windows SDK containing MakeAppx. Build the portable first, then:

```powershell
./scripts/Build.ps1
./scripts/Build-MSIX.ps1 -Preview
```

The preview uses `Checkpoint.PackagingPreview`, not a registered Store identity. The script neither signs nor installs the package and does not change Windows security settings. A successful build verifies packaging; it does not establish SmartScreen reputation or pass Store certification.

## Prepare the real Store submission

1. Register an individual developer account in Partner Center and complete identity verification. Microsoft currently offers free individual registration.
2. Reserve an available product name. Open Product management → Product identity and copy **Package/Identity/Name**, **Package/Identity/Publisher** and **Package/Properties/PublisherDisplayName** exactly. These are public identifiers, not passwords.
3. Build with those actual values and the reserved display name:

```powershell
./scripts/Build-MSIX.ps1 -IdentityName '<Package/Identity/Name>' `
    -Publisher '<Package/Identity/Publisher>' `
    -PublisherDisplayName '<Package/Properties/PublisherDisplayName>' `
    -DisplayName '<reserved product name>'
```

4. Test a package with the final identity on a separate development/test Windows account or machine. Run Windows App Certification Kit. Validate installation, Store update, uninstall, tray/global shortcut, Steam sign-in, account/friends, backups, startup and data behavior. Packaging checks alone do not test these.
5. Complete English/Spanish listings, suitable screenshots, support/privacy URLs, age rating, availability and certification notes. The existing screenshots contain explicitly labeled sample users/games. Screenshots, descriptions and claims must match the submitted build.
6. Submit the MSIX through Partner Center. Microsoft signs Store MSIX packages during publication after certification. The existing MSI/EXE listing route requires your own Authenticode signing and is not the route prepared here.

The submission build is named **store-unsigned**. Do not distribute it as an installable GitHub download. Outside Microsoft Store, MSIX still requires trusted signing.

## Versioning

The app remains on its current preview version. The package defaults to **(app major + 1).minor.patch.0**: app `0.6.1` becomes package `1.6.1.0`. This satisfies Store's nonzero major and reserved final zero without claiming app version 1.6.1. You can set `-PackageVersion 1.6.2.0`; maintain strictly increasing package versions for updates and preserve the Store identity. Each field must be <= 65535. ARM64 packaging can use `-Runtime win-arm64`, but requires a corresponding portable build and real ARM64 validation before availability.

## Data and startup require installed testing

The app currently uses `%LOCALAPPDATA%\Checkpoint` and creates an optional Startup shortcut. MSIX can virtualize application data paths, and its installation directory can change with updates. Do not assume portable/MSI libraries automatically appear in the packaged app or that the current startup shortcut survives Store updates. These are open Store compatibility checks; migrate startup to the package-supported Windows startup mechanism if testing requires it.

Before moving from portable/MSI to Store, export a `.checkpoint` backup, then import it in the Store edition. Game privacy and lists are included; sessions and the publication queue are excluded. After signing in again, tracked non-private games publish automatically. Test actual migration, update and uninstall behavior before promising data preservation; MSIX uninstall may remove package-scoped data.

The `runFullTrust` capability is needed because Checkpoint is a desktop widget with a WPF shell and WebView2 CSS interface with SQLite, a tray icon, keyboard shortcuts and user-selected backups. Provide that explanation to certification reviewers. This capability does not request elevation.

## Pending external requirements

Developer registration/verification, a reserved Store identity, installed-package/upgrade/data/startup testing, Windows App Certification Kit, a public privacy URL describing the current service, real Steam/two-Checkpoint-account validation, and Microsoft's review are still pending. No Partner Center account was created and no Store submission was sent by these scripts.

Sources: [Microsoft MSIX packaging guide](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-manual-conversion), [Store package/signing/version requirements](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/app-package-requirements), [product identity](https://learn.microsoft.com/en-us/windows/apps/publish/view-app-identity-details), [free individual registration](https://learn.microsoft.com/en-us/windows/apps/publish/whats-new-individual-developer).
