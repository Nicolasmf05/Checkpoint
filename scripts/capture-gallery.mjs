// Marketing captures are separate from regression tests and use an isolated browser profile.
import http from 'node:http';
import path from 'node:path';
import os from 'node:os';
import { createHash } from 'node:crypto';
import { createRequire } from 'node:module';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { readFile, writeFile, mkdir, readdir, copyFile, rename, stat } from 'node:fs/promises';
import assert from 'node:assert/strict';
import { gallerySeed, galleryGames } from './fixtures/gallery.mjs';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const args = process.argv.slice(2);
const options = {};
for (let i = 0; i < args.length; i++) {
  const key = args[i];
  if (['--help', '--promote', '--promote-only', '--verify-links'].includes(key))
    options[key] = true;
  else if (
    ['--site', '--output', '--windows-evidence', '--web-evidence', '--provenance-helper'].includes(
      key,
    )
  ) {
    if (!args[i + 1] || args[i + 1].startsWith('--')) throw new Error(`Missing value for ${key}`);
    options[key] = path.resolve(args[++i]);
  } else throw new Error(`Unknown option: ${key}`);
}
if (options['--help']) {
  console.log(`Capture the actual Checkpoint browser UI with three isolated examples, ES/EN and Dark.

node scripts/capture-gallery.mjs [--site dist/web] [--output .qa/gallery]
  [--windows-evidence <WebView2 render directory>] [--web-evidence <browser test directory>]
  [--provenance-helper <impeccable executable>] [--promote]
node scripts/capture-gallery.mjs --promote-only --windows-evidence <render> --web-evidence <browser>
node scripts/capture-gallery.mjs --verify-links

Capture stages images and a manifest; it does not overwrite public resources by default.
Promotion requires successful WebView2/browser reports and real Windows Miniature captures.
No account, personal library, remote artwork, native-window simulation or AI imagery is used.`);
  process.exit(0);
}

const site = options['--site'] || path.join(root, 'dist/web');
const output = options['--output'] || path.join(root, '.qa/gallery');
const manifestPath = path.join(output, 'manifest.json');
const coverFolder = path.join(root, 'scripts/fixtures/covers');
const checksum = (bytes) => createHash('sha256').update(bytes).digest('hex');
function pngPixelPayload(bytes) {
  assert.equal(bytes.subarray(0, 8).toString('hex'), '89504e470d0a1a0a', 'Expected PNG evidence');
  const hash = createHash('sha256');
  for (let position = 8; position < bytes.length;) {
    const length = bytes.readUInt32BE(position);
    const type = bytes.toString('ascii', position + 4, position + 8);
    assert.ok(position + length + 12 <= bytes.length, 'Truncated PNG evidence');
    if (['IHDR', 'PLTE', 'tRNS', 'IDAT'].includes(type))
      hash.update(bytes.subarray(position + 4, position + 8 + length));
    position += length + 12;
  }
  return hash.digest('hex');
}
const relative = (file) => path.relative(root, file).replaceAll('\\', '/');
const confined = (base, target) => {
  const destination = path.resolve(base, target);
  if (!destination.startsWith(base + path.sep)) throw new Error(`Path outside ${base}: ${target}`);
  return destination;
};
const exists = async (file) =>
  stat(file).then(
    () => true,
    () => false,
  );
