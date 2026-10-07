# Measure release-player timings without false allocation zeros

Ordinary opt-in performance capture was unnecessarily restricted to Development
players. Nonbinary wall-clock timing now supports the release player. Binary
profiler capture still requires a supported Development build and reports a
normal failed receipt when requested incorrectly. Normal play and tournament
launches do not install these probes.

A retained4096-byte allocation calibrates the current-thread managed counter
once before timing begins. No valid signal reports unavailable and uses-1;
zero no longer implies no allocation. Reports identify backend, Editor versus
player, Development status, wall-clock/binary mode, actual profiler flags and
the limits of current-thread managed measurement. Full BuildOptions still
require the matching build receipt.

Native52924 passes the existing timing capture control and ten receipt/CSV
checks. This Unity EditorMono returned0 for the retained allocation, correctly
producing unavailable status and-1 frame/window values with profiling disabled.
Source-derived managed checks pass four mode guards and five signal cases;
the separate .NET counter measures4120bytes for calibration and8216 for a later
8192-byte allocation. That does not prove the Unity player counter is supported.

The disjoint opt-in package probe compiles in the same native input snapshot.
Its new required-effects flag verifies actual fresh decoded FX at a UI seek,
the owned mesh count and atlas binding; flag-absent legacy behavior is unchanged.
Actual packaged effects and release measurements are the next acceptance gates.

[Exact proof](pc-source-proof.json) verifies three compiled source files and raw
receipts. Native/preservation/restoration workers are terminal and all21,387
inputs/shared preferences restore exactly. Focused gameplay FPS, GPU/native/
worker allocations, full build/player behavior and tournament readiness remain
unproven by this Editor/source-derived check.
