import { B } from '../lib/beats';
import { inCubic, kf, linear, outBack, outCubic, outElastic, outQuad } from '../lib/kf';
import { clamp01, env, loopNoise, loopSin } from '../lib/time';
import { wind } from '../lib/wind';
import { addPose, Face, mixPose, Pose, SlipperState } from './actor';

/**
 * Zack's performance in the WIDE, on his real rig, frame by frame.
 *
 * ⚠️ ASTRA.md is the direction: bladed and side-on, confidence from ACCURACY rather than
 * swagger, a small pause before the snap, and electrical chatter instead of a settle. LORE.md:
 * he "makes a difficult play look almost casual". So the idle is a hero who is RELAXED, not a
 * mannequin: the weight sits on one leg and moves, the breath lifts the shoulders, the head
 * drifts, and every trick (the flip, the spin) is done without looking at the slipper.
 *
 * Conventions, measured on the test boards: yaw +90 faces screen right. On a bone, +x tips its
 * top forward, so a LIMB with +x swings its end BACK; arms take x forward-negative, y across,
 * z out from the body. Head y negative turns his face toward screen left.
 */

export type ZackFrame = {
  yaw: number;
  pose: Pose;
  lift: [number, number, number];
  face: Face;
  slipper: SlipperState;
  /** Screen-space shift of the whole figure in the wide (the run in from the left). */
  dx: number;
  /** 0..1 how much electricity is on him: sparks at the hands and feet. */
  charge: number;
  /** 0..1 static crackling off his hair (the shake). */
  hairStatic: number;
  /** Flat colour wash for the dead-stop flash. */
  flash: number;
};

/** The resting hero, before anything else is layered on. */
const STANCE: Pose = {
  root: [0, 0, -1.5],
  torso: [2, 8, 1],
  head: [-3, -16, 2],
  // The tsinelas is held up in front of his jacket by the strap, where it reads: hung at his
  // side it vanished against his black shorts in the first real-model render.
  armR: [-52, -14, 6],
  armL: [12, -4, 7],
  legL: [0, 0, 9],
  legR: [-5, 0, -5],
};
const STANCE_YAW = 20;

/** Breath, weight and drift: everything periodic on the loop so frame 900 is frame 0. */
const alive = (f: number): { pose: Pose; lift: [number, number, number] } => {
  const breath = loopSin(f, 10);
  const weight = loopSin(f, 3, 0.7);
  const nod = loopNoise('zhead', f, 1.4, 2);
  const turn = loopNoise('zheady', f, 1.2, 2, 3);
  return {
    pose: {
      root: [0, 0, weight * 1.6],
      torso: [1.4 * breath, 2.5 * weight, -weight * 1.2],
      head: [-1.2 * breath + 2.2 * nod, 5 * turn, 1.2 * weight],
      armR: [2 * breath, 0, 2.2 * (breath + 1) / 2],
      armL: [-1.5 * breath, 0, 2.4 * (breath + 1) / 2],
    },
    lift: [0.004 * weight, 0.0035 * (breath + 1) / 2, 0],
  };
};

/** Side-on run, his own rig: long pendulum legs, arms counter-swung, a bob on each contact. */
export const runCycle = (t: number, period = 10, amp = 1): { pose: Pose; lift: [number, number, number] } => {
  const p = (2 * Math.PI * t) / period;
  const s = Math.sin(p);
  return {
    pose: {
      root: [16 * amp, 0, 0],
      torso: [6 * amp, 4 * s * amp, 0],
      head: [-18 * amp, -3 * s, 0],
      legL: [-58 * s * amp, 0, 2],
      legR: [58 * s * amp, 0, -2],
      armL: [62 * s * amp, 0, 10],
      armR: [-62 * s * amp, 0, 10],
    },
    lift: [0, 0.03 * Math.abs(Math.cos(p)) * amp, 0],
  };
};

