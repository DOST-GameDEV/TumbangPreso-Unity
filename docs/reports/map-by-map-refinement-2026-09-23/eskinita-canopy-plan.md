# Eskinita canopy visibility,2026-09-24

Judge this under the adopted bright lighting. The material/shadow problem is no
longer a reason to replace good house or tree geometry. Actual house views still
show overlapping crowns masking facades, especially5_Wand6_E. The inventory shows
two east-side pairs close together: trees0/2near z15-16and7/8near z-17to-19.
The west broadleaf6masks the terrace home. Keep the other four trees unchanged.

The existing models are stylized garden mango, young narra and broadleaf forms,
not botanical reconstructions. Their authored branching and materials are useful.
[NParks' mango profile](https://www.nparks.gov.sg/florafaunaweb/flora/3/0/3013)
describes its broad/hemispherical form; keep rounded, spreading crown groups and
avoid turning them into cones. This is a regional botanical reference, not evidence
that every Philippine street uses the same tree. The DENR urban-greening guidebook
was located but its PDF failed to load, so no detailed claim is based on it.
Prior PEAK/A Short Hike visual studies support grouped foliage with readable gaps.

Five individually named decisions are saved in
MapSource/environment/layouts/eskinita-canopy-refinement-20260924.json, including
original mesh paths/positions/bounds from the actual scene inventory. Reduce upper
spread moderately at0and2, the obstructing west6, and the overlapping7/8pair;
small crown lifts only where they leave useful gaps. Keep low trunk geometry,
root locations, collider sizes/positions and the remaining trees intact.

Derive map-local static meshes from original geometry, deforming upper branches
and foliage together so support remains connected. Preserve materials/UVs and
use correctly transformed normals. Do not add per-leaf objects, new runtime
behavior, tree replacement everywhere, or additional collision. These are authored
game composition choices, not a claim about real arboricultural practice.

Reuse existing near-house/render routes to inspect the changed facades under the
bright look. The author checks original lower-trunk geometry and collision. No
new test framework or perfect screenshot project. If the result opens facade views
without looking pinched or removing the neighborhood's greenery, keep it and advance.
