# Seat transfers preserve the surviving input producer

Switching player → spectator → player within one frame could reuse a reader
already queued for deferred destruction. Returning from a bot-controlled seat
could likewise skip a replacement brain. The character then lost its producer
at the end of the frame and the host could retain the destroyed cached owner.

MatchInstaller and the online rebind loop now retire disabled readers and reuse
an enabled survivor or create its replacement. CharacterMotor prefers enabled
replacements while preserving a paused producer's existing ownership if no
replacement exists. No wire messages, hero behavior or protocol change.

Eight native PlayMode cases: the original produced four causal failures and four
passing controls; the first candidate passed all eight without fixture repair or
retry. XML hashes were checked before copying. All five published source/test
files match the qualified candidate byte for byte. Both jobs terminated and
restored preferences; the worker restored its prior files and 2,917 protected
inputs were unchanged. Raw original failures and candidate XML are retained here.

These exercise actual deferred component destruction and simulation ownership
with a dormant transport. Physical controls and live-peer role changes remain
separate acceptance. This fix is excluded from the frozen protocol134 1003g build.
