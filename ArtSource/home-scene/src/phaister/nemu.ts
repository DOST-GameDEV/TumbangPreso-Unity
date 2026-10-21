import { KuroFace, Pose, addPose, mixPose } from '../three/actor';
import { Ease, inQuad, kf, outBack, outCubic, outElastic, outQuad } from '../lib/kf';
import { easeInOut, env, LOOP, loopNoise, loopSin, s } from './time';
import { PB } from './beats';
import { NEMU_MARK } from './map';
import { TAG } from './perform';

/**
 * Nemu, the taya, and Kuro, keyed by hand like Phaister (no game clips).
 *
 * ⚠️⚠️ SHE IS THE ONE WHO NEVER TOUCHES THE ROAD, AND HER LIMBS ARRIVE BEFORE HER BODY. ASTRA.md 1E:
 * her feet are always clear of the road, nothing she does is a push-off, and her arms reach their
 * extreme a beat EARLY with the torso catching up, which reads as a body being carried rather than
 * moving itself. Her legs drift the same way instead of opposing. So every pose change here is applied
 * to the arms first and to the trunk `LAG` frames later.
 *
 * ⚠️ AND SHE IS THE JOKE'S STRAIGHT MAN. docs/CHARACTER_ORIGINS.md: *"Looks distracted. Already knows
 * your next move"*, and Phaister *"wishes she would at least look surprised"*. She never reacts to
 * anything, never hurries, and is always already where Phaister is going. Kuro, *"a curious little
 * shadow at her shoulder"*, is the one who reacts, and the loop's punchline is his.
 */

export type NemuFrame = { x: number; z: number; y: number; yaw: number; pose: Pose; cursed: number };
export type KuroFrame = { x: number; y: number; z: number; yaw: number; face: KuroFace; tilt: [number, number, number]; bang: number };
export type CanFrame = { x: number; z: number; hop: number; rot: [number, number, number]; lying: number; ring: number };

const P = PB;
const N = NEMU_MARK;
const LAG = 5;

/** Distracted: drifting, looking up and off at nothing, both legs trailing the same way. */
const DRIFT: Pose = { root: [0, 0, 0], torso: [2, 0, 0], head: [-14, 22, 6], armR: [-10, 0, 14], armL: [-16, 0, 10], legL: [12, 0, 2], legR: [16, 0, -2] };
/** One finger to the fallen can. */
const FLICK: Pose = { root: [6, 0, 0], torso: [14, -8, 0], head: [12, -6, 0], armR: [-66, 8, 12], armL: [-8, 0, 16], legL: [18, 0, 2], legR: [22, 0, -2] };
/** Travelling: carried forward, arms trailing. */
const GLIDE: Pose = { root: [8, 0, 0], torso: [4, 0, 0], head: [-6, 0, 0], armR: [24, 0, 22], armL: [24, 0, 22], legL: [26, 0, 2], legR: [30, 0, -2] };
/** In the lane: arms a little out, head tipped, completely calm. */
const BLOCK: Pose = { root: [0, 0, 0], torso: [0, 0, 0], head: [-2, 0, 10], armR: [-22, 0, 32], armL: [-22, 0, 32], legL: [10, 0, 4], legR: [12, 0, -4] };
/** The tag. */
const REACH: Pose = { root: [6, 0, 0], torso: [8, -10, 0], head: [-2, -8, 0], armR: [-86, -8, 14], armL: [10, 0, 24], legL: [20, 0, 2], legR: [24, 0, -2] };
/** Looking at the smoke in her hand. */
const SMOKE: Pose = { root: [0, 0, 0], torso: [4, -6, 0], head: [16, -8, 4], armR: [-62, 12, 10], armL: [-4, 0, 12], legL: [12, 0, 2], legR: [16, 0, -2] };
/** Square to Phaister, nothing on her face at all. */
const FLAT: Pose = { root: [0, 0, 0], torso: [0, 0, 0], head: [0, 0, 0], armR: [0, 0, 8], armL: [0, 0, 8], legL: [8, 0, 2], legR: [8, 0, -2] };
/** Held by the curse: arms pinned, stiff, lifted. */
const HELD: Pose = { root: [0, 0, 0], torso: [-2, 0, 0], head: [-2, 0, 0], armR: [0, 0, 3], armL: [0, 0, 3], legL: [2, 0, 1], legR: [2, 0, -1] };
/** Held, and turning just her head to look at Phaister. */
const LOOK: Pose = { ...HELD, head: [0, 0, 6] };

