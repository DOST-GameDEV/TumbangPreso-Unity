# Active execution plan

## Live state, 2026-09-13

Branch ASTRAReworks only. Pushed HEAD is9ff95072. Authorized checkout:
C:/Users/Matthew/Documents/Codex/2026-09-09/ok-x20/work/TumbangPreso-Unity.
The current rooftop/recovery batch after that commit is uncommitted. Preserve it.

ACTIVE: build v4 to Builds/RoofReview/TumbangPreso.exe, session55708,
Logs/roof-network-build-v4.log. The attempted ScreenCapture recorder wrote no
images in batch mode. That capture run is explicitly failed despite its gameplay
checks passing. Recorder now renders the actual player camera to HDR, resolves
sRGB, then composites HUD through an ungraded UI camera and restores all state.
Runner rejects missing/insufficient image files. No C#/imported edits until exit.

Verified actual build v2 (protocol30, base9ff95072+dirty, built11:58:41), RuntimeDLL
SHA256 dda2c764d68630269c3784202a98f03fbb8b367caa2cd0e26c796ba34b2a3f27:
- Classic host/owner/observer PASS, Logs/roof-network-classic-v2: all10accepted
  presses,3.89-3.91s down (fixture deliberately waits2s),10.027-10.035s shoe loss,
  subsequent pickup visible on every peer.
- Hero150ms one-way delay + observer rejoin PASS,
  Logs/roof-network-hero-delay-rejoin-v1: all10presses,4.17-4.27s down,10.019-10.046s
  shoe loss. Rejoined observer saw114unavailable samples,0trip samples; it arrived
  after get-up. Do NOT claim rejoin-during-trip from that run.
- The runner preserved only its3named probe profiles, main profile untouched.

Earlier Classicv1 FAILED because Bootstrap loaded Sa locally but announced the
stale SelectedMap(Eskinita). Map adoption now precedes transport; joining follows
authoritative selection. Destroyed registry actors are filtered after an observed
OnThrowCharge scene-reload exception. Focused launch/lifecycle regressions2/2.
Both successful runs now assert actual mapindex3. Failed traces retained.

Next after buildv3: capture normal-speed Classic and delayedHero/concurrenttag
through net_roof_matrix --capture/--overlap-tag, encode measured timestamps with
encode_motion_review.py, inspect actual owner/observer frames. Then finish Sa
art/repeatability/edge qualification, author and review Eskinita laundry/trees.

Every Editor run uses run_unity_guarded with -tp-profile owner-review-editor.
That guard now snapshots/restores only the named profile, protecting concurrent
Desktop saves. The new network runner also scopes preservation to its three
named probe profiles. No subagents, paid work, usage resets or other checkout work.

Desktop review remains d9c0314b, built2026-09-13 07:29:22, protocol29:
C:/Users/Matthew/Desktop/TumbangPreso-Unity/TumbangPreso.exe.
Its RuntimeDLL SHA256 is
1ce3858fc9f59ffbc09df3aeceb5ab0c55d9f8275521952d4e3885b75abff7e1.
Do not call it the new rooftop checkpoint or replace it while running.

## Pushed checkpoint9ff95072

- Street Hype mechanic, meter, rewards, celebrations and PlayStyle message removed.
  Actual scoring and highlight recording retained. Main RULES shortcut removed;
  existing Custom Game editor's dead entry button repaired and raycast/click tested.
- Distinct3-map daylight/sky author and original lightweight sky shader. Bayan
  paving aggregate/normal/tangents; Ilalim continuous asphalt through visible roads.
- Actual roof surface placement, billboard support endpoints, independently designed
  shop signs and directly painted Bawal lettering. Optional unverified crane retired.
- Proof: Core562, fullEdit510 plus focused2surface tests, lobby3, all8checks,
  all14source audits, matched FPP150images and24Ilalim review views. Two saved
  authoring runs have no semantic drift. See the portable report and images:
  reports/improvement-2026-09-13/owner-map-review.md.
- Original18 people and Kuro direction retained. No historical48-idle attribution.

## Implemented after9ff95072, still qualifying

Sa Bubong has an actual scene and is registered in SceneFlow/GameLaunch/build
settings. Protocol30 rejects old peers which would choose a different map; the
same protocol also carries the clarified separate normal-stun/trip timer semantics.

SaBubongBuilder owns the scene: roof court, real open edge, fenced pool, resident
shade, stairhead, laundry, tank, thick condo shell and city below. V1's empty/blank
box skyline was rejected in our own review. V3 replaces it with five retained
commercial skyscraper families at measured uniform scales and connected streets,
adds a faded green recreation-court coating and lower afternoon sun. V3 actual
player-view critique is still due. Do not describe this map as finished.

RooftopRecovery owns a real descent latch, prone safe return, and ten-second
unavailable slipper return. Pool-stranded slippers also recover. Host owns state
and time; existing inactive-slipper snapshots carry availability. Slipper's new
begin/end map-recovery entry points release possession; inactive stock rejects
all authoritative grabs. Generic GroundY and the other maps' recovery are unchanged.

The owner wants all-context mashing. We reproduced two failures:
- Quick TouchButton down/up could disappear before physics (0presses). TouchInput
  now retains one recovery press until PlayerInputReader consumes it; holds do not
  repeat, and chat discards queued recovery input.
