# Carátulas de IGDB

**Español** · [English](en/IGDB.md)

Cuando falla la carátula de Steam de un juego añadido a tu lista, Checkpoint busca en IGDB por su título y propone la coincidencia más cercana. También funciona con juegos añadidos manualmente sin ID de Steam. Las carátulas personalizadas existentes se conservan.

## Elegir una carátula

La ventana muestra la imagen, el nombre encontrado y el año disponible. Comprueba la coincidencia: la similitud del nombre no garantiza que sea la edición correcta.

- **Usar esta carátula**: guarda una copia PNG local. En el editor debes pulsar **Guardar** para aplicar la imagen aceptada.
- **No usar esta carátula** o cerrar la propuesta: recuerda el identificador de la imagen para ese juego. No se volverá a proponer, incluso si cancelas el editor o reinicias.
- **Editar juego → Buscar otra carátula en IGDB**: hace una consulta nueva al proveedor y excluye todas las imágenes rechazadas. Puedes ajustar el título antes de buscar. Si no hay otra coincidencia, se informa sin cambiar la carátula actual.

Una propuesta atendida o una búsqueda sin resultados no se repite automáticamente para el mismo título. Cambiar el título permite otra búsqueda, manteniendo los rechazos. Los errores temporales no se guardan como rechazos. Las consultas automáticas se aplazan mientras hay un diálogo abierto; Windows espera a que Checkpoint esté activo. El modo ligero y Miniatura no buscan carátulas. Hay un máximo de 10 búsquedas automáticas por ejecución/pestaña y 200 imágenes rechazadas por juego; alcanzado este último límite, se detienen las búsquedas para conservar todos los rechazos.

Las imágenes aceptadas y los rechazos sobreviven a las copias e importaciones compatibles de Checkpoint. No se publican esos datos en el progreso de amigos. En el navegador el almacenamiento pertenece al navegador y al sitio: borrar sus datos elimina esa información salvo que exportes una copia. Windows y web no sincronizan estos datos entre sí automáticamente.

## Configurar una distribución propia

1. Registra una aplicación confidencial en [Twitch Developers](https://dev.twitch.tv/console/apps). Sigue la [documentación oficial de IGDB](https://api-docs.igdb.com/#account-creation) para obtener Client ID y Client Secret.
2. Guarda `IGDB_CLIENT_ID` e `IGDB_CLIENT_SECRET` en los secretos de Supabase. Nunca en el código, archivos públicos, configuración del navegador ni repositorio.
3. Aplica las migraciones de [Supabase](SUPABASE.md), incluida la tabla de estado del servicio Steam utilizada como caché privada del servidor.
4. Despliega `supabase/functions/checkpoint-covers/index.ts` como `checkpoint-covers`, con `verify_jwt = false` según `supabase/config.toml`. Este endpoint devuelve únicamente metadatos públicos de carátulas y no necesita una sesión de usuario. Los servicios sociales y de Steam mantienen su propia autenticación.
5. Configura en la distribución la URL y clave pública de tu proyecto. Para otra página, adapta la lista explícita de orígenes CORS del servicio.

No necesitas una cuenta de Twitch, una clave personal ni iniciar sesión en Checkpoint para recibir las propuestas en la distribución oficial. Los secretos pertenecen al responsable del servicio. IGDB indica que su acceso gratuito es para uso no comercial; revisa sus condiciones antes de comercializar otra distribución.

## Datos y límites

El título buscado se envía a Supabase y a IGDB, también si el juego es privado para amigos. No se envían notas, contraseñas, sesiones de Steam ni progreso. Los identificadores rechazados viajan a Supabase para filtrar las propuestas; la respuesta contiene nombre, año e identificador público de imagen. El servidor conserva resultados durante 7 días y limita consultas por IP mediante un identificador resumido. La búsqueda manual omite la caché de resultados. Las imágenes pasan por un proxy restringido a IGDB, se validan y se convierten a PNG de hasta 160 × 240 píxeles para uso local. Descargas limitadas a 2 MB; sin URL arbitrarias.

La función aplica límites locales de frecuencia; varias instancias del proveedor pueden compartir la cuota de IGDB. Si el proveedor limita consultas o falla, no se sustituye tu carátula. Fuente de las imágenes: **IGDB**; el código de Checkpoint no concede derechos sobre las carátulas de terceros.
