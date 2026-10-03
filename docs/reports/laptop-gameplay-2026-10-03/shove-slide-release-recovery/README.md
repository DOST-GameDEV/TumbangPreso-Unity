# Shared shove and slide button recovery

Combat now observes shared-button release before action and role routing. A press
spent by a successful shove or retrieval slide can rearm when released during
stun or a defender role, so the next fresh attacker press works after recovery.
Attacker dispatch still requires the button to be pressed and unspent. Cooldowns,
refusal refunds, free shove, slide cost, lunge behavior, hero kits and art remain.

## Reproduction and checks

The original six native cases reproduce three failures: a fresh shove after
release during stun or a registered defender snapshot, and a fresh retrieval
slide after release during stun, all fail to start a cooldown. Three controls
pass: ordinary shove and slide release/fresh-press cycles, and an initially
cooldown-blocked unspent edge that can predict after rollback. The slide uses an
actual owned loose shoe and asserts the public retrieval predicate. Each shove
keeps stamina unchanged; each slide spends Balance.SlideStaminaCost (25).

The first candidate passes6/6 with the same fixture and assertions. The change
moves existing spent-latch release observation ahead of CanAct and role gates,
and retains the held-button condition in attacker dispatch. No source or fixture
repair and no repeated unchanged test run. Original exits2 for behavioral failures;
candidate exits0. Both guards restore isolated preferences and release leases.

## Exact source and scope

Logical code/editor/test/shader/package/settings source6d5d273334e780016f9d18a6d19490f9f34c5553,
plus the declared candidate CombatVerbs overlay. Native working candidate SHA256:
603983ced35a3279b61d9375b82ad713d0050ceff707d55f83dab95d444baa05.
Fixturef2eba7a6cdd0a707394e479d459bd26871cb8eaf84214c025e4499c6638876bd.
Both3348-file manifests were verified unchanged after execution. Older worker
Gitbase8e7 and validation company/product isolation are explicitly disclosed.
Raw XML, receipts and complete source manifests remain byte-preserved in Git.

This qualifies shipping consumer Update with supplied client/nullRPC predictions,
public refusal rollback, public registered Match/Round snapshots, actual stagger
and manual input commit. It does not simulate a wire denial, peer connection,
physical operator input, full match play or current packaged-player acceptance.
The fabricated unheld buffered-lunge hypothesis was rejected: shipping producers
supply held input, and the existing unheld dispatch behavior is preserved.
Frozen1003g134 and earlier broad61 evidence do not qualify this newer source.

Committed canonical CombatVerbs SHA256 is
0d93959e5510cd7ddb886fd642bb59220b10b57a8d4df6b295c7d654af7fb9b4.
It equals the checked603983 working file afterCRLF normalization. Publication
also normally integrates the separately checked Carrier role cleanup and incoming
PC lobby protocol140. This reuses the focused source proof without claiming a new
whole-integration, wire or packaged-player pass.
