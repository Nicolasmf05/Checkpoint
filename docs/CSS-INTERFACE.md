# Interfaz CSS

Checkpoint 0.7.0 muestra colección, Miniatura, amigos, formularios y avisos con HTML/CSS/JavaScript locales en Microsoft Edge WebView2. Sin alojar una web, React, compilación npm ni interfaz remota.

## Dónde modificarla

- `src/Checkpoint.App/Web/app.css`: colores, translucidez, distribución, tipografía, reglas adaptables y formularios.
- `Web/index.html`: documento local y política de seguridad de contenido.
- `Web/app.js`: elementos HTML, filas virtualizadas, menús, teclado, foco y órdenes explícitas al código nativo.
- `Web/ui-model.mjs`: cálculos de virtualización y navegación; pruebas Node en `tests/web`.
- `WebSurface.cs`: control de composición transparente, documento local, validación de mensajes y esquema de formularios.
- `WebInterface.cs`: estado de colección y órdenes que utilizan los controladores C# existentes.

Modifica los archivos y ejecuta `./scripts/Build.ps1 -Installer`. CSS/JS se copia al directorio `Web` del paquete. `Verify-WebInterface.ps1` prueba la aplicación WebView2 real del paquete con datos nuevos. Se necesita WebView2 Evergreen Runtime; `Ensure-WebViewRuntime.ps1` comprueba su disponibilidad. Solo GitHub Actions puede solicitar explícitamente su instalación automática para pruebas.

## Responsabilidades nativas

C#/.NET conserva SQLite, validación, Steam, Supabase, sesiones cifradas, decodificación de imágenes, copias, bandeja, atajos y ventana. Los controladores WPF de formularios permanecen ocultos y generan un esquema JSON de presentación; se reutilizan sus validaciones y botones. Por eso aún existe XAML, aunque no define el aspecto de la interfaz de producción. Los selectores de archivos siguen el aspecto de Windows. Si WebView2 no puede iniciarse, un aviso nativo permite abrir la descarga oficial del runtime.

El navegador envía órdenes explícitas, sin objetos nativos arbitrarios. Se bloquean navegación remota, marcos, descargas y permisos. La política del documento impide llamadas de red desde JavaScript. Las carátulas utilizan un origen interno y los identificadores de juegos existentes; no se pueden solicitar rutas arbitrarias. Títulos y notas usan propiedades de texto/valor del DOM. Las contraseñas no vuelven a JavaScript en las instantáneas: permanecen en el campo mientras se escriben y se envían al controlador nativo de acceso. Claves de Steam y sesiones guardadas permanecen en C#.

## Actualización y consumo

La biblioteca SQLite y las preferencias mantienen ubicación y formato. No hace falta migrar cuentas ni desplegar cambios de servidor. El perfil del navegador se guarda en `webview-profile` junto a la biblioteca y queda fuera de copias y publicaciones del código.

WebView2 añade una dependencia compartida. .NET sigue incluido, pero WebView2 no se incluye en el ZIP/MSI de GitHub. La biblioteca manual funciona sin Internet una vez instalado. Consulta la [documentación de distribución de Microsoft](https://learn.microsoft.com/microsoft-edge/webview2/concepts/distribution).

Las filas están virtualizadas y no se reenvía continuamente una colección que no cambia. Modo ligero y Miniatura evitan carátulas. Los procesos del navegador añaden memoria; no se promete menor RAM que WPF. Siguen pendientes mediciones en equipos antiguos, teclado/ratón físicos, cambios de DPI, lectores de pantalla y actualización de paquetes instalados.
