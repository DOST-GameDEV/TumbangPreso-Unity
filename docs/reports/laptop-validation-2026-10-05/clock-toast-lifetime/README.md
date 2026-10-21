# Broadcast-clock toast lifetime

Actual two-machine online gameplay showed BACK TO ACTION remaining visible through later rounds and the final result. Accepted MatchClockMessage snapshots re-announce their scale even when unchanged. The HUD treated every normal-scale notification as a fresh1.4second resume toast, so continuing clock packets renewed it indefinitely. The initial normal clock snapshot also announced a resume that had never occurred.

The HUD now remembers the last announced scale, initially normal1x, and ignores unchanged notifications. Real pause, slow-motion and resume changes continue through the existing toast path; normal native toast timing remains unscaled. No network clock, timer, synchronization, artwork or other UI flow was changed.

## Native evidence

Immutable base5de4675d3c83a04750cfab3539026e6073fd6562, Unity6000.5.8f1/D3D11 PlayMode, isolatedqa-a identity/profile. Tests serialize real scoped MatchClockMessage packets and invoke the actual OnSyncTimeMsg receiver, which applies accepted clock state and raises the HUD's subscribed event. They do not bypass the event path by calling only the HUD's private callback.

OriginalUnity38736: exact4, two causal failures(initialnormalfalse-resume and repeatedacceptedresume renewal), two controls pass(actualpause/slow/resume notices and ordinarynative toast expiry whilepaused), zero skips, exit2. CandidateUnity37400: same4/4 pass, zero skips, exit0. The only original/candidate input difference is Hud.cs.19276 inputs frozen and verified; each210 generated GUI/Auditor metadata deltas preserved before/after and restored exactly,9 existing EditorPrefs values and QualitySettings restored. Main runtime/test bytes match the native candidate after newline normalization.

Raw XML, launches, terminal/restoration receipts, code patch, behavioral fixture and full input-map digest receipts are hashed in raw-hashes.json. Full maps, logs and generated before/after bytes remain at the paths in those receipts. Previous live screenshots showing the persistent banner are retained in the visible-online148-client and online148-hero8-client reports.

## Limits

This qualifies native state/event/expiry behavior. The next combined protocol150 player pair should confirm actual rendered banner disappearance; the earlier protocol148 screenshots establish the failure, not the new fix. Movement feel, held hardware input, network recovery and tournament-wide readiness remain separate acceptance requirements.
