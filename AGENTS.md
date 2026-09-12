# Working instructions for ChatGPT / Codex

This is the primary, self-contained repository instruction file, maintained at
owner request on 2026-09-12. Start here. `CLAUDE.md` retains historical reasoning
and incident receipts; it is no longer the entry point or a competing rulebook.
Explicit current owner instructions override repository guidance. Newer decisions
override dated scope notes, not unrelated safety, data or gameplay contracts.

## Read and resume

1. Read this file, then `docs/VISION.md`, then `docs/TODO.md` (implementation index
   and complete numbered entries relevant to the work, especially 152/152.4).
2. Read the **newest execution pointer** in `docs/ACTIVE_REWORK_LEDGER.md`. Older
   process IDs, plans and rejected studies are history, never live instructions.
   `docs/EXECUTION_PLAN.md` is the owner's requested compact action plan, current
   dirty-work/test state and next steps; read it alongside that pointer.
3. For post-map movement/throw/Pektus/equipment scope use `docs/PLAY_FEEL_REWORK_PLAN.md`.
   For the current map revision read `docs/MAP_TRANSFORMATION_PLAN.md` first. The
   owner rejected V9 as barely changed and insufficiently Filipino: plain houses,
   empty Bayan paving/no wider town, implausible Ilalim pisonets, bad signs and
   oversized crossing bands. Substantial architecture/material/spatial changes
   take priority; technical passes are not visual acceptance. Also read
   `docs/MAP_FINAL_PASS.md` and
   `docs/IMPROVEMENT_PLAN.md` (P1-P10). Ability work also requires
   `docs/ABILITY_REWORK_PLAN.md`, `docs/HERO_KIT_REWORK_DECISIONS.md` and
   `docs/PHILIPPINE_ABILITY_DIRECTION.md`.
4. Use `docs/Art_Direction.md` sections 0/0.1 for all models and motion, `LORE.md`
   for the sporting world, `ASTRA.md` for rigs/actions, and relevant `Attention.md`
   entries for human judgments. `docs/README.md` indexes other references.
5. Consult the relevant `CLAUDE.md` incident only when its historical mechanism
   matters. Do not reread its entire archive or revive its superseded commands.

## Workspace and ownership

- Live game: `DOST-GameDEV/TumbangPreso-Unity`. The Godot repository is read-only
  historical reference. Never overwrite this game's source with its older files.
- Work and push on **ASTRAReworks only**. Fetch first; inspect actual branch,
  local/remote HEAD and dirty files. Preserve newer work. Never reset to a supplied
  checkpoint, switch to/merge into/push main, or touch another checkout.
- Authorized checkout:
  `C:\Users\Matthew\Documents\Codex\2026-09-09\ok-x20\work\TumbangPreso-Unity`.
  The separate `Documents\GitHub` checkout is outside this task.
- No subagents, other-task delegation, usage resets or paid work without explicit
  approval. Parallelize independent reads/preparation, not agent work.
- Controller support has a separate owner: `GenericPadBridge`, CONTROLLER MAP,
  `MenuNav` and device mapping. Read TODO 142 before entering that area; do not
  independently implement/refactor it. Preserve a failure receipt and work around
  test-process contamination with a fresh Editor rather than changing ownership.
- Preserve profiles, saves, identifiers, source assets and unrelated changes.
  Windows is the delivery target; Android qualification is deferred, its existing
  input/data contracts still apply.

## Product and creative authority

- Improve the existing game substantially. Four players, one rotating defender,
  the can, throwing and risky slipper retrieval remain its foundation.
- The owner wants a fully realized game that surprises people through strong
  presentation and feel while keeping this style. Assess complete normal-speed
  play sequences and coherent places; technical passes alone do not meet that bar.
- **Latest map/graphics authority, 2026-09-12:** the owner permits removing or
  adding anything in the maps to thoroughly improve them. Legacy placement is not
  protected merely because it exists. Architecture and props must make spatial
  sense, with natural Filipino character. Thorough graphics and graphics-settings
  improvements are also required: lower-spec play and visible higher-end benefits.
  Measure frame cost and image/readability changes; retain gameplay tells on every
  preset. Functional graphics UI is in scope, not a general UI-art remake.
  Preserve each place's intention: Eskinita's neighborhood street, Bayan's civic
  plaza and Ilalim's guideway/shop setting. Freedom to rebuild does not mean
  replacing their Filipino identity with an unrelated setting.
- **Fourth map approved with changes, 2026-09-12: Sa Bubong.** The owner accepted
  the Metro Manila condo roofdeck concept after initially selecting 'Revise', and
  requested a Filipino name. Keep an open court, fenced pool, shaded residents'
  area and separate stairwell/utility/laundry corner. A clearly visible outer ledge
  permits actual falls. **Players can button-mash to get back up. Fallen slippers
  remain unavailable for about10 seconds before teleporting to a safe rooftop spot;
  that delay is an intentional penalty.** Preserve both modes and the core game.
  Also verify button mashing across ALL recovery contexts and keyboard/controller/
  touch/network paths. The latest plain-text acceptance supersedes the earlier
  pending concept-approval state; no further confirmation is required to implement.
