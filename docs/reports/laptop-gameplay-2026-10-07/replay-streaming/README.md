# Continuous replay loading and seek lifetime

Original native29 loses1.502081wallseconds at1x and1.531502at4x across three
actual recorded Arena segment seams. It requests/decompresses only at the seam
and blocks playback while loading. A real damaged-segment seek immediately
superseded by a valid seek also poisons that final position. Close during a real
read already passes. Original XML remains1PASS/3FAIL.

The viewer now preloads one next segment while continuing to render the current
segment. It retains the current view, one prepared segment and one pending read,
not the whole match. A prepared segment is installed on the main thread at the
actual timeline edge, through the existing continuous audio/camera path. Latest
seek identity decides whether a completion/error is relevant; obsolete data is
discarded. A future read error is held until that segment is actually requested.
Closing releases the owned references and observes any orphaned task fault
without calling Unity from its disk thread.

Identical candidate30 passes all four actual1x/4x continuous-play, stale corrupt
seek/final rendered clip and close tests. The100ms cumulative missed-time bound
is unchanged. Measurements and native/source hashes describe exact scope.
The fixture is the real completed30second Custom Arena replay from native24,
with ten effects segments and15149unique captured render times; no fake reader
or estimate stands in for disk/decode/render work.

Both jobs run on gamergmae, Unity6000.5.8f1/D3D11, named qa-a identity. All21383
input bytes,13 original Editor preferences and four original profile files
restore after terminal execution. Exact native30 sources preserve mixed line
endings; explicit CRLF-only normalization must match the published Git blobs.

This resolves tested loading-induced timeline stalls, not all GPU/GC/frame-pacing
or long-session performance. A current packaged repeat, physical controls,
round-transition audio/performance, rapid close/reopen under repeated pending
reads and wider scene/effect fidelity remain separate gates. No startup, Net,
PC-owned history/camera/AI source or finalized hero art changed.