/**
 * ⚠️ THE CASUAL TOSS: THE IDLE'S HEARTBEAT. 🧑 2026-09-23 on the first real-model render: the
 * idle read as a mannequin, standing still for most of the loop. LORE.md's Zack makes the hard
 * thing look casual, so between the big tricks he keeps lobbing his tsinelas a hand's height and
 * catching it without looking, the way a player waits for his turn. A small dip, a flick, one turn
 * in the air, a soft catch that the whole arm absorbs.
 *
 * Only whole cycles that sit entirely inside a calm stretch play, so a toss never collides with a
 * beat; cycles are on a fixed 75-frame grid so the loop seam lands between two tosses.
 */
const TOSS = 75;
const BUSY: [number, number][] = [
  [B.pushIn, B.settle + 40],
  [B.peek - 4, B.peek + 48],
  [B.flip - 4, B.flip + 66],
  [B.spin - 4, B.spin + 74],
  [B.shake - 4, B.shake + 50],
];
const calm = (a: number, b: number) => BUSY.every(([x, y]) => b <= x || a >= y) && b <= 900;
export const toss = (f: number) => {
  const c0 = Math.floor(f / TOSS) * TOSS;
  if (!calm(c0, c0 + 60)) return null;
  const t = f - c0;
  const h = 0.12 + 0.05 * ((c0 / TOSS) % 3) / 2;
  const arm = kf(t, [[0, 0], [7, 9, outQuad], [11, -16, outCubic], [16, -4], [36, -6], [40, 12, outQuad], [48, -3, outQuad], [60, 0]]);
  const inAir = t >= 11 && t < 40;
  const u = (t - 11) / 29;
  return { arm, inAir, up: h * 4 * u * (1 - u), rot: 360 * u, catchBump: env(t, 39, 40, 42, 48) };
};

/** Where the flipped tsinelas is, relative to his fist, and how far it has turned. */
export const flipArc = (f: number) => {
  const t0 = B.flip + 12;
  const t1 = B.flip + 36;
  if (f < t0 || f > t1) return null;
  const t = (f - t0) / (t1 - t0);
  return { up: 0.44 * 4 * t * (1 - t), rot: 720 * outQuad(t), t };
};

/** How far left of his mark he is while running home, in wide pixels. 0 at rest. */
export const arriveDX = (f: number) =>
  f >= B.arrive && f < B.settle + 10 ? kf(f, [[B.arrive, -1250], [B.settle, 0, outCubic], [B.settle + 3, 22, outQuad], [B.settle + 10, 0, outCubic]]) : 0;

