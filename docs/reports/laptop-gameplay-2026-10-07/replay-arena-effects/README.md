# Local Arena effect timeline

Local Arena replays now retain every actual effect render-frame state, including
one-frame immediate shapes, through optional independently hashed .fx.gz sidecars.
The compact versioned binary stream saves exact quad corners, orientation basis,
shape cell and quantized original vertex colour. Playback uses the actual Arena
material/atlas in an owned mesh; fixed quads remain fixed, camera-facing shapes
reorient for the replay camera. It never emits effects, steps the live pool,
consumes effect random numbers or triggers gameplay. Owned buffers reuse capacity.
Legacy manifests without effect data continue to load.

## Concrete defects fixed

Natural recording exposed active-only slipper discovery removing recovering,
inactive props from Archive history. Inactive identity is now retained together
with sampled ancestor visibility; true unsupported changes remain refused.
Playback then exposed the centre-floor probe returning negative infinity on a
miss and passing it to court visual construction. Preparation now accepts finite
actual support queries and preserves a finite fallback. History, catch, TagBody,
network and live gameplay/hero designs are unchanged.

## Native evidence

Native12 actual Arena pool snapshot matches mesh corners/material/cell without
stepping. Native13 actual material/live-pool versus reconstructed mesh RGB0 at
matching camera, visible control required; alternate billboard facing/size pass.
Native15 compressed short flash/empty transitions within the20Hz pose interval
survive exactly, truncated streams reject. Native18 naturally completed30-second
Custom Arena capture retains13274 render-frame states/2905900 quads/10segments,
about91MB compressed, Warningempty; actual viewer opens/seeks17/2/disposes.
Native21 full1664-slot pool across180frames round-trips exact corners/RGBA,
3,085,394 encodedbytes, encode1184.8651ms/decode292.6263ms on this laptop. This is
one measured codec sample, not long-session or real-time performance approval.

Native22 all7 existing preview receipts pass without rewriting footage/posters.
Canonical source removes only verified replay bookkeeping; exact original live
ArenaFx calculations match byte-for-byte. A real atlas-edge change invalidates
footage; an unqualified adapter change also invalidates provenance. The source
rule conservatively accepts only the verified adapter hash.

All final21375 frozen inputs,13 original Editor preferences and four existing
profile files restore after terminal Unity. checked-source.json identifies the
exact packet/source; publication proof compares normalized Git text. Original
failures remain: native14 CompressionLevel ambiguity compile, native16 partial
world-change recording, native17 invalid Arena floor bounds plus fixture leak,
and native21 stale preview receipt. Neither zero tests nor those failures are
acceptance. Independently shipped331 corrects per-level shader clock identity.

## Limits

Comparisons are focused512px render/geometry tests, not all full-resolution Arena
frames. Natural trace has an uncapped editor frame rate; file size is not a final
storage-rate promise. FullPool sample covers60Hz maximum density; higher density
and rates, prolonged capture, disk interruption, GPU/package/device/peer behavior
remain separate. Drones, wildlife, other transients/material states and complete
visual parity remain open. No Desktop replacement or full tournament-ready claim.
