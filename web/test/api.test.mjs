// Comprueba que los fallos de Steam retienen su causa sin confundirlos con la sesión de Checkpoint.
import test from 'node:test';
import assert from 'node:assert/strict';
import { BrowserApi, RemoteError } from '../api.mjs';

test('Steam errors retain backend details for localized display', async () => {
  const detail =
    'Steam no permite consultar estos logros. Revisa la privacidad de tus detalles de juegos.';
  const api = new BrowserApi({ url: 'https://fixture.supabase.co' }, async () =>
    Response.json({ error: detail }, { status: 403 }),
  );
  await assert.rejects(
    api.steamRequest('v1/games/620/achievements'),
    (error) =>
      error instanceof RemoteError && error.code === 'forbidden' && error.detail === detail,
  );
});

test('expired Steam sessions request Steam relinking independently of Checkpoint auth', async () => {
  const api = new BrowserApi({ url: 'https://fixture.supabase.co' }, async () =>
    Response.json({}, { status: 401 }),
  );
  await assert.rejects(api.steamRequest('v1/library'), { code: 'steam-unauthorized' });
  await assert.rejects(api.request('rest/v1/fixture'), { code: 'unauthorized' });
});

test('malformed or oversized remote error details stay out of the UI error contract', () => {
  for (const detail of [null, {}, ['unexpected'], 'x'.repeat(300)])
    assert.equal(new RemoteError('remote', detail).detail, '');
});
