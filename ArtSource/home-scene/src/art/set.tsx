import React from 'react';
import { random } from 'remotion';
import { P } from '../lib/palette';
import { loopNoise, loopSin, LOOP, clamp01 } from '../lib/time';
import { flutter, wind } from '../lib/wind';
import { chamfer, polyD, Pt } from '../lib/shapes';

type XY = { x: number; y: number };

/*
 * The set pieces, blocky and LINELESS (🧑 2026-09-23: no outlines, "he should atleast be
 * blocky"). Every piece separates from what is behind it by value: a face, a darker plane, and
 * where the low sun catches it, a golden rim on the edge that faces the light.
 */

/* ------------------------------------------------------------------ clouds */

/**
 * A blocky cloud: a stack of chamfered slabs on one flat base, lit from below by the sunset,
 * which at dusk is the bright side. `slabs` are [dx, dy, w, h] off the base line.
 */
export const Cloud: React.FC<{
  x: number;
  y: number;
  slabs: [number, number, number, number][];
  fill: string;
  under: string;
  top?: string;
  opacity?: number;
}> = ({ x, y, slabs, fill, under, top, opacity = 1 }) => (
  <g transform={`translate(${x} ${y})`} opacity={opacity}>
    {slabs.map(([dx, dy, w, h], i) => (
      <path key={i} d={chamfer(dx, dy - h, w, h, Math.min(16, h / 3))} fill={fill} />
    ))}
    {top &&
      slabs.map(([dx, dy, w, h], i) => <path key={'t' + i} d={chamfer(dx + 10, dy - h, w - 20, 8, 3)} fill={top} />)}
    {slabs
      .filter(([, dy]) => dy >= -2)
      .map(([dx, dy, w], i) => (
        <path key={'u' + i} d={chamfer(dx + 4, dy - 16, w - 8, 16, 6)} fill={under} />
      ))}
  </g>
);

/**
 * ⚠️ SEAMLESS CLOUDS. A cloud that crosses the frame and wraps would have to cross it once per
 * loop, and 30 s for a screen width is far faster than the Slay the Spire 2 drift the owner
 * asked for. So each cloud drifts a short way over one loop, and forms and dissolves at the two
 * ends of its trip, with the four of them a quarter of a loop apart. Only one is ever fading.
 */
export const CloudDrift: React.FC<{ f: number; phase: number; children: (dx: number, alpha: number) => React.ReactNode; travel?: number }> = ({
  f,
  phase,
  children,
  travel = 220,
}) => {
  const t = (((f / LOOP + phase) % 1) + 1) % 1;
  const alpha = clamp01(t / 0.16) * clamp01((1 - t) / 0.16);
  return <>{children(-travel / 2 + travel * t, alpha)}</>;
};

/* ------------------------------------------------------------------ skyline */

export type TowerSpec = {
  x: number;
  w: number;
  top: number;
  base: number;
  fill: string;
  shade: string;
  win: string;
  lit: string;
  crown?: 'flat' | 'step' | 'slant' | 'mast' | 'tank';
  cols?: number;
  seed: string;
};

/**
 * Pasig condo towers. ⚠️ Generic on purpose: LORE.md says Sa Bubong is a FICTIONAL condo, and
 * these are not a claim to be any real skyline. Windows come on across the loop as the sun goes
 * down, each on its own periodic flicker, so the city never snaps at the seam.
 */
export const Tower: React.FC<{ t: TowerSpec; f: number; sunX?: number; rim?: number }> = ({ t, f, sunX = 960, rim = 0.8 }) => {
  const h = t.base - t.top;
  const cols = t.cols ?? Math.max(2, Math.round(t.w / 26));
  const gapX = t.w / cols;
  const rowH = 22;
  const rows = Math.floor((h - 30) / rowH);
  const litRight = t.x + t.w / 2 < sunX;
  const wins: React.ReactNode[] = [];
  for (let r = 0; r < rows; r++) {
    for (let c = 0; c < cols; c++) {
      const k = `${t.seed}-${r}-${c}`;
      const on = random(k) > 0.62 ? clamp01(0.5 + 0.9 * loopNoise(k, f, 0.9)) : 0;
      const wx = t.x + c * gapX + gapX * 0.26;
      const wy = t.top + 22 + r * rowH;
      wins.push(<rect key={k} x={wx} y={wy} width={gapX * 0.48} height={9} fill={t.win} />);
      if (on > 0.05) wins.push(<rect key={k + 'l'} x={wx} y={wy} width={gapX * 0.48} height={9} fill={t.lit} opacity={on} />);
    }
  }
  const crown = (() => {
    switch (t.crown) {
      case 'step':
        return <rect x={t.x + t.w * 0.18} y={t.top - 26} width={t.w * 0.64} height={28} fill={t.fill} />;
      case 'slant':
        return <polygon points={`${t.x},${t.top} ${t.x + t.w},${t.top - t.w * 0.35} ${t.x + t.w},${t.top}`} fill={t.fill} />;
      case 'mast': {
        const blink = loopSin(f, 15) > 0.55;
        return (
          <g>
            <rect x={t.x + t.w / 2 - 3} y={t.top - 70} width={6} height={70} fill={t.shade} />
            <rect x={t.x + t.w / 2 - 6} y={t.top - 78} width={12} height={10} fill={blink ? P.rimRed : t.shade} />
          </g>
        );
      }
      case 'tank':
        return <rect x={t.x + t.w * 0.55} y={t.top - 22} width={t.w * 0.3} height={22} fill={t.shade} />;
      default:
        return null;
    }
  })();
  return (
    <g>
      {crown}
      <rect x={t.x} y={t.top} width={t.w} height={h} fill={t.fill} />
      <rect x={litRight ? t.x : t.x + t.w * 0.72} y={t.top} width={t.w * 0.28} height={h} fill={t.shade} />
      {wins}
      {rim > 0 && <rect x={litRight ? t.x + t.w - 5 : t.x} y={t.top} width={5} height={h} fill={P.golden} opacity={rim} />}
    </g>
  );
};

