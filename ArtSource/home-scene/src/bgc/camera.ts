import { outCubic } from '../lib/kf';
import { clamp01, easeInOut, idleSin, INTRO } from './time';
import { B } from './beats';
import { aim, V3, View } from '../phaister/view';
import { LINE, POST, rago, STAND, zack } from './perform';

/**
 * ⚠️ THE SHOT LIST (docs/reports/home-scene/research-2026-11-01.md, rules 2, 3, 6). Everything is worked
 * BACK from `landing`, the frame the player lives with. A shot blends from wherever the last one is at
 * that moment over `in` frames (0 a cut, 4 to 7 a whip, 20+ a move), so nothing restarts at a join.
 *
 * After `B.land` there are no cuts at all, and past INTRO the camera is only `landing`, periodic on IDLE.
 */
type Cam = { p: V3; t: V3; f: number; roll: number };
type Shot = { at: number; in: number; cam: (f: number) => Cam };

const lerp3 = (a: V3, b: V3, t: number): V3 => [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t];
const blend = (a: Cam, b: Cam, t: number): Cam => ({
  p: lerp3(a.p, b.p, t),
  t: lerp3(a.t, b.t, t),
  f: Math.exp(Math.log(a.f) + (Math.log(b.f) - Math.log(a.f)) * t),
  roll: a.roll + (b.roll - a.roll) * t,
});

/** The settle: Zack on the line, Rago over his shoulder, getting up and keeping the score. */
const settle = (f: number): Cam => ({
  p: [1.2 + 0.12 * idleSin(f, 1, 0.4), 1.42 + 0.025 * idleSin(f, 2, 1.1), -12.9 + 0.08 * idleSin(f, 1, 2.1)],
  t: [-2.05 + 0.05 * idleSin(f, 1, 1.4), 0.486 + 0.02 * idleSin(f, 2, 0.2), -2.0],
  f: 1500 * (1 + 0.006 * idleSin(f, 1, 0.3)),
  roll: 0.2 * idleSin(f, 1, 0.5),
});

/**
 * ⚠️ THE HOLD IS HIS ALONE (owner, 2026-11-01: "make him turn tapos focus ung camera to him tapos di na kita si rago"). The lens
 * stands on the line from Rago through Zack, so Rago is behind him, and the drift is too small to bring him out.
 */
export const landing = (f: number): Cam => ({
  p: [LINE.x - 0.39 + 0.04 * idleSin(f, 1, 0.4), 1.35 + 0.02 * idleSin(f, 2, 1.1), LINE.z - 4.5 + 0.05 * idleSin(f, 1, 2.1)],
  t: [LINE.x + 0.16 + 0.015 * idleSin(f, 1, 1.4), 1.0 + 0.012 * idleSin(f, 2, 0.2), LINE.z],
  f: 1700 * (1 + 0.006 * idleSin(f, 1, 0.3)),
  roll: 0.15 * idleSin(f, 1, 0.5),
});

