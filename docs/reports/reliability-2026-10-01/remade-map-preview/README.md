# Remade previews retain the explicitly requested map

Owner report: all remade maps showed a grey preview this morning. Reproduced
actual additive preview requests for Kanto, Lagoon Cove and Ilalim ng Tulay:
Showing matched the request, but all had0active renderers and background-only
PNGs. Meshes were loaded but parked; this was not a missing shader asset.

MapPreviewSurface.Start unconditionally requested SceneFlow.SelectedMap even
when its caller had already requested a different map. The queued global default
parked that explicit request, leaving a briefly reported chosen map behind the
background. Start now asks for the default only when no request/showing/queue
already exists. Existing maps/cache/shader/camera/art settings are untouched.

The first completion-callback fence hypothesis failed3/3 and was removed. That
failed run and captures are retained; no hidden green rerun or weakened checks.
The diagnosed Start fix passes3/3 with original visibility/shader assertions:
Kanto2316active/804in-frustum, Lagoon2552/1342, Ilalim1349/1069; no missing/
unsupported/error shaders and all cameras adopt scoped world look. Actual PNGs
were visually inspected and show their maps.755frozen inputs unchanged.

Unity6000.5.8f1 Windows/D3D11 isolated source0e3b34908+owned overlays. Sequential
map retirement, installed idle import-worker retirement temporarily1ms/restored;
no guard/count changes, tooling repair or new memory failure. Guards78488/11325/
92826 terminal, named profiles/input preserved. No map/lighting/model/clip/VFX/
SFX asset edits or wider loading overhaul. This narrow preview defect is the
owner's explicit loading-scope exception; broader loading still belongs to friend.

This qualifies actual native preview surfaces, not a fresh Windows/Android
player, network-driven menu selection, all cached transitions or whole shader/
effect readiness. The older126player observations predate this change. Continue
the owner's independent optimization/readiness goal; DOTS owns Feedback queue.
