# Baha rolled-wave scene result

The cinematic wave has a five-level rolled cross-section and matching narrow
crest, with a single triangle winding under Cull Off. The fixed far stilt houses
now fade with the sea in muted teal instead of abruptly appearing solid black.
The original3.4second body, held shoe, cameras and live gameplay are unchanged.

Final render-only1/1 passes the95vertex/432index wave contract, stage isolation,
unchanged gameplay/RNG, grounded body and zero shoe vertices inside head surfaces.
Final sampled clearance is -0.00329 to0.00009metres.29timestamped camera samples
were reviewed: side curvature reads as a rolling wave, front face stays broad
and readable, and the distant silhouettes no longer dominate the hero. The
front view still uses a simple translucent colour field; richer surface detail
is a future critique, not evidence of a new mechanics problem.

One bounded baseline fixture repair explicitly allowed the already-muted theme
for this visual study. It reports StartSound=False and never enables sound. The
final art change needed no additional repair. Inputs match and no new OOM.
This is not an accepted-cast freeze/handback test, fixed-frame-rate recording,
audio listening, target-device, actual-peer or human quality approval.
