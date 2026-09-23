# Seventh hero and water-village expansion

## Latest owner style correction: blocky hair, no eyebrows

The v4 swept volumes and eyebrows are REJECTED. The owner identifies two concrete
cast rules: blocky hair and no eyebrows. Remove both eyebrow meshes, replace Rafi's
hair volumes with native box/chamfer forms and preserve his own readable layout.
The HERO lineup is authoritative over the generated concept. Keep the fitted left
wrist cord, functional accessories, palette, body/FPP parity, rig and clip work.
The older continuous-hair/weighted-brow directions below are history, not current
acceptance.
Implementation plan: ten native chamfered hair boxes, short asymmetric crop with
three squared fringe lengths and a stepped crown. Remove the tied bun and both
brow polygons. Keep the headwrap/clip and expose the forehead and ears. Reuse
the copied builder mesh/palette/outline/bone pipeline. Inspect native four-angle
head/full body and HERO lineup before accepting this pass internally. Archive v4 and render the corrected native head plus HERO lineup.

## Owner art rejection and revised delivery contract, 2026-09-22

### Newest body-part quality pass: HERO ONLY

The owner reiterates Rafi belongs only to Hero Strike, never the Classic cast,
and asks for careful renders/refinement of every body part, especially bracelets,
accessories and hair. Roster.cs already puts him only in HeroPeople; AllPeople is
the asset union, not the Classic selection. Classic figures in the deck diagnostic
are not Rafi references. His only cast comparison is Sean/Cheska/Dante/Zack/Nemu/
Phaister. The published56a220a9 model is a checkpoint, not final visual acceptance.

Work order, retained before editing:

1. **Native part sheets:** use the canonical four angles for head/face/hair, torso
   and waist, each arm/hand, legs/sandals; then the full HERO lineup. Show native
   geometry/palette/outline, not an AI picture as implementation proof. Review at
   close-up and normal game distance, and keep the generated v3 concept as design
   reference while native hero proportions remain authoritative.
2. **Hair:** replace the plank-like front/crown pieces with a few connected,
   tapered low-poly volumes following a deliberate swept path. Keep broad roots,
   an asymmetric hook/forelock, exposed temple and compact tied tail; no floating
   locks, flat helmet cap, copied lightning/horns or photoreal strand texture.
3. **Face:** retain the native skull/flat graphic-face construction, but author
   Rafi's own focused eye shapes and weighted brows rather than donor smile-eyes.
   Keep a short restrained closed mouth. No broad grin, gills, face markings or
   realistic sculpted eyes/nose. Check frontal and quarter expression together.
4. **Wrist cord/bracelet:** a continuous fitted faceted rope loop with a small
   tied closure, navy binding and one restrained sea-glass bead. Fit it around
   the left wrist with actual clearance; avoid a thick rectangular cuff masquerading
   as rope. The right wrist stays clean. Body and FPP are the SAME authored geometry.
5. **Accessories:** make the hip rope a continuous elongated coil with clear hollow
   centre and an actual hanger, rather than a gear made of repeated cubes. Refine
   the headwrap's knot and tapered folded tails so they attach and drape visibly.
   Keep the one orange personal float clip, with a fitted fastening. No arbitrary
   repeated ornaments or invented cultural/tribal symbolism.
6. **Cloth/torso:** shape the V opening and collar, shoulder lining, sash knot/tails
   and wrapped sailcloth panel as distinct fitted volumes. Readable thickness,
   overlap, two purposeful seams and clean intersections. No badge floating above
   the shirt or giant flat belt that obscures the whole torso.
7. **Hands/legs/feet:** keep native simple no-thumb block hands; improve the palm
   depth/taper if the FPP reads edge-on and skinny. Fit the left cord to the revised
   wrist. Keep the short HERO-family legs, distinct shorts cuffs, supported feet,
   sandal sole/strap volumes and current pivots/grounding.
8. **Delivery check:** regenerate ONLY Rafi's model/palette/roster/portrait/arms;
   preserve original characters/builders/GUIDs and all33clips. Inspect the part
   sheets and full hero lineup, then changed body/FPP carry/throw/cast poses. Make
   specific shape corrections where necessary, not repeated unchanged broad tests.
   Resume the full preserved backlog afterward; no task is deleted or abandoned.

