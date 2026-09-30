# Visible tag-contact refinement

## Owner review and diagnosis

The owner rejected the earlier replay picture because it did not visibly show the
tag landing and requested animation refinement. That picture was not sufficient
contact evidence: the minimal fixture disabled motors/used default capsules, and
multiple same-frame seeks could show stale native skinning despite updated bones.
The review now uses actual grounded simulation, the shipped person capsule and
completed real-time render frames. Existing historical captures are preserved.

Camera-side and limb-axis hypotheses were ruled out. On an accepted1.65m tag, the
recorded palm remained0.4777m short of even the target's renderer bounds. The limb
axis aligned with the measured palm (dot0.9693), so changing the camera alone would
not repair this reach gap.

## What changed

- The existing accepted Tag flair supplies its actor, subject and contact position
  to the ordinary player tag pose. The hand aims at the target's torso-side surface,
  with bounded temporary limb-axis extension, restored on recovery/rebind. Misses
  retain their generic gesture; late metadata from a completed gesture cannot aim
  the next miss. Non-finite points are ignored.
- The jab holds its touch through0.22s and still recovers by0.34s. First-person reach
  shares that hold and reaches a little farther forward. Lunge body/hand presentation
  now holds through its actual0.45s active sweep, with the same0.62s recovery.
- The replay retains0.5s of real follow-through, within the existing3s total, so
  contact is visible before the final fade. Only the victim's penalty teleport is
  held at contact; the tagger's recorded movement is no longer flattened afterward.

No new hit, range, cooldown, score, actor teleport or wire field is introduced.
The pose consumes an already accepted outcome and does not authorize one. Protected
hero ability behavior, authored meshes, rig names and asset metadata are unchanged.

## Native evidence

Unity6000.5.8f1 Linux64 with graphics and two job workers. The isolated world uses
four actual Classic models, the real directors, accepted punch/tag, animator,
history and replay camera on a simple floor. This is not a full authored map.

- The grounded1.65m baseline fails the visible-bounds contact check by0.4777m.
- Real-time near1m and far1.65m cases pass. The final far contact has0bounds gap,
  full replay opacity and a sample within0.008s of the intended reach peak.
- Both cases verify the immediate score change and exactly one accepted Tag event;
  temporary limb scale returns to its original value.
- The metadata lifecycle case accepts the preceding-event/action order, refuses
  stale metadata reuse on a following miss, ignores a non-finite point and awards
  no Tag event from presentation alone.
- The continuous-motion/history-wrap/render-isolation case remains passing.
  Unchanged cases were reused after the final metadata-only guard.
- Real-time stills and the3s film were inspected. The hand now visibly reaches the
  target. Source inputs stayed frozen throughout the final runs.

One assertion originally compared total score after3s and correctly saw normal
passive defence points (100 became130). It was corrected to retain the immediate
exact score assertion and count Tag events specifically. A longer follow-through
also outlived the0.4s original burst; the isolation fixture now creates a real
concurrent ImpactBurst rather than asserting an expired object must still exist.
Neither change weakens the behavior being checked.

[Close contact](tag-contact-close.png), [long contact](tag-contact-far.png) and
[silent real-time native film](tag-contact-native.mp4).
The film is960x720, encoded at its recorded timestamps into a3s H.264 file; repeated
encoding frames are not invented intermediate poses.

These checks establish the measured Classic pair's live/replayed contact and
lifecycle. Human taste, other bodies/angles, first-person film, full-map behavior,
physical hardware and actual transport peers remain separate qualification work.
Human verified stays unchecked. Renderer-bounds proximity is a useful gate, not a
replacement for the inspected pixels or exact skin-surface collision measurement.

## Results and hashes

[Original far failure](checks/tag-contact-far-before.xml),
[near/far final results](checks/tag-contact-final.xml),
[final far/metadata results](checks/tag-contact-lifecycle.xml).

