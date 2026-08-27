# Vision: what this game is for

**Read this before `Port_Plan.md`, before `Design.md`, before the code.** Everything else in
this repository says HOW. This says WHY, and it is the thing that keeps getting re-derived
wrongly by whoever picks the project up next.

Written 2026-08-23, from the team's own words.

---

## 0 · The one-paragraph version

Tumbang Preso is a Filipino street game: one kid guards a tin can, the others throw their
slippers at it and then have to run in and get them back. This is that, for four players, as
a competitive video game. **The tension is the retrieval, not the throw.** Throwing is safe
and free; going back in for your tsinelas is the only moment you can be caught. Every rule in
`Design.md` exists to protect that sentence.

---

## 1 · There are TWO modes, and that is the product decision

⚠️⚠️ **THIS IS THE THING TO GET RIGHT. A session that treats one mode as the real game and the
other as a variant will make bad calls for weeks.** Both ship. Both are first class. They are
aimed at different people.

| | **CLASSIC** | **HERO STRIKE** |
|---|---|---|
| For | People who want the street game. Less happening on screen. | Competitive and esports play. |
| Roster | The twelve street characters | The five heroes |
| Powers | None, and that is the feature | Two skills and an ultimate each |
| Verbs | Move, sprint, jump, throw, grab, shove, lunge, punch | The same, plus the kit |
| Governed by | `docs/Design.md` | `Design.md` § 13 points at the files |

**The stated reason, in the team's words:** *"We want to make original game ceiling cap higher
and more fun for esports so im adding a second gamemode. i want to keep original simple
gamemode for ppl that prefer less shit happening, but we want to add a second more competitive
gammeode with a lot of stuff."*

### 1.1 What follows from that, and these are rules

- **CLASSIC IS NOT "HERO STRIKE WITH THE POWERS TURNED OFF".** It is the whole original game
  and it is somebody's preferred way to play. Do not add a power to it, do not add a HUD
  element that only makes sense with a kit, and do not let a Hero Strike balance change reach
  into `Balance.cs` values Classic shares without saying so out loud.
  *(Classic already has its own bottom-of-screen identity: Street Hype, which names skilled
  curves, banks, close calls and blocks without changing a single point. That is the pattern.
  Give Classic its own depth; do not give it powers.)*

- **HERO STRIKE IS WHERE THE CEILING GOES UP.** Combos, timing, counterplay, reading which
  ultimate is banked. If a change makes the game deeper for a player who has put fifty hours
  in, it belongs here.

- **THE ROUND RULES ARE SHARED; THE MATCH LENGTH IS NOT.** Four players, 90 s rounds, one taya
  rotating clockwise, cumulative score. Classic plays one complete rotation, **4 rounds**, so
  everybody defends once. Hero Strike plays two complete rotations, **8 rounds**, so every
  seat defends twice. The role schedule and scoring stay shared; Hero Strike gets the longer
  competitive set without changing Classic's shorter street-game format.

- **CLASSIC IS THE TOURNAMENT RULESET UNTIL SOMEONE SAYS OTHERWISE.** Hero Strike is the one
  being grown toward that. Neither statement is permission to neglect the other.

### 1.2 The trap this replaces

`docs/Port_Ledger.md` § 12 records that an entire ability layer was **deleted** in the
HARRYDAKS rewrite, on the grounds that *"there were so many skills and stuff earlier, it was
too complicated and far from tumbang preso"*. A new session reads that and concludes the hero
kits are a mistake being repeated.

**They are not the same thing.** The deleted layer was eight verbs bolted onto the ONE game.
Hero Strike is a **separate mode the player opts into**, and Classic still exists untouched
beside it. Deleting the old layer and adding this one are consistent decisions, twenty days
apart, by the same people.

---

## 2 · The readability budget, and why it is a hard number

An arena is `CONFINEMENT_RADIUS` **7.0**, so the danger zone is **14 m by 14 m = 196 m²**.
Four players, one lata, four tsinelas and up to twelve live abilities share that.

⚠️⚠️ **"MORE STUFF" IS THE POINT OF HERO STRIKE AND "UNREADABLE" IS ITS FAILURE MODE. These
pull against each other and the budget is how the argument gets settled.** From the team,
looking at a live match: *"it just looks like puddles everywhere, theyre all too big"*, *"the
game starts to get confusing when theres so much shit on the screen in such a small arena"*,
and the resolution: *"its okay for there to be big skills but not every single skill should be
big"*.

The working rules that came out of that:

1. **A skill's floor footprint should be about 1.8 to 2.5 m of radius**, which is 3 to 8 per
   cent of the box. Sean's Fire Trail and Zack's Shock Trail already sit there and nobody has
   ever complained about them. They are the reference, not the exception.
