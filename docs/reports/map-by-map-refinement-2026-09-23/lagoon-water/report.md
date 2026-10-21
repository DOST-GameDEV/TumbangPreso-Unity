# Lagoon water clarity and bed rendering,2026-09-24

Near water is clearer, with stronger far/grazing coverage, gentle surface variation
and broken moving ripple highlights. The real bed now contributes quiet sand/
vegetation fields and moving light, with submerged piles, hulls and shadows visible.
Only Lagoon's water/bed shaders change. A visual-only bed mesh extends1600m so
transparency does not expose the old square edge; the original180m collider,
transform, bed height and water level are unchanged. No new renderer, camera-depth
owner, collider, swimming/fall rule, stock or boat-motion change.

A real routing fault was found while inspectingv2: NearFade's generic lit-material
copy replaced LagoonBed at runtime, discarding its procedural rendering. The bed
now uses the existing NearFade=Preserve opt-out, appropriate for a below-water
surface. No shared fade rule was weakened. The focused case asserts actual runtime
water/bed shader names so a compile-only pass cannot hide this again.

V1passed1/1in5.147s and improved visibility but still looked too smooth. V2passed
1/1in5.097s with ripple traces, revealing the bed routing fault. Correctedv3passed
1/1in5.065s. Actual preview, piles, houseboat, overlook, Low and held20/40time samples
were inspected, plus25percent grey. Keepv3. Ripples/caustics remain restrained and
the sporting deck stays prominent. Originalv1before frames are used in the paired
sheet because later legacy shader toggles already include the routing correction.
The revised assertions verify that the custom bed shader survives scene startup.

Source PNGs/logs remain in QUAL Logs/refine2-lagoon-water-v1/v2/v3. This is native
staged art/physical-surface evidence, not full normal-play/performance qualification.
No extra taste variants or repeated broad tests. Boat material/construction, island/
coast/sky refinement and final map acceptance remain; overall TODO stays active.
