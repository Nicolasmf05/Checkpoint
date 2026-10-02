# Windows security messages

**English** · [Español](../WINDOWS-SECURITY.md)

Checkpoint preview EXE/MSI packages are unsigned. ZIP extraction does not change the executable's signature or reputation.

## Identify the message

- **“Windows protected your PC” / unknown publisher:** Microsoft Defender SmartScreen evaluates file/publisher reputation. A new unsigned release can trigger a warning. A trusted code-signing certificate and consistent signed releases are part of proper distribution; signing alone does not guarantee immediate reputation.
- **Smart App Control:** Windows can block untrusted/unsigned applications. This requires a suitable trusted distribution/signing route, not a different ZIP layout.
- **Threat name such as `Trojan:…`:** open Windows Security → Virus & threat protection → Protection history and record the exact detection name and affected file. Compare the downloaded file's SHA-256 with the release. Do not assume a false positive from successful app tests.

Do not disable protection, add exclusions or restore a quarantined executable to make the app run. A file detection requires investigation; if incorrectly classified, the developer can submit the exact public release artifact to Microsoft for review. A security message is not fixed by renaming, repacking or self-signing the application.

No public-trust code-signing certificate is configured for this project. Signing requires a suitable certificate/service and publisher identity verification. Security reviews and certificate provisioning are separate from the packaging fix.

Sources: [Microsoft SmartScreen developer guidance](https://learn.microsoft.com/windows/apps/package-and-deploy/smartscreen-reputation), [Smart App Control](https://learn.microsoft.com/windows/security/book/application-security-application-and-driver-control), [Microsoft file analysis submissions](https://learn.microsoft.com/unified-secops/submission-guide).

## Microsoft Store alternative

MSIX Store distribution lets Microsoft sign the package after certification, without a personal code-signing certificate. The project has a validated unsigned packaging preview, not a published Store app. See [Store preparation and remaining steps](MICROSOFT-STORE.md).
