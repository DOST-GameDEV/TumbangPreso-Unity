# Zack Quick Circuit: single-cut presentation

The remaining heroes must reach Phaister/Paete/Amihan's presentation standard.
This is one motion unit, not whole-Zack completion.

## Change and critique

The shipping0.64s clip retained two alternating skate pushes from the old2.5s
Bolt Sprint. Current Quick Circuit is a2m lateral cut with a0.15s tell. Native
avatar-backed baseline poses and the original author table confirm the mismatch.
The replacement gathers, braces once at0.15s and settles without a second cycle.
Body X/Z travel stays with the motor. The first draft's feet crossed inward;
review corrected their roll to an open stance. Owner hands now gather and release
once on matching beats, with modest offsets keeping the held shoe inside the lens.

Only hero-zack-sprint is replaced in the GLB.37 other clips,38 total, geometry,
rig/material/scene data and original binary prefix are preserved. No mechanics,
audio, camera shake, networking or protected reference-hero changes.

## Native checks

Unity6000.5.8f1, isolated complete project, llvmpipe/OpenGL. Named profiles and
Editor/Quality settings restored. Native component/editor2/2 checks pass, including
real first-person lens/shoe visibility and shipping body release/recovery. Ten
existing QuickCircuitTests pass. Actual accepted input, shipping CharacterAnimator,
left/right physical cuts and recovery pass1/1 in3.4142078s at07:30UTC. Final frozen
inputs unchanged, exit0, no new OOM event. This is13 distinct scoped cases, not a
full suite. Source baseline824f30b1 plus this unit.

The first live fixture omitted normal aim-facing input; its actor turned and
moved0.36m along worldZ. That assertion failure remains. Supplying FaceAimPoint,
as ordinary aimed input does, fixes the fixture without relaxing bounds or
changing gameplay. Both directions then pass. Controlled-stage real-time images
and timestamps produced an11.53s silent review with normal repeats and labelled
half speed. Owner images are static composition checks, not a live owner film.

Earlier preview-only attempts are not acceptance: direct SampleAnimation did not
correctly evaluate the rendered rig; an Editor-only test selected as PlayMode ran
zero cases; a compile-heavy avatar-backed preview stalled and required stopping
its exact Editor after an ignored graceful request. Fresh cached playback worked.

Full-court/player/peer, SFX listening and the rest of Zack's presentation remain
open. This pass changes how the cut reads, not its balance or timing.
