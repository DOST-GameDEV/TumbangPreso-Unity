# U8 transfer checkpoint, 2026-09-15

Status: source/build checkpoint, NOT completed UI and NOT visual acceptance.
The owner requested this transfer before remaining redesign/qualification.

## Latest owner rejection takes priority

The owner explicitly rejects the last batch as hideous because the same
main-menu assets and orange patterned background were reused throughout the UI.
Existing assets were meant to inspire NEW designs. Re-study the original PDF,
especially slides 39-49 as layout ideas, not final compositions. Make different
screen compositions and use built-in GPT image generation for new purpose-specific
art/backgrounds in the newer hand-drawn style. Do not just reskin current builders,
recolour a universal button/panel or produce an unused generic concept board.
Keep original supplied files intact, but do not mistake preservation for a mandate
to paste them everywhere. Preserve native editable text, useful callbacks and the
approved controller diagram/buttons/18 callouts/connecting lines.

Original PDF: ArtSource/ui/owner-brand-2026-09-13/TUMP-moodboard-original.pdf.
Old logo JPGs and palette sheets are alongside it. New source PNGs and measured
fonts/colours/regions: ArtSource/ui/owner-handdrawn-2026-09-15. No Downloads path
is needed on the destination. All three new source hashes verified at transfer.

U1-U7 reports describe first functional implementations, not approved final art.
The owner has now rejected their repeated visual language. Those mechanics can
be retained while genuinely redesigning views. Finish UI before resuming the
entire GAMEPLAY_RESUME_AFTER_UI queue; selected seventh hero/map remains last.

## Source saved in this checkpoint

- OwnerUiPaper now has curved Reading, Note and Dialog treatments rather than
  repeated bevels. This alone does not resolve the owner's creative rejection.
- OwnerUiOverrideBook/OwnerUiOverrides permit Inspector text/font/ink/position/
  size/art/paper changes. Exact-label replacement preserves live values; offsets
  do not accumulate; empty book performs no traversal. Native shape replacement
  can place a Sprite behind editable labels. See OWNER_UI_AUTHORING.md for limits.
- Editor menu selects overrides and exports named visible element paths/defaults.
  Export omits InputField text. Original authored source layouts remain editable.
- owner_app_icon.png copies the original 407x273 logo pixels into a transparent
  407x407 square. Builder and PlayerSettings icon references use this asset.
  No owner logo redraw/nonuniform stretching. No other ProjectSettings changes.
- OwnerUiPlayerReview is an opt-in native diagnostic. The guarded normal-window
  runner uses an isolated named profile and checks shared standalone input prefs.
  Explicit non-tournament -tp-uireview skips online sign-in; normal launches are
  unchanged. It invokes real UI raycasts/callbacks, not physical OS input.

## Exact validation and limits

Evidence is under owner-ui-u8-evidence beside this report. Logs/Builds themselves
are ignored and do not transfer. No full EditMode/PlayMode suite was run.

- Foundation v1: 4 tests, 3 passed, 1 failed. Override contract, actual picker
  lighting and actual startup/no-Back/terms passed. Front-end expected the old
  TumpSettingsCanvas name. The source assertion was corrected to OwnerSettingsCanvas.
- Foundation v2: 2 tests, both passed, including the extended shape override
  contract and title/play/credits/settings return route. Both XMLs are retained.
- Picker lighting matched authored ModelPreview ambient (.568,.504,.425); only
  preview key/fill lights affected layer30. No foreign-light leak reproduced.
  Do not recolour approved characters merely to match flatter portrait art.
- Internal Win64 build v2 succeeded: 1071 MB, 36 seconds, protocol37. Artifact was
  Builds/owner-ui-final-review/TumbangPreso.exe. Runtime.dll SHA256:
  E8C548461AFA7BA870B0FAD7C5A2B692A6807FACC5E9B57C30EFC85FB5276D85.
  Built from 6e7a2b13 plus these U8 source changes; internal stamp is
  6e7a2b13ad73+dirty. It is not the destination's future build identity.
