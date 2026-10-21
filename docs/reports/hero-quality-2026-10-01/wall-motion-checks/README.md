# Water wall motion and flowing surface

Water wall no longer borrows Crosscurrent's lateral cut. It has a grounded,
shipping Rafi-only clip and dedicated first-person rotational/position paths:
low scoop, paired palm lift at the unchanged0.25second gather, brief open hold
and return. Four sparse edge rivulets sample field age, not global shader time.
The centre stays clear; contact retires every layer through the existing break.
No gameplay range, cooldown, collision, input, sound or protocol change.

Targeted bake preserved the three old clips and their metadata. Only the new
hero-rafi-wall asset and one Rafi roster reference were added. Second bake changed
only the new wall clip while retaining GUID3b02d45fb4ee5731dafbef9225991e70.

Native review:
- First review launch had a missing namespace in the new fixture; one bounded
  fixture repair corrected it. No runtime pass claimed for that compile failure.
- V2 passed one case and filmed60frames per view. Review exposed missing FPP
  rotational mapping and body hands crowding the cheeks. Those product issues
  were corrected; the test now requires real first-person palm displacement.
- V3 passes the same strengthened case: baked clip reference, actual arm94.8degree
  and torso14.4degree movement, FPP displacement, actual first slipper interception
  and collapse, zero replay colliders and deterministic age-driven material.
- Final inputs unchanged, source/assets match candidate, no new OOM. The two-view
  silent export is60frames at30fps, two seconds. It is sampled native motion,
  not a measured hardware frame-rate or an audio/listening result.

Critique: the palms now move in both views and leave the centre open. The body
pose no longer crowds the face. The faint downward surface strokes improve the
water cue, but the large clear sheet still reads partly as a pane in a still.
No claim of finished reference-level artistic quality. The small blue staged
cast particle is outside this surface change. Correct dedicated icons, original
critically listened sound and combined longer gameplay review remain. This is
not a fresh player build, actual-peer or human acceptance claim.
