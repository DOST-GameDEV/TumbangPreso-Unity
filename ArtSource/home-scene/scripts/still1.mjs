// One full-resolution frame. Usage: node scripts/still1.mjs <Composition> <frame> <out.png>
import './platesrv.mjs';
import { bundle } from '@remotion/bundler';
import { renderStill, selectComposition } from '@remotion/renderer';
import path from 'node:path';
const [id, frame, out] = process.argv.slice(2);
const serveUrl = await bundle({ entryPoint: path.resolve('src/index.ts') });
const composition = await selectComposition({ serveUrl, id });
await renderStill({ serveUrl, composition, frame: Number(frame), output: path.resolve(out), imageFormat: 'png', chromiumOptions: { gl: 'angle' } });
