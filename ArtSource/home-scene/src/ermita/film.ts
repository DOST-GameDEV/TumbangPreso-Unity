import { PoseFilm } from '../three/PoseDump';
import { camAt } from './camera';
import { canAt, flyingAt, kuroRow, mothRows, nemu, phaister, streetRate } from './perform';
import { B } from './beats';
import { IDLE, INTRO } from './time';

/** What the plates and the poses need to know about her film (scripts/plates.mjs, three/PoseDump.tsx). */
export const POSES: PoseFilm = {
  intro: INTRO,
  idle: IDLE,
  // While she is moths she is nowhere: a row has no way to hide a body, so it stands her far off the map.
  first: { model: 'phaister-redesign', frame: (f) => { const p = phaister(f); return p.visible > 0 ? p : { ...p, x: 1000, z: 1000 }; } },
  second: { model: 'nemu-redesign', frame: nemu },
  flyingAt,
  canAt,
  kuro: kuroRow,
  moths: mothRows,
};

const pt = (x: number, y: number, z: number) => ({ x, y, z });

export const PLATES = {
  hero: 'phaister',
  poses: 'PhaisterPoses',
  map: 'IlalimNgTulay',
  /** The lot's surface: the map's origin sits on it (ilalim_layout.json, originY). Measured in the batch log. */
  floor: 0,
  cast: [
    { id: 'phaister', slipper: true, atlas: 'Assets/TumbangPreso/Art/CharacterRedesign/phaister/phaister-redesign-atlas.png', eyeLow: 0.44, eyeHigh: 0.475, lashTop: 0.34, lashOuter: 0.24, glow: { r: 255, g: 226, b: 246, a: 255 } },
    { id: 'nemu', slipper: false },
  ],
  kuro: true,
  INTRO,
  IDLE,
  camAt,
  subjectsAt: (f: number) => {
    const a = phaister(f);
    const b = nemu(f);
    const c = canAt(f);
    return [pt(a.x, 0.2, a.z), pt(a.x, 1.1, a.z), pt(a.x, 2.2, a.z), pt(b.x, 0.2, b.z), pt(b.x, 1.1, b.z), pt(b.x, 2.2, b.z), pt(c.x, 0.3, c.z)];
  },
  rate: streetRate,
  green: -1,
  /** Street seconds after the first plate at which the LRT is level with the court. */
  train: B.trainLevel,
  /** The near track: from the lot, the parapet hides the far one. */
  trainDir: -1,
};
