# Eskinita outer context, first refinement group

Source base6c792aa24 plus the adjacent committed author, scene, palette-registration
and focused check. This completes one context group, not Eskinita or REFINE-2.

The lobby's real map overview exposed the edge of an isolated neighborhood.
EskinitaContextAuthor adds26retained-family houses along four explicit outer rows,
with two side streets connected to the existing cross streets and two outer cross
streets. Footpaths leave junctions open. Existing houses, shop pockets, court,
original models and source materials are retained.

The scene review found218new serialized blocks,0removed blocks and1modified
existing block: Dressing's child list gains the new root. Authoring checks verify
grounding, nonoverlapping house bounds, no new colliders and unchanged bounds/count
for every existing collider. The new context contains54mesh renderers, including
road/path pieces. Fine near-house detail is intentionally omitted at this distance.

## Visual finding and correction

Variant1passed its placement check but failed visual acceptance: the new names
bypassed EnvColourPass and left every roof in the source mint atlas. The rejected
image and passing-but-insufficient XML are preserved. It was not accepted as done.

Variant2registers only the new Eskinita group with the existing palette pass and
uses its established Bahay_ instance-name contract. Existing palette values,
atlas order and old instance names are unchanged. The check now asserts actual
roof-atlas assignment on every new house, catching the missing integration.

The corrected before/after uses the real MapPreviewSurface camera, with a fixed
pose and only the new group toggled. The full1280x720and960x540frames and25percent
greyscale were personally inspected: the added roofline/side streets extend the
visible neighborhood, match its existing palette and leave the central court clear.
This is actual preview rendering, not the generated concept image or a native
full-HUD capture.

## Bounded evidence

- Initial author attempt stopped during the owner-confirmed accidental app close.
  No scene output was produced; profile hashes and shared Editor input were intact.
- Resumed author:26grounded/nonoverlapping houses,4road segments; old colliders unchanged.
- v1focused placement check:1/1passed,1.431s; visual result rejected for mint roofs.
- v2focused placement/palette/preview check:1/1passed,1.259s; images inspected.
- No capture-tool repair. Two visual variants. Both guarded runs restored the4
  named-profile files and1shared Editor input preference. Generated meta/auditor
  churn was restored after saving full patches; DEV's2original metas untouched.

The run question is answered. No unchanged rerun or native rebuild is required for
this group. Low/native performance and complete play/spectator integration remain
in the final map gate; no frame-rate improvement is claimed from these captures.

## Still open in Eskinita

Individual near-house/prop inspection and refinement, material/construction detail
where genuinely weak, more coherent household/activity pockets, vegetation, sky
and distant-context assessment. LIGHT-1is a separate unmerged branch, so its bright
grading/edge changes are not claimed here. Keep the whole Eskinita TODO unchecked.
