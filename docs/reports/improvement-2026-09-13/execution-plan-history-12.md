# Active execution plan

**Newest owner additions:** keep working while the owner sleeps; MAPS FIRST.
Add a tournament spectator/cinematic review and improvement pass to the broader
queue: existing free/follow/POV, smoother intentional motion, collision/framing,
important plays/ultimates/replays and uncluttered caster information. Preserve
manual observer control and competitive readability; plan from actual code/play.

**Newest owner optimization:** no routine full EditMode/PlayMode runs. Use only
tests/checks related to the changed feature, batch coherent implementation and
keep visual/behavior quality. Full-suite mandates in older records are superseded.
Do not repeat an already passed check without a new change or unresolved concern.

## Current instruction and order, 2026-09-13

The owner cancelled the requested handoff and explicitly resumed work IN THIS
CHAT. Do not prepare a handoff or stop at the current checkpoint. Continue the
whole authorized improvement scope. MAPS FIRST -> FULL UI/UX -> ANIMATIONS,
SKILLS/EFFECTS AND PLAY FEEL -> remaining graphics/network/TODO qualification.
Swimming/body/FPP and ambient-life animation belong to the current map work.
The owner also explicitly requires believable roof-fall recovery NOW: a landing,
visible struggle/bracing while mashing, and a convincing push back to the feet.
Keep the cute blocky style, match actual recovery time/accepted mash progress,
and synchronize owner FPP/observer body. No instant upright pop or unrelated
generic pickup pose. Plan and qualify this within the current map pass.
The larger TODO152/152.4 goal and the map pass remain OPEN.

## Actual checkpoint

Only ASTRAReworks in C:/Users/Matthew/Documents/Codex/2026-09-09/ok-x20/work/TumbangPreso-Unity.
Fetched branch, no divergence from pushed dbd2344f68b59750bc060a780e61830c0e8e8792.
Tree/Sa city/swimming/UI/Figma/reference/doc work after it is still uncommitted.
No Unity, Blender or player was active at the resume preflight. No other checkout,
main, resets, subagents, paid work or usage resets. Preserve saves/IDs/cast.
Every Editor launch: tools/run_unity_guarded.py and -tp-profile owner-review-editor.
Do not edit C#/imported assets while the Editor runs. Leave Desktop build alone.

## Active work: accessible Sa Bubong pool and all-side rails

The current source authors a38x44m roof with a7.6x16m open pool, real basin,
steps, transparent water, no pool fence, and continuous jumpable outer rails.
RooftopPool + CharacterMotor provide buoyancy, slower swimming and existing jump;
Slipper floats stock; RooftopRecovery keeps10s off-roof loss only. SwimmingMotion,
SwimmingAnimationAuthor, CharacterAnimator and ViewmodelArms provide body/FPP
motion.80serialized clips for20rig hierarchies authored successfully. These
are initial implementations, NOT completed gameplay or visual qualification.

Baseline author: Logs/sa-bubong-swim-v1-author.log. Last check:
Logs/handoff-current-checks.log; saved result Logs/sa-bubong-swim-v1-checks.txt.
7/8pass. Pool floor reported24.204m floating above distant city road.
Source investigation: pool slab underside-1.8 overlapped shell top-1.6, and the
pool was a sibling of its combined support mesh, so the support query skipped
that higher bounding box. Current correction lowers shell cap to basin underside
and parents the basin to its real structure so existing triangle query applies.
Do not exempt the pool from support checks or weaken tolerances.

NEXT:
1. Regenerate Sa through its author and repeat geometry check. Preserve failures.
2. Replace stale closed-pool/single-gap expectations in RooftopRecoveryProbe.
   Verify real enter/swim/tread/exit, holding/throwing/floating pickup, all4rail
   directions, real descent/mash/10s loss, round reset and both modes.
3. Trace pool entry velocity, camera height, body/held grip, FPP strokes and
   transitions at normal speed. Revise defects. Add readable restrained water
   contact feedback only after actual contact is verified.
4. Update MapRouteProbe to include reachable water, not exclude it as timed loss.
   Update NetRoofProbe's old x13 walk-only staging; new water/all-side behavior
   needs separate-process proof. Old3process receipts are pre-pool-redesign.
5. Revisit surrounding city/roof layout and resident detail, author repeatability,
   all-map ordinary-speed views/routes/quality cost and self-critique.
6. Refine/integrate animated birds, then street cats/aspins, per AMBIENT_LIFE_PLAN.
   Bird sources now preserved under MapSource/environment/ambient-life/birds/study-v1.
   Six-bone Maya/pigeon/fantail drafts have oversized dark beaks and boot-like feet;
   they are NOT imported/integrated/accepted. Cats/dogs not authored yet.

## Latest owner requirements, still active

