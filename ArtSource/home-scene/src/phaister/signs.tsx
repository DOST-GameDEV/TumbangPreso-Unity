import React from 'react';
import { LOOP, loopNoise } from './time';
import { depth, V3, View } from './view';
import { box, emit, FACING, Item, lit, onPlane, tone } from './set';
import { PARES, PAVE_TOP, PISONET } from './map';
import { Paint } from './lettering';

/*
 * ⚠️⚠️ THE STREET'S SIGNS ARE INVENTED, AND RESEARCHED. 🧑 2026-09-24: *"the signs we have in the game
 * are not final so js imagine it on ur own, research PH store signs"*, with two photos: a steel pylon of
 * six stacked lightboxes, and a sari-sari row under soft-drink privilege signs. What the research found
 * and where each finding went (docs/reports/home-scene/phaister.md § 2.1):
 *
 *  - PRIVILEGE SIGNS. A sponsor's panel with the store's own name in a band beneath, the name usually
 *    the owner's ("Aling Nena"). The sponsor here is a made-up soft drink, "Kola"; never a real brand.
 *  - TARPAULINS: maximalist, cheap, everywhere, often plain black letters on a telco colour.
 *  - HAND-PAINTED LETTERING on the older trades (tyre shop, pisonet, carinderia): uneven, a little
 *    crooked, a drop shadow, letters that do not quite fit their board.
 *  - PYLON STACKS: a frame of lightboxes, each a different business in a different face and colour,
 *    a phone number on half of them, one or two faded or flickering.
 *  - The trades are the Gilmore strip's: computers first, then everything a street needs.
 *
 * PC Express is the one real name, because it is the map's authored showroom (set.tsx `pcExpress`).
 * No blue in anything here (CLAUDE.md § 6.4): telco colours are chartreuse, red and yellow.
 */

type Lines = { t: string; size: number; fill: string; font?: string; y: number; x?: number; anchor?: 'start' | 'middle' | 'end'; w?: number; rot?: number }[];

/**
 * Painted letters on a board, in metres (lettering.tsx: never a typeset font). `size` is the old
 * em size and sets the cap height; `font` 'Darumadrop One' now means "the looser script-ish hand"
 * (a lean, a thinner brush); `w` fits a line to a width, as a sign painter does; `rot` tips it.
 */
const words = (lines: Lines, dark = '#1E1414') =>
  lines.map((l, i) => {
    const loose = l.font === 'Darumadrop One';
    const h = l.size * 0.74;
    const body = (
      <Paint key={i} text={l.t} x={l.x ?? 0} y={l.y} h={h} fill={l.fill} fit={l.w} anchor={l.anchor ?? 'middle'}
        slant={loose ? 0.2 : 0} weight={loose ? 0.14 : h > 0.2 ? 0.2 : 0.16} wobble={loose ? 1 : 0.6}
        outline={h > 0.2 && !loose ? dark : undefined} shadow={h > 0.2 ? 'rgba(20,10,10,0.45)' : undefined} seed={`${l.t}${i}`} />
    );
    return l.rot ? <g key={i} transform={`rotate(${l.rot} ${l.x ?? 0} ${l.y})`}>{body}</g> : body;
  });

/** A board on the west row's shop fascias (read from the road, facing east). Origin: its south top corner. */
const westBoard = (v: View, z0: number, top: number, w: number, h: number, body: React.ReactNode, key: string): Item | null => {
  const g = onPlane(v, [-10.98, top, z0], FACING.east.u, FACING.east.dn, w, h, body, key);
  return g ? { z: depth(v, [-11, top - h / 2, z0 + w / 2]) - 1.2, node: g } : null;
};
/** On the east row, facing west. Origin: its north top corner. */
const eastBoard = (v: View, z0: number, top: number, w: number, h: number, body: React.ReactNode, key: string): Item | null => {
  const g = onPlane(v, [10.98, top, z0], FACING.west.u, FACING.west.dn, w, h, body, key);
  return g ? { z: depth(v, [11, top - h / 2, z0 - w / 2]) - 1.2, node: g } : null;
};

