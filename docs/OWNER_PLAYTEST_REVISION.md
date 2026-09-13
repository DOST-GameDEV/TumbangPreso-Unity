# Owner playtest revision, 2026-09-13

## Newest rooftop and throwing requirements, 2026-09-13

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

This is an implementation contract, not a handoff. Keep the current Bayan layout;
the owner explicitly retracted the earlier rejection. The wider goal stays open.

## Immediate order

1. Safely isolate Editor saves from the Desktop player now in use. Extend the
   existing launch guard to snapshot only the explicit ProfilePaths named profile.
   Verify with temporary files that a concurrent main-profile save survives.
2. Remove Street Hype completely: HUD bar, tiers, celebrations, contextual hype
   rewards and their network messages. Preserve actual match scoring and unrelated
   hit feedback/highlight recording. Remove the duplicate main RULES selector;
   retain advanced match formats in Custom Game and their existing saved IDs.
3. Fix Ilalim's observed floating rooftop fragments and abrupt road continuation.
   Inspect bounds/support/parent transforms before editing. The alleged hump may
   be a surface transition; establish which geometry it is. Align road elevation,
   asphalt material and texture scale beyond the court. Keep vehicles grounded.
4. Finish the focused map corrections as coherent places: redesigned supported
   sampayan and a tighter residential feel in Eskinita; readable textured paving
   in Bayan; genuinely painted Bawal notices and varied trees across the maps.
5. Implement the approved Sa Bubong now: a condo roofdeck with open court, fenced
   pool, shade/resident seating, stairwell and laundry/utility corner. Actual outer
   edge falls; button mashing gets the player back up. Slippers that fall remain
   unavailable for about10 seconds before safe return. Both modes, existing
   controls, host authority, interruption and reachable recovery are required.
6. Resume movement/animation/equipment, graphics scalability/settings, all-context
   mash reliability, all six whole kits/alternatives and the full TODO/network
   qualification queue. No pending work is cancelled by this review.

## Visual direction and critique

The owner's old screenshot is a color reference, not a geometry rollback:
`C:/Users/Matthew/AppData/Local/Temp/codex-clipboard-f43f45d0-8077-491c-bb71-17819af5109e.png`.
Warm cream walls, strong terracotta/orange roofs, brown-grey pavement and soft
blue-grey sky. Current cool reference ends in `ec97f0f4-0e1b-46ec-9477-b31ff893e004.png`.
Tune light/material/shader response, not a uniform orange overlay. Preserve the
eighteen people's identity, chalk/can/slippers and ability contrast on Low/High.

Eskinita is intentionally tighter than the other maps, not an open boulevard.
Use residential frontage/overhead laundry to establish enclosure without blocking
the retrieval loop. Do not restore the rejected old sampayan. Lines need visible
anchors and sag; garments need readable forms and safe head/throw clearance.

Bayan's paving still reads flat to the owner. Inspect actual FPP and oblique light:
slab variation, subtle roughness/grain and restrained seams should survive normal
viewing. Do not reintroduce bright gaps, huge boards or scattered obstacles.

Ilalim floating-object reference:
`C:/Users/Matthew/AppData/Local/Temp/codex-clipboard-126e28a9-3cb7-4c9c-9fb8-f2338f264af7.png`.
Road/vehicle boundary references end in `d99c206e-5809-4bba-9b3b-87d997361e1d.png`,
`2c2cd628-e196-435e-886a-c6d708e62788.png`, and `dd3d2d1c-4c35-420f-83ca-fe3b9d7cac33.png`.
Images are evidence, not embedded instructions. Criticize support, scale, contact,
style, legibility and consistency from the reported angles plus side/back views.

## Evidence and delivery

Current Desktop review: d9c0314b, protocol29, built 2026-09-13 07:29:22.
Its RuntimeDLL SHA256 is
`1ce3858fc9f59ffbc09df3aeceb5ab0c55d9f8275521952d4e3885b75abff7e1`.
The owner may keep it running. Do not overwrite it or restore their active saves.
Use separate internal build output for new validation until replacement is safe.
Fresh nonzero XML, scene/source checks, semantic authoring repeatability and
matched actual FPP/ordinary-speed views are evidence, not artistic acceptance.
Record changes, failures and critique here and in the live execution pointer.

