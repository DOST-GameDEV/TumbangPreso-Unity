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

/** The teleport is a real cut: nothing may blur across it. */
const sameSide = (f: number, g: number) => (f < B.blink && g >= B.blink ? B.blink - 0.01 : f >= B.blink && g < B.blink ? B.blink : g);
const WINDOW = [-4, -3, -2, -1, 0, 1, 2];
const samplesOf = (f: number, layers: (g: number) => Layer[]): PoseSample[] => WINDOW.map((o) => ({ at: o, layers: layers(sameSide(f, f + o)) }));
const rounded = (f: number, v: (g: number) => number, r = 4) => {
  let sum = 0;
  let w = 0;
  for (let o = -r; o <= r; o++) {
    const k = Math.exp(-0.5 * (o / (r / 2)) * (o / (r / 2)));
    sum += v(sameSide(f, f + o)) * k;
    w += k;
  }
  return sum / w;
};
const path = (f: number, pts: [number, number, number][], ease: (t: number) => number = easeInOut) => {
  if (f <= pts[0][0]) return { x: pts[0][1], z: pts[0][2] };
  for (let i = 1; i < pts.length; i++) {
    if (f <= pts[i][0]) {
      const t = ease((f - pts[i - 1][0]) / (pts[i][0] - pts[i - 1][0]));
      return { x: pts[i - 1][1] + (pts[i][1] - pts[i - 1][1]) * t, z: pts[i - 1][2] + (pts[i][2] - pts[i - 1][2]) * t };
    }
  }
  const l = pts[pts.length - 1];
  return { x: l[1], z: l[2] };
};
const line = (t: number) => t;

// ------------------------------------------------------------------------------------ places

/** Her mark on the south line: near the hold's lens, the whole lot in front of her. */
export const POST = { x: -0.45, z: -8 };
/** Nemu's post by the can, and where the blast sends her back to. */
export const GUARD = { x: 1.15, z: -1.15 };
export const SLIP_REST = { x: 3.2, z: 1.6 };
/** Where the ring burns: between them, closing on Nemu. */
export const RING = { x: 0.75, z: -3.6 };
const BEFORE = { x: 1.6, z: -5.2 }; // where she vanishes
export const SKID_FROM = { x: -2.2, z: -8.7 };
const CAN_DOWN = { x: 1.9, z: 1.3 };

// ------------------------------------------------------------------------------------ Phaister

