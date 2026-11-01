import { Face, Layer, Pose, PoseSample, SlipperState } from '../three/actor';
import { kf, outCubic, outQuad } from '../lib/kf';
import { clamp01, easeInOut, idleSin, INTRO } from './time';
import { B } from './beats';

/**
 * On the models' own clips with keyed tweaks, crossfaded, each bone smoothed over neighbouring frames
 * (`bgc/perform.ts` has the reasons, all of them the owner's). Yaw 0 faces SOUTH, toward the lens.
 */
export type Frame = {
  x: number; z: number; yaw: number; layers: Layer[]; samples?: PoseSample[];
  lift: [number, number, number]; face: Face; eyeGlow: number; slipper: SlipperState; visible: number;
};
type Seg = { at: number; fade: number; clip?: string; rate?: number; t0?: number; once?: boolean; abs?: number; pose?: Pose };

const layersAt = (f: number, track: Seg[]): Layer[] => {
  let i = 0;
  for (let n = 0; n < track.length; n++) if (f >= track[n].at) i = n;
  let first = i;
  while (first > 0 && (f - track[first].at) / Math.max(1, track[first].fade) < 1) first--;
  const out: Layer[] = [];
  for (let n = first; n <= i; n++) {
    const g = track[n];
    const u = g.fade <= 0 ? 1 : clamp01((f - g.at) / g.fade);
    const t = g.abs !== undefined ? (f - INTRO) / 30 + g.abs : (g.t0 ?? 0) + ((f - g.at) * (g.rate ?? 1)) / 30;
    out.push({ clip: g.clip, t, once: g.once, pose: g.pose, w: easeInOut(u) });
  }
  return out;
};

/** Both blinks are real cuts: nothing may blur across them. */
const CUTS = [B.blink, B.home];
const sameSide = (f: number, g: number) => {
  for (const c of CUTS) {
    if (f < c && g >= c) return c - 0.01;
    if (f >= c && g < c) return c;
  }
  return g;
};
const WINDOW = [-5, -4, -3, -2, -1, 0, 1, 2, 3];
const samplesOf = (f: number, layers: (g: number) => Layer[]): PoseSample[] => WINDOW.map((o) => ({ at: o, layers: layers(sameSide(f, f + o)) }));
const rounded = (f: number, v: (g: number) => number) => {
  let sum = 0;
  let w = 0;
  for (let o = -6; o <= 6; o++) {
    const k = Math.exp(-0.5 * (o / 3) * (o / 3));
    sum += v(sameSide(f, f + o)) * k;
    w += k;
  }
  return sum / w;
};
const path = (f: number, pts: [number, number, number][]) => {
  if (f <= pts[0][0]) return { x: pts[0][1], z: pts[0][2] };
  for (let i = 1; i < pts.length; i++) {
    if (f <= pts[i][0]) {
      const t = easeInOut((f - pts[i - 1][0]) / (pts[i][0] - pts[i - 1][0]));
      return { x: pts[i - 1][1] + (pts[i][1] - pts[i - 1][1]) * t, z: pts[i - 1][2] + (pts[i][2] - pts[i - 1][2]) * t };
    }
  }
  const l = pts[pts.length - 1];
  return { x: l[1], z: l[2] };
};
const yawTo = (a: { x: number; z: number }, b: { x: number; z: number }) => Math.atan2(b.x - a.x, a.z - b.z) / (Math.PI / 180);

// ------------------------------------------------------------------------------------ places

/** Her mark: between the lens and the can, her back to it. */
export const POST = { x: -0.45, z: -2.1 };
export const NEMU_LINE = { x: 1.5, z: 8 };
const CAN_DOWN = { x: -1.7, z: 1.9 };
const SLIP_REST = { x: 0.95, z: 3.0 };
const NEMU_AT_SLIP = { x: 1.1, z: 3.75 };
/** Where she appears: right in front of Nemu. */
export const TADA = { x: -0.15, z: 2.75 };
const DOLL_SIT = { x: 0.62, z: -0.2 };
const DOLL_PUSH = { x: -1.3, z: 1.45 };

// ------------------------------------------------------------------------------------ Phaister

