# Detección de juegos y logros

Checkpoint para Windows revisa los procesos cada 10 segundos mientras permanece abierto, incluso en la bandeja. Al detectar un juego guardado en su biblioteca abre una ventana independiente de logros pendientes, sin activar el foco. No requiere permisos de administrador. Una ejecución no reabre la ventana después de cerrarla; al volver a iniciar el juego se puede abrir otra vez. La detección se puede desactivar en **Ajustes → Detectar juegos y abrir sus logros automáticamente**.

## Acceso directo y descripciones

Desde cada tarjeta de juego, pulsa **Ver logros** o el botón **Logros · completados / total**. Está disponible en la lista, la vista compacta y la cuadrícula, tanto en Windows como en el navegador. La miniatura mantiene solo nombres y estados.

El panel destaca el contador, la barra de progreso y los logros pendientes. Pulsa **Ver descripción** en una tarjeta para consultar cómo conseguir ese logro; **Ocultar descripción** la vuelve a plegar. Las descripciones largas se ajustan al ancho de la ventana. Si el proveedor no da una descripción, se indica claramente.

Los logros secretos no muestran su nombre ni su descripción hasta activar la opción de revelar secretos. Revelar el nombre no despliega automáticamente la descripción. Los títulos y descripciones proporcionados por las APIs conservan el idioma del proveedor.

El contador de la tarjeta y del panel de Windows refleja los objetivos visibles de Checkpoint (Steam, RetroAchievements y manuales), con tus marcas personales. La web muestra el progreso oficial de Steam. El avance de historia o tareas se conserva separado; los amigos siguen recibiendo el progreso oficial.

## Steam

Vincula Steam en Ajustes y guarda el juego con su ID de Steam. Checkpoint lee las carpetas de las bibliotecas instaladas de Steam y reconoce los ejecutables dentro de la carpeta del juego. Si no puede leer esa información o el juego se ejecuta fuera de ella, configura **Ejecutable para detectar** en el editor. Los juegos de una familia de Steam pueden necesitar añadirse manualmente si no aparecen en la API. Los detalles y logros de tu perfil deben estar accesibles para Steam.

La ventana consulta al abrirse y cada 60 segundos mientras siga abierta. El servicio de Steam conserva su caché de progreso hasta 15 minutos: los desbloqueos oficiales pueden tardar en aparecer. Los fallos de conexión conservan los últimos datos.

## RetroAchievements

1. En **Ajustes → Configurar RetroAchievements**, introduce tu usuario y tu **Web API key personal**, obtenida en la configuración de tu cuenta de RetroAchievements. No uses tu contraseña. La clave se guarda cifrada con Windows DPAPI para tu usuario y no se incluye en copias de seguridad, GitHub ni publicaciones de amigos.
2. En el editor del juego, indica su **ID de RetroAchievements** (el número de su página de juego); no es su ID de Steam.
3. Indica el ejecutable, por ejemplo `retroarch.exe`, y un texto del título específico de ese juego. Usar solo el nombre del emulador no distingue sus diferentes juegos. Si el emulador no muestra un título útil, abre los logros manualmente desde el editor.
4. Opcionalmente activa **Contar solo logros en modo Hardcore**. Desactivado cuenta los obtenidos en cualquiera de los dos modos.

Las consultas se hacen directamente a la [API oficial de RetroAchievements](https://api-docs.retroachievements.org/v1/get-game-info-and-user-progress.html), con un intervalo mínimo de 60 segundos por juego. No se escanea ni se calcula el hash de tus ROM y no se cambia la configuración del emulador. Para que RetroAchievements registre desbloqueos oficiales, el emulador necesita su propia conexión compatible con ese servicio. Los títulos y descripciones oficiales se muestran como los devuelve cada proveedor; los controles y avisos siguen el idioma elegido en Checkpoint.

## Tu lista de logros

- **Mostrar solo los pendientes** está activado al abrir.
- **Añadir logro manual** crea un objetivo propio con nombre y descripción.
- **Completado en Checkpoint** permite marcar o desmarcar cualquier objetivo. Las marcas sobreviven a la sincronización.
- **Usar estado de la API** elimina una marca manual de un logro oficial.
- **Quitar de mi lista** oculta un logro oficial o elimina un objetivo manual. **Restaurar logros quitados** recupera los oficiales ocultados.

Estos cambios son personales y no desbloquean ni eliminan logros en Steam/RetroAchievements. Los contadores actuales de Steam y publicaciones para amigos conservan el progreso oficial; las marcas, objetivos propios y credenciales no se publican. Las copias conservan objetivos, marcas, ocultaciones e IDs, pero no las credenciales.

## Navegador y límites

La detección de procesos y esta ventana pertenecen a Windows. Una página de GitHub Pages no puede detectar los programas del PC. Importar/exportar JSON desde la web conserva los nuevos campos de Windows; su panel web de logros continúa mostrando los de Steam. La detección cubre juegos ya guardados, no añade todos los programas abiertos como juegos. Si un proceso está protegido o el ejecutable/título cambia, puedes necesitar ajustar su asociación.

La integración RetroAchievements se prueba con respuestas HTTP simuladas y claves de prueba. No se ha verificado con una clave personal real. No hay cambios de esquema ni secretos nuevos que desplegar en Supabase.
