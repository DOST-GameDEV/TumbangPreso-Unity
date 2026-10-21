# Cheska current Wiki rules

The current Abilities - Technical and Status Effects tabs specify a7.5-second Cold
Feet field, Chilling Touch on a landed shove, and a1.5-second Absolute Zero delay
followed by Frozen on every player and Chilled after thawing. They supersede the
older five-second field and generic0.4-second windup. Frostbite delivery/held-hand
eligibility was fixed separately in ba2e7eef8.

## Implemented

- Cold Feet uses the central7.5-second field constant; its description agrees.
- A landed Hero Strike Cheska shove applies the existing five-second Chilled
  status. Both local host and accepted client shoves share ApplyShoveTo. Misses,
  Classic and other heroes do not gain this passive.
- Absolute Zero uses a central1.5-second delay. The Wiki's every-player rule
  includes the caster. Existing status immunity remains authoritative. Frozen is
 2.5seconds and the Chilled clock spans the freeze plus five seconds after thawing.
- Protocol103 excludes builds using earlier timing/passive/target contracts.
  Stable IDs, shared delivery and motor status/slipper snapshots are unchanged.
- No authored models, clips, sound, VFX, map or lighting changed. Existing
  presentation consumes the corrected ability/field clocks normally.

## Evidence and retained failures

Unity6000.5.8f1 Windows D3D11; guarded feedback-cheska-wiki-1001 profile.
Sourcef3ea63a1a plus the explicit owned candidate;596 frozen inputs.

Initial baseline compilation failed because the new fixture omitted the UI
namespace. One extra baseline launch corrected that: one other-hero control
passed, three actual rule mismatches failed, and the Classic fixture dereferenced
its deliberately absent ability component. Final prep corrected its bind access,
but its shared setup still assumed that component for disabling it. That final
run passes four Hero Strike cases and retains the Classic setup failure.

The remaining Classic setup access was guarded, then only that control was run.
It passes1/1. The four unchanged Hero Strike results were reused, not rerun.
This is five distinct passed native cases across two final receipts, not a single
5/5 result. Namespace and Classic setup mistakes remain in the raw logs/receipts.

Actual Cold Feet survives5.2simulation seconds and expires after its full7.5s.
Actual ultimate activation/tick holds all targets until1.5s, then applies the
specified Frozen/Chilled clocks. Real HostResolveShove connects and applies the
passive; real Classic and another hero's landed shoves stay unchilled. Frozen
inputs have no drift after the final control. The existing focused Core numeric
contract also passes1/1 with the updated field/delay values.

Checks and exact inputs are in cheska-wiki-checks. Raw logs remain in the isolated
checkout Logs/feedback-0930/cheska-wiki-*.log. This is native activation/field/combat
qualification, not actual peer transport, a player build, physical devices or
human approval of the resulting gameplay. Further character/UI reconciliation
remains in the original F0930-09 row; do not mark unexamined work complete.
