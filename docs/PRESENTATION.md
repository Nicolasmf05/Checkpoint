# Portada pública de Checkpoint

Este documento cubre únicamente `web/index.html`, `web/presentation.css` y `web/presentation.mjs`. [DESIGN.md](../DESIGN.md) registra sus tokens y componentes; [.impeccable/design.json](../.impeccable/design.json) usa el esquema 2 para metadatos, movimiento, breakpoints y ejemplos aislados. Las pantallas y estilos de la aplicación quedan fuera de este sistema.

## Arquitectura y temas

La portada es HTML/CSS/JavaScript estático y permanece alojada en GitHub Pages. No inicia la biblioteca, no solicita una cuenta y no carga el controlador de la aplicación. Los botones abren la app con el idioma elegido o la release estable disponible para Windows.

`scripts/build-web.mjs` genera `presentation-themes.css` y `presentation-themes.mjs` desde los bloques de tema de `src/Checkpoint.App/Web/app.css`, los identificadores de `web/model.mjs`, los nombres de `web/bridge.mjs` y las traducciones combinadas de Core y web. El build valida el catálogo de 22 temas, incluye ambos archivos en la revisión de contenido y la caché offline, y versiona sus URLs. Los archivos generados no se editan a mano ni contienen estilos de las pantallas de la app.

Los temas son: `dark`, `light`, `midnight`, `ocean`, `forest`, `plum`, `amber`, `contrast`, `cyber-purple`, `electric-blue`, `neon-lime`, `black-red`, `black-orange`, `synthwave`, `blue-white`, `purple-dark`, `emerald-neutral`, `black-white`, `navy-cyan`, `coral-cream`, `orange-charcoal` e `indigo-gray`. `dark` es el valor inicial y el fallback para una preferencia inválida.

La portada guarda el tema en `localStorage` bajo `checkpoint.presentation.theme` y el idioma de sesión bajo `checkpoint.presentation.language`. Los parámetros `lang`, `theme` y `view` permiten compartir el estado; `view` acepta `grid`, `list` y `mini`. Este almacenamiento es independiente de la biblioteca IndexedDB de la app. La demo de tareas no guarda partidas ni modifica la colección. La galería conserva el tema original de cada captura y lo explica en su pie.

## Recursos y atribución

Los seis rasters de `web/assets/presentation` llevan su procedencia embebida; se leyó esa metadata durante la documentación:

| Recurso            | Origen registrado                                                                                              |
| ------------------ | -------------------------------------------------------------------------------------------------------------- |
| `library-es.png`   | `docs/screenshots/widget-grid-wide-dark.png`                                                                   |
| `library-en.png`   | `docs/screenshots/widget-grid-wide-dark-en.png`                                                                |
| `list.png`         | `docs/screenshots/widget-dark.png`; ejemplo en español                                                         |
| `miniature-es.png` | `docs/screenshots/widget-miniature.png`                                                                        |
| `miniature-en.png` | `docs/screenshots/widget-miniature-en.png`                                                                     |
| `portal2.jpg`      | Recurso existente `.qa/figma-portal2.jpg`, aportado para la referencia de ficha; carátula de Portal 2 de Valve |

Las capturas muestran la interfaz real y colecciones de ejemplo. Las carátulas pertenecen a sus respectivos editores; su presencia no implica una licencia Creative Commons. No se generaron imágenes con IA para esta portada.

Bagel Fat One e Inconsolata se sirven desde los recursos existentes `assets/game-details`, junto con sus archivos SIL Open Font License 1.1. Los iconos y el logo reutilizan los recursos del proyecto. Se conserva la atribución **Checkpoint originally created by Nicolasmf05**, el enlace al repositorio original y los archivos [LICENSE](../LICENSE) y [ATTRIBUTION.md](../ATTRIBUTION.md).

## Evidencia de verificación

La revisión se realizó sobre el snapshot limpio `.qa/presentation-gaming-source`, con las modificaciones pendientes de la aplicación excluidas. El navegador de prueba fue Chromium real sin interfaz gráfica, controlado por Playwright del repositorio.

- `.qa/presentation-gaming/checks.json`: 162 comprobaciones y ningún error; fuentes, 22 temas, reflujo en 1440/1024/768/390/320px, ES/EN, teclado de galería y tareas, menú móvil, texto al 200%, preferencias, enlaces y movimiento reducido.
- `.qa/presentation-gaming/contrast.json`: mediciones de los 22 temas sin incidencias en los elementos y umbrales comprobados.
- `.qa/presentation-gaming-source/.qa/web-browser/report.json`: 157 comprobaciones de navegador con resultado correcto.
- En el mismo snapshot se ejecutaron correctamente 62 pruebas de datos y las comprobaciones de la ficha de juego.

El controlador manual CUA no estuvo disponible por un fallo de su entorno. No se ejecutó Lighthouse ni se presenta una puntuación de rendimiento. Las mediciones de contraste cubren los elementos muestreados, no constituyen una certificación de accesibilidad completa.
