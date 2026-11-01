import { kf, outCubic } from '../lib/kf';
import { clamp01, easeInOut, idleSin, INTRO } from './time';
import { B } from './beats';
import { aim, V3, View } from '../phaister/view';
import { flyingAt, GUARD, phaister, POST, RING } from './perform';

/**
 * The shot list, worked back from `landing`. `in` frames blend from the shot before; 0 is a cut.
 * ⚠️ EVERY SHOT HAS ONE SUBJECT AND IT IS LARGE (owner: "i cant tell wtf is happening"); the lens moves with the action.
 */
type Cam = { p: V3; t: V3; f: number; roll: number };
type Shot = { at: number; in: number; cam: (f: number) => Cam };
const lerp3 = (a: V3, b: V3, t: number): V3 => [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t];
const blend = (a: Cam, b: Cam, t: number): Cam => ({ p: lerp3(a.p, b.p, t), t: lerp3(a.t, b.t, t), f: Math.exp(Math.log(a.f) + (Math.log(b.f) - Math.log(a.f)) * t), roll: a.roll + (b.roll - a.roll) * t });

/** The hold. Phaister in the gap between HOME's tiles and its mode card, Nemu and Kuro by the can past her. */
export const landing = (f: number): Cam => ({
  p: [POST.x + 1.75 + 0.12 * idleSin(f, 1, 0.4), 1.5 + 0.025 * idleSin(f, 2, 1.1), POST.z - 4.55 + 0.08 * idleSin(f, 1, 2.1)],
  t: [POST.x - 1.5 + 0.05 * idleSin(f, 1, 1.4), 0.53 + 0.02 * idleSin(f, 2, 0.2), POST.z + 6.35],
  f: 1500 * (1 + 0.006 * idleSin(f, 1, 0.3)),
  roll: 0.2 * idleSin(f, 1, 0.5),
});

export const focusAt = (f: number) => (f >= B.land ? kf(f, [[B.land, 1], [B.land + 40, 2.2]]) : 1.2);

