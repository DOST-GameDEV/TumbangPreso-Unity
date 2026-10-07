# Replay scene state and shader fidelity, October 7

Two concrete omissions are addressed: local replays previously had no saved traffic
poses or traffic-light material state, and authored animated shaders used live time
while replay time was paused or rewound. New optional, independently hashed scene
sidecars retain the actual BGC traffic samples and signal colour/emission slots.
The viewer applies them only during rendering and restores the live transforms,
visibility and property blocks, including after exceptions. It never steps road
clocks, routes, hits or activation. Exact route-wrap edges jump to the saved
next position without interpolating through the court.

Fourteen authored shaders now select recorded time inside the synchronous replay
render scope. The default branch remains the original Unity shader clock. No
hero design, art, kit, palette or gameplay source is retuned. Paused playback and
forward/backward seeking apply to shader animation as well as recorded poses.
An exact adapter-equivalence rule keeps existing recorded preview fingerprints
valid; changing the adapter's ordinary-time fallback still invalidates footage.
No receipt, movie or poster was rewritten.

## Evidence

The frozen native worker baseline is913134715 plus the explicit packet overlays,
not the worker's Git label. checked-source.json lists exact tested/publisher raw
hashes, launch source and all run classifications. Native7 is4/4: production CPU
clip time agrees with independently read GPU time; authored water rewind has
mean RGB difference0 at256x256; pause, forward/back seeking and live restoration
pass; all14 ordinary original/candidate default-material shader comparisons have
RGB difference0 at128x128 and no compilation errors; all7 actual preview receipts
remain valid while a changed live fallback is detected; every segment of a
retained natural30-second Custom capture has a readable complete scene window.

Native5's five passing cases are reused: actual Kanto save/reload on a fresh map,
scoped pose/exception/visibility restoration, a naturally completed Custom match
with full segmented capture and actual controls, and existing legacy playback.
Native6's wrap/signal-slot test passes on unchanged final runtime source. Native
parent receipts confirm all21357 final inputs,13 original Editor preferences and
all four existing profile files restored after terminal Unity. Fixtures and
comparison images are retained. The final job used graphics/D3D11.

## Original failures and limits

Native1's earlier compiler preparation failed due to a wrong KantoTraffic
namespace; it was corrected before the successful run. Native3 ran zero tests
because its filter used the wrong test namespace; that run is not acceptance.
Native4 reproduces the original shader rewind pixel failure (RGB0.358688354).
Native5 retained a first manual-render candidate comparison failure (RGB1.52464294).
Native6/7 independently read the GPU clock before comparison; the final check also
asserts that the CPU production clock agrees. Cold first-render behavior is not
established by this primed comparison. These failures are preserved, not relabelled.

This is partial visual qualification, not a1:1 claim for the whole game. The
shader tests use representative default-material fixtures, not every ability or
material variant. ArenaFx, Arena drones, ambient animals, crowd response and water
wake state still need recorded timelines and actual same-camera/time comparisons.
Older recordings cannot gain scene data that was never saved. Long sessions,
packaged playback on this exact new source, full-resolution game visual comparisons
and real peer recording remain open. The currently installed PC Desktop725e build
excludes this unit until the PC owner integrates and qualifies it.

Unity's documented shader creation API is used only in Editor verification:
[ShaderUtil.CreateShaderAsset](https://docs.unity3d.com/cn/2023.1/ScriptReference/ShaderUtil.CreateShaderAsset.html).
