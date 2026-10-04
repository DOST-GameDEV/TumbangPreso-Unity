# Practice resume and input producer retirement

Reviewed October 3, 2026 from fetched ASTRAReworks base
`f9552d58a4d59ecdc87a15a2203d82893e3aa5a3` on the separate
`competition-laptop-gameplay` branch. Root TODO and checkpoint were read but not edited.
Runtime/Net, lobby/account UI, NetIceProbe, finalized hero designs and private
artwork are outside this change.

## PRACTICE-BOT-RESUME-1002 reconciliation

The old practice report records a real retained lunge window and two fixture
defects. Its proposed missing OnDisable correction is no longer missing:
`CombatVerbs.OnDisable` already clears lunge/slide contact and local/observed
lunge windup. `LUNGE-DISABLE-LIFETIME-1002` separately qualifies the public
HostResolveLunge body-disable boundary; `SLIDE-DISABLE-LIFETIME-1002` and
carrier-disable evidence cover adjacent component retirement. They do not
qualify the full PracticeRange menu operator, physical tag or bot planner.

The actual removal branch of `PracticeRange.SetBot` disables the body, so it
reaches those existing lifecycle hooks. This work does not revive the retired
helper or reinterpret its original failures as a passing operator gate.
That operator gate was open at this report's initial checkpoint. The separate
[current menu acceptance](practice-menu-operator/README.md) now qualifies exactly
two cases on source `75de68446`: Classic pointer callbacks and Hero Strike
programmatic submit callbacks both pass on the first native run. The actual
prepared menu selects a non-default seat, changes behaviour, removes/restores
the same attacker and defender bodies with correct shoe state, preserves the
untouched seat, and resumes its clock, input and bounded bot-planner liveness.
No new SetBot correction was required. The original retired fixture remains
retired; physical device navigation, victim contact and broader competition
acceptance are separate limits.

## Separate pending-action defect

Focus loss and disabling only PlayerInputReader previously cleared/committed
InputIntent while leaving Carrier and CombatVerbs enabled. CharacterMotor.CanAct
does not equate a missing input producer with an interrupted actor.
Carrier's pending throw and CombatVerbs.StepLunge therefore observed false held
input as an intentional release. Setting an existing active practice bot to Idle
had the same mechanism: park input and disable its brain, with consumers still
enabled. The synchronous public transition can precede a paused-world Update
that would otherwise cancel the windup; this is not a claim that every menu idle
click reproduces it.

The focused correction cancels pending throw/reset and lunge windups before
publishing the producer's release. It preserves already committed contact
windows, cooldowns, held shoes, ordinary physical releases and global CanAct
semantics. Reader disable also enters the existing release gate, including
Interact, so re-enabling a still-held input requires release before a fresh press.
An in-progress reset emits the existing Cancel phase before its local channel
is cleared. The existing host Start handling retains an earlier entry unless it
receives Cancel; actual-peer verification of this path remains separate.

## Executed managed mechanism check

`source_lunge_cancellation_check.py` extracts the shipping StepLunge and the
reader focus/disable callbacks from the pinned fetched base and candidate checkout. It
executes those methods in a managed harness. Hardware discard and engine release
effects are supplied seams; ReleaseLunge records invocation rather than running
travel, presentation, scoring or Unity lifecycle. This is source-bound synthetic
branch evidence, not PlayMode, device or SetBot operator acceptance.

The first generated applications compiled but could not execute: net8.0 was
requested, while the installed runtime is Microsoft.NETCore.App 9.0.19 with
SDK 9.0.317. That launch produced zero cases and is retained under local
`Logs/laptop-source-lunge-cancellation1003`. ONE harness correction selected the
installed net9.0 runtime, with no product/assertion change. Corrected results:

| Case | Original | Candidate |
| --- | --- | --- |
| Focus loss during pending lunge | FAIL, one release call | PASS, zero release calls |
| Reader disable during pending lunge | FAIL, one release call | PASS, zero release calls |
| Ordinary release | PASS | PASS |
| Already committed contact and cooldown | PASS | PASS |

