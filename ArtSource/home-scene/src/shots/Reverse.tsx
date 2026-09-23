import React from 'react';
import { random } from 'remotion';
import { B, bt, K } from '../lib/beats';
import { Bolt, boltPoints, branchesFor, Pt } from '../lib/bolt';
import { inCubic, kf, outBack, outCubic, outQuad } from '../lib/kf';
import { clamp01, env, onN } from '../lib/time';
import { CAN_AT, Court, EastCity, EastSky, ParapetPigeons, SEAN_AT } from '../art/court';
import { Can, Debris, Dust, SpeedLines } from '../art/props';
import { Burst } from '../art/burst';
import { Whip } from '../lib/whip';
import { Tump } from '../art/tump';
import { ActorImage, addPose, Built, drawActor, DUSK, HAND_R, Light, mixPose, Pose, useActor } from '../three/actor';

/**
 * The reverse shot, over his right shoulder: the owner's own "video(?)" composition from the
 * earlier HOME sketch (a big thrower in the foreground, the can and a figure down the court),
 * and her storyboard frames 25 to 34.
 *
 * ⚠️ THE PAUSE BEFORE THE SNAP IS THE ACTING (ASTRA.md: "a small pause before the snap"). He
 * lines the tsinelas up on the can and HOLDS, then the wind-up is fast and the snap is faster.
 * Then three frozen frames on contact, which is what makes a hit read as heavy.
 *
 * Every figure is the game's model: Zack's back big in the foreground, Sean small by the can, and
 * the thrown tsinelas is the real one drawn on its own.
 */

// Where his collar sits in frame, and how big he is. He is cropped at the waist.
// ⚠️ At 1450 ppu from straight behind, the back of his hair was a black block over half the
// frame (the first render of this pass, and the same fault the drawn version had at 1.7x).
const BACK = { x: 360, y: 990, ppu: 860 };

const RELEASE = B.snap + bt(2);

// Key poses, on his real rig. Arms: x forward-negative, y across, z out from the body.
const AIM: Pose = { root: [4, 0, 0], torso: [2, 18, 0], head: [6, 14, 0], armR: [-84, -6, 4], armL: [16, 0, 14] };
const WINDUP: Pose = { root: [-6, 0, 0], torso: [-10, 38, -6], head: [4, 4, 0], armR: [150, -10, 18], armL: [-40, 0, 26] };
const SNAP: Pose = { root: [14, 0, 0], torso: [16, -26, 4], head: [10, 10, 0], armR: [-112, 18, 6], armL: [30, 0, 18] };
const FOLLOW: Pose = { root: [10, 0, 0], torso: [12, -30, 2], head: [6, 16, 0], armR: [-70, 40, 4], armL: [26, 0, 16] };

const armAt = (f: number): Pose => {
  const settle = kf(f, [[B.reverse, 0], [B.reverse + bt(12), 1, outCubic]]);
  if (f < B.snap - bt(7)) return mixPose(addPose(AIM, { armR: [18, 0, 0] }), AIM, settle);
  if (f < B.snap) return mixPose(AIM, WINDUP, kf(f, [[B.snap - bt(7), 0], [B.snap, 1, outCubic]]));
  if (f < B.snap + bt(3)) return mixPose(WINDUP, SNAP, outQuad((f - B.snap + bt(1)) / bt(3)));
  const drift = kf(f, [[B.snap + bt(3), 0], [B.hit, 0.5], [B.hit + bt(20), 1]]);
  return mixPose(SNAP, FOLLOW, drift);
};

const drawSlipper = (zack: Built, at: Pt, ppu: number, rot: [number, number, number], light: Light) =>
  drawActor(zack, { x: at[0], y: at[1], ppu, reach: 0.2, only: 'slipper', slipper: { at: 'free', pos: [0, 0.4, 0], rot }, res: 1.5, light });

