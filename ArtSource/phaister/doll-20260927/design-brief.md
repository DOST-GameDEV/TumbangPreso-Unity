# Phaister's voodoo doll: design brief (2026-09-27)

Owner: *"create a new model for the voodoo i guess"*, for the VOODOO DOLL ultimate in his table: *"The voodoo doll becomes a
sentient being that assists you in attacking or defending for the rest of the round."* The kit's plan is
`docs/reports/phaister-kit-2026-09-27/plan.md` section 9; the doll is section 9.10.

## Who it is

Her rag manika, the one she carries at her hip and works her curses on, grown to a player's size and awake. It must read as
HERS at a glance (her magenta, her hat, her purple) and as a DOLL (cloth, stitches, pins), never as a second witch.

## What it is made of

| Part | What | Why |
|---|---|---|
| Body | a stuffed linen sack, a round belly, a gathered hem, stubby mitten arms and rounded feet | a doll's body, not a person's; stuffed, so it can slump and puff when tagged |
| Face | a purple button sewn on for her right eye (rim, two holes, the thread), an ink X for her left, a stitched grin higher on her left | the three marks every voodoo doll is known by; the grin is her smirk. The purple button is the one colour in the face: it is a button, and it is the eye that lights when the doll wakes |
| Hair | magenta yarn, a short uneven fringe, five strands down each side, six down the back, every strand its own width, length and lean, with gaps | her hair colour, as yarn |
| Hat | a small copy of hers, worn askew toward its left, the tip drooping | hers, and a doll's |
| Pins | out of its right temple, twice from the back of its head, one through its right arm | a voodoo doll is full of pins |
| Twine | tied at the neck (knot on its left, two loose ends), both wrists, the right ankle (knot outside, two loose ends) | how a manika is bound |
| Patches | a purple heart patch crossed by the last stitch (the cutscene sews it), a crimson knee patch, a crimson patch low on the back, a purple patch on the right forearm | repairs, placed unevenly |
| Stuffing | a tuft out of a torn hem at the front of the right hip | a thing that has been used |

Palette: linen `b59c74` (head `c6ae86`, shadow `8c7352`), twine `e2d2a8`, stuffing `f3e9c9`, her magenta yarn `d8186e` and
`a4105a`, her purple `4a1e78`, her crimson `8c1424`, her hat black `181622`, lilac pin heads `9838d8`, her gold `f8b824`
(buckle) and dulled gold shafts `b87814`, ink `14101c`. Written by `tools/build_phaister_doll_voxel.py` to `palette.json`.

## Versions (renders in `Logs/phaister-doll-vN/`, `Editor/PhaisterDollReview`)

| v | Change | Why |
|---|---|---|
| 1 | first build | |
| 2 | twenty-one identical straight strands replaced by 23 typed strands with a fringe and gaps; a domed yarn cap under the hat; round buttons (a disc primitive); the stuffing moved; a paler linen | v1's hair was a curtain from every side and a barcode from behind, with a bare forehead under a flat lid; the button read as a spiky square; the stuffing hid under the head; the tan sat beside Paete's bark as a third brown |
| 3 | uneven fringe spacing; the stuffing to the front of the right hip | the fringe read as a comb; the arm hid the tuft |

Still to do on the same design: the small doll at her hip (in her glb, by `add_phaister_details.py`-style surgery) and the
teleport's decoy (a doll of HER: her hat, her hair in yarn, her coat's colours).
