import React from 'react';
import { AbsoluteFill } from 'remotion';
import { ActorImage, ActorProps, drawActor, useActor } from './three/actor';

// Test board: the real models at the angles the loop uses. Not part of the loop.
export const ActorTest: React.FC = () => {
  const zack = useActor('team-zack', true);
  if (!zack) return null;
  const cells: [string, Omit<ActorProps, 'x' | 'y'>][] = [
    ['slipper free front', { ppu: 1400, reach: 0.16, focus: 0.4, pitch: 40, slipper: { at: 'free', pos: [0, 0.4, 0.4], rot: [0, 0, 0] } }],
    ['slipper free rotx90', { ppu: 1400, reach: 0.16, focus: 0.4, pitch: 10, slipper: { at: 'free', pos: [0, 0.4, 0.4], rot: [90, 0, 0] } }],
  ];
  return (
    <AbsoluteFill style={{ background: 'linear-gradient(#C8452A, #F2A15A)' }}>
      <svg width={1920} height={1080}>
        {cells.map(([label, props], i) => {
          const d = drawActor(zack, { ...props, x: 240 + (i % 4) * 480, y: 270 + Math.floor(i / 4) * 540 });
          const h = d.at('arm-right', [-0.2, 0, 0.02]);
          return (
            <g key={i}>
              <ActorImage d={d} />
              <circle cx={h[0]} cy={h[1]} r={6} fill="#00ff66" />
              <text x={240 + (i % 4) * 480} y={520 + Math.floor(i / 4) * 540} textAnchor="middle" fontSize={26} fill="#fff" fontFamily="sans-serif">{label}</text>
            </g>
          );
        })}
      </svg>
    </AbsoluteFill>
  );
};