/** A lightbox: a lit face in a frame, glowing at night, optionally flickering like a tired tube. */
const lightbox = (w: number, h: number, face: string, night: string, frame: string, content: React.ReactNode, flick = 1) => (
  <g opacity={flick}>
    <rect x={-0.07} y={-0.07} width={w + 0.14} height={h + 0.14} fill={tone(frame)} />
    <rect width={w} height={h} fill={emit(face, night)} />
    {content}
  </g>
);

export const Signs = (v: View, f: number): Item[] => {
  const out: (Item | null)[] = [];

  // ------------------------------------------------------------------ the west row: the computer strip
  out.push(westBoard(v, -5.6, 4.35, 4.0, 0.95, lightbox(4.0, 0.95, '#F4EEE0', '#FFF6E0', '#6A5A50', <>
    {words([
      { t: 'COMPUTER REPAIR', size: 0.42, fill: tone('#8E1A1E'), y: 0.52, x: 2.0, w: 3.6 },
      { t: 'LAPTOP  ·  DESKTOP  ·  UPGRADE  ·  REFORMAT', size: 0.17, fill: tone('#3A2A26'), y: 0.8, x: 2.0, w: 3.6 },
    ])}
  </>), 'sg-repair'));
  // A yellow tarp strapped over the next shop's fascia, plain black letters, a little slack.
  out.push(westBoard(v, -11.6, 4.45, 4.1, 1.15, <g>
    <path d="M0 0 L4.1 0.04 L4.08 1.15 L0.02 1.1 Z" fill={tone('#F6D43A')} />
    <rect x={0} y={0} width={0.9} height={1.12} fill={tone('#E8483A')} />
    {words([
      { t: 'LAPTOP', size: 0.28, fill: tone('#FFF4D8'), y: 0.5, x: 0.45, w: 0.8, rot: -8 },
      { t: 'SA MURANG HALAGA!', size: 0.34, fill: tone('#1E1414'), y: 0.52, x: 2.5, w: 2.9 },
      { t: 'Brand new · 2nd hand · Installment', size: 0.15, fill: tone('#3A2A26'), y: 0.82, x: 2.5, w: 2.9, font: 'Darumadrop One' },
      { t: '0917-TUMP-000', size: 0.16, fill: tone('#8E1A1E'), y: 1.02, x: 2.5, w: 1.6 },
    ])}
  </g>, 'sg-tarp'));
  out.push(westBoard(v, 9.4, 4.4, 4.2, 1.0, lightbox(4.2, 1.0, '#2A2222', '#3A2A2A', '#1E1616', <>
    {words([
      { t: 'GAMING RIG', size: 0.4, fill: emit('#C8F040', '#E0FF6A'), y: 0.5, x: 2.1, w: 3.2 },
      { t: 'BUILD NA · RTX · RGB', size: 0.2, fill: emit('#F050C8', '#FF7AE0'), y: 0.82, x: 2.1, w: 2.8 },
    ])}
  </>), 'sg-gaming'));
  // Hand-painted: a copy-shop board, crooked, with a drop shadow on every letter.
  out.push(westBoard(v, 14.8, 4.2, 3.2, 0.95, <g>
    <rect width={3.2} height={0.95} fill={tone('#F7EEDC')} />
    {words([
      { t: 'PA-XEROX', size: 0.36, fill: tone('#3A2A26'), y: 0.46, x: 1.62, w: 2.6, rot: -2 },
      { t: 'PA-XEROX', size: 0.36, fill: tone('#C8322A'), y: 0.44, x: 1.6, w: 2.6, rot: -2 },
      { t: 'PRINT · LOAD · SCAN', size: 0.18, fill: tone('#2A7A3A'), y: 0.78, x: 1.6, w: 2.4, font: 'Darumadrop One' },
    ])}
  </g>, 'sg-xerox'));

  // ------------------------------------------------------------------ the east row
  // PISONET, hand-painted: the peso sign in a circle, "P1 = 5 MINS".
  out.push(eastBoard(v, 5.4, 4.3, 3.6, 1.0, <g>
    <rect width={3.6} height={1.0} fill={tone('#F2E6C8')} />
    <circle cx={0.55} cy={0.5} r={0.38} fill={tone('#F2C23A')} stroke={tone('#8E1A1E')} strokeWidth={0.05} />
    {words([
      { t: '₱1', size: 0.36, fill: tone('#8E1A1E'), y: 0.63, x: 0.55 },
      { t: 'PISONET', size: 0.42, fill: tone('#2A7A3A'), y: 0.52, x: 2.2, w: 2.3 },
      { t: '5 MINS · PRINT · SCAN', size: 0.16, fill: tone('#3A2A26'), y: 0.82, x: 2.2, w: 2.2, font: 'Darumadrop One' },
    ])}
  </g>, 'sg-pisonet'));
  out.push(eastBoard(v, -2.9, 4.4, 4.0, 1.0, lightbox(4.0, 1.0, '#8E1A1E', '#C8242A', '#4A1A1A', <>
    {words([
      { t: 'PARES MAMI', size: 0.46, fill: emit('#F8D048', '#FFE880'), y: 0.52, x: 2.0, w: 3.4 },
      { t: 'BEEF PARES · LUGAW · 24 HRS', size: 0.17, fill: emit('#FFF4D8', '#FFFFFF'), y: 0.84, x: 2.0, w: 3.2 },
    ])}
  </>, 0.85 + 0.15 * Math.max(0, loopNoise('pares', f, 5, 7))), 'sg-pares'));
  // THE SARI-SARI PRIVILEGE SIGN: the sponsor's panel, the store's name in a white band under it.
  out.push(eastBoard(v, -9.7, 4.9, 4.6, 1.75, <g>
    <rect x={-0.06} y={-0.06} width={4.72} height={1.87} fill={tone('#5A4A44')} />
    <rect width={4.6} height={1.2} fill={emit('#D8282A', '#F0343A')} />
    <circle cx={0.62} cy={0.6} r={0.46} fill={tone('#FFF4E6')} />
    <rect x={0.5} y={0.28} width={0.24} height={0.62} rx={0.08} fill={tone('#6A1A1E')} />
    {words([
      { t: 'Kola', size: 0.72, fill: emit('#FFFFFF', '#FFF6EE'), y: 0.78, x: 2.55, font: 'Darumadrop One', w: 2.3, rot: -4 },
      { t: 'SARAP NG MALAMIG!', size: 0.14, fill: tone('#FFE0A0'), y: 1.08, x: 2.6, w: 2.2 },
    ])}
    <rect y={1.2} width={4.6} height={0.55} fill={tone('#F7F2E6')} />
    {words([{ t: 'ALING NENA STORE', size: 0.36, fill: tone('#1E1414'), y: 1.62, x: 2.3, w: 3.8 }])}
  </g>, 'sg-sari'));
  // Taped to the grille under it, the joke every tindahan has.
  out.push(eastBoard(v, -11.2, 2.4, 1.5, 0.55, <g>
    <rect width={1.5} height={0.55} fill={tone('#FFF8E8')} transform="rotate(3)" />
    {words([
      { t: 'BAWAL UTANG', size: 0.17, fill: tone('#C8322A'), y: 0.24, x: 0.76, w: 1.3, rot: 3 },
      { t: 'bukas pwede', size: 0.14, fill: tone('#3A2A26'), y: 0.44, x: 0.78, font: 'Darumadrop One', rot: 3 },
    ])}
  </g>, 'sg-utang'));
  // The laminated boards every tindahan zip-ties to its grille (the owner's collage): chunky outlined
  // letters on a bright ground with a coloured border.
  out.push(eastBoard(v, -12.9, 2.5, 1.1, 0.6, <g>
    <rect width={1.1} height={0.6} fill={tone('#FFF8EC')} stroke={tone('#C8322A')} strokeWidth={0.05} />
    {words([
      { t: 'ICE', size: 0.3, fill: tone('#C8322A'), y: 0.3, x: 0.55, w: 0.6 },
      { t: 'FOR SALE', size: 0.16, fill: tone('#2A7A3A'), y: 0.52, x: 0.55, w: 0.9 },
    ], '#FFF8EC')}
  </g>, 'sg-ice'));
  out.push(eastBoard(v, -13.9, 2.2, 1.3, 0.62, <g>
    <rect width={1.3} height={0.62} fill={tone('#F8D83A')} stroke={tone('#1E1414')} strokeWidth={0.05} />
    {words([
      { t: 'LOAD NA', size: 0.22, fill: tone('#C8322A'), y: 0.28, x: 0.65, w: 1.1 },
      { t: 'DITO!', size: 0.24, fill: tone('#1E1414'), y: 0.54, x: 0.65, w: 0.8 },
    ], '#FFF8EC')}
  </g>, 'sg-load'));
  out.push(eastBoard(v, 13.4, 4.3, 3.9, 1.0, <g>
    <path d="M0 0 L3.9 0.05 L3.88 1.0 L0 0.96 Z" fill={tone('#C8E040')} />
    {words([
      { t: 'WATER REFILLING', size: 0.34, fill: tone('#1E1414'), y: 0.46, x: 1.95, w: 3.5 },
      { t: 'PURIFIED · ALKALINE · FREE DELIVERY', size: 0.14, fill: tone('#3A2A26'), y: 0.78, x: 1.95, w: 3.4 },
    ])}
  </g>, 'sg-water'));
  out.push(eastBoard(v, 18.8, 4.1, 3.0, 0.9, <g>
    <rect width={3.0} height={0.9} fill={tone('#2A2222')} />
    <circle cx={0.45} cy={0.45} r={0.36} fill={tone('#141010')} stroke={tone('#5A5050')} strokeWidth={0.1} />
    {words([{ t: 'VULCANIZING', size: 0.34, fill: tone('#F8D048'), y: 0.56, x: 1.8, w: 2.2, rot: -3 }])}
  </g>, 'sg-vulca'));

  // ------------------------------------------------------------------ the pylon stacks
  out.push(pylon(v, f, 9.6, 21.0, 'south'));
  out.push(pylon(v, f, -9.6, -21.5, 'north'));

  // ------------------------------------------------------------------ the barangay tarp on a column
  const tarp = onPlane(v, [-4.45 + 0.72, 3.4, 19 - 0.8], FACING.east.u, FACING.east.dn, 1.6, 1.1, <g>
    <rect width={1.6} height={1.1} fill={tone('#FFF4E0')} />
    <rect width={1.6} height={0.3} fill={tone('#C8322A')} />
    {words([
      { t: 'BRGY. MARIANA', size: 0.15, fill: tone('#FFF4E0'), y: 0.21, x: 0.8, w: 1.3 },
      { t: 'LIGA NG', size: 0.16, fill: tone('#3A2A26'), y: 0.5, x: 0.8 },
      { t: 'TUMBANG PRESO', size: 0.2, fill: tone('#8E1A1E'), y: 0.74, x: 0.8, w: 1.45 },
      { t: 'SABADO · 4PM · SA ILALIM', size: 0.1, fill: tone('#3A2A26'), y: 0.96, x: 0.8, w: 1.4 },
    ])}
  </g>, 'sg-liga');
  if (tarp) out.push({ z: depth(v, [-3.7, 2.9, 19]) - 0.1, node: tarp });

  return out.filter((x): x is Item => x !== null);
};

