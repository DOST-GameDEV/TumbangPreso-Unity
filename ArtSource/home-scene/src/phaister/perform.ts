import { Face, Pose, SlipperState, addPose, mixPose } from '../three/actor';
import { Ease, inCubic, inQuad, kf, Key, linear, outBack, outCubic, outQuad } from '../lib/kf';
import { easeInOut, env, LOOP, loopNoise, loopSin, s } from './time';
import { COVEN, PB } from './beats';
import { HER_MARK, PAD } from './map';

/**
 * Phaister's performance, frame by frame, KEYED BY HAND on her real rig.
 *
 * ⚠️⚠️ NO GAME CLIPS. 🧑 2026-09-24: *"make ur own animation"*, *"zack had his own animation made"*, *"it
 * wont be as dynamic if u js reuse animations she has"*. Her glb carries sprint, slide and her three
 * casts, and none of them is used: every pose below is authored for this loop, the way Zack's were.
 *
 * ⚠️ IT IS A POSE TRACK, NOT A PILE OF IF-BLOCKS. The first version switched between per-beat functions
 * and had to hand-match every join. Here each channel (pose, place, turn, lift) is a list of keys with
 * the ease into each, so every hand-over is continuous by construction (method § 4.1), and cycles (the
 * run, the walk, breath) are layered ON TOP of the track rather than replacing it.
 *
 * ASTRA.md 1F still directs her: *"SHE PERFORMS"*. Big gestures pass through an off-axis flourish and
 * finish front-on, chest open, held; the blink is the one move with no flourish at all.
 *
 * Rig conventions, measured on the boards (docs/HOME_SCREEN_ANIMATION_METHOD.md § 2.2): an arm's x
 * negative swings it forward and up, z positive lifts it out to the side, y swings it across; head x
 * negative lifts the chin, head y positive turns her face to screen right; torso and root x positive
 * lean forward. ⚠️ Her hat: bent or leaned back past about 20 degrees the brim's black underside becomes
 * a slab over the whole figure, and arms raised past about 45 degrees in front vanish behind the hat,
 * so every raised arm here goes up in a V to the side.
 */

export type HerFrame = {
  x: number;
  z: number;
  /** Metres off the road (the float). */
  y: number;
  /** World yaw: 0 faces SOUTH, +90 faces east (view.ts `apparent`). */
  yaw: number;
  pose: Pose;
  lift: [number, number, number];
  face: Face;
  eyeGlow: number;
  visible: number;
  flash: number;
  fold: number;
  slipper: SlipperState;
};

// ------------------------------------------------------------------------------------ key poses

/** Weight on her left leg, chest up, chin a touch high, the tsinelas hanging from her right hand. */
const STANCE: Pose = { root: [0, 0, 1.5], torso: [-3, 10, -2], head: [-5, -6, 4], armR: [6, 0, 8], armL: [-2, 4, 16], legL: [0, 0, 7], legR: [3, 0, -5] };
/**
 * *That one.* Facing US, she jerks the tsinelas back over her shoulder at Nemu down the court, chin up:
 * the showwoman introduces her mark to the audience rather than turning her back on it.
 */
const POINT: Pose = { root: [0, 0, 3], torso: [-4, -16, 0], head: [-8, -18, 6], armR: [58, 10, 34], armL: [-6, 0, 26], legL: [0, 0, 8], legR: [4, 0, -6] };
/**
 * *No looking.* The brim pulled down over her eyes with her free hand, head bowed so the brim covers them
 * from the lens, the tsinelas hanging from the other hand. The whole trick is that she cannot see the can.
 */
