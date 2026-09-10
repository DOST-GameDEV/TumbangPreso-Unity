# Working in this repository

**Latest delivery scope,2026-09-10:** the owner now asks to wrap up the current
work, commit/push and hand off. They explicitly allow unfinished map work to be
included in the continuation handoff. Verify the current batch and record its
limits; do not claim the full map pass is complete or start further scope.
Read [docs/MAP_FINAL_PASS.md](docs/MAP_FINAL_PASS.md) and the current execution
pointer in ACTIVE_REWORK_LEDGER.md. Architecture, ground/props, materials/light,
collision/retrieval and ordinary-play verification all remain part of the map
goal for continuation. Preserve the
ongoing Kuro/ability/network work and its exact verification state.

## Standing model-style requirement

**New models must look native to TUMP, not overly detailed or from another game.**
Read [docs/Art_Direction.md section0](docs/Art_Direction.md#0--new-models-must-belong-to-tump)
before model/asset work. Keep the existing cute blocky language: few clean chunky
forms, broad planes, restrained materials and purposeful detail. This covers pets,
skills/VFX models, FPP arms, props, trees and maps as well as characters. Avoid
realistic anatomy and noisy reconstruction/voxel results. Judge the actual Unity
asset beside the existing cast. Realistic animation means better weight and
timing within the style, not realistic-looking models. Current owner corrections
and rejected drafts in ACTIVE_REWORK_LEDGER.md must survive compaction.

**Animation means the whole action:** improve casting, body/FPP motion, skill
VFX/geometry motion, release, impact and recovery. Give different actions/skills
appropriate separate animations; do not repeat a generic cast with new colors.
Reuse only when necessary for the same action or an existing shared contract,
and record why. See Art_Direction.md section0.1.

## Start here

Read [CLAUDE.md](CLAUDE.md) first, [docs/VISION.md](docs/VISION.md) second,
then [docs/TODO.md](docs/TODO.md). Read the implementation queue and the full
numbered entries relevant to the work. Do not treat a handoff as a substitute.

The current improvement brief is [docs/IMPROVEMENT_PLAN.md](docs/IMPROVEMENT_PLAN.md),
tracked in TODO section 152. It explicitly supersedes historical single-task,
animation-only and stop-after-one-hero restrictions in ASTRA.md. Current user
instructions take precedence over repository guidance. The rules below make the
active scope explicit so an older reference cannot silently undo it.

Read [docs/ACTIVE_REWORK_LEDGER.md](docs/ACTIVE_REWORK_LEDGER.md) for the latest
owner decisions, rejected first-pass model result, individual character/skill
queues and exact in-flight state. The owner explicitly requested this record to
survive compactions. Keep it current; do not restart or narrow the task after one.

For current skill work read [docs/ABILITY_REWORK_PLAN.md](docs/ABILITY_REWORK_PLAN.md).
It preserves the all-eighteen geometry/animation/SFX/VFX audit, current owner feedback,
action-specific direction and completion checks. Start with the rejected ice skill;
keep the current character designs and finish the remaining overall game scope.
The detailed default/alternate decision matrix is
[docs/HERO_KIT_REWORK_DECISIONS.md](docs/HERO_KIT_REWORK_DECISIONS.md); it records
all twelve choice pairs and candidate replacements without claiming they are done.
Also read [docs/PHILIPPINE_ABILITY_DIRECTION.md](docs/PHILIPPINE_ABILITY_DIRECTION.md):
the latest owner request covers culturally grounded reworks for all six heroes,
English copy, research/planning before implementation, and autonomous AFK execution.

## Active scope and boundaries

**Latest scope expansion, 2026-09-10:** thoroughly repair networking and improve
all eighteen abilities plus every existing loadout alternative. The owner now
explicitly permits replacing boring/redundant abilities. Plan each whole hero kit
with distinct tactical purposes, useful alternatives, counterplay, role-specific
use cases and synchronized animation/FPP/VFX/SFX before implementation. This
supersedes presentation-only or blanket no-mechanic-change restrictions for those
existing ability slots. Preserve the core game, six heroes, both modes and the
existing loadout structure; do not add modes or unnecessary systems. The latest
sections of ACTIVE_REWORK_LEDGER.md and ABILITY_REWORK_PLAN.md record decisions,
open defects, exact verification state and next actions. Keep them current.


**Latest owner correction (2026-09-10): keep the cleaned current cast at `7c7fcb5`
and move to other work. Stop the individual character redesign queue. Prioritize
animation, all eighteen skills/SFX/VFX, gameplay/maps and Windows delivery. The
older model-rebuild instructions below are historical and superseded.**

- Work on `ASTRAReworks` only. Fetch first, inspect branch, status and remote HEAD,
  and preserve newer commits and uncommitted user changes. Never switch to, merge
  into or push `main`. Do not touch another checkout's unrelated changes.
- Complete the authorized improvement pass autonomously. Do not ask whether to
  continue work already requested. Collect non-blocking questions for delivery.
- Do not spawn subagents or delegate to other tasks. Independent read-only tools
  and preparation may run in parallel.
- Prioritize repeated play, all three maps, the Classic cast, six heroes, authored
  motion and existing effects. UI is functional placeholder work for a later art
  remake: fix navigation, loading, focus, raycasts and readability without making
  placeholder decoration the project.
- Preserve four players, rotating defender, can, throws and slipper retrieval,
  Classic and Hero Strike. Add no new mechanics, modes or unnecessary systems.
- Windows is the delivery target. Android qualification is deferred; preserve its
  existing input and data contracts. Never perform a usage reset. Ask before paid
  external work. Preserve profiles, saved identifiers and authored source assets.

## Locate the truth before changing it

| Question | Source |
|---|---|
| Rules and historical failure modes | CLAUDE.md |
| Product direction and readability | docs/VISION.md and LORE.md |
| Open work and completion evidence | docs/TODO.md; closed bodies in docs/TODO_Archive.md |
| Current improvement order and outcomes | docs/IMPROVEMENT_PLAN.md |
| Authored rigs, exact action names and motion evidence | ASTRA.md |
| Human taste, hardware and tournament rulings | Attention.md |
| Placeholder UI and font roles | docs/CALM_FRONT_END.md and docs/FONT_USAGE.md |
| Runtime balance | Packages/com.tumbangpreso.core; docs/Design.md explains Classic |
| Network protocol, roster and build paths | Read their current implementation, never a copied number |

Trace the actual producer, runtime path and serialized asset before editing it.
A clip existing on disk does not prove the player loads it. A showcase effect is
not necessarily the live ability. A test invoking a callback does not prove a
player can click the control. Reproduce a defect before replacing a working fix.

## Preserve these contracts

- The core package has no UnityEngine references. Core and Unity compile the same
  sources, not divergent copies.
- Host distance checks resolve contact; MatchDirector.AddScore awards points.
  Preserve square confinement, derived role rotation and authority deduplication.
- Bots use InputIntent and the same motor as people. Presentation reads observed
  movement and accepted actions so owner, observer and bot views agree.
- People use first person; props use third person. Preserve emote and spectator
  camera paths. Emotes end by interruption, not by a new completion timer.
- Keep Generic rigs, exact clip names, authored slide timing and stable asset
  GUIDs. After GLB exports rebuild the roster and verify serialized clip resolution.
- The owner rejected the first model refinement as too subtle and now requires
  individual character authoring, especially distinct Classic faces and silhouettes.
  Review each model before moving to the next; regenerate matching FPP arms after
  its design settles. Keep recognizable identities and do not homogenize the cast.
  Keep the cute blocky forms and simple block hands. The rounded Berto draft and
  added thumb shapes were explicitly rejected; do not revive that recipe.
  Keep simple flat graphic faces. Realistic noses, brows and wrinkles are not the
  requested style improvement. Motion realism concerns weight and articulation.
  Do not decimate, compress or repaint sourced art for an unmeasured performance concern.
- Keep the character maker inaccessible to players while retaining implementation,
  assets and saved data. English display copy must not rename persisted IDs.

## Work, verify, deliver

Use one Unity process per checkout. Never edit C# or imported assets while its
Unity run is active. Launch long verification in the background, then read source,
review evidence or update documents. Inspect the process and fresh log before
starting the next run. Use hidden windows for background helpers.

Choose verification for the behavior changed. Core tests are cheap. Unity test
success requires fresh XML with nonzero total, zero failures and expected fixture
coverage; process exit code alone proves nothing. Never use `-nographics` for
PlayMode and never use `-batchmode -quit` alone as a compile check. Test filters
use semicolons. The discovered `tools/playmode_suite.py --gate` partition is the
PlayMode gate; a monolithic run is not a substitute. Run the gate twice for the
release candidate, plus EditMode, Core, Checks.RunAll and the source audits.

Capture before/after evidence at matched settings and ordinary camera heights.
Inspect moving sequences at normal speed, including preparation, contact,
interruption and recovery. Model iterations need versioned in-engine turnarounds
and a cast lineup. A passing test or attractive still is not human playtest approval.

Push significant stable verified batches to `origin ASTRAReworks`. Update TODO in
the same commit. Move completed sections whole into TODO_Archive, preserving
numbers and index pointers; keep unfinished acceptance criteria open. Keep the
improvement plan current with decisions and their evidence.

Commits are sole-authored, without coauthor trailers or tooling attribution.
Write the message to a file and use `git commit -F`. No em dashes in new repository
text. Describe why a fix exists and preserve useful historical reasoning.

When the improvement work is ready, use GameBuilder.BuildWindows. Its validated
purge protects against stale output; check for a running player before building.
Verify the executable and data timestamps, launch that exact executable, and
exercise both modes. Report the pushed checkpoint, actual test/build results,
evidence and unresolved limitations. Any further handoff goes directly in chat,
never in a committed handoff file.

## Historical routing notes

Read [CLAUDE.md](CLAUDE.md), then [docs/VISION.md](docs/VISION.md), then
[docs/TODO.md](docs/TODO.md). CLAUDE.md is the canonical repository rulebook and
wins if this routing file or another repository guide conflicts with it.

For presentation and polish, consult [docs/NATIONALS_POLISH.md](docs/NATIONALS_POLISH.md),
the current strategic roadmap. For animation and Blender work, also read
[ASTRA.md](ASTRA.md). Human judgments belong in [Attention.md](Attention.md).

The roadmap sets priorities; the existing files remain the execution queues.
FUTURE.md and INSPIRATION.md remain historical/reference material, not the default
next-work order. [docs/README.md](docs/README.md) indexes the documentation.
