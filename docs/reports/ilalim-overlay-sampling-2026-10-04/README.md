# Ilalim painted-overlay speckles

The open Feedback rendering report contains two distinct observations. Its exact
red/cyan foliage screenshot has not been reproduced and remains open. Current
native Ilalim catch playback does reproduce near-black facade speckles with bright
halos. Original authored grime textures are subtle, not black: source RGB minima
are159/183 of255. Read-back GPU textures are finite, minimum0.61176/0.70980.

## Isolation

At the same replay camera, disabling outline/colour grading leaves the black
speckles (grading amplifies the halos). Disabling normal keywords, zeroing actual
painted-surface normal strength and replacing base albedo do not remove them.
Disabling only the painted overlays does. The selected pale-facade region contains
315near-black pixels with the original overlays and0without them. Material/ray
witnesses identify IlalimPainted on the PGH Nurses Home trim and walls.

These were passing diagnostic context checks with visible defects, not successful
visual acceptance. Seven validation-only materials gained the unused serialized
_EMISSIONFROMALBEDO_ON keyword during temporary material controls. Their exact
pre-run bytes were verified against source and restored; the receipt is retained.
No production materials were changed. Owner-provided screenshots remain in the
original Wiki; only our native captures are included here.

## Correction

Explicit-gradient overlay sampling still fails the new visual guard:315dark
pixels remain. ForcedLOD0 removes the defect but is only a diagnostic because it
would discard ordinary distance filtering. The retained correction computes an
isotropic mip level from the overlay UV footprint and its own texture dimensions,
then samples that explicit level. All three overlay slots retain their authored
UV transforms, textures, multiply/mix rules and palette. No map geometry, tuning,
lighting, grade or normal strength is retuned. This establishes the failing
sampling path on the tested backend, not an unverified driver-internal cause.

Explicit-footprint candidate2/2 passes in32.5349141s: zero dark facade pixels of2166
samples with overlays enabled, unchanged replay world-look/global restoration,
and finite imported grime texture ranges. Same-camera native before/after pixels
were inspected. Final trimmed fixture validation is recorded separately below.

This is Unity6000.5.8f1 LinuxOpenGL/llvmpipe evidence. No new packaged player,
Windows/Vulkan/actual-peer or human acceptance is claimed. The exact red/cyan
foliage case remains open. No network protocol change is required.

Final trimmed fixture2/2 passes in32.6115639s, with the same0of2166near-black
pixels. Editor exit0, exact frozen inputs unchanged, settings restored and no
new OOM. Temporary material mutation/capture diagnostics are not shipping code.
