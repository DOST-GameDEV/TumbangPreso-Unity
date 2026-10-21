# Ability overhaul and Amihan's kit: design and every moving part

Owner, 2026-09-25: *"we are overhauling how abilities work, abilities will change for defending and
attacking. there will be 2 abilities, one signature ability that doesnt change and stays no matter
what role and one that changes."* Amihan's kit is the first written for the new shape. Owner
answers to the open questions (same day) are recorded in section 2; the direction every effect,
sound and animation follows is [direction.md](direction.md).

## 1. The new shape

- **Signature** (slot 1, `Verb.Skill1`): the same whatever the role.
- **Role ability** (slot 2, `Verb.Skill2`): one for ATTACKING, one for DEFENDING (the taya). It is
  whichever the current role gives, and the role is derived each round, `(round - 1) % 4`
  (`CLAUDE.md` § 4), so it swaps at every round boundary for every hero.
- **Ultimate**: unchanged, paid in objective points.

### 1.1 Data shape

`HeroKit` gains `AttackingSkill` and `DefendingSkill` (both null on a legacy kit) and `IsDefending`.
`Skill2` becomes the LIVE role ability: `DefendingSkill` or `AttackingSkill` by role, or the kit's
own `Skill2` on a legacy kit. Every existing reader of `Kit.Skill2` (the deck, touch, bots, the
wire, the inspect panel) therefore reads the live one with no change, and a legacy kit is
byte-for-byte what it was. `HasRoleAbilities` says which kind a kit is.

`HeroKit.SetRole(bool defending, ctx)` is the one writer. On a change it ends the outgoing role
ability through `EndEarly` (a grant must never outlive its role) and resets its cooldown the way a
round boundary does.

### 1.2 Role timing

`HeroAbilitySystem.Update` reads `_motor.IsDefender` (already derived from the round and already on
every peer) and calls `SetRole` before `Kit.Tick`. The taya is assigned in `ResetWorld` at the round
boundary, which is also where `ResetKit` runs, so the swap lands with the round's cooldown reset
and never mid-round.

### 1.3 Network

Nothing about the role goes on the wire: every peer derives it from the same `IsDefender`. The
ability-state snapshot's second slot carries the LIVE role ability's cooldown; both role
abilities are reset at a round boundary, so the idle one is always at rest and has nothing to
carry. New wire content is for the new mechanics (section 4), not for the role.

### 1.4 Presentation of the swap (three devices)

The deck's slot-2 tile shows the live role ability's glyph and name, with a small role badge
(attack chevron or taya shield, in the role colours) on the tile's corner. When the role changes
the tile plays a flip (0.35 s) and the badge pulses once. Touch reads `kit.Skill2` already, so the
thumb button swaps with it; the pad and keyboard prompts read the live binding and do not change.
The hold-to-read tray lists the signature, BOTH role abilities (the live one first, the idle one
dimmed with its role named) and the ultimate.

### 1.5 Screens

Character select, the skill tree and the loadout screen show **Signature, Attacking, Defending,
Ultimate** for a role kit (four tiles, the two role tiles labelled by role) and the old three for
a legacy kit.

### 1.6 Loadout sidegrades

Owner: *"we will figure out the variants soon, keep them all as extra skills for now"*. The
existing variants are untouched for every hero. Amihan's placeholder rows are renamed to her real
abilities (signature and ultimate slots) with no gameplay tuning until the owner designs them.

### 1.7 The other seven heroes

Unchanged until the owner approves a mapping. Proposal in section 6.

## 2. The owner's answers, 2026-09-25

| Question | Answer |
|---|---|
| Whirled | Owner's status table: *"Drops slipper if currently in hand. Prevents slipper retrieval for 2.5 seconds."* Tooltip: *"Disabled Slipper Retrieval"*. |
| Other statuses | Same table: **Chilled** 50 % slower for 5 s (*"Reduced Movement Speed"*); **Frozen** no movement or interaction 2.5 s (*"Disabled Movement and Interaction"*); **Tagged** no movement or interaction 5 s, *"Cannot be removed or be immune to."* Chosen: one shared status system with all four; Cheska's ice sheet applies Chilled. |
| Updraft | *"flies high and can throw slippers but cant pick up unless they choose to go down"* |
| Storm Surge distance | *"very far, the rsn for this is we want them to fall off the map or pushed to the edge"* |
| Loadouts | *"keep them all as extra skills for now"* |

## 3. The status system

Engine-free table in `Core/StatusRules.cs`: kind, seconds, speed scale, whether it blocks movement,
interaction and slipper retrieval, whether it drops a held slipper, whether it can be removed, and
whether immunity applies. Four rows, the owner's numbers.

