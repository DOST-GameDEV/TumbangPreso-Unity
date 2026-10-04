# Immediate catch recognition upgrades

The live Feedback refinement asks Single Catch to change to Double Catch rather
than waiting for the current message to disappear. The same newer actor/match/
round now replaces a lower catch immediately and gets the normal 2.5-second
interval. A waiting catch is upgraded in its existing queue position. Unrelated
recognition and other actors retain their order. Lower/new-chain catches do not
replace higher current catches. No scoring, gameplay or wire changes.

## Evidence

- Native baseline fails the actual replica-event route: expected DOUBLE CATCH,
  actual SINGLE CATCH immediately after the accepted upgrade.
- Corrected native case passes current Single-to-Double-to-Triple replacement,
  complete lifetime, a different actor staying queued, queued Single-to-Double
  coalescing, generated title geometry and unchanged local score.
- Existing stale/duplicate/queue/clean-feed/rematch/round regression passes in an
  isolated 960x540 run. Its rendered MULTI KNOCKDOWN title was inspected.

Two distinct native cases passed across the retained results; this is not a clean
combined-suite pass. The first two-case run exceeded memory headroom and its
unchanged full-HD capture case timed out at 180 seconds, while the new case
passed. One bounded candidate-only repair used the existing Render width/height
parameters at 960x540 for that old capture and ran it alone. All behavioral
assertions remained unchanged. It passed in 4.89 seconds, but the outer memory
guard still requested shutdown during the launch. Profiles restored. Retain both
failed/guarded receipts; do not claim full-HD or performance qualification.

The source regression retains its default capture size; only the candidate call
was reduced. No refreshed player, actual peers or human verification claimed.
Warning height/opacity, ambience and circle-radius refinements remain separate.
