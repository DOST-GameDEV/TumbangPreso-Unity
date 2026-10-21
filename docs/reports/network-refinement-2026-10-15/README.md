# Network refinement, October 15, 2026

Base: `26def0cd9`, branch `ASTRAReworks`, Unity `6000.5.8f1`, protocol160.

The latest movement/possession integration exposed three bounded defects:

- A crouched owner still submitted the raw sprint key, so the host drained a bar that the local motor kept full. Suppress the sprint request while actually crouched, retaining crouch/slide presentation bits and resuming the held request on standing.
- Remote host stamina ignored the existing curse-reach sprint restriction. Apply the same restriction as the local motor, preserving the held request for the end of the reach.
- `SyncUnit` reserved308 bytes, but the possession clock plus a valid61-byte aim ID needs309. Share the206-byte fixed prefix and aim bounds between allocation and minimum-read checks. The packet layout remains protocol160.

## Evidence

- Core:742 passed,0 failed,0 skipped. The first sparse-checkout run had740 passes and2 failures from omitted `ugs/cloud-code` fixtures; adding the tracked `ugs` directory resolved both without code changes. The retained TRX digest and counters are in `results.json`.
- Original owner feedback suite:0/6, because its fixture omitted `PossessedLeft`. Both manual body packet fixtures now carry that field. Their acceptance and serial assertions remain active.
- Reproduction:7 passes and4 failures. Three are the product defects above: actual `SendUnitPose` buffer overflow, crouch sprint flag still4, and the host spending20 stamina during a blocked reach. The fourth is a new rejection test hitting the helper's unconditional acceptance assertion. Its helper now explicitly asserts the expected unchanged serial for refused packets.
- Candidate:20/21 pass. This includes the actual maximum309-byte sender reaching the local NGO named-message callback; possession finite/range/stale/clear handling; owner corrections, movement epochs and75 delayed accepted echoes; voodoo state; resource leases; remote interaction; aim tells; and the seven existing Nemu possession checks.
- The candidate's one failure was the crouch test's standing control: its body started inside the helper floor, so the real headroom check correctly refused standing. Place the fixture above the floor and assert successful standing. The focused final rerun passes1/1. Production bytes are unchanged between these candidate runs; all21 distinct checks have passing evidence. No failing result is discarded.
- Unity compiled the project for these native runs. Existing obsolete-API warnings remain. Guarded launches restored the named profile's existing files and all3 shared Editor input preferences. Import-generated resource/settings bytes were preserved locally and restored exactly from the base commit.

See the four raw XML files and `results.json` for exact cases, failures and final source hashes.

## Limits and remaining work

This is single-Editor logic/receiver coverage and a real local host named-message loopback. It does not qualify a remote client, separate devices, Relay/WAN, packet loss, full matches, physical input or overall competition readiness. No player build or Desktop replacement was performed. Existing hop-recovery and remote crouch-capsule gaps remain separate work.

The legacy source scanners were also inspected: `audit_wire_payloads.py` reports9 extraction mismatches, including helper/partial-method paths; `audit_wire_finite.py` reports3 legacy `TimedKit` fields whose finite checks occur in `ReadFeatherfallSnapshot`. These pre-existing scanner outputs are not claimed as passing tests or evidence of nine/three newly reproduced product faults.
