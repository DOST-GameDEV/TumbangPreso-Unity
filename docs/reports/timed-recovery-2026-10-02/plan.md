# Retire mash-to-escape recovery

The latest Wiki Feedback explicitly deprecates all space-mashing status/skill
recovery, including Cheska's Frozen state. Frozen remains incapacitating and
ends on its authored clock; pressing faster must not shorten it. Ordinary Jump
and the single jump used to initiate a valid swim-edge climb remain controls.
Paete's distinct hold-Interact root removal is not a mash and is unchanged.

Retire active human, bot, local prediction and host-request mash paths together.
Trips should count down their existing authored TripTotal normally, including
the final existing get-up beat; they must not be stranded behind a deleted
press meter or wait for the old emergency timeout. Edge catches must progress
through their existing catch/hang/pull-up phases autonomously. Keep poses,
landing validation, immunity and movement ownership. Replace live mash/press
prompts and press-count pips with truthful timed state/recovery presentation.

Keep legacy snapshot/recording fields and public entry points harmless where
needed for compatibility, rather than silently changing packet layouts. New
mash entry points must be inert. Protocol134 separates the new recovery semantics
from clients that still predict input-shortened recovery. Recording format13
stays unchanged; old recorded poses are not recomputed as live gameplay.

Claim CharacterMotor.cs, CharacterMotor.EdgeRecovery.cs, AIController.cs,
UI/Hud.cs, UI/TumpMatchReadout.cs, Net/MatchRpc.cs only for retired recovery
handling, Net/NetSession.cs version/reason, focused recovery tests and affected
legacy recovery expectations, plus owning documentation. Do not modify private
HeroHazards, finalized per-hero art/skills, ordinary jump bindings or the separate
intro flow in this unit. Retain Core's unused legacy pure functions if required
by old callers; no live recovery route may invoke them.

First reproduce input-shortened Frozen/trip behavior in small native cases.
Then qualify timed Frozen/trip/edge expiry, human/bot input neutrality, refused
legacy recovery requests and snapshot stability, ordinary jump control, and
live HUD text/progress without mash prompts. Update tests for superseded mash
contracts without discarding unrelated input/ownership/menu guarantees. Use
one focused native case per process; separate import/compile from runtime.
No memory-limit increase or personal-PC use. Actual peers/player/human review
remain distinct from native checks. The BH Studios startup request is next.
