# Possession contact at ordinary paused game speed

Date: 2026-10-03. Baseline `89270ce88`. Focused three-case fixture authored,
reviewed. Original native gate reproduced one actual paused-stagger failure
and passed both controls. The first narrow contact candidate passes all three
unchanged cases. Main audited frozen source/fixture hashes and terminal
preservation before publication.

## Question and source path

Ordinary offline pause requests game speed 0, without the shared introduction's
PresentationClock.Held flag. GhostPetCompanion.LateUpdate correctly passes
scaled deltaTime to possessed movement, so that movement is already frozen.
UpdatePossession still calls ResolvePossessionContacts at zero delta. That
method can apply a new host-side stagger/impulse to a nearby registered player
without checking requested game speed. The question is whether an accepted
possession therefore changes gameplay while ordinary pause freezes the world.

Only demonstrated contact at requested speed 0 is in scope. Preserve live
contact, the same possession across Resume, its timing/resources, movement,
network ownership, authored appearance and ordinary hitstop behavior. Do not
use owner CanAct as a broad guard that would change the finalized familiar
behavior under unrelated statuses. No ordinary pause-movement retune is needed.

## Frozen original gate

Run exactly `TumbangPreso.PlayTests.KuroPauseContactLifetimeTests`, three cases.
Expected original: one new paused-stagger causal failure and two passing controls.

- Public BeginPossession with a registered nearby eligible victim. Public
  RequestScale(0), wait one rendered frame, verify zero deltaTime/requested speed
  and Held false,
  then call shipping LateUpdate. No new victim stagger may be applied, and the
  accepted possession remains active with unchanged XZ position.
- Live possession contact still applies a real nearby-player stagger.
- Pause then Resume without ending possession; the first live contact works.

Actual authored PetModel and the established Companion property seam are used.
Body movement, ability ticking, temporary brain and live pet updates are
disabled to isolate the actual contact callback. A primitive floor and real
registered CharacterMotors supply geometry/state. This is the public requested
clock/contact boundary, not actual PausePanel/device navigation or full skill
admission. Both hooks reset the world and restore provider/launch/network/
sandbox/stats state. No profiles, assets, art or kit direction are rewritten.

Fixture SHA256:
`3a5402b97c17cd497959e5bd33649d1374c4d13923848144d8bac39a8afe3ba9`.
Metadata SHA256:
`fe16c20b77ca37918acaa1f244ea3f7d085df47ecec128e8a21d8b5db6db355a`.
GUID: `54c28394acd54514aa50a6436b0807f6`.
Original Ghost canonical Git SHA256:
`b5ce4b8be39500f8474c1421a877ffc2ca6727a2fc2d6c53bc1a6c51be4c536d`.
Original working Ghost SHA256:
`b7b20a5b5dc0223bf1a91b0b43cad2470b949102bc65128b8fa8d6c9cae2a32d`.
Normalizing the unchanged working CRLF bytes gives the exact Git identity.
Reader stays at the separately qualified Kuro input fix, with no new edit.

Main owns worker inputs, native scheduling, exact XML and preservation audit.
Stop at fresh three-case XML and terminal receipt. A possession/physics/clock
precondition failure does not prove contact leakage. Keep fixture/metadata and
assertions unchanged between original and candidate. Product correction follows
only native causal proof and remains specific to contact at paused requested
speed; no extra animation or generic clock framework changes.

This does not qualify physical devices, UI operator routes, live peer timing,
all-map behavior, performance, visuals/audio or competition readiness.

## Original native evidence and pending correction

Main's `qa-d/Logs/kuro-pause-original3` shows actual IsStunned true after the
requested0/delta0/Heldfalse tick, against the no-new-stagger contract. Both live
contact and Resume controls pass. [Original XML](native-original/tests.xml) and
[receipt](native-original/job-receipt.json) retain exit2, terminal state,
preservation completed and no lease held. The guard ended07:29:16Z.
No fixture repair was required.

Candidate Ghost working SHA256:
`8ec3522fb16c2e8e8d47c0e9902fb6905b193c9a59c1c8dad3e0a3ace149d27f`.
Only the central contact resolver returns when requested game speed is zero.
Movement, accepted possession, resources, kit, appearance and timing are
unchanged. The helper is shared by local and host-remote contact routes; this
is source-reviewed route coverage, not an actual-peer result. Hitstop preserves
a positive requested speed through the existing clock implementation; its
preservation is source-reviewed, not a separate native control in these three
cases. The same fixture/metadata are used for candidate3.

## Candidate qualification

Main's `qa-d/Logs/kuro-pause-candidate3` passes all three unchanged cases on
the first native candidate. [Candidate XML](native-candidate/tests.xml) and
[receipt](native-candidate/job-receipt.json) retain exit0, terminal state,
preservation completed and no lease held. The guard ended07:33:12Z. No fixture
repair or native retry occurred. The owning gameplay rule now states requested
zero-speed contact freeze with possession/live Resume preserved.

Native scope is supplied-nearby/public-clock possession contact plus live and
Resume controls. Positive-request hitstop and both shared resolver callers are
source-reviewed preservation only. No actual PausePanel, physical device,
network peer, full kit, movement retune or authored art result is claimed.
