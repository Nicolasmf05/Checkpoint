[English](CHANGELOG.md) · **Español**

# Historial de cambios

## 0.8.29

- La ficha de juego ocupa el ancho de la ventana y reorganiza carátula, progreso y servicios según el espacio disponible. Formularios y metadatos se adaptan sin cambiar el modo Miniatura.

## 0.8.28

- La app de Windows y navegador comparte la identidad de la portada y la ficha: titulares Bagel Fat One, textos y controles Inconsolata, paneles y foco coherentes en los 22 temas existentes.
- Biblioteca, lista, compacta, cuadrícula, formularios, ajustes, cuenta, menús, fichas y Miniatura usan una capa visual común sin modificar los datos ni la geometría de las filas virtualizadas.
- Capturas actuales de las interfaces reales, con carátulas locales y colecciones de ejemplo en ambos idiomas. Las evidencias WPF antiguas quedan identificadas como históricas.

## 0.8.27

- La actualización individual, automática y el repaso comparten la aplicación de resultados. Las consultas simultáneas del mismo juego comparten una petición; actualizar un juego durante otro repaso ya no se ignora. El límite de un minuto se aplica solo a actualizaciones automáticas correctas.
- Consultas con concurrencia limitada, un único reintento para fallos temporales y cancelación de peticiones pendientes ante sesión caducada, límites o fallos generales. Los errores de privacidad de un juego no detienen los demás.
- Validación de respuestas antes de sustituir logros, protección frente a cambios de cuenta o juego durante una consulta y conservación de objetivos manuales, notas, privacidad y progreso parcial.
- La web guarda resultados por lotes y cada dos segundos, con guardado final al terminar o detenerse. Supabase comparte consultas simultáneas, conserva el progreso durante un minuto y evita guardar definiciones inválidas.

## 0.8.26

- Las fichas de juegos integran el diseño de Figma en Windows y en la web, con carátula, tiempo jugado, privacidad, progreso de logros, sincronización, listas y datos del juego. Las tareas y notas se guardan automáticamente.
- La navegación superior conserva Volver, el nombre de la ficha y Volver a la colección. El título y las acciones aparecen en la banda oscura; Ver logros y Editar juego usan el mismo estilo, y se elimina el rótulo repetido Ficha del juego del contenido.
- Las fuentes y los iconos se distribuyen localmente; la ficha se adapta a ventanas estrechas y la versión web los conserva para el uso sin conexión.

## 0.8.25

- La barra de selección de Biblioteca incluye Añadir a Mi lista para varios juegos seleccionados. Aparece cuando alguno todavía no está añadido y conserva la privacidad, las otras listas y el progreso; los juegos que ya estaban en Mi lista se mantienen.
- Volver a pulsar el botón del apartado activo de Biblioteca o Mi lista vuelve a su pantalla inicial, limpiando filtros y selección. Mi lista vuelve a todos sus juegos. Pulsar Amigos de nuevo vuelve desde un amigo o subapartado al menú principal de amigos.
- Las actualizaciones de logros de Steam muestran la causa del fallo en el idioma seleccionado, en lugar de un mensaje genérico o solo el contador. El repaso en segundo plano identifica el primer juego que falla, y una sesión web de Steam caducada pide volver a vincular Steam. Se conserva el progreso guardado cuando hay errores.

## 0.8.24

- El menú de clic derecho de Biblioteca permite añadir juegos directamente a Mi lista. La opción aparece cuando la selección incluye juegos que todavía no están en ella, funciona con varios juegos seleccionados y conserva la privacidad, las otras listas y el progreso.

## 0.8.23

