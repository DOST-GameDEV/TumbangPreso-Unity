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

**The one sentence for her whole kit (v2, owner: *"make her a frigging witchh not a showman"*, *"MAGIC AND VOODOO"*,
*"think of fil cutlure integration"*): a mischievous Visayan witch from Capul whose magic is moths, moonlight and a rag
doll full of pins.** She is playful and pleased with herself, never menacing. v1 (a stage magician) is rejected; see
`plan.md` section 8.

## 2. How the personality shows up, channel by channel

| Channel | Rule |
|---|---|
| Body | Unhurried and sure. She casts with small, precise hands (a prick of a pin, a flick of the wrist, a twist of a doll's head) and saves the big body moment for OMEN, where the power visibly surges through her. She never strains. A smirk and a glance at her victim are her tells, not a bow. |
| Face | Ink only, her existing face. Her lids stay half-closed; in OMEN her eyes light violet. |
| Effects | Witch and voodoo, Visayan folklore: moths and beetles (the barang swarm), black butterflies (the omen), moonlight, her lunar sigils, the rag manika, long hat pins, ash and embers. Solid toon geometry in her palette; only sigils, moonlight, her eyes and the eye's rim glow. |
| Sound | Wings as grains, a needle through cloth, a pin's bright tick, a glassy moon chord, candle crackle, a detuned music box for the doll, ash crumbling; her lines recorded by the team. |
| Directing | The camera treats her as the source of the power: low angles as it surges, close on her hands as she shapes it, and the payoff seen from above as it swallows her victims. |

## 3. Her palette (from the model; no blue anywhere in the UI, CLAUDE.md 6.4)

Black robe, violet trim, magenta hair, gold hardware, pale skin. Effects use violet and magenta for magic, gold for pin
shafts and embers, bone only for the pins' heads and moonlight's pale core. OMEN's eye is her robe's black with her hair's
magenta rim; its butterflies are black with violet undersides.

## 4. The model

A light refinement (owner: *"u can refine a bit i dont mind"*), `plan.md` section 5: hat pins in the band, a rag manika at
her hip, a crescent moon buckle, two moths on the brim. The PROPS of her skills (the manika, the hat pin, the moth, the
beetle, the butterfly, the black eye) are modelled like characters in `tools/build_phaister_props.py`, every part by hand,
the way `tools/build_paete_props.py` does it. The first-pass doll and ring in `tools/build_rework_props.py` are replaced.