type PoseKey = [number, Pose, Ease?];
const poseTrack = (f: number, keys: PoseKey[]): Pose => {
  if (f <= keys[0][0]) return keys[0][1];
  for (let i = 1; i < keys.length; i++) {
    const [f1, p1, e] = keys[i];
    const [f0, p0] = keys[i - 1];
    if (f <= f1) return mixPose(p0, p1, (e ?? easeInOut)(f1 === f0 ? 1 : (f - f0) / (f1 - f0)));
  }
  return keys[keys.length - 1][1];
};

const KEYS: PoseKey[] = [
  [0, DRIFT],
  [P.reset, DRIFT],
  [P.reset + 10, GLIDE],
  [P.upright - 8, GLIDE],
  [P.upright, FLICK, outQuad],
  [P.upright + 8, FLICK],
  [P.upright + 16, GLIDE],
  [P.block + 4, BLOCK],
  [P.tag - 6, BLOCK],
  [P.tag + 2, REACH, outQuad],
  [P.blink + 6, REACH],
  [P.blink + 16, SMOKE],
  [P.deadpan, SMOKE],
  [P.deadpan + 24, FLAT],
  [P.resolve + 6, FLAT],
  [P.resolve + 20, GLIDE],
  [P.rise + 6, DRIFT],
  [P.stamp - 2, DRIFT],
  [P.stamp + 4, HELD, outQuad],
  [P.nothing, HELD],
  [P.nothing + 14, LOOK],
  [P.free, LOOK],
  [P.free + 20, DRIFT],
];

/** Her arms follow the track now; her trunk and legs follow it LAG frames late (ASTRA.md 1E). */
const lagged = (f: number): Pose => {
  const early = poseTrack(f, KEYS);
  const late = poseTrack(f - LAG, KEYS);
  return { root: late.root, torso: late.torso, legL: late.legL, legR: late.legR, head: early.head, armL: early.armL, armR: early.armR };
};

/** Where the can lands, knocked over, and where she floats to set it back up. */
export const CAN_DOWN = { x: -1.9, z: 3.35 };
const AT_CAN = { x: -1.25, z: 2.7 };
const LANE = { x: -7.5, z: 1.4 };

const place = (f: number): [number, number] => {
  const x = kf(f, [[P.reset, N.x], [P.upright - 6, AT_CAN.x, easeInOut], [P.upright + 6, AT_CAN.x], [P.block + 6, LANE.x, easeInOut], [P.juke, LANE.x], [P.tag, TAG.x + 0.85, easeInOut], [P.resolve + 6, TAG.x + 0.85], [P.rise + 10, N.x, easeInOut]]);
  const z = kf(f, [[P.reset, N.z], [P.upright - 6, AT_CAN.z, easeInOut], [P.upright + 6, AT_CAN.z], [P.block + 6, LANE.z, easeInOut], [P.juke, LANE.z], [P.tag, TAG.z + 0.75, easeInOut], [P.resolve + 6, TAG.z + 0.75], [P.rise + 10, N.z, easeInOut]]);
  // During the juke she mirrors Phaister's feint with no effort and a little late.
  const juke = env(f, P.juke + 4, P.juke + 10, P.tag - 6, P.tag) * 0.4 * Math.sin(((f - P.juke - 4) / 9) * Math.PI);
  // And in the calm she drifts on a small slow loop round her mark, as ghosts idle.
  const idle = 1 - env(f, P.reset - 6, P.reset, P.free, P.free + 30);
  return [x + juke + idle * 0.25 * loopSin(f, 2, 0.4), z + idle * 0.18 * loopSin(f, 3, 1.1)];
};

const yawAt = (f: number) =>
  kf(f, [
    [0, -24],
    [P.reset, -24],
    [P.reset + 8, 110, easeInOut],
    [P.upright + 6, 110],
    [P.upright + 14, 70],
    [P.block, 30],
    [P.tag - 4, 20],
    [P.deadpan, 20],
    [P.deadpan + 26, 10, easeInOut],
    [P.resolve + 6, 10],
    [P.rise + 10, -24, easeInOut],
    [P.stamp, -10],
    [P.nothing, -10],
    [P.nothing + 14, 0],
    [P.free + 20, -24],
  ]) + 14 * loopNoise('nyaw', f, 1.1, 2) * (1 - env(f, P.reset - 4, P.reset, P.free, P.free + 30));

