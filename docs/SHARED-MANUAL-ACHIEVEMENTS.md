# Shared manual achievements

Checkpoint friends can see the name, description and effective completion state of manually created achievements on shared games. Each entry is labeled **Manual achievement · Checkpoint**. These entries are personal Checkpoint goals; they do not unlock Steam or RetroAchievements achievements.

Private games remain excluded by the existing sharing rules. Private notes, task labels, local executable names, achievement images and completion overrides for provider achievements are not included in this new field.

## Backend

Apply `supabase/migrations/202610040005_checkpoint_shared_manual_achievements.sql` before deploying the updated clients. The migration was applied to the configured Supabase project on 2026-10-04. Existing clients and publications remain compatible. Friend access, revision checks, idempotency and text-only storage constraints remain in place.

The publication field `manualAchievementsJson` contains a bounded JSON string with up to 200 entries, each containing only `name`, `description`, and `completed`. A deterministic string retains value equality in the desktop outbox, preventing unchanged content from being uploaded repeatedly. It excludes removed entries and uses the effective manual completion state.

## Client rollout

Both the person sharing and the friend viewing need Checkpoint 0.8.23 or later. After the sharing client updates, its normal publication process sends the manual achievement data. Previous publications cannot recover manual details that were never sent to Supabase.
