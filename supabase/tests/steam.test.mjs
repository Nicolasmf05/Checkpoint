import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createSteamHandler } from '../functions/checkpoint-steam/index.ts';
const base='https://test.supabase.co/functions/v1/checkpoint-steam/';
const steamId='76561198000000000';
const timestamp=Date.parse('2026-10-02T12:00:00Z');
const digest=async value=>Buffer.from(await crypto.subtle.digest('SHA-256',new TextEncoder().encode(value))).toString('hex');
function fixture(options={}) {
  let time=timestamp; const db=new Map(),calls=[];
  // Model the atomic database operations; PostgreSQL tests separately exercise their real implementation.
  const rpc=async ({p_action:a,p_kind:k,p_id:id,p_value:v,p_ttl:ttl})=>{
    const key=k+':'+id; const old=db.get(key); const current=old?.expires>time?old.value:null;
    const put=(key,value,seconds)=>db.set(key,{value:structuredClone(value),expires:time+seconds*1000});
    if(a==='get') return structuredClone(current);
    if(a==='put') {put(key,v,ttl);return true;}
    if(a==='delete') {db.delete(key);return true;}
    if(a==='limit') {const count=(current?.count||0)+1;db.set(key,{value:{count},expires:current?old.expires:time+ttl*1000});return count<=v.max;}
    if(a==='claim') {
      const nonce=db.get('nonce:'+v.nonceHash);
      if(!current||current.steamId||nonce?.expires>time) return false;
      put('nonce:'+v.nonceHash,{},600);db.set(key,{...old,value:{...current,steamId:v.steamId}});return true;
    }
    if(a==='poll') {
      if(!current||current.pollHash!==v.pollHash) return null;
      if(!current.steamId) return {status:'pending'};
      put('session:'+v.tokenHash,{steamId:current.steamId},604800);db.delete(key);
      return {status:'complete',steamId:current.steamId};
    }
    if(a==='logout') {
      db.delete(key);
      for(const entry of db.keys()) if(entry==='cache:library:'+current.steamId||entry.startsWith('cache:achievements:'+current.steamId+':')) db.delete(entry);
      return true;
    }
    throw new Error('Unexpected state action');
  };
  const upstream=async (input,init)=>{
    const url=new URL(input);calls.push({url,init});
    if(url.hostname==='steamcommunity.com') return new Response(options.invalidAssertion?'is_valid:false':'is_valid:true\n');
    if(url.pathname.includes('GetOwnedGames')) return Response.json(options.privateLibrary?{response:{}}:{response:{games:[{appid:620,name:'Portal 2',playtime_forever:70},...(url.searchParams.get('include_family_licenses')==='true'?options.familyGames||[]:[])]}});
    if(url.pathname.includes('GetRecentlyPlayedGames')) return options.recentUnavailable ? new Response('',{status:503}) : Response.json({response:{games:options.recentGames||[]}});
    if(url.pathname.includes('GetSchemaForGame')) return Response.json({game:{availableGameStats:{achievements:[{name:'FIRST',displayName:url.searchParams.get('l'),hidden:0}]}}});
    if(url.pathname.includes('GetPlayerAchievements')) return Response.json(options.privateAchievements?{playerstats:{success:false}}:{playerstats:{success:true,achievements:[{apiname:'FIRST',achieved:1,unlocktime:1000}]}});
    throw new Error('Unexpected upstream');
  };
  const make=()=>createSteamHandler({baseUrl:base,apiKey:options.noKey?'':'server-only-secret',rpc,fetchImpl:upstream,now:()=>time});
  let handler=make();
  const send=(path,data,token)=>handler(new Request(base+path,{method:data===undefined?'GET':'POST',
    headers:{...(data===undefined?{}:{'content-type':'application/json'}),...(token?{authorization:'Bearer '+token}:{})},body:data===undefined?undefined:JSON.stringify(data)}));
  async function start(){return await (await send('v1/auth/start',{})).json();}
  function assertion(flow,nonce='unique') {
    const returnTo=new URL(flow.authorizeUrl).searchParams.get('openid.return_to');
    return new URLSearchParams({flow:flow.flowId,'openid.ns':'http://specs.openid.net/auth/2.0','openid.mode':'id_res',
      'openid.op_endpoint':'https://steamcommunity.com/openid/login','openid.return_to':returnTo,
      'openid.claimed_id':'https://steamcommunity.com/openid/id/'+steamId,'openid.identity':'https://steamcommunity.com/openid/id/'+steamId,
      'openid.signed':'op_endpoint,claimed_id,identity,return_to,response_nonce,assoc_handle',
      'openid.response_nonce':new Date(time).toISOString().replace('.000Z','Z')+nonce,'openid.assoc_handle':'mock','openid.sig':'verified-by-upstream'});
  }
  async function login(nonce='unique') {
    const flow=await start();assert.equal((await send('v1/auth/callback?'+assertion(flow,nonce))).status,200);
    return await (await send('v1/auth/poll',{flowId:flow.flowId,pollSecret:flow.pollSecret})).json();
  }
  return {send,start,assertion,login,calls,db,restart:()=>handler=make(),advance:ms=>time+=ms,
    direct:request=>handler(request)};
}
test('health identifies missing server key without exposing secrets',async()=>{
  const f=fixture({noKey:true});assert.equal((await (await f.send('health')).json()).steamConfigured,false);
  assert.equal((await f.send('v1/auth/start',{})).status,503);
});
test('public and gateway-stripped routes both work',async()=>{
  const f=fixture();assert.equal((await f.direct(new Request('http://edge-runtime/checkpoint-steam/health'))).status,200);
  assert.equal((await f.direct(new Request(base.replace('/checkpoint-steam/','/different/')))).status,404);
});
test('login verifies Steam, imports owned games and never forwards the API key to the client',async()=>{
  const f=fixture(),session=await f.login();assert.equal(session.steamId,steamId);
  const library=await (await f.send('v1/library',undefined,session.token)).json();assert.equal(library.games[0].appId,620);
  assert.ok(!JSON.stringify(library).includes('server-only-secret'));
  const call=f.calls.find(c=>c.url.pathname.includes('GetOwnedGames'));
  assert.equal(call.init.headers['x-webapi-key'],'server-only-secret');assert.equal(call.url.searchParams.has('key'),false);
  assert.equal(call.init.redirect,'error');assert.equal(call.url.searchParams.get('steamid'),steamId);
});
test('flows and sessions survive handler restarts; database stores hashes only',async()=>{
  const f=fixture(),flow=await f.start();f.restart();
  assert.equal((await f.send('v1/auth/callback?'+f.assertion(flow))).status,200);f.restart();
  const session=await (await f.send('v1/auth/poll',{flowId:flow.flowId,pollSecret:flow.pollSecret})).json();f.restart();
  assert.equal((await f.send('v1/library',undefined,session.token)).status,200);
  const serialized=JSON.stringify([...f.db]);assert.ok(!serialized.includes(session.token));assert.ok(!serialized.includes(flow.pollSecret));
  assert.ok(f.db.has('session:'+await digest(session.token)));
});
test('poll secret is mandatory and a flow can only produce one session under concurrent polls',async()=>{
  const f=fixture(),flow=await f.start();
  assert.equal((await f.send('v1/auth/poll',{flowId:flow.flowId,pollSecret:'A'.repeat(43)})).status,400);
  assert.deepEqual(await (await f.send('v1/auth/poll',{flowId:flow.flowId,pollSecret:flow.pollSecret})).json(),{status:'pending'});
  await f.send('v1/auth/callback?'+f.assertion(flow));
  const responses=await Promise.all([1,2].map(()=>f.send('v1/auth/poll',{flowId:flow.flowId,pollSecret:flow.pollSecret})));
  assert.deepEqual(responses.map(r=>r.status).sort(),[200,400]);
});
test('invalid provider, return URL, duplicate fields and missing signed fields are rejected',async()=>{
  for(const modify of [p=>p.set('openid.op_endpoint','https://evil.example'),p=>p.set('openid.return_to','https://evil.example'),
    p=>p.append('openid.identity',p.get('openid.identity')),p=>p.set('openid.signed','identity')]) {
    const f=fixture(),flow=await f.start(),p=f.assertion(flow);modify(p);assert.equal((await f.send('v1/auth/callback?'+p)).status,400);
    assert.equal(f.calls.length,0);
  }
});
test('a Steam assertion rejected by Steam never creates a session',async()=>{
  const f=fixture({invalidAssertion:true}),flow=await f.start();assert.equal((await f.send('v1/auth/callback?'+f.assertion(flow))).status,400);
});
test('nonce replay across concurrent flows is rejected atomically',async()=>{
  const f=fixture(),flows=await Promise.all([f.start(),f.start()]);
  const responses=await Promise.all(flows.map(flow=>f.send('v1/auth/callback?'+f.assertion(flow,'same'))));
  assert.deepEqual(responses.map(r=>r.status).sort(),[200,400]);
});
test('session expiry and logout prevent further imports and remove private game cache',async()=>{
  const f=fixture(),session=await f.login();await f.send('v1/library',undefined,session.token);
  assert.equal((await f.send('v1/auth/logout',{},session.token)).status,200);
  assert.equal((await f.send('v1/library',undefined,session.token)).status,401);assert.ok(!f.db.has('cache:library:'+steamId));
  const second=await f.login('second');f.advance(604800001);assert.equal((await f.send('v1/library',undefined,second.token)).status,401);
});
test('private libraries and private achievements produce errors rather than fabricated progress',async()=>{
  const f=fixture({privateLibrary:true}),s=await f.login();assert.equal((await f.send('v1/library',undefined,s.token)).status,403);
  const g=fixture({privateAchievements:true}),t=await g.login();assert.equal((await g.send('v1/games/620/achievements',undefined,t.token)).status,403);
});
test('ownership is required; English and Spanish schemas and progress use separate caches',async()=>{
  const f=fixture(),s=await f.login();assert.equal((await f.send('v1/games/570/achievements',undefined,s.token)).status,403);
  for(const lang of ['en','es']) {
    const response=await (await f.send('v1/games/620/achievements?lang='+lang,undefined,s.token)).json();
    assert.equal(response.achievements[0].name,lang==='en'?'english':'spanish');assert.equal(response.achievements[0].unlocked,true);
  }
  const before=f.calls.length;f.restart();await f.send('v1/games/620/achievements?lang=en',undefined,s.token);assert.equal(f.calls.length,before);
});
test('durable global login limit survives restarts and ignores caller-supplied IPs',async()=>{
  const f=fixture();for(let i=0;i<30;i++){assert.equal((await f.send('v1/auth/start',{})).status,200);f.restart();}
  assert.equal((await f.send('v1/auth/start',{})).status,429);
});
test('oversized chunked request, arrays and missing auth are rejected',async()=>{
  const f=fixture();assert.equal((await f.send('v1/auth/start',{text:'x'.repeat(5000)})).status,413);
  assert.equal((await f.send('v1/auth/start',[])).status,400);assert.equal((await f.send('v1/library')).status,401);
});
test('database failure returns a generic error without service secrets',async()=>{
  const h=createSteamHandler({baseUrl:base,apiKey:'secret-key',rpc:()=>{throw new Error('service-role-secret');}});
  const r=await h(new Request(base+'v1/library'));assert.equal(r.status,503);assert.ok(!(await r.text()).includes('service-role-secret'));
});

