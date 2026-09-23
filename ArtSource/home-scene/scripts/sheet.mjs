// Contact sheet of rendered stills: node scripts/sheet.mjs <tag> <cols> <out>
import sharp from 'sharp';
import fs from 'node:fs';
const [tag, cols = '4', out = `scratch/${tag}_sheet.png`] = process.argv.slice(2);
const files = fs.readdirSync('scratch').filter((f) => f.startsWith(tag + '_') && /_\d{4}\.png$/.test(f)).sort();
const W = 640, H = 360, c = Number(cols), rows = Math.ceil(files.length / c);
const comp = [];
for (let i = 0; i < files.length; i++) {
  const buf = await sharp('scratch/' + files[i]).resize(W, H).toBuffer();
  const label = Buffer.from(`<svg width="${W}" height="40"><rect width="120" height="34" fill="black" opacity="0.6"/><text x="8" y="26" font-size="24" fill="white" font-family="Arial">${files[i].match(/_(\d{4})/)[1]}</text></svg>`);
  comp.push({ input: buf, left: (i % c) * W, top: Math.floor(i / c) * H }, { input: label, left: (i % c) * W, top: Math.floor(i / c) * H });
}
await sharp({ create: { width: W * c, height: H * rows, channels: 3, background: '#000' } }).composite(comp).png().toFile(out);
console.log(out, files.length);
