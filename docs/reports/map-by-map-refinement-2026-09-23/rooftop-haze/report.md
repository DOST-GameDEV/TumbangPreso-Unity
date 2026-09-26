# Sa Bubong haze and current map card, 2026-09-24

Baseline07a387779. A fixed-camera native study compared the existing60-300m haze,
90-380m and120-480m. Actual colour and25percent greyscale inspected.90-380m keeps
more identity in the middle city facades without giving far blocks the stronger
contrast of120-480m. Retain the warm haze hue and all light/material values.

Only SaBubong's two distance fields changed in both fallback defaults and the
saved Resources/WorldLookProfile.asset. The explicit WorldCueProfileAuthor command
updates this map alone. Serialized diff confirms the two distances and the newly
explicit, unchanged CastInkFloor0; other authored maps/style values are retained.
The lower ground is still warm by material choice. This does not claim that haze
alone resolves its surface/detail assessment or every outstanding lighting issue.

Study1/1 passed in1.438s. The first final export passed its capture check, but
actual inspection caught an unchanged look: the saved profile still overrode the
new code defaults. Its image/XML remain as unapplied-profile evidence. After the
targeted data update, final native v2 passed1/1 in1.155s; actual card and grey
inspected and accepted. This was a production data correction, not fixture repair.

The SaBubong map-vote PNG now shows the final actual roof with dark court marks
and selected haze. Its original importer bytes/GUID remain unchanged, verified
by card-importer.json. Existing capture tooling gained only a single-map selector;
default street-card behavior remains. No UI layout, camera descriptor, collision,
character/animation, rule or other-map asset changed.

Raw frames/XML/full pre-restore patches remain in QUAL Logs/roof-haze-* and
Logs/roof-final-map-card*. Known generated churn restored. No fixture retry or
extra study variant. The parent map and LIGHT-1.6 retain outstanding integrated
intro/spectator/platform and remaining contrast review; no broad completion claim.
