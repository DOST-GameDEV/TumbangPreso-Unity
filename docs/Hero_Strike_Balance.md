# Hero Strike: footprints, economy and the rework plan

**This file is the home of the per-ability footprint table.** `docs/VISION.md` § 2 sets the
readability budget and used to point at `docs/TODO.md` § 1 for the numbers; § 1 became peer
rematch voting and the numbers were never written anywhere. They are here now.

`docs/Design.md` § 13 says it does not govern Hero Strike and points at the kit files. This is
the layer between the two: the kit files hold the constants, this holds what they were measured
against and why.

Written 2026-08-25. § 1 and § 2 are measurements taken off the code at HEAD `2bff8536`.

✅ **§§ 3 and 4 WERE ARGUED AND THEN BUILT, THE SAME DAY.** 🧑 read the plan and said *"pls work
on this now"*, so everything below except where marked **NOT BUILT** is shipped. The numbers in
those sections are what is in the code; the reasoning is repeated in a ⚠️ note above each
constant, so this file and the code are two copies of the same decision and either can catch the
other drifting.

**What is still NOT BUILT and why:**

| Item | § | Why not |
|---|---|---|
| A choice between rounds | 4.7 | It adds asymmetry to a mode whose fairness argument rests on four identical seats, and it was flagged as the one idea that could make the mode worse. It needs 🧑's call, not mine |
| The cooldown bar's halfway tick | 4.6 | The deck already prints the seconds, which is the half that matters. Marginal, and a widget change wants a render pass of its own |
| The `BotBehaviourProbe` A/B on every new number | 3.1, 3.2 | Every cooldown, charge count and ultimate cost is a starting position with reasoning, not a measured result. 🧑 2026-08-25: *"u can test but dont test fairness yet"* |

---

## 0 · The arena, so every percentage below means something

`CONFINEMENT_RADIUS` is **7.0**, so the danger zone is **14 m by 14 m = 196 m²**. Every "% box"
in this file is `π r² / 196`.

`VISION.md` § 2 rule 1: a skill wants **1.8 to 2.5 m of radius**, which it states as 3 to 8 per
cent of the box. That arithmetic is worth writing out because it is quoted a lot: r 1.8 is
**5.19 %** and r 2.5 is **10.02 %**. The stated 3 to 8 per cent band is narrower than the stated
radius band. Where the two disagree in this file, **the radius wins**, because it is the number
that appears in the code.

---

## 1 · The footprint table

Measured off `HeroHazards.cs` and the five `*HeroKit.cs` files. **Gameplay radius** is what
`HazardVolume.Radius` or the distance check actually uses. **Visual radius** is the widest thing
drawn on the floor, which is not always the same number and in several places is larger.

Unity's `Cylinder` primitive is 1 m across at scale 1, so a `localScale.x` of `radius * 2.0`
draws a disc of world radius `radius`. Every visual figure below is already converted.

| # | Hero | Power | Slot | Gameplay r | Visual r | Area m² | % box | Verdict |
|---|---|---|---|---|---|---|---|---|
| 1 | Sean | Flame Rush | S1 | 1.6 per disc | 1.6 | 8.04 | **4.10 %** | in budget per disc, **over per cast**: see § 1.1 |
| 2 | Sean | Ignition Cannon | S2 | none | none | 0 | 0 % | correct. Its throw impact is row 3 |
| 3 | Sean | (ignited tsinelas impact) | - | 4.5 | ring to 6.30 | 63.62 | **32.46 %** | **over**, and it is a SKILL's payload |
| 4 | Sean | Supernova | ULT | 4.8 | sphere 4.80, ring 6.72 | 72.38 | **36.93 %** | big on purpose. Keep |
| 5 | Zack | Bolt Sprint | S1 | 1.8 per disc | 1.8 | 10.18 | **5.19 %** | in budget per disc, **worst in game per cast**: § 1.1 |
| 6 | Zack | Static Charge | S2 | none | none | 0 | 0 % | correct, no floor effect |
| 7 | Zack | Thunderstrike | ULT | 4.5 | ion core 4.05, ring 6.75 | 63.62 | **32.46 %** | big on purpose. Keep |
| 8 | Dante | Seismic Stomp | S1 | 2.4 blast, 3.2 slipper repel | 2.4, ring 3.36 | 18.10 | **9.23 %** | marginally over. Trim |
| 9 | Dante | Demonic Carapace | S2 | none (self) | 0.93 on the body | 0 | 0 % | **correct, and it is the model to copy** |
| 10 | Dante | Titan Fissure | ULT | 4.5 at 2.2 m out, plus a 5.5 m / 50 deg cone | 4.5 | 63.62 | **32.46 %** | big is fine. **Painting twice is not**: § 1.2 |
| 11 | Dante | (Titan Fissure pillars) | ULT | 4 x 1.4, out to 3.8 m | 0.70 each | 24.63 | **12.57 %** | second effect on the same cast |
| 12 | Cheska | Permafrost Sheet | S1 | 2.3 at 2.8 m out | 2.3 | 16.62 | **8.48 %** | radius fine, **render is the problem**: § 1.3 |
| 13 | Cheska | Ice Barricade | S2 | 1.6 at 2.2 m out | 2.35 across the face | 8.04 | **4.10 %** | in budget. The best-behaved power in the game |
| 14 | Cheska | Glacial Nova | ULT | 4.6 freeze, 4.8 slipper clear | 4.6 | 66.48 | **33.92 %** | big on purpose. Keep |
| 15 | Nemu | Ghost Step | S1 | none (self) | light r 5.0 | 0 | 0 % | correct |
| 16 | Nemu | Astral Projection | S2 | none (pet) | pet 0.40 | 0 | 0 % | correct |
| 17 | Nemu | Seance Void | ULT | 3.2 at 3.5 m out | 3.2 | 32.17 | **16.41 %** | the only ult that is modest. **Over the bot cap**: § 1.4 |

Also on the floor and not owned by any hero: **Ilalim ng Tulay's eight LRT pillar hazards**, and
the two street trip sites inside the chalk.

### 1.1 The trails are measured as discs and played as corridors, and that is the miss

⚠️⚠️ **`VISION.md` § 2 names Sean's Fire Trail and Zack's Shock Trail as the reference the whole
budget is set from: *"Sean's Fire Trail and Zack's Shock Trail already sit there and nobody has
ever complained about them. They are the reference, not the exception."* That sentence measures
ONE DISC. Neither ability places one disc.**

Both drop a disc on a timer for the whole cast, and each disc lives **3.0 s**, which is longer
than either cast. What is on the floor is the swept corridor, not a circle.

`Balance.Friction` is 30.0 and every impulse in this game resolves as `v² / (2 · Friction)`.

