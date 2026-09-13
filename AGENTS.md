# Working instructions for ChatGPT / Codex

**No usage-reset permission:** the owner manually redeemed a reset and explicitly
revoked all assistant reset authorization on2026-09-14. Do not redeem any credit.
Previous conditional permission is cancelled, not waiting for a threshold. Do not
restore it after compaction. The private Logs state records revocation.

**No subagents:** the owner explicitly ended all delegation on2026-09-14. Do
not spawn, fork, resume, or assign work to any agent, including the former UI
agent. It stopped at the usage limit before finishing. Parent does the remainder.
The previous one-agent exception is revoked. Preserve its existing work/source.

**Latest priority:** the former UI agent's remaining TODO is now LAST in the
parent's queue, after the other requested game work. This supersedes older UI-
before-animation ordering for unfinished UI. See docs/UI_REMAINING_TODO.md for
exact remaining screens, known failure and unintegrated commit. Parent continues
its own gameplay/throwing/animation/skills work without watching or restarting UI.

**Efficient quality work:** prioritize visual/gameplay quality, parallelize
independent tools/preparation, and do useful work while tests/builds run. Run only
related focused checks, never routine full EditMode/PlayMode suites. For rendering,
capture matched ORIGINAL player imagery and real counters before changing the
path. Separate variables and batch diagnostics; reject ugly or unproven gains.

**Newest bird scope clarification:** the owner permits birds to pass through
scenery during flight. Prioritize appealing motion and reliably flying away when
a player approaches. Do not spend time perfecting bird collision avoidance or
block the map pass on it. This exception does not excuse floating/perched animals
or ground cats/dogs walking through the scenery.

**Newest verification instruction,2026-09-13:** use only tests/checks related to
the current changes. Do not routinely run full EditMode or full PlayMode suites.
The owner explicitly requested this to improve throughput without lowering
quality. Batch coherent implementation, use focused behavioral regressions and
targeted ordinary-speed visual review, and keep evidence/limitations truthful.
This supersedes older blanket full-suite and repeated-suite mandates below and
in historical documents. Do not automatically restore them for a checkpoint.

**Current delivery instruction:** continue the full work in this chat for now.
The owner plans to move to another PC later and will explicitly ask for a handoff.
WHEN asked: finish/save current runs, COMMIT AND PUSH to ASTRAReworks first, then
provide the precise continuation prompt with exact commit, remaining work, source
PDF/logo paths and current conditional reset state. Do not create a new task or
handoff prematurely. Finish remaining game work, then the deferred UI TODO last. No Desktop rebuild now. See the newest active-ledger pointer and compact
EXECUTION_PLAN for actual implementation and validation state. Older process
records and contradictory historical requests never replace that pointer.

## Newest rooftop and throwing requirements, 2026-09-13

**Newest swimming direction:** the owner wants a horizontal breaststroke-style
body/arm sweep and kick ONLY when choosing to move forward, referencing Minecraft
and the supplied swimming photo. Stopping, strafing and backward movement use
upright treading/paddling. Match body and FPP, visible to observers, with smooth
transitions and held-slipper handling. Do not apply the horizontal swim merely
because there is any planar speed. Current upright screenshot was treading;
forward-only breaststroke revision now takes priority before final pool signoff.

The owner explicitly rejects the small closed/scenic pool. Enlarge it, REMOVE
the pool fence, make it accessible, and implement actual swimming with a swimming
animation. Swimming/body/FPP/observer support belongs to this MAP pass, not the
later whole-roster animation pass. Pool geometry must have a real basin/opening,
entry/exit and water interaction; standing on a solid blue block is not swimming.
Pool slipper handling must be reconsidered because it is now accessible; the
10second penalty is required for slippers falling OFF the roof, not automatically
for all water contacts. Preserve retrievability and both modes.

Fence ALL outer roof edges; retire the isolated east opening/paint stripe. Actual
falls must be possible over any outer railing with existing controls. Keep mash
get-up and the10second off-roof slipper penalty. Add reasonable pots/resident
objects and several types of cosmetic birds that arrive/perch/peck/fly away.
Avoid new controls/objectives and keep all shared gameplay random streams untouched.

