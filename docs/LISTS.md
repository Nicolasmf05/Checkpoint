# Listas y privacidad

[English](en/LISTS.md) · **Español**

Desde 0.7.6, al iniciar sesión los juegos de **Mi lista** son visibles automáticamente para tus amigos aceptados de Checkpoint. No son públicos en Internet. Los juegos de Steam importados en **Biblioteca** no se comparten hasta añadirlos a Mi lista. Las notas, nombres de tareas y detalles individuales de logros permanecen privados; el objetivo personalizado sí se comparte.

- **Privado desde el principio:** marca **Privado para mis amigos** en el editor antes de Guardar. Al añadir desde Privados, ya viene marcado.
- **Cambiar después:** menú del juego → **Mover a Privados** o **Hacer visible para amigos**; también puedes cambiar la casilla en el editor o la selección en Amigos → Compartir.
- **Ver privados:** selecciona **Privados** junto al buscador. No aparecen en Mi lista ni en las vistas de listas personalizadas; permanecen en Biblioteca.
- **Privados por defecto:** Gestionar listas → Crear juegos nuevos como privados → Guardar. No cambia los juegos existentes.
- **Varias listas:** Gestionar listas → escribe un nombre → Crear lista. En el editor marca las listas del juego. Puede pertenecer a varias sin duplicarse. Hasta 30 listas de 1–40 caracteres, con nombres únicos sin distinguir mayúsculas.
- **Renombrar o quitar:** elige la lista en Gestionar listas. Quitar conserva juegos, privacidad y otras pertenencias. Las listas vacías también se guardan. Renombrar o quitar actualiza también los juegos del historial de recuperación.

Los nombres y pertenencias son organización local. Los amigos ven todos los juegos visibles, sin agrupación de listas. En la versión de Windows, Miniatura conserva la lista seleccionada y solo se muestran nombres y estados; usa su menú para cambiar de lista.

Las retiradas explícitas de versiones anteriores se conservan como privadas. Los demás juegos de Mi lista pasan al nuevo valor visible por defecto. Los cambios de privacidad se guardan localmente incluso sin red, pero una publicación anterior puede seguir visible hasta que el servidor confirme su retirada. Cerrar sesión no retira publicaciones.

Las copias JSON y .checkpoint de Windows conservan privacidad, pertenencias y listas vacías; la web admite JSON. Sesiones y cola social quedan excluidas. Importar omite juegos ya existentes para conservar sus datos actuales. El valor inicial para nuevos juegos es un ajuste local y no se exporta.

## Crear y gestionar juegos

Escribe el nombre en Gestionar listas y pulsa Guardar: se crea y abre la lista. Crear lista sigue disponible para crear varias sin cerrar el gestor. Si seleccionas una lista existente y cambias su nombre, Guardar aplica el cambio.

El menú con clic derecho sobre un juego contiene acciones de ese juego. Ofrece las tres listas creadas más recientemente, Cambiar de lista para cualquier destino y Añadir a otra lista para conservar las pertenencias anteriores. Las opciones de ventana y de la app se consultan desde sus controles o desde el menú del fondo en Miniatura.

En Biblioteca y las listas normales, marca las casillas de los juegos. La barra de selección permite actuar sobre hasta 500 juegos. Seleccionar resultados incluye hasta 500 resultados del filtro actual; cambiar de vista, lista o filtro limpia la selección.

Mover desde una lista personalizada quita solo la pertenencia de origen y añade el destino, conservando otras listas. Mover desde Biblioteca o Mi lista reemplaza las listas actuales. Añadir conserva las demás. Desde Privados se conservan las otras pertenencias y la privacidad. Estos movimientos conservan la privacidad, las notas, tareas y logros. Quitar de una lista personalizada conserva el juego en Biblioteca; Quitar de Mi lista deja de seguirlo y retira su publicación cuando haya conexión. Hacer visible para amigos también añade a Mi lista un juego que antes no se seguía.

Ver ficha de la lista abre el resumen, búsqueda y juegos con selección individual, en páginas de 50. Incluye sus miembros privados para que puedas gestionarlos, aunque no aparezcan en la vista pública de esa lista. Puedes abrir la ficha de cada juego, mover los seleccionados, quitar pertenencias o cambiar privacidad. Marcar privado afecta al juego completo en todas sus listas; no cambia sus pertenencias.
