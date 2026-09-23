# Ilalim structural material finish, 2026-09-24

The viaduct now reads as pale neutral concrete with quieter steel rails and concrete
sleepers. The large mint/pink/yellow stripes no longer dominate the actual preview.
Existing joints, caps, bearings and surfaces remain visible; the under-deck shade
stays readable under the adopted bright look. Keep this first visual result.

Scope is24pillars,28guideway bays and56track sections. Two local numeric material
lookup textures preserve value variation in selected atlas columns. Original
atlases, train livery, shops, vehicles, global lighting and other maps are unchanged.
Guideway bays use their concrete surface role; a track-only derived mesh changes
72sleeper vertex roles from wood to concrete. No vertex positions, normals, UVs,
renderer bounds or collider bounds changed. Original references are saved in local
material tags and the new mesh's importer metadata for repeatable restore/rebuild.

Author scope/bounds/collision guards passed. Same-camera original/new actual-preview,
north-eye and under-deck views were rendered and inspected, plus25percent grey.
The focused case passed1/1in7.212s. Native imagery establishes this material result,
not an FPS or full gameplay qualification. No further unchanged palette checks.

Next: existing low-detail skyline models have large blank-looking masses. Inspect
their real glazing UVs/materials and facade geometry first, preserving the existing
city layout. Near shops, roof/service details, sky and final card remain in the
individual Ilalim pass; all other unfinished tasks remain assigned.
