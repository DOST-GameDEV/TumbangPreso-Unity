# Ambient life in TUMP

Current integrated source: street-animalstudy-v7/birdstudy-v5. Focused runtime
probev7 passed3/3 on all4maps, includes gait/contact, actual approach fleeing,
12staged bird responses and3surface-leg-lift interruptions. Six ground animals'
minimum drawn contact samples stayed within-.006 to+.0023m. Rare pauses retain
private randomness/long cooldowns; staging proves reactions, not distribution.
Smaller cat/longer dog native cast comparison and live front views reviewed.
Old study notes below preserve rejected history, not current deployment state.
Resident pots are three original .blend/.glb sources in roof-residents/study-v1,
with thicker segmented leaf volumes after rejecting the first diamond-like forms.
Roof author/import and placement review is in progress, not visually accepted yet.

**Newest owner model rejection:** dogs/cats look too similar, cats are WAY too
large, and neither looks cute enough. Treat current imported studies as rejected
art direction, not accepted finished models. Redesign the silhouettes: much smaller,
lower compact cats with short legs, broad feline face/ears and expressive graphic
features; friendly local aspins with different muzzle/ear/body proportions. Keep
TUMP's chunky style and do not merely recolor/scale the same shared box body.

**New occasional dog behavior:** rare brief urination/leg-lift at an appropriate
tree or surface, with long random gaps rather than frequent repetition. Interrupt
when a player approaches. No new interaction, reward or UI. Cats/dogs stay on the
three street maps. Add the appropriate distinct animation and inspect in context.

**Latest owner decision:** birds passing through scenery during flight is okay,
provided they fly away when someone gets close. Good motion and reliable proximity
response take priority; exact flight-obstacle avoidance is not a completion gate.
Ground-animal paths and believable perching still need appropriate placement.

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

Current map batch after pushed6ef38b02: pool ceramic/world-scale joints, softer
water and local swimmer wakes are implemented and authored. Focused normal-speed
water review1/1 passed both modes, Logs/roof-water-detail-v1. No full suite run.
Bird study-v3 exports exactly idle/peck/fly for3species. Street study-v4 has6models
with exactly idle/walk/run/alert. Source IDs and earlier rejected studies retained.
AmbientLife runtime and AmbientLifeAuthor now prepare real model/clip references,
private-random visits, ground routes/fleeing and checked bird approach paths.
Authorv1 is currently importing/placing; session48421. Do not claim animals
validated or finished. Next inspect author result, batch any corrections, then
focused in-engine model/path/animation review across maps. No subagents or Desktop
update. Latest owner focused-test policy remains mandatory.

Ambient importv1 hit the installed glTFast multi-primitive skin-buffer job conflict.
No package/Library/quality-setting workaround was applied. Reused the repository's
existing tools/normalize_gltf_skin_primitives.py, separating skinned primitive
children without changing geometry/materials/animation data. New source versions:
street-animals/study-v5 and birds/study-v4. Importv2 now running, session81551.
The first attempted binary comparison incorrectly compared two independent Blender
exports and failed. Same-input normalization proof now passes all9models: binary,
accessors, animation/skin/material data and original node transforms unchanged.
Proof: Logs/ambient-normalizer-proof/result.json. Ground route analysis now caches
clear neighbor edges and uses component sweeps; runtime animals no longer stop at
every tiny route sample, and bird fidgets use time intervals instead of per-frame
probability. All are unreviewed runtime integrations until current author and
focused in-engine review succeed. Keep maps first and focused-only validation.

Ambient reviewv1 failed at Eskinita aspin approach (0movement). Cause not yet
isolated; the probe now preserves actual player position and actor wait/panic/
heading/target state before asserting, so a missed proximity trigger is distinct
from broken fleeing. The image also exposed two concrete defects: every exported
material had default(.8,.8,.8) despite colored Blender viewport previews, and the
small route capsule admitted a dog's head into a tree. Source PBR nodes now carry
the real palettes (nine GLBs verified3+distinct colors), and routes validate the
full visible/turning envelope. New sources street-animals/study-v6, birds/study-v5.
Authorv4 running. No package changes or broad tests. Bird flight through scenery
is explicitly allowed; nearby-player fleeing remains required. Both grounded and
approaching birds now respond to proximity, with editor-only staged review to
verify that path without making shipped visits deterministic.

UI agent a21b23b was integrated as primary537984f6. Only BrandPreparationTests is
running now (Logs/ui-preparation-agent-v1.xml/.log). Agent continues other UI work
in isolated clone, no Editor. No broad suite. Parent maps/models remain dirty
outside that UI commit; do not mix their state into UI validation claims.
Animal source study-v7 now gives smaller compact cats and friendlier distinct
aspins, plus dog-only leg-lift clip. Authorv5 completed all4map placement. Runtime
rare surface pauses use long private-random cooldowns and cancel on proximity;
not yet behavior/visual-qualified. Default skin seating now measures the actual
neutral mesh instead of animation bounds that left dogs5-7cm high. Next focused
animal review should check smaller cats/front faces/contact and the rare pause.
