[English](en/VALIDATION.md) · **Español**

# Validación de 0.5.0

Comprobaciones realizadas durante la preparación local del 2 de octubre de 2026:

- Compilación Release de la solución, sin errores ni advertencias.
- 108 comprobaciones de modelos, independencia entre historia y logros, deduplicación de Steam, SQLite, transacciones, preferencias, reordenación, copias completas, archivos malformados, límites al descomprimir y dimensiones/proporciones PNG, recuperación, migración, privacidad de publicaciones, cola por cuenta, conflictos, usuario sin correo real, autenticación y rotación de sesión.
- 15 pruebas Node.js de autenticación OpenID, nonce, callback, secretos de sondeo, revocación, caché, privacidad, autorización y límites de consultas.
- 64 comprobaciones WPF de búsqueda, estados, cuadrícula de 2/3/4 columnas, reordenación persistente, autorización del arrastre, alta de juegos con Unicode, tareas, cancelación y guardado, logros secretos, preferencias, eliminación, botón de deshacer, diálogo de recuperación y copias con imágenes. También se comprueban registro, sesión DPAPI, solicitudes, publicación seleccionada, carátula privada autenticada, bloqueo, retirada y separación entre cuentas. Se abren los controles y diálogos reales de la app con una biblioteca aislada y respuestas sociales simuladas.
- Importación de una carátula sin depender de Internet, repetición sin duplicados ni sobrescrituras, rechazo de una imagen inválida y fallo provocado de SQLite que conserva la colección y retira imágenes preparadas.
- Virtualización con 1.003 juegos: menos de 30 filas creadas tanto al abrir como después de desplazarse al final de la colección.
- Renderizado de lista, compacta, cuadrícula y diálogos en temas claro/oscuro. La prueba nativa finaliza con código 0 y un informe JSON.
- Prueba del ejecutable autocontenido extraído del ZIP de distribución; sin depender del SDK o la carpeta de compilación.
- Generación de ZIP autocontenido y MSI por usuario con WiX 5.0.2. Los artefactos incluyen sumas SHA-256.

Las respuestas de Steam en pruebas de servicio son simuladas. No se ha usado una cuenta o clave real. El servicio público no está desplegado y su dirección en esta edición está vacía.

En Supabase real pasaron 23 comprobaciones sociales SQL, 10 de Storage/límites y 7 de permisos HTTP anónimos. Los usuarios SQL ficticios se revirtieron; no quedaron datos de prueba. La API pública de Auth confirmó registro habilitado y confirmación de correo desactivada. La prueba nativa de amigos utiliza un transporte HTTP simulado: aún no se ha probado el registro con dos cuentas reales ni la subida/descarga real de Storage.

La inspección visual utiliza imágenes generadas por `RenderTargetBitmap` desde la propia app. Los diálogos se comprueban mediante eventos y controles WPF; estas pruebas no sustituyen una prueba manual del gesto de arrastre, del teclado físico, de los selectores de archivos o de todos los diálogos del sistema.

El MSI se ha compilado y validado por el compilador; no se ha instalado ni desinstalado en este PC. Antes de una release pública, verificar en una máquina limpia:

1. Instalación por usuario, acceso del menú Inicio y arranque sin un runtime externo.
2. Añadir/editar juegos, tareas, carátulas propias y copias con nombres internacionales.
3. Mover/redimensionar el widget, opacidad sobre distintos fondos, varios monitores y cambios de DPI.
4. Vincular una cuenta real con datos visibles, importar, consultar logros y probar fallos de conexión.
5. Ocultar/mostrar desde la bandeja, atajo global y retorno tras suspensión.
6. Inicio con Windows, actualización y desinstalación que conserva la biblioteca y retira el acceso de inicio.
7. Firma de los artefactos y configuración real de identidad y país del servicio.
8. Crear dos usuarios de Checkpoint, intercambiar solicitudes y carátulas privadas, publicar/retirar juegos, bloquear, cerrar sesión y comprobar reintentos sin conexión contra Supabase real.

La translucidez es alfa sin desenfoque. La base de datos de versiones 0.1.0 y 0.2.0 migra al esquema 2, que añade el historial de recuperación. Las versiones anteriores rechazan una base de datos ya migrada. Las copias JSON de esas versiones siguen siendo importables y el formato JSON exportado sigue siendo compatible; las copias completas requieren 0.3.0 o posterior.

La validación 0.5.0 comprueba el cambio de idioma desde Ajustes, persistencia, nombres accesibles del editor inglés, progreso compartido y vuelta al español, sin modificar notas ni publicaciones.
