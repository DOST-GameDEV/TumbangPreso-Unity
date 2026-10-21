import { bundle } from '@remotion/bundler';
import { renderStill, selectComposition } from '@remotion/renderer';
import path from 'node:path';
const [tag = 'rig'] = process.argv.slice(2);
const serveUrl = await bundle({ entryPoint: path.resolve('src/index.ts') });
const composition = await selectComposition({ serveUrl, id: 'RigTest' });
await renderStill({ serveUrl, composition, frame: 0, output: path.resolve('scratch', `${tag}.png`), imageFormat: 'png' });
console.log('ok');
