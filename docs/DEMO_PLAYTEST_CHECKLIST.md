# Demo playtest checklist for September16

The full project queue remains active. This list puts visible demo checks first
and records what we still need to test together. Do not mistake source changes
or Editor screenshots for a verified native demo build.

## Current evidence

- D1-D9 UI changes and the8-round/dark-settings/four-sampayan correction are
  committed through8f21e603 with focused tests and in-engine screenshots.
- D10 credits/queue/touch changes are unfinished. Queue passed. A Play heading
  disappears at1366x768 in a repeatable capture; small touch labels also need work.
- A fresh native demo candidate has not yet been qualified. The old source-PC
  native review stopped early and must not be presented as a current pass.
- Hero/gameplay reports qualify their named cases only. Whole-kit ordinary play,
  device feel and the owner's visual approval remain separate.

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
- [ ] Show Dante's stone protection/earthquake and Phaister's Nemu ghost-to-monster
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

- Executable and build-data path: pending.
- Source commit and executable/runtime hashes: pending.
- Native routes/matches passed: pending.
- Measured frame behavior on this PC: pending.
- Known issues or intentionally deferred features: see current ledger; not yet frozen.
- Preserved fallback artifact and restart steps: pending.

Update this file with exact evidence and open items as work progresses. Leave
human approval boxes unticked until the corresponding playtest actually happens.
