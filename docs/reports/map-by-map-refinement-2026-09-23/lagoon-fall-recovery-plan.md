# Lagoon platform fall and mash recovery, owner2026-09-24

Request: copy SaBubong's mechanic so players can fall off Lagoon and button-mash
back up. This is a new gameplay requirement alongside the preserved map art queue.

Use the established SaBubong sequence: real descent, return prone to the player's
safe spawn, then the existing press-gated get-up. Reuse its animation, camera,
recovery readout, keyboard/controller/touch input and bot tap path. The shared late
anti-stranding guard stays; holding a button is still only one press. Both modes
and every character use the same physical-fall rule, including stun-immune heroes.

Lagoon currently only triggers that recovery when out of bounds or trapped below
a deck, so ordinary edge jumps simply become swimming. Track genuine falls from
dry supported positions: a drop of at least0.75m arms the fall; reaching the water
swim-entry height finishes it. Clear tracking on grounded support, teleports/respawns
and round changes. Intentional access via the existing water stairs remains usable;
walking down supported steps is different from an airborne platform fall. This
preserves the authored swimming routes while implementing the requested edge risk.

On a genuine fall, held stock enters Lagoon's existing eight-second lost-slipper
return. Existing loose floating stock and under-deck/out-of-bounds failsafes retain
their behavior. No score award, new death system, protocol field or recovery HUD.
LagoonWater remains host-authoritative; existing teleport/control snapshots and
recovery-episode requests carry the result. No map collider or model changes.

Focused qualification: real outer-rail jump in each mode, visible descent and
mash state, held-input single press, repeated taps regain control, delayed stock
return; retain existing real inner/outer stairs check. Use one bounded native
sequence if needed for presentation. Real multi-peer/physical-device qualification
remains in final integration, not a claim from local simulation. After this task,
add requested flying birds if absent, then resume the saved gable/other art work.
