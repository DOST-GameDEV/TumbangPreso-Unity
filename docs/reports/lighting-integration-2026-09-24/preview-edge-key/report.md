# Preview edge sun and native probe integration, 2026-09-24

Merged tracked source 429643416 into baseline 81054255e. WorldOutline now reads
the live look's selected KeyLight, instead of SkyEvent's potentially unrelated
menu/portrait fallback. The getter uses our already-cached explicit sun, retaining
the qualified preview API, HDR target, scene handback and cached-ground fix.
No duplicate backing fields, look components or UI layout changes.

The source's native graphics probe addition is preserved: it measures look-on and
look-off in the same player, with a separate world-render-look-off.csv so the
existing 15-cell report contract stays intact. It compiled here, but no new native
player was built or performance run made. TODO retains the source branch's Apple
M5/Metal measurements with machine/revision scope. Windows performance and the
owner's window shape still belong to the final coherent build gate.

The expanded five-map native preview case verifies KeyLight identity and the
actual rendered WorldOutline material's view-space sun vector with a portrait
light present. Refresh, revisit, camera isolation and scene handback also pass.
V1 passed 1/1 in 8.235s. Inspection caught different idle-orbit angles in its
cross-run comparisons, so those files are retained as unmatched-v1, not matched
visual proof. One bounded capture correction adds an editor-only legacy-key
switch, absent from built players; old fallback and selected sun render back to
back without yielding. V2 passed 1/1 in 7.056s. No further fixture loop.

All five actual v2 matched pairs and 25 percent greyscale images were inspected.
Directional edge accents remain subtle; the bright palettes and courts stay
intact. Ilalim's difference is especially small in this view. This is a correction
to light ownership, not a claimed dramatic art redesign. Source dark-skin hull,
pastel/contrast and remaining native performance review remain explicitly open.

Raw results/images/full pre-restore patches: QUAL Logs/lighting-preview-key-v1/v2.
Known generated churn backed up/restored; no new browser/server or intermediate
build. Resume Ilalim tuxedo cat, then remaining bird visits and older queue.