**Sean's Flame Rush.** Impulse 17.0, so the dash carries `17² / 60 = 4.82 m`. One disc at cast
plus one every 0.10 s for the 0.6 s duration. Swept area of a 1.6 m radius over 4.82 m:

    2 · 1.6 · 4.82 + π · 1.6²  =  15.42 + 8.04  =  23.46 m²  =  11.97 % of the box

**Zack's Bolt Sprint.** Impulse 12.0 gives `144 / 60 = 2.40 m` of dash, but the ability then
applies `forward · 4.0 · dt` for the whole **2.5 s** duration on top of normal running. A player
holding forward covers roughly 12 m in that window, which is most of the arena. One disc every
0.30 s gives about 9 discs, each living 3.0 s, so the entire corridor is live at once:

    2 · 1.8 · 12.0 + π · 1.8²  =  43.20 + 10.18  =  53.38 m²  =  27.24 % of the box

**Zack's Bolt Sprint paints more floor than any ultimate in the game, off a 6.0 s cooldown.**
That is the single largest readability fault found, and it is invisible in a per-disc table,
which is why it has survived every previous pass.

### 1.2 Titan Fissure paints the floor twice on one cast

`VISION.md` § 2 rule 2: *"An ultimate may be big. One at a time. A single cast should not paint
the floor twice."* Titan Fissure places a 4.5 m explosion (32.46 %) **and** four 1.4 m earth
pillars out to 3.8 m (12.57 %) **and** resolves a separate 5.5 m / 50 degree launch cone that
matches neither. Three geometries, one keypress.

### 1.3 Permafrost Sheet's radius is fine and its render is four effects

At 2.3 m it sits inside the budget. What it draws is an outer ghost disc at r 2.3, an inner ghost
disc at r 1.495, **four translucent crossbars 3.68 m long**, a point light at range 3.68, and a
frost mote emitter. Five overlapping translucent primitives is `VISION.md` § 2 rule 4 being
broken by a single ability against itself, before a second player casts anything.

### 1.4 Seance Void is the only live hazard over the bot cap

`AiTuning.HazardAvoidMaxRadius` is **3.0**. Every registered hazard is under it except Seance
Void at **3.2**, which is therefore the one thing in the game the bots are told to walk through
rather than around.

### 1.5 The overlap check, `VISION.md` § 2 rule 4

The worst credible frame, all four seats acting inside one second, using only floor area:

| Cast | % box |
|---|---|
| Zack, Bolt Sprint corridor | 27.24 |
| Sean, Supernova | 36.93 |
| Cheska, Permafrost Sheet | 8.48 |
| Dante, Seismic Stomp | 9.23 |
| **Total painted** | **81.88 %** |

Before Dante's pillars, before the eight LRT pillar hazards, before four tsinelas, four
nameplates, the lata and the chalk. **Rule 5 cannot pass against that frame**, and no amount of
better rendering fixes 82 per cent coverage. This is "puddles everywhere", with a number on it.

---

## 2 · The cooldown and charge economy today, and why it feels like 20 abilities at once

| Ability | Cooldown | Casts per 90 s round |
|---|---|---|
| Sean Flame Rush | 6.5 | 13.8 |
| Sean Ignition Cannon | 8.0 | 11.2 |
| Zack Bolt Sprint | 6.0 | 15.0 |
| Zack Static Charge | 8.0 | 11.2 |
| Dante Seismic Stomp | 6.5 | 13.8 |
| Dante Demonic Carapace | 9.0 | 10.0 |
| Cheska Permafrost Sheet | 7.0 | 12.8 |
| Cheska Ice Barricade | 9.0 | 10.0 |
| Nemu Ghost Step | 8.0 | 11.2 |
| Nemu Astral Projection | 9.0 | 10.0 |

**Four seats, two skills each, is 44 to 56 casts per round**, or roughly one every 1.8 seconds
for the whole 90 seconds. Nothing is a decision at that rate. 🧑 2026-08-25: *"game feels awkward
when theres 20 abilities at once"*. The count is worse than 20.

### 2.1 The ultimate meter is a timer wearing an economy's clothes

`Balance.UltimatePassiveChargePerSecond` is **1.0** against `HeroKit.UltimateMax` **100**.

⚠️⚠️ **A player who does nothing at all reaches 90 of 100 in a 90 s round.** Objective play is
worth 25 for knocking the lata over and 20 for a tag, so a good round adds one ultimate on top of
one that time was going to hand over anyway. The meter is a 100 second clock with a small bonus.

That is also a direct conflict with `VISION.md` § 4, which lists **"Nothing may reward waiting"**
as a competitive requirement and names the ultimate charge specifically. The passive drip is
waiting, paid.

`UltimateMax` is additionally a `const` shared by all five heroes, so a Thunderstrike that stuns
everyone within 4.5 m on demand costs exactly what a Seance Void costs.

---

## 3 · PROPOSAL: what to change, in the order it is worth doing

Every item below is a proposal. Numbers are starting positions with the reasoning attached, not
measured results.

### 3.0 First, the map is black, and it is arithmetic rather than taste

⚠️⚠️ **This is not a Hero Strike item and it outranks every Hero Strike item, because none of the
rest can be judged through it.** 🧑, on the current build: *"New map is just black wtf, i cant
see shit properly"*.

`MapGrade` carries a per-map tonemap exposure that `ColourGrade.shader` applies to the whole
frame. The three maps in the build:

| Map | Exposure | White | Contrast |
|---|---|---|---|
| Eskinita | **0.92** | 1.9 | 1.03 |
| Bayan Plaza | **0** (tonemap off) | 1.2 | 1.07 |
| **Ilalim ng Tulay** | **0.15** | 1.85 | 1.12 |

`TscnImporter.cs:871` reads `tonemap_mode > 0 ? SubProp(env, "tonemap_exposure", 1.0f) : 0.0f`,
so an imported map's exposure is either 0 or a Godot value that defaults to 1.0. **0.15 is not a
value the importer can produce.** It was typed by hand into `IlalimNgTulayBuilder.cs:192`, which
is the one map built from code rather than imported.

Run the shader's own arithmetic on a mid-grey linear 0.5, `_White` 1.85 so the divisor is
`1.85 / 1.9 = 0.9737`:

    x       = 0.5 · 0.15 · 0.6 / 0.9737 = 0.0462
    mapped  = x(2.51x + 0.03) / (x(2.43x + 0.59) + 0.14) = 0.0391

The same pixel on Eskinita at exposure 0.92 comes out at **0.4088**. Ilalim renders a mid-grey
**10.5 times darker than Eskinita**.

Then the frame's own contrast of **1.12** finishes it. The BCS step is
`lerp(0.5, c, contrast)`, which drives anything below 0.5 further down, and it reaches zero at

    c = 0.5 − 0.5 / 1.12 = 0.05357

