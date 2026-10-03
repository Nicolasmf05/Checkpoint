[English](en/FRIENDS-PLAN.md) · **Español**

# Amigos de Checkpoint

## Alcance solicitado

Los amigos son usuarios de Checkpoint. Cada usuario añade a otros dentro de la app y consulta el progreso que estos comparten desde su propia colección de Checkpoint. La amistad y la consulta de progreso funcionan sin una cuenta de Steam.

Steam conserva su papel opcional de importación de biblioteca y actualización de logros del propietario. La pestaña Amigos consulta el servicio de Checkpoint, no la lista de amigos ni los perfiles de Steam.

La versión 0.4.0 incluye cuentas, solicitudes, amistades, publicación selectiva y consulta del progreso mediante Supabase. Se usa usuario y contraseña sin correo ni confirmación, según la preferencia del propietario. No incluye recuperación por correo, avatar editable, invitaciones por enlace o copia de la colección privada en la nube. Las pruebas del cliente usan cuentas simuladas; quedan pendientes las pruebas de registro y amistad con dos usuarios reales. El resto de este documento conserva el alcance previsto y posibles ampliaciones; el estado concreto está en [SUPABASE.md](SUPABASE.md).

## Experiencia prevista

1. Crear una cuenta de Checkpoint y configurar alias y avatar.
2. Añadir a un amigo por un identificador único o una invitación.
3. Aceptar, rechazar o cancelar solicitudes. La amistad solo existe tras la aceptación.
4. Los juegos de Mi lista son visibles por defecto; marcar como privados los que no quieras compartir.
5. Abrir Amigos para ver sus perfiles, juegos compartidos y progreso.
6. Los cambios del propietario se publican automáticamente salvo en juegos privados.
7. Eliminar la amistad, bloquear a alguien o dejar de compartir retira el acceso en el servidor y la caché de la app cuando esta vuelve a contactar con el servicio.

La app local sigue siendo usable sin cuenta y sin conexión. El propietario edita su colección; sus amigos consultan la parte compartida. Todos deben usar el mismo servicio de Checkpoint para encontrarse; la federación entre servidores queda fuera del alcance inicial.

## Datos visibles y progreso

Por cada juego compartido se propone publicar título, plataforma, referencia de carátula, estado, objetivo, progreso permitido, fecha de finalización y fecha de actualización. Los estados son pendiente, jugando, pausado, terminado y abandonado.

- Historia: el propietario marca la finalización. Un porcentaje o capítulos intermedios necesitan campos nuevos y entrada manual; no se deducen de horas o logros.
- Tareas: si el propietario comparte su progreso, mostrar completadas/total. Las etiquetas de las tareas requieren una opción de compartición aparte.
- Logros: mostrar el progreso que el propietario tiene guardado en Checkpoint, si decide compartirlo, junto a su fecha de última sincronización.
- Objetivos personalizados: mostrar el texto y el avance que el propietario permita publicar.
- Notas personales: privadas por defecto y excluidas de la publicación inicial.

Ejemplo: un amigo ve «Hollow Knight · Jugando · Objetivo: terminar la historia · Tareas: 6/10 · Actualizado hace 2 minutos». El porcentaje de tareas describe las tareas, no el porcentaje de historia.

Los distintos tipos de progreso deben llevar etiquetas independientes. Un juego sin tareas o sin progreso numérico muestra su estado; no se presenta como un 0 % ficticio.

Los juegos añadidos manualmente y de otras plataformas también se pueden compartir. Para una comparación automática del mismo juego entre colecciones hace falta una referencia común de catálogo o una asociación explícita; los identificadores locales de juego y los títulos por sí solos no bastan.

## Desarrollo necesario

### Identidad y amistades

- Identificador interno de cuenta independiente de Steam, alias único y avatar.
- Registro, inicio de sesión, recuperación de acceso y cierre de sesiones.
- Elegir un mecanismo de autenticación mantenido y documentado antes de implementarlo. Si usa correo, preparar verificación y entrega de mensajes de recuperación.
- Solicitudes pendientes, aceptación, rechazo, cancelación, eliminación y bloqueo.
- Límites para solicitudes e invitaciones, sin exponer el correo de otros usuarios.
- Permisos comprobados por el servidor en cada consulta, incluidas imágenes privadas.

