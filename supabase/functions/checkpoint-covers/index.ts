// Public cover lookup; provider credentials and tokens remain server-side.
class CoverError extends Error { constructor(status,code){super(code);this.status=status;} }
const imageId=value=>typeof value==='string'&&/^[A-Za-z0-9_-]{1,80}$/.test(value);
const normalized=value=>value.replace(/[™®]/g,'').normalize('NFKD').replace(/[\u0300-\u036f]/g,'').toLowerCase().replace(/[^\p{L}\p{N}]+/gu,' ').trim();
export function similarity(left,right){
  const a=normalized(left),b=normalized(right);if(a===b)return 1;if(!a||!b)return 0;
  let row=Array.from({length:b.length+1},(_,i)=>i);
  for(let i=1;i<=a.length;i++){const next=[i];for(let j=1;j<=b.length;j++)next[j]=Math.min(next[j-1]+1,row[j]+1,row[j-1]+(a[i-1]===b[j-1]?0:1));row=next;}
  return 1-row[b.length]/Math.max(a.length,b.length);
}
async function bytes(response,max){
  if(Number(response.headers.get('content-length'))>max)throw new CoverError(502,'igdb-unavailable');
  const reader=response.body?.getReader();if(!reader)throw new CoverError(502,'igdb-unavailable');
  const chunks=[];let size=0;try{for(;;){const {done,value}=await reader.read();if(done)break;size+=value.length;if(size>max)throw new CoverError(502,'igdb-unavailable');chunks.push(value);}}finally{await reader.cancel();}
  const result=new Uint8Array(size);let offset=0;for(const chunk of chunks){result.set(chunk,offset);offset+=chunk.length;}return result;
}
const hash=async value=>Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256',new TextEncoder().encode(value)))).map(n=>n.toString(16).padStart(2,'0')).join('');
export function createCoverHandler({clientId='',clientSecret='',rpc,community,fetchImpl=fetch,now=Date.now}){
  let token=null,renewing;
  const state=(action,id,value={},ttl=600,kind='cache')=>rpc({p_action:action,p_kind:kind,p_id:'igdb:'+id,p_value:value,p_ttl:ttl});
  async function limit(id,max,ttl){if(!await state('limit',id,{max},ttl,'limit'))throw new CoverError(429,'rate');}
  async function authorize(){
    if(!clientId||!clientSecret)throw new CoverError(503,'igdb-not-configured');
    if(token&&token.expires>now()+60000)return token.value;
    if(!renewing)renewing=(async()=>{
      const response=await fetchImpl('https://id.twitch.tv/oauth2/token',{method:'POST',redirect:'error',signal:AbortSignal.timeout(20000),headers:{'content-type':'application/x-www-form-urlencoded'},body:new URLSearchParams({client_id:clientId,client_secret:clientSecret,grant_type:'client_credentials'}).toString()});
      if(!response.ok)throw new CoverError(503,'igdb-unavailable');const value=JSON.parse(new TextDecoder().decode(await bytes(response,16000)));
      if(typeof value.access_token!=='string'||!Number.isFinite(value.expires_in)||value.expires_in<1)throw new CoverError(503,'igdb-unavailable');
      token={value:value.access_token,expires:now()+value.expires_in*1000};return token.value;
    })().finally(()=>renewing=null);return renewing;
  }
  async function search(title,excluded,refresh=false){
    if(!clientId||!clientSecret)throw new CoverError(503,'igdb-not-configured');
    const cacheId='search:'+await hash(normalized(title));let games=refresh?null:await state('get',cacheId);
    if(!Array.isArray(games)){
      const bearer=await authorize();await limit('upstream',3,1);
      const response=await fetchImpl('https://api.igdb.com/v4/games',{method:'POST',redirect:'error',signal:AbortSignal.timeout(20000),headers:{'Client-ID':clientId,Authorization:'Bearer '+bearer,'content-type':'text/plain'},body:'search '+JSON.stringify(title)+'; fields name,alternative_names.name,cover.image_id,first_release_date; where cover != null; limit 30;'});
      if(response.status===401)token=null;
      if(!response.ok)throw new CoverError(response.status===429?429:502,response.status===429?'rate':'igdb-unavailable');
      const raw=JSON.parse(new TextDecoder().decode(await bytes(response,2000000)));if(!Array.isArray(raw))throw new CoverError(502,'igdb-unavailable');
      games=raw.slice(0,30).filter(g=>Number.isSafeInteger(g.id)&&g.id>0&&typeof g.name==='string'&&imageId(g.cover?.image_id)).map(g=>({id:g.id,name:g.name.slice(0,250),imageId:g.cover.image_id,year:Number.isFinite(g.first_release_date)?new Date(g.first_release_date*1000).getUTCFullYear():null,aliases:(g.alternative_names||[]).slice(0,20).filter(a=>typeof a.name==='string').map(a=>a.name.slice(0,250))}));
      await state('put',cacheId,games,604800);
    }
    const candidates=games.filter(g=>imageId(g.imageId)&&!excluded.includes(g.imageId)).map(g=>({...g,score:Math.max(similarity(title,g.name),...(g.aliases||[]).map(n=>similarity(title,n)))})).sort((a,b)=>b.score-a.score||a.id-b.id);
    const best=candidates[0];return {candidate:best?{id:best.id,name:best.name,imageId:best.imageId,year:best.year,score:best.score}:null};
  }
  async function identity(data){
    if(typeof data?.title!=='string'||!data.title.trim()||data.title.length>140||typeof data.platform!=='string'||data.platform.length>80||!data.platform.trim()||data.steamAppId!=null&&(!Number.isInteger(data.steamAppId)||data.steamAppId<1||data.steamAppId>2147483647))throw new CoverError(400,'invalid-query');
    const title=data.title.trim(),platform=data.platform.trim();
    return {title,platform,key:await hash(data.steamAppId?'steam:'+data.steamAppId+'|title:'+normalized(title):'title:'+normalized(title)+'|platform:'+normalized(platform))};
  }
  async function shared(data){
    const {key}=await identity(data);
    if(!Array.isArray(data.excluded||[])||(data.excluded||[]).length>200||!(data.excluded||[]).every(imageId))throw new CoverError(400,'invalid-query');
    const candidate=community?await community('get',key):null;
    return {candidate:candidate&&imageId(candidate.imageId)&&!(data.excluded||[]).includes(candidate.imageId)?candidate:null};
  }
  async function confirm(data,ip){
    const {title,platform,key}=await identity(data);if(!imageId(data.imageId))throw new CoverError(400,'invalid-query');
    if(!community)throw new CoverError(503,'igdb-unavailable');
    // First confirmed cover wins. A personal replacement does not overwrite everyone's choice.
    const existing=await community('get',key);if(existing)return {shared:existing.imageId===data.imageId};
    await limit('confirm:'+ip,20,86400);await limit('confirm-global',1000,86400);
    // Resolve the submitted image against IGDB metadata fetched by this service, never a client URL.
    await search(title,[],false);
    const games=await state('get','search:'+await hash(normalized(title)));
    const game=Array.isArray(games)?games.find(g=>imageId(g.imageId)&&g.imageId===data.imageId):null;
    if(!game)throw new CoverError(400,'invalid-query');
    const candidate={id:game.id,name:game.name,imageId:game.imageId,year:game.year,score:Math.max(similarity(title,game.name),...(game.aliases||[]).map(n=>similarity(title,n)))};
    const stored=await community('confirm',key,{title,platform,candidate});
    return {shared:stored?.imageId===data.imageId};
  }
  return async request=>{
    const origin=request.headers.get('origin'),allowed=origin==='https://nicolasmf05.github.io';
    const headers={'cache-control':'no-store','x-content-type-options':'nosniff','vary':'Origin',...(allowed?{'access-control-allow-origin':origin}: {})};
    const json=(status,value)=>new Response(JSON.stringify(value),{status,headers:{...headers,'content-type':'application/json; charset=utf-8'}});
    if(request.method==='OPTIONS')return new Response(null,{status:allowed?204:403,headers:{...headers,'access-control-allow-methods':'POST, GET, OPTIONS','access-control-allow-headers':'content-type, apikey','access-control-max-age':'600'}});
    try{
      if(origin&&!allowed)throw new CoverError(403,'forbidden');
      const ip=request.headers.get('x-forwarded-for')?.split(',')[0].trim()||'desktop';await limit('ip:'+await hash(ip),60,60);
      const url=new URL(request.url);
      if((url.pathname.endsWith('/v1/shared')||url.pathname.endsWith('/v1/confirm'))&&request.method==='POST'){
        if(!request.headers.get('content-type')?.startsWith('application/json'))throw new CoverError(415,'invalid-query');
        let data;try{data=JSON.parse(new TextDecoder().decode(await bytes(request,20000)));}catch{throw new CoverError(400,'invalid-query');}
        return json(200,url.pathname.endsWith('/v1/shared')?await shared(data):await confirm(data,await hash(ip)));
      }
      if(url.pathname.endsWith('/v1/search')&&request.method==='POST'){
        if(!request.headers.get('content-type')?.startsWith('application/json'))throw new CoverError(415,'invalid-query');
        const raw=await bytes(request,20000);let data;try{data=JSON.parse(new TextDecoder().decode(raw));}catch{throw new CoverError(400,'invalid-query');}
        if(typeof data?.title!=='string'||!data.title.trim()||data.title.length>140||!Array.isArray(data.excluded||[])||(data.excluded||[]).length>200||!(data.excluded||[]).every(imageId))throw new CoverError(400,'invalid-query');
        if(data.refresh!==undefined&&typeof data.refresh!=='boolean')throw new CoverError(400,'invalid-query');
        return json(200,await search(data.title.trim(),data.excluded||[],data.refresh===true));
      }
      const id=url.pathname.match(/\/v1\/image\/([A-Za-z0-9_-]{1,80})$/)?.[1];
      if(id&&request.method==='GET'){
        const response=await fetchImpl('https://images.igdb.com/igdb/image/upload/t_cover_big/'+id+'.jpg',{redirect:'error',signal:AbortSignal.timeout(20000)});
        if(!response.ok||!response.headers.get('content-type')?.startsWith('image/'))throw new CoverError(502,'igdb-unavailable');
        return new Response(await bytes(response,2000000),{headers:{...headers,'content-type':response.headers.get('content-type')}});
      }
      return json(404,{code:'not-found'});
    }catch(error){return json(error instanceof CoverError?error.status:503,{code:error instanceof CoverError?error.message:'igdb-unavailable'});}
  };
}
if(typeof Deno!=='undefined'){
  const origin=Deno.env.get('SUPABASE_URL'),key=Deno.env.get('SUPABASE_SERVICE_ROLE_KEY');
  const rpc=async args=>{const response=await fetch(origin+'/rest/v1/rpc/cp_steam_state',{method:'POST',redirect:'error',signal:AbortSignal.timeout(10000),headers:{apikey:key,authorization:'Bearer '+key,'content-type':'application/json'},body:JSON.stringify(args)});if(!response.ok)throw new Error('Cover state unavailable');return await response.json();};
  const community=async(action,id,value={})=>{
    const response=await fetch(origin+'/rest/v1/rpc/cp_community_cover',{method:'POST',redirect:'error',signal:AbortSignal.timeout(10000),headers:{apikey:key,authorization:'Bearer '+key,'content-type':'application/json'},body:JSON.stringify({p_action:action,p_id:id,p_value:value})});
    if(!response.ok)throw new Error('Community cover unavailable');return await response.json();
  };
  Deno.serve(createCoverHandler({community,clientId:Deno.env.get('IGDB_CLIENT_ID')||'',clientSecret:Deno.env.get('IGDB_CLIENT_SECRET')||'',rpc}));
}
