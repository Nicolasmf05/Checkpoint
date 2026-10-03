# Checkpoint: lee esto antes de utilizarla

## AVISO IMPORTANTE: WINDOWS SMARTSCREEN

**El EXE y el MSI de GitHub NO tienen firma digital ni certificado de editor. Por eso, y por no tener todavía reputación suficiente, Windows SmartScreen puede mostrar «Windows protegió su PC» o «Editor desconocido».**

**El desarrollador no dispone de certificado de firma de código porque obtenerlo para estas descargas supone un coste. No se adquirirá hasta que los ingresos del proyecto cubran al menos lo que cueste. Hasta entonces, las descargas de GitHub seguirán sin ese certificado.**

SmartScreen evalúa reputación: incluso una firma válida no garantiza que desaparezca el aviso inmediatamente. Esta explicación se refiere al aviso de aplicación/editor no reconocido; una detección de virus con nombre concreto es otro caso y debe investigarse. No desactives Defender ni añadas exclusiones para ejecutar la app.

La vía alternativa es Microsoft Store con MSIX, donde Microsoft firma tras aprobar la app sin comprar un certificado propio. Está preparada parcialmente, pero todavía no se ha publicado en la tienda.

## Requisitos para usarla

- Un PC Windows x64. Windows 11 es la plataforma probada. Windows 10 no está validado; Windows 7/8/8.1 no son compatibles. ARM64 requiere su paquete específico y validación en ese hardware.
- Una carpeta donde tu usuario pueda escribir y espacio para la app, biblioteca, carátulas y copias.
- **No necesitas instalar .NET, Node.js, Python, Visual Studio, Supabase ni un servidor. El runtime viene incluido. No pide permisos de administrador.**
- **No necesitas cuenta ni Internet para añadir y organizar juegos manualmente, utilizar carátulas locales o importar/exportar copias.** Internet solo se necesita para descargar carátulas y las funciones de Steam/amigos; Steam y Checkpoint se vinculan por separado y son opcionales. Hace falta un navegador para iniciar sesión en Steam.

Portable: extrae el ZIP y abre `Checkpoint.exe` dentro de `Checkpoint`. Conserva todos los archivos juntos. El MSI instala para tu usuario. Ambas ediciones incluyen este aviso y su traducción inglesa.

## Vista Miniatura

Selecciona **Ajustes → Vista de la colección → Miniatura**, o pulsa F6 hasta llegar a ella. Muestra únicamente **nombre y estado** de los juegos de Mi lista, sin carátulas, botones de fila ni barras de progreso. Oculta cabecera, navegación, filtros y pie. Su ventana puede reducirse hasta 240 × 90 unidades lógicas de Windows. Arrastra el borde superior y redimensiona desde la esquina inferior derecha. **F6 o clic derecho → Salir de miniatura** vuelve a la vista normal. Ambos tamaños se guardan por separado; no modifica el progreso ni los archivos. No carga carátulas mientras está activa.

En Miniatura, **clic derecho sobre un juego** permite cambiar entre pendiente, jugando, pausado, historia terminada y abandonado. El menú marca el estado actual y también permite salir de Miniatura o abrir Ajustes. Los cambios se guardan y siguen las reglas habituales de progreso y compartición: no se comparte un juego nuevo por cambiar su estado. La lista continúa mostrando solo nombre y estado.

También puedes usar el teclado en Miniatura: **↑/↓** para recorrer los juegos, **Inicio/Fin** para ir al primero/último y **Enter/Espacio** para abrir el menú del juego activo. El contorno resalta la fila enfocada. Al cerrar el menú o cambiar el estado, el foco vuelve a ese juego. No afecta a los atajos de las otras vistas.

Las actualizaciones conservan el juego seleccionado y recuperan su foco de teclado solo si la lista tenía el foco. Retirarlo de Mi lista limpia la selección. Cambiar de vista o mostrar el widget ajusta la ventana al área de trabajo del monitor actual.

**Clic derecho en el fondo o un juego → Mantener siempre visible / Bloquear posición y tamaño.** Las opciones marcadas reflejan los ajustes guardados; se aplican inmediatamente y se conservan. Desbloquea desde el mismo menú para mover o redimensionar. El borde de arrastre usa un cursor normal al bloquearse. La lista conserva únicamente nombres y estados.