| Status | Seconds | Implementation | Wire |
|---|---|---|---|
| **Tagged** | 5.0 (`Balance.TagStunTime`) | the tag's stagger, now `ApplyTagged`, which ignores stun immunity (Carapace) and cannot be mashed | existing stun fields; tagged is `StunElement.None` with a stun running |
| **Frozen** | 2.5 | an Ice-element stun (Glacial Nova already 2.5 s) | existing stun fields |
| **Chilled** | 5.0, speed x0.5 | new timer on the motor, refreshed by Max, applied in the speed product | new field on `SyncUnit` |
| **Whirled** | 2.5 | new timer on the motor; host drops the held slipper; pickup refuses while it runs | new field on `SyncUnit` |

Overlap is `Max()` for every timer (`CLAUDE.md` § 4). HUD: `StatusMarks` draws each live status as
an icon over the body (world space, every player) and a row on the local player's own HUD with the
tooltip text and a draining ring. Icons are drawn by `tools/build_ability_icons.py` beside the
ability icons.

## 4. Amihan's kit

Numbers are authored as DISTANCES and solved against `Balance.Friction` (30) as the rules require.
`Balance.MaxKnockbackSpeed` (16 m/s) caps a single impulse at 4.27 m, which is shorter than
anything this kit asks for, so the long moves use a **carry**: a velocity held for a solved time,
then released to decay against `Friction`. Distance = v x t + v^2 / (2 x Friction), so
t = (D - v^2/60) / v. One motor method, `BeginCarry`, host-resolved, sent to a remote owner by one
message.

### 4.1 Quick Dash (signature)

