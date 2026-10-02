[English](en/VALIDATION.md) · **Español**

# Validación

## 0.6.8 — acciones de ventana en Miniatura

Han pasado 270 comprobaciones: 108 de biblioteca, 15 Node, 14 Edge y 133 WPF. Las siete nuevas comprueban valores marcados, aplicación/guardado de siempre visible desde fondo y juego, bloqueo de redimensionado, cursor de arrastre, desbloqueo y traducción inmediata a inglés. Los menús nativos se capturan en ambos idiomas. No se afirma haber probado gestos físicos de arrastre, instalación/actualización ni cuentas reales.

## 0.6.7 — selección y ventana estables

Han pasado 263 comprobaciones: 108 de biblioteca, 15 Node, 14 Edge y 126 WPF. Las pruebas conservan selección/foco al actualizar y reordenar, comprueban que una actualización sin foco no lo adquiere, limpian selecciones retiradas y recuperan filas virtualizadas entre 1.003 juegos sin crear 30 filas. Se prueba salir de Miniatura desde la esquina de la pantalla, entrar desde una posición parcialmente exterior y mostrar una ventana fuera del área de trabajo. La restauración de tamaño se limita al espacio disponible, también en escritorios pequeños. Estas pruebas usan el monitor actual: varios monitores y cambios de DPI físicos siguen pendientes, junto con instalación y cuentas reales.

## 0.6.6 — teclado en Miniatura

Han pasado 255 comprobaciones: 108 de biblioteca, 15 Node, 14 Edge y 118 WPF. Los eventos de teclado prueban Fin/↑/Inicio, límites de fila, menú con Enter/Espacio, recuperación del foco al cerrar/cambiar estado y salto a una fila virtualizada entre 1.003 juegos con menos de 30 filas creadas. El foco espera al diseño diferido y la prueba espera a su recuperación. Captura nativa incluida. No sustituye pruebas físicas de teclado/lector de pantalla; siguen pendientes instalación y cuentas reales.

## 0.6.5 — estados en Miniatura

Han pasado 247 comprobaciones locales: 108 de biblioteca, 15 Node, 14 Edge y 110 WPF. Las pruebas abren los menús reales de las filas, comprueban cinco estados y selección actual, guardan historia terminada sin salir de Miniatura, reabren la historia eliminando la fecha y verifican etiquetas inglesas. Capturas nativas del menú en ambos idiomas. Siguen pendientes las pruebas físicas de teclado/arrastre, paquetes instalados y cuentas reales.

## 0.6.4 — vista Miniatura

Han pasado 243 comprobaciones locales: 108 de biblioteca, 15 Node, 14 Edge y 106 WPF. Miniatura se prueba desde Ajustes, con persistencia, tamaños independientes, redimensionado, filas de nombre/estado, interfaz exterior oculta, ausencia de imágenes/botones/progreso, contraste, cambio inmediato a inglés, salida con menú y ciclo de vistas. Capturas nativas en ambos idiomas. Generados ZIP/MSI y MSIX de prueba validado con SDK. Siguen pendientes pruebas físicas de teclado/arrastre, actualización/desinstalación instalada y cuentas reales.

## 0.6.3 — modo ligero

Han pasado 233 comprobaciones: 108 de biblioteca, 15 Node, 14 Edge y 96 WPF. La casilla real de Ajustes se prueba al guardar, persistir, cancelar y desactivar; las imágenes de colección permanecen ocultas con caché vacía. Las peticiones simuladas de Storage privado demuestran que el progreso de amigos sigue visible sin descargar carátulas y que estas vuelven al desactivar el modo. Traducción inglesa validada. Generados ZIP/MSI y MSIX sin firma validado con SDK. Se conservan las limitaciones de instalación y cuentas reales indicadas abajo.

## 0.6.2 — menor consumo

Han pasado 222 comprobaciones: 108 de biblioteca, 15 Node, 14 Edge y 85 WPF. Se prueban liberación/recarga de carátulas, caché con 40 imágenes y cuadrícula de 1.003 juegos con comprobación de referencias fuera de pantalla. Generados ZIP/MSI sin firma y MSIX de prueba; se verificaron los 283 archivos de este último. Frente a 0.6.1, la app extraída baja de 173,72 a 158,00 MiB y el ZIP de 73,85 a 68,44 MiB. Los paquetes contienen los avisos de primer uso en inglés/español. No se afirma haber medido mínimos RAM/CPU en hardware antiguo ni haber validado instalación MSI/MSIX o cuentas reales.

## Preparación MSIX anterior a 0.6.2

MakeAppx validó esquema y contenido del MSIX x64 de prueba sin firma. La extracción verificó por SHA-256 los 485 archivos de la app, tamaños de iconos, idiomas, manifiesto y arquitectura PE. Se rechazaron cinco casos de identidad/versión inválidos. La app extraída del MSIX pasó 79 comprobaciones WPF con datos aislados y el perfil real de Windows para DPAPI. Esto no valida MSIX instalado, actualizaciones, desinstalación, arranque ni certificación. No se instaló ni se envió el paquete a Store.

## Empaquetado de 0.6.1

El portable contiene una sola carpeta `Checkpoint` en la raíz. La verificación exige esa estructura y ejecuta la app desde ella. EXE/MSI siguen sin firma: el bloqueo comunicado de SmartScreen necesita una vía de firma/distribución de confianza. No se encontró un certificado adecuado en el almacén del usuario Windows. Las pruebas funcionales no validan reputación de SmartScreen.

## Integración Steam de 0.6.0

Han pasado 108 comprobaciones de biblioteca, 15 del servidor Node, 14 HTTP/seguridad de la función Supabase y 79 nativas WPF (216 en total). Las pruebas Steam nativas verifican dirección Supabase, autorización vinculada al servicio, restauración DPAPI, rechazo de sesiones incorrectas, idioma y desvinculación. ZIP y MSI generados; el MSI sigue sin firma y sin prueba de instalación/desinstalación.

La función `checkpoint-steam` y su migración privada están desplegadas. Han pasado 20 comprobaciones PostgreSQL, con datos de prueba revertidos: permisos, consumo atómico, respuestas repetidas, caducidad, límites y revocación. El ajuste JWT del gateway se ha desactivado solo para esta función tras aprobación. El servicio real detecta la clave configurada: health 200, biblioteca/logros sin sesión 401, callback inválido 400, inicio/sondeo pendiente 200 y secreto de sondeo incorrecto 400. La clave permanece en los secretos de Supabase. La vinculación con una cuenta real, validez de la clave ante Steam e importación de juegos/logros requieren completar personalmente el acceso.

## 0.5.0

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
