# Current Windows demo-loop checkpoint

Nativev24 passes the real startup/Guest, final main menu, settings/controller map,
both-mode loadout, pause/resume, custom-match results, Classic rematch/leave and
Hero Strike return routes. Both normal defaults are asserted as8rounds before
explicitly selecting short custom rehearsals. The fixture uses actual raycast/UI
callbacks and ReadyGate, not direct result-screen invocation.

This binary includes the finalized owner art/motion, recovery menu-input fix and
client round authority fix. Its matched two-player delayed rejoin evidence is in
../client-round-boundary-2026-09-16/. Build path:
Builds/round-boundary-v24/TumbangPreso.exe. Runtime SHA256:
e08238cfe7b6d8880e4c55c0ab56ba98a66630d36af9f974240f72f3fb79f0fc.
Production/diagnostic source was published in ec2cae4a. Desktop build untouched.

The native runner exited0 with reviewPassed=true, sharedInputUnchanged=true and
no pre-existing files in the isolated demo-loop-v24 profile. PID12576 retired.
No other task-owned Editor or player ran during these frame windows. At1366x768
on this PC's Ryzen52600/RX6600, Classic averaged171.86FPS,p99 16.67ms,max36.66ms;
Hero Strike averaged191.02FPS,p99 9.97ms,max40.01ms. Each window is about28seconds
of offline bot play. These samples are not a worst-case overlap/performance matrix,
physical controller certification, venue LAN proof or coverage of every player verb.

Selected actual screenshots and exact result/viewport receipts are preserved here.
The broader demo acceptance checklist remains open, including direct verbs,
whole-kit counterplay and the owner's play-feel/art review. This is a preserved
working checkpoint, not a claim that the entire project is finished.
