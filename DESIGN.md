---
name: 'Checkpoint — identidad compartida'
description: 'Sistema visual compartido por la portada y la aplicación web/Windows, basado en la ficha de juego existente.'
colors:
  accent: '#8fb7df'
  accent-text: '#172536'
  surface: 'rgb(32 32 32)'
  text: '#ededed'
  muted: '#b9b9b9'
  panel: '#ffffff08'
  input: '#292929'
  line: '#666666'
typography:
  display:
    fontFamily: "'Checkpoint Bagel', sans-serif"
    fontSize: 'clamp(3.9rem, 7.6vw, 5.75rem)'
    fontWeight: 400
    lineHeight: 1.1
    letterSpacing: '-0.025em'
  headline:
    fontFamily: "'Checkpoint Bagel', sans-serif"
    fontSize: 'clamp(2.5rem, 4.5vw, 3.75rem)'
    fontWeight: 400
    lineHeight: 1.1
    letterSpacing: '-0.025em'
  body:
    fontFamily: "'Checkpoint Inconsolata', ui-monospace, monospace"
    fontSize: '1.125rem'
    fontWeight: 400
    lineHeight: 1.55
  label:
    fontFamily: "'Checkpoint Inconsolata', ui-monospace, monospace"
    fontSize: '0.875rem'
    fontWeight: 400
  control:
    fontFamily: "'Checkpoint Inconsolata', ui-monospace, monospace"
    fontSize: '1rem'
    fontWeight: 700
    lineHeight: 1.35
  metric:
    fontFamily: "'Checkpoint Bagel', sans-serif"
    fontSize: '1.7rem'
    fontWeight: 400
    lineHeight: 1.25
  app-caption:
    fontFamily: "'Checkpoint Inconsolata', ui-monospace, monospace"
    fontSize: '12px'
    fontWeight: 400
  app-summary:
    fontFamily: "'Checkpoint Inconsolata', ui-monospace, monospace"
    fontSize: '13px'
    fontWeight: 400
  app-body:
    fontFamily: "'Checkpoint Inconsolata', ui-monospace, monospace"
    fontSize: '15px'
    fontWeight: 400
  app-card-mobile:
    fontFamily: "'Checkpoint Bagel', sans-serif"
    fontSize: '17px'
    fontWeight: 400
  app-card:
    fontFamily: "'Checkpoint Bagel', sans-serif"
    fontSize: '19px'
    fontWeight: 400
  app-section-small:
    fontFamily: "'Checkpoint Bagel', sans-serif"
    fontSize: '22px'
    fontWeight: 400
  app-editor:
    fontFamily: "'Checkpoint Bagel', sans-serif"
    fontSize: '23px'
    fontWeight: 400
  app-section-short:
    fontFamily: "'Checkpoint Bagel', sans-serif"
    fontSize: '24px'
    fontWeight: 400
  app-metric:
    fontFamily: "'Checkpoint Bagel', sans-serif"
    fontSize: '25px'
    fontWeight: 400
  app-brand:
    fontFamily: "'Checkpoint Bagel', sans-serif"
    fontSize: '26px'
    fontWeight: 400
  app-section:
    fontFamily: "'Checkpoint Bagel', sans-serif"
    fontSize: '28px'
    fontWeight: 400
  app-headline:
    fontFamily: "'Checkpoint Bagel', sans-serif"
    fontSize: '30px'
    fontWeight: 400
rounded:
  sheet: '5px'
  screenshot: '8px'
  miniature: '10px'
spacing:
  small-gap: '8px'
  control-gap: '12px'
  block-gap: '18px'
  stack: '24px'
  card-grid: '28px'
  section: '96px'
  mobile-section: '64px'