- Biblioteca conserva todos los juegos; Mi lista queda como una selección aparte. Volver a Amigos abre su menú inicial.
- Los grupos de logros permiten seguir un juego conjuntamente con amigos de Checkpoint. Los logros manuales publicados incluyen descripción y estado, identificados como propios de Checkpoint; pueden eliminarse sin borrar los logros de los proveedores.
- Miniatura en Windows mantiene siempre la opacidad mínima y la superposición, recuperando las preferencias normales al salir. Se puede salir directamente de ventana completa y cambiar a Miniatura restaura una ventana pequeña.
- Las fichas de juegos usan una única barra de desplazamiento exterior, muestran las horas en una tarjeta con reloj y permiten seleccionar ejecutables desde el administrador de archivos. Los créditos reconocen a Yus.
- Se reduce el trabajo de renderizado y carátulas cuando la ventana está oculta o en Miniatura. La colección permite priorizar juegos mediante su orden.
- El formato del código y las versiones fijadas de las herramientas facilitan la colaboración y conservan los cambios de CSS del colaborador.
- Los logros de amigos tienen un bloque propio por juego, con contadores grandes de completados, pendientes y barra de progreso. Se indica cuando no hay datos publicados; historia y tareas quedan en segundo plano. No se comparten nuevos datos privados.

## 0.8.22

- Las listas vacías no permiten abrir su ficha desde la colección ni desde Gestionar listas. Al quitar o mover el último juego se cierra la ficha; añadir un juego vuelve a habilitarla. La búsqueda y los filtros no afectan a esta regla.

## 0.8.21

- Opción Sin objetivo. Los objetivos de historia y logros cumplidos desaparecen de tarjetas y fichas sin borrar la elección guardada; al reabrir el progreso vuelven a mostrarse. Los logros vacíos o sin sincronizar no cuentan como completados.

## 0.8.20

- Filtros de estado visibles en Biblioteca y colecciones, también en las fichas de listas. Se combinan con la búsqueda por nombre; cambiar los filtros limpia la selección para evitar acciones sobre juegos ocultos.

## 0.8.19

- Una carátula de IGDB aceptada y guardada puede servir de carátula por defecto para otros usuarios del mismo juego cuando falta la de Steam. Se aplica sin otra confirmación, incluso en Biblioteca, conservando las imágenes personales y los rechazos. Supabase guarda solo texto e IDs de IGDB; las imágenes siguen almacenándose localmente.

## 0.8.18

- Las acciones principales de la ficha aparecen arriba: Jugar, Ver logros y Editar juego. Jugar, guardar y aceptar tienen mayor tamaño y contraste; la ficha web también permite abrir juegos de Steam instalados en el equipo. Los juegos manuales destacan sus logros sin ofrecer un inicio de Steam incorrecto.

## 0.8.17

- Las descripciones ausentes del esquema de Steam se recuperan de la respuesta de logros del jugador cuando está disponible. Se evita la caché anterior sin revocar sesiones. Los logros ocultos siguen protegidos hasta revelarlos; si Steam omite la descripción en ambas respuestas, la interfaz explica la limitación del proveedor.

## 0.8.16

- Minimizar deja Checkpoint en la barra de tareas de Windows por defecto. Ajustes ofrece una opción independiente para ocultar en la bandeja al minimizar; ocultar al cerrar sigue siendo otra elección. El atajo global recupera una ventana minimizada.

## 0.8.15

- Las pantallas de Checkpoint comparten una sola ventana/superficie: ajustes, fichas de juegos/listas, logros, avisos y repasos. Volver y Volver a la colección muestran una ruta clara. Los formularios conservan su pantalla anterior; Miniatura se amplía temporalmente para las fichas y recupera su tamaño. El progreso del repaso sigue visible en las páginas.

## 0.8.14

- El repaso de logros continúa en segundo plano en Windows y en el navegador, con progreso y Detener repaso. Hasta tres consultas simultáneas con inicio espaciado para respetar los límites de Steam. Windows guarda solo el juego actualizado; detener conserva los resultados recibidos y cambios locales.

## 0.8.13 — Juegos importados en Miniatura

Miniatura muestra Biblioteca cuando Mi lista está vacía, para que los juegos recién importados de Steam sigan visibles. Una etiqueta identifica el origen sin cambiar selección ni privacidad. Las listas personalizadas vacías muestran un aviso traducido y una acción Biblioteca en vez de un panel en blanco.

## 0.8.12 — Accesos para desinstalar

El MSI añade accesos al desinstalador de Windows en Inicio y en la carpeta instalada. El nombre aparece en español para Windows en español y en inglés para los demás idiomas. Se mantiene la confirmación de desinstalación y se conservan biblioteca y ajustes. Se documenta aparte la eliminación del portátil.

