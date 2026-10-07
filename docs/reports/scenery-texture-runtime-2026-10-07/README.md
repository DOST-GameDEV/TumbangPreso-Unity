# Portable scenery texture identity

The ordinary Windows player build failed because Texture.imageContentsHash is
Editor-only. Editor checks cannot prove player compilation. The failed source3e9
build and all21403restored inputs are retained in Logs/release-ui-perf1007e.

Imported scenery texture identities now use a build-prepared table of exact
Texture references and their Editor content hashes. Player lookup uses that data,
preserving the prior imported texture provenance. It does not use names or sizes
as identity, change texture readability/compression or read back GPU pixels per
frame. Readable dynamic textures use content bytes and unknown non-readable
textures fail closed. The table covers authored animal/drone textures and builtin
fallbacks; actual build-generation coverage is still pending.

Native50640 passes six checks: exact-reference lookup for same-name/same-size
textures, pixel mutation, unprepared texture rejection, null texture and actual
animal/line plus drone/mark GPU parity. All21409source inputs/preferences restored.
[Exact source and raw evidence](evidence.json) is retained. This establishes the
focused Editor/native behaviors, not actual player compile/reopen or frame pacing.
A new ordinary player build and prepared-asset check remain next.

[Unity texture identity documentation](https://docs.unity3d.com/es/2020.2/ScriptReference/Texture-imageContentsHash.html)
identifies the property as Editor-only. No payload, art or kit changes were made.
