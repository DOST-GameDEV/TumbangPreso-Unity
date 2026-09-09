# Game improvement plan

Current state: UI PLACEHOLDERS IMPLEMENTED; FINAL VERIFICATION AND GAMEPLAY PASS QUEUED.
Starting checkpoint: `0028b3a901b8c338538d31251380c6433a7f6aa6` on `ASTRAReworks`.
Execution ledger: TODO section 152. This plan is not a claim that all phases are done.

## Authority and current delivery

The latest request is to finish the UI placeholders and planning, push the branch,
and provide a copy-paste handoff in chat. The user explicitly stopped further test
runs and player builds in this chat; the next chat owns those checks. Earlier
passes do not verify later changes. No final player was built for this UI state.

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
| V0 | next | Verify the final UI/asset/loading changes before building on them |
| P1 | queued | Complete the throw/can/retrieval/chase motion and feedback reference |
| P2 | partly complete | Close concrete correctness and release-integrity findings |
| P3 | implemented, verification pending | Home, mode selection, icons, fonts, graphics settings, loading and dormant maker |
| P4 | queued | Establish one coherent visual/sound reference on Eskinita |
| P5 | queued | Finish Sean's complete three-cast presentation |
| P6 | queued | Carry distinct finish through Zack, Dante, Cheska, Nemu and Phaister |
| P7 | queued | Improve all three maps and investigate the Ilalim retrieval outlier |
| P8 | queued | Connect round starts, interruptions, final action and match ending |
| P9 | queued | Resolve remaining actionable findings and documentation drift |
| P10 | next chat | Run release checks, build Windows and verify the exact player |

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