test('family licenses and recently played borrowed games merge without duplicates and use the linked player achievements',async t=>{
  const options={familyGames:[{appid:998,name:'Family game',playtime_forever:50}],recentGames:[{appid:620,name:'Portal 2',playtime_forever:100},{appid:999,name:'Recently borrowed',playtime_forever:25}]};
  const f=fixture(options),{token}=await f.login();const send=(path)=>f.send(path,undefined,token);
  const library=await (await send('v1/library')).json();
  assert.deepEqual(library.games.map(game=>game.appId),[620,998,999]);assert.equal(library.games[0].playtimeMinutes,100);
  assert.equal(f.calls.find(call=>call.url.pathname.includes('GetOwnedGames')).url.searchParams.get('include_family_licenses'),'true');
  const response=await send('v1/games/999/achievements?steamid=76561198099999999');assert.equal(response.status,200);
  assert.equal(f.calls.find(call=>call.url.pathname.includes('GetPlayerAchievements')).url.searchParams.get('steamid'),steamId);
  assert.equal((await send('v1/games/1000/achievements')).status,403);
});
test('optional recent lookup failures retain the owned library and do not bypass private profiles',async t=>{
  const options={recentUnavailable:true};const f=fixture(options),{token}=await f.login();const send=(path)=>f.send(path,undefined,token);
  assert.equal((await (await send('v1/library')).json()).games[0].appId,620);
  const hidden=fixture({privateLibrary:true,recentGames:[{appid:999,name:'Borrowed'}]});const hiddenSession=await hidden.login();
  assert.equal((await hidden.send('v1/library',undefined,hiddenSession.token)).status,403);
  assert.ok(!hidden.calls.some(call=>call.url.pathname.includes('GetRecentlyPlayedGames')));
});
test('recent library entries reject invalid IDs and names and normalize time without duplicate games',async t=>{
  const options={recentGames:[null,{appid:0,name:'Invalid'},{appid:2147483648,name:'Invalid'},{appid:999,name:' '},{appid:999,name:'Borrowed',playtime_forever:-2},{appid:999,name:'Borrowed',playtime_forever:2.9}]};
  const f=fixture(options),{token}=await f.login();const send=(path)=>f.send(path,undefined,token);
  assert.deepEqual((await (await send('v1/library')).json()).games,[{appId:620,name:'Portal 2',playtimeMinutes:70},{appId:999,name:'Borrowed',playtimeMinutes:2}]);
});

