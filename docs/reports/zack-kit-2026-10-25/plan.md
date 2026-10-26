# Zack rework: plan and direction (2026-10-25)

> Superseded in part on 2026-10-26: Full Throttle replaced Afterimage and the V4 numbers apply. See
> [the refinement](refinement-2026-10-26.md). Afterimage rows below are history.

Status: IMPLEMENTED ON MAIN, focused local validation complete; owner visual/audio review and actual peers pending.
Refined 2026-10-26 from owner feedback (speed, Charge, past moments, persistent Live Wire, Overclock fuel and zap):
[refinement](refinement-2026-10-26.md) supersedes the matching numbers below. The owner authorized enabling Zack sounds and publication; see [electrical audio](lightning-audio.md). [Verification](verification-session3.md). Approved kit (Wiki "Zack Kit Rework" tab, V3 SKILL SHEET), the owner's four open answers recorded in
section 7. Every number is a starting value: section 8 lists the ones to watch in play. Research is `research.md` beside
this file. Method: `docs/HERO_KIT_METHOD.md`. Network: `docs/SKILL_NETWORK_CONTRACT.md`.

## 0 · Who he is in play

The poster boy: one clear verb per button, readable on first sight, and every button teaches the core loop (run in,
retrieve, get out, dodge the taya). He is the speedster: he gets faster the longer he runs (Momentum), he can take back
the last three seconds (Backtrack), he can send a copy of himself to draw the taya off (Afterimage), and as taya he
strings a live wire across a lane (Live Wire). The ultimate is simply MORE of him, for 25 seconds.

**Look family (written before any effect).** Gold street electricity, narrow and branching.

| | Zack |
|---|---|
| Colours | core `#FFE27A`, body `#FFC21A`, edge `#C8740E`, ink line `#2A2230`. Never white, never blue, never a flag colour |
| Shapes | thin jagged lines with knees, short forks, sparks as four-point glints, his own body as the decoy, two compact floor-mounted reel anchors |
| Motion | snap, then chatter: everything arrives in one frame and then crackles in short bursts; nothing glides |
| Ends | a line breaks into three or four short sparks that fall and wink out; a body breaks into rising flecks from the feet up |
| Sound family | dry snaps, a buzzing hum at mains frequency, wire twang, a rising whine for speed, a pop for the decoy |

Rules: (1) everything starts ON him or on the court and ends there; (2) every line has a dark edge so it reads on a bright
court; (3) nothing sits on the screen centre in his own first person; (4) no constant colour wash on the court or the
screen; (5) the decoy is his body, not a ghost.

## 1 · The kit

| Slot (kept id) | Name | Cooldown | In-game description (word for word) |
|---|---|---|---|
| Passive | **Momentum** | none | The longer Zack keeps running, the faster he gets, up to 20% faster after 3 seconds. Stopping, throwing or being hit resets it. |
| Signature (`zack_skill1`) | **Backtrack** | 30 s | Snap back to where you were 3 seconds ago, keeping whatever you are holding. |
| Attacking (`zack_skill2`) | **Afterimage** | 30 s | Attacking. Send an electric copy of yourself running straight ahead for 3 seconds. It vanishes if the taya touches it. |
| Defending (`zack_skill2d`) | **Live Wire** | 30 s | Defending. Hold to aim, release to string a sparking wire between two points up to 6 metres apart. The first attacker to cross it is Zapped. Lasts 10 seconds. |
| Ultimate (`zack_ultimate`) | **Overclock** | 15 points | Zack becomes Overclocked for 25 seconds. |
| Status | **Overclocked** | | Moves 40% faster. Backtrack recharges in 3 seconds, Afterimage sends two copies, and Live Wire Zaps every attacker who crosses it. |
| Status | **Zapped** | | Cannot move or act for 1 second. |

Display names are the table's (title case on the Wiki, the game's own casing in the tiles). Stable ids are kept so saves,
mastery, loadouts and HUD slots hold.

## 2 · The rules, precisely (all in `Core.ZackRules`, asserted by `Core.Tests/ZackRulesTests.cs`)

**Momentum** (owner-simulated, both roles, Hero Strike only)
- Running = grounded, steering input held, planar speed at least `MomentumRunSpeed` 1.5 m/s. Time spent running builds
  `MomentumSeconds` to 3.0; scale = 1 + 0.20 x min(1, seconds / 3). Three visible steps at 1, 2 and 3 s.
