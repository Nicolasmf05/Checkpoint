-- Pruebas de políticas RLS con identidades distintas para amistad, bloqueos y publicaciones.
-- Run in the Supabase SQL editor after the migration, as postgres.
-- Synthetic users only; every write is rolled back, including on failure
-- (if the editor reports an error, run ROLLBACK before another query).
BEGIN;

CREATE TEMPORARY TABLE cp_test_results (test text PRIMARY KEY);

GRANT insert,
SELECT
  ON cp_test_results TO authenticated;

INSERT INTO
  auth.users (id, email, raw_user_meta_data)
VALUES
  (
    '90000000-0000-4000-8000-000000000001',
    'cp-test-a@example.invalid',
    '{"display_name":"Test A"}'
  ),
  (
    '90000000-0000-4000-8000-000000000002',
    'cp-test-b@example.invalid',
    '{"display_name":"Test B"}'
  ),
  (
    '90000000-0000-4000-8000-000000000003',
    'cp-test-c@example.invalid',
    '{"display_name":"Test C"}'
  );

DO $$ begin
 if (select count(*) from public.cp_profiles where user_id::text like '90000000-%') <> 3 then
   raise exception 'Signup must create three profiles'; end if;
 if has_table_privilege('anon','public.cp_game_publications','SELECT') or
    has_function_privilege('anon','public.cp_publish_game(uuid,bigint,uuid,jsonb)','EXECUTE') then
   raise exception 'Anonymous access is forbidden'; end if;
 if has_table_privilege('authenticated','public.cp_game_publications','UPDATE') or
    has_table_privilege('authenticated','public.cp_friendships','INSERT') then
   raise exception 'Direct publication/friendship writes are forbidden'; end if;
 insert into cp_test_results values ('Signup profile trigger'), ('Anonymous grants denied'), ('Direct writes denied');
end $$;

SET
  local role authenticated;

SELECT
  set_config(
    'request.jwt.claim.sub',
    '90000000-0000-4000-8000-000000000001',
    TRUE
  );

DO $$
declare payload jsonb := '{"title":"Test game","platform":"PC","status":"playing","goalKind":"story","storyPercent":40}';
begin
 if (select count(*) from public.cp_profiles) <> 1 then raise exception 'A can only see own profile initially'; end if;
 if public.cp_publish_game('90000000-0000-4000-8000-000000000010',0,'90000000-0000-4000-8000-000000000011',payload) <> 1 then
   raise exception 'First revision must be one'; end if;
 if public.cp_publish_game('90000000-0000-4000-8000-000000000010',0,'90000000-0000-4000-8000-000000000011',payload) <> 1 then
   raise exception 'Retry must be idempotent'; end if;
 begin
   perform public.cp_publish_game('90000000-0000-4000-8000-000000000010',0,'90000000-0000-4000-8000-000000000011',payload || '{"storyPercent":60}');
   raise exception 'Changed retry must fail';
 exception when invalid_parameter_value then null; end;
 begin
   perform public.cp_publish_game('90000000-0000-4000-8000-000000000010',0,'90000000-0000-4000-8000-000000000012',payload);
   raise exception 'Stale revision must fail';
 exception when serialization_failure then null; end;
 begin
   perform public.cp_publish_game('90000000-0000-4000-8000-000000000010',1,'90000000-0000-4000-8000-000000000012',payload || '{"notes":"private"}');
   raise exception 'Private notes must be rejected';
 exception when invalid_parameter_value then null; end;
 begin
   perform public.cp_publish_game('90000000-0000-4000-8000-000000000010',1,'90000000-0000-4000-8000-000000000012',payload || '{"storyPercent":101}');
   raise exception 'Invalid progress must be rejected';
 exception when check_violation then null; end;
 begin
   perform public.cp_publish_game('90000000-0000-4000-8000-000000000010',1,'90000000-0000-4000-8000-000000000012',payload || '{"title":{"notes":"unexpected object"}}');
   raise exception 'Object in text field must be rejected';
 exception when invalid_parameter_value then null; end;
 insert into public.cp_friend_requests(sender_id,recipient_id) values
   (auth.uid(),'90000000-0000-4000-8000-000000000002');
 if (select count(*) from public.cp_profiles) <> 2 then raise exception 'Pending request reveals participant profile'; end if;
 update public.cp_friend_requests set status = 'accepted';
 if exists(select 1 from public.cp_friendships) then raise exception 'Sender cannot accept own invitation'; end if;
 insert into cp_test_results values ('Own profile only'), ('Publish revision'), ('Idempotent retry'),
   ('Changed retry rejected'), ('Stale revision rejected'), ('Notes rejected'),
   ('Invalid progress rejected'), ('Invalid field type rejected'), ('Pending profile visible'), ('Sender cannot accept');