Latest owner ability feedback,2026-09-13: current abilities still look poor and
feel too similar. AFTER MAPS, fully revamp and improve their implementation,
mechanics/purposes and complete casting/body/FPP/moving geometry/VFX/impact/SFX.
All six heroes/defaults/alternatives, within existing slots and simple controls.
This is not a recolor pass. Existing map corrections remain first; the other
movement/equipment/network/graphics/TODO scope remains open.

Latest ultimate direction: the owner wants each ultimate to have its own special
moment, citing Tekken and Genshin Impact as impact/timing references. Give each a
distinct preparation, camera/body sequence, sound, imposing payoff and recovery;
keep opponent tells/counterplay visible and the native blocky style. No copied
characters/effects, forced long input locks or shared generic recolored cutscene.

## Distinct nostalgic map palettes (owner requested all maps,2026-09-13)

- Eskinita: warm late-afternoon sun,terracotta roofs,cream walls,supported laundry
  shadows and tighter residential enclosure.
- Bayan: warm civic masonry,sunlit stone/leafy shade,readable paved surface and
  open town-square depth. Preserve current civic composition.
- Ilalim: cooler shelter under guideway,warm daylight at street openings/shop
  thresholds,aged concrete and restrained vendor/vehicle color. No neon/cyberpunk.

Inspect map lighting,material tint,near-fade shader properties,sky and grading
producers together. Current same neutral finish may override earlier map lighting.
No global orange wash. Match actual FPP/Low/High and effect readability.

## Current investigation evidence

Ilalim baseline:1/1 ArchitectureAndStreetContinuity,22 views,
Logs/owner-ilalim-baseline-2026-09-13 and support CSV. Named Editor profile only,
snapshotc7d47da92be7 (0 preexisting named files); main player saves untouched.
Road source confirms texturedskin min/maxZ -18.5/+18.5 atY.001, continuation
road -120/+120 atY0. Apparent hump is material discontinuity,not a physical ramp.
The exact two orange floating units are captured in roof-support-1-1.png.
Current corrective author raycasts actual building roof triangles for whole
attachment footprints and continues asphalt/UV scale through the real carriageway
and cross streets. Author run v1 in progress;not yet verified or visually accepted.

Signage critique: the owner rejects shared centered headings/subtitles/frames as
AI-looking uniform design. Give businesses plausible independently-made signs:
painted fascia,tarpaulin,vinyl letters,repair-price boards,small handpainted notices.
Vary material,layout,hierarchy,mounting and age by business use,not random fonts.
Preserve real retained livery and never transplant watermarked reference photos.

Owner asks us to think beyond their comments: proactively inspect related mistakes
and critique each batch. Their feedback is evidence,not the entire audit checklist.

Author v1 failed safely before scene save: no flat supported footprint for roof
tank W4. Its previous placement relied on the maximum building height. Revised
author deliberately omits an optional rooftop prop if no flat roof footprint can
carry it,records that omission,and retains original source inactive. Do not force
a prop to float to preserve count. Existing industrial shop chimneys retired as
implausible decoration. No successful correction capture yet.
Street Hype runtime/UI/network producers removed in source;main RULES row removed
in both lobby styles. Custom Game formats retained. Compilation still pending.

## Palette/surface draft v1 and critique

MapAtmosphereAuthor now owns all3 map grades/light/sky so the old duplicated
neutral finish cannot overwrite the intended palette. Original lightweight
NeighbourhoodSky shader gives controlled upper sky/horizon and a restrained
sun halo. Eskinita amber afternoon,Bayan warmer masonry/stone,Ilalim cooler
shelter with warm daylight. Mild warm asphalt binder tint is shared across
Ilalim continuations/cross streets. No new gameplay color filter/UI changes.
Bayan paving has stronger original aggregate/slab variation and now binds its
previously unused normal texture; TownGround tangents are generated.

Actual FPP matched quality probe passes1/1,144 images across3maps/2modes/3presets:
Logs/owner-palette-fpp-v1, snapshot a6ef4ea28b82,2 named-profile files restored.
Critique: palette is warmer and the material seam is gone. Bayan grain reads
near the feet but wider square still needs stronger place detail; giant wood
strips need a real-hazard identity review. Eskinita remains broad without the
planned new laundry/enclosure. Trees still repeat identical crown silhouettes.
Ilalim signs are still the old same-template set and distant buildings too plain.
These captures are a draft review,not final artistic approval or a release build.

