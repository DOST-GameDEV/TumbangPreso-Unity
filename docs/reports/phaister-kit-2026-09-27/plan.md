# Phaister's overhaul: the plan, 2026-09-27 (v2, the witch)

Owner, 2026-09-27: *"rename her shit too hahah it sucks ass"*, *"it needs really great presentation VFx ANIIMATION SFX AND
DIRECTING"*, *"think abt her personality too in making her cutscenes and vfx"*, *"thoroughly refine existing animation
effects and models and vfx of her skills"*. Then, on v1: *"i want u to make her a frigging witchh not a showman"*, *"she's
supposed to be using MAGIC AND VOODOO BTW"*, *"think of fil cutlure integration with her shit"*. Method:
`docs/HERO_KIT_METHOD.md`. Brief: `ArtSource/phaister/kit-20260927/design-brief.md`. Research: [research.md](research.md).

**v3, 2026-09-27 evening: the owner's new table REPLACES sections 2 to 4.5; the live plan is section 9.** (What follows up to
section 8 is v2, kept for its reasons.)

**The one sentence for her kit (v2): a mischievous Visayan witch from Capul whose magic is moths, moonlight and a rag doll
full of pins.** Playful and pleased with herself, never cruel (LORE.md). v1's showman (spotlights, curtains, bows) is
REPLACED; section 8 records what was rejected and why.

## 0. What is there today (audited from the code, 2026-09-27)

| Ability | Today | Verdict |
|---|---|---|
| SHADOW BLINK | a torn shadow sheet at the start (`HeroHazards.SpawnShadowRift`), falling glyphs at the end (`PhaisterArrivalSeal`), the old `hero-phaister-blink` clip (a kick), `sfx_blink_arrive` | two unrelated effects; nothing travels between them |
| CURSE: DISORIENTED | a first-pass doll (`build_rework_props.py`, block fallback) spinning on an arc; lands with `sfx_blink_arrive`, the BLINK's sound; cast clip `hero-phaister-hex` from the retired Hex | shares a clip and a sound with other abilities; the method forbids both |
| CURSE: VULNERABLE | `VoodooConeFlash`, a flat fan gone at 0.6 s; the SAME `hero-phaister-hex` clip and `cast-hex` first-person action | nobody can tell who is Vulnerable |
| HIGOP | a placeholder dark sphere with a ring (`VoodooBlackHole`); clip `hero-phaister-eclipse` from the retired Eclipse; the cutscene is still the retired Grand Coven ritual | *"make blackhole really cool"* is not met anywhere |

## 1. Filipino folklore she draws on (inspiration for a fictional witch, never a depiction of a real rite)

| Folklore | What it gives her |
|---|---|
| The *mangkukulam*: a witch who curses through a doll and pins | MANIKA MISCHIEF and SPOTLIGHT PIN: the rag doll and the long hat pins |
| The *mambabarang* (Visayas): sorcery sent as a swarm of insects | VANISHING ACT: she travels as a swarm of moths and beetles |
| The black butterfly (*paru-parong itim*), an omen | OMEN, the ultimate: a maelstrom of black butterflies round a black eye |
| Capul's moon story (`CHARACTER_ORIGINS.md`) | her sigils are lunar (the fictional writing already in `PhaisterSpellGeometry`); moonlight is her one light |
| Siquijor's herb brewing, black candles | small details on her model and in her sounds (a crackle of candle flame under her hexes) |

Boundaries: her spell writing stays fictional (as `CHARACTER_ORIGINS.md` says); no real prayer, *orasyon* or ritual is
shown or quoted; the tone is mischief, not horror. **Names are English** (owner: *"no tagalog"* for the ultimate).

## 2. Names (ids unchanged)

| Slot | Was | Now |
|---|---|---|
| Signature | SHADOW BLINK | **VANISHING ACT** (approved) |
| Attacking | CURSE: DISORIENTED | **MANIKA MISCHIEF** (approved) |
| Defending | CURSE: VULNERABLE | **SPOTLIGHT PIN** (approved) |
| Ultimate | HIGOP | **OMEN** (the black butterfly; *"no tagalog"*) |

## 3. The effect family

1. **Solid, toon-lit props** in her palette (black, violet, magenta, gold, bone for pin heads and moonlight). Only her
   sigils, moonlight, her eyes during OMEN and the eye's rim glow. Never white.
2. **Everything comes out of her and goes back**: moths from her sleeves and back into them, the doll from her belt and
   back to her hand, pins from her hat band, butterflies out of her robe and away into the sky. Endings crumble to ash,
   burn out as embers or fly off; nothing fades a solid by alpha.
3. **Wings are her particles.** Moths (small, violet, fluttering) for the signature, black butterflies (bigger, slow wing
   beats) for the ultimate; each typed by hand with its own size, wing angle and flight path. Never one quad stamped
   round a circle.
4. **Moonlight is her one light**: pale violet columns with a warm edge, drawn as edges on the court, never a fill over
   the can.
