import { Face, Pose, addPose, mixPose } from '../three/actor';
import { inCubic, kf, linear, outBack, outCubic, outElastic, outQuad } from '../lib/kf';
import { easeInOut, env, LOOP, loopNoise, loopSin } from '../lib/time';
import { PB, pt } from './beats';

/**
 * Phaister's performance, frame by frame, on her real rig.
 *
 * ⚠️ ASTRA.md 1F IS THE DIRECTION: *"SHE PERFORMS"*. The other five heroes do something TO the
 * court; she does something IN FRONT of it. Every big gesture passes through an off-axis
 * FLOURISH and finishes front-on with the chest open, *"held for the room"*; she takes the long
 * way round on purpose. The blink is her one exception, with no flourish at all. LORE.md: she
 * enjoys the audience almost as much as the contest, so between the acts she plays to the lens.
 *
 * ⚠️ NOTHING HERE IS ZACK'S. No tsinelas tricks, no toss, no run. Her idle is a showwoman's: a
 * doodled sigil fading off her fingertip, a hat tip, the moon, a false opening.
 *
 * Rig conventions, measured on the test boards (docs/HOME_SCREEN_ANIMATION_METHOD.md § 2.2): yaw
 * +90 faces screen right; an arm's x negative swings it forward and up, z positive lifts it out
 * from her side; head x negative lifts the chin, head y positive turns her face to screen right;
 * torso x positive bends her forward. The arms are one bone each: there are no elbows and no
 * fingers, so every gesture is a whole-arm line, which is exactly what reads at this size.
 */

export type Pt = [number, number];

export type HerFrame = {
  x: number;
  z: number;
  /** World yaw, the actor's sense: 0 faces back down the road at the lens, +90 faces +X. */
  yaw: number;
  pose: Pose;
  lift: [number, number, number];
  face: Face;
  eyeGlow: number;
  /** 0..1 how much of her is there (the vanish and the return). */
  visible: number;
  /** 0..1 orchid wash (the fold into nothing and the unfold out of it). */
  flash: number;
  /** 0..1 how she is shrinking into the blink (her figure scales toward her centre). */
  fold: number;
  anchors?: { hand: Pt; handL: Pt; eyes: Pt; top: Pt };
};

/** Her mark, the can's mark and where she reappears: metres on the road (view.ts). */
export const HOME = { x: -0.85, z: 3.9 };
export const CAN = { x: 0.2, z: 4.45 };
export const REVEAL = { x: 0.55, z: 6.3 };

/** Her resting stance: weight on her left leg, chest up, chin a touch high, a showwoman at ease. */
const STANCE: Pose = {
  root: [0, 0, 1.5],
  torso: [-3, 10, -2],
  head: [-5, -6, 4],
  armR: [6, 0, 6],
  armL: [-2, 4, 16],
  legL: [0, 0, 7],
  legR: [3, 0, -5],
};
const STANCE_YAW = 16;

/** Breath and weight, periodic on the loop so the seam cannot show. */
const alive = (f: number) => {
  const breath = loopSin(f, 10);
  const weight = loopSin(f, 3, 0.7);
  return {
    pose: {
      root: [0, 0, weight * 1.4],
      torso: [1.2 * breath, 2.2 * weight, -weight],
      head: [-1 * breath + 2 * loopNoise('phn', f, 1.4, 2), 4 * loopNoise('phy', f, 1.2, 2, 3), 1.5 * weight],
      armR: [1.5 * breath, 0, 2 * (breath + 1) / 2],
      armL: [-1.5 * breath, 0, 2 * (breath + 1) / 2],
    } as Pose,
    lift: [0.003 * weight, 0.003 * (breath + 1) / 2, 0] as [number, number, number],
  };
};

// ------------------------------------------------------------------------------------ the doodle

