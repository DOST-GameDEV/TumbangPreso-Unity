# Ability rework plan

**Standing model-style rule:** all reworked skill models, summons, pets and FPP
assets must look native to TUMP's cute blocky world and avoid unnecessary detail.
Use clean chunky forms and purposeful effects; scale, pose, timing and sound make
an ultimate imposing. Follow
[Art_Direction.md section0](Art_Direction.md#0--new-models-must-belong-to-tump).
Kuro's current purple/blocky/floating-eye corrections remain in the active ledger.

**Owner's animation clarification:** improve skill VFX and casting as well as
body/FPP clips. Different abilities/actions need appropriate distinct animation
through preparation, release, impact and recovery. Repetition is allowed only
when necessary, with the reason recorded. A generic cast or burst recolored across
kits does not satisfy this work. Art_Direction.md section0.1 is the standing rule.

Updated 2026-09-10. Status: ACTIVE, NOT COMPLETE. Source checkpoint: `c09fba2`.
This is the owner's requested durable plan, not a final handoff or completion claim.
Read AGENTS.md, docs/VISION.md and docs/TODO.md first, then ACTIVE_REWORK_LEDGER.md.

Latest checkpoint: Kuro's matching purple forms, three graphic expressions and
eleven idle clips are pushed at864cead, documented in
[kuro-matching-forms.md](reports/improvement-2026-09-10/kuro-matching-forms.md).
Whole-kit/default-alternate redesign and broader animation/SFX/VFX/network
acceptance remain open. The owner subsequently asked to wrap up with unfinished
maps included in a thorough chat handoff; preserve this plan for continuation.

### Next source-grounded Phaister review after the Kuro batch

Fresh source inspection confirms another concrete placement defect:
HeroHazards.SpawnGrandCovenEclipse hangs its moon at11m and claims this is under
Ilalim's guideway, but MapKit.IlalimNgTulayBuilder sets the soffit to8m. That comment
is false. Verify the real overhead view and author a coherent visible placement
under the guideway, without moving the ground footprint onto its roof.

Grand Coven still applies its first curse immediately in OnActivate while the
inscription takes1.55s to build, and its constructor has no windup. Its current
10.5m radius also needs a deliberate counterplay decision. Design invocation,
inscription/alignment, actual curse, repeated pulses and unbinding as one sequence;
do not hide active control behind an unfinished visual warning. Ground-draped
layers must not subsequently rotate/scale off their sampled floor. Keep the
fictional lunar/eclipse grammar in PHILIPPINE_ABILITY_DIRECTION.md, and inspect
the whole Q/E/ultimate kit and loadout alternatives rather than only adding rings.

Before alternate-ability comparisons, fix the reproduced same-hero build-binding
gap in MatchRpc.RebindKitIfHeroChanged without resetting active kit state or
applying variant multipliers twice. This remains open, not implemented here.

## Current owner instructions

- **Latest cultural brief, all six heroes:** make abilities and descriptions more
  Filipino through well-fitting folklore, places/materials and cultural references,
  without forced decoration or cringe. Keep English copy; use Tagalog only if a
  specific term is truly needed. Thoroughly plan before implementation. The owner
  is AFK and expects complete autonomous execution. The researched direction is
  [PHILIPPINE_ABILITY_DIRECTION.md](PHILIPPINE_ABILITY_DIRECTION.md); read it before
  further cultural design changes. It complements every action below.

- **Explicit Phaister brief:** improve all her magic. Grand Coven must be grand
  spellcasting, with runes and a complex magic circle unfolding through distinct
  phases. This is a core deliverable. Readable complexity is wanted; a large flat
  ring, generic column or pure brightness does not satisfy it.
- **Explicit Nemu brief:** rework her ghost and all her skills. The ultimate must
  transform the cute ghost into a giant, raging/scary ghost that sucks everyone
  inward. A floor maw or colored aura without that transformation is insufficient.
  Make anticipation, transformation, rage, intake and return readable. Inspect the
  live companion/hero link rather than guessing which mesh is actually transformed.
- **Latest Kuro personality/trailer brief:** author distinct idle expressions and
  body/tail gestures, including curiosity, sleepy drift, playfulness and attention
  to Nemu. Keep them subtle during matches and reproducibly stageable for future
  trailers. Audit existing fidget states and make them operate on the real model's
  separated parts; random root movement alone is insufficient.

- Thoroughly improve **all eighteen existing abilities**, including the geometry
  and models they spawn, body/FPP animation, sounds, effects, utility, timing,
  recovery, interruption and overlapping presentation. Do not stop at one hero.
- Every skill must read as a deliberate cast. Every ultimate must feel imposing
  through preparation, body weight, a distinct event and aftermath. Large opaque
  shapes, full-frame color wash, blur and extra flashes are not a substitute.
- The owner rejected the ice skill as an ugly platform with spikes and a central
  cutout. Rework it thoroughly. Grounding alone did not make its art acceptable.
- Ground skills must stay on the actual ground, including airborne casts, raised
  floors, kerbs and Ilalim's overhead guideway. Preserve the verified placement
  correction while replacing meshes. Airborne particles can rise; the floor
  footprint, source and any physical barrier must stay physically coherent.
- **Keep the cleaned character models and outfits.** The owner liked Berto's old
  clothes and then called the cleaned Classic cast solid, asking to move on.
  This supersedes the earlier individual character-redesign queue. Do not revive
  either rejected Berto body or integrate the separate forearm study by default.
  Skill models/effect geometry remain major deliverables and may be reauthored.
- Keep cute blocky people, simple flat faces, original clothes and simple block
  hands. The added thumbs, cuff squares and pasted-on pockets/buttons are removed.
  Realistic animation means convincing weight, contact and recovery within that
  style. Narrow rig support may be considered for motion, with unchanged rendered
  identities and proper compatibility checks; it is not permission to redesign them.
- Use Blender and other suitable tools where useful. The owner permits necessary
  tool downloads. Blender 5.2.1, Python, image/audio tools and Unity 6000.5.8f1 are
  already installed. Download only for a real need, from a trusted source with
  suitable licensing. Ask before paid external work. Never perform a usage reset.
- Stay on ASTRAReworks. No subagents. Preserve four players, rotating defender,
  can, slippers, Classic/Hero Strike, profiles, saved IDs and network authority.
  No new skill slots, modes or unrelated mechanics. Functional improvements to
  existing skills are authorized. Do not add complicated systems for hypothetical bugs.
- UI redesign is excluded. Change player-facing descriptions only as needed to
  keep them truthful. English copy; preserve proper names and persisted IDs.
- Work autonomously. Do not wait for optional taste questions or stop at a plan,
  passing tests or a single improved effect. Significant stable batches get TODO
  updates, sole-author commits from message files, and pushes to ASTRAReworks.
- After this pass, complete remaining ordinary animation, gameplay/map scrutiny
  and Windows verification/build/exact-player evidence from the active ledger.
  Do not omit that work because the recent messages concentrate on abilities.

## Evidence and interpretation

All six kit sources and their eighteen constructor/cast paths were inventoried in
[ability-runtime-inventory.json](reports/improvement-2026-09-10/ability-runtime-inventory.json).
The inventory records actual clip/action/cue names, spawners and direct primitives.
It is not a claim that every nested helper or every sound has been fully reviewed.

All six existing `*-sequence-review-v5.jpg` sheets in
`docs/reports/improvement-2026-09-10` were visually reviewed again. They show body
and FPP phases for all eighteen accepted actions. They are earlier evidence, not
captures of the latest grounding/recall correction. Some frames contain the prior
skill's legitimately lingering effects. Isolate casts for individual review, then
make separate overlap captures. Do not call every lingering trail a leak.

Concrete shared findings:

- Much of the geometry reads as stacked primitives: ice platform/spikes, cube armor
  plates, long opaque pillars, repeated cylinders and discs. More small blocks do
  not address the silhouette or the actual role of the skill.
- Several ultimates share the same column, filled ground flare, broad light wash
  and camera pulse. The generic presentation competes with their bespoke payloads.
- First-person hands can cover the useful center of the frame at the peak. Actual
  animation timing and contact must be read at normal speed, not from clip existence.
- Sean looks pink in some earlier effects, and faces look intensely yellow in some
  skill and warm-up captures. Trace tint/material/lighting and capture color handling
  before assuming a missing shader or blindly recoloring every effect.
- Upright sprite sheets can look like stickers inside the 3D field. Keep them only
  where the source art, scale, perspective and duration support the physical event.
- The large skill-word callouts sometimes read backwards from the witness side.
  Review world feedback orientation and redundancy as part of presentation, without
  turning the work into a menu/HUD redesign.

## Verified foundation to preserve

`c09fba2` fixed actual floor placement for eleven spawners and ground reticles.
Ice/fire floor geometry was checked across a kerb, with 3 mm clearance. Airborne
placements now match the actual floor on all three maps. Actors, loose objects,
generated barriers and the overhead deck no longer become the support surface in
those cases. Core 559/559, EditMode 470/470, fourteen gating audits and the final
focused ground/hand/movement cases passed. See
[ground-and-zack-correction.md](reports/improvement-2026-09-10/ground-and-zack-correction.md).

Zack's sustained sprint now gives its intended 25% wish-speed gain: measured
2.6565 -> 3.3206 -> 2.6565 m/s before/during/reset. The initial dash and existing
cooldown/trail limits remain. Thunderstrike's stale active-tail self-impulse was
removed after a red 30/60/144 update-rate check. Its charged-throw window remains.
Magnet's short source-to-hand trace, measured hand aura and owned cleanup replace
its unrelated nearby-object arcs. These correctness fixes are not the completed
animation/SFX/VFX pass.

## Execution order and completion gate

Start with Cheska's Permafrost Sheet, the owner's latest explicit rejection, then
finish her complete kit. Continue through Sean, Dante, Zack, Nemu and Phaister,
including all three actions per hero. Adjust this order for a concrete dependency,
recording why. The scope remains eighteen actions regardless of order.

For each action:

1. Trace its real press/hold/release, accepted/refused path, authority, resources,
   gameplay footprint and termination. Inspect the spawned geometry and live material.
2. State the specific visual/function problem and an action-specific design.
   Author in Blender or code-native mesh/shader tools as appropriate. Preserve
   working art and audio that contribute to the result; do not replace everything
   with one shared recipe in different colors.
3. Review preparation, commit, contact/active period, interruption and recovery
   with body, FPP, SFX and VFX together. Tune from actual event times. Distinguish
   private aiming from an accepted warning; no premature confirmation or spending.
4. Render the actual game path at ordinary height and gameplay distance. Inspect
   normal-speed sequences, refusal/cancellation and a separate overlap scenario.
   Check low-quality visibility and 30/60/144 Hz timing where it matters.
5. Verify relevant rules/authority/lifecycle/geometry, update the row below and
   record exact artifact paths and limitations. A file count or screenshot is not
   a complete action. Keep building after one hero is finished.

At the end, verify real host/joiner/observer behavior and the exact Windows player.
A second camera in one process is local-witness evidence, not a remote client.
Do not claim human playtest or taste approval beyond what the owner actually said.

## Cheska: cold that forms on the street

### Permafrost Sheet / cheska_skill1: OPEN, next art implementation

Current path: `CheskaHeroKit.PermafrostSheetAbility` -> `SpawnIceSheet`.
Body `hero-cheska-frostwave`, FPP `frost-sweep`, cast `sfx_cast_cheska_sheet`.
Actual field is a 2.3 m radius, 5-second slipping zone (modifiers still apply).
Current geometry is a 0.26 m hexagonal slab, collar, five tall shards, frost cracks,
light, motes and a central upright formation sheet. The owner explicitly rejects
this appearance. It reads like a physical platform/obstacle despite being a slick.

Direction: a low frozen skin over the street, with deliberately fractured facets,
frost at the perimeter and a quick spreading formation. Keep a readable danger
boundary immediately when the hazard becomes active; the internal frost may grow
within it. Road texture and retrieval objects should remain readable through the
center. Remove the pedestal composition, five decorative spikes and sticker-like
central cutout. Use restrained glints and close ice-forming sound with a distinct
end. Keep the field's actual footprint, slip behavior and charge economy.

Gate: new surface remains grounded at center and across kerbs, cannot be mistaken
for the wall, does not obscure slippers, and expires cleanly. Update geometry
probe child names if needed while preserving real bottom-surface checks.

### Ice Barricade / cheska_skill2: OPEN

Path: `IceBarricadeAbility` -> `SpawnIceBarricade`. Body `hero-cheska-raise`, FPP
`ice-raise`; exact cue is in the inventory. Current barrier is three cube pillars
with topper pieces and small shards. The source retains solid body/slipper blocking.

Direction: three joined but individually fractured glacial uprights with clear
bases and a deliberate top profile. Growth should read upward from the planted
hand action. Use cold interior mass, frosted edges and fracture accents rather
than a flat cyan wall plus decoration. Keep openings and occupied volume honest.

Gate: all three bases rest on ground/kerbs; collider and visible barrier agree;
body and slipper interception remain reliable; end/break sounds match removal.

### Glacial Nova / cheska_ultimate: OPEN

Path: `GlacialShatterBurstAbility` -> `SpawnIceBurst`, victim `SpawnIceCubePrison`,
stagger/deflect paths. Body `hero-cheska-nova`, FPP `nova-burst`. Current effect shares generic ultimate wash and particle burst;
victim prison is a separate model and must also be reviewed.

Direction: compress cold inward during commitment, then release one sharp fracture
front and a low outward frost wake. Strong body brace, a clear crack/low impact
and a short crystalline tail. The prison should look like encasing ice around a
readable victim, not an opaque blue box. Avoid a white screen or a duplicate wall.

Gate: footprint/victims are truthful, freeze and break/end are legible, existing
mash/deflect/authority rules remain. Review the prison on all relevant body sizes.

## Sean: directed heat, ignition and a committed landing

### Flame Rush / sean_skill1: OPEN

Path: `RocketBurnDashAbility` -> accepted dash/contact, `SpawnFireTrail`, fire aura.
Body `hero-sean-dash`, FPP `thrust-fire`, cue `sfx_cast_sean_rush`.

Finding: a very quick whole-body tip and repeated procedural fire tongues/char
patches; read the complete run, not only the first kick. Contact uses 2.2 m while
trail radius is 1 m, so the visual body-contact promise needs review.
Direction: a planted push into a committed forward drive, trailing heat following
where he actually travelled. A tapered ash/ember wake and sparse flames should
support motion without carpeting the lane with separate ornaments. Burst sound
at push-off, ground-contact rhythm and a clearly relaxing finish.
Gate: no floating trail, honest swept contact, no fake hits, stable grip and readable
retrieval route. Do not enlarge reach merely to match a noisy effect.

### Ignition Cannon / sean_skill2: OPEN

Path: `IgnitionCannonAbility` -> hand charge; actual throw in `Carrier` and actual
fire affinity impact in `Slipper`. Body `hero-sean-ignite`, FPP `ignite`, cue
`sfx_cast_sean_cannon`. Reviewing the buff press alone misses most of the skill.

Direction: a deliberate ignition gesture, compact heat held on the slipper, an
obvious empowered release, then a grounded/contact-centered impact. Review the
pink-looking sheet/material path before changing the fire palette. Retain a clear
slipper silhouette and distinguish arming, launch, hit and consumption sounds.
Gate: carried charge belongs to the actual grip; refusal/expiry/throw remove it;
impact VFX match the real collision and do not pretend an airborne hit is a floor hit.

### Supernova / sean_ultimate: OPEN

Path: `SupernovaSmashdownAbility` -> leap/dive, `CreateExplosion`,
`SpawnSupernovaCrater`. Body `hero-sean-supernova`, FPP `supernova-slam`.

Direction: clear preparation, upward commitment, a readable dive and a heavy
landing. One main fire event with directional debris/embers; a crater aftermath
that looks part of the street. Author crater/rim geometry if the current ring/bed
reads as a flat badge. Keep the retained body recognisable during the leap.
Gate: body and FPP contact/recovery follow the actual landing, no recovery in midair,
no full-frame red/pink wash, 5.4 m footprint truthful, crater grounded and cancellable
at lifecycle boundaries. Review owner, witness and actual network observer.

## Dante: weight, fractured stone and defensive armor

### Seismic Stomp / dante_skill1: OPEN

Path: `CreateExplosion`, `SpawnCrackedLavaDecal`, `SpawnVolcanicRockDebris`.
Body `hero-dante-stomp`, FPP/cue from inventory.
Finding: many large debris pieces and labels obscure the actual body contact in
v5 evidence. Direction: visible support-foot transfer and heel contact, a short
fracture through the ground and sparse asymmetric stone slabs. Lower dust should
locate the impact, not become a wall in the caster's face.
Gate: contact precedes debris, footprint remains clear, decorative debris has no
colliders, and recovery preserves control/authority rules.

### Demonic Carapace / dante_skill2: OPEN

Current model path is inline in `DemonicCarapaceAbility`: a 1.85 m sphere and three
0.5 x 0.7 x 0.15 m cubes orbiting at 1.25 m radius. Evidence shows large orange
rectangles in FPP. This is a confirmed source/model problem, not an inference.

Direction: close-fitting fractured basalt armor plates or a restrained protective
shell that expresses the actual temporary immunity. It must look attached to
Dante's protection, with hands still usable and the view clear. Tension/locking
stone at start, readable protected state, and a distinct release. Keep heavy-plating
modifier and immunity cleanup; do not introduce damage or a new shield resource.
Gate: both owner and opponents can read armor without cubes sweeping through the
camera; every owned piece/aura and any slow are removed on end/reset/refusal.

### Titan Fissure / dante_ultimate: OPEN

Path: forward impact, `SpawnEarthPillar`, volcanic debris and eruption.
Body `hero-dante-fissure`, FPP/cue from inventory.
Finding: long columns and debris can occlude the whole witness/owner view; v5 uses
a green-tinted common ultimate pulse despite the rock/fire payload.
Direction: a heavy two-stage brace and ground strike, a directional rupture with
purposeful fractured pillars and a brief low dust front. The fissure's path should
lead the eye toward the actual impact and leave playable gaps visible.
Gate: preserve forward offset and actual contacts/obstacle behavior; pillars stand
on the floor, not the caster's airborne height. Sound must have distinct stone
fracture and settling mass without constant rumble masking pursuit.

## Zack: skating momentum, a magnetic return and a sky strike

### Bolt Sprint / zack_skill1: OPEN presentation; function correction verified

The sustained-speed correction is in `c09fba2`. Preserve it.
Direction: a clear skating drive and cadence matched to measured speed; compact
sparks at contact and a narrow wake behind the skates. Current shock discs/star
pieces need overlap review. Skating/charge sound should follow movement and end.
Gate: no body/face wash, truthful lagged trail and six-disc cap, clean duration/reset,
footwork coherent when walking, sprinting, turning and stopping under the boost.

### Magnet / zack_skill2: OPEN deeper presentation; trace/anchor correction verified

The actual host equip is immediate. Preserve the short source-to-hand trace and
owned aura correction. Body `hero-zack-charge`, FPP `overcharge`, cue
`sfx_cast_zack_magnet`. Direction: a receiving hand and decisive pull/settle, a
brief connection and a distinct return/charge sound. Keep the shoe clearly visible.
Gate: successful recall, held/airborne/other-owned refusal, consumed/expired charge,
reset and network order. Do not treat the old unused FlightSeconds as gameplay.

### Thunderstrike / zack_ultimate: OPEN imposing-ultimate pass

Preserve removal of the stale self-impulse. The existing seven-second charged-throw
window still exists through `IsThunderstrikeActive`; keep descriptions truthful.
Direction: one arm calls the sky, a meaningful held preparation, then a strong
pointing release and a discrete sky-to-target stroke. Branching lightning should
actually connect its intended endpoints. Contact crack, low thunder and fading
roll should be staged with the strike, without burying the can/players in yellow.
Gate: aimed 4.5 m strike remains on the warning; inspect `SpawnLightningBolt` actual
orientation, shared column/flare, light spill, FPP occlusion and recovery. Compare
isolated and overlapping casts instead of judging a capture containing old trails.

## Nemu: phase, displacement and a readable predatory companion

### Phantom Veil / nemu_skill1: OPEN function/model/animation review

Path: `PhantomPhaseAbility`, body aura/light and start/end Bloom sheets. Current
source applies 3 m/s² per-tick impulse against 30 m/s² friction, the same sustained-
speed mismatch found in Zack. It also ends whenever HoldingSlipper is true, including
when one was already held on activation. v5 shows an accepted cast immediately
followed by PHASE BROKEN. Reproduce the intended held/retrieval transition before
choosing a correction; do not silently waste the cooldown.

Direction: a spectral transition with a coherent body rim/afterimage and a compact
wake, not a purple light bulb. Phase state and its end should be visible without
losing Nemu's small silhouette. Preserve role/tag/immunity and modifier contracts.
Gate: existing-held, empty-handed, actual retrieval, refusal, expiry/reset and
authority paths; real movement gain if the description continues to promise it.

### Astral Hijack / nemu_skill2: OPEN

Path: `GhostlyPoltergeistAbility` -> `SpawnGhostPoltergeist`, then reactivation/swap.
Body `hero-nemu-project`, FPP/cue from inventory.
Direction: a clear separation of body and projection, an authored ghost silhouette
related to Nemu/Kuro, and a decisive exchange with distinct departure/arrival sounds.
The useful projection must be visible without large repeated bloom circles.
Gate: first press and second press, flight/lifetime, destination legality, cleanup
and remote presentation. Preserve the already-fixed reactivation input edge logic.

### Devouring Seance / nemu_ultimate: OPEN, explicit transformation deliverable

The owner explicitly wants the cute ghost to become a giant raging/scary ghost
that sucks everyone inward. Rework the ghost itself and its temporary ultimate
form; this is authorized despite keeping the current person cast. The character
and companion linkage must be traced in the live path, so the intended ghost
actually transforms rather than a generic replacement shape appearing nearby.

Live path: `NemuCompanion.Devour` when present, with `SpawnKuroUnbound` providing
the floor zone. The old `SpawnSeanceVoid` showcase is not the shipping ultimate.
Review the normal ghost's model/material/animation and every transformed part.

Direction: a recognizably cute resting ghost; an unsettling, visible escalation;
a large, furious devouring silhouette with an expressive mouth/face and forceful
body motion; then an intake whose inward movement and sound explain the pull.
Keep the giant form recognizably derived from the cute one. Make the rise, raging
sustain, strongest intake and collapse/return distinct phases. The floor boundary
supports the creature; it must not be the main event. Do not replace this request
with a purple pole, unreadable black heap, larger aura or more particles alone.

Gate: normal and transformed ghost both reviewed from multiple angles; actual
pet-present and fallback paths; 4 m gameplay pull footprint remains truthful unless
a documented functional change is verified; affected bodies/slippers remain
trackable; expiry/reset returns to the correct cute form without stale scale,
materials or effects. Layer the anticipation, roar/inhale, pull sustain and return
sounds. Verify owner, observer and actual network presentation at normal speed.

## Phaister: deliberate inscription, a torn passage and an enclosing eclipse

### Hex / phaister_skill1: OPEN

Path: `SpawnHexSigil`; body `hero-phaister-hex`, FPP/cue from inventory.
Direction: a purposeful writing/placing gesture, an inscription that forms in
readable strokes, and a clear active/ending curse. Keep the witch's existing
geometric language, simplifying redundant marks instead of adding more nested rings.
Gate: the 2.4 m field and actual slow/curse behavior match the visible mark,
terrain conformity survives new art, and the control state ends cleanly.

### Shadow Blink / phaister_skill2: OPEN

Path: `SpawnShadowRift` and `SpawnShadowArrival`; aimed teleport plus existing shove.
Direction: departure tear, short movement impression and a decisive arrival, with
separate spatial sound at each endpoint. The portals must have purposeful torn
geometry and perspective, rather than loose line fragments in front of the camera.
Gate: valid/invalid destinations, held aim, actual teleport/recovery, arrival shove
footprint and collider legality; no visual promise of travelling through blocked space.

### Grand Coven / phaister_ultimate: OPEN, explicit grand-magic deliverable

The owner explicitly wants grand magic being cast, with runes and a complex magic
circle that has phases. Rework all of Phaister's magic coherently, with this
ultimate as its most elaborate expression. Complexity should have structure and
meaning, rather than uniform density, random symbols or every ring moving at once.

Current live source: `Reach = 10.5f`, repeated curse and seven-second duration.
Do not quote the helper's default radius as the gameplay radius. The constructor
currently does not set `Windup = UltimateWindup`; the other kits' common-windup
comments do not prove it has one. Verify the actual committed warning and defender
counterplay before changing timing or range. At a central cast, 10.5 m exceeds
the confined box's center-to-corner distance; test that case.

Proposed readable phases to author and review as one spell:

1. Invocation: a purposeful body/FPP gesture and a sparse first inscription make
   the origin and impending event unmistakable.
2. Inscription: major concentric rules and intersecting structures draw in a
   deliberate sequence; secondary runes activate in groups with clear hierarchy.
3. Alignment: selected rings/rune groups align and lock, with restrained motion
   elsewhere. The composition should feel like an actual ritual reaching its trigger.
4. Payoff: the eclipse/coven arrives with a weighty visual and sonic event, visibly
   connected to the completed circle and the real accepted control effect.
5. Sustain and release: readable periodic curse pulses and summoned figures, then
   ordered unbinding/return. Avoid a field that stays at peak brightness for seven seconds.

The exact phase clocks must match the live gameplay warning and repeated curse,
not invent a delayed visual warning after victims are already affected. Keep the
existing witch identity. Review every summon model, inscription layer, eclipse,
shader, sound and body/FPP pose. Preserve correct ground draping and the visible
playable space inside the spell. The common magenta column/flare must not replace
or outshine the actual ritual or turn the hands into a solid pink block.

Gate: each phase recognisable at normal speed from both caster and opponents;
complexity readable across all three maps and low graphics; actual reach/curse/
recurse/empowered-throw behavior is truthful; every layer cleans up on expiry and
round/reset/refusal paths. Any timing/range/function retune needs a specific
before/after rationale and role/counterplay evidence.

## Resume state

- No full deep ability pass is complete. The verified correction checkpoint is
  `c09fba2`; it is a foundation, not the finished visual/animation result.
- Cheska's deep pass and Kuro model/personality have now landed (1b83710 and
  a1ac87b); Nemu local function/audio is pushed at 74664f4. Finish the explicit
  networking request in the active ledger, then continue Phaister's phased magic
  and the remaining kits. Keep all other rows open and continue without asking to proceed.
