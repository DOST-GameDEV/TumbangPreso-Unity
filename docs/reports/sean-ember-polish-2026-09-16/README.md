# Sean's close-up explosion debris

The existing real-press capture, restricted to Sean's ultimate, reproduced the
previously noted roughness: large square ExplosionSpark debris formed a yellow
slab across the upper owner view near landing. The body view also showed large
cube chunks beside the otherwise retained fire bloom/heat footprint.

Fire-style debris now uses a cached small fractured-ember mesh and55percent of
the previous sampled scale. Other explosion styles retain their own debris.
Random draws, initial rotations, velocities, angular velocities, counts and
lifetimes are unchanged. The original model, hand geometry, cast/leap/landing
timing, blast core, shrapnel sheet and gameplay footprint are untouched.
VfxMaterial.Ghost already removes decorative colliders, so no collision fix was
needed. This is a small fire-specific visual correction, not a generic texture
applied across the kits.

Baseline real-press capture PASS1/1, guard7ed998ae3c35. An intermediate mesh had
inward winding, caught by a signed face-normal check and corrected before final
qualification. Final real-press capture PASS1/1, guard7d85f8a3a886. Owner/body
landing and recovery frames were inspected; the large foreground block is absent
in the reviewed final landing frame, with smaller chips in the body view.
Frames are selected by nearby game time, not claimed pixel-identical simulation.

Exact frame traces, coverage and XML are included with selected original images.
The complete local sequences are Logs/sean-ultimate-close-review-v1 and-v3.
Nativev37 predates this small source change. No extra native build, full suite,
model replacement or artistic-acceptance claim is made. Broader whole-kit,
counterplay, movement and owner play-feel review remain open.
