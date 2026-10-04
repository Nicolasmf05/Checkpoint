-- Comprueba acceso al catálogo comunitario y confirmaciones mediante el rol del servicio.
-- Run in the SQL Editor after the community-cover migration. All test writes are rolled back.
BEGIN;

DO $$
declare k text:=repeat('f',64); a jsonb:='{"title":"Checkpoint cover test","platform":"PC","candidate":{"id":1,"name":"Checkpoint cover test","imageId":"test_first","year":2020,"score":1}}'; b jsonb;
begin
  assert not has_schema_privilege('anon','checkpoint_steam','usage');
  assert not has_schema_privilege('authenticated','checkpoint_steam','usage');
  assert not has_function_privilege('anon','public.cp_community_cover(text,text,jsonb)','execute');
  assert not has_function_privilege('authenticated','public.cp_community_cover(text,text,jsonb)','execute');
  assert has_function_privilege('service_role','public.cp_community_cover(text,text,jsonb)','execute');
  assert (select relrowsecurity from pg_class where oid='checkpoint_steam.community_covers'::regclass);
  assert public.cp_community_cover('get',k) is null;
  b:=public.cp_community_cover('confirm',k,a);
  assert b->>'imageId'='test_first';
  assert public.cp_community_cover('get',k)=b;
  assert public.cp_community_cover('confirm',k,jsonb_set(a,'{candidate,imageId}','"test_second"'))=b;
  assert (select title from checkpoint_steam.community_covers where id=k)='Checkpoint cover test';
  assert (select pg_column_size(candidate) from checkpoint_steam.community_covers where id=k)<2048;
  assert not b ? 'customCover';
  begin
    perform public.cp_community_cover('confirm',repeat('e',64),jsonb_set(a,'{candidate,imageId}','"https://invalid.test/image.png"'));
    raise exception 'Invalid image reference was accepted';
  exception when others then
    if sqlerrm='Invalid image reference was accepted' then raise; end if;
  end;
end $$;

SELECT
  '14 checks passed: text only, private permissions and stable first choice; test data is rolled back' AS result;

ROLLBACK;