- Store new model/source assets and licenses appropriately. Preserve before/after
  normal-speed evidence; do not overwrite a rejected iteration and call it approved.
- Update this file and ACTIVE_REWORK_LEDGER.md when priorities, findings or results
  change. Keep TODO current in the same commits. On compaction, resume this exact
  queue and the remaining overall game work; do not restart the model redesigns.


## Additional source findings, 2026-09-10

- Cheska's grounded sheet still rotated its root 20 degrees/sec after initial mesh
  conformance. The strengthened probe now checks after one second as well as at
  spawn. Its old 5.5*dt impulse also loses to the motor's Friction=30. A bounded
  traction correction is under actual movement verification; see active ledger.
- **Kuro's current GLB has ONE node and ONE mesh**, named GhostPetRoot/GhostPetMesh,
  with bounds x[-.038,.036], y[-.126,.042], z[-.036,.047]. This is decisive for the
  failed transformation: GhostPetCompanion.FindFace searches named mouth, eye-l,
  eye-r and tail transforms, but none exist in that source. Scaling the root and
  adding horns cannot animate those missing facial parts. Reauthor the explicitly
  authorized ghost as a native Blender model with separately named simple face,
  body, arm-wisp and tail parts, preserving a cute flat-face baseline and enabling
  a visibly widening mouth, angry eyes, broader silhouette and trailing wisps.
