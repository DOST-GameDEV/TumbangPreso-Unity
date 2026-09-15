# Nemu's body hands match the no-finger direction

The body/FPP baseline exposed three stepped skin pieces at each human-body cuff.
The source generator explicitly named the last two pieces fingers and fingertips.
The older generic thumb cleanup had not removed these original pieces.

Each side is now one lightly beveled block palm, using the original skin slot and
arm joint. The overall hand reach is unchanged. The repair changes only those
hand triangles; costume, face, skeleton, palette and animation samples are retained.
The current familiar and monster assets are unchanged. Inday was not reworked.

Source durability: build_nemu_voxel.py now generates single palms. The new surgical
repair tool defaults to inspection and marks its completed operation. A repeated
applied run validates one connected skin component per arm and leaves identical
GLB bytes. It does not regenerate the rest of the character.

Evidence:

- Animation SHA256 stayed
  c9253a340f2654e51418f2da621c603a26ccbff3acbb6b12f3c88d3ca41c4eab.
- Untouched geometry was compared after serialized roundtrip; receipt included.
- Roster and arm assets were rebuilt and validated. Eight unrelated hero arm
  rebakes omitted their old tangent streams but had exactly identical positions,
  normals, UVs and indices; those unrelated files were restored to their old bytes.
- Focused actual body/FPP render passed1/1; images show the single tucked palm.
- Quick, held-left and moving-right spin throws passed through the normal intent/
  Carrier path. Head-surface intersections were0/1117 shoe vertices over6,96 and94
  held samples respectively. Recorded owner/body motion uses measured real timing.
- Guard receipts:e56b619d3f4c,d9e0190eee2d,287b170f60ba.

The throw launch used the wrong output environment-variable name and therefore
reused its default scratch directory. Only fresh files referenced by the new CSVs
were copied into Logs/demo-nemu-throws; surplus old frames were excluded. The
correct future variable is TUMP_THROW_MOTION_REVIEW. Provenance is retained.

Visual review: no finger steps remain; the sleeves keep their original silhouette
and the existing FPP colors correspond to the body. The three recorded releases
remain intact. Moving aim-guide visibility merits its own next check; this report
does not claim every aiming, skill, device or network condition is qualified.

The preserved native v2 demo-loop baseline predates this model correction. A later
native candidate must include it. Continue the visible-gameplay and full TODO queue.
