# TODO: Tumbang Preso Unity

**How this file is organised (docs cleanup, 2026-09-23).** This file is the ONE status
queue: the current queue and priority order, then one index row for every numbered entry.
Nothing was deleted or renumbered.

- Open numbered entries (§ 155 down to § 69) keep their whole bodies, unchanged, in
  [TODO_Backlog.md](TODO_Backlog.md). A `docs/TODO.md § N` pointer anywhere in the repo
  lands on its index row below, which links to the body.
- Finished entries and batch reports stay whole in [TODO_Archive.md](TODO_Archive.md),
  indexed at the bottom of this file. The queue text as it stood at `a1110a9e`
  (2026-09-22) is preserved verbatim in
  [archive/TODO_queue_2026-09-22.md](archive/TODO_queue_2026-09-22.md).
- Detailed design lives in [NATIONALS_POLISH.md](NATIONALS_POLISH.md); the exact resume
  state lives in [ACTIVE_REWORK_LEDGER.md](ACTIVE_REWORK_LEDGER.md).
- Section numbers are not unique (§ 53, 63, 64, 65 repeat). Search by title too.

## CURRENT IMPLEMENTATION QUEUE

**Standing mandate (owner, 2026-09-21):** "finish everything note yet done", then "i want
every single thing in todo to be done pls mark that in todo and shit". Every unfinished
item in this file, including the backlog index, is in scope. Each item ends with
implementation plus evidence, a specific completed replacement, or a named external
dependency. An old OPEN heading is not proof code is missing, and a task may not be
skipped because it is old. No parent is checked while an actionable child is open.

**Supersession check (owner2026-09-23):** newer instructions and current adopted
designs win. FUTURE.md and archived plans are historical, not independent task
sources. Resolve each old entry as current work, already implemented, superseded
or retired before adding it to execution. An old OPEN label does not reactivate a
proposal. Preserve the original IDs/body and link its replacement or reason; never
restore obsolete behavior or completed-UI layouts merely to clear a checkbox.

**Owner direction, 2026-09-23:** make the game more visually appealing and satisfying to
play without making it more realistic; make the in-game HUD minimalist, professional and
easy to look at; explore on-screen effects and indicators that work together without
overwhelming the player (Sepak U named as the example); rethink priority. This is
VISUAL-1 below. It supersedes the 2026-09-01 "do not touch the in-match HUD" scope note
for this pass and changes no rule, timing, authority or network contract.
Owner, same day: "u can edit all UI and hud in the actual game btw including the match
end and mid round report and icon", "u figure out which to edit and thoroughly revamp
it too and make it more visually pleasing + good to look at". Every in-match surface is
in scope: HUD, prompts, feed, banners, the halftime and round reports, the match-end
board and the in-match icon set (VISUAL-1.4, 1.16, 1.17, 1.18). Owner and girlfriend
supplied artwork is still preserved, and the front-end menus stay as they are.

**Standing sequencing rule (owner, 2026-09-22):** avoid verification and test-repair
loops. Use the smallest check that resolves the risk a change introduced; comprehensive
regression happens once, at final integration. Quality stays the priority.

**Latest quality rule (owner2026-09-23):** inspect before changing. Improve only a
concrete visual, usability or functional weakness; keep successful parts. Apply this
to UX-1 and each REFINE-2 map/character/aspect, without dropping unfinished tasks.

### Priority order, rethought 2026-09-23

1. **P0, finish what is in flight. ✅ DONE 2026-09-23.** The 134.10 reduced-effects link
   (Hitstop, SkyEvent, CanContactAccent, TumpHudEffects, UltimatePresentationDirector and
   others, plus camera shake) and the native `--accessibility-only` route are published;
   native v57 passed all 15 stages, EditMode 19/19, Core 615/615. Receipts in
   `reports/full-backlog-2026-09-21/accessibility-completion.md`. One 127.3 follow-up and
   the shutdown crash below remain open.
2. **P1, VISUAL-1 batch A: communication and the in-match UI.** Order: 1.4 and 1.18 (the
   HUD and its icon family), 1.6, 1.1, 1.2, 1.3, 1.5, 1.16, 1.17, 1.7. The HUD leads because
   the owner asked for the UI revamp twice and every later capture sits under it; the
   danger work follows because the game's thesis (retrieval risk) has no picture yet.
3. **P2, VISUAL-1 batch B: the world.** 1.8, 1.9, 1.10. Lighting, court and hero objects
   change every frame of every map.
4. **P3, remaining expansion work.** The lagoon deck sampling refinement and any open
   Rafi or lagoon integration items below.
5. **P4, VISUAL-1 batch C with PRESENTATION-1.5.** 1.11, 1.12, 1.13.
6. **P5, VISUAL-1 batch D with the rest of the existing scope.** 1.14 and 1.15 alongside
   152.4 / MAP_FINAL_PASS, 151.9 / 151.19 and 153 / U8 / 149.4 / 145 / 143.
7. **P5.5, UX-1 front-end flow and progression addition (owner2026-09-23).**
   After the existing visible work, implement the new Home and flows below. Preserve
   login and main menu; Home opens from the existing TAP TO START. This is separate
   from the completed in-match UI/HUD pass.
8. **P6, backlog disposition. Source/design review COMPLETE2026-09-23.** All366
   original heading entries are preserved and individually accounted for in
   `reports/full-backlog-2026-09-21/todo-disposition.json`; none remain unreviewed.
   This is not full implementation/qualification completion. Current gaps remain
   explicitly open below; retired FUTURE/layout recipes are not reactivated.
9. **P7, final coherent qualification and build.** PRESENTATION-5.2, the final
   integration item and 152 final delivery on one frozen, identified candidate.
10. **P8, REFINE-2: map-by-map asset and actual-gameplay refinement (owner2026-09-23).**
    After the older actionable work above, thorough reference research and planning,
    then each map individually, natural ambient life, all-bot inactivity diagnosis
    and gameplay/animation refinement. Existing tasks remain; this is additional work.

**Why this order.** P0 protects dirty work. P1 comes before P2 because a clearer game
reads better even with today's lighting, and because it removes HUD and text that P2's
captures would otherwise have to be retaken around. Paperwork (P6) moved behind visible
work because the owner's standing complaint is loops that do not change the game.

### LIGHT-1 · Bright PEAK-style lighting and edges ⚠️ IN PROGRESS, 2026-09-23

Integrated into ASTRAReworks on2026-09-24at owner request, through lighting branch
50f1fc255(merge241e13bb5). Existing Windows Stage/ramp checks2/2passed; five-map
after frames inspected. This is now the map-art baseline. Remaining preview,
tuning and performance rows below stay open; keep tracking the source branch.
[Integration evidence](reports/lighting-integration-2026-09-24/report.md).

Owner request: overhaul the "gloomy and dark" lighting to be bright and pleasing like PEAK
(Aggro Crab and Landfall, 2025), not realistic, and make the edges similar to PEAK. Work is on
branch `lighting/peak-bright-overhaul` (off `ASTRAReworks` at `2a3c7e16`), one commit per
change. This supersedes VISUAL-1.8's darker ambient and near fog on that branch; the off value
(`WorldCueProfile.WorldLighting` 0) still restores each scene's own lighting.

**Research (PEAK Steam store frames, Game Informer review):** high key with no true black;
shadows carry hue (warm sun, sky-coloured shade); bright horizon-matched haze layers the
distance; soft plush shading on the cast with no ink; edges read from light and colour (lit
convex bevels, coloured inside corners), not black lines; soft bloom on sky and highlights.

- [x] LIGHT-1.1 Per-map rig in `WorldLookProfile` (+ asset): sun colour, intensity, lifted
  elevation (azimuth kept), shadow strength, saturated sky-tinted ambient, haze colour and
  range, sky/cloud colours, coloured black lift. Applied and restored by `WorldLookPresentation`.
- [x] LIGHT-1.2 Cast soft wrapped terminator with warm band (`Toon`, `ToonTransparent`).
- [x] LIGHT-1.3 Cast hull drawn in a deeper shade of its own colour at 72% width; coloured
  gameplay outlines (landed slipper) untouched; previews keep black ink.
- [x] LIGHT-1.4 World edges: near-side silhouette deepening, sun-side convex highlight,
  coloured concave crease, replacing black ink under the look (`WorldOutline`).
- [x] LIGHT-1.5 Grade: HDR bloom (off on Low tier), coloured lift, vibrance (`ColourGrade`).
- [ ] LIGHT-1.6 Tune from the first render. The v2 render fixed four
  of the five v1 problems, each its own commit: bloom threshold 1.7 and intensity 0.12 so the lit
  cast no longer haloes (`7f7dc42e`); Eskinita and SaBubong skies blue instead of lavender, top of
  frame (195,196,239) to (154,191,241), by pairing a cream or gold horizon with a cyan-leaning
  zenith (`d30fcb16`); Bayan and Lagoon haze deepened to a light sky blue (`eb27679c`); per-map
  `GroundLift` 1.6 on the Eskinita and Ilalim court asphalt through a property block, sunlit road
  (90,85,71) to (158,150,119) on Eskinita and (124,126,106) to (181,185,156) on Ilalim, found by
  shape and logged (`68d297ba`). Edge close-up checked: no black ink, silhouettes darken their own
  colour, eaves carry a lighter bevel. Still open: SaBubong read milky in v2, so its haze now runs
  60 to 300 m (`848e21d4`, NOT yet rendered); whether Ilalim's sunlit road at 181 is too pale is a
  taste call; overall contrast is lower than PEAK's, left for the owner rather than tuned blind.
- [x] LIGHT-1.7 The two Stage tests assert the bright look's own claims (`20c977e5`): applied rig,
  bright shade colour, haze past the court, court ground found; toon ramp measured 1.70:1 under the
  look against 1.95:1 authored, asserted inside 1.35 to 2. WorldCourtCueTests 3/3 on the Mac.
- [x] LIGHT-1.8 Map-select/lobby previews now install the adopted selected-map look
  with an explicit sun and measured floor. Tagged world cameras only; same-map
  refresh reuses the live rig/sky. Map switches and destruction release the old
  state; cached-map court brightening fixed by clearing property-block ownership.
  Initial preview/transitions4/4passed, focused revisit/preview2/2passed; actual
  overview/small/grey25inspected. [Evidence](reports/lighting-integration-2026-09-24/preview/report.md).
- [ ] LIGHT-1.9 Performance check of bloom plus edges on the Balanced tier, and a native build
  look at the owner's window shape.

Capture: `WorldCourtCueTests.BrightLookSameCameraCapturesOnAllFiveMaps` writes stage, eye and
cast frames per map to `TUMP_WORLD_CUE_OUT`. Baseline 1/1 and branch v1 1/1 passed on the Mac.

### REFINE-2 · Map-by-map assets, natural life and actual play (queued after older work)

