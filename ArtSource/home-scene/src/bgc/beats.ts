import { s } from './time';

/**
 * ⚠️ THE ONE TIMETABLE for "Green Light", Zack's film at BGC. docs/reports/home-scene/zack-bgc.md is
 * this list in prose. Every other file reads these names.
 *
 * Rago is taya and stands square in front of the can. Zack waits for the traffic light, curves the
 * slipper in one huge electric arc round Rago, and it takes the can from behind him. Rago's whole body
 * goes when he sees it. Then the retrieval: Momentum out, Rago resets the can, creeps up and lunges for
 * the tag, Backtrack home, and his foot catches the can he just set up. He gets up, rights it again and
 * chalks one up. He keeps the score. Both of them stay there.
 */
export const B = {
  // Small to whole: the light, the foot, the wall. Zack's whole figure is saved for `whole`.
  signal: s(0),
  spark: s(1.2), // current runs along the wire
  feet: s(4.2), // a sneaker tapping on the chalk
  rago: s(7.4), // the wall
  chin: s(9.2), // he lifts his chin a little: go on then
  whole: s(11.2), // Zack at last, and he is not looking at the can
  lookUp: s(12.4), // he looks up at the signal
  finger: s(13.6), // one finger to Rago: wait
  amber: s(15), // cut back to the signal: red, amber, and the green held long enough to name
  green: s(16.2),
  eyes: s(17.6), // his face: the eyes light
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
  reset: s(27.6), // Rago rights the can
  scoop: s(29.0),
  wall: s(30.0), // Zack strolls to the far side of the can and waits for him
  load: s(30.8), // Rago creeps up, hands out
  commit: s(32.4), // the lunge for the tag
  snapBack: s(33.0), // his hand is on him: Backtrack
  trip: s(33.13), // and his foot finds the can he just set up
  sprawl: s(33.7), // face down. Stars. Hold it.
  land: s(34.9), // whip home: Zack on the line, looking back at him
  // The settle, into the frame the player lives with
  shake: s(35.8), // static flicked off his hand
  rise: s(36.6), // Rago gets up
  walk: s(37.8), // walks to the can
  pick: s(39.2), // rights it, again
  home: s(40.2),
  one: s(40.8), // one arm up, and a chalk mark: he is counting
  fold: s(42.2), // arms folded again
};
