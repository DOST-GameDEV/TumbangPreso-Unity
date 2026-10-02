# Studio intro before loading

The existing BH Studios animation now runs on its own lightweight surface before
building the loading UI or beginning heavy preload. Any fresh keyboard, mouse,
controller button or touch press fades the intro to white in0.22seconds. Natural
completion uses the same white join. The existing loading screen then covers
actual asset/account work and retains its prepared-menu/login readiness barrier.
Skipping presentation cannot skip loading.

The old skip label is empty/inactive in the serialized scene, hidden at runtime,
and removed by its importer binding too. Logo aspect is preserved with a fitted
picture and white margins. Missing video falls through; preparation or playback
failure has a bounded fallback. Owned video target/canvas resources are released
on handoff and destruction. Supplied loading art and source sound are unchanged.

## Native evidence

Five distinct cases pass in separate processes after a separate import, all with
guard null. [Results](native-results.json) and raw XML/receipts are retained.
- Real92frame intro playback before any loading canvas/menu preload, natural
  completion, native logo/white captures, and handoff beneath opaque white.
- Fresh keyboard Escape, mouse right-click, controller East and touch presses
  exercise the normal skip query and coroutine, including intermediate fade.
- Missing media, an injected decoder-failure callback, and destruction cleanup
  settle without claiming readiness. This is not an induced physical decoder fault.
- Existing real menu activation retains its curtain and defers input/login.
- Existing failed-menu route keeps exactly one focusable, pointer-accessible exit.

The initial native check found the original H264 MP4 cannot be loaded as a
VideoClip by this Linux editor. The original MP4 remains byte-identical and
bound. A VP8 WebM derivative is explicitly bound for Linux and missing-original
fallback; Windows keeps the original. Both preserve1280x720,30fps and92frames.
Decoded SSIM is0.999168; the actual native picture was inspected, not accepted
from that number alone. This compatibility route follows
[Unity's video compatibility guidance](https://docs.unity.com/en-us/engine/6000.0/manual/video/sources/file-compatibility).

The first synthetic-input case was filtered by editor focus. Its single fixture
repair reuses the existing suite's IgnoreFocus/AllDeviceInputAlwaysGoesToGameView
settings and enables its test devices, restoring settings afterward. No production
input behavior or assertion was relaxed. Failed results are retained.

This is focused native editor evidence, not a fresh packaged Windows/Linux boot,
physical-device acceptance, full regression or sound-quality approval. The fresh
packaged startup check remains the next qualification step.
