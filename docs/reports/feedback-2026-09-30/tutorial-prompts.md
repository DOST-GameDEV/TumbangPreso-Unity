# Tutorial prompt readability and skip control

First coherent part of the owner-approved new tutorial feedback. The larger
lesson-route revision remains in progress under F0930-01.

## Changed

- Enter and numpad Enter skip a lesson; the retired N shortcut does not.
- Main keycap layout grows from42 to72canvas units, retaining actual binding glyphs
  and text fallback. Chip rows grow with them rather than clipping their height.
- Footer uses real Xelu Enter/Backspace sprites and Darumadrop One action labels.
  Existing Skip/Quit button callbacks and click targets remain on the same controls.
- Completion still updates the action label. This unit has not yet changed the
  previous completion destination or implemented the new20-lesson exercise flow.

## Native evidence

Unity6000.5.8f1 Linux OpenGL, real Eskinita scene, named isolated profile, texture
mip limit2 restored after the test. Candidate4077fed7 plus the3owned tutorial files.

The first fixture assumed the asynchronous installer had already made the route;
it failed before acceptance. One readiness-wait repair follows the existing scene
fixtures. The repaired baseline reproduces the defect: injected N advances Look
to Move. Final fresh native case passes: N does not advance; Enter advances exactly
one lesson; real footer sprite identities and display fonts agree; keycap bounds
are at least64units; action bounds and native captures pass at960x540 and1600x680.
Both captures were inspected for readable, separated icons and labels.

The cold final compile/import was stopped by the private memory guard at36.27s;
one unchanged warmed-candidate run passed in38.21s. oom_kill stayed5. Raw failed
and final receipts are preserved; no missing results are described as passes.

The full-map cloud hand/slipper material artifact remains visible in these shots.
It predates this UI change; no authored material was changed to mask it. The UI
layout is qualified, not all scene rendering. No new player build, physical-device
certification or human checkbox approval is claimed. Tutorial text, exercises,
completion range and broader Tab coexistence will be checked with the next unit.
The existing glyph-row fixture now counts its own row, since footer glyphs are
separate controls; that older full fixture was not rerun in this one-case check.
