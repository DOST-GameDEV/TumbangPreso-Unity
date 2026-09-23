import React from 'react';
import { random } from 'remotion';
import { P } from '../lib/palette';
import { LOOP, clamp01, loopNoise, loopSin } from '../lib/time';
import { chamfer, polyD } from '../lib/shapes';
import { Burst } from './burst';
import { Banderitas, Cloud, CloudDrift, Laundry, Pigeon, Plant, Tower, TowerSpec } from './set';

/*
 * The Sa Bubong set at dusk, seen from the court looking out past Zack to Pasig.
 *
 * ⚠️ WHY DUSK. The first pass was golden afternoon and every surface came out orange next to
 * orange: no value structure, so nothing led the eye and the lightning had nothing darker than
 * itself to glow against. Dusk gives the values in order: the deep-red sky at the top (dark,
 * and the hub's top bar sits on it), the golden glow on the horizon behind him (lightest), and
 * the towers as silhouettes between. The sun is her burst, set low and straight behind his head.
 */

// ⚠️ Upper right, where the owner's own HOME sketch (zip-sketches/20.png, labelled "video(?)")
// puts it, in the one corner the hub leaves free (x 1470 to 1900, y 160 to 580). Behind his head
// it was hidden by his hair and by the towers on both sides, and read as nothing.
export const SUN = { x: 1570, y: 360, r: 150 };

export const SKY_BANDS: [number, string][] = [
  [0, '#560815'],
  [140, '#6A0B1A'],
  [250, '#84111D'],
  [340, '#A41F1F'],
  [418, '#C63520'],
  [486, '#E05726'],
  [546, '#EF7B34'],
  [600, '#F8A043'],
  [648, '#FCC05A'],
];

export const Sky: React.FC<{ f: number; w?: number; h?: number }> = ({ f, w = 1920, h = 760 }) => (
  <g>
    {SKY_BANDS.map(([y, c], i) => {
      const next = SKY_BANDS[i + 1]?.[0] ?? h;
      // Stepped band edges, the blocky way to draw a gradient: a few flat risers per band.
      const d: string[] = [`M -60 ${next + 40}`, `L -60 ${y}`];
      for (let x = -60; x <= w + 60; x += 160) {
        const yy = y + (i === 0 ? -20 : Math.round(6 * Math.sin(x / 170 + i * 1.7) + 3 * loopSin(f, 1, x / 300 + i)));
        d.push(`L ${x} ${yy}`, `L ${x + 160} ${yy}`);
      }
      d.push(`L ${w + 60} ${next + 40} Z`);
      return <path key={i} d={d.join(' ')} fill={c} />;
    })}
  </g>
);

/** Early stars in the dark top of the sky, each twinkling on its own periodic clock. */
export const Stars: React.FC<{ f: number }> = ({ f }) => {
  const out: React.ReactNode[] = [];
  for (let i = 0; i < 26; i++) {
    const x = 60 + random(`st${i}x`) * 1800;
    const y = 20 + random(`st${i}y`) * 260;
    const tw = clamp01(0.45 + 0.75 * loopNoise(`st${i}`, f, 1.4));
    const r = 3 + random(`st${i}r`) * 4;
    out.push(
      <g key={i} transform={`translate(${x} ${y}) scale(${0.6 + tw * 0.5})`} opacity={0.25 + 0.75 * tw}>
        <rect x={-r * 0.4} y={-r * 1.8} width={r * 0.8} height={r * 3.6} fill="#FFE7A8" />
        <rect x={-r * 1.8} y={-r * 0.4} width={r * 3.6} height={r * 0.8} fill="#FFE7A8" />
      </g>,
    );
  }
  return <g>{out}</g>;
};

