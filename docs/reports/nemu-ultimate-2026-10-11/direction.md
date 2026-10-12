# Nemu ultimate: PRUSISYON (direction, 2026-10-11)

Research: [research.md](research.md). Method: `docs/HERO_KIT_METHOD.md`. Stable IDs are kept:
`nemu`, `nemu_ultimate`, `AbilityGlyph.NemuSeanceVoid`. This replaces KURO: HAUNT! and the unpitched
LIGHTS OUT cutscene that arrived with QoLUpdates. Nemu's three basic skills are unchanged; they are
future work. Kuro's model, his first-person sleeve view and his basic-skill jobs are unchanged.

## 0. One sentence

**Nemu hushes the world grey, and the dead she is always half-listening to file past in procession
and step into every player.**

## 1. The kit concept: a family of ghosts (Gekko rule)

Each ghost is one character with one silhouette and one job. They share one accent: the light they
carry (candle amber). Kuro is still the pet and still does the basic skills. The procession is the
ultimate's cast.

| Ghost | Silhouette | Job | Where |
|---|---|---|---|
| Kuro | the small round shadow with a fading tail | her pet: sit, fetch, catch | basics (unchanged) |
| Lola | hunched, shawl (alampay) over the head, rosary swinging | leads the procession, sets the pace | cutscene |
| Sakristan | small, square cassock, a hand bell held high | rings the beat the dead walk to | cutscene |
| Kandila souls | child-sized sheet ghosts, two stubby hands round a candle | the possessors: one rides each player | cutscene and play |
| White Lady | very tall, faceless veil, long hair, no feet | the scale of the parade; looks at the lens last | cutscene |
| Paru-paro | black moths | the souls' messengers; the near layer at the lens and round the possessed | cutscene and play |

Owner, 2026-10-11: *"i dont want ghosts to be too blocky kahit translucent sila or like not real
models"*. So no ghost is a stack of cubes: each is a smooth surface turned from its own hand-typed
profile (`Visual.ProcessionShapes`: a domed hood pinching at the neck, a sheet that flares and hangs,
a hem that ripples unevenly), drawn as light by its own shader (`Shaders/ProcessionGhost`: only the
front surface shows, the body veils what is behind it, the rim glows, the hem dissolves into mist).
Faces stay the cast's: two ink eyes, set unevenly. Bodies are pale lilac grey, never pure white;
eyes are her violet; flames are candle amber.

## 2. Mechanics (numbers live in `Core.NemuRules`; every one is asserted by `Core.Tests`)

