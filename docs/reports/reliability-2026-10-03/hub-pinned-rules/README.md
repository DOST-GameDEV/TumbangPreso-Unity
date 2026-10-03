# HOME refresh preserves pinned match rules

The current Windows normal-lobby run applied Classic tournament rules at boot,
then played all eight rounds in Hero Strike. HOME refresh calls ApplyChoice even
when SceneFlow.RulesPinned protects a deliberately selected rule set.

ApplyChoice now preserves pinned rules by default. Explicit mode-card choices and
owned-hero selections pass an explicit-selection argument and retain their normal
ability to replace the mode. No visual layout, hero mechanics or protocol changes.

Original native EditMode run 33345: two causal failures, two controls passed.
Pinned Classic/saved Hero and pinned Hero/saved Classic both changed modes.
Candidate 72484: all four cases passed on its first run. The ordinary unpinned
saved card and explicit card-selection controls still change the mode correctly.
The pinned cases also preserve the complete custom wire and stored preference.
No fixture repair or native retry.

This tests the real shared rule-selection path in Unity6000.5.8f1, with isolated
named profiles, 1536MB CPU job budget and 2048MB reserve. It is not rendered
physical-input evidence, an owned-hero click test or a refreshed Windows peer
qualification. The earlier 1002m executable predates this fix.

Four owned files match MAIN and qualification. All 18,434 other qualification
assets remain unchanged; guards restored profiles/shared input and released
their leases. Original/candidate XML and input hashes are retained here.

The separate missing-client-career finding remains open. No unchanged full-match
rerun or broader competition-readiness claim follows from this focused pass.
