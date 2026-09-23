import React from 'react';
import { random } from 'remotion';
import { B, bt, K } from '../lib/beats';
import { Bolt, boltPoints, Crackle, Pt } from '../lib/bolt';
import { kf, outBack, outCubic, inCubic, outQuad } from '../lib/kf';
import { clamp01, env, onN } from '../lib/time';
import { chamfer } from '../lib/shapes';
import { GROUND, SideCity, SideCourt, SideForeground, SideSky } from '../art/sideSet';
import { ActorImage, Built, drawActor, DUSK, Light, Pose, useActor } from '../three/actor';
import { runCycle } from '../three/zackWide';
import { Dust } from '../art/props';

/**
 * THE CHASE, side-on, the camera running with him. Four beats inside one continuous shot:
 * the sprint out, the slide where Magnet snaps the tsinelas into his hand, the plant and turn,
 * and the tag that closes on an afterimage. Two of them are in slow motion.
 *
 * ⚠️ TIME RAMPS. `ct` is the chase's own clock: it runs at full speed, slows to 0.35 for the
 * scoop and 0.4 for the grab, then snaps back. Everything in the shot is placed by `ct`, never
 * by the frame, so a ramp slows the whole world together and nothing drifts out of sync.
 */
const RAMPS: [number, number][] = [
  [B.run, 1],
  [B.scoop, 0.35],
  [B.turn, 1],
  [B.lunge, 0.42],
  [B.lunge + bt(14), 1],
];
/** The chase's clock in the ORIGINAL cut's frames: re-timed by K so every beat has room. */
export const ct = (f: number) => ctRaw(f) / K;

/** Frames actually elapsed on screen, with the slow-motion ramps applied: the legs run on this. */
const ctRaw = (f: number) => {
  let t = 0;
  for (let i = 0; i < RAMPS.length; i++) {
    const [a, rate] = RAMPS[i];
    const b = RAMPS[i + 1]?.[0] ?? Infinity;
    if (f <= a) break;
    t += (Math.min(f, b) - a) * rate;
  }
  return t;
};

const T_SCOOP = ct(B.scoop); // 36
const T_TURN = ct(B.turn);
const T_LUNGE = ct(B.lunge);
const SLIPPER_X = 1520;
const ZIP_T = T_LUNGE + 3; // the instant he blinks forward and leaves the afterimage

const zackX = (t: number) => {
  if (t < T_SCOOP) return -260 + 44 * t;
  if (t < T_TURN) return -260 + 44 * T_SCOOP + (SLIPPER_X + 60 - (-260 + 44 * T_SCOOP)) * outCubic((t - T_SCOOP) / (T_TURN - T_SCOOP));
  const x0 = SLIPPER_X + 60;
  if (t < T_TURN + 3) return x0;
  const run = t - (T_TURN + 3);
  const x = x0 - 12 * run - 2.2 * run * run;
  return t > ZIP_T ? x - kf(t, [[ZIP_T, 0], [ZIP_T + 2, 360, outQuad]]) : x;
};

const seanX = (t: number) => 2300 - 46 * Math.max(0, t - (T_TURN - 2)) - (t > T_LUNGE ? 30 * (t - T_LUNGE) : 0);

/**
 * ⚠️ POSES ARE KEY DRAWINGS, NOT BLENDS. Each beat is one or two designed poses of the real rig
 * held, the way limited animation is shot; the timing sells them.
 * Side-on, yaw +90 faces screen right and -90 faces left.
 */