## 0.8.11 — Cierre completo por defecto

La X cierra Checkpoint para Windows completamente cuando se usan ajustes nuevos. Ocultar en la bandeja sigue siendo opcional en Ajustes; se conservan las preferencias guardadas. Las capturas del README muestran colecciones sencillas de ejemplo con carátulas reales de Steam.

## 0.8.10 — Almacenamiento en la nube solo de texto

Supabase rechaza nuevas subidas de archivos, incluidas las escrituras privilegiadas en Storage. Los archivos existentes se conservan. Windows comparte únicamente datos de juegos y progreso; las carátulas locales y las rutas antiguas de imágenes se excluyen de las publicaciones. La migración y las pruebas con reversión documentan esta política.

## 0.8.9

- Ajustes permite repasar los logros de toda la Biblioteca, incluidos juegos privados y fuera de Mi lista, avanzar juego por juego y actualizar todos los vinculados. Se conservan los cambios manuales.
- El asistente de carátulas comprueba Steam y busca en IGDB juego por juego: Aceptar, Siguiente carátula o Siguiente juego. Guarda al aceptar y recuerda las imágenes descartadas.
- Ambos recorridos están en Windows y en la web. Windows actualiza Steam y RetroAchievements; la web actualiza Steam y muestra los logros de RetroAchievements importados y los objetivos manuales.

## 0.8.8 — 2026-10-04

- Guardar crea la lista escrita y la abre, incluso desde Biblioteca; ya no depende del botón Crear lista por separado.
- Menús de juegos con acciones del juego, las tres listas de destino más recientes y un selector completo.
- Selección múltiple en Biblioteca y listas normales: mover, añadir otra pertenencia, quitar de una lista y cambiar privacidad. Quitar conserva el juego en Biblioteca, notas y logros.
- Fichas de listas con recuentos, progreso, juegos privados, búsqueda, páginas de 50 y acciones individuales o por lotes. Controles completos en español e inglés.

## Web — 2026-10-04

- La dirección principal abre una presentación bilingüe. Abrir Checkpoint entra en la app existente; app.html permite el acceso directo. Volver a la presentación conserva la sesión de la pestaña y los juegos locales. Las PWA instaladas abren directamente la app.

## 0.8.7 — 2026-10-04

- Aspecto de escritorio más sencillo: controles rectos, bordes discretos, colores predeterminados neutros y títulos más pequeños en Windows y navegador. Se conservan los 22 temas.
- Eliminados el eslogan y los títulos promocionales de la interfaz, accesos directos y README.
- Release completa y estable con portable, MSI, código fuente y archivos SHA-256; versiones anteriores conservadas como borradores.

## 0.8.6 — 2026-10-04

- Catorce paletas nuevas para un total de 22 temas: Púrpura cibernético, Azul eléctrico, Lima neón, Negro y rojo, Negro y naranja, Onda sintética, Azul y blanco, Púrpura oscuro, Esmeralda y neutro, Negro y blanco, Marino y cian, Coral y crema, Naranja y carbón e Índigo y gris suave.
- Nombres en español e inglés, vista previa inmediata, persistencia y tratamiento claro/oscuro en Windows y navegador. Tonos de texto derivados para conservar los principales solicitados y su legibilidad.
- Los paquetes nuevos incluyen Checkpoint Attribution License 1.0 y los documentos de crédito al creador. Las publicaciones originales de v0.8.5 y anteriores conservan MIT.

## 0.8.5 — 2026-10-04

- Pulsa la tarjeta, el nombre, la carátula o la fila de Miniatura para abrir la ficha completa; los botones conservan sus acciones directas.
- Estado, objetivo, resumen de logros, tiempo jugado, privacidad, listas, tareas, notas, fechas e identificadores, con botones para editar y ver los logros.
- Fichas en Windows y navegador traducidas, acceso con teclado y pruebas de navegación.

## 0.8.4 — 2026-10-04