const BLIND: Pose = { root: [0, 0, 2], torso: [3, 6, 0], head: [14, -4, 3], armR: [8, 0, 10], armL: [-44, 0, 128], legL: [0, 0, 8], legR: [3, 0, -6] };
/** The twirl: the tsinelas spun on one finger at her hip, eyes still under the brim, weight settled. */
const TWIRL: Pose = { root: [0, 0, 2], torso: [2, 10, 0], head: [14, -8, 4], armR: [-34, 10, 22], armL: [-8, 0, 20], legL: [0, 0, 8], legR: [3, 0, -6] };
/** The wind-up for the behind-the-back flick: throwing hand forward and low, chest turned to it. */
const BACKWIND: Pose = { root: [2, 0, 0], torso: [6, 16, 0], head: [14, -6, 0], armR: [-38, 0, 12], armL: [-10, 0, 30], legL: [-6, 0, 6], legR: [8, 0, -6] };
/** The flick: the arm whipped back behind her, the chest turning after it, her head never turning. */
const BACKFLICK: Pose = { root: [-2, 0, 0], torso: [-4, -18, 0], head: [12, 8, 0], armR: [72, 0, 16], armL: [-18, 0, 34], legL: [4, 0, 6], legR: [-6, 0, -6] };
/** Arms folded, chin up, brim still low: not watching her own trick land. */
const SMUG: Pose = { root: [0, 0, 3], torso: [-4, 4, -2], head: [4, -12, 9], armR: [-42, 46, -4], armL: [-42, -46, -4], legL: [0, 0, 8], legR: [4, 0, -6] };
/** The launch into the run: low, leaning in. */
const DRIVE: Pose = { root: [18, 0, 0], torso: [8, 0, 0], head: [-18, 0, 0], armR: [-40, 0, 12], armL: [40, 0, 12], legL: [-40, 0, 2], legR: [30, 0, -2] };
/** The slide onto the pad: front leg out, back leg tucked, reaching down for the slipper. */
const SLIDE: Pose = { root: [4, 0, 0], torso: [16, -10, 0], head: [-10, 0, 0], armR: [-58, 10, 26], armL: [-10, 0, 62], legL: [-72, 0, 6], legR: [44, 0, -6] };
/** Plant and turn: she sees Nemu already in the lane. A small recoil, arms out for balance. */
const RECOIL: Pose = { root: [-4, 0, 0], torso: [-6, 0, 0], head: [-4, 0, 0], armR: [-20, 0, 46], armL: [-20, 0, 46], legL: [-10, 0, 10], legR: [8, 0, -10] };
/** Ready to break either way: knees in, arms wide, low. */
const READY: Pose = { root: [10, 0, 0], torso: [6, 0, 0], head: [-12, 0, 0], armR: [-16, 0, 58], armL: [-16, 0, 58], legL: [-22, 0, 14], legR: [-22, 0, -14] };
/** Folded into the blink: crouched, arms crossed in (ASTRA.md: "collapse inward"). */
const FOLD: Pose = { root: [12, 0, 0], torso: [18, 0, 0], head: [12, 0, 0], armR: [-40, 40, -8], armL: [-40, -40, -8], legL: [-30, 0, 4], legR: [-30, 0, -4] };
/** Thrown open front-on, held for the room. */
const OPEN: Pose = { root: [-2, 0, 0], torso: [-6, 0, 0], head: [-6, 0, 0], armR: [-12, 0, 118], armL: [-12, 0, 118], legL: [0, 0, 11], legR: [0, 0, -11] };
/** *Seriously?* Shoulders drop, the head tips over. */
const TILT: Pose = { root: [0, 0, 2], torso: [2, 0, -3], head: [4, -10, 17], armR: [4, 0, 5], armL: [4, 0, 5], legL: [0, 0, 6], legR: [0, 0, -4] };
/** Brim down: a hand to the brim's edge, out at her side (in front, the arm covers her face). */
const BRIM: Pose = { root: [0, 0, 0], torso: [4, -4, 0], head: [12, -6, 0], armR: [-44, 0, 128], armL: [0, 0, 14], legL: [0, 0, 6], legR: [0, 0, -6] };
/** Floating: legs together and a little bent, arms low and out, palms to the road. */
const HOVER: Pose = { root: [0, 0, 0], torso: [-2, 0, 0], head: [-4, 0, 0], armR: [-8, 0, 40], armL: [-8, 0, 40], legL: [10, 0, 3], legR: [16, 0, -3] };
/** Grand Coven: arms thrown up in a V, chest open. Chin level: a lean back makes the brim a slab. */
const SKY: Pose = { root: [0, 0, 0], torso: [-4, 0, 0], head: [2, 0, 0], armR: [-28, 0, 124], armL: [-28, 0, 124], legL: [8, 0, 5], legR: [14, 0, -5] };
/** The stamp: both arms driven down and out at the circle. */
const STAMP: Pose = { root: [6, 0, 0], torso: [12, 0, 0], head: [6, 0, 0], armR: [-26, 0, 64], armL: [-26, 0, 64], legL: [-6, 0, 8], legR: [-6, 0, -8] };
/** *Well?* Palms up, a tilt, eyebrows she does not have. */
const WELL: Pose = { root: [0, 0, 0], torso: [-4, 0, 3], head: [-2, 0, -12], armR: [-38, -12, 50], armL: [-38, -12, 50], legL: [6, 0, 5], legR: [12, 0, -5] };
/** The shrug and the tip of the hat to Kuro: *I'll take it.* */
const TIP: Pose = { root: [0, 0, 0], torso: [6, -6, 0], head: [12, -10, 8], armR: [-44, 0, 132], armL: [-26, -10, 48], legL: [6, 0, 5], legR: [12, 0, -5] };

