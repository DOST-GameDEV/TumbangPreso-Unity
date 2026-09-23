import React from 'react';
import { random } from 'remotion';
import { P } from '../lib/palette';
import { chamfer, polyD, Pt } from '../lib/shapes';

/* The lata, the dust it throws up, and the pieces of the hit. Blocky and lineless. */

/**
 * The lata: a milk can, drawn as a box with a lit rim on top, two pressed ridges, a persimmon
 * label and a dent, standing on its chalk circle. Origin at the centre of its base.
 */
export const Can: React.FC<{ s?: number; lit?: 'left' | 'right' }> = ({ s = 1, lit = 'left' }) => {
  const w = 64;
  const h = 86;
  const side = lit === 'left' ? [w * 0.62, w * 0.38] : [0, w * 0.38];
  return (
    <g transform={`scale(${s})`}>
      <rect x={-w / 2} y={-h} width={w} height={h} fill="#E9D6B4" />
      <rect x={-w / 2 + side[0]} y={-h} width={side[1]} height={h} fill="#B9A07E" />
      <rect x={-w / 2} y={-h * 0.72} width={w} height={h * 0.36} fill={P.persimmon} />
      <rect x={-w / 2 + side[0]} y={-h * 0.72} width={side[1]} height={h * 0.36} fill="#C9602A" />
      <rect x={-w / 2 + 8} y={-h * 0.6} width={w * 0.38} height={7} fill={P.honey} />
      {[-h * 0.82, -h * 0.26].map((y, i) => (
        <rect key={i} x={-w / 2} y={y} width={w} height={5} fill="#A88E6C" />
      ))}
      <path d={chamfer(-w / 2 - 3, -h - 9, w + 6, 12, 4)} fill="#F6E8CC" />
      <rect x={-w / 2 + 4} y={-h - 5} width={w - 8} height={5} fill="#8C7658" />
      {/* A dent from every other time. */}
      <rect x={-w / 2 + 12} y={-h * 0.22} width={14} height={9} fill="#C9B48F" />
      <rect x={-w / 2 + 2} y={-h} width={4} height={h} fill={P.golden} opacity={lit === 'left' ? 0.9 : 0} />
    </g>
  );
};

/** A puff of court dust: stacked chamfered blocks that grow, drift and thin out. */
export const Dust: React.FC<{ t: number; x: number; y: number; s?: number; seed: string }> = ({ t, x, y, s = 1, seed }) => {
  if (t <= 0 || t >= 1) return null;
  const out: React.ReactNode[] = [];
  for (let i = 0; i < 9; i++) {
    const a = Math.PI + (i / 8) * Math.PI + (random(`${seed}a${i}`) - 0.5) * 0.4;
    const dist = (40 + random(`${seed}d${i}`) * 90) * (1 - Math.pow(1 - t, 3));
    const size = (26 + random(`${seed}s${i}`) * 30) * (0.5 + t * 0.9) * (1 - t * 0.5);
    const cx = Math.cos(a) * dist * 1.6;
    const cy = Math.sin(a) * dist * 0.6 - t * 30;
    out.push(<path key={i} d={chamfer(cx - size / 2, cy - size / 2, size, size, size * 0.3)} fill={i % 3 ? '#F2C58E' : '#E6AE74'} opacity={1 - t} />);
  }
  return <g transform={`translate(${x} ${y}) scale(${s})`}>{out}</g>;
};

/** Squares of chalk and grit kicked out of the hit, falling with gravity. */
export const Debris: React.FC<{ t: number; x: number; y: number; seed: string; n?: number }> = ({ t, x, y, seed, n = 10 }) => {
  if (t <= 0 || t >= 1.4) return null;
  const out: React.ReactNode[] = [];
  for (let i = 0; i < n; i++) {
    const vx = (random(`${seed}vx${i}`) - 0.3) * 700;
    const vy = -300 - random(`${seed}vy${i}`) * 600;
    const px = vx * t;
    const py = vy * t + 1400 * t * t;
    const sz = 6 + random(`${seed}z${i}`) * 10;
    out.push(<rect key={i} x={x + px} y={y + py} width={sz} height={sz} fill={i % 2 ? '#FFE3B8' : P.golden} transform={`rotate(${t * 400 + i * 40} ${x + px} ${y + py})`} opacity={Math.min(1, 1.4 - t)} />);
  }
  return <g>{out}</g>;
};

/** Speed lines: blocky streaks radiating from a point, for the hit and the snap. */
export const SpeedLines: React.FC<{ cx: number; cy: number; r0: number; r1: number; n: number; seed: string; opacity?: number; colour?: string; w?: number }> = ({
  cx,
  cy,
  r0,
  r1,
  n,
  seed,
  opacity = 1,
  colour = '#FFF3C8',
  w = 10,
}) => {
  const out: React.ReactNode[] = [];
  for (let i = 0; i < n; i++) {
    const a = (i / n) * Math.PI * 2 + random(`${seed}${i}`) * 0.3;
    const a0 = r0 * (0.85 + random(`${seed}o${i}`) * 0.3);
    const a1 = r1 * (0.8 + random(`${seed}p${i}`) * 0.4);
    const ww = w * (0.5 + random(`${seed}w${i}`));
    const pts: Pt[] = [
      [cx + Math.cos(a) * a0 - Math.sin(a) * ww * 0.2, cy + Math.sin(a) * a0 + Math.cos(a) * ww * 0.2],
      [cx + Math.cos(a) * a1 - Math.sin(a) * ww, cy + Math.sin(a) * a1 + Math.cos(a) * ww],
      [cx + Math.cos(a) * a1 + Math.sin(a) * ww, cy + Math.sin(a) * a1 - Math.cos(a) * ww],
      [cx + Math.cos(a) * a0 + Math.sin(a) * ww * 0.2, cy + Math.sin(a) * a0 - Math.cos(a) * ww * 0.2],
    ];
    out.push(<path key={i} d={polyD(pts)} fill={colour} />);
  }
  return <g opacity={opacity}>{out}</g>;
};
