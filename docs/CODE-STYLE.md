# Formato y comentarios del código

El código mantenido por Checkpoint usa UTF-8, saltos de línea LF y espacios.
C# sigue CSharpier con líneas de hasta 100 caracteres; JavaScript, TypeScript,
CSS, HTML, JSON, YAML, TOML y XML siguen Prettier. Python sigue Black y los scripts
PowerShell siguen las reglas de `scripts/PSScriptAnalyzerSettings.psd1`.
Las declaraciones SQL se formatean como PostgreSQL; los cuerpos de funciones
entre dólares conservan su texto para respetar PL/pgSQL y las migraciones.

Desde la carpeta del proyecto, prepara las herramientas una vez:

```powershell
./scripts/Format-Code.ps1 -Setup
```

Para aplicar el formato o comprobarlo sin modificar código:

```powershell
npm run format
npm run format:check
```

Se requieren Node.js, Python, PowerShell 7 y el SDK de .NET del proyecto. Las
versiones de las herramientas están fijadas en el lockfile y en el script;
las herramientas auxiliares se guardan en `.tools`. El inventario procede de
Git y excluye dependencias, distribuciones, evidencias y archivos generados.

Los comentarios de cada módulo explican su responsabilidad. Los comentarios
internos describen contratos, prioridades y decisiones que no resultan evidentes
al leer las instrucciones, como el control de revisiones, los reintentos de
publicaciones, los límites de descarga o la conservación del foco de formularios.
Al cambiar una regla, actualiza también el comentario que la describe.

JSON no admite comentarios. Sus archivos mantienen JSON estándar: `global.json`
elige el SDK; `package.json` fija herramientas de desarrollo; `service-config.json`
y `supabase/project.json` contienen configuración pública; los catálogos `en.json`
y `LocalizationKeys.json` relacionan textos y recursos de traducción.

Los estilos comunes de la interfaz están en `src/Checkpoint.App/Web/app.css`.
`web/web.css` adapta la aplicación al navegador y `web/presentation.css` define
la página pública. Sus bloques comentados separan paletas, controles, navegación,
tarjetas, formularios y ajustes por tamaño de pantalla. Las medidas `px` forman
parte de esas reglas CSS.
