// Renders nothing but the posters: takes a hero's two masters in out/ and puts the game's copies where
// HubSceneVideo loads them. The film is <hero>-home-intro, the idle keeps the <hero>-home-loop name.
// Usage: node scripts/ship-zack.mjs [zack|phaister], after both masters (scripts/chunks.mjs <Hero>Intro and <Hero>Idle).
import './platesrv.mjs';
import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { bundle } from '@remotion/bundler';
import { renderStill, selectComposition } from '@remotion/renderer';

const home = path.resolve('../../Assets/TumbangPreso/Resources/UI/home');
fs.mkdirSync(path.resolve('renders'), { recursive: true });
const hero = process.argv[2] ?? 'zack';
const Hero = hero[0].toUpperCase() + hero.slice(1);
// ⚠️ ONE CRF FOR BOTH CLIPS, so nothing changes at the swap. No animation tune (owner: "medyo blurry"): the tune's heavy deblocking softened the map's fine detail. 18 keeps Phaister's film at 93 MB; 17 is 106 MB, over GitHub's 100 MB limit.
const CRF = 18;
const parts = [
  { master: `out/${hero}_intro_1080p60.mp4`, clip: `${hero}-home-intro.mp4`, poster: `${hero}-home-intro-poster.png`, id: `${Hero}Intro`, crf: CRF },
  { master: `out/${hero}_idle_1080p30.mp4`, clip: `${hero}-home-loop.mp4`, poster: `${hero}-home-poster.png`, id: `${Hero}Idle`, crf: CRF },
];
const serveUrl = await bundle({ entryPoint: path.resolve('src/index.ts') });
fs.mkdirSync(path.resolve('scratch'), { recursive: true });
for (const p of parts) {
  execFileSync('ffmpeg', ['-v', 'error', '-y', '-i', path.resolve(p.master),
    '-c:v', 'libx264', '-preset', 'slow', '-x264-params', 'deblock=-1,-1', '-crf', String(p.crf),
    '-profile:v', 'high', '-level', '4.1', '-pix_fmt', 'yuv420p', '-g', '60',
    '-movflags', '+faststart', '-an', path.join(home, p.clip)], { stdio: 'inherit' });
  const composition = await selectComposition({ serveUrl, id: p.id });
  const still = path.resolve('scratch', p.poster);
  await renderStill({ serveUrl, composition, frame: 0, output: still, imageFormat: 'png', chromiumOptions: { gl: 'angle' } });
  fs.copyFileSync(still, path.resolve('renders', p.poster));
  fs.copyFileSync(still, path.join(home, p.poster));
  for (const f of [p.clip, p.poster]) console.log(f, (fs.statSync(path.join(home, f)).size / 1e6).toFixed(1), 'MB');
}
