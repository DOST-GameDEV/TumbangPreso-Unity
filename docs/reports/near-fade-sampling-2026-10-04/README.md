# NearFade atlas sampling correction

The owner's six-map aerial capture exposed severe black patches and bright grading halos on Sa Bubong apartment walls/roofs, plus smaller affected props on Eskinita and Kanto.

## Causal investigation

- Original fixed Sa Bubong east capture: 968 near-black pixels in a 70×130 pale-facade region and 3,468 in a 120×85 rooftop region.
- A one-dimensional palette-only LOD0 candidate changed neither count and was discarded. The visibly affected apartment material actually uses a 528×528 authored atlas, not the nearby 16×1 construction palette.
- Disabling NearFade procedural surface strength or normal mapping did not remove corruption. Disabling grading/outlining removed the bright halos but left the black surface defects.
- Changing the albedo lookup from implicit sampling to explicit footprint-based LOD removed both measured sets of black pixels (0/0), retaining the actual texture, tint, geometry, normals, surface finish and post stack.

The correction computes the texel-space UV footprint and selects its logarithmic mip level. It retains distance mip filtering rather than pinning every surface to mip zero. This isolates the faulty sampling path on the tested OpenGL software renderer; it does not establish a driver-internal cause or identical anisotropic filtering on every backend.

## Validation

- map-surface-final-bubong: 1/1, 17.8232422s; process exit0, unchanged inputs, settings restored, no new OOM.
- map-surface-final-eskinita: 1/1, 20.0381736s; process exit0, unchanged inputs, settings restored, no new OOM.
- map-surface-final-kanto: 1/1, 18.3076336s; process exit0, unchanged inputs, settings restored, no new OOM.
- map-surface-near-fade: 1/1, 8.1540063s; process exit0, unchanged inputs, settings restored, no new OOM.

Actual screenshots inspected: Sa Bubong buildings and Eskinita pots/roofs are corrected; the post remains solid/dithered/cleared at the sampled approach distances. Kanto still has black/white roadside planter artifacts and remains open; its capture case passing does not assert visual correctness. The existing aerial fixture includes an opt-in fixed Sa Bubong region check, enabled with TUMP_MAP_SURFACE_CHECK=1. This is a narrow image regression, not a general proof that every map or renderer is defect-free.

Runtime: Unity 6000.5.8f1, Linux OpenGL/llvmpipe. One native job at a time, separate validation project/profile, actual resource monitoring and settings restoration. Windows/player/GPU acceptance remains separate. The earlier exact red/cyan foliage Feedback screenshot remains open.

Integrated contributor Carrier ownership correction at92733918: current Unity compile and all4 ReaderRestoreChargeOwnershipTests pass in0.099s (exit0). This is a separate lifecycle regression, not additional map visual coverage.