const BOW: Pose = { root: [24, 0, 0], torso: [16, 0, 0], head: [8, 0, 0], armR: [-40, 0, 30], foreR: [0, 50, 0], armL: [30, 0, 30], foreL: [0, -18, 0] };
const FLOURISH: Pose = { root: [-3, 0, 0], torso: [-5, 6, 0], head: [-8, -8, 3], armR: [-52, 0, 60], foreR: [0, 30, 0], armL: [14, 0, 44] };
const WAIT: Pose = { torso: [-3, 0, 0], head: [-5, 8, 4], armR: [-84, 10, 24], foreR: [0, 96, 0] };
const REACH_BACK: Pose = { root: [2, 0, 0], torso: [3, 16, 0], head: [-3, -12, 4], armR: [52, 0, 36], foreR: [0, 6, 0], armL: [-18, 0, 14], foreL: [0, -44, 0] };
const TADA_POSE: Pose = { root: [-2, 0, 0], torso: [-3, 0, 0], head: [-4, 0, 0], armR: [-58, 0, 88], armL: [-58, 0, 88], foreR: [0, 22, 0], foreL: [0, -22, 0], legL: [-8, 0, 12], legR: [6, 0, -12] };
const SAGGED: Pose = { root: [3, 0, 0], torso: [7, 0, 0], head: [10, 0, 0], armR: [12, 0, 8], armL: [12, 0, 8], foreR: [0, 10, 0], foreL: [0, -10, 0] };
const JOY: Pose = { root: [-2, 0, 0], torso: [-4, 0, 0], head: [-6, 0, 0], armR: [-140, 0, 40], armL: [-140, 0, 40], foreR: [0, 30, 0], foreL: [0, -30, 0] };
const LOOK_UP: Pose = { head: [-22, 0, 0], torso: [-5, 0, 0] };
const LOOK_DOWN: Pose = { head: [28, 0, 0], torso: [6, 0, 0] };

const PI = (at: number, fade: number, pose?: Pose): Seg => ({ at, fade, clip: 'idle', abs: 0, pose });
const P_TRACK: Seg[] = [
  PI(0, 0, LOOK_DOWN),
  PI(B.lift, 16, LOOK_UP),
  PI(B.whole - 4, 12, { head: [-4, 0, 0] }),
  { at: B.bow - 16, fade: 14, pose: FLOURISH },
  { at: B.bow, fade: 12, pose: BOW },
  PI(B.bow + 34, 16, { head: [-6, -6, 3] }),
  PI(B.klang + 12, 12, WAIT),
  PI(B.fetch, 16, { head: [-6, -8, 4] }),
  PI(B.reach - 2, 16, REACH_BACK),
  { at: B.marked + 10, fade: 12, clip: 'hero-phaister-blink', rate: 0.7, once: true },
  { at: B.blink, fade: 0, clip: 'hero-phaister-blink', t0: 0.5, rate: 0.5, once: true },
  { at: B.tada - 12, fade: 9, pose: TADA_POSE },
  { at: B.sag, fade: 16, pose: SAGGED },
  { at: B.joy, fade: 5, pose: JOY },
  { at: B.home, fade: 0, clip: 'emote-yes', rate: 1, once: true },
  PI(B.land + 4, 18, { head: [-5, 12, 3] }),
  PI(B.fold, 40),
];

const herPlace = (f: number) => (f >= B.blink && f < B.home ? TADA : POST);
const herYaw = (f: number) => (f >= B.home ? kf(f, [[B.home, 10], [B.fold, 8]]) : f >= B.blink ? yawTo(TADA, NEMU_AT_SLIP) - 24 : kf(f, [[0, 6], [B.fold, 6]]));

/** Off through the train's light, between the two halves of the blink. */
export const herShown = (f: number) => (f >= B.blink && f < B.tada - 14 ? 0 : 1);

export const phaister = (f: number): Frame => {
  const at = f >= INTRO ? POST : herPlace(f);
  const hop = kf(f, [[B.joy, 0], [B.joy + 6, 0.2, outQuad], [B.joy + 12, 0], [B.joy + 18, 0.13, outQuad], [B.joy + 24, 0]]) + (f >= B.tada - 13 && f < B.tada ? kf(f, [[B.tada - 13, 0.1], [B.tada - 6, 0]]) : 0);
  return {
    x: at.x,
    z: at.z,
    yaw: f >= INTRO ? 8 + 2 * idleSin(f, 2, 0.4) : rounded(f, herYaw),
    layers: f >= INTRO ? [{ clip: 'idle', t: (f - INTRO) / 30, w: 1 }] : layersAt(f, P_TRACK),
    samples: f < INTRO ? samplesOf(f, (g) => layersAt(g, P_TRACK)) : undefined,
    lift: [0, hop, 0],
    face: 'rest',
    eyeGlow: 0,
    slipper: { at: 'none' },
    visible: f >= INTRO ? 1 : herShown(f),
  };
};

// ------------------------------------------------------------------------------------ Nemu

