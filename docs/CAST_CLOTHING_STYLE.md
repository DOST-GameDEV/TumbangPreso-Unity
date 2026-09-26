# Cast clothing style, anchored on Dante and Phaister

Owner, 2026-09-27: *"the clothes of amihan dont look like dante's style too"*, *"can u make the scope wider lets make the
clothes of everyone have a uniform look with dante and phaister as anchor"*. Read beside
[CHARACTER_MODEL_METHOD.md](CHARACTER_MODEL_METHOD.md) (how a model is made and reviewed) and
[Art_Direction.md section 0](Art_Direction.md#0--new-models-must-belong-to-tump) (the laws).

⚠️⚠️ **ONE HERO AT A TIME (CLAUDE.md section 0).** This file is the shared LANGUAGE, measured off the two anchors. Each hero is
restyled in its own builder, rendered beside the anchors (`CastRestyleReview.Run -hero <id>`), looked at, shown to the owner,
and only then does the next hero start. Keep each hero's identity and quiet colour; change the construction.

## The language, measured off `build_bayan_voxel.py` (Dante) and `build_phaister_voxel.py` (Phaister)

1. **A dark base garment carries the body.** Dante's torso is `LEATHER_BROWN`, Phaister's coat `COAT_DARK` black. The
   torso's visible mass, and the lower body under the tails, is the darkest cloth the character wears.
2. **The signature colour is on the LAYERS over the base**, not the base: Dante's green lapels and coat-tails, Phaister's
   crimson collar and cape lining and purple skirt trims.
3. **Every layer's edge is piped in gold (or the hero's one metal), built as GEOMETRY**: boxes 14 to 20 mm wide standing
   4 mm proud (Dante's `lapel-trim-*`, `coattail-hem-*`, `coattail-edge-*`). Never a 7 to 9 mm painted decal line, which
   reads as a thin stripe at game distance.
4. **One big fastening is the focal point**, centred: Dante's jade medallion 96 by 66 mm on a two-tier belt (18 and 32 mm
   tiers with a dark seam), frog knots on the chest; Phaister's big gold belt buckle and collar jewel.
5. **A flared lower layer** (coat-tails, skirt panels) with its own trimmed hem, over dark legs.
6. **Cuffs are geometry** in a contrasting cloth with a trim edge (Phaister's white cuffs with gold emblem).
7. **Hair is two-toned**: a lit tone on the top locks and fringe, the dark tone underneath and behind.

## Hero by hero

| Hero | State | Evidence |
|---|---|---|
| Dante | anchor | |
| Phaister | anchor | |
| Amihan | v7 restyled 2026-09-27: dark teal base and tails, cream lapels and cuffs piped gold, two-tier rust belt with a gold kasikus medallion, gold-edged teal capelet, two-tone hair. Face not yet touched. Awaiting the owner. | `ArtSource/amihan/concept-20260925/evidence/amihan-v5..v7-restyle-beside-dante-phaister.png` |
| Rafi | next after the owner's word on Amihan (PRIORITY 1 with her) | |
| Nemu | owner 2026-09-27: *"nemu needs a rework too / she doesnt look like she belongs in the cast / i want hher to stay small and cute with loose clothhes"* | |
| Sean, Zack, Cheska, custom | to follow, one at a time | |
| Paete | a tree: bark and vines, not cloth; the language does not apply | |