export const Reverse: React.FC<{ f: number }> = ({ f }) => {
  const zack = useActor('team-zack', true);
  const sean = useActor('team-sean');
  if (!zack || !sean) return null;
  // Hit-stop: three frames where the world does not advance at all.
  const frozen = f >= B.hit && f < B.tump;
  const ff = frozen ? B.hit : f;
  // In the original cut's frames, so the can's flight and the dust keep their shape over the
  // longer TUMP! hold instead of the can leaving the frame half way through it.
  const after = Math.max(0, ff - B.tump) / K;

  const charge = kf(ff, [[B.reverse, 0.35], [B.snap - bt(7), 0.5], [B.snap, 1], [RELEASE, 1], [RELEASE + 1, 0.2], [B.hit + bt(20), 0]]);

  // ⚠️ The camera is the throw's second actor (🧑 2026-09-23: *"dynamic camera movement"*). It
  // creeps toward the can while he aims, PULLS BACK with his wind-up (the anticipation belongs to
  // the lens too), rushes down the court with the tsinelas, punches in on the hit, then tilts up
  // after the can as it flies, all while rolling a few degrees against each move.
  const zoom = kf(ff, [
    [B.reverse, 1.0], [B.snap - bt(7), 1.06], [B.snap, 0.98, outCubic], [B.hit, 1.34, inCubic], [B.tump, 1.46, outBack], [B.tump + bt(10), 1.28], [B.run, 1.2],
  ]);
  const fx = kf(ff, [[B.reverse, 900], [B.snap - bt(7), 980], [B.snap, 860], [B.hit, CAN_AT.x], [B.tump + bt(14), CAN_AT.x + 60], [B.run, CAN_AT.x + 160]]);
  const fy = kf(ff, [[B.reverse, 640], [B.snap, 700], [B.hit, CAN_AT.y - 30], [B.tump + bt(6), CAN_AT.y - 90], [B.run, CAN_AT.y - 210]]);
  const roll = kf(ff, [[B.reverse, -1.5], [B.snap - bt(7), 0], [B.snap, 2.5], [B.hit, -3], [B.tump + bt(6), 1.5], [B.run, 3]]);
  const shakeAmt = env(f, B.hit, B.hit + bt(1), B.tump + bt(6), B.tump + bt(22)) * 26;
  const sx = (random(`rsx${onN(f, 2)}`) - 0.5) * shakeAmt;
  const sy = (random(`rsy${onN(f, 2)}`) - 0.5) * shakeAmt;
  const cam = `translate(${960 + sx} ${540 + sy}) rotate(${roll}) scale(${zoom}) translate(${-fx} ${-fy})`;
  const whipIn = f < B.reverse + 5 ? 1 - (f - B.reverse) / 5 : 0;
  const whipOut = f >= B.run - 4 ? (f - (B.run - 4)) / 4 : 0;

  // ⚠️ The sun is BEHIND THE LENS in this shot (the east court is lit gold), so it lights his back.
  // Lit from beyond him, his back and his near-black hair went to one flat black mass.
  const light: Light = { ...DUSK, dir: [0.35, 0.55, 0.76], rim: '#FFD27A', rim_strength: 0.9, flash: 0.1 * charge, flashColour: '#E8F53A' };
  // Zack from behind: yaw 180 shows his back, a little more turns him toward the can.
  // ⚠️ FOREGROUND PARALLAX. He is much nearer the lens than the court, so when the camera rushes
  // down to the can he slides out of frame faster than the world does. Drawn at the court's
  // depth, the back of his head sat in the corner as a black block through the hit and TUMP!.
  const fgX = BACK.x - 1.1 * (fx - 900) - 260 * clamp01((ff - RELEASE) / 6);
  const fgY = BACK.y + 0.6 * (fy - 640);
  const z = drawActor(zack, {
    x: fgX,
    y: fgY,
    focus: 0.34,
    reach: 0.52,
    ppu: BACK.ppu,
    // Turned toward profile, so his face and throwing arm read over his shoulder, not his crown.
    yaw: 124,
    pitch: 6,
    pose: armAt(ff),
    slipper: ff < RELEASE ? { at: 'hand', swing: ff < B.snap - bt(7) ? -80 : ff < B.snap ? kf(ff, [[B.snap - bt(7), -80], [B.snap, 30]]) : -120, twist: 0 } : { at: 'none' },
    res: 1.1,
    light,
  });
  const hand = z.at('arm-right', HAND_R);

  // The tsinelas in flight: from the hand at release to the can, shrinking into the distance,
  // spinning, dragging a narrow branching bolt behind it.
  const flightT = clamp01((ff - RELEASE) / (B.hit - RELEASE));
  const inFlight = ff >= RELEASE && ff < B.hit;
  const start: Pt = hand;
  const target: Pt = [CAN_AT.x - 20, CAN_AT.y - 48];
  const pos: Pt = [start[0] + (target[0] - start[0]) * flightT, start[1] + (target[1] - start[1]) * flightT - 60 * Math.sin(flightT * Math.PI)];
  const trailEnd = inFlight ? pos : target;
  const trailOn = ff >= RELEASE && f < B.tump + bt(14);
  const trailFade = f >= B.tump ? 1 - (f - B.tump) / bt(14) : 1;
  const trail = trailOn ? boltPoints(start, trailEnd, `trail${onN(f, 2)}`, 5, 0.14) : null;

  // The can: frozen on the hit, then thrown up and away from him, turning.
  const canPos: Pt = [CAN_AT.x + after * 16, CAN_AT.y - after * 30 + after * after * 0.5];
  const canRot = after * 22;

  // The tsinelas after the hit drops onto the court in front of the circle and bounces once.
  const dropT = after / 12;
  const slipperDown: Pt = [CAN_AT.x - 70 + after * 2, CAN_AT.y - 4 - Math.abs(Math.sin(Math.min(dropT, 2) * Math.PI)) * 40 * Math.max(0, 1 - dropT / 2)];

  // Sean, the taya, bored by the can until it goes, then he flinches back from it.
  const flinch = kf(f, [[B.hit, 0], [B.tump + bt(2), 1, outCubic], [B.tump + bt(14), 0.85], [B.run, 0.8]]);
  const seanPose: Pose = mixPose(
    { root: [0, 0, 0], torso: [4, 0, 0], head: [8, 10 * Math.sin(f * 0.05), 0], armL: [0, 0, 12], armR: [0, 0, 12] },
    { root: [-16, 0, 0], torso: [-12, 0, 0], head: [-14, 0, 0], armL: [-120, -20, 30], armR: [-120, 20, 30], legR: [-20, 0, 0] },
    flinch,
  );
  const SEAN_PPU = 300;
  const s = drawActor(sean, {
    x: SEAN_AT.x,
    y: SEAN_AT.y - 0.4 * SEAN_PPU,
    ppu: SEAN_PPU,
    yaw: -20,
    pitch: 8,
    pose: seanPose,
    lift: [0, 0.05 * Math.sin(Math.PI * clamp01(after / 8)), 0],
    res: 1.6,
    light: { ...DUSK, dir: [0.2, 0.8, 0.6] },
  });

  const flight = inFlight ? drawSlipper(zack, pos, 1100 * (1.1 - flightT * 0.7), [70, flightT * 900, 20], light) : null;
  const down = ff >= B.tump ? drawSlipper(zack, slipperDown, 420, [0, 30, -8 + Math.min(after, 18) * 2], { ...DUSK, dir: [0.2, 0.8, 0.6] }) : null;

  return (
    <Whip id="rwhip" blur={80 * whipIn + 90 * whipOut} dx={-520 * whipIn * whipIn + 600 * whipOut * whipOut}>
      <g transform={cam}>
        <g filter="url(#boilSoft)">
          <EastSky f={ff} />
          <EastCity f={ff} />
          <Court f={ff} />
        </g>
        <ParapetPigeons f={f} scatter={clamp01(after / 26)} />
        {/* His shadow, cast forward toward the can by the sun behind us. */}
        <path d="M 120 1080 L 820 1080 L 960 760 L 700 740 Z" fill="#9E5A36" opacity={0.35} />
        <ellipse cx={SEAN_AT.x} cy={SEAN_AT.y} rx={70} ry={14} fill="#8E3E2C" opacity={0.4} />
        <ActorImage d={s} />
        {/* The lata. */}
        <g transform={`translate(${canPos[0]} ${canPos[1]}) rotate(${ff >= B.tump ? canRot : 0})`}>
          <Can s={1} lit="left" />
        </g>
        <Dust t={after / 26} x={CAN_AT.x} y={CAN_AT.y} s={1.2} seed="cd" />
        <Debris t={after / 28} x={CAN_AT.x} y={CAN_AT.y - 40} seed="cdb" n={12} />
        {down && <ActorImage d={down} />}
        {/* The throw's bolt, lingering a moment after the hit. */}
        {trail && (
          <g opacity={trailFade}>
            {branchesFor(trail, `tb${onN(f, 2)}`, 3, 0.25).map((b, i) => (
              <Bolt key={i} pts={b} w={5} glow={0.8} />
            ))}
            <Bolt pts={trail} w={11} glow={1.4} />
          </g>
        )}
        {flight && <ActorImage d={flight} />}
        {/* The hit itself: her burst behind the can for the frozen frames. */}
        {frozen && (
          <g transform={`translate(${CAN_AT.x} ${CAN_AT.y - 44})`}>
            <SpeedLines cx={0} cy={0} r0={140} r1={560} n={26} seed="hit" colour="#FFF3C8" w={16} />
            <Burst r={170} arms={9} seed="hitb" rot={f * 3} inner={0.5} fill="#E8F53A" shade="#C9D400" swirl={null} />
            <Burst r={90} arms={7} seed="hitc" rot={-f * 5} inner={0.55} fill="#FFFDE8" shade="#FFF3A0" swirl={null} />
          </g>
        )}
        {/* Zack, big in the foreground. Last, so he overlaps everything down the court. */}
        <ActorImage d={z} />
        {charge > 0.3 && ff < RELEASE + 2 && (
          <g>
            {[0, 1, 2].map((i) => {
              const a = onN(f, 2) * 0.9 + i * 2.1;
              return <Bolt key={i} pts={boltPoints([hand[0] + Math.cos(a) * 20, hand[1] + Math.sin(a) * 20], [hand[0] + Math.cos(a) * 140, hand[1] + Math.sin(a) * 110], `rh${onN(f, 2)}${i}`, 4, 0.3)} w={5} glow={charge} />;
            })}
          </g>
        )}
      </g>
      {/* TUMP! is lettered in SCREEN space, like a comic, so the camera punching in on the can
          never crops or shrinks it. */}
      <Tump f={f} start={B.tump} x={560} y={330} size={236} />
      {f === B.hit && <rect x={0} y={0} width={1920} height={1080} fill="#FFF6D0" opacity={0.7} />}
      {f === B.hit + bt(1) && <rect x={0} y={0} width={1920} height={1080} fill="#FFF6D0" opacity={0.25} />}
    </Whip>
  );
};
