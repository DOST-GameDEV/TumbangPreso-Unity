# Bastion: set the field forward

Bastion now has hero-dante-bastion / bastion-brace rather than borrowing
Unstoppable's personal roar/flex. The0.80second action draws one shoulder first,
sets both arms forward with a small torso turn, settles the weight and recovers.
The feet remain planted. First-person hands reach toward the lower sides of the
view, then return while the original following barrier remains active.

## What was observed

Baseline public defender Skill2 input was accepted. Its7.5second duration,
35second cooldown, following transform and round-reset cleanup checks all
succeeded before the expected shared-roar assertion failed.76 paired native
frames retain that original appearance.

The candidate passes the same behavioral/action case1/1 in4.800seconds,
35seconds outer, guard null.77 paired body/owner frames cover the shoulder draw,
forward set and recovery. Leg angle stays0degrees, body height0.08m. The real
field is present and partly obscures the external view; the separate labelled
pose sheet checks the actual GLB without effects and is not an in-engine look.
The hands stay below the upper aiming region. Existing barrier geometry,
transparency and slab-rise sequence are unchanged; this is not field-art approval.

[Silent before/after and half-speed review](review.mp4) follows simulation
timestamps. The authored pose sheet includes front/side samples with no floor
penetration. No tooling repair or repeat was needed for this unit.

## Preservation and limits

All37 earlier animations and the complete original binary prefix are identical.
Model nodes, skins, meshes, materials, textures, images and GUID are preserved.
One imported clip reference is appended to the roster. Only Bastion's two
presentation action names change in the kit; no activation delay, cooldown,
reflection rule, geometry, ownership, network or recording-schema change.
Unstoppable and the previously shipped Boulder remain untouched.

This is isolated native input/visual/lifecycle evidence. It does not establish
full-map player/peer, moving-cast combinations, performance, audio or human
approval. Existing broader Geo work stays open. Use tools/author_dante_bastion.py
for this clip and DanteBastionMotionAuthor.Wire for its serialized roster entry.
