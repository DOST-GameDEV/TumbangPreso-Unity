# Phaister's overhaul: brief, 2026-09-27

Owner, 2026-09-27: *"okay start working on the phaister overhaul next i think theres like a doc there or smth thhats
details how the quality of paete's shit was produced so that u can reproduce it"*, *"rename her shit too hahah it sucks
ass"*, *"it needs really great presentation VFx ANIIMATION SFX AND DIRECTING"*, *"think abt her personality too in
making her cutscenes and vfx"*, *"thoroughly refine existing animation effects and models and vfx of her skills"*.

The method is `docs/HERO_KIT_METHOD.md`. The mechanics were settled on 2026-09-26 (ABILITY-2,
`docs/reports/ability-rework-2026-09-26/plan.md` section 3.5, numbers in `Core.VoodooRules`) and are NOT reopened here
except where a number was "set here" and never reviewed. This overhaul is her names, her presentation and her ultimate's
direction. Research: `docs/reports/phaister-kit-2026-09-27/research.md`. Plan and questions: `plan.md` beside it.

## 1. Who she is (read from the repo, not invented)

| Source | What it says |
|---|---|
| `docs/CHARACTER_ORIGINS.md` | Capul, Northern Samar. Plays under the rail line at Ilalim ng Tulay. Grew up on stories of a moon that disappears into a serpent's mouth. Treats a match like a carefully timed reveal: invite a chase, leave a false opening, close the circle. Enjoys the setup more than winning. Wants Nemu to at least LOOK surprised. |
| Character-select line | *"Sets the stage. Lets you discover the trick."* |
| `LORE.md` personality row | Enjoys the audience almost as much as the contest. Deliberate flourishes, readable sigils and an open finish. Stagecraft and sporting mischief, **not cruelty or a sinister conquest**. |
| `GaitStyles.Phaister` | Struts, one foot across the centre line, chin up, a flourish in the arm swing; the run is an exit with a cape that is not there. |
| Her model (`tools/build_phaister_voxel.py`, 23 iterations) | Tall stepped witch hat with a violet band, gold buckle and a feather; magenta hair; sleepy-lidded confident eyes, a small smirk; black robe with violet trim, gold collar chain and a violet jewel; gold buckle belt. She and Dante are the look the rest of the cast is being restyled toward (TODO, Nemu and Rafi restyles are rendered "beside Dante and Phaister"). |

**The one sentence for her whole kit: she is a stage magician whose tricks happen to be curses, and every ability is a
little show with a setup, a reveal and a bow.** Voodoo is her props (pins, a stitched doll, thread), not her mood: she is
playful and pleased with herself, never menacing.

## 2. How the personality shows up, channel by channel

| Channel | Rule |
|---|---|
| Body | Every cast has a FLOURISH before the release and a PRESENTING gesture after it (open palm, a hat tip, a curtsey, a conductor's point). Each ability gets a different one: the owner rejects repeated gags. She never flinches, never strains; the effort is hidden, like a magician's. |
| Face | Ink only, her existing face. Her lids stay half-closed (bored-confident) and OPEN only at the reveal of the ultimate. That one change of face is the cutscene's punctuation. |
| Effects | Stage and moon: a spotlight, a curtain, a moon disc, gold sparkle on the reveal; voodoo props: pins, stitched thread, doll cloth. Solid toon geometry in her palette; only her sigils, the moon and the spotlight glow. |
| Sound | A performer's kit: a snare roll into a cymbal-less "ta-da" sting, a slide whistle, cloth swish, a pin's bright tick, thread pulled taut (a plucked, tightening pitch), her laugh. Every cue has a transient, a body and a tail (method section 5). |
| Directing | She plays to a camera. The cutscene is a performance for the audience: she looks INTO the lens at the setup and at the bow. |

## 3. Her palette (from the model; no blue anywhere in the UI, CLAUDE.md 6.4)

Black robe, violet trim, magenta hair, gold hardware, pale skin. Effects use violet and magenta for magic, gold for the
reveal (sparkle, the spotlight's warm edge), bone white only for the moon and the pins' heads. The black hole's core is
her robe's black, its rim her hair's magenta.

## 4. The model

Her body model is the cast's reference and is NOT reworked unless the owner says so (plan question 2). The PROPS of her
skills are: the rag doll, the pin, the spotlight rig, the curtain, the moon and the serpent are modelled like characters
in a typed prop builder (`tools/build_phaister_props.py`), every part by hand, the way `tools/build_paete_props.py` does it.
The current doll and ring in `tools/build_rework_props.py` are first-pass and are replaced.
