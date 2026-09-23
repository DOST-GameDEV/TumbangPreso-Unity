import React from 'react';
import { random } from 'remotion';
import { P } from '../lib/palette';
import { chamfer } from '../lib/shapes';

/**
 * Her burst, from `slipper with hit` and the corner of the logo, made blocky and lineless: an
 * irregular star of straight edges whose arms are not the same length, a golden fill, and a
 * SQUARE spiral inside it in the darker gold (her swirl, squared off to match the look). It
 * is the sun in the wide and the hit on the can: the thing that lights the court is the thing
 * a good throw makes.
 */
export const burstPts = (arms: number, rIn: number, rOut: number, seed: string, wobble = 0.2) => {
  const pts: [number, number][] = [];
  for (let i = 0; i < arms; i++) {
    const a = (i / arms) * Math.PI * 2 - Math.PI / 2 + (random(`${seed}a${i}`) - 0.5) * 0.22;
    const out = rOut * (1 - wobble + 2 * wobble * random(`${seed}o${i}`));
    const aIn = a + Math.PI / arms;
    const inn = rIn * (0.92 + 0.16 * random(`${seed}i${i}`));
    pts.push([Math.cos(a) * out, Math.sin(a) * out], [Math.cos(aIn) * inn, Math.sin(aIn) * inn]);
  }
  return pts;
};

export const burstPath = (arms: number, rIn: number, rOut: number, seed: string, wobble = 0.2) =>
  burstPts(arms, rIn, rOut, seed, wobble)
    .map(([x, y], i) => `${i ? 'L' : 'M'}${x.toFixed(1)} ${y.toFixed(1)}`)
    .join('') + 'Z';

export const squareSpiral = (r: number, step: number) => {
  const d: string[] = ['M 0 0'];
  let x = 0;
  let y = 0;
  let len = step;
  const dirs = [
    [1, 0],
    [0, 1],
    [-1, 0],
    [0, -1],
  ];
  for (let i = 0; Math.abs(x) < r && Math.abs(y) < r; i++) {
    const [dx, dy] = dirs[i % 4];
    x += dx * len;
    y += dy * len;
    d.push(`L ${x} ${y}`);
    if (i % 2 === 1) len += step;
  }
  return d.join(' ');
};

export const Burst: React.FC<{
  r: number;
  arms?: number;
  seed?: string;
  rot?: number;
  fill?: string;
  inner?: number;
  swirl?: string | null;
  shade?: string;
}> = ({ r, arms = 7, seed = 'b', rot = 0, fill = P.golden, inner = 0.56, swirl = P.goldenSwirl, shade = '#E0951C' }) => {
  const pts = burstPts(arms, r * inner, r, seed);
  const d = pts.map(([x, y], i) => `${i ? 'L' : 'M'}${x.toFixed(1)} ${y.toFixed(1)}`).join('') + 'Z';
  // Each arm is split along its spine into a lit half and a shade half: the blocky way to say
  // the star has a form, where an outlined star would say it with a line.
  const halves: React.ReactNode[] = [];
  for (let i = 0; i < pts.length; i += 2) {
    const tip = pts[i];
    const next = pts[(i + 1) % pts.length];
    halves.push(<path key={i} d={`M 0 0 L ${tip[0]} ${tip[1]} L ${next[0]} ${next[1]} Z`} fill={shade} />);
  }
  return (
    <g transform={`rotate(${rot})`}>
      <path d={d} fill={fill} />
      {halves}
      <clipPath id={`burst-${seed}`}>
        <path d={d} />
      </clipPath>
      {swirl && (
        <g clipPath={`url(#burst-${seed})`}>
          {/* Her swirl, squared off: two concentric chamfered rings, turned against the star. */}
          <path d={chamfer(-r * inner * 0.72, -r * inner * 0.72, r * inner * 1.44, r * inner * 1.44, r * inner * 0.3)} fill="none" stroke={swirl} strokeWidth={r * 0.07} transform="rotate(22)" />
          <path d={chamfer(-r * inner * 0.36, -r * inner * 0.36, r * inner * 0.72, r * inner * 0.72, r * inner * 0.15)} fill={swirl} transform="rotate(-12)" />
        </g>
      )}
    </g>
  );
};
