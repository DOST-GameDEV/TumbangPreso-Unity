import React from 'react';
import { B } from '../lib/beats';
import { Crackle } from '../lib/bolt';
import { kf, outBack, outCubic } from '../lib/kf';
import { env, onN } from '../lib/time';
import { chamfer, polyD } from '../lib/shapes';
import { Whip } from '../lib/whip';
import { ActorImage, drawActor, DUSK, HAND_R, useActor } from '../three/actor';

/**
 * Her frames 21 to 24: looking down past his right shoulder at his grip on the tsinelas, the
 * court far below and out of focus. His grip TIGHTENS once, which is the only move in the shot:
 * the pause before the snap, held on the one part of him that is about to act.
 *
 * ⚠️ HIS ACTUAL FIST AND THE GAME'S ACTUAL TSINELAS. The camera is aimed at the end of his right
 * arm block from above and behind, so the shot is his model, not a drawing of a hand (🧑: *"our
 * characters DONT have fingers"*; the model's hand is one block, and so is this one).
 */
export const Hand: React.FC<{ f: number }> = ({ f }) => {
  const zack = useActor('team-zack', true);
  if (!zack) return null;
  const zoom = kf(f, [[B.hand, 1], [B.reverse, 1.12]]);
  const roll = kf(f, [[B.hand, -9], [B.reverse, 4]]);
  const whipOut = f >= B.reverse - 4 ? (f - (B.reverse - 4)) / 4 : 0;
  const squeeze = kf(f, [[B.hand + 6, 0], [B.hand + 9, 1, outCubic], [B.hand + 12, 0.7, outBack]]);
  const charge = kf(f, [[B.hand, 0.4], [B.reverse - 2, 1]]);
  const step = onN(f, 2);
  const d = drawActor(zack, {
    x: 880,
    y: 460,
    ppu: 4200,
    reach: 0.16,
    target: 'handR',
    yaw: 118,
    pitch: 40,
    roll,
    fov: 28,
    res: 1.3,
    ink: 0.0035,
    pose: { armR: [-28 + 4 * squeeze, 4, 16 - 3 * squeeze], torso: [4, -6, 0], head: [8, 0, 0] },
    slipper: { at: 'hand', swing: -8 + 5 * Math.sin(f * 0.4) - 6 * squeeze, twist: 12 },
    light: { ...DUSK, rim: '#F6FFA0', rim_strength: 0.6 + 0.4 * charge, flash: 0.06 * charge, flashColour: '#E8F53A' },
  });
  const fist = d.at('arm-right', HAND_R);
  const tip = d.at('arm-right', [HAND_R[0] - 0.27, 0, 0.02]);
  const sparks: React.ReactNode[] = [];
  const n = 2 + Math.round(charge * 5);
  for (let i = 0; i < n; i++) {
    const a = step * 0.8 + i * 1.3;
    const r0 = 150 + 60 * (i % 2);
    sparks.push(<Crackle key={i} a={[fist[0] + Math.cos(a) * r0, fist[1] + Math.sin(a) * r0]} b={[fist[0] + Math.cos(a + 1.1) * (r0 + 170), fist[1] + Math.sin(a + 1.1) * (r0 + 150)]} seed={`hs${step}-${i}`} w={8} branches={2} glow={charge} />);
  }
  const jump = env(f, B.hand + 9, B.hand + 10, B.hand + 13, B.hand + 16);
  return (
    <Whip id="hwhip" blur={80 * whipOut} dx={-700 * whipOut * whipOut}>
      <g transform={`translate(960 540) scale(${zoom}) translate(-960 -540)`}>
        {/* The court far below, in his shadow: big soft blocks, the chalk line, his sneaker. */}
        <g filter="url(#defocus)">
          <rect x={-100} y={-100} width={2120} height={1280} fill="#5E261A" />
          <path d={polyD([[-100, 820], [2020, 520], [2020, 600], [-100, 900]])} fill="#E8B484" opacity={0.55} />
          <path d={polyD([[-100, 300], [700, -100], [1100, -100], [-100, 520]])} fill="#7A3622" />
          <path d={chamfer(1380, 820, 300, 150, 30)} fill="#E8C21E" />
          <path d={chamfer(1360, 950, 340, 60, 20)} fill="#F4E8D6" />
          <path d={polyD([[1300, 1180], [2020, 700], [2020, 1180]])} fill="#C97C4C" opacity={0.7} />
        </g>
        <ActorImage d={d} />
        {sparks}
        {/* A single arc jumps from his fist to the sole as the grip closes. */}
        {jump > 0 && <Crackle a={fist} b={tip} seed={`hj${step}`} w={14} branches={3} glow={1.4} opacity={jump} />}
        <rect x={-200} y={-200} width={2320} height={1480} fill="url(#vignette)" />
      </g>
    </Whip>
  );
};
