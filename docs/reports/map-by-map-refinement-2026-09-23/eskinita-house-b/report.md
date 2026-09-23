# Eskinita stepped home,2026-09-23

Only Bahay_Rework_Bahay_4_W receives this fitted finish. The existing stepped
body, roof hue, deep frames, jalousies, downpipe, household arrangement and
collision remain. A warmer mineral-plaster lower story separates it from the
terra upper wall; broad restrained paint variation and a maintained lower band
replace the former uniformly treated facade. Metal sheet seams follow each
roof's slope. A small braced shade fits the measured1.35m chibi doorway below
the existing eave, inside the private lot. Embedded shrubs regain green: their
source palette had been recolored by the shared roof atlas.

The implementation copies only measured wall/roof/foliage triangles into a
map-local finish mesh, leaving original solid meshes and openings intact. Added
shade members share that mesh/material.1509vertices, one renderer, one1056x792
texture/material, no per-frame behavior and no new collision. Original house
geometry continues to cast shadows. Imported masters/global shaders/other maps
are unchanged. The full Eskinita author clears and regenerates this owned group.

Research and concept critique are in ../house-material-study.md. The image is
inspiration; it was not imported as a house texture. The source-created texture
has surface-specific projection and an explicit finish for this particular home.

Variant1passed geometry checks but was visually rejected: flat wall and roof
seams across the low roof's pitch. Its image/XML are retained. Variant2corrects
those issues, separates construction regions and adds the measured entrance
shade/foliage correction. The focused PlayMode case passed1/1in4.408s. Matched
street/frontage before/after,960x540and25percent greyscale were inspected.
No unchanged recheck or native build is needed for this local finish group.

The actual game lighting is retained. Viewmodel is hidden in the comparison so
it does not cover the lower wall. Existing poles/laundry/trees still occlude parts
of the house, and east-side lighting is still dark. This is one improved home,
not whole-map acceptance, every camera qualification or native performance proof.
Other primary houses need their own decisions, starting the grid-like timber
finish at5_W. All other map, sky, animal, bot, motion and final integration tasks
remain open. Named profiles/shared input were preserved; owned generated churn
was backed up in QUALLogs and restored. Original DEVPNGmetas remain excluded.
