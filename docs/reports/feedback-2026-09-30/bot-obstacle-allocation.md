# Bot Obstacle Query, 2026-09-30

ShoveRouteIsClear now reuses32ray hits instead of creating a RaycastAll result
array on ordinary queries. A full buffer falls back to the complete old query,
so ignored bodies cannot conceal the real wall beyond them. Existing body,
slipper, can and trigger filters, probe height and decisions are unchanged.
No tag-commitment, kit, loading, geometry or human combat behavior was altered.

Two native behavior cases pass for clear/body/trigger/wall/zero-length routes and
a dense40-body scene with a wall beyond the hit buffer. The initial three-case
run also printed zero through GC.GetAllocatedBytesForCurrentThread. Calibration
then showed that counter printed zero for a deliberate4096-byte allocation even
on this Windows Editor. That initial numeric claim is not accepted.

A bounded replacement uses [Unity's current-thread GC.Alloc recorder](https://docs.unity.com/en-us/engine/6000.5/script-reference/unity/profiling/profilerrecorderoptions/collectonlyoncurrentthread).
It observes the deliberate allocation, then records zero allocation events across
100 warmed bot queries. That changed case passes1/1 in0.1367135s. The same calibrated
method now checks100support queries plus dense highest-floor fallback, passing1/1
in0.1625355s. Unchanged behavior cases were not repeated. No whole-player FPS/hitch
claim follows from these isolated measurements.

- [Initial behavior/unreliable counter](checks/ai-obstacles.xml)
- [Counter calibration failure](checks/ai-obstacles-calibrated.xml)
- [Calibrated native allocation recording](checks/ai-obstacles-recorder.xml)
- [Ground-query allocation/fallback check](checks/ground-query-recorder.xml)
- [Frozen inputs](checks/ai-obstacles-inputs.json)

The prior ground-query numeric claim in the landing-circle report is corrected;
its original native flight/support evidence remains valid.
