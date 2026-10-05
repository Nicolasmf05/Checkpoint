import test from 'node:test';
import assert from 'node:assert/strict';
import { BrowserApi } from '../api.mjs';
import { validateAchievements, createAchievementSaver, runAchievementReview } from '../review.mjs';
const config = { url: 'https://fixture.supabase.co' };
const sample = { id: 'ONE', name: 'One', description: 'Details', hidden: false, unlocked: true };

test('Steam reads retry transient failures once, without retrying writes or session failures', async () => {
  for (const [status, retries] of [
    [502, 2],
    [504, 2],
    [401, 1],
    [403, 1],
    [429, 1],
    [503, 1],
  ]) {
    let calls = 0;
    const api = new BrowserApi(config, async () => {
      calls++;
      return Response.json({}, { status });
    });
    api.retryDelay = () => Promise.resolve();
    await assert.rejects(api.steamRequest('v1/games/620/achievements'));
    assert.equal(calls, retries, String(status));
  }
  let calls = 0;
  const api = new BrowserApi(config, async () => {
    calls++;
    throw new TypeError('network');
  });
  api.retryDelay = () => Promise.resolve();
  await assert.rejects(api.steamRequest('v1/auth/start', {}, false), { code: 'offline' });
  assert.equal(calls, 1);
  await assert.rejects(api.steamRequest('v1/library'), { code: 'offline', stopsBatch: true });
  assert.equal(calls, 3);
});

test('rate limiting enforces cooldown across games without new requests', async () => {
  let calls = 0;
  const api = new BrowserApi(config, async () => {
    calls++;
    return Response.json({}, { status: 429, headers: { 'retry-after': '120' } });
  });
  await assert.rejects(api.steamRequest('v1/games/1/achievements'), {
    status: 429,
    stopsBatch: true,
  });
  await assert.rejects(api.steamRequest('v1/games/2/achievements'), { code: 'rate' });
  assert.equal(calls, 1);
  assert.ok(api.steamRetryAfter - Date.now() > 110000);
});

test('duplicate reads share a request and one cancelled consumer does not cancel another', async () => {
  let calls = 0,
    release,
    underlyingSignal;
  const api = new BrowserApi(config, async (_, options) => {
    calls++;
    underlyingSignal = options.signal;
    await new Promise((resolve) => {
      release = resolve;
    });
    return Response.json({ achievements: [sample] });
  });
  const a = new AbortController(),
    b = new AbortController();
  const first = api.steamRequest('v1/games/620/achievements', undefined, true, a.signal);
  const second = api.steamRequest('v1/games/620/achievements', undefined, true, b.signal);
  a.abort();
  await assert.rejects(first, { name: 'AbortError' });
  assert.equal(underlyingSignal.aborted, false);
  release();
  assert.deepEqual((await second).achievements, [sample]);
  assert.equal(calls, 1);
  assert.equal(api.steamRequests.size, 0);
});

test('cancelling every consumer stops the underlying request and permits later retries', async () => {
  let calls = 0,
    cancelled = 0;
  const api = new BrowserApi(config, async (_, options) => {
    if (++calls > 1) return Response.json({ achievements: [] });
    await new Promise((_, reject) =>
      options.signal.addEventListener(
        'abort',
        () => {
          cancelled++;
          reject(options.signal.reason);
        },
        { once: true },
      ),
    );
  });
  const stop = new AbortController();
  const first = api.steamRequest('v1/games/1/achievements', undefined, true, stop.signal);
  stop.abort();
  await assert.rejects(first);
  assert.equal(cancelled, 1);
  assert.deepEqual(await api.steamRequest('v1/games/1/achievements'), { achievements: [] });
  assert.equal(calls, 2);
});

test('provider validation distinguishes empty achievements from incomplete or corrupt data', () => {
  assert.deepEqual(validateAchievements([]), []);
  assert.equal(validateAchievements([sample])[0].unlocked, true);
  for (const items of [
    null,
    undefined,
    {},
    [sample, sample],
    [{ ...sample, name: '' }],
    [{ ...sample, unlocked: 1 }],
    [{ ...sample, description: null }],
    [{ ...sample, unlockedAt: 'not a date' }],
  ])
    assert.throws(() => validateAchievements(items));
});

test('achievement snapshots batch changes and flush partial progress on completion or stop', async () => {
  let writes = 0;
  const saver = createAchievementSaver(
    async () => {
      writes++;
    },
    { size: 8, interval: 10000 },
  );
  for (let i = 0; i < 23; i++) await saver.changed();
  assert.equal(writes, 2);
  await saver.flush();
  assert.equal(writes, 3);
  await saver.flush();
  assert.equal(writes, 3);
});

test('achievement batches persist slow partial results and surface storage failures', async () => {
  let writes = 0;
  const saver = createAchievementSaver(
    async () => {
      writes++;
    },
    { interval: 15 },
  );
  await saver.changed();
  await new Promise((resolve) => setTimeout(resolve, 35));
  await saver.flush();
  assert.equal(writes, 1);
  const broken = createAchievementSaver(
    async () => {
      throw new Error('storage');
    },
    { size: 1 },
  );
  await assert.rejects(broken.changed(), { message: 'storage' });
  await assert.rejects(broken.flush(), { message: 'storage' });
});

test('fatal batch errors stop queued games, cancel in-flight work and retain their cause', async () => {
  let started = 0,
    drained = 0;
  const failure = new Error('session-expired');
  await assert.rejects(
    runAchievementReview(
      Array.from({ length: 30 }, (_, i) => i),
      async (id, signal) => {
        started++;
        try {
          if (id === 0) {
            await new Promise((resolve) => setTimeout(resolve, 15));
            throw failure;
          }
          await new Promise((_, reject) =>
            signal.addEventListener('abort', () => reject(signal.reason), { once: true }),
          );
        } finally {
          drained++;
        }
      },
      new AbortController().signal,
      { spacing: 0 },
    ),
    (error) => error === failure,
  );
  assert.equal(started, 3);
  assert.equal(drained, 3);
});
