# Possessed movement across input producer withdrawal

Date: 2026-10-03. Baseline `4e356179c`. Eight-case fixture is authored and
reviewed. Original native gate reproduced five actual movement failures and
passed all three controls. The first narrow reader candidate passes all eight
unchanged native cases. Main verified exact worker/primary reader and fixture
hashes, terminal state and preservation before approving publication.

## Source mechanism and contract

The possessed familiar receives a separate cached movement vector through
`GhostPetCompanion.SetPlayerInput`. `UpdatePossession` prefers that cache once
`_hasPlayerInput` is true. Reader chat/loading return, focus loss and reader
disable clear ordinary intent/touch state but do not retire this cache. The
companion branch also reads raw movement while the body's Intent is parked.
The familiar can therefore continue moving after its producer withdraws.

Withdrawal must publish zero movement while preserving the accepted possession,
its duration, kit direction, body AI, art and resources. A stale reader that no
longer owns this network seat must not clear its familiar cache. Keep the same
existing local/offline custody guard. `SetPlayerInput(Vector2.zero)` retains an
explicit zero cache; resetting `_hasPlayerInput` would instead let body AI intent
become the familiar's movement fallback and would not satisfy this contract.

The ordinary pause-movement suspicion was rejected before implementation:
LateUpdate passes scaled `Time.deltaTime` to UpdatePossession. Its separate
unscaled follow/fidget fallback does not advance possessed movement. Possible
dt0 contact resolution is a separate lead, outside this input cache fixture.

## Frozen original gate

Run exactly `TumbangPreso.PlayTests.KuroInputProducerLifetimeTests`, eight cases.
Expected original: five causal failures and three passing controls.

- Chat, public loading curtain, reader disable, focus false and parked intent
  each retire possessed movement, without ending possession.
- Ordinary touch movement, fresh movement after chat and obsolete other-slot
  reader cache preservation remain valid.

The fixture uses the actual authored PetModel, the established
CharacterVisual.Companion property seam and public BeginPossession. Actual
TouchInput feeds the shipping reader, then the shipping UpdatePossession
movement executes against a primitive floor with a supplied 0.05 s step. Both
pre-retirement movement and post-withdrawal XZ travel are measured; failure
cannot be hidden by only inspecting a cache field. The cached vector and active
possession are additionally checked. Body movement, ability ticking, temporary
AI and live pet updates are disabled to isolate the reader consumer. This is
public possession/reader acceptance, not a full skill-cast or autonomous AI test.

Both hooks reset the world and restore provider, touch, typing, launch, network,
sandbox and stats state. Only the fixture's loading curtain is cancelled. No
model, mesh, animation, SFX, profile or private art is authored or replaced.

Fixture SHA256:
`65a1ee1e1183e88a3db845a4613ecbf54477b066b14b418d8d385655cd9762aa`.
Metadata SHA256:
`381c5cd3b0da7add6325fe88233509811032e06e991de26763a0cb6846569432`.
GUID: `2f329283d5da463cb00e71f0f5c67a34`.

Exact Git original hashes in `4e356179c`: Reader
`8491f40782dbbcf8c38817f0d014597f4a134f10a814f41ddcd4f950cb4f036b`,
GhostPetCompanion `b5ce4b8be39500f8474c1421a877ffc2ca6727a2fc2d6c53bc1a6c51be4c536d`.
Working originals: Reader `23ded6eb3a6e88b1b3e0c5a7b08dc9e3f75f8f4421629abbef98b5d97f37c169`,
Ghost `b7b20a5b5dc0223bf1a91b0b43cad2470b949102bc65128b8fa8d6c9cae2a32d`.
Each working copy normalizes CRLF to its exact Git byte identity.

Stop at fresh exact eight-case XML and terminal guard receipt. Readiness,
physics-floor or possession setup failures are not input-retirement proof.
Keep the fixture/metadata/geometry/assertions unchanged between original and
candidate. Use the existing guarded reader cancellation and cache setter after
native cause proof, with no EndPossession or ordinary pause-movement change.
No physical focus/menu navigation, hardware, peer transport, all-map, timing,
performance, authored-visual or competition-readiness claim is made.

## Original native evidence and pending correction

Main's `qa-d/Logs/kuro-reader-original8` produced fresh eight-case XML. Each
chat, loading, focus, disable and parked case moved 0.391 m horizontally after
withdrawal, against a 0.0001 m bound. Ordinary movement, fresh movement after
chat and obsolete other-slot cache preservation pass. [Original XML](native-original/tests.xml)
and [receipt](native-original/job-receipt.json) retain exit2, terminal state,
preservation completed and no lease held. No fixture repair was needed.

Candidate Reader working SHA256:
`a3733e7cade7c5e965a5f05c870a79d2d458181f13ababb8dfeb166646c2e7c2`.
The companion branch supplies zero raw movement when intent is parked. Existing
local/offline pending-input cancellation calls the existing cache setter with
zero for an active possession, retaining `_hasPlayerInput` true. The accepted
possession stays active; body AI fallback cannot become familiar input.
GhostPetCompanion source, kit/resources/contact/time/net-pose code and authored
assets are unchanged. The same fixture/metadata are used in candidate8.

## Candidate qualification

Main's `qa-d/Logs/kuro-reader-candidate8` passes all eight unchanged cases on
the first candidate, including ordinary and fresh movement, stale-reader
custody and preserved active possession. [Candidate XML](native-candidate/tests.xml)
and [receipt](native-candidate/job-receipt.json) record exit0, terminal state,
preservation completed and no lease held. The guard ended07:17:12Z. No fixture
repair or native retry occurred. The owning input rule states cached movement
retirement and local custody without ending possession.

This qualifies actual authored-pet/public-possession movement driven through
the reader and supplied contexts/callbacks. It does not prove actual body-AI
planning, full skill admission/timers, physical focus/device navigation, live
peer ownership transitions or a new player. Retaining an explicit zero rather
than switching to body-AI fallback is additionally source-reviewed through the
existing setter's implementation. No GhostPetCompanion source or authored
hero behavior was redesigned.