- Trip mash shortened an independent4s tag to3.78s. Normal stun and trip now keep
  separate clocks; IsStunned includes either. Trip/prediction never spends tag or
  elemental time. Physical roof falls use ApplyFallRecovery, so ordinary ability
  immunity does not negate a physical get-up. Old explanations and failed XML stay
  in the reports/Logs. Tolerances and the tag rule were not weakened.

Native laundry: tools/author_resident_laundry.py, MapSource/environment/resident-
laundry and Assets/.../models/resident-laundry.4.4m courtyard and18m alley versions
have separate shirts/shorts/towels, pegs and fixed sagging rope. LaundryMotion
billows below fixed top vertices. ResidentLaundryAuthor is hooked into the Eskinita
source author, but that scene has not yet been re-authored/reviewed with it.

## Evidence for the current batch

- Rooftop basic physics2/2, then expanded3/3: actual fall, hold vs taps,10s shoe loss,
  returned pickup in both modes, pool/inactive grab, reset cancellation and laundry.
  Logs/rooftop-recovery-v1.xml and rooftop-recovery-v2.xml.
- Actual Sa FPP v2:1/1,48matched views plus2HUD images across both modes/3presets,
  Logs/sa-bubong-fpp-v2. These predate v3 skyline/court art.
- Laundry motion1/1 confirms moving cloth with fixed pegs.
- FullEdit511/511 before the newest NetRoofProbe/guard-list addition and v3 art,
  Logs/rooftop-edit-v2.xml. First run only failed the deliberately updated protocol
  constant expectation (29 to30); failed XML is retained.
- RecoveryDeviceProbe5/5:63trip/element/device/requested-cadence combinations plus
  independent tag and delayed-snapshot regressions. Keyboard/gamepad use synthetic
  devices through the real configured action asset/reader. Touch uses real button
  callbacks/reader. Requested30/60/144 rates have observed frame timing logged;
  this is not a handset or physical controller certification.
- NemuKitContractProbe33/33 after recovery changes: nemu-after-recovery-v1.xml.
- Last completed test guardc1c3b54d769d restored2named-profile files.
- Current Core/source gates must be rerun after the NetRoofProbe modifier addition.

## Next concrete actions

1. Collect build83324, verify exact executable/data/RuntimeDLL and build identity.
2. Run tools/net_roof_matrix.py against THAT executable: Classic real3processes,
   then Hero with150ms one-way delay and observer rejoin during lost stock. Read
   actual descent, mash timing, ten-second absence, return and pickup on every peer.
   NetRoofProbe is opt-in and blocked by the tournament guard/list/CLI filter.
   The runner preserves only roof-review-host/owner/observer profiles, with an
   on-disk manifest. Do not use the old net_throw_matrix broad main-profile restore
   while the owner is playing.
3. Fix observed network/physics/visual failures. Qualify roof recovery during skill
   immunity/overlap, teleport/possession destinations and inaccessible amenities,
   round/rematch, reconnect and host loss. Keep pending snapshot tests distinct
   from genuine separate-process evidence. Capture ordinary-speed falling/get-up.
4. Re-author/review Eskinita's new laundry and tighter residential enclosure. Keep
   the current Bayan layout; the owner retracted the earlier rejection. Vary trees
   across maps using existing assets/native alternatives, not scaled identical
   crowns. Inventory is Logs/tree-source-inventory-2026-09-13.txt. Complete further
   map placement, side/back architecture and retrieval-route critique.
5. AFTER MAPS, the latest owner priority is full ability implementation/mechanics
   and visual revamps across all6kits/defaults/alternatives. Distinct purposes,
   tradeoffs/counterplay, complete body/FPP/casting/moving geometry/VFX/SFX, and a
   signature moment for every ultimate (Tekken/Genshin impact as timing references,
   not copied assets or lengthy mandatory lockouts). Phaister and Kuro's remaining
   qualification and same-hero loadout binding stay explicit.
6. Continue remaining movement/animation, meaningful slipper/can choices, graphics
   settings/scalability, networking and actionable TODO work. Preserve the original
   game,18people, both modes and simple controls. Maker stays inaccessible.
7. Final release still needs appropriate gates, isolated PlayMode gate twice,
   Windows build and that exact executable at ordinary speed in both modes.

## Durable references and cleanup

Read AGENTS.md as primary instructions, OWNER_PLAYTEST_REVISION.md for all recent
feedback, MAP_TRANSFORMATION_PLAN.md and PLAY_FEEL_REWORK_PLAN.md, plus the broader
plans required by AGENTS. The larger improvement goal is OPEN. Do not stop at a
handoff or ask again to continue work already authorized.

Keep genuine authored map assets. Restore only proven test-only arm tangents via
Logs/restore_verified_throw_test_dirt.py and exact known QualitySettings Ultra
noise (4/2/4/150/true became2/1/2/40/false). Preserve before bytes and compare the
whole recognized diff. Never use old map cleanup scripts to restore newer scenes.
Ordinary Unity serialization has trailing spaces; do not strip entire scene YAML.
Use normal Git settings, sole-author message files and push ASTRAReworks only.

Previous execution plans are archived whole through execution-plan-history-09.md.
All previous process IDs outside this live section are historical.
