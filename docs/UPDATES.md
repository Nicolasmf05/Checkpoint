# Actualizaciones de Windows

A partir de **0.8.1**, entra en **Ajustes → Actualizaciones**. La aplicación comprueba las releases públicas de `Nicolasmf05/Checkpoint` al abrir, como máximo una vez cada 24 horas. Puedes desactivarlo o pulsar **Buscar actualizaciones** cuando quieras. Desde 0.8.6 se publica únicamente la release completa y estable más reciente; las anteriores se conservan como borradores. No requiere una cuenta ni token de GitHub y no envía tu biblioteca, sesiones ni datos de Steam.

Si hay una versión superior compatible, aparece **Descargar e instalar**. La descarga comprueba el tamaño y SHA-256 del MSI contra su archivo `.sha256` y el digest de GitHub. Solo se aceptan el repositorio oficial y las redirecciones HTTPS de sus servidores de archivos. Una descarga incompleta o alterada no se instala. Se comprueba de nuevo el hash antes de ejecutar el instalador.

Antes de instalar, Checkpoint guarda una copia `.checkpoint` en la subcarpeta `updates` de tu carpeta de datos. Luego cierra la app, espera a que termine, ejecuta el MSI por usuario y vuelve a abrirla con la misma carpeta de datos. El instalador conserva la biblioteca y los ajustes. Si falla o lo cancelas, se intenta abrir la versión anterior; también puedes iniciarla manualmente. La instalación completa exige pulsar el botón: no se descarga ni instala silenciosamente al abrir.

**Portable:** este actualizador instala la nueva versión en `%LOCALAPPDATA%\Programs\Checkpoint`, con acceso en el menú Inicio. **No sustituye el ZIP ni el ejecutable portable original.** Después utiliza el acceso del menú Inicio; el antiguo portable seguirá siendo una versión anterior. La carpeta de datos original se pasa al reinicio, incluyendo la elegida con `--data-dir`.

Los registros del instalador Windows y el resultado local determinan si la actualización terminó. El código 3010 indica que Windows puede necesitar un reinicio; la app no reinicia el PC automáticamente. Los instaladores siguen sin firma: SHA-256 comprueba integridad, no sustituye un certificado ni elimina SmartScreen. No se cambia la configuración de seguridad de Windows.

Las descargas/copia pueden ocupar espacio. Puedes borrar manualmente los MSI y copias antiguos dentro de `updates` cuando estés seguro de que la nueva versión funciona. No borres tu `checkpoint.db`. Las sesiones cifradas permanecen locales y no forman parte de la copia. El resultado se notifica al abrir de nuevo; si el helper no pudo arrancar o reiniciar, abre Checkpoint desde el menú Inicio.

La API de GitHub tiene límites y necesita Internet; un fallo no impide usar la versión instalada. El actualizador requiere Windows PowerShell incluido en Windows; si una política del equipo bloquea ese proceso o MSI, instala manualmente desde [Releases](https://github.com/Nicolasmf05/Checkpoint/releases). No cambia las políticas de PowerShell ni desactiva SmartScreen. En la web, recarga para obtener el despliegue de GitHub Pages; no se utiliza el MSI.

Las pruebas cubren selección de versiones/arquitectura, orígenes, hashes, descargas simuladas y el helper que rechaza un hash incorrecto antes de instalar. No se ha ejecutado una actualización MSI real sobre la instalación personal del usuario durante las pruebas.

La descarga conserva la marca de procedencia de Internet de Windows. La carpeta de descarga necesita admitir este metadato (NTFS habitual en Windows); si no lo admite, instala desde el navegador. Los accesos del menú Inicio y, si está activado, inicio con Windows conservan también una carpeta de datos personalizada.