let helper;
async function resolveHelper() {
  if (helper) return helper;
  const versionFile = path.join(os.homedir(), '.codex/skills/impeccable/scripts/VERSION');
  const version = await readFile(versionFile, 'utf8').then(
    (s) => s.trim(),
    () => '0.1.14',
  );
  const candidates = [
    options['--provenance-helper'],
    process.env.IMPECCABLE_BIN,
    path.join(
      os.homedir(),
      `.impeccable/bin/${version}/impeccable${process.platform === 'win32' ? '.exe' : ''}`,
    ),
  ].filter(Boolean);
  for (const candidate of candidates)
    if (await exists(candidate)) {
      helper = candidate;
      return helper;
    }
  throw new Error(
    'Impeccable provenance helper is required. Pass --provenance-helper or IMPECCABLE_BIN.',
  );
}
async function embed(file, origin) {
  const command = await resolveHelper();
  execFileSync(command, ['embed-prompt', file, '--prompt', origin], { stdio: 'pipe' });
  const stored = execFileSync(command, ['embed-prompt', file, '--read'], { encoding: 'utf8' });
  assert.ok(stored.includes(origin), `Provenance not readable: ${file}`);
}
async function fingerprint() {
  const files = [
    'app.html',
    'app.css',
    'app-identity.css',
    'start.mjs',
    'ui.js',
    'ui-model.mjs',
    'bridge.mjs',
    'model.mjs',
    'store.mjs',
    'web.css',
    'review.mjs',
    'shortcuts.mjs',
    'en.json',
  ];
  const hash = createHash('sha256');
  // Build cache-busters include presentation images. Exclude their generated suffix so
  // embedding this fingerprint into an image cannot create a recursive content hash.
  for (const file of files)
    hash
      .update(file)
      .update((await readFile(path.join(site, file), 'utf8')).replace(/\?v=[a-f\d]{16}/g, ''));
  for (const folder of ['assets/game-details', 'assets/identity'])
    for (const file of (await collectFiles(path.join(site, folder))).sort())
      hash.update(path.relative(site, file)).update(await readFile(file));
  hash.update(await readFile(path.join(root, 'scripts/fixtures/gallery.mjs')));
  for (const { appId } of galleryGames)
    hash.update(await readFile(path.join(coverFolder, `${appId}.jpg`)));
  return hash.digest('hex');
}
async function collectFiles(folder) {
  const files = [];
  for (const item of await readdir(folder, { withFileTypes: true })) {
    const full = path.join(folder, item.name);
    if (item.isDirectory()) files.push(...(await collectFiles(full)));
    else files.push(full);
  }
  return files;
}
async function verifyLinks() {
  const sources = [
    path.join(root, 'README.md'),
    ...(await collectFiles(path.join(root, 'docs'))).filter((f) => f.endsWith('.md')),
  ];
  const failures = [];
  let checked = 0;
  for (const source of sources) {
    const text = await readFile(source, 'utf8');
    for (const match of text.matchAll(/!?\[[^\]]*\]\(([^\s)]+)(?:\s+"[^"]*")?\)/g)) {
      const target = match[1];
      if (/^(https?:|mailto:|#|data:)/.test(target)) continue;
      const file = path.resolve(path.dirname(source), decodeURIComponent(target.split('#')[0]));
      if (!(await exists(file))) failures.push(`${relative(source)} → ${target}`);
      checked++;
    }
  }
  assert.equal(failures.length, 0, `Broken local documentation links:\n${failures.join('\n')}`);
  return checked;
}
async function archiveLegacyCaptures() {
  const folder = path.join(root, 'docs/screenshots');
  let archived = 0;
  for (const name of await readdir(folder)) {
    const wpf = /^(widget-|dialog-|miniature-).*\.png$/.test(name);
    const superseded = /^css-achievements-focus-(es|en)\.png$/.test(name);
    if (!wpf && !superseded) continue;
    const source = confined(folder, name);
    const target = confined(folder, `archive/${wpf ? 'wpf' : 'pre-unified-css'}/${name}`);
    assert.equal(
      await exists(target),
      false,
      `Historical archive already contains ${name}; refusing to overwrite it`,
    );
    await mkdir(path.dirname(target), { recursive: true });
    const pixels = pngPixelPayload(await readFile(source));
    await rename(source, target);
    await embed(
      target,
      `Checkpoint historical ${wpf ? 'WPF 0.6.x' : 'superseded CSS'} capture archived from docs/screenshots/${name}. Retained as historical evidence; it does not represent the current application design. Original pixels retained; capture date not recorded. No AI image generation. Original creator Nicolasmf05.`,
    );
    assert.equal(
      pngPixelPayload(await readFile(target)),
      pixels,
      `Historical pixels changed: ${name}`,
    );
    archived++;
  }
  return archived;
}
if (options['--verify-links']) {
  console.log(`PASS ${await verifyLinks()} local documentation references`);
  process.exit(0);
}

await mkdir(output, { recursive: true });
let manifest;
if (options['--promote-only']) {
  manifest = JSON.parse(await readFile(manifestPath, 'utf8'));
  assert.equal(manifest.ok, true, 'Staged capture manifest did not pass');
  assert.equal(
    manifest.fingerprint,
    await fingerprint(),
    'App/fixtures changed since capture. Capture again.',
  );
  for (const entry of manifest.images)
    assert.equal(
      checksum(await readFile(confined(output, entry.file))),
      entry.sha256,
      `Staged image changed: ${entry.file}`,
    );
} else {
  const require = createRequire(path.join(root, 'web/package.json'));
  const { chromium } = require('playwright');
  const covers = {};
  for (const { appId } of galleryGames)
    covers[appId] =
      'data:image/jpeg;base64,' +
      (await readFile(path.join(coverFolder, `${appId}.jpg`))).toString('base64');
  manifest = {
    version: 1,
    ok: false,
    capturedAt: new Date().toISOString(),
    fingerprint: await fingerprint(),
    fixture: 'scripts/fixtures/gallery.mjs',
    theme: 'dark',
    games: galleryGames.map((g) => g.title),
    images: [],
    checks: [],
  };
  const server = http.createServer(async (req, res) => {
    try {
      const pathname = decodeURIComponent(new URL(req.url, 'http://localhost').pathname);
      const file = confined(site, '.' + (pathname === '/' ? '/index.html' : pathname));
      const types = {
        '.html': 'text/html',
        '.js': 'text/javascript',
        '.mjs': 'text/javascript',
        '.css': 'text/css',
        '.json': 'application/json',
        '.svg': 'image/svg+xml',
        '.ttf': 'font/ttf',
        '.woff2': 'font/woff2',
        '.png': 'image/png',
        '.jpg': 'image/jpeg',
        '.webmanifest': 'application/manifest+json',
      };
      res.setHeader('Content-Type', types[path.extname(file)] || 'application/octet-stream');
      res.end(await readFile(file));
    } catch {
      res.writeHead(404);
      res.end();
    }
  });
  await new Promise((resolve) => server.listen(0, '127.0.0.1', resolve));
  const base = `http://127.0.0.1:${server.address().port}`;
  let browser;
  try {
    browser = await chromium.launch({
      headless: true,
      channel: process.env.CHECKPOINT_BROWSER_CHANNEL || undefined,
    });
    for (const language of ['es', 'en']) {
      const context = await browser.newContext({
        viewport: { width: 900, height: 1200 },
        deviceScaleFactor: 1,
        serviceWorkers: 'block',
        reducedMotion: 'reduce',
        locale: language,
        timezoneId: 'Europe/Madrid',
      });
      const page = await context.newPage();
      const errors = [];
      page.on('pageerror', (error) => errors.push(error.message));
      await context.route('**/*', (route) =>
        new URL(route.request().url()).origin === base ? route.continue() : route.abort(),
      );
      const seed = gallerySeed(language, covers);
      const ready = async (scene = 'list') => {
        await page.locator(scene === 'details' ? '.game-detail-sheet' : '.window').waitFor();
        await page.waitForFunction(() => ['main', 'dialog'].includes(window.checkpointState?.kind));
        await page.evaluate(async () => {
          await document.fonts.ready;
          await Promise.all(
            [...document.images].map((img) =>
              img.complete
                ? Promise.resolve()
                : new Promise((resolve) => {
                    img.onload = resolve;
                    img.onerror = resolve;
                  }),
            ),
          );
          await new Promise((resolve) =>
            requestAnimationFrame(() => requestAnimationFrame(resolve)),
          );
        });
        const check = await page.evaluate(() => ({
          lang: document.documentElement.lang,
          theme: window.checkpointState.theme,
          covers: [...document.images]
            .filter((i) => i.src.startsWith('data:image'))
            .every((i) => i.naturalWidth > 0),
          font: document.fonts.status,
        }));
        assert.equal(check.lang, language);
        assert.equal(check.theme, 'dark');
        assert.equal(check.covers, true);
        assert.equal(check.font, 'loaded');
      };
      const capture = async (scene, destinations) => {
        await ready(scene);
        assert.deepEqual(errors, [], `Page errors in ${language}/${scene}`);
        const file = `${scene}-${language}.png`;
        const target = path.join(output, file);
        await page.screenshot({ path: target });
        const viewport = page.viewportSize();
        const origin = `Checkpoint actual browser UI captured with Playwright Chromium; scene ${scene}; language ${language}; theme dark; viewport ${viewport.width}x${viewport.height}; three isolated illustrative games from scripts/fixtures/gallery.mjs, no user account/data. Local Steam cover fixtures: scripts/fixtures/covers/SOURCE.md. No AI image generation. App snapshot SHA-256 ${manifest.fingerprint}.`;
        await embed(target, origin);
        manifest.images.push({
          file,
          renderer: 'Chromium/Playwright',
          scene,
          language,
          theme: 'dark',
          viewport,
          sha256: checksum(await readFile(target)),
          origin,
          destinations,
        });
        manifest.checks.push(
          `${language}/${scene}: actual UI, isolated seed, local covers, fonts loaded, no page errors, embedded provenance`,
        );
      };
      await page.goto(base + '/app.html?lang=' + language);
      await page.locator('.window').waitFor();
      await page.evaluate(async (library) => {
        const { BrowserStore } = await import('./store.mjs');
        const { normalize } = await import('./model.mjs');
        const store = await new BrowserStore().open();
        library.games = library.games.map(normalize);
        await store.save(library, (await store.get('library'))?.revision || 0);
      }, seed);
      await page.reload();
      await ready();
      assert.equal(
        await page.locator('.game').count(),
        3,
        'Marketing collection must contain exactly three games',
      );
      await capture('list', [
        `docs/screenshots/readme-library-${language}.png`,
        `web/assets/presentation/list-${language}.png`,
        ...(language === 'es' ? ['web/assets/presentation/list.png'] : []),
      ]);
      await page.evaluate(async () => {
        const { BrowserStore } = await import('./store.mjs');
        const store = await new BrowserStore().open();
        const data = await store.get('library');
        data.settings.layout = 2;
        await store.save(data, data.revision);
      });
      await page.setViewportSize({ width: 900, height: 920 });
      await page.reload();
      await capture('grid', [
        `docs/screenshots/readme-grid-${language}.png`,
        `web/assets/presentation/library-${language}.png`,
      ]);
      await page.locator('.game').filter({ hasText: 'Portal 2' }).locator('.game-title').click();
      await page.locator('.game-detail-sheet').waitFor();
      await page.setViewportSize({ width: 900, height: 1200 });
      await capture('details', [`docs/screenshots/readme-details-${language}.png`]);
      await context.close();
    }
    manifest.ok = true;
  } catch (error) {
    manifest.error = String(error);
    throw error;
  } finally {
    await writeFile(manifestPath, JSON.stringify(manifest, null, 2) + '\n');
    await browser?.close();
    await new Promise((resolve) => server.close(resolve));
  }
}

// Import successful regression evidence without mixing its mutated fixtures into marketing captures.
async function importEvidence(folder, renderer, reportName, prefix) {
  const report = JSON.parse(await readFile(path.join(folder, reportName), 'utf8'));
  assert.equal(report.ok, true, `${renderer} regression report failed`);
  assert.ok(report.checks > 0, `${renderer} report contains no checks`);
  for (const file of await readdir(folder)) {
    if (!file.startsWith(prefix) || !file.endsWith('.png') || file.includes('failure')) continue;
    const staged = path.join(output, file);
    await copyFile(path.join(folder, file), staged);
    const language = /-es\.png$/.test(file) ? 'es' : 'en';
    const origin = `Checkpoint actual ${renderer} screenshot from successful isolated regression tests (${report.checks} checks). Original evidence ${relative(path.join(folder, file))}; illustrative test games and simulated service replies, no real user account. No AI image generation. Captured output promoted separately from marketing. Original creator Nicolasmf05.`;
    await embed(staged, origin);
    const miniature = /^css-gallery-miniature-(es|en)\.png$/.exec(file);
    const destinations = [`docs/screenshots/${file}`];
    if (miniature) destinations.push(`web/assets/presentation/miniature-${miniature[1]}.png`);
    manifest.images = manifest.images.filter((i) => i.file !== file);
    manifest.images.push({
      file,
      renderer,
      language,
      source: relative(path.join(folder, file)),
      sha256: checksum(await readFile(staged)),
      origin,
      destinations,
    });
  }
  manifest.checks.push(`${renderer}: successful ${report.checks}-check regression report`);
}
if (options['--windows-evidence'])
  await importEvidence(
    options['--windows-evidence'],
    'Windows WebView2/CapturePreviewAsync',
    'web-smoke.json',
    'css-',
  );
if (options['--web-evidence'])
  await importEvidence(options['--web-evidence'], 'Chromium/Playwright', 'report.json', 'web-');
await writeFile(manifestPath, JSON.stringify(manifest, null, 2) + '\n');
if (options['--promote'] || options['--promote-only']) {
  assert.ok(
    options['--windows-evidence'] && options['--web-evidence'],
    'Promotion requires both successful Windows and browser evidence',
  );
  for (const language of ['es', 'en'])
    assert.ok(
      manifest.images.some((i) => i.file === `css-gallery-miniature-${language}.png`),
      `Real Windows Miniature capture missing for ${language}`,
    );
  // Validate all source files and destinations before publishing any image.
  for (const entry of manifest.images) {
    assert.equal(
      checksum(await readFile(confined(output, entry.file))),
      entry.sha256,
      `Image checksum changed: ${entry.file}`,
    );
    for (const destination of entry.destinations) {
      confined(root, destination);
      assert.ok(
        destination.startsWith('docs/screenshots/') ||
          destination.startsWith('web/assets/presentation/'),
        `Unexpected public target ${destination}`,
      );
    }
  }
  for (const entry of manifest.images)
    for (const destination of entry.destinations) {
      const target = confined(root, destination);
      await mkdir(path.dirname(target), { recursive: true });
      await copyFile(confined(output, entry.file), target);
      assert.equal(
        checksum(await readFile(target)),
        entry.sha256,
        `Promotion checksum mismatch: ${destination}`,
      );
    }
  manifest.legacyCapturesArchived = await archiveLegacyCaptures();
  manifest.promotedAt = new Date().toISOString();
  manifest.localReferencesChecked = await verifyLinks();
  await writeFile(
    path.join(root, 'docs/screenshots/SOURCE.json'),
    JSON.stringify(
      {
        version: 1,
        promotedAt: manifest.promotedAt,
        fixture: manifest.fixture,
        fingerprint: manifest.fingerprint,
        note: 'Actual renderer captures; illustrative isolated data. Game artwork rights remain with its owners. Checkpoint originally created by Nicolasmf05.',
        images: manifest.images.flatMap((entry) =>
          entry.destinations.map((destination) => ({
            file: destination,
            renderer: entry.renderer,
            language: entry.language,
            scene: entry.scene,
            theme: entry.theme,
            viewport: entry.viewport,
            sha256: entry.sha256,
            origin: entry.origin,
          })),
        ),
      },
      null,
      2,
    ) + '\n',
  );
  await writeFile(manifestPath, JSON.stringify(manifest, null, 2) + '\n');
  console.log(
    `PROMOTED ${manifest.images.length} real captures; ${manifest.localReferencesChecked} local references verified`,
  );
} else
  console.log(
    `STAGED ${manifest.images.length} real captures at ${relative(output)}; public resources unchanged`,
  );
