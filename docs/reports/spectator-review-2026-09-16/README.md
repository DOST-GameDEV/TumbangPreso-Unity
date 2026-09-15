# Real spectator controls and POV review

Status: this bounded Windows spectator/control/replay pass is qualified. Actual
POV, free-flight and replay captures were inspected. The larger cinematic,
multiplayer-spectator, visual-feel and performance matrices remain separate.

The opt-in --spectator-only driver enters through the actual Watch instead UI,
uses the spectator's real keyboard bindings on a cloned input asset, and leaves
three bots playing while one existing held-item target is kept steady for the
POV check. It does not use the earlier ad-hoc CameraRig witness as proof of a
SpectatorCamera defect. The installer correctly omits the debug seat switcher
in spectator mode, so the function-key POV cut has one owner.

Nativev33 passed autopilot/manual-flight takeover, then failed the real seat2 POV:
the world-held slipper remained rendered beside the first-person copy. Captured
under Logs/spectator-native-v33. Build1135MB/45s, guard7aeb354700d4; PID12960
retired, shared input unchanged. Source cause: HidePovBody only reached character
child renderers, while the carried slipper remains a separate world object.

SpectatorCamera now polls the carried item separately, retains its world shadow,
restores original renderer modes when the item is released/replaced or POV exits,
and applies the existing local-view near-eye flight grace. RestorePovBody also
restores the item on disable/target change. No throw origin, collision, model,
item ownership or network payload is changed. The driver additionally stages
release/re-equip and checks restoration on leaving POV and disabling the camera.

The remaining native sequence waits for actual replay GPU readbacks, presses the
bound manual replay key, verifies a real captured texture in the overlay, and
returns to live view. Do not lower the12-frame floor or treat the older batch
showcase's live screenshots as replay evidence.

Separate visible finding: the current spectator score rail labels one seat You.
TumpMatchReadout.Scores omits the spectating condition on that label/local strip.
Those presentation conditions are corrected without changing role/ownership.
Idle defenders no longer falsely show Retrieving, and POV/replay labels include
seat numbers so two characters with the same name remain distinguishable.

## Additional reproduced failures and corrections

- Nativev34 fixed the held copy and passed release/re-equip, but free flight still
  showed borrowed hands. Its branch never ran POV cleanup. Free flight and autopilot
  now release borrowed body/item/viewmodel presentation; direct F-key POV cuts take
  over from autopilot. Existing timers and camera movement remain intact.
- Nativev35 passed those transitions and captured a real replay. Its exit hint was
  clipped. A focused test also reproduced Escape exiting replay and opening the
  match menu on the same press. Replay now uses the existing ScreenTakeover owner/
  frame-stamp contract. Clear dark header/footer bands use the current fonts, and
  the exit instruction is rendered rather than truncated.
- The same local fixture reproduced fallback polling erasing a scored event's
  known actor in the same frame. A same-frame duplicate fallback preserves the
  known actor; another unknown event remains unknown. Both local regressions fail
  in v1 and PASS2/2 in v2, guard18bf87bc749a; baseline0c5ca20912e0.
- Nativev36 passed bookmark/paused movement/exit instructions/Escape, then failed
  replay proportions after changing to4:3. ReplayFrame now stores the source
  aspect at capture and restores it at playback. Texture dimensions, capture cadence,
  memory budget and12-frame entry floor remain unchanged.

## Final native qualification

Nativev37 PASSED the real Watch instead route, manual flight takeover, POV cut,
held-item release/re-equip, POV/autopilot/free-flight transitions, bookmark/recall,
tactical pause with unscaled camera movement, manual replay from actual readbacks,
visible exit instructions, Escape without an extra menu, replay at16:9/4:3/
ultrawide, and renderer restoration on camera disable. Captures are1280x720,
960x720 and1680x720. The altered-aspect footage was visually inspected.

Artifact: Builds/spectator-review-v37/TumbangPreso.exe,1135MB/49s,
guard2c1d7e32de97. Runtime SHA256:
4D325BF644979A5DFB36F19B9E4F8BFBC882A3EC408BAFF1689B0A0E4224C508.
Native PID13780 exited0; shared input unchanged, zero pre-existing named-profile
files. No task-owned Editor/player remains. Failed receipts and selected original
captures are preserved here with the final run. The only later source edit is a
comment clarifying the now-per-frame aspect contract.

One existing synthetic GPU warm-up request reports an error on this driver; the
real frame readbacks succeeded, and the native check required FailedReadbacks=0.
This is not a claim of zero warnings, physical keyboard/gamepad certification,
network spectator/rejoin coverage, a complete event-selection audit or all 134/152.4
criteria. The held target and release/re-equip transitions are explicitly staged.
Near-eye flight grace shares the existing local algorithm but was not separately
captured as a flight-timing window in this native sequence.

A subsequent focused local contract PASSED1/1, guardd6f6d93f54b1. It uses a real
slipper's held/flight states and renderer modes: the released mesh stays hidden
near the spectator eye, restores its original mode at3metres, and is not retained
after changing to another target. This closes that local geometry/state contract;
it is not an additional native flight-timing capture. Receipt: flight-visibility.xml.
