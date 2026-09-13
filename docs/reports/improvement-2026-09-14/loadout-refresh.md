# Same-hero loadout synchronization

A repeated character pick used to return before reading the new build whenever
the hero class was unchanged. Late lobby build data also only updated player
names, leaving realized skills on the old sidegrade.

Same-hero synchronization now refreshes variant selection/presentation/tuning
without replacing the live kit or abilities. Restore only the five authored
loadout-tuning fields before applying multipliers, so repeated updates and
alternate/default round trips cannot stack modifiers. Cooldowns, charges,
ongoing duration grants, ultimate meter and hero-specific live state retain their
existing owner objects. A changed hero still gets its own newly constructed kit.
Late roster build arrivals refresh when their character pick matches the live
unit; otherwise the subsequent pick update applies the matching build. Classic
has no ability component and remains without powers.

Validation: HeroLoadoutRefreshTests8/8passed, Logs/hero-loadout-refresh-v2.xml.
Six real hero kits cover selected variants, repeated sync, defaults/alternate
round trips, live timers/charges/grants/ultimate retention. Two routing cases
cover remote-default refresh via the actual sync helper and Classic no-powers.
The v1fixture incorrectly expected17banked points on heroes whose caps are10-15;
the corrected test compares each hero's actual starting bank, without changing
runtime costs. No full suites were run.

Not yet qualified with actual separate-process delayed/rejoin packets. That
belongs to the later coherent networking/ability integration pass; these are
focused runtime-object and routing tests, not a multiplayer release claim.
