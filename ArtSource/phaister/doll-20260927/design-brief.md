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

## v11 (2026-09-27 night): the owner's reference, his size, the light from inside

Owner on v10: *"i hate that u js drew the pink glow in"*, *"i want it to actually look like its coming out of the holesa"*,
*"remove the finger stoo we dont have fingers for anyone"* and again *"again no fingers"*, *"thoroughly texture it"*, *"i like the
idea of this big fat voodoo doll that's kinda sllow(to balance it) but make like paete size"*, *"give him a fatter belly"*, *"make the
glow really look like it comes from within not js drawn on"*, *"manually and thoroughly do each detail instead of mass generating"*;
and a reference render, `owner-reference-20260927.png`, with *"use this as inspiration js change its colors around"*.

| Rule | What v11 does |
|---|---|
| Paete's size | 0.81 rig units with its straw (Paete 0.79); the builder refuses anything outside 0.76 to 0.84 |
| Big and fat, slow | a big head on a stout sack, a pot belly pushed forward and sagging over the belt, flanks, short thick legs; slow is a rule (plan 9.11) |
| No fingers | MITTEN STUMPS: a stuffed stump and its rounded end, whip-stitched shut. The builder refuses any part named finger or thumb |
| The light comes from inside | every glowing place is a HOLE CUT INTO the doll (v17; v11 to v16 stood lips up round them and the owner saw them pop out): a flat mouth on the cloth and a cup of walls and floor sunk below it, which `SoulGlow` draws through the mouth with a stencil cut, the walls dim at the cloth and hot toward the floor, the floor white-hot along its middle; the light falls out onto the cloth round each hole (`spill-mesh`, additive, `SoulSpill`). Stitches, the X, loose threads and the button lie over the holes and cut the light. NOTHING STANDS PROUD OF THE CLOTH except the crown's torn top, which is the sack's own open end |
| Textured, in the game's look | `cloth.png`, painted by `tools/paint_phaister_doll_cloth.py`, embedded in the glb, as PIXEL ART: each cloth a small weave tile typed pixel by pixel in three or four flat tones (a lit thread, its shaded side, a thread diving under, the hole), scaled up six times with hard edges; a lighter head cloth, a coarser capelet, her purple twill wraps, a gold rope, straw, stuffing, a bone tag, and six felt patches each with its own pixel embroidery and whip-stitched edge. The owner on the first (simulated, photographic) weave: *"make it feel like its part of teh game and not ultra realistic"* |
| Each detail by hand | 403 parts, every flap, stitch, wrap turn, pin, charm, lip and straw typed with its own numbers; the helpers only build what a row says |

What it takes from the reference, and what changes colour:

| In the reference | In v11 |
|---|---|
| burlap weave, torn edges | kept: the weave is painted; the capelet's fringe and the hem are torn flaps, each typed |
| cream bandage wraps | her ROYAL PURPLE cloth, three turns on each forearm, one on the left upper arm, two at each ankle, knots and loose ends |
| natural twine belt | a GOLD rope, sagging under the belly, knotted on its left with frayed ends |
| crimson pouch, cream bundles, a bone tag | a charcoal pouch on a gold string, two little purple bundle dolls, the bone tag with a crimson rune |
| dark brown diamond patch, crimson belly patch | her CHARCOAL coat cloth with lilac crosshatch; her hair's MAGENTA with a charcoal rune |
| purple patch on the back of the head | CRIMSON with a gold rune; charcoal with a gold rune on the back; crimson with a gold X on the thigh; charcoal with a lilac X on the calf |
| gem-headed pins | seven pins with cut gems in her lilac, gold, magenta and crimson |
| light pouring out of the crown, the seams, the grin, the eyes | the same places, built as openings: the crown's torn top with straw and tongues of light, the front seam, the back seam, both legs' outer seams, the grin, the X eye's socket, and the socket behind the button |
| fingers on the fists | NONE (the owner, twice) |

v11 to v15, what each render changed (renders `Logs/phaister-doll-v11` to `v15`):

| v | Change | Why |
|---|---|---|
| 11 | first build of the above | |
| 12 | pins stand out of the cloth at their typed angle; the light graded (a thin white-hot line at the bottom of each gap, darker toward the lips); the spill one continuous strip per side; rounder stuffed forms; a fatter belly; a head tapering to its tie; charms hung from wherever the rope lands; a finer weave | v11's pin shafts lay flat and floated; its eyes and grin read as flat bright fills; the spill showed as hard pink panels at every bend |
| 13 | the belly the widest and furthest-forward part, the chest narrower, the shoulders out; the chest diamond up on a taller capelet; the crown's torn flaps thinner and taller, ten tongues of light and a soft halo over it; no spill up the outside of the lips | the belly still read as a box; the crown read as a flat pink pool; the lips' outer spill read as panes of glass |
| 14 | the front tear split into one down the capelet and one down the belly; the crown's inner walls capped low; its tie a real rope ring | one tear stepped out through the air between the capelet and the belly |
| 15 | the cloth repainted as pixel art | the owner: *"not ultra realistic"* |
| 16 | the pixel weave's dive rows softened | v15's dark grid read as plaid |
| 17 | ⚠️⚠️ EVERY OPENING CUT INTO THE BODY: no lips stand up anywhere; each opening is a flat mouth on the cloth and a lit cup sunk into the doll below it, drawn through the mouth by `SoulGlow` (a stencil cut), so the light sits down inside; the button sits on the face; the X lies on its hole; the sack slimmer and shallower, the belly gentler, the shoulders in with it; the cloth offered three ways (`texture-options.png`: felt, painted, chunky) | the owner on v16: *"this shit is too fat it looks weird af from the side and the texture is so bad"*, *"why does it pop out its the opposite it should look like its from withhin"*, *"same with the mouth why does the mouth smirk pop out"*, *"why do u pop out his features"* |
| 18 | twelve worn-through holes clustered on the belly, back, head, shoulder and knee | the owner: *"give it real holes bruh not js texture"* |
| 19 | the cavities wider inside than their mouths and deeper than wide, dark at the cloth | v18's shallow funnels read as pink sweets on the cloth |
| 20 | the worn-through holes REMOVED; the light only where it means something (grin, eyes, seams, crown) | the owner on them at a distance: *"what is this supposed to eb tf"* |
| 21 | the chunky weave, its contrast set for the match street, the cloth moved from orange to a duller sack tan | the owner on the first street film (felt): *"why dont i see his texture at all here"*; the street's grade turned the tan orange |