const AIM: Pose = { torso: [2, 12, 0], head: [2, -8, 0], armR: [-92, 0, 22], foreR: [0, 60, 0] };
const LOOSE: Pose = { torso: [6, -14, 0], armR: [-58, 0, 16], foreR: [0, 8, 0] };
const NEMU_PHASE = 2.6;
const NI = (at: number, fade: number, pose?: Pose): Seg => ({ at, fade, clip: 'idle', abs: NEMU_PHASE, pose });
const N_TRACK: Seg[] = [
  NI(0, 0),
  NI(B.raise, 24, AIM),
  { at: B.throw - 2, fade: 4, pose: LOOSE },
  NI(B.klang + 6, 16, { head: [-4, 0, 0] }),
  { at: B.fetch, fade: 14, clip: 'walk', rate: 0.8 },
  { at: B.marked, fade: 20, clip: 'walk', rate: 0.52, pose: { head: [8, 0, 0] } },
  { at: B.scoop - 6, fade: 8, clip: 'pick-up', rate: 0.75, once: true },
  NI(B.rise, 12),
  NI(B.nothing, 14, { head: [-6, 0, 0] }),
  { at: B.nod, fade: 6, clip: 'emote-yes', rate: 0.55, once: true },
  NI(B.joy + 24, 22),
];
const nemuPlace = (f: number) => path(f, [[B.fetch, NEMU_LINE.x, NEMU_LINE.z], [B.scoop - 6, NEMU_AT_SLIP.x, NEMU_AT_SLIP.z]]);
const nemuYaw = (f: number) => kf(f, [[0, 8], [B.fetch, 8], [B.fetch + 12, yawTo(NEMU_LINE, NEMU_AT_SLIP)], [B.scoop - 6, yawTo(NEMU_LINE, NEMU_AT_SLIP)], [B.rise, 38], [B.nod + 30, 38], [B.home + 10, 18], [B.fold, -6], [INTRO, -6]]);

export const nemu = (f: number): Frame => {
  const at = f >= INTRO ? NEMU_AT_SLIP : nemuPlace(f);
  const has = f < B.throw || f >= B.scoop + 8;
  return {
    x: at.x,
    z: at.z,
    yaw: f >= INTRO ? -6 + 1.5 * idleSin(f, 3, 1) : rounded(f, nemuYaw),
    layers: f >= INTRO ? [{ clip: 'idle', t: (f - INTRO) / 30 + NEMU_PHASE, w: 1 }] : layersAt(f, N_TRACK),
    samples: f < INTRO ? samplesOf(f, (g) => layersAt(g, N_TRACK)) : undefined,
    lift: [0, 0, 0],
    face: 'rest',
    eyeGlow: 0,
    slipper: has ? { at: 'hand', swing: 4 * idleSin(f, 18) } : { at: 'none' },
    visible: 1,
  };
};

/** Kuro, at her shoulder. His faces are his own parts (`phaister/nemu.ts` has the sets). */
export const kuroAt = (f: number, n: Frame) => {
  const flinch = kf(f, [[B.tada, 0], [B.tada + 4, 1, outQuad], [B.nothing + 14, 0.2], [B.nod, 0]]);
  const happy = f >= B.joy;
  return {
    p: [n.x + 0.85 + 0.16 * flinch, 1.72 + 0.07 * idleSin(f, 8) + 0.16 * flinch, n.z - 0.18] as [number, number, number],
    yaw: kf(f, [[0, 0], [B.tada, 0], [B.tada + 6, 30]]),
    puff: 0.5 * flinch,
    face: flinch > 0.3 && !happy
      ? { show: ['KuroSparkleL', 'KuroSparkleR', 'KuroOhMouth'], hide: ['ghost-mouth-dot'], eyes: 1 }
      : happy ? { show: ['KuroHappyEyeL', 'KuroHappyEyeR', 'KuroGrinMouth'], hide: ['ghost-mouth-dot', 'ghost-eye-l', 'ghost-eye-r'] } : {},
  };
};

// ------------------------------------------------------------------------------------ the doll