- Kuro is currently given Nemu's sixteen-color PERSON palette by CharacterVisual.
  The reauthored companion needs its own deliberate materials; do not allow an
  untextured new mesh's default UV to map every surface to the same person slot.
  Preserve the live possession/return/owner-visibility link and actual pull center.
  Keep persistent human character designs unchanged.


## Latest explicit networking request, 2026-09-10

The owner explicitly says networking is broken and asks for a thorough fix. Treat
this as required release work, not an optional polish item. Verify separate
host/joiner/observer processes, actual skill/familiar positions and contacts,
seating/ownership, denied or repeated requests, reconnects, round/rematch transitions,
stale effects and authority. Include the proven missing controlled-familiar motion
route and the projection cancellation fixes already in progress. Keep both modes,
four players, saved IDs and profiles. Use existing transport/movement contracts,
finite/bounds/elapsed-distance validation and a protocol bump if the wire changes.
Do not claim same-process witness evidence verifies LAN. Network work remains in
this same task; no new task, subagent, paid service or usage reset is authorized.

## Latest ability and loadout scope expansion, 2026-09-10

The owner explicitly authorizes changing or replacing boring abilities, as well
as improving their presentation. Every hero's loadout alternatives must differ
enough to have a real use case and deserve their slot. This supersedes the earlier
blanket restriction against changing existing ability mechanics where it would
prevent the requested kit rework. Preserve four players, rotating defender,
can/slipper throw/retrieval, both modes, six hero identities and the existing
loadout structure. No extra modes or feature systems are requested.