Part-review evidence: baseline five sheets showed blocky bracelet/coil/hair and
buried sandal straps. The first coherent continuous-form revision improves the
coil/collar/eyes/palms/sandals but exposed wrist-corner clipping and a rectangular
hair base. These are being corrected together; part studies remain actual native
four-angle source geometry, not concept approval. Both the old draft and failed
fit evidence are retained. The reference/cast checks use HERO characters only.

Latest part-v4 native views and v52 owner/observer/shared/peer motion checks now
complete this specific authored pass. The shipping body-clip gap discovered during
review is fixed with3Rafi-only serialized clips and explicit playback registration.
See reports/full-backlog-2026-09-21/rafi-parts-and-motion.md for limits and evidence.
Human art approval is not inferred; the broader assignment remains active.

The Lagoon deck fix completed its focused runtime comparison before this new
steering: separated thin board batches remove the exaggerated ink gaps, physical
walking collision is unchanged. Assets recovered via expansion-asset-transfer-v7;
broader final qualification remains pending. Current priority is this Rafi pass.

**Further owner correction:** the copied-builder draft's broad curved smile and
helmet-like hair are also rejected. Refine only Rafi: calm focused native eyes,
short restrained mouth, a shaped swept fringe with visible forehead/temples and a
compact tied back silhouette. Preserve his identity/outfit and the other heroes.
The owner also identified mismatched FPP arms. Root cause: ApplyCharacterStyle
still selected the generic arm/accessory fallback for Rafi, despite his extracted
arm assets existing. Route only Rafi through his source arm meshes and body palette;
confirm actual holding/empty/cast views. Do not redesign the other cast's FPP.
The first routing patch remained unreachable because NormalizeCharacterId also
omitted Rafi and returned classic. That registration is repaired; nativev51 passed
8owner/8observer cases with actual arm mesh identity, charging/release and inspected
cloth/skin/cord parity. The former v50 appearance failure remains in the receipts.

**Newest scope: a distinctive hero, not a retexture.** The owner says Rafi lacks
his own features. This supersedes treating the previous outfit as finished. Keep
the native rig/family proportions and water-trickster/boat-repairer identity, but
author a distinct readable silhouette and garment shapes, not just a face tweak.

Revised execution, saved before authoring:
1. Native hero lineup is the proportion/face/material reference. Generate one new
   concept with hooked swept hair, visible temple, compact tied tail, practical
   narrow headwrap and one recognizable float/cord detail. No borrowed signature
   hat/horns/lightning fringe, realistic anatomy, gills or invented cultural markings.
2. Choose a coherent silhouette from it: asymmetric short work overshirt, turned
   lining/shoulder flap, off-centre tie, tapered sailcloth hip panel and fitted rope
   coil. Purposeful broad forms first; avoid turning every surface into decoration.
   These are Rafi's individual fictional practical belongings, not traditional dress.
3. Retrofit ONLY tools/build_rafi_voxel.py recipe, native flat face and scoped hair
   transforms. Original builders/other heroes remain byte-identical. Short focused
   expression replaces the generic grin; retain native skull and simple hands.
4. FPP must use extracted Rafi arm geometry and AppliedPalette, including any final
   cuff/cord. The runtime fallback fix is already in DEV only. Regenerate Rafi's
   roster/portrait/arms together after the new model; keep all existing GUIDs.
5. Review native front/quarter/back/side plus HERO lineup, then actual holding,
   empty, throw and skill views. One focused correction if a real issue is found;
   do not restart already-passed water mechanics/network/replay tests for cosmetic work.
6. Resume every preserved backlog task afterward. Current recall Hero route is an
   independent existing-build check while image ideation runs; no existing task is dropped.

Rejected face/hair source/model/palette are retained in
ArtSource/rafi/rejected-face-hair-20260922. Built-in image generation is ideation;
its tool exposes no model-version selector. No paid API, reset or delegation.

**Latest owner change: no gills.** The active Rafi design, model, animation and lore
must contain none. Earlier gill references below and in archived concepts describe
superseded history, not an outstanding task. His water magic remains personal magic.

The owner rejected BOTH the separate-box Rafi and the sparse, regular lagoon dock.
Those images/assets are drafts, not approved art. The later request explicitly
requires Rafi to belong beside **Sean, Cheska, Dante and the HERO cast**, and says
to copy and retrofit the old voxel builder without editing existing characters.
`tools/build_rafi_voxel.py` is now a dedicated copy of `build_person_voxel.py`;
the original and all existing character models/builders stay untouched. The old
`build_rafi.py` is only an entry wrapper, with its rejected recipe archived.

