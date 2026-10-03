[English](en/SCREENSHOTS.md) · **Español**

# Capturas de Checkpoint

Las capturas actuales proceden de WebView2 real. Juegos y progresos son ejemplos; no representan un registro con cuentas reales.

## 0.8.4 — propuestas de carátulas de IGDB

Vista previa con aprobación explícita y rechazo persistente. Imagen de color de prueba; [guía](IGDB.md).

![IGDB](screenshots/css-igdb-cover-en.png)

## 0.8.3 — salir de miniatura con un clic

El botón **Salir de miniatura** aparece encima de la lista y recupera la vista normal anterior sin usar el teclado ni abrir un menú.

![Miniatura con botón para volver](screenshots/css-miniature-es.png)

## 0.8.2 — logros destacados y descripciones

Acceso directo desde cada juego, resumen con progreso y botón para mostrar u ocultar la descripción. Datos de prueba; [guía](ACHIEVEMENTS.md).

![Logros destacados en Windows](screenshots/css-achievements-focus-es.png)

## 0.8.1 — actualizador

[Funcionamiento y edición portable](UPDATES.md). [Captura de la ventana en inglés](en/SCREENSHOTS.md#081--updater), con pruebas aisladas.

## 0.8.0 — detección y logros

[Guía](ACHIEVEMENTS.md)

![Checkpoint](screenshots/css-achievements-es.png)

## Navegador adaptable

La web ocupa el espacio disponible sin modos de tamaño de escritorio. [Capturas de navegador ancho y estrecho con interfaz inglesa](en/SCREENSHOTS.md#responsive-browser-layout), con datos de prueba.

## 0.7.6 — listas y privacidad

Las capturas de Windows y navegador muestran Gestionar listas y la vista Privados con datos de prueba. Están en inglés en la [galería correspondiente](en/SCREENSHOTS.md#076--lists-and-privacy).

## 0.7.4 — ocho temas

En Ajustes → Tema se elige Oscuro, Claro, Medianoche, Océano, Bosque, Ciruela, Ámbar o Alto contraste. La selección muestra los colores al instante y Cancelar recupera el anterior. Las ocho paletas y el selector se muestran en la [galería con interfaz inglesa](en/SCREENSHOTS.md#074--eight-themes). Las capturas siguientes mantienen la interfaz en español.

## 0.7.3 — atajos visibles

La franja inferior muestra las teclas más útiles; F1 abre la guía completa. En Miniatura se accede desde F1 o el menú con clic derecho.

![Guía de atajos en español](screenshots/css-shortcuts-es.png)

## 0.7.1 — ventana completa

La captura inglesa muestra la ventana completa con opacidad al 100 %: [ver captura](screenshots/css-full-window-en.png). El selector también está traducido a español en las capturas siguientes.

## 0.7.0 — interfaz CSS actual

Capturas de HTML/CSS real en WebView2 mediante `CapturePreviewAsync`, con datos aislados. La carátula verde es una imagen local de prueba. Las imágenes WPF siguientes se conservan como archivo histórico.

![Widget en español](screenshots/css-widget-es.png)

| Editor | Miniatura |
| --- | --- |
| ![Editor](screenshots/css-editor-es.png) | ![Miniatura](screenshots/css-miniature-es.png) |

Las capturas inglesas están en la [galería en inglés](en/SCREENSHOTS.md).

## Interfaz WPF histórica (0.6.x)

## Biblioteca y carátulas

![Cuadrícula de carátulas en tema oscuro](screenshots/widget-grid-wide-dark.png)

## Lista y vista compacta

| Lista oscura | Compacta clara |
| --- | --- |
| ![Lista oscura](screenshots/widget-dark.png) | ![Compacta clara](screenshots/widget-compact-light.png) |

## Cuentas y amigos de Checkpoint

| Usuario y contraseña | Progreso compartido de ejemplo |
| --- | --- |
| ![Registro e inicio de sesión](screenshots/widget-friends-login.png) | ![Progreso de un amigo](screenshots/widget-friends-progress.png) |

## Edición y ajustes

![Editor de juego](screenshots/dialog-editor.png)

![Preferencias del widget](screenshots/dialog-settings.png)

## Regenerar

En Windows, `./scripts/Build.ps1 -Installer` ejecuta las pruebas contra el ZIP extraído y guarda las imágenes en `.qa/package-…/render`. Publica únicamente las capturas seleccionadas en `docs/screenshots`, sin subir bases de datos, sesiones o registros locales. Los límites de estas pruebas están en [VALIDATION.md](VALIDATION.md).

## Modo ligero — 0.6.3

Sin carátulas; la colección y el progreso se conservan. Captura de prueba en español.

![Modo ligero](screenshots/widget-lightweight.png)

## Miniatura — 0.6.4

Solo nombre y estado. Se activa en Ajustes o con F6; clic derecho para salir.

![Miniatura](screenshots/widget-miniature.png)

## Menú de juego en Miniatura — 0.6.11

El menú incluye estados, edición, búsqueda, alta y ajustes de ventana; las filas siguen mostrando solo nombre y estado.

![Menú de estados](screenshots/miniature-state-menu.png)

## Teclado en Miniatura — 0.6.6

El contorno indica la fila enfocada. ↑/↓ e Inicio/Fin recorren juegos; Enter/Espacio abren su menú.

![Foco de teclado](screenshots/widget-miniature-keyboard.png)


## Añadir con Miniatura vacía — 0.6.11

Clic derecho en el fondo → Añadir juego, también sin ninguna fila.

![Menú de Miniatura vacía](screenshots/miniature-empty-menu.png)


## Navegación por páginas — 0.6.12

Re Pág/Av Pág adaptan el salto a la altura de Miniatura. Datos de prueba de una colección grande.

![Miniatura con navegación por páginas](screenshots/widget-miniature-pages.png)


## Texto ampliado — 0.6.13

Miniatura con tamaño de texto 18 y filas adaptadas.

![Miniatura con texto ampliado](screenshots/widget-miniature-large-text.png)

## Avisos en español

![Aviso con texto y botón en español](screenshots/dialog-notice-es.png)

## Código de amigo completo

![Cuenta con código de amigo de Checkpoint completo](screenshots/widget-friends-account.png)

## Aplicación web

Capturas del navegador con cuentas y servicios de prueba ficticios.

![Biblioteca web](screenshots/web-library-en.png)

![Atajos configurables](screenshots/web-shortcuts-en.png)

![Amigos de Checkpoint](screenshots/web-friends-en.png)

![Ajustes en pantalla estrecha](screenshots/web-mobile-en.png)