type Act = { pose: Pose; lift: [number, number, number]; facing: 1 | -1; face: 'rest' | 'glow'; holding: boolean };
const poseAt = (t: number): Act => {
  if (t < T_SCOOP - 2) {
    const r = runCycle(t * K, 8, 1.15);
    return { ...r, facing: 1, face: 'rest', holding: false };
  }
  if (t < T_TURN) {
    // The slide, two poses: the drop (leaning back, lead leg out, the hand down for it), then the
    // catch (the tsinelas snapped up into his fist, eyes lit).
    const got = t > T_SCOOP + 3;
    return {
      pose: {
        root: [-24, 0, 0], torso: [-6, 0, 0], head: [16, 0, 0],
        legL: [-80, 0, 4], legR: [24, 0, -4],
        armR: got ? [-150, 0, 16] : [-40, 0, 30], armL: [40, 0, 30],
      },
      lift: [0, -0.1, 0],
      facing: 1,
      face: 'glow',
      holding: got,
    };
  }
  if (t < T_TURN + 3) {
    // The plant: squashed low on both feet, already turned, the tsinelas up by his shoulder.
    return {
      pose: { root: [16, 0, 0], torso: [8, 0, 0], head: [-10, 0, 0], legL: [-30, 0, 8], legR: [34, 0, -8], armR: [-105, 0, 34], armL: [50, 0, 20] },
      lift: [0, -0.05, 0],
      facing: -1,
      face: 'glow',
      holding: true,
    };
  }
  const r = runCycle(t * K, 8, 1.15);
  return { pose: { ...r.pose, armR: [-100, 0, 30] }, lift: r.lift, facing: -1, face: "rest", holding: true };
};

/** Sean: running in, then a dive that lays him out flat with both arms at full stretch. */
const seanPose = (t: number, dive: number, grab: number): { pose: Pose; lift: [number, number, number] } => {
  const r = runCycle((t + 3) * K, 8, 1.1);
  if (dive <= 0) return r;
  return {
    pose: {
      root: [70 * dive, 0, 0], torso: [8, 0, 0], head: [-40 * dive, 0, 0],
      legL: [30 * dive, 0, 4], legR: [50 * dive, 0, -4],
      armL: [-160 + 20 * grab, -10, 16], armR: [-160 + 20 * grab, 10, 16],
    },
    lift: [0, 0.12 * Math.sin(dive * Math.PI), 0],
  };
};

// ⚠️ Turned about 20 degrees toward the lens (yaw 68, not 90): dead side-on, the model's hair
// hides the whole face and the runner reads as a black block with legs.
const PPU = 700;
const SIDE_LIGHT: Light = { ...DUSK, dir: [-0.6, 0.6, 0.55] };

const drawSlipper = (zack: Built, at: Pt, rot: [number, number, number]) =>
  drawActor(zack, { x: at[0], y: at[1], ppu: PPU, reach: 0.2, only: 'slipper', slipper: { at: 'free', pos: [0, 0.4, 0], rot }, res: 1.6, light: SIDE_LIGHT });