Working that back through brightness 1.05 and the tonemap gives an input threshold of **0.5922**.

⚠️⚠️ **Every linear pixel below 0.59 on Ilalim ng Tulay clips to pure black before it reaches the
screen.** The arena is under a solid viaduct with the sun shadowed out, so essentially the whole
street is below 0.59. That is the screenshot exactly: black, with only the emissive HUD, the sign
lights and the road paint surviving.

**Fix:** set the exposure to Eskinita's **0.92** and re-render before touching anything else.
`IlalimNgTulayBuilder.cs:192` becomes `grade.Set(1.05f, 1.12f, 1.15f, 0.92f, 1.85f)`, then the
scene is rebuilt through `IlalimNgTulayPipeline` so the serialized value in
`IlalimNgTulay.unity` follows. The contrast of 1.12 is worth a second look at 0.92 and may want
to come back to Eskinita's 1.03, but that is a judgement to make against a render rather than
against arithmetic.

**Guard so it cannot happen again:** an EditMode assertion that every shipped map's `MapGrade`
exposure is either exactly 0 or inside 0.6 to 1.2. One test, no Unity launch of its own, and it
catches the whole class.

#### 3.0.1 The contrast, resolved against the render it was waiting for

The section above shipped the exposure at 0.92 and deliberately left the contrast alone, saying
the 1.12 was *"worth a second look at 0.92"* and was a judgement to make against a render.

🧑, off the build carrying the 0.92 exposure: *"less dark as before but still dark."* That is
the render, so the fallback named above is taken: **contrast 1.12 becomes Eskinita's 1.03.**

Why fixing the exposure improved it without finishing it: exposure and contrast crush the frame
independently, and only the exposure was corrected. Running the same arithmetic forward at the
shipped 0.92, with brightness 1.05, `lerp(0.5, c, contrast)` reaching zero at
`c = 0.5 - 0.5/contrast`, and inverting the ACES fit at `_White` 1.85:

| Contrast | Clips at (post-BCS) | Tonemapped | **Linear input** |
|---|---|---|---|
| 1.12 (as shipped) | 0.05357 | 0.05102 | **0.0966** |
| 1.03 (Eskinita) | 0.01456 | 0.01387 | **0.0422** |

So the black floor moved 0.5922 → 0.0966 when the exposure was fixed, which is why the map went
from unplayable to merely dark, and 1.03 moves it again to **0.0422**: **2.3x less of the range
crushed to pure black.** Under a solid viaduct with the sun shadowed out, the shaded pavement and
the shopfronts in the deck's shadow sit exactly in the 0.04 to 0.10 band that 1.12 was still
eating.

⚠️ Eskinita has run 1.03 for the whole port and is the map nobody has ever called dark, so this
matches a known-good frame rather than inventing a value. Saturation stays 1.15: the complaint is
about value, not colour.

⚠️⚠️ **`MapGradeSanityTests` asserts the builder's `grade.Set` call against the value baked into
`IlalimNgTulay.unity`, so changing the builder alone FAILS the test.** The scene has to be rebuilt
through `IlalimNgTulayPipeline` in the same change.

### 3.1 The economy: charges, long cooldowns, and an ultimate that has to be earned

🧑 2026-08-25: *"maybe make it like valorant wherein they have charges for their skill that they
can use once per round"*, and *"for some skills they can have a cooldown instead of charges that
reset each round, make it long tho like 30 seconds to 45 seconds"*.

**The rule I want to argue for, so the split is not case by case:** an ability that **leaves an
object on the floor** takes **charges**. An ability that **moves or protects your own body**
takes a **long cooldown**. A placed object is a decision the whole court then plays around, so
scarcity is what makes placing it interesting. A dash or an armour is a reaction, and a reaction
you have permanently spent is a character who stops being able to play. Valorant splits the same
way: Sage's wall and slows are charges, Jett's dash recharges off play.

#### Charges. Refreshed to full at the start of every round.

| Hero | Power | Charges | How it comes back mid-round |
|---|---|---|---|
| Cheska | Permafrost Sheet | 2 | not at all |
| Cheska | Ice Barricade | 1 | **+1 when you retrieve your own tsinelas** |
| Dante | Seismic Stomp | 2 | not at all |
| Sean | Ignition Cannon | 2 | **+1 when you knock the lata over** |
| Zack | Static Charge | 2 | **+1 when you knock the lata over** |
| Nemu | Astral Projection | 2 | not at all |

**Why those recharge triggers and not a timer.** `VISION.md` § 0: *"The tension is the retrieval,
not the throw."* Retrieving your own tsinelas is the only act in this game that costs you
something, and today it earns nothing at all. Making it the recharge trigger pays the exact
behaviour the game is built around. Knocking the lata over is the objective, and paying the
throw skills off it closes a loop: charge the throw, land it, get the charge back.

⚠️ **Not every skill recharges, on purpose.** A kit where everything comes back is a kit with
cooldowns and extra steps. Cheska's sheet and Dante's stomp are meant to run out.

#### Long cooldowns. Reset to ready at the start of every round.

| Hero | Power | Cooldown | Why this length |
|---|---|---|---|
| Zack | Bolt Sprint | **30 s** | 3 casts a round. It is escape and chase, the thing Zack is for, so it is the shortest of the four |
| Sean | Flame Rush | **34 s** | 2.6 casts. It is a dash that also knocks down, so it is worth more than Zack's |
| Nemu | Ghost Step | **36 s** | 2.5 casts. Tag immunity is the strongest defensive verb in the game |
| Dante | Demonic Carapace | **45 s** | 2 casts. Immunity to stun, shove and slip for 4 s is a free retrieval, and a free retrieval is a point |

**Why cooldowns rather than charges for these four.** All four are the ability that gets you out
of trouble. A player holding their last charge of an escape does not escape, they hoard, and the
round goes quiet. That is the failure mode `VISION.md` § 4 calls out as forbidden.

**What this does to the count.** Casts per round across four seats falls from **44 to 56** to
roughly **14 to 18**, plus at most one ultimate. Roughly one ability every 5 to 6 seconds instead
of every 1.8. Each one is now worth watching, which is the whole point.

#### Ultimates: points, per hero, and no passive drip

Delete `UltimatePassiveChargePerSecond` outright. Retune the earn table and make the cost a
per-kit value rather than a shared `const`:

| Event | Now | Proposed | Why |
|---|---|---|---|
| Knock the lata over | 25 | **25** | the objective. Unchanged |
| Tag an attacker as taya | 20 | **20** | the taya's only way to earn. Unchanged |
| **Retrieve your own tsinelas** | 0 | **12** | the act the game is about, currently worth nothing |
| Legal throw released | 8 | **4** | `VISION.md` § 0: throwing is safe and free. Paying 8 for the safe act is backwards |
| Time passing | 1.0/s | **0** | it is waiting, and § 4 forbids paying for waiting |

