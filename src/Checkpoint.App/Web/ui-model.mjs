// Cálculos puros para virtualización y navegación por teclado.
// El DOM solo crea las filas del intervalo visible más un pequeño margen.

// Calcula el intervalo de filas que cubre la pantalla con margen para desplazarse sin mostrar huecos.
export function visibleRange(count, scroll, height, rowHeight, columns = 1) {
  const start = Math.max(0, Math.floor(scroll / rowHeight) - 2);
  const end = Math.min(Math.ceil(count / columns), Math.ceil((scroll + height) / rowHeight) + 2);
  return {
    start: start * columns,
    end: end * columns,
    height: Math.ceil(count / columns) * rowHeight,
  };
}
// Limita los movimientos de teclado al primer y último elemento disponibles.
export function nextIndex(index, key, count, page) {
  if (!count) return -1;
  const current = Math.max(0, index);
  return Math.max(
    0,
    Math.min(
      count - 1,
      key === 'Home'
        ? 0
        : key === 'End'
          ? count - 1
          : current +
            (key === 'ArrowUp' ? -1 : key === 'ArrowDown' ? 1 : key === 'PageUp' ? -page : page),
    ),
  );
}
