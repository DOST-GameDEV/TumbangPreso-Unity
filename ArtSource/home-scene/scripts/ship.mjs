// Renders nothing: takes a hero's full-quality master in out/ and puts the game's copy of the loop
// and its poster where HubSceneVideo loads them. Usage: npm run ship (Zack), npm run ship:phaister,
// or node scripts/ship.mjs <hero>, after that hero's master.
//
// ⚠️ THE GAME COPY IS RE-ENCODED, NOT THE MASTER. The repo has no LFS, and a crf 17 master is ~50 MB.
// `-tune animation` at crf 21 keeps the flat fills clean at 25 to 38 MB. No audio track: the loops
// are silent by design (docs/reports/home-scene/README.md § 6).
//
// ⚠️ THE FILE NAMES ARE THE CONTRACT with HubSceneVideo.ClipPathFor / PosterPathFor:
// Resources/UI/home/<hero>-home-loop.mp4 and <hero>-home-poster.png. A hero also has to be listed in
// HubSceneVideo.Heroes before the random pick can land on it.
import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { bundle } from '@remotion/bundler';
import { renderStill, selectComposition } from '@remotion/renderer';

const hero = process.argv[2] ?? 'zack';
const HEROES = {
  zack: { master: 'out/zack_home_loop_1080p30.mp4', composition: 'HomeScene', crf: 21 },
  // ⚠️ crf 22 FOR HER NIGHT: the dark grain costs bits, and at 21 her copy was 41.2 MB against the
  // 25 to 38 MB budget. Measured 2026-09-24 against the crf 17 master: crf 21 SSIM 0.977 at 41.2 MB,
  // crf 22 SSIM 0.974 at 33.9 MB, crf 23 SSIM 0.970 at 27.6 MB.
  phaister: { master: 'out/phaister_home_loop_1080p30.mp4', composition: 'PhaisterScene', crf: 22 },
};
const h = HEROES[hero];
if (!h) throw new Error(`Unknown hero ${hero}; add it to HEROES in scripts/ship.mjs.`);
const master = path.resolve(h.master);
const home = path.resolve('../../Assets/TumbangPreso/Resources/UI/home');
if (!fs.existsSync(master)) throw new Error('No master at ' + master + '. Render its master first.');

const clip = `${hero}-home-loop.mp4`;
const poster = `${hero}-home-poster.png`;
execFileSync('ffmpeg', ['-v', 'error', '-y', '-i', master,
  '-c:v', 'libx264', '-preset', 'slow', '-tune', 'animation', '-crf', String(h.crf),
  '-profile:v', 'high', '-level', '4.1', '-pix_fmt', 'yuv420p', '-g', '60',
  '-movflags', '+faststart', '-an', path.join(home, clip)], { stdio: 'inherit' });

// The poster is frame 0, so the hub shows the loop's own first frame before the decoder is ready.
const serveUrl = await bundle({ entryPoint: path.resolve('src/index.ts') });
const composition = await selectComposition({ serveUrl, id: h.composition });
const still = path.resolve('scratch', `poster_${hero}.png`);
fs.mkdirSync(path.resolve('scratch'), { recursive: true });
await renderStill({ serveUrl, composition, frame: 0, output: still, imageFormat: 'png', chromiumOptions: { gl: 'angle' } });
fs.copyFileSync(still, path.resolve('renders', poster));
fs.copyFileSync(still, path.join(home, poster));
for (const f of [clip, poster]) console.log(f, (fs.statSync(path.join(home, f)).size / 1e6).toFixed(1), 'MB');
