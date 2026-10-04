# Friend JOIN uses the existing hub join flow

PlayerHub.JoinFriend previously closed Friends, set PendingJoinCode and reloaded
MatchSetup. That code was consumed only after AutoHost: ordinary HOME skips it
because Networked is false, and an existing hosted room skips it because its
transport is already live. Both native handler baselines reached the existing
join controller zero times instead of once.

The action now opens HubJoin in the current hub with its known code and uses the
existing controller/panel admission path. It returns the stack to HOME first so
Back cannot reveal a vacated old lobby. HubJoin shows its normal pending/refusal
state and cancellation behavior. A known code avoids public-room browsing; choosing
an Internet or LAN list afterward still starts the existing browser. The fallback
for a surface without the current hub remains unchanged. No transport, account,
friendship, gameplay or protocol rule changes.

Native Unity6000.5.8f1 PlayMode: the two handler cases fail causally on the original
PlayerHub/HubCustom source and pass2/2 on the candidate with the identical fixture.
The tests invoke the actual private JoinFriend handler after opening real HOME,
then observe the existing panel's isolated delayed connection boundary. HOME
checks exact code/one dispatch, no scene reload, no unnecessary room browsing,
retained refusal/code and Back. The real local-host case checks dispatch, Back
cancellation and suppression of a later successful completion. The shipped
Friends button's binding to this handler was inspected in PlayerHub.OwnerFriends.

These are native handler/control-flow checks. They do not qualify physical button
clicking, input devices, button layout, a service join or actual remote peers.
No endpoint, friendship, message, account or persisted social-list action was run.
The earlier row-harvesting fixture failures are preserved in the
[admission investigation](../friend-join/README.md); they were not repaired or
relabelled as passes. This later approach measures the production action directly.

Execution used isolated tump-feedback-0930, named friend-handler1002 profile,
D3D11, Low graphics and requested960x540window. The size request is a resource
precaution, not an Editor viewport measurement. Baseline native11.8585s and final
8.2683s are different outcomes, not a performance comparison. The four frozen
source/fixture inputs and five protected qualification files retained their
hashes. Both guards completed and restored existing profile files/shared input;
native PIDs8708/22152 exited. No helper or browser remains owned by this unit.
Raw logs remain under qualification Logs/friend-handler1002. No additional fixture
repair, screenshot suite, broader suite or unchanged reconnect rerun was used.