/** Maya birds: one small flock crosses once per loop, off screen at the seam. */
export const Birds: React.FC<{ f: number }> = ({ f }) => {
  const t = (f / LOOP + 0.62) % 1;
  if (t > 0.45) return null;
  const k = t / 0.45;
  const out: React.ReactNode[] = [];
  for (let i = 0; i < 5; i++) {
    const x = -120 + k * 2200 - i * 46 + Math.sin(i * 2.3) * 20;
    const y = 250 - k * 90 + Math.cos(i * 1.7) * 26 + Math.sin(k * 12 + i) * 6;
    const flap = Math.sin(f * 0.9 + i * 1.3);
    out.push(
      <g key={i}>
        <polygon points={`${x - 16},${y - flap * 9} ${x},${y} ${x - 4},${y + 4}`} fill="#3B0710" />
        <polygon points={`${x + 16},${y - flap * 9} ${x},${y} ${x + 4},${y + 4}`} fill="#3B0710" />
      </g>,
    );
  }
  return <g>{out}</g>;
};

const far = { fill: '#E27E42', shade: '#D66F3A', win: '#EC9150', lit: '#FFE3A0' };
const mid = { fill: '#B63622', shade: '#962A1B', win: '#C8472A', lit: '#FFD45E' };
const near = { fill: '#7E1519', shade: '#621014', win: '#94261E', lit: '#FFC94A' };

// ⚠️ NOTHING TALL BEHIND HIS HEAD. The sun is there instead. Towers cluster left and right and
// drop below the horizon glow in the middle, so his silhouette always has clean sky round it.
export const TOWERS_FAR: TowerSpec[] = [
  { x: 470, w: 70, top: 468, base: 700, crown: 'flat', seed: 'f1', ...far },
  { x: 560, w: 56, top: 430, base: 700, crown: 'mast', seed: 'f2', ...far },
  { x: 640, w: 90, top: 520, base: 700, crown: 'step', seed: 'f3', ...far },
  { x: 1250, w: 64, top: 505, base: 700, crown: 'slant', seed: 'f4', ...far },
  { x: 1340, w: 80, top: 455, base: 700, crown: 'flat', seed: 'f5', ...far },
  { x: 1470, w: 60, top: 520, base: 700, crown: 'tank', seed: 'f6', ...far },
  { x: 1700, w: 90, top: 470, base: 700, crown: 'step', seed: 'f7', ...far },
  { x: 60, w: 100, top: 480, base: 700, crown: 'flat', seed: 'f8', ...far },
];

export const TOWERS_MID: TowerSpec[] = [
  { x: 390, w: 110, top: 350, base: 700, crown: 'step', seed: 'm1', ...mid },
  { x: 525, w: 82, top: 290, base: 700, crown: 'mast', seed: 'm2', ...mid },
  { x: 630, w: 96, top: 470, base: 700, crown: 'slant', seed: 'm3', ...mid },
  { x: 1245, w: 88, top: 450, base: 700, crown: 'tank', seed: 'm4', ...mid },
  { x: 1330, w: 120, top: 430, base: 700, crown: 'step', seed: 'm5', ...mid },
  { x: 1500, w: 84, top: 480, base: 700, crown: 'mast', seed: 'm6', ...mid },
  { x: 1780, w: 130, top: 380, base: 700, crown: 'flat', seed: 'm7', ...mid },
  { x: 160, w: 120, top: 395, base: 700, crown: 'slant', seed: 'm8', ...mid },
];

export const TOWERS_NEAR: TowerSpec[] = [
  { x: 300, w: 150, top: 560, base: 705, crown: 'tank', seed: 'n1', ...near },
  { x: 610, w: 150, top: 610, base: 705, crown: 'flat', seed: 'n2', ...near },
  { x: 1190, w: 150, top: 620, base: 705, crown: 'tank', seed: 'n3', ...near },
  { x: 1540, w: 170, top: 575, base: 705, crown: 'step', seed: 'n4', ...near },
  { x: 1370, w: 110, top: 600, base: 705, crown: 'flat', seed: 'n5', ...near },
  { x: -20, w: 190, top: 590, base: 705, crown: 'step', seed: 'n6', ...near },
];

