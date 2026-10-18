# Loose footwear surface support, October9

Independent actual-mesh vertices and actual floor collision rays reproduce
floating across10 selectable shoes on IlalimNgTulay, LagoonCove and Kanto.
All30 pairs originally float45-91.8mm. MatchInstaller and the doll factory both
instantiate these authored sole-pivot models directly; a renderer's half-height
is not the distance between that pivot and its sole.

RestHeight now projects the mesh's bounds into the upright shoe-root space and
subtracts its centre offset before converting to world scale. It retains1mm
surface clearance. Missing mesh data keeps the earlier fallback. This leaves
hand support and the existing flight collision clearance independent.
No map geometry, hero model, authored motion or kit changes occur.

The initial mesh-bottom candidate passes30 actual contact pairs at1mm but the
broader batch returns9PASS1FAIL: resting support varies with the held rotation,
so the deck control's pre-landing and actual resting heights disagree by60-65mm.
The final implementation removes that rotation dependence. Final native74308
passes12 controls:30 actual mesh/collider contacts,10 shoes at three held/tumble
rotations,8 prediction/Skim controls and2 actual Arena ramp-retrieval/deck/roof/
floating-recovery controls. All21,453 protected inputs/preferences restore.

The first launcher mistakenly selected3 existing companion tests. Its expected
one-case flag is false and it has no ground evidence; it is retained and excluded.
The corrected original run executes the intended ground test and fails all30
contacts. No assertion or tolerance was weakened to make the final batch pass.

This source unit is not in the installed Desktop e1c1 package. It awaits the next
batched ordinary replacement of existing G and Desktop. Actual Windows contact,
nonuniform/scaled custom rigs, inclined/irregular ground visual support, broad
natural ground/air/kit attachment and full matches remain separate acceptance.
The Arena cases exercise retrieval and recovery, not creative old-map changes.
