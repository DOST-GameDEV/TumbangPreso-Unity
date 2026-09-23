import React from 'react';
import { random } from 'remotion';
import { P } from '../lib/palette';
import { clamp01, loopNoise } from '../lib/time';
import { chamfer, polyD, Pt } from '../lib/shapes';
import { Plant } from './set';

/*
 * The reverse angle: the court Zack faces, looking east down Sa Bubong to the lata.
 *
 * ⚠️ THE LIGHT FLIPS AND THAT IS THE CONTINUITY. In the wide the sun is behind him; here it is
 * behind the CAMERA, so everything facing us is lit gold, his shadow runs AWAY from us toward the
 * can, and the eastern sky is the darker half of the dusk with the moon up. A reverse shot that
 * kept the wide's backlight would read as a different evening.
 */

export const HORIZON = 560;
export const CAN_AT = { x: 1180, y: 642 };
export const SEAN_AT = { x: 1372, y: 660 };

const BANDS: [number, string][] = [
  [0, '#3A0612'],
  [130, '#4C0917'],
  [240, '#661020'],
  [330, '#851A25'],
  [410, '#A82E2A'],
  [480, '#C84C33'],
  [530, '#DA6A3C'],
];

export const EastSky: React.FC<{ f: number }> = ({ f }) => (
  <g>
    {BANDS.map(([y, c], i) => {
      const next = BANDS[i + 1]?.[0] ?? HORIZON + 40;
      const d: string[] = [`M -60 ${next + 40}`, `L -60 ${y}`];
      for (let x = -60; x <= 1980; x += 160) {
        const yy = y + (i === 0 ? -20 : Math.round(6 * Math.sin(x / 150 + i * 2.1)));
        d.push(`L ${x} ${yy}`, `L ${x + 160} ${yy}`);
      }
      d.push(`L 1980 ${next + 40} Z`);
      return <path key={i} d={d.join(' ')} fill={c} />;
    })}
    {Array.from({ length: 34 }).map((_, i) => {
      const x = 40 + random(`es${i}x`) * 1840;
      const y = 20 + random(`es${i}y`) * 320;
      const tw = clamp01(0.45 + 0.75 * loopNoise(`es${i}`, f, 1.4));
      const r = 3 + random(`es${i}r`) * 4;
      return (
        <g key={i} transform={`translate(${x} ${y})`} opacity={0.3 + 0.7 * tw}>
          <rect x={-r * 0.4} y={-r * 1.8} width={r * 0.8} height={r * 3.6} fill="#FFE7A8" />
          <rect x={-r * 1.8} y={-r * 0.4} width={r * 3.6} height={r * 0.8} fill="#FFE7A8" />
        </g>
      );
    })}
    {/* The moon, rising in the east as the sun sets behind us. Blocky, one crater. */}
    <g transform="translate(1560 210)">
      <path d={chamfer(-52, -52, 104, 104, 30)} fill="#FFE9BE" />
      <path d={chamfer(8, -52, 44, 104, 18)} fill="#EFCF98" />
      <rect x={-22} y={-14} width={18} height={16} fill="#EFCF98" />
      <rect x={10} y={16} width={12} height={10} fill="#DDBA82" />
    </g>
  </g>
);

type T = { x: number; w: number; top: number; crown?: 'step' | 'tank' | 'mast' | 'flat'; seed: string };
// Lit fronts facing the sun behind us; the shade is the far side of each tower.
const EAST: T[] = [
  { x: 60, w: 130, top: 330, crown: 'step', seed: 'e1' },
  { x: 210, w: 90, top: 400, crown: 'tank', seed: 'e2' },
  { x: 330, w: 110, top: 280, crown: 'mast', seed: 'e3' },
  { x: 470, w: 80, top: 420, crown: 'flat', seed: 'e4' },
  { x: 700, w: 120, top: 360, crown: 'step', seed: 'e5' },
  { x: 850, w: 90, top: 440, crown: 'flat', seed: 'e6' },
  { x: 1010, w: 130, top: 310, crown: 'mast', seed: 'e7' },
  { x: 1180, w: 100, top: 400, crown: 'tank', seed: 'e8' },
  { x: 1320, w: 140, top: 340, crown: 'step', seed: 'e9' },
  { x: 1690, w: 120, top: 300, crown: 'flat', seed: 'e10' },
  { x: 1830, w: 100, top: 410, crown: 'tank', seed: 'e11' },
];

