# Training completion respects the paused menu

A player can complete or skip a training lesson, then pause during its 0.70-second
completion beat. Previously AdvanceAfterBeat waited in real time and entered the
next lesson while the menu remained open and Time.timeScale was zero. EnterLesson
can change roles, targets, actor position and lesson effects. The ordinary training
Update already refused work while pause or loading owned the screen; the delayed
transition bypassed that boundary.

AdvanceAfterBeat now retains the same completion beat, then waits while Panel.AnyOpen
or HubLoading.Visible before entering the next lesson. Completion feedback, skip
deduplication and hero behavior are unchanged.

## Native evidence

The three PlayMode cases use the current public SkipFromUi callback and its actual
coroutine, the actual PausePanel, and real CharacterMotor/Lata components in a
minimal arena-classified scene. The route dependencies are supplied directly; this
does not load or qualify the populated shipping map.

- Original run43529: exactly3 cases,2 controls passed and1 intended causal failure.
  With Panel.AnyOpen and a paused clock, the lesson was Look instead of Ready.
- Candidate run17680: exactly3 cases passed. Pausing retains Ready past the real-time
  beat, closing the menu permits Look, unpaused completion still advances, and
  repeated skips advance exactly one lesson.
- Identical fixture and valid32hex metadata for both runs. Zero fixture/tool repairs.
  Source original is retained in MAIN Logs/training-pause-transition1002.
- Each preparation completed with exit0 before its dependent native launch.
  Exact3 owned inputs match MAIN/qualification; all12527 protected qualification
  hashes remained unchanged, including the separately qualified LAN receive fix.
- Unity6000.5.8f1, graphics PlayMode, profiletraining-pause-transition1002,
  GPU job2048MB with2048MB reserve and450-second ceiling. Both guards are terminal,
  preservation completed and no lease remains. No task browser or preview opened.

Raw baseline/candidate XML and job receipts, owned-input manifests, case counts and
the protected-input result accompany this report. Full native logs and protected
hash snapshots remain in qualification Logs/training-pause-transition1002.

This accepts the real pause/resume completion boundary and its two nearby controls.
The loading branch shares the existing Update predicate but was not independently
exercised. There is no standalone build, complete tutorial, physical controller,
touch, multiplayer, performance or whole competition-readiness claim.