- Reset to 0: speed under the threshold for more than 0.15 s (a stop, not a footstep), a throw release, any stun or trip,
  any host impact or carry on him, any negative status gained, a tag, a Backtrack (the snap stops him), the round reset.
- Speed is multiplied into the motor's existing formula through `HeroKit.MovementSpeedScale`, like Second Wind and
  Backwash. It stacks with Overclocked (owner, 2026-10-25): 1.4 x 1.2 = 1.68 at most.
- Not on the wire: the peer that simulates his body computes it. Observers draw its look from his observed speed
  (`ZackMomentumLook` reads `PresentationTravelVelocity`), so every screen sees the same three steps within a frame.

**Backtrack** (Predicted, the Phaister TELEPORT route: owner moves, host replays and broadcasts the teleport epoch)
- Every peer keeps a 4.0 s ring of his planar positions at 20 Hz (on every body that carries the kit), covering the host's 3.5 s validation window.
- The owner picks the destination from his own ring at 3.0 s ago (or the oldest sample this round if fewer than 3 s
  have passed, at least 0.5 s) in `CaptureLocalCastContext`, so the accepted cast carries it as the aim point.
- The host accepts only a destination within `BacktrackTolerance` 2.5 m of its own ring of that body anywhere from 2.5 to
  3.5 s ago, inside the playable rectangle, on ground, with no wall between it and the ring point. Otherwise refused and
  refunded (the existing predicted-refusal path).
