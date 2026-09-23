import React, { useEffect, useState } from 'react';
import { AbsoluteFill, continueRender, delayRender, staticFile, useCurrentFrame } from 'remotion';
import { Defs } from './lib/Defs';
import { Textures } from './lib/texture';
import { B } from './lib/beats';
import { WideActs } from './shots/WideActs';
import { CloseUp, Impact } from './shots/CloseUp';
import { Hand } from './shots/Hand';
import { Reverse } from './shots/Reverse';
import { Chase } from './shots/Chase';

const FILL_COLOURS: string[] = [];

const useFonts = () => {
  const [handle] = useState(() => delayRender('fonts'));
  useEffect(() => {
    const faces = [
      new FontFace('Darumadrop One', `url(${staticFile('fonts/DarumadropOne-Regular.ttf')})`),
      new FontFace('Paalalabas', `url(${staticFile('fonts/PaalalabasDisplayWide.otf')})`),
    ];
    Promise.all(faces.map((f) => f.load()))
      .then((loaded) => {
        loaded.forEach((f) => (document.fonts as unknown as { add: (x: FontFace) => void }).add(f));
        continueRender(handle);
      })
      .catch(() => continueRender(handle));
  }, [handle]);
};

/**
 * ⚠️ THE CUT LIST. One shot is on screen per frame, chosen here and nowhere else, from the
 * timetable in lib/beats.ts. docs/reports/home-scene/README.md § 4 is this list in prose.
 */
export const shotAt = (f: number) => {
  if (f < B.cu) return 'wide';
  if (f < B.black) return 'closeup';
  if (f < B.powered) return 'impact';
  if (f < B.hand) return 'closeup';
  if (f < B.reverse) return 'hand';
  if (f < B.run) return 'reverse';
  if (f < B.arrive) return 'chase';
  return 'wide';
};

export const HomeScene: React.FC = () => {
  useFonts();
  const f = useCurrentFrame();
  const shot = shotAt(f);
  return (
    <AbsoluteFill style={{ backgroundColor: '#560815' }}>
      <svg width={1920} height={1080} viewBox="0 0 1920 1080">
        <Defs frame={f} />
        <defs>
          <Textures colours={FILL_COLOURS} />
        </defs>
        {shot === 'wide' && <WideActs f={f} />}
        {shot === 'closeup' && <CloseUp f={f} />}
        {shot === 'impact' && <Impact f={f} />}
        {shot === 'hand' && <Hand f={f} />}
        {shot === 'reverse' && <Reverse f={f} />}
        {shot === 'chase' && <Chase f={f} />}
        <rect width={1920} height={1080} filter="url(#grain)" opacity={0.45} />
      </svg>
    </AbsoluteFill>
  );
};
