# Distinguish completed match departure from abandonment

The actual Windows protocol150 client log reports match over with the same
complete saved record as the PC, then ABANDONED at round4of8 when the host
leaves its result screen. MatchAbandon classified transport loss alone and read
round rules after the disconnected room had relinquished its host selection.
The completed four-round game was consequently described as an unfinished
local eight-round game. No saved-result loss was observed.

MatchDirector now captures completion and intended round count at its existing
host/client MatchEnded boundary. It clears the marker on live/reset/new-match
entry. MatchAbandon retains that fact for departure diagnostics and player text;
NetSession's actual disconnect formatter uses it. HostLost cause and authority
revocation remain unchanged, so a stopped client cannot become a referee. This
changes no wire schema, score/result computation, recovery, ranking or save data.

Windows Unity6000.5.8f1 native original21484: host and client completion paths
both fail with the exact observed ABANDONED4of8 text; fresh-live-after-reset and
pre-start controls pass. Candidate22748: same4/4 pass. Tests use actual match
snapshot/end methods, return the local8-round rules, invoke departure and the
shipping UI formatter. They verify the completed4-round count, correct wording
and retained authority revocation. Raw results and compact receipts are adjacent.

Both parents exited, settings/input/Quality were restored and202 generated UI
metadata changes per run returned to frozen bytes. Current packaged result-board
host/client departure remains a separate acceptance check. The laptop is owner-
paused/off; no new separate-machine or non-host human-motion pass is claimed.
