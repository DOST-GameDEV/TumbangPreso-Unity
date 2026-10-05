# Menu text sampling and persistent texture quality

The owner requests sharp UI that stays sharp after reimport, graphics settings
and future player builds. Based on2c9f790bf with the scoped changes in this commit.

## Text change

OwnerUiLayout and HubKit use CrispUiText, preserving the existing legacy Text API,
font roles, declared sizes, preferred layout and controls. Small dynamic glyphs
are generated at up to twice their rendered pixel size then returned to the same
logical coordinates. Sampling is bounded to160 pixels for large headings.
Static fonts and best-fit labels keep their existing renderer. The font texture
rebuild callback is restored in finally, including empty text. This improves
sampling; it does not erase the chosen brush fonts' authored rough silhouettes.

Native six checks pass for display/reading fonts at canvas scales0.75,1 and1.5.
The real J glyph height improves24->45,31->59 and45->86 pixels for display and
25->46,32->61 and46->89 for reading. Preferred dimensions and vertex count are
unchanged. Ink hinting remains within the explicit four-unit bound, with the
same line placement. No per-label RenderTexture or image replacement is created.
This adds font atlas detail/cost, bounded for large headings; no font-memory gain claimed.

ActualHome native capture passes at1080p,1440p and4K. Full1080p and4K detail inspected;
the before/after crop compares the same controls, not the animated background.
Remaining embedded painted borders and broader screen/hardware taste acceptance
are still separate. The supplied original PNGs remain byte-identical.

## Future import/build guards

Scoped owner and brand importers opt UI textures out of global texture mip limits.
Stale Standalone,Android,iPhone,WebGL andWindows Store Apps target overrides are
cleared so they cannot silently replace the source-resolution/uncompressed defaults.
These rules apply only to the explicit owned artwork paths, not world textures.
The importer version is increased to ensure existing artwork receives the policy.

Native effective-import and stale-preset checks pass5/5. The latter deliberately
sets256-pixel DXT5 Standalone overrides on a cloud layer and a logo, then performs
real SaveAndReimport. The actual textures return to full source dimensions and
uncompressed RGB24/RGBA32; the stale override is gone. This is native import proof,
not a rebuilt Windows package or every graphics API/target-hardware certification.

All parents exit0 with isolated profiles/shared preferences/input/Quality restored.
202 incidental generated metadata files were restored to frozen bytes. Two fixture
platform-list reorderings were removed while retaining the intentional mip policy.
The first text fixture failed an ambiguous reflection overload; it was repaired
with the VertexHelper signature and that failure remains private. No runtime
failure was hidden. No unused task-owned editor/game/browser remains.
The current QA online host shutdown remains OPEN.