**PRUSISYON** (`nemu_ultimate`, 15 points, unchanged cost). After the cutscene every other player is
**POSSESSED** for 15 s of round time (owner's number). Nemu is never possessed by her own cast.

### 2.1 The contest (the Jockey rule)

The ghost does not take the stick. It adds its own pull to whatever the player asks for, and the sum
is clamped to a full walk:

`wish = clamp1(input + grip x pull(t) x lure(t))`

| Phase | Pull | What it feels like |
|---|---|---|
| Settle, first 0.6 s | ramps 0 to 0.42 | the rider finds its seat (the cutscene already showed it entering) |
| Drift | 0.42 | hands-off you walk at 42 % where it wants; fighting it straight on you still make 58 % of your speed |
| Tell, 0.5 s before each surge | 0.20 | it inhales: the pull slackens, the rider rears and its candle flares. The read |
| Surge, 0.75 s | 1.05 | it lunges: hands-off you are walked at full speed; fighting straight on you are HELD (5 % backward), never thrown |
| Leave, last 1.2 s | ramps to 0 | it climbs out |

Six surges per possession, typed per seat so four victims never lurch on the same frame.

**Grip** (the skill expression): starts at 1.0. While the player pushes against the pull (input
at least half a stick and pointing more than 110 degrees away from it) grip falls 0.12 per second, to
a floor of 0.45. Otherwise it recovers 0.05 per second. A player who fights the whole time ends
with a ghost that pulls at less than half strength: steady fighting nets 81 % speed and a surge nets
53 %. Grip is local to the body's own simulating peer (like the stick itself) and never decides an
outcome on the host.

### 2.2 Where it pulls (the lure, the caster interaction)

The ghosts always pull toward the worst place for their host, and that is always good for Nemu:

- **An attacker** is pulled toward the taya. If Nemu is the taya, her prey walks to her; if she is
  attacking, her rivals walk into the tag.
- **The taya** is pulled away from the can. If Nemu is attacking, the can is left open for her.
- No taya on the court: toward the can.

The direction sways up to 28 degrees round the lure (a slow seeded sine), and straightens during a
surge. Everything is a pure function of seat, elapsed status time and replicated positions
(`NemuRules.PossessionPull`), so every peer draws the rider leaning the same way and the victim can
predict it.

### 2.3 What it never does (fairness)

- Never a stun: throwing, picking up, skills, jumping, shoving, sprinting and stamina all work.
- Never the camera: the player's view is theirs; only the picture is treated.
- Never a takeover: at full grip, pushing against a surge holds you in place.
- Never stacking: a second cast refreshes to 15 s (`Max`), never adds.
- Stunned, tagged, frozen, rooted or feared bodies feel no pull (the existing status wins).
- Removable by cleanse and refused by status immunity (Geo shield and Carapace are counterplay).
- Round end, reset and rejoin clear it like every other status.

### 2.4 Bots

Bots press the same buttons, so the same contest applies to them and their goal-seeking already
pushes against the pull. Nemu's bot casts PRUSISYON when at least two opponents are on court and the
round has more than eight seconds left.

## 3. Network

- `StatusKind.Possessed = 14`, appended. `SyncUnit` appends `PossessedLeft` after `ZappedLeft`
  (bounded 0 to 15, finite). The protocol is bumped because the wire and the ultimate's semantics
  change. Peers with the Haunt cannot join.
- The host applies POSSESSED to every eligible body when the shared ultimate activates. Clients only
  receive it. The body's own peer computes the pull from the replicated clock and positions.
- The old Haunt chase is removed. `FamiliarEffect` stays on the wire for compatibility and is
  ignored for the ultimate (`ReceiveHaunt` returns false; Kuro never devours).
- Replay: statuses already ride the recorded unit state where present; the rider is presentation
  read from `IsPossessed`, so a replay that carries the status draws it.

## 4. The cutscene: 5.8 s, three shots, one travelling light (owner: *"below 6 seconds"*)

The travelling thing is **candlelight**, moving left to right across the screen in every shot.

| Time | Shot | Picture | Sound | Reference |
|---|---|---|---|---|
| 0.00 | A THE HUSH (close, straight on, slow push in) | a black moth crosses the lens left to right; Nemu is gazing off, Kuro bobbing at her shoulder | room tone | Castorice moths, open on the element |
| 0.15 | A | her eyes come round to the lens | | her canon look |
| 0.40 | A | finger to her lips: *shh* (punch) | a breath | Arlecchino's hush |
| 0.50 | A | THE GREY: a ring of grey sweeps out from her feet over the court and up the walls; the grade drains to 18 % saturation | everything ducks to near silence | Acheron's monochrome |
| 0.55 | A | | one agunyas toll | the motif |
| 0.70 | A | Kuro flinches and hides behind her head | | character beat: even her ghost is scared |
| 0.85 | A | a tiny candle flame kindles at her fingertip | a match hiss | the accent arrives |
| 1.05 | A | the flame drifts off screen right | | the travelling light starts |
| 1.20 | B THE PROCESSION (wide, low, trucking left to right) | the flame lands in Lola's candle; the dead rise out of a grey mist bank on the left and file across behind Nemu, in step | footsteps without feet: a soft shuffle on the beat | Pom Poko lockstep, prusisyon |
| 1.55, 2.05, 2.55 | B | the Sakristan's bell rings; every candle bobs on the beat | hand bell | |
| 1.3 to 2.9 | B | the White Lady glides at the back, a head above the line | a low choir hum swells | scale contrast |
| 2.95 | B | EVERY head turns to the lens at once; every flame flares (punch) | the hum cuts dead | Acheron's held beat |
| 3.30 | C THE ENTERING (one continuous move) | every candle snuffs together; a smoke thread rises off each; near dark, only violet eyes | a single breath out | |
| 3.42 | C | the souls burst into amber-tailed wisps streaming at the real targets; the camera rides the lead stream out from behind Nemu | rushing whispers, panned with the stream | Paete's TAKE rule: follow it to the people |
| 4.15 | C | the wisps hit: every target jolts (shock clip), a puff of moths bursts off each | thumps, staggered | |
| 4.30 to 5.00 | C | a Kandila soul re-forms on each target's shoulders, hands over their eyes; heads loll; the camera swings wide | | the Jockey silhouette |
| 5.25 | C | every rider's candle relights at once; every target's eyes glint amber | the toll again, softer | the motif |
| 5.55 to 5.80 | C | ink closes in from the frame edges to black | | hand-back out of the dark |

Nobody else on court: the wisps dive into the court at three typed spots and no rider is shown. No
target is invented. Play picks up with the riders already seated (never shown twice).

Grade: 1/1 until 0.45; to brightness 0.80, saturation 0.18 by 1.0; a dip to 0.45 at the snuff; back
to 0.80/0.18; the iris finishes it.

## 5. In play: VFX that says what is happening

| Question a player has | Answer on screen | Who sees it |
|---|---|---|
| Who is possessed? | a Kandila soul riding their shoulders, hands over their eyes, two black moths circling them, the POSSESSED icon overhead | everyone |
| Which way are they being dragged? | the rider leans and reaches that way; a short trail of faint grey footprints appears on the court ahead of them in the pull direction | everyone |
| Which way am I being dragged? | ghost hands reach in from THAT edge of my screen | the victim |
| Is a surge coming? | the rider rears back and its candle flares; the footprints brighten; my screen's hands tighten; an inhaled whisper | everyone (the whisper is the victim's) |
| Is it surging now? | the rider thrusts its arms forward with a moth streak; the hands yank; a breath push and a heartbeat | everyone / victim |
| Am I winning? | as grip falls, the rider flickers and its candle gutters; my screen's hands slip back and fade | everyone / victim |
| When does it end? | the icon's timer; the last 1.2 s the rider climbs out, its candle snuffs and a smoke thread rises | everyone |