5. **Power sits in a small point; an area is drawn by what orbits it** and a ring of her sigils on the ground. Nothing
   reaches out to grab a player (that is Paete's).
6. **Readable in the 14 m box**; footprints 1.2 to 2.3 m except the ultimate; the can, chalk and players stay visible on Low.
7. **Typed by hand** in `tools/build_phaister_props.py`: the manika (and its victim-coloured variant), the hat pin, the
   moth, the beetle, the butterfly, the black eye.

**Her sound instruments** (`tools/build_phaister_audio.py`, numpy, seeded; transient, body and tail per cue; no two cues
share a recipe): `flutter` (many wings as grains, not hiss), `buzz` (beetle wings, a low rasp), `stitch` (a needle through
cloth, a small tearing pop), `tick` (a pin's bright metal), `moon` (a glassy chord), `candle` (a flame's crackle and
whoomph), `musicbox` (a detuned tine phrase, the doll's), `ash` (a dry crumble), `wind` (the maelstrom's draw), and the
motif `hex` (three falling tones) every time her sigil is drawn. Her laugh and lines will be recorded by the team.

## 4. The six beats per ability

### 4.1 VANISHING ACT (hold to aim, release, she is there; whoever she left is shoved 2.5 m)

| Beat | Body (everyone) | First person | Effect | Sound |
|---|---|---|---|---|
| Tell (aiming) | wrists crossed at her chest, moths crawling out of her cuffs | a few moths on the backs of her hands | her lunar sigil drawn on the court where she will arrive, three moths circling it | `flutter`, quiet |
| Release | she flings her arms open and bursts | her hands break into moths from the fingertips in | ~40 moths and beetles burst outward from her body (typed), the burst's ring is the shove | `candle` whoomph, `buzz` |
| Travel | (gone) | a flicker of dark wings across the screen | the swarm streams low along the aim as a ribbon, 0.15 s | `flutter` doppler |
| Arrival | the swarm spirals up at the sigil and knits her back from the feet up; she lands with a hand on her hat | the hands re-knit from the wrists out | the sigil flares | `hex` motif |
| Linger | strut resumes | | stragglers settle on her brim and shoulders, then crawl into her sleeves | a last wing flutter |
| Dissipate | | | the sigil burns out as violet embers | `ash` |

### 4.2 MANIKA MISCHIEF (a thrown doll; the first player within 1.2 m is Disoriented 4 s). Seen in FIRST AND THIRD person
(owner: *"i want ppl to see and hher to see that shees using it"*)

| Beat | Body | First person | Effect | Sound |
|---|---|---|---|---|
| Tell | she unhooks the rag manika from her belt, pricks its chest with a pin, smirks | the doll held up in her left hand, the pin going in | | `stitch` |
| Release | an overhand throw | the doll leaves the hand | a trail of violet hex smoke off the doll | a cloth swish |
| Travel | | | the doll tumbles on its arc, pins rattling in it | a pin rattle |
| Contact | | | it slaps onto the victim, drinks their colours (it becomes a doll OF them) and flies BACK to her hand | a soft slap, a rising sucking chime |
| Linger (4 s) | she holds the victim's doll in her off hand, slowly twisting its head, glancing at the victim | the doll of the victim in her lower left, her thumb turning its head; the victim's name tag on it | a small spinning sigil over the victim; on the VICTIM's screen, the hallucinations (built) and a glimpse of a doll of themselves at the edge | `musicbox` detuning |
| Dissipate | she crushes it | the doll crumbles in her fist | the doll pops its stitches and falls away as cloth and ash | `ash` |
| Miss | | | the doll lands, sits up, looks around, crumbles | one sad tine, `ash` |

### 4.3 SPOTLIGHT PIN (a 60 degree, 7 m cone; every attacker in it is Vulnerable 5 s; the light follows them)

| Beat | Body | First person | Effect | Sound |
|---|---|---|---|---|
| Tell | she draws a long hat pin from her hat band and holds it like a wand | the pin comes down into view | the pin's head glows | `tick` |
| Release | she stabs the pin down into the air in front of her | the stab | a crescent of her sigils sweeps across the cone on the court | `hex` motif |
| Contact | | | each attacker in it: a pin of light drives into the court at their feet and a column of pale violet MOONLIGHT drops on them from the sky | `tick` + `moon` per victim |
| Linger (5 s) | she points the pin at the lit victim | | the moonlight FOLLOWS them, everyone can see who is exposed (approved) | a low moon hum on them |
| Dissipate | she slides the pin back into her hat | | the column thins to one beam and snaps off; the pin crumbles to ash | a snap, `ash` |

### 4.4 OMEN (ultimate; 2.2 s cast, a black eye at an aimed spot up to 8 m; 5 s pull within 7.5 m; no escape)

**Direction rules for everything she makes** (so every moving part agrees with every other):
- Everything of hers that turns, turns **CLOCKWISE seen from above** (the maelstrom, the ground ring, the sigils, the moths
  circling the aim mark, the butterflies round her in the cutscene). One handedness reads as one magic.
- **Wings are always slow; speed lives in streaks, bodies and the camera** (Seele: fast strokes and slow butterflies in the
  same frame). A butterfly never moves faster than 4 m/s on screen; its wings beat 3 to 6 times a second, each its own rate.
- In the cutscene the omen flows **left to right** across the frame in every shot, so each cut continues the one before.
- In play, the pull reads **inward and down**: streaks on the court point at the eye, the butterflies' orbit tightens and
  speeds up toward the core, and nothing of hers ever moves outward except at the release and the end.

**Live, every moving part:**

| # | Part | Starts | Moves (direction) | Speed and shape | Anchored to | Ends |
|---|---|---|---|---|---|---|
| 1 | The mark (the 2.2 s cast) | a ring of her sigils drawn on the court at the aimed spot, 7.5 m, as she begins | the sigils write themselves clockwise round the ring, one after another | the full ring in 2.2 s, a rising pace | the court | the ring stays for the 5 s, turning slowly clockwise (6 degrees a second) |
| 2 | Her body (the cast) | the court | rises 0.4 m, robe, sleeves and hair lifting UPWARD; a violet light in her eyes | slow rise over 2.2 s, cloth fluttering fast | her | lands when the eye opens |
| 3 | Butterflies from her | her sleeves and hat brim | pour out and stream to the aimed spot in a low arc, clockwise round it | 30 of them over 2 s, each on its own curve | her, then the spot | join the maelstrom |
| 4 | The eye | a pinpoint at 1.1 m over the spot at 2.2 s | opens to 0.6 m | 0.15 s, an overshoot to 0.7 and back | the spot | see 11 |
| 5 | The rim | the eye's edge | a magenta corona that flickers | a slow throb, 1.5 a second | the eye | |
| 6 | The landing | the court under the eye | ONE shockwave ring racing out to 7.5 m; the court flashes in the shape of a BUTTERFLY (the emblem, Castorice) | 0.25 s | the court | fades in 0.4 s |
| 7 | The maelstrom | 60 black butterflies with magenta wing edges, typed by hand | orbit the eye CLOCKWISE in a flattened disc, faster and lower toward the middle, some dipping into the core | 0.4 turns a second at 7 m, 2 turns a second at 1 m; wings slow | the eye | see 11 |
| 8 | Pull streaks | the court at the boundary | thin violet streaks slide INWARD along the court toward the eye | 3 m/s, a few at a time, never many | the court | vanish into the rim |
| 9 | Caught bodies | wherever they are | slide in; they may push away and gain ground for a moment, then are dragged back (the owner's rule); at the rim they bob and struggle; butterflies swarm round each | the pull 1.5 m/s over their run speed | the eye | dropped at 11 |
| 10 | Slippers not hers | loose or flying | spiral in clockwise, spinning, and vanish into the core with a pop | faster than bodies | the eye | swallowed |
| 11 | The end (5 s) | the maelstrom | every butterfly bursts UP into the sky at once, a black fountain; the eye pops; everyone drops at the rim | 0.3 s up and out | the eye | three butterflies stay, land on caught players' heads, then flutter away one by one (Seele's aftermath, Hu Tao's single butterfly) |
| 12 | Her own screen | | a faint violet veil at the edges while the eye is open | | her camera | clears at 11 |
| 13 | A victim's screen | | butterflies flutter across the edges; the edges pull toward the eye as they near it; a low hum rises | | their camera | clears at 11 |
| 14 | The court light | the eye's 7.5 m disc | dims a little and cools to her violet (Castorice's domain, small) | 0.4 s in | the court | back at 11 |

**The cutscene, 4.0 s** (owner: *"try with shorter first like 3-5 seconds"*). About the power surging through HER (*"it can
be seen in her that power is surging in her and clothes are flying"*); no summon, nothing grabbing anyone.

One sentence: **"The omen pours out of her, and she throws it at them."** The travelling thing is the BUTTERFLIES, flowing
left to right in every shot.

| Shot | Time | Picture, every moving part | Camera | Sound |
|---|---|---|---|---|
| 1 SURGE | 0.0 to 1.5 | frame 0: black butterflies already crossing the lens left to right on diagonals (Castorice's near layer); the backdrop drops to HER NIGHT (the sky dims to violet-black, a pale moon, the court's surface picks up tiny glowing motes); she rises off the court, robe, sleeves and hair whipping UPWARD, her lids still half shut and a smirk; butterflies pour OUT of her sleeves and hat brim and spiral round her CLOCKWISE; curved violet streaks follow the camera's orbit | ORBITS her clockwise at waist height while pushing in (Seele's whip), from 3.2 m to 1.8 m | a rising wind, dense `flutter`, `candle` crackle |
| 2 THE EYE | 1.5 to 2.6 | a held close-up: her eyes OPEN and light violet, one glint (Seele's eye beat, 0.4 s); then her hands: the butterflies stream left to right into the space between her palms and crush into one black eye with a magenta rim; her face lit from below; she WINKS at the lens (Hu Tao's cheeky beat) | cut to the eyes, then a slow pull back to hands and face | the `hex` motif, the wind sucked in and cut dead |
| 3 OMEN | 2.6 to 4.0 | she flicks the eye down left to right; it lands on the aimed spot: TWO FRAMES INVERTED (Seele's impact frame, black and white with violet ink splashes), then the butterfly-shaped flash and the shockwave ring; the maelstrom blooms clockwise; the REAL targets (staged copies, their own clips) are yanked off their feet and slide in from the frame edges | a high crane looking down into the spiral, turning with it, her floating at the top of frame | a deep whump, the maelstrom's `wind`, one slipper whistle per slipper |

Play hands back at the cutscene's end state (the catch is never shown twice). The cutscene's night and moon are the
cutscene only; in play it is the small dim of row 14.

### 4.5 Every moving part of the three skills (the direction of each)

**VANISHING ACT**

| # | Part | Starts | Moves (direction) | Speed | Anchored to | Ends |
|---|---|---|---|---|---|---|
| 1 | Aim sigil | drawn on the court at the aim | writes itself clockwise; follows the aim | 0.3 s to draw | the aim point | burns out as embers after the arrival |
| 2 | Aim moths (3) | her cuffs | fly to the sigil and circle it clockwise | 1.5 turns a second | the sigil | join the swarm |
| 3 | Her body | standing | bursts from the core outward; the pieces ARE the swarm | instant | | re-forms at 6 |
| 4 | The burst | her chest | ~40 moths and beetles fly OUTWARD in all directions for 0.1 s | fast out, then they turn | her position | turn and stream to 5 |
| 5 | The swarm | her start | streams LOW along the aim in a twisting ribbon (clockwise twist about the travel) | 0.15 s | the path | spirals UP at the sigil |
| 6 | The knit | the sigil | the swarm spirals up clockwise and packs into her from the FEET UP | 0.2 s | the sigil | she is whole |
| 7 | Stragglers | 5 moths | settle on her brim and shoulders, then crawl into her sleeves | 1 s | her | gone |
| 8 | Shove ring | her start | a flat ring of dust out to 2.5 m | 0.15 s | the start | fades |

**MANIKA MISCHIEF**

| # | Part | Starts | Moves (direction) | Speed | Anchored to | Ends |
|---|---|---|---|---|---|---|
| 1 | The doll (hers) | her belt hook | up to her face, pricked, then thrown overhand | the throw's 12 m/s arc, tumbling end over end | her hand, then the arc | hits or lands |
| 2 | Hex smoke trail | the doll | a thin violet trail curling behind it | lingers 0.3 s | the doll's path | frays into motes |
| 3 | Pins inside | the doll | rattle (a sound, and two pin heads glinting as it tumbles) | | the doll | |
| 4 | The steal | the victim | the victim's colours drain INTO the doll as a swirl (clockwise) | 0.25 s | the victim to the doll | the doll wears their colours |
| 5 | The return | the victim | the doll flies BACK to her hand on a low arc | 0.35 s | her hand | held |
| 6 | Held doll | her off hand | she turns its head slowly back and forth; in first person it sits lower left, her thumb turning it | 4 s | her hand | crumbles to ash in her fist |
| 7 | Victim mark | over the victim | a small sigil spins clockwise over their head | 4 s | the victim | pops |
| 8 | Hallucinations | the victim's screen | phantom players and slippers (built), and a glimpse of a doll of themselves at the edge | 4 s | their camera | fade |
| 9 | A miss | the landing | the doll lands, sits up, looks both ways, crumbles | 1 s | the court | ash |

**SPOTLIGHT PIN**

| # | Part | Starts | Moves (direction) | Speed | Anchored to | Ends |
|---|---|---|---|---|---|---|
| 1 | The pin | her hat band | drawn out, held like a wand, stabbed DOWN in front of her | 0.3 s | her hand | slid back into the hat |
| 2 | Sigil crescent | her feet | sweeps across the 60 degree cone from her LEFT to her RIGHT, along the court | 0.2 s | the court | burns out |
| 3 | Light pins | above each attacker hit | drop straight DOWN into the court at their feet | 0.1 s | the victim's feet | crumble to ash at the end |
| 4 | Moonlight | the sky over each victim | a pale violet column drops onto them, then FOLLOWS them | 5 s | the victim | thins to one beam and snaps off |
| 5 | Motes in the light | inside the column | drift slowly DOWN | slow | the column | |

### 4.6 Her own animations (owner: *"refine the animations of phaister herself btw try to really show her personality in everyting"*)

Every clip of hers gets a pass for who she is: unhurried, sure, mischievous, a witch who enjoys it. One at a time, filmed,
never stamped across the cast (CLAUDE.md section 0).

| Clip | Today | Her version |
|---|---|---|
| Idle | the shared idle | weight on one hip; now and then (three variants, each rare) she adjusts her hat brim, spins a hat pin between her fingers, or a moth lands on her finger and she blows it away |
| Walk | a strut (`GaitStyles.Phaister`) | kept, plus her free hand touching the doll at her hip every few steps |
| Run | an exit with an imaginary cape | one hand pinning her hat to her head |
| Throw | shared | a flick from the wrist, a small wink on release |
| Pick-up | shared | she scoops it with two fingers, as if it were beneath her |
| Hit, stunned | shared | she clutches her hat first |
| Taya (defending) | shared | arms crossed, tapping a finger, watching |
| Win | shared emote | a smug curtsey with the doll held up |

## 5. The model, a light refinement (owner: *"u can refine a bit"*)

She stays who she is (she and Dante are the cast's reference). Witch details only, each typed by hand:
three hat pins through her hat band on her right, lying out, uneven (the pin SPOTLIGHT PIN draws); a small rag manika
hanging from the front of her belt at her left hip (the doll MANIKA MISCHIEF throws); two moths resting on her brim at
different turns. A crescent buckle was dropped: she already wears a gold crescent on her back, and a moon buckle was Nemu's
focal point in her rework. Applied by `tools/add_phaister_details.py` INTO the shipped glb (a rebuild loses her cast clips
and shortened arms). Turnarounds: `Logs/phaister-model-v24` (before) to `v27`.

## 6. Files this touches

- **New**: `tools/add_phaister_details.py` (done), `tools/build_phaister_props.py`, `tools/build_phaister_audio.py`, `Runtime/Visual/PhaisterSwarm.cs`
  (VANISHING ACT), `PhaisterManika.cs`, `PhaisterMoonlight.cs`, `PhaisterOmen.cs` (bodies for the classes in
  `VoodooVfx.cs`, whose host logic stays), `HeroAbilityClips.Phaister.cs` + motion author (four clips, none shared),
  `HeroIntroductionScene.Phaister.cs` rewritten, `PhaisterKitPlayProbe`, a rejoin probe for the eye and the moonlight.
- **Changed**: `PhaisterHeroKit.cs` (names, descriptions, actions, cues), `ViewmodelArms` (four first-person actions and the
  held doll), `CharacterAnimator` action map, `tools/author_ultimate_intros.py` `phaister()`, icons, `HeroLines.cs`,
  `AudioCues`, `WorldEffectSnapshot`, `tools/build_phaister_voxel.py` (section 5), `docs/TODO.md` HERO-10.

## 7. The owner's answers, 2026-09-27

| # | Question | Answer | What it becomes |
|---|---|---|---|
| 1 | Names | *"ok good"*; the ultimate: *"idk smth diff they suck rn"*, *"no tagalog hehe"* | VANISHING ACT, MANIKA MISCHIEF, SPOTLIGHT PIN; the ultimate is OMEN |
| 2 | Her model | *"u can refine a bit i dont mind"* | section 5 |
| 3 | Teleport | *"too flas, try to think of smth else, she's supposed to be using MAGIC AND VOODOO"*; then the barang swarm | 4.1 |
| 4 | Doll copies the victim | *"yes,,,, show it FPP and TPP okay? i want ppl to see and hher to see that shees using it"* | 4.2, she holds the victim's doll for the 4 s |
| 5 | The light follows the Vulnerable | *"its good, try to redo ur plan tho i want u to make her a frigging witchh not a showman"* | the whole plan redone; the light is moonlight |
| 6 | Serpent | *"give me diff proposition"*; then of a thread-knot pull: *"ur copying paete's ult"*; then the black butterfly omen | 4.4 |
| 7 | Cutscene length | *"try with shorter first like 3-5 seconds, we adjust if needed"* | 4.0 s |
| 8 | The ending | *"give me diff propsotiion"* | the butterflies burst up into the sky and everyone drops |
| 9 | ABILITY-2's numbers | *"those can stay"* | unchanged in `Core.VoodooRules` |
| 10 | Voice | *"we will record"* | `docs/HUMAN.md` rows for her lines; synthesized sounds only in the builder |

## 7b. The owner's notes while it was built (2026-09-27, films v1 to v6)

| Note | His words | What it became |
|---|---|---|
| The first eye | *"needs serious refinement"* (a black ball in a spiky magenta ring) | `Resources/Shaders/CosmosEye.shader` |
| Bigger, prettier | *"make it look bigger and make the actual blackwhole prettier like ur peeking into the cosmos or smth"* | the eye is a WINDOW INTO SPACE: a procedural nebula in her colours, two star layers, all turning clockwise and spiralling in, each layer shifting with the camera at its own rate (parallax), a thin hot magenta event horizon and a soft outer glow; 2.4 m across; two thin accretion rings turning round it |
| Aim and height | *"make it so that she can choose as well where blackhole goes and how high bcz i want it to be able to put ppl on the air as well haha"* | OMEN is placed where she looks (`AimsWhereLooking`) and as HIGH as she looks (`HeroAbility.AimsInTheAir`, `CameraRig.TryLookHeight`): 1.1 to 4.5 m (`VoodooRules.HigopMinHeight`, `HigopMaxHeight`); the height rides in the commit's aim, so every peer agrees; the pull's lift holds caught chests at the eye |
| Forming | *"glitchy and unstable as fuck when forming (make it pulsate?) it starts out small and gradually gets bigger"* | `CosmosEye._Glitch`: slices jumping sideways, a ragged rim, a colour split, dropouts; in the cutscene the eye is born tiny between her palms, pulsing on two beats and growing, and snaps open when it lands; in play it continues from there |

Found and fixed on the way: the doll was thrown at 1 m/s since ABILITY-2 (`Slipper.SolveArc` returns a direction; film v1 showed it
dropping at her feet); caught bodies were dragged into the eye's middle and covered it (`SeanceVoidComponent.HoldRadius`, a ring at
1.65 m, film v4); her own screen was decided by `NetAuthority.LocalSlot`, which is 0 in a solo match while her seat is 1
(`ViewmodelArms.IsFirstPersonFor`, film v2); fading effect materials must write `_BaseColor` too (`PhaisterProp.SetAlpha`, film v3).

Never shown twice: the cutscene ends with the eye thrown and half grown, so play opens on her hovering and watching it
(`hero-phaister-omen` v2, `PhaisterOmenLift`) and the live eye continues from half size.

## 8. Rejected, and why (so nobody proposes them again)

- **The showman** (v1: spotlights on a lamp rig, a stage curtain, a ta-da sting, bows): *"make her a frigging witchh not a
  showman"*. Her personality stays playful, but it shows as a witch's mischief, not stagecraft.
- **A curtain teleport**: *"too flas"*, not magic.
- **The serpent swallowing the moon**, and **a knot of thread reeling every player in**: the second is Paete's ultimate
  (something reaching out to each player and dragging them). Her pull is a force that swallows.
- **Filipino-word names** for the ultimate: *"no tagalog"*.


## 9. v3, the voodoo kit (2026-09-27, evening): the owner's new table replaces sections 2 to 4.5

Owner: *"the only one i like from last session is the teleport but that has to be improved too"*, *"here new status effects
and skill i want to let phaister have"*, *"we will completley refine hhow phaister works can u think with me on art direction and
how skills shouyld look and be executed"*, then *"thoroughly think abt each aspect of skill cast from top to bottom like all vfx
animation etc to deliver the messaghe that it is happening"*. MANIKA MISCHIEF, SPOTLIGHT PIN and OMEN (the moonlight, the
butterflies, the cosmos eye and its cutscene) are RETIRED by this table. `butterfly.glb` is kept for a later use (owner, on the
v11 film).

### 9.1 The table (the owner's words; numbers he gave are his, the rest are marked proposed)

| Slot | Name | The owner's text | Cooldown / cost |
|---|---|---|---|
| Passive | VOODOO | *"Whenever Phaister marks someone she takes 10% of their speed and they slow down by 10%"* | |
| Signature | TELEPORT | *"Teleport to the target location instantly."* | 35 s |
| Attacking | CURSE: DRAIN | *"Mark a person with a curse and attach their soul to the voodoo doll. After a 1.5 seconds delay, inflict Drained"* | 35 s |
| Defending | CURSE: HEX | *"Mark a person with a curse and attach their soul to the voodoo doll. After 10 seconds it can be recast to inflict Hex"* | 35 s |
| Ultimate | VOODOO DOLL | *"The voodoo doll becomes a sentient being that assists you in attacking or defending for the rest of the round."* *"The doll does not give points when tagged/sabotaged"*, *"The doll gives points gained to Phaister"*, *"The doll is a Hard AI"* | 12 objective points |

Statuses (the owner's status table, 2026-09-27):

| Status | Description | Tooltip |
|---|---|---|
| DRAINED | *"Depletes stamina to 0. Prevents stamina recovery for 2.5 seconds."* | *"Disabled Stamina Recovery"* |
| HEXED | *"Hallucinations of slippers randomly appear on your screen for 7.5 seconds."* (HUD Hexed visual effect) | *"Hallucinations"* |

The same table also retunes other heroes' statuses (Concussed 75 % slower for 2.5 s, Rooted 2.5 s, Whirled also blocks the can
reset, new Zapped and Haunted; Feared, Disoriented and Vulnerable absent). Those belong to those heroes' passes and are NOT
applied by this one; recorded in `docs/TODO.md` HERO-10. The owner will send each hero's passive on that hero's turn.

### 9.2 The owner's answers (2026-09-27)

| Question | Answer | What it becomes |
|---|---|---|
| "Players near it" | *"this is a typo bcz old version was throw based, only affects marked"* | each curse lands on the marked player only; no area |
| How she marks | first a thrown hat pin; then *"actually dont throw needle to mark them"*, *"i want her to just hold her hand out towards someone for like 2 seconds or smth and thats marked and whiels he's holding towards them it shows like an eerie vfx connecitng the two"* | THE REACH (9.5) |
| The teleport | moths plus a doll decoy | 9.6 |
| The doll's network side | *"You build it all"* | its own contract in `SKILL_NETWORK_CONTRACT.md` |
| The doll's body | *"create a new model for the voodoo i guess"* | 9.10, `tools/build_phaister_doll_voxel.py` |
| The slipper during a skill | *"think abt where slipper goes when u use skill and make it so that u can use right hand when doing skills"* | 9.4 |
| The doll while she ATTACKS (asked 2026-09-28: assist without a slipper, its own slipper, or defend only) | **"Own slipper, throws"** | a true fifth player with its own fifth slipper, its knockdowns paid to her; 9.12 |
| The cutscene pitch 9.8 (SEW / GROW / WAKE: THE THREAD, THE STITCH, THE LIFT, THE WAKE), asked 2026-09-28 | **"her ult is supposed to be like its own character i approve everything except for any part where she ends up dead and controls the vooodoo"** | APPROVED as pitched. Standing rule: the doll is its own character (its own AI); she never dies, faints or possesses it, in the cutscene or in play |
| The cast's spectacle (his note, 2026-09-29) | *"to make it cooler cast like a big magic circle in the sky or smth when she ults"* | THE CIRCLE: a huge stitched magic circle opens in the sky over the court when she ults; the doll's string hangs from its centre. In the cutscene (9.8 shot 3) and in play for every player (9.7, 9.9 rows 17 and in-play 12) |

### 9.3 The idea, the look, and how a cast talks

**One sentence: what she does to the doll happens to you.** Every skill is two beats: her hands act on the doll (the cause), and
the one she marked feels it across the court (the effect). Players learn to watch her hands.

- **One material family:** rag cloth, thread, X stitches, pins, stuffing. No moon, no butterflies, no space.
- **One light:** a soul glow in her palette, only on threads, marks, pin heads and the doll's eye. DRAIN glows **crimson** (her
  `CRIMSON`, lifted), HEX glows **violet** (her `LILAC_GEM`), so a player can tell which curse is on whom from across the court.
  Shape carries it too, for colour-blind players: DRAIN is a TWIST (spirals, a wrung cloth), HEX is an EYE (a button, a stitched band
  across the eyes).
