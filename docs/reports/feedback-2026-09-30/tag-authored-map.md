# Authored-map contact qualification

The existing whole-body tag presentation is now checked on the authored Eskinita
map with the corrected toon shaders, beyond the earlier isolated floor.
No runtime animation or camera change is included in this unit.

Two real-time close/far cases pass on Unity6000.5.8f1 Linux OpenGL. They use actual
grounded bodies, accepted host tag, one immediate score event, recorded poses and
completed camera frames. Existing checks cover hip movement, torso lean, bounded
arm stretch, unchanged motor location and normal recovery.

The first2/2pass in14.54s used renderer bounds. Visual criticism of the apparent
far gap led to a stronger baked-skin triangle distance check. That2/2pass in14.67s
measures the hand anchor2.3cm/6.7cm from the actual visible skin rather than merely
inside its enclosing box. This is proximity, not a claim of mesh intersection.
Native close/far frames were inspected; visual taste remains human-owned.

## Review history and limits

- Initial setup stopped at compilation because the new conditional targeted a
  neighbouring helper. The exact insertion was corrected once, without runtime edits.
- A private opposite-side camera review exposed the other arm/face and passed its
  existing contact checks. A subsequent automatic reaching-side preference trial
  did not establish a product correction: this actor's shoulder already matched
  the original default side. The trial was removed and original runtime restored.
- That trial's two contact cases passed, but an existing both-wall fallback case
  failed its one-frame observation. It is retained as inconclusive evidence, not
  repaired to obtain a green aggregate or claimed as qualified here.
- No new OOM or resource stop occurred. The final shipped tests retain the two
  authored-map cases and stronger surface metric. Original runtime is unchanged.

[Raw results and labelled review frames](tag-authored-checks/).
The initial frames are from unchanged runtime. Opposite-review and review frames
are labelled camera experiments and are not new shipped camera behavior.
All-roster, interrupted-lunge, actual-peer and player/human qualification remain
open. Do not interpret this report as complete replay acceptance.
