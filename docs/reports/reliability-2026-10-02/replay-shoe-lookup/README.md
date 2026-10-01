# Reuse live slipper inventory during replay trail capture

MatchPoseHistory samples at20Hz. RecordedTrail.Capture previously queried all
native slippers on every sample, despite an existing inventory already maintained
for bots. Its birth/destruction invalidation and live active/flight/seat reads
preserve the same input objects, including disabled Slipper components.

Only the capture enumeration changes to BotSlipperInventory.All. No new registry,
wire semantics, trail shape, colour, sampling or gameplay change. Birth/destruction
still trigger the existing native refresh; activity and state are never cached.

Actual native four-shoe capture and lifecycle case passes before and after1/1.
It checks trail ID/kind/width/head colour/head-to-tail points, inactive/reactivated
objects, changed origin, landing, destruction and replacement.1000captures each
produce4000trails. One Windows/D3D11 batch measures19.7619ms before and16.7017ms
after. This is a small isolated capture-cost observation, not a measured FPS gain
or full-match/network qualification.

The Mono allocated-bytes counter returned0 despite obvious array allocations.
It is unavailable for this comparison;0is not allocation-free evidence. No extra
benchmark run was made to chase it. The first fixture failed authored float-colour
equality because TrailRenderer stores8bitColor32. One bounded fixture repair uses
the authored colour converted to Color32; initial failure retained. Four frozen
input hashes unchanged; profile/shared input preferences restored; all jobs terminal.