/* ------------------------------------------------------------------ banderitas */

/**
 * Fiesta banderitas, the triangle flags strung over every Filipino roofdeck party, in the
 * logo's five fills. Each flag is split along its spine into a lit and a shade half, and swings
 * from the string on the shared wind.
 */
export const Banderitas: React.FC<{ f: number; a: XY; b: XY; sag: number; n: number; size: number; seed: string }> = ({ f, a, b, sag, n, size, seed }) => {
  const fills: [string, string][] = [
    [P.chartreuse, '#AFA800'],
    [P.persimmon, '#D9622B'],
    [P.honey, '#E6B57C'],
    [P.golden, '#D69414'],
    [P.rimRed, '#9A220A'],
  ];
  const pt = (t: number) => ({
    x: a.x + (b.x - a.x) * t,
    y: a.y + (b.y - a.y) * t + sag * 4 * t * (1 - t) + 6 * wind(f) * Math.sin(t * Math.PI) * loopSin(f, 20, t * 3),
  });
  const d: string[] = [];
  for (let i = 0; i <= 40; i++) {
    const p = pt(i / 40);
    d.push(`${i ? 'L' : 'M'}${p.x.toFixed(1)} ${p.y.toFixed(1)}`);
  }
  const flags: React.ReactNode[] = [];
  for (let i = 0; i < n; i++) {
    const t = (i + 0.5) / n;
    const p = pt(t);
    const q = pt(t + 0.5 / n);
    const ang = (Math.atan2(q.y - p.y, q.x - p.x) * 180) / Math.PI;
    const swing = flutter(f, `${seed}-${i}`, 1.6, 26) + 10 * wind(f);
    const [lit, shade] = fills[i % fills.length];
    flags.push(
      <g key={i} transform={`translate(${p.x} ${p.y}) rotate(${ang})`}>
        <g transform={`rotate(${swing * 0.6}) skewX(${-swing * 0.5})`}>
          <polygon points={`${-size / 2},0 0,0 0,${size * 1.15}`} fill={lit} />
          <polygon points={`0,0 ${size / 2},0 0,${size * 1.15}`} fill={shade} />
        </g>
      </g>,
    );
  }
  return (
    <g>
      <path d={d.join('')} fill="none" stroke="#3A0610" strokeWidth={3.5} />
      {flags}
    </g>
  );
};

/* ------------------------------------------------------------------ laundry */

type Garment = { kind: 'shirt' | 'towel' | 'shorts' | 'sock' | 'sando'; w: number; h: number; fill: string; shade: string; stripe?: string };

const garmentPts = (g: Garment): Pt[] => {
  const { w, h } = g;
  switch (g.kind) {
    case 'shirt':
      return [[-w / 2, 0], [-w * 0.2, 0], [0, h * 0.1], [w * 0.2, 0], [w / 2, 0], [w * 0.62, h * 0.28], [w * 0.4, h * 0.36], [w * 0.38, h], [-w * 0.38, h], [-w * 0.4, h * 0.36], [-w * 0.62, h * 0.28]];
    case 'sando':
      return [[-w * 0.42, 0], [-w * 0.24, 0], [0, h * 0.22], [w * 0.24, 0], [w * 0.42, 0], [w * 0.46, h * 0.34], [w * 0.44, h], [-w * 0.44, h], [-w * 0.46, h * 0.34]];
    case 'shorts':
      return [[-w / 2, 0], [w / 2, 0], [w * 0.56, h], [w * 0.08, h], [0, h * 0.42], [-w * 0.08, h], [-w * 0.56, h]];
    case 'sock':
      return [[-w / 2, 0], [w / 2, 0], [w / 2, h], [-w * 0.9, h], [-w * 0.9, h * 0.66], [-w / 2, h * 0.66]];
    default:
      return [[-w / 2, 0], [w / 2, 0], [w / 2 + 3, h], [-w / 2 - 2, h]];
  }
};

