# Política de releases completas

**Español** · [English](en/RELEASES.md)

Se publican releases completas y estables (`prerelease = false`), dejando visible solo la más reciente. La preparación se hace como borrador; la publicación requiere las pruebas aplicables de GitHub y los seis archivos verificados:

- ZIP portable para Windows x64 y su archivo SHA-256.
- Instalador MSI para Windows x64 y su archivo SHA-256.
- ZIP del código fuente y su archivo SHA-256.

Incrementa la versión de `Directory.Build.props` si cambian los paquetes. Guarda el código final en un commit, ejecuta `./scripts/Build.ps1 -Installer` y `./scripts/Export-Source.ps1` y prepara notas en UTF-8. Cuando Build and verify termine correctamente para ese commit (y Checkpoint Web también, si se ejecuta), utiliza:

```powershell
python scripts/Publish-Release.py --notes .qa/release-notes.md --name "Checkpoint VERSION — título"
```

La herramienta de mantenimiento requiere Python 3.11 o posterior y Git; quienes usan la app no los necesitan. Utiliza las credenciales existentes de Git o `GH_TOKEN`/`GITHUB_TOKEN`, sin mostrarlas, y rechaza sobrescribir una release pública. Comprueba las sumas locales antes de escribir en GitHub y verifica tamaño y SHA-256 remotos antes de publicar. La etiqueta debe corresponder al commit comprobado. Si falla la preparación, el borrador se conserva y la release actual sigue visible.

Después de verificar que la nueva release completa es pública y está marcada como más reciente, las anteriores pasan a borrador. Sus archivos quedan conservados para los responsables con permiso de escritura. Las etiquetas, el historial y el código del repositorio siguen públicos. Ocultar releases no revoca descargas anteriores ni licencias concedidas. El 2026-10-04 se convirtió v0.8.6 en release completa y se conservaron las 31 anteriores como borradores.

GitHub Actions compila los paquetes y los comprueba; no ejecuta automáticamente este comando de publicación. La release más reciente está en [Releases/latest](https://github.com/Nicolasmf05/Checkpoint/releases/latest).
