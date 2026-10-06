# Continuous running under delayed accepted movement echoes

The owner reported smooth rendering with movement delay and rubber-banding for
non-host players. The protocol150 owner-echo correction already addresses one
specific cause. This check extends its earlier single-packet evidence through
continuous motor movement without changing production networking.

The native fixture runs the actual CharacterMotor FixedUpdate and
CharacterController for 100 steps at 0.020 seconds. It runs forward with sprint
intent and delivers 75 actual protocol152 SyncUnit payloads to the real receiver,
using the owner position from 25 steps earlier. That models a half-second echo
round trip at the receiver. Match, round, epoch and increasing pose serial remain
valid. Packet size and authoritative stamina receipts are asserted.

With only the accepted-owner early return removed, the original reconciliation
path rewinds movement by up to 2.5000 metres. Native 9496/session 89632 terminates
with the expected rollback assertion failure. The unchanged current correction
passes the identical fixture: native 13268/session 25542 records zero rollback.
Both runs perform about 10.001 metres of cumulative controller travel. This is
cumulative forward travel, not final displacement after a rewind.

The final fixture also asserts that every packet advances the actual receiver
serial. Native 17912/session 75138 passes that final qualification with 75 serial receipts
and zero rollback. This final additional assertion is stronger than the first
comparison; its movement and rollback assertions are unchanged.

## Scope

These are native motor and actual receiver checks with synthetic scheduled
packets. The loop explicitly advances motor steps; it does not delay socket
traffic or simulate a remote host. It does not qualify host movement budgets,
real transport, every skill, keyboard/controller hardware, frame pacing or human
feel. Normal two-machine controlled-latency acceptance remains open.

Original explicit correction, new-epoch teleport and observer controls retain
their earlier causal evidence. The production motor and wire are unchanged in
this unit. The added regression protects continuous running through the existing
receiver rather than adding a new gameplay or diagnostic framework.

All three jobs are terminal. Each 21,136 frozen source inputs, shared input and
Editor preferences, QualitySettings and isolated profile seed are restored. The 216 known native metadata deltas are saved locally and restored byte-exactly.
The unrelated Auditor change and private model script remain untouched.
No player build, temporary server, browser tab or new branch was created.

[Raw evidence checksums](SHA256SUMS.json) include the expected failure, both
passing receipts, final fixture and exact original-path comparison.
