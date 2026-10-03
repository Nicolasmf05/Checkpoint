[English](en/PRIVACY.md) · **Español**

# Privacidad

El idioma es una preferencia local. Cambiarlo no traduce ni envía contenido escrito por el usuario.

Checkpoint guarda en el equipo del usuario títulos, estados, notas, tareas, preferencias, carátulas y el último progreso de Steam. No utiliza publicidad ni telemetría.

Desde la versión 0.4.0, entrar en Checkpoint permite usar Amigos mediante Supabase en Irlanda. El registro utiliza un nombre de usuario y una contraseña; no solicita correo real ni confirmación. Internamente, el nombre normalizado se representa como `usuario@accounts.checkpoint.invalid` para Supabase Auth. El identificador de cuenta, nombre visible, código de amigo, solicitudes, amistades, bloqueos y publicaciones se conservan en el servicio. La contraseña se envía por HTTPS al sistema de autenticación; la app no la guarda ni la incluye en copias.

Desde 0.7.6, los juegos de Mi lista son visibles por defecto para amigos aceptados de Checkpoint al entrar en la cuenta. Puedes marcarlos privados antes de guardarlos o después; las retiradas anteriores conservan su privacidad. Las importaciones de Steam no añadidas a Mi lista siguen locales. Los nombres y pertenencias de listas personalizadas son locales. Los campos publicados son: título, plataforma, estado, objetivo, porcentaje manual de historia, contadores de tareas/logros, referencia de carátula y fecha de finalización. Las notas, etiquetas de tareas y detalles individuales de logros quedan en el PC. El nombre visible también se muestra a los participantes de solicitudes pendientes. Los juegos compartidos requieren una amistad aceptada.

Las publicaciones pendientes y las decisiones de publicación se guardan por cuenta en una cola local. Retirar un juego elimina su contenido publicado cuando el servidor confirma el cambio; sin conexión, la última publicación puede seguir visible hasta que se complete la retirada. Se conserva una revisión para evitar que un cliente antiguo vuelva a publicar sin detectar el conflicto. Cerrar sesión conserva publicaciones y pendientes de esa cuenta; no equivale a retirar contenido.

Las carátulas personalizadas compartidas se suben a un bucket privado. Los amigos solo pueden leer imágenes referenciadas por datos a los que tengan permiso. Bloquear o quitar una amistad retira el acceso en el servidor; la interfaz vuelve a consultar periódicamente y elimina la vista anterior cuando detecta la retirada o falla la consulta. Las imágenes privadas consultadas se mantienen en memoria, sin caché en disco del cliente. Esto no elimina capturas, descargas hechas por otros clientes, imágenes sin referencia que permanezcan en Storage ni copias y registros del proveedor. La eliminación completa de una cuenta y sus archivos aún requiere gestión del operador.

Las carátulas automáticas requieren peticiones a la CDN de Steam, que recibe la dirección de conexión y el ID del juego consultado. Las carátulas personalizadas se copian en la carpeta local de datos.

La app conserva los últimos 20 juegos eliminados en un historial local de recuperación, con sus notas, tareas y progreso. Se mantienen después de reiniciar y hasta que otras eliminaciones los desplazan del historial. Eliminar un juego no borra inmediatamente sus imágenes de la caché local.

Al solicitar la vinculación y sincronización, el servicio del operador consulta a Steam el identificador autenticado, biblioteca, tiempo jugado y logros visibles. No recibe contraseñas. Las sesiones se mantienen en memoria hasta siete días; la caché de datos consultados dura 15 minutos. Las definiciones de logros, que no identifican usuarios, se cachean 24 horas. Desvincular revoca la sesión y elimina la caché del usuario en esa instancia.

El operador debe completar el nombre, contacto y país de alojamiento en las variables del servicio antes de distribuirlo. `/privacy` publica estos datos. El proveedor de alojamiento puede conservar registros de conexión según su propia política.

La biblioteca local permanece después de desvincular o desinstalar. Puedes exportarla desde Ajustes y eliminar la carpeta `%LOCALAPPDATA%\Checkpoint` cuando la app esté cerrada para retirar todos los datos locales. Las copias exportadas son archivos propios del usuario, incluyen sus notas y deben compartirse solo cuando lo desee.

Las sesiones de Steam y Checkpoint se protegen mediante DPAPI de Windows para la cuenta del usuario actual. Los tokens no se incluyen en los archivos exportados. Checkpoint es una aplicación independiente de Valve.

Las copias completas `.checkpoint` contienen la colección actual y sus carátulas personalizadas; no contienen sesiones, cola social de la cuenta ni historial de juegos eliminados. Los JSON compatibles incluyen la colección sin imágenes. Ningún formato de copia lleva cifrado propio.

En el despliegue Steam de Supabase de 0.6, SteamID, hashes de sesiones/respuestas, biblioteca y logros persisten en un esquema privado en Irlanda. Vinculaciones: 10 minutos; sesiones: siete días; caché del usuario: 15 minutos; definiciones públicas: 24 horas. Los registros caducados se eliminan en consultas posteriores. Desvincular revoca la sesión y limpia su caché de juegos. No se guardan contraseñas ni tokens en texto claro; la clave del operador permanece en los secretos del servidor. Registros/copias siguen la retención de Supabase. La descripción previa en memoria corresponde a la alternativa Node.

La interfaz local utiliza WebView2 y guarda su perfil en `webview-profile` junto a la biblioteca. No carga páginas web remotas; C# realiza las llamadas a Steam/Supabase. El runtime compartido de Microsoft se actualiza por separado según sus ajustes y condiciones.

## Versión web

La web guarda biblioteca privada, ajustes y cola de publicaciones en IndexedDB del navegador, sin sincronización privada entre dispositivos. Las sesiones se guardan en sessionStorage de la pestaña y no se exportan; no utilizan DPAPI. Al borrar los datos del sitio se pierde la biblioteca local: conserva copias JSON. El caché sin conexión contiene archivos públicos de la app, no respuestas privadas del servicio. GitHub Pages sirve los archivos públicos y Supabase mantiene la autenticación y los datos sociales. [Detalles y límites](WEB.md).

Las copias incluyen privacidad, pertenencias y nombres de listas vacías.
