import React from 'react';
import { random } from 'remotion';
import { P } from '../lib/palette';
import { clamp01, loopNoise } from '../lib/time';
import { chamfer, polyD } from '../lib/shapes';
import { Sky } from './roofdeck';
import { Burst } from './burst';
import { Can } from './props';
import { Plant } from './set';

/*
 * The court side-on, for the chase: the camera runs alongside him down Sa Bubong. Every layer
 * scrolls at its own depth, so the tracking shot has parallax without a 3D scene: the sky barely,
 * the city slowly, the court at his speed, and a few things between us and him faster still.
 * World x runs along the court; GROUND is where feet land on screen.
 */
export const GROUND = 880;

const TOWER_ROW = Array.from({ length: 40 }).map((_, i) => ({
  x: -2600 + i * 170 + random(`sx${i}`) * 60,
  w: 90 + random(`sw${i}`) * 80,
  top: 300 + random(`st${i}`) * 220,
  seed: `sv${i}`,
}));

export const SideSky: React.FC<{ f: number; camX: number }> = ({ f, camX }) => (
  <g>
    <g transform={`translate(${-camX * 0.02} 0)`}>
      <Sky f={f} w={2400} />
    </g>
    <g transform={`translate(${380 - camX * 0.03} 330)`}>
      <circle r={420} fill="url(#sunGlow)" />
      <Burst r={140} arms={7} seed="sun" rot={f * 0.08} inner={0.74} />
    </g>
  </g>
);

export const SideCity: React.FC<{ f: number; camX: number }> = ({ f, camX }) => (
  <g transform={`translate(${-camX * 0.22} 0)`}>
    <path d={polyD([[-3000, 700], [-3000, 560], [5000, 560], [5000, 700]])} fill="#C8603A" />
    {TOWER_ROW.map((t) => {
      const h = 700 - t.top;
      const cols = Math.max(2, Math.round(t.w / 26));
      const gx = t.w / cols;
      const rows = Math.floor((h - 30) / 22);
      const wins: React.ReactNode[] = [];
      for (let r = 0; r < rows; r++)
        for (let c = 0; c < cols; c++) {
          const k = `${t.seed}-${r}-${c}`;
          const on = random(k) > 0.66 ? clamp01(0.5 + 0.9 * loopNoise(k, f, 0.9)) : 0;
          wins.push(<rect key={k} x={t.x + c * gx + gx * 0.26} y={t.top + 22 + r * 22} width={gx * 0.48} height={9} fill={on > 0.3 ? '#FFD45E' : '#C8472A'} />);
        }
      return (
        <g key={t.seed}>
          <rect x={t.x} y={t.top} width={t.w} height={h} fill="#A83220" />
          <rect x={t.x + t.w * 0.7} y={t.top} width={t.w * 0.3} height={h} fill="#8A261A" />
          <rect x={t.x} y={t.top} width={5} height={h} fill={P.golden} opacity={0.6} />
          {wins}
        </g>
      );
    })}
  </g>
);

/** The court plane: the far parapet with its props, and the floor he runs on. Depth 1. */
export const SideCourt: React.FC<{ f: number; camX: number; canUp: boolean }> = ({ f, camX, canUp }) => {
  const seams: React.ReactNode[] = [];
  const first = Math.floor((camX - 1400) / 220);
  for (let i = first; i < first + 16; i++) {
    const x = i * 220;
    seams.push(<path key={i} d={polyD([[x - 2, 720], [x + 2, 720], [x + 60 + 4, 1180], [x + 60 - 4, 1180]])} fill="#B86A45" />);
  }
  return (
    <g transform={`translate(${-camX + 960} 0)`}>
      {/* Parapet. */}
      <rect x={camX - 2000} y={630} width={4000} height={90} fill="#A8492C" />
      <rect x={camX - 2000} y={630} width={4000} height={20} fill="#D0724A" />
      <rect x={camX - 2000} y={630} width={4000} height={5} fill={P.golden} opacity={0.9} />
      {/* Props along it, in world x. */}
      <g transform="translate(300 640)">
        {[-100, -40, 40, 100].map((lx, i) => (
          <rect key={i} x={lx - 6} y={-60} width={12} height={60} fill="#3E0A10" />
        ))}
        <path d={chamfer(-130, -330, 260, 280, 22)} fill="#77701A" />
        <rect x={-130} y={-310} width={56} height={250} fill="#5E5914" />
        <rect x={120} y={-310} width={6} height={250} fill={P.golden} opacity={0.8} />
      </g>
      {[880, 1380].map((x) => (
        <g key={x}>
          <rect x={x - 7} y={400} width={14} height={240} fill="#4A0A12" />
        </g>
      ))}
      <path d="M 880 420 Q 1130 470 1380 420" fill="none" stroke="#3A0610" strokeWidth={3} />
      {[[940, P.golden], [1030, P.honey], [1120, P.chartreuse], [1220, P.persimmon], [1300, '#FFE7C0']].map(([x, c], i) => (
        <path key={i} d={chamfer(Number(x) - 30, 430 + (i % 2) * 6, 60, 80 + (i % 3) * 14, 6)} fill={String(c)} transform={`skewX(${-8 - 6 * Math.sin(f / 5 + i)})`} />
      ))}
      <Plant f={f} x={620} y={634} s={0.8} seed="sp1" />
      <Plant f={f} x={1760} y={634} s={0.7} seed="sp2" pot={P.persimmon} potShade="#C9602A" />
      {[2250, 2340].map((x, i) => (
        <g key={i} transform={`translate(${x} 700)`}>
          <rect x={-30} y={-70} width={60} height={46} fill={P.rimRed} />
          <rect x={-34} y={-30} width={68} height={12} fill="#E0502A" />
          <rect x={-30} y={-18} width={8} height={18} fill="#9A220A" />
          <rect x={22} y={-18} width={8} height={18} fill="#9A220A" />
        </g>
      ))}
      {/* Floor. */}
      <rect x={camX - 2000} y={720} width={4000} height={460} fill="#D08655" />
      <rect x={camX - 2000} y={720} width={4000} height={40} fill="#E09A5E" />
      {seams}
      <rect x={camX - 2000} y={1010} width={4000} height={5} fill="#B86A45" />
      {/* The can's circle and the can, stood back up on it by the taya. */}
      <path d={chamfer(2060, GROUND - 22, 140, 30, 12)} fill="none" stroke="#FFEAC4" strokeWidth={7} opacity={0.85} />
      {canUp && (
        <g transform={`translate(2130 ${GROUND - 6})`}>
          <Can s={1.2} lit="left" />
        </g>
      )}
    </g>
  );
};

/** Things between the camera and him: dark, soft, fast. They sell the speed. */
export const SideForeground: React.FC<{ camX: number }> = ({ camX }) => (
  <g transform={`translate(${-camX * 1.8 + 960 * 1.8} 0)`} filter="url(#defocus)" opacity={0.92}>
    {[400, 1300, 2300, 3200].map((x, i) => (
      <g key={i}>
        <rect x={x} y={i % 2 ? 0 : 560} width={i % 2 ? 60 : 110} height={1080} fill="#3A0A10" />
      </g>
    ))}
  </g>
);
