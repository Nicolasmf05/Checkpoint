import {test} from 'node:test';
import assert from 'node:assert/strict';
import {createCoverHandler,similarity} from '../functions/checkpoint-covers/index.ts';
const base='https://test.supabase.co/functions/v1/checkpoint-covers/';
function fixture(options={}){
 const db=new Map(),sharedCovers=options.sharedCovers||new Map(),calls=[];let time=100000;
 const rpc=async a=>{const k=a.p_kind+':'+a.p_id,old=db.get(k);if(old?.expires<=time)db.delete(k);if(a.p_action==='get')return db.get(k)?.value??null;if(a.p_action==='put'){db.set(k,{value:a.p_value,expires:time+a.p_ttl*1000});return true;}if(a.p_action==='limit'){const entry=db.get(k)||{value:{count:0},expires:time+a.p_ttl*1000};entry.value.count++;db.set(k,entry);return entry.value.count<=a.p_value.max;}};
 const fetchImpl=async(url,args={})=>{calls.push({url,args});if(url.includes('oauth2/token'))return Response.json({access_token:'server-token',expires_in:3600});if(url.endsWith('/games'))return Response.json(options.games||[{id:2,name:'Halo 2',cover:{image_id:'halo_two'}},{id:1,name:'Halo',cover:{image_id:'halo_first'}}]);return new Response(new Uint8Array([1,2,3]),{headers:{'content-type':'image/jpeg'}});};
 const community=async(action,id,value)=>{if(action==='confirm'&&!sharedCovers.has(id))sharedCovers.set(id,value.candidate);return sharedCovers.get(id)||null;};
 const handler=createCoverHandler({community,clientId:'client-id',clientSecret:'private-secret',rpc,fetchImpl,now:()=>time,...options});
 const search=(title='Halo',excluded=[],refresh=false)=>handler(new Request(base+'v1/search',{method:'POST',headers:{'content-type':'application/json','origin':'https://nicolasmf05.github.io'},body:JSON.stringify({title,excluded,refresh})}));
 const post=(path,data)=>handler(new Request(base+'v1/'+path,{method:'POST',headers:{'content-type':'application/json',origin:'https://nicolasmf05.github.io'},body:JSON.stringify(data)}));
 return {handler,search,calls,post,sharedCovers,advance:()=>time+=2000};
}
test('cover matches use normalized names and distinguish numbered sequels',()=>{assert.equal(similarity('Pokémon™','Pokemon'),1);assert(similarity('Halo','Halo')>similarity('Halo','Halo 2'));});
test('closest cover, exclusions and cache avoid repeating a declined image',async()=>{const f=fixture();let r=await f.search();assert.equal(r.status,200);assert.equal((await r.json()).candidate.imageId,'halo_first');r=await f.search('Halo',['halo_first']);assert.equal((await r.json()).candidate.imageId,'halo_two');r=await f.search('Halo',['halo_first','halo_two']);assert.equal((await r.json()).candidate,null);assert.equal(f.calls.filter(c=>c.url.endsWith('/games')).length,1);});
test('manual refresh makes a new provider request and tokens remain server-side',async()=>{const f=fixture();await f.search();f.advance();const r=await f.search('Halo',['halo_first'],true);assert.equal((await r.json()).candidate.imageId,'halo_two');assert.equal(f.calls.filter(c=>c.url.endsWith('/games')).length,2);assert.equal(f.calls.filter(c=>c.url.includes('oauth2')).length,1);assert(!f.calls[0].url.includes('private-secret'));assert(f.calls[0].args.body.includes('private-secret'));});
test('alternative names rank matches and games without usable covers are skipped',async()=>{const f=fixture({games:[{id:1,name:'Wrong',cover:{image_id:'x'}},{id:2,name:'Localized title',alternative_names:[{name:'Halo'}],cover:{image_id:'local'}},{id:3,name:'Halo',cover:{image_id:'../bad'}}]});assert.equal((await (await f.search()).json()).candidate.imageId,'local');});
test('missing setup reports only a safe code without requesting credentials',async()=>{const f=fixture({clientSecret:''}),r=await f.search();assert.equal(r.status,503);assert.deepEqual(await r.json(),{code:'igdb-not-configured'});assert.equal(f.calls.length,0);});
test('query strings are escaped and arbitrary image URLs cannot be proxied',async()=>{const f=fixture();await f.search('Halo"; fields *; search "');const query=f.calls.find(c=>c.url.endsWith('/games')).args.body;assert(query.includes('Halo\\"; fields *; search \\"'));const r=await f.handler(new Request(base+'v1/image/https://evil.test'));assert.equal(r.status,404);assert.equal(f.calls.filter(c=>c.url.includes('evil.test')).length,0);});
test('image proxy is fixed to IGDB, bounded and allows only the Pages CORS origin',async()=>{const f=fixture(),r=await f.handler(new Request(base+'v1/image/halo_first',{headers:{origin:'https://nicolasmf05.github.io'}}));assert.equal(r.status,200);assert.equal(r.headers.get('access-control-allow-origin'),'https://nicolasmf05.github.io');assert.equal(f.calls[0].url,'https://images.igdb.com/igdb/image/upload/t_cover_big/halo_first.jpg');const denied=await f.handler(new Request(base+'v1/image/halo_first',{headers:{origin:'https://evil.test'}}));assert.equal(denied.status,403);});
test('provider errors and oversized images never echo credentials or upstream content',async()=>{const f=fixture({fetchImpl:async()=>new Response('private-secret',{status:500})}),r=await f.search();assert.equal(r.status,503);assert(!(await r.text()).includes('private-secret'));const large=fixture({fetchImpl:async()=>new Response(new Uint8Array(2000001),{headers:{'content-type':'image/jpeg'}})}),image=await large.handler(new Request(base+'v1/image/a'));assert.equal(image.status,502);});
test('upstream rate limit caps fresh provider searches at three per second',async()=>{const f=fixture();for(let i=0;i<3;i++)assert.equal((await f.search('Halo '+i)).status,200);assert.equal((await f.search('Halo 4')).status,429);assert.equal(f.calls.filter(c=>c.url.endsWith('/games')).length,3);});

