# Recover queue state after connection startup exceptions

Matchmaker's host/join startup awaits had finally blocks but no exception
boundary. A delayed startup dependency failure left state Hosting/Joining with
busy false and faulted the task awaited by the fire-and-forget Evaluate caller.
Two native baseline cases reproduce both states and escaping task faults.

Only the startup await now maps cancellation/failure to the existing false-result
path. Active-owner errors remain logged. Existing join candidate cooldown and
search recovery remain; cancelled or destroyed attempts still fail the existing
CanContinue fence. UI Raise/Joined callbacks remain outside this narrow catch.
No new retries, backoff configuration, protocol or transport change.

Native final 4/4: host/join failures complete without fault and return Searching;
cancelled host/join failures remain Cancelled, not busy, with no revival. Uses the
existing SessionRestartTests delayed-start hooks and actual Matchmaker component.
No live online or paid service calls, Relay/WAN or actual-peer outage claim.
Two frozen input hashes unchanged; zero fixture repairs; guarded profile and
shared input restored. Final log is Logs/queue-start-fault1002/final.log in the
isolated native project. The current internal Windows player predates this fix,
the preview-audio guard and empty-capture optimization; no refreshed binary claim.
