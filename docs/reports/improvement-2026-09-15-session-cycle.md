# Same-process reconnect checkpoint before UI

Status: the current focused lifecycle check passed. No production lifecycle
change was necessary. The original suspicion was that MatchRpc's lifetime
_snapshotRequestStarted flag might prevent a second session from restoring the
arena. Source inspection also found MatchInstaller's explicit request after
each arena build; the actual run confirms the normal path restores this case.

The controlling client reconnected to the same host through real
NetSession.StartClientAsync and reloaded its arena while retaining its native
process/profile. Diagnostic world requests were disabled for that returning
owner. Only normal game code could request its live world state. Host and a
second client remained separate native players, with150ms one-way owner delay.

All seven persistent field kinds returned to the owner with original parameters,
no duplicates and correct eventual expiry. Host/owner/observer wrote744/496/700
field records; each reached seven live types and finished at zero. The largest
sampled projected-expiry deviation was371ms, within the predeclared550ms bound.
The evaluator also requires unchanged process ID, preserved local seat, both
pre/post-rejoin records and no diagnostic snapshot-request flags on the owner.

Internal player: Builds/RejoinLifecycleReview/TumbangPreso.exe. Runtime SHA256:
efb9425612d04080060885e7bf8f50b4e0ba7f45cb1bca7af22345ece6ddcfaf.
Build succeeded; guardbc5324a9102c restored the named Editor profile/preferences.
Native named profiles were restored. Protocol37 is unchanged from the preceding
qualified ground-field build; no repeated compatibility run was needed.
Evidence: [session-cycle receipts](session-cycle-evidence/).

This checks one HeroStrike arena reload/rejoin, not every rematch, host change,
Classic flow, recovery input or active windup. Those remain explicitly open in
[the gameplay resume checklist](../GAMEPLAY_RESUME_AFTER_UI.md). The native fixture
creates host-side fields to isolate session recovery; it is not seven new casts
or additional artistic approval. No gameplay, art or UI implementation changed
in this checkpoint, only opt-in diagnostic coverage and continuation records.

Owner's new priority now applies: analyze the supplied hand-drawn UI assets,
plan the complete replacement and implement it before resuming that gameplay list.
