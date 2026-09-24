# Tracked lighting branch follow-up, 2026-09-24

Integrated source 8d73471f3, the five commits after previously merged 50f1fc255,
into ASTRA baseline 4e2c5bc6d. Two owner-class conflicts were resolved deliberately:
retain our qualified explicit-sun/tagged-camera preview API, same-map reuse,
immediate release and cached-ground property-block ownership fix; add incoming
HDR target, active-scene handback guard and surviving-root/sun selection. One look
component owns each selected map. No UI layout, sign art or map-camera redesign.

The preview now uses supported DefaultHDR (ARGBHalf on this machine), falling back
to ARGB32 when unavailable. Scene settings are restored only while the scene from
which they were read remains active. Object-owned light/ground state still returns
to its original values. A stripped match root or its transient lights cannot own
the preview look. Source history and the incoming five-map test are retained.

Two focused native cases passed 2/2 in 25.645s: incoming five-map preview/sun
handback plus existing cached-map refresh/ground-lift regression. The first now
also verifies that new active-scene ambient/fog survive preview destruction, and
captures old LDR and new HDR targets through the same camera without advancing
simulation between them. No fixture retry or broader suite.

All five actual paired images and 25 percent greyscale thumbnails were inspected.
The adopted bright palette and readable court/context remain; differences in
these views are subtle, not a claimed dramatic visual redesign. All six visits
(including Bayan revisit) used ARGBHalf with live bloom. Fog/sun/profile handback
and menu-camera isolation passed. Evidence is in results.xml, preview-look.csv,
paired-1/2.jpg and grey25-1/2.jpg. Raw frames/full patch are QUAL
Logs/lighting-preview-followup-v1. Known generated churn was backed up/restored.

LIGHT-1.6's remaining contrast/taste review and LIGHT-1.9's native performance gate
remain open. This is not a complete map-parent or overall-goal qualification. No
intermediate build, unchanged storefront/card rerun or browser helper was used.
Resume Bayan ginger cat, then remaining individual ambient life and older queue.
