# REFINE-2 comparative reference notes, 2026-09-23

Research in progress. This records inspected references and initial design
inferences, not a completed map pass. P6 accounting and the127.3 chat follow-up
are complete; remaining current multiplayer/release checks stay open in TODO.
No new map geometry, materials or lighting have been changed from this research.

## What was actually inspected

The public Brown City gallery, PEAK's official animated environment reference and
A Short Hike's official environment gallery were visually inspected in a temporary
browser tab. The tab was closed afterward. No packs were bought, downloaded or
imported. Developer/cultural text sources below are distinguished from visual
observations. The owner's supplied PEAK and lobby images remain in ArtSource.

### Construction depth: 3D Asset, Brown City

[Public gallery](https://3d-asset.com/packs/city/), screenshot9/11, inspected in
the browser. The scene has substantial window surrounds and sills, overlapping
roof edges, different wall/roof/paving treatments, recessed shop openings, an
air-conditioning support and a service alley with purposeful objects. Surface
pattern sits on recognisable construction rather than replacing it.

TUMP inference: first give a weak frontage believable layers and material joins.
Use a few readable surface marks sized for the gameplay camera. Preserve quiet
wall areas and different treatments for timber, plaster, sheet metal and concrete.
The American brick-tenement forms, shop identity and uniform window glow are not
a Philippine architectural brief; do not transplant the pack into Eskinita.
Catalog claims about texture resolution are not proof those textures suit TUMP.

### Large shapes and depth: PEAK

[Official Landfall page](https://landfall.se/peak), WORK TOGETHER/CLIMB media,
visually inspected. The warm foreground, pale faceted rocks, grouped palms and
receding mountain layers remain distinguishable without dense realistic textures.
Plant groups and overlapping landforms give the image depth. The owner's supplied
reference shows the same useful hierarchy.

TUMP inference: compose the visible background into distinct near, middle and far
groups, with silhouettes that survive the lobby overview. Give distant islands
and mountains different profiles and overlapping positions. Keep the court's
ground simpler than the surrounding neighborhood. A climbing game's tall playable
terrain, dense foliage and progression layout do not transfer to TUMP's flat
competitive court; its palette also should not recolour all five TUMP places.

### Detail grouped around activity: A Short Hike

[Official site and gallery](https://ashorthike.com/), visually inspected. Coastal
and woodland scenes use clear paths, compact groups of trees/rocks and small
activity pockets. The snowy cabin scene distinguishes a warm path and porch from
cool surrounding snow. Props support locations rather than filling every gap.

TUMP inference: place chairs, plants, containers and work objects around an actual
door, shop, landing or household task. Give the background reasons to exist while
keeping traversable routes legible. Its fixed distant camera and pixelated render
style are not appropriate replacements for TUMP's first-person/third-person views.
Borrow the grouping discipline, not its biome palette or camera assumptions.

### Place and culture as connected decisions: Splatoon3

[Nintendo developer interview,2022](https://splatoon.nintendo.com/en/news/ask-the-developer-vol-7-splatoon-3-part-1/).
The developers describe street culture as an organising idea and explain how
geography, climate, fashion, music and changes in the fictional community relate.
The article text was available through the indexed official source; one direct
page retrieval failed. No new Splatoon footage review is claimed here.

TUMP inference: write a specific place story for each map and let construction,
household uses, signs and background activity follow it. Filipino identity should
not be a collection of unrelated symbols attached after a generic town is built.
Preserve TUMP's own sport, cast and colour roles instead of copying Splatoon's
ink, typography, invented language or neon saturation.

The earlier MDA, Valve TF2 rendering paper and Hi-Fi RUSH developer notes remain in
[foundation-research.md](foundation-research.md). Their application still requires
inspection of TUMP's actual assets and motion, not a global shader recipe.

## Philippine place references

### Eskinita: home and sari-sari threshold

[Johanna Victoria A. Faustino, UP Diliman,2025](https://digitalarchives.upd.edu.ph/item/64306/7/)
studied storefronts in Sitio Olandes, Marikina. The published abstract distinguishes
full, half, grilled/screened and hybrid openings; it records canopies, signage,
counters/tables and seating. The abstract was read, not the entire dissertation.

TUMP inference: vary actual frontage structure and how it meets the sidewalk.
One store can have a half-height counter under shade; another a grilled opening
and small receiving ledge. Adjacent houses need their own thresholds and household
uses. Do not make every house a shop or assume one neighborhood represents all
Philippine streets. Specific visual references and current asset measurements
are still required before authoring these changes.

### Bayan Plaza: civic relationships and distinct construction

[UNESCO's Vigan account](https://whc.unesco.org/en/list/502/) describes connected
streets/plazas, civic and religious landmarks, mixed domestic/commercial use and
brick, wood and sliding shell-window construction. It supports thinking about
how buildings define an open civic place, not simply adding isolated monuments.

[National Museum: Casa Rocha,2022](https://www.nationalmuseum.gov.ph/2022/08/25/casa-rocha-balay-na-tisa-sa-bohol/)
describes stone below lighter timber, a recessed entrance and a simpler service
wing with exposed roof framing. It also documents differences from other houses.
These are distinct place references, not permission to combine all regional
details indiscriminately. Choose the plaza's local story before selecting features.

### Lagoon: distinguish fixed homes and boats

[National Museum, Peoples of Southwestern Philippines](https://www.nationalmuseum.gov.ph/exhibitions/nm-western-southern-mindanao-regional-museum/peoples-of-southwestern-philippines/)
identifies Sama Dilaut stilt housing, a dugout boat beneath and a separate lepa
houseboat. Keep those functions distinct: a fixed pile-supported house does not
bob like a boat. Household access, boat landings and working space should explain
the village arrangement, including the owner's requested detached houses.

[BARMM/UNHCR/JIPS Bongao report,2021](https://www.jips.org/uploads/2021/09/2020-Philipines-profiling_report-SamaBajau_TawiTawi.pdf)
was located and its provenance/cover context read. Detailed architectural pages
have not yet been visually reviewed, so do not cite the search snippet as a full
construction survey. Further community/place reference review remains in Lagoon's
individual gate. Ilalim and Sa Bubong still need their own primary place research.

## Current-source findings for the first map, Eskinita

Existing work must be assessed before adding more:

- `EskinitaNeighborhoodAuthor` already uses retained solid house families and
  fitted house-detail models, measured lots, private boundaries, cross streets,
  back/corner blocks, shop reconciliation and laundry. Do not rebuild these under
  another name or mistake an old JSON status label for the current scene state.
- `EskinitaUtilityAuthor` already places retained utility hardware and connects
  wires using named anchors. Preserve the functioning construction and collisions.
- `MapSurfaceAuthor` already assigns construction-specific surface treatments.
  Investigate a weak renderer's actual material/binding before adding new textures.
- `MapFinalPassAuthor.Run` supports `TUMP_MAP_AUTHOR=Eskinita`. The default loops
  three maps; always select the individual map for this requested refinement.
- Source routes: `MapSource/environment/layouts/eskinita-neighborhood-plan-v1.json`,
  `tools/author_retained_house_details.py`, and the four editor authors above.
  No edits have yet been made to those authors or assets.

## Next, before the first implementation

1. Inspect current Eskinita from the actual lobby overview, introduction and normal
   play cameras. Use existing suitable evidence where current; avoid a new general
   screenshot-tool project. List concrete weak objects and successful ones to keep.
2. Map each visible object to its author/source and construction role. Separate
   shape, material, lighting and composition causes. Check the concurrent lighting
   branch's actual scope without merging it automatically.
3. Save an Eskinita-only asset decision register and spatial plan: quiet court,
   inhabited frontages, logically connected side/back streets and a coherent skyline.
   Preserve original model masters, painting, lanes, collision and map identity.
4. Generate reference variations as the owner requested, critique them against
   the real sources and TUMP's blocky style, then adopt only useful details.
5. Implement one local asset/context group, inspect the actual result once with its
   smallest risk check, and continue through the map. Repeat independently for the
   other maps. Animal, bot and per-character/verb motion research remain assigned.
