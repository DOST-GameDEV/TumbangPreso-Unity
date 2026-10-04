# Current custom rules reach joining players

Actual standalone LAN testing on cf405096c selected Hero Strike, four rounds and
30 seconds on the host before the client joined. Both saved full records matched
at four rounds but the client result header and completed-departure denominator
retained its local eight-round default. See the adjacent local-lan151 report.

The custom Rules screen already publishes each change. HandleIdentify sends mode,
map and difficulty to an arriving peer but omitted the full rules. A peer arriving
after a change never received that earlier broadcast. The fix sends one existing
SyncRules payload directly to that peer before seating can start its arena. It
also replies on repeated introductions without adding a whole-room fan-out or
changing host preferences. The existing serializer excludes the password.

Two native PlayMode tests use a real listening host and its named-message
loopback transport to observe first-arrival and retry replies. Both fail before
the fix for the missing reply. Both pass after it, checking four/then-six rounds,
30 seconds, one delivery per introduction and host-only password preservation.
This proves the arrival sender path; fresh standalone display acceptance remains
open. No WAN latency or scene-absent receiver acceptance is claimed.

Original PID23204 exited2; candidate PID21052 exited0. Both parents are terminal.
19,288 protected inputs were compared. The only intentional production delta is
MatchRpc.cs. Unity's 202 generated importer changes were restored to exact original
bytes after both runs. Shared input/editor preferences, QualitySettings and named
profiles were restored. Pre-existing ProjectAuditorSettings and the untracked
cancelled Yasmin wardrobe draft were preserved.
