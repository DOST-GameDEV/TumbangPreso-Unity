# Lighting branch integration,2026-09-24

The owner explicitly requested tracking and merging lighting/peak-bright-overhaul
into ASTRAReworks. Commit50f1fc255merged cleanly at241e13bb5, preserving the map,
UI and Rafi work. The new lighting is the baseline for subsequent art decisions.

The two existing Stage/ramp cases passed2/2in61.384s on this Windows checkout.
They cover the applied five-map rig, haze past the court, ground-lift discovery,
measured toon contrast and restoration/isolation of shader globals. This is a
bounded integration check, not a new full regression or performance benchmark.
Five-map stage after images and Eskinita before/after were personally inspected;
greyscale thumbnails were inspected for Eskinita, Ilalim and Lagoon.

Eskinita's previously near-black road/shadow areas are much more readable. Ilalim's
working frontages and bridge structure remain visible in shade. The five maps have
lighter ambient color and clearer depth while retaining their existing geometry.
Keep judging material/shape changes under this look; do not add extra geometry or
local brightness merely because an older dark screenshot hid existing detail.

The lighting lane still records remaining tuning questions, preview integration
LIGHT-1.8and performance/native qualification1.9. Do not mark those complete from
this merge. MapPreviewSurface integration was noted as uncommitted work in that
lane, so this task does not create a competing implementation. Fetch the tracked
branch during subsequent batches and merge its next available changes safely.

Masonry draft was preserved separately: its first whole-wall overlay flattened
existing plaster despite a passing structural check. The revised draft retains
the original walls and limits additional coating to lower courses. Resume that
work under this adopted lighting; the rejected generated scene is not merged.

Guard preserved four named-profile files and shared Editor input. Full generated
diff saved in QUALLogs/lighting-merge-20260924then known churn restored. Original
DEVPNGmetas excluded. No standalone build, Desktop replacement or extra test
framework was used. All remaining map/gameplay/qualification requirements stay open.
