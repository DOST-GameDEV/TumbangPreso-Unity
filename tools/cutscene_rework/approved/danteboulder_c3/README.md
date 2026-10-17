# BOULDER (Basilio, dante_skill2): the look approved 2026-10-12

The owner, having seen the bench frames, the first-person sheet and the court film of this state:

> "looks good. just make sure its optimized."

So the LOOK is frozen as filmed here: court film c3, bench film u4. Do not change how it looks on our own
critique. The faults we listed ourselves at the time (the dust is small, the molten light is modest, a puff
peaks late in its life) were in front of him when he approved it.

What "optimized" may change: how it is drawn (fewer draw calls and triangles), never the picture. Prove a
cheaper version against the frames kept here.

## What is here

| File | What it is |
|---|---|
| `FxStudyDanteBoulder.cs.txt` | the effect as filmed |
| `FxStudyRock.shader.txt` | the stone's shader as filmed (edge wear read from the bake, no ink) |
| `DanteBoulderFilm.cs.txt` | the court film that made c3 |
| `dante_boulder.glb`, `dante_boulder_mask.png` (+ `.meta`) | the four rocks and three rubble pieces, and their baked mask |
| `dante_boulder_bench.glb` | the bench's stand-in shoe and body |
| `dante_split.png` | the ground split's mask |
| `build_dante_rocks.py.txt` | carves and bakes the rocks (Blender in the background; calls the Lagoon rock tools) |
| `build_dante_fx_textures.py.txt` | paints the split's mask |
| `fxbench_dantecourt_c3.mp4`, `_small.mp4` | the court film he saw: OLD inlay, NEW, NEW with bench bloom; third person, his eyes, the target's eyes |
| `fxbench_danteboulder_u4.mp4`, `fxbench_danteboulder_nobloomu4.mp4` | the bench films |
| `dantecourt_c3_sheet.jpg`, `dantecourt_c3_first_person.jpg`, `danteboulder_u4_close.jpg` | the sheets he saw |
| `frames_court/`, `frames_bench/` | ten frames of each film (0.45, 0.72, 0.80, 1.30, 1.80, 2.75, 2.93, 3.05, 3.30, 4.00 s) to difference a later state against |
| `done_fxbench_dantecourt_c3.txt` | the film's own report, with the cost measured then |

The dust is the bench's cloud (`FxStudyCloud.cs`, `FxStudyCloud.shader`), which is another effect's and is
locked in its own right: it was called, not copied, and is not kept here.

## Cost when it was approved (measured by the court film, editor, outside Play)

0.10 s after the hit: 15 renderers, 20 draw calls, 31,296 triangles drawn. Posing 0.0228 ms a frame, nothing
allocated. Carried: 4 draws, about 2,900 triangles.

## It was staged, not hooked

When this was approved nothing in a match drew it: the films stand the bench effect on a court and put the
real tsinelas where the effect says the shoe is.

## Kept over ASTRAReworks' Boulder (2026-10-17)

He watched this Boulder side by side with the one on branch ASTRAReworks, commit `a7c1189` (a crust of 13 basalt
chunks over the slipper, grit rising into his palm, grit in flight, a burst on a hit), in
`Logs/paete-ability-film/danteboulder_versus_v1.mp4` (sheet: `danteboulder_versus_v1_sheet.jpg`; report:
`done_fxbench_danteversus_v1.txt`), and said: "we're still keeping our design".

⚠️ WHEN THE BRANCHES MERGE, ASTRA'S REWRITTEN `DanteBoulderCoating` MUST STAND DOWN, ON PURPOSE. Ours stands down
behind `DanteBoulderStone.Rocks = true` through one guard at the top of its `Sample`
(`bool loaded = !DanteBoulderStone.Rocks && ...`). ASTRA's rewrite does NOT have that guard: taken as it is, it
would draw its chunks on the same shoe as our rocks. Keep the guard in whatever `DanteBoulderCoating` the merge
produces (and keep `Slipper.cs` calling `DanteBoulderStone.TrackWorld`, which tracks the coating as well), or the
two Boulders draw at once.

The film's copies of ASTRA's code were taken out of the project after he picked; they are kept outside it, not in
this folder.

## The edge wear's noise toned down a little (2026-10-17)

He picked "use the toned down a little one" off the noise board. The game now draws the wear with `_Noise`
(0.55, 0.50, 0.60, 0); nothing else in this approved state changed. Its backup, films, frames and proof are in
`noise_2026-10-17/`.