- **One graphic, the stitch:** dashed seams for threads, aims and trails; X stitches for marks. A dashed line reads at 14 m on Low
  where a soft glow does not.
- **Moths only in the teleport** (the one he kept).

**How a cast talks.** Every cast answers five questions, for three people, at the same moment on every layer (her body, her screen,
the effect, the target's body, the target's screen, the sound): **is it coming? who is it for? is it working? did it land? when
is it over?** The three people are HER (it is working), the TARGET (it is happening to me, and here is my way out) and EVERYONE
ELSE (who is doing what to whom). A layer that says something different from the others at that moment is cut.

| Question | Her | The target | Everyone else |
|---|---|---|---|
| Is it coming? | her tile lights; a bracket on the player she would reach | her body's tell (arm rising toward them) | the tell |
| Who is it for? | the bracket, the thread | the thread lands on THEIR chest; an edge marker points to her | the thread across the court |
| Is it working? | the doll changing into their colours | the vignette creeping in, the heartbeat quickening | the thread tightening, their ghost leaning out |
| Did it land? | the tug, the doll wearing them | the stitch mark, the jolt, the status icon | the snap, the mark over them |
| When is it over? | the doll going back to burlap at her hip | the pins popping out, the screen clearing | the mark fraying away |

### 9.4 Where the slipper goes

