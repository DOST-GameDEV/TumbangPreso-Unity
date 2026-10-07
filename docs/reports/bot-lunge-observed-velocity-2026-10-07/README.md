# Lunge reach uses observed velocity without a tier multiplier

The release-reach calculation used AheadOf for future target positions. That helper intentionally scales lead by the bot tier, but a physical reach check then treated a retreating observed target as running more slowly. Original native32416 reproduces a full-dash false positive against a3metre target retreating5m/s; stationary and approaching controls pass.

The fixture obtains belief through the real Observe method. Its independent continuous friction solve is a generous upper bound on actual discrete dash travel. Even that bound leaves the retreating target outside the tag radius, so approving the release is incorrect. This is a staged physics/perception calculation with simple floor and known velocity rather than a complete moving match.

LungeCanReach now projects from the already perceived position and cached measured velocity at its full magnitude. It retains the existing reaction-delayed belief and does not read an unseen live actor's velocity. Ordinary movement anticipation still uses the tier's lead policy. The body's velocity, actual consumer charge, friction, radius, obstruction and edge rules remain unchanged.

Candidate46708 passes28 observed-velocity, charge-unit/continuity, motor-turn, tag commitment, power, obstruction and lifetime controls. All21371 frozen inputs/preferences restore after both jobs terminate. No direct transform write, dash/cancellation bypass, mechanic retune or authored hero/old-map presentation change is added.

Preceding corrected-charge natural Arena native45944 passes both Classic/Hero windows and records actual consumer charge seconds. Classic has9throws and3 counted hits from4lunges; Hero has6throws and no completed lunge. One Classic charge lasts4.639seconds and misses. Short unseeded observations are not evidence of an improved hit rate or full feature/role/map quality. All21369 inputs/preferences restore. The next natural check must evaluate this latest forecast against actual contacts.

Desktop725e remains its qualified release; this Editor source work is not current package acceptance. Laptop effects/preview/Archive/floor lanes stay reserved. No LAN, new worker/service or foreground control was used.
