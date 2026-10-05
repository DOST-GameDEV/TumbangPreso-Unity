# Current locomotion Feedback investigation

The broad walking/running report remains open. Current native Eskinita checks
exercise actual motors and authored gait layers through empty sprint, empty walk
and held-slipper sprint. Classic Bayan/Inday passes1/1 in19.4837809s, Yasmin1/1
in20.3307126s and Basilio1/1 in20.1777378s. Each samples60or61 LateUpdate frames
per state, observes complete stride phases and preserves the limited carrying
hand swing. Exit0, restored settings, frozen inputs unchanged and no extra OOM.

The existing probe can now select a particular hero with TUMP_LOCOMOTION_PERSON,
using the actual roster model; TUMP_LOCOMOTION_OUTPUT isolates its captures.
Whole-body three-quarter framing avoids the cropped-hat/obscured-arm limitation
seen in Yasmin's original tight side view. Photo samples cover0.75seconds rather
than only0.35seconds. Basilio whole-body images were inspected. Yasmin's earlier
side images are not claimed as complete silhouette acceptance.

No production animation was changed and no causal defect was demonstrated by
these cases. This is neither all-roster qualification nor human approval of the
movement style. Keep the Feedback report open while isolating a specific problem.

## Yasmin full-body follow-through

The corrected existing fixture now covers Yasmin's original cyan/goggles model
with full-body framing and the visible non-owner carried prop. One native case
passes, sampling 60 completed poses for each of empty walk, empty sprint and
carrying sprint. Ninety captured frames include the complete hat and feet.
Selected start, middle and end carrying poses and the empty sprint were inspected;
a silent three-state review was shared. The free arm covers its authored range;
the carrying arm remains between 22 and 46 degrees forward, preserving the
restricted swing. No new production gait defect was demonstrated or animation
changed. These are fixed 60 Hz simulation captures, not hardware frame-pacing,
all-angle or human-feel acceptance. The broad Feedback item remains open.

The job exits 0 with one actual passed case, all frozen inputs unchanged and no
new OOM. Retained evidence is in [yasmin-fullbody](yasmin-fullbody/).