/**
 * ⚠️ THE IDLE'S HEARTBEAT: SHE DOODLES. The way a card magician shuffles without looking, she draws a
 * small sigil in the air with her right hand, idly, every few seconds, and lets it fade. It is her
 * power shown small at rest (method § 1 rule 2) and her personality at once. Whole cycles only,
 * inside calm stretches, on a fixed grid so the loop seam lands between two doodles; the rate is
 * natural and not re-timed.
 */
export const DOODLE = 96;
const BUSY: [number, number][] = [
  [PB.present - 10, PB.settle],
  [PB.hat - 6, PB.hat + pt(40)],
  [PB.moon - 6, PB.moon + pt(62)],
  [PB.train2 - 6, PB.train2 + pt(52)],
  [PB.tease - 6, PB.tease + pt(78)],
];
const calm = (a: number, b: number) => BUSY.every(([x, y]) => b <= x || a >= y) && b <= LOOP;
export const doodle = (f: number) => {
  const c0 = Math.floor(f / DOODLE) * DOODLE;
  if (!calm(c0, c0 + 70)) return null;
  const t = f - c0;
  // Raise the hand out at her side (0 to 12), trace one small loop (12 to 44), let it drop (44 to 60).
  // ⚠️ OUT TO THE SIDE, NOT FORWARD: an arm swung forward at this lens crosses her face (test board).
  const up = env(t, 0, 12, 44, 60);
  const th = kf(t, [[12, 0], [44, 1, easeInOut]]) * Math.PI * 2;
  return { t, up, th, drawn: kf(t, [[12, 0], [44, 1, easeInOut]]), fade: env(t, 12, 20, 50, 70) };
};

// ------------------------------------------------------------------------------------ the raps

/** Frames, from PB.rap, at which her knuckles meet the lid. The can's shudders read the same numbers. */
const KNOCK0 = pt(8);
const KNOCK_P = pt(5);
export const KNOCKS = [KNOCK0 + Math.round(KNOCK_P / 4), KNOCK0 + Math.round(KNOCK_P / 4) + KNOCK_P];
/** 0..1 how far down her rapping arm is, t frames after PB.rap: two strokes, eased. */
const knockArm = (t: number) => (t >= KNOCK0 && t < KNOCK0 + 2 * KNOCK_P ? Math.max(0, Math.sin(((t - KNOCK0) / KNOCK_P) * Math.PI * 2)) : 0);

// ------------------------------------------------------------------------------------ the sigil

/** 0..1 how much of the big sigil ring is drawn, and the hand's angle round it. */
export const sigilDraw = (f: number) => kf(f, [[PB.sigil + pt(4), 0], [PB.sigil + pt(34), 1, easeInOut]]);

// ------------------------------------------------------------------------------------ her frame

const base = (f: number): HerFrame => {
  const a = alive(f);
  const d = doodle(f);
  const doodlePose: Pose | undefined = d
    ? { armR: [(-24 + 12 * Math.sin(d.th)) * d.up, 0, (50 + 12 * Math.cos(d.th)) * d.up], head: [4 * d.up, -10 * d.up, 0], torso: [0, -4 * d.up, 0] }
    : undefined;
  return {
    x: HOME.x,
    z: HOME.z,
    yaw: STANCE_YAW,
    pose: addPose(STANCE, a.pose, doodlePose),
    lift: a.lift,
    face: 'rest',
    eyeGlow: 0,
    visible: 1,
    flash: 0,
    fold: 0,
  };
};

/** The open finish: front-on, chest open, both arms up and out. ASTRA.md: "held for the room". */
const OPEN: Pose = { root: [-2, 0, 0], torso: [-8, 0, 0], head: [-8, 0, 0], armR: [-10, 0, 128], armL: [-10, 0, 128], legL: [0, 0, 9], legR: [0, 0, -9] };
/** Folded into the blink: crouched, arms crossed in. ASTRA.md: "collapse inward". */
const FOLD: Pose = { root: [12, 0, 0], torso: [22, 0, 0], head: [14, 0, 0], armR: [-40, 40, -8], armL: [-40, -40, -8], legL: [-30, 0, 4], legR: [-30, 0, -4] };
/** The bow: deep from the hips, one arm across, one flung out. */
// ⚠️ NOT A DEEP BOW: bent past about 20 degrees, her hat turns into a black slab over the whole figure
// (the first two contact sheets). A performer's curtsey instead: a leg back, a dip, a tilt, one arm across
// and one flung wide, face still to the room.
const BOW: Pose = { root: [4, 0, 0], torso: [8, -8, 0], head: [2, -6, 12], armR: [-36, 44, 8], armL: [-10, 0, 112], legL: [-6, 0, 4], legR: [30, 0, -3] };