export const nemu = (f: number): NemuFrame => {
  const [x, z] = place(f);
  const cursed = env(f, P.stamp, P.stamp + 4, P.free, P.free + 14);
  // Her head wanders on its own clock in the calm: looking at nothing, then something, then nothing.
  const wander: Pose = { head: [4 * loopNoise('nhd', f, 1.3, 3), 14 * loopNoise('nhy', f, 1.2, 2), 4 * loopNoise('nhz', f, 1, 3)] };
  const calmW = 1 - env(f, P.reset - 4, P.reset, P.free, P.free + 30);
  const shake: Pose = { root: [0, 0, 1.2 * Math.sin(f * 2.3) * cursed] };
  return {
    x,
    z,
    // Never on the road: a float with a slow bob, lifted higher by the curse.
    y: 0.08 + 0.035 * loopSin(f, 18) * (1 - cursed) + 0.3 * cursed,
    yaw: yawAt(f),
    pose: addPose(lagged(f), mixPose({}, wander, calmW), shake),
    cursed,
  };
};

// ------------------------------------------------------------------------------------ Kuro

/**
 * Kuro at her right shoulder, bobbing. Two reactions only: a flinch at KLANG!, and the punchline, when
 * he shoots up out from behind her, balloons and shakes, the one face in the street that is surprised.
 * His face is his own parts (actor.tsx `drawKuro`), grown the way the game grows them.
 */
export const kuro = (f: number, n: NemuFrame): KuroFrame => {
  const orbit = (f / LOOP) * Math.PI * 2 * 3;
  const calmW = 1 - env(f, P.klang - 6, P.klang, P.free, P.free + 30);
  const base: [number, number, number] = [0.62 * Math.cos(orbit * calmW + 0.4), 1.72 + 0.06 * loopSin(f, 22), 0.3 * Math.sin(orbit * calmW + 0.4)];
  const flinch = env(f, P.klang, P.klang + 2, P.klang + 5, P.klang + 16);
  // The punchline: up and out from behind her shoulder, overshoot, shake; then he hides behind her.
  const pop = kf(f, [[P.kuro - 2, 0], [P.kuro + 5, 1, outBack], [P.tipK + 4, 1], [P.tipK + 18, 0, easeInOut]]);
  const hide = env(f, P.tipK + 36, P.tipK + 46, P.descend + 20, P.descend + 40);
  const off: [number, number, number] = [
    base[0] * (1 - pop) + 0.15 * pop + 0.2 * hide,
    base[1] + 0.25 * flinch + 0.95 * pop - 0.3 * hide,
    base[2] * (1 - pop) - 0.1 * pop + 0.35 * hide,
  ];
  const shake = pop * env(f, P.kuro + 5, P.kuro + 7, P.take, P.take + 6);
  // ⚠️ HIS FACES ARE HIS OWN AUTHORED PARTS (tools/cute_kuro.py, `GhostPetCompanion.ExpressionFor`),
  // the same ones he wears in a match. He is the one who reacts to everything Nemu will not:
  // dizzy at KLANG!, curious at the blink, the big sparkle-eyed "o" of the punchline, heart eyes when
  // Phaister tips her hat to him, then shy behind Nemu; happy, cheeky and sleepy in the calm.
  const SET: Record<string, Pick<KuroFace, 'show' | 'hide'>> = {
    dizzy: { show: ['KuroCrossLeft', 'KuroCrossRight', 'KuroGoofyMouth'], hide: ['ghost-mouth-dot', 'ghost-eye-l', 'ghost-eye-r'] },
    curious: { show: ['KuroSparkleL', 'KuroSparkleR', 'KuroOhMouth'], hide: ['ghost-mouth-dot'] },
    heart: { show: ['KuroHeartEyeL', 'KuroHeartEyeR', 'KuroCatMouth'], hide: ['ghost-mouth-dot', 'ghost-eye-l', 'ghost-eye-r'] },
    shy: { show: ['KuroShyEye', 'KuroPoutMouth'], hide: ['ghost-mouth-dot', 'ghost-eye-r'] },
    happy: { show: ['KuroHappyEyeL', 'KuroHappyEyeR', 'KuroGrinMouth'], hide: ['ghost-mouth-dot', 'ghost-eye-l', 'ghost-eye-r'] },
    cheeky: { show: ['KuroHappyEyeL', 'KuroHappyEyeR', 'KuroCatMouth', 'KuroTongue'], hide: ['ghost-mouth-dot', 'ghost-eye-l', 'ghost-eye-r'] },
    sleepy: { show: ['KuroSleepEyeL', 'KuroSleepEyeR'], hide: ['ghost-eye-l', 'ghost-eye-r'] },
    cat: { show: ['KuroCatMouth'], hide: ['ghost-mouth-dot'] },
  };
  const within = (a: number, b: number) => f >= a && f < b;
  const mood =
    within(P.klang + 1, P.klang + 30) ? 'dizzy'
      : within(P.blink, P.deadpan + 10) ? 'curious'
        : within(P.stamp - 20, P.tipK) ? 'curious'
          : within(P.tipK, P.tipK + 34) ? 'heart'
            : within(P.tipK + 34, P.descend + 40) ? 'shy'
              : within(P.dare, P.blind + 10) ? 'cat'
                : within(s(41.0), s(42.4)) ? 'happy'
                  : within(P.hat, P.hat + 50) ? 'heart'
                    : within(s(47.0), s(48.2)) ? 'cheeky'
                      : within(s(51.5), s(53.0)) ? 'sleepy'
                        : within(s(1.0), s(2.2)) ? 'happy'
                          : null;
  const face: KuroFace = {
    ...(mood ? SET[mood] : {}),
    eyes: mood === 'curious' ? 0.35 + 0.35 * pop : 0,
    mouthTall: 1,
    mouthWide: 1,
    arms: 20 * flinch + 50 * pop,
    puff: 0.25 * flinch + 0.35 * kf(f, [[P.kuro, 0], [P.kuro + 4, 1.2, outQuad], [P.kuro + 10, 0.8, outElastic], [P.take + 10, 0]]),
  };
  return {
    x: n.x + off[0],
    y: n.y + off[1],
    z: n.z + off[2],
    yaw: 10 * loopSin(f, 10),
    face,
    tilt: [0, 0, 14 * Math.sin(f * 1.9) * shake + 8 * loopSin(f, 14)],
    bang: env(f, P.kuro + 2, P.kuro + 4, P.take, P.take + 6),
  };
};

