# Mensajes de seguridad de Windows

[English](en/WINDOWS-SECURITY.md) · **Español**

Los EXE/MSI preliminares de Checkpoint no tienen firma digital. Extraer el ZIP no cambia la firma ni la reputación del ejecutable.

## Identificar el mensaje

- **«Windows protegió su PC» / editor desconocido:** SmartScreen evalúa la reputación del archivo/editor. Una versión nueva sin firma puede generar el aviso. La distribución adecuada requiere firma con un certificado de confianza y continuidad entre versiones; firmar no garantiza reputación inmediata.
- **Control inteligente de aplicaciones:** Windows puede bloquear aplicaciones sin firma o sin confianza. Necesita una vía adecuada de distribución/firma; cambiar la estructura del ZIP no resuelve ese bloqueo.
- **Nombre de amenaza como `Trojan:…`:** abre Seguridad de Windows → Protección contra virus y amenazas → Historial de protección y anota el nombre y archivo afectado. Compara su SHA-256 con el publicado. Las pruebas funcionales no demuestran que sea un falso positivo.

No desactives la protección, añadas exclusiones ni restaures un ejecutable en cuarentena para abrir la app. Una detección requiere investigación; si la clasificación es incorrecta, el desarrollador puede enviar el archivo público exacto a Microsoft para revisión. Renombrar, reempaquetar o usar una firma autofirmada no soluciona un mensaje de seguridad.

El proyecto no tiene un certificado de firma de confianza pública configurado. Hace falta un certificado/servicio adecuado y verificación de identidad del editor. La revisión de seguridad y la obtención del certificado son trabajos separados del arreglo del paquete.

Fuentes: [Guía SmartScreen para desarrolladores](https://learn.microsoft.com/windows/apps/package-and-deploy/smartscreen-reputation), [Control inteligente de aplicaciones](https://learn.microsoft.com/windows/security/book/application-security-application-and-driver-control), [Revisión de archivos por Microsoft](https://learn.microsoft.com/unified-secops/submission-guide).

## Alternativa Microsoft Store

La distribución MSIX por Store permite que Microsoft firme el paquete tras la certificación, sin un certificado propio. El proyecto tiene una prueba de empaquetado sin firma validada; aún no está publicado en Store. Consulta [preparación y pasos pendientes](MICROSOFT-STORE.md).