export const phaister = (f: number): HerFrame => {
  const b = base(f);

  // ================================================================== THE PLEDGE
  // She turns to us and presents the can with her left arm: an ordinary tin, shown in the light.
  // Then she steps to it, raps the lid twice, and steps back to her mark: see, solid. ⚠️ ONE BLOCK,
  // because the presenting arm has to hand straight over into the reach: two blocks popped at the join.
  if (f >= PB.present && f < PB.gaze) {
    const out = kf(f, [[PB.present, 0], [PB.present + pt(6), -0.25, easeInOut], [PB.present + pt(16), 1, outBack], [PB.rap, 1], [PB.rap + pt(8), 0, easeInOut]]);
    const s = Math.max(0, out);
    const tuck = f < PB.present + pt(10) ? Math.max(0, -out) * 4 : 0;
    const step = env(f, PB.rap, PB.rap + pt(8), PB.rap + pt(18), PB.rap + pt(25));
    const knock = knockArm(f - PB.rap);
    return {
      ...b,
      x: HOME.x + 0.5 * step,
      z: HOME.z + 0.2 * step,
      yaw: kf(f, [[PB.present, STANCE_YAW], [PB.present + pt(14), 4]]) + 52 * step,
      pose: addPose(
        b.pose,
        { armL: [-26 * s - 20 * tuck, -30 * tuck, 58 * s], head: [2 * s, 10 * s, 7 * s], torso: [-3 * s, -8 * s, 2 * s] },
        { torso: [14 * step, 0, 0], head: [-6 * step, 0, 0], root: [4 * step, 0, 0], armL: [-40 * step + 18 * knock, 0, 18 * step], legR: [-18 * step, 0, 0] },
      ),
    };
  }

  // She looks up past us at the moon and points at it: every eye goes where she looks.
  if (f >= PB.gaze && f < PB.sigil) {
    const look = kf(f, [[PB.gaze, 0], [PB.gaze + pt(12), 1, easeInOut]]);
    const back = kf(f, [[PB.sigil - pt(8), 0], [PB.sigil, 1, easeInOut]]);
    const l = look * (1 - back);
    return {
      ...b,
      yaw: kf(f, [[PB.gaze, 4], [PB.gaze + pt(12), 26], [PB.sigil - pt(8), 26], [PB.sigil, 20]]),
      pose: addPose(b.pose, { head: [-6 * l, 30 * l, -4 * l], torso: [-2 * l, 8 * l, 0], armL: [-40 * l, 0, 104 * l], armR: [0, 0, 12 * l] }),
    };
  }

  // ================================================================== THE TURN
  // The sigil: her right arm goes the long way round, one and a quarter turns, the body swinging
  // with it, finishing front-on. Her own eyes kindle orchid as it closes (never redrawn).
  if (f >= PB.sigil && f < PB.train) {
    const dr = sigilDraw(f);
    const th = dr * Math.PI * 2.5;
    const up = env(f, PB.sigil, PB.sigil + pt(5), PB.sigil + pt(34), PB.sigil + pt(40));
    const glow = kf(f, [[PB.sigil + pt(26), 0], [PB.sigil + pt(28), 0.6], [PB.sigil + pt(29), 0.15], [PB.sigil + pt(32), 1]]);
    return {
      ...b,
      yaw: kf(f, [[PB.sigil, 20], [PB.sigil + pt(20), -10], [PB.sigil + pt(38), 2]]),
      pose: addPose(b.pose, {
        armR: [(-22 + 44 * Math.cos(th)) * up, -6 * up, (84 + 44 * Math.sin(th)) * up],
        armL: [0, 0, 22 * up],
        torso: [-4 * up, (-8 + 8 * Math.sin(th)) * up, (4 + 3 * Math.cos(th)) * up],
        head: [-6 * up, (-14 + 5 * Math.sin(th)) * up, -4 * up],
      }),
      face: glow > 0 ? 'glow' : 'rest',
      eyeGlow: glow,
    };
  }

  // She conducts the train in with her right hand, then throws both arms at the sky. ASTRA.md:
  // Grand Coven's anticipation is the longest in the game, sixteen frames between the arms reaching
  // the sky and the night coming down (stageAt's `night`).
  if (f >= PB.train && f < PB.curtain + pt(6)) {
    const conduct = env(f, PB.train, PB.train + pt(8), PB.curtain - pt(12), PB.curtain - pt(6));
    const sky = kf(f, [[PB.curtain - pt(8), 0], [PB.curtain - pt(2), 1, outBack]]);
    const fold = kf(f, [[PB.curtain + pt(1), 0], [PB.curtain + pt(6), 1, inCubic]]);
    // ⚠️ A V, NOT STRAIGHT UP: raised past about 45 degrees her arms vanish behind her hair and hat
    // (test board, 'R z160'). ⚠️ THE ARMS GO UP, THE HEAD DOES NOT GO BACK. Any lean back tips her brim's black underside to the
    // low lens and the whole hat becomes a slab (two contact sheets). The chin stays level or a touch down.
    const skyPose: Pose = { root: [0, 0, 0], torso: [2, 0, 0], head: [4, 0, 0], armR: [-30, 0, 122], armL: [-30, 0, 122] };
    const cond: Pose = { armR: [-50 + 10 * Math.sin((f - PB.train) * 0.35), 0, 108], head: [2, -22, 0], torso: [0, -10, 0] };
    return {
      ...b,
      yaw: 2,
      pose: mixPose(addPose(b.pose, mixPose({}, cond, conduct), mixPose({}, skyPose, sky)), FOLD, fold),
      face: 'glow',
      eyeGlow: 1,
      fold,
      flash: 0.7 * fold,
      visible: 1 - kf(f, [[PB.curtain + pt(3), 0], [PB.curtain + pt(6), 1]]),
    };
  }

  // ================================================================== THE PRESTIGE
  // Gone. Then thrown open front-on behind the can, and held.
  if (f >= PB.curtain + pt(6) && f < PB.reveal) return { ...b, visible: 0 };

  if (f >= PB.reveal && f < PB.home + pt(6)) {
    const unfold = kf(f, [[PB.reveal, 1], [PB.reveal + pt(5), 0, outCubic]]);
    const hold = Math.sin((f - PB.reveal) * 0.08) * 1.5;
    // The flick at the can: her right arm snaps down toward it, then back up to the open finish.
    const flick = env(f, PB.klang - pt(4), PB.klang, PB.klang + pt(2), PB.klang + pt(10));
    // The release: she tips her head up to watch the moon come back.
    const watch = env(f, PB.release, PB.release + pt(8), PB.bow - pt(4), PB.bow);
    const bow = env(f, PB.bow, PB.bow + pt(8), PB.home - pt(8), PB.home - pt(2));
    const fold = kf(f, [[PB.home, 0], [PB.home + pt(5), 1, inCubic]]);
    let pose = addPose(OPEN, { torso: [hold, 0, 0], armR: [60 * flick, 0, -70 * flick], head: [-14 * watch + 12 * flick, 12 * watch - 10 * flick, 0] });
    pose = mixPose(pose, BOW, bow);
    pose = mixPose(pose, FOLD, Math.max(unfold, fold));
    return {
      ...b,
      x: REVEAL.x,
      z: REVEAL.z,
      yaw: -6,
      pose,
      lift: [0, 0.01 * (1 - unfold), 0],
      face: f < PB.release + pt(10) ? 'glow' : 'rest',
      eyeGlow: kf(f, [[PB.release + pt(2), 1], [PB.release + pt(10), 0, outQuad]]),
      fold: Math.max(unfold, fold),
      flash: 0.7 * Math.max(unfold, fold),
      visible: Math.min(kf(f, [[PB.reveal, 0], [PB.reveal + pt(2), 1]]), 1 - kf(f, [[PB.home + pt(3), 0], [PB.home + pt(6), 1]])),
    };
  }

  // Back on her mark: unfolded out of a smaller puff, no flourish, straight into the stance.
  if (f >= PB.home + pt(6) && f < PB.settle) {
    const unfold = kf(f, [[PB.home + pt(8), 1], [PB.home + pt(14), 0, outCubic]]);
    return {
      ...b,
      pose: mixPose(b.pose, FOLD, unfold),
      fold: unfold,
      flash: 0.7 * unfold,
      visible: kf(f, [[PB.home + pt(8), 0], [PB.home + pt(10), 1]]),
    };
  }

  // ================================================================== SHOWMANSHIP
  // The hat tip: hand to the brim, a nod to the room.
  if (f >= PB.hat && f < PB.hat + pt(36)) {
    const up = env(f, PB.hat, PB.hat + pt(8), PB.hat + pt(24), PB.hat + pt(34));
    const nod = env(f, PB.hat + pt(8), PB.hat + pt(13), PB.hat + pt(18), PB.hat + pt(26));
    return {
      ...b,
      yaw: STANCE_YAW - 10 * up,
      // To the brim's EDGE, out at her side: swung up in front, her arm covered her whole face.
      pose: addPose(b.pose, { armR: [-44 * up, 0, 132 * up], head: [14 * nod - 2 * up, -10 * up, 8 * nod], torso: [8 * nod, -6 * up, 0] }),
    };
  }

  // The moon: she turns to it and just looks for a while. The light on her comes up (stageAt).
  if (f >= PB.moon && f < PB.moon + pt(58)) {
    const l = env(f, PB.moon, PB.moon + pt(14), PB.moon + pt(42), PB.moon + pt(56));
    return {
      ...b,
      yaw: STANCE_YAW + 14 * l,
      // Chin up only a little and a partial turn: further, and her hair and hat closed over her face.
      pose: addPose(b.pose, { head: [-12 * l, 18 * l, -3 * l], torso: [-4 * l, 8 * l, 0], armL: [4 * l, 0, -8 * l] }),
    };
  }

  // An ordinary train: she holds her hat on against its draught, and does not perform for it.
  if (f >= PB.train2 && f < PB.train2 + pt(48)) {
    const hold = env(f, PB.train2 + pt(6), PB.train2 + pt(14), PB.train2 + pt(34), PB.train2 + pt(46));
    const buffet = loopNoise('buf', f, 12) * hold;
    return {
      ...b,
      pose: addPose(b.pose, { armL: [-150 * hold, -30 * hold, 16 * hold], head: [8 * hold + 2 * buffet, -4 * hold, 3 * buffet], torso: [6 * hold, 0, 0] }),
    };
  }

  // The false opening (CHARACTER_ORIGINS.md: "invite a chase, leave a false opening"): she turns
  // away down the road as if she had lost interest, holds it, then looks back at us over her
  // shoulder, sly, and turns round. She never turns fully away: dead from behind, these models are
  // a wall of hair and a hat (method § 3).
  if (f >= PB.tease && f < PB.tease + pt(74)) {
    const away = kf(f, [[PB.tease, 0], [PB.tease + pt(14), 1, easeInOut], [PB.tease + pt(52), 1], [PB.tease + pt(70), 0, easeInOut]]);
    const peek = env(f, PB.tease + pt(30), PB.tease + pt(36), PB.tease + pt(48), PB.tease + pt(56));
    return {
      ...b,
      yaw: STANCE_YAW + 112 * away,
      pose: addPose(b.pose, { head: [-4 * away, -64 * peek, 8 * peek], torso: [0, -18 * peek, 0], armR: [0, 0, 8 * away], armL: [0, 0, -6 * away] }),
    };
  }

  return b;
};

