-- Consulta el uso de Storage para comprobar la presencia de recursos del esquema anterior.
-- Read-only; no user identifiers or file names.
SELECT
  'database_total' AS scope,
  pg_database_size(current_database()) AS bytes
UNION ALL
SELECT
  'checkpoint_tables',
  coalesce(sum(pg_total_relation_size(c.oid)), 0)::bigint
FROM
  pg_class c
  JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE
  c.relkind = 'r'
  AND (
    n.nspname IN ('checkpoint_private', 'checkpoint_steam')
    OR (
      n.nspname = 'public'
      AND c.relname LIKE 'cp_%'
    )
  )
UNION ALL
SELECT
  'storage_files',
  coalesce(sum((metadata ->> 'size')::bigint), 0)
FROM
  storage.objects;

SELECT
  count(*) AS existing_files
FROM
  storage.objects;
