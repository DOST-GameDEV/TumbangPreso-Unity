# Game improvement plan

**Standing art constraint, owner 2026-09-10:** every new model must look like it
came from TUMP. Preserve the clean cute/blocky visual language; avoid overly
detailed, realistic or noisy assets. This applies across all character, pet,
ability, FPP, prop and map deliverables. See
[Art_Direction.md section0](Art_Direction.md#0--new-models-must-belong-to-tump).
Stronger animation also includes better casting and skill VFX, with distinct
sequences for different actions. Do not repeat generic animations unless reuse
is necessary. Better weight/contact/timing is one part of that broader scope;
see Art_Direction.md section0.1.

## Latest owner additions, 2026-09-10

Thorough networking repair is required. The owner also authorizes changing or
replacing boring abilities and making every existing loadout alternative justify
its slot. Whole-kit planning and acceptance are in
[HERO_KIT_REWORK_DECISIONS.md](HERO_KIT_REWORK_DECISIONS.md), with cultural direction
in [PHILIPPINE_ABILITY_DIRECTION.md](PHILIPPINE_ABILITY_DIRECTION.md). This overrides
older presentation-only or blanket no-mechanic-change restrictions for the existing
ability slots. Preserve the core game, six heroes, both modes and loadout structure.
Keep the current approved people models; do not reopen their redesign queue.
Current execution and failures are in [ACTIVE_REWORK_LEDGER.md](ACTIVE_REWORK_LEDGER.md).


Current state: CLEANED CAST KEPT BY OWNER; MODEL REDESIGNS STOPPED; ANIMATION, DEEP SKILL AND REMAINING GAMEPLAY/MAP WORK OPEN; FINAL QUALIFICATION PENDING.
Starting checkpoint: `4623348069eb0be9d9a00f429fd97c9252cf1fc3` on `ASTRAReworks`.
Execution ledger: TODO section 152. This plan is not a claim that all phases are done.

**Latest owner decisions and exact current execution state:**
[ACTIVE_REWORK_LEDGER.md](ACTIVE_REWORK_LEDGER.md). Read it after the required
CLAUDE/VISION/TODO order. The owner explicitly requested that planning survive
compactions. The rejected first model refinement is not a completed deliverable. The later
rounded Berto draft was also rejected and restored. Keep cute blocky forms, simple
flat faces and block hands; remove the added thumbs. Improve within that style.

## Authority and current delivery

Latest owner decision: the cleaned Classic cast is already solid; keep the current
models at `7c7fcb5` and move on. The individual character redesign queue is
superseded. Preserve the models/outfits and focus on remaining motion, all eighteen
skills and their effects/sound, gameplay, maps and Windows verification/delivery.

The 2026-09-10 request activates the full improvement pass, tests and Windows
delivery. The earlier test/build deferral applied to the preceding UI task only.
The user is AFK and requests autonomous completion of all actionable work, with
particular emphasis on maps, model geometry and all eighteen skill animations.
Keep UI effort limited to functional verification and genuine defects.

The next executor may change the priorities, proposed designs and implementation
choices below when current evidence supports a better result. Record the reason
and keep the plan current. Do not follow a proposal mechanically when it would
make the game worse. Preserve the user's constraints and the repository invariants.

Work only on `ASTRAReworks`. Fetch first. Never work on, merge into or push `main`.
Use the current conversation for the work, without subagents or other-task delegation.
Parallelize independent tools and asset work, but use one Unity process per checkout
and do not edit C# or imported assets while that checkout's Unity run is active.
Push every substantial verified change, using sole-authored commits, no coauthor
trailers or tooling attribution, and messages from files with `git commit -F`.
Never perform a usage reset. Ask before paid external work. Preserve user profiles.

The user expects to be asleep/AFK during the next execution. Continue autonomously
through all necessary authorized improvements and routine decisions. Collect
non-blocking questions and human taste decisions for the final reply. Do not wait
for optional approvals or stop after the first batch. Real external blockers should
be recorded while independent work continues. Do not add new gameplay mechanics.

The old stop-after-one and animation-only limits were removed by the user. The
remaining gameplay pass may edit code, models, animations, maps, collision, shaders,
SFX, VFX, descriptions and icons. Windows is the target; Android is deferred.

## Product direction

Latest map-stage feedback: the owner rejects the current trees and expects suitable
replacement assets may already be installed. Inventory existing nature/tree assets
first, then make a thorough per-map plan for vegetation, materials, architecture,
lighting and playable routes. Schedule this after the active ability pass. The
first map presentation batch is not final approval of its trees or composition.

Improve this game substantially, not a replacement game. Keep four players, the
rotating defender, the can and the risky run back for a slipper. Classic and Hero
Strike both remain first-class. Do not overload the player with new modes, heroes,
controls, meters, progression systems or complicated features.

The desired result is smoother and more satisfying movement and action, clear
contact and recovery, coherent graphics, stronger Philippine place identity and
more intentional maps and characters. Cultural character should come through
materials, architecture, clothing details, gestures and sound, rather than forced
symbols or Tagalog slogans. Player-facing copy is English; proper names retain
identity. [LORE.md](../LORE.md) connects the existing world as a major spectator sport
with the Philippines as its cultural home and a leading competitor. It need not
be explained wholesale in the game. No killing/gun narrative.

The girlfriend will remake final UI art. Keep the current UI usable and appealing
as placeholders, and preserve the flows and replaceable art slots. Do not spend
the next gameplay pass perfecting every placeholder panel.

## What a substantial improvement must look like

The request is a major improvement in the experience, not a target count of edits.
Passing the existing tests or clearing the engineering queue alone cannot satisfy
it. Deliver complete improvements to the repeated actions and ordinary match views.
Use these outcomes to decide what to do and when a batch is worth pushing:

| Player experience | Intended change | Evidence to keep |
|---|---|---|
| Moving with and without a slipper | Feet visibly travel with the body, carrying stays stable, turns and starts do not snap, fatigue remains readable while moving | Comparable uncut idle/walk/sprint/turn/carry/charge/recovery captures on owner and observer |
| Committing to a throw and running back | Preparation builds clearly into release; can contact, bounce, catch and failed catch have distinct, correctly timed responses | One complete throw-to-retrieval sequence, including a miss and interrupted recovery |
| Reading the chase | Can, loose slipper, defender and safe route remain visible without extra HUD clutter or exaggerated effects | Ordinary first-person and spectator frames during competing actions |
| Playing a hero | Body, hand pose, effect and sound express the same personality and action, then release control cleanly | All three casts per existing hero, successful, refused and interrupted |
| Recognizing a place | Each map feels inhabited and Philippine, with a strong silhouette, coherent materials and intentional quiet space | Matching eye-height views before/after on all three maps, not only showcase cameras |
| Playing on a modest Windows PC | Stable motion and clear text survive lower quality settings; the controls respond consistently across frame rates | Frame-time evidence on the available machine with hardware/settings disclosed |

First finish one representative sequence on Eskinita through animation, collision,
camera, sound and lighting. Then carry its quality through the cast and other maps.
This is a reference slice inside the existing game, not a new prototype or a reason
to stop after one character. Do not claim a literal tenfold improvement; explain
what became better and provide evidence a human can judge at normal speed.

Before implementation, save comparable baseline captures and name the three most
noticeable problems in each sequence. Afterward revisit those same situations at
the same settings, including low quality. A change that is prettier in a still but
harder to read in motion needs revision. Preserve worthwhile assets rather than
adding decoration simply to make the diff larger.

## Read order and references

1. Repository rulebook, then VISION, then TODO. Existing architectural invariants
   and authority rules remain binding.
2. NATIONALS_POLISH for the perceptual goals. Its old one-task and blanket UI
   restrictions are superseded by this request; historical missing-feature claims
   must be checked against current code.
3. ASTRA for authored rigs, cast grammar, slide evidence and unresolved ultimate
   presentation hooks. Attention owns actual human judgments.
4. CALM_FRONT_END and FONT_USAGE for the current placeholder UI and typography.
5. `docs/refs/ui/TUMP.pdf`: pages 1-7 and especially 36-40; 20-26 are examples.
   Original logo/wordmark/slipper JPGs and supplied font ZIPs are beside it.

Darumadrop owns display headings, Kawit Extended short accents/actions, and Lydian
reading text. The owner confirmed Lydian embedding/distribution permission. Its
signed descent was corrected without changing glyph outlines. Fonts are dynamic;
do not return to a small static atlas. Back/Close use icons. Keep words for
meaningful choices and explanations. Both rejected backgrounds, native game footage
and a detailed realistic porch, are outside the shipping resource set.

## Status and order

| Phase | State | Outcome |
|---|---|---|
| V0 | in progress | Fresh import compiled; home/story real-raycast journeys 3/3; inherited screen sweep remains |
| P1 | in progress | Frozen carry legs measured at 3.6053 m/s; masked gait, live charge, blend/rebind continuity and consistent FPP recovery implemented; regression/capture review underway |
| P2 | partly complete | Close concrete correctness and release-integrity findings |
| P3 | implemented, verification pending | Home, mode selection, icons, fonts, graphics settings, loading and dormant maker |
| P4 | in progress | Hemisphere lighting, camera depth precision, quieter cast/weather lights, distinct scrape and grounded rubber footfalls implemented; final play comparison pending |
| P5 | in progress | Sean's three body/FPP actions retimed, including descent/recovery; authored hands and sleeves now shared between views |
| P6 | in progress | All eighteen body actions reauthored and two-hand FPP curves implemented; actual baseline press/release coverage is 18/18 |
| P7 | in progress | Reauthored civic/storefront geometry, balcony/louver details, viaduct structure and broad shade canopies; final geometry/render review and idle investigation pending |
| P8 | in verification | Connect round starts, interruptions, final action and match ending |
| P9 | in progress | Resolve remaining actionable findings and documentation drift |
| P10 | pending implementation | Run release checks, build Windows and verify the exact player in this task |

### Active evidence and decisions, 2026-09-10

- Fetch confirmed clean local and remote `4623348`; no newer work was overwritten.
- `Logs/improvement-baseline-v1` contains timestamped 1x carry/charge/release
  frames, ordinary Classic footage and four eye-height views per map. The trace
  records frozen leg rotation through a carrying sprint at 3.6053 m/s.
- Baseline Core: 557/559; EditMode: 445/446. The failures were obsolete English
  display-copy expectations. They are updated without changing IDs or rules.
- HomeFlowTests now asserts top raycast targets before clicking; real loading
  story open/next/close holds past the maximum dwell and reaches the ready menu.
  Fresh result: 3/3. Keyboard/controller and full inherited screen coverage remain.
- The body change preserves holding-right's stable upper body with a masked
  authored leg gait. Charge writes after graph evaluation and before the hand
  attachment. Rebinding retires the previous graph; interrupted crossfades retain
  their visible mixture. No movement or action/contact timings were retuned.
- First-person smoothing preserves the original 60 Hz response and composes across
  frame rates. Authored hero clips retain priority; player-only empty procedural
  fallback creation is removed from the shipping path.
- Map baseline priorities: deepen Eskinita's frontages; reauthor Bayan's bare civic
  landmark geometry; establish material and shadow separation under Ilalim's rail
  deck. Preserve quiet ground and sourced livery. Actual Philippine references
  are being reviewed before authoring.
- Do not mark P1 or section 152.4 complete from these engineering changes alone.
  Full-speed comparison, cast modeling, all hero presentations, maps and player
  verification remain part of the active request.

### Second implementation batch, focused verification complete

- The first motion checkpoint was `8275111`. This document accompanies the larger
  model/world/action checkpoint; it is not a release-qualified Windows delivery.
- All eighteen live character models received grip, footwear and costume refinement.
  Faces, hair, skeletons and palettes are retained. Inday's distal hand props were
  removed and unused vertices compacted because the hand anchor/slide solver read
  those arrays. Her slide was re-solved against the repaired geometry at unchanged
  timing. Versioned turnarounds for the whole roster: `Logs/cast-finish-v2`.
- `ViewmodelArmAuthor` bakes actual arm-weighted model geometry and atlas coordinates
  into both first-person arms. It runs in RosterBookBuilder so subsequent model work
  cannot silently leave the first-person costume behind. The close camera uses a
  bounded cross-section; shoe normalization compensates each transformed axis.
- `author_hero_action.py` now owns current presentation times. The action batch
  replaces only named clips on the six heroes and both retained custom rigs, leaving
  other clips and mesh data intact. `Logs/hero-action-finish.json` records all 54
  exports. `Logs/hero-actions-baseline-v2/hero-coverage.csv` records eighteen real
  input-driven baseline casts. Magnet required a loose owned shoe, correctly.
- All eighteen first-person curves now use both hands, differentiated by the hero's
  existing job, with continuity on interruption and return. Refusal/reset cancels
  matching hero presentation without changing ability gameplay. Trips cannot resume
  a stale body one-shot after getting up.
- Warning geometry corrected to existing live reach: Supernova 5.4 m, Kuro 4.0 m.
  Kuro's preview uses its actual pet position and retains the petless fallback. The
  old 3 m AI cap ignored that 4 m field; a focused actual HazardMap reproduction now
  recognizes it and returns a side route with the 4 m cap. No hit radius was enlarged.
- New neighborhood meshes are generated by `author_neighborhood_models.py`.
  `NeighborhoodFinishAuthor` runs on the existing scenes and from the map builders.
  Its first render exposed reversed facade orientation and overly dark bridge fill;
  these were corrected before further review. The new rain-tree-like canopy is used
  at two Eskinita and four Bayan perimeter locations. Ilalim keeps its urban planting.
- Grounded footfalls use the retained landing recording, and slide has a separate
  scrape derived from retained dash/rubber audio. Preferred can/UI recordings remain
  untouched. The new cues are registered; all positional and NetCue ownership audits pass.
- Fresh EditMode result: 467/467. This includes exact arm vertex/UV/topology matching,
  all eighteen two-hand curves, interrupted recovery and live Kuro avoidance.
  The first editor-check pass caught a clearance edit applied to the generic prop
  loop; it was corrected so props remain at 1.4 m and trip hazards alone require 7 m.
  The corrected full checks are running under `Logs/presentation-checks-v2.log`.
- All new Unity runs use `tools/run_unity_guarded.py`, which copies and restores the
  existing Windows profile files and verifies hashes. The partition and qualification
  tools use it too. Never delete the profile. The first guarded runs restored 9 files.
- V0 screen sweep exposed two stale translated control names, a below-floor caption,
  an inactive-tab measurement, and two remaining investigations (custom-room summary
  layout and match-record fixture identity). The narrow fixes are implemented;
  focused reruns remain. Maker remains inaccessible.

Remaining before release: finish P2/P9 actionable audits and current UI failures,
run complete after-captures including ordinary play and all eighteen real casts,
review FPP framing, grounded contact, overlap and map boundaries, run the full
isolated gate twice and Windows build, then exercise the exact player and local
host/joiner paths. Do not substitute the EditMode pass for these outcomes.

A release/correctness blocker moves ahead of art. Otherwise prioritize perceptible
improvement to the repeated play sequence over housekeeping and invisible detail.
A verified decision to keep a good existing asset counts as work; exporting it just
to make it different does not. Human feel/taste approval is never inferred from a probe.

## V0: final UI verification

Implemented surfaces are described in CALM_FRONT_END. The final illustrated/loading
integration was only statically reviewed after the user stopped testing. Start here:

- Compile/import the exact branch. Check the four illustration textures, alpha,
  layer alignment and real 14-second motion. Ensure UI text stays sharp and still.
- Run HomeFlowTests and TypographyTests; update stale expectations only when the
  user-requested UI change makes them obsolete, preserving stronger coverage.
- Walk home to settings/back, profile/close, character and gear/back, both rules
  branches, tutorial, ranked and custom entry. Verify actual raycasts, keyboard and
  controller focus/submit/back, not just callbacks invoked in isolation.
- Verify the maker is inaccessible through normal Open and every player door;
  keep the authoring implementation and data intact.
- Exercise startup: random 5-15 second display window, actual asset/account barrier,
  click art, next story, close story, no auto-exit while reading, then clean menu
  activation. Check offline and a slow preload. Do not bypass real readiness.
- Check normal, 1280x720, short wide, 4:3 and ultrawide layouts. The remaining
  inherited screens may need spacing/contrast adjustments with the supplied fonts.
- Review English display copy in the HUD, tutorial, picker, abilities, ranks and
  rewards. Labels changed while identifiers and thresholds stayed intact. Check
  longer defender labels and existing saved titles; do not overwrite player data.
  Update literal-copy expectations where the requested language change warrants it.
- Run the screens group and isolated InputSurfaceProbe. Resolve repeatable current
  UI failures; the single-process PlayMode aggregate is not a trustworthy gate.

Earlier checkpoints: Core 559/559; EditMode progressed from 442 to 446 passed;
InputSurfaceProbe 5/5 after the result-plane repair; focused HomeFlowTests 2/2 before
the final illustration/loading replacement; all eight editor checks and gating
source audits passed before those final changes. No final-state test/build claim.

## P1: the core sequence

Review throw preparation/release, bank/bounce/rest, can knockdown/reset, ordinary
pickup, successful and failed slide, missed lunge, tag, interruption and recovery
as one sequence. Keep the slipper, defender and exit route trackable throughout.

Sean's authored slide and dedicated first-person slide already exist. Preserve
what passes. ASTRA task 3 still requires full-speed match entry/exit and pickup
alignment, plus owner/observer evidence. Contact samples at 0.140, 0.250 and 0.342 s
are review points, not new gameplay timers. The 0.950 s duration and current motion
numbers must not be retuned from the old bot usage ratio alone.

New Inday evidence is `docs/reports/motion/inday_slide_*_v1.png` and
`inday_slide_v1.txt`: authored slide resolves at slot 0; height 1.846 m; the low
arm-weighted geometry contacts the floor through most of the clip. The image shows
long supports/accessory-like geometry below the hands. Confirm exactly what it is
before changing the rig or action. The metric labels all arm-weighted vertices as
hand; it does not prove that the touching point is a palm. No GLB changed here.

### Highest-value motion candidates found in the source

These findings are grounded in current code but still need runtime observation.
They are not permission to remove historical fixes without replacing their purpose.

1. **Moving while carrying currently chooses a holding pose before locomotion.**
   `Runtime/Visual/CharacterAnimator.cs::Choose` returns `HoldingRight` before it
   reads horizontal speed. Its comment explicitly accepts still legs to stop the
   hand/slipper from swimming. Fix both problems together: investigate an authored
   carry walk/run or a carefully masked lower-body gait with a stable grip. Simply
   moving the holding check below walk would reintroduce the documented hand fault.
   Inspect charging and fatigue too; both also preempt the gait. Keep their visible
   upper-body tells without making a moving body skate or corrupting the grip.
2. **The sprint threshold does not match ordinary attacker speeds.** `Choose`
   uses 7.5 m/s. `Balance` currently gives base speed 4.6, attacker scale .55 and
   sprint scale 1.5, or 3.795 m/s before character/zone modifiers. Defender sprint
   is 7.59 before those modifiers. Confirm actual speeds and authored clip cadence
   for both modes and roles. Choose gait and playback rate from observed motion
   relative to the correct movement range, with hysteresis where needed. Do not
   read only the local sprint key, which broke remote/bot presentation previously.
   Avoid speeding up the rules just to cross an animation threshold.
3. **First-person recovery uses a frame-dependent interpolation factor.**
   `Runtime/Camera/ViewmodelArms.cs::StepToward` uses `Clamp01(ReachSpeed * dt)`
   with ReachSpeed 14. Assess carry/recovery at 30, 60 and 144 fps and during a
   short stall. Exponential smoothing is a candidate for consistent settling;
   preserve the intended response speed and authored release/contact timing.
4. **Interrupted transitions need continuity and lifecycle inspection.**
CharacterAnimator.Bind creates a new PlayableGraph without an obvious prior-graph
retirement in that method. Verify the live rebind path and lifecycle before fixing.
Also examine interrupted crossfades: the two-input mixer discards the prior outgoing
input when another transition arrives. Demonstrate a visible snap before changing it.

For all four, inspect the complete graph and procedural pose order before choosing
a solution. Maintain the current first-person slide and the observer's authored
action. No root motion may fight the authoritative motor. Review actual bone and
slipper trajectories; increasing a blend duration everywhere can soften a pop while
making the controls feel delayed. Favor a small explicit solution over a replacement
animation framework unless the existing design demonstrably cannot carry the fix.

### Input and contact review

Use ordinary keyboard/mouse play as the primary Windows path. Inspect press,
hold, release, cancellation, focus loss, pause/resume and regained possession.
Distinguish input latency from slow anticipation or a camera/hand settling delay.
Keep the aim point and the actual slipper release aligned; do not add aim assist,
input buffering or extra movement mechanics by default. If an existing action drops
a valid press, reproduce that case before changing the input contract.

Review running diagonally into scenery, turning beside a prop, sliding past a curb,
landing near a boundary, retrieving a resting slipper and getting tagged during
recovery. Use visible body geometry, motor position and authority events together.
Smoothness must not come from displaying a body somewhere other than its legal
position or accepting a pickup/hit the host refused.

Done: convincing preparation/contact/recovery at full speed, truthful possession,
clean interruption and return, and the same authored action on owner and observer.
No animation event may invent a pickup, score, hit or authority result.

## P2/P9: concrete engineering and collision work

Already implemented: result controls above the gameplay input plane (152.1),
dormant maker and graphics profiles/linear outline mask (152.2).

Work from the existing queue, reproducing before changing:

- 151.6: the hazard exclusion rule and MapGeometryCheck disagree. Give hazards
  their correct bound without increasing the general prop-clearance constant.
- 151.18: orphan roster assets. Preserve intentional internal custom entries;
  remove only proven orphans and make the builder detect their return.
- 151.21: PersonSwapProbe face/hair checks must suit the deliberately bare custom
  rig while retaining coverage on a subject that actually has those features.
- 151.15: classify positional audio outside NetCue, including owner-driven calls.
- 149.4: remaining one-shot/duplicate/replayed requests; preserve already-cleared
  vote/reset paths. Fix reachable effects with focused regressions, not a framework.
- 149.5: measure scene lookup cost before caching. Active/inactive slippers have
  different semantics; the defender's parked shoe must not disappear from diagnostics.
- 149.7: remove redundant tests only when the stronger owner of each invariant is named.
- 151.9: trace the defence/round clock discrepancy, low lunge hit ratio and Ilalim
  idle outlier. Distinguish probe artifacts, aim and balance with actual data.

For collision/body meshes, inspect capsule-to-visible-body alignment, true skinned
foot contact, obstacles and map edges during walking, sprinting, jump, slide and
recovery. Preserve host distance-based contact, square confinement and one scoring
owner. Fix demonstrated contact/geometry faults; do not substitute trigger-based
combat or speculative per-mesh player collision.

## P4: coherent presentation and sound

Create a comparable ordinary-play reference on Eskinita. Use the current illustrated
palette as direction, not a mandate to repaint every authored object. A warm,
readable street can keep cool-neutral shade and distinct hero accents. Role orange
and blue still communicate gameplay.

- Inspect shader response, line weight, aliasing, depth and material separation.
  The game camera currently leaves its far clip at the default; WorldOutline's own
  note identifies wasted depth precision. Assess a shorter plane against real map
  visibility before changing it. The exploratory 240 m edit was not landed.
- Keep the foreground crisp. No blur, chromatic fringe, constant shake, FOV pumping,
  whiteout or stronger bloom as substitutes for art direction.
- Compare Low/Balanced/High graphics in actual matches. Lower settings may reduce
  expensive decoration/shadows, never hide possession, roles or ability tells.
- Slide and shove still resolve to the same recording. Give their decisions distinct
  sound where needed, keep failed pickup free of a catch cue, preserve preferred tin
  recordings, and verify the cue is heard once on the right peers.
- Verify that approaching footfalls actually have a producer. The audit/read pass
  found references in prose but no clearly named stride producer or footstep asset.
  If absent, add restrained motion-driven spatial foley without omniscient chase cues.
- Build a short sound hierarchy around grip/release, rubber travel/landing, the
  preferred tin hit, catch and movement. Use existing recordings where they fit.
  A variation should sound like the same object; random pitch extremes are not
  personality. Ambience can suggest neighborhood life while leaving room for the
  can and chase. Do not add a constant wall of crowd, traffic or music to every map.
- The non-gating audio audit still flags six files. Inspect the actual DC-offset
  findings; any correction must preserve recording identity, timing and mix intent.
- Shop reactions should sound like their cause rather than another score or fight.

Done: a clearer and more satisfying throw-to-retrieval sequence with space in the
mix and no lost information. Preserve local/opponent/spectator parity.

## P5/P6: characters and heroes

Preserve canonical faces, skin and personalities. Subtle Philippine clothing,
material and gesture cues are welcome, not compulsory costumes on everyone.
Fix visible silhouette, clothing overlap, mesh/skinning and material problems at
normal play distance before adding detail that only appears in a turnaround.

**Maps, model geometry and animation are major deliverables of this request.**
Give the full existing Classic cast and all six heroes an explicit asset review,
including the reusable body, hands, footwear and can models. Record which assets
need substantial reauthoring, which need focused repair and which already support
the intended standard. A shared material adjustment does not establish that the
modeling work is complete. Where the design is weak, improve the design itself.

For characters, review head/body/hand proportions, readable hair and clothing
silhouettes, facial expression, shoulder/elbow deformation, footwear attachment,
accessory intersections and the body's relationship to its collider. Preserve
recognizable faces and skin while making the construction deliberate. Maintain
the game's expressive stylized bodies; extra polygons should buy a visible shape
or better deformation. Choose locally plausible fabric and clothing details where
they fit that person. Do not recolor every character into the same warm uniform.

For the shared action library, review idle variation, starts/stops, turns, walk,
run, carry, charging, release, jump/landing, slide/retrieve, lunge/shove, fatigue,
tag response and recovery on representative different body proportions. Work on
weight transfer, planted support, action arcs and follow-through as authored motion.
Timing and hand contact must support the existing rules. Preserve the effective
body/FPP timing contract while improving poses, curves, skin weights and transitions.
Keep the can and slipper visible when the action needs the player to track them.

Use source assets and repeatable export/import steps. Keep rig names, skinning,
roster bindings, materials and animation reachability valid after reauthoring.
Save turnarounds plus in-engine motion evidence for changed geometry, including
normal gameplay distance. If a shared rig change improves one hero but breaks
another body or its first-person arms, the batch is incomplete.

Review each hero's three casts in body, first person, VFX, audio and exit:
Sean propels; Zack snaps/carves; Dante plants; Cheska draws/holds; Nemu drifts/pulls;
Phaister performs. Their eighteen authored body clips already exist. Align the
procedural first-person tells with them rather than assuming eighteen clips are missing.

Use the current accepted-cast presentation flow. If an ultimate needs a separate
pose/action hook, establish and verify that contract before authoring unreachable
assets. No longer shared freeze, input lock, altered hit window or larger footprint.
Zack's ThunderShockRing is a known composition target. Nemu's live ultimate uses
KuroUnbound; do not polish a showcase-only SeanceVoid path by mistake.

Build a working review sheet for all eighteen existing hero actions with their
actual producer, body clip, first-person tell, effect, sound and gameplay purpose.
Improve preparation, the active moment and recovery together. VFX need deliberate
shapes, timing, material response and restrained secondary detail: fire can gather
and stretch, lightning can branch with a clear origin, stone can carry weight,
ice can have readable planes, and spirit/sigil effects can keep their distinct
motion. Keep the actual footprint and warning boundary truthful. An attractive
effect that hides the target, changes perceived reach or stays after cancellation
is unfinished. Abilities may be substantially re-presented without adding a new
mechanic, another input or an extra status system. Balance changes require their
own demonstrated gameplay reason.

Review overlap in real matches, not just each effect on an empty stage: priority
between the can and an ultimate, two casts at once, owner near clipping, opponent
counterplay, spectator view, color/shape readability and Low graphics. Particle
density, flashes, sound peaks and camera response form one attention budget.
Produce one unmistakable peak per action with room to recover. Existing skills
should become easier to understand and more satisfying to use at the same time.

Done per hero: recognizable jobs without effects, distinct effects without the
body, one clear peak and recovery, interruption/refusal coverage, and remote parity.
Changed GLBs require versioned in-engine images, roster rebuild and import validation.

## P7: maps with purpose

For each map capture a normal first throw, retrieval at eye height, defender reverse
view and spectator view. Improve composition, light, materials, landmarks and depth
across those views. Add, remove, relocate or author assets when they make the place
more convincing. Furniture needs a reason to be where it is. Keep quiet playable ground.

All three maps receive this pass. Rebuild weak architectural forms, ground/curb
meshes, facades or landmark silhouettes when that materially improves the place.
Use existing assets or author suitable new ones as needed. Judge the result from
the player's repeated routes and camera height, with the original play space and
core rules preserved. Better geometry, placement, lighting and materials must read
together as one designed environment.

- Eskinita: intimate neighborhood scale, recognizable shop/frontage, laundry and
  layered homes. Warmth and close rivalry, not a corridor of repeated empty houses.
- Bayan Plaza: deliberate civic openness, monument/church/tree silhouettes, shade
  and clear places for a crowd. Empty ground can be a positive feature.
- Ilalim: real Gilmore/LRT reference, guideway mass, electronics/food/computer edges,
  intact jeepney and restrained shop-light pockets. Trace the recorded idle seed
  and slipper resting positions before changing collision or routes.

No unmeasured asset decimation or blanket sourced-art repaint. Existing imported
materials/livery deserve preservation. Lighting fixes and model edits must improve
ordinary frames, not only a chosen beauty shot.

Give each map a small composition sheet before dressing it: its real-place
references, dominant large shapes, sun/shade relationship, two material families,
one memorable landmark and which space must stay quiet for play. Use actual
Philippine references, not a generic tropical street with flags added. Keep the
architecture and infrastructure internally plausible. Add cables, plants, stalls,
signage or seating in purposeful groups, not evenly scattered clutter. English
interface copy remains the rule; do not invent Tagalog slogans as decoration.

Check visual and physical boundaries together. Remove irrelevant blocking props,
repair floating/intersecting meshes, and ensure every legal slipper resting place
can be understood and reached. Put visual richness at the perimeter when that
improves the map without obscuring the contest. Do not fill the plaza just because
its center is open, or leave the neighborhood map empty just because collision passes.

## P8: beginnings, endings and spectator clarity

Check ready/round start, role swap, interrupted casts, final action, result and
rematch. Clear stale effects, input ownership and animation state. Use existing
victory/audio/replay hooks. A small truthful result highlight reader can reuse
MatchHighlights (147.3) if it improves recall; no new score/reward system or noisy ticker.

## P10: verification and Windows delivery

After the implementation batches, run appropriate focused regressions, Core,
EditMode, the discovered PlayMode partition, Checks.RunAll and source audits.
For a release candidate use the isolated gate twice; assert fresh non-empty XML,
actual fixture coverage and zero failures. Reproduce isolated failures rather than
blaming everything on historical aggregate contamination. Keep cloud/hardware skips
explicit. Do not lower readability bounds to obtain a green report.

Build Windows only when the selected work is ready. GameBuilder purges its validated
previous output. Verify timestamps and launch the exact player. Exercise Classic and
Hero Strike plus relevant host/joiner/observer paths. Do not claim handset, physical
pad, human feel or venue-acoustic approval without actually obtaining it.

Update TODO in every work commit. Archive finished numbered sections whole, keep
index pointers, and give unresolved findings a location, owner and done criteria.
Push ASTRAReworks. Finish with the actual checkpoint, evidence, limitations and any
human decisions still needed. Re-plan from evidence whenever that produces a better game.

### Current checkpoint evidence

The current presentation batch is described in
[the report](reports/improvement-2026-09-10/presentation-batch.md), with all eighteen
accepted cast rows, owner/body reels, complete cast turnarounds and matched map views.
Core 559/559, EditMode 467/467, targeted capture/navigation 18 passed plus one skip,
clock/AI diagnostics 3/3, PersonSwapProbe PASS, fourteen gating audits clean.
The bot clock discrepancy was inherited eight-round configuration, now pinned and
printed explicitly. No scoring retune. Full isolated PlayMode twice, fresh Checks,
Windows build, exact-player play/network/performance and outstanding queue review
remain required. UI effort remains functional only.

## Owner review and expanded individual pass, 2026-09-10

The owner reviewed the full cast sheet and explicitly rejected the first model
refinement as too subtle. Most Classic characters still read as identical or odd.
The model deliverable is NOT accepted or complete. Author and review characters
one by one, with individually designed faces, proportions, hair, clothing and
silhouettes. Shared low-level mesh/export utilities are fine; a shared face/body
recipe with different colors is not the requested work. Rebuild first-person arms
after the body designs settle. Classic characters go first. Existing names, saved
IDs, rig/clip contracts and the game's recognizable identity remain. The latest
request permits redesigning the Classic faces; earlier face-preservation guidance
does not freeze the generic faces the owner is rejecting.

The owner also adopted all seven scrutiny priorities: stronger silhouettes, better
footwork, more convincing cast commitment, readable effect overlap, believable
chases, purposeful map edges and consistent first-person feedback. UI is excluded
from further polish. They explicitly say most skills feel weak and authorize
improving every existing skill's functionality, sound and effects. All six
ultimates must feel imposing through buildup, impact and aftermath. Preserve the
core game, skill count, networking authority and two modes; do not add complex
systems. Per-ability timing/function may change when a concrete improvement merits
it; document the reason and update the actual descriptions and coverage.

### Cadence follow-up

The e730878 checkpoint passed Core 559/559, EditMode 466/466, all eight checks and
the isolated PlayMode gate twice: 197 passed, 9 skipped, zero failed per pass. This
proved correctness of that checkpoint, not the owner's model/skill taste approval.
All original results are retained under Logs/qualification-e730878.

An additional actual-clip measurement found Berto's walk advanced the calibration
2.016 m per cycle while his feet traveled only 0.769 m. The new regression was seen
red across the roster. Cadence now uses each rig's bind-pose leg reach and the
authored swing angles. Walk swing is 38 degrees; sprint remains 44. The authoring
script reads these same angles. Footfall accents follow the resulting cycle length
and do not advance the AI random stream. Fresh EditMode: 467/467; targeted real
motion/retrieval: 18/18. Logs/gait-cadence.csv records all forty gait comparisons.

The existing network report now exposes the already-collected live frame histogram
and actual graphics/hardware settings for exact-player verification. It honors an
existing collection opt-out. Bot reports now record resting slipper positions and
owner state every five continuously-loose seconds, for the historical bridge lead.

### Next individual work

Berto: a grounded neighborhood regular, with an individually shaped head, simple
flat eye/mouth graphics and cropped hair; a complete rolled-sleeve work shirt,
khaki shorts, towel and rubber footwear. The owner explicitly clarified that flat
faces are intentional; the earlier proposal for realistic noses/brows is withdrawn.
Author his geometry individually with compatible articulated limb joints,
inspect front/side/back and ordinary gameplay distance, then continue the remaining
Classic cast and the six heroes. This is a visual brief, not new gameplay lore.

Skill review has already found a concrete stale behavior: Zack's aimed Thunderstrike
still applies forward impulses for its seven-second active period, inherited from
an older overdrive design. Its recall also uses a radial arc footprint scaled by
the entire recall distance. These need targeted functional/presentation review,
not a generic increase in brightness or hit radius.