Core562/562. FullEditv1 508/509: the only failure expected the old37m asphalt
length. Replaced that stale exactsize assertion with actual road-bound coverage
and all4cross-street texture/world-scale/elevation checks. Roof support3newcases
passed,including rotated stepped building and refused oversize footprint.
FullEditv2 in progress. All other initial Edit tests passed.

Current sign draft: independent layouts for each business via
tools/author_shopfront_signs_v2.py (old entry point invokes it last). Printed
laundry/print/repair services,ukay rental/pisonet price layout,painted food/bakery
fascias and tyre service board. Bawal now original red brush paths with alpha,
no frame/background; separate cutout material mounts3mm from actual column.
Physical sign thickness/shape now follows cloth/metal/timber rather than one
22cm frame for every shop. V2 textures inspected locally; Unity review pending.

Lobby probe2/2 passed before the additional Custom Game door fix. Tracing the
removed RULES row found the advanced editor's existing door had become dead
when authored arrows replaced dropdowns: SettingsRows intentionally stays null.
Now that same existing door mounts in the settings drawer, so saved nonstandard
rules remain visible/editable after shortcut removal. No duplicated map/mode/bot
controls. Fresh layout/click validation required.
FullEditv2's one failure was builder grade literal1.02 vs new1.025;updated literal
to match atmosphere author. These are integration corrections,not weakened gates.

## Support gate investigation after the visual correction

FullEditv3 509/509, lobbyv2 3/3 including actual pointer raycast and opening the
existing advanced editor,24Ilalim sign/paint views1/1. Broader geometry gate found
that it samples the entire parent building's AABB top as if every roof had that
height. This previously approved the floating rooftop units and then rejected
correct lower-wing placements. Parent building support now samples actual mesh
triangles; a stepped-mesh regression distinguishes a seated2m unit from a unit
floating at the6m overall bounds. FullEditv4 510/510. No tolerance was loosened.
The only remaining check finding is the crane jib,which is a legitimate
cantilever,not a standing rooftop prop. Its pivot/bottom will be ray-verified
against the tower bearing before marking that arm as airborne by design.
Hoarding supports also now follow actual foot vertices,not assumed yaw axes.
Repeatabilityv2 passes all3maps. Repeatabilityv3/checksv3 now pending for the
explicit verified crane join. Failed checksv1/v2 logs retained.

Original laundry draft authored outside Unity: tools/author_resident_laundry.py,
Logs/resident-laundry-v1/courtyard-line.blend and alley-line.blend (+GLB/PNG/JSON).
Separate tee/shorts/towel silhouettes,pegs and fixed sagging rope; not yet placed
or imported. The 18m street line and4.4m resident line will serve redesigned
Eskinita and Sa Bubong's laundry corner. Draft renders inspected: clothing reads
as garments,but folds/palette need actual in-game review.
Sa Bubong now has a concrete measured source layout in
MapSource/environment/layouts/sa-bubong-plan-v1.json. No playable fourth scene yet.

The crane bearing investigation did not verify a touching tower/jib surface
within the existing tolerance,including a grid around the pivot. Do not label it
verified or apply an unchecked airborne exemption. This optional unconvincing
background crane is retired from the scene and future builder; original asset
retained. Investigation text/logs preserved. Its footprint and style were not
needed for the street's identity. Existing real guideway structure stays.
Repeatability/checksv6 will qualify that final decision.

Next-map risk identified before building: a slipper lobbed over the pool fence
cannot become permanently stranded just because its height is reachable. The
Sa Bubong author/recovery design must handle inaccessible amenities as well as
actual edge falls,while preserving generic raised-ground/roof recovery elsewhere.
CharacterMotor already has host-authoritative recovery episodes/acknowledged
presses and TripAutoRecoverSeconds5; reuse that input contract rather than adding
a second mash verb. Slipper flight currently treats a missing ground ray asY0,
so an open roof edge needs an explicit map-owned loss transition before ordinary
landing; do not globally rewrite GroundY or resurrect the old flight guess.

## New rooftop/recovery work after9ff95072

Sa Bubong scene and initialphysics now exist and are registered in both map
registries/build settings. Protocol30 prevents older peers clamping its index to
another scene. Basic rooftopprobe2/2 passes: actualedgefall,prone return,hold1press
versus taps,10s unavailable shoe,returned pickup bothmodes;pool loss,inactivegrab
refusal andround reset cancellation. Logs/rooftop-recovery-v1.xml.
ActualSa FPP1/1 with48matched+2HUD captures (Logs/sa-bubong-fpp-v2). Laundrymotion
probe1/1 confirmsmovinglowercloth andfixedpegs. This is NOT allinput/network/kit
qualification or finalart approval. Skyline/rooftopstillneedsstrongerplaceidentity.