A carried slipper leaves her right hand whenever a skill needs it: for THE REACH and both curses she tucks it into her belt at the
back (the right hand reaches, the left holds the doll), and it returns to her right hand when the gesture ends. In first person
the right hand dips out of the bottom right of the frame as it tucks the slipper, and comes back up with it. The TELEPORT keeps it in
hand (she arrives holding it). This is a shared mechanism: each ability declares where the carried slipper goes while it is cast
(`Hand` right as today, `OffHand` left, `Belt`, or `Kept`), the default is the left hand, and each hero's choice is their own.

### 9.5 THE REACH: how both curses mark (proposed numbers)

Input (proposed): **tap** the skill to start reaching toward the opponent under the crosshair; it keeps going on its own while she
keeps them in view; tap again to let go. (Holding the key for the 2 s is the alternative: it matches "hold her hand out", but on a
phone it ties up the thumb that aims the camera.) Reach 9 m to start, breaks past 11 m, behind a wall, or if they leave 35 degrees
of her aim; she can walk while reaching but not sprint. A broken reach refunds half the cooldown. Each curse reaches in its own way.

| Time | Her body (everyone) | Her screen | The effect | The target's body | The target's screen | Sound |
|---|---|---|---|---|---|---|
| Ready | the doll at her hip twitches once | a thin stitched bracket on the opponent she would reach (her screen only) | | | | |
| 0.00 lock | slipper tucked to the belt (0.12 s); left hand unhooks the doll; right arm swings up toward them, palm out. DRAIN: arm at their chest height, leaning back as if pulling a rope. HEX: arm higher, at their head, the doll lifted to her own cheek, peeking over it | right hand rises into view, the doll lower left | the thread whips from her palm to them in 0.12 s and pierces with a small X stitch: DRAIN at the chest, HEX at the eyes | a small flinch | a stitched thread enters from the edge in her direction; a marker at the edge points to her; a status chip "BEING CURSED" with a 2 s ring, named DRAIN or HEX | a sharp thread whip and a needle prick; for the target, a heartbeat thump |
| 0.1 to 2.0 hold | she leans into it; her arm trembles; the doll twitches in time with the target's heartbeat | the thread runs from her palm; a ring fills round her crosshair | the thread: a thin wavering cord of dark smoke with a bright core in the curse's colour and dashed stitches along it; the stitches crawl from them TO her; it tightens as it fills (less sway, thicker, brighter). The doll in her hand changes into their colours from the feet up (the progress bar is the doll) | a pale ghost of them leans out of their body toward her, further as it fills | the vignette creeps in from her side; the heartbeat quickens; the chip's ring fills | a low hum rising; cloth creaking; the heartbeat for the target |
| 2.0 mark | a sharp tug back to her chest, as if pulling a stitch tight | the tug; the doll now wears them | the thread snaps taut, zips into the doll; their ghost is yanked along it INTO the doll | a stagger (0.3 s); the MARK appears over them (DRAIN: a twisted crimson knot; HEX: a violet button with a stitch through it) | a stitch stamp flashes at the edge; one skipped heartbeat; the chip becomes the mark's status | a stitch pulled tight; a breathy gasp for the target |
| broken | her arm snaps back; the doll goes limp back to burlap | the thread frays off her palm | the thread unravels into strands and snaps back toward her | the ghost slides back in | the thread and vignette withdraw | a fray and snap |

