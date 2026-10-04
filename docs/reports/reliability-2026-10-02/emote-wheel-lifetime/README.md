# Focus loss and owner retirement cancel the selected emote

The wheel had no focus-loss cancellation. OnDisable only cleared AnyOpen while
leaving its instance open, canvas activeSelf and selection armed. Focus loss or
owner deactivation/reactivation followed by a public release could therefore
commit the old selection. Both teardown hooks now use existing Close(false),
closing the instance, hiding its canvas and clearing the selection without
invoking EmoteChosen. Ordinary selected release still commits once.

Three native cases use the real wheel/canvas, public Open/Close, a temporary actual
InputSystem Gamepad and queued right-stick state, processed by the enabled wheel.
No private selection/pointer state is assigned. The fixture detaches only the
wheel's action reference to isolate steering/public release from unrelated held
hardware. InputSystem focus settings and previous current gamepad are restored;
the temporary device and owned UI are removed. Focus is a Unity SendMessage
boundary, not OS alt-tab or hardware automation. No new animation or art.

Initial70589 ran3 failed cases: the two retired selections committed, but the
ordinary control was invalid because the fixture's event count accumulated across
NUnit cases. ONE fixture initialization repair resets that count in Before;
no assertion or scenario was weakened. Original fixture bytes/output retained.
Corrected61530 ran3:2 intended retired-selection commits,1 ordinary selected-release
control passed. Candidate89005 passed3/3 FIRST, same corrected fixture/meta.
No further repair or native retry. All launches followed direct prep exit0/exact3,
Unity6000.5.8f1/PlayMode-nographics/GPU2048MB plus2048MB reserve/450s ceiling.
Guards terminal/restored/no lease. Post93049 terminal0 verifies exact3 MAIN/q
hashes and all18433 protected Assets/TumbangPreso files unchanged. Raw initial/
corrected-original/candidate XML, receipts and owned/preservation manifests
accompany this report. Full logs and source
snapshots remain in local Logs/emote-wheel-lifetime1002.

Acceptance is native UI/input state and public selection events. It does not
qualify rendered layout, physical focus, actual held emote-key release, played
animation, peer delivery, multiple simultaneous wheels or a new Windows build.
Frozen1002k predates this correction and the previous input-focus49 fix.
