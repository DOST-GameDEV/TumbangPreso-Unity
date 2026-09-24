import { noise2D } from '@remotion/noise';
import { easeInOut, env, loopSin } from './time';
import { kf, outCubic, outQuad } from '../lib/kf';
import { PB } from './beats';
import { aim, V3, View } from './view';
import { CAST, phaister, SAFE } from './perform';
import { kuro, nemu } from './nemu';
import { HER_MARK } from './map';

/**
 * ⚠️ THE SHOT LIST. A camera that performs with her: one wide shot that holds the whole throw, from her
 * twirl to the slipper coming to rest on the overclock pad, a tracking run to it past PC Express, a crane up and out from under the guideway to see Grand Coven whole, and HARD
 * CUTS only for the punchline, because comedy is timed in cuts (well? / nothing / Kuro / the take).
 *
 * Each shot is a lens position, a point it looks at, a focal length and a roll. A shot starts by
 * blending from wherever the previous shot is at that moment, over its `in` frames: 0 is a cut, 4 to 7 is
 * a whip (blurred by `panSpeed`), 20+ is a camera move. Because the blend reads the previous shot LIVE,
 * nothing restarts at a join (docs/HOME_SCREEN_ANIMATION_METHOD.md § 4.1).
 *
 * ⚠️ The calm camera is periodic on the loop, and the last shot is the calm camera, so frame LOOP is
 * frame 0.
 */

type Cam = { p: V3; t: V3; f: number; roll: number };
type Shot = { at: number; in: number; cam: (f: number) => Cam };

const lerp3 = (a: V3, b: V3, t: number): V3 => [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t];
const blend = (a: Cam, b: Cam, t: number): Cam => ({
  p: lerp3(a.p, b.p, t),
  t: lerp3(a.t, b.t, t),
  // Focal length blends in log space, so a zoom has an even feel across its range.
  f: Math.exp(Math.log(a.f) + (Math.log(b.f) - Math.log(a.f)) * t),
  roll: a.roll + (b.roll - a.roll) * t,
});

const M = HER_MARK;

/**
 * The calm: low behind the south line and to her right, looking up the court under the deck. Her face
 * is to us (she stands turned to the lens, a showwoman), the court, the can, Nemu and the column pair
 * behind her. The lens walks a slow arc and breathes, all periodic on the loop.
 */
const calm = (f: number): Cam => ({
  p: [0.6 + 0.45 * loopSin(f, 2, 0.4), 1.3 + 0.07 * loopSin(f, 3, 1.1), -13.2 + 0.35 * loopSin(f, 2, 2.1)],
  t: [-0.5 + 0.3 * loopSin(f, 2, 1.4), 1.6 + 0.05 * loopSin(f, 3, 0.2), -4.0],
  f: 1080 * (1 + 0.03 * loopSin(f, 3, 0.3)),
  roll: 0.6 * loopSin(f, 2, 0.5),
});

const her = (f: number) => phaister(f);
const herHead = (f: number): V3 => {
  const h = her(f);
  return [h.x, h.y + 1.78, h.z];
};

