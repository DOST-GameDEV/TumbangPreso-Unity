import { kf, outCubic } from '../lib/kf';
import { clamp01, easeInOut, idleSin, INTRO } from './time';
import { B } from './beats';
import { aim, V3, View } from '../phaister/view';
import { NEMU_LINE, POST, STOP, TADA } from './perform';

/** The shot list, worked back from `landing`, as Zack's. `in` frames blend from the shot before; 0 is a cut. */
type Cam = { p: V3; t: V3; f: number; roll: number };
type Shot = { at: number; in: number; cam: (f: number) => Cam };
const lerp3 = (a: V3, b: V3, t: number): V3 => [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t];
const blend = (a: Cam, b: Cam, t: number): Cam => ({ p: lerp3(a.p, b.p, t), t: lerp3(a.t, b.t, t), f: Math.exp(Math.log(a.f) + (Math.log(b.f) - Math.log(a.f)) * t), roll: a.roll + (b.roll - a.roll) * t });

/** The hold. Phaister in the gap between HOME's tiles and its mode card, Nemu and Kuro over her shoulder. */
export const landing = (f: number): Cam => ({
  p: [POST.x + 1.75 + 0.12 * idleSin(f, 1, 0.4), 1.5 + 0.025 * idleSin(f, 2, 1.1), POST.z - 4.55 + 0.08 * idleSin(f, 1, 2.1)],
  t: [POST.x - 1.5 + 0.05 * idleSin(f, 1, 1.4), 0.53 + 0.02 * idleSin(f, 2, 0.2), POST.z + 6.35],
  f: 1500 * (1 + 0.006 * idleSin(f, 1, 0.3)),
  roll: 0.2 * idleSin(f, 1, 0.5),
});

export const focusAt = (f: number) => (f >= B.land ? kf(f, [[B.land, 1], [B.land + 40, 2.2]]) : f >= B.doll && f < B.whole ? 3 : 1.4);

const u = (f: number, a: number, b: number) => clamp01((f - a) / (b - a));
const N = NEMU_LINE;
const SHOTS: Shot[] = [
  // Under the deck, low, sliding along the piers with the far lamp and the lot through them.
  { at: B.under, in: 0, cam: (f) => ({ p: [12.8, 0.9, -24 + 2.6 * easeInOut(u(f, B.under, B.doll))], t: [-2, 2.2, 6], f: 900, roll: -1.5 }) },
  // The doll, slumped against the can.
  { at: B.doll, in: 0, cam: (f) => ({ p: [1.55, 0.36, -1.75 + 0.2 * easeInOut(u(f, B.doll, B.brim))], t: [0.28, 0.42, 0], f: 1500, roll: 1.4 }) },
  // A hat brim, from low.
  { at: B.brim, in: 0, cam: (f) => ({ p: [POST.x + 0.45, 1.2, POST.z - 1.75], t: [POST.x, 1.95, POST.z], f: 1500 * (1 + 0.05 * easeInOut(u(f, B.brim, B.whole))), roll: -1.5 }) },
  // Whole, with everything she has her back to behind her.
  { at: B.whole, in: 0, cam: (f) => ({ p: [POST.x + 1.2, 1.15, POST.z - 4.9 - 0.3 * u(f, B.whole, B.nemu)], t: [POST.x - 0.3, 1.5, POST.z + 3], f: 1060, roll: 0.6 }) },
  // Nemu. Kuro. Nothing on her face and there never will be.
  { at: B.nemu, in: 0, cam: (f) => ({ p: [N.x - 0.75, 1.0, N.z - 3.6], t: [N.x + 0.28, 1.35, N.z], f: 1300 * (1 + 0.08 * easeInOut(u(f, B.nemu, B.throw))), roll: -0.8 }) },
  // Side on and wide: the line, the can, Phaister's back to the lot. The shot is short and straight.
  { at: B.throw - 16, in: 0, cam: () => ({ p: [8.6, 1.0, 2.8], t: [0, 1.0, 2.8], f: 900, roll: 0.8 }) },
  // Low in front of her, the can going up over her shoulder. She does not turn.
  { at: B.klang + STOP, in: 0, cam: (f) => ({ p: [POST.x + 0.65, 0.7, POST.z - 3.1], t: [POST.x + 0.1, 1.28, POST.z], f: 1150 * (1 + 0.07 * easeInOut(u(f, B.hold, B.fetch))), roll: 1.5 }) },
  // Nemu's long walk, the lens beside her.
  { at: B.fetch, in: 0, cam: (f) => { const q = u(f, B.fetch, B.wake); return { p: [7.2, 1.2, 7.2 - 2.4 * q], t: [1.6, 1.1, 6.6 - 2.4 * q], f: 1050, roll: -0.6 }; } },
  // The doll. The light comes up from inside. It stands and goes to work.
  { at: B.wake, in: 0, cam: (f) => { const q = easeInOut(u(f, B.stand + 10, B.push)); return { p: [1.7 - 2.2 * q, 0.5 + 0.2 * q, -1.95 + 0.9 * q], t: [0.45 - 1.45 * q, 0.42, 1.4 * q], f: 1380 * (1 - 0.15 * q), roll: -1.2 }; } },
  // From the south west: Phaister to one side, her hand out behind her, Nemu across the lot on the other.
  { at: B.reach, in: 0, cam: (f) => ({ p: [POST.x - 3, 1.1, POST.z - 3], t: [0, 1.35, -0.2], f: 1120 * (1 + 0.08 * easeInOut(u(f, B.reach, B.train))), roll: 0.8 }) },
  // The whole lot under the deck, the train coming in over it all.
  { at: B.train, in: 0, cam: (f) => ({ p: [-4.4, 0.8, -7.6 - 0.4 * u(f, B.train, B.rise)], t: [3.4, 3.2, 3.2], f: 820, roll: -1.2 }) },
  // Nemu comes up from the slipper. And.
  { at: B.rise, in: 0, cam: (f) => ({ p: [TADA.x + 4.6, 1.05, TADA.z - 1.3], t: [TADA.x + 0.3, 1.3, TADA.z + 0.5], f: 1200 * (1 + 0.05 * easeInOut(u(f, B.rise, B.sag))) * (1 + 0.1 * outCubic(u(f, B.nod, B.nod + 14))), roll: 0.8 }) },
  // One whip home, landing exactly on the hold.
  { at: B.home - 1, in: 10, cam: (f) => landing(f) },
];

