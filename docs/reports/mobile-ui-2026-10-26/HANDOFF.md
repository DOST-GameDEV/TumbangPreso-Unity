# Mobile UI handoff, 2026-10-27

The owner requested wrapping up and handing off unfinished work. This is a
snapshot; [TODO](../../TODO.md) remains the only active queue.

## Source and download are different states

- Branch: `mobile+network`, separate from main. The final fetch found NEW main
  `43c158b867bd45c80a0418a9a2e597a5667338a5` (Zack speed/recharge/zap polish).
  It is not integrated. The branch includes earlier main078f98e87.
- Built Android candidate: `ff9aafafdc887145ea895e500328ab333e8b555a`.
  [Download candidate](https://drive.google.com/file/d/1VJ0ktaPibFREgKZGoGemVugpp3itiMeq/view?usp=drivesdk).
  Filename explicitly says CANDIDATE. Private sharing unchanged; exact size and
  destination folder verified after browser upload.
- APK:1,677,245,048 bytes, SHA256
  `6cf28fcaee05b933e3191e498a6a300887c0da0ecf2921c89029d0f1fae15472`.
  ARM64 IL2CPP, Android API26 minimum/target36. One APK, no OBB.
- Later source patches compile but are unfinished and are not in that APK.
  Their focused native run passed6/7; the picker viewport check still fails.
  Do not describe the current source as release-qualified.
- Existing Windows player remains code0d8. A new Android/old Windows peer run
  has not qualified the ff9 candidate. Earlier0d8 peer results retain their scope.

## Completed evidence

The ff9 source passed41/41 focused D3D11 PlayMode checks. Its APK built and
installed successfully. Actual normal Android launcher/Guest/Home, new menu,
settings, keyboard Back ownership, Keep Editing/Discard, touch-layout drag/Cancel,
tutorial Ready/look/move/run/three jumps/legal charged throw/retrieval/curved throw,
held Info and player Menu/pause were exercised. Info completed its real lesson.
Complex tutorial objectives09 through15 were skipped through the normal UI to
reach Info; they were not passed. The full20-step route and full match/result
journey are not certified.

## Unfinished work, in priority order

0. Integrate/review main43c158b86 safely before another final build. Preserve the
   owner's newer Zack tuning/presentation; reassess gameplay-contract compatibility
   and matching Windows/Android binaries after that integration.
1. Fix the device picker. The first Keyboard & mouse choice remains clipped.
   The attempted spacing/padding correction did not pass the new test:
   `PhoneSettingsExposeArrangeWithoutScrollingAndKeepNestedReturnPaths`,
   native09-handoff:Option0 top311.9999 exceeds viewport top241. Investigate actual
   content geometry and focus scrolling; preserve the strict visibility/pointer test.
2. Recheck the other newer patches on Android: larger-text Player field above
   the IME; curve buttons moved clear of Interact; compact charging tutorial;
   hide preview cooldown control; scrolling phone Info sheet above gameplay controls;
   touch-generated legacy mouse excluded from player/spectator look and takeover.
   The ff9 screenshots prove the original defects, not the corrected Android behavior.
3. Run focused native checks again, inspect real rendered references across current
   kits/roles and larger text, then build a new canonical APK. Recheck stationary
   two-finger aim, held Info/scroll/input blocking, picker, layout save/reset, pause
   and normal gameplay on that exact binary. Do not reuse ff9 as proof of new patches.
4. Finish normal short-match/results/replay and affected spectator/crossplay flows.
   Reuse unchanged map/earlier network evidence with its original binary scope.
5. Replace/update the Drive candidate only after qualification; record its actual
   source, size/hash and metadata. Keep the previous package for rollback.

Physical phone performance, thermals, separate-machine Wi-Fi discovery, WAN/Relay
remain unqualified. Account submission/terms acceptance was not performed.

## Resume evidence and safety

- Source native logs: `Logs/mobile-ui-1026/native08.xml` (ff9:41/41),
  `native09-handoff.xml` (newer source:6/7). The existing desktop full-settings
  capture also fails on unchanged baseline03 with a destroyed-canvas exception.
- Actual ff9 screenshots/logs: canonical `Logs/mobile-ui-candidate-1026-11/`.
  Screenshot23 proves the large-text IME issue;43 the curve overlap;53/54 the
  stationary second-finger camera jump;58 the Info/control overlap;59 Info completion.
- Build preservation receipt is in the task's
  `work/canonical-before-mobile-mobile-ui-1026-11-android/`. All127 original
  canonical dirty files were restored by hash. Worktree126 importer/settings changes
  are unrelated and unstaged. Never reset/clean or stage those files.
- Emulator/app and native validation are stopped; touch visualization restored off.
  One heavy job at a time, named profiles, explicit canonical build output, preserve
  existing playables. Source worktree and exact local paths are in the output handoff.
- Use purpose Android input. The task-only SDK `TumpTouchDriver` supports real
  two-finger injection; the kernel sendevent experiment is not product evidence.
- No new mobile mandate applies to unrelated future sessions. Protected authored
  kits/art and main's approved changes remain intact.
