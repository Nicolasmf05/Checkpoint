// Genera dist/web con interfaz compartida, traducciones y configuración pública.
// El hash del contenido versiona módulos y estilos para mantener coherente la caché del navegador.

import { createHash } from 'node:crypto';
import { readFile, mkdir, copyFile, writeFile, cp, readdir } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..'),
  out = path.join(root, 'dist', 'web');
await mkdir(out, { recursive: true });
const files = [
  'index.html',
  'app.html',
  'presentation.mjs',
  'presentation.css',
  'start.mjs',
  'bridge.mjs',
  'review.mjs',
  'model.mjs',
  'store.mjs',
  'api.mjs',
  'web.css',
  'sw.js',
  'icon.svg',
  'manifest.webmanifest',
];
const generatedFiles = ['presentation-themes.css', 'presentation-themes.mjs'];

// Keep the public theme picker in sync with the application without loading its UI.
// These sources are literal catalogs; incompatible edits fail the build clearly.
function literalCatalog(source, pattern, name) {
  const body = source.match(pattern)?.[1];
  if (!body || body.replace(/'([^'\\]+)'/g, '').replace(/[,\s]/g, ''))
    throw new Error(`Cannot generate presentation themes: incompatible ${name} catalog.`);
  return [...body.matchAll(/'([^'\\]+)'/g)].map((match) => match[1]);
}

async function buildPresentationThemes(english) {
  const [model, bridge, stylesheet] = await Promise.all([
    readFile(path.join(root, 'web', 'model.mjs'), 'utf8'),
    readFile(path.join(root, 'web', 'bridge.mjs'), 'utf8'),
    readFile(path.join(root, 'src', 'Checkpoint.App', 'Web', 'app.css'), 'utf8'),
  ]);
  const ids = literalCatalog(model, /export const themes\s*=\s*\[([\s\S]*?)\];/, 'theme IDs');
  const labels = literalCatalog(
    bridge,
    /const themeLabels\s*=\s*\(\)\s*=>\s*\[([\s\S]*?)\]\.map\(T\);/,
    'theme names',
  );
  if (
    ids.length !== 22 ||
    labels.length !== ids.length ||
    new Set(ids).size !== ids.length ||
    !ids.every((id) => /^[a-z][a-z0-9-]*$/.test(id)) ||
    ids[0] !== 'dark'
  )
    throw new Error('Cannot generate presentation themes: expected 22 unique, aligned themes.');

  const palettes = new Map();
  for (const match of stylesheet.matchAll(
    /:root(?:\[data-theme="([a-z0-9-]+)"\])?\s*\{([^{}]*)\}/g,
  )) {
    const id = match[1] || 'dark';
    if (palettes.has(id))
      throw new Error(`Cannot generate presentation themes: duplicate CSS palette ${id}.`);
    const declarations = new Map();
    for (const declaration of match[2].split(';')) {
      const token = declaration.trim().match(/^(--[a-z][a-z0-9-]*|color-scheme)\s*:\s*(.+)$/);
      if (token) declarations.set(token[1], token[2].trim());
    }
    palettes.set(id, declarations);
  }
  if (palettes.size !== ids.length || ids.some((id) => !palettes.has(id)))
    throw new Error('Cannot generate presentation themes: CSS palettes do not match theme IDs.');

  const baseScheme = palettes.get('dark').get('color-scheme');
  const required = [
    '--surface',
    '--text',
    '--muted',
    '--panel',
    '--input',
    '--line',
    '--accent',
    '--accent-text',
  ];
  const themes = [];
  const rules = [];
  for (const [index, id] of ids.entries()) {
    const declarations = new Map(palettes.get(id));
    if (required.some((token) => !declarations.has(token)))
      throw new Error(
        `Cannot generate presentation themes: ${id} is missing a required color token.`,
      );
    const scheme = declarations.get('color-scheme') || baseScheme;
    const rgb = declarations.get('--surface').split(/\s+/).map(Number);
    if (
      !['light', 'dark'].includes(scheme) ||
      rgb.length !== 3 ||
      rgb.some((channel) => !Number.isInteger(channel) || channel < 0 || channel > 255)
    )
      throw new Error(`Cannot generate presentation themes: invalid scheme or surface in ${id}.`);
    if (typeof english[labels[index]] !== 'string' || !english[labels[index]].trim())
      throw new Error(`Cannot generate presentation themes: missing English name for ${id}.`);
    themes.push({
      id,
      es: labels[index],
      en: english[labels[index]],
      scheme,
      background: '#' + rgb.map((channel) => channel.toString(16).padStart(2, '0')).join(''),
    });
    if (!declarations.has('--accent-ink')) declarations.set('--accent-ink', 'var(--accent)');
    if (!declarations.has('--secondary')) declarations.set('--secondary', 'var(--accent)');
    if (!declarations.has('--secondary-ink'))
      declarations.set('--secondary-ink', 'var(--accent-ink)');
    declarations.set('color-scheme', scheme);
    declarations.set('--theme-bg', 'rgb(var(--surface))');
    declarations.set('--theme-icon-invert', scheme === 'light' ? '0' : '1');
    const selector = id === 'dark' ? ':root' : `:root[data-theme="${id}"]`;
    rules.push(
      `${selector} {\n${[...declarations].map(([key, value]) => `  ${key}: ${value};`).join('\n')}\n}`,
    );
  }
  await Promise.all([
    writeFile(
      path.join(out, generatedFiles[0]),
      '/* Generated from shared Checkpoint theme tokens. */\n' + rules.join('\n\n') + '\n',
    ),
    writeFile(
      path.join(out, generatedFiles[1]),
      '// Generated from shared Checkpoint theme IDs and translations.\nexport const themes = ' +
        JSON.stringify(themes, null, 2) +
        ';\n',
    ),
  ]);
}
for (const file of ['LICENSE', 'ATTRIBUTION.md', 'ATTRIBUTION.es.md'])
  await copyFile(path.join(root, file), path.join(out, file));