const SHOTS: Shot[] = [
  // The map's real signal, red, from under it. A slow tilt onto the lamp.
  {
    at: B.signal, in: 0, cam: (f) => {
      const u = clamp01((f - B.signal) / (B.feet - B.signal));
      return { p: [15.1 - 1.0 * u, 1.3, 11.3], t: [16.9 + 0.4 * easeInOut(u), 3.7 - 0.2 * easeInOut(u), 16.6], f: 1180, roll: -2 + 1.6 * u };
    },
  },
  // The foot on the chalk. Low, level with the line.
  {
    at: B.feet, in: 0, cam: (f) => {
      const u = clamp01((f - B.feet) / (B.rago - B.feet));
      return { p: [LINE.x + 1.25 + 0.3 * u, 0.62, LINE.z - 1.3], t: [LINE.x - 0.05, 0.34, LINE.z + 0.15], f: 1750, roll: 1.4 };
    },
  },
  // The wall. Low and close enough to look up at; a very slow push.
  {
    at: B.rago, in: 0, cam: (f) => {
      const u = clamp01((f - B.rago) / (B.whole - B.rago));
      return { p: [POST.x + 0.9, 0.95, POST.z - 4.8 + 0.5 * easeInOut(u)], t: [POST.x + 0.05, 1.5, POST.z], f: 1120, roll: -1 };
    },
  },
  // Zack, whole, for the first time. He looks away from the can.
  {
    at: B.whole, in: 0, cam: (f) => {
      const u = clamp01((f - B.whole) / (B.wide - B.whole));
      return { p: [LINE.x - 0.1, 1.15, LINE.z - 4.2 - 0.4 * u], t: [LINE.x + 0.75, 1.72, LINE.z + 3], f: 1000, roll: 0.8 };
    },
  },
  // The signal again, the same lens as the opening, closer: it changes.
  {
    at: B.amber, in: 0, cam: (f) => {
      const u = clamp01((f - B.amber) / (B.eyes - B.amber));
      return { p: [14.8 - 0.2 * u, 2.0, 12.2], t: [17.0, 3.15, 16.6], f: 1300 * (1 + 0.1 * easeInOut(u)), roll: -1 };
    },
  },
  // His face, and what the green does to it. Held.
  {
    at: B.eyes, in: 0, cam: (f) => {
      const u = clamp01((f - B.eyes) / (B.wide - B.eyes));
      return { p: [LINE.x + 0.5, 1.25, LINE.z - 2.2], t: [LINE.x + 0.08, 1.38, LINE.z], f: 1200 * (1 + 0.14 * easeInOut(u)), roll: 1.2 };
    },
  },
  // ONE WIDE FRAME holds the throw, the lamp, the ricochet and the can behind Rago. No whip after the
  // slipper (method § 7: an unreadable blur for a whole second).
  {
    at: B.wide, in: 8, cam: (f) => {
      const u = clamp01((f - B.wide) / (B.run - B.wide));
      return { p: [2.75 - 0.15 * u, 1.6, -12.6 - 0.25 * u], t: [-2.6, 2.05, -2.0 ], f: 980, roll: -0.4 };
    },
  },
  // ⚠️ THE CURVE ARCS IN DEPTH, SO FROM EYE LEVEL IT IS A FLAT LINE. The lens cranes up for the throw so
  // the whole C reads on the court: Zack at the bottom, Rago square in the way, the can behind him.
  {
    at: B.wide + 22, in: 22, cam: (f) => {
      const u = clamp01((f - B.wide - 22) / (B.closeOn - B.wide - 22));
      return { p: [-3.4 + 0.5 * u, 13.2 + 0.5 * u, -17.5], t: [1.7, 0.2, -3.2], f: 1250, roll: -1.5 };
    },
  },
  // Low behind the can, Rago's back filling the frame beyond it: nobody is watching this can.
  {
    at: B.closeOn, in: 0, cam: (f) => {
      const u = clamp01((f - B.closeOn) / (B.deadpan - B.closeOn));
      return { p: [1.5, 0.42, 4.3 - 0.25 * u], t: [0.1, 0.72, -0.3], f: 1300, roll: 2 };
    },
  },
  // His front. He has not moved. The head comes round, and when he sees it the lens crashes in.
  {
    at: B.deadpan, in: 0, cam: (f) => {
      const u = clamp01((f - B.deadpan) / (B.jolt - B.deadpan));
      const crash = clamp01((f - B.jolt) / 5);
      return { p: [POST.x + 1.5, 1.0, POST.z - 3.9], t: [POST.x + 0.3, 1.2 + 0.2 * crash, POST.z + 0.3], f: 1050 * (1 + 0.06 * easeInOut(u)) * (1 + 0.5 * outCubic(crash)), roll: -0.8 + 3 * outCubic(crash) };
    },
  },
  // Zack, low, the hero of his own story, with Rago small and appalled behind him.
  {
    at: B.stare, in: 0, cam: (f) => {
      const u = clamp01((f - B.stare) / (B.run - B.stare));
      return { p: [LINE.x + 1.6, 0.85, LINE.z - 3.1], t: [LINE.x + 0.4, 1.2, LINE.z], f: 1120 * (1 + 0.08 * easeInOut(u)), roll: 2 };
    },
  },
  // The run: the lens runs beside him on the far side from the can, so Rago stays in frame, turning with him.
  {
    at: B.run, in: 6, cam: (f) => {
      const z = zack(f);
      return { p: [z.x + 3.6, 1.3, z.z - 0.6], t: [z.x - 0.7, 1.05, z.z], f: 980, roll: 1.6 };
    },
  },
  // Rago at the can, for the whole of it: he gets there, rights it and comes up running.
  {
    at: B.reset - 14, in: 0, cam: (f) => {
      const u = clamp01((f - B.reset + 14) / 40);
      return { p: [-2.6 + 0.4 * u, 0.85, -3.6], t: [0.8, 0.62, -0.7], f: 1250, roll: -1.2 };
    },
  },
  // Back to Zack as he slides in on the slipper, with Rago already coming.
  {
    at: B.reset + 26, in: 0, cam: (f) => {
      const z = zack(f);
      return { p: [z.x + 3.2, 1.2, z.z - 2.6], t: [z.x - 0.7, 0.85, z.z + 0.2], f: 980, roll: 1.2 };
    },
  },
  // The chase: the lens keeps the two of them in frame, the can somewhere between.
  {
    at: B.scoop + 12, in: 14, cam: (f) => {
      const z = zack(f);
      const r = rago(f);
      const mx = (z.x + r.x) / 2;
      const mz = (z.z + r.z) / 2;
      return { p: [mx + 4.0, 1.4, mz - 3.4], t: [mx, 0.95, mz], f: 1150, roll: 1 };
    },
  },
  // He runs at the lens and looks into it, Rago's reach right behind him. The push in is the only move.
  {
    at: B.look, in: 0, cam: (f) => {
      const z = zack(f);
      const u = clamp01((f - B.look) / (B.wall - B.look));
      return { p: [z.x + 1.9 - 0.5 * u, 1.2, z.z - 2.3 + 0.6 * u], t: [z.x, 1.28, z.z], f: 1050 + 300 * easeInOut(u), roll: -1.5 };
    },
  },
  // The tag, side on and low, the can between them. It pushes all the way in to the lunge, then follows
  // Rago down.
  {
    at: B.wall, in: 0, cam: (f) => {
      const u = 1;
      const go = clamp01((f - B.commit) / (B.snapBack - B.commit));
      const down = clamp01((f - B.trip) / (B.sprawl - B.trip));
      return {
        p: [6.5 - 0.5 * easeInOut(u) - 0.4 * go, 0.7 - 0.2 * go, 0.05],
        t: [0.65, 0.85 - 0.15 * down, 0.35 + 0.6 * easeInOut(down)],
        f: 1280 * (1 + 0.12 * easeInOut(u)) * (1 + 0.08 * easeInOut(go)) * (1 - 0.1 * easeInOut(down)),
        roll: -1 + 3 * outCubic(go) - 5 * easeInOut(down),
      };
    },
  },
  // ⚠️ THE EYE LOCK: each face from where the other one is looking, from between them so nobody blocks it. Each only pushes in.
  {
    at: B.lockR, in: 0, cam: (f) => {
      const z = zack(f); const r = rago(f);
      const u = clamp01((f - B.lockR) / (B.lockZ - B.lockR));
      const dx = z.x - r.x; const dz = z.z - r.z; const d = Math.hypot(dx, dz) || 1;
      return { p: [r.x + (dx / d) * 1.5 + (dz / d) * 0.25, 1.25, r.z + (dz / d) * 1.5 - (dx / d) * 0.25], t: [r.x, 1.38, r.z], f: 950 * (1 + 0.12 * u), roll: 1.5 };
    },
  },
  {
    at: B.lockZ, in: 0, cam: (f) => {
      const z = zack(f); const r = rago(f);
      const u = clamp01((f - B.lockZ) / (B.yank - B.lockZ));
      const dx = r.x - z.x; const dz = r.z - z.z; const d = Math.hypot(dx, dz) || 1;
      return { p: [z.x + (dx / d) * 1.5 - (dz / d) * 0.25, 1.2, z.z + (dz / d) * 1.5 + (dx / d) * 0.25], t: [z.x, 1.32, z.z], f: 1000 * (1 + 0.14 * u), roll: -1.5 };
    },
  },
  {
    at: B.yank - 1, in: 0, cam: (f) => {
      const down = clamp01((f - B.trip) / (B.sprawl - B.trip));
      return { p: [5.7, 0.55, 0.05], t: [0.3, 0.8 - 0.15 * down, -0.2 + 0.4 * easeInOut(down)], f: 1280 * 1.12 * (1 - 0.1 * easeInOut(down)), roll: 2 - 5 * easeInOut(down) };
    },
  },
  // The crash zoom: five frames across the court, arriving exactly as he does.
  { at: B.land - 1, in: 5, cam: (f) => settle(f) },
  // He turns to us and the lens comes in on him.
  { at: B.fold + 2, in: 74, cam: (f) => landing(f) },
];

