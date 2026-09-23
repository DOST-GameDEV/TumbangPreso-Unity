# Later map pass: comparative games, Filipino places and visible context

Status: owner's requirements saved2026-09-23. Research and implementation are
queued after the assigned UI and current tests. This is a research brief, not a
claim that those comparisons or map improvements have already been completed.
It extends REFINE-2.1 and each map's existing row; no older task is removed.

## Owner references and observed problem

- ArtSource/map-refinement-20260923/owner-references/peak-owner-reference.png:
  owner-supplied PEAK reference. The supplied image has a large layered landform,
  warm foreground/cooler distance, grouped vegetation and simplified rock forms.
  Investigate how composition, values, silhouettes and depth work in actual play.
  This observation of one image is not a review of the whole game.
- ArtSource/map-refinement-20260923/owner-references/lobby-map-context.png:
  owner points out that players see the real map behind the lobby. Existing houses
  read as an isolated perimeter around a sparse arena, with large empty surroundings.
  Fill the visible context coherently; hiding it behind UI is not the requested fix.

## Research before implementation

1. Study PEAK and a small, justified set of other games. Candidate comparisons to
   investigate: A Short Hike for landscape/landmark composition, Tchia for the
   relationship between place and cultural identity, Alba for environmental detail
   and foliage grouping, and prior Hi-Fi RUSH/TF2 notes for visual hierarchy.
   These are research questions, not conclusions or permission to copy their art.
   Prefer developer/artist talks, breakdowns, official footage and gameplay views.
2. Record reference frames/motion, sources and dates, useful principles, weaknesses,
   and exactly what transfers to a blocky3D four-player TUMP court. Explain what
   does not transfer: climbable mountain layouts, dense occluding foliage, unrelated
   cultural motifs, excessive distance detail or effects hiding the lata/slippers.
3. Independently research Philippine places, construction and everyday use for
   each map. Use reliable local/municipal/museum/cultural sources plus dated place
   photographs and, for community-specific settings, community-grounded sources.
   Trace building types, materials, roof forms, street furniture, signs, vegetation,
   household/working practices and how structures connect. Do not treat generic
   tropical scenery, foreign motifs or repeating flags as Filipino identity.
4. Write a separate cultural/place brief for each map before changing it. Different
   maps should show different parts of culture and daily life, with specific visual
   stories rather than the same decorations recoloured. Avoid presenting one
   neighborhood or community as representative of the whole country.
5. Then produce the map's asset/context plan and optional generated reference
   variations. Critique generated images against real sources and TUMP cameras;
   adopt only sound ideas. Retain approved work and supplied artwork.

## Per-map questions, to refine through research

- Eskinita: neighborhood street life, house/shop frontages, sari-sari uses, utility
  lines, shade and thresholds. How do side lanes and neighboring blocks continue
  beyond the court, without replacing its clear playing surface with clutter?
- Bayan Plaza: civic gathering and community activity, local building fronts,
  public furniture, planting and purposeful stalls. Establish a specific place
  reference; do not indiscriminately combine unrelated regional/period details.
- Ilalim ng Tulay: infrastructure and the community uses around it, structural
  supports, connected streets, shade, drainage and locally grounded vendors/play.
  Show functioning everyday life rather than making deprivation a decorative theme.
- Sa Bubong: household rooftop uses, water/storage systems, washing, plants and
  varied neighboring rooflines. Give roof access, setbacks and skyline believable
  relationships; no floating props or arbitrary buildings that only fill a frame.
- Lagoon: preserve the specific Sama Bajau village direction. Research varied
  stilt homes, detached over-water houses, piles, boat access, working/community
  spaces, timber repairs and local seascape. Do not substitute a generic island
  resort or transfer another culture's architecture because it looks tropical.

## Fill the visible world with a spatial plan

Audit actual HOME/lobby previews, map vote images, loading-to-map introduction,
ordinary first/third-person play and spectator views. Save representative cameras
before authoring. The lobby angle is explicitly an acceptance view, not an incidental
editor overview. Study the whole visible frame beyond the playable perimeter.

Plan a readable progression from court to useful perimeter detail, then coherent
nearby streets/house clusters/working areas, then lower-detail skyline, islands or
mountains. Decide why every addition belongs, how it is supported/accessed, what
adjoins it and what sightline it preserves. Shape/scale/depth variation must look
intentional. Retain quiet areas where they help composition; "fill" is not a demand
for maximum object count, random props or repeating the same house in a grid.

Maintain the gameplay dimensions, routes, collision and can/slipper/character
readability. Keep menu text contrast, reveal useful landmarks around the panels,
and check occlusion during the actual camera pan. Distant fill should use an
appropriate geometry/material budget; avoid paying near-camera detail costs for
tiny silhouettes. Animated skies/islands/background layers remain REFINE-2.6a.

## Acceptance within each existing map row

Research and cultural brief saved; current visible weaknesses identified; sensible
spatial/context plan saved; keep/refine/replace decisions per asset; implementation
limited to that map; actual before/after at lobby, gameplay and spectator cameras;
25percent greyscale and Low/small-view readability; bounded changed-risk checks.
Record what improved and what remains weak. Research notes or generated concept
images alone never complete the map row.
