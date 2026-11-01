import { Face, Layer, Pose, PoseSample, SlipperState } from '../three/actor';
import { Ease, kf, outCubic, outQuad } from '../lib/kf';
import { clamp01, easeInOut, idleSin, INTRO } from './time';
import { B } from './beats';

/**
 * Zack and Rago, KEYED BY HAND on the real rigs. No game clips (method § 7, and the owner again on
 * 2026-11-01: "u can make ur own animations for it, para mas dynamic").
 *
 * Each channel is a key track, so every hand-over is continuous. Past INTRO a figure is only the idle
 * function, and the tracks finish by blending into that same function, so the join cannot pop.
 *
 * LORE.md: Zack makes a difficult play look casual, with a small pause before the snap. Rago is quiet
 * for a long time and then all at once. Rig conventions are method § 2.2. World yaw 0 faces SOUTH
 * (toward the lens behind the line), +90 east.
 */

export type Frame = {
  x: number;
  z: number;
  y: number;
  yaw: number;
  layers: Layer[];
  lift: [number, number, number];
  face: Face;
  eyeGlow: number;
  slipper: SlipperState;
  visible: number;
  samples?: PoseSample[];
};

/**
 * ⚠️ A TRACK OF THE MODELS' OWN CLIPS, TWEAKED (owner, 2026-11-01: "we alr have animations for updated
 * character models u can js tweak"; hand-keyed pose to pose read as clunky). Each entry starts at `at`
 * and fades in over `fade` on top of whatever came before, so nothing can pop. `abs` runs a clip on the
 * idle's clock, so it tiles and lands exactly on the idle clip.
 */
type Seg = { at: number; fade: number; clip?: string; rate?: number; t0?: number; once?: boolean; abs?: number; pose?: Pose };

