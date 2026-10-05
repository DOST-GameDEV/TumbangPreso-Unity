# Transparent vector TUMP logo

The owner supplied `TUMP (9).png` and requested vectorization to remove visible
white paper and stretched presentation. Source SHA256:
`897ed8b5cd3f0ac7d2c6463bd586a3ac9ee78dada833f712097337b44cdf78ca`.

The editable master is `Assets/TumbangPreso/Art/ui/brand/source/tump-logo.svg`.
It contains 38 spline paths and no embedded raster image, script, external
reference or background rectangle. Its transparent viewBox includes two pixels
of clearance around the ink. Tracing keeps nine measured source fills and removes
white-paper fringes on the outer red outline. This is a trace of the supplied
artwork, with small curve/colour approximation inherent in tracing.

Unity's existing shared brand resources use the transparent 2048x1348 export of
those paths. Existing Image `preserveAspect` and legacy RawImage `FitInParent`
retain the logo's ratio in their layout slots. The full UV rectangle avoids
cropping or stretching the earlier temporary export. No new Unity SVG package
or runtime tracing dependency is introduced.

Native PlayMode logo/credits1/1 passed on D3D11, PID11788, exit0. Shared routes,
complete texture bounds, the supplied artwork's proportions and existing credit
layout are checked. Actual credits pixels were inspected: no white paper,
cropping or stretched mark. Quality, editor/player input preferences and profile
were restored; 202 generated GUI importers were retained then restored exactly.
The unrelated frozen F489 multiplayer package remains unchanged.

Tracing tool: [VTracer](https://github.com/visioncortex/vtracer), local Python
0.6.15 spline conversion. Cairo renders the path-based PNG export. Temporary
conversion dependencies are task-owned and removed after the export is accepted.
