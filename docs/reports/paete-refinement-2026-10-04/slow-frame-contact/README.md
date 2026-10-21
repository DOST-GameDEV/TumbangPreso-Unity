# Actual tag contact across slow frames

A focused authored-map replay test failed after the headless fixes were published:
the hand missed visible body bounds by0.246m. A second instrumented reproduction
measured0.221m bounds gap and0.686m actual skinned-surface gap. The baseline
images and native receipts are retained outside the checkout; compact raw
measurements, timing CSV and result XML are retained here.

The timing trace shows the live tag clock advancing0.001 ->0.018 -> expired in
a0.328s frame. The0.12–0.22s touch window never appeared in a recorded source
frame. Replay then trimmed its end at0.18s between sparse actual samples.
The candidate shows the contact peak once if a frame crosses the whole window,
without slowing/resetting the actual gesture clock. It retains the first real
recorded follow-through sample, rather than manufacturing a replay pose.
Combat range, scores, cooldowns, movement and recovery are unchanged.

The original close-contact check now passes: bounds gap0, actual skin gap0.0343m,
body shift0.153m, torso lean22.9degrees, arm scale1.0. Its existing alpha,
restoration, unchanged motor and exactly-one-score assertions remain. The
fixture samples the retained endpoint (which can now be a real post-hitch frame)
instead of assuming that frame always occurred at exactly0.17s after acceptance.
No distance/contact tolerance was relaxed. The before/after target was inspected.

Three further native cases pass: far authored-map contact, continuous isolated
replay motion, and old contact metadata neither aiming the next miss nor awarding
a score. A proposed60Hz capture-delta control failed, but its trace shows that it did not
represent60Hz gameplay: simulation advanced only0.053s during2.66s of unscaled
replay playback. `captureDeltaTime` changes the game clock while the software
renderer/replay still advances on wall time. That newly introduced control was
removed rather than used as a normal-rate certification; its failed XML and
timing trace are retained. Existing isolated-world near/far contact cases with
unmodified clocks both pass. Their live source deltas include0.0125–0.0683s
frames through contact, contrasting with the0.328s authored-map failure.
In total six focused native cases pass across the three candidate runs. These are
Linux Editor focused checks, not Windows/multiplayer/release-package acceptance.
