# Checkpoint en el navegador

[English](en/WEB.md) · **Español**

[**Abrir la app web**](https://nicolasmf05.github.io/Checkpoint/). No requiere instalar Checkpoint ni .NET. Usa un navegador moderno con JavaScript y almacenamiento IndexedDB habilitados.

La web permite crear y editar juegos, marcar estado e historia, notas privadas, tareas, favoritos, carátulas locales, lista/compacta/cuadrícula, ventana completa/pequeña/Miniatura, ocho temas, español o inglés y [atajos configurables](SHORTCUTS.md). En ventana completa el fondo tiene opacidad máxima. La transparencia afecta al fondo de la página; el navegador no es un widget sobre otras aplicaciones.

Steam se vincula en Ajustes y abre el acceso oficial de Steam. La biblioteca y los logros se consultan al abrir, al recuperar conexión y cada 15/30/60/120 minutos mientras la pestaña está visible. Steam exige que los datos pertinentes sean públicos. Los juegos familiares que Steam devuelve en sus licencias o actividad reciente se incorporan; no se garantiza encontrar todo el catálogo familiar. La historia, estado y notas no se completan ni sustituyen con Steam.

Amigos utiliza tu cuenta de Checkpoint, con usuario y contraseña, código completo checkpoint-…, solicitudes y progreso publicado desde Checkpoint. Los juegos de Mi lista se comparten por defecto con amigos aceptados; los privados y las importaciones no añadidas a Mi lista quedan excluidos. [Listas y privacidad](LISTS.md). Se publican título, plataforma, estado y cifras de progreso; las notas, títulos de tareas y carátulas propias quedan privadas. Cerrar sesión no retira publicaciones: usa Dejar de compartir. Los conflictos entre dispositivos necesitan revisión antes de volver a publicar.

## Datos y copias

La biblioteca privada, ajustes y publicaciones pendientes se guardan en este navegador y origen. No hay sincronización privada automática entre Windows y la web ni entre navegadores. Borrar los datos del sitio elimina la biblioteca local. **Exporta copias JSON regularmente.** Importar admite JSON de Checkpoint y evita duplicados; los paquetes ZIP .checkpoint de escritorio no se importan aquí. Las copias JSON pueden contener notas y tareas privadas. Las carátulas integradas de la web se incluyen en su JSON; las carátulas empaquetadas de Windows requieren volver a elegirse.

Las sesiones de Steam y Checkpoint se guardan en sessionStorage de la pestaña; no forman parte de los snapshots o copias. Cerrar la pestaña puede requerir volver a entrar. La biblioteca queda disponible sin cuenta. Tras la primera visita completada, los archivos públicos de la app se almacenan para abrir sin conexión; Steam y amigos requieren red. No hay telemetría propia. GitHub sirve la página y Supabase procesa las consultas autenticadas. [Privacidad](PRIVACY.md).

La web no dispone de bandeja, arranque con Windows, atajos globales, transparencia del navegador sobre el escritorio ni lanzamiento del ejecutable de Steam. Descarga la versión de Windows para esas funciones.

## Desarrollo y despliegue

```powershell
node scripts/build-web.mjs
node --test tests/web/*.test.mjs web/test/*.test.mjs supabase/tests/steam.test.mjs
npm --prefix web ci --ignore-scripts
cd web
npx playwright install chromium
cd ..
node web/test/browser.mjs
```

GitHub Actions compila únicamente los archivos estáticos permitidos en dist/web y publica con Pages. La función Steam permite CORS para https://nicolasmf05.github.io; sus comprobaciones de sesión y privacidad siguen activas. La clave publicable de Supabase es pública por diseño; la clave privada de Steam permanece en los secretos del servidor. No subas claves privadas ni tokens.

Las pruebas del navegador utilizan servicios simulados y cuentas ficticias. Se comprueba además el CORS real y que la biblioteca sin sesión sigue devolviendo 401. La disponibilidad de un juego familiar concreto depende de lo que Steam devuelva para la cuenta vinculada.
