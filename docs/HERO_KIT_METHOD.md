# How a hero kit is built here: Paete, the worked example

Owner, 2026-09-27: *"i want u to record in a docs somewhere too how this entire character (paete) shit was build all abiltiies sfx
and requirements and ult cutscene and refinements so that it can be used as inspiration ... to make the abilities for other
charcters"*, and on Paete himself, 2026-09-26: *"I WANT THIS TO BE THE BASELINE QUALITY OF EVERYTHING ELSE MOVING FORWARD"*.

**Who this is for.** Any session building or reworking a hero's kit (abilities, their animation, effects and sound, the ultimate's
cutscene), including a less capable model working alone. It is written as steps to follow, in order, with the reason for each
step and the mistake that step prevents. Every reason is something that actually went wrong on Paete or was said by the owner.

**How to use it.** Read `CLAUDE.md`, `docs/VISION.md` and `docs/TODO.md` first (they are the rules; this is a method). Then read
this whole file once. Then, for your hero, copy the checklist in section 9 into the hero's TODO entry and work down it. When a step
here disagrees with the code, the code is current and this file is out of date: fix this file in the same commit.

**What it is NOT.** Not Paete's look. Paete is a tree from Mount Makiling; your hero is not. Take the METHOD, the order, the beat
tables, the review loop and the traps. Never copy his shapes, colours, timings or gags (owner, 2026-09-24: *"dont js spam copy paste
stuff bcz it will be boring"*).

Where Paete's own records live, for the detail behind every line below:

| What | Where |
|---|---|
| Brief, lore, model plan | `ArtSource/paete/concept-20260925/design-brief.md` |
| Research (Groot, Kinich, Zyra, Scorpion, Dead by Daylight, Zarya) | `docs/reports/paete-kit-2026-09-25/research.md` |
| Plan, the owner's twelve answers | `docs/reports/paete-kit-2026-09-25/plan.md` sections 6 and 7 |
| Direction, every version, every owner note | `docs/reports/paete-kit-2026-09-25/direction.md` sections 0 to 5.15 |
| The shared cutscene VFX and SFX vocabulary | `docs/reports/ultimate-performances-2026-09-24/research.md` section 4 |
| Status and what is still open | `docs/TODO.md` HERO-9 |
| Model method (research, cast fit, markings, review) | `docs/CHARACTER_MODEL_METHOD.md` |

---

## 0 · The owner's standing requirements for a kit (every one is a quote or a rule, not a style choice)

1. **Research first, then a written plan, then build.** *"thoroughly plan how to do it first and research"*, and 2026-09-27 *"can u
   do more reserach on genshin ult cutscenes and otehr games before actually starting"*. The plan goes in
   `docs/reports/<hero>-kit-<date>/`; the research is from FOOTAGE where it can be (section 2 says how).
2. **Typed by hand, one part at a time.** *"do it one by one dont try to mass generate it"*, *"manually do each part of that builder
   dont js auto generate looping shit"*. Every leaf, cord, petal, root, row of a particle table has its own typed numbers. A loop
   that stamps one shape round a circle at one size is the thing he rejects.
3. **Each ability gets its own animation, effects and sound.** *"i want each of his skill to have their own animation"*, *"think of
   vfx that should accompany it as well as sfx"*. No clip, effect builder or sound recipe is shared between two verbs.
4. **Nothing appears from empty air.** *"coming out of the ground each time and forming on the spot not just spawning in"*, *"i also
   dotn want the tree to jsut spawn in or teleport in"*. An effect starts ON something (his arm's strands, the court, her hands) and
   ends back IN something (it withers, reels back, sinks).
5. **It must look like it belongs in TUMP.** `docs/Art_Direction.md` section 0: blocky, cute, readable on Low, no realistic detail.
   Faces: ink only, typed by hand (Paete's two engraved hollows were his one exception, asked for).
6. **Readable in a 14 m box.** `docs/VISION.md` section 2: a normal skill's footprint is 1.6 to 2.3 m of radius; only an ultimate may
   be big; a frame mid-fight must still show the can, the chalk and every player. 2026-09-27: *"a lot more sleek so that it isnt too
   distracting"* and *"dont let it be placed in a place it STANDS on can"*.
7. **Film it in a match and send the video.** The owner judges from mp4s in chat, from HIS screen, the court, and a caught player.
   A green test is not a verdict.
8. **The ultimate's cutscene never passes 5.0 s, and match time is frozen under it.** *"dont go past 5 seconds for cutscene"*,
   *"match time should pause during cutscenes"*.
9. **The repo's hard rules, every time** (`CLAUDE.md` sections 3, 4, 4a, 6.4): the host resolves contact by distance; stuns overlap
   with `Max()`; every impulse comes from `Friction` (write the distance, solve the speed); every action has a pad and a thumb
   control; no blue in any UI; no em dashes; no co-author trailer; no AI mentions; sourced voices are human recordings only.

---

## 1 · The order of work, and the deliverable of each step

Paete took about fifteen commits and more than a dozen rounds of owner review (section 7 lists them). Doing it in this order is what
keeps a round from undoing the one before.

| Step | Deliverable | Gate before the next step |
|---|---|---|
| 1 Brief and research | `design-brief.md` (who, lore, references read), `research.md` (what each reference TEACHES, and what we do NOT take) | written, pushed |
| 2 Plan and questions | `plan.md`: names, the kit mechanically (every number marked proposed), the six beats per ability, the file list, ONE batch of numbered questions | the owner's answers recorded in `plan.md` as a table: answer, quote, what it becomes |
| 3 Model | the character, `docs/CHARACTER_MODEL_METHOD.md`; versioned turnarounds and a cast lineup | the owner's verdict on the model |
| 4 Core rules | `Packages/com.tumbangpreso.core/Runtime/<Hero>Rules.cs` + `Core.Tests/<Hero>RulesTests.cs` | `dotnet test` green |
| 5 Kit and wiring | the kit class, hazards, statuses, input, roster, loadout, lines, HUD, network | it compiles; the kit plays in a match (step 11's probe) |
| 6 Bodies | one cast clip per ability, first-person actions, any shared status clip | filmstrips reviewed side and front |
| 7 Effects | the family's rules written first, then each effect; props modelled like characters | filmed in a match |
| 8 Sound | one recipe per cue, timed to the frames | heard in the film's mp4 |
| 9 Ultimate cutscene | direction written first (one sentence, three shots, a beat table), then built | filmed on the caster's screen, clock measured |
| 10 Bots | the AI uses every ability the way a player would | measured in a match |
| 11 Film and review | mp4s sent in chat; each verdict recorded in `direction.md` with the owner's words | the owner says it reads |
| 12 Verify and ship | Core, EditMode, the PlayMode gate, `Checks.RunAll`, audits, a build, TODO and docs updated | pushed |

---

## 2 · Research: how, and what to extract

**Look at footage, not only text.** The 2026-09-24 ultimate research could not play video and said so; Paete's 2026-09-27 research
stepped through frames. The technique that works on this machine, in the built-in browser:

1. Find a compilation with chapters (for Genshin: "All 5 Star Characters Elemental Burst Animations", 56 chapters, one per character).
   Read the chapter list from the page (`ytd-macro-markers-list-item-renderer`) to get each character's start time.
2. Pin the `<video>` element full-window with a script, pause it, and seek it (`video.currentTime = t`, wait for `seeked`). Take a
   screenshot every 1.2 to 1.5 s through the chapter. The embed player is blocked; the watch page works.
3. Close the tab when done (`AGENTS.md`: close what you open).

**Extract rules, not looks.** For each reference write three columns: what it does, what TUMP takes, what TUMP does NOT take. Paete's
table (`direction.md` section 0) took Groot's burst-into-a-core and held bodies, Kinich's always-travels grapple, Scorpion's hold beat
before the yank, Dead by Daylight's struggle loop; it did NOT take Groot's realistic bark, Kinich's bright particle line, floating
bodies. The cutscene vocabulary (research section 4 of the ultimate performances report) is written the same way and is shared by
every hero: open on the element already in the air, the backdrop steps back, particles ride the motion, one emblem, a stroke for the
strike, vertical light for power, a near layer at the lens, a sound motif for the emblem.

**Ask the owner once, in one numbered batch** (plan section 6 had twelve questions). Record every answer with his words and what it
becomes in the kit. When he gives a number ("like 7 seconds", "WITHIN 9 meters"), it goes into the core with his quote on it and a
test asserting it (`PaeteRulesTests.TheOwnersNumbersAreTheOnesInTheKit`).

---

## 3 · The rules in core (numbers you can assert)

Every number lives in `Packages/com.tumbangpreso.core/Runtime/<Hero>Rules.cs`, engine-free, and is asserted by `dotnet test` in under
a second. Read `PaeteRules.cs` as the template:

- **Write a distance, solve the speed** (CLAUDE.md section 4). A pull that must stop at the trunk is `SentryPullSpeedFor(distance) =
  min(15, sqrt(2 * Friction * (distance - hold)))`, because a fixed 15 m/s slides 15^2 / (2 x 30) = 3.75 m and flung near bodies past
  the tree (found by the first match film). The test sweeps 2 to 9 m and asserts every body stops at the hold distance.
- **Owner quotes on the constants.** `SentryRadius = 9 // "WITHIN 9 meters"`. Proposed numbers say "PROPOSED, NOT OBJECTED TO".
- **Geometry that gameplay depends on is measured, then written down.** The v9 tree's root reach (2.14 m) and shin radius (1.17 m)
  were measured with `python tools/build_paete_props.py --measure sentry 1.3`, and `SentryCanClearance` (2.4) and `SentryHoldDistance` (1.4) are sized from those
  measurements with the arithmetic in their comments.
- **Anything every peer must agree on is a pure function in core**, so the host, every client and every bot compute it from the
  same inputs (`SentrySpotClearOfCan`: pushed straight out from the can, toward the caster if aimed dead on, round the can in a fixed
  order against a wall). Continuous in its inputs, so two peers with a can a few centimetres apart disagree by centimetres.

---

## 4 · Every place a new hero touches (Paete's commits, as a list)

This is the checklist that stops a kit from shipping half-wired. `plan.md` section 3 is the original; this is what was actually
touched. Follow how the previous hero (Amihan, then Paete) did each one.

- **Core**: `<Hero>Rules.cs`; `StatusRules.cs` for a new status (append, never renumber; Paete added `Rooted`); `MatchRules.cs` for a
  new score event (`ScoreEvent.SproutKnock`, +50 through `MatchDirector.AddScore`); `Roster.cs` (append the row); `HeroLoadout.cs`
  (two rows per slot); `HeroLines.cs` (text only); `AiTuning.cs`.
- **Kit**: `Runtime/Abilities/<Hero>HeroKit.cs` (the four abilities, each with id, display name, one-sentence description, summary,
  glyph, telegraph radius and range, `castAction`, `viewmodelAction`, `castCue`); `Runtime/Abilities/<Hero>Hazards.cs` (the world
  objects; host-only resolution behind `NetAuthority.ShouldResolve()`); `HeroAbilitySystem`/`HeroKit` factory.
- **Input**: a new verb needs its pad binding and thumb target or it will not compile (`InputCatalogue`, CS8509 is an error); run
  `InputAssetSync.Regenerate`. Paete added INTERACT (hold to pull a plant, hold to break out of roots).
- **Status**: `CharacterMotor.Status.cs`, `StatusStack.cs`, `StatusIcons.cs`, `StatusOverhead.cs`, the camera (`CameraRig` forced
  third person while Rooted), the shared animation (`RootedAnimationAuthor`: `rooted-struggle`, `plant-heave`, `root-breakout` on
  every rig).
- **Network**: `MatchRpc` (appended messages only), `NetSession.ProtocolVersion` bumped with its test (`ChatAndLobbyChromeTests`)
  whenever the match FORMAT changes, including when no byte changes but every peer computes a rule differently (Paete's 57, the
  cutscene length; 58, the spot kept off the can and the 1.4 m hold),
  `WorldEffectSnapshot` kinds appended for a rejoiner (Plant, Thorns, Sentry), `RecordedFieldView` for replay (render-only bodies
  marked `Staged`), `MatchReplayArchive`'s hero list.
- **Presentation**: `CharacterAnimator` action map, `HeroAbilityClips.<Hero>.cs` baked by `<Hero>MotionAuthor`, `ViewmodelArms`
  first-person actions, `HeroGlyphs`, `AbilityIcons`, `tools/build_ability_icons.py`, `Hud.<Hero>.cs` for any hero-specific card,
  `UiTheme` accent, `SkyEvent` look for the ultimate, portrait (`TumpPortraitAuthor`) and avatar (`tools/build_avatars.py`), roster
  book bake (`RosterBookBuilder.RefreshPersonFromCommandLine -person <id>`), FPP arms.
- **Audio**: `tools/build_<hero>_audio.py`, `AudioCues` wiring; the cutscene theme is one of the cues.
- **Cutscene**: `tools/author_ultimate_intros.py` (`<hero>()` body and shots), `Resources/UltimateIntros/<hero>.txt` (generated),
  `HeroIntroductionScene.<Hero>.cs` (+ a second partial for effects if it grows), `tools/intro_stage_sketch.py` (the storyboard).
- **Bots**: `AIController`, with the rules for when each ability is worth it.
- **Tests**: `<Hero>RulesTests`, a play probe (`PaeteKitPlayProbe`: real input on a real map), a rejoin probe
  (`PaeteWorldSnapshotProbe`), review probes that film (`PaeteReviewProbe`, `PaeteSpiritReviewProbe`), and any EditMode test that
  enumerates heroes. Place new PlayMode fixtures in `tools/playmode_suite.py`'s partition or `--plan` refuses to run.
- **Docs**: `docs/TODO.md` entry, `docs/HUMAN.md` voice rows, `docs/CHARACTER_ORIGINS.md`, `LORE.md`, `character-stories.json`.
- **Services**: cloud-code hero lists need the owner's UGS deploy; say so in the TODO rather than pretending it is done.

---

## 5 · Each ability: the six beats, then body, effect and sound per beat

Every ability is directed as a table before any code (`direction.md` section 1), one row per beat, one column per layer:

| Beat | Time | Body (everyone sees) | First person (his screen) | Effect | Sound |
|---|---|---|---|---|---|
| Tell | the wind-up the other players read | | | | |
| Release | the commit | | | | |
| Travel | it crosses the court | | | | |
| Contact | it lands, catches, hits | | | | |
| Linger | it holds | | | | |
| Dissipate | it ends by withering, never by fading | | | | |

**Paete's four, as built** (ids, and where each lives):

| Ability (id) | What it does | The idea in one line | Body clip / FPP action | Effects | Sounds |
|---|---|---|---|---|---|
| LIANA LEAP (`paete_skill1`), signature | vines out of both forearms to a wall, prop or the floor within 8 m; reeled in at 14 m/s, 0.8 m short | "the vines drag the tree": the vines ARE his arms' strands unravelling | `hero-paete-vine` / `vine-reach` | entangled bark limbs with dark vines and forked twigs (`GrowthTwigs`), a six-leaf rosette at the anchor | `sfx_cast_paete_vine` (leaf shiver, two plucked fibres, whip, wood knock, rising creak, leaf rush) |
| BAKYA BLOOM (`paete_skill2`), attacking | plants a Makiling pitcher plant up to 6 m; it grows a wooden clog (bakya) every 15 s and fires it at his aim on the second press; +50 on a can knockdown; untouchable 15 s, then any opponent can pull it out (hold INTERACT 1.2 s); withers at 40 s | "the gardener plants it", then "the command" | `hero-paete-sprout`, `hero-paete-command` / `seed-toss`, `seed-command` | its own species: a lime pitcher with a wine lip and a lid that opens as the clog ripens and rises out of the mouth, spits with a head-back recoil, dries to straw in four palette steps | `sfx_cast_paete_sprout`, `sfx_paete_sprout_land`, `_ready`, `_fire`, `_uproot`, `sfx_cast_paete_command` |
| THORN HARVEST (`paete_skill2d`), defending | stamps; every slipper within 7 m (loose, flying, even in hands) is caught, held 0.25 s, yanked to 1 m from him in 0.5 s | "stamp, then take it back" (the Scorpion hold before the yank) | `hero-paete-thorns` / `thorn-stamp` | its own species: an armed rattan that bursts up, fronds that whip, talon tips; never the can | `sfx_cast_paete_thorns`, `sfx_paete_thorn_burst` (a rasp whose teeth speed up, three whips, a scrape home) |
| MAKILING'S EMBRACE (`paete_ultimate`), 16 points | the cutscene; then, called from the ground, the guardian crawls out at his aim (never on the can) and drags everyone within 9 m to 1.4 m of it, Rooted (they can still throw; camera to third person; hold INTERACT 7 s to break out, progress kept; a tag frees them; it wilts at 10 s) | "her light, his ground, the mountain's answer" | `hero-paete-sentry` (a kneel with his hands in the court) / `ground-call` | roots race under the court as a ridge (`PaeteRootRidge`), the guardian crawls out in three hauls (`PaeteSentryBody`), woven limbs round each prisoner's waist and bands up their shins, bark shatter on break-out | `sfx_cast_paete_sentry`, `sfx_paete_root_vein`, `_sentry_burst`, `_sentry_heave` (x3), `_sentry_catch`, `_sentry_wake`, `_sentry_wilt`, `sfx_status_rooted`, `sfx_paete_root_break`, `sfx_sky_canopy` |

**The effect family's rules** (`direction.md` section 2), which made all of it read as one hero:
1. Solid, not glowing: vines, bark, thorns and leaves are toon-lit geometry in his palette. Only his eyes, his palms and the light glow.
2. Things GROW along their own path from their root; scaling a finished shape from zero is only for pop-ups, with an overshoot.
3. Everything ends by withering (curl, darken, drop leaves, sink). Never fade a solid by alpha.
4. Every catch has a hold beat before the pull.
5. Blocky and readable on Low; the can, slippers and players stay readable through all of it.
6. Hand-typed, part by part.

**Props are modelled like characters**, not built from cubes at runtime (owner: *"the current models of all his skills look ugly still
its js blocks"*). `tools/build_paete_props.py` types every part (cords, leaves, petals, roots) into glbs with named nodes for the parts
that move; `PaeteTreeBodies.cs` poses those nodes from the object's age. And each prop is its own SPECIES (owner: *"i wanted all his
sentries ... TO ALL look diff and distinct and have their own style"*): three silhouettes, three materials, three motion languages.

**Sound** (`tools/build_paete_audio.py`, header): every cue has a TRANSIENT, a BODY in the ability's own texture and a TAIL; the
hero's element is a family of instruments, and no two abilities share a recipe. His instruments are named where they are defined:
`creak` (stick-slip, the root groan), `snap` (bark breaking), `knock` (a struck wooden bar, the carving town's voice), `rustle` (leaf
contacts as grains, not hiss), `pluck` (Karplus-Strong, a vine let go), `rasp` (a thorn dragged), and for light only `glide`; plus
`whoosh` and the cutscene motif `bell`. numpy only, seeded, so a rebuild writes identical bytes; `docs/reports/<hero>-kit/…-audio.json`
records peak, RMS and hash. Time each cue to the frame its picture happens on, and check the loudness curve against the previous
version (a 0.1 s RMS table) so new layers support the beats instead of burying them.

---

## 6 · The ultimate's cutscene

**The contract** (it is shared plumbing; read `HeroIntroductionScene.cs` and `UltimatePhaseView.cs`): the phase pauses the world
(`Time.timeScale` 0) and the round clock (`PresentationClock`), renders the caster's copy and the scene through its own camera onto
an overlay, and lasts the longest accepted caster's length. So: **nothing in the cutscene may run on `Update`**; every piece is posed
from the scene clock in `Sample<Hero>(t)`; flat effects sit on `Slipper.GroundY`; new fields get names unique across all the
`HeroIntroductionScene.*` partials; the live ability starts from the cutscene's last pose so the hand-back does not jump.

**Direct it before you build it.** Paete's v3 had five cuts in 4.6 s, each carrying a different idea, and the owner said *"ur
direction of the entire cutscene sucks"*. What fixed it (`direction.md` 5.12 to 5.15):
1. **One sentence of meaning.** "Her power comes down into him, he gives it to the ground, and the ground answers with the guardian."
2. **Three shots, one idea each** (CALL, ROOT, RISE), each with a camera move that serves that idea only.
3. **One thing travelling through all of it** (the light: her hands, his hand, his eyes, down his arms, under the court, up as the
   guardian's eyes), always travelling the same way across the screen (left to right), so every cut continues the one before.
4. **A beat table** (time, picture, sound, reference it answers). Write the times first; everything hangs on them.
5. **Faster where it is setup, held where it is the point, slower where he asked for slow**, and say why in the doc.
6. **Storyboard outside Unity**: `python tools/author_ultimate_intros.py --preview <hero>` renders the real posed glb from the real
   shots with a sketch of the stage (`tools/intro_stage_sketch.py`); fix framing there before the first Unity film.

**The effects pass** (v6, the owner's Genshin references; research section 4 of the ultimate performances report):

| Principle | Paete's v6 |
|---|---|
| Open on the element already in the air | frame 0 is a gust of 22 leaves and petals, five at the lens; three spiralling ribbons of her jade light draw a column round her and the leaves ride it; it bursts as she forms. At their first typed size the leaves were six specks at five metres (film r16): judge sizes at the camera's real distance |
| The backdrop steps back | `HeroIntroductionScene.GradeAt`: the phase camera dims to 0.68 and desaturates to 0.78, held through the roots and hauls, back to 1 for the payoff (0.76 was measured at only 12 per cent on the sky and did not read) |
| Particles ride the motion | petals spiral round her falling light; leaves orbit him and ride out with his roots; each haul spirals leaves up the trunk |
| One emblem, drawn where the power moves | the mark of the mountain (three leaves in a whorl in two rings) at 0.78 in the air behind the top of his head, 1.22 on the court under his palms, 2.75 under the spot, 4.5 wide on the court at the guardian's foot. Put an emblem where the body cannot hide its middle: behind his neck and behind the trunk only its rings showed, and behind the tree's face it read as a target |
| Vertical light is power | streaks up out of the court at each heartbeat (2, 3, 4, then 3 on the send); spears of light at the spot |
| A stroke for the strike | a brush stroke painted along the court to the spot at the send, wiped from its start: a translucent lime body with the additive light down its middle. Additive light alone clips toward white on a bright court and disappears (film r16) |
| A near layer | leaves and petals drifting past the lens in every shot, placed on the lens line as a share of eye-to-focus so a push-in never puts them behind the lens |
| Saturated hero colour, never white | his lime and her jade only |

**Its sound is one theme file timed beat for beat** (`sfx_ult_theme_paete`), with a cut-in accent on frame 0, a swell drawn in before
each impact and cut dead on it, the impact in his own material, the emblem's motif (`bell`) every time the mark is drawn, and a tail.

**Film it on the caster's screen in a match** (`PaeteKitPlayProbe.FilmTheUltimateOnHisScreen`): his screen with the cutscene overlay
copied in, the court, a caught player; `Time.captureFramerate` 30 plus `SharedUltimatePhase.FilmClock` so the cutscene lasts its
real length; every world cue logged with its film time and mixed into the mp4; the round clock read before, during and after and
asserted frozen.

---

## 7 · The review loop, and every refinement round (what he rejected, and what replaced it)

**The loop:** build, film in a match (`TUMP_PAETE_FILM=1`, `TUMP_EVIDENCE=<dir>`), review the frames yourself as contact sheets, fix
what you see, film again, then `python tools/stitch_ability_film.py <film dir> <out.mp4> --title ... --view owner "HIS SCREEN" --view
wide "THE COURT" --view victim "A CAUGHT PLAYER"`, version the filename (`_v5`, `_v6`: chat clients cache by name) and send it. Record
his verdict in `direction.md` with his exact words, then act on it.

**Measure what "distracting" means.** On 2026-09-27 the tree was judged by sampling pixels off a film frame: its crown rendered
(158, 228, 44) against the plaza trees' (97, 124, 71), nearly twice as bright and far more saturated. That number is what the fix
aimed at.

| Round | The owner said | What was wrong | What replaced it |
|---|---|---|---|
| Model | laughed off carved faces and brows; *"Js make 2 fucking holles"* | a face that acted | two engraved hollows with a slanted light |
| Vines | *"why do they just twirl there for no reason"*, *"make it look likle this"* (a Groot crop) | even corkscrews | vines that climb from somewhere to somewhere, threading a woven rope |
| Props | *"its js blocks"* | runtime cubes | modelled glbs, every part typed |
| Plants | *"do all his sentries look the same?"*, of the rattan *"a flimsy plant"* | all three wore the ultimate's bark | three species; the rattan re-armed |
| Size | *"make tree bigger"*, *"REALLY big and imposing"* | a 4 m tree pulling people | 9 m (v8) |
| Tree in play | *"it sucks"*, *"make it look like the roots GO INT he ground"*, *"actually TIED"*, *"animate taht shit"* | roots lying on the court; prisoners standing | roots diving in with heaves of soil; woven limbs and shin bands; a straining loop |
| Spirit | *"make her look see thru"*, *"how she looks needs to be refined"*, *"why is there a deer"* | a translucent box; an unasked-for deer | a baro't saya ghost, typed; the deer cut; her brief full form on request |
| Direction | *"ur direction of the entire cutscene sucks"* | five cuts, five ideas | one sentence, three shots, one travelling light |
| The throw | *"i dont want him to be throwing an orb I want him to be CALLING IT FROM THE GROUND"*, *"i also dont like that paete just throws seeds"* | a thrown seed, in the cutscene AND in the live cast | the kneel, roots into the court, the channel glow, a root ridge; nothing thrown anywhere |
| Growth | *"i also dotn want the tree to jsut spawn in"* | a 0.5 s pop | it crawls out in three hauls |
| Length | *"dont go past 5 seconds"*, *"match time should pause"* | 4.6 s, clock unmeasured | 5.0 s, the clock asserted frozen |
| v5 verdict | *"Make the tre a bit smaller and a lot more sleek"*, *"dont let it be placed in a place it STANDS on can"*, *"add more special effects and vfx ... open it with leaves"* | a loud 9 m tree anywhere, a plain cutscene | v9 (five cords one way, muted greens), the can clearance, the v6 effects pass with its sound |
| v9 crown | *"remove the leaves ... it makes it look goofy"*, *"js pointy on the top with a glow coming from within"*, *"a few leaves at the edge of the top but dont put like a green blob"* (an Ent as the picture) | round leaf clouds on a spire read as a green blob | v10: a pointed spire of forked branches, a few leaves only at the tips, a light inside shining between the branches; 7.0 m |
| Looking round | *"also tree doesnt need to look left and right"*, *"lokks very weird"* | a 7 m tree swivelling its trunk from prisoner to prisoner every 2.6 s read as a turret | it holds still, facing along its roots' travel; its life is its breath, its blinks and its light |

The general lessons under those rows:
- **Ask of every render: does it belong in the cast, is it pleasing, is it great** (CHARACTER_MODEL_METHOD section 0).
- **Say each idea once.** Most of his "sucks" were two ideas fighting in one frame (five cuts; eight crossing cords plus vines plus a
  sash plus moss). Cut until each shape says one thing.
- **"Bigger" and "less distracting" are not opposites.** Scale is not the lever; value, saturation and clutter are.
- **Big things should move little.** A 7 m tree that turns to look reads as machinery; its life belongs in small motions ON it (its
  vines crawling up it, breath,
  blinks, light), the same finding as the title screen's weather (nothing large travels; small correlated motions).
- **A blob of foliage reads as goofy on a blocky character.** Silhouette by line (branches, twigs, a point), with a few small
  details at the edges, and light for the magic, reads as a creature; a mass reads as a prop.
- **The live cast must tell the same story as the cutscene.** v4's cutscene stopped throwing but the live cast still threw a seed;
  the owner saw the live one.

---

## 8 · Traps that cost time on Paete (technical; each one bit once)

- **glTFast negates X on import.** Any prop laid out by position must be written mirrored (`meadow()`'s `mx`), or it lands on the
  wrong side (v1 of the meadow did).
- **`RosterBookBuilder.RefreshPersonFromCommandLine -person <id>` re-bakes `RosterArms/<id>_*.asset` without the tangent channel the ink
  reads.** Restore them from HEAD after any refresh.
- **Regenerating the intro table rewrites every hero's file.** `git checkout` all the others back. And check the regenerated file for
  THIS hero: Paete's committed `paete.txt` had drifted from its source (the ROOT shot), so v5 shipped a framing the review had
  rejected. The source table is the truth.
- **`Mathf.Pow(Mathf.Sin(PI), x)` is NaN** (sin of pi is a hair below zero). It stalled a whole film. Clamp with `Mathf.Max(0, …)`.
- **The world is paused under the cutscene**: anything on `Update` in the cutscene freezes; anything that spawns into the world from a
  staged body (ground breaks, leaf bursts) must be switched off (`Staged`), including in replay (`RecordedFieldView`).
- **A restored age is already past the flight**: `PaeteSentry.Spawn` subtracted the seed's flight from a rejoiner's age and put their
  tree 0.45 s behind everyone's. Write a rejoin probe (`PaeteWorldSnapshotProbe`) for every world object you add.
- **Flat effects must sit on `Slipper.GroundY`**; v4's veins were under the plaza's court surface and never drew.
- **Mesh winding**: runtime tubes and leaves were drawn inside out (solid ink) until `PaeteInk` flipped them. And a two-sided runtime
  mesh must give each side its own vertices: shared, `RecalculateNormals` cancels them to zero and a lit material renders a white slab
  with black dashes (Paete's brush stroke, film r17; `VfxShapes.TwoSided` is the same fix).
- **A `MaterialPropertyBlock` overrides `_Color` on every submesh** (`docs/TODO.md` section 87's lesson): when a surface is one flat colour,
  ask which renderer carries a property block.
- **Bash heredocs eat backslashes and quotes on this machine**: write edit scripts to files. Never round-trip a UTF-8 source through
  PowerShell 5.1.
- **Unity only through `python tools/run_unity_guarded.py -batchmode -tp-profile presentation-validation-20260921 …`, in the
  background; never edit `.cs` while it runs; PlayMode never takes `-nographics`; assert on the test XML, not the exit code.** If you
  must stop a run, stop only that worktree's `Unity.exe` and delete `Temp/UnityLockfile` after.
- **Editor films run at 7 to 17 fps**: use `Time.captureFramerate` and the film clock, or the cutscene reads as slow motion.
- **Measure a portrait against the cast, and know what the knob does.** Paete's sat 45 px lower than all 36 others (measured off the
  PNG alpha), and his "closer" zoom had never done anything: `ModelPreview.ZoomMin` (0.55) clamps it. Aim height is the lever.
- **`-executeMethod` without `-quit` never exits** unless the method calls `EditorApplication.Exit`; a chain behind it waits for ever.

---

## 9 · The checklist for the next hero (copy it into the hero's TODO entry)

- [ ] Brief and lore (`ArtSource/<hero>/…/design-brief.md`), research from footage (`docs/reports/<hero>-kit-<date>/research.md`)
- [ ] Plan with the kit mechanically, the six-beat table per ability, the file list, ONE batch of questions; answers recorded
- [ ] Model per `CHARACTER_MODEL_METHOD.md`; owner's verdict
- [ ] `<Hero>Rules.cs` + tests: owner numbers quoted, distances solved against `Friction`, measured geometry written down
- [ ] Kit, hazards, status, input (pad + thumb), network (protocol bump + test), snapshot kinds + a rejoin probe, replay `Staged`
- [ ] Roster, loadout, lines, glyphs, icons, HUD card, portrait (framing measured against the cast), avatar, FPP arms, bake
- [ ] One body clip and one first-person action per ability; filmstrips reviewed
- [ ] The effect family's rules written; every effect grows from something and withers into something; props modelled and typed
- [ ] One sound recipe per cue, transient / body / tail, timed to frames, loudness checked against the previous version
- [ ] Cutscene: one sentence, three shots, one travelling thing, a beat table, the effects vocabulary, the grade, the theme; storyboard
      with `--preview`; at most 5.0 s; nothing on `Update`
- [ ] Bots use every ability; measured in a match
- [ ] Films in a match (his screen, the court, a caught player); mp4 versioned and sent; verdict recorded and acted on
- [ ] Core, EditMode, `python tools/playmode_suite.py --gate`, `Checks.RunAll`, the audits, a build in `Builds/<name>/`
- [ ] TODO entry, `direction.md`, this file if the method changed; pushed
