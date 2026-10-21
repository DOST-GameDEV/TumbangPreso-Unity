import { bundle } from '@remotion/bundler';
import { renderStill, selectComposition } from '@remotion/renderer';
import path from 'node:path';
import fs from 'node:fs';
const [model, tres, clip, angle = '90'] = process.argv.slice(2);
const m = fs.readFileSync(tres, 'utf8').match(/palette = PackedColorArray\(([^)]*)\)/);
const n = m[1].split(',').map(Number);
const palette = [];
for (let i = 0; i < n.length; i += 4) palette.push([n[i], n[i + 1], n[i + 2]]);
const serveUrl = await bundle({ entryPoint: path.resolve('src/index.ts') });
const inputProps = { model, palette, clip, angle: Number(angle) };
const composition = await selectComposition({ serveUrl, id: 'ZackRef', inputProps });
for (let frame = 0; frame < 8; frame++) {
  const out = path.resolve('scratch', `clip_${model}_${clip}_${angle}_${frame}.png`);
  await renderStill({ serveUrl, composition, frame, output: out, imageFormat: 'png', inputProps, chromiumOptions: { gl: 'angle' } });
}
console.log('done');
