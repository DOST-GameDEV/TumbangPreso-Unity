# Map-vote packet validation

## Reproduced issue

A seated guest's zero-, one- or three-byte SelectMapVote payload raised a native
FastBufferReader OverflowException. Truncated host tally packets also threw.
Trailing data was accepted, an invalid map entry partially replaced the visible
ballot, and a sender ID above Int32.MaxValue could wrap into an existing peer ID.
QueueVoteState likewise accepted an extra trailing byte. These are handler-level
reproductions with real Unity Netcode readers, not a claim about observed malicious
traffic or transport exploitation in a shipped match.

## Correction

The three map-vote handlers now validate their existing wire lengths before reads
or mutation. Guest votes require exactly one integer and a representable sender;
map indexes retain the current map-catalogue range. Tallies validate count, exact
remaining size and every vote before applying the complete ballot. Queue state
requires the existing exact28-byte payload. Host/guest authority and authenticated
seat resolution remain in place. No wire layout, gameplay semantics, abilities,
map assets, input or loading behavior changed; protocol95 is unchanged.

## Native evidence

Unity6000.5.8f1, Linux64, graphics enabled with Mesa llvmpipe, guarded isolated
cloud-map-vote profile. The complete integrated candidate includes the prior
published tutorial and Nemu units unchanged.

- Corrected baseline fixture:13 cases,3 passed and10 failed. Exact valid packets
  passed; malformed sizes, partial mutation and sender narrowing failed.
- Corrected runtime:all13 packet cases passed with identical assertions.
- Four previously authored but unrun rematch/intermission cases passed4/4 on this
  candidate: fresh rematch identity, current membership and local acknowledgement,
  seated guest buffer votes, and observer tally without authoritative round events.
- Total corrected run:17/17 passed, none skipped. Frozen inputs unchanged.

One bounded fixture repair initialized the hidden result board's ballot to NoVote,
matching its normal Show lifecycle. The first run's empty-board default assertion
was a fixture error; the original13-case result (2passed/11failed) is retained.
No production behavior or failure assertion was weakened. No actual sockets,
ranked/LAN/Relay game, reconnect, physical-device or full-player-build qualification
is claimed. Existing passed compiler/native cases were not rerun broadly.

## Acceptance

Deliver malformed sizes or invalid entries to the actual handlers: no exception
and no changed ballot. Deliver an exact valid guest vote: only its authenticated
seat changes. Deliver an exact valid host tally/queue state: all valid entries
apply. Oversized sender IDs cannot alias an admitted peer.

## Evidence hashes

- baseline.xml: SHA-256 55edd8fcdb98e499ceb70774dfe17e3fac7cf9c9d270b03bd71480a80c2608ea
- baseline-retry.xml: SHA-256 7d1be360c66479136d629eb48490922c1679ccf5d6ffa452be4dca034b90db8c
- fixed.xml: SHA-256 376c466fa22e379c17f21e8366ae49d6a166106bf3344513c51962d98b87b14c
- baseline-inputs.json: SHA-256 8cc6bb58241cdead5a1bfe8aad3d97f5cce21904aedf16476f6e24eaa78d7a5a
- baseline-retry-inputs.json: SHA-256 f62936ea8d0bd5b8b07fd6eef724858cb4e5f8ebc8c637dae8b4411d5473d40e
- fixed-inputs.json: SHA-256 dd794ad885016a7fd0ce0400333582a9b2d0b4b445a80b7ea27ec7596fb0498a
