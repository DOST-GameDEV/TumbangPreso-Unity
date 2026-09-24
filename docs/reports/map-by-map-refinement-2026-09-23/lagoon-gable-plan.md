# Lagoon gabled-thatch family,2026-09-24

Native house0and the saved built-in lagoon-gable-v1study establish the problem:
the plain gold roof reads as clean timber slabs. Keep real existing rafters, roof
pitch, openings, porch, residents, deck, piles and all collision. Four targets are
ThatchGable homes0,8,12,16; other house/roof families remain separate decisions.

Accepted reference cues: distinguish golden thatch from dark timber rafters, fibres
run down the actual roof slopes, modest uneven eave ends and restrained ridge ties.
Reject excessive individual reed geometry, a thick piled roof, changed character
proportions or a whole-village recolour. Wall plank geometry already reads as boards;
do not add another universal fake seam layer.

Implementation: derive only the selected houses' roof UVs/materials. A local tiling
texture provides broad bundle variation plus filtered lengthwise fibre, oriented
from actual face normals/down-slope axes at metre scale. Preserve source material
references and all non-roof slots. Add a small supported eave fringe and ridge ties
with deterministic per-house variation. Keep simplified native geometry, no new
collider or roof silhouette replacement. Do not run the whole Lagoon builder.

Measured source sizes (house width/depth, before0.9m roof overhang):0=5.4/4.5,
8=4.6/4.2,12=4.2/4.0,16=4.2/3.7. Eaves=2.15+(id%3)*0.12local; ridge rise1.24.
Use current mesh/material/name guards rather than assuming old data still matches.

One native paired close/roof-angle view of home0and detached8, plus actual preview
and25percent grey, should resolve this material/construction choice. Small author
checks preserve source topology/other materials/collision. No unrelated test work.
Afterward continue other roof/screen/repair families, boat/water/island/sky work.
The newly shipped fall recovery and flying birds remain intact.
