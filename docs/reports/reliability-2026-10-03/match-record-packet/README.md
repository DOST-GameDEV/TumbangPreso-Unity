# Refuse damaged match-record packets without losing the current result

The actual OnMatchRecordMsg receiver previously threw OverflowException for an
incomplete string frame and ArgumentException for invalid JSON. The callback
could escape before preserving the current result. Baseline MAIN def71d8f4,
original27464: two causal failures, valid-Unicode/normalisation and sender/
loopback controls both pass. Four native EditMode cases, no skipped tests.

The candidate validates the complete UTF-16 frame with existing SkipWireString
before decoding and refuses trailing bytes. It catches only JsonUtility's
ArgumentException; valid-record normalisation, adoption, sender checks and host
loopback behavior remain. No schema, protocol or authority change.

Candidate23704 FIRST4/4, zero tooling/fixture repairs or native retries. Same
fixture before/after; malformed/truncated/overflow/trailing data cannot replace
Last or publish RecordReady. Valid Unicode and existing placement/score
normalisation pass. Guards preserve named profiles/shared input and release
leases. Post11495: all18439 protected qualification assets unchanged,
exact three owned inputs match MAIN.

This is native direct-handler packet evidence in the protected qualification
overlay. No crafted live-peer fault injection or whole readiness claim. Current
Windows133 artifact1003a predates this fix and incoming134; build the combined
source before claiming packaged acceptance of this unit.
