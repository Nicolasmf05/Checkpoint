// Regresión de navegador con servidor local y respuestas remotas simuladas.
// Valida flujos reales, adaptación de pantalla, persistencia y navegación, y guarda evidencias.

import { createRequire } from 'node:module';
import http from 'node:http';
import path from 'node:path';
import { readFile, mkdir, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import assert from 'node:assert/strict';
const require = createRequire(import.meta.url),
  { chromium } = require('playwright');
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..'),
  site = path.join(root, 'dist/web'),
  evidence = path.join(root, '.qa/web-browser');
await mkdir(evidence, { recursive: true });
const server = http.createServer(async (req, res) => {
  try {
    const name =
        decodeURIComponent(new URL(req.url, 'http://localhost').pathname).replace(
          /^\/Checkpoint\//,
          '',
        ) || 'index.html',
      target = path.resolve(site, name);
    if (!target.startsWith(site + path.sep)) throw new Error();
    const mime = {
      '.html': 'text/html',
      '.js': 'text/javascript',
      '.mjs': 'text/javascript',
      '.css': 'text/css',
      '.json': 'application/json',
      '.svg': 'image/svg+xml',
      '.webmanifest': 'application/manifest+json',
    };
    res.writeHead(200, {
      'content-type': mime[path.extname(target)] || 'application/octet-stream',
    });
    res.end(await readFile(target));
  } catch {
    res.writeHead(404);
    res.end();
  }
});
const testPort = Number(process.env.CHECKPOINT_TEST_PORT || 4173);
await new Promise((resolve) => server.listen(testPort, '127.0.0.1', resolve));
const browser = await chromium.launch({
    headless: true,
    ...(process.env.CHECKPOINT_BROWSER_CHANNEL
      ? { channel: process.env.CHECKPOINT_BROWSER_CHANNEL }
      : {}),
  }),
  context = await browser.newContext({
    viewport: { width: 1280, height: 900 },
    serviceWorkers: 'block',
  }),
  page = await context.newPage();
const errors = [],
  checks = [],
  diagnostics = [];
page.on('console', (m) => {
  if (m.type() === 'error') diagnostics.push(m.text());
});
page.on('requestfailed', (r) => diagnostics.push(r.url() + ': ' + r.failure()?.errorText));
page.on('pageerror', (error) => errors.push(error.message));
const check = (pass, name) => {
  assert.ok(pass, name);
  checks.push(name);
  console.log('PASS ' + name);
};
const click = async (name) => page.getByRole('button', { name, exact: true }).first().click();
const input = async (name, value) => page.getByRole('textbox', { name, exact: true }).fill(value);
const theme = async (id) =>
  page.waitForFunction((id) => document.documentElement.dataset.theme === id, id);
const own = '11111111-1111-4111-8111-111111111111',
  other = '22222222-2222-4222-8222-222222222222',
  requestId = '33333333-3333-4333-8333-333333333333';
let coversEnabled = false,
  coverRequests = [],
  coverConfirmations = [],
  sharedCovers = new Map();
const coverPng = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAKAAAADwCAIAAAAII/iNAAACPklEQVR4nO3RAQkAIQDAQP2EhjCiAT+FCOMuwWDzrD3o+l4HcJfBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQbHGRxncJzBcQaPth/g7gNIuBbg8wAAAABJRU5ErkJggg==',
  'base64',
);
let libraryEmpty = false,
  steamFailure = null,
  libraryCalls = 0,
  achievementCalls = 0,
  authCalls = 0,
  inviteBody,
  ownPublications = [],
  published = [],
  operations = new Map();
