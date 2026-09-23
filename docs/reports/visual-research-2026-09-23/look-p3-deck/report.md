# P3 lagoon deck visual sampling

The saved gap concerned fine dark/dashed deck patterns at oblique player angles,
not physical deck support. Existing controlled captures were reused first. Material
detail added repeated divisions; outline reinforced fine seams; removing shadows
alone did not remove the pattern. The builder already batches boards by elevation
into thin renderers rather than combining them with tall piles.

The real deck has.38m board rows, while generic timber also drew.19m shader joints.
LagoonDeckPresentation opts only the47saved board/thin-deck renderers into filtered
single-plank grain. Existing geometric joints own the board structure. The outline
pass suppresses extra micro-step ink only when its whole sample footprint stays on
the local thin deck plane; structural/water height changes keep their edges. No
extra per-mesh mask pass, mesh/collider/material asset rewrite or old-map treatment.
WorldCueProfile.LagoonDeckDetail0 restores the original look.

## Evidence and limits

- Earlier validation/Logs/lagoon-deck-live-v3 five-way captures personally inspected;
  their archived receipt is under the full-backlog map evidence. No unchanged rerun.
- v2-world.xml: isolation1/1,7.252s,guard9c591eba146c. Current material, depth-edge
  and normal-edge comparisons, actual47renderer/two-material discovery.
- v3-world.xml: refinement1/1,9.655s,guarddae0f59a8c16. Actual deck coverage,
  unchanged mesh references/ground support, off restoration and960x540 comparisons.
  Full normal and25percent-grey before/after/comfort personally inspected.

Keep first refinement look. The extra synthetic grid is removed; real joints and
grain remain. Native motion aliasing/performance and full coherent Rafi/lagoon
qualification remain P7. No native build. The original v1probe assumed a generator
renderer count and failed on saved batching; it now checks actual28x26m main-deck
coverage. That failure is preserved, no production assertion was relaxed.
