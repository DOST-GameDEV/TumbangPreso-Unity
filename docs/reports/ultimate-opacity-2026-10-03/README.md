# Ultimate frame bleed-through

The shared ultimate RawImage now displays the camera's already-composited RGB
as an opaque frame. Residual render-texture alpha no longer reveals the frozen
gameplay view underneath. The existing canvas entry/return fades, UI clipping
and stencil are preserved. The image starts hidden until its first Draw; the
view owns and disposes its material. No hero-specific art, shots, timing, audio,
mechanics or protocol change.

## Reproduction and verification

A native GPU test constructs the actual UltimatePhaseView frame consumer and
supplies controlled camera-frame alpha over an underlying red view. Original:
alpha 0 fully exposes the underlying frame; alpha 0.25 visibly mixes red into
blue. The opaque input control passes. This is two causal failures and one
control, not a claim that a full hero film was captured. See investigation.md
for the separate full-scene memory-limited attempts.

First candidate removed the RGB bleed, but inspection found that its deliberate
half-fade produced output alpha 0.749 over an opaque background. Separate alpha
blending corrected that regression; the final test explicitly asserts it.

Final **3/3 pass**: source alpha 0, 0.25 and 1 all show blue without red leakage
at full canvas opacity, and deliberate half-fades retain the blend and opaque
composed alpha. Final tree RSS 3,844,784,128; container 7,489,794,048; exit 0,
no guard stop. Settings/profile restored, frozen source hashes unchanged.
Earlier compile/import and shutdown guard events remain recorded.

These are actual native shader/UI pixel checks. Full hero/player footage and
human confirmation of the reported all-hero afterimage remain separate.
No automatic passing claim is made for the retired full-scene capture probe.
