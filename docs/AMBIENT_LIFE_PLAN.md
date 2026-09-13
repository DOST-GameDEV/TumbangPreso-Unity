# Ambient life in TUMP

Owner-directed map scope,2026-09-13. MAPS remain first, then full UI, then whole
animation/skills/play-feel work. All animals must belong to the approved cute
chunky game style, with broad colours and simple graphic features. They need
actual movement/idle/flee animations and must be inspected in Unity beside the
cast, at gameplay scale, from multiple views. No realistic/sculpted anatomy,
no noisy voxel reconstructions and no fixed public schedule.

## Bird life

Original six-bone Blender drafts: Maya, kalapati and fantail, currently only in
MapSource/environment/ambient-life/birds/study-v1 and tools/author_roof_birds.py
+(original renders also in Logs/roof-birds-v1). They are not integrated yet.
A first critique found oversized dark beaks and boot-like feet; revise before
Unity acceptance. Use differences in proportions, colours, tails and flight.
Study-v2 now reduces bill/feet and separates cheek marks. It remains a draft;
wings still need a more convincing folded silhouette and in-engine motion review.
Reference: https://birdwatch.ph/2013/07/03/10-most-common-urban-birds/ documents
maya/tree sparrows and Maria Capra/pied fantails as familiar urban birds.

Birds can arrive at the roof AND the actual court, perch, peck, turn, then leave
or take flight when players approach. Vary chance, intervals, visit duration,
group size, species, destinations and small gestures using a nondeterministically
seeded PRIVATE cosmetic generator. Do not consume UnityEngine.Random or game RNG.
No physics collision, scoring, prompts or pet-care system. Keep the can/ring and
loose slippers unobscured. Freeze animation with game time for paused/quality
comparison captures. Bound the population and flight paths; no flying through
building meshes, guideway or props. Roof arrivals come through clear overhead
space, with paths and perch heights validated from actual geometry.

## Cats and dogs

Original first rigged studies now exist under
MapSource/environment/ambient-life/street-animals/study-v1, authored by
tools/author_street_animals.py. Three aspin coat/ear/tail/build combinations and
three cat studies, nine-bone rigs, idle/walk pose renders. NOT in Unity yet.
First critique: cat face/muzzle is too dog-like; square rump, angular tail bends
and boot-like feet need refinement. Compare at actual scale beside the cast before
acceptance. Do not call these finished models or live ambient animals.
Study-v2 separates the cat's short cheek/nose silhouette from the dog's muzzle,
uses tapered curved tails and makes coat patches flush. Tabby and tan-aspin idle
renders were inspected. They are cleaner and distinguish the animals better,
but are still unaccepted studies pending in-engine scale, gait/contact and cast
comparison. None is imported, spawning, pathing or fleeing in the game yet.

Place these in Eskinita/Bayan/Ilalim, not the roof. Use recognizable local aspin/
askal mixed-dog forms and domestic cat variations. Vary builds, ears, tails and
coats (not only material colour); keep them cute and stylistically consistent.
Idle, look, groom/tail movement, walk and run-away sequences need weight and ground
contact. Retreat paths must lead to believable yards, shade, shop margins or side
passages. Validate against real props/walls; no drifting through scenery or
vanishing in clear view. Avoid blocking the player's retrieval lanes or suggesting
an interaction the game does not support.

Eskinita: household cats and occasional aspin by a yard, small maya visits.
Bayan: restrained pigeons near garden edges, a cat/dog at a shaded margin.
Ilalim: a shop cat or resting aspin in a sheltered pocket, no random animals
standing under traffic in the road. Keep audible/visual activity subordinate to
combat tells and the street-game loop.

## Condo identity

Use plausible resident elements: household plants in pots/reused paint pails,
monobloc seating, the washing/drying corner and selective original painted
community drawings. The condo remains modern. Do not add ornamental clutter,
copied sacred motifs, fake interactive slippers or a sign on every surface.
The enlarged pool is accessible and supports actual swimming/body+FPP animation;
it is not fenced off or a solid blue block. Full outer railing allows all-side
falls with existing controls, mash get-up and10s off-roof stock return.

## Validation

Verify source rigs/scale/poses, scene support and clear routes, ordinary-speed
owner and observer views, unpredictable sparse visits and flee behavior. Check
that random cosmetics leave gameplay sequences unchanged, that can/slippers/tells
stay legible, and that cost remains bounded on Low/Balanced/High. A rendered study
is a draft; actual Unity appearance and movement decide acceptance.

Study-v4 now contains six native models with exactly four exported actions each:
idle, walk, run, alert. The GLB animation-channel check confirmed4x27channels per
file. V3 accidentally accumulated compatible actions across models; it is kept
as history and must not be imported. V4 clears the preceding file's actions.
Walking uses stance/swing timing; running uses a different grouped-foot cadence.
They remain source studies, not in-game behavior or approved artistic output.
