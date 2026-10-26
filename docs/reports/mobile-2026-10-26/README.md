# Mobile refresh and crossplay

Branch `mobile+network`, candidate `0d8bee6ba`. Implemented and validated within the recorded Windows/Android emulator scope.
[Matching builds and hashes](build-validation.json). Unrelated future sessions do not automatically owe mobile updates.

## Changes

- Five primary touch spectator actions, separate camera/match tool pages, working
  replay return and pause/resume controls. Actual held abilities and throw/lunge
  support aiming with the same thumb; immediate skills retain tap behavior.
- Owned, resolution-normalized swipe input; background/rotation cancellation and
  safe HUD replacement. Cutout insets preserve the actual camera aim centre.
- Touch-first settings, swipe sensitivity/invert, fitted safe composition and
  mobile frame caps. Desktop fullscreen/v-sync controls do not block phone settings.
- Negotiated view v3 transmits aspect and clip planes, fits the watched frustum
  without cropping/stretching and preserves v2 peers. Gameplay protocol 183 remains.
- ARM64 IL2CPP, Android 8/API 26 minimum and target 36, landscape rotation, foreground
  Wi-Fi multicast discovery lease and default frame budget of up to 60 FPS.
- Lossless LZ4HC reduced asset payload from 5,607,586,576 to 1,643,858,805 bytes,
  about 71%. Intermediate lint AAR ZIP64 fixes packaging; source art is retained.

## Cross-platform defects found

A one-bit Amihan DRIFT duration difference rejected Android at the strict rule
check. Shared compile-time derivation preserves the Windows value; the check
remains intact. Direct Android capture matches all 2,713 reference bytes.
[Exact comparison](crossplay-rule-difference.json).

A missing view stream left the camera easing back from its fallback angle when
packets returned. Fresh views now return directly to the player's eyes. Actual
Android-to-Windows maximum eye error fell from 2.7 m to 0.021 m.

Cold Android loading filled NGO's 128-packet queue. A bounded 512-packet queue
removed the warning. Two activation stalls totaling about ten seconds motivated
12 seconds of transport silence allowance; deliberate host exits still notify
immediately, and the quality badge warns earlier. Longer translated-emulator
stalls remain a separate performance limitation.

A longer outage then exposed identity changing after late online sign-in. LAN
approval, roster introduction and name/pick updates now use the saved local token.
Relay retains its online identity and handle verification. Native payload-to-lobby
regressions prove the returning LAN player keeps its seat after identity changes.

## Qualified evidence

- All three actual view roles passed at source `582ecc243`: Android player,
  spectator and host with two Windows peers. Phone spectator: 303 rendered samples,
  361 received views, maximum eye error 0.0021 m. Windows watching Android: 449
  samples, 284 received, maximum error 0.0207 m. All nine private roots, held
  marker/beacon, exact payload matches, source lens fitting and cleanup checked.
  [Receipts](crossplay-view-validation.json).
- Native codec/input 38/38; touch/lens/lifecycle 41/41; referee 5/5; compatibility
  4/4; settings and recovered camera 25/25; stable identity/admission 23/23;
  host identity collision/legitimate rejoin 4/4. Core 841/841.
  [Hashes and scope](native-validation.json). Groups overlap; do not sum them.
- Broader hardening run: 61/65. Four unrelated static-switch/action/audio/old slide
  scanner failures remain recorded. The Phaister scanner misses its explicit
  second arm-action argument; protected authored kits were not changed.
- Actual cross-platform pause/resume passed, yellow/red quality states were
  observed, and outages revoked authority and stopped the old Android round.
  Rebuilt same-process and background reconnect reclaim the original seat/match,
  and host loss stops both remote survivors. [Lifecycle receipt](lifecycle-validation.json).

- Actual phone spectator taps operated pause/resume across peers, follow/FPP/TPP,
  camera/match pages, inline help, replay/return and the peer-left notice. The card
  appeared before a manual pause; it did not auto-pause. Host loss retained Home
  notice. [Observed actions](touch-journey-validation.json).
- An ordinary lobby round accepted touch movement and one skill on both Android
  and the Windows host. A throw was refused by the danger-zone rule; no accepted
  throw claim. [Natural input scope](natural-touch-validation.json).

- All six active maps reached live Android rounds with four actor identities and
  inspected HUD renders: Eskinita, Ilalim ng Tulay, Lagoon Cove, Kanto, Arena and
  Payo. A separate Classic round passed with hero-only controls absent. These are
  scene smoke checks, not physical performance or per-map remote gameplay proof.
  [Map receipts](map-validation.json).

All Android execution uses an API 36 emulator running ARM64 through translation,
currently given 4 GB/four virtual CPUs after longer dual-core stalls. This does
not certify physical Wi-Fi discovery, phone thermals/performance, WAN or Relay.
Early black Windows first-capture PNGs are excluded from pixel acceptance;
Android screenshots were inspected and normal Windows UI was viewed separately.
Earlier failed fixtures are retained; none is represented as a passing run.

[Original network evidence](../networkfix-2026-10-25/README.md) retains its dated
Windows scope. Casual play supports crossplay; ranked device pools remain separate.
