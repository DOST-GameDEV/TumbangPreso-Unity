# Glacial Wall restore and replay fidelity

The new compact wall previously became the legacy three-piece straight wall in
world restoration and replay. Its prior slipper hits were also lost, leaving the
restored wall without the intended three-hit budget.

Capture now retains the five-metre arc, three-metre radius and remaining hits.
Restoration preserves aged life and existing damage; replay creates five
render-only arc pieces without colliders or live hazard behavior. Shattered
walls are excluded. Radius-zero legacy straight/split snapshots retain their
existing interpretation. No new packet fields are added.

Protocol148 and recording format14 gate the changed barricade field semantics:
positive Radius denotes arc radius, FirstScale arc length, SecondScale an integer
remaining-hit budget. Matching rebuilt clients are required. This does not
change the owner's ten-second lifetime or three-hit rule.

## Native evidence

- Original baseline: three causal failures, 0.2601933s. Restored geometry and
  replay each had three rather than five pieces; damage was lost.
- Candidate: seven passed, one failed in31.7064238s. All three causal failures
  pass. The remaining failure was an obsolete fixture expecting Cold Feet's
  removed charge pool. Production code was not changed to satisfy that fixture.
- Corrected resource guard uses the current cooldown-based kit, checking both
  cooldowns, both charge counts and ultimate charge survive restoration. It
  passes alongside recording encode/decode and malformed-arc rejection checks:
  final3/3 in2.4909952s. Across candidate/final, ten distinct cases pass.

Final inputs match their frozen manifest; jobs restored isolated settings and
show no new OOM. Original failures remain in the receipts. No new actual-peer,
packaged-player or human acceptance is claimed.

Compatibility review retains format13 alongside10–12 for older saved clips,
while refusing arc geometry labelled as an earlier schema. The extended native
recording round-trip passes1/1 in0.1579953s, exit0, restored settings and unchanged
frozen inputs. No extra OOM. The EditMode header filter did not run in this
PlayMode invocation, so no additional header-test pass is claimed.
