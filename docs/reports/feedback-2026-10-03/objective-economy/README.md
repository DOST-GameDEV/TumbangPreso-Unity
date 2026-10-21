# Objective-point economy revision

The owner's October 3 request makes income exclusive to can knockdowns (+1),
successful tags (+1), and one passive defender grant (+1) at round start.
Throws and own-slipper retrievals now award zero. Ultimate costs are unchanged.

The defender grant follows the actual derived defender at BeginRound. It is
host-resolved and fenced by match identity and round number, so repeated entry,
intermission callbacks, or resetting a kit cannot farm points. It reads actual
round state rather than the kit's potentially stale pre-round PracticeMode flag.
Replica hydration, Classic and guided tutorial do not generate this grant.
The existing snapshot carries the resulting bank; no packet fields are added.

Objective charge remains capped, persists across rounds, is spent by accepted
ultimates, and resets for a new match. Ordinary skill-recharge hooks remain.
Zack's existing five-second cooldown reduction follows the new income sources;
throws and retrievals therefore no longer reduce those cooldowns. The defender
bonus does not add scoreboard points or change passive defence scoring.

## Qualification

Four focused native Unity 6000.5.8f1 cases pass in 0.282s:
- Only knockdowns/tags grant activity income, including Zack cooldown effects.
- One defender grant per round, stale PracticeMode, rotation and new match.
- Replica, tutorial and Classic refusal, with normal Hero Strike control.
- Full-meter cap and no reissue after a same-round kit reset.

Initial compilation hit the container-headroom guard. A verified orphaned
compiler cleanup retired 732,123,136 bytes RSS and recovered about 671 MB of
container memory; one bounded tooling retry compiled and ran cleanly. Final
peak tree RSS 4,330,954,752 bytes; container 7,334,989,824 bytes. No final guard
stop. Named profile and exact original validation EditorSettings restored.
Frozen input hashes are retained. The existing Amped-Up test expectations are
updated and compile, but that separate case was not rerun in this batch.

Protocol 136 requires matching rebuilt clients. Native grant-route checks do
not establish real contact gameplay, refreshed player, actual-peer transport,
whole-match balance, physical hardware or human approval.
