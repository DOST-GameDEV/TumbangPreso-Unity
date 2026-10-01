# Existing slipper beam qualification

Runtime implementation851f8ca8 was already present. No second implementation or
appearance change was made. Current beam, shader and original probe matched the
isolated candidate byte-for-byte before testing.

The existing native SlipperRecallShots.TheRecallMarkIsPhotographedInEveryState
passes1/1 in14.56seconds on Linux graphics. Seven same-camera comparisons at1280x720
and1600x720 measure changed-pixel coverage from0.0051percent to0.689percent, below
the12percent budget, with zero newly-white pixels. The test checks real recall
states, shader use, chosen highlight color, pickup handover and fade positions.
Near and side frames were inspected: the blue line and compact pool locate the
shoe without whitening the road. The UI recall marker can sit in front of the
line by design; it remains a separate useful interaction cue.

TODO had promised a far frame that the source probe did not actually produce.
One capture-only fixture addition now takes an11m side-camera frame. It shows the
thin line clearly while preserving can/chalk/players. The extra run produced its
frames and coverage but was stopped by the memory safety guard before a final
XML result. No new OOM kill occurred. It is partial capture evidence, not a
second passed test or a performance qualification. The original passed contract
still applies to unchanged runtime. Do not repeat it just to hide this limitation.

The far frame also contains genuine bot tag/bonk feedback and camera-obstruction
stippling; those are not beam pixels. Coverage uses paired frames to isolate the
effect. Human visual approval, actual peers and new integrated player are separate.