2. **An ultimate may be big. One at a time.** A single cast should not paint the floor twice.
3. **Spend the budget on DETAIL, not on AREA.** A flat coloured plane at 40 per cent of the
   arena reads as a puddle. The same silhouette at 2.2 m with a cracked edge, a rim, depth and
   particles reads as ice.

   ⚠️⚠️ **AND "DETAIL" MEANS HOW THE THING IS BUILT, NOT HOW MANY LAYERS ARE STACKED ON IT.**
   This rule was followed for a year by adding a second translucent plate under the first, a
   handful of `PrimitiveType.Cube`s on top, and a point light over the lot. That is what every
   effect in the game was made of, and 🧑 named it from play on 2026-08-26: *"the same logic and
   code was used to generate all of them"*. Stacking is not detail; it is more area in the same
   place, and two coplanar translucent plates also sort arbitrarily, which shipped one trail
   drawing a different colour per drop. **A slab with walls, a field of broken plates, a swept
   flame, a branching tube and a dished funnel are five things. Five polygons handed to one
   builder are one thing.** `docs/TODO.md` § 19 and `Hero_Strike_Balance.md` § 8.2.
4. **Cap what can overlap.** Two translucent floor planes plus a disc plus a wall plus four
   popup labels is not four effects, it is one unreadable frame.
5. **A screenshot taken mid-fight must still show the lata, the chalk and every player.** If
   it does not, the effect is too big however good it looks alone.

   ✅ **THIS RULE IS MEASURED AND GATED AS OF 2026-08-26, AND IT CAUGHT SOMETHING THE DAY IT
   WAS.** `AbilityShowcaseProbe` photographs the ability TRANSIENTS as well as the persistent
   zones, and fails a run in which one blows more than **12 per cent** of the frame to white
   (Rec. 601 luminance at or above 245/255; a saturated colour is a colour, white is an absence
   of picture). The bound is measured rather than picked: the empty street reads 3.0 per cent,
   the ability corridors 3.0, the deliberate worst-frame pile-up 4.1, and the loudest legitimate
   effect 8.3. Zack's Thunderstrike read **62.8**, with the road markings themselves gone. It is
   now 6.5. `docs/TODO.md` § 8 has the two defects that first frame found.

⚠️ **THIS IS NOT ONLY AN ART CONSTRAINT, IT IS A GAMEPLAY ONE.** `AiTuning.HazardAvoidMaxRadius`
exists because a bot cannot path around a disc that covers half the arena; it walks the
perimeter until the round ends. When the footprints come down, that cap stops mattering and
the avoidance starts working for every hazard with no further change. The bots are a canary
for whether a human can read the floor.

⚠️ **THE PER-ABILITY NUMBERS LIVE IN [`docs/Hero_Strike_Balance.md`](Hero_Strike_Balance.md) § 1.**
This line pointed at `docs/TODO.md` § 1 until 2026-08-25, and by then § 1 had become peer rematch
voting, so the most important page in this file pointed at nothing. Nothing held the table at all.

Two findings from writing it that change how this section should be read:

- **The trails are measured as discs and played as corridors.** Rule 1 above names Sean's Fire
  Trail and Zack's Shock Trail as the reference the budget is set from. That measures ONE DISC,
  and neither ability places one disc: both drop a disc on a timer for the whole cast, each
  living longer than the cast. Zack's Bolt Sprint corridor is **27.2 per cent of the box off a
  6.0 s cooldown**, which is more floor than any ultimate in the game.
- **The worst credible frame today paints 81.9 per cent of the box**, before props, tsinelas and
  nameplates. Rule 5 cannot pass against it, and no amount of better rendering fixes 82 per cent.

---

## 3 · A player must be able to understand a power by looking at it

⚠️ **From the team: *"i want ppl to be able to get what all skills do js by looking at them?
or reading them from char select"*, and, on how: *"games like valorant overwatch league etc
dont clog their screen with text, to see how abilities work they usually click a button and
then let go when they dont wanna see it anymore"*.**

That produced a three-layer answer, and all three layers must stay in step:

| Layer | Where | What it carries |
|---|---|---|
| **Learn** | Character select | Icon, name, what KIND of power it is, one sentence, cooldown |
| **Recall** | Hold the ability-info key in a match | The same, in full, sliding in and out |
| **Play** | The deck at the bottom of the screen | Icon, key, name, and whether it is up RIGHT NOW |

Design rules that fell out of it, each of which replaced something that failed:

- **The icon says what the power does to the WORLD, not what element it is made of.**
  `AbilityGlyph` is Zone, Wall, Dash, Shield, Burst, Projectile, Phase, Slam, Empower. A flame
  icon on a fire hero's three powers tells a player nothing about which of the three to press.
  Two heroes with completely different fiction share a glyph when they share a job.
- **The glyph lives on the ability, not in a lookup table**, so a new hero cannot ship with
  three blank tiles.
- **Key labels come from the live binding, never from a literal.** A screen that teaches the
  wrong key is worse than one that teaches none.
- **Cooldown and ultimate charge must not look alike.** A cooldown drains a smooth bar; the
  ultimate fills a notched one. They are different quantities and used to share a widget.
- **The in-match HUD carries no sentences.** Every sentence lives behind the hold key.

---

## 4 · What competitive play requires, and what it forbids

Hero Strike is aimed at a bracket. That is a set of engineering constraints, not a mood.