export const DOLL_SCALE = 0.4;
const DOLL_TRACK: Seg[] = [
  { at: 0, fade: 0, clip: 'sit', t0: 0.5, rate: 0.2 },
  { at: B.stand, fade: 12, clip: 'idle', rate: 1 },
  { at: B.stand + 12, fade: 8, clip: 'walk', rate: 1.5 },
  { at: B.push - 8, fade: 6, clip: 'pick-up', rate: 0.9, once: true },
  { at: B.push + 18, fade: 8, clip: 'walk', rate: 1.2 },
  { at: B.reach + 8, fade: 10, clip: 'sit', t0: 0.5, rate: 0.2 },
];
const dollPlace = (f: number) => path(f, [[B.stand + 10, DOLL_SIT.x, DOLL_SIT.z], [B.push - 6, DOLL_PUSH.x, DOLL_PUSH.z], [B.push + 18, DOLL_PUSH.x, DOLL_PUSH.z], [B.reach + 10, DOLL_SIT.x, DOLL_SIT.z]]);
const dollYaw = (f: number) => kf(f, [[0, 340], [B.stand + 10, 340], [B.stand + 18, 360 + yawTo(DOLL_SIT, DOLL_PUSH)], [B.push + 16, 360 + yawTo(DOLL_SIT, DOLL_PUSH)], [B.push + 24, yawTo(DOLL_PUSH, DOLL_SIT)], [B.reach + 6, yawTo(DOLL_PUSH, DOLL_SIT)], [B.reach + 14, 340]]);

export const doll = (f: number): Frame => {
  const at = f >= INTRO ? DOLL_SIT : dollPlace(f);
  return {
    x: at.x,
    z: at.z,
    yaw: f >= INTRO ? 340 : rounded(f, dollYaw),
    layers: f >= INTRO ? [{ clip: 'sit', t: 0.5, w: 1, pose: { head: [3 * idleSin(f, 3), 6 * idleSin(f, 2), 0] } }] : layersAt(f, DOLL_TRACK),
    lift: [0, 0, 0],
    face: 'rest',
    eyeGlow: 0,
    slipper: { at: 'none' },
    visible: 1,
  };
};
export const dollLight = (f: number) => (f >= INTRO ? 0.3 + 0.1 * idleSin(f, 6) : kf(f, [[B.wake, 0], [B.wake + 10, 0.15], [B.wake + 13, 0], [B.wake + 18, 1], [B.reach + 10, 1], [B.reach + 34, 0.3], [INTRO, 0.3]]));

// ------------------------------------------------------------------------------------ the slipper and the can

const NEMU_HAND: [number, number, number] = [NEMU_LINE.x - 0.3, 1.5, NEMU_LINE.z - 0.3];
const CAN_HIT: [number, number, number] = [0.05, 0.42, 0.05];
export const STOP = 6;
export type Fly = { p: [number, number, number]; rot: [number, number, number]; big: boolean };
export const flyingAt = (f: number): Fly | null => {
  if (f < B.throw || f >= B.scoop + 8) return null;
  if (f < B.klang) {
    const u = (f - B.throw) / (B.klang - B.throw);
    return { p: [NEMU_HAND[0] + (CAN_HIT[0] - NEMU_HAND[0]) * u, NEMU_HAND[1] + (CAN_HIT[1] - NEMU_HAND[1]) * u, NEMU_HAND[2] + (CAN_HIT[2] - NEMU_HAND[2]) * u], rot: [(f - B.throw) * 50, 80, 0], big: true };
  }
  if (f < B.klang + STOP) return { p: CAN_HIT, rot: [30, 120, 0], big: false };
  const land = B.klang + STOP + 20;
  if (f < land) {
    const u = (f - B.klang - STOP) / 20;
    return { p: [CAN_HIT[0] + (SLIP_REST.x - CAN_HIT[0]) * u, 0.1 + 1.1 * 4 * u * (1 - u) * (1 - u), CAN_HIT[2] + (SLIP_REST.z - CAN_HIT[2]) * u], rot: [(f - B.klang) * 24 * (1 - u), 40 * u, 0], big: false };
  }
  return { p: [SLIP_REST.x, 0.06, SLIP_REST.z], rot: [0, 40, 0], big: false };
};

export type CanFrame = { x: number; z: number; hop: number; rot: [number, number, number]; lying: number };
export const canAt = (f: number): CanFrame => {
  const at = B.klang + STOP;
  if (f < at || f >= B.push + 20) return { x: 0, z: 0, hop: 0, rot: [0, 0, 0], lying: 0 };
  const air = 28;
  if (f < at + air) {
    const u = (f - at) / air;
    return { x: CAN_DOWN.x * u, z: CAN_DOWN.z * u, hop: 2.8 * 4 * u * (1 - u), rot: [0, 60 * u, 900 * outQuad(u)], lying: u };
  }
  if (f < B.push + 6) return { x: CAN_DOWN.x, z: CAN_DOWN.z, hop: 0, rot: [0, 60 + 30 * outCubic(clamp01((f - at - air) / 14)), 90], lying: 1 };
  const u = easeInOut((f - B.push - 6) / 14);
  return { x: CAN_DOWN.x * (1 - u), z: CAN_DOWN.z * (1 - u), hop: 0.5 * 4 * u * (1 - u), rot: [0, 90 * (1 - u), 90 * (1 - u)], lying: 1 - u };
};
