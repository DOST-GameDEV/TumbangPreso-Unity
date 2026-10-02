# Cinder Gate implementation plan

Status: implementation beginning; no gameplay or completion claim.

The live Pyro Wiki proposes a three-metre ember line placed within four metres,
a0.35-second warning, up to three armed seconds and35-second cooldown. The first
grounded rival crossing is pushed toward its approach side and consumes it.
Jumping and thrown slippers pass over/through. Use0.8metres as the initial modest
push target, solved from existing friction, not a transform teleport. No score,
damage, stun, root or pickup/reset restriction. Preserve all other Sean slots.

## Causality and integration

- Existing defending skill ID sean_skill2d, host-confirmed cast, existing held-aim
  input for mouse/controller/touch. Reject invalid placement before spending.
- Spawn the visible warning immediately on accepted cast. No extra base windup;
  the field's single3.35second clock owns warning and armed time.
- Finite swept crossing in Core, retaining last nonzero side across a centre-line
  sample. Reset history while airborne or warning so landing/arming cannot invent
  a crossing. Deterministic earliest contact wins if two rivals cross together.
- Resolve only on the host through ApplyResolvedImpact. Preserve confinement,
  obstruction, impairment/immunity and Earthbound; consume once before any later
  body. No gameplay Collider or alternate movement controller.
- Extend the existing bounded reliable dynamic-field route, prepared snapshots
  and render-only recording. Append a new field kind; do not renumber or create a
  Sean-only transport. Version compatibility before publication. Identity/age and
  consumed state must reject stale resurrection and repeated contact.
- Timed HUD reads actual remaining field state; reset, expiry, role and match
  changes retire it. Shared ability cooldown and input remain authoritative.

## Presentation

Use the published all-hero research and Sean's compact craft-like direction.
A braced drawing gesture leads to a thin dark ember seam. Its short warning is
flat and visibly distinct from the armed upward pressure edge. The one accepted
crossing snaps outward toward the approach side and extinguishes the seam;
jumping and passing shoes get no false hit cue. Expiry quiets and sinks. Keep
can/shoes visible; no opaque fire wall. New body action is authored on Sean only,
with its own first-person path. Sound remains disabled pending actual listening.

## Files and checks

Claimed exact existing surfaces: SeanHeroKit.cs; WorldEffectSnapshot.cs;
MatchRpc.RafiWater.cs dynamic-field identity/transport only; NetSession.cs
ProtocolVersion only; RecordedMatchClip.cs field extras; RecordedFieldView.cs;
AIController.cs Sean defending decision only; CharacterAnimator.cs Sean action
registration only; ViewmodelArms.CastGesture.cs new gate entry only (never Amihan
storm-call); tools/author_hero_action.py new Sean gate clip only; team-sean.glb
new gate clip only. New Core SeanGateRules/Tests, SeanCinderGate runtime,
SeanCinderVisual, focused SeanCinderGateTests/Probe and matching metadata, plus
this report/TODO/network-contract/checkpoint. Expand ownership explicitly before
editing another surface. Preserve protected HeroHazards and all other authored clips.

First validate finite crossing/boundaries/airborne/arming and priority as pure
rules. Then native actual cast, warning, one-use impulse, immunity, expiry,
role/round cleanup, aged/duplicate/consumed restore and render-only replay.
Finally matching host/owner/observer players before claiming network-qualified
completion. Use one focused pass and one bounded tooling repair per coherent
validation unit; record failures and limits rather than looping unchanged.
