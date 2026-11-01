import { s } from './time';

/**
 * ⚠️ THE ONE TIMETABLE for "Palabas", Phaister's film at Ermita, under the LRT at night.
 * docs/reports/home-scene/phaister-ermita.md is this list in prose.
 *
 * She is taya, and she turns her back on the can on purpose: LORE.md's "invite a chase, leave a false
 * opening, close the circle". Nemu takes the shot. Phaister never looks. Her doll gets up and rights the
 * can, her hand finds Nemu behind her, the train goes over, and when its light has passed she is standing
 * in front of Nemu. Nemu does not jump. Then, once, she nods.
 */
export const B = {
  under: s(0), // under the deck, in the dark
  lamp: s(1.6), // one sodium lamp stutters on over the far court
  doll: s(4.2), // the doll, slumped against the can. Its head comes up.
  brim: s(7.2), // the brim of a hat
  lift: s(8.2), // and she looks up at us
  whole: s(9.6), // Phaister, whole, her back to the can and to Nemu
  bow: s(11), // she bows to an empty street: the false opening
  nemu: s(13), // Nemu on the line, Kuro at her shoulder. Not impressed. Not anything.
  raise: s(14.4),
  throw: s(16),
  klang: s(16.5), // the can
  hold: s(17.2), // Phaister has not moved. One finger: wait.
  fetch: s(19.4), // Nemu walks for her slipper
  wake: s(22), // the doll's light comes up from inside it
  stand: s(22.8),
  push: s(24.6), // it rights the can
  reach: s(25.8), // her hand comes out behind her, still not looking
  marked: s(27.6), // the mark closes on Nemu; she slows
  train: s(28.8), // the train comes in over the deck
  blink: s(30.6), // in its light, she is gone
  scoop: s(30.9), // Nemu picks up the slipper
  rise: s(31.9), // and straightens up
  tada: s(32.5), // and Phaister is standing right there. Arms open.
  nothing: s(33.2), // Nemu looks at her
  sag: s(34.6), // nothing. The shoulders go.
  nod: s(35.5), // one nod
  joy: s(36.1), // it is the best thing that has ever happened to her
  home: s(38), // a blink home, because she can
  land: s(38.4), // the frame the player lives with
  fold: s(42), // everyone settles
};
