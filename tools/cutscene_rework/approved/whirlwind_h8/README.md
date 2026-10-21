# The whirlwind, as approved (film h8)

2026-10-11. Of the bench film `fxbench whirlwind h8` and its frames 60 and 150 the owner said:

> "that looks amazing as is"

"As is" is the whole bench whirlwind exactly as filmed: nothing of it is to be changed, not the balance of the mix,
not its brightness, not its top. How it got here, in his words along the way:

- the tapes, the ground and the motes were approved at film h1: "rest of the sfx looks good";
- the horn was to be a noise: "the middle horn is supposed to be a skewed large transparent celled perlin noise", then,
  shown simplex noise cut at nothing, "check the 3rd image. but more white" and "feather"; then "a bit too sparse",
  "increase the empty cells near the top so its more wispy at the rim", "use the finer cells", "too blob-y. and you
  should still fade out the top", "some parts should reach bright whites", "you arent fading out the rim" (film h7);
- and last: "try doing a mix of the original and this latest h7 one". The mix is h8: the noise horn as the body, with
  the original horn's vein net and three of its shells inside and over it (`FxStudyWhirlwind.Horn.Mix`, at 0.55).

What is here:

- `FxStudyWhirlwind.cs.txt`: the class exactly as it was for film h8. `FxStudyKit.cs.txt`, `FxStudyWisp.cs.txt`,
  `FxStudyTrails.cs.txt` and `FxStudyRibbon.shader.txt`: the bench code it calls, as it was that day.
- `noise_simplex_cells.png` and `build_ultimate_fx_textures.py`: the one picture that is the whirlwind's own, and the
  generator that paints it (`noise_simplex_cells`). Everything else it wears is the trails' and the wisp's.
- `frames/`: seven frames of h8 (f030 to f210). `fxbench_whirlwind_h8.mp4`: the film.
- `whirlwind_h1_h7_h8.jpg`: his picture, the original horn, the noise horn and the mix, at one size.

THE GUARD. After any change to code or pictures the whirlwind shares (the bench kit, the ribbon shader, the wisp, the
trails, the generator), film it again and lay it against these frames:

    bash tools/cutscene_rework/fxbench.sh whirlwind <tag> <draft dir>
    py -3 tools/cutscene_rework/fxsame.py whirlwind <tag>

It must read SAME, as the trails must against t32. The horns it was mixed from are still filmable by name
(`whirlwindh1`, the original; `whirlwindh7`, the noise horn alone) and each matched its own film on the day
(worst frame mean 0.000 of 255, largest single pixel difference 1).