const POINT: Pose = { torso: [-3, 8, 0], head: [-4, 0, 0], armR: [-92, 0, 22], foreR: [0, 8, 0], armL: [14, 0, 30], foreL: [0, -60, 0] };
const BOAST: Pose = { root: [8, 0, 0], torso: [8, -8, 0], head: [-8, 8, 4], armL: [-46, 20, 18], foreL: [0, -104, 0], armR: [-40, 0, 66], foreR: [0, 30, 0] };
const BRIM: Pose = { torso: [4, 0, 0], head: [20, 0, 0], armR: [-136, -18, 16], foreR: [0, 104, 0] };
const TWIRL: Pose = { root: [-2, 0, 0], torso: [-5, -10, 3], head: [14, 4, 0], armR: [-156, 0, 22], foreR: [0, 22, 0], armL: [14, 0, 34], foreL: [0, -70, 0] };
const WINDUP: Pose = { root: [8, 0, 0], torso: [8, 28, 0], head: [12, -24, 0], armR: [28, 0, 42], foreR: [0, 74, 0], armL: [-34, 0, 40] };
const FLICK: Pose = { root: [4, 0, 0], torso: [2, -26, 0], head: [10, 22, 0], armR: [66, 0, 36], foreR: [0, 8, 0], armL: [-46, 0, 46] };
const TADA: Pose = { root: [-3, 0, 0], torso: [-5, 0, 0], head: [-6, 0, 0], armR: [-60, 0, 90], armL: [-60, 0, 90], foreR: [0, 22, 0], foreL: [0, -22, 0], legL: [-8, 0, 12], legR: [6, 0, -12] };
const SAG: Pose = { root: [4, 0, 0], torso: [10, 0, 0], head: [14, 0, 0], armR: [10, 0, 6], armL: [10, 0, 6], foreR: [0, 8, 0], foreL: [0, -8, 0] };
const FRIGHT: Pose = { root: [-8, 0, 0], torso: [-8, 0, 0], head: [-6, 0, 0], armR: [-60, 0, 66], armL: [-60, 0, 66], foreR: [0, 50, 0], foreL: [0, -50, 0] };
const LANDED: Pose = { root: [24, 0, 0], torso: [8, 0, 0], head: [-14, 0, 0], armR: [-20, 0, 74], armL: [-20, 0, 74], legL: [-44, 0, 14], legR: [18, 0, -14] };
const STOMP: Pose = { root: [4, 0, 0], torso: [8, 0, 0], head: [6, 0, 0], armR: [8, 0, 26], armL: [8, 0, 26], legR: [-46, 0, 0] };
const RESOLVE: Pose = { torso: [-3, 0, 0], head: [-12, 0, 0], armR: [-30, 24, 14], foreR: [0, 110, 0], armL: [-30, -24, 14], foreL: [0, -110, 0] };
const RAISE: Pose = { root: [-4, 0, 0], torso: [-8, 0, 0], head: [-16, 0, 0], armR: [-168, 0, 26], armL: [-168, 0, 26], foreR: [0, 10, 0], foreL: [0, -10, 0] };
const SLAM: Pose = { root: [18, 0, 0], torso: [14, 0, 0], head: [-16, 0, 0], armR: [-70, 0, 14], armL: [-70, 0, 14], foreR: [0, 8, 0], foreL: [0, -8, 0], legL: [-30, 0, 12], legR: [22, 0, -12] };
const JOY: Pose = { root: [-3, 0, 0], torso: [-7, 0, 0], head: [-10, 0, 0], armR: [-150, 0, 36], armL: [-150, 0, 36], foreR: [0, 30, 0], foreL: [0, -30, 0] };
const PI = (at: number, fade: number, pose?: Pose): Seg => ({ at, fade, clip: 'idle', abs: 0, pose });

const P_TRACK: Seg[] = [
  { at: 0, fade: 0, clip: 'walk', rate: 1.15, pose: { head: [-6, 0, 0], torso: [-3, 0, 0] } },
  PI(B.dare - 8, 8),
  PI(B.dare, 8, POINT),
  PI(B.boast - 6, 8, BOAST),
  PI(B.blind - 4, 10, BRIM),
  PI(B.twirl, 12, TWIRL),
  PI(B.flick - 12, 8, WINDUP),
  { at: B.flick, fade: 3, clip: 'idle', abs: 0, pose: FLICK },
  { at: B.tada - 2, fade: 5, pose: TADA },
  { at: B.sag, fade: 7, pose: SAG },
  { at: B.run - 3, fade: 4, clip: 'sprint', rate: 1.05 },
  { at: B.scoop - 7, fade: 4, clip: 'slide', rate: 1.2, once: true },
  { at: B.faceOff - 2, fade: 3, pose: FRIGHT },
  { at: B.chase - 2, fade: 3, clip: 'sprint', rate: 1.15 },
  { at: B.blink, fade: 0, clip: 'hero-phaister-blink', rate: 1, once: true },
  { at: B.skid - 1, fade: 2, pose: LANDED },
  PI(B.skid + 22, 10, { head: [-8, 0, 3] }),
  PI(B.stomp, 5, STOMP),
  PI(B.stomp + 10, 8, { head: [4, 0, 0] }),
  PI(B.resolve, 10, RESOLVE),
  { at: B.raise, fade: 12, clip: 'idle', abs: 0, pose: RAISE },
  { at: B.slam - 3, fade: 3, clip: 'idle', abs: 0, pose: SLAM },
  PI(B.slam + 30, 14, { head: [-6, 0, 0] }),
  PI(B.laugh - 4, 5, JOY),
  PI(B.settle, 14, { head: [-6, 12, 3] }),
  PI(B.home + 6, 18, { head: [-5, 12, 3] }),
  { at: B.land + 14, fade: 10, clip: 'emote-yes', rate: 0.7, once: true },
  PI(B.fold, 30),
];

