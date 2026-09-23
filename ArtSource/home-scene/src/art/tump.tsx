import React from 'react';
import { random } from 'remotion';
import { P } from '../lib/palette';
import { kf, outBack } from '../lib/kf';
import { onN } from '../lib/time';
import { Burst } from './burst';
import { SpeedLines } from './props';

/**
 * TUMP!, the sound the lata makes, lettered the way the hit feels. Her storyboard ends on it.
 *
 * ⚠️ NO OUTLINE (🧑 2026-09-23). The letters are the game's display face, Darumadrop One, in
 * cream, and they read off the sky by a blocky EXTRUSION underneath: the same letter stacked in
 * persimmon then rim red, stepping down and right. That is a side, not a line round the letter.
 * Each letter lands two frames after the last with an overshoot, then they jitter on twos.
 */
const LETTERS = ['T', 'U', 'M', 'P', '!'];
const ADV = [0, 150, 292, 468, 602];

export const Tump: React.FC<{ f: number; start: number; x: number; y: number; size?: number }> = ({ f, start, x, y, size = 230 }) => {
  if (f < start) return null;
  const t = f - start;
  const burstS = kf(f, [[start, 0.2], [start + 5, 1.15, outBack], [start + 12, 1]]);
  const jitterStep = onN(f, 2);
  return (
    <g transform={`translate(${x} ${y})`}>
      <g transform={`translate(300 -60) scale(${burstS})`} opacity={0.95}>
        <SpeedLines cx={0} cy={0} r0={330} r1={620} n={22} seed="tl" opacity={Math.max(0, 1 - t / 30)} colour="#FFE3A0" w={14} />
        <Burst r={330} arms={9} seed="tumpb" rot={-8 + t * 0.4} inner={0.62} fill={P.golden} shade="#E0951C" swirl={P.goldenSwirl} />
      </g>
      {LETTERS.map((ch, i) => {
        const t0 = start + i * 2;
        if (f < t0) return null;
        const sc = kf(f, [[t0, 0.1], [t0 + 4, 1.34, outBack], [t0 + 8, 1]]);
        const jx = (random(`tj${jitterStep}${i}`) - 0.5) * 6;
        const jy = (random(`tk${jitterStep}${i}`) - 0.5) * 6;
        const rot = (i % 2 ? 7 : -6) + (random(`tr${i}`) - 0.5) * 6;
        const lift = i === 4 ? -10 : i % 2 ? 12 : -8;
        const common = { fontFamily: 'Darumadrop One', fontSize: size, textAnchor: 'middle' as const };
        return (
          <g key={i} transform={`translate(${ADV[i] + jx} ${lift + jy}) rotate(${rot}) scale(${sc})`}>
            {[10, 8, 6, 4, 2].map((d, k) => (
              <text key={k} x={d * 1.6} y={d * 2.2} fill={k < 2 ? '#7A0612' : k < 4 ? P.rimRed : P.persimmon} {...common}>
                {ch}
              </text>
            ))}
            <text x={0} y={0} fill="#FFF3DC" {...common}>
              {ch}
            </text>
            {/* Her lower-half shade on each letter, the honey quartz of the logo's cream. */}
            <clipPath id={`tumpclip${i}`}>
              <rect x={-200} y={-size * 0.28} width={400} height={size} />
            </clipPath>
            <text x={0} y={0} fill={P.honey} clipPath={`url(#tumpclip${i})`} {...common}>
              {ch}
            </text>
          </g>
        );
      })}
    </g>
  );
};
