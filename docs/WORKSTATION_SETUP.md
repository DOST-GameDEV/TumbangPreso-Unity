# Workstation setup and reproducible workflow

Recorded 2026-09-14 for continuing this project on another PC. This is an
environment guide, not a claim that the whole game is finished. The continuation
prompt is delivered directly in chat. Start with AGENTS.md and the newest
ACTIVE_REWORK_LEDGER / EXECUTION_PLAN entries, not historical process notes.

## Checkout and source of truth

- Repository: https://github.com/DOST-GameDEV/TumbangPreso-Unity.git
- Only authorized work/push branch: **ASTRAReworks**. Never main.
- Old primary checkout: C:/Users/Matthew/Documents/Codex/2026-09-09/ok-x20/work/TumbangPreso-Unity.
- Pick a new local project path on the destination PC. Do not reuse the old
  absolute path blindly, reset existing work, or operate on another checkout.
- A fresh clone needs the complete Assets, ProjectSettings, Packages, Core,
  Core.Tests, tools, MapSource, ArtSource and docs folders with all tracked .meta
  files. Restore packages from manifest.json and packages-lock.json unchanged.
- Packages/com.tumbangpreso.core and Packages/com.unity.transport are local
  packages. Do not replace them with unrelated registry versions.
- Builds, Library, Logs and user profiles are ignored. They do NOT arrive through
  a Git push. Checked-in reports contain selected real evidence; rebuild an
  INTERNAL player on the new PC when required for new runtime work.

For a fresh destination directory, use git clone --branch ASTRAReworks with the
repository URL. For an existing authorized checkout: inspect status, fetch,
inspect divergence and fast-forward only if safe. Never reset to an old hash in
TODO.md. The transfer's full latest hash is supplied with the chat prompt.

## Known working versions

Destination verified on 2026-09-14: active checkout is
C:/Users/matth/Documents/GitHub/TumbangPreso-Unity-ASTRAReworks. Unity's exact
Editor/revision and .NET SDK match below. Python is 3.12.8 and Blender is 5.2.0;
do not assume the source-PC patch versions or username. The official CLI beta.5
was installed and checksum-verified at C:/Users/matth/AppData/Local/Unity/bin/unity.exe.
Use that absolute CLI path: C:/Users/matth/bin/unity.cmd is an older Editor wrapper.

The task shell omitted ALLUSERSPROFILE although PROGRAMDATA exists. Unity's
package manager failed with an undefined path before compilation. The guard now
restores this alias from the existing local ProgramData directory in the child
environment only. Explicit values/global settings are preserved. Clearing the
project package cache did not fix it; the original cache was restored. Do not
change package versions or transport source to work around this environment bug.

All 16 portable skills are downloaded under Logs/portable-skills-2026-09-14;
all 225 files match the manifest sizes/hashes. Current corresponding installed
skills were retained. The owner explicitly excludes Figma calls on this PC.

| Component | Source workstation |
| --- | --- |
| OS / target | Windows, Win64 player |
| Unity Editor | 6000.5.8f1, revision 5cb7df797b7d |
| Unity Editor executable | C:/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe |
| Unity CLI | 1.0.0-beta.5; C:/Users/Matthew/AppData/Local/Unity/bin/unity.exe |
| Blender | 5.2.1; C:/Program Files/Blender Foundation/Blender 5.2/blender.exe |
| Python | 3.12.10; C:/Users/Matthew/AppData/Local/Programs/Python/Python312/python.exe |
| .NET SDK | 9.0.317; Core.Tests targets net9.0, Core targets netstandard2.1 |
| Git / Git LFS | 2.55.0.windows.3 / 3.7.1 |
| Current renderer | Built-in RP; GraphicsSettings.m_CustomRenderPipeline is 0 |

URP 17.5.0 is in the package manifest, but that does not make this an active URP
project. Do not migrate it. Active packages include NGO 2.13.1, Input System
1.20.0, glTFast 6.14.1, Test Framework 1.7.0, uGUI 2.5.0 and Pipeline 0.6.0-exp.1.
The manifest/lockfile, not this summary, owns exact dependencies.

Use Unity Hub or the official CLI to install the matching Editor and necessary
Windows build modules if missing. Authenticate/license locally as needed; never
copy another PC's credentials. The CLI and Editor are DIFFERENT executables.
The preserved unity-unity-cli skill contains official installation and Pipeline
connection instructions. Do not upgrade the project merely to match a preinstalled
Editor. Detect actual paths before changing a runner.

