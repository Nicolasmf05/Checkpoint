import { test } from 'node:test';
import assert from 'node:assert/strict';
import { once } from 'node:events';
import { createService } from '../src/service.mjs';
import { validateOrigin, verifySteamAssertion, digest, secretMatches } from '../src/security.mjs';

const steamId = '76561198000000000';
const clock = Date.parse('2026-10-02T12:00:00Z');
function assertion(returnTo, nonce = 'unique', id = steamId) {
  return new URLSearchParams({ 'openid.ns': 'http://specs.openid.net/auth/2.0', 'openid.mode': 'id_res',
    'openid.op_endpoint': 'https://steamcommunity.com/openid/login', 'openid.return_to': returnTo,
    'openid.claimed_id': 'https://steamcommunity.com/openid/id/' + id,
    'openid.identity': 'https://steamcommunity.com/openid/id/' + id,
    'openid.signed': 'op_endpoint,claimed_id,identity,return_to,response_nonce,assoc_handle',
    'openid.response_nonce': new Date(clock).toISOString().replace('.000Z', 'Z') + nonce,
    'openid.assoc_handle': 'test', 'openid.sig': 'mock-verified-on-Steam' });
}
async function fixture(t, options = {}) {
  const calls = [];
  const upstream = async (input, init) => {
    const url = new URL(input); calls.push({ url, init });
    if (url.hostname === 'steamcommunity.com') return new Response('ns:http://specs.openid.net/auth/2.0\nis_valid:true\n');
    if (url.pathname.includes('GetOwnedGames')) return Response.json(options.privateLibrary ? { response: {} } : { response: { games: [{ appid: 620, name: 'Portal 2', playtime_forever: 70 },...(url.searchParams.get('include_family_licenses')==='true'?options.familyGames||[]:[])] } });
    if(url.pathname.includes('GetRecentlyPlayedGames')) return options.recentUnavailable ? new Response('',{status:503}) : Response.json({response:{games:options.recentGames||[]}});
    if (url.pathname.includes('GetSchemaForGame')) return Response.json({ game: { availableGameStats: { achievements: options.definitions || [
      { name: 'FIRST', displayName: 'First', description: 'First step', hidden: 0 }, { name: 'SECRET', displayName: 'Secret', description: 'Spoiler', hidden: 1 }
    ] } } });
    if (url.pathname.includes('GetPlayerAchievements')) return Response.json(options.privateAchievements ? { playerstats: { success: false } } : {
      playerstats: { success: true, achievements: options.progress || [{ apiname: 'FIRST', achieved: 1, unlocktime: 1000 }, { apiname: 'SECRET', achieved: 0 }] }
    });
    throw new Error('Unexpected upstream');
  };
  const server = createService({ apiKey: 'server-only-secret', publicUrl: 'http://127.0.0.1:34871', now: () => clock, fetchImpl: upstream, ...options });
  server.listen(0, '127.0.0.1'); await once(server, 'listening'); t.after(() => new Promise(resolve => server.close(resolve)));
  const base = 'http://127.0.0.1:' + server.address().port;
  const send = (path, data, token) => fetch(base + path, { method: data === undefined ? 'GET' : 'POST', headers: {
    ...(data === undefined ? {} : { 'content-type': 'application/json' }), ...(token ? { authorization: 'Bearer ' + token } : {})
  }, ...(data === undefined ? {} : { body: JSON.stringify(data) }) });
  async function login(nonce = 'login') {
    const start = await (await send('/v1/auth/start', {})).json();
    const returnTo = new URL(start.authorizeUrl).searchParams.get('openid.return_to');
    const params = assertion(returnTo, nonce); params.set('flow', start.flowId);
    assert.equal((await send('/v1/auth/callback?' + params)).status, 200);
    return { ...(await (await send('/v1/auth/poll', { flowId: start.flowId, pollSecret: start.pollSecret })).json()), start };
  }
  return { send, login, calls };
}