/** *Watch this.* A hand flat on her chest, chin up, to us. */
const CHEST: Pose = { root: [0, 0, 2], torso: [-5, 0, 0], head: [-9, -4, 7], armR: [-40, 42, -2], armL: [-4, 0, 30], legL: [0, 0, 8], legR: [4, 0, -6] };
/** The flourish, off-axis, before she finishes front-on (ASTRA.md 1F): one arm swept wide, one across, a leg back, a small dip. */
const FLOURISH: Pose = { root: [5, 0, 0], torso: [10, -12, 0], head: [2, 12, 4], armR: [-22, 0, 98], armL: [-34, 34, 18], legL: [-4, 0, 6], legR: [18, 0, -4] };
/** Nothing. The shoulders go. */
const SLUMP: Pose = { root: [3, 0, 0], torso: [9, 0, 0], head: [10, 6, 0], armR: [4, 0, 2], armL: [4, 0, 2], legL: [0, 0, 5], legR: [0, 0, -5] };
/** A stamp of frustration: fists down and back, knees in. */
const STOMP: Pose = { root: [8, 0, 0], torso: [8, 0, 0], head: [8, 0, -8], armR: [22, 0, 12], armL: [22, 0, 12], legL: [-22, 0, 6], legR: [8, 0, -6] };
/** The huff: arms folded, face turned away. */
const HUFF: Pose = { root: [0, 0, 2], torso: [-3, -10, 0], head: [-6, -32, 6], armR: [-42, 46, -4], armL: [-42, -46, -4], legL: [0, 0, 8], legR: [4, 0, -6] };
/** Knuckles: both hands meeting in front of her chest. */
const KNUCK: Pose = { root: [2, 0, 0], torso: [5, 0, 0], head: [8, 0, 0], armR: [-64, 38, 6], armL: [-64, -38, 6], legL: [0, 0, 9], legR: [0, 0, -9] };
/** A clap, and the hands thrown open after it. */
const CLAP: Pose = { root: [0, 0, 0], torso: [-4, 0, 0], head: [-8, 0, 0], armR: [-60, 32, 8], armL: [-60, -32, 8], legL: [6, 0, 5], legR: [12, 0, -5] };
const CLAPOPEN: Pose = { root: [0, 0, 0], torso: [-6, 0, 0], head: [-10, 0, -6], armR: [-50, -6, 34], armL: [-50, -6, 34], legL: [6, 0, 5], legR: [12, 0, -5] };

// ------------------------------------------------------------------------------------ tracks

type PoseKey = [frame: number, pose: Pose, ease?: Ease];
const poseTrack = (f: number, keys: PoseKey[]): Pose => {
  if (f <= keys[0][0]) return keys[0][1];
  for (let i = 1; i < keys.length; i++) {
    const [f1, p1, e] = keys[i];
    const [f0, p0] = keys[i - 1];
    if (f <= f1) return mixPose(p0, p1, (e ?? easeInOut)(f1 === f0 ? 1 : (f - f0) / (f1 - f0)));
  }
  return keys[keys.length - 1][1];
};

