import { PoseFilm } from '../three/PoseDump';
import { B } from './beats';
import { camAt } from './camera';
import { canAt, flyingAt, rago, zack } from './perform';
import { IDLE, INTRO } from './time';

/** What the plates and the poses need to know about "Green Light" (scripts/plates.mjs, three/PoseDump.tsx). */
export const POSES: PoseFilm = {
  intro: INTRO,
  idle: IDLE,
  first: { model: 'zack-redesign', frame: zack },
  second: { model: 'sean-redesign', frame: rago },
  flyingAt,
  canAt,
  flyScale: (f, fl) => ((fl as { trail?: boolean }).trail && f < B.closeOn ? 1.9 : 1),
};

const pt = (x: number, y: number, z: number) => ({ x, y, z });

export const PLATES = {
  hero: 'zack',
  poses: 'ZackPoses',
  map: 'Kanto',
  /** The court's surface in Kanto (kanto_layout.json, gameplay.walk). */
  floor: 0.12,
  cast: [
    { id: 'zack', slipper: true, atlas: 'Assets/TumbangPreso/Art/CharacterRedesign/zack/zack-redesign-atlas.png', eyeLow: 0.441, eyeHigh: 0.474, glow: { r: 255, g: 250, b: 150, a: 255 } },
    { id: 'sean', slipper: false },
  ],
  kuro: false,
  INTRO,
  IDLE,
  camAt,
  /** What must stay clear of the map, on this frame. */
  subjectsAt: (f: number) => {
    const z = zack(f);
    const r = rago(f);
    const c = canAt(f);
    return [pt(z.x, 0.2, z.z), pt(z.x, 1.1, z.z), pt(z.x, 2, z.z), pt(r.x, 0.2, r.z), pt(r.x, 1.1, r.z), pt(r.x, 2.2, r.z), pt(c.x, 0.3, c.z)];
  },
  /** The street slows with the climax: cars and all. */
  rate: (f: number) => (f >= B.snapBack && f < B.yank ? 0.12 : 1),
  /** The film's clock is 30 a second, and its green opens at B.green. */
  green: B.green / 30,
  train: -1,
};
