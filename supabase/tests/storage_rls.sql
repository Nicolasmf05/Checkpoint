-- Verifica las políticas históricas de Storage con usuarios y rutas de prueba.
-- No file bytes or existing data changes; all synthetic rows roll back.
BEGIN;

CREATE TEMPORARY TABLE cp_storage_results (test text PRIMARY KEY);

GRANT insert,
SELECT
  ON cp_storage_results TO authenticated,
  service_role;

DO $$ begin
 if not exists(select 1 from pg_trigger where tgname='cp_text_only_storage_guard' and tgenabled='O') then raise exception 'Missing guard'; end if;
 if (select count(*) from pg_policies where schemaname='storage' and tablename='objects' and policyname in ('cp_text_only_insert','cp_text_only_update') and permissive='RESTRICTIVE') <> 2 then raise exception 'Missing restrictive rules'; end if;
 begin
  insert into storage.objects(bucket_id,name) values('checkpoint-assets','text-only-test.png');
  raise exception 'Privileged insert allowed';
 exception when insufficient_privilege then null; end;
 insert into cp_storage_results values('Upload guard enabled'),('Restrictive rules enabled'),('Privileged upload denied');
end $$;

SET
  local role service_role;

DO $$ begin
 begin
  insert into storage.objects(bucket_id,name) values('checkpoint-assets','text-only-service.png');
  raise exception 'Service role insert allowed';
 exception when insufficient_privilege then null; end;
 begin
  perform public.cp_steam_state('put','cache','text-only-test','{"image":"data:image/png;base64,AAAA"}',60);
  raise exception 'Embedded image allowed';
 exception when check_violation then null; end;
 perform public.cp_steam_state('put','cache','text-only-test','{"title":"Text metadata"}',60);
 insert into cp_storage_results values('Service-role upload denied'),('Embedded image rejected'),('Text state accepted');
end $$;

RESET ROLE;

INSERT INTO
  auth.users (id, email, raw_user_meta_data)
VALUES
  (
    '91000000-0000-4000-8000-000000000001',
    'text-only-test@example.invalid',
    '{}'
  );

SET
  local role authenticated;

SELECT
  set_config(
    'request.jwt.claim.sub',
    '91000000-0000-4000-8000-000000000001',
    TRUE
  );

DO $$ declare revision bigint; begin
 begin
  insert into storage.objects(bucket_id,name) values('checkpoint-assets','91000000-0000-4000-8000-000000000001/covers/test.png');
  raise exception 'Authenticated upload allowed';
 exception when insufficient_privilege then null; end;
 revision:=public.cp_publish_game('91000000-0000-4000-8000-000000000010',0,'91000000-0000-4000-8000-000000000011','{"title":"Text test","platform":"PC","status":"playing","goalKind":"story","coverPath":"legacy/covers/image.png"}');
 if revision<>1 or exists(select 1 from public.cp_game_publications where owner_id=auth.uid() and (cover_path is not null or operation_payload->>'coverPath' is not null)) then raise exception 'Legacy image path retained'; end if;
 revision:=public.cp_publish_game('91000000-0000-4000-8000-000000000010',0,'91000000-0000-4000-8000-000000000011','{"title":"Text test","platform":"PC","status":"playing","goalKind":"story","coverPath":null}');
 if revision<>1 then raise exception 'Retry failed'; end if;
 begin
  update public.cp_profiles set avatar_path='legacy/avatar.png' where user_id=auth.uid();
  raise exception 'Avatar accepted';
 exception when check_violation then null; end;
 begin
  perform public.cp_publish_game('91000000-0000-4000-8000-000000000010',1,'91000000-0000-4000-8000-000000000012','{"title":"data:image/png;base64,AAAA","platform":"PC","status":"playing","goalKind":"story"}');
  raise exception 'Embedded publication image accepted';
 exception when check_violation then null; end;
 insert into cp_storage_results values('Authenticated upload denied'),('Legacy cover path ignored'),('Text retry idempotent'),('New avatar rejected'),('Embedded publication image rejected');
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
  cp_storage_results;

ROLLBACK;
