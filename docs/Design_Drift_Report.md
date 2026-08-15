# Design Drift Report

**Investigated 2026-08-15, from the Godot repo's git history.**

Four numbers disagree between `docs/Design.md` in the Godot repo and the GDScript that ships.
`Design.md` opens with *"a number in the code must match a number here, or one of the two is a
bug"*, so each needed a verdict rather than a shrug.

## The verdict, in one line

**All four are stale documentation, not code defects.** Every constant in the current build is
deliberate, human-instructed, and correctly derived. **`Balance.cs` in this port is right, and
nothing in the Godot build needs a gameplay change.** What needs fixing is prose in `Design.md`.

Two facts establish the direction on their own: `docs/Design.md` was last touched **2026-08-02**
(`1d81383`), while `scripts/characters/character_base.gd` was last touched **2026-08-05**
(`1d3a1d1`). The code is newer everywhere it disagrees.

---

## A · The lunge, and why it looked alarming

**This was the one worth chasing**, because it is the taya's primary scoring verb and the doc
claimed a reach 39% longer than the code delivers.

| | |
|---|---|
| Code | `LUNGE_SPEED` 7.746 → a 1.0 m dash → **2.30 m** reach with the 1.3 sweep |
| `Design.md` §6 table | agrees: *"7.746 → a 1.0 m dash by v²/60"* |
| `Design.md` §6 prose and §2.6 MEASURED | still say 12.247, a 2.5 m dash, **3.20 m** reach |

**History.** `LUNGE_SPEED` entered at **12.247** in `25f6cca` (2026-08-01, *"Make the taya
active"*). It was changed to **7.746** later the same day in `071061c` (*"Stamina, the taya's
two tag verbs, spawns, and instant music"*). Only the abandoned `HANSDAKS-ai` branch still
carries 12.247; `main`, `ESPORTS` and `online/dedicated-lobbies` all carry 7.746.

**It was deliberate, instructed, and re-derived rather than nudged.** The commit added this
comment beside the constant:

> `12.247 -> 7.746 ON 2026-08-01, ON HUMAN INSTRUCTION: "Lunge Tag (Hold E for 0.5s): ... a
> short 1-meter forward dash." Re-derived on the same v²/FRICTION_2 solve every impulse in
> this file uses rather than nudged: sqrt(1.0 × 60) = 7.746.`

⚠️ **AND THE REACH LOSS IS COMPENSATED, WHICH IS THE PART THAT SETTLES IT.** The same commit
gave the taya a **second** tag verb. Its own comment: *"The lunge stops being the taya's only
verb in the same change, the PUNCH below."* The taya went from one long verb to two
complementary short ones: the punch has 1.7 m of reach, a 0.9 s cooldown, no charge, and does
not move the taya at all, while the lunge keeps the dash for somebody running past. The commit
message states the design directly: *"They answer different problems: the lunge is a charge, a
dash and a cooldown, which is right for somebody running past you and wrong for somebody
standing next to you, because the charge is exactly long enough for them to leave."*

**Verdict: no regression. High confidence.** §2.6's measurement (3.20 m) and the §6 prose were
taken before the nerf and never re-run. **Action: correct §2.6 and the §6 prose to 2.30 m, and
note that the punch now covers the close case.** The constants table is already right.

## B · Stamina

| | |
|---|---|
| Code | `STAMINA_MAX` **60.0**, so 1.5 s of sprint at 40/s |
| `Design.md` §3 table | agrees: 60, 1.50 s |
| §3 note, §2.5 MEASURED, §5.3 shove maths | still describe a **50** point pool and 1.25 s |

Changed in the **same commit** as the lunge, `071061c`, and the message is explicit:

> *"Stamina goes to a 60-point pool with a 1.0 s recovery delay, both on instruction. The pool
> matters as distance rather than seconds: a full sprint now covers 8.2 m against 6.84, which
> puts it back in step with the 7.5 box after mech_probe established that the bar is
> dimensioned to one crossing of the danger zone."*

⚠️ **THIS IS THE INTERLOCK WORKING, NOT BREAKING.** §3 argues that `STAMINA_MAX`,
`STAMINA_DRAIN_RATE`, `SPRINT_SCALE` and `CONFINEMENT_RADIUS` are one set. The pool was raised
*because* the box had grown, specifically to restore the "one sprint crosses the danger zone"
property. The doc's own headline finding (6.84 m) is the number from **before** that
correction, which is exactly why it reads as contradicting the table.

**Verdict: deliberate and correct. High confidence. Action:** update the three prose passages
to a 50 → 60 pool, 1.5 s, and 8.2 m.

## C · The reset channel

Code: `lata.gd:178` is `RESET_CHANNEL_TIME / _scale(bilis, TRAIT_SPEED_PER_POINT)`, i.e. 5% per
point, giving **1.36 s** on PASIP and **1.67 s** on BOYBEN. `Design.md` §6 says 1.30 and 1.79,
which require roughly 8% per point.

No commit ever set a per-point constant to 0.08. The doc's pair appears to have been computed
by hand against an assumed spread rather than read off the code.

**Verdict: doc arithmetic error, low consequence. Medium-high confidence. Action:** correct to
1.36 and 1.67. The ORDERING, which is the actual design (the tall empty can is quickest to
right), is unaffected.

## D · The throw gate

`Design.md` §5.1 writes the gate as `max(|x|,|z|) >= 5.0`. The code tests against
`CONFINEMENT_RADIUS`, which is **7.0**.

5.0 was the radius before it was raised, twice, on 2026-08-01. §2 documents both raises at
length; §5.1 simply kept the old literal instead of naming the constant.

**Verdict: stale literal. High confidence. Action:** replace the number with the constant name
so it cannot go stale again.

---

## What this means for the port

**`Packages/com.tumbangpreso.core/Runtime/Balance.cs` needs no change.** It was transcribed
from the GDScript rather than from `Design.md`, which was the right call and is now confirmed
to have been the only correct one: three of these four numbers would have been wrong had the
doc been trusted.

`Core.Tests` already asserts the code's values and names each disagreement in a comment on the
affected test. Those comments can now be updated from "unresolved" to "resolved: doc is stale",
and the two `Combat.LungeReach` and stamina tests are correct as written.

⚠️ **The one thing still genuinely unmeasured** is what the shorter lunge did to the TAG share
of all points across a whole match. The nerf was compensated by the punch **in design**, and
no probe has been run since to confirm it was compensated **in practice**. That is a `fair_probe`
run on the Godot build, not a port task, and it is worth doing before nationals.
