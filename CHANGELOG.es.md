[English](CHANGELOG.md) · **Español**

# Historial de cambios

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
