import React from 'react';
import { AbsoluteFill, useCurrentFrame } from 'remotion';
import { ActorImage, ActorProps, drawActor, useActor } from '../three/actor';

/**
 * Phaister's model test board (docs/HOME_SCREEN_ANIMATION_METHOD.md § 2.2: "always build a test
 * board first"). Not part of the loop. Frame N picks page N of the cells below.
 */
type Cell = [string, Omit<ActorProps, 'x' | 'y' | 'ppu'>];

const PAGES: Cell[][] = [
  [
    ['front rest', { yaw: 0 }],
    ['yaw 25', { yaw: 25 }],
    ['yaw 90', { yaw: 90 }],
    ['back 180', { yaw: 180 }],
    ['glow front', { yaw: 10, face: 'glow', eyeColour: '#F444D4' }],
    ['idle clip .5', { yaw: 20, clip: 'idle', t: 0.5 }],
    ['walk clip .3', { yaw: 60, clip: 'walk', t: 0.3 }],
    ['pitch -14 low', { yaw: 15, pitch: -14 }],
  ],
  [
    ['hex .15', { yaw: 20, clip: 'hero-phaister-hex', t: 0.15 }],
    ['hex .3', { yaw: 20, clip: 'hero-phaister-hex', t: 0.3 }],
    ['hex .45', { yaw: 20, clip: 'hero-phaister-hex', t: 0.45 }],
    ['hex .55', { yaw: 20, clip: 'hero-phaister-hex', t: 0.55 }],
    ['blink .1', { yaw: 20, clip: 'hero-phaister-blink', t: 0.1 }],
    ['blink .25', { yaw: 20, clip: 'hero-phaister-blink', t: 0.25 }],
    ['blink .35', { yaw: 20, clip: 'hero-phaister-blink', t: 0.35 }],
    ['blink .42', { yaw: 20, clip: 'hero-phaister-blink', t: 0.42 }],
  ],
  [
    ['eclipse .1', { yaw: 10, clip: 'hero-phaister-eclipse', t: 0.1 }],
    ['eclipse .3', { yaw: 10, clip: 'hero-phaister-eclipse', t: 0.3 }],
    ['eclipse .45', { yaw: 10, clip: 'hero-phaister-eclipse', t: 0.45 }],
    ['eclipse .6', { yaw: 10, clip: 'hero-phaister-eclipse', t: 0.6 }],
    ['eclipse .7', { yaw: 10, clip: 'hero-phaister-eclipse', t: 0.7 }],
    ['eclipse .85', { yaw: 10, clip: 'hero-phaister-eclipse', t: 0.85 }],
    ['eclipse .95', { yaw: 10, clip: 'hero-phaister-eclipse', t: 0.95 }],
    ['eclipse low', { yaw: 10, clip: 'hero-phaister-eclipse', t: 0.7, pitch: -16 }],
  ],
  [
    ['R x-90', { pose: { armR: [-90, 0, 0] } }],
    ['R x-165', { pose: { armR: [-165, 0, 0] } }],
    ['R z90', { pose: { armR: [0, 0, 90] } }],
    ['R z160', { pose: { armR: [0, 0, 160] } }],
    ['both x-150 z40', { pose: { armR: [-150, 0, 40], armL: [-150, 0, 40] } }],
    ['OPEN -10,0,128', { pose: { armR: [-10, 0, 128], armL: [-10, 0, 128] } }],
    ['both z150', { pose: { armR: [0, 0, 150], armL: [0, 0, 150] } }],
    ['R -30,0,45', { pose: { armR: [-30, 0, 45] } }],
  ],
];

export const PhaisterBoard: React.FC<{ cells?: Cell[] }> = ({ cells }) => {
  const f = useCurrentFrame();
  const ph = useActor('team-phaister', true);
  if (!ph) return null;
  const list = cells ?? PAGES[f % PAGES.length];
  return (
    <AbsoluteFill style={{ background: 'linear-gradient(#3A0A2A, #8A3A3A)' }}>
      <svg width={1920} height={1080}>
        {list.map(([label, props], i) => {
          const cx = 240 + (i % 4) * 480;
          const cy = 250 + Math.floor(i / 4) * 540;
          const d = drawActor(ph, { focus: 0.5, reach: 0.6, pitch: 4, slipper: { at: 'hand' }, ...props, x: cx, y: cy, ppu: 400 });
          return (
            <g key={i}>
              <ActorImage d={d} />
              <text x={cx} y={cy + 270} textAnchor="middle" fontSize={24} fill="#fff" fontFamily="sans-serif">{label}</text>
            </g>
          );
        })}
      </svg>
    </AbsoluteFill>
  );
};
