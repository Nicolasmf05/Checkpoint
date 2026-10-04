# Checkpoint on Supabase

**English** · [Español](../SUPABASE.md)

## Deployment

Organization **Checkpoint**, Free plan, in the owner's second account. Project **checkpoint**, Ireland `eu-west-1`, ref `fumdnvvvoiwoiziwtmsu`, API `https://fumdnvvvoiwoiziwtmsu.supabase.co`.

Migration `202610020001_checkpoint_social.sql` was applied through SQL Editor on October 2, 2026. Real database: 23 social SQL checks and 10 Storage/request-limit checks with synthetic users in rolled-back transactions. Seven live HTTP checks rejected anonymous access to five tables/two RPCs (401). No test users/assets remained; the bucket is private.

The native client implements accounts, requests, friends, blocks and selected publications. Sessions use DPAPI; the durable outbox is bound to account/project. The local library works without sign-in.

Registration is enabled; **Confirm email is disabled**, at the owner's request, saved and verified through public Auth settings. Auth uses internal `username@accounts.checkpoint.invalid` addresses. No real email, confirmations or SMTP. No password recovery. Former Oracle Checkpoint services are stopped/disabled; files were preserved for recovery.

## Schema

- Auth-linked profiles independent of Steam; exact `checkpoint-…` friend codes.
- Recipient-approved requests, canonical friendships and blocks.
- Explicitly selected publications with status, goal and counters. Notes/task labels rejected.
- Row-level access to own data and accepted friends' shared games; pending requests grant no game access.
- Revisions, idempotent operations and conflicts; withdrawal clears content and retains a tombstone.
- Legacy private `checkpoint-assets`: existing files are preserved; migration 0.8.10 blocks new uploads.
- Persistent invitation limits including canceled attempts; blocking deletes relationships/requests.


## Reproduce setup

1. Create a dedicated Free EU project. Enable Data API, disable automatic exposure and enable automatic RLS.
2. Run the full transactional migration once in SQL Editor. It does not replace existing tables.
3. Run social/storage SQL tests as postgres; they finish with `ROLLBACK`. Roll back after errors before further queries.
4. Include only project URL and **publishable** key in public config. Never distribute Postgres passwords or secret/service-role keys.
5. Enable registration and disable Confirm email for username/password. Do not configure recovery for internal addresses.
6. Build the native app. Imports do not publish new games without selection. Offline withdrawals need acknowledgment.
7. Before a public release, test two real users, text-only upload rejection, disconnects, conflicts and access revocation.

`supabase/project.json` is public config only. SQL Editor does not automatically populate migration history; preserve migration files and `deployment.json`.

SQLite stores the private collection; Supabase stores selected social data. Full cloud restore, avatar editing and recovery are pending. Version 0.6 adds the Steam Edge Function; its server-side key and custom-auth setting must be configured before linking.

Tests cover anonymous/direct-write permissions, profiles, publications/retries/conflicts, private/invalid fields, consent, third-party isolation, blocking and withdrawal. Current Storage tests exercise upload denial across roles and text-only publication, without multi-connection concurrency. Native HTTP responses are simulated. Repeat HTTP checks with `node scripts/Verify-Supabase.mjs`; audit: `supabase/tests/deployment_audit.sql`. See [validation](VALIDATION.md).

Version 0.6 source adds a private Steam-state migration and Edge Function. See [Steam deployment](STEAM-SERVICE.md) for secrets, custom authentication and validation.

## Text-only storage (0.8.10)

Apply `supabase/migrations/202610040001_checkpoint_text_only.sql` after the social and Steam migrations. It blocks INSERT/UPDATE in Storage through restrictive RLS and a database trigger, including service-role writes. Existing objects remain readable under their original permissions. New publications ignore legacy cover paths; new avatar references and embedded base64 image data are rejected. Windows no longer uploads local covers. External provider URLs and local covers do not consume Supabase Storage.

Audit on October 4, 2026: database 12,007,091 bytes (about 11.5 MiB, including infrastructure); Checkpoint tables 614,400 bytes (600 KiB); one existing Storage image 101,125 bytes (about 99 KiB), retained at the owner’s request. Use `supabase/tests/storage_usage.sql` to measure again and `storage_rls.sql` for rollback checks. This policy limits file storage, not the growth of legitimate text data.
