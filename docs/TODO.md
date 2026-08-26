# TODO: Tumbang Preso Unity

Open work, ordered by what is worth doing next. Each entry says what is wrong, where it lives,
and what "done" looks like, so nobody has to re-derive it.

**Check this before inventing a task, and update it in the same commit as the work.** Finished
items move to **Closed** at the bottom with one line on how they were verified.

Read [`VISION.md`](VISION.md) first if you have not. Several entries here only make sense
against the readability budget in its § 2.

---

## 0 · Hero Strike is being reworked, and the plan is its own file

**Numbered 0 rather than 1 on purpose: every other entry here keeps the number it already had,
because `VISION.md`, `CLAUDE.md` and two handoffs cite them.**

The measurements and the proposal are [`Hero_Strike_Balance.md`](Hero_Strike_Balance.md). Do not
copy them here; that is how § 1 came to be pointed at by `VISION.md` § 2 while holding something
else entirely.

**The three things it found that are facts rather than proposals**, all derived from the code
with no Unity launch:

1. ⚠️⚠️ **Ilalim ng Tulay renders black, and it is one wrong constant.**
   `IlalimNgTulayBuilder.cs:192` sets the map's tonemap exposure to **0.15** where Eskinita uses
   **0.92**. Every linear pixel below **0.59** clips to pure black before it reaches the screen.
   The derivation is § 3.0 of the plan. **This outranks every other item in the file**, because
   nothing else on that map can be judged through it. Reported from the built player, not a test.
2. **Zack's Bolt Sprint paints 27.2 % of the arena off a 6.0 s cooldown**, which is more floor
   than any ultimate. Invisible to every previous pass because the trails were always measured
   one disc at a time and neither trail ability places one disc.
3. **The ultimate meter is a timer.** `UltimatePassiveChargePerSecond` 1.0 against
   `UltimateMax` 100 hands a player who does nothing 90 of the 100 in a 90 s round, which
   `VISION.md` § 4 forbids in as many words.

✅ **STATUS: ARGUED AND BUILT, SAME DAY.** 🧑 read the plan and said *"pls work on this now"*, so
§§ 3 and 4 of that file are shipped except for three items it marks **NOT BUILT** and explains.
§ 7 of it is the as-built record.

⚠️⚠️ **WHAT IS OPEN IS THE MEASUREMENT, AND IT IS OPEN ON PURPOSE.** Every cooldown, charge
count and ultimate cost is a starting position with reasoning attached, not a measured result.
🧑 2026-08-25: *"u can test but dont test fairness yet, js think abt what is fair and what makes
sense and what needs it"*. The `BotBehaviourProbe` Hero Strike A/B that settles them is the next
real piece of work on this entry, and it wants a played build first.