The victim's screen also goes grey (saturation 0.35, brightness 0.86) under a breathing ink
vignette. Reduced effects keeps every shape and drops flicker and flashes. Effects family rules:

1. Nothing appears from empty air: souls rise from the mist, re-form out of their own wisps, and
   leave by climbing out and snuffing.
2. One accent: candle amber. Her violet only in eyes. Bodies pale lilac grey. Never white.
3. Small on the court: a rider is a body's size; the footprint trail is 1.2 m. The can, chalk and
   every player stay readable.
4. Everything is posed from a clock (the scene clock in the cutscene, the status clock in play).

## 6. Sound (`tools/build_nemu_audio.py`, numpy, seeded)

Instruments: `toll` (agunyas bell, inharmonic partials, long tail), `bell` (hand bell), `hum` (a
low choir of detuned formants), `breath` (shaped noise in/out), `shuffle`, `whisper`, `snuff`,
`heart`. Cues: `sfx_ult_theme_nemu` (the cutscene, one file timed to section 4), `sfx_possess_enter`,
`sfx_possess_exit`, `sfx_nemu_possess_tell`, `sfx_nemu_possess_surge`, and the victim's loop
`sfx_nemu_possessed_loop`. No voice.

## 7. Files

Core: `NemuRules.cs` (+ tests), `StatusRules.cs` (append). Runtime: `NemuHeroKit.cs` (the ultimate),
`CharacterMotor.Status.cs`/`CharacterMotor.cs` (status and contest), `Net/MatchRpc.cs` and
`NetSession.cs` (wire), `UI/StatusIcons.cs`, `Visual/PossessedPresentation.cs` (rider, moths,
footprints), `Visual/PossessedScreen.cs` (victim HUD), `Visual/HeroIntroductionScene.Nemu.cs` (+ a
procession partial), `tools/author_ultimate_intros.py` (`nemu()`), `AIController.cs`, audio builder.
Docs: TODO entry, `SKILL_NETWORK_CONTRACT.md`, `Hero_Strike_Balance.md` row, this file.

## 8. Acceptance (what is evidence and what is not)

- `dotnet test` for every number and the pull function (resisting holds against a surge, hands-off
  drifts, grip floor, lure for taya/attacker/no-taya, deterministic across calls).
- A PlayMode check on a real map: cast, every other body POSSESSED for 15 s, Nemu not, a hands-off
  body drifts toward the lure, a resisting body holds, a stunned body does not move, cleanse clears.
- The cutscene sampled at every beat, its length below 6 s, the clock frozen.
- Film of the caster's screen and a possessed player's screen. Compilation is not acceptance.

## 9. Evidence so far