const P = PB;
const M = HER_MARK;
/** Where the tsinelas skids to, on the overclock pad. */
export const PAD_SLIPPER = { x: PAD.x + 0.1, z: PAD.z - 0.4 };
/** Where she runs to and slides. */
const SCOOP = { x: PAD.x + 0.25, z: PAD.z - 0.6 };
/** Where Nemu's hand closes on nothing. */
export const TAG = { x: -8.4, z: -3.0 };
/** Where the blink throws her open: across the south line, 5.8 m away. */
export const SAFE = { x: -6.0, z: -8.45 };
/** The middle of the line, where she casts. */
export const CAST = { x: 0.0, z: -8.4 };

const YAW_CALM = 24;

/** Her pose track, all of it: the loop told in key poses. */
const track = (f: number): Pose =>
  poseTrack(f, [
    [0, STANCE],
    [P.dare - 2, STANCE],
    [P.dare + 12, POINT, outBack],
    [P.boast - 6, POINT],
    [P.boast + 8, CHEST],
    [P.boast + 22, CHEST],
    [P.boast + 34, FLOURISH, outBack],
    [P.blind - 4, FLOURISH],
    [P.blind + 12, BLIND, outBack],
    [P.twirl - 6, BLIND],
    [P.twirl + 8, TWIRL],
    [P.release - 10, TWIRL],
    [P.release - 3, BACKWIND, outQuad],
    [P.release + 2, BACKFLICK, outCubic],
    [P.release + 9, BACKFLICK],
    [P.release + 20, SMUG],
    [P.klang, SMUG],
    [P.tada + 6, OPEN, outBack],
    [P.sag - 4, OPEN],
    [P.sag + 14, SLUMP],
    [P.run - 4, SLUMP],
    [P.run + 4, DRIVE, outQuad],
    [P.scoop - 4, DRIVE],
    [P.scoop + 3, SLIDE, outCubic],
    [P.scoop + 13, SLIDE],
    [P.block, RECOIL],
    [P.warn + 6, READY],
    [P.tag - 2, READY],
    [P.tag + 4, FOLD, inCubic],
    [P.safe, FOLD],
    [P.safe + 6, OPEN, outBack],
    [P.deadpan + 6, OPEN],
    [P.deadpan + 18, TILT],
    [P.stomp - 2, TILT],
    [P.stomp + 5, STOMP, outQuad],
    [P.stomp + 12, STOMP],
    [P.stomp + 22, HUFF],
    [P.resolve, HUFF],
    [P.resolve + 10, BRIM],
    [P.knuckles - 4, BRIM],
    [P.knuckles + 6, KNUCK],
    [P.knuckles + 20, KNUCK],
    [P.knuckles + 30, STANCE],
    [P.rise, STANCE],
    [P.rise + 16, HOVER],
    [P.sky - 4, HOVER],
    [P.sky + 4, SKY, outBack],
    [P.stamp - 6, SKY],
    [P.stamp, STAMP, inQuad],
    [P.stamp + 10, STAMP],
    [P.well, SKY],
    [P.well + 8, WELL],
    [P.take + 4, WELL],
    [P.clap - 4, CLAPOPEN],
    [P.clap, CLAP, inQuad],
    [P.clap + 5, CLAPOPEN, outQuad],
    [P.clap + 10, CLAP, inQuad],
    [P.clap + 16, CLAPOPEN, outBack],
    [P.tipK, CLAPOPEN],
    [P.tipK + 8, TIP],
    [P.tipK + 26, TIP],
    [P.descend + 6, HOVER],
    [P.descend + 40, STANCE],
  ]);