export const zackWide = (f: number): ZackFrame => {
  const a = alive(f);
  const w = wind(f);
  const swing = 6 * loopSin(f, 10, 0.6) + 12 * w;
  const tt = toss(f);
  const base: ZackFrame = {
    yaw: STANCE_YAW,
    pose: addPose(STANCE, a.pose, tt ? { armR: [tt.arm, 0, 0], torso: [0.4 * tt.arm * 0.2, 0, 0], head: [-0.6 * tt.catchBump, 0, 0] } : undefined),
    lift: a.lift,
    face: 'rest',
    slipper: tt?.inAir
      ? { at: 'fromHand', offset: [-0.08, 0.05 + tt.up, 0.05], rot: [90 + tt.rot, 0, 90] }
      : { at: 'hand', swing: swing + (tt ? 26 * tt.catchBump : 0), twist: 8 * loopSin(f, 5) },
    dx: 0,
    charge: 0,
    hairStatic: 0,
    flash: 0,
  };

  // ---------------------------------------------------------------- the gather, before the close-up
  // The wind comes up. He squares to the camera, lifts his chin into it, lets his arms float
  // out from his sides as the charge builds, and rises a touch onto his toes.
  if (f >= B.pushIn && f < B.cu) {
    const t = kf(f, [[B.pushIn, 0], [B.cu, 1, inCubic]]);
    const gather: Pose = { head: [-12, 16, 0], armR: [0, 0, 22], armL: [0, 0, 22], torso: [-4, -8, 0], root: [0, 0, 1.5] };
    return {
      ...base,
      yaw: kf(f, [[B.pushIn, STANCE_YAW], [B.cu, 4, outCubic]]),
      pose: addPose(base.pose, mixPose({}, gather, t)),
      lift: [a.lift[0], a.lift[1] + 0.012 * t, 0],
      charge: 0.15 + 0.5 * t,
      slipper: { at: 'hand', swing: swing + 18 * t * Math.sin(f * 0.9), twist: 0 },
    };
  }

  // ---------------------------------------------------------------- the run home and the skid
  if (f >= B.arrive && f < B.settle) {
    const skid = f >= B.settle - 7;
    if (!skid) {
      const r = runCycle(f - B.arrive, 9, 1.1);
      return {
        ...base,
        yaw: 90,
        pose: r.pose,
        lift: r.lift,
        face: 'sharp',
        slipper: { at: 'hand', swing: -60, twist: 0 },
        dx: arriveDX(f),
        charge: 0.8,
      };
    }
    // Plant: heels dig in, the body leans back hard against his own speed, arms thrown back.
    const t = (f - (B.settle - 7)) / 7;
    return {
      ...base,
      yaw: 90,
      pose: {
        root: [-22, 0, 0],
        torso: [-6, 0, 0],
        head: [10, 0, 0],
        legL: [-48, 0, 0],
        legR: [-20, 0, 0],
        armL: [40 - 20 * t, 0, 30],
        armR: [50 - 20 * t, 0, 34],
      },
      lift: [0, -0.02, 0],
      face: 'grit',
      slipper: { at: 'hand', swing: -80 + 30 * t, twist: 0 },
      dx: arriveDX(f),
      charge: 1,
    };
  }

  // ---------------------------------------------------------------- the dead stop
  // He snaps round to face us in FOUR frames with an overshoot, squashes into the stop, and the
  // electricity chatters off him instead of a settle (ASTRA.md). Only then does the smirk return.
  if (f >= B.settle && f < B.settle + 36) {
    const turn = kf(f, [[B.settle, 90], [B.settle + 4, STANCE_YAW - 14, outCubic], [B.settle + 12, STANCE_YAW + 4, outQuad], [B.settle + 22, STANCE_YAW]]);
    const squash = kf(f, [[B.settle, 1], [B.settle + 3, 0, outCubic], [B.settle + 14, 1, outElastic]]);
    const recover = kf(f, [[B.settle + 4, 0], [B.settle + 30, 1, outCubic]]);
    const stop: Pose = { root: [8, 0, 0], torso: [10, 0, 0], head: [-6, 0, 0], armR: [26, 0, 30], armL: [22, 0, 28], legL: [-6, 0, 14], legR: [10, 0, -10] };
    const chatter = env(f, B.settle, B.settle + 1, B.settle + 14, B.settle + 24);
    return {
      ...base,
      yaw: turn,
      pose: addPose(mixPose(stop, base.pose, recover), { head: [3 * chatter * Math.sin(f * 2.3), 4 * chatter * Math.sin(f * 1.7), 0] }),
      lift: [0, -0.03 * (1 - squash), 0],
      face: f < B.settle + 14 ? 'sharp' : 'rest',
      slipper: { at: 'hand', swing: kf(f, [[B.settle, -70], [B.settle + 8, 40, outQuad], [B.settle + 18, -14], [B.settle + 30, swing]]), twist: 0 },
      dx: arriveDX(f),
      charge: chatter,
    };
  }

  // ---------------------------------------------------------------- idle personality
  // The glance: his eyes come up to the camera, hold it, and the smirk comes back.
  if (f >= B.peek && f < B.peek + 44) {
    const look = env(f, B.peek, B.peek + 5, B.peek + 30, B.peek + 40);
    return {
      ...base,
      pose: addPose(base.pose, { head: [-4 * look, 12 * look, -4 * look], torso: [0, 4 * look, 0] }),
      face: look > 0.5 && f < B.peek + 30 ? 'sharp' : 'rest',
    };
  }

  // The flip: a dip, a flick, the tsinelas turns twice in the air, caught without looking. He
  // never takes his eyes off the camera; that is the whole joke.
  if (f >= B.flip && f < B.flip + 62) {
    const x = kf(f, [[B.flip, -10], [B.flip + 8, 18, outQuad], [B.flip + 12, -78, outCubic], [B.flip + 30, -70], [B.flip + 36, -58], [B.flip + 40, -40, outBack], [B.flip + 62, -10]]);
    const arc = flipArc(f);
    const pose = addPose(base.pose, { armR: [x + 10, 0, 0], torso: [0, kf(f, [[B.flip, 0], [B.flip + 12, -6], [B.flip + 40, 0]]), 0] });
    return {
      ...base,
      pose,
      slipper: arc
        ? { at: 'fromHand', offset: [0, 0.08 + arc.up, 0.02], rot: [90, 0, arc.rot] }
        : { at: 'hand', swing: f > B.flip + 36 ? kf(f, [[B.flip + 36, -50], [B.flip + 48, 22, outQuad], [B.flip + 62, swing]]) : swing, twist: 0 },
      charge: env(f, B.flip + 36, B.flip + 37, B.flip + 42, B.flip + 50),
      face: 'rest',
    };
  }

  // The strap spin: forearm up, the tsinelas goes round his fist twice and stops dead, hanging.
  if (f >= B.spin && f < B.spin + 70) {
    const up = env(f, B.spin, B.spin + 10, B.spin + 52, B.spin + 66);
    const ang = kf(f, [[B.spin + 8, 0], [B.spin + 46, 720, linear], [B.spin + 52, 720, outElastic]]);
    const spinning = f >= B.spin + 8 && f < B.spin + 52;
    const r = 0.13;
    const th = (ang * Math.PI) / 180;
    return {
      ...base,
      pose: addPose(base.pose, { armR: [-55 * up, 10 * up, 6 * up], head: [0, 6 * up, 0] }),
      slipper: spinning
        ? { at: 'fromHand', offset: [Math.sin(th) * r * 0.3, -Math.cos(th) * r, Math.sin(th) * r], rot: [90 + ang, 0, 90] }
        : { at: 'hand', swing: f >= B.spin + 52 ? kf(f, [[B.spin + 52, 30], [B.spin + 62, -12, outQuad], [B.spin + 70, swing]]) : swing, twist: 0 },
      charge: env(f, B.spin + 46, B.spin + 47, B.spin + 52, B.spin + 60) * 0.8,
    };
  }

  // The static shake: his streak crackles, he shakes it off like a wet dog, a spark pops.
  if (f >= B.shake && f < B.shake + 46) {
    const build = env(f, B.shake, B.shake + 12, B.shake + 16, B.shake + 22);
    const shake = env(f, B.shake + 14, B.shake + 16, B.shake + 26, B.shake + 32);
    const osc = Math.sin((f - B.shake) * 2.2);
    return {
      ...base,
      pose: addPose(base.pose, {
        head: [-4 * build, 16 * shake * osc, 6 * shake * osc],
        torso: [0, 5 * shake * osc, 0],
        armR: [0, 0, 16 * shake * (0.5 + 0.5 * osc)],
        armL: [0, 0, 16 * shake * (0.5 - 0.5 * osc)],
      }),
      face: shake > 0.3 ? 'grit' : 'rest',
      hairStatic: build * 0.9,
      slipper: { at: 'hand', swing: swing + 30 * shake * osc, twist: 0 },
    };
  }

  return base;
};

export { clamp01 };
