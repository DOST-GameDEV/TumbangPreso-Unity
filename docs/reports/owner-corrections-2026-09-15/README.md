# Owner corrections: match length, settings and sampayan

Based on ASTRAReworks416e48a9, Unity6000.5.8f1 on the resumed matth PC.
This batch follows the owner's latest corrections while the larger UI/gameplay
queue continues. The approved original login remains unchanged.

## Eight rounds

Classic's source default was still four, which produced the reported1/4 display.
Both normal modes now default to eight rounds and each seat defends twice. Default
tournament rules and menu copy follow the same policy. Explicit custom lengths
remain valid; this does not change player count or protocol37.

A one-time settings revision updates only the exact former untouched Classic
preset. Other saved custom rules and later deliberate four-round choices remain
intact. The old preset did not record whether a user had explicitly picked the
same values, so an old choice identical to that preset is also upgraded once.

Focused .NET tests:42 match/default/tournament/rotation cases passed, plus one
profile distance-per-round case. Settings migration: three EditMode cases passed
in owner-corrections-edit-v1. Both live HUD cases passed in the combined PlayMode
run; the actual Classic screenshot now shows Round1/8.

## Dark settings

Settings has a local charcoal/grey palette, warm-white text and a restrained
yellow-green focus accent. Rows, options, switches, inputs, scrollbars and the
unsaved-changes dialog use that palette. The global theme and login are unchanged.

All four focused settings contracts passed: pages/frame-pacing states,
controller/touch return and Cancel, binding Save/Discard, and nested live pause
Escape. Page captures cover ten representative PC sizes from960x540 through4K,
including16:10,4:3 and ultrawide. These are in-engine layout/render checks, not
certification of every physical display or input device.

## Four supported clotheslines

The existing rope ends measured6mm from the actual pole trunk surfaces. There was
no large misplaced endpoint in the saved scene; the attachment lacked a readable
tie. The author now adds wraps, crossed hitches and short loose tails around
measured supports, keeping the rope below the electrical conductors.

Three additional original Blender sets create one more long street line and two
shorter front-yard lines. The four lines contain23 garments in total, including
shirts, shorts, trousers, dresses, towels and a sheet. Pegs and ropes stay fixed;
the cloth billows below its pinned top. Source files are under
MapSource/environment/resident-laundry.

Two laundry-only author runs produced identical semantic geometry, SHA256:
D0C8DBC09E277AC3C429A8FF7703AF5BA2F9C025CFA034517A9427C4F628AC97.
The scene diff changes only the replaced laundry objects and the neighbourhood's
child list. Existing house, street and utility-pole blocks remain identical.

Seven placement/import checks passed, including four distinct lines, eight hitches,
and yard-post ground/facade clearance. The first PlayMode run finished6/7: all HUD
and settings cases passed, but the laundry test attempted to read Unity's combined
static meshes after their CPU data was released. The test now checks fixed parts'
world transforms and rendered bounds; production batching remains unchanged.
The targeted laundry rerun passed1/1. All23 garments moved below their pinned tops,
and all fixed parts kept their transforms and rendered bounds. Four actualFPP
captures were inspected: the second street line adds depth, distinct garment
silhouettes read against the sky, and yard posts sit behind the boundary with
visible paving contact. Close views show pegs and the rope entering its hitch.
The small ties naturally become subtle at street distance. Existing near-camera
dither on fence/pole rendering and the deferred Inday arms are outside this change.

## Preservation and remaining work

Guarded runs restore and hash-check profile files and Editor input preferences.
Restoration receipts:0625811c08db,c020cfbe64c9,d38e5e43b0e0,131fbce1b6fd,
febe345036f5,fdcb2f89224a. No player build or remote social/account action was needed.

This report does not close the full project queue. Results/round-break/chat/training
and the remaining UI/native qualification are next, followed by the saved gameplay
queue, deferred Inday FPP work and the seventh hero/map last.
