// Stills of any composition, one bundle, one contact sheet. Usage:
// node scripts/look.mjs <Composition> <tag> <frame> [frame...]   (files in scratch/, sheet scratch/<tag>_sheet.png)
import { bundle } from '@remotion/bundler';
import { renderStill, selectComposition } from '@remotion/renderer';
import path from 'node:path';
import sharp from 'sharp';

const [id, tag, ...frames] = process.argv.slice(2);
const serveUrl = await bundle({ entryPoint: path.resolve('src/index.ts') });
const composition = await selectComposition({ serveUrl, id });
const files = [];
for (const frame of frames.map(Number)) {
  const out = path.resolve('scratch', `${tag}_${String(frame).padStart(4, '0')}.png`);
  await renderStill({ serveUrl, composition, frame, output: out, imageFormat: 'png', scale: 0.5, chromiumOptions: { gl: 'angle' } });
  files.push(out);
}
const cols = Math.min(4, files.length);
const W = Number(process.env.TILE ?? 480);
const H = Math.round(W * 9 / 16);
const rows = Math.ceil(files.length / cols);
const tiles = await Promise.all(files.map((f, i) => sharp(f).resize(W, H).png().toBuffer().then((input) => ({ input, left: (i % cols) * W, top: Math.floor(i / cols) * H }))));
await sharp({ create: { width: W * cols, height: H * rows, channels: 3, background: '#000' } }).composite(tiles).png().toFile(path.resolve('scratch', `${tag}_sheet.png`));
console.log('sheet', path.resolve('scratch', `${tag}_sheet.png`));
