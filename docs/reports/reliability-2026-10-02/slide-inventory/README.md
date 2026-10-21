# Slide slipper inventory qualification

## Question and stopping condition

Slide prediction and the active host pickup sweep currently perform whole-scene
slipper searches. Measure the existing prediction path before changing it, then
reuse the established BotSlipperInventory only if the same focused controls pass
and the warmed prediction timing improves materially. The cache retains object
references; positions, ownership, state and active hierarchy remain live reads.
Slipper.Awake and OnDestroy invalidate births and destruction. Component disable
alone must not exclude an otherwise eligible active shoe.

The fixture measures seven5000-call batches after100 warm calls, with four
slippers and one eligible target. It reports the median time per public
SlideMayStartFrom call and separately estimates heap growth over fifteen1000-call
batches. Heap growth is not an exact allocation counter and a narrow Editor
measurement is not a representative match FPS measurement.

Original and candidate use identical tests in the isolated qualification checkout.
Six cases cover warmed repeated prediction, live eligibility/lifecycle, actual
input-driven pickup, wall refusal, one pickup among two candidates and an empty
retrieval route. Use one heavy Unity job, unique outputs/profiles and at most one
bounded tooling repair. Keep all raw failures. If behavior regresses or no
material timing improvement appears, do not publish the proposed optimization.

## Results and scope

Initial original run reached its450second guard timeout with no result XML.
Both owned Editors were stopped; the guard completed preservation and released
its lease. This is neither a passing test nor proof of a product performance bug.
One fixture repair establishes/restores a solo provider and the requested game
clock, and puts a ten-second wall deadline on scaled waits. The six case names
and behavior assertions remain unchanged. Corrected original run7466 completed
with fresh six-test XML:6/6 passed. Candidate15335 passed the same6/6 on its
first run, with no further fixture or native retry. Both completed guard
preservation, released their leases and exited.

The product change replaces two FindObjectsByType calls in CombatVerbs with
BotSlipperInventory.All. Eligibility, reach, line of sight, cooldown, stamina,
input, network and hero rules remain unchanged. The existing cache already
serves bots, replay and Fetch; this unit introduces no new invalidation rule.

Warmed public prediction measured17.12962 microseconds/call before and9.41678
after, a45.03percent reduction. Each timing is the median of seven5000-call
batches under the same corrected fixture. The separate heap-growth estimate was
1490.944 versus1363.968 bytes/call across15 non-shrinking batches each. These
are heap-growth estimates, not exact allocation counts; this path still allocates
elsewhere. Sweep timing and representative player FPS were not measured.

Actual input-driven pickup, wall refusal, exactly one pickup with two candidates,
empty retrieval, live ownership, object disable/reactivation, disabled components,
destruction and replacement all passed. Native Windows Unity6000.5.8f1 D3D11,
isolated tump-feedback-0930; corrected runs use180second guard deadlines,
2048MB declared budget and2048MB reserve. No physical controller, WAN, hero
presentation, whole tournament or scene-specific stall qualification is claimed.

Corrected fixture/source hashes, XML, timings and receipts are retained alongside
the original timeout receipt. The original failed fixture hash is retained; its
exact bytes were not archived before repair. A reconstructed copy failed the hash
check and is not represented as the original. Original and candidate measured
runs use the identical corrected fixture, verified by their input hashes.
Post-run verification found all18434 protected qualification assets unchanged;
the two owned candidate files exactly match MAIN. No owned Editor/player remains
running. The packaged1002l player predates this optimization and incoming Cheska
presentation; neither change is claimed qualified in that older executable.
This work does not identify the cause of the historical270.33ms host frame.