**Also open:** whether 45 s on Carapace and 30 s on Bolt Sprint are the right ENDS of the band.
The order is argued (a power that ignores the game's central risk waits longest) and the band is
🧑's (*"like 30seconds to 45 seconds"*), but nothing has measured whether four seats at those
numbers produces a round worth watching.

**Folded in §§ 2 and 5 below.** § 2 is closed by construction: the barricade returned to 6.0 s
because the charge economy removed the premise that kept it at 3.2. § 5 stays open but its
question changed, and the entry says how.

---

## 8 · The abilities still look repetitive, and half the fix is not done

**Raised by 🧑 on 2026-08-25, twice, off the ability captures:** *"look at this shit all of them
look like circles lang"*, then *"thoroughly plan how to make the skills all look better bcz they
all look repetitive and look like circles"*.

**The plan and the diagnosis are [`Hero_Strike_Balance.md`](Hero_Strike_Balance.md) § 8.** The
short version: every floor effect was a scaled `Cylinder`, so five different fictions were one
primitive in five colours, and HUE was doing all the work of telling them apart. Hue is the one
channel this game cannot spare, because `Art_Direction.md` § 1 already spends orange and blue on
the two ROLES and `UiTheme` spends five more on hero identity.

✅ **Done:** a silhouette each for the five skills (streak, star, crystal, splat) via
`Visual.VfxShapes`, and the Seance Void lifted to 1.35 m so it changes AXIS rather than outline.

✅ **ALSO DONE, 2026-08-25**, after 🧑 played the build and said the effects still *"feel
repetitive or too simple, or too empty"*, which is the played-build judgement item 4 was waiting
for:

1. ✅ **`CreateExplosion` carries a style.** It was one function drawing four events: a 2.2 m
   stomp, a 4.5 m fissure, a 4.8 m Supernova and a thrown tsinelas shared a fire sphere, a
   **`Cylinder` shockwave**, ten fixed-size cubes, one flash, one shake and one sound.
   `ExplosionStyle` gives Fire, Quake, Frost and Slipper their own `VfxShapes` silhouette (none
   is a circle), colour, debris material, flash, radius-scaled shake and cue. Quake drops the
   fireball entirely, because Dante breaks the ground rather than lighting it.
2. ✅ **The payload sounds exist and are the right fiction.** Every kit already fired its element
   on the CAST and then shared two leftovers for the payload: `CreateExplosion` played
   `ability_bagsak_bomb` for all four callers and `CreateThunderstrike` played
   `ability_flick_dash`, **which is a dash**, and both are from the deleted ability set.
   `tools/generate_ability_audio.py` adds six seeded cues: `sfx_quake_slam`,
   `sfx_thunder_impact`, `sfx_frost_nova`, `sfx_possess_enter`, `sfx_possess_exit`,
   `sfx_slipper_burst`.
3. ✅ **Sean's Supernova was spawning Dante's magma.** `SpawnMagmaEruption` in
   `SeanHeroKit`, where Sean is `HeroFire` and Dante is `HeroMagmaCore`. Two heroes reading as
   one is the most expensive form of "repetitive", because it costs a character.
4. ✅ **Auras thin as they expire.** `AttachAura` emitted at a flat rate for its whole life, so
   Dante's carapace looked identical at 0.4 s left and at 6.0 s and the counterplay was counting
   in your head. The RATE falls on a curve that holds to 65 % and then drops to 14 %; the colour
   is left alone so it reads as running out rather than dimming.
5. ✅ **The blast primitives no longer drop colliders.** `CreatePrimitive` hands out a
   `SphereCollider` and a `CapsuleCollider` and neither was ever stripped, so every explosion
   briefly put two solid bodies in the street.

⚠️⚠️ **STILL NOT DONE:**

1. **The three ultimates' own shapes.** Supernova, Thunderstrike and Glacial Nova now differ in
   colour, debris, sound, shake and ground silhouette, but the CORE is still an expanding sphere
   and Thunderstrike's ion disc is still a cylinder. § 8.5 item 1 wants a slam to be a shockwave
   with a FRONT and a nova to be radial but crystalline, and that is unbuilt.
2. ⚠️⚠️ **NONE OF THE ABOVE HAS BEEN SEEN.** `AbilityShowcaseProbe` captures the persistent
   zones only, and every one of these changes is on a transient that lives 0.4 to 1.1 s, so the
   v7 captures do not show a single one of them. **This wants a played build to judge**, and
   until then it is written work rather than verified work.

**Where.** `Assets/TumbangPreso/Runtime/Visual/VfxShapes.cs`,
`Assets/TumbangPreso/Runtime/Abilities/HeroHazards.cs`,
`Assets/TumbangPreso/Editor/MapKit/AbilityShowcaseProbe.cs` for the captures.

---

## 9 · Ilalim ng Tulay dressing defects, reported off the 2026-08-25 player

Five things 🧑 found by playing the build rather than by any check. Grouped because four of
them are the same map and the fifth was found in the same pass.

**9.1 ✅ FIXED: a facade sign whose lettering hung in the air.** 🧑: *"floating texg here pls
remove"*, with `Sign_ComputerParts` in the shot. `ShopFaceX` solves the wall PLANE from the
building's rendered bounds and carries a ⚠️ note about never typing that number in, but the
signs bolted to that wall still typed in their z centre, their width and their height, so
nothing tied a 4.60 m run of capitals to the extent of the wall it names. `PaintedWall` draws
letters and no plate on purpose, so the overhang is not a board past a corner, it is loose
capitals over a pavement gap. `FitToFacade` now narrows the sign to the facade and only then
moves its centre, so a sign that already fitted is left where it was authored. Applied to
`Sign_ComputerParts`, `Sign_Labada` and `Sign_Paluto`.
**Done looks like:** a render of the west corridor with the whole word on the wall.

**9.2 ✅ FIXED: the map is still too dark.** 🧑: *"less dark as before but still dark."* The
exposure fix landed and the frame contrast did not. `Hero_Strike_Balance.md` § 3.0.1 has the
arithmetic: at the shipped 0.92 exposure a contrast of 1.12 still clipped every linear pixel
below **0.0966** to pure black, and Eskinita's 1.03 moves that floor to **0.0422**.
⚠️⚠️ **`MapGradeSanityTests` compares the builder call against the value baked into the scene,
so this REQUIRES a rebuild through `IlalimNgTulayPipeline` in the same change.**

**9.3 ✅ FIXED BY 9.2, AND IT WAS NEVER ITS OWN BUG.** 🧑: *"the pc for 10 mins sign is bugged
too"*. `Sign_Pisonet` is a `FramedFascia` with a `SignCream` face and `SignMaroon` ink, and it
shipped as a dark board with barely legible text.

⚠️ `EnvColourPass` was ruled out before anything was changed: the signs live in a `Karatula`
group, which is in none of `SlabGroups`, `RoadGroups` or `FacadeGroups`, so the pass reaches
`continue` and never touches them. That left lighting, and the fascia sits at y = 3.42 on the
east shopfront directly under the guideway, which is exactly the shadowed 0.04 to 0.10 band the
1.12 contrast was still crushing.

**No colour was touched.** `ability_corridors_v7.png` shows the board reading cream with
"PISONET / P1 5 MIN" legible from the middle of the road. ⚠️ Worth keeping as the example of why
9.2 had to be measured rather than eyeballed: a second "fix" recolouring this sign would have
made it wrong once the grade was corrected.

**9.4 ✅ FIXED: the train floated, and the ride height was never the reason.** 🧑: *"it wasnt on
tracks it was js floating there"*, then *"its weird that the bridge js cuts off, i want the rails
to continue past map or smth"*.

⚠️⚠️ **The first guess was wrong and the check said so.** Seating the consist by measuring its
car bounds against `RailHead` moved it by **exactly 0.000 m**: the kit's origin really is at the
wheel underside and the train really was on the rail, which is why the report has always read
`train on rail`. The seat is kept as a guard, not as the fix.

**The actual cause:** `LrtTrainFlyby` runs z from **-48 to +48** and **parks at -48 between
passes**, while the deck was `GuidewayLength` 48 m, spanning **z -24 to +24**. So a carriage sat
24 m past the south end of the viaduct over open sky for about **21 of every 24 seconds**, and
every existing check measured it against the RAIL rather than asking whether there was rail under
it.

**Fixed** by splitting scenery from structure: `GuidewayVisualLength` 112 m draws bays and rail
out to z +/-56 (the train's +/-48 plus its 7.8 m consist half length), while `GuidewayLength`
stays 48 m and still owns the deck collider and the four live pillar rows, so no footprint in
`Hero_Strike_Balance.md` § 1 moves. Eight far column pairs continue the 9 m rhythm to z +/-55,
seated on the `FarGroundPlate` by measurement, built without the `HazardVolume` and mercury lamp
the live rows carry.

**Guard added:** `MapGeometryCheck` now fails if the deck does not span the flyby's own travel,
which is the bound nothing was asserting. Its bay count is also derived from the builder instead
of the hardcoded 12 that failed at 28 when the map was fixed.
**Verified:** `geometry OK, capture OK`, 28 joined bays, 0 floating, and `ilalim_guideway_v23.png`.

**9.5 ✅ FIXED: text overflowed the objective card.** 🧑's screenshot, bottom right of a gameplay
frame: the card reads `FETCH SLIPPER  ·  -5 / SEC`. The string is `-5 / SECOND`. `HudLabel` sets
`horizontalOverflow = Overflow`, so the line neither wraps nor shrinks, and the card is anchored
to the RIGHT screen corner, so what runs past its 380 px simply leaves the screen.

**Fixed by measuring, not by typing a wider number**, following the idiom `WorstCaseNameWidth`
already uses in the same file: `LataHintLines` lists every string `UpdateLataCard` can show and
the card sizes to the widest of them through the label that will draw it.
⚠️ **The font size is not the lever.** `ui_theme.gd`'s note records these sizes going 16/13,
22/19 and 30/28, answered each time with *"text still small"*. Shrinking text to fit a box walks
straight back into that. ⚠️ **Keep `LataHintLines` in step with `UpdateLataCard`**: a line added
there and not here is a line that overflows again.

**9.6 ✅ FIXED: the fall had a mechanic and no feedback.** 🧑: *"i dont feel like i fell down"*,
then *"make sure theres ui for the button clicking to get back up, make sure it has progress
animation too"*.

⚠️ **Every part of the mechanic already existed.** `MashRecover`, `CanMashUp`, `TripLeft`,
`TripTotal` and `MashPresses` are all there, and `MashPresses` even carries the comment *"so the
HUD can show it filling"*. Nothing ever filled: the whole feedback was a text toast, and it
switched OFF at `MinTripDown`, so the last 0.9 s of every fall went silent while the player was
still on the floor. `BuildGetUpCard` draws a centred prompt with a bar that spans the whole trip,
and it now follows `IsTripped` rather than `CanMashUp`, so it stays up until the player is up.

**And the fall is inside the 1 to 2 s 🧑 asked for.** `MashRecoverPerPress` 0.13 was solved
against the mashing window rather than the time on the floor: 1.60 / 0.13 = 12.3 presses at
0.10 s is 1.23 s, but the floor adds 0.90 s, so a perfect answer still took **2.13 s**. At 0.20
it is 8 presses, 0.80 s, for **1.70 s** total. Still a real burst, and `MashCooldown`'s anti-turbo
bound is untouched.

**9.7 ✅ FIXED: the fall now cuts to third person.** `Hero_Strike_Balance.md` § 8.6 lists falling
as one of only two things in the game that earn the camera, on the rule that an event takes it
when the body changes or the player stops driving it. In first person a fall was the floor
arriving and then 2.5 s of looking at it: the knockdown clip, the get-up clip and the whole
moment happened off screen.

⚠️ **It reuses the emote swing rather than adding a second one.** `BeginEmoteView` already calls
`RestoreSelfHide`, and without that the camera orbits a body `ApplyFppSelfHide` has put into
SHADOWS_ONLY, which is the exact bug reported for emotes as *"doing emote doesnt show myself in
tpp"*. A fall-specific path would have rediscovered it.
⚠️ **It is a CUT, not a blend**, and deliberately the opposite of the possession: a possession is
a transformation and the eye has to travel, an impact is an impact.
⚠️ `_fallView` clears in `Follow` beside `_emoteView`, or a seat change would leave the rig
believing it had already swung and refusing to swing for the next fall.

**9.8 ✅ FIXED: a fall no longer freezes the body.** 🧑: *"i dont want the effect to be ice too
when i fell down it feels weird"*.

⚠️ **The cause is one line and it was a reasonable line.** `CharacterMotor.ApplyTrip` calls
`ApplyStagger` as well as setting `_tripLeft`, correctly, because a player on the floor must not
be able to act. But both frost drivers keyed off `IsStunned` alone, so tripping over a kerb
rendered exactly like being tagged.

**The frost means one specific thing and that is its whole value:** its own note says the taya who
spent their scoring verb on a tag needs to SEE the attacker freeze, and the other two need to know
that seat is gone for five seconds. A trip is a stumble nobody scored for. Both halves now read
`IsStunned && !IsTripped` and they must stay in step, or a fall would frost the screen while the
body on it did not. The trip already has its own read: the knockdown clip, the get-up clip and the
mash card.
**Verified:** PlayMode 57/57 including `StunFrostTests`, which still ices on a tag because it
staggers directly rather than tripping.

**9.9 ✅ FIXED: a recastable ability now says so.** 🧑: *"i dont feel or know that some abilities
are recast too"*. It was invisible by construction: a running ability drew a countdown, and a
running ability you can press AGAIN drew the same countdown. Nemu's Astral Projection is one press
out and one press back, so the entire second half of the ability was an affordance the deck never
mentioned.

The tile now reads **RECAST** in place of the number while `CanReactivate` is true.
⚠️ **The word replaces the number rather than crowding it**: `card.Fill` already carries
`DurationRatio` in the same tile, so the timer is not lost and the text slot is spent on the thing
the player cannot otherwise know.
⚠️ **Gated on `CanReactivate`, never on a hero id**, so a recast added to any future ability lights
up the day it is added.
⚠️ **And it is drawn at 14 pt, not 22.** Six bold capitals do not fit a 60 px tile at the
countdown's size, and `HudLabel` sets `horizontalOverflow = Overflow`, so it would have hung out of
both sides: the identical fault as 9.5, one commit later. The size resets every frame in
`PaintSkillCard` so a tile that showed RECAST cannot keep drawing its cooldown small afterwards.

**9.10 ✅ FIXED: seven trip hazards in one street, and none of them read as trippable.**
🧑, 2026-08-26, off the played build: there were *"too many"* of them and they did not
look like things you fall over.

**Both halves were the same mistake.** Seven triggers across an 18 by 15 m street is not a
hazard, it is a hazard FIELD: the choice a hazard buys is *do I cut this corner*, and it stops
existing once every line across the road crosses one. And three of the seven were drawn as flat
coloured cylinders, which is the puddle failure `VISION.md` § 2 rule 3 forbids for abilities:
footprint doing the work that detail should do.

**Cut to four, by fiction rather than by position.** Both `RoadPothole`s were "a hole in the
road", which the loose manhole and the sunken trench already are, and they sat 2.6 m and 2.8 m
from them: exactly the pair spacing the re-trip loop in 9.11 fed on. `ParesSpill` was a SLICK,
and sliding and tripping are different verbs of which the game has one. The four that stayed are
at least 5.66 m apart and each sits on something `BuildRoadSurfaceDetail` already drew.

**And the survivors got depth instead of area.** A dark square on tarmac is a stain; a dark
square with a raised cast rim standing proud of the road is an opening, and the rim is what a toe
catches. The manhole gained a twelve-segment rim and a shaft throat, the trench gained a ragged
asphalt lip down both long sides over a dropped floor, and the pisonet cords were lifted off the
road to 0.055 m and given the extension block and coil that explain the lift. Nothing grew: the
trigger footprints are untouched. Three tones (`HazardVoid`, `HazardLip`, `HazardBreak`) replace
the single flat colour.
⚠️ **The generic fallback visual is gone and is now a `Debug.LogWarning`.** A hazard added with
no drawing of its own used to get a ring of flat cylinders, which is how three of these shipped.
⚠️ **The lip jitter is derived from the loop index, never from `Random`**, because
`MapGradeSanityTests` compares the builder against the baked scene and a random edge would differ
on every rebuild.
**Verified:** rebuilt through `IlalimNgTulayPipeline`, `geometry OK, capture OK`, Ilalim reports
0 floating, 0 buried, 0 over void.

**9.11 ✅ FIXED: the get-up bar resolved itself, and mashing put you straight back down.**
🧑, 2026-08-26: it *"automatically resolves without doing anything"*, and separately that you
*"CAN'T get up"*. Two defects that look contradictory and are both real.

**The bar was a countdown wearing a mash meter's clothes.** `_tripLeft` decayed by
`Time.deltaTime` every frame whatever the player did, so a fall ended on its own in 2.50 s and
mashing perfectly ended it in 1.70 s. A 0.80 s saving on a 2.50 s event is inside the time it
takes to work out what to press. `Balance.TripPassiveDecayRate` (0.60) slows the bleed only while
there is slack a press could buy, so an answered fall is unchanged at **1.70 s** and an ignored
one is **3.57 s**. The bleed is deliberately not zero: a fall that only a press can end strands a
player whose hands left the keyboard.
⚠️ **And the bar now shows whose work it was.** `CharacterMotor.MashRemoved` is drawn as its
own gold segment over the passive fill, and the bar pops for 0.14 s on every ACCEPTED press, so a
press refused inside `Balance.MashCooldown` reads as a dead press rather than as a punishment.

**The "can't get up" half was the mash key.** It is bound to `Verb.Jump`, so the instant
`_tripLeft` reaches zero the same hammering becomes real jumps, a jump clears
`StreetTripHazard.MinSpeedToTrip` (1.0 m/s) on the spot, and the hazard trips you again.
`StreetTripHazard.Cooldown` cannot answer it because it is PER HAZARD, so a neighbour 2.6 m away
re-tripped with no wait at all. `CharacterMotor.IsTripImmune` is one window on the body that every
hazard reads, opened where a fall actually ends so it covers a mashed fall and a timed-out one
alike. `Balance.TripGraceAfterGetUp` is 1.20 s, which carries an attacker 4.14 m, more than the
widest hazard footprint on the map (2.60 m).
**Verified:** `Trip_AnsweringItIsWorthAtLeastHalfTheFall` and
`Trip_GraceCarriesAPlayerClearOfTheHazardThatFelledThem` in `Core.Tests`, plus the hazard cut in
9.10 which removed the two pairs that made the loop reachable at all.

**9.12 ✅ FIXED: the fall camera was framed for a standing body.**
🧑, 2026-08-26: the placement is *"awkward"*. 9.7 gave the fall a third-person cut and reused
the emote swing wholesale, including its SHOT. An emote is a pose you chose while standing, so it
is framed off `TppMountHeight` = 1.20 m at 4.5 m out; a fall is a body flat on the tarmac, and
that mount is now 1.2 m of empty air above a subject 0.40 m tall.

The fall gets three numbers of its own (`FallMountHeight` 0.20, `FallSpringLength` 2.80,
`FallPitchDeg` 26) and its own pitch clamp, because the emote band tops out at 20 degrees, BELOW
the angle a fall opens at, so sharing it would have pulled the shot back to the standing framing
on the first frame. The eye ends up 1.43 m up and 2.52 m back, looking DOWN at the road.
⚠️ **The shared entry point is untouched and must stay that way.** `BeginEmoteView` calls
`RestoreSelfHide`; without it the camera orbits a body in SHADOWS_ONLY, which is the reported
*"doing emote doesnt show myself in tpp"* bug. Only the numbers are the fall's own.
⚠️ **Emotes are not affected.** `ApplyEmoteView`'s own note records a hand-picked short boom
being wrong FOR AN EMOTE; that argument is about a standing body and it still holds.

**9.13 ✅ FIXED: you were never actually on the floor, and the cause was in the assets.**
🧑, 2026-08-26: *"it js plays an animation and ur already up"*.

⚠⚠ **EVERY CLIP ON ALL 29 RIGS IS 0.333 s AND EVERY ONE IMPORTS WITH `isLooping = true`,
measured on 2026-08-26.** That single fact is behind both halves and neither is guessable from
the code:

* `SetDuration(clip.length)` does not stop an `AnimationClipPlayable` whose CLIP is marked
  looping. So `die`, played with `loop: false` precisely so it would hold its last frame, wrapped
  every third of a second: over the 1.6 s of a fall the body dropped and sprang upright about
  five times. The note above that call already described that exact bug as fixed. It was not: the
  flag on the asset outranks the call.
* `pick-up` is also 0.333 s and played at 1x into the 0.90 s floor, so the get-up finished 0.57 s
  early and the body held a bent-over pose for the rest.

`CharacterAnimator._holdAtEnd` freezes any non-looping clip on its last frame in code rather than
trusting the importer, which fixes the emote hold path too. `StepTripPose` owns a fall end to end
and time-scales `pick-up` from the clip's own measured length, so a re-export cannot silently
reintroduce a get-up that lands early.
⚠️ **`MinTripDown` was NOT the lever**, exactly as its note demands. What moved is the
hardcoded **0.70** that `Choose` switched clips at, which disagreed with the 0.90 the mash floor
and the HUD both used, so 0.20 s of every fall was a state where the press was refused, the HUD
said GETTING UP, and the body was still face down. One number, one meaning.

**9.14 ✅ FIXED: two ways the guided training route could not be finished by playing it.**
Found by reading `GuidedTraining.cs` against the code it drives, both on the 17-lesson route
behind `START TRAINING`.

* **`TripRecovery` could strand the player.** The trip is applied ONCE on entering the lesson and
  the exit condition is five ACCEPTED presses, but a fall holds at most
  (2.50 - 0.90) / 0.20 = 8 of them. A player who watched the first fall out instead of mashing
  reached zero with the counter short and nothing left to press. It now puts you back down until
  the counter is met, which is also the honest teaching: mashing is what ends a fall.
* **The four hero lessons cannot be answered by a seat with no kit.** `AbilityInfo`, `Skill1`,
  `Skill2` and `Ultimate` all check `HeroAbilitySystem`, and Classic is a shipping mode with no
  powers at all (`CLAUDE.md` § 1), so pressing the key produced no cast and `WasSuccessfulCast`
  stayed false forever. `LessonNeedsAKit` skips them when there is no kit. The N key would have
  carried a player past both, but a tutorial whose only exit is the skip key has failed.
⚠️ **`TripRecovery`'s "natural drain is one second per second" comment was stale** the moment
9.11 landed. The detector still cannot credit the bleed as a press: the bleed got SMALLER, which
only widens the gap a press has to clear.

**9.15 ✅ FIXED: `DeadFeatureAudit` failed a feature that was never removed.**
`TacticalPauseBelongsOnlyToSpectatorCamera` asserted the exact literal
`"Time.timeScale = 0.0f"` in `SpectatorCamera.cs`. `ToggleBroadcastPause` became
`Time.timeScale = _broadcastPaused ? 0.0f : _selectedTimeScale` when the broadcast speed keys
landed: same pause, same P binding, same zeroed clock, different spelling. A source audit that
pins the spelling of a line reports a refactor as a deleted feature, which is the opposite of what
that file is for. Now a regex, with a lookbehind so it does not match the `0f` inside
`PausePanel`'s `Time.timeScale = 1.0f` resume line.

**9.17 ✅ FIXED: an effect parented to the can failed the prop outline test.**
`InputEdgeTests.EverySlipperAndTheLataWearTheToonOutline` walks every renderer under a slipper or
the lata and demands `TumbangPreso/Toon`, because the ink outline and the palette remap are the
look. `LataRestoreShield`, the restore-protection shell, is a transparent sphere parented to the
can, so it is a renderer under the lata by construction and it came back on `Standard`.

⚠️ **Making it toon would have been the wrong fix.** A shell with an ink outline drawn round it
is a solid object, and the entire point of it is that the can is visible through it. The rule the
test wants is "is this part of the model", so `Visual.VfxRenderTag` is attached by
`VfxMaterial.Ghost` and `VfxMaterial.Solid` themselves and the test skips anything carrying it. An
effect written later is exempt on the day it is written, and a name in a skip list would have had
to be added again for the next one.
**Verified:** PlayMode 59/59.

**9.16 ✅ DONE: three dead documents deleted.** `docs/README.md` has the reasoning per file.
`CUSTOMIZATION_SYSTEM_PROMPT.md` was a committed agent handoff for a feature that was never built,
which `CLAUDE.md` § 2.4 forbids; `character_bayan_reference.md` pointed every reference image and
render at a `.gemini/antigravity/brain/` path that no longer exists; `Feature_Audit.txt` was a
stale second answer to the question `Port_Ledger.md` answers and keeps current.
⚠️ **`ZACK_AND_EXPRESSIONS_HANDOFF.md` and `ZACK_HAIR_AND_ELECTRICITY_HANDOFF.md` are still on
`main`.** They were deleted on this branch on 2026-08-23 and `d6131f67`, the revert of the first
Ilalim merge, put them back on `main`. Deleting them here cannot remove them there. **Open: one
commit on `main` that deletes both.**

---

## 1 · Peer rematch voting across the wire

**The last genuine PARTIAL row in the ledger, and the only one.**

`match_result.gd`'s rematch is a VOTE in a networked match. Here the button acts locally, so
four peers can each think a rematch is or is not happening. Single-player rematch works.

**Needs:** an RPC pair (a peer votes, the host broadcasts the tally), the tally drawn on the
result card, and the same "counts peers, not characters" rule `ReadyGate` already uses, since
bot-filled seats cannot vote.

**Where.** `Assets/TumbangPreso/Runtime/MatchResult.cs`,
`Assets/TumbangPreso/Runtime/Net/MatchRpc.cs`, and `ReadyGate.cs` for the pattern to copy.

⚠️ **This cannot be finished honestly without two real processes on a LAN**, which has never
been run. Write it, cover it in `RuntimeLayerTests` the way reconnect is covered, and say
plainly in the handoff that the wire half is simulated.

---

## 2 · Cheska's Ice Barricade duration was set by accident ✅ CLOSED 2026-08-25

**A one-line balance question left open on purpose, because it wants a measurement.**

`CheskaHeroKit.IceBarricadeAbility.OnActivate` calls
`SpawnIceBarricade(position, forward, duration)`. A calibration pass on 2026-08-23 meant to
set the wall's FOOTPRINT to 3.2 m and passed 3.2 into the third parameter, which is the
DURATION in seconds. The signature has no radius parameter at all, so the footprint stayed at
its `HazardVolume` default of 1.6 m and the wall's life quietly went from 6.0 s to 3.2 s.

The 3.2 was kept rather than reverted, on the balance rather than on the history: the skill
cools in 9 s, so a 6 s wall stands for two thirds of every cycle in front of a lata that only
has to survive 90 s. But nobody has measured either value.

**Needs:** a `BotBehaviourProbe` Hero Strike run at 3.2 s and at 6.0 s, comparing knockdowns
against the round and unretrieved-slipper penalties, and the winner written into the call with
its number. The argument is named now (`duration: 3.2f`) so the next reader cannot repeat the
mistake, and the telegraph radius (1.6 m) is asserted against the `HazardVolume` in
`HeroPresentationTests.TelegraphsMatchWhatTheAbilityPlaces`.

✅ **CLOSED BY CONSTRUCTION RATHER THAN BY THE A/B ABOVE, AND THE A/B IS NO LONGER THE RIGHT
TEST.** The whole argument for keeping 3.2 was the one in the paragraph above: the skill cooled
in 9 s, so a 6 s wall stood for two thirds of every cycle. **That premise is gone.** § 0's charge
economy makes the barricade ONE charge per round, refilled only by Cheska retrieving her own
tsinelas, so the wall is up for 6 s out of 90 rather than for 60 s out of 90.

A wall you get once a round has to be worth walking around, and 3.2 s is barely long enough to
cross the box. Restored to **6.0 s**, which is what the signature always defaulted to and what
the ability was written against before the 2026-08-23 parameter mix-up.

⚠️ **The named argument stays** (`duration: 6.0f`), because the mistake this entry records was a
positional one and naming it is what stops it recurring.

**Where.** `Assets/TumbangPreso/Runtime/Abilities/CheskaHeroKit.cs`,
`Assets/TumbangPreso/Tests/PlayMode/BotBehaviourProbe.cs`.

---

## 3 · The five hero accents have not been seen in a real match

**Not a bug. A judgement that needs a human and a played round.**

`UiTheme`'s five hero accents were re-tuned on 2026-08-23 to answer `Art_Direction.md` § 1:
Dante was four degrees off the Offense orange and Cheska twenty off the Defence blue, so both
could read as a role rather than as a hero. The new set is asserted at 25 degrees clear of both
role hues and 30 degrees clear of each other by `HeroPresentationTests`, and the reasoning is
in `docs/Hero_Strike_UI.md` § 3.

**The one worth arguing with is Dante.** His kit is magma and orange is the colour he cannot
have, so his accent is now jade (`#3fa65c`), the colour of the crust, while his fissure light,
embers and magma core stay hot orange through `UiTheme.HeroMagmaCore`. It is defensible and it
is a real change to a character's identity.

**Needs:** a played Hero Strike round, four seats, and an answer to one question: can you still
tell at a glance which player is the taya. If the answer is no the accents move again; if it is
yes, this closes.

---

## 4 · Bayan Plaza's monument stands inside the defender's box

**Found by `MapGeometryCheck`, not by playing, and it is a Hero Strike fairness problem.**

`BayanPlaza/Obstacles/MonumentBody/CollisionShape3D` occupies **0.70 m by 1.90 m of the chalk,
from y = 0.10 to y = 5.10**. The taya is CLAMPED into that box (`Confinement.ClampToBox`) and
cannot step out to walk around it, so one approach to the can is permanently shielded for
whoever is defending.

In Classic that is a quirk you play around. In Hero Strike it is a coin flip: a wall or a zone
placed against the monument closes a lane outright, so the seat that draws the taya round with
the good geometry has a different game from the other three, and `docs/VISION.md` § 4 says the
mode is aimed at a bracket.

**Needs:** the monument moved to the plaza edge outside |x|, |z| = 7.0, or its collider reduced
to something below `CharacterController.stepOffset` (0.30 m) so it is a plinth rather than a
wall. Then add `BayanPlaza.unity` to `MapGeometryCheck.Gated`.

⚠️ **The scene is an IMPORTED `.tscn`, not built from code**, so this is a scene edit rather
than a builder change. That is also why the map is only reported on today and not gated.

**Where.** `Assets/TumbangPreso/Scenes/Maps/BayanPlaza.unity`,
`Assets/TumbangPreso/Editor/MapKit/MapGeometryCheck.cs`.

---

## 5 · The overclock window has not been measured against a match

**A new Hero Strike mechanic with a defensible number and no evidence behind it.**

`OverheadPassWindow.OverclockRate` is **2.0** for the 2.70 s the LRT consist is over the street,
every 24 s. The reasoning is in `docs/Ilalim_Ng_Tulay.md` § 3.5 and it is sound: it pays a
player who is already casting rather than one who is waiting, so it cannot violate
`docs/VISION.md` § 4. What nobody has is the number.

At 24 s intervals and a 2.70 s window, the mode spends **11.25 per cent of a round** at double
cooldown rate. Against a 9 s skill that is roughly one extra cast every four cycles for a player
who plays around it, and zero for one who does not. Whether that gap is "a skill" or "a tax on
not knowing" is the open question.

**Needs:** a `BotBehaviourProbe` Hero Strike run on this map at `OverclockRate` 1.0 (off), 1.5
and 2.0, comparing skill uses, ultimates and knockdowns per round. The winner goes into the
constant with its number, the way every other measured value in this repo does.

⚠️ **The probe runs on Eskinita today.** Pointing it at a second map is part of the work, and it
is worth doing anyway: § 4 above and `docs/Ilalim_Ng_Tulay.md` § 1 are both arguments that map
geometry changes Hero Strike outcomes, and nothing in the harness has ever measured that.

⚠️⚠️ **THE COOLDOWNS MOVED UNDER THIS ENTRY ON 2026-08-25 AND THE NUMBER ABOVE IS STALE.**
`OverclockRate` is **3.5** now, not 2.0, and the raise is arithmetic rather than a buff: a rate
multiplier saves the same absolute 2.70 s whatever the cooldown is, so against § 0's 30 to 45 s
cooldowns the old 2.0 was worth **6.0 to 9.0 per cent of a cycle** where it used to be worth
**41 per cent**. A map mechanic nobody plays around is a map mechanic that does not exist.

**What is still owed is the A/B, and it should now compare 1.0, 3.5 and a FLAT saving rather
than three multipliers.** `Hero_Strike_Balance.md` § 4.5 argues the mechanic should stop being a
multiplier at all and become a flat cooldown reduction or a charge, on the grounds that a flat
figure survives any later cooldown retune where a multiplier does not. That is the real question
here and the three-rate sweep in the paragraph above no longer answers it.

**Where.** `Assets/TumbangPreso/Runtime/Map/OverheadPassWindow.cs`,
`Assets/TumbangPreso/Tests/PlayMode/BotBehaviourProbe.cs`.

---

## 6 · `AiDiagnosticProbe`'s Classic round is a real-time test and it flickers red

**Found on 2026-08-25 while verifying an unrelated fix. It is NOT a gameplay regression, and
the evidence for that is written down here so nobody re-derives it.**

`OneClassicRoundAtRealSpeedIsFullyExplained` asserts no tsinelas stays loose longer than
**20.0 s**. It failed twice in a row at **21.6 s** and then **29.9 s**, and passed on the same
machine minutes earlier.

⚠️ **A third data point, 2026-08-26: it failed at 37.6 s and then passed on an immediate re-run
with nothing changed between the two**, on a machine that had just finished a map rebuild and an
EditMode run. That is the same signature again and it is the strongest evidence yet that the
failure is the harness rather than the AI: 37.6 s is not a near miss of a 20 s bound, it is a
round that spent most of its wall clock somewhere other than this test.**

⚠️ **It runs at 1x for 40 real seconds by design** (see the class note: anything measured at a
high time scale is partly a measurement of the harness). That makes it the one test in the
repo whose result depends on how busy the machine is, and two consecutive failures 8.3 s apart
in value is the signature of a frame-rate difference, not of a behaviour change.

**Why it is not the hazard fix that landed the same day:** Eskinita, the map it loads, contains
exactly four MonoBehaviours (`EnvColourPass`, `KillPlane`, `MapGrade`, `MatchInstaller`) and no
`HazardVolume` or `StreetTripHazard` at all, and Classic casts no hero abilities, so `HazardMap`
is empty for the entire run. `OneHeroRoundAtRealSpeedIsFullyExplained`, which is the mode that
does populate it, passed in the same suite.

**Needs:** a decision, not a bug hunt. Either the bound moves with a measured reason, or the
probe stops asserting on wall-clock-sensitive quantities and only prints them, or it is marked
explicit-run-only so it stops costing a full PlayMode suite to learn nothing. The worst outcome
is the current one, where a red result carries no information and the next session spends a run
finding that out again.

⚠️ **The second failure is worth one look before deciding.** It printed
`own=3 plan=Fetch ownerAct=True d3=1.10 grabbable=True`: a bot 1.10 m from a grabbable slipper
it had already decided to fetch, still not holding it. If that is reproducible at a normal frame
rate it is a real retrieval bug and this entry becomes a gameplay one.

**Where.** `Assets/TumbangPreso/Tests/PlayMode/AiDiagnosticProbe.cs:242`.

---

## 7 · The test suite costs more to run than it is currently returning

**Raised by 🧑 on 2026-08-25: *"we have too many tests and we are wasting so many credits to run
them all and fix the code for the test"*. This is a real constraint and it belongs on the list
rather than in a chat log.**

A full verification pass today is Core (1 s) plus EditMode (105 tests) plus PlayMode (55 tests,
several of which run whole matches at 1x) plus four separate editor checks, and **each of the
last five is its own Unity launch**. The launches, not the assertions, are the cost.

⚠️ **THE ANSWER IS NOT TO DELETE TESTS, AND SPECIFICALLY NOT THE MEASURED ONES.** `CLAUDE.md`
§ 7.1 lists three faults that no amount of playing would have found, and the crash closed on
2026-08-25 was caught by nothing at all and cost a whole session. Coverage is not the problem.
**Cadence and batching are.**

**Needs, in order of payoff:**

1. **One launch, many checks.** `ArenaCheck`, `MapGeometryCheck`, `AudioCueCheck` and
   `SceneScriptCheck` are four `-executeMethod` launches that could be one entry point running
   all four and exiting non-zero if any fails. That is the single biggest saving and it changes
   no test logic.
2. **Name a fast gate and a full gate.** Fast: Core plus EditMode plus the combined checks, for
   every change. Full: adds PlayMode, for anything touching gameplay, and before a build.
   Right now every change pays for everything.
3. **Take the wall-clock probes out of the default PlayMode run** (§ 6). `AiDiagnosticProbe`
   alone is 80 real seconds of the suite and produces a report to read, not a pass to rely on.

**Done looks like:** a documented two-tier command list in `docs/TESTING.md`, and a full pass
that is fewer than four Unity launches.

---

## Closed

- **Lobby client synchronization, pick normalization, and host non-zero seat picks.** ✅ 2026-08-26.
  `LobbySession._peers` was only populated host-side on `Admit()`, leaving client `PeerCount` at 0
  and `PeerInSeat(slot)` returning null. On clients, `MatchInstaller` fell back to building remote
  seats as nameless default bots, and appearances snapped only when `ReadyGate` broadcast
  `SyncPicksClientRpc` on countdown completion.

  **The fix, in three parts:**
  1. Authoritative seat roster (`LobbySeatInfo`): Host broadcasts the full seat roster
     (`SyncLobbyPicks` carrying `Seat`, `PeerId`, `Name`, `Occupied`, `Spectator`, `CharacterPick`,
     `CanPick`, `SlipperPick`). `MatchRpc` persists `_replicatedSeats` across scenes, and
     `MatchInstaller` builds remote seats directly from the authoritative roster without querying
     client `LobbySession`.
  2. Host seat vs transport ID: `SelectLobbyPickServerRpc` previously used `LocalSlot` (0-3) as peer
     ID, causing a host in seat 1+ to update whichever peer held client ID 1 and silently no-op on
     its own record. Host now derives its transport client ID from `_nm.LocalClientId`.
  3. Pick normalization and name integrity: Live on-screen picks are normalized
     (`CharacterPick >= 0`) and published on lobby entry (`ConvertedMatchSetup.Wire` and mode cycle)
     so default index 0 is known to peers before opening the picker. `SyncPicksClientRpc` no longer
     clobbers `PlayerName` with `Roster.People[charIndex].Name`.
  Verified by 122/122 EditMode tests including `HostInNonZeroSeatUpdatesItsOwnRecordWithoutTouchingOtherPeers`,
  `SetPicksRejectsInvalidIndicesAndDefaultsToMinusOne`, and
  `AuthoritativeRosterPreservesPeerNamesAndPicksWithoutClobbering`.

- **Gameplay readability, spectator highlights, match length and guided training.** ✅ 2026-08-26.
  Throw commitments now survive a teammate knocking the lata down; a restored lata has a real
  1.25 s impact shield; a persistent world beacon, central alert and pulsing card make a down
  lata unmistakable. Hero Strike plays eight rounds while Classic remains four. Scoreboard
  rows spell out ATTACKER and DEFENDER with role rails instead of replacing role colour with a
  yellow local-player tint. The final 30 seconds build through three visual pressure bands and
  a continuous gain curve on the same music bed, with no track cut.

  Spectator replay now captures local graded pixels, detects lata knockdowns, tags, sabotage
  and large score plays, then shows a picture-in-picture replay while live play and the local
  spectator camera continue. It sends no RPC and rewinds no live transform.

  The existing How to Play pages now include `START TRAINING`, which launches a local 17-lesson
  route covering look, movement, sprint, jump, normal throw, retrieval, Pektus, shove, ability
  information, both skills, ultimate, defender reset, punch, lunge, trip recovery and emotes.
  Verified by 61 Core tests plus clean full-runtime and test-assembly compilation. Native Unity
  was attempted but the machine's package manager returned `path ... Received undefined` even
  on an empty Unity project; `-noUpm` also omitted package assemblies, so native XML and the
  Windows player build still need to be produced after that machine-level failure clears.

- **The shipped build hard crashed the moment a player selected Ilalim ng Tulay.** ✅ 2026-08-25.
  Reported from the actual .exe, not from a test.

  **The symptom lied.** `Player.log` read
  `The file '.../TumbangPreso_Data/level8' is corrupted! Remove it and launch unity again!`
  followed by `[Position out of bounds!]` and a native `Crash!!!`. Nothing was corrupt. Every
  serialized file in the build parses clean: headers self-consistent, all 12,045 objects in
  `level8` inside the data section with zero overlaps and zero slack, all 8 external references
  present, and all 78 mesh and texture streaming records inside their `.resS`.

  **The cause.** Eight `HazardVolume` components in the scene had an `m_Script` pointing at an
  inline `!u!115 MonoScript` document written into the scene file itself rather than at a script
  asset. Unity emits that stub when it cannot resolve a `MonoScript` for a type, which happens
  whenever **the class name does not match the file name**: `HazardVolume` was declared at line
  182 of `HazardMap.cs`. The player has no layout to deserialize the component against, reads
  past the end of the object, and dies.

  **Why nothing caught it.** Core 60/60, EditMode 105/105, PlayMode 55/55, HeadlessCheck,
  ArenaCheck, AudioCueCheck and MapGeometryCheck were all green on the commit that shipped it.
  ⚠️⚠️ **Every one of them runs in the editor, and the editor resolves the stub by class name.**
  This failure is invisible to any in-editor check by construction. Every other `HazardVolume`
  in the game is attached at RUNTIME by `HeroHazards` and `StreetTripHazard`, where nothing is
  serialized and the defect cannot occur; Ilalim ng Tulay is the first map to bake one into a
  scene at author time (`IlalimNgTulayBuilder` attaches one per LRT pillar), which is why one
  map and only one map crashed.

  **The fix, in three parts.** `HazardVolume` moved to its own `HazardVolume.cs` with a note
  saying why nothing may be merged back into it. All three affected scenes repointed at real
  script assets: `IlalimNgTulay.unity` (8 x `HazardVolume`), and two that never shipped and had
  never been mentioned anywhere, `CharacterSelect.unity` (1 x `ConvertedCharacterSelect`) and
  `VerticalSlice.unity` (4 x `EmotePlayer`) whose stubs were stale rather than structural, since
  both of those classes already live in correctly named files. Then `SceneScriptCheck`, which
  fails any build scene carrying a stub, a guid-less `m_Script` or a guid that resolves to
  nothing, reading the scene as TEXT because opening it is what hides the fault.

  **Verified** by reintroducing the exact defect into `IlalimNgTulay.unity` and confirming the
  new check exits 1 and names all nine findings, then restoring and confirming it exits 0 across
  all 9 build scenes and 8 non-shipping ones. `GameBuilder` now runs it before every build, and
  the rebuilt player reaches the map with no `MonoScript` object in `level8` at all.


- **Ilalim ng Tulay looked assembled rather than lived in.** ✅ 2026-08-25. Four faults, all
  found in renders and all fixed against renders rather than against prose.

  **The PC Express sign was not the PC Express mark.**
  `tools/build_pc_express_logo_mesh.py` kept only the WHITE pixels of the supplied artwork, so
  the blue "P" of EXPRESS vanished (v14 reads "PC EX RESS"), the italic X collapsed into a
  starburst and the slanted red-over-blue field was replaced by a blue rectangle with a red bar.
  It now segments all three brand colours (`#FFFFFF`, `#D22630`, `#003DA5`) off one quantised
  image and extrudes **five stacked plates**: the parallelogram panel, the red field band, the
  white keyline around the monogram, the red PC outline with its counters, and the white letter
  faces. 5,272 vertices, 18 contours, 48 mm of relief, registered mark omitted. Three faults
  were found and fixed by capture on the way: the ® sat INSIDE the panel so forcing its corner
  to red before the panel was solved grew a square red horn (v15); the dark metal return was a
  SOLID box pushed 0.19 m proud, which buried the whole mark behind a grey slab (v16), and is
  four perimeter rails now; and the plate emission at 0.30 to 0.52 Ke under a 1.5-intensity sign
  light clipped the whites to paper and washed the field pink, so both came down by more than
  half. Verified in `ilalim_pcexpress_close_v22.png`, `ilalim_pcexpress_v22.png` and
  `ilalim_thrower_view_v22.png`, where it reads from the throwing line.

  **Every sign on the strip was the same sign.** § 9.2 was ticked off with six different
  STRINGS on six near-identical wall-flush rectangles. `StreetSignKit` now holds **eleven sign
  systems** (lightbox, framed fascia, projecting blade, ground A-board, enamel placard, lashed
  tarpaulin, pole pylon, hung panel, painted wall, vertical banner, corrugated tin sheet) and
  one `LetterStyle` (aspect, weight, tracking, slant, relief) applied to the ONE 5-by-7 glyph
  table this repo is allowed to have. Thirteen businesses, and no two neighbours share a
  silhouette. Two real bugs were found by capture: glyph aspect was computed in the plate's
  normalised space, so on the 1.80 by 0.92 m tin sheet a 0.78 ratio became 1.53 in world and the
  strokes merged into slabs; and the tin sheet's corrugation ribs were 13 mm PROUD of the board,
  interleaving through every letter. Verified in `ilalim_pavement_west_v22.png` and
  `ilalim_pavement_east_v22.png`, where GOMA and XEROX PRINT read cleanly.

  **The background was underfilled and visibly repeated.** Renderers went from **1,242 to
  2,314**. The near rows carry per-instance scale (4.40 to 6.20 instead of a flat 5.0) and
  setbacks of 0.00 to 1.10 m, and the two sides run different sequences so the street is not
  mirrored. Added: roof tanks, chimneys, aircon plant and aerials on every near shophouse, five
  swaying `Sampay` washing lines, a twelve-building second shop row, yard fencing, back-lot
  stacks, a pipe run on trestles, a crane, a hopper, six hoardings on masts, district lamp rows,
  cross-street traffic and a four-piece stabled consist. Verified in
  `ilalim_depth_overview_v22.png` and `ilalim_background_north_v22.png`.

  **And the far ground plate was being painted as a BUILDING.** It was named `MalayoX_Ground`
  under `Malayo`, and `EnvColourPass.IsBuilding` matches any `MalayoX_` instance, so the pass
  gave the 240 m ground a facade tint and mapped a corrugated ROOF atlas across it. That is why
  every gap in the district read warm pink. It sits in its own `Lupa` group now.

  **Two grounding bugs the gate caught that no render would have.** `Renderer.bounds` is a
  cached world AABB and had not taken the position written one line earlier, so solving a ground
  offset from it read the model's LOCAL underside as a world height: `TryVisibleBounds` pushes
  the mesh bounds through `localToWorldMatrix` instead, which cannot be stale. And
  `AirborneByDesign` on a whole vehicle was hiding the resulting float behind an excuse whose own
  text named the number; `ExcuseSuperstructure` excuses the body and leaves the WHEELS gated, so
  the solve stays verifiable. The boundary cars now touch the road.

  Verified: `MapGeometryCheck` **0 findings** on the gated map with 12 joined bays, 8
  pillar-to-soffit joins, 2 track-to-deck joins, train on rail, 26 wire spans and 28 grounded
  poles; box clear; floor solid across x +/-11.2, z +/-16.7; Core 60/60, EditMode 105/105,
  PlayMode 55/55, `HeadlessCheck` OK, `ArenaCheck` OK, `AudioCueCheck` OK. Renders v15 to v22 in
  `Logs/shots-ilalim/`. The plan they were built against is `docs/Ilalim_Ng_Tulay.md` § 10.

- **A trip put you on the floor and gave you nothing to do about it.** ✅ 2026-08-25.
  🧑: *"like maybe places u can trip on? then fall down animation plays and u have to spam a
  button to get back up"*. The knockdown already shipped (`CharacterAnimator` plays `die` while
  `TripLeft > 0.70` and `pick-up` under it, both non-looping) but nothing could shorten it.
  `Combat.MashRecover` takes `Balance.MashRecoverPerPress` (0.13 s) off per press, rate-capped by
  `Balance.MashCooldown` (0.10 s, so 10 Hz) and floored at `Balance.MinTripDown` (0.90 s), which
  leaves 0.20 s of knockdown before the get-up begins. `CharacterMotor.MashRecover` takes the
  STUN down with the trip, without which the player mashes free and then stands frozen for the
  rest of the original 2.5 s. Bound to Jump contextually, so no binding was added; the AI toggles
  the same verb so a bot is held to the same 10 Hz ceiling by the same function; the HUD prompt
  reads the live binding. Two new trip sites inside the chalk, both on road detail that explains
  them (the loose lid at -4.60, 2.40 and the sunken trench at 4.60, -2.60), both clearing the can
  by over 5 m against a 1.40 m minimum. Four Core tests assert the bound rather than the feel:
  a mash cannot cancel a trip, presses inside the cap do nothing, the saving fits inside the fall
  (12.3 presses over 1.23 s of a 2.50 s trip), and the floor leaves the knockdown clip time to
  play.

- **The three measured art faults under the LRT guideway.** ✅ 2026-08-24. The 6.88 m custom
  deck, its unsupported third rails and the wrong-gauge custom train were replaced together by
  a 10.5 m `roads/road-bridge` guideway, two `train/track-detailed` lines and a three-piece
  `train-electric-city` consist. The cargo tricycle now has a stem from its y = 0.93 frame to
  its y = 1.025 handlebar and a second join across the 0.15 m handlebar-to-grip gap. The
  basketball rim's separate 0.23 m gap to its backboard was found in the same island audit and
  received a bracket too. Verified from the v9 guideway and hoop captures, the committed island
  checker, and gated `MapGeometryCheck` including the elevated-assembly joins.

- **First-Person Character-Specific Viewmodel Arms.** ✅ 2026-08-23.
  Customized first-person viewmodel arms (`ViewmodelArms.cs`) with bespoke skin tones, sleeves, wristbands/bracers, and elemental signatures for each hero (Sean, Zack, Dante, Cheska, Nemu, and Classic street mode):
  - Sean: Warm golden brown tan skin, red athletic rolled sleeves with gold trim, fiery orange wristbands with ember warmth, and crimson wraps.
  - Zack: Athletic warm tan skin, high-tech carbon compression sleeves with electric yellow/teal speed stripes, angular tech bracers with glowing lightning conductor plates.
  - Dante: Dark volcanic bronze skin, heavy faceted basalt rock arm guards with jade crust studs, molten glowing magma fissure veins (`UiTheme.HeroMagmaCore`), and volcanic rock knuckles.
  - Cheska: Fair porcelain skin, frost-blue winter coat sleeves, insulated fluffy white cuff trim, crystalline ice bracers with snowflake facets (`UiTheme.HeroIceBright`), and fingerless winter gloves.
  - Nemu: Pale lavender ghostly skin, dark shadow-purple spectral wraps, flowing spirit ribbon wisps (`UiTheme.HeroSpiritBright`), void energy wristbands with glowing runes.
  - Classic: Canonical street tan skin (`ArmColour`), rolled streetwear shirt sleeves, and neutral athletic sweatbands.
  - Preserved camera mounting, all 15 bespoke hero action clips (`PlayAction`), wind-up charge (`WindupRad`), carry poses, and held slipper attachment under `RightPivot/Arm`.
  - Shaded with canonical `ToonSkin` ink outlines and `VfxMaterial` emission.
  - Verified with 56/56 Core tests, 100 EditMode tests (`HeroPresentationTests`), 55 PlayMode tests, and clean Windows standalone build.

- **Hero Ability Animations & VFX Overhaul.** ✅ 2026-08-23.
  Overhauled cast animations and visual feedback across all 5 hero kits (15 abilities total).
  - Built procedural 3D AnimationClips on the 7-bone rig (`HeroAbilityClips.cs`) replacing borrowed generic fallback clips (`dash`, `shove`, `jump`).
  - Added bespoke 1st-person viewmodel animation keyframe clips (`ViewmodelArms.cs`) for all 15 hero cast verbs.
  - Implemented elemental hand empower VFX (`AbilityVfx.AttachHandVfx`) for Sean's Ignition Cannon and Zack's Static Charge.
  - Added responsive cast flash VFX bursts (`AbilityVfx.SpawnCastFlash`) and tightened integration with character squash/stretch.
  - Preserved color laws, readability budget, and Quiesce rules (no auras on Cheska's body, no auras on trail discs).
  - Verified with 56/56 core tests, 98 EditMode tests (`HeroPresentationTests.EveryHeroAbilityHasBespokeCastAndViewModelActions`), 55 PlayMode tests, and standalone Windows build.

- **Hero abilities felt clunky, and the hero UI was cramped and off-brand.** ✅ 2026-08-23.
  One request, seven separate faults, every one verified rather than assumed.

  **It did not compile.** `Hud.BuildAbilityCard` was missing its closing brace at
  `feat/hero-modes-and-abilities-ui-antigravity` HEAD, and `InputBinding.ToHumanReadableString`
  was being reached as an extension method in a file with no `using UnityEngine.InputSystem`.
  Two compile errors, so nothing on the branch had run.

  **Presses were being eaten.** `HeroAbilitySystem.Update` returned before it read the intent
  table whenever `CanAct()` was false, and `JustPressed` is a one-frame edge, so a skill
  pressed during a stun, a stagger or a shove recovery vanished with nothing recording it. A tag
  is a five second stun. There is now a 0.30 s buffer, and only "cannot act" is held: a cooldown
  or charge refusal is answered and cleared. `TheInputBufferIsShortEnoughToBeAnAid`.

  **A refused press looked identical to one that worked.** `TryActivate*` returned a bare
  `bool`, so nothing could tell "on cooldown" from "stunned" from "meter empty", and all three
  drew nothing at all. `HeroKit.CastOutcome` reports which; the tile now flashes hero-accent on
  a cast and `Danger` with `ui_error` on a refusal, inside one frame.
  `ACoolingAbilityAnswersDifferentlyFromAnEmptyMeter`, `ASkillOnCooldownSaysSo`.

  **The ground telegraph lied.** The HUD invented 7.5 m for any ultimate, 5.0 m for any first
  skill and 3.5 m for any second, and offset the ring forward only for Cheska. Nine of twelve
  numbers disagreed with the ability they were drawn for: Dante's 2.4 m stomp drew 5.0 m, Nemu's
  3.2 m void drew 7.5 m centred on Nemu when it lands 3.5 m ahead. `TelegraphRadius` and
  `TelegraphRange` live on `HeroAbility` now and every pair is asserted against the spawn call
  in `TelegraphsMatchWhatTheAbilityPlaces`. The ring also survives 0.35 s past the cast, because
  every one of these fires on the press edge and the held ring was unreachable on a tap.

  **Every see-through effect in Hero Strike was opaque.** `CreatePrimitive` returns the
  built-in `Default-Material`, which is Standard in OPAQUE mode, and writing an alpha into
  `material.color` there does nothing. Thirty-odd effects authored at 0.25 to 0.92 alpha all
  rendered solid. Worst two: Sean's Supernova grew a solid 10.6 m sphere over the camera and
  popped out at full brightness, and Dante's Carapace put a solid sphere around his own head for
  four seconds of a nine second cooldown. `Visual.VfxMaterial` configures Standard for Fade
  properly (the five flags the material inspector normally writes) and 25 call sites go through
  it. It also strips the collider every decorative primitive arrives with: ice shards and
  volcanic debris shipped with rigidbodies AND colliders and were physically shoving players.

  **The hero UI was a different game's palette.** Seventeen `new Color(...)` slate-blue
  literals across `Hud.cs`, `AbilityInspectPanel.cs` and `ConvertedCharacterSelect.cs`, none of
  them in `UiTheme`, against `Art_Direction.md` § 1's "ui_theme.gd is the only place a colour is
  named". 🧑: *"i lowk dont get why we use light blue and shit in some parts of ui, it doesnt
  really look good with brown"*. All seventeen are gone; the chrome is six named constants
  derived from the wood set. Two hero accents were also sitting on the role hues (Dante four
  degrees off Offense, Cheska twenty off Defence) and the whole set was re-spaced.
  `TheHeroChromeIsTheWoodSet`, `NoHeroAccentSitsOnARoleColour`,
  `TheFiveHeroAccentsAreTellableApart`.

  **Four of fifteen ability descriptions were cut off mid-word.** Character select drew
  `Description` into a 46 px box with `VerticalWrapMode.Truncate`, silently, on the one screen a
  player uses to CHOOSE a hero. `Summary` is a short line for that box and `Description` is the
  full sentence for the tray, which no longer truncates. All fifteen were rewritten out of
  shouty marketing copy into plain sentences on request. `EverySummaryFitsTheCardItIsDrawnIn`,
  `EveryAbilityNameFitsItsHeaderRow`.

  Design and reasoning: `docs/Hero_Strike_UI.md`. 95 EditMode tests green, 56 core tests green.

- **The comic callouts were unreadable, and there really were ten of them.** ✅ 2026-08-23.
  🧑: *"they feel diff earlier and weird and overwhelming bcz like 10 show up at once and u
  cant read and they were a weird font"*. Three faults, none of them tunable:

  **The font really was wrong.** They were `TextMesh`, which draws off a font's atlas material
  and does not rebuild when that atlas does. Darumadrop is a dynamic font, so any other text
  requesting a new glyph at a new size re-packs the atlas and every live callout's UVs then
  point at other letters' pixels. Rewritten onto a world-space `Canvas` with a `Text`, which
  re-runs its own layout on `Font.textureRebuilt`.

  **They were blurry.** Rasterised at 64 px and drawn at about 112 screen px, so every glyph was
  being blown up nearly two to one. Now 110 px into 0.48 m plus a 2x dynamic scaler.

  **Ten really did show up.** The cap was 4 and evicted the OLDEST, so a hero exchange threw
  away the score callout and kept four flavour hits. There is a `Weight` now (Flavour, Cast,
  Score) and the LEAST important is evicted; duplicates within 0.35 s and 3 m kick the live one
  instead of stacking; flavour hits past 15 m are not drawn at all. Eight call sites that fired
  one callout per victim were cut: Dante's ultimate alone used to put five on screen in one
  frame.

- **The ability glyphs were a smudge at the size they are actually drawn.** ✅ 2026-08-23.
  A deck tile shows a 128 px sprite at about 40 px, and the old set was line art at 0.06 to
  0.09 stroke, which is one and a half screen pixels. All nine redrawn to one fat stroke
  (`AbilityIcons.Stroke`, 0.16), at most three elements each, solid mass over outline. The
  `Ring` and `Diamond` primitives went with them. `Logs/shots-hero/hero_glyphs_v1.png` draws
  every glyph at 128, 64, 40 and 24 px on the real plate colour; the 24 px column is the test.

- **The intermission banner was the loudest thing on screen and unreadable.** ✅ 2026-08-23.
  `ReadyObjective` was 32 pt of ALL-CAPS `UiTheme.Offense` across 900 px, over a sunlit
  asphalt court, which is both illegible and a role colour used decoratively. It is 20 pt cream
  on a dark plate now, with the role colour on the plate's rim, in sentence case. The ready
  prompt lost three of its four clauses.

- **Dante 3D Model Stray Geometry Fix.** ✅ 2026-08-23. Removed 1,340 stray vertex and triangle
  elements (islands 959-992) from `team-dante.glb` head mesh that formed an asymmetrical floating horn/spike
  protruding through the temple and rear of the character's head. Cleaned binary buffers and re-indexed
  mesh to restore symmetrical head bounds `[-0.195, 0.188]`.

- **In-Game Ability HUD Slimdown & First-Person Hand Clearance.** ✅ 2026-08-23. Redesigned `HeroDeck`
  in `Hud.cs` into a slim, minimalist 240x68 dark glass panel anchored at `y = 10` (down from 592x122 at `y = 24`).
  Replaced cramped 3-line text wrapping with centered high-DPI vector SDF glyphs, corner key chips (`[Q]`, `[F]`, `[X]`),
  and bold centered cooldown countdowns (`4.2s`). Moved `ReadyObjective` to top-center (`y = -210`) and
  `ReadyPrompt` to `y = 96` so game text never obstructs first-person hands.

- **Character Select Ability Ribbon & Tile Polish.** ✅ 2026-08-23. Polished `ConvertedCharacterSelect.cs`
  to style selected ability tiles with a crisp gold/accent glowing border over dark slate glass rather than
  a solid orange background fill. Expanded ability details card to an uncluttered 2-line tactical readout.

- **Ability VFX footprints, procedural particles & UI overhaul.** ✅ 2026-08-23. Calibrated all
  hazard footprints (Cheska Permafrost 2.3m / Barricade 3.2m / Nova 4.6m, Dante Stomp 2.4m / Fissure
  5.5m, Nemu Void 3.2m, Sean Supernova 4.8m, Zack Thunderstrike 4.5m). Introduced `AbilityVfx.cs`
  procedural ParticleSystem generators for ice bursts, magma eruptions, void wisps, and electric arcs.
  Overhauled `ComicPopup.cs` with comic font (Darumadrop One), ink outline layers, and punchy bounce.
  Redesigned `AbilityIcons.cs` with 128px high-DPI procedural vector glyphs and modern tactical shapes.
  Overhauled UI theming across Character Select, HUD Deck, and Inspect Panel, replacing solid bright
  orange tiles with sleek dark glass plates (`rgba(16, 22, 34, 0.90)`) with glowing white/accent glyphs.
  Redesigned Character Select Hero Loadout into a Valorant-style horizontal ability ribbon with an
  interactive details card, eliminating button collisions. Rewrote all ability descriptions across all 5
  hero kits into intuitive, action-driven tactical instructions (`ACTIVATE`, `DEPLOY`, `SLAM`, `PHASE`, `SURGE`).
  Fixed font blurriness by increasing dynamic TTF raster size to 32 and removing unnecessary outline overhead.

- **The 8 PARTIAL rows in `docs/Port_Ledger.md`.** ✅ 2026-08-23, audited against the code
  rather than against the rows. **Seven of the eight were stale bookkeeping**: the work had
  landed and the row was never updated. The audit table is in the ledger's status summary.
  Two pieces were genuinely missing and were written: the music **intensity lift** in the last
  15 s of a round, and the **duck-trigger table** that drops the bed under the countdown, the
  round end, the win and the score award. One row remains genuinely partial and is § 1 above.

- **Load every resource on the BH Studios loading screen.** ✅ 2026-08-23. The preload covered
  the roster, audio and the MAIN MENU scene, and then the arena, its materials, the baked UI
  sprites and the hero kits were all still cold when Play was pressed. It now also warms both
  maps through their assets, every `GodotTheme` box, every ability glyph, the input asset with
  the player's rebinds, and all five kits. `SplashScreen.PreloadGameAssets`.

- **Plan the whole keymap and put throw on left click.** ✅ 2026-08-23. Throw always WAS on
  left click; Grab was on it too, which is why it did not behave like it. There were four live
  collisions in total (left click carried Throw and Grab, E carried Grab, Lunge and Skill 1,
  Q carried Throw and Skill 2). Every action now owns exactly one control:
  left click throws or punches, E is the contextual pick up / shove / reset, right click
  lunges, Q and F are the skills, X is the ultimate, Tab holds the ability panel open.
  `InputMapAndAbilityTests` asserts no control is shared and that throw is on left click.

- **Redesign the skill UI.** ✅ 2026-08-23. Cards are an icon tile with the bound key on a chip
  in the corner; cooldowns drain a smooth bar and the ultimate fills a notched one, so the two
  quantities can no longer be confused; the deck moved out from under the practice prompt; key
  labels come from the live bindings. Descriptions moved off the HUD entirely into a
  hold-to-read panel, and character select now shows every power with its icon, its kind and
  its sentence.

- **Ultimate charging during the ready screen.** ✅ 2026-08-23, and the requirement changed
  mid-flight. Charge now PERSISTS across rounds and is frozen whenever the round clock is not
  running; during the warm-up and the between-round buffer the ultimate is free to cast off a
  practice counter, so it can be rehearsed without spending the meter or earning one by
  waiting. Cooldowns still run in practice, deliberately.

- **Organise the settings controls into groups.** ✅ 2026-08-23. Four headed sections
  (Movement, Playing the game, Hero powers, Round and screen) instead of fourteen unlabelled
  rows. `SettingsGroupsCoverEveryActionExactlyOnce` asserts nothing can fall out of the panel.

- **Hero Strike unretrieved-slipper penalty variance.** ✅ 2026-08-23. Two causes, both
  measured. The probe was unseeded, so the same build measured 110 and then 467 penalties on
  consecutive runs either side of its own 200 ceiling; it is seeded now. And bots walked
  straight through hero hazards on the way to a tsinelas, so `HazardMap` and
  `AIController.AvoidHazards` steer around them. Hero Strike now measures 77 throws, 77
  retrievals, 182 skill uses, 21 ultimates and **1** unretrieved-slipper penalty in a match.
  ⚠️ The avoidance is capped at 3 m until § 1 lands; see § 1.2.

- **The stun frost is very strong.** ✅ 2026-08-23, reduced. Reach 0.36 to 0.24 screen heights,
  body alpha 0.36 to 0.30. Both opposite edges spend the reach, so at 0.36 the clear strip left
  in the middle was 0.28 of the screen height for a five second stun. Still worth a look in a
  real match: § 1.5.

- **The preview idle pose vs the Godot reference.** No need. The character preview was reworked
  in a separate pass; the arms-crossed mismatch in `ModelPreview.PlayIdle` is not being chased.

- **Character-Specific Viewmodel Arms for All Heroes and Classic Characters.** ✅ 2026-08-23.
  First-person viewmodel arms dynamically match each character's TPP model, palette skin tone,
  sleeve cuts, arm markings/tattoos, watches, wristbands, and elemental accessories across all 5
  Heroes (Sean, Zack, Dante, Cheska, Nemu) and all 12 Classic characters (Berto/bayan, Maring,
  Totoy, Inday, Kuya Boy, Ate Girlie, Tikboy, Bebang, Jun-Jun, Lola Pacing, Mang Kanor, Aling
  Nena). Held slipper parenting and all 15 bespoke hero ability animations preserved. Verified
  by 100 EditMode tests, 55 PlayMode tests, and 56 Core tests.

- **Map "Ilalim ng Tulay" (LRT Gilmore Strip).** ✅ 2026-08-24, rebuilt from the cross section
  out. It shipped on 2026-08-24 and every one of the faults below was in that build; the map
  now has a design document, `docs/Ilalim_Ng_Tulay.md`, and a check that refuses it,
  `MapGeometryCheck`.

  **The geometry was wrong in ways four signed-off renders did not show.** `MapGeometryCheck`
  measured **62 findings** on the shipped scene and **0** on this one:
  - Both pavements floated **0.15 m over open air**: 40 plaza tiles with nothing built under
    them. Five buildings, a pole, the pares cart and the PC Express storefront stood on nothing
    at all, 1.5 m past where the ground stopped.
  - Every prop on either pavement was **sunk 0.062 m into it**, because the placement height was
    the plaza tile's ORIGIN and not its TOP. Everything is placed through `SurfaceTop(x)` now and
    the builder holds no typed-in heights.
  - All 50 kerb tiles were laid **across** the carriageway instead of along it, because
    `env_kerb_tile` is 2.0 m on local X and 0.35 m on local Z and the street runs along Z. Those
    are the loose pale slabs strewn over the road in `ilalim_thrower_view.png`.
  - The map **ended in white sky** in every direction a metre past the pavement. There is a
    240 m ground plate now. The road and supported pavements continue to the fog limit, with
    car-kit traffic, background intersections, corner shops and a lower mid-rise belt instead
    of the later cross rows that made the carriageway look built against a wall.
  - All four **utility poles were yawed the wrong way**, hanging their 6.6 m wire spans out over
    the back lots instead of over the street.
  - The collision floor was one flat plane at y = 0 while the pavement was drawn 0.212 m up, so
    every body walked through both pavements to the shin.

  **The rules geometry was wrong, and that mattered more.**
  - There was **no chalk box at all**. It drew a "throwing line" at z = 3.0 and a "base circle"
    at z = 13.5, neither derived from anything, while the can spawns at the world origin: the
    circle was **13.5 m from the can it was drawn for**. All of it comes from
    `Balance.ConfinementRadius` and `Confinement` now.
  - Two 3.4 m **viaduct columns stood inside the box** at z = -5.0. The taya is clamped in there
    and cannot step out to walk around one. Both live rows are outside |z| = 7 now; 1.4 m kit
    pillars at x = +/-4.45 leave a measured 7.5 m centre gap and 1.85 m kerb gaps.
  - A **trip hazard was centred on the world origin**, which is where the can spawns and where
    every retrieval in the match converges.
  - The **overclock pad was inside the PC Express collider** and could not be reached.
  - The PC Express collider itself reached **2.1 m into the carriageway**.

  **It did not look like the same game, and the cause was one word.**
  `EnvColourPass.DressingRoot()` looks for a child named exactly `Dressing`; the map put
  everything under `Geometry`, so the pass walked nothing and repainted nothing while both other
  maps were getting the seeded Manila palette, the roof atlases, the road correction and the
  belt fade. The map was not using a different palette, it was using no palette. Groups are
  named `Kalsada`, `Slab` and `Malayo` now. The hand-built near blocks are gone; commercial,
  industrial, roads, train, factory and car kits use complete generated warm atlas replacements
  so their orange and blue source swatches never become role-colour decoration. The showcase
  probe also runs `EnvColourPass.Apply()` before rendering, without which an edit-mode capture
  shows raw materials.

  **PC Express is the shop it is named after.** It shipped as a green lightbox with two blank
  white slabs and a green-white-red awning. `PcExpressSignAuthor` now builds the deep red-blue
  lightbox and metal return from the supplied real exterior. The official horizontal artwork
  is traced by `build_pc_express_logo_mesh.py` into smooth raised white letters with the real PC
  monogram, italic X and one clean blue channel return. The registered-mark badge is omitted
  because it is not mounted on the real storefront. Glass mullions, centre doors, kick plate and
  a slim overhang replace the market awning. Both authoring tools are idempotent.

  **New for Hero Strike**, all of it argued in `docs/Ilalim_Ng_Tulay.md` § 3:
  - The chalk box IS the carriageway, so a player reads the danger zone off the kerb line.
  - **4.2 m of legal standing room** outside the box on each long side against Eskinita's 1.6 m,
    which is the measured reason 🧑 reported both existing maps as *"weird to play abilities
    gamemode on"*. `ArenaCheck` bound 3 now clears its wall by 2.6 m instead of by 0.0.
  - The **train pass is a mechanic**: `OverheadPassWindow` doubles ability COOLDOWN rate (only
    the cooldown, never the ultimate charge) for the measured 2.70 s the consist is overhead,
    every 24 s.
    Classic gets Street Hype and the spectacle instead, per `VISION.md` § 1.1.
  - The **bridge hoop**: a tsinelas through the ring fires "TRES!" and Street Hype, and awards
    no score, because `MatchDirector.AddScore` stays the only place a point is made.
  - "BAWAL UMIHI DITO" on the column faces, two potholes off the spawn-to-can line, and clutter
    that is all on the pavements.

  **The final composition pass removed the set edge.** The road and pavements now reach the
  120 m plate and disappear into fog; 26 joined wire spans sit on 28 single shopfront-edge
  posts; the side gaps contain dense second and outer building rows, industrial tanks and far
  intersection blocks; the pisonet, PC-repair, pares, regulatory and civic signs each use a
  different silhouette and mounting. Boundary cars remain wholly outside |z| = 16.5.

  Verified by 56 core tests, 105 EditMode, 55 PlayMode, `HeadlessCheck`, `ArenaCheck`,
  `AudioCueCheck`, `MapGeometryCheck` at 0 findings including every elevated join, eight v14
  in-engine renders in `Logs/shots-ilalim/`, idempotent palette/logo/sign generators, the model
  island checker, and a clean Windows player build smoke-launched from the Desktop.
