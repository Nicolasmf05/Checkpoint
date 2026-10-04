// Genera dist/web con interfaz compartida, traducciones y configuración pública.
// El hash del contenido versiona módulos y estilos para mantener coherente la caché del navegador.

import { createHash } from 'node:crypto';
import { readFile, mkdir, copyFile, writeFile } from 'node:fs/promises';
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
const english = {
  ...JSON.parse(
    await readFile(path.join(root, 'src', 'Checkpoint.Core', 'Localization', 'en.json'), 'utf8'),
  ),
  ...JSON.parse(await readFile(path.join(root, 'web', 'en.json'), 'utf8')),
};
await writeFile(path.join(out, 'en.json'), JSON.stringify(english, null, 2));
const project = JSON.parse(await readFile(path.join(root, 'supabase', 'project.json'), 'utf8'));
await writeFile(
  path.join(out, 'config.json'),
  JSON.stringify({ url: project.url, publishableKey: project.publishableKey }),
);
const hash = createHash('sha256');
for (const file of [
  ...files,
  'ui.js',
  'app.css',
  'ui-model.mjs',
  'shortcuts.mjs',
  'en.json',
  'config.json',
])
  hash.update(await readFile(path.join(out, file)));
const revision = hash.digest('hex').slice(0, 16);
// Versioned module and stylesheet URLs prevent mixing cached files across deployments.
for (const file of [...files, 'ui.js', 'ui-model.mjs', 'shortcuts.mjs']) {
  if (!/\.(?:mjs|js|html)$/.test(file) || file === 'sw.js') continue;
  let source = await readFile(path.join(out, file), 'utf8');
  source = source.replace(
    /(['"])(\.\/[\w.-]+\.(?:mjs|js))\1/g,
    (_, quote, url) => quote + url + '?v=' + revision + quote,
  );
  if (file.endsWith('.html'))
    source = source.replace(
      /((?:href|src)=")((?:app|web|presentation)\.css|(?:start|presentation)\.mjs)(")/g,
      (_, prefix, url, suffix) => prefix + url + '?v=' + revision + suffix,
    );
  await writeFile(path.join(out, file), source);
}
const sw = await readFile(path.join(out, 'sw.js'), 'utf8');
await writeFile(
  path.join(out, 'sw.js'),
  sw
    .replace('checkpoint-web-1', 'checkpoint-web-' + revision)
    .replace(/'([\w.-]+\.(?:mjs|js|css))'/g, (_, url) => "'" + url + '?v=' + revision + "'"),
);
await writeFile(path.join(out, '.nojekyll'), '');
console.log('Checkpoint web built: ' + out);