| Claim | Evidence | Level |
|---|---|---|
| Rules and owner numbers | `dotnet test Core.Tests` 742/742 (`NemuRulesTests` 11) | managed tests |
| It compiles in Unity | EditMode `ChatAndLobbyChromeTests` 10/10, protocol 160 | Unity compile and EditMode |
| Who is possessed, by whom, cleanse, wire bounds, stray Haunt receipts | PlayMode `NemuPrusisyonTests`, `NemuFamiliarPoseTests` | native PlayMode, [XML](evidence/play-possession.xml) |
| The contest moves real bodies on Eskinita | `NemuPossessionMovementTests` (with the 10 % slow): hands-off 0.64 m toward the taya in 1 s; fighting it 1.63 m of a free 2.50 m; stunned 0.00 m ([CSV](evidence/possession-movement.csv)) | native PlayMode, real physics steps |
| The cutscene in a match | `NemuPrusisyonFilm` on Bayan Plaza at 30 frames per game second: 5.80 s, round clock frozen (85.141 to 85.141); bots playing after the hand-back wear the grip to about 0.67 ([summary](evidence/film-v1/summary.txt), [timing](evidence/film-v1/timing.csv), [frames](evidence/film-v1/r7.jpg)) | native film, preview v1 |

Not yet evidence: actual networked peers, the owner's verdict on the film and the draft sound, human taste.

### Film review rounds (what each film showed and what changed)

| Film | Seen | Changed |
|---|---|---|
| n1 | the dead invisible against the dome; their eyes sub-pixel; souls white blobs; the payoff wide and small; the dome faded to the plain plaza before the end | brighter edges, bigger figures and eyes, violet souls, the camera stays with the first player, the dark holds to the end |
| n2 | the whole-frame grade at 0.14 turned violet to white glass; most of the line rose too late; Nemu blocked the procession | saturation 0.38, the mist edge at 7.2 m, the procession shot from her right-front |
| n3 to n5 | eyes beside the face; the ink only dimmed (a linear frame) and put the eyes out | eyes along the line to her from the head bone; the ink pass keeps bright violet and goes fully black |
| n6 | the procession lilac glass (the rim covered small bodies); her offering hand filled the lens | denser darker veils with a quiet rim; the offer kept low |


## 10. Owner corrections, 2026-10-11 (these supersede the sections above where they differ)

| The owner said | What it became |
|---|---|
| *"i dont want ghost to be above the person like a jockey, i js wanted u to mimic the jockey's steering effect"* | No rider. The Jockey is only the steering contest (section 2.1, unchanged). |
| *"the ghosts will all go INSIDE the ppl and theyre gonna look possessed"*, *"some sort of purple eerie aura"* | `Visual.PossessedPresence`: a soul's sheet sized just larger than their body, mostly hidden by it, so a dark aura in a ghost's shape glows round their silhouette; violet light over their eyes; it sinks into them as it starts and climbs out as it ends. |
| *"darker purple"*, *"ang weird ng hue of that purple"*, *"use black and purple or low opacity black"* | A smoky near-black veil (it darkens what is behind it) with a deep violet edge; the procession's flames are ghost-fire violet, not candle amber. |
| *"the ghosts are tryna get out of the body but they get sucked in anyways"* | Between surges the trapped soul pushes a hand out through the body and is sucked back (typed escape rows); each surge it lunges half out toward the lure, dragging them, and is snapped back in. In the cutscene each soul wheels round its player fighting, then is sucked in. |
| *"maybe possessed players get 10% slow too"* | `NemuRules.PossessedSpeedScale` 0.9 on the status (`StatusRules`, asserted). |
| *"make it look very eerie"*, *"below 6 seconds"* | 5.8 s; the world drains grey and dark under a death bell; a dark dome closes; the dead walk in lockstep and turn to the lens together; the players turn slowly to face her with violet eyes; the ink closes. |
| *"add a lot of cool vfx that communicates whatever is happening"* | Section 5's table, with the aura, eyes, escapes, lunges, moths and footprints. |

