# Replay archive disable and recovery

The archive previously stopped retaining all new highlights after disable and
re-enable. Binding a history while disabled also let it consume live samples.
Both failures are reproduced with the actual Unity lifecycle; the correction
passes3/3 focused PlayMode cases. This qualifies archive component recovery,
not the entire tournament replay or operator experience.

## Cause and correction

OnDisable called Bind(null), discarding the selected pose-history owner.
OnEnable restored flair/audio listeners but never the pose subscription. A new
accepted catch could be nominated but could not complete into a retained clip.
Separately, Bind subscribed unconditionally, even when the archive was disabled;
event callbacks continued consuming samples despite Unity disabling Update.

Bind now subscribes only while active and enabled. Disable detaches the event
without forgetting its selected history owner; enable reconnects exactly once.
Interrupted prop, pending clip, audio and field state is cleared, so a resumed
window cannot bridge the missing recording time. Existing disable behavior
already cleared the retained shortlist. No score, kit, authored art, pose format,
network transport or ordinary round identity behavior is changed.

## Native evidence

Laptop gamergmae, Windows11, Unity6000.5.8f1/D3D11; source base
f69a64a0350467346805fa1fd97658cf1a208a44, isolated warm qa-a worker and named
validation profile. Each run froze19314inputs and serialized heavy execution.
Filter: TumbangPreso.PlayTests.ReplayArchiveRecoveryTests.

| Case | Original | First candidate | Final fixture |
|---|---|---|---|
| Reenabled archive retains a fresh actual accepted catch | FAIL:0clips instead of1 | PASS | PASS |
| Disabled archive consumes no history even when rebound | FAIL:field samples10 to14 | PASS | PASS |
| Uninterrupted archive still retains a fresh actual catch | PASS | PASS | PASS |

The catch cases load Eskinita, position actual actors, build a fresh2.5s lead-in
through MatchPoseHistory.LateUpdate, accept a real HostResolvePunch, wait for its
aftermath, and decode the retained CATCH bytes. They do not manually invoke the
archive's private sample or retention methods. The disabled case observes its
field sample count across real player-loop frames after public Bind while disabled.

Original Unity43228/parent52729 exits2 with two expected causal failures.
Candidate1 Unity14012/parent77186 exits0,3/3. Final candidate Unity14416/parent45303
exits0,3/3 with unchanged production correction and explicit restoration of the
test helper's rules, solo-seat, bot and spectator globals. This final fixture
qualification was necessary after adding isolation cleanup, not an unchanged
repeat. No assertions were weakened or cases skipped.

All parents are terminal. Each run preserved265generated import changes before
restoring their exact qualified bytes, plus Quality and13existing preference
values. The original runner's post-run classifier initially omitted the known
ProjectAuditorSettings import delta; it was classified/restored after termination,
without rerunning the original or changing its product outcome. Raw receipts,
XML and SHA256 inventories retain that distinction. No UI-close shortcut was used.

## Scope and remaining acceptance

This is a confirmed component lifecycle defect under an explicit disable/re-enable
interruption. It does not establish that it caused the owner's networking or
physical-input symptoms. Native same-machine gameplay and decodable clip retention
are qualified; current packaged replay pixels/audio, impaired transport and human
acceptance remain separate. The desktop owns network/root queue/integration and
has received this source reservation and the causal results.

## Integrated rendering and fallback follow-up

Integrated source a9ee42365b46a64fa8be17cfbb7a8668dc886b1c also passes the two
existing ReplayVisibilityRecoveryTests: ordinary actual retained-clip Draw and
Dispose preserve original live visibility; an interrupted draw with lost world
owner restores live renderers, canvases and lights on fallback. This executes
Camera.Render through the normal case and retains the exception-path control.
It is native graphical recovery evidence, not human visual/audio acceptance.

Unity29740/parent99905 are terminal0,2/2PASS. The warm worker verified19316inputs
and applied only seven bounded source updates from the preceding qualified run.
All265generated changes, Quality and13existing preferences are preserved/restored.
No production or test edits were needed for this follow-up; passing13input and
3archive cases were reused rather than repeated.
