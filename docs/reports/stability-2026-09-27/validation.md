# Validation receipts

Portable receipts for this batch are in [checks](checks/): presentation 24/24,
session 11/11, initial HUD 2/3 plus rooted retry 1/1, pending join 1/1,
avatar/symbol 2/2 and selector 1/1. Versioned inspected frames are in [native](native/).
The failed initial HUD result is preserved alongside the passing retry.

## Baseline at 04886cc4

Working tree: documentation intake only. No gameplay or test source changes.
Core command: `dotnet test Core.Tests/TumbangPreso.Core.Tests.csproj --nologo --verbosity minimal`.
Result: 658 passed, 0 failed, 0 skipped, 332 ms test duration.

### Named inherited EditMode failures

Question: which handed-off hero regressions reproduce locally with graphics enabled,
including the toon light test previously failing under `-nographics`?
Stopping condition: fresh nonzero XML for the six requested classes, or a recorded
compile/tool failure. Tooling retry count: 0. Source inputs remain frozen during the run.
Profile: `presentation-validation-20260921`. Results: `Logs/stability-0927-edit-baseline.xml`;
log: `Logs/stability-0927-edit-baseline.log`. Result: 100 tests, 86 passed, 14 failed,
0 skipped, 1.5304329 seconds. Process exited; source freeze released. Both ToonLightFalloff
cases passed with D3D11 on RTX 4050. No rendering fault established for those cases.

Failures: Dante ultimate old name; six sidegrade-refresh expectations; duplicate Dante
BARRIER glyph; Phaister description 134 > 125 characters; FROSTBITE old placement radius;
charge recovery; old universal 45-second cooldown floor; old ultimate cost ordering;
old Nemu Astral Projection recast expectation. Reconcile each against current kit design;
do not restore retired skills to satisfy a stale assertion. XML preserves exact messages.

Unity generated changes: Inday arm detail assets, Rafi RosterArms, Paete/Rework colormap
metas, two protected composition PNG metas, ProjectAuditorSettings, QualitySettings.
These are not authored changes and remain unstaged; preserve before any restoration.

Performance source findings are hypotheses until native timing: boot warmup enumerates
only two maps; ability warmup initializes one particle material but not every effect.
Existing Desktop player is a dirty Sep 25 build at 85832b6b, not a current-source baseline.

## First integrated presentation check

Candidate: 04886cc4 plus the reviewed icon/copy, native interaction, cancellation and
avatar/symbol changes listed in implementation.md and menu-qa.md. Source writers frozen.
Question: do the current real powers have unique imported glyphs, valid card copy and
telegraphs matching the adopted kits? Filter: `TumbangPreso.Tests.HeroPresentationTests`.
Stop at fresh nonzero XML or actual compile/test failure. Tooling retries: 0.
Guarded graphics-enabled EditMode, profile `presentation-validation-20260921`.
Artifacts: `Logs/stability-0927-presentation.xml` and `.log`. Result: 24/24 passed,
0 failed/skipped, 0.7577935 seconds. Guarded process exit 0. No tooling retry used.

## Session lifecycle check

Same frozen candidate. Question: do cancelled or superseded host/client operations stay
cancelled, preserve successor sessions, wait for seating and retain an actionable failure?
Filter: `TumbangPreso.PlayTests.SessionRestartTests` (11 expected cases).
Stop at fresh nonzero XML with all expected cases or an actual failure. Tooling retries: 0.
Guarded graphics-enabled PlayMode, same isolated profile. Artifacts:
`Logs/stability-0927-session.xml` and `.log`. Result: 11/11 passed, 0 failed/skipped,
3.7187964 seconds; process exit 0; no tooling retry. This does not prove live Relay
or two-peer online admission.

## Native interaction HUD check

Same frozen candidate. Question: does the real HUD show the correct bound keyboard/pad/touch
control and progress for rooted escape and plant removal, and truthful reset-can cues?
Filter: the three added `TumpNativeHudTests` methods named in implementation.md.
Expected 3 cases with native captures. Stop at fresh XML/captures or actual failure;
tooling retries 0. Guarded PlayMode with graphics and isolated profile.
Artifacts: `Logs/stability-0927-hud.xml` and `.log`. Result: 3 cases, 2 passed, 1 failed,
22.2104237 seconds. Plant prompt and can-reset cue cases pass. Rooted prompt updates its
F10 label, but the synthetic rebound keyboard hold produced zero BreakFreeProgress at
test line 212. Diagnosis pending: input behavior versus fixture setup; no claim of a
root cause or completed root-escape/device coverage. No retry yet.

## Hub pending-join check

