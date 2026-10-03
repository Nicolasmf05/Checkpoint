[English](en/STEAM-SERVICE.md) · **Español**

# Servicio de Steam

El código de 0.6 incluye una función de Supabase sin dependencias externas. Node sigue disponible para desarrollo local y no necesita `npm install`.

## Sincronización automática en la aplicación

Con Steam vinculado, Checkpoint consulta la biblioteca y las horas jugadas cada vez que se inicia y cada 30 minutos por defecto. En Ajustes puedes elegir 15, 30, 60 o 120 minutos. No hace falta pulsar Actualizar. Los nuevos juegos aparecen en Biblioteca; añadirlos a Mi lista sigue siendo una decisión manual. Se actualizan los logros de hasta 20 juegos seguidos por ciclo, empezando por los menos recientes. Los estados, notas y progreso de historia manual no se sobrescriben. Sin conexión se conserva lo guardado y se vuelve a intentar en el siguiente ciclo. No se ejecutan dos sincronizaciones a la vez.

## Despliegue en Supabase

1. Aplica `supabase/migrations/202610020002_checkpoint_steam.sql` después de la migración social. Ejecuta `supabase/tests/steam.sql`; las pruebas terminan con ROLLBACK.
2. Despliega `supabase/functions/checkpoint-steam/index.ts` como `checkpoint-steam`. Solo el servidor usa `SUPABASE_URL` y `SUPABASE_SERVICE_ROLE_KEY`; nunca distribuyas la clave privilegiada.
3. Crea personalmente una clave en [Steam](https://steamcommunity.com/dev/apikey), acepta las condiciones de Valve y guárdala como `STEAM_WEB_API_KEY` en Supabase → Edge Functions → Secrets. No la pegues en el chat ni en el código.
4. Desactiva **Verify JWT with legacy secret** solo para esta función. Las rutas públicas de vinculación verifican OpenID con Steam; biblioteca y logros exigen una sesión propia, aleatoria y con caducidad. Auth y permisos sociales siguen independientes.
5. Comprueba que `/health` indica `steamConfigured: true`. Pulsa Vincular Steam en la app y termina tú el acceso en Steam. Si Steam deniega acceso, revisa Detalles de juegos.
6. El endpoint predeterminado es `https://fumdnvvvoiwoiziwtmsu.supabase.co/functions/v1/checkpoint-steam/`. Para distribuir un fork, configura tu propio proyecto y servicio.

Vinculaciones, hashes de sesiones/respuestas, límites y caché persisten en un esquema privado, accesible solo por RPC privilegiada. El sondeo consume la vinculación y crea una sesión en una transacción; si se pierde su respuesta correcta, hay que volver a vincular. Vinculaciones: 10 minutos; sesiones: siete días; registros caducados se eliminan en consultas posteriores. Desvincular revoca la sesión y limpia su caché de juegos. Biblioteca/logros: 15 minutos; definiciones por idioma: 24 horas. Límites compartidos: 30 vinculaciones/minuto, 2.000 peticiones/minuto, 90 peticiones/sesión/minuto y 90.000 consultas Steam/día. No se confía en cabeceras IP del cliente. Revisa capacidad antes de distribución amplia. Registros/copias siguen la retención de Supabase. El callback muestra texto bilingüe porque Supabase convierte HTML a texto.

Pruebas simuladas: `node --test supabase/tests/steam.test.mjs`. La prueba con Steam real requiere configurar el secreto y completar personalmente el inicio de sesión.

## Desarrollo local

1. Crea una clave para tu aplicación desde la [página oficial de Steam](https://steamcommunity.com/dev/apikey). No la introduzcas en el chat ni en el código.
2. Copia `server/.env.example` a `server/.env` y configura `STEAM_API_KEY`.
3. Mantén `PUBLIC_URL=http://127.0.0.1:34871`, `HOST=127.0.0.1`, `PORT=34871`.
4. Ejecuta `npm start` desde `server`.
5. En los ajustes de Checkpoint, introduce `http://127.0.0.1:34871` en Conexión avanzada y pulsa Vincular Steam.
6. Termina tú el inicio de sesión en `steamcommunity.com`; la app recibirá la sesión y ofrecerá la biblioteca.

El servicio debe seguir ejecutándose para sincronizar. Importar no llena el widget: los juegos nuevos aparecen en Biblioteca y puedes activar Mostrar en Mi lista en su ficha.

## Alternativa de alojamiento Node

1. Aloja el servicio en una instancia Node.js o con el Dockerfile incluido, detrás de HTTPS.
2. Configura `PUBLIC_URL` con el **origen HTTPS externo exacto**, `HOST=0.0.0.0` y el puerto requerido por el proveedor.
3. Guarda `STEAM_API_KEY` como secreto del proveedor, nunca como variable del workflow de la app ni en una URL.
4. Completa `OPERATOR_NAME`, `HOSTING_COUNTRY` y `PRIVACY_CONTACT` para que `/privacy` identifique la operación real. Revisa también los registros del proveedor.
5. El proxy debe conservar el camino y los parámetros de `/v1/auth/callback`. No añadas cookies ni registres cabeceras Authorization o URLs completas de autenticación.
6. Comprueba `/health`, `/privacy` y una vinculación real antes de entregar la edición.
7. Genera el paquete con `scripts/Build.ps1 -Installer -ServiceUrl https://tu-servicio.example`.

Solo se usa el host público `api.steampowered.com`, con la clave en `x-webapi-key`. El host para partners no acepta una clave estándar.

## Diseño y límites de Node

- OpenID se verifica directamente contra el proveedor oficial de Steam. Se verifican origen, callback, identidad, campos firmados, nonce y caducidad.
- La app recibe un token aleatorio vinculado al SteamID autenticado; no puede indicar otro SteamID para consultar información.
- El secreto de sondeo de inicio de sesión evita que un tercero obtenga la sesión a partir del callback del navegador.
- Vinculaciones caducan a los 10 minutos; el cliente deja de esperar a los 5. Sesiones duran siete días y se revocan al desvincular.
- Sesiones y caché están en memoria. Reiniciar el servicio obliga a los clientes a vincular de nuevo. Usa **una instancia**; escalar requiere compartir sesiones, caché y límites en un almacén como Redis.
- La biblioteca y los logros se cachean 15 minutos; las definiciones de logros 24 horas. Actualizar el widget no fuerza a Steam a refrescar su propia información.
- 90 consultas por sesión/minuto, 10 vinculaciones por IP/minuto y presupuesto interno de 90.000 llamadas a Steam por día y proceso.
- Detrás de un proxy, el límite por dirección verá el proxy; no se confía ciegamente en `X-Forwarded-For`. Ajusta el límite en el proxy para tráfico público y establece un límite global de egress para varias instancias.
- Se consultan solo juegos de la biblioteca visible del usuario autenticado. Un perfil privado produce un error claro, no una biblioteca vacía ficticia.
- No hay telemetría, notificaciones push, modificación de logros ni almacenamiento de contraseñas.

La cuenta del operador puede tener restricciones para registrar claves. Las condiciones y disponibilidad dependen de Valve. Referencias: [Web API](https://partner.steamgames.com/doc/webapi_overview), [claves](https://partner.steamgames.com/doc/webapi_overview/auth), [OpenID](https://partner.steamgames.com/doc/features/auth), [condiciones](https://steamcommunity.com/dev/apiterms).

## Importación de Steam Families

El servicio solicita licencias familiares junto a los juegos propios y combina los juegos jugados recientemente como fuente complementaria. Los AppID duplicados conservan el mayor tiempo total comunicado. Solo se importan los juegos que Steam expone para el perfil visible del usuario vinculado; no es una enumeración completa de juegos familiares todavía sin jugar. No se importan credenciales ni progreso de otros familiares.

Los logros se solicitan para el SteamID vinculado, también en juegos prestados presentes en la biblioteca visible combinada. Los perfiles privados siguen restringidos. Si falla la consulta de recientes, se conserva la biblioteca propia. La caché dura 15 minutos. El servicio alojado reemplaza automáticamente las cachés antiguas de juegos propios; pulsa Actualizar en el widget y abre Biblioteca. Las versiones actuales del escritorio funcionan sin reinstalar.

El parámetro `include_family_licenses` se utiliza como indicación de compatibilidad; Valve puede ignorarlo o limitar los datos devueltos. La consulta de recientes está documentada por [Valve](https://partner.steamgames.com/doc/webapi/IPlayerService). Sigue pendiente validar una cuenta familiar real; las pruebas HTTP simuladas no demuestran que todos los juegos compartidos sean accesibles para cualquier perfil.
