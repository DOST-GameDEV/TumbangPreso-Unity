# Match entry and custom map choice, October 3

## Behavior

- Queued games retain character select and map voting.
- Custom START GAME opens timed character selection for the room. MAP VOTE in
  RULES / THE ROOM is OFF by default, using the host-selected court. ON opens
  the ballot after lock-in. The host owns the phase, deadline and final start.
- Normal arena entry is automatic: slow map overview, four distinct player
  greetings, camera return, 5 / 4 / 3 / 2 / 1 / START.
- There is no second user-facing READY step. The authenticated seated-peer
  loaded barrier remains. Gameplay is held until countdown completion; phase
  cancellation restores the clock, camera, visual offsets and fresh inputs.
- Protocol 142 is required for the new preparation phase message and countdown.
  MapVote is appended to custom rules; old records default it to false.
- Practice and tutorial continue to use their dedicated installer paths.

## Focused evidence

Final native PlayMode cohort: 30/30 passed in 12.9169404 seconds. Normal exit0,
no resource guard stop; peak process-tree RSS 3325997056 bytes, container
6003793920 bytes. The 19 frozen input hashes match the validation overlay.
Named profile, EditorSettings and QualitySettings restored.

Coverage includes custom selection-to-vote policy, map choice default/clone/wire,
actual MAP VOTE button changes, absence of a manual-ready toggle, exact packet
framing, authenticated host phase reception, stale phase rejection, cancellation
when a required human leaves, observer cancellation, five ticks and START,
consumed duplicate starts, clock release, legacy manual-ready suppression,
four real-rig rotations, physical-root stability, same-phase pose restoration,
idempotent cleanup and existing scoped ready/countdown receiver controls.
This is in-process host/observer and real Unity component evidence, not a
multiple-player network session or packaged Windows build.

## Findings and retained failed attempts

- The first pose fixture sampled before LateUpdate. A late-frame observer now
  measures the real presentation consumer without manually invoking it.
- The UI fixture initially searched below the controller instead of its separate
  owner-painted canvas. The actual canvas lookup now exercises the real buttons.
- Native images exposed a falling spawn clip underneath the greetings. Arrival
  now explicitly establishes idle and exclusively owns animation while posed,
  so normal locomotion layers cannot replace the base during the frozen phase.
- Pose restoration now compares against the neutral base in the SAME held phase.
  Restarting a new phase deliberately restarts idle and was an invalid baseline.
  The original restoration failures are retained; tolerance remains two degrees.
- Final review separated lobby START from selection completion. Repeated START
  clicks can open the stage but cannot complete its lock-in.
- Compilation/import and some shutdowns crossed the RAM guard. Completed XML
  is distinguished from guard-free process completion. Exact idle compiler
  retirement and advisory release of clean task-file pages preceded fresh runs.
  No limits were weakened, source files deleted or installed tools removed.

## Full-route and graphical limits

The complete custom-room test reached a real local LAN host and character
lock-in, but did not finish: its null-graphics attempt crashed Unity's native
shadow renderer; the subsequent real-graphics full-scene attempt hit the memory
reserve. No passing full-route receipt is claimed. Dedicated full-flow cases
remain in the suite for queued voting, custom voting ON and host-selected maps.

Final native graphics checks: 2/2 passed in1.4390258seconds, exit0 with no guard stop. Peak tree RSS4567359488bytes and container7206481920bytes. These repeat two of the30 cases, not two additional unique cases. Four distinct standing-pose images were inspected; camera samples allow a full render frame after each pose change. See poses/. A small body stage
and actual UI controls cannot establish full-court direction, real peer timing,
packaged Windows acceptance, performance or human taste. Practice/tutorial
routes are preserved by source integration, not newly playtested here. The new
5 and 4 ticks use the existing voice lookup; no voice assets were synthesized.