/** Where she is on the road. */
const place = (f: number): [number, number] => {
  // The run: out of the blocks with a kick, flat out, into the slide.
  if (f >= P.run && f < P.scoop + 14) {
    const u = kf(f, [[P.run, 0], [P.run + 6, 0.02, inQuad], [P.scoop, 0.94, linear], [P.scoop + 14, 1, outQuad]]);
    return [M.x + (SCOOP.x - M.x) * u, M.z + (SCOOP.z - M.z) * u];
  }
  if (f >= P.scoop + 14 && f < P.tag + 1) {
    const zz = kf(f, [[P.block, SCOOP.z], [P.warn, SCOOP.z - 0.3], [P.juke, 1.2, easeInOut], [P.tag, TAG.z, outQuad]]);
    const juke = env(f, P.juke, P.juke + 6, P.tag - 8, P.tag) * 0.55 * Math.sin(((f - P.juke) / 9) * Math.PI);
    const xx = kf(f, [[P.block, SCOOP.x], [P.juke, -8.8], [P.tag, TAG.x]]) + juke;
    return [xx, zz];
  }
  if (f >= P.tag + 1 && f < P.blink + 12) return [TAG.x, TAG.z];
  if (f >= P.blink + 12 && f < P.knuckles + 26) return [SAFE.x, SAFE.z];
  if (f >= P.knuckles + 26 && f < P.descend) {
    const u = kf(f, [[P.knuckles + 26, 0], [P.rise, 1, easeInOut]]);
    return [SAFE.x + (CAST.x - SAFE.x) * u, SAFE.z + (CAST.z - SAFE.z) * u];
  }
  if (f >= P.descend && f < P.descend + 50) {
    const u = kf(f, [[P.descend, 0], [P.descend + 48, 1, easeInOut]]);
    return [CAST.x + (M.x - CAST.x) * u, CAST.z + (M.z - CAST.z) * u];
  }
  return [M.x, M.z];
};

/** The run heading, world yaw: from her mark to the pad. */
const RUN_YAW = 180 - (Math.atan2(SCOOP.x - M.x, SCOOP.z - M.z) * 180) / Math.PI;

const yawAt = (f: number) =>
  kf(f, [
    [0, YAW_CALM],
    [P.dare - 2, YAW_CALM],
    [P.dare + 12, YAW_CALM + 12, outBack],
    [P.boast - 6, YAW_CALM + 12],
    [P.boast + 8, YAW_CALM - 4],
    [P.boast + 22, YAW_CALM - 4],
    [P.boast + 34, YAW_CALM + 30],
    [P.blind + 8, YAW_CALM + 6],
    [P.release - 3, YAW_CALM + 14, outQuad],
    [P.release + 4, YAW_CALM - 8, outCubic],
    [P.sag, YAW_CALM - 8],
    // She turns and looks back up the court at Nemu, over her shoulder.
    [P.sag + 14, -168, easeInOut],
    [P.run - 2, -168],
    [P.run + 5, RUN_YAW - 360, outQuad],
    [P.scoop + 13, RUN_YAW - 360],
    [P.block, 0, easeInOut],
    [P.juke, 0],
    [P.juke + 8, 18],
    [P.juke + 17, -18],
    [P.tag - 4, 0],
    [P.safe, -28],
    [P.resolve, -28],
    [P.resolve + 12, 100],
    [P.knuckles + 20, 100],
    [P.knuckles + 26, 90, easeInOut],
    [P.rise - 4, 90],
    [P.rise + 16, 180, easeInOut],
    [P.descend + 6, 180],
    [P.descend + 44, YAW_CALM, easeInOut],
  ]);

/** Metres off the road. */
const floatAt = (f: number) =>
  kf(f, [
    [P.rise, 0],
    [P.rise + 22, 1.35, easeInOut],
    [P.sky, 1.45],
    [P.stamp, 1.6, outQuad],
    [P.stamp + 6, 1.45, outBack],
    [P.descend, 1.45],
    [P.descend + 40, 0, easeInOut],
  ]) + (f > P.rise + 10 && f < P.descend + 36 ? 0.05 * Math.sin((f - P.rise) * 0.11) * env(f, P.rise + 10, P.rise + 30, P.descend, P.descend + 36) : 0);

// ------------------------------------------------------------------------------------ cycles

/** Her run: long reach, arms pumping across her (hers, not a copy of anyone's), a bob per contact. */
const runCycle = (f: number, period: number, amp: number): { pose: Pose; lift: [number, number, number] } => {
  const p = (2 * Math.PI * f) / period;
  const sn = Math.sin(p);
  return {
    pose: {
      root: [4 * amp, 0, 3 * sn * amp],
      torso: [3 * amp, 7 * sn * amp, 0],
      head: [-3 * amp, -5 * sn * amp, 0],
      legL: [-54 * sn * amp, 0, 0],
      legR: [54 * sn * amp, 0, 0],
      armL: [48 * sn * amp, -14 * sn * amp, 0],
      armR: [-48 * sn * amp, -14 * sn * amp, 0],
    },
    lift: [0, 0.035 * Math.abs(Math.cos(p)) * amp, 0],
  };
};

