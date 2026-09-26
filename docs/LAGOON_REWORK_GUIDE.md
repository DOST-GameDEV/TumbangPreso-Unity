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

**Decided 2026-09-26:** the court sits on a **rock shelf** partway up the island (the village
below and around it); **one green hill behind** (north) with open water south. Blockout:
`tools/author_lagoon_blockout.py` -> `ArtSource/lagoon/lagoon_blockout.blend`, renders in
`Logs/lagoon-blender/blockout_*_vN.png`. Court floor z = 0, walkways 2.7 m below, water 4.5 m
below; stairs from the walkway loop up to the shelf at its south corners; a white capilla on the
hill as the landmark. ⚠️ The shelf changes the water mechanic: slippers that leave the court now
fall 4.5 m. Whether they return (as today) or the shelf edge gets a low wall is a gameplay
decision still to take before the Unity build.

## 4 · Layout analysis of the reference (owner, 2026-09-26: "i was thinking of a more natural platform. need you to analyze the layout from the references")

The blockout's square rock shelf was rejected as not natural. What the fishing-village reference
actually does with its ground:

1. **A horseshoe cove, not an island with a platform.** Two rock arms wrap a tongue of lagoon;
   the village faces inward across the water. Open sea shows only through the cove mouth.
2. **Flat ground is made of pockets between boulders.** Every level space is a sand or grass
   clearing CRADLED by clusters of huge rounded-faceted boulders (several stacked, each 3 to 8 m).
   No cut edges, no retaining walls: the rocks are the edges.
3. **Terraces climb in three or four tiers**: beach, a mid terrace, upper ledges and the peak,
   each a clearing among boulders, joined by wooden stairs and plank walkways with rail fences.
4. **The beach is a curved ribbon** following the water, with an organic edge, a pale foam band
   and piers poking out from it into the lagoon.
5. **The landmark sits at the waterline in the middle of the view** (the painted rock), so the
   cove has a focal point from every tier.
6. **Planting clusters where rock meets flat ground**: palms leaning out of rock seams, broad
   leaves and red/orange accents at boulder feet, never spread evenly.
7. **A tall rock spire behind everything** gives the silhouette.

**Proposed for the court:** the court is the cove's BIGGEST CLEARING, a packed-sand terrace one
step (about 1.2 m) above the beach, cradled on three sides by boulder clusters whose inner faces
stand just outside the walls at +/-13, and open on the fourth side down to the beach, the lagoon
tongue and the stilt houses along its edges. Upper terraces with more houses and stairs climb
the rock behind; the capilla sits on the highest ledge under the spire.

**2026-09-26, owner on cove v4:** *"could the main beach area be more organic?"*, *"how absurdly
symmetrical the map is"*, *"we need space for boats and free-standing stilt houses because badjao
tribe isnt particularly land based"* (with a photograph of a Bajau water village). Cove v6
(`tools/author_lagoon_cove.py`): one asymmetric island weighted north-west, drawn from a
hand-placed coast curve (a long sand spit curling south on the west, rock dropping straight into
the sea on the east), a beach band with an irregular landward edge, and the whole south and
south-east left as open water for a Sama-Bajau village: small free-standing stilt homes strung
in loose clusters along plank walks, some alone, laundry lines, lepa houseboats and bangkas, and
one long walk to the beach by the court.
