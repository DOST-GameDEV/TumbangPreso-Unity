# Protected PCExpress sign fitting, 2026-09-24

Read-only inspection found an actual aspect mismatch: supplied source14107x3729
is3.7830517565:1, but the existing importer rounds NPOT dimensions and the sign
face is3.15x.7875, i.e.4:1. This slightly widens the logo. The supplied artwork,
master importer and other uses are protected; no redesign/repaint/generation.

Use a byte-identical local copy with NPOT disabled and2048import ceiling. Fit this
map's physical face/backing to source dimensions, not rounded imported dimensions.
Keep material family, placement, room/retail equipment and collision. Actual Unity
source-import API derives ratio, so no magic aspect constant. Existing other sign
art has identical source/import aspect and stays visually unchanged.

One focused native front/street before-after, source byte equality and world aspect
check,25percent grey. Stop after actual inspection; no new visual variants. The
repair source is already publishedb32ccf428and QUAL preserved/advanced. This is the
only active unit before Load. All remaining older tasks survive.
