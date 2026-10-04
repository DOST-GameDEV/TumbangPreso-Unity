# Replay visibility recovery

RecordedWorldView hid live effects, UI, lights and ambient objects before entering
the camera-render try/finally. A playback preparation fault skipped that cleanup;
Halftime's fallback disposed the view while live objects remained hidden.

The fix extends the existing try/finally over the live-visibility preparation.
It preserves replay/camera design, scores, kit mechanics and private artwork.

Native Unity6000.5.8f1 PlayMode with actual D3D11 on laptop gamergmae: a real
CombatVerbs catch retained a clip; a ready RecordedWorldView played that clip.
The supplied world-owner loss makes actual Draw throw, followed by the same Dispose
path as Halftime fallback. Original: one causal failure (live renderer remained
hidden), one ordinary draw/dispose control pass. Same candidate: two passes,
zero skips; renderer, canvas and light restore, previously hidden art stays hidden.
This is controlled lifecycle fault injection, not a physical LAN disconnect test.

Both runs freeze19224 inputs at13069614c8b92d8b3d4779d93b976336e9c6b8fe;
maps differ only in RecordedWorldView.cs. Runtime and fixture remained unchanged
during each run. Generated importer metadata and ProjectAuditor deltas are
recorded separately, preserved before/after and restored to exact frozen bytes.
QualitySettings and nine existing product EditorPrefs are restored. Original
PID12316 exited2; candidate's exact PID/exit is in the terminal raw record.

The first fixture's physical throw never toppled its can in either case, so it
failed before Draw. Those results are preserved and do not establish a game bug.
One fixture repair uses the existing deterministic catch path; production was
unchanged between that repaired original and its control. No acceptance from
compilation alone, no RAM admission or external-timeout wrapper.

Full slow transfer/playback, physical interruption, all maps/heroes and operator
PRACTICE-BOT-RESUME-1002 acceptance remain open. Earlier packages lack this fix.
