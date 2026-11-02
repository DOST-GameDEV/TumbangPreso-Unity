import { Face, Layer, Pose, PoseSample, SlipperState } from '../three/actor';
import { Ease, kf, outCubic, outQuad } from '../lib/kf';
import { clamp01, easeInOut, idleSin, INTRO } from './time';
import { B } from './beats';
import { litAt } from './light';

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
/** Where Rago's hand nearly has him and Backtrack takes him: heading home past the can. */
export const STAND = { x: -0.6, z: -1.6 };
/** Where Rago's lunge starts: the can is between him and Zack, and he does not see it. */
const CREEP = { x: 0.3, z: 0.75 };
/** Where he ends up, past the can. */
export const FLAT = { x: -0.4, z: -1.2 };
/** Where the can rolls the second time. */
const CAN_KICKED = { x: -1.3, z: 0.2 };
/** Rago's yaw through the lunge. */
const LUNGE_YAW = 339;

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
const linear: Ease = (t) => t;
/**
 * ⚠️ NOBODY STANDS AND STARES (owner, 2026-11-02: "rago shouldve been running after him the moment he got close"). Zack
 * scoops and runs with Rago on him: out round the far side of the court and home past the can, with Rago cutting inside.
 */
const CHASE_Z: [number, number, number][] = [
  [B.scoop + 12, RUN[3][1], RUN[3][2]],
  [B.scoop + 26, 3.6, 3.4],
  [B.scoop + 42, 1.8, 4.4],
  [B.scoop + 60, -0.6, 4],
  [B.scoop + 76, -2.3, 2.4],
  [B.scoop + 90, -2.3, 0.2],
  [B.snapBack, STAND.x, STAND.z],
];
const CHASE_R: [number, number, number][] = [
  [B.reset + 24, SET.x, SET.z],
  [B.reset + 51, 2.3, 0.9],
  [B.reset + 75, 2.6, 3],
  [B.reset + 96, 1, 3.4],
  [B.commit - 9, 0.35, 1.6],
  [B.commit, CREEP.x, CREEP.z],
];
const unwrapTo = (yaw: number, near: number) => yaw + 360 * Math.round((near - yaw) / 360);
const headingTable = (pts: [number, number, number][], from: number) => {
  const memo = new Map<number, number>();
  let h = from;
  for (let g = pts[0][0]; g <= pts[pts.length - 1][0]; g += 0.5) {
    const a = path(g - 1, pts, linear);
    const b = path(g + 1, pts, linear);
    h = unwrapTo(yawTo(a, b), h);
    memo.set(g, h);
  }
  return (f: number) => memo.get(Math.round(f * 2) / 2) ?? from;
};
const zackHeading = headingTable(CHASE_Z, 175);
const ragoHeading = headingTable(CHASE_R, 139);

/**
 * ⚠️ BACKTRACK: SLOW, THEN GONE (owner, 2026-11-02: "slow motion dapat ... they briefly lock eyes but its too late"). He drifts the
 * first metre out of Rago's reach while the world nearly holds, then the rest of the way home in a third of a second.
 */
export const PULL = B.yank + 15 - B.snapBack;
const SLOW_U = 0.13;
const pullRoute = () => [STAND, { x: -1.9, z: -2.9 }, LINE];
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
/** How far home he is. */
export const pullU = (f: number) => {
  if (f <= B.snapBack) return 0;
  if (f < B.yank) return SLOW_U * ((f - B.snapBack) / (B.yank - B.snapBack));
  return Math.min(1, SLOW_U + (1 - SLOW_U) * ((f - B.yank) / 15) ** 1.6);
};
/** When he was that far home. */
export const pullWhen = (u: number) => (u <= SLOW_U ? B.snapBack + (u / SLOW_U) * (B.yank - B.snapBack) : B.yank + 15 * ((u - SLOW_U) / (1 - SLOW_U)) ** (1 / 1.6));

const zackPlace = (f: number) => {
  if (f >= B.snapBack && f < B.snapBack + PULL) return pullAt(pullU(f));
  if (f < B.run) return LINE;
  if (f < B.scoop) return routeAt(f);
  if (f < B.snapBack) return path(f, CHASE_Z, linear);
  return LINE;
};

