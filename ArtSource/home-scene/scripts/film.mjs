// One command from source to the game: renders a hero's film and idle, then ships both. See README.md.
// Usage: node scripts/film.mjs <zack|phaister> [--keep]
// Without --keep the old pieces go first: chunks.mjs skips any piece it finds, so they would keep the old frames.
import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';

const hero = process.argv[2];
if (!hero) throw new Error('Usage: node scripts/film.mjs <zack|phaister> [--keep]');
const keep = process.argv.includes('--keep');
const Hero = hero[0].toUpperCase() + hero.slice(1);
fs.mkdirSync('out', { recursive: true });
fs.mkdirSync('scratch', { recursive: true });
const node = (...a) => execFileSync(process.execPath, a, { stdio: 'inherit' });
for (const [id, out] of [[`${Hero}Intro`, `out/${hero}_intro_1080p60.mp4`], [`${Hero}Idle`, `out/${hero}_idle_1080p30.mp4`]]) {
  if (!keep) fs.rmSync(path.join('scratch', `chunks_${id}_1`), { recursive: true, force: true });
  node('scripts/chunks.mjs', id, out, '1', '17', '270', '2', '45');
}
node('scripts/ship-zack.mjs', hero);
