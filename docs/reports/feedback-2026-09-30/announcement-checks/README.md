# Current announcement scoring

Current Feedback replaces paid accuracy milestones with first/late/multi knockdown
and single/double/triple/multi catch recognition. First credited knock in a round
pays50; any credited knock with10seconds or less pays50; third and later knocks
since tag/round pay50. Independently qualifying criteria stack. Misses leave this
paid streak intact; the older accuracy statistic still tracks misses/blocks but
no longer pays its retired10/20/25 reward schedule.

Catches use a five-second rolling active-play window: first0bonus, each subsequent
accepted tag25. A naturally recovered repeat victim counts, duplicate event IDs
do not. Every accepted can-down, including an uncredited one, resets catch timing.
All points pass through MatchDirector.AddScore. Normal/Sprout base awards and
normal ultimate charging remain unchanged. Appended enum values and protocol121
carry the new reasons. The replica presentation path never awards score.

Banners last2.5seconds and queue simultaneous qualifications. Hide/round/rematch
clears the queue. Images show actual native recognition; filename accuracy is
retained from the existing probe, while the visible text is MULTI KNOCKDOWN.

Evidence:
- Core8/8: values, repeated victims, five-second boundary, invalid/duplicate events,
  can/tag/round resets and independent knockdown counters.
- First native4/4: Classic/Hero actual flights and punches, base ultimate charge,
  stacked first/late/multi, neutral can-down reset and replica/queue idempotency.
- One additional natural-recovery case initially timed out90seconds because its
  fixture disabled RoundDirector, which owns Hitstop.Step. This also explains the
  prior Hero case taking137seconds. One bounded fixture repair restored stepping
  and bounded real-time waits; no product rule was changed to satisfy the fixture.
- Final repaired2/2 reruns only first/late and the new fourth-catch/Sprout case.
  Retain the three unaffected first-run cases: five distinct native cases total,
  not one5/5suite. ScoreFeedback arithmetic was updated but its full case not rerun.

Final frozen inputs match the isolated candidate and owned source. No new OOM.
No new player build, actual remote-peer test, physical-device or human acceptance
claim. The client test uses the existing provider simulation and proves only
local replica idempotency/no-score behavior.