| Hero | Ultimate | Cost | In lata knocks | Why this cost |
|---|---|---|---|---|
| Zack | Thunderstrike | **150** | 6.0 | 4.5 m stun at your own feet, no aim, no counterplay. The most reliable ult in the game |
| Cheska | Glacial Nova | **140** | 5.6 | freezes everyone near you AND clears every loose tsinelas. It is an escape and a reset in one |
| Sean | Supernova | **130** | 5.2 | knocks the lata over itself, so it is an ultimate that pays a point directly |
| Dante | Titan Fissure | **110** | 4.4 | needs facing and a 50 degree cone. Whiffs completely if they scatter |
| Nemu | Seance Void | **90** | 3.6 | a zone that drags and slows. No burst, no knockdown, the least round-ending of the five |

A strong round is roughly 3 knocks, 4 retrievals and 6 throws, which is `75 + 48 + 24 = 147`
points. So Nemu ultimates most rounds, Zack ultimates when the round went well, and a player
having a bad round gets none. That is the shape Valorant has, and it is what makes an ultimate an
event.

⚠️ **The HUD needs a third widget and `VISION.md` § 3 already constrains two of them.** A
cooldown drains a smooth bar, the ultimate fills a notched one, and neither may be reused for
charges. **Charges are pips**, drawn as discrete filled dots on the tile. Three states, three
shapes, no text, so § 3's "the in-match HUD carries no sentences" holds.

### 3.2 Footprint changes, and what detail replaces the area

`VISION.md` § 2 rule 3: a smaller flat plane is still a puddle. Every shrink below says what is
drawn instead.

| Power | Now | Proposed | What replaces the area |
|---|---|---|---|
| Zack Bolt Sprint trail | r 1.8, every 0.30 s, unbounded corridor | r **1.0**, **cap 6 live discs per caster** | the discs stop being discs. Draw a live arc between consecutive drops so it reads as one cable on the ground, plus a short vertical spark column at each anchor. The cap is what bounds the corridor: it can never exceed about 6 m regardless of how far he runs, so the trail reads as "just behind him", which is what a speed trail should mean |
| Sean Flame Rush trail | r 1.6, every 0.10 s | r **1.0**, every 0.15 s, **cap 6** | a scorch mark with a bright licking rim and embers rising off it, rather than a flat orange disc. The corridor narrows to 2.0 m, which is one body plus margin, so you have to actually step on it |
| Sean ignited tsinelas impact | 4.5 | **2.6** | it is a skill's payload, not an ultimate, and 32 % of the box for a skill is indefensible. Replace the reach with a hard vertical: a tall thin flame column at the impact point and a fast bright ring, so it reads as a hit rather than as an area |
| Dante Seismic Stomp | 2.4 blast, 3.2 repel | **2.2** blast, **2.6** repel | a raised cracked lip at the rim with rock chunks standing proud of the floor, instead of a flat lava plane. The stomp gains depth and loses 1.4 % of the box |
| Cheska Permafrost Sheet | 2.3, five translucent layers | **2.3 unchanged**, two layers | the radius is correct and the render is the fault. Delete the four crossbars and the inner disc. Draw one frosted disc, a hard crystalline rim, and a small cluster of ice spikes at the centre with real height. Fewer primitives, more silhouette |
| Nemu Seance Void | 3.2 | **2.8** | brings it under `AiTuning.HazardAvoidMaxRadius` 3.0, which is the last live hazard over the cap. Replace the reach with the funnel: a visible cone of pulled debris and a deeper core, so the danger reads vertically |
| Dante Titan Fissure pillars | 4 pillars, arc, out to 3.8 m | **2 pillars, on the fissure line** | stops the ultimate painting the floor twice. Two pillars flanking the crack read as the crack's edge; four in an arc read as a second ability |
| Cheska Ice Barricade | 1.6, duration 3.2 s | 1.6, duration **6.0 s** | closes `TODO.md` § 2 by construction. At one charge per round the wall has to be worth the charge, and a wall that stands 3.2 s is not |

**Ultimates that stay big and why.** Supernova 36.93 %, Glacial Nova 33.92 %, Thunderstrike
32.46 % and Titan Fissure 32.46 % all stay. Rule 2 allows an ultimate to be big **one at a
time**, and the charge economy in § 3.1 is what makes "one at a time" true: at 90 to 150 points
with no passive drip, two ultimates inside the same second stop being a normal occurrence.

**The worst frame after all of the above:**

| Cast | % box now | % box proposed |
|---|---|---|
| Zack Bolt Sprint corridor | 27.24 | 8.13 |
| Sean Supernova | 36.93 | 36.93 |
| Cheska Permafrost Sheet | 8.48 | 8.48 |
| Dante Seismic Stomp | 9.23 | 7.76 |
| **Total** | **81.88 %** | **61.30 %** |

Still high, and it is dominated by one ultimate. Without an ultimate in frame it falls to
**24.37 %**, which is a frame that can show the lata, the chalk and four players. **That is the
rule 5 test and it has to be taken as a render, not asserted here.**

### 3.3 Where `AiTuning.HazardAvoidMaxRadius` stops mattering

The constant is **3.0** and its own note says: *"WHEN THE ABILITY FOOTPRINTS COME DOWN, every
hazard falls under this cap and avoidance starts applying to all of them with no further change
here. That is the intended end state."*

After § 3.2 the registered hazards are: Permafrost Sheet 2.3, Ice Barricade 1.6, Seance Void
**2.8**, earth pillars 1.4, and Ilalim's LRT pillars. **Every one is under 3.0**, so the cap
binds nothing and the bots path around all of them.

⚠️ **Do not delete the constant.** It stops being a limit and becomes a guard, and the guard is
what stops the next ability re-breaking the bots the way registering the trails once did (59
throws and 122 skill uses down to 11 throws, 3 skill uses and 661 idle penalties). Change the
note to say it is now a ceiling nothing reaches, and add an EditMode test asserting no shipped
ability registers a hazard above it. `VISION.md` § 2 is right that the bots are the canary; this
makes the canary automatic.

---

## 4 · PROPOSAL: things that are not about size at all

🧑 has not played the current build, so "smaller or better rendered" is the opening bid rather
than the brief. These are ranked by fun per unit of work.

### 4.1 Hitstop on the victim, never on the caster

A hit today is `DizzyStars` plus a `bump` sound. There is no impact frame. **A 70 ms freeze on
the victim's own view at the moment of a knockdown** is the cheapest large upgrade to how a hit
feels, and it is what every fighting game and Valorant's headshot both do.

