# Rebuilt Ilalim preview camera isolation

Source de307b08a, Unity6000.5.8f1, Windows/D3D11 on RX6600. The actual
additive MapPreviewSurface route displays rebuilt Ilalim with supported shaders,
but generated scenery was created after the preview confined its scene.

Baseline1/1fails:33 renderers remained on default layer0. Sidewalk-only repair
removed32; the remaining BlockyClouds renderer still failed the same assertion.
Generated bodies, copied rigs, wear and props now inherit their parent layer.
The look and cloud roots inherit it too. Existing meshes, animation, sound,
materials, placement and gameplay are unchanged. No per-frame hierarchy sweep.

Final same case1/1passes:2433active,1127in-frustum,0unsupported/missing shaders,
0escaped renderers. Native actual PNG inspected. Four owned code/test input
hashes unchanged; this is not a new player, all-map, FPS or tournament claim.
Profiles and shared input preferences restored by the guarded runner.

Initial import was interrupted by the owner closing the PC and had no result.
A UTF-8 setup error launched an unchanged candidate; that launch was stopped,
its guard restored profiles, and one bounded tooling repair applied the fix.
The sidewalk-only failure is retained, not described as a pass. Final log is
Logs/ilalim-readiness1002/final-all.log in the isolated native project.

The new map image still frames the viaduct/shop roof rather than the playing
court. That framing is a separate open defect; this layer repair does not
claim to correct it. No authored scene, lighting or asset redesign was made.
