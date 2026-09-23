# Ilalim existing skyline facade plan, 2026-09-24

The native isolated study (1/1,4.685s) resolves the issue: these are deliberately
minimal source models. Variant a has tall uninterrupted recessed glazing; b has a
blank upper shaft above a stepped base; c has a mostly blank shaft with one diagonal
glass face. Preserve their useful different silhouettes, positions and city layout.
Do not add another town to conceal them. Scope:11a/9b/14cinstances already present
under SkylineKit,34buildings total. Other skyline pieces are retained.

Per-model treatment:
- a: restrained horizontal floor divisions inside the existing long glass panels,
  with one central mullion where width permits. Keep its deep inset and heavy frame.
- b: pairs of modest windows on sufficiently large opaque shaft faces, aligned to
  consistent storeys. Retain the stepped base and roof mast.
- c: slender grouped windows on its broad remaining walls; preserve the diagonal
  glazed face and asymmetric profile.

Use quiet local blue-gray glazing lookup data for their existing glass column,
retaining source wall colors and original atlases. Add fitted static face geometry
only: window glass and narrow frame strips, with no collision/shadows/runtime code.
Group coplanar vertical triangles, project candidate rectangles in each face's own
basis, and accept only rectangles whose corners/midpoints/center all lie on that
actual face. This avoids windows across cutouts, narrow piers, steps or open gaps.
Use metre-based sizes with each instance's measured scale, not arbitrary pixel grids.

Combine overlays by source variant for a small renderer count. Original bodies,
transforms, footprints, cast/collision and other maps remain unchanged. Save source
material references for repeatable restoration. One paired actual preview/street
check plus a close diagnostic/grey25, then move to near shops and sky/card. No
photoreal window interiors, dense universal grunge, giant signs or new skyscrapers.