for (const file of files) await copyFile(path.join(root, 'web', file), path.join(out, file));
for (const [from, to] of [
  ['app.css', 'app.css'],
  ['app.js', 'ui.js'],
  ['ui-model.mjs', 'ui-model.mjs'],
  ['shortcuts.mjs', 'shortcuts.mjs'],
])
  await copyFile(path.join(root, 'src', 'Checkpoint.App', 'Web', from), path.join(out, to));
const detailAssets = 'assets/game-details';
await cp(
  path.join(root, 'src', 'Checkpoint.App', 'Web', detailAssets),
  path.join(out, detailAssets),
  {
    recursive: true,
  },
);
const detailFiles = (await readdir(path.join(out, detailAssets)))
  .sort()
  .map((file) => `${detailAssets}/${file}`);
const presentationAssets = 'assets/presentation';
await cp(path.join(root, 'web', presentationAssets), path.join(out, presentationAssets), {
  recursive: true,
});
const presentationFiles = (await readdir(path.join(out, presentationAssets)))
  .sort()
  .map((file) => `${presentationAssets}/${file}`);
const imageFiles = [...detailFiles, ...presentationFiles];
const english = {
  ...JSON.parse(
    await readFile(path.join(root, 'src', 'Checkpoint.Core', 'Localization', 'en.json'), 'utf8'),
  ),
  ...JSON.parse(await readFile(path.join(root, 'web', 'en.json'), 'utf8')),
};
await writeFile(path.join(out, 'en.json'), JSON.stringify(english, null, 2));
await buildPresentationThemes(english);
const project = JSON.parse(await readFile(path.join(root, 'supabase', 'project.json'), 'utf8'));
await writeFile(
  path.join(out, 'config.json'),
  JSON.stringify({ url: project.url, publishableKey: project.publishableKey }),
);
const hash = createHash('sha256');
for (const file of [
  ...files,
  ...generatedFiles,
  'ui.js',
  'app.css',
  'ui-model.mjs',
  'shortcuts.mjs',
  'en.json',
  'config.json',
  ...imageFiles,
])
  hash.update(await readFile(path.join(out, file)));
const revision = hash.digest('hex').slice(0, 16);
// Versioned module and stylesheet URLs prevent mixing cached files across deployments.
for (const file of [...files, ...generatedFiles, 'ui.js', 'ui-model.mjs', 'shortcuts.mjs']) {
  if (!/\.(?:mjs|js|html)$/.test(file) || file === 'sw.js') continue;
  let source = await readFile(path.join(out, file), 'utf8');
  source = source.replace(
    /(['"])(\.\/[\w.-]+\.(?:mjs|js))\1/g,
    (_, quote, url) => quote + url + '?v=' + revision + quote,
  );
  if (file.endsWith('.html'))
    source = source.replace(
      /((?:href|src)=")((?:app|web|presentation(?:-themes)?)\.css|(?:start|presentation)\.mjs)(")/g,
      (_, prefix, url, suffix) => prefix + url + '?v=' + revision + suffix,
    );
  await writeFile(path.join(out, file), source);
}
const sw = await readFile(path.join(out, 'sw.js'), 'utf8');
await writeFile(
  path.join(out, 'sw.js'),
  sw
    .replace('checkpoint-web-1', 'checkpoint-web-' + revision)
    .replace(
      'const assets = [',
      'const assets = [' +
        [...imageFiles, ...generatedFiles.map((file) => file + '?v=' + revision)]
          .map((file) => JSON.stringify(file))
          .join(',') +
        ',',
    )
    .replace(/'([\w.-]+\.(?:mjs|js|css))'/g, (_, url) => "'" + url + '?v=' + revision + "'"),
);
await writeFile(path.join(out, '.nojekyll'), '');
console.log('Checkpoint web built: ' + out);
