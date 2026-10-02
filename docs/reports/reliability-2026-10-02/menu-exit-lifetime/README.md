# Retire persistent simulation when leaving a match

The match and round directors, together with the break presenter, survive arena
unload. `LeaveMatchToMainMenu` previously stopped transport and reset launch flags
while leaving a live round or intermission active. Its clock could keep ticking,
and a persistent break could advance an abandoned match.

After recording the existing telemetry and stopping transport, the exit now calls
`HalftimePresentation.End(false)` and the existing Round/Match reset methods.
This clears registered actors and active simulation without synthesising MatchEnded,
RecordReady or a completed result. The existing HOME route remains unchanged.

Native EditMode acceptance: **three reproduced lifecycle failures and one ticking
control**, then **4/4 passed** with the identical repaired fixture. The tests invoke
the actual public exit API, verify live/intermission state and clock retirement,
no manufactured MatchEnded or round advance, and repeated-exit safety. A control
verifies that the round ticks before exit.

The first baseline had four setup failures because the fixture assumed the Editor
could stream the shipping HOME scene. One bounded fixture correction retains the
product assertions, restores the requested lobby mode, and expects the exact
scene-unavailable error when necessary. When the scene is available, the existing
same-frame duplicate-load latch suppresses the unrelated load. These tests prove
the exit API's persistent-state cleanup, not a rendered HOME, a physical pause-menu
click or actual transport teardown. No scene/settings asset was changed to make
the fixture pass. No second fixture repair was attempted.

Sessions: initial61758, repaired baseline93401, final97844. All jobs terminated,
completed profile/input preservation and released their leases. Source snapshots,
raw XML, job receipts and guard output are preserved. Qualification logs are
`C:/Users/matth/Documents/Codex/work/tump-feedback-0930/Logs/menu-exit-lifetime1002`.
Product source is one cleanup hunk in SceneFlow; no director rule, authored
presentation, account schema, protocol or default round setting changed.