- 40 s cooldown. Propels 5.0 m in the aimed direction (the body's facing): carry 14 m/s for
  0.124 s, tail 3.27 m.
- Every other player within 0.9 m of her swept path (host, by distance, segment sweep like the
  lunge) is **Whirled** and pushed 1.2 m away from her line (v = sqrt(2 x 30 x 1.2) = 8.49 m/s).
  Each once per dash.
- Cannot be cast while stunned (standard) or while flying.

### 4.2 Updraft (attacking role)

- 45 s cooldown (not given by the owner; set to sit between her other two, flagged in the handoff).
- 10 s flight at 2.8 m above the ground she took off from, rising in 0.45 s. Air steering at her
  normal speed. Height is held by the motor (`BeginFlight`), not by gravity.
- **Can throw** from the air. **Cannot pick up** while aloft. **Going down**: press the role key
  again, or Grab, and she glides down (3.5 m/s); when she lands the flight ends and she can grab.
  Ends early on a stun (Frozen, Tagged, a trip).
- Not taggable while aloft (the taya's reach is a flat distance; a body 2.8 m up is out of it).
  **Cannot be cast while holding a slipper inside the taya's box**, so it is never an escape from
  the retrieval (`VISION.md` § 0: the tension is the retrieval).

### 4.3 Whirlwind (defending role)

- 35 s cooldown. An arc of gale 3.2 m across, 0.9 m deep, born 1.0 m in front of her and rolling
  forward at 5.5 m/s for 2.5 s (13.75 m). Players it passes through are **Whirled** (once each).
- Host resolves contact by distance to the arc each step. Every peer runs the same arc from the
  cast's accepted pose and time, so the picture agrees; a rejoiner gets it from the world-effect
  snapshot.

### 4.4 Storm Surge (ultimate)

- 15 objective points. The standard 0.4 s wind-up, then **2.5 s of gathering** during which a
  fan-shaped telegraph (70 degrees, map-wide) shows exactly where the wind will go, locked to her
  position and aim at the cast. She may walk at half speed while it gathers; the storm is not hers
  to ride.
- On release every other player inside the fan is **carried 16 m** along the fan's direction
  (15 m/s for 0.817 s; tail 3.75 m) with a small lift, and every loose slipper inside it is thrown
  the same way. On street maps they end against the edge; on Sa Bubong they go off the deck into
  the rail catch; on Lagoon into the water. The can is untouched.
- The introduction (section 5) plays first, in the shared ultimate phase.

## 5. Presentation, part by part

| Part | Where | Notes |
|---|---|---|
| Wind VFX toolkit | `Visual/WindVfx.cs`, `Resources/Shaders/WindRibbon.shader` | ribbon, fan and ring meshes; a shader with a bright core line, ink edge, streaks, head-to-tail fade and thinning dissolve, all driven by a sampled phase so replays and pauses agree |
| Ability VFX | `Visual/AmihanVfx.cs` | dash slipstream, updraft column and hover ring, whirlwind front, storm fan and wall, Whirled mark, contact bursts, cotton and thread motif particles |
| Status marks | `UI/StatusMarks.cs`, icons in `Resources/UI/status-icons/` | over-head icons for everyone, own-HUD row with tooltip |
| Body casts | `Visual/HeroAbilityClips.Amihan.cs`, baked by `Editor/HeroMotionAuthor` into `Art/characters/amihan-motion/` | dash, updraft launch, flight hover loop, whirlwind sweep, storm plant |
| First-person hands | `Camera/ViewmodelArms` actions | a separate gesture per ability |
| Glyphs | `AbilityGlyph.AmihanQuickDash`, `AmihanUpdraft`, `AmihanWhirlwind`, `AmihanStormSurge`; drawings from `tools/build_ability_icons.py` | |
| Audio | `tools/build_amihan_audio.py`, cues in `AudioCues` | one recipe per ability, contact and status cues, the ultimate theme and gather |
| Ultimate introduction | `tools/author_ultimate_intros.py` (`amihan`), `Visual/HeroIntroductionScene.Amihan.cs` | who, intent close-up, gather in the kasikus storm, release; 3.6 s |
| Sky | `SkyEvent.Look.Monsoon` | the amihan season sky: a bright cool overcast with fast cloud streaks |
| Lines | `HeroLines` amihan rows, `docs/HUMAN.md` | rewritten for the real abilities; recordings owed |
| HUD role swap | `TumpPowerReadout`, `TumpSkillView`, `AbilityInspectPanel` | badge, flip, tray |
| Screens | character select, skill tree, loadout | four tiles for a role kit |
| Bots | `AIController` | signature to close or escape, Updraft to reach a throw line or dodge, Whirlwind when attackers come for slippers, Storm Surge when several are in a fan toward an edge |

## 6. Proposed mapping for the other seven (NOT APPLIED; owner to approve)

| Hero | Signature | Attacking | Defending | New ability needed |
|---|---|---|---|---|
| Dante | Seismic Stomp | Demonic Carapace | Demonic Carapace | a defending ground control (e.g. a short stone wall) to replace Carapace on defence |
| Cheska | Permafrost Sheet | (new) an ice slide for the run back in | Ice Barricade | attacking |
| Sean | Flame Rush | Ignition Cannon | (new) a short fire line at the box edge that burns whoever crosses it | defending |
| Zack | Bolt Sprint | Magnet (recall) | (new) a shock trap on the box line | defending |
| Nemu | Phantom Veil | Astral Hijack | (new) a spirit ward that slows a lane | defending |
| Phaister | Shadow Blink | (new) a hex on the thrown slipper | Hex | attacking |
| Rafi | Crosscurrent | Mirrorwake | (new) an undertow at the box edge | defending |

## 7. Verification

`dotnet test Core.Tests` (status table, Amihan balance rows, carry arithmetic), EditMode (glyphs
and actions for all eight, role swap, legacy kits unchanged), `playmode_suite.py --gate`,
`Checks.RunAll`, every `tools/audit_*.py`, `BotBehaviourProbe`, `AbilityShowcaseProbe`, and a
Windows build into `Builds/`.

## 8. Decisions taken where the owner's answers left a gap (for his review)

| Point | Decision | Why |
|---|---|---|
| Updraft cooldown | 45 s | not given; sits beside her 35 and 40, and a move-your-own-body power gets a long cooldown (`HeroAbility`'s charge rule) |
| Updraft and the taya | aloft = out of the tag's reach; cannot START holding a slipper inside the box | "flies high"; keeps the retrieval the moment you can be caught (`VISION.md` § 0) |
| "Go down" | press Updraft again, or Grab, and she glides down at 3.5 m/s | "cant pick up unless they choose to go down" |
| Storm Surge targets | every other player (allies too) and every loose slipper in the fan; never the can | the table says "all players and slippers"; the whole game's abilities hit everyone but the caster |
| Storm Surge distance | 16 m carry (15 m/s held 0.82 s) with a small lift; on Sa Bubong they go over the rail, on Lagoon into the water | "very far ... fall off the map or pushed to the edge" |
| Storm Surge delay | the 2.5 s is her wind-up: she is rooted while the fan telegraph shows | "After a 2.5 s delay"; the root is what she pays |
| Quick Dash push | 1.2 m, sideways off her line and a little forward | "slightly pushes back" |
| Chilled on Cheska's sheet | the old 0.55 speed zone is replaced by Chilled (50 %, runs 5 s from stepping off); the slip stays | "Cheska's ice sheet becomes Chilled"; the slip is the ice, not a status |
| Frozen | still mashable, as every element hold has been | the table says 2.5 s and nothing about mashing |
| Tagged | now ignores stun immunity (Carapace) | "Cannot be removed or be immune to" |
| Whirled on the dash's own caster | never; nothing she does Whirls herself | |

## 9. Known gaps (tracked in `docs/TODO.md` ABILITY-1 and HERO-8)

- A rejoiner who arrives during Storm Surge's 2.5 s gather sees no telegraph (the host's push still lands); a rejoiner mid-flight sees her position but not the hover ring until her next cast.
- Replays record the gale and the storm; the dash slipstream, the updraft column and the hover ring are not yet recorded fields.
- The voice lines are text only until recorded (`docs/HUMAN.md`).
