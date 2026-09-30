# Finite toon-ramp sampling

## Reproduction and cause

Cloud Linux full-map characters and slippers appeared black/speckled with bright
edges. The same symptom appeared in live bodies, reconstructed footage and the
main camera. Prior controlled tests excluded camera grade, screen-space outline,
ink hull and shadow settings as the cause. Windows reproduction was unverified.

The new same-camera experiment retained the exact scene/materials and changed
only the camera-scoped ramp inputs. Setting world-look weight0 still reproduced
the corruption; substituting a white texture for the ramp restored normal body
colours. Replacing only the implicit ramp sample with explicit level0 then
restored colours with the original authored ramp retained.

The generated64x1 lighting ramp has no mipmaps. Both opaque and transparent
implicit lookups reproduced non-finite lit/shade measurements in the existing
native lighting calibration. Their explicit level0 samples produce finite values.
This establishes the reproducing shader lookup and a bounded correction on this
platform; it does not establish a driver-internal cause or a Windows reproduction.

## Fix

Only the ramp lookup changes from tex2D to tex2Dlod at level0 in Toon.shader and
ToonTransparent.shader. The authored texture, palette, formulas, shadows, material
values and character assets remain unchanged. Shared rendering corrections apply
to all heroes; no character-specific redesign was made.

## Qualification

Unity6000.5.8f1 LinuxOpenGL/Mesa llvmpipe, isolated named profiles, guarded graphics,
mip2 texture residency. Baseline2/2 fail on non-finite native lighting. Final opaque
lighting contract and actual-camera comparison pass. Transparent finite lighting
also recovers; its first parity check failed because its authored .55/.02 band
settings differed from the opaque calibration's .45/.03. The test material now
uses identical calibration inputs without changing authored defaults or relaxing
assertions. That one corrected transparent case passes1/1 in6.79seconds, exit0.
Both final shader byte hashes match the shipping candidate.

Three distinct final cases are qualified across the incremental runs. The existing
contrast and unowned-preview-global checks remain. Same-camera before/after frames
were inspected: normal body, face and slipper colours return with the original
ramp. No new OOM or memory-guard stop. The temporary comparison probe was removed
from the validation import tree after preserving its private source/run evidence.

[XML, receipts, source hashes, measured values and images](toon-ramp-checks/).
No new standalone player build, Windows D3D11, physical GPU/device or peer claim.
The previous Vulkan symptom was observed, but this fix has not been rerun there.