The **VOODOO passive** starts at the mark and lasts while the mark lives (proposed): the marked one runs 10 % slower and she runs
10 % faster. Seen on both: their footsteps leave faint stitched prints in the curse's colour, hers a short trail of moth dust; her
HUD shows VOODOO +10 %, theirs VOODOO -10 %.

**CURSE: DRAIN (attacking): she wrings them out.**

| Time after the mark | Her body | Her screen | The effect | The target's body | The target's screen | Sound |
|---|---|---|---|---|---|---|
| 0.0 to 1.5 the wind-up | both hands on the doll of them, twisting it tighter and tighter (she keeps walking) | the doll twisting in both hands | a crimson spiral tightens round the target's middle in time with each twist | their body twists a little with each turn of hers | their stamina bar shakes and starts to fray at its ends; the crimson vignette pulses with each twist. The message: your stamina is about to go, spend it now | rope creak on each twist; the heartbeat heavy |
| 1.5 the wring | the last hard wring; drops wrung out of the doll | the drops fall past her hands | the spiral snaps tight and bursts; crimson drops scatter from them | a slump: shoulders drop, head bows (0.6 s), then they keep moving | the bar pinches in the middle like a twisted cloth and empties at once; two crossed pins stamp over it | a wet-cloth squeeze; an exhausted exhale |
| 1.5 to 4.0 DRAINED | the doll goes limp and back to burlap; she hooks it on her hip; the slipper comes back to her hand | | | a little grey and crimson in their colour; two crossed pins as the status over them | the pinned bar; no sprint | |
| 4.0 recovered | | | | the colour returns | the pins pop out and the bar starts refilling | a tink and a breath in |

