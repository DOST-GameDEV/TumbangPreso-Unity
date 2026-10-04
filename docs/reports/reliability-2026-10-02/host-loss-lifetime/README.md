# Retire abandoned simulation before lobby authority returns

Unexpected host loss takes a different route from the manual MAIN MENU action.
`MatchRpc.HandleClientDisconnected` navigated directly to the empty online lobby.
The persistent directors remained live; `MatchAbandon` revokes authority only until
the next single scene load, which could let the old round clock resume in the lobby.

The handler now retires simulation before its unchanged online-lobby navigation.
It shares the manual exit's existing break/round/match cleanup through
`SceneFlow.RetireMatchSimulation`. The already-in-lobby early return is unchanged.
`NetSession` captures the abandonment cause and round before raising the disconnect
event; retirement preserves that diagnostic and does not manufacture a completed
result. Restoring lobby authority cannot revive the abandoned round.

Native EditMode baseline: **2/2 causal failures**. The handler left the round active,
and restoring authority allowed its clock to move from90 to89.98seconds.
Candidate: **6/6 passed**, comprising those two cases and the four existing manual
exit controls because their cleanup was extracted into the shared method. Zero
fixture or tooling repairs in this unit. Both jobs completed profile/input
preservation and released their leases.

The fixture invokes the real disconnect handler with controlled persistent
directors, retains the recorded failed round, and restores authority through the
existing public Clear method. Its existing same-frame scene latch or expected
Editor unavailable-scene log keeps navigation out of the test. This proves handler
cleanup and authority/clock behavior, not a real transport loss, rendered lobby,
physical input or WAN. No SDK or network call was made.

Sessions98447/27370. Raw XML, job receipts, guard output and the original/candidate
source manifest are retained. Qualification logs:
`C:/Users/matth/Documents/Codex/work/tump-feedback-0930/Logs/host-loss-lifetime1002`.
Owned paths are MatchRpc's disconnect hunk, SceneFlow's shared cleanup and two
appended MenuExitLifetimeTests cases. Protocol and packet formats are unchanged.
