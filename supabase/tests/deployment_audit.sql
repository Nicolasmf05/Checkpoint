-- Read-only deployment audit; run as postgres in the SQL Editor.
select
 (select jsonb_object_agg(c.relname,c.relrowsecurity) from pg_class c
  join pg_namespace n on n.oid=c.relnamespace
  where n.nspname='public' and c.relkind='r' and c.relname like 'cp_%') as table_rls,
 (select count(*) from auth.users where id::text like '90000000-%' or id::text like '91000000-%') as remaining_test_users,
 (select count(*) from storage.objects where name like '91000000-%') as remaining_test_assets,
 (select public from storage.buckets where id='checkpoint-assets') as bucket_public;
