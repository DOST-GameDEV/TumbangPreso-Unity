# Existing Title And Home Routes, 2026-09-30

One focused native case passes1/1 in9.7102842s on the current source candidate.
An actual non-submit Period key invokes the shipped title's full-screen door and
opens Home. The shared Back action opens the hamburger without returning to title;
a second Back closes it. ShowingHome remains true under that popup, preserving
the background-selection route. No duplicate frontend code or art was added.

The case uses real Input System key events for title entry and the actual hub
Back API for menu navigation. Physical Escape routing, visual backdrop comparison,
queue-button cancellation and stamina-direction rendering were not separately
qualified by this case. Those limits are retained in F0930-13, not marked complete.

- [Native result](checks/menu-route.xml)
- [Fixture inputs](checks/menu-route-inputs.json)

## Escape And Stamina Follow-Up

The frozen ecd6b8348 source plus FeedbackHudMenuTests passes2/2 native PlayMode
cases. Escape key events open and close HubMenu through the shipped input route,
keep MatchSetup active and preserve the actual enabled RawImage texture. Reduced
UI motion is temporarily enabled to hold the poster stable; moving-video texture
continuity and a physical keyboard are not independently qualified.

Spending actual local-player stamina reduces the real CanvasRenderer fill mesh
from near-full to half. Its upper boundary retreats more than20local units while
the lower boundary stays within2units. This qualifies depletion direction, not
every HUD state or a visual redesign. Queue-button cancellation remains open.
No runtime code, artwork or loading code changed in this follow-up.

The first run stopped at a fixture compile error: this Unity version has a
parameterless CanvasRenderer.GetMesh returning its borrowed current mesh. One
bounded correction used that signature and left the renderer-owned mesh intact.
The retry produced fresh XML with both cases passing, durations3.056145s and
5.719465s. The original failure log stays in the isolated validation checkout.

- [Native result](checks/hud-menu-retry.xml)
- [Fixture inputs and repair](checks/hud-menu-retry-inputs.json)

## Queue Button

The actual PlayButton starts a casual Classic search, changes to IN QUEUE and
stays pressable. Pressing it again cancels the Matchmaker, clears QueueRoom and
SceneFlow.Networked, restores PLAY and keeps Home in MatchSetup. The same button
then starts and cancels casual Hero Strike. One focused native case passes1/1 in
4.240036s, with no tooling retry or runtime edit. This closes the permitted
already-labelled-fixed menu/stamina checks in F0930-13.

This batch run disables UGS sign-in before any authentication call. It verifies
the real offline search/button cancellation path. It does not establish teardown
of a live Relay room, ranked behavior or remote-peer cancellation. The named
profile/preferences were restored and the run's listed inputs stayed unchanged.

- [Native result](checks/queue-button.xml)
- [Fixture inputs](checks/queue-button-inputs.json)