/**
 * A steel pylon of stacked lightboxes, the owner's photo: six businesses in six faces, a phone number
 * on some, the one at the bottom faded, one flickering. `facing` is the way its faces look.
 */
const pylon = (v: View, f: number, x: number, z: number, facing: 'south' | 'north'): Item | null => {
  const fc = FACING[facing];
  const w = 3.2;
  const x0 = facing === 'south' ? x - w / 2 : x + w / 2;
  const zf = facing === 'south' ? z - 0.2 : z + 0.2;
  const nodes: React.ReactNode[] = [];
  for (const px of [x - w / 2 + 0.1, x + w / 2 - 0.1]) nodes.push(...box(v, px - 0.08, px + 0.08, PAVE_TOP, 9.6, z - 0.1, z + 0.1, { side: '#6A6260' }));
  const panels: { h: number; face: string; night: string; body: React.ReactNode; flick?: boolean }[] = [
    { h: 0.9, face: '#F4EEE0', night: '#FFF4E0', body: <>{words([{ t: 'GILMORE TECH HUB', size: 0.34, fill: tone('#8E1A1E'), y: 0.5, x: w / 2, w: 2.9 }, { t: 'LEVEL 2 · 40 STALLS', size: 0.14, fill: tone('#3A2A26'), y: 0.76, x: w / 2 }])}</> },
    { h: 0.85, face: '#C8322A', night: '#FF4A3A', body: <>{words([{ t: 'RAM · SSD · GPU', size: 0.3, fill: emit('#FFE880', '#FFF0A0'), y: 0.46, x: w / 2, w: 2.6 }, { t: 'MURANG PRESYO', size: 0.16, fill: emit('#FFFFFF', '#FFFFFF'), y: 0.72, x: w / 2 }])}</> },
    { h: 0.8, face: '#1E1818', night: '#2A2020', body: <>{words([{ t: 'Pinky Nails & Spa', size: 0.3, fill: emit('#F07AB0', '#FF9AD0'), y: 0.48, x: w / 2, font: 'Darumadrop One', w: 2.6 }, { t: 'FOOT SPA · MANI · PEDI', size: 0.12, fill: emit('#F4EEE0', '#FFFFFF'), y: 0.7, x: w / 2 }])}</>, flick: true },
    { h: 0.85, face: '#C8E040', night: '#E0FF6A', body: <>{words([{ t: 'CP REPAIR · UNLOCK', size: 0.26, fill: tone('#1E1414'), y: 0.44, x: w / 2, w: 2.8 }, { t: 'LOAD · E-LOAD · GCASH', size: 0.15, fill: tone('#3A2A26'), y: 0.7, x: w / 2 }])}</> },
    { h: 1.0, face: '#8E1A3A', night: '#B8244A', body: <>{words([{ t: 'DENTAL CLINIC', size: 0.3, fill: emit('#FFF4E0', '#FFFFFF'), y: 0.42, x: w / 2, w: 2.7 }, { t: 'Dra. L. Santos', size: 0.16, fill: emit('#F8D048', '#FFE880'), y: 0.64, x: w / 2, font: 'Darumadrop One' }, { t: '8-TUMP-0000', size: 0.2, fill: emit('#FFF4E0', '#FFFFFF'), y: 0.9, x: w / 2 }])}</> },
  ];
  let top = 9.4;
  for (const [i, p] of panels.entries()) {
    const flick = p.flick ? 0.7 + 0.3 * Math.max(0, loopNoise(`py${x}${i}`, f, 6, 9)) : 1;
    const g = onPlane(v, [x0, top, zf], fc.u, fc.dn, w, p.h, lightbox(w, p.h, p.face, p.night, '#4A4442', p.body, flick));
    if (g) nodes.push(g);
    top -= p.h + 0.22;
  }
  return { z: depth(v, [x, 6, z]), node: <g key={`py${x}`}>{nodes}</g> };
};

