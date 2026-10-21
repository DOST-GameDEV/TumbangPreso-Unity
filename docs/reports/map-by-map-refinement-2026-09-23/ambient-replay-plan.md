# Ambient life and replay isolation, 2026-09-24

Source review at d98e592ae found a concrete integration gap. AmbientLife correctly
advances with scaled delta time, and its local dog/cat/bird checks already cover
pause. Its live animals are not recorded in MatchReplayArchive, but neither
RecordedWorldView nor CatchReconstruction hides those live renderers. A current
dog/bird can therefore appear in an earlier recorded event at the wrong time.

Keep the approved per-map routes, activities, models and motion. Hide unrecorded
ambient renderers only during each reconstruction camera's synchronous render,
using that camera's existing hide/restore lists. Do not pause the live world,
turn off components/GameObjects, add replay bytes or invent animal history.
Live player/spectator cameras still see the animals; their previous render flags
must restore exactly, including renderers that were already hidden.

Use the existing retained-exchange check and a short catch reconstruction case.
Observe the real render callback, live transform/visibility preservation and
restoration. Stage one existing dog into the retained camera's view only to make
the before/after difference visible; label that witness, not ordinary animal
behavior. Preserve the before failure and use one focused post-fix pair. Inspect
paired captures and 25 percent greyscale. Tooling repair budget 0/1 initially;
stop at one bounded correction rather than growing another capture framework.

New character/ability/ultimate animation remains the owner's cloud assignment.
This local change only prevents present-time environment actors leaking into
past-time reconstruction. Full replay/peer acceptance stays in 2.10/P7.

Native baseline reproduced both ambient leaks. First post-fix run passed the
catch case and retained-camera ambient assertions, then the original retained
test's can-model replacement exposed a real MissingReferenceException in
Lata.RestoreRaisePresentation. Its cached mesh survives replacement and is written
after destruction. Fix invalidation/rebinding only, preserving authored raise/
clunk motion and authoritative can transform. Prefer the replacement Visual
subtree over unrelated effect meshes. Run the same focused pair after this
product fix; this is not a fixture repair or a new animation direction.