Sound: skill sounds are off in the game until the owner picks a draft by ear, and an unapproved one has no file in the
game. The drafts (`tools/build_nemu_ult_audio.py`) are in [audio-drafts](audio-drafts/) and mixed into the preview film;
the in-game tell and surge hooks are wired and silent until released. The possessed player's hearing is muffled
(not a skill sound; Haunted's filter, a little lighter).

## 11. LET THEM IN (2026-10-13, the owner rejected preview v1; this section supersedes 0, 1, 4 and 5)

Research round two: [research.md](research.md) section 6. Gameplay (section 2), network (3) and the victim's steering
contest are unchanged. Only the name, the cutscene and the look of a possessed player change.

### 11.1 The owner's answers (2026-10-13)

| Asked | The owner said | What it becomes |
|---|---|---|
| What sucks about v1 | *"everything about it sucks"*: the ghosts' look, the staging and camera, too many ideas, the possession payoff | A new direction, not a fix list |
| The procession | *"tangina nung procession cringe na"*, *"fuck that name and idea"* | No procession, no cast of named ghosts, no candles, no bell. The name is gone |
| The name | LET THEM IN (picked from SAPI, SANIB, LET THEM IN, HOLLOW) | The display name. The ID, glyph and cost stay `nemu_ultimate` |
| Ghost design | Smoky wraiths: low-opacity black smoke bodies, deep purple edges, violet eyes, not solid models | Section 11.3 |
| How many, from where | A flood from her shadow: it spreads over the court and dozens claw up out of it, then race to the players | Shot B |
| The ending | Players fighting it: the ghosts thrash half out of their bodies and are sucked back in, violet eyes snap on, hand back with no black-out | Shot C |
| The possessed look | *"i dont like hwo the ghosts js sit inside on the ppl"*, *"i wanted the ghost to go inside the ppl tapos may parang aura nalang sila that theyre nasasapian"*, *"HINDI UNG KITA UNG GHHOST IS IN THEM"* | After the entry no ghost is drawn on or in them. They are themselves plus an aura (section 11.6) |
| Pets | (Oct 11 brief) no pets | Kuro is not in the cutscene |

### 11.2 One sentence and one travelling thing

**Nemu stops holding the dead back: her shadow floods the court, the dead claw up out of it and pour into every player,
and all that is left to see is the aura they cannot shake.**

The travelling thing is **her shadow**: it leaves her feet in shot A, runs out over the court in B, and drains into the
players in C. It always moves away from her toward them, left to right on screen.

### 11.3 The look (rules, each from a reference or an owner quote)

1. **Black and one violet.** Smoke is near-black and alpha-blended, so it darkens what is behind it (*"low opacity
   black"*). The only light is one hot violet: eyes, the smoke's frayed edges, the ink's rim, embers. No candle amber, no
   white, no lilac. (Black Swan.)
2. **No grey grade.** The world dims (brightness to about 0.55) but keeps its colour (saturation about 0.85), and a
   low-opacity ink vignette breathes in from the frame edges. v1's saturation 0.38 is what turned violet to glass.
3. **The dead are smoke with a face and claws of light.** A wraith is a column of black smoke puffs: a head, shoulders,
   a body that tears off into a long tail, two arms that end in long thin clawed fingers. On the head: two slanted violet
   eye slits and a long open mouth of violet light, set unevenly. Nothing lathed, nothing solid, no sheet, no candle.
   (Fade: faces and claws in light inside a wall of black.)
4. **Many, at every scale.** About twenty, from just bigger than a player to twice their height; the nearest are big in
   the lens so the mass reads as a crowd, not a line. Each moves on its own typed row (never a stamp), but all of them
   turn together once (the one held beat).
5. **After the entry there is no ghost.** Possessed = the player's own body and colours, plus the aura (Viego). Only
   when a soul tries to get out does a CLAW show: an arm and hand of smoke pushing out of the body, yanked back in. Never
   a face, never a body sitting on or round them.
6. **Everything is posed from a clock** (the scene clock in the cutscene, the status clock in play), no `Random`;
   reduced effects keeps every shape and drops flicker and flashes.

### 11.4 The cutscene: 5.6 s, three shots (below 6 s; the hand-back starts at 5.2)

Faster where it is setup (A), held where it is the point (C). Nobody on the court: the dead dive into the ink at three
typed spots and it drains there.

| Time | Shot | Picture | Sound | From |
|---|---|---|---|---|
| 0.00 | A THE HUSH (close, three-quarter from her LEFT so the hushing right hand is on the far side of her face, slow push in) | already dark at the edges; two curls of black smoke drift across the lens; Nemu looking down and away | a low room drone | open on the element in the air |
| 0.15 | A | her eyes come up to the lens | | her canon look: already knows |
| 0.30 | A | finger to her lips, beside the mouth so the face stays clear (punch) | a whispered *shh* | Arlecchino |
| 0.42 | A | her eyes ignite violet; the frame dims and the ink closes in | everything ducks | Black Swan's one hot light |
| 0.55 | A | her shadow under her blackens and starts to spread, a violet hairline at its rim, smoke curling off the edge | a deep sub swell | the travelling thing starts |
| 0.85 | A | a clawed hand rises out of the ink beside her and closes | a wet crackle | the tell |
| 1.00 | A | she lets the hand fall open: go | | |
| 1.10 | B THE FLOOD (low behind her right shoulder, craning up and forward) | the ink races out over the court toward the players | rushing low wind | |
| 1.20 to 2.30 | B | the dead claw up out of the ink, front to back with the ink's edge: claws first, then heads (eyes ignite), then the body pulls free, writhing | whispers rising, a choir swell | Fade's wall, Black Swan's hands |
| 2.35 | B | every head snaps round to the lens at once; every eye flares (punch, held) | the swell cuts dead | the held beat |
| 2.55 | B | they launch: bodies stretch into long smoke tails, claws ahead | a shriek, panned with them | |
| 2.90 | C LET THEM IN (one continuous move) | the camera rides behind the lead pack out to the first player, ending beside them | | Paete's TAKE rule |
| 3.10 to 3.75 | C | they pour into every player through the chest, five or six each, staggered: each dive spirals its tail in after it like water down a drain, a burst of black smoke and a violet flash at the chest, the player jolts (shock clip) | thumps, one per entry | Viego's dive and burst |
| 3.80 to 4.70 | C | THEY FIGHT IT: a claw of smoke tears out of a back, a shoulder, a chest, stretches, and is yanked back in; the player jerks with each (struggle clip); their eyes snap violet (4.0) | groans under the breath, a snap per claw | the owner's *"tryna get out but they get sucked in"* |
| 4.20 | C | the ink drains into the players' feet; what is left on each is the aura (11.6) | a low settle | the travelling thing ends in them |
| 4.70 to 5.20 | C | the camera eases back to hold every player; they straighten, heads tipping toward the lens, eyes burning | one last whisper | |
| 5.20 to 5.60 | C | hold; the vignette loosens; hand back to play (no black-out) | the drone releases | |

Nemu's own poses: looking down, the eyes up, the hush beside the mouth, the hand falling open; in B and C she is out of
frame or a small silhouette at the edge. Play picks up with them possessed (the cutscene showed the entry; it is never
shown twice).

### 11.5 The parts, built

- `Shaders/NemuSmoke` (new): one shader for every puff and every light. Particle billboards, premultiplied: a smoke
  puff darkens what is behind it and its frayed rim glows violet; a light (eyes, mouth, claw tips, embers) only adds
  violet. Its noise runs on a clock the scene sets, so a paused phase still animates exactly with the scene clock.
- `Visual/NemuSmokeBatch` (new): a particle system laid out by hand each frame from typed rows (the way
  `VoodooSoulDraw` does), one draw for all the dead. Used by the cutscene and by the aura in play.
- `HeroIntroductionScene.Nemu.cs`: rewritten to section 11.4. Kuro, the procession, the candles, the bell, the moths,
  the dome and the black-out are removed. `Shaders/NemuInk` is now only the breathing ink at the frame's edges.
  `ProcessionShapes.Sole` and `Shaders/ProcessionGhost` stay for the footprints only.
- `tools/author_ultimate_intros.py` `nemu()`: new keys and shots, storyboarded with `--preview nemu` first.

### 11.6 In play: the aura, and what each piece says

| Question | Answer on screen | Who sees it |
|---|---|---|
| Who is possessed? | black smoke burning up off their silhouette like dark fire, its edges violet; a faint violet halo behind them that reads across the court; violet eyes; embers rising | everyone (not drawn in their own first person) |
| Which way are they dragged? | the smoke streams that way, like a wind only they are in; the existing faint footprints ahead of them | everyone |
| Is a surge coming? | the aura swells and darkens, embers quicken, the eyes flare | everyone |
| Is it surging? | a claw of smoke reaches out of them toward where it drags them, then is yanked back in when the surge ends | everyone |
| Is it trying to get out? | between surges, on the typed escape rows, a claw pushes out of a side and is sucked back with a puff | everyone |
| Am I winning? | as grip falls the aura thins and flickers | everyone |
| When does it end? | the last 1.2 s the smoke burns off upward and the eyes go out | everyone |

Cut from play: the ghost-shaped sheet, the procession hands, the moths. The POSSESSED status icon (a head ridden by a
sheet ghost) is redrawn as a head with violet eyes in a smoke aura. The victim's own screen keeps its job; its two
hands are restyled as smoke claws with violet edges if the owner agrees (they are the old glow-stick outlines).

### 11.7 Sound (drafts only, out of `Resources/Sfx` until the owner approves by ear)

`tools/build_nemu_ult_audio.py` is retimed to 11.4: drone, *shh*, the duck, the ink's sub swell, crackle, wind, whispers
and choir swelling, cut dead on the held beat, the shriek, one thump per entry, groans and snaps, the settle, a last
whisper, the release. The tell and surge cues stay as drafts.

### 11.8 Acceptance

Storyboard frames from `--preview nemu` reviewed for framing first; then `NemuPrusisyonFilm` (the class keeps its name;
only shown text changes) on Bayan Plaza: length below 6 s, round clock frozen, contact sheets reviewed at every beat
(are the dead readable as a crowd, is violet the only light, does the entry read, is no ghost visible on a possessed
player after it, can a claw be told from a body); then the stitched four-view preview video.

### 11.9 As built, and the film review rounds (2026-10-13)

The owner approved section 11 (*"i like it goated"*) and asked for sound too (*"try mo na ren gumawa sfx"*). Mid-build,
seeing a round-3 frame: *"i like the idea but these doesnnt read as ghosts anymroe it isnt ahhth well done"*. That changed
the dead from columns of puffs to one ghost silhouette each (row r5 below).

| Film | Seen | Changed |
|---|---|---|
| r1 | smoke read as glowing lavender cotton, each puff a violet ring; the close shot swallowed by smoke; the ride through her head; the aura hid the player; no eyes; the world bright; the aura fading in from zero after the hand-back | thin broken violet edge on black bodies; smoke kept below her chest in A; the camera kept off her; a smaller aura kept off the face; the world darker; the aura whole from the first frame |
| r2 | black now, but every puff still a closed violet ring (bubbles); no lights at all | the edge lights only where the noise runs high; lights drawn as plain billboards (stretched ones on a system that never simulates drew nothing) |
| r3 | eerie black smoke with ember edges; lights as dull lilac beads; the saturation cut turned violet lilac | darkness from brightness only (saturation 0.92); a hot glow profile; beads close enough to merge into strokes; A pulled back. **Owner: these do not read as ghosts** |
| r4 | hush framing good; lights too tight to see | (folded into r5) |
| r5 | ONE GHOST per wraith (`Shaders/NemuSmoke` mode 2: hood, body into a tattered swaying tail, raised hooked claws, slanted burning eyes, a long screaming mouth): the crowd reads as the dead; in flight they stretched into huge black birds | a small stretch, smaller as they close in, claws ahead |
| r6 to r8 | eyes still missing: the face measured with the 2.38 scale applied twice, and each light a few pixels | the baked skin's own scale; a wide glow; eyes nudged toward the lens and faded when the face turns away |
| r9, final | Nemu's slanted violet eyes in the hush; the crowd; the dive; the aura with its halo and escaping claws; the smoke claws on their own screen | the aura forced whole from the first possessed frame |

| Claim | Evidence | Level |
|---|---|---|
| The cutscene in a match | `NemuPrusisyonFilm` on Bayan Plaza at 30 frames per game second: 5.57 s, round clock frozen ([summary](evidence/film-v2/summary.txt), [timing](evidence/film-v2/timing.csv), [beats](evidence/film-v2/owner-beats.jpg)) | native film, preview v2 |
| The possessed look in play and on their own screen | [in play](evidence/film-v2/possessed-in-play.jpg), [their screen](evidence/film-v2/their-screen.jpg) | native film |
| Gameplay unchanged | PlayMode `NemuPrusisyonTests`, `NemuPossessionMovementTests`, `NemuFamiliarPoseTests` and the film, 11/11 ([XML](evidence/film-v2/play-tests.xml)) | native PlayMode |
| Sound | drafts rebuilt to 11.4; 0.1 s loudness peaks on the entries (0.29 to 0.32 RMS) over a 0.07 to 0.18 setup | numbers only; the owner's ear is the test |

Known: the aura shows about 0.3 s after the hand-back, when the ability's status lands in play (unchanged gameplay
timing). Not yet evidence: networked peers, the owner's verdict on v2 and on the sound.
