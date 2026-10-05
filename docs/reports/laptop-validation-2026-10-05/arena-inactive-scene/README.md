# Arena recovery activation follows scene ownership

An enabled ArenaFallRecovery in a loaded inactive scene installed global fall
and vine callbacks even though it did not claim ArenaFallRecovery.Instance.
OnEnable now returns before both ownership and hook installation outside the
active scene. Active-scene setup and existing current/retired teardown stay intact.

Base57b2eb1ec; laptop gamergmae Windows11/Unity6000.5.8f1, isolated qa-a.
Original Unity11620/parent61704: one causal failure and six passing controls.
Candidate Unity37292/parent75124: seven passes, no skips. Both are terminal;
21126 inputs,279 generated deltas, QualitySettings and13 editor preferences
were preserved/restored. Candidate source and fixture bytes match their manifest.
Existing fixture metadata is unchanged. A missing-baseline orchestration error
before the first launch is retained; it produced no Unity job or behavioral result.

The new causal case creates an actual additive scene, moves an inactive object
there, adds recovery and enables it through GameObject.SetActive. With no active
recovery owner, the original installs callbacks; the candidate leaves them null.
The second new case verifies an existing active owner's hooks remain installed.
The previous five current/retired stage/recovery teardown controls also pass.

Initial active-hook takeover reasoning was too broad: Updraft/VineCatch are
static and the same callback values remain when an active Arena owns them.
No alternate production writers were found. This fixes the activation ownership
contract; it does not prove an observed gravity/catch failure, ordinary production
additive loading, physical gameplay, QA Relay causality or tournament readiness.
It leaves all current map recovery numbers, kit behavior and art unchanged.

The native input set includes the unchanged additional Home readability fixture
from the previous UI unit, while published original HubHome is restored. Only
ArenaOwnerLifetimeTests ran; no Home or complete-map acceptance is inferred.
Current152 player/operator and matching peers remain separate.

Production callers load maps additively in MapPreviewSurface and SplashScreen.
Preview claims and parks map roots after the load completes. This test exercises
the component activation boundary rather than executing the entire preview or
prewarm flow. The metadata receipt distinguishes raw CRLF from normalized LF;
script/GUID content is unchanged.