const camOf = (f: number, n: number): Cam => {
  const sh = SHOTS[n];
  const c = sh.cam(f);
  if (n === 0 || sh.in <= 0) return c;
  const t = (f - sh.at) / sh.in;
  if (t >= 1) return c;
  return blend(camOf(f, n - 1), c, sh.at === B.land - 1 ? outCubic(clamp01(t)) : easeInOut(clamp01(t)));
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
  const hit = f >= B.tump && f < B.tump + 26 ? (1 - (f - B.tump) / 26) * 0.09 : 0;
  const slam = f >= B.sprawl - 2 && f < B.sprawl + 16 ? (1 - (f - B.sprawl + 2) / 18) * 0.06 : 0;
  const arrive = B.land + 5;
  const jolt = (f >= B.jolt && f < B.jolt + 16 ? (1 - (f - B.jolt) / 16) * 0.05 : 0) + (f >= arrive && f < arrive + 18 ? (1 - (f - arrive) / 18) * 0.05 : 0);
  // Decaying smooth shakes, layered on the current camera.
  const sx = hit * Math.sin(f * 1.7) + (slam + jolt) * Math.sin(f * 1.3);
  const sy = hit * Math.sin(f * 2.3 + 1) + (slam + jolt) * Math.sin(f * 1.9 + 1);
  return { x: c.p[0] + sx, y: c.p[1] + sy, z: c.p[2], yaw: a.yaw, pitch: a.pitch, roll: c.roll, f: c.f, cx: 960, cy: 540 };
};

/** Screen speed of the whip, for its blur. */
export const panSpeed = (f: number) => {
  const a = camAt(f);
  const b = camAt(f - 1);
  const dy = Math.abs(a.yaw - b.yaw) * (a.f / 57.3);
  const dp = Math.abs(a.pitch - b.pitch) * (a.f / 57.3);
  const cut = SHOTS.some((s) => s.in === 0 && s.at === f);
  return cut ? { x: 0, y: 0 } : { x: dy, y: dp };
};

export { rago, STAND };