- Propuestas de carátulas de IGDB cuando falla Steam, con búsqueda del nombre más parecido y aprobación explícita en Windows y navegador.
- Rechazos persistentes por juego y búsqueda de alternativas nuevas desde el editor. Las imágenes aceptadas se guardan localmente y en las copias compatibles.
- Credenciales de Twitch solo en el servidor, proxy de imágenes restringido, límites y caché de metadatos. Controles, capturas y pruebas en ambos idiomas.

## 0.8.3 — 2026-10-03

- Botón visible **Salir de miniatura** encima de la lista: un clic recupera y guarda la vista normal anterior.
- El botón tiene su propio espacio y no tapa nombres ni estados; desaparece en las vistas normales.
- Pruebas reales de WebView2 en español e inglés de visibilidad, posición y restauración. Capturas y documentación actualizadas.

## 0.8.2 — 2026-10-03

- Botones y contadores de logros directamente en cada juego de Windows y del navegador, también en las vistas compacta y de carátulas.
- Resumen destacado de completados, barra de progreso y tarjetas de logros con títulos más grandes.
- Botón Ver/Ocultar descripción por logro; los nombres y descripciones secretos siguen protegidos hasta que se decide revelarlos.
- Controles, capturas y pruebas en español e inglés; la miniatura conserva su lista de nombres y estados.

## 0.7.6 — 2026-10-03

- Mi lista visible por defecto para amigos aceptados, privacidad antes del primer guardado o después y vista Privados separada.
- Varias listas locales sin duplicar juegos; crear, renombrar, quitar y configurar nuevas incorporaciones privadas.
- Conservar retiradas anteriores y privacidad/listas en las copias; reconciliar operaciones públicas pendientes antes de retirar un juego privado.
- Disponible en Windows y navegador, con interfaz completa en español o inglés.

## 0.7.5 — 2026-10-03

- Atajos locales, de Miniatura, reordenación y globales de Windows configurables, con validación, guardado, restauración y ayuda actualizada.
- App utilizable en GitHub Pages, con almacenamiento local, copias JSON, Steam y amigos de Checkpoint.
- CORS de Steam limitado al origen de Pages; la autenticación se mantiene y las sesiones web no se exportan.

## 0.7.4 — 2026-10-03

- Ocho temas: Oscuro, Claro, Medianoche, Océano, Bosque, Ciruela, Ámbar y Alto contraste.
- Selector con vista previa en toda la interfaz CSS; Guardar conserva la elección y Cancelar recupera el aspecto anterior.
- Compatibilidad con preferencias claras/oscuras antiguas, identificadores desconocidos seguros y opacidad/modo de ventana independientes.
- Catorce pruebas de biblioteca y catorce CSS reales de temas; galería de capturas incluida.

## 0.7.3 — 2026-10-03

- Franja visible de atajos, teclas destacadas en menús, indicaciones y accesibilidad.
- Guía de atajos desde F1 o su botón, traducida por completo; Miniatura ofrece F1 y acceso desde el menú sin añadir controles a la lista.
- La ayuda conserva el foco y se cierra con Esc; se indica si el atajo global está ocupado. Siete pruebas CSS reales nuevas.

## 0.7.2 — 2026-10-03

- Sincronización automática de la biblioteca de Steam, horas jugadas y logros de los juegos seguidos al abrir y en el intervalo configurado (30 minutos por defecto).
- Ajustes explica la sincronización al abrir y periódica. Los errores sin conexión conservan el progreso y se evitan peticiones simultáneas.

## Servicio Steam alojado — 2026-10-03

- Solicitar licencias familiares y combinar juegos recientes con propios, sin duplicar AppID y conservando el tiempo total.
- Los juegos prestados visibles permiten sincronizar logros del usuario vinculado. Se conservan las restricciones de perfiles privados.
- Renovar la caché persistente de bibliotecas antiguas; las versiones actuales usan el cambio sin reinstalar.
- 36 comprobaciones HTTP entre ambas implementaciones; sigue pendiente probar una cuenta familiar real.

## 0.7.1 — 2026-10-03

- Tres modos explícitos en cabecera, Ajustes y menú contextual: Ventana completa, Ventana pequeña y Miniatura.
- La completa ocupa el área de trabajo del monitor al 100 % de opacidad, conservando tamaño, posición y translucidez de la pequeña al volver.
- Modo guardado, compatibilidad con preferencias anteriores y pruebas reales de cambios y ajustes en CSS.

