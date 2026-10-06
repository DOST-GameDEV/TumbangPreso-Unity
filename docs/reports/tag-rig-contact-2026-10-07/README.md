# Grounded contact through the actual hand rig

The adopted models have forearms, but the old tag pose resolved only upper arms.
It therefore substituted a0.28-unit endpoint when the real hand lived below an
elbow. Native Isagani-to-Nemu originally leaves the hand0.486m short of its aim.
The recorded footwear also penetrates the court by about0.23m.

The shared tag pose now settles and restores both forearm joints, aims the actual
hand through the full chain and solves the visual body step from its current
reach. It samples fully weighted sole vertices to keep the stance supported.
Tag presentation no longer stretches the limb. No authored models, kits, clip
clocks, hit rules, score authority or gameplay capsules were redesigned.

The gameplay capsule is also an imperfect visual surface. A classic far-tag
control still had a0.091m skin gap after the rig fix. The accepted receipt now
selects a point on the visible skinned triangle once, then follows its three
weighted vertices during the hit reaction. The original contact-position offset
survives the victim's authoritative teleport. Missing retained bones retire that
surface sample while preserving the accepted point and receipt.

Evidence:

- Native27992 reproduces Isagani's original hand/bounds gap and floor penetration.
  BakeMesh with scale compensation matches the native renderer bounds; the
  uncorrected scale mode is oversized. [Unity API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SkinnedMeshRenderer.BakeMesh.html).
- Native29964 passes all9 roster contact, supported-footwear, capsule/score and
  recovery checks after the elbow and sole correction.
- Native36800 passes11/12 wider controls and preserves the failed classic skin
  gap, which prompted actual-surface targeting rather than a looser assertion.
- Native34896 then passes all12 controls: close/far/authored classic touch,
  metadata retirement and score, motion, freeze/fade, side clearance, shaders
  and four match-identity lifetime cases. Classic skin gap is0.04193m.
- Native20040 rechecks all9 hero pairings after skin targeting:9PASS. Actual skin
  gaps range0.0083to0.0787m, soles remain about0.005m above the test floor,
  opacity stays full and tag-driven limb extension is absent.
- Geometry/identity/missing-rig controls also pass9cases on native32316.
  Native37544 additionally passes the strengthened actual-bone-destruction fixture.

The old recovery assertion compared two different phases of authored idle scale
animation. Isagani's idle forearm scale ranges1to1.7 and Paete's1to1.38 in the
adopted GLBs. Elbow-rig recovery now compares an independent render-only skeleton
sampled from the current authored clip at the same time, retaining the0.001
tolerance. The existing seven-bone assertion remains. Contact, sole support,
capsule immobility, single accepted score and scale/stretch checks stay strict.

Selected source hashes, native XML/results, restoration receipts and actual
contact views are retained under raw/ and roster/. Each terminal run restores
all frozen inputs and shared preferences; final runs protect21309 inputs.

Remaining acceptance: natural moving and airborne contacts, varied ground/map
positions, physical device/camera feel, current peers and the later combined
package. Bot/navigation, map intros/arrivals and loose/held slipper reliability
remain active. Current Desktop still contains the qualified startup9ab package.
