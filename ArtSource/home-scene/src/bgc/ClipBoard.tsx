import React from 'react';
import { AbsoluteFill, useCurrentFrame } from 'remotion';
import { ActorImage, drawActor, useActor } from '../three/actor';

/** Test board (method § 2): the redesigned models' own clips, sampled through their length. Frame = page. */
const PAGES: ['zack-redesign' | 'sean-redesign', string][] = [
  ['zack-redesign', 'idle'], ['zack-redesign', 'hero-zack-bankshot'], ['zack-redesign', 'hero-zack-sprint'], ['zack-redesign', 'sprint'], ['zack-redesign', 'slide'],
  ['zack-redesign', 'pick-up'], ['zack-redesign', 'holding-right'], ['zack-redesign', 'emote-no'], ['zack-redesign', 'hero-zack-charge'], ['zack-redesign', 'hero-zack-circuit'],
  ['sean-redesign', 'idle'], ['sean-redesign', 'walk'], ['sean-redesign', 'pick-up'], ['sean-redesign', 'attack-melee-right'], ['sean-redesign', 'hero-sean-dash'], ['sean-redesign', 'hero-sean-ignite'], ['sean-redesign', 'emote-yes'], ['sean-redesign', 'hero-sean-gate'],
];
export const CLIP_PAGES = PAGES.length;

export const ClipBoard: React.FC = () => {
  const f = useCurrentFrame();
  const z = useActor('zack-redesign');
  const r = useActor('sean-redesign');
  const [m, name] = PAGES[f % PAGES.length];
  const b = m === 'zack-redesign' ? z : r;
  if (!b) return null;
  const d = b.clips[name]?.duration ?? 1;
  const cells = Array.from({ length: 8 }, (_, i) => {
    const img = drawActor(b, { x: 240 + (i % 4) * 480, y: 270 + Math.floor(i / 4) * 500, ppu: 560, focus: 0.4, reach: 0.72, yaw: 25, pitch: 6, clip: name, t: (d * i) / 8, res: 1 });
    return <ActorImage key={i} d={img} />;
  });
  return (
    <AbsoluteFill style={{ background: '#D9BE9A' }}>
      <svg width={1920} height={1080} viewBox="0 0 1920 1080">
        {cells}
        <text x={20} y={46} fontSize={40} fill="#300">{`${m} ${name} ${d.toFixed(2)}s`}</text>
      </svg>
    </AbsoluteFill>
  );
};
