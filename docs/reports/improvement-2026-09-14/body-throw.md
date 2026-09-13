# Throw motion and carried-shoe contact

The body now has one coherent upper-body throw instead of adding procedural
motion over the stock shooting clip. A readable early gather leads into a55ms
forward sweep,180msfollow-through and540msreturn to the CURRENT locomotion pose.
Walking legs keep running through release. Signed Pektus has distinct shoulder
and wrist directions, and FPP follows the same contact/follow/recovery phases.
Character meshes, outfits, root movement and projectile release time/origin stay
unchanged. Aim settling remains the separate validatedcfa36268checkpoint.

Visual iteration rejected the first high windup because the held shoe overlapped
hair. Lowering/widening it helped, and geometry sampling then found55/1117shoe
vertices inside the inset head volume on held-left. Final lower side load clears
that volume: zero vertices inside across31quick,228full-held-left and217full-held-
right/moving samples. The two long holds last2.8s, beyond full charge. The moving
case continues through release; recorded leg angles keep cycling at2.4035m/s.
These are Berto's actual weighted head geometry and current shoe, not an all-
outfit/all-equipment collision certificate. All retained rig binding and FPP
interruption/return contracts are covered separately by focused tests.

A real contact bug was also fixed: Carrier used WORLD AABB height as support along
rotating hand.up. A tilted long shoe therefore acquired extra apparent thickness
and floated away from the palm. CarrySupportExtent now projects the oriented
local mesh bounds onto the palm normal. The drawn centre rests at that support;
loose/floating-shoe RestHeight behavior is unchanged. Off-centre mesh origins and
tilted palms have dedicated geometry checks. Existing moving/static carry tests
now assert visible-centre contact instead of world-height/origin assumptions.

Validation receipts and timestamped1xowner/body videos are in body-throw-evidence.
Normal-speed quick/held-left/moving-right input-path review passed; moving/static
carry cases passed. Final focused12case rig/continuity/grip run passed12/12.
No full EditMode or PlayMode suite. Named profiles/shared input prefs preserved.

Critique: the loaded arm stays close to the shoulder in this chunky style, but
the shoe clears the measured head volume and the through motion is stronger.
This does not certify every camera/outfit or delayed/rejoining network peer.
Next: fresh internal Windows build and targeted host/owner/observer throw checks,
then remaining movement/equipment/skills. UI leftovers stay LAST. No agents or
reset authority. Do not revive discarded high/head-intersecting pose variants.
