[English](en/SUPABASE.md) · **Español**

# Backend de Checkpoint en Supabase

## Estado actual

La organización elegida es **Checkpoint**, plan **Free**, en la segunda cuenta de Supabase seleccionada por el propietario. El proyecto **checkpoint** está activo en **Irlanda (`eu-west-1`)**. Referencia: `fumdnvvvoiwoiziwtmsu`. URL de API: `https://fumdnvvvoiwoiziwtmsu.supabase.co`.

La migración `supabase/migrations/202610020001_checkpoint_social.sql` se aplicó mediante el SQL Editor el **2 de octubre de 2026**. Pasaron **23 comprobaciones sociales** y **10 de imágenes y límites de solicitudes**, en la base real con usuarios ficticios y transacciones revertidas. Otras **7 comprobaciones HTTP desde el PC** confirmaron que las cinco tablas y las dos operaciones sociales rechazan el acceso sin sesión (`401`, permiso denegado). Se verificó que no quedaron usuarios ni imágenes de prueba y que el bucket es privado.

El backend social está desplegado y **la app 0.4.0 lo utiliza**: cuentas, solicitudes, amigos, bloqueos y publicación seleccionada de juegos. Las sesiones se protegen con Windows DPAPI y la cola de publicación se guarda por cuenta. La biblioteca local funciona sin iniciar sesión.

Por petición del propietario, el registro está habilitado y **Confirm email está desactivado**, guardado y verificado mediante la API pública de Auth el 2 de octubre de 2026. La interfaz utiliza usuario y contraseña. Auth recibe una dirección interna determinista `usuario@accounts.checkpoint.invalid`; no se recoge correo real, no se envían confirmaciones y no se necesita SMTP. No hay recuperación de contraseña en esta versión.

El servidor de Oracle se ha desactivado: las unidades de Checkpoint están detenidas y deshabilitadas. Se conservan los archivos para recuperación. No recibe el nuevo backend.

## Qué crea la migración

- Perfiles vinculados a Supabase Auth, independientes de Steam, con código de amigo `checkpoint-…`.
- Solicitudes que debe aceptar el destinatario, amistades y bloqueos.
- Publicaciones de juegos elegidas por su propietario, con estado, objetivo y contadores de progreso. Las notas y los títulos de las tareas se rechazan.
- Permisos por fila: acceso propio y juegos compartidos de amigos aceptados. Una solicitud pendiente no permite leer juegos.
- Revisiones y operaciones idempotentes para detectar conflictos. Retirar una publicación borra su contenido y conserva la revisión.
- Bucket privado histórico `checkpoint-assets`: sus archivos existentes se conservan; la migración 0.8.10 bloquea nuevas subidas.
- Límites de solicitudes, incluyendo intentos cancelados, y eliminación de la relación al bloquear.


## Pasos de despliegue

1. Crear el proyecto gratuito en Europa. Mantener Data API activada, exposición automática de tablas desactivada y RLS automático activado.
2. Ejecutar la migración completa una vez en el SQL Editor, sobre este proyecto dedicado. Es transaccional; no reemplaza tablas existentes.
3. Ejecutar `supabase/tests/social_rls.sql` y `supabase/tests/storage_rls.sql` con el rol postgres. Usan usuarios ficticios y finalizan con `ROLLBACK`. Si aparece un error, ejecutar `ROLLBACK` antes de otra consulta. Revisar el resultado y cualquier aviso del editor.
4. Anotar la URL del proyecto y su clave **publishable** en la configuración pública de la app. La contraseña de Postgres y las claves secretas/service_role nunca se distribuyen con el ejecutable ni con GitHub.
5. Habilitar el registro y desactivar Confirm email para el modo actual de usuario y contraseña. Ya está aplicado; no configurar recuperación por correo para las direcciones internas.
6. Distribuir el cliente nativo 0.4.0 con la configuración pública. Incluye sesiones DPAPI y la pestaña Amigos.
7. La selección explícita, cola local, operaciones idempotentes, conflictos y retiradas están implementadas. Las notas y los títulos de tareas no se envían. Las retiradas pendientes necesitan conexión para hacerse efectivas.
8. Antes de una release pública, validar dos cuentas reales, rechazo de nuevas subidas, pérdida de conexión y revocación del acceso. Las pruebas nativas actuales simulan las respuestas HTTP.

## Alcance pendiente

La distribución 0.4.0 utiliza SQLite para la colección privada y Supabase para las cuentas y el contenido social seleccionado. No sincroniza automáticamente toda la biblioteca privada. Quedan pendientes avatares, recuperación de cuenta y una prueba completa con dos cuentas reales.

El código de 0.6 incluye la función Steam de Supabase. Su clave debe permanecer como secreto del servidor y debe activarse la autenticación propia de la función antes de vincular.

## Comprobaciones preparadas

Las pruebas SQL cubren creación de perfil, permisos anónimos, escrituras directas, visibilidad de perfiles, publicaciones, reintentos, conflictos, rechazo de notas y progreso inválido, consentimiento del destinatario, aislamiento del tercero, bloqueo y retirada. Las pruebas actuales de Storage comprueban el bloqueo de nuevas subidas y la publicación de texto con distintos roles. No sustituyen una prueba de concurrencia con varias conexiones.

`supabase/project.json` contiene solo la URL, la referencia, la región y la clave publishable que se puede distribuir con el cliente. No contiene la contraseña de Postgres ni claves privilegiadas. Al aplicar SQL manualmente, el panel de historial de migraciones de Supabase no registra automáticamente esta versión; conservar el archivo de migración y su registro local de despliegue.

La verificación HTTP se puede repetir con `node scripts/Verify-Supabase.mjs` (Node 22 o posterior, acceso a Internet). La auditoría SQL de despliegue está en `supabase/tests/deployment_audit.sql`.

El código de 0.6 añade una migración privada y una función Edge para Steam. Consulta [despliegue Steam](STEAM-SERVICE.md) para secretos, autenticación y validación.

## Almacenamiento solo de texto (0.8.10)

Aplicar `supabase/migrations/202610040001_checkpoint_text_only.sql` después de las migraciones sociales y de Steam. Bloquea INSERT/UPDATE en Storage mediante RLS restrictivo y un disparador de base de datos, incluidas las escrituras de service_role. Los objetos existentes conservan sus permisos de lectura. Las nuevas publicaciones ignoran las rutas antiguas de carátulas; se rechazan nuevas referencias de avatar e imágenes base64 incrustadas. Windows deja de subir carátulas locales. Las direcciones de proveedores externos y las imágenes locales no consumen Storage de Supabase.

Auditoría del 4 de octubre de 2026: base de datos de 12.007.091 bytes (unos 11,5 MiB, incluida la infraestructura); tablas de Checkpoint de 614.400 bytes (600 KiB); una imagen existente de 101.125 bytes (unos 99 KiB), conservada por petición del propietario. Repetir con `supabase/tests/storage_usage.sql` y comprobar el bloqueo con `storage_rls.sql`, que revierte sus datos ficticios. Esta política limita archivos, no el crecimiento del texto legítimo.
