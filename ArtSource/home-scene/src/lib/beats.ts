import { s } from './time';

/**
 * ⚠️ THE ONE TIMETABLE. Every shot, every effect and the sound-cue list in
 * docs/reports/home-scene/README.md § 4 read these numbers, so a beat moved here moves
 * everywhere at once. Frames at 30 fps; the loop is 900.
 *
 * The first two seconds are calm on purpose: the hub's own UI arrives over them.
 *
 * ⚠️ THE CHASE IS THE OTHER HALF OF THE PLAY. 🧑 2026-09-23: *"ITS supposed to show running"*.
 * In tumbang preso the throw is half of it: a thrower who knocks the can must then RUN for his
 * tsinelas while the taya resets the can and tries to tag him. So after TUMP! Zack goes on Bolt
 * Sprint for it, Magnet snaps it into his hand as he slides past, and Sean's tag closes on the
 * afterimage he leaves behind.
 */
export const B = {
  pushIn: s(2.0), // the wind rises, the camera starts in
  cu: s(3.5), // cut to his face
  eyesOpen: s(4.6), // her frames 13 to 15
  black: s(4.9), // impact frame, black, only the eyes (her 16 to 17)
  yellow: s(5.07), // impact frame, the golden flash (her 18)
  powered: s(5.17), // powered-up close-up (her 19 to 20)
  hand: s(5.8), // his grip on the tsinelas (her 21 to 24)
  reverse: s(6.4), // over the shoulder, the lata down the court (her 25 to 27)
  snap: s(7.0), // the throw
  hit: s(7.23), // contact: three frozen frames
  tump: s(7.33), // the can flies and TUMP! lands (her 31 to 34)
  run: s(8.4), // Bolt Sprint for the tsinelas, side-on tracking shot
  scoop: s(9.6), // the slide; Magnet snaps it into his hand, in slow motion
  turn: s(10.2), // he plants and turns back
  lunge: s(10.6), // Sean's tag closes on the afterimage, in slow motion
  arrive: s(11.3), // back in the wide: the skid onto the throw line
  settle: s(11.9), // the dead stop, the chatter, and the long calm begins
  // Idle personality, inside the calm (LORE.md: "makes a difficult play look almost casual").
  peek: s(14.6),
  flip: s(17.8),
  spin: s(22.4),
  shake: s(26.8),
};

