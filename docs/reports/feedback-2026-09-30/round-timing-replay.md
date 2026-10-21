# Revised round timing and halftime playback

Harry's newer Feedback refinement (hidden record QA_TUMP_0045) supersedes the
previous uniform10-second/no-replay choice. Ordinary round standings last3seconds.
Halftime remains10seconds, including retained authoritative footage followed by
standings. Protocol99 requires matching builds for this derived timing.

## Behavior

- The host still authors one start/deadline. Late clients join the remainder;
  neither a repeated packet nor a changed timestamp can reopen a consumed break.
- Both breaks hold simulation and gameplay/UI input. No skip or practice dismissal
  is restored. Only the host advances the round, and never beyond the final round.
- Halftime selects a retained canonical highlight, preferring audience-ready bytes.
  The existing render-only replay and contact slowdown return. Missing, late,
  reduced-motion or failed footage gets an honest fallback on the same deadline.
- The frozen frame stays cached. Its overlay alone hides while replay is visible;
  simulation/input remain locked. Standings restore those same final-image pixels.
  This avoids placing the order259 freeze over the order240 replay canvas.
- Existing introduction cancellation order and requested-clock restoration remain.
  No character mechanic, authored material, animation or protected hero changed.

## Native checks

Unity6000.5.8f1 LinuxOpenGL/llvmpipe, isolated named profile, mip2 texture residency,
one guarded job. The eight-case focused run passed8/8 in49.95seconds; runner exit0
in81.91seconds. No additional cgroup OOM or memory-guard stop.

1. Ordinary3-second host deadline, actual held input, immutable frame, next round.
2. Late ordinary client uses remaining2seconds, cannot advance/restart the boundary.
3. The last displayed overlay is frozen before its presenter closes.
4. Cancelling an introduction cannot release the new break's clock/input hold.
5. Abort restores requested time scale and the original enabled UI input modules.
6. A real accepted catch is retained across rounds, visibly renders changing poses
   at halftime, transitions to standings with unchanged scores/simulation/image,
   and reaches round5 only on the10-second deadline.
7. A late halftime client gets honest missing-highlight fallback and cannot restart.
8. Shared ordinary/halftime timing and no break after the final round.

The first launch did not execute tests: a missing CameraSystem import in the new
fixture failed compilation. One import-only repair produced the eight-case run.
Capture inspection then found the ordinary fixture had reused the preceding test's
magenta cached overlay, because the match survives scene resets and its conditional
batch-camera draw saw an existing texture. Its owned test now unconditionally draws
the current scene before freezing; a single focused capture/input rerun passes1/1 in6.39seconds (exit0).
Its current-scene frozen capture was inspected and shows the3-second timer.
Runtime source stayed identical for that fixture-only correction.

## Scope

The retained catch and halftime standings captures were inspected at960x540. The
known cloud character-material black/speckled artifact remains visible; this is
behavior/layout evidence, not authored full-quality GPU or human taste approval.
No new standalone player or actual-peer qualification is claimed. The earlier
protocol98 Windows pair remains historical evidence, not protocol99 coverage.
Raw XML, runner receipts, owned-source hashes and captures are in
[round-timing-replay-checks](round-timing-replay-checks/).
