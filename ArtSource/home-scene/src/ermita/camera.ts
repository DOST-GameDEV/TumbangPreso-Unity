import { outCubic } from '../lib/kf';
import { clamp01, easeInOut, idleSin, INTRO } from './time';
import { B } from './beats';
import { aim, V3, View } from '../phaister/view';
import { ARRIVE, FLAT, GUARD, kuroAt, nemu, phaister, POST, STAND } from './perform';

/**
 * The shot list, worked back from `landing`. `in` frames blend from the shot before; 0 is a cut.
 * ⚠️ EVERY SHOT HAS ONE LARGE SUBJECT AND IT IS ALWAYS OBVIOUS WHAT JUST HAPPENED (owner: "i cant tell wtf is happening"). Her face is
 * in front of the lens: she is shot from the front or three-quarters, chin level, so the hat never hides it.
 * East is the viaduct: the LRT and Taft's traffic. Whatever needs the map's moving things looks that way.
 */
type Cam = { p: V3; t: V3; f: number; roll: number };
type Shot = { at: number; in: number; cam: (f: number) => Cam };
const lerp3 = (a: V3, b: V3, t: number): V3 => [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t];
const blend = (a: Cam, b: Cam, t: number): Cam => ({ p: lerp3(a.p, b.p, t), t: lerp3(a.t, b.t, t), f: Math.exp(Math.log(a.f) + (Math.log(b.f) - Math.log(a.f)) * t), roll: a.roll + (b.roll - a.roll) * t });

const u = (f: number, a: number, b: number) => clamp01((f - a) / (b - a));
const ei = easeInOut;
const deg = Math.PI / 180;
/** A lens `d` metres from a spot on bearing `b`: 0 is south of it, which is in front of someone at yaw 0, +90 is east. */
const round = (x: number, z: number, b: number, d: number, h: number): V3 => [x + Math.sin(b * deg) * d, h, z - Math.cos(b * deg) * d];

/**
 * ⚠️ THE HOLD FACES HER (owner's rule from Zack's sketch). She is in the gap between HOME's tiles and its mode card, looking at us;
 * past her right shoulder Nemu is back at her can with Kuro, moving, never hidden behind her, with the viaduct and the street beyond.
 */
export const landing = (f: number): Cam => ({
  p: [POST.x - 1.1 + 0.04 * idleSin(f, 1, 0.4), 1.35 + 0.02 * idleSin(f, 2, 1.1), POST.z - 4.1 + 0.05 * idleSin(f, 1, 2.1)],
  t: [POST.x + 0.42 + 0.015 * idleSin(f, 1, 1.4), 1.2 + 0.012 * idleSin(f, 2, 0.2), POST.z],
  f: 1500 * (1 + 0.006 * idleSin(f, 1, 0.3)),
  roll: 0.15 * idleSin(f, 1, 0.5),
});

/** The settle: side on from the fence, the two of them apart in frame with the street behind. */
const settle = (f: number): Cam => ({
  p: [-8.9, 1.25, -8.3],
  t: [-3.2, 1.05, -5.1],
  f: 1230,
  roll: 0,
});