**CURSE: HEX (defending): she pins its eye.**

| Time after the mark | Her body | Her screen | The effect | The target's body | The target's screen | Sound |
|---|---|---|---|---|---|---|
| 0 to 10 the fuse | the doll of them hangs at her hip in their colours, a pin in its head; it swings as she moves (everyone can see whom she holds) | her Hex tile turns into a fuse ring filling for 10 s; a thread marker at the edge of her screen points to the marked one | the pin's head in the doll lights up slowly, like a fuse | their mark (the violet button) fills with light as the fuse burns | a small chip HEX MARKED with the same ring; a faint stitched seam at the corners | a slow tick for the target only, speeding up near 10 |
| 10 to 25 armed | the doll at her hip jitters, the pin glowing | the tile reads RECAST, pulsing | | the button over them is fully lit and throbs | the chip throbs; the tick holds steady | a low hum while armed |
| recast | she yanks the doll to her face and stabs the pin into its button eye, grinning | the stab, close in her hands | the pin goes in; violet threads burst from the doll's eye | the button over them bursts; a stitched band snaps across their eyes (seen by all for the 7.5 s) | a stitch-blink: a seam closes across the whole screen for 0.15 s and opens; then HEXED | the stab; a squelchy pop; a music-box sting falling |
| 0 to 7.5 HEXED | the doll goes back to burlap at her hip; the slipper returns | | | the stitched band across their eyes, fraying at the end | phantom slippers appear at random, among the real ones, each for a few seconds; a phantom casts no shadow (the tell for sharp players); the seam vignette at the corners | a detuned music box, muffled, and whispers under it |
| 7.5 end | | | | the band frays away | the seam unstitches and pulls off the screen | a breath out |
| 25 expiry (never recast) | the doll at her hip goes limp and back to burlap | the tile goes to cooldown | the pin in the doll crumbles | the button over them unravels | the chip fades | a soft fray |

### 9.6 TELEPORT (the kept one, improved): she leaves you a doll

| Time | Her body | Her screen | The effect | Anyone chasing her | Sound |
|---|---|---|---|---|---|
| aim (hold) | wrists crossed at her chest, moths crawling out of her cuffs, more as the hold goes on | her lunar sigil on the court where she will arrive, three moths circling it; her crossed hands low in view with moths on them | | the tell: crossed wrists and gathering moths mean she is about to vanish | a quiet flutter |
| 0.00 release | she is at the sigil at once, already upright, a hand on her hat brim (the slipper still in her hand) | a flutter of wings at the frame's EDGES only, a small field-of-view kick, a violet pulse at the edges; her hands re-knit from moths at the fingertips | at the sigil: a swirl of moths spirals up round her and settles; the sigil flares and burns out as embers | where she stood, a limp rag doll OF HER (her hat, yarn hair in her colour) in her pose | a whoosh in, her three-note motif |
| 0.00 to 0.35 | | | | the decoy flops: knees buckle, it folds and sits slumped (the gag lands; a taya lunging at it gets nothing) | a cloth flop and a small music-box plink |
| 0.35 to 0.9 | | | the decoy bursts into about 30 moths that stream in a low arc to her new spot and into her sleeves; a dashed stitched trail on the court from there to her fades over 0.8 s | the trail says where she went | a flutter doppler toward her |

Fixes owed from v11: the tell is filmed from the front; no smoke or insect on her own lens. The 2.5 m shove is dropped (not in
his text).

### 9.7 VOODOO DOLL (the ultimate)

