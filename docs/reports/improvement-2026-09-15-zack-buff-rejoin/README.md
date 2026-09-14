# Zack joining charge windows

Status: scoped joining-window correction verified locally and in three actual
delayed-player runs. Baselines and raw corrected traces are preserved here.

The real held-Magnet observer restart lost the entire remaining charge/pole
window:0/92active/visual samples. The already-active Thunderstrike window likewise
lost0/54after rejoin. Host and owner retained each effect and correct resources.

The new hydration uses independent Magnet and Thunderstrike guards/clocks.
It never recalls a slipper or creates another lightning strike, and it does not
spend a skill charge or ultimate bank. Consuming an electric throw settles the
Magnet state only; it must not suppress an independently active ultimate window.
Real casts settle their own hydration guard so late joining records cannot
override them. Sean's already-qualified initial hydration remains supported.

HeldCharge now includes the separate ultimate remaining window, requiring
protocol35. Four focused PlayMode cases passed, including separate expiry,
consumed Magnet, independent ultimate and Sean stale-state protection. The
protocol assertion passed1/1 and an actual35host refused the old34client.

Corrected INTERNAL player: Builds/ZackBuffRejoinReview/TumbangPreso.exe.
Runtime SHA256:99958c1251710c8c7797c04ff70fd2bf086bfb3cf6f9c16b32d08bb32f0145e7.
Build log: Logs/zack-buff-corrected-build-v1.log, Result: Success.

| Returning peer and effect | Continuous live/visual samples | Initial hydration | Expiry offset from host |
| --- | --- | --- | --- |
| Observer, Magnet |89/89|46ms|+36ms|
| Observer, Thunderstrike tail |47/47|58ms|+17ms|
| Controlling owner, Magnet |74/74|332ms|+161ms|

Each run used three separate native players,150ms one-way owner-link delay,
the same returning profile, and no cast/resource/input reseeding on return.
Every peer retained spent resources and cleaned up its active flag and visual.
No returning observer replayed a lightning strike. Named profiles were restored.
Owner hydration is evaluated after the configured300ms request roundtrip plus
250ms setup allowance; it remains continuously correct afterward. Exact expiry
was additionally checked from the existing UTC-wall-clock traces, without
rerunning the players. These stricter results are expiry-review.json beside the
original reports; the maximum absolute peer expiry offset was184ms.

Critique: this restores existing approved art and does not establish new visual
or human-feel approval. Server/owner prediction still differs by roughly one
configured transport leg, rather than being frame-identical. Fresh-instance
joining is covered; same-process reconnect and pending-windup recovery remain
separate work. Native owner coverage here is Magnet, not a second owner-ultimate run.

This scope concerns an ultimate that has already struck and is still charging
throws. Reconstructing a not-yet-completed lightning windup is a separate pending
case; do not replay a completed strike or claim that preparation is covered.
Other timed buffs and world fields remain in the non-UI queue.
