[English](en/STEAM-SERVICE.md) · **Español**

# Servicio de Steam

El servicio Node.js utiliza únicamente módulos integrados. No necesita `npm install`.

## Desarrollo local

1. Crea una clave para tu aplicación desde la [página oficial de Steam](https://steamcommunity.com/dev/apikey). No la introduzcas en el chat ni en el código.
2. Copia `server/.env.example` a `server/.env` y configura `STEAM_API_KEY`.
3. Mantén `PUBLIC_URL=http://127.0.0.1:34871`, `HOST=127.0.0.1`, `PORT=34871`.
4. Ejecuta `npm start` desde `server`.
5. En los ajustes de Checkpoint, introduce `http://127.0.0.1:34871` en Conexión avanzada y pulsa Vincular Steam.
6. Termina tú el inicio de sesión en `steamcommunity.com`; la app recibirá la sesión y ofrecerá la biblioteca.

El servicio debe seguir ejecutándose para sincronizar. Importar no llena el widget: los juegos nuevos aparecen en Biblioteca y puedes activar Mostrar en Mi lista en su ficha.

## Distribución

1. Aloja el servicio en una instancia Node.js o con el Dockerfile incluido, detrás de HTTPS.
2. Configura `PUBLIC_URL` con el **origen HTTPS externo exacto**, `HOST=0.0.0.0` y el puerto requerido por el proveedor.
3. Guarda `STEAM_API_KEY` como secreto del proveedor, nunca como variable del workflow de la app ni en una URL.
4. Completa `OPERATOR_NAME`, `HOSTING_COUNTRY` y `PRIVACY_CONTACT` para que `/privacy` identifique la operación real. Revisa también los registros del proveedor.
5. El proxy debe conservar el camino y los parámetros de `/v1/auth/callback`. No añadas cookies ni registres cabeceras Authorization o URLs completas de autenticación.
6. Comprueba `/health`, `/privacy` y una vinculación real antes de entregar la edición.
7. Genera el paquete con `scripts/Build.ps1 -Installer -ServiceUrl https://tu-servicio.example`.

Solo se usa el host público `api.steampowered.com`, con la clave en `x-webapi-key`. El host para partners no acepta una clave estándar.

## Diseño y límites

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
