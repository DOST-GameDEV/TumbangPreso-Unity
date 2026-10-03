# Closed Circuit acquisition motion

Scope: the accepted defending cast's missing body animation and generic first-person
thrust. The existing six-metre sight/aim selection, 0.4-second acquisition, two-second
Zapped, 35-second cooldown and Overclock follow-up are unchanged.

Source inspection found `castAction: "cast"`, no corresponding CharacterAnimator
action chain, and a generic ThrustClip in the first-person dispatcher. The shipping
roster must reference an authored clip; Editor-only generated curves cannot fix a
player build.

Direction follows the existing full-roster presentation plan: an angular, bladed
upper-body aim. A short shoulder preparation opens the free hand toward the target,
then settles through acquisition and returns to neutral. Feet stay planted and the
carrying wrist stays quiet. This acquisition gesture does not assert a successful
hit, so a cancelled lock must never produce an authored impact burst or victory snap.
Actual success/contact remains a separate visual unit. Native review subsequently
showed the old beam starting near his face, so this unit also attaches acquisition
to the measured left palm in world and owner views; no new targeting behavior.

Beats: neutral at 0; compact opposing shoulder/head preparation at .08; directed
free-hand extension at .18; steadier held acquisition at .36; recovery at .48;
neutral at .64 seconds. No added cast lock, displacement, camera cut or timing gate.

Author only one appended GLB action, preserving geometry and every existing clip.
Wire it to the serialized Zack roster and explicit action chain. Add a distinct
first-person key sequence through the existing body/viewmodel bridge. Verify the
asset reference, dispatch, actual playback, neutral recovery and preserved gameplay.
Inspect motion visually before publication; structural checks alone do not accept
the pose. No new SFX or listening claim belongs to this unit.

Status: implemented and narrowly qualified; publication pending. See README for
native evidence, retained failures and owner-view/player/peer review limits.
