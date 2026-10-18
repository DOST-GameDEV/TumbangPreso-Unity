# Recorded crowd response and water wakes

New local scene sidecars retain actual active-map Arena crowd clock, cheer, groan
and wave shader globals, and RoofPoolWater swimmer vectors/wake strength. These
are rendering samples only: playback does not trigger crowd events, swimming,
map clocks, route/hit logic or gameplay. Per-render scopes restore original
uniforms and property blocks even when rendering fails. Existing optional v1
sidecars remain readable. Malformed correctly hashed vector arrays are rejected.

Water targets are cached per scene/name, refreshing destroyed targets. Native
warm Arena capture averaged0.092973ms for100 samples on this laptop; this is a
short capture-path measurement, not full match performance or zero allocations.
Live map/shader assets and finalized hero designs are unchanged.

## Native evidence and original failure

Native8 failed compilation because Unity6.5 rejects implicit int/SceneHandle
conversion. The repaired cache uses Scene identity/name and invalid-target refresh.
Zero cases in that original run are not acceptance. All original output is retained.

Native9:2PASS actual Arena/rooftop capture and scoped seek/exception restoration.
Native10:1PASS actual authored-water wake saved/reloaded through the asynchronous
writer, same-camera/time original/replay RGB difference0 at256x256, visible changed
wake control0.5476583, live pixels restored, malformed arrays rejected, and legacy
v1 sidecars without the new fields read/applied.
Native11:1PASS actual Arena CrowdChunk/atlas/shader saved/reloaded response, original
and replay RGB difference0 at512x512, visible changed response control1.38296,
original globals/live pixels restored. Captured images were inspected.

The crowd comparison isolates one actual crowd bank with a temporary culling layer
and controlled shader response values. It does not claim a natural crowd event,
the entire Arena frame or full-resolution packaged footage. Fixed shader time in
the wake comparison isolates swimmer data from the already qualified time adapter.
Parent receipts confirm all21361 final inputs,13 original Editor preferences and
four existing profile files restored. checked-source.json identifies exact frozen
packet source bytes; publication proof compares them with normalized Git text.

## Remaining fidelity

ArenaFx, drones, ambient wildlife and other transient scene/material state still
need recording and matching camera/time checks. Full scene/ability variants,
long sessions, packaged integration, device and peer recordings remain open.
This unit extends the checked traffic/shader-time work; it does not establish
1:1 parity for every visual in every match. Keep a compatible tournament build
with saved recordings. Older files cannot gain state that was never recorded.
