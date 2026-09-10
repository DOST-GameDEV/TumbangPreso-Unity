# Recovery input and remote impacts

Quick Jump taps could disappear between the input and physics updates. The reader
now buffers an observed recovery press until the motor consumes it, once. Kuro
possession also no longer skips Jump, Skill1 or Ultimate input. Held buttons still
produce one edge, and Core's recovery floor and10Hz acceptance cap are unchanged.

Network state previously replaced the owner's predicted recovery progress with an
older host count. Protocol27 identifies each recovery episode and acknowledges
numbered requests. The owner reapplies only unacknowledged accepted taps to the
matching episode. New stuns discard old predictions; rejected and duplicate inputs
cannot buy recovery. This is state reconciliation, not a balance change.

Host-resolved victim impulses now travel reliably to the actual movement owner.
The message is host-authored, finite-checked and tied to the movement epoch.
Caster prediction and continuous fields keep their existing simulation ownership.
A separate reproduced handover race cached that a destroyed bot component still
existed. Caching its Unity reference instead of a boolean fixes the stale answer
without adding component searches per frame.

## Verification

- Core562/562, full EditMode486/486, Nemu/recovery PlayMode29/29.
- Hardware-event test: a short controller press fails before the fix. Keyboard and
  controller quick taps pass afterward, including exactly-once consumption. This
  synthesizes Input System device events; no physical controller was used.
- Deferred bot destruction regression fails before and passes after the cache fix.
- Old snapshots, acknowledged taps, repeated snapshots, new same-element stuns,
  trips, stale requests, replay and same-frame rate-limit refusal have regressions.
-67 named messages have matching payloads;75 numeric fields have finite guards.
- Three actual players, configured150ms each way and2% packet loss: before, owner
  recovery progress decreases once. After, all peers record five accepted presses,
  no progress reversals, and about1.57s for a4s unanswered ice hold.
- Discrete stomp: before, target displacement is0m despite real contact. After,
  all three peers agree on0.7281m and exactly the same final position, including the
  delayed-link run after the ownership fix.

Evidence: Logs/mash-input-before-v3.xml, mash-input-after.xml,
ownership-cache-before.xml, recovery-contracts-after.xml, recovery-editmode.xml;
Logs/mash-network-before, mash-network-after, remote-impact-before-v2,
remote-impact-after, remote-impact-owner-fixed. The impact evaluator now anchors
to each peer's observed charge transition, avoiding a local elapsed window that
sampled an already-moving owner as its baseline. Invalid original results remain
preserved separately. Initial hardware fixtures were also corrected for headless
Editor focus before being used as evidence.

Internal player: Logs/network-recovery-after/TumbangPreso.exe. Runtime DLL SHA256:
`c980e50e557191983235d125c15d8b9d18f5149a09b691089844e034381c3504`.
This is a local real-process verification build, not final Windows delivery.

## Remaining scope

Check the newly reachable possession controls in ordinary play and the pet art
rework, along with remaining kit/loadout binding, active-effect rejoin, rematch,
host-loss and both-mode matrices. Full release and human playtest approval are
not claimed. The owner has requested a cuter small Kuro and a more frightening
giant with better arms; that Blender work is in progress separately.
