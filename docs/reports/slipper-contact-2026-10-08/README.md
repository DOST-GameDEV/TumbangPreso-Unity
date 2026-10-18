# Slipper contact and render-copy attachment

Original source base: ASTRAReworks `0a4aabb48a78590bad6872e5f2f24a8a8765db05`. Safely fast-forwarded to `1570d5de6598cf112adb8d929facae8f008012b3` before final focused replay/swap checks. The incoming commits did not overlap the four changed runtime files.

## Corrections

- Body carry anchors used a fixed lift measured from an older human model. Current rigs have different hand thicknesses and separated fingers. Independent triangle intersection and native close-ups showed both floating and clipping, including roughly 13 cm of separation on Ilyas. Anchors now use the actual weighted hand surface, with 3 mm clearance. This is calculated when a model binds, not every frame. Authored meshes, animations, ability mechanics and movement remain unchanged.
- Ultimate and tag-replay copies previously baked the live shoe's position relative to the hand. A cast between a body movement and Carrier.LateUpdate could bake a stale offset for the entire presentation. Original native reproduction measured 0.4907552 m of planar error. Copies now reconstruct the fitted grip from mesh-relative data without moving the live authoritative shoe. The same fit covers a live stowed shoe.

## Passing evidence so far

- Surface-fit: two native cases pass, covering 23 art entries (21 selectable bodies plus two custom art entries), independent hand-surface contact and the stale ultimate-copy reproduction.
- Live physics: both cases pass. The first case includes all 21 roster bodies, despite its original Classic-prefixed name; the second repeats nine hero bodies. There are 231 unique body/state rows plus 99 repeated hero rows. Idle, walk, run, all seven emotes and teleport are observed after actual physics and LateUpdate. Recorded attachment error is below the six-decimal CSV precision over 305,243 sampled frames. This does not establish full-duration emote taste or cross-device/network performance.
- Full ultimate timelines: all nine heroes sampled at 121 time points each retain the fitted copied surfaces. Gameplay slipper identity stays unchanged.
- Accepted throw: legal normal InputIntent charge and release in the controlled native stage; selected post-LateUpdate hand captures for Bayan, Yasmin, Ilyas and Paete inspected.
- Existing support math: four native cases pass for hand selection, rotated support and offset mesh origins.
- First-person surface check: 21 bodies, ten shoe skins and four charge poses, 840 combinations. Distal hand vertices are measured against real shoe triangles; the maximum sampled contact-distance upper bound is 0.03275 m against a 0.035 m bound. This is not an exact zero-gap proof. Twelve rendered owner-view images cover the four hand-geometry families and three representative skins. No first-person production code changed.

## Retained failures and limits

- Initial palm-window sampling included unrelated nearby geometry; replaced with exact projected triangle intersections. The original failing output remains available.
- A clone created under an active stage produced a bindpose error; the corrected test uses the same inactive-clone setup as the existing production review.
- First motion attempt sampled render frames too quickly to guarantee a physics step. Its missing-motion failures were preserved; the passing retry explicitly observes fixed/render pairs.
- Early motion PNGs were captured by the coroutine before LateUpdate and are not accepted final-pose evidence. Passing attachment metrics use an after-carry observer. Accepted-throw images were captured from that observer.
- First-person centre-ray and sole-plane assumptions did not measure a heel/strap grip correctly. Those diagnostic failures are retained, not treated as product failures. Actual mesh-surface checks and native images replace that unsupported interpretation.
- Existing ThrowEquipmentClearanceTests fails on both original and corrected palm source, on the same 200 of 210 body/skin pairs. Original/candidate CSVs are retained. This is not a claimed all-pose head-clearance pass. Selected actual-input captures do not reproduce the same apparent head entry; the older fixture versus live-pose discrepancy remains separate.
- One combined graphics equipment run exhausted the 8 GB cloud limit. Headless split geometry checks completed. A later full-map capture was interrupted after resource starvation. Profile restoration was verified; only its positively identified orphan import/compiler helpers were retired. The final smaller legal-position stage succeeds.
- Windows, physical controllers, actual separate-device peers, long sessions, all view transitions and human approval remain separate acceptance work. No universal no-bug claim is made.

## Final lifecycle and integration

- Five retained CarryTests pass: held-on-hand fit, movement and missing-anchor recovery, remote visual smoothing, anatomical anchor riding and first-person ownership.
- The sixth case, full-map CatchCameraAdoptsGameplayShaderContextAndRestoresGlobals, exceeded its 90-second timeout. The batch finished normally with exit 2 and restored both named-profile files. No additional OOM kill occurred. This rendering case is not accepted as passed.
- Final focused native batch: four passes on 1570d5de plus this diff. Independent surface contact for 23 art entries, repeatable Paete/Bayan model swaps retaining the same shoe, stale-pose ultimate copy and CatchReconstruction.CopyHeldItem across all 21 selectable bodies pass. Replay copies stay hidden outside capture and leave the live slipper transform/ownership unchanged. This is direct render-copy placement coverage, not a full replay camera/scene acceptance pass. Both final XML and the independently measured palm CSV are included. The guard verified restoration of 31 existing named-profile files and zero shared input preference entries.
- The old Paete body-swap fixture expected the superseded literal human HandTopLift. It now measures independent actual triangle contact and retains slipper identity and repeatable Paete-anchor checks. Its separate full-map execution is pending; the new minimal-stage swap regression exercises the same actual body swap.
- Test names now describe all-roster coverage accurately. Future movement screenshots are taken from the post-carry observer; the earlier retained screenshots remain explicitly unaccepted.


## Reproduction

Unity 6000.5.8f1 on Linux, native graphics with `-force-glcore`, one guarded job at a time and isolated named profile. Run `SlipperContactTests` in PlayMode. Pure first-person mesh checks are EditMode; rendered owner-view checks require a real graphics device. Full-map shader rendering remains separately unaccepted as documented above.