/** The Sierra Madre, low and far behind the city, in the horizon's haze. Stepped, not curved. */
export const Hills: React.FC = () => {
  const d: string[] = ['M -40 700'];
  let x = -40;
  let i = 0;
  while (x < 1980) {
    const y = 620 + Math.round(18 * Math.sin(x / 210) + 10 * Math.sin(x / 67 + 1));
    d.push(`L ${x} ${y}`, `L ${x + 60} ${y}`);
    x += 60;
    i++;
  }
  d.push('L 1980 700 Z');
  return <path d={d.join(' ')} fill="#F09A50" />;
};

const HIGH = { fill: '#B8321F', under: '#EE8A42', top: '#9A2419' };
const LOW = { fill: '#EE8540', under: '#FFC45C', top: '#D96E34' };

export const Clouds: React.FC<{ f: number }> = ({ f }) => (
  <g>
    <CloudDrift f={f} phase={0}>
      {(dx, a) => <Cloud x={250 + dx} y={300} opacity={a} {...HIGH} slabs={[[0, 0, 240, 40], [40, -36, 120, 40], [120, -20, 90, 30]]} />}
    </CloudDrift>
    <CloudDrift f={f} phase={0.25}>
      {(dx, a) => <Cloud x={1170 + dx} y={240} opacity={a} {...HIGH} slabs={[[0, 0, 210, 34], [30, -30, 110, 36], [110, -18, 70, 26]]} />}
    </CloudDrift>
    <CloudDrift f={f} phase={0.5}>
      {(dx, a) => <Cloud x={640 + dx} y={200} opacity={a} {...HIGH} slabs={[[0, 0, 170, 28], [26, -26, 90, 30]]} />}
    </CloudDrift>
    <CloudDrift f={f} phase={0.75}>
      {(dx, a) => <Cloud x={1540 + dx} y={500} opacity={a} {...LOW} slabs={[[0, 0, 280, 40], [50, -36, 150, 42], [150, -22, 100, 30]]} />}
    </CloudDrift>
  </g>
);

/** The horizon bank, in front of the sun's lower part, behind the far city. Never moves. */
export const Bank: React.FC = () => (
  <Cloud x={0} y={668} {...LOW} under="#FFD06A"
    slabs={[[-40, 0, 2000, 36], [40, -30, 300, 34], [420, -40, 240, 44], [720, -24, 180, 30], [1180, -34, 260, 40], [1520, -44, 300, 48], [1800, -26, 180, 30]]} />
);

export const SunDisc: React.FC<{ f: number }> = ({ f }) => {
  // A seventh of a turn per loop: the burst has seven arms, so frame 900 lands on frame 0.
  const rot = (f / LOOP) * (360 / 7);
  const pulse = 1 + 0.012 * loopSin(f, 5);
  return (
    <g transform={`translate(${SUN.x} ${SUN.y}) scale(${pulse})`}>
      <circle r={SUN.r * 2.1} fill="url(#sunGlow)" />
      <Burst r={SUN.r} arms={7} seed="sun" rot={rot} inner={0.74} />
    </g>
  );
};

export const Parapet: React.FC<{ top?: number; bottom?: number }> = ({ top = 700, bottom = 800 }) => (
  <g>
    <rect x={-60} y={top + 16} width={2040} height={bottom - top - 16} fill="#A8492C" />
    {[250, 610, 1310, 1670].map((x) => (
      <rect key={x} x={x - 2} y={top + 18} width={5} height={bottom - top - 20} fill="#8E3C25" />
    ))}
    <rect x={-60} y={bottom - 22} width={2040} height={22} fill="#8E3C25" />
    <rect x={-60} y={top} width={2040} height={22} fill="#D0724A" />
    <rect x={-60} y={top} width={2040} height={5} fill={P.golden} opacity={0.9} />
    {/* Chalk left by the kids who play here: a tally and a heart, not words. */}
    <g fill="#FFD9A8" opacity={0.7}>
      {[0, 12, 24, 36].map((dx) => (
        <rect key={dx} x={1350 + dx} y={752} width={5} height={26} />
      ))}
      <rect x={1342} y={763} width={52} height={5} transform="rotate(-12 1368 765)" />
      <path d="M 692 748 h 12 v 6 h 6 v -6 h 12 v 12 h -6 v 6 h -6 v 6 h -6 v -6 h -6 v -6 h -6 z" />
    </g>
  </g>
);