## 0.7.0 — 2026-10-03

- Interfaz visible migrada a HTML/CSS/JavaScript locales en WebView2 transparente: colección, Miniatura, amigos, formularios y avisos propios.
- C# conserva SQLite, Steam, Supabase, bandeja, copias y validaciones existentes; los datos mantienen su formato.
- Filas virtualizadas, menús y orden por teclado, español/inglés coherentes, temas claro/oscuro y carátulas mediante origen interno.
- **Nuevo requisito: Microsoft Edge WebView2 Evergreen Runtime.** .NET sigue incluido; WebView2 se comparte y se instala por separado si falta.
- Pruebas del paquete en WebView2 real, pruebas del modelo de interfaz y nuevas capturas/documentación. Sigue vigente el aviso de descargas sin firma.

## 0.6.16 — 2026-10-03

- Códigos de amigo mostrados y copiados con el nombre completo `checkpoint-` y sus 12 caracteres originales.
- Campo de código ampliado a 23 caracteres; etiquetas y avisos sin abreviaturas en español e inglés.
- Los códigos copiados anteriormente siguen resolviendo la misma cuenta mediante una capa de compatibilidad del servicio. No se modifican cuentas, amistades ni publicaciones.
- Documentación y capturas actualizadas; pruebas de formato, compatibilidad y ventanas de Cuenta en ambos idiomas.

## 0.6.15 — 2026-10-03

- Avisos propios con botones en el idioma de Checkpoint, independientes del idioma de Windows.
- Errores externos con explicaciones traducidas; los mensajes de validación conocidos conservan su traducción.
- Traducidos contadores de tareas recuperadas, publicaciones sin título, opciones del idioma y nombres accesibles.
- Idioma guardado disponible antes de los avisos de inicio y aplicado a la cultura de los hilos.
- Comprobación automática de traducciones y pruebas de ventanas y errores en ambos idiomas.

## 0.6.14 — 2026-10-03

- Salir de miniatura recupera la vista normal anterior: lista, compacta o cuadrícula. Ajustes conserva esa vista al entrar en Miniatura o guardar mientras está activa.
- Los menús de fondo/juego guardan la vista restaurada. F6 conserva el ciclo de cuatro vistas; Buscar sigue abriendo la lista normal.

## 0.6.13 — 2026-10-03

- Ajustes incluye Tamaño de texto en Miniatura, de 12 a 20 y predeterminado 12, guardado sin cambiar otras vistas. Nombres/estados y altura de fila crecen juntos.
- La navegación por páginas mide las filas ampliadas y reduce el salto. Sin dependencias nuevas.

## 0.6.12 — 2026-10-03

- Miniatura admite Re Pág/Av Pág con salto según altura visible y tamaño real de fila, foco de teclado y límites en el primer/último juego.
- Buscar cierra el menú contextual antes de enfocar el buscador visible.
- La prueba de regresión confirma que una actualización conserva el desplazamiento de lectura sin perder virtualización.

## 0.6.11 — 2026-10-03

- Menús de fondo/juego de Miniatura incluyen Añadir juego y el atajo Ctrl+N, también con Mi lista vacía.
- Abre la ficha habitual sin salir de Miniatura; Guardar añade la fila y Cancelar no deja datos. La vista vacía conserva su interfaz mínima.

## 0.6.10 — 2026-10-03

- Menú de juego de Miniatura incluye Editar juego; F2 abre la ficha seleccionada. Notas/tareas y acciones habituales del editor sin cambiar de vista ni añadir controles a las filas.
- Al cerrar la ficha se recupera el foco por identidad si el juego sigue visible; retirarlo de Mi lista limpia la selección.

## 0.6.9 — 2026-10-03

- Ctrl+F abre la búsqueda visible de la colección desde Miniatura o Amigos y selecciona la consulta actual, en lugar de intentar enfocar un campo oculto.
- Menús de fondo/juego de Miniatura incluyen Buscar juego y el atajo Ctrl+F. Buscar vuelve a la lista normal y conserva el tamaño guardado de Miniatura.

