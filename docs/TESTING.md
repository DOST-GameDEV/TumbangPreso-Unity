# Testing And Evidence

Current policy: the smallest meaningful check for the changed behavior. Reuse
unaffected evidence. A broad gate is for a coherent integration candidate, not
every edit. The older "two launches every change" schedule is superseded.
Full suite rationale,category history and checker details remain in the
[complete prior testing guide](archive/snapshots-2026-09-27/docs/TESTING.md).

## Before A Run

Record the source/asset revision, dirty inputs, question, expected cases, stopping
condition and tooling retry count. Freeze that candidate. Use an isolated checkout
with separate writable Assets/Library/Temp/obj, a named profile, and unique Logs/
Builds outputs. One heavy job; work independently while it runs.

Use `tools/run_unity_guarded.py`, never a raw Unity launch for a diagnostic.
The guard chooses this checkout's editor and preserves named-profile files/shared
input preferences. It is a pass-through runner: **do not call it with --help**.
Check actual ProjectVersion/install paths and disk/memory headroom first. Do not
retry a previously blocked native run at unchanged headroom.

One focused pass per coherent unit, at most one bounded tooling repair/retry.
An actual product failure justifies a specific fix and its relevant regression
check, not unrelated fixture repair or weakened assertions.

## Choose The Check

| Claim | Appropriate evidence |
|---|---|
| Pure rules/serialization calculations | Existing Core/managed test; no Unity needed where code is engine-free |
| Source compiles | Unity compilation or clearly labelled direct compiler check |
| Native lifecycle/input/state behavior | Focused EditMode/PlayMode case with fresh expected XML |
| Art/layout | Native changed-state render at relevant shapes, visually inspected |
| Animation/VFX/cutscene | Actual time samples and normal-speed owner/observer playback, not only a pose |
| Network behavior | Host,owner,observer/spectator,rejoin on actual peers as applicable |
| Windows/Android compatibility | Matching target builds and actual cross-platform peers |
| Performance | Representative player timings,first/repeat use,hardware and memory cost |
| Taste or physical controller behavior | Human review/actual hardware, not inferred from synthetic input |

Direct Roslyn checks do not prove Unity imports,IL postprocessing,native behavior,
rendering or player builds. Existing reports explicitly distinguish these levels.

## Focused Commands

Run from the ISOLATED checkout. The guard sets its own project root; select
current case names from source,not this page's illustrative names.

```powershell
dotnet test Core.Tests/TumbangPreso.Core.Tests.csproj --nologo
python tools/run_unity_guarded.py -batchmode -tp-profile presentation-validation-20260921 -runTests -testPlatform EditMode -testFilter "TumbangPreso.Tests.MatchmakingWireTests" -testResults Logs/targeted-edit.xml -logFile Logs/targeted-edit.log
python tools/run_unity_guarded.py -batchmode -force-d3d11 -tp-profile presentation-validation-20260921 -runTests -testPlatform PlayMode -testFilter "TumbangPreso.PlayTests.OwnerLoginFeedbackTests" -testResults Logs/targeted-play.xml -logFile Logs/targeted-play.log
```

Use graphics for native checks. Never `-nographics` for PlayMode/rendering.
Do not add `-quit` to a test launch that must finish writing XML. Multiple test
filters are SEMICOLON-separated,not comma-separated. A bare batchmode/quit launch
can exit before a useful compilation; choose a real test or execute method.

Fresh XML must contain nonzero expected cases and the actual failures/skips.
Exit0 and `result="Passed" total="0"` are not a pass. Missing/stale XML is not a
result. Preserve logs and exact input hashes with the claim.

## Integration, Not Per-Edit Ritual

```powershell
python tools/playmode_suite.py --plan
python tools/playmode_suite.py --group match --profile presentation-validation-20260921
python tools/playmode_suite.py --gate --profile presentation-validation-20260921
```

The grouped runner discovers an exhaustive non-overlapping fixture partition and
uses the guarded launcher/named profile. A single giant PlayMode process leaked
state between fixtures; it is not equivalent to this gate. `--twice` and
`tools/qualify.py --nationals` are broader release tools,not everyday defaults.
Select them only when the coherent candidate and assignment justify that cost.

WallClock cases measure real time and belong in deliberate runs; Explicit alone
does not reliably exclude them in batchmode. ThumbFloor is a known measurement
category,not permission to call skipped touch coverage complete. Run expensive
surface-discovery cases alone when their behavior changed. The source and prior
guide preserve the detailed category rationale and historical measurements.

## Existing Tools

- Core.Tests compiles the same engine-free package source. Never duplicate it.
- Checks.RunAll uses the actual current checker list. SceneScriptCheck reads serialized
  scenes; SceneDependencyCheck opens dependencies without saving. They catch different bugs.
- InputSurfaceCheck/ScreenFocus/InputContractTests preserve controller/touch coverage.
- ShaderWarmupCollection regenerates the collection used by boot; builds refresh it.
- PersonSwapProbe,CastRestyleReview and ClipMotionStrip serve different visual questions;
  use the [rendering pipeline](CANONICAL_RENDERING_PIPELINE.md).
- tools/audit_*.py are source checks. Read their current headers/allowlists; text audits
  cannot establish native behavior or peer playback.
- tools/net_link.py,net_matrix.py and existing presentation-peer tools use actual
  players. Obsolete DebugSimulator APIs may do nothing; never infer link impairment.
- tools/run_ui_player_review.py uses an internal player,explicit named profile and
  opt-in diagnostics. Do not add player `-batchmode`: that selects a dedicated-server
  route. Synthetic diagnostic input is not physical-device certification.
- FrameRateHistogram,existing profiler markers and tools/cold_start.py support
  performance work. Preserve seeds; one bot match is a liveness floor,not an A/B study.

## Builds And Cleanup

Use the guarded GameBuilder with an explicit internal `-buildOutput`.
The builder can purge a prior player at its target; choose an owned candidate path
and preserve any needed existing output. Never target the Desktop player by default.
Verify executable/data and run the exact produced player before claiming it works.

Keep profiles,source/supplied art,contributor dirt and unrelated processes intact.
A stale lockfile is not permission to kill unrelated Unity processes or delete
their locks. Identify ownership first. Record blocked cleanup without trying a
different destructive route. See [workstation setup](WORKSTATION_SETUP.md).