Python packages used by the workflow include Pillow 12.3.0, numpy 2.5.1,
pypdf 6.14.2, imageio-ffmpeg 0.6.0 and requests 2.32.3. Use a local virtual
environment when appropriate. Install only imports needed by the selected tool;
do not copy the entire source PC's Python installation. Blender author scripts
requiring bpy run through Blender, not ordinary Python. For PDF rendering, use
the document runtime's Poppler when available; load_workspace_dependencies
discovers that runtime on the destination. pypdf text extraction alone cannot
replace visual inspection of the moodboard. Existing Unity portrait/source art
and MP4 evidence can be inspected before any rendering dependencies are installed.

## Portable skills and tool capabilities

docs/tooling/skill-reference-bundle.zip contains 16 selected skill packages,
including their relative references/scripts. skill-reference-manifest.json lists
the original provenance, entries and SHA256 of every file. It contains no account
configuration, conversation history, saved profiles or authentication tokens.

Extract into a NEW local reference directory such as Logs/portable-skills. An
agent can read the SKILL.md files and their references directly there. For skills
to appear automatically in a future Codex session, install the corresponding
plugins or use the destination's supported skill installation workflow. A ZIP of
instructions does NOT install MCP servers, create a tool, or transfer logins.
Do not overwrite newer installed skills indiscriminately.

| Skill package | Use and setup |
| --- | --- |
| unity-bug-investigation | Evidence-led Unity defects and visual regressions |
| unity-feature-implementation | Existing architecture, state ownership, implementation and focused verification |
| unity-build-validation | Honest compile/runtime/build/evidence boundaries |
| unity-project-onboarding | Consult existing project context; no repeated broad audit just to resume |
| unity-mcp-workflow | Discover/check a real Unity provider and Editor connection |
| unity-project-health-check | Read-only audit if an actual task warrants it |
| unity-ui | Detect framework before UI implementation |
| unity-ui-ugui | This game's Canvas/uGUI work |
| unity-unity-cli | Official CLI/Pipeline workflow, commands and installation references |
| game-ui-design | Includes patterns, sharp_edges and validations references |
| imagegen | Built-in image generation/editing, art direction and critical review |
| pdf | Render and visually read the 49-page owner moodboard |
| figma-use | Required before Figma JavaScript read/write actions |
| figma-create-new-file | Required before any Figma create_new_file call |
| figma-generate-design | Full composed layouts in Figma when actually useful |
| figma-generate-library | Editable components/tokens when actually useful |

The six workbench skills came from **Unity Essentials / unity-workbench 0.1.3**.
The three Unity skills came from **Unity 0.1.5-beta**. Figma was **2.0.21**;
PDF runtime **26.909.12148**; imagegen was the supplied system skill;
game-ui-design was installed under the user's .agents/skills.

Relevant destination capabilities: shell/file editing, image viewing, web search,
built-in image generation, document runtime discovery, optionally official Unity
CLI/Pipeline and Figma. Check actual available tools; do not invent tool names.
The latest primary passes used guarded Unity batch processes and repository
probes, so lack of a live Unity MCP bridge is not a reason to halt independent
work. Figma reached the free MCP allowance here; local source assets are complete.
Do not pay, bypass the quota, or delay gameplay to reconnect Figma.

Use built-in imagegen when useful; no exact GPT image model version was exposed
by that tool. No paid API fallback or copied API key is authorized. Preserve the
owner's actual logo. Generated art is a candidate to critique, never automatic
approval. For future/current tools, live system/developer/user constraints take
precedence over archived skill text, especially NO agents, NO resets and focused
tests only. Do not load all 225 skill files into every prompt; route to the relevant
skill and its required references for the task.

## Launch, profile safety and focused verification

Every Editor launch goes through tools/run_unity_guarded.py. It uses its own
repository path automatically but has a source-PC UNITY executable constant.
If the matching Editor is elsewhere, adapt only that executable path after
checking it. Preserve the profile and Editor input-preference guards.

The guard snapshots/restores existing profile file bytes and these shared Editor
PlayerPrefs: tumbangpreso.bindings and tumbangpreso.touchlayout (including Unity
hashed registry names). It verifies restoration. Named -tp-profile isolates
test files; some Editor input preferences are shared, hence both protections.
Never clear the player profile, reset all PlayerPrefs, or copy authentication
data into Git. Avoid any native review that touches a profile the owner is using.

One Editor at a time. Check process ownership first. No C# or imported-asset
edits during an active Editor run. While it runs, review footage, docs, research
or prepare nonimported files. Start Windows helpers hidden. Do not kill unrelated
Unity/player processes. Active sessions from the old PC are all retired.

Examples below are recipes for RELEVANT future checks, not a request to rerun
everything at transfer. Use new output names and ensure Logs exists.

