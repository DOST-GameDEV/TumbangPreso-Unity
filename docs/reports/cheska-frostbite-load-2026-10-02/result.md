# Frostbite loaded-shoe cue

Source baseline: 9374de2d8e18fbaab404073f1eff5948fc2a03fd on ASTRAReworks.
The five focused lifecycle checks passed in isolated native graphics runs.
Publication identity is recorded separately after the final visual check.

## Established defect

The repaired minimal real-shoe baseline ran two native PlayMode tests at
13:12:13 UTC on October 2. The inactive/material control passed. The loaded
case failed specifically because no Frostbite surface cue existed. Before that,
the original fixture failed because it omitted the real model setup required
by Slipper. That fixture error was repaired once using the production model
setup order; the earlier failure is not a product defect.

## Local implementation

Three unequal angular frost seams overlay the exact authored shoe mesh using
an owned material. World and owner-held copies share the existing load clock.
The base mesh/materials, load duration, Frozen behavior, ownership, protocol,
body clips and sound remain unchanged. Five tests cover the inactive control,
load/drop/regrab/consumption, expiry/reset, delayed equipment hydration and
owner mesh/material preservation. Assertions do not substitute for results.

## Retained failed batch attempt

The guarded native graphics launch began at 13:19:43 UTC. It hit the existing
memory guard at 45 seconds (about 8 GiB total, 6.86 GiB anonymous/shared memory).
Only the owned editor and worker were signalled. Exit 255, no result XML and
no retained frames. The guard finished profile preservation and released its
lease. Shutdown then logged native faults; this is not a passing run or proof
of a gameplay defect. This batch supplied no visual evidence.

The owner then explicitly requested a RAM workaround and splitting the tests.
All five unchanged cases were run one at a time in fresh Editors, retaining
the same graphics backend, assertions and memory guard. No parallel job, raised
limit or PC fallback. All five passed, each with fresh one-test XML, exit 0,
profile preservation and no held lease. Sampled total-memory peaks were
6.236 to 7.242 GiB, versus the batch reaching 8 GiB.

The first owner image was obscured by the fixture floor because the real
viewmodel is camera-local. One capture repair isolates the target shoe subtree
on the camera mask and restores layers afterwards. The owner test passed again;
its unobscured image exposed an overly regular chevron appearance. The shader
was refined to narrower, unequal branched fractures. The final world and owner
visual checks each passed in separate 30-second guarded runs. Their frames were
reviewed: frost follows the real mesh with no detached halo, and the consumed
world render returns byte-for-byte to the inactive frame. Other lifecycle
evidence is reused because its runtime inputs and assertions are unchanged.
These are isolated shoe renders, not a full player-camera composition review.

## Other checks and limits

Focused native lifecycle results are scoped evidence, not the full PlayMode
gate. The test partition entry is present exactly once; the
repository-wide partition plan still rejects pre-existing unassigned fixtures
outside this lane. Its complete output is retained, not repaired here.

Evidence and exact candidate input hashes are in evidence/. Full editor logs
remain with the private validation run. No actual-peer, full-map, hardware,
body-animation, listening or human-taste claim follows from these checks.