// ------------------------------------------------------------------------------------ the can

/**
 * The lata. KLANG!: it jumps, turns over twice and lands on its side toward the west kerb, rolls a
 * little and lies there; Nemu sets it upright with one finger and it hops back into its circle. At
 * Grand Coven's stamp it jumps in its circle and rings.
 */
export const canAt = (f: number): CanFrame => {
  const rest: CanFrame = { x: 0, z: 0, hop: 0, rot: [0, 0, 0], lying: 0, ring: 0 };
  const land = P.klang + 14;
  if (f >= P.klang && f < land) {
    const u = (f - P.klang) / (land - P.klang);
    return { x: (CAN_DOWN.x + 0.3) * u, z: (CAN_DOWN.z - 0.4) * u, hop: 1.3 * 4 * u * (1 - u), rot: [0, 40 * u, 810 * outQuad(u)], lying: u, ring: 1 };
  }
  if (f >= land && f < P.upright + 2) {
    const roll = kf(f, [[land, 0], [land + 14, 1, outCubic]]);
    const bounce = env(f, land, land + 2, land + 3, land + 8);
    return { x: CAN_DOWN.x + 0.3 - 0.3 * roll, z: CAN_DOWN.z - 0.4 + 0.4 * roll, hop: 0.08 * bounce, rot: [0, 40 + 50 * roll, 90], lying: 1, ring: kf(f, [[land, 1], [land + 24, 0]]) };
  }
  if (f >= P.upright + 2 && f < P.upright + 18) {
    const u = kf(f, [[P.upright + 2, 0], [P.upright + 16, 1, easeInOut]]);
    return { x: CAN_DOWN.x * (1 - u), z: CAN_DOWN.z * (1 - u), hop: 0.55 * 4 * u * (1 - u), rot: [0, 90 * (1 - u), 90 * (1 - kf(f, [[P.upright + 4, 0], [P.upright + 16, 1, outBack]]))], lying: 1 - u, ring: 0 };
  }
  if (f >= P.stamp && f < P.stamp + 16) {
    const u = (f - P.stamp) / 16;
    return { ...rest, hop: 0.22 * 4 * u * (1 - u), rot: [0, 30 * u, 6 * Math.sin(u * 20) * (1 - u)], ring: 1 - u };
  }
  void inQuad;
  return rest;
};
