# Replay animal binding and render failures

The ordinary Windows release at source `1570d5de6` could reopen its own completed
recording through the UI while repeatedly failing to draw animals. Its narrow
UI probe reported success because the timeline continued to advance. The actual
player log contains 1,389 `Recorded animal owner changed` exceptions. This package
is rejected and the qualified Desktop remains `725e28f13`.

Recorded sibling indices can change when the replay installer builds runtime
presentation children. The fix uses the recorded path first, then permits a
unique AmbientLife owner with the same authored name in the actual replay scene.
Missing or ambiguous owners fail. Existing model, geometry, material and texture
provenance checks remain in place. Rendering exceptions now pause the viewer and
show its error once. Package observation also checks that error after opening.

The native candidate passed the shifted-owner control, its ambiguity and altered
art rejection controls and both actual rendered animal/line and drone/mark
comparisons. Its fourth case reached the expected paused error state but failed
because the test incorrectly treated ordinary environment logs as unexpected.
The corrected test counts the specific replay warning and passed in a fresh
native run. All three production files match both tested input snapshots.
These four controls were qualified across two runs; the earlier failed fourth
case remains retained. All 21,411 inputs and preferences were restored.

The initial regression fixture failed compilation because it referenced internal
helpers. The corrected original run failed to reopen the player recording on
art content and its sibling fixture hit an unrelated round-start precondition.
These failures are retained. The candidate sibling control captures actual
ambient scenery without requiring an active gameplay round.

A separate editor/player art-fingerprint mismatch remains unresolved. The
stored fingerprint includes Material.ComputeCRC. Unity explicitly warns against
serializing those values, but that alone does not identify this mismatch's
cause. Mesh layout, shader and material content still require comparison.
[Unity's API contract](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Material.ComputeCRC.html).

The same current executable passed startup order, first Home playback, legacy
replay controls, decoded recorded effects and all seven map videos on first and
repeat visits. Its 191-window controlled route accepted 74 actions and 26 required
consequences. Two fresh custom one-round/30-second matches completed naturally
and saved ten scenery segments each without recorder warnings. These facts do
not qualify drawing those recordings, all-map fidelity or tournament readiness.
First Hero opening at 134.53 ms and preparation at 877.67 ms remain performance failures.

[Exact source and raw evidence hashes](evidence.json) preserve the compiler
failure, unrelated round-start failure, informational-log fixture mistake and
the fresh qualified controls. Package receipts remain scoped to their recorded
observations; the actual self-recorded replay package gate is failed.

Only the existing candidate folder may be replaced for a future build. No new
build was created during this investigation. Desktop installation is held until
the recording and material checks pass. Superseded build deletion remains tool
policy rejected; no space has been reclaimed.
