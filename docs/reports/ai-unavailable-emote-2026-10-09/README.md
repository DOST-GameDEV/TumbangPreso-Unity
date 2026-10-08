# Bot social pauses for unavailable emote motion, October9

Original native69472 reproduces a real consumer mismatch: basic CanEmote permits
an eligible actor whose rendered rig is unavailable, but Play refuses the clip.
StepSocial still clears movement/gameplay keys and consumes the frame, then holds
for the250ms delivery grace while no animation plays. This fixture deliberately
uses an unavailable rig; it does not establish which current player rigs lack
particular emote assets.

A read-only clip availability check now runs before HoldStill. An unavailable
request is retired with the normal social cooldown and leaves movement/gameplay
keys untouched. Playable requests retain key ordering, the existing bounded
network delivery grace and ordinary emote eligibility. No request/broadcast
signature, authored pose, clip data, hero behavior or network protocol changes.

Candidate native66348 passes6 controls: unavailable rig preserves movement/Grab
and creates no hold, actual Rafi-rig dance still starts and holds, a controlled
pending playable request keeps the early grace then resumes after no delivery,
and3 existing lesson/unrestricted eligibility cases. The pending-delivery control
simulates scheduler state and is not actual-peer acceptance. All21,455 protected
inputs/preferences restore. The fixture restores random state between cases.

Actual current Windows rig availability, natural1x useful bot movement/actions,
all-map/role/kit/full-match efficacy and matching peers remain open. This bounded
fix prevents one concrete no-animation pause; it does not explain every reported
stare. Installed Desktop/internalG remain e1c1 and will receive this and the ground
support correction through a single next batched replacement.
