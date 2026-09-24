// Pulls listed frames out of a rendered video into scratch/<tag>_NNNN.png, for per-beat review sheets
// (docs/HOME_SCREEN_ANIMATION_METHOD.md § 5: look at every frame of a transition, not samples).
// Usage: node scripts/frames.mjs <video> <tag> <frame> [frame...]   (frames at 30 fps)
// Then: node scripts/sheet.mjs <tag> 4
import { execFileSync } from 'node:child_process';
import path from 'node:path';

const [video, tag, ...frames] = process.argv.slice(2);
for (const f of frames.map(Number)) {
  const out = path.resolve('scratch', `${tag}_${String(f).padStart(4, '0')}.png`);
  execFileSync('ffmpeg', ['-v', 'error', '-y', '-ss', (f / 30).toFixed(4), '-i', video, '-frames:v', '1', out]);
}
console.log(frames.length, 'frames from', video);