export const Laundry: React.FC<{ f: number; a: XY; b: XY; sag: number; items: (Garment & { t: number })[]; seed: string }> = ({ f, a, b, sag, items, seed }) => {
  const pt = (t: number) => ({
    x: a.x + (b.x - a.x) * t,
    y: a.y + (b.y - a.y) * t + sag * 4 * t * (1 - t) + 5 * wind(f) * Math.sin(t * Math.PI) * loopSin(f, 12, 1 + t),
  });
  const d: string[] = [];
  for (let i = 0; i <= 30; i++) {
    const p = pt(i / 30);
    d.push(`${i ? 'L' : 'M'}${p.x.toFixed(1)} ${p.y.toFixed(1)}`);
  }
  return (
    <g>
      <path d={d.join('')} fill="none" stroke="#3A0610" strokeWidth={3} />
      {items.map((g, i) => {
        const p = pt(g.t);
        const lean = 8 + 22 * wind(f) + flutter(f, `${seed}-${i}`, 1.3, 10);
        const billow = 1 + 0.08 * flutter(f, `${seed}-b${i}`, 2.4, 1);
        const pts = garmentPts(g);
        return (
          <g key={i} transform={`translate(${p.x} ${p.y}) rotate(${-lean * 0.35}) skewX(${-lean * 0.9}) scale(${billow} 1)`}>
            <path d={polyD(pts)} fill={g.fill} />
            {/* Backlit cloth: the half away from the sun is the shade. */}
            <path d={polyD(pts.map(([x, y]) => [Math.max(x, g.w * 0.08), y] as Pt))} fill={g.shade} />
            {g.stripe && <rect x={-g.w * 0.5} y={g.h * 0.45} width={g.w} height={g.h * 0.16} fill={g.stripe} opacity={0.9} />}
            <rect x={-5} y={-8} width={10} height={14} fill={P.golden} />
          </g>
        );
      })}
    </g>
  );
};

/* ------------------------------------------------------------------ plants */

export const Plant: React.FC<{ f: number; x: number; y: number; s: number; seed: string; leaves?: number; pot?: string; potShade?: string }> = ({
  f,
  x,
  y,
  s: sc,
  seed,
  leaves = 7,
  pot = P.rimRed,
  potShade = '#8E220A',
}) => {
  const ls: React.ReactNode[] = [];
  for (let i = 0; i < leaves; i++) {
    const base = -70 + (140 * i) / (leaves - 1) + (random(`${seed}-a${i}`) - 0.5) * 18;
    const len = 70 + random(`${seed}-l${i}`) * 50;
    const sway = flutter(f, `${seed}-${i}`, 0.9, 7) + 6 * wind(f);
    // Angular leaves: a long diamond, lit half and shade half.
    ls.push(
      <g key={i} transform={`rotate(${base + sway})`}>
        <polygon points={`0,0 -14,${-len * 0.45} 0,${-len}`} fill={i % 2 ? '#9A9020' : '#7C7418'} />
        <polygon points={`0,0 14,${-len * 0.45} 0,${-len}`} fill={i % 2 ? '#6E6814' : '#5C5610'} />
      </g>,
    );
  }
  return (
    <g transform={`translate(${x} ${y}) scale(${sc})`}>
      <g transform="translate(0 -52)">{ls}</g>
      <path d={polyD([[-46, -58], [46, -58], [36, 0], [-36, 0]])} fill={pot} />
      <path d={polyD([[14, -58], [46, -58], [36, 0], [10, 0]])} fill={potShade} />
      <rect x={-52} y={-68} width={104} height={16} fill={P.persimmon} />
      <rect x={-52} y={-72} width={104} height={5} fill={P.golden} opacity={0.8} />
    </g>
  );
};

/* ------------------------------------------------------------------ pigeons */

/** A kalapati on the parapet, blocky. Pecks on its own periodic clock. */
export const Pigeon: React.FC<{ f: number; x: number; y: number; s: number; flip?: boolean; seed: string }> = ({ f, x, y, s: sc, flip, seed }) => {
  const peck = Math.max(0, loopSin(f, 7 + Math.floor(random(seed) * 5), random(seed + 'p') * 6)) ** 6;
  const bob = loopSin(f, 23, random(seed + 'b') * 6) * 1.5;
  return (
    <g transform={`translate(${x} ${y + bob}) scale(${flip ? -sc : sc} ${sc})`}>
      <path d={chamfer(-30, -34, 58, 34, 12)} fill="#8E6A5A" />
      <path d={chamfer(-30, -34, 58, 12, 6)} fill="#A88070" />
      <polygon points="-30,-22 -48,-14 -30,-6" fill="#6E4E44" />
      <rect x={-14} y={-26} width={28} height={10} fill="#6E4E44" />
      <g transform={`translate(22 -38) rotate(${peck * 48})`}>
        <rect x={-10} y={-12} width={20} height={20} fill="#9A7464" />
        <rect x={2} y={-6} width={5} height={5} fill={P.ink} />
        <rect x={10} y={-2} width={9} height={5} fill={P.golden} />
        <rect x={-10} y={4} width={20} height={4} fill="#B98A5C" />
      </g>
      <rect x={-8} y={0} width={4} height={9} fill={P.rimRed} />
      <rect x={6} y={0} width={4} height={9} fill={P.rimRed} />
    </g>
  );
};
