# Haunted bot spacing information leak

The shared bearing board represents rivals' positions/intended throwing lanes.
Actor targeting already respected Haunted, but this board still supplied the
latest claim from an unseen rival. A native ten-metre rival reproduction exposes
the fresh .2bearing during active Haunted.

Hero Strike Haunted reads now use the existing ActorIsVisible check through
RoundDirector.BodyAt, including companions. Hidden or absent bodies cannot
provide a live claim. Existing chosen goals and observed-position memory are
retained; this does not invent a new claim-memory system. Normal/Classic reads,
claim TTL, scoring and per-brain retained storage stay unchanged.

Windows Unity6000.5.8f1 baseline1/1fails at the intended leak. Final3/3passes:
hidden updated claims, seven-metre sensing boundary/status clear/Classic control,
and visible/inactive companion plus absent-body exclusion. All697frozen inputs
remain unchanged. No native tooling repair;89524/24002terminal, profiles and
shared input preferences preserved. [Raw receipts](haunted-spacing-checks).

Reuse the unchanged normal-mode query/allocation evidence from
[the spacing optimization](bot-spacing-allocation.md); no claim of a new combined
seven-case run. Body-distance sensing is not pixel-identical camera fade. No
fresh player/actual-peer or whole-match difficulty claim. Hero kit, protocol120,
loading and authored assets remain unchanged.
