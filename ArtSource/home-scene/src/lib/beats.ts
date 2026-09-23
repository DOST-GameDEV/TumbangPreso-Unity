import { s } from './time';

/**
 * ⚠️ THE ONE TIMETABLE. Every shot, every effect and the sound-cue list in
 * docs/reports/home-scene/README.md § 4 read these numbers, so a beat moved here moves
 * everywhere at once. Frames at 30 fps; the loop is `LOOP` (1260, 42 s).
 *
 * The first two seconds are calm on purpose: the hub's own UI arrives over them.
 *
 * ⚠️ THE CHASE IS THE OTHER HALF OF THE PLAY. 🧑 2026-09-23: *"ITS supposed to show running"*.
 * In tumbang preso the throw is half of it: a thrower who knocks the can must then RUN for his
 * tsinelas while the taya resets the can and tries to tag him. So after TUMP! Zack goes on Bolt
 * Sprint for it, Magnet snaps it into his hand as he slides past, and Sean's tag closes on the
 * afterimage he leaves behind.
 */
/**
 * ⚠️⚠️ EVERY MOMENT GETS ROOM TO BREATHE. 🧑 2026-09-23 on the 30 s cut: *"its going so fast i
 * cant comprehhend it anymore"*, *"give each moment more time to breath"*, and expressly not by
 * "changing a slider": *"create more frames for each part happening and let it flow naturally"*.
 * So this is a RE-TIMING, not a time-stretch: the beats below are spaced for reading (the face
 * holds before the eyes ignite, the aim holds before the snap, TUMP! holds before the run), and
 * every in-beat offset in every shot is written `B.x + bt(n)`, which gives each key `K` times the
 * frames it had. Cycles that should keep their natural rate (the run's legs, sparks, the idle
 * toss) are NOT scaled, which is what keeps it from reading as slow motion.
 */
export const K = 1.6;
/** An in-beat duration in the 30 s cut's frames, re-timed. */
export const bt = (n: number) => Math.round(n * K);

export const B = {
  pushIn: s(2.5), // the wind rises, the camera starts in
  cu: s(4.9), // cut to his face
  eyesOpen: s(6.7), // his eyes ignite (her frames 13 to 15)
  black: s(7.2), // impact frame, black, only the arcs (her 16 to 17)
  yellow: s(7.45), // impact frame, the golden flash (her 18)
  powered: s(7.6), // powered-up close-up (her 19 to 20)
  hand: s(8.7), // his grip on the tsinelas (her 21 to 24)
  reverse: s(9.8), // over the shoulder, the lata down the court (her 25 to 27)
  snap: s(11.0), // the throw, after a held aim
  hit: s(11.35), // contact: the frozen frames
  tump: s(11.5), // the can flies and TUMP! lands (her 31 to 34)
  run: s(13.4), // Bolt Sprint for the tsinelas, side-on tracking shot
  scoop: s(15.2), // the slide; Magnet snaps it into his hand, in slow motion
  turn: s(16.1), // he plants and turns back
  lunge: s(16.8), // Sean's tag closes on the afterimage, in slow motion
  arrive: s(17.9), // back in the wide: the run home onto the throw line
  settle: s(18.9), // the dead stop, the chatter, and the long calm begins
  // Idle personality, inside the calm (LORE.md: "makes a difficult play look almost casual").
  peek: s(22.5),
  flip: s(27.0),
  spin: s(32.0),
  shake: s(37.0),
};