components:
  button-primary:
    backgroundColor: '{colors.accent}'
    textColor: '{colors.accent-text}'
    typography: '{typography.control}'
    rounded: '{rounded.sheet}'
    padding: '11px 16px'
  button-secondary:
    backgroundColor: 'transparent'
    textColor: '{colors.text}'
    typography: '{typography.control}'
    rounded: '{rounded.sheet}'
    padding: '11px 16px'
  button-on-accent:
    backgroundColor: '{colors.accent-text}'
    textColor: '{colors.accent}'
    typography: '{typography.control}'
    rounded: '{rounded.sheet}'
    padding: '11px 16px'
  theme-select:
    backgroundColor: '{colors.input}'
    textColor: '{colors.text}'
    rounded: '{rounded.sheet}'
    padding: '4px 8px'
    width: '148px'
  status-chip:
    backgroundColor: '{colors.panel}'
    textColor: '{colors.text}'
    typography: '{typography.label}'
    rounded: '{rounded.sheet}'
    padding: '6px 10px'
  status-chip-playing:
    backgroundColor: '{colors.accent}'
    textColor: '{colors.accent-text}'
    typography: '{typography.label}'
    rounded: '{rounded.sheet}'
    padding: '6px 10px'
  note-card:
    textColor: '{colors.text}'
    rounded: '{rounded.sheet}'
    padding: '12px'
  gallery-tab:
    backgroundColor: '{colors.surface}'
    textColor: '{colors.text}'
    rounded: '{rounded.sheet}'
    padding: '9px 20px'
  gallery-tab-selected:
    backgroundColor: '{colors.accent}'
    textColor: '{colors.accent-text}'
    rounded: '{rounded.sheet}'
    padding: '9px 20px'
  demo-task:
    textColor: '{colors.text}'
    rounded: '{rounded.sheet}'
    padding: '12px'
  navigation:
    textColor: '{colors.text}'
---

# Design System: Checkpoint — identidad compartida

## Overview

**Creative North Star: "La ficha de juego ampliada"**

La portada usa el lenguaje visual de Checkpoint para hablar a quien juega: letras redondeadas, cuerpo monoespaciado, carátulas y bloques reconocibles de tiempo, logros, tareas y notas. Su personalidad viene de los recursos del producto y de ejemplos concretos, con una voz cercana y sin promesas grandilocuentes.

Este sistema cubre la portada pública y el renderizador común de la app web y Windows. La extensión autorizada se aplica en `src/Checkpoint.App/Web/app-identity.css`, cargada después de los estilos base. No incluye publicar ni revertir las modificaciones pendientes del colaborador ni el cambio pausado de logros de amigos. Los tokens del frontmatter representan el tema predeterminado `dark`. Las demás paletas se generan desde la aplicación, sin mantener una segunda colección de valores aquí.

**Key Characteristics:**

- Tipografía de la ficha: Bagel Fat One e Inconsolata, servidas desde el proyecto.
- Fondos y acentos de los 22 temas existentes.
- Cabeceras de color, tarjetas planas y bordes finos.
- Carátulas de ejemplo y vistas recreadas de la app, con datos ilustrativos identificados.
- Controles nativos, foco visible y movimiento breve opcional.

## Colors

El tema oscuro combina un fondo gris carbón, texto claro y el azul suave de Checkpoint. La cabecera del hero y la llamada final usan el par acento/texto de acento; el resto de la página conserva superficies tranquilas para leer las funciones y explorar las vistas de ejemplo.

### Primary

- **Azul de Checkpoint:** el acento identifica acciones, selección de idioma, pestañas seleccionadas, estado «Jugando» y cabeceras de sección.
- **Tinta del acento:** proporciona el texto legible sobre esas superficies y el fondo de la acción principal invertida en el hero.

### Neutral

- **Carbón:** fondo continuo de la página y la navegación sticky.
- **Blanco de lectura:** titulares y contenido principal.
- **Gris de apoyo:** etiquetas, pies de imagen y explicaciones secundarias.
- **Velo de panel:** capa translúcida de la ficha y la sección de vistas.
- **Fondo de control:** selector de tema y áreas que cambian al interactuar.
- **Línea estructural:** bordes de tarjetas, controles y divisores.

**The Shared Palette Rule.** Los valores de los 22 temas proceden de los bloques de tema del CSS compartido de la aplicación. Se cambian en su fuente y se regeneran; no se duplican paletas en la portada ni en este documento.

`scripts/build-web.mjs` genera `presentation-themes.css` con las variables de color y `color-scheme`, y `presentation-themes.mjs` con identificadores, nombres ES/EN, esquema y color de fondo. Los temas claros son `light`, `blue-white`, `emerald-neutral`, `coral-cream` e `indigo-gray`; los demás usan esquema oscuro. La tinta del acento utiliza `--accent-ink` cuando existe y el acento como fallback. El selector adapta también los iconos al esquema.

## Typography

**Display Font:** Bagel Fat One, registrada como `Checkpoint Bagel`, con fallback sans-serif.