- **Both Classic and Hero Strike ship as first-class modes.** Classic has no
  powers and four rounds; Hero Strike has six heroes and eight rounds. Do not
  treat one as the real game and neglect the other.
- After the maps, the owner explicitly requests a movement/play-feel and equipment
  pass: convincing physics/animation, visible windup lean shared with observers,
  evaluated small aiming shake/error, and meaningful slipper AND can attributes
  with reasons/tradeoffs beyond appearance. Plan from actual mechanics; preserve
  IDs, both modes, simple controls and the approved blocky people. Do not equate
  skill with arbitrary random spread. EXECUTION_PLAN.md carries full requirements.
- The owner permits improving/replacing boring abilities within existing slots.
  Plan each whole kit and all existing alternatives: purpose, role-specific use,
  cost, tradeoff, counterplay, tell, interruption, authority and truthful copy.
- **2026-09-12 addition:** useful additions are welcome, including more animation
  and feedback, as long as they do not overcomplicate the game. Prefer a concrete
  player benefit to accumulating controls, systems, meters or compulsory text.
- Keep all eighteen approved people/outfits at the retained `7c7fcb5` direction.
  Do not restart redesign. Preserve cute blocky shapes, flat graphic faces, simple
  hands and recognizable identities. Rounded bodies, added thumbs, realistic
  anatomy and noisy reconstructed/voxel studies were rejected.
- All new models, trees, props, summons and effect geometry must look native to
  TUMP. Use clean chunky forms and purposeful detail. Do not decimate, compress,
  recolor or collapse sourced art/materials for unmeasured performance concerns.
- Animation means the **whole action**: body/FPP preparation, moving skill
  geometry/VFX, release, impact, interruption and recovery. Different actions need
  appropriate distinct sequences. Reuse only for a justified shared action or
  contract; record why. Convincing weight does not imply realistic anatomy.
- Philippine identity comes naturally through place, material, movement, lore and
  sound. Keep English player-facing copy and proper names. No forced symbols,
  copied sacred writing or invented historical claims. This is extraordinary
  sport, without killing, guns or lasting destruction.
- UI art is low priority. Fix documented navigation, focus, raycast, loading and
  readability defects. Keep the character maker inaccessible while retaining its
  implementation, assets and saved data. Do not open a new UI decoration project.
- Can, chalk, loose slippers, defender and useful routes must remain readable
  during overlapping effects and Low graphics. More brightness/area is not polish.

## Runtime contracts

- `Packages/com.tumbangpreso.core` stays engine-free. `Core` compiles those same
  sources; do not create a second copy or introduce UnityEngine references.
- Host distance checks resolve contact; `MatchDirector.AddScore` awards every
  point. Derive defender rotation; preserve square confinement and deduplication.
- Bots submit `InputIntent` through the same motor as people. Animation reads
  observed motion/accepted actions; it must not invent a hit, pickup or score.
- Overlapping stuns use Max, not addition. Derive impulse speed from friction and
  intended travel. Balance/copy disagreements need a documented resolution.
- People use FPP, props TPP; retain emote and spectator paths. Emotes end through
  interruption, not a new timer. Preserve proper owner/observer visibility.
- Keep Generic rigs, bone paths, exact action names and stable GUIDs. After GLB
  edits rebuild the Roster Book and matching FPP assets, then verify that actual
  serialized clips resolve in a player. A file existing is not runtime coverage.
- Preserve keyboard/mouse, controller and touch for any changed feature. Use the
  live binding for prompts. New verbs require InputCatalogue, pad and thumb paths;
  regenerate InputAssetSync. Menus use MenuKit/ConvertedScreen and MenuNav to exit.
- Input never changes the wire protocol. Read the current protocol from source;
  legitimate wire changes need compatibility tests and explicit platform limits.

## Evidence before edits

Trace the actual producer, caller, state owner and cleanup path. Reproduce a bug
before choosing a correction. Isolate competing input/AI, carry, effect and network
writers. Change one causal variable at a time; preserve unsuccessful experiments.
A direct helper call, same-process witness or stale build proves only that path.

Use controlled failures and focused regressions, actual ordinary-speed owner and
observer views, and real separate processes for networking. Seeded bot counts are
liveness evidence with measured run-to-run noise, not an n=1 balance comparison.
Do not retune slide/lunge numbers from bot ratios without the corresponding play
question. The historical Ilalim 48-idle outlier is not explained by another flight
bug unless a trace connects them. Preserve raised-ground and roof recovery.

