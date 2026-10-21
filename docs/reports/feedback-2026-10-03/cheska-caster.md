# Absolute Zero caster exclusion

The new Feedback human note says Cheska must not freeze herself. This supersedes
protocol 103's every-player targeting. Exclude only the casting motor from both
Frozen and its thaw Chilled status; other players keep the existing 1.5s windup,
2.5s freeze and 5s thaw slow. Other Cheska players are still valid rivals.

## Native reproduction and correction

Relevant source from 7501fae26, staged on the isolated validation checkout
with Linux Unity 6000.5.8f1: the focused
AbsoluteZeroFreezesRivalsButNeverItsCaster test fails at caster exclusion. Rival
and windup controls pass before that assertion. The same test passes after the
single target-filter correction, 1/1 in 0.379s. No test threshold was relaxed.
The old full-map test is aligned with the latest requirement but was not rerun;
this result is the new lightweight native stage, not full-map/player/peer proof.

Candidate peak tree RSS 4,306,317,312bytes; cgroup 7,199,440,896bytes. No resource
stop. Named profile restored and isolated EditorSettings restored exactly to
SHA256 a926892106cfb3220e513f7fe4caee34e9cc5d201d13446683cc99f785dd2c3a.
Compile/import and runtime were separate processes. Original failure retained.

Protocol 135 marks the gameplay contract change, with no new packet fields.
Use matching rebuilt clients. In-game descriptions remain under the owner's
existing copy lock. The Wiki records the corrected target rule. New actual-peer,
refreshed player and human play verification remain open.