Same frozen candidate. Question: does leaving the hub cancel its real join-panel request
without allowing an older completion to interfere with a newer request? One expected case:
`TumbangPreso.PlayTests.TumpNativeJoinTests.LeavingTheHubCancelsItsJoinBeforeANewerRequestStarts`.
Guarded PlayMode, isolated profile; fresh XML required, tooling retry0. Artifacts:
`Logs/stability-0927-join.xml` and `.log`. Result: 1/1 passed, 0 failed/skipped,
0.2102692 seconds; process exit0. No retry used.

The native plant/reset images at `Logs/shots-native-ui/CourtHud-pull-available.png` and
`CourtHud-can-reset-touch.png` were visually inspected: action text and progress are
legible, and the reset touch control is highlighted. The reset fixture places another
body close to the camera; these frames establish the prompt, not general scene approval.

## Picker asset and graphic check

Same frozen candidate. Question: does every offered avatar load directly as its own sprite,
and does changing a glyph update the CanvasRenderer mesh and actual texture?
Two expected added `TumpNativePickerTests` cases. Guarded PlayMode, isolated profile,
fresh XML required, tooling retry0. Artifacts `Logs/stability-0927-picker.xml` and `.log`.
Result: 2/2 passed, 0 failed/skipped, 0.2933051 seconds; process exit0, no retry.
The current character-selection route follows separately.

## Character-selection route check

Same frozen candidate. Question: does the current selector refresh all four role tiles
and inline details for every hero at normal/large text, while the selection clock keeps
running and Classic remains power-free? One expected case:
`TumbangPreso.PlayTests.MatchArrivalFlowTests.CharacterSelectionExplainsItsKitWithoutPausingTheClock`.
Guarded PlayMode, isolated profile; fresh XML required, tooling retry0. Artifacts:
`Logs/stability-0927-selection.xml` and `.log`. Result: 1/1 passed, 0 failed/skipped,
51.1264314 seconds; process exit0, no retry. All candidate1 native processes have exited.

## Rooted-input fixture repair

One bounded fixture repair approved: configure unfocused-input policy before synthetic device
creation and explicitly enable the test keyboard/pad. Installed Input System disables devices
added while unfocused; changing background policy later does not guarantee re-enabling them.
Production PlayerInputReader uses the shared action asset. Retain the actual bound-control
escape-progress assertions; add direct device/action state diagnostics. No production input
change is justified yet. Retry only the failed rooted test after this source change. Retry
count reserved: 1 of 1. No other passed cases need repeating for this fixture-only change.

Repair applied in `TumpNativeHudTests` only; original behavior assertions retained, with
direct enabled/key/action/intent diagnostics. Retry1 launches the single rooted method,
fresh artifacts `Logs/stability-0927-rooted-retry.xml` and `.log`, same isolated profile.
All source writers stopped again. Result pending.

Retry1 result: 1/1 passed, 0 failed/skipped, 14.5510138 seconds; process exit0.
Diagnostic confirms enabled/pressed key and action, active reader, effective intent, local
simulation, unlocked input and increasing root progress (0.057 at the diagnostic frame).
Keyboard/pad/touch captures were visually inspected; the current live binding is legible,
progress is visible, and touch names the action while emphasising the Interact target.
Original escape/release/spectator assertions pass. No production input code was changed.

## Integration boundary

All receipts above describe `04886cc4` plus the reviewed local batch. Before publication,
fetch found shared branch `c3977bb4` with 40 incoming commits. Those source changes must be
integrated and checked separately; the pre-integration results are not relabelled as a run
of the later revision. No Windows performance, full gate, live Relay or physical-device
claim is made by this batch.

## Shared-branch integration

Verified local batch committed as `45c2217`, then incoming `c3977bb4` merged cleanly into
`577c1a95`. No gameplay/test source conflict; GameBuilder retained both the incoming shader
registration and the opt-in Development-build flag.

Question for the post-merge frozen check: does the combined assembly compile, preserve current
hero presentation contracts, and pass the incoming motion/light contracts affected by the
shared presentation changes? Guarded graphics-enabled EditMode, expected 37 cases across
HeroPresentationTests, MotionContinuityTests and ToonLightFalloffTests; isolated profile.
Fresh XML required; tooling retry0. Artifacts `Logs/stability-0927-merged-edit.xml` and `.log`.
Result: 37/37 passed, 0 failed/skipped, 2.4754656 seconds; process exit0, no retry.
Earlier results retain their earlier source identity.

One native interaction integration check follows because the incoming shared batch changed
the input reader used by menus, global settings/presentation and character motion. The rooted
keyboard/pad/touch method runs once on `577c1a95`; this is a changed-source integration check,
not another repair attempt. Fresh XML required; artifacts `Logs/stability-0927-merged-rooted.xml`
and `.log`. No production source edits during the run. Result: 1/1 passed,
0 failed/skipped, 14.3505603 seconds; process exit0. The current merged candidate therefore
has a fresh compile/presentation/motion/light pass and a native three-device interaction pass.
It still has no full-suite, fresh player-build, live Relay or physical-device qualification.
