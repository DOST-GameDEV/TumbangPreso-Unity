# Five transparent vector brand variants

Owner supplied `TUMP (11).png`, requested all five pictures vectorized and matching
game artwork replaced, then authorized a suitable outline. Each variant is a
real spline-path SVG with no embedded bitmap. The SVG masters live under
`Assets/TumbangPreso/Art/ui/brand/source`:

- `tump-logo-colour.svg`: coloured complete TUMP mark.
- `tsinelas-mark.svg`: slipper alone.
- `tsinelas-hit.svg`: slipper and impact.
- `tump-logo-outline.svg`: black outline with transparent interior.
- `tump-logo-textured.svg`: black outline and the supplied light texture detail.

The thin outer keyline uses the existing game cream `#FCD39F`. It follows only
the external silhouette; the black outlines, transparent holes and intended
light texture strokes are preserved. Native renders on both existing dark-red
and paper backgrounds were inspected. Curve tracing slightly approximates the
original raster edges; it does not redesign the marks.

Transparent exports replace the matching Art/Resources brand assets, old
login/main logo pieces and configured application icon. The existing icon GUID
and square padded canvas are retained. The plain slipper is available as
`UI/brand/tsinelas_mark`; unrelated character/object avatars and baked backgrounds
are preserved. Existing shared logo and slipper resource paths reach current
login, loading, credits, picker and settings consumers. Layout slots retain
aspect fitting rather than stretching the artwork.

`BrandArtworkImport` targets only these named PNG exports. It retains alpha,
clamps edges, disables power-of-two rescaling, compression and mipmaps and keeps
2048px source resolution. This fixes the old slipper mark's opaque white page
and its old NPOT-resize distortion; future reimports keep the corrected settings.
Unity's built-in SVG import generated the five new masters' metadata.

One existing native PlayMode case now exercises the shared logo/credits routes
and actual five-variant gallery: 1/1 passed, PID18064, D3D11, exit0. It checks all
five imported aspect ratios, four transparent texture corners per mark and
preserved UI aspect fitting. Actual gallery and credits screenshots were
inspected. Quality, editor/player input preferences and the isolated profile
were restored. 202 unrelated generated GUI importers were retained then restored
exactly; 15 intended PNG import changes and five new SVG import records are kept.
This native UI evidence does not relabel the earlier frozen F489 peer package.

Original sheet regions, SVG/PNG hashes and installation mapping are retained in
the accompanying receipts. SVGs plus transparent PNGs are delivered in the local
Downloads `TUMP-vector-set` folder. Local VTracer0.6.15 tracing follows
[the upstream tool](https://github.com/visioncortex/vtracer); temporary dependency
files are removed after acceptance.
