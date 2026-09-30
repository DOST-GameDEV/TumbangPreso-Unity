# Dante ward cue result

The existing drawn DanteShield symbol now accompanies the active stone ward.
It is a36cm camera-facing badge, above the head and below the existing status
row, with a short restrained settle and final0.18second fade. It uses the ward's
StepTo age and duration directly, including restored and recorded playback.
It has no independent clock, status flag, physics, gameplay or wire change.
The wearer's actual first-person camera hides it; observer cameras see it.
Ending the ward destroys the detached badge. Existing plates/orbit and shared
icon art remain unchanged; no sounds, HUD duplication or ability text edits.

## Native verification

Unity6000.5.8f1 Linux graphics, guarded isolated profiles.
Missing-cue baseline fails as expected. Final contract passes1/1 in8.28seconds:
actual Dante model, existing sprite identity, both15/20second assigned durations,
live and recorded construction, two viewing sides, active/last-frame/expired/
backwards/negative samples, no colliders and complete detached-object cleanup.
The first-person assertion requires IsLocalFpp to be true rather than silently
skipping that branch. No guard stop or new OOM kill.

The earlier7.78second pass is retained, not counted as another distinct case.
The actual-court image was inspected: the small shield reads clearly against the
warm street and does not cover Dante's face. Other actors overlap the composition,
but the cue and fitted ward are visible. This is a native render/lifecycle check,
not an integrated player run, actual-peer qualification or human final approval.

The current SHIELD20seconds versus Wiki Unstoppable15seconds discrepancy stays
owner-reserved. Accepting15seconds as a test input does not change live rules.
