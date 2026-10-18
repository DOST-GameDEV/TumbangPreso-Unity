# New-map entrance cadence follows travel

Bridge, Cove and Kanto move the visual player along an eased supported path but
previously advanced each walk clip at a fixed0.8 speed. Shortened paths retained
the same stepping speed. Natural automatic openings expose a mismatch: Cove
seat2 moves2.497m while its calibrated animation represents6.763m. All twelve
observed original actor paths have1.06–4.27m of cumulative mismatch.

The first probe omitted a Playables namespace and did not compile; that failure
is retained. Corrected original62688 passes two per-frame cases and fails Bridge.
The small per-frame threshold was too weak at higher frame rates: the retained
CSV totals independently show drift on Cove and Kanto too. The final probe also
checks whole-path drift, retaining the original weaker result explicitly.

The new3 direction now advances walk phase using actual planar visual-root
travel and the existing per-body stride calibration. Idle and crossfade clocks
still advance by time. Delayed actors wait before starting their walk. Arena's
existing AdvanceHeld caller, old-map presentation, paths, art, kits and physical
player positions are unchanged.

Candidate50836 passes all three real automatic Hero openings at1x. Calibrated
step distance agrees with actual travel for all twelve observed actors. The
separate existing22380 geometry controls pass floor support, carried-slipper
attachment, Taya vertex framing, supported arrival, camera return and cancel
restoration on the three maps. Both runners terminate and all21,425 frozen
source inputs/preferences restore. Exact native source and raw receipts are
retained; [comparison](cadence-comparison.json) is derived from the CSV data.

These are unseeded short openings and posed geometry checks. They do not prove
stance-foot sliding is eliminated, every hero works, continuous cinematic taste,
overlay composition, player latency, replay, peer quality or whole-match bot
efficacy. No build was made. The current internal G and qualified Desktop remain
unchanged until the next batched replacement is tested.
