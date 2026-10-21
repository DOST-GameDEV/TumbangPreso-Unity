# Touch stick pointer lifetime

The actual patched candidate passed8/8 after original8 reproduced two pointer
causes and passed six controls. An earlier candidate-labelled job ran original
source because preparation and launch overlapped; it is preserved as an invalid
candidate attempt, not a product failure or candidate acceptance.

The stick accepted movement from every Down and Drag, and every Up zeroed its
movement. A second finger could therefore replace or stop the first finger's
steering while it remained down. The correction leases the first captured
(pointerId, button) until release and ignores foreign Down, Drag and Up.
Centering through ordinary Move retains ownership. Exact-zero public SetValue,
Release and the unchanged managed disable callback retire it; nonzero SetValue
keeps its clamp/knob override and the captured owner. Bind, coordinate conversion,
radius, unit-circle clamp, Rescale, knob behavior and the checked TouchButton
region are unchanged. Only21 additions/3 deletions inside TouchStick.

| Cohort | Actual runtime | Outcome | End UTC |
| --- | --- | --- | --- |
| Original8 | 792addf5… | Two causes, six controls | 15:06:05 |
| Invalid candidate attempt8 | Original792addf5… | Reran two causes/six controls; excluded from candidate acceptance | 15:14:04 |
| Actual candidate8 | 94ca1949… | Eight passing cases | 15:15:37 |

All three receipts ended October3,2026 terminal with preservation completed and
lease free. Original and invalid attempt exited2; patched candidate exited0.
The original and actual candidate have complete3396-file qualified-source maps;
Main verified those hashes unchanged. Logical source
9bcb0912eb5c94d58a91a63382982a6f8f8a62ef overlays older worker Git base
8e7cfc7feb4eee614d456347ccbb26ad962d1714. Their maps identify actual runtime and
fixture bytes, not merely the worker Git checkout. Local baseline669a19227
contains the preceding checked TouchButton unit.

Main launched the invalid attempt while preparation process92434 was still
hashing. The runner created the evidence folder first; preparation then failed
at mkdir before writing runtime. The diagnostic records source remaining original
and post-comparison of all3396 files against the original map. That invalid folder
has raw XML, receipt and preparation-failure.json, with no qualified-source.json:
none was generated before the preparation failure. No map is invented here.
One bounded scheduling repair completed preparation and confirmed94ca bytes before
launching the patched job. There was no fixture change, runtime-failure retry or
second repair. The preparation record and the actual acceptance remain separate.

Original wrong-vector distance was1.0 after foreign Down; foreign lift zeroed
nonzero steering (distance0.400000006). The eight cases manually invoke public
pointer handlers on a supplied bound RectTransform and assert Value,
TouchInput.Move and knob offset. The first case includes foreign Drag too because
the UI module can retain a drag handler after a void Down declines ownership.
Six controls cover owning drag through center, owning release, fresh ownership,
the managed disable callback, direct SetValue clamp/nonzero-owner continuation/
explicit zero, and normalization/clamping after public resizing. Nonzero initial
checks expose an unbound/zero-coordinate setup. There was no fixture failure or
native fixture repair. The foreign-Drag assertion was a pre-native refinement;
removing its two added lines exactly reproduces the priorbc400199 fixture. The
other seven cases and setup stayed unchanged.

The installed Input System UI module defaults to independent concurrent touches.
That shipping dispatch path is source-reviewed; native cases supply touch-like
Left pointer IDs. Tuple identity keeps other accepted mouse-button types available,
with mouse/pen behavior source-reviewed only. The customization view explicitly
disables/restores the stick; no global epoch or TouchInput framework was added.

The prepared Canvas and GraphicRaycaster are disabled. RectTransform conversion
runs as geometric math; no UI raycaster, input-module delivery, rendering or
physics scene is invoked. The disable control directly invokes the existing
managed shipping callback body. This qualifies public handler/vector/knob state,
not natural player lifecycle, physical phone, actual input routing, rendered art,
character travel, focus-loss/global-reset behavior or current player-build
acceptance. Main alone runs jobs; the agent made no worker/cache writes.

Byte references:

- Original runtime raw `792addf59ce60d2e1629b11b0ad070862c3d8e8ff3f15f27332ed30d8b636e8e`;
  LF `5496095c8ff8aafa61ef0ae14ea0b2082cf9b12992cf31057e7b8c14e82e538c`.
- Candidate raw `94ca19494df5617fd0d1cbe35ef371daef2413f9e01627679f80b87e18367d6c`;
  LF `30bcd67c859fa690755029df4e23f664169cbaa32df9469c61b68a7054482dd9`.
- Fixture `7dac7bd611fa6539830c6fde86699c1dd03126d1619c0ec113844d9ac03c705c`;
  unchanged meta `0d453901c725a1ccac82549ac6ca1cfdbacae97ce8c255b385f2e2d5a8f4389a`.

Nine raw evidence files are copied unchanged and preserved in Git by local
attributes. Held F6/Hop fixtures, device investigation notes and private work are
excluded from this unit's publication and native filter.
