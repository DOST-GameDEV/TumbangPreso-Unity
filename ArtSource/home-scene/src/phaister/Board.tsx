import React from 'react';
import { AbsoluteFill, useCurrentFrame } from 'remotion';
import { ActorImage, drawActor, drawKuro, useActor, useKuro } from '../three/actor';

/**
 * Her test board (docs/HOME_SCREEN_ANIMATION_METHOD.md § 2.2: always board the model first). Each frame
 * of this composition is one sheet: frame 0 samples the game clips this loop uses, frame 1 Nemu's, frame
 * 2 Kuro's faces. Look at it before choreographing to a clip.
 */
const SHEETS: { model: 'team-phaister' | 'team-nemu'; cells: [string, number, number][] }[] = [
  {
    model: 'team-phaister',
    cells: [
      ['hero-phaister-eclipse', 0.0, 0], ['hero-phaister-eclipse', 0.3, 0], ['hero-phaister-eclipse', 0.6, 0], ['hero-phaister-eclipse', 0.9, 0],
      ['hero-phaister-eclipse', 1.2, 0], ['hero-phaister-eclipse', 1.5, 0], ['hero-phaister-eclipse', 1.8, 0], ['hero-phaister-eclipse', 2.1, 0],
      ['hero-phaister-blink', 0.0, 0], ['hero-phaister-blink', 0.15, 0], ['hero-phaister-blink', 0.3, 0], ['hero-phaister-blink', 0.45, 0],
      ['hero-phaister-hex', 0.1, 0], ['hero-phaister-hex', 0.3, 0], ['hero-phaister-hex', 0.5, 0], ['sprint', 0.12, 30],
      ['slide', 0.2, 30], ['slide', 0.5, 30], ['walk', 0.2, 60], ['attack-melee-right', 0.2, 40],
    ],
  },
  {
    model: 'team-nemu',
    cells: [
      ['idle', 0.3, 0], ['hero-nemu-ghoststep', 0.2, 0], ['hero-nemu-ghoststep', 0.5, 20], ['hero-nemu-project', 0.25, 0],
      ['hero-nemu-seance', 0.3, 0], ['hero-nemu-seance', 0.7, 0], ['emote-no', 0.3, 0], ['static', 0, 30],
    ],
  },
];

export const PhaisterBoard: React.FC = () => {
  const f = useCurrentFrame();
  const ph = useActor('team-phaister', true);
  const ne = useActor('team-nemu');
  const kuro = useKuro();
  if (!ph || !ne || !kuro) return null;
  const cells: React.ReactNode[] = [];
  if (f < 2) {
    const sh = SHEETS[f];
    const b = sh.model === 'team-phaister' ? ph : ne;
    sh.cells.forEach(([clip, t, yaw], i) => {
      const cx = 120 + (i % 7) * 260;
      const cy = 150 + Math.floor(i / 7) * 330;
      const d = drawActor(b, { x: cx, y: cy, ppu: 300, focus: 0.45, reach: 0.62, clip, t, yaw, pitch: 6, res: 1, slipper: sh.model === 'team-phaister' ? { at: 'hand' } : undefined });
      cells.push(<g key={i}><ActorImage d={d} /><text x={cx} y={cy + 170} textAnchor="middle" fontSize={18} fill="#222">{`${clip} ${t}`}</text></g>);
    });
  } else {
    // The game's faces on his restored body: rest, then CatSmile, GoofyDizzy and ShyPout exactly as
    // `PoseIdleExpression` shows them, then the loop's surprise.
    const faces = [
      {},
      { show: ['KuroCatMouth'], hide: ['ghost-mouth-dot'] },
      { show: ['KuroCrossLeft', 'KuroCrossRight', 'KuroGoofyMouth'], hide: ['ghost-mouth-dot', 'ghost-eye-l', 'ghost-eye-r'] },
      { show: ['KuroShyEye', 'KuroPoutMouth'], hide: ['ghost-mouth-dot', 'ghost-eye-r'] },
      { show: ['KuroHappyEyeL', 'KuroHappyEyeR', 'KuroGrinMouth'], hide: ['ghost-mouth-dot', 'ghost-eye-l', 'ghost-eye-r'] },
      { show: ['KuroSparkleL', 'KuroSparkleR', 'KuroOhMouth'], hide: ['ghost-mouth-dot'], eyes: 0.5 },
      { show: ['KuroSleepEyeL', 'KuroSleepEyeR'], hide: ['ghost-eye-l', 'ghost-eye-r'] },
      { show: ['KuroHappyEyeL', 'KuroHappyEyeR', 'KuroCatMouth', 'KuroTongue'], hide: ['ghost-mouth-dot', 'ghost-eye-l', 'ghost-eye-r'] },
      { show: ['KuroHeartEyeL', 'KuroHeartEyeR', 'KuroCatMouth'], hide: ['ghost-mouth-dot', 'ghost-eye-l', 'ghost-eye-r'] },
    ];
    faces.forEach((fc, i) => {
      const d = drawKuro(kuro, { x: 130 + i * 208, y: 540, ppu: 2600, yaw: 0, pitch: 4, face: fc, res: 1 });
      cells.push(<ActorImage key={i} d={d} />);
    });
  }
  return (
    <AbsoluteFill style={{ backgroundColor: '#E8DCC8' }}>
      <svg width={1920} height={1080}>{cells}</svg>
    </AbsoluteFill>
  );
};
