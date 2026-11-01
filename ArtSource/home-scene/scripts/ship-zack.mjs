// Renders nothing but the posters: takes Zack's two masters in out/ and puts the game's copies where
// HubSceneVideo loads them. The film is <hero>-home-intro, the idle keeps the <hero>-home-loop name.
// Usage: node scripts/ship-zack.mjs, after both masters (scripts/chunks.mjs ZackIntro and ZackIdle).
import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { bundle } from '@remotion/bundler';
import { renderStill, selectComposition } from '@remotion/renderer';

const home = path.resolve('../../Assets/TumbangPreso/Resources/UI/home');
const parts = [
  { master: 'out/zack_intro_1080p60.mp4', clip: 'zack-home-intro.mp4', poster: 'zack-home-intro-poster.png', id: 'ZackIntro', crf: 21 },
  { master: 'out/zack_idle_1080p30.mp4', clip: 'zack-home-loop.mp4', poster: 'zack-home-poster.png', id: 'ZackIdle', crf: 21 },
];
const serveUrl = await bundle({ entryPoint: path.resolve('src/index.ts') });
fs.mkdirSync(path.resolve('scratch'), { recursive: true });
for (const p of parts) {
  execFileSync('ffmpeg', ['-v', 'error', '-y', '-i', path.resolve(p.master),
    '-c:v', 'libx264', '-preset', 'slow', '-tune', 'animation', '-crf', String(p.crf),
    '-profile:v', 'high', '-level', '4.1', '-pix_fmt', 'yuv420p', '-g', '60',
    '-movflags', '+faststart', '-an', path.join(home, p.clip)], { stdio: 'inherit' });
  const composition = await selectComposition({ serveUrl, id: p.id });
  const still = path.resolve('scratch', p.poster);
  await renderStill({ serveUrl, composition, frame: 0, output: still, imageFormat: 'png', chromiumOptions: { gl: 'angle' } });
  fs.copyFileSync(still, path.resolve('renders', p.poster));
  fs.copyFileSync(still, path.join(home, p.poster));
  for (const f of [p.clip, p.poster]) console.log(f, (fs.statSync(path.join(home, f)).size / 1e6).toFixed(1), 'MB');
}
