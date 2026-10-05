# Throw freely across can states; lower charge on restore

Owner revision: remove can-down and can-barrier throw refusal. At the exact
down-to-upright edge, lower an active held charge to zero over0.5s and prohibit
release during that return.

## Implementation

- Core throw legality retains live round, attacker, held slipper, position and
  individual lock rules. Can orientation and barrier no longer gate throwing.
- Lata's host transition and received snapshot transition start the same Carrier
  decay for registered bodies, including companions. Duplicate upright updates
  never restart it. Idle/unheld actors do not invent a wind-up.
- Charge and observed preparation use a smooth0.5s curve. Existing body and
  first-person hands consume that same preparation. Release during lowering is
  discarded, never buffered. Keeping the button held starts a fresh charge.
- Input, RoundDirector and direct HostThrowAt all refuse release during decay.
  Received charge keepalives cannot raise the returning hand. Losing the shoe,
  role changes and normal input retirement still clear the pending action.
- Both obsolete warnings are removed, and reticle/legacy hints no longer call
  can protection a throw refusal. The barrier still protects can contact.
- Protocol146 requires matching rebuilt peers; packet layout unchanged.

## Native and managed validation

- Six focused native cases pass: actual releases while down/protected; host and
  snapshot restore edges; mid-decay local and direct-host refusal; exact midpoint;
  duplicate snapshots; fresh charge; observer keepalive; pause; lost equipment.
- Eight existing disable/role/input-lifetime cases pass unchanged.
- Eight old duration cases failed because they still asserted superseded1.25s
  charging. Expectations now use the owner's already-shipped1.5s and0.75s half
  charge. No production charge duration changed in this unit.
- Final15/15 native checks pass4.6938915s09:41:37–41UTC, including all six new
  cases, all eight corrected duration cases and actual body/FPP motion capture.
- Managed throw-legality case1/1 passes. Initial managed invocation could not
  write its default CLI home; repeated with a task-owned CLI home, no permissions
  or credentials changed.
- The first motion stage had a disabled ungrounded motor and showed its fall
  pose afterwards. The corrected stage uses the actual grounded motor. Both
  films retain evidence; only the final grounded clip is presentation evidence.
- Final input hashes unchanged, settings restored, native exit0, no OOM increase.
  Real frame timestamps are retained, not an FPS/performance benchmark.

## Limits

Actual paired devices, full-match integration and human feel acceptance remain
open. Focused native host/snapshot seams are not actual network-peer testing.
The full scene-dependent older UI/tutorial tests were updated for this rule but
were not included in the focused15case run.