/** The pares cart and the pisonet cabinets on the east pavement, where the map stands them. */
export const Stalls = (v: View, f: number): Item[] => {
  const items: Item[] = [];
  {
    const { x, z } = PARES;
    const nodes: React.ReactNode[] = [];
    nodes.push(...box(v, x - 0.5, x + 0.5, 0.45, 1.25, z - 0.8, z + 0.8, { side: '#D98C9A', top: '#E8D8C8', end: '#C87888' }));
    nodes.push(...box(v, x - 0.06, x + 0.06, 1.25, 2.4, z - 0.05, z + 0.05, { side: '#E8E0D0' }));
    nodes.push(...box(v, x - 1.1, x + 1.1, 2.35, 2.5, z - 1.1, z + 1.1, { side: '#A898B8', top: '#B8A8C0', bottom: '#8E7E9E' }));
    nodes.push(...box(v, x - 0.3, x + 0.1, 1.25, 1.6, z - 0.5, z - 0.1, { side: '#B8B0A4', top: '#8A8278' }));
    nodes.push(...box(v, x - 0.3, x + 0.1, 1.25, 1.6, z + 0.1, z + 0.5, { side: '#B8B0A4', top: '#8A8278' }));
    for (const dz of [-0.6, 0.6]) nodes.push(...box(v, x - 0.45, x - 0.3, PAVE_TOP, 0.45, z + dz - 0.08, z + dz + 0.08, { side: '#2A2222' }));
    for (let i = 0; i < 5; i++) {
      const ph = ((f * 25) / LOOP + i / 5) % 1;
      const [sx, sy] = [x - 0.1 + 0.08 * Math.sin(ph * 6 + i), 1.7 + ph * 1.0];
      const d = onPlane(v, [sx, sy, z - 0.2], FACING.west.u, FACING.west.dn, 0.2, 0.2, <rect width={0.2 + ph * 0.3} height={0.2 + ph * 0.3} fill="#F7EEE4" opacity={0.35 * (1 - ph)} />);
      if (d) nodes.push(d);
    }
    const ab = onPlane(v, [x - 1.2, 1.2, z + 1.9], FACING.west.u, FACING.west.dn, 0.9, 0.95, <g>
      <rect width={0.9} height={0.95} fill={tone('#2A2222')} />
      {words([{ t: 'PARES', size: 0.22, fill: tone('#F8D048'), y: 0.34, x: 0.45 }, { t: '₱60', size: 0.2, fill: tone('#F7EEDC'), y: 0.6, x: 0.45 }, { t: 'unli sabaw', size: 0.11, fill: tone('#F07AB0'), y: 0.8, x: 0.45, font: 'Darumadrop One' }])}
    </g>);
    if (ab) nodes.push(ab);
    items.push({ z: depth(v, [x, 1.2, z]), node: <g key="pares">{nodes}</g> });
  }
  {
    const { x, z } = PISONET;
    const nodes: React.ReactNode[] = [];
    for (const dz of [-0.55, 0.55]) {
      nodes.push(...box(v, x - 0.35, x + 0.35, PAVE_TOP, 1.7, z + dz - 0.4, z + dz + 0.4, { side: '#3A7A4A', top: '#2A5A3A', end: '#2E6A3E' }));
      const scr = onPlane(v, [x - 0.36, 1.5, z + dz + 0.3], FACING.west.u, FACING.west.dn, 0.6, 0.45, <rect width={0.6} height={0.45} fill={emit('#E8F0C8', '#F8FFD8')} opacity={0.85 + 0.15 * loopNoise(`pn${dz}`, f, 4, 5)} />);
      if (scr) nodes.push(scr);
    }
    items.push({ z: depth(v, [x, 1, z]), node: <g key="pisonet">{nodes}</g> });
  }
  void lit;
  return items;
};

export type { V3 };