**Body Font:** Inconsolata variable, registrada como `Checkpoint Inconsolata`, con fallbacks ui-monospace y monospace.

Los titulares redondeados y las cantidades conectan la portada con la ficha. Inconsolata permite leer etiquetas, acciones y notas con el mismo tono directo del producto. Se conservan los archivos originales y sus licencias SIL Open Font License 1.1.

### Hierarchy

- **Display:** pregunta principal del hero. En tablet se ajusta a `clamp(3.25rem, 8vw, 4.7rem)` y en móvil a `clamp(3.6rem, 14vw, 5rem)`.
- **Headline:** titulares de sección. Los títulos de las vistas tienen su propia escala `clamp(1.9rem, 3vw, 2.65rem)` y en móvil usan `2rem`.
- **Body:** explicaciones breves. Los textos de funciones mantienen anchos de lectura entre 36 y 48 caracteres según el bloque.
- **Label:** metadatos y pies de imagen; las etiquetas principales de controles no se esconden.
- **Control:** acciones principales con énfasis de peso, sin mayúsculas forzadas.
- **Metric:** cantidades de la ficha de ejemplo, con números tabulares. Se ajustan al espacio disponible en cada breakpoint.

**The Product Type Rule.** Bagel se reserva para titulares y cantidades; Inconsolata lleva el contenido operativo. Se conserva esta pareja porque forma parte de la referencia fijada por el usuario.

En la app, los títulos operativos son fijos: marca 26px, sección 30px, formulario 23px y juego 19px (16px en cuadrícula). Los controles usan Inconsolata a 15px, con tamaños compactos explícitos de 12–14px. Bagel no sustituye etiquetas de formulario ni los nombres de Miniatura.

## Layout

El contenedor público alcanza 1200px y deja 80px en total de margen horizontal en escritorio. Los bloques editoriales alternan dos columnas; la ficha de ejemplo usa tres: carátula, estadísticas y diario. Las separaciones de sección se reducen de 96px a 64px en móvil.

Los breakpoints implementados son 1100px, 900px, 640px y 380px. En 900px aparece el menú móvil y el diario pasa debajo de la carátula y los datos. En 640px el hero y las secciones se apilan, y el margen total del contenedor pasa a 32px. En 380px la cabecera divide marca y controles en dos filas, y la ficha pasa a una columna. Los textos y botones permiten reflujo sin depender de una altura fija.

La navegación sticky mantiene el acceso a secciones, idioma y tema. Las anclas dejan un margen superior de 112px para que la cabecera no cubra su destino. La galería reserva dimensiones de imagen y muestra una única vista activa.

La aplicación usa un contenedor flex con regiones de cabecera, navegación, filtros, filas virtualizadas y pie. Las filas mantienen 184px en lista, 126px en compacta y 274px en cuadrícula. En móvil (640px) el selector de lista ocupa una fila completa y los botones van debajo. Los formularios permiten scroll; la Miniatura conserva nombres y estados, sin carátulas ni cabecera.

## Elevation & Depth

La portada es plana: no utiliza sombras de tarjetas, vidrio ni perspectiva. La jerarquía aparece mediante superficies de acento, paneles, bordes y separación. Las recreaciones de la app se muestran de frente y comparten los colores del tema seleccionado.

**The Flat Surface Rule.** El contenido se organiza con color, bordes y espacio. No se añade elevación como sustituto de una jerarquía clara.

La entrada del titular desplaza 10px durante 450ms, solo cuando se permite movimiento. Los botones responden con un desplazamiento de 2px en hover y vuelven al origen al pulsar, durante 160ms. Ambos usan `cubic-bezier(0.16, 1, 0.3, 1)`. El modo de movimiento reducido desactiva animaciones, transiciones y scroll animado; el contenido siempre está disponible.

## Shapes

Los bloques de la ficha, botones, controles, pestañas, estados y notas comparten esquinas discretas. El borde de 1px usa la línea de cada tema. Las vistas recreadas emplean el mismo radio de 5px y proporciones propias de la app.

El encabezado de la ficha redondea solo las esquinas superiores y su cuerpo solo las inferiores, para que se lean como una pieza. Las carátulas mantienen proporción vertical y recorte controlado.

## Components

### Buttons

