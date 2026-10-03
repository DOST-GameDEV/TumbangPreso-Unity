# Nemu asks Kuro to guard the can

Source 7f2f9bdc binds Kuro: Catch to hero-nemu-seance and seance-channel, also
used by Haunt. Those legacy assets perform the old wide lift/inward collapse.
First inspect the real accepted defender cast and companion response before
replacing it. The existing Nemu research calls for child intention followed by
an autonomous companion, not a miniature adult summoner.

## Proposed motion

A small attention turn, one compact forward command toward the can, a short
hold while Kuro departs, then quiet recovery. Keep Nemu's cowl, face, model,
existing floating identity and companion completely unchanged. The body and
owner-view hands use a distinct guard command, leaving Haunt and Sit/Fetch intact.
No new delay or camera takeover: Catch already protects immediately for five
seconds, and Kuro moves under its existing task clock.

## Owned paths

- tools/author_nemu_guard.py: one new authored action only
- Art/characters/persons/team-nemu.glb: append hero-nemu-guard, preserve every
  existing clip, geometry table and binary byte
- Runtime/Abilities/NemuHeroKit.cs: only KuroGuard's body/FPP action names
- Runtime/Visual/CharacterAnimator.cs: register only the new named guard action
- Runtime/Camera/ViewmodelArms.CastGesture.cs: new kuro-guard gesture
- Resources/Roster/person_nemu.asset: add only that imported clip reference
- Editor/NemuGuardMotionAuthor.cs/meta: existing narrow importer pattern
- Tests/PlayMode/NemuGuardMotionTests.cs/meta, suite registration and this report

Preserve the separately owned Nemu/Rafi RosterArms files, all other heroes,
HeroHazards and the account/input/reliability contributor lanes.

## Check

One actual defender input in Eskinita, paired body and real owner views; original
shared-seance observation retained. Candidate uses the distinct shipping clip;
Kuro remains a separate companion, can protection lasts under its unchanged
clock, and cancellation retires that protection. Held prop and face stay clear.
Check clip/binary preservation and imported roster binding. Use one guarded
native job at a time; no broad unchanged regression. Do not claim player, peer,
physical-device, listening or human approval from this presentation check.

## Native visual correction

The first candidate passed import, roster and ability contracts but its inspected
world frames remained idle. CharacterAnimator resolves actions through its named
chain registry; the new authored clip had not been registered. Add the one named
chain and require observed live playback in the same test. Preserve the passing
but visually rejected candidate. This is a product integration correction, not
an unchanged tooling retry or a reason to weaken an assertion.