export const EastCity: React.FC<{ f: number }> = ({ f }) => (
  <g>
    {/* Low far city first, in the haze. */}
    <path d={polyD([[-60, HORIZON], [-60, 470], [140, 470], [140, 490], [380, 490], [380, 460], [600, 460], [600, 480], [900, 480], [900, 455], [1250, 455], [1250, 485], [1560, 485], [1560, 465], [1980, 465], [1980, HORIZON]])} fill="#C8603A" />
    {EAST.map((t) => {
      const h = HORIZON - t.top;
      const cols = Math.max(2, Math.round(t.w / 26));
      const gx = t.w / cols;
      const rows = Math.floor((h - 30) / 22);
      const wins: React.ReactNode[] = [];
      for (let r = 0; r < rows; r++)
        for (let c = 0; c < cols; c++) {
          const k = `${t.seed}-${r}-${c}`;
          const on = random(k) > 0.7 ? clamp01(0.5 + 0.9 * loopNoise(k, f, 0.9)) : 0;
          wins.push(<rect key={k} x={t.x + c * gx + gx * 0.26} y={t.top + 22 + r * 22} width={gx * 0.48} height={9} fill={on > 0.3 ? '#FFE9A8' : '#D9774A'} />);
        }
      const crown =
        t.crown === 'step' ? <rect x={t.x + t.w * 0.2} y={t.top - 24} width={t.w * 0.6} height={26} fill="#F2A160" /> :
        t.crown === 'tank' ? <rect x={t.x + t.w * 0.5} y={t.top - 20} width={t.w * 0.3} height={20} fill="#C4552F" /> :
        t.crown === 'mast' ? <rect x={t.x + t.w / 2 - 3} y={t.top - 64} width={6} height={64} fill="#C4552F" /> : null;
      return (
        <g key={t.seed}>
          {crown}
          <rect x={t.x} y={t.top} width={t.w} height={h} fill="#F09A58" />
          <rect x={t.x + t.w * 0.7} y={t.top} width={t.w * 0.3} height={h} fill="#C4552F" />
          <rect x={t.x} y={t.top} width={t.w} height={5} fill="#FFD27A" />
          {wins}
        </g>
      );
    })}
  </g>
);