- Sa: accessible larger unfenced pool and genuine swimming visible to owner/FPP
  and observers; full outer fencing with falls possible over any rail; mash get-up;
  off-roof slippers unavailable about10s then safely retrievable. Water is accessible,
  so water contacts do not automatically invoke that penalty. Sensible entry/exit.
- Birds: varied types, random chance/timing/group/targets, may visit arena and flee
  from players. Private cosmetic randomness only. No fixed obvious sequence or
  interference with can/slippers/tells. Cute game-native model and motion.
- Cats/dogs in OTHER3maps, not roof. Local aspin/askal mixed builds and varied cats;
  idle/walk/run away, actual grounded routes, no pet-care/scoring/collision system.
- Eskinita: tighter neighborhood, newly designed supported sampayan, solid houses.
  Bayan: keep current layout, varied trees, tangible paving and connected town.
  Ilalim: logical recessed pisonets/vendors, varied genuine-looking shop treatments,
  surface-painted Bawal lettering, no floating/unsupported clutter, continuous road.
- All maps: distinct nostalgic lighting/material feel, natural Filipino context,
  plausible placement and human scale. Critique FPP and awkward side/back views,
  actual normal-speed play and cost, not just aerial beauty or green tests.

## Saved UI work, continue after maps

Full quirky coherent logo-led overhaul authorized, superseding older UI-art limits.
Calm title menu with original logo, few choices and subtle animated street scene;
intuitive progressive flows, distinct control treatments, consistent fonts/roles,
natural Filipino hint, keyboard/controller/touch/Back/focus and data retained.
Read UI_BRAND_REWORK_PLAN.md. Original49-page PDF and four JPGs are preserved in
ArtSource/ui/owner-brand-2026-09-13 with SHA256 manifest. PDF pages39-49 are ideas,
not final layouts/instructions. Original source paths remain in the manifest.
Resources/UI/brand holds existing transparent logo and slipper mark. Do not redraw
or claim ownership of the supplied logo. Old code preserved under reports/ui-brand-2026-09-13/before.

Home/Play/MenuKit/StreetUi/StreetGraphic changed partially. HomeFlow4/4 and fullEdit
516/516 before swimming. Screenshots Logs/shots-runtime/Home-brand-v1* and Play-brand-v2*.
Early picker Back failure remains unisolated: v2 scoped locator plus settling wait
passed, but identical old/scoped paths mean no proven production fix.
Lobby/settings/pickers/HUD/loading/results/complete UI flows are still unfinished.

Figma connected and editable: https://www.figma.com/design/I1snFz26WypEywfWMhF9jq
3pages, primitive/semantic colours,5text styles and3uploaded original assets.
Native screen components/prototypes not built yet. IDs/hashes saved in
ArtSource/ui/owner-brand-2026-09-13/figma-*.json. Starter3page cap; no paid upgrade.

## Later play-feel and whole-game requirements

Moving throws less accurate; still throws more accurate; immediate releases shaky;
longer hold settles with small residual shake. Match actual authoritative aim and
trajectory to readable body/FPP preparation, observer lean and Pektus arm/wrist
spin/release. Avoid hidden arbitrary random misses. Meaningful10slipper/6can choices.
Carry/sprint/backward/turn cadence, contacts/foot sliding/interruption/FPP sync.
All6hero kits/default18slots plus alternatives need distinct purposes/tradeoffs/
counterplay, mechanics AND body/FPP/VFX/SFX redesign. Ultimates deserve signature
special moments like Tekken/Genshin in buildup/payoff, within native style/readability.
Phaister ritual timing/reach/moon height, same-hero loadout rebinding/live modifiers,
accepted Kuro separate-process yaw/rejoin/roster/overlap remain open. Preserve18cast
7c7fcb5 direction and accepted purple Kuro; no realistic/noisy redesigns.
Graphics/scalability Low through high-spec, active effects/alternative builds,
round/rematch/reconnect/host loss both modes, and remaining actionable TODO.

## Evidence limits and delivery

Before new pool: tree FPP200images,4maps/bothmodes58actual pickups and0unreachable
samples, old3map semantic21629rows/0drift, SaV6semantic2048rows/0drift,66architecture
views. Logs/all-map-tree-v2-routes.xml and related output;14source audits passed.
These do NOT qualify the new pool/rails. Previous real3process roof recovery
Classic/Hero delayed/rejoin proof is for old single-edge version (report roofdeck-recovery.md).
Desktop remains d9c0314b protocol29; internal RoofReview remains older protocol30.
Neither executable represents current source. No Desktop replacement requested.
Final appropriate Core/Edit/check/source and isolated PlayMode gate, then exact
Windows executable ordinary-speed/both-mode/network qualification still required.
Commit stable batches with sole-author message files; push only ASTRAReworks.