## 0.6.8 — 2026-10-02

- Los menús de Miniatura incluyen Mantener siempre visible y Bloquear posición y tamaño, con estado marcado, aplicación inmediata y guardado.
- Fondo y juegos comparten las acciones de ventana. El borde para arrastrar usa cursor normal cuando está bloqueado; las filas siguen mostrando nombre/estado.

## 0.6.7 — 2026-10-02

- Miniatura conserva el juego seleccionado y el foco al actualizar o reordenar, incluso en filas virtualizadas. Actualizar sin foco en la lista no lo adquiere; al retirar el juego se limpia la selección.
- Cambiar de vista y mostrar el widget ajustan la ventana al área de trabajo del monitor actual.

## 0.6.6 — 2026-10-02

- Navegación con ↑/↓/Inicio/Fin y menú de estados con Enter/Espacio en Miniatura.
- Contorno de foco y recuperación del foco al cerrar el menú/cambiar estado; conserva estilos y atajos de otras vistas.

## 0.6.5 — 2026-10-02

- Menú contextual de juego en Miniatura: cinco estados traducidos, estado actual marcado y guardado sin salir de la vista.
- El menú conserva Ajustes/Salir; filas enfocables con nombre accesible de juego/estado.
- Pruebas nativas abren los menús reales, terminan/reabren la historia y comprueban etiquetas inglesas.

## 0.6.4 — 2026-10-02

- Vista Miniatura: solo nombres/estados, sin carátulas ni controles alrededor; tamaño guardado independiente, borde para mover y esquina para redimensionar.
- Activación en Ajustes/F6 y salida mediante clic derecho/F6; estados/menú bilingües, contraste comprobado y capturas nativas.

## 0.6.3 — 2026-10-02

- Modo ligero opcional y persistente: oculta carátulas de colección/amigos, evita nuevas consultas de imágenes y vacía la caché sin borrar juegos/archivos ni cambiar la compartición.
- Restauración al desactivarlo; ajuste bilingüe y cancelación comprobados con pruebas nativas.

## 0.6.2 — 2026-10-02

- Caché de carátulas limitada a un presupuesto estimado de 8 MiB/32 entradas; las tarjetas recicladas liberan imágenes y las vuelven a cargar.
- Sin consulta periódica de progreso de amigos estando oculto/minimizado; se conservan reintentos de publicaciones.
- Solo recursos de framework en inglés/español y avisos bilingües de primer uso dentro de ZIP/MSI.
- Aviso destacado de SmartScreen y política de financiación del certificado; requisitos sin runtime externo/administrador ni Internet para uso manual.

- Preparación para Microsoft Store: MSIX sin firma con identidades separadas de prueba/tienda, iconos reproducibles, validación SDK y comprobación de archivos/arquitectura.
- Guías de publicación y migración en inglés/español; CI genera una prueba de empaquetado. Pendientes certificación y pruebas instaladas.

## 0.6.1 — 2026-10-02

- El ZIP portable se extrae en una sola carpeta `Checkpoint`, con el ejecutable, runtime, configuración y licencias.
- La validación comprueba esa estructura y ejecuta la app desde la carpeta. El MSI recoge el mismo paquete.
- Documentación para distinguir avisos de reputación/editor sin firma de detecciones antivirus. Los paquetes siguen sin firma digital.

## 0.6.0 — 2026-10-02

- Función Steam de Supabase: OpenID verificado, hashes privados persistentes, sondeo atómico, protección contra respuestas repetidas, límites y caché compartidos.
- Endpoint Steam predeterminado y logros en español/inglés con cachés separados.
- Guardado atómico mediante DPAPI y pruebas HTTP nativas. La prueba real requiere el secreto del servidor y el acceso personal a Steam.

## 0.5.0 — 2026-10-02

- Interfaz en español e inglés, selector en Ajustes y cambio inmediato persistente.
- Navegación, formularios, diálogos, ayudas, nombres accesibles y errores propios traducidos, sin alterar contenido del usuario ni publicaciones.
- Documentación y capturas bilingües; carpetas de compilación por versión; comprobación compatible con versiones que incluyen el hash Git.
- Páginas de conexión y privacidad del servicio Steam bilingües, y traducción de sus errores conocidos en el cliente.
- La exportación de código excluye explícitamente los metadatos Git y se ignoran extensiones de claves y sesiones.

