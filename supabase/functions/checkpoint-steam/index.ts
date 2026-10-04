// Dependency-free Edge Function. The same handler is exercised by Node's test runner.
class ApiError extends Error {
  constructor(status, message) { super(message); this.status = status; }
}
const secret = () => {
  const bytes = crypto.getRandomValues(new Uint8Array(32));
  return btoa(String.fromCharCode(...bytes)).replaceAll('+','-').replaceAll('/','_').replaceAll('=','');
};
const hash = async value => Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256',new TextEncoder().encode(value))))
  .map(x=>x.toString(16).padStart(2,'0')).join('');
const validSecret = value => typeof value === 'string' && /^[A-Za-z0-9_-]{43}$/.test(value);

export function createSteamHandler({baseUrl, apiKey='', rpc, fetchImpl=fetch, now=Date.now}) {
  const base = new URL(baseUrl.endsWith('/') ? baseUrl : baseUrl+'/');
  if (base.protocol !== 'https:' || base.username || base.password || base.search || base.hash)
    throw new Error('Invalid public endpoint');
  const state = (action,kind,id,value={},ttl=600) => rpc({p_action:action,p_kind:kind,p_id:id,p_value:value,p_ttl:ttl});
  const limit = async (id,max,ttl=60) => {
    if (!await state('limit','limit',id,{max},ttl)) throw new ApiError(429,'Demasiadas consultas. Espera un minuto y vuelve a intentarlo.');
  };
  async function body(request) {
    if (!request.headers.get('content-type')?.startsWith('application/json')) throw new ApiError(415,'Usa JSON.');
    // Bound the stream before decoding, including chunked requests without Content-Length.
    const reader=request.body?.getReader(); const chunks=[]; let size=0;
    if (!reader) throw new ApiError(400,'JSON no válido.');
    while (true) {
      const {done,value}=await reader.read(); if(done) break;
      size+=value.length; if(size>4096) { await reader.cancel(); throw new ApiError(413,'Petición demasiado grande.'); }
      chunks.push(value);
    }
    const bytes=new Uint8Array(size); let offset=0;
    for(const chunk of chunks) { bytes.set(chunk,offset); offset+=chunk.length; }
    try {
      const data=JSON.parse(new TextDecoder().decode(bytes));
      if(!data || typeof data!=='object' || Array.isArray(data)) throw new Error(); return data;
    } catch { throw new ApiError(400,'JSON no válido.'); }
  }
  async function authenticate(request) {
    const header=request.headers.get('authorization') || '';
    if(!header.startsWith('Bearer ') || !validSecret(header.slice(7))) throw new ApiError(401,'Vuelve a vincular tu cuenta de Steam.');
    const id=await hash(header.slice(7)); const session=await state('get','session',id);
    if(!session || !/^7656119\d{10}$/.test(session.steamId)) throw new ApiError(401,'La sesión ha caducado. Vuelve a vincular Steam.');
    await limit('session:'+id,90); return {...session,id};
  }
  async function steam(path,params) {
    if(!apiKey) throw new ApiError(503,'El responsable de esta edición todavía debe configurar el servicio de Steam.');
    const day=new Date(now()).toISOString().slice(0,10);
    if(!await state('limit','limit','steam:'+day,{max:90000},86400)) throw new ApiError(503,'Se ha alcanzado el límite diario. Inténtalo mañana.');
    const url=new URL(path,'https://api.steampowered.com/');
    for(const [key,value] of Object.entries(params)) url.searchParams.set(key,String(value));
    let response;
    try { response=await fetchImpl(url,{headers:{'x-webapi-key':apiKey},redirect:'error',signal:AbortSignal.timeout(20000)}); }
    catch { throw new ApiError(502,'Steam no responde. Se conserva tu último progreso.'); }
    if(!response.ok) throw new ApiError(502,'Steam no ha devuelto estos datos. Comprueba la privacidad de tus detalles de juegos.');
    try { return await response.json(); } catch { throw new ApiError(502,'Steam ha devuelto una respuesta no válida.'); }
  }
  async function cached(id,produce,ttl=900,valid=()=>true) {
    const old=await state('get','cache',id); if(old!==null&&valid(old)) return old;
    const value=await produce(); await state('put','cache',id,value,ttl); return value;
  }
  async function library(steamId) {
    const result=await cached('library:'+steamId,async()=>{
      const data=await steam('IPlayerService/GetOwnedGames/v1/',{steamid:steamId,include_appinfo:true,include_played_free_games:true,include_family_licenses:true});
      if(!Array.isArray(data.response?.games)&&data.response?.game_count!==0)
        throw new ApiError(403,'Steam no permite consultar tu biblioteca. Revisa la visibilidad de Detalles de juegos en Steam.');
      const owned=data.response.games||[];
      let recent=[];
      try { const played=await steam('IPlayerService/GetRecentlyPlayedGames/v1/',{steamid:steamId,count:0}); if(Array.isArray(played.response?.games)) recent=played.response.games; }
      catch(error) { if(!(error instanceof ApiError)) throw error; } // Optional recent data must not hide the owned library.
      const games=new Map();
      for(const game of [...owned.slice(0,10000),...recent.slice(0,10000)]) {
        if(!Number.isInteger(game?.appid)||game.appid<=0||game.appid>2147483647||typeof game.name!=='string'||!game.name.trim()) continue;
        const minutes=Math.min(2147483647,Math.floor(Math.max(0,Number(game.playtime_forever)||0)));
        const previous=games.get(game.appid);
        if(previous) previous.playtimeMinutes=Math.max(previous.playtimeMinutes,minutes);
        else if(games.size<10000) games.set(game.appid,{appId:game.appid,name:game.name.trim().slice(0,140),playtimeMinutes:minutes});
      }
      return {libraryRevision:2,games:[...games.values()]};
    },900,value=>value.libraryRevision===2);
    return {games:result.games};
  }
  const headers={'cache-control':'no-store','x-content-type-options':'nosniff','referrer-policy':'no-referrer',
    'content-security-policy':"default-src 'none'; style-src 'unsafe-inline'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'"};
  const json=(status,data)=>new Response(JSON.stringify(data),{status,headers:{...headers,'content-type':'application/json; charset=utf-8'}});
  // Supabase serves Edge Function HTML as plain text; a readable bilingual text callback works in every browser.
  const page=(en,es)=>new Response(`Checkpoint\n\n${en}\n\n${es}\n\nPrivacy / Privacidad: ${new URL('privacy',base)}`,{headers:{...headers,'content-type':'text/plain; charset=utf-8'}});
  async function verify(params,returnTo) {
    const single=name=>{if(params.getAll(name).length!==1) throw new Error(); return params.get(name);};
    if(single('openid.mode')!=='id_res'||single('openid.ns')!=='http://specs.openid.net/auth/2.0'
      ||single('openid.op_endpoint')!=='https://steamcommunity.com/openid/login'||single('openid.return_to')!==returnTo) throw new Error();
    const claimed=single('openid.claimed_id'),identity=single('openid.identity');
    const match=/^https?:\/\/steamcommunity\.com\/openid\/id\/(7656119\d{10})$/.exec(claimed);
    if(!match||identity!==claimed) throw new Error();
    const signed=new Set(single('openid.signed').split(','));
    for(const field of ['op_endpoint','claimed_id','identity','return_to','response_nonce','assoc_handle']) if(!signed.has(field)) throw new Error();
    const nonce=single('openid.response_nonce'),time=Date.parse(nonce.slice(0,20));
    if(!Number.isFinite(time)||Math.abs(now()-time)>300000) throw new Error();
    const verification=new URLSearchParams();
    for(const [key,value] of params) if(key.startsWith('openid.')) {single(key);verification.set(key,value);}
    verification.set('openid.mode','check_authentication');
    const response=await fetchImpl('https://steamcommunity.com/openid/login',{method:'POST',body:verification,redirect:'error',
      signal:AbortSignal.timeout(15000),headers:{'content-type':'application/x-www-form-urlencoded'}});
    if(!response.ok||!(await response.text()).split(/\r?\n/).includes('is_valid:true')) throw new Error();
    return {steamId:match[1],nonceHash:await hash(nonce)};
  }
  const handle = async request=>{
    try {
      const url=new URL(request.url);
      // The gateway may strip /functions/v1 before forwarding to the isolate.
      const incoming=url.pathname.endsWith('/checkpoint-steam')?url.pathname+'/':url.pathname;
      const prefix=[base.pathname,'/checkpoint-steam/'].find(p=>incoming.startsWith(p));
      if(!prefix) throw new ApiError(404,'Ruta no encontrada.');
      const path='/'+incoming.slice(prefix.length),method=request.method;
      if(method==='GET'&&path==='/health') return json(200,{ok:true,version:'0.6.0',steamConfigured:Boolean(apiKey),libraryImportVersion:2});
      if(method==='GET'&&path==='/privacy') return page(
        'Independent application, not affiliated with Valve. Operator: Checkpoint. Contact: https://github.com/Nicolasmf05/Checkpoint/issues. Steam ID, visible games, playtime and achievements are processed on Supabase (Ireland). Login flows expire after 10 minutes, session hashes after 7 days, library and progress cache after 15 minutes and public achievement definitions after 24 hours. Expired rows are removed during subsequent requests. Unlinking revokes the session and clears its game cache. Passwords and plaintext session tokens are never stored in the database. Local data remains on your PC. Hosting logs/backups follow Supabase retention. Steam data availability and accuracy depend on Valve.',
        'Aplicación independiente, sin afiliación con Valve. Responsable: Checkpoint. Contacto: https://github.com/Nicolasmf05/Checkpoint/issues. SteamID, juegos visibles, horas y logros se procesan en Supabase (Irlanda). Vinculaciones: 10 minutos; hashes de sesiones: 7 días; caché de biblioteca y progreso: 15 minutos; definiciones públicas: 24 horas. Los registros caducados se eliminan en consultas posteriores. Desvincular revoca la sesión y elimina su caché de juegos. No almacenamos contraseñas ni tokens de sesión en texto claro. Los datos locales permanecen en tu PC. Registros y copias de seguridad siguen la retención de Supabase. Los datos dependen de Valve.');
      if(method==='GET'&&path==='/') return page('Steam connection service for the Checkpoint Windows widget.','Servicio de conexión Steam para el widget de Windows Checkpoint.');
      // A shared, durable limit cannot be bypassed by supplying a fake IP header.
      await limit('requests',2000);
      if(method==='POST'&&path==='/v1/auth/start') {
        if(!apiKey) throw new ApiError(503,'El responsable de esta edición todavía debe configurar el servicio de Steam.');
        await body(request); await limit('login',30);
        const flowId=secret(),pollSecret=secret(),returnTo=new URL('v1/auth/callback?flow='+flowId,base).href;
        await state('put','flow',await hash(flowId),{pollHash:await hash(pollSecret),returnTo});
        const authorize=new URL('https://steamcommunity.com/openid/login');
        for(const [key,value] of Object.entries({ns:'http://specs.openid.net/auth/2.0',mode:'checkid_setup',return_to:returnTo,
          realm:base.href,identity:'http://specs.openid.net/auth/2.0/identifier_select',claimed_id:'http://specs.openid.net/auth/2.0/identifier_select'})) authorize.searchParams.set('openid.'+key,value);
        return json(200,{flowId,pollSecret,authorizeUrl:authorize.href});
      }
      if(method==='GET'&&path==='/v1/auth/callback') {
        const id=url.searchParams.get('flow');
        if(url.searchParams.getAll('flow').length!==1||!validSecret(id)) throw new ApiError(400,'Vinculación no válida.');
        const flowHash=await hash(id),flow=await state('get','flow',flowHash);
        if(!flow||flow.steamId) throw new ApiError(400,'Esta vinculación ha caducado o ya fue utilizada.');
        if(url.searchParams.get('openid.mode')==='cancel') return page('Connection canceled. Close this tab and return to Checkpoint.','Vinculación cancelada. Cierra esta pestaña y vuelve a Checkpoint.');
        let assertion;
        try { assertion=await verify(url.searchParams,flow.returnTo); } catch { throw new ApiError(400,'Steam no ha validado esta vinculación. Vuelve a intentarlo desde el widget.'); }
        if(!await state('claim','flow',flowHash,assertion)) throw new ApiError(400,'Vinculación ya utilizada.');
        return page('Steam account linked. Close this tab and return to Checkpoint.','Cuenta de Steam vinculada. Cierra esta pestaña y vuelve a Checkpoint.');
      }
      if(method==='POST'&&path==='/v1/auth/poll') {
        const data=await body(request);
        if(!validSecret(data.flowId)||!validSecret(data.pollSecret)) throw new ApiError(400,'Vinculación no válida o caducada.');
        const token=secret();
        const result=await state('poll','flow',await hash(data.flowId),{pollHash:await hash(data.pollSecret),tokenHash:await hash(token)});
        if(!result) throw new ApiError(400,'Vinculación no válida o caducada.');
        return json(200,result.status==='complete'?{...result,token}:result);
      }
      if(method==='POST'&&path==='/v1/auth/logout') {
        const session=await authenticate(request); await body(request); await state('logout','session',session.id); return json(200,{ok:true});
      }
      if(method==='GET'&&path==='/v1/library') return json(200,await library((await authenticate(request)).steamId));
      const match=/^\/v1\/games\/([1-9]\d{0,9})\/achievements$/.exec(path);
      if(method==='GET'&&match) {
        const session=await authenticate(request),appId=Number(match[1]);
        if(appId>2147483647) throw new ApiError(400,'Juego no válido.');
        if(!(await library(session.steamId)).games.some(g=>g.appId===appId)) throw new ApiError(403,'Este juego no está en tu biblioteca visible de Steam.');
        const language=url.searchParams.get('lang')==='en'?'english':'spanish';
        const result=await cached(`achievements:${session.steamId}:${appId}:${language}:descriptions-v2`,async()=>{
          const schema=await cached(`schema:${appId}:${language}`,()=>steam('ISteamUserStats/GetSchemaForGame/v2/',{appid:appId,l:language}),86400);
          if(!schema.game||typeof schema.game!=='object') throw new ApiError(502,'Steam no ha devuelto la definición de logros de este juego.');
          const definitions=schema.game.availableGameStats?.achievements;
          if(!definitions?.length) return {achievements:[]};
          if(!Array.isArray(definitions)) throw new ApiError(502,'Steam ha devuelto una respuesta no válida.');
          const progress=await steam('ISteamUserStats/GetPlayerAchievements/v1/',{steamid:session.steamId,appid:appId,l:language});
          if(progress.playerstats?.success!==true||!Array.isArray(progress.playerstats.achievements)) throw new ApiError(403,'Steam no permite consultar estos logros. Revisa la privacidad de tus detalles de juegos.');
          const unlocks=new Map(progress.playerstats.achievements.map(item=>[item.apiname,item]));
          return {achievements:definitions.slice(0,10000).map(def=>{
            const item=unlocks.get(def.name),unlocked=item?.achieved===1;
            const date=unlocked&&Number.isFinite(item.unlocktime)&&item.unlocktime>0?new Date(item.unlocktime*1000):null;
            const text=value=>typeof value==='string'&&value.trim()?value:''; return {id:def.name,name:text(def.displayName)||text(item?.name)||def.name,description:text(def.description)||text(item?.description),hidden:Boolean(Number(def.hidden)),
              unlocked,unlockedAt:date&&!isNaN(date.valueOf())?date.toISOString():null};
          })};
        }); return json(200,result);
      }
      throw new ApiError(404,'Ruta no encontrada.');
    } catch(error) { return json(error instanceof ApiError?error.status:503,{error:error instanceof ApiError?error.message:'El servicio de Steam todavía no está configurado o no está disponible.'}); }
  };
  // Browser tokens stay on the client. CORS permits this owner's Pages origin only;
  // authentication, privacy gates and rate limits remain in the same handler.
  return async request=>{
    const origin=request.headers.get('origin');
    const allowed=origin==='https://nicolasmf05.github.io';
    if(request.method==='OPTIONS') return new Response(null,{status:allowed?204:403,headers:allowed?{
      'access-control-allow-origin':origin,'access-control-allow-methods':'GET, POST, OPTIONS',
      'access-control-allow-headers':'authorization, content-type, apikey','access-control-max-age':'600','vary':'Origin'
    }:{'vary':'Origin'}});
    const response=await handle(request);
    if(!allowed) return response;
    const outgoing=new Headers(response.headers); outgoing.set('access-control-allow-origin',origin); outgoing.set('vary','Origin');
    return new Response(response.body,{status:response.status,headers:outgoing});
  };
}

if(typeof Deno!=='undefined') {
  const origin=Deno.env.get('SUPABASE_URL');
  const serviceKey=Deno.env.get('SUPABASE_SERVICE_ROLE_KEY');
  const rpc=async args=>{
    const response=await fetch(origin+'/rest/v1/rpc/cp_steam_state',{method:'POST',redirect:'error',signal:AbortSignal.timeout(10000),
      headers:{apikey:serviceKey,authorization:'Bearer '+serviceKey,'content-type':'application/json'},body:JSON.stringify(args)});
    if(!response.ok) throw new Error('Steam state unavailable'); return await response.json();
  };
  Deno.serve(createSteamHandler({baseUrl:origin+'/functions/v1/checkpoint-steam/',apiKey:Deno.env.get('STEAM_WEB_API_KEY')||'',rpc}));
}