const camOf = (f: number, n: number): Cam => {
  const sh = SHOTS[n];
  const c = sh.cam(f);
  if (n === 0 || sh.in <= 0) return c;
  const t = (f - sh.at) / sh.in;
  if (t >= 1) return c;
  return blend(camOf(f, n - 1), c, outCubic(clamp01(t)));
};
const cam = (f: number): Cam => {
  if (f >= INTRO) return landing(f);
  let n = 0;
  for (let i = 0; i < SHOTS.length; i++) if (f >= SHOTS[i].at) n = i;
  return camOf(f, n);
};

export const camAt = (f: number): View => {
  const c = cam(f);
  const a = aim(c.p, c.t);
  const hit = f >= B.klang && f < B.klang + 22 ? (1 - (f - B.klang) / 22) * 0.07 : 0;
  // The train's weight in the concrete: a low rumble while it passes.
  const rumble = f < INTRO ? kf(f, [[B.train + 30, 0], [B.train + 60, 0.022], [B.rise, 0.022], [B.rise + 20, 0]]) : 0;
  const sx = hit * Math.sin(f * 1.7) + rumble * Math.sin(f * 2.9);
  const sy = hit * Math.sin(f * 2.3 + 1) + rumble * Math.sin(f * 3.7 + 1);
  return { x: c.p[0] + sx, y: c.p[1] + sy, z: c.p[2], yaw: a.yaw, pitch: a.pitch, roll: c.roll, f: c.f, cx: 960, cy: 540 };
};

export const panSpeed = (f: number) => {
  const a = camAt(f);
  const b = camAt(f - 1);
  const cut = SHOTS.some((s) => s.in === 0 && f >= s.at && f - 1 < s.at);
  return cut ? { x: 0, y: 0 } : { x: Math.abs(a.yaw - b.yaw) * (a.f / 57.3), y: Math.abs(a.pitch - b.pitch) * (a.f / 57.3) };
};