Both corrected harness builds succeed. Original exits 2 for the two causal
contract failures; candidate exits 0, exactly four passing cases. Raw logs and
source hashes live in local `Logs/laptop-source-lunge-cancellation1003-runtime9`.
The checker requires the exact expected case names and outcomes as well as exit
codes. No source-unchanged Unity/tournament gate was repeated.

The reusable checker now defaults to original `f9552d58a` and qualified candidate
`88dba66a1`, with explicit revision overrides. This preserves reproduction of the
recorded lunge-only mechanism after later reader changes add unrelated touch
dependencies. The stored result reflects the same first candidate source read
from the checkout at execution; the native qualification below is stronger
evidence. Pinning the reusable witness is not a new execution or a claim about
later source.

## Native regression qualification

`InputProducerCancellationTests.cs` has twelve focused cases: four pending
throw/lunge focus/disable cases; two ordinary release controls; an already
committed lunge control; a held-touch reader re-enable case; two fresh-Interact
release cases; and two public SetBot active-to-Idle cases. The latter supply
only range readiness/seat registration directly. They do not load a map, operate
PausePanel, run bot planning, simulate movement or qualify the entire operator.
Actual TouchInput feeds the reader in the reader cases. Motor and slipper
simulation are disabled to isolate pending-action consumers.

New test metadata GUID: `e714324d4f1245bb9a54aa47fbcdf9a6`. Initial low disk space
blocked native import. External cleanup recovered enough room for a separate
complete native checkout at `C:/Users/Matthew/dev/tump-laptop-native1003`; no
reset, clean, stash or discarding source work formed part of this report.
The parent ran one heavy job at a time using Unity 6000.5.8f1, isolated named
profiles, GPU classification, 2048 MB job memory and 1024 MB reserve. Only the
four input/practice production files were overlaid for this candidate. Import
metadata churn is retained separately and is not staged as product work.

Original native `laptop-cancellation-original1003` ran exactly twelve cases:
nine causal failures and three passing controls. Pending throw/lunge state
survived focus loss, reader disable and the active-to-Idle SetBot call; held
Interact restarted on focus/reader re-enable; held touch restarted the old
throw windup on reader re-enable. Ordinary throw/lunge release and already
committed lunge contact/cooldown controls passed. Candidate
`laptop-cancellation-candidate1003` ran the identical twelve named cases once:
12 passed, zero failed. No test fixture repair or assertion change was required.

Fresh XML and terminal guard receipts are retained in
[practice-native](practice-native/). The original receipt has exit 2; candidate
has exit 0. Both guards are terminal, preservation completed and lease released.
Candidate XML records the actual test window 2026-10-03 03:24:10Z to 03:24:11Z;
the guard ended at 03:24:15Z. Exact nonzero case counts and named controls,
not process success alone, establish this focused native result.

Acceptance is the native touch-reader callback and supplied-readiness public
SetBot boundary. It does not qualify actual OS focus, human device movement,
full practice menu, victim tag, live peer reset cancellation or a new player.

## Review limits

The candidate has no global Parked/CanAct change and does not erase committed
contact on focus loss. Carrier uses the existing charge cancellation and reset
phase; CombatVerbs factors its existing windup cleanup. The practice idle hook
acts only on an actual active-to-idle transition.

The same review found AIController.OnDisable previously only unsubscribed, so
private planner windup accumulators could survive Idle-to-Active. A separate
adjacent fix extracts the existing ReleaseAll private accumulator reset into
ResetActionState and calls it on disable, preserving shared human hero input.
The adjacent [bot/replay native report](replay/README.md) now records its original
seeded-state failure and first passing candidate, including shared human hero
input preservation. Fresh planner gameplay cadence after resumption remains
unqualified. Physical alt-tab, OS suspension,
keyboard/controller/touch hardware, live peer reset cancellation, new player
builds and wider practice/device qualification remain unverified. The narrow
two-mode menu callback acceptance above is now qualified separately.

A proposed replay camera-marker fix was rejected during review:
WorldLookPresentation.HandlesCamera already explicitly recognizes the shipping
RecordedWorldCamera name. No missing-marker product fix was shipped.