- **The host decides everything that scores.** One function awards every point
  (`MatchDirector.AddScore`). A point that can only be created in one place cannot be created
  on a client at all.
- **Contact resolves by distance, never by a trigger volume.** 16 of 36 overlaps were measured
  failing to land. This also keeps the correctness-critical code free of the physics engine,
  which is most of why the port was tractable.
- **A bot presses the same buttons a human does.** One physics step serves both. There is no
  second path where a bot can do something a player cannot.
- **The taya role is derived, `(round - 1) % 4`, never accumulated.** "Everyone defends exactly
  once, clockwise" is true by construction rather than by bookkeeping.
- **Nothing may reward waiting.** The anti-camp and anti-stall clocks exist for this, they HOLD
  rather than run while a unit cannot act, and ultimate charge does not accrue while the round
  clock is stopped.
- **Every number that matters was measured, and the measurement is written next to it.** A
  constant with no recorded reason is a constant the next person will "clean up".

---

## 5 · How a session should work on this repository

Read in this order. It is short on purpose.

1. **`CLAUDE.md`**: the rules of the repo. Which git repo is live, the engine-free core rule,
   how to build and test on this machine.
2. **This file**: what the game is for.
3. **`docs/TODO.md`**: what is actually open. Check it before inventing a task.
4. **`docs/Design.md`**: every balance number, and § 13 for what it does NOT govern.
5. **`docs/Port_Plan.md`** and **`docs/Port_Ledger.md`**: only when doing port work.

⚠️ **VERIFY BY MEASURING, NOT BY LOOKING.** This project has a probe harness because eyeballing
has been wrong repeatedly and expensively:

- `dotnet test Core.Tests/...` for every balance number, in about a second.
- `BotBehaviourProbe` runs a whole match in both modes and prints throws, retrievals, tags,
  skills, ultimates, penalties, emotes and hops, on Eskinita and on Ilalim ng Tulay. It is
  **seeded**; do not
  change the seed to make a run pass. ⚠️ **Its numbers are LIVENESS FLOORS, never comparisons.**
  It is stepped at a fixed 1/60 s now, which removed most of the noise and not all of it: eight
  matches at the shipped settings spread from 58 to 100 throws. ⚠️⚠️ **`docs/TODO.md` § 10 claims
  this was solved and § 16 is the measurement that says it was not**; § 16 also carries how many
  runs an arm an A/B has to buy before its answer means anything.
- `AiDiagnosticProbe` runs one round at 1x with every decision written out, when you need to
  know WHY rather than how much. ⚠️ **It is `[Category("WallClock")]` and excluded from the
  default PlayMode run** with `-testCategory "!WallClock"`, because at 1x its result depends on
  how busy the machine is: it has failed at 21.6 s, 29.9 s and 37.6 s against a 20.0 s bound and
  passed on immediate re-runs. `docs/TODO.md` § 6.
- `AbilityShowcaseProbe` photographs every ability, including the TRANSIENTS, and fails a run
  where one blows more than 12 per cent of a frame to white. That is rule 5 above as a number.
- `Checks.RunAll` runs all five editor checks in ONE Unity launch. The launches are what a
  verification pass costs, not the assertions.
- `AspectRatioProbes` drives real layout through nine resolutions.
- `tools/` has player-side screenshot scripts for anything a picture would settle.

Three findings from this session, as evidence that the harness earns its keep: a HUD string
being rebuilt every frame cost the 6x probe an eighth of its frames and most of its physics
steps; a slipper came to rest 0.7 m outside the arena wall because the bounce only ran while
in flight; and the probe itself was unseeded, so the same build measured 110 and then 467
penalties on consecutive runs. **None of the three were visible by playing it.**

---

## 6 · Things that are settled, so nobody re-litigates them

- **Unity, not Godot.** Decided 2026-08-15. The Godot repo is frozen reference for the old
  version. `CLAUDE.md` § 2.
- **The rules layer is engine-free C#.** Non-negotiable; it is the whole verification strategy.
- **Both modes ship.** § 1.
- **The camera is FPP for people and TPP for props**, and emotes swing to TPP and back.
  `CLAUDE.md` § 3a, which exists because a previous session recorded the opposite and was wrong.
- **Emotes end only by interruption.** There is no emote timer.
- **The art is the team's own and is being built character by character.** The replacement
  queue is `docs/Port_Plan.md` § 8; the authoring guide is `docs/Voxel_Person_Guide.md`.
- **His UI art is the design system.** Wood, amber, cream, ink. Anything drawn in a different
  visual language is the thing that looks broken, not the thing that looks new.

---

## 7 · The shortest possible summary, for a session that reads nothing else

> Four players, one rotating taya, and the whole game is the run back in for your slipper.
> **Two modes: Classic keeps it simple, Hero Strike raises the ceiling for competition.**
> Classic plays four rounds; Hero Strike plays eight.
> Both ship, neither is the "real" one. In a 14 by 14 box, an effect that cannot be read is a
> bug regardless of how good it looks. A player must be able to tell what a power does by
> looking at its icon, and read the details by holding one key, and never by reading the HUD
> during a round. Measure everything; the probes exist because looking has been wrong before.