const layersAt = (f: number, track: Seg[]): Layer[] => {
  let i = 0;
  for (let n = 0; n < track.length; n++) if (f >= track[n].at) i = n;
  // Walk back to the newest entry that is fully faded in: nothing below it can show.
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

// ------------------------------------------------------------------------------------ places

/** His mark on the south throwing line, left of centre so he stands in the hub's gap. */
export const LINE = { x: -0.55, z: -8.35 };
/** Rago stands between the line and the can. */
export const POST = { x: 0.05, z: -1.45 };
export const REST_SLIPPER = { x: 2.1, z: 2.0 };
const CAN_DOWN = { x: 1.15, z: -0.35 };
/** Where he rights it from: beside it, still on the side he guards. */
const SET = { x: 0.7, z: -0.95 };
/** Where Zack waits for him, on the far side of the can. */
export const STAND = { x: 0.8, z: 1.4 };
/** Where Rago's lunge starts. */
const CREEP = { x: 0.45, z: -1.15 };
/** Where he ends up. */
export const FLAT = { x: 0.7, z: 0.75 };
/** Where the can rolls the second time. */
const CAN_KICKED = { x: 1.45, z: -0.55 };
/** Rago's yaw through the lunge: north, a little toward the side lens. */
const LUNGE_YAW = 152;

/** Height of a key on a list of [frame, x, z] waypoints. */
const path = (f: number, pts: [number, number, number][], e: Ease = easeInOut) => {
  if (f <= pts[0][0]) return { x: pts[0][1], z: pts[0][2] };
  for (let i = 1; i < pts.length; i++) {
    if (f <= pts[i][0]) {
      const t = e((f - pts[i - 1][0]) / (pts[i][0] - pts[i - 1][0]));
      return { x: pts[i - 1][1] + (pts[i][1] - pts[i - 1][1]) * t, z: pts[i - 1][2] + (pts[i][2] - pts[i - 1][2]) * t };
    }
  }
  const l = pts[pts.length - 1];
  return { x: l[1], z: l[2] };
};

/** The yaw that faces from one spot to another. */
const yawTo = (a: { x: number; z: number }, b: { x: number; z: number }) => Math.atan2(b.x - a.x, a.z - b.z) / (Math.PI / 180);

/** Backtrack is a real cut: nothing may blur across it. */
const sameSide = (f: number, g: number) => (f < B.snapBack ? Math.min(g, B.snapBack - 0.01) : Math.max(g, B.snapBack));
const WINDOW = [-5, -4, -3, -2, -1, 0, 1, 2, 3];
const samplesOf = (f: number, layers: (g: number) => Layer[]): PoseSample[] =>
  WINDOW.map((o) => ({ at: o, layers: layers(sameSide(f, f + o)) }));
/** Paths and turns rounded over a third of a second, so no corner or start is sudden. */
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

// ------------------------------------------------------------------------------------ Zack's poses

/** The pause before the snap: bladed, side-on, slipper back by the ear. */
const Z_AIM: Pose = { foreR: [0, 96, 0], foreL: [0, -40, 0], root: [2, 0, 0], torso: [4, 24, 0], head: [0, -14, 0], armR: [54, 8, 62], armL: [-46, 0, 26], legL: [-12, 0, 6], legR: [14, 0, -6] };
const Z_SNAP: Pose = { foreR: [0, 6, 0], foreL: [0, -70, 0], root: [8, 0, 0], torso: [12, -22, 0], head: [2, 10, 0], armR: [-78, -8, 18], armL: [28, 0, 22], legL: [-18, 0, 6], legR: [24, 0, -6] };
/** The dead stop on the line: arms out with the arrival, weight still coming. */
const Z_ARRIVE: Pose = { foreR: [0, 28, 0], foreL: [0, -28, 0], root: [10, 0, 0], torso: [6, 6, 0], head: [-8, -4, 0], armR: [-30, -6, 42], armL: [-18, 0, 54], legL: [-14, 0, 13], legR: [-6, 0, -12] };
const Z_PULLED: Pose = { root: [-16, 0, 0], torso: [18, 0, 0], head: [14, 0, 0], armR: [-78, 0, 24], armL: [-78, 0, 24], foreR: [0, 8, 0], foreL: [0, -8, 0], legL: [-34, 0, 8], legR: [-52, 0, -8] };
const Z_SHAKE: Pose = { foreR: [0, 24, 0], foreL: [0, -64, 0], root: [1, 0, -1], torso: [3, 8, 1], head: [8, -14, 3], armR: [-30, -10, 14], armL: [-48, 26, 30], legL: [0, 0, 9], legR: [-5, 0, -5] };

// ------------------------------------------------------------------------------------ Rago's poses

/** Square on, arms folded. He could stand here all day. */
const R_WALL: Pose = { root: [0, 0, 0], torso: [-2, 0, 0], head: [2, 0, 0], armR: [-22, 10, 6], armL: [-22, -10, 6], foreR: [0, 112, 0], foreL: [0, -112, 0], legL: [0, 0, 11], legR: [0, 0, -11] };
const R_CHIN: Pose = { ...R_WALL, head: [-9, 0, 0] };
const R_EYES_DOWN: Pose = { ...R_WALL, head: [11, 0, 0] };
const R_TURNING: Pose = { ...R_WALL, torso: [-2, 12, 0], head: [4, 58, 0] };
const R_SQUARE: Pose = { foreR: [0, 42, 0], foreL: [0, -42, 0], root: [4, 0, 0], torso: [6, 0, 0], head: [-6, 0, 0], armR: [-14, 0, 22], armL: [-14, 0, 22], legL: [-8, 0, 13], legR: [-8, 0, -13] };
/** Everything he has: arms flung out, hopping, still looking round at the can. */
const R_SHOCK: Pose = { root: [-6, 0, 0], torso: [-12, 10, 0], head: [-8, 36, 0], armR: [-44, 0, 96], armL: [-44, 0, 96], foreR: [0, 34, 0], foreL: [0, -34, 0], legL: [0, 0, 18], legR: [0, 0, -18] };
/** Creeping up: low, both hands out in front, ready to tag. */
const R_CREEP: Pose = { root: [14, 0, 0], torso: [10, 0, 0], head: [-14, 0, 0], armR: [-58, 0, 22], armL: [-58, 0, 22], foreR: [0, 30, 0], foreL: [0, -30, 0], legL: [-26, 0, 13], legR: [-6, 0, -13] };
const R_REACH: Pose = { root: [28, 0, 0], torso: [14, 0, 0], head: [-26, 0, 0], armR: [-92, 0, 12], armL: [-82, 0, 22], foreR: [0, 6, 0], foreL: [0, -10, 0], legL: [-48, 0, 8], legR: [38, 0, -8] };
/** The foot has stopped and the rest of him has not. */
const R_TRIP: Pose = { root: [58, 0, 0], torso: [10, 0, 0], head: [-42, 0, 0], armR: [-128, 0, 46], armL: [-112, 0, 54], foreR: [0, 20, 0], foreL: [0, -30, 0], legL: [-22, 0, 8], legR: [54, 0, -8] };
const R_SPRAWL: Pose = { root: [84, 0, 0], head: [-44, 0, 8], armR: [-150, 0, 34], armL: [-150, 0, 34], foreR: [0, 8, 0], foreL: [0, -8, 0], legL: [10, 0, 9], legR: [-14, 0, -9] };
/** Getting up, in no hurry to be looked at. */
const R_UP: Pose = { root: [40, 0, 0], torso: [14, 0, 0], head: [-18, 0, 0], armR: [-58, 0, 16], armL: [-58, 0, 16], foreR: [0, 20, 0], foreL: [0, -20, 0], legL: [-58, 0, 8], legR: [12, 0, -8] };
const R_ONE: Pose = { ...R_WALL, torso: [-2, 0, -1], head: [-6, 0, 2], armR: [-96, 8, 34], foreR: [0, 58, 0] };

// ------------------------------------------------------------------------------------ Zack

/** The run out, waypoint by waypoint; Backtrack reads this same route. */
const RUN: [number, number, number][] = [
  [B.run, LINE.x, LINE.z],
  [B.run + 18, LINE.x + 1.7, LINE.z + 2.0],
  [B.run + 34, 3.45, -3.1],
  [B.scoop, REST_SLIPPER.x + 0.65, REST_SLIPPER.z - 0.25],
];
const routeAt = (f: number) => path(f, RUN, (t) => t);

/** ⚠️ BACKTRACK IS A YANK, NOT A TELEPORT (owner, 2026-11-01): quick, across the frame and off toward the line. */
export const PULL = 12;
// Straight past Rago on the far side from the lens: the way he came would swing him up against the camera.
const pullRoute = () => [STAND, { x: -0.75, z: -0.6 }, { x: -0.75, z: -3 }, LINE];
/** u 0..1 along the route home, by length. */
export const pullAt = (u: number) => {
  const r = pullRoute();
  const len = r.slice(1).map((p, i) => Math.hypot(p.x - r[i].x, p.z - r[i].z));
  let d = Math.max(0, Math.min(1, u)) * len.reduce((a, b) => a + b, 0);
  for (let i = 0; i < len.length; i++) {
    if (d <= len[i]) return { x: r[i].x + (r[i + 1].x - r[i].x) * (d / len[i]), z: r[i].z + (r[i + 1].z - r[i].z) * (d / len[i]) };
    d -= len[i];
  }
  return LINE;
};
/** It catches him, then hauls him in: slow for two frames, then everything. */
export const pullU = (f: number) => clamp01((f - B.snapBack) / PULL) ** 2.2;

const zackPlace = (f: number) => {
  if (f >= B.snapBack && f < B.snapBack + PULL) return pullAt(pullU(f));
  if (f < B.run) return LINE;
  if (f < B.scoop) return routeAt(f);
  if (f < B.snapBack) return path(f, [[B.scoop + 8, RUN[3][1], RUN[3][2]], [B.wall + 10, STAND.x, STAND.z]]);
  return LINE;
};

/** His own idle, the hold's whole performance. Tweaks ride on it. */
const ZI = (at: number, fade: number, pose?: Pose): Seg => ({ at, fade, clip: 'idle', abs: 0, pose });
/** Looking toward screen right: the signal, and Rago in the hold. */
const LOOK_UP: Pose = { torso: [-3, 10, 0], head: [-16, 28, 0] };
const LOOK_BACK: Pose = { torso: [0, 10, 0], head: [-2, 36, 4] };
const Z_TRACK: Seg[] = [
  ZI(0, 0),
  ZI(B.lookUp, 16, LOOK_UP),
  // Not yet: his own head-shake, slowed to read, still looking up at the light.
  { at: B.finger, fade: 8, clip: 'emote-no', rate: 0.55, pose: { head: [-12, 16, 0] } },
  ZI(B.amber - 8, 12, LOOK_UP),
  // The green reaches him: his own charge, slowed, while the eyes light.
  { at: B.eyes + 6, fade: 14, clip: 'hero-zack-charge', rate: 0.42, once: true },
  ZI(B.wide - 10, 18),
  // The pause before the snap, held; he turns away the moment it leaves his hand.
  { at: B.snap - 28, fade: 14, pose: Z_AIM },
  { at: B.snap - 1, fade: 4, pose: Z_SNAP },
  { at: B.snap + 16, fade: 22, clip: 'idle', abs: 0, pose: { head: [-6, -10, 5] } },
  // The cut to him after Rago's jump: chin up, slipper hand lifted, head on one side.
  ZI(B.stare - 12, 14, { head: [-8, 12, 6], torso: [-4, 8, 3], armL: [-22, 0, 0] }),
  { at: B.run - 3, fade: 5, clip: 'sprint', rate: 1 },
  { at: B.scoop - 8, fade: 5, clip: 'slide', rate: 1.1, once: true },
  { at: B.scoop + 8, fade: 10, clip: 'walk', rate: 0.9 },
  ZI(B.wall + 6, 14, { head: [-4, 14, 8], torso: [-3, 8, 0] }),
  // Rago's hand is nearly on him and he looks at us.
  ZI(B.commit, 8, { head: [-8, 26, 8], torso: [-4, 14, 0] }),
  // Home, already looking back to see how it went.
  // Hauled backward from the hips, arms and head left behind; then the stop arrives through him.
  { at: B.snapBack, fade: 3, pose: Z_PULLED },
  { at: B.snapBack + PULL - 2, fade: 3, pose: Z_ARRIVE },
  ZI(B.snapBack + PULL + 8, 14, LOOK_BACK),
  { at: B.shake - 2, fade: 8, pose: Z_SHAKE },
  ZI(B.shake + 18, 14, LOOK_BACK),
  // His answer to the score.
  { at: B.one + 8, fade: 10, clip: 'emote-no', rate: 0.5, once: true, pose: { head: [-3, 10, 0] } },
  ZI(B.fold, 30),
];

const zackLayers = (f: number) => {
  const tap = f >= B.feet && f < B.lookUp ? Math.max(0, Math.sin((((f - B.feet) % 18) / 18) * Math.PI * 2 + 1.2)) : 0;
  const layers = layersAt(f, Z_TRACK);
  if (tap > 0) layers[layers.length - 1] = { ...layers[layers.length - 1], pose: { ...(layers[layers.length - 1].pose ?? {}), legR: [-11 * tap, 0, 0] } };
  return layers;
};

const zackYaw = (f: number) => kf(f, [
    [0, 8],
    [B.lookUp, 8],
    [B.lookUp + 14, 22],
    [B.eyes + 30, 8],
    [B.snap - 24, 8],
    [B.snap - 8, 110],
    [B.snap + 2, 150],
    [B.snap + 12, 150],
    [B.snap + 34, 18],
    [B.run - 3, 18],
    [B.run + 6, 160],
    [B.run + 34, 200],
    [B.scoop, 175],
    [B.scoop + 8, 175],
    [B.scoop + 14, 250],
    [B.wall + 6, 250],
    // Square up to Rago across the can, cheated a little to the side lens.
    [B.wall + 20, 380],
    [B.snapBack - 1, 380],
    // He faces where he was as he is dragged away from it, and comes round on the stop.
    [B.snapBack, 20, () => 1], // 380 is 20: the same way round, so the settle below is short
    [B.snapBack + PULL - 1, 20],
    [B.snapBack + PULL + 8, 12],
    [INTRO, 12],
]);

export const zack = (f: number): Frame => {
  const layers = f >= INTRO ? [{ clip: 'idle', t: (f - INTRO) / 30, w: 1 }] : zackLayers(f);
  const pulling = f >= B.snapBack && f < B.snapBack + PULL + 6;
  const at = f >= INTRO ? LINE : pulling ? zackPlace(f) : { x: rounded(f, (g) => zackPlace(g).x), z: rounded(f, (g) => zackPlace(g).z) };
  const yaw = f >= INTRO ? 12 + 2.5 * idleSin(f, 2, 0.6) : rounded(f, zackYaw);
  const glow = Math.max(
    kf(f, [[B.eyes + 8, 0], [B.eyes + 16, 0.35], [B.eyes + 19, 0.1], [B.eyes + 26, 1], [B.snap + 14, 0.9], [B.snap + 30, 0.25], [B.stare, 0.25], [B.stare + 8, 1], [B.run, 1]]) * (f < B.run ? 1 : 0),
    f >= B.run && f < B.land + 30 ? kf(f, [[B.run, 1], [B.scoop, 1], [B.wall + 10, 0.3], [B.commit - 6, 0.3], [B.commit, 1], [B.land, 1], [B.land + 30, 0]]) : 0,
  );
  const flying = f >= B.snap + 2 && f < B.scoop + 3;
  return {
    x: at.x,
    z: at.z,
    y: 0,
    yaw,
    layers,
    samples: f < INTRO ? samplesOf(f, zackLayers) : undefined,
    lift: [0, 0, 0],
    face: glow > 0.02 ? 'glow' : 'rest',
    eyeGlow: glow,
    slipper: flying ? { at: 'none' } : { at: 'hand', swing: 5 * idleSin(f, 18) },
    visible: 1,
  };
};

/** How much current is on him, for the sparks: Momentum's three visible stages while he runs. */
export const momentum = (f: number) => {
  if (f < B.run || f >= B.scoop + 4) return 0;
  const t = (f - B.run) / (B.scoop - B.run);
  return t < 0.34 ? 1 : t < 0.68 ? 2 : 3;
};

/** Where he was `n` frames ago on the run, for the Backtrack echoes. */
export const echoAt = (f: number) => zackPlace(f);

// ------------------------------------------------------------------------------------ Rago

const RAGO_PHASE = 3.1;
const R_TRACK: Seg[] = [
  { at: 0, fade: 0, pose: R_WALL },
  { at: B.chin, fade: 18, pose: R_CHIN },
  { at: B.finger + 40, fade: 18, pose: R_WALL },
  // The noise. The head comes round, slowly, and stops.
  // His head follows it round: it should have missed, and it has not.
  { at: B.snap + 8, fade: 10, pose: { ...R_WALL, head: [-4, 30, 0] } },
  { at: B.closeOn, fade: 8, pose: R_TURNING },
  { at: B.jolt - 1, fade: 4, pose: R_SHOCK },
  // Then he goes for the can, as a taya must, and from the moment it stands he never takes his eyes off
  // Zack (owner, 2026-11-01: "he wouldve followed him while he was running").
  { at: B.stare + 6, fade: 12, pose: { ...R_SQUARE, head: [10, 0, 0] } },
  { at: B.reset - 26, fade: 8, clip: 'walk', rate: 1.5 },
  { at: B.reset - 4, fade: 7, clip: 'pick-up', rate: 0.75, once: true },
  { at: B.reset + 24, fade: 10, clip: 'walk', rate: 0.75, pose: { root: [12, 0, 0], armR: [-34, 0, 0], armL: [-34, 0, 0], head: [-8, 0, 0] } },
  { at: B.commit - 10, fade: 10, pose: R_CREEP },
  { at: B.commit, fade: 9, pose: R_REACH },
  { at: B.trip - 1, fade: 4, pose: R_TRIP },
  { at: B.sprawl - 5, fade: 5, pose: R_SPRAWL },
  { at: B.rise, fade: 22, pose: R_UP },
  { at: B.rise + 24, fade: 12, pose: R_SQUARE },
  { at: B.walk, fade: 10, clip: 'walk', rate: 0.9 },
  { at: B.pick - 4, fade: 8, clip: 'pick-up', rate: 0.34, once: true },
  { at: B.pick + 22, fade: 8, clip: 'walk', rate: 0.9 },
  { at: B.home - 6, fade: 14, pose: R_WALL },
  { at: B.one - 2, fade: 12, pose: R_ONE },
  { at: B.fold - 4, fade: 18, pose: R_WALL },
  { at: B.fold + 24, fade: 30, clip: 'idle', abs: RAGO_PHASE },
];

const ragoLayers = (f: number) => layersAt(f, R_TRACK);

const ragoPlace = (f: number) =>
  f < B.reset - 26
    ? POST
    : path(f, [
        [B.reset - 26, POST.x, POST.z],
        [B.reset - 4, SET.x, SET.z],
        [B.reset + 24, SET.x, SET.z],
        // He shadows Zack along his side of the can: out with the run, back with the stroll.
        [B.wall - 4, SET.x + 0.65, SET.z - 0.05],
        [B.commit - 6, CREEP.x, CREEP.z],
        [B.commit, CREEP.x, CREEP.z],
        [B.trip, CREEP.x + 0.05, -0.55],
        [B.sprawl, FLAT.x, FLAT.z],
        [B.walk, FLAT.x, FLAT.z],
        [B.pick - 4, CAN_KICKED.x - 0.1, CAN_KICKED.z - 0.75],
        [B.pick + 22, CAN_KICKED.x - 0.1, CAN_KICKED.z - 0.75],
        [B.home, POST.x, POST.z],
      ]);

const ragoYaw = (f: number) => {
  const keyed = kf(f, [
    [0, 0],
    [B.noLook, 0],
    [B.noLook + 30, 14],
    [B.jolt, 26],
    [B.stare, 26],
    [B.stare + 16, yawTo(POST, CAN_DOWN)],
    [B.reset + 22, yawTo(POST, CAN_DOWN)],
    [B.commit, LUNGE_YAW],
    [B.rise + 20, LUNGE_YAW],
    [B.walk + 10, 170],
    [B.pick + 22, 200],
    [B.pick + 34, 300],
    [B.home - 12, 300],
    [B.home, 360],
    [INTRO, 360],
  ]);
  // A beat behind Zack, as a head turning after someone is. Cheated toward the tag's side lens once the can is between them.
  const on = kf(f, [[B.reset + 12, 0], [B.reset + 30, 1], [B.commit - 8, 1], [B.commit, 0]]);
  if (on <= 0) return keyed;
  const cheat = kf(f, [[B.wall - 8, 0], [B.wall + 8, -28]]);
  return keyed + (yawTo(ragoPlace(f), zackPlace(f - 7)) + cheat - keyed) * on;
};

export const rago = (f: number): Frame => {
  const layers = f >= INTRO ? [{ clip: 'idle', t: (f - INTRO) / 30 + RAGO_PHASE, w: 1 }] : ragoLayers(f);
  const at = f >= INTRO ? POST : { x: rounded(f, (g) => ragoPlace(g).x), z: rounded(f, (g) => ragoPlace(g).z) };
  const yaw = f >= INTRO ? 0 + 1.5 * idleSin(f, 3) : rounded(f, ragoYaw);
  // The hop out of his skin; the bounce off the floor.
  const hop = kf(f, [[B.jolt - 2, 0], [B.jolt + 5, 0.13, outQuad], [B.jolt + 12, 0], [B.jolt + 16, 0.02], [B.jolt + 20, 0]]) + kf(f, [[B.sprawl - 4, 0.09], [B.sprawl, 0.015], [B.sprawl + 4, 0.05], [B.sprawl + 8, 0.015]]) * (f >= B.sprawl - 4 && f < B.rise + 20 ? 1 : 0);
  return { x: at.x, z: at.z, y: 0, yaw, layers, lift: [0, hop, 0], face: 'rest', eyeGlow: 0, slipper: { at: 'none' }, visible: 1, samples: f < INTRO ? samplesOf(f, ragoLayers) : undefined };
};

/** Where his head is while he is flat on the court, for the stars. */
export const flatHead = (r: Frame): [number, number, number] => {
  const a = (LUNGE_YAW * Math.PI) / 180;
  return [r.x + Math.sin(a) * 1.55, 0.62, r.z - Math.cos(a) * 1.55];
};

// ------------------------------------------------------------------------------------ the slipper

export type Flying = { p: [number, number, number]; rot: [number, number, number]; trail: boolean };

const CAN_HIT: [number, number, number] = [0.15, 0.38, 0.12];
/** ⚠️ THE CURVE IS THE SHOT: out wide of Rago, round, and into the can from behind him. */
const CURVE: [number, number, number][] = [[LINE.x + 0.5, 1.7, LINE.z + 0.2], [6.2, 2.5, -5.0], [5.2, 1.9, 4.6], CAN_HIT];
export const curveAt = (u: number): [number, number, number] => {
  const v = 1 - u;
  const w = [v * v * v, 3 * v * v * u, 3 * v * u * u, u * u * u];
  return [0, 1, 2].map((i) => CURVE.reduce((sum, p, n) => sum + p[i] * w[n], 0)) as [number, number, number];
};
/** The hit-stop: everything holds this long at the moment of impact. */
export const STOP = 8;

export const flyingAt = (f: number): Flying | null => {
  if (f < B.snap + 2 || f >= B.scoop + 3) return null;
  if (f < B.tump) {
    const u = (f - B.snap - 2) / (B.tump - B.snap - 2);
    return { p: curveAt(u), rot: [(f - B.snap) * 40, 90 + 60 * u, 0], trail: true };
  }
  if (f < B.tump + STOP) return { p: CAN_HIT, rot: [30, 150, 0], trail: true };
  const land = B.tump + STOP + 22;
  if (f < land) {
    const u = (f - B.tump - STOP) / (land - B.tump - STOP);
    return {
      p: [CAN_HIT[0] + (REST_SLIPPER.x - CAN_HIT[0]) * u, 0.12 + 0.8 * 4 * u * (1 - u) * (1 - u), CAN_HIT[2] + (REST_SLIPPER.z - CAN_HIT[2]) * u],
      rot: [(f - B.tump) * 26 * (1 - u), 40 * u, 0],
      trail: false,
    };
  }
  return { p: [REST_SLIPPER.x, 0.1, REST_SLIPPER.z], rot: [0, 40, 0], trail: false };
};

/** The whole ribbon so far: it stays on the air after the hit, so the shape of the shot can be read. */
export const slipperTrail = (f: number) => {
  const out: [number, number, number][] = [];
  const end = Math.min(f, B.tump);
  for (let g = B.snap + 2; g <= end; g++) out.push(curveAt((g - B.snap - 2) / (B.tump - B.snap - 2)));
  return out;
};

// ------------------------------------------------------------------------------------ the can

export type CanFrame = { x: number; z: number; hop: number; rot: [number, number, number]; lying: number };

export const canAt = (f: number): CanFrame => {
  const rest: CanFrame = { x: 0, z: 0, hop: 0, rot: [0, 0, 0], lying: 0 };
  const fall = (at: number, to: { x: number; z: number }, up: number, air = 16): CanFrame | null => {
    const land = at + air;
    if (f < at) return null;
    if (f < land) {
      const u = (f - at) / air;
      return { x: to.x * u, z: to.z * u, hop: (air > 16 ? 3.2 : 1.5) * 4 * u * (1 - u), rot: [0, 60 * u, 900 * outQuad(u)], lying: u };
    }
    if (f < up) {
      const r = clamp01((f - land) / 14);
      return { x: to.x, z: to.z, hop: 0.06 * Math.max(0, 1 - r * 2), rot: [0, 60 + 30 * outCubic(r), 90], lying: 1 };
    }
    if (f < up + 12) {
      const u = easeInOut((f - up) / 12);
      return { x: to.x * (1 - u), z: to.z * (1 - u), hop: 0.3 * 4 * u * (1 - u), rot: [0, 90 * (1 - u), 90 * (1 - u)], lying: 1 - u };
    }
    return null;
  };
  return (f >= B.trip ? fall(B.trip, CAN_KICKED, B.pick + 4) : fall(B.tump + STOP, CAN_DOWN, B.reset + 2, 30)) ?? rest;
};