// ------------------------------------------------------------------------------------ the can

export type CanFrame = { x: number; z: number; home: Pt; dx: number; hop: number; rot: [number, number, number]; lift: [number, number, number]; yaw: number; ring: number };

/**
 * The lata. Two small shudders when she raps it, then at KLANG! it jumps, turns over once and lands
 * on its side a metre on, rolls, and lies there until she sets the stage again: it rolls back into
 * its ring and hops upright. `ring` 0..1 is how hard it is ringing (the tremble after each hit).
 */
export const canAt = (f: number): CanFrame => {
  const rest: CanFrame = { x: CAN.x, z: CAN.z, home: [CAN.x, CAN.z], dx: 0, hop: 0, rot: [0, 0, 0], lift: [0, 0, 0], yaw: 22, ring: 0 };
  // The raps: a shiver at each knock.
  if (f >= PB.rap && f < PB.gaze) {
    const t = f - PB.rap;
    const k1 = env(t, KNOCKS[0] - 1, KNOCKS[0], KNOCKS[0] + 1, KNOCKS[0] + 6);
    const k2 = env(t, KNOCKS[1] - 1, KNOCKS[1], KNOCKS[1] + 1, KNOCKS[1] + 6);
    const sh = (k1 + k2) * Math.sin(t * 2.1);
    return { ...rest, rot: [0, 0, 2.5 * sh], ring: Math.max(k1, k2) };
  }
  const jump = PB.klang + 2;
  const land = jump + pt(12);
  const back = PB.home + pt(10);
  if (f >= jump && f < back + pt(22)) {
    // Flight: up, over, down.
    if (f < land) {
      const u = (f - jump) / (land - jump);
      // ⚠️ LEFT, AWAY FROM HER: flown to the right it passed straight across her face.
      return { ...rest, dx: -0.9 * u, hop: 0.6 * 4 * u * (1 - u), rot: [0, 90 * u, 450 * outQuad(u)], lift: [0, 0.125 * u, 0], ring: 1 };
    }
    // On its side, a short roll that settles.
    const lying: [number, number, number] = [0, 0.125, 0];
    if (f < back) {
      const roll = kf(f, [[land, 0], [land + pt(14), 1, outCubic]]);
      const bounce = env(f, land, land + 2, land + 3, land + 8);
      return { ...rest, dx: -0.9 - 0.35 * roll, hop: 0.06 * bounce, rot: [0, 90 - 60 * roll, 90], lift: lying, ring: kf(f, [[land, 1], [land + pt(20), 0]]) };
    }
    // Set the stage: it rolls home and hops upright with a little overshoot.
    const home = kf(f, [[back, 0], [back + pt(12), 1, easeInOut]]);
    const up = kf(f, [[back + pt(10), 0], [back + pt(20), 1, outBack]]);
    const hopUp = env(f, back + pt(10), back + pt(14), back + pt(15), back + pt(20));
    return {
      ...rest,
      dx: -1.25 * (1 - home),
      hop: 0.18 * hopUp,
      rot: [0, 30 * (1 - home), 90 * (1 - up)],
      lift: [0, 0.125 * (1 - up), 0],
    };
  }
  return rest;
};

