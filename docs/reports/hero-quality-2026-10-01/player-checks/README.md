# Current Linux player qualification

Built the owned internal Linux candidate from the checked runtime/Core overlay
through5cce9a55, protocol112. Production GameBuilder gates passed;2077MB/45s build.
The isolated checkout correctly reports base831368991/dirty, not a fictional clean
5cce9a55 checkout. Runtime-input hashes are retained with no build-time drift.
Runtime DLL SHA256:67e444f5f668430913d517fc9eaadd2186f27c7ad426d29b58f39501ac988ff5.

Actual player opened through its normal UI in an isolated QA home. Observed via
native desktop controls/screenshots: guest entry, title/Home, Learn to play,
visible hollow aim circle, Ready advancing to Look, measured walking/running,
jump progress thirds advancing to Throw, an actual throw reaching Retrieve with
a loose slipper on the ground, offline Escape pause/Resume, tutorial Quit back
to Home, and normal Quit Game. Exit0 after852s, no guard termination or new OOM.
Look was skipped intentionally; this is not a full20lesson tutorial qualification.
No physical pad/touch, Windows, actual-peer, full-match or human taste claim.
The supplied mouse-drag input also changed view direction substantially, so this
session is not a controlled charge-animation timing measurement; native focused
UI tests retain that narrower evidence. No audio quality claim: cloud output-device
initialization failed, so silent visuals cannot certify the SFX mix.

## New observation requiring correction

A Return press used to leave the title reached Home already searching for a match.
The unintended search was cancelled. The player log confirms its temporary lobby
was deleted; a stale update then reported lobby-not-found. This session therefore
was not completely network-side-effect-free despite the intended offline route.
No external player match was observed. Do not conceal the unexpected search or
claim it was an intentional multiplayer acceptance test.

The next focused investigation is title Submit leaking into Home's selected PLAY.
Existing any-key evidence used Period, which cannot reveal an Enter/Submit leak.
Reproduce with an offline native input test, fix the smallest transition path,
and verify fresh Submit still works after the opening press is released.
