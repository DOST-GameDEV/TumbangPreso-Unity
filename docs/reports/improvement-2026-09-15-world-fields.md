# Complete persistent ground-field snapshots

The prior joining snapshot captured only ice sheets and barricades. A focused
mixed fixture reproduced seven active kinds but only two captured records.
WorldEffectSnapshot now carries those two plus fire trails, shock trails,
Supernova craters, Hex sigils and solid Dante fissure pillars in one bounded
atomic batch. The original snapshot source meta GUID is retained.

Restoration preserves position, facing, remaining life, radius, ownership and
sidegrade parameters. Fire/shock orientation is retained explicitly. Component
Start no longer resets a restored remaining timer. Craters now retire immediately
at that timer's expiry. Existing heat, wake, Hex and pillar visuals seek their
mature timeline instead of replaying birth; Hex restoration is silent. Player
casts, resources, transient impacts, familiar/Coven/sky state and host pulse
cadences remain on their existing paths.

Protocol37 carries WorldFieldBegin/Item/End on the same reliable sequence as
accepted casts. Invalid/incomplete/old batches still cannot replace the world.

## Verification

- Baseline mixed fixture failed with expected7/actual2, preserved unchanged.
- Three focused PlayMode cases passed: all seven kinds' shape/lifetime/repeat
  behavior, existing ice/resource behavior, invalid and expired replacement.
- Five focused EditMode cases passed: batch completeness/deduplication, invalid
  records, empty batch, additional kinds' direction/strength/side bounds and
  protocol assertion. Wire audit:72named messages,0 mismatches.
- Internal Windows player built successfully in49seconds of BuildPipeline time:
  Builds/WorldFieldReview/TumbangPreso.exe. Runtime SHA256:
  0a32ea0cc286134fc71f2997f72ed9efa04f66d2aeeb91d12f6f9a8a3b676dec.
- Three native players: the host creates a controlled seven-kind field fixture;
  two real clients request and repeat complete snapshots. Delayed client link
  is150ms each way. Both received all seven kinds, never exceeded seven fields,
  retained every sampled parameter and ended with zero live fields. This proves
  the packet/factory path; it is not a claim of seven new skill casts or a cold
  process reconnect in this run.
- Actual37host refused a36client. Named profiles and Editor preferences restored.

Evidence: [world-field receipts](world-field-evidence/). Local logs remain under
Logs/mixed-world-*. Raw native traces and original overview PNGs are preserved.
The maximum sampled projected-expiry offset from the host's initial reference
was193ms on host,445ms on delayed client and364ms on direct observer; all remain
inside the predefined550ms link/sampling bound. This capture-bearing run does
not prove frame-identical timing or new performance acceptance.

## Visual critique and limits

Original host and observer overview captures were inspected. The visible frozen
sheet, Split Spires gap, warm crater edge and Hex footprint retain their form,
scale and placement. Ice remains blue/cyan. No new artwork or map layout is part
of this change: deliberately distributed field positions belong to the fixture.
Foreground scenery hides portions of the small fire/shock effects and the far
pillar is partly occluded, so these wide stills are not complete all-angle art
approval. The local mixed test separately verifies the mature solid pillar's
collider height, both spires and all copied directions/scales.

Next: pending-cast state and actual same-process reconnect/rematch/host-loss
lifecycle, then remaining movement/spectator/engineering work. No UI work or
new agents. Deferred Inday and the seventh hero/map remain in the saved order.
