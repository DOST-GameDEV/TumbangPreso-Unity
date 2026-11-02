// Copies the character models and the tsinelas the reference renders photograph (ZackRef, RigTest,
// scripts/ref.mjs, scripts/refclip.mjs) out of the game into public/ref/. They are not committed
// twice: the game's copies under Assets/ are the source. Usage: npm run refs
import fs from 'node:fs';
import path from 'node:path';

const persons = path.resolve('../../Assets/TumbangPreso/Art/characters/persons');
const out = path.resolve('public/ref');
fs.mkdirSync(path.join(out, 'Textures'), { recursive: true });
for (const f of ['team-zack.glb', 'team-sean.glb', 'team-phaister.glb', 'team-nemu.glb', 'phaister-doll.glb', 'Textures/colormap.png']) {
  fs.copyFileSync(path.join(persons, f), path.join(out, f));
  console.log('copied', f);
}
// The tsinelas he holds is the game's own.
fs.copyFileSync(path.resolve('../../Assets/TumbangPreso/Art/models/tsinelas_tsinelas.glb'), path.join(out, 'tsinelas_tsinelas.glb'));
console.log('copied tsinelas_tsinelas.glb');
// Phaister's lata is the game's KALAWANG starter can, with its own texture.
const models = path.resolve('../../Assets/TumbangPreso/Art/models');
fs.copyFileSync(path.join(models, 'lata_metal.obj'), path.join(out, 'lata_metal.obj'));
fs.copyFileSync(path.join(models, 'textures/lata_metal.png'), path.join(out, 'lata_metal.png'));
console.log('copied lata_metal.obj and its texture');
// The redesigned cast the game shows now, each with its own atlas (Art/CharacterRedesign).
for (const [dir, f] of [['zack', 'zack-redesign.glb'], ['zack', 'zack-redesign-atlas.png'], ['sean', 'sean-redesign.glb'], ['sean', 'sean-redesign-atlas.png'], ['phaister', 'phaister-redesign.glb'], ['phaister', 'phaister-redesign-atlas.png'], ['nemu', 'nemu-redesign.glb'], ['nemu', 'nemu-redesign-atlas.png']]) {
  fs.copyFileSync(path.resolve('../../Assets/TumbangPreso/Art/CharacterRedesign', dir, f), path.join(out, f));
}
// Nemu's companion Kuro is the game's own pet model.
fs.copyFileSync(path.resolve('../../Assets/TumbangPreso/Art/characters/pets/pet-nemu-ghost.glb'), path.join(out, 'pet-nemu-ghost.glb'));
console.log('copied pet-nemu-ghost.glb');
