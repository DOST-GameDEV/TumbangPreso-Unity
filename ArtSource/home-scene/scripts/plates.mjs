// Records the real map through a hero's film camera, with the game's own cast acting in it. Effects are composited over these plates.
// Usage: node scripts/plates.mjs <zack|phaister>             the whole film and idle, one unbroken street
//        node scripts/plates.mjs <zack|phaister> <frame...>  just those film frames, to check an angle (60 fps numbering).
//                                                            The street's clock still runs through the frames between, so traffic and the train are where they will be.
//        SCOUT=file.json node scripts/plates.mjs phaister    a list of [{ "p": [x,y,z], "t": [x,y,z], "f": zoom }] lenses instead of the film's camera, to look round a map
// Plates land in plates/<hero>-intro, -idle and -idlepre (ignored by git; this rebuilds them).
// Re-record after changing the camera, the acting or the map. One Unity job per machine; nothing else may have the project open.
import { spawnSync } from 'node:child_process';
import './platesrv.mjs';
import { bundle } from '@remotion/bundler';
import { renderStill, selectComposition } from '@remotion/renderer';
import { build } from 'esbuild';
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

const DIRS = { zack: 'bgc', phaister: 'ermita' };
const hero = process.argv[2];
if (!DIRS[hero]) throw new Error('Usage: node scripts/plates.mjs <zack|phaister> [frame...]');
const picks = process.argv.slice(3).map(Number);

fs.mkdirSync('scratch', { recursive: true });
const entry = path.resolve('scratch/_film.mjs');
await build({
  stdin: { contents: `export { PLATES } from './src/${DIRS[hero]}/film';`, resolveDir: process.cwd(), loader: 'ts' },
  bundle: true, format: 'esm', platform: 'node', outfile: entry, logLevel: 'error',
});
const { PLATES: P } = await import(pathToFileURL(entry).href + `?${Date.now()}`);
const { INTRO, IDLE } = P;

// The film's acting, computed on its rigs in the browser, for the game's own models to wear.
const serveUrl = await bundle({ entryPoint: path.resolve('src/index.ts') });
const composition = await selectComposition({ serveUrl, id: P.poses });
await renderStill({ serveUrl, composition, frame: 0, output: path.resolve('scratch/_poses.png'), imageFormat: 'png', timeoutInMilliseconds: 900000, chromiumOptions: { gl: 'angle' } });
const poses = JSON.parse(fs.readFileSync('scratch/poses.json', 'utf8'));

const view = (clip, i, f, dt, skip = false) => {
  const row = poses.rows[String(f >= INTRO ? INTRO + ((f - INTRO) % IDLE) : f)];
  if (skip) return { clip, i, skip: true, dt };
  const v = P.camAt(f);
  return { clip, i, x: v.x, y: v.y, z: v.z, yaw: v.yaw, pitch: v.pitch, roll: v.roll, f: v.f, dt, subjects: P.subjectsAt(f), a: row.a, b: row.b, kuro: row.k, can: row.c, moths: row.m ?? [] };
};

const frames = [];
const FILM = INTRO * 2;
if (process.env.SCOUT) {
  const rad = 180 / Math.PI;
  JSON.parse(fs.readFileSync(process.env.SCOUT, 'utf8')).forEach((c, i) => {
    const d = [c.t[0] - c.p[0], c.t[1] - c.p[1], c.t[2] - c.p[2]];
    const base = view('scout', i, 0, 1 / 60);
    frames.push({ ...base, x: c.p[0], y: c.p[1], z: c.p[2], yaw: Math.atan2(d[0], d[2]) * rad, pitch: -Math.atan2(d[1], Math.hypot(d[0], d[2])) * rad, roll: 0, f: c.f, subjects: [] });
  });
} else if (picks.length) {
  const wanted = new Set(picks);
  const last = Math.max(...picks);
  for (let i = 0; i <= last; i++) frames.push(view(`${hero}-intro`, i, i / 2, P.rate(i / 2) / 60, !wanted.has(i)));
} else {
  const BLEND = 60; // idle frames over which the idle's end crossfades into the street at its start
  for (let i = 0; i < FILM; i++) {
    // The last of the film also looks through the idle's lens: those are what the idle's end fades to, so its loop has no seam.
    const pre = (i - (FILM - BLEND * 2)) / 2;
    if (pre >= 0 && Number.isInteger(pre)) frames.push(view(`${hero}-idlepre`, pre, INTRO + IDLE - BLEND + pre, 0));
    frames.push(view(`${hero}-intro`, i, i / 2, P.rate(i / 2) / 60));
  }
  for (let i = 0; i < IDLE; i++) frames.push(view(`${hero}-idle`, i, INTRO + i, 1 / 30));
}
const camFile = path.resolve('scratch', `plates_${hero}.json`);
fs.writeFileSync(camFile, JSON.stringify({ map: P.map, floor: P.floor, cast: P.cast, kuro: P.kuro, bones: poses.bones, warm: 900, green: P.green, train: process.env.SCOUT ? -1 : P.train, trainDir: P.trainDir ?? 1, film: process.env.SCOUT || picks.length ? 0 : FILM, frames }));

const repo = path.resolve('../..');
const log = path.join(repo, 'Logs', `home-plates-${hero}.log`);
const run = spawnSync('python', ['tools/run_unity_guarded.py', '-batchmode', '-force-d3d11', '-tp-profile', 'presentation-validation-20260921', '-executeMethod', 'TumbangPreso.EditorTools.HomeFilmPlateRecorder.Record', '-tp-plate-cam', camFile, '-tp-plate-out', path.resolve('plates'), '-logFile', log], { cwd: repo, stdio: 'inherit' });
console.log('exit', run.status, 'log', log);
process.exit(run.status ?? 1);