export const Court: React.FC<{ f: number }> = ({ f }) => {
  const vp: Pt = [1010, 470];
  const seams: React.ReactNode[] = [];
  for (let i = -8; i <= 10; i++) {
    const xb = vp[0] + i * 300;
    const t = (HORIZON + 60 - vp[1]) / (1080 - vp[1]);
    seams.push(<path key={i} d={polyD([[vp[0] + (xb - vp[0]) * t - 1, HORIZON + 60], [vp[0] + (xb - vp[0]) * t + 1, HORIZON + 60], [xb + 2.5, 1080], [xb - 2.5, 1080]])} fill="#D08A58" />);
  }
  [680, 760, 880].forEach((y, i) => seams.push(<rect key={'h' + i} x={-60} y={y} width={2040} height={3 + i} fill="#D08A58" />));
  return (
    <g>
      {/* The far parapet, lit. */}
      <rect x={-60} y={HORIZON} width={2040} height={60} fill="#E4905A" />
      <rect x={-60} y={HORIZON} width={2040} height={16} fill="#F6B97C" />
      <rect x={-60} y={HORIZON + 50} width={2040} height={10} fill="#C4703F" />
      {/* The floor, gold where the last sun lies on it. */}
      <rect x={-60} y={HORIZON + 60} width={2040} height={600} fill="#E8A56B" />
      {seams}
      {/* The taya box in chalk, in perspective round the can, and the can's own circle. */}
      <path d={polyD([[980, 668], [1392, 668], [1470, 770], [900, 770]])} fill="none" stroke="#FFEAC4" strokeWidth={8} strokeLinejoin="miter" opacity={0.85} />
      <path d={chamfer(CAN_AT.x - 62, CAN_AT.y - 14, 124, 28, 12)} fill="none" stroke="#FFEAC4" strokeWidth={7} opacity={0.9} />
      {/* The throw line, under his feet, broken where the chalk ran thin. */}
      {[[380, 170], [580, 90], [700, 220], [960, 120], [1110, 260], [1400, 160]].map(([x, w], i) => (
        <rect key={i} x={x} y={948 + i * 3} width={w} height={11} fill="#FFEAC4" opacity={0.8} />
      ))}
      {/* A residents' water tank far left, plastic chairs and a plant far right. */}
      <g transform="translate(270 612)">
        <rect x={-52} y={-40} width={10} height={40} fill="#6E3A22" />
        <rect x={42} y={-40} width={10} height={40} fill="#6E3A22" />
        <path d={chamfer(-62, -150, 124, 112, 12)} fill="#A09528" />
        <rect x={30} y={-150} width={32} height={112} fill="#7C7420" />
        <rect x={-62} y={-150} width={124} height={6} fill="#D8CC4A" />
      </g>
      {[1610, 1700].map((x, i) => (
        <g key={i} transform={`translate(${x} ${660 + i * 6})`}>
          <rect x={-30} y={-70} width={60} height={46} fill={P.rimRed} />
          <rect x={-34} y={-30} width={68} height={12} fill="#E0502A" />
          <rect x={-30} y={-18} width={8} height={18} fill="#9A220A" />
          <rect x={22} y={-18} width={8} height={18} fill="#9A220A" />
        </g>
      ))}
      <Plant f={f} x={1830} y={680} s={0.7} seed="rp1" pot={P.persimmon} potShade="#C9602A" />
    </g>
  );
};

/** Pigeons on the far parapet. `scatter` 0..1 is the TUMP! sending them up. */
export const ParapetPigeons: React.FC<{ f: number; scatter: number }> = ({ f, scatter }) => {
  const birds: [number, number][] = [
    [820, 0],
    [880, 1],
    [930, 2],
    [1010, 3],
  ];
  return (
    <g>
      {birds.map(([x, i]) => {
        const t = clamp01(scatter - i * 0.05);
        const flap = Math.sin(f * 1.3 + i * 2) * (t > 0 ? 1 : 0);
        const bx = x + t * (i % 2 ? 260 : -220) * (1 + i * 0.2);
        const by = HORIZON - 4 - t * (360 + i * 60) + (t > 0 ? 0 : Math.sin(f / 9 + i) * 1.2);
        const sc = 0.55 + t * 0.2;
        return (
          <g key={i} transform={`translate(${bx} ${by}) scale(${(i % 2 ? -1 : 1) * sc} ${sc})`}>
            <path d={chamfer(-30, -34, 58, 30, 10)} fill="#B48A74" />
            <rect x={14} y={-50} width={20} height={20} fill="#A07A66" />
            <rect x={28} y={-44} width={8} height={4} fill={P.golden} />
            {t > 0 ? (
              <>
                <path d={polyD([[-14, -30], [-40, -30 - 40 * flap], [6, -30]])} fill="#C9A08A" />
                <path d={polyD([[-4, -30], [20, -30 - 44 * -flap], [18, -26]])} fill="#8E6A5A" />
              </>
            ) : (
              <rect x={-24} y={-28} width={30} height={10} fill="#8E6A5A" />
            )}
          </g>
        );
      })}
    </g>
  );
};
