# Live ice on reconnect, 2026-09-15

Current status: the targeted local and actual delayed reconnect checks pass.
This batch follows ced13a20; broader non-ice state coverage remains open.

## Reproduction

The protocol32 internal player reproduced missing ice on a real returning
observer. The host still had18sheet samples and79wall samples after the observer
was ready; the observer recorded139samples with neither field. Owner/host had
the expected live ice and resources. The same observer profile was restarted,
not replaced by a helper pretending to receive a snapshot. RawCSV/results are
in improvement-2026-09-14/ice-rejoin-evidence/baseline.

## Implementation under qualification

HostSyncPeer now sends the complete active ice set to the synchronizing peer.
IceBegin/IceItem/IceEnd carry a bounded batch with round, scene, generation and
timing context. All use the same ReliableSequenced pipeline as SyncWorld and
PlayAbility, so a newer cast cannot overtake a partial snapshot and be erased by
its completion. The installed NGO source confirms that default delivery choice.
Small item packets avoid depending on a different fragmented pipeline.

Only a complete validated batch can replace the local ice set. Incomplete,
duplicate, stale, wrong-scene and malformed data do not partially erase it.
Generation tracking resets when a new messaging-manager instance is registered.
Protocol33 is required because older clients cannot reconstruct these active
collision and traction fields.

Snapshots preserve position, direction, full/remaining lifetime, ownership,
sheet radius/drag/slipperiness and barricade span/thickness/split form. Restored
frost starts at its existing visual age, with no cast sound or fresh formation.
No hero activation, charge spend, score or player reset is replayed. Expired
records are skipped. Deactivating an old sheet releases its occupants immediately,
before deferred destruction.

The batch is limited to256fields. This covers the normal charged ice budget;
unbounded debug/sandbox field flooding is not qualification. Other hero fields,
active buffs and longer match lifecycle cases are still separate remaining work.

## Evidence so far

- Protocol assertion1/1passed; wire audit71named messages,0layout mismatches.
- Three targeted Play cases passed: lifetime/shape/resource preservation and
  mature frost; invalid/expired state and unrelated scenery preservation; the
  existing ice traction/overlap case with immediate deactivation cleanup.
- Three batch-assembly cases passed: duplicate/stale/incomplete records cannot
  complete the batch, malformed/bounded data is rejected, and an empty completed
  set can finish only once.
- The INTERNAL33player built successfully. Runtime SHA256:
  cacf88c25a636cd20dd9cecd2a5ab1a81954c24c350ca85da7330e4d216ec95b.
  Builds/IceRejoinReview/TumbangPreso.exe contains this implementation.
- The same actual observer reconnect at150ms one-way owner delay now passes.
  Host/owner/returned-observer recorded392/402/154samples. The returned observer
  sees both fields at matching positions, retains the spent resources and sees
  both expire. Host still had31sheet/95wall samples after the observer was ready.
  Shared-clock expiry spread was about191-193ms under the delayed link.
- Actual33host/32client testing produced the explicit version refusal. Raw
  baseline/corrected traces, results and focused XML are retained in the evidence
  folder. Named profiles and shared Editor input settings were restored by guards.

The ice restore is qualified for this scope, not all field types, physical
devices, packet loss or cross-platform play. The remaining non-UI queue continues.
Two Inday source-arm assets received build-generated whitespace changes only;
their content was compared, backed up and restored. No deferred Inday edit was made.

## Next independent source finding

SyncAbility currently transmits cooldowns, charge counts and ultimate bank only.
It does not transmit active ability duration or charge flags. Beyond the already
specialized familiar/Coven/sky paths, inspect Dante Carapace, Nemu Veil, Sean
Ignition and Zack Magnet/Thunderstrike-tail after reconnect. Reproduce missing
state before adding restore-only callbacks; do not replay impacts, casts or spend
resources. This follow-up is not implemented by the ice snapshot.
