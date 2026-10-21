# Sa Bubong lower-street context, 2026-09-24

Baseline4dde9837a. The final actual roof preview now has readable court marks and
better middle-distance facades. Its adjacent streets still read as empty dark
ribbons. Source confirms City asphalt already has the retained asphalt texture
and world-scaled UVs; adding another generic noise texture would miss the problem.
Existing buildings, footpaths and planted lots are present and must be retained.

Use the earlier SaBubong architectural research and individually reviewed native
Eskinita/Bayan passenger-tricycle assets. For place context, the official
[Philippines tourism entry](https://philippines.travel/destinations/quezon/index)
describes the local jeepney/tricycle network. The indexed
[PNA Kamias Road caption](https://www.pna.gov.ph/photos/71147) identifies thick
white pedestrian-crossing blocks. The PNA page returned403 and a Commons photo
timed out; no new photograph was viewed, copied or shipped. These are contextual
references, not a claim of road-standard compliance or an exact place replica.

## Local design

- Add two parked passenger tricycles beside the existing inner street edges,
  one on each side of the condominium. Reuse the two existing native models,
  uniform scale and original materials. Keep tyre contact on the actual road top,
  their footprints inside the road strips and away from crossings/buildings.
- Add restrained faded centre dashes and a few pedestrian-crossing groups around
  existing junctions. Keep them out of junction centres, aligned to the road grid
  and grouped into at most two simple paint meshes/materials. No text signs.
- Do not repaint asphalt, façades or the roof. No new colliders, traffic simulator,
  moving vehicles, shadow casters or objects in the playing/recovery space. This is
  visible occupied street context below the roof, not a new playable district.
- Save a scoped, repeatable author and hook it after the existing finished city.
  Validate supported vehicle bounds and unchanged existing collider bounds.

## Native review

Toggle only the new group for same-camera before/after high preview and an actual
roof-edge view. Inspect colour and25percent grey; the court must keep attention.
Keep only additions that read as plausible street use without clutter. Then
refresh only the roof card if the final frame changes. One focused native pass,
one bounded fixture repair maximum; preserve exact failures and no capture loop.
Other maps and the complete integration gates remain separate.
