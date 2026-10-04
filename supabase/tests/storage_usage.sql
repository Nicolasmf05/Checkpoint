-- Read-only; no user identifiers or file names.
select 'database_total' as scope,pg_database_size(current_database()) as bytes
union all select 'checkpoint_tables',coalesce(sum(pg_total_relation_size(c.oid)),0)::bigint from pg_class c join pg_namespace n on n.oid=c.relnamespace where c.relkind='r' and (n.nspname in ('checkpoint_private','checkpoint_steam') or (n.nspname='public' and c.relname like 'cp_%'))
union all select 'storage_files',coalesce(sum((metadata->>'size')::bigint),0) from storage.objects;
select count(*) as existing_files from storage.objects;
