# Quick Circuit (Isagani, zack_skill1), approved bench look (film z4)

The owner approved this look on 2026-10-11. His words, of the film `fxbench zackcircuit z4` and its
two comparison sheets: "zack looks good to go".

Everything here is a copy of what produced that film. Nothing in this folder is read by the game or
by the bench. It exists so that a later pass cannot lose the approved state.

## What is here

| File | What it is |
|---|---|
| `FxStudyZackCircuit.cs.txt` | The effect's class exactly as filmed. `.txt` so no compiler picks it up. |
| `zack_bolt.png`, `zack_crackle.png`, `zack_ground_star.png`, `zack_spark_sheet.png` | Its four masks, as installed in `Assets/TumbangPreso/Resources/Textures/UltimateFx/`. |
| `build_zack_fx_textures.py` | Their generator as it was. It rebuilds the four byte for byte. |
| `frames/`, `frames_nobloom/` | Six frames of each film (0.43, 0.53, 0.62, 0.73, 0.88, 0.98 s), to lay a later film against. |
| `fxbench_zackcircuit_z4.mp4`, `fxbench_zackcircuit_nobloomz4.mp4` | The film he saw, and the same without bloom. |
| `pair_zackcircuit_z2_z4_a.jpg`, `_b.jpg`, `pair_zackcircuit_z4_bloom_nobloom.jpg` | The sheets he saw. |

It also draws through shared bench code that is kept elsewhere: `FxStudyKit.cs`, `FxStudyTrails.cs`
(`Dress`, the fifth trail) and `FxStudyRibbon.shader` (see `approved/trails_t32/`).

## It was approved WITH these faults. Do not fix them unasked.

Listed to the owner before he approved: the streak reads a little like flame, the five landing
sparks look stamped, two launch sparks stray high, and the start strike's heart is a dark star for a
few frames while it is eaten. Approval froze them as they are.

## The rule that goes with it

After any change to the class or to anything it shares, film the bench again and difference it
against the kept frames:

    fxbench zackcircuit <tag>      (and a tag beginning `nobloom`)

The six frames must match. A game-side input added to the class must leave the bench film as it was.

## The numbers the picture tells the truth about

`ZackHeroKit.QuickCircuitAbility`: tell 0.15 s, cut 2.0 m, launch 10.95 m/s, friction 30, so the slide
is 0.365 s. The streak ends at his body and is gone 0.16 s after he stops. Nothing stays on the road.
