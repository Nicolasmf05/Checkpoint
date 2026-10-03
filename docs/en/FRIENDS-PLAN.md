# Checkpoint friends: scope and future work

**English** · [Español](../FRIENDS-PLAN.md)

Friends belong to Checkpoint, independently of Steam. The native client and Supabase backend implement accounts, accepted requests, blocks and default progress sharing with per-game privacy. Real two-account testing remains required before general release.

## Progress and privacy

Tracked games are published by default unless marked private; friends never query their Steam account directly. States: pending, playing, paused, finished, abandoned. Story percentage is manual, never inferred from hours or achievements. Tasks and saved Steam achievements show separate completed/total counters. Custom goals can publish their text. Notes, task labels and individual achievements are excluded.

A game without numerical progress shows its state, not a fabricated 0%. Manual games and other platforms may be shared. Matching the same game across collections needs a common catalog ID or explicit association; local IDs/titles alone do not suffice.

## Identity and authorization

Accounts use username/password without real email or confirmation; friend codes provide exact lookup. Requests require acceptance. Reject, cancel, unfriend, block and unblock are implemented. Rate limits include canceled attempts. Every read/write/image request is checked by server permissions; changing IDs does not grant access to another account.

Password recovery, editable avatars and full account deletion UI remain pending. The schema supports avatars. Public signup is configured, but end-to-end registration still needs real-account validation.

## Persistence and synchronization

Supabase persists profiles, requests, friendships, blocks and shared game revisions. The private SQLite collection remains local; restoring the entire library on another PC is separate future work.

Consent/outbox belong to the active account and project. Allowlisted payloads prevent accidental note uploads. Persisted operation IDs/revisions enable idempotent retry after disconnects and explicit cross-device conflicts. Withdrawals are synchronized and acknowledged; old published data may remain visible while offline. New imports/restores do not acquire sharing consent automatically. Signing out does not withdraw existing publications.

Shared custom covers have authenticated reads, validated types/sizes and owner paths. No local filesystem paths are published. Private client images stay in memory. Already received data/screenshots cannot be erased remotely.

## Interface and hosting

Friends, Requests, Sharing and Account pages support narrow/resizable widgets, both themes and both languages. Friends refresh by polling every 60 seconds. Favorites among friends, recent-activity feeds and richer statistics are future work requiring consent and publication permissions.

An HTTPS backend must stay available even when another PC is off. GitHub distributes source/artifacts, not the running Supabase service. The friend feature needs no Steam key; optional Steam achievements still require the separate Steam server setup.

## Validation

Implemented automated checks cover consent, third-party isolation, pending-request restrictions, blocks, private fields, manipulated IDs, retries, conflicts and withdrawals. Remaining tests: two real accounts/PCs; live private-image upload/download; simultaneous clients; offline deletion and backup restore; account changes; clean installer and real Steam linking. Account deletion/retention needs an operator-managed process.

See [deployment](SUPABASE.md), [privacy](PRIVACY.md) and [validation](VALIDATION.md). Design references: [OWASP authentication](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html), [authorization](https://cheatsheetseries.owasp.org/cheatsheets/Authorization_Cheat_Sheet.html); these guides do not themselves prove implementation correctness.
