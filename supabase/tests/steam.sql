-- Pruebas de RPC del estado Steam: expiración, caché, límites y reutilización de nonces.
-- Run as postgres in SQL Editor, after the Steam migration. No persistent fixtures.
BEGIN;

DO $$
declare result jsonb; i integer;
begin
  assert not has_schema_privilege('anon','checkpoint_steam','usage');
  assert not has_schema_privilege('authenticated','checkpoint_steam','usage');
  assert not has_function_privilege('anon','public.cp_steam_state(text,text,text,jsonb,integer)','execute');
  assert not has_function_privilege('authenticated','public.cp_steam_state(text,text,text,jsonb,integer)','execute');
  assert has_function_privilege('service_role','public.cp_steam_state(text,text,text,jsonb,integer)','execute');
  perform public.cp_steam_state('put','flow','test-flow','{"pollHash":"poll","returnTo":"https://example.invalid/"}',600);
  assert public.cp_steam_state('poll','flow','test-flow','{"pollHash":"wrong","tokenHash":"token"}') is null;
  assert public.cp_steam_state('poll','flow','test-flow','{"pollHash":"poll","tokenHash":"token"}')->>'status'='pending';
  assert public.cp_steam_state('claim','flow','test-flow','{"steamId":"76561198000000000","nonceHash":"nonce"}')='true';
  assert public.cp_steam_state('claim','flow','test-flow','{"steamId":"76561198000000000","nonceHash":"nonce2"}')='false';
  perform public.cp_steam_state('put','flow','test-other','{"pollHash":"other"}',600);
  assert public.cp_steam_state('claim','flow','test-other','{"steamId":"76561198000000000","nonceHash":"nonce"}')='false';
  result=public.cp_steam_state('poll','flow','test-flow','{"pollHash":"poll","tokenHash":"token"}');
  assert result->>'status'='complete' and result->>'steamId'='76561198000000000';
  assert public.cp_steam_state('poll','flow','test-flow','{"pollHash":"poll","tokenHash":"token2"}') is null;
  assert public.cp_steam_state('get','session','token')->>'steamId'='76561198000000000';
  perform public.cp_steam_state('put','cache','library:76561198000000000','{"games":[]}');
  perform public.cp_steam_state('put','cache','achievements:76561198000000000:620:english','{"achievements":[]}');
  perform public.cp_steam_state('logout','session','token');
  assert public.cp_steam_state('get','session','token') is null;
  assert public.cp_steam_state('get','cache','library:76561198000000000') is null;
  assert public.cp_steam_state('get','cache','achievements:76561198000000000:620:english') is null;
  for i in 1..3 loop
    result=public.cp_steam_state('limit','limit','test-rate','{"max":2}',60);
    assert result=to_jsonb(i<=2);
  end loop;
  perform public.cp_steam_state('put','session','expired','{"steamId":"76561198000000000"}');
  update checkpoint_steam.entries set expires=now()-interval '1 second' where kind='session' and id='expired';
  assert public.cp_steam_state('get','session','expired') is null;
  raise notice 'Steam database assertions passed (20 including rate-limit iterations)';
end $$;

SELECT
  'Steam assertions passed' AS result,
  20 AS checks;

ROLLBACK;
