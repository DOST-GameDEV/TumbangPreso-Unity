# Cheska remaining field and alternate review

## Current qualified batch

Implemented after Nemu a029f96e and the owner's removal of UI work. Existing native
ice art and the previously verified Nova loose-slipper launch are retained.

Two defects were reproduced: a denied sheet/barricade survived its zero-duration
ability, and actual ice spawned19.698m from the captured cast pose after the body
moved. The first fixture was strengthened with an explicit predicting-owner
provider; a fixture-only internal-property compile error was corrected by using
the actual ApplyNetworkCast API. Baseline-v3 then failed both real contracts.

Cheska's two instant world effects now wait for host acceptance. Input, local
resource feedback and cast motion remain immediate. The existing PlayAbility
message acknowledges these owners; confirmation creates the payload without a
second resource spend or replaying the cast. A denied request has no unaccepted
physical ice to erase, and a later denial does not delete an earlier accepted
sheet. The grounded cast-confirm marker also waits and uses the accepted pose.
Phaister's separate deferred sky confirmation remains intact.

The shared AimedDestination calculation now consumes the supplied cast context,
with the same held range and rectangular clamp. Confirmed held time is scoped so
it cannot overwrite a newer local hold. The packet field layout is unchanged,
but protocol32 is necessary: protocol31 hosts do not send the required owner
confirmation. Actual32host/31client testing returned the explicit version refusal.

### Local evidence

- CheskaIceContractProbe7/7: prediction/refusal, captured placement, confirmation
  without double spend, an accepted sheet surviving later refusal, two accepted
  sheets coexisting, physical confirmed wall faces, delayed lightning pose, and
  the retained wall/thaw and early ice-mash contracts. Some criteria share cases;
  exact seven names are in confirmation-local-v1.xml.
- Protocol assertion1/1. Existing Phaister Hex/Slow Brand and Blink/Long Stride
  actual placement/behavior2/2 still pass after the shared aim correction.
- Wire audit:68named messages,0mismatched layouts. Authority audit:0ungated
  other-body effect calls. Stat audit:18constructors,0drift findings.

### Real delayed-player evidence

Three independent Windows players, owner link150ms one way, explicit test
profiles and preserved files. Final geometry/confirmation/diagnostic runtime:
09ca7b6a497a7d27e7153e9b8500cd6006b69de1f5db855ae020ff4e89a804e6,
Builds/IceNetworkReview/TumbangPreso.exe. The final wording-only Black Ice change
was made afterward and compiled in Core; this player is not claimed to contain it.

| Case | Result |
| --- | --- |
| Accepted sheet and barricade | PASS,391/402/398host/owner/observer samples, one of each, matching positions despite owner movement, physical wall colliders, one spend each, then expiry. |
| Both denied | PASS,381/396/392samples, owner predicts and receives denial, zero ice on every peer. Deliberately stale owner resource estimates exercise the real host refusal. |
| Accepted sheet, then denied second request | PASS,392/410/399samples, earlier field remains, no second field, matching location and final expiry. |
| Protocol32host,31client | PASS, old player receives Game version mismatch and is not approved. |

The first pair evaluator incorrectly compared each peer's buffered NGO ServerTime
and reported a75ms early owner sample. Counts, positions and retention were all
correct. The raw failure is retained under net-pair-network-clock. The corrected
diagnostic uses same-PC UTCwallTime and the host's last-absence sample, because
first-presence samples bound an event rather than timestamp it exactly. Fresh
both/pair runs passed that check. Shared-clock expiry spread was about175-177ms
for the both case and214ms for the pair under the150ms link. This measures current
network latency; it is not exact same-tick expiry or a distributed clock claim.

### Split Spires and Black Ice

Split Spires was also wrong: the old builder always made three slabs. Actual
variant activation, a .35m body capsule and a low slipper-sized sphere proved
the promised passage blocked. The correction was drafted outside Assets during
other tool work, then applied only after the failure was reproduced. An explicit
split flag keeps the two authored side slabs and omits the centre for this
alternate. It does not infer behavior from a numeric scale or change the default.

Passage/default-wall2/2pass: default3slabs block body/shoe, split2slabs pass both
through the centre, while both side faces still block. The actual arena cast
capture1/1passes and shows the open lane with retained ice forms. The mesh bases
areY0 and retain existing ground support; no geometry was lifted to create a gap.
Images and original-timing video are in cheska-network-evidence.

