# Kanto street-tree guard UV repair

The black/white glowing objects in the owner aerial screenshots were street-tree metal guards, not missing tree or planter models. Screen-ray material witnesses identify street_young trunk submeshes using Standard and railing/metal_dark materials.

The tree buffer keeps cylindrical bark UVs, but its added metal boxes/cylinders never received UVs. All180metal triangles collapsed to the same texture coordinate (0,1 in glTF). The canonical builder now projects only those two metal materials at its existing2m texture scale while retaining the authored bark map.

The retained GLB was corrected in place only in its312metal UV coordinates. All other binary data, geometry, normals, indices, bark/leaf UVs, materials and transforms stay byte-identical. No model regeneration or foliage simplification. The two repaired primitives now have0degenerate UV triangles versus180originally. Existing10degenerate bark cap triangles are unchanged and outside this repair. The guarded applied repair and byte-range receipts are retained here.

Native Unity final capture1/1 passes in17.923s, exit0; east/west frames inspected. Black patches and white halos on the street guards are gone with original textures, normal mapping and grading active. A separate actual Blender check passes3cases: ordinary mapping, keep-all-authored mapping, and selected-metal-only mapping. Python syntax checks pass.

Diagnostic history: initial renderer witness failed because it tried reading a nonexistent cloud material texture property; corrected witness passes. Initial material controls failed because static-batched meshes are non-readable; removed that diagnostic access (the reported zero tangent count is NOT valid mesh evidence). Corrected normal-off and white-albedo controls both preserve the defect. Those temporary diagnostic edits are not in the shipped fixture.

One native job at a time, frozen validation input manifest unchanged, profile/settings restored, no new OOM. Linux OpenGL/llvmpipe native evidence; Windows/player/GPU and human verification remain separate. This does not close the original exact red/cyan foliage report.
