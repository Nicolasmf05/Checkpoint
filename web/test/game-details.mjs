// Ficha de Figma: persistencia real, datos dinámicos, recursos locales y adaptación.
import http from 'node:http';
import path from 'node:path';
import { readFile, mkdir, writeFile, readdir, stat } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import assert from 'node:assert/strict';
import { chromium } from 'playwright';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const site = path.join(root, 'dist/web');
const evidence = path.join(root, '.qa/game-details');
await mkdir(evidence, { recursive: true });
const server = http.createServer(async (req, res) => {
  try {
    const target = path.resolve(site, '.' + new URL(req.url, 'http://localhost').pathname);
    if (!target.startsWith(site + path.sep)) throw new Error('path');
    const mime = {
      '.html': 'text/html',
      '.js': 'text/javascript',
      '.mjs': 'text/javascript',
      '.css': 'text/css',
      '.json': 'application/json',
      '.svg': 'image/svg+xml',
      '.ttf': 'font/ttf',
      '.png': 'image/png',
    };
    res.setHeader('content-type', mime[path.extname(target)] || 'application/octet-stream');
    res.end(await readFile(target));
  } catch {
    res.writeHead(404);
    res.end();
  }
});
await new Promise((resolve) => server.listen(4174, '127.0.0.1', resolve));
const browser = await chromium.launch({
  headless: true,
  channel: process.env.CHECKPOINT_BROWSER_CHANNEL || undefined,
});
const context = await browser.newContext({
  viewport: { width: 1280, height: 1000 },
  serviceWorkers: 'block',
});
const page = await context.newPage();
let coverFixture;
try {
  coverFixture = await readFile(
    process.env.CHECKPOINT_QA_COVER || path.join(root, '.qa/figma-portal2.jpg'),
  );
} catch {
  coverFixture = Buffer.from(
    'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aZ1sAAAAASUVORK5CYII=',
    'base64',
  );
}
await context.route(
  'https://cdn.cloudflare.steamstatic.com/steam/apps/620/library_600x900.jpg',
  (route) => route.fulfill({ body: coverFixture, contentType: 'image/jpeg' }),
);
const failures = [];
page.on('pageerror', (error) => failures.push(error.message));
page.on('response', (response) => {
  if (response.url().includes('/assets/game-details/') && !response.ok())
    failures.push('asset ' + response.status());
});
const id = 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb';
const open = async () => {
  await page.locator('.game').filter({ hasText: 'Portal 2' }).locator('.game-title').click();
  await page.locator('.game-detail-sheet').waitFor();
  await page.evaluate(() => document.fonts.ready);
};
try {
  await page.goto('http://127.0.0.1:4174/app.html');
  await page.locator('.window').waitFor();
  await page.evaluate(async (id) => {
    const { BrowserStore } = await import('./store.mjs');
    const store = await new BrowserStore().open();
    const library = (await store.get('library')) || {
      revision: 0,
      deleted: [],
      settings: { language: 'es', theme: 'dark' },
    };
    library.settings = {
      ...library.settings,
      language: 'es',
      theme: 'dark',
      lightweight: false,
      gameLists: ['Favoritos', 'Pendientes'],
    };
    library.games = [
      {
        id,
        title: 'Portal 2',
        platform: 'Steam',
        steamAppId: 620,
        status: 4,
        goal: 1,
        tracked: true,
        friendsPrivate: false,
        playtimeMinutes: 690,
        lists: ['Favoritos', 'Pendientes'],
        achievements: Array.from({ length: 10 }, (_, index) => ({
          id: 'a' + index,
          name: 'Logro ' + index,
          unlocked: index < 9,
        })),
        tasks: [
          { title: 'Terminar el modo cooperativo', done: false },
          { title: 'Encontrar todos los secretos', done: true },
          { title: 'Una tarea larga '.repeat(12), done: false },
        ],
        notes: 'Volver al modo cooperativo con mi compañero.',
        addedAt: '2026-10-04T09:42:00Z',
        syncedAt: '2026-10-04T10:42:00Z',
      },
    ];
    await store.save(library, library.revision);
  }, id);
  await page.reload();
  await page.locator('.window').waitFor();
  await open();
  assert.equal(await page.locator('.page-trail').textContent(), 'Ficha del juego · Portal 2');
  assert.equal(
    await page.locator('.game-detail-sheet').getByText('Ficha del juego', { exact: true }).count(),
    0,
  );
  assert.equal(await page.locator('.game-detail-intro .game-actions').count(), 1);
  const secondaryButtons = await page
    .locator('.game-actions button:not(.accent)')
    .evaluateAll((buttons) =>
      buttons.map((button) => {
        const style = getComputedStyle(button);
        return [
          style.backgroundColor,
          style.color,
          style.borderColor,
          style.fontSize,
          style.fontWeight,
        ];
      }),
    );
  assert.equal(secondaryButtons.length, 2);
  assert.deepEqual(secondaryButtons[0], secondaryButtons[1]);
  assert.equal(await page.locator('.game-playtime .heading').textContent(), '11,5 h');
  assert.equal(await page.locator('.game-detail-percent').textContent(), '90%');
  assert.equal(
    await page.locator('.game-detail-progress-line progress').getAttribute('value'),
    '90',
  );
  await page.getByRole('checkbox', { name: 'Terminar el modo cooperativo', exact: true }).check();
  await page
    .getByRole('textbox', { name: 'Notas', exact: true })
    .fill('Notas editadas desde la ficha <script> como texto.');
  await page.waitForFunction(async () => {
    const { BrowserStore } = await import('./store.mjs');
    const store = await new BrowserStore().open();
    const library = await store.get('library');
    return (
      library.games[0].notes === 'Notas editadas desde la ficha <script> como texto.' &&
      library.games[0].tasks[0].done
    );
  });
  await page.getByRole('button', { name: 'Cerrar', exact: true }).click();
  await page.reload();
  await page.locator('.window').waitFor();
  await open();
  assert.equal(
    await page.getByRole('textbox', { name: 'Notas', exact: true }).inputValue(),
    'Notas editadas desde la ficha <script> como texto.',
  );
  assert.equal(
    await page
      .getByRole('checkbox', { name: 'Terminar el modo cooperativo', exact: true })
      .isChecked(),
    true,
  );
  const geometry = [];
  for (const width of [480, 390, 320, 1280]) {
    await page.setViewportSize({ width, height: 1000 });
    const result = await page.locator('.game-detail-sheet').evaluate((sheet) => {
      const viewport = innerWidth;
      const overflow = [...sheet.querySelectorAll('button,textarea,label,.type-card')]
        .filter((node) => {
          const rect = node.getBoundingClientRect();
          return rect.left < -1 || rect.right > viewport + 1;
        })
        .map((node) => node.className);
      return {
        width: viewport,
        overflow,
        horizontal: document.documentElement.scrollWidth > viewport,
        cover: sheet.querySelector('img.cover-preview')?.getBoundingClientRect().toJSON(),
        icons: [
          ...sheet.querySelectorAll(
            '.game-playtime,.game-detail-visibility,.game-detail-progress,.game-detail-panel-heading,.game-detail-steam,.game-detail-retro,.game-detail-added,.game-detail-finished,.game-detail-steam-id,.game-detail-synced,.type-check,.game-detail-top',
          ),
        ].map((node) => ({
          slot: node.className,
          before: getComputedStyle(node, '::before').backgroundImage,
          size: [
            getComputedStyle(node, '::before').width,
            getComputedStyle(node, '::before').height,
          ],
          background: getComputedStyle(node).backgroundImage,
        })),
      };
    });
    assert.deepEqual(result.overflow, [], 'controls fit width ' + width);
    assert.equal(result.horizontal, false, 'page fits width ' + width);
    geometry.push(result);
    await page.locator('.dialog-page').evaluate((node) => node.scrollTo(0, 0));
    await page.screenshot({ path: path.join(evidence, 'sheet-' + width + '.png'), fullPage: true });
  }
  await page.setViewportSize({ width: 480, height: 2400 });
  await page.locator('.dialog-page').evaluate((node) => node.scrollTo(0, 0));
  await page
    .locator('.game-detail-sheet')
    .screenshot({ path: path.join(evidence, 'sheet-full.png') });
  await page.setViewportSize({ width: 1280, height: 1000 });
  await page.getByRole('button', { name: 'Volver arriba', exact: true }).click();
  await page.waitForFunction(() => document.querySelector('.dialog-page').scrollTop < 2);
  await page.getByRole('button', { name: '· Favoritos', exact: true }).click();
  await page.getByText('Favoritos', { exact: true }).first().waitFor();
  await page.getByRole('button', { name: 'Volver', exact: true }).click();
  await page.locator('.game-detail-sheet').waitFor();
  await page.locator('.game-detail-hero img').evaluate((image) => {
    image.src = '/missing-cover.jpg';
  });
  await page.locator('.game-detail-cover-empty').waitFor();
  assert.equal(await page.locator('.game-detail-cover-empty').textContent(), 'Steam');
  for (const file of await readdir(path.join(site, 'assets/game-details')))
    assert.ok((await stat(path.join(site, 'assets/game-details', file))).size > 0, file);
  assert.equal(
    await page.evaluate(
      () =>
        document.fonts.check('30px "Checkpoint Bagel"') &&
        document.fonts.check('15px "Checkpoint Inconsolata"'),
    ),
    true,
  );
  assert.deepEqual(failures, []);
  await writeFile(path.join(evidence, 'geometry.json'), JSON.stringify(geometry, null, 2));
  console.log(
    'PASS Figma sheet: real progress, notes/tasks survive reload, list navigation, fonts/assets, widths 320/390/480/1280, back to top',
  );
} finally {
  await browser.close();
  await new Promise((resolve) => server.close(resolve));
}
