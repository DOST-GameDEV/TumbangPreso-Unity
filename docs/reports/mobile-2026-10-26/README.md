# Mobile refresh and crossplay

Branch `mobile+network`; integration validation is in progress. Unrelated future
sessions do not automatically owe mobile updates.

The current refresh adds a five-action touch spectator toolbar, separate camera
and match tools, same-thumb aiming for actual held abilities, input cancellation
on background/rotation, cutout insets and an operable return-to-live replay target.
Negotiated actual-view v3 adds the source aspect and near/far clip planes while
preserving v2 peers and gameplay protocol183. The watched frustum fits the other
screen without stretching or cropping. Seatless referees can request it.

Android uses ARM64 IL2CPP, minimum26/target36, landscape rotation, foreground-only
Wi-Fi discovery multicast lease and an explicit default of up to60 FPS. Lossless
LZ4HC reduced asset payload from5,607,586,576 to1,643,858,805 bytes, about71%.
Intermediate lint AAR ZIP64 fixes packaging without changing authored art.

Actual Windows/Android admission exposed a one-bit Amihan DRIFT duration
difference. Shared compile-time derivation preserves the existing Windows rule;
the strict fingerprint check remains. Fixed Android metadata directly matches all
2713 reference bytes. [Exact comparison](crossplay-rule-difference.json).

Source192b34991 passed Windows-to-Android actual FPP:112 rendered samples,
358 received views, all nine private roots, held marker/beacon and cleanup;
70 owner-to-host and22 host-to-Android exact private payload matches. Wider-screen
viewport width0.8 and lens/clip errors0. Reverse Android-to-Windows transport
worked, but packet gaps exposed fallback-to-FPP easing. The recovery fix and
phone settings refinements passed25/25 native cases; rebuilt peer checks follow.

Prior native evidence: codec/input38/38, touch/lens/lifecycle41/41, referee5/5,
strict compatibility4/4 and Core841/841. [Receipts](native-validation.json).
These groups overlap; do not add them as one run.

Current Android05 and Windows06 builds use source192b34991. Final builds, reconnect,
host-loss, touch journeys and reverse actual-view qualification remain in progress.
All Android execution uses an API36 emulator running ARM64 through translation.
It does not certify physical Wi-Fi discovery, phone performance/thermals, WAN or
Relay. Earlier failed fixtures and the reverse-view defect are retained in local
Logs; no failed run is represented as acceptance.

[Network evidence](../networkfix-2026-10-25/README.md) retains its Windows scope.
Casual play supports crossplay; ranked device-separated pools remain intentional.
