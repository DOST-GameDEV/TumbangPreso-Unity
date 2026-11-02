import { s } from './time';

/**
 * ⚠️ THE ONE TIMETABLE for "Green Light", Zack's film at BGC. docs/reports/home-scene/zack-bgc.md is
 * this list in prose. Every other file reads these names.
 *
 * Rago is taya and stands square in front of the can. Zack waits for the traffic light, curves the
 * slipper round him, and it takes the can from behind. Rago's whole body goes. Then the retrieval: Rago
 * rights the can and goes straight after Zack the moment he reaches the slipper, a real chase round the
 * court. It ends on Zack looking into the lens as his eyes light for the first time.
 *
 * ⚠️ THE CRUX: Backtrack, in slow motion. Rago's hand is on him and time nearly stops. Zack drifts away,
 * Rago's head tries to follow, they lock eyes, it is too late. Then real time: the yank, and Rago's foot
 * finds the can he set up. He gets up, rights it again and chalks one up. He keeps the score.
 */
export const B = {
  // Small to whole: the light, the foot, the wall. Zack's whole figure is saved for `whole`.
  signal: s(0),
  spark: s(1.2), // current runs along the wire
  feet: s(5.0), // a sneaker tapping on the chalk
  rago: s(8.2), // the wall
  chin: s(10), // he lifts his chin a little: go on then
  whole: s(12.4), // Zack at last, and he is not looking at the can
  lookUp: s(13), // he looks up at the signal
  finger: s(13), // Rago settles back as Zack looks away
  amber: s(14.6), // cut back to the signal: red, amber, and the green held long enough to name
  green: s(16.4),
  eyes: s(17.8), // his face: the corner of a smile. No lightning yet, that is saved for the chase
  // The bank: one wide frame holds all of it.
  wide: s(19.4), // the angle draws itself: one curve from his hand round Rago to the can
  snap: s(21),
  clang: s(21.7), // the arc's bend: the current snaps it round
  closeOn: s(21.98), // cut low behind the can, Rago's back over it: the slipper comes in from behind him
  tump: s(22.3), // the can
  deadpan: s(23.25), // cut to Rago's front. He has not moved.
  noLook: s(21.2), // a slow turn of the head toward the noise
  jolt: s(23.3), // he sees it: the whole body goes, crash zoom, "!"
  stare: s(25.4), // cut to Zack, who is enjoying this
  // The retrieval
  run: s(27.2),
  reset: s(28.1), // Rago rights the can, in its own shot, and goes straight from it into the chase
  scoop: s(29.0),
  look: s(32), // the chase ends on him: he looks into the lens and only now the eyes light
  wall: s(32.68), // cut to the side lens for the tag
  commit: s(32.7), // the lunge for the tag
  snapBack: s(33.0), // his hand is on him: Backtrack catches, and time nearly stops
  lockR: s(33.7), // over Zack's shoulder: Rago's face, his head coming round after him
  lockZ: s(34.3), // over Rago's arm: Zack's face, the eyes lit, already out of reach
  yank: s(34.95), // real time again: gone
  trip: s(35.0), // and his foot finds the can he just set up
  sprawl: s(35.6), // face down. Stars. Hold it.
  land: s(35.28), // Rago is going down: the lens crashes across to Zack landing on the line, his back to us, watching him
  // The settle, into the frame the player lives with
  shake: s(37.2), // static flicked off his hand
  rise: s(37.8), // Rago gets up
  walk: s(38.4), // walks to the can
  pick: s(39.2), // rights it, again
  home: s(40.7),
  one: s(41.2), // one arm up, and a chalk mark: he is counting
  fold: s(42.2), // arms folded again
};