Read and follow `CANONICAL_RENDERING_PIPELINE.md`, `Voxel_Person_Guide.md`, relevant
`Voxel_Person_Log.md` history and the current art rules. Keep their native donor
skull/face, seven-bone contracts, family remapping, chamfers and smoothed outline
normals. Refine the recipe, not a different mesh generator with the same skeleton.
Judge a fresh four-angle turnaround AND a current hero-cast lineup in Unity; the
old four-hero sheet was a style input to concept generation, not current proof.

Image ideation was explicitly requested and generated with the built-in tool.
`ArtSource/badjao/rafi-refinement-20260922/rafi-hero-concept-v2-no-gills.png` develops the
selected B identity: swept tied hair, shaped rolled work shirt, rope tie, repair
pouch, blue workcloth and personal orange buoy token. Native hero proportions and
face construction govern the model; generated anatomy/facet noise is not binding.
The tool does not expose a model-version selector, so its requested2.5 version
was not claimed as verified. No paid API or reset was used.

### Rafi's own character

Initial qualification found his prototype4/3/3stat row duplicated Zack. The
retained distinct-row rule is not weakened: Rafi now has4Bilis/2Lakas/4Tatag,
trading direct throwing power for endurance while retaining his agile utility
identity. This is a prototype tuning choice, not a human balance verdict; other
characters' stats are unchanged. Classic remains neutral and excludes Rafi.

Rafi is an inventive boat-repairer and athlete from a fictional Sama Dilaut community
in Tawi-Tawi. He notices the loose board, the awkward stair and the predictable
opponent, then immediately has an idea. He repairs his own gear and makes a small
buoy token from a useful offcut; the token is his personal keepsake, not an ethnic
symbol. His teasing confidence is warm rather than cruel. He will help fix a rival's
slipper strap, then bet that rival cannot read his next throw.

His weakness is overplaying a clever idea when a simple shot would do. Tournament
play challenges him to make the useful choice, not merely the funny one. Sean's
directness frustrates his elaborate feints; Cheska spots more of them than he likes;
Dante's steadiness makes an opponent worth studying. These relationships do not
change or redesign those heroes. Rafi's movement is an off-hand cut, an abandoned
heel-turn and a cupped hip-to-side wave release. No borrowed weapon, armor or outfit.
His water magic is an individual fantasy power.

### Latest Lagoon gameplay addition,2026-09-24

Owner asks for SaBubong-style falling and button-mash recovery, plus flying birds.
Genuine airborne platform falls now enter the shared prone get-up at water-entry
depth, with Lagoon's existing eight-second held-stock return. Intentional stair
entry/swimming remains available. Host authority and existing recovery/input/camera/
HUD/network paths are reused; no new protocol, score or collision change. Details
and evidence: [Lagoon fall report](reports/map-by-map-refinement-2026-09-23/lagoon-fall-recovery/report.md).
Birds and the newer building/water/island refinement remain in REFINE-2.6. This does
not reopen historical Rafi designs or replace newer cast work.

### Lagoon: reference-to-construction decisions

The owner photo shows the important missing relationships: water between houses,
slender piles and visible underside framing, small boat-access landings, connected
clusters as well as detached houses, and light roofs above compact dwellings.
The existing court can remain a clear sporting surface, but it must sit inside a
village rather than look like six identical huts placed around a resort dock.

1. **Composition:** retain the28x26m legal court and its clear throwing/chase space.
   Break the presentation's perfect symmetry with offset residential spurs, different
   porch depths, varied setbacks/angles and three overlapping settlement clusters.
   Add detached offshore stilt houses beyond the playable routes, with open water
   and boat access between them. Keep broad safe gameplay routes and all four exits.
2. **Elevation/support:** retain the tested court/water levels. Raise selected home
   floors and landings0.6-1.2m, joined with gentle access or short readable steps.
   Their taller exposed piles, diagonal braces, crossbeams and lashings must actually
   meet their decks and the seabed. Compute support depth from world height, including
   raised parents. Fixed houses are not floating boats and never bob or levitate.