export const Floor: React.FC<{ top?: number }> = ({ top = 800 }) => {
  const vx = 960;
  const vy = 560;
  const lines: React.ReactNode[] = [];
  for (let i = -9; i <= 9; i++) {
    const xb = vx + i * 260;
    const t = (top - vy) / (1080 - vy);
    const xt = vx + (xb - vx) * t;
    lines.push(<path key={'v' + i} d={polyD([[xt - 1.5, top], [xt + 1.5, top], [xb + 2.5, 1080], [xb - 2.5, 1080]])} fill="#B86A45" />);
  }
  [848, 912, 1000].forEach((y, i) => lines.push(<rect key={'h' + i} x={-60} y={y} width={2040} height={4} fill="#B86A45" />));
  return (
    <g>
      <rect x={-60} y={top} width={2040} height={1080 - top + 60} fill="#CC8354" />
      <rect x={-60} y={top} width={2040} height={46} fill="#E09A5E" />
      {lines}
      {/* The throw line in chalk, broken where the chalk ran thin. */}
      {[[520, 70], [608, 40], [672, 90], [790, 50], [870, 110], [1010, 60], [1100, 90], [1220, 40], [1290, 110]].map(([x, w], i) => (
        <rect key={i} x={x} y={994 + (i % 2)} width={w} height={9} fill="#FFE3B8" opacity={0.85} />
      ))}
    </g>
  );
};

/** The residents' water tank, army green, rim-lit on the side facing the sun. */
export const WaterTank: React.FC<{ x: number; y: number; s?: number }> = ({ x, y, s = 1 }) => (
  <g transform={`translate(${x} ${y}) scale(${s})`}>
    {[-100, -40, 40, 100].map((lx, i) => (
      <rect key={i} x={lx - 6} y={-40} width={12} height={160} fill="#3E0A10" />
    ))}
    <rect x={-104} y={56} width={208} height={8} fill="#3E0A10" />
    <path d={chamfer(-130, -334, 260, 316, 22)} fill="#77701A" />
    <rect x={-130} y={-312} width={56} height={272} fill="#5E5914" />
    {[-250, -170, -90].map((yy) => (
      <rect key={yy} x={-130} y={yy} width={260} height={8} fill="#5E5914" />
    ))}
    <path d={chamfer(-122, -352, 244, 28, 10)} fill="#9A9124" />
    <rect x={-22} y={-372} width={44} height={22} fill="#77701A" />
    <rect x={120} y={-312} width={6} height={270} fill={P.golden} opacity={0.9} />
    <rect x={62} y={-280} width={12} height={40} fill="#A89E2C" transform="rotate(-30 68 -260)" />
    <rect x={86} y={-284} width={12} height={40} fill="#A89E2C" transform="rotate(-30 92 -264)" />
  </g>
);

export const Pole: React.FC<{ x: number; top: number; bottom: number }> = ({ x, top, bottom }) => (
  <g>
    <rect x={x - 7} y={top} width={14} height={bottom - top} fill="#4A0A12" />
    <rect x={x + 3} y={top} width={4} height={bottom - top} fill={P.golden} opacity={0.6} />
  </g>
);

/**
 * Motes in the low sun: square specks of dust and pollen rising slowly through the light.
 * ⚠️ Seamless by construction: each rises exactly one or two screen heights per loop and wraps,
 * so frame 900 puts every mote where frame 0 had it.
 */
