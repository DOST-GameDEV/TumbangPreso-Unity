// One composition, listed frames, to scratch/: node scripts/still.mjs <Composition> <tag> <frame...>
// ⚠️ gl 'angle': the models are WebGL, and the default headless GL renders them blank.
import { bundle } from '@remotion/bundler';
import { renderStill, selectComposition } from '@remotion/renderer';
import path from 'node:path';
const [id, tag, ...frames] = process.argv.slice(2);
const serveUrl = await bundle({ entryPoint: path.resolve('src/index.ts') });
const composition = await selectComposition({ serveUrl, id });
for (const frame of (frames.length ? frames : ['0']).map(Number)) {
  const out = path.resolve('scratch', `${tag}_${String(frame).padStart(4, '0')}.png`);
  await renderStill({ serveUrl, composition, frame, output: out, imageFormat: 'png', chromiumOptions: { gl: 'angle' } });
  console.log(out);
}
