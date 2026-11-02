import React from 'react';
import { Composition } from 'remotion';
import { PoseDump } from './bgc/PoseDump';
import { HomeScene } from './HomeScene';
import { ZackRef } from './ZackRef';
import { ActorTest } from './ActorTest';
import { PhaisterBoard } from './phaister/Board';
import { PhaisterScene } from './phaister/PhaisterScene';
import { LOOP as P_LOOP } from './phaister/time';
import { LOOP, FPS } from './lib/time';
import { BgcScene } from './bgc/BgcScene';
import { ClipBoard, CLIP_PAGES } from './bgc/ClipBoard';
import { IDLE, INTRO } from './bgc/time';
import { ErmitaScene } from './ermita/ErmitaScene';

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
    <Composition id="ClipBoard" component={ClipBoard} durationInFrames={CLIP_PAGES} fps={FPS} width={1920} height={1080} />
    <Composition id="ZackPoses" component={PoseDump} durationInFrames={1} fps={FPS} width={16} height={16} />
    <Composition id="ZackIntro" component={BgcScene} defaultProps={{ part: 'intro' as const }} durationInFrames={INTRO * 2} fps={FPS * 2} width={1920} height={1080} />
    <Composition id="PhaisterIntro" component={ErmitaScene} defaultProps={{ part: 'intro' as const }} durationInFrames={INTRO * 2} fps={FPS * 2} width={1920} height={1080} />
    <Composition id="PhaisterIdle" component={ErmitaScene} defaultProps={{ part: 'idle' as const }} durationInFrames={IDLE} fps={FPS} width={1920} height={1080} />
    <Composition id="ZackIdle" component={BgcScene} defaultProps={{ part: 'idle' as const }} durationInFrames={IDLE} fps={FPS} width={1920} height={1080} />
  </>
);
