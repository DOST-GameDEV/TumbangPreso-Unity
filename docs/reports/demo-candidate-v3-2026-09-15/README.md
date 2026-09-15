# Native v3 candidate and focused gameplay checks

The internal v3 Windows player includes Nemu's block palms, the regenerated poses
that use its palm centre, and the corrected singular result caption. Two fresh
native-profile runs passed startup/Guest, settings/motion, both short custom
matches, actual results and Classic rematch/leave. All captures are1366x768 windowed.
Normal8-round defaults were checked before selecting the custom rehearsal lengths.

Artifact: Builds/demo-2026-09-16-v3/TumbangPreso.exe and its adjacent data folder.
Runtime.dll timestamp2026-09-15T05:21:21.9526779Z; SHA256:
E66713F7FCEE4BC4E5AB0948F9272198D7824ECED18BC9A082FBEFC7CDFD3F72.
Build1081MB/54s; profile restoration21d801b3fdba. Earlier v2 is retained as a
separate tested fallback. No Desktop build was replaced.

## Measured frame behavior

The opt-in driver samples active gameplay while waiting for the result, outside
its screenshot operations. On this RX6600/Ryzen52600 PC, the quiet repeat measured
173.9FPS average in Classic and179.1FPS in Hero Strike over roughly28seconds each.
Both95th-percentile frame times were about6.7ms;99th percentiles were about10ms.

There are isolated hitches. The first Classic run recorded363.3ms; the repeat's
largest Classic frame was36.7ms, while Hero Strike had one90.0ms frame. The larger
Classic outlier did not reproduce, but its cause is unresolved. Do not call the
game stutter-free or generalize these windows to all PCs,1080p/fullscreen, every
ability overlap or the venue. The raw measurements remain on the playtest watchlist.

## Nemu recovery and swimming

The geometry-based authors intentionally rebaked arm curves from the new palm
centre. The embedded GLB animation samples remain unchanged; these separate
recovery/swimming assets are calibrated outputs and are retained. The focused
case passed all four combinations of forward/tread and empty/held swimming, with
the eye above water. Supported recovery returned to standing; measured model
support gaps ranged from-0.0283m to0.0224m, within the existing bounds. Actual body
and owner captures were inspected. Restoration17958fc6d0e4.

## Additional focused loop checks

Two cases passed with restoration17f170b3da07. The existing accelerated synthetic
match ran all8rounds and gave each seat exactly two defender turns. Separately,
both modes walked into the Sa Bubong pool, retrieved floating stock through real
grab input, walked back out and entered again with a jump. The accelerated case
is logic/rotation evidence; the water route uses the existing live motor/Carrier.

This is a working candidate and a tested fallback point, not completion of the
entire backlog or human approval. Remaining real-device feel, LAN rematch/rejoin,
rare-hitch diagnosis, deferred Inday framing and broader counterplay/variants stay
active in TODO.md and DEMO_PLAYTEST_CHECKLIST.md.
