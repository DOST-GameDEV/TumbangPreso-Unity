# Original civic paving source

Authored by tools/author_civic_paving.py. One repeat is6m, with1.5m slabs and12mm
joints. The deterministic seed and dimensions are in paving-source.json. The
albedo is imported in Art/CivicTown with mipmaps and repeat wrapping. This is
original procedural artwork, not a downloaded photograph.

CivicTownAuthor owns world UVs and the stepped road/pavement terrain. The terrain
has closed outer skirts/bottom and matching mesh collision; road markings are a
submesh of that surface. The existing lower floor remains a fallback. Ground is
outside scenery NearFade so its paving remains visible near the camera.

The generated normal-map study remains under Logs and is not wired or claimed as
a visible improvement. Broader material/normal-map/scalability work remains open.
