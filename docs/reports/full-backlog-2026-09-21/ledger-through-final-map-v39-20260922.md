# Active TUMP rework ledger

## Owner direction

Complete EVERY unfinished, autonomously actionable requirement in the preserved
[TODO](TODO.md), including remaining maps, UI, kits, qualification, Rafi and lagoon.
The owner rejected two premature stops and explicitly said a phase/checkpoint is
not completion. Keep working in this conversation without needing another continue.
The current goal is ACTIVE. Preserve task IDs, prior implementation and failures.
No questions while the owner sleeps, no agents/other chats, paid services, resets,
main edits or Desktop replacement. [AGENTS](../AGENTS.md) holds the full contract.

Latest additions: animate cloud movement, and remove Inday's pogo-like arm props/
guards in every model/FPP view in favour of normal brown arms. Her latest instruction
supersedes preserving the old guards. Other body parts, rigs and animations stay.

## Workspace and source

Development: C:/Users/matth/Documents/GitHub/TumbangPreso-Unity-ASTRAReworks,
branch ASTRAReworks. Last fetched development and origin HEAD:
e50f8c372b2acd079e87493a8328e92ea39c1d5a, pushed and remote HEAD verified.
The map/sky/Inday/preview-fix batch is published. New edge diagnostics follow it.

Validation: sibling TumbangPreso-Unity-validation, detached db976126 with explicitly
copied owned changes and generated assets. Independent Assets/Library/Temp/obj.
Use one heavy job and keep warm caches. Every Editor launch goes through
`python tools/run_unity_guarded.py`, profile `presentation-validation-20260921`.
Unity is 6000.5.8f1. No unguarded launches. Never change validation inputs mid-job.

Protocol remains48, replay schema10. Latest verified internal candidate is environment-v37:
validation/Builds/environment-v37/TumbangPreso.exe,1180MB/50s, guard9f36f40872e7.
It contains the preview-transition fix and passed the full native map and busy-Low
routes. Its stamp honestly says db976126 dirty with copied inputs; this is not the
final all-TODO candidate. Previous presentation-v33 remains. Desktop was untouched.

## Exact current action

Current job session42564: exact native environment-v39 full map review, PID27360,
Logs/environment-v39-native. Build passed1192MB/85s, guard80aa3ba39f21. Last observed
state reached the eighth map's correct ready gate. Read final result/runner JSON,
verify96views/48windows, inspect selected latest frames and compare timings. No other
owned jobs/tabs. Final4132-file dependency closure is already recovered in development.

Final19family material coverage has no unassigned families; semantic source tags,
strong repeated-save checks and full generator hooks have been qualified. Final
owner views passed1/1,64.804s/200frames. New shader/material/editor work is still
uncommitted beyond e50f8c37. Preserve all previous failures and original unrelated dirt.
After nativepass, update receipts, publish this owned map batch and continue the
remaining whole TODO. Rafi/lagoon remain unimplemented; goal ACTIVE.

Next UI preparation: --plan initially found22recent presentation/preview fixtures
unassigned to the partition. They are now explicitly placed:19arena/lifecycle contract
fixtures in match,3continuous-camera capture fixtures in capture. Plan passes all161
fixtures; this is a partition check, not a test-suite pass. tools/playmode_suite.py is
an owned new change. Before/after logs are saved in the full-backlog report.

Original dirty UI work was inspected, not absorbed: PaintedScreens.cs helper plus
stale-fixture migrations, settings note-height fix, RoundLine/HUD test catalog and
an extra halftime-round label, and an opt-in SeanVisualOnly native route. Review
those against current compact halftime/UI semantics and qualify in bounded batches;
keep original backups and don't blindly copy dependent shared pieces into validation.

Verification/publishing checklist:

1. Read the native result and runner-result JSON. Preserve any failure and fix its cause.
2. The exact native map route command is: `python tools/run_ui_player_review.py --exe
   Builds/environment-v37/TumbangPreso.exe --out Logs/environment-v37-native
   --profile presentation-native-environment-20260922 --map-surfaces-only`.
   Clear TUMP_NATIVE_MAP for all maps. That env var can isolate SaBubong if needed.
3. Require all96 map/mode/quality/view rows,8 walking sequences,48 paired process
   frame-time windows and12s native sky motion. Inspect actual images and errors.
   Run the existing Low/comfort mixed-caster busy route on the same artifact.
   Then run the existing NearFadeProbe into fresh TUMP_NEAR_FADE_EVIDENCE output and
   inspect its three band-distance captures; the optional-output test change is dev-only.
4. Encode timestamped footage with tools/encode_motion_evidence.py only after the
   player exits. Preserve failed34/35/36 receipts and mark their coverage honestly.
5. Publish the verified owned batch, fetch before push and verify origin HEAD.
   Then continue remaining material-family/hook checks and the ENTIRE TODO.
   A successful map batch does not finish the assignment.

## Preview transition defect, current fix

Nativev34 failed at the second map because the review wrote SelectedMap directly;
the setup screen owns its own index and overwrote it. The route now uses the actual
MapNextButton. Nativev35 then completed three Classic maps but timed out at rooftop
ready. V36 added target-scene ready/HUD diagnostics and passed an isolated roof run
in both modes, yet the complete route failed on its eighth map.

