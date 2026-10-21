# Lagoon flying birds,2026-09-24

Lagoon now has two existing kalapati models and one maya flying above the village
and surrounding water. They use an opt-in aerial mode in the existing AmbientLife
actor: independent start times, newly chosen curved flight legs, banked turns, and
short glides between wingbeats. The authored fly clip's spread-wing phase supplies
the glide pose. Private cosmetic randomness does not affect gameplay or peers.
Motion pauses with scaled time; no new models, colliders, score or network state.
Other maps remain on their existing ground/perched-bird paths. The normal builder
includes the Lagoon-only bird author, so this survives regeneration.

First native check passed1/1in18.246s; flap/glide/world frames were inspected. The
world-scale range was too subtle, so v2 brings one kalapati and maya nearer/lower
to the village while retaining the second kalapati's farther water flights. Native
model size/palette remain unchanged. Revised check passed1/1in19.378s: each actor
travels more than8m, wing motion exceeds15degrees, configured altitude/area bounds
hold, actual flap/glide states occur and pause stops movement. Native close poses,
world scale and25percent grey were inspected; keepv2.

Birds remain small ambient details, not foreground characters or a dense flock.
Flight starts were staged visible for inspection; shipped starts keep independent
private delays. Screenshots are world-scale and pose evidence, not a full ordinary-
play/performance certificate. Matched full-map visibility and integrated performance
remain in Lagoon's final gate. No repeated whole-game/old-animal runs were used.

Broader all-map natural animal behavior REFINE-2.7 remains open. Resume the saved
Lagoon gabled-thatch/other material, boat, water and island work next. The owner's
all-map building-texture requirement and older TODOs remain assigned.