// ------------------------------------------------------------------------------------ the stage

export type Stage = {
  /** 0..1 the eclipse's darkness over the whole street. */
  dark: number;
  /** 0..1 how far the shadow is across the moon. */
  bite: number;
  /** 0..1 how red the swallowed moon has gone. */
  blood: number;
  /** 0..1 moonlight on her (the rim) and the moon's halo. */
  moonlight: number;
  /** The train's front end, metres down the line; 999 is no train. */
  train: number;
  /** 0..1 the passing windows' light on the street. */
  strobe: number;
  /** 0..1 the theatre: the dark iris round the follow spot. */
  theatre: number;
  /** Where the follow spot is, metres, and its radius. */
  spot: [number, number, number];
  /** Smooth rumble for the camera. */
  rumble: number;
};

/** Frames between her arms reaching the sky and the night coming down (ASTRA.md 1F). */
const COVEN = 16;

export const stageAt = (f: number): Stage => {
  const bite = kf(f, [[PB.bite, 0], [PB.curtain - pt(2) + COVEN, 1, easeInOut], [PB.release, 1], [PB.release + pt(16), 0, outCubic]]);
  const night = kf(f, [[PB.curtain - pt(2) + COVEN - 6, 0], [PB.curtain - pt(2) + COVEN + 6, 1], [PB.release, 1], [PB.release + pt(10), 0, easeInOut]]);
  const dark = Math.max(0.55 * kf(f, [[PB.bite, 0], [PB.curtain, 1], [PB.release, 1], [PB.release + pt(14), 0]]), night);
  // The trick's train, then an ordinary one in the calm, slower.
  let train = 999;
  // ⚠️ IT MUST CLEAR HER HAT EARLY. She stands in front of the vanishing point, so the train is
  // hidden behind her until it is about 25 m out; at the first timing that happened as she folded away
  // and the curtain was never seen. Now it bursts out from behind her hat while she conducts it in, and
  // its cars are still thundering overhead as she goes.
  if (f >= PB.train - pt(4) && f < PB.empty + pt(8)) train = kf(f, [[PB.train - pt(4), 150], [PB.train + pt(10), 34, linear], [PB.curtain, -6, linear], [PB.empty + pt(8), -75, linear]]);
  if (f >= PB.train2 - pt(6) && f < PB.train2 + pt(50)) train = kf(f, [[PB.train2 - pt(6), 230], [PB.train2 + pt(50), -60, linear]]);
  // Windows light the street while the cars are overhead (their length is 48 m).
  const over = train < 40 && train > -52 ? env(train, -52, -40, 0, 40) : 0;
  const strobe = over * (0.55 + 0.45 * Math.sin(f * 1.3));
  const theatre = kf(f, [[PB.present, 0], [PB.gaze, 0.55], [PB.empty, 0.8], [PB.bow, 0.6], [PB.settle, 0, easeInOut]]);
  // The follow spot: her, then the empty can, then her again behind it, then home.
  const sx = kf(f, [[PB.curtain, HOME.x], [PB.empty, CAN.x], [PB.reveal, CAN.x], [PB.reveal + pt(6), (CAN.x + REVEAL.x) / 2], [PB.home, (CAN.x + REVEAL.x) / 2], [PB.settle, HOME.x]]);
  const sz = kf(f, [[PB.curtain, HOME.z], [PB.empty, CAN.z], [PB.reveal, CAN.z], [PB.reveal + pt(6), (CAN.z + REVEAL.z) / 2], [PB.home, (CAN.z + REVEAL.z) / 2], [PB.settle, HOME.z]]);
  const sr = kf(f, [[PB.empty, 1.3], [PB.reveal, 0.9], [PB.reveal + pt(6), 1.7], [PB.settle, 1.4]]);
  const moonlight = 1 + 0.9 * env(f, PB.moon + pt(8), PB.moon + pt(18), PB.moon + pt(40), PB.moon + pt(54)) + 0.8 * env(f, PB.release, PB.release + 3, PB.release + pt(6), PB.release + pt(24));
  const rumble = env(f, PB.curtain - pt(10), PB.curtain - pt(2), PB.curtain + pt(4), PB.empty + pt(6)) + 0.35 * env(f, PB.train2 + pt(14), PB.train2 + pt(22), PB.train2 + pt(30), PB.train2 + pt(40));
  return { dark, bite, blood: Math.min(1, night), moonlight, train, strobe, theatre, spot: [sx, sz, sr], rumble };
};

export { outElastic };