const walkIn = (f: number) => path(f, [[B.walk - 30, -6.5, -8], [B.dare - 6, POST.x, POST.z]], line);
const RUN_OUT: [number, number, number][] = [[B.run, POST.x, POST.z], [B.run + 18, 1.3, -5.6], [B.run + 36, 3.5, -2.5], [B.scoop, SLIP_REST.x + 0.55, SLIP_REST.z - 0.3]];
const RUN_BACK: [number, number, number][] = [[B.chase, SLIP_REST.x + 0.55, SLIP_REST.z - 0.3], [B.chase + 24, 3.6, -2.6], [B.blink, BEFORE.x, BEFORE.z]];
const herRoute = (f: number) => {
  if (f < B.scoop) return path(f, RUN_OUT, line);
  if (f < B.chase) return { x: SLIP_REST.x + 0.55, z: SLIP_REST.z - 0.3 };
  return path(f, RUN_BACK, line);
};
const herPlace = (f: number) => {
  if (f < B.run) return walkIn(f);
  if (f < B.blink) return herRoute(f);
  if (f < B.skid) return SKID_FROM;
  return path(f, [[B.skid, SKID_FROM.x, SKID_FROM.z], [B.skid + 16, POST.x, POST.z]], outCubic);
};
const herYaw = (f: number) => kf(f, [
  [0, 90], [B.dare - 6, 90], [B.dare + 4, 172], [B.boast - 8, 172], [B.boast, 20], [B.twirl - 2, 20], [B.twirl + 8, 4],
  [B.flick + 3, 4], [B.tada, 0], [B.run - 4, 0], [B.run + 3, 135], [B.scoop - 8, 170], [B.scoop, 190], [B.faceOff - 4, 190], [B.faceOff + 2, 318],
  [B.chase - 1, 318], [B.chase + 4, 366], [B.blink - 0.5, 340], [B.blink, 60, () => 1], [B.skid + 14, 62], [B.skid + 22, 150], [B.raise, 150], [B.raise + 8, 165], [B.laugh - 6, 165], [B.laugh, 16], [B.fold, 8],
]);

export const phaister = (f: number): Frame => {
  const at = f >= INTRO ? POST : herPlace(f);
  const hop = kf(f, [[B.laugh, 0], [B.laugh + 6, 0.22, outQuad], [B.laugh + 12, 0], [B.laugh + 18, 0.16, outQuad], [B.laugh + 24, 0]]);
  const held = f < B.flick || f >= B.scoop + 1;
  const lit = f >= B.raise && f < B.laugh;
  return {
    x: at.x,
    z: at.z,
    yaw: f >= INTRO ? 8 + 2 * idleSin(f, 2, 0.4) : rounded(f, herYaw),
    layers: f >= INTRO ? [{ clip: 'idle', t: (f - INTRO) / 30, w: 1 }] : layersAt(f, P_TRACK),
    samples: f < INTRO ? samplesOf(f, (g) => layersAt(g, P_TRACK)) : undefined,
    lift: [0, hop, 0],
    face: lit ? 'glow' : 'rest',
    eyeGlow: lit ? kf(f, [[B.raise, 0], [B.raise + 10, 1], [B.laugh - 8, 1], [B.laugh, 0]]) : 0,
    slipper: !held ? { at: 'none' } : f >= B.twirl && f < B.flick - 12 ? { at: 'hand', swing: 0, twist: (f - B.twirl) * 44 } : { at: 'hand', swing: 5 * idleSin(f, 18) },
    visible: f >= INTRO || f < B.blink || f >= B.skid - 2 ? 1 : 0,
  };
};

// ------------------------------------------------------------------------------------ Nemu

