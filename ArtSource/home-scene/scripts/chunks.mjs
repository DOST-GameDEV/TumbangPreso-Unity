// Renders a composition in chunks and joins them, each chunk in short pieces with a fresh browser.
// Usage: node scripts/chunks.mjs <Composition> <out.mp4> [scale=1] [crf=17] [chunk=270] [concurrency=2] [piece=45]
//
// ⚠️ WHY CHUNKS. Phaister's second loop draws three posed models, a can and a slipper per frame through
// WebGL. A finished chunk is kept on disk and skipped on a rerun, so an interrupted master resumes.
//
// ⚠️⚠️ WHY PIECES, AND WHY A FRESH BROWSER FOR EACH. Every figure in every frame is rendered offscreen
// and read back as a PNG data URL (`three/actor.tsx` `drawActor`), up to 4096 px square on a close-up.
// A headless tab that renders frame after frame keeps accumulating those images until it spends all
// its time in garbage collection: the render stops advancing, no error is raised, and the renderer
// tabs sit at full CPU. Measured 2026-09-24 on frames 1080 to 1349 (the descent and the calm's close
// hat tip): a 270-frame chunk at concurrency 4 stalled three times, and at concurrency 2 it froze at
// its 209th frame with two tabs at ~474 s of CPU, while every frame of that range rendered alone in
// 2 to 3 s. It is the same "stall with no error" the first whole-loop render hit at frames 1178 and
// 224. A `renderMedia` call opens its own browser, so rendering a chunk as 45-frame pieces gives
// every 45 frames a clean tab; each piece took ~10 s. Do not raise `piece` or `concurrency` to go
// faster without re-measuring a close-up stretch.
import { bundle } from '@remotion/bundler';
import { renderMedia, selectComposition } from '@remotion/renderer';
import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';

const [id, out, scaleArg = '1', crfArg = '17', chunkArg = '270', concArg = '2', pieceArg = '45'] = process.argv.slice(2);
const scale = Number(scaleArg);
const crf = Number(crfArg);
const chunk = Number(chunkArg);
const concurrency = Number(concArg);
const piece = Number(pieceArg);
const serveUrl = await bundle({ entryPoint: path.resolve('src/index.ts') });
const composition = await selectComposition({ serveUrl, id });
const dir = path.resolve('scratch', `chunks_${id}_${scale}`);
fs.mkdirSync(dir, { recursive: true });

const join = (files, target) => {
  const list = target + '.txt';
  fs.writeFileSync(list, files.map((p) => `file '${p.split(path.sep).join('/')}'`).join('\n'));
  execFileSync('ffmpeg', ['-v', 'error', '-y', '-f', 'concat', '-safe', '0', '-i', list, '-c', 'copy', target], { stdio: 'inherit' });
};

const renderPiece = async (file, a, b) => {
  for (let attempt = 1; ; attempt++) {
    const t = Date.now();
    try {
      // ⚠️ A STALL IS "NO NEW FRAME FOR 90 s", NEVER A TOTAL TIME. The dawn's frames take up to
      // 9.5 s each alone, so a fixed 60 s limit killed a healthy 45-frame piece five times in a row
      // (2026-09-24) while a real freeze produces no frame at all. 45 s then failed the dawn three times running:
      // its frames take up to 9.5 s each and a fresh browser needs ~10 s before the first one lands.
      const cancel = new AbortController();
      let last = Date.now();
      const watch = setInterval(() => { if (Date.now() - last > 90000) cancel.abort(); }, 2000);
      try {
        await renderMedia({
          serveUrl, composition, codec: 'h264', outputLocation: file + '.tmp.mp4', frameRange: [a, b], scale, crf,
          pixelFormat: 'yuv420p', concurrency, chromiumOptions: { gl: 'angle' }, timeoutInMilliseconds: 60000,
          cancelSignal: (cb) => cancel.signal.addEventListener('abort', cb),
          onProgress: () => { last = Date.now(); },
        });
      } finally { clearInterval(watch); }
      fs.renameSync(file + '.tmp.mp4', file);
      console.log('piece', a, b, `${Math.round((Date.now() - t) / 1000)} s`);
      return;
    } catch (e) {
      console.log('piece', a, b, 'failed on attempt', attempt, String(e).slice(0, 200));
      if (attempt >= 5) throw e;
    }
  }
};

const parts = [];
for (let a = 0; a < composition.durationInFrames; a += chunk) {
  const b = Math.min(composition.durationInFrames - 1, a + chunk - 1);
  const file = path.join(dir, `${String(a).padStart(5, '0')}.mp4`);
  parts.push(file);
  if (fs.existsSync(file)) { console.log('have', a, b); continue; }
  const pieceDir = path.join(dir, 'pieces', String(a).padStart(5, '0'));
  fs.mkdirSync(pieceDir, { recursive: true });
  const pieces = [];
  for (let s = a; s <= b; s += piece) {
    const e = Math.min(b, s + piece - 1);
    const pf = path.join(pieceDir, `${String(s).padStart(5, '0')}.mp4`);
    pieces.push(pf);
    if (!fs.existsSync(pf)) await renderPiece(pf, s, e);
  }
  join(pieces, file);
  console.log('chunk', a, b);
}
join(parts, path.resolve(out));
console.log('wrote', out);
