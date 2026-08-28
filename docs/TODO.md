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

✅ **CLOSED 2026-08-26. Both remaining items, and the second one found two real defects on the
first frame it could finally photograph.**

1. ✅ **The three ultimates' own shapes were already built, and this entry was stale.** Re-read
   against the code rather than against the entry: `ExplosionLook.Core` returns
   `VfxShapes.Shockfront` for Quake (a leading edge, not a ball), `VfxShapes.NovaShell(5, 9)` for
   Frost and `NovaShell(6, 10)` for Fire, and `CreateThunderstrike` draws a `VfxShapes.Spire`
   with a `VfxShapes.Star` ground discharge. Not one of them is a `PrimitiveType` any more. The
   work landed with the § 8.5 volume pass; nobody updated the item.
2. ✅ **`AbilityShowcaseProbe` photographs the transients now**, which is what "none of this has
   been seen" actually needed. Two things were in the way and both were fixed rather than worked
   around:
   * `CreateExplosion` opened with `if (round == null) return;`, so in an edit-mode capture it
     drew **nothing at all**. `HeroHazards.CreateExplosionVisual` is now the half that puts
     pixels on screen and needs no match; `CreateExplosion` calls it and then resolves damage.
   * `Update` never runs in edit mode, so a spawned blast froze on its first frame at scale 0.35.
     `Visual.IVfxTimeline` lets a capture wind an effect to any moment of its own life, and
     `VfxTimeline.StepAll(0.35f)` winds every one to the same FRACTION (a core runs 0.5 s and its
     wave 0.4 s, so a shared number of SECONDS would photograph them at different moments of one
     event). ⚠️ The player's frame and the capture's frame come from the same body: each
     implementer's `Update` is one line calling `StepTo`.

⚠️⚠️ **AND THE FIRST TRANSIENT CAPTURE IMMEDIATELY FOUND TWO SHIPPED DEFECTS, WHICH IS THE
ARGUMENT FOR THE WHOLE EXERCISE:**

**8a ✅ FIXED: every ground shockwave in the game was drawn at DOUBLE its size.**
`ShockwaveRingAnim` read `Mathf.Lerp(0.5f, TargetRadius * 2.0f, t)`, which was correct while the
ring was a `PrimitiveType.Cylinder` (one unit ACROSS, so a scale of 2R gives a radius of R). The
§ 8 silhouette pass swapped in a `VfxShapes` mesh, and every one of those is built at one unit of
**RADIUS**, so the same line started giving 2R. `ExplosionVfxAnim.MeshRadius` carries a long note
about exactly this trap and the core was fixed at the time; the ring was missed. Measured off
`ability_blast_thunder_v8.png`: Sean's Supernova ring reached **26.9 m across in a 14 m box** and
Zack's Thunderstrike star **42 m**. Restoring the divide puts the final radius back on
`TargetRadius`, which is what the cylinder drew and what every footprint in
`Hero_Strike_Balance.md` § 1 was measured against, so this is a regression fix and not a balance
change.

**8b ✅ FIXED: Thunderstrike whited out the street, and now there is a number that says so.**
Measured on the v9 set: **62.8 per cent** of the overhead frame and **49.9 per cent** of the
eye-height frame were at or above 245/255 luminance, against **8.3 per cent** for the worst of
every other effect and **3.0 per cent** for the empty street. The road markings themselves were
gone, which is `VISION.md` § 2 rule 5 failing outright. Cause: a point light at intensity 6.0
over a 17.5 m range in a 14 m box (the fire blast uses 5.5 over 12.5 m), plus a near-white ion
spire sitting inside it. Now 3.0 over `radius * 1.6`, and the spire keeps its hue at a lower
value. **After: 6.5 per cent overhead and 3.6 at eye height**, the loudest of the five rather
than seven times over everything.

⚠️⚠️ **AND RULE 5 IS A GATE NOW RATHER THAN AN OPINION.** `AbilityShowcaseProbe.MaxBlownFraction`
is **12 per cent**, and the probe FAILS a run where a transient frame exceeds it. The bound is
measured, not picked: everything the team has already accepted fits under 9 (empty street 3.0,
corridors 3.0, the deliberate worst-frame pile-up 4.1, frost 8.3), and the one defect sat at
62.8. It measures Rec. 601 luminance rather than the max channel, because a saturated blue at
full strength is a colour and white is an absence of picture.

**Verified:** the `v10` set in `Logs/shots-abilities`, every transient under the bound.

⚠️ **What is still a human call:** whether the five now read as five different events in motion,
in a real round with four seats. A still frame cannot answer that and this entry never claimed
it could.

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

## 12 · Everything 🧑 found playing the 2026-08-26 build ✅ ALL CLOSED SAME DAY

**Reported from the built player in one sitting. Grouped because they came from one session, not
because they share a cause.**

**12.1 ✅ The fall resolved itself, and mashing hit a wall two thirds of the way through.**
🧑: *"if i mash it, the progress pauses"*, and *"if i dont mash, i get up in 2 seconds wtf"*.
Two symptoms, two separate causes, both arithmetic:

* **The pause was `MinTripDown` = 0.90.** `Combat.MashRecover` clamps there, so a 2.50 s fall had
  1.60 s a press could buy and **0.90 s, over a third of the event, in which every press was
  refused**. A fast masher spent that third hammering a dead button while the bar crawled at the
  passive rate. The floor was 0.90 because the note said the knockdown clip needed it; that
  stopped being true when `StepTripPose` made the knockdown a separate held phase that ENDS at
  the floor and time-scales the get-up to fill it. The only thing the floor must now cover is the
  get-up animation, and every clip on every rig is **0.333 s**, so it is **0.35**. Presses count
  for **86 per cent** of a fall instead of 64.
* **Getting up early was the stagger.** `ApplyTrip` calls `ApplyStagger(duration)` once, with the
  trip's STARTING length, so the stun ran down at real time while the trip ran down at
  `TripPassiveDecayRate`. An unanswered fall lasted 3.22 s and control came back at 2.50: for the
  last 0.72 s the body could walk, aim and throw while `IsTripped` was still true, the camera was
  still in the fall view and the HUD still said GETTING UP. `CharacterMotor` now holds the
  stagger to the trip with a `Max`, never an assignment, so a 5 s tag landing on a downed player
  is not cut short to the remaining fall.

**The numbers, solved together:** slack 2.15 s, `MashRecoverPerPress` **0.35**,
`TripPassiveDecayRate` **0.75**. Answered perfectly: **0.96 s**, inside the 1 to 2 s asked for.
Ignored: **3.22 s**. Answering a fall is worth **3.3x**.

**12.2 ✅ A trip hazard could swallow a tsinelas for the rest of the round.**
🧑: *"if slippers falls there i cant get close enough to get it back"*. Exact, and not bad luck:
`Balance.PickupRadius` is **1.40 m** and the widest hazard footprint is **2.60 m**, so a slipper
resting near the middle of one cannot be reached from outside it. Walking in costs a trip
(`MinSpeedToTrip` is 1.0 m/s and there is no slow-walk binding) and the trip puts you back out.
The slipper is then unrecoverable and its owner takes the unretrieved-tsinelas penalty every
second for the rest of the round. `StreetTripHazard.EjectSlipper` pushes a **resting, loose**
slipper out along its **shortest** exit. ⚠️ Shortest, not toward the can or the owner: a hazard
that nudged ammunition somewhere useful would reward throwing into it.

**12.3 ✅ A charge against a down lata could be neither spent nor cleared.**
🧑: *"my charge still pauses when lata is down"*, *"i dont want it to pause"*. See
`docs/Design.md` § 2 and `Design_Drift_Report.md` § 9: the upright-lata condition is removed, it
is a deliberate rule change rather than a drift, and it is safe because a slipper reaching a
downed lata cannot score and the reset channel is protected by two other mechanisms that exist
precisely because this clause never covered an airborne slipper.

**12.4 ✅ The taya had no crosshair.** See § 3, where the played frame that showed it is also the
frame that answered the hero-accent question.

**12.5 ✅ `ATTACKER  ·  YOU` on the scoreboard.** See § 3.

**12.6 ✅ Three silent sounds, found in the player log rather than by playing.**
`[Audio] no cue registered for 'ui_move'`, once every 24 s, all match: `LrtTrainFlyby` called a
cue that has never existed, so **the map's signature recurring event has been silent for its
whole life**. `AudioCueCheck` could not see it, because both of its directions started from the
declared cue list and neither ever looked at a CALL SITE. Direction 3 does, and on the first run
it found four more; two were the music bed being misread and are excluded by anchoring the
pattern on `Audio`, and two were real: **every trip in the game** (`StreetTripHazard` fired
`"shove"`, which is the input verb's name and not a cue) and **the bridge hoop bonus**
(`"sfx_lata_hit"`, which has no file). `tools/generate_ability_audio.py` gained
`sfx_lrt_pass`, built to `OverheadPassWindow.PassSeconds` = 2.70 s so the sound is exactly as
long as the pass; the other two now use `hit_body` and `score_award`.

⚠️ **`round_end` also logged an FMOD "File not found" twice per round end.** All 59 clips in
`Resources/Sfx` shipped with `preloadAudioData: 0`, so each loads on first play and a cue fired
twice in one frame can race its own load. They are preloaded now; the bank is a couple of
megabytes.

**12.7 ✅ It did not crash.** 🧑: *"the game i had just closed, idk if it crashed or u closed it"*.
`Player.log` ends cleanly on `[Slice] round 2 begins, taya is seat 1` with no exception and no
stack. It was closed.

⚠️⚠️ **12.1 AND 12.2 WERE REPORTED AGAIN OFF THE VERY NEXT BUILD. See § 13.** Both fixes were
argued from the source and neither was measured against a running game, and both were wrong in a
way no amount of re-reading the arithmetic would have found. That is the whole lesson of § 13.

---

## 13 · Everything the 2026-08-26 evening build showed, and the pattern in it

**Six reports in one sitting. ✅ ALL CLOSED. Three of them were things this file already claimed
were fixed**, which is the part worth reading: each had been reasoned from the source, none had
been measured, and each was wrong for a reason the source could not show.

⚠️⚠️ **THE PATTERN: A FIX ARGUED FROM THE CODE IS A HYPOTHESIS, NOT A RESULT.** The hero picker's
gap was "fixed" three times; the tsinelas ejector was written against a callback that could never
fire; the trip loop was answered with two clocks when the fault was geometric. In every case a
single probe run would have said so in seconds. `HeroPickerLayoutProbe` and
`InputEdgeTests.MashingShortensAFallByWhatBalanceSays` exist so the next reader inherits the
measurement rather than the argument.

**13.1 ✅ The get-up bar was a countdown wearing a mash meter's clothes, and § 12.1 did not fix
that.** 🧑: *"progress bar increases on its own when u trip (not supposed to happen) and if i
mash, the progress pauses (opposite of what i want)"*. § 12.1 changed the ARITHMETIC of the fall
and left the bar reading `1 - TripLeft / TripTotal`, which is elapsed time: it filled at the
passive bleed whatever the player did. A second gold fill was added to explain that, and it did
not help, because the thing being explained was still a clock.

`Hud.UpdateGetUpPrompt` now draws **one** bar and its only input is `CharacterMotor.MashRemoved`,
written in exactly one place by a press `Combat.MashRecover` accepted. Denominator is the fixed
mashable slack, `TripTotal - MinTripDown` = **2.15 s**: react in 0.25 s and mash cleanly and it
reads **91 per cent**; never press and it reads zero and you stand up anyway. Nothing but a press
can move it by a pixel.

⚠️⚠️ **AND THE OTHER HALF WAS A REAL DEFECT THAT NO AMOUNT OF READING FOUND: A PRESS BELOW THE
FLOOR MADE THE FALL LONGER.** `Combat.MashRecover` clamped its result UP to `MinTripDown`
unconditionally. Inside the floor — the last 0.35 s, the get-up clip, where `tripLeft` is already
BELOW `MinTripDown` — `reduced` is negative, the clamp fires, and the function **returns a larger
number than it was given**. A player still hammering the key during their own get-up reset the
fall to the floor on every accepted press, at up to 10 Hz, and could not stand up for as long as
they kept mashing. That is *"if i mash, the progress pauses"* literally, mechanically, and it
survived the § 12.1 pass because that pass changed the CONSTANTS and this is a comparison.

`InputEdgeTests.MashingShortensAFallByWhatBalanceSays` found it on its first run: **105 accepted
presses against a 2.50 s trip, 36.75 s of nominal recovery bought, still on the floor at 12.00 s.**
A press at or below the floor is now REFUSED rather than clamped, and reports `accepted: false` so
the HUD does not pop for a press that bought nothing. Two Core tests hold it:
`Mash_NeverLengthensAFall` asserts the property from seven starting points, and
`Mash_IsRefusedOnceTheFloorIsReached` asserts the rule.

⚠️ **The INPUT path was never broken, and that is now asserted rather than believed.** The same
test first checks that one held Jump produces exactly one accepted press through the real physics
step, then measures the ratio: an answered fall must be at least 1.6x shorter than an ignored one.
The two are separate assertions on purpose, because a dead Jump edge and a mis-tuned constant both
come back as "the fall was too long" and only one of them is a bug. The bound is a RATIO because
both constants behind it are open balance questions.

**13.2 ✅ `StreetTripHazard.EjectSlipper` was unreachable code, so § 12.2 shipped nothing.**
🧑, reporting the same fault again: *"if slipper drops there i cant get it anymore i perma trip"*.
A tsinelas in this game has **no `Collider` and no `Rigidbody`**: `MatchInstaller.BuildSlipper`
strips the model's colliders and `Slipper.FixedUpdate` integrates the flight by writing
`transform.position`, which is the same "contact resolves by distance, never by a trigger volume"
rule the whole game is built on. Unity fires no trigger callback for a collider-less object, so
`OnTriggerEnter` and `OnTriggerStay` were only ever going to deliver the PLAYER, whose
`CharacterController` does generate them. The ejector read as correct and could not execute.

It is a **poll** now: `SweepSlippers` at **5 Hz**, sharing one `FindObjectsByType` across the
whole hazard field through a static cache. 0.20 s is 0.69 m of attacker travel, less than half
`Balance.PickupRadius`, so the tsinelas is out before anyone could have reached it.

**13.3 ✅ Trip spam: one trip per visit.** 🧑: *"sometimes i am trip spammed, the moment i get out
of trip i trip again"*. Neither existing clock could answer this and both look like they should.
`Balance.TripGraceAfterGetUp` is 1.20 s on the BODY and exists for the neighbouring hazard 2.6 m
away; `StreetTripHazard.Cooldown` is 3.5 s per motor per hazard and starts when the trip BEGINS,
so an unanswered 3.22 s fall plus 1.20 s of grace has already outlived it. The fault is
geometric, not temporal: `ApplyTrip` zeroes the horizontal velocity, so the player gets up
STANDING IN THE FOOTPRINT and the first step out clears `MinSpeedToTrip` from a standing start.
A `_spent` set, cleared on `OnTriggerExit`, makes a hazard something you RUN ONTO.

**13.4 ✅ The tutorial was a match with a card over it.** 🧑: *"why is there a timer and rounds too
in tutorial"*, *"make it an actual dedicated tutorial not js a copy pasted shit from the game"*,
*"ui for it really sucks"*, and *"make sure this shit doesnt affect the actual game"*.

* `Hud.StripToTrainingChrome` takes the clock column, the ROUND x / 8 line, the scoreboard, the
  lata card and the Street Hype deck off a guided run, and `MatchInstaller` skips `RoleSwapCard`
  and `YouCard`. Everything a lesson TEACHES stays: stamina, crosshair, hero deck, inspect tray,
  the get-up card. ⚠️ The flag is `GameLaunch.GuidedTutorial`, false on every path into a real
  round, and two of those readouts decide their own visibility every frame, so deactivating them
  at build time alone would have lasted exactly one frame.
* `GuidedTrainingHud` is rebuilt: a route rail of one pip per lesson, and controls drawn as KEY
  CAPS parsed out of the `[...]` tokens `GuidedTraining.Key` already writes.

**13.5 ✅ `[2DVECTOR(MODE:2)]`.** 🧑: *"wtf is 2d vector modee?"*. `Hud.KeyLabel` read
`act.bindings[0]`, which for a composite action is the composite HEAD: its `effectivePath` is the
composite's type string, not a control path. Fixed at the source rather than at the one screen
that showed it, because `docs/VISION.md` § 3 makes that function the single origin of every key
the game teaches. Composites now render their PARTS in reading order, so `Move` prints **WASD**
and not WSAD, and a rebind to IJKL prints IJKL.

**13.6 ✅ The tutorial beacon was a 5.2 m pole.** 🧑: *"what are these big ass lines"*. A
`PrimitiveType.Cylinder` is two units tall, so `localScale.y = 2.6` draws 5.2 m and the pulse took
it to 5.7. In an FPP game with a 1.6 m eye height, a marker met at three metres put its own top
off the top of the frame: the beacon was standing in front of the thing it was pointing at. It is
a 0.70 m ground ring and a pip bobbing at 1.05 m now, with the point light cut from 4.0 m at 3.0
to 2.6 m at 1.8.

**13.7 ✅ The hero picker's dead band, fixed on the fourth attempt and the first measurement.**
🧑: *"fix ui here, theres big open space"*, the same screenshot as 2026-08-25. `HeroPickerLayoutProbe`
answered it in one run:

```
TaglineLabel  h=96  LE(on=True, min=96, pref=46, prio=1)
```

The preference had been 46 for a day. **`LayoutUtility.GetPreferredHeight` returns
`Max(minHeight, preferredHeight)`**, and the 96 px floor comes straight from the .tscn's
`custom_minimum_size.y` through `TscnUiImporter`, authored for a three-line Classic tagline in a
panel that had no ability rows under it. Three passes wrote the preference and none wrote the
floor. Both are written now, always, and they always agree. **Measured after: box 56 px around
51 px of text, slack 5.** Forty pixels of wood returned to the ability rows, and the probe fails
on any slack over 28.

**13.8 ✅ NONE in the practice lobby, and nobody else in the street.** 🧑: *"add None as an option
there and make it so that theres actually no bots ... just you there no bots"*. `MatchInstaller`
does not BUILD the other three seats; disabling three `AIController`s would have left three
parked bodies on the attacker line, still scored and still on the board. ⚠️ NONE is index **3**,
appended after HARD rather than prepended before EASY, because `GameSettings.AiDifficulty` is a
saved int that `MatchRpc` also replicates and `(Difficulty)` is cast straight off it. ⚠️ Offline
only: a seat is what a peer joins. Three follow-on guards were needed and each is a real hole:
`SliceRunner.EquipOwnedSlippers` maps tsinelas to attackers BY INDEX and would have handed slipper
0 to a body that is not there, `RoundDirector.StepPassiveDefence` paid +10/s into an empty chair,
and `Hud.UpdateScores` printed three rows for seats with nobody in them.

⚠️ **The guided route keeps its cast whatever the lobby says.** It parks the other three itself
and stands one up as the dummy the shove, punch and lunge lessons need.

**Still open on this entry:** nothing, but the NONE path has been measured only by the checks and
by reading. It wants one played round to confirm the empty street feels like a range rather than
like a broken match.

---

## 14 · The 4.69 player's second batch, shipped in `349b0171`

**Six reports in one sitting, all closed the same day, and the entry exists because
`CLAUDE.md` § 2.3 asks for the account to live here rather than only in a commit message.**
The commit has the full derivation; this is the short form plus the numbers, in the same shape
as § 13.

**14.1 ✅ The mash was a tap.** 🧑: *"mash is weird now, I js have to click it twice to get up im
not fr mashing"*. He was measuring correctly. At `MashRecoverPerPress` 0.35 the 2.15 s of
mashable slack was **6.1 presses**, so two of them plus a second and a half of passive bleed had
him up: the bleed was doing most of the work in any real fall, which is the same complaint as
*"it automatically resolves"* in different clothes. 0.22 makes the slack **9.8 presses**, which
is 0.98 s of hammering at the 10 Hz cap and **1.33 s** on the floor. `TripPassiveDecayRate`
0.75 → 0.60 is the smaller half and stops "press twice and wait" being a strategy: ignored is
**3.93 s** against 1.33 s answered, worth **3.0x**.

⚠️ **Superseded by § 15.1 on 2026-08-26.** He played it and reported the fall still ending on a
clock. The constants above are still the constants; what changed is that the clock no longer
ends a fall at all.

**14.2 ✅ "Cant pick up any slipper", in every mode, and the mechanism was not broken.**
`SoloPracticeTests` puts a loose tsinelas at a seat's own feet and the grab connects, so what he
met was REACH. `Balance.PickupRadius` is a 3D distance from the motor's transform, which sits at
the SOLE OF THE FOOT, while the camera is at about 1.6 m and pitched down: at 1.40 m a legally
grabbable tsinelas sits near the bottom edge of the frame and anything visible at the crosshair
is three or four metres out and refuses silently. **1.75 m**, still well under the 2.60 m hazard
footprint § 12.2 turns on.

⚠️ **The real fix is the prompt.** `Hud.UpdatePickupPrompt` says when a tsinelas is in reach, and
asks `Slipper.CanBeGrabbedBy` rather than its own distance check, so it cannot promise a pickup
the carrier would refuse. A silent refusal is indistinguishable from a broken key, which is
exactly how this was reported.

**14.3 ✅ The hazard sweep no longer takes a tsinelas out of somebody's hands.** § 13.2's poll
moved slippers without asking anybody, so a player closing on one would have had it teleport as
they arrived. The guard is `PickupRadius` itself: if a body is close enough to grab it, the grab
is the answer and the ejector has nothing left to fix.

**14.4 ✅ The tutorial is a sequence, not a sandbox with a card over it.** 🧑: *"i can still do
everything at lesson one"*. `InputIntent.AllowOnly` locks the seat to the verbs the route has
taught. Cumulative, because the retrieval run wants sprint and jump and the trip lesson is
answered with the jump key; what is removed is running ahead. ⚠️ It defaults to **null** so no
match is affected, and it is released in `OnDestroy` because `InputIntent` belongs to the SEAT
and outlives the route.

**14.5 ✅ The other three seats and their tsinelas are off for the whole route**, and one comes
back for the shove, the punch and the lunge. See § 15.2: their PET did not go with them.

**14.6 ✅ The training card had dead space and covered the ability deck.** Both were layout. Six
rows at hand-written offsets inside a fixed 274 px box is a hole for a short title and an
overdraw for a long body, and nothing in the code said so; it is a `VerticalLayoutGroup` under a
`ContentSizeFitter` now, so dead space is impossible by construction. The route controls moved
into the card as its last row because bottom-centre is the ability deck's lane.
`TrainingCardProbe` photographs the REAL card at four lesson shapes (`Logs/shots-training`).
⚠️ **Use it before changing that card again.** `CLAUDE.md` § 6.1 was written about models and is
just as true of a card that has now been rejected twice on its layout.

**Also in that commit, and worth keeping:** `BotBehaviourProbe` carries an explicit **420 s**
timeout (it measures 170 to 174 s against NUnit's 180 s default) and `SoloPracticeTests` ends its
match in `TearDown`. Both are the same lesson: a PlayMode test that leaves a live round poisons
the next suite, because the directors are `DontDestroyOnLoad`. `LandedHighlightTests` failed that
way twice and passes alone.

---

## 15 · The 4.70 tutorial batch, and why four screenshots were one probe apart

**Seven reports off the played 4.70 player. ✅ ALL CLOSED.** Three of the four things he
photographed were objects nobody could name from the pixels, and every one of them was named in
a single probe run. `TrainingStreetProbe` is that probe and it is the lasting part of this entry.

⚠️⚠️ **THE PATTERN, AND IT IS § 13'S ONE LEVEL UP: A REPORT ABOUT A PICTURE NEEDS A PROBE THAT
NAMES OBJECTS, NOT A CLOSER LOOK AT THE PICTURE.** `FppFrameProbe` exists for exactly this class
of report and could not have caught any of these: it skips every path containing "Slipper",
which is what two of them turned out to be, and it never loads the tutorial. The new probe walks
the route lesson by lesson, prints every renderer within two metres of the eye with its viewport
position and its WORLD SIZE, and prints every tsinelas with its state, its holder and its
clearance off the road. The four answers fell out of one run in three seconds.

**15.1 ✅ The mash was still a clock, and the fix was to delete the clock.** 🧑: *"mashing still
weird, u randomly get up after set amt of time, i dont have to actually mash it"*, and *"i want
it so that i can only get up when ive reached the end of the mashing shit bcz sometimes i get up
with it still at middle or when i only clicked once"*.

Three passes had answered this by retuning a decay RATE (§ 12.1, § 13.1, § 14.1) and each left
the property he was describing standing: **while a rate above zero exists, time ends the fall
and the meter decorates a countdown.** `Balance.TripPassiveDecayRate` is **deleted**. Above
`MinTripDown` only an accepted press moves a fall; below it, where the get-up clip is playing,
it runs at real time so the animation and the clock agree.

`Balance.TripAutoRecoverSeconds` = **5.0** replaces it as a STRANDING GUARD, not a second way
up: a perfectly answered fall is 1.33 s, so it is 3.8x that and 5.1x the 0.98 s mash window, and
nobody waits five seconds on the road to save ten presses. ⚠️ **When it fires it credits the
whole remaining slack to `MashRemoved`**, so the invariant a player can see holds with no
exception: **you never stand up with the bar part-full.**

Held by `Trip_OnlyPressesEndAFallInsideTheGuard` in Core and by
`InputEdgeTests.MashingShortensAFallByWhatBalanceSays`, which now asserts three things rather
than one: an unanswered fall lasts the guard (not a decay), the meter reads full at the moment
of standing on BOTH paths, and answering is still worth more than waiting.

**15.2 ✅ *"the pet of nemu is here??"* in a street with nobody in it.**
`GhostPetCompanion.Bind` unparents Kuro to the scene ROOT on purpose, because he lags behind
Nemu and must not inherit her transform. The consequence nobody had traced: every path that
hides a SEAT leaves the pet floating in the street on his own, and `GuidedTraining.HideTheCast`
hides three of them. The pet now mirrors its owner's `activeInHierarchy` every `LateUpdate`.
⚠️ **Renderers, not `SetActive`**: deactivating the object stops the `LateUpdate` that is the
only thing that could bring it back.

**15.3 ✅ *"theres a floating slipper check ss"*: the route was pointing at somebody else's
tsinelas.** `SliceRunner.EquipOwnedSlippers` **rewrites `OwnerSlot` every round**, walking the
attackers in seat order and handing them `Slippers[0]`, `[1]`, `[2]`, so with seat 0 as the taya
the local seat 1 owns SLIPPER 0. `GuidedTraining.Configure` asked one frame EARLIER, matched on
the pre-round ownership, and got slipper 1. `HideTheCast` then switched off the tsinelas that was
really in the player's hand and KEPT the other one, which was in a hidden seat's hand: measured
at **0.85 m over an empty road**. Resolution moved after `_runner.Begin()` and asks the CARRIER
first, which cannot be wrong.

**15.4 ✅ *"i can pick up slippers from ppl's hands wtf?"*, and this one is a MATCH bug, not a
tutorial one.** `Slipper.HostForceEquip` wrote `Holder` and told the NEW carrier and nothing
anywhere told the old one. `Carrier.RideAnchor` writes `Held`'s transform every `LateUpdate` and
asks nothing about the slipper's STATE, so a carrier never told it had lost one keeps dragging
it: the shoe hangs at hand height wherever that body goes, and because it is LOOSE it lights up,
prompts, and can be taken. Measured on the punch lesson: a LOOSE tsinelas resting **0.91 m** off
the road in the dummy's hand. Every write of `Holder` now goes through one
`ReleasePreviousHolder`, which also refuses to touch a carrier that has since picked up a
different shoe. ⚠️ `HostForceEquip` is the ROUND-START ARMING and runs in every match, so this
was reachable outside the tutorial.

**15.5 ✅ *"wtf is this yellow shit on me"*: the objective marker is a 2 m ball.**
`VfxShapes.Lay` scales X and Z by the radius and **leaves Y at 1.0**, because every other caller
hands it a flat mesh. `NovaShell` is a unit SPHERE shell, so the marker drew a translucent amber
ball **1.40 by 2.00 by 1.39** standing on its target, half of it under the road. It is
`Crystal(22)`, the flat fan the hazard footprints use, and the probe now fails any marker
renderer over 0.60 m tall. ⚠️ **This is the second oversized marker on this entry's route:**
§ 13.6 replaced a 5.2 m pole with what its own note called a ground ring, and the ring it reached
for was a ball. Two halves of the same lesson about drawing a pointer from a description.

Two supporting fixes came out of the same measurement: the marker hides itself inside **1.10 m**
of the eye (a pointer you are standing in is a wall), and the retrieval lesson **puts the shoe on
the road** rather than binding the marker to the player's own hand. That last one also made the
lesson completable: skipping the throw with N arrived at RETRIEVE still holding the tsinelas, and
you cannot pick up what is already in your hand.

**15.6 ✅ The kit is off screen until the lesson that teaches it.** 🧑: *"make it so that my
skills cant be seen too until i need to use them myself"*, and *"THIS IS FOR TUTORIAL BTW NOT THE
ACTUAL GAME"*. `Hud.SetTrainingDeckHidden`, instance state rather than a static so it cannot
survive into a match by construction. The deck appears on READ YOUR HERO KIT and the probe
asserts it is absent on every lesson before that. Same argument as `InputIntent.AllowOnly` one
layer up: the route already refuses the skill verbs, so a deck showing three powers you cannot
cast invites presses the tutorial will ignore.

**15.7 ✅ The pektus curve was not in the settings, because it was not in the input map.** 🧑:
*"im not sure as well if pektus controls are in settings, allow them to be rebindable"*. It was
`Keyboard.current.leftArrowKey` read inline in `PlayerInputReader`, which breaks `CLAUDE.md` § 4's
*"one control, one action, in the input map"*: unbindable, unlistable, and unprintable by
`Hud.KeyLabel`, which is why the pektus lesson had to name the arrow keys in a hard-coded string
while every other lesson drew the live binding. `CurveLeft` and `CurveRight` are real actions now,
in PLAYING THE GAME, and the lesson prints whatever they are bound to. ⚠️ The mouse wheel is
still read directly and that is not the same fault: a scroll axis is not a button and there is
nothing to rebind it to.

**15.8 ✅ Scrolling the settings.** 🧑, twice: *"make it easier to scroll thru settings bcz its so
hard to"* and *"here its so weird to scroll in setttings here"*, with a screenshot of a row cut
in half at the bottom edge. Three things, and only the first is the one people reach for:

* **No scrollbar existed at all.** `ScrollRect.verticalScrollbar` was never assigned, so there
  was no handle, no indication that there was more below, and no sense of how much. A cut-off row
  was the only cue, and a cut-off row reads as a layout bug rather than as an invitation. There
  is a wood track with an amber handle down the right edge now, drawn in code because Godot's
  `ScrollContainer` draws its bar from the theme and `TscnUiImporter` had no node to convert.
* **The wheel was set to 45**, about four rows a notch, commented "fast, smooth, responsive". It
  is the first two. 24 is two rows, and inertia is off so the row under the cursor is the row
  that ends up there.
* **No keyboard.** Page Up / Page Down / Home / End and the arrows move the list, refused while a
  rebind is listening.

⚠️ **The rows are PADDED away from the bar rather than the viewport being shrunk**, and the first
version did the other one: the content is authored at a fixed width out of the .tscn rather than
stretched, so moving the viewport's right edge moved the window and left the rows where they
were. The bar drew over the right end of every key cap and cut the username field in half.
`Logs/shots-runtime/SettingsPanel.png` showed it in one frame, which is why that shot is worth
taking after any change to this panel.

**Still open on this entry:** nothing, but every item wants a played build. The mash in
particular is a FEEL change and the guard at 5.0 s is a starting position: if an unanswered fall
reads as being stuck rather than as being punished, that constant is the one to move, and its
note says what it was solved against.

---

## 16 · The probe was never deterministic, and § 10 was closed on an argument

**Found on 2026-08-26 by the first thing that ever ran one configuration twice.**

⚠️⚠️ **THIS IS § 13'S LESSON A THIRD TIME AND IT COST TWO ENTRIES.** § 10 was marked done because
`Time.captureDeltaTime` plus a seed REMOVE THE CLOCK, which is true, and because that argument is
convincing, which is not the same as it being enough. § 5 then waited on a sweep the probe was
believed able to run. The sweep ran the shipped overclock rate as its first row and its last, one
build, one seed, one session:

```
  rate   saves  skills  ults  knocks  tags  throws  retr  restores  idlePen  frames
  3.50   6.75s      18     6      24     7      43    40        39      822   49809   (ship-a)
  3.50   6.75s      37    19      43    39      83    80        58      464   50612   (ship-b)
```

**Twice as much game in the second run**, with the two rows being tested landing in between, so a
sweep read at face value would have ranked the rates by WHEN THEY RAN.

**16.1 ✅ `Hitstop` measured its freeze against `Time.unscaledTime`.** It has to measure against
something unscaled, because the thing it freezes is scaled time, and its own note has always said
so. But `Time.captureDeltaTime` does not pin unscaled time: that keeps running at whatever speed
the machine renders. So a 60 ms freeze lasted a number of FRAMES that depended on the machine
while `Time.timeScale` was 0.05 for every one of them, and the wall clock was back inside the
simulation through the one door the fixed step left open. A cold first match is the slowest match
in a session, which is why the first row was the outlier rather than a random one.

`Hitstop` now advances its own clock by `Time.captureDeltaTime` when a capture is running and by
`Time.unscaledDeltaTime` when one is not. ⚠️ **Nothing in the game sets a capture**, so every
shipped path takes the branch it always took.

**16.2 ✅ The first match of a session was never warmed up.** 25 frames after the load was not
enough. With 120 more before the whistle, the first match went from 58 throws and 28 skill uses
to **100 throws, 41 skill uses and 144 idle penalties**, the healthiest run this probe has
recorded, and the first-run-is-worst pattern stopped: in the run after the change the FIRST match
was the busier of the two. ⚠️ The loop was written as a physics-phase alignment and does not
align anything (`Time.time - Time.fixedTime` never reaches the threshold, and the phase it
reports is 8 to 9 ms either way). It is kept for what it measurably does, under its real name.

**16.3 ⏳ AND THEY ARE STILL NOT IDENTICAL.** Eight matches at the shipped settings spread from
**58 to 100 throws** around a mean near 80. What changed is that the spread is no longer ORDERED:
before these two fixes, run order predicted the result, and now it does not.

⚠️⚠️ **SO THE HARNESS IS A NOISY SIMULATOR, NOT A DETERMINISTIC ONE, AND THAT CHANGES HOW EVERY
A/B IN THIS FILE HAS TO BE BOUGHT.** `TwoIdenticalMatchesLandInsideTheNoiseFloor` measures the
spread and gates a collapse rather than a difference. The arithmetic that follows from it:

| runs per arm | error on the mean | smallest effect it can resolve |
|---|---|---|
| 1 | ~20 % | ~40 % |
| 3 | ~11 % | ~23 % |
| 9 | ~7 % | ~13 % |

§ 5's overclock window is worth about 20 per cent of a cooldown cycle, so **it needs at least
three runs an arm**, which is nine matches and about half an hour. That is the real price of the
answer and it was never one run.

**Still open:** what the residual is. Candidates not yet ruled out: a `Random` draw whose count
depends on how many frames a visual effect lived for, PhysX's own solver state carried across
scene loads, and the frame-to-step interleave that § 17 shows the bots are extremely sensitive to.

**Where.** `Assets/TumbangPreso/Runtime/Hitstop.cs`,
`Assets/TumbangPreso/Tests/PlayMode/BotBehaviourProbe.cs`.

---

## 17 · The bots are steeply sensitive to the frame step, and a 50 fps machine is in the bad band

**Found on 2026-08-26 while trying to make `BotBehaviourProbe` deterministic (§ 16). It is not a
harness finding. It is about the shipped AI.**

Four configurations, same build, one whole match each:

| frame step | physics step | what the bots did |
|---|---|---|
| 1/30 s | 0.02 s | 9 throws, 0 tags, 673 idle penalties (Classic) |
| **1/60 s** | **0.02 s** | 40 to 90 throws, 27 to 38 skill uses, seats travelling 600 to 1100 m |
| 0.02 s | 0.02 s | **18 throws, 0 skill uses**, three of four seats travelling 190 m |
| 1/60 s | 1/60 s | 20 retrievals, under the probe's own liveness floor |

⚠️⚠️ **A 20 PER CENT CHANGE IN DECISION RATE COST FIVE SIXTHS OF THE THROWS AND ALL OF THE
CASTING.** 60 decisions a second is healthy and 50 is not. That is far too steep to be a smooth
sensitivity to `Time.deltaTime`, and `AIController.Update` scales everything it owns by `dt`, so
something further down is quantised: a window measured in frames, an edge that has to be seen
twice, or a threshold that a slightly larger step steps over.

⚠️⚠️ **THE SHIPPED GAME CAN BE IN THAT BAND.** The project's physics step is 0.02 s
(`ProjectSettings/TimeManager.asset`, 50 Hz) and a machine rendering at 50 fps has
`Time.deltaTime` = 0.02: a 50 Hz panel, vsync on a heavy scene, a laptop under load. That is the
row with **zero skill uses** in it. Every probe number in this repository was taken at 1/60, and
nobody has played the game at a capped frame rate to see whether the bots stop playing.

⚠️ **The fourth row is a separate lever and it is worth keeping.** Pinning the PHYSICS to 1/60
while leaving the frame at 1/60 also broke the bots, at a decision rate that is otherwise
healthy. So both the frame step and the physics step move the outcome on their own, and the
shipped pair is the only combination that has been measured working.

**Needs, in order:**
1. **Reproduce it in the player, not in the probe.** `Application.targetFrameRate = 50`, vsync
   off, one Hero Strike match against bots, and watch whether they cast and retrieve. If they do,
   the effect is a batch-mode artefact and this entry closes with that written down. **Do this
   first: everything below is only worth doing if a player can meet it.**

   ✅ **THE HARNESS FOR THIS EXISTS AS OF 2026-08-26.**
   `Assets/TumbangPreso/Runtime/Diagnostics/FrameCapProbe.cs` runs exactly that measurement from
   the SHIPPED EXECUTABLE, which is the part no PlayMode test can do, because a PlayMode test is
   the probe. It is off unless asked for on the command line:

   ```
   TumbangPreso.exe -tp-framecap 50 -tp-botmatch -tp-report frames50.txt
   ```

   It writes throws, skill uses, ultimate uses, lata knocks, tags and idle penalties to a named
   file beside the player, alongside the **measured** frame rate as well as the requested one.

   ⚠️⚠️ **THE MEASURED RATE IS THE NUMBER THAT MATTERS AND THAT IS WHY IT IS IN THE
   REPORT.** `targetFrameRate` is a request: a machine that cannot hold 50 reports a cap of 50
   and runs at 31, and the whole premise of this entry is that those two bands behave
   differently. A sweep read off the requested value could close the entry against a run that was
   never in the band being tested.

   ⚠️ **`vSyncCount = 0` IS SET WITH IT, NOT NEAR IT.** With vsync on, `targetFrameRate` is
   ignored outright and the run looks like a 50 fps machine while being paced by the display.
   That is the one failure mode that would make this probe LIE rather than fail.

   **The sweep to run:** 30, 50, 60 and 120, three runs each (§ 16's noise floor: three runs an
   arm for anything worth 20 per cent), and compare skill uses first. The batch-mode row that
   started this entry read **zero** at 0.02 s, so a player row with any casting at all in it
   already answers the question.
2. **Find the quantised thing.** The first suspect is the `InputIntent` edge protocol.
   `CharacterMotor.FixedUpdate` reads `JustPressed` and calls `CommitFrame` at the END of the
   physics step while the producers write in `Update`, so how many `Update`s fall between two
   commits decides how many decisions are seen at all. At 1/60 against 0.02 one frame in five
   carries no physics step; at 1:1 every frame carries exactly one. A verb that needs to be seen
   twice, or a release edge being swallowed, would look exactly like this.
3. **Then decide whether it is a bug or a bound.** If the AI genuinely needs `Update` to run
   faster than `FixedUpdate`, that is a shipping constraint and belongs in `CLAUDE.md` § 4 beside
   "a bot presses the same buttons a human does", not in a probe comment.

**Where.** `Assets/TumbangPreso/Runtime/AIController.cs`,
`Assets/TumbangPreso/Runtime/InputIntent.cs`, `Assets/TumbangPreso/Runtime/CharacterMotor.cs`,
and `BotBehaviourProbe.FixedStep`, which carries the table.

---

## 18 · HUD strings overflow their boxes, in more than one place

**Reported by 🧑 on 2026-08-26, from playing: *"fix all UI overflows as well for the HUDS bcz
theres a lot"*. One instance is already closed (§ 9.5) and it was closed one string at a time,
which is why this entry is about the CLASS rather than about the next one he happens to see.**

⚠️⚠️ **THE CAUSE IS ONE LINE AND IT IS DELIBERATE.** `Hud.HudLabel` sets
`horizontalOverflow = Overflow` and `verticalOverflow = Overflow` on every label it builds, so a
string that does not fit **neither wraps nor shrinks: it hangs out of its box.** That is the
right default for a HUD (a wrapped timer or a shrunk score is worse than a wide one), and it
means every card has to be sized against the LONGEST STRING IT CAN EVER SHOW rather than against
the one that was in it when somebody looked.

⚠️⚠️ **AND THE FONT SIZE IS NOT THE LEVER.** `ui_theme.gd` records these sizes going 16/13, then
22/19, then 30/28, answered every time with *"text still small"*. Shrinking text to fit a box
walks straight back into that. **Size the box, or shorten the string. Never the font.**

**The idiom to follow already exists in the same file, twice:**

* `Hud.WorstCaseNameWidth` measures `Balance.PlayerNameMax` (14) "W"s **in the real theme font,
  through the label that will draw them**, and keeps the .tscn's authored 132 as a floor. So the
  name column cannot drift when a font size changes somewhere else.
* `LataHintLines` lists every string `UpdateLataCard` can show and sizes the card to the widest
  of them through the label that draws it (§ 9.5). ⚠️ **Keep that list in step with the method**:
  a line added to one and not the other is an overflow again.

✅ **STEP 1 DONE 2026-08-26: `HudOverflowProbe` EXISTS AND RUNS IN THE DEFAULT PLAYMODE SET.**
`Assets/TumbangPreso/Tests/PlayMode/HudOverflowProbe.cs`, writing `Logs/hud-overflow.txt`. It
measures every `Text` the HUD builds, at all nine resolutions, substituting the worst-case string
each label can hold, and it asserts two separate things:

* the label fits its own box, for the labels whose box is FIXED (§ 18's own instruction:
  *"Assert on the ones that are inside a fixed-width card; report the rest"*). Everything else is
  printed, because `CrosshairLabel` is a 34 pt glyph centred in a 24 unit box ON PURPOSE and
  asserting on it would fail for the design rather than for a fault;
* **and the drawn string stays on the SCREEN**, which is the half § 9.5 actually was. A card
  anchored to the right corner does not merely overflow its plate: what runs past it leaves the
  display. ⚠️ The alignment decides which way a label spills and the calculation depends on it:
  left-aligned grows right off its rect, right-aligned grows left, centred grows both ways at
  half the rate. Assuming centred for everything reports a right-anchored card as fine, which is
  exactly the bug.

⚠️⚠️ **THE WORST-CASE LATA STRINGS COME FROM `Hud.LataHintLines` ITSELF, NOT FROM A COPY.** This
entry warns that a line added to `UpdateLataCard` and not to that array is an overflow again; a
probe carrying its own transcription would be a THIRD place to forget, and the one nothing fails
over. The array is now `public` for that reason and for no other.

⚠️ **WHAT THE PROBE DELIBERATELY DOES NOT DO, so nobody reads more into a green run than is
there.** This entry asks for the HUD to be driven through every state it has. Forcing every
hidden group active would run `Update` on components whose match state does not exist in a probe,
and an unexpected error log is a PlayMode failure: it would go red for reasons unrelated to a
string being too wide. It measures every label the HUD BUILDS instead, active or not, because a
preferred width is a property of the font, the size and the string and does not depend on whether
the object is switched on. **What that cannot see is a box whose width is computed by a layout
group only while it is active**, and the report marks those lines `[hidden]`.

### 18.1 What the first three runs found, including two faults in the probe itself

✅ **THE ONE REAL DEFECT: the round line overflowed its card at every resolution.**
`RoundLabel` needed **510 units for `ROUND 8 / 8   ·   DEFENDER: <14 characters>` in a 240-unit
box**, so it hung 270 units out, on all nine screens. The top-centre column is measured through
the label now and the wooden plate stays at its authored 240, because **the plate was never what
was clipping**: the round line is a SIBLING of the card in `TopCentre`, not a child of it, and
the first fix widened the card and changed nothing.

⚠️⚠️ **AND MEASURING IT ONCE AT BUILD TIME IS NOT ENOUGH.** The build-time version produced a
column of **276 units at 720p, 304 at 900p, 315 at 1080p and 306 at 1440p** against 500 to 525
needed. Two faults in one number: the font's glyph metrics are not final while the HUD is being
constructed, so the measurement is taken cold; and `preferredWidth` comes from integer pixel
metrics divided by the canvas scale, so it moves about **14 per cent** across the shipped
resolutions for one unchanged string. `Hud.FitTopCentre` re-measures on a text change or a scale
change, and is guarded because its caller runs every frame (`CLAUDE.md` § 7.1 records a HUD
string rebuilt per frame costing the 6x probe an eighth of its frames).

⚠️⚠️ **TWO OF THE THREE FINDINGS WERE THE PROBE BEING WRONG, AND BOTH ARE WORTH KEEPING WRITTEN
DOWN BECAUSE THEY ARE THE SAME MISTAKE IN TWO PLACES.**

**18.1a A rect is not a box when something else sizes it.** The first run reported **205
overflows** and almost all of them were labels that had never been laid out: a `RectTransform`
that has not run reports its authored `sizeDelta`, which for anything built by `MenuKit.Stretch`
or driven by a parent is the uGUI default of **100 x 100**. So `LataHintLabel` came back as a
527-unit string in a "100-unit box" while the card it lives in is sized by `Hud.WidestLineWidth`
and fits it exactly. Then the same fault in its other form: a label whose parent
`HorizontalOrVerticalLayoutGroup` has `childControlWidth` is sized to ITS OWN preferred width, so
the rect tracks whatever string is in it right now; swapping in a longer one and comparing
against the old rect reported `RoundLabel` as "needs 510 in a 305-unit box" when the column around
it was already over 510 wide. **Both are now reported and not asserted**, and the fitter case was
exempt from the start for exactly this reason.

**18.1b The HUD is not one canvas.** `CanArrow` was reported as running **3,323,799 units off the
LEFT of the screen**. `OffscreenIndicators` builds its OWN canvas for the arrows that point at the
lata, and converting one of its corners into the HUD canvas's local space is two coordinate
systems, not an overflow. Each label is measured against `label.canvas` now. ⚠️ **A number that
absurd is the tell**: a real overflow here is tens of units, so anything in the millions means the
conversion is wrong rather than the layout.

⚠️ **The `RECAST` exception has to be honoured or the probe reports it as the bug.**
`Hud.PaintSkillCard` sets `RecastFontSize` (14) instead of the deck's 22 for that one string,
because six bold capitals do not fit a 60 px tile. Measuring it at the label's own size invents a
12-unit overflow that does not exist.

**Verified:** PlayMode 66/66 with the probe green, and `Logs/hud-overflow.txt` carries the full
table for every label at every resolution.

**Needs, in order:**

1. ✅ **A probe that FINDS them, before anything is fixed.** `HudOverflowProbe`: drive the HUD
   through every state it has (attacker, taya, lata up and down, tripped, stunned, the ready
   gate, the countdown, a toast, the hero deck, Street Hype, spectator), at the same nine
   resolutions `AspectRatioProbes` uses, and for every `Text` under the HUD canvas print the
   label, its string, its preferred width, its box width and the overflow in pixels. Assert on
   the ones that are inside a fixed-width card; report the rest. ⚠️ **Measure in the CANVAS's own
   space, not in world corners:** `SettingsScrollProbe` did the latter first and printed zero for
   every column while passing nine resolutions, because on a canvas rendering to a camera every
   element sits within a hair of the same world x.
2. **Feed it the worst case, not the typical one.** Four names at `PlayerNameMax` = 14
   characters, the longest ability names in the game (**PERMAFROST SHEET**, **DEMONIC CARAPACE**,
   **ASTRAL PROJECTION** at 17 characters), `ROUND 8 / 8`, `TAYA (DEFENDER) P4`, and every
   `LataHintLines` string. A probe fed "P1" proves nothing.
3. **Then fix by sizing, one card at a time**, each one measured through its own label.

**Already known and already closed, as the worked example:** the objective card read
`FETCH SLIPPER · -5 / SEC` because the string is `-5 / SECOND` and the card is anchored to the
RIGHT screen corner, so what ran past its 380 px left the screen entirely (§ 9.5).

**Also known:** `RecastFontSize` is 14 against the deck's 22 because **six bold capitals do not
fit a 60 px tile**, which is the one place in the HUD where shrinking WAS the answer, and it is
documented as an exception rather than a pattern.

**Where.** `Assets/TumbangPreso/Runtime/UI/Hud.cs` (`HudLabel`, `WorstCaseNameWidth`,
`LataHintLines`, `PaintSkillCard`), `Assets/TumbangPreso/Runtime/UI/AbilityDeckHud.cs`,
`Assets/TumbangPreso/Tests/PlayMode/HudLayoutProbe.cs` for the pattern of measuring live rects,
and `Assets/TumbangPreso/Tests/PlayMode/AspectRatioProbes.cs` for the resolution list.

---

## 19 · The powers were fifteen poses sharing one construction, at every layer

**Raised by 🧑 on 2026-08-26, from playing the build, and it is the sharpest diagnosis anybody
has given this problem:** *"the problem i found out earlier that made all powers look ugly was
that the same logic and code was used to generate all of them"*, then *"maybe use different
techniques to make them if we can"*, *"make the particles better too"*, and *"thoroughly make
all animations better and more fun"*.

⚠️⚠️ **HE IS DESCRIBING SOMETHING § 8 AND `Hero_Strike_Balance.md` § 8 BOTH MISSED, AND THOSE
TWO PASSES ARE THE EVIDENCE FOR IT.** § 8.2 lists four channels an effect has (silhouette, axis,
motion, hue) and both passes spent all four. What neither one asked was **how the geometry is
BUILT**, and the answer was: identically, everywhere, at three separate layers.

* **Meshes.** `VfxShapes.Splat`, `Star`, `Streak` and `Crystal` are four different POLYGONS
  handed to ONE builder. `Fan` triangulates a rim of points around a centre vertex, `Lay` drops
  the result flat at y = 0.01 with the Y scale left at 1, and `VfxMaterial.Ghost` paints it
  translucent. Measured off the `v11` set: fire, ice, lightning and magma each render as a
  coloured plate on the road with a brighter plate under it and four or five cubes standing on
  top, because that is literally what each one is.
* **Particles.** All five auras set a lifetime, a speed, a size, a gravity, a rate, an emitter
  shape and a gradient, and nothing else. `GetParticleMaterial` never assigns a texture and the
  renderer defaults to a billboard, so **every mote, ember, spark and wisp in Hero Strike was an
  untextured quad: a literal rectangle.** That is the same fault `SpawnFireTrail` already records
  against its own embers and fixed there in 2026-08-25, in a file nobody re-checked.
* **Animation.** Fifteen clips are keyed bespoke and interpolated identically.
  `AnimationCurve.AddKey(time, value)` gives a key SMOOTH tangents, so every clip arrives at
  every pose decelerating and leaves it accelerating. That is right for a walk cycle and wrong
  for all fifteen of these, because every one of them is a strike and a strike is defined by the
  moment it stops.

✅ **DONE 2026-08-26. Five construction techniques, per-aura particle stacks, and an impact frame
per clip.**

### 19.1 The fifth channel: construction

`Visual.VfxShapes` gains five builders that are not outlines, and each one goes to the fiction it
belongs to. None of them buys a single extra square metre; every shape is still built at one unit
of radius so `Lay` and every footprint in `Hero_Strike_Balance.md` § 1 keep working untouched.

| Builder | What it makes | Who has it |
|---|---|---|
| `Prism` | an extruded slab with real walls | Cheska's sheet, its rim, its shards |
| `Wedges` | ground broken into separate plates with gaps | Dante's crust |
| `Tongue` | a leaning, curling flame of triangular section | Sean's trail |
| `Bolt` | a branching tube walked from a to b | Zack's arc |
| `Funnel` | a dished surface that goes DOWN | Nemu's void |
| `Collar` | a real annulus with walls | every rim in the game |

⚠️ **`FacetedOriented` is why five new builders did not cost a capture pass.** `Fan`'s note
records one lost to a fan wound the wrong way, where every mesh was culled from the only angle
the game looks at it from and "invisible" read as "not spawned". Each triangle is now turned
against a per-shape reference point instead of hand-wound, so a builder can emit its triangles in
whatever order is convenient and cannot ship inside out.

⚠️ **`Stand` exists because `Lay` scales X and Z and leaves Y at 1.0**, which is right for a flat
fan and silently wrong for anything with height. § 15.5 records the 2 m ball that shipped from
exactly that mistake. Anything with real height uses `Stand`.

### 19.2 Four defects the renders found on the way, and two are classes rather than instances

**19.2a ⚠️⚠️ TWO COPLANAR TRANSLUCENT PLATES SORT ARBITRARILY, SO ONE CALL DREW A DIFFERENT
COLOUR PER DROP.** Unity orders transparent renderers by the distance from the camera to each
one's bounds centre. The fire trail's dark char and the bright plate under it are concentric and
5 mm apart, so their centres are the same point to within rounding and the comparison is a coin
toss. **`ability_worstframe_v11.png` had this on disk unexplained: six drops of ONE trail,
alternating dark brown and bright salmon, from one call with one set of constants.** The same
fault then reappeared the moment a hot bed was put under Dante's crust. Nothing was wrong with
any of the colours.

**The rule that replaces it: ground that has been BURNT or BROKEN is opaque, and only things you
can genuinely see through are ghosted.** An opaque material renders in the geometry queue and
writes depth, so it occludes what is beneath it by construction rather than by winning a sort.
The fire char's own note had already reasoned its way to *"a scorch is opaque anyway"* and then
set an alpha of 0.92.

**19.2b ⚠️⚠️ `tools/generate_ability_audio.py` SEEDED FROM A POSITION IN A SORTED LIST, so
adding a cue silently rewrote every existing one.** The comment beside it claimed *"regenerating
one cue cannot change another"*, which was true only while nothing was ever added. Adding six
cues inserted names ahead of five of the original seven alphabetically, every following index
shifted, and one run rewrote all seven shipped sounds with different audio. **`git status` caught
it, not listening.** The seed slot is now written down per cue and the original seven carry their
old numbers, so they regenerate byte-identical.

**19.2c `Prism`'s twist sheared the shape instead of turning it.** Applying the offset to the cap
only rotates the top face against its own base, which at six sides is a 15 degree disagreement
and is plainly visible in `ability_ice_sheet_v13.png` as two hexagons at different angles. The
parameter exists to match `Crystal`, where it is a rotation.

**19.2d `Wedges` at even spacing is a pinwheel, and its inner jitter is a multiplier.** Nine
plates of identical width at identical spacing read as a black FLOWER
(`ability_lava_decal_v13.png`), which is a manufactured object and the one thing broken ground
must never look like. Separately, the inner-radius jitter is a multiplier on `innerRatio`, so a
0.7 to 1.3 swing that is centimetres on Dante's 0.22 band throws plates three times deeper than
the band on a 0.9 rim: `ability_seance_void_eye_v14.png` is a ring of huge purple spikes around a
telegraph that is supposed to be a line.

⚠️ **AND A RIM WANTS `Collar`, NOT `Wedges`.** A boundary has to be continuous to read as a
boundary; broken ground wants the opposite because it genuinely is in pieces. Same area budget,
different builder, and picking the wrong one is `ability_seance_void_eye_v15.png`.

### 19.3 Footprint, which this pass also had to pay for

`VISION.md` § 2 measures Sean's and Zack's corridors as the two worst offenders in the game at
**27.2 per cent of the box off a 6 s cooldown**. Both were carrying a full-radius bright plate on
top of a mark, and the plate was the largest disc in each drop.

* **Sean's bright lozenge is deleted**, replaced by three `Tongue` flames and a thin `Collar`
  lip. It was `radius * 1.66` by `radius * 0.86` against the char's `1.28` by `0.62`, so this is
  roughly 40 per cent of the painted area out of every drop he makes.
* **Zack's full-radius bright `Star` fill is replaced by twelve short rim plates.** The dark
  scorch keeps the star silhouette, so which ability it is does not change; what goes is the
  fill.
* **Nemu's ground telegraph was a filled `Cylinder` at the FULL radius at 0.42 alpha**: a 2.8 m
  void laid a **24.6 m² violet plate on a 196 m² court, 12.5 per cent of the box for a marker
  whose entire job is to say where the edge is.** It is now a 7 per cent band, about 1.1 m².
* **Trail marks shrink toward their own end** (`HeroHazards.Burn`), which is both the age read
  and the thing that stops six drops looking like one drop six times. ⚠️ It shrinks the MARK and
  never `Radius`, so what a player is standing in is exactly what § 1 measures.

### 19.4 Particles: four meshes and a streak, and five different module stacks

| Aura | Made of | The module that carries it |
|---|---|---|
| VoidWisp | a flat four-sided chip | `velocityOverLifetime` ORBITAL: a vortex's motes go around |
| MagmaEmber | a square-section grain | shrink and slow tumble: rock cooling, and rock does not flicker |
| ElectricSpark | a STRETCHED billboard | scaled along its own velocity, and deliberately no noise |
| FireEmber | a square-section grain | strong `noise`: hot air does not travel where it was thrown |
| FrostMote | a thin six-sided plate | fast spin plus `limitVelocityOverLifetime`: it settles |

⚠️ **A mesh particle that does not tumble is a static block**, and rotation needs three axes
turned on explicitly: the default rotates around the view axis only, which on a mesh reads as a
spinning sign.

### 19.5 Animation: an impact frame, opt in, one line per clip

`ClipBuilder.PunchAt(t)` names the instant an ability LANDS, and three things follow: the pose
BEFORE it leaves slowly (anticipation), the impact pose is arrived at accelerating, and the body
**stops dead on it** rather than easing through. It is one line per clip because fifteen clips
times seven bones times three axes is 315 curves and nothing applied per curve would ever be
applied consistently.

⚠️ **The tangents are WRITTEN, not smoothed.** `Keyframe(time, value, in, out)` is the
constructor that leaves tangents alone; `AddKey` followed by editing `keys` does not, because a
key added that way carries an AUTO tangent mode that recomputes and throws the edit away.

⚠️ **The baseline is exactly the Catmull-Rom slope `AddKey` would have produced**, so a clip with
no `PunchAt` animates as it did before. That matters because **three clips deliberately get
nothing**: Zack's Bolt Sprint is a locomotion cycle with no instant at which anything lands,
Zack's Static Charge is a vibration that is already all attack, and **Nemu's Ghost Step is a
character decision** rather than a technical one. She is untaggable while it runs and the whole
read is that the body stops being a body; every other hero gets a frame where the world stops and
hers does not.

**⚠️ What is still a human call:** whether the fifteen now feel like fifteen different events with
a controller in hand. A still frame cannot answer it and neither can a probe.

**Where.** `Assets/TumbangPreso/Runtime/Visual/VfxShapes.cs`,
`Assets/TumbangPreso/Runtime/Visual/AbilityVfx.cs`,
`Assets/TumbangPreso/Runtime/Visual/ArcFlicker.cs`,
`Assets/TumbangPreso/Runtime/Visual/HeroAbilityClips.cs`,
`Assets/TumbangPreso/Runtime/Abilities/HeroHazards.cs`.

---

## 20 · Cheska's kit played the wrong sounds, and every zone died in silence

**Raised by 🧑 on 2026-08-26: *"maybe make the sfx better too and add sounds effects in places u
think there should be"*.** Two separate faults, and the first is the exact one
`tools/generate_ability_audio.py` was written to fix, surviving in the one kit that pass did not
reach.

✅ **DONE 2026-08-26.**

**20.1 Three borrowed cues, one of them backwards.**

* `SpawnIceBarricade` and `SpawnIceSheet` **both** opened on `ability_shatter_trap`, so two
  different powers shared one cue **and that cue is the sound of something BREAKING, played at
  the moment something is BUILT.**
* `IceBarricadeComponent.Shatter` played **`slipper_land`: a rubber sandal hitting the road**,
  for a wall of ice failing and coming down in twelve pieces. It is in the one place in her kit
  where ice genuinely does break.

Now `sfx_barricade_raise` (a grind that ARRIVES and locks, because three pillars are a solid
object that is now in the way), `sfx_ice_form` (a rising shimmer, because a sheet spreads), and
`sfx_ice_shatter` (one hard crack, then debris).

**20.2 Every hazard ticked down and called `Destroy` without making a sound.**
`Hero_Strike_Balance.md` § 8.5 item 2 argues that a player who cannot tell a spent effect from a
live one has lost a real gameplay read and that fixing it is free. **That argument was applied to
the visuals and never to the audio, which is the channel a player still has while they are
looking somewhere else.** Added: `sfx_ice_thaw`, `sfx_void_close` (it CUTS rather than fades: a
hole in the world is either there or it is not) and `sfx_magma_cool`.

⚠️⚠️ **THE TRAILS DELIBERATELY GET NOTHING.** One dash drops up to thirty marks and each lives
3 s, so trail expiry cues would be thirty overlapping tails inside three seconds. Same
measurement `AbilityVfx` uses to keep emitters off trails; it applies harder to sound.

⚠️ **The cracked lava decal had no component at all**, only `Object.Destroy(go, duration)`, which
is a deletion and not an event. `HeroHazards.ExpiryCue` is the smallest thing that turns one into
the other.

**Where.** `tools/generate_ability_audio.py`,
`Assets/TumbangPreso/Runtime/Abilities/HeroHazards.cs`.

---

## 21 · Phaister merged in, and everything she arrived without

**`feat/hero-witch-v2` merged into `feat/ilalim-ng-tulay-map` on 2026-08-26 at 🧑's request, in
TWO passes.** The first took `3c582df`; `3c4e756` was pushed while that was being verified and
was merged on top. ⚠️ **Check the remote head before believing a merge is current**: the first
pass looked clean and was already stale by the time the tests finished.

The second merge conflicted in `PhaisterHeroKit.cs` and `AbilityVfx.cs`, because both sides had
independently written her a kit and an aura. **Theirs was taken for the gameplay wiring and the
particle constants; the geometry and the construction are this side's.** The reasoning is below
and it is measured rather than asserted.

⚠️ **She is the SIXTH hero and several places in the docs still say five.** `VISION.md` § 1 and
`Hero_Strike_Balance.md` § 1 both enumerate five kits. Nothing is broken by that, but the next
person to count heroes from the prose will be wrong.

### 21.1 Three regressions the merge introduced, all caught by EditMode

Every one of these was green on neither side alone and red the moment the branches met.

1. ⚠️⚠️ **`TheFiveHeroAccentsAreTellableApart`: *"sean and phaister are only 18.1 degrees apart,
   which is one colour on a deck tile"*.** Her accent shipped at `e82882`, hue 332, against
   Sean's 350. The law is 30 degrees between any two hero accents and 25 clear of both ROLE
   colours, because orange tracks the attacker and blue the defender and those rotate every
   round. **With fire at 350, ice at 170, electric at 64, spirit at 275, earth at 137 and the
   roles at 22 and 207, exactly three hue windows satisfy both constraints: 95 to 106, 232 to
   244, and 305 to 320.** The first two are not colours a witch can have. She is now `e828c5`,
   hue 311, which sits furthest from its nearest neighbour inside that window at 36.2 degrees
   from Nemu and 39.1 from Sean. Saturation and value are the ones she shipped with.
2. ⚠️⚠️ **`EveryHeroAbilityHasBespokeCastAndViewModelActions`: *"phaister: HEX
   ViewmodelAction 'cast-hex' is not supported by ViewmodelArms"*.** All three of her powers
   named a viewmodel action and `PlayAction`'s chain had an arm for none of them, so the
   first-person arms did NOTHING for the entire sixth kit. The hand is the whole character in
   first person: the sigil she draws is on the floor and out of frame at the moment she casts it.
   Three clips added, and the blink is the shortest in the file on purpose.
3. **`GameMode_Rosters_AreDistinctAndCorrectSizes` expected five heroes.** Updated to six, with
   the number left asserted rather than derived: a hero appearing or disappearing is a product
   decision and should have to be typed.

### 21.2 Her three powers were drawn as filled discs, and one of them was the largest object ever put in this game

⚠️⚠️ **THE ARITHMETIC, BECAUSE IT IS NOT CLOSE.** A Unity `Cylinder` is one unit ACROSS and it is
SOLID, so `localScale = radius * 2.0` is a filled disc of that radius, not a ring.

| Effect | As merged | Painted | Now |
|---|---|---|---|
| Hex | two stacked discs at `r*2.0` and `r*1.25`, plus 3 spokes and 6 cubes | about **18 m²** of a 196 m² court for one SKILL | pentagram line art, about 1.4 m² |
| Shadow Blink | two discs, 1.6 m and 1.2 m radius | 8 m² and 4.5 m², for marks living under half a second | two cast glyphs |
| Grand Coven Eclipse | corona at `r*2.0` plus a dark core at `r*1.1` plus 8 cube beams 9 m long | **78.5 m², 40 per cent of the box, in one plate**, with 23.8 m² stacked on it | heptagram at the same 5 m reach, plus a 1.7 m moon |

For scale: `VISION.md` § 2 rule 1 puts a skill's floor at 3 to 8 per cent of the box, and the
worst offender ever measured in this game was Zack's corridor at 27.2 per cent.

⚠️ **AN ULTIMATE MAY BE BIG. RULE 2 SAYS SO, AND THAT IS NOT WHAT THIS WAS.** Big and FILLED are
different claims. The heptagram keeps the full 5 m reach, so the power still reads as arena-wide;
it paints about **8 per cent of the circle it covers** because it is strokes. Same footprint, a
twelfth of the pixels. **Nothing about her range or her damage changed.**

⚠️ **The two stacked discs also could not have rendered stably.** Two coplanar translucent plates
sort arbitrarily; § 19.2a records that shipping on Sean's trail and drawing a different colour per
drop.

### 21.3 The sigils, and why her kit breaks the silhouette rule on purpose

🧑: *"the abilities i want for phaister are witch based and she does hexes curses and spells and
has glyphs effects during spells or abilities casting"*, and *"yk witch symbols"*.

`VfxShapes.Sigil` is **line art, and nothing else in that file is.** Every other builder makes a
solid: a fan, a slab, a shell, a funnel. A sigil is strokes with the road showing between them,
which is a different way of making geometry rather than a variation on a filled shape. An outer
ring, an inner ring, a `{points/skip}` star and rune ticks.

⚠️⚠️ **AND IT COSTS ALMOST NO FLOOR, WHICH IS WHY IT IS THE RIGHT ANSWER RATHER THAN A LUCKY
ONE.** A hero whose entire identity is drawing symbols on the ground is exactly the hero who could
break the readability budget. Strokes are how she is the most ornate hero in the game and the
cheapest on screen at once.

⚠️⚠️ **ALL THREE OF HER POWERS DRAW THE SAME KIND OF MARK, AND THAT INVERTS § 8.3 DELIBERATELY.**
For the other five heroes the silhouette says WHICH ability it is, because their kits are five
unrelated physical events. Phaister's kit is one CRAFT. So the sigil is her signature and the
three are told apart the way occult diagrams escalate: the skills draw a **pentagram**, the
ultimate a **heptagram** at double the radius, and a small short-lived **cast glyph** at her feet
says a spell is being cast without saying which. A player learns one visual language, not three.

⚠️ **The blink marks BOTH ends.** A blink that only marks where she arrived tells the three people
chasing her nothing they had not already worked out by looking at her; the mark left behind is the
one that carries information, and it is where the knockback `OverlapSphere` is centred.

⚠️ **The "crescent moon" was a `Cylinder` tilted 30 degrees**, which is an ellipse seen at an
angle and not a crescent by any construction. Deleted; the sigil's inner wheel occupies that space
and carries an actual symbol.

### 21.4 Audio: three borrowed cues and one that never existed

* ⚠️⚠️ **Her ULTIMATE called `sfx_ghost_appear`, which has no file and no registration**, so
  `AudioDirector` logged `no cue registered` and returned. A warning, not an exception, which is
  exactly how `LrtTrainFlyby` called `ui_move` for two months. Now `sfx_eclipse_toll`, the only
  bell in the game: every other payload here is an impact, a whump or a hiss, and an eclipse is
  announced rather than delivered.
* **Her hex cast played `ability_shatter_trap`** (a trap breaking, from the deleted ability set)
  **plus `sfx_ghost_teleport`** (Nemu's). § 20 had just finished taking that first cue off
  Cheska's two ground powers; a third kit reaching for it would have made it three. Now
  `sfx_hex_cast`: an incantation with no clear pitch, then a chime when the sigil catches.
* **The victim of a hex got the same cue as the cast.** Now `sfx_hex_afflict`, which FALLS rather
  than rises and carries a sour detuned pair: every other on-hit sound in this game is an impact,
  and a curse is not struck, it settles. Mixed ten down because it fires per victim per 1.1 s.

### 21.5 Her aura was Nemu's

She shipped attaching `Aura.VoidWisp`. **Nemu and Phaister are the only pair in the game who share
an ELEMENT**, so hue and motion are doing more work between those two than anywhere else, and
borrowing put both spirit heroes in the same purple with the same motes falling the same way. Her
own aura keeps the branch's magenta-into-gold gradient, which is the one hero palette with two
hues in it, and takes this side's construction: mesh chips that **orbit the opposite way from
Nemu's**. Hers is a vortex pulling in; a spell is wound out.

**Still open, and deliberately not done here:**

* ⚠️ **She borrows `hero_nemu_grunt` for two casts.** Same class as the above and it cannot be
  fixed the same way: a hero voice is a recorded asset, and `tools/generate_hero_audio.py` is
  UNSEEDED, so touching it rewrites all seventeen existing voice files. Needs its own pass.
* **Her footprints have no row in `Hero_Strike_Balance.md` § 1**, and her cooldowns have not been
  measured against anything.
* **Nothing about her has been seen in motion**, which is the same open judgement § 19 ends on.

⚠️ **`HANDOFF.md` CAME IN WITH THE MERGE AND WAS DELETED.** `CLAUDE.md` § 2.4: a handoff goes in
the chat reply, never as a file, and a stale one committed here has now had to be removed three
times.

**Verified:** `ability_hex_sigil_v17.png` and `ability_coven_eclipse_v17.png`, both at 2.1 per
cent blown against the 12 per cent gate, and the worst frame unchanged at 4.1.

**Where.** `Assets/TumbangPreso/Runtime/Visual/VfxShapes.cs` (`Sigil`),
`Assets/TumbangPreso/Runtime/Abilities/HeroHazards.cs` (`SpawnWitchSigil`, `SpawnCastGlyph`,
`WitchSigilSpin`, and the three merged spawners),
`Assets/TumbangPreso/Runtime/Camera/ViewmodelArms.cs`,
`Assets/TumbangPreso/Runtime/Visual/HeroAbilityClips.cs`,
`Assets/TumbangPreso/Runtime/UI/UiTheme.cs`, `tools/generate_ability_audio.py`.

---

## 22 · Everything the 4.71 player showed, and the two entries that were ticked but not wired

**Reported by 🧑 on 2026-08-26 across a single play session, with screenshots. Nine separate
faults, and the two most interesting are not in his list: they are entries in THIS file that
say "done" against work that was only half landed.**

⚠️⚠️ **THE PATTERN, WHICH IS WORTH MORE THAN ANY ONE FIX. Twice this session an entry recorded a
defect as closed, the supporting asset existed, and the CALL SITE was never changed.** § 9.5 and
§ 18 both describe the objective card being sized through `Hud.WidestLineWidth`; that function
was written, `LataHintLines` was written, both were documented, and `grep WidestLineWidth`
returned **the definition and the prose and no call site**. The card shipped at a flat 380 units
against a 527-unit string. § 21.4 records Phaister's ultimate moving from `sfx_ghost_appear` to
`sfx_eclipse_toll`; the cue was generated, mixed and registered, and `PhaisterHeroKit` still
called `sfx_ghost_appear`, which has no file, so her ultimate was silent.

**Both were verified by the half of the work that shows up in a file listing.** When closing an
entry, grep for the call site, not for the asset.

### 22.1 The stray IKE tsinelas, which was in every match of every round

🧑, with it circled: *"thres this random Ike slipper that u cant pick up in the map, is that
intentional? idc if it is pls remove it"*, and *"it's on ALL games"*.

⚠️⚠️ **IT WAS NOT RANDOM AND IT WAS THE SAME MODEL EVERY TIME, WHICH IS THE TELL.**
`SliceRunner.EquipOwnedSlippers` assigned ownership as
`index < attackers.Count ? attackers[index] : -1`, and `attackers` is the three non-defender
seats. Four slippers over three attackers means **index 3 always fell off the end**, every round,
whoever was taya. `MatchInstaller.BuildSlipper` gives a non-local seat `pick = slot`, and roster
entry 3 is **IKE** (`Roster.Slippers`). One specific model, on the ground, in every single match.

⚠️ **A SECOND BUG WORE THE SAME CAUSE.** Ownership counted through the attacker list while
`SlipperHome` and `BuildSlipper` both index by SEAT, so with seat 0 defending, seat 1 was handed
slipper 0, the tsinelas built with seat 0's art. **A player who picked their slipper in the
settings panel carried somebody else's for the whole match.** Owning by seat makes all three
agree.

⚠️ **THE TAYA'S TSINELAS NOW LEAVES THE ARENA, AND AN ABSENT SEAT'S DOES NOT.** `SoloPracticeTests`
caught the difference immediately: with the practice lobby set to NONE the three bot seats are
never built, so parking every ownerless tsinelas left the one human holding the only one in the
arena and nothing to practise retrieving. A seat that EXISTS and is defending may not throw; a
seat that does not exist has simply left its slippers in the street.

### 22.2 Two strings drawn through each other, and the class behind it ✅

🧑: *"theres many cases of text going on top of each other"*, with a frame at 00:14 showing
`FINAL PUSH · ATTACK NOW` and `LATA IS BACK UP` in the same place.

⚠️⚠️ **THE ARITHMETIC.** Everything in the top band uses a TOP pivot, so an offset is the row's
top edge. `TopCentre` flows down from y = 28: clock card **28..124**, gap 4, `RoundLabel`
**128..162**, gap 4, `TimerPressure` **166..198**. The toast was `Place`d at a literal **-160**
with height 44, so it occupied **160..204 and swallowed `TimerPressure` whole.**

⚠️⚠️ **NEITHER NUMBER WAS EVER WRONG. IT WAS TWO COORDINATE SCHEMES.** -160 is the .tscn's own
offset and it was correct when the toast was the only thing under the clock. `TimerPressure` was
added to the COLUMN later, and a layout group's height depends on which children are enabled, so
**no literal can be safe: any fixed offset under a layout group is a guess about a number the
layout owns.** The toast and the lata alert are rows of `TopCentre` now, which makes the overlap
impossible rather than merely fixed.

⚠️ **THE COLUMN IS MEASURED THROUGH THE ALERT'S OWN LABEL AS WELL AS THE ROUND LINE'S.**
`childControlWidth` hands every child the column's width, and the alert is 42 pt against the
round line's 20; running its strings through `_round` would be the same error wearing a
measurement.

### 22.3 The objective card overflowed the screen, and § 9.5 said it did not ✅

The bottom-right card read `FETCH SLIPPER · -5 / SEC`, clipped at the display edge. **This is the
exact string § 18 uses as its worked example of a fix that had already landed.**

`BuildLataCard` passed a hard-coded `380.0f` and never called `WidestLineWidth`. The longest line
it can hold needs about **527**, and because the card is pinned to the RIGHT screen corner the
147 units of overflow left the display rather than merely the wood. `FitLataCard` now measures
both rows through their own labels, keeps 380 as a floor, and re-fits on a canvas scale change
for the reason `FitTopCentre` records: `preferredWidth` moves about **14 per cent** across the
nine shipped resolutions for one unchanged string.

### 22.4 No UI may stack, as a game feature rather than a probe ✅

🧑: *"i want ut o make sure too that no Ui's stack on each otehr and if they do force one to go
below it or smth"*, and, when a probe was offered: *"i dont want a probe for it i want it in th
egame as a feature"*.

`UI.HudDeclutter` runs in the player, in `LateUpdate`, and pushes a lower-priority element clear
of a higher-priority one. Registration order is priority order.

⚠️⚠️ **IT IS THE BACKSTOP, NOT THE FIRST ANSWER,** and he set that bound himself: *"make sure it
dont break shit too / and touch shit that dont have the capability to stack on each other
already"*. `Track` **refuses any element whose parent is a `LayoutGroup`** and logs, because a
layout group already guarantees its children cannot stack and pushing one would fight the parent
every rebuild. The corner cards are excluded too: they cannot reach each other at any shipped
resolution. What is registered is the bottom-centre column and the two mash cards.

⚠️ **AND IT FOUND A SECOND REAL COLLISION AT SOURCE.** `VulnerableWarning` sat at 84..124 and
`InspectHint` at 78..96, both bottom-anchored, both live in-round: **12 units of shared band**
between the line meaning "you are about to lose five seconds" and a tutorial aside. The warning
moved to 112, structurally, rather than being left to the declutterer.

⚠️ **NO `Canvas.ForceUpdateCanvases()`.** It was in the first draft and it is a full canvas
rebuild every frame, which is the class of per-frame HUD cost `CLAUDE.md` § 7.1 records. It is
not needed: this writes only `anchoredPosition`, never a size.

### 22.5 The lata-down beacon, deleted rather than retuned ✅

🧑: *"that red line, thats red beacon when lata is down, that looks bad ... the purpose of it is
to put emphasis on lata being down but its shit"*, and the shape of the fix in the same message:
*"without putting a fkn beacon on it or covering the lata completley with some effect"*.

⚠️ **WHAT IT WAS MADE OF IS WHY IT LOOKED LIKE THAT.** A 4 m translucent `Cylinder`, a second
translucent `Cylinder` flat under it, and a point light over the pair: the exact stack
`docs/VISION.md` § 2 rule 3 names, and that § 19 spent a pass removing from the ability kits.
Nothing had come back to the objective itself. **A 0.18 m tube seen from eye height across a
14 m arena is foreshortened into a red line lying on the road**, which is what he photographed.

⚠️⚠️ **AND THE GAME ALREADY SAID "LATA DOWN" SIX OTHER TIMES**: the world popup, the centre
alert, the card title, the objective line, the score toast and the crosshair. The problem was
never that the message was too quiet. **Emphasis is not repetition.**

Replaced with two things built two different ways, per § 19: a **rim pulse on the can's own
renderers** (no floor area at all, and it cannot cover the object because it IS the object's
silhouette) and a **`VfxShapes.Collar`** at the foot, an annulus with an open middle. 0.95 m of
radius is **1.4 per cent of the box**, against a skill's budgeted 3 to 8.

### 22.6 The tag stopped being ice, and ice became Cheska's ✅

🧑: *"freeze effects show up when u get tagged, this was an old stale version bcz back then we js
put freeze effect on screen and on 3d model of chara when they get tagged. pls plan what to
replace that with bcz it doesnt make sense anymore"*.

⚠️ **IT WAS NEVER BADLY MADE, IT WAS OVERTAKEN.** The frost was asked for on 2026-08-06 and on
that date it was unambiguous: nothing else in the game was cold. Hero Strike then shipped Cheska,
whose entire kit is ice, and a frozen body had two possible causes. **The frost's own note argues
that firing one signal for two causes makes it mean "something happened to that player", which is
"not worth a channel"** — written about trips, and Cheska walked into it from the other side.

A tag now drains colour toward Rec. 601 luminance with a **taya-blue rim**, which the frost never
carried: the mark says WHO made it. `_FrostAmount` is kept and is now the ABILITY coat.

### 22.7 The train never moved, because its sound never did ✅

🧑: *"make it feel like its getting farther and add sound or movement to screen to make it
realistic? bcz usually when it passes by u feel the shaking"*.

⚠️ **NOT A CLIP PROBLEM.** Both cues went through `PlayAtVaried`, which parks a pooled voice **at
a fixed position**. The consist travels **96 m in 5.3 s at 18 m/s**, so a one-shot fired at the
nose stayed where it was fired: the pass faded by the listener WALKING, never by the train
leaving. There is no better clip that fixes a stationary emitter.

⚠️⚠️ **AND THE FIRST FIX FOR IT WAS ITSELF WRONG, CAUGHT BEFORE THE BUILD.** It looped
`sfx_lrt_pass`, which is a **one-shot**: 2.70 s long, beginning and ending on a sample value of
**zero** because it was authored with a fade in and a fade out. The pass lasts 5.33 s, so the loop
**dropped the train to silence at 2.70 s and swelled it back from nothing while it was directly
overhead**. There is now a second cue, `sfx_lrt_rumble`: a 2.0 s bed with no envelope, its noise
filtered **circularly** (three copies concatenated, filter run across, middle third kept) so the
filter state matches at the seam, and every tonal component completing a whole number of cycles in
the loop. Measured seam step **35** against a typical sample-to-sample step of **3,667**.
`sfx_lrt_pass` keeps its own job as the distant warning; it was a good one-shot and only ever a
bad loop.

Now a looping source parented to the consist, linear rolloff **12 to 70 m** (logarithmic drops
its range inside the first few metres and would be full volume across the whole arena), and
`dopplerLevel` **2.2** because the true 5 per cent shift at 18 m/s is inaudible. Screen shake is
re-armed every frame at a **squared** distance falloff so it is local to the pass, peaking at
0.30 against `CameraRig.Shake`'s 0.35 default.

### 22.8 The tutorial bar, where spacing destroyed the pairing ✅

🧑: *"confusing to look at this tutorial ui, didnt know clicking n would let u skip it or
backspace would let u quit"*.

⚠️ **THE GAP BETWEEN A KEY AND ITS OWN ACTION WAS THE SAME AS THE GAP BETWEEN THE TWO PAIRS.**
Five children of one row at a uniform 10 px, so nothing in the spacing said which word went with
which cap, and proximity is the only thing that ever says so. Now sub-rows: **7 px inside a pair,
30 px between them.** The action word also moved from `CreamMuted` to full cream, because
`KeyCap` draws a bright plate and the eye was landing on the box and reading "N" as a button
label. `SKIP` became `SKIP LESSON`, which is what separates it from BACKSPACE quitting the lot.

### 22.9 The witch's voice, and the generator that was never committed ✅

⚠️⚠️ **`tools/generate_hero_audio.py` DID NOT EXIST IN THE REPOSITORY AT ALL.** § 21.4 leaves her
borrowed `hero_nemu_grunt` open on the grounds that the generator "is UNSEEDED, so touching it
rewrites all seventeen existing voice files". `git log --all -- tools/generate_hero_audio.py`
returns **nothing on any branch**. And `tools/generate_ability_audio.py` line 35 does
`from generate_hero_audio import SAMPLE_RATE, write_wav`, so **the payload generator could not be
run from a clean clone**: the import failed before it reached a synth.

It is written now, seeded per cue from a written-down slot, and it **refuses to overwrite an
existing file** unless given `--force` or `--only`, which permanently removes the hazard the
entry was worried about. Verified two ways: a second `--force` run is byte-identical, and running
`generate_ability_audio.py` afterwards rewrote **zero** ability files, which proves the recovered
`write_wav` is byte-compatible with the original.

Phaister has `hero_phaister_grunt` and `hero_phaister_ult` and no longer borrows Nemu's throat.
The six ultimate voices were regenerated as vocalisations on request (*"maybe screams or laughter
or something ominously sounding that is in chracter"*) through a glottal-pulse-and-formant model,
one vowel shape per hero. **Hers is a laugh, and she is the only one who gets one**: five of the
six ultimates are efforts, and a witch calling an eclipse is not exerting herself.

⚠️ `sfx_ghost_teleport` **stays shared** between Nemu and Phaister deliberately. A blink IS the
same physical event a phase is; what she needed was her own throat over it.

### 22.10 `Checks.RunAll` had been red since the witch merge ✅

`HeadlessCheck` asserted `HeroPeople.Count == 5` and `AllPeople.Count == 17`. § 21 merged Phaister
as the **sixth** hero and updated `Roster` without updating the check that counts it, so every
`Checks.RunAll` launch since has failed on two assertions. **It went unnoticed because the § 21
verification pass quoted Core, EditMode, PlayMode and `AbilityShowcaseProbe` and never ran this.**
The totals are derived from the two lists now rather than retyped.

---

## 23 · Ability stuns are now fought out of, not waited out

**Asked for by 🧑 on 2026-08-26, in four messages:** *"for abilities that freeze or stun enmies /
i want them to look frozen or have the element cover them when stunned"*, *"i want them to go to
TPP and to have a button mashing thing to get unstunned or unfrozen (same as when u trip) but
maybe diff UI and effect"*, *"maybe with this change u have to make sure the countdown for their
stun is gone as well as the ui for the countdown"*, *"i want their ui to also have the frozen or
stunned effect (depending on the element) / atleast until they button mash and then theyre out of
it"*, and *"maybe chaneg the amt needed to be button mash for each skill? make it dependent on how
hard the skill is supposed to hit"*.

⚠️⚠️ **THE TAYA'S TAG IS DELIBERATELY EXCLUDED AND THAT IS THE LOAD-BEARING DECISION.**
`Balance.TagStunTime` is 5.0 s and the tag is the ONE scoring verb a defender has
(`docs/VISION.md` § 4). Letting an attacker hammer out of it would take the single thing the taya
can do and halve it, in the mode aimed at a bracket. **A tag is answered by not being caught.**
So the two statuses now read differently on purpose: a tag drains colour and cannot be fought, an
ability stun coats you in the caster's element and can be. One is a rule, the other is a fight.

⚠️⚠️ **MASHABILITY IS READ OFF THE ELEMENT, NEVER OFF THE DURATION.** `StunElement.None` is the
tag. Guessing from the number would have made the tag escapable the moment somebody tuned an
ability to 5 s.

⚠️ **THE COST IS A PRESS COUNT PER ABILITY AND THE SECONDS ARE DERIVED**, which is what makes
"how hard does this skill hit" a stable thing to tune: `perPress = (stunTotal - MinStunDown) /
breakPresses`. A stun retuned from 3 s to 5 s still breaks in the number of presses its ability
asked for. Shipped: Cheska's Glacial Nova **9**, Dante's Titan Fissure **8**, Zack's Thunderstrike
**7**, Nemu's poltergeist **6**, Sean's burn **4**. Clamped to 3..14.

⚠️ **A STAGGER SHORTER THAN `MinStunDown` IS FORCED BACK TO `None` INSIDE `ApplyStagger`.** Most
kit calls are 0.2 to 0.5 s knockback hitches; at those lengths the card would appear reading
BREAKING FREE, the camera would swing to TPP and back, and the body would flash a coat, several
times a round. **The floor is the definition of a hold**, so kits do not each have to judge
whether their number is big enough.

⚠️ **THE FLOOR IS NOT CLEARED BY THE LAST PRESS.** Releasing the body when the meter fills would
put a perfectly answered 3.0 s stun at **0.6 s** and refund the cooldown that bought it. The
honest total is the mash plus the floor, about **1.7 s** against 3.0 unanswered, because the clock
drains underneath the presses.

⚠️ **THE CARD IS PIPS, NOT A BAR, AND THE TWO REQUESTS ANSWER EACH OTHER.** A bar hides how many
presses are left because it is a ratio; one pip per required press shows the number, so the
per-skill tuning is legible instead of secret. **The thing being tuned is the thing being
displayed.**

⚠️ **THE COUNTDOWN ROW IS GONE FOR ELEMENT STUNS ONLY.** `Balance.TripAutoRecoverSeconds` records
the lesson: once a status is ended by presses, a countdown beside it is a second, contradictory
account of when it ends, and he reported exactly that against the fall (*"u randomly get up after
set amt of time, i dont have to actually mash it"*). **The tag keeps its countdown**, because time
really is the only thing that ends it.

**Still open:**

* **The press counts have not been measured against a match.** They are reasoned from
  `Hero_Strike_Balance.md` § 1's weighting and want a `BotBehaviourProbe` sweep, remembering
  § 16's noise floor: three runs an arm for anything worth 20 per cent.

✅ **CLOSED 2026-08-26, both of the others:**

* ✅ **Phaister's eclipse curse now holds.** It was 0.50 s, below `MinStunDown`, so it drew no
  coat and could not be mashed. It is **1.60 s, five presses, inside a 5.0 m reach**, and § 24.3
  has the arithmetic for each of the three numbers and why a multi-target hold has to be shorter
  per victim than a single-target one.
* ✅ **Bots DO mash out of stuns, and this entry was stale the day it was written.** The
  condition at `AIController.Update` reads
  `if (_motor.IsTripped || _motor.StunElement != StunElement.None)` and alternates `Verb.Jump` off
  a dedicated field. § 22's bot-mash fix covered element stuns as well as trips in the same line;
  the entry was written from the older code. ⚠️ **Verified by grepping the call site rather
  than the entry**, which is § 22's own rule turned on § 23.
* ⚠️ **A NEW ASYMMETRY REPLACED THE OLD ONE AND IS FIXED.** `Tap` alternates every frame, so
  a bot's hold of a `HeroAbility.HoldToAim` power is one frame long: every bot Phaister would have
  blinked the minimum 2.0 m forever. `AIController.HoldAim` holds for the ramp. § 24.2.

---

## 24 · Phaister's three powers were one builder at three radii

**Reported by 🧑 on 2026-08-26, and it is the sharpest kind of report there is: he read the code
off the screen without seeing it.** *"the fucking abilities of phaister are repetitive they use
the same magic circle i want them to have different colors and different animations and different
symbols. DIFFERENT EVERYTHING FIGURE OUT HOW THEY WILL ALL BE DIFF"*, then, precisely: *"her Q is
just 2 stars on top of each other"*, and the general rule: *"pls dont use the same script to
generate any abilitiy as it will feel cheap and it will look all the same"*.

⚠️⚠️ **HE WAS DESCRIBING SOMETHING LITERALLY TRUE.** Every witch effect went through
`HeroHazards.SpawnWitchSigil`, which drew `VfxShapes.Sigil` **twice**, an outer star polygon and
an inner one, counter-rotating. `SpawnCastGlyph` called it with a hard-coded `5, 2`. So the hex,
**both ends** of the blink and the ultimate were the same pentagram stacked on itself, varying
only by radius and by a seed that jitters rim ticks and nothing else. Two stars on top of each
other, four times, in one kit.

⚠️⚠️ **AND § 21.5 ARGUED FOR IT ON PURPOSE, WHICH IS THE PART WORTH KEEPING.** That entry says
her kit is one CRAFT, so the three should be told apart *"by SIZE, by how many rings they carry
and by where they sit"*. The reasoning is good and the conclusion does not follow: **a shared
visual language is a palette and a vocabulary, not a shared mesh function.** Taking it to mean
one builder is how a whole hero became three sizes of one object, and it is the same class § 19
named for the other five (*"fifteen poses sharing one construction"*) arriving by a different
argument.

✅ **DONE 2026-08-26. Three constructions, three motions, three places in space, three hues.**

| | Q, HEX | E, SHADOW BLINK | R, GRAND COVEN |
|---|---|---|---|
| Built by | `VfxShapes.WardCircle` | `VfxShapes.Rift` + `Rune` | `VfxShapes.Corona` + `SkyEvent` |
| Made of | rings, a written band, nested squares, medallions | two ragged edges and the threads between them | tapering teeth around an empty middle |
| Geometry | rectilinear, closed figures | a vertical split | a ring seen from BELOW |
| Where | on the road | at head height, facing the blink | 11 m up |
| Motion | inscribed in 0.42 s, then **dead still** | opens across, snaps shut vertically | falls out of the sky, turns slowly |
| Hue | magenta rules, gold writing | near-white: a rip is not a colour | gold on black, no magenta at all |

⚠️ **NOT ONE OF THE THREE IS A `{points/skip}` STAR POLYGON**, which is the one shape he named.

### 24.1 The ward, and why it is static

`VfxShapes.WardCircle` is built the way 🧑's references are: two rules with a **band of written
glyphs** between them, radial dividers making the band into cells, **two squares 45 degrees
apart** forming an octagram with flat sides and real corners, an inscribed triangle to break the
eight-fold symmetry, and four **medallions** on the rim each holding a glyph.

⚠️ **A SQUARE IS THE SHAPE NOTHING ELSE IN `VfxShapes` MAKES, AND THAT IS THE SEPARATION.** A
star polygon is one loop of strokes chasing itself around a centre, so however many points it has
it reads as a spinner. Closed straight-sided figures laid over each other read as a diagram
somebody ruled.

⚠️ **THE MEDALLIONS ARE WHY IT SURVIVES EYE HEIGHT.** A ground mark is nearly edge-on at 1.65 m
and a rim band compresses to a line there; four small circles keep four places on the mark
legible from the side, and they say which way it is facing, which a rotationally symmetric ring
cannot.

⚠️⚠️ **AND IT DOES NOT ROTATE. THAT IS THE ANIMATION.** `WitchSigilSpin` turned two wheels
against each other for the whole six seconds, which is right for a summoning circle in a cutscene
and wrong for a trap on a road: a moving mark is one the eye keeps returning to while it is armed.
`WardInscribe` writes it on in 0.42 s, rules first and gold writing lagging a third behind, and
then it is still until it expires. **Its motion happens once.**

⚠️ **ONE MESH, NOT TWO STACKED ONES.** The old pair were two `Sigil`s 8 mm apart, which is the
coplanar translucent pair § 19.2a records sorting arbitrarily on Sean's trail and drawing a
different colour per drop. Everything the ward is made of goes into one triangle list.

### 24.2 The blink is a teleport now, and the effect used to lie about that

🧑: *"let her HOLD e to control where she will go and make it a teleport abilitiy and make it
prettier"*.

⚠️⚠️ **THE VFX WAS LYING ABOUT WHAT THE CODE DID.** `OnActivate` computed
`endPos = startPos + forward * 4.2` **only to feed the visual**, then moved the body with
`ctx.Motor.ApplyImpulse(pushDir * 12.0f)`. She was shoved four metres **through whatever was in
the way** while two glyphs claimed she had vanished and reappeared.

* **Hold E to aim, release to blink.** `HeroAbility.AimByHolding` puts the mechanism on the base
  class: the press begins aiming, the release casts, and the reach runs **2.0 m to 5.5 m over the
  first 0.55 s** of the hold.
* **`CharacterMotor.Teleport`, never an impulse.** That method clamps **X and Z independently**,
  which is `CLAUDE.md` § 4's square box: a radial clamp disagrees with it by 2.9 m on the
  diagonal, exactly where somebody blinks when cutting a corner. Its own note records it as the
  path that once put *"a seat 45.8 m out on X against a half width of 8.6"*, so it is the one
  that has been fixed. `HeroAbilitySystem.AimPoint` clamps the reticle from the same constants,
  so the ring and the landing cannot disagree.
* **Holding is worth nothing by itself**, which is `docs/VISION.md` § 4. She keeps full movement,
  nothing about aiming touches the anti-camp or anti-stall clocks, the hold **fires itself at
  1.10 s** rather than being cancellable, and the reach stops growing at 0.55 s so the second
  half of the ceiling buys nothing. The cap alone would only have bounded the stall; the ramp
  ending early is what removes the incentive.
* **The shove is host-side and has a request path.** It moves three bodies the caster does not
  own; on a client that meant three victims rubber-banding on the host's next transform sync.
  `MatchRpc.RequestBlinkShoveServerRpc` is the ask, and it carries a POINT and a FACING rather
  than a list of who was hit, so a client cannot name its own victims. ⚠️ The seat comes from the
  sender's lobby record, never from the payload.
* **A bot holds the key.** `Tap` alternates every frame, so a bot's hold is one frame and every
  bot blink would have been the minimum 2.0 m forever. `AIController.HoldAim` holds for the ramp
  and releases, which is `CLAUDE.md` § 4's *"a bot presses the same buttons a human does"*.

⚠️ **THE TWO ENDS ARE TWO DIFFERENT EFFECTS.** Departure is `SpawnShadowRift`: a torn vertical
sheet, two ragged edges pinched shut at both ends by a sine so it is a split in a surface rather
than a gap in a wall, with three cross-strokes reading as the last threads. Arrival is
`SpawnShadowArrival`: six written characters falling onto the spot on their own clocks. **They
share no geometry.** The departure is the bigger of the two because it is where the shove is
centred and the thing the three people chasing her did not already know.

### 24.3 What the ultimate does, which is the open balance question in § 23 answered

⚠️⚠️ **§ 23 LEFT IT OPEN IN AS MANY WORDS AND THE ANSWER IS WRITTEN DOWN HERE.** *"Phaister's
eclipse curse staggers for 0.50 s, below `MinStunDown`, so her ultimate does not hold anybody and
gets no coat."* `ApplyStagger` forces anything at or under the 1.20 s floor back to
`StunElement.None`, so the most expensive power in her kit applied a knockback hitch, drew no
coat, raised no mash card, and was unmashable and unnoticeable at once.

**Now 1.60 s, five break presses, and only inside 5.0 m.** The three numbers, each with its
reason:

* **1.60 s** clears `Balance.MinStunDown` by 0.40, which is the smallest hold that actually is
  one.
* **5 presses** against Cheska's 9, Dante's 8, Zack's 7, Nemu's 6 and Sean's 4. § 23's rule is
  *"how hard the skill is supposed to hit"*, and what separates this from Cheska's nova is that it
  can hold **three people at once**: a multi-target hold has to be shorter per victim than a
  single-target one or it is three novas for one price. `perPress = (1.60 - 1.20) / 5 = 0.08 s`,
  so an answered curse is about 1.2 s against 1.6 unanswered.
* **5.0 m**, where it used to hit `round.Players` with **no distance test at all**. An ultimate
  that reaches the far corner of a 14 m box cannot be positioned against, and positioning is the
  counterplay. The reach is drawn on the ground and asserted by
  `TelegraphsMatchWhatTheAbilityPlaces`.

### 24.4 The sky, which is the other half of what an ultimate is now

🧑: *"can we make her ult cooler? on top of magic and shit / i want the sky to look ominous and
shit and change for a brief moment into night and filled with magic"*, and then *"maybe give some
other characters other versions of this"*. That is § 26.

`SpawnGrandCovenEclipse` puts **nothing on the floor** but a thin `Collar` at the reach. The
eclipse is a `Corona` hung **11 m up** with an opaque moon in the middle of it, and the weather is
`Visual.SkyEvent`.

⚠️ **11 M IS UNDER THE GUIDEWAY.** Ilalim ng Tulay has a deck over the street, so an eclipse at
40 m would be behind the map on the one arena it was designed for.

⚠️ **THE MOON IS THE ONLY OPAQUE THING IN THE EFFECT, AND THAT IS § 19.2a's RULE APPLIED.** A dark
disc ghosted over a bright corona is two coplanar translucent plates; an eclipse also has to
actually OCCLUDE to read as one, and an opaque renderer writes depth by construction rather than
by winning a sort. It costs no floor because it is eleven metres up.

⚠️ **THE LIGHT IS 2.4 AT 26 M, NOT SOMETHING BRIGHT AND CLOSE.** § 8b is the recorded cost of the
other choice: Zack's ultimate ran a 6.0 point light over 17.5 m in a 14 m box and blew **62.8 per
cent** of the frame to white.

**Where.** `Assets/TumbangPreso/Runtime/Visual/VfxShapes.cs` (`WardCircle`, `Rift`, `Corona`,
`FlatRune`), `Assets/TumbangPreso/Runtime/Abilities/HeroHazards.cs`,
`Assets/TumbangPreso/Runtime/Abilities/PhaisterHeroKit.cs`,
`Assets/TumbangPreso/Runtime/Abilities/HeroAbility.cs` (hold to aim),
`Assets/TumbangPreso/Runtime/Abilities/HeroAbilitySystem.cs`,
`Assets/TumbangPreso/Runtime/Visual/SkyEvent.cs`.

⚠️ **`SpawnWitchSigil`, `WitchSigilSpin` AND `SpawnCastGlyph` WERE DELETED RATHER THAN LEFT
UNUSED.** § 22 opens on the pattern that cost two sessions: *"grep for the call site, not the
asset"*. A helper named `SpawnWitchSigil` sitting in the file every witch effect is written in is
the helper the next witch power gets built out of, whatever any comment says. `VfxShapes.Sigil`
itself survives and is now used by nothing.

---

## 25 · Which peers actually hear a sound, measured rather than assumed

**Asked for by 🧑 on 2026-08-26, and he called it correctly:** *"Verify SFX actually reach every
peer in multiplayer. This is unverified and is the risky one. Most cues fire from kit code that
runs host-side; a cue played inside a branch gated on `NetAuthority.ShouldResolve()` is silent on
clients. Audit every `GameServices.Audio` call site for which peers reach it."*

✅ **AUDITED AND TOOLED. `tools/audit_audio_reach.py` walks the runtime tree, finds each audio
call's enclosing method, and reports whether a `ShouldResolve` early return is open at that brace
depth.** 63 call sites, and **two came back HOST-ONLY**. Both are load-bearing:

| Where | Cue | What it means |
|---|---|---|
| `Carrier.HostThrowAt` | `throw_release` | **No peer but the host has ever heard a throw leave a hand**, and the throw is the most frequent verb in the game. |
| `Lata.HostKnockDown` | `lata_seal` | The sound of the **objective going over**, which is the most important event in a round. |

⚠️⚠️ **THE FIX IS NOT TO MOVE THE CALL OUT OF THE GATE.** The gate is right: only the host may
DECIDE a throw happened. What was wrong is that deciding and announcing were the same line.
`TumbangPreso.NetCue` separates them, which is the shape `NetAuthority`'s own class note already
prescribes for every verb (*"host decides -> HostResolveX() ... host announces -> RpcX()"*).

⚠️ **IT IS A NO-OP IN SINGLE PLAYER**, because `NetAuthority.IsNetworked` is false with no
transport up. Nothing about the offline game, the bot probes or the editor checks changes, and no
call site has to ask which mode it is in.

⚠️ **THE RELAY EXCLUDES THE PEER THAT MADE THE SOUND.** `NetCue` plays locally first so the
thrower hears it on the frame they threw with no round trip; sending to everybody would give that
one peer the sound twice a few tens of milliseconds apart, which is a flam rather than an echo and
is worse than either.

⚠️ **UI AND REFUSAL CUES MUST NOT GO THROUGH IT.** `HeroAbilitySystem.PlayRefusal`'s own note is
explicit that its cue is *"on the player rather than at a world point"*; broadcasting it would
play one player's mis-press in three other people's ears.

### 25.1 The larger finding, which is not about audio ✅ CLOSED 2026-08-27, SEE § 38

**The ability layer is not replicated at all.** `MatchRpc` has RPCs for movement, the punch, the
lunge, the shove, the grab, the throw, the reset, emotes, the lata, slippers, seats, picks, maps
and the world snapshot. **It has none for a skill or an ultimate**, and
`grep -n "Skill\|Ultimate\|Ability" Assets/TumbangPreso/Runtime/Net/*.cs` returns nothing outside
comments.

Every seat gets a `HeroAbilitySystem` on every peer (`MatchInstaller`), and it casts off
`_motor.Intent`, which only the owning peer writes. So in a networked match a remote player's
ultimate produces **no VFX, no sound, no `UltimateColumn` and now no sky** on anybody else's
screen. What they see is the consequences: bodies moving, a score changing.

⚠️ **THIS IS BIGGER THAN A BUG AND IT IS WHY IT IS WRITTEN DOWN RATHER THAN PATCHED HERE.** The
right shape is the one every other verb already uses: the owning peer sends a cast INTENT (ability
id, position, facing, and for a hold-to-aim power the held seconds), the host resolves it with the
same code the solo game runs, and a `PlayAbilityClientRpc` makes every peer draw it. That is a
session's work on its own and it wants `NetAuthority`'s three-line pattern followed exactly.

**Done looks like:** two peers, one Hero Strike match, and the same effect on both screens for
every one of the eighteen powers.

✅ **DONE 2026-08-27, exactly as this entry specified.** `ReqAbility` carries the CAST (seat, slot,
position, facing, aim point, hold time), the host re-checks cooldown, charge, role and stun state
and resolves with the same kit code the solo game runs, and `PlayAbility` makes every observer
replay the presentation. `tools/audit_ability_authority.py` now reports **40 effect call sites, 25
host-gated, 0 ungated on another body**, against the 23 this entry was written about. The 15 still
listed as "ungated on the caster" are correct: the owner predicts its own body and
`CharacterMotor.MayMutateGameplayState` is what stops an observer doing it for somebody else.
⚠️ **§ 38 is the rest of the network pass** and § 38.15 is what is still open, which is two real
machines on a LAN.

⚠️⚠️ **AND THERE IS A NUMBER ON IT NOW.** `tools/audit_ability_authority.py` (2026-08-27, § 31.11)
walks the ability tree for every call that moves a body or writes score and reports whether a
`NetAuthority.ShouldResolve()` gate is open at that brace depth, exactly as `audit_audio_reach.py`
measures the audio half. It reports **39 effect call sites, 1 host-gated, 23 ungated on another
body, 15 ungated on the caster**. The one gated call is Phaister's `Curse`. Run it for the
worklist rather than re-deriving it: HOST-ONLY is the correct state for anything in that column.

### 25.2 Two cues added, and the bar they had to clear

🧑 set it himself: *"Find where a sound is missing, but keep it sparse. The bar is a player having
to guess whether something happened. Nothing that already reads visually."* Two passed:

* **`sfx_blink_arrive`.** The blink plays `sfx_ghost_teleport` at the DEPARTURE, and after § 24
  the far end can be 5.5 m away: whoever is standing there heard nothing at all. A cue fired at
  the start point cannot cover it, for the reason § 22.7 records about the train, which is that
  `PlayAtVaried` parks a pooled voice at the position it is given.
* **`sfx_stun_break`.** § 23 built the whole mash-out system and ended it in silence. It fires on
  the press that reaches `MinStunDown`, not on every press: one per break is sparse, one per press
  is a cue at up to 10 Hz, which is the buzzsaw `AudioCues.HeadroomDb` exists to keep out. **One
  cue for all six elements**, because the coat says what is on you and the break is the same event
  whoever caused it.

⚠️ **THE SEEDING HELD, AND IT WAS CHECKED RATHER THAN ASSUMED.** § 19.2b records
`generate_ability_audio.py` once seeding from a position in a sorted list, so adding a cue rewrote
seven shipped files. Adding these two changed **four files on disk: the two new cues, in the two
output directories.** Nothing else moved.

⚠️ **NOTHING ELSE PASSED THE BAR**, and that is the result rather than the absence of one. The
sky change was considered and rejected: six ultimates already fire a payload cue at the instant
the weather turns, and a seventh layer there is clutter, not sparsity.

---

## 26 · Every ultimate changes the weather, and each hero changes it differently

**🧑 2026-08-26, after asking for Phaister's eclipse:** *"maybe give some other characters other
versions of this"*.

✅ **DONE. `Visual.SkyEvent`, six looks, wired at the one point every ultimate in the game passes
through (`HeroAbilitySystem.PlayUltimatePresentation`).**

⚠️⚠️ **IT EXISTS BECAUSE THE FLOOR IS FULL AND THE SKY IS EMPTY.** `VISION.md` § 2 is a budget on
painted floor and every previous attempt to make an ultimate feel bigger spent it.
`Visual.UltimateColumn` made the argument first: **an environment change costs zero square
metres.**

| Look | Hero | What it is | What carries it |
|---|---|---|---|
| `Eclipse` | Phaister | afternoon into night | the deepest of the six, and the only fill that is a hero accent |
| `Stormfront` | Zack | iron cloud, cold blue key | the only one whose key light **flickers**, on Perlin noise rather than a sine |
| `Whiteout` | Cheska | colour drains, fog closes | saturation **0.34**, the strongest desaturation of the six |
| `Emberfall` | Sean | a sun behind smoke | the sun stays high and goes orange, which is hotter than a brighter frame |
| `Dustveil` | Dante | ochre, thick and low | fog comes closest of the six; the sky nearly goes out |
| `Seance` | Nemu | the light barely moves, the COLOUR goes wrong | green ambient under a violet sky, a combination daylight never makes |

⚠️⚠️ **EVERY LOOK IS NET-DARKENING OR NEUTRAL, AND IT IS ENFORCED IN CODE RATHER THAN BY THE
TABLE.** `ColourGrade.SetEventGrade` clamps brightness to a maximum of 1.0. A system that can
brighten the whole screen for five seconds is § 8b's Thunderstrike defect with a longer fuse, and
that one measured **62.8 per cent** of a frame blown to white. The looks are separated by HUE, by
ambient DIRECTION and by fog, which are free.

⚠️⚠️ **AND DARKENING IS PAID FOR WITH A FILL LIGHT RATHER THAN OUT OF READABILITY.** Rule 5 asks
that a mid-fight frame still show the lata, the chalk and every player; dropping the sun to a
fifth would break that outright. Every look raises a coloured fill 11 m over the arena centre, so
the court is lit **differently** rather than **less**. That is also what "ominous" actually looks
like.

⚠️ **NEITHER FAILURE MODE IS CAUGHT BY ONE NUMBER.** `AbilityShowcaseProbe` now photographs all
six, gated for the blowout bound, which catches the bright half. The dark half has no number and
is what the eye-height frame is for.

⚠️ **IT RESTORES FROM EVERY EXIT, AND `RoundDirector.EndRound` IS THE ONE THAT SAYS WHEN.**
`RenderSettings` is scene-global and outlives the object that wrote it: an ultimate cast in the
last second of a round would otherwise still be blending toward night over the scoreboard, and a
teardown at the wrong moment would leave the map permanently dark with nothing on screen to say
why. The skybox is **instanced** rather than written through, because `RenderSettings.skybox` is a
project asset and editing it in play mode changes the map on disk.

⚠️ **A HERO WITH NO ROW GETS NO WEATHER, NOT SOMEBODY ELSE'S.** `LookFor` returns null rather than
a default, which is § 8 item 3's fault avoided by construction: *"Sean's Supernova was spawning
Dante's magma. Two heroes reading as one is the most expensive form of repetitive, because it
costs a character."*

---

## 27 · The other five heroes need a motif, and it is not more symbols

**🧑 2026-08-26, setting the task and the bound in one line:** *"look for a motif OR something
else we can try to add to increase the quality or experience of playing the characters, so that it
doesnt feel like party confetti or some shit"*, and separately: *"pls dont use the same script to
generate any abilitiy"*.

⚠️⚠️ **PHAISTER'S MOTIF IS WRITTEN LANGUAGE BECAUSE SHE IS A WITCH. COPYING IT IS THE FAILURE
MODE.** § 24 gave her rings, glyph bands and medallions; giving Zack recoloured runes would be
§ 19's fault at the level of the whole roster instead of the level of one builder. **Each hero
needs their own answer to "what does this element leave behind".**

⚠️ **THE METHOD IS § 19.1's AND IT IS THE ONLY PART THAT TRANSFERS.** Ask how the geometry is
BUILT, not what colour it is. A motif is a construction rule that produces many different objects,
which is why one builder per hero is wrong too: `WardCircle`, `Rift` and `Corona` are three
builders serving one motif.

**The five, as proposals with their acceptance test. None of these is built.**

### 27.1 Sean, fire: **what is left burning**

His trail already has `Tongue` flames and an opaque char. The motif is that fire **spreads and
outlives the cast**: a mark that grows outward for a moment after it lands, edges that char
inward, and heat shimmer over anything he has touched in the last few seconds.

* **Construction:** a char boundary that ADVANCES, built as a ring of independently-timed
  segments rather than a scaling disc, so the edge is ragged and different every drop.
* **Done looks like:** two drops of the same trail, photographed 1 s apart, are visibly different
  ages rather than different sizes. `Burn`'s shrink-toward-its-own-end already does half of this.

### 27.2 Zack, electric: **the circuit**

Every other hero's effects happen in empty space. Electricity is the one element that wants to
**jump between things that exist**: arcs from him to the lata, to a barricade, to another player,
following what is actually on the court.

* **Construction:** `Bolt` already walks a tube from a to b. The motif is choosing a and b from the
  live scene rather than from a random offset, which is a different kind of code from every other
  effect in the game.
* **Done looks like:** standing next to the lata while he is charged does something visible that
  standing in an empty corner does not.
* ⚠️ **It must not become a targeting aid.** The arc says where charge is, not where a player is;
  arcing to a body through a wall would be an aimbot drawn in lightning.

### 27.3 Cheska, ice: **the fracture**

Ice does not appear, it **propagates along cracks**. Her sheet grows as a branching crack pattern
rather than as a disc, and her barricade fails by **shedding real plates** rather than fading.

* **Construction:** a recursive branch walk laying `Prism` slivers along its path. `Wedges` is the
  wrong builder: it makes separate plates with gaps, and a crack is connected.
* **Done looks like:** the sheet's outline is different every cast from one seed, and the
  barricade's death leaves geometry on the ground for a moment.

### 27.4 Dante, earth: **displacement**

His motif is that the ground he breaks **goes somewhere and stays there**. `Wedges` already tips
plates; what is missing is that nothing he does leaves a permanent mark on the round.

* **Construction:** the ground under a stomp drops a few centimetres and the displaced material
  stands up at the rim. One mesh, two halves, conserved.
* **Done looks like:** you can see where Dante has been fighting from across the arena, thirty
  seconds later.
* ⚠️ **It must not become collision.** `MapGeometryCheck` refuses props that float or bury, and a
  hole a player can stand in is a hole the bots will path into.

### 27.5 Nemu, spirit: **absence**

The hardest one and the most valuable. Every other hero ADDS something to the frame; hers should
**take something away**. Things she touches lose colour, lose their outline, lose parts of
themselves.

* **Construction:** not geometry at all, mostly. A material effect on what is already there, which
  is a category this game has exactly one of (`_FrostAmount`, and § 22.6 freed it up by taking the
  frost off the tag).
* **Done looks like:** a screenshot of her ultimate has fewer things in it than the same frame
  without it, and it still passes rule 5.
* ⚠️ **She and Phaister are the only pair sharing an element** (§ 21.5), so this is the one that
  most has to not be a sigil.

**Where it would go.** `Assets/TumbangPreso/Runtime/Visual/VfxShapes.cs` for builders,
`Assets/TumbangPreso/Runtime/Abilities/HeroHazards.cs` for the spawners, and a row per hero in
`AbilityShowcaseProbe` so each is photographed alone before anything overlaps it.

---

## 28 · Nemu's ultimate is her pet now, and her kit is named after him

**Reported by 🧑 on 2026-08-26, and the diagnosis is his:** *"her black hole dont make sense
lowkey? maybe just make nemu's pet the black whole and make it look like it got bigger and is
sucking everyone up, change the text that says its a blackhole"*, then *"for nemu i want her
skills to involve her pet more as well as her ult"*, *"make her pet move and do shit and go back
to her after"*, and *"use her pet name in new skill name and skill descriptions"*.

⚠️⚠️ **HE IS RIGHT AND THE REASON IS A DESIGN RULE RATHER THAN A PREFERENCE. Everything Nemu
does is Kuro, and her most expensive power was the one that ignored him.** Seance Void opened a
vortex three metres in front of her, out of nothing, with the pet standing beside it unchanged.
That is a physics effect wearing her colour, and it is why the ultimate never read as hers.

✅ **DONE 2026-08-26.**

### 28.1 KURO UNBOUND

* **It opens ON Kuro** whenever Kuro is out, which makes RIDE KURO a setup for it: send the pet
  somewhere, then unbind it there. With no pet out it falls back to a point in front of her, and
  the fallback is deliberately the worse option, because the reward for playing her kit as a kit
  is choosing the spot in advance.
* **The pet is consumed by it.** `GhostPetCompanion.Devour` hides Kuro for the duration and the
  maw stands where he was, so what the other three see is the small thing that has been following
  her all round becoming the thing that is eating them. A vortex spawned beside an unchanged pet
  would have been the old effect with a new name.
* ⚠️ **A possession ends the moment he stops being a body.** Driving a pet that is currently a
  black hole is a state nothing else in the game has an answer for, and the camera would be
  mounted 2 m behind an object eight times its own size. It ends with `teleportNemu: false`,
  because her ultimate is not a mobility power.
* ⚠️ **The hazard is untouched at 2.8 m.** The drag, the slow and the `HazardVolume` are what
  `Hero_Strike_Balance.md` measured and what the bots path around. **This is a fiction and
  presentation change and it must not quietly become a balance one.**
* **The maw has real height and the old void did not.** `Funnel` dished DOWN, which reads from
  above and disappears at eye level; a mouth standing 1.2 m off the road is readable from a
  standing player's own height, which is where this game is played from.

### 28.2 The pet flies home, and everyone sees it

🧑: *"after her ult ends make the pet go back to her make sure she sees that as well as everyone
else"*.

⚠️⚠️ **THE CHEAP IMPLEMENTATION IS THE WRONG ONE AND IT IS THE OBVIOUS ONE.** Re-enabling the
renderer at her feet is a pet that VANISHES and REAPPEARS, and **from her own first-person view
nothing happens at all**, because the bind offset puts him behind her shoulder. So the return is
FLOWN: `ReturnSeconds` **0.85 s**, on an arc lifted **1.6 m** at its middle so the whole flight is
against the sky for players who are not standing on it, scaling back up as it goes, with a small
overshoot that closes in the last fifth. It is a world-space move, so it is not a local effect.

### 28.3 Her kit is named after him

| | Was | Now |
|---|---|---|
| Skill 1 | GHOST STEP | **KURO'S SHADOW** |
| Skill 2 | ASTRAL PROJECTION | **RIDE KURO** |
| Ultimate | SEANCE VOID | **KURO UNBOUND** |

⚠️ **THE WORD "VOID" IS GONE FROM EVERY STRING A PLAYER READS**, and `Id` is unchanged at
`nemu_ultimate`. Ids are keys: `HeroPresentationTests`, the HUD deck and the ability tray all
index off them, and renaming a key to match a label is how a rename becomes six silent lookup
failures. `AbilityGlyph.NemuSeanceVoid` is kept for the same reason.

⚠️ **AND THE SUMMARIES WERE CAUGHT BY A TEST RATHER THAN BY EYE.**
`EverySummaryFitsTheCardItIsDrawnIn` bounds a summary at **62 characters**, because the character
select card truncates silently; the first draft of all three ran over. That test exists because
four of the fifteen powers once described themselves in a sentence that stopped mid-word.

### 28.4 Two faults found on the way, and one was a live hazard

**28.4a ⚠️⚠️ HER TELEPORT HOME SPAWNED ZACK'S SHOCK TRAIL, WITH A `HazardVolume` ON IT.**
`GhostPetCompanion.EndPossession` called `HeroHazards.SpawnShockTrail(...)`, so every time Nemu
teleported to Kuro she dropped **a two-second electric damage zone belonging to another hero** on
the road. It is § 8 item 3's fault (*"Sean's Supernova was spawning Dante's magma"*) with a
gameplay consequence on top of the visual one. Replaced with `SpawnSpiritReturn`, which is built
on her own `VfxShapes.Hollow`, lasts half a second and damages nobody.

**28.4b ⚠️⚠️ LEAVING A POSSESSION WAS A HARD CUT.** `CameraRig.PossessBlendSeconds` carries the
eye from her head to the mount behind Kuro over 0.28 s, and that blend is most of why the
possession reads as one; the return simply stopped drawing that view and the next frame was
rendered from her skull. 🧑: *"make sure i switch to tpp view when i go to the body of the pet of
nemu and control it, when it ends too"*. Now blended both ways, same length.

⚠️ **The exit blend also covers the teleport**, which is the worst frame in the ability: pressing
again while possessing moves her BODY to the pet, so the eye's destination jumps several metres in
the same frame the possession ends.

### 28.5 Her motif, and two cues

`VfxShapes.Hollow` is a rim around **nothing** with a torn inner edge, and nothing else in the
game uses it. § 27.5's argument: every other effect in this game ADDS something to the frame and
hers has to look like it removed something, so the way to draw a hole is to draw only its edge.
It is deliberately not `Collar`, which is the boundary builder every other rim uses: a `Collar`
says *here is an edge* and this says *here is where something stopped existing*.

* **`sfx_kuro_unbound`** is an INHALE, which is the one envelope nothing else in the cue set uses:
  every other payload is a strike, an edge and a decay. It swells from nothing into its loudest
  moment and stops there, and the stop is what says the maw is open rather than still arriving.
  Her ultimate had been playing `sfx_ghost_teleport`, the blink she shares with Phaister.
* **`sfx_kuro_return`** is the only cue in her set with a smile in it. Everything else she has is
  a hole, a possession or a maw; the pet flying back to her shoulder is the one moment in the kit
  that is not sinister, and another dark swell would have made the character one note.

---

## 29 · The other four heroes got their motif, and none of them shares a builder

**🧑 2026-08-26:** *"improve all other abilities thoroughly too thank you, in their own ways u
figure out how, make sure they dont share builders"*, and *"make all other skills better as well,
look better, feel better etc / nemu and phaister are done, do everyone else"*.

⚠️⚠️ **THE FIVE KITS ALREADY HAD DIFFERENT BUILDERS AND STILL FELT ALIKE, WHICH IS THE FINDING.**
§ 19 gave each of them its own construction and that pass was right. What none of them got was an
answer to the question a MOTIF answers, which is not *"what does this ability look like"* but
**"what does this element LEAVE BEHIND"**. Four new builders, four signature layers, one per hero:

| Hero | Builder | The motif | The acceptance test |
|---|---|---|---|
| Cheska | `Fracture` | ice **propagates** along cracks | the outline differs every cast from one seed |
| Dante | `Upheaval` | earth is **displaced**, not removed | you can see where he fought, later |
| Sean | `Cinder` | fire **spreads** and outlives the cast | two drops of one trail read as different ages |
| Zack | `Filament` | current wants a **circuit** | standing by the lata looks different from standing alone |

⚠️ **NOT ONE OF THEM IS A FAN, WHICH IS WHAT EVERY EFFECT IN THIS GAME USED TO BE.** § 19:
*"`Splat`, `Star`, `Streak` and `Crystal` are four different POLYGONS handed to ONE builder."*
`Fracture` is a recursive walk, `Upheaval` tips plates out of a dish, `Cinder` is a field of
separate quadrilaterals and `Filament` is a web between caller-supplied points.

⚠️⚠️ **AND THEY COST ALMOST NO AREA, WHICH IS THE ONLY REASON THEY COULD BE ADDED AT ALL.**
`docs/VISION.md` § 2 rule 3 spends the budget on detail rather than area, and Sean's corridor is
the worst offender ever measured in this game at **27.2 per cent of the box**. Every one of these
is strokes or pieces: the cracks are hairlines, the cinders cover about **9 per cent** of the ring
they are scattered in, and the arcs are 3.5 cm bars. Between them the four add roughly **3 m²
across the whole roster**.

⚠️ **THE TWO THAT DRAW OUTSIDE THEIR HAZARD CARRY THE SAME BOUND.** `HeroAbility.TelegraphRadius`
exists because *"a telegraph that lies is worse than no telegraph"*, so Cheska's cracks and Sean's
cinders are unmistakably decoration: no fill, no rim, no glow, a third of the alpha of the thing
they came off, and reaching only 1.28 to 1.35 times its radius. A player reads the slab as the ice
and the cracks as what the ice did to the road.

⚠️⚠️ **ZACK'S IS THE ONE THAT COULD BECOME A CHEAT AND THE BOUND IS WRITTEN INTO IT.** An arc that
reached a body through a barricade would tell a player where somebody is hiding, which is
information this game does not otherwise give: **an aimbot drawn in lightning**. It reaches 3.2 m,
takes at most the nearest three ends, places no `HazardVolume`, staggers nobody, and draws two
stubs going nowhere when it finds nothing, because *"the charge is live and found no route"* is a
real answer and inventing a target is not.

⚠️ **`SpawnUpheaval` IS A DECAL WITH HEIGHT AND MUST NOT BECOME COLLISION.** `MapGeometryCheck`
refuses geometry that floats or buries, the bots path around `HazardVolume` radii and nothing
else, and a hole a player can stand in is a hole they will get stuck in. The slabs lean OUT from
the rim so they never occupy the middle a player walks through.

**Where.** `Assets/TumbangPreso/Runtime/Visual/VfxShapes.cs` (`Fracture`, `Upheaval`, `Cinder`,
`Filament`, `Hollow`, `TwoSided`), `Assets/TumbangPreso/Runtime/Abilities/HeroHazards.cs`
(`SpawnFrostCracks`, `SpawnUpheaval`, `SpawnCinderFringe`, `SpawnCircuitArcs`).

---

## 30 · Two findings from measuring the cue files, and one stale line in `CLAUDE.md`

**`tools/audit_cue_audio.py` was written on 2026-08-26 to answer the third part of 🧑's audio
ask:** *"Check no cue is broken. AudioCueCheck passes but only proves a file exists, is a real
WAV, and has a call site."* It opens every cue and reports peak, rms, DC offset and the loop
seam. **80 files, and it found things nothing else could.**

⚠️⚠️ **NINE SHIPPED CUES CARRY A DC OFFSET AND THEY ARE ALL THE UI AND ANNOUNCER SET.**
`countdown_tick` reads **-0.139**, `ui_click` -0.121, `score_award` -0.125, and `boot_sting`,
`countdown_go`, `match_win`, `round_win`, `ui_back` and `ui_hover` are all between -0.04 and -0.11.

A non-zero mean is not a subtlety: it is a step at the start and the end of every play, it eats
headroom that the mix then cannot use, and on a cue that fires as often as `ui_hover` it is a low
thump under the whole menu. **None of the 2026-08-26 cues has one** (every new file measures under
0.01), so whatever produced these is not `generate_ability_audio.py`.

**Done looks like:** find what generated the nine, subtract the mean, regenerate, and confirm the
peak did not move. ⚠️ **Do not simply high-pass them.** These are short clicks and a filter with
any real slope will ring on a 30 ms sample; the offset is a constant and subtracting it is exact.

⚠️ **THE SEAM COLUMN IS ONLY MEANINGFUL FOR A CUE SOMETHING LOOPS**, and today that is
`sfx_lrt_rumble` alone. It is reported for every file because the point is to have the number when
somebody decides to loop one: § 22.7 records `sfx_lrt_pass` being looped and dropping the train to
silence at 2.70 s because it was authored with a fade at both ends. `sfx_stun_break` reads **0.85**
and `sfx_sky_seance` **0.71**; both are one-shots and both would do exactly that.

⚠️⚠️ **AND `CLAUDE.md` § 7 IS WRONG ABOUT THE BUILD PATH ON THIS MACHINE.** It says
*"`GameBuilder.BuildWindows` targets `C:\\Users\\matth\\Desktop`"*. `GameBuilder` actually calls
`Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)`, which resolves per
machine, and this profile is `C:\\Users\\Matthew`. **The code is right and the note is a hard-coded
path from the other laptop.** It matters because the § 2.2 build procedure tells you to check a
folder that does not exist here, which reads as a build that never ran.

---

## 31 · Everything the 4.72 playtest reported, and the two faults it exposed

**🧑 played the 4.72 player on 2026-08-27 and sent nine items with two screenshots.** They are
listed here in his order, with what was actually wrong under each, because five of the nine had a
cause that was not what the symptom looked like.

### 31.1 The bots turned like nothing alive ✅

*"ai movement is stupidly unrealistic, moving and looking back and forth unnaturally, like who
does that, ppl have to flick their mouse to move, they can look straight behind them and turn in
0.1 seconds"*.

⚠️⚠️ **TWO SEPARATE FAULTS THAT COMPOUNDED, AND NEITHER IS IN THE AI'S DECISIONS.**

1. **A bot's body had no turn rate at all.** `CharacterMotor.Steer` ran
   `transform.rotation = Quaternion.LookRotation(wish)` for every movement-aimed unit. That is an
   instant snap: a full reversal completed in one 60th of a second. `AiTuning.BodyTurnDegPerSecond`
   is **520**, derived against a human's own flick through `CameraRig.StepLook` (about 0.3 s for
   180 degrees on the shipped sensitivity), which puts a bot's half turn at **0.35 s**.
2. **The heading itself flipped frame to frame.** `EightWay` snaps onto eight compass directions
   and the planner reruns every frame, so a wanted direction near an octant boundary alternated
   between two neighbours indefinitely. `AIController.CommitHeading` holds a chosen heading for
   `AiTuning.HeadingCommitSeconds` **0.18 s** unless the wanted heading swings more than
   `HeadingBreakDeg` **90 degrees**, which a 45-degree octant boundary cannot reach.

⚠️ **THE COMMIT IS NOT A SMOOTHING FILTER AND MUST NOT BECOME ONE.** Averaging two octants gives a
heading no keyboard can press, which breaks `CLAUDE.md` § 4's *"a bot presses the same buttons a
human does"*. It commits to one of the eight and refuses to change; that is what a player does.

⚠️ **AND THE TURN CAP APPLIES TO EVERY MOVEMENT-AIMED UNIT, NOT ONLY TO BOTS**, for the same
reason. A gamepad human steers through the same branch.

⚠️ **A FIELD NAME COLLIDED AND THE COMPILER CAUGHT IT, BUT ONLY JUST.** `AIController` already had
`_commitLeft`, the PLAN commit clock driven by `_self.Hesitation`. The new one is
`_headingCommitLeft`. The first draft also decremented the old field a second time, in a different
method from the one that owns it, which would have halved every bot's hesitation window silently.

**Not fixed then, and § 33 is where it was:** *"im not sure if they even have proper ai logic for
when to use skills"*. There is per-hero logic and it is in `AIController.StepHeroAbilities`. Half
of what was wrong is § 31.7, the spam. ⚠️ **The other half is § 33.2 and it was worse: three kits
gated their most expensive power on a distance more than twice the radius of the circle they
actually cast**, so a correct cast by the old rule usually landed on nobody.

### 31.2 Phaister's blink fired itself ✅

*"u cant control the E of phaister and it autocasts after some seconds, i want it to cast only
when i let go"*. `AimByHolding(..., maxHoldSeconds: 1.10f)` fired at the ceiling. It is **0.0**
now, and `HeroAbility.CastsOnReleaseOnly` is what that means.

⚠️⚠️ **THIS DOES NOT REOPEN `docs/VISION.md` § 4's "NOTHING MAY REWARD WAITING", WHICH IS WHAT THE
CEILING WAS FOR.** The ceiling was one of four defences and the weakest. The load-bearing one is
that `AimRampSeconds` stops the reach growing at **0.55 s**, so every second of hold past the
first half second pays out nothing at all. She is also not rooted while aiming and the anti-camp
clocks never stop. A hold that buys nothing is not a reward, it is a player standing still.

⚠️ **THE CLAMP HAD TO MOVE WITH IT.** `HeroAbilitySystem.Aim` clamped `HeldSecondsOnCast` to
`MaxAimSeconds`; at 0 that would hand every blink the minimum 2.0 m, which is the same fault
§ HOLDING A HOLD-TO-AIM POWER records against a bot's one-frame tap. It clamps to the ramp now.

### 31.3 The blink was *"js a shadow"* ✅

Both marks were too short-lived to be looked at. `RiftOpen.Life` **0.52 s to 1.30 s**,
`SpawnShadowArrival`'s object **0.75 s to 1.55 s**, `GlyphSettle.Life` **0.40 s to 1.15 s** (its
last glyph started at 0.175 s, so the runes finished at 0.575 s inside a 0.75 s object). Nothing
about the geometry changed: it is a near-white two-sided rip with its own light, and it existed
for about thirty frames.

### 31.4 The eclipse *"feels like it does nothing"*, and it was a one-frame power ✅

⚠️⚠️ **`Curse` RAN ONCE, INSIDE `OnActivate`.** Everything else the ultimate owned (the falling
eclipse, the ground ring, the aura, the weather) then played for five seconds over an arena in
which the power had already completely finished happening. A player who walked into the ring one
frame after the cast walked through a light show. That is why the most expensive ability in the
game at `UltimateCost` **115** read as a screensaver.

**It is a zone now.** `Duration` **5.0 to 7.0 s**, and `OnTick` re-curses whoever is standing
inside `Reach` every `RecurseEvery` **1.85 s**, from a `_centre` remembered at the cast so it
cannot follow her.

⚠️ **THE RE-CHECK IS DELIBERATELY SLOWER THAN THE HOLD IS LONG.** 1.85 s against a 1.60 s hold
leaves a quarter-second window after every break in which a player who has mashed free can run.
A re-check faster than the hold is an inescapable lock, and three people held forever by one
press is a round that ends without being played.

⚠️ **THE REACH IS UNCHANGED AT 5.0 m AND SHOULD STAY THERE.** It grew along the TIME axis, which
is free; growing the footprint as well would spend `VISION.md` § 2's budget twice for one ability.

**And it answers the role question directly:** a zone the taya cannot stand in is a hole opened in
the defence, and the same zone centred on the lata by a DEFENDING Phaister makes the retrieval run
impossible for its duration. One power, opposite uses, chosen by where she stands.

### 31.5 Kuro Unbound could not be seen, and two correct changes collided ✅

*"make nemu' look better and more imposing, he can barely be seen and i think u should change his
shape too during this form"*, with a screenshot.

⚠️⚠️ **`PoseDevourBody` TOOK HIM TO 22 PER CENT OF HIS OWN VALUE AND `Visual.SkyEvent`'s `Seance`
LOOK SIMULTANEOUSLY DARKENED THE WHOLE STREET.** Each was right alone. A dark violet animal at 22
per cent value, under a violet sky, at night, is a hole in the picture. The floor is **0.46** now
and it is set against the weather he brings rather than against the daylight street it was tuned
in. Same class as § 26's own clamp: a system that changes the global light can break an effect
tuned without it.

⚠️⚠️ **AND MAKING HIM VISIBLE IS WHAT REVEALED THAT HIS SIZE HAD NEVER BEEN JUDGED.**
`ability_kuro_unbound_eye_v30.png` is the first frame in the ultimate's life in which he can be
seen at all, and at `DevourScale` **7.0** he filled the view from eye height and hid the arena
behind him, which `VISION.md` § 2 rule 5 forbids outright. **5.6** now. He is still by a wide
margin the largest object any ability puts on the court.

**The other three channels, all measured off renders rather than reasoned:**

* **Emission, which is the half a value floor cannot do.** Body **0.30**, horns **0.30**, both
  ramped by `k` so they unwind on the flight home like every other channel. It survives the
  ambient drop his own weather causes, so the silhouette holds without the skin reading lighter.
* **Proportion 0.80 / 1.18 / 1.32**, up from 0.82 / 1.14 / 1.26.
* **The horns keep their 0.16 length** and now divide `_devourStretch` back out.

⚠️⚠️ **THREE NUMBERS HERE WERE WRONG ON THE FIRST TRY AND EVERY ONE WAS CAUGHT BY A RENDER, NOT BY
READING THE CODE.** Horn length 0.30 was a **6.7 m spike over a 2.8 m maw** (they are children of
a body at `PersonScale` 2.38 times `DevourScale`, so a local length is not metres: the same trap
`DevourLift` records); horn emission 1.35 clipped them to flat near-white cutouts with no facets;
and the body stretch inherited by the horns turned spikes into slabs.

⚠️ **AND THE LIFT NOW CARRIES THE VERTICAL PROPORTION.** `_originAboveFeet * grown` was correct
only while the body and the origin scaled together. Stretching the body to `tall` without the lift
sinks him by `_originAboveFeet * grown * (tall - 1)`, which is his jaw in the tarmac.

**Open, and it is a human judgement:** whether the form now reads as *"better"*. It is legible and
imposing; whether it is handsome is 🧑's call against `ability_kuro_unbound_eye_v31.png`.

### 31.6 Riding Kuro spun the camera, and it was a feedback loop ✅

*"nemu e is uncontrollable, the camera spins very fast and u cant see shit"*.

⚠️⚠️ **A CLOSED LOOP WITH GAIN, WHOSE THREE LEGS WERE EACH REASONABLE.**

1. `CameraRig.ApplyCompanionPossessionView` takes its yaw from the PET, because the eye rides him.
2. `GhostPetCompanion.UpdatePossession` took its movement basis from `Camera.main.forward`.
3. The same method then turned the PET toward that movement direction.

Holding W is stable because all three agree. **Holding A or D is not**: a strafe sits at an angle
to the facing, the pet turns into it, the camera follows, the strafe direction rotates again. It
accelerates and never stops while the key is down.

⚠️ **THE FIX IS TO DELETE LEG 3, NOT TO DAMP IT.** `CameraRig.StepCompanionLook` was *already*
routing the mouse onto that transform; leg 3 overwrote it every frame in `LateUpdate`. The
possession now behaves like every other mouse-aimed body in the game. The basis is read from the
PET rather than from the camera so a future change to the mount cannot reopen it.

⚠️ **AND THE FLY SPEED WAS 12.5 m/s AGAINST A PLAYER'S 4.6.** Half a second of input crossed six
metres of a 14 m box, so it read as uncontrollable even with the spin gone. It is
`Balance.Speed * 1.7` now, named rather than copied.

### 31.7 Ability timers, and a bot that spent its whole kit on frame one ✅

*"make ability timers way longer too for all or just replace them with 1-2 charges and make sure
ai doesnt just spam them all at the start"*, sent twice in the same message.

**Cooldowns scaled by about 1.5**, band 30 to 45 s becomes **46 to 62 s**: Zack 46, Sean 50, Nemu
52, Phaister 52, Dante 62. The ORDER is unchanged and is the argued part
(`Hero_Strike_Balance.md` § 3.1). The charge abilities were already at 1 or 2 per round.

⚠️⚠️ **THE SPAM WAS A SEPARATE DEFECT AND LONGER COOLDOWNS WOULD NOT HAVE FIXED IT.** Every branch
in `StepHeroAbilities` gates its cast on a distance to the correct target, and **all of those
gates are satisfied simultaneously at a round boundary**, because four seats spawn around one lata
inside a 14 m box. At t = 0 a Dante is inside 5.0 m, a Zack inside 8.0 and a Phaister inside 8.5,
so the ultimate and both skills fired on the first live frame for all four seats.

* `AiTuning.AbilityCadenceSeconds` **1.6 s**, one clock for all three slots, which is what makes
  it a cadence rather than a second cooldown: per-slot spacing still allows a whole kit at once.
* `AiTuning.AbilityOpeningDelaySeconds` **2.5 s**, roughly the time to cross the box, so the
  distance gates mean something again once the opening scatter has happened.

⚠️ **AN IN-PROGRESS HOLD IS EXEMPT.** Closing the gate mid-hold would release Phaister's blink
early and pin every bot blink to the minimum range. ⚠️ **And `_aimHeld` keeps a released key in
the table with -1 in it**, so the exemption tests the VALUES, not `Count`; testing `Count` latches
the gate open for the rest of the match after the first blink.

⚠️ **THE CADENCE RESTARTS ON A TOUCH, NOT ON A CONFIRMED CAST**, because this side cannot know
whether the press was answered: `HeroAbilitySystem` buffers for 0.30 s and may refuse it. Spacing
what the bot ASKS for is the honest reading, and it also means a refused press costs the same beat
a successful one does, which is what stops a bot mashing an empty meter.

### 31.8 Twenty-one callouts deleted, and the rule that replaces them ✅

*"maybe lessen the words//text that pop out too when shit happens?"*. Twenty-one of the
fifty-five callouts in the game were a hero announcing their own press. In Hero Strike four seats
cast 44 to 56 times in a 90 s round (§ 19), so **the most frequent thing on screen was the game
reading its own keypresses back to the player**.

⚠️⚠️ **THE RULE IS WRITTEN INTO `ComicPopup`'s CLASS NOTE SO A NEW HERO CANNOT REINTRODUCE THEM:
A CAST GETS NO WORD.** The test is who learns something, and a caster learns nothing: they pressed
the key, the deck tile greyed out, the viewmodel played and the effect is under their own feet.
Four confirmations before the word. What earns a callout is a state change the reader did NOT
choose (TAGGED!, HEXED!, CURSED!, PHASE BROKEN!, FREEZE!) or something that moved the score, and
those are all kept. `MaxLive` **3 to 2**.

⚠️ **THE ULTIMATES LOST THEIR SHOUT ON PURPOSE**, which looks like the opposite of § 31.9 and is
not: the word lasted 1.25 s and said what the player already knew. What replaced it is the sky
held for seconds rather than 2.2, a 2.2 s column, and a sound tail that outlives the blast.

### 31.9 The ultimates had no weight, and most of it was one arithmetic expression ✅

*"i want all ults to feel like they hit harder ... the change in weather lasts liek 2 seconds,, u
dont even notice it"*, and *"make the changes in lighting and color and the sfx to continue
playing for some time after too"*.

⚠️⚠️ **`Mathf.Max(2.2f, Kit.Ultimate.Duration)` WAS THE WHOLE PROBLEM. FOUR OF THE SIX ULTIMATES
CARRY A `Duration` OF 0** because they are instantaneous blasts, so that expression resolved to
the 2.2 s floor for most of the roster. With both ramps the entire weather event was **2.65 s**,
of which about one second was the sky actually being the new colour. `SkyEvent.SecondsFor` is
`Max(MinimumSeconds 7.0, duration + FallSeconds)`.

⚠️⚠️ **THE AFTERMATH LIVES IN THE FALL, NOT IN THE HOLD, AND THAT IS WHY THE FALL IS NOW THE
LONGEST PHASE.** `FallSeconds` **1.10 to 3.20 s**. `k` is already decaying through it, so every
frame of the fall is closer to the untouched street than the one before: a long fall is a long
return to normal, not a long period of being unable to see. Extending the HOLD would have spent
rule 5's budget for the whole duration.

⚠️⚠️ **AND THE ECLIPSE WAS CUTTING ITS OWN SKY SHORT EVERY SINGLE CAST.**
`SpawnGrandCovenEclipse` still called `SkyEvent.Play` itself, left behind when § 26 moved the sky
to the one point every ultimate passes through. Both calls asked for the same LOOK, so it read as
a redundancy; the **wind-up** is what made it a bug. `HeroAbility` roots the caster for 0.4 s and
runs `OnActivate` at the END of it, so the presentation started the sky for its full length and
0.4 s later this line restarted the same weather at the ability's raw duration. `SkyEvent.Begin`
zeroes `_elapsed` on every call. Deleted; `SkyEvent.Play` now has one runtime caller.

**The rest of the weight, none of which costs any luminance** (§ 8b measured Thunderstrike blowing
62.8 per cent of a frame to white, and the probe fails a run over 12):

| | was | is |
|---|---|---|
| `UltimateColumn.Life` | 0.9 s | **2.2 s** |
| `ImpactPunch` strength | 0.9 | **1.45** |
| `PulseChromatic` | 0.75 for 0.32 s | **0.95 for 0.85 s** |
| A second, longer rumble | none | **0.30 for 1.10 s** |
| `Hitstop` (local caster only) | 0.045 / 0.12 | **0.075 / 0.20** |

⚠️ **THE SIX WEATHER CUES WERE AUTHORED AT 2.6 to 2.9 s AGAINST THE 2.65 s EVENT.** They are
**7.4 to 8.4 s** now. `_rumble_tail` grew a `decay` parameter for it; every other caller keeps
0.9 by omitting it. `synth_sky_whiteout` has no rumble bed by design (it is air, not mass) so its
own two decays carry the tail instead, and `synth_sky_seance` needed a 0.22 s release because its
envelope peaks AT `duration` by construction: at 2.7 s that was a deliberate stab, at 7.6 s it is
a seven-second crescendo terminated by a click.

⚠️ **THE SEEDING HELD AND IT WAS CHECKED.** Exactly 12 files moved, which is the six cues in the
two output directories. `tools/audit_cue_audio.py` reports all six clean on DC and seam.

**Open: the per-hero creative revamp.** 🧑 also asked to *"give everyone more creative ults that
have an impact that effects or is usable either in attacker/defender roles or both roles"*.
Phaister's is done (§ 31.4) and every ultimate got the presentation above, but the other five
kits keep their existing effects. **This is deliberately not done blind:** Dante already leaves
earth pillars for 5.0 s and Nemu's maw runs 5.0 s, so two of the five already have the lasting
half; Cheska's nova and Sean's supernova are the two that genuinely resolve in a frame. Any
redesign is an unmeasured balance change of exactly the kind § 0 says wants a played build first.

### 31.10 "kulam" is gone ✅

*"kulam sounds so shit too replace it with hex, in everything"*. The ability is named **HEX**, and
the four identifiers that carried the old word are now `SpawnHexSigil`, `HexSigilComponent`,
`HexSigilAbility` and the `HexWardZone` object. Its callout was deleted outright by § 31.8. The
cue files were already `sfx_hex_cast` and `sfx_hex_afflict`, and `StunElement.Hex` already
existed, so nothing on the audio or stun side had to move. **`Id` is untouched at
`phaister_skill1`**: ids are keys, and § 28's note on `nemu_ultimate` records why renaming one to
match a label is how a rename becomes six silent lookup failures.

### 31.11 What this session measured that nobody had asked for

⚠️⚠️ **`tools/audit_ability_authority.py` PUTS A NUMBER ON § 25.1.** It is the sibling of
`audit_audio_reach.py` and asks the other half of the same question: that one asks who HEARS an
event, this asks who DECIDES it. Over the ability tree it reports **39 effect call sites, 1
host-gated, 23 ungated on another body, 15 ungated on the caster.**

The one gated call is Phaister's `Curse`. **So twenty-three places in the ability layer move a
body the caster does not own, with no host gate anywhere in the path**, which is a sharper
statement of § 25.1 than its own prose and a ready-made worklist. HOST-ONLY is the CORRECT state
for anything in that column; a blank there is the defect.

---

## 32 · The networking was broken by one unreplicated static, and four other faults on top of it

**🧑 2026-08-27, after a LAN playtest:** *"can u refine netwrok of this game? both lan and online
server? its heavily broken with other ppl not seeing the character"*, *"its so bad that in heroes
gamemode, ffrequently they see the older version of the skin"*, *"apparently only host sees the
skin of other players"*, and *"theres a lot of shit u dont see that ur supposed"*.

### 32.1 ⚠️⚠️ THE GAME MODE WAS NEVER REPLICATED AT ALL ✅

**`UI.SceneFlow.SelectedMode` is a plain static**, set by whoever last touched the toggle in
`ConvertedMatchSetup`. The map is replicated (`SyncMap`), the difficulty is replicated
(`SyncDiff`), the picks are replicated, the seats are replicated. **The mode is not, and it
decides more than any of them.**

A client whose own menu last said Classic, joining a Hero Strike match, **builds a different
game**. `MatchInstaller` reads `SelectedMode` in three load-bearing places:

* `_book.PersonArt(motor.CharacterIndex, SceneFlow.SelectedMode)` resolves the model against
  `Roster.GetPeople(mode)`, which is **twelve street characters in Classic and five heroes in
  Hero Strike**. A hero index looked up in the street cast is a different person, and past the
  end of the list `RosterBook.Resolve` falls back to `art[0]`, so several seats collapse onto the
  same wrong body. **That is "they see the older version of the skin" exactly: the Classic roster
  IS the older one.**
* `if (SceneFlow.SelectedMode == GameMode.HeroStrike)` gates installing `HeroAbilitySystem` at
  all, so a client in the wrong mode gives all four seats no kit.
* `CharacterMotor.Mode` feeds `Roster.GetPeople(Mode)` behind the nameplate, so the labels
  disagree with the bodies.

**Fixed** with `SelectMode` / `SyncMode`, on the same client-asks-host-decides-host-tells-everyone
shape as `SelectMap`. ⚠️ **It is sent BEFORE `StartMatch` and before a late joiner's snapshot**,
because `OnStartMatchMsg` calls `SceneFlow.StartMatch()` which builds every seat: a mode that
arrives one message later arrives after the bodies exist. `ConvertedMatchSetup.OnModeCycle` now
pushes it when the host cycles the toggle, which it never did while the map and difficulty cycles
both sent theirs.

### 32.2 `SyncPicksClientRpc` had three more faults, all invisible on the host ✅

The host never runs the client half of this method, which is why every one of these reads as
*"only host sees the skin of other players"*.

1. **It resolved art against the wrong roster.** It called `book.PersonArt(charIndex)`, the
   overload with no mode, which always uses `Roster.People`. `MatchInstaller` has always used the
   mode-aware overload. Same fault as § 32.1 and it needed fixing in both places.
2. **It only applied the model when the index CHANGED**
   (`if (charIndex >= 0 && who.CharacterIndex != charIndex)`). A seat that already carried the
   right NUMBER never got the right ART, which is the common case on a joining client: the seats
   are built from the lobby table and then this sync arrives agreeing with it, so the one message
   whose whole job is to fix the model decided there was nothing to do. **Applying art is
   idempotent; skipping it is not.**
3. **It dropped the pet.** `MatchInstaller` passes `art.PetModel` as a sixth argument; this passed
   five. Every client rebuilt Nemu without Kuro, and her entire kit is him (§ 28), so on a client
   she had three powers pointing at an object that did not exist.

### 32.3 The settings sliders could barely be dragged, and it is the same bug as the scrolling ✅

*"sound and volume dont decrease theyre hard locked"* and *"awkward scrolling motion for settings
(repeated problem)"* are **one fault**. `TscnUiImporter.BuildSlider` gave the Slider's root
GameObject **no Graphic at all**. Unity's EventSystem raycasts to find a graphic and then walks UP
the hierarchy for a drag handler, so the only parts of a slider that could start a drag were the
14 px `Background` strip and the 22 by 34 px `Handle`. Every other pixel of the row hit nothing,
the event carried on up to the `ScrollRect` those rows live in, **and the list scrolled instead**.

Fixed with a full-rect transparent raycast target on the root, added before the children so it
sits behind them and cannot swallow the handle's own hit test.

⚠️ **§ 15.8 fixed the scrolling twice already and this is why it kept coming back**: both previous
passes treated it as a scrolling problem (a missing scrollbar, a wheel step of 45) and it was
never only that.

### 32.4 A slipper cannot exceed 34 m/s any more, and this is a GUARD not a cure ⚠️ OPEN

*"appparently slippers randomly fly to sky too? idk how playtesters did that"*. **The source is
not identified.** `Slipper.StepFlight` now clamps speed while preserving direction, so one bad
frame cannot remove a slipper from the match, but the cause is still out there.

**Three places can manufacture a large velocity and none is obviously wrong alone:**

* `Deflect` off the lata passes `-_velocity.normalized * LataRecoilScale * _velocity.magnitude`,
  so two recoils in quick succession compound.
* `BounceOffObstacles` falls back to `normal = -disp.normalized`, which is **zero if the slipper
  did not move that frame**, and `Vector3.Reflect` about a zero normal returns the velocity
  unchanged rather than reversing it. The slipper is then also teleported to `closest.point`,
  still travelling into the surface.
* `HeroHazards.SeanceVoidComponent` teleports loose slippers every frame during Nemu's ultimate,
  which can drive one into a collider that ejects it.

**If the clamp ever fires in normal play the number is wrong. If the sky-launch stops being
reported, the cause is still there.**

### 32.5 What is still NOT replicated, and it is the biggest thing left ✅ CLOSED 2026-08-27

⚠️⚠️ **§ 25.1 IS STILL OPEN AND IS NOW THE LARGEST KNOWN NETWORK GAP.** The ability layer has no
RPC of any kind: `tools/audit_ability_authority.py` reports **39 effect call sites, 1 host-gated,
23 ungated on another body**. A remote player's ultimate produces no VFX, no sound, no column and
no sky on anybody else's screen, and twenty-three places move a body the caster does not own.
Everything in this entry was upstream of that; none of it fixes it.

✅ **The ability layer is replicated as of 2026-08-27.** § 25.1 has the shape and § 38 has the rest
of the pass, including the loopback fault behind four more of these.

---

## 33 · The bots picked a target by seat number, aimed powers at rings they do not cast, and had no keyboard between decisions

**🧑 2026-08-27, on the 4.72 build:** *"they dont move like humans and just spam skills and always
just target the human, they also dont seem smart with using their skills, the game feels
overstimulating bcz all bots spam their skills at the start"*, and, asked what else was worth
doing: *"find other shit to improve on ai to make it better and smarter and more human like"*.

§ 31.1 and § 31.7 answered two of those clauses the same day (the turn rate and the heading
commit; the cadence and the opening delay). **This entry is the other three, and each one turned
out to be a different fault with a different cause.**

### 33.1 ⚠️⚠️ "ALWAYS JUST TARGET THE HUMAN" WAS SEAT ORDER, AND NOTHING EVER READ A HUMAN ✅

`AIController.TagTarget` was a `foreach` over `RoundDirector.Players` with a `return` in it: **the
first taggable attacker in seat order**. Seat order is a fixed list, so it is a fixed priority.
Whichever seat sits lowest in it was chased in every round it was not the taya, by every taya, for
the whole match. A person who sits in one seat all night is therefore chased all night.

⚠️⚠️ **THAT IS WHY NOBODY FOUND IT BY LOOKING FOR A HUMAN CHECK.** There is no human check
anywhere in the AI and there never was. `IsHumanSlot` exists and this path does not call it. The
selector had no idea a person was involved; it was singling somebody out by construction.

⚠️⚠️ **AND IT DECIDED MORE THAN THE CHASE.** `StepHeroAbilities` asks the same function for a
DEFENDING hero's target, so the same seat also ate every skill and every ultimate a defending hero
spent. One line produced both halves of the report.

**It is a score now**, on the shape `LiveThreat` has used for the neighbouring GUARD decision since
the port. `AiTuning` carries the four weights and `TagTargetWeightTests` carries the relationships
between them:

| term | value | what it is for |
|---|---|---|
| `TagHelplessBonus` | **2.5** | already stunned or tripped: the only certain tag on the board |
| `TagDepthWeight` | **0.22**/m | how far inside the chalk they are, which `IsTaggable` cannot say |
| `TagDistanceWeight` | **0.08**/m | the same weight `LiveThreat` uses, so guard and chase agree |
| `TagSwitchMargin` | **0.75** | what a rival must beat to take the chase off whoever has it |

⚠️⚠️ **THE COMMIT TERM POINTS THE OPPOSITE WAY TO `LiveThreat`'S AND BOTH ARE RIGHT.** Guarding is
a standing post, so its anti-fixation term is a PENALTY on whoever was guarded last and an equal
rival pulls the taya round. Chasing is a pursuit, and a pursuit that changes target on a tie is a
taya running down the middle of two attackers and catching neither. So this one is a BONUS on
whoever is already being chased. Without it the score would be worse than seat order, not better:
every term moves continuously while both bodies run.

⚠️ **AND IT READS `At(who)`, THE OBSERVED POSITION.** A selector on the true position picks targets
off information the body it steers has not been given yet.

### 33.2 ⚠️⚠️ THREE KITS AIMED THEIR MOST EXPENSIVE POWER AT A CIRCLE THEY DO NOT CAST ✅

*"they also dont seem smart with using their skills"*. Every branch in `StepHeroAbilities` gated
its cast on **one distance to one target**, and a distance is not a question about what the cast
would achieve. Asking the question properly is what exposed that the hand-picked distances were
mostly wrong, and wrong in the direction that misses:

| | telegraph | old gate | what that meant |
|---|---|---|---|
| Zack, Thunderstrike | 4.5 m **on Zack** | target within **8.0 m** | lightning on empty road, target watching from outside it |
| Dante, Seismic Stomp | 2.2 m on his feet | target within **5.0 m** | more than twice the ring, so usually nobody |
| Dante, Titan Fissure | 4.5 m, **2.2 m ahead** | target within **9.0 m**, any direction | half of every cast opened the ground behind his back |
| Nemu, Seance Void | 2.8 m, 3.5 m ahead | hand-written **7.5 m at 4.5 m** | the PRE-§ 8 numbers, nearly three times the area |
| Cheska, Permafrost | 2.3 m, **2.8 m ahead** | *"drop it while fleeing"* | frost laid across her own escape, never behind her |

⚠️⚠️ **THE GATE IS THE ABILITY'S OWN TELEGRAPH NOW, NOT A NEW TABLE OF NUMBERS.**
`HeroAbility.TelegraphRadius` and `TelegraphRange` already say where a power lands and how wide it
is, they are already asserted against what `OnActivate` actually spawns
(`TelegraphsMatchWhatTheAbilityActuallyPlaces`), and they are already the ring the PLAYER is shown.
A bot aiming at the ring the player sees needs no second set of numbers, **and a new hero cannot
ship with a wrong one here because it no longer has one here to get wrong.**

Four questions replace the distance, in `AIController` § WOULD THIS CAST DO ANYTHING:

* **`WouldCatch`** projects the footprint off the telegraph and asks whether anybody is under it,
  with `AbilityVictimMargin` **0.9 m** of lead. ⚠️ That is HALF the 1.84 m a body crosses during
  `UltimateWindup` 0.4 s, on purpose: a margin at the full crossing is a distance gate again.
* **`stunPayload`** is `CLAUDE.md` § 4's *"stuns overlap via Max(), never additively"* read from
  the other side. A freeze laid on somebody already frozen extends nothing, so for a stun power a
  helpless body in the circle is **not** a victim. A power that shoves or launches counts
  everybody, because moving a stunned body is one of the better things you can do with one.
* **`WorthDenying`** refuses to lay ground denial on ground already denied
  (`AbilityDenialOverlap` **0.6** of a radius) and requires the footprint to be somewhere the game
  FORCES a body to walk: onto somebody, onto a loose tsinelas, or onto the lata. ⚠️⚠️ The
  duplicate case costs twice: a second frost sheet on the first denies nothing new, spends a 46 to
  62 s cooldown, and stacks two translucent plates in one place, which is `VISION.md` § 2 rule 4
  and the § 19 sorting bug in one press.
* **`SlotIsSpendable`** refuses to recast a power that is still running. `IsReady` only answers the
  cooldown or the charge count, so a charge ability with a duration could be spent on top of
  itself. ⚠️ The one exception is real: Nemu's poltergeist is the game's only `CanReactivate`
  power and its second press is *"press again to follow him there"*.

⚠️ **AND `targetDistance` READ THE TRUE POSITION UNTIL NOW.** It was the one place in the ability
layer that saw through `Observe`'s reaction lag, so a bot answered a rival's step on the frame it
happened while every other decision about the same body was `Me.React` behind. That is a power
cast faster than a hand can move, and it reads as cheating rather than as skill.

### 33.3 ⚠️ THE BOTS HAD NO KEYBOARD BETWEEN DECISIONS ✅

*"they dont move like humans"*. With § 31.1's flicking gone, what was left was a machine walking
perfect lines at one speed and never once looking up. Three things a person does, all of them
expressed as keys because `CLAUDE.md` § 4 allows nothing else:

* **The key change beat.** `KeyChangeBeatSeconds` **0.12 s** with **no movement key at all**, when
  a committed heading is replaced by one more than `KeyChangeBeatDeg` **100°** away. ⚠️⚠️ A
  keyboard cannot go from W to S without passing through nothing, and the bots had no gap: a
  reversal was a frame in which the movement vector became its own negative at unchanged speed.
  Capping the TURN made the body honest about which way it faced; it did not put the pause back.
  ⚠️ 100° is above `HeadingBreakDeg` 90 so the octant flapping the commit window exists to absorb
  can never charge one, and ⚠️ `_driving` goes back off during it or `StepUnstick` would read a
  deliberate pause as being stuck.
* **Sprint in bursts.** `SprintBurstMin/Max` **0.70 to 1.15 s** down, `SprintRestMin/Max`
  **0.35 to 0.65 s** up, after `SprintCommitDelay` **0.15 s** of walking the new heading first.
  ⚠️⚠️ The sprint was a STATE: `MaySprint` asks the stamina bar, so a bot held the key until the
  bar bottomed out, limped through two seconds of fatigue at 0.75 speed, and did it again. **The
  burst ceiling is under the 1.31 s of usable bar**, so it cannot empty it alone, and the whole
  rest range sits under the whole burst range so a crossing is still mostly running whichever way
  the dice fall. ⚠️ It probably makes bots FASTER over an arena crossing, because the fatigue
  lockout it used to walk into cost more than a rest does.
* **Glancing.** During a loiter rest, `GlanceChance` **0.45** to press toward the lata, the taya or
  the nearest tsinelas for `GlanceSeconds` **0.09 s**. ⚠️⚠️ A movement-aimed body can only look by
  walking: `CharacterMotor.Steer` does nothing on a frame with no key down, so a standing bot is
  frozen facing wherever its last step pointed. A human on a gamepad has the identical constraint
  and answers it the identical way. ⚠️ **The leash sizes the press, not the neck.** 0.18 s would
  walk 0.83 m against `LoiterLeash` 0.45 and spend the rest of the beat walking home, which is the
  pacing that leash was added to delete; 0.09 s travels 0.41 m and lands inside `LoiterStepMin`
  0.07 to `LoiterStepMax` 0.13, so it is the shipped loiter step aimed at something worth watching.

### 33.4 ⚠️⚠️ THE TAYA KEPT COMMITTING TO A CAMP IT COULD NOT WALK TO ✅

Found while reading the file for § 33.2, not reported. `HasCoverPoint` answered **"any loose
slipper exists anywhere"**, and `PlanDefender` chose `AiPlan.Cover` off it.

⚠️⚠️ **THAT IS THE FAULT `TryInterceptPoint`'S OWN HEADER RECORDS, ONE SCREEN UP IN THE SAME
FILE.** It reads *"SOMETHING IS IN FLIGHT IS NOT AN INTERCEPT"*: a plan chosen off a condition far
weaker than the plan's own requirements, so the taya committed to a verb it could not execute.
`TryCoverPoint` additionally requires the slipper to be **inside the box**, which the taya cannot
leave, and a claimant with free hands who can act. Neither was tested, so one tsinelas on the
pavement outside the chalk with everybody carrying put every camping taya into Cover, whereupon
`DoCover` fell straight through to `DoGuard`.

⚠️ **THE COST WAS NOT THE FALLTHROUGH, IT WAS THE FLIPPING.** A plan change costs
`_self.Hesitation` and `StepPlan` clears the goal with it, so Cover and Guard traded places every
time a slipper's state changed. It also made `Plan` LIE, which reaches past the taya:
`AiDiagnosticProbe` prints it and four of the skill gates in `StepHeroAbilities` branch on it.

**It is `TryCoverPoint(out _)` now**, exactly as `HasInterceptPoint` is `TryInterceptPoint(out _)`.
⚠️ The unused `lata` parameter is kept to match that sibling; `PlanDefender` has already
null-checked the can before it asks either of them.

### 33.5 ⚠️⚠️ THE SHOVE NEVER ASKED WHICH WAY IT WOULD PUSH THEM ✅

Also found by reading rather than reported. `SabotageTarget`'s header has said *"a rival worth
shoving into the taya's reach"* since the port, and the code picked **the nearest rival in any
direction** with the taya used only as a null check.

⚠️⚠️ **`CombatVerbs.HostResolveShove` PUSHES ALONG `victim - shover`.** So which rival is worth a
shove depends entirely on where the shover is standing relative to the taya, and that was the one
thing not being computed. **Half of every sabotage was shoving somebody to SAFETY**, which is
worse than not casting it at all: it costs a cooldown, 25 stamina, and it helps a rival.

**It is scored now**: the push direction is dotted against the direction from the victim to the
taya, anything that does not close on the taya is refused outright, and a rival with a tsinelas in
hand is worth a point on top, because being taggable needs a slipper AND a body inside the chalk,
so shoving an empty-handed rival at the taya sets up nothing.

⚠️ **THE BAR IS "NOT COUNTERPRODUCTIVE", NOT A TIGHT CONE, AND THAT IS THE WHOLE TRADE.** `Spacing`
is deliberately pushing the three attackers apart, so the opportunities are already rare: this
function's own note records the willingness dial measuring **zero sabotages over a whole match**
before the reach was scaled by it. A cone here would take it straight back to zero, and a dial
that changes nothing is the defect that note exists about.

### 33.6 What the probe measured, and the honest reading of it

`BotBehaviourProbe`, **one seeded run an arm**, both maps, whole matches (Hero Strike 50,060
frames / 834.3 s simulated, `match in progress at exit: False` on every arm). Three builds:
`d8e0da6`, then § 33 alone, then § 33 with § 34.

**Hero Strike on Eskinita**

| | `d8e0da6` | § 33 | § 33 + § 34 |
|---|---|---|---|
| throws | 131 | 127 | **173** |
| retrievals | 130 | 123 | **170** |
| tags | 75 | 77 | **102** |
| lata knocks | 76 | 70 | **91** |
| lata restores | 89 | 85 | **108** |
| unretrieved-slipper penalties | 102 | 113 | **0** |
| skill uses | 38 | 19 | **34** |
| ultimate uses | 26 | 28 | **37** |
| seat travel, m | 565 / 1241 / 1222 / 1349 | 530 / 1133 / 1175 / 1153 | **1337 / 1372 / 1384 / 1317** |

**Hero Strike on Ilalim ng Tulay**

| | `d8e0da6` | § 33 | § 33 + § 34 |
|---|---|---|---|
| throws | 129 | 114 | **164** |
| retrievals | 126 | 110 | **161** |
| tags | 54 | 63 | **130** |
| lata knocks | 75 | 66 | **83** |
| unretrieved-slipper penalties | 161 | 278 | **97** |
| skill uses | 31 | 21 | **30** |
| ultimate uses | 24 | 25 | **39** |
| seat travel, m | 593 / 1109 / 1288 / 1388 | 585 / 999 / 1044 / 1026 | **1248 / 1231 / 1263 / 1260** |

**Classic on Eskinita**, § 33 against § 33 + § 34 (no `d8e0da6` Classic report survives to compare):
throws **55 to 72**, retrievals **54 to 71**, tags **43 to 62**, knocks **31 to 37**, restores
**37 to 44**, seat travel **224 / 523 / 556 / 498** to **549 / 546 / 578 / 540**.

⚠⚠ **THE MIDDLE COLUMN IS THE ONE TO READ CAREFULLY AND IT IS THE LEAST IMPRESSIVE.** § 33 on
its own moved every headline figure by less than § 16's noise floor, in both directions, which is
what it should have done: none of it was meant to make bots busier. **The one number it moved past
the floor is skill uses, 38 to 19 and 31 to 21**, and that is the whole intent of § 33.2. Roughly
half of what those kits were casting was landing on nobody.

✅ **THE THIRD COLUMN IS § 34 AND THE TRAVEL ROW IS NOT A NOISE QUESTION.** Seat 0 goes from 530
to 1337 m on Eskinita and 585 to 1248 on Ilalim, and on all three arms the four seats close to
inside 5 per cent of one another, which is what four bots running one brain with four personality
rolls is supposed to look like.

⚠⚠ **AND THE UNRETRIEVED-SLIPPER CLOCK IS THE CLEAREST SINGLE RESULT IN THIS FILE.** It goes to
**ZERO** on Eskinita, from 102 and 113, across a whole eight-round match, and **278 to 97** on
Ilalim. That penalty is charged once a second for as long as an attacker is short of its tsinelas,
so it is a DURATION: with every seat able to walk where it decided to walk, nobody was still
short of theirs when the fine started. Everything else follows from it. Throws and retrievals up
about a third on both maps, tags up 36 per cent on Eskinita and **141 per cent on Ilalim**.

⚠️ **THE SKILL FIGURE IS THE ONE WORTH NOT MISREADING.** 34 against 38, and 30 against 31, is not
*"the gating did nothing"*: those casts come from four seats playing a third more game than the
originals were, and each one had to pass a footprint test the originals never faced. The rate per
unit of play is down and what a cast lands on is what went up. Ultimates rose anyway, 26 to 37 and
24 to 39, which also answers the worry logged in § 33.8 about directional powers going uncast.

⚠️ **ONE RUN AN ARM, SO § 16 SAYS TREAT THE SIZES AS INDICATIVE.** Its table puts a single run at
about 20 per cent error on the mean and 40 per cent as the smallest resolvable effect. The throws,
tags and penalty movements are at or past that boundary on both maps, which is as much as one run
an arm can buy; the travel row needs no statistics at all, because it is one seat matching its
siblings instead of halving them, on every map, in both modes.

### 33.7 Two faults caught while writing this, both by a test rather than by a run

⚠️ **`SprintRestMax` WAS 0.80 AGAINST A `SprintBurstMin` OF 0.70.** The average was fine and an
unlucky pair of rolls gave a bot that walked more of a journey than it ran.
`ACrossingIsStillMostlyRunning` asserts the whole ranges rather than the means, and it went red on
the first run. 0.65 now.

⚠️⚠️ **THE GLANCE USED `Vector3.zero` AS "NOTHING TO LOOK AT" AND THE LATA STANDS AT THE ORIGIN.**
The arena is centred on the world origin, so the single most likely thing a bot in this game wants
to watch was the one value the sentinel threw away. `TryGlanceAt` is an out parameter now. Same
class as every sentinel bug: the sentinel was a legal value of the thing it was standing in for.

### 33.8 Still open

* ⚠️ **A DIRECTIONAL POWER IS HARDER FOR A BOT TO AIM THAN FOR A HUMAN, AND THAT ASYMMETRY IS NOW
  LOAD-BEARING.** A bot is movement-aimed, so `transform.forward` is wherever it last walked; a
  human Dante aims the fissure with the mouse while walking somewhere else. `WouldCatch` on a
  power with a `TelegraphRange` therefore fires only when the bot happens to be pointing the right
  way. For a taya this is fine, because `DoHunt` drives at the victim. **For an ATTACKER holding a
  ready directional ultimate it could mean never casting it**, which would show up as ultimate uses
  falling in `BotBehaviourProbe` rather than as anything visible in a round.

  ✅ **MEASURED, AND IT IS NOT BITING TODAY.** Ultimate uses went **26 to 37** on Eskinita and
  **24 to 39** on Ilalim, so the strictly tighter gate produced MORE casting rather than less
  (§ 33.6). It is left open rather than closed because the probe seats only Dante among the
  directional kits and one run an arm cannot resolve a small effect. ⚠️ **If it ever does bite,
  the fix is a short aim step (walk at the target for a beat, then cast), NOT a facing written
  directly**, which would be the second movement path § 4 forbids.
* ⚠️⚠️ **§ 34 WAS FOUND BY READING THESE RUNS' OWN PER-SEAT COLUMN**, and it moved the numbers
  in § 33.6 far more than anything in this entry did: one seat in four was being steered in a
  rotated frame in every all-bots run this project has ever measured.
* ⚠️ **THE PROBE ONLY EVER SEATS DANTE AND ZACK.** The cast is seeded, so all four seats come
  up `dante / zack / dante / zack` on both maps and **four of the six kits are exercised by no
  automated run at all**: Cheska, Sean, Nemu and Phaister. Every gate rewritten in § 33.2 for
  those four is reasoned from their telegraphs and unmeasured in play. A probe arm that forces
  the other cast would be worth more than another run of this one.
* ⚠️ **Nothing here was A/B'd to § 16's standard.** § 33.6 has what was measured, one run an
  arm, and how far to trust each row. An A/B on any single weight in this entry still costs three
  runs an arm.

---

## 34 · Seat 0 was steered by a different movement model in every all-bots run, and it is § 11's second layer

**Not reported. Found on 2026-08-27 by reading `seat N travelled` in the § 33 probe reports and
asking why one column was always half the others.**

| | seat 0 | the other three |
|---|---|---|
| Classic, Eskinita | **224.1 m**, score 1925 | 522.7 / 556.1 / 498.4, scores 2475 to 2950 |
| Hero Strike, Eskinita (before § 33) | **564.9 m**, score 3480 | 1241.3 / 1221.9 / 1348.5 |
| Hero Strike, Ilalim ng Tulay | **593.0 m**, score 3060 | 1109.4 / 1287.9 / 1388.2 |
| Hero Strike, Eskinita (after § 33) | **530.0 m**, score 3360 | 1133.4 / 1175.0 / 1153.3 |

**Roughly 45 per cent of the movement of a seat running the same brain, on both maps, in both
modes, before and after the AI changes, and lowest score every time.** The personality roll does
not explain it: seat 0 rolls Tempo 1.159 and Hesitation 0.230 against seat 1's 1.125 and 0.217,
which is nothing like a factor of two.

### 34.1 ⚠️⚠️ A FOLLOWED SEAT IS STEERED BY A DIFFERENT MOVEMENT MODEL, AND SEAT 0 WAS ALWAYS FOLLOWED ✅

`MatchInstaller.BuildCameraAndHud` set `_spectating = GameLaunch.Spectator` and turned the
gameplay rig off only on that. **`HumanSeat` answers -1 for THREE reasons**:
`GameLaunch.Spectator`, `GameLaunch.AllBots`, and the serialised `_allBots`. So in an all-bots run
the rig stayed **active**, kept **following** `seats[Mathf.Max(0, HumanSeat)]`, which that clamp
makes seat 0, and kept **`AimSource.Mouse`** set on it.

⚠️⚠️ **AND THAT CHANGES HOW THE BODY STEERS.** `CharacterMotor.MouseAimed` is
`_rig.IsFollowing(this) && Aim == Mouse`, and the mouse-aimed branch of `Steer` runs
`transform.TransformDirection(wish)` and **returns without rotating the body**. An `AIController`
writes a WORLD-space heading through `EightWay`. So seat 0's heading was re-interpreted as
body-relative and rotated by a yaw that never changed, for the whole match: a bot asking to walk
north walked wherever its shoulders happened to be pointing.

⚠️⚠️ **IT IS THE IDENTICAL FAULT `CharacterMotor.MouseAimed`'S OWN HEADER RECORDS FOR NEMU'S
POSSESSION**, reached from a different direction. That header even spells out the mechanism, word
for word, and the guard it added is specific to the pet, so it could not catch this one. Two
sightings of one bug now: **any body an active mouse rig follows while an `AIController` drives it
is being steered in the wrong frame.**

⚠️ **§ 11 CLOSED THE FIRST LAYER OF THIS AND THIS IS THE SECOND.** `GameLaunch.AllBots` fixed seat
1 getting a `PlayerInputReader` with nobody at the keyboard, and the seat the CAMERA was bolted to
was left behind. `BotBehaviourProbe`'s travel floor is 150 m, which seat 0 cleared every time.

**Fixed** with `bool nobodyIsDriving = HumanSeat < 0`, which covers all three reasons.

✅ **AND THE FIX IS MEASURED, NOT ARGUED.** The same seeded runs, before and after the one line:

| | seat 0 | seat 1 | seat 2 | seat 3 |
|---|---|---|---|---|
| Classic, Eskinita, before | **224.1 m** | 522.7 | 556.1 | 498.4 |
| Classic, Eskinita, after | **548.7 m** | 546.0 | 578.4 | 540.4 |
| Hero Strike, Eskinita, before | **530.0 m** | 1133.4 | 1175.0 | 1153.3 |
| Hero Strike, Eskinita, after | **1336.5 m** | 1371.9 | 1383.8 | 1317.2 |
| Hero Strike, Ilalim, before | **584.5 m** | 999.1 | 1043.8 | 1026.1 |
| Hero Strike, Ilalim, after | **1247.8 m** | 1230.9 | 1263.2 | 1259.8 |

**Seat 0 travels 2.1 to 2.5 times as far and the four seats now sit inside 5 per cent of each
other on every arm**, which is what four bots running one brain with four personality rolls is
supposed to look like.

⚠️⚠️ **AND THE WHOLE MATCH GOT LIVELIER, WELL PAST WHAT A QUARTER OF THE SEATS EXPLAINS ON ITS
OWN.** Classic throws **55 to 72** and tags **43 to 62**; Hero Strike on Eskinita throws **127 to
173** and tags **77 to 102**; on Ilalim throws **114 to 164** and tags **63 to 130**. The sharpest
single figure is the unretrieved-slipper clock, which is a DURATION rather than an event count:
**113 to 0** on Eskinita and **278 to 97** on Ilalim. **A seat that cannot reliably walk where it
decided to walk is not just a quiet seat; it is a seat the other three keep waiting for.** § 33.6
has the full tables.

⚠️ One run an arm, so § 16 says treat the sizes as indicative. The travel rows are the exception
and are not a noise question: they are one seat matching its siblings instead of halving them, on
both maps, in both modes.

⚠️ **THE SPECTATOR CAMERA IS NOW BUILT FOR THE ALL-BOTS CASE TOO, AND THAT IS LOAD-BEARING RATHER
THAN TIDY.** `Diagnostics/FrameCapProbe` measures the ACHIEVED frame rate from the shipped player
under `-tp-botmatch`, and turning the gameplay rig off without putting a camera back would leave
it rendering nothing and hitting any cap it was asked for. § 17 is an open investigation resting
on exactly that number.

### 34.2 What this invalidates, and it is not nothing

⚠️⚠️ **EVERY PER-SEAT FIGURE IN EVERY `Logs/bot-behaviour-*.txt` IN THIS REPOSITORY WAS TAKEN WITH
ONE SEAT IN FOUR STEERING IN A ROTATED FRAME.** Totals are diluted by roughly one eighth of the
match (a quarter of the seats at about half effectiveness), and per-seat comparisons between seat
0 and anybody else are meaningless. That includes the § 16 noise-floor sweep and the § 17 frame
step table.

⚠️ **§ 17'S OPEN QUESTION IS THE ONE MOST WORTH RE-ASKING.** It reports that a 20 per cent change
in decision rate cost five sixths of the throws and all of the casting, and calls that *"far too
steep to be a smooth sensitivity"*. One of the four seats being steered in the wrong frame is a
new candidate for where the steepness comes from, and it was not on that entry's suspect list
because nobody knew it was happening. **Re-run the frame step table before spending any more time
on the `InputIntent` edge protocol.**

⚠️ **THE NOISE FLOOR IS ALSO WORTH RE-MEASURING.** § 16 records eight matches spreading 58 to 100
throws and lists the residual as unexplained. A seat whose effective heading depends on a yaw
frozen at whatever the spawn happened to leave it at is a per-run variable nobody had accounted
for. It may be part of the residual; it may be none of it. `TwoIdenticalMatchesLandInsideTheNoiseFloor`
is the six-minute `WallClock` test that answers it.

---

## 35 · The spectator flies itself, every key is in the panel, and a reconnect stops refunding cooldowns

**🧑 2026-08-27, four requests in one stretch**, after the § 33 AI work: *"try to improve network
as well, try to fix lan and online servers ... make sure rejoin logic works and think of edge
cases like u disconnect as taya, do u stay as taya or do u change role ... or if u retain ur skill
cooldowns and charges and shi"*; *"pls make sure everything is rebindable in settings and actually
works"*; *"make sure all keys are in settings and properly classified"*; *"add autopilot option in
spectator that moves on its own naturally and looks good, assume A LOT OF PPL WILL be watching how
it moves"*, with *"dont let autopilot spectator pause or replay thats for human only"*.

### 35.1 ⚠️⚠️ A RECONNECT REFUNDED EVERY COOLDOWN AND CONFISCATED EVERY ULTIMATE ✅

*"or if u retain ur skill cooldowns and charges and shi"*. **The answer was no.**

`MatchRpc.BroadcastWorldSnapshot` sends the round number, the defender slot, the clock, the
scores, the lata, every slipper, the picks and every unit transform. **It had never sent one byte
of ability state.** A client that dropped and came back rebuilt its kit from the constructor:
every cooldown zero, every charge full, the ultimate meter empty.

⚠️⚠️ **BOTH DIRECTIONS ARE WRONG AND ONLY ONE IS OBVIOUS.** Reconnecting to refresh a 62 s
cooldown is the cheat everybody thinks of first. The half that actually gets reported is the
other: a player who had banked 115 charge toward an ultimate lost all of it to a dropped packet,
which reads as the game stealing a round's work.

⚠️⚠️ **AND THE HOST NEVER HAD IT, WHICH IS WHY IT SURVIVED THIS LONG.** The host's kits are
continuous objects that were never rebuilt, so none of this is visible on one machine or to the
person running the lobby. It is the same shape as § 32.2: *"the host never runs the client half of
this method"*.

**Fixed** with `SyncAbility`, a per-seat named message riding the existing world snapshot:
ultimate charge, and a cooldown and charge count for each of the three slots.
`HeroKit.ApplyNetworkSnapshot` and `HeroAbility.ApplyNetworkSnapshot` are the entry points and
`AbilityRejoinStateTests` covers them.

⚠️⚠️ **IT DELIBERATELY DOES NOT CARRY `DurationRemaining`, AND THAT IS A CORRECTNESS RULE.** A
duration is not a number, it is a **grant that `OnEnd` has to take back**. `HeroAbility.Reset`'s
own header records what zeroing one behind an ability's back costs: Demonic Carapace's stun
immunity and Phantom Phase's tag immunity stay switched on with no timer left to switch them off.
Writing a duration IN from the wire is that fault from the other direction, and it would ship a
**permanently unstunnable rejoiner**. `ADurationIsNeverWrittenInFromTheWire` asserts it. A running
duration expires by itself in seconds; a cooldown does not, which is the whole reason only one of
the two is worth a packet.

⚠️ **THE COUNT IS CLAMPED, NOT TRUSTED.** It arrives off the wire, and a stale or malformed packet
must not be able to hand somebody more charges than the ability has.

### 35.2 The rejoin edge cases, checked one by one. Three were already right ✅

**These were asked as questions and the honest answer to most of them is that the existing code
already does the right thing.** Written down so nobody has to re-derive it.

| question | answer | why |
|---|---|---|
| Disconnect as taya, do you stay taya? | **Yes, and it is right.** | The role is DERIVED, `(round - 1) % 4`, and it belongs to the SEAT. `LobbySession.Depart` holds your seat by token while the match runs, `RuleOnArrival` returns `Reclaim` and `ReclaimSeatFor` gives you the same chair back. Same seat, same round, same role. |
| What fills the seat while you are gone? | A bot. | `MatchRpc.HostPeerLeft` sets `IsBot` and adds an `AIController`, so a 1-vs-3 does not become a 0-vs-3 and the round stays playable. |
| Rejoin after the round changed? | You get the CURRENT role, which may not be the one you left with. | `SendRebindLocalSeat` carries `match.DefenderSlot` and `ApplyRebindLocalSeat` writes `IsDefender` per seat off it; `RoundDirector.ApplySnapshot` does the same for everybody. That is correct: the taya rotates by round, not by person. |
| Does input authority come back? | Yes. | `ApplyRebindLocalSeat` destroys the seat's `AIController`, adds a `PlayerInputReader`, and strips readers from every other seat. `HostLateJoin` destroys the host-side AI and clears `IsBot`. |
| Do cooldowns and charges come back? | **They do now.** § 35.1. |

⚠️ **THE ONE PLACE THE SEAT-HOLD CAN STILL SURPRISE SOMEBODY** is `LobbySession.StartMatch`, which
clears `_heldSeats`. A held seat means *"somebody in THIS match left it"*, so it is deliberately
not carried across a match boundary. Leaving during the result screen and coming back after a
rematch starts is a new seat, by design.

### 35.3 ⚠️⚠️ NINE SPECTATOR KEYS EXISTED AND NONE OF THEM WAS IN THE PANEL ✅

*"make sure all keys are in settings and properly classified"*. Every action in
`TumbangPreso.inputactions` was already covered by `Rebinding.RebindableActions`, so the asset
side was complete. **The gap was keys read outside the asset entirely**: `SpectatorCamera` and
`Hud` read `Keyboard.current` and `Input.GetKeyDown` directly for TAB, F, V, B, N, P, R and C.
Nine controls a player can press, none of them visible in settings, none of them rebindable, and
none of them checked by `FindDuplicateBindings`.

**All nine are actions now**, in two new groups, **SPECTATOR CAMERA** and **BROADCAST GALLERY**,
alongside the new `SpectatorAutopilot`.

⚠️⚠️ **AND THAT REQUIRED REFINING `CLAUDE.md` § 4, WHICH IS THE MOST DANGEROUS KIND OF CHANGE TO
MAKE.** The rule reads *"one control, one action, in the input map"*. The spectator set
deliberately reuses TAB, F, B and R, which gameplay actions already hold, and giving them fresh
non-clashing defaults would have moved four keys out from under every existing spectator to
satisfy a rule about simultaneity that was never at risk. **A spectator has no body, no seat and
no `CharacterMotor`**: while watching, every gameplay action is inert, and while playing none of
the spectator set is reachable. The rule is now **one control, one action PER CONTEXT**, and
`Rebinding.SpectatorContext` names the second context.

⚠️ **THE NARROWING IS ASSERTED FROM BOTH SIDES** (`SpectatorBindingTests`), because if the reading
is ever wrong it is wrong silently: two actions really would fire on one key and nothing would
say so. `CleanFeed` is deliberately left in the player context, because it is a player action a
spectator also uses and its H default clashes with nothing.

⚠️⚠️ **TWO LITERALS WERE ALREADY LYING AND ONE HAD NO BINDING AT ALL.** `Hud.Update` read
`KeyCode.H` for a key **`CleanFeed` has always owned in the input map**, so rebinding "Hide HUD"
moved the action and left the HUD reading the old key with no error anywhere, which is exactly the
failure `docs/VISION.md` § 3 names. `KeyCode.C` was worse: bound to nothing, so it could not be
seen or changed. `SpectatorCamera.ControlsText` had the same problem at a larger scale, spelling
out seven key names in a string literal that would have started lying the first time anybody
opened the panel. All of them read the live binding now.

⚠️ **F1 TO F4 AND THE THREE SPEED DIGITS STAY LITERAL, ON PURPOSE.** They are a positional set
(POV of seat 1 to 4) and a numeric set (quarter, half, three-quarter speed). Splitting either into
separate rebindable rows adds seven lines to the panel so somebody can move "2" to "5".
`ControlsText` names them.

### 35.4 The autopilot spectator ✅

*"assume A LOT OF PPL WILL be watching how it moves so make sure it moves smooth and decides where
to move camera properly"*. It is `CameraSystem.SpectatorDirector`.

⚠️⚠️ **IT IS A NEW COMPONENT BECAUSE `SpectatorCamera`'S OWN HEADER DEMANDED ONE**, in a line
written beside 🧑's **2026-07-31** instruction *"dont give spectator AI... spectator should only be
controllable by a person"*: *"If a cinematic auto-cam is ever wanted it is a new component with a
new name."* **The 2026-08-27 request supersedes the 2026-07-31 one**, and the header now says so
in as many words, because that paragraph alone reads as forbidding a feature that ships. What the
old instruction was protecting is intact: `SpectatorCamera` is still the only thing in the game
that reads a spectator's hardware, and the director writes a **pose**, never an input.

**How it decides.** `docs/VISION.md` § 0 says the tension is the retrieval, so an armed attacker
inside the chalk is worth **6.0** and outranks everything; a body on the floor **2.6**; a hero
mid-ultimate **5.0**; and the gap between the taya and a retriever is worth up to **3.0** more,
because proximity between hunter and hunted IS the tension. The taya alone scores **0.6** and is
almost never the subject: it is the SECONDARY in somebody else's frame, since a defender alone in
shot is a person standing near a tin can.

**How it moves**, and all three of these are why it does not look like a script:

* ⚠️⚠️ **IT CUTS RATHER THAN WHIP-PANNING.** Past `CutDistance` **6.0 m**, under half the arena,
  the pose is written outright and the springs are cleared. Flying the whole way arrives after the
  moment it was sent for and sweeps the viewer past everything else on the way. **Clearing the
  `SmoothDamp` velocities is half the fix**: a leftover velocity makes the camera drift off its
  mark for half a second after a cut, which reads as the shot being wrong rather than as a cut.
* ⚠️⚠️ **IT COMMITS TO A SUBJECT.** `SubjectSwitchMargin` **1.25** and `MinShotSeconds` **2.4**,
  which is the same fix as `AiTuning.TagSwitchMargin` and `HeadingCommitSeconds` one layer down
  and for the identical reason: every term moves continuously while four bodies run, so the leader
  changes several times a second and a camera that followed it would point at the middle of the
  court and shake.
* ⚠️ **IT IS NEVER COMPLETELY STILL.** `DriftDegPerSecond` **3.4** keeps a slow orbit under every
  held shot, because a locked-off camera on a quiet moment reads as a frozen game.

Also: it aims `LeadSeconds` **0.42** ahead of the subject off `CharacterMotor.Velocity` (an
operator tracking a runner is always slightly ahead; one exactly on them looks dragged), it pulls
back with the spread of what it is framing so a chase keeps the chaser in shot, and it clamps to
`AIController.PlayableHalfX/Z` plus a margin so it never solves for a bearing that puts it inside
a house facade or a viaduct pillar.

⚠️ **THE HUMAN TAKES THE WHEEL BY MOVING IT.** Any look, fly key or target key disengages on the
spot. The mouse threshold is 0.01 rather than zero because a resting hand reports sensor jitter,
and a zero test hands the camera back within a second of engaging every time.

⚠️⚠️ **AND THE HARD PART OF *"dont let autopilot pause or replay"* IS NOT IN THE DIRECTOR.**
Nothing in that file calls `ToggleBroadcastPause` or `StartReplay`. But **`SpectatorCamera` has
replayed by itself since the highlight reel landed**: `StepPendingHighlight` starts one on a
knockdown, a tag or a score play with nothing pressed at all. Engaging the autopilot without
touching that would have produced a camera that flies itself AND replays itself, which is the
thing the instruction forbids however little of it the director wrote. `Update` suppresses it
while engaged, and **drops the queue rather than deferring it**, because replaying a play from
thirty seconds ago the moment a human touches the mouse is worse than not replaying it.

### 35.5 Still open

* ⚠️⚠️ **NOBODY HAS WATCHED THE AUTOPILOT, AND FOR THIS FEATURE THAT IS THE MEASUREMENT THAT
  MATTERS.** 🧑 asked for it on the grounds that *"A LOT OF PPL WILL be watching how it moves"*, so
  the acceptance test is how it LOOKS and `SpectatorDirectorTuningTests` cannot answer that: it
  holds the relationships between the numbers (a shot is held long enough to read, a cut is
  cheaper than crossing the court, the frame can hold a chase, the aim trails the body, a held
  shot is never still) and nothing about whether the result is watchable.
  `CLAUDE.md` § 6.1 is explicit that a change judged by eye needs a render attached, and this one
  has none. **What to build:** a probe on the `BotBehaviourProbe` pattern that loads Eskinita
  all-bots, adds a `SpectatorCamera` plus a `SpectatorDirector`, engages it, steps a whole round,
  and writes a frame every few seconds. Four or five stills across one round would settle the
  shot selection; the smoothness needs a person watching it live.
* ⚠️ **THE HANDOVER HAS NOT BEEN FELT EITHER.** `ManualTakeover` uses a 0.01 mouse threshold
  chosen against sensor jitter rather than measured on this machine. If it ever engages and hands
  itself straight back, that number is why.
* ✅ **§ 25.1 IS CLOSED AS OF 2026-08-27**, by `ReqAbility` / `PlayAbility` and the host gating
  that took the audit to zero. The paragraph below is what it said while it was open.
* ⚠️⚠️ **§ 25.1 WAS THE LARGEST NETWORK GAP.**
  `tools/audit_ability_authority.py` now reports **40 effect call sites, 2 host-gated, 23 ungated
  on another body, 15 ungated on the caster**. The ability layer still has no CAST rpc, so a
  remote player's skill or ultimate produces no VFX, no sound, no hazard and no sky on anybody
  else's screen. That is *"theres a lot of shit u dont see that ur supposed"* in one line, and
  § 35.1 only fixed what a rejoiner's METERS say. **The shape of the fix is known**: replicate the
  CAST (seat, ability slot, position, facing) and let every peer run its own presentation, with
  the host alone resolving effects on bodies, which is the architecture the rest of the game
  already uses.
* ✅ **STAMINA, STUN AND TRIP ARE STREAMED CONTINUOUSLY NOW**, not merely snapshotted:
  `SyncUnit` carries stun, its element, its mash progress, trip, mash removal, stamina, idle
  seconds and fatigue on every physics step, and `Stamina.ApplyNetworkSnapshot` re-enters the
  fatigue speed zone rather than only writing its timer. The paragraph below is the original.
* ⚠️ **STAMINA, STUN AND TRIP WERE NOT SNAPSHOTTED.** A rejoiner arrives with a full bar and
  no stun. Unlike a cooldown these self-correct in seconds, which is why they were left; if a
  reconnect ever needs to be exact, they are the next three fields on `SyncAbility`'s message.
* ✅ **THE TOURNAMENT CLOCKS TRAVEL NOW**, on `SyncWorld` at 5 Hz, through
  `RoundDirector.ApplyNetworkTournamentState`. Scoring stays host-only; this is so a rejoiner's
  HUD stops under-reporting a penalty already running against them. The paragraph below is the
  original.
* ⚠️ **THE TOURNAMENT CLOCKS WERE NOT SNAPSHOTTED EITHER.** `RoundDirector`'s taya-camp timer and
  per-attacker idle timers live host-side and only the host scores, so this is cosmetic, but a
  rejoiner's HUD will under-report a penalty already running against them.
* ✅ **THE SCROLLING IS MEASURED NOW AND THE WHEEL WAS THE PART THAT WAS BROKEN.** § 39 has the
  cause, which is that a wheel event is delivered by raycast and most of the panel had no graphic
  to hit. `SettingsWheelProbe` samples a 5 by 9 grid and asserts every point scrolls. The
  paragraph below is the original.
* ⚠️ **THE SCROLLING WAS NOT RE-MEASURED IN A PLAYER.** § 32.3 fixed the last known cause (a
  Slider root with no Graphic, so every drag fell through to the `ScrollRect`) and this session
  added **ten more rows** to the panel across two new groups, which makes the list longer than it
  has ever been. The configuration is right (`WheelStep` 24, a built scrollbar, inertia off,
  clamped, `RectMask2D`, keyboard steps), and nobody has dragged it in a build since.

---

## 36 · The host never transmitted its own bodies, so a joiner saw three statues

**🧑 2026-08-27, from a LAN test of the build BEFORE this session's fixes landed:**
*"movements only existed in host's side and no one that joined could see movement and shi
happening from them"*.

### 36.1 ⚠️⚠️ ONE `if` WITH ONLY HALF OF ITS PAIR ✅

`CharacterMotor.FixedUpdate` ended with this and nothing else:

```csharp
if (NetAuthority.ShouldRequest() && _playerSlot == NetAuthority.LocalSlot)
    Net.MatchRpc.Instance?.SubmitMoveServerRpc(...);
```

`ShouldRequest()` is `IsNetworked && !IsHost`, so **on the host it is false, always.** A client
submitted its own transform and the host relayed it, which is why a joiner could see OTHER
JOINERS move. **Nothing ever transmitted the host's own player, and nothing ever transmitted a
bot**, because bots are host-owned and have no client to ask on their behalf. In the usual test
(one host, one joiner, two bots) a joiner saw one moving body and three statues.

⚠️⚠️ **`NetAuthority`'S OWN HEADER PREDICTS THIS EXACT FAULT** in the paragraph above
`ShouldRequest`: *"Any verb that calls ShouldResolve MUST also handle ShouldRequest. If a verb has
one without the other, it is broken for somebody and probably silently."* It records the lunge
shipping dead for three of four players for weeks from the same shape. **Movement had the REQUEST
half and no host half at all**, which is that warning with the sides swapped, and it is the more
expensive version: a dead verb is a verb nobody uses, a dead transform is the entire game not
happening.

⚠️ **AND IT IS INVISIBLE FROM THE HOST'S CHAIR**, which is where it gets tested. Same family as
§ 32.2 (*"the host never runs the client half of this method"*) and § 35.1 (the host's kits were
never rebuilt). **Three separate networking faults this month have had the property that the
person running the lobby cannot see them.**

**Fixed** in `CharacterMotor.StepNetworkTransform`: a client still submits only the body it drives,
and the host now broadcasts every body IT drives on the same physics step.

⚠️ **WHICH BODIES, AND THE TEST IS THE INPUT SOURCE RATHER THAN A PEER LIST.**
`MatchInstaller.BuildSeat` gives the local human a `PlayerInputReader`, an unoccupied seat an
`AIController`, and a REMOTE human's seat **neither**, because that body is moved by the transforms
its owner submits. So "has one of the two" is exactly "the host simulated this body", it needs no
lobby lookup, and it is right the instant `HostPeerLeft` drops an `AIController` onto a seat
somebody just left.

⚠️ **RE-BROADCASTING A REMOTE SEAT WOULD NOT BE HARMLESS**, which is why the predicate is not
just "everything". The host's copy is up to one step behind whatever that client last sent, so
echoing it back out puts a body driven at 50 Hz into a fight with a stale copy at 50 Hz. That is
visible as a jitter on precisely the players who are playing well.

⚠️ **THE CACHE IS INVALIDATED ON EVERY SEAT HANDOVER** (`CharacterMotor.ForgetInputSource`,
called from `HostPeerLeft` and `HostLateJoin`). Miss one and the body either goes silent or starts
double-talking. `HostLateJoin` invalidates rather than recomputes because `Destroy` is deferred to
the end of the frame, so `GetComponent` would still find the `AIController` on that line.

⚠️ **IT SENDS ON THE PHYSICS STEP.** `ApplyUnitMove` snaps rather than interpolating, so a
slower host tick would make host-owned bodies visibly choppier than client-owned ones on the same
screen, which reads as those specific players lagging. Four seats at about forty bytes on a 50 Hz
step is roughly 8 KB/s downstream. **If that ever needs to come down, add interpolation on the
receiving end first**; a lower send rate on its own just moves the ugliness.

### 36.2 Still open, and it is the same shape ✅ CLOSED 2026-08-27

⚠️⚠️ **§ 25.1 IS THE REMAINING HALF OF *"a lot of shit u dont see that ur supposed"*.** Bodies
move on every screen now; **abilities still do not**. `tools/audit_ability_authority.py` reports
**40 effect call sites, 2 host-gated, 23 ungated on another body**, and there is no cast rpc at
all, so a remote player's skill or ultimate produces no VFX, no sound, no hazard and no sky on
anybody else's screen. § 35.5 has the shape of the fix and the warning that its two halves must
land together.

---

## 37 · Two Phaister presentation faults from the 4.72 player ✅ CLOSED, SEE § 43

**🧑 2026-08-27, with two screenshots:** *"her magic circle doesnt draw over the sidewalk and
thats weird af"*, and, on Shadow Blink: *"to teleport u have to hold her E skill and all it shows
is a frigging shadow, it's very easy to miss and not in her theme at all"*.

Both were fixed in the same session they were reported. § 43 carries the diagnosis for each, and
the first one turned out to be a class of fault rather than an instance: **every flat ground effect
in the game is a plane at the caster's own height**, so all of them stop at a kerb.
⚠️ **§ 43.3 is what is still open on them**, and it is renders rather than code.

---

## 38 · The network pass: eleven faults the host cannot see, and the loopback behind four of them

**🧑 2026-08-27, across one session:** *"can u pls thoroughly fix network and make sure everything
is heard and seen and experienced equally by everyone not just host"*, and *"when chat gpt started
making fixes it found out that there were more bugs than we originally thought with network"*.

§ 25.1, § 32, § 35 and § 36 are the same shape and this entry is the rest of it. Read § 36.1's
closing line first: **three separate networking faults that month had the property that the person
running the lobby cannot see them.** Eleven more did.

### 38.1 ⚠️⚠️ `SendNamedMessageToAll` LOOPS BACK INTO THE HOST, AND ELEVEN HANDLERS DID NOT KNOW ✅

The largest finding, and it is a property of the transport rather than of this game.
`CustomMessageManager.SendNamedMessage(name, clientIds, ...)` contains:

```csharp
if (m_NetworkManager.IsHost)
    for (var i = 0; i < clientIds.Count; ++i)
        if (clientIds[i] == m_NetworkManager.LocalClientId)
            InvokeNamedMessage(hash, m_NetworkManager.LocalClientId, ...);
```

A listen host is in its own `ConnectedClientsIds`, so **every broadcast the host sends is also
delivered to the host, synchronously, inside the send call.** Parts of the game rely on that
deliberately: `BeginCountdown`, `RematchTally`, `BeginRematch` and `PlayEmote` are how the host
starts its own countdown, tally, rematch and emote. Eleven handlers did not know:
`SyncUnit`, `SyncLata`, `SyncSlipper`, `SyncWorld`, `SyncAbility`, `SyncPicks`, `SyncMap`,
`SyncMode`, `SyncDiff`, `SyncLobbyPicks` and `StartMatch`.

Each applied the host's own snapshot back over the authoritative state it had produced one line
earlier: the can re-placed from a packet, the round clock re-applied from itself, every seat's
stun and stamina written back through the wire's precision, the pick table applied twice per
broadcast. Idempotent by luck rather than by design, at 50 Hz, on the machine that is also
simulating the match.

**Fixed** with one guard, `if (NetAuthority.IsHost) return;`, on exactly those eleven. The four
that WANT the loopback keep it and now say so in a comment.

⚠️ **The sender check is a separate question and it was also missing.** `FromHost(senderClientId)`
now gates every "play this" handler. It is not the last line of defence (Netcode refuses
client-to-client named messages at the sender) and it is not a loopback guard either, because a
listen host's own client id IS `ServerClientId`. It is what stops the next transport change from
turning a presentation message into somebody else's input.

### 38.2 ⚠️⚠️ THE HOST LOADED THE ARENA THREE TIMES ON EVERY NETWORKED START ✅

The same loopback with a consequence rather than a redundancy. `MatchRpc.HostStartMatch`:

1. broadcasts `StartMatch`, which loops back into `OnStartMatchMsg` on the host, which fires
   `OnMatchStarted` **and** calls `SceneFlow.StartMatch()`;
2. then fires `OnMatchStarted` itself, which `ConvertedMatchSetup.HandleMatchStarted` answers with
   another `SceneFlow.StartMatch()`;
3. and `ConvertedMatchSetup.OnStartPressed` called `SceneFlow.StartMatch()` on the line after
   `HostStartMatch()`.

`SceneManager.LoadScene` is deferred to the end of the frame, so three calls in one frame queue
**three loads of the same arena**. Everything the first load installed (four seats, the lata, the
ability systems, the camera rig) was torn down and rebuilt underneath whatever already held a
reference to it.

**Fixed** in three places and guarded in a fourth: the handler no longer runs on the host, the
button no longer double-calls, and `SceneFlow.Go` latches on `(scene, Time.frameCount)`.
⚠️ **The latch is scoped to one frame on purpose**, which is exactly the window the fault lives
in, so it cannot get stuck and a rematch on the same map is unaffected.

### 38.3 ⚠️⚠️ EMOTES HAVE NEVER TRAVELLED BETWEEN PEERS ✅

`MatchRpc.RequestEmoteServerRpc` and a whole `PlayEmote` broadcast behind it, complete and
correct, with **zero call sites**. `EmotePlayer.Request` had this where the call belonged:

```csharp
if (NetAuthority.ShouldResolve()) HostPlay(id);
// Phase 5: else send the request to the host here.
```

A client's emote played on its own screen only, and the host's played on the host's only.
⚠️ **Nobody reports this**, which is why it survived: an emote you cannot see is
indistinguishable from one nobody pressed.

**Fixed.** A client asks, the host validates with `CanEmote` and broadcasts, and the loopback is
how the host plays its own. ⚠️ **It predicts nothing locally**, unlike a throw: an emote is cheap,
`CanEmote` is a rule the host may legitimately refuse, and predicting it would start a dance the
host then cancels with the camera swung to third person for the length of one.

### 38.4 ⚠️⚠️ THE TAYA'S RESET WAS A LOCAL CHANNEL AND A ONE-BYTE REQUEST ✅

The taya holds Grab inside the ring for `Lata.ResetChannelTime` to stand the can back up, and that
hold is the whole counterplay. Both halves were wrong:

* **`Carrier.StepDefender` called `Lata.HostRestore()` on whichever peer held the key.** On a
  client the can stood up locally and the host's stream knocked it straight back down, so the
  reset flickered and failed.
* **`ReqReset` carried one slot and nothing else**, so the host restored the can the instant it
  arrived: a client could send it with no hold at all, from anywhere on the map, as often as it
  liked. It had no caller, so nothing exercised that either.

**Fixed** with a three-phase channel. The owner sends START, CANCEL and COMPLETE; the host stamps
its own clock at START, drops the channel **on its own physics step** the moment the defender
leaves `Balance.InteractionRadius`, loses the role or is stunned, and refuses a COMPLETE that
arrives early. ⚠️ **The host measures the duration rather than believing a reported one**: a number
in a payload is a number the sender chose. ⚠️ **One physics step of slack** and not a frame more,
because two processes step at different offsets and refusing that COMPLETE would fill the bar and
do nothing, which is the worst of both.

### 38.5 ⚠️⚠️ THREE VERBS HAD TWO PROTOCOLS EACH, AND THE DEAD ONE WAS THE ONE MAINTAINED ✅

`LungeCharge` and `ShoveCharge` were host-only broadcasts of an animation flag with **no
production call site anywhere in the tree since the day they were written**. `ReqBlink` was a
bespoke request for ONE power, written while the ability layer had no cast replication at all;
with § 25.1's general `ReqAbility` in place it was a second wire for a verb that already had one,
and a peer on a build carrying both would double-resolve the shove on the host.

All three deleted, along with `PhaisterHeroKit.ResolveBlinkShove`.

⚠️⚠️ **`tools/audit_request_call_sites.py` IS WHAT FOUND THEM AND IS WHAT STOPS THE NEXT PAIR.**
It walks every wire entry point in `Runtime/Net/` and reports whether anything calls it, splitting
the two halves because they fail differently. A CLIENT half (`Request*`, `Submit*`, `Select*`,
`Declare*`, `Vote*`) needs a caller OUTSIDE the network layer or the game never makes the request;
a HOST half (`Broadcast*`, `Host*`, `*ClientRpc`) is usually driven from inside the router and
needs one anywhere. ⚠️ **Tests do not count**, which is the point: a test calling a request proves
the method works, not that the game reaches it, and both dead charge relays would have been
"covered" by one. It reported three unreachable entry points when written and reports **zero** now.

### 38.6 ⚠️⚠️ EVERY PAYLOAD IS NOW CHECKED FIELD BY FIELD, AND NOTHING CHECKED THEM BEFORE ✅

`MatchRpc` speaks 41 named messages and every one is a hand-written pair: a run of
`WriteValueSafe` and a run of `ReadValueSafe`. **Netcode does not check that the two agree.** A
field added to one half does not fail; the reader consumes the same bytes in the wrong order and
hands the game plausible garbage. `SyncWorld` grew a tournament-clock block during this very
session, and had the reader not grown with it every client would have read the taya's camp timer
out of the middle of the score array.

`tools/audit_wire_payloads.py` prints each message's written and read field counts side by side,
type-checks every field whose type is knowable from a literal or a cast, and exits 1 on a
mismatch. ⚠️ **It is a COUNT and TYPE check, not a NAME check**: two floats swapped between the
halves are invisible to it and are a real bug. What it closes is the class that has actually
happened. Three deliberate asymmetries are listed with their reasons rather than silenced.

Current state: **41 named messages, 0 mismatched.**

### 38.7 ⚠️ A CLIENT'S LUNGE RESOLVED ITS OWN TAG ✅

`CombatVerbs.SweepLungeTag` ran on whichever peer was lunging, every frame the dash was live, and
called `RoundDirector.ResolveTag` directly: it staggered a body it does not own, respawned
somebody on its own screen alone, and asked for a score the host had not awarded. `CLAUDE.md` § 4:
**contact resolves by distance ON THE HOST.** The host runs the same sweep off `HostResolveLunge`
from the position the client reported, and that is the result everybody sees.

### 38.8 ⚠️ A HELD TSINELAS HAD TWO AUTHORS AND BUZZED BETWEEN THEM ✅

`Carrier` parents a held slipper to the carry anchor every FixedUpdate on every peer, and the host
streams `SyncSlipper` at the same 50 Hz. The packet put it where the host's hand was a step ago and
the carry put it where this screen's hand is now. `Slipper.ApplySnapshotState` now writes the
transform only when the state is not `Held`: **the state and the holder are authoritative; while
it is in a hand the position is a consequence of them.**

### 38.9 ⚠️ TWO REQUEST CHANNELS COULD BE FLOODED BY ANY CLIENT ✅

`ReqCue` takes a cue NAME off a client and the host fans it out to every peer, which makes it the
cheapest amplifier in the protocol: one client sending a cue every frame costs the host sixty
messages a second times the peer count, on the audio thread, at whatever world position it chose.
It is now validated against the cue catalogue (`AudioCues.IsKnown`, added for this), bounded in
position and volume, and capped at **25 a second per peer**, which is far above anything play
produces and far below anything that hurts.

`ReqSnapshot` is worse per message, because `HostSyncPeer` ends in a full world broadcast to
everybody: match state, the can, four slippers, four transforms and four ability kits. Capped at
**twice a second per peer**, which is more than a cold rejoin needs.

### 38.10 ⚠️⚠️ THE LAN BEACON HAD ONE NUMBER FOR THREE DIFFERENT QUESTIONS ✅

`_beacon.Players = Lobby.PeerCount` counts CONNECTIONS. So:

* a lobby with two players and six spectators advertised **8/4** and every browser struck it out
  as full;
* a lobby holding a seat for somebody who dropped mid-match advertised **3/4** and then refused
  whoever pressed join.

Both are "the server browser lies" and neither is fixable while the concepts share a field. The
payload now carries **seated**, **occupied** (seated plus held), **connections**, and both
capacities (4 seats, 12 sockets). `LanEntry.IsJoinable` asks for a free chair AND a free socket;
`CanSpectate` asks only for the socket.

⚠️ **The old seven-field payload is still READ, not still written**, so a build from before this
is listed rather than silently missing. ⚠️ **And the host name is taken as everything from its
index onwards**, because it is the only value on this wire a person types: a name containing the
separator truncates rather than corrupting every field after it.

⚠️ **The online browser had the same confusion with a different cause.** `UpdateHostedLobbyAsync`
was handed `Lobby.PeerCount` as "occupied", so a lobby with spectators reported itself full to the
UGS `AvailableSlots` filter. It is `OccupiedSeatCount()` now, and lobby capacity stays
`MaxPlayers` (4) rather than the Relay allocation's `MaxConnections` (12).

### 38.11 ⚠️⚠️ THE UGS LOBBY LOST THE UPDATE THAT MATTERED MOST ✅

`NetSession` fires `Query.CreateHostedLobbyAsync` and does not await it, so `_activeHostLobbyId`
is null for as long as UGS takes to answer. **Every `UpdateHostedLobbyAsync` in that window
returned on its first line**, and the update that got dropped was usually the first player
joining: the lobby then advertised 0 seated until somebody else connected, which in a two-player
match is forever.

The latest pending counts are now held and applied the moment creation settles. ⚠️ **The LATEST,
not the first**, so a burst of joins and leaves during creation collapses into the truth at the
end of it. ⚠️ **And `DeleteHostedLobbyAsync` waits for a creation in flight**: hosting online and
backing out inside the round trip used to leave a live lobby with nobody behind it, which the
browser advertised until the 30 second heartbeat expiry retired it.

### 38.12 ⚠️⚠️ ONE `LobbySession` OUTLIVES EVERY SESSION AND NOTHING RESET IT ✅

`NetSession` owns one `LobbySession` for the lifetime of the process. Host, quit to the menu, host
again reached `OpenLobby` with the previous match's peer table, its leader id and
`MatchInProgress` still set. A brand new lobby therefore believed it already had four players,
obeyed a leader whose transport no longer exists (so nobody could change the map), and answered
**Spectate** to the first person who tried to join it.

`LobbySession.Reset` is new and is called from `OpenLobby` and from `NetSession.Stop`. ⚠️ **It is
separate from `EndMatch`**, which ends a MATCH inside a lobby that keeps its peers, and the two
must not be merged. `OpeningASecondLobbyForgetsTheFirstOneEntirely` asserts it.

Two smaller things in the same file:

* **`Admit` searched for the same token twice.** The second lookup ran after the first had already
  found, copied and REMOVED that record, so it could never match. Two searches for one fact is how
  one of them stops being exercised; the dead one is deleted.
* **A seat that could not be found left `Seat == -1` with `Spectator` false**, which
  `PlayingPeerCount` and the ready gate both read as a player, so the gate waited forever for a
  press from somebody with no body to press it with.

### 38.13 ⚠️ A REFUSED CONNECTION SAID "disconnected" AND NOTHING ELSE ✅

The protocol-version check refuses a mismatched build at approval, and an approval refusal arrives
on the client as an ordinary disconnect. A version mismatch, a full lobby and a host that vanished
were all one word, and the version mismatch is the one a player can actually fix.
`NetworkManager.DisconnectReason` is surfaced now, and a client that loses the host also clears
its seat, its relay flags and its lobby state rather than carrying a dead allocation into the next
join attempt.

### 38.14 What the working tree already had, confirmed and finished

Everything below arrived in the tree this session started from, uncompiled. Each is verified
rather than re-derived, and the ones that needed finishing say so.

| | what | state |
|---|---|---|
| 1 | The duplicate spawned `MatchRpc` prefab is gone; `Awake` refuses a second router | ✅ |
| 2 | `ProtocolVersion` in the approval payload: refused clearly rather than joining wrong | ✅ plus § 38.13 |
| 3 | Fast reconnect replaces AND disconnects the stale transport | ✅ the `Contains` overload it used did not compile; fixed |
| 4 | `LobbySession.Depart` happens exactly once and returns the departed record | ✅ finished: the AI takeover reads the seat off the return value |
| 5 | Leader sentinel `0` to `-1`, because NGO client id 0 is legal | ✅ three tests updated with it |
| 6 | Sender-to-seat ownership, finite values, plausible poses, movement bounds and rate | ✅ |
| 7 | Remote bodies stop simulating locally and smooth toward the host's transforms | ✅ |
| 8 | Stun, trip, mash, stamina and fatigue stream authoritatively | ✅ |
| 9 | Host-side mash request, so a client's prediction is not reset by the host | ✅ |
| 10 | `ReqAbility` / `PlayAbility`: the general cast rpc § 25.1 asked for | ✅ |
| 11 | Cue relay suppressed during replicated ability execution | ✅ and made allocation-free |
| 12 | Every ability effect on another body host-gated | ✅ audit: **0 ungated on another body** |
| 13 | Live host streams for lata, slippers, match state, scores and tournament clocks | ✅ |
| 14 | Clients request pickup and throw rather than changing their own copy | ✅ |
| 15 | One authoritative throw, keeping Pektus spin and the three kit modifiers | ✅ |
| 16 | Throw wind-up and ordinary combat verbs replicated | ✅ |
| 17 | Shove, punch and lunge routed through the host request methods | ✅ plus § 38.7 |

⚠️ **`NetCue.SuppressRelay` allocated 240 objects a second.** It wraps `HeroKit.Tick`, which runs
once per seat per frame on every peer, and it returned a class. It returns a struct now and the
`using` binds `Dispose` directly rather than boxing it. Four seats at 60 fps for a whole match is
not a tidiness note.

⚠️ **Per-peer host bookkeeping is dropped when a peer leaves and per-seat bookkeeping when a chair
changes hands.** Client ids are handed out monotonically rather than reused, so the rate-limit
tables would otherwise grow by one entry per connection for the life of the lobby, and an arriving
player would inherit a movement-rate window or a half-finished reset channel from the bot that was
sitting there.

### 38.15 ⚠️⚠️ STREET HYPE WAS DEAD ON EVERY CLIENT, AND CLASSIC IS THE MODE IT BELONGS TO ✅

`Hud.ReportStyle` only fires for the LOCAL slot, and **every caller is host-side**:
`Carrier.HostThrowAt`, `Lata.HostKnockDown`, the tag, the snatch and the reset all sit behind
`NetAuthority.ShouldResolve()`. So on the host all four seats' events reached it and it drew the
host's own; on a client not one of them ran at all.

⚠️ **That is Classic's entire bottom-of-screen identity, missing.** `VISION.md` § 1.1 is explicit
that Classic gets depth instead of powers and names Street Hype as the pattern: it *"names skilled
curves, banks, close calls and blocks without changing a single point"*. A joiner was playing
Classic with the one thing that makes it Classic switched off, and it cannot be reported as a bug
because there is nothing on screen to say it should have moved.

The host now relays the award to the seat's owner, **to that one peer rather than by broadcast**,
because hype is a personal quantity and the other three screens would throw it away.
⚠️ **Two callers pass `relay: false`**: the LRT flyby and the bridge hoop run on every peer from
local state, so those award themselves and a relay on top would pay them twice.

### 38.16 ⚠️ A CLIENT'S THROW HAD NO ARM, NO VIEW KICK AND NO HYPE UNTIL THE HOST ANSWERED ✅

`Carrier.Release` on a client sent the request and did nothing else. The most frequent verb in the
game gave its own player no feedback for a round trip.

The PICTURE is predicted and the PHYSICS is not: the arm swings and the view kicks on the frame
the key comes up, and `HostThrowAt` still decides where the tsinelas goes from the origin and aim
point sent with the request. ⚠️ **The sound and the hype are deliberately NOT predicted**, because
both already arrive by relay and a predicted copy would give that one player a flam and double
hype. ⚠️ **And `Held` is not cleared optimistically**: with the wire correctly not writing a HELD
slipper's position (§ 38.8), an empty local hand would leave the shoe hanging in the air for the
round trip.

The host also announces the release, so the other peers see the arm move: `BroadcastActionExceptOwner`
skips the thrower, who has already played it.

### 38.17 ⚠️ THE SEAT ROSTER WAS READ ONCE, WHEN THE ARENA WAS BUILT ✅

`MatchInstaller.BuildSeat` takes each seat's name and human/bot flag from the replicated table at
build time, and nothing re-read it. So on a client, somebody joining an empty seat mid-match stayed
a nameless bot on three screens, and somebody dropping stayed a named human while a bot drove their
body. The host sees neither, because `HostPeerLeft` and `HostLateJoin` fix its own copy directly.
`SyncLobbyPicks` now re-applies the roster to live bodies, idempotently, skipping the local seat
because `ApplyRebindLocalSeat` owns that one.

### 38.18 ⚠️ THE PROP STREAM SENT 250 MESSAGES A SECOND FOR AN ARENA AT REST ✅

The world tick is the physics step, so a can standing still and four tsinelas lying in the road
cost five messages a step to every peer whether or not one of them had moved. Most of a round is
exactly that. They are sent on CHANGE now, with a **twice a second keepalive**.

⚠️ **The keepalive is not optional and it is what makes this safe.** A joiner who missed the one
packet that said "the can went over" would believe it upright until it moved again, which on a can
that has come to rest is the rest of the round. ⚠️ **And the unconditional senders stay**:
`Carrier` calls `BroadcastSlipperState` on a grab and a throw and the reset calls
`BroadcastLataState` on a restore, because those are EVENTS and an event may never wait for a poll
to notice it.

### 38.19 The two-process run, and what it proved ✅ / ⚠️

Two real player processes off the Desktop build, 2026-08-27, using the new
`NetStateReport` and `-tp-allbots` switches:

```
TumbangPreso.exe -tp-host 8910 -tp-profile nethost -tp-allbots -tp-map Eskinita                  -tp-netreport host.txt -tp-netseconds 40 -logFile host.log
TumbangPreso.exe -tp-join 127.0.0.1 8910 -tp-profile netclient -tp-allbots -tp-map Eskinita                  -tp-netreport client.txt -tp-netseconds 28 -logFile client.log
```

✅ **What agreed, and every one of these has been a live bug at some point:** the mode
(HeroStrike on both, § 32.1), the map, the protocol version, the character index of all four
seats (0, 3, 0, 3 on both, § 32.2), the taya, all four slipper states and holders, the can, and
the scores. The host seated itself at 0 and the joiner at 1, and neither log carries an error or
an exception.

✅ **AND THE JOINER SAW EVERY BODY MOVE**, which is § 36.1's exact failure: 8.4, 8.6 and 8.6 m
travelled on the three bot seats against the host's own 2.4, 1.8 and 1.9 over a shorter sample.
A client that saw statues would report zeroes.

⚠️⚠️ **BUT THE MATCH NEVER STARTED, SO THIS PROVES CONNECTION AND NOT GAMEPLAY.** Both reports
read `round 0` and `round active: False`, because nothing presses READY in a headless pair: the
seats only wandered their spawn settle, no ability was cast, no tsinelas was thrown and no point
was scored. **Everything this pass changed about throwing, casting, hype and the reset channel is
still unproven between two processes.** The missing piece is an autostart switch that satisfies
the ready gate host-side after a fixed delay; it is a small addition to `NetBootstrap` and it is
the single most valuable thing left in this entry.

⚠️ **ONE REAL DISAGREEMENT, AND IT IS A NEW BUG.** The host reports seat 1 as `bot: True` while
the client reports it as `bot: False`. The joiner arrived AFTER the host had built its arena, and
`MatchInstaller.BuildSeat` reads the seat table once at build time (§ 38.17). § 38.17 fixed this
for CLIENTS, through `SyncLobbyPicks`, and the host's own copy is fixed by `HostLateJoin`, which
clears `IsBot` and destroys the AI. One of the two did not happen here. **It is cosmetic today**,
because the seat is driven by the transforms its owner submits either way and the flag only feeds
the nameplate, but it is the same class as § 32.2 and it should be traced from `HandleIdentify`
into `HostLateJoin` with a log line at each step. `Logs/` is not where the evidence is: it is
`host.txt` beside the built player.

### 38.20 Still open

* ✅ **THE UNITY GAME DOES NOT USE THE OLD VULTR POOL.** The handoff for this pass called the
  Singapore VPS at `139.180.212.110` live, but the Unity source says the opposite:
  `ServerQuery` retired that pool and `ConvertedMultiplayerSetup` routes online hosting through
  UGS Relay plus Lobby. `docs/Port_Plan.md` and `docs/Port_Ledger.md` now state the current path
  explicitly and distinguish the optional Linux server build target from an active deployment.
  Verified by a repository-wide search: the Unity runtime contains no Vultr endpoint or pool
  query, and the only address reference is the retirement note in `ServerQuery`.
* ⚠️⚠️ **TWO REAL MACHINES ON A LAN HAVE STILL NOT PLAYED THIS.** § 1's closing note has said so
  since 2026-08-26 and it is still true. Everything in this entry was found by reading the wire and
  by running two processes on one machine, which shares a clock, a filesystem and a loopback
  adapter with itself. The Relay path in particular is exercised only as far as allocation.
* ⚠️ **The bot takeover on disconnect is now reachable and has not been WATCHED.** § 38.14 row 4
  is what makes it possible; somebody still has to see a dropped seat keep playing.
* ⚠️ **`ReqCue`'s 25 a second is a bound, not a measurement.** Nothing has counted the cues a real
  fight produces.
* ⚠️ **`audit_wire_payloads.py` cannot see two same-typed fields swapped between the halves.** The
  only thing that would is naming the fields on both sides, and they are locals.

---

## 39 · The settings wheel, for the fourth time, and the cause the first three missed

**🧑 2026-08-27:** *"the scroll in settings is still broken! yes u can scroll by holding scroll and
yes i want to keep that feature but u cant scroll by using mouse scroll or laptop pad scroll!
repeated complaint! it feels so clunky/doesnt work at all!!"*

### 39.1 ⚠️⚠️ UNITY DELIVERS A WHEEL EVENT BY RAYCAST, AND A PANEL IS MOSTLY HOLES ✅

§ 15.8 added a scrollbar and changed the wheel step from 45 to 24. § 32.3 gave the slider rows a
hit rectangle. Both were real and neither is this.

`StandaloneInputModule` takes `pointerCurrentRaycast.gameObject`, asks
`GetEventHandler<IScrollHandler>` for the nearest ancestor that handles a scroll, and **when the
raycast hits nothing there is no object to walk up from and the wheel is discarded.**
`TscnUiImporter`'s `ScrollContainer` case adds a `ScrollRect` and a `RectMask2D` and **no
graphic**, and the content is a layout group with no graphic either. So the only raycastable
pixels in the whole list are the row widgets themselves: the gaps between rows, the padding down
both edges, the strip beside the scrollbar and every part of the panel outside the viewport are
holes.

The wheel worked over a key cap and did nothing one pixel above it. That is not "broken", which is
why it survived three passes, and it is exactly what *"clunky"* describes.

**Fixed in two halves, because either alone leaves a dead region:**

* an invisible full-rect raycast target at the BACK of the viewport, closing the gaps inside the
  list. Same idiom § 32.3 used on the slider rows, and `SetAsFirstSibling` keeps it behind the
  content so it can never swallow a click meant for a row;
* `ScrollWheelRelay` on the panel root, which forwards `OnScroll` to the one `ScrollRect`, so the
  wheel works over the heading, the margins and the button row too. ⚠️ **It forwards rather than
  scrolling itself**, so the step, the clamping and the scrollbar stay owned by the `ScrollRect`
  and cannot drift; and ⚠️ **it can never steal from an inner list**, because `IScrollHandler`
  bubbles from the deepest handler outwards.

### 39.2 ⚠️⚠️ NOTHING HAD EVER MEASURED THE WHEEL, WHICH IS WHY IT TOOK FOUR PASSES ✅

`SettingsScrollProbe` checks the bar's geometry and that the list moves **when its normalised
position is set**, which is not the thing a player does and passes cleanly on a panel the wheel
cannot reach.

`SettingsWheelProbe` is new and does the thing: it opens the panel, walks a **5 by 9 grid** of
points across it, raycasts through the real `EventSystem` at each, resolves the scroll handler the
way the input module does, dispatches a real scroll event, and asserts the content actually moved.
⚠️ **It samples a grid rather than the centre**, because the fault was never "the wheel does
nothing", it was "the wheel does nothing over about half the panel", and one sample in the middle
of a key cap passes against the broken build. It writes `Logs/settings-wheel.txt` naming every
dead point and what was under it.

**Dragging the bar is untouched.** 🧑 asked for that to stay and it stays.

---

## 40 · The train is one field recording now, and it plays rarely

**🧑 2026-08-27, handing over a 10.55 s clip:** *"can u also replace current train sound. i keep
reporting its broken and i give up on it. replace train passing by sound and train sound as a
whole with this. make it play very rarely"*.

### 40.1 ⚠️⚠️ THE TRAIN HAD THREE SYNTHESISED SOUNDS AND THAT IS MOST OF THE PROBLEM ✅

A distant one-shot warning (`sfx_lrt_pass`), a 2.0 s seamless bed looped on the moving source
(`sfx_lrt_rumble`), and a borrowed `sfx_fire_whoosh` fired from a fixed point at z = -18. Three
sounds for one object, two of them synthesised and one of them about fire.

Now: **one recording, on the moving source, non-looping.** `sfx_lrt_rumble` and the whoosh are
deleted, and `tools/generate_ability_audio.py` no longer registers either synth so a regeneration
cannot overwrite the recording with the synthesis it replaced. ⚠️ **The retired seed slots are not
reused**, so every other cue keeps the seed that produced the audio in the repository.

⚠️⚠️ **THE CLIP IS TIME-ALIGNED TO THE PASS BY ARITHMETIC AND NEEDS NO OFFSET.** Measured on the
recording, its loudest quarter-second is at **2.70 s** (RMS 0.234 against 0.10 for the carriage
tail). The consist spawns at z = -48 and crosses at 18 m/s, so it is overhead at **48 / 18 =
2.67 s** after the source starts. Starting the clip when the run starts puts the recording's own
pass within **0.03 s** of the real one. If `Speed`, `StartZ` or the clip change, that is the sum
to redo.

⚠️ **The moving source is kept and is the point.** `LrtTrainFlyby` § THE PASS records why: a
one-shot parked at a fixed position faded by the LISTENER walking, never by the train leaving.

⚠️ **The mix row moved with the job.** `sfx_lrt_pass` inherits the bed's -16 dB rather than the
one-shot's 0, because it is now 10.55 s of sustained noise and that is the shape the -16 was
measured for. A long sound is mixed as a background whatever it is called.

**Prepared as the project's other cues are:** decoded to 44.1 kHz mono, peak-normalised to 0.85
(`AudioCues.HeadroomDb`'s convention), with 30 ms fades so the source cannot click.

### 40.2 The interval, raised for the third time ✅

24 to 78 to 150 to **300 s**, with the first pass at 20 s. A Classic match (4 x 90 s) sees the
opener and about one more; a Hero Strike match (8 x 90 s) sees the opener and two.
⚠️ **`InitialDelay` is what keeps it learnable at any interval**: the first pass is what teaches
the map has one. ⚠️ **And it is a balance change**: `OverheadPassWindow` gives Hero Strike double
cooldown rate while the consist is overhead, so a whole match now carries two or three of those
windows. § 5 already records that the overclock window has never been measured against a match;
this makes measuring it more urgent.

---

## 41 · The ultimate meter counts events now

**🧑 2026-08-27:** *"wtf how many points or charges to ult does downing can give? i want downing
can and tayaing to only give one point for the charges"*, and then *"i wanted like 10-20 charges
required on ult depending on impact"*.

A knockdown was 25 and a tag 20 against costs of 90 to 150, so the only way to answer "how close
am I" was to divide two numbers nothing on screen ever showed. **One knockdown is one charge. One
tag is one charge.** An ultimate costs 10 to 20 of them, ranked by impact:

| Hero | Ultimate | Cost | was |
|---|---|---|---|
| Zack | Thunderstrike | **20** | 150 |
| Cheska | Glacial Nova | **17** | 140 |
| Sean | Supernova | **15** | 130 |
| Phaister | Grand Coven | **13** | 115 |
| Dante | Titan Fissure | **12** | 110 |
| Nemu | Seance Void | **10** | 90 |

Retrieval is 0.5 and a throw 0.15, which are exactly what 12 and 4 were against 25. The ONE
deliberate change inside the rescale is the tag, from the 0.8 a straight division gives to a full
1.0, because both objectives were asked to be worth one: a 25 per cent raise to the taya's only
source of charge, for one round in four.

⚠️⚠️ **IT IS A REAL PACING CHANGE AND THE ARITHMETIC IS WRITTEN NEXT TO IT.** The old economy
bought the dearest ultimate for six knockdowns; this asks twenty. A live attacker earns about
**4.3 charges a round** (1 to 2 knockdowns, 3 to 4 retrievals, 5 to 6 throws), so Nemu's 10 lands
after about two and a half rounds and Zack's 20 after about four and a half: between one and three
ultimates per seat per match, against roughly three to five before.

⚠️ **If a match measures fewer than one ultimate per seat, the COST is the lever, not what an act
pays.** The earn table is what makes the meter readable and inflating it undoes that.
`BotBehaviourProbe` prints ultimates per match; `Hero_Strike_Balance.md` § 3.1 has the tables.

⚠️ **Whole-number costs were tried and rejected.** Rounding the six to integers moves them by up to
11 per cent and collapses Dante against Phaister, so a readability change would have silently
re-tuned six ultimates.
`InputMapAndAbilityTests.UltimateCostsAreRankedByHowMuchTheUltimateSwingsARound` asserts both the
order and the 10-to-20 band.

---

## 42 · Nemu's ride home was being erased by her own body's bot

**🧑 2026-08-27:** *"nemu E recast doesnt work as intended, she's supposed to teleport to where her
ghost is when she recasts or when ability ends but right now recasting just extends ghost form
time and that doesnt make sense, u cant end ability early and shi"*, and *"ALSO she is supposed to
be controlled by a bot (her real body) but it doesnt work that way right now"*.

### 42.1 ⚠️⚠️ TWO WRITERS ON ONE `InputIntent`, IN AN UNDEFINED ORDER ✅

Both reports are one fault. `GhostPetCompanion.BeginPossession` adds a temporary `AIController` to
Nemu's body so she is not a statue, and `PlayerInputReader` deliberately keeps Skill2 live during a
possession so the player can come home. **Both write `CharacterMotor.Intent`, and neither component
declared an execution order**, so Unity picked one arbitrarily: the AI wrote `Skill2 = false` after
the player wrote `true` often enough that the return press simply did not exist. The possession
then ran its full 6 s and ended on the timer, which reads exactly as *"recasting just extends ghost
form time"*. Nothing was extended; the press was eaten.

It is also why the bot takeover *"doesnt work that way"*: the AI was there and driving, and the
thing it was visibly doing was cancelling her recast.

**Fixed with a rule rather than a race.** `AIController.AbilitiesEnabled` is false for exactly one
controller, the temporary one: **while a human is driving the pet, the human owns the hero keys
and the bot owns the legs.** `CLAUDE.md` § 4's *"a bot presses the same buttons a human does"* is
unharmed, because this is one body with two drivers rather than a second path into the game.

⚠️ **The stun branch had the same hole.** `ReleaseAll` calls `intent.Clear()`, which empties the
whole table, so Nemu's body being stunned would have stranded the player inside the pet with no way
back. A suppressed controller now clears the legs and leaves the hero keys alone.

⚠️ **And the order is declared anyway**, `AIController` at -130 and `PlayerInputReader` at -120, so
the human's write lands last. The flag is the rule; the order is the belt.

### 42.2 Still open

* ⚠️ **The recast has not been felt in a build.** The teleport home, the early end and the bot
  driving her body are all reachable now; whether the trip reads correctly at 6 s is a judgement.

---

## 43 · Two Phaister presentation faults, and a class of fault behind one of them

**🧑 2026-08-27, with two screenshots:** *"her magic circle doesnt draw over the sidewalk and thats
weird af"*, and, on Shadow Blink: *"to teleport u have to hold her E skill and all it shows is a
frigging shadow, it's very easy to miss and not in her theme at all"*.

### 43.1 ⚠️⚠️ EVERY GROUND EFFECT IS A FLAT PLANE AT THE CASTER'S HEIGHT ✅

The screenshot is Ilalim ng Tulay at night. Grand Coven's 12.8 m inscription paints the road
perfectly and then **ends in a hard straight line along the pavement edge**, rings and writing
simply gone where the sidewalk begins.

⚠️ **It is the depth buffer, not missing geometry.** `VfxShapes.Lay` puts the mesh a few
centimetres above the cast point and the pavement stands about a quarter of a metre higher, so the
far half of the inscription is UNDER the kerb and correctly occluded. Nothing was clipped; it was
buried.

⚠️⚠️ **AND THE TWO OBVIOUS FIXES ARE BOTH WORSE.** Raising the plane to clear the kerb makes it
hover over the road, which is the one surface it must look painted on. Drawing it with the depth
test off puts it over the PLAYERS, and `VISION.md` § 2 rule 5 is that a frame mid-fight must still
show every player.

**Fixed with draping.** `VfxShapes.DrapeToGround` pushes every vertex of a laid-flat mesh onto the
surface under it, cast per vertex and cached on a 12 cm grid, once, at build time.
⚠️ **`maxRise` is 0.60 m and that is what keeps it a GROUND effect**: it climbs any kerb, step or
pavement in either arena and refuses everything taller, so a circle overlapping a wall is hidden by
the wall exactly as a real inscription would be. The small pieces (medallions, ticks, script
characters, spokes, floating glyphs) are SNAPPED whole rather than bent, because the ground under a
30 cm medallion is flat at its own scale and that costs one ray instead of a dozen.

Applied to the ultimate's rings, its reach collar and her HEX ward, which is 4.8 m across and can
be thrown onto a pavement too. ⚠️ **It is a class of fault, not an instance**: every flat ground
effect in the game has it, and `DrapeToGround` is the tool for the next one.

### 43.2 ⚠️⚠️ THE AIM TELEGRAPH WAS THREE FLAT DISCS AT EMISSION 0.5 ✅

*"all it shows is a frigging shadow"* is literally accurate. `GroundReticle` drew three
`PrimitiveType.Cylinder` plates, ghosted, at emission 0.5, which on asphalt is a grey smear and on
Ilalim ng Tulay, where the whole street is under a viaduct, is genuinely dark. It is also the
construction § 19 named as the game's default mistake, made three times.

Two things were wrong and both are fixed:

* **Legibility, for every hero.** The ring is a real annulus with a tick crown at emission 1.40 and
  alpha 0.95, and the interior wash drops from 0.22 to 0.12 so the telegraph tints the court
  instead of covering it. ⚠️ **What carries a telegraph is its EDGE**, because the question a
  player is asking is whether they are inside it. The tint emission goes from 0.50 to **1.60**, and
  that single number is most of why the accent colour never reached the screen.
* **Theme, for the blink.** `HeroAbility.AimBeacon` stands a mark up at the destination, and it is
  a `Rift`, the same torn sheet `HeroHazards.SpawnShadowRift` tears at the place she LEAVES. The
  aim mark, the departure and the arrival are now one visual idea rather than a grey decal followed
  by two unrelated effects. ⚠️ **It is for powers that put YOU somewhere, not powers that LAND
  somewhere**: a ring on the road is right for a zone and wrong for a teleport, because the player
  is looking along the street at head height where a flat decal five metres away is a few pixels.

### 43.3 Still open

* ⚠️⚠️ **NEITHER HAS A RENDER ATTACHED, AND `CLAUDE.md` § 6.1 IS EXPLICIT ABOUT THAT.** Both are
  changes judged by eye. `AbilityShowcaseProbe` will photograph the blink's transient and the
  ultimate's circle; the draping in particular wants a shot taken ON Ilalim ng Tulay standing on
  the kerb, because that is the only place the fault was visible.
* ⚠️ **The reticle is brighter for every hero now**, which is a change to five kits made to answer
  a report about one. `AbilityShowcaseProbe`'s 12 per cent white bound is the guard, and it has not
  been re-run against a held telegraph.

---

## 44 · § 32.3's slider fix was muted by the sweep on the next line ✅ CLOSED 2026-08-27

⚠⚠ **§ 32.3 IS TICKED AND THE SLIDERS WERE STILL DEAD, AND THE REASON IS WORTH MORE THAN THE
BUG.** That entry is right about the cause and its fix is the right fix: `BuildSlider` gives the
Slider's root a full-rect transparent raycast target. **`ClearStrayRaycastTargets` then turned it
off, on the same import run, a few milliseconds later.** The sweep kept a graphic only when it
WAS a Selectable's `targetGraphic`; the pad is on the slider's own node, so the question found
the Slider and the answer was the Handle.

**The evidence is in the regenerated asset.** `SettingsPanel.prefab` was reconverted with the pad
in place and shipped **4 live raycast targets against 54 muted** — the same four Buttons as
before the fix. Four pads made, four pads muted.

⚠ **A ticked entry whose fix is cancelled downstream is worse than an open one**, because the
next person reads it as done. This entry finishes it.

The report that reopened it: the settings sliders are "hardcoded and broken", and the volume
cannot be changed with the mouse. All four of them, on both the title screen's panel and the
pause overlay, which instances the same converted panel.

**They were not hardcoded and their listeners were wired.** They were receiving no pointer event
at all, so a press at the centre of a volume row went through the slider and landed on the card
behind it.

### 44.1 The cause: one sweep that can only see a hit area on the control's own node

`TscnUiImporter.ClearStrayRaycastTargets` inverts Unity's default and mutes every graphic that is
not the `targetGraphic` of a Selectable, because a converted Godot scene is wall to wall
`mouse_filter = 2` decoration and Unity's default is "eat every click". It decided that per
graphic, by asking `graphic.GetComponent<Selectable>()`.

⚠⚠ **That question only has the right answer when the hit area sits on the control's OWN node.**
A Button passes: its `targetGraphic` is the Image beside it on the same GameObject. **A Slider
does not.** Unity puts a Slider's Background, Fill and Handle on CHILD nodes, so all three
answered "no Selectable here" and all three were muted. `SettingsPanel.prefab` shipped with **50
graphics at `m_RaycastTarget: 0` against 4 at 1**, and every one of the four live ones is a
Button.

The sweep now collects the live set from the Selectables instead: each one's `targetGraphic`
wherever it lives, plus the graphic on its own node.

### 44.2 The hit area is the whole row, not the groove

`BuildSlider` lays a transparent Image over the slider's own node, and `MenuKit.EnsureHitArea`
does the same at runtime for the panels that are already committed as prefabs. Both are the
control's full rect on purpose: the converted groove is a **14 px band centred in a 34 px row**,
so restoring the Background alone would have handed the player a 14 px tall target. Alpha plays
no part in a graphic raycast, so nothing about the drawing changes.

⚠️ **The runtime repair is not belt and braces.** The converted panels are committed assets; a
player running the shipped build never re-runs the importer, so the importer fix alone would have
changed nothing until somebody reconverted the scenes.

### 44.3 The second defect on the same row: a window resize per drag frame

Every slider's callback called `SettingsStore.Current.Apply()`, and `GameSettings.Apply` is
`ApplyDisplay` plus the AI difficulty. **`ApplyDisplay` is a `Screen.SetResolution`**, so dragging
one volume slider across its groove fired a window resize on every frame of the drag. No slider on
this panel feeds either system: the three volumes are read live off the store by the music bed,
the announcer and the SFX bus, and the sensitivity is read live by `CameraRig` and
`SpectatorCamera`. The call is gone.

### 44.4 Why nothing caught it

`UiClickProbe` is the only check in the project that can see this class of bug, and its comment
said in as many words that it enumerated Buttons and Dropdowns alone and that sliders should be
swept in **deliberately, not by accident.** It is widened to Sliders now, which is the regression
test: it scrolls each one into view, raycasts its centre, and fails if the topmost hit is not the
control or one of its own children.

---

## 45 · The in-match HUD had five ambient sines, three copies of "LATA DOWN" and twelve coloured cells

🧑, 2026-08-27, off two gameplay frames of Ilalim ng Tulay in Hero Strike:

> *"theres pulsing shit that feels weird to look at and unnecessary (huds and ui for the actual
> game)"*
>
> *"repetitive lata down and theres too many animations happpening on screen (for the huds as
> well as text) it can feel overstimulating TOO many diff colored shits for text too, try to
> simplify hud and ui while playing but still let it show essential things like penalties,
> score"*

He also asked, of Nemu's deck tile: *"why does nemu have 2 charges if its just recast? should
just show 1"*.

### 45.1 What was actually moving, counted

Every one of these ran off `Mathf.Sin(Time.unscaledTime * k)` and none of them was an event: they
were **states drawn as motion**, so on a live round several were going at once, at different
frequencies, out of phase with each other.

| Widget | Rate | Amplitude | How much of a round it ran for |
|---|---|---|---|
| Timer card | 3 / 5 / 8 rad/s | 2.5 / 6.5 / 12 % | the last **30 s of 90**, always |
| Pressure line under it | 6 rad/s | 5.5 % | the last 15 s, always |
| Lata card | 7 rad/s | 10 % | every second the can is down |
| Lata alert, centre screen | 8 rad/s | 9 % | every second the can is down |
| Status stack row | 10 rad/s | 3.5 % | the last quarter of every timed effect |
| Active skill rim | 7 rad/s | colour, 35 % toward white | the whole duration of the power |
| Ultimate-ready rim | 4.5 rad/s | colour, 55 % toward white | every second it is up and unspent |

Seven, and the deck's own comment claimed the ultimate breath was *"the only continuous motion
the deck is allowed"*. It had not been that for a long time.

**All seven are gone.** What is left on the in-match HUD is exactly three motions, and every one
of them fires on an EDGE and returns to rest:

- `ApplyPop`, 0.18 s, when a skill or the ultimate comes back up.
- The score-row punch, 0.34 s, on a swing of 20 points or more.
- The pip-grant flash, 0.45 s, when a charge is handed back by `Recharge`.

⚠⚠ **The rule this settles: an EVENT gets the motion, a STATE gets a colour.** Anything that
is true for tens of seconds at a time may not move, however gently, because the player cannot
look away from the screen it is drawn on.

⚠️ **Every removal PINS the transform rather than skipping the assignment.** Timer card, lata
card, lata alert, pressure line and status rows are all built once and reused, so one left
mid-sine would keep that scale for the rest of the match.

### 45.2 "LATA DOWN" was on screen up to FOUR times at once

The card title (as `⚠  LATA DOWN  ⚠`), the centre-screen alert (`LATA DOWN  ·  RETRIEVE NOW`),
a 1.6 s toast fired from `MatchInstaller` on the same edge (`LATA DOWN  ·  RETRIEVE NOW` again),
and the crosshair while a slipper was charging (`LATA DOWN\nHOLDING CHARGE`). Two of the four
were animated, and all four sat within one glance of each other.

⚠⚠ **The fourth was only found by PHOTOGRAPHING a live round, not by reading the HUD.** Three
of them live in `Hud.cs` and were obvious side by side in one file; the toast is registered in
`MatchInstaller.InstallHudSignals` and reads as an unremarkable event announcement until you see
it land on top of the other three. `GameplayShots.ALiveRoundIsPhotographed` is what showed it.

Split by sentence now, one surface each:

- **Card**: the state. `LATA DOWN`, no glyphs.
- **Alert**: the action only. `RESET IT NOW` / `RETRIEVE NOW`.
- **Crosshair**: the throw. `HOLDING CHARGE`, which is the one thing neither of the others knows.
- **Toast**: nothing. The 0.45 s `SetDownedFlash` on the same edge and the alert appearing are
  the event; the words had three homes already. `LATA IS BACK UP` is kept, because when the can
  comes up the card goes quiet and the alert vanishes, and a thing disappearing does not announce
  itself.

⚠️ **A FIFTH instance survives on purpose and is worth a decision.** `ComicPopup` throws a
world-space "LATA DOWN!" above the can on the same edge, visible in
`Logs/shots-play/round-eyes.png`. It is kept because 🧑 said *"the abilities and other effects
are okay"* and because unlike the four HUD surfaces it is anchored to WHERE the can is, which is
information none of the others carry. **If the words still feel repetitive in play, this is the
one left to cut**, at `Lata.cs:262` where the popup string is chosen, rather than in the
HUD.

The pressure line lost its two earlier bands the same way: `PRESSURE BUILDING` at 30 s and
`FINAL PUSH · ...` at 15 s said nothing the clock beside them did not, and rewrote themselves
twice on the way down. One string, in the last ten seconds, that does not change while it is up.

### 45.3 Twelve coloured cells became four

The scoreboard painted **name, role word and score** of every row in that seat's role colour, so
a four-seat board put up to three saturated hues across twelve pieces of text in one corner. The
role colour is now spent on the **role word only**, which is the one cell where the colour is the
content; the rail and the row plate underneath are untouched and still carry the role at a
distance. Name and score are Cream on your own row and CreamMuted on the other three, so "which
one am I" is carried by weight rather than by a second hue.

Classic's deck lost the same class of thing: the Street Hype title swapped hue at tier 3 and the
fill LERPED orange to yellow across its range, both changing continuously while hype drains. The
tier NAME is in the same string and the bar's length is the number; neither needed a colour.

### 45.4 Nemu's pips read as "the second one is the way home"

Astral Projection is one press out and one press back, and the tile says `RECAST` for the whole
six seconds it is out. The two charge dots above it were answering a different question at the
same moment ("you get two of these a round"), and the two readings collided.

**`PaintCharges` now hides the pip row while `skill.IsActive && skill.CanReactivate`.** Gated on
`CanReactivate`, not on a hero id, so any future ability with a return press gets it for free.

⚠⚠ **`MaxCharges` IS UNTOUCHED AT 2, AND 🧑 CONFIRMED THAT IS WHAT HE MEANT.** Asked on
2026-08-27 whether the ask was the readout or the number, he answered **the display**. Every
other hero's charge skill carries 2 (`CheskaHeroKit` skill1, `DanteHeroKit`, `PhaisterHeroKit`,
`SeanHeroKit`, `ZackHeroKit`), and the free reactivation means Nemu's 2 buys two round trips
rather than two casts, which is the same shape as the rest of the roster.

⚠️ **So do not "fix" this later by cutting her to 1.** The confusion was two readings colliding
in one tile, and the tile is what changed.

### 45.5 What was deliberately kept

Penalties, the score board, the round line, the clock and its colour bands, the status stack with
its draining bars and tenths, the crosshair, the ability deck's three states, the ultimate's
notched meter, the toast and the countdown. 🧑 asked for *"still let it show essential things
like penalties, score"* and nothing in that list lost a fact; what came out was repetition,
motion and hue, not information.

In-world ability effects are **not** touched: *"the abilities and other effects are okay"*.
`GroundReticle`'s aim breath stays.

### 45.6 Verified

Core **69/69**, EditMode **124/124**, PlayMode **66/66** (`!WallClock`), and `Checks.RunAll`.
Fresh gameplay frames in `Logs/shots-play/` from `GameplayShots.ALiveRoundIsPhotographed`, copied
versioned to `Logs/shots-hud/hud_calm_round_v2.png` and `hud_calm_taya_v2.png`. The v1 pair from
the run BEFORE the toast fix is what showed the fourth "LATA DOWN"; the v2 pair is one alert, one
card, one in-world popup.

⚠⚠ **`Checks.RunAll` comes back FAILED on `headless` and `audio cues`, and both are § 21's,
not this work's.** `HeadlessCheck` asserts `HeroPeople.Count == 5` and `AllPeople.Count == 17`
against a roster that gained Phaister, and `AudioCueCheck` reports `PhaisterHeroKit` firing
`sfx_ghost_appear`, which § 21 already records as arriving with no file and no registration.
Neither reads a file this change touches. `ArenaCheck`, `MapGeometryCheck` and `SceneScriptCheck`
are all OK.

⚠️ **The first `RunAll` launch of the session came back exit 1 with a 2 KB log and no checks
run at all**, because a previous Unity was still holding the project lock. That is § 7's
lockfile trap and it looks exactly like a broken install. The second launch, after the processes
had gone, ran everything.

**Done looks like:** a gameplay frame where nothing is moving unless something just happened.

---

## 46 · Both intermission banners were drawn on top of something ✅ CLOSED 2026-08-27

Reported off a Hero Strike frame of Ilalim ng Tulay: raise the practice line, lower the "open a
gap with your powers" line because it covers things, and make that one go away after five to ten
seconds because it is annoying.

**All three are one fault repeated: the two banners were positioned against the screen edges
rather than against what is already parked at those edges.** The arithmetic, because it is not
close in either case.

### 46.1 The practice line was inside the ability deck

Both decks are bottom-anchored with a bottom pivot. The hero row spans **y 14 to 92**
(`DeckBottomMargin` + `DeckHeight`); the Classic row spans **24 to 124**. `ReadyPromptPlate` was
pinned at 92, so it drew from **92 to 126**: flush against the top edge of the hero deck, and
**32 of its 34 px inside the Classic one**. `InspectHint` at 78 was fully buried by both, which
means the one line in the game that names the inspect key has never been legible in Classic.

They are stacked upward off the taller deck now, so one set of numbers is right in both modes:
Classic's 124 is the floor, the hint takes 132 to 150, the prompt plate 156 to 190.

### 46.2 The objective line was in the LATA DOWN band

`ReadyObjective` at -206 with a top pivot spanned **206 to 244**. `LataDownAlert` is at -228 and
70 tall, so it owns **228 to 298**, and `ToastLabel` owns 160 to 204. Three transient banners
sharing one 140 px strip at the top of the frame. The objective moved to **-308**, which is 10 px
of daylight below where LATA DOWN ends, still in the top third and nowhere near the countdown.

### 46.3 It now retires itself after 7 seconds

`Hud.ObjectiveVisibleSeconds`. The window is reset on the false-to-true edge of
`ReadyGate.ReadyPromptChanged`, which fires once per phase transition rather than per frame, so
the line gets its full seven seconds every time the gate opens.

⚠ **The practice prompt deliberately does NOT expire with it.** "Press [R] when ready" is
the only way out of the practice window, and a player who spends a minute in there must still be
able to find the key. The coaching goes, the instruction stays.

⚠ **The tick is its own method rather than a call back into `RefreshObjective`.** That
method calls `GodotTheme.Box`, which allocates a sprite. `CLAUDE.md` § 7.1 records a HUD string
rebuilt every frame costing the probe an eighth of its frames, and a sprite is worse than a
string. `UpdateReadyObjective` flips two `enabled` flags.

### 46.4 Found while measuring this, NOT fixed: "YOU ARE VULNERABLE" is behind the deck too

`VulnerableWarning` is placed at y 84 and is 40 tall, so it draws from **84 to 124**. That is
inside the hero deck (14 to 92) by 8 px and inside the Classic deck (24 to 124) **completely**.
The one line that means "you are about to lose five seconds" is painted over by the Classic
deck's wooden plate for its whole life.

It is left alone here because it was not reported and because it is not obvious where it should
go instead: 46.1 has just filled 132 to 190 with the practice stack, and the two are never on
screen together, so it could take the same band. Done looks like the warning legible at both deck
heights, with the numbers written down the way 46.1's are.

---

## 47 · `Checks.RunAll` has been red since the Phaister merge, in two places

Found while verifying § 24 against `67f88aa`, which is the tip of `feat/ilalim-ng-tulay-map`.
**Neither of these is caused by anything in § 24 and neither is fixed there**, because a HUD
placement branch is the wrong place for a roster constant and a sound file. Both are two-line
jobs for whoever picks them up.

⚠⚠ **The point is not the two bugs, it is that the project's one-launch verification command
has been failing and the failure has been carried.** `RunAll` prints
`RESULT: FAILED. headless, audio cues.` at the end of every pass, so the next person to run it
learns nothing from a red result.

### 47.1 `HeadlessCheck` still counts five heroes

`Assets/TumbangPreso/Editor/HeadlessCheck.cs:50-51` asserts `Roster.HeroPeople.Count == 5` and
`Roster.AllPeople.Count == 17`. Phaister made them **6 and 18** (`Roster.cs:95` and `:109`), so
both lines fail on every run.

§ 21.1 item 3 caught exactly this in the EditMode suite and updated
`GameMode_Rosters_AreDistinctAndCorrectSizes` to six, **and this second copy of the same
assertion was missed.** § 21 also notes the docs still enumerate five heroes; that is prose, this
is a red check.

Done looks like: 5 becomes 6 and 17 becomes 18, left as typed literals rather than derived from
`Roster` — § 21.1 already recorded the reasoning, which is that a hero appearing or disappearing
is a product decision and should have to be typed.

### 47.2 Phaister fires a cue that does not exist

`AudioCueCheck`: *"UNDECLARED: PhaisterHeroKit.cs fires 'sfx_ghost_appear', which is in no cue
list, so it plays silence."* 69 files on disk, 75 live cues declared, and this one reaches
neither.

⚠ **This is § 20 again, one hero later.** § 20 is "Cheska's kit played the wrong sounds, and
every zone died in silence". The sixth kit arrived with the same class of hole, which suggests
the check is doing its job and the merge checklist is not.

Done looks like: either the cue is declared and a file exists for it, or the call site is changed
to a cue that does. Whichever it is, `RunAll` comes back OK for audio.

---

## 48 · Kuro's projected body deleted itself mid-ability, and took Nemu's way home with it

🧑, 2026-08-27, off a gameplay frame: *"dont make nemu pet aura disappear (purple light) until
she comes back"*.

He is describing the aura. The aura was the symptom.

### 48.1 Two clocks that nothing kept in step

Astral Projection (`NemuHeroKit.GhostlyPoltergeistAbility`) runs for **6.0 s** and ends by
teleporting Nemu onto whatever body she projected. For a seat with a live
`Visual.GhostPetCompanion` that body is Kuro and the possession path is correct. For a seat
WITHOUT one, `HeroHazards.SpawnGhostPoltergeist` builds a stand-in, and that stand-in carried
`private float _lifetime = 4.0f`.

⚠️⚠️ **So the ghost destroyed itself two seconds before the ability that owned it ended.** The
purple `GhostLight` went out, which is what is visible, and then `OnEnd` ran:

```
else if (_projectedGhost != null)   // null. Nothing happens at all.
```

**Nemu was never teleported.** She finished the cast standing exactly where she started, with no
trip, no sound and no explanation, on every run that reached 4.0 s. The aura going out was the
player watching the return anchor be deleted.

### 48.2 And the faster path was worse

The haunt branch called `Object.Destroy(gameObject)` the moment the ghost reached a victim. A
ghost that found somebody **half a second** after being cast deleted itself, and Nemu's route home
with it, five and a half seconds early.

### 48.3 The fix, in the shape the repo already uses for this

- `SpawnGhostPoltergeist` takes `lifetime` and the component exposes `Lifetime`. The literal is
  gone from beside a duration it had to agree with, which is the same rule as
  *never hard-code a distance beside a speed*.
- `GhostlyPoltergeistAbility` passes `Duration + ProjectionOutlivesAbilityBy` (0.5 s). The only
  property that margin needs is to be greater than zero: it makes `OnEnd` the thing that removes
  the ghost in **every** run instead of a race between two clocks.
- The haunt sets `_haunted` instead of destroying, lands once, and then holds station. The body
  stays where the player just watched it connect, which is where they are looking anyway, and
  stays the place Nemu returns to.

⚠️ **The possession path was already correct and is untouched.** `GhostPetCompanion` builds
`GhostPossessLight` in `BeginPossession` and destroys it in `EndPossession`, which runs after the
teleport: the light is lit for exactly as long as she is away.

### 48.4 Verified

Core 69/69, EditMode 124/124, PlayMode 66/66. `InputMapAndAbilityTests` exercises the full
reactivation lifecycle for every kit, including the EditMode branch of `OnEnd` that moves the
transform directly.

⚠️ **What is NOT verified is the 4.0 s case in a live match**, because the fallback path only
runs for a seat with no companion and the probes give every Nemu one. If you see her fail to
return in play, this entry is the first place to look and `_projectedGhost == null` is the thing
to check.

---

## 49 · Seat 0 travels about half what seats 1 to 3 do, in Classic, every run

**Found on 2026-08-27 while verifying § 48, not looked for.** `BotBehaviourProbe`'s
`ClassicBotsPlayAWholeMatch` went red on the distance floor and passed on an immediate re-run
with nothing changed, which is § 16's signature. It is not § 16.

| Run | seat 0 | seat 1 | seat 2 | seat 3 |
|---|---|---|---|---|
| red | **140.1 m**, score 1050 | 418.2 m, 1955 | 408.6 m, 2015 | 394.1 m, 1860 |
| green | **241.7 m**, score 1280 | 476.5 m, 2815 | 564.4 m, 3175 | 573.9 m, 2610 |

⚠️⚠️ **SEAT 0 IS THE LOWEST ON BOTH AXES IN BOTH RUNS, BY ROUGHLY A FACTOR OF TWO.** That is not
a noise floor; § 16's noise is a spread the seats share, and this is one seat apart from the
other three in a fixed direction. The floor's own note at `BotBehaviourProbe.cs:655` says the
observed Classic spread is **460 to 1190 m**, and seat 0 has now been seen at 140 and at 242,
under the bottom of that range twice running.

⚠️ **THE FLOOR IS THE SYMPTOM, NOT THE BUG, SO DO NOT LOWER IT.** 150 m was chosen to sit
"comfortably under anything a playing bot does", and a seat that keeps arriving near it is
telling us something about seat 0 rather than about the number.

**Where to start.** Seat 0 is the taya in round 1 (`(round - 1) % 4`), and it is the seat
`GameLaunch.SoloSeat` used to park a human on before `GameLaunch.AllBots` landed (§ 11). Both
make seat 0 the one seat with a history of being special, and § 11's note is that every probe
report from before 2026-08-26 was measuring a seat that could not play. The question is whether
something still treats slot 0 differently: a spawn point, an input reader that is still bound, or
an `AIController` that is added later on that seat than on the others.

**Done looks like:** four seats whose travel figures are drawn from the same distribution across
three runs, and a `ClassicBotsPlayAWholeMatch` that does not go red one run in three.

⚠️ **Not caused by § 45 or § 48.** Both are Hero Strike and HUD work; this is Classic, which
has no kits (`kits seen False` in the same report) and does not draw an ability deck. Two full
PlayMode runs with § 45 alone were green, which is consistent with an intermittent that predates
both.

---

## 50 · Fourteen reports off the 4.73 player ✅ CLOSED 2026-08-27

Everything 🧑 raised in one sitting after playing the `integration/ui-batch-on-ilalim` build.
Grouped by what they turned out to be rather than by the order they arrived, because four of
them are the same fault.

### 50.1 ✅ One key press was casting an ability TWICE, and that is Nemu's E

🧑: *"her e is kind of bugged, sometimes it doesnt cast sometimes it does? idk why"*.
**Sometimes is exactly right and it is a frame-rate bug.**

`InputIntent.JustPressed` is a diff against a snapshot taken by `CharacterMotor` **at the end of
the physics step**, and `HeroAbilitySystem.Aim` runs in `Update`. Above 50 fps there are two or
more Updates per FixedUpdate, so one physical press reads as `JustPressed` on every Update until
the next physics step takes a snapshot.

⚠️⚠️ **For twelve of the fifteen powers that is a harmless re-buffer of the same press. For the
one `CanReactivate` power in the game it is the whole bug:** `HeroKit.Fire` ACTIVATES on the
first read and REACTIVATES on the second, so Kuro went out and came straight home inside a single
click and nothing appeared to happen. Whether a FixedUpdate landed between two Updates is a
function of machine load, which is why the identical click worked or did not.

`HeroAbilitySystem` keeps its own per-slot edge now (`_keyWasDown`). It also fixes the hold:
`_heldSince` was being rewritten on every Update the stale edge was still true, so every
hold-to-aim lost up to a physics step of reach.

**Verified:** EditMode 143/143, and the possession is one press out, one press back at any frame
rate because the edge no longer depends on one.

### 50.2 ✅ Nemu's second skill shows one charge, because it has one

🧑: *"why does nemu have 2 charges if its just recast? should just show 1"*. § 45 answered this
in the HUD by hiding the pip row *while the power is out*, which left two pips on the tile for
the rest of the round: the reading he objected to. `NemuHeroKit` carries `charges: 1` now. It is
the only power in the game whose SECOND press of the same key is part of the same cast, so two
pips were describing an allowance the ability does not have.

⚠️ The reactivation is still free and `HeroKit.Fire` still does not gate it on readiness. At one
charge that stopped being a nicety: it is the only thing between the player and a possession they
cannot leave.

### 50.3 ✅ Nemu's sleeves phase through themselves, in first person and in third

🧑: *"the arms of Nemu her sleeves are phasing and looks weird ... maybe js remove the physics on
her sleeves bcz it looks so ugly, js show me cute blocky sleeves"*.

⚠️⚠️ **The fault was structural rather than a tuning value, and both solvers had it.**
`ViewmodelClothPhysics` instanced the sleeve mesh and pushed every vertex by a weighted rotation
plus an offset plus a sine ripple, up to 0.12 m and 35 degrees. `BuildNemuAccessories` builds the
sleeve, the inner lining and the lavender cuff rim as **three separate meshes occupying the same
volume**, and only ONE of them was being deformed. No damping ratio fixes that; the surfaces are
not solved together and cannot be. `BaggyClothingPhysics` was the same shape on the body: it
post-multiplied up to 6 degrees onto `arm-left` and `arm-right` after the animator had written
them, under only one of the three sets.

Both files are deleted, with `BaggyPhysicsProbe`. `HeroPresentationTests` asserts the ABSENCE
now, by checking the sleeve still holds its shared mesh rather than an instanced `_Deformed` copy,
so a solver reintroduced under another name fails a test.

### 50.4 ✅ Invert Y and Fullscreen could not be clicked at all

🧑, with a screenshot of both rows. Same bug as § 32.3's sliders, one control across:
`TscnUiImporter.BuildCheckBox` puts the tick box on a CHILD node and points `Toggle.targetGraphic`
at it, so the Toggle's own GameObject carries no `Graphic` and the row has no hit area.

⚠️⚠️ **And the importer-side fix cannot reach a shipped scene.** `ClearStrayRaycastTargets` keeps
a Selectable's `targetGraphic` alive, but that runs at IMPORT time and writes a `.unity` asset;
**running the player never re-runs the converter**, which is why `ConvertedSettingsPanel` calls
`MenuKit.EnsureHitArea` on the sliders at runtime. There is a `Toggle` overload now and the panel
calls it. The hit area is the WHOLE ROW, not the 30 px box.

### 50.5 ✅ Phaister's aim ring read as a shadow, and everybody could see it

🧑: *"I dont want Phaister's E HOLD for casting To just be a shadow, keep that outline and give it
her color so that it could be seen more, make sure only she can see it"*. Two things:

* **Colour.** The held ring took `UiTheme.ColorForHero`, which is the accent picked against cream
  UI. `HeroWitch` is `e828c5`: saturated, mid-VALUE. Ghosted geometry is lit by its emission and
  almost nothing else, and Ilalim ng Tulay is a street under a viaduct, so a mid-value colour
  there is that colour's silhouette. `UiTheme.BrightForHero` is new and the HELD ring takes it;
  the post-cast `Flash` keeps the base accent, because it is read over a lit explosion rather
  than off bare asphalt. `Logs/shots-abilities/ability_blink_aim_reticle_eye_v36.png` is the
  frame, and it is the first time this telegraph has ever been photographed.
* **Privacy.** An aim is a decision that has NOT been made yet, so painting it on the road told
  the other three where somebody was about to teleport before they committed. That is strictly
  worse than no telegraph: it gives away the one thing a hold-to-aim power buys. `GroundReticle`
  asks `CameraRig.IsFollowing` and draws the held ring for the driven body only. **`Flash` is
  deliberately unchanged and stays visible to everybody**: by then the power has landed and
  "where did that go off" is a question all four players need answered.

### 50.6 ✅ The AI thinks before it casts, and holds an ultimate for a better moment

🧑: *"try to make it so that AI think or pretend to think when to use skills bcz they all js spam
it at the same time bru at thhe start"*, then *"Make sure u actually make ai better/ smarter with
skill usage"*. Three changes, and the second and third are the "smarter" half.

1. **The opening gate is per seat.** § 31.7 added `AbilityOpeningDelaySeconds` 2.5, ONE constant
   four bots share, so all four unlocked on the same frame and the frame-one dump became a
   frame-150 dump. `AiPersonalityRoll.Patience` is a new deterministic per-seat roll and
   `AbilityOpeningJitterSeconds` is 4.0, so the four openings spread across 2.5 to 6.5 s.
2. **A conviction window.** `AIController.Consider` requires the SAME slot to still be worth
   casting for 0.25 to 0.85 s CONTINUOUSLY (scaled by `Tempo`) before it presses, and drops the
   whole thing the moment the reason stops holding. That is the difference between hesitating and
   being slow: a target who steps out of a footprint mid-window is not chased by a press that was
   already committed, and a bot can no longer weigh Skill1 then Skill2 and fire both.
3. **The ultimate waits for a window.** Nothing used to ask whether a cast was worth the METER,
   only whether it would land, so a bot spent its most expensive power on the first single body
   to wander into the circle on the frame the meter filled. It wants
   `AiTuning.UltimateWantsVictims` (2) under the footprint, with two unconditional escapes so it
   can never become hoarding (`docs/VISION.md` § 4): 14 s of patience, or the last 12 s of the
   round.

Cadence is 2.0 s plus a rolled 0 to 1.5 rather than a flat 1.6, so two bots that fire together
drift apart instead of staying locked.

### 50.7 ✅ The pektus curve is Z and C

🧑: *"rebind pektus to keyboard keys that are close to wasd bcz its so hard to touch the arrow
keys and some keyboards dont have it"*. Both halves are real: the curve is held WHILE the throw
charges on the left mouse button and WHILE moving on WASD, so it is the one input that must
overlap the movement hand, and 60 per cent boards do not carry the arrows at all. `Grab` already
holds X, so the bottom row reads curve-left, contextual, curve-right.

⚠️ C is also `SpectatorControls`, which `Rebinding.FindDuplicateBindings` allows because they are
in different contexts (§ 35.3). Both rows stay rebindable in PLAYING THE GAME.

### 50.8 ✅ The tutorial taught keys that were not bound, in two different ways

🧑: *"make it so that tutorial shows the actual keys u rebinded to and arent just hardcoded"*.

⚠️⚠️ **And the literals were already wrong before anybody rebound anything.** `TutorialContent`
said pickup was `E` and the taya's lunge was `E · hold`; the shipped map has `Grab` on **X** and
`Lunge` on the **right mouse button**, and has since the one-control-one-action pass. The HOW TO
PLAY screen, which exists to teach the controls, was naming a key that does something else. That
is the quieter half of what a hard-coded chip costs: a literal cannot go stale loudly.

`TutorialContent.Row.Keyed` resolves chips AND bodies from the live map at draw time, and the
pektus curve is on that page now for the first time. Mouse look and ESC stay literal on purpose:
neither is in the input map, so there is nothing to ask and nothing to rebind.

### 50.9 ✅ The instant replay was starting itself, in a box, forever

🧑: *"why is instant replay just spam showing"*, *"i alsoo really dont like that instant replay on
the top right"*, *"i want it to cover whole screen if i click it and i dont want it to just loop
every second"*.

⚠️⚠️ **It was never looping.** `StepReplay` plays the clip once and ends. It fired on EVERY
scoring event behind a 4.0 s floor, and Hero Strike scores constantly: a knockdown, a tag and a
sabotage are three triggers, and `PollHighlights` added a fourth by watching the lata on top of
the `Scored` event reporting the same knockdown.

The self-start is **deleted**, not suppressed. `AutopilotSuppressesAutoReplay` had already
established that a camera must not replay by itself; the same argument (*"thats for human only"*)
applies to a human flying it by hand. The highlight reason survives as a LABEL so a manual replay
is titled `INSTANT REPLAY · TAG`. The overlay is full screen with an `AspectRatioFitter`, because
nothing is behind it to keep framing any more.

⚠️ `DeadFeatureAudit` used to pin the toast `LIVE PLAY CONTINUES`; the premise under that
assertion is gone, and it now greps for the two names of the self-start instead.

### 50.10 ✅ Spectator HUD: the YOU card did not hide, from any of three paths

🧑: *"fix all these spectator hud problems wtf some shit dont hide"*, with a card and a stamina
bar in the corner of a watcher's screen. `YouCard.Build` makes its own ROOT canvas, so
`Hud.SetCleanFeed` (which disables `_canvas`), `Hud.EnterSpectatorMode` (which strips the HUD's
own children) and `MatchHost.EnterSpectatorMode` (which deactivates the HUD object) all missed it.

⚠️⚠️ **Reparenting it under the HUD was tried and reverted the same hour.** A NESTED Canvas
ignores its own `CanvasScaler`, so the card lost `AspectSafeCanvas` and its fixed 380 x 132 rect
stopped being anchored to a screen-sized parent: `HudOverflowProbe` found the identity row
**274 units off the right edge at all nine resolutions**. That is § 18.1b's two-canvas hazard from
the other direction. All three paths sweep for the component by type instead, and
`MatchInstaller` does not build it for a watcher at all. **A fourth way to hide the HUD has to
add this card to it.**

`RoleSwapCard` genuinely is nested and its canvas RectTransform is stretched to its parent now; a
child Canvas's rect is not driven the way a root one's is, and that card only survived because
its backdrop and its column are both stretched or centred.

### 50.11 ✅ The YOU card's identity row drew over itself

🧑, with a screenshot reading `TAYA (DEFENDEDANTE`. The row is a `HorizontalLayoutGroup` with two
`flexibleWidth: 1` children both on `HorizontalWrapMode.Overflow`, so a pair too wide for the
336 px content box does not shrink, it overlaps. Two fixes, because shortening the string alone
would only have made the collision rarer:

* The gloss is gone. `TAYA`, not `TAYA (DEFENDER)`. `TutorialContent`'s premise strip is where a
  player meets the word, and every other in-match readout already says the bare one.
* The name is `resizeTextForBestFit` down to `MenuKit.MinReadableUnits`, so a long PLAYER-TYPED
  name shrinks rather than colliding. `Balance.PlayerNameMax` allows more characters than the row
  can hold at 34 pt however short the role word is.

### 50.12 ✅ One trip hazard on Ilalim ng Tulay, down from two

🧑: *"lessen trip areas in map, maybe js one is okay, its overstimulating to have allat"*. Seven
to four (2026-08-26), four to two (§ 45), two to one. Three cuts for the same reported feeling is
the tell that the map had one hazard's worth of design in it.

The cord stays because it is the only one attached to a business that is already on the street:
`BuildPisonetRow` authors three terminals, three chairs and a cable running to them.
`TripHazard_GpuBoxDebris` was cardboard drawn for the hazard's own sake, on a corner nothing else
happens on. The distance rule from § 45 still binds anything added back: the cord is 8.55 m from
the can against a `CONFINEMENT_RADIUS` of 7.0.

### 50.13 ✅ The floating COMPUTER PARTS text is deleted, and fitting it was the wrong fix

🧑: *"flowing computer parts text pls remove"*, with a screenshot of the same wall he reported on
2026-08-25 (*"floating texg here pls remove"*), which was answered by `FitToFacade`.

⚠️⚠️ **The second report is the proof that constraining the rect never addressed it.**
`StreetSignKit.PaintedWall` draws LOOSE CAPITALS AND NO PLATE by construction: every letter is its
own geometry standing a few centimetres off a wall with nothing behind it. On a stepped voxel
facade under a viaduct, at any angle but straight on, that reads as text hanging in the air
whether or not it is inside the wall's bounds. `Sign_PcRepair` and PC Express's own fascia already
say what that row sells, and both are carried by real geometry. `PaintedWall` stays in the kit; it
is correct on a flat plastered wall.

**And the pisonet fascia's wall plane is solved from `Shophouse_E3` now** (🧑: *"as well as the
pisonet sign"*). `10.94` was a literal, and `BuildSideFacade` gives every shophouse a
per-instance setback, so a typed x is only correct until somebody moves the building. Same two
calls the west side has had since 2026-08-25: `ShopFaceX` then `FitToFacade`.
`Logs/shots-ilalim/ilalim_street_life_v23.png` is the frame.

### 50.14 ✅ The overstimulation pass: nine words deleted, none of them an effect

🧑, three times in one sitting: *"lessen the words showing up on screen, game feels
overstimulating"*, *"do not touch effect and abilities"*, *"js remove some of the words that pop
up bcz its so confusing to process when theres 5 words popping up at the same second"*, and
*"only touches AI, HUD AND UI"*. Nothing below changes a particle, a light, a sound or a number.

⚠️⚠️ **The rule the cuts follow, so it is not decided case by case: a surface may say a thing
once. A repeat is either the same fact on a second surface, or the same fact again on a clock.**

| What | Was | Now |
|---|---|---|
| Slipper-idle penalty | a world callout, a `-5 SLIPPER IDLE` toast AND the lata card line, **every second** | the card line, which is the only STATE of the three |
| Taya camping penalty | identical shape, also every second | the card line |
| `RETRIEVE NOW` centre alert | `enabled = !upright`, so up for most of a live round in 42 pt | 2.2 s on the knockdown edge |
| Round line | `ROUND 1 / 8 · DEFENDER: DANTE` for 90 s | names the taya for 6 s, then `ROUND 1 / 8` |
| Nameplates | `· ATK` over three of the four bodies | the taya's word only; the other three are attackers by definition |
| `PEKTUS!` / `FIREBALL!` / `OVERCHARGE!` | one per throw, 127 to 173 throws a match | nothing. `ComicPopup`'s own rule is A CAST GETS NO WORD |
| `SABLAY!` near miss | on a 1.35 m threshold, so most misses | nothing. The player is looking straight at it |
| `PROTECTED!` | on every pulse, beside a live countdown saying the same | nothing |
| `LATA IS BACK UP` | subscribed **twice**, in `Hud` and in `MatchInstaller` | one owner |
| `TAG · <name>` | raced `+100 TAG` for the same label on the same frame | the score toast, which is deterministic |

⚠️ **The hitmarkers, the cues and the award sting all survive.** A player being penalised still
hears and feels it once a second; what went is the reading. That is the distinction he drew:
*"do not touch effect and abilities ... js remove some of the words"*.

⚠️ **Two toasts that look like candidates and are not.** The victim's `TAGGED · BACK TO THE SAFE
ZONE` is the only thing on their screen explaining why they cannot move (the `TAGGED!` callout
spawns inside their own head in first person), and `OUT OF BOUNDS` explains a teleport that has no
animation and no sound.

---

## 51 · The four follow-ups off § 50 ✅ CLOSED 2026-08-27

Raised while § 50 was still being verified, so they are their own entry rather than more
sub-sections of it.

### 51.1 ✅ Nemu's first-person sleeve is her sleeve now, not just a still one

🧑, after the cloth solver came out: *"did u replace nemu's sleeves with something that looks
like sleeves of her 3d model?"*, having asked for *"cute blocky sleeves"*.

⚠️⚠️ **Deleting the solver stopped the phasing and left the wrong SHAPE standing.** The
viewmodel carried three lofted 24-segment tubes that flared toward the cuff with a lavender RIM
around the opening. `Logs/model-ref-nemu.png` disagrees on every point: her arms are **straight
plum boxes**, they do not flare, and the lavender is a **vertical bar down the outer edge**. It
was a different garment in the right two colours.

⚠️ **In first person the sleeve is most of the screen**, so it is the piece of her a player looks
at longest and the one that most has to be her. It is `AddBoxAccessory` calls now, the same
construction Cheska's and Phaister's arms already use; Nemu was the only hero carrying bespoke
lofted geometry and three mesh builders. The stripe is on BOTH side faces because
`RightBasisX` and `LeftBasisX` are rotated frames rather than mirrored scales, so a single
stripe would be correct on one arm and inside the other.

`HeroPresentationTests` pins the vertex count at 32 or under, so a loft cannot come back
quietly. `Logs/shots-fpp/fpp_nemu_holding_v3.png` is the frame; that tool versions its
filenames now, which it never did.

### 51.2 ✅ The road keeps the props, it just stops tripping you over them

🧑: *"if u removed the trip shit can u atleast keep the models that was in play area before? js
delete the trip mechanic on them, bcz i dontw ant play area to look empty"*. He is right, and all
three earlier cuts made the same mistake: **they deleted the OBJECT to delete the RULE.**

The open manhole with its rim tipped up, the settled trench and the dropped GPU boxes are eleven
pieces of authored geometry drawn to `docs/VISION.md` § 2 rule 3, on the one part of this map
with nothing else on it. `BuildFormerHazardDressing` rebuilds all three through the SAME
`BuildTripHazardVisual` the hazards used, with no `BoxCollider`, no `StreetTripHazard` and a home
under `Kalat` rather than `Hazards`.

⚠️ **Nothing avoids them any more, and that is the point.** The bots' hazard avoidance walks
`StreetTripHazard` components, so a bot now walks over the manhole exactly as a player does. A
prop that still bent bot routes would be a hazard wearing a different name.

### 51.3 ✅ Four real changes to how a bot decides, not four constants

🧑: *"i asked u to thoroughly improve ai logic not just adjust some values"*, and then
*"i dont want them to use all skills consecutively"* and *"i want it to be possible too for them
to not use some skills at all if they cant find opportunity bcz thats normal and human"*.

**One runner at a time.** `FetchIsSafe` asks *"is the box safe for me"*, and every one of its
escapes is a fact about the WORLD rather than the asker: the can is down, the taya spent their
lunge, the taya is far from the shoe. Three attackers therefore agreed, entered the chalk
together, and handed the taya the easiest round of their life. It is also not how the game is
played: `docs/VISION.md` § 0 says *"the tension is the retrieval"*, and a retrieval is tense
because ONE person is exposed. `IHaveTheBestRun` compares head starts (`taya-to-shoe` minus
`me-to-shoe`) and yields to a clearly better-placed rival. **Derived, with no shared state**:
every bot runs the identical comparison over the identical board, exactly as `ClaimSlack`
already does, so there is no channel between bots that a human is not on.

**A taya gives up a chase that is going nowhere.** `DoHunt` closed every frame and nothing asked
whether it was working, so an attacker who was simply faster could walk the taya to the far end
of Aurora Boulevard with the can undefended and the passive score stopped.
`ChaseIsGoingSomewhere` measures against the CLOSEST the chase has ever been, not last tick, so a
quarry who jinks does not read as progress. A helpless quarry is never abandoned.

**Nobody throws at a can that is already going over.** Four seats had no notion of each other, so
a knockdown was routinely followed by one or two more releases inside the same second:
`CanThrow` refused them the moment it landed, and those bots spent a full charge and their
tsinelas for nothing. `RivalShotIsInbound` walks the arc with the same gravity and step
`TryInterceptPoint` uses and asks the game's own knockdown window,
`SlipperHitRadius + LataHitMargin`. It only ever delays; the bot keeps working its angle.

**Sean's and Zack's throw buffs need a shot to buff.** These two were the one place a bot spent a
power with no opportunity test at all: *"holding a tsinelas and the throw is legal"* is true of
almost every second an attacker is alive, so both armed on cooldown rather than on a chance.
`ArmingThisShotIsWorthIt` wants the bot planted or on its mark, a clear lane by `LaneBlocked`,
and no rival shoe inbound.

**And the kit is no longer played as one burst.** `AbilityCadenceSeconds` spaced any two presses
and did nothing about Q, then E, then the ultimate over six seconds.
`AbilityChainSeconds` (5.5 s plus jitter) prices a DIFFERENT slot higher than the same one: a
genuine combo still lands, it just has to be worth waiting out.

**A bot may now finish a round without using a power.** `AiPersonalityRoll.SkillAppetite` is a
per-seat, per-slot eagerness that scales the conviction window between 0.7x and 1.9x. ⚠️ **It
lengthens the window, it does not roll a die and refuse a chance the bot saw** (which reads as
broken): a shy bot wants a longer unbroken reason, so a marginal window passes it by and a clear
one is still taken. Whether a slot goes unused is then decided by the BOARD.

⚠️⚠️ **THE SHY END WAS 2.6 AND THAT OVERSHOT, MEASURED RATHER THAN FELT.** `BotBehaviourProbe`
over a whole eight-round Hero Strike match on Eskinita:

| | skills | ultimates | throws | knocks | tags |
|---|---|---|---|---|---|
| shy 2.6 | 27 | 15 | 205 | 88 | 118 |
| **shy 1.9** | **34** | **17** | **209** | **104** | **120** |

27 casts is 1.3 per seat per round. The complaint being answered was § 19's *"44 to 56 casts in a
90 s round"*, and a thirtyfold cut lands in a mode whose whole reason to exist is the kits
(`docs/VISION.md` § 1). At 1.9 the match is livelier on every axis and still nowhere near the
pile-up. ⚠️ **Read § 16 before quoting either row as a comparison**: at n = 1 these are liveness
floors, and the honest reading is the direction rather than the digits.

### 51.4 ✅ Two more events with two owners each

Found chasing 🧑's *"shows -5 slipper idle twice bruh"*. `RoundDirector.Tagged` had subscribers
in BOTH `Hud` and `MatchInstaller`, writing the same two strings into the same label on the same
frame, in whichever order Unity's delegate list happened to hold. `LataRestored` had it too.
Both installer copies are deleted; the HUD is the owner, because the decision about who gets told
what is a HUD decision and is written down there.

⚠️ **`MatchInstaller._tagged` going away does not touch tagging.** That field held a TOAST
handler. The tag itself is `CombatVerbs` and `RoundDirector`, and neither was touched.

**Do not add a toast to the installer. Wire the event and let the HUD say it.**

### 51.5 ✅ Two faults the AI work introduced, both caught by PlayMode

⚠️⚠️ **`IHaveTheBestRun` DEADLOCKED EVERY ATTACKER AND `BotMotionProbe` PRINTED IT.** Seat 3
covered **0.94 m in six seconds** of a live round against a 1.0 m floor, with `plan=Stalk` and
`axis=(0.00, 0.00)` on nearly every sample.

The first pass compared pairwise: *is theirs better by more than the margin, or inside the margin
with a lower seat*. **That is not transitive.** Odds of 5.0 (seat 0), 5.5 (seat 1) and 6.1
(seat 2) at a 0.75 m margin make all three yield: seat 0 loses outright to seat 2, seat 1 loses
the tiebreak to seat 0, and seat 2 loses the tiebreak to seat 1. Nobody runs until the tournament
clock breaks it, which is a worse failure than the pile-up the rule exists to stop.

`RunRank` quantises the odds to whole margins and subtracts the seat, so "who has the best run"
is a **total order** and exactly one candidate holds the maximum at any instant. A deadband
applied pairwise cannot give you that, however it is written.

⚠️⚠️ **AND `AiPlan.Stalk` WAS A STATUE.** Its own comment claimed it *"keeps the bot MOVING"*;
the probe disagreed. It walked to the ring point on the bearing of its own tsinelas, arrived, and
`Loiter` is a small shuffle with rest periods. It is also the wrong place to wait: the bearing of
your own shoe is the bearing the taya is already guarding.

A stalker slides around the box away from the defender now, and 🧑's *"make sure ai actually
moving and it moves like human"* is the standard it is measured against. Two things had to be
right before it read as a person rather than as a pinball:

* **The side is chosen off `AiPersonalityRoll.HomeBearing`, which does not move.** Taking it off
  the bot's own current bearing meant every step changed the direction it wanted to step next: a
  chattering sign, and the probe caught seat 2 walking **16.35 m in six seconds**, from x = -3.39
  to x = +7.45, straight across the arena.
* **The wait is anchored on that same corner, pulled 0.55 toward the shoe.** At a pure shoe
  bearing both stalkers finished a metre apart in the same corner, which is the pile-up moved
  from the box to the ring. `HomeBearing` already exists to stop exactly that and the first pass
  ignored it.

Measured after: the two attackers take opposite sides, **7.97 m and 10.29 m in six seconds**, and
then hold their post with small adjustments.

⚠️ **`AnyAttackerCanPickUpAnySlipper` was measuring how far a bot walked.** It drops a slipper at
an attacker's feet and holds Grab for twenty steps, on a seat an `AIController` is also driving.
Two faults in one: the bot's `Update` writes `Grab = false` on every frame its plan does not want
it, and a walking bot carries the body out of `Balance.PickupRadius` while the loop runs, so the
failure reads as *"the press never arrived"* when the press arrived at a character who had left.
It passed for as long as the planner happened to choose `Fetch` for that seat. The test disables
the seat's bot **before positioning anything** now. A test that drives an intent has to own the
intent and the body.

---

## 52 · The ready and rematch gates counted a seat as a peer, and five guards allocated before they guarded

Two halves of one session, 2026-08-27. The first is a networking fault that made a two-human
match unstartable; the second is a measured allocation pass with a probe behind every number.

### 52.1 ✅ FIXED: ready and rematch mixed transport peer ids with seat ids

**The fault.** `ReadyGate.DeclareReady` and `Core.RematchVote.Add` both opened with
`if (peerId == 0) peerId = hostPeerId;`. The belief behind it is recorded in § 1 above, in
`RematchVote`'s header, in `MatchRpc`'s and in a `Core.Tests` case: that a sender id of 0 was a
Godot placeholder the host had to translate into its own identity.

**It is not a placeholder.** `NetworkManager.ServerClientId` is 0 and it is the host's real
transport id, which is why `LobbySession` already keys `_peers` by it and why
`SelectLobbyPickServerRpc` carries a ⚠️ note saying to take the host's peer id from
`LocalClientId` and never from `LocalSlot`. Remapping 0 to a SEAT put two namespaces in one set:

* a host sitting in **seat 1** wrote entry `1`;
* the client whose transport id is **1** also wrote entry `1`;
* the set held one entry for two peers, `ExpectedReadyCount` still wanted two, and **the ready
  countdown never opened**. The rematch gate failed identically from the result board.

**The fix is to stop naming yourself.** `DeclareReadyServerRpc` and `VoteRematchServerRpc` now
carry **no payload at all**: the host reads the sender id NGO authenticated at the door, and its
own press comes from `NetAuthority.LocalPeerId`. That is also the only value a client cannot lie
about, so it closes the smaller hole on the same line: a peer that could name itself in the
payload could ready somebody else. `RematchVote.Add(peerId)` lost its `hostPeerId` parameter,
`HasVoted` with it, and `NetSession.ProtocolVersion` went **2 to 3** because the wire changed.

**The same fault one call frame out, found while fixing it.** `LobbySession.PlayingPeerCount`
takes a peer id (it compares against `PeerRecord.PeerId`) and all three callers,
`ReadyGate.ExpectedReadyCount`, `MatchResult.ExpectedVotes` and the new `NetAutomationProbe`,
were handing it `NetAuthority.LocalSlot`. So a host in seat 1 forgave a spectating client 1, and
a spectating host in seat 1 was dropped from its own quorum. `NetAuthority.LocalPeerId` is new,
is on `INetProvider` so `SoloProvider` answers it offline, and is what all three pass now.

**Verified.** `Core` 91/91 with a rewritten case that asserts peer 0 and peer 1 are two voters;
EditMode 143/143; PlayMode 68/68; all three wire audits clean (40 ability sites, 25 host-gated,
**0 ungated on another body**; 40 request sites, **0 unreachable**; 42 payloads,
**0 mismatched**). `audit_wire_payloads.py` lost its two `ACCEPTED` waivers, because
`DeclareReady` and `VoteRematch` now genuinely write nothing and read nothing.

### 52.2 The two-process driver, and the deadlock in its first shape

`Assets/TumbangPreso/Runtime/Diagnostics/NetAutomationProbe.cs` presses the real controls so two
built players can verify each other without a person driving two sets of menus.

* `-tp-autostart 2` waits until the host can see two playing peers and then presses the same
  `DeclareReadyServerRpc` a keyboard does.
* `-tp-autorematch` presses `MatchResult.RequestRematch`, which is the REMATCH button's own
  handler.

⚠️⚠️ **ONLY THE HOST WAITS FOR THE PEER COUNT, AND THAT ASYMMETRY IS A FIX RATHER THAN A
SHORTCUT.** `LobbySession` is filled by the connection-approval path, which runs on the server, so
on a client the table is empty and `PlayingPeerCount` floors at 1 forever. Gating both processes
on it deadlocked the run outright: the client waited for a second peer it can never see while the
host waited for a press the client was never going to send. A client pressing early is safe
because the host opens `AwaitingNetReady` in `MatchInstaller.BuildReadyGate` as it loads the
arena, before any client finishes connecting, and `DeclareReady` is a set add.

### 52.3 ✅ MEASURED: the stable HUD tick allocated 952 B/frame and now allocates 100

`Assets/TumbangPreso/Tests/PlayMode/HudPerformanceProbe.cs` freezes gameplay with
`Time.timeScale = 0` while leaving the HUD's unscaled tick live, then compares 180 live frames
with `Hud.enabled` true against 180 with it false, off the engine's own `GC Allocated In Frame`
counter. The difference is what the HUD costs on a frame where **nothing is happening**, which is
exactly where a guard is supposed to pay nothing.

| | HUD active | HUD off | **attributable** | at 60 fps |
|---|---|---|---|---|
| Before | 1938.58 B | 986.29 B | **952.29 B/frame** | 57,137 B/s |
| After | 1164.04 B | 1063.84 B | **100.20 B/frame** | 6,012 B/s |

**An 89.5 per cent reduction, and not one displayed string, number, timing, score or decision
changed.** Evidence: `Logs/hud-perf-baseline4.xml` and `Logs/hud-frame-cost.txt`.

⚠️⚠️ **THE PATTERN IN ALL FIVE IS THE SAME: THE EXPENSIVE HALF RAN BEFORE THE GUARD COULD
REFUSE IT.** Four of the five already carried a comment saying the value was only written when it
changed, and every one of those comments was telling the truth about the ASSIGNMENT and not about
the work that produced the value.

1. **`Hud.UpdateScores` built its change-stamp with a `StringBuilder`.** It appended four scores
   and four names into a fresh string on every frame and then compared that string. It compares
   primitive score, name and occupancy snapshots now. The occupancy flag is not decoration: the
   old stamp could not tell an empty seat drawing its `P1` fallback from an occupant actually
   called `P1`.
2. **`Hud.UpdateTimer` formatted the round line on every frame.** Both `RoundLine` and
   `ShortRoundLine` are interpolated strings, and `FitTopCentre` guards on the FINISHED string, so
   the string had to exist before anything could decide it was unnecessary. The inputs are
   compared now, the defender's NAME among them, because a seat can change hands without the
   round number moving. ⚠️ The warm-up branch drops the cache, or `WARM UP` would stay on
   screen for the whole round.
3. **`Hud.UpdateIndicators` ran `FindObjectsByType<Slipper>` on every frame**, a whole-scene type
   scan plus a fresh array, to find one object that changes a handful of times a round. It is a
   validated cache now rather than a rate limit: the arrow is one of only two in-world markers
   that answer "what am I doing", so it may not be stale for even a tenth of a second.
   `UpdatePickupPrompt` had already refused to pay this; this row had simply never been looked at.
4. **`Hud.KeyLabel` re-resolved a key label on every frame it was on screen.** One label is
   `FindActionMap`, `FindAction`, `InputControlPath.ToHumanReadableString` and a
   `ToUpperInvariant`, the last two of which allocate. It is cached against a new
   `Settings.Rebinding.Revision`, bumped by the only three places a binding can change plus the
   settings panel's own intermediate override. ⚠️ Keyed on a revision rather than a timer,
   because `VISION.md` § 3 is explicit that a screen teaching the wrong key is worse than one
   teaching none. The asset-missing fallback is deliberately not cached.
5. **`CharacterMotor.DisplayName` allocated on every call**, via `ToUpperInvariant` or a seat
   interpolation, and the scoreboard asks all four seats every frame while each nameplate and the
   YOU card ask again. It compares its five inputs rather than being invalidated from the setters,
   deliberately: `_playerSlot`, `_characterIndex`, `_isBot` and `_playerName` are all
   `[SerializeField]` and written from several places including the seat-rebind path, and a cache
   cleared by hand in four setters is one future writer away from a body wearing somebody else's
   name. `MatchRpc.HostPeerLeft` flipping `IsBot` on a departed seat is exactly that case, and it
   is why the comparison list includes `Mode`: the two rosters are different people.

### 52.4 ✅ FIXED: the host re-scanned the whole scene for slippers 200 times a second

`MatchRpc.FindSlipper` was a bare `FindObjectsByType<Slipper>`, and `MatchRpc.FixedUpdate` calls
it for **all four seats on every physics step**. On a 50 Hz host that is 200 scene-wide type scans
and 200 arrays per second, for four objects created once per match, on the one code path that
only runs while somebody is actually connected. Same validated-cache shape as 52.3 item 3, and a
miss refills the whole table so a fresh arena costs one scan rather than four. The sweep keeps the
FIRST match per seat, which is what the loop it replaced returned.

⚠️ **Validated per call rather than refreshed on a timer**, because `BroadcastSlipperState` is
what every other peer draws that object from: a stale entry would be broadcast as truth.

### 52.5 Found and NOT fixed

* ⚠️⚠️ **`AIController` holds eleven `FindObjectsByType<Slipper>` calls in its decision
  helpers**, at lines 465, 755, 916, 937, 1951, 2494, 2822, 2905, 3187, 3203 and 3770. They sit in
  `RivalShotIsInbound`, `SlipperOwnedBy`, `MySlipper`, `TryInterceptPoint`, `ChooseSlipper`,
  `TryGlanceAt`, `TryCoverPoint`, `NearestFlyingSlipper`, `WorthDenying`,
  `AnyLooseSlipperInsideTheBox` and `HasRelevantVoidTarget`, several of which run more than once
  per decision tick, for up to four bots. **Deliberately left alone here**: the fix is the same
  shape as 52.3 item 3, but it is eleven sites inside the code that chooses what a bot does, and
  § 16 says `BotBehaviourProbe` spreads about 20 per cent run to run, so at n = 1 it cannot prove
  that changing the AI's lookups changed no decision. **Done looks like:** a `Slipper`-owned
  static registry maintained in `OnEnable`/`OnDisable` so the query is a field read, plus three
  runs an arm of `BotBehaviourProbe` on both modes showing throws, retrievals and tags inside
  § 16's noise floor.
* **The rest of the game allocates about 1,064 B/frame** on a frozen Hero Strike round with the
  HUD disabled entirely (`hud-frame-cost.txt`, "HUD off average"). That is the floor every HUD
  number above is measured against, it is ten times what the HUD now costs, and **nobody has ever
  looked at what is in it.** `HudPerformanceProbe` already reports it on every run, so the next
  pass starts with a number rather than with a search.
* ⚠️⚠️ **TWO REAL MACHINES ON A LAN HAVE STILL NOT PLAYED THIS**, which is § 38.20's standing
  note and is not closed by anything here. `NetAutomationProbe` runs two processes on one desktop,
  which shares a clock, a filesystem and a loopback with itself.


---

## 53 · A joining client could not move, and the cause is that its keyboard was left on seat 0

🧑 2026-08-27, on the shipped build and on a two-process run: *"why was everyone just stuck"*,
*"there was zero movement for anyone"*, *"its been reported too by playtesters that multiplayer
allowed for 0 movement whatsoever"*. This is that, and it is one component that never moved.

### 53.1 ✅ FIXED: `MatchInstaller.RebindLocalSeat` moved everything except the input reader

**The ordering that makes it bite every client.** `NetBootstrap` and the menus both call
`StartClient` and then load the arena, so `MatchInstaller` builds its seats while
`NetSession.LocalSlot` is still its **default 0**. `HumanSeat` therefore answers 0 on every
joining client, and `BuildSeat(0)` bolts the `PlayerInputReader` to **seat 0**. The seat the peer
is actually given gets no input source at all.

**The correction was incomplete.** The host's `Seating` message lands in
`MatchRpc.OnSeatingMsg`, which calls `MatchInstaller.RebindLocalSeat`. That moved the CAMERA, the
HUD and the READY GATE onto the real seat and stopped there. So everything a joining player could
SEE moved to seat 1 and everything they could DO stayed on seat 0.

**What that produces is exactly the report:**

* the camera watches seat 1, which has no input source on this machine, so it never moves and
  never submits a transform. `HostLateJoin` has meanwhile destroyed the host's `AIController` on
  that seat, correctly, so **nobody drives it and it is frozen on every screen**;
* the keys drive seat 0, which the host owns and overwrites with `SyncUnit` every physics step,
  so it twitches and snaps back;
* `CharacterMotor.StepNetworkTransform` submits only `_playerSlot == NetAuthority.LocalSlot`,
  which is now 1, so the body being driven is never sent upstream either.

⚠️⚠️ **THERE ARE TWO SEAT-ASSIGNMENT PATHS AND ONLY THE OTHER ONE WAS COMPLETE.**
`MatchRpc.ApplyRebindLocalSeat` (the `RebindSeat` message, sent on a late join or a reconnect)
moves the reader, the YOU card, the pause watcher and the slipper owner glow, and even carries a
note saying it owns the input. `RebindLocalSeat` is the FIRST-JOIN path and carried none of it.
**Two routines for one job, one of them a subset, is how this survived**: whichever you read, it
looked finished. A mid-match join worked, because that branch calls `SceneFlow.StartMatch()`
AFTER `SetLocalSeating`, so the arena is built with the right seat in the first place. Only a
normal pre-match join was broken, which is every ordinary game.

⚠️ **AND THE HOST CANNOT SEE ANY OF IT**, which is § 38's whole thesis. On the host `LocalSlot`
is 0 before the arena loads, so the rebind is a no-op there and its own player moves normally.

**Fixed** by `MatchInstaller.RebindInputSource`, which moves the reader, takes any `AIController`
off the seat that gains it, and disables before destroying so one keypress cannot drive two
bodies for the remainder of the frame. `RebindLocalSeat` now also rebinds the YOU card, the pause
watcher, the owner glow and the nameplate.

### 53.2 ✅ FIXED: a spectator kept a player's body and a player's HUD

Same function, second line. The guard read
`if (_seats == null || seat < 0 || seat >= _seats.Length) return;` and **a spectator's seat IS
-1**: `LobbySession.Admit` hands one out for a full lobby and for anybody who asked to watch. So
the entire rebind was skipped for the case that needs it most, and a watcher arrived holding seat
0's gameplay camera, seat 0's HUD and a `PlayerInputReader` their keyboard still drove.

⚠️ **That is a watcher puppeting a player, not a cosmetic fault.** `CLAUDE.md` § 4 states the
narrowing the whole spectator key set rests on: *"A spectator has no body, no seat and no
`CharacterMotor`"*. Whichever seat the host gave away, the watcher was also driving seat 0.

### 53.3 ✅ FIXED: `-tp-allbots` skipped the one seat this peer holds

`HumanSeat` answers -1 under AllBots so no seat gets a reader, which is correct. But this peer's
own chair is OCCUPIED in the lobby, by this peer, so `isHumanPlayer` was true for it and it fell
through BOTH arms of `BuildSeat`: no reader, and no bot either. **A body with no input source.**

⚠️ **Measured**: seat 0 travelled **0.0 m** on the host over a 150 s sample while seats 1, 2 and
3 travelled 77.6, 56.0 and 45.0 m. Not "less", as §§ 34 and 49 record for the offline probe:
nothing at all. **Every networked measurement this project has taken has been three seats' worth**,
and the missing one is the seat the camera is pointed at. `CLAUDE.md` § 7.1 states the intended
behaviour in as many words; it was true offline and false online.

### 53.4 ✅ FIXED: the all-bots HUD disagreed with the all-bots camera

🧑: *"for some reason im in spectator but im also defender"*, *"its so weird bcz spectator has
skills and defender UI"*. `MatchInstaller` set `_spectating = GameLaunch.Spectator` while the
camera below turns the gameplay rig off for `HumanSeat < 0`, which is THREE conditions. So an
all-bots run flew the free camera over a HUD still wearing seat 0's clothes: a YOU card reading
TAYA P1, a DEFENDER badge and a live ability deck nobody could press. `_spectating` is
`HumanSeat < 0` now, so the two halves cannot disagree.

⚠️⚠️ **THIS CHANGES WHAT `FrameCapProbe` RENDERS.** It sets `GameLaunch.AllBots = true`, so its
runs now draw a stripped HUD with no YOU card and no role-swap card. **§ 17's achieved-frame-rate
numbers from before 2026-08-27 are no longer comparable** and that investigation needs a fresh
baseline before it quotes any of them again.

### 53.5 ✅ FIXED: a ready press made during the join window vanished

`NetAuthority.IsNetworked` reads `NetworkManager.IsListening`, which goes true the instant
`StartClient` is called and **not** when connection approval finishes. Everything that asks "am I
networked" answers yes during the join, and a `SendNamedMessage` on that transport goes nowhere
and reports nothing. A player who pressed R in that window had their vote disappear while the
lobby screen told them *"Ready! Waiting for other players..."* to somebody the host was itself
waiting for.

`DeclareReadyServerRpc` and `VoteRematchServerRpc` check `IsConnectedClient` and return whether
they delivered. `ReadyGate` holds an undelivered press and resends it (free: the host's set is
idempotent), `ConvertedMatchSetup` no longer claims ready until the host has been told, and
`MatchResult` no longer deadens the REMATCH button on a vote that never left.

### 53.6 The AI takeover and seat reclaim paths, audited against 🧑's spec

Read end to end after the above, because three of the four faults were in the same seam.

| Requirement | Where | State |
|---|---|---|
| A bot fills a seat nobody is in | `MatchInstaller.BuildSeat`, host only | ✅ |
| A bot takes over when a player leaves | `NetSession.OnClientDisconnected` → `MatchRpc.HostPeerLeft` | ✅ sets `IsBot`, adds `AIController`, calls `ForgetInputSource` |
| The seat is HELD, not freed, mid-match | `LobbySession.Depart` banks it against the peer's token | ✅ |
| A joiner replaces the bot | `HandleIdentify` → `HostLateJoin` destroys the `AIController` | ✅ on a first join as well as a late one |
| A REJOINER gets their own chair back | `LobbySession.ReclaimSeatFor(token)` | ✅ the new transport id is a fresh `_spawned` entry, and `HostPeerLeft` dropped the old one |
| The joiner's own machine then drives it | `RebindLocalSeat` | ❌ **this was 53.1** |

⚠️ **The last row is the one that was missing, and it is why the other five looked fine.** The
host handed the seat over correctly every time; the arriving machine never picked it up.

### 53.7 Still open

* ⚠️⚠️ **NONE OF THIS HAS BEEN PLAYED BY TWO PEOPLE YET.** The diagnosis is from the source and
  from a two-process run on one desktop; the fixes are unplayed. § 38.20's standing note applies.
* **The root cause is still there and only its consequence is fixed.** A client builds its arena
  before it has been told its seat, and `RebindLocalSeat` is a correction after the fact. **Done
  looks like:** the client not loading the arena until `Seating` has landed, which removes the
  guess instead of repairing it, and deletes the need for one of the two rebind routines.
* **`NetStateReport` calls `Application.Quit()` after it writes**, so in a two-process run
  whichever peer's window elapses first takes the other's connection with it and the second
  report is never written. Give the client the SHORTER `-tp-netseconds` until that is changed.


---

## 54 · Which of the two lobby fixes was kept, and why

Two sessions worked the same evening from the same base, `6ecabb86`, and both landed on the
lobby. **§ 55 below is the other one's work and it is kept nearly whole**, because its diagnosis
was the better one: READY in the lobby resolved to a `FindFirstObjectByType<ReadyGate>()` that
only exists inside the ARENA, so every press on that screen, the host's included, hit a null.
It answers that with a real host-side set, a `ReadyTally` broadcast so every screen can draw the
count, and an auto-start through `HostStartMatch` so there is exactly one path into an arena.

**Two things from § 52 were kept over it, and both are about the same field:**

1. **`DeclareReady` carries no peer id at all.** § 55's version keeps the field on the wire and
   READS it into a discard, to keep the writer and the reader the same length for
   `tools/audit_wire_payloads.py`. That is honest and it is still a value the host has to
   remember to ignore, and remembering is exactly what failed the first time: every caller
   reached for `NetAuthority.LocalSlot`, which is a SEAT, and the host keyed its set by a seat
   from one peer and a transport id from another. **A field that cannot be trusted should not be
   sent.** The message is now one `bool`, the sender is NGO's authenticated id, and the audit
   needs no waiver for it or for `VoteRematch`.
2. **The press reports whether it was delivered.** § 53.5's finding, which § 55 does not cover:
   `IsListening` goes true at `StartClient` and not at approval, so a press made during the join
   window went to a transport with nowhere to send it and said nothing.

**Everything else in § 55 is kept as written**, including the toggle (the button is a toggle and
the message was not, so un-readying was swallowed as a duplicate), counting the set against the
live lobby rather than trusting it, and the `FromHost` sender check on the tally.

## 55 · The lobby was a picture of a lobby ✅ CLOSED 2026-08-27

🧑, 2026-08-27, three reports in one line: *"in a multiplayer lobby, a player cannot switch
from p1 to p4"*, *"it also does not reflect when a person joins the lobby"*, and *"when all player
ready up and the game starts, it only starts for the host"*.

⚠⚠ **ALL THREE ARE THE SAME SHAPE AND IT IS THE SHAPE SECTION 36.1 NAMED: the person running
the lobby cannot see it.** Every one of these is a control or a display that works on the host's
screen, or works on nobody's, and none of them had a wire message behind it at all. Section 38
went through `MatchRpc` verb by verb and found eleven faults; it never looked at the screen the
players sit on before any of those verbs exist.

### 52.1 ⚠⚠ THE SEAT BUTTONS WERE NEVER CONNECTED TO THE NETWORK ✅

`ConvertedMatchSetup.WireSeats` did this on a press:

```csharp
GameLaunch.SoloSeat = seat;
GameLaunch.Spectator = false;
```

`GameLaunch.SoloSeat` is read by `MatchInstaller.HumanSeat` **for the offline practice match and
by nothing else**: two lines above it, `if (net != null && net.IsNetworked) return net.LocalSlot;`
takes the networked answer from the transport instead. So in a lobby the press wrote a number
nobody reads, and `RefreshSeats` redrew the rows from `LocalSlot`, which had not moved. There was
no request, no host rule and no reply: **seat choice did not exist as a feature.**

⚠️ **And on top of that the buttons were dead for everyone except the host.**
`button.interactable = !GameLaunch.Spectator && (!isNetworked || NetAuthority.IsHost)`, so the
one peer whose press would have been meaningless was also the only peer allowed to make it.

**Fixed** with the same idiom the map and the mode already use, client asks and host decides:
`ReqSeat` carries a chair number (-1 means spectate), `LobbySession.TryTakeSeat` is the rule, and
the host answers the mover with the existing `Seating` message and everybody with
`BroadcastLobbyPicks`. ⚠️ **The person comes from the sender's transport id, never from the
payload**: the message names a chair, not a player, or a peer could move somebody else out of
theirs.

The rules, each with a test in `LobbyAndSettingsTests`:

| rule | why |
|---|---|
| a chair somebody else is in is refused | two players in one seat is one body |
| a HELD chair is refused | it belongs to somebody who dropped out of THIS match, which is the promise `RuleOnArrival` branch 1 makes |
| any change is refused once `MatchInProgress` | a seat carries a score, a body and a turn in the taya rotation |
| the dedicated server is refused | it referees; section 38 already had it seatless everywhere else |
| asking for the chair you are in succeeds and changes nothing | a request is allowed to be idempotent |
| a LEADER who starts spectating hands leadership on | `ReassignLeader` already skips spectators but is only reached from `Depart`, so nothing covered leaving the table without leaving the lobby |

⚠️ **SPECTATE goes down the same wire.** It used to flip `GameLaunch.Spectator` locally, so the
host went on counting that peer towards the ready gate and went on building it a body, and a
spectator who wanted to play again had no way back into a chair. It is `ReqSeat(-1)` now, and
pressing a free seat is how you stop.

### 52.2 ⚠⚠ READY IN THE LOBBY WAS ROUTED TO AN OBJECT THAT IS NOT IN THE LOBBY SCENE ✅

`MatchRpc.DeclareReadyServerRpc` and `OnDeclareReadyMsg` both did
`FindFirstObjectByType<ReadyGate>()?.DeclareReady(...)`, and **`ReadyGate` is a component of the
ARENA**. In `MatchSetup` that find returns null. Every READY press in the lobby, the host's
included, resolved to a null and did nothing: the tick on screen was a local `bool` this screen
owned and told nobody about. Nothing counted, nothing started, and the only way into a match was
the host's own START button, which is the report.

**Fixed** with a lobby-side tally in `MatchRpc`. ⚠️ **It counts SEATED PEERS, not characters**,
for the reason `ReadyGate` gives at length: the empty chairs are played by bots and a bot cannot
press a key. Spectators are excluded on the same rule and the count floors at one, so a solo host
still presses its own button. ⚠️ **And it starts through `HostStartMatch`, the same path the
button uses**, so there is exactly one way into an arena and the broadcast that carries the other
peers in with it cannot be forgotten on one of them.

Three holes closed with it:

* **The press had no state.** The button is a toggle and the message was not, so un-readying sent
  a second "I am ready", which the host's set swallowed as a duplicate. `DeclareReady` carries a
  bool now; the peer id stays on the wire and is read and discarded, because the host resolves the
  sender at the door.
* **A peer leaving did not re-evaluate the gate**, which is the hole `ReadyGate.OnPeerLeft` and
  `MatchResult.OnPeerLeft` already close for the other two tallies. `HostPeerLeft` now does it for
  this one, in the lobby only.
* **Moving seats leaves your ready standing.** It clears it: the arrangement you agreed to is not
  the one on screen any more.

### 52.3 ⚠⚠ THE MAP WAS NEVER SENT TO A JOINER, SO A CLIENT STARTED A DIFFERENT ARENA ✅

The second half of "it only starts for the host", and the worse half. `SelectMap` and `SelectDiff`
only ever travelled when the host **cycled** them. A peer joining a lobby the host had already set
up was told the mode (section 38 added that) and nothing else, so its lobby drew whatever map its
own menu last held. `SceneFlow.SelectedMap` is exactly what `SceneFlow.StartMatch` loads.

⚠️ **A joiner who never saw the host touch the arrows loaded a different street.** `SyncMap`
and `SyncDiff` are sent from `HandleIdentify` now, beside the mode, and for the same reason the
mode's own note gives: everything below them is interpreted through them, and a joiner may be
about to build an arena from them.

### 52.4 ⚠️ THE LOBBY REDREW ITSELF ON ONE EVENT AND THREE THINGS MOVE IT ✅

`ConvertedMatchSetup` subscribed to `OnLobbyPicksSynced` and to nothing else, so the seat rows
were redrawn only when a pick table happened to arrive. The local seat changing (`LocalSlot` is
written from three places and not one of them told anybody), the mode arriving, and the ready
tally moving all changed the screen and said nothing. `NetSession.SeatingChanged` is new;
`OnLobbyRosterSynced`, `OnModeChanged` and the new `OnLobbyReadyChanged` are now all answered.

⚠️ **`MatchInProgress` is written on the client too.** It arrived on the `Seating` message, was
read for the scene-load branch two lines further down and then dropped, so a client's
`LobbySession` said false for the whole of a running match. The lobby reads it to grey the seat
rows out, which is the difference between a button that explains itself and one that silently does
nothing when the host refuses it.

⚠️ **And `HostStartMatch` now tells the lobby the match is running**, which it never did.
`MatchInProgress` is the switch behind three separate rules: `Depart` only HOLDS a dropped
player's chair while it is set, `RuleOnArrival` only answers Spectate rather than Refuse while it
is set, and 52.1's seat change is refused once it is set. Left false, a player who dropped
mid-match lost their seat and their score to the next arrival, and anybody joining a running match
was turned away outright. That is section 35's whole reconnection story silently switched off.

### 52.5 ⚠️ `AnyAttackerCanPickUpAnySlipper` SILENCED ONE OF THE TWO PRODUCERS ✅

Found by this work rather than reported: the PlayMode suite went red on a test nothing here
touches, and it is an order-dependent flake that has been latent since the test was written.

`MatchInstaller.HumanSeat` gives ONE seat a `PlayerInputReader` instead of an `AIController`, and
its offline default is `GameLaunch.SoloSeat`, which is **1**. The test picks its attacker as the
first non-defender `CharacterMotor` that `FindObjectsByType(FindObjectsSortMode.None)` hands back,
and that order is **explicitly unsorted**. When it handed back seat 1, the test disabled an
`AIController` that was not there, left `PlayerInputReader.Update` writing `Grab = false` over the
press on every frame, and failed with *"the Grab press edge never reached Carrier"* against a
pickup that works perfectly in the player.

⚠️ **It is the same fault the sibling test three methods up records having lived with "for the
whole of its first life", and `Silence` is the helper that already existed for it.** It turns off
both producers. The test calls it now, and restores both afterwards.

⚠️ **It passes on its own and fails after `BotBehaviourProbe` has run**, which is why it read
as "the lobby change broke the arena": what actually moved was which body the unsorted find
returned first. Measured both ways before touching it, the whole suite on the pristine tree and
the whole suite with the lobby work, which is the only way to tell a flake from a regression.

### 52.6 What was measured

* `dotnet test`: **91 core tests**, green.
* EditMode: **152**, green, including **eight new seat-change tests**.
* PlayMode: **67**, green, `-testCategory "!WallClock"`.
* `Checks.RunAll`: all five green in one launch.
* `tools/audit_wire_payloads.py`: **44 named messages, 0 mismatched** (was 41; `ReqSeat` and
  `ReadyTally` are new and `DeclareReady` is symmetric now rather than an accepted asymmetry).
* `tools/audit_request_call_sites.py`: **42 wire entry points, 0 unreachable.**
* `tools/audit_ability_authority.py`: **40 sites, 25 gated, 0 ungated on another body.**

⚠⚠ **NONE OF IT IS A TWO-MACHINE TEST, AND THAT IS THE ONE THING STILL OPEN HERE.** The rules
are asserted against `LobbySession`, which is transport agnostic on purpose, and the wire halves
are checked field by field by the audit. What no automated check in this repo can currently do is
put two processes in one lobby and press the buttons: `NetBootstrap` drops both straight into an
arena and skips `MatchSetup` entirely. Everything in 52.1 to 52.4 is reasoned from the source and
verified at the seams; **the four-way lobby itself still wants a human on two machines.**

---

## 56 · What the merged network pass still leaves open

Written after §§ 52, 53 and 55 landed together, so the next session starts from a list rather
than from a search. Nothing here is speculation: each is a specific line that was read.

* ⚠️⚠️ **NONE OF THIS HAS BEEN PLAYED BY TWO PEOPLE.** Every diagnosis in §§ 52, 53 and 55 came
  from the source and from two processes on one desktop, which share a clock, a filesystem and a
  loopback. § 38.20's standing note is still the honest state of the network.
* **`MatchRpc.SendSeating`'s local branch does less than its remote one.** For a remote peer it
  sends `Seating`, and `OnSeatingMsg` calls `SetLocalSeating` AND then either
  `SceneFlow.StartMatch()` or `MatchInstaller.RebindLocalSeat`. For the host it calls
  `SetLocalSeating` and stops. It is unreachable today because `LobbySession.TryTakeSeat` refuses
  every seat change while `MatchInProgress`, so the host can only move seats in the lobby where
  there is no arena to rebind. **It is the same subset-of-the-other-path shape as § 53.1** and it
  becomes a live fault the day mid-match seat changes are allowed. **Done looks like:** the host
  branch running the same two lines the message handler does.
* **The client still builds its arena before it has been told its seat, and § 53.1 repairs the
  guess rather than removing it.** `NetBootstrap` and the menus both call `StartClient` and then
  load the map. **Done looks like:** the client not loading the arena until `Seating` has landed,
  which deletes the need for one of the two rebind routines.
* **`NetStateReport` calls `Application.Quit()` after it writes**, so in a two-process run
  whichever peer's window elapses first takes the other's connection with it and the second
  report is never written. Give the client the SHORTER `-tp-netseconds` until that changes.
* **`ProtocolVersion` moved twice in one day**, 2 to 3 for dropping the peer id from
  `DeclareReady` and `VoteRematch`, and 3 to 4 for the lobby gate's `ready` bool and the new
  `ReadyTally`. Any player built between those two points speaks a 3 that is not this 3.
* **The AI's eleven `FindObjectsByType<Slipper>` calls are still there** (§ 52.5). They run on
  the HOST, for up to four bots, several times per decision tick, so they are a networked cost as
  well as an offline one: the host's frame rate is every peer's tick rate.


---

## 57 · The match ends on one machine, and three other events never reach a client at all

A read of the client's whole event path, 2026-08-27, after §§ 52 to 56. **They are all the same
shape and it is § 38's thesis again: the host cannot see any of them.** `MatchDirector` and
`RoundDirector` raise their events from the methods that CHANGE the state, and every one of those
methods is behind `SliceRunner`'s `NetAuthority.ShouldResolve()`. On a peer that is not the host
the only thing that moves that state is `ApplySnapshot`, and `ApplySnapshot` raised nothing.

### 57.1 ✅ FIXED: `MatchEnded` fired on exactly one machine in the room

`MatchDirector.ApplySnapshot` assigned `_scores`, `RoundNumber` and `MatchInProgress` and raised
no event. `AdvanceRound` and `BeginIntermission` are the only other writers and both are
host-only, so on a client the match simply stopped being in progress and nothing noticed.

**That is the whole end of the game, for everybody except the host:**

* **`UI.MatchResult` shows itself from `MatchEnded` and from nothing else**, so a client never saw
  the final standings. It stood in a dead arena.
* ⚠️⚠️ **AND REMATCH LIVES ON THAT BOARD, SO THE ENTIRE PEER REMATCH VOTE WAS UNREACHABLE FOR
  ANYONE BUT THE HOST.** § 1 has been open since 2026-08-25 and § 52 rewrote its counting rules;
  none of it could ever have run, because a client had no button to press and `RematchTally` and
  `BeginRematch` arrived at a screen that was never raised. `NetAutomationProbe`'s
  `-tp-autorematch` waits on `MatchResult.IsVisible`, which on a client was false forever.
* `SliceRunner.OnMatchEnded` never ran there, so the round rules were never stopped, and
  `MatchInstaller`'s `_wonVoice` never played the announcer's win line.

**Fixed** by raising `MatchEnded` on the **true-to-false edge** of `MatchInProgress` inside
`ApplySnapshot`, off the replicated scores, so the winner is the host's answer rather than a
client's arithmetic. The edge matters: a joining client is told `false` before a match starts and
`false` again after one ends, so raising on the VALUE would show the result board to somebody who
has just walked into a lobby.

⚠️ **The host reaches `ApplySnapshot` too**, through `MatchRpc.HostSyncPeer`, and it is a no-op
there by construction: it hands the host its own `MatchInProgress` back, so the edge cannot fire.

⚠️ **And the final `inProgress = false` does travel, which this fix depends on.**
`MatchResult.OnMatchWon` stops the clock with `Time.timeScale = 0`, and `MatchRpc.FixedUpdate` is
what broadcasts `SyncWorld`, so a frozen host would never send the packet that says it is over.
It is already guarded: *"SINGLE PLAYER PAUSES, NETWORKED DOES NOT. A networked peer that froze its
own time would stop answering the host."* Do not remove that guard.

### 57.2 ✅ FIXED: the intermission card never appeared on a client

`IntermissionStarted` is raised only by `BeginIntermission`, host-only, so `UI.RoleSwapCard` never
runs on a client and a joiner sees no end-of-round card, no rotation announcement and no
standings between rounds.

⚠️⚠️ **AND IT CANNOT BE FIXED THE WAY 57.1 WAS, WHICH IS WHY IT IS ITS OWN ITEM RATHER THAN THE
SECOND HALF OF THAT ONE.** `IntermissionStarted` and `RoundStarted` are both wired to
`SliceRunner`, and **both of its handlers mutate the world**: `OnRoundStarted` calls `ResetWorld`,
which teleports all four bodies and hands out the tsinelas, and `OnIntermission` additionally
schedules `Advance`, which calls `AdvanceRound`. Raising either on a client would give every peer
its own second authority over the round number, and four peers each advancing a match is four
matches, which `VISION.md` § 4 forbids in its first rule.

✅ **FIXED the same session, and with no wire change.** `MatchRpc.ApplyNetworkRoundBoundary`
derives the boundary from the snapshot the client already receives and drives the CARD directly
through `RoleSwapCard.ShowForShot`, which was written for the capture pass and is exactly the
right shape. The runner gets no signal at all, which is the point.

⚠️ **The derivation:** during the host's intermission `RoundActive` is false while
`MatchInProgress` is still true and `RoundNumber` has not moved yet, because `AdvanceRound`
increments it when the buffer ends. That combination happens at no other time; a match ENDING
drops `inProgress` with it, which is what rules it out.

⚠️ **It acts on the EDGE, not the state.** `SyncWorld` arrives at 5 Hz, so acting on the value
would re-raise the card ten times over one intermission and restart its timeline on every packet.

⚠️ **And it hides the card on the way back in.** The host dismisses it from `RoundStarted`, which
a client never gets either, so without the second edge the card would sit as a full-screen panel
over the whole of the next round: worse than never showing it.

### 57.3 ✅ FIXED: no score event reached a client, so scoring was silent there

`MatchDirector.Scored` is raised only inside `AddScore`, which opens with
`if (!NetAuthority.ShouldResolve()) return;`. There is **no score message on the wire** (44 named
messages, and none of them is one). So on every client:

* no `score_award` sting, on any award or penalty;
* no `+100  LATA DOWN` toast, so a player is never told what they were paid for;
* no scoreboard row pulse, because `Hud.OnScored` is what sets `_scorePulses`.

The numbers simply appear, 200 ms later, via `SyncWorld`. **In a game whose entire feedback loop
is scoring, three of the four things that acknowledge a point are host-only.**

⚠️ **It cannot be recovered by diffing the replicated scores**, which is the obvious cheap fix:
the toast and the sting both need the `ScoreEvent` KIND (`MatchRules.PointsFor(e)` and the label),
and a score delta does not carry it. Two events in one 200 ms window would also collapse into one.

✅ **FIXED the same session.** A host-to-all `Score` message carrying `(slot, ScoreEvent)`, sent
from `AddScore` **inside the host guard** so the announcement cannot be made anywhere a point
cannot be created, and raising `Scored` on the receiving peer through
`MatchDirector.ApplyNetworkScoreEvent`. It is the shape `BroadcastStyle` already uses for Street
Hype (§ 38.15), which was the same fault for the same reason: Classic's whole bottom-of-screen
identity was host-only until somebody looked. `ProtocolVersion` went 4 to 5.

⚠️ **`ApplyNetworkScoreEvent` DOES NOT TOUCH THE SCOREBOARD**, and that is the whole reason it is
a separate method from `AddScore`. The totals arrive in `SyncWorld` and `ApplySnapshot` sets them
from the host's own numbers; adding here as well would make a client's board the sum of a
replicated total and its own arithmetic, which disagree at exactly the moments that matter.

⚠️ **An unknown event is dropped rather than cast.** `Enum.IsDefined` gates it, because a cast of
an out-of-range int would reach `MatchRules.PointsFor` as a value it has no case for.

⚠️ **`DefenseTick` and the two penalties are broadcast too**, at roughly one a second while they
apply. `Hud.OnScored` discards the first outright and gives the other two a sound and no words, so
this is a few bytes a second to keep the event faithful rather than to teach the receiver a rule
the sender should not be making for it.

### 57.4 Checked and NOT broken, so nobody re-derives it

* **The lata's knockdown and restoration DO reach clients.** `Lata.ApplySnapshotState` raises
  `UprightChanged` itself, so the HUD's centre alert and card both fire on a joiner. It raises it
  on every apply rather than on the edge, which is free: `Hud` guards on `_lataUprightShown`.
* **Ability cooldowns and ultimate charge are replicated** by `SyncAbility` at 5 Hz, so the fact
  that `HeroAbilitySystem.ResetKit` is only reached from `ResetWorld` (host-only) does not strand
  a client's kit at a round boundary: it converges within 200 ms. ⚠️ The one part that does NOT
  converge is `ClearBuffers`, so a key pressed in the last frames of a round can come out in the
  next one on a client. The press still goes to the host as a request and is re-checked there, so
  it is a presentation oddity rather than an authority hole. Not fixed.
* **Street Hype reaches the one peer it belongs to** (`BroadcastStyle`, per-peer rather than
  broadcast, because `Hud.ApplyStyle` refuses any slot that is not local).
* **The prop stream already sends only what changed**, with a twice-a-second keepalive so a joiner
  who missed the packet that said "the can went over" is wrong for at most half a second.


---

## 59 · Two machines could discover each other and could not join, and it is one missing string split

🧑 2026-08-27, with both firewalls off and two laptops on one LAN: *"they are detected on lan and
server but i cant join"*, *"both devices cannot reach each other"*, *"they can discover but they
cant join"*. Reported with a screenshot of the join box reading
`Could not reach 192.168.1.144:8910`, naming the exact machine that had just advertised itself.

### 59.1 ✅ FIXED: nothing anywhere parsed `host:port`

`NetSession.Configure` hands its argument straight to `UnityTransport.SetConnectionData`, which
wants a bare address. **The join field is filled with `ip:port` from three directions:**

* `LanBeacon` advertises `ip:port` and `ConvertedMultiplayerSetup.OnLanRowClicked` copies that
  string into the box verbatim, so **clicking a discovered LAN game could never work**;
* the online browser row does the same;
* the box's own help text reads *"An online code, or a LAN address. Port defaults to 8910"*,
  which tells a player the port is optional and therefore that writing it is allowed.

The whole string then went in as the HOSTNAME. It is not an address and not a resolvable name, so
the transport refused to start, `StartClient` returned false, and the screen printed
`Could not reach 192.168.1.144:8910`.

⚠️⚠️ **DISCOVERY AND JOINING NEVER SHARED A CODE PATH, WHICH IS WHY THIS SURVIVED.** Discovery is
`LanBeacon` over UDP and it worked perfectly; joining is `UnityTransport` and it was handed a
string only the beacon's own formatter had ever produced. Every test of the join path used
`-tp-join 127.0.0.1 8910`, which passes the host and the port as **two arguments** and never
exercises the one-string form a human uses.

**Fixed** in `NetSession.StartClient`, which is the single door every caller goes through, so the
menu, the CLI and the join-code resolver all benefit. `NetSession.SplitHostPort` is public and
asserted in `LobbyAndSettingsTests`:

* `192.168.1.144:8910` splits; `192.168.1.144` keeps the caller's port, so `-tp-join 127.0.0.1
  7777` is unchanged;
* the field is trimmed, because nothing else trims it;
* ⚠️ **a bare IPv6 literal is left alone.** It is full of colons and is a valid address on its
  own, so splitting on the last colon would turn `fe80::1` into a host of `fe80:` and a port of 1,
  which is a worse failure than the one being fixed because it would look like it worked.
  Bracketed IPv6 (`[::1]:7000`) is read, and the brackets come off even without a port because
  they are join-address syntax rather than part of the address;
* a trailing colon, an empty host or a port outside 1 to 65535 is left alone, so the transport
  reports the address the player actually typed rather than a guess made out of it.

### 59.2 ✅ FIXED: a refused join left the player in a lobby that said CONNECTED

`ConvertedMultiplayerSetup.Join` navigates to the lobby the moment `StartClient` returns true, and
**that only means the transport was told to start.** Approval has not happened yet and can still
be refused, for a protocol mismatch or a full lobby. The refusal arrives at
`NetSession.OnClientDisconnected` seconds later, on a screen that has already been left behind, so
the reason was written to a status label nobody was looking at.

🧑 sent the frame: a screen headed **LOBBY · CONNECTED** with `P1 · TAYA FIRST ◀ YOU` and every
other chair drawn as **BOT**, because no roster or seating ever arrived, and READY answering
*"Still connecting. Press again in a moment."* (§ 53.5's guard, correctly refusing to claim a
readiness the host had never been told about).

**Fixed** with `NetSession.ClientDisconnected` and `NetSession.LastDisconnectReason`. The lobby
returns to the join screen, and the join screen prints the reason once and clears it, so a
version mismatch says so instead of looking like a hang. ⚠️ A protocol mismatch is the likeliest
cause whenever two machines were built from different commits, and it is the one thing in that
list a player can actually fix.

### 59.3 ✅ CHANGED ON REQUEST: READY no longer starts the match

🧑: *"i also dont like that if u click ready it auto starts, i want to have to click start match
to start it as host"*. § 55's gate called `HostStartMatch` itself once the tally was satisfied, so
the last person to tick a box decided when four people were dropped into an arena and the host's
own START button was decoration it could never reach. Readiness is now what the button says: an
ANSWER, drawn on every screen by `ReadyTally`, that the host reads before choosing its moment.

⚠️ **The host is not blocked on the tally either.** START stays live whatever it reads, because a
lobby of one host and three bots is a legitimate match and waiting for a quorum of one would be a
gate with nothing behind it.

⚠️ **`HostPeerLeft` stopped starting matches too.** It called `HostStartMatch` when a departure
satisfied the gate, which was right for a gate that started matches. A peer QUITTING the lobby and
thereby dropping three other people into an arena is the same surprise from a worse direction; it
now just redraws the tally.

### 59.4 Still open

* ⚠️ **`ProtocolVersion` is 5 and has moved three times today** (2→3 dropping the peer id from
  `DeclareReady` and `VoteRematch`, 3→4 for the lobby gate's `ready` bool and `ReadyTally`, 4→5
  for `Score`). **Both machines must be built from the same commit**, and 59.2 is what makes it
  say so out loud instead of hanging.
* **Nothing above has been played by two people.** 59.1 is the only one of the four whose failure
  was reproduced first-hand, and it was reproduced by reading rather than by playing.


---

## 60 · The host announces a seat twice, by two protocols, and only one of them does the job

🧑 2026-08-27, from the joining laptop, in the arena, with the free-roam prompt on screen:
*"i can move camera and see updates but i cant move"*, and before that *"host can move but
everyone else is stuck even bots"*.

### 60.1 ✅ FIXED (the symptom): `ApplyAssignedSeat` told nobody

**The evidence is one missing line in the client's `Player.log`:**

```
[Net] connecting to 192.168.1.144:8910
[Net] connected
[Net] connected as seat 2
```

`connected as seat 2` is `NetSession.ApplyAssignedSeat`. **`[Net] seated in slot 1
(spectator=False)` is nowhere in the file**, and that is `NetSession.SetLocalSeating`. So the seat
NUMBER was applied and the thing that announces it never ran.

⚠️⚠️ **THERE ARE TWO SEAT PROTOCOLS AND THE HOST SENDS BOTH.**

| | `tp.seat.assignment.v1` | `Seating` |
|---|---|---|
| Sent by | `NetSession.OnClientConnected` → `SendSeatAssignment` | `MatchRpc.HandleIdentify` |
| Lands in | `OnSeatAssignmentMessage` → `ApplyAssignedSeat` | `OnSeatingMsg` → `SetLocalSeating` |
| Sets `LocalSlot` | ✅ | ✅ |
| Join code, leader, `MatchInProgress` | ❌ | ✅ |
| Raises `SeatingChanged` | ❌ **(was)** | ✅ |
| Rebinds camera, HUD, ready gate, **input reader** | ❌ | ✅ |

The host admits the same peer **twice**, by two unrelated routes, and announces the result twice.
Whichever message the client processes decides how much of the job gets done, and one of the two
does almost none of it. **`LocalSlot` moves to the right chair while the arena is never told**, so
the `PlayerInputReader` stays where `BuildSeat` put it and the player's keys drive nothing. That
is § 53.1's fault one layer further down, reached by a different message.

⚠️ **And it explains the earlier report too.** *"host can move but everyone else is stuck even
bots"* was that same client, in the free-roam window, where the round has not begun: the host
walks around because free-roam allows it, the bots stand still because `RoundActive` is false, and
the client cannot press READY into a body it does not drive, so the countdown never starts and
nothing ever moves. One fault, two descriptions.

**Fixed** by raising `SeatingChanged` from `ApplyAssignedSeat` as well, so whichever message wins
the race, `MatchInstaller.FollowLocalSeat` hears it and moves the reader.
`RebindLocalSeat` is idempotent, so both winning costs nothing.

### 60.2 ⚠️ OPEN: retire one of the two protocols

60.1 makes both paths complete enough to play. **It does not make there be one path.** Two
protocols for one fact, one a subset of the other, is precisely the shape of § 53.1
(`RebindLocalSeat` versus `ApplyRebindLocalSeat`) and § 57.1, and it will produce a third bug.

⚠️ **It was deliberately not done in this pass**, because deleting a seat path while two laptops
are mid-test is how a working build becomes an unworking one.

**Done looks like:** `NetSession.OnClientConnected` no longer admitting the peer or sending a seat
at all, leaving `MatchRpc.HandleIdentify` as the single admission and the single announcement, and
`tp.seat.assignment.v1`, `SendSeatAssignment`, `OnSeatAssignmentMessage` and `RegisterSeatHandler`
deleted with it. ⚠️ **Check first what `OnClientConnected`'s `Lobby.Admit` is doing that
`HandleIdentify`'s is not**, in particular the `replacedPeerId` disconnect of a stale transport,
which only the `NetSession` copy performs.

### 60.3 ✅ ADDED: one log line that names which of these it is

*"i can move camera and see updates but i cant move"* is produced by at least three different
faults and they are indistinguishable from a screenshot: the reader on the wrong seat,
`LocalSlot` disagreeing with the body the camera follows, and `CharacterMotor.IsLocallySimulated`
answering false so `FixedUpdate` treats this peer's own body as a host-authored picture.

`MatchInstaller.LogSeatWiring` prints all of them, at arena install and on any seat change:

```
[NetSeat] arena installed: LocalSlot=1 spectator=False host=False body=ok reader=True ai=False simulated=True
```

⚠️ **Paste that line from the client when reporting a movement fault.** `reader=False` is § 53.1,
`simulated=False` with a correct `LocalSlot` is the motor's gate, and `body=MISSING` is a seat the
arena never built. It is two lines a match, not per frame.

### 60.4 ✅ FIXED: leaving on purpose was reported as a failure

🧑: *"this shit shows even if i close on my own"*, over
`[Disconnect Event][Client-0][TransportClientId-0][TransportShutdown] NetworkConnectionManager was
shutdown.` printed on a menu in the game's own font. That is § 59.2's reason line showing two
things it should not:

* **a disconnect we asked for.** `Shutdown` raises the same callback a refusal does, so pressing
  BACK told the player why they had been thrown out of a lobby they had just chosen to leave.
  `NetSession.Stop` sets a flag now and the handler stays quiet for it. ⚠️ `StartClient` calls
  `Stop` first when a session is already live, so this also covers the disconnect a re-join makes
  on its way out of the old connection.
* **Netcode's own event envelope, which is not player-facing text.** It describes the mechanism
  and never the cause. `PlayerFacingDisconnectReason` keeps what the HOST itself wrote,
  `ApproveConnection`'s "Game version mismatch (network protocol 5)" and "Lobby is full", and
  turns anything bracketed or empty into "Lost connection to the host." Those two host-authored
  strings are the whole point of the mechanism: a version mismatch is a thing the player CAN fix.

---

## 53 · The corner stamp is the branch name ✅ CLOSED 2026-08-27

🧑, 2026-08-27: *"for every branch made it would replace the version number on the bottom
right corner with the branch name instead"*.

⚠⚠ **THE LABEL EXISTS TO ANSWER "IS THIS THE BUILD I ASKED FOR", AND A VERSION NUMBER HAD
STOPPED ANSWERING IT.** `bundleVersion` is bumped per change rather than per branch, so several
branches in flight at once all read `v4.72` and the only way to tell which .exe was on the Desktop
was to diff files. That is the same failure `GameVersion`'s own header records from the PGH project
one level up: there the stale build was four days old, here it is the wrong branch entirely, and
section 7 of `CLAUDE.md` already carries two separate incidents of a build being judged by the
wrong evidence.

**`main` keeps the number, and that is the rule rather than an exception to it.** A branch name
means "work in flight"; a build off `main` is the game, and the number on it is what goes into a
screenshot to a sponsor, which is what the stamp was for in the first place.

### 53.1 How it works, and why none of it is a step anybody runs

`GameBuilder.StampBuildBranch` writes the checked-out branch to
`Assets/TumbangPreso/Resources/BuildBranch.txt` on **every** build, because a player has no git.
`BuildBranch` reads it, `GameVersion.DisplayString` picks between the name and the number, and both
corner labels, the HUD's code-built one and the `VersionStamp` baked into every converted menu
scene, go through the one `GameVersion.ApplyTo`.

⚠️ **In the editor git is read live and the file is ignored**, because a stamp left over from
the last build is precisely the stale thing this is meant to prevent.

⚠️ **The file is written even when the name is empty.** A build off `main` or a detached HEAD
OVERWRITES the previous branch's stamp rather than inheriting it. Empty and missing both mean
"show the version"; only one of them can be left lying around from three branches ago.

⚠️ **It is gitignored.** It changes per build and per branch, so committing it is a one-line
diff per build and a conflict per merge, over a file whose whole job is to be regenerated.

### 53.2 ⚠⚠ EVERY SESSION HERE RUNS IN A WORKTREE, WHERE `.git` IS A FILE

The naive `repoRoot/.git/HEAD` does not exist in a linked worktree: `.git` is a file reading
`gitdir: <path>`, and the real HEAD is under it. A reader that does not follow the pointer reports
"no git" on exactly the checkouts this stamp is for. `BuildBranch.GitDirFromPointer` follows it,
absolute or relative, and `AWorktreePointerIsFollowedToTheRealGitDirectory` asserts both forms.

⚠️ **And the whole ref path after `refs/heads/` is the name, slashes included.** Taking the last
segment would print `hud-calm-down` for a branch that could equally have been `fix/hud-calm-down`
or `claude/hud-calm-down`, which is the one thing this label exists to disambiguate. A detached
HEAD is not a branch and falls back to the number rather than printing a sha.

### 53.3 ⚠⚠ THE BRANCH NAME MUST NEVER REACH THE WIRE

`Application.version` still carries the real version into the LAN beacon payload, the online lobby
record and the connection-approval hello, and **those are compared between peers**: a name there
would refuse two players built from the same commit on different branches, which is the exact
failure `NetSession.ProtocolVersion`'s note describes as much worse than a clear mismatch.
`BuildBranch` is the LABEL and nothing else. `TheBranchNameNeverReachesTheVersionTheWireCompares`
asserts the separation so a later tidy-up cannot merge the two.

### 53.4 ⚠️ THE BOX WAS SIZED FOR "v4.72" AND WOULD HAVE WRAPPED SILENTLY

The authored rect is 132 px. `claude/multiplayer-lobby-switching-bugs-d1546c` is three times that,
and legacy `Text` defaults to **Wrap**, so the overflow is invisible: the name folds onto a second
line inside a 22 px box and the half you can read is the wrong half. `ApplyTo` switches to Overflow
and widens to 440 px, growing leftward from a bottom-right pivot so a short string still sits in
the same corner. **Third time an authored label in this project has been handed a longer string
than its author measured**; `ConvertedScreen.SetHeadline` carries the other two.

### 53.5 What was measured

91 core, **156 EditMode** (four new: the two parsers, the detached-HEAD case, and the wire
separation), **68 PlayMode**, all five editor checks, and the three audits unchanged at 44/0, 42/0
and 0 ungated. ⚠️ **And it was photographed rather than described**: `UiRuntimeShots` captures
`Logs/shots-runtime/branch_stamp_mainmenu_v1.png` (a converted menu, the `VersionStamp` path) and
`branch_stamp_hud_v1.png` (a live match, the `AttachTo` path), both reading
`fix/multiplayer-fpp-camera-inside-head` in the corner.

---

## 62 · Losing the host left a client playing on alone, and § 60.1 did not fix the movement

### 62.1 ✅ FIXED: a client whose host quits mid-match stayed in the arena

🧑 2026-08-27: *"i closed server and i didnt get kicked out on non host accounts"*, and the
client's `Player.log` carries the disconnect on the line where nothing happened:

```
[Net] disconnected: Disconnected due to host shutting down.
```

§ 59.2 added `NetSession.ClientDisconnected` and subscribed **`ConvertedMatchSetup`**, which is
the LOBBY screen. **It does not exist in a match.** So the one place the event was handled was one
of the four places a player can be when the host vanishes, and the other three (the arena, the
character select, the result board) heard nothing.

**Fixed** by moving the handler to `MatchRpc`, which is `DontDestroyOnLoad` and is therefore the
one object that exists in every scene the player can be in. ⚠️ The lobby's copy is DELETED rather
than kept: two owners for one job is the shape of §§ 53.1, 57.1 and 60, three times in one
evening, and the second owner is always the one that is missing a case.

⚠️ **The reason line itself now works.** `"Disconnected due to host shutting down."` is
host-authored and unbracketed, so § 60.4's filter passes it through unchanged.

### 62.2 ✅ FIXED: the client's seat wiring was CORRECT, and the gate was `RoundActive`

§ 60.1 raised `SeatingChanged` from `ApplyAssignedSeat` so the arena would follow the seat, and the
diagnostic added with it says the wiring is now right on the client:

```
[NetSeat] arena installed: LocalSlot=1 spectator=False host=False body=ok reader=True ai=False simulated=True
```

**`reader=True` and `simulated=True`.** The keyboard is bolted to the body the camera follows, and
`CharacterMotor.FixedUpdate` is simulating it locally rather than treating it as a host-authored
picture. ⚠️⚠️ **So "I cannot move" is NOT the input wiring, and §§ 53.1 and 60.1 were both real
faults that were not this one.** The seat log is what proves it, which is the argument for having
added it.

**What has not been ruled out, in the order worth checking:**

1. ⚠️⚠️ **THE HOST IS STILL BROADCASTING SEAT 1 AND SNAPPING IT BACK.** `HostLateJoin` destroys the
   `AIController` on a joiner's seat so `CharacterMotor.HostDrivesThisBody` stops returning true
   for it, and it opens with `if (!_spawned.Add(peerId)) return;`. **A peer whose id is already in
   `_spawned` skips the whole method**, keeps the host's bot on its chair, and the host then
   transmits its own copy at 50 Hz over whatever the client submits. The client would move locally
   for one frame and be snapped back forever, which is exactly what it looks like. `_spawned` is
   only cleared by `HostPeerLeft`, so a reconnect that reuses a client id, or a second `Identify`,
   reaches it. **The host's `[NetSeat]` line settles this**: `s1:` reading `+ai` on the HOST is the
   proof.
2. **`AcceptMove` can deadlock inside its own leeway band.** It records
   `_lastAcceptedMoveAt[slot]` only on ACCEPT, so after a rejection `dt` stops growing and the
   allowance stops opening; and the correction it sends back is applied with
   `reconcileLocal: true`, which `CharacterMotor.ApplyNetworkTransform` DISCARDS when the error is
   under 1.25 m. A body 0.2 m out of sync is therefore refused by the host forever and never
   corrected on the client. **Done looks like:** stamping the time on a rejection too, and forcing
   the correction the host sends after refusing a move.
3. The free-roam window itself. Every report so far has been a screenshot with
   *"Practice freely, scores are paused. Press [R] when ready."* still on it, so the round had not
   begun in any of them.

✅ **IT WAS NONE OF THOSE THREE. IT WAS ITEM 3, THE FREE-ROAM WINDOW, AND IT IS A ONE-LINE
ASYMMETRY.** `CharacterMotor` gates steering on `CanAct()`, which is `RoundActive && !IsStunned`,
and **`CharacterMotor.RoundActive` DEFAULTS TO TRUE**. Nothing writes it until `BeginRound` or
`EndRound`, and that default is exactly what makes the pre-round window work: the DIRECTOR says
the round is not active, correctly, because nothing scores yet, while the four BODIES say they may
act, so everybody walks around the arena they are about to play in.

`RoundDirector.ApplySnapshot` stamped the director's `false` onto all four bodies, at 5 Hz, before
the first round had ever begun. The host's bodies kept the default `true`. **So the host walked
around the free-roam window and every client stood frozen**, camera still turning because that is
local and ungated. Every screenshot of this bug has *"Practice freely, scores are paused"* on it.

⚠️⚠️ **AND `[NetSeat]` IS WHAT SENT THIS LOOKING IN THE RIGHT PLACE.** `reader=True simulated=True`
ruled out §§ 53.1 and 60.1 outright: the keyboard was on the right body and the motor was
simulating it locally. Without that line the next move would have been a third guess at the input
wiring, which is where the two previous fixes had already been spent.

**Fixed** by stamping `RoundActive` onto bodies only once the match is actually running, which is
exactly when the host writes it too. All four states agree now: before the match both sides leave
the default true; in a round both are true; in an intermission `EndRound` sets the host's and this
sets the client's; after the match `MatchEnded` reaches `EndRound` on both (§ 57.1).

✅ **Item 1 is fixed anyway, on its own merits.** `HostLateJoin` opened with
`if (!_spawned.Add(peerId)) return;`, and that set exists to send the world snapshot ONCE, which
is a bandwidth question. Handing a chair over is a correctness one and was sharing the return, so
a peer already in the set kept the host's bot on its seat and the host went on transmitting its
copy over whatever that player submitted. `HostTakeSeatBackFromBot` runs unconditionally now and
only the snapshot stays gated. It is reachable: `HandleIdentify` calls it on EVERY identify.

⚠️ **Item 2 is still open.** `AcceptMove` records `_lastAcceptedMoveAt[slot]` only on ACCEPT, so
after a rejection `dt` stops growing and the allowance stops opening, and the correction it sends
back is applied with `reconcileLocal: true`, which `ApplyNetworkTransform` DISCARDS under 1.25 m.
A body 0.2 m out of sync is refused by the host forever and never corrected on the client.
**Done looks like:** stamping the time on a rejection too, and forcing the correction the host
sends after refusing a move. Not reproduced, so not fixed blind.

### 62.3 The seat log answers every seat now, not just the local one

`MatchInstaller.LogSeatWiring` prints all four chairs, because the local one answers "can I move"
and the other three answer "why is nobody else moving", which is the other half of every report so
far and used to cost a second run:

```
[NetSeat] arena installed: LocalSlot=1 spectator=False host=False allBots=False | s0:-+sim | s1:reader+sim | s2:-+bot | s3:-+bot
```

**Read it as:** `reader` is a `PlayerInputReader`, `+ai` an `AIController`, `+sim`
`IsLocallySimulated`, `+bot` the `IsBot` flag, and `MISSING` a seat the arena never built. On a
HOST every seat it drives must show `+sim`; on a CLIENT exactly one seat may.

⚠️⚠️ **`+ai` ON A JOINER'S SEAT, ON THE HOST, IS 62.2 ITEM 1 CONFIRMED.**


---

## 63 · A game could be joined exactly once per launch, and remote bodies never animated

### 63.1 ✅ FIXED: the message handlers registered once per PROCESS, not once per session

🧑 2026-08-28, and it is as exact a description of a process-lifetime flag as anybody could
write: *"so i was able to start a game when i first opened and i could join as non host"*,
*"afterwards i couldnt"*, **"i could only join a game again after restart"**.

`NetworkManager.Shutdown` **destroys** its `CustomMessagingManager`, and `StartClient` builds a
new one. Every handler registered on the old instance dies with it. Both routers guarded
registration with a plain bool that survived the shutdown:

* `MatchRpc._handlersRegistered` gated all **44** named messages;
* `NetSession._seatHandlerRegistered` gated `tp.seat.assignment.v1`.

So the second session of a process registered **nothing at all**. A client would connect, be
seated by the low-level seat message it also no longer received, and then hear no `Seating`, no
`SyncWorld`, no `StartMatch` and no `SyncUnit` for the rest of the launch.

**Fixed** by remembering the `CustomMessagingManager` the handlers are registered ON and
re-registering whenever it is a different instance. ⚠️ **Comparing the instance is self-healing
and a reset call would not be**: clearing a flag from `Stop` works only while every teardown path
remembers to call it, and remembering is exactly what failed here. `OnDestroy` unregistered,
`Stop` did not, and NGO can replace the manager without either being involved.

### 63.2 ✅ FIXED: § 62.1's navigation fired during a join

Sending a disconnected peer back to the join screen also fired for a connection that had never
completed. `OnClientDisconnectCallback` is raised for a refused approval, for a retry inside
`MaxConnectAttempts` and for a transport rebind, all while the player is still arriving, so
pressing JOIN bounced straight back out. 🧑: *"oh shit now i cant join any game wtf"*.
`NetSession._everConnected` gates it: a connection that never completed is not a disconnection.

### 63.3 ✅ FIXED: every remote body was frozen in the falling pose

🧑, from the host's screen: *"the nonhosts that join can move and interact but theyre stuck at
this pose, they cant do animations and shit"*.

`CharacterMotor._grounded` is written only by `ApplyGravity` in the local simulation, and
`FixedUpdate` returns through `StepNetworkReplica` long before reaching it, so on a replica it
stayed **false** for the whole match. `Visual.CharacterAnimator.ClipFor` asks `IsGrounded` FIRST:

```
if (!_motor.IsGrounded) return _motor.Velocity.y > 0.5f ? Jump : Fall;
```

so Walk, Sprint and Idle were unreachable for anybody you were not driving yourself. Every other
player on your screen has been standing in a fall pose for the entire life of the netcode.

**Fixed** by deriving `_grounded` on a replica from the vertical velocity, which is already
replicated, using the animator's own 0.5 threshold.

⚠️⚠️ **THAT FIX HAD ITS OWN BUG, FOUND AND FIXED 2026-08-28: the window's lower bound was -0.5,
and a standing body never transmits 0.** `ApplyGravity` holds a grounded, stationary unit at a
constant -2.0 rather than 0 so `CharacterController.isGrounded` does not flicker, and that
constant is exactly what gets replicated. A window of `(-0.5, 0.5)` sits entirely above it, so
every idle body on every replica read `_grounded` as **false**, permanently, and played Fall (or,
whenever a real jump's arc happened to line up, Jump) instead of Idle, Walk or Sprint. 🧑, from
the host's screen watching bodies that were in fact standing still: *"you could see other players
on what looks like a jumping emote"*. Fixed by widening the lower bound to the same constant
(`CharacterMotor.GroundedRestVelocityY`) minus the existing 0.5 margin, so the two call sites
that both care about "at rest" cannot drift apart again.

### 63.4 ✅ FIXED: transmit `IsGrounded` rather than inferring it

63.3 is an inference and the owner of a body knows the truth. ⚠️ **`SyncUnit` alone cannot carry
it**: the HOST's copy of a client-driven body has the same stale `false`, because `ApplyUnitMove`
does not run gravity either. It needs a bool on **`SubmitMove`** as well, which the host then
relays in `SyncUnit`. Two payloads and a protocol bump.

**Fixed**, and the inference is deleted rather than kept as a fallback. `SubmitMove` carries the
owner's `IsGrounded`; `ApplyUnitMove` stores it onto the host's copy, which is what lets
`SyncUnit` read `unit.IsGrounded` off the unit for every seat exactly as it already reads stun
and stamina; `StepNetworkReplica` assigns it straight through. `audit_wire_payloads.py` reports
`SubmitMove 5/5` and `SyncUnit 17/17`, 45 messages and 0 mismatched.

⚠️⚠️ **THE INFERENCE WAS NOT MERELY IMPRECISE, IT WAS WRONG IN THE MIDDLE OF EVERY JUMP**, which
is the one place it was load-bearing. The window read grounded for any vertical velocity in
`(-2.5, 0.5)`, and a jump crosses that whole band going up AND coming down. The note claimed
"a frame or two at the apex"; at `Balance.Gravity` it is about **0.12 s, six fixed steps, twice
per jump**. Every remote jump therefore broke into Jump → a flicker of Idle or Walk at the top →
Fall. 🧑 2026-08-28: *"joining players bug pag nag jjump"*, which is this.


---

## 64 · The bots had no face, no feet, a perfect memory and one opinion

**🧑 2026-08-28, a whole-session brief on the AI and nothing else:** *"try to thoroughly make AI
better, like figure out all aspects of it to make it feel like a human/smarter/better"*, and then,
itemised: *"make it randomly emote to taunt or when it does something cool"*; *"i want it so that
it can sometimes chose to not use skill if opportunity doesnt arise like a human"*; *"I dont want
the bots to only go after the human too (sometimes it only targets human)"*; *"let it make mistakes
bcz humans do mistakes sometimes"*; *"make it move around like a human, (jumping and sprinting and
shit)"*; *"(make sure its head turns like how a human's camera/mouse turns)"*; *"make sure they
dont just stand around sometimes and perma wait or stay near eachother without doing anything"*;
and the tier brief, *"OFC harder bots will be humanlike but less mistakes and shi but yea i want
the most humanlike bots to be normal mode bots (middle tier difficulty)"*.

§ 33 answered the 2026-08-27 version of four of these clauses. **This entry is what was left, and
two of the six turned out to be defects rather than missing features.**

### 64.1 ⚠️⚠️ THE BOTS HAD EMOTE CODE AND IT FIRED ZERO TIMES ✅

*"make it randomly emote to taunt or when it does something cool"*. There WAS bot emote code:
`AIController.TryTriggerEmote`, a celebration on `RoundDirector.Tagged`, an idle roll inside
`Loiter`, and a third call site where `Lata` reached into the AI on a knockdown. None of it had
ever produced a single visible emote.

⚠️⚠️ **A BOT CANCELLED ITS OWN EMOTE ON THE FRAME IT STARTED ONE, AND NOTHING IN EITHER FILE SAID
SO.** `EmotePlayer.Update` stops an emote on any frame `intent.MoveAxis` is non-zero, which is
correct and deliberate (its header: an emote must be abortable instantly, because it is a
self-inflicted stun). `AIController` is `[DefaultExecutionOrder(-130)]` and writes that axis every
single frame. `EmotePlayer` runs at the default 0. So `HostPlay` set `Current`, and the same frame
the bot walked and cleared it.

⚠️ **NOTHING ERRORED, AND NOBODY REPORTS AN EMOTE THEY NEVER SAW.** `BotBehaviourProbe` counted
throws, tags, skills and travel, and every one of those numbers was byte-identical on both sides of
the fault. This is the class of bug § 7.1 says the probe harness exists for, and the probe could
not see it because nothing counted the thing that was broken.

**The fix is a HOLD, and it is not a timer on the clip.** `CLAUDE.md` § 4 is explicit that emotes
end only by interruption and that there is no emote timer, and this adds none: `_emoteHoldLeft` is
how long the BOT keeps its hands off the movement keys, which is exactly what a player does when
they choose to emote. The clip still ends the way every clip ends, by the bot going back to
playing. `AIController` § THE FACE.

| | |
|---|---|
| `EmoteHoldMin/Max` | **1.1 to 2.3 s** of standing still, rolled |
| `EmoteCooldownMin/Max` | **9 to 22 s** between one bot's emotes |
| `EmoteSafeRadius` | **6.0 m**, about twice the longest tier lunge |
| `EmoteCelebrateChance` | **0.55**, before `Flair` and `Showmanship` scale it |
| `EmoteTauntChance` | **0.05 per second**, ⚠️ a RATE, multiplied by the frame time |
| `EmoteStartGrace` | **0.25 s** before the hold will believe the clip failed |

⚠️⚠️ **THE SAFETY GATE IS RE-ASKED EVERY FRAME OF THE HOLD, WHICH IS THE MOST HUMAN PART OF IT.**
A celebration that becomes dangerous is abandoned mid-clip. `SafeToEmote` refuses a bot that is
taggable, inside the chalk, mid-wind-up, within 6 m of the taya, or defending while anybody is
taggable; between rounds it passes unconditionally, which is the one moment that is safe by
construction and is where most of them actually land.

⚠️ **THREE SECOND PATHS WERE DELETED RATHER THAN KEPT.** `Lata` no longer reaches into
`AIController` on a knockdown (it skipped the safety gate entirely, so a bot could be told to dance
inside the chalk with a taya on it, and a rules object had no business knowing what a bot is); the
idle roll came out of `Loiter`, where four plans re-entered it every frame so a `0.15f` written on
that line was a chance per frame per plan and the real rate was unknowable from reading it; and the
request goes through `EmotePlayer.Request`, the entry the emote wheel uses, rather than `HostPlay`.

⚠️ **THE START GRACE IS A NETWORK ROUND TRIP, NOT A FEEL VALUE.** In single player `Request`
reaches `Play` on the same line. On a listen host it does not: `HostPlay` sends
`RequestEmoteServerRpc`, which broadcasts `PlayEmote` with `SendNamedMessageToAll`, and Netcode
delivers that on its own update. `IsEmoting` is false for a frame or two afterwards, so without the
grace the hold would end immediately, the bot would walk, and the clip would be cancelled by the
movement the instant it arrived: **the original bug, reintroduced through the wire instead of
through the execution order.**

### 64.2 ⚠️⚠️ SEPARATION WAS APPLIED IN `Goto` AND NOWHERE ELSE ✅

*"stay near eachother without doing anything"*. `Separation()` is called from exactly one place,
`Goto`, so it governed bots that were **travelling** and did nothing whatever for bots that had
**arrived**. `Stalk`, `Cover` and `Guard` all end in `if (_arrived) Loiter(intent)`, and `Loiter`
is a 0.45 m leash: two seats whose goals happened to be close stopped close, loitered close, and
had no term pushing them apart for as long as neither plan changed.

It reaches the loiter now at `LoiterSeparationWeight` **0.35**, deliberately under the travelling
`SeparationWeight` 0.65. ⚠️ A push at the travelling weight would spend every shuffle fighting the
0.45 m leash and the pair would visibly vibrate apart instead of drifting.

### 64.3 ⚠️ NOTHING MEASURED HOW LONG A WAIT HAD BEEN GOING ON ✅

*"dont just stand around sometimes and perma wait"*. The loiter is a shuffle in place and it is
correct; a bot waiting for an opening should look like it is waiting. What was missing is anything
noticing the WAIT ITSELF had stopped going anywhere.

`BoredomSeconds` **6.5 s** without covering `BoredomProgressMetres` **1.25 m** shifts this bot's
home bearing by `BoredomShiftRadians` **1.15**, then settles for `BoredomSettleSeconds` **4.0 s**.

⚠️ **MEASURED ON TRAVEL, NEVER ON THE PLAN.** A taya guarding a can nobody is attacking is playing
perfectly and holding one plan; a bot can also change plan every tick while standing still.
Distance covered is the only honest question. ⚠️ **AND THE PROGRESS BAR SITS ABOVE `LoiterLeash`**
0.45, or the shuffle would reset the clock forever, which is the exact stalemate this breaks.

⚠️ **IT MOVES A BEARING, NOT A PLAN.** Overriding the plan would fight the planner and reproduce
the flip-flopping § 33.4 records. `HomeBearing` is a property now and both readers go through it;
a shift applied to `DoStalk` alone would move a stalker without moving where it throws from.

### 64.4 ⚠️⚠️ THE TIER'S `Mistake` REACHED EXACTLY ONE DECISION ✅

*"let it make mistakes bcz humans do mistakes sometimes"*. `Mistake` is read in one place,
`DoWindup`'s `_blundering`: scatter doubles, the power margin drops to 1.0 and the lane check is
skipped. **So a bot could only ever err while charging a throw**, which is a few seconds of an
attacker's round and none at all of a taya's. Every chase, every fetch, every plan change and every
cast was perfect.

**A lapse is a LATE answer and never a wrong one**, and that distinction is the whole design.
Choosing the second-best plan on purpose reads as a broken bot, because the error is visible in the
decision and a watcher sees the body walk the wrong way for no reason. Slowing the decision is
invisible in the choice and visible only in the timing, which is what being outplayed by a person
looks like from the other side.

`LapseSeconds` **0.42** (about one Normal think tick) at `LapseSlowdown` **2.4x**, rolled once per
think tick at the tier's `Lapse` reduced by up to half by the bot's own `Focus`. It slows the think
interval, every reaction gate, and `Observe`'s belief lag. ⚠️ **IT NEVER FREEZES THE BODY**: the bot
keeps walking its last plan and simply does not notice the board has moved. A lapse that stopped
the legs would be 64.3's own complaint, added deliberately.

⚠️ **ROLLED PER THINK TICK, NOT PER FRAME**, so the rate cannot depend on the frame rate. § 17 is
what happens when a bot number does.

### 64.5 ⚠️ NO BOT HAD EVER LEFT THE GROUND ON PURPOSE ✅

*"make it move around like a human, (jumping and sprinting and shit)"*. Sprinting was § 33.3.
Jumping was not: before this, `Verb.Jump` appeared in `AIController` **exactly once**, in the mash
that gets a tripped bot off the floor.

⚠️ **IT BUYS NOTHING, WHICH IS EXACTLY WHY A PERSON DOES IT.** `CharacterMotor.ApplyGravity`
charges no stamina for a jump and no rule rewards one, so hopping while you wait is fidgeting with
the one verb that is free. `HopIntervalMin/Max` **2.6 to 11.0 s**, `HopChance` **0.55** scaled by
the tier's `Hops` and the bot's `Springiness`. ⚠️ The interval range is wide on purpose: four bots
hopping on a shared beat is worse than four that never hop, because it announces that one clock
drives all of them.

⚠️ **REFUSED IN THE THREE MOMENTS A HOP COSTS SOMETHING**, and each one is a mechanic rather than a
judgement about when jumping looks silly: during `Reset` (the reset channel is the game's one held
button and zeroes itself the instant it goes false), during a wind-up, during an emote (a jump is
`EmotePlayer`'s own cancel condition), and while taggable (an airborne body cannot change
direction, and the retrieval is the only window where that matters).

### 64.6 ⚠️⚠️ THE BODY TURNED AT ONE SPEED FROM THE FIRST FRAME OF A TURN TO THE LAST ✅

*"(make sure its head turns like how a human's camera/mouse turns)"*. § 31.1 capped the turn at
`BodyTurnDegPerSecond` 520 and that fixed the reported fault (*"they can look straight behind them
and turn in 0.1 seconds"*). It replaced it with a different tell: a **constant** rate, no ramp at
either end, stopping dead on the mark. Nothing physical moves like that.

⚠️⚠️ **AND THE SPEED SCALING WITH THE ANGLE IS THE HALF THAT READS.** A person makes a 15°
correction slowly and a 170° check fast, so a single rate is wrong at both ends: at 520 the
correction snaps and only the big check ever looked right.

| | |
|---|---|
| `BodyTurnReachSeconds` | **0.18 s**, the time the hand wants any turn to take |
| `BodyTurnSettleDegPerSecond` | **180**, the floor, a hand already tensed |
| `BodyTurnAccelDegPerSecond2` | **3200**, floor to ceiling in about 0.11 s |
| `BodyTurnDegPerSecond` | **520**, unchanged |

⚠️ **NOTHING GOT FASTER.** A 180° reversal wants 1000°/s at 0.18 s and clamps to the shipped 520,
so the longest turn in the game runs at exactly the old cap;
`TheLongestTurnStillSaturatesTheShippedCeiling` asserts it. Only turns under about 94° behave
differently, and those are the ones that used to snap.

⚠️⚠️ **THE FLOOR IS NOT A START FROM ZERO, AND A GLANCE IS WHY.** Accelerating from rest makes
every short press worthless, and `GlanceSeconds` is 0.09 s: from zero that is about **12°** and the
look-around § 33.3 added would have quietly stopped happening. With the floor it is about **29°**,
against 47 under the flat rate. ⚠️ `AGlanceTurnsALongWayWithoutFinishingAReversal` used to multiply
`GlanceSeconds` by the CEILING, which is arithmetic that died with the flat rate; it computes the
area under the ramp now, or it would have gone green while the feature was broken.

⚠️ **IT IS IN THE MOTOR AND APPLIES TO GAMEPAD HUMANS TOO.** `CLAUDE.md` § 4 forbids a second
movement model in as many words. Mouse-aimed players are untouched: `Steer` returns before this on
their branch, because their hand IS the curve.

### 64.7 ⚠️ FOUR IDENTICAL SCORERS AGREE, AND THAT IS THE RESIDUE OF § 33.1 ✅

*"I dont want the bots to only go after the human too (sometimes it only targets human)"*. § 33.1
deleted the seat-order `foreach` that singled somebody out **by construction**, and the feeling was
reported again a build later, softer. The cause this time is **agreement, not order**: all four
seats score one board with one identical set of weights, so whoever the score favours is favoured
by every bot at once, and a person plays differently from three bots in exactly the terms the score
reads (deeper into the chalk, holding a tsinelas longer, getting caught out).

**So the fix is to make the four disagree on ties.** `AiPersonalityRoll.RivalPick` gives each seat a
rolled favourite rival worth `TagRivalryWeight` **0.45**.

⚠️⚠️ **THE SIZE IS THE WHOLE SAFETY ARGUMENT AND IT IS ASSERTED.** 0.45 sits under
`TagSwitchMargin` 0.75, so a grudge can never drag a taya off a chase it is already winning (which
would be § 33.1's taya running down the middle of two attackers and catching neither), and it is a
fifth of `TagHelplessBonus` 2.5, so a body already on the floor is still chased first, every time.
It decides ties and near ties, and the close ones are all this ever was.

### 64.8 ⚠️ A SHY SLOT WAS SHY FOR EIGHT ROUNDS RUNNING ✅

*"i want it so that it can sometimes chose to not use skill if opportunity doesnt arise like a
human"*, which § 33.2 and `SkillAppetite` had already largely answered. The residue:
`SkillAppetite` is rolled once per **seat** and read for the whole **match**, so "seat 2 hardly ever
ults" was true in every round of a Hero Strike match. That stops reading as a person and starts
reading as a dead key.

`AppetiteRoundSwing` **0.35** drifts each slot's appetite around its seat baseline once per round.
⚠️ **IT STILL DOES NOT ROLL A REFUSAL**: everything `SkillAppetite` says about a long conviction
window beating a dice roll holds unchanged. It only makes how patient a bot feels about one power a
fact about a round instead of about a match. ⚠️ Asserted under 0.5, because at or above that the
drift is bigger than the roll it drifts around and the seat's personality stops existing.

### 64.9 Normal is the most humanlike tier, and two of its rows are deliberately not monotonic

*"i want the most humanlike bots to be normal mode bots (middle tier difficulty)"*.

| | Bata | Normal | Astig |
|---|---|---|---|
| `Flair` (emote appetite) | 0.80 | **1.00** | 0.55 |
| `Hops` (idle fidgeting) | 1.15 | **1.00** | 0.55 |
| `Lapse` (per think tick) | 0.10 | 0.045 | 0.012 |
| `Mistake` (unchanged) | 0.30 | 0.10 | 0.02 |

⚠️⚠️ **`Flair` AND `Hops` PEAK IN THE MIDDLE AND `DifficultyIsMonotonicWhereItShouldBe`
DELIBERATELY DOES NOT COVER THEM.** Every other row in the tier table is monotonic because every
other row measures SKILL, and skill has an order. Sociability does not: a tournament player
celebrates less than a casual one because a celebration costs position, and a child celebrates less
than a casual one because they are still working out where to stand.
`NormalIsTheMostHumanTierAndThatIsNotMonotonic` asserts the peak, so nobody later "fixes" the table
to look consistent with its neighbours.

⚠️ **`Lapse` IS AN ERROR ROW, SO IT IS MONOTONIC LIKE `Mistake`.** And it is not zero at Astig, for
the reason `Mistake` is not zero there either: a bot that never looks away and never plays to the
crowd reads as a cheat rather than as a hard opponent.

### 64.10 What was measured, and the one number that was nearly misread

⚠️⚠️ **THE FIRST READING OF THIS PASS LOOKED LIKE A 60 PER CENT COLLAPSE IN ULTIMATE USE, AND IT
WAS A STALE BASELINE.** § 33.6's table reports 37 and 39 ultimates; this build reports 14 and 15,
which is past § 16's smallest resolvable effect and reads as a serious regression. It is not.
**§ 33.6 was measured on 2026-08-27, before §§ 36 to 63 landed, and § 41 changed the ultimate meter
to count events.** `CLAUDE.md` § 7.1 says in as many words not to compare an old report against a
new one, and this is what happens when you do.

**So the arm below is a real A/B: `HEAD~1` of this branch against `HEAD`, both run today, one
seeded run an arm.**

**Hero Strike, Eskinita** (50,115 frames, 835.3 s simulated, `match in progress at exit: False`)

| | baseline | this pass |
|---|---|---|
| throws | 180 | 178 |
| retrievals | 177 | 172 |
| tags | 117 | 113 |
| lata knocks | 95 | 97 |
| lata restores | 108 | 107 |
| unretrieved penalties | 0 | 0 |
| skill uses | 33 | 31 |
| ultimate uses | 15 | 14 |
| seat travel, m | 1394 / 1377 / 1301 / 1447 | 1391 / 1368 / 1359 / 1442 |

**Hero Strike, Ilalim ng Tulay**

| | baseline | this pass |
|---|---|---|
| throws | 163 | **178** |
| retrievals | 157 | **175** |
| tags | 101 | 101 |
| lata knocks | 88 | 89 |
| skill uses | 29 | 32 |
| ultimate uses | 14 | 15 |
| seat travel, m | 1407 / 1344 / 1279 / 1439 | 1343 / 1358 / 1392 / 1413 |

**Classic, Eskinita**: knocks 40 to 45, tags 64 to 60, throws 84 to 88, retrievals 81 to 84,
restores 47 to 50, travel 634 / 639 / 717 / 682 to 659 / 646 / 692 / 626.

✅ **EVERY ROW IS INSIDE § 16's NOISE FLOOR, AND THAT IS THE RIGHT RESULT RATHER THAN A
DISAPPOINTING ONE.** § 33.6 makes the identical point about its own middle column: none of this was
meant to make bots busier. A pass about how bots READ that moved the throw count would be a pass
that had changed the game underneath it. The Ilalim throws and retrievals are up about a tenth,
which is inside the floor and in the good direction.

### 64.11 ⚠️⚠️ THE PROBE NOW COUNTS THE TWO THINGS IT COULD NOT SEE ✅

The whole of 64.1 is a feature that shipped, played, and did nothing, while every number in
`BotBehaviourProbe` stayed byte-identical. That cannot be allowed to be true a second time.

`Tally` counts **emotes** (off `EmotePlayer.EmoteStarted`, subscribed per seat inside the sample
loop, because the seats are rebuilt between rounds) and **hops** (off the resolved body: grounded
going false with an upward velocity).

⚠️ **HOPS ARE COUNTED OFF THE BODY AND NEVER OFF THE KEY.** A tripped bot MASHES jump to get up and
those presses are refused by `CanAct`; counting the key would report hundreds of fictional hops a
match.

Measured across two runs of this build:

| | Classic / Eskinita | HS / Eskinita | HS / Ilalim |
|---|---|---|---|
| emotes | 6, 8 | 22, 12 | 18, 16 |
| hops | 75, 92 | 234, 254 | 239, 248 |

That is about **0.4 to 0.7 emotes per seat per round** and **one hop per seat every 13 seconds**.

⚠️ **THE ASSERTION IS A FLOOR OF ZERO, NOT A BAND, AND THE 22-TO-12 SWING IS WHY.** The failure
mode this guards is *"this does not happen at all"*, and a floor that tried to pin the rate would
fail on the dice instead, which the long note on `deadLoopFloor` explains costs a reader more than
no assertion at all. There is a ceiling as well, at four per seat per round: the other failure is a
bot that stops playing to dance.

### 64.12 Still open

* ⚠️ **THE PROBE STILL ONLY EVER SEATS DANTE AND ZACK**, which § 33.8 already records. Nothing in
  this entry is kit-specific, so it does not add to that debt, but the four unexercised kits are
  still unexercised.
* ⚠️ **THE HOP RATE IS MEASURED BUT NOT A/B'D FOR FEEL.** One hop per seat every 13 seconds is what
  the shipped constants produce; whether that is the right amount is a human judgement and the
  number is written down here so it can be moved against something.
* ⚠️ **THE EMOTE COUNT SWINGS 22 TO 12 BETWEEN TWO RUNS OF ONE BUILD.** That is the safety gate
  depending on the match state rather than an unseeded input, but it means the rate cannot be
  tuned from one run. § 16's arithmetic applies: three runs an arm for anything worth 20 per cent.
* ⚠️ **`Ilalim` REPORTED 2 UNRETRIEVED-SLIPPER PENALTY SECONDS AGAINST A BASELINE OF 0.** Two
  seconds of fine across an 834 s match is nothing, and the ceiling is 1200, but emote holds and
  boredom shifts are two new ways for a bot to not be walking toward its tsinelas. Worth a look if
  that row ever climbs.

## 65 · Hosting or joining a SECOND time in one launch was refused, silently

🧑 2026-08-28: *"it sometimes says failed to join online host via relay. it's consistent because
sometimes i get it to work"*, and *"i cant also seem to host in lan. it just says starting lan
session.."*.

### 65.1 ✅ FIXED: `NetworkManager.Shutdown()` does not shut anything down

**All four start paths in `NetSession` opened with the same two lines** — `if (_nm.IsListening)
Stop();` and then a start on the very next statement. NGO's `Shutdown()` only sets a flag;
`ShutdownInternal` runs later, from the network update loop at `PostLateUpdate`. `CanStart`
refuses outright while `IsListening` is still true. **So the start in the same frame was rejected
every single time.** Measured with a probe before the fix:

```
straight after Stop() (same frame): IsListening=True ShutdownInProgress=True
SAME-FRAME restart returned False; status='failed to start hosting'
```

⚠️⚠️ **THE FIRST START OF A PROCESS ALWAYS WORKED, WHICH IS WHY THIS READ AS RANDOM.** Nothing
was listening yet, so `Stop()` was never reached. It failed only once a session was already up:
backing out of a lobby and hosting again, or retrying a join after one that did not take. That is
exactly "sometimes I get it to work".

⚠️ **The two relay paths were hit less often, not exempt.** They happen to `await` a sign-in and
an allocation between the stop and the start, so a frame usually passes by luck; a cached sign-in
and a fast allocation both continue synchronously and then they fail identically.

**Fixed** with one shared `NetSession.EnsureStoppedAsync()` used by all four paths, which stops
and then waits real frames (bounded at `ShutdownWaitFrames = 12`, and it warns rather than hanging
if the transport is still up). `StartHost`/`StartClient` became `StartHostAsync`/`StartClientAsync`
so the wait is expressible; `ConvertedMultiplayerSetup` and `NetBootstrap` await them.
**Verified** by `SessionRestartTests`, which hosts twice without an intervening `Stop()` and
asserts the second one; it fails against the old code and passes against the new.

### 65.2 ✅ FIXED: the relay paths never got the transport's generous timeouts

`Configure` set `ConnectTimeoutMS = 2000`, `MaxConnectAttempts = 12` and
`DisconnectTimeoutMS = 30000`, with a comment about venue wifi and Philippine home connections.
**Only the two LAN paths could call it**, because it opens with `SetConnectionData`, which resets
the transport protocol to plain UnityTransport and would undo the `SetRelayServerData` the relay
paths had just done. So relay ran on whatever the last LAN attempt left behind, or on UTP's own
defaults in a process that had never touched LAN — a **1000 ms** connect timeout, on the one route
in this game that goes through a datacentre rather than across the room. The more latent path had
the less patient settings. Split into `ConfigureTimeouts()` and called from all four.

### 65.3 ✅ FIXED: every failed start threw away the reason it had just worked out

`NetSession` writes a precise status on the way out of each failure ("relay allocation failed:
...", "invalid relay join code", "cannot go online: no network route", "failed to start hosting")
and **every caller in `ConvertedMultiplayerSetup` overwrote it with one fixed sentence.** A dead
join code, a rate-limited lookup, a refused port and a machine with no internet were all the same
line on screen. `Reason(headline)` now appends the session's own status, which is the same fix
that file's `LastDisconnectReason` block already made for disconnects.

### 65.4 ⚠️ OPEN: the online browser can offer a lobby whose relay allocation is gone

`ServerQuery.ResolveCodeAsync` returns `Found = true` off a UGS Lobby record, and the record
outlives the allocation: a host that force-quits stops heartbeating but the row survives until the
service expires it. `JoinAllocationAsync` then throws and the join fails for a lobby the browser
had just listed as live. 65.3 makes it *say* so rather than fixing it. **Done looks like:** a
failed `JoinAllocationAsync` deletes or hides the stale row, and the browser stops listing a lobby
whose last heartbeat is older than the service's expiry.
⚠️ `ResolveCodeAsync` returning `Found = true` with an EMPTY `RelayCode` is the same fault's other
face and should be treated as not-found rather than handed to `StartRelayClient`.

---

## 66 · Joining bounced the view about, and rejoining a running match was impossible

🧑 2026-08-28: *"when a non host player tries to join, it just bounces back and forth a lot of
times"*, and *"the sequence should be like this: when a player quits, an ai automatically takes
over. after rejoining, the player takes full control ... what happens when you try to rejoin is
that you'll only get ported back to the lobby with no way of joining back"*.

⚠️ **Measured on two REAL peers**, not reasoned about: a macOS player built with
`GameBuilder.BuildMac`, run twice headlessly with `-tp-host` and `-tp-join` over loopback. That
harness is the cheapest honest way to see any of this and is worth reaching for again.

### 66.1 ✅ FIXED: one join applied the seat three times and rebuilt it six

The seat is announced by **two protocols** (`tp.seat.assignment.v1` and `Seating`, `docs/TODO.md`
§ 60), and neither application was idempotent. One join measured:

```
[Net] connected as seat 2          <- ApplyAssignedSeat
[NetSeat] seat changed: ...        <- x2, the raise was written twice
[Net] seated in slot 1             <- SetLocalSeating, same chair
[NetSeat] seat changed: ...
[Net] connected as seat 2          <- ApplyAssignedSeat AGAIN, same chair
[NetSeat] seat changed: ... x2
[Net] connected as seat 2          <- and again, seconds later
[NetSeat] seat changed: ... x2
```

Every `SeatingChanged` makes `MatchInstaller` move the camera, the HUD, the input reader and the
`PlayerInputReader` onto the chair again. Six of those for one join is the *"bounces back and
forth"*. ⚠️ **The seat number was never wrong**, which is why nothing caught it: a test asserting
`LocalSlot` passes against the bug. **The count is the assertion.**

Two causes, both fixed: `ApplyAssignedSeat` raised `SeatingChanged` on two consecutive lines (a
duplicated line, not a race guard — the paragraph above it argues for raising it here *as well as*
in `SetLocalSeating`, which is one raise), and neither method checked whether anything had actually
changed. Both now drop a repeat of the chair they already hold.

⚠️⚠️ **`_seatApplied` IS A SEPARATE FLAG AND CANNOT BE FOLDED INTO `LocalSlot`.** `LocalSlot`
defaults to **0** and 0 is a real seat, so "has a seat been applied yet" is unanswerable from the
number alone: without the flag the host's own first announcement of seat 0 reads as a no-op, is
dropped, and the arena is never wired up at all. `SeatAnnouncementTests` has a case for exactly
that.

**After the fix, remeasured on the same two peers: 1 application, 1 rebuild**, on both a first
join and a rejoin.

### 66.2 ✅ FIXED: the seating packet overtook the map and the mode

`HandleIdentify` sent `Seating` **before** `SyncMode`, `SyncMap` and `SyncDiff`. `OnSeatingMsg` is
not just a seat: when its `inProgress` flag is set it calls `UI.SceneFlow.StartMatch()`, which
loads **`SceneFlow.SelectedMap`**. Arriving first, it fired on a rejoining player whose
`SelectedMap` and `SelectedMode` were still whatever their own menu last held, so a player
rejoining a Hero Strike match on Ilalim ng Tulay loaded Classic on Eskinita, alone, and the map
that arrived one line later had nothing left to correct.

⚠️ **Named messages are reliable-sequenced, so the order they are written in is the order they are
processed in.** This is the same rule `HostStartMatch` already states three paragraphs of reasoning
for; the mid-match path was the one place that broke it. The seat now goes last.

### 66.3 ✅ FIXED: a rejoining client could be stranded in the lobby

Two independent one-shot routes lead into a running match: `ConvertedMultiplayerSetup.Join` loads
the lobby, and `OnSeatingMsg` loads the arena. Both are `SceneManager.LoadScene`, both are deferred
to the end of the frame, and **the seating packet can arrive before the lobby scene has finished
loading**. When it does the arena load is queued first and the lobby second, the lobby wins, and
`OnSeatingMsg` has already fired and will not fire again. Nothing on that screen leads in: START is
host-only and the seat rows are greyed out *because* a match is in progress. That is *"no way of
joining back"*.

**Fixed** by making the lobby self-correcting: `ConvertedMatchSetup.RejoinRunningMatch` runs on
arrival and, for a non-host peer whose `LobbySession.MatchInProgress` is set, goes straight to the
arena. Whichever of the two happens last now works. Spectators go too; the host is excluded,
because it sits in this lobby legitimately between matches.

### 66.4 ✅ FIXED: a `-tp-host` host never told its lobby the match had started

`LobbySession.MatchInProgress` had exactly one caller, `MatchRpc.HostStartMatch`, reached from the
lobby's START button. A host launched with `-tp-host` goes straight into the arena and never passes
through that screen, so the flag stayed **false** for a host that was visibly playing. It is the
switch behind three rules: `Depart` only HOLDS a dropped player's seat while it is set (so a player
who quit lost their chair instead of leaving a bot in it), `RuleOnArrival` answers Refuse rather
than Spectate, and the `inProgress` flag on the seating packet is what sends a joining client into
the arena. `NetBootstrap` now calls `Lobby.StartMatch()` before loading the map.

⚠️ `Lobby.StartMatch()` rather than `MatchRpc.HostStartMatch()`: the latter also broadcasts to every
peer and fires `OnMatchStarted`, which is right when a lobby full of people presses go and wrong for
a host that is alone and loading the arena itself on the next line.

### 66.5 ⚠️ OPEN: the AI takeover is 30 seconds late on a hard quit

`Configure`'s `DisconnectTimeoutMS = 30000` is deliberate (§ venue wifi), but it also means a player
who force-quits leaves a **driverless body for a full 30 seconds** before `HostPeerLeft` runs and
the bot takes the chair. Measured: the host logged nothing at all for 12 s after the client was
killed, and `1 connected` only at ~30 s. A clean quit through the pause menu sends a real
disconnect and is immediate; this is only the crash/kill path. **Done looks like:** either a
heartbeat that fails faster than the transport timeout, or the pause-menu quit path being the one
the game teaches, with the timeout left alone for genuine stalls.

⚠️ **STILL OPEN ON PURPOSE AFTER THE HARRYDAKS PASS.** The takeover machinery in `HostPeerLeft`
is correct; only its TRIGGER is late, and every way to make it fire sooner is a judgement about
how long a stalled player on venue wifi keeps their chair. `_lastAcceptedMoveAt` already gives
per-seat liveness at 50 Hz, so the mechanism is a few lines; the THRESHOLD is a design decision
and picking one unattended would have silently traded this bug for bots stealing seats from
lagging players, which § ConfigureTimeouts spends a paragraph arguing against. It needs a number
from the team, not from a merge.

---

## 67 · What the HARRYDAKS merge was hiding, found by building it

`HARRYDAKS` is `fix/join-bounce-and-rejoin` (which already contained every other `fix/*` branch
and the `claude/*` one by ancestry) merged with `integration/ui-batch-on-ilalim` and `main`.

### 67.1 ⚠️⚠️ A "RECORD ONLY" MERGE MAKES GIT DROP LATER WORK WITHOUT A CONFLICT

`c1e0b80` on this line was a merge that kept *"this branch's StarRayX-excluded tree"* — it
recorded `integration` as merged while deliberately taking none of its content. Git then treats
that revert as this branch's OPINION. Anything `integration` had not touched AGAIN since is
"unchanged on one side, deleted on the other", which resolves silently to deleted.

**It cost 11 whole files and 12 files' worth of hunks, and only two of them ever raised a
conflict.** `Fxaa.shader`, `WorldOutlineProbe.cs`, `OutlineWeldTests.cs` and five `.meta` files
vanished with no marker at all; a lost `.meta` would have reassigned the shader's GUID and broken
every material pointing at it. Inside files, `VfxRenderTag` was rolled back from `Own(go, mat)` to
`Attach(go)` while its caller kept calling `Own`, and `ConvertedSettingsPanel` ended up with
`integration`'s generic `BuildDropdownRow` SIGNATURE on top of the old hardcoded body.

⚠️ **A FILE-LEVEL SUPERSET CHECK IS NOT ENOUGH, WHICH IS THE LESSON.** The tree passed "every
path in both parents is present" and still did not compile. What actually settles it is
`git diff <merged> <parent>` in BOTH directions, reading every hunk the merged tree is missing.

### 67.2 ✅ FIXED: LAN discovery died on the first beacon it ever received

See the commit; `LanBeacon.OnReceive` ran Unity calls on the socket thread and the throw landed
before the `BeginReceive` re-arm, so discovery stopped permanently on packet one while the host
went on advertising correctly. That is the whole of *"di pa siya gumagana rn pero nakakapag host
na"*. ⚠️ **NO TEST COULD HAVE CAUGHT IT**: every `TryParsePayload` case calls it from the test
thread, which is the main thread, where the illegal call is legal.

### 67.3 ✅ FIXED: three shaders were reachable in the Editor and stripped from the build

`TumbangPreso/WorldOutline`, `/VolcanicRock` and `/ButtonOutline` are reached only by
`Shader.Find` and were not in `m_AlwaysIncludedShaders`. **All eight of this project's
name-loaded shaders depend on that list — not one is referenced by a `.mat`, prefab or scene.**
`PostAntiAlias.cs` states the rule; nothing enforced it. `tools/audit_shader_stripping.py` does
now, and the Editor cannot reproduce the failure it guards.

### 67.4 ✅ FIXED: the LAN beacon advertised `InProgress = false` for a whole match

`PublishLobbyCounts` ran on connect and on disconnect only, and a match STARTING is neither, so
the browser drew a running game as "IN THE LOBBY" and `LanEntry.IsJoinable` — which opens with
`!InProgress` — offered JOIN on it. Split into `RefreshBeaconCounts` (four local writes, every
frame while hosting) and the UGS push (still edge-triggered, because it is a network call).

---

## 68 · The lobby is a form, and it should be a room ⚠️ OPEN, PLANNED 2026-08-28

🧑, 2026-08-28, with a PUBG lobby screenshot beside a capture of ours: *"i want our boring ass
lobby to look like this"*, *"i want multiplayer to go straight to lobby and thats where u can
join"*, *"all the shit like map mode bots character join yk everything is togglable"*, *"its okay
if character select still makes u go to a dif screen. i dont want character select to be
touched"*, *"make sure u dont break lobby"*, *"dont delete old huds and ui tho keep them incase ur
shit turns ugly"*, *"organize everything and make sure theres no redudnant shit"*, *"make sure
shit works like end to end"*.

**The reference is the LAYOUT, never the skin.** PUBG's lobby is grey military chrome and this
game's brand is painted wood, cream and amber (`UiTheme`, and `Art_Direction.md` § 1 says the
palette file is the only place a colour is named). What is borrowed is the ARRANGEMENT: the room
is the picture, the cast stands in it, the controls are small furniture pushed to the edges.

### 68.0 The four decisions, taken 2026-08-28 before any code

Asked and answered rather than assumed, because each one changes what gets built:

| Question | Answer |
|---|---|
| Network state on arriving at the lobby | **Auto-host on LAN.** § 68.5 |
| Which of the two references | **Hybrid**: PUBG Mobile's chrome, PC PUBG's four-person line-up. § 68.7 |
| Chat | **Lobby AND in-match, on this branch.** § 69, and it is the only wire change |
| Nav bar | **`PRACTICE ǀ MULTIPLAYER` tabs.** § 68.7 |

### 68.1 What the two screens actually are today

`ConvertedMatchSetup` is ALREADY both screens. It draws `PRACTICE MODE` offline and `LOBBY`
when `NetSession.IsNetworked`, off one `isNetworked` branch in `Refresh()`. Nothing has to be
invented to make multiplayer land here; the screen has been the lobby since § 55.

What lives on the OTHER screen, `ConvertedMultiplayerSetup`, is only the four ways IN: host
online (Relay), host LAN, a join address/code field, and the two browsers (`LanBeacon` and
`ServerQuery`). Those are the only things that have to move.

⚠️ **THE FOUR SELECTORS ALREADY REPLICATE AND THE READY TALLY ALREADY ARRIVES.** Map
(`SelectMapServerRpc`), mode (`SelectModeServerRpc`), difficulty (`SelectDifficultyServerRpc`),
picks (`SelectLobbyPickServerRpc`), the seat table (`LobbySeatInfo`, which carries `Name`,
`Occupied` and `CharacterPick`) and the ready tally (`OnLobbyReadyChanged`) are all on the wire
already. **The lobby redesign is therefore a pure client-side reskin.**

### 68.2 ⚠️⚠️ THE LOBBY WORK ADDS NO WIRE CHANGE. CHAT IS THE ONLY ONE, AND IT BUMPS THE PROTOCOL

Everything the new lobby draws is already replicated (§ 68.1), so § 68's own work moves
`NetSession.ProtocolVersion` not at all. **§ 69's chat does**, 5 → 6, because a chat line is a
named message that has never existed.

⚠️ **THAT MEANS BOTH LAPTOPS MUST BE REBUILT FROM THIS BRANCH.** § 59.4 records what a bump
costs: a peer on a different protocol is REFUSED at approval, by design, because a build that
"mostly works" presents as wrong characters and frozen bodies. § 59.2 is what makes the refusal
say so out loud instead of hanging. **Land and verify § 68 with the protocol still at 5, bump it
once in § 69, and never twice.**

⚠️ If a step in § 68 seems to need a new field, that step is wrong.
`tools/audit_wire_payloads.py` is the check, and `audit_request_call_sites.py` catches the other
half (a protocol added and never called).

### 68.3 ⚠️⚠️ THE OLD CHROME IS KEPT AND SWITCHABLE, NOT DELETED

🧑: *"dont delete old huds and ui tho keep them incase ur shit turns ugly"*.

`LobbyStyle` is one enum with two values, read once in `ConvertedMatchSetup.Wire()`:

* `Classic`, the authored converted panels exactly as they are today. Nothing new is drawn.
* `Street`, the new arrangement. Default.

⚠️ **THE OLD NODES ARE DEACTIVATED, NEVER DESTROYED**, and the new chrome is BUILT FROM CODE in
`Wire()` the way `BuildRightPanelNetwork` already builds the address and code rows. So
`MatchSetup.unity` barely changes on disk, `Classic` is a working screen at every commit, and
reverting is a one-line default rather than git archaeology.

⚠️ **`MultiplayerSetup.unity` AND `ConvertedMultiplayerSetup.cs` STAY ON DISK AND STAY IN THE
BUILD ORDER.** They cost one scene entry and they are the fallback if the in-lobby join panel
turns out worse. Only the LINK from `ConvertedModeSelect` is removed. `UiClickProbe`,
`ScreenshotTool` and `UiRuntimeShots` keep passing because the scene still exists.

### 68.4 ⚠️⚠️ RE-SKIN, DO NOT RENAME

`ConvertedScreen` finds every control by the name Godot gave it and `Node()` logs an error on a
miss. `SeatButton0..3`, `PrimaryButton`, `StartButton`, `BackButton`, `MapValueLabel`,
`ModeValueLabel`, `DifficultyValueLabel`, `CharacterButton`, `BannerLabel`, `DetailLabel`,
`SeatHeading`, `SeatHint`, `StatusLabel`, `MapPreview` and `CharacterSelectPanel` keep their
names and their handlers. **Renaming one is how this breaks silently**, and it is the exact
failure that class header exists to describe.

### 68.5 The navigation change, and the third lobby state it creates

`ConvertedModeSelect` sends MULTIPLAYER to `SceneFlow.MultiplayerSetup`. It goes to
`SceneFlow.MatchSetup` with `Networked = true`, **and the lobby auto-hosts on LAN the moment it
arrives.**

That gives the screen a state it has never had: networked, but no transport yet. Today
`IsNetworked` is false until somebody hosts or joins and the screen reads that as practice.

| State | Headline | Selector rows | Join panel |
|---|---|---|---|
| Practice (offline) | `PRACTICE MODE` | all live | hidden |
| Lobby, host bind failed | `LOBBY` + the reason | live, local only | OPEN |
| Lobby, hosting | `LOBBY · YOU ARE HOSTING` | live, replicated | available |
| Lobby, connected | `LOBBY · CONNECTED` | greyed, host picks | LEAVE |

⚠️⚠️ **A REFUSED PORT BIND MUST FALL BACK, NEVER HARD-FAIL.** Auto-hosting binds 8910 the moment
somebody presses MULTIPLAYER, and the usual reason it is already bound is the player's own second
copy of the game. The screen drops to row 2 with `NetSession.Status` on the status label and the
join panel already open, so the path OUT is on screen rather than being an error message. That is
`ConvertedMultiplayerSetup.Reason()`'s finding applied one screen earlier.

### 68.6 ⚠️⚠️ HOST → LEAVE → JOIN IN ONE LAUNCH IS THE PATH THIS FEATURE LIVES OR DIES ON

Auto-hosting means joining somebody else is STOPPING a host and STARTING a client in the same
process. That is § 65.1's fault (`NetworkManager.Shutdown()` does not shut anything down; a
second host or join in one launch was refused, silently) and § 63.1's (handlers registered once
per process, not once per session). **Both are fixed. Neither has ever been exercised in this
order.** § 68.14's two-process run exists for this and nothing else.

### 68.7 The arrangement: PUBG Mobile's chrome, PC PUBG's line-up

Hybrid, as decided. The bottom nav from the mobile shot is DROPPED rather than invented: its
tabs are RANK / SEASON / WORKSHOP / MISSIONS / INVENTORY and this game has none of them. The two
tabs that are real go along the top.

| Reference | Here | Built from |
|---|---|---|
| Full-bleed 3D scene | The chosen arena, live, still swaying | `MapPreviewSurface`, already shipped |
| Four players standing in it (pic 1) | The four seats as their picked characters | NEW, § 68.8 |
| Names + ready ticks over their heads | Same, plus a `TAYA FIRST` tag | NEW, § 68.9 |
| Top nav (pic 1) | `PRACTICE ǀ MULTIPLAYER` tabs | `MenuKit`, wood |
| Player card top-left (pic 3) | Name, avatar, the character you picked | `MenuKit` |
| Stacked selectors bottom-left | MAP / MODE / BOTS / CHARACTER | The existing selectors, restyled |
| Big yellow START bottom-left (pic 3) | `PrimaryButton` / `StartButton`, amber | Existing nodes, restyled |
| Bottom-right LEAVE + ticks + cog | LEAVE / SPECTATE / settings | Existing `BackButton`, `SpectateButton` |
| Party code / region | Join code + address + JOIN | Lifted, § 68.11 |
| (none) | Chat, bottom-left above START | § 69 |

⚠️⚠️ **THE TABS SWITCH IN PLACE, WITH NO SCENE CHANGE.** `PRACTICE` stops the transport and
clears `SceneFlow.Networked`; `MULTIPLAYER` sets it and auto-hosts. Both then re-run the same
`Refresh()`. A scene reload here would tear down the cast, the cached arenas and both render
textures the screen just built, and `SceneFlow.Go`'s one-load-per-frame latch will not save a
same-scene reload because it is scoped to a single frame on purpose.

⚠️ **THE PANELS GET SMALLER, THEY DO NOT GET TRANSPARENT.** `UiTheme.HeroPlate`'s note is
explicit that translucent near-black is COMBAT chrome, where the court behind it is the subject,
and that menu chrome is FURNITURE and may be opaque. The room becomes the picture by pushing
opaque wooden furniture to the EDGES, which is what both references actually do. A translucent
wood panel is the "brown shit" 🧑 already rejected once.

⚠️ **THE SCRIM CHANGES SHAPE, NOT STRENGTH.** `Scrim` currently flattens the whole backdrop. It
becomes a top and bottom gradient so the middle of the room is clean and the text still reads.

### 68.8 The cast, which is the whole feature and the only real engineering

Four characters standing on the map, lit by the map's own sun and graded by the map's own
`MapGrade`.

⚠️⚠️ **THEY GO INSIDE THE PREVIEW ARENA, NOT INTO A SECOND RENDER TEXTURE.** `ModelPreview` draws
ONE subject on layer 30 with its own lights and its own camera; four of those composited over the
map would be four cameras, four targets and four subjects lit by nothing the map knows about,
which is the pasted-on look. `MapPreviewSurface` already loads the arena, strips the match out of
it, confines it to layer 29, copies the arena's ambient, fog, sky and colour grade, **and already
finds the map's `SpawnPoints` because it averages them for its camera pivot.** The cast stands on
those same markers.

Shape of the work:

* `MapPreviewSurface` grows `ShowCast(...)`. Models are parented into the cached arena scene
  **AFTER `StripMatchObjects`**, because that method destroys every `CharacterMotor` GameObject it can
  reach, and although a plain roster prefab has no motor, ordering it wrongly is a silent
  deletion of the cast. They are then re-layered to 29 with the existing `SetLayerRecursively`.
* Art comes from `RosterBook.PersonArt(index, mode)` → `.Model`, `.Clips`, `.Palette`, exactly as
  `ConvertedCharacterSelect` resolves it. ⚠️ Rigs are imported **Generic**, so
  `ModelPreview.EnsureAvatar` is required or every model stands in its bind pose, arms out. That
  is `ModelPreview`'s own recorded fault 4, and it also wrecks any framing measured off the
  silhouette.
* A seat's character is `LobbySeatInfo.CharacterPick` when occupied and the roster default when
  it is a bot. ⚠️ `RosterBook`'s header: a missing entry must render SOMEBODY, never nothing.
* Scale is `ModelPreview.PreviewScale` 2.38, which is the match's own PERSON_SCALE. Previewing at
  native scale frames a doll.
* Framing: the registry's per-map `Yaw`/`Distance`/`Height` were tuned for an EMPTY street
  (Eskinita 0/22/16). A four-person line-up needs its own shot, so one `LobbyFraming` joins the
  three existing fields on `MapEntry`. ⚠️ It goes in the registry and not in the map scene, for
  the reason that struct's own note gives: `tools/maps/build_*.py` emit the map scenes
  WHOLESALE, so a camera placed by hand survives exactly until the next layout run.
* ⚠️ The sway stays. `SwayDegrees` 7 over `SwayPeriod` 26 is what stops the shot being a
  photograph, and it is a sway rather than an orbit so the camera never swings behind the facades.

### 68.9 Nameplates

A name, a ready tick and a `TAYA FIRST` tag floating over each character. UI, not world geometry:
projected with the preview camera's `WorldToViewportPoint` and mapped into the `MapPreview`
RawImage rect.

⚠️ **THEY ARE NOT TINTED WITH `Offense` OR `Defense`.** Those two colours mean "attacker" and
"defender", and `UiTheme.ForRole`'s note is explicit that the taya ROTATES every round, so a
fixed per-seat colour tells the player the wrong thing three rounds out of four. Cream for names,
Amber for the taya tag, nothing else.

⚠️ **EVERY PLATE IS SIZED AGAINST ITS STRING.** Legacy `Text` defaults to WRAP and the converted
labels ship `Overflow`, so a long player name either wraps out of its plate or draws straight past
it. `ConvertedScreen.SetHeadline` records this happening three times in one session and
`GameVersion.ApplyTo` records the fourth. A player name is arbitrary text from another machine,
which makes this the worst case in the game, not the mildest.

⚠️ **`raycastTarget = false` ON EVERY DECORATIVE GRAPHIC**, or `UiClickProbe` reports the controls
underneath as unreachable and it will be right.

### 68.10 Everything togglable, and the greying that is missing today

MAP, MODE, BOTS, CHARACTER, seat, SPECTATE, READY, START, the `PRACTICE ǀ MULTIPLAYER` tabs, and
the LAN/ONLINE choice in the join panel.

⚠️ **A NON-HOST'S CYCLE BUTTONS SILENTLY DO NOTHING TODAY.** `OnMapCycle`, `OnModeCycle` and
`OnDifficultyCycle` all open with `if (!NetAuthority.IsHost && SceneFlow.Networked) return;`,
which is correct authority and a bad control: the button lights, clicks, plays its sound and
changes nothing, which is indistinguishable from broken. They get `interactable = false` and a
line saying the leader picks. **This is a live defect being fixed in passing, not a new feature.**

⚠️ **BOTS STOPS AT HARD IN A NETWORKED LOBBY**, and that is `DifficultyOptionCount`'s existing
rule rather than an oversight: `NONE` removes three seats, and a seat is what a peer joins.

⚠️⚠️ **THE BIG AMBER BUTTON IS `START MATCH` FOR THE HOST AND `READY` FOR EVERYONE ELSE.** 🧑,
2026-08-28: *"start should be ready for everyone else except for host"*. One button in one place,
two labels, decided by `NetAuthority.IsHost`. **That is a layout change, not a behaviour change:**
§ 59.3 already made readiness an ANSWER the host reads rather than a trigger, on request (*"i
also dont like that if u click ready it auto starts"*), and the host's START is already live
whatever the tally reads, because a host plus three bots is a legitimate match.

⚠️ **SO `StartButton` AND `PrimaryButton` STOP BEING TWO CONTROLS ON SCREEN AT ONCE.** Today the
host sees both: `PrimaryButton` reads READY and `StartButton` is shown host-only right beside it.
In `Street` exactly one of them is visible per peer. ⚠️ **Both nodes stay in the scene and keep
their handlers** (§ 68.4); the one that is not this peer's is deactivated, not rewired, so
`OnPrimaryPressed` and `OnStartPressed` keep the meanings § 54 and § 59.3 settled.

⚠️ **CHARACTER STILL OPENS THE EXISTING PANEL IN PLACE.** 🧑: *"i dont want character select to be
touched"*. `ConvertedCharacterSelect.cs` and `CharacterSelect.unity` are not edited, and
`OpenCharacterSelect` keeps revealing `CharacterSelectPanel` as a child of this scene. § 68.13.

### 68.11 The join panel, lifted rather than rewritten

`ConvertedMultiplayerSetup`'s LAN browser, online browser, address/code field and Relay host
button move into a `LobbyJoinPanel` opened from the lobby. The logic is transcribed, not
redesigned: `Reason()` (which stopped four different failures reading as one sentence),
`LastDisconnectReason` (read once and cleared), the `host:port` split from § 59.1, and the code
lookup through `ServerQuery.ResolveCodeAsync`.

⚠️⚠️ **ONLINE IS A FIRST-CLASS LOBBY, NOT A LEFTOVER OF THE OLD SCREEN.** 🧑: *"make sure u can do
online server lobby too"*. Auto-hosting on LAN (§ 68.5) is the LANDING state and not the only
one. The lobby carries a `LAN ǀ ONLINE` toggle beside the join code:

* **LAN → ONLINE** stops the local host and calls `NetSession.StartRelayHost()`, then redraws in
  place. The join code row swaps from `address + code` to the Relay code, and `ServerQuery`
  publishes the lobby to the online pool so it shows up in other players' browsers.
* **ONLINE → LAN** is the same move back.
* **Joining** is symmetric and already is: `ResolveCodeAsync` returns `IsLan`, and the panel takes
  `StartClientAsync` or `StartRelayClient` off that flag. A four-character code works for both, so
  a player reading a code out loud never has to know which kind of lobby they are in.

⚠️ **A TOGGLE IS A SECOND HOST → LEAVE → HOST IN ONE LAUNCH**, which is § 65.1 again, from a
third direction. It is on the two-process list (§ 68.14 step 7).

⚠️ **AND ONLINE HAS ONE OPEN FAULT ALREADY: § 65.4**, the online browser can offer a lobby whose
Relay allocation is gone. Moving the browser does not fix it and must not hide it; the failure
has to reach the status label through `Reason()` like every other one.

⚠️⚠️ **`SceneFlow.Go(MatchSetup)` AFTER A SUCCESSFUL JOIN BECOMES A REFRESH IN PLACE.** The player
is already on that scene. Reloading would destroy the cast, the cached arenas and the render
textures, and the one-load-per-frame latch does not cover it.

⚠️⚠️ **AND THE REJOIN PATH MUST STILL FIRE.** `RejoinRunningMatch` runs inside `Wire()` and reads
`Lobby.MatchInProgress` on arrival; joining in place never re-runs `Wire()`. Its header records
what that hole costs: *"you'll only get ported back to the lobby with no way of joining back"*.
Whatever replaces the navigation has to ask the same question again at the same moment.

### 68.12 Organisation, so nothing lives in two places

🧑: *"organize everything and make sure theres no redudnant shit and that everything is easy to
find"*.

| File | Owns | Knows about |
|---|---|---|
| `ConvertedMatchSetup.cs` | The state machine and the wiring | Everything below |
| `LobbyChrome.cs` (new) | Building the `Street` furniture and the tabs | `MenuKit`, `UiTheme` |
| `LobbyCast.cs` (new) | The four models and their nameplates | `RosterBook`, `MapPreviewSurface` |
| `LobbyJoinPanel.cs` (new) | Hosting, joining, both browsers | `NetSession` |
| `LobbyChat.cs` (new, § 69) | The chat log and entry field | `MatchRpc` |
| `MapPreviewSurface.cs` | The room. Grows `ShowCast` + lobby framing | The arena scenes |
| `ConvertedMultiplayerSetup.cs` | Nothing. Unreferenced, kept as the fallback | (nothing) |

⚠️ **ONE COLOUR SOURCE AND ONE CONTROL BUILDER.** Every colour from `UiTheme`, every control
through `MenuKit`/`GodotTheme` so `GodotButton` variations keep applying. `UiTheme`'s header
records the whole hero layer drifting into a slate-blue palette because seventeen colours were
named inline; this is the same trap on a different screen.

### 68.13 What must NOT be touched

* `ConvertedCharacterSelect.cs`, `CharacterSelect.unity`, by request. `ModelPreviewTests` and
  `HeroPickerLayoutProbe` passing unchanged is the proof.
* `Packages/com.tumbangpreso.core/`, engine-free, and no rule changes here.
* `MatchRpc` payloads, until § 69 and then exactly once.
* `ConvertedMultiplayerSetup.cs`, `MultiplayerSetup.unity`, kept per § 68.3.
* `GameVersion` / `BuildBranch`. The corner reads `1.00` on every branch as of 2026-08-28.

### 68.14 Done looks like

1. `dotnet test Core.Tests` green.
2. EditMode green, `LobbyAndSettingsTests` included, plus a new test asserting that BOTH
   `Classic` and `Street` resolve every node `ConvertedMatchSetup` reaches by name. That test is
   what makes § 68.4 an assertion instead of a warning.
3. PlayMode green (no `-nographics`, it crashes the editor; assert on the `.xml`, never the exit
   code, because both a crash and a failure come back as 0): `UiClickProbe` finds every new
   control reachable, `AspectRatioProbes` clears nine resolutions, `HeroPickerLayoutProbe` and
   `ModelPreviewTests` still pass.
4. `Checks.RunAll` green, `SceneScriptCheck` above all: it is the only check that can see a scene
   holding a component the PLAYER cannot bind, and a shipped build once crashed on the map select
   with every other check green.
5. `tools/audit_wire_payloads.py` and `audit_request_call_sites.py` exit zero: § 68 adds no
   protocol, § 69 adds exactly one and it is called.
6. `UiRuntimeShots` captures of the lobby in BOTH styles, versioned filenames per `CLAUDE.md`
   § 6.1, so `Classic` and `Street` can be compared side by side rather than described.
7. ⚠️⚠️ **THE TWO-PROCESS RUN, AND IT IS THE ACCEPTANCE TEST.** § 38.19's driver, in this exact
   order, because the order is what is untested:
   1. A presses MULTIPLAYER, lands hosting, shows a join code.
   2. B presses MULTIPLAYER, lands hosting its OWN lobby.
   3. B opens JOIN, sees A on the LAN browser, joins. **This is the host → leave → join of
      § 68.6.**
   4. A's cast grows a second body wearing B's character, with B's name over it.
   5. B changes character; A sees the model change. B readies; A's tally moves.
   6. A starts; both land in the same arena on the same map in the same mode.
   7. B quits to the lobby and joins A AGAIN in the same launch (§ 65.1).
   8. B rejoins while the match is still running (§ 68.11's `RejoinRunningMatch`).
   9. Chat carries in both directions in the lobby and in the arena (§ 69).
8. Clean Windows build, previous output deleted first, timestamps verified on BOTH
   `TumbangPreso.exe` and `TumbangPreso_Data`. A `SUCCEEDED` line does not prove the launcher
   was re-emitted.

### 68.15 Order of work, so the screen is never half-broken

1. Navigation, auto-host, and the four lobby states. **No visual change at all.** Verify joining
   works from the lobby before anything is made pretty.
2. `LobbyJoinPanel`, lifted.
3. The cast in the backdrop.
4. The `Street` chrome and the tabs, behind the `LobbyStyle` switch.
5. Nameplates.
6. § 69's chat, and the single protocol bump.
7. The full verification pass in § 68.14.

### 68.17 What landed on the `PUBG` branch, and the five things the renders found

Steps 1 to 5 of § 68.15 are in. Every number below was MEASURED off a capture rather than
argued, which is the only reason any of them is right: five of the six were wrong on the first
pass and none of the six would have been caught by a test.

**The files.** `LobbyChrome.cs` (the `Street` arrangement and the tabs), `LobbyCast.cs` (the four
bodies), `LobbyNameplates.cs` (the plates over them), `LobbyJoinPanel.cs` (host, join, both
browsers), `LobbyChat.cs` (§ 69, used by the lobby AND the arena). `ConvertedMatchSetup` gained
the state machine and nothing else; `MapPreviewSurface` gained `Adopt`, `MapShown` and the lobby
shot; `MenuKit` gained `Fit`, `FitBox` and `FitBlock`.

**⚠️⚠️ THE CAST FACED THE WRONG WAY, AND THE NOTE THAT SAYS SO IS MISLEADING.**
`ModelPreview.FacingYaw` is 180 with a header about Godot's handedness, and reading it as "the
model's front is its local -Z" is the wrong inference. `Lobby-v1.png` is four backs.
`LookRotation` aligns local **+Z**, and these rigs face **+Z**, so the direction to point along is
`-forward` (subject toward camera). One sign, and no test in this repo can see it.

**⚠️⚠️ A RECT HANDED TO A LAYOUT GROUP IS A REQUEST, NOT AN INSTRUCTION, AND THAT COST THREE
RENDERS.** `LeftColumn` was set to 580 and `ReportColumns` measured it at 580, and the panel
inside it drew **820**. Three separate things get to overrule the width: the authored
`VerticalLayoutGroup` ships with `childControlWidth` OFF so it positions children without sizing
them, a child's `LayoutElement.minWidth` outranks the group even once control is on, and a child's
own `ContentSizeFitter` rewrites the rect after the group has finished. `Narrow` answers all
three. **`localScale` is what actually settled it**, because nothing in Unity's layout reads it,
and it shrinks the panel WITH its type and its borders, which a width alone does not.
`LeftScale` 0.72 and `RightScale` 0.86 open the middle band from 320 px to about 800.

**⚠️⚠️ THE FIT PASS HAS TO RUN MORE THAN ONCE.** `Lobby-v2.png` still reads `LOBBY · YOU ARE
HOSTIN` under the SPECTATE button after a fit pass that had already run and reported success: the
widths it measured came from a chain of layout groups that had not converged, so it measured
against a width nothing would ever have and concluded the string fitted. It now repeats for
`FitPasses` frames and forces a real `LayoutRebuilder` pass first, because
`Canvas.ForceUpdateCanvases` flushes the canvas and does not run the layout system.

**⚠️ THE NAMEPLATES WERE ALL IN THE BOTTOM-LEFT CORNER**, drawn as four stray `BOT` chips over the
BACK button, which reads as a chrome bug rather than a projection one. A plate is anchored at
(0,0), so its `anchoredPosition` is already measured from the parent's bottom-left; adding
`rect.xMin`, which on a centred-pivot stretched rect is minus half the width, subtracted half a
screen twice.

**⚠️ THE LOBBY SHOT NEEDED ITS OWN LENS, NOT JUST ITS OWN DISTANCE.** Framing four people to fill
half the height at the map shot's 58 degrees puts the camera about 3 m away and leaves the outer
two at 34 degrees off axis, visibly stretched. `LobbyFieldOfView` 32 puts the same framing about
7 m back at 17 degrees off axis, and keeps more of the street readable behind them.
`LobbyDistance` 12.6 and `LobbyHeight` 3.4 with `LobbyLookHeight` 0.85: aiming LOWER is what
lifts the cast clear of the corner furniture without changing how big they are.

**⚠️ THE ACTIVE TAB IS AMBER, NOT GREEN.** `WoodPrimaryButton` is green and means ACT (START
MATCH, READY); a tab is not an action, it is a statement about where you already are, and painting
it green put two "press me" buttons on one screen with the more important one further from the
hand. `WoodAmberButton` is new in `GodotTheme` and introduces no colour: amber is already this
UI's attention colour and is in `UiTheme`.

**What is verified.** `dotnet test` 111 green. `Checks.RunAll` all five green.
`audit_wire_payloads.py` 47 named messages, 0 mismatched, `Chat` and `ChatLine` among them.
`audit_request_call_sites.py` 43 entry points, 0 unreachable. `audit_ability_authority.py` 40
sites, 0 ungated on another body. `TheLobbyDraws` passes and writes
`Logs/shots-runtime/Lobby-v*.png`.

**⚠️ WHAT IS NOT VERIFIED IS THE ONLY THING THAT MATTERS: § 68.14 STEP 7, THE TWO-PROCESS RUN.**
Every fault this batch could still hold is on the far side of a second machine: host to leave to
join in one launch (§ 68.6), the LAN/online toggle, a joiner's cast wearing the right character,
the ready ticks moving on somebody else's screen, and chat in both directions. **Nothing here has
been played by two people.**

---

### 68.18 The second pass, 2026-08-28: navigation, the settings panel, and one rail per side

🧑, off the 4.7x player: *"Rewire clicking play from main menu to directly the lobby bcz we dont
need single player multiplayer selection anymroe as practice is bascally singleplayer already"*,
*"match settings look ugly"*, *"also maybe plan out where to put ui for char select, remove it in
match settings"*, *"pic 3 doesnt have animations or move but everyone else does in here"*, *"Also
rewire tutorial from main menu to the start training already, the text based tutorial is stale and
should be deleted and completley replaced by game tutorial"*, *"Pic 4 fix player name"*, *"Also
sometimes the pillars in the ilalim ng tulay map block the camera of lobby"*, *"put BACK somewhere
else, it looks ugly that its right below start match, it fucks up the visual hierarchy"*, *"also
remove this lobby thing bcz we all know this is lobby already"*, *"make sure all buttons work and
shit works right ennd to end"*, then, mid-pass: *"make these huds or ui look good bruh its so weird
to look at as none of them have visual harmony or shit"*, *"make sure all sfx play the right way"*,
*"i want thgis to say my name instead of YOU"*, *"i want u to make sure that everyone can see the
names in multiplayer (lan or server)"*, *"do u not feel weird that theres b ig ass empty space left
and right"*.

#### 68.18.1 Navigation: two screens left the path, and one panel was deleted

* PLAY goes straight to `MatchSetup` with `Networked = true`, so the landing state is the lobby
  auto-hosting on LAN. `ModeSelect` is unreferenced and **kept on disk and in the build order**,
  per § 68.3, alongside `MultiplayerSetup`. `SceneFlow.ModeSelect`'s own note carries the reasoning
  and `UiClickProbe` still probes both, because a fallback nobody checks is not a fallback.
* `MatchSetup`'s `CancelTarget` and BACK both go to `MainMenu` now. They have to agree or one of
  them is a step the other does not take.
* **The text tutorial is DELETED**, and it is the one place this batch departs from § 68.3's
  keep-the-old-chrome rule: that rule protects a REPLACEMENT that might turn out worse, and this
  was a deletion asked for by name with a shipped, played replacement. Gone:
  `ConvertedTutorialPanel.cs`, `TutorialContent.cs`, `Scenes/Ui/Tutorial.unity` and the
  `TutorialPanel` node inside `MainMenu.unity` (27 GameObjects, removed through the scene API).
  ⚠️ **The node had to go with the script.** A `MonoBehaviour` whose `m_Script` guid resolves to
  nothing is a yellow warning in the editor and a **refused build** under `SceneScriptCheck`, which
  is the only gate that can see it (`CLAUDE.md` § 7.1).
* The route moved to `SceneFlow.StartTraining`, because it was a private static on the deleted
  panel. `TUTORIAL` on the title screen enters it directly. `TutorialContent.ChipWidth` moved to
  `CreditsContent`, which is the only overlay left that draws a chip row.
* `DeadFeatureAudit` now asserts both halves: `SceneFlow` still arms `GuidedTutorial`, and the menu
  still reaches `StartTraining`. Either alone is silent.

#### 68.18.2 ⚠️⚠️ THE MATCH SETTINGS PANEL WAS UGLY FOR ONE MEASURABLE REASON

In `MatchSetup.unity` every caption is authored at **52 units** and every value at **34**, so the
word `MAP:` was drawn half again as large as the map's name. The label shouted and the thing it
labelled whispered. The rebuilt row inverts that (caption 22 amber, value 26 cream) and adds the
half nobody sees: **a fixed caption column**. `MAP:`, `MODE:` and `BOTS:` are three different
widths, so the authored `HorizontalLayoutGroup` started each stepper at a different x and nothing
in the panel lined up vertically.

* Every authored node is **restyled, never rebuilt** (§ 68.4). The arrows keep their
  `TextureButtonFeedback`, the values keep their `GodotOutline`, all of them keep their wiring.
* The colon is dropped, which is worth 54 px: the caption column has to hold the longest caption,
  and `BOTS` is 54 px narrower than `BOTS:`.
* CHARACTER left the panel entirely. See § 68.18.4.

#### 68.18.3 ⚠️⚠️ ONE RAIL PER SIDE, AND THAT IS A STRUCTURAL FIX RATHER THAN BETTER NUMBERS

Measured off `Logs/shots-runtime/Lobby-v35.png`, the bottom-left had **three left edges and three
widths**: the MATCH SETTINGS pill at x=75 running 300 px, its summary at x=60, START MATCH at x=55
running 380. The cause was that the left side was TWO hosts (`LeftColumn` at one anchor, a
`SettingsDrawer` beside it at another) at two different scales. **Two containers cannot share an
edge by arithmetic.** There is one `VerticalLayoutGroup` per side now and the group gives every
child the rail's width by construction.

* `LeftScale` 0.66 is **deleted**. It made every number on that side a lie: a 56 px header drew at
  37 and an 18 unit caption rendered at 12. The rail is authored at its real size.
* `LeftWidth` came down 560 → **460**, and the 100 px came out of the caption column rather than
  out of the type. At 460: 96 caption + 14 gap + a stepper of 20 padding, two 42 px arrows, two
  6 px gaps, leaving **214 px** for a value against `ILALIM NG TULAY` measuring about 195.
* The right-hand furniture **left `Columns` entirely**. `Columns` is a child of `Body`, a
  full-screen `VerticalLayoutGroup`: disabling the group ON `Columns` never stopped `Body` driving
  `Columns` itself, so "48 px in from my parent's right edge" was 48 px in from a moving rect. The
  old code compensated with a `-47` constant; `Lobby-v36.png` still had the pill 145 px from the
  edge against the chat's 48.
* ⚠️ **The lobby drawer stacks off `LobbyChat.PanelHeight`, not off its capacity.** The chat
  reserves six line slots and then collapses onto its content, so an empty log is about 65 px and
  the capacity expression gives 224. It is re-asked every frame because the chat grows as lines
  arrive; the guard makes that free.
* One harmony set decides every edge: `EdgeMargin` 48, `BottomMargin` 40, `TopMargin` 34,
  `RailSpacing` 12, `HeaderHeight` 56 (BACK and both tabs), `ToggleHeight` 52 (both drawers),
  `ActionHeight` 104.
* The three selector values are fitted **as a set**, to the largest size all three accept, and
  **reset to `ValueSize` on every pass**. Fitting them individually is why `Lobby-v35.png` has
  `ESKINITA` and `HARD` at full size and `HERO STRIKE` visibly smaller; `MenuKit.Fit` only shrinks,
  so a pass that measured a half-built rect pinned the type small permanently.
* The closed drawer's summary was **composed once inside `LobbyChrome.Apply`**, which runs before
  the screen's first `Refresh`, so it shipped the authored placeholder: `Lobby-v35.png` reads
  `ESKINITA · CAPTURE · NORMAL` on a lobby set to Hero Strike, and `CAPTURE` is not a mode this
  game has. It hangs off `Refresh` now.
* BACK moved to the top-left corner the banner vacated, and the banner is `SetActive(false)` in
  `Street` (not destroyed: `Refresh` still writes `BannerLabel`, and `Classic` keeps the pennant).
* The status line under START MATCH is **hidden unless it is an alert**. The four messages a player
  has to act on (refused port, dropped connection, relay refused, still connecting) still open it.

#### 68.18.4 The player card, and where character select went

CHARACTER left the match settings for a reason that is **authority, not tidiness**: MAP, MODE and
BOTS are greyed on every client by `RefreshLeaderControls`, so keeping CHARACTER as the fourth row
of a panel that greys out told three players in every four-player lobby that they could not pick a
fighter. It is the one choice on this screen that is always yours.

* The authored `CharacterButton` is **reparented**, keeping its name, its `Button`, its
  `GodotButton` skin and its handler. `OpenCharacterSelect` is untouched and still reveals
  `CharacterSelectPanel` in place. § 68.13 holds.
* The button gained two lines (character at 24, loadout at 18) because one line of
  `CHESKA · KALAWANG · CROCS ▸` at 24 units drew the name you chose at the same size as the slipper
  you did not think about.
* ⚠️ **`›` (U+203A), not `▸` (U+25B8) or `▶` (U+25B6), and `EDIT`, not `✎`.** Checked against
  Darumadrop One's own cmap: 525 glyphs, and it has none of `✎`, `✓`, `◀`, `▶` or `▸`. Unity's
  dynamic-font fallback draws them from a system font at a different weight and baseline.
* `CardWidth` is **330 and deliberately not `RightRailWidth` 392**. Matching the chat looked like
  the harmonious answer and left a visible hole: the gap is between the end of a short left-aligned
  string and an affordance pinned to the right edge, so only the width closes it. **The shared axis
  is the right edge, not the width.** 330 is measured against the worst case in the roster:
  `LOLA PACING` (11 chars, ~154 px at 24 against 244) and `DECADES TUNA  ·  TSINELAS` (25 chars,
  ~225 px at 18 against 244), not against `CHESKA`.

#### 68.18.5 The names, on every machine

* The local nameplate carried the pronoun `YOU`, so the three other people in the lobby saw
  `Matthew` over that body and its owner did not. It carries the name now; the `◀` marker still
  says which one is yours, and an unset name falls back to `YOU` rather than to the literal
  `Player` that `PlayerLabel`'s header already records four seats sharing.
* ⚠️⚠️ **A name edited in the lobby never travelled.** `NetSession.OnClientConnected` sends
  `IdentifyServerRpc(token, PlayerName, ...)` ONCE, on the frame the transport comes up, which was
  the whole story while the only editable field was in Settings on the title screen. The lobby card
  is editable while connected. `PublishName` re-sends `Identify` on commit: `LobbySession.Admit` is
  idempotent for a peer re-identifying under the same durable token (it copies the seat, the
  spectator flag and all three picks across and takes only the new name), which is the
  fast-reconnect path exercised on every relaunch. ⚠️ **No new message, so `ProtocolVersion` stays
  at 6** (§ 68.2, § 69.1).
* Every box that can hold a name already grows-then-fits: the plates size from the measured string
  up to a 420 px cap then shrink the type, the card's field is best-fit 13..20, and the seat rows
  go through `FitLine`. `Balance.PlayerNameMax` is 14.

#### 68.18.6 The cast's third character did not move, and no test could see it

🧑, with it circled: *"pic 3 doesnt have animations or move but everyone else does in here"*.
`LobbyCast.Poses` slot 2 asked for `holding-right`, which is a **carry POSE, not a performance**:
the rig's six `holding-*` clips are what the arm does while a tsinelas is in it, sampled by
`CharacterAnimator` as a state. It is a real clip on all twelve rigs, it has a length,
`SampleAnimation` succeeds, and it returns the same frame at every phase.

⚠️ **`PickPose` resolves by NAME, and a name that resolves is indistinguishable from a name that
animates.** So the pick is measured: `LobbyCast.MotionOf` samples the clip five times across its
length and takes the largest distance any transform travels. A hold measures 0.0 and a breathing
idle measures centimetres; `MotionFloor` is 1 cm. The table now asks for `interact-right` (the
pick-up reach, a real animation) and the check is the floor under it. Five samples rather than two,
because a clip whose first and last keys match is ordinary (`DanceClip` is built that way).

#### 68.18.7 The Ilalim ng Tulay pillars, and why a camera angle was not the fix

The lobby shot is 12.6 m out at a 32 degree lens, which puts the camera **inside the colonnade**
rather than outside it, and `SwayDegrees` 7 over 26 s walks it across the gaps: intermittent by
construction, which is why *"sometimes"* is the word in the report and why no still render was ever
going to settle it.

A per-map lobby yaw aimed down a clear lane would still swing into a pillar at the ends of the
sway, and widening the shot would undo `LobbyFieldOfView`'s finding about the outer two characters
distorting. What is actually wrong is that concrete is between the player and a face, so
`MapPreviewSurface.ClearSightlines` takes it out of the way for as long as it is: a ray from the
camera to each body's **head and chest** (two points, because a pillar is tall and thin and one
sample at chest height clears while the same pillar is still across the face) against every
renderer's AABB.

* ⚠️ `MaxOccluderSpan` 14 m, or **the sweep hides the road**: the floor slab's AABB contains the
  camera, so a ray enters it at t≈0. A viaduct pillar is about 1x1x8 and a jeepney 6 long.
* ⚠️ The hit must be in FRONT of the person: `Bounds.IntersectRay` reports a hit anywhere along an
  infinite ray, so without the distance test every building behind the cast would count.
* ⚠️ Adopted objects are excluded, or the arc's inner two characters delete the outer two: the cast
  is adopted into the SAME scene as the arena.
* ⚠️ **Renderers are disabled, not GameObjects**, which would fight `Park`/`Unpark` over the active
  flag they use to decide which map lights the world. The previous sweep is undone first, every
  time, or the street is stripped one pillar at a time. Rate limited to 6 sweeps a second, and the
  renderer list is cached per map because `GetComponentsInChildren` allocates per root per call.
* It is the LOBBY shot only. The practice screen is a picture OF the map; hiding a pillar there
  hides the thing being chosen.

#### 68.18.8 ⚠️⚠️ ONE PRESS WAS FIRING THE CLICK UP TO THREE TIMES

🧑: *"make sure all sfx play the right way"*. The UI click is added in three independent layers,
each individually correct and none aware of the others:

1. the CONTROL on pointer down (`GodotButton`, `ArrowButtonView`, `TextureButtonFeedback`);
2. the WIRING on click (`ConvertedScreen.WireOne`, so a screen cannot forget);
3. the HANDLER (`Cycle`, `TakeSeat`, `SelectTab`, both COPY buttons).

A map arrow has all three. `AudioDirector.PlayAtVaried` has **no dedupe** and `PlayAt` pins the
pitch at 1.0, so three copies of one 40 ms recording start in the same frame at the same position
and sum to about **+9.5 dB**, undecorrelated. It read as a clipped clack on the arrows and a doubled
one on every wood button, next to a clean single click on the runtime-built controls that only have
layer 1.

⚠️ **The fix is in `MenuSfx`, not at the call sites.** Deleting two of three layers is a nine-file
edit that leaves the rule written down nowhere and regresses to SILENCE the first time somebody
removes the wrong one. **One press is one sound** is a property of the sound layer: all three may
ask, and the first ask per cue per frame plays. Per cue, because a frame may legitimately carry a
click and an error. Per frame rather than per time window, because a time window would also swallow
a genuine fast second press on a map arrow.

Also: **a BACK button plays `ui_back` now**, the same as Escape always has. `ui_back.wav` and its
own mix entry exist because backing out is meant to be audibly distinct, and every BACK button in
the game was playing a plain click. `GodotButton.PressCue` is the field, `ConvertedScreen.WireOne`
sets it, and the wiring names the SAME cue the control does so the frame guard collapses them.
`DeadFeatureAudit.EveryMenuSoundGoesThroughTheOncePerFrameGuard` is the tripwire.

#### 68.18.9 What is verified, and what still is not

`dotnet test` 111 green. EditMode **188/188**. PlayMode `UiRuntimeShots`, `LobbyStyleProbe` and
`UiClickProbe` all green, which means every node the screen reaches by name resolves under BOTH
styles and BOTH tabs and no label draws outside its box in any of the four arms.
⚠️ `LobbyStyleProbe` caught one live defect in passing: `MiniSection`'s headings drew `SHARE THIS
LOBBY` in a **4 px box** in `Classic`, because the holder has no layout group so its preferred
width is zero, and `Street` had only ever hidden it by `Narrow` writing a width onto the column.
`Checks.RunAll` five green. `audit_wire_payloads.py` 47 named messages 0 mismatched;
`audit_request_call_sites.py` 43 entry points 0 unreachable. Renders at
`Logs/shots-runtime/Lobby-v41.png` and `LobbySettings-v41.png`.

#### 68.18.10 ✅ THE CHAT IS PROVEN, ON TWO REAL PROCESSES

🧑: *"does say something even work? can u even chat with people?"*. Fair question, because nothing
on either end said so: the send side printed `sent=True`, which only proves a message reached the
transport, and the receiving end drew a label and logged nothing. A run where the host relayed
correctly and a run where it dropped everything produced identical logs on both machines.
`LobbyChat.Add` logs the receipt now, for the reason `ConvertedScreen.WireOne` gives about menu
presses: in a shipped .exe a line in `Player.log` is the only way to tell "it never arrived" from
"it arrived and the panel did not draw it".

Two built players, one machine, `-tp-lobby` on 8910 and 8911, B joining A with
`-tp-lobbyjoin 127.0.0.1:8910 -tp-lobbychat hello_from_B`:

```
A (host)    [Net] 2 connected, seat 1
            [Chat] received from 'Matthew': hello_from_B
B (client)  [LobbyAuto] join result True
            [Net] connected as seat 2
            [LobbyAuto] chat 'hello_from_B' sent=True
            [Chat] received from 'Matthew': hello_from_B
```

**Both legs are in those six lines.** Client to host is B's send arriving on A. Host to client is B
receiving its OWN line back, which only reaches it through `HostRelayChat`'s
`SendNamedMessageToAll("ChatLine")`: `OnChatLineMsg` refuses the host, so A's copy comes from the
local `OnChatLine?.Invoke` beside it and B's comes off the wire. It also incidentally proves the
name work of § 68.18.5, because the line is attributed to `Matthew` rather than to `P2` or
`SOMEBODY`, which means `LobbySession.PeerById` had the real name at relay time.

⚠️ **This does NOT close § 68.14 step 7.** It closes 7.9 for the lobby. Host to leave to join in one
launch, the LAN/online toggle, a joiner's cast wearing the right character, the ready ticks, the
rejoin path and chat IN THE ARENA are all still unexercised, and the join here was driven by
`NetAutomationProbe` on one machine rather than by two people on two.

#### 68.18.11 The lobby log opens on demand

🧑: *"big empty sapce here for lobby and say something"*, then *"make it so that if u clcik chat u
see like the logs for it and who sent but it clsoes when u click out"*.

* ⚠️⚠️ **An empty log row is DEACTIVATED, not set to zero height.** Zeroing it was the obvious fix
  and it did half the job: a `VerticalLayoutGroup` puts its `spacing` between every pair of ACTIVE
  children whatever their heights are, so six zero-height rows still contributed six 4 px gaps.
  With 20 px of padding the idle panel was 44 px of nothing above a 44 px field.
* The lobby log is **closed by default** and opens on a click anywhere on the panel. The plate eats
  clicks by design, so `OnPointerClick` focuses the field rather than swallowing the press, which
  is also what makes "click chat" mean the whole panel rather than one 56 px strip of it.
* It closes when the field loses focus, **polled rather than evented**: an `InputField` losing
  focus to a click elsewhere on the canvas raises nothing here, so the only honest test is whether
  it still HAS focus. `Typing` is that test and the input reader already asks it.
* ⚠️ **An arriving line opens it anyway**, for `LobbyLogLife` 9 s. A log that only opened on a click
  would be silent about the one thing it exists for, because the message you most need to see is
  the one that arrives while you are looking at the cast. Focus overrides the timer, so a long
  message is never cut off mid-typing. This is the same shape the MATCH log already has.
* `FieldHeight` 44 → 56, so with the rows away the field IS the panel: at that moment the control
  is an invitation to type and it should look like one.


⚠️⚠️ **§ 68.14 STEP 7, THE TWO-PROCESS RUN, IS STILL THE THING THAT HAS NOT HAPPENED**, and this
pass adds one item to it: **the name published after connecting** (§ 68.18.5) has to be seen
arriving on the other machine's plate, in both directions, host and client.

---


## 69 · The game has no chat, in the lobby or in a match ⚠️ OPEN, PLANNED 2026-08-28

🧑, 2026-08-28: *"yea maybe add a chat to our game too that works in lobby and ingame"*.

Four people in a lobby have no way to say anything to each other, and four people in a match have
no way to call a play. Emotes exist (§ 38.3 put them on the wire) and they are not the same thing.

### 69.1 ⚠️⚠️ THIS IS THE ONE THING IN BOTH SECTIONS THAT MOVES `ProtocolVersion`, 5 → 6

A chat line is a named message that has never existed, so both machines must be rebuilt from this
branch or they refuse each other at approval. § 68.2 has the full reasoning. **Bump it once, in
this section, after § 68 has been verified at 5.**

### 69.2 Shape

* One named message, host-relayed: sender's seat, and the text. ⚠️ **The sender is NGO's
  authenticated client id, never a seat carried in the payload.** § 54 records exactly this: a
  field the host has to remember to ignore is a field that gets trusted, and `DeclareReady` was
  cut down to a single `bool` for it.
* ⚠️ **The host clamps length and rate.** § 38.9 found two request channels any client could
  flood; a text channel is the obvious third. A cap on characters and a minimum interval per
  peer, enforced host-side, not client-side.
* ⚠️ **The name is the one already in `LobbySeatInfo`**, not a second name field. There is exactly
  one identity per peer and it crossed the wire before this.
* `tools/audit_wire_payloads.py` must show the writer and the reader agreeing field for field:
  § 38.6 exists because netcode does not check that and a mismatch is silently misread bytes.

### 69.3 In the lobby

Bottom-left above the START button, in the wood set. Always visible, last few lines, entry field
below. No key needed to focus it: the lobby has no gameplay to steal a keystroke from.

### 69.4 In the match, where the rules are different

⚠️⚠️ **A CHAT FIELD THAT SWALLOWS MOVEMENT KEYS IS A WEAPON.** Enter opens it, Enter sends and
closes it, Escape cancels, and while it is open the gameplay input map is suspended.

⚠️ **THE INPUT MAP RULE APPLIES.** `CLAUDE.md` § 4: one control, one action, PER CONTEXT, and
`InputMapAndAbilityTests` asserts it. Chat is a THIRD context after gameplay and spectating, and
it is a narrowing of the same kind § 35.3 records: a player typing has no verbs, so its keys can
never collide with theirs. It goes in the input asset and in the rebinding panel like every other
key. It does not become a ninth `Keyboard.current` read outside the asset.

⚠️ **THE LOG FADES, THE HUD DOES NOT GROW.** `VISION.md` § 2 rule 5: a screenshot mid-fight must
still show the lata, the chalk and every player. Chat lines retire after a few seconds like
§ 46.3's banner rather than accumulating, and they sit clear of the ability deck, which is where
§ 46.1 and § 46.4 both found something drawn on top of something else.

⚠️ **A SPECTATOR CAN READ AND SEND.** They have no body and no seat, and their name is still in
the roster.

### 69.5 Done looks like

The two-process run of § 68.14 step 7.9, both directions, in the lobby and in the arena, plus
`audit_wire_payloads.py` and `audit_request_call_sites.py` green on the new message, plus an
EditMode test on the host-side clamp and rate limit.

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

✅ **DONE 2026-08-26, with the one caveat this entry always predicted.**

Three named messages, mirroring the ready gate deliberately: a peer sends `VoteRematch` to the
host, the host tallies and broadcasts `RematchTally`, and `BeginRematch` starts everybody at
once. ⚠️ **The middle one is the point.** A vote that only travelled peer-to-host would start the
rematch correctly and leave the other three staring at a button they had already pressed with no
way to tell whether anybody agreed; `match_result.gd` draws the count for the same reason.

⚠️ **The rules are engine-free in `Core.RematchVote` and asserted in `Core.Tests`, not played.**
Every bug this has ever had was a counting bug, and all five are the ready gate's own scars:
 * ⚠️⚠️ **this bullet used to say the opposite, and it was the fault § 52 fixed.** It read
   *"the host's press arrives with a sender id of 0 and is resolved at the door, or the host can
   never satisfy a gate it is part of"*. NGO client id **0 is the host's real transport
   identity**, so resolving it to `LocalSlot` was not a fix, it was a collision: a host in seat 1
   beside a client whose peer id is 1 collapsed into one set entry. **Peer 0 is a voter like any
   other**;
 * a second press from one peer changes nothing, because it is a set;
 * it counts **peers, never seats** (`ReadyGate.ExpectedReadyCount`'s rule), since bot-filled
   seats cannot press a button and a gate waiting for four seats in a two-human match never
   opens;
 * a peer leaving re-evaluates rather than stranding the rest, hooked at the same place
   `ReadyGate.OnPeerLeft` is;
 * **zero votes never opens the gate**, however small `expected` gets, or a lobby that empties
   out would start a rematch nobody asked for.

⚠️ **Only the host calls `StartMatch`.** Every peer hides its own board and unlocks its own
cursor, which is local presentation, but match state is host-side per `CLAUDE.md` § 4: four peers
each starting a match is four matches.

⚠️ The tally line is its own label, not `_broadcastLine`. The first version wrote into it and
deleted "HERO STRIKE · FINAL STANDINGS · 8 ROUNDS" from a screen whose entire job is the final
standings.

⚠️⚠️ **THE TRANSPORT ITSELF IS STILL UNPROVEN AND THE LEDGER ROW SAYS SO.** Two real processes on
a LAN have never been run, and nothing in this repository can stand in for that. `Port_Ledger.md`
records the row as CONVERTED with that caveat written into it rather than implying otherwise.

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

✅ **ANSWERED 2026-08-26, FROM A PLAYED FRAME, AND THE ACCENTS ARE NOT THE PROBLEM.** 🧑 sent a
gameplay screenshot from a defender's seat. Telling which player is the taya was never in doubt
in it: the scoreboard row says **DEFENDER** in words, carries a Defence-blue rail and a row plate
at twice the alpha of the others, and the card in the bottom-left corner reads
`TAYA (DEFENDER) P2`. Four independent channels, only one of which is hue. The hero accents are
25 degrees clear of both role hues and 30 degrees clear of each other by
`HeroPresentationTests`, and nothing in the frame contradicted that.

**What the same frame did show is a real fault, and it was the opposite one:** the local player
had **no crosshair at all**. 🧑: *"theres no crosshair here, i want one here so that i can figure
out where the fuck is the clickable place i need to aim camera at"*. `Hud` gated the crosshair on
`GameServices.Round.CanThrow(_local)`, and `ThrowRules.CanThrow` returns false for a defender on
its second line, so **the seat that aims most had the one aiming aid switched off**. Everything a
taya does is aimed: the tag is a distance-and-facing check, the reset is a hold at the can, the
shove has an arc. Fixed: the crosshair follows `RoundActive`, and the taya's reads
`HOLD [key] AT THE LATA` while the can is down.

**Also fixed from that frame:** the scoreboard's `ATTACKER  ·  YOU` suffix. 🧑: *"attacker dot
you is ugly"*. It was a fourth answer to a question three other things on the same screen already
answered, and it lengthened exactly one row so its columns fell out of line, which is the same
argument that deleted the leading arrow on 2026-08-02. Your own row now spends its NAME and SCORE
colour on Cream instead, which costs no width and moves no column.

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

✅ **FIXED 2026-08-26, and Bayan Plaza is gated now.** `BayanPlazaMonumentFix` moves the statue
**0.45 m west** and solves the collider's east face **0.05 m outside the chalk**, then saves the
scene. `MapGeometryCheck` reports `box 0 solid object(s) inside the chalk` and the map has moved
from `Informational` to `Gated`.

⚠️ **The collider and the statue live in different hierarchies and both had to move.** The box is
`Obstacles/MonumentBody/CollisionShape3D` and the mesh is `Dressing/Monument/Monument`, and they
measured identically before the fix. Moving one alone is an invisible wall or a statue you walk
through, so the script measures both and refuses rather than guesses.

⚠️ **Neither option in the original entry worked on its own, and the corner is why.**
 * The plaza rail runs at x = -9.51, so the statue cannot travel the full 0.70 m it needs to clear
   the chalk by itself: at -0.70 its base sits inside the railing.
 * "Move it to the plaza edge" is not free either. The corner outside the box is occupied by
   `MonHedge_1` and `Rail_5`, so it would mean relocating a hedge and a railing somebody
   arranged.
 * A collider below `stepOffset` would leave a 5 m statue you walk straight through.

So: move what can move, take the rest off the collider. **0.30 m of statue is left inside the box
and it is walkable rather than solid.** A taya clipping the corner of a statue for a third of a
metre is a smaller fault than a 1.90 m wall that decides who wins the round.

⚠️ **The script is idempotent** (it solves the collider from where the statue actually is) and it
is a script rather than a hand edit because the scene is an imported `.tscn`.

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

✅ **THE REAL QUESTION IS ANSWERED, 2026-08-26. THE A/B IS NOT, AND CANNOT BE BY THIS PROBE.**

**The question § 4.5 actually asked was multiplier versus flat, and it is settled by
construction.** `OverheadPassWindow.OverclockSeconds` is **6.75 s** and is now the AUTHORED
number; `OverclockRate` is derived as `1 + OverclockSeconds / PassSeconds` and comes out at
exactly the 3.5 that was there before, so nothing in play changes today. What changes is which
number a person edits.

The arithmetic that makes the swap correct rather than cosmetic: a rate `r` held for a window `W`
advances the cooldown clock by `W * r`, so it saves `W * (r - 1)` seconds **whatever the cooldown
is**. The saving was always flat. Only its WORTH moved, and it moved a long way:

| Skill cooldown | Rate | Seconds saved | Share of a cycle |
|---|---|---|---|
| 6.5 s (pre-2026-08-25) | 2.0 | 2.70 | **41 %** |
| 30 to 45 s (today) | 2.0 | 2.70 | **6.0 to 9.0 %** |
| 34 s (today) | 3.5 | 6.75 | **20 %** |

So `3.5f` was a proxy for 6.75 s that nobody reading the file could see, and it needed hand-editing
every time cooldowns moved. The flat figure does not. ⚠️ **`PassSeconds` is a deliberate copy of a
number `LrtTrainFlyby` derives** (`(33.0 + 15.6) / 18 = 2.70`), because this is a plain static that
must answer before any flyby exists, and
`MapGradeSanityTests.TheOverclockWindowAgreesWithTheTrainThatCausesIt` asserts the two still agree
AND that the derived rate still delivers the authored saving. Change the train's speed and it goes
red instead of silently paying a different amount.

⚠️⚠️ **THE THREE-RATE SWEEP IS STILL OWED AND `BotBehaviourProbe` CANNOT SUPPLY IT.** This is not
a scheduling excuse, it is the probe's own measurement: its seeding note records two runs of the
**same seeded build, back to back**, reporting **530 and then 83** unretrieved-slipper penalties,
because the match is stepped in real time at 6x and the bots think in frames. A comparison between
two such runs measures the machine. Every assertion in that probe is deliberately a liveness
FLOOR for the same reason. **Settling this wants the probe stepping the world by hand at a fixed
delta**, which is its own piece of work and is written up as § 10 below.

✅ **What did land is the second map.** `HeroStrikeBotsPlayAWholeMatchUnderTheBridge` runs a whole
Hero Strike match on Ilalim ng Tulay, so the flyby, the overclock window, the eight pillar hazards
and the trip hazards are exercised against the same liveness floors. The harness had never run a
match on any map but Eskinita, while two entries in this file argue that map geometry changes Hero
Strike outcomes. ⚠️ **The counts from the two maps must not be compared**, for the reason above;
what this catches is a map that breaks the loop.

⚠️⚠️ **THE SWEEP WAS RUN ON 2026-08-26 AND IT MEASURED THE HARNESS, NOT THE WINDOW.**
`BotBehaviourProbe.TheOverclockWindowSweep` runs four whole Hero Strike matches on Ilalim ng
Tulay in one session, at the shipped rate, at 1.0 (the window off), at the 2.25 midpoint and at
the shipped rate again, and compares the two shipped runs line for line before it will let anyone
read the table. **They disagreed, twice**, so the table is noise both times and the entry stays
open. § 16 is the investigation that produced, and the two determinism holes it closed.

The second run, after `Hitstop` was taught about captures:

```
  rate   saves  skills  ults  knocks  tags  throws  retr  restores  idlePen  frames
  3.50   6.75s      27    10      28    25      59    58        43      492   49654   (ship-a)
  1.00   0.00s      33    20      47    44      86    85        60      492   49761   (off)
  2.25   3.38s      33    13      35    29      66    65        51      358   49687   (mid)
  3.50   6.75s      38    20      50    50      94    92        65      332   49789   (ship-b)
```

⚠️ **READ THE FIRST AND LAST ROWS BEFORE THE MIDDLE TWO.** They are the same configuration and
they differ by 41 per cent of the throws. Everything between them is ordered by WHEN it ran.

**What the sweep is worth keeping for anyway:** it is the only thing in the repository that has
ever asked the probe to answer a comparison, and it is the reason two real determinism faults
were found rather than a table being quoted for the next year. It writes its rows incrementally,
so a run that goes red at the third rate still leaves the two it measured.

**Still needed, unchanged:** the three-rate comparison, once
`BotBehaviourProbe.TwoIdenticalMatchesAreIdentical` passes. The arithmetic in this entry (a rate
`r` held for a window `W` saves `W * (r - 1)` seconds whatever the cooldown is) is not in
question; what nobody has is what that saving is WORTH in a played round.

**Where.** `Assets/TumbangPreso/Runtime/Map/OverheadPassWindow.cs`,
`Assets/TumbangPreso/Tests/PlayMode/BotBehaviourProbe.cs`,
`Assets/TumbangPreso/Tests/MapGradeSanityTests.cs`.

---

## 11 · Every probe number ever printed was an average over a seat that could not play ✅ CLOSED 2026-08-26

**Found by reading the report rather than the assertions, while adding the Ilalim ng Tulay run.**

`GameLaunch.SoloSeat` defaults to **1**, so in a headless probe seat 1 was given a
`PlayerInputReader` and no human to drive it. Measured across all three matches on 2026-08-26,
before the fix:

| Run | seat 0 | seat 1 | seat 2 | seat 3 |
|---|---|---|---|---|
| Classic, Eskinita | 573.0 m / 3590 | **23.1 m / 30** | 477.9 m / 3510 | 463.1 m / 3020 |
| Hero, Eskinita | 1154.8 m / 6720 | **68.3 m / 50** | 999.0 m / 4670 | 1191.1 m / 6700 |
| Hero, Ilalim ng Tulay | 996.0 m / 6060 | **69.1 m / 40** | 1018.2 m / 4070 | 1064.0 m / 5070 |

⚠️⚠️ **It is not an AI bug and that is what made it invisible.** A parked human seat is correct
behaviour for a seat with no human on it. What was wrong is that the probe called this a four-bot
match, and every throw, tag and knockdown count it has ever printed was three bots' worth of
activity divided by four seats.

⚠️ **The "they leave spawn" floor was 20 m, which is under 23.1.** The one assertion that could
have caught it had been set just low enough to pass it, which is how a placeholder number becomes
a permanent blind spot.

**Fixed** with `GameLaunch.AllBots`, set and restored by `RunMatch`. `MatchInstaller` already had
`_allBots`, but it is a `[SerializeField]` authored per scene and a caller that loads a scene
cannot reach it before `Awake` reads it. ⚠️ Not `Spectator`, which also builds a fourth camera rig
and changes the HUD: a measurement may only change one thing. The travel floor rose to **150 m**,
comfortably under the 460 m a real bot manages in the shortest match and comfortably over a stuck
one.

⚠️⚠️ **EVERY NUMBER IN `Logs/bot-behaviour-*.txt` FROM BEFORE THIS DATE IS THREE SEATS' WORTH.**
Do not compare an old report against a new one.

---

## 10 · `BotBehaviourProbe` cannot answer a comparison, and every open balance question is one

**Raised 2026-08-26 while trying to run § 5's A/B, and it blocks § 0's too.**

The probe steps a match in real time at `MatchTimeScale` 6x and the AI decides in `Update` on
`Time.deltaTime`, so the number of decisions a bot makes depends on how busy the machine was. Its
own note records the consequence: two runs of the **same seeded build, back to back**, measured
**530 and 83** unretrieved-slipper penalties. `UnityEngine.Random.InitState` removes one source of
noise and the clock is the other.

Everything in this file that is still open and says "needs an A/B" is blocked on this:
 * **§ 0**, every Hero Strike cooldown, charge count and ultimate cost, all of which are starting
   positions with reasoning attached.
 * **§ 5**, the overclock window at 1.0 against a flat saving.
 * The Ice Barricade duration, if § 2's premise ever comes back.

✅ **DONE 2026-08-26. `BotBehaviourProbe.FixedStep` and `Time.captureDeltaTime` replace
`Time.timeScale = 6`.** A frame advances the same slice of game time whatever the machine is
doing, the guard is a frame budget rather than a wall clock, and the report prints frames and
simulated seconds beside the wall clock so only the reproducible numbers get compared.

⚠️⚠️ **AND THE FIRST VALUE WAS WRONG, WHICH IS THE MOST USEFUL THING THIS ENTRY NOW RECORDS.**
1/30 s was chosen from a wall-clock estimate of what 6x had been producing. It was not the same:
a Classic match at 1/30 reported **9 throws, 0 tags and 673 unretrieved-slipper penalties**
against 47 throws, 52 tags and 0 penalties on the same code the day before. The AI decides once
per `Update` on `Time.deltaTime`, so **the step IS the reaction rate**, and halving it does not
halve the outcome: a bot that re-decides half as often loses a 2.5 s charge to an interruption it
would otherwise have steered around, and the losses compound. 1/60 reproduces the shipped
numbers.

⚠️ **SO THE STEP IS A TUNING CONSTANT OF THE AI, NOT A HARNESS DETAIL.** Treat it like
`AiTuning`: if it moves, every figure in `Logs/bot-behaviour-*.txt` moves with it and none may be
compared across the change.

**What this was believed to unblock:** § 0 and § 5, on the grounds that two runs of one build
would be the same run.

⚠️⚠️ **THAT LAST SENTENCE WAS NEVER TESTED AND IT IS FALSE. SEE § 16.** The first sweep to run one
configuration twice measured 43 throws and then 83 on one build, one seed and one session. Two
causes were found and fixed and the runs are still not identical; what changed is that the spread
stopped being ordered by run order. An A/B on this probe has to be bought with repeats, and § 16
carries the arithmetic for how many.

**Where.** `Assets/TumbangPreso/Tests/PlayMode/BotBehaviourProbe.cs`.

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

✅ **DECIDED 2026-08-26: the third option. `AiDiagnosticProbe` is `[Category("WallClock")]` and
is excluded from the default PlayMode run.**

The entry asked for a decision rather than a bug hunt, and three data points all of one shape is
enough to make it: 21.6 s, then 29.9 s, then 37.6 s against a 20.0 s bound, each passing on an
immediate re-run with nothing changed. 37.6 is not a near miss; it is a round that spent most of
its wall clock somewhere other than this test.

⚠️ **The tests are not deleted and must not be.** They are the only thing in the harness that
explains WHY a bot did something rather than how much, and § 7 is explicit that the answer to a
slow suite is never to delete the measured ones. What changed is cadence: roughly **80 real
seconds** of every PlayMode suite now only runs when somebody is going to read the report.

⚠️⚠️ **`[Explicit]` ALONE DOES NOT WORK IN BATCH MODE AND WAS TRIED FIRST.** The run with the
attribute in place still reported 60 tests including both of these. The exclusion has to be in
the COMMAND, so the default PlayMode line in `CLAUDE.md` § 7 and `docs/TESTING.md` now carries
`-testCategory "!WallClock"`, and running them is `-testCategory "WallClock"`.

✅ **AND THE ONE GENUINE LEAD IN THIS ENTRY IS CLOSED, 2026-08-26.** The second failure printed
`own=3 plan=Fetch ownerAct=True d3=1.10 grabbable=True`: a bot 1.10 m from a grabbable slipper it
had already decided to fetch, still not holding it.

Reading the two constants side by side is what found it. `AIController.DoFetch` tapped Grab only
within **`AiTuning.Reach` = 1.15 m**, a generic melee reach shared with the shove and the punch,
while `Slipper.CanBeGrabbedBy` measures **`Balance.PickupRadius` = 1.40 m**. Between 1.15 and
1.40 m the pickup was legal, the bot knew where the slipper was, its plan was Fetch, and it
pressed nothing. `Goto` stops it at 0.86 m, but a bot is shoved, jostled and knocked back
constantly, and any drift into that 0.25 m band left it standing beside its own ammunition doing
nothing while the unretrieved-tsinelas penalty ticked.

⚠️ **The fetch now reads the rule's own constant**, so a retune of the pickup radius cannot leave
the AI reaching for the old one. ⚠️ The 1.10 m frame in the log is inside 1.15 and so is not
itself this bug; it is what made somebody look.

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

1. ✅ **DONE 2026-08-26. One launch, many checks: `TumbangPreso.EditorTools.Checks.RunAll`.**
   It runs `HeadlessCheck`, `ArenaCheck`, `MapGeometryCheck`, `AudioCueCheck` and
   `SceneScriptCheck` in a single editor start and exits non-zero if any fails. Five launches
   became one and no test logic changed.
   ⚠️ **It runs all five even after one fails**, because stopping at the first is how a session
   fixes one thing, relaunches, finds the second, and pays the start-up cost this exists to
   remove. Each check still writes its own report file, and `Logs/checks.txt` says which went
   red so the next reader opens one file rather than five.
   ⚠️ **A thrown check is a failed check, not a dead run.**
   ⚠️ **`HeadlessCheck` had to be split** into `Execute` and `Run`: it called
   `EditorApplication.Exit` from inside, which kills the process, so a batched caller reaching it
   first would never reach the rest.
   ⚠️ **`GameBuilder` still runs `SceneScriptCheck` separately and that is deliberate.** It is
   the only check that can see a scene holding a component the PLAYER cannot bind, a shipped
   build once crashed with everything else green, and a build-time gate must not depend on
   somebody having run this first.
2. **Name a fast gate and a full gate.** Fast: Core plus EditMode plus the combined checks, for
   every change. Full: adds PlayMode, for anything touching gameplay, and before a build.
   Right now every change pays for everything.
3. ✅ **DONE 2026-08-26** (§ 6). `AiDiagnosticProbe` carries `[Category("WallClock")]` and the
   default PlayMode command excludes it with `-testCategory "!WallClock"`.

**Done looks like:** a documented two-tier command list in `docs/TESTING.md`, and a full pass
that is fewer than four Unity launches.

✅ **ALL THREE ITEMS ARE DONE AS OF 2026-08-26.** A full pass is **three** Unity launches
(`Checks.RunAll`, EditMode, PlayMode) plus `dotnet test`, down from seven.

✅ **Item 2 is `tools/verify.sh`.** `./tools/verify.sh fast` for every change, `full` for
anything touching gameplay and before every build, `wallclock` for the probes that are a report
rather than a gate. It was prose in `docs/TESTING.md` until the script existed, and a rule that
lives only in a document is a rule that is followed until somebody is in a hurry.

⚠️ **It asserts on the XML and never on the exit code**, because a PlayMode crash, a genuine test
failure and a run that wrote no XML at all are three different things that look identical from a
shell. It prints a per-suite line and names every failing test, so a red run does not need a
second command to be readable.

---

## 58 · The ink outline tore open at every hard edge ✅ FIXED 2026-08-27

🧑 2026-08-27: *"the issue is the outlines dont fully connect"*.

**The outline is an inverted hull, and an inverted hull assumes ONE NORMAL PER POSITION.**
`Toon.shader`'s OUTLINE Pass draws back faces pushed out along `v.normal`. That is correct only
when every copy of a given corner agrees on which way "out" is. These rigs do not agree: a hard
edge, a UV seam or a material split all force the importer to emit the same corner SEVERAL times,
one per adjoining face, each carrying its own face normal. Pushing those copies along their own
normals sends them to different points in space, so the shell parts company with itself and the
border shows a gap.

⚠️ **The gap is widest where the angle is sharpest**, which is why it read as "some parts are
missing" rather than as a uniformly thin border. Two normals 90° apart separate by the full
outline width; a shallow bend barely parts at all. On a Kenney mini that is the fingers, the jaw,
the shoulders and the tops of the shoes, which is most of what the eye uses to read the pose.

**The fix is a welded normal.** `Visual.OutlineNormals.Weld` averages every normal sharing a
position and the Pass inflates along that average instead, so the copies all travel to the same
point and the shell stays closed.

⚠️⚠️ **It goes in the TANGENT channel, and a spare UV would have been wrong.** Unity skins
POSITION, NORMAL and TANGENT for a `SkinnedMeshRenderer` and passes UVs through untouched, so a
welded normal parked in UV3 would have stayed in bind pose and the border would have split open
the moment a limb rotated. The toon shader samples no normal map and never writes `o.Normal`, so
nothing reads a real tangent and the channel was free. **If a normal map is ever added to the toon
shader this has to move**, and there is nowhere good left to put it.

⚠️ **The weld hangs off `ToonSkin.Apply` rather than off the ten call sites.** That function is
the one chokepoint every outlined surface in the game already passes through, so a renderer cannot
acquire the toon shader and miss the weld. It caches per shared mesh, so twelve people wearing one
rig cost one bake between them and a respawn costs a set lookup.

⚠️ **The shader falls back to the raw normal when the tangent is zero.** A mesh imported without a
CPU copy cannot be welded, and inflating along a zero vector would collapse the hull onto the model
and delete the outline entirely. The fallback restores the pre-weld look for that mesh, which is a
seam rather than nothing at all.

⚠️⚠️ **THE PROPS WERE STILL TORN AFTER THE CHARACTERS WERE FIXED, AND ONLY THE TEST CAUGHT IT.**
The `.glb` people arrive from glTFast with a CPU copy and welded immediately. The nine inked
`.obj` props (four lata, four tsinelas, `viewmodel_arm`) were imported with Read/Write OFF, so
`mesh.vertices` came back empty, `Weld` returned early and their borders kept the exact split the
cast had just lost. **Nothing logged and nothing looked wrong in the inspector**, which is the
whole reason `EveryOutlinedMeshIsReadableSoTheWeldCanRun` is written as a readability assertion
rather than as a geometry one. `ModelImportSetup` now sets `isReadable` on them.

⚠️⚠️ **AND THE FIRST-PERSON HANDS SURVIVED EVEN THAT, BECAUSE THE MESH IS DUPLICATED.** 🧑
2026-08-27, after the cast and the props were both green: *"can you apply it also to the hands in
first person view"*. They were genuinely still torn. `ViewmodelArms` builds both arms from
`Resources.Load<Mesh>("Models/viewmodel_arm")`, which resolves to
`Assets/TumbangPreso/Resources/Models/viewmodel_arm.obj`, a **second, byte-identical copy** of
`Art/models/viewmodel_arm.obj`. The import fix and the test suite both walked `Art/models` only, so
**both reported success against a file the game never loads** while the mesh on screen for the
entire match kept its split border. `tsinelas_classic.obj` is duplicated the same way for the
slipper held in first person.

⚠️ **That is the sharpest version of the lesson in this whole entry.** A directory sweep proves
that whatever is on disk in that directory is correct. It cannot notice that the asset the player
actually looks at is somewhere else. `TheFirstPersonArmIsWeldedAtThePathViewmodelArmsActuallyLoads`
therefore names the path literally, the way `ViewmodelArms` spells it, rather than globbing.

⚠️ **The duplication itself is left alone on purpose.** Deduplicating a mesh that two loaders reach
by two different mechanisms is a separate change with its own failure modes, and it is not worth
bundling into an outline fix. It is written down here so the next person meets it deliberately.

⚠️ **`env_` is deliberately excluded from that, and the reason is memory rather than style.**
Readable means a second copy of the mesh in system RAM for the life of the process. The street
never reaches `ToonSkin.Apply` at all after the 2026-07-29 world-toon revert, so it has no hull to
weld and would be paying for a copy nothing reads.

**Verified:** `OutlineWeldTests`, four tests. One asserts the rig meshes are readable at all,
because a silent no-op on an unreadable mesh is the most likely way this regresses and it looks
completely normal in the inspector. One asserts the closure property directly as geometry, that
vertices sharing a position inflate the same way. One asserts the rigs still HAVE split normals,
so the suite reports it if the premise ever stops holding. One names the first-person arm at the
exact path `ViewmodelArms` loads, because a directory sweep cannot notice that the mesh on screen
most of the match is filed somewhere else.

---

## 63 · The world outline was aliased because MSAA was never able to see it

🧑 2026-08-28: the screen-space ink outline stair-steps, and thin geometry like the overhead
wires breaks into dashes.

**MSAA is on, MSAA is working, and MSAA cannot help.** Measured in play mode on 2026-08-28:
quality level Ultra, `QualitySettings.antiAliasing` 4, `Camera.allowMSAA` true, and a render
target requested with four samples really does come back holding four. But MSAA anti-aliases
GEOMETRY during rasterisation, out of coverage samples taken per triangle, and this outline is
painted by a fragment shader into an image that has **already been resolved**. There were never
any coverage samples for the ink. Every line it draws is hard-edged by construction, and a wire
narrower than a pixel is detected at some pixel centres and not others.

**The fix, on `feat/outline-supersample`: manufacture the coverage.** `WorldOutline.shader`'s
composite now evaluates the edge term at N x N sub-pixel positions inside each pixel and averages
the answers, and both thresholds became `smoothstep` over exactly the interval the linear ramp
already used, so `_DepthSensitivity`, `_NormalSensitivity` and both deadzones keep their meanings
and need no retuning. `WorldOutline._supersample` is the knob, `[Range(1, 3)]`, **default 2**,
because `QualitySettings.antiAliasing` is 4 and four sub-samples per pixel resolves the ink at the
same granularity as the geometry it sits beside. 1 restores the previous sampling exactly.

⚠️ **What it genuinely fixes and what it does not, because half of this is not fixable in screen
space.** The edge TERM gains resolution: it is a function of position, so four evaluations give
five ink levels instead of two, and the stair steps on wall and roof silhouettes go. The depth
DATA does not: `_CameraDepthNormalsTexture` is generated at camera resolution and point filtered,
so sub-pixel taps re-read the same texels in different combinations. **A wire the prepass
rasteriser missed at a pixel is missing from every sub-sample of that pixel.** The dashes get
softer ends and shorter gaps; they do not become solid lines.

⚠️ **The thing that would fix the wires was costed and rejected rather than overlooked.** Genuinely
finer depth means rendering the depth-normals prepass at 2x, through a second camera doing
`RenderWithShader` with `Hidden/Internal-DepthNormalsTexture` into a target of four times the area:
a SECOND full rasterisation of roughly 450 renderers on a dressed Eskinita, on top of the one this
feature already added, on the machines whose 2026-07-29 report was *"severe lag on other PCs"*.
The feature's whole claim to being worth retrying is that it costs one scene pass instead of 450
extra draws. **If the wires are judged worth it, that is the upgrade, and it must be measured on a
low-end machine before it ships, not after.**

**Cost, in full.** Still one full-screen pass and zero extra render targets, so no extra memory of
any kind. The composite's tap count goes from 8 to 4N² + 4: 8 at N = 1, **20 at N = 2**, 40 at
N = 3. The mask is sampled once per pixel outside the loop rather than once per sub-sample, which
is where the `+ 4` instead of `+ 4N²` comes from, and it is exact rather than an approximation
because the mask multiplies the result: `mean(edge) x mask` and `mean(edge x mask)` are the same
number.

⚠️ **The exclusion mask stays at camera resolution, and that is the reason the fragment does the
sub-sampling instead of a 2x blit.** Blitting the composite into a 2x target would have left the
mask, which is allocated from `_camera.pixelWidth/pixelHeight`, at 1x, and the exclusion would have
crept by half a pixel across the frame. Sub-sampling in the fragment means nothing changed
resolution and nothing can misalign. **The one thing that did have to follow is the dilation
radius:** the mask is max-sampled at the tap radius to cover the hull's ink band, and the widened
kernel now reaches a further `centre/N` of a texel, a quarter of a pixel at N = 2. The shader adds
that in. Without it a ring that thin around every character would have been inside the edge term's
reach and outside the mask's, putting back the faint second hairline the dilation exists to remove,
and only at N > 1.

⚠️ **`ShadowsOnly` renderers are still skipped when the mask is built.** That is § 58's sibling fix
on `integration/ui-batch-on-ilalim`, the one that stopped the local player's self-hidden head
masking most of the screen. Nothing here touches it. Do not let a merge drop it.

**Not yet verified:** this branch authored the change without launching Unity. What is still open
is (a) that the shader compiles, in particular `#pragma target 3.0`, `[unroll(3)]` over a
uniform bound and `tex2Dlod` on `_CameraDepthNormalsTexture`; (b) an A/B render at
`Supersample` 1 against 2 on the same frame; (c) the frame cost of the extra 12 taps on a low-end
machine. `WorldOutlineCoverageProbe` already renders on and off at 2.06:1 and prints ink percentage
per cell, so it is the cheapest place to add a 1-against-2 arm.
## 63 · Walking into a utility pole blanks half the screen, and it now dithers away

🧑, off the played build: a wooden utility pole filling the **entire left half of the screen**,
view completely blocked, with a reference image of the screen-door stipple that games use for
near-camera occluders.

**Measured, so nobody has to re-derive it.** `env_post_electric.obj` is 7.2 m tall and 0.634 m
across the timber, taken from the .obj's own vertices. `CameraRig` runs a 95 degree VERTICAL FOV
in first person, about 125.4 degrees horizontal at 16:9, with the eye 1.25 m up and a 0.05 m near
plane. The post therefore subtends, as a fraction of frame width:

| eye to post surface | frame width covered |
|---|---|
| 2.00 m | 12.5 % |
| 1.20 m | 18.8 % |
| 0.60 m | 30.5 % |
| 0.35 m | 40.4 % |
| 0.20 m | 92.0 % |

⚠️ **And nothing stops you reaching the bottom of that table.** The poles carry no collider on
either map: `Eskinita.unity` has six colliders in total (four walls, the floor, the kill plane) and
`IlalimNgTulayBuilder` never adds one to a kit prop. `MatchInstaller.StripColliders` exists to take
colliders OFF, because every contact in this game is a host-side distance check. So the
`CharacterController`'s 0.35 m radius is not a bound here: the eye goes inside the post.

### 63.1 ✅ SHIPPED: a screen-door dissolve in the occluder's own fragment shader

⚠️⚠️ **IT CANNOT BE A POST-PROCESS, AND THAT DECIDED EVERYTHING ELSE.** Fading an occluder means
revealing what is BEHIND it, and a full-screen pass only ever has the composited frame: the pixels
behind the pole were never rendered. The only place the geometry behind an occluder can still be
drawn is the occluder's own fragment shader, discarding before it writes colour or depth.

`Assets/TumbangPreso/Shaders/NearFade.shader`, `Runtime/Visual/NearFade.cs`, one call at the end of
`EnvColourPass.Apply`, one line in `GameBuilder.EnsureRuntimeShaders`, `Tests/NearFadeTests.cs`.

**The band, in radial metres from the eye to the FRAGMENT:** solid beyond **1.80 m**, gone under
**0.35 m**, `smoothstep` between. Multiply the coverage above by the fraction still drawn and the
product peaks at **13.6 %** and falls from there, so the post can never obscure more than about an
eighth of the frame. 0.35 m is seven times the near plane, so the near plane never gets to slice a
solid cross-section. The band is 1.45 m wide because `Balance.Speed` is 4.6 m/s and an attacker
runs at 3.45, which puts the whole dissolve between 0.32 s and 0.42 s of walking.

⚠️ **PER FRAGMENT, NOT PER OBJECT, AND A 7.2 m POLE IS WHY.** A per-object fade would dissolve the
crossarm seven metres over your head at the same rate as the section in front of your face.

⚠️ **RADIAL, NOT VIEW-SPACE Z, AND THE WIDE LENS IS WHY.** At the edge of a 125 degree frame a
fragment's z is under half its true distance, so a z-driven band dissolves a pole 2 m off to your
side while leaving one 1 m dead ahead solid. Exactly backwards.

⚠️ **THE DITHER CELL IS 2 SCREEN PIXELS, NOT 1.** `PostAntiAlias` runs FXAA over both gameplay
cameras and a one-pixel checker is below the scale its edge test resolves, so it smears into a flat
haze and the effect reads as fog rather than as a screen door. At 2 the pattern's period is 8 px.

**Scope: the named props only.** `NearFade.OccluderPrefixes` is `Poste` (Eskinita, 12 under
`Dressing/Kable`) and `SidewalkPole` (Ilalim ng Tulay, 28), both counted off the shipped scenes.
Putting the whole street on a new shader is the 2026-07-29 shape again and buys nothing: a house is
8 m wide and its walls stop you. Trees were considered and left out because a 2.4 m canopy
dissolving at head height removes a large part of the frame rather than a thin strip.

### 63.2 ⚠️⚠️ OPEN, AND IT IS ONE LINE IN A FILE THIS WORK DID NOT OWN

**`WorldOutline` will trace a pole's ink silhouette while the pole is dissolving.**

`CameraRig` attaches `WorldOutline` and sets `PrototypeEnabled`, so the screen-space ink is live on
the match camera. It reads `_CameraDepthNormalsTexture`, and in the built-in pipeline that texture
is filled by Unity's `Internal-DepthNormalsTexture` REPLACEMENT shader, chosen by the object
shader's `RenderType` tag. A replacement shader brings its own code, so **a `clip()` written in
`NearFade.shader` is structurally invisible to it.** There are exactly two reachable behaviours:

* **`RenderType = "Opaque"`**, which is what shipped. Ink and occlusion are correct at every
  distance, and inside the 1.8 m band the outline traces a silhouette the prop no longer has.
* **Any other tag value.** The prop leaves the depth-normals texture entirely, so it can never
  ghost, but it loses its own ink permanently AND stops occluding the edges behind it, so rooflines
  draw straight across a pole in a street where poles stand against facades.

Opaque wins because its cost is bounded to the band and the other's is not.

⚠️ **THE ACTUAL FIX IS IN `WorldOutline.IsToonSurface`.** It already answers "does this renderer
draw its own ink, so keep the screen-space pass off it" by comparing `material.shader.name` against
`TumbangPreso/Toon`. A prop on `NearFade.ShaderName` belongs in that same set for the inverted
reason: it must NOT be inked, because its silhouette is a lie while it is dissolving. Widening that
one comparison masks the ghost while KEEPING the prop in depth-normals, which is the behaviour both
options above are trying to buy and neither can. `NearFade.ShaderName` is a public const so that
line needs no second string literal.

**Done looks like:** `WorldOutline.IsToonSurface` accepts both names, and a render taken from
0.8 m off a `Poste_*` on Eskinita shows a stippled post with no ink around it and correct ink on
the facade behind.

### 63.3 What could NOT be verified without a Unity launch

Everything below is authored and reasoned from source. **None of it has been compiled, rendered or
played**, because the session that wrote it was not permitted to launch the editor.

* **That the shader compiles at all**, and that a surface shader's `screenPos` gives the pixel
  coordinates this assumes after the perspective divide.
* **That the dissolved shading matches the solid one.** The lit half is
  `#pragma surface surf Standard`, and the two shaders it replaces are Unity's `Standard` (the
  `.obj` posts) and glTFast's `glTF/PbrMetallicRoughness` (the kit poles), whose property names were
  read out of the package rather than guessed. The BRDF should be the same; only a render says so.
* **Whether 1.80 m reads as too early.** The number comes from screen coverage, not from taste, and
  taste is the half a render settles.
* **Whether a 2 px cell survives FXAA as a clean grid.** The argument is sound and the measurement
  is a screenshot with anti-aliasing ON.

**First thing to look at:** a first-person render taken about 0.8 m from a `Poste_*` on Eskinita,
with FXAA on. It answers the compile, the shading match, the stipple size and § 63.2's ghost in one
picture.

---

## 64 · The player can switch render styles, and the alternative is a chromatic look

🧑 wants to look at a softer post-processed frame with visible colour fringing INSTEAD of the hard
ink outlines, and wants to flip between the two from the settings panel while deciding. This is an
A/B, not a replacement.

⚠️⚠️ **TOON IS ROW 0, IT IS THE DEFAULT, AND EVERY SWITCH IN IT IS AN EXACT NO-OP.** `WorldOutline`
already carries the rule this is an application of: *"a prototype that quietly becomes the look
because it happened to be enabled on one camera in one scene is the failure mode to guard against"*.
A player who never opens settings, and a player upgrading from a `settings.json` written before the
field existed, both render exactly what this branch inherited.
`LobbyAndSettingsTests.RenderStyleDefaultsToToonAndIsClampedIntoTheTable` and
`ToonRowIsAnExactNoOpAndEveryOtherStyleChangesSomething` assert both halves rather than trusting
them.

### 64.1 What the two rows are

`Assets/TumbangPreso/Runtime/Settings/RenderStyles.cs` is the table, built on the shape
`AntiAliasModes` established: an entry struct, a stored int on `GameSettings` with a clamp in
`Validate` and a push in `Apply`, and one dropdown row in `ConvertedSettingsPanel`.

| | Toon (row 0) | Chromatic (row 1) |
|---|---|---|
| `Toon.shader` OUTLINE pass | draws | suppressed |
| `Visual.WorldOutline` | live | gated off |
| persistent colour split | 0.00 | 0.25, radial |

### 64.2 The three switches, and why each is where it is

* **The hull outline is suppressed by a GLOBAL shader float**, `_OutlineSuppress`, declared in
  `Toon.shader`'s OUTLINE pass CGPROGRAM and deliberately **absent from its `Properties` block**,
  which is what keeps it global rather than per-material. `ToonSkin.SetOutlinesSuppressed` is the
  only writer.
  ⚠️ **The two alternatives both break something `ToonSkin` had already fixed.** Swapping the
  material for an outline-free shader fights the `(source material, quantised width)` cache and the
  `Origin` map that make `Apply` idempotent, and would need the palette remap, the welded tangent
  and the carried atlas re-derived on a second branch. Writing `_OutlineWidth` to zero on the cached
  materials means re-dressing every renderer in the arena on a settings pick, across **more than
  thirty `ToonSkin.Apply` call sites**, and DOUBLES the cache, because the width is part of its key.
  ⚠️ **The sense is inverted (0 suppresses nothing) because an unset global reads 0**, and six
  editor probes dress models and render them without ever loading a settings file. A flag named
  `_OutlineScale` with 1 meaning "draw" would have silently deleted the ink from every turnaround,
  lineup and showcase render in the repo.
  ⚠️ **A zero width collapses the hull to a degenerate triangle rather than drawing it.** `Cull
  Front` draws BACK faces; at width 0 they land on the surface, and on coplanar geometry (a
  zero-thickness sheet, a card) that z-fights the lit pass as near-black speckle. Three vertices at
  one clip position is a zero-area triangle, so the pass also stops paying its fill rate.
* **The world outline is gated in `WorldOutline.Live`**, beside `_prototypeEnabled` rather than by
  clearing it. `CameraRig.Awake` sets that flag true on every match camera it builds and would put
  the outline back on the next scene load. `Live` also gates `LateUpdate`, so Chromatic mode drops
  the depth-normals request and the exclusion-mask rescan too, not just the composite.
* **The split is read by `ColourGrade` and ADDED to the impact pulse**, never max'd with it. See
  § 64.3.

### 64.3 The persistent split adds to the transient one, and a `Max` would have been wrong

`Visual.HitFeel.ChromaticPeak` is **0.10 / 0.22 / 0.35 / 0.55** by hit weight and `HeroAbilitySystem`
pulses an ultimate at **0.95**. Against a base of 0.25:

* Under `Mathf.Max` the two lightest hits are **swallowed**: `max(0.25, 0.10)` is 0.25, the frame
  does not move, and the feedback whose entire job is to say "you were hit" fires and shows nothing.
* Under a **sum**, every hit moves the frame by its own full peak whatever the base is, so the pulse
  keeps the amplitude it was tuned at. 0.25 + 0.55 is 0.80 and still in range; only an ultimate
  saturates, and 0.95 was already within five per cent of the top.

**0.25 is solved for, not picked.** The shader's constant is 0.006 in UV, which is 11.5 px of a
1920-wide frame at amount 1. Radially that offset is reached at the left and right edges, about
0.0085 at the corners and zero at the centre, so 0.25 is **about 2.9 px of fringe at the edges and
4.1 px at the corners**, against the 6.6 px a heavy hit already puts across the whole frame.

### 64.4 The split is now radial, and the impact path is untouched on Toon

⚠️ **The shipped split is `half2(_Chromatic * 0.006, 0)`: the same offset at every pixel.** Held for
0.4 s by a hit that is an impact artefact and is fine. Held for a whole match it fringes the
crosshair, the centre of the HUD and every piece of text, which is exactly where a real lens fringes
nothing: refraction disperses by angle from the optical axis.

`ColourGrade.shader` now takes `_ChromaticRadial`, and **0.012 is solved for**: at the left and
right edges `d.x` is 0.5, so `0.012 * 0.5` is exactly the 0.006 the flat path uses everywhere. It is
a `lerp` on a uniform rather than a branch, and `RenderStyles.RadialSplit` is **false on row 0**, so
in the default look the shader evaluates the identical horizontal term it always has.

### 64.5 Cost

**No new pass.** The split rides in `ColourGrade`'s existing full-screen blit and the suppression
rides in `Toon.shader`'s existing OUTLINE pass.

⚠️ **One honest caveat.** `ColourGrade.IsIdentity` skips its blit outright on a map that grades
nothing, and **Bayan Plaza is such a map** (it has no `adjustment_enabled` line at all). A non-zero
persistent split makes that frame non-identity, so Chromatic mode **un-skips an existing blit**
there. It does not add one. Chromatic also stops paying for the inverted hull's fill rate and for
`WorldOutline` entirely, so the net is very likely negative.

### 64.6 What could NOT be verified without a Unity launch

Authored and reasoned from source. **Nothing below has been compiled, rendered or played**, because
the session that wrote it was not permitted to launch the editor.

* **That both shaders compile**, in particular that a uniform declared in a CGPROGRAM and left out
  of `Properties` resolves from `Shader.SetGlobalFloat` on every backend in this project's build set.
  This is the single load-bearing assumption of the whole feature.
* **That the degenerate-triangle early-out actually discards**, and that `UNITY_TRANSFER_FOG` on a
  `(0,0,0,1)` clip position is harmless in every fog variant.
* **Whether 0.25 is the right amount of fringe.** The number comes from pixel arithmetic, not from
  taste, and taste is the half a render settles.
* **Whether Chromatic reads as "softer and warmer" at all.** Only the outlines and the split are
  implemented; **no grade change was made**, because `MapGrade` owns brightness, contrast and
  saturation per map and a style that overrode them would fight Eskinita's authored 1.03 / 1.18. If
  warmth is wanted it belongs as two more fields on `RenderStyles.Entry`, multiplied into
  `ColourGrade` the way `SetEventGrade` already multiplies rather than replaces.

**First thing to look at:** an in-game render of Eskinita on each style, same camera, same frame.
It answers both compiles, whether the ink is genuinely gone rather than z-fighting, and whether the
fringe at 0.25 is visible without being a glitch. After that, a render taken during an impact in
Chromatic mode, which is the only thing that shows the sum and the radial profile behaving together.

---

## 65 · The white keyline round every silhouette, measured rather than argued

🧑 2026-08-28, having tested all three rows: *"off and fxaa gets rid of the outlines. msaa brings
it back"*. That isolates MSAA as **necessary**. It does not identify the mechanism, and § 64 shows
why that matters: the chromatic split was blamed for *"white outlines on distant objects"* first,
correctly for that build, and fixed. The report survived the fix.

`Settings.AntiAliasModes` and `Visual.PostAntiAlias` both carry the diagnosis: multisample resolve
AVERAGES its samples in linear HDR **before** `Visual.ColourGrade` runs its ACES curve, the curve is
compressive, so `tonemap(mean(a, b))` is not `mean(tonemap(a), tonemap(b))`. The shipped mitigation
is `PostAntiAlias.ApplyHdrForResolve`, which turns `allowHDR` off on the MSAA rows only.

**Nobody had put a number on any of it.** This entry does, and the numbers change the conclusion in
two places.

### 65.1 The arithmetic on record undershot by 2x to 3x, and the two amplifiers are both measurable

The estimate that opened this work used a sky of about **1.0 linear** against a roof of 0.3, took
the 50/50 pixel, and got a difference of **0.06** of display luma. That is smaller than the reported
artefact, which is what made the diagnosis worth checking rather than acting on. Both halves of that
estimate are wrong, and both in the same direction.

**Amplifier one: the sky is 2.0 to 3.2 linear, not 1.0.** `Sky.mat` is `Skybox/Panoramic` with
`_Exposure` 1 and `_Tint` **(1, 1, 1)**. Every built-in Unity skybox shader multiplies by
`unity_ColorSpaceDouble`, which is **4.59479** in linear colour space, and the shader's own neutral
tint is (0.5, 0.5, 0.5) precisely so that the product lands near 1. This material is at double
neutral. Measured off `Assets/TumbangPreso/Art/models/materials/sky_panorama.png` (2048x1024,
`sRGBTexture: 1`), sampled on an 8 px grid:

| band of the lat-long | mean linear luma | after x4.59479 | brightest texel, after x4.59479 |
|---|---|---|---|
| zenith, top 15 % | 0.179 | 0.82 | 3.82 |
| upper, 15 to 35 % | 0.256 | 1.17 | 3.90 |
| **just above the horizon, 35 to 50 %** | **0.436** | **2.00** | **3.18** |

The band that matters is the third one, because a distant roofline is seen against the sky just
above the horizon, not against the zenith.

**Amplifier two: the worst pixel is not the 50/50 one, it is the one that is mostly ROOF.** The
curve flattens fastest just as the mean climbs past 1.0, so the error peaks at LOW sky coverage.
With sky 2.0 linear, roof 0.3 linear, `_Exposure` 0.92, `_White` 1.9 and the 1.96 pre-scale, so
`tonemap(c) = saturate(ACES(1.8032 c))`, `tonemap(2.0) = 0.967` and `tonemap(0.3) = 0.642`:

| sky samples of 4 | resolved then tonemapped | tonemapped then resolved | error |
|---|---|---|---|
| 3 of 4 | 0.950 | 0.886 | 0.064 |
| 2 of 4 | 0.919 | 0.804 | **0.115** |
| **1 of 4** | **0.855** | **0.723** | **0.132** |

At a bright patch of sky (3.18 linear) the 1-of-4 pixel goes to **0.177**, about **45 levels of
255**. So the predicted artefact is a pale band **on the dark object, one to two pixels inside its
silhouette**, 0.13 to 0.18 of display luma, and that is a keyline rather than a soft edge. It also
explains the shape of the complaint: a softened edge would read as blur, and what was reported is a
line.

⚠️ **This is arithmetic over measured inputs, not a rendered measurement.** It closes the gap that
made the diagnosis doubtful; it does not by itself convict the resolve. § 65.2 is what settles it.

### 65.2 `MsaaResolveProbe` is the measurement, and it is five arms in one frame

`Assets/TumbangPreso/Tests/PlayMode/MsaaResolveProbe.cs`, `[Category("WallClock")]`. It loads
Eskinita, pitches the rig camera to a fixed 12 degrees above the horizon so there is guaranteed
skyline in frame, and renders the same instant five times:

| arm | msaa | `allowHDR` | `WorldOutline` | what it is |
|---|---|---|---|---|
| A | 1 | on | on | the reference, i.e. AA Off |
| B | 4 | on | on | the artefact, i.e. the game before the workaround |
| C | 4 | **off** | on | what `integration/ui-batch-on-ilalim` ships today |
| D | 1 | on | **off** | reference with the ink pass out of the way |
| E | 4 | on | **off** | artefact with the ink pass out of the way |

It writes all five plus four difference images to `Logs/shots-msaa/`, and prints, per comparison:
max and mean luma increase, the count of pixels brighter by more than 0.02 / 0.04 / 0.10 / 0.20, an
8x5 grid of where they are, and **the share of brightened pixels that sit within 2 px of both a
bright and a dark pixel in the reference frame**.

⚠️ **That last number is the whole test.** A resolve can only misbehave where the samples inside one
pixel disagree, so if the brightening is genuinely the HDR resolve then almost all of it lands on
high-contrast boundaries. **A low share means something is brightening flat surfaces and the
diagnosis on record is wrong**, which is a more valuable answer than the fix would have been.

Three construction notes, each of which was a way to get a wrong answer:

* ⚠️⚠️ **Every arm writes into an identical ARGB32 sRGB destination.** The obvious build gives the
  HDR arm an `ARGBHalf` target, and that is wrong: `ReadPixels` off a half-float target returns
  LINEAR values and off an `ARGB32` target returns sRGB-ENCODED ones, so the arms would differ by a
  transfer function everywhere and the difference image would mean nothing. HDR is varied with
  `Camera.allowHDR` alone, which is what picks the format of the intermediate the scene is
  rasterised into and resolved from.
* ⚠️ **`allowHDR` is written one statement before `Render()`**, because `PostAntiAlias.LateUpdate`
  owns that field and writes it every frame. A `[UnityTest]` coroutine resumes in the Update phase,
  so LateUpdate has not run yet and the value holds for that render, then is taken back next frame
  with no cleanup.
* ⚠️ **MSAA is set on the descriptor AND on `QualitySettings` together.** A camera writing into a
  `targetTexture` takes its sample count off that texture; one writing to the screen takes it off
  `QualitySettings`. This camera is doing the first while carrying the image effects of the second,
  and which governs has never been pinned down here, so the probe sets both and prints what the
  target actually came back holding. If an arm reports 1 delivered sample the log says outright that
  every number below it is measuring something else.

**How to run it:**

```bash
"/c/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe" -batchmode -projectPath . \
  -testPlatform PlayMode -runTests -testCategory "WallClock" \
  -testFilter "MsaaResolveProbe" -testResults Logs/msaa.xml -logFile Logs/msaa.log
```

⚠️ **No `-nographics`.** It crashes the editor rather than the tests, writes no `.xml`, and still
exits 0. Assert on the `.xml`, and read the `=== MSAA RESOLVE ===` block out of `Logs/msaa.log`.

⚠️ **`MsaaResolveProbe.Version` is a const, and it must be bumped before any run whose images will
be looked at.** `CLAUDE.md` § 6.1: chat clients cache by filename, so overwriting a render leaves
the previous one on screen and the whole review is conducted against an image that is no longer on
disk.

### 65.3 The shipped workaround's cost, in numbers rather than in "mild"

`PostAntiAlias` says clamping at 1.0 costs the highlight roll-off and calls that mild on a flatly
lit game. With the sky measured, the number is:

* `tonemap(1.0) = 0.902`. Anything at or above 1.0 linear collapses to that.
* The sky at **2.0 linear** rendered **0.967** under HDR and renders **0.902** clamped: **6.5 per
  cent darker**. At a bright patch, 3.18 linear, it was 0.992: **9 per cent darker**.
* Worse than the darkening: **the sky and a fully lit 1.0-linear white surface become the same
  number**, where HDR separated them by 0.065 to 0.089. That is the roll-off doing its only job.
* **Everything below 1.0 linear is bit-identical**, because the clamp does nothing there. The cast,
  the street and every midtone are untouched. That half of the claim holds exactly.

⚠️ **So the AA setting changes the exposure of the game**, and the FXAA rows and the MSAA rows are
two slightly different pictures of the same scene. `MsaaResolveProbe` prints mean whole-frame luma
per arm specifically to size this. If A minus C is large it is a defect of its own and belongs in
this entry as its own item.

### 65.4 ⚠️⚠️ THE WEIGHTED RESOLVE WAS NOT BUILT, AND THE REASON IS ARCHITECTURAL RATHER THAN HARD

A Karis-style tonemapped resolve weights each sample by `1 / (1 + luma(sample))` before averaging
and divides by the summed weights, so the mean is taken in a perceptually flatter space. The maths
is four lines. **It cannot be reached from where this game's post chain stands, and finding out
costs a rewrite rather than an experiment.**

**The blocker, stated exactly.** In the built-in pipeline the engine resolves MSAA before
`OnRenderImage` is ever called, so `source` has one sample and there is nothing to weight. Sample
access requires the camera to render into a target THIS code allocated with `bindTextureMS = true`,
sampled as `Texture2DMS<float4>`. But **a camera carrying any `OnRenderImage` component does not
render into `camera.targetTexture`**: Unity allocates its own intermediate for the chain and blits
into the target at the end. `Visual.WorldOutline`, `Visual.ColourGrade` and `Visual.PostAntiAlias`
are all `OnRenderImage`, and `CameraRig.Awake` adds all three. So the scene camera would have to
carry **none** of them, and the whole chain would have to move to a second composite camera.

**And `WorldOutline` cannot follow, which is what makes this circular rather than merely large.** It
needs `_CameraDepthNormalsTexture` from the camera that rasterised the scene, a `CommandBuffer` at
`CameraEvent.BeforeImageEffectsOpaque` on that same camera to draw its exclusion mask, and view rays
reconstructed from that camera's projection. A composite camera that culls nothing has none of
those. Leaving the outline on the scene camera puts an `OnRenderImage` back on it, which is exactly
what destroys the sample access. Getting out of that means re-authoring the outline as a
`CommandBuffer` pass, which is a rewrite of a feature that landed two days ago and is still a
prototype under review (§ 63).

**Everything else that would also have to move:** `SpectatorCamera` builds the same chain and adds
the replay capture on top; `AspectRatioProbes`, `GameplayShots` and `WorldOutlineCoverageProbe` all
drive `cam.targetTexture` directly, which a scene camera that owns a multisampled bound target can
no longer allow; and `PostAntiAlias.ReportOnce` filters on `targetTexture == null` to decide which
camera is the screen, which stops being a valid question.

**Cost, if it is ever built.** `bindTextureMS = true` means the multisampled surface stays resident
and is read by a shader instead of being resolved on store, so at 1920x1080 in `ARGBHalf` the 4x
surface (**66 MB**) and the resolve destination (**16 MB**) are both live, against one transient 4x
surface today. The hardware resolve is fixed-function and effectively free; it is replaced by a
full-screen pass doing **8.3 million half4 fetches per frame** at 4x, and 16.6 million at 8x. The
shader needs `#pragma target 4.5`. That is not a real hardware objection on this project's build set
(`GameBuilder` ships StandaloneWindows64 and StandaloneOSX only, and both D3D11 feature level 11 and
Metal support `Texture2DMS`), so **do not reject it on hardware grounds**; reject it on the chain.

⚠️ **What is NOT a cheaper way out, so nobody spends a session on it.**

* **Lowering `Sky.mat`'s `_Tint` to Unity's neutral 0.5** halves the sky to about 1.0 linear and
  reduces the 1-of-4 error from 0.132 to roughly 0.10. It is a real contributor and a **25 per cent
  improvement, not a fix**, and it changes how the game looks. Worth knowing, not worth shipping as
  a fix.
* **Lowering `_White` to put the curve's white point back at 1.0 under the clamp** needs
  `_White = 0.473`, which takes mid grey from 0.71 to 0.97. That is not restoring the roll-off, it
  is cranking exposure until nothing has any.
* **Moving the tonemap back into `Toon.shader`** is explicitly forbidden by that shader's own note
  and by `ColourGrade`'s: the two would compound, and it would tonemap the cast while leaving the
  sky, the street and the fog raw, which is the fault the pass was created to fix.

**The recommendation:** keep `PostAntiAlias.ApplyHdrForResolve` as it is. It is scoped to the MSAA
rows, its cost is now quantified in § 65.3 and is confined to values above 1.0 linear, and the
default row is FXAA anyway, so the ordinary player never meets it. Revisit the weighted resolve only
if `WorldOutline` is either accepted and re-authored as a command-buffer pass, or rejected and
removed. Either outcome makes the composite-camera split tractable; while it stands as an
`OnRenderImage` prototype, it does not.

### 65.5 What could NOT be verified without a Unity launch

Authored and reasoned from source. **The probe has never been run**, because the session that wrote
it was not permitted to launch the editor.

* **That `MsaaResolveProbe` compiles.** It is the only new file and it uses nothing exotic, but it
  has not been through the compiler.
* **That the five arms actually differ.** If `rt.antiAliasing` reports 1 on arms B, C and E then a
  `targetTexture` render never gets MSAA in this configuration, the whole probe is measuring one
  frame five times, and it has to be rebuilt against the screen path instead. The probe prints this
  and says so in the log rather than passing quietly, but **it is the first thing to read**.
* **That the forced pitch of -12 degrees actually finds a roofline** from wherever the scene parks
  the rig. If the difference images are empty, check `A_off_hdr_v1.png` before concluding anything.
* **Whether `WorldOutline` is a co-cause.** Arms D and E exist to answer it and nothing else here
  assumes an answer.
* **The 0.13 to 0.18 prediction itself.** § 65.1 is arithmetic over a measured texture and a
  transcribed curve. The probe is what turns it into a measurement.

**First thing to look at:** the `arm ... target delivered N` lines at the top of the probe's log
block. Every other number in it is worthless if that reads 1 where 4 was asked for.

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