V36's failed SaBubong-ready-boundary.png shows the setup screen still visible over
an empty preview. State: active sceneSaBubong, HUDfalse, gatefalse, scale1,
RoundActivetrue. Do not erase this with the isolated roof pass.

The new deterministic test calls SceneFlow.StartMatch while an actual additive
preview load holds MatchInstaller.PreviewOnly. Before-fix it failed1/1 in20.429s
because that flag remained true (guard3a1678661441). MapPreviewSurface's coroutine
was cancelled when its setup scene was replaced, leaving the real installer disabled.

Fix: MapPreviewSurface.DeferTransition drains ongoing preview work before a scene
change, blocks new swaps during exit, honours the latest destination, and owns/
releases the preview flag through finally and OnDestroy. SceneFlow.Go calls it
before LoadScene. First unchanged regression passed1/1 in6.853s, guard2cdd64da0ed7.
Files: Runtime/UI/MapPreviewSurface.cs, Runtime/UI/SceneFlow.cs and new
Tests/PlayMode/MapPreviewTransitionTests.cs. Native qualification is still required.

## Current implemented map and arm batch

- Sixteen distinct opt-in construction finishes in EnvironmentSurface.cginc and
  NearFade.shader, including preserved normal maps. Palette mesh roles separate
  wall/roof/trim/glass/door/step and reviewed bridge/track/pole parts. Original
  geometry, colors, UV0/UV1 remain. Metre coordinates in UV3 survive static batching.
  Shader target3.5 is necessary; previous parser/interpolator failures are retained.
- MapSurfaceAuthor v3 assigns689/257/870/179 renderers in E/B/I/Sa. All-map owner
  capturev3 passed1/1,70.310s,200frames. Surface repeatabilityv2 passed with zero
  protected changes and zero repeat differences across27508 normalized rows,
  guard6d710041d5f7. NearFade checks passed15/15,0.564s.
- Four verified CC0 Poly Haven2K pure-sky HDRs, provenance in
  ArtSource/environment/skies/polyhaven. Cloud shapes use map colors, fixed sun/
  horizon and slow per-map drift. NeighbourhoodSkyMotion uses scaled Time.time;
  RecordedWorldView temporarily samples existing clip timestamps. No new wire bytes.
  Pause/reverse/error restoration and actual rendered-pixel drift/rewind checks pass.
- Inday's character-female-a and team-inday GLBs now have brown arms and retained
  simple hands. Raw backups: ArtSource/inday/before-plain-arms-20260922.
  tools/inday_plain_arms.py verifies non-arm data, nodes/skins/materials and all33/32
  animation samples unchanged. See inday-plain-arms.json for hashes. V1 pinched wrist
  was corrected in V2 by extending the forearm under the hand centre. Three arm
  audits passed1.253s; quick/held/moving body+FPP review passed1/1,17.931s.
  FppGuardAuthor and guard assets are removed. Both FppDetails and RosterArms updated.
  Inspected Editor/native frames show continuous brown arms without attachments.
- Current generated asset closure has been recovered to development:1971 files,
  73,284,138bytes, exact paths/hashes in surface-assets.json. Only referenced final
  assets were recovered; orphan prototype assets remain in validation. This includes
  current4scenes,409meshes,566materials,4HDRs and988metadata files.

## Preservation and publishing

Original26 dirty files are backed up in Logs/presentation-pass-2026-09-21/intake.
Eighteen remain byte-identical. Six shared files retain original work; two previously
unchanged Inday arm files are now deliberately changed by the latest owner request.
Do not stage unrelated UI tests, PNG metas or the original untracked PaintedScreens.

Logs/environment-owned-paths.txt lists2059 owned paths from before the preview fix;
regenerate it to include that fix, its new test/meta and new receipts. Stage shared
OwnerUiPlayerReview.cs and tools/run_ui_player_review.py using HEAD plus ONLY the
new map-review hook/argument, not whole working files. Their original SeanVisualOnly
changes depend on a separate original dirty file. Accidentally copying those into
validation caused repeatabilityv1's compile failure, which remains preserved.
Validation's OwnerUiPlayerReview.cs intentionally contains HEAD plus only our map hook.

Use explicit staging, git commit -F, sole author, no trailers, no force/reset/clean.
Commit/push stable verified batches to ASTRAReworks only. Preserve all old failures.

## Remaining full scope

Continue material-family coverage and geometry-generator hooks, remaining existing
kit/movement/equipment work, actual UI routes and stale qualification fixtures,
network/rejoin qualification, then selected RafiB / sheltered-lagoonC expansion and
final coherent delivery. Rafi/lagoon implementation has NOT begun. Do not disguise
unimplemented work as a human-review blocker. Physical devices, unavailable audio
listening, human taste and separate-machine checks remain specifically unverified.

Detailed design: MAP_FINAL_PASS.md, NATIONALS_POLISH.md and BADJAO_EXPANSION.md.
Full backlog report: reports/full-backlog-2026-09-21/README.md; disposition index
preserves366 historical numbered headings. Prior presentation software,59 movies,
real3-peer checks and default8-round matches remain preserved in the presentation
report. Prior skyline/rebind/dead-code batch was published79bf5c26.

Earlier chronology and exact prototype failures are archived in
reports/full-backlog-2026-09-21/ledger-through-preview-race-20260922.md and
material-prototype-ledger-20260922.md. Resume this current action, not their old jobs.
