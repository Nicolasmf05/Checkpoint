-- Consulta el despliegue para revisar tablas, permisos y funciones del esquema Checkpoint.
-- Read-only deployment audit; run as postgres in the SQL Editor.
SELECT
  (
    SELECT
      jsonb_object_agg(c.relname, c.relrowsecurity)
    FROM
      pg_class c
      JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE
      n.nspname = 'public'
      AND c.relkind = 'r'
      AND c.relname LIKE 'cp_%'
  ) AS table_rls,
  (
    SELECT
      count(*)
    FROM
      auth.users
    WHERE
      id::text LIKE '90000000-%'
      OR id::text LIKE '91000000-%'
  ) AS remaining_test_users,
  (
    SELECT
      count(*)
    FROM
      storage.objects
    WHERE
      name LIKE '91000000-%'
  ) AS remaining_test_assets,
  (
    SELECT
      public
    FROM
      storage.buckets
    WHERE
      id = 'checkpoint-assets'
  ) AS bucket_public;

SELECT
  EXISTS (
    SELECT
      1
    FROM
      pg_trigger
    WHERE
      tgname = 'cp_text_only_storage_guard'
      AND tgenabled = 'O'
  ) AS uploads_blocked,
  (
    SELECT
      count(*)
    FROM
      storage.objects
  ) AS retained_files;