⚠️ **Victim only.** Freezing the caster's frame would leak information about a hit the caster
should have to read off the world, and in a four-player game it would stutter three screens for
one event.

### 4.2 The telegraph should be louder for the person about to be hit than for the caster

Today the ground ring exists so the caster can aim. The person standing in it gets the same ring
or less, seen edge-on from ground level in first person, which is the worst possible angle for
reading a disc.

**When a live telegraph contains you and you did not cast it:** a second bright rim on the ground
ring and a short pulse at the edge of your screen in the caster's hero colour. Costs one boolean
comparison. This is the change most likely to turn "I got hit by something I never saw" into
"I got hit by something I saw and was too slow for", which is the difference between a game
feeling unfair and feeling hard.

### 4.3 Give the ultimate a wind-up so it has a payoff moment

Every ultimate currently resolves in the frame it is cast. There is no moment.

**0.4 s of wind-up**: the caster roots, a column of light in their hero colour rises off them,
and the existing `ComicPopup` promotes to a global banner. Then it goes off. The other three
players get a beat to react, which is what makes an ultimate an event rather than a large skill,
and it is what makes counterplay possible at all. `VISION.md` § 1.1 says combos, timing and
counterplay are the reason Hero Strike exists.

⚠️ **The wind-up is also the balance lever.** An ultimate you can be interrupted out of is worth
less than one you cannot, and that is a much better knob than the radius.

#### 4.3.1 ✅ BUILT 2026-08-25, and the refusal it had to get past

This was proposed, **refused**, and then built, and all three steps matter to anyone reading it.

The refusal lived in `HeroAbilitySystem.PlayUltimatePresentation` and was correct on its own
terms: *"A real 0.4 s wind-up on the ABILITY would be a fourth thing that can be interrupted, and
every one of the five ultimates would need its own answer to being stunned halfway through; the
buffering rules in `docs/Hero_Strike_UI.md` § 7 have no story for a half-cast power."* What
shipped instead was the column, the camera punch and the hitstop, all fired **as** the ultimate
resolved.

⚠️⚠️ **The objection is entirely about INTERRUPTION, not about the delay, so the wind-up is
uninterruptible and the objection dissolves.** Once the press lands the ultimate fires: no stun,
trip or death cancels it. There is therefore no half-cast state for any of the five to answer
for, and § 7's buffering never meets one. The meter is still spent at the press, so there is no
refund path to get wrong either.

**What it costs the caster** is a 0.4 s root, entered through `Stamina.SpeedZones` at 0.0. That is
the price the beat is paid for with, and it is real in a 14 m box.

**What it buys** is the thing this section asked for: the other three players get 0.4 s to run,
reposition or spend something defensive. The column now rises at the press instead of at the
resolve, so it is a telegraph rather than an announcement, which is the role § 4.3 described.

⚠️ **Interruptibility is still NOT taken**, and taking it later means first writing the half-cast
story `Hero_Strike_UI.md` § 7 is missing. Do not add a stun check to `HeroAbility.Tick` without it.

⚠️ **Ultimates only.** 🧑, authorising it: *"just dont spam it for everything"*. `UltimateWindup`
is one shared constant rather than five tuned numbers, because what a player reads is "somebody is
ultimate-ing" and that must look and last the same whoever cast it.

### 4.4 Sean and Zack are currently the same kit in two colours

This is the kit-identity answer and I think it matters more than any single radius.

| | Sean | Zack |
|---|---|---|
| Skill 1 | forward dash leaving a damaging trail | forward dash leaving a damaging trail |
| Skill 2 | your next throw explodes where it lands | your next throw explodes where it lands |
| Ultimate | ground-centred blast at your own feet | ground-centred blast at your own feet |

Three slots, three matches. Fire and lightning are a palette, not a design.

**Proposed split, using each hero's own description as the brief:**

- **Zack's trail becomes defensive.** His description already says it *"shocks anyone chasing
  you"*, and the mechanic does not do that: it drops discs on his **current** position, which is
  in front of a chaser rather than behind him. Drop at his position **0.5 s ago** instead. Same
  code, opposite meaning, and it makes the ability read as running away rather than running
  through.
- **Zack's Static Charge loses its floor effect entirely.** Its description already says the
  throw *"flies much faster"*. Make it only that: speed, flatter arc, harder to read, no zone on
  impact. Sean keeps the explosion. That deletes a duplicated 32 % floor effect and gives Zack a
  single clear identity, which is **speed**: fastest dash, fastest throw, no ground control.
- **Sean stays the aggressive one:** the dash you commit forward with, the throw that punishes a
  near miss, the ult that ends the round.

### 4.5 The overclock window becomes worthless under long cooldowns, and should change with them

`TODO.md` § 5 asks whether `OverheadPassWindow.OverclockRate` 2.0 is right. The economy in § 3.1
answers part of it for free and makes the rest worse.

Double cooldown rate for 2.70 s saves **2.70 s** of cooldown, whatever the cooldown is. Against a
6.5 s skill that is **41 %** of a cycle. Against the proposed 34 s cooldown it is **7.9 %**.
**The mechanic loses four fifths of its value the moment cooldowns get long**, and it stops being
worth learning.

**Proposal:** stop scaling the rate and pay a flat amount instead. Standing on the pad while the
LRT passes gives **a flat 10 s off your longest cooldown**, or **+1 charge** if you are holding a
charge skill below full. That turns a passive multiplier into a contested spot on the map that
players fight over, which is what an Ultimate Orb is in Valorant and what makes map control mean
something. It also survives any later cooldown retune, which the multiplier does not.

### 4.6 Cooldown legibility at 30 to 45 seconds, which is smaller than it first looks

I was going to propose putting the seconds inside the tile. **It is already there.**
`Hero_Strike_UI.md` § 4 ships a three-state deck where Cooling draws the seconds in amber in the
centre alongside the draining meter, and its note says the two are kept deliberately because they
are read at different distances: the meter peripherally, the number on a glance.

So only half the widget breaks at 40 s. **The number keeps working and the meter stops**, because
a bar crossing 2.5 per cent a second reads as not moving. Two options, and I prefer the first:

- **Mark the meter instead of lengthening it.** Put a tick at the halfway point so the bar has a
  landmark to be measured against. Costs one line, changes no state logic.
- Let the meter run non-linearly, fast at the start and slow at the end. **I would not**: it
  makes the bar lie about time remaining, and `Hero_Strike_UI.md` § 8 is titled "Telegraphs tell
  the truth" for a reason.

⚠️ **The real deck work is the fourth state, not the third.** § 3.1 adds charges, and
`Hero_Strike_UI.md` § 4's table has exactly three rows. A charge tile is neither Ready nor
Cooling: it can be ready with 1 of 2 left, which no current row can express.

