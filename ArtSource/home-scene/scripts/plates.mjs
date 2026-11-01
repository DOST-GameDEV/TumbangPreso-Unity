// Records the real map through Zack's film camera, as the plates his cast and effects are composited over.
// Usage: node scripts/plates.mjs             the whole film and idle, one unbroken street
//        node scripts/plates.mjs <frame...>  just those film frames, to check an angle (60 fps numbering)
// Plates land in plates/zack-intro, zack-idle and zack-idlepre (ignored by git; this rebuilds them).
// Re-record after changing camera.ts or the map. One Unity job per machine; nothing else may have the project open.
import { spawnSync } from 'node:child_process';
import { build } from 'esbuild';
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

fs.mkdirSync('scratch', { recursive: true });
const entry = path.resolve('scratch/_cam.mjs');
await build({
  stdin: { contents: "export { camAt } from './src/bgc/camera'; export { zack, rago, canAt } from './src/bgc/perform'; export { B } from './src/bgc/beats'; export { INTRO, IDLE } from './src/bgc/time';", resolveDir: process.cwd(), loader: 'ts' },
  bundle: true, format: 'esm', platform: 'node', outfile: entry, logLevel: 'error',
});
const { camAt, zack, rago, canAt, B, INTRO, IDLE } = await import(pathToFileURL(entry).href);

/** What must stay clear of the map, on this frame. */
const subjectsAt = (f) => {
  const z = zack(f);
  const r = rago(f);
  const c = canAt(f);
  const pt = (x, y, zz) => ({ x, y, z: zz });
  return [pt(z.x, 0.2, z.z), pt(z.x, 1.1, z.z), pt(z.x, 2, z.z), pt(r.x, 0.2, r.z), pt(r.x, 1.1, r.z), pt(r.x, 2.2, r.z), pt(c.x, 0.3, c.z)];
};
const view = (clip, i, f, dt) => {
  const v = camAt(f);
  return { clip, i, x: v.x, y: v.y, z: v.z, yaw: v.yaw, pitch: v.pitch, roll: v.roll, f: v.f, dt, subjects: subjectsAt(f) };
};

const picks = process.argv.slice(2).map(Number);
const frames = [];
if (picks.length) {
  for (const i of picks) frames.push(view('zack-intro', i, i / 2, 1 / 60));
} else {
  const FILM = INTRO * 2;
  const BLEND = 60; // idle frames over which the idle's end crossfades into the street at its start
  for (let i = 0; i < FILM; i++) {
    // The last of the film also looks through the idle's lens: those are what the idle's end fades to, so its loop has no seam.
    const pre = (i - (FILM - BLEND * 2)) / 2;
    if (pre >= 0 && Number.isInteger(pre)) frames.push(view('zack-idlepre', pre, INTRO + IDLE - BLEND + pre, 0));
    frames.push(view('zack-intro', i, i / 2, 1 / 60));
  }
  for (let i = 0; i < IDLE; i++) frames.push(view('zack-idle', i, INTRO + i, 1 / 30));
}
const camFile = path.resolve('scratch', 'plates_zack.json');
// The film's clock is 30 a second, and its green opens at B.green.
fs.writeFileSync(camFile, JSON.stringify({ map: 'Kanto', warm: 900, green: picks.length ? -1 : B.green / 30, frames }));

const repo = path.resolve('../..');
const log = path.join(repo, 'Logs', 'home-plates-zack.log');
const run = spawnSync('python', ['tools/run_unity_guarded.py', '-batchmode', '-force-d3d11', '-tp-profile', 'presentation-validation-20260921', '-executeMethod', 'TumbangPreso.EditorTools.HomeFilmPlateRecorder.Record', '-tp-plate-cam', camFile, '-tp-plate-out', path.resolve('plates'), '-logFile', log], { cwd: repo, stdio: 'inherit' });
console.log('exit', run.status, 'log', log);
process.exit(run.status ?? 1);
