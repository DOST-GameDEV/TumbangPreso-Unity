# Stoke Step: committed movement migration

The researched Pyro signature replaces Flame Rush with a short grounded entry:
2m aimed travel,0.18s anticipation,0.25s recovery before acting again,30s cooldown.
The current Wiki still labels it proposed. This report is a plan and candidate,
not shipped evidence. The separate Empowered throw unit remains unchanged.

## Player decision and critique

Use it to enter space created by a throw, then accept a readable planted finish.
It must differ from sprint through commitment, not a free stun or bigger flame.
The caster stays vulnerable; cover, bodies and confinement remain real. No
mid-burst steering, invulnerability, contact stagger or burning trail. Compare
its actual traversal and recovery to ordinary sprint before claiming feel.

The old17m/s lift, radial hit check and repeating fire fields do not fit this
contract. Reusing their timing and deleting only a particle would be a false
migration. Retired Afterburn/Flare Shot sidegrades stay retired; stale catalog
text is not authority to restore them.

## Implementation boundaries

Preserve sean_skill1 and the existing accepted-aim windup. Use the ordinary
motor impulse/friction/collision path, with a continuous2m distance bound and
native fixed-step measurement. No transform teleport or generic budget increase.
Default-false kit gates make anticipation/live recovery suppress voluntary
locomotion and actions without granting a status effect or rewriting input.
Other heroes retain their current behavior.

Commit alone is insufficient: it only reduces steering. BeginCarry includes a
friction tail and incoming-knockback scaling. Do not mistake held travel for total
travel or apply Paete's Rooted status to the caster. A new impairment during the
windup cancels release without restoring the spent cooldown. Round reset and
rejected prediction must release gates. Do not erase unrelated external pushes.

MovementWindow now carries only Sean's remaining live gate, with zero emission
phase and no wake points. Rejoining must not relaunch an impulse, spend resources,
replay a cast or request obsolete fire emissions. Existing movement snapshots own
position and velocity. Change protocol for these incompatible semantics.

## Focused acceptance

- Grounded eligibility, free refusal,30s cooldown and accepted aim held across0.18s
- Native motor near2m, no steering/lift, no fire fields, real wall/body/confinement
-0.25s recovery and cleanup on expiry, tag, round reset and rejected prediction
- Aged/repeated/terminal recovery cannot extend gates or add impulse
- Default gates leave other heroes and Classic unchanged
- Actual host/owner/observer delivery on a fresh matching player before peer claims

One scoped pass and at most one bounded tooling repair. Preserve failures.
Do not repeat unchanged suites as a substitute for missing behavior checks.

## Presentation remains separate

The current authored Sean dash clip/cue is retained for the first mechanical
candidate; that is not presentation approval. The intended motion is heel and
shoulder compression, whole-body travel, then a planted knee/elbow recovery.
Inspect normal-speed owner/observer views and the real handback before polishing.
A short body ember can support travel; no floor fire may falsely imply damage.
SFX stay under the current listening gate. No silent-film sound approval.

## First native results

Six distinct cases pass across two scoped receipts (4+2), with no fixture repair
or new OOM. The actual native motor reaches the1.75..2.05m acceptance interval,
with under1.5cm lateral drift under perpendicular input. The grounded refusal,
accepted aim, held recovery, wall/body/confinement, absence of fire fields,
tag cancellation, prediction rollback and non-extending restoration pass.
Other-kit default action/movement gates remain false. These are native controls,
not physical-device, actual-peer, animation or SFX approval.

A normal full twelve-scene129GameBuilder candidate is now running. The earlier
private two-scene map settings were restored to the source's twelve-scene list.
Disk headroom was recovered before launch by removing exact retained-player cache
copies and unused precomputed new-project template Library caches; template source
archives, installed package/runtime binaries, imported project assets and user data remain.
No current build or peer pass is claimed until its terminal result is inspected.
