import { bundle } from '@remotion/bundler';
import { renderStill, selectComposition } from '@remotion/renderer';
import path from 'node:path';
import fs from 'node:fs';
// Usage: node scripts/ref.mjs <model> [tres path]
const [model = 'team-zack', tres] = process.argv.slice(2);
let palette;
if (tres) {
  const m = fs.readFileSync(tres, 'utf8').match(/palette = PackedColorArray\(([^)]*)\)/);
  const n = m[1].split(',').map(Number);
  palette = [];
  for (let i = 0; i < n.length; i += 4) palette.push([n[i], n[i + 1], n[i + 2]]);
}
const serveUrl = await bundle({ entryPoint: path.resolve('src/index.ts') });
const inputProps = { model, palette };
const composition = await selectComposition({ serveUrl, id: 'ZackRef', inputProps });
for (let frame = 0; frame < 6; frame++) {
  const out = path.resolve('scratch', `${model}_${frame}.png`);
  await renderStill({ serveUrl, composition, frame, output: out, imageFormat: 'png', inputProps, chromiumOptions: { gl: 'angle' } });
  console.log(out);
}
