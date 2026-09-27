# Phaister's overhaul: the plan, 2026-09-27 (v2, the witch)

Owner, 2026-09-27: *"rename her shit too hahah it sucks ass"*, *"it needs really great presentation VFx ANIIMATION SFX AND
DIRECTING"*, *"think abt her personality too in making her cutscenes and vfx"*, *"thoroughly refine existing animation
effects and models and vfx of her skills"*. Then, on v1: *"i want u to make her a frigging witchh not a showman"*, *"she's
supposed to be using MAGIC AND VOODOO BTW"*, *"think of fil cutlure integration with her shit"*. Method:
`docs/HERO_KIT_METHOD.md`. Brief: `ArtSource/phaister/kit-20260927/design-brief.md`. Research: [research.md](research.md).

**The one sentence for her kit: a mischievous Visayan witch from Capul whose magic is moths, moonlight and a rag doll
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