const SHOTS: Shot[] = [
  { at: 0, in: 0, cam: calm },
  // She points down the court at Nemu: the lens leans past her shoulder toward the target.
  {
    at: PB.dare - 4, in: 20, cam: (f) => {
      const h = her(f);
      const n = nemu(f);
      const push = kf(f, [[PB.dare, 0], [PB.blind, 1]]);
      // Leaning in from the calm: her in front, the thumb-back point, Nemu small down the court behind her.
      const c = calm(f);
      return { p: lerp3(c.p, [h.x + 1.1, 1.4, h.z - 4.2], 0.7 + 0.2 * push), t: lerp3(herHead(f), [n.x, 1.3, n.z], 0.25), f: 1120 + 60 * push, roll: 0.5 };
    },
  },
  // The boast: front on, medium, for her hand on her chest and the flourish to us.
  {
    at: PB.boast - 2, in: 12, cam: (f) => {
      const h = her(f);
      const push = kf(f, [[PB.boast, 0], [PB.blind, 1]]);
      return { p: [h.x + 1.0, 1.45, h.z - 3.5 + 0.3 * push], t: [h.x - 0.05, 1.45, h.z], f: 1120 + 80 * push, roll: 0 };
    },
  },
  // The blindfold: in close on her face as the brim comes down over her eyes. No looking.
  {
    at: PB.blind + 2, in: 16, cam: (f) => {
      const h = her(f);
      return { p: [h.x + 1.4, 1.35, h.z - 3.6], t: [h.x - 0.2, 1.4, h.z], f: 1120 + 40 * (f - PB.blind) / 30, roll: -1.5 };
    },
  },
  // ⚠️⚠️ THE THROW IS ONE WIDE SHOT, BECAUSE EVERY LINK OF IT MUST BE SEEN. Raised, south east of her,
  // looking up the court to the north west: her in the foreground turned to us, the can and Nemu down the court to the
  // right, the overclock pad and PC Express beyond her to the left. The twirl, the flick behind her back,
  // the lob over her head, KLANG!, the ricochet onto the pad, her ta-da without looking, and the look back
  // at Nemu all happen in this frame. The old throw cut and whipped after a slipper a few pixels wide and
  // lost it off screen, and 🧑 read it as *"it flies to a wall FOR NO REASON"*.
  // ⚠️ RAISED TO 4.4 m. At eye height the pad sat right behind her hat and the ricochet landed out of sight
  // (the first stills); moved east to clear it, a bridge column filled half the frame. From 4.4 m the pad,
  // 22 m away, shows ABOVE her hat, and the lens stays west of the south east column.
  {
    at: PB.twirl - 8, in: 30, cam: (f) => {
      const push = kf(f, [[PB.twirl, 0], [PB.run, 1]]);
      const p: V3 = [4.0 - 0.3 * push, 4.4 - 0.2 * push, -13.5 + 0.5 * push];
      const t: V3 = [-2.0, 1.2, -5.2];
      return { p, t, f: 1180 + 60 * push, roll: 0 };
    },
  },
  // The run, side-on from the carriageway, looking west: PC Express, the tarps and the pylon slide past.
  // She is turned about 30 degrees to the lens so she is a face and not a wall of hair.
  {
    at: PB.run + 2, in: 7, cam: (f) => {
      const h = her(f);
      // Her heading is about (-0.52, 0.85); the lens runs alongside, ahead and to her right.
      // Ahead of her and a little to her right, backing away as she comes: she runs AT the lens.
      return { p: [h.x - 0.6, 1.25, h.z + 4.4], t: [h.x - 0.2, 1.2, h.z + 0.3], f: 1050, roll: -1 };
    },
  },
  // The scoop, low and slow on the pad.
  {
    at: PB.scoop - 5, in: 12, cam: (f) => {
      const h = her(f);
      return { p: [h.x - 0.2, 0.75, h.z + 3.3], t: [h.x - 0.1, 0.8, h.z], f: 1100 + 4 * (f - PB.scoop), roll: -2 };
    },
  },
  // The standoff, from the west pavement south of them, low, looking up the pavement: Nemu in the lane,
  // the deck edge overhead, the train's shadow and window light arriving from behind the lens.
  {
    at: PB.block, in: 7, cam: (f) => {
      // ⚠️ Framed on where she WAS at the blink. Her position jumps to SAFE at blink + 12 while she is
      // invisible, and following it jumped this shot's aim in one frame (19.57 s, smooth.py, 2026-09-24).
      const h = her(Math.min(f, PB.blink));
      const n = nemu(f);
      const mid: V3 = [(h.x + n.x) / 2, 1.5, (h.z + n.z) / 2];
      const dutch = kf(f, [[PB.juke, 0], [PB.tag, -7, outQuad]]);
      const push = kf(f, [[PB.block, 0], [PB.tag + 6, 1]]);
      return { p: [mid[0] + 6.4 - 1.6 * push, 1.05, mid[2] - 1.2], t: mid, f: 1000 + 200 * push, roll: dutch };
    },
  },
  // Safe: whip to her, thrown open across the line; the two-shot holds for Nemu's slow turn.
  {
    at: PB.safe - 2, in: 6, cam: (f) => {
      const n = nemu(f);
      const push = kf(f, [[PB.deadpan, 0], [PB.stomp + 20, 1]]);
      return { p: lerp3([-9.6, 1.3, -14.6], [-9.6, 1.35, -13.8], push), t: lerp3([SAFE.x, 1.5, SAFE.z], [n.x, 1.5, n.z], 0.12 + 0.12 * push), f: 1380 + 120 * push, roll: 0 };
    },
  },
  // Resolve, rise, ignite: in front of her as she walks to the middle of the line and leaves the road.
  // The lens rises with her, then pushes in, dutched, on her face as her eyes strike.
  {
    at: PB.resolve + 8, in: 10, cam: (f) => {
      const h = her(f);
      const face: V3 = [h.x, h.y + 1.8, h.z];
      const close = env(f, PB.ignite - 6, PB.ignite + 8, PB.sky - 6, PB.sky + 4);
      const az = kf(f, [[PB.resolve + 8, 80], [PB.rise - 10, 80], [PB.rise + 30, 12, easeInOut]]) * Math.PI / 180;
      const dist = kf(f, [[PB.resolve + 8, 4.2], [PB.rise + 20, 3.6]]);
      const p: V3 = [h.x + Math.sin(az) * dist, 1.1 + 0.75 * h.y, h.z + Math.cos(az) * dist];
      return { p, t: lerp3([h.x, h.y + 1.35, h.z], face, 0.3 + 0.6 * close), f: 1150 * (1 + 0.95 * close), roll: -5 * close };
    },
  },
  // The crane: up and out from under the guideway onto the east pavement, looking down across the
  // whole street, the only frame that sees the circle whole. Its stamp shakes it.
  {
    at: PB.sky - 2, in: 28, cam: (f) => {
      const drift = kf(f, [[PB.sky, 0], [PB.stamp + 10, 1]]);
      const sh = env(f, PB.stamp, PB.stamp + 1, PB.stamp + 3, PB.stamp + 16);
      const n = (k: string) => noise2D(k, f * 0.45, 0) * sh;
      return { p: [5.9 - 0.3 * drift + 0.12 * n('cx'), 6.5 + 0.3 * drift + 0.12 * n('cy'), 4.4 - 1.0 * drift], t: [-0.3, 0.8 + 0.1 * n('ty'), -7.2], f: 720 - 30 * drift, roll: 1.5 * n('cr') };
    },
  },
  // The punchline, in cuts. Well? (her) / nothing (Nemu) / Kuro (a snap push) / the take (her).
  {
    at: PB.well, in: 0, cam: (f) => {
      const h = her(f);
      return { p: [h.x + 0.9, h.y + 1.45, h.z + 2.9], t: [h.x - 0.05, h.y + 1.72, h.z], f: 1500 + 2 * (f - PB.well), roll: 0 };
    },
  },
  {
    at: PB.nothing, in: 0, cam: (f) => {
      const n = nemu(f);
      return { p: [n.x - 0.7, n.y + 1.85, n.z - 3.2], t: [n.x, n.y + 1.95, n.z], f: 1300 + 2 * (f - PB.nothing), roll: 0 };
    },
  },
  {
    at: PB.kuro, in: 4, cam: (f) => {
      const n = nemu(f);
      const k = kuro(f, n);
      return { p: [n.x - 0.7, n.y + 1.45, n.z - 3.0], t: [k.x, k.y + 0.2, k.z], f: 3000, roll: 3 * Math.sin((f - PB.kuro) * 1.3) * env(f, PB.kuro, PB.kuro + 4, PB.kuro + 10, PB.take) };
    },
  },
  {
    at: PB.take, in: 0, cam: (f) => {
      const h = her(f);
      return { p: [h.x + 1.3, h.y + 1.4, h.z + 3.6], t: [h.x - 0.1, h.y + 1.6, h.z], f: 1250, roll: 0 };
    },
  },
  // Her hat tip lands on Kuro: cut to his heart eyes, and hold as he goes shy behind Nemu.
  {
    at: PB.tipK + 14, in: 0, cam: (f) => {
      const n = nemu(f);
      const k = kuro(f, n);
      return { p: [n.x - 0.9, n.y + 1.6, n.z - 3.2], t: [k.x * 0.8 + n.x * 0.2, k.y + 0.1, k.z * 0.8 + n.z * 0.2], f: 2400, roll: 0 };
    },
  },
  // Back out to the calm: she floats down onto her mark, the circle still turning on the road.
  { at: PB.descend + 6, in: 44, cam: calm },
];

