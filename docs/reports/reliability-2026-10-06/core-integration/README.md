# Current engine-free integration gate

Source 30ffb90abc5bc9e597994fd8b32885381e1f24e9 compiles its actual embedded Core
package to netstandard2.1 and its existing test project to net9.0. The fresh full
suite passes 729 tests, fails 0 and skips 0. The TRX contains 729 actual passed test
results; this is not an exit-code-only or empty-suite result.

The 111 tracked Core/project/test source hashes match this Git candidate after
LF normalization, with no Core source dirt. Tests include balance, account,
telemetry, progression, match invariants/records, social, movement-budget and
other engine-free rules. NuGet restore was not run; installed dependencies were
reused. No Unity/player process, saved profile or shared preferences was touched.

The runner's 163ms is test execution time, not game loading, frame time or a
performance improvement. This gate does not compile Unity scripts or qualify
imports, rendered UI, physical input, actual skill effects or live transport.
Those requirements remain open in TODO.

The test process is terminal. Its orphaned compiler server 23340 was stopped only
after checking its exact SDK 9.0.317 command, parent 17044 absence, creation during
the test launch and no other live build clients. Owner game 8204 and its crash
handler remain untouched.

[Raw TRX](core-current.trx), [receipt](receipt.json) and
[qualified source hashes](source-lf-sha256.json) preserve the exact evidence.