/** A brisk walk, shoulders rolling: she has decided something. */
const walkCycle = (f: number, amp: number): { pose: Pose; lift: [number, number, number] } => {
  const p = (2 * Math.PI * f) / 19;
  const sn = Math.sin(p);
  return {
    pose: { torso: [0, 6 * sn * amp, 0], legL: [-30 * sn * amp, 0, 0], legR: [30 * sn * amp, 0, 0], armL: [16 * sn * amp, 0, 0], armR: [-16 * sn * amp, 0, 0] },
    lift: [0, 0.014 * Math.abs(Math.cos(p)) * amp, 0],
  };
};

/** Breath and weight, periodic on the loop so the seam cannot show. */
const alive = (f: number): { pose: Pose; lift: [number, number, number] } => {
  const breath = loopSin(f, 10);
  const weight = loopSin(f, 3, 0.7);
  return {
    pose: {
      root: [0, 0, weight * 1.4],
      torso: [1.2 * breath, 2.2 * weight, -weight],
      head: [-1 * breath + 2 * loopNoise('phn', f, 1.4, 2), 4 * loopNoise('phy', f, 1.2, 2, 3), 1.5 * weight],
      armR: [1.5 * breath, 0, 2 * (breath + 1) / 2],
      armL: [-1.5 * breath, 0, 2 * (breath + 1) / 2],
    },
    lift: [0.003 * weight, 0.003 * (breath + 1) / 2, 0],
  };
};

// ------------------------------------------------------------------------------------ the calm

/**
 * ⚠️ THE IDLE'S HEARTBEAT: SHE DOODLES. The way a card magician shuffles without looking, she draws a
 * small sigil in the air with her left hand, idly, and lets it fade. Whole cycles only, inside calm
 * stretches, on a fixed grid so the loop seam lands between two doodles.
 */
export const DOODLE = 96;
const BUSY: [number, number][] = [
  [P.dare - 10, P.descend + 60],
  [P.hat - 6, P.hat + 40],
  [P.train2 - 6, P.train2 + 52],
  [P.float - 6, P.float + 70],
  [P.dawn + 60, LOOP + 1],
];
const calm = (a: number, b: number) => BUSY.every(([x, y]) => b <= x || a >= y) && b <= LOOP;
export const doodle = (f: number) => {
  const c0 = Math.floor(f / DOODLE) * DOODLE;
  if (!calm(c0, c0 + 70)) return null;
  const t = f - c0;
  const up = env(t, 0, 12, 44, 60);
  const th = kf(t, [[12, 0], [44, 1, easeInOut]]) * Math.PI * 2;
  return { t, up, th, drawn: kf(t, [[12, 0], [44, 1, easeInOut]]), fade: env(t, 12, 20, 50, 70) };
};

/** The calm's showmanship, layered on the stance: hat tip, holding the hat in a train's draught, the float. */
const calmLayer = (f: number): Pose | undefined => {
  const d = doodle(f);
  if (d) return { armL: [(-24 + 12 * Math.sin(d.th)) * d.up, 0, (50 + 12 * Math.cos(d.th)) * d.up], head: [4 * d.up, 10 * d.up, 0], torso: [0, 4 * d.up, 0] };
  if (f >= P.hat && f < P.hat + 36) {
    const up = env(f, P.hat, P.hat + 8, P.hat + 24, P.hat + 34);
    const nod = env(f, P.hat + 8, P.hat + 13, P.hat + 18, P.hat + 26);
    return { armR: [-44 * up, 0, 124 * up], head: [14 * nod - 2 * up, -10 * up, 8 * nod], torso: [8 * nod, -6 * up, 0] };
  }
  if (f >= P.train2 && f < P.train2 + 48) {
    const hold = env(f, P.train2 + 6, P.train2 + 14, P.train2 + 34, P.train2 + 46);
    const buffet = loopNoise('buf', f, 12) * hold;
    return { armL: [-44 * hold, 0, 126 * hold], head: [8 * hold + 2 * buffet, -4 * hold, 3 * buffet], torso: [6 * hold, 0, 0] };
  }
  if (f >= P.float && f < P.float + 66) {
    const up = env(f, P.float, P.float + 8, P.float + 50, P.float + 62);
    return { armR: [-46 * up, 0, 26 * up], head: [10 * up, 6 * up, 0], torso: [3 * up, 4 * up, 0] };
  }
  return undefined;
};

