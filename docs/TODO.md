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
2. ⚠️⚠️ **`EveryHeroAbilityHasBespokeCastAndViewModelActions`: *"phaister: KULAM HEX
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
| Kulam Hex | two stacked discs at `r*2.0` and `r*1.25`, plus 3 spokes and 6 cubes | about **18 m²** of a 196 m² court for one SKILL | pentagram line art, about 1.4 m² |
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

## 22 · Every settings slider took no pointer event at all ✅ CLOSED 2026-08-27

**The report: the settings sliders are "hardcoded and broken", and the volume cannot be changed
with the mouse.** All four of them, on both the title screen's panel and the pause overlay, which
instances the same converted panel.

**They were not hardcoded and their listeners were wired.** They were receiving no pointer event
at all, so a press at the centre of a volume row went through the slider and landed on the card
behind it.

### 22.1 The cause: one sweep that can only see a hit area on the control's own node

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

### 22.2 The hit area is the whole row, not the groove

`BuildSlider` lays a transparent Image over the slider's own node, and `MenuKit.EnsureHitArea`
does the same at runtime for the panels that are already committed as prefabs. Both are the
control's full rect on purpose: the converted groove is a **14 px band centred in a 34 px row**,
so restoring the Background alone would have handed the player a 14 px tall target. Alpha plays
no part in a graphic raycast, so nothing about the drawing changes.

⚠️ **The runtime repair is not belt and braces.** The converted panels are committed assets; a
player running the shipped build never re-runs the importer, so the importer fix alone would have
changed nothing until somebody reconverted the scenes.

### 22.3 The second defect on the same row: a window resize per drag frame

Every slider's callback called `SettingsStore.Current.Apply()`, and `GameSettings.Apply` is
`ApplyDisplay` plus the AI difficulty. **`ApplyDisplay` is a `Screen.SetResolution`**, so dragging
one volume slider across its groove fired a window resize on every frame of the drag. No slider on
this panel feeds either system: the three volumes are read live off the store by the music bed,
the announcer and the SFX bus, and the sensitivity is read live by `CameraRig` and
`SpectatorCamera`. The call is gone.

### 22.4 Why nothing caught it

`UiClickProbe` is the only check in the project that can see this class of bug, and its comment
said in as many words that it enumerated Buttons and Dropdowns alone and that sliders should be
swept in **deliberately, not by accident.** It is widened to Sliders now, which is the regression
test: it scrolls each one into view, raycasts its centre, and fails if the topmost hit is not the
control or one of its own children.

---

## 24 · Both intermission banners were drawn on top of something ✅ CLOSED 2026-08-27

Reported off a Hero Strike frame of Ilalim ng Tulay: raise the practice line, lower the "open a
gap with your powers" line because it covers things, and make that one go away after five to ten
seconds because it is annoying.

**All three are one fault repeated: the two banners were positioned against the screen edges
rather than against what is already parked at those edges.** The arithmetic, because it is not
close in either case.

### 24.1 The practice line was inside the ability deck

Both decks are bottom-anchored with a bottom pivot. The hero row spans **y 14 to 92**
(`DeckBottomMargin` + `DeckHeight`); the Classic row spans **24 to 124**. `ReadyPromptPlate` was
pinned at 92, so it drew from **92 to 126**: flush against the top edge of the hero deck, and
**32 of its 34 px inside the Classic one**. `InspectHint` at 78 was fully buried by both, which
means the one line in the game that names the inspect key has never been legible in Classic.

They are stacked upward off the taller deck now, so one set of numbers is right in both modes:
Classic's 124 is the floor, the hint takes 132 to 150, the prompt plate 156 to 190.

### 24.2 The objective line was in the LATA DOWN band

`ReadyObjective` at -206 with a top pivot spanned **206 to 244**. `LataDownAlert` is at -228 and
70 tall, so it owns **228 to 298**, and `ToastLabel` owns 160 to 204. Three transient banners
sharing one 140 px strip at the top of the frame. The objective moved to **-308**, which is 10 px
of daylight below where LATA DOWN ends, still in the top third and nowhere near the countdown.

### 24.3 It now retires itself after 7 seconds

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

### 24.4 Found while measuring this, NOT fixed: "YOU ARE VULNERABLE" is behind the deck too

`VulnerableWarning` is placed at y 84 and is 40 tall, so it draws from **84 to 124**. That is
inside the hero deck (14 to 92) by 8 px and inside the Classic deck (24 to 124) **completely**.
The one line that means "you are about to lose five seconds" is painted over by the Classic
deck's wooden plate for its whole life.

It is left alone here because it was not reported and because it is not obvious where it should
go instead: 24.1 has just filled 132 to 190 with the practice stack, and the two are never on
screen together, so it could take the same band. Done looks like the warning legible at both deck
heights, with the numbers written down the way 24.1's are.

---

## 25 · `Checks.RunAll` has been red since the Phaister merge, in two places

Found while verifying § 24 against `67f88aa`, which is the tip of `feat/ilalim-ng-tulay-map`.
**Neither of these is caused by anything in § 24 and neither is fixed there**, because a HUD
placement branch is the wrong place for a roster constant and a sound file. Both are two-line
jobs for whoever picks them up.

⚠⚠ **The point is not the two bugs, it is that the project's one-launch verification command
has been failing and the failure has been carried.** `RunAll` prints
`RESULT: FAILED. headless, audio cues.` at the end of every pass, so the next person to run it
learns nothing from a red result.

### 25.1 `HeadlessCheck` still counts five heroes

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

### 25.2 Phaister fires a cue that does not exist

`AudioCueCheck`: *"UNDECLARED: PhaisterHeroKit.cs fires 'sfx_ghost_appear', which is in no cue
list, so it plays silence."* 69 files on disk, 75 live cues declared, and this one reaches
neither.

⚠ **This is § 20 again, one hero later.** § 20 is "Cheska's kit played the wrong sounds, and
every zone died in silence". The sixth kit arrived with the same class of hole, which suggests
the check is doing its job and the merge checklist is not.

Done looks like: either the cue is declared and a file exists for it, or the call site is changed
to a cue that does. Whichever it is, `RunAll` comes back OK for audio.

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
 * the host's press arrives with a sender id of **0** and is resolved at the door, or the host can
   never satisfy a gate it is part of (and counts twice in a two-peer match);
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
