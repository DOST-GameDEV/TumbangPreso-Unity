# Keep late-created Ilalim street voices out of map previews

MapPreviewSurface.Silence disables sources present when it claims a loaded map.
SidewalkLife creates its voice pool later, so it bypassed that pass. The actual
additive preview plus its real authored story steps created 10 enabled sources
with assigned clips behind the menu. Normal game scope also created 10.

One playback condition now excludes the preview layer before creating or using
voices. Authored clips, gains, mix, RNG/story and normal game playback are unchanged.
This is a demonstrated playback bug fix, not sound replacement or redesign.

Same native case fails before and passes 1/1 after: preview voices 0, normal
game-scope voices 10. It uses real SidewalkLife.Simulate steps to reach delayed
sources without a long idle wait, and preserves AudioListener.pause afterward.
Three frozen input hashes unchanged; zero fixture repairs; guarded profile and
shared input preferences restored. No listening or whole-audio-system claim.

Current internal protocol-129 Windows player from7b48f76d7 predates this narrow
preview-audio fix. Its actual rebuilt-map network/render observations remain
separate evidence; no rebuilt player or repeated peer run is claimed here.
Native final log is Logs/ilalim-preview-audio1002/final.log in the isolated project.