// ------------------------------------------------------------------------------------ her frame

export const phaister = (f: number): HerFrame => {
  const [x, z] = place(f);
  let pose = track(f);
  let lift: [number, number, number] = [0, 0, 0];
  const al = alive(f);
  pose = addPose(pose, al.pose, calmLayer(f));
  lift = al.lift;
  // The run's cycle over the DRIVE key; it winds down into the slide.
  const runAmt = env(f, P.run + 2, P.run + 8, P.scoop - 6, P.scoop + 2);
  if (runAmt > 0) {
    const r = runCycle(f - P.run, 12, 1);
    pose = mixPose(pose, addPose(pose, r.pose), runAmt);
    lift = [lift[0], lift[1] + r.lift[1] * runAmt, 0];
  }
  // The short run back south to the juke, overclocked: faster cadence.
  const run2 = env(f, P.block + 6, P.warn + 2, P.juke, P.juke + 6);
  if (run2 > 0) {
    const r = runCycle(f - P.block, 9, 0.8);
    pose = mixPose(pose, addPose(pose, r.pose), run2);
    lift = [lift[0], lift[1] + r.lift[1] * run2, 0];
  }
  const walk = env(f, P.knuckles + 26, P.knuckles + 32, P.rise - 8, P.rise);
  if (walk > 0) {
    const w = walkCycle(f - P.resolve, 1);
    pose = mixPose(pose, addPose(pose, w.pose), walk);
    lift = [lift[0], lift[1] + w.lift[1] * walk, 0];
  }
  // The slide sits her low.
  lift = [lift[0], lift[1] - 0.05 * env(f, P.scoop, P.scoop + 3, P.scoop + 12, P.block), 0];

  // ⚠️ THE BLINK: collapse inward on the spot (no flourish), gone, then thrown open front-on 5.8 m away.
  const foldIn = kf(f, [[P.tag + 1, 0], [P.blink, 1, inCubic]]);
  const foldOut = kf(f, [[P.safe - 1, 1], [P.safe + 5, 0, outCubic]]);
  const fold = f < P.safe - 1 ? (f >= P.tag + 1 ? foldIn : 0) : f < P.safe + 5 ? foldOut : 0;
  const visible = f >= P.blink + 1 && f < P.safe - 1 ? 0 : 1;

  // Her eyes: two flickers then full, like a tube striking, held through the ultimate.
  const glow = kf(f, [[P.ignite, 0], [P.ignite + 2, 0.7], [P.ignite + 4, 0.1], [P.ignite + 7, 0.9], [P.ignite + 9, 0.3], [P.ignite + 13, 1, outQuad], [P.descend + 10, 1], [P.descend + 40, 0]]);

  // The stamp is a little hop; the double take snaps her head to Kuro and back.
  const hop = 0.06 * env(f, P.stomp, P.stomp + 3, P.stomp + 4, P.stomp + 8);
  const take = env(f, P.take, P.take + 3, P.take + 6, P.take + 9) - env(f, P.take + 9, P.take + 11, P.take + 14, P.take + 18) * 0.6;
  pose = addPose(pose, { head: [0, 34 * take, 0] });
  lift = [lift[0], lift[1] + hop, 0];
  return {
    x,
    z,
    y: floatAt(f),
    yaw: ((yawAt(f) % 360) + 360) % 360,
    pose,
    lift,
    face: glow > 0.01 ? 'glow' : 'rest',
    eyeGlow: glow,
    visible,
    flash: 0.7 * fold,
    fold,
    slipper: slipperAt(f),
  };
};

// ------------------------------------------------------------------------------------ the tsinelas

/** When the tsinelas is not in her hand, where it is in the world, and how it has turned. */
export type Flying = { p: [number, number, number]; rot: [number, number, number]; trail: boolean };

