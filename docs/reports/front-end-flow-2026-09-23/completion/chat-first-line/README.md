# First match-chat line clipping, 2026-09-23

The127.3 follow-up exposed a real content-fitting defect, separate from the already
corrected right-hand anchor. `SetNativeLines` activated a row and immediately
called `Ellipsise`, before the vertical layout group assigned its width. The
shortened string remained in the match row, so a message with ample eventual room
displayed only `LOCAL...`.

The fix sets the visible rows and their heights, rebuilds this small chat column
once on message receipt, then measures/fits the text. It does not add a per-frame
layout pass, change the chat design, or change networking and message limits.

Focused check: `LobbyChatStripProbe.FirstMatchLineUsesItsLaidOutWidthAtLargeHudSize`.
It attaches the real in-match chat, supplies its final corner, then delivers the
message before the first layout frame at HUD1.2 and Larger text. It checks readable
content, rendered characters, height and right-hand anchor, with1680x720 and960x540
captures. Existing suite registration already includes this fixture.

- Baseline runtime52ef62443-equivalent:0/1passed,15.997seconds. Message was `LOCAL...`.
- After targeted runtime fix:1/1passed,8.347seconds. Full sentence rendered.
- Both captures personally inspected; corresponding25percent greyscale thumbnails
  retained. These isolate the HUD on the capture helper's flat background, not a
  claim about map rendering or native-player pixels.
- Guarded Unity6000.5.8f1 in the owned QUALcheckout at47d5502cb plus the recorded
  selector inputs and this focused change. Named profile and shared input restored.
  Generated meta/ProjectAuditor churn restored after preserving each diff.

The existing native accessibility runner still follows retired pre-hub menu routes.
It was not run blindly or expanded into another fixture-migration campaign. The
original127.3 native-player confirmation remains in P7. No full regression, native
chat transport or human gameplay claim follows from this focused Editor result.

Stop condition met. No further unchanged captures or test reruns for this fix.