### 4.7 A choice between rounds (the most arguable thing in this document)

The buffer period already exists and already runs a practice range. Valorant's identity lives as
much in the buy phase as in the round.

**At the buffer, each player picks one of three small street modifiers for the coming round:** an
extra charge on one skill, 8 s off one cooldown, or faster retrieval. Three cards, one pick, no
currency to track.

⚠️ **I am putting this last on purpose.** It adds a screen, it adds asymmetry to a mode whose
fairness argument rests on four identical seats (`VISION.md` § 4, aimed at a bracket), and it is
the one item here that could make the mode worse. It is in because it is the highest-ceiling
idea, not because I am confident about it.

---

## 5 · Renders, and what this plan is NOT allowed to claim without one

`CLAUDE.md` § 6.1: show, do not describe, and every iteration gets a new versioned filename.

Nothing in §§ 3 and 4 has been rendered. When it is built, these are the frames that decide it,
through `IlalimNgTulayShowcaseProbe` and the play-capture scripts in `tools/`:

1. `ilalim_grade_v23.png` and `ilalim_thrower_view_v23.png`. The § 3.0 exposure fix alone,
   before any ability work, because everything else is judged through it.
2. `herostrike_worstframe_v1.png`. Four seats, all abilities forced live in the same second,
   taken from the thrower's eye. **This is the `VISION.md` § 2 rule 5 test**: the lata, the chalk
   and all four players must be identifiable. Take it BEFORE the changes as well, as the
   before-and-after.
3. `herostrike_trails_v1.png`. Sean and Zack dashing across the same frame, which is where the
   corridor problem in § 1.1 actually shows.
4. `herostrike_deck_v1.png`. The bottom deck carrying a bar, a pip row and a notched meter at
   once, which is the § 3.1 three-widget question.

---

## 6 · What is measured here and what is not

**Measured off the code, no Unity launch required, safe to argue from:** every radius and area in
§ 1, the dash distances from `Balance.Friction`, the cast counts in § 2, the passive-charge
arithmetic in § 2.1, and the whole of § 3.0 including the 0.59 black threshold.

**Not measured, and stated as a proposal:** every number in §§ 3.1, 3.2 and 4. They are starting
positions with reasoning attached. `BotBehaviourProbe` is what settles them, and per 🧑 on
2026-08-25 the suite is not to be run out of habit, so they get measured once when there is code
to measure rather than at each step.

---

## 7 · As built, 2026-08-25

What actually shipped, so this file can be read as a record rather than only as an argument.
Every number below is also written next to the constant it governs, in a ⚠️ note with its
reasoning, which is the repo's rule from `CLAUDE.md` § 3.

### 7.1 The economy, as shipped

| Hero | Skill 1 | Skill 2 | Ultimate cost |
|---|---|---|---|
| Sean | Flame Rush, **34 s cooldown** | Ignition Cannon, **2 charges**, +1 per lata knockdown | **130** |
| Zack | Bolt Sprint, **30 s cooldown** | Static Charge, **2 charges**, +1 per lata knockdown | **150** |
| Dante | Seismic Stomp, **2 charges**, no recharge | Demonic Carapace, **45 s cooldown** | **110** |
| Cheska | Permafrost Sheet, **2 charges**, no recharge | Ice Barricade, **1 charge**, +1 per own retrieval | **140** |
| Nemu | Ghost Step, **36 s cooldown** | Astral Projection, **2 charges**, no recharge | **90** |

Ultimate points: lata knockdown **25**, tag **20**, own tsinelas retrieved **12** (new), legal
throw **4** (down from 8), time passing **0** (deleted).

Casts per 90 s round across four seats fell from **44 to 56** to roughly **14 to 18**.

⚠️⚠️ **THE TWO KINDS OF "CHARGE" GO OPPOSITE WAYS AT A ROUND BOUNDARY, AND THE WORD IS
OVERLOADED SINCE THIS REWORK.** 🧑 2026-08-25: *"i want ult charges to stay in between rounds if
u havent yet. Only ult tho"*.

| | Round boundary | Match boundary |
|---|---|---|
| **Ultimate meter** (`HeroKit.UltimateCharge`) | **persists** | cleared |
| **Skill charges** (`HeroAbility.ChargesRemaining`) | **refill to full** | refill to full |
| Cooldowns, live durations | cleared | cleared |

The meter is a resource earned across a whole match; skill charges are a per-round allowance.
Both live one line apart in `HeroKit.ResetForRound`, so a change aimed at either can silently
take the other, and `UltimateChargePersistsButSkillChargesRefill` asserts them together.
⚠️ `HeroKit.Reset()` zeroes the meter and is **not** the round boundary despite sitting directly
above it; it has no runtime callers.

### 7.2 The footprints, as shipped

| Power | Was | Now |
|---|---|---|
| Sean Flame Rush trail | 1.6 per disc, uncapped corridor, 11.97 % | **1.0, capped at 6 live discs** |
| Zack Bolt Sprint trail | 1.8 per disc, uncapped corridor, 27.24 % | **1.0, capped at 6, laid 0.6 s behind him** |
| Sean ignited tsinelas | 4.5, 32.46 % | **2.6** |
| Zack static tsinelas | 5.5 stagger with no telegraph at all, 48 % | **2.0** |
| Dante Seismic Stomp | 2.4 blast, 3.2 slipper repel | **2.2 blast, 2.6 repel** |
| Dante Titan Fissure pillars | 4 in an arc to 3.8 m | **2 on the fissure line** |
| Nemu Seance Void | 3.2, over the bot cap | **2.8, under it** |
| Cheska Ice Barricade | 3.2 s duration | **6.0 s** |
| Cheska Permafrost Sheet | 2.3, five translucent layers | **2.3, two layers plus rim and spikes** |

Ultimates kept their size, per rule 2. `AiTuning.HazardAvoidMaxRadius` now binds nothing and is
a guard rather than a limit; `EveryRegisteredHazardStaysUnderTheBotAvoidanceCap` asserts it.

### 7.3 ⚠️⚠️ What the renders found, which is more than the arithmetic did

**This is the part worth reading if you read nothing else in § 7.** The footprint work in § 3.2
was derived on paper and it was correct. It was also not the biggest problem. Four capture passes
through `AbilityShowcaseProbe` found faults that no amount of measuring radii could have:

1. **Everything was clipping to pure white.** The frost sheet's 2.3 m disc came back at
   255,255,255 across its whole area. The cause is a pair rather than either half: a ghost
   material at 0.65 alpha is fine and a 2.5-intensity point light 0.6 m above it is fine, and
   together they write far past 1.0 before `ColourGrade`'s ACES curve ever sees the frame. **Every
   hazard light in the game came down by roughly two thirds and moved up**, so it lights the
   street rather than its own effect.
