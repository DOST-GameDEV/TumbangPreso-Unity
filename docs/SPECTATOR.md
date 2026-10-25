# Spectator source and acceptance

Current implementation: `Runtime/Camera/SpectatorCamera`, `SpectatorDirector`,
`SpectatorInterest`, CameraRig/ViewmodelArms and the spectator UI/input owners.
Read actual bindings in InputCatalogue/ScreenInputCatalogue and the live HUD.

The owner reopened spectator work on 2026-10-25. Desktop controls and camera
pacing now have a focused native pass; [evidence and remaining scope](reports/spectator-2026-10-25/README.md).
[TODO](TODO.md) owns the work; this page records its contract and source route.

Tab opens grouped controls using live desktop bindings. Space/right trigger fly
up; T/Select cycles match speed; equals/right-stick press cycles framing/fly speed.
Gamepad B recalls a saved camera position without also hiding the HUD. Camera
movement/aim/drift are slower; ordinary event changes preserve a 4.5-second view.
Height follows court elevation. Followed first person now copies the active
player rig's eye, full rotation, FOV and hand state through a bounded optional
host relay. Missing/stale streams visibly fall back to third-person follow;
bot/reference views are labeled. [Actual-view evidence](reports/spectator-2026-10-25/view-candidate.md)
includes three real Windows processes. Broader framing and private-effect coverage remain open.

- Manual free/follow/first-person/third-person controls must be understandable
  on desktop keyboard/controller. Help carries controls, without a wall
  of unrelated spectator information.
- Automatic shots hold long enough to understand the play, preserve useful
  framing/visibility and yield to manual control. Autopilot never requests pause
  or replay on its own.
- Follow/POV observes a selected player's actual look, eye pose and relevant
  first-person presentation without activating their input or controlling them.
- Spectators see accepted skills, ghosts/companions, held/loose/thrown slippers,
  can pickup/carry/reset/drop, cast/contact feedback, ultimates and authored
  cutscenes. Check role/round/target departure and perspective transitions.
- Human pause/speed/replay remain deliberate controls using the existing host
  authorization. Peer-departure notification belongs to the network work.

Validation must include native graphics and target/event transitions, plus real
observer peers for replication. No claim that current spectator work is complete
is implied by this source map. Update this document with final behavior/evidence.
The separate mobile refresh follows NETWORKFIX; preserve existing touch support
in the meantime. Mobile work is explicitly assigned, not a universal per-feature gate.
