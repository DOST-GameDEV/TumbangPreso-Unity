# Original Hero reconnect result and its limit

Frozen Windows release2664 runtime7342fa1b0720960a2238c537142ed826fa8ac90e0591751ae7ceb0b4f8286a00,
local host/client with150ms one-way UDP delay. Same-process rejoin reached round2;
both sides recorded49 buffer samples and zero invalid warmup samples. Original
aggregate remains FALSE: client did not retain its expected seat.

Client434 rows include421 seat1 rows and13 seat0 rows. Twelve seat0 rows are
inactive round0/pre-admission. One live seat0 row combines the previous round
reference with LocalSlot reset during Rejoin's synchronous transport restart.
The probe starts the new arena before waiting for admission. Production seating
applies the assigned seat before arena entry; no host-seat corruption was shown.
This is a diagnostic observation failure, not a passed seat-retention check.

The actual supported join panel separately reports transport startup as successful
room entry. Its product fix/acceptance is tracked separately and does not change
this frozen artifact or retroactively turn this result green. Preserve these
original traces when qualifying an admission-aware future probe.

Own player/link processes retired; profile/input guard restored. No real WAN,
physical devices, default full Hero tournament or paid service established.
