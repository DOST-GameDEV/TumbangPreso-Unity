# Domestic window at3_W,2026-09-23

The repeated shop display is now a household window: a partly raised bamboo shade,
small open basket and folded cloth use the existing rail and supported sill.
Only3_W's two product submeshes are removed. Its other fitted details, house body,
windows, door and steps remain, and0_Wkeeps its original store display. Added
geometry is1176vertices/one renderer/material, with no new collision. Placement
uses the measured opening center(2,1.25,2.45) and sill height.745in house coordinates.
Imported masters remain unchanged. Research/concept critique: ../eskinita-domestic3w.md.

The focused case passed1/1in4.995s. Matched frontage/street before and after,
960x540and25percent greyscale were inspected. The domestic sill is legible without
changing the house silhouette or blocking the doorway. Existing utility poles,
laundry and other pending facade/roof/composition work remain. This is a completed
window/use distinction, not the entire house or map.

The same check catches a real material re-authoring defect discovered when
comparing candidate files: refreshing existing timber materials reverted their
saved grain option, despite the earlier first-creation visuals passing. The first
focused run failed on5_W. CopyPropertiesFromMaterialalone did not resolve it.
Configuring a fresh material completely before copying its final serialized data
into the existing asset preserves the original GUID and the intended property.
All12saved files now retain _DeckSurface1and the fresh PlayMode check confirms it.
The rejected XML is retained. No assertion was weakened and no capture fixture
repair or whole-suite rerun was performed.

Guarded runs preserved the named profile/shared input; generated churn was backed
up then restored. Evidence is Editor world rendering, not native performance or
full integration qualification. The source and generated inputs are preserved in
QUALLogs/refine2-domestic3w-v1andv2and named candidate stashes.
