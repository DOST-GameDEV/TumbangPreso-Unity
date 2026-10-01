# Pre-round handover applies the chosen character

The first actual117 reconnect attempt restored identity, seat and active world,
but both peers showed character3 rather than the seeded character2. Source review
found the arena can build a bot placeholder before client Identify arrives.
HostTakeSeatBackFromBot changed driver/name but left that placeholder's character;
BroadcastPicks then overwrote the client's chosen character with the wrong one.

Before the first round, handover now uses the existing pick/model/kit/skin sync
route for the accepted choice. During play it keeps the existing character.
The existing UTC-week Mirror rule still overrides the preference in that format.
No authored model, animation, effect, sound or loading source changed. This fixes
selection/binding; it is not a character redesign or kit retune. Protocol117 stays.

Native listening-host baseline2cases: pre-round choice fails2→3, live-match
retention passes. Final3/3passes those plus Mirror preservation. Source72a1187b4
with two owned overlays;674frozen inputs unchanged and both files match candidate.
Jobs56793/88521terminal, no fixture repair. [Receipts](pre-round-pick-checks).

The original actual reconnect failure is retained in the isolated
Logs/feedback-reconnect-player-1001/rejoin. Native checks do not retroactively
turn it into success. A changed player and the same strict reconnect expectation
are the next acceptance step; no new peer/player pass is claimed here.
