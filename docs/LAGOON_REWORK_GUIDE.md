# Lagoon Court rework guide

Owner, 2026-09-26: *"i wanna rework lagoon court"*. This is a full rework, not the
REFINE-2.6 per-family refinement (`docs/TODO.md`), and it follows the Kanto pipeline:
blockout, models and review renders in Blender first, export to Unity only after approval
(`docs/KANTO_DESIGN_GUIDE.md` § 8). The current map as found: `Logs/map-lineup-v1/sheet_lagoon.png`
(a flat brown deck ring over flat teal water, plain huts, little vegetation).

## 1 · References (owner-chosen)

| Reference | Take from it |
|---|---|
| [Anastasia Papaioanou, Stylized Fishing Village](https://www.artstation.com/artwork/GvJv5a) (owner: *"seems like a good ref"*) | **The art style.** Chunky faceted rock island the village climbs; saturated turquoise water with a lighter band at the sand; stocky plank houses with oversized, deep-eaved plank roofs; dense tropical planting (palms, broad leaves, red and orange flowers) packed round every building; piers, stairs and boardwalks at several heights; boats, crates, barrels, nets, lanterns everywhere; one landmark visible from anywhere; warm sun under big painted clouds. Built from a modular kit (plank wall modules, stacked roof modules, stair pieces, a small prop kit), the same way as Kanto. |
| Owner's photograph of a Filipino stilt-house village at sunset (supplied 2026-09-26) | **The subject.** *"we should make it more filipino like too btw, like these stilt houses but following the stylized artstyle"*. A row of stilt houses joined by a raised walkway over clear shallow water; steep **nipa/cogon thatch** roofs; woven **sawali** (split-bamboo) and plank walls; slender irregular bamboo and timber piles with X bracing; bamboo railings and ladders down to the water; fishing nets hung to dry between houses; small painted **bangka** outriggers moored underneath; a green palm-covered hill and a sand beach behind. |

The existing primary-source notes still apply (Sama Dilaut stilt homes, lepa houseboats,
Lookan Banaran photograph): `docs/reports/map-by-map-refinement-2026-09-23/lagoon-reference-notes.md`.

## 2 · Direction

- **Filipino stilt village in the fishing-village style**: stylized shapes and hand-painted
  textures from the ArtStation reference, applied to the Filipino subject of the photograph.
  Thatch (not plank) roofs on most homes, sawali and plank walls, bamboo piles and railings,
  nets drying, bangka boats, a green hill and beach behind.
- ⚠️ **Role hues** (`Art_Direction.md` § 1): the reference's bright turquoise roofs sit near
  defence blue `#0080e8`. Roofs are thatch, with the odd painted tin roof in teal-green or red;
  painted walls avoid offence orange `#f87020`.
- The Kanto texture rules carry over (`KANTO_DESIGN_GUIDE.md` § 3): flat fills, feathered
  patches, hand-drawn shapes, low contrast, no grain. Bark is settled on J.
- **Gameplay constraints to keep until decided otherwise** (from the notes above): a tested
  28 x 26 m court, the circulation routes, deck and water recovery, and collision.

## 3 · Open decisions (asked 2026-09-26)

1. Where the court sits: a raised deck over the water (as now), a sand platform, or a rock shelf.
2. Island shape: one tall island behind the court, or a cove with rock walls around it.
3. Pipeline: Blender first like Kanto (default).
