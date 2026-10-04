// Comprueba límites de virtualización y desplazamiento de selección por teclado.

import test from 'node:test';
import assert from 'node:assert/strict';
import { visibleRange, nextIndex } from '../../src/Checkpoint.App/Web/ui-model.mjs';

test('large collections create only viewport rows with bounded overscan', () => {
  const range = visibleRange(10000, 200000, 220, 43);
  assert.ok(range.end - range.start < 12);
  assert.equal(range.height, 430000);
  assert.ok(range.start > 4000);
});
test('grid virtualization keeps complete rows and handles an incomplete last row', () => {
  const range = visibleRange(101, 7200, 400, 230, 3);
  assert.equal(range.start % 3, 0);
  assert.equal(range.end, 102);
  assert.equal(range.height, 34 * 230);
});
test('page navigation clamps both ends and adjusts to the viewport', () => {
  assert.equal(nextIndex(0, 'PageUp', 1004, 5), 0);
  assert.equal(nextIndex(1003, 'PageDown', 1004, 5), 1003);
  assert.equal(nextIndex(1003, 'PageUp', 1004, 5), 998);
  assert.equal(nextIndex(1003, 'PageUp', 1004, 10), 993);
});
test('keyboard navigation works without selection and with an empty collection', () => {
  assert.equal(nextIndex(-1, 'Home', 1004, 5), 0);
  assert.equal(nextIndex(0, 'End', 1004, 5), 1003);
  assert.equal(nextIndex(-1, 'ArrowDown', 0, 5), -1);
});
