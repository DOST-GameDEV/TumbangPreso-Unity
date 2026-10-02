# Retiring a carrier cancels its old throw windup

Carrier canceled charge in Update when action was blocked, but a disabled body
stopped updating. Reactivation after releasing input let the old charge throw its
held shoe. A previously received throw tell/spin also survived body retirement.
OnDisable now uses the existing CancelAll routine, clearing local charge and
observed presentation immediately. Held-shoe ownership and pickup lock are not
reset by that routine. Reset-channel transport is not separately qualified.

The three PlayMode cases use public SpecialAbility input and native enabled
Carrier.Update, an actual owned held shoe, an upright can and a legal attacker
outside the confinement box. Body deactivation/reactivation is real. Observed
charge uses public ApplyObservedCharge. The normal control charges and throws
through its public release. No private charge writes, reflected Step invocation,
timer expiry, input framework, map or physical travel is used; motor and slipper
simulation are disabled to isolate this lifetime boundary.

Initial29117 was a compile failure with no XML: the old qualification HeroKit
lacked the current Carrier.ThrowChargeRate API. ONE dependency compatibility
repair copied the exact current MAIN base, differing only by four virtual API
members/five lines. That required dependency is the fourth owned qualification
path; its original bytes and the earlier protected snapshot are retained. MAIN
HeroKit is unchanged and is not part of this production patch. The aligned API
remains in the qualification checkout to preserve compilation. These Classic
cases have no HeroAbility and do not qualify hero-specific charge-rate behavior.

Corrected original24020 ran exactly3 cases:2 intended causal failures,1 ordinary
control passed. Reactivation released the held shoe; observed tell returned
0.159999996 instead of-1. Candidate93894 passed exactly3/3 first candidate run,
same fixture/meta/API overlay. No source, fixture or native repair after the
single dependency correction. Post78275 terminal0 verifies exact4 MAIN/q hashes
and all18428 other protected Assets/TumbangPreso inputs unchanged.
Preparation completed with direct exit0 before each dependent launch. Native jobs
use Unity6000.5.8f1/PlayMode-nographics/GPU2048MB plus2048MB reserve/450s ceiling.
All guards are terminal/restored/no lease. No fixture repair or weakened
assertion. Full original/compiler logs and dependency snapshots remain in local
Logs/carrier-disable-lifetime1002. Raw compiler failure, corrected-original and
candidate receipts/XML, compatibility and owned/preservation manifests accompany
this report. No XML is claimed for the initial compile failure.

Acceptance is native component behavior, not physical input, rendered animation,
actual SetBot menu operation, hero hooks, network stop delivery or a new player
build. The frozen1002j artifact predates this change.