const SHOTS: Shot[] = [
  // The can on the chalk, the street behind it, and a small ghost coming down to look.
  { at: B.lata, in: 0, cam: (f) => { const q = u(f, B.lata, B.nemu); return { p: [0.1 + 0.25 * q, 0.42, -2.3 + 0.2 * ei(q)], t: [0.1, 0.46, 0], f: 1250, roll: 1.2 }; } },
  // The taya. Low and looking up. She is doing nothing, and she will go on doing it.
  { at: B.nemu, in: 0, cam: (f) => { const q = ei(u(f, B.nemu, B.twirl)); return { p: [GUARD.x + 0.45, 0.72, GUARD.z - 3.9 + 0.55 * q], t: [GUARD.x + 0.2, 1.22, GUARD.z], f: 1180, roll: -1 }; } },
  // A slipper going round a finger; the lens climbs her arm to the brim and the grin.
  { at: B.twirl, in: 0, cam: (f) => { const q = ei(u(f, B.twirl + 24, B.walk - 8)); const z = phaister(f); return { p: [z.x + 0.75, 1.3, z.z - 2.15], t: [z.x + 0.1, 1.05 + 0.42 * q, z.z], f: 1300 - 120 * q, roll: 2 - 3 * q }; } },
  // She walks on with the lens going backward in front of her, Taft's traffic far behind the lens side.
  { at: B.walk, in: 0, cam: (f) => { const z = phaister(f); return { p: [z.x + 3.5, 0.9, z.z - 1.25], t: [z.x, 1.18, z.z], f: 1050, roll: 1.2 }; } },
  // She points. Three-quarters, so her face and the arm both read.
  { at: B.dare, in: 0, cam: (f) => { const q = ei(u(f, B.dare, B.nothing)); return { p: round(POST.x, POST.z, 150, 3.2, 1.05), t: [POST.x + 0.3, 1.3, POST.z], f: 1050 + 130 * q, roll: -1 }; } },
  // Nothing. Kuro waves.
  { at: B.nothing, in: 0, cam: (f) => ({ p: [GUARD.x + 0.45, 1.15, GUARD.z - 3.2], t: [GUARD.x + 0.3, 1.45, GUARD.z], f: 1360 * (1 + 0.04 * u(f, B.nothing, B.noLook)), roll: 0 }) },
  // Her back to the can: the hold's own lens, nearer. The can is over her shoulder, and she is not looking at it.
  { at: B.noLook, in: 0, cam: (f) => { const q = ei(u(f, B.noLook, B.flick)); return { p: [POST.x - 1.0, 1.0, POST.z - 3.3 + 0.35 * q], t: [POST.x + 0.3, 1.25, POST.z], f: 1120 + 130 * q, roll: 1.4 }; } },
  // Low behind the can, looking down the throw: it comes at us, from a hand that never looked.
  { at: B.flick, in: 0, cam: (f) => { const q = u(f, B.flick, B.klang); return { p: [-1.5, 0.46, 2.1 - 0.12 * q], t: [-1.0, 0.62, -1], f: 1000, roll: -2 }; } },
  // It goes past Nemu's head. She has not moved. Kuro has.
  { at: B.reactN, in: 0, cam: (f) => { const q = ei(u(f, B.reactN, B.tada)); return { p: [GUARD.x - 0.3, 0.95, GUARD.z - 4.9 + 0.5 * q], t: [GUARD.x + 0.45, 1.4, GUARD.z], f: 1000, roll: 1 }; } },
  // Ta-da.
  { at: B.tada, in: 0, cam: (f) => { const q = outCubic(u(f, B.tada, B.tada + 14)); return { p: [POST.x - 0.55, 0.95, POST.z - 3.4], t: [POST.x, 1.3, POST.z], f: 1050 * (1 + 0.14 * q), roll: 1.5 }; } },
  // She runs, the lens running ahead and beside her. The lot goes by; the viaduct comes up behind her.
  { at: B.run, in: 4, cam: (f) => { const z = phaister(f); return { p: [z.x + 3.3, 0.85, z.z + 0.9], t: [z.x + 0.2, 1.1, z.z], f: 1000, roll: 2.6 }; } },
  // Nemu's hand on the can, the whole of it: the walk, the stoop, the hand, up and running.
  { at: B.rights - 4, in: 0, cam: (f) => { const q = ei(u(f, B.rights, B.rights + 44)); return { p: [4.6 - 0.2 * q, 0.62, -2.1], t: [1.9, 0.72 + 0.4 * q, -0.65], f: 1300, roll: -1.2 }; } },
  // The slide, low, the slipper on the floor, her hand taking it: and Nemu already coming over her shoulder.
  { at: B.scoop - 22, in: 0, cam: () => ({ p: [8.6, 0.55, -0.5], t: [4.5, 0.75, 2.5], f: 1050, roll: 1.2 }) },
  // The chase: the lens running backward ahead of her, low, Nemu right behind.
  {
    at: B.chase + 6, in: 8, cam: (f) => {
      const z = phaister(f);
      const n = nemu(f);
      const dx = z.x - n.x;
      const dz = z.z - n.z;
      const d = Math.hypot(dx, dz) || 1;
      return { p: [z.x + (dx / d) * 3.5 + (dz / d) * 1.1, 0.82, z.z + (dz / d) * 3.5 - (dx / d) * 1.1], t: [(z.x * 2 + n.x) / 3, 1.2, (z.z * 2 + n.z) / 3], f: 1000, roll: -2.4 };
    },
  },
  // The hand, the hat, and then moths. It closes on them.
  { at: B.reach, in: 0, cam: (f) => { const q = ei(u(f, B.reach, B.blink)); return { p: [0.9, 0.95, -10.6], t: [0.75, 1.35, -6.5], f: 1250 * (1 + 0.1 * q), roll: 1.5 }; } },
  // They knit her back, the far side of the lot, and she is very pleased. Nemu is small behind her and already running.
  { at: B.arrive - 4, in: 0, cam: (f) => { const q = ei(u(f, B.arrive, B.notice)); return { p: [ARRIVE.x - 3.1, 0.92, ARRIVE.z - 1.4], t: [ARRIVE.x + 0.5, 1.15, ARRIVE.z - 0.2], f: 1100 + 120 * q, roll: -1 }; } },
  // She runs for the line, right at us. Nemu comes into the frame from the side, faster.
  { at: B.bolt, in: 0, cam: (f) => { const z = phaister(f); return { p: [z.x - 2.1, 0.8, -8.9], t: [z.x + 0.7, 1.2, z.z - 0.9], f: 950, roll: 2.4 }; } },
  // ⚠️ SIDE ON, THE VIADUCT BEHIND THEM. This one lens is the crux: she stops running and turns, the hand comes, the train comes.
  { at: B.cutOff, in: 0, cam: (f) => { const q = ei(u(f, B.cutOff, B.eyes)); return { p: [-10.2 + 0.7 * q, 1.5, -5.1], t: [-4.6, 1.8, -5.1], f: 1040 + 80 * q, roll: 0.6 }; } },
  // Her face, from where Nemu's hand is. The eyes catch.
  { at: B.eyes, in: 0, cam: (f) => { const q = u(f, B.eyes, B.nemuFace); return { p: [STAND.x - 1.25, 1.2, STAND.z - 1.5], t: [STAND.x + 0.05, 1.42, STAND.z], f: 1050 * (1 + 0.13 * q), roll: 1.6 }; } },
  // Nemu's, from Phaister's palm, and the ghost coming out toward us.
  { at: B.nemuFace, in: 0, cam: (f) => { const q = u(f, B.nemuFace, B.kuroFace); const n = nemu(f); return { p: [n.x - 2.3, 1.15, n.z + 0.35], t: [n.x + 0.05, 1.3, n.z + 0.25], f: 1180 * (1 + 0.1 * q), roll: -1.6 }; } },
  // Kuro. The only one who is frightened.
  { at: B.kuroFace, in: 0, cam: (f) => { const q = u(f, B.kuroFace, B.full); const k = kuroAt(f); return { p: [k[0] + 0.75, k[1] - 0.1, k[2] + 1.05], t: [k[0], k[1] - 0.3, k[2]], f: 1200 * (1 + 0.12 * q), roll: 2 }; } },
  // The whole of it, wider: the two of them, the ghost at full stretch, the LRT crawling over.
  { at: B.full, in: 0, cam: (f) => { const q = u(f, B.full, B.gulp); return { p: [-10.3, 1.5, -5.1], t: [-4.6, 1.95, -5.1], f: 1120 * (1 + 0.03 * q), roll: 0.4 }; } },
  // ⚠️ THE GULP: a crash zoom on to her palm.
  { at: B.gulp, in: 0, cam: (f) => { const q = outCubic(u(f, B.gulp, B.gulp + 7)); return { p: [-8.4, 1.1, -4.7], t: [-4.75, 1.42, -4.55], f: 1000 * (1 + 0.85 * q), roll: 2.4 * q }; } },
  // Real time, and the hand is still coming. Close, at the height of the brim. It gets an inch away.
  { at: B.hand, in: 0, cam: (f) => { const q = ei(u(f, B.hand, B.drained)); return { p: [-7.1, 1.4, -5.05], t: [-4.7, 1.32, -4.85], f: 1000 + 280 * q, roll: -1 }; } },
  // And she drops, the lens going down with her.
  { at: B.drained, in: 0, cam: (f) => { const q = ei(u(f, B.drained, B.drained + 12)); return { p: [-7.4, 1.25 - 0.5 * q, -5.2], t: [-4.6, 1.3 - 0.75 * q, -4.9], f: 1120 - 80 * q, roll: 1.4 * q }; } },
  // The best thing that has ever happened to her. Nemu flat in the foreground of it.
  { at: B.joy, in: 0, cam: (f) => { const q = outCubic(u(f, B.joy, B.joy + 10)); return { p: [FLAT.x - 1.5, 0.52, FLAT.z - 2.5], t: [STAND.x + 0.1, 1.35, STAND.z], f: 1000 * (1 + 0.1 * q), roll: -2 }; } },
  // From here there are no cuts. She strolls home and Nemu gets up behind her.
  { at: B.home - 16, in: 20, cam: (f) => settle(f) },
  // Then the lens goes round to face her.
  {
    at: B.round, in: 0, cam: (f) => {
      const q = ei(u(f, B.round, B.settled - 45));
      const a = settle(f);
      const b = landing(f);
      return { ...blend(a, b, q), t: lerp3(a.t, b.t, clamp01(q * 1.6)) };
    },
  },
];

