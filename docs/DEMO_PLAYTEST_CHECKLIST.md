# Demo playtest checklist for September16

The full project queue remains active. This list puts visible demo checks first
and records what we still need to test together. Do not mistake source changes
or Editor screenshots for a verified native demo build.

## Current evidence

- Latest source includes the final supplied menu/login and delivered motion video
  (nativev22), recovery menu-input fix (nativev23), and client round authority fix
  (nativev24). Both modes' delayed rejoin/intermission/next-round paths PASS onv24.
  See reports/client-round-boundary-2026-09-16. Older candidate metadata below is
  historical, not the latest binary. No complete candidate freeze is claimed yet.

- Controller map/settings now match the dark theme while retaining the central
  artwork,18callouts and real connector lines. Focus highlights the correct control.
  Open it directly from Settings > Controls > Controller map. Nativev11 verifies
  entry/three window sizes/return; physical controller comfort remains pending.

- Latest correction: Classic people are cosmetic. Their obsolete stat block is
  removed, and old personal movement/contact multipliers are now neutral. Core17/17
  and actual-motor/picker2/2 checks pass. All12choices walk the same measured distance;
  equipment handling is retained. Protocol38/nativev9 qualification is in progress.
  Older preserved builds still have the unwanted character multipliers.

- The owner requested a second picker revision: less blank space, Darumadrop
  headings, icon controls and no redundant stat fractions. The compact source has
  passing34entry/ten-size picker checks; other changed heading routes and a newer
  native candidate are still being qualified. v6 is the preserved earlier version.

- Revised PC loadout now passes34entry/ten-size Editor checks and nativev6's
  icon-only selection, mouse drag, save and both-mode routes at960x540/1366x768/
  1920x1080. Selected identity appears once beside the enlarged model. The roster
  contains portraits only. Original login/models are preserved. Human approval is
  still pending; see reports/pc-loadout-2026-09-15.

- Nativev5 includes display-pixel previews and movable touch tools and passed the
  existing Windows UI/both-mode short-match/result/rematch loop. The owner has
  since requested a new PC loadout composition, so v5 predates that revision.
  No concurrent Unity/player job ran during v5's gameplay sample: at1366x768,
  Classic171.8FPS/Hero186.1FPS averages and33.3/26.7ms maxima in roughly28s windows.
  These short offline-bot samples do not qualify worst-case ability overlap or
  all PCs. Owner playtest and the complete gameplay verbs remain open.

- The rematch-map defect is fixed and qualified in nativev4. Both modes' real
  connected votes reload BayanPlaza on both processes and start the new round.
  The offline/native UI path also checks the actual next scene. Older candidates
  still contain the old-map restart defect.

- D1-D9 UI changes and the8-round/dark-settings/four-sampayan correction are
  committed through8f21e603 with focused tests and in-engine screenshots.
- D10 credits/queue/touch and the Play heading fix have scoped passing evidence.
  The heading is visible at ten PC sizes, including the formerly failing1366x768.
  The touch toolbar can now move above/below controls; the focused viewport test
  exposes three covered controls without moving their saved positions. Pixel-sized
  model previews also pass their regression and actual picker checks. Both source
  changes postdate the preserved nativev4 candidate.
- Fresh internal nativev2 passed Guest/settings, both short custom modes, real
  results, Classic rematch/leave and Hero return at1366x768. Normal defaults were
  verified8 before the custom rehearsal. This is a native-loop baseline; remaining
  gameplay, LAN, clean performance and human review are still pending.
- All18 body/FPP views were captured. Nemu's original finger steps were replaced
  with block palms; its body/FPP view and three real throw paths passed focused
  checks. Nativev3 and v4 include the Nemu correction.
- Hero/gameplay reports qualify their named cases only. Whole-kit ordinary play,
  device feel and the owner's visual approval remain separate.
- The18default hero slots accepted real presses/releases and showed their declared
  body actions in controlled recordings. All six ultimate presentations were
  inspected; actual opponents, alternate kits, interruption and owner feel still need testing.
