# Preparación para Microsoft Store

[English](en/MICROSOFT-STORE.md) · **Español**

Checkpoint ya tiene un proceso reproducible para generar MSIX. Todavía no está publicado ni certificado por Microsoft Store. Los EXE/MSI de GitHub siguen sin firma.

## Qué está preparado

- Manifiesto para Windows 11 y la app WPF, con español/inglés y `runFullTrust`.
- Iconos de tienda, menú Inicio y mosaico derivados del icono de Checkpoint. Se regeneran con `scripts/New-StoreAssets.ps1`.
- Runtime de .NET, configuración pública de Steam/Supabase y licencias incluidos.
- Validación de esquema/contenido con MakeAppx, SHA-256 y comprobación del paquete extraído: todos los archivos, dimensiones de iconos y arquitectura nativa.
- GitHub Actions genera un paquete **preview-unsigned**, destinado a validar el empaquetado. No es un instalador para usuarios.

## Generar una prueba local

Instala Windows SDK con MakeAppx. Genera primero el portable:

```powershell
./scripts/Build.ps1
./scripts/Build-MSIX.ps1 -Preview
```

La prueba usa `Checkpoint.PackagingPreview`, una identidad ajena a la tienda. El script no firma ni instala el paquete ni cambia la seguridad de Windows. Compilar correctamente no acredita reputación de SmartScreen ni certificación de Microsoft.

## Preparar la publicación real

1. Registrar una cuenta individual de desarrollador en Partner Center y verificar tu identidad. Actualmente Microsoft ofrece el registro individual gratuito.
2. Reservar un nombre disponible. En Product management → Product identity, copiar exactamente **Package/Identity/Name**, **Package/Identity/Publisher** y **Package/Properties/PublisherDisplayName**. Son identificadores públicos, no contraseñas.
3. Generar el paquete con esos valores y el nombre reservado:

```powershell
./scripts/Build-MSIX.ps1 -IdentityName '<Package/Identity/Name>' `
    -Publisher '<Package/Identity/Publisher>' `
    -PublisherDisplayName '<Package/Properties/PublisherDisplayName>' `
    -DisplayName '<nombre reservado>'
```

4. Probar el paquete con la identidad final en otra cuenta o máquina Windows de pruebas. Ejecutar Windows App Certification Kit. Validar instalación, actualización, desinstalación, bandeja/atajo global, acceso a Steam, cuentas/amigos, copias, arranque y comportamiento de los datos. Las comprobaciones del archivo no sustituyen estas pruebas.
5. Completar fichas en español e inglés, capturas adecuadas, enlaces de soporte/privacidad, clasificación por edades, disponibilidad y notas para revisión. Las capturas existentes contienen usuarios/juegos de ejemplo identificados. Las descripciones y capturas deben corresponder a la versión enviada.
6. Enviar el MSIX mediante Partner Center. Microsoft firma los MSIX al publicarlos tras la certificación. Publicar el MSI/EXE actual en la tienda exigiría firma propia; esa ruta no es la preparada aquí.

El paquete final se llama **store-unsigned**. No debe ofrecerse como instalador de GitHub. Fuera de la tienda, MSIX sigue necesitando firma de confianza.

## Versiones

La app conserva su versión actual. El MSIX usa por defecto **(versión mayor de la app + 1).menor.revisión.0**: app `0.6.1` → paquete `1.6.1.0`. Así cumple la versión mayor distinta de cero y el último campo reservado sin llamar 1.6.1 a la app. Puede especificarse `-PackageVersion 1.6.2.0`; las actualizaciones deben aumentar la versión y conservar la identidad. Cada campo debe ser <= 65535. `-Runtime win-arm64` exige generar antes el portable correspondiente y validar hardware ARM64 antes de ofrecerlo.

## Datos y arranque: pendientes de pruebas instaladas

La app utiliza `%LOCALAPPDATA%\Checkpoint` y un acceso directo opcional de Inicio. MSIX puede virtualizar las rutas de datos y cambiar el directorio instalado al actualizar. Falta comprobar si la biblioteca portable/MSI aparece en la edición empaquetada y si el acceso directo de arranque sobrevive a las actualizaciones. Si las pruebas lo requieren, habrá que adaptar el arranque al mecanismo de Windows para paquetes.

Antes de cambiar de portable/MSI a Store, exporta una copia `.checkpoint` y luego impórtala en la edición Store. Las sesiones y el consentimiento de compartición no se exportan: vuelve a iniciar sesión y elige los juegos compartidos. La migración, actualización y desinstalación deben probarse antes de prometer conservación de datos; desinstalar MSIX puede eliminar datos del paquete.

`runFullTrust` permite ejecutar el widget WPF con SQLite, bandeja, atajos y copias elegidas por el usuario. Esta explicación debe incluirse para los revisores. No solicita permisos de administrador.

## Requisitos externos pendientes

Registro/verificación, identidad reservada, pruebas del paquete instalado y sus actualizaciones/datos/arranque, Windows App Certification Kit, URL pública de privacidad que describa el servicio actual, pruebas con Steam y dos cuentas Checkpoint reales, y revisión de Microsoft. Estos scripts no crean una cuenta Partner Center ni envían una publicación.

Fuentes: [Empaquetado MSIX](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-manual-conversion), [requisitos de firma y versión](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/app-package-requirements), [identidad del producto](https://learn.microsoft.com/en-us/windows/apps/publish/view-app-identity-details), [registro individual gratuito](https://learn.microsoft.com/en-us/windows/apps/publish/whats-new-individual-developer).
