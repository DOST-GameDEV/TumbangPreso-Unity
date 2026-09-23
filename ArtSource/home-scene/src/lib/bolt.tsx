import React from 'react';
import { random } from 'remotion';
import { P } from './palette';

export type Pt = [number, number];

/**
 * ⚠️ LORE.md: Zack's electricity is NARROW and BRANCHING. Midpoint displacement gives the
 * zigzag; `jag` is the sideways throw as a fraction of the segment, and it halves at every
 * subdivision so the bolt stays one readable line rather than a scribble.
 */
export const boltPoints = (a: Pt, b: Pt, seed: string, depth = 5, jag = 0.28): Pt[] => {
  let pts: Pt[] = [a, b];
  let amp = jag;
  for (let d = 0; d < depth; d++) {
    const next: Pt[] = [pts[0]];
    for (let i = 0; i < pts.length - 1; i++) {
      const [x1, y1] = pts[i];
      const [x2, y2] = pts[i + 1];
      const mx = (x1 + x2) / 2;
      const my = (y1 + y2) / 2;
      const dx = x2 - x1;
      const dy = y2 - y1;
      const len = Math.hypot(dx, dy) || 1;
      const off = (random(`${seed}-${d}-${i}`) - 0.5) * 2 * amp * len;
      next.push([mx + (-dy / len) * off, my + (dx / len) * off], pts[i + 1]);
    }
    pts = next;
    amp *= 0.55;
  }
  return pts;
};

export const branchesFor = (main: Pt[], seed: string, count = 2, reach = 0.35): Pt[][] => {
  const out: Pt[][] = [];
  const total = main.length;
  for (let k = 0; k < count; k++) {
    const i = Math.floor(total * (0.25 + 0.5 * random(`${seed}-bi-${k}`)));
    const [x, y] = main[i];
    const [x2, y2] = main[Math.min(total - 1, i + 3)];
    const ang = Math.atan2(y2 - y, x2 - x) + (random(`${seed}-ba-${k}`) > 0.5 ? 1 : -1) * (0.5 + 0.5 * random(`${seed}-bb-${k}`));
    const segLen = Math.hypot(main[total - 1][0] - main[0][0], main[total - 1][1] - main[0][1]) * reach * (0.5 + 0.5 * random(`${seed}-bl-${k}`));
    out.push(boltPoints([x, y], [x + Math.cos(ang) * segLen, y + Math.sin(ang) * segLen], `${seed}-br-${k}`, 3, 0.3));
  }
  return out;
};

const toD = (pts: Pt[]) => pts.map(([x, y], i) => `${i ? 'L' : 'M'}${x.toFixed(1)} ${y.toFixed(1)}`).join('');

/**
 * Lineless like everything else: Zack's electric yellow over a soft golden glow, with a
 * near-white core. The glow is what separates it from a bright sky, not an outline.
 */
export const Bolt: React.FC<{ pts: Pt[]; w?: number; glow?: number; opacity?: number; outline?: string | null }> = ({
  pts,
  w = 6,
  glow = 1,
  opacity = 1,
  outline,
}) => {
  const d = toD(pts);
  return (
    <g opacity={opacity}>
      {glow > 0 && (
        <path d={d} fill="none" stroke={P.golden} strokeWidth={w * 5} strokeLinecap="round" strokeLinejoin="round" opacity={0.45 * glow} filter="url(#glow)" />
      )}
      {outline && <path d={d} fill="none" stroke={outline} strokeWidth={w + 5} strokeLinecap="round" strokeLinejoin="miter" />}
      <path d={d} fill="none" stroke={P.electric} strokeWidth={w} strokeLinecap="round" strokeLinejoin="miter" />
      <path d={d} fill="none" stroke={P.boltCore} strokeWidth={Math.max(1.2, w * 0.38)} strokeLinecap="round" strokeLinejoin="miter" />
    </g>
  );
};

/** A small crackle between two points: one main bolt plus a branch, redrawn every other frame. */
export const Crackle: React.FC<{ a: Pt; b: Pt; seed: string; w?: number; branches?: number; glow?: number; opacity?: number }> = ({
  a,
  b,
  seed,
  w = 4,
  branches = 1,
  glow = 1,
  opacity = 1,
}) => {
  const main = boltPoints(a, b, seed, 4, 0.3);
  const br = branchesFor(main, seed, branches, 0.4);
  return (
    <g opacity={opacity}>
      {br.map((p, i) => (
        <Bolt key={i} pts={p} w={w * 0.6} glow={glow * 0.6} />
      ))}
      <Bolt pts={main} w={w} glow={glow} />
    </g>
  );
};