Historical process IDs are retired. Previous plan preserved whole in
reports/improvement-2026-09-13/execution-plan-history-11.md. AGENTS older conflicts
preserved in agents-history-before-swimming.md. New state updates belong here and
at the newest ACTIVE_REWORK_LEDGER pointer; preserve detailed reasoning in reports.

V1 swimming fixture failed because Grab was held before reaching range; source
confirmed pickup requires JustPressed. Only the fixture timing changed. V2 passed
1/1 for both modes: entry, stable treading, floating pickup, stair exit and settling
from a jump. The trace exposed a real fast-entry defect despite that pass: y=-1.52,
FPP eye=-.27, hitting the basin bottom before buoyancy. V3 adds immersed downward
speed damping and an assertion that normal jump entry stays above y=-1.16. It also
records8seconds of body+FPP swim/tread/holding/empty motion per mode. Current run
Logs/roof-swimming-v3.xml/.log; collect before edits. Editor session57557 at launch.

Current2026-09-13 continuation: v4swim author completed and profile restored.
Water-inclusive routev1 found197samples without grid connection, while independent
actual pool entry/exit already passed. The half-metre grid spans two .24m stair
rises; subdivision fixed the first connection, then the descending capsule cast
still counted the previous higher tread as a wall. v3 preserved body-grid.csv
showing connection stops between z0 andz.5. The test now uses intermediate support
and max endpoint support height for descending casts; actual pickup paths remain
required. No production map dimensions or physical tolerances changed for this.
Routev2 had a compile-only failure from missing Visual using in the new recovery
capture, fixed before v3. Core562/562 passed concurrently with verification.
Current Editor: recovery-motion-before-v1, actual fall/mash/get-up capture before
new animation. Existing approved fall camera switches to TPP to show the body,
then returns to FPP; preserve that behavior rather than inventing FPP while down.
Birdv2 and street-animalv1/v2 native studies authored while tests ran; not imported.
V1cat muzzle looked dog-like and tails too angular; v2 revisions still need visual
review. Sources in MapSource/environment/ambient-life, tools/author_street_animals.py.

## First recovery implementation, awaiting review

RecoveryAnimationAuthor now creates landing, bracing and stand resources for the
retained rigs. Landing copies the existing fall; original assets remain intact.
Bracing/standing explicitly animate root, torso/head/arms/legs, with rendered-mesh
contact fitted per sample. CharacterAnimator selects resources for physical trip
recovery, samples bracing from accepted trip progress, and standing from actual
remaining MinTripDown. Physics/timers and independent tag clocks are unchanged.
GameBuilder ensures resources are authored. Current author log:
Logs/roof-recovery-author-v1.log; session36291 at launch. NOT visual acceptance.
Next capture and inspect contact, posture, interruptions and owner camera return.

Water-inclusive routev4 passed1/1 for both modes: each5976body nodes,5958connected,
6285clear slipper rest samples,0unreachable. Actual pickups include floating pool
stock and all outer edges/services. Logs/sa-bubong-water-routes-v4.xml and CSVs.
No runtime geometry or pickup-radius changes were used to satisfy the audit.

V2recovery motion1/1 passed. Actual bracing/stand contact measures bottom=.100m
on support=.100m. Palm approaches support while accepted presses progress, and
owner view returns to FPP. BUT the original copied landing clip penetrates up to
.896m during its rotation, revealed by the new trace. V3 now fits landing contact
through the original gesture too, without modifying the original die/emote clip.
Authorv3 is running; collect before further imported/code edits. Afterward recapture
and measure the landing, then verify current swimming posture and full EditMode.

Newest pointer: recovery-authorv3 completed60clips/20rigs, no compile error;
profile2files restored. Motion-v3 is now running after fitting the original
landing's floor contact. Prior motion-v2 passed1/1 but recorded an up-to.896m
landing penetration, despite bracing/stand bottom=.100 on support=.100. This
is why a green behavioral test did not close the motion review. Current animals
remain draft sources only; UI references/hash manifest intact; no Desktop build.
After this run: inspect contact and ordinary motion, recheck v4swimming posture,
fullEdit/checks for current source, then commit/push a stable checkpoint and KEEP
WORKING on maps, water response/ambient life and the broader ordered scope.

V3landing motion1/1 passed. Actual complete recovery trace has drawn-bottom minus
support between-.0002m and+.011m, replacing v2's-.896m penetration. Grounded recovery
now receives an explicit regression bound and nonzero sample requirement. The
bracing/stand remains within original mash/MinTripDown control timing. Original
people/models and die/pick-up source clips are preserved. All18roster and real
separate-process motion acceptance remain required; this trace is one Classic
body, not full roster proof. Current swim-v4 now records revised torso/legs and
raised right grip in both modes before broad Edit/check/commit qualification.