const NEMU_PHASE = 2.6;
const NI = (at: number, fade: number, pose?: Pose): Seg => ({ at, fade, clip: 'idle', abs: NEMU_PHASE, pose });
const N_REACH: Pose = { root: [12, 0, 0], torso: [8, 0, 0], head: [-10, 0, 0], armR: [-92, 0, 10], foreR: [0, 4, 0] };
const N_FINGER: Pose = { armR: [-46, 0, 14], foreR: [0, 10, 0] };
const N_THROWN: Pose = { root: [-10, 0, 0], armR: [-16, 0, 40], armL: [-16, 0, 40] };
const N_TRACK: Seg[] = [
  NI(0, 0),
  NI(B.reset - 6, 6, N_FINGER),
  { at: B.reset + 10, fade: 8, clip: 'sprint', rate: 0.9 },
  NI(B.faceOff - 4, 6, { head: [-6, 0, 0], armR: [-40, 0, 10] }),
  { at: B.chase + 8, fade: 6, clip: 'sprint', rate: 1 },
  NI(B.reach - 3, 4, N_REACH),
  NI(B.blink + 3, 6, { ...N_REACH, head: [6, 0, 0] }),
  NI(B.nemuComes - 4, 12),
  { at: B.nemuComes + 6, fade: 8, clip: 'walk', rate: 0.75 },
  NI(B.ring + 10, 10, { head: [8, 0, 0] }),
  { at: B.slam, fade: 2, pose: N_THROWN },
  NI(B.slam + 22, 12),
  { at: B.nod, fade: 6, clip: 'emote-yes', rate: 0.55, once: true },
  NI(B.nod + 30, 22),
];
const nemuPlace = (f: number) => {
  if (f < B.reset + 10) return GUARD;
  if (f < B.faceOff) return path(f, [[B.reset + 10, GUARD.x, GUARD.z], [B.faceOff - 2, 2.45, 0.35]], line);
  // Her heels: exactly where Phaister was a third of a second ago.
  if (f < B.blink) {
    if (f < B.chase + 8) return { x: 2.45, z: 0.35 };
    const h = herRoute(f - 10);
    return { x: h.x + 0.1, z: h.z + 0.5 };
  }
  const h = herRoute(B.blink - 10);
  if (f < B.nemuComes + 6) return { x: h.x + 0.1, z: h.z + 0.5 };
  if (f < B.slam) return path(f, [[B.nemuComes + 6, h.x + 0.1, h.z + 0.5], [B.ring + 10, RING.x, RING.z]], line);
  return path(f, [[B.slam, RING.x, RING.z], [B.slam + 22, GUARD.x, GUARD.z]], outCubic);
};
const nemuYaw = (f: number) => kf(f, [[0, 4], [B.reset, 4], [B.reset + 14, 150], [B.faceOff - 3, 130], [B.chase + 6, 130], [B.chase + 12, 4], [B.nemuComes, 4], [B.nemuComes + 8, 340], [B.nod, 350], [INTRO, 354]]);

export const nemu = (f: number): Frame => {
  const at = f >= INTRO ? GUARD : nemuPlace(f);
  const air = f >= B.slam && f < B.slam + 22 ? 4 * ((f - B.slam) / 22) * (1 - (f - B.slam) / 22) * 1.1 : 0;
  return {
    x: at.x,
    z: at.z,
    yaw: f >= INTRO ? -6 + 1.5 * idleSin(f, 3, 1) : rounded(f, nemuYaw),
    layers: f >= INTRO ? [{ clip: 'idle', t: (f - INTRO) / 30 + NEMU_PHASE, w: 1 }] : layersAt(f, N_TRACK),
    samples: f < INTRO ? samplesOf(f, (g) => layersAt(g, N_TRACK)) : undefined,
    lift: [0, air, 0],
    face: 'rest',
    eyeGlow: 0,
    slipper: { at: 'none' },
    visible: 1,
  };
};

