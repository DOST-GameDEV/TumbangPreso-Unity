# Forced slipper handover clears the old pickup lock

A recent real pickup followed by the round equipment pass could retain its 1.25-second throw lock. `SliceRunner.EquipOwnedSlippers` calls `Slipper.HostForceEquip`, but `Carrier.NotifyEquipped` returned immediately when the carrier already held that same shoe, before clearing the pickup delay. Forced round equipment is explicitly exempt from that delay.

`Carrier.NotifyEquipped` now accepts an optional `resetPickupLock` flag, default false. Only `Slipper.HostForceEquip` passes true, allowing its same-shoe handover to retire the old lock. Replicated held snapshots retain the default idempotent behavior. Manual pickup, retrieval rewards, Sean's callbacks, throw rules, lock duration and presentation are unchanged.

Three focused native EditMode cases were appended to the existing `SlipperHandoverTests` fixture. They make a real `HostGrab`, then compare the actual `SliceRunner.EquipOwnedSlippers` pass with a repeated held snapshot and repeated pickup notification.

- Original baseline: **2 controls passed / 1 causal failure**. The equipment pass left the lock at 1.25 seconds instead of zero.
- Candidate: **3/3 passed**. The forced handover clears the lock; duplicate snapshot and pickup notifications retain it. All three preserve the actual held-shoe/carrier relationship.
- Zero fixture/tool repairs or retries. No input-system fixture machinery, transport, SDK, hero callback or live service was used.

These three cases shared a serial guarded run with four independent wallet tests. The complete original XML files are retained: baseline 7 total / 5 passed / 2 failed; final 7/7 passed. Only three are claimed by this report. Joint native durations were 0.2482455 and 0.3040738 seconds; no performance conclusion is drawn from them.

Baseline source is `69e3414913c3bc582b9ce5b028239f65503e62d7`. Owned source changes are `Runtime/Carrier.cs`, the force-equip notification in `Runtime/Slipper.cs`, and the three cases in `Tests/SlipperHandoverTests.cs`, under `Assets/TumbangPreso`. Existing test metadata stayed unchanged. Exact original/candidate bytes are indexed in `inputs.json`; main and qualification candidate hashes matched after execution.

Qualification used `tump-feedback-0930`, profile `wallet-force-equip1002`, the serial CPU pool with 1536 MB job budget / 2048 MB reserve and a 450-second timeout. Baseline session 95238 used guard 17996; final session 63788 used guard 11300. Both jobs completed preservation and released their leases. The baseline preserved zero existing task-profile files; the final preserved one, and both preserved one shared Editor input preference. All 1,285 protected source/settings hashes remained unchanged. Both job-owned Editor processes exited.

Raw XML, guard/job receipts, run inputs and protection receipts are retained here; full Editor logs and frozen source copies remain under the checkout `Logs/wallet-force-equip1002` and main `Logs/force-equip-lock1002-inputs` folders. Private profile contents are excluded. This establishes the controlled authoritative handover and duplicate-state behavior, not real-peer timing, hardware input or a fresh player build containing this change.