### Servicio y persistencia

El servicio actual usa sesiones y caché de Steam en memoria. Hace falta almacenamiento persistente de cuentas, perfiles, solicitudes, amistades, bloqueos y juegos compartidos, además de sus revisiones y eliminaciones.

La publicación inicial puede guardar únicamente la parte compartida de la colección. Sincronizar toda la biblioteca privada y restaurarla en otro PC es una función adicional, no un requisito para consultar amigos.

Los cambios solo los acepta el servidor del propietario autenticado. Consultar un juego requiere amistad aceptada, ausencia de bloqueo y permiso de compartición vigente. Cambiar un identificador en una petición no debe permitir consultar o modificar otra cuenta.

Los avatares y carátulas personalizadas compartidos necesitan almacenamiento, validación de formato y tamaño, límites de subida y controles de acceso. Nunca se publican rutas locales del PC.

### Sincronización desde Windows

- Guardar localmente la elección de compartir por cuenta y por juego.
- Publicar una representación limitada a los campos permitidos, evitando subir por accidente notas u otros datos privados.
- Cola persistente de cambios sin conexión, reintentos y confirmación de recepción.
- Operaciones idempotentes, revisiones del servidor y avisos de conflicto para evitar duplicados o sobrescrituras silenciosas.
- Separar sesiones y colas de distintas cuentas al cambiar de usuario.
- Comunicar eliminaciones y retirada de permisos, no solo altas y modificaciones.
- Las importaciones y restauraciones de copias no publican nuevos juegos sin una elección de compartición.
- Mostrar estado de conexión y última actualización. Mientras un cliente esté sin conexión, una copia anterior puede seguir siendo visible en su PC; no se promete revocación instantánea de información ya recibida.

### Interfaz

Pestaña Amigos con búsqueda, solicitudes, favoritos, lista de perfiles y ficha de colección compartida. Mantener tamaño variable, temas y texto legible sobre el fondo translúcido. En anchuras pequeñas, lista y ficha se muestran en pasos separados.

Mostrar un resumen de juegos en curso y terminados, objetivos y progreso compartido. La actividad reciente puede limitarse inicialmente a cambios de estado y finalizaciones; necesita consentimiento y los mismos permisos que el juego correspondiente.

## Alojamiento y distribución

Para que dos PCs se comuniquen cuando uno esté apagado se necesita un servicio accesible por HTTPS con una base de datos persistente y copias de seguridad. GitHub distribuye el código y los paquetes; subir el repositorio no ejecuta ese servicio.

Preparar origen público del servicio, almacenamiento de imágenes si se comparten, configuración de autenticación y, si procede, envío de correos. Identificar operador, contacto, ubicación de almacenamiento y política de conservación. Implementar borrado de cuenta y de datos publicados.

La función Amigos no requiere una clave de Steam. La conexión opcional de logros sigue necesitando la configuración independiente del servicio Steam.

## Orden propuesto y validación

1. Identidad de Checkpoint y almacenamiento persistente.
2. Solicitudes, amistades, bloqueos y pruebas de permisos.
3. Publicación por juego y sincronización de cambios y retiradas.
4. Pestaña Amigos, perfiles y estados de actualización.
5. Validación con dos cuentas y dos PCs, desconexiones, reinicio del servicio y cambio de cuenta.
6. Alojamiento público, pruebas del instalador y nueva distribución.

Probar acceso de una tercera cuenta sin amistad, solicitudes no aceptadas, bloqueos, permisos retirados, identificadores manipulados, imágenes, duplicados de reintentos, eliminación sin conexión y restauración de copias. Verificar que las notas privadas nunca salen del PC en la publicación inicial.

Referencias de implementación: [autenticación de OWASP](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html) y [autorización de OWASP](https://cheatsheetseries.owasp.org/cheatsheets/Authorization_Cheat_Sheet.html). Son guías para el desarrollo; no confirman que estos controles estén ya implementados en Checkpoint.