test('production origins require HTTPS; loopback supports local development', () => {
  assert.equal(validateOrigin('https://checkpoint.example/'), 'https://checkpoint.example');
  assert.equal(validateOrigin('http://127.0.0.1:34871'), 'http://127.0.0.1:34871');
  for (const value of ['http://example.com', 'https://user:pass@example.com', 'https://example.com/path', 'https://example.com/?token=x']) assert.throws(() => validateOrigin(value));
});
test('poll secrets compare hashes and reject invalid inputs', () => {
  assert.equal(secretMatches('secret', digest('secret')), true);
  assert.equal(secretMatches('wrong', digest('secret')), false);
  assert.equal(secretMatches(null, digest('secret')), false);
});
test('OpenID is verified directly against Steam and nonce replay is rejected', async () => {
  const nonces = new Map(), params = assertion('https://checkpoint.example/v1/auth/callback?flow=one');
  let target;
  const fetchImpl = async (url, options) => { target = url; assert.equal(options.body.get('openid.mode'), 'check_authentication'); return new Response('is_valid:true\n'); };
  assert.equal(await verifySteamAssertion(params, params.get('openid.return_to'), fetchImpl, nonces, clock), steamId);
  assert.equal(target, 'https://steamcommunity.com/openid/login');
  await assert.rejects(() => verifySteamAssertion(params, params.get('openid.return_to'), fetchImpl, nonces, clock));
});
test('OpenID rejects wrong callback, unsigned identity and spoofed provider', async () => {
  for (const mutate of [p => p.set('openid.return_to', 'https://attacker.test/'), p => p.set('openid.signed', 'return_to'), p => p.set('openid.op_endpoint', 'https://attacker.test/')]) {
    const params = assertion('https://checkpoint.example/callback'); mutate(params);
    await assert.rejects(() => verifySteamAssertion(params, 'https://checkpoint.example/callback', async () => new Response('is_valid:true'), new Map(), clock));
  }
});
test('OpenID rejects stale response and duplicate fields', async () => {
  const stale = assertion('https://checkpoint.example/callback'); stale.set('openid.response_nonce', '2020-01-01T00:00:00Zold');
  await assert.rejects(() => verifySteamAssertion(stale, stale.get('openid.return_to'), fetch, new Map(), clock));
  const duplicate = assertion('https://checkpoint.example/callback'); duplicate.append('openid.claimed_id', duplicate.get('openid.claimed_id'));
  await assert.rejects(() => verifySteamAssertion(duplicate, duplicate.get('openid.return_to'), fetch, new Map(), clock));
});
test('unconfigured release has health and privacy but no fake Steam login', async t => {
  const { send } = await fixture(t, { apiKey: '' });
  assert.equal((await (await send('/health')).json()).steamConfigured, false);
  assert.equal((await send('/v1/auth/start', {})).status, 503);
  const privacy = await send('/privacy');
  assert.equal(privacy.status, 200);
  const privacyText = await privacy.text();
  assert.match(privacyText, /<section lang="en">/);
  assert.match(privacyText, /<section lang="es">/);
  assert.match(privacyText, /Operator:/);
  assert.match(privacyText, /Responsable:/);
});
test('Steam library and achievement endpoints require a session', async t => {
  const { send } = await fixture(t);
  assert.equal((await send('/v1/library')).status, 401);
  assert.equal((await send('/v1/games/620/achievements')).status, 401);
});
test('login supports pending, rejects wrong poll secret, and consumes successful flow once', async t => {
  const { send, login } = await fixture(t);
  const start = await (await send('/v1/auth/start', {})).json();
  assert.equal((await (await send('/v1/auth/poll', { flowId: start.flowId, pollSecret: start.pollSecret })).json()).status, 'pending');
  assert.equal((await send('/v1/auth/poll', { flowId: start.flowId, pollSecret: 'wrong' })).status, 400);
  const result = await login(); assert.equal(result.steamId, steamId); assert.ok(result.token);
  assert.equal((await send('/v1/auth/poll', { flowId: result.start.flowId, pollSecret: result.start.pollSecret })).status, 400);
});
test('library is mapped, Steam API key stays in server header, and requests are cached', async t => {
  const { send, login, calls } = await fixture(t); const { token } = await login();
  const response = await send('/v1/library', undefined, token); const text = await response.text();
  assert.deepEqual(JSON.parse(text).games, [{ appId: 620, name: 'Portal 2', playtimeMinutes: 70 }]);
  assert.ok(!text.includes('server-only-secret'));
  await send('/v1/library', undefined, token);
  const steam = calls.filter(call => call.url.hostname === 'api.steampowered.com');
  assert.equal(steam.length, 2); assert.equal(steam[0].init.headers['x-webapi-key'], 'server-only-secret'); assert.ok(!steam[0].url.searchParams.has('key'));
});
test('achievement mapping keeps secrets marked and story state is absent', async t => {
  const { send, login } = await fixture(t); const { token } = await login();
  const data = await (await send('/v1/games/620/achievements', undefined, token)).json();
  assert.equal(data.achievements[0].unlocked, true); assert.equal(data.achievements[1].hidden, true);
  assert.equal(data.achievements[1].unlocked, false); assert.equal(data.status, undefined);
  assert.equal((await send('/v1/games/999/achievements', undefined, token)).status, 403);
});
test('private library gives an actionable error instead of an empty successful import', async t => {
  const { send, login } = await fixture(t, { privateLibrary: true }); const { token } = await login();
  assert.equal((await send('/v1/library', undefined, token)).status, 403);
});
test('private achievement response is a failure instead of zero progress', async t => {
  const { send, login } = await fixture(t, { privateAchievements: true }); const { token } = await login();
  assert.equal((await send('/v1/games/620/achievements', undefined, token)).status, 403);
});
test('logout immediately revokes the session', async t => {
  const { send, login } = await fixture(t); const { token } = await login();
  assert.equal((await send('/v1/auth/logout', {}, token)).status, 200);
  assert.equal((await send('/v1/library', undefined, token)).status, 401);
});
test('bad JSON shape and missing flow cannot create sessions', async t => {
  const { send } = await fixture(t);
  assert.equal((await send('/v1/auth/poll', null)).status, 400);
  assert.equal((await send('/v1/auth/poll', {})).status, 400);
});
test('login rate limit bounds unauthenticated flow creation', async t => {
  const { send } = await fixture(t);
  for (let i = 0; i < 10; i++) assert.equal((await send('/v1/auth/start', {})).status, 200);
  assert.equal((await send('/v1/auth/start', {})).status, 429);
});

