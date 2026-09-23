# Filipino street life and vehicles,2026-09-23

Owner explicitly asks to add Filipino objects such as tricycles and research them
thoroughly. This expands the existing per-map house/context plan, not a generic
prop-scatter pass. No new vehicle placement is implemented at this pointer.

## Initial source and asset inventory

- The[PNA report on Binalonan's tricycle service](https://www.pna.gov.ph/articles/1052257)
  identifies local operators organized through a TODA. Its indexed report was
  read as a place/use lead, not a current nationwide transport rule.
-[BSP's Paleng-QR programme](https://www.bsp.gov.ph/Pages/InclusiveFinance/PalengQR/PalengQRProgram.aspx)
  links public-market/community-shop activity and tricycle operators. This supports
  researching connections between a waiting point, shops and actual routes. It
  does not prescribe the game's signage or payment systems.
- PNA's2020pandemic transport restrictions are dated and irrelevant to the desired
  timeless street scene. Do not turn that search result into game rules or copy
  old pandemic-only details.
- Existing repo assets found: terminal/bayan-passenger-tricycle.glb, env_tricycle.obj,
  env_cargo_tricycle_boxes.obj and kits/car/jeepney.glb. Existing original builder:
  tools/author_terminal_tricycle.py. Bayan already has a roadside waiting bay;
  Ilalim already has a cargo tricycle and supplied jeepney finish work. Inspect
  these actual models/placements before claiming vehicles are absent or replacing
  them. Refine useful existing assets; create missing appropriate forms only.

## Next research and design steps

Two actual photographs have now been visually inspected in a temporary browser
tab, then the tab was closed and the tab list verified empty. No image downloaded.
[Eva Rapoport's photographic series](https://jathilan.me/living-amongst-the-dead)
shows a motorcycle beside an enclosed metal sidecar, three separate wheel contacts,
low passenger floor, windscreen/sign panel, curved sheet-metal edges, a driver
shade on its own supports, and a rear view of another sidecar with a small window.
Use those vehicle-construction observations, not the cemetery setting or religious
lettering. The[Philippine Star/Michael Varcas photograph carried by OneNews](https://www.onenews.ph/articles/139-000-tricycle-drivers-to-start-receiving-p5-000-cash-aid-today-march-17-marcos)
shows a modern integrated three-wheeler in front and motorcycle-sidecar vehicles
behind, arranged under a terminal shelter. These are distinct vehicle forms;
do not accidentally combine the integrated nose with an attached motorcycle.
Canopy support, passenger entry, roof structure and waiting-space context matter
more for the blocky model than copying decals or small chrome fittings.

The photographic observations are visual reference, not engineering dimensions,
current transport policy, or evidence of our existing model's quality. Existing
model/scene inspection and map-local design are still required before placement.

## Existing model and chosen Eskinita variant

The actual Bayan passenger tricycle was inspected in native Unity front/quarter/
side/back images, saved in tricycle-existing. Diagnostic1/1passed6.043s; this is
an inventory, not new art approval.2148vertices, about2.04x1.64x1.98m, seven already
mapped surface materials. The side image is partly obscured by a neighboring
vehicle's near-camera fade; other views show the relevant construction. Do not
repair the capture fixture or claim a complete unobstructed side review.

Keep the understandable motorcycle/sidecar split, seated passenger space, grounded
wheels and broad blocky forms. Weak: the windscreen reads as a black slab, body
panels and fenders are plain, mirrors are absent, and the driver has no shade.
The source is a useful base. Eskinita currently has no named tricycle placement.

Generated concept: concepts/eskinita-tricycle-v1.png, built-in tool with the actual
quarter view as reference. Keep muted blue/cream/metal separation, readable glass,
an octagonal lamp, small mirrors, a simple unit stripe and supported driver shade.
Reject over-wide tyres, excessive bolts/chips/diamond tread, ambiguous canopy
supports and altered entry proportions. Preserve the original model's scale;
the driver shade mounts to the rear frame, not the handlebar. No real branding.

Make a new Eskinita variant by copying the existing original author; preserve the
Bayan model and its waiting bay. Add only medium-scale construction/readability
improvements and a small fictional07identifier. Keep three wheel contacts and
clear entry. Use native Unity for all preview/qualification; Blender only generates
the mesh/native source, never approval images.

First proposed placement: the existing west vehicle lot2_W, now occupied by a
generic delivery vehicle, is sunlit and visible from the court. Measure its live
bounds before adopting the swap. A tricycle parked at an operator's household bay
is coherent; do not label a closed private driveway a public terminal. Preserve
the lot/court collision and neighbor houses. Check fitting/support and real preview/
player view before accepting. Other vehicle lots and each map retain their own plan.

1. Inspect real local passenger tricycle photographs and distinguish motorcycle
   plus sidecar, passenger entry/seat, canopy supports, windscreen and three wheel
   contacts. Compare with the existing blocky asset and the hero/world scale.
2. Pick a coherent Eskinita side-street waiting/parked location, outside the court,
   with room for entry, walking and turning. Consider a modest waiting bench or
   operator board only where its actual use explains it. No fake real route labels.
3. For Bayan review the existing waiting bay; Ilalim's cargo/repair context differs.
   SaBubong can show transport below in street context, never parked on an
   inaccessible roof. Lagoon's over-water community needs boats and landing
   relationships, not tricycles placed on stilt decks.
4. Also research map-specific street/household objects and services as clusters
   of use, with readable silhouettes/material identity. No flag/decoration checklist
   and no one identical pack on every map.
5. Use generated inspiration only after real-reference/model inspection; critique
   it, implement locally, check actual camera visibility, wheel support, entry
   clearances and existing collision. Preserve older house/sky/gameplay work.
