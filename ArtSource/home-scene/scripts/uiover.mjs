// Lays HOME's real board art (Resources/UI/handmade-lobby, board.json) over a frame, to judge composition
// before a build. Not a substitute for the game. Usage: node scripts/uiover.mjs <in.png> <out.png>
import fs from 'node:fs';
import path from 'node:path';
import sharp from 'sharp';
const [src, out] = process.argv.slice(2);
const dir = path.resolve('../../Assets/TumbangPreso/Resources/UI/handmade-lobby');
const board = JSON.parse(fs.readFileSync(path.join(dir, 'board.json'), 'utf8'));
const order = (n) => (/_slab$/.test(n) ? 0 : /_face$/.test(n) ? 1 : 2);
const sprites = [...board.sprites].sort((a, b) => order(a.name) - order(b.name));
const layers = [];
for (const s of sprites) {
  const f = path.join(dir, s.name + '.png');
  if (!fs.existsSync(f)) continue;
  const input = await sharp(f).resize(Math.max(1, Math.round(s.w)), Math.max(1, Math.round(s.h)), { fit: 'fill' }).png().toBuffer();
  layers.push({ input, left: Math.round(s.x), top: Math.round(s.y) });
}
await sharp(src).resize(1920, 1080).composite(layers).png().toFile(out);
console.log(layers.length, 'sprites', sprites.map((s) => `${s.name}@${s.x},${s.y}`).filter((n) => /tile|play|mode_card|name_plate|currency/.test(n)).join(' '));
