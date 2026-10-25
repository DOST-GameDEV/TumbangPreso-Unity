# Player-view perception

Selected player-view playback now inherits the watched player's Haunted near
sight, Possessed world grading, Concussed double image and associated world audio
filter. HUD status effects and private Nemu ghost-reclaim cues follow that same
presentation owner. Manual third-person follow and free views clear them.

The player's actual emote or companion camera remains player-view playback, so
its private perception persists just as it does locally. This is separate from
the spectator choosing a third-person camera. A target switch cannot borrow the
previous target's view identity while the new stream is pending. No spectator
input, reclaim action or gameplay authority is granted.

Native validation:

- `private-filters-final.xml`: 22/22, including actual near/far rendering, aspect
  checks, local status/audio/menu/replay lifecycle and watched-view transitions.
- `private-ring-play.xml`: 13/13, including private ring visibility, immediate
  manual-view clearing and unchanged public trap/ghost replication behavior.

These focused suites overlap. Their hashes are in
[the receipt](perception-validation.json). They establish local presentation
behavior; the existing [three-process view evidence](view-candidate.md) remains a
separate transport check.

An existing near-sight render assertion initially failed with both the previous
predicate and the observed-view predicate at exactly the same pixel value. The
test compared sRGB readback values against a linear-light threshold. Its
measurement now decodes to linear light; the threshold, authored purple ink,
shader and gameplay were preserved. Both failed receipts remain in scoped Logs.

Private hallucination geometry and the owner's exact held aim marker still need
observed-view data and real-peer checks. Broader skill/flight coverage, NETWORKFIX
reliability and the final mobile refresh remain active work.
