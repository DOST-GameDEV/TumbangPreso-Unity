# Yasmin Absolute Zero crystal refinement

## Demonstrated problems

The native baseline almost hides the gathered crystal. Its old shape is a
horizontal triangle fan with zero Y extent: Y scaling cannot create volume.
The original regression also measures the first shatter origin0.8213m away from
the gathered crystal. The drawn frost used fixed chest/world points instead of
the actual drawing palm and crossed the face in the film.

## Refinement

- A small, faceted three-dimensional crystal, with matching buds and shards.
- Frost drawing ends on the actual free hand, or right hand when empty-handed.
- Shards begin where that crystal is gathered, not at an unrelated fixed point.
- The carrying arm keeps the actual selected slipper. The free palm opens to
  the side; the empty-hand version retains a two-hand gather below the face.
- The last widening shot remains on the same side and rises enough to preserve
  the face and gesture. The initial larger/higher crystal was visually rejected
  even though its geometry checks passed.
- Existing3.2s phase, body grounding, score/authority, ability radius/statuses,
  palette, model, voice and intentionally muted SFX remain unchanged.

## Validation

Original assertion fails with mesh height0 and shatter-origin gap0.8213m. First
assertion attempt had a compile error because this NUnit lacks Assert.Multiple;
it is retained as a failed attempt, not a test pass.

Final held/empty native studies pass2/2,8.6852892s10:03:07–15UTC. Both films
inspected: connected hand stroke, visible faceted gather, readable face, matching
shatter, preserved equipment and standing support. Checks cover real mesh
volume/origin/stroke endpoint, held-shoe head clearance, grounding, unchanged
actor position/score/RNG and a render-only scene. Input hashes unchanged; both
settings restored; Editor exit0; no OOM counter increase.

## Equipment fixture corrections

The former focused stage left slipper owner unset, so HostForceEquip returned
false. It now assigns the actual owner and asserts successful equipment.
Isagani and Yasmin rerun with real held shoes passes2/2,8.6090852s09:11:08–17UTC.
Earlier Isagani isolated films did not qualify held equipment. This changes the
evidence scope, not the shipping equipment logic. The empty-hand review now
parks the disarmed loose shoe off-stage and asserts the carrier stays empty.

## Limits

This is a native render stage, not full-map/peer or player approval. Sound is
muted and unqualified. These scoped improvements do not close Yasmin's full kit
or the remaining-hero presentation assignment.