export const Motes: React.FC<{ f: number; n?: number }> = ({ f, n = 46 }) => {
  const out: React.ReactNode[] = [];
  const H = 1200;
  for (let i = 0; i < n; i++) {
    const laps = random(`mo${i}l`) > 0.6 ? 2 : 1;
    const y = ((random(`mo${i}y`) * H - (f / LOOP) * H * laps) % H + H) % H - 60;
    const x = 80 + random(`mo${i}x`) * 1760 + 30 * loopSin(f, 1 + (i % 3), i);
    const edge = clamp01(y / 180) * clamp01((H - 120 - y) / 180);
    const tw = 0.5 + 0.5 * loopSin(f, 8 + (i % 7), i * 1.3);
    const sz = 3 + random(`mo${i}s`) * 5;
    out.push(<rect key={i} x={x} y={y} width={sz} height={sz} fill={i % 4 ? '#FFD98A' : '#FFF3C8'} opacity={edge * (0.25 + 0.5 * tw)} transform={`rotate(45 ${x + sz / 2} ${y + sz / 2})`} />);
  }
  return <g>{out}</g>;
};

export const WideSet: React.FC<{ f: number; layer: 'sky' | 'city' | 'mid' }> = ({ f, layer }) => {
  if (layer === 'sky')
    return (
      <g>
        <Sky f={f} />
        <Stars f={f} />
        <Clouds f={f} />
        <Birds f={f} />
        {/* The horizon glows hardest straight behind him, so his silhouette always reads. */}
        <ellipse cx={968} cy={660} rx={720} ry={380} fill="url(#sunGlow)" opacity={0.8} />
        <SunDisc f={f} />
        <Bank />
      </g>
    );
  if (layer === 'city')
    return (
      <g>
        <Hills />
        <g filter="url(#haze)">{TOWERS_FAR.map((t) => <Tower key={t.seed} t={t} f={f} sunX={SUN.x} rim={0.5} />)}</g>
        {TOWERS_MID.map((t) => (
          <Tower key={t.seed} t={t} f={f} sunX={SUN.x} rim={0.9} />
        ))}
        {TOWERS_NEAR.map((t) => (
          <Tower key={t.seed} t={t} f={f} sunX={SUN.x} rim={0.7} />
        ))}
      </g>
    );
  return (
    <g>
      <WaterTank x={250} y={590} s={1.05} />
      <Parapet />
      <Pole x={1210} top={470} bottom={712} />
      <Pole x={1800} top={228} bottom={712} />
      <Laundry
        f={f}
        seed="ln"
        a={{ x: 1210, y: 486 }}
        b={{ x: 1800, y: 470 }}
        sag={46}
        items={[
          { t: 0.13, kind: 'towel', w: 64, h: 110, fill: P.golden, shade: '#D69414', stripe: P.rimRed },
          { t: 0.34, kind: 'shirt', w: 92, h: 104, fill: P.honey, shade: '#E6B57C', stripe: P.persimmon },
          { t: 0.56, kind: 'shorts', w: 70, h: 70, fill: P.chartreuse, shade: '#AFA800' },
          { t: 0.72, kind: 'sock', w: 22, h: 44, fill: P.persimmon, shade: '#D9622B' },
          { t: 0.86, kind: 'sando', w: 66, h: 92, fill: '#FFE7C0', shade: '#E8C696' },
        ]}
      />
      <Plant f={f} x={560} y={704} s={0.9} seed="p1" />
      <Plant f={f} x={1470} y={704} s={0.75} seed="p2" leaves={6} pot={P.persimmon} potShade="#C9602A" />
      <Pigeon f={f} x={1290} y={702} s={0.9} seed="pg1" />
      <Pigeon f={f} x={1360} y={702} s={0.8} flip seed="pg2" />
      <Banderitas f={f} seed="bd" a={{ x: 250, y: 240 }} b={{ x: 1800, y: 240 }} sag={58} n={21} size={44} />
      <Floor />
    </g>
  );
};
