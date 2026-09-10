# Nationals polish: coherence, identity and memorable play

**Standing owner art requirement, 2026-09-10:** all new/reworked models must look
like they came from TUMP. Keep clean chunky forms and purposeful detail consistent
with the actual game. Visual fidelity must not drift into realistic anatomy or
surface noise. Motion can gain weight and precision while the models stay cute
and blocky. [Art_Direction.md section0](Art_Direction.md#0--new-models-must-belong-to-tump)
defines the rule and review criteria. Section0.1 also requires animation polish
to include casting and skill VFX, with distinct action sequences and no repeated
animation unless reuse is necessary.

## 0. MISSION

The current strategic roadmap for game feel, presentation and Nationals polish.
The systems-expansion era has supplied enough machinery. The next win is making a
player remember the moment they ran back for a slipper with the taya closing in.
**Make the core coherent, then author the whole experience.** Both modes ship;
Classic remains the tournament ruleset unless the human changes that decision.

This file decides **what matters next and why**. It is not another execution queue.
When an increment is selected, engineering belongs in [TODO.md](TODO.md), animation
and Blender work in [ASTRA.md](../ASTRA.md), and human judgments in
[Attention.md](../Attention.md). Reuse the existing entry where one exists. Do not
copy this whole roadmap into those queues or track completion here. ASTRA and
CLAUDE below are ownership lanes, not instructions to contact another conversation.

UI/HUD design is outside this roadmap. So are new modes, heroes, abilities, meters,
controls and progression. [FUTURE.md](FUTURE.md) is the retired systems-era roadmap;
[INSPIRATION.md](INSPIRATION.md) remains research and reasoning, not a next-work order.
[CLAUDE.md](../CLAUDE.md) governs repository work and [VISION.md](VISION.md) governs
the product. Existing balance and authority contracts stay intact.

**Evidence boundary, revised 2026-09-09 from `58e0f57f`, game source still
`7612a04c8a1a`:** this preserves the earlier source/asset-reference and report
evaluation, with selective source checks, not a fresh playtest. New art, sound and
staging directions are proposals, not freshly observed defects. No
Unity or Blender run, new motion capture, or listening approval was performed.
The earlier evaluation inspected old lineup and Ilalim captures as history only;
they predate the latest art, map and animation work and cannot certify today's look.
Below, **confirmed** means visible in current code or serialized references;
**recorded** means an earlier observation; **judge in play** is a proposed quality
test, not a defect claimed from a screenshot nobody took.

## 1. DIRECTOR'S VERDICT AND CURRENT STATE

**Yes, the first roadmap was too conservative.** Executed well, it would chiefly
make this a cleaner version of the same game. Its best insight was the retrieval
loop; its mistake was letting isolated repairs stand in for production direction.
Keep those prerequisites, then invest in a complete audiovisual sequence, distinct
places, a cast with presence and an ending worth remembering. A competition win
cannot be certified by a plan; the ambition is a visible difference in ordinary
play, not a longer list of closed issues.

### ALREADY STRONG

- **The retrieval premise is specific and worth protecting.** A safe throw creates
  an exposed possession, then a dangerous return. Classic does not need a power
  system to produce a memorable play. Street Hype already recognizes skill without
  changing the score.
- **The feedback infrastructure is substantial.** `MatchFlair` distributes world
  presentation; `NetCue` carries sound; `HitFeel` supplies victim-specific camera
  holds and directional impact; `Hitstop` supplies bounded shared punctuation.
  Camera response, throw anticipation, block/bank/near-miss recognition, replay
  interest and ultimate introductions already exist. Protect their authority and
  deduplication rather than replacing the stack.
- **Animation is authored, not absent.** All eighteen hero actions are on their
  respective rigs and both custom rigs; all twenty shipped character entries carry
  `slide`. [Roster import evidence](reports/roster-clip-import-v5.json) resolves the
  serialized clips. The six motion identities in ASTRA.md are meaningfully different
  in their pose design. This proves coverage and import, not excellence in motion.
- **There is already a Filipino world to finish.** Lata label art, sourced tsinelas,
  sari-sari dressing, pisonet, pares, the LRT and the intact sourced jeepney are
  concrete identities. Wholesale asset replacement would discard that investment.

### FIVE QUALITY GAPS TO ADDRESS

1. **Retrieval contradicts itself across body, hand and sound.** Confirmed:
   `ViewmodelArms.PlayAction("slide")` selects `LungeClip`; slide requests `dash`,
   and shove's `bump_swing` aliases that recording. Low retrieval reads as combat.
2. **Animation approval stops at structural evidence.** TODO section 151.16 records
   frame-zero probes; section 151.20's clip-count comparison rejects valid rigs.
   Import cannot approve clipping, transitions or recovery. Those remain unverified.
3. **Finish varies between layers.** TODO section 131.3 records five sourced VFX
   families, while section 131.6 leaves compositions unfinished. Confirmed:
   `HeroHazards.CreateThunderstrike` still draws the flat `ThunderShockRing` star;
   `StreetParesInteractive` reacts with `slipper_bounce` and `ImpactBurst`.
4. **Map-wide presentation lacks an approved target here.** Roadside repairs did
   not define lighting, depth or ordinary camera composition. This is a planning
   gap, not a fresh finding that every map looks bad.
5. **The whole experience lacks current feel approval.** Sound hierarchy, camera
   recovery, transitions and ending have not been judged together. TODO section 151
   fixed the listener and remote wind-up/slide cues; their usefulness under music
   and abilities needs ears, not another implementation. Sourced sounds remain
   provisional until heard in play.

### FIVE THINGS PEOPLE SHOULD REMEMBER

- The tin crack, sudden opening, low retrieval and taya's missed reach as one scene.
- A bank whose contact and changed flight can be followed without an explanation.
- Three Filipino places, each with its own light, depth and sound.
- Six heroes whose posture, timing and powers belong to the same authored person.
- A final play that resolves into a character response and clean musical closure.

## 2. POLISH NORTH STAR

- **The slipper is the promise; retrieval is the payoff.** Keep the shoe, taya and
  escape route readable from release through recovery.
- **Commitment has a beginning and an aftermath.** The reach, tag and shove must
  disclose what a body is doing before effects announce its consequence.
- **The lata has the clearest sound in the street.** Its first knockdown deserves
  more presentation priority than ordinary impacts or incidental scenery.
- **Each hero has a verb-shaped silhouette.** Sean drives, Zack snaps and carves,
  Dante plants, Cheska draws and holds, Nemu drifts and pulls, Phaister performs.
  More authored, not more noisy.
- **The neighborhood frames the action.** Spend cultural detail at landmarks and
  roadside edges; preserve quiet ground around a loose slipper.
- **Another player must understand the same event.** Local polish that disappears
  for the defender, joiner or spectator is unfinished polish.

### What the two modes should feel like

**Classic: tactile, intimate street rivalry.** Sun and shade frame a small can,
rubber skims the road, approaching feet tighten the space, and a missed hand makes
a tiny escape thrilling. Ordinary pickup stays calm; banks, commitment and timing
supply the spectacle. Its sparse mix, grounded contact and neighborhood presence
are a complete aesthetic, with the same quality of transitions and ending as Hero
Strike. Simplicity should feel chosen at every beat.

**Hero Strike: six strong personalities interrupt the same street tension.** The
street remains visible beneath the power. A cast has recognizable preparation,
a readable job, one peak and an aftermath returning attention to possession.
The pleasure is seeing a hero create or deny a retrieval opening. Coordinated
motion, material, sound and timing make powers feel expensive.

**Screenshot identity:** voxel silhouettes, authored can labels, sourced footwear,
chalk on quiet road, locally specific facades, strong depth and short comic-like
impact punctuation. Their composition should identify this game beyond its voxel
style. Preserve canonical art and the established toon/outline treatment.

## 3. CORE MOMENTS

Quality bars for existing actions, not orders to add an effect to every beat.
The throw/lata/retrieval/chase sequence gets the majority of effort.

### Throw, consequence, exposed slipper

**Charge/release:** body and viewmodel wind-up plus relayed `throw_charge` already
provide anticipation. Power builds through shoe and shoulder without vibration;
the taya can hear preparation while looking elsewhere. `Carrier` already plays
the throw, varied release cue and replicated flair. Hand release must coincide
with the world slipper leaving; flight, bank, bounce and rest stay readable across
light and dark road. Judge short legal charge, long hold and moving release.
Do not alter flight, collision or aim to repair presentation.

**Lata knockdown:** preserve the replicated 45 ms freeze and directional punch
(`Lata.AnnounceUprightChange`) and preferred `lata_impact` / `lata_knockdown` sounds.
Tin attack, tipping and settling should read as one consequence. Clear transients
so the shoe becomes the next focus. Camera strength 0.8 has no distance attenuation;
judge the distant observer before strengthening it. The opening should be clear
without imposing the nearby player's camera violence on the whole court.

**Reset:** existing channel cues, grab and restoration end on one clear upright-can
beat. Judge cancellation and sabotage: no final cue may promise safety before the
authoritative completion. No cinematic pause.

**Bank / near miss / block:** `Slipper` and `MatchFlair` already recognize them.
Show bank contact then redirected flight, a small passing read for a miss, and the
defender meeting the shoe for a block. Preserve block burst, flash and body squash;
keep these beats below knockdown prominence. No false tin hit, new labels or Street
Hype redesign. Improve the action that earns recognition, not its display.

### Enter, commit, escape or get caught

**Approach / normal pickup:** attention shifts to shoe-taya distance. Landed-shoe
presentation and footsteps must survive texture, shade and powers at eye height.
No omniscient chase sound revealing unseen opponents. Ordinary grab remains calm,
confirms possession once and joins hand to shoe without apparent teleportation.

**Committed slide:** low entry, reach toward this shoe, catch only on real pickup,
then vulnerable rise. A failure retains skid and recovery without catch confirmation.
The authored 0.95 s clip and 0.14/0.25/0.342 s contact samples are review points, not
new gameplay timers. Align FPP with that body commitment; keep the taya trackable.

**Chase / lunge / punch / tag:** foot planting and sprint-turn-action blends sell
pursuit. Stationary punch is arm-led; lunge spends the body and has a readable missed
recovery. Approaching steps communicate distance. `MatchFlair` already supplies
tag burst, freeze, flash, camera and voice: let contact win, then return control
without extra holds. A tiny escape reads from the missed reach and continued
momentum, followed by space in the mix. No new escape detector or reward system.

**Shove / trip / recovery:** shove is an outward push, distinct from punch and slide,
with different hit/miss sound. Trips and get-up disclose loss and return of control
without lengthening mash rules. TODO section 151.6 measured Ilalim's live trip hazard
outside the competitive box; no added hazards. Review transitions around one
selected family, including interruption and remote playback.

### Hero Strike and the ending

**Skills / ultimate:** review a hero's three casts in order, body/FPP, effects,
audio and return together. The first skill discloses direction/footprint; the second
shows its own job. Sean's chambering is not an immediate blast. The ultimate earns
one peak. Judge with effects hidden, then during contested retrieval.

**Round / match closure:** existing countdown, voice and music transitions clear
the previous round and establish the new taya. Preserve requested clean music cuts.
`MatchResult` already handles result audio; do not re-add a jingle. `CharacterAnimator`
maps the emote relabelled victory to `crouch`, but that does not prove the ending
calls it. Trace the live path before selecting a celebration. This is A tier after
the core reference, with results UI untouched.

## 4. CHARACTER DIRECTION AND HERO SIGNATURES

Protect voxel proportions, canonical skin, faces, hair geometry and identities.
No wholesale redesign or retargeting. The highest-value model pass fixes a
visible gameplay-distance silhouette, clothing overlap or material separation
problem on one character. Review lineup and real camera distance before details.
Improve pose, clothing readability or shader response within those constraints;
canonical hair is not a silhouette-cleanup free-for-all. Keep existing Generic
rigs and authored action names.

Sean's slide is the first motion reference: ASTRA task 3 corrected short-arm reach
with torso roll. Verify it before reauthoring it. `character-female-a`'s recorded
accessory/sleeve floor-contact ambiguity is a separate single-rig review. Later,
select one idle-to-movement or locomotion-to-action family on one character when
it strengthens presence; do not commission six locomotion sets by default.

### Compact hero grammar

Motion below builds on ASTRA section 1's authored work; paired VFX/audio direction
is the proposed next quality bar. Preserve established accents and role colors.
Shared finish means readable onset, material intent and clean decay, not one
builder recolored six ways.

- **Sean: propelled confidence.** Hip coil, one-axis extension, braced arrest;
  arms rake back. Fire sweeps and tears along travel, with a compact body and sparse
  embers. Pressure build, breathy release and brief hot crack support the motion.
  Supernova's signature is the body becoming the descending impact. Ignition
  chambers a future throw; old sourcing prose calling it a projectile must not
  turn it into an immediate blast. Avoid bloom balls and sustained fire noise.
- **Zack: bladed electrical precision.** Side-on counter-rotation, lateral skate,
  asymmetric aimed call. Branching paths and discontinuous light differ from
  Sean's continuous fire. Tense chatter resolves into a dry crack and short decay.
  Bolt Sprint has no impact beat; do not force one onto locomotion. Thunderstrike
  should be remembered as aimed bolt/contact, not its flat saturated ground star.
- **Dante: planted mass.** Low head, wide braced base, downward force; the carapace
  widens him rather than replaying the stomp. Opaque broken planes, fissures and
  restrained dust carry weight. A compressed low body sound with a distinct crack
  supplies impact without sustained rumble. Titan Fissure splits the ground;
  no leap, glowing shield bubble or generic round explosion.
- **Cheska: exact control.** Upright body, one forearm drawing a plane, precise stop
  and held shape. Facets, tapered shards and crisp boundaries make ice solid.
  A fine formation sound locks into a short crystalline attack and sparse tail.
  Glacial Nova's release-to-stillness is its signature; avoid blue smoke, whiteout
  and Phaister-like ornament.
- **Nemu: unsettling weightlessness.** Limbs lead the torso; she rises without a
  push-off and is pulled inward. Dark negative space, inward wisps and the live
  Kuro presence carry supernatural identity. Inward air and an uncanny hollow
  release should identify her without purple light. No heavy stomp, generic cloud
  or automatic impact hold on Phantom Veil.
- **Phaister: theatrical witchcraft.** Off-axis flourish, deliberate presentation,
  open held finish; Shadow Blink's instant departure is the exception. Written
  sigils, torn vertical wisps and sparse overhead corona belong to distinct
  actions. Incantatory texture, stamped attack and clipped magical tail suggest
  performance. Grand Coven earns its flourish and reveal without stacked circles.
  No Nemu-like drifting blink or Cheska-like shortest-path ritual.

Test both ways: hide VFX and identify movement; hide the body and distinguish the
effect/audio family. Custom characters borrowing a kit must retain its tells.
Review one hero's full three-cast sequence, then select only necessary corrections.
`ViewmodelArms` already has hero actions; ASTRA task 6 is alignment, not eighteen
missing clips. Do not change hit timing, range or recovery rules to fit a pose.
Extreme angular-speed measurements are not a polish score.

### Ultimates: explicit two-phase investment

**Phase A, CLAUDE: establish the reusable presentation contract.** ASTRA task 8
records no cinematic action name. Existing `UltimateStarted` / presentation flow
and introductions already work (TODO section 134.7); do not replace them or redesign
their UI. Select the smallest route: stage the existing cast when it supplies the
pose, or expose a named separate presentation action where it truly needs one.
A shared integration hook must permit different staging, not mandate a shared pose.

The contract states accepted-cast trigger, action selection, authoritative onset,
local/opponent/spectator camera behavior, existing audio/VFX cues, deduplication,
overlapping ultimates, interruption/round-end cleanup and recovery. Exercise one
real cast on host/joiner and the existing observer path, plus refusal and
interruption, before commissioning a cinematic clip. No longer freeze, new input
lock or altered damage window. ASTRA section 2's proposed global cinematic freeze
is not authorization to expand shared hitstop. Reject any version requiring it
and keep the signature within existing play timing.

**Phase B, ASTRA then CLAUDE then HUMAN: one authored signature at a time.** Start
with Sean's kit reference. One session reviews or authors only his ultimate
motion/pose on the agreed hook; a separate bounded integration aligns camera,
effect and sound. The other five follow their own grammar and release/recovery.
Do not stretch existing cast clips to hide a missing intro hook. A brief lighting
response is optional; existing weather may already do the job. Decline extra
lighting if it washes out role colors or hides the shoe.

Advance only after the signature works in contested retrieval from caster and
opponent viewpoints. If the hook adds no visible value, retain the existing cast
and direct its staging. Missing cinematic support must block unreachable assets,
not improvements to live casts.

## 5. VFX / CAMERA / GAME-FEEL PASS

**Direct emphasis before adding response.** Sound, body and camera should agree
on which instant matters. Tune timing, direction, duration, recovery and distance
before amplitude. The core lata/retrieval camera pass belongs in S tier; the wider
action-family pass is A. Use `CameraRig` and existing spectator/replay interest.

- **Release / pickup:** small arm/camera timing agreement where needed. Possession
  gets clarity, not a celebratory hold or automatic FOV kick.
- **Lata / tag:** contact-aligned directional punctuation, then quick recovery to
  shoe and opponent. Judge the distant-view cost of lata's unattenuated punch.
- **Slide / lunge:** restrained inertia and stable horizon; track the taya through
  entry and recovery. Judge misses, repeated use, and mouse, pad and touch comfort.
- **Skills / ultimate:** emphasize the hero's real release or consequence, not
  every particle onset. Distinct timing may require less camera movement.
- **Start / ending / spectator:** establish place and retain the final action when
  control is already outside live play. During play, keep caster, consequence and
  retrieval opening in context instead of cutting to the biggest effect.

Reject forced live-player cuts, aim displacement, horizon rolls, nausea-inducing
inertia, constant shake and automatic FOV pumping. Keep stronger framing in existing
spectator/replay contexts where appropriate. Do not add settings/UI here or assume
an unbuilt reduced-effects setting exists.

Use the existing stack. Shared `Hitstop` is bounded to 20-80 ms and ignores overlap;
`HitFeel` holds only the victim's view, while the world keeps moving. They solve
different problems. Do not globalize ordinary hits or add caster feedback that
reveals offscreen victims. Existing camera holds were repaired for drift in § 150;
judge repeated impacts at the end of a chase before increasing any strength.

The first repair within Zack's hero pass is **ThunderShockRing**: retain the lightning
star's directionality, break up the flat filled appearance, and allow the existing
bolt/contact to lead. Keep gameplay radius and the sourced bolt intact. Later,
select one unfinished composition from § 131.6: Carapace's body plates, Barricade's
shard silhouette, or Sean's ultimate impact. An unused sprite sheet is an ingredient,
not an automatic replacement order.

Phaister's current ward already conforms to the sidewalk. A flat sourced quad was
rejected for losing that property; do not retry it. Nemu's actual ultimate uses
`SpawnKuroUnbound`, not the old `SpawnSeanceVoid` still reached by a showcase path.
Judge the live ability rather than polish a probe-only effect.

Follow VISION § 2: normal skill radius guidance is **1.6-2.3 m**; trails are governed
by their live cap, not one disc. The older sourcing document's radius range is not
the current bar. Preserve authoritative per-ability geometry; compare to the
[commit-stamped footprint report](reports/ability-footprint-8327a1ec7671.md) and
refresh it only when that geometry changes. Keep the **12% white-frame ceiling**,
but also demand visible lata, chalk, players and loose slippers in overlapping
effects. Passing a luminance bound alone does not establish readability.

TODO § 151.8 already measured maximum-effects load and found no accumulating leak.
Its warm cost is a diagnostic reading, not target-device certification. Do not start
a pooling rewrite or simplify sourced models without a new measured player problem.

## 6. AUDIO DIRECTION

**S tier within the core sequence:** space, preparation, release, tin, opening,
approaching feet, scrape/catch, tag or escape, release of tension. Flight connects
hand to contact without a constant whistle. The can transient commands knockdown;
its tail yields to footsteps and retrieval. Missed slides never confirm a catch.

Direct loudness, frequency and duration together. Tin needs a clear upper attack,
approaching steps a usable midrange, and hero weight must not smother either.
Trim competing tails and ambience before raising foreground gain. Leave holes in
the mix rather than sounding every beat. These are audition hypotheses, not fixed
EQ numbers. Preserve preferred can recordings; section 4 gives each hero its own
preparation, peak and decay. An ultimate is not simply a louder skill.

**First audition:** slide scrape/catch against shove in contested pickup. Pitch
variation exists but cannot make one recording mean two decisions. Human chooses
the distinction; engineering integrates the approved timing and relay.

**Space:** `AudioDirector` follows the active camera; world voices use **2-32 m
linear rolloff**. Attention section 18 records far-diagonal and FPP/TPP questions.
Hear approach, behind-camera landing and slide from both sides, including a joiner.
Tune one family only when inaudible or misleading, not all SFX. The 42 elemental
replacements and earlier sourced cues remain provisional (Asset_Sourcing section
5.5). Pisonet's score-sting misuse is fixed; its pitched click needs a coin-sound
judgment. Pares should not impersonate a bouncing slipper.

Music already has late-round pressure lift and announcement ducking. Hear a full
ending with the OST: feet and tin survive the lift, victory registers, deliberate
cuts remain clean. No new adaptive-score system, music fades, constant chatter or
global chase loops. Human-recorded tsinelas, neighborhood sounds or Tagalog exertion
can serve S/A identity when filling a specific gap, one family at a time (Attention
section 9). Judge headphones and ordinary speakers for nearby watchers as well.

## 7. MAPS / ENVIRONMENT PASS

**Each map needs a final art-direction pass, delivered in small increments.**
The unit of direction is the whole map; the unit of work is one lighting/composition
pass, one landmark or one frontage. A prettier corner alone does not finish a map.
For each, select three existing gameplay views: first throw toward the lata,
retrieval at eye height, and the defender's reverse view, plus one existing
spectator angle. Stage foreground edges, readable action midground and a background
landmark. At least one should make a strong hero shot from the ordinary camera.

**Players, lata, loose shoe and chalk first; landmark second; dressing last.**
Start with key-light direction, ambient fill, broad shadow shapes, contact grounding
and material response. Check slippers in sunlight and shade, can metal/label
separation, asphalt versus pavement, cloth versus hard surfaces and characters
against facades. Use existing rendering controls before new props or shaders.
No automatic PBR conversion, wet-road makeover, volumetric/bloom upgrade or renderer
replacement. A new rendering feature needs visible benefit and measured cost on
target hardware, including the supported lower setting. Warm probe cost alone
cannot approve it. Cooler shadow does not mean saturated defense-blue streets.

Read [Art_Direction.md](Art_Direction.md) before choosing new environment colors:
offense orange and defense blue identify roles, not decoration. Preserve authored
assets under CLAUDE § 6.0, including the jeepney's original materials and livery;
do not recolor or decimate it to enforce a generic palette. Some old art/map prose
still describes Godot-era geometry. Current Unity scenes, builders and later fixes
decide what exists.

### ESKINITA

**Strength:** the intimate sari-sari, sampay and kanal street is the best immediate
frame for Classic's personal chase. Its narrow lateral space makes the two long
approaches important. **Recorded roughness already addressed:** § 134.14 grounded
cars and extended the geometry gate; do not list floating cars as still broken.

**Direction:** close neighborhood warmth and claustrophobic chase. Warm roadside
life, neutral readable ground, layered facades, sampay, roofs and cables frame the
long approaches. Light should distinguish the near sari-sari frontage from the road
and far layer without an orange wash. Keep rich edges and broad quiet lane values.
The sound is low domestic/shop life with occasional distant activity, not chatter
over footsteps. Existing cloth or prop motion is a peripheral accent only.

**First increment:** one lighting/composition pass across the three views, then
frontage refinement if needed. Done when the normal and reverse views identify a
neighborhood, the near/far layers separate, and the retrieval line survives both
shade and narrow-lane Hero Strike overlap. Do not widen the map. A decorative
reaction is optional; the stronger whole-map view is the S-tier outcome.

### BAYAN PLAZA

**Strength:** the open barangay square, monument, church/basketball vocabulary and
trees can provide a calmer, more public counterpoint to Eskinita. **Correction:**
`BayanPlazaMonumentFix` already removed the blocking intrusion into the defender's
box; the older map document is history on that point.

**Direction:** open civic space and confident sunlight. Broad sunlight/shadow
rhythm, a clear monument and church/tree silhouettes create a public barangay
feeling. Foreground paving leads into open action space and a far landmark. Empty
ground is a positive part of this identity. Use a more distant, open street bed
than Eskinita and sparse canopy motion; no extra stalls or clutter to fill frames.

**First increment:** one monument-side lighting/composition pass checked from the
reverse defense view. The fix retains a small walkable visual overlap: judge the
seam, do not replan collision. Done is deliberate open ground, grounded characters,
a clear landmark and a view distinct from Eskinita without map text.

### ILALIM NG TULAY

**Strength:** the carriageway-as-box, overhead guideway, PC Express, pisonet/pares
edge and jeepney provide the richest local specificity. Train motion and a moving
field-recording source already exist. `LrtTrainFlyby` now defaults to a rare
300-second interval; do not restore the old document's 24-second rhythm or retune
its gameplay-linked pass while adding ambience.

**Direction:** cool-neutral bridge shadow, restrained shop-light pockets and urban
weight. Guideway mass sits above a clear carriageway; pisonet/pares/PC Express frame
the edge, the jeepney anchors a distant silhouette. Depth and selective highlights
reveal metal without road darkness or glare. Avoid a neon reskin. A subdued urban
bed yields to footsteps, while the existing rare train provides the interruption.

**Foundation concern is retrieval continuity.** The
[six-seed report](reports/bot-sweep-2fde55d32246.md), on its recorded commit, has
0-48 idle penalties in Hero Strike on Ilalim, with almost all the dead time in one
seed. That is a location to investigate, not proof of an unreachable slipper or
broken map. Trace that recorded outlier before polishing the implicated retrieval
area; independent lighting direction need not wait. Do not infer a balance change.

**First art increment:** shadow-to-shop composition across the three gameplay
views, then one pisonet/pares frontage audition after the route check. It already
reacts; the question is whether the sounds and bursts make it
feel like a food/computer street or another fight. Judge shop light and signage
against the bridge shadow, material consistency at the asphalt/pavement boundary,
and environmental noise during an ultimate. The jeepney's metallic factors and
reflection probe were repaired in § 151.5; approve the finish visually before
changing lighting again. Done is one legible, recognizable roadside zone whose
reactions leave room for footsteps, can and slipper. No new hazards or rules.

**Life is selective direction, not mandatory animation everywhere.** Prefer an
existing reaction with recognizable cause, brief local response and quiet afterward.
Do not trigger a gag to compete with a tin hit or chase. Still frames and mix come
first. One meaningful shop response can earn A tier; another decorative background
motion accent remains C. No automatic traffic, moving everything or hidden players.

## 8. REVISED NATIONALS PRIORITIES

### Two layers, one quality reference

**Foundation / coherence** removes contradictory signals: body/FPP mismatch,
misleading sounds, unreadable ground, broken blends or missing remote feedback.
Fix the relevant blocker. Motion capture earns its place by answering a selected
move, not by becoming a tooling project.

**Transformative polish** gives a whole sequence and place deliberate rhythm:
light and composition, motion and sound identity, camera emphasis, quiet and payoff.
Start when its own prerequisites work, without waiting for every minor defect to
close. Each increment states its intended perceptual change and ends in comparable
gameplay evidence. An improvement visible only in an isolated preview is unfinished.

First approve the core sequence on Eskinita; extend that standard to its ordinary
map views and one hero's complete presentation. Carry the quality bar across maps
and heroes, not the same treatment. Start and ending must bookend the experience.
Tiers describe impact, not task size. Release/security blockers keep their separate
priority; this presentation plan does not demote them.

### S TIER: the production-value change

1. **Complete throw/lata/retrieval/chase reference.** Foundation: body/FPP, sound
   meaning and motion evidence agree. Transformative: author anticipation, tin
   payoff, footsteps, contact and recovery as one scene. Audio and camera direction
   are part of S, not optional later fixes. Highest repeated player/judge return;
   narrow integration protects control and authority.
2. **Map-wide art direction, Eskinita first.** Deliberate lighting, composition,
   material response and Filipino specificity in ordinary views. Establish one
   map, then give Bayan Plaza and Ilalim their distinct treatments in separate
   passes. This outranks scattered seam repair; readability and device cost gate it.
3. **One complete Hero Strike reference, Sean first.** Character presence, three
   casts, body/FPP transitions, VFX and sonic identity, with the existing ultimate
   as the peak. Several small sessions deliver one result. Phase A of the ultimate
   contract is a prerequisite only when a new presentation action is selected.

### A TIER: carry the standard through the experience

- **Five remaining hero treatments and Phase B ultimate signatures**, one hero and
  one art/integration task at a time. Zack's star repair belongs within his grammar,
  not ahead of whole-game identity just because it is easy to name.
- **Transitions and match closure.** Around one selected family, review idle to
  sprint, sprint to throw, throw to recovery, pickup to sprint, slide/get-up, hit
  to locomotion and cast exits, including interruption and remote playback. Start
  establishes the street; new taya begins without stale effects; final play resolves
  into character response and existing music/audio closure. Trace `MatchResult`
  and victory-to-`crouch` before selecting one celebration. No results UI, extra
  jingle, live-player camera cut or timer change. The last impression matters.
- **Supporting sound/camera hierarchy and meaningful environmental reactions** in
  full play with the OST. One bank/block family, one map ambience or one shop
  response can strengthen the whole scene after the reference is approved.
- **Ilalim's route outlier**, a foundation investigation rather than promised
  spectacle. Reproduce the recorded seed and log the slipper's rest position;
  escalate demonstrated obstruction, not an inferred map or balance defect.

### B TIER: observed local roughness

One rig's accessory contact, material seam or noncentral action mismatch surviving
main passes. Promote it if it obscures a player, misstates possession or blocks a
selected S-tier sequence. Do not repair every model because a metric is unusual.

### C TIER: optional decorative rewards

Background motion accents, detail invisible at match distance and extra dressing.
Human-recorded audio is not automatically C: a distinctive foreground source can
serve S/A, while another background accent can wait. Remove tasks whose only value
is being measurable or whose addition duplicates an existing beat.

### DO NOT DO BEFORE NATIONALS

No new modes, heroes, abilities, meters, controls, progression, hazards or UI/HUD;
no wholesale roster, audio-library or renderer replacement; speculative pooling,
asset decimation or sourcing sweep; six unreachable cinematics; larger footprints,
stacked floor planes or white flashes; constant shake, chatter or street motion;
wet/glossy everything; arbitrary prop density; generic capture frameworks or broad
repeat probes without a new question. Orphan-file cleanup and comment-count repairs
remain engineering housekeeping, not transformative polish. Do not revive old
FUTURE/INSPIRATION prompts as the default order.

No new gameplay is recommended. Changes to routes, collision, scoring, control or
freeze duration need a separate human gameplay decision and are outside this plan.
Do not retune slide recovery from bot usage alone. Preserve the geometry, white-frame
and overlap constraints in section 5 throughout every art pass.

## 9. HUMAN QUALITY BAR AND STOPPING RULES

The first view establishes a Filipino street; the first knockdown creates a clear
opening; the first chase sells ordinary pickup and risky slide; the first skill
shows its job; the ultimate has one signature; the ending resolves the final play.
Use existing player/replay/capture tools. No large new test campaign.

**Ten ordinary screenshots:** sample ten times without selecting pretty frames
from an unedited segment. Record commit, mode, map and viewpoint. Count frames with
intentional silhouettes, depth, landmark, color/value hierarchy and VFX coverage;
compare before/after and name repeated failures. **8/10 appealing ordinary frames**
is an initial working aspiration, not a measured result or permission for two
unreadable frames. Necessary in-view gameplay information must remain readable
throughout. Apply this as each map pass lands; a staged hero shot cannot certify it.

**Thirty-second clip:** use ordinary unedited gameplay with natural audio. When a
bank, knockdown, retrieval, missed lunge or ultimate occurs, can an uncoached watcher
understand it and react? Two or three reactions are an aspiration when those events
occur, not a quota requiring extra spectacle during quiet play. If nothing eventful
occurs, judge pacing in a longer segment rather than inventing events. Ask what was
memorable without suggesting the answer. Watch muted for motion clarity, listen
without the picture for sound identity, then judge them together.

**Player and judge:** include FPP thrower, defender/opponent, joiner and existing
spectator view where relevant. Someone behind the player should follow the same
lata-to-shoe-to-taya story. Spectator framing keeps cause and consequence in view,
not just the largest explosion; maps supply readable backdrops. Headphones alone
cannot approve a room-facing presentation. Compare the same action, map, viewpoint,
mix level and supported quality setting. Retain a change for perceptible benefit,
not asset count; revert if it costs tracking, comfort or clarity.

**Whole experience:** once references work, judge one full Classic match and one
Hero Strike match with a watcher, including start and ending. Classic must stand
on its own without powers; Hero Strike should still be remembered for street-game
decisions. Judge each remaining map as its pass lands. Mechanical checks prove
wiring; humans approve timing, taste, sound, comfort and competitive readability.
Record actual approvals in Attention.md with build/viewpoint, using section 17.2
for slide feel, section 18 for spatial mix, section 13 for sourced cues and section
9 for recordings. Reuse existing entries; do not duplicate an approval ledger here.
Nothing in this revision claims those judgments or playtests are complete.

## FIRST POLISH BATCH

Ordered recommendations, not a parallel work assignment. Select one available
increment per session, recheck its queue entry, and stop at its stopping condition.
If a human decision is pending, choose an independent item rather than invent its
answer. Ask before credit-consuming external work; never perform a usage reset.

1. **HUMAN: establish the retrieval reference.**
   **Problem:** the corrected spatial mix and authored slide have not been felt.
   **Why:** they decide whether this game has its promised tension.
   **Scope:** one Classic round on Eskinita with ordinary pickups, successful slides
   and a deliberately failed slide; hear shove and slide from the defender's side.
   **Done:** name the confusing beat, or explicitly approve the existing choice and
   recovery. **Verify:** a short captured sequence with build/viewpoint and the
   human's judgment. Use Attention § 17.2 and § 18; no balance edits in this increment.

2. **CLAUDE: make one moving slide reviewable.**
   **Problem:** frame-zero probes cannot approve animation, and clip-count equality
   rejects valid added actions. **Why:** art revisions need trustworthy pictures.
   **Scope:** TODO § 151.16's motion-strip support, including the slide contact
   samples, plus § 151.20's relevant base-clip preservation assertion.
   **Done:** Sean's slide shows entry, contacts and recovery through the game shader
   and camera; extra clips pass while a missing required base clip fails.
   **Verify:** one versioned strip and the focused assertion. No general capture
   framework or roster-wide probe campaign.

3. **ASTRA: approve and, only where needed, refine Sean's retrieval slide.**
   **Problem:** short-arm reach was corrected numerically, not judged in motion.
   **Why:** this is the signature action on the rig with the clearest known reach
   challenge. **Scope:** Sean's existing `slide` entry/contact/recovery only, under
   ASTRA task 3; preserve duration, rig and gameplay values.
   **Done:** a low skid, readable reach and vulnerable rise with no visible floor
   penetration or abrupt return; keep the clip if it already passes.
   **Verify:** increment 2's strip plus full-speed player/observer motion. If an
   export changes, rebuild roster references and verify the resolved clip.

4. **CLAUDE: give the existing first-person slide its own reaching motion.**
   **Problem:** `slide` selects `LungeClip`. **Why:** the owner currently sees the
   wrong decision. **Scope:** ASTRA engineering context task 4, one arm action and
   its blend into carry/recovery; keep movement and existing network verb intact.
   **Done:** low reach and recovery agree with the body; failed pickup has no false
   catch; lunge remains distinct. **Verify:** successful and failed slides in FPP,
   with remote body comparison and the action path exercised on keyboard, pad and
   touch. No input or UI redesign.

5. **SHARED: separate slide and shove by ear.**
   **Problem:** `dash` and `bump_swing` resolve to one recording. **Why:** the taya
   needs to hear whether an attacker is retrieving or pushing.
   **Scope:** audition one slide scrape/catch treatment using existing sources or
   a human recording, then integrate only the approved cue distinction. Human
   chooses sound; CLAUDE handles alias, timing and relay.
   **Done:** the two decisions are distinguishable without drowning footsteps or
   implying pickup on a miss. **Verify:** blind alternation, then a contested pickup
   heard once on host and joiner. Route judgment to Attention § 18.1 and selected
   implementation to TODO; do not bulk-regenerate audio.

6. **SHARED: make the first lata knockdown the reference payoff.**
   **Problem:** repaired distributed feedback has no current cohesive feel approval.
   **Why:** this is the first spectator reaction and the chase's starting gun.
   **Scope:** one knockdown sequence, align existing tin attack/tip/settle and nearby
   camera punctuation; preserve the preferred recordings and replicated event.
   **Done:** one unmistakable hit, no doubled cue, then a clear retrieval opening.
   **Verify:** nearby thrower, defender and distant observer hear/see the same
   consequence; compare against unchanged feedback and retain it if already better.
   Human approves, CLAUDE changes only a demonstrated mismatch (TODO § 151.3 context).
   Close the batch by watching the resulting throw-to-retrieval/chase sequence with
   the OST, so separate successful fixes become one approved timing/mix reference.

## NEXT: MAKE THE REFERENCE TRANSFORMATIVE

After the first six increments, select a transformative S-tier outcome rather
than filling the schedule with B-tier repairs. These are ownership lanes, never
instructions to contact another conversation. Each selected task enters its
existing queue; do not copy all future proposals into TODO or ASTRA.

- **ASTRA:** one hero's full presentation review (Sean first, ASTRA section 1A),
  then only the necessary motion correction. Done when the three jobs read without
  VFX in full-speed in-engine evidence. A separate model pass is another task.
- **CLAUDE:** one approved cast's FPP/body/VFX/audio timing or transition seam.
  Done when owner and opponent see the same release and recovery in a player.
- **SHARED:** one map's lighting/composition pass across section 7's three views,
  Eskinita first. CLAUDE owns scene/builder/shader integration; ASTRA owns one
  selected Blender asset only if needed. Human judges depth, mood and clear ground.
  This does not widen ASTRA.md into scene-code work. A landmark is a separate task.
- **CLAUDE then ASTRA:** one Phase A ultimate contract increment, then one hero's
  Phase B motion/pose on the agreed hook, then separate integration and human review.
- **SHARED:** one match-ending review, then one approved body/audio/camera correction
  through existing hooks. Done when final action and closure feel connected.
- **CLAUDE:** Zack's ThunderShockRing shape/value/decay within his hero pass
  (TODO section 131.6). Retain bolt, star direction, footprint and timing; compare
  solo and Eskinita overlap, plus the white-frame gate. Bolt/contact leads and
  the ground opens between branches. No new VFX framework.

One Plus-sized session owns one hero review, animation family, model, map view
family or integration seam. Stop with evidence and a decision; integration and
human approval are explicit dependencies, not assumed outcomes. Keep useful
existing work when it passes. Ask before credit-consuming external work, never
reset usage, and never contact another conversation.
