// Unifica logros de proveedores y limita el ritmo del repaso de biblioteca.
// La señal de cancelación interrumpe tanto las esperas como el trabajo pendiente.

// The explicit settings review includes imported, untracked and private games.
// La clave incluye el proveedor para que dos logros con el mismo id no se mezclen.
export function reviewItems(game) {
  const removed = new Set(game.removedAchievements || []),
    overrides = game.achievementOverrides || {};
  return [
    ['steam', game.achievements],
    ['retro', game.retroAchievements],
    ['manual', game.manualAchievements],
  ]
    .flatMap(([provider, items]) =>
      (items || []).map((a) => {
        const key = provider + ':' + a.id;
        return {
          ...a,
          key,
          provider,
          sourceUnlocked: a.unlocked === true,
          unlocked: Object.hasOwn(overrides, key) ? overrides[key] : a.unlocked === true,
        };
      }),
    )
    .filter((a) => !removed.has(a.key));
}

// Reject incomplete responses before touching the saved provider progress.
export function validateAchievements(items) {
  if (!Array.isArray(items) || items.length > 10000) throw new Error('invalid-achievements');
  const ids = new Set();
  return items.map((item) => {
    if (
      !item ||
      typeof item.id !== 'string' ||
      !item.id.trim() ||
      item.id.length > 250 ||
      ids.has(item.id) ||
      typeof item.name !== 'string' ||
      !item.name.trim() ||
      item.name.length > 250 ||
      typeof item.description !== 'string' ||
      item.description.length > 2000 ||
      typeof item.unlocked !== 'boolean' ||
      typeof item.hidden !== 'boolean' ||
      (item.unlockedAt != null &&
        (typeof item.unlockedAt !== 'string' || !Number.isFinite(Date.parse(item.unlockedAt))))
    )
      throw new Error('invalid-achievements');
    ids.add(item.id);
    return {
      id: item.id,
      name: item.name,
      description: item.description,
      unlocked: item.unlocked,
      hidden: item.hidden,
      unlockedAt: item.unlockedAt ?? null,
    };
  });
}

// Snapshot the whole IndexedDB library only once per batch or time window.
export function createAchievementSaver(save, { size = 8, interval = 2000 } = {}) {
  let dirty = 0,
    timer,
    queue = Promise.resolve(),
    failure;
  function flush() {
    clearTimeout(timer);
    timer = undefined;
    if (dirty) {
      dirty = 0;
      queue = queue.then(save);
      queue.catch((error) => {
        failure = error;
      });
    }
    return queue;
  }
  return {
    async changed() {
      if (failure) throw failure;
      dirty++;
      if (dirty >= size) await flush();
      else if (!timer)
        timer = setTimeout(() => {
          flush().catch(() => {});
        }, interval);
    },
    flush,
  };
}

// Three requests at most, and no more than 80 starts/minute for a full review.
// El turno de inicio se serializa; las consultas activas pueden solaparse hasta la concurrencia máxima.
export async function runAchievementReview(
  ids,
  process,
  signal,
  { spacing = 750, concurrency = 3 } = {},
) {
  if (!Number.isInteger(concurrency) || concurrency < 1 || concurrency > 3 || spacing < 0)
    throw new Error('invalid-review-options');
  const stop = new AbortController();
  signal = AbortSignal.any([signal, stop.signal]);
  let failure;
  let cursor = 0,
    nextStart = 0,
    gate = Promise.resolve();
  const pause = (ms) =>
    new Promise((resolve, reject) => {
      signal.throwIfAborted();
      const abort = () => {
        clearTimeout(timer);
        reject(signal.reason);
      };
      const timer = setTimeout(() => {
        signal.removeEventListener('abort', abort);
        resolve();
      }, ms);
      signal.addEventListener('abort', abort, { once: true });
    });
  async function worker() {
    while (cursor < ids.length) {
      signal.throwIfAborted();
      const id = ids[cursor++];
      const turn = gate.then(async () => {
        signal.throwIfAborted();
        const wait = Math.max(0, nextStart - performance.now());
        if (wait) await pause(wait);
        nextStart = performance.now() + spacing;
      });
      gate = turn.catch(() => {});
      await turn;
      signal.throwIfAborted();
      try {
        await process(id, signal);
      } catch (error) {
        failure ||= error;
        stop.abort(error);
        throw error;
      }
    }
  }
  const results = await Promise.allSettled(
    Array.from({ length: Math.min(concurrency, ids.length) }, worker),
  );
  const error = results.find((r) => r.status === 'rejected');
  if (error) throw failure || error.reason;
}