- Native v1 FAILED because the first runner mistakenly used -batchmode, which
  NetBootstrap treats as dedicated-server mode. It did not reach menus. That
  launch mistake is corrected in the saved runner; its failure receipt remains.
- Native v2 reached actual cold startup, Guest, home and settings. It recorded
  normal and reduced motion, then FAILED its reduced-motion scale assertion.
  Exit1, sharedInputUnchanged=true, no pre-existing named-profile files needed
  restoration. Process15116 and its runner session are finished. No active owned
  Editor/player remains at transfer. No real online account/social action occurred.
- Normal:29 frames, measured span2.256s, scale .983771..1.025. Reduced:30frames,
  span2.284s, scale1..1.019501. Reduced CSV decays from1.019501 to1 immediately
  after enabling the setting and remains1 during subsequent pointer actions.
  Source OwnerUiMotion lerps existing scale toward1. This suggests the assertion
  includes transition settling rather than continuing hover/press animation;
  it is an inference, not a resolved regression. Separate preference transition
  from steady reduced-state behavior and qualify both, without weakening checks
  to hide motion. Preserve this failed evidence.
- Native v2 stopped BEFORE Classic/Hero preparation, matches, pause, results and
  rematch. Those planned driver stages have NOT passed. The first startup PNG
  caught entry animation before logo/button labels appeared; it is raw timing
  evidence, not a qualified final startup composition. Source/Editor startup
  no-Back proof is separately in U1. Future final stills must wait for entry.
- Player PNGs are actually1920x1080, despite runner startup arguments1280x720;
  inspect applied game display settings before claiming a smaller resolution.
- Ordinary-speed MP4s use real capture timestamps. They contain no generated or
  interpolated frames. Physical mouse/controller/phone input was not certified.
- U6 training v2 still has its earlier XML pass/Unity requested0/native process1
  limitation. This native review does not settle that training-specific issue.

## Exact next work on the destination

1. Discover actual paths/tools, preserve local work and pull ASTRAReworks safely.
   Read newest AGENTS/ledger plus this report; do not trust historical PIDs/paths.
2. Re-open/render the old PDF visually and make a deliberate per-screen design
   plan that answers the owner's rejection. Use its layout ideas and the newer
   palette/type language to create NEW art and backgrounds with imagegen and
   distinct native editable compositions. Retain approved original-controller
   treatment, actual data/actions and no Back on startup login. Inspect outputs
   critically in-game, not just as concept images. Do not spam the orange pattern.
3. Review sensitive async account/settings/join and navigation edges; preserve
   services and profiles, never send real social messages for testing. Inventory
   actual active dialogs/tooltips/nameplates/notifications/unusual states rather
   than mechanically replacing every inactive old-builder string match.
4. Finish motion/reduced-motion qualification. Fix the native driver's initial
   settling/capture timing issue based on evidence. Build a new INTERNAL player
   only after source changes, via the guard and explicit output. Run a fresh
   native output/profile; do not reuse either failed receipt as a pass. Complete
   the real two-mode routes/pause/results/rematch plus targeted missing states.
5. Check real input/focus/Back ownership, meaningful keyboard/controller/touch
   paths and supported smaller/awkward aspect ratios. At115% touch scale, inspect
   default/saved-layout overlaps without silently replacing owner coordinates.
   Review ordinary-speed animations, hierarchy, contrast, readable font sizes,
   restrained information density, cultural warmth and distinct screen purpose.
6. Save honest evidence and push coherent batches. U8 may need substantial visual
   rework, not just a final test. After UI is genuinely done, work through the
   complete GAMEPLAY_RESUME_AFTER_UI.md list autonomously until finished.

No agents/forks, Figma, reset credits or Desktop updates are authorized. Only
focused relevant tests. Parallelize independent work and draft outside imported
Assets while an Editor run is active. Keep the ledger fresh through compaction.
