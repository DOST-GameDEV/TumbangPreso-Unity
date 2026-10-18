# Trails, approved state (film t32)

The owner approved the trails bench effect on 2026-10-11. His words, of the film `fxbench trails t32`
and its fifth-trail board: "trails looks good".

Everything here is a copy of what produced that film. Nothing in this folder is read by the game or
by the bench. It exists so that a later pass cannot lose the approved state.

## What is here

| File | What it is |
|---|---|
| `trail_shards.png`, `trail_smoke.png`, `trail_ink.png`, `trail_squiggle.png`, `trail_bands.png` | The five trail masks, as installed in `Assets/TumbangPreso/Resources/Textures/UltimateFx/` for t32. |
| `build_ultimate_fx_textures.py` | The generator as it was. Its five `trail_*` functions rebuild those masks byte for byte. |
| `FxStudyTrails.cs.txt` | The trails' code: solved camera, the five plans, cuts and colour stops, and the trails' own bloom (`BloomIntensity => 0.16f`). |
| `FxStudyRibbon.shader.txt` | The ribbon shader the trails are drawn with. Shared with every other bench effect. |
| `FxStudyBloom.shader.txt`, `PaeteAbilityFilm.FxBench.cs.txt` | The bench's bloom and its constants (threshold 1.0, knee 0.30, white 1.6, five levels). Shared. |
| `FxStudyKit.cs.txt` | The kit as it was (the `Sweep` mesh and `FxPiece` the trails use). |
| `frames/f030.png` ... `f210.png` | Five frames of the approved film, for `fxsame.py`. |
| `fxbench_trails_t32.mp4`, `look_trail5_t32.jpg`, `measure_trails_t32.txt` | The film, the fifth trail's board and ribbon beside the reference, and the measurements against the reference. |

Code copies carry a `.txt` ending so that no compiler picks them up.

## The rule that goes with it

The trails share `FxStudyRibbon.shader` and the bench's bloom with every other effect. After any
change to either, film the trails again and run, from the worktree root:

    py -3 tools/cutscene_rework/fxsame.py trails <new tag>

It lays the new film against the kept frames and says SAME or CHANGED with the numbers. Say the
number in the report. A change that moves the trails is put right before anything else goes on.

To check the masks, rebuild them (`py -3 tools/build_ultimate_fx_textures.py`) and `cmp` each
`trail_*.png` against the copy here.

## Trail 5, in the owner's words

"the 5th is one ribbon, fold applied, copied as is, then shifted down". The copy sits 0.115 of the
sheet lower, about half the strip's own width.
