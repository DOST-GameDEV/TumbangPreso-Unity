# Hub admission gate and pending friend route

The hub treated a listening client transport as an entered room. Its room observer
could replace the join screen before the host admitted a connection and seat,
removing the pending or refused join feedback. NetSession now exposes IsAdmitted;
the hub's InRoom property uses it. Hosts remain in-room, and clients require both
connection and seat admission, including spectator seats. IsNetworked, authority,
default slots, protocol and gameplay rules are unchanged.

Native Unity6000.5.8f1 PlayMode reproduced that admission defect and the final
focused case passes1/1. It uses a real local client listener, checks that transport
startup and connection without a seat keep HOME, then injects the existing
connection flag and real seat-application entry to check player/spectator entry.
Client stop clears entry; a real local host remains admitted. The assigned-seat
part is a controlled native callback test, not actual remote-peer qualification.

The original three-case baseline is preserved as baseline.xml: one admission
assertion reproduced the product defect; two friend-route cases failed before
dispatch because the fixture searched the wrong UI parent for a Friends row.
Those two failures are not evidence that the candidate friend route works or
fails. The first launch stopped before tests at a SceneHandle-to-int compilation
error. One bounded fixture repair inferred the native handle type; its original
source, input manifest and error are retained. No second fixture repair was run.

Final execution selected only
TumbangPreso.PlayTests.PlayerHubFriendJoinTests.HubEntersClientRoomOnlyAfterConnectionAndSeatAdmission,
with unchanged repaired fixture input. Afterward the same admission method,
setup/teardown and port helper were packaged as HubAdmissionTests; the two broken
row-harvesting cases were removed from the default suite and kept in
executed-three-case-fixture.cs.txt. Packaging was not rerun. Executed and packaged
source hashes are recorded separately.

The friend button also has a source-inspected candidate correction in PlayerHub
and HubCustom: reuse the current hub's join screen with its known code, existing
controller, refusal and Back handling. This avoids the old scene request that
only ran after AutoHost, which skips ordinary HOME and an already-live room.
This friend route is pending separate supported handler/operator acceptance.
Its physical click, button layout, service join and actual peers are unqualified.

All runs used the isolated tump-feedback-0930 checkout and named
friend-join-admission1002 profile, D3D11, requested960x540window and Low graphics.
The size request is a resource precaution, not an Editor viewport measurement.
No screenshot suite, external endpoint, friendship or message action was run.
Six frozen execution inputs remained unchanged; the root lobby and SocialStore
candidates retained their recorded hashes. Guard sessions90938/21838/61254 are
terminal, profile/input restoration reported, and owned Unity processes exited.
Raw logs remain in qualification Logs/friend-join-admission1002. The earlier
Hero reconnect evaluator and raw verdict were not changed or rerun.
