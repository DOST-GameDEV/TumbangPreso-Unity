# Private Supernova visibility change: evidence review

Read-only review on 2026-10-02. The retained 2026-09-24 early-impact colour frame
and 25 percent greyscale show the central witness losing substantial contrast
behind the orange shell. The lata is also covered by the opaque central burst.
This is a gameplay-visibility observation, not a preference for a smaller or less
expressive ultimate.

An earlier after-run was recovered in the existing
TumbangPreso-Unity-world-qualification-20260923 checkout. Its
Logs/supernova-overlap-after.xml passes the accepted-cast/landing capture case 1/1
at 17:11:43–17:11:57 UTC. The same folder's supernova-overlap-live PNG timestamps
match that run; its HeroHazards source was last written at 17:10:35 and still has
the private Fire StartAlpha 0.50 change. Exact paths, hashes and timestamps are
recorded in evidence-references.json. This establishes a strong after association,
but it is not a complete frozen input manifest or current-player qualification.

The after early frame exposes more central-player detail. The after middle frame
has a visibly different shell radius/animation phase from the retained before
middle frame, so those frames cannot establish a matched-phase comparison.
The opaque central burst still covers the lata. The evidence therefore supports
partial player-visibility improvement, not a completed player/lata/rim fix.
The historical test's pass means its capture/landing conditions completed; it
does not certify visual readability.

The retained temporary helper is SeanSkillTimingProbe.SupernovaWaitsForLandingDuringAnElevatedCast
in that old checkout: three parked witnesses, spectator camera at (0,1.65,-7.2),
FOV 72, look-at (0,1,0), real accepted elevated cast, then 0.06s and another 0.12s waits.
It adopts ColourGrade but lacks the current WorldLookCamera context marker.
It must not be treated as a faithful current-rendering fixture without that scope.

When a separate GPU slot is available, the smallest remaining check is one current
baseline capture and one candidate capture through that accepted cast, preserving
the same world, camera, witness positions and authored effect. Record the actual
impact elapsed time and shell scale with each image; compare matching phases.
Use the existing world-camera context, inspect the central witness, lata and rim,
and stop after the pair. If the lata remains hidden by the burst, retain the
partial finding instead of claiming opacity alone solved it. No VFX redesign,
new shader, mechanics, audio or finalized-hero changes belong in this check.

No source, private asset or original evidence was changed or staged. No Editor,
player or new capture was launched. The private HeroHazards candidate remains
unqualified for publication by this review.