- far.xml: SHA-256 918ea0c18db2e81b3997df4b49f86f3c1c3c666a402a3a2976124277fd914289
- real-time.xml: SHA-256 8589b89aefd8fe290aa42c7e82b2b39fdc519ac80041f1a5c8d5183a7adc1b4e
- contact-final.xml: SHA-256 378b050e87e5b3f5229f7f63cc96baf10d6be233a53f598bdb033a180a994d86
- lifecycle.xml: SHA-256 0cd7e3584a85a01703b1a653e917a54e934840af7d2749c015c462a3262ddd65
- lifecycle-inputs.json: SHA-256 3344dea64666b6a2179f55eb734161c3454015144ef8063113ff5d3a904d7892
- tag-contact-native.mp4: SHA-256 6ca54483085804def18a5651710cbe909852c7858f312818607376635a491583
- reach-1.00/contact.png: SHA-256 ea94da0bc06801f5287dc36339b6ca686d3c02f5698b45974e6111f4a8e829ba
- reach-1.65/contact.png: SHA-256 bce634794ac001de4f7452910ffee565fb2f7042a29865ae753cf825db9b3009

## Shared branch integration

The automatic merge with80ff8b58 preserves the incoming charge-reticle and revised
middle-mouse/F controls byte-for-byte. Far real-time contact and metadata lifecycle
pass2/2 on the combined candidate, with frozen inputs unchanged. This does not
expand the all-roster/full-map/actual-peer or first-person-film claims.

- integration.xml: SHA-256 27cceded7eb78c16957f5a71a9dfdb07e546346c4e203cc068a71f4960be7c1d
- integration-inputs.json: SHA-256 0b659ad1d9e042e5d3723a9985f96c771d7864f7a4a130d082392fad8056c0f8

## Whole-body refinement after owner review

The owner rejected the first film's arm-heavy reach and requested a body that
tries to reach. The earlier film remains above as history, not visual acceptance.
The next pose resets the old melee wind-up under the ordinary tag, turns the
shoulder/chest into the touch and transfers the hips into a split supporting step.
The offset is on the visual skeleton only. Leg-length geometry determines the
forward shift and hip drop, preserving the authored limb lengths. Contact-driven
arm scaling is now bounded to0.90-1.10 instead of0.75-2.25.

The first body iteration passed both contact cases, but the close-range picture
showed too much commitment into the target. The final jab adapts its lean/step to
the accepted contact distance. Its close/far real-time cases both pass:

-1m: body offset0.144m, torso lean23.33degrees, no arm elongation, bounds gap0.
-1.65m: body offset0.273m, torso lean38degrees, arm scale1.10, bounds gap0.
-Both retain full contact opacity, exactly one accepted Tag event, unchanged live
 capsule position and restoration of the root offset and arm scale after recovery.
-Restoration now unwinds tag offsets before the underlying locomotion layer in
 Update, LateUpdate and graph release, matching the layer application order.

[New close pose](tag-contact-body-v2-close.png),
[new far pose](tag-contact-body-v2-far.png),
[new3s native replay film](tag-contact-body-v2.mp4).
The film uses36completed native frames and their recorded timestamps, encoded to
960x720/30fps without interpolated poses. The extracted encoded contact frame and
source close/far pictures were inspected. These remain isolated Classic pair
checks, not an authored-map, all-body, actual-peer or human acceptance claim.

The final run contains two passing contact cases and one failed new recovery
probe. That probe first sampled before LateUpdate; its single bounded correction
observed no camera callbacks outside an active replay in this batch fixture. It
was removed from shipping test source as an incomplete harness, not treated as
passing or used to claim interruption coverage. Both failed receipts and the
incomplete probe are retained with the private run evidence. No further tooling
loop was started. Normal recovery is covered by both actual replay checks;
dedicated missed/lunge-interrupted playback remains unqualified. Runtime inputs
are unchanged between the final contact capture and that harness-only attempt.

[Final body run: two contact passes, incomplete recovery probe](checks/tag-contact-body-v2.xml).
[Bounded recovery-harness attempt, not a product pass](checks/tag-contact-body-recovery-inconclusive.xml).