3. **House families:** build distinct steep thatch gables, low nipa-like hipped roofs,
   repaired corrugated gables with kitchen lean-tos, open boat-repair shelters and
   ventilated screen verandas. Vary wall construction: horizontal planks, vertical
   repairs, open timber frames, simple woven-screen panels and rolled openings.
   No more one sealed box with a recoloured roof for every household.
4. **Construction detail:** model roof thickness/overhang/ridge, uneven thatch ends,
   structural rafters, window shutters, ladders to boats, porch benches and localized
   tide marks. Use material grain/fasteners where physically appropriate, not a
   repeated noise layer or a cultural pattern stamped across every object.
5. **Boatcraft and life:** replace the blunt rectangular boat forms with pointed,
   curved dugout/planked hulls, readable gunwales/cross-seats and fitted paddles.
   Include a small number of distinct sheltered houseboats apart from fixed homes.
   Add useful drying frames, hanging/coiled nets, water containers, repair stock,
   cooking/storage corners and household mats, with quiet residents. Keep the
   community maintained and inhabited; do not use trash or exaggerated decay as identity.
6. **Water/landscape:** retain moving water and sky, refine shallow-water colour,
   underwater depth and localized light variation. Keep the requested islands and
   mountains, but distribute them in irregular groups and layered distant ranges,
   with an open sea passage; avoid a necklace of evenly spaced identical cones.
7. **Culture/source boundary:** the supplied photograph remains internal reference.
   Use primary sources to distinguish fixed stilt homes, dugouts and lepa houseboats.
   No invented sacred/tribal markings or copied contemporary artists' mat patterns.
   The sporting court/layout, Rafi's costume and his magic are original fiction.
8. **Implementation order:** finish the copied Rafi recipe and native hero comparison;
   refine the lagoon architecture/composition in authored batches; then update the
   native expansion candidate. Keep every other TODO and existing gameplay result.
   Broad regression remains final integration work, not a reason to stall the art.

### Cross-reference used for this revision