2. **The Seance Void rendered as a cartoon eyeball.** A violet ring around a blown-white disc with
   a solid purple ball in it. It is dark-cored with a bright lip and inward-leaning shards now,
   because a thing that pulls you in has to look like it goes down.
3. **`SpawnCrackedLavaDecal` drew no cracks.** It was a flat yellow plate under a name that
   promised fissured ground. It is a dark crust with seven glowing seams now.
4. **The ember billboards rendered as literal yellow squares**, because a `Quad` with an
   untextured material is a rectangle and facing the camera does not change that. They are small
   voxel cubes now, which is also the right answer rather than a cheap one: this is a voxel game
   and a soft photographic flame is the thing that would look broken.
5. ⚠️ **And three of the four passes fixed the wrong thing.** The fire trail's char was specified
   dark and rendered orange. v1 blamed the colour, v2 blamed the light, v3 blamed both, and all
   three still rendered orange. The cause was **alpha**: the char is drawn on top of `edge`, which
   is a full bright DISC and not a ring, so any alpha under about 0.9 blends the char with the
   orange plate beneath it rather than with the road. `ability_fire_trail_v1.png` through `_v3.png`
   are kept on disk as the three wrong answers.

⚠️⚠️ **AND THE PROBE THAT SHOULD HAVE CAUGHT THE BLACK MAP WAS HARD-CODING THE GRADE.**
`IlalimNgTulayShowcaseProbe.Shot` built its camera with `ColourGrade.Set(..., 0.92f, ...)`, which
is **Eskinita's** exposure, while the map's own `MapGrade` carried 0.15. Every showcase render
from v15 to v22, eight sets of fifteen frames, was taken through a grade the game never used.
That is why four signed-off render passes missed a map that shipped black. It calls
`AdoptFromScene()` now, the same call `CameraRig` makes when a match starts, so a capture grades
the way the match grades or it is not evidence.

**Renders on disk:** `Logs/shots-ilalim/*_v23.png` (the map at the corrected exposure) and
`Logs/shots-abilities/*_v1.png` through `*_v4.png` (the ability passes, all four kept).

---

## 8 · The visual identity plan: why every skill looked the same, and the rule that fixes it

🧑, 2026-08-25, looking at the first ability capture: *"look at this shit all of them look like
circles lang"*, and then: *"thoroughly plan how to make the skills all look better bcz they all
look repetitive and look like circles"*.

⚠️⚠️ **HE IS RIGHT, AND THE DIAGNOSIS IS NOT "THEY NEED MORE POLISH". IT IS THAT THE GAME WAS
SPENDING ONE CHANNEL AND HAD FOUR.**

### 8.1 The actual cause

Every floor effect in Hero Strike was built the same way: `GameObject.CreatePrimitive(Cylinder)`
scaled flat. Fire, lightning, ice, magma and a tear in the world were **five different fictions
drawn as one primitive in five colours**.

That leaves **hue** doing all the work of telling them apart, and hue is the one channel this
game cannot spare, because it is already spent twice:

- `Art_Direction.md` § 1 reserves **orange for OFFENSE and blue for DEFENCE**, and those two
  track the role, which rotates every round. They are the only two colours a player has to READ
  rather than merely see.
- `UiTheme` then spends five more hues on hero identity, asserted 25 degrees clear of both role
  colours and 30 degrees clear of each other by `HeroPresentationTests`.

So by the time an effect gets to pick a colour, the palette is full and every remaining
difference is a few degrees of hue on the same shape. That is why they read as repetitive: **they
are repetitive.** It is one shape, seven times.

### 8.2 The four channels an effect actually has, and what each is for

This is the rule the rest of this section applies, and it is what should be checked before any
new ability ships.

| Channel | What it carries | Read at |
|---|---|---|
| **Silhouette** | WHICH ability this is | any distance, any angle, colour-blind |
| **Axis** | whether it is ground, air or body | any distance |
| **Motion** | whether it is live or spent | mid distance |
| **Hue** | WHOSE it is | close, and only if the palette has room |
| **Construction** | what the thing is MADE of | close and mid, and it decides all of the above |

⚠️⚠️ **THE FIFTH ROW WAS ADDED 2026-08-26 AND IT IS THE ONE THIS SECTION MISSED.** 🧑, after
playing the build that came out of this very plan: *"the problem i found out earlier that made all
powers look ugly was that the same logic and code was used to generate all of them"*. The four
channels above were all spent, and every effect was still assembled the same way: a polygon handed
to `VfxShapes.Fan`, dropped flat by `Lay`, painted by `VfxMaterial.Ghost`, with a few
`PrimitiveType.Cube`s on top and a point light over it. **Change the vertex radii and you have a
different outline; you do not have a different effect.** `docs/TODO.md` § 19 has the whole
account, the five builders that answer it, and the four defects the renders found on the way.

⚠️ **Silhouette is the channel that was completely unused and it is the strongest one.** It
survives being seen edge-on in first person, it costs nothing per frame because the shape is
baked once at spawn, and it works for a player who cannot separate the hues at all.

### 8.3 One silhouette per fiction, and none of them is a circle

`Visual.VfxShapes` builds these as meshes in the XZ plane at one unit of radius, so a caller
scales them exactly the way it scaled a cylinder and no footprint arithmetic in § 1 changes.

| Ability | Shape | Why that shape |
|---|---|---|
| Sean, Flame Rush trail | **Streak** | a dash leaves a smear, and a smear POINTS: it tells you which way he went, which a circle cannot |
| Zack, Bolt Sprint trail | **Star**, 7 uneven points | lightning arrives at a point and runs out along the ground. A discharge pattern, not a puddle |
| Cheska, Permafrost Sheet | **Crystal**, 6 hard sides | the only thing here that GREW rather than spread or broke, so it is the only shape that reads as ordered |
| Dante, Seismic Stomp | **Splat**, 11 ragged sides | stomped ground fractures along uneven lines |
| Nemu, Seance Void | **stays round, and hovers** | see § 8.4 |

⚠️ **THE SHAPES ARE ANGULAR ON PURPOSE.** This is a voxel game and the whole cast is built from
boxes. A smooth airbrushed decal would be the thing that looked broken beside it, which is
`VISION.md` § 6: *"his UI art is the design system."*

⚠️ **AND THEY ARE SEEDED OFF POSITION.** Two scorch marks in one trail differ from each other,
but any given mark is identical between captures, which is what makes the probe's renders
comparable version to version. `CLAUDE.md` § 7.1 records what an unseeded probe costs.

### 8.4 Axis: the void hovers, and it is the only one that does

🧑: *"make make blackhole float, its on the floor"*.

