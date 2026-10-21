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

Follow the current owner directives in AGENTS and COMPETITION_COORDINATION.
Use one heavy job per machine and preserve contributor reservations. Source work
can continue independently while native validation runs. Do not create workers
or agents merely because a slot is available.

`tools/run_unity_job.py` coordinates cooperating jobs and defaults to SERIAL.
It delegates profile restoration to this checkout's guard, claims project,
physical Library, named profile, Editor preference hive and declared ports,
may apply historical memory/admission checks, and waits for outside Unity/game-player
processes. The October4 owner directive explicitly removes those automatic
execution barriers. Reuse its preservation helpers or launch directly with exact
profile/input restoration when an obsolete admission check blocks authorized work.
GPU and build jobs remain exclusive. Timeout handling only stops verified owned Editors
and lets their guard restore data; it preserves a lease if restoration is pending.
Use a dedicated output and inspect fresh XML as usual; a scheduler receipt alone
is not a test pass.

`tools/prepare_unity_test_workers.py` prepares new validation-only worktrees with
separate physical caches and company/product identities. This matters because
named profiles alone do not separate the Editor PlayerPrefs registry. Unity keys
that registry by company/product. [Unity's PlayerPrefs reference](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/playerprefs).
Existing worker directories are refused and Library copying is explicitly opt-in
from an idle matching Editor cache. These workers must never build shipping
players. The updated guard derives both save and preference roots from the actual
ProjectSettings identity; older guards cannot support this worker isolation.

Optional CPU overlap requires separately isolated writable caches, profiles,
preference identities and ports. Do not infer isolation from available RAM or
named profiles alone. The standing default is one heavy job per machine; honor
the owner's current PC use and keep the other machine's coherent job intact.

The preservation helpers in `tools/run_unity_guarded.py` select the editor and
preserve named-profile files/shared input preferences. Direct local Unity launches
are owner-authorized when they preserve those inputs and restore them after the
actual parent terminates. Verify ProjectVersion/install paths and process ownership;
observe real progress rather than treating a chosen RAM reserve as a blocker.
The older guard is a pass-through runner: **do not call it with --help**.

Use a focused pass per coherent unit. Diagnose actual failures and change the
approach when needed; a self-imposed retry ceiling is not proof work cannot run.
An actual product failure justifies a specific fix and its relevant regression
check. Preserve failed attempts and avoid unrelated fixture repair, weakened
assertions or unchanged passing loops.

## Visible Operator And Window Ownership

Select a fresh returned app/window and verify its actual contents after activation
before sending input. Returned window metadata alone is insufficient: October5
local-pair evidence showed a requested client window paired with an occluding
host screenshot. Never infer the intended target from a stale handle or picture.

When using a read-only desktop crop, require the expected PID and foreground HWND
to match the target before accepting the image as game evidence. Reject occluded
captures. Preserve failures without calling them rendered acceptance. Do not
close ChatGPT/Codex, entire browsers or unrelated windows. The owner's close-action
audit keeps close shortcuts stopped until the target/focus issue is resolved.
Physical Escape stops Computer Use; resume app input only after explicit owner
authorization. Personal PC use does not stop authorized source work on either side.

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

For paired LAN acceptance, use `tools/run_lan_peer.py` on each machine with the
SAME accepted player and full artifact manifest. Pass the agreed `--protocol`,
`--artifact-manifest` and `--runtime-sha256` explicitly; there is no legacy Runtime
or protocol default. `--character-pick` defaults to Hero index0 and may differ by
peer using valid picks from that artifact's roster. The fresh profile seed includes
the pick so the normal lobby can pass character selection.
The current LAN scenario uses the canonical eleven-field rules wire with map
voting explicitly disabled. Its strict Core parser check receives that same wire;
the completed-arrival helper retains its legacy default for historical scenarios.
Ready, natural match end, terminal standings and each peer's own saved
history/witness checks remain.
Before launch and after termination the runner checks every packaged file against
the manifest, source/build identity and executable/Runtime/Core hashes. It also
checks that the manifest itself stayed unchanged. Agree host availability/listening
before joining. Local runner unit checks do not establish a real peer pass.

Use the guarded GameBuilder with an explicit internal `-buildOutput`.

`tools/run_host_loss.py` also requires explicit `--protocol` and
`--artifact-manifest` alongside its source/Runtime/build receipt arguments.
It uses the same full artifact validator and canonical rules as the LAN runner,
checks manifest stability after owned processes retire and seeds valid Hero
choices for both fresh profiles. Explicit artifact classification can account
for preserved importer churn; build preservation and identity gates still apply.
The loss scenario still requires both live receipts before terminating only its
owned host and refuses resumed rounds or fabricated completion/results.

The builder can purge a prior player at its target; choose an owned candidate path
and preserve any needed existing output. Never target the Desktop player by default.
Verify executable/data and run the exact produced player before claiming it works.

Keep profiles,source/supplied art,contributor dirt and unrelated processes intact.
A stale lockfile is not permission to kill unrelated Unity processes or delete
their locks. Identify ownership first. Record blocked cleanup without trying a
different destructive route. See [workstation setup](WORKSTATION_SETUP.md).