Owner reference: [3D Asset](https://3d-asset.com/). Requirements, order and research
questions are preserved in [the intake](reports/map-by-map-refinement-2026-09-23/intake.md).
[Exact owner feedback](reports/map-by-map-refinement-2026-09-23/owner-request.md) and
[the research/execution plan](reports/map-by-map-refinement-2026-09-23/research-and-execution-plan.md)
are saved. Aesthetic appeal matters more than realism; research must use multiple
references. Work one map, character/movement or aspect at a time.
Owner reiterated: finish assigned UI first. During this later map pass, generate
additional visual references, critique them and adopt only useful ideas; iterate
references to solve concrete gaps and verify the improvement in the actual3D game.
Latest owner additions: research PEAK and other relevant games first; make every
map more distinctly Filipino, each showcasing different cultural/place aspects.
The real map is visible behind LOBBY: fill sparse surrounding context with a
coherent spatial plan, not random decoration. Detailed later research/camera scope:
[cultural and camera brief](reports/map-by-map-refinement-2026-09-23/cultural-and-camera-brief.md).
The owner says assets still feel bland, animals repeat obvious fixed routes, and
bots were sometimes AFK/standing. Treat these as new experience reports; earlier
passing checks do not prove these complaints resolved. Do not start another global
map shader sweep as a substitute for individually improving all five places.

Owner reiterated: extend meaningful surroundings beyond the arena on ALL maps,
and improve plain/bland houses themselves with appropriate texture/construction
detail and stronger Filipino place identity. Research first and use built-in image
generation for critically reviewed inspiration. Extra background copies are not
house-quality completion. Each map gets its own distance plan and individual
building/material decisions, preserving the one-map-at-a-time order.

Owner also explicitly requests recognizable Filipino street life, including
TRICYCLES, researched thoroughly first. Inspect the existing passenger/cargo
tricycle and jeepney assets, choose map-appropriate forms and purposeful pickup,
parking, service/household settings. Ground/support them and preserve routes.
Include other researched place-specific objects; no identical cultural-prop pack
scattered across all maps. Execute inside each map's existing refinement row.

- [ ] **REFINE-2.0 Older-work gate.** Finish/account for older actionable TODOs with
  actual implementation/evidence; preserve shared/concurrent and external dependencies.
- [ ] **REFINE-2.1 Thorough reference research and plan.** First research good-game
  qualities and visual/motion satisfaction, then translate them into TUMP criteria.
  [Initial foundation notes](reports/map-by-map-refinement-2026-09-23/foundation-research.md)
  and [comparative reference notes](reports/map-by-map-refinement-2026-09-23/comparative-reference-notes.md)
  have begun; this research is not complete. Brown City, official PEAK media and
  A Short Hike's gallery were visually inspected; initial Philippine place sources
  and current Eskinita authoring routes are recorded. Eskinita context is implemented;
  the fresh [house material study](reports/map-by-map-refinement-2026-09-23/house-material-study.md)
  adds primary construction research, generated inspiration and explicit critique.
  Inspect actual rendered
  assets, cross-reference place/material construction, inventory per-map assets,
  write concrete keep/refine/replace decisions and ordered implementation plans.
  Include comparative game research, a sourced Filipino cultural/place brief per
  map, and a context-density plan at actual lobby/introduction/play/spectator angles.
  Owner PEAK and sparse-lobby reference images are preserved in ArtSource.
- [ ] **REFINE-2.2 Eskinita.** Individual asset/material/shape/detail refinement.
  Owner2026-09-24: building texture/detail acceptance remains OPEN; inspect and refine
  each family under bright lighting per [the all-map requirement](reports/map-by-map-refinement-2026-09-23/building-texture-brief-20260924.md).
  First context group implemented:26retained-family outer houses and4connected
  streets, existing collision preserved. Matched actual-preview/small/grey25
  inspected; focused1/1passed after correcting new-group roof palette routing.
  [Evidence and remaining work](reports/map-by-map-refinement-2026-09-23/eskinita-context/report.md).
  Owner correctly rejected the remaining empty distance beyond that first row.
  A continuing96-block district now fills the exposed preview background using
  retained middle-distance geometry and simpler distant houses, one palette material
  and no new collision. Matched preview/small/grey25inspected; focused1/1passed.
  [Distance evidence](reports/map-by-map-refinement-2026-09-23/eskinita-distance/report.md).
  Primary home4_W now has individually fitted plaster/roof finishes, a measured
  supported door shade and a local correction for roof-tinted shrubs. Variant1
  rejected, variant2inspected at matched near cameras/small/grey; focused1/1pass.
  [House evidence](reports/map-by-map-refinement-2026-09-23/eskinita-house-b/report.md).
  Home5_W's horizontal timber no longer gets an incorrect vertical procedural grid;
  original boards/colors/windows preserved. Matched near/street/small/grey inspected,
  focused1/1passed. [Timber evidence](reports/map-by-map-refinement-2026-09-23/eskinita-timber5w/report.md).
  The same demonstrated cladding/material mismatch is now corrected individually
  at2_E/5_E/6_E, each inspected from a lit side and its shadowed street frontage;
  focused1/1passed. [East timber evidence](reports/map-by-map-refinement-2026-09-23/eskinita-east-timber/report.md).
  Other primary homes and5_W's remaining roof/composition details remain open.
  Home3_Wnow differs from0_W's shop: raised bamboo shade, basket and folded cloth
  on the retained sill. Neighbor shop/windows/steps preserved, no new collision.
  Matched near/street/small/grey inspected; focused1/1passed including the corrected
  persistence of reauthored timber materials.[Domestic evidence](reports/map-by-map-refinement-2026-09-23/eskinita-domestic3w/report.md).
  Bay2_Wnow has a researched, map-specific motorcycle/sidecar tricycle with improved
  glass, panels, mirrors, lamp and supported driver shade. Original vehicle sources/
  colliders preserved;3grounded tyres and old footprint checked. Placement variant2
  inspected in matched street/small/grey and four model views; focused1/1passed.
  [Vehicle evidence and limits](reports/map-by-map-refinement-2026-09-23/eskinita-tricycle/report.md).
  Five remaining primary masonry homes retain original plaster with fitted lower
  courses, roof detail and foliage fixes. Whole-wall draft rejected; clipped revision
  inspected under adopted bright lighting, focused1/1passed.[Masonry evidence](reports/map-by-map-refinement-2026-09-23/eskinita-masonry/report.md).
  Five overlapping tree crowns now open selected facade views; four timber-home
  planters have corrected green foliage. Lower trunks/roots/collision and other
  trees retained. Matched bright-look pairs/grey25 inspected, existing route1/1pass.
  [Garden evidence](reports/map-by-map-refinement-2026-09-23/eskinita-garden/report.md).
  Oversized mountain paintings now sit lower behind the district; original art
  retained. Matched game/grey25 and actual preview inspected, existing2/2passed.
  [Background evidence](reports/map-by-map-refinement-2026-09-23/eskinita-backdrop/report.md).
  Remaining shop/seating/court views inspected at High/Low; retained useful props,
  lettered two blank shop boards and replaced the outdated map-vote card with the
  actual bright preview. Sign pass1/1passed and paired/grey25inspected.
  [Final art-batch decisions and remaining integrated acceptance](reports/map-by-map-refinement-2026-09-23/eskinita-prop-review.md).
  Art implementation is ready for the integrated intro/spectator/native gate;
  this parent remains open until that acceptance. Continue Bayan art next.
- [ ] **REFINE-2.3 Bayan Plaza.** Individual asset/material/shape/detail refinement.
  Owner2026-09-24: building texture/detail acceptance remains OPEN; inspect and refine
  each family under bright lighting per [the all-map requirement](reports/map-by-map-refinement-2026-09-23/building-texture-brief-20260924.md).
  Next active map. [Primary place references and generated-study critique](reports/map-by-map-refinement-2026-09-23/bayan-reference-notes.md)
  saved. Eight mint hedge bars now have locally authored low foliage in the retained
  planter shells; variant3selected after paired/overview/grey25inspection,1/1passed.
  [Planting evidence](reports/map-by-map-refinement-2026-09-23/bayan-garden/report.md).
  Abrupt town/ground edge replaced by32connected blocks,38retained-family homes,
  68far homes and22planted plots. Original collision/landmarks retained; actual
  preview/overview/grey25inspected, focused1/1passed.
  [Context evidence](reports/map-by-map-refinement-2026-09-23/bayan-context/report.md).
  Four monument-edge pots now have supported shrubs/contained soil, original concrete
  retained. Paired/grey25/native preview inspected; focused2/2passed. Civic fronts/rear
  and south High/Low reviewed; sky20/180motion samples inspected and retained.
  [Pot and near-art findings](reports/map-by-map-refinement-2026-09-23/bayan-potted-shrubs/report.md).
  Sixteen e/o houses now have their existing fitted timber/jalousie/terrace finishes;
  original bodies/collision retained. Paired front/back/grey25and final preview
  inspected, focused2/2passed. Native map-vote card refreshed.
  [House finish evidence and art-batch disposition](reports/map-by-map-refinement-2026-09-23/bayan-house-finish/report.md).
  Art batch ready for integrated intro/spectator/native acceptance; keep parent open
  for that gate and continue Ilalim art next.
- [ ] **REFINE-2.4 Ilalim ng Tulay.** Individual asset/material/shape/detail refinement.
  Owner2026-09-24: building texture/detail acceptance remains OPEN; inspect and refine
  each family under bright lighting per [the all-map requirement](reports/map-by-map-refinement-2026-09-23/building-texture-brief-20260924.md).
  Next active map. [Primary reference intake and Gilmore/LRT2identity](reports/map-by-map-refinement-2026-09-23/ilalim-reference-notes.md)
  saved. First local finish replaces mint concrete/pink-yellow track lookup colors
  with neutral structural materials on24pillars/28bays/56tracks. Original geometry,
  collision, palettes and train livery retained. Paired preview/eye/under-deck/grey25
  inspected, focused1/1passed. [Evidence](reports/map-by-map-refinement-2026-09-23/ilalim-structure/report.md).
  The34existing low-detail skyline bodies now have distinct fitted facade treatments:
  514window groups/144glass divisions in3overlay renderers. Shapes/positions/collision
  preserved. Paired preview/street/district/grey25inspected, focused1/1passed.
  [Skyline evidence](reports/map-by-map-refinement-2026-09-23/ilalim-skyline/report.md).
  Outer visual ground edge corrected without new collision; near shop/service
  pockets, Low street and sky20/180reviewed and retained. Native card refreshed.
  [Final art-batch disposition](reports/map-by-map-refinement-2026-09-23/ilalim-final-art/report.md),
  focused1/1passed. Parent remains open for integrated intro/spectator/native gate;
  continue SaBubong art next.
- [ ] **REFINE-2.5 Sa Bubong.** Individual asset/material/shape/detail refinement.
  Owner2026-09-24: building texture/detail acceptance remains OPEN; inspect and refine
  each family under bright lighting per [the all-map requirement](reports/map-by-map-refinement-2026-09-23/building-texture-brief-20260924.md).
  Active map. [Primary reference and concept critique](reports/map-by-map-refinement-2026-09-23/sabubong-reference-notes.md)
  saved. Existing water tank now has fitted lid/bands/service outlet, original body/
  collision retained. Paired/grey25inspected, focused1/1passed.
  [Tank evidence](reports/map-by-map-refinement-2026-09-23/sabubong-tank/report.md).
  Stairhead now has fitted door/vent/entry housing/flashing detail; original body/
  noticeboard/mural/collision retained. Paired/grey25inspected, focused1/1passed.
  [Stairhead evidence](reports/map-by-map-refinement-2026-09-23/sabubong-stairhead/report.md).
  Shade now has supported beams/rafters and restrained panel seams; original
  furniture/posts/plants/collision retained. Paired/grey25inspected, focused1/1passed.
  [Shade evidence](reports/map-by-map-refinement-2026-09-23/sabubong-shade/report.md).
  Six individually used neighboring roofs now occupy verified vacant plots, with
  grouped windows/access and per-part material roles. Paired/grey/Lowinspected,1/1pass.
  [Neighbor roof evidence](reports/map-by-map-refinement-2026-09-23/sabubong-neighbors/report.md).
  Farther city/ground continuity, building-texture review and final acceptance remain.
- [ ] **REFINE-2.6 Lagoon.** Individual homes/piles/boats/water/context refinement.
  Owner2026-09-24: building texture/detail acceptance remains OPEN; inspect and refine
  each family under bright lighting per [the all-map requirement](reports/map-by-map-refinement-2026-09-23/building-texture-brief-20260924.md).
- [ ] **REFINE-2.6a Skies, islands and backgrounds.** After UI, inspect and improve
  animated skies, island/mountain layers and other distant/background scenery on
  every map. Evaluate composition, silhouettes, depth, materials, motion and harmony
  with that map. Existing animation is not automatic completion or a reason to skip
  refinement. Carry out changes within each individual map pass, keeping good parts.
- [ ] **REFINE-2.7 Natural ambient movement.** Diagnose repeated paths and author
  plausible habitat/goal/activity choices, motion and reactions per map.
- [ ] **REFINE-2.8 All-bot behaviour.** Observe both modes/roles/maps/roster/choices,
  trace idle decisions and fix actual stalls; distinguish deliberate tactical waits.
- [ ] **REFINE-2.9 Gameplay and animation.** Review actual complete exchanges,
  movement/collision, interaction and interruption, body/FPP/contact/observer timing.
  Owner specifically reopens walking (Sean arms stick to body; similar across cast),
  throwing and left/right pektus. Earlier tests are not final visual acceptance.
  - [ ] REFINE-2.9a motion reference research and every-character/state coverage list.
  - [ ] REFINE-2.9b Sean walking/arm clearance, weight and carrying blends first.
  - [ ] REFINE-2.9c each other character walking, inspected and fitted individually.
  - [ ] REFINE-2.9d run/strafe/start/stop/turn and locomotion transitions separately.
  - [ ] REFINE-2.9e ordinary throw anticipation/release/follow-through/recovery.
  - [ ] REFINE-2.9f left pektus and right pektus, distinct real release/flight intent.
  - [ ] REFINE-2.9g cancel/interruption/pickup and remaining verbs/hero performances.
- [ ] **REFINE-2.10 Integrated qualification.** One coherent candidate with specific
  evidence/limits; no blanket completion from screenshots or object-spawn tests.

### VISUAL-1 · Visual communication and appeal pass ⚠️ OPEN, 2026-09-23

Design, reasoning and file-level routes:
[NATIONALS_POLISH.md, Visual communication and appeal pass](NATIONALS_POLISH.md#visual-communication-and-appeal-pass-visual-1-2026-09-23).
Each child ends with a same-camera before/after pair plus a 25 percent greyscale
thumbnail (V4). Every new look lever has an "off" value that reproduces today's look.
Human taste approval is separate and never blocks the next child.
**Research and rules (owner, 2026-09-23: "i really dont wanna have to communicate by using
text"):** every finding, source, plan and idea for this pass is recorded in
[reports/visual-research-2026-09-23/findings.md](reports/visual-research-2026-09-23/findings.md)
(Sepak U, Knockout City, Rocket League, Valorant, Overwatch, TF2, the VALORANT shading article,
Riot's VFX principles, Peacocke et al. 2018, Fagerholt and Lorentzon 2009, Hodent's heuristics,
the Apex ping system). The step-by-step build plan with files, traps and acceptance checks is
[reports/visual-research-2026-09-23/implementation-plan.md](reports/visual-research-2026-09-23/implementation-plan.md). Standing rules from it: no sentences in ordinary play, a
state is a shape, colour and place; world first, then screen edge, then HUD; one card and
badge family for every in-match surface; timers as rings; the HUD recedes when effects peak.
- [ ] **VISUAL-1.0 Research record kept current.** Append each new reference and decision to
  the findings file in the same commit as the work it informs.

Batch A, communication:
- [x] **VISUAL-1.1 Danger made visible.** Implementation and focused qualification DONE.
  Existing HUD danger frame unchanged. World square follows `Balance.ConfinementRadius`
  and all five map surfaces, with rest/armed/restore sweep; local nearest exit;
  physical escape dust/swish; quiet filtered air. Per-camera restoration prevents
  preview/replay leakage. Shared boundary and escape replay use recorded state.
  Authorable `WorldCueProfile` supplies independent off values. Same-camera normal,
  comfort and25percent-grey evidence personally inspected in
  [look-1.1-world](reports/visual-research-2026-09-23/look-1.1-world/report.md).
  Final changed checks2/2, five-map geometry1/1; earlier replay/audio check retained.
  Muted stranger-readability and human listening acceptance remain P7 external review,
  not a claimed result. No gameplay rule, collision or completed HUD changes.
- [x] **VISUAL-1.2 The taya's view and shared restore clock.** Implementation and
  focused qualification DONE. Camera-only Defense-blue upper rim and open catchable
  brackets use actual tag predicates and restore after nested views. Existing reach
  tick unchanged. Close can clock fills with actual reset progress, drains protection,
  shares presentation snapshots with peers and records both in existing clip11 state.
  Bounded distance readability for cast/can preserves their authored colours. Normal,
  comfort and25percent-grey frames personally inspected in
  [look-1.2-world](reports/visual-research-2026-09-23/look-1.2-world/report.md).
  Target checks passed v4; wire compatibility v3; strengthened actual clock v7 (1/1).
  Real peer/latency and native/human interpretation acceptance remain P7.
- [x] **VISUAL-1.3 Timers on objects.** DONE (look-batchA-v4 icon sheet): the recall ring
  drains in gold through the fetch warning, turns solid Offense orange while the penalty
  runs and drains in the owner's seat colour during a roof or lagoon return
  (`SlipperRecallMark.Timer`); the two prompt sentences are gone and the recall mark's own
  edge chevron was already the off-screen cue. Original scope: own-slipper marker ring for fetch grace, penalty
  and roof or lagoon return; an edge chevron when your slipper is off-screen. No timer
  text in ordinary play.
- [x] **VISUAL-1.6 Reticle as the personal-state hub.** DONE (look-batchA-v4
  reticle-states): `HudReticle` draws a dot and ticks, a charge ring from the 0.35 floor, a
  pektus arc on the curve's side, a thin cooldown sweep for throw, shove, lunge and tag, a
  grey refused state while the can is protected and (1.2's HUD part) a blue reach tick for
  the taya from `Combat.InCone`. Charge text and timed status lines are gone. Original scope: a drawn reticle with charge ring,
  pektus tick, cooldown sweep and refusal state, replacing the "+" glyph and the charge
  and pektus sentences.
- [x] **VISUAL-1.4 Minimalist in-game HUD.** DONE. HUD 120 plus High contrast captured at
  1920x1080, 1600x680 and 960x540 (look-batchA-v4); chip badges now turn white on the
  contrast plate. Area with the drawn reticle: Classic 4.15, Hero Strike 5.71 percent. First slice DONE at `4d85395c` (match bar, can
  glyph, taya-coloured round pips, stamina arc, prompt pill, powers in the lower right, 28-unit
  floor; before and after in `reports/visual-research-2026-09-23/look-1.4-v2`). Second slice
  DONE (look-1.4-v3): pictogram feed (portrait, Knock/Restore/Tag/Block glyph, portrait on dark
  plates, words kept only as `Entry()` records), "+N" pops rising into the scorer's chip instead
  of centre score toasts, a drawn hit mark, the toast on the halftime brush under the bar,
  "TAGGED" instead of a sentence, the owner's 1600x680 window in every HUD capture. Measured
  permanent HUD: Classic 3.72 percent and Hero Strike 5.30 percent of 1920x1080 (budget 8,
  asserted by `TumpNativeHudTests`). Version label confirmed hidden; sandbox line only offline
  in warm-up or practice. Remaining: HUD 120 and High contrast captures. Original scope:
  centred top bar of four player chips around
  the clock with state and role badges, pips for rounds and one can glyph; a compact
  bottom-centre kit with corner keycaps and a contextual stamina arc; a pictogram feed;
  no sentences in ordinary play; one plate style; permanent HUD under about 8 percent of
  the frame at 1920x1080. Replaces the four score slabs and duplicate can text. Checked at
  1920x1080, 1280x720 and the owner's short wide window with every accessibility setting.
- [x] **VISUAL-1.5 One signal language.** Implementation/comparisons DONE.
  Completed HUD glyphs, recede and tint priority retained. The supposed remaining
  world LATA DOWN sentence was already absent; actual event test confirms it.
  V2 owner comments added. Existing grade supports separate off controls for quiet
  peripheral sprint/dash strokes, audible-footstep bearing pips and world-only
  ultimate desaturation preserving cast/can/prop colour and scene occlusion.
  All experiments default OFF until the owner's later default choice.
  [look-1.5-experiments](reports/visual-research-2026-09-23/look-1.5-experiments/report.md):
  v2 behavior3/3, v3 corrected personal captures2/2; normal/25percent-grey reviewed.
  External taste/default selection and integrated performance/native review remain P7.
- [x] **VISUAL-1.7 FPP viewmodel framing.** Implementation/focused qualification DONE.
  Fixed95degree apparent lens independent of world75..110, lower/smaller rest,
  lit Toon arms and separate held-slipper rim; camera scopes cover attached effects
  and spectator copies. Existing charge/release timing unchanged, with a short
  preparation pulse and lower-screen follow-through past centre/settle. Off and
  reduced-motion routes retained; physics/rig/model assets untouched.
  [look-1.7-viewmodel](reports/visual-research-2026-09-23/look-1.7-viewmodel/report.md):
  v1 behavior2/2, v5 actual release1/1; normal/comfort/grey25 and before/after
  release sequences personally inspected. Integrated native/all-cast acceptance P7.

Batch B, the world:
- [x] **VISUAL-1.8 Toon lighting.** Implementation/focused qualification DONE.
  Authorable per-map two-band ramp, lower tinted ambient, upper rim/body foot gradient,
  cap-only can metal accent, contact blobs/lower-wall grounding and nearer horizon fog.
  Runtime off restores original scene settings; previews keep independent shader scope.
  Raw HDR diagnostic1.95:1 before,2.39:1 selected look; Built-in/geometry unchanged.
- [x] **VISUAL-1.9 Court and ground as the stage.** DONE. Reversible contrasting
  chalk/charcoal, static grain, scuffed home area and5medium-specific floor-wear modes.
  Multiplicative overlays preserve real lighting/shadows and omit unsupported cells.
  No collision or route changes; original meshes/material assets retained.
- [x] **VISUAL-1.10 Hero objects.** DONE. Can contrast/caps/ground support replace the
  permanent red rim under the new look. A dotted footprint follows real toppling or
  elevated authoritative poses; normal knocks have no fabricated ballistic flight.
  Short ordinary ink trails use actual ThrowerSlot and retain their look in replay.
  [look-batchB-world](reports/visual-research-2026-09-23/look-batchB-world/report.md)
  contains all5normal/comfort/grey25 comparisons, inspected can/stroke frames and
  focused receipts. Integrated native/performance/peer/human acceptance remains P7.

Batch C, effects and motion:
- [x] **VISUAL-1.11 Effects in the ink language.** Two-tone ink shapes, erosion
  dissipation, chalk dust and puffs, confetti rebuilt as fluttering paper outside the
  camera's central cone, `CanContactAccent` and slipper trails off `Sprites/Default`.
  Ink/paper/contact-dust slice implemented and focused1/1 passed; first look retained.
  [Evidence](reports/visual-research-2026-09-23/look-batchC-ink/report.md).
  Chalk-line skid sampling is integrated through real motion and saved cue timing;
  native qualification remains P7.
- [x] **VISUAL-1.12 The exchange as one performance.** Check PRESENTATION-1 against the
  V3 beat sheet for knockdown, tag, block, escape and failure cases; confirm remote charge
  and lunge windups read at 8 to 12 m and strengthen poses before adding markers.
  Implemented: remote lunge preparation was missing and now uses a compatible
  presentation suffix; body counterbalance, expiry/stop, no gameplay mutation.
  [Focused1/1 and8/12m views](reports/visual-research-2026-09-23/look-batchC-exchange/report.md).
  Live peer/readability acceptance remains P7; stills are not that acceptance.
- [x] **VISUAL-1.13 Round rhythm.** Round-start role swap beat, round-end settle,
  chalk accents on the compact halftime popup. Existing role ring now gives one
  bounded settle on a real role change, removed by reduced motion. Round arm/rest
  and settle verified in the exchange case; completed HUD/popup/results preserved.

Batch D, with existing scope:
- [x] **VISUAL-1.14 Environment appeal and life.** Building value structure, window
  glass, roof edges, landmarks, banderitas where they fit, event reactions. Tracks with
  PRESENTATION-1.5 and 152.4. Implemented sky-gradient glazing, wall/roof value
  treatment and supported peripheral cloth in Bayan/Eskinita; retained existing
  distinct map motion/reactions. Five-map focused1/1 passed; normal/comfort/grey25
  inspected. [Evidence and limits](reports/visual-research-2026-09-23/look-batchD-world/report.md).
- [x] **VISUAL-1.15 Spectator, ultimates and audio.** Framing hysteresis, complete
  seven-hero ultimates through the shared phase, one audio peak at a time. Tracks with
  PRESENTATION-3, PRESENTATION-4 and PRESENTATION-5.2. Existing committed shot
  and seven-hero shared-phase paths retained; overlapping bass bodies now leave
  headroom while every attack remains audible. Rubber-step DC corrected without
  changing contact timing. Focused audio1/1 passed; live listening/device/normal-speed
  qualification stays P7. This is not human approval or a final build claim.

In-match UI revamp (owner addition, 2026-09-23), in batch A alongside 1.4:
- [x] **VISUAL-1.16 Match-end board.** DONE (look-1.16-v1): the court stays visible under a
  62 percent warm plate (still the click blocker), a brush winner line, the standings as the
  bar's chips grown (seat-colour portrait, crown and gold border for the winner, gold
  underline for you) with two record-backed accolades each as glyph and number
  (`MatchResult.Accolades`: knockdowns, catches, close retrievals, sabotages, retrievals;
  zero is never drawn), a gold REMATCH and quiet NEXT MAP and MAIN MENU. Every route, tab
  and name kept; `TumpNativeResultTests` 5 of 5. Owner-podium with models on the court is
  left to the world agent. Original scope: rework the result screen in the same visual
  language as the new top bar and the halftime popup: a clear winner moment, the four
  chips ranked with scores that count only after the final value is shown, per-player
  highlights from the existing recognition facts, and one obvious next action. Keep the
  existing flow, rematch and exit routes, ranked readouts and all three input devices.
- [x] **VISUAL-1.17 Mid-round and halftime reports.** DONE (look-batchA-v4 CourtBreak): the
  popup moved to the upper third in the match bar's family (brush headline, next taya
  ticket with the taya border and can badge, a draining return ring, standings as cream
  chips with crown and your underline); every label is 28 units or more, so the NextRole
  floor fault is fixed and `TumpNativeHudTests` is 8 of 8 with the exchange and icon tests.
  Original scope: the compact halftime popup and any
  round-end summary share one card style with the match-end board: standings as chips,
  next taya as the one highlighted fact, the court visible behind. No full-screen board.
- [x] **VISUAL-1.18 In-match icon set.** DONE (look-batchA-v4 HudIconSheet): power icons in
  the deck draw at one stroke weight with round ends and a black keel
  (`TumpAbilitySymbol.HudStyle`), `HudBadge` holds every state and event glyph, and the can
  glyph replaces `TumpSymbol` in the bar and the off-screen marker. The drawings (each
  ability's job) are unchanged; the front end keeps its thin line. Original scope: one consistent family for ability tiles, state
  badges, the can glyph, role badges, feed pictograms and prompts: one stroke weight,
  one corner treatment, black outlines, readable at the smallest HUD size. Replace
  placeholder project-generated icons (AGENTS: swappable, not approved art); never
  repaint supplied artwork. Every ability keeps its `AbilityGlyph` job (VISION § 3).

### UX-1 · Owner front-end flow and real progression systems, OPEN2026-09-23

**Latest correction overrides the attached brief:** DO NOT TOUCH LOGIN AND MAIN
MENU. Both existing surfaces/art stay intact. New HOME opens when the existing
main-menu TAP TO START action is pressed; rewire only the destination needed for
that flow. Do not replace main menu or automatically bypass it after login.
This adds to the queue without deleting or restarting VISUAL-1. The completed
match HUD, halftime popup and match-end board are outside this front-end lane.

Full supplied requirements: [verbatim brief](reports/front-end-flow-2026-09-23/owner-brief.txt),
[correction and intake](reports/front-end-flow-2026-09-23/intake.md). Original flow,
custom-game, profile-door and final Home sketches plus all seven zip images are
preserved under `ArtSource/front-end-flow-20260923/` with provenance hashes.
The UX is prescribed; design the visuals in TUMP's playful street-game identity,
logo palette, Darumadrop/supporting face and purposeful motion. No generic white
or pale styling or blue/navy UI chrome; Defense blue remains a rule cue. Preserve
supplied final artwork. Use actual engine renders for cast/map placeholders.

**Lane, 2026-09-23:** UX-1 is being implemented on a separate machine in parallel with the
VISUAL-1 batch D lane. The design, routes, per-screen four answers, economy rules and
verification plan are in [ux1-plan.md](reports/front-end-flow-2026-09-23/ux1-plan.md).
Key decision: HOME is the view of the `MatchSetup` scene over the live court, so every
existing networking path stays in `ConvertedMatchSetup`; TAP TO START and match exits land there.

**Status 2026-09-23 (UX-1 lane):** every screen and system below is implemented in
`Runtime/UI/Hub/` (map in ux1-plan.md § 7b) and walked by `HubFlowTests` through its
real buttons with captures at 960x540, 1280x720, 1920x1080, 1280x960 and 1600x680. An
item is ticked only when its screen passed that fixture and its captures were looked at.
`wallet.js` is now published as version1 in the existing production project; live
source and all3parameters verified against local. Rules pass the existing node
contract. Runtime service actions remain unchecked; deployment receipt is in
[completion/wallet-deployment.json](reports/front-end-flow-2026-09-23/completion/wallet-deployment.json).
Restore path (the § 68.3 keep-the-old-chrome rule): launching with `-tp-preparation-board`
sets `ConvertedMatchSetup.HubEnabled = false` and the retired preparation board is the view
again. It is also how the old fixtures are run against the view they were written for.

Implementation order within UX-1:
- [x] **UX-1.0 Plan and route/data audit.** Done in ux1-plan.md (routes, four answers per
  screen, data ownership, saves/IDs, three devices). Read CLAUDE4a,6.2-6.5 and
  Front_End_Design. Answer the four screen-design questions per surface. Map every
  old feature to one visible destination and preserve saves/IDs and three devices.
- [x] **UX-1.1 HOME hub after TAP TO START.** DONE: `HubHome`, walked by `HubFlowTests` and
  `HomeFlowTests.TapToStartOpensHomeWhoseDoorsReachProfileAndLoadout`, captured at five shapes. Top-left square avatar opens picture
  view/change; adjacent level/name/#tag/XP plate opens Profile Settings. Skill Tree
  card and notification dot below. Left HERO, LOADOUT, larger SHOP with TASK beside
  it. Top-centre elapsed queue/cancel-X only while queued. Top-right currency/+ and
  hamburger. Bottom-right selected map/mode card above PLAY. Reserve a clean
  full-bleed animated-scene layer; use a still/current court now, not a new animated
  background project. Login and main menu remain intact.
- [ ] **UX-1.2 Mode and match-entry flow.** Built and walked offline (GAMEMODE SELECT, both
  popups, PRACTICE, the queue plate and X, MATCH FOUND, timed CHARACTER SELECT). Open: a
  real two-to-four-peer queue pop through MATCH FOUND into a match, which needs UGS Relay. Mode card opens four-card GAMEMODE SELECT:
  small stacked Practice/Custom, tall Classic/Ranked, descriptions on hover/focus,
  top-right currency/menu. Practice enters practice directly. Custom opens HOST/JOIN
  popup. Ranked is Hero Strike and returns Home. Classic asks Classic/Hero Strike
  casual in a popup and returns Home. PLAY queues, then MATCH FOUND, then character
  select for everyone, then loading. Keep real queue cancellation and pick rules.
- [x] **UX-1.3 HERO screen.** DONE: `HubHero` with the real kit, STORY door, UNLOCK/PLAY AS. Large illustration/model left, previous/next/back;
  role/name, actual-kit clickable ability details, biography and real UNLOCK right.
- [x] **UX-1.4 LOADOUT and ITEM POPUP.** DONE: `HubLoadout`, `HubItemPopup` (inspect, star, EQUIP/BUY). Owned/unowned tabs, currency/menu, item grid,
  equipped tag, favourite star, selected corner brackets, dim unowned silhouettes.
  Right categories TSINELAS/LATA only; skill alternatives move to Skill Tree. EQUIP
  bottom-right. Item opens popup over dimmed grid with back/name/favourite, inspectable
  3D model, fullscreen/inspect brackets and EQUIP. Remove old STATS button.
- [ ] **UX-1.5 Character select.** Built (`HubCharacterSelect`, today's pick RPC, lock-in via
  the ready tally, host start on all-locked or 30 s). Open: a multi-peer lock-in check. After match found, big name/model,3x4 portrait grid
  and SELECT using existing legal pick rules; preserve the complete roster.
  P6 review found the timed selector lost VISION3's Learn layer. Add a compact
  selected-ability readout using actual kit/variant data: icon, name, kind, one
  sentence and cooldown/ultimate charge. Keep the selection clock running while
  inspecting it; no modal that suspends host Tick, no redundant navigation tutorial.
  Implemented: all7heroes/3slots checked with normal and large/high-contrast text,
  five shapes and running timer; both960x540frames inspected. Peer lock-in remains open.
- [ ] **UX-1.6 Custom host/join.** Built; a real LAN host is opened by `HubFlowTests`. Open: a
  second process joining by code and from the LAN and online lists (`tools/net_matrix.py`).
  Immediate native code entry exposed cold LAN lookup plus concurrent online-query429;
  bounded discovery/query-spacing fix passed its actual datagram case and native
  immediate code/rejoin run. Both clients were seated and saw LOBBY; no429. Host: lobby name, defaulted map, game mode,
  public/private/friends-only visibility, LAN/Online, CREATE LOBBY; subtle selected-map
  art updates. Join sources: Dedicated Internet, Dedicated LAN, Code. Shared server
  list with name/map/player count/join and correct Online/LAN heading; code field/JOIN.
- [ ] **UX-1.7 Custom lobby and loading.** Built (`HubLobby`, `HubLoading`). Actual two-native-peer lobby entry, automatic
  intro/countdown and rematch passed; both exited0 and shared input prefs stayed intact.
  Native code reconnect into LOBBY also passed using the same isolated profile.
  Online list/Relay coverage stays separate under UX-1.6. Lobby name/back, selected-map background,
  n/4 portrait list/host mark, START GAME and character/loadout/settings doors.
  Loading uses map art/name/percentage, bottom tips, optional BH Studios mark. Preserve
  UGS Lobby/Relay,4-character codes,LAN discovery,quick/ranked,reconnect and rematch.
- [ ] **UX-1.8 Real soft currency and hero/item shop.** Built: `EconomyRules`, `wallet.js`,
  `WalletStore`, SHOP popup, UNLOCK/BUY. Deployment DONE: version1 and exact source/params verified in existing project
  production. Live wallet action checks remain open; no runtime pass inferred. SHOP is a popup exposing Hero
  and Loadout shops; Home HERO/LOADOUT open directly. Server-authoritative Cloud Code
  balances/unlocks, profile migration and graceful offline behavior. No client grants,
  real-money purchases or paid services. Keep the existing UGS project/IDs.
- [ ] **UX-1.9 Tasks, skill tree and unlocks.** Built: `HubTasks`, `HubSkillTree`, server
  claims. Deployment complete as UX-1.8; live claim/action verification remains open. Currency+ opens earning/tasks. Skill
  Tree owns actual hero alternatives and integrates XP/levels/mastery,
  HeroBuildRules/AbilityChallenges,Cloud Save/Cloud Code. Real unlock transactions;
  cosmetics never alter gameplay and Classic stays neutral. Verify declared Cloud
  Code params and UGS refused:0 when live service checks are available/authorized.
- [x] **UX-1.10 Profile and hamburger doors.** DONE: `HubAvatar`, name plate → `PlayerHub`,
  `HubMenu` (settings, party, career, match rules, learn to play, credits, title, quit). Separate avatar-picture view/change
  from name-plate Profile Settings (name,achievements,friends,etc.). Hamburger retains
  settings,party,career hub and every former feature, with one visible route each.
- [ ] **UX-1.11 Per-screen acceptance.** Five shapes, 28 floor, bounds and one-press BACK are
  asserted per screen. Retired preparation-board fixture routes migrated; complete
  High contrast + Larger text route passed at five shapes. Core629/629, requested
  EditMode36/36 and final editor checks8/8 pass. Open: full/native regression and
  physical pad/touch passes. Actual mouse/keyboard,controller focus/B and
  thumb-sized touch; live bindings,one-press Back.960x540,1280x720,1920x1080,4:3 and
  1600x680; every label28canvas units or more; High contrast/larger text. Use shared
  approved canvas/input construction. Inspect captures of default,hover,focus,locked,
  empty and error states. Update old-screen probes to reachable new routes with a
  design reason. Focused changed-behavior checks only; full/native gate remains P7.

- [ ] **UX-1.12 Complete queued match arrival (owner addition2026-09-23).** Selected
  Ranked on HOME -> PLAY -> queue elapsed timer -> MATCH FOUND -> character select ->
  map vote -> deliberate loading presentation -> short map/player introduction with
  camera panning for5..10seconds ->3,2,1,START. Remove the in-game R-to-start step for
  queued matches. Custom rooms may retain manual ready, behind a room option. Reuse
  existing ballot/load/intro/countdown authority; inspect and fill actual missing
  connections, do not replace working stages or remove prior TODO requirements.
  Exact request and implementation decisions in the UX-1 completion plan.
  Local arrival/manual/reduced-motion cases pass. Two native peers entered the lobby,
  saw arrival/automatic countdown and rematched without any diagnostic READY press.
  Both exited0; correct authentication profiles and no duplicate-sign-in errors.
  Native evidence is custom automatic rooms, not a live UGS queued-map-ballot claim.
  Build missing UI to finished quality now; keep every built/changed surface in
  [the UI inventory](reports/front-end-flow-2026-09-23/ui-authorship-inventory.md)
  for a possible later refinement review, with paths/evidence/remaining issues.

- [ ] **UX-1.13 Mode names, awaiting owner selection.** Owner requests simpler,
  meaningful replacements for Classic and Hero Strike throughout the game. Options
  revised after the owner clarified the distinction: Chill/Powers (recommended),
  Chill/Chaos, Relaxed/Powered. The first should sound relaxed, the second ability-based. Do not pick on the owner's behalf or rename before their choice.
  After selection, audit all player-facing menus/HUD/help/settings/announcements and
  current design copy; preserve existing save, analytics and wire identifiers through
  a display-name mapping. Historical quoted feedback and evidence remain intact.

- [x] **UX-1.14 Loading tips and rotating artwork (owner2026-09-23).** Tips belong
  directly on the loading screen, not behind STORIES & TIPS opening a separate
  overlay/HUD. Replace the rejected loading-street illustration with a small set of
  newly generated, cohesive game-world images; rotate about every5seconds during
  loading with reduced-motion support. Applies to boot loading and match loading;
  preserve the title/login artwork. Save prompts/provenance and add surfaces to the
  UI inventory. Do not slow fast loads solely to cycle through every image.
  Implemented in both loading owners; actual boot readiness and three-image rotation
  passed. Revised contrast/type captured and inspected. See completion evidence.
- [x] **UX-1.15 Terms reading and consent (owner2026-09-23).** Explicit narrow
  exception to login protection: improve the Terms and Conditions presentation and
  behavior, expand meaningful content, and make consent unmistakable. Unaccepted is
  an empty square; accepted is a solid-filled square, NEVER a tick/checkmark. Preserve
  actual acceptance state, keyboard/controller/touch operation and account gating.
  Draft terms against actual implemented features, not invented services or promises.
  Implemented and passed: normal/large text at five shapes, scroll, unchanged consent
  on open/Back, solid fill and actual validation. Latest arrow-only popup inspected.
  Product draft remains subject to the owner's public-release legal/contact review.

- [x] **UX-1.16 BH Studios proportions (owner2026-09-23).** Fix the stretched studio
  mark without repainting the supplied logo. The runtime copy's power-of-two import
  changed its445x370 proportions despite preserveAspect on the Image. Preserve
  original texture dimensions and alpha; check the actual loading mark and dismissal.
  Existing loading/room-flow case passed; actual five-shape loading captures checked.
  Corrected screenshot:completion/Loading-studio-proportions-960x540.png.
  Owner screenshot retained under the UX-1 completion references. Map emptiness in
  the same screenshot belongs to the later per-map context/cultural brief.

- [x] **UX-1.17 Per-hero HOME loops, picked at random (owner 2026-09-24).** "i decided to
  make diff character lobby screens and the current one will js be random", "make it
  random which one shows up". Phaister's loop (Ilalim ng Tulay at night, a three-act trick
  in one unbroken shot) is built: research, design and timetable in
  [phaister.md](reports/home-scene/phaister.md), source `ArtSource/home-scene/src/phaister/`.
  `HubSceneVideo.Pick` rolls over `HubSceneVideo.Heroes` whose clip ships and falls back
  to Zack's; `HubSceneVideoTests` covers every listed hero's files, the pick and the
  fallback, and Phaister's loop playing in the real hub. Evidence and remaining limits
  are recorded in phaister.md § 5. Next heroes (Sean, Dante, Cheska, Nemu) each need their
  own place and hero moment by the method, never a copy; at 25 to 38 MB per clip and no
  LFS, raise repo size with the owner before shipping all six.

### UI-REVIEW · Research-first UI and HUD refinement ⚠️ IN PROGRESS, 2026-09-23

Owner brief: research first, then refine the whole game's UI and HUD, with Settings as a
priority; later the same day "improve ui LOOK of all current screens", "genuinly improve all
buttons theyre so bland and ugly", "their colors are ugly", "make sure the colors all work
well tgthr with the background", "improve all skill icons" then "do a drawing for all",
"generate profile pics too", and Rafi "DOESNT LOOK GOOD ... compared to reference" and "doesnt
look like it belongs in hero cast". Research, critique and plan:
`docs/reports/ui-hud-review-2026-09-23/` (`research.md`, `critique-and-plan.md`).
This machine's checkout: `C:/Users/Matthew/dev/TumbangPreso-Unity-ASTRAReworks`.

- [x] Settings (both routes): warm grey palette, grouped sections, loud row focus (band,
  bar, accent label), keycap and pill chips, filled SAVE with UNSAVED marker, patronising
  notes cut. `ui-batch1`/`ui-batch2` 11/11 and 12/12 (HubFlow, NativeSettings, NativeHud,
  HudIconSheet); five tabs inspected at 1920x1080.
- [x] Pause card as the sticker family (RESUME primary, SETTINGS, LEAVE MATCH destructive).
- [x] Button finish for every pressable sticker (`HubShape`): cream die-cut rim, hue-shifted
  gradient, varnish, same-hue lip, hover light, breathing focus ring, primary shimmer; paper
  lettering with an ink outline on saturated and dark stickers (`HubKit.Letterpress`).
- [x] Palette: olive grounds and fills replaced; full screens on `HubStyle.Maroon`, secondary
  stickers one warm-dark family; HOME doors with coloured wells. Mock of olive/Night/maroon
  on the real HERO capture chose maroon.
- [x] Character select: contact shadow, clock plate with PICK, stacked ability text.
- [x] HOME hero door and XP rim, LOADOUT selected-item name, JOIN one-line empty state,
  keyboard ESC cap removed from BACK (pad glyph kept).
- [x] Skill icons: 31 coloured cel-shaded illustrations (`tools/build_ability_icons.py`,
  `Resources/UI/ability-icons`), drawn untinted by `TumpAbilitySymbol` and `AbilityIcons.Tint`.
  Checked in HERO, character select, skill tree and the in-match tray.
- [x] Profile pictures: 20 composed from the real roster portraits (`tools/build_avatars.py`),
  `Avatars.Ids` and a self-sizing picker grid. `ui-batch3` HubFlowTests 4/4; picker and HOME
  door inspected. `avatar_rafi` rebuilt from the v7 portrait. Saved old ids still load.
- [ ] Rafi model (his own builder only): saturated palette, voxel-stepped crest, nape hair,
  slanted eyes without brows, small hip coil and float, decluttered waist. v7 lineup, turnaround
  and head study inspected (evidence/rafi-v7-*). Owner of v7: "rafi looks weird". v8: hair frames
  the face (locks over the wrap to just above the eyes, sideburns), one clean waist (no coil,
  straight cream wrap, orange float), chunky cream soles; evidence/rafi-v8-*. His motion clips
  re-baked from the new feet (RafiMotionAuthor). Owner approval of v8 still required.
- [x] GAMEMODE posters: `Editor/ModeCardPoseAuthor.cs` renders the real models in their clips
  (1061 poses); `tools/build_mode_cards.py` composes PRACTICE, CUSTOM, CLASSIC, RANKED and the
  two choice cards; `HubCards.Art` shows a poster when one exists. Inspected at 1920x1080,
  1280x960 and 1600x680 Larger text (PRACTICE sits close to its card edge there, fitted).
  The CLASSIC/HERO STRIKE choice popup is not in the capture set yet.
- [x] Follow-up round (`ui-batch4` 11/11, `ui-batch5`/`ui-batch6` 4/4): in-match charges moved to a
  gold pip so the icon stays whole, drawn icons larger in the dials; Settings "KEEP YOUR CHANGES?"
  as three ranked slabs; JOIN source labels on two lines; HOME's mode card wears the mode poster
  anchored right (words keep the left); item popup frames the item closer; Rafi v9 chest pocket.
- [ ] Remaining critique rows once the above land: five-shape and Larger-text captures of the
  changed screens, 4:3 and 1600x680 checks, and the owner's look approval.
- Known pre-existing failures, not caused here: `OwnerAccountUsesExactArtworkTypeColoursAndWorkingTerms`
  (retired "PLAY FAIR" copy), `TitlePlayCreditsAndSettingsReturnThroughNativeViews` (retired
  title route), `RematchActuallyLoadsTheChosenArena` (rematch stays on Eskinita; gameplay lane).

### P6 supersession decisions

- **140.4:** the historical textual connection/countdown layout is not reactivated.
  Current VISUAL-1's adopted signal language and the completed HUD scope take
  precedence. Existing RTT telemetry and peer-departure notices remain. The new
  [research draft](reports/full-backlog-2026-09-21/connection-readout-plan.md) is
  explicitly not adopted; no HUD/network changes were made from it.
- **136.1:** the old shared debug-key catalogue was a proposed implementation
  approach. Current context guards already resolve the documented F1-F4 collision.
  Do not add a catalogue solely because the old proposal has no corresponding class;
  retain the one-action-per-context rule and investigate actual new clashes if found.

### Active items carried forward (status unchanged by this cleanup)

- [x] **127.3 accessibility software and targeted native completion.** FPP FOV, hold or toggle sprint and can restore,
  HUD size and larger text, high contrast, reduced particles and flashes, and
  delivered-announcer captions are implemented; the wider angular taya ring reads in
  greyscale; controls, layout, captions, native spectator v56 and the bounded native
  accessibility route v57 (15/15 stages) passed; the 134.10 reduced-effects link is
  published. The final specific follow-up was v57's wide large-HUD frame showing the injected
  match-chat line clipped to "LOCA...". The current focused check reproduced a second
  cause: `Ellipsise` ran before the row received its width. Fixed by laying out the
  visible column before fitting text; before0/1, after1/1, wide/small enlarged-HUD
  frames inspected in [chat-first-line](reports/front-end-flow-2026-09-23/completion/chat-first-line/README.md).
  The right anchor is also asserted. Native7a0ae3363plus the opt-in diagnostic passed
  both1680x720 and960x540: full line visible over the actual map, personally inspected;
  player exit0 and shared input unchanged. The focused route bypasses retired menu
  traversal and does not claim a new whole-settings run. Physical device and final
  integrated acceptance remain separately in P7.
- [ ] **Native player shutdown crash.** v57 (and one earlier recorded runner result)
  exited with 0xC0000005 after the review had passed and `CodeReloadManager destroyed`
  was logged. The verdict stands; the cause is unknown. Reproduce on the next build,
  read the crash dump if one is written, fix or name the engine-side cause.
- [ ] **Rafi B / lagoon C expansion, final integration.** Model, kit, map, v47 to v52
  evidence and the three-peer water checks are done (see the done list). Final coherent
  qualification remains in P7. Local deck sampling refinement is DONE:47deck
  renderers use filtered single-plank grain and local thin-plane outline suppression;
  original mesh/collision preserved, off restored, focused1/1 and personally inspected
  before/after/comfort/grey25 in [look-p3-deck](reports/visual-research-2026-09-23/look-p3-deck/report.md). Owner addition
  2026-09-22 (varied background islands and mountain layers with stable seeded
  construction and clear routes) is implemented and awaits final qualification.
- [ ] **Final integration:** remaining overlap, real-peer, whole-backlog disposition and
  coherent candidate qualification (P7; P6 disposition complete). Preserve all existing task IDs.
- [ ] **PRESENTATION-1 /154/155/152.4: Complete ordinary exchange.** Unchecked while 1.5's
  full listening review is open.
  - [x] PRESENTATION-1.1 throw, contact, flight, landing, retrieval, restore and chase as
    one body/FPP/VFX/SFX/UI sequence in both modes.
  - [x] PRESENTATION-1.2 starts, stops, turns, carrying, grip/cancel, slide, shove,
    punch/lunge whiff, stagger, get-up and grounded sound/contact timing.
  - [x] PRESENTATION-1.3 contextual state: own slipper, can state/protection, legal
    danger and next action, with stable identity and glyphs. (VISUAL-1.1 to 1.3 extend
    the presentation of this; the state logic stays.)
  - [x] PRESENTATION-1.4 material sound, personal confirmation, world reactions,
    score-row accents and earned graphics shared with PRESENTATION-2.
  - [ ] PRESENTATION-1.5 character/crowd/prop/atmosphere reactions on existing maps and
    the full mix review. Reactions and audio signal checks are implemented; the full
    listening review remains. VISUAL-1.14 extends it.
  - [x] PRESENTATION-1.6 normal/busy play and Low/reduced/small-view clarity.
- [x] **PRESENTATION-2 /152.4: catch and capped chains** (2.1 host bookkeeping, 2.2 capped
  rewards and milestones published through ce0e83cf, 2.3 victim catch and return).
- [x] **PRESENTATION-3 /152.4: six complete hero performances** (3.1 shared phase,
  3.2 authored performances, 3.3 live warnings and ordinary skills, 3.4 role/view and
  cleanup). Human taste and broader device review stay in 5.2.
- [x] **PRESENTATION-4 /134.20: watchability and halftime** (4.1 recorded clips, 4.2 live
  spectator shots, 4.3 halftime package and punctuation).
- [ ] **PRESENTATION-5: integrated qualification.**
  - [x] PRESENTATION-5.1 four active players, overlapping events, both modes, all views.
  - [ ] PRESENTATION-5.2 audio and muted review, ordinary-speed and short replay,
    compression, 720p/1080p/aspect, Low and comfort controls; human listening and taste,
    full human play and physical input/device/separate-machine validation.
  - [x] PRESENTATION-5.3 native/LAN late, duplicate and interrupted cases, invariants,
    costs and cleanup.

Remaining existing scope, unchanged, now scheduled in P3 and P5:
1. **152.4 / MAP_FINAL_PASS / owner map revisions:** finish remaining architecture,
   vegetation, street life, material/lighting, readable routes and Sa Bubong
   pool/edge/resident gaps from current source and evidence; keep swimming, recovery,
   laundry and map palettes; do not restart the completed foundation.
2. **151.9 / 151.19:** audit remaining alternative skills, body/FPP/contact/cancel
   continuity and graphics controls; implement unmet contracts.
3. **153 / U8 / 149.4 / 145 / 143:** remaining primary and secondary UI routes, prompt
   rebind and device-state behaviour, native preview/layout and feature-relevant
   authority/rejoin gaps. Hardware, external services and human judgment are named checks.
4. **152 final delivery:** qualify changed contracts, all maps and roster routes, native
   controls, representative full matches and real peers on one Windows candidate.

### Done in the 2026-09-21 and 2026-09-22 passes

Kept here so their pointers still land. Full wording is in
[archive/TODO_queue_2026-09-22.md](archive/TODO_queue_2026-09-22.md) and the named reports.
- [x] Black in-game UI outlines (`reports/full-backlog-2026-09-21/black-ui-outlines.md`).
- [x] 152.4 complete environment surface pass: 2686 renderers, 19 construction families,
  native v39.
- [x] 152.4 animated sky, and Inday plain arms (environment v37; the preview-to-match
  lifecycle defect found during it was repaired with a failed-before/passed-after check).
- [x] Rafi blocky hair with no eyebrows (`rafi-block-hair.md`); Rafi rebuilt through the
  copied builder (`rafi-parts-and-motion.md`); lagoon refinement with 8 connected and
  10 detached stilt homes, boats, residents and islands (`BADJAO_EXPANSION.md`, v47 to v49).
- [x] The quality and verification rule preserved in AGENTS.md and the ledger.
- [x] Final-integration recall renderer checks in both modes (v49c, five peers).
- [x] Compact animated halftime popup over the visible court replacing the green board.
- [x] Sepak U and Blue Lock research merged into NATIONALS_POLISH (2026-09-21).

**Still owed from those passes and not hidden:** 1.5's listening review and 5.2's human
listening, taste, play and physical device checks; the owner's full-size valid-mark
export and the Google OAuth client id (`TUMP_GOOGLE_CLIENT_ID` absent, checked
2026-09-22); live UGS deployment of the Rafi mastery source. Retired from automatic
pick-next and kept with their fixes: unchanged C2 lunge-ratio and C3 registry work and old
spectator, manual-control and menu rechecks, reopened only for a relevant regression.
C1's exact idle attribution stays historical and unresolved.

**Ownership:** owner handback 2026-09-21, "get everything done u are the only agent
working on this". Necessary C4 network integration belongs to this queue; its report,
tests and unfinished 149.4 checks are preserved. No delegation or cross-chat work.
**Map feedback still binding:** blank or poorly textured buildings and empty skies need
construction-specific material and sky refinement on every map as part of 152.4, never
one texture or noise stamped everywhere.

## Open backlog index

One row per numbered entry whose body lives in [TODO_Backlog.md](TODO_Backlog.md), in the
order they appear there. The disposition column follows the cumulative review recorded in
`reports/full-backlog-2026-09-21/todo-disposition.json`; all 366 headings now have
source/design dispositions. Remaining implementation and qualification are explicit
in those dispositions and the current queue. The heading's own status word is
history, not proof of what is missing.

| § | Entry, heading as written | Reviewed disposition |
|---|---|---|
| [§ 155](TODO_Backlog.md#s155) | THE RECALL BEAM, DRAWN BY A SHADER RATHER THAN BUILT OUT OF TUBES ⚠️ IN PROGRESS, 2026-09-20, branch `feature/slipper-beam-shader` | Shader recall beam implemented and qualified in actual five-peer Classic and Hero v49c runs; human art acceptance remains separate. |
| [§ 154](TODO_Backlog.md#s154) | THE RECALL MARK, AND OWNERSHIP BECOMES A LOCK ⚠️ IN PROGRESS, 2026-09-19, branch `ASTRAReworks` | Own-slipper lock and recall marker implemented; binding/device and actual peer rendering evidence retained. Human match-feel judgment is not inferred. |
| <a id="153--the-title-street-and-the-login-redrawn-by-her-and-put-in-motion"></a>[§ 153](TODO_Backlog.md#s153) | THE TITLE STREET AND THE LOGIN, REDRAWN BY HER AND PUT IN MOTION ⚠️ IN PROGRESS, 2026-09-18, branch `ASTRAReworks` | Painted title/login and later menu routes are implemented. Whole-source final qualification remains active; original full-size valid-mark export and OAuth configuration are external dependencies. |
| [§ 152](TODO_Backlog.md#s152) | Coherent game improvement and release verification: IN PROGRESS, 2026-09-09 | Current expanded implementation is integrated across game feel, maps, characters and painted UI. Whole-file disposition and coherent final qualification are still active; historical UI exclusion and checkpoint stop rules are superseded. |
| [§ 142](TODO_Backlog.md#s142) | CONTROLLER SUPPORT: A PICTURE OF THE PAD, A PAD THAT CAN LEAVE A SCREEN, AND AN UNRECOGNISED PAD THAT WORKS ⚠️⚠️ OPEN, 2026-09-04, merged to `main` | Controller software paths are implemented and retain current native/synthetic-device evidence. Physical unknown-pad certification and owner art acceptance remain external; no backend replacement is required. |
| [§ 151](TODO_Backlog.md#s151) | THE NATIONALS FUN PASS: EARS AT THE PLAYER, AN IMPACT FRAME ONE PEER GOT, AND A SLIDE NOBODY COULD SEE OR HEAR ⚠️ IN PROGRESS, 2026-09-06, branch `main` | All specified listener/feedback/slide/cue implementation and later authored hero presentation are retained. Final coherent candidate qualification remains active; human audio/feel acceptance is unverified. |
| [§ 149](TODO_Backlog.md#s149) | THE FRESH-AUDIT FOLLOW-UP: MOVEMENT BUDGET, RE-ADMISSION, AND THE ONE-SHOT REQUESTS ⚠️⚠️ IN PROGRESS, 2026-09-05, branch `main` | Confirmed movement/admission/request/lifecycle defects are fixed with retained failed-before/passed-after evidence. Current native request/rehost and receipt-window results supersede the old unfinished audit; final coherent qualification remains pending. |
| [§ 148](TODO_Backlog.md#s148) | THE 2026-09-05 NATIONALS BATCH ✅ CLOSED, and archived in the commit that wrote it | Closed historical batch index. Its cited child requirements remain individually tracked; this index does not create a second unfinished implementation. |
| [§ 147](TODO_Backlog.md#s147) | THE GAME NOTICES ITS OWN GOOD MOMENTS AND WRITES THEM DOWN ⚠️ IN PROGRESS, 2026-09-05, branch `main` | Structured highlights, deduplication and replay-window markers are implemented; current shared highlights/replay/halftime extend this system without adding score side effects. |
| [§ 146](TODO_Backlog.md#s146) | CLASSIC'S DEPTH COMES FROM MOVEMENT: THE COMMITTED RETRIEVAL SLIDE ⚠️ IN PROGRESS, 2026-09-05, branch `main` | Dedicated slide/input/host/relay/bot implementation retained; final candidate motion and human feel remain P7/external. |
| [§ 145](TODO_Backlog.md#s145) | THE HARDENING THAT COULD STILL PRODUCE FALSE CONFIDENCE ⚠️ IN PROGRESS, 2026-09-05, branch `main` | Harness and identity implementation retained; exact Windows candidate qualification remains P7. |
| [§ 144](TODO_Backlog.md#s144) | THE TWO ACCOUNT-GATED DOWNLOADS LANDED, AND THE AUDIO GATE WAS GRADING A COPY THE GAME CANNOT LOAD ⚠️ IN PROGRESS, 2026-09-04, branch `main` | Sourcing and actual Resources audio gate implemented. Current listening/art assessment and final candidate remain separate. |
| [§ 143](TODO_Backlog.md#s143) | THE NATIONALS HARDENING PASS: A QUALIFICATION THAT CANNOT LIE ⚠️⚠️ IN PROGRESS, 2026-09-04, branch `main` | Qualification machinery implemented; coherent current-candidate regression/native evidence remains P7. |
| [§ 141](TODO_Backlog.md#s141) | SPECTATOR AND A DRIVEN SEAT WERE ON SCREEN AT THE SAME TIME, AND F1-F4 HAVE TWO READERS ⚠️⚠️ OPEN, 2026-09-04, branch `abilities-rework` | Spectator handover, input-context guard and distinguishable labels implemented; no speculative revival of the old catalogue proposal. |
| [§ 140](TODO_Backlog.md#s140) | THE PLAYER CANNOT SEE THE NETWORK, AND THE TIMEOUT GIVES THEM EIGHT BLIND SECONDS ⚠️⚠️ OPEN, 2026-09-04, branch `abilities-rework` | RTT/departure implementation retained; historical warning/countdown layout not reactivated under the newer completed-HUD scope. |
| [§ 139](TODO_Backlog.md#s139) | SETTINGS IS FOUR PAGES NOW, AND THE RENDERS FOUND THREE FAULTS THAT HAD SHIPPED FOR THE WHOLE PORT ⚠️ OPEN, 2026-09-04, branch `abilities-rework` | Implemented current five-section settings workspace and controller-map/rebinding routes supersede the former four-page converted panel. Owner visual acceptance remains separate. |
| [§ 138](TODO_Backlog.md#s138) | A CONTROLLER UNITY DOES NOT RECOGNISE IS INVISIBLE TO THIS WHOLE GAME ⚠️ OPEN, 2026-09-04, branch `abilities-rework` | All software discovery/fallback/settings paths are implemented and current hotplug/native evidence is retained. Actual unrecognised-controller vendor/product certification requires hardware. |
| [§ 134](TODO_Backlog.md#s134) | THE BROADCAST PASS: AUTOPILOT, REPLAY, ULTIMATE INTRODUCTIONS, THE SHOVE THAT MEANT NOTHING, AND THE KEYBOARD ON THE PHONE ⚠️⚠️ OPEN, 2026-09-04, branch `abilities-rework` | Current broadcast/touch systems and later evidence retained. Old spectator-letter layout superseded; device/final limits remain explicit. |
| [§ 133](TODO_Backlog.md#s133) | ONE FONT IS DOING EVERY JOB, AND IT IS A DISPLAY FACE ⚠️⚠️ OPEN, 2026-09-03, NEXT SESSION'S BRIEF | Old font/composition plan superseded by current owner-painted, VISUAL-1 and UX-1 designs. Surviving usability checks stay in UX-1. |
| [§ 132](TODO_Backlog.md#s132) | The loadout said nothing about the hero, and a build vanished the moment the match started ⚠️ IN PROGRESS, 2026-09-03, branch `abilities-rework` | Current HERO/SKILL TREE/LOADOUT/selector routes replace the combined board. Later variant evidence retained; no old layout or blanket font sweep. |
| [§ 131](TODO_Backlog.md#s131) | Replace Hero Strike VFX and synthesised SFX from the licensed source list ⚠️⚠️ IN PROGRESS, 2026-09-03, branch `abilities-rework` | Licensed VFX/SFX sourcing and later authored presentation implemented; current listening/overlap acceptance remains mapped. |
| [§ 130](TODO_Backlog.md#s130) | Crossplay, the boot ANR, and the lobby that was drawn in a different language ⚠️⚠️ OPEN, 2026-09-03, branch `ui-redesign` | Preview parity and boot fixes implemented; old layouts superseded by UX-1. Physical crossplay/handset acceptance remains P7. |
| [§ 128](TODO_Backlog.md#s128) | Phases 11 and 12 are almost entirely built, and this entry was wrong about it once ⚠️ OPEN, 2026-09-03, branch `ui-redesign` | Former missing format implementation is superseded by implemented LastTsinelasDirector and network map ballot; final format integration remains in the retained qualification gate. |
| [§ 127](TODO_Backlog.md#s127) | Phase 16.1: the taya is a RING and an attacker is a DISC ⚠️⚠️ OPEN, 2026-09-03, branch `ui-redesign` | Accessibility and role readability implemented; current chat-clip/device/final checks remain in127.3. |
| [§ 88](TODO_Backlog.md#s88) | Accounts and identity ⚠️ IN PROGRESS 2026-08-31 | Account/project/service setup implemented; current isolated startup fix checked. Historical relink blockers superseded. |
| [§ 89](TODO_Backlog.md#s89) | The profile, the stats and the match history ⚠️ IN PROGRESS 2026-08-30 | Career/history implemented; old FUTURE phase allocations retired. Current PlayerHub and P7 govern acceptance. |
| [§ 126](TODO_Backlog.md#s126) | The full PlayMode suite had never been run on this commit, and it was 42 red ⚠️⚠️ 2026-09-03, branch `ui-redesign` | Historical failures resolved or superseded; current full regression and physical-device limits remain P7. |
| [§ 121](TODO_Backlog.md#s121) | The v61 report: one material for the primaries, a hub with a tab column, and the stuck hover ⚠️⚠️ OPEN, 2026-09-02, branch `ui-redesign` | Historical layout/palette recipes superseded by current UX-1/VISUAL-1. Surviving functions and current acceptance remain mapped in UX-1. |
| [§ 119](TODO_Backlog.md#s119) | The whole front end is repainted in PAPER, and the lobby is rebuilt around the room ⚠️⚠️ OPEN, 2026-09-01, branch `ui-redesign` | Historical layout/palette recipes superseded by current UX-1/VISUAL-1. Surviving functions and current acceptance remain mapped in UX-1. |
| [§ 118](TODO_Backlog.md#s118) | The lobby is coherent now and it is not finished ⚠️⚠️ OPEN, 2026-09-01, branch `ui-redesign` | Historical layout/palette recipes superseded by current UX-1/VISUAL-1. Surviving functions and current acceptance remain mapped in UX-1. |
| [§ 96](TODO_Backlog.md#s96) | OPEN: he has never found the way into the hub ⚠️⚠️ | superseded by the implemented lobby player-card/profile door; native UI route qualified, user discoverability judgment remains |
| [§ 95b](TODO_Backlog.md#s95b) | OPEN: nothing asserts that a menu label fits, only that it is legible ⚠️ | Current capture gates assert label fit/rendered characters and bounds. Whole current surface acceptance remains UX-1.11/P7. |
| [§ 72](TODO_Backlog.md#s72) | Two lobby controls reported dead that every headless check says are alive ⚠️ OPEN | Old controls replaced by current profile/JOIN routes; migrated caret/code checks pass. Native physical typing remains P7. |
| [§ 68](TODO_Backlog.md#s68) | The lobby is a form, and it should be a room ⚠️ OPEN, PLANNED 2026-08-28 | Old PUBG lobby design superseded by owner UX-1. Native code/reconnect and arrival/rematch pass; remaining current peer checks stay P7. |
| [§ 69](TODO_Backlog.md#s69) | The game has no chat, in the lobby or in a match ⚠️ OPEN, PLANNED 2026-08-28 | Chat implemented with host limits and input isolation. Current both-direction native chat and 127.3 clip follow-up remain P7. |

<a id="earlier-execution-index-and-supporting-reasoning"></a>
Earlier startup notes (September 15 demo queue, reserved lanes, the old execution index and
"how this file stays short") were already pointer stubs to
`reports/presentation-pass-2026-09-21/planning-intake/TODO-startup-history.md`; the stubs
now sit at the top of [TODO_Backlog.md](TODO_Backlog.md).

## The archive index

- **152.7, CLOSED 2026-09-12:** owner-view spatial composition and retrieval-route batch, with original Bayan passenger tricycle and repeatable authoring. Full entry in [TODO_Archive.md](TODO_Archive.md); complete maps/graphics/new-map/game scope remains under152.4.
- **152.6, CLOSED 2026-09-12:** isolated under-guideway flight/direct-carrier release correction and saved-map semantic repeatability. Full entry in [TODO_Archive.md](TODO_Archive.md); broader map/game qualification remains under152.4.

- **147.3, CLOSED 2026-09-10:** one observed result-board moment, without score changes; reader and clearing verified. Whole entry in [TODO_Archive.md](TODO_Archive.md).

- **149.7, CLOSED 2026-09-10:** duplicate protocol assertion removed; the exact compiled owner remains in ChatAndLobbyChromeTests. Whole review in [TODO_Archive.md](TODO_Archive.md).

- **151.6, CLOSED 2026-09-10:** Trip hazards now have their own Balance.ConfinementRadius clearance, separate from the unchanged 1.4 m generic-prop clearance. Current Checks.RunAll reports all eight checks passed. The first patch accidentally changed the generic-prop loop; the geometry gate caught it and that loop was corrected before this checkpoint. Whole entry in [TODO_Archive.md](TODO_Archive.md).

- **151.21, CLOSED 2026-09-10:** PersonSwapProbe now tests face ink, facing and head-weighted dye on the named Zack reference while the naked base retains rig, clip, height, palette and hand-anchor checks. The current probe reports RESULT: PASS in Logs/person-swap-finish-v2.log. Face/hair assertions were retained. Whole entry in [TODO_Archive.md](TODO_Archive.md).

- **151.15, CLOSED 2026-09-10:** Added audit_positional_audio.py: all 21 direct positional producers carry explicit ownership/reach classifications, with new or stale sites rejected. Private motor confirmations are local 2D cues; jump/land use NetCue. Fourteen gating source audits pass. Live network verification remains part of section 152.4. Whole entry in [TODO_Archive.md](TODO_Archive.md).

- **151.18, CLOSED 2026-09-10:** Removed the two unregistered person assets and made RosterBookBuilder reject new unregistered person filenames. All twenty intended roster entries remain, including the inaccessible maker rigs. Historical root names were preserved. Roster rebuild and exact arm-geometry tests pass. Whole entry in [TODO_Archive.md](TODO_Archive.md).

- **151.23, dedicated first-person retrieval slide, CLOSED 2026-09-09:** own low
  reach and recovery, authoritative transition into carry, versioned action evidence
  and focused successful/failed tests in [TODO_Archive.md](TODO_Archive.md).
- **151.22, Sean retrieval-slide strip review, CLOSED 2026-09-09:** kept unchanged;
  five judgments, corrected hand-height record and evidence limits in
  [TODO_Archive.md](TODO_Archive.md). Full task 3 remains open for match review.

One row per section that now lives in [`TODO_Archive.md`](TODO_Archive.md). Same numbers,
whole bodies, nothing deleted. **This table exists so that a pointer written anywhere in the
repository still lands on something**: follow it here, find the number, read it there.

| § | What it was |
|---|---|
| 150 | The camera/feel/lifecycle pass: a hitstop that drifted 11.9 m, a bearing nobody passed, and an audit blind to its own worst case ✅ CLOSED 2026-09-06 by § 151. ⚠️ Its four open halves each have a successor: § 150.7's listener is § 151.1 and § 151.2, the jeepney is § 151.5, the sweep is § 151.9 and the maximum-effects measurement is § 151.8 |
| 93 | A held tsinelas "drifted" 0.084 m from the hand ✅ CLOSED 2026-09-05. ⚠️ It was never a carry regression: `CarryTests` subtracted `RestHeight` and not the `DrawnCentreOffset` that `RideAnchor` also applies, so it measured half a shoe and called it slack. The bound is unchanged at 0.05 m |
| 137 | The two-process harness § 135.7 said did not exist, and the tables it was blocking ✅ CLOSED 2026-09-04. ⚠️ Read § 137.2 before reaching for `UnityTransport`'s simulator: it is `[Obsolete]` with no effect here. Closes § 135.6, § 135.7's buildable half, § 136.4 and § 134.9 |
| 136 | F1 did three things at once in practice, and the whole `ui_*` sound family went back ✅ CLOSED 2026-09-04. § 136.4's touch control is built in § 137 |
| 135 | The tournament network pass: the baseline, and the three verbs that refuse in silence ✅ CLOSED 2026-09-04. ⚠️ § 135.7's premise about the harness is corrected in § 137.1; its two HUMAN-blocked parts are in `Attention.md`, not here |
| 131 | The suite became a gate, the tutorial got its glyphs, and a red that was never about steering ✅ CLOSED 2026-09-03 |
| 129 | Three faults off the first phone render, and the one that was invisible on a monitor ✅ CLOSED 2026-09-03. § 129.3's mechanism is § 130.9 |
| 90 | The impersonation guard, and telemetry ⚠️ 2026-08-30 |
| 91 | Phase 4: XP, levels and hero mastery ⚠️⚠️ 2026-08-30 |
| 92 | The account and career screens, rebuilt ⚠️⚠️ 2026-08-30 |
| 94 | Phase 4.5: quality control across phases 1 to 4 ⚠️⚠️ 2026-08-30 |
| 125 | Controller, touch and crossplay, built so that forgetting is impossible ⚠️⚠️ 2026-09-02, branch `ui-redesign` |
| 124 | The skills are aimed and drawn in their own hand, the tutorial stopped lying, and Zack stopped being Sean ⚠️⚠️ 2026-09-02, branch `ui-redesign` |
| 123 | The match settings go back to steppers, the shadow was retuned on the wrong axis, and a tab pair sat at half its neighbour's contrast ⚠️⚠️ 2026-09-02, branch `ui-redesign` |
| 122 | The black line everywhere, the picker goes back to wood, and the loadout moves to the hero ⚠️⚠️ 2026-09-02, branch `ui-redesign` |
| 120 | The buttons get a thickness, and the four screens § 119.11 left get finished ⚠️⚠️ 2026-09-02, branch `ui-redesign` |
| 117 | The front end was two design systems stacked, and the code-drawn one was the wrong one ⚠️⚠️ 2026-09-01, branch `ui-redesign` |
| 116 | The front end had one material and no focus state ⚠️⚠️ 2026-09-01, branch `ui-redesign` |
| 115 | Eight faults in one build, phases 11 and 12, and the door he could not find ⚠️⚠️ 2026-09-01 |
| 114 | The boot is four screens, the lobby is the home, and the colour dial is deleted ⚠️⚠️ 2026-09-01 |
| 113 | The clothes were not clothes, the screen was see-through, and the door was a chip ⚠️⚠️ 2026-09-01 |
| 112 | The base rig is naked now, and the custom character walks into a match ⚠️⚠️ 2026-08-31 |
| 111 | The build he opened: no studio mark, and the boot screen in the wrong unit space ⚠️⚠️ 2026-08-31 |
| 110 | The character maker gets a wardrobe, and the custom hero borrows a kit ⚠️⚠️ 2026-08-31 |
| 109 | Phase 6's last mile: the three-hour hang, and the presence state nothing had ever lit ⚠️⚠️ 2026-08-31 |
| 108 | The custom character had no screen, and two screens were drawn under the screen that opened them ⚠️⚠️ 2026-08-31 |
| 107 | Roster Integrity and the 3-Slot Custom Character Creator ⚠️⚠️ 2026-08-31 |
| 106 | Phases 5 and 6 finished: the free colour dial, and parties as queue tickets ⚠️⚠️ 2026-08-31 |
| 105 | Phase 9: one ladder, five tiers, Glicko-2 ⚠️⚠️ 2026-08-31 |
| 104 | Phase 8: the witnessed result, and the finding that the plan's design would have been theatre ⚠️⚠️ 2026-08-31 |
| 103 | Phase 7: QUICK MATCH as a rating-banded queue ⚠️⚠️ 2026-08-31 |
| 102 | Phase 6: friends, presence and blocking ⚠️⚠️ 2026-08-31 |
| 101 | Phase 5 continued: the banner on the wire, palettes on remote seats, and the colour picker ⚠️⚠️ 2026-08-31 |
| 100 | ⚠️⚠️ THE BOOT SCREEN'S ART WAS FITTED TO A FRAME NOBODY CAN SEE, AND THE COLUMN WAS SIZED AGAINST THE WINDOW INSTEAD OF AGAINST THE FORM |
| 99 | ⚠️⚠️ EVERY `sortingOrder` A CODE-BUILT SCREEN SET WAS SILENTLY IGNORED, AND § 92.7'S FIX NEVER WORKED |
| 98 | Phase 5 begins: the banner, and wiring the rewards nothing wore ⚠️⚠️ 2026-08-31 |
| 97 | The boot account screen, PUBG-shaped, with the guest escape ⚠️⚠️ 2026-08-31 |
| 95 | ✅ CLOSED: the four title-screen buttons overflowed their own artwork at 720p |
| 95c | CLOSED: the loading screen was a black rectangle for most of the boot |
| 71 | The 2026-08-29 report, and the two faults only a non-host could see |
| 73 | The rest of the 2026-08-29 batch: feel, audio, and the casts nobody could tell apart |
| 74 | Zack's shock trail has the hazard bug that was fixed everywhere else ✅ CLOSED 2026-08-29 |
| 75 | The slipper throw wind-up, and what was actually checked ✅ CLOSED 2026-08-29 |
| 76 | Holding the pickup key does not right the can in the tutorial ✅ CLOSED 2026-08-29 |
| 77 | The network deep-dive: the half of § 71.3 that was never applied, and a refusal that was never sent ✅ CLOSED 2026-08-29 |
| 78 | The two-machine acceptance test, run at last, and the batch it paid for |
| 79 | The 2026-08-29 evening batch: what he reported, what landed, and what is still open |
| 81 | ⚠️⚠️ THE PLAYMODE ARENA SUITE IS NOT A GATE ANY MORE, AND HERE IS THE EVIDENCE |
| 80 | The 2026-08-29 late batch, reported while § 79 was being fixed |
| 82 | The 2026-08-29 night batch: the match that was over before it started |
| 83 | The 2026-08-29 balance-and-controls batch, reported while § 82 was being pushed |
| 84 | The 2026-08-30 batch: twelve reports off the shipped build, and a lighting number read off a dead field |
| 85 | The 2026-08-30 AUDIO and VISUAL list, sent as one block |
| 86 | The spectator pause, and the 35 ms every non-host was standing behind |
| 87 | Every tsinelas rendered flat brown in first person, and the fix for it flattened the shading on all of them ✅ FIXED 2026-08-31 |
| 0 | Hero Strike is being reworked, and the plan is its own file |
| 8 | The abilities still look repetitive, and half the fix is not done |
| 9 | Ilalim ng Tulay dressing defects, reported off the 2026-08-25 player |
| 12 | Everything 🧑 found playing the 2026-08-26 build ✅ ALL CLOSED SAME DAY |
| 13 | Everything the 2026-08-26 evening build showed, and the pattern in it |
| 14 | The 4.69 player's second batch, shipped in `349b0171` |
| 15 | The 4.70 tutorial batch, and why four screenshots were one probe apart |
| 16 | The probe was never deterministic, and § 10 was closed on an argument |
| 17 | The bots are steeply sensitive to the frame step, and a 50 fps machine is in the bad band |
| 18 | HUD strings overflow their boxes, in more than one place |
| 19 | The powers were fifteen poses sharing one construction, at every layer |
| 20 | Cheska's kit played the wrong sounds, and every zone died in silence |
| 21 | Phaister merged in, and everything she arrived without |
| 22 | Everything the 4.71 player showed, and the two entries that were ticked but not wired |
| 23 | Ability stuns are now fought out of, not waited out |
| 24 | Phaister's three powers were one builder at three radii |
| 25 | Which peers actually hear a sound, measured rather than assumed |
| 26 | Every ultimate changes the weather, and each hero changes it differently |
| 27 | The other five heroes need a motif, and it is not more symbols |
| 28 | Nemu's ultimate is her pet now, and her kit is named after him |
| 29 | The other four heroes got their motif, and none of them shares a builder |
| 30 | Two findings from measuring the cue files, and one stale line in `CLAUDE.md` |
| 31 | Everything the 4.72 playtest reported, and the two faults it exposed |
| 32 | The networking was broken by one unreplicated static, and four other faults on top of it |
| 33 | The bots picked a target by seat number, aimed powers at rings they do not cast, and had no keyboard between decisions |
| 34 | Seat 0 was steered by a different movement model in every all-bots run, and it is § 11's second layer |
| 35 | The spectator flies itself, every key is in the panel, and a reconnect stops refunding cooldowns |
| 36 | The host never transmitted its own bodies, so a joiner saw three statues |
| 37 | Two Phaister presentation faults from the 4.72 player ✅ CLOSED, SEE § 43 |
| 38 | The network pass: eleven faults the host cannot see, and the loopback behind four of them |
| 39 | The settings wheel, for the fourth time, and the cause the first three missed |
| 40 | The train is one field recording now, and it plays rarely |
| 41 | The ultimate meter counts events now |
| 42 | Nemu's ride home was being erased by her own body's bot |
| 43 | Two Phaister presentation faults, and a class of fault behind one of them |
| 44 | § 32.3's slider fix was muted by the sweep on the next line ✅ CLOSED 2026-08-27 |
| 45 | The in-match HUD had five ambient sines, three copies of "LATA DOWN" and twelve coloured cells |
| 46 | Both intermission banners were drawn on top of something ✅ CLOSED 2026-08-27 |
| 47 | `Checks.RunAll` has been red since the Phaister merge, in two places |
| 48 | Kuro's projected body deleted itself mid-ability, and took Nemu's way home with it |
| 49 | Seat 0 travels about half what seats 1 to 3 do, in Classic, every run |
| 50 | Fourteen reports off the 4.73 player ✅ CLOSED 2026-08-27 |
| 51 | The four follow-ups off § 50 ✅ CLOSED 2026-08-27 |
| 52 | The ready and rematch gates counted a seat as a peer, and five guards allocated before they guarded |
| 53 | A joining client could not move, and the cause is that its keyboard was left on seat 0 |
| 54 | Which of the two lobby fixes was kept, and why |
| 55 | The lobby was a picture of a lobby ✅ CLOSED 2026-08-27 |
| 56 | What the merged network pass still leaves open |
| 57 | The match ends on one machine, and three other events never reach a client at all |
| 59 | Two machines could discover each other and could not join, and it is one missing string split |
| 60 | The host announces a seat twice, by two protocols, and only one of them does the job |
| 53 | The corner stamp is the branch name ✅ CLOSED 2026-08-27 |
| 62 | Losing the host left a client playing on alone, and § 60.1 did not fix the movement |
| 63 | A game could be joined exactly once per launch, and remote bodies never animated |
| 64 | The bots had no face, no feet, a perfect memory and one opinion |
| 65 | Hosting or joining a SECOND time in one launch was refused, silently |
| 66 | Joining bounced the view about, and rejoining a running match was impossible |
| 67 | What the HARRYDAKS merge was hiding, found by building it |
| 70 | The prop art was replaced wholesale, and IKE was never a bad model ✅ MOSTLY DONE 2026-08-28 |
| 1 | Peer rematch voting across the wire |
| 2 | Cheska's Ice Barricade duration was set by accident ✅ CLOSED 2026-08-25 |
| 3 | The five hero accents have not been seen in a real match |
| 4 | Bayan Plaza's monument stands inside the defender's box |
| 5 | The overclock window has not been measured against a match |
| 11 | Every probe number ever printed was an average over a seat that could not play ✅ CLOSED 2026-08-26 |
| 10 | `BotBehaviourProbe` cannot answer a comparison, and every open balance question is one |
| 6 | `AiDiagnosticProbe`'s Classic round is a real-time test and it flickers red |
| 7 | The test suite costs more to run than it is currently returning |
| 58 | The ink outline tore open at every hard edge ✅ FIXED 2026-08-27 |
| 63 | The world outline was aliased because MSAA was never able to see it |
| 63 | Walking into a utility pole blanks half the screen, and it now dithers away |
| 64 | The player can switch render styles, and the alternative is a chromatic look |
| 65 | The white keyline round every silhouette, measured rather than argued |
| - | Closed |

- **152.8, CLOSED2026-09-12:** preserve transparent/cutout scenery during near-camera fading. Whole entry in [TODO_Archive.md](TODO_Archive.md); [evidence](reports/improvement-2026-09-12/near-fade-alpha.md).

Latest owner ability feedback,2026-09-13: current abilities still look poor and
feel too similar. AFTER MAPS, fully revamp and improve their implementation,
mechanics/purposes and complete casting/body/FPP/moving geometry/VFX/impact/SFX.
All six heroes/defaults/alternatives, within existing slots and simple controls.
This is not a recolor pass. Existing map corrections remain first; the other
movement/equipment/network/graphics/TODO scope remains open.
# Deferred by owner, 2026-09-14

- [x] Finish Inday's FPP arms. Superseded and completed2026-09-22: both original
  Inday models and both FPP routes now use plain brown arms with simple hands,
  per the latest owner correction. Published e50f8c37; source/rig/animation audits,
  quick/held/moving casts and nativev37/v39 views pass. Original arm backups and
  earlier failed studies are retained. The following describes the old candidate,
  not unfinished work: owner explicitly moved this below other
  project work. Keep her actual restored left/right arm meshes, not the rejected
  reconstructed guards or red mittens/purple bands. Source-copy v1 passed the
  character-switch and quick/held/moving throw checks, but its sleeve ends showed
  in the carrying camera. Uniform full-reach source-copy v2 is the pending framing
  candidate and has not been visually reviewed. Current author evidence is in
  Logs/inday-source-arm-author-v2; prior motion in Logs/inday-source-arm-motion-v1.
  Retain existing animations, source proportions, material and character palette.
  Resolve framing and remove obsolete reconstruction code/assets when resumed.
