# Spectator camera role lifetime, October 2

Taking a seat disabled `SpectatorCamera` but left its separate Unity `Camera` enabled. That camera can retain the elevated depth acquired while watching and continue supplying the view over the seated player's rig. Returning to spectating found the existing disabled controller and did not re-enable it.

`SpectatorCamera.OnDisable` now disables its Unity camera; the existing `OnEnable` restores it. `MatchInstaller.RebindLocalSeat` re-enables an existing watcher when the authoritative role returns to spectating, while retaining the original creation path when none exists. Only these two product hunks changed. Camera framing, authored hero presentation, visibility rules, input bindings, spectator controls and `SpectatorDirector` remain unchanged.

## Focused native acceptance

Three synchronous PlayMode tests use actual Unity lifecycle callbacks, a real Unity Camera, one minimal body and the public seat-rebind method. They load no arena, send no input events, replace no InputSettings, and perform no transport or service action. The fixture restores action enable states, cursor state, authority provider and launch flags.

- Original baseline: **3/3 causal failures**. Disabling the controller left the Unity camera enabled; the public watch-to-seat route retained that camera; returning to watch left the existing controller disabled.
- Candidate: **3/3 passed**, including the complete public watch → seat → watch sequence and reuse of one spectator camera. The seated input reader is disabled again when watching.
- Baseline native duration: 0.5706253 seconds. Final: 0.4404603 seconds. No fixture repairs or native reruns. One tool-call syntax typo was corrected before any preparation command executed; the subsequent preparation completed with exit 0 before launch.

This qualifies component enable/disable behavior, actual callback dispatch and the tested public role transition. It does not claim rendered-frame comparison, a populated gameplay rig, physical controls, chat-focus behavior, real-peer seating or authored camera quality.

## Provenance and preservation

Baseline source is `f163882cc9ea65d6b4fb2411f2781deb2e885a1b`. Owned paths under `Assets/TumbangPreso` are `Runtime/Camera/SpectatorCamera.cs`, the watcher branch in `Runtime/MatchInstaller.cs`, and new `Tests/PlayMode/SpectatorCameraLifetimeTests.cs` with valid metadata. Original/candidate bytes are indexed by `inputs.json` and retained in main `Logs/spectator-lifetime1002-inputs`.

Both jobs ran in `tump-feedback-0930` through the serial GPU pool, D3D11 enabled, 960×540 window, profile `spectator-lifetime1002`, 1536 MB job budget / 2048 MB reserve and 450-second timeout. Baseline session 12358 used guard 14560 / Unity 15972; final session 18449 used guard 23100 / Unity 23820. Both completed profile/input preservation and released their leases. Both owned Editors exited. All 1,469 protected source/settings hashes remained unchanged, and all four candidate files in main and qualification matched their frozen hashes.

Raw baseline/final XML, job/guard receipts, preparation receipt, input/protected manifests and case-level `result.json` are retained here. Full Editor logs remain in qualification `Logs/spectator-lifetime1002`. Private profile contents and unrelated private assets are excluded. The separately pending Ready/Buffer/Emote chat guards were not qualified by these camera tests.
