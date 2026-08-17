import { copyFileSync, mkdirSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

// pdf.js ships its worker as .mjs, an extension plenty of static servers still answer with
// application/octet-stream — and a browser refuses to run a module script served with a non-JavaScript MIME
// type, so the viewer dies with "Setting up fake worker failed". Copying it under a .js name sidesteps every
// server's MIME table instead of asking each host to be configured correctly.
const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const source = resolve(root, 'node_modules/pdfjs-dist/build/pdf.worker.min.mjs');
const target = resolve(root, 'public/pdf.worker.min.js');

mkdirSync(dirname(target), { recursive: true });
copyFileSync(source, target);