/** His own idle, the hold's whole performance. Tweaks ride on it. */
const ZI = (at: number, fade: number, pose?: Pose): Seg => ({ at, fade, clip: 'idle', abs: 0, pose });
/** Looking toward screen right: the signal, and Rago in the hold. */
const LOOK_UP: Pose = { torso: [-3, 10, 0], head: [-16, 28, 0] };
const LOOK_BACK: Pose = { head: [-3, 0, 0] };
const Z_TRACK: Seg[] = [
  ZI(0, 0),
  // ⚠️ ONE LOOK (owner, 2026-11-02: "why does he stare back at the middle"). Over his shoulder at the map's real
  // signal, behind him at screen right, and the film cuts straight to what he sees.
  ZI(B.lookUp, 14, LOOK_UP),
  // The green: he only lifts his chin at it.
  ZI(B.eyes, 12, { head: [-7, 6, 3] }),
  ZI(B.wide - 10, 18),
  // The pause before the snap, held; he turns away the moment it leaves his hand.
  { at: B.snap - 28, fade: 14, pose: Z_AIM },
  { at: B.snap - 1, fade: 4, pose: Z_SNAP },
  { at: B.snap + 16, fade: 22, clip: 'idle', abs: 0, pose: { head: [-6, -10, 5] } },
  // The cut to him after Rago's jump: chin up, slipper hand lifted, head on one side.
  ZI(B.stare - 12, 14, { head: [-8, 12, 6], torso: [-4, 8, 3], armL: [-22, 0, 0] }),
  // ⚠️ RATES ARE THE PATH'S SPEED OVER THE CLIP'S STRIDE, so feet stay planted: sprint covers about 4.1 m/s at rate 1, walk 1.8.
  { at: B.run - 3, fade: 5, clip: 'sprint', rate: 1.4 },
  { at: B.scoop - 8, fade: 5, clip: 'slide', rate: 1.1, once: true },
  // Up and straight into it, glancing back at Rago coming. 3.8 m/s over the sprint's 4.1.
  { at: B.scoop + 8, fade: 6, clip: 'sprint', rate: 0.92 },
  // He looks straight down the lens: nothing else on him changes.
  { at: B.look, fade: 6, clip: 'sprint', rate: 0.92, pose: { head: [-5, 0, 0] } },
  // Home, already looking back to see how it went.
  // Hauled backward from the hips, arms and head left behind; then the stop arrives through him.
  { at: B.snapBack, fade: 10, pose: Z_PULLED },
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

const zackYaw = (f: number) => f >= B.scoop + 12 && f < B.snapBack ? zackHeading(f) : kf(f, [
    [0, 8],
    [B.lookUp, 8],
    [B.lookUp + 16, 118],
    [B.amber + 2, 118],
    [B.eyes - 4, 8],
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
    [B.scoop + 12, zackHeading(B.scoop + 12)],
    // He faces where he was as he is dragged away from it, and comes round on the stop.
    // Taken from behind, he comes round to face the hand that missed, and holds it.
    [B.snapBack, zackHeading(B.snapBack - 0.5), () => 1],
    [B.snapBack + 16, 161],
    [B.yank, 161],
    // ⚠️ THE TURN (owner, 2026-11-02: "make him TURN AROUND"). He lands with his back to us, still watching Rago get up and keep the score.
    // Only when that is done does he turn all the way round to the lens.
    [B.snapBack + PULL + 8, 175],
    [B.fold, 175],
    [B.fold + 44, 4.4], // the idle starts from here
    [INTRO, 4.4],
]);

export const zack = (f: number): Frame => {
  const layers = f >= INTRO ? [{ clip: 'idle', t: (f - INTRO) / 30, w: 1 }] : zackLayers(f);
  const pulling = f >= B.snapBack && f < B.snapBack + PULL + 6;
  const at = f >= INTRO ? LINE : pulling ? zackPlace(f) : { x: rounded(f, (g) => zackPlace(g).x), z: rounded(f, (g) => zackPlace(g).z) };
  const yaw = f >= INTRO ? 3 + 2.5 * idleSin(f, 2, 0.6) : rounded(f, zackYaw);
  // ⚠️ NO LIGHTNING EYES BEFORE THE CHASE IS DONE (owner, 2026-11-02). They catch on the look, stutter once, and hold through Backtrack.
  const glow = f >= B.look && f < B.land + 30 ? kf(f, [[B.look + 8, 0], [B.look + 11, 0.5], [B.look + 13, 0.1], [B.look + 16, 1], [B.land, 1], [B.land + 30, 0]]) : 0;
  // On the hold his eyes answer only his biggest moment of current.
  const held = f >= INTRO ? Math.max(0, litAt(f) - 0.4) / 0.5 : 0;
  const flying = f >= B.snap + 2 && f < B.scoop + 3;
  return {
    x: at.x,
    z: at.z,
    y: 0,
    yaw,
    layers,
    samples: f < INTRO ? samplesOf(f, zackLayers) : undefined,
    lift: [0, 0, 0],
    face: Math.max(glow, held) > 0.02 ? 'glow' : 'rest',
    eyeGlow: Math.max(glow, held),
    slipper: flying ? { at: 'none' } : { at: 'hand', swing: 5 * idleSin(f, 18) },
    visible: 1,
  };
};

/** How much current is on him, for the sparks: Momentum's three visible stages while he runs. */
export const momentum = (f: number) => {
  if (f < B.run || f >= B.snapBack) return 0;
  if (f >= B.scoop - 2 && f < B.scoop + 12) return 1;
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
  { at: B.reset - 26, fade: 8, clip: 'walk', rate: 0.8 },
  { at: B.reset - 4, fade: 7, clip: 'pick-up', rate: 0.75, once: true },
  // He comes up off the can already running at Zack.
  { at: B.reset + 22, fade: 8, clip: 'sprint', rate: 0.7, pose: { root: [6, 0, 0], head: [-6, 0, 0] } },
  { at: B.commit - 2, fade: 8, pose: R_REACH },
  // The hand stays where he was. The head goes after him, and when he is yanked it whips after him, too late.
  { at: B.lockR - 8, fade: 22, pose: { ...R_REACH, head: [-26, -22, 0] } },
  { at: B.yank - 1, fade: 4, pose: { ...R_REACH, head: [-20, -62, 0] } },
  { at: B.trip + 2, fade: 4, pose: R_TRIP },
  { at: B.sprawl - 5, fade: 5, pose: R_SPRAWL },
  { at: B.rise, fade: 22, pose: R_UP },
  { at: B.rise + 24, fade: 12, pose: R_SQUARE },
  { at: B.walk, fade: 8, clip: 'walk', rate: 1 },
  { at: B.pick - 4, fade: 8, clip: 'pick-up', rate: 0.34, once: true },
  { at: B.pick + 22, fade: 8, clip: 'walk', rate: 1.2 },
  { at: B.home - 6, fade: 14, pose: R_WALL },
  { at: B.one - 2, fade: 12, pose: R_ONE },
  { at: B.fold - 4, fade: 18, pose: R_WALL },
  { at: B.fold + 24, fade: 30, clip: 'idle', abs: RAGO_PHASE },
];

const ragoLayers = (f: number) => layersAt(f, R_TRACK);

const ragoPlace = (f: number) => {
  if (f < B.reset - 26) return POST;
  if (f < B.reset + 24) return path(f, [[B.reset - 26, POST.x, POST.z], [B.reset - 4, SET.x, SET.z]]);
  if (f < B.commit) return path(f, CHASE_R, linear);
  return path(f, [
    [B.commit, CREEP.x, CREEP.z],
    [B.snapBack, 0.12, 0.3],
    [B.yank, 0.0, 0.05],
    [B.trip, -0.1, -0.15],
    [B.sprawl, FLAT.x, FLAT.z],
    [B.walk, FLAT.x, FLAT.z],
    [B.pick - 4, CAN_KICKED.x - 0.1, CAN_KICKED.z - 0.75],
    [B.pick + 22, CAN_KICKED.x - 0.1, CAN_KICKED.z - 0.75],
    [B.home, POST.x, POST.z],
  ]);
};

const ragoYaw = (f: number) => {
  if (f >= B.reset + 24 && f < B.commit) return ragoHeading(f);
  return kf(f, [
    [0, 0],
    [B.noLook, 0],
    [B.noLook + 30, 14],
    [B.jolt, 26],
    [B.stare, 26],
    [B.stare + 16, yawTo(POST, CAN_DOWN)],
    [B.reset + 22, yawTo(POST, CAN_DOWN)],
    [B.reset + 24, ragoHeading(B.reset + 24)],
    [B.commit, LUNGE_YAW],
    [B.rise + 20, LUNGE_YAW],
    [B.walk + 10, 245],
    [B.pick + 22, 180],
    [B.pick + 34, 58],
    [B.home - 6, 58],
    [B.home + 6, 0],
    [INTRO, 0],
  ]);
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