/** Kuro, at her shoulder, and the only one who jumps. His faces are his own parts (`phaister/nemu.ts`). */
export const kuroAt = (f: number, n: Frame) => {
  const flinch = Math.max(kf(f, [[B.ring, 0], [B.ring + 6, 1, outQuad], [B.laugh, 0.6], [B.nod, 0]]), kf(f, [[B.klang + 2, 0], [B.klang + 6, 0.5, outQuad], [B.nemuFlat, 0]]));
  const happy = f >= B.nod + 16;
  return {
    p: [n.x + 0.85 + 0.16 * flinch, 1.72 + 0.07 * idleSin(f, 8) + 0.2 * flinch + n.lift[1], n.z - 0.18] as [number, number, number],
    yaw: 0,
    puff: 0.6 * flinch,
    face: flinch > 0.3 ? { show: ['KuroSparkleL', 'KuroSparkleR', 'KuroOhMouth'], hide: ['ghost-mouth-dot'], eyes: 1 }
      : happy ? { show: ['KuroHappyEyeL', 'KuroHappyEyeR', 'KuroGrinMouth'], hide: ['ghost-mouth-dot', 'ghost-eye-l', 'ghost-eye-r'] } : {},
  };
};

// ------------------------------------------------------------------------------------ the slipper and the can

const CAN_HIT: [number, number, number] = [0.05, 0.42, 0.05];
export const STOP = 7;
export type Fly = { p: [number, number, number]; rot: [number, number, number]; big: boolean };
/** Round her back, and a low hard line to the can. */
const throwAt = (u: number): [number, number, number] => {
  const a: [number, number, number] = [POST.x + 0.4, 1.3, POST.z + 0.5];
  const c: [number, number, number] = [-2.8, 2.2, -3.5];
  const v = 1 - u;
  return [0, 1, 2].map((i) => v * v * a[i] + 2 * v * u * c[i] + u * u * CAN_HIT[i]) as [number, number, number];
};
export const flyingAt = (f: number): Fly | null => {
  if (f < B.flick || f >= B.scoop + 1) return null;
  if (f < B.klang) {
    const u = (f - B.flick) / (B.klang - B.flick);
    return { p: throwAt(u), rot: [(f - B.flick) * 50, 80, 0], big: true };
  }
  if (f < B.klang + STOP) return { p: CAN_HIT, rot: [30, 120, 0], big: false };
  const land = B.klang + STOP + 24;
  if (f < land) {
    const u = (f - B.klang - STOP) / 24;
    return { p: [CAN_HIT[0] + (SLIP_REST.x - CAN_HIT[0]) * u, 0.1 + 1.6 * 4 * u * (1 - u) * (1 - u), CAN_HIT[2] + (SLIP_REST.z - CAN_HIT[2]) * u], rot: [(f - B.klang) * 24 * (1 - u), 40 * u, 0], big: false };
  }
  return { p: [SLIP_REST.x, 0.06, SLIP_REST.z], rot: [0, 40, 0], big: false };
};
/** The throw's path so far, for its ribbon. */
export const throwTrail = (f: number) => {
  const out: [number, number, number][] = [];
  const end = Math.min(f, B.klang);
  for (let g = B.flick; g <= end; g += 0.5) out.push(throwAt((g - B.flick) / (B.klang - B.flick)));
  return out;
};

export type CanFrame = { x: number; z: number; hop: number; rot: [number, number, number]; lying: number };
export const canAt = (f: number): CanFrame => {
  const at = B.klang + STOP;
  if (f < at || f >= B.reset + 14) return { x: 0, z: 0, hop: 0, rot: [0, 0, 0], lying: 0 };
  const air = 32;
  if (f < at + air) {
    const u = (f - at) / air;
    return { x: CAN_DOWN.x * u, z: CAN_DOWN.z * u, hop: 3.6 * 4 * u * (1 - u), rot: [0, 60 * u, 900 * outQuad(u)], lying: u };
  }
  if (f < B.reset) return { x: CAN_DOWN.x, z: CAN_DOWN.z, hop: 0, rot: [0, 80, 90], lying: 1 };
  // One finger: the can hops back into its circle.
  const u = easeInOut((f - B.reset) / 14);
  return { x: CAN_DOWN.x * (1 - u), z: CAN_DOWN.z * (1 - u), hop: 1.3 * 4 * u * (1 - u), rot: [0, 80 * (1 - u), 90 * (1 - u)], lying: 1 - u };
};
