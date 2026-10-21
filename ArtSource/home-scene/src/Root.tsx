import React from 'react';
import { Composition } from 'remotion';
import { HomeScene } from './HomeScene';
import { ZackRef } from './ZackRef';
import { ActorTest } from './ActorTest';
import { PhaisterBoard } from './phaister/Board';
import { PhaisterScene } from './phaister/PhaisterScene';
import { LOOP as P_LOOP } from './phaister/time';
import { LOOP, FPS } from './lib/time';

export const Root: React.FC = () => (
  <>
    <Composition
      id="HomeScene"
      component={HomeScene}
      durationInFrames={LOOP}
      fps={FPS}
      width={1920}
      height={1080}
    />
    <Composition id="ActorTest" component={ActorTest} durationInFrames={30} fps={FPS} width={1920} height={1080} />
    <Composition id="PhaisterScene" component={PhaisterScene} durationInFrames={P_LOOP} fps={FPS} width={1920} height={1080} />
    <Composition id="PhaisterBoard" component={PhaisterBoard} durationInFrames={4} fps={FPS} width={1920} height={1080} />
    <Composition id="ZackRef" component={ZackRef} durationInFrames={8} fps={FPS} width={1000} height={1000} />
  </>
);
