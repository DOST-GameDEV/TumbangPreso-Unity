# Lighting names, order and defaults reconciled

This was already implemented in the branch before the Feedback intake. Current
source and BUGS-0926.5 record the owner's clarified choice: Bright becomes
Standard/default/first; Classic becomes Nostalgic/second. Coming soon stays disabled.
Legacy saved indices migrate once so existing players retain their chosen look.
No lighting values, artwork or current implementation were changed in this unit.

Current Linux Unity6000.5.8f1 native graphics checks pass2/2 in10.39seconds:
WorldCourtCueTests.LightingStyleThumbnails checks real world-look application,
authored ambient/fog restoration, default, both legacy migrations and retained new
settings. TumpNativeSettingsTests.LightingStyleCardsSwitchTheLookAndJoinSaveAndDiscard
checks current cards/thumbnails, disabled placeholder, live selection and discard.
Its save-related coverage is the transaction becoming dirty; it does not claim a
new save/relaunch test. Captures1920x1080 and1600x680 are retained. Short-wide cards
and both actual-map lighting renders were inspected. No guard stop/new OOM kill.

Evidence source583e2ec5 plus its documentation checkpoint, isolated overlay with
the newly integrated bot-hop source. Raw test/guard receipts and captures are in
lighting-checks. This closes the stale settings row without another implementation.
Human approval, physical devices and a new integrated player remain separate.