end $$;

SELECT
  set_config(
    'request.jwt.claim.sub',
    '90000000-0000-4000-8000-000000000002',
    TRUE
  );

DO $$ begin
 if exists(select 1 from public.cp_game_publications) then raise exception 'Pending invitation cannot see games'; end if;
 update public.cp_friend_requests set status = 'accepted';
 if (select count(*) from public.cp_friendships) <> 1 then raise exception 'Acceptance must establish friendship'; end if;
 if (select count(*) from public.cp_game_publications where story_percent = 40) <> 1 then raise exception 'Accepted friend sees shared progress'; end if;
 begin
   update public.cp_game_publications set story_percent = 99;
   raise exception 'Friends cannot edit publications';
 exception when insufficient_privilege then null; end;
 insert into cp_test_results values ('Pending games hidden'), ('Acceptance creates friendship'),
   ('Friend sees shared progress'), ('Friend cannot edit games');
end $$;

SELECT
  set_config(
    'request.jwt.claim.sub',
    '90000000-0000-4000-8000-000000000003',
    TRUE
  );

DO $$ begin
 if (select count(*) from public.cp_profiles) <> 1 or exists(select 1 from public.cp_game_publications) or
    exists(select 1 from public.cp_friendships) or exists(select 1 from public.cp_friend_requests) then
   raise exception 'Third party sees private relationship/data'; end if;
 begin
   insert into public.cp_friend_requests(sender_id,recipient_id) values
     ('90000000-0000-4000-8000-000000000001',auth.uid());
   raise exception 'Forged sender must fail';
 exception when insufficient_privilege then null; end;
 insert into cp_test_results values ('Third party isolated'), ('Forged sender rejected');
end $$;

SELECT
  set_config(
    'request.jwt.claim.sub',
    '90000000-0000-4000-8000-000000000002',
    TRUE
  );

DO $$ begin
 insert into public.cp_blocks(blocker_id,blocked_id) values(auth.uid(),'90000000-0000-4000-8000-000000000001');
 if exists(select 1 from public.cp_game_publications) or exists(select 1 from public.cp_friendships) or
    exists(select 1 from public.cp_friend_requests) then raise exception 'Block must revoke relationship/access'; end if;
 delete from public.cp_blocks;
 if exists(select 1 from public.cp_game_publications) or exists(select 1 from public.cp_friendships) then
   raise exception 'Unblocking must not restore friendship'; end if;
 insert into cp_test_results values ('Block revokes access'), ('Unblock needs new acceptance');
end $$;

SELECT
  set_config(
    'request.jwt.claim.sub',
    '90000000-0000-4000-8000-000000000001',
    TRUE
  );

DO $$ begin
 if public.cp_publish_game('90000000-0000-4000-8000-000000000010',1,'90000000-0000-4000-8000-000000000013',null) <> 2 then
   raise exception 'Withdrawal increments revision'; end if;
 if exists(select 1 from public.cp_game_publications where is_shared or title is not null or operation_payload is not null) then
   raise exception 'Withdrawal must erase shared payload'; end if;
 if (select count(*) from public.cp_game_publications where revision = 2) <> 1 then raise exception 'Owner needs tombstone for conflicts'; end if;
 insert into cp_test_results values ('Withdraw clears shared payload'), ('Owner retains revision tombstone');
end $$;

RESET ROLE;

SELECT
  count(*) AS passed_checks,
  array_agg(
    test
    ORDER BY
      test
  ) AS checks
FROM
  cp_test_results;

ROLLBACK;
