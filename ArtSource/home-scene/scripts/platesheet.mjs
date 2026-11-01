// A contact sheet of recorded plates. Usage: node scripts/platesheet.mjs <clip> <out.png> <frame> [frame...]
import path from 'node:path';
import sharp from 'sharp';

const [clip, out, ...frames] = process.argv.slice(2);
const W = 480;
const H = 270;
const cols = 4;
const tiles = await Promise.all(frames.map(async (f, i) => ({
  input: await sharp(path.resolve('plates', clip, `${String(f).padStart(5, '0')}.jpg`)).resize(W, H).png().toBuffer(),
  left: (i % cols) * W,
  top: Math.floor(i / cols) * H,
})));
await sharp({ create: { width: W * cols, height: H * Math.ceil(frames.length / cols), channels: 3, background: '#000' } }).composite(tiles).png().toFile(out);