Capture versioned in-engine images/motion with timestamps and matched settings.
Model changes need multiple angles and a cast comparison. A 1.65 m witness camera
is not the real FPP rig; stills are not a complete action or human feel approval.
Use source authors for maps/assets and verify a semantic two-run comparison;
random Unity object IDs alone are not a geometry change.

## Criticize each batch before accepting it

Owner requirement: thoroughly examine whether every finished visual/play-feel
batch looks and feels right. Record weaknesses and revisions, not just strengths.
Judge style fit, Filipino place identity, spatial logic, human scale/materials,
composition/world depth, gameplay readability/physical routes and measured cost.
Use actual player views, awkward side/back angles and ordinary-speed motion.
Tests/imports/isolated renders do not establish artistic success. Revise obvious
failures proactively. MAP_TRANSFORMATION_PLAN.md carries the full critique rubric.

## Unity, tests and builds

- Unity is 6000.5.8f1. Official Pipeline is configured; CLI is
  `C:\Users\Matthew\AppData\Local\Unity\bin\unity.exe`.
  Blender is `C:\Program Files\Blender Foundation\Blender 5.2\blender.exe`.
  Check actual installation/process state before using copied paths or IDs.
- **Every Unity launch uses `python tools/run_unity_guarded.py ...`.** It backs up,
  restores and hash-verifies the Windows profile. Never delete/reset the profile.
- One Editor per checkout. Inspect active processes and lock/log state first.
  Start long runs in the background; read, review or update docs while they run.
  **Do not edit C# or imported assets during a Unity run.** Do not terminate an
  unrelated process. Diagnose a stale lock only after confirming its owner is gone.
- Core: `dotnet test Core.Tests/TumbangPreso.Core.Tests.csproj`.
  Fresh EditMode: guarded `-batchmode -runTests -testPlatform EditMode`, explicit
  `-buildTarget Win64`, unique XML/log paths. No bare `-batchmode -quit` compile claim.
- PlayMode: no `-nographics`. Require **fresh nonzero NUnit XML**, zero failures
  and expected fixture coverage, never just exit code or result='Passed'. Single
  fixture CLI filters worked; a semicolon-combined CLI filter produced zero tests.
- The PlayMode gate is `python tools/playmode_suite.py --gate`; its discovered
  partition must cover every fixture exactly once. Run `--gate --twice` for a
  release candidate. A monolithic process is not a substitute. WallClock and
  ThumbFloor exclusions do not constitute coverage; run relevant probes explicitly.
- Run `Checks.RunAll` through the guard and all gating `tools/audit_*.py` source
  checks via the qualification script. Use `PYTHONIOENCODING=utf-8` as needed.
  `tools/qualify.py` records Core/EditMode/check/source stages. Inspect its actual
  stage options. Scene text and loaded-reference checks detect different defects.
- Preserve failed logs and classify new, inherited, environment and unverified
  failures. Do not weaken tests, hide skips or modify production just to turn green.
- When the selected work is ready, run guarded `GameBuilder.BuildWindows`. Its
  validated purge removes the previous player first. Check for a running player;
  never manually delete a computed directory without verifying the absolute path.
- Verify executable AND data timestamps, launch that exact build, and exercise
  both modes at ordinary speed plus relevant network paths. Older Desktop/internal
  executables do not represent newer source. Report the actual artifact path.

## Keep work safe across compaction

Update the **newest execution pointer** in `docs/ACTIVE_REWORK_LEDGER.md` whenever
an important decision, experiment, code batch or run changes state. Include:
actual checkout/branch/HEAD, dirty files, accepted constraints, rejected approaches,
observed versus inferred cause, exact command/log/XML/capture paths, active process
or session ownership, profile snapshot and **next concrete step**. Retire completed
run IDs immediately. Compaction is continuation, not a fresh start or a stop.

Keep TODO and the improvement plan current in the same commits as work. Move
completed numbered entries and session reports whole to `TODO_Archive.md`, keep
numbers and index pointers, and never delete the reasoning. Do not claim the larger
goal complete from one verified batch. Latest owner permission allows unfinished
maps in a handoff; disclose unfinished criteria and do not call them done.

## Commit and delivery

Push substantial stable verified batches automatically to origin ASTRAReworks.
Commit with a message file (`git commit -F`), sole author, no coauthor trailers or
tooling attribution in commit messages/code. No em dashes in new repository text.
This owner-requested instruction file may name ChatGPT/Codex as its intended reader.
Inspect the final diff; restore only proven test-generated changes to their exact
pre-run values, never unrelated work. Do not stop to ask whether to continue
already-authorized work. Give concise useful progress during execution, and report
completion honestly with tests, artifacts and limitations. Any further handoff
belongs directly in chat, never in a committed handoff-prompt file.