| Beat | What happens | Sound |
|---|---|---|
| Ready | the doll at her hip wakes: its button eye glows faintly, it turns its head to look at whoever is near, it twitches now and then. Everyone who sees her knows her ultimate is up | a faint music-box note when it first wakes |
| Cast | the cutscene, 5.0 s, the match clock frozen (9.8). THE CIRCLE opens in the sky over the court: a ring of glowing thread about 12 m across, 9 m up, stitched with X stitches, eight pins driven in round its rim at the compass points, her sigil inside it, crimson outer ring and violet inner, turning slowly. Everyone on the court sees it the moment the cutscene ends | a deep music-box chord and a cloth-tearing swell |
| Hand-back | the doll stands beside her at player size, swaying, head tilted, eye lit, its string running up to the centre of THE CIRCLE; play picks up here. The circle hangs at full size for 3 s, then draws in to a small ring (about 1.5 m) that follows the doll high overhead as the top of its string for the round | the chord decays |
| In play | a fifth body on her side, Astig (hard) bot. It walks like a puppet with half its strings cut: head lolling, arms swinging late, a jerk now and then. Nameplate PHAISTER'S DOLL in her colour. Attacking: it throws its own slipper at the can with a floppy overhand. Defending: it guards and tags with a flopping lunge. Its points pop over it as +100 in her colour with a small doll icon, and go to her | soft cloth footsteps, creaks, a music-box motif when it scores |
| Tagged or sabotaged | stuffing puffs out, it sits slumped like a dropped doll for the stun; a grey stitched X pops over it so the one who tagged it knows it paid nothing; it gets back up with a jerk as if its strings were pulled | a stuffing puff; a sad tine |
| Round end | it shrinks back in three jolts (the cutscene's growth in reverse) to a small limp doll that unravels into thread; her hip doll is back | a descending music box |

### 9.8 The ultimate's cutscene, v2 pitch (about 5.6 s)

Owner: *"thoroughly plan as well how the new ult cutscene woudl look like and pitch it to me"*, *"the vfx theme animation direciton
etc"*, *"thoroughly think abt how to make each part of the voodoo doll ult that moves"*. Written for the v7 doll: a rag sack
glowing inside and stitched shut.

**One sentence: "She pours her soul into the doll, sews it shut, and her magic lifts it off the ground."**
**The travelling thing: her soul light, left to right in every shot:** out of her palm as a thread, through the needle, into the
doll's chest, out through its seams, up its crown string, and out of its eyes at the end.

**The theme.** A puppet show in the dark. When she casts, the world steps back: the court drops to violet-black and the only
light is hers (the method's "the backdrop steps back"). Everything is cloth, thread, pins and that light. The emblem is her X
stitch, the last one she sews; it flashes once on the court under the doll. The power is the string and THE CIRCLE (owner, 2026-09-29: *"a big magic circle in
the sky"*): a single glowing thread from the doll's tied crown up into a huge stitched magic circle that opens in the dark sky
above the court, the vertical light and the spectacle every ultimate needs. The sound is a detuned music box over
a slow heartbeat.

| Shot | Time | Picture, every moving part | Camera | Sound |
|---|---|---|---|---|
| 1 THE THREAD | 0.00 to 1.40 | The court goes dark round her at frame 0; motes of her light drift up out of the dark. The small doll lies limp in her left palm, its chest split open and dark. She draws a long pin from her hat band; a strand of soul light pulls out of her right palm like thread off a spool and she threads the pin with it. Her eyes catch the glow from below. Loose glowing threads drift across the lens left to right (the near layer). | a close two-shot of her hands and face, pushing in slowly, a slight tilt | a low hum rising; a thread drawn off a spool; one faint heartbeat |
| 2 THE STITCH | 1.40 to 2.60 | She stabs the pin through the doll's chest split and draws the thread through in one long pull: the thread whips across the frame left to right as a stroke of light (the strike). She knots the X. Two impact frames on the knot (the whole picture inverted, her ink splashing). Then the doll floods with light: the splits light up one after another (chest, shoulders, spine, thigh), its grin glows, light leaks round its button, the eye under its X opens behind the stitches. | tight on the doll in her palm, then a whip pan riding the thread's pull | the pin's prick; a long cloth draw rising in pitch; the knot's snap; silence for the two frames; a deep heartbeat as it floods |
| 3 THE LIFT | 2.60 to 4.20 | She tips it off her palm. It tumbles down through the frame and lands in a heap with a puff of stuffing. Her X stitch flashes on the court under it as a ring of light races out. Far above, THE CIRCLE opens in the dark sky: its rim sews itself round in one sweep (stitches racing clockwise), eight pins stab into the rim one after another, her sigil blooms in the middle. A glowing string shoots up out of the doll's tied crown to the circle's centre and pulls taut: it is hauled up off the court by its head, limp, toes dragging. It grows in three jerks, each one a yank on the string: its seams flare and spit light, its pins pop out and slide back in, stuffing puffs from its hem, until it hangs at full size, limbs dangling. | low on the court looking up past the doll to THE CIRCLE filling the sky; the doll grows up into the frame, the string rising to the circle's centre | a soft thud; three cloth stretches with a bass swell, each bigger; the music box plays one note per jerk |
| 4 THE WAKE | 4.20 to 5.60 | A held beat, hanging still, head lolled. Then its head snaps up: light streaks out of its X eye and round its button across the lens, and its grin splits wider and brighter. It cracks its neck side to side. It turns its head to the REAL opponents (staged copies of who is on the court, their own clips), who flinch. She steps in beside it, leans her elbow on its shoulder and points at them with her chin, smirking. | a slow orbit from behind its shoulder (the opponents ahead, soft) round to a two-shot of her and the doll with the opponents in frame | a held silence; a neck crack; her three-note motif on the music box, resolving |

**Hand-back.** Play opens on the doll standing beside her, lit, facing the opponents, its string up. Nothing is shown twice: the
cutscene ends on it waking; play is it moving. The world was frozen underneath (the match clock stops, as Paete's did).

### 9.9 Every moving part of the doll, in the cutscene and in play

| # | Part | Starts | Moves (direction) | Speed and shape | Anchored to | Ends |
|---|---|---|---|---|---|---|
| 1 | The dark | frame 0 | the court's light drops to violet-black round her | 0.2 s in | the scene | lifts at the hand-back (0.4 s) |
| 2 | Motes | the dark | drift UP, slowly, a few at a time | 0.3 m/s | the air | fade in the last shot |
| 3 | The soul thread | her right palm | pulled out to the RIGHT like thread off a spool, then threaded | 0.8 s | her hand, then the pin | pulled through the doll |
| 4 | The pin | her hat band | drawn up and out, then stabbed DOWN through the doll's chest | 0.25 s stab | her hand | stays in (it is the pin in its chest) |
| 5 | The pull | the doll's chest | the thread whips LEFT TO RIGHT across the frame as a stroke | 0.2 s | the pin | knotted |
| 6 | The flood | the chest split | light runs split to split: chest, shoulders, spine, thigh, grin, button, eye | 0.08 s apart | the doll | stays lit (the glow mesh) |
| 7 | The fall | her palm | tumbles DOWN, turning once | gravity | the air | a heap on the court |
| 8 | The emblem | under the doll | her X stitch flashes on the court and a ring races OUT | 0.25 s | the court | fades 0.4 s |
| 9 | The string | the doll's crown | shoots UP into the sky and pulls taut | 0.1 s | the crown, and a point in the dark far above | stays for the round |
| 10 | The growth | a heap | hauled UP by the string in three jerks: 0.35, 0.7, full size | each jerk 0.12 s, a rest between | the string | hanging at full size |
| 11 | Spurts | each split | light spits OUT of the splits at each jerk | 0.1 s | the doll | gone |
| 12 | Pins | its head | pop OUT at each jerk and slide back IN | 0.1 s out, 0.2 s in | the doll | back in |
| 13 | Stuffing | its hem | puffs DOWN and out at each jerk | 0.3 s | the doll | settles |
| 14 | The wake | its head, lolled | snaps UP, then cracks side to side | 0.08 s snap, two cracks | the doll | facing the opponents |
| 15 | Eye streaks | its eyes | light streaks out sideways across the lens | 0.15 s | its eyes | fade |
| 16 | Her lean | beside it | steps in, elbow onto its shoulder | 0.4 s | the doll | the hand-back pose |
| 17 | THE CIRCLE | the dark sky over the court, about 9 m up, 12 m across | its rim sews round CLOCKWISE in one sweep, eight pins stab IN one after another, the sigil blooms OUT from its centre, then it turns slowly | 0.5 s sew, 0.06 s per pin, 0.3 s bloom, then 8 degrees/s | the court (the doll's string hangs from its centre) | stays through the hand-back (row 12 in play) |

**In play** (the doll is a fifth body on her side for the rest of the round; the owner: *"make the walking animation of this
voodooo look like its fucking dead or js getting dragged around by magic idk"*, *"js dont make it human like"*):

| # | Part | What it does | Why |
|---|---|---|---|
| 1 | The string | a thin glowing thread from its crown up into the air, fading out a few metres up; it sways behind the doll's moves and snaps taut on every jerk | it is her magic that moves it, not legs; everyone can see what is doll and what is player |
| 2 | Walk | it hangs from the string: head up, body limp under it, feet barely lifting and dragging their toes, arms dangling and swinging only when its body sways; every few seconds a jerk yanks it upright and its limbs flick | dead, carried by magic, not walking |
| 3 | Run | the string drags it: its chest leads, its legs trail behind with their toes scraping, arms flung back | dragged, not running |
| 4 | Idle | it hangs and sways, head lolled, a twitch now and then, a slow breath of light up its seams | alive only inside |
| 5 | Throw (attacking) | the string yanks its arm up and flings it; the slipper leaves; it flops forward after | a puppet's throw |
| 6 | Tag (defending) | it is yanked forward along the court by the string, arms first, lands in a heap and is hauled back up | a puppet's lunge |
| 7 | Pick-up | it folds at the waist like a dropped puppet and is hauled back up | not a human bend |
| 8 | Tagged or sabotaged | the string goes slack: it collapses sitting, its light dims to embers, stuffing puffs, a stitched X pops over it (no points); after the stun the string snaps taut and yanks it up | reads as "switched off", and the tagger sees it paid nothing |
| 9 | Scores | its seams flare, the music box plays a note, +100 floats up in her colour with a small doll mark | the points are hers |
| 10 | Toe scuffs | faint lit scuffs where its toes drag, fading in a second | you can follow where it went |
| 11 | Round end | the string hauls it up, it shrinks in three jerks back to hand size and the thread reels it back to her hip | nothing vanishes; it goes back where it came from |
| 12 | THE CIRCLE | full size over the court for 3 s after the hand-back, seen by every player; then it draws in to a small ring high over the doll, the top of its string, turning; at round end it unstitches and frays away | the whole court learns her ultimate is on; the string has somewhere to hang from |

### 9.10 The doll's model

⚠️⚠️ ITS OWN VOODOO DOLL, NOT A SMALL PHAISTER. Owner on v3 (which wore her hair as yarn and her hat): *"this shit suckls why
does it have her hair hahahahaa"*, *"and her hat"*, *"make it look like its own vooodoo wtf"*. v5: a stuffed burlap sack on the
cast's seven-bone rig, tied off at the crown with a frayed tuft, a purple button sewn on for one eye and an ink X for the other,
a stitched grin, eight pins with coloured heads (one through its heart patch), twine at the neck, waist, wrists and one ankle,
patches and stuffing out of a torn hem. Her colours only in the pin heads and patches. `tools/build_phaister_doll_voxel.py`,
brief `ArtSource/phaister/doll-20260927/design-brief.md`, renders `Logs/phaister-doll-vN/`. The small doll at her hip will be
the same design at hand size.

**v11 (2026-09-27 night) supersedes the body above.** The owner sent a reference render to take from with its colours moved round
(`ArtSource/phaister/doll-20260927/owner-reference-20260927.png`): Paete's size, big and fat with a pot belly, textured burlap,
mitten stumps with NO fingers (twice), and the light coming out of real openings rather than painted on. The brief's v11 section has
the table of what was taken and what changed colour.

### 9.11 Proposed numbers (the owner may retune any)

| Number | Value | Why |
|---|---|---|
| Reach start / break range | 9 m / 11 m | the court is a 14 m box; 9 m reaches across half of it |
| Reach cone / time | 35 degrees / 2.0 s | his *"like 2 seconds or smth"* |
| Broken reach refund | half the cooldown | a broken reach should cost something, not everything |
| Hex mark life | 25 s (10 armed-in plus 15 to use it) | long enough to pick the moment, short enough not to hang over a round |
| Passive duration | while the mark lives | his text ties it to the mark |
| Teleport range | 2.0 to 5.5 m | today's, kept |
| Doll body | PAETE'S SIZE (owner: *"make like paete size"*), a player's rules; a tag stuns it 5 s; no skills | the Astig bot plays it like a player |
| Doll speed | 0.65 of a player's walk and run (`VoodooRules.DollSpeedScale`, set on the body as `CharacterMotor.BodySpeedScale`; proposed number, the rule is the owner's: *"big fat voodoo doll that's kinda sllow(to balance it)"*, *"make him look sluggish and its okay if he's slower than others"*) | a Hard AI that never tires would otherwise out-run the round; and at a player's speed its short legs could only skate (film v3 "looks liek he is floating") |

### 9.12 The doll as a fifth body: how it is built (2026-09-28)

The owner chose the full version (9.2): when she attacks, the doll is a real attacker with its OWN slipper, which it throws at the
can; when she defends, it defends. The game has four seats everywhere (slippers by seat of origin, poses by seat, holders by seat),
so the doll gets a seat of its own rather than borrowing hers: a COMPANION SEAT, `PlayerCount + her seat` (4 to 7). One number then
means one body on every peer, in every message that already names a body by seat: its pose, its slipper (seat of origin and
holder), its throws and lunges. Nemu's KURO PLAYS is the same kind of body and will use the same seats.

| Rule | How |
|---|---|
| Its points are hers (*"The doll gives points gained to Phaister"*) | `MatchDirector.AddScore` maps a companion seat to its owner (`Core` `CompanionSeats.OwnerOf`), so a knockdown by its slipper and a tag by its lunge score for her through the paths that already exist |
| Tagging it pays nobody (*"The doll does not give points when tagged/sabotaged"*) | the tag stuns it 5 s where it stands (no respawn), no score, no sabotage credit; a grey stitched X over it (9.7) |
| Hard AI (*"The doll is a Hard AI"*) | an `AIController` at Astig on the host; no skills |
| Its side | her role for the round (`IsDefender` copied from her); as the taya it guards and tags with her, confined like her |
| Its slipper | a fifth slipper, seat of origin and owner = the doll's seat, armed into its hand when it attacks, parked when it defends |
| Lifetime | spawned by her ultimate beside her, removed with its slipper at the round's end, the match's end, or if she leaves |
| Slow | `VoodooRules.DollSpeedScale` 0.65 on the body; its own dead, dragged gait (`GaitStyles.PhaisterDoll`) |
| Network | host-owned; a spawn and a despawn message scoped to match and round; its pose, slipper and actions ride the existing seat-keyed messages with companion seats admitted; a rejoiner is sent live companions; protocol bumped. The contract is `docs/SKILL_NETWORK_CONTRACT.md` "Companion bodies" |