Black Ice footing1/1passes: default radius2.3m affects a target at1.9m; alternate
radius1.495m leaves that outer lane clear. Controlled crossing samples were
1.333m/s and.966m/s respectively, with correct traction/slow cleanup. These are
short controlled movement samples, not maximum-speed or match-balance claims.
No compulsory trip occurred; source applies stronger drag/traction loss. Copy now
describes that instead of promising nobody can cross or keep their feet. The
final Core build succeeded with0warnings/errors.

### Remaining scope and boundaries

This closes the named confirmation, placement, passage and footing issues. Static
ice and other non-familiar persistent fields still need dedicated reconnect/world
snapshot coverage; do not infer it from Nemu or Phaister rejoin success. Mixed-kit
overlap, wider input/movement/recovery, match lifecycle, spectator and remaining
engineering work continue. UI is removed from the active queue. Inday remains
later and the selected seventh hero/map LAST LAST. No Desktop update, agents,
Figma, usage resets or unapproved paid work.

## Earlier investigation and design record

The notes below preserve the original investigation state and proposed checks.
They are historical; the current result above supersedes their pending wording.

## Reproduced and pending evidence

An isolated activation/refusal callback regression left both separately timed
ice effects running after rejection: sheet1, barricade1. The abilities have
Duration0, while the world components own5s/6s lifetimes. CancelActive exits on
DurationRemaining0, and the kit has no cancellation-owned field references.
Logs/cheska-refusal-baseline-v1.xml and cheska-denied-ice.csv record the failure.

The initial isolated case used the default solo provider. A second run explicitly
uses a predicting-owner provider and also checks actual ice placement after a
captured caster pose diverges from the live body. Current files:
Logs/cheska-refusal-pose-baseline-v2.xml/.log. Collect before edits. These are
callback/physics reproductions, not separate-process network qualification.

Source additionally shows HeroAbility.AimedDestination(ctx) calling the ability
system's LIVE _context instead of the captured context supplied to OnActivate.
The new placement case attaches a real HeroAbilitySystem so it exercises that
path. The existing DelayedAimProbe only proves the context is carried into the
callback; it does not prove a real kit uses it.

## Planned correction, conditional on the second baseline

Do not remove whichever ice object happens to be newest when a delayed denial
arrives. Requests currently have no per-cast identity; that could delete another
accepted sheet. Ice Barricade also has physical colliders, so retaining a false
predicted wall is more than a cosmetic issue.

Prefer a narrow opt-in for Cheska's two instant world effects: predict the input,
charge spend and cast motion immediately, but create the actual ice only after
host acceptance. Reuse the existing reliable PlayAbility message to acknowledge
the owner, with accepted position/forward/aim/hold values. Apply only the payload;
do not spend a second charge, reset clocks or replay the full animation. Refusal
then has no predicted physical field to erase, and an earlier accepted field
remains independent. Offline/host behavior and other heroes retain their paths.

The existing PlayAbility owner branch is currently used only to confirm
Phaister's deferred ritual sky. Preserve that behavior. Forward the accepted ice
context through a narrow effect-confirmation path. AimedDestination should use
the supplied cast pose with the same rectangular clamp and held range as the
live targeting helper. Confirmed held time must not corrupt a newer local hold.

This changes a required networking behavior even if payload fields stay the
same: old hosts exclude ordinary owners from PlayAbility. A new deferring owner
would never spawn ice with an old host. Bump ProtocolVersion31 to32 together with
its focused assertion, and verify actual mixed-version refusal using the retained
protocol31 FamiliarLaptopReview player. Do not update Desktop. No protocol bump
or production change has happened yet as of this plan.

## Focused acceptance

- Predicted owner creates no unaccepted wall/traction field; refusal refunds
  normally and leaves earlier accepted fields alone.
- Host confirmation creates the captured effect without spending another charge;
  two accepted sheets coexist, while a refused later cast does not erase one.
- Actual placement uses accepted pose even if the caster moves/turns afterward.
- Existing physical wall, collision-free thaw and early ice-mash release stay valid.
- Related aimed Phaister/Zack casts retain captured pose, and same-hero tuning
  does not reset. Choose exact needed checks; no full suites.
- Fresh internal Windows player: owner request -> host acceptance/denial ->
  owner/observer geometry and expiry in real delayed peers; old protocol refused.
- Then Black Ice/Split Spires movement/body/slipper gap counterplay and remaining
  persistent-state/rejoin work. Do not claim this plan completes the wider queue.