await context.route('https://fumdnvvvoiwoiziwtmsu.supabase.co/**', async (route) => {
  const request = route.request(),
    url = new URL(request.url()),
    body = request.postDataJSON(),
    headers = {
      'access-control-allow-origin': '*',
      'access-control-allow-headers': 'content-type, authorization, apikey',
      'access-control-allow-methods': 'GET,POST,PATCH,DELETE,OPTIONS',
    };
  const respond = (data, status = 200) =>
    route.fulfill({
      status,
      headers,
      contentType: 'application/json',
      body: data === null ? '' : JSON.stringify(data),
    });
  if (request.method() === 'OPTIONS') return respond(null, 204);
  const coverKey = (g) =>
    g.steamAppId
      ? 'steam:' + g.steamAppId + '|' + g.title.toLowerCase()
      : g.title.toLowerCase() + '|' + g.platform.toLowerCase();
  if (url.pathname.includes('/checkpoint-covers/v1/shared')) {
    const candidate = sharedCovers.get(coverKey(body));
    return respond({
      candidate: candidate && !body.excluded.includes(candidate.imageId) ? candidate : null,
    });
  }
  if (url.pathname.includes('/checkpoint-covers/v1/confirm')) {
    coverConfirmations.push(body);
    if (body.title === 'IGDB cover fixture' && !sharedCovers.has(coverKey(body)))
      sharedCovers.set(coverKey(body), {
        id: 2,
        name: 'Second cover fixture',
        imageId: body.imageId,
        year: 2020,
        score: 0.95,
      });
    return respond({ shared: true });
  }
  if (url.pathname.includes('/checkpoint-covers/v1/search')) {
    if (
      !coversEnabled ||
      !['IGDB cover fixture', 'AA cover review', 'ZZ cover review'].includes(body.title)
    )
      return respond({ code: 'igdb-not-configured' }, 503);
    coverRequests.push(body);
    const next = body.excluded.includes('fixture_first');
    return respond({
      candidate: {
        id: next ? 2 : 1,
        name: next ? 'Second cover fixture' : 'Closest cover fixture',
        imageId: next ? 'fixture_second' : 'fixture_first',
        year: 2020,
        score: 0.95,
      },
    });
  }
  if (url.pathname.includes('/checkpoint-covers/v1/image/'))
    return route.fulfill({ status: 200, headers, contentType: 'image/png', body: coverPng });
  if (url.pathname.endsWith('/auth/v1/token')) {
    authCalls++;
    assert.equal(body.email, 'browser_tester@accounts.checkpoint.invalid');
    return respond({
      access_token: 'fixture-token',
      refresh_token: 'fixture-refresh',
      expires_in: 3600,
      user: { id: own },
    });
  }
  if (url.pathname.endsWith('/auth/v1/logout')) return respond(null, 204);
  if (url.pathname.endsWith('/cp_profiles'))
    return respond([
      { user_id: own, display_name: 'Fixture User', friend_code: 'cp-123456abcdef' },
      { user_id: other, display_name: 'Fixture Friend', friend_code: 'cp-abcdef123456' },
    ]);
  if (url.pathname.endsWith('/cp_friendships'))
    return respond([{ user_low: own, user_high: other }]);
  if (url.pathname.endsWith('/cp_friend_requests')) {
    if (request.method() === 'POST') {
      inviteBody = body;
      return respond(null, 201);
    }
    if (request.method() === 'PATCH') return respond(null, 204);
    return respond([{ id: requestId, sender_id: other, recipient_id: own, status: 'pending' }]);
  }
  if (url.pathname.endsWith('/cp_find_friend'))
    return respond([
      { user_id: other, display_name: 'Fixture Friend', friend_code: 'cp-abcdef123456' },
    ]);
  if (
    url.pathname.endsWith('/cp_groups') ||
    url.pathname.endsWith('/cp_group_invites') ||
    url.pathname.endsWith('/cp_group_members')
  )
    return respond([]);
  if (/\/rpc\/cp_(create_group|invite_group_friend)$/.test(url.pathname))
    return respond('fixture-group-id');
  if (
    /\/rpc\/cp_(answer_group_invite|update_group_goal|add_group_goal|remove_group_goal)$/.test(
      url.pathname,
    )
  )
    return respond(true);
  if (url.pathname.endsWith('/cp_game_publications'))
    return respond(
      url.searchParams.get('owner_id') === 'eq.' + own
        ? ownPublications
        : [
            {
              owner_id: other,
              game_id: requestId,
              revision: 1,
              is_shared: true,
              operation_payload: {
                title: 'Friend game',
                platform: 'PC',
                status: 'playing',
                goalKind: 'story',
                storyPercent: 60,
                tasksDone: 1,
                tasksTotal: 3,
                achievementsUnlocked: 7,
                achievementsTotal: 12,
                manualAchievementsJson: JSON.stringify([
                  {
                    name: 'Friend personal challenge',
                    description: 'Finish without damage',
                    completed: true,
                  },
                ]),
              },
            },
          ],
    );
  if (url.pathname.endsWith('/cp_publish_game')) {
    const old = ownPublications.find((p) => p.game_id === body.p_game_id);
    if (operations.has(body.p_operation_id)) return respond(operations.get(body.p_operation_id));
    if ((old?.revision || 0) !== body.p_expected_revision) return respond({ code: '40001' }, 409);
    const revision = (old?.revision || 0) + 1,
      entry = {
        owner_id: own,
        game_id: body.p_game_id,
        revision,
        is_shared: body.p_game !== null,
        operation_payload: body.p_game,
      };
    ownPublications = ownPublications.filter((p) => p.game_id !== body.p_game_id);
    ownPublications.push(entry);
    published.push(body);
    operations.set(body.p_operation_id, revision);
    return respond(revision);
  }
  if (url.pathname.endsWith('/v1/auth/start'))
    return respond({
      flowId: 'F'.repeat(43),
      pollSecret: 'P'.repeat(43),
      authorizeUrl: 'https://steamcommunity.com/openid/login?fixture=1',
    });
  if (url.pathname.endsWith('/v1/auth/poll'))
    return respond({ status: 'complete', token: 'S'.repeat(43), steamId: '76561198000000000' });
  if (url.pathname.endsWith('/v1/library')) {
    libraryCalls++;
    return respond({
      games: libraryEmpty
        ? []
        : [
            { appId: 620, name: 'Portal 2', playtimeMinutes: 90 },
            { appId: 999, name: 'Borrowed family fixture', playtimeMinutes: 25 },
          ],
    });
  }
  if (url.pathname.endsWith('/achievements')) {
    achievementCalls++;
    if (steamFailure) return respond({ error: steamFailure }, 403);
    if (Number(url.pathname.split('/').at(-2)) >= 10000)
      await new Promise((resolve) =>
        setTimeout(resolve, Number(url.pathname.split('/').at(-2)) === 10000 ? 4000 : 1800),
      );
    return respond({
      achievements: [
        { id: 'FIRST', name: 'First', description: 'Visible', hidden: false, unlocked: true },
        {
          id: 'SECRET',
          name: 'Hidden secret name',
          description: 'Spoiler description',
          hidden: true,
          unlocked: false,
        },
      ],
    });
  }
  if (url.pathname.endsWith('/v1/auth/logout')) return respond({ ok: true });
  return respond({ error: 'unknown fixture path' }, 404);
});
await context.route('https://steamcommunity.com/**', (route) =>
  route.fulfill({ contentType: 'text/html', body: '<title>Steam fixture</title>' }),
);
await context.route('https://cdn.cloudflare.steamstatic.com/**', (route) => route.abort());
try {
  await page.goto(`http://127.0.0.1:${testPort}/Checkpoint/`);
  check(
    (await page.getByRole('link', { name: 'Abrir Checkpoint', exact: true }).isVisible()) &&
      (await page.locator('.window').count()) === 0 &&
      authCalls === 0,
    'home opens the presentation without starting the app or signing in',
  );
  await page.screenshot({ path: path.join(evidence, 'web-presentation-es.png') });
  await page.getByRole('button', { name: 'English', exact: true }).click();
  check(
    (await page.getByRole('link', { name: 'Open Checkpoint', exact: true }).isVisible()) &&
      (await page.getByText('Qué puedes hacer', { exact: true }).count()) === 0,
    'presentation switches entirely to English',
  );
  await page.screenshot({ path: path.join(evidence, 'web-presentation-en.png') });
  await page.getByRole('button', { name: 'Español', exact: true }).click();
  await page.getByRole('link', { name: 'Abrir Checkpoint', exact: true }).click();
  await page.locator('.window').waitFor();
  check(
    (await page.getByRole('button', { name: 'Añadir juego', exact: true }).count()) > 0,
    'browser application starts in Spanish without an account',
  );
  await click('Iniciar sesión');
  await page.getByRole('textbox', { name: 'Usuario de Checkpoint', exact: true }).waitFor();
  check(
    (await page
      .getByRole('textbox', { name: 'Nombre visible (para crear cuenta)', exact: true })
      .count()) === 0 &&
      (await page.getByRole('button', { name: 'Entrar', exact: true }).isVisible()),
    'Spanish sign-in opens the existing-account form without registration fields',
  );
  await page.screenshot({ path: path.join(evidence, 'web-login-es.png') });
  await click('Crear cuenta');
  await page
    .getByRole('textbox', { name: 'Nombre visible (para crear cuenta)', exact: true })
    .waitFor();
  check(
    (await page.getByRole('button', { name: 'Ya tengo cuenta', exact: true }).isVisible()) &&
      authCalls === 0,
    'registration requires an explicit switch and does not submit login',
  );
  await input('Contraseña de Checkpoint', 'fixture-switch-password');
  await click('Ya tengo cuenta');
  check(
    (await page
      .getByRole('textbox', { name: 'Nombre visible (para crear cuenta)', exact: true })
      .count()) === 0 &&
      (await page
        .getByRole('textbox', { name: 'Contraseña de Checkpoint', exact: true })
        .inputValue()) === '',
    'returning to Spanish sign-in clears the password and registration fields',
  );
  await click('Mi lista');
  await click('Añadir juego');
  await input('Nombre del juego', 'Browser fixture <img src=x>');
  await input('Notas privadas', 'Private fixture notes');
  await input(
    'Tareas: una por línea. Usa [x] para las terminadas.',
    '[x] Done task\n[ ] Next task',
  );
  await click('Guardar');
  await page.locator('.game').first().waitFor();
  check(
    (await page.locator('.game-title').textContent()).includes('<img') &&
      (await page.locator('.game img').count()) === 0,
    'game creation saves notes/tasks and treats titles as text',
  );
  await page.keyboard.press('F1');
  await page.locator('.shortcut-help[open]').waitFor();
  check(
    (await page.locator('.shortcut-help').textContent()).includes('aplicación de Windows'),
    'web help explains unsupported global gestures',
  );
  await page.keyboard.press('Escape');
  await page.reload();
  await page.locator('.game').first().waitFor();
  await page.locator('.game-tools button').nth(1).click();
  check(
    (await page.getByRole('textbox', { name: 'Notas privadas' }).inputValue()) ===
      'Private fixture notes',
    'private library survives browser reload',
  );
  await click('Cancelar');
  await click('Ajustes');
  await page.getByRole('combobox', { name: 'Tema', exact: true }).selectOption('3');
  await theme('ocean');
  await page.keyboard.press('Escape');
  await theme('dark');
  check(
    (await page.locator('.window').count()) === 1,
    'Escape cancels live theme preview and returns to the library',
  );
  await click('Ajustes');
  await page.getByRole('combobox', { name: 'Idioma' }).selectOption('1');
  await page.getByRole('combobox', { name: 'Tema', exact: true }).selectOption('3');
  await click('Guardar');
  await page.getByRole('button', { name: 'Settings', exact: true }).waitFor();
  check(
    (await page.locator('html').getAttribute('lang')) === 'en',
    'language switches the complete browser interface to English',
  );
  await click('Settings');
  const colors = new Set();
  for (const [i, id] of [
    'dark',
    'light',
    'midnight',
    'ocean',
    'forest',
    'plum',
    'amber',
    'contrast',
    'cyber-purple',
    'electric-blue',
    'neon-lime',
    'black-red',
    'black-orange',
    'synthwave',
    'blue-white',
    'purple-dark',
    'emerald-neutral',
    'black-white',
    'navy-cyan',
    'coral-cream',
    'orange-charcoal',
    'indigo-gray',
  ].entries()) {
    await page.getByRole('combobox', { name: 'Theme', exact: true }).selectOption(String(i));
    await theme(id);
    colors.add(
      await page
        .locator('.dialog-page')
        .evaluate(
          (n) =>
            getComputedStyle(n).backgroundColor +
            getComputedStyle(document.documentElement).getPropertyValue('--accent'),
        ),
    );
  }
  check(colors.size === 22, 'all 22 themes preview distinct palettes');
  await click('Cancel');
  await theme('ocean');
  check((await page.locator('.window-mode').count()) === 0, 'browser has no window size presets');
  for (const [width, height] of [
    [1440, 900],
    [800, 650],
    [390, 844],
    [320, 640],
    [1100, 550],
    [1100, 360],
  ]) {
    await page.setViewportSize({ width, height });
    check(
      await page.locator('.window').evaluate((n) => {
        const r = n.getBoundingClientRect();
        return (
          r.width >= innerWidth - 26 &&
          r.right <= innerWidth &&
          r.bottom <= innerHeight &&
          document.documentElement.scrollWidth <= innerWidth
        );
      }),
      'collection adapts to browser viewport ' + width + 'x' + height,
    );
    if (width === 1440 || width === 390)
      await page.screenshot({ path: path.join(evidence, 'web-responsive-' + width + '-en.png') });
  }
  await page.setViewportSize({ width: 1280, height: 900 });
  await click('Settings');
  check(
    (await page.getByRole('combobox', { name: 'Window mode', exact: true }).count()) === 0 &&
      (await page.getByRole('combobox', { name: 'Miniature text size', exact: true }).count()) ===
        0,
    'web settings omit desktop sizing controls',
  );
  await click('Cancel');
  await page.locator('.game-tools button').nth(1).click();
  check(
    (await page.getByRole('textbox', { name: 'Private notes' }).inputValue()) ===
      'Private fixture notes',
    'responsive collection opens the saved game editor',
  );
  await click('Cancel');
  for (let i = 0; i < 4; i++) {
    await page.keyboard.press('F6');
    await page.waitForFunction(() => window.checkpointState?.kind === 'main');
  }
  check(
    (await page.locator('.minirow').count()) === 0 &&
      !(await page.evaluate(() => window.checkpointState.mini)),
    'layout cycling never switches browser into a desktop window mode',
  );
  const downloadPromise = page.waitForEvent('download');
  await click('Export backup');
  const download = await downloadPromise,
    stream = await download.createReadStream();
  let raw = '';
  for await (const part of stream) raw += part;
  const backup = JSON.parse(raw);
  check(
    backup.games[0].notes === 'Private fixture notes' && backup.games[0].tasks[0].done,
    'export includes private data and completed tasks for offline recovery',
  );
  await writeFile(
    path.join(evidence, 'import.json'),
    JSON.stringify({
      games: [
        backup.games[0],
        {
          title: 'Imported Steam game',
          steamAppId: 620,
          status: 'Paused',
          notes: 'Steam preserves me',
        },
      ],
    }),
  );
  await click('Import backup');
  await page.locator('input[type=file]').setInputFiles(path.join(evidence, 'import.json'));
  await click('Import');
  await page.waitForFunction(
    () => window.checkpointState?.kind === 'main' && window.checkpointState.games?.length === 2,
  );
  check(
    (await page.locator('.game').count()) === 2,
    'JSON import deduplicates existing games and adds desktop-format games',
  );
  await page
    .locator('.game')
    .filter({ hasText: 'Imported Steam game' })
    .locator('.game-title')
    .click();
  check(
    (await page
      .getByRole('button', { name: 'Play', exact: true })
      .evaluate((n) => n.classList.contains('accent') && n.getBoundingClientRect().height >= 42)) &&
      (await page
        .locator('.game-actions')
        .getByRole('button', { name: 'View achievements', exact: true })
        .isVisible()),
    'Steam game sheet exposes Play and achievements above its details',
  );
  for (const width of [320, 390, 1280]) {
    await page.setViewportSize({ width, height: 900 });
    check(
      await page.locator('.game-actions').evaluate(
        (n) =>
          [...n.querySelectorAll('button')].every((b) => {
            const r = b.getBoundingClientRect();
            return r.left >= 0 && r.right <= innerWidth;
          }) && document.documentElement.scrollWidth <= innerWidth,
      ),
      'primary game actions fit viewport ' + width,
    );
  }
  await page.screenshot({ path: path.join(evidence, 'web-primary-actions-en.png') });
  await click('Back to collection');

  await click('Settings');
  await click('Configure shortcuts');
  await page.screenshot({ path: path.join(evidence, 'web-shortcuts-en.png') });
  await page.getByRole('textbox', { name: 'Add game', exact: true }).press('Control+Shift+N');
  await click('Save');
  await click('Back to collection');
  await page.waitForFunction(() => window.checkpointState?.kind === 'main');
  await page.keyboard.press('Control+Shift+N');
  await page.getByRole('textbox', { name: 'Game title' }).waitFor();
  check(true, 'custom keyboard combination opens the game editor');
  await click('Cancel');
  await page.reload();
  await page.locator('.game').first().waitFor();
  await page.keyboard.press('Control+Shift+N');
  await page.getByRole('textbox', { name: 'Game title' }).waitFor();
  check(true, 'custom shortcuts persist after reload');
  await click('Cancel');
  await click('Settings');
  await click('Configure shortcuts');
  await page.getByRole('textbox', { name: 'Search games', exact: true }).press('Control+Shift+N');
  await click('Save');
  await page
    .getByText(
      'Some shortcuts are duplicated or reserved. Space and Escape are kept for navigation and closing.',
      { exact: true },
    )
    .waitFor();
  check(true, 'duplicate shortcuts are rejected without saving');
  await click('Restore defaults');
  await click('Save');
  await click('Back to collection');
  await page.waitForFunction(() => window.checkpointState?.kind === 'main');
  await page.keyboard.press('Control+n');
  await page.getByRole('textbox', { name: 'Game title' }).waitFor();
  check(true, 'restore defaults restores the original working combinations');
  await click('Cancel');
  await click('Manage lists');
  await input('List name', 'Weekend');
  await click('Create list');
  await page.waitForFunction(
    () =>
      document.querySelector('select[aria-label="Game list"]').value === '1' &&
      !document.querySelector('input[aria-label="List name"]').disabled,
  );
  await input('List name', 'Next');
  await click('Create list');
  await page.waitForFunction(
    () =>
      document.querySelector('select[aria-label="Game list"]').options.length === 3 &&
      !document.querySelector('input[aria-label="List name"]').disabled,
  );
  await click('Save');
  await page.waitForFunction(() => window.checkpointState?.kind === 'main');
  await click('Add game');
  await input('Game title', 'Shared list fixture');
  await page.getByRole('checkbox', { name: 'List: Weekend', exact: true }).check();
  await click('Save');
  await page.waitForFunction(() => window.checkpointState?.kind === 'main');
  check(
    (await page.locator('.game-title').allTextContents()).includes('Shared list fixture'),
    'new game joins the active custom list',
  );
  await page
    .getByRole('combobox', { name: 'Game list', exact: true })
    .selectOption('custom:Weekend');
  await page.getByText('Shared list fixture', { exact: true }).waitFor();
  check(true, 'one game belongs to two lists without duplicate copies');
  await page.reload();
  await page.getByText('Shared list fixture', { exact: true }).waitFor();
  check(
    (await page.getByRole('combobox', { name: 'Game list', exact: true }).inputValue()) ===
      'custom:Weekend',
    'active list and memberships survive reload',
  );
  await click('Manage lists');
  await page.getByRole('combobox', { name: 'Game list', exact: true }).selectOption('1');
  await input('List name', 'Renamed weekend');
  await click('Rename list');
  await click('Save');
  await page.waitForFunction(() => window.checkpointState?.kind === 'main');
  check(
    (await page.getByRole('combobox', { name: 'Game list', exact: true }).inputValue()) ===
      'custom:Renamed weekend' &&
      (await page.getByText('Shared list fixture', { exact: true }).count()) === 1,
    'renaming a list updates membership without cloning the game',
  );
  await click('Manage lists');
  await page.getByRole('combobox', { name: 'Game list', exact: true }).selectOption('2');
  await click('Remove list');
  await page.getByText('Next', { exact: true }).waitFor();
  await click('Remove list');
  await click('Save');
  await page.waitForFunction(() => window.checkpointState?.kind === 'main');
  check(
    (await page.locator('.game-title').allTextContents()).includes('Shared list fixture'),
    'removing a list keeps its game and its other membership',
  );
  await click('Add game');
  await input('Game title', 'Private from the start');
  await page.getByRole('checkbox', { name: 'Private to my friends', exact: true }).check();
  await click('Save');
  await page.waitForFunction(() => window.checkpointState?.kind === 'main');
  check(
    (await page.getByText('Private from the start', { exact: true }).count()) === 0,
    'a game created private is absent from public lists',
  );
  await page.getByRole('combobox', { name: 'Game list', exact: true }).selectOption('private');
  await page.getByText('Private from the start', { exact: true }).waitFor();
  check(true, 'private games have their separate view');
  await page.screenshot({ path: path.join(evidence, 'web-private-games-en.png') });
  await click('Manage lists');
  await page.getByRole('checkbox', { name: 'Create new games as private', exact: true }).check();
  await click('Save');
  await page.waitForFunction(() => window.checkpointState?.kind === 'main');
  await page.getByRole('combobox', { name: 'Game list', exact: true }).selectOption('all');
  await click('Add game');
  check(
    await page.getByRole('checkbox', { name: 'Private to my friends', exact: true }).isChecked(),
    'new games can default to private before their first save',
  );
  await click('Cancel');
  await click('Manage lists');
  await page.getByRole('checkbox', { name: 'Create new games as private', exact: true }).uncheck();
  await page.screenshot({ path: path.join(evidence, 'web-lists-en.png') });
  await click('Save');
  await page.waitForFunction(() => window.checkpointState?.kind === 'main');
  const legacyId = '44444444-4444-4444-8444-444444444444';
  await page.evaluate(
    async ({ id, own }) => {
      const { BrowserStore } = await import('./store.mjs'),
        { normalize } = await import('./model.mjs'),
        store = await new BrowserStore().open(),
        library = await store.get('library');
      library.games.push(normalize({ id, title: 'Legacy private fixture', friendsPrivate: null }));
      await store.save(library, library.revision);
      await store.put('shares.' + own, {
        [id]: { selected: false, revision: 0, published: null, desired: null, pending: null },
      });
    },
    { id: legacyId, own },
  );
  await page.reload();
  await page.locator('.window').waitFor();
  await page.screenshot({ path: path.join(evidence, 'web-library-en.png') });
  await click('Sign in');
  await page.getByRole('textbox', { name: 'Checkpoint username', exact: true }).waitFor();
  check(
    (await page.getByRole('button', { name: 'Create account', exact: true }).isVisible()) &&
      (await page
        .getByRole('textbox', { name: 'Display name (for account creation)', exact: true })
        .count()) === 0,
    'English sign-in opens only the existing-account form',
  );
  await page.screenshot({ path: path.join(evidence, 'web-login-en.png') });
  await click('Create account');
  await page
    .getByRole('textbox', { name: 'Display name (for account creation)', exact: true })
    .waitFor();
  await click('I already have an account');
  check(
    (await page
      .getByRole('textbox', { name: 'Display name (for account creation)', exact: true })
      .count()) === 0 && authCalls === 0,
    'English registration switches back to sign-in without an authentication request',
  );
  await input('Checkpoint username', 'browser_tester');
  await input('Checkpoint password', 'fixture-not-a-real-password');
  const defaultPublication = page.waitForResponse(
    (r) =>
      r.url().includes('/cp_publish_game') && r.request().method() === 'POST' && r.status() === 200,
  );
  await click('Sign in');
  await defaultPublication;
  await page.getByText('Fixture Friend', { exact: true }).waitFor();
  check(authCalls === 1, 'Checkpoint username/password authentication uses the configured service');
  await click('My account');
  await page.getByText('checkpoint-123456abcdef', { exact: true }).waitFor();
  check(
    !JSON.stringify(await page.evaluate(() => window.checkpointState)).includes('fixture-token'),
    'account code uses full Checkpoint and snapshots exclude session tokens',
  );
  await page.getByRole('button', { name: 'Friends', exact: true }).last().click();
  await input('Friend code (checkpoint-…)', 'checkpoint-abcdef123456');
  await click('Send request');
  await page.waitForFunction(() => document.body.textContent.includes('Request sent'));
  check(inviteBody.recipient_id === other, 'friend invitation targets the Checkpoint identity');
  await click('View progress');
  await page.getByText('Friend game', { exact: true }).waitFor();
  check(
    (await page.locator('.friends-content').textContent()).includes('60%'),
    'friends progress displays Checkpoint publications',
  );
  check(
    (await page
      .locator('.friend-achievements')
      .getByText('7 / 12 achievements completed', { exact: true })
      .isVisible()) &&
      (await page
        .getByRole('progressbar', { name: 'Achievements', exact: true })
        .getAttribute('value')) === '58',
    'friend achievements show prominent completed and pending progress',
  );
  check(
    (await page
      .getByText('Manual achievement · Checkpoint — Friend personal challenge', { exact: true })
      .isVisible()) && (await page.getByText('Finish without damage', { exact: true }).isVisible()),
    'friend manual achievements are visible with explicit Checkpoint source and descriptions',
  );
  await page.screenshot({ path: path.join(evidence, 'web-friends-en.png') });
  await page.locator('[data-tab=friends]').click();
  await page.getByRole('button', { name: 'View progress', exact: true }).waitFor();
  check(
    (await page.getByText('Friend game', { exact: true }).count()) === 0,
    'pressing the active Friends button returns from friend progress to its initial menu',
  );
  await click('View progress');
  await page.getByText('Friend game', { exact: true }).waitFor();
  await click('Library');
  await page.locator('[data-tab=friends]').click();
  await page.getByRole('button', { name: 'View progress', exact: true }).waitFor();
  check(
    (await page.getByText('Friend game', { exact: true }).count()) === 0,
    'returning to Friends opens the initial friends menu instead of the previous friend',
  );

  await click('Sharing');
  await page.getByText('Shared', { exact: true }).first().waitFor();
  check(
    published.some((p) => p.p_game) &&
      !published.some(
        (p) =>
          p.p_game?.title === 'Private from the start' ||
          p.p_game?.title === 'Legacy private fixture',
      ) &&
      !JSON.stringify(published).includes('Private fixture notes'),
    'default sharing publishes listed games while excluding private games and notes',
  );
  await page.getByRole('button', { name: 'My list', exact: true }).click();
  await page.getByRole('combobox', { name: 'Game list', exact: true }).selectOption('private');
  await page.getByText('Legacy private fixture', { exact: true }).waitFor();
  check(true, 'migration preserves a legacy explicit private choice');
  await page.getByRole('combobox', { name: 'Game list', exact: true }).selectOption('all');
  const withdrawal = page.waitForResponse(
    (r) =>
      r.url().includes('/cp_publish_game') &&
      r.request().postDataJSON()?.p_game === null &&
      r.status() === 200,
  );
  await page.locator('.game').filter({ hasText: 'Shared list fixture' }).click({ button: 'right' });
  await page.getByRole('menuitem', { name: 'Move to private games', exact: true }).click();
  await withdrawal;
  await page.getByRole('combobox', { name: 'Game list', exact: true }).selectOption('private');
  await page.getByText('Shared list fixture', { exact: true }).waitFor();
  check(
    published.some((p) => p.p_game === null),
    'moving a public game to private withdraws its server publication',
  );
  await page.getByRole('combobox', { name: 'Game list', exact: true }).selectOption('all');
  await page.getByRole('link', { name: 'Overview', exact: true }).click();
  await page.getByRole('link', { name: 'Open Checkpoint', exact: true }).waitFor();
  await page.setViewportSize({ width: 320, height: 640 });
  check(
    await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
    'presentation fits a narrow mobile screen',
  );
  await page.screenshot({ path: path.join(evidence, 'web-presentation-mobile-en.png') });
  await page.setViewportSize({ width: 1280, height: 900 });
  await page.getByRole('link', { name: 'Open Checkpoint', exact: true }).click();
  await page.locator('.window').waitFor();
  check(
    (await page.locator('.game-title').allTextContents()).includes('Imported Steam game') &&
      (await page.getByRole('button', { name: 'My account', exact: true }).isVisible()),
    'returning through the presentation retains the library and same-tab session',
  );
  await page.getByRole('button', { name: 'My list', exact: true }).click();
  await click('Settings');
  await click('Link Steam');
  await page.waitForFunction(
    () => window.checkpointState?.kind === 'main' && !window.checkpointState.busy,
  );
  await page.getByRole('button', { name: 'Library', exact: true }).click();
  await page.locator('.viewport').evaluate((node) => {
    node.scrollTop = node.scrollHeight;
  });
  await page.getByText('Borrowed family fixture', { exact: true }).waitFor();
  check(
    libraryCalls > 0 && achievementCalls > 0,
    'Steam linking imports the library and synchronizes tracked achievements',
  );
  const before = libraryCalls;
  await page.reload();
  await page.waitForFunction(
    () => window.checkpointState?.kind === 'main' && !window.checkpointState.busy,
  );
  check(libraryCalls > before, 'Steam synchronization runs automatically on browser app reload');
  await page.getByRole('button', { name: 'My list', exact: true }).click();
  await page
    .locator('.game')
    .filter({ hasText: 'Imported Steam game' })
    .locator('.game-tools button')
    .nth(1)
    .click();
  check(
    (await page.getByRole('textbox', { name: 'Private notes' }).inputValue()) ===
      'Steam preserves me' &&
      (await page.getByRole('combobox', { name: 'Status', exact: true }).inputValue()) === '2',
    'Steam sync preserves manual state and notes',
  );
  await click('Achievements');
  check(
    !(await page.locator('.dialog-page').textContent()).includes('Hidden secret name'),
    'locked hidden achievements remain spoilers until explicitly revealed',
  );
  await page.getByRole('checkbox', { name: 'Show hidden achievements', exact: true }).check();
  await page.getByText('○ Hidden secret name', { exact: true }).waitFor();
  check(
    !(await page.locator('.dialog-page').textContent()).includes('Spoiler description'),
    'revealing secret names keeps descriptions collapsed',
  );
  await click('Show description · Hidden secret name');
  await page.getByText('Spoiler description', { exact: true }).waitFor();
  check(
    await page.locator('.achievement-description').isVisible(),
    'achievement descriptions open on request',
  );
  await page.screenshot({ path: path.join(evidence, 'web-achievements-en.png') });
  await click('Hide description · Hidden secret name');
  check(
    (await page.locator('.achievement-description').count()) === 0,
    'achievement descriptions close on request',
  );
  await click('Close');
  await click('Cancel');
  await page
    .locator('.game')
    .filter({ hasText: 'Imported Steam game' })
    .locator('.achievement-link')
    .click();
  check(
    await page.locator('.achievement-summary progress').isVisible(),
    'each library card opens the achievement overview directly',
  );
  await click('Close');
  await page
    .locator('.game')
    .filter({ hasText: 'Imported Steam game' })
    .locator('.game-tools button')
    .nth(1)
    .click();
  const storeCheck = await page.evaluate(async () => {
    const { BrowserStore } = await import('./store.mjs'),
      s = await new BrowserStore().open(),
      value = await s.get('library');
    try {
      await s.save(value, value.revision - 1);
      return false;
    } catch (e) {
      return e.message === 'local-conflict';
    }
  });
  check(storeCheck, 'IndexedDB refuses stale writes instead of overwriting another tab');
  await click('Cancel');
  await page.setViewportSize({ width: 390, height: 844 });
  await page.getByRole('button', { name: 'Settings', exact: true }).click();
  check(
    await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
    'settings fit a narrow mobile viewport',
  );
  await page.screenshot({ path: path.join(evidence, 'web-mobile-en.png') });
  await click('Cancel');
  const offlineContext = await browser.newContext({ serviceWorkers: 'allow' }),
    offlinePage = await offlineContext.newPage();
  await offlinePage.goto(`http://127.0.0.1:${testPort}/Checkpoint/app.html`);
  await offlinePage.locator('.window').waitFor();
  await offlinePage.waitForFunction(() => !!navigator.serviceWorker.controller);
  await offlineContext.setOffline(true);
  await offlinePage.reload();
  await offlinePage.locator('.window').waitFor();
  check(true, 'the cached application opens offline after a first visit');
  await offlineContext.close();
  await page.setViewportSize({ width: 1280, height: 900 });
  await click('My account');
  await click('Sign out');
  await page.getByRole('textbox', { name: 'Checkpoint username', exact: true }).waitFor();
  await page.getByRole('button', { name: 'My list', exact: true }).click();
  check(
    (await page.getByRole('button', { name: 'Sign in', exact: true }).isVisible()) &&
      !JSON.stringify(await page.evaluate(() => window.checkpointState)).includes('fixture-token'),
    'signing out restores the visible login entry without exposing tokens',
  );
  coversEnabled = true;
  await click('My list');
  await click('Add game');
  await input('Game title', 'IGDB cover fixture');
  await click('Save');
  await page.getByText('Closest cover fixture · 2020', { exact: true }).waitFor();
  await page.locator('img.cover-preview').waitFor({ state: 'visible' });
  check(
    await page.locator('img.cover-preview').isVisible(),
    'missing Steam covers automatically open an IGDB preview without an account',
  );
  await page.screenshot({ path: path.join(evidence, 'web-igdb-cover-en.png') });
  await click('Do not use this cover');
  await page.locator('.window').waitFor();
  const rejectedRequests = coverRequests.length;
  await page.reload();
  await page.locator('.window').waitFor();
  await page.waitForTimeout(3500);
  check(
    coverRequests.length === rejectedRequests && (await page.locator('.dialog-page').count()) === 0,
    'rejected IGDB cover is not offered again after browser reload',
  );
  await page
    .locator('.game')
    .filter({ hasText: 'IGDB cover fixture' })
    .locator('.game-tools button')
    .nth(1)
    .click();
  await click('Search IGDB for another cover');
  await page.getByText('Second cover fixture · 2020', { exact: true }).waitFor();
  check(
    coverRequests.at(-1).excluded.includes('fixture_first') &&
      coverRequests.at(-1).refresh === true,
    'manual cover search excludes rejected images and refreshes IGDB results',
  );
  await click('Use this cover');
  await click('Save');
  await page.locator('.window').waitFor();
  const coverState = await page.evaluate(async () => {
    const { BrowserStore } = await import('./store.mjs'),
      s = await new BrowserStore().open();
    return (await s.get('library')).games.find((g) => g.title === 'IGDB cover fixture');
  });
  check(
    coverState.customCover.startsWith('data:image/png;base64,') &&
      coverState.igdbCoverImageId === 'fixture_second' &&
      coverState.rejectedIgdbCovers.includes('fixture_first'),
    'accepted IGDB image and rejected-cover history persist as local browser data',
  );
  await page.waitForFunction(() => window.checkpointState?.kind === 'main');
  await page.waitForTimeout(1600);
  check(
    coverConfirmations.some(
      (g) =>
        g.title === 'IGDB cover fixture' &&
        g.imageId === 'fixture_second' &&
        !Object.values(g).some((v) => typeof v === 'string' && v.startsWith('data:')),
    ),
    'saved accepted IGDB cover contributes metadata only',
  );
  // A second installation of the same game has no local cover, even after an earlier search found none.
  await page.waitForFunction(
    () => window.checkpointState?.kind === 'main' && !window.checkpointState.busy,
  );
  await page.evaluate(async () => {
    const { BrowserStore } = await import('./store.mjs'),
      store = await new BrowserStore().open(),
      v = await store.get('library'),
      g = v.games.find((g) => g.title === 'IGDB cover fixture');
    g.customCover = null;
    g.igdbCoverImageId = '';
    g.igdbCoverSearchTitle = g.title;
    g.tracked = false;
    await store.save(v, v.revision);
  });
  await page.reload();
  await page.locator('.window').waitFor();
  await click('Library');
  await page.waitForFunction(
    async () => {
      const { BrowserStore } = await import('./store.mjs'),
        store = await new BrowserStore().open();
      const g = (await store.get('library')).games.find((g) => g.title === 'IGDB cover fixture');
      return (
        g?.igdbCoverImageId === 'fixture_second' &&
        g.customCover?.startsWith('data:image/png;base64,')
      );
    },
    { timeout: 20000 },
  );
  check(
    (await page.locator('.dialog-page').count()) === 0,
    'a library game without a cover applies the community default without a confirmation dialog',
  );
  await page.waitForFunction(
    () => window.checkpointState?.kind === 'main' && !window.checkpointState.busy,
  );
  await page.evaluate(async () => {
    const { BrowserStore } = await import('./store.mjs'),
      store = await new BrowserStore().open(),
      v = await store.get('library');
    v.games.find((g) => g.title === 'IGDB cover fixture').tracked = true;
    await store.save(v, v.revision);
  });
  await page.reload();
  await page.locator('.window').waitFor();

  await page.reload();
  await page.locator('.window').waitFor();
  await page
    .locator('.game')
    .filter({ hasText: 'IGDB cover fixture' })
    .locator('img.cover')
    .waitFor();
  check(true, 'accepted IGDB cover remains available after app reload');
  await page
    .locator('.game')
    .filter({ hasText: 'IGDB cover fixture' })
    .locator('.game-title')
    .click();
  await page.locator('.game-detail-sheet').waitFor();
  check(
    (await page.getByRole('button', { name: 'Play', exact: true }).count()) === 0 &&
      (await page
        .getByRole('button', { name: 'View achievements', exact: true })
        .evaluate((n) => !n.classList.contains('accent'))),
    'manual game sheet keeps achievements and editing consistent without offering an invalid Steam launch',
  );
  const redesignedSheet = (await page.locator('.game-detail-visibility').count()) > 0;
  check(
    (await page.locator('.game-playtime').getByText('0 h', { exact: true }).isVisible()) &&
      (redesignedSheet
        ? (await page.getByRole('textbox', { name: 'Notes', exact: true }).inputValue()) === '' &&
          (await page
            .locator('.game-detail-visibility')
            .getByText('Visible', { exact: true })
            .isVisible())
        : (await page.getByText('No notes', { exact: true }).isVisible()) &&
          (await page.getByText('Visibility: Visible to my friends', { exact: true }).isVisible())),
    'clicking a game title opens its full localized sheet without editing it',
  );
  await page.screenshot({ path: path.join(evidence, 'web-game-details-en.png') });
  await click('View achievements');
  await page.getByText('Achievements', { exact: true }).first().waitFor();
  await click('Close');
  check(
    await page.locator('.game-detail-sheet').isVisible(),
    'achievement navigation returns to the game sheet',
  );
  await click('Close');
  await page.locator('.game').filter({ hasText: 'IGDB cover fixture' }).focus();
  await page.keyboard.press('Enter');
  await page.locator('.game-detail-sheet').waitFor();
  check(true, 'Enter on a focused game opens its sheet');
  await click('Edit game');
  await page.getByRole('textbox', { name: 'Game title', exact: true }).waitFor();
  check(
    (await page.getByRole('textbox', { name: 'Game title', exact: true }).inputValue()) ===
      'IGDB cover fixture',
    'sheet edit action opens the original game editor',
  );
  await click('Cancel');
  await click('Back to collection');
  coversEnabled = false;
  await click('My list');
  for (const name of [
    'First bulk list',
    'Second bulk list',
    'Third bulk list',
    'Fourth bulk list',
  ]) {
    await click('Manage lists');
    await input('List name', name);
    await click('Save');
    await page.locator('.window').waitFor();
    check(
      (await page.getByRole('combobox', { name: 'Game list', exact: true }).inputValue()) ===
        'custom:' + name &&
        (await page.locator('[data-tab=list]').evaluate((n) => n.classList.contains('active'))),
      'Save creates and selects a typed list: ' + name,
    );
    check(
      await page.getByRole('button', { name: 'View list details', exact: true }).isDisabled(),
      'empty list cannot open its sheet: ' + name,
    );
  }
  await click('Library');
  await page.locator('.game').filter({ hasText: 'Imported Steam game' }).click({ button: 'right' });
  check(
    (await page
      .getByRole('menuitem', { name: 'Move to Fourth bulk list', exact: true })
      .isVisible()) &&
      (await page
        .getByRole('menuitem', { name: 'Move to Second bulk list', exact: true })
        .isVisible()) &&
      (await page
        .getByRole('menuitem', { name: 'Move to First bulk list', exact: true })
        .count()) === 0,
    'game menu offers the three newest destination lists',
  );
  check(
    (await page
      .locator('.menu')
      .getByRole('menuitem', { name: 'Settings', exact: true })
      .count()) === 0 &&
      (await page
        .locator('.menu')
        .getByRole('menuitem', { name: 'Add game', exact: true })
        .count()) === 0,
    'game menus contain game actions and omit application commands',
  );
  await page.screenshot({ path: path.join(evidence, 'web-game-menu-en.png') });
  await page.keyboard.press('Escape');
  const bulkTitles = ['Browser fixture <img src=x>', 'Imported Steam game'];
  for (const title of bulkTitles)
    await page.getByRole('checkbox', { name: 'Select a game: ' + title, exact: true }).check();
  check(
    (await page.locator('.selection-count').textContent()).includes('2'),
    'Library supports selecting several games',
  );
  await page.screenshot({ path: path.join(evidence, 'web-bulk-library-en.png') });
  await page
    .locator('.selectionbar')
    .getByRole('button', { name: 'Change list', exact: true })
    .click();
  await page
    .getByRole('combobox', { name: 'Destination list', exact: true })
    .selectOption({ label: 'Fourth bulk list' });
  await click('Move games');
  await page.locator('.window').waitFor();
  const readBulk = () =>
    page.evaluate(async () => {
      const { BrowserStore } = await import('./store.mjs'),
        store = await new BrowserStore().open();
      return (await store.get('library')).games.filter((g) =>
        ['Browser fixture <img src=x>', 'Imported Steam game'].includes(g.title),
      );
    });
  check(
    (await readBulk()).every((g) => g.lists.length === 1 && g.lists[0] === 'Fourth bulk list'),
    'bulk movement from Library assigns the selected destination',
  );
  await click('My list');
  await page
    .getByRole('combobox', { name: 'Game list', exact: true })
    .selectOption('custom:Fourth bulk list');
  await click('View list details');
  await page.getByText('List details', { exact: true }).waitFor();
  check(
    (await page.getByRole('checkbox').count()) === 2 &&
      (await page.getByText('Games: 2', { exact: true }).isVisible()),
    'list sheet shows summary and selectable members',
  );
  await page.screenshot({ path: path.join(evidence, 'web-list-details-en.png') });
  await page.getByRole('combobox', { name: 'Filter by status', exact: true }).selectOption('5');
  await page.getByText('No games match the filters.', { exact: true }).waitFor();
  check(
    (await page.getByRole('checkbox').count()) === 0,
    'list status filter hides nonmatching members',
  );
  await page.getByRole('combobox', { name: 'Filter by status', exact: true }).selectOption('0');
  await page.getByRole('textbox', { name: 'Search games', exact: true }).fill('Imported');
  await page.getByText('Results: 1 / 2', { exact: true }).waitFor();
  await click('Select this page');
  await page.getByRole('combobox', { name: 'Filter by status', exact: true }).selectOption('5');
  await page.getByText('Selected games: 0', { exact: true }).waitFor();
  check(
    await page.getByRole('button', { name: 'Move to private games', exact: true }).isDisabled(),
    'changing a list filter clears hidden selections',
  );
  await page.getByRole('combobox', { name: 'Filter by status', exact: true }).selectOption('0');
  await page.getByRole('textbox', { name: 'Search games', exact: true }).fill('');
  await page.getByText('Results: 2 / 2', { exact: true }).waitFor();
  await click('Select this page');
  await click('Move to private games');
  await page.getByText('Selected games: 0', { exact: true }).waitFor();
  check(
    (await readBulk()).every((g) => g.friendsPrivate === true) &&
      (await page.getByRole('checkbox').count()) === 2,
    'private members remain manageable in their owner list sheet',
  );
  await click('Select this page');
  await click('Make visible to friends');
  await page.getByText('Selected games: 0', { exact: true }).waitFor();
  check(
    (await readBulk()).every((g) => g.friendsPrivate === false),
    'list sheet changes several members back to public',
  );
  await page
    .getByRole('checkbox', { name: 'Select a game: Browser fixture <img src=x>', exact: true })
    .check();
  await click('Remove from this list');
  await page.getByText('Games: 1', { exact: true }).waitFor();
  check(
    (await readBulk()).length === 2 &&
      (await readBulk()).find((g) => g.title.startsWith('Browser fixture')).notes ===
        'Private fixture notes',
    'removing membership preserves the library game and notes',
  );
  await click('Select this page');
  await click('Change list');
  await page
    .getByRole('combobox', { name: 'Destination list', exact: true })
    .selectOption({ label: 'Third bulk list' });
  await click('Move games');
  await page.locator('.window').waitFor();
  check(
    (await readBulk())
      .find((g) => g.title === 'Imported Steam game')
      .lists.includes('Third bulk list') &&
      (await page.getByRole('button', { name: 'View list details', exact: true }).isDisabled()),
    'moving the final member closes the empty list sheet and disables reopening',
  );
  await page.reload();
  await page.locator('.window').waitFor();
  check(
    (await readBulk())
      .find((g) => g.title === 'Imported Steam game')
      .lists.includes('Third bulk list'),
    'batch list changes persist after browser reload',
  );

  await click('Library');
  const directTitle = 'Browser fixture <img src=x>',
    directBefore = (await readBulk()).find((g) => g.title === directTitle),
    directGame = () => page.locator('.game').filter({ hasText: directTitle });
  await directGame().click({ button: 'right' });
  await page.getByRole('menuitem', { name: 'Remove from My list', exact: true }).click();
  await page.waitForFunction(
    (title) => window.checkpointState.games.some((g) => g.title === title && !g.tracked),
    directTitle,
  );
  await directGame().click({ button: 'right' });
  await page.getByRole('menuitem', { name: 'Add to My list', exact: true }).click();
  await page.waitForFunction(
    (title) => window.checkpointState.games.some((g) => g.title === title && g.tracked),
    directTitle,
  );
  await directGame().click({ button: 'right' });
  check(
    (await page.getByRole('menuitem', { name: 'Add to My list', exact: true }).count()) === 0 &&
      (await page.getByRole('menuitem', { name: 'Remove from My list', exact: true }).isVisible()),
    'Library right-click tracks directly and hides the add action for tracked games',
  );
  await page.keyboard.press('Escape');
  await page.reload();
  await page.locator('.window').waitFor();
  const directAfter = (await readBulk()).find((g) => g.title === directTitle);
  check(
    directAfter.tracked &&
      directAfter.friendsPrivate === directBefore.friendsPrivate &&
      JSON.stringify(directAfter.lists) === JSON.stringify(directBefore.lists) &&
      directAfter.notes === directBefore.notes,
    'direct Library tracking survives reload and preserves privacy memberships and notes',
  );

  await click('Library');
  await directGame().click({ button: 'right' });
  await page.getByRole('menuitem', { name: 'Remove from My list', exact: true }).click();
  await page.waitForFunction(
    (title) => window.checkpointState.games.some((g) => g.title === title && !g.tracked),
    directTitle,
  );
  const selectionBefore = await readBulk();
  for (const title of bulkTitles)
    await page.getByRole('checkbox', { name: 'Select a game: ' + title, exact: true }).check();
  await page
    .locator('.selectionbar')
    .getByRole('button', { name: 'Add to My list', exact: true })
    .click();
  await page.waitForFunction(
    (titles) =>
      titles.every((title) =>
        window.checkpointState.games.some((g) => g.title === title && g.tracked),
      ),
    bulkTitles,
  );
  check(
    (await page
      .locator('.selectionbar')
      .getByRole('button', { name: 'Add to My list', exact: true })
      .count()) === 0,
    'Library selection bar adds a mixed selection and hides the action when all are tracked',
  );
  await page.reload();
  await page.locator('.window').waitFor();
  const selectionAfter = await readBulk();
  check(
    selectionAfter.every((g) => {
      const previous = selectionBefore.find((p) => p.id === g.id);
      return (
        g.tracked &&
        g.friendsPrivate === previous.friendsPrivate &&
        JSON.stringify(g.lists) === JSON.stringify(previous.lists) &&
        g.notes === previous.notes
      );
    }),
    'Library bulk tracking survives reload without changing privacy memberships or notes',
  );

  // Explicit settings review must include more than the periodic 20-game batch.
  libraryEmpty = true;
  await page.evaluate(async () => {
    const { BrowserStore } = await import('./store.mjs'),
      { normalize } = await import('./model.mjs'),
      store = await new BrowserStore().open(),
      value = await store.get('library');
    value.games = Array.from({ length: 22 }, (_, i) =>
      normalize({
        title: 'Review game ' + String(i).padStart(2, '0'),
        steamAppId: 10000 + i,
        tracked: false,
        friendsPrivate: true,
        notes: 'Keep review notes',
        manualAchievements: [{ id: 'local', name: 'Local goal', unlocked: true }],
        achievementOverrides: { 'steam:FIRST': false },
      }),
    );
    await store.save(value, value.revision);
  });
  await page.reload();
  await page.waitForFunction(
    () => window.checkpointState?.kind === 'main' && !window.checkpointState.busy,
  );
  await click('Settings');
  await click('Review all achievements');
  check(
    await page.getByText('Game 1 / 22', { exact: true }).isVisible(),
    'achievement review includes untracked private library games',
  );
  await page.screenshot({ path: path.join(evidence, 'web-achievement-review-en.png') });
  await click('Update all achievements');
  await page.locator('.window').waitFor();
  check(
    await page.locator('.achievement-review').isVisible(),
    'achievement update returns to the usable library and shows background progress',
  );
  await click('Library');
  await page.locator('[data-game]').first().waitFor();
  check(
    (await page.locator('[data-game]').count()) > 0 &&
      (await page.locator('.achievement-review').isVisible()),
    'library stays usable while delayed achievement requests are running',
  );
  await click('Stop review');
  await page.getByText(/^Achievement review stopped:/).waitFor();
  const stoppedCalls = achievementCalls;
  await page.waitForTimeout(1100);
  check(achievementCalls === stoppedCalls, 'stopping review prevents new queued requests');
  await page.locator('.achievement-review button').click();
  await click('Settings');
  await click('Review all achievements');
  const reviewBefore = achievementCalls;
  await click('Update all achievements');
  await click('Library');
  await page.getByText('Review game 00', { exact: true }).click();
  await click('Edit game');
  await input('Private notes', 'Edited during background review');
  await click('Save');
  await click('Back to collection');
  await page
    .getByText('Achievement review finished: 22 / 22 · Could not update: 0', { exact: true })
    .waitFor({ timeout: 45000 });
  check(
    achievementCalls - reviewBefore === 22,
    'explicit background review updates every Steam game beyond the periodic batch limit',
  );
  await click('Settings');
  await click('Review all achievements');
  const reviewed = await page.evaluate(async () => {
    const { BrowserStore } = await import('./store.mjs'),
      store = await new BrowserStore().open();
    return (await store.get('library')).games;
  });
  check(
    reviewed.every(
      (g) =>
        g.manualAchievements.length === 1 &&
        g.achievementOverrides['steam:FIRST'] === false &&
        g.notes ===
          (g.title === 'Review game 00'
            ? 'Edited during background review'
            : 'Keep review notes') &&
        g.achievements.length === 2 &&
        g.friendsPrivate &&
        g.tracked === false,
    ),
    'full achievement refresh preserves local goals, overrides, notes and visibility',
  );
  await click('View achievements');
  await page.getByRole('checkbox', { name: 'Pending only', exact: true }).uncheck();
  await page.getByText('✓ Local goal', { exact: true }).waitFor();
  check(
    await page.getByText('○ First', { exact: true }).isVisible(),
    'achievement sheet honors local overrides and includes manual goals',
  );
  await click('Delete manual achievements');
  await page
    .getByText(
      'All manually created achievements for this game will be deleted. Steam and RetroAchievements achievements will be kept.',
      { exact: true },
    )
    .waitFor();
  await click('Cancel');
  await page.getByText('✓ Local goal', { exact: true }).waitFor();
  check(true, 'canceling manual reset keeps the local achievement');
  await click('Delete manual achievements');
  await click('Delete');
  await page.getByRole('button', { name: 'Delete manual achievements', exact: true }).waitFor();
  const resetGame = await page.evaluate(async () => {
    const { BrowserStore } = await import('./store.mjs'),
      store = await new BrowserStore().open();
    return (await store.get('library')).games.find((g) => g.title === 'Review game 00');
  });
  check(
    resetGame.manualAchievements.length === 0 &&
      resetGame.achievements.length === 2 &&
      resetGame.achievementOverrides['steam:FIRST'] === false,
    'confirmed reset removes only manual achievements and preserves Steam progress',
  );
  await click('Close');
  await click('Next game');
  check(
    await page.getByText('Game 2 / 22', { exact: true }).isVisible(),
    'achievement review advances through games and returns from their sheets',
  );
  await click('Close');
  await click('Cancel');
  // Covers review: covered games are skipped; rejection, acceptance and skipping persist independently.
  steamFailure =
    'Steam no permite consultar estos logros. Revisa la privacidad de tus detalles de juegos.';
  const privacyMessage =
    'Steam does not allow achievement access. Check your Game details privacy settings.';
  await page.getByText('Review game 00', { exact: true }).click();
  await click('View achievements');
  await click('Refresh');
  await page.getByText(new RegExp(privacyMessage.replaceAll('.', '\\.'))).waitFor();
  check(
    (await page.locator('.dialog-page').textContent()).includes(privacyMessage),
    'individual Steam achievement refresh displays the localized backend privacy cause',
  );
  await click('Back to collection');
  await click('Settings');
  await click('Review all achievements');
  await click('Update all achievements');
  await page.waitForFunction(
    () => window.checkpointState.achievementReview?.running === false,
    null,
    { timeout: 30000 },
  );
  check(
    (await page.locator('.achievement-review').textContent()).includes('Could not update: 22') &&
      (await page.locator('.achievement-review').textContent()).includes(privacyMessage),
    'background Steam review displays a failing game and translated cause alongside the error count',
  );
  await click('Settings');
  await page.getByRole('combobox', { name: 'Language', exact: true }).selectOption('0');
  await click('Save');
  await page.waitForFunction(
    () => window.checkpointState.kind === 'main' && window.checkpointState.language === 'es',
  );
  check(
    (await page.locator('.achievement-review').textContent()).includes(steamFailure) &&
      !(await page.locator('.achievement-review').textContent()).includes(privacyMessage),
    'stored background failure follows a later language change without mixing languages',
  );
  await click('Ajustes');
  await page.getByRole('combobox', { name: 'Idioma', exact: true }).selectOption('1');
  await click('Guardar');
  await page.waitForFunction(
    () => window.checkpointState.kind === 'main' && window.checkpointState.language === 'en',
  );
  const afterFailure = await page.evaluate(async () => {
    const { BrowserStore } = await import('./store.mjs'),
      store = await new BrowserStore().open();
    return (await store.get('library')).games;
  });
  check(
    afterFailure.every(
      (g) =>
        g.achievements.length === 2 &&
        g.syncedAt === reviewed.find((previous) => previous.id === g.id).syncedAt,
    ),
    'Steam privacy failures preserve the previously saved achievements and sync timestamps',
  );
  steamFailure = null;
  await page.locator('.achievement-review button').click();
  coversEnabled = true;
  await context.route('https://cdn.cloudflare.steamstatic.com/steam/apps/99999/**', (route) =>
    route.fulfill({ status: 200, contentType: 'image/png', body: coverPng }),
  );
  await page.evaluate(async () => {
    const { BrowserStore } = await import('./store.mjs'),
      { normalize } = await import('./model.mjs'),
      store = await new BrowserStore().open(),
      value = await store.get('library');
    value.games = [
      normalize({ title: '00 Steam cover exists', steamAppId: 99999, tracked: false }),
      normalize({ title: 'AA cover review', tracked: false, notes: 'Cover notes' }),
      normalize({ title: 'ZZ cover review', tracked: false }),
    ];
    value.settings.lightweight = true;
    await store.save(value, value.revision);
  });
  await page.reload();
  await page.waitForFunction(
    () => window.checkpointState?.kind === 'main' && !window.checkpointState.busy,
  );
  await click('Settings');
  const coverBefore = coverRequests.length;
  await click('Find missing covers');
  await page.getByText('Closest cover fixture · 2020', { exact: true }).waitFor();
  check(
    (await page.getByText('AA cover review', { exact: true }).isVisible()) &&
      coverRequests.length === coverBefore + 1,
    'cover wizard skips valid Steam covers and works explicitly in lightweight mode',
  );
  await page.screenshot({ path: path.join(evidence, 'web-cover-review-en.png') });
  await click('Next cover');
  await page.getByText('Second cover fixture · 2020', { exact: true }).waitFor();
  check(
    coverRequests.at(-1).excluded.includes('fixture_first'),
    'Next cover dismisses the candidate before searching for another',
  );
  await click('Accept');
  await page.getByText('ZZ cover review', { exact: true }).waitFor();
  await page.getByText('Closest cover fixture · 2020', { exact: true }).waitFor();
  await click('Next game');
  await page.getByText('Review complete', { exact: true }).waitFor();
  const coverReviewed = await page.evaluate(async () => {
    const { BrowserStore } = await import('./store.mjs'),
      store = await new BrowserStore().open();
    return (await store.get('library')).games;
  });
  check(
    coverReviewed[1].igdbCoverImageId === 'fixture_second' &&
      coverReviewed[1].customCover.startsWith('data:image/png;base64,') &&
      coverReviewed[1].notes === 'Cover notes',
    'Accept immediately stores the cover and retains game data',
  );
  check(
    coverReviewed[2].customCover === null &&
      coverReviewed[2].rejectedIgdbCovers.includes('fixture_first'),
    'Next game skips without accepting and remembers the dismissed cover',
  );
  await click('Close');
  await click('Cancel');
  const tabsBeforeNavigation = context.pages().length;
  await click('Settings');
  await page.getByRole('combobox', { name: 'Collection view', exact: true }).selectOption('2');
  await click('Configure shortcuts');
  await page.waitForFunction(
    () => document.querySelector('.page-trail')?.textContent === 'Settings › Configure shortcuts',
  );
  check(
    (await page.locator('.page-trail').textContent()) === 'Settings › Configure shortcuts' &&
      context.pages().length === tabsBeforeNavigation,
    'nested screens keep a clear path in the same browser tab',
  );
  await page.getByRole('textbox', { name: 'Add game', exact: true }).press('Control+Shift+J');
  await click('Back');
  await page.getByText('Customize Checkpoint to suit you.', { exact: true }).waitFor();
  check(
    (await page.getByRole('combobox', { name: 'Collection view', exact: true }).inputValue()) ===
      '2',
    'Back preserves the parent draft without saving the cancelled shortcut',
  );
  await click('Back to collection');
  await page.waitForFunction(() => window.checkpointState.kind === 'main');
  check(
    (await page.locator('.window').count()) === 1,
    'Back to collection restores the same library surface',
  );
  await page.getByRole('searchbox', { name: 'Search games', exact: true }).fill('ZZ');
  await page.getByRole('combobox', { name: 'Filter by status', exact: true }).selectOption('1');
  await click('My list');
  check(
    (await page.getByRole('searchbox', { name: 'Search games', exact: true }).inputValue()) ===
      '' &&
      (await page.getByRole('combobox', { name: 'Filter by status', exact: true }).inputValue()) ===
        '0',
    'My list has filters independent of Library',
  );
  await click('Library');
  check(
    (await page.getByRole('searchbox', { name: 'Search games', exact: true }).inputValue()) ===
      'ZZ' &&
      (await page.getByRole('combobox', { name: 'Filter by status', exact: true }).inputValue()) ===
        '1',
    'Library restores its own filters',
  );
  check(
    await page.locator('.collectionbar').isHidden(),
    'Library does not expose My list collection controls',
  );
  await click('Library');
  check(
    (await page.getByRole('searchbox', { name: 'Search games', exact: true }).inputValue()) ===
      '' &&
      (await page.getByRole('combobox', { name: 'Filter by status', exact: true }).inputValue()) ===
        '0',
    'pressing the active Library button resets filters to its initial screen',
  );
  await click('My list');
  const collection = page.locator('.collectionbar select');
  await collection.selectOption('private');
  await page.getByRole('searchbox', { name: 'Search games', exact: true }).fill('ZZ');
  await click('My list');
  await page.waitForFunction(
    () => window.checkpointState.collection === 'all' && window.checkpointState.search === '',
  );
  check(
    (await collection.inputValue()) === 'all' &&
      (await page.getByRole('searchbox', { name: 'Search games', exact: true }).inputValue()) ===
        '',
    'pressing the active My list button returns from Private to all tracked games',
  );
  await click('Library');
  await page.getByRole('searchbox', { name: 'Search games', exact: true }).fill('');
  await page.getByRole('combobox', { name: 'Filter by status', exact: true }).selectOption('0');
  await click('Add game');
  await input('Game title', 'Optional goal fixture');
  check(
    !(await page.getByRole('checkbox', { name: 'Show in My list', exact: true }).isChecked()),
    'new Library game defaults to staying outside My list',
  );
  await page.getByRole('combobox', { name: 'Goal', exact: true }).selectOption('3');
  await click('Save');
  await page.locator('.game').filter({ hasText: 'Optional goal fixture' }).waitFor();
  check(
    (await page
      .locator('.game')
      .filter({ hasText: 'Optional goal fixture' })
      .locator('.next')
      .count()) === 0,
    'No goal removes the objective line in the browser card',
  );
  await page
    .locator('.game')
    .filter({ hasText: 'Optional goal fixture' })
    .locator('.game-title')
    .click();
  await page.locator('.game-detail-sheet').waitFor();
  check(
    !(await page.locator('.dialog-page').innerText()).includes('Goal:'),
    'No goal removes the objective from game details',
  );
  await click('Edit game');
  await page.getByRole('combobox', { name: 'Goal', exact: true }).selectOption('0');
  await page.getByRole('combobox', { name: 'Status', exact: true }).selectOption('3');
  await click('Save');
  await click('Back to collection');
  const completedCard = page.locator('.game').filter({ hasText: 'Optional goal fixture' });
  await completedCard.waitFor();
  check((await completedCard.locator('.next').count()) === 0, 'finished story hides its objective');
  await completedCard.locator('.game-tools button').nth(0).click();
  await completedCard.locator('.next').waitFor({ state: 'attached' });
  check(
    (await completedCard.locator('.next').textContent()) === 'Story',
    'reopening a finished story restores the saved objective',
  );
  check(errors.length === 0, 'browser flow has no uncaught JavaScript errors');
  await writeFile(
    path.join(evidence, 'report.json'),
    JSON.stringify({ ok: true, checks: checks.length, names: checks }, null, 2),
  );
  console.log('Browser checks passed: ' + checks.length);
} catch (error) {
  await writeFile(
    path.join(evidence, 'cover-debug.json'),
    JSON.stringify(
      await page.evaluate(async () => {
        const { BrowserStore } = await import('./store.mjs'),
          store = await new BrowserStore().open();
        return (await store.get('library')).games.map((g) => ({
          title: g.title,
          cover: g.customCover?.slice(0, 100),
          igdb: g.igdbCoverImageId,
        }));
      }),
    ),
  );
  await page.screenshot({ path: path.join(evidence, 'failure.png') });
  console.error(error);
  await writeFile(
    path.join(evidence, 'report.json'),
    JSON.stringify(
      {
        ok: false,
        checks: checks.length,
        error: String(error),
        pageErrors: errors,
        authCalls,
        diagnostics,
      },
      null,
      2,
    ),
  );
  process.exitCode = 1;
} finally {
  await context.close();
  await browser.close();
  await new Promise((resolve) => server.close(resolve));
}
