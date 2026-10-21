# REFINE-2 intake: individual maps and actual play

Owner request2026-09-23. This is additional work AFTER the older actionable queue,
not a replacement for it or permission to delete/defer old requirements. All existing
IDs and unresolved acceptance remain in TODO. Do not mark the overall goal complete
at the VISUAL1 or P7checkpoint while this addition is unimplemented.

The owner reference is https://3d-asset.com/ and the reported problem is that TUMP's
3D map assets still look bland and lack the material/detail appeal of that reference.
The owner explicitly rejects solving this with broad changes across many maps:
improve each map individually, thoroughly, with considered asset-level work.
They also report animals repeating fixed paths and bots sometimes standing/AFK,
and ask for a review of every part of actual gameplay, including animation.
Earlier local visual checks, animal reaction checks and bot fixes do NOT by themselves
close these newly reported experience problems.

## Order and research gate

1. Finish/reconcile older actionable work and its required qualification first.
   Preserve actual unfinished tasks and external/concurrent dependencies distinctly.
2. Thorough visual research and a saved design/implementation plan before new art.
   Inspect the reference's rendered assets, not only its sales copy. Compare shape,
   construction layers, material breakup, value, edge treatment, roughness, recesses,
   trim, texture scale/density, prop clustering and use of empty space. Separate
   lessons useful to TUMP's cute stylised 3D game from photorealism/purchased packs.
   Cross-reference local/cultural construction sources for each neighbourhood.
3. Inventory visible assets on ONE map, decide keep/refine/replace per family/object,
   document source/licence and an intended visual result, then implement/inspect that
   map before the next. Shared helpers may support it but must not silently change
   other maps. Author per-map/asset selections and distinct construction details.
4. Work through Eskinita, Bayan Plaza, Ilalim ng Tulay, Sa Bubong and Lagoon one by
   one (research may justify reordering; preserve all five and record the reason).
   Preserve gameplay geometry/colliders, routes, map identity, supplied imagery,
   original cast and can/slipper readability. Add real shape/material/context detail,
   not the same noise, stain, colour switch or shader treatment on every building.
5. Natural ambient life, diagnosed in actual motion: bounded habitat/goal choices,
   varied dwell/turn/speed/action timing, meaningful pauses and reactions, plausible
   ground contacts, birds/dogs/residents/boats appropriate to each map. Fixed looping
   routes with only random waits are insufficient. Preserve safe navigation and
   gameplay RNG; no animal wandering through buildings, off decks or into camera.
6. All-bot gameplay review across both modes, roles, maps, roster and choices. Record
   what each apparently idle bot is trying to do; distinguish a justified wait from
   a dead decision, unreachable goal, stale ownership, path deadlock or failed action.
   Fix actual causes, do not mask them by random movement or unconditional actions.
   Retain existing C1/C2fixes and evidence; new owner report requires new observation.
7. Whole gameplay refinement: throw/retrieval/restore/chase/tag, navigation, collision,
   targeting, action availability, interruption/recovery, body/FPP contact and motion,
   remote/spectator communication, audio and overlap. Maintain a concrete issue list
   with before behaviour, intended benefit, implementation owner and completion proof.
8. Integrate and qualify the resulting candidate. Quality matters, but follow the
   existing bounded-verification rule; no repeated screenshot/fixture repair loop.

## First per-map questions (not implementation claims)

- Eskinita: distinguish each house's plaster, timber, roof, shutters, gates, utilities
  and domestic/street props. Look for silhouette depth and lived-in construction,
  without cluttering the retrieval lane or repeating one wall treatment everywhere.
- Bayan: civic stone/plinths/columns, roof courses, door/window recesses, vendor/garden
  furniture and paving detail should have different construction and wear logic.
- Ilalim: heavy concrete, joints and soffits, repair/retail frontage, shutters, metal,
  infrastructure and retained signage/livery should read as a working neighbourhood.
- Sa Bubong: roof waterproofing/drainage/curbs, rails, pool surfaces, rooftop utilities
  and skyline depth should support this particular place, not duplicate the street.
- Lagoon: retain culturally researched stilt construction/detached houses. Distinguish
  timber/piles/tide marks/woven walls/roof patches/rope/nets/boats and lived activity;
  fixed houses do not float. Revisit water/shore/sky depth alongside individual homes.

## Reference status

Homepage opened2026-09-23: modular architectural packs and PBR texture sets are
advertised. This is reference intake only. Rendered asset comparison, independent
source cross-check and the full plan are NOT done yet. No purchase or asset import.
Existing no-paid-services rule and Asset_Sourcing/licence requirements remain.

Progress/status belongs in TODO; exact resume is in ACTIVE_REWORK_LEDGER. This file
preserves the new owner's intent until the older queue reaches the research gate.

## Owner clarification: map references after UI,2026-09-23

Finish all currently assigned UI work before starting map refinement. The older map
feedback remains active: all five maps individually, construction-specific detail
and material identity, pleasing stylised art rather than realism, more convincing
Sama Bajau lagoon architecture including detached over-water homes, animated sky
and island/mountain context, natural ambient movement. Keep good existing work.

Use built-in image generation as an additional reference source during that later
pass. Critique every result: record useful silhouette/material/composition ideas,
errors, over-detail, cultural/place mismatches and parts that would not work in the
actual3D camera or navigable map. Copy only the good ideas into authored game assets.
Generate further targeted references when the comparison reveals a concrete gap;
do not turn reference generation into a substitute for implementing and viewing a
better map. Continue using3d-asset.com and independent real-world/game references.
Compare actual in-engine before/after at player/spectator distances for each map.
No global texture/noise/recolour sweep, no deleting older tasks, no premature closure.
Current generated images are loading-screen illustrations, not a started map rebuild.

## Owner additions: PEAK, Filipino identity and visible lobby context

Research PEAK and other suitable games thoroughly before later map implementation.
Each map should showcase a different part of Filipino culture, using independent
real-place/cultural references rather than copying foreign motifs. Preserve the
stylised, pleasing direction and the per-map workflow. The owner specifically
points out the empty real map behind the lobby: add coherent surrounding structures,
connections and lived detail that make sense as a place. LOBBY becomes an explicit
acceptance camera alongside gameplay, introduction and spectator views.
See [the detailed research/camera brief](cultural-and-camera-brief.md). Both supplied
images and provenance are in ArtSource/map-refinement-20260923/owner-references/.
These requirements are saved for after UI/current tests, not claimed researched or
implemented now. Older tasks, image-generation critique and all-map coverage remain.