const halo={title:'Halo',platform:'PC',steamAppId:620};
test('a saved IGDB choice becomes the same default for a second client without storing images',async()=>{
 const f=fixture();const picked=(await (await f.search()).json()).candidate;
 assert.equal(f.sharedCovers.size,0);assert.equal((await (await f.post('confirm',{...halo,imageId:picked.imageId})).json()).shared,true);
 const second=fixture({sharedCovers:f.sharedCovers});const result=await (await second.post('shared',{...halo,title:'HALO™',excluded:[]})).json();
 assert.equal(result.candidate.imageId,'halo_first');assert.equal(second.calls.length,0);
 assert.deepEqual(Object.keys([...f.sharedCovers.values()][0]).sort(),['id','imageId','name','score','year']);
});
test('rejected images and other games never receive a shared default',async()=>{
 const f=fixture();await f.post('confirm',{...halo,imageId:'halo_first'});
 for(const query of [{...halo,excluded:['halo_first']},{...halo,steamAppId:621},{...halo,title:'Halo 2'},{...halo,steamAppId:null}])assert.equal((await (await f.post('shared',query)).json()).candidate,null);
 await f.post('confirm',{title:'Halo',platform:'PC',steamAppId:null,imageId:'halo_first'});
 assert.equal((await (await f.post('shared',{title:'Halo',platform:'Xbox',steamAppId:null})).json()).candidate,null);
});
test('shared covers require provider metadata and cannot be overwritten by another personal choice',async()=>{
 const f=fixture();assert.equal((await f.post('confirm',{...halo,imageId:'invented'})).status,400);assert.equal(f.sharedCovers.size,0);
 await f.post('confirm',{...halo,imageId:'halo_first'});assert.equal((await (await f.post('confirm',{...halo,imageId:'halo_two'})).json()).shared,false);
 assert.equal((await (await f.post('shared',halo)).json()).candidate.imageId,'halo_first');
 for(const data of [{...halo,imageId:'https://evil.test/a.png'},{...halo,steamAppId:-1,imageId:'halo_first'},{...halo,platform:'',imageId:'halo_first'}])assert.equal((await f.post('confirm',data)).status,400);
});
test('community lookups respect CORS and the public request limit',async()=>{
 const f=fixture();const denied=await f.handler(new Request(base+'v1/shared',{method:'POST',headers:{origin:'https://evil.test','content-type':'application/json'},body:JSON.stringify(halo)}));assert.equal(denied.status,403);
 for(let n=0;n<60;n++)assert.equal((await f.post('shared',halo)).status,200);assert.equal((await f.post('shared',halo)).status,429);
});