export const Chase: React.FC<{ f: number }> = ({ f }) => {
  const zackB = useActor('team-zack', true);
  const seanB = useActor('team-sean');
  if (!zackB || !seanB) return null;
  const t = ct(f);
  const x = zackX(t);
  const act = poseAt(t);
  const facing = act.facing;

  // ---------------------------------------------------------------- camera
  // Lead space in the direction he runs; tighter and tilted into the slide; parked on the grab.
  const lead = facing === 1 ? 300 : -260;
  let camX = x + lead;
  let zoom = 1.0;
  let rot = -3;
  if (t >= T_SCOOP - 4 && t < T_TURN + 3) {
    const k = clamp01((t - (T_SCOOP - 4)) / 4);
    camX = kf(t, [[T_SCOOP - 4, x + 300], [T_SCOOP + 2, SLIPPER_X + 40, outCubic], [T_TURN + 3, SLIPPER_X - 60]]);
    zoom = 1 + 0.26 * k;
    rot = -3 + 5 * k;
  }
  if (t >= T_TURN + 3) {
    const follow = x - 280;
    const park = 1150;
    const toPark = clamp01((t - (T_LUNGE - 4)) / 4);
    camX = follow + (park - follow) * outCubic(toPark);
    zoom = kf(t, [[T_TURN + 3, 1.18], [T_LUNGE - 2, 1.02], [T_LUNGE + 2, 1.2, outBack], [ZIP_T + 8, 1.14]]);
    rot = kf(t, [[T_TURN + 3, 2], [T_LUNGE, -2], [ZIP_T + 8, 1]]);
  }
  // Whip pans in and out of the shot: horizontal motion blur, strongest on the first frames.
  const whipIn = clamp01(1 - (f - B.run) / 5);
  const whipTurn = env(f, B.turn + 1, B.turn + 3, B.turn + 5, B.turn + 8);
  const whipOut = clamp01((f - (B.arrive - 5)) / 5);
  const blur = 60 * whipIn + 36 * whipTurn + 70 * whipOut;
  const panX = 700 * inCubic(whipOut) - 500 * (1 - outCubic(1 - whipIn)) * (whipIn > 0 ? 1 : 0);

  const cam = `translate(${960 + panX} 540) rotate(${rot}) scale(${zoom}) translate(-960 ${-540 - 40})`;
  const step = onN(f, 2);
  // ⚠️ ONE scale for both of them (PPU): they are the same rig in the game.

  // Where things are on screen at depth 1.
  const sx = (wx: number) => wx - camX + 960;

  // ---------------------------------------------------------------- effects
  // The Bolt Sprint ribbon: a narrow zigzag along the court where his feet have been.
  const trailFrom = facing === 1 ? x - 520 : x + 520;
  const ribbon = t < T_SCOOP - 1 || t > T_TURN + 4 ? boltPoints([sx(trailFrom), GROUND - 8], [sx(x) - facing * 30, GROUND - 6], `rib${step}`, 5, 0.08) : null;
  // A spark where the planted foot strikes, on each contact.
  const running = t < T_SCOOP - 2 || t > T_TURN + 3;
  const foot: Pt | null = running && act.lift[1] < 0.006 ? [facing * 30, 0] : null;
  const spark = foot && step % 4 === 0;
  const slipperSnap = t >= T_SCOOP + 1 && t < T_SCOOP + 3;
  const afterAlpha = t >= ZIP_T ? clamp01(1 - (t - ZIP_T) / 7) : 0;
  const grab = clamp01((t - (ZIP_T + 1.5)) / 2);
  const burst = t >= ZIP_T + 3.5 && t < ZIP_T + 9;

  const sean = seanX(t);
  const dive = clamp01((t - (T_LUNGE - 1)) / 4);
  const seanOn = t > T_TURN - 2;

  // ⚠️ THE PLANT IS A TURN, NOT A SWAP. He spins through facing the camera over a few frames;
  // flipping yaw from +68 to -68 in one frame measured as a single-frame spike and read as a pop.
  const turnT = clamp01((t - (T_TURN - 1)) / 3);
  const turnYaw = t < T_TURN - 1 ? 68 : t > T_TURN + 2 ? -68 : 68 - 136 * (turnT * turnT * (3 - 2 * turnT));
  const place = (wx: number, lift = 0) => ({ x: sx(wx), y: GROUND - 0.4 * PPU - lift });
  const zd = drawActor(zackB, {
    ...place(x), ppu: PPU, reach: 0.62, yaw: turnYaw, pitch: 4, pose: act.pose, lift: act.lift, face: act.face,
    slipper: act.holding ? { at: 'hand', swing: facing * 20, twist: 0 } : { at: 'none' }, res: 1.5 * zoom, light: SIDE_LIGHT,
  });
  const ghost = afterAlpha > 0 && !burst
    ? drawActor(zackB, { ...place(zackX(ZIP_T)), ppu: PPU, reach: 0.62, yaw: -68, pitch: 4, ...runCycle(ZIP_T, 8, 1.15), ink: 0, res: 1.2 * zoom,
      light: { ...SIDE_LIGHT, flash: 0.85, flashColour: '#E8F53A' }, slipper: { at: 'hand', swing: -20 } })
    : null;
  const sp = seanPose(t, dive, grab);
  const sd = seanOn
    ? drawActor(seanB, { ...place(sean), ppu: PPU, reach: 0.7, yaw: -64, pitch: 4, pose: sp.pose, lift: sp.lift, res: 1.4 * zoom, light: SIDE_LIGHT })
    : null;
  const courtSlipper = t < T_SCOOP + 1 ? drawSlipper(zackB, [sx(SLIPPER_X), GROUND - 12 - 0.4 * PPU + 0.4 * PPU], [0, 20, -6]) : null;
  const snapT = clamp01((t - T_SCOOP - 1) / 2);
  const flying = slipperSnap
    ? drawSlipper(zackB, [sx(SLIPPER_X) + (sx(x) + 80 - sx(SLIPPER_X)) * snapT, GROUND - 20 - 100 * snapT], [60, t * 60, 30])
    : null;

  return (
    <g>
      <g filter={blur > 1 ? 'url(#mblur)' : undefined}>
        <g transform={cam}>
          <g filter="url(#boilSoft)">
            <SideSky f={f} camX={camX} />
            <SideCity f={f} camX={camX} />
            <SideCourt f={f} camX={camX} canUp={t > T_TURN - 6} />
          </g>
          <g filter="url(#boil)">
            {/* The tsinelas on the court until Magnet takes it. */}
            {courtSlipper && <ActorImage d={courtSlipper} />}
            {slipperSnap && (
              <g>
                <Crackle a={[sx(SLIPPER_X), GROUND - 20]} b={[sx(x) + 120, GROUND - 170]} seed={`sn${step}`} w={14} branches={3} glow={1.8} />
                <Crackle a={[sx(SLIPPER_X) - 30, GROUND - 10]} b={[sx(x) + 90, GROUND - 200]} seed={`sm${step}`} w={8} branches={2} glow={1.2} />
                {flying && <ActorImage d={flying} />}
              </g>
            )}
            {ribbon && (
              <g opacity={0.9}>
                <Bolt pts={ribbon} w={7} glow={1} />
              </g>
            )}
            {/* Sean: running in from the right, then flat out for the tag. */}
            {sd && <ActorImage d={sd} />}
            {/* The afterimage he leaves when he blinks forward: his running shape in his yellow. */}
            {ghost && <ActorImage d={ghost} opacity={afterAlpha * 0.85} filter="url(#electricGhost)" />}
            {burst && (
              <g transform={`translate(${sx(zackX(ZIP_T))} ${GROUND - 300})`}>
                {Array.from({ length: 18 }).map((_, i) => {
                  const a = (i / 18) * Math.PI * 2;
                  const d = 40 + (t - ZIP_T - 3.5) * 60 * (0.6 + random(`gb${i}`) * 0.8);
                  const sz = 14 + random(`gs${i}`) * 16;
                  return <rect key={i} x={Math.cos(a) * d - sz / 2} y={Math.sin(a) * d * 1.4 - sz / 2} width={sz} height={sz} fill={i % 3 ? '#E8F53A' : '#FFFDE8'} opacity={1 - (t - ZIP_T - 3.5) / 5.5} />;
                })}
                <Crackle a={[-120, -80]} b={[140, 120]} seed={`gz${step}`} w={8} branches={3} glow={1.2} />
              </g>
            )}
            {/* Zack. */}
            <ActorImage d={zd} />
            {spark && foot && (
              <g transform={`translate(${sx(x) + foot[0] * facing} ${GROUND + foot[1]})`}>
                <Crackle a={[0, 0]} b={[-facing * 90, -40]} seed={`fs${step}`} w={5} branches={1} glow={1} />
              </g>
            )}
            <Dust t={((t - T_SCOOP + 2) / 10)} x={sx(SLIPPER_X) - 40} y={GROUND} s={1.1} seed="sd" />
            <Dust t={((t - T_TURN) / 9)} x={sx(SLIPPER_X + 60)} y={GROUND} s={1} seed="td" />
          </g>
          <SideForeground camX={camX} />
        </g>
      </g>
      {/* Speed streaks in screen space while he is at full tilt. */}
      {(t < T_SCOOP - 2 || t > T_TURN + 4) &&
        Array.from({ length: 12 }).map((_, i) => {
          const y = 120 + random(`cs${i}`) * 860;
          const len = 200 + random(`cl${i}`) * 400;
          const xx = ((-facing * f * 140 + random(`cx${i}`) * 3000) % 2800 + 2800) % 2800 - 400;
          return <rect key={i} x={xx} y={y} width={len} height={5 + random(`ch${i}`) * 6} fill="#FFE6B0" opacity={0.3} />;
        })}
      {/* Slow motion reads as a held breath: the frame darkens a touch and the edges close in. */}
      {((f >= B.scoop && f < B.turn) || (f >= B.lunge && f < B.lunge + bt(14))) && <rect x={0} y={0} width={1920} height={1080} fill="url(#vignette)" />}
      <MotionBlurDef amount={blur} />
    </g>
  );
};

/** The whip pan's horizontal blur, sized per frame. */
const MotionBlurDef: React.FC<{ amount: number }> = ({ amount }) => (
  <defs>
    <filter id="mblur" x="-10%" y="-10%" width="120%" height="120%">
      <feGaussianBlur stdDeviation={`${Math.max(0.01, amount)} 0`} />
    </filter>
  </defs>
);

export { chamfer, random, outBack, inCubic };
