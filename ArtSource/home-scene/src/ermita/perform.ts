import { Face, Layer, Pose, PoseSample, SlipperState } from '../three/actor';
import { kf, outCubic, outQuad } from '../lib/kf';
import { clamp01, easeInOut, idleSin, INTRO, ph } from './time';
import { B } from './beats';
import { calmLayer } from '../lib/calm';

/**
 * On the models' own clips with keyed tweaks, crossfaded, each bone smoothed over neighbouring frames
 * (`bgc/perform.ts` has the reasons, all of them the owner's). Yaw 0 faces SOUTH, +90 east.
 * The map's can is at the origin; the south throwing line is at z -8; Taft and the LRT are 23 m east.
 */
export type Frame = {
  x: number; z: number; yaw: number; layers: Layer[]; samples?: PoseSample[];
  lift: [number, number, number]; face: Face; eyeGlow: number; slipper: SlipperState; visible: number;
  /** The slipper is at her belt: `CharacterMotor.StowsCarriedSlipper`, for the reach. */
  belt?: boolean;
};
type Seg = { at: number; fade: number; clip?: string; rate?: number; t0?: number; once?: boolean; abs?: number; pose?: Pose };
type P = { x: number; z: number };
type Key = [number, number, number];
type V = [number, number, number];

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
const rounded = (f: number, v: (g: number) => number, r = 6) => {
  let sum = 0;
  let w = 0;
  for (let o = -r; o <= r; o++) {
    const k = Math.exp(-0.5 * (o / (r / 2)) * (o / (r / 2)));
    sum += v(sameSide(f, f + o)) * k;
    w += k;
  }
  return sum / w;
};
const line = (t: number) => t;
const path = (f: number, pts: Key[], ease: (t: number) => number = line): P => {
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
/** The yaw that faces from one spot to another. */
export const yawTo = (a: P, b: P) => Math.atan2(b.x - a.x, a.z - b.z) / (Math.PI / 180);
const unwrapTo = (yaw: number, near: number) => yaw + 360 * Math.round((near - yaw) / 360);

// ------------------------------------------------------------------------------------ places

/** Her mark, on the throwing line's side of the court, left of the can from the hold's lens. */
export const POST = { x: -3.5, z: -6.3 };
/** Where she waits to walk on from. */
const WINGS = { x: -7.5, z: -6.3 };
/** Nemu's post by the can. */
export const GUARD = { x: 0.9, z: 1 };
/** Where the slipper comes to rest. */
export const SLIP_REST = { x: 5, z: 3 };
/** Where the can lands and is righted, and stays. */
export const CAN_DOWN = { x: 2.2, z: -0.6 };
/** Where Nemu waits for the next round, in the hold. */
export const HOLD_NEMU = { x: 1.2, z: -0.9 };
const SET = { x: 1.25, z: -0.75 };
/** The teleport: from the home stretch, across the lot. */
export const GONE = { x: 0.8, z: -6.6 };
export const ARRIVE = { x: -5.2, z: -2.4 };
/** Where she stops running and turns. Nemu comes up from the south, so from the west the two are side on, the viaduct behind. */
export const STAND = { x: -4.8, z: -4.2 };
const LUNGE_FROM = { x: -4.5, z: -6.3 };
const LUNGE_SLOW = { x: -4.55, z: -5.75 };
export const LUNGE_END = { x: -4.65, z: -5.2 };
/** Where Nemu lies, her head by Phaister's feet. */
export const FLAT = { x: -4.5, z: -5.1 };

const SCOOP_AT = { x: SLIP_REST.x + 0.5, z: SLIP_REST.z - 0.3 };
// Wide of the can and of Nemu, round the east side.
const RUN_OUT: Key[] = [[B.run, POST.x, POST.z], [B.run + 42, 5.6, -4.4], [B.run + 66, 6.9, 0], [B.scoop - 6, SCOOP_AT.x, SCOOP_AT.z]];
const CHASE: Key[] = [[B.chase, SCOOP_AT.x, SCOOP_AT.z], [B.chase + 39, 6.2, -2], [B.chase + 72, 4.2, -6], [B.blink, GONE.x, GONE.z]];
const BOLT: Key[] = [[B.bolt, ARRIVE.x, ARRIVE.z], [B.cutOff, -4.9, -5.2]];

const herPlace = (f: number): P => {
  if (f >= INTRO) return POST;
  if (f < B.walk) return WINGS;
  if (f < B.dare) return path(f, [[B.walk, WINGS.x, WINGS.z], [B.dare - 6, POST.x, POST.z]]);
  if (f < B.run) return POST;
  if (f < B.scoop - 6) return path(f, RUN_OUT);
  if (f < B.chase) return SCOOP_AT;
  if (f < B.blink) return path(f, CHASE);
  if (f < B.bolt) return ARRIVE;
  if (f < B.cutOff) return path(f, BOLT);
  // She skids and backs off a step from the hand.
  if (f < B.home) return path(f, [[B.cutOff, -4.9, -5.2], [B.turn, STAND.x, STAND.z]], outCubic);
  // Round Nemu, and home at a stroll: 3.8 m at 1.8 m/s.
  return path(f, [[B.home, STAND.x, STAND.z], [B.home + 30, -3.2, -5.3], [B.home + 63, POST.x, POST.z]]);
};
const headingOf = (pts: Key[]) => (f: number) => yawTo(path(f - 1, pts), path(f + 1, pts));
const chaseHeading = headingOf(CHASE);

// ------------------------------------------------------------------------------------ Phaister

const POINT: Pose = { torso: [-3, 8, 0], head: [-5, 8, 0], armL: [14, 0, 30], foreL: [0, -60, 0] };
const TWIRL: Pose = { torso: [-3, -6, 2], head: [-5, -8, 0], armR: [-112, 0, 30], foreR: [0, 42, 0], armL: [8, 0, 28], foreL: [0, -50, 0] };
const WINDUP: Pose = { root: [4, 0, 0], torso: [2, 24, 0], head: [-6, -22, 0], armR: [28, 0, 42], foreR: [0, 74, 0], armL: [-30, 0, 40] };
const FLICK: Pose = { root: [2, 0, 0], torso: [0, -24, 0], head: [-6, 18, 0], armR: [66, 0, 36], foreR: [0, 8, 0], armL: [-46, 0, 46] };
const TADA: Pose = { root: [-3, 0, 0], torso: [-5, 0, 0], head: [-5, 0, 0], armR: [-60, 0, 90], armL: [-60, 0, 90], foreR: [0, 22, 0], foreL: [0, -22, 0], legL: [-8, 0, 12], legR: [6, 0, -12] };
const LANDED: Pose = { root: [10, 0, 0], torso: [4, 0, 0], head: [-6, 0, 0], armR: [-20, 0, 74], armL: [-20, 0, 74], legL: [-44, 0, 14], legR: [18, 0, -14] };
const SMUG: Pose = { torso: [-3, 0, 2], head: [-7, 10, 3], armL: [-30, 0, 40], foreL: [0, -96, 0] };
const FRIGHT: Pose = { root: [-8, 0, 0], torso: [-8, 0, 0], head: [-6, 0, 0], armR: [-60, 0, 66], armL: [-60, 0, 66], foreR: [0, 50, 0], foreL: [0, -50, 0] };
/** Curse: Drain's reach, without the doll: a palm out at them on the lens's side, the slipper at her belt as in the game, her hat level so the face stays clear of the brim. */
const CURSE: Pose = { root: [-2, 0, 0], torso: [-2, -8, 0], head: [-2, 0, 0], armR: [8, 0, 22], foreR: [0, 24, 0], legL: [-14, 0, 12], legR: [14, 0, -12] };
/** The gulp goes up her arm: she rocks back on it. */
const GULPED: Pose = { root: [-6, 0, 0], torso: [-6, -8, 0], head: [-3, 0, 0], armR: [8, 0, 34], foreR: [0, 24, 0], legL: [-20, 0, 12], legR: [18, 0, -12] };
const JOY: Pose = { root: [-3, 0, 0], torso: [-7, 0, 0], head: [-8, 0, 0], armR: [-150, 0, 36], armL: [-150, 0, 36], foreR: [0, 30, 0], foreL: [0, -30, 0] };
/** ⚠️ A STRAIGHT ARM IS THE MODELS' OWN CLIP, HELD. Keyed from the hanging rest, these rigs' arms swing out sideways. */
const AIM_R = 'holding-right-shoot';
const AIM_L = 'holding-left-shoot';
const AIM_AT = 0.25;
const PI = (at: number, fade: number, pose?: Pose): Seg => ({ at, fade, clip: 'idle', abs: 0, pose });

const P_TRACK: Seg[] = [
  PI(0, 0, TWIRL),
  { at: B.walk - 3, fade: 6, clip: 'walk', rate: 1, pose: { head: [-5, 0, 0] } },
  PI(B.dare - 6, 8),
  { at: B.dare, fade: 8, clip: AIM_R, rate: 0, t0: AIM_AT, pose: POINT },
  PI(B.noLook - 6, 10, { head: [-5, 0, 0] }),
  PI(B.flick - 24, 16, WINDUP),
  { at: B.flick, fade: 3, clip: 'idle', abs: 0, pose: FLICK },
  { at: B.tada - 3, fade: 6, pose: TADA },
  { at: B.run - 3, fade: 4, clip: 'sprint', rate: 1.45 },
  { at: B.scoop - 8, fade: 4, clip: 'slide', rate: 1.2, once: true },
  { at: B.chase - 2, fade: 4, clip: 'sprint', rate: 1 },
  { at: B.blink, fade: 0, clip: 'hero-phaister-blink', rate: 1, once: true },
  { at: B.arrive - 1, fade: 2, pose: LANDED },
  PI(B.arrive + 8, 10, SMUG),
  { at: B.notice, fade: 3, pose: FRIGHT },
  { at: B.bolt - 2, fade: 4, clip: 'sprint', rate: 0.86 },
  { at: B.cutOff - 2, fade: 3, pose: FRIGHT },
  { at: B.turn - 2, fade: 12, clip: AIM_L, rate: 0, t0: AIM_AT, pose: CURSE },
  { at: B.gulp - 1, fade: 3, clip: AIM_L, rate: 0, t0: AIM_AT, pose: GULPED },
  { at: B.drained + 4, fade: 14, clip: 'idle', abs: 0, pose: { ...SMUG, armR: [-24, 0, 26], foreR: [0, 30, 0] } },
  PI(B.joy - 4, 6, JOY),
  { at: B.home - 3, fade: 8, clip: 'walk', rate: 1, pose: { head: [-5, 0, 0] } },
  PI(B.home + 58, 12, { head: [-4, 0, 0] }),
  PI(B.settled - 30, 28),
];

/** Facing the hold's lens, as the idle does. */
const HER_CALM = { hold: 0, ramp: [B.home + 60, B.home + 100] as [number, number] };
const NEMU_CALM = { hold: 2.6, move: [80, 240] as [number, number], phase: 1.7, ramp: [B.atCan, B.atCan + 26] as [number, number] };
const herLayers = (f: number) => (f >= B.home + 60 ? [...layersAt(f, P_TRACK), calmLayer(f, HER_CALM)] : layersAt(f, P_TRACK));
const IDLE_YAW = (f: number) => -8 + 2 * idleSin(f, 2, 0.4);
const herYawKeys = (f: number) => {
  if (f >= B.chase + 1 && f < B.blink) return unwrapTo(chaseHeading(f), 150);
  return kf(f, [
    [0, 20], [B.walk - 3, 20], [B.walk + 2, 90], [B.dare - 8, 90], [B.dare + 2, 140], [B.noLook - 6, 140], [B.noLook + 6, -8], [B.tada, -8],
    [B.run - 4, -8], [B.run + 3, 102], [B.run + 40, 102], [B.run + 46, 163], [B.run + 64, 163], [B.run + 70, 207], [B.scoop - 8, 207], [B.scoop, 170], [B.chase, 170],
    [B.blink, 300], [B.arrive, -100], [B.notice - 4, -100], [B.notice + 3, 90], [B.bolt, 90], [B.bolt + 4, 10], [B.cutOff, 10],
    [B.turn, -55], [B.drained + 8, -55], [B.joy, -20], [B.home - 2, -20], [B.home + 4, 60], [B.home + 30, 20], [B.home + 58, -10], [B.settled - 30, IDLE_YAW(INTRO - 30)], [B.settled, IDLE_YAW(INTRO - 30)],
  ]);
};

export const phaister = (f: number): Frame => {
  const at = f >= INTRO ? POST : { x: rounded(f, (g) => herPlace(g).x), z: rounded(f, (g) => herPlace(g).z) };
  const hop = kf(f, [[B.joy, 0], [B.joy + 6, 0.22, outQuad], [B.joy + 12, 0], [B.joy + 18, 0.16, outQuad], [B.joy + 24, 0]]);
  const held = f < B.flick || f >= B.scoop;
  const lit = f >= B.eyes && f < B.joy + 20;
  const glow = lit ? kf(f, [[B.eyes, 0], [B.eyes + 2, 0.6], [B.eyes + 4, 0.1], [B.eyes + 6, 1], [B.drained + 8, 1], [B.joy + 20, 0]]) : 0;
  return {
    x: at.x,
    z: at.z,
    // ⚠️ THE JOIN: the last second is the idle's own motion, unsmoothed, so nothing changes on the frame the clips swap.
    yaw: f >= INTRO - 30 ? IDLE_YAW(f) : rounded(f, herYawKeys),
    layers: f >= INTRO - 30 ? [{ clip: 'idle', t: (f - INTRO) / 30, w: 1 }, calmLayer(f, HER_CALM)] : herLayers(f),
    samples: f < INTRO - 30 ? samplesOf(f, herLayers) : undefined,
    lift: [0, hop, 0],
    face: glow > 0.02 ? 'glow' : 'rest',
    eyeGlow: glow,
    slipper: !held ? { at: 'none' } : f < B.walk ? { at: 'hand', swing: 0, twist: f * 44 } : { at: 'hand', swing: 5 * idleSin(f, 18) },
    visible: f >= B.blink && f < B.arrive - 2 ? 0 : 1,
    belt: f >= B.turn + 6 && f < B.drained + 10,
  };
};

// ------------------------------------------------------------------------------------ Nemu

const NEMU_PHASE = 2.6;
const NI = (at: number, fade: number, pose?: Pose): Seg => ({ at, fade, clip: 'idle', abs: NEMU_PHASE, pose });
const N_REACH: Pose = { root: [12, 0, 0], torso: [10, 0, 0], head: [-14, 0, 0],  };
/** Mid-stride in the slow part, the arm on the lens's side all the way out. */
const N_LUNGE: Pose = { root: [14, 0, 0], torso: [8, 0, 0], head: [-16, 0, 0], armL: [30, 0, 22], foreL: [0, -20, 0], legL: [-42, 0, 8], legR: [34, 0, -8] };
const N_GOING: Pose = { root: [34, 0, 0], torso: [18, 0, 0], head: [-24, 0, 0], armL: [8, 0, 16], legL: [-14, 0, 8], legR: [10, 0, -8] };
const N_FLAT: Pose = { root: [84, 0, 0], head: [-46, 0, 6], armR: [-150, 0, 20], armL: [-20, 0, 18], foreR: [0, 6, 0], foreL: [0, -6, 0], legL: [8, 0, 8], legR: [-10, 0, -8] };
const N_UP: Pose = { root: [40, 0, 0], torso: [14, 0, 0], head: [-16, 0, 0], armR: [-48, 0, 16], armL: [-48, 0, 16], foreR: [0, 20, 0], foreL: [0, -20, 0], legL: [-56, 0, 8], legR: [12, 0, -8] };

const N_TRACK: Seg[] = [
  NI(0, 0),
  { at: B.tada + 10, fade: 8, clip: 'walk', rate: 1 },
  { at: B.rights + 6, fade: 6, clip: 'pick-up', rate: 0.72, once: true },
  { at: B.rights + 44, fade: 6, clip: 'sprint', rate: 0.9, pose: { root: [6, 0, 0], head: [-6, 0, 0] } },
  { at: B.chase + 6, fade: 8, clip: 'sprint', rate: 1 },
  { at: B.reach - 3, fade: 4, clip: AIM_L, rate: 0, t0: AIM_AT, pose: N_REACH },
  { at: B.blink + 3, fade: 6, clip: AIM_L, rate: 0, t0: AIM_AT, pose: { ...N_REACH, head: [4, 0, 0] } },
  { at: B.blink + 12, fade: 5, clip: 'sprint', rate: 1 },
  { at: B.cutOff, fade: 4, clip: 'sprint', rate: 0.7, pose: { root: [8, 0, 0], head: [-14, 0, 0] } },
  { at: B.slow - 6, fade: 10, clip: AIM_R, rate: 0, t0: AIM_AT, pose: N_LUNGE },
  { at: B.hand, fade: 16, clip: AIM_R, rate: 0, t0: AIM_AT, pose: { ...N_LUNGE, root: [18, 0, 0] } },
  { at: B.drained - 1, fade: 4, pose: N_GOING },
  { at: B.drained + 7, fade: 6, pose: N_FLAT },
  { at: B.rise, fade: 20, pose: N_UP },
  { at: B.rise + 24, fade: 8, clip: 'walk', rate: 1 },
  NI(B.atCan - 6, 14),
];

const ROUND_CAN: Key[] = [[B.tada + 10, GUARD.x, GUARD.z], [B.rights + 6, SET.x, SET.z]];
const nemuRun = (f: number): P => {
  // Her heels: where Phaister was, closing as she lunges.
  const lag = kf(f, [[B.chase, 16], [B.reach - 18, 12], [B.reach, 7]]);
  const h = path(f - lag, f - lag < B.chase ? [[B.rights + 44, SET.x, SET.z], [B.chase, 4.4, 1.6]] : CHASE);
  return { x: h.x - 0.18, z: h.z + 0.28 };
};
const nemuPlace = (f: number): P => {
  if (f >= INTRO) return HOLD_NEMU;
  if (f < B.tada + 10) return GUARD;
  if (f < B.rights + 6) return path(f, ROUND_CAN);
  if (f < B.rights + 44) return SET;
  if (f < B.blink + 3) return nemuRun(Math.min(f, B.blink));
  const gone = nemuRun(B.blink);
  if (f < B.blink + 12) return gone;
  // She simply keeps coming, straight along the line for the corner Phaister has to pass.
  if (f < B.cutOff) return path(f, [[B.blink + 12, gone.x, gone.z], [B.cutOff, LUNGE_FROM.x, LUNGE_FROM.z - 0.2]]);
  if (f < B.slow) return path(f, [[B.cutOff, LUNGE_FROM.x, LUNGE_FROM.z - 0.2], [B.slow, LUNGE_FROM.x, LUNGE_FROM.z]]);
  if (f < B.gulp) return path(f, [[B.slow, LUNGE_FROM.x, LUNGE_FROM.z], [B.gulp, LUNGE_SLOW.x, LUNGE_SLOW.z]]);
  if (f < B.drained) return path(f, [[B.gulp, LUNGE_SLOW.x, LUNGE_SLOW.z], [B.drained, LUNGE_END.x, LUNGE_END.z]], outCubic);
  if (f < B.rise + 24) return path(f, [[B.drained, LUNGE_END.x, LUNGE_END.z], [B.drained + 8, FLAT.x, FLAT.z]]);
  return path(f, [[B.rise + 24, FLAT.x, FLAT.z], [B.atCan, HOLD_NEMU.x, HOLD_NEMU.z]]);
};
const nemuLayers = (f: number) => (f >= B.atCan ? [...layersAt(f, N_TRACK), calmLayer(f, NEMU_CALM)] : layersAt(f, N_TRACK));
const nemuYawKeys = (f: number) => {
  if (f >= B.rights + 44 && f < B.blink) {
    const a = nemuPlace(f - 1);
    const b = nemuPlace(f + 1);
    return Math.hypot(a.x - b.x, a.z - b.z) > 0.01 ? unwrapTo(yawTo(a, b), 140) : 140;
  }
  return kf(f, [
    [0, -6], [B.tada + 8, -6], [B.tada + 14, 60], [B.rights + 6, 90], [B.rights + 44, 100],
    [B.blink + 3, 150], [B.blink + 12, 262], [B.cutOff, 262], [B.cutOff + 8, 240], [B.drained, 240], [B.drained + 8, 178],
    [B.rise + 24, 178], [B.rise + 30, 130], [B.atCan - 6, 130], [B.atCan + 10, 354], [INTRO, 354],
  ]);
};

export const nemu = (f: number): Frame => {
  const at = f >= INTRO ? HOLD_NEMU : { x: rounded(f, (g) => nemuPlace(g).x), z: rounded(f, (g) => nemuPlace(g).z) };
  const thud = kf(f, [[B.drained + 5, 0.09], [B.drained + 8, 0.012], [B.drained + 11, 0.04], [B.drained + 14, 0.012]]) * (f >= B.drained + 5 && f < B.rise ? 1 : 0);
  return {
    x: at.x,
    z: at.z,
    yaw: f >= INTRO - 30 ? -6 + 1.5 * idleSin(f, 3, 1) : rounded(f, nemuYawKeys),
    layers: f >= INTRO - 30 ? [{ clip: 'idle', t: (f - INTRO) / 30 + NEMU_PHASE, w: 1 }, calmLayer(f, NEMU_CALM)] : nemuLayers(f),
    samples: f < INTRO - 30 ? samplesOf(f, nemuLayers) : undefined,
    lift: [0, thud, 0],
    face: 'rest',
    eyeGlow: 0,
    slipper: { at: 'none' },
    visible: 1,
  };
};

// ------------------------------------------------------------------------------------ the street's clock

/** The street slows with the climax: cars, and the train over them. */
export const streetRate = (f: number) => (f >= B.slow && f < B.gulp ? 0.12 : 1);

// ------------------------------------------------------------------------------------ Kuro

/** His size in the game: bound at `CharacterVisual.PersonScale`. */
const KURO_SCALE = 2.38;
/** `HomeFilmPlateRecorder.Pet.Faces`. */
const REST = 0;
const OH = 1;
const HAPPY = 2;
const DIZZY = 3;
const SHY = 4;
const kuroFace = (f: number) => {
  if (f >= INTRO) { const q = ph(f); return q >= 300 && q < 360 ? HAPPY : REST; }
  if (f < B.nemu) return f >= 40 ? OH : REST;
  if (f >= B.nothing && f < B.noLook) return HAPPY;
  if (f >= B.klang && f < B.canDown + 14) return OH;
  if (f >= B.canDown + 14 && f < B.tada + 8) return DIZZY;
  if (f >= B.blink && f < B.notice) return OH;
  if (f >= B.slow && f < B.drained + 10) return OH;
  if (f >= B.drained + 10 && f < B.back) return SHY;
  if (f >= B.back && f < B.atCan) return HAPPY;
  return REST;
};
/** Where he floats: at her shoulder, except when he has gone to look at something. */
export const kuroAt = (f: number): V => {
  const n = nemu(f);
  const near: V = [n.x + 0.85, 1.7 + 0.07 * idleSin(f, 8), n.z - 0.18];
  // At the start he is down at the can, looking into it, and comes back up to her.
  const can: V = [0.42, 0.55 + 0.03 * idleSin(f, 8), -0.42];
  const flung: V = [near[0] + 0.5, near[1] + 0.6, near[2] + 0.1];
  const atFlat: V = [FLAT.x + 0.7, 0.75 + 0.04 * idleSin(f, 8), FLAT.z - 0.55];
  const mix = (a: V, b: V, u: number) => a.map((v, i) => v + (b[i] - v) * u) as V;
  let p = mix(can, near, easeInOut(clamp01((f - (B.nemu - 16)) / 16)));
  if (f >= B.klang) p = mix(p, flung, kf(f, [[B.klang, 0], [B.klang + 6, 1, outQuad], [B.tada, 1], [B.tada + 16, 0]]));
  if (f >= B.drained) p = mix(p, atFlat, kf(f, [[B.drained + 6, 0], [B.drained + 24, 1], [B.rise + 20, 1], [B.rise + 44, 0]]));
  return p;
};
const kuroYaw = (f: number) => {
  if (f >= INTRO - 30) return -6;
  if (f < B.nemu) return kf(f, [[0, 20], [B.nemu - 16, 20], [B.nemu, -6]]);
  return nemu(f).yaw;
};
export const kuroRow = (f: number): number[] => {
  const p = kuroAt(f);
  const tilt = f < B.nemu - 16 ? 12 : 0;
  const swell = f >= B.klang && f < B.tada ? 1.14 : 1;
  return [p[0], p[1] - 0.45, p[2], kuroYaw(f), tilt, 0, KURO_SCALE * swell, kuroFace(f)];
};

// ------------------------------------------------------------------------------------ her moths

/** Twenty moths, each its own row as in `PhaisterSwarm`: a burst direction, a ribbon phase and radius, a height to knit at. */
const SWARM = Array.from({ length: 20 }, (_, i) => {
  const a = (i * 137.5 * Math.PI) / 180;
  return { bx: Math.cos(a) * (0.7 + (i % 3) * 0.2), bz: Math.sin(a) * (0.7 + ((i + 1) % 3) * 0.2), by: 0.5 + (i % 5) * 0.28, ph: a, rr: 0.18 + (i % 4) * 0.05, knit: 0.3 + (i % 7) * 0.2, hz: 3.6 + (i % 5) * 0.55, size: 2.0 + (i % 3) * 0.28, late: (i % 5) * 0.6 };
});
const flap = (f: number, hz: number, ph0: number) => 38 * Math.sin((f / 30) * hz * 6.283 + ph0);
const moth = (x: number, y: number, z: number, yaw: number, wing: number, size: number) => [x, y, z, yaw, -18, wing, size];
const lerp = (a: number, b: number, t: number) => a + (b - a) * t;

/** A few moths, rarely, on the hold's 24 s clock: from her sleeve, round her, and back. Nothing else happens on the hold. */
const IDLE_MOTHS = [{ at: 170, len: 110, n: 2 }, { at: 520, len: 100, n: 3 }];

export const mothRows = (f: number): number[] => {
  const out: number[] = [];
  if (f >= INTRO) {
    const q = ph(f);
    for (const e of IDLE_MOTHS) {
      const t = (q - e.at) / e.len;
      if (t < 0 || t >= 1) continue;
      for (let i = 0; i < e.n; i++) {
        const k = Math.sin(Math.PI * t);
        const a = t * 6.283 * 1.2 + i * 2.1;
        out.push(...moth(POST.x + Math.cos(a) * 0.72 * k + 0.4 * (1 - k), 0.85 + 0.75 * k + 0.12 * Math.sin(a * 2), POST.z + Math.sin(a) * 0.72 * k - 0.1, (a * 57.3 + 90) % 360, flap(q, 5, i), 2.0));
      }
    }
    return out;
  }
  const from = GONE;
  const to = ARRIVE;
  const start = B.blink;
  const end = B.arrive + 8;
  if (f < start || f >= end + 30) return out;
  const yaw = yawTo(from, to);
  for (let i = 0; i < SWARM.length; i++) {
    const m = SWARM[i];
    const t = (f - start - m.late) / (end - start);
    if (t < 0) continue;
    if (t >= 1) {
      // The stragglers settle on her, beat slowly and creep into her sleeves.
      if (i < 5) {
        const q = (f - end) / 30;
        const a = i * 1.26;
        out.push(...moth(to.x + Math.cos(a) * 0.5 * (1 - q), 1.45 - 0.4 * q + i * 0.08, to.z + Math.sin(a) * 0.5 * (1 - q), a * 57.3, flap(f, 2.4, i), m.size * (1 - 0.5 * q)));
      }
      continue;
    }
    // Out of her body along its own line, along the aim low in a turning ribbon, and spiralling up into her at its own height.
    const burst = clamp01(t / 0.24);
    const stream = clamp01((t - 0.18) / 0.52);
    const knit = clamp01((t - 0.62) / 0.38);
    const bx = from.x + m.bx * outQuad(burst);
    const bz = from.z + m.bz * outQuad(burst);
    const by = 0.5 + m.by * outQuad(burst);
    const ang = m.ph + stream * 9;
    let x = lerp(bx, to.x, easeInOut(stream)) + Math.cos(ang) * m.rr * 1.6 * Math.sin(Math.PI * stream);
    let z = lerp(bz, to.z, easeInOut(stream)) + Math.sin(ang) * m.rr * 1.6 * Math.sin(Math.PI * stream);
    let y = lerp(by, 0.95, stream);
    if (knit > 0) {
      const r = 0.9 * (1 - knit) + 0.1;
      const a = m.ph + knit * 7;
      x = lerp(x, to.x + Math.cos(a) * r, knit);
      z = lerp(z, to.z + Math.sin(a) * r, knit);
      y = lerp(y, m.knit + 0.3, knit);
    }
    out.push(...moth(x, y, z, yaw + ang * 10, flap(f, m.hz, m.ph), m.size));
  }
  return out;
};

// ------------------------------------------------------------------------------------ the slipper and the can

const CAN_HIT: V = [0, 0.42, 0];
export const STOP = 8;
export type Fly = { p: V; rot: V };
/** Round her back, and a low hard line to the can. */
const throwAt = (u: number): V => {
  const a: V = [POST.x - 0.2, 1.1, POST.z + 0.3];
  const c: V = [-3.2, 2.3, -3.2];
  const v = 1 - u;
  return [0, 1, 2].map((i) => v * v * a[i] + 2 * v * u * c[i] + u * u * CAN_HIT[i]) as V;
};
export const flyingAt = (f: number): Fly | null => {
  if (f < B.flick || f >= B.scoop) return null;
  if (f < B.klang) return { p: throwAt((f - B.flick) / (B.klang - B.flick)), rot: [(f - B.flick) * 50, 80, 0] };
  if (f < B.klang + STOP) return { p: CAN_HIT, rot: [30, 120, 0] };
  const land = B.klang + STOP + 30;
  if (f < land) {
    // It kicks off the can and bounces once.
    const u = (f - B.klang - STOP) / 30;
    const up = u < 0.7 ? 1.7 * 4 * (u / 0.7) * (1 - u / 0.7) : 0.3 * 4 * ((u - 0.7) / 0.3) * (1 - (u - 0.7) / 0.3);
    return { p: [CAN_HIT[0] + (SLIP_REST.x - CAN_HIT[0]) * u, 0.1 + up, CAN_HIT[2] + (SLIP_REST.z - CAN_HIT[2]) * u], rot: [(f - B.klang) * 24 * (1 - u), 40 * u, 0] };
  }
  return { p: [SLIP_REST.x, 0.06, SLIP_REST.z], rot: [0, 40, 0] };
};

export type CanFrame = { x: number; z: number; hop: number; rot: V; lying: number };
/** ⚠️ NOTHING MOVES THE CAN BUT A BLOW OR A HAND. It lands by Nemu; her hand rights it where it lies, and there it stays. */
export const canAt = (f: number): CanFrame => {
  const at = B.klang + STOP;
  if (f < at) return { x: 0, z: 0, hop: 0, rot: [0, 0, 0], lying: 0 };
  const air = B.canDown - at;
  if (f < at + air) {
    const u = (f - at) / air;
    return { x: CAN_DOWN.x * u, z: CAN_DOWN.z * u, hop: 3.2 * 4 * u * (1 - u), rot: [0, 60 * u, 900 * outQuad(u)], lying: u };
  }
  const up = B.rights + 20;
  if (f < up) {
    const r = clamp01((f - at - air) / 14);
    return { x: CAN_DOWN.x, z: CAN_DOWN.z, hop: 0.06 * Math.max(0, 1 - r * 2), rot: [0, 60 + 30 * outCubic(r), 90], lying: 1 };
  }
  const u = easeInOut(clamp01((f - up) / 16));
  return { x: CAN_DOWN.x, z: CAN_DOWN.z, hop: 0, rot: [0, 90 * (1 - u), 90 * (1 - u)], lying: 1 - u };
};
