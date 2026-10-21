# Carry regression checks exercise a playable round

The unchanged current production implementation reproduced four failures across
the five CarryTests: three refused pickup setups and one assumed idle hand
motion. The existing remote-smoothing control passed. These matched the older
failures retained during Paete palm qualification, rather than showing a new
Paete runtime regression.

The fixture previously loaded an arena but never entered its playable round.
Setting only CharacterMotor.RoundActive did not clear the other real input and
action gates. The repair uses the existing map fixture and actual countdown,
asserts Round.RoundActive, selects an explicit attacker and drops/retrieves
that attacker's real owned slipper. It does not bypass ownership or CanAct.
The first-person case checks the actual held object hiding after drop and
appearing after accepted pickup. Movement now explicitly must exceed0.1m.

Entering the round alone fixed pickup but not the motion assumption: a carrying
idle can keep its palm still. That retained partial run passed1/2. The pose
check now requests an actual accepted T-pose emote and measures maximum observed
motion rather than two potentially identical loop endpoints. Its original
0.0005m motion threshold and bounds assertion remain. The existing3cm/5cm
carry bounds, missing-anchor fallback, first-person renderer and remote
smoothing assertions are unchanged. Launch flags and pinned rules are restored
after each case. No production gameplay or rendering code changed.

The explicit-pose/pickup pair passes2/2. The first full fixture reached4/5: the walking test now exercised real
motion but read0.118m before the runtime carry update. A camera callback
experiment produced zero samples in the batch editor and was rejected as
evidence. A test-only late observer then measured60 completed poses: coroutine
gap0.117518m versus after-LateUpdate gap0.0000000596m. The final test observes
that completed pose, retains the5cm limit and requires at least50 samples.
No production carry update was altered. Final full CarryTests fixture passes5/5 in one graphics-enabled native run
(exit0,125.0s). Frozen inputs and both Editor/Quality setting backups match
after completion. No additional OOM occurred. This restores a focused regression
gate; it is not a full-project suite, multiplayer or release-build pass.

Incoming late-join-rules fix8d8822d3 was preserved in the merge. The integrated
runtime then passes3/3: actual first-person carry and both incoming current-rule
arrival/repeated-identification controls. Process156.2s, no additional OOM.
