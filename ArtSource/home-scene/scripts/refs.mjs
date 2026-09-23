// Copies the two character models and the tsinelas the reference renders photograph (ZackRef, RigTest,
// scripts/ref.mjs, scripts/refclip.mjs) out of the game into public/ref/. They are not committed
// twice: the game's copies under Assets/ are the source. Usage: npm run refs
import fs from 'node:fs';
import path from 'node:path';

const persons = path.resolve('../../Assets/TumbangPreso/Art/characters/persons');
const out = path.resolve('public/ref');
fs.mkdirSync(path.join(out, 'Textures'), { recursive: true });
for (const f of ['team-zack.glb', 'team-sean.glb', 'Textures/colormap.png']) {
  fs.copyFileSync(path.join(persons, f), path.join(out, f));
  console.log('copied', f);
}
// The tsinelas he holds is the game's own.
fs.copyFileSync(path.resolve('../../Assets/TumbangPreso/Art/models/tsinelas_tsinelas.glb'), path.join(out, 'tsinelas_tsinelas.glb'));
console.log('copied tsinelas_tsinelas.glb');