- He keeps the slipper in hand, his statuses and his facing. The accepted gameplay snap is instant. Its render-only body and eye are pulled back along the actual path in 0.42 s (owner refinement, section 7).
- Cannot be cast while stunned, Zapped, tagged, rooted, flying or carrying the can.
- Cooldown 30 s; while Overclocked 3 s (and Overclock's start clamps a running cooldown to 3 s).
- Momentum resets (he stopped).

**Afterimage** (HostConfirmed, a host-owned dynamic field like Rafi's water)
- Attackers only. One copy, two while Overclocked (fanned 14 degrees either side of his aim).
- Starts at his feet, facing his flat aim; runs straight at 5.0 m/s (an attacker's run) for 3.0 s, so 15 m at most.
- Its path is solved once by the host against static geometry and the playable rectangle (`ZackAfterimage.SolvePath`);
  where the path ends early it stops there, runs in place for 0.25 s and fizzles. Every peer draws the same path from
  the field's start, end and age: the host sends only spawn and pop.
- Pops when the taya's body comes within 0.8 m of it (host only, distance, never a trigger). It never blocks, scores,
  carries, triggers a tag, picks up, knocks the can, sets a hazard or appears in `round.Players`.
- Carries a drawn copy of the slipper he is holding, if any (render only).
- Bots: an AI taya treats a live copy as a chase candidate it has not yet seen through (section 5).

**Live Wire** (HostConfirmed, a host-owned dynamic field)
- Defending only. Hold to aim (0 to 6 m, `AimByHolding(1.5, 6, whereLooking: true)`), release: post A at his feet, post B at
  the aimed point (owner, 2026-10-25). Length 1.5 to 6.0 m. Both posts on ground within 0.3 m of his floor, inside the
  playable rectangle, with no wall between them at 0.9 m.
- The tension wire sits at 0.28 m on compact floor anchors and 0.24 m in the middle. A timed jump clears it (owner refinement, 2026-10-25).
  It has NO collider: bodies, slippers and the can pass through it.
- Crossing = an attacker's feet go from one side of the segment to the other between two host physics steps, within the
  segment's length, feet no more than 0.40 m above the wire floor at the crossing plane. Taya, tagged and already-zapped-by-this-wire bodies are ignored.
- Normal wire: the first crosser is Zapped and the wire snaps. Overcharged wire (strung while Overclocked): every attacker
  is Zapped the first time they cross it, and it lasts its full 10 s.
- Lasts 10 s. A new cast retires his old wire. Round end retires it.

**Overclock** (SharedUltimate, Dante's MOUNTAIN FORM pattern)
- Cost 15. The cutscene (section 4) is the windup (`IntroductionIsTheWindup`): play begins with him Overclocked.
- 25 s on one clock on every peer (the accepted activation reaches every peer, the kit ticks everywhere). A late joiner
  gets the time left in the timed-kit snapshot's passive fields (Momentum has nothing to restore).
- While Overclocked: x1.4 speed, Backtrack cooldown 3 s, Afterimage two copies, Live Wire overcharged; the meter does not
  charge; the ultimate tile counts the 25 s down (`UltimateAftermath`). Round end ends it.
- The old match-long state, the 4.5 m Zap ring and the old "permanent" snapshot flag are gone.

**Zapped** (redefined, `StatusRules.ZappedSeconds` 1.0)
- A 1.0 s full stun on the existing stun stack with `StunElement.Shock`: no movement, no throw, no pickup, no punch, no
  skill, no reset. Overlaps by `Max()`, refused by status immunity (Dante), lengthened by Vulnerable like every stun,
  overridden by a tag, ends flight. `IsZapped` reads the stun (as `IsFrozen` reads Ice), so every gate that already
  respects a stun respects it, and the wire, replay and rejoin already carry the stun state.
- Distinct from Frozen (2.5 s, ice): gold electric rim on the body and a short jolt clip; the victim STAYS IN FIRST
  PERSON (a 1 s swing to third person and back is noise), with a gold crackle round the frame edges and their hands seized.
- The old `_zappedLeft` timer and its ability-only gates are removed; the `SyncUnit` float stays on the wire carrying the
  derived value so the layout does not move (protocol bump for the meaning, section 6).

## 3 · Six beats per ability (body, first person, effect, sound)

| Ability | Tell | Release | Travel | Contact | Linger | Dissipate |
|---|---|---|---|---|---|---|
| Momentum | none: it is earned | step 1 (1 s): sparks at his heels | step 2 (2 s): a short gold ribbon from his heels | step 3 (3 s, full): three ghost frames trail him, his FPP hands crackle | held while he runs | a stop: the ribbon breaks into sparks; a hit: one snap and it is gone |
| Backtrack | (ready) his own faint echo at the 3 s-ago spot, only on his screen | a sharp inhale of current: his body flickers, a snap | the rewind: a gold streak runs BACKWARDS along his real path in 0.42 s, ghost frames pulling into the landing | he reappears in a ring of sparks on the court, a short static rewind sound | ring holds 0.4 s | the ring breaks into four sparks |
| Afterimage | a quick sideways shrug of his shoulders (the copy peels out) | a crackle at his feet, the copy steps out of him | the copy runs his real run, crackling, a thin trail at its heels | the taya touches it: it pops into flecks with a dry pop and a "zip" | none | after 3 s it stumbles a step and fizzles from the feet up |
| Live Wire | hold: he pulls a wire out from one fist across his body (aim pose); the ring and a dotted line show where it will run | release: he throws the coil; post B drops on the spot, post A bangs into the court at his feet | the wire whips out between them and snaps taut with a twang | an attacker crosses: a flash at the crossing point, the wire jumps, the victim jolts (Zapped) | the wire hums and sparks at the tangles; overcharged it is doubled and sparks more | it snaps (normal) or after 10 s goes slack, sags to the court and the posts sink |
| Overclock (play) | the cutscene | he sets off already charged | gold crackle round him, three ghost frames behind him, a lit trail at his feet | his skills show it: two copies, a double wire, the Backtrack tile cycling fast | the tile counts 25 s down | at 5 s left the crackle stutters; at 0 a last snap and the light drains down his body |
| Zapped (victim) | none | a hard jolt: body locked, arms out, a gold rim and arcs | | | 1 s of crackle, hands seized in first person, gold frame edges | a shake-off, back to control |

## 4 · The Overclock cutscene, trimmed (owner, 2026-10-25: trim the bolt one, about 4.5 s)

One sentence: he calls the storm, the world stops for everyone but him, and he takes the lightning into himself.
One travelling thing: the bolt, from the sky into his raised finger. The staging, sky, city and bolt look of the current
6.4 s film are kept; only time is cut and the ending is changed. New beat table:

| Beat | Was | Now | Change |
|---|---|---|---|
| THE CALL: flick, finger up, the far bolt answers | 0.00 to 1.20 | 0.00 to 1.05 | slightly quicker |
| THE STOP: the bolt comes down at him and the world stops | 1.20 to 1.94 | 1.05 to 1.70 | kept |
| THE LOOK: he looks at the lens | 1.94 to 2.70 | cut | |
| THE POKE: he pokes the frozen tip, it wobbles | 2.70 to 3.60 | 1.70 to 2.70 | kept |
| THE SHRUG: wide round the stopped court | 3.60 to 4.50 | cut | |
| THE LET GO: the world restarts, the bolt finishes into him | 4.50 to 6.40 | 2.70 to 4.50 | no ring, no Zap of staged players; the bolt enters his fist and runs down him; he drops into a sprinter's set and the cut comes as he goes |

Play picks up with him Overclocked, his crackle already on (never charged twice). The held-equipment rule is unchanged
(the carrying hand keeps its shoe). The theme cue keeps its name. The owner later authorized enabling the electrical family (section 9).

## 5 · Everything else the kit touches (and what each one needs)

| System | What changes |
|---|---|
| `Core`: `ZackRules`, `StatusRules`, `HeroLoadout`, `HeroLines` text, `AiTuning` | new rules; Zapped 1 s full stun; loadout variant rows that named old skills |
| Kit: `ZackHeroKit.cs` | rewritten: Momentum, Backtrack, Afterimage, Live Wire, Overclock; `ZackHeroKit.ClosedCircuit.cs` deleted |
| World objects | `ZackAfterimage`, `ZackLiveWire` (host-owned dynamic fields, `WorldEffectSnapshot.Kind` 20 and 21 appended, the existing dynamic-field message), `ZackPathHistory` |
| Status | `CharacterMotor.Status.cs` (ApplyZapped on the stun stack), `CharacterMotor.cs` gates, `HeroKit`/`HeroAbility` cast gates (the ability-only Zapped gate removed), status UI text from `StatusRules` |
| Camera | `CameraRig`: a Zapped stun keeps first person; victim screen crackle in `TumpHudEffects` (existing Shock coat colours) |
| First person | `ViewmodelArms` new actions `backtrack-snap`, `afterimage-split`, `livewire-pull` (aim) and `livewire-throw`, `overclock-take`; Overclocked and Momentum hand crackle on `ZackSparkHand` |
| Body clips | `hero-zack-backtrack`, `hero-zack-afterimage`, `hero-zack-wire-aim`, `hero-zack-wire`, `hero-zack-overclock` (`HeroAbilityClips.Zack.cs`, baked by `ZackMotionAuthor`) and the Zapped jolt reusing the shared status clip path |
| Network | the dynamic-field route carries spawn/pop/snap; Backtrack uses the predicted teleport route; Overclock rides `SharedUltimate` plus the timed-kit passive fields; `MatchRpc.Circuit.cs` and `ZackCircuitState.cs` deleted; protocol 182; fingerprint follows the kit automatically |
| Maps | Ilalim ng Tulay's `OverclockBoostPad` popup said "OVERCLOCKED!": it now says "TURBO!" so the map never names Zack's status. Its 1.5x boost stacks with Momentum and Overclocked (measured cap 2.52 x 7.5 = 18.9 m/s, under `MoveBudget` 30). Wires and copies are clamped to the playable rectangle and stop at walls, so they work on every map, rooftops and the arena shaft included. Jump pads launch through a wire without a Zap only if the feet clear 1.6 m |
| Other heroes | Dante's immunity refuses Zapped; Paete's roots and a tag override it; Amihan flying passes over a wire; Phaister's mark slow and Chilled still multiply Zack's speed down; Rafi's water and Nemu's ghosts ignore the copy; nothing can carry, pull or freeze a copy (it is not a body); Thorn Harvest and Fetcher never see the copy's drawn slipper (render only) |
| Bots | Momentum: nothing to decide. Backtrack: an attacker who just picked up its slipper inside the box with the taya within 4.5 m, if the 3 s-ago spot is outside the box and farther from the taya. Afterimage: an attacker about to go in for its slipper with the taya between it and the slipper, aimed away from the slipper. Live Wire: the taya strings it across the lane between the nearest attacker and the can. Overclock: cast early in a round with at least 15 s left. AI taya and copies: a copy within its chase range becomes a candidate with a reaction delay; it chases it until it touches it or "sees through it" (0.6 to 1.4 s by difficulty). Attacker bots steer round a live wire (its segment registered as hazard discs) |
| UI | `AbilityIcons`, `HeroGlyphs` (new glyph shapes for the four skills), `AbilityInspectPanel` text, `HeroPassives` (Momentum), ultimate tile countdown, status tooltips from `StatusRules` |
| Presentation | `UltimatePresentationDirector` motif kept (a bolt), `UltimateLiveLook.Zack` flag and the ring removed, `UltimateProp.Zack` kept (the storm city), `HeroIntroductionScene.Zack.cs` trimmed, `ZackArcHands`/`ZackSparkHand` kept for the cutscene hands and his Overclocked hands |
| Removed | Quick Circuit, Bank Shot, Closed Circuit, Thunderstrike's ring; `ZackCircuitState`, `MatchRpc.Circuit`, the BankShot slipper affinities, `ZackMagnetCharge`, `ZackBankFlight`, `FxStudyZackBank*`, `FxStudyZackCircuit`, `ZackCircuit*` visuals, `ViewmodelArms.ZackBankShot/ZackCircuit`, the two old motion authors and films, their tests |
| Docs and Wiki | this plan, `HERO_KIT_REWORK_DECISIONS.md`, `TODO.md`, `ACTIVE_REWORK_LEDGER.md`; the Wiki's Abilities - Technical and Status Effects tabs and a summary on the Zack Kit Rework tab |

## 6 · Network contract

- Protocol 183 retains the 182 rework contract and adds the low, jumpable wire contact rule. Zapped means a 1 s full stun (a 181 peer would let a zapped body walk), Backtrack's host check,
  Overclock is timed (a 181 peer would keep it for the match), and two new dynamic-field kinds (20 Afterimage, 21 Live
  Wire) on the existing reliable dynamic-field message, validated by `WorldEffectSnapshot.Valid` before any mutation.
- Afterimage: the host spawns it from the accepted cast (start, flat forward, solved end, copy index) and broadcasts the
  complete field with a fresh event id; a pop or fizzle is the same field with `Split` set. Peers restore at elapsed
  time, never recast; a late joiner gets live copies in the world snapshot; replay draws them presentation-only.
- Live Wire: the host validates placement, spawns, broadcasts; a crossing Zaps through `ApplyZapped` (host only) and,
  for a normal wire, broadcasts the snapped state. Overcharged is a field flag (`SecondScale` 1), decided at the cast.
- Backtrack: owner prediction plus host validation of the destination against the host's own ring; refusal rolls back.
- Overclock: SharedUltimate activation on every peer; 25 s clock locally; timed-kit snapshot passive fields for a joiner.
- Momentum: owner-local, nothing on the wire.

## 7 · Owner answers (2026-10-25, one batch)

| Question | Answer | What it becomes |
|---|---|---|
| Live Wire's two points | His feet to the aimed point | post A at his feet, post B where he aims, 1.5 to 6 m |
| Overclocked and Momentum | Stack (1.68x) | both multiply the motor's speed; flagged for the taya role |
| Who sees Backtrack's return point | Zack always, everyone on cast | a private echo while ready; a public rewind trail and landing ring at the cast |
| The ultimate intro | Trim the bolt one | section 4, about 4.5 s |
| Stun breakout, clarified in session 3 | Follow shared timer-only recovery | Zapped ends on its timer; no button mash shortens it |
| Visual refinement | Ekko-like body pull, no old FPP magnets, low Cypher-like wire anchors | 0.42 s render-only reverse body travel, bare powered hands, low floor reels and a jumpable wire |
| Publication branch | Commit on main as work progresses | Main is authorized for this rework including its required compatibility changes |

## 8 · Numbers to watch in play (starting values)

- Overclocked taya with full Momentum: 7.5 x 1.68 = 12.6 m/s against attackers at 5.0. May dominate the chase.
- Backtrack every 3 s while Overclocked: 8 snaps in 25 s. May make an Overclocked attacker untaggable.
- Afterimage at a fixed 5 m/s: slower than a running Zack with Momentum; the copy may read as "not him" to sharp eyes.
- Live Wire's 1 s Zap on a 30 s cooldown with a 10 s life: probably fair; overcharged across the box mouth may be harsh.
- Zapped 1 s is shorter than Frozen 2.5 s; Vulnerable makes it 1.5 s like any stun.

## 9 · Sound

The owner later explicitly delegated creation/refinement, requested a stronger buzz
and taser feel, authorized enabling Zack's sounds and requested publication on main.
Zack's sixteen original electrical cues now enter the scoped allowlist. The global
switch stays false and other heroes retain their existing release policy. The
[implementation and native capture evidence](lightning-audio.md) supersede the
earlier audition-only boundary for Zack. Owner listening judgment remains open.