/**
 * Its flight, as HOPS: each one a straight run across the ground with a parabola over it, so every arc
 * and every bounce reads as a thrown thing under gravity rather than a spline drifting through the air.
 *
 * ⚠️⚠️ SEEN FROM HAND TO PAD, IN UNDER A SECOND A HOP. 🧑: *"it just floats on her hand and then it flies to a
 * wall FOR NO REASON"*, *"the place she picks up slipper from isnt really the right place"*. The throw is
 * one lob from behind her back onto the can; the can kicks it up and away to the north west, it bounces
 * twice on the road, hops the kerb and skids to a stop on the overclock pad, in front of PC Express, in
 * the same wide shot, which is exactly where she runs to scoop it.
 */
type Hop = [f0: number, f1: number, from: [number, number, number], to: [number, number, number], apex: number];
const RELEASE_AT: [number, number, number] = [M.x - 0.2, 0.95, M.z + 0.4];
const CAN_HIT: [number, number, number] = [0.0, 0.45, -0.35];
const HOPS: Hop[] = [
  [P.release, P.klang, RELEASE_AT, CAN_HIT, 3.1],
  [P.klang, P.klang + 12, CAN_HIT, [-3.3, 0.05, 2.3], 1.15],
  [P.klang + 12, P.klang + 21, [-3.3, 0.05, 2.3], [-6.0, 0.05, 3.9], 0.45],
  [P.klang + 21, P.klang + 29, [-6.0, 0.05, 3.9], [-8.1, 0.25, 4.85], 0.32],
];
const hopAt = (f: number): { p: [number, number, number]; i: number } | null => {
  for (let i = 0; i < HOPS.length; i++) {
    const [f0, f1, a, b, h] = HOPS[i];
    if (f >= f0 && f < f1) {
      const u = (f - f0) / (f1 - f0);
      return { p: [a[0] + (b[0] - a[0]) * u, a[1] + (b[1] - a[1]) * u + 4 * h * u * (1 - u), a[2] + (b[2] - a[2]) * u], i };
    }
  }
  return null;
};
const SKID_FROM = HOPS[HOPS.length - 1][3];
const SKID_START = HOPS[HOPS.length - 1][1];

export const flyingAt = (f: number): Flying | null => {
  const hop = hopAt(f);
  if (hop) {
    // Tumbling end over end in the throw, a slower wobble on the bounces.
    const spin = hop.i === 0 ? 38 : 22;
    return { p: hop.p, rot: [((f - P.release) * spin) % 360, 90, hop.i === 0 ? 0 : 20], trail: hop.i === 0 };
  }
  if (f >= SKID_START && f < P.skid) {
    const u = outQuad((f - SKID_START) / (P.skid - SKID_START));
    return { p: [SKID_FROM[0] + (PAD_SLIPPER.x - SKID_FROM[0]) * u, 0.24, SKID_FROM[2] + (PAD_SLIPPER.z - SKID_FROM[2]) * u], rot: [0, 30 * (1 - u), 0], trail: false };
  }
  if (f >= P.skid && f < P.scoop + 7) return { p: [PAD_SLIPPER.x, 0.24, PAD_SLIPPER.z], rot: [0, 0, 0], trail: false };
  return null;
};

const slipperAt = (f: number): SlipperState => {
  if (flyingAt(f)) return { at: 'none' };
  // The twirl: spun round one finger at her hip, held, never floating free. The same idle trick in the calm.
  const twirl = (a: number, b: number) => f >= a && f < b ? env(f, a, a + 6, b - 6, b) : 0;
  const tw = Math.max(twirl(P.twirl + 4, P.release - 4), twirl(P.float + 8, P.float + 56));
  if (tw > 0) return { at: 'fromHand', offset: [0, 0.03, 0.05 * tw], rot: [0, ((f - P.twirl) * 34 * tw) % 360, 0] };
  return { at: 'hand', swing: 6 * Math.sin((f / LOOP) * Math.PI * 2 * 7) };
};

/** A slipper whip for the throw: frames of trail behind it. */
export const slipperTrail = (f: number, n = 7) => {
  const out: [number, number, number][] = [];
  for (let g = Math.max(P.release, f - n); g <= f; g++) {
    const fl = flyingAt(g);
    if (fl?.trail) out.push(fl.p);
  }
  return out;
};

export { COVEN, s, outBack };
