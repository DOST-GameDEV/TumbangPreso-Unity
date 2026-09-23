import React from 'react';
import { B } from '../lib/beats';
import { Crackle } from '../lib/bolt';
import { kf, outBack, outCubic } from '../lib/kf';
import { env, onN } from '../lib/time';
import { Whip } from '../lib/whip';
import { WideSet } from '../art/roofdeck';
import { ActorImage, drawActor, DUSK, EYE_L, EYE_R, HAND_R, useActor } from '../three/actor';

/**
 * Her frames 21 to 24: his grip on the tsinelas, the pause before the snap, held on the one part
 * of him that is about to act. His grip TIGHTENS once; that is the only move in the shot.
 *
 * ⚠️ LOW AND IN FRONT, WITH HIS FACE BEHIND THE FIST. The first real-model version looked down at
 * the fist from over his shoulder, and a forearm block on a brown floor read as abstract shapes.
 * From in front, the fist and the tsinelas are big and near, and his glowing eyes sit soft behind
 * them, so the shot says whose hand this is and what he is about to do with it.
 */
export const Hand: React.FC<{ f: number }> = ({ f }) => {
  const zack = useActor('team-zack', true);
  if (!zack) return null;
  const zoom = kf(f, [[B.hand, 1], [B.reverse, 1.1]]);
  const roll = kf(f, [[B.hand, 6], [B.reverse, -3]]);
  const whipOut = f >= B.reverse - 4 ? (f - (B.reverse - 4)) / 4 : 0;
  const squeeze = kf(f, [[B.hand + 6, 0], [B.hand + 9, 1, outCubic], [B.hand + 12, 0.7, outBack]]);
  const charge = kf(f, [[B.hand, 0.5], [B.reverse - 2, 1]]);
  const step = onN(f, 2);
  const d = drawActor(zack, {
    x: 900,
    y: 700,
    ppu: 2000,
    // Wide enough that his shoulder and head never meet the edge of the rendered square.
    reach: 0.6,
    target: 'handR',
    yaw: -28,
    pitch: -6,
    roll,
    fov: 30,
    res: 1.2,
    ink: 0.004,
    face: 'glow',
    pose: { armR: [-78 - 3 * squeeze, -18, 4], armL: [0, 0, 14], head: [-6, -10, 0], torso: [2, -12, 0] },
    slipper: { at: 'hand', swing: -6 + 4 * Math.sin(f * 0.45) - 8 * squeeze, twist: -20 },
    light: { ...DUSK, dir: [-0.3, 0.5, 0.8], rim: '#F6FFA0', rim_strength: 0.7 + 0.3 * charge, flash: 0.05 * charge, flashColour: '#E8F53A' },
  });
  const fist = d.at('arm-right', HAND_R);
  const tip = d.at('arm-right', [HAND_R[0] - 0.24, 0, 0.02]);
  const eyes = [d.at('head', EYE_R), d.at('head', EYE_L)];
  const sparks: React.ReactNode[] = [];
  const n = 2 + Math.round(charge * 5);
  for (let i = 0; i < n; i++) {
    const a = step * 0.8 + i * 1.3;
    const r0 = 120 + 50 * (i % 2);
    sparks.push(<Crackle key={i} a={[fist[0] + Math.cos(a) * r0, fist[1] + Math.sin(a) * r0]} b={[fist[0] + Math.cos(a + 1.1) * (r0 + 150), fist[1] + Math.sin(a + 1.1) * (r0 + 130)]} seed={`hs${step}-${i}`} w={8} branches={2} glow={charge} />);
  }
  const jump = env(f, B.hand + 9, B.hand + 10, B.hand + 13, B.hand + 16);
  return (
    <Whip id="hwhip" blur={80 * whipOut} dx={-700 * whipOut * whipOut}>
      <g transform={`translate(960 540) scale(${zoom}) translate(-960 -540)`}>
        {/* The roofdeck behind him, far out of focus: the dusk he stands in. */}
        <g filter="url(#defocus)" transform="translate(960 540) scale(2.2) translate(-980 -470)">
          <WideSet f={f} layer="sky" />
          <WideSet f={f} layer="city" />
          <WideSet f={f} layer="mid" />
        </g>
        <rect x={-200} y={-200} width={2320} height={1480} fill="#3A0610" opacity={0.3} />
        {eyes.map((e, i) => <circle key={i} cx={e[0]} cy={e[1]} r={40} fill="#E8F53A" opacity={0.4} filter="url(#glowBig)" />)}
        <ActorImage d={d} />
        {sparks}
        {/* A single arc jumps from his fist to the sole as the grip closes. */}
        {jump > 0 && <Crackle a={fist} b={tip} seed={`hj${step}`} w={14} branches={3} glow={1.4} opacity={jump} />}
        <rect x={-200} y={-200} width={2320} height={1480} fill="url(#vignette)" />
      </g>
    </Whip>
  );
};
