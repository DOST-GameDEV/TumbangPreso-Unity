# Buffered replay effects compression

The existing writer sent approximately 24 scalar writes per effect quad to
GZipStream. A full three-second60fps pool control has180frames and1664slots,
with299520quads and millions of tiny compressor calls. A64KiB BufferedStream
now collects those writes into blocks. No frame, geometry, colour, atlas cell,
camera basis, timeline, file version or queue bound changes.

On gamergmae, Unity6000.5.8f1/D3D11, identical native fixtures measure
1255.7599ms original encoding and875.2107ms buffered
encoding (1.43times faster in these runs). The decompressed payload SHA
matches exactly: `b0e083f696f670dcfd7e1c57bdb1fcb40ff25547c8ddcaed941e0604310f6c5d`. Full RecordedQuad equality checks
all geometric/facing/atlas/colour fields, not just a position and colour.
Single-frame flashes and damaged-data rejection also pass in both runs.

Both native27/28 runs pass2checks and restore all21381 source inputs,13 original
Editor preferences and four original profile files after terminal execution.
Retained XML, raw receipts and native/source hashes identify exact scope.
These are controlled codec measurements, not a300Hz gameplay, long-session,
capture backpressure or packaged performance claim. No stronger visual parity
claim follows from compression speed. The completed-match endpoint fix is in
the separate replay-closing-boundary report.
