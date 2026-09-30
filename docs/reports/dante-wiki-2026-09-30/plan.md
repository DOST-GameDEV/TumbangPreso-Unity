# Dante current Wiki reconciliation

Current complete Wiki: Earthbound halves shove/knockback distance. Unstoppable
cleanses removable status and grants15s immunity,40s cooldown. Boulder imbues the
held slipper with Concussed,35s cooldown. Bastion follows/reflects for7.5s,35s
cooldown. Continental Drift costs12points and sends quick successive blasts
forward across the map, applying Concussed. Status table says Concussed slows
movement75percent for2.5s. No new skill SFX under the current owner restriction.

Current source instead has20s/45s SHIELD, a separate thrown rock at30s, BARRIER
at25s and an instant global EARTHQUAKE at14points. Preserve existing authored
ward/badge/barrier work and stable IDs. Do not call the whole row complete after
only correcting names or numbers. Treat each mechanical unit coherently.

## First unit: Earthbound

Reuse the motor's existing locally simulated incoming impulse/carry path, including
host results delivered to owners. A default-one kit distance multiplier keeps
other heroes unchanged, with Dante0.5 only in Hero Strike. Apply square-root scale
to an incoming horizontal impulse, because its friction distance is v²/(2f).
For held carry, scale both horizontal speed and held time by sqrt(0.5), halving
both its held travel and friction tail. Keep vertical lift, existing caps,
source ownership and authority gates. Do not alter voluntary walking or Classic.

Claim HeroKit.cs one default property, DanteHeroKit.cs passive override,
CharacterMotor.cs incoming impulse, CharacterMotor.Status.cs incoming carry,
GeoRules in RosterReworkRules.cs, NetSession.cs compatibility and new
Tests/PlayMode/DanteEarthboundTests.cs/meta. Baseline equal incoming impulses and
carries with actual physics; compare Dante with a neutral kit under identical
geometry, then require approximately half horizontal travel. Include Classic and
new input after reset. Keep actual-peer qualification separate. One heavy job;
stop at fresh nonzero test results, one bounded tooling repair if needed.
