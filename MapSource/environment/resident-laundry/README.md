# Resident clotheslines

Original metre-scale Blender source, authored by tools/author_resident_laundry.py.
The original courtyard and alley assemblies retain their original palette.

Owner addition on2026-09-15: three more lines for Eskinita, using different garments
and visibly supported rope ends. The new sets are alley-colour-line (eight garments,
including trousers and a dress), courtyard-family-line (four mixed garments), and
courtyard-sheets-line (sheet, towel and shirt). No stock photography or generated
texture is applied. Each garment uses a separate folded mesh and fixed wooden pegs.

The .blend files preserve native geometry and materials. Matching .json files record
span and sag. Runtime .glb files live in Assets/TumbangPreso/Art/models/resident-laundry.
ResidentLaundryAuthor places two street lines below the electrical conductors and
two lines between house facades and their front boundaries. It builds measured rope
wraps and crossed hitches around utility trunks or freestanding posts, with short
loose tails. The private-yard support feet meet the existing paving.

Reauthor only these additions with Blender --background --python
tools/author_resident_laundry.py -- --new-only --out Logs/resident-laundry-review.
Review the outputs before copying them into the source/import folders. Use the
guarded Unity runner with ResidentLaundryAuthor.RefreshEskinita to refresh only
the laundry groups; it does not rebuild houses, streets or utility poles. Compare
Logs/laundry-review/semantic.txt across two runs to ignore random Unity object IDs.

LaundryMotion clones only cloth meshes at runtime. Pegs, rope and hitches stay fixed;
billow weight falls to zero at the pinned top edge. Source meshes remain reusable.