A vortex is genuinely radial, so a circle is the honest outline for it and changing that would be
a lie. Instead it changes **axis**: the whole assembly lifts to **1.35 m** and the street carries
on underneath it, with debris shards caught partway up the column so the hover reads as lifting
rather than as art floating by mistake.

⚠️⚠️ **Horizontal versus vertical is a bigger difference than any two outlines on the same
plane**, which is why this is the better answer for the one effect whose shape should not change.

⚠️ **The ground still says where the danger is.** `SeanceVoidComponent` resolves by flat distance
on the FLOOR, so lifting the art without leaving a mark would put the gameplay circle somewhere
the player cannot see it. A faint ring at the true radius stays on the road. A telegraph that
lies is worse than no telegraph.

### 8.5 What is still open, and it is the honest part of this section

✅ **ITEMS 1 AND 4 CLOSED 2026-08-26; SEE `docs/TODO.md` § 8 FOR HOW.** Item 1 turned out to have
been built with the volume pass and never ticked: every ultimate core is a generated `VfxShapes`
mesh (`Shockfront` for the quake, `NovaShell` for fire and frost, `Spire` plus a `Star` discharge
for Thunderstrike) and not one is a `PrimitiveType`. Item 4 needed the probe to be able to
photograph a transient at all, which is now `Visual.IVfxTimeline` plus
`HeroHazards.CreateExplosionVisual`, and **the first frame it could finally take found two
shipped defects**: every ground shockwave in the game was drawn at double its size, and
Thunderstrike blew 62.8 per cent of the frame to white. Both fixed, and rule 5 is a gate now.

Applied so far: silhouette for five effects and axis for one. **That is the cheap half.** What
has NOT been done, roughly in order of payoff:

1. **The ultimates are untouched.** Supernova, Thunderstrike and Glacial Nova are still expanding
   spheres and rings, which is four more circles, and they are the biggest things on screen.
   They want the same pass: a slam should be a shockwave with a FRONT, a lightning strike should
   be vertical, a nova should be radial but crystalline.
2. **Motion is doing nothing.** Nothing in this set animates except the shock arc and the void's
   spin. A spent effect and a live one look identical, so a player cannot tell whether a patch of
   ice is about to expire. That is a real gameplay read and it is free: fade the rim, not the
   whole thing.
3. **The explosion, the ring and the flash are still shared by every ability.** `CreateExplosion`
   draws the same sphere and shockwave for a 2.2 m stomp and a 4.8 m Supernova, so the two
   biggest moments in two different kits are the same picture at two sizes.
4. **Nothing has been seen in motion.** Every judgement in this section is off still frames.
   Timing, overlap in a real round and how a corridor reads while it is being laid are all
   unmeasured, and a still cannot answer any of them.

**`docs/TODO.md` § 8 carries this list.** ⚠️ Items 2 and 3 above are also closed (motion: the
auras thin as they expire; the shared explosion: `ExplosionStyle` gives each fiction its own
silhouette, colour, debris, flash, shake and cue). **What is genuinely still open is a human
judgement: do the five read as five different events IN MOTION, in a real round with four
seats.** A still frame cannot answer that, and this section never claimed it could.

### 8.6 When an ability is allowed to take the camera, and the answer for all fifteen

⚠️⚠️ **THIS SECTION EXISTS SO THAT "GIVE IT A TPP BEAT" NEVER BECOMES A PER-ABILITY TOGGLE.**
`CLAUDE.md` § 4 states the invariant in one line: *"A Person is always FPP, a Prop is always TPP,
derived from `is_person` and asserted."* Two exceptions are already shipped and both are
categorical rather than per-case: emotes swing to TPP and back, and the spectator is a fourth rig
entirely. Adding a third exception one ability at a time is how an invariant stops being one.

**The rule.** An ability takes the camera only when **the body the player is driving changes, or
the player stops driving it.** Not when the effect is large, not when it is the ultimate, not
when it looks good from outside.

⚠️ **The test that decides it:** *if the player looked down at their own hands, would the game
have already told them what changed?* The viewmodel is not a decoration. Every skill in the game
carries a `viewmodelAction`, so the arms already animate for the cast, and where the arms can say
it, FPP says it better: it is faster, it keeps the crosshair, and it does not cost the player
their aim in a fight.

| Ability | Body or control changes? | Camera |
|---|---|---|
| Sean, Flame Rush | no, he moves faster | FPP |
| Sean, Ignition Cannon | no, `ignition-cannon` arms | FPP |
| Sean, Supernova | no. Big is not the test | FPP |
| Zack, Bolt Sprint | no, he moves faster | FPP |
| Zack, Static Charge | no, a buff | FPP |
| Zack, Thunderstrike | no. The bolt is vertical and FPP sees it | FPP |
| Cheska, Permafrost Sheet | no, floor effect | FPP |
| Cheska, Ice Barricade | no, it places a prop | FPP |
| Cheska, Glacial Nova | no. Big is not the test | FPP |
| Dante, Seismic Stomp | no, floor effect | FPP |
| Dante, Demonic Carapace | silhouette only, and `carapace-guard` shows it on the arms | FPP |
| Dante, Titan Fissure | no, floor effect | FPP |
| Nemu, Ghost Step | silhouette only, and `ghost-step` shows it on the arms | FPP |
| **Nemu, Astral Projection** | ⚠️⚠️ **yes. The player drives a different body** | **TPP, already** |
| Nemu, Seance Void | no. § 8.4 changes the EFFECT's axis, not the camera's | FPP |
| **Falling** | ⚠️ **yes. The player stops driving anything** | **TPP** |

**So the rule produces two, and it produced them rather than being written to fit them.** That is
the point of writing it first. Carapace and Ghost Step are the two that look like candidates and
are not, and the reason they are not is the same reason: their arms already say it.

#### 8.6.1 Astral Projection was already TPP, and that was never the complaint

🧑: *"it doesnt feel like im in the pet's body."*

⚠️⚠️ **THE CAMERA WAS ALREADY ON THE PET AND HAD BEEN THE WHOLE TIME.**
`CameraRig.ApplyCompanionPossessionView` mounts 2.0 m behind Kuro at 0.35 m, hides the human
viewmodel arms, and `StepCompanionLook` steers the pet with the mouse. Reading the complaint as
"the camera does not move" and moving it again would have produced a second camera path doing
what the first already did.

**What is actually wrong is that the swap has no duration.** The rig calls
`SetPositionAndRotation` with the finished pose on the first frame of the possession and again on
the way out. A cut with no travel between two poses does not read as *becoming* something, it
reads as a glitch or as nothing: the player never sees themselves leave, so there is no moment to
attribute the new body to. ⚠️ This is the same fault as § 8.5 item 2 in a different channel:
**motion was carrying nothing.**
