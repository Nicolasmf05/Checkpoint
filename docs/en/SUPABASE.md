# Checkpoint on Supabase

**English** · [Español](../SUPABASE.md)

## Deployment

Organization **Checkpoint**, Free plan, in the owner's second account. Project **checkpoint**, Ireland `eu-west-1`, ref `fumdnvvvoiwoiziwtmsu`, API `https://fumdnvvvoiwoiziwtmsu.supabase.co`.

Migration `202610020001_checkpoint_social.sql` was applied through SQL Editor on October 2, 2026. Real database: 23 social SQL checks and 10 Storage/request-limit checks with synthetic users in rolled-back transactions. Seven live HTTP checks rejected anonymous access to five tables/two RPCs (401). No test users/assets remained; the bucket is private.

The native client implements accounts, requests, friends, blocks and selected publications. Sessions use DPAPI; the durable outbox is bound to account/project. The local library works without sign-in.

Registration is enabled; **Confirm email is disabled**, at the owner's request, saved and verified through public Auth settings. Auth uses internal `username@accounts.checkpoint.invalid` addresses. No real email, confirmations or SMTP. No password recovery. Former Oracle Checkpoint services are stopped/disabled; files were preserved for recovery.

## Schema

- Auth-linked profiles independent of Steam; exact `cp-…` friend codes.
- Recipient-approved requests, canonical friendships and blocks.
- Explicitly selected publications with status, goal and counters. Notes/task labels rejected.
- Row-level access to own data and accepted friends' shared games; pending requests grant no game access.
- Revisions, idempotent operations and conflicts; withdrawal clears content and retains a tombstone.
- Private `checkpoint-assets`, PNG/JPEG/WebP up to 2 MiB; owner paths and visibility-based reads.
- Persistent invitation limits including canceled attempts; blocking deletes relationships/requests.

The client uploads normalized PNGs and downloads private images with authorization, keeping them in memory. Lost access clears views on refresh; received images cannot be revoked retroactively. Avatars are supported by the schema but have no UI. Polling every 60 seconds; no Realtime publication.

## Reproduce setup

1. Create a dedicated Free EU project. Enable Data API, disable automatic exposure and enable automatic RLS.
2. Run the full transactional migration once in SQL Editor. It does not replace existing tables.
3. Run social/storage SQL tests as postgres; they finish with `ROLLBACK`. Roll back after errors before further queries.
4. Include only project URL and **publishable** key in public config. Never distribute Postgres passwords or secret/service-role keys.
5. Enable registration and disable Confirm email for username/password. Do not configure recovery for internal addresses.
6. Build the native app. Imports do not publish new games without selection. Offline withdrawals need acknowledgment.
7. Before a public release, test two real users, Storage upload/download, disconnects, conflicts and access revocation.

`supabase/project.json` is public config only. SQL Editor does not automatically populate migration history; preserve migration files and `deployment.json`.

SQLite stores the private collection; Supabase stores selected social data. Full cloud restore, avatar editing and recovery are pending. Version 0.6 adds the Steam Edge Function; its server-side key and custom-auth setting must be configured before linking.

Tests cover anonymous/direct-write permissions, profiles, publications/retries/conflicts, private/invalid fields, consent, third-party isolation, blocking and withdrawal. Storage tests exercise metadata permissions/request limits, not live Storage upload/download or multi-connection concurrency. Native HTTP responses are simulated. Repeat HTTP checks with `node scripts/Verify-Supabase.mjs`; audit: `supabase/tests/deployment_audit.sql`. See [validation](VALIDATION.md).

Version 0.6 source adds a private Steam-state migration and Edge Function. See [Steam deployment](STEAM-SERVICE.md) for secrets, custom authentication and validation.
