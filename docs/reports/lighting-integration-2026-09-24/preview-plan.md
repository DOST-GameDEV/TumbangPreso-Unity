# LIGHT-1.8 preview integration, 2026-09-24

The actual Eskinita map preview still uses the old dark court and sky. Source branch
50f1fc255 has not advanced after repeated meaningful fetches. Its TODO described
an explicit-sun preview installation but did not contain it. Implement that narrow
integration here using WorldLookPresentation, without parallel shaders or grading.

Preview gets the selected scene's directional sun and measured floor. Only tagged
world cameras receive the preview look; Camera.main alone is insufficient during
preview. Cache all three ambient colors, keep the authored scene baseline, and
reuse the same live rig/ramp/sky during repeated ReapplyEnvironment calls. Restore
and disable the old rig immediately before switching/parking or destroying it.
Gameplay installation keeps its existing ownership and camera behavior.

One focused behavior case cycles Eskinita/Lagoon/Eskinita, repeats refresh, checks
same sky/rig, selected map fog, menu exclusion and cleanup. Reuse the existing actual
preview render and two existing transition cases in one guarded run. Inspect the
same-camera before/after from refine2-backdrop-v1, including grey25. No new framework,
whole-game regression or native build for this integration unit.