- A direct two-process Hero Strike connection passed into round2 with matching
  protocol/map/defender/structural state and clean client departure/bot takeover.
  Both modes' two-process rematches also pass in v4. Physical two-PC LAN and venue
  Wi-Fi remain unverified; broader rejoin cases stay in the gameplay queue.
- Nativev3 includes the Nemu correction and passed two fresh native loops. The
  repeated frame sample is fast on this PC but retains isolated hitch observations.
  Nemu's rebaked swim/recovery poses, both-mode water retrieval and the accelerated
  complete8-round rotation checks pass. Human feel/venue checks remain unticked.

## First visible checks when the owner returns

- [ ] Start the exact recorded demo executable twice. It opens without errors,
  retains the approved old login and lets Guest enter without an account.
- [ ] Use Play, character/equipment selection and Back. Check the chosen character
  matches the portrait, first-person arms, clothes and equipped slipper.
- [ ] Start normal Classic and Hero Strike. Both show Round1/8. A shorter custom
  demo match must be explicitly selected and labelled; normal defaults stay8.
- [ ] Throw, retrieve, defend the can and recover from a fall. The direction guide
  stays visible and indicates general aim. Early/moving throws are less accurate;
  holding steadily should feel more controlled without a guaranteed landing mark.
- [ ] Try each existing hero's two skills and ultimate at normal speed. Check
  different casts, clear effects, understandable cooldowns and interruption.
  Look for detached slippers, clipping and effects that linger after they should end.
- [ ] Show Dante's stone protection/earthquake and Nemu's ghost-to-monster
  sequence. Keep their approved forms and distinct visual identity.
- [ ] Open the grey/dark settings during play, return with Back/Escape and resume.
  There should be no stuck cursor, input or paused-looking live match.
- [ ] Finish a match, read the standings/reward summary and rematch. Confirm scores,
  next defender and controls reset correctly. The player should also return to menu.
- [ ] Look around Eskinita: four varied sampayan lines, believable ties/posts,
  fixed pegs and moving cloth. Check the other existing maps used in the demo.
- [ ] Listen on the actual speakers/headphones and check smoothness on the chosen
  display. Test the demo resolution and one smaller window, then restart once more.

## Secondary checks, continuing after the visible path

- [ ] Two-player LAN host/join, round, rematch, clean leave and rejoin. Record exact
  machines/build IDs and whether owner/observer skills agree. Keep offline fallback.
- [ ] Controller and touch paths using real devices when available. Simulated
  UI callbacks do not certify physical hardware or comfort.
- [ ] High-DPI model preview sharpness, secondary dialogs, complete settings and
  reduced-motion transitions. Keep the original controller illustration.
- [ ] Remaining lifecycle, variants/counterplay, movement/recovery/swimming,
  spectator and performance queue. Keep working before tomorrow when feasible.
- [ ] Deferred Inday framing, then the approved seventh hero/map LAST LAST.

## Candidate record to fill after qualification

- Executable/data: Builds/demo-2026-09-16-v11/TumbangPreso.exe and its adjacent data folder.
- Runtime SHA256:8F4CE2F929F7AA9BA910A9A8987F660895F2E58DEB6477F9D5918EB65E64B61F.
- Protocol38: use matching current builds for LAN. Actualv8(protocol37) refusal passed.
- Native routes/matches: both modes' short custom loops, Classic rematch and menu return passed.
- Nativev3 measured1366x768 quiet repeat:173.9/179.1FPS averages, p99 about10ms, isolated
  maxima36.7/90.0ms. An earlier363.3ms Classic outlier has no confirmed cause.
  Nativev4 checks ran concurrently and do not provide clean performance evidence.
- Known issues or intentionally deferred features: see current ledger; not yet frozen.
- Fallback: internal v4 is preserved while newer source is qualified. Relaunch the selected executable and
  use Guest -> Play -> With bots if a demo session needs restarting.

Update this file with exact evidence and open items as work progresses. Leave
human approval boxes unticked until the corresponding playtest actually happens.