- [Philippine Embassy cultural exhibition](https://cultural.philembassy.org.au/phinau/beyond-borders/pearldiving1)
  identifies a2022 photograph of Sama-Badjao stilt houses in Lookan Banaran,
  Sapa-Sapa, Tawi-Tawi. It supplies a Philippine locality reference; it does not
  establish the location or reuse licence of the owner's different photograph.
- [National Museum ethnology collection](https://www.nationalmuseum.gov.ph/our-collections/ethnology/)
  documents collected boats and boat-building materials/tools from Sama communities
  in Sitangkai. Use this for boatcraft context, not a universal claim about occupations.
- [NCCA's lepa description](https://www.flickr.com/photos/nccaofficial/18259381850/)
  describes a curved keel/planked hull and compact shelter with practical household
  and fishing storage. Adapt the construction/functional relationships; do not copy
  its photograph or culturally specific carved prows as unresearched decoration.
- [QAGOMA's Sama Dilaut artist project](https://collection.qagoma.qld.gov.au/page/bajau-sama-dilaut-weavers)
  situates living weaving practice in Semporna's tidal stilt villages and maritime
  daily life. The works are named contemporary artists' designs; they are not
  generic free patterns for game textures.
- [Maritime Museum of Barcelona exhibition dossier](https://www.mmb.cat/wp-content/uploads/ws/641015-76083_uri.pdf)
  distinguishes sheltered boat settlements from fixed houses on wooden piles.
  Only the architectural distinction is used here; broad biological/social
  generalizations in the dossier are not adopted as character design facts.

**Current implementation order,2026-09-22:** start this expansion now. The owner
explicitly corrected excessive validation work while these features were missing.
Existing request/rehost checks are complete; remaining broad verification moves
to final integration. Historical LAST LAST/research-only text below is retained
as decision history and does not postpone or prohibit this authorized implementation.
Quality remains the priority. Plan concrete architecture/asset choices here, then
implement with focused checks; do not turn planning into another testing detour.

**ACTIVE after the2026-09-21 owner correction:** "finish everything note yet done".
The presentation checkpoint is retained; this expansion is now required in the same
assignment after existing-game work. Preserve selected B hero/C map concepts and
kit/lore direction. Earlier "research only"/"later" instructions below describe
the previous phase and no longer prohibit implementation.
[Current queue](TODO.md#current-implementation-queue).

**SELECTED by the owner, 2026-09-14: character B + map C.** Use the tied-hair,
rolled green shirt and practical boatcraft direction, with a central court
surrounded by the lagoon village. Source sheets are in
ArtSource/badjao/concepts-2026-09-14. This selects the concept, not every generated
surface detail. Preserve native chunky TUMP forms and no-thumb hands. The
working name Rafi is not a separately approved final name. Implementation stays
LAST LAST after the existing queue.

**LAST LAST. Research and concept selection only for now.** The owner explicitly
places this after the existing rework, UI and deferred Inday work. Do not add
runtime roster rows, character assets, abilities, unlock rules or a playable map
until that stage. Generated concept images are choices, not production approval.

## Rafi, working name

## Latest map addition, 2026-09-22

The owner explicitly requested random islands and mountains in this map's
background. Generate varied coastlines and layered mountain silhouettes with a
fixed authoring seed, so peers and repeated builds retain the same scene. Give
near islands real terrain volume and restrained shore palms; keep distant ranges
simpler and hazier. This is background scenery outside all gameplay/recovery
bounds, not extra platforming routes or replacements for existing-map buildings.

## Implementation contract, 2026-09-22

The selected sheets have now been inspected. Implement in this order, with focused
compile/behavior/art checks at useful integration points, then return to features:

1. Author `team-rafi.glb` from the native CC0 skeleton with preserved ordinary
   animation channels. New chunky tied hair, flat expressive face, rolled sage
   work shirt, navy shorts, brown simple hands/legs, rope tie and one small repair
   pouch. No anatomical fingers, oversized props or concept-sheet facet noise.
   Append `rafi` to HeroPeople/AllPeople; preserve every existing index and GUID.
   Build roster/palette/FPP assets using the existing editor authoring route.
2. Add RafiHeroKit through the established HeroAbilitySystem. Starting tuning:
   Q two uses per round, narrow 6m current with a 0.18s visible gather and one
   flight interception; turn the incoming horizontal velocity up to 40 degrees
   toward the committed aim, preserving speed, vertical arc, throw/chain identity,
   affinity and scoring eligibility. Own throws remain eligible. Never use the
   existing Slipper.Deflect, which intentionally clears thrower/chain attribution.
   Alternate TIGHT CUT travels faster but catches a narrower band.
3. E two uses per round: replay a short bounded recent movement/throw-feint trace
   as a translucent reflection, no collider/actor/projectile or combat target.
   No teleport, immunity, concealment or fake score. Alternate LONG WAKE lives
   longer but reveals itself sooner through stronger ribbon breakup. Host accepts
   its trace; observers/rejoin/replay reconstruct that same bounded path.
4. Breakwater costs16 objective charges. Gather at one hip, spiral through the
   torso, release a low 6m-wide crest travelling8m. Obstacles cut each lane; a
   player jumping above the crest or standing on raised ground is not touched.
   One bounded impulse per victim, no stun, normal steering/protection/confinement.
   Loose slippers get bounded swept movement without becoming damaging throws.
   The actual can is never an implicit target. Author separate Q/E/ult body/FPP,
   water geometry, glyphs and short sounds; carry and interruption remain honest.
5. Extend live field snapshot and recorded-field rendering for the moving water
   and echo. Append kind IDs, bump wire/replay compatibility when changing their
   payloads. Cosmetic reconstruction must never run authority or collision code.
6. Author `Lagoon` as a fifth arena, preserving the four existing scene IDs.
   A broad central deck (about28x26m) carries the normal rules; four broad bridges
   reach a perimeter loop and distinct fixed homes. Piles/crossbeams visibly meet
   the waterbed. Handrails protect the main circulation; explicit water-access
   steps/ramp recovery work for all people/modes. Reuse established swimming and
   recovery contracts, without a Rafi-only passive. No narrow jumping maze.
7. Different house construction/detail: weathered board frontage, painted repair
   workshop, bamboo-screen veranda, patched corrugated roof, laundry porch and
   boat landing. Use restrained wood grain/board joints, roof ribs/fasteners,
   damp lower piles and new repair boards where plausible. Scenery stays outside
   the throwing lane. Add boats, repair tools, potted plants and quiet residents.
   Water has directional motion/shore detail; sky drifts with the shared clock.
8. Integrate map selection/rotation/preview/build scenes, bots, environment grade,
   audio, UI copy, tutorials/field guide and final roster/map coverage. Finish
   remaining genuine old feature gaps, then comprehensive final qualification.

These dimensions/numbers are initial implementation decisions, not balance proof
or owner taste approval. Revise only when actual integration reveals a reason.
Research refreshed2026-09-22: both National Museum pages below still support the
stilt-house/boatcraft background. All clothing details, sporting layout and magic
remain original fiction; no claims of an authenticated cultural pattern.

A young adult male Sama Dilaut athlete from a fictional waterside community in
Tawi-Tawi. The owner's term Badjao is retained in the planning title for retrieval;
the character is given a specific Sama Dilaut background rather than mixing every
Sama, Yakan, Tausug or other community into one costume. His name is a proposal.

He is an inventive, teasing competitor who likes making a rival commit too early.
He helps maintain his family's boat and is forever proposing tiny improvements
to the community court. His cousins keep score on which ones actually work.
He enters the circuit to play people he cannot already predict. He enjoys beating
Zack at a trick shot; Cheska is harder to distract than he expected.

Short lore: **Draws you into the wrong current. Leaves with his slipper.**

Long introduction draft:

Rafi grew up in a Sama Dilaut community in Tawi-Tawi, where the playing deck was
also the place to catch up with everyone on the way home. He repairs boats with
his family and spends the rest of his time inventing shots his cousins insist
should not count. On the tournament court, his water takes the shape of a nudge,
a false step or a route that changes beneath you. He is here for stronger rivals
and better stories. Preferably stories where he gets the last laugh.

The earlier gill proposal was removed by the owner on2026-09-22. Rafi has normal
human skin and personal water magic. Keep him expressive and native to the retained
hero cast, with simple no-thumb/no-finger hands.

## Three powers with different jobs

Design proposal, not implemented or balanced. Keep the existing two-skill plus
ultimate controls. Water works on every existing arena; standing in actual map
water is not a prerequisite. The kit creates chances, never automatic points.

### Q: Crosscurrent

An aimed narrow crescent of water bends the first flying slipper it crosses
sideways. Rafi chooses the lateral direction before release. This can save him
from an incoming throw or turn his own throw into a bank around cover.

- Tradeoff: a small interception window, one slipper only and a visible lead-in.
  A feint, a second throw or a shot from the opposite side can beat it.
- Authority: preserve original throw ownership, hit eligibility and scoring.
  A deflection is one host-resolved event; never clone or steal equipment.
- Form: one travelling crescent with a crisp curl at the actual interception.
  No ring field or persistent blue puddle.
- Cast: quick off-hand sideways cut with a planted opposite foot; the throw hand
  retains the prop and can clearly recover. Not Zack's magnetic recall gesture.

### E: Mirrorwake

A brief watery echo repeats Rafi's last movement and throw feint. It invites a
mistimed chase while he takes another route. It does not attack, score, tag,
collide, grant immunity or hide the actual can and slippers.

- Tradeoff: the echo gives away its watery material and dissolves quickly.
  Watch the true slipper or wait for the feint to resolve. No free teleport.
- Authority: host-accepted activation with bounded recent motion data. Cosmetic
  echo never becomes a second player, targetable body or authoritative projectile.
- Form: a sparse translucent body reflection that peels into ribbons when spent.
  Distinct from Nemu's independently moving companion and possession.
- Cast: a short heel turn and shoulder misdirection; the echo continues the
  abandoned line while Rafi opens toward his actual movement direction.

### Ultimate: Breakwater

After a clearly visible gather, a broad low wave travels forward. It carries
opponents and loose slippers along its direction for a short distance, opening
a retrieval route or pushing a chase away. Players retain steering and can
escape across the edge. It is a travelling event, not a sustained prison.

- Tradeoff: committed direction, meaningful warning, finite length and modest
  height. Obstacles cut the wave; jumping or lateral escape must have honest,
  tested outcomes. No repeated stun, full-screen water or automatic can knockdown.
- Authority: one accepted moving volume with deduplicated bounded displacement.
  Honour defender confinement, protection, raised ground and roof recovery.
  Loose slippers retain ownership and remain retrievable on every map.
- Form: low glassy crest, visible trough, trailing foam that settles quickly.
  Keep the can readable through the water. No blue copy of a fire nova or ice wall.
- Cast: low cupped-water gather at one hip, a continuous rising spiral through
  the torso, then an open sideways release. No Dante ground slam, Sean leap,
  Phaister overhead invocation or Cheska inward burst.

Numbers and alternates wait for the later prototype. Start with role, tell,
counterplay and ownership before tuning size/cooldown. Do not add complexity
through an extra resource bar, swimming passive or mandatory combo.

## Visual choices to make together

Three hero silhouette studies: agile current athlete, compact boatcraft athlete,
and playful reflection specialist. Vary garment construction, hair, silhouette,
posture and spell form, not just colours. His familiar life can appear in one
small woven accessory, a useful pouch or a rope repair detail. A cultural motif
is not a texture to stamp across body, water, UI and every ability.

No fabricated tribal tattoos, sacred inscriptions or costume borrowed at random
from neighbouring communities. Research any specific markings before adding
them. His personal water magic supplies the fantasy distinction.

## Future map: a community court above the water

The owner supplied a stilt-house photograph on 2026-09-14. Its exact location and
reuse licence are not established. Preserve it as an internal reference; do not
ship it as a texture. Architecture: fixed houses on wooden piles, connected decks,
boats, water and daily community life. Houses do not levitate or bob like boats.

Three concept compositions are being explored:

- **Neighbourhood boardwalk:** broad court between staggered home frontages.
- **Community jetty:** generous end-of-pier court, homes to one side and open sea.
- **Sheltered lagoon:** broad central deck, roomy bridges and homes around it.

All require a flat clear court, believable supports, generous chase/retrieval
routes and readable rail/edge rules. Water access/recovery must be designed for
all heroes and Classic, not only Rafi. No narrow platform-jumping maze. Homes
remain safe scenery; powers do not destroy a community for spectacle. Everyday
colour, laundry, maintenance and boats should communicate a living place.

## Research base and limits

- [National Museum: Peoples of Southwestern Philippines](https://www.nationalmuseum.gov.ph/exhibitions/nm-western-southern-mindanao-regional-museum/peoples-of-southwestern-philippines/)
  identifies Sama Dilaut stilt houses, dugout boats and lepa houseboats. It also
  distinguishes the other communities represented in the exhibit. Do not merge
  their rituals or clothing into Rafi's design.
- [National Museum: transport and fishing collections](https://www.nationalmuseum.gov.ph/our-collections/ethnology/agriculture-fishing-hunting-transportation/)
  supports boatbuilding and lepa use. Not every present-day Sama Dilaut person
  lives aboard a boat or has the same occupation. Avoid the outdated label used
  in some source descriptions.
- [BCPCH: Tepo of Tawi-Tawi](https://bcpch.bangsamoro.gov.ph/tepo-of-tawi-tawi/)
  indexed text supports woven-mat craft. Direct access returned 403. This is
  limited background, not permission to reproduce a ceremonial pattern or
  claim any invented stripe is authentic. Further pattern-specific research
  belongs to asset authoring after a concept is selected.

Reviewed 2026-09-14. Hero personality, powers, sporting deck and map layouts
are original fiction. Concept selection does not resolve later mesh, animation,
collision, ability balance, network, accessibility or culture-specific art review.


## Implemented source and current qualification boundary

Source now includes the seventh roster row, authored model/rig/FPP assets, portrait,
three distinct actions/effects, ordinary alternatives, seven hero audio cues, bot
choices and bounded live/replay field transport. Protocol49 prevents older peers
from joining incompatible fields. Recorded schema11 writes new water paths; the
reader retains schema10 compatibility for existing water-free saved clips.

The lagoon scene is generated and selectable through both map registries, with
supported circulation, four stairways covering both sides of the promenade,
swimming/recovery, scenery and the requested background archipelago. The owner
has not approved its appearance. Current further authoring adds complete route
rails, gable/eave fixes, per-island static mesh combination, mooring-line/bob motion,
quiet deck contact/swimming/lapping foley. The proposed gill helper was subsequently
removed at the owner's request.

Initial gameplay evidence is narrow and explicit: Q steered one flight while
preserving credit; E and its recorded view introduced no actors/colliders/equipment;
wave carried loose stock with bounded movement, left held stock/can safe and did
not move a raised target. Those three checks passed. Four stair routes across
Classic/Hero passed after correcting the fixture's camera-relative input; no
buoyancy workaround was added. One attempted private-property fixture edit failed
compilation and remains in the report. This is not native multiplayer/balance or
full presentation acceptance. See the ledger for the next exact work, not this
section as an independent queue.
