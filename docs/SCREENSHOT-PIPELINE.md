# Capturas reproducibles / Reproducible captures

Las capturas públicas salen de la interfaz real. La galería editorial y las evidencias funcionales tienen ejecuciones distintas: una prueba puede añadir juegos, cambiar ajustes y simular respuestas de servicios; esos datos no se reutilizan como la colección de presentación.

## Galería editorial

`scripts/capture-gallery.mjs` abre Chromium mediante Playwright, sirve `dist/web` en un puerto local libre y crea perfiles efímeros separados para español e inglés. Guarda el fixture con `BrowserStore` y la normalización real, recarga la app y espera a que se carguen fuentes e imágenes. No modifica el DOM ni los estilos para la captura. No usa bibliotecas, sesiones o cuentas personales; bloquea las peticiones externas.

- Seed: [scripts/fixtures/gallery.mjs](../scripts/fixtures/gallery.mjs). Hollow Knight, Hades y Portal 2; textos traducidos, tema Oscuro, tiempos y progreso ilustrativos.
- Carátulas: [scripts/fixtures/covers/SOURCE.md](../scripts/fixtures/covers/SOURCE.md). JPEG locales de Steam ya disponibles en el proyecto. Sus derechos pertenecen a los editores; no se atribuyen licencias Creative Commons.
- Lista y ficha: viewport real de 900 × 1200; cuadrícula: 900 × 920. `deviceScaleFactor: 1`, movimiento reducido y zona horaria Europe/Madrid.
- El manifiesto de staging incluye SHA-256 de imágenes, snapshot de código/recursos y comprobaciones de idioma, tema, fuentes, carátulas y errores de página.

```powershell
node scripts/build-web.mjs
$env:CHECKPOINT_BROWSER_CHANNEL = 'msedge' # optional; omit for installed Chromium
node scripts/capture-gallery.mjs --output .qa/gallery
```

La ejecución anterior solo prepara seis PNG y `.qa/gallery/manifest.json`; no modifica imágenes públicas. Revise los PNG antes de promoverlos. `--site` y `--output` permiten usar otros directorios explícitos.

## Miniatura y evidencias funcionales

**Miniatura es un modo exclusivo de Windows.** Las imágenes `css-gallery-miniature-es/en.png` se obtienen del paquete de Windows con `CoreWebView2.CapturePreviewAsync`, en una fase dedicada antes de que las pruebas añadan otros juegos. No se imita el estado nativo en la app web.

Antes de ejecutar las pruebas del paquete, configure las mismas carátulas locales:

```powershell
$env:CHECKPOINT_GALLERY_COVERS = (Resolve-Path scripts/fixtures/covers).Path
./scripts/Build.ps1 -Installer
```

`scripts/Verify-WebInterface.ps1` también permite verificar un ejecutable ya extraído; crea su propio directorio de datos aislados y escribe `.qa/web-package-…/render/web-smoke.json`. Solo se aceptan capturas de un informe con `ok: true` y comprobaciones realizadas.

Las pruebas Chromium del repositorio generan `.qa/web-browser/report.json` y los PNG `web-*`. En Windows puede utilizarse `CHECKPOINT_BROWSER_CHANNEL=msedge`. Las respuestas de Steam/Supabase son fixtures; los datos de ejemplo no demuestran el acceso a cuentas reales. Los modos de ventana no se simulan en navegador.

## Promoción y procedencia

```powershell
node scripts/capture-gallery.mjs --promote-only `
  --windows-evidence .qa/web-package-ID/render `
  --web-evidence .qa/web-browser
node scripts/capture-gallery.mjs --verify-links
```

Cambie `ID` por la ejecución verificada que utiliza el mismo diseño. La promoción exige los dos informes correctos, ambas Miniaturas reales, un snapshot de app/fixtures sin cambios y SHA-256 intactos de los PNG preparados. El runner usa el helper nativo Impeccable `embed-prompt`, comprueba la lectura de procedencia y puede recibir `--provenance-helper` o `IMPECCABLE_BIN` para localizarlo.

El README conserva `readme-library-*`, `readme-grid-*` y `readme-details-*`. La portada usa `library-es/en.png`, `list-es/en.png` y `miniature-es/en.png`; `list.png` permanece como alias del español. Los PNG `css-*` y `web-*` de documentación se renuevan desde las pruebas exitosas. `docs/screenshots/SOURCE.json` registra las imágenes promovidas; el manifiesto completo se conserva en staging. Tras integrar imágenes se vuelve a construir y verificar la web para actualizar su revisión offline.

Los antiguos PNG WPF y dos capturas de logros sin productor vigente se trasladan, sin cambiar sus píxeles, al [archivo histórico](screenshots/archive/README.md). La galería actual no los presenta como la app vigente. No se generan imágenes con IA para este pipeline. Se conserva la atribución original de Checkpoint y los derechos de las carátulas.

## English

`capture-gallery.mjs` stages actual Chromium UI screenshots in separate temporary ES/EN profiles with three isolated examples, local covers and the Dark theme. List/details use 900 × 1200 and grid uses 900 × 920. It waits for fonts and artwork and uses the real store/model; it does not restyle the DOM, connect an account or access personal data.

Promotion requires successful browser and Windows WebView2 reports, actual Windows Miniature images in both languages, unchanged app/fixture fingerprints and verified PNG checksums. Marketing captures remain separate from regression data. Every new raster has readable embedded provenance. Current documentation filenames remain stable, and old WPF images are explicitly archived. Rebuild and rerun the web checks after promotion to refresh the offline revision.
