# Owner-painted player hub, rank and stories

Profile, friends, career, history, account and scorecard views now have new native
builders using the owner theme. Old visual builders remain inactive. Existing
account/social/career operations, account deletion safeguard, banner selection,
mastery and achievement data are retained. Native history adds older/newer page
controls on the existing paged service and clears its displayed cache when the
account changes. The scorecard uses real columns instead of spaces in a
proportional font; all four players fit without scrolling past the fourth.

Edited profile values survive group/tab/service redraws. Only modified values
are kept as drafts; they clear on account change or successful save. Hidden
optional drafts are submitted through the existing profile operation. Inputs and
Save lock while pending, and completion does not move someone back from another
tab. Friend search text also survives presence refresh. No real account or social
operation was submitted in tests; broader live-service edges remain U8.

The actual fixed tag stays in the header, so duplicate Identity/tag rows were
removed from the main profile form. Empty history has a clear message and no
pointless Page1 label. The known batch-only UGS diagnostic is presented as a
plain online-unavailable explanation. Raw rating was removed from preparation;
existing displayed tier names and underlying rank IDs remain unchanged.

Six editable biographies from CHARACTER_ORIGINS.md now live in
Resources/UI/character-stories.json. The hero chooser shows origin and a short
personality line; optional MEET pages give the full introduction without delaying
selection. Mechanics remain available through the existing Skills view. Cheska
and Nemu keep their off-screen hometown courts, and Phaister's birthplace remains
distinct from her adopted Quezon City court. No new character locks or seventh
hero were introduced.

Rank graphics are native editable tin-can, tsinelas, pennant, cup and star
silhouettes. The first Rookie drawing read like a ladder; curved rims and bands
now make it a recognizable can. The imagegen companion study is preserved with
its exact prompt and critique under ArtSource/ui/owner-handdrawn-2026-09-15/studies.
Its bitmap, washed-out gradient, repeated circular badges and leaves were
rejected for production. Only limited contour/recognizable-slipper ideas informed
native graphics. Original supplied artwork remains unchanged.

## Scoped evidence

Original Unity images and fresh receipts are in owner-ui-u7-evidence.

- Hubv1:1/1, five pages, profile draft across service/tab refresh, hidden optional
  draft retention and actual close. No edits submitted.
- Hubv2:2/2, the above plus seeded career/history and real scorecard columns.
  Seeded profile/history values existed only in memory and were restored.
- Story1/1: all six biography entries present, origin displayed, optional story
  opened/closed without changing selection, then existing skills/return path.
- Rank/scorecard1:2/2, five visible native glyphs and populated career/table.
- Rank refinement2:1/1 after the curved-can correction.

Named profiles and shared Editor input preferences restored. No Desktop update.
These cases do not validate real remote profile writes, friend requests, reports,
deletion, every unlock/long record or physical controller/phone behavior. They do
not certify every frame as final art. The native3D preview still needs its
brightness comparison, and universal beveled paper surfaces remain a U8 concern.

Finish U8 editability, distinct frame treatments, ordinary-speed/reduced motion,
input/layer/route checks and an internal Windows player. Then resume the full
gameplay queue from GAMEPLAY_RESUME_AFTER_UI.md.
