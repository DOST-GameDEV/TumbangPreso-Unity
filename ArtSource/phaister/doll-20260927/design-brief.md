# Phaister's voodoo doll: design brief (2026-09-27)

Owner: *"create a new model for the voodoo i guess"*, for the VOODOO DOLL ultimate in his table: *"The voodoo doll becomes a
sentient being that assists you in attacking or defending for the rest of the round."* The kit's plan is
`docs/reports/phaister-kit-2026-09-27/plan.md` section 9; the doll is section 9.10.

## Who it is

Her rag manika, the one she works her curses on, grown to a player's size and awake. ⚠️⚠️ IT IS ITS OWN VOODOO DOLL, NOT A
SMALL PHAISTER. Owner on v3, which wore her magenta hair as yarn and a copy of her hat: *"this shit suckls why does it have her
hair hahahahaa"*, *"and her hat"*, *"make it look like its own vooodoo wtf"*. It reads as a voodoo doll by its own marks (the
tied sack top, the stitches, the pins, the binding); her colours live only in its pin heads and patches.

## What it is made of

| Part | What | Why |
|---|---|---|
| Body | a stuffed linen sack, a round belly, a gathered hem, stubby mitten arms and rounded feet | a doll's body, not a person's; stuffed, so it can slump and puff when tagged |
| Face | a purple button sewn on for its right eye (rim, two holes, the thread), an ink X for its left, a stitched grin higher on its left | the three marks every voodoo doll is known by. The purple button is the one colour in the face: it is a button, and it is the eye that lights when the doll wakes |
| Top | the sack gathered in two steps and tied off with twine (knot on its left, two ends), the burlap fraying up into a tuft of seven threads and stuffing, each splayed its own way | a tied-off sack is a doll's head; it is the silhouette her hat used to be |
| Pins | eight, heads in lilac, crimson, magenta and gold: its right temple and brow, the crown, twice from the back of its head, through the heart patch, its right arm, its left thigh | a voodoo doll bristles with pins |
| Twine | tied at the neck, bound round the waist (knot on its left, two ends), both wrists, the right ankle (knot outside, two ends) | how a manika is bound |
| Patches | a purple heart patch crossed by the last stitch (the cutscene sews it), a crimson knee patch, a crimson patch low on the back, a purple patch on the right forearm | repairs, placed unevenly |
| Stuffing | a tuft out of a torn hem at the back of the right hip | a thing that has been used |
| The light | a third skinned mesh, `glow-mesh`, painted unlit by `Resources/Shaders/SoulGlow` (`PhaisterDollArt.ApplyGlow`): the splits, the grin, the eye under the X, the ring round the button, the top of the tie; an edge colour and a hot core, breathing up the body | it is alive inside; the splits are where the soul shows, and the stitches are what holds it in |

Palette: linen `b59c74` (head `c6ae86`, shadow `8c7352`), twine `e2d2a8`, stuffing `f3e9c9`, her purple `4a1e78` and
crimson `8c1424` (patches), pin heads in her lilac `9838d8`, magenta `d8186e`, crimson and gold `f8b824`, dulled gold shafts
`b87814`, ink `14101c`. Written by `tools/build_phaister_doll_voxel.py` to `palette.json`.

## Versions (renders in `Logs/phaister-doll-vN/`, `Editor/PhaisterDollReview`)

| v | Change | Why |
|---|---|---|
| 1 | first build | |
| 2 | twenty-one identical straight strands replaced by 23 typed strands with a fringe and gaps; a domed yarn cap under the hat; round buttons (a disc primitive); the stuffing moved; a paler linen | v1's hair was a curtain from every side and a barcode from behind, with a bare forehead under a flat lid; the button read as a spiky square; the stuffing hid under the head; the tan sat beside Paete's bark as a third brown |
| 3 | uneven fringe spacing; the stuffing to the front of the right hip | the fringe read as a comb; the arm hid the tuft |
| 4 | her hair and hat removed; the crown gathered and tied with a frayed tuft; eight pins; a waist binding | the owner: it must be its own voodoo doll, not her hair and her hat |
| 5 | the crown tapers in two steps into the tie | v4's top read as a flat lid with a stub on it |
| 6 | GLOWING INSIDE, STITCHED SHUT: split seams down the chest, spine, shoulders and right thigh with the soul light showing, a glowing grin, a glowing eye under the X, light round the button; crosshatched patches and a woven back of the head; a gris-gris pouch with two black feathers; twine wound round the forearms and shin; thumbs | the owner: *"refine this shit more"*, *"make it look way more detailed"*, *"add crosshatch or smth"*, and with a reference of a stitched bear with glowing seams, *"make it look liek its glowing inside and it stitched tgthr also u dont have to copy other models in making it let it be its own"* |
| 8 | REMADE FROM NOTHING: its own skeleton (the seven bone names kept so the gait, animator and bot can drive it), a hulking hunched stuffed poppet about 1.10 tall (about 2.6 m in game, far over a player): a small lumpy head sunk low between huge shoulders, a wide glowing grin across a jaw pushed forward, a barrel of patchwork cloth (a dark panel, a grey scrap, purple and crimson patches) with a hump, long arms that hang to the ground ending in three stubby fingers and a thumb, stumpy legs; split down the front, the spine, the back, the shoulders, the right leg and the head's back seam, held by six big X stitches and dark staples; six long pins; the crown tied with a loop for its string | the owner on v7: *"dude this shit sucks i wanted a complete overhaul i didnt want it to look like a fucking character"*, *"I gave u permission to compeltley remake it"*. v1 to v7 were the cast's body under cloth |
| 7 | a hot core line down every split (`SoulGlow` `_Core`); the chest held by bold black cross-stitches; the chest split longer and wider; light spilling out of the tied top | v6's light was one flat pink; its linen tape vanished into the linen |

Still to do on the same design: the small doll at her hip (in her glb, by `add_phaister_details.py`-style surgery). The
teleport's decoy is the one place a doll wears her likeness (it is the joke: you grabbed a doll of her); ask before building it.
