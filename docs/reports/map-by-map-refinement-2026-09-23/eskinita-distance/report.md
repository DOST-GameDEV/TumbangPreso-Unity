# Eskinita visible distance, 2026-09-23

The owner correctly pointed out that the first outer row still ended at an empty
plane in map-select renders. This change continues that neighborhood through the
visible distance. The first row stays present in both comparison images; only the
new deeper district is toggled.

The district has96street blocks,104medium-distance houses using retained meshes,
448simpler distant houses and24planted shared yards. Buildings face streets, plots
sit on the ground, and connecting roads link into the existing extension. Farther
houses keep the retained families' proportions and stepped/gabled massing while
dropping small construction detail. The original court, nearby buildings, source
GLBs and existing collider bounds remain unchanged.

## Rendering scope

Blocks are grouped into96meshes plus one street mesh. They share a single408x416
palette atlas/material derived from the existing six roof atlases and six facade
tints. The established palette selection is reused; original palettes and old
instance names are unchanged. Distant windows are simple faces, not full repeated
window assemblies. Distant shadow casting is disabled.

The author reports315742district vertices. Generated source assets total about
43.9MB including text-serialized meshes and metadata. This is an asset cost record,
not a claim about native frame rate; final map performance remains to be measured.
There are no new gameplay colliders or per-building runtime behavior components.

## Evidence and judgment

The focused case passed1/1in1.781s. It uses the real MapPreviewSurface camera at a
fixed pose, checks all26initial houses' palette/grounding,96new blocks, their one
material, disabled distant shadows, and separation from the existing neighborhood.
The author also checks unchanged existing collider count/bounds.

Before/after1280x720,960x540and25percent greyscale were personally inspected. The
previously empty top band now contains a continuing roofline and street context,
with distance reducing its contrast. The central court and foreground remain
readable and unchanged. The background request is materially addressed in these
preview views. This is not blanket approval of Eskinita's near assets, lighting,
every camera angle, or all five maps.

The guarded author and focused check completed normally and restored the named
profile/shared input. Source and generated-churn patches remain in the owned
qualification checkout under Logs/refine2-eskinita-v3. No capture-tool repair or
native rebuild was used for this change. The two original DEVPNGmetas remain excluded.

## Next

Continue Eskinita's individual near-building/prop, material, vegetation and sky
review. Refresh its static map-card renders after the map's authored pass is stable.
The other four maps, animal behavior, bot stalls, individual locomotion/throw/pektus
work and outstanding multiplayer/final qualification are still open. LIGHT-1is a
separate unmerged branch and its work is not claimed here.
