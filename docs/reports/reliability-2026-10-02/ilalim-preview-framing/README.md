# Rebuilt Ilalim preview framing

The actual rebuilt-map preview still used the old35degree yaw and13.5m
camera height. Its native image showed the viaduct roof and adjacent shop roof,
obscuring the under-viaduct playing street. The preceding [capture](../ilalim-preview-scope/final.png)
is the baseline; no duplicate unchanged baseline launch.

Only the Ilalim registry camera entry changes:0degree yaw and5.5mheight,
below the authored8msoffit. Distance22m, field of view and lobby distance/height
remain. The shared yaw also faces lobby shots down the street. No authored scene,
geometry, lighting, shaders, animation, models or audio changed.

Existing actual additive preview case1/1passes on Unity6000.5.8f1/Windows/D3D11.
Fresh native PNG visually inspected: the road between the viaduct supports,
sidewalks and surrounding shops are visible instead of the deck above them.
2433active/1096in-frustum renderers,0missing/unsupported shaders,0layer escapes.
Five owned input hashes unchanged; named profile/shared input preferences restored.
No tooling repair for this unit. Logs/ilalim-readiness1002/framing.log is retained
in the isolated native project. This is a map-preview correction, not a new
player, full lobby journey, all-map/performance or tournament-readiness claim.
