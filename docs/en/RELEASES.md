# Complete release policy

**English** · [Español](../RELEASES.md)

Publish complete stable releases (`prerelease = false`), with only the newest public release visible. Prepare privately as a draft, then publish after successful applicable CI checks and verification of all six assets:

- Windows x64 portable ZIP and its SHA-256 file.
- Windows x64 MSI and its SHA-256 file.
- Source ZIP and its SHA-256 file.

Bump `Directory.Build.props` for changed packages. Commit the final source, run `./scripts/Build.ps1 -Installer` and `./scripts/Export-Source.ps1`, and prepare UTF-8 release notes. After the Build and verify workflow succeeds for that exact commit (and Checkpoint Web too, when it runs), execute:

```powershell
python scripts/Publish-Release.py --notes .qa/release-notes.md --name "Checkpoint VERSION — title"
```

Python 3.11 or later and Git are required for this maintainer tool; end users need neither. It uses an existing Git credential or `GH_TOKEN`/`GITHUB_TOKEN`, keeps credentials out of output and refuses to overwrite an already public release. Local checksum verification happens before API writes; remote SHA-256 and size are checked before publishing. The release tag must match the checked commit. Failed preparation leaves the release as a draft and does not hide the currently published release.

After the new complete stable release is verified as public and latest, earlier public releases become drafts. Their files are retained for maintainers with write access. Tags, commit history and repository source remain public. Draft visibility does not revoke prior downloads or license grants. On 2026-10-04, v0.8.6 was promoted to a complete release and the previous 31 releases were retained as drafts.

GitHub Actions builds packages and checks them; it does not automatically run this publishing command. The latest release is available at [Releases/latest](https://github.com/Nicolasmf05/Checkpoint/releases/latest).