test('family licenses and recently played borrowed games merge without duplicates and use the linked player achievements',async t=>{
  const options={familyGames:[{appid:998,name:'Family game',playtime_forever:50}],recentGames:[{appid:620,name:'Portal 2',playtime_forever:100},{appid:999,name:'Recently borrowed',playtime_forever:25}]};
  const f=await fixture(t,options),{token}=await f.login();const send=(path)=>f.send(path,undefined,token);
  const library=await (await send('/v1/library')).json();
  assert.deepEqual(library.games.map(game=>game.appId),[620,998,999]);assert.equal(library.games[0].playtimeMinutes,100);
  assert.equal(f.calls.find(call=>call.url.pathname.includes('GetOwnedGames')).url.searchParams.get('include_family_licenses'),'true');
  const response=await send('/v1/games/999/achievements?steamid=76561198099999999');assert.equal(response.status,200);
  assert.equal(f.calls.find(call=>call.url.pathname.includes('GetPlayerAchievements')).url.searchParams.get('steamid'),steamId);
  assert.equal((await send('/v1/games/1000/achievements')).status,403);
});
test('optional recent lookup failures retain the owned library and do not bypass private profiles',async t=>{
  const options={recentUnavailable:true};const f=await fixture(t,options),{token}=await f.login();const send=(path)=>f.send(path,undefined,token);
  assert.equal((await (await send('/v1/library')).json()).games[0].appId,620);
  const hidden=await fixture(t,{privateLibrary:true,recentGames:[{appid:999,name:'Borrowed'}]});const hiddenSession=await hidden.login();
  assert.equal((await hidden.send('/v1/library',undefined,hiddenSession.token)).status,403);
  assert.ok(!hidden.calls.some(call=>call.url.pathname.includes('GetRecentlyPlayedGames')));
});
test('recent library entries reject invalid IDs and names and normalize time without duplicate games',async t=>{
  const options={recentGames:[null,{appid:0,name:'Invalid'},{appid:2147483648,name:'Invalid'},{appid:999,name:' '},{appid:999,name:'Borrowed',playtime_forever:-2},{appid:999,name:'Borrowed',playtime_forever:2.9}]};
  const f=await fixture(t,options),{token}=await f.login();const send=(path)=>f.send(path,undefined,token);
  assert.deepEqual((await (await send('/v1/library')).json()).games,[{appId:620,name:'Portal 2',playtimeMinutes:70},{appId:999,name:'Borrowed',playtimeMinutes:2}]);
});

test('hidden descriptions use matching localized player data when schema text is missing',async t=>{
 const f=await fixture(t,{definitions:[{name:'SECRET',hidden:1,description:''},{name:'EMPTY',hidden:1}],progress:[{apiname:'SECRET',name:'Secret',description:'Player description',achieved:0},{apiname:'EMPTY',achieved:0}]});
 const session=await f.login();const result=await (await f.send('/v1/games/620/achievements',undefined,session.token)).json();
 assert.equal(result.achievements[0].description,'Player description');assert.equal(result.achievements[0].hidden,true);assert.equal(result.achievements[0].unlocked,false);assert.equal(result.achievements[1].description,'');
});
