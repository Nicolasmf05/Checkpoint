// Formatea declaraciones PostgreSQL y conserva los cuerpos de funciones entre dólares.
// El llamador Python verifica los tokens antes de escribir el resultado en disco.

import { readFile } from 'node:fs/promises';
import { format } from 'sql-formatter';

const source = await readFile(process.argv[2], 'utf8');
process.stdout.write(
  format(source, {
    language: 'postgresql',
    tabWidth: 2,
    keywordCase: 'upper',
    linesBetweenQueries: 1,
  }).trimEnd() + '\n',
);