```powershell
python tools/run_unity_guarded.py -batchmode -runTests -testPlatform EditMode -testFilter 'TumbangPreso\.Tests\.(CarrySupportExtentTests|ThrowMotionTests|ThrowGestureContinuityTests)' -buildTarget Win64 -testResults Logs/related-edit.xml -logFile Logs/related-edit.log -tp-profile owner-review-editor
```

Unity testFilter accepts regex group names in this installed test runner.
Semicolon lists previously ran ZERO tests. Always inspect fresh XML for the
expected fixture names, nonzero counts and actual failures. PlayMode rendering
requires NO -nographics. A process exit code alone is not proof. For core-only
changes use dotnet test Core.Tests/TumbangPreso.Core.Tests.csproj --filter with
the appropriate xUnit class/method filter. No routine full Core/Edit/Play suites.
Do not invoke a broad qualification wrapper merely because it exists.

```powershell
python tools/run_unity_guarded.py -batchmode -executeMethod TumbangPreso.EditorTools.GameBuilder.BuildWindows -buildTarget Win64 -buildOutput Builds/TransferReview/TumbangPreso.exe -logFile Logs/transfer-review-build.log -tp-profile owner-review-editor
```

Always give buildOutput. The builder's default can write to the owner's Desktop,
which is NOT currently requested. It purges its previous selected build output:
use a deliberate internal folder and check that player is not running. Do not
hand-delete an unchecked computed path. Verify exe AND data/build identity, then
launch that exact artifact. New screenshots must be captured in-engine, never
generated/painted as if they were game evidence.

```powershell
python tools/net_throw_matrix.py Builds/TransferReview/TumbangPreso.exe --mode classic --out Logs/throw-classic-new
python tools/net_throw_matrix.py Builds/TransferReview/TumbangPreso.exe --mode hero --delay 150 --rejoin --out Logs/throw-hero-new
```

These TWO matrices run SEQUENTIALLY: shared ports 8950/8951 and profiles
throwhost/throwowner/throwobserver. Runner restores only those named profiles.
The three processes within each run are intentional networking peers, not agents.
The probe's batchmode is valid for state replication but not render timings.
Use tools/net_roof_matrix.py and net_familiar_matrix.py only for their related
changes; read their current flags first. tools/graphics_review.py uses a normal
hidden rendered player, not batchmode, and measures actual available counters.

tools/encode_motion_evidence.py encodes timestamped captures with
imageio_ffmpeg, preserving their real speed; no generated/interpolated frames.
tools/playerprefs_guard.py, test_playerprefs_guard.py and
test_run_unity_guarded.py preserve the testing workflow. The old narrow
tangent-only import-dirt helper is archived as a .py.txt in transfer-evidence.
It is historical diagnostic source, NOT blanket permission to restore assets
against HEAD. Inspect a pre-run diff and back up first; retain unrelated changes.

## Source assets and editing routes

- Original branding: ArtSource/ui/owner-brand-2026-09-13, including the PDF,
  all four JPGs, newest logo/palette sheets, measured palette and source hashes.
- Runtime brand/portraits/illustration imports:
  Assets/TumbangPreso/Resources/UI/{brand,portraits,illustrations}.
- Original swimming reference: ArtSource/maps/owner-swimming-2026-09-13.
- Research and source attribution: docs/Asset_Sourcing.md,
  docs/reports/improvement-2026-09-12/map-reference-research.md and
  docs/PHILIPPINE_ABILITY_DIRECTION.md. Reuse findings; browse for new claims.
- Environment authoring: Assets/TumbangPreso/Editor/MapKit and
  tools/author_*.py (street stalls/signs, civic paving, trees, laundry, rooftop,
  animals, birds). Use semantic repeated authoring checks instead of interpreting
  changing Unity object IDs as geometry changes.
- Retained cast/animations: MapSource, ArtSource, RosterBookBuilder,
  ViewmodelArmAuthor, SwimmingAnimationAuthor, RecoveryAnimationAuthor and
  GeneratedAnimationAuthor. Preserve existing rig/action/GUID contracts.
- Paid raw control-icon pack under scratchpad/input-icons is intentionally NOT
  tracked. Runtime derivatives are tracked; obtain the owner's licensed original
  privately only if regeneration becomes necessary. Do not publish that pack.
- CC0 raw downloads under scratchpad/asset-src are ignored for size; sourcing URLs
  and runtime derivatives are in the repo. Do not mistake absent download caches
  for missing final runtime assets.

Do not transfer .codex auth/config/history, browser sessions, game profiles,
Library, entire Downloads, or installed executables through the repository.
Ordinary owner authentication on the new machine is separate from code/art setup.
