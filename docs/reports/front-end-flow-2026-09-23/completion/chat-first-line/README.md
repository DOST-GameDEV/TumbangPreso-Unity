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
It was not run blindly or expanded into a whole fixture migration. A small opt-in
`--match-chat-only` route reuses the existing match canvas and native screenshot
machinery, loads the real arena and checks exactly this message/anchor requirement.

Native candidate7a0ae3363plus that diagnostic built successfully,1278MB/157seconds.
Both1680x720 and960x540frames show the complete sentence over the map and were
personally inspected. Result passed; exit0; shared input unchanged. This closes the
specific127.3 native clip follow-up. It does not requalify all settings, prove chat
transport or diagnose the earlier intermittent shutdown crash. The direct-scene
warmup capture includes nearby character geometry in the foreground; it is not a
full ordinary-entry camera/gameplay assessment.

Build identity is honestly dirty: diagnostic source plus the known four generated
recovery/swim outputs and two original PNGmeta whitespace changes. Source hashes
and artifact details are in native-build-inputs.json. Owned generated churn was
restored only after saving the complete diff; existing DEVmeta files are untouched.

Stop condition met. No further unchanged captures, rebuilds or reruns for this fix.
