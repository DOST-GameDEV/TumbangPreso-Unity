# Current pause LEAVE MATCH loads HOME and cancels old simulation

Native PlayMode2/2 passed first run63868 on Unity6000.5.8f1, D3D11 960x540,
profile current-pause-exit1002, serialized GPU2048/reserve2048/450s. Preparation
completed exit0 before launch; guard terminal/restored/no lease. All12522 protected
qualification hashes unchanged and two new fixture/meta inputs frozen.

Both cases construct an arena-classified minimal scene, one registered actor and
real persistent game services with deterministic four-round Classic rules. This
explicitly starts with SceneFlow.InMatch true. They open current PausePanel,
invoke its real LeaveMatch Button callback, and wait for the actual MatchSetup
HOME load/current TumpHub HubHome. Both assert retired round/match, empty player
registry, no open pause/break/held clock, normal time and released visible cursor.
The intermission case starts the actual host break and observes beyond its five-
second deadline, proving no deferred RoundStarted or fabricated MatchEnded.

No production change, baseline/candidate replay or repair was needed for this
additional acceptance of the already shipped exit lifecycle fixes. Astra's source
review caught that an empty starting scene would not exercise arena classification;
that was corrected before the first native run, together with explicit temporary
Classic rules and rule restoration. No assertions were weakened.

The setup has no populated map or transport. Current button/listener, HOME scene
load, cursor state and director retirement are accepted; physical clicking,
shipping-map teardown, hosted-peer quit, WAN and full competition readiness remain
unqualified. Boot/network/licensing diagnostics in the raw log are retained; no
live-service or licence-repair claim follows from this narrow passing scenario.
