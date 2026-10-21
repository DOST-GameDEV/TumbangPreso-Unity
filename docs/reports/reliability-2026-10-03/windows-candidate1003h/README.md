# Protocol139 development player: accepted build and startup

Frozen source9d21820c32503fd2af6bfc23829fd794243c97bb, protocol139, Windows64,
Development plus ConnectWithProfiler. Current Main140 and later loading changes
are excluded. This is a diagnostic candidate, not the final release acceptance.

The first graphics build crossed the1.5GiB physical reserve and was interrupted;
only its owned Editors stopped and restoration completed. Its partial output
and receipt remain. One distinct lower-memory attempt used headless build mode,
one job worker and one asset-GC helper with the same reserve. It built12scenes,
2506MB in98s, normal exit/restoration/free lease. No quality or source reduction.
The old1003g idle Library was moved into this checkout, avoiding another9GB cache
copy. Its source and accepted packaged134 artifact remain intact.

Strict retry receipt stays **false**: six deltas are retained. The two stamped
identity files changed, and the performance-test package's postprocess callback
removed its four temporary JSON/meta files. Classification verifies that exact
cleanup against package code, unchanged C#/Packages, matching generated/packaged
identities and the original213 import/generated deltas. Separate artifact
classification passes; no strict failure was erased.

Full manifest:400files,2,628,366,970bytes, SHA256
90d1e910b71591e3c51173e2b087c49aee56ecaac5ed48750543d120a1284273.
Runtime520ec9902719ee7381ac70d2111bcb3dbb88a2cad19d73d7a42f56f83b0d2ac8;
Corea50fcc83147f6140682a73115f2043be4f8d23a531ff9861dd194364d12e6fca;
launcher8c08116b215c1375d3c156f92957773458c4575f92ad560cfdc815c3d9ca0953.
Absolute artifactRoot is retained in the manifest.

Normal-player startup/menu route passed on a fresh profile: cold boot/intro,
2722silent loading/login frames, Terms check, supplied login layouts, revealed-home
music, title motion/reduced motion and home/settings/credits/mode/back routes.
Normal exit, restored inputs and unchanged runtime hash. Native Hub capture
visually inspected; SDK submission and physical input are separate.

The broad performance capture was stopped at its incorrectly inherited450s
outer wrapper limit.127completed windows are retained; no complete-route pass.
Conditions: Development/profiler overhead, quality2, windowed1280x720, staged
actors/actions and post-preparation windows. Native profiler read identifies
multi-second Nemu spikes in profiler collection; first map entry includes a
2.57s first TextMesh/font cache cost. These are diagnostic findings, not release
FPS or a proven Nemu skill defect. Nameplate loading fix is a later source unit.

Raw traces total14,434,505,500bytes. Automatic review blocked the requested
unused-trace pruning with only "blocked by policy". Nothing deleted or bypassed;
retain selected hotspot files and all CSV/context/actions/results until cleanup
is authorized by the execution policy. No share server, Desktop replacement or
matching-peer acceptance is claimed here.
