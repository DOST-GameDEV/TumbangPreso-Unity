# Philippine place references and implementation decisions

Researched/reviewed2026-09-12 after the owner rejected V9 as insufficient. This
records observations and design decisions, not a claim that new scenes are done.
The owner requests research, a durable next-step plan, then implementation.

## What the maps actually reference

LORE.md identifies Eskinita and Bayan as composite Philippine neighborhood/civic
places. Ilalim specifically draws from the team's Gilmore/LRT reference. The
older world-direction report records Project8, Pila and Gilmore photographs.
Those exact local photographs remain in Logs/reference-review and were inspected
again in this pass. Gilmore must not silently become a literal recreation of Raon.
Raon and the owner's vendor photographs provide supporting street-life references.

## Observed references

| Reference | What was actually observed/read | Application in this game |
|---|---|---|
| [Project8 sari-sari storefront, Exec8](https://commons.wikimedia.org/wiki/File:Sari-sari_Store.JPG) | The photograph shows a shop integrated into a home: a shaded barred service window, goods behind it, a supported corrugated canopy, weathered render and roof layers. | Build a stocked service frontage with a proper counter, shade and customer space. Give nearby homes construction and household use. Do not paste the photograph's advertisements onto assets. |
| [Pila church/town plaza, Ralff Nestor Nacor](https://commons.wikimedia.org/wiki/File:Pila_Church_%26_Town_Plaza,_Laguna,_Jul_2026_(1).jpg) | The retained reference shows a broad community lawn, shade at its edges, a monument, church/convent frontage and activity beyond the grounds. | Keep open gathering/play space, but compose planted edges and civic approaches as part of a wider town. Bayan's requested paving can draw from another appropriate plaza rather than copying Pila's grass. |
| [Molo Church and Plaza, Renz0903](https://commons.wikimedia.org/wiki/File:Molo_Church_and_Plaza.jpg) | Direct browser photo review shows distinct paving panels/joints, connected paths, trees and planting, civic lighting and neighboring architecture. | Give Bayan a legible constructed surface and connected perimeter walks. Keep any garden/water feature outside retrieval lanes; a fountain is not required just because one appears in the photo. |
| [Gilmore Avenue and MRT2 right of way](https://commons.wikimedia.org/wiki/File:Gilmore_Avenue_and_MRT2_RoW.jpg) | The retained photograph shows a heavy gray viaduct, drainage/weathering, layered utility lines, flat commercial frontage, visible electronics businesses and a real street junction. | Rework Ilalim's concrete/material weight, shop depth and urban continuity. Use the existing8m gameplay clearance and plausible support structure. Stage traffic/parking beyond the match. |
| [Aurora Boulevard sidewalk/lighting, Quezon City](https://quezoncity.gov.ph/installation-of-lighting-and-electrical-fixtures-sidewalk-maintenance-aurora-boulevard/) | Official photos were reviewed in-browser. They show a continuous tiled pedestrian route between building fronts and heavy columns, covered shop edges and lighting attached to the overhead frontage. The article concerns the Cubao part of Aurora, not an exact Gilmore survey. | Separate road, drainage/kerb, through-pavement and shop activity. Integrate shade/lights with buildings and structure. Preserve a clear pedestrian route past stalls. |
| [Raon and Paterno, iDiscover](https://i-discoverasia.com/walks/quiapo/locations/raon-and-paterno/) | The page and its photo collection show covered retail passages, dense goods at shop edges, street signs and umbrella vendors. It identifies electronics and optical-shop activity. | Stock specific shops and place stalls at meaningful thresholds. Translate the useful organization into simple geometry, with a clear match lane. Do not turn every facade into a billboard. |
| [Sta.Ana church construction, National Museum](https://www.nationalmuseum.gov.ph/2021/10/28/built-heritage-tradition-of-the-sta-ana-church/) | The museum describes stone/stucco/hardwood construction, a long nave, an adjoining convent/courtyard and extensive capis-window galleries. | Check the retained civic buildings as complete masses with connected wings/roofs and purposeful openings. Use appropriate simplified window construction; do not copy sacred content or attach invented history. |
| [Pila historic town center, NHCP](https://philhistoricsites.nhcp.gov.ph/registry_database/town-center-of-pila/) and [NCCA plaza description](https://talapamana.ncca.gov.ph/index.php/component/content/article/talapamana-national-and-overseas?Itemid=101&catid=14) | These official records describe a legible historical town plan and the relationship of plazas to surrounding blocks, streets, civic/religious buildings and community use. | Replace Bayan's detached building ring with a planned town district and distinct entrances/street mouths. The final arrangement remains fictional and gameplay-led. |

References span different dates and places. They establish useful construction and
spatial relationships, not a claim about present-day prices, business operations
or exact surveyed layouts. The original architect's Gilmore site had a certificate
failure, PNA presented a verification block, and a new Commons Gilmore image hit
429. None was bypassed. Existing local Gilmore/Pila photos plus the accessible
primary/community sources provided the visual basis above.

## The owner's supplied street references

Four originals are preserved unchanged in ignored Logs/map-owner-references-2026-09-12,
with filenames, descriptions and hashes in index.json. Their watermarks remain.
They are reference images, not game textures or models.

- food-stall-street.png: working steel/cloth counters, wok and tray displays,
  containers, umbrella shade and the vendor/customer/pavement relationship.
- fishball-kwekkwek.png: fishballs/kwek-kwek/kikiam, skewer basket, wok and sauces.
- fruit-cart.png: wheeled glass display, recognizable fruit and a shaded work area.
- clothes-accessories-street.png: hanging clothes, accessory racks, small signs,
  shop steps/thresholds, umbrellas and overhead services.

The owner explicitly requests Filipino stalls, natural 'Bawal umihi dito' notices,
some graffiti and other similarly recognizable cues. Diegetic Filipino wording is
therefore authorized; the English rule still applies to gameplay/UI descriptions.
Use original lettering/graphics and adapt colors to keep role cues distinct.

## Decisions after research

1. Build places as coherent working spaces. Architecture, goods, shop names,
   utilities, customer area and circulation must agree before decoration is added.
2. Ilalim gets recognizable food/fruit/clothes-accessory stall types, a recessed
   computer shop, simpler legible signs, neutral concrete and real street edges.
   Dense street life sits beside a clearly maintained play/through-route.
3. Remove the confusing former-hazard manhole/trench scenery inside play. These
   are decorative remnants, not needed hazards. Use restrained flush service
   covers/grates where their function is legible. Preserve one readable optional
   trip risk only where it has a real physical source and a route around it.
4. Bayan gets visible stone/concrete paving, connected civic walks and a surrounding
   street/block plan. Reconsider landmark placement as a whole; an isolated center
   surrounded by repeated houses on bare ground will not meet the request.
5. Eskinita gets house/storefront construction, layered roofs, openings, shading,
   service windows, household details and believable lots/side passages. Keep
   close domestic scale and a calm, clear play lane.
6. More detail must not become more noise. Concentrate distinctive detail where
   people use it, maintain broad blocky forms and review at actual player distance.
   Validate both modes, movement/retrieval and Low/High rendering after changes.

The exact next steps and acceptance checks are in ../../MAP_TRANSFORMATION_PLAN.md.
Native building V3 files are preparatory studies, not accepted or placed results.
No scene transformation from this revised plan is yet claimed complete.

## Critical review of the proposed translation

- The first building studies are too uniform to populate an entire street. In
  particular, Gilmore needs a stronger flat-roof commercial frontage family and
  layered retail use; filling it with the same gabled house would weaken its
  established identity. Revise that before broad placement.
- Reference photographs contain much denser activity than a TUMP match can use.
  Concentrate recognizable goods/shade at believable shop pockets; preserve
  customer and through routes. Do not scatter every photographed object in play.
- The retained church/hall arrangement and detached building belt still need a
  composed town plan. More textured houses alone will not give Bayan a wider world.
- The existing source art has enough color; the weak point is construction, use,
  hierarchy and surfaces. Do not answer this with blanket saturation, heavy grime
  or indiscriminate banners. Check the work at real FPP distance and in movement.
- Examine whether finished vendor spaces feel inhabited and coherent. Any quiet
  background activity considered later must remain visibly separate from the
  four match participants and must not add controls or confuse targeting.

The owner requires a candid critique after every batch. Record shortcomings and
revise before calling a visual result successful. Current V3 studies remain open.