## 0.4.0 — 2026-10-02

- Pestaña Amigos conectada al backend de Supabase: cuentas de Checkpoint con usuario y contraseña, independientes de Steam y sin correo ni confirmación.
- Códigos de amigo, solicitudes, aceptación, rechazo, cancelación, retirada de amistad y bloqueo; progreso compartido desde la colección de cada usuario.
- Elección explícita de juegos compartidos, sin publicar notas, nombres de tareas ni detalles de logros. Carátulas personalizadas privadas y carátulas públicas de Steam.
- Cola local persistente por cuenta, reintentos idempotentes, conflictos entre equipos y retirada de publicaciones. Cerrar sesión no retira los juegos ya compartidos.
- Sesiones de Checkpoint protegidas con DPAPI, renovación de tokens y cierre de la sesión del dispositivo.
- Porcentaje opcional y manual de historia, independiente de los contadores de tareas y logros.
- Configuración pública de Supabase incluida en el paquete; las claves privilegiadas permanecen fuera de la app.
- Registro sin confirmación habilitado en el proyecto. Esta versión no ofrece recuperación por correo, avatar editable ni copia de la biblioteca privada en la nube.
- Pruebas del cliente con respuestas simuladas y permisos verificados en el backend real. El servicio Node de Steam conserva su implementación anterior; su traslado a Supabase está pendiente.

## 0.3.0 — 2026-10-02

- Copias completas `.checkpoint` con juegos, notas, tareas, progreso y carátulas personalizadas en un solo archivo; importación compatible con los JSON anteriores.
- Exportación que conserva el archivo anterior si falla y restauración que valida las imágenes antes de cambiar datos. Los juegos existentes conservan sus datos y sus carátulas.
- Deshacer eliminaciones con el botón ↶ o Ctrl+Z; recuperación individual desde Ajustes → Juegos eliminados. Los últimos 20 permanecen disponibles después de reiniciar.
- Conservación de tareas, notas, progreso, estado, favoritos, orden y carátulas al recuperar. Los conflictos de Steam no sobrescriben juegos actuales.
- Ampliación de la base de datos con historial de recuperación; migración de las bibliotecas anteriores sin perder juegos ni ajustes.
- Límites del tamaño descomprimido, validación de rutas y referencias, y rechazo de archivos desconocidos o duplicados en las copias.
- 76 comprobaciones de biblioteca, copias y persistencia; 36 comprobaciones nativas y 15 del servicio de Steam.
- Las pruebas nativas rechazan bibliotecas existentes y sesiones para trabajar siempre con datos aislados.

## 0.2.0 — 2026-10-02

- Cuadrícula de carátulas que adapta sus columnas al ancho del widget, con filas virtualizadas.
- Reordenación desde el asa de cada juego, con indicador de posición y desplazamiento automático al acercarse al borde. Alternativa de teclado con Alt+↑ / Alt+↓.
- Orden persistente que conserva los favoritos arriba y funciona con búsquedas y filtros.
- Selector de las tres vistas en Ajustes y cambio rápido con F6 o el botón inferior.
- Caché de imágenes en memoria limitada a 96 carátulas, deduplicación de descargas simultáneas y espera de diez minutos tras un fallo.
- Comprobación automática del ZIP extraído y 24 comprobaciones de la interfaz y los diálogos reales, incluyendo una colección de 1.003 juegos.
- 37 comprobaciones de biblioteca y persistencia, y 15 pruebas del servicio de Steam.
- Versión central para ejecutables y paquetes, y sumas SHA-256 del código fuente.
- El instalador conserva el acceso de inicio de Windows durante actualizaciones; una desinstalación completa lo retira. Si falta al abrir la app, se restaura cuando el usuario tenía activada esa opción.

## 0.1.0 — 2026-10-02

Primera versión local: widget translúcido para Windows, biblioteca SQLite, carátulas, tareas y objetivos, Steam mediante servicio OpenID, bandeja y paquetes de distribución.
