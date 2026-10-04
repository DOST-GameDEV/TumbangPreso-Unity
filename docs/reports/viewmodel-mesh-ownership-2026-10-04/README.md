# Source-safe first-person working meshes

Outline welding writes tangents. The old mesh cache returned serialized source
assets directly, changing Rafi arm .asset files during native rendering tests.
The existing per-path cache now owns one reusable copy, shared by direct loading
and asynchronous warmup. Geometry, UVs and triangles remain unchanged. Reset
forgets outline preparation and frees only owned copies; Editor exit also resets.
No new manager, per-actor copy, art modification or visual redesign is introduced.

Original native four cases at05:52:54UTC: three failures and one missing-path
control. Welding changed the original arm from0 to914 tangent entries. Ownership
retirement and async/direct separation also failed. The fixture restores source
tangents even when reproducing the old failure; Main art is never modified.

Candidate four/four passed05:55:07–08UTC,0.0744516s, zero skips, exit0/no guard.
Original and candidate input maps differ only in ViewmodelMeshAssets.cs. Every
candidate input, including serialized arm assets, remained byte-identical after
execution. Editor/Quality settings restored. Peak tree2,497,609,728bytes and
cgroup7,149,854,720bytes. Checks cover source preservation, geometry, reuse,
retirement without source deletion, asynchronous/direct reuse and missing paths.

Reduced-project native PlayMode scope only. The larger loading-scene fixture
now checks source geometry instead of requiring source/working object identity,
but that suite was not rerun. Packaged performance and owner visual composition
remain open; this report makes no frame-rate or whole-game acceptance claim.