test('deployment replaces an owned-only persisted library cache without revoking the session',async()=>{
  const f=fixture({recentGames:[{appid:999,name:'Borrowed',playtime_forever:25}]}),{token}=await f.login();
  f.db.set('cache:library:'+steamId,{expires:timestamp+900000,value:{games:[{appId:620,name:'Portal 2',playtimeMinutes:70}]}});
  assert.equal((await (await f.send('v1/library',undefined,token)).json()).games.length,2);
  const before=f.calls.length;f.restart();assert.equal((await (await f.send('v1/library',undefined,token)).json()).games.length,2);assert.equal(f.calls.length,before);
});


test('Pages browser preflight allows only the authorized origin without credentials or state mutation',async()=>{
 const f=fixture();const r=await f.direct(new Request(base+'v1/library',{method:'OPTIONS',headers:{origin:'https://nicolasmf05.github.io','access-control-request-method':'GET','access-control-request-headers':'authorization'}}));
 assert.equal(r.status,204);assert.equal(r.headers.get('access-control-allow-origin'),'https://nicolasmf05.github.io');assert.ok(r.headers.get('access-control-allow-headers').includes('authorization'));assert.equal(r.headers.get('access-control-allow-credentials'),null);assert.equal(f.db.size,0);
});
test('browser preflight rejects unrelated, lookalike and opaque origins',async()=>{
 const f=fixture();for(const origin of ['https://evil.example','https://nicolasmf05.github.io.evil.example','null','http://nicolasmf05.github.io']){const r=await f.direct(new Request(base+'v1/library',{method:'OPTIONS',headers:{origin}}));assert.equal(r.status,403);assert.equal(r.headers.get('access-control-allow-origin'),null);}
});
test('Pages CORS responses preserve bound-session authentication and private-data checks',async()=>{
 const f=fixture();const unauthorized=await f.direct(new Request(base+'v1/library',{headers:{origin:'https://nicolasmf05.github.io'}}));assert.equal(unauthorized.status,401);assert.equal(unauthorized.headers.get('access-control-allow-origin'),'https://nicolasmf05.github.io');
 const {token}=await f.login();const valid=await f.direct(new Request(base+'v1/library',{headers:{origin:'https://nicolasmf05.github.io',authorization:'Bearer '+token}}));assert.equal(valid.status,200);assert.equal((await valid.json()).games[0].appId,620);assert.equal(valid.headers.get('vary'),'Origin');
});
