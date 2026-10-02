# Only the displayed match can carry its dispute warning

The current native result details call RankLine, which read global LastVerdict
without checking the displayed record. A late older queued dispute could therefore
label a different match as disputed. The corrected native baseline reproduces
that mismatch; the same-match warning control passes.

CareerStore now retains the submitted match ID beside its in-memory verdict.
RankLine only displays the dispute warning when that ID matches its displayed
_lastRecord. Final2/2 passes older/current result cases. No save schema, backend,
rank rule, verdict policy, visual layout or protocol change.

Unity6000.5.8f1 EditMode, isolated tump-feedback-0930, result-verdict1002 profile.
Three frozen hashes unchanged; guard restored profile/input. One bounded fixture
repair was necessary: the first EditMode case had not bound the isolated career
Instance, so its current-match control failed. Original fixture failure retained;
baseline-corrected is the actual product baseline. No further repair/retry.
No live submission, deployed service, physical UI or refreshed-player claim.
