# Custom-room arrows

MAP, GAME MODE and VISIBILITY now show previous/current/next on the existing
room form without a popup. Options, default indices, selected-map callback and
HostRoom arguments stay intact. Only the selector presentation changes; network
creation remains owned by IHubHost. Standard HubButton preserves focus, sounds,
pointer/touch and keyboard/pad Submit. Choices wrap at both ends.

Hidden D3D11 Unity31336 on basec53a3d1a1 plus the frozen source hashes passes2/2:
RoomArrowChoiceTests cycles every option with real graphic raycasts and pointer
click dispatch, checks exactly one callback per action, Submit on Previous,
horizontal navigation, no popup, the live selected-map callback and Back.
OwnerLoginFeedbackTests also passes its existing local/server-verdict controls.
[1080 frame](room-1080.png) and [720 frame](room-720.png) pass action/text bounds.
All21230 source inputs and shared preferences restore after parent termination.

An initial fixture attempt Unity23368 had two CS0117 compiler errors from calling
nonexistent SceneFlow selection methods in teardown. Replacing those calls with
the actual public properties fixes the fixture. No runtime was exercised by that
failed attempt; its log and restoration evidence remain in local Logs.

This qualifies the foreground choices and callbacks, not physical-device input,
room creation or peer networking. The staged background still shows Home footage;
the new recorded map-preview implementation/playback is the independent laptop
lane. Desktop770 remains preserved. Existing other dropdowns keep their popups.
