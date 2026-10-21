# Current Windows loading stall: graphics threading comparison

The existing ordinary Windows `001d80b568` package still has conspicuous loading
stalls. Changing graphics threading is not a sufficient remedy and is not adopted.
No production source, project settings, Desktop files or build folders changed.

All three sequential native graphics players use runtime SHA256 `2005a5d0ac9bb5c4573e77a46eb0bc115f700f49e22d05afa06891702c242a20`,
Direct3D11, 1920x1080, Balanced game quality, Ultra Unity quality, vSync=1 and
targetFrameRate=-1. The actual device logs confirm ClientWorkerJobs for the two
normal runs and Direct for the experimental middle run. Each completes all 14
menu windows with no binary profiler and restores input, editor preferences and
the isolated profile after its terminal player exits. Hidden native graphics and
synthetic UI raycasts are the execution scope; this is not a human visual check.

- Normal baseline, PID66940: loading maximum1099.490ms, seven frames over100ms;
  first Hero39.683ms with one frame over33ms. Home idle maximum16.824ms.
- Direct experiment, PID28080: loading maximum818.523ms, eight frames over100ms;
  first Hero44.624ms with three frames over33ms. Home idle maximum23.604ms.
- Normal repeat, PID53308: loading maximum832.293ms, five frames over100ms;
  first Hero73.407ms with four frames over33ms. Home idle maximum17.045ms.

The ABA order checks whether the apparently lower experimental maximum can be
explained by warmed resources or run variation. The subsequent normal maximum
is similar. Neither threaded mode solves the loading defect. The ordinary first
Hero35.362ms from the earlier release route is a scoped observation; these fresh
39.683/73.407ms normal runs establish that first-use consistency remains open.

Hypotheses and next decision:

- Graphics handoff alone causes the loading stall: weakened. Direct mode still
  has818.523ms stalls and worse loading p99 than either normal run. Keep the normal
  graphics mode; this comparison does not identify the underlying driver/GPU cost.
- Asset texture upload creates long render work: supported by separate older
  Development evidence, not confirmed as this ordinary package's largest stall.
  Its Render Thread includes216.757/215.168/218.220ms individual
  `Gfx.UploadTextureData` samples and328.137ms across six such calls in frame18.
  Nested parent markers are not additive and main/render frame indices are not
  assumed synchronized. The909.092ms main frame cannot be assigned to a particular
  texture from this export. Next isolate actual preload resources and upload paths.
- Remaining first-use CPU work and environment variance affect Hero presentation:
  supported by the repeat variation; current ordinary CSVs do not contain a CPU/GPU
  breakdown. Do not call the wall-clock maxima GPU timings or attribute them to GC.

Exact runner commands, logs, settings, all frame/context CSVs and copied-file SHA256
values are retained in [evidence.json](evidence.json) and the three run folders.
The older trace extracts retain their distinct source/mode scope. No additional
build, release update, graphics preference mutation or background helper remains.
The tournament goal and all broader AI, camera, new-map creative and peer gates
remain open.
