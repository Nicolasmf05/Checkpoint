# Portada pública de Checkpoint

Este documento cubre únicamente `web/index.html`, `web/presentation.css` y `web/presentation.mjs`. [DESIGN.md](../DESIGN.md) registra sus tokens y componentes; [.impeccable/design.json](../.impeccable/design.json) usa el esquema 2 para metadatos, movimiento, breakpoints y ejemplos aislados. La app web y Windows cargan la misma capa de identidad `app-identity.css`; [DESIGN.md](../DESIGN.md) recoge también su densidad y controles.

## Arquitectura y temas

La portada es HTML/CSS/JavaScript estático y permanece alojada en GitHub Pages. No inicia la biblioteca, no solicita una cuenta y no carga el controlador de la aplicación. Los botones abren la app con el idioma elegido o la release estable disponible para Windows.

`scripts/build-web.mjs` genera `presentation-themes.css` y `presentation-themes.mjs` desde los bloques de tema de `src/Checkpoint.App/Web/app.css`, los identificadores de `web/model.mjs`, los nombres de `web/bridge.mjs` y las traducciones combinadas de Core y web. El build valida el catálogo de 22 temas, incluye ambos archivos en la revisión de contenido y la caché offline, y versiona sus URLs. Los archivos generados no se editan a mano ni contienen estilos de las pantallas de la app.

Los temas son: `dark`, `light`, `midnight`, `ocean`, `forest`, `plum`, `amber`, `contrast`, `cyber-purple`, `electric-blue`, `neon-lime`, `black-red`, `black-orange`, `synthwave`, `blue-white`, `purple-dark`, `emerald-neutral`, `black-white`, `navy-cyan`, `coral-cream`, `orange-charcoal` e `indigo-gray`. `dark` es el valor inicial y el fallback para una preferencia inválida.

La portada guarda el tema en `localStorage` bajo `checkpoint.presentation.theme` y el idioma de sesión bajo `checkpoint.presentation.language`. Los parámetros `lang`, `theme` y `view` permiten compartir el estado; `view` acepta `grid`, `list` y `mini`. Este almacenamiento es independiente de la biblioteca IndexedDB de la app. La demo de tareas no guarda partidas ni modifica la colección. La galería usa capturas nuevas en tema oscuro de una misma colección de ejemplo: lista/cuadrícula de Chromium y Miniatura de WebView2 Windows. Los dos idiomas tienen imágenes propias.

## Recursos y atribución

Los rasters de `web/assets/presentation` llevan procedencia embebida. La colección se renueva desde la app actual; su [pipeline reproducible](SCREENSHOT-PIPELINE.md) mantiene perfiles y fixtures aislados:

| Recurso            | Origen registrado                                                                                              |
| ------------------ | -------------------------------------------------------------------------------------------------------------- |
| `library-es.png`   | Chromium/Playwright real, `grid-es.png`; tema Oscuro, tres juegos de `scripts/fixtures/gallery.mjs`            |
| `library-en.png`   | Chromium/Playwright real, `grid-en.png`; mismo fixture en inglés                                               |
| `list-es.png`      | Chromium/Playwright real, `list-es.png`; viewport 900 × 1200                                                   |
| `list-en.png`      | Chromium/Playwright real, `list-en.png`; mismo fixture en inglés                                               |
| `list.png`         | Alias de `list-es.png`                                                                                         |
| `miniature-es.png` | Windows/WebView2 real, `css-gallery-miniature-es.png`; tres juegos, fase dedicada anterior a regresiones       |
| `miniature-en.png` | Windows/WebView2 real, `css-gallery-miniature-en.png`; mismo modo nativo en inglés                             |
| `portal2.jpg`      | Recurso existente `.qa/figma-portal2.jpg`, aportado para la referencia de ficha; carátula de Portal 2 de Valve |

Las capturas muestran la interfaz real y colecciones de ejemplo. Las carátulas locales de Steam tienen su [origen y propietarios](../scripts/fixtures/covers/SOURCE.md) documentados; su presencia no implica una licencia Creative Commons. No se generaron imágenes con IA para esta portada. Las antiguas capturas WPF se conservan únicamente como [archivo histórico](screenshots/archive/README.md).

Bagel Fat One e Inconsolata se sirven desde los recursos existentes `assets/game-details`, junto con sus archivos SIL Open Font License 1.1. Los iconos y el logo reutilizan los recursos del proyecto. Se conserva la atribución **Checkpoint originally created by Nicolasmf05**, el enlace al repositorio original y los archivos [LICENSE](../LICENSE) y [ATTRIBUTION.md](../ATTRIBUTION.md).

## Identidad compartida y capturas actuales

La versión 0.8.28 carga `src/Checkpoint.App/Web/app-identity.css` en ambos hosts, conservando los estilos base y cambios del colaborador. El build versiona la capa y la añade a la caché offline. El runner `scripts/capture-gallery.mjs` usa fixtures locales, contexto de navegador aislado y evidencia Windows verificada; su manifiesto registra SHA-256, idioma, tema, dimensiones, renderer y procedencia. No recolorea capturas antiguas. Las evidencias históricas WPF se conservan identificadas en el archivo de capturas.

La revisión de la app comprobó 95 casos de temas, cinco estados, iconos, fuentes, tarjetas, formularios, teclado y reflujo; la distribución local ejecutó 219 comprobaciones del controlador WPF y 152 de WebView2 real. Son comprobaciones concretas; no certifican WCAG ni rendimiento.

La revisión focal posterior confirmó también 9 casos móviles en ES/EN a 320 y 390px, con selector legible y viewport utilizable. La revisión final de Impeccable emitió `ship` tras comprobar esos ajustes y la documentación.

## Evidencia de verificación de la portada anterior

La revisión se realizó sobre el snapshot limpio `.qa/presentation-gaming-source`, con las modificaciones pendientes de la aplicación excluidas. El navegador de prueba fue Chromium real sin interfaz gráfica, controlado por Playwright del repositorio.

- `.qa/presentation-gaming/checks.json`: 162 comprobaciones y ningún error; fuentes, 22 temas, reflujo en 1440/1024/768/390/320px, ES/EN, teclado de galería y tareas, menú móvil, texto al 200%, preferencias, enlaces y movimiento reducido.
- `.qa/presentation-gaming/contrast.json`: mediciones de los 22 temas sin incidencias en los elementos y umbrales comprobados.
- `.qa/presentation-gaming-source/.qa/web-browser/report.json`: 157 comprobaciones de navegador con resultado correcto.
- En el mismo snapshot se ejecutaron correctamente 62 pruebas de datos y las comprobaciones de la ficha de juego.

El controlador manual CUA no estuvo disponible por un fallo de su entorno. No se ejecutó Lighthouse ni se presenta una puntuación de rendimiento. Las mediciones de contraste cubren los elementos muestreados, no constituyen una certificación de accesibilidad completa.
