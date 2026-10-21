# UI source quality and generated edges

Owner report: artwork, text and controls look pixelated with unwanted light rims.
This unit preserves existing artwork and makes generated control edges smoother.

## Change

The scoped artwork importer preserves source resolution up to8192 pixels,
uncompressed color/alpha, NPOT dimensions and correct color-space handling.
It runs on future reimports and shares its policy with manual menu authoring.
Only masks use linear data; the sky background color remains sRGB. Cloud and leaf
layers retain mipmaps for minification. Raising the cap does not upscale source art.
The source scan found three old undersized caps: the3840x2160 legacy backdrop,
2172x724 cloud-bank-a and2081x756 cloud-bank-b. All source PNG bytes are unchanged.
Current selected assets have no enabled platform overrides.

Generated controls default to no extra cream rim. Dark outlines, shadows, focus
rings and line glyphs use a transparent one-screen-pixel edge instead of jagged
hard triangles. Arc detail adapts to rendered size. Hit areas, supplied painted
borders, authored colors and navigation stay intact.
Seeded contour values are cached: rebuilding an unchanged shape no longer creates
Random objects or corner arrays. Changing a seed still changes the same contour.

## Native evidence

Based on f198637b8 with the exact changes in this commit. Native PlayMode realHome
capture passes at1920x1080,2560x1440 and3840x2160; actual images inspected.
Native allocation checks pass2/2:500 warmed pressable and rounded contour rebuilds
allocate zero bytes and preserve seed determinism. Effective-import checks pass3/3
with actual full dimensions and uncompressed RGB24/RGBA32. Opaque RGB24 is valid;
an earlier test incorrectly requiredRGBA32 and its failure remains private.
All parents exited0 and isolated profiles/shared preferences/input/Quality restored.
202 incidental input/composition metadata changes were restored to frozen bytes;
76 intended owner/brand metadata changes remain. Unrelated Auditor/private work preserved.
No unused task-owned player, editor, proxy or recorder remains. No browser opened.

## Remaining scope

Text rendering is still a separate active issue. Painted white borders embedded in
supplied art are not automatically erased. This is an Editor/native visual and
import result, not a newly packaged Windows release or owner visual approval.
The reported online host shutdown remains open; this unit makes no network claim.

## Filled glyph edges

Filled icon polygons and dots now receive the same one-physical-pixel transparent
edge as line glyphs. Polygon winding determines outward normals and corner miters
are bounded. Circle segments scale with rendered radius, between14 and96, rather
than keeping a14-sided silhouette at every size. The colors, positions, radius,
control bounds, materials and authored artwork remain; no new texture/material
or glyph renderer is added. This is subtle edge sampling, not an icon redesign.

Based on04ffa1bb8 plus this glyph change, native1072 passes the actualHome viewport
check at1080p,1440p and4K. The actual-size4K store-icon detail was inspected. The
unchanged text/browser/caret checks are reused rather than repeated. Additional
edge geometry is bounded; no GPU timing gain is claimed. Parent exits0,19310
frozen inputs verified unchanged after202 incidental metadata deltas are retained
and restored exactly; profile/input/editor preferences/Quality restored.
No owned game/editor/proxy/recorder or browser remains. The latest Windows893
package predates the final field-layer and filled-glyph source changes.