const u = (f: number, a: number, b: number) => clamp01((f - a) / (b - a));
const ei = easeInOut;
const face = (x: number, z: number, y: number, dist: number, side: number, zoom: number, roll: number, h = 1.1): Cam => ({ p: [x + side, h, z - dist], t: [x, y, z], f: zoom, roll });
const G = GUARD;
const SHOTS: Shot[] = [
  // Down from the moon, over the street, the train going by: this is the place.
  { at: 0, in: 0, cam: (f) => { const q = ei(u(f, 0, B.walk)); return { p: [-7 + 3 * q, 15 - 12.8 * q, -25 + 10 * q], t: [-3 + 3.6 * q, 12 - 10.4 * q, 8 - 8 * q], f: 880, roll: -3 + 3 * q }; } },
  // She walks in with the lens going backward in front of her.
  { at: B.walk, in: 0, cam: (f) => { const z = phaister(f); return { p: [z.x + 1.5, 0.7, z.z - 3.2], t: [z.x + 0.15, 1.3, z.z], f: 1180, roll: 1.6 }; } },
  // Over her shoulder, her arm along the shot, the can and Nemu at the end of it.
  { at: B.dare, in: 0, cam: (f) => ({ p: [POST.x - 1.1, 1.7, POST.z - 2.5], t: [0.5, 0.95, -1.2], f: 1180 * (1 + 0.1 * ei(u(f, B.dare, B.nemuLook))), roll: -1 }) },
  // Nemu and Kuro. Nothing.
  { at: B.nemuLook, in: 0, cam: (f) => face(G.x + 0.3, G.z, 1.45, 3.4, 0.5, 1260 * (1 + 0.05 * ei(u(f, B.nemuLook, B.boast))), -0.6, 1) },
  // To us.
  { at: B.boast, in: 0, cam: (f) => face(POST.x, POST.z, 1.5, 3, 0.8, 1240 * (1 + 0.1 * ei(u(f, B.boast, B.blind))), 1.6, 1) },
  // The brim.
  { at: B.blind, in: 0, cam: (f) => face(POST.x, POST.z, 2, 2.3, 0.3, 1500 * (1 + 0.06 * ei(u(f, B.blind, B.twirl))), -1.4, 1.5) },
  // The can far behind her, the slipper spinning. Low, and it creeps round.
  { at: B.twirl, in: 0, cam: (f) => { const q = ei(u(f, B.twirl, B.flick)); return { p: [POST.x - 2.6 - 0.5 * q, 0.55, POST.z - 2.2 + 0.9 * q], t: [POST.x + 0.25, 1.65, POST.z + 0.6], f: 1120, roll: 3 }; } },
  // ⚠️ The lens rides behind the slipper all the way to the can.
  { at: B.flick, in: 0, cam: (f) => { const s = flyingAt(f)?.p ?? [0, 0.4, 0]; const q = u(f, B.flick, B.klang); return { p: [s[0] - 2.2 + 0.8 * q, s[1] + 0.5 - 0.25 * q, s[2] - 3.2 + 0.6 * q], t: [s[0] * (1 - q), s[1], s[2] + 1 - q], f: 1000 + 200 * q, roll: 4 - 6 * q }; } },
  // Ta-da, with the can still coming down behind her.
  { at: B.tada, in: 0, cam: (f) => face(POST.x + 0.5, POST.z, 1.55, 4, 1.1, 1080 * (1 + 0.07 * outCubic(u(f, B.tada, B.tada + 18))), 1.2, 0.9) },
  { at: B.nemuFlat, in: 0, cam: () => face(G.x + 0.2, G.z, 1.45, 3, 0.4, 1420, 0, 1) },
  { at: B.sag, in: 0, cam: () => face(POST.x, POST.z, 1.4, 3.1, -0.7, 1240, -1.2, 1.05) },
  // The run: the lens runs beside her and tilts into it.
  { at: B.run, in: 4, cam: (f) => { const z = phaister(f); return { p: [z.x + 3.3, 0.85, z.z - 1.6], t: [z.x - 0.1, 1.1, z.z + 0.3], f: 1080, roll: 3.5 }; } },
  // She turns round and Nemu is there.
  { at: B.faceOff - 3, in: 0, cam: (f) => ({ p: [6.8, 1.1, -1.5], t: [3, 1.35, 1.0], f: 1300 * (1 + 0.28 * outCubic(u(f, B.faceOff, B.faceOff + 6))), roll: -2 }) },
  // The chase: the lens ahead of her, low, looking back, Nemu on her heels, the train over all of it.
  { at: B.chase, in: 0, cam: (f) => { const z = phaister(f); return { p: [z.x - 1.7, 0.75, z.z - 4.2], t: [z.x + 0.35, 1.3, z.z + 0.4], f: 1120, roll: -3.5 }; } },
  // The hand.
  { at: B.reach, in: 0, cam: (f) => { const z = phaister(B.reach); return { p: [z.x + 3.2, 1.1, z.z - 1.4 - 0.2 * u(f, B.reach, B.skid)], t: [z.x, 1.3, z.z + 0.5], f: 1340, roll: 2 }; } },
  // Out of the rift on the line, skidding.
  { at: B.skid, in: 0, cam: () => ({ p: [POST.x + 1.2, 0.65, POST.z - 3.4], t: [POST.x - 0.6, 1.1, POST.z], f: 1060, roll: 1.4 }) },
  // Past her, up the lot, and Nemu has not stopped.
  { at: B.nemuComes, in: 0, cam: (f) => ({ p: [POST.x - 2.4, 0.95, POST.z - 2], t: [POST.x + 1.2, 1.25, POST.z + 2.4], f: 1120 * (1 + 0.08 * ei(u(f, B.nemuComes, B.resolve))), roll: -0.8 }) },
  // Enough.
  { at: B.resolve, in: 0, cam: (f) => face(POST.x, POST.z, 1.8, -2.6, 0.4, 1460 * (1 + 0.08 * ei(u(f, B.resolve, B.raise))), 0.8, 1) },
  // Arms up, from the floor in front of her, Nemu's side of the lot, her eyes lit.
  { at: B.raise, in: 0, cam: (f) => ({ p: [POST.x + 0.9, 0.5, POST.z + 3.2], t: [POST.x, 1.9, POST.z], f: 980 * (1 + 0.06 * ei(u(f, B.raise, B.ring))), roll: -2 }) },
  // ⚠️ The ring is the size of the lot, so the lens goes up to see all of it close round Nemu.
  { at: B.ring, in: 24, cam: (f) => { const q = ei(u(f, B.ring + 16, B.slam)); return { p: [-2, 11 + 3 * q, -15.5], t: [0.4, 0.1, -4.4], f: 1080, roll: -2 }; } },
  // She brings both hands down, and it goes off under Nemu. Side on: she goes the whole way.
  { at: B.slam - 8, in: 0, cam: (f) => ({ p: [8.4, 1.3, -4.6 + 1.3 * u(f, B.slam, B.slam + 22)], t: [0.7, 1.9, -3.4 + 1.3 * u(f, B.slam, B.slam + 22)], f: 880, roll: -1.5 }) },
  // Her.
  { at: B.laugh, in: 0, cam: (f) => face(POST.x, POST.z, 1.5, 3, 0.7, 1140 * (1 + 0.32 * outCubic(u(f, B.laugh, B.laugh + 8))), 2, 0.95) },
  // Nemu, across the lot.
  { at: B.nod, in: 0, cam: (f) => face(G.x + 0.3, G.z, 1.45, 3.2, -0.3, 1340 * (1 + 0.05 * ei(u(f, B.nod, B.settle))), 0, 1) },
  // The ring burns out; the lens is already heading for the hold.
  { at: B.settle, in: 0, cam: (f) => { const q = ei(u(f, B.settle, B.home)); return { p: [POST.x + 3.5 - 1.75 * q, 1.5, POST.z - 5.5 + 0.95 * q], t: [POST.x + 1 - 2.5 * q, 1 - 0.47 * q, POST.z + 4 + 2.35 * q], f: 900 + 600 * q, roll: 0 }; } },
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
  const pulse = (at: number, len: number, amp: number) => (f >= at && f < at + len ? (1 - (f - at) / len) * amp : 0);
  const hit = pulse(B.klang, 22, 0.07) + pulse(B.blink, 12, 0.05) + pulse(B.slam, 30, 0.16) + pulse(B.stomp + 2, 6, 0.02);
  // The train's weight in the concrete while it passes over the chase.
  const rumble = f < INTRO ? kf(f, [[B.chase, 0], [B.chase + 8, 0.024], [B.blink, 0.024], [B.blink + 20, 0]]) : 0;
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
void RING;
