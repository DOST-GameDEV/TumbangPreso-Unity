# Spectator replay format compatibility

The protocol100 Linux player completed its match and rematch entry, but its
spectator pixel ring lost447readbacks in the first match and319in the rematch.
This ring is separate from the recorded-world halftime replay.

## Cause and change

The camera checked general async capability and RGB565 render support, without
checking whether the backend can read that format. Native reproduction reports
async=True, render565=True, read565=False; all three requested colour frames are
lost. Unity documents the separate ReadPixels format capability check:
[AsyncGPUReadback.Request](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/rendering/asyncgpureadback/request).

The added format gate then exposed a second real defect: the old synchronous
fallback also tried ReadPixels directly into unsupported RGB565. It now reads
into one reused RGBA32 staging image and packs directly into each retained
RGB565 NativeArray. No managed colour array is created. The100frame/two-byte
ring remains unchanged; staging is destroyed with its camera. Supported devices
retain the existing asynchronous path, queue cap and lifetime rules.

## Evidence

Unity6000.5.8f1 Linux OpenGL, isolated named profiles, real native pixel capture:

- Original baseline1/1fails: three captures become zero ready frames.
- Format-gate-only1/1fails: unsupported synchronous destination format7 assertion.
- Complete correction1/1passes in0.190247seconds. Three red/green/blue frames
  become ready, each640x360 RGB565 with460800bytes and correct sampled colour.
  The same staging image is reused between captures; failed readbacks remain0.
- All runs preserve their fresh XML/logs/receipts. No memory guard or new OOM.

[Raw evidence](replay-format-checks/). This is a capture-correctness fix, not a
measured performance improvement. Synchronous fallback can stall; target hardware
performance and a refreshed full player are not qualified by this focused check.
No hero presentation, scene, asset, scoring or protocol change.