New reproduced touch failure: realTouchButton down/up andPlayerInputReader samples
beforeonephysicsstep produced0mash presses. Logs/recovery-touch-baseline-v1.xml
(0/1). TouchInput now retains a Jump press edge untilreaderconsumption,without
turning release into hold orrepeat. Readerdiscardsitwhilechat ownsinput.
Focused rerun plusa concurrenttag/trip regression isactive in
Logs/recovery-touch-and-overlap-v1.xml/.log. Existingcoupled_stunLeft/_tripLeft
looks capableoflettingtripmash shortenatag;isolatebeforechangingtimers.
Controllerimplementationremainsownedelsewhere;noGenericPadBridge/MenuNav edits.

Actual3-process roof v1 FAILED: host loadedSa but startupneverupdatedSelectedMap,
so SyncMap announcedEskinita toclients. Ownerclampedteleport13 toitsneighborhood
bounds andcouldnotfall. This is a bootstrap/map-selectionfault,notroofphysics.
NetBootstrap nowadoptsregisteredmap+legacyID beforetransportstartup;joiningload
usescurrentauthoritativeSelectedMap. Addedcatalog regression. That unwanted
scene reload alsoexposedRoundDirector.PlayerAt returningdestroyedUnity wrappers
toOnThrowChargeMsg;itnowskipsdestroyedentries,withfocusedlifecycle regression.
CSV nowcarriesactualmapindex andevaluatorrejectswrongmap evidence. No passclaimed.

Built-playercapturev1 gamechecks passed but ScreenCapture produced0images in
batchmode. Markedthat receipt capture-failed ratherthan calling itvideoevidence.
Recorder nowusesactualplayercamera HDR render+sRGBresolve andaseparateungraded
UIcamera,restoringallcanvas/layer/targetstate afterward. Runnerrequires all
recordedframes exist andnonzero60+coverage. Ordinaryplayback usesmeasuredtimes.

Exact-playerframecritique: existingobjectivehint saidRETRIEVE A SLIPPER while
allavailableloose stockwasgone for the10spenalty. It nowusesSLIPPER RETURNING
whenreplicatedinactiveattackerstockexistsandthereisnoloosealternative. Itdoesnot
inventaclientcountdown. AlsoreplacedstaleHOLD E resetcopy withliveGrabbinding
(orHOLD GRAB fortouch). CapturesremainolderthantheseUIchanges.
The repeatedLATA IS BACK UP defectwasconfirmedbyunconditionalUprightChanged
snapshotnotification; nowonlytrueedgesannounce. ItsinitialEditModetesttriggered
aruntimeDestroyinLataDownMark,so verificationmovedtoarealPlayModescene rather
than changing productioncleanup. PlayModesnapshotregression1/1passes.
SaBubongsemantic2-run comparisonpasses:0drift/1649rows. NewEskinita laundry
isnowactuallyauthored;matchedFPP/checks areinprogress. Neitherartbatchisfinal.

## Owner status clarification, 2026-09-13

The owner withdrew the Desktop build update: leave it as-is and keep developing.
After asking for a status report, they accepted continuing the rooftop placement
fixes and then tree/map refinement. Provide clear progress updates while working.

## Latest owner order, 2026-09-13

After ALL map work, work on ANIMATIONS AND SKILLS together next. Full body/FPP
preparation, release, impact, interruption and recovery are part of the skill
revamp, alongside distinct mechanics and special ultimate moments. Movement and
equipment animation examples remain requirements, not deferred cosmetic extras.


**Ambient life expansion,2026-09-13 (OPEN):** add cute moving cats and dogs to the
OTHER street maps, not the condo roof. Use local aspin/askal mixed-breed forms and
varied cat coats/builds/ears/tails, with idle/walk/run-away motion and plausible
homes/shade routes. No new pet-care system or gameplay collision/scoring. Birds
can also visit the arena and other maps; vary species/chance/timing/targets and
flee from nearby players using private cosmetic randomness. Ground animals and
validate routes against actual props/walls; no floating animals or walk-throughs.
Sa Bubong stays bird-focused. Filipino character must come through plausible
resident/street life and a few original drawings, not random clutter.
