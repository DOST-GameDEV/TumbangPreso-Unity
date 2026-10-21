import React from 'react';
import { random } from 'remotion';

/**
 * ⚠️ MARKER TEXTURE, AND WHY EVERY FILL CARRIES IT. The owner's bar for this piece (a
 * hand-drawn loop he sent on 2026-09-23) hatches every single surface; a flat vector fill next
 * to it reads as clip art however good the drawing is. Her own logo is digital marker, so the
 * texture is marker: long, slightly curved, semi-transparent strokes laid at one hand angle,
 * a few darker, a few lighter, over the flat fill. One pattern per colour, generated from a
 * seed so the strokes are identical on every frame (the boil filter is what moves them).
 */

const shade = (hex: string, k: number) => {
  const n = parseInt(hex.slice(1), 16);
  const ch = (v: number) => Math.max(0, Math.min(255, Math.round(k >= 0 ? v + (255 - v) * k : v * (1 + k))));
  const r = ch((n >> 16) & 255);
  const g = ch((n >> 8) & 255);
  const b = ch(n & 255);
  return `#${((1 << 24) | (r << 16) | (g << 8) | b).toString(16).slice(1)}`;
};

export const texId = (hex: string) => `tx-${hex.replace('#', '').toLowerCase()}`;
export const tex = (hex: string) => `url(#${texId(hex)})`;

const TILE = 360;

const strokesFor = (seed: string, n: number, len: number, tile: number) => {
  const out: string[] = [];
  for (let i = 0; i < n; i++) {
    const x = random(`${seed}x${i}`) * tile;
    const y = random(`${seed}y${i}`) * tile;
    const l = len * (0.5 + random(`${seed}l${i}`));
    const ang = -0.62 + (random(`${seed}a${i}`) - 0.5) * 0.3;
    const bend = (random(`${seed}b${i}`) - 0.5) * l * 0.18;
    const x2 = x + Math.cos(ang) * l;
    const y2 = y + Math.sin(ang) * l;
    const mx = (x + x2) / 2 - Math.sin(ang) * bend;
    const my = (y + y2) / 2 + Math.cos(ang) * bend;
    // Draw each stroke at the tile's wraps too, so the seams of the pattern never show a cut.
    for (const [ox, oy] of [[0, 0], [-tile, 0], [0, -tile], [-tile, -tile], [tile, 0], [0, tile]]) {
      out.push(`M${(x + ox).toFixed(1)} ${(y + oy).toFixed(1)}Q${(mx + ox).toFixed(1)} ${(my + oy).toFixed(1)} ${(x2 + ox).toFixed(1)} ${(y2 + oy).toFixed(1)}`);
    }
  }
  return out.join('');
};

export const TexturePattern: React.FC<{ hex: string; strength?: number }> = ({ hex, strength = 1 }) => {
  const id = texId(hex);
  const dark = shade(hex, -0.14 * strength);
  const darker = shade(hex, -0.24 * strength);
  const light = shade(hex, 0.16 * strength);
  return (
    <pattern id={id} width={TILE} height={TILE} patternUnits="userSpaceOnUse">
      <rect width={TILE} height={TILE} fill={hex} />
      <path d={strokesFor(id + 'd', 70, 120, TILE)} stroke={dark} strokeWidth={9} strokeLinecap="round" fill="none" opacity={0.22} />
      <path d={strokesFor(id + 'D', 26, 90, TILE)} stroke={darker} strokeWidth={5} strokeLinecap="round" fill="none" opacity={0.18} />
      <path d={strokesFor(id + 'l', 40, 100, TILE)} stroke={light} strokeWidth={7} strokeLinecap="round" fill="none" opacity={0.22} />
      <path d={strokesFor(id + 'h', 60, 26, TILE)} stroke={darker} strokeWidth={2} strokeLinecap="round" fill="none" opacity={0.15} />
    </pattern>
  );
};

/** Registers a texture for every colour the scene fills with. */
export const Textures: React.FC<{ colours: string[] }> = ({ colours }) => (
  <>
    {Array.from(new Set(colours.map((c) => c.toUpperCase()))).map((c) => (
      <TexturePattern key={c} hex={c} />
    ))}
  </>
);