Acciones legibles y directas. El botón primario usa acento/tinta de acento; el secundario es transparente con borde. Sobre el hero y la llamada final, el primario invierte ese par de colores. La altura mínima de las acciones principales es 48px. Hover, pulsación y foco quedan documentados en el sidecar; el foco ordinario usa un contorno de 3px y separación de 4px, y sobre acento usa su tinta.

### Chips

Los estados son etiquetas informativas, no filtros. Un borde y el fondo del panel definen su forma; «Jugando» usa el par de acento para explicar el estado seleccionado del ejemplo.

### Cards / Containers

Las tarjetas del ejemplo muestran tiempo, visibilidad y progreso. Los paneles de conexión tienen una cabecera de acento y un cuerpo legible. Las notas se presentan como un bloque de texto con borde, sin apariencia de campo editable cuando no hay edición.

### Shared App Layer

`app-identity.css` es la única extensión de estilo del renderizador: botones/campos con radio de 5px, paneles con borde de 1px, navegación activa con acento, títulos Bagel y texto Inconsolata. Se conservan los archivos base del colaborador. Las tintas de enlaces y estados se mezclan con el texto de cada tema para mantener legibilidad; los iconos de la ficha se adaptan mediante máscaras o inversión según la paleta. La app no usa entradas decorativas: solo transiciones de controles de 160ms, desactivadas con movimiento reducido. En móviles estrechos el selector ocupa una fila completa; el shell permite desplazamiento y reserva 180px al área de juegos cuando la cabecera necesita varias filas.

### Inputs / Fields

El selector de tema es un `select` nativo con etiqueta visible y opciones bilingües. Su ancho se reduce de 148px a 125px y 112px al estrechar el viewport; en la cabecera más pequeña ocupa el espacio disponible. Los controles nativos mantienen su teclado y comportamiento de plataforma.

### Navigation

Los enlaces a secciones usan Inconsolata y subrayado en hover. El menú móvil comunica su estado con `aria-expanded`; Escape lo cierra y devuelve el foco. Idioma y tema permanecen accesibles fuera del menú de secciones.

### Gallery

Tres pestañas muestran cuadrícula, lista y Miniatura recreadas con HTML/CSS. La pestaña activa usa el acento; las demás conservan fondo y borde. Flechas, Home y End cambian la vista y su foco. Las vistas siguen el tema y el idioma activos. Tema, idioma y vista se restauran desde la URL.

### Example tasks

Una lista con checkboxes nativos invita a probar la ficha. La tarea marcada se tacha y actualiza el texto de estado del ejemplo. El foco de la fila aparece al usar su control. La demo está identificada y no modifica la colección de la aplicación.

## Do's and Don'ts

### Do:

- **Do** aplicar la identidad común a la portada y al renderizador web/Windows; adaptar densidad a la tarea.
- **Do** regenerar las paletas desde la fuente compartida y mantener nombres ES/EN coherentes.
- **Do** conservar las fuentes, el logo, los iconos existentes y la atribución original de Checkpoint.
- **Do** usar recreaciones HTML/CSS en la portada, aplicarles la paleta activa y señalar los datos de ejemplo; conservar capturas reales para documentación y verificación.
- **Do** mantener controles nativos, foco visible, reflujo y movimiento reducido.

### Don't:

- **Don't** publicar o revertir cambios pendientes del colaborador ni alterar el trabajo pausado de logros de amigos.
- **Don't** inventar testimonios, cifras, integraciones o capacidades del producto.
- **Don't** ocultar contenido detrás de animaciones o convertir la portada en una colección de efectos decorativos.
- **Don't** recolorear capturas para aparentar que representan el tema seleccionado.


### Window adaptation

La ficha ocupa la ventana y centra su contenido en un eje compartido de hasta 960px. Cabecera, servicios, tareas, notas y metadatos comparten alineación y márgenes; hero y servicios permanecen en filas distintas para evitar columnas descompensadas. La carátula conserva su proporción y un máximo de 220px. Hasta 450px se apilan carátula, estadísticas y servicios. Los formularios se adaptan al ancho disponible y el contenido largo se desplaza verticalmente. Miniatura mantiene su geometría y comportamiento anteriores.

### Public product previews

La portada recrea cuadrícula, lista y Miniatura con HTML semántico, carátulas de ejemplo y los mismos tokens de tema. Las vistas traducen su contenido y se adaptan al ancho sin escalar textos rasterizados. Se etiquetan como vistas recreadas; las capturas reales quedan en la documentación, no en la web pública.
