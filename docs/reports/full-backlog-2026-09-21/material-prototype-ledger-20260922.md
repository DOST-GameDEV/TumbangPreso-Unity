CURRENT MATERIAL/SKY WORK, not yet accepted or published:
- NeighbourhoodSky.shader and MapAtmosphereAuthor.cs now have per-map cloud banks/
  wisps, sun and existing weather tint/exposure. Authorv1 passed; owner-viewv1 passed
  1/1,59.612s/200frames, guardc258918bf0b1. Visually REJECTED internally as too blurry/
  smeared. V2code tightens the edges, adds finer billows and changes scale/coverage;
  copied to validation, but its4sky material values still need RefreshCloudMaterials
  before another capture. Do not copy v1materials back to development as final.
- New EnvironmentSurface.cginc plus opt-in NearFade.shader properties implement
  16distinct material families at object-relative metre scale. Defaultkind0 preserves
  current unassigned surfaces. NearFade.cs has an editor-only copy helper that reuses
  its existing color/texture/roughness translation. No per-frame material creation.
- New MapSurfaceAuthor.cs (+generatedmeta) owns scoped persistent material variants,
  source-preserving palette-role meshes and coverage reporting. Its UV studyv1 used
  a transparent sprite shader and drew internal floors over facades: invalid study.
  V2switched to opaque but exposed a surface-parser stack overflow from nested16way
  branching. Flattened the disjoint branches; v3passed with ShaderHasError guards,
  guard90cbbb5adc56. Imported commercial UV columns identify walls/glazing/plinth;
  column0upperV is roof and lowerV is trim. Never infer these roles from color hue.
  Original GLBs remain unchanged; unsupported UV/color layouts refuse, not guess.
- Current guarded job session59941: MapSurfaceAuthor.Run on ESKINITA ONLY,
  Logs/map-surfaces-eskinita-v1.log and output folder. New derived materials/meshes
  and scene edits are being authored in validation only. Sourcebase there is still
  db976126 plus the explicit copied owned files; all jobs use the named profile.

Newest results supersede the prototype pointers above:
- Surface authorv2mapped678Eskinita renderers, including36actual residential houses
  from the CITY kit (the earlier commercial-only study did not cover them). Saved
  prefab overrides are now explicitly recorded and verified after scene reopen.
  City palette has separate wooden-door and concrete-step regions; originalgeometry
  stays intact. Cotton/rope are also classified. Remaining materials are reported.
- Actual Eskinita surface/sky-v2owner views50frames passed. Sky timeline check passed.
  The recorded-tint check initially failed exact float equality; logged material
  round-trip error is1.04e-7/restoration4.21e-8. Strict1e-6color comparison now passes
  1/1/3.866s in map-weather-tint-v2, guard0ac208f6712a. Originalfailure preserved.
- Procedural skyv2still looked like repeating shapes, so it is REJECTED too. Source
  shader is archived in full-backlog report/sky-noise-v2.shader.txt. Four2kCC0Poly
  Haven pure-skyHDRs downloaded with verified size/MD5/SHA256, total20.7MB; rawfiles
  and provenance are in ArtSource/environment/skies/polyhaven. Public API requires
  a named User-Agent; used documentedTUMPAssetImport/1.0, no paid service or key.
  New NeighbourhoodSky uses their cloud shapes with map colors; sun/exposure/ground
  are not pasted. Import/normalization passed map-cloud-sources-v3, guardb29842e6a8d5.
  Four importedHDRs/metas and sky materials are ONLY in validation so far; unreviewed.
- Native static batching rewrites object matrices. DetailMesh now bakes physical
  UV3coordinates on shared(source,scale)derived meshes; original UV0/UV1/geometry
  remain. Shaderpacks roles/UV into one varying. Authorv3FAILED before edits because
  the initial separate UVvarying used11interpolators(max10); packed form is the fix.

CURRENT JOB session48629: map-surfaces-eskinita-v4.log, same named profile. Reauthor
Eskinita with batch-safeUV3and packed shader input. Next inspect result/errors, then
actual owner views with sourced-cloudv3 and the new material coordinates. Continue
all-family review and the other3maps; generation hooks/repeatability, normalmapped
surface handling, native build/cost and additionaldetail remain unfinished.

Owner latest AFK: keep going while they sleep, no random stop. They asked about
weird colored buildings; those are temporary diagnostic UV studies, never saved
as map art. All actualsurface materials have_SurfaceDebug0. Label future studies.
The entire TODO remains assigned; Inday/full remaining checks and hero/map expansion
still follow. Never close the goal at a material draft, passedtest or map checkpoint.
