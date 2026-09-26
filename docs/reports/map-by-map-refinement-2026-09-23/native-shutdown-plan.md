# Native shutdown diagnosis, 2026-09-24

The historical accessibility-v57 player passed its15stage review and then exited
with0xC0000005. Recorded notes identify D3D12Core1.618.1.0, offset0x264831; this
localizes the fault but does not prove its root cause. The original binary exists
in the older owned validation worktree at Builds/accessibility-v57. Do not build
another intermediate player or test the Desktop copy.

Copy that exact internal build into the current qualification worktree's Builds
for a bounded comparison, recording engine/runtime hashes and original source.
Use the existing profile-preserving native review driver with one optional Windows
graphics-API argument. Run the original accessibility route once with D3D12 and
once with D3D11, same binary, separate fresh profiles/outputs, one process at a time.
Confirm actual backend from the player log, not just the requested flag.

Compare the route verdict, clean exit, final log ordering and any new crash evidence.
If the fault recurs, inspect the stack/module details before choosing a project
compatibility change. If it does not recur, do not call it fixed or broaden into
an indefinite soak. Keep current-source/current-binary final qualification open.

Unity's public issue searches show several distinct D3D12 crash families, including
swapchain and driver cases. None is an exact match established for this failure;
do not cite a similar module name as proof or upgrade engine/drivers speculatively.
No paid services, account changes, external bug report, Desktop replacement or
unrelated process cleanup. Preserve all profiles/input and the original v57 build.

Comparison completed on AMD Radeon RX6600, driver32.0.21043.19003. Actual D3D12
log confirms level12.2:15stage pass followed by0xC0000005. New Windows event1000
names D3D12Core1.618.1.0 at0xa1f5 (different offset from the older note). Actual
D3D11 level11.1 ran the same15stages and exited0. Input preferences unchanged.
This supports a backend compatibility mitigation on this machine, not an exact
internal engine/driver root cause or a claim about every GPU.

Set Windows standalone explicit API order to D3D11 then D3D12 through the Unity
PlayerSettings API. The [documented order](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityeditor/playersettings/setgraphicsapis)
tries the first available backend when automatic selection is disabled. Retain
other platforms, shader quality and opt-in D3D12 diagnosis. Launch the author
itself with-force-d3d11 so no active-editor API switch is needed. Inspect the
serialized diff, then one existing current-source five-map bright-look case on
D3D11 for shader compatibility. Final current player default-backend/exit/perf
still belongs to P7; do not create an intermediate build or repeat the old pair.
