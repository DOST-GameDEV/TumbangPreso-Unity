# Optional cast hull floor integration, 2026-09-24

Integrated tracked lighting source a28037622, including aba9b9d51. The optional
CastInkFloor raises only cast outlines darker than a chosen fraction of their own
colour. It ships at0, preserving the current appearance. Source author comparison
images and reasoning remain in light-1-2026-09-24; they are separate Mac evidence.

One merge conflict was resolved in WorldLookPresentation: retain this branch's
explicit cached sun, preview installation API and restoration behavior; add the
new InkFloor field to per-camera capture/set/restore. No preview API was replaced.

Focused Windows native case passed1/1 in19.144s. Actual Ilalim/Eskinita first-person
and cast comparisons at0/.25/.35 plus25percent greyscale were inspected. The
baseline-repeat comparisons changed0 pixels; raised floors affected only a small
outline fraction. The current storefronts and map assets remain in the local frames.

The effect is subtle at game size, clearer at an arm-edge crop. Retain the existing
default0: the baseline outlines remain readable, and this integration does not
establish a need to change every cast edge. The alternatives remain available for
the owner's taste decision without blocking the outstanding map work. No owner
approval of an alternative, whole-game performance or native player build claimed.

Raw frames/XML and full pre-restore patch: QUAL Logs/light-hull-integration-v1.
Known generated Inday/meta/ProjectAuditor churn restored. No fixture repair or
unchanged-test rerun. Outstanding map contrast/high-preview and platform gates
remain in LIGHT-1.6/1.9/1.10 and final integrated qualification.
