# Next Round: latest3.5second owner request

The latest unstruck Feedback edit replaces the earlier5second request with3.5.
Change the shared ordinary BreakDuration only; Halftime remains10seconds with
its existing replay/fallback/standings sequence. All views and late arrivals
must keep deriving remaining time from the original host-authored beginning.
Do not introduce a separate UI timer or unfreeze controls early.

Claim Runtime/HalftimePresentation.cs for the one duration constant,
Runtime/Net/NetSession.cs for protocol133 and its reason, the existing focused
assertions in Tests/PlayMode/ReplayRetentionTests.cs and RoundBreakFreezeTests.cs,
and owning documentation. A mixed-version peer would derive the wrong deadline,
so the compatibility gate must distinguish the new shared timing; packet fields
and recording schema do not change. Preserve other contributor reservations.

First retain the actual scheduled-break case failing the new duration assertion
against old source. Then change the constant and run three separate fresh native
cases: scheduled ordinary/halftime/end-of-match boundary, frozen final view and
real inputs, and late client observation without advancing/restarting. Keep the
existing actual-replay evidence; do not imply a new remote-peer or full-player
pass. Separate import/compilation from map runtime under the unchanged guard.