/** Shot i at frame f, blended from shot i-1 (itself evaluated the same way) over its `in` frames. */
const evalShot = (i: number, f: number): Cam => {
  const sh = SHOTS[i];
  const c = sh.cam(f);
  if (i === 0 || sh.in === 0 || f >= sh.at + sh.in) return c;
  return blend(evalShot(i - 1, f), c, easeInOut(Math.max(0, (f - sh.at) / sh.in)));
};

const raw = (f: number): Cam => {
  let i = 0;
  while (i < SHOTS.length - 1 && f >= SHOTS[i + 1].at) i++;
  return evalShot(i, f);
};

/** Small smooth rumbles: KLANG!, the train overhead. Never a per-frame jitter (method § 4.1). */
const rumble = (f: number) => env(f, PB.klang, PB.klang + 1, PB.klang + 3, PB.klang + 12) * 0.6 + env(f, PB.tag - 12, PB.tag - 4, PB.tag + 10, PB.safe) * 0.5;

export const camAt = (f: number): View => {
  const c = raw(f);
  const r = rumble(f);
  const n = (k: string) => noise2D(k, f * 0.4, 0) * r;
  const t: V3 = [c.t[0] + 0.06 * n('rx'), c.t[1] + 0.06 * n('ry'), c.t[2]];
  const a = aim(c.p, t);
  return { x: c.p[0], y: c.p[1], z: c.p[2], yaw: a.yaw, pitch: a.pitch, roll: c.roll + 0.8 * n('rr'), f: c.f, cx: 960, cy: 540 };
};

/** How far the frame slides between this frame and the next, in pixels: the whip blur reads it. */
export const panSpeed = (f: number) => {
  const a = camAt(f - 1);
  const b = camAt(f + 1);
  let dy = b.yaw - a.yaw;
  if (dy > 180) dy -= 360;
  if (dy < -180) dy += 360;
  const dx = (dy * Math.PI) / 180 * b.f;
  const dp = ((b.pitch - a.pitch) * Math.PI) / 180 * b.f;
  return { x: Math.abs(dx) / 2, y: Math.abs(dp) / 2 };
};

export { CAST, M };
