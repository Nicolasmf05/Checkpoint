import { createHash, randomBytes, timingSafeEqual } from 'node:crypto';

export const randomSecret = () => randomBytes(32).toString('base64url');
export const digest = value => createHash('sha256').update(value).digest('hex');
export function secretMatches(candidate, expectedHash) {
  return typeof candidate === 'string' && candidate.length < 200 &&
    timingSafeEqual(Buffer.from(digest(candidate)), Buffer.from(expectedHash));
}
export function validateOrigin(value) {
  const url = new URL(value);
  if (url.username || url.password || url.search || url.hash || url.pathname !== '/' ||
      !(url.protocol === 'https:' || (url.protocol === 'http:' && ['localhost', '127.0.0.1', '[::1]'].includes(url.hostname))))
    throw new Error('PUBLIC_URL debe ser un origen HTTPS, o HTTP local para desarrollo.');
  return url.origin;
}
export function escapeHtml(value) {
  return String(value).replace(/[&<>"']/g, char => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[char]));
}

export async function verifySteamAssertion(params, expectedReturnTo, fetchImpl, usedNonces, now = Date.now()) {
  const single = name => { if (params.getAll(name).length !== 1) throw new Error('Aserción no válida.'); return params.get(name); };
  if (single('openid.mode') !== 'id_res' || single('openid.ns') !== 'http://specs.openid.net/auth/2.0' ||
      single('openid.op_endpoint') !== 'https://steamcommunity.com/openid/login' || single('openid.return_to') !== expectedReturnTo)
    throw new Error('Aserción no válida.');
  const claimed = single('openid.claimed_id'); const identity = single('openid.identity');
  const match = /^https?:\/\/steamcommunity\.com\/openid\/id\/(7656119\d{10})$/.exec(claimed);
  if (!match || identity !== claimed) throw new Error('Identidad de Steam no válida.');
  const signed = new Set(single('openid.signed').split(','));
  for (const field of ['op_endpoint', 'claimed_id', 'identity', 'return_to', 'response_nonce', 'assoc_handle'])
    if (!signed.has(field)) throw new Error('Faltan campos firmados.');
  const nonce = single('openid.response_nonce'); const time = Date.parse(nonce.slice(0, 20));
  if (!Number.isFinite(time) || Math.abs(now - time) > 5 * 60_000 || usedNonces.has(nonce)) throw new Error('Respuesta caducada o repetida.');
  const verification = new URLSearchParams();
  for (const [key, value] of params) if (key.startsWith('openid.')) {
    if (params.getAll(key).length !== 1) throw new Error('Campos duplicados.'); verification.set(key, value);
  }
  verification.set('openid.mode', 'check_authentication');
  const response = await fetchImpl('https://steamcommunity.com/openid/login', {
    method: 'POST', body: verification, redirect: 'error', signal: AbortSignal.timeout(15_000),
    headers: { 'content-type': 'application/x-www-form-urlencoded' }
  });
  if (!response.ok || !(await response.text()).split(/\r?\n/).includes('is_valid:true')) throw new Error('Steam no ha validado el inicio de sesión.');
  // Recheck after await: simultaneous requests cannot reuse the same assertion.
  if (usedNonces.has(nonce)) throw new Error('Respuesta repetida.');
  usedNonces.set(nonce, now + 10 * 60_000); return match[1];
}
