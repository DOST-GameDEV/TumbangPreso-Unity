# Ilalim structural materials, 2026-09-24

The adopted bright native preview shows the existing viaduct geometry clearly,
but its reused palettes make concrete mint and rails/sleepers pink/yellow. Source
inspection confirms this is asset data, not a reason to darken the lighting:
24pillars and28guideway bays use roads/Textures/tumbang-warm-a.png;56track segments
use train/Textures/tumbang-lrt.png, which is also used by actual train bodies.
Preserve those original atlases and the train livery, shops, vehicles and other maps.

Observed raw GLB palette coordinates: concrete pillars useu.34375/.46875; bridge
bays use.03125/.28125/.34375/.40625/.46875. Track sleepers useu.21875and rails.96875.
These are16-column numeric material lookup tables. Derive map-local lookup textures
for these structural renderers only, retaining approximate value variation while
moving concrete to warm neutral gray, sleepers to quiet mineral gray and rail steel
to a darker neutral blue-gray. No generated painting or generic noise overlay.
Existing cast-concrete surface detail remains. Guideway bays use concrete rather
than the road-kit asphalt role. Track-only derived mesh changes sleeper vertex
roles from wood to concrete; no vertex position, normal, UV, bounds or collider changes.

One material finish author owns these108renderers and stores original material/mesh
references for ClearPrevious and matched review. No geometry additions, global
shader/lighting changes, train movement changes or palette changes elsewhere.
Copy/configure new materials before persisting. Keep native source references stable.
Check original collider bounds and exact target scope, then inspect paired actual
preview/under-deck/eye views plus25percent grey. Keep bright readable shade; no
photoreal dirt/cracks. Reference study supports material hierarchy, not copying its
more detailed shop scene, extra railings or different pier placement.

After this, examine actual existing SkylineKit/BacklotKit blank masses, near shops,
roof/service details and map-specific sky/preview/card. No duplicate city until the
actual existing context is accounted for. All remaining maps/gameplay tasks stay open.
