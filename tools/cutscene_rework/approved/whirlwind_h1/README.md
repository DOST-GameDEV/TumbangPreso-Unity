# The whirlwind's tapes, ground and motes, as approved (film h1)

2026-10-11. Of the first bench film of the whirlwind study (`fxbench whirlwind h1`) the owner said:

> "rest of the sfx looks good. the middle horn is supposed to be a skewed large transparent celled perlin noise"

So everything in that film except the horn is approved and kept as it was: the five tapes (the approved trail
ribbons on tilted rings), the ground disc (the wisp's dish streaks) with trail 2 round its rim, the sixteen motes,
and their colours, widths, counts, brightness and the effect's bloom (0.30). The horn of h1 (six wisp shells with
trail 1's veins over them) was not approved and has been replaced.

What is here:

- `FxStudyWhirlwind.cs.txt`: the class exactly as it was for film h1. The parts that are approved are marked KEEP in
  the live file (`Assets/TumbangPreso/Runtime/Visual/FxStudy/FxStudyWhirlwind.cs`).
- `frames/`: seven frames of h1 (f030 to f210).
- `fxbench_whirlwind_h1.mp4`: the film.
- `whirlwind_beside_picture_h1.jpg`: two of its frames beside the owner's picture of the whirlwind he means.

To check the approved parts have not moved: film `fxbench whirlwindparts <tag>` (the whirlwind built without its
horn) and lay it against these frames with `tools/cutscene_rework/fxparts.py <tag>`. Outside the box the horn stands
in, the two should differ only by what h1's bright horn spilled through the bloom (measured on the day: mean 0.02 to
0.08 of 255, no pixel more than 8 levels out).

## The horn of this film is "the original", and it is still there

Of the noise horn's later passes the owner said: "if this pass doesnt look good, lets just keep the original". The
horn of h1 (six wisp shells with trail 1's veins over them) is kept whole in the live class behind one switch,
`FxStudyWhirlwind.Horn.Shells`, and films by its own bench name: `fxbench whirlwindh1 <tag>`. Filmed again after the
switch went in, every one of its 241 frames matched this film (worst frame mean 0.000 of 255, largest single pixel
difference 1). Going back to it is changing `Horn.Noise` to `Horn.Shells` where a whirlwind is built.
