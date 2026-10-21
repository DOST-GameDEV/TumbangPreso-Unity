# City ambience trim

Harry requested a 25 percent reduction for Kanto and Ilalim ambience. Their
existing city bed, traffic engines/horns/sirens, sidewalk voices and passing
train now apply one shared 0.75 gain factor. Authored clip gains, spatial
attenuation, fade envelopes, replay/preview suppression and the live SFX slider
remain in place. No audio assets, skill cues or other map systems changed.

## Evidence

Five native Unity 6000.5.8f1 EditMode cases reproduced the previous gain and then
passed with the trim. Two city-bed levels and two train levels exercise the
actual AudioSource update paths; the fifth exercises the sidewalk live-mix
route. Train authored gain is also unchanged. Baseline: 0/5, all expected gain
mismatches. Final: 5/5 at 02:12:52 UTC, no skips; outer exit 0, 40 seconds,
no memory-guard request. Fresh result XML and exit receipts are retained here.

The isolated candidate was 9590f4bd with explicit previously validated overlays
and these exact four source files; source-hashes.json records their identity.
This is a measured linear gain reduction, not a claim of 25 percent perceived
loudness. No listening approval, target-player build, performance or real-peer
qualification is implied. Existing replay/preview conditions were preserved by
inspection rather than newly qualified through this five-case check.
