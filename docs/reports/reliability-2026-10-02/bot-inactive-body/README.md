# Inactive bodies stop supplying live bot observations

ActorIsVisible checked active hierarchy only in its Haunted branch. Ordinary
Classic/Hero bots therefore kept sampling a disabled registered companion's live
position and included it in their perceived target view. Both native shipping-
mode baselines reproduce that visibility fault.

A shared active-hierarchy guard now rejects inactive bodies before mode/status
checks. Final2/2 preserves active actors and freezes remembered position instead
of following disabled-body movement. Existing out-of-sight memory remains; no
new nullable-position behavior, kit, sensory radius, balance or wire change.

Unity6000.5.8f1 PlayMode/D3D11, isolated tump-feedback-0930, bot-inactive-body1002
profile. Two frozen hashes unchanged, zero fixture repairs; guard restored
profile/input. This proves production observation/visibility in both modes, not
whole-match bot strength or physical/network skill behavior. Native logs remain
in isolated Logs/bot-inactive-body1002.
