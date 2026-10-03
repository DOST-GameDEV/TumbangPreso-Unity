# Recorded camera frame composition

Playback now presents the recorded camera RGB as an opaque frame through the
existing UI/OpaqueCameraFrame shader. Residual camera texture alpha no longer
reveals or mixes the present-time view underneath. RawImage tint/UI opacity
still controls deliberate fades. The view owns and disposes its frame material.
No hero art, shots, audio, gameplay, protocol or shader design changes.

## Failure and checks

The native GPU fixture constructs the actual RecordedWorldView in the Classic
Eskinita scene, then supplies controlled blue camera-frame textures over a red
underlying view. The original has two causal failures: alpha0 gives blue0 and
alpha0.25 gives blue0.53725493. The opaque-input control passes. This tests the
real frame consumer with controlled input, not a complete retained gameplay film.

The first candidate passes3/3. All three full frames are blue1/red0/alpha1.
Deliberate half UI opacity still blends red0.733/blue0.737 with composed alpha1;
owned material disposal is checked after the frame ends. Source alpha and fixture
remain identical between original and candidate; no repair or repeated run.
Both jobs exit normally through their guard (original behavioral exit2,
candidate0), restore isolated preferences and release their leases.

## Source and limits

Logical source368618be9f8caac641db47b72ceb70608578d52d, with only the minimal
RecordedWorldView candidate overlay. Candidate working SHA256:
fae995d51bb4bc98d37c39ae27fd20d495bb54a1ebf8655af30c3531967dd74f.
Fixture1a863edd889662856c132ddd891015e17c8c199a5f9c911e81c0246cf847986e.
Both3336-file code/editor/test/shader/package/settings manifests were checked
unchanged after execution. Worker Gitbase8e7 is older; the declared source overlay
and isolated validation company/product are recorded in each manifest. Cached
Classic scene art supplies constructor bindings, not a whole-current-art audit.

The reused shader is already integrated by2450f0f5b. This evidence does not
qualify the frozen1003g protocol134 player, new peer/recovery behavior, complete
replay footage, physical input or player first-use/frame performance. A matching
rebuilt player and operator acceptance remain separate. No speculative capture
exception or preload change was shipped.
