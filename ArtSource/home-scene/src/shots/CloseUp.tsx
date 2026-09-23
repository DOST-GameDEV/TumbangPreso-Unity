import React from 'react';
import { random } from 'remotion';
import { B, bt, K } from '../lib/beats';
import { Crackle, Pt } from '../lib/bolt';
import { kf, outBack, outCubic } from '../lib/kf';
import { onN } from '../lib/time';
import { P } from '../lib/palette';
import { ActorImage, drawActor, DUSK, EYE_L, EYE_R, Face, HEAD_TOP, Pose, useActor } from '../three/actor';
import { Cam, layerTransform, Wide, ZACK_AT, zackAnchor } from './Wide';

/**
 * Her frames 1 to 20: the face. The wide's own figure and set, pushed in, so the close-up is
 * the same Zack in the same place by construction.
 */
const FACE_Y = ZACK_AT.y - 0.47 * ZACK_AT.ppu;

// ⚠️ The close-up zooms are written for a 1000 ppu figure; this keeps his face the same size
// in frame whatever the wide's `ZACK_AT.ppu` is.
export const CU_CAM = (f: number): Cam => {
  const c = cuCam(f);
  return { ...c, zoom: (c.zoom * 1000) / ZACK_AT.ppu };
};

const cuCam = (f: number): Cam => {
  const drift = kf(f, [[B.cu, 0], [B.powered + bt(18), 1]]);
  // ⚠️ The eyes opening is a CRASH ZOOM: four frames in on the eyes with an overshoot, the
  // camera's version of the snap. Then the powered close-up rolls and shakes.
  const crash = kf(f, [[B.eyesOpen, 0], [B.eyesOpen + bt(4), 1, outBack], [B.black, 1]]);
  if (f >= B.eyesOpen && f < B.black) {
    return { zoom: 2.3 + 0.9 * crash, cx: 958, cy: FACE_Y + 10 + 10 * crash, rot: -2 + 3 * crash };
  }
  if (f >= B.powered) {
    const shake = onN(f, 2);
    return { zoom: kf(f, [[B.powered, 2.9], [B.hand, 2.45, outCubic]]), cx: 956 + (random(`cux${shake}`) - 0.5) * 10, cy: FACE_Y + 14 + (random(`cuy${shake}`) - 0.5) * 10, rot: kf(f, [[B.powered, 3], [B.hand, -4]]) };
  }
  return { zoom: 2.1 + drift * 0.12, cx: 958, cy: FACE_Y - 10 - drift * 6, rot: -5 + 3 * drift };
};

/** The face pose: square to us, chin up into the wind. */
const cuPose = (f: number): { pose: Pose; face: Face; yaw: number } => {
  const base: Pose = { head: [-8, 2, 0], torso: [-2, 0, 0], armR: [0, 0, 18], armL: [0, 0, 18] };
  if (f < B.eyesOpen) {
    // Eyes shut into the gust, the head nodding a hair in it.
    return { pose: { ...base, head: [-8 + 1.2 * Math.sin(f * 0.5), 2 + 1.5 * Math.sin(f * 0.31), 0.8 * Math.sin(f * 0.43)] }, face: 'rest', yaw: 2 };
  }
  // ⚠️ The eyes-open beat is his own eyes IGNITING, on the crash zoom: his eyes never change shape.
  if (f < B.black) return { pose: { ...base, head: [-10, 0, 0] }, face: 'glow', yaw: 0 };
  const shake = onN(f, 2);
  return {
    pose: { ...base, head: [-10 + (random(`hx${shake}`) - 0.5) * 3, (random(`hy${shake}`) - 0.5) * 3, (random(`hz${shake}`) - 0.5) * 2] },
    face: 'glow',
    yaw: 0,
  };
};

/** Wind streaks: blocky dashes racing across the frame with the gust. */
export const WindStreaks: React.FC<{ f: number; amount: number; seed?: string }> = ({ f, amount, seed = 'ws' }) => {
  if (amount <= 0.02) return null;
  const out: React.ReactNode[] = [];
  for (let i = 0; i < 16; i++) {
    const y = 60 + random(`${seed}y${i}`) * 960;
    const speed = 90 + random(`${seed}s${i}`) * 80;
    const len = 120 + random(`${seed}l${i}`) * 260;
    const x = ((f * speed + random(`${seed}x${i}`) * 2400) % 2600) - 400;
    out.push(<rect key={i} x={x} y={y} width={len} height={6 + random(`${seed}h${i}`) * 8} fill="#FFE6B0" opacity={0.35 * amount} />);
  }
  return <g>{out}</g>;
};