const camOf = (f: number, n: number): Cam => {
  const sh = SHOTS[n];
  const c = sh.cam(f);
  if (n === 0 || sh.in <= 0) return c;
  const t = (f - sh.at) / sh.in;
  if (t >= 1) return c;
  return blend(camOf(f, n - 1), c, easeInOut(clamp01(t)));
};
const cam = (f: number): Cam => {
  if (f >= B.settled - 45) return landing(f);
  let n = 0;
  for (let i = 0; i < SHOTS.length; i++) if (f >= SHOTS[i].at) n = i;
  return camOf(f, n);
};

export const camAt = (f: number): View => {
  const c = cam(f >= INTRO ? f : f);
  const a = aim(c.p, c.t);
  const pulse = (at: number, len: number, amp: number) => (f >= at && f < at + len ? (1 - (f - at) / len) * amp : 0);
  const hit = pulse(B.klang, 22, 0.07) + pulse(B.blink, 10, 0.03) + pulse(B.gulp, 26, 0.11) + pulse(B.drained + 6, 14, 0.05);
  const sx = hit * Math.sin(f * 1.7);
  const sy = hit * Math.sin(f * 2.3 + 1);
  return { x: c.p[0] + sx, y: c.p[1] + sy, z: c.p[2], yaw: a.yaw, pitch: a.pitch, roll: c.roll, f: c.f, cx: 960, cy: 540 };
};

export const panSpeed = (f: number) => {
  if (f >= B.land) return { x: 0, y: 0 };
  const a = camAt(f);
  const b = camAt(f - 1);
  const cut = SHOTS.some((s) => s.in === 0 && f >= s.at && f - 1 < s.at);
  return cut ? { x: 0, y: 0 } : { x: Math.abs(a.yaw - b.yaw) * (a.f / 57.3), y: Math.abs(a.pitch - b.pitch) * (a.f / 57.3) };
};

