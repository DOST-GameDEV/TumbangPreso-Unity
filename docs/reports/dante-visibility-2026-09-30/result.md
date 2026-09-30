# Dante barrier visibility result

## Implemented

The actual authored barrier keeps its geometry, stone/gold palette and existing
rise. Its private materials use50percent alpha with backface culling, avoiding
doubled front/back opacity. Source/shared materials remain unchanged. Per-cast
clones are owned and released through VfxRenderTag. GameBuilder includes the new
shader so the next player build retains it. No ability mechanics, timings,
reflection rules, names, descriptions, audio or other hero presentation changed.

## Native evidence

Unity6000.5.8f1, Linux graphics, isolated profiles and guarded runs.

- Baseline1case fails: changing a red/green target behind the actual barrier has
  zero visible response from either side.
- Initial half-alpha candidate passes its numerical check but visual review finds
  dark stone. Shader reassignment lost its vector-array palette. Rejected.
- Corrected candidate2/2passes6.52seconds: target response is0.3708/0.4571 of
  the unobstructed encoded-RGB response. These are visibility measurements, not
  physical transmittance or an assertion that output RGB equals50percent.
  Material alpha is exactly0.5; all16palette slots match the original, original
  alpha stays1, no colliders added and owned materials retire after destruction.
- One bounded capture correction starts the round before placement. The same
  court case passes again8.03seconds, not a third distinct acceptance case.
- No memory guard stop or new OOM kill. Raw XML and run receipts are in checks.

## Visual critique and limits

The corrected actual-court observer capture shows Dante's body and street/chalk
through the stepped slabs while stone edges and gold seams remain recognizable.
Overlapping edge pieces are naturally darker; no additional shell or flash was
added. The controlled views from both sides confirm the central face now reveals
its background. The fixed owner-eye capture is still inside the visible character
head, so it is retained as an unsuccessful capture and not called FPP acceptance.
The existing one-repair budget is exhausted; do not churn that fixture further.
A later coherent player check should inspect the actual first-person camera.

The current player predates this source. No new player, actual-peer, hardware
performance or human final-approval claim is made. Wiki ability reconciliation and
the separate shield-logo request remain separate work.
