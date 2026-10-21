# Retain LAN participant identity for career results

The actual 1002m normal-lobby match completed eight rounds with matching scores.
The host saved its result; the client had no career file. The host's record has
an empty PlayerId for remote human seat1, while that client's local identity is
present. No private identity values are included in the retained presence check.

Three boundaries explain the missing line: an unsigned LAN hello omitted the
cached local profile identity; admission did not retain the hello's identity;
the following Identify introduction rebuilt PeerRecord without retaining it.
MatchStatsCollector reads that remote identity and CareerStore refuses to save
when its local player has no matching record line.

LAN hello now includes the cached profile identity without requiring sign-in.
Admission retains the identity claim, and repeated introductions carry it forward.
This does not mark a handle verified or grant an authenticated session. Unsigned
Relay continues to omit a cached account claim; signed Relay keeps its account ID.
Connection tokens remain separate. No wire/schema/protocol or SDK operation changes.

Original native EditMode60319: two causal failures/two controls passed.
Candidate51489:4/4 passed first run, no fixture repairs or retries. Tests execute
the actual hello serializer on dormant native components and LobbySession.Admit;
the full OnClientConnected transport callback is not exercised by these controls.
Actual peer result persistence remains a separate acceptance requirement.

The four owned files match MAIN/qualification. All18,436 other qualification
assets are unchanged. Guards preserved profiles/input and released leases;
post verification28952 completed. Native Unity6000.5.8f1, CPU1536MB/reserve2048MB,
named isolated profiles,240second ceiling. Raw original/candidate results retained.

The 1002m executable predates this fix and the pinned-HOME correction. A coherent
new player and short normal-lobby result-save check follow; no unchanged13minute
match repeat or whole competition-ready claim is warranted by this focused pass.
