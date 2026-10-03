[English](CHANGELOG.md) · **Español**

# Historial de cambios

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
