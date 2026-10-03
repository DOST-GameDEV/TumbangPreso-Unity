# Observed scope

Owner clarification at 17:06 Manila: every hero's ultimate shows an afterimage
of the last gameplay frame over the animation. This directs the investigation
to shared composition, not individual hero artwork.

The full small Nemu stage could not reach its capture within the cloud memory
guard, including the bounded Low-preset attempt. No usable actual-hero alpha
statistics or frame came from those runs. Private quality/editor settings were
restored. Missing WorldLookCamera was ruled out as a source hypothesis because
WorldLookPresentation already handles UltimateSceneCamera by name.

A smaller native GPU EditMode test uses the actual UltimatePhaseView RawImage
consumer without full game/audio startup. At CanvasGroup alpha 1, a blue input
frame with alpha 0 lets the entire red underlying frame show through; alpha
0.25 yields RGBA(0.878,0,0.537,1). An opaque input yields blue only and preserves
the intentional half-opacity canvas fade. Thus two causal failures and one
control are retained. The process hit the memory guard during shutdown after
writing its complete XML; it is not a guard-free process claim.

The correction ignores residual camera-texture alpha only at this one consumer,
while retaining the source RGB, CanvasGroup transitions, clipping and stencil.
It starts hidden until its first Draw and owns/disposes its material per view.
Hero art, camera shots, performance timing, audio and gameplay are unchanged.

The unqualified full-scene capture source is retained under diagnostics as text,
not added as an automatic PlayMode test requiring a capture environment variable.
The ordinary regression fixture is the focused EditMode composition test.
