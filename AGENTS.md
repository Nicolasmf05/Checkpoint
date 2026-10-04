# Checkpoint project instructions

## Release policy

The owner requires complete, stable GitHub releases, never prereleases. Publish only after the applicable CI checks succeed and all six distribution assets are uploaded and SHA-256 verified: Windows x64 portable ZIP, Windows x64 MSI, source ZIP, and one checksum file per archive/installer. Use a draft during preparation; do not expose an incomplete release.

After a new complete stable release is verified as public/latest, convert all earlier public releases to drafts. Preserve their assets and tags; never delete them or rewrite Git history. This hides older release publications but does not make Git history, tags, existing forks or previously downloaded copies private.

Use `scripts/Publish-Release.py` with release notes. Keep credentials out of source and logs. Do not replace assets of an existing public release; use a new version for changed packages. The owner has authorized publication and this older-release draft policy for ongoing Checkpoint work.

Creator attribution is required by the current LICENSE. Original v0.8.5 and earlier MIT grants remain unchanged; release visibility changes do not revoke old licenses.

## Collaboration and design

The owner develops Checkpoint with a collaborator. Preserve the collaborator’s existing edits; never overwrite or revert them. Do not modify graphic design, CSS, themes, colors, spacing, or visual styling. Functional fixes may change behavior and data binding while retaining the existing design. The unfinished friends-achievement visual change is paused: do not publish it or revert it without the owner’s instruction.

Exception authorized by the owner: improve the game details sheet, including a clock card for playtime. Keep styling changes scoped to that sheet and preserve the collaborator’s design elsewhere.
