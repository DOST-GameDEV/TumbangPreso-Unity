import React from 'react';
import { Motes, WideSet } from '../art/roofdeck';

/**
 * The frontal wide: the shot the hub sits on for most of the loop.
 *
 * ⚠️ PARALLAX. A push-in scales every layer by a different amount about the same point, the sky
 * least and Zack most, which is what makes a flat painting read as a place with depth. The
 * depths are fractions of one camera's zoom, not separate cameras, so they can never disagree.
 */
export type Cam = { zoom: number; cx: number; cy: number; shakeX?: number; shakeY?: number; rot?: number };

export const layerTransform = (cam: Cam, depth: number) => {
  const z = 1 + (cam.zoom - 1) * depth;
  const cx = 960 + (cam.cx - 960) * depth;
  const cy = 540 + (cam.cy - 540) * depth;
  return `translate(${960 + (cam.shakeX ?? 0)} ${540 + (cam.shakeY ?? 0)}) rotate(${(cam.rot ?? 0) * (0.4 + 0.6 * depth)}) scale(${z}) translate(${-cx} ${-cy})`;
};

/**
 * ⚠️ Where he stands and how big: his face has to sit inside every aspect the hub supports
 * (x 240 to 1680 at 4:3, y 132 to 948 on the owner's 1600x680 window), and the sun behind him.
 * `ppu` is pixels per model unit: the model is 0.79 units tall, so he stands 711 px, and the tsinelas hanging from his fist clears the bottom edge.
 */
export const ZACK_AT = { x: 960, y: 1012, ppu: 900 };

/** Where the actor's focus point goes for a given model-space focus height. */
export const zackAnchor = (dx = 0, focus = 0.4) => ({ x: ZACK_AT.x + dx, y: ZACK_AT.y - focus * ZACK_AT.ppu });

export const Wide: React.FC<{ f: number; cam: Cam; figure: React.ReactNode; under?: React.ReactNode; over?: React.ReactNode }> = ({ f, cam, figure, under, over }) => (
  <g>
    <g transform={layerTransform(cam, 0.3)} filter="url(#boilSoft)">
      <WideSet f={f} layer="sky" />
    </g>
    <g transform={layerTransform(cam, 0.5)} filter="url(#boilSoft)">
      <WideSet f={f} layer="city" />
    </g>
    <g transform={layerTransform(cam, 0.8)} filter="url(#boilSoft)">
      <WideSet f={f} layer="mid" />
      <Motes f={f} n={34} />
    </g>
    <g transform={layerTransform(cam, 1)}>
      {under}
      {figure}
      {over}
    </g>
    {/* A few motes between us and him, nearer and softer. */}
    <g transform={layerTransform(cam, 1.15)} filter="url(#haze)">
      <Motes f={f + 311} n={10} />
    </g>
  </g>
);