export const CloseUp: React.FC<{ f: number }> = ({ f }) => {
  const zack = useActor('team-zack', true);
  const cam = CU_CAM(f);
  if (!zack) return null;
  const c = cuPose(f);
  const powered = f >= B.powered;
  const d = drawActor(zack, {
    ...zackAnchor(0, 0.47),
    focus: 0.47,
    reach: 0.34,
    ppu: ZACK_AT.ppu,
    yaw: c.yaw,
    pose: c.pose,
    face: c.face,
    slipper: { at: 'hand' },
    res: 1.35 * cam.zoom,
    ink: 0.006,
    light: powered ? { ...DUSK, rim: '#F6FFA0', rim_strength: 1, flash: 0.08, flashColour: P.electric } : DUSK,
  });
  const step = onN(f, 2);
  const over: React.ReactNode[] = [];
  if (powered) {
    // Her frame 20: arcs crackling off both sides of his head and up out of his hair.
    const top = d.at('head', HEAD_TOP);
    for (const [side, eye] of [[-1, d.at('head', EYE_R)], [1, d.at('head', EYE_L)]] as [number, Pt][]) {
      for (let i = 0; i < 3; i++) {
        const y = eye[1] - 120 + i * 90 + (random(`cs${step}${side}${i}`) - 0.5) * 40;
        const x0 = eye[0] + side * 190;
        over.push(<Crackle key={`${side}${i}`} a={[x0, y]} b={[x0 + side * (70 + random(`cl${step}${i}`) * 90), y - 30 + random(`cm${step}${i}`) * 60]} seed={`cu${step}${side}${i}`} w={4} branches={2} glow={1.2} />);
      }
    }
    for (let i = 0; i < 3; i++) {
      const a = -Math.PI / 2 + (i - 1) * 0.6 + (random(`ht${step}${i}`) - 0.5) * 0.4;
      over.push(<Crackle key={`t${i}`} a={[top[0] + (i - 1) * 60, top[1] + 30]} b={[top[0] + (i - 1) * 60 + Math.cos(a) * 110, top[1] + 30 + Math.sin(a) * 110]} seed={`ct${step}${i}`} w={4} branches={1} glow={1.1} />);
    }
    // The eyes throw a little light.
    for (const e of [d.at('head', EYE_R), d.at('head', EYE_L)]) over.push(<circle key={`g${e[0]}`} cx={e[0]} cy={e[1]} r={30} fill={P.electric} opacity={0.6} filter="url(#glowBig)" />);
  }
  const open = f >= B.eyesOpen && f < B.eyesOpen + bt(3);
  return (
    <g>
      <Wide f={f} cam={cam} figure={<ActorImage d={d} />} over={over} />
      {powered && <rect x={0} y={0} width={1920} height={1080} fill="#3A0610" opacity={0.12} />}
      <WindStreaks f={f} amount={1} />
      {open && <rect x={0} y={0} width={1920} height={1080} fill="#FFF3C8" opacity={0.18} />}
      <rect x={0} y={0} width={1920} height={1080} fill="url(#vignette)" />
    </g>
  );
};

/**
 * Her frames 16 to 18, the impact frames: flat colour, nothing on screen but his eyes. Black
 * with the eyes glowing, then a golden flash with the eyes and brows in ink.
 *
 * ⚠️ THE EYES ARE HIS, NOT DRAWN OVER HIM. The figure is rendered washed to exactly the frame's
 * colour with no ink hull, so the body vanishes into the flat field and only the face decals
 * (which the wash does not touch) remain, exactly where his eyes are in the close-up either side.
 */
export const Impact: React.FC<{ f: number }> = ({ f }) => {
  const zack = useActor('team-zack', true);
  if (!zack) return null;
  const black = f < B.yellow;
  const bg = black ? '#0B0306' : '#FFC23A';
  const cam = CU_CAM(B.eyesOpen + bt(6));
  const d = drawActor(zack, {
    ...zackAnchor(0, 0.47),
    focus: 0.47,
    reach: 0.34,
    ppu: ZACK_AT.ppu,
    pose: { head: [-10, 0, 0] },
    // 🧑 2026-09-23 on the black frame with only the arcs showing: *"this is amazing i love it"*.
    // So the black frame keeps his eyes in the dark; the gold frame shows them in ink.
    face: black ? 'rest' : 'ink',
    res: 1.35 * cam.zoom,
    ink: 0,
    light: { ...DUSK, flash: 1, flashColour: bg },
  });
  const flick = f % 2 === 0;
  const eyes = [d.at('head', EYE_R), d.at('head', EYE_L)];
  const t = (p: Pt): Pt => {
    // The eye points live in the wide's space; map them through the close-up camera.
    const z = cam.zoom;
    return [960 + (p[0] - cam.cx) * z, 540 + (p[1] - cam.cy) * z];
  };
  const se = eyes.map(t);
  return (
    <g>
      <rect x={0} y={0} width={1920} height={1080} fill={bg} />
      <g transform={layerTransform({ ...cam, rot: 0 }, 1)}>
        <ActorImage d={d} />
      </g>
      {!black && <rect x={0} y={0} width={1920} height={1080} fill={P.electric} opacity={0.3} />}
      {black && flick && (
        <>
          <Crackle a={[se[0][0] - 60, se[0][1]]} b={[se[0][0] - 260, se[0][1] - 70]} seed={`ib${f}`} w={8} branches={1} glow={1} />
          <Crackle a={[se[1][0] + 60, se[1][1]]} b={[se[1][0] + 260, se[1][1] - 70]} seed={`ic${f}`} w={8} branches={1} glow={1} />
        </>
      )}
    </g>
  );
};
