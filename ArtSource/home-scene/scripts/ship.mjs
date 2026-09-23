// Renders nothing: takes the full-quality master in out/ and puts the game's copy of the loop and
// its poster where HubSceneVideo loads them. Usage: npm run ship (after npm run master).
//
// ⚠️ THE GAME COPY IS RE-ENCODED, NOT THE MASTER. The repo has no LFS, and the crf 17 master is
// ~51 MB. `-tune animation` at crf 21 keeps her flat fills clean at ~23 MB. No audio track: the
// loop is silent by design (docs/reports/home-scene/README.md § 6).
import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';

const master = path.resolve('out/zack_home_loop_1080p30.mp4');
const home = path.resolve('../../Assets/TumbangPreso/Resources/UI/home');
if (!fs.existsSync(master)) throw new Error('No master at ' + master + '. Run npm run master first.');

execFileSync('ffmpeg', ['-v', 'error', '-y', '-i', master,
  '-c:v', 'libx264', '-preset', 'slow', '-tune', 'animation', '-crf', '21',
  '-profile:v', 'high', '-level', '4.1', '-pix_fmt', 'yuv420p', '-g', '60',
  '-movflags', '+faststart', '-an', path.join(home, 'zack-home-loop.mp4')], { stdio: 'inherit' });

// The poster is frame 0, so the hub shows the loop's own first frame before the decoder is ready.
execFileSync('node', ['scripts/stills.mjs', 'poster', '0'], { stdio: 'inherit' });
fs.copyFileSync(path.resolve('scratch/poster_0000.png'), path.resolve('renders/zack-home-poster.png'));
fs.copyFileSync(path.resolve('scratch/poster_0000.png'), path.join(home, 'zack-home-poster.png'));
for (const f of ['zack-home-loop.mp4', 'zack-home-poster.png'])
  console.log(f, (fs.statSync(path.join(home, f)).size / 1e6).toFixed(1), 'MB');