Throwing after maps+UI: movement worsens accuracy; standing still improves it;
immediate releases are shakier/less accurate; holding longer settles and improves
aim, with a small residual shake. The owner compares aiming behavior to PUBG sniper
settling. Author visible body/FPP aiming/release to match actual authoritative
trajectory, preserve Pektus signed arm action, and avoid invisible arbitrary misses.
The order remains MAPS -> FULL UI -> ANIMATIONS/SKILLS/PLAY FEEL.

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

**Latest ability direction:** after current maps/UI work, audit every ability but
preserve ones that are already polished. Improve or rebuild weak mechanics and
presentation; the owner explicitly permits changing what skills do when it makes
the game better. Image generation may help concepts, but critically reject ugly
outputs instead of treating them as final direction. Ultimates must retain their
individual special moments inspired by Tekken/Genshin. No automatic wholesale
replacement of good work. Apply the same deliberate critique to all future art.

### Newest owner UI direction, 2026-09-13

**Explicit controller exception:** the owner says the original controller screen
already looked good and specifically wants its controller artwork, button
callouts/buttons and connecting lines preserved/restored. Leave that presentation
alone. This overrides the no-old-builders requirement for the approved controller
screen only. It does not cancel the rest of the overhaul or change mapping logic.

The owner specifically warns that the previous universal UI generator made
everything look similar and ugly. Do not reproduce that with one new universal
button/panel template. Use distinct compositions and component families for
menus, portraits, settings, tabs, dialogs and HUD while sharing the brand system.
Use image generation for stronger design direction or original loading/menu
background artwork, as explicitly requested; keep logo and native UI editable.

**Latest explicit implementation constraint:** build genuinely NEW UI builders,
screen layouts and native editable components. Do not reuse/reskin the old UI
builders or mutate their old visual hierarchies as the overhaul. Preserve their
source inactive and preserve functionality/data through nonvisual adapters to
the existing gameplay/settings/network services. The owner rejected the first
conservative reskin. Use actual character portraits/equipment thumbnails and
recognizable icons for suitable controls; avoid text carrying every choice.
Anchor the entire visual language to the supplied logo and full palette sheet:
deep red, orange, peach/cream, yellow, yellow-green, olive and dark olive, with
hand-drawn contours and restrained original pattern/impact/slipper motifs.
Palette/shape source images are in the UI agent's owner-brand source folder.
The supplied brand palette supersedes older UI colour prohibitions; gameplay
role readability must still be explicit. Uniform boxes/legacy brown-amber
buttons plus a new header do not satisfy this overhaul.

**Future ownership, explicitly long-term:** the owner's girlfriend will eventually
replace this UI with her own, but not soon. This does NOT reduce the current
quality/completeness requirement. Build a polished genuinely good interface now.
Keep text, shapes, layouts, theme and source art easy to edit, with clear
view/domain boundaries and authoring documentation for her future replacement.
Continue the UI overhaul. Relevant additional skills may be installed as needed;
existing no-paid-work/no-additional-agent limits remain.

**Newest typography clarification:** Darumadrop (Darumadrop One) remains the main
font, including scoreboard, settings and primary controls, not only titles.
The owner liked it and rejected how it looked when tiny. Use adequate size and
spacing; reserve a supporting font for genuinely small/dense secondary details.
Do not replace the main Darumadrop identity with a neutral font.

The owner now authorizes a FULL UI/UX overhaul, before animations and skills.
This supersedes earlier UI-art-low-priority and functional-only limits. Preserve
unfinished map work and the broader game scope. Use the supplied girlfriend's
new TUMP logo and supporting marks as the brand anchor. Review the49-page
TUMP (1).pdf moodboard; pages39-49 are composition ideas, not final layouts or
instructions. Font choices are flexible. Research game UI/UX and write a durable
screen-by-screen plan before implementation. Existing game-ui-design/Unity UI/PDF
skills are installed; install more only if materially needed, no paid work.

