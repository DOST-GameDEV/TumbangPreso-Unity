# Live pose-history discontinuities

## Reproduction and correction

Catch reconstruction's live Track.Apply interpolated through a teleport although
retained RecordedPoseTrack playback already respected an epoch edge. A native test
recorded an actor at0, teleported it to4, then replayed the midpoint: the copy appeared
at1.99996185, a position the actor never occupied. Both live and retained playback
also missed offline teleports because recording used only network MovementEpoch.
A visibility change was applied halfway before its recorded timestamp.

Recording now derives a local discontinuity token from either the existing accepted
network epoch or the existing presentation teleport serial. It stays stable during
ordinary movement and repeated contact captures. No live actor, movement, ability,
score or network authority is changed. The serialized epoch field retains its sole
playback role as an equality token; packet layout and protocol remain unchanged.

Live playback now preserves the last recorded pose until the teleport edge, then
applies the new pose exactly. Normal fast body motion and fast slipper flight still
interpolate. Visibility changes at the actual sample time, matching retained clips.
This removes one source of false replay movement; it does not establish that the
separate visible tag-contact gap is fully corrected.

## Native acceptance

Unity6000.5.8f1 Linux64, guarded cloud-live-pose profile, real CharacterMotor and
Unity transform/copy components in an isolated fixture.

- Baseline:8 cases,3 passed and5 failed. Failures cover live network-epoch playback,
  live and retained offline teleports, contact-capture tokens and early visibility.
- Corrected:the same8 cases pass, plus the existing retained fast-motion/teleport/
  projectile case,9/9 total. No skipped cases, fixture repair or retry.
- Actual Teleport and AdoptMovementEpoch APIs are exercised; no fabricated body
  epoch fields. A fast4-metre move with no discontinuity still interpolates halfway.
- Frozen source hashes remain unchanged. Live actor positions are not modified by
  playback. No actual socket peers, full catch composition screenshots, character
  art judgement or whole-game/player-build qualification is claimed.

## Evidence hashes

- baseline.xml: SHA-256 72dee00bed9739e74f1c4b9a4f5736bc7e38f132d4097b5ac231bf22a628f4ec
- fixed.xml: SHA-256 08638901bef4db718ff293cd999e71b8226d32060ee5b701a449b71d30ba68fa
- baseline-inputs.json: SHA-256 8638f09524f1dc685e2fd22ad627569e93fcf09a909df6f56330c5653e484925
- fixed-inputs.json: SHA-256 7c89dc1e8294deb307aafbb65bf4f4e2fb41b583048f6644c8a4f7dcf02e137c
