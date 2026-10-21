// Renders a list of frames to scratch/ with one bundle, so a look at five frames costs one
// webpack build rather than five. Usage: node scripts/stills.mjs <tag> <frame> [frame...]
import { bundle } from '@remotion/bundler';
import { renderStill, selectComposition } from '@remotion/renderer';
import path from 'node:path';

const [tag = 'look', ...frames] = process.argv.slice(2);
const list = frames.length ? frames.map(Number) : [0];
const serveUrl = await bundle({ entryPoint: path.resolve('src/index.ts') });
const composition = await selectComposition({ serveUrl, id: 'HomeScene' });
for (const frame of list) {
  const out = path.resolve('scratch', `${tag}_${String(frame).padStart(4, '0')}.png`);
  const t = Date.now();
  await renderStill({ serveUrl, composition, frame, output: out, imageFormat: 'png', chromiumOptions: { gl: 'angle' } });
  console.log(out, `${Date.now() - t} ms`);
}
