import http from 'node:http';
import { randomSecret, digest, secretMatches, validateOrigin, verifySteamAssertion, escapeHtml } from './security.mjs';

class ApiError extends Error { constructor(status, message) { super(message); this.status = status; } }
export function createService({ apiKey = '', publicUrl = 'http://127.0.0.1:34871', fetchImpl = fetch,
    now = Date.now, operator = 'Checkpoint', country = 'Sin configurar', contact = 'Sin configurar' } = {}) {
  const origin = validateOrigin(publicUrl);
  const flows = new Map(), sessions = new Map(), nonces = new Map(), cache = new Map(), limits = new Map();
  let steamCalls = 0, steamDay = '';
  function clean() {
    const timestamp = now();
    for (const map of [flows, sessions, cache, limits]) for (const [key, entry] of map) if (entry.expires <= timestamp) map.delete(key);
    for (const [nonce, expires] of nonces) if (expires <= timestamp) nonces.delete(nonce);
  }
  function limit(key, max, duration = 60_000) {
    let bucket = limits.get(key);
    if (!bucket || bucket.expires <= now()) { bucket = { count: 0, expires: now() + duration }; limits.set(key, bucket); }
    if (++bucket.count > max) throw new ApiError(429, 'Demasiadas consultas. Espera un minuto y vuelve a intentarlo.');
  }
  async function body(req) {
    if (!req.headers['content-type']?.startsWith('application/json')) throw new ApiError(415, 'Usa JSON.');
    const chunks = []; let bytes = 0;
    for await (const chunk of req) { bytes += chunk.length; if (bytes > 4096) throw new ApiError(413, 'Petición demasiado grande.'); chunks.push(chunk); }
    try { const data = JSON.parse(Buffer.concat(chunks).toString()); if (!data || typeof data !== 'object' || Array.isArray(data)) throw new Error(); return data; } catch { throw new ApiError(400, 'JSON no válido.'); }
  }
  function authenticate(req) {
    const authorization = req.headers.authorization;
    if (!authorization?.startsWith('Bearer ') || authorization.length > 200) throw new ApiError(401, 'Vuelve a vincular tu cuenta de Steam.');
    const id = digest(authorization.slice(7)); const session = sessions.get(id);
    if (!session || session.expires <= now()) throw new ApiError(401, 'La sesión ha caducado. Vuelve a vincular Steam.');
    limit('session:' + id, 90); return { ...session, id };
  }
  async function steam(path, params) {
    if (!apiKey) throw new ApiError(503, 'El responsable de esta edición todavía debe configurar el servicio de Steam.');
    const day = new Date(now()).toISOString().slice(0, 10);
    if (day !== steamDay) { steamDay = day; steamCalls = 0; }
    if (++steamCalls > 90000) throw new ApiError(503, 'Se ha alcanzado el límite diario. Inténtalo mañana.');
    const url = new URL(path, 'https://api.steampowered.com/');
    for (const [key, value] of Object.entries(params)) url.searchParams.set(key, String(value));
    let response;
    try { response = await fetchImpl(url, { headers: { 'x-webapi-key': apiKey }, redirect: 'error', signal: AbortSignal.timeout(20_000) }); }
    catch { throw new ApiError(502, 'Steam no responde. Se conserva tu último progreso.'); }
    if (!response.ok) throw new ApiError(502, 'Steam no ha devuelto estos datos. Comprueba la privacidad de tus detalles de juegos.');
    try { return await response.json(); } catch { throw new ApiError(502, 'Steam ha devuelto una respuesta no válida.'); }
  }
  async function cached(key, produce, ttl = 15 * 60_000) {
    const old = cache.get(key); if (old && old.expires > now()) return old.value;
    if (cache.size > 20000) clean();
    if (cache.size > 20000) throw new ApiError(503, 'Servicio ocupado. Inténtalo más tarde.');
    const value = produce(); cache.set(key, { value, expires: now() + ttl });
    try { return await value; } catch (error) { cache.delete(key); throw error; }
  }
  async function library(steamId) {
    return cached('library:' + steamId, async () => {
      const payload = await steam('IPlayerService/GetOwnedGames/v1/', { steamid: steamId, include_appinfo: true, include_played_free_games: true });
      if (!Array.isArray(payload.response?.games)) {
        if (payload.response?.game_count === 0) return { games: [] };
        throw new ApiError(403, 'Steam no permite consultar tu biblioteca. Revisa la visibilidad de Detalles de juegos en Steam.');
      }
      return { games: payload.response.games.slice(0, 10000).map(game => ({ appId: game.appid, name: game.name, playtimeMinutes: game.playtime_forever ?? 0 })) };
    });
  }
  function page(res, title, text) {
    const english = {
      'Checkpoint': ['Checkpoint', 'One game at a time. Steam connection service for the Windows widget. Independent application, not affiliated with Valve. Steam data is provided as-is; availability and accuracy depend on Valve.'],
      'Privacidad': ['Privacy', `Operator: ${operator}. Service and cache hosting: ${country}. Contact: ${contact}. Linking Steam requests your authenticated ID, visible library, playtime and achievements. We do not receive passwords. Sessions remain in memory for up to seven days, queried data for fifteen minutes; restarting clears sessions and unlinking revokes them. Your library and images also remain on your PC until deleted. No advertising or analytics. Consult your hosting provider's connection-log policy. Steam data is provided as-is, without guarantees of accuracy, availability or continuity; liability is limited to the extent permitted by law.`],
      'Vinculación cancelada': ['Connection canceled', 'You can close this tab and return to the widget.'],
      'Cuenta vinculada': ['Account linked', 'You can close this tab and return to Checkpoint. The app will import your available library.']
    }[title] ?? [title, text];
    res.writeHead(200, { 'content-type': 'text/html; charset=utf-8' });
    res.end(`<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>${escapeHtml(english[0])} · Checkpoint</title><style>body{font:16px system-ui;background:#111827;color:#edf5fb;max-width:640px;margin:8vh auto;padding:24px;line-height:1.7}h1,h2{color:#8cebc6}a{color:#8cebc6}</style><section lang="en"><h1>${escapeHtml(english[0])}</h1><p>${escapeHtml(english[1])}</p></section><section lang="es"><h2>${escapeHtml(title)}</h2><p>${escapeHtml(text)}</p></section><p><a href="/privacy">Privacy / Privacidad</a> · <a href="https://store.steampowered.com/">Steam</a></p></html>`);
  }
  const server = http.createServer(async (req, res) => {
    res.setHeader('cache-control', 'no-store'); res.setHeader('x-content-type-options', 'nosniff');
    res.setHeader('referrer-policy', 'no-referrer');
    res.setHeader('content-security-policy', "default-src 'none'; style-src 'unsafe-inline'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'");
    if (origin.startsWith('https:')) res.setHeader('strict-transport-security', 'max-age=31536000');
    const json = (status, data) => { res.writeHead(status, { 'content-type': 'application/json; charset=utf-8' }); res.end(JSON.stringify(data)); };
    try {
      const url = new URL(req.url, 'http://localhost'); const method = req.method; clean();
      limit('ip:' + req.socket.remoteAddress, 250);
      if (method === 'GET' && url.pathname === '/health') return json(200, { ok: true, version: '0.3.0', steamConfigured: Boolean(apiKey) });
      if (method === 'GET' && url.pathname === '/') return page(res, 'Checkpoint', 'Un juego cada vez. Servicio de conexión con Steam para el widget de Windows. Aplicación independiente, sin afiliación con Valve. Los datos de Steam se ofrecen tal como están y su disponibilidad depende de Valve, sin garantía de continuidad o exactitud.');
      if (method === 'GET' && url.pathname === '/privacy') return page(res, 'Privacidad', `Responsable: ${operator}. Alojamiento del servicio y su caché: ${country}. Contacto: ${contact}. Al vincular Steam solicitas la consulta de tu identificador, biblioteca, horas y logros visibles. No recibimos contraseñas. Guardamos sesiones en memoria hasta siete días y datos consultados hasta quince minutos. Reiniciar el servicio borra esas sesiones. Al desvincular se revoca la sesión. La biblioteca y las imágenes se almacenan también en tu PC, en el país donde lo utilizas, hasta que las elimines. No usamos publicidad ni analítica. Consulta la política del proveedor de alojamiento respecto a sus registros de conexión. Los datos de Steam se proporcionan tal como están, sin garantía de exactitud, disponibilidad o continuidad; en la medida permitida por la ley, no asumimos responsabilidad por daños derivados de estos datos.`);
      if (method === 'POST' && url.pathname === '/v1/auth/start') {
        if (!apiKey) throw new ApiError(503, 'El responsable de esta edición todavía debe configurar el servicio de Steam.');
        limit('login:' + req.socket.remoteAddress, 10);
        await body(req); if (flows.size >= 1000) throw new ApiError(503, 'Demasiadas vinculaciones en curso.');
        const flowId = randomSecret(), pollSecret = randomSecret(), returnTo = origin + '/v1/auth/callback?flow=' + flowId;
        flows.set(flowId, { pollHash: digest(pollSecret), returnTo, expires: now() + 10 * 60_000, claimed: false });
        const authorize = new URL('https://steamcommunity.com/openid/login');
        for (const [key, value] of Object.entries({ ns: 'http://specs.openid.net/auth/2.0', mode: 'checkid_setup',
            return_to: returnTo, realm: origin + '/', identity: 'http://specs.openid.net/auth/2.0/identifier_select', claimed_id: 'http://specs.openid.net/auth/2.0/identifier_select' })) authorize.searchParams.set('openid.' + key, value);
        return json(200, { flowId, pollSecret, authorizeUrl: authorize.href });
      }
      if (method === 'GET' && url.pathname === '/v1/auth/callback') {
        if (url.searchParams.getAll('flow').length !== 1) throw new ApiError(400, 'Vinculación no válida.');
        const flow = flows.get(url.searchParams.get('flow'));
        if (!flow || flow.expires <= now() || flow.claimed) throw new ApiError(400, 'Esta vinculación ha caducado o ya fue utilizada.');
        if (url.searchParams.get('openid.mode') === 'cancel') return page(res, 'Vinculación cancelada', 'Puedes cerrar esta pestaña y volver al widget.');
        let steamId;
        try { steamId = await verifySteamAssertion(url.searchParams, flow.returnTo, fetchImpl, nonces, now()); }
        catch { throw new ApiError(400, 'Steam no ha validado esta vinculación. Vuelve a intentarlo desde el widget.'); }
        if (flow.claimed) throw new ApiError(400, 'Vinculación ya utilizada.');
        flow.steamId = steamId; flow.claimed = true;
        return page(res, 'Cuenta vinculada', 'Ya puedes cerrar esta pestaña y volver a Checkpoint. La app importará tu biblioteca disponible.');
      }
      if (method === 'POST' && url.pathname === '/v1/auth/poll') {
        const data = await body(req); const flow = flows.get(data.flowId);
        if (!flow || flow.expires <= now() || !secretMatches(data.pollSecret, flow.pollHash)) throw new ApiError(400, 'Vinculación no válida o caducada.');
        if (!flow.steamId) return json(200, { status: 'pending' });
        if (sessions.size >= 5000) throw new ApiError(503, 'Servicio ocupado.');
        const token = randomSecret(); sessions.set(digest(token), { steamId: flow.steamId, expires: now() + 7 * 24 * 60 * 60_000 });
        flows.delete(data.flowId); return json(200, { status: 'complete', token, steamId: flow.steamId });
      }
      if (method === 'POST' && url.pathname === '/v1/auth/logout') {
        const session = authenticate(req); await body(req); sessions.delete(session.id);
        cache.delete('library:' + session.steamId);
        for (const key of cache.keys()) if (key.startsWith('achievements:' + session.steamId + ':')) cache.delete(key);
        return json(200, { ok: true });
      }
      if (method === 'GET' && url.pathname === '/v1/library') return json(200, await library(authenticate(req).steamId));
      const match = /^\/v1\/games\/([1-9]\d{0,9})\/achievements$/.exec(url.pathname);
      if (method === 'GET' && match) {
        const session = authenticate(req), appId = Number(match[1]);
        if (appId > 2147483647) throw new ApiError(400, 'Juego no válido.');
        const owned = await library(session.steamId);
        if (!owned.games.some(game => game.appId === appId)) throw new ApiError(403, 'Este juego no está en tu biblioteca visible de Steam.');
        const result = await cached(`achievements:${session.steamId}:${appId}`, async () => {
          const schema = await cached('schema:' + appId, () => steam('ISteamUserStats/GetSchemaForGame/v2/', { appid: appId, l: 'spanish' }), 24 * 60 * 60_000);
          if (!schema.game || typeof schema.game !== 'object') throw new ApiError(502, 'Steam no ha devuelto la definición de logros de este juego.');
          const definitions = schema.game.availableGameStats?.achievements;
          if (!definitions?.length) return { achievements: [] };
          const progress = await steam('ISteamUserStats/GetPlayerAchievements/v1/', { steamid: session.steamId, appid: appId, l: 'spanish' });
          if (progress.playerstats?.success !== true || !Array.isArray(progress.playerstats.achievements))
            throw new ApiError(403, 'Steam no permite consultar estos logros. Revisa la privacidad de tus detalles de juegos.');
          const unlocks = new Map(progress.playerstats.achievements.map(item => [item.apiname, item]));
          return { achievements: definitions.map(definition => {
            const item = unlocks.get(definition.name);
            return { id: definition.name, name: definition.displayName || definition.name, description: definition.description || '',
              hidden: Boolean(Number(definition.hidden)), unlocked: item?.achieved === 1,
              unlockedAt: item?.achieved === 1 && item.unlocktime > 0 ? new Date(item.unlocktime * 1000).toISOString() : null };
          }) };
        }); return json(200, result);
      }
      throw new ApiError(404, 'Ruta no encontrada.');
    } catch (error) {
      if (res.headersSent) { res.end(); return; }
      json(error instanceof ApiError ? error.status : 500, { error: error instanceof ApiError ? error.message : 'No se pudo completar la operación.' });
    }
  });
  server.requestTimeout = 30_000; server.headersTimeout = 10_000;
  const cleanup = setInterval(clean, 60_000); cleanup.unref();
  server.on('close', () => clearInterval(cleanup));
  return server;
}
