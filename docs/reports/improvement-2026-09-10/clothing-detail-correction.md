# Remove misplaced clothing details

The owner identified the added blue blocks on Cheska's hands and raised clothing
plates as defects. The first-pass exporter had added generic cuff fasteners,
pockets, button blocks, towel blocks and apron plates. Their placement and equally
heavy outlines made them read as pasted-on pieces. Those additions are removed
from all eighteen playable bodies; original costume geometry remains. The exporter
cannot recreate them. Animation sampler data is checked unchanged per actor.

The current [eighteen-person sheet](style-correction/current-18-clean-clothes.png)
was rendered from actual Unity imports and inspected. Roster generation succeeded
with 20 retained people, 6 cans and 10 slippers, including matching arm regeneration.
`Logs/clean-clothing-contracts-v3.xml` reports 14/14 passing outline, arm-geometry
and motion tests. This is corrective cleanup, not a finished individual cast pass.

The later Berto body experiments were rejected and restored before this batch.
Their oversized torso enclosed shoulder pivots and made shortened arms disappear.
The current body is the original blocky source with the rejected additions removed.
Both failed authoring paths are quarantined in Logs and recorded in the active
ledger. Direct Blender front/side inspection now precedes further body work.
