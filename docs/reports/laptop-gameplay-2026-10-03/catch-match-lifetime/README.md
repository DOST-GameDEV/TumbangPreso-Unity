# Catch playback match lifetime

Same-round match identity adoption could keep a previous match's active catch
view. Its same-victim duplicate shortcut also ignored a legitimate new-match tag
while that previous view remained active. CatchReconstruction now captures active
and pending presentation match IDs, retires the old active view before that
shortcut, and fences both consumer paths. End clears those IDs. Recovery,
scoring, history, authored camera direction, assets and kit behavior are intact.

The original rendered native baseline reproduced exactly2 causal failures and
2 passing controls. Existing history ownership already blocked the pending old
contact case, so that case is a control, not a third demonstrated defect. The
same-identity accepted catch and recovery remain valid. Only CatchReconstruction
runtime source changed before the same four cases first passed4/4.

The fixture uses actual accepted HostResolvePunch and public client
AdoptPresentationMatch at an unchanged round. Callback order is synchronous,
before history's next presentation tick. Both runs use the full frozen e3
Runtime dependencies, including existing history ownership guards, in isolated
qa-d with graphics and Unity6000.5.8f1 on the laptop. Source and fixture hashes
are in summary.json. All guards terminated, restored state and released leases.

The first fixture compile lacked the Visual namespace for MatchFlair. One
bounded namespace-only repair retained setup, assertions, case count and GUID;
the original CS0103 diagnostics and receipt are preserved. No gameplay or zero
test pass is claimed for that compile failure.

This fences active and pending state already observed before identity adoption.
MatchFlair itself has no match ID; arbitrary stale flair first delivered after
adoption remains a different network contract question. These local public
client-state/renderer checks do not qualify wire delivery or physical operator
screen acceptance. The unit is excluded from frozen1003g until a later artifact.