Before implementing each kit, record the base action and each existing alternative:
its distinct tactical job, attacker and defender use case, readable tell, response
available to opponents, cost/opportunity cost and cancellation/network contract.
Compare alternatives side by side. A tiny statistical difference or cosmetic
recolour alone does not satisfy the latest request. Replace a redundant option
within its existing slot rather than accumulating more abilities or complexity.
Review actual play and loadout descriptions together, in English with restrained
Philippine cultural grounding. Keep all existing IDs/save migration obligations
explicit if an ability's function changes. Animation/FPP/VFX/SFX must communicate
that function, especially imposing but readable ultimates.

Finish the currently exposed network failures first, then execute this expanded
six-kit review and remaining animation/map work. Do not quietly return to the
old presentation-only interpretation after compaction. No subagents or new tasks.

### Lore anchor confirmed with the owner

The owner asked whether the intended ability/lore vision was retained and accepted
this answer: powers express the character's personality and lore; cultural cues
belong in behavior, material, motion and sound, not forced names or decoration;
casting must be visible through body/FPP preparation, release and recovery;
loadout choices need real jobs; ultimates are major readable events. Phaister is
phased grand ritual, Nemu's cute familiar becomes a giant inward-pulling rage form.

LORE.md remains the world constraint: this is expressive, extraordinary SPORT,
without lasting injury, destroyed neighborhoods or a world-ending villain. Phaister
is a showy performer with sporting mischief; Nemu/Kuro are curious and playful
outside their imposing competitive cast. Sean commits, Zack finds precise angles,
Dante holds difficult space, Cheska shapes useful routes. Reworks should strengthen
those differences, not turn all six into generic destructive spellcasters.

The whole-kit/default-alternative review is now in [HERO_KIT_REWORK_DECISIONS.md](HERO_KIT_REWORK_DECISIONS.md). It records all twelve choice pairs, source contradictions, candidate replacements, lore boundaries and actual-play acceptance. Candidates are not marked implemented or balanced.