**Ctrl+F o clic derecho → Buscar juego** abre la colección normal y enfoca su buscador. Guarda esa vista y conserva el tamaño de Miniatura para cuando vuelvas. Desde Amigos, Ctrl+F también abre la búsqueda visible de la colección. Si existe una consulta, se selecciona para sustituirla al escribir; su texto se conserva hasta cambiarlo. Miniatura sigue mostrando únicamente nombres y estados.

**Clic derecho en un juego → Editar juego, o F2 sobre el seleccionado** abre su ficha habitual manteniendo Miniatura activa. Puedes editar notas, tareas y los demás campos del editor. Guardar aplica los cambios; Cancelar conserva los datos guardados. Al cerrar recupera el foco si el juego sigue visible. Desmarcar Mostrar en Mi lista retira su fila de Miniatura sin borrar el juego de la biblioteca. Las filas siguen mostrando solo nombre y estado.

**Clic derecho en el fondo o un juego → Añadir juego**, también con la lista vacía. Se abre la ficha habitual manteniendo Miniatura activa. Guardar crea el juego y Cancelar no deja una entrada. Los juegos marcados Mostrar en Mi lista aparecen como filas de nombre/estado. Ctrl+N sigue disponible. La vista vacía conserva su interfaz mínima.

**Re Pág/Av Pág** recorren Miniatura por una página visible, según la altura actual de ventana y fila. Redimensionar cambia el salto. Se detiene en el primer/último juego y conserva el foco en la fila activa; las listas grandes siguen virtualizadas. Actualizar sin foco en la lista conserva el desplazamiento de lectura existente. Buscar cierra el menú antes de enfocar el buscador normal.

**Ajustes → Tamaño de texto en Miniatura** permite valores de 12 a 20, con 12 como predeterminado. Guardar aplica y conserva la elección; Cancelar mantiene el valor guardado. Nombres, estados y altura de fila crecen juntos. Re Pág/Av Pág se adaptan a las filas ampliadas. El texto de otras vistas se conserva. Amplía Miniatura si se recortan nombres largos; su tooltip mantiene el título completo. No necesita instalaciones adicionales.

**Clic derecho → Salir de miniatura** recupera lista, compacta o cuadrícula usada antes de entrar en Miniatura. Guarda la vista restaurada y utiliza su tamaño normal independiente, limitado al espacio de la pantalla. Guardar Ajustes mientras Miniatura está activa conserva la vista recordada. **F6** mantiene el ciclo lista → compacta → cuadrícula → Miniatura → lista; **Buscar juego / Ctrl+F** abre la lista normal con su buscador.

## Modo ligero opcional

Actívalo en **Ajustes → Modo ligero (sin carátulas)**. Oculta las carátulas de colección y amigos, evita nuevas descargas de imágenes y vacía la caché de imágenes decodificadas. Los juegos, objetivos, progreso, archivos de carátulas y publicaciones se conservan; puedes desactivarlo cuando quieras. La elección queda guardada. Una descarga ya iniciada puede terminar. No desactiva Steam, la sincronización ni el envío de publicaciones pendientes.

## Consumo y límites

La interfaz virtualiza las listas; las carátulas recicladas liberan sus referencias y la caché de imágenes decodificadas tiene un presupuesto estimado de 8 MiB y un máximo de 32 entradas. Ese presupuesto no es la memoria total de la app. Los recursos de .NET no utilizados de otros idiomas no se distribuyen. El progreso de amigos no se consulta periódicamente mientras el widget está oculto o minimizado; las publicaciones pendientes siguen pudiendo enviarse.

No hay un mínimo de RAM/CPU medido en equipos antiguos. No se promete una cifra inventada: el consumo depende del tamaño de la biblioteca, la ventana y las imágenes. Las carátulas y copias pueden aumentar el espacio utilizado; el usuario controla sus datos.

Código, documentación y descargas: [Nicolasmf05/Checkpoint](https://github.com/Nicolasmf05/Checkpoint). Guía: [seguridad de Windows](https://github.com/Nicolasmf05/Checkpoint/blob/main/docs/WINDOWS-SECURITY.md). [Reputación de SmartScreen según Microsoft](https://learn.microsoft.com/windows/apps/package-and-deploy/smartscreen-reputation).

**Idioma coherente:** los avisos y sus botones siguen el idioma elegido en Ajustes, incluido Miniatura y las ventanas de edición. Los cuadros propios de Windows siguen el idioma del sistema.

**Código de amigo completo:** Amigos → Cuenta muestra y copia `checkpoint-` seguido de 12 caracteres. Introduce este formato en Checkpoint 0.6.16 o posterior; los códigos anteriores siguen funcionando.
