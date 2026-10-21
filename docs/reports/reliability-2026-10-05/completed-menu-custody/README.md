# Menu input custody after match completion

The normal c171 PC result view exposed an inaccurate Match Menu notice saying
the match keeps playing after it had ended. Source inspection found the related
custody defect: closing PausePanel always set the local input's Parked flag false,
even when it was already parked before opening or the match ended while open.

Three focused native PlayMode causes fail on the original source: reopening a
parked body, closing after a match-end freeze and the completed-match notice.
Two relevant controls pass: the live network menu keeps time running and the
offline menu pauses/resumes time and physics. Original PID11892 exits2 with the
expected three failures; no unrelated failure is inferred.

The fix restores only the menu's own withdrawal, retaining a prior park or a
completed match's freeze. It leaves the result pointer free and says that the
match ended. Live network and offline pause behavior stay unchanged. Candidate
PID6288 exits0 with all five cases passing. This qualifies the custody/notice
change locally, not every device or multiplayer input path.

Both parents are terminal and restore isolated settings, shared input/editor
preferences and QualitySettings. Each run's202 generated UI metadata changes
are retained locally then restored to exact pre-run bytes. Original failures,
candidate XML and restoration receipts accompany this report. The unrelated
Auditor setting and cancelled private wardrobe draft are preserved.