Target a unified, quirky, intuitive interface with low information overload.
Main menu: game name, a few clear choices and a simple animated background that
introduces the game. Slay the Spire is a restraint/composition reference, not
art to copy. Do not paste one button treatment across every control. Keep coherent
type/colour/spacing/focus rules while differentiating actions, tabs, cards, settings
rows and transient HUD information. Preserve working navigation/input/save/net
contracts and keep the character maker inaccessible. The new logo's own colours
are approved. Critique actual task flows and every screen, not just a theme mockup.
Source files and hashes: ArtSource/ui/owner-brand-2026-09-13/source-manifest.json.


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
  preset. Full UI overhaul is separately authorized below the map priority.
  Preserve each place's intention: Eskinita's neighborhood street, Bayan's civic
  plaza and Ilalim's guideway/shop setting. Freedom to rebuild does not mean
  replacing their Filipino identity with an unrelated setting.
- **Fourth map approved with changes, 2026-09-12: Sa Bubong.** The owner accepted
  the Metro Manila condo roofdeck concept after initially selecting 'Revise', and
  requested a Filipino name. Keep an open court, enlarged accessible swimming pool, shaded residents'
  area and separate stairwell/utility/laundry corner. All outer edges have rails;
  existing jump/movement permits actual falls over any rail. No pool fence. **Players can button-mash to get back up. Fallen slippers
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
- **Latest owner clarification,2026-09-13:** after initially rejecting the current
  Bayan view,the owner said it is good and asked for modest further improvement
  and an aerial view before choosing composition changes. Keep the current layout;
  do not execute the earlier rollback proposal. Their specific current complaint
  is uniform trees across all maps. Inventory existing tree assets,then vary useful
  species/silhouettes/canopy shape/age/spacing while keeping the native style and
  clear routes. The framed Bawal plaque remains rejected;replace it with natural
  surface-painted lettering. The requested Desktop review was delivered; its replacement is now deferred.
- **Latest owner playtest,2026-09-13:** delete Street Hype as a mechanic and all
  associated UI. Earlier Vision/archive claims that it is Classic's identity are
  superseded. Remove the duplicate main lobby RULES row, preserving Custom Game.
  Fix Ilalim floating attachments/road continuity; redesign Eskinita sampayan and
  retain its comparatively tight alley feeling; improve Bayan surface texture.
  Warm cream/terracotta/brown-grey palette is now requested over the neutral pass.
  Sa Bubong implementation moves ahead of the remaining abilities/equipment queue.
  After maps and the full UI overhaul, continue ability implementation/mechanics
  and visual revamps, including distinct signature ultimate moments. All six kits
  and alternatives are covered; movement/equipment/network work remains open.
  The owner is playing the delivered Desktop build: never replace it while running
  or restore broad profile snapshots over their new saves. Use a separate named
  Editor profile. Full current contract is docs/OWNER_PLAYTEST_REVISION.md.
- House-model clarification,2026-09-12: the owner rejects the flimsy V1-V5 native
  house studies. Keep the old houses' solid chunky silhouettes/proportions and
  substantial walls/roof edges/frames/supports. Build Filipino details into that
  mass. Compare new work beside a retained old house and the cast; do not revive
  thin panels, wafer roofs or spindly supports as accepted direction.
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
- Full UI/UX overhaul is authorized after maps, using the supplied logo and
  moodboard under the newest UI direction above. Keep the character maker
  inaccessible while retaining its implementation, assets and saved data.
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
- Core: `dotnet test Core.Tests/TumbangPreso.Core.Tests.csproj` when relevant.
  Focused EditMode: guarded `-batchmode -runTests -testPlatform EditMode` with an
  explicit related fixture filter, `-buildTarget Win64`, unique XML/log paths.
  No bare `-batchmode -quit` compile claim and no routine full-suite run.
- PlayMode: no `-nographics`. Require **fresh nonzero NUnit XML**, zero failures
  and expected fixture coverage, never just exit code or result='Passed'. Single
  fixture CLI filters worked; a semicolon-combined CLI filter produced zero tests.
- Run the specific PlayMode fixtures affected by the change. Existing full-suite
  tooling remains available as historical infrastructure, but the newest owner
  instruction supersedes automatically running it. Label uncovered areas honestly.
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
goal complete from one verified batch. Do not stop at a handoff: the owner
cancelled it and asked to continue here. Disclose unfinished criteria honestly.

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

Historical wording before this clarification is preserved whole in
`docs/reports/improvement-2026-09-13/agents-history-before-swimming.md`.
