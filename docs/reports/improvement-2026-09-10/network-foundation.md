# Familiar replication and network receive recovery

This is a verified repair batch within the broader unfinished improvement task.
It is not a final Windows release or a claim that every network path is fixed.

## Material repairs

- Controlled Kuro now has a 20 Hz replaceable pose stream, seat/finite/floor/court/
  swept-collision validation and a time-accrued distance budget. Accepted echoes
  do not rewind the owner. Reliable casts carry the final accepted anchor.
- Accepted teleports establish a new movement epoch. Older movement cannot undo
  recall or blink. Predicted teleports retain local response while awaiting the
  host; a denial returns the authoritative pose. Corrections preserve owner look.
- Delayed abilities keep their accepted release pose/aim through windup. Cast
  requests capture their source before a local teleport changes the motor.
- Familiar effect snapshots reconstruct remaining life and actual pull, without
  spending charge or recalling Nemu. Roster binding precedes live state. Identical
  roster updates preserve models/animation/Kuro instead of deleting them.
- Remote model alignment no longer stores a temporary smoothing offset as its
  permanent base. Teleports align the rendered and physical bodies consistently.
- Variable-length roster/identity/loadout messages allocate their actual encoded
  string sizes and use reliable fragmentation. A real three-player room first
  exposed the old 512-byte roster overflow.
- Explicit command-line profiles isolate settings/career/social files. Ordinary
  launches retain the existing default paths and IDs. Existing profile files are
  backed up and restored around verification runs.
- State announcements validate the host sender. Linked-worktree builds resolve
  commondir refs and stamp the actual commit instead of no-sha.
- Unity Transport 6.5.0 is embedded with its original notices and a minimal receive
  buffer repair. Failed/empty/invalid receives return their acquired buffers.
  Upstream code exhausted the receive pool and stopped accepting new traffic.
  See Packages/com.unity.transport/TUMP_PATCH.md for version/hash and exact scope.

## Actual evidence

These are independent Windows processes on this PC, using loopback and a shaped
UDP link. They do not substitute for final ordinary play, physical venue Wi-Fi or
Relay qualification after the remaining game changes.

| Case | Result |
|---|---|
| Real local UDP: 2048 empty datagrams then legitimate connection, upstream | FAILED to accept the connection |
| Identical UDP regression after buffer repair | PASSED |
| Three-process clean controlled recall | Common 6.04 m flight; identical recorded destination |
| Three-process clean ultimate | Identical actual field center; 46 ms expiry spread |
| Ultimate,150 ms each way plus2% configured loss | Identical center; 14.5 ms expiry spread |
| Correctly seated delayed-owner recall, same impaired link | 6.0908 m flight; identical destination, validated against last controlled pet position |
| Hard-restart owning profile during a live ultimate, direct connection, v6 | Correct seat reclaimed; field rebuilt at(1.1712,-1.5592); resource counts retained; remaining peer stays connected |
| Settled remote model/motor alignment in v6 |0 recorded horizontal error |

The earlier delayed v3 row accidentally shaped the observing connection because
connection timing reversed the two clients' seats. It is invalid for delayed-owner
coverage and was repeated as v3b after adding a seat gate. Earlier direct reconnect
rows are retained as failed evidence. The foreign-disconnect-callback hypothesis
was disproved; it was not patched around.

The v6 runtime assembly SHA256 is
f9a972cfd93e926f6f5f048b43efbe3637c7d736e6ea69447d477bd76d7bc460.
Unity launchers share a hash across builds, so the runtime hash is recorded too.
V6 predates a small temporary-AI cleanup and the final test-assertion timing fix.

Full current EditMode 486/486 and Core 562/562 pass. Plugin-driven Nemu contracts
report 20/20; the current batch NUnit XML also passes 20/20. All eight checks and all fourteen
gating source audits pass on this batch.
Audio audit has seven informational flags; no listening approval is claimed.

## Still open

Remote human one-shot impact delivery, same-hero alternate loadout binding, other
kits' live-effect recovery, broader round/rematch/disconnect matrices and final
candidate requalification. Continue all hero/loadout/animation/map work afterward.
The official Unity plugin is connected for live-editor work; runtime control stays
disabled in normal builds by the Pipeline configuration default.

The embedded vendor files retain upstream whitespace and metadata; project-authored
changes pass the scoped diff whitespace check. Vendor source was not reformatted.
