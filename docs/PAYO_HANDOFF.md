# PAYO_HANDOFF: historical reference

Retired from the active reading path on 2026-10-25. Payo is enabled in Build Settings; this earlier handoff does not govern current execution.

Current authority: [current reference](REFERENCE_INDEX.md).
The [complete original](archive/cleanup-2026-10-25/docs/PAYO_HANDOFF.md) preserves decisions, source receipts and old IDs.
Its unchecked boxes and earlier work orders do not assign new work.
## OPEN AT THE FIFTH COMPACT (2026-10-31): performance pass

**Update, 2026-10-31.** Done: High at two cascades, the floor stripes (normal bias 0.6), plant outlines through walls, tree leaf LOD; rules in docs/CANONICAL_RENDERING_PIPELINE.md. Earlier numbers called 1080p were 1440p; the benchmark now states its size and skips the arrival film. Found, not fixed: the replay's 10 Hz capture is the main cause of the uneven frames (`SpectatorCamera.ReplaySampleInterval`), the outline's rescan allocates once a second, Arena throws `Scenery texture identity was not prepared: arena_stage_props` from the replay's scenery sampler about 250 times a second, and the Eskinita hitch sits at the end of the arrival film. World AO was not baked on the other maps: it is worth at most about 0.2 to 1.1 ms a frame. Probe switches are listed in `FrameBudgetProbe.cs`.

Branch is `QoLUpdates-v2` (`QoLUpdates` was merged into `mobile+network` and deleted). Pushed through 76b20eab3; a859214a3 (PerfBuildRequest lock fix) is local only. Push only on the owner's word in this chat. The VFX coordinator session ("Eight heroes ultimate cutscenes rework") shares this worktree: commit by pathspec only (`git commit -F msg -- paths`), never add -A, stash or reset; its uncommitted files stay. Editor lock: `( set -o noclobber; echo "kanto <what> $(date +%T)" > Temp/cutscene-film.lock )` before Play, build, measure, scene saves or .cs/.shader writes; delete only your own, right after each step. GitHub refuses files over 100 MB and there is no LFS.

**Goal (owner):** 300+ fps on the High tier on an RTX 5070 at all times, and playable on integrated graphics.

**Benchmarks:**
- `Builds/PerfRTX/TumbangPreso.exe -tp-framebudget <dir> [-tp-maps A,B] [-tp-seconds N] -screen-width 1920 -screen-height 1080 -screen-fullscreen 0 -logFile <log>` writes frame-budget.csv. Build by writing an absolute exe path into `Temp/perfbuild.request` (answer `Temp/perfbuild.done`), under a lock line starting `kanto perfbuild`.
- Integrated Radeon: `Builds/PerfIGPU/` and `Builds/PerfIGPU2/` exes are set to the Radeon through HKCU\Software\Microsoft\DirectX\UserGpuPreferences (remove when done). `-tp-graphicsreport <dir>`, `-tp-look-parts`, `-tp-maps`.
- Editor: `measure LABEL` in Temp/payo.request (label with "quick" for A/B, `bakedao` for live vs baked shots). Editor numbers inflate CPU; trust builds.

**Latest RTX, High, 1080p windowed (Builds/PerfRTX/budget-final), mean fps / p99 ms / worst ms:** Eskinita 319 / 7.1 / 275; Arena 315 / 5.8 / 19; Kanto 232 / 10.1 / 14; Ilalim 238 / 9.4 / 23; Lagoon 208 / 10.7 / 14; Payo 199 / 10.7 / 19. GPU is the limit on every map (Payo 4.96 of 5.02 ms); main thread is 1.4 to 3.6 ms.
**Radeon (2 CU), before the AO fix, Balanced, ms:** Payo 152, Kanto 115, Arena 68; Low after: Payo 50, Kanto 42, Arena 21. Benchmark draws a fixed 1080p, so Low's 70% render scale is not in those numbers.

**Done:** Low tier with first-run auto-detect (GraphicsProfiles.cs; own PC's profile already recorded Balanced, pick Low by hand); Balanced AO 4 probes; Payo low detail (PayoDetail.cs); plaza merge; far mesh LODs (PayoSceneBuilder.MeshLevels.cs); shadow trim past 40 m on every tier (PayoDetail.TrimFarShadows); Payo baked AO (PayoSceneBuilder.BakedAO.cs, UV2 streams with a z mark, split copies in PayoBakedAOSplit.asset, bulul/plinth/small props stay live through mask channel G).

**Next, approved by the owner, in order:**
1. Bake world AO on the other maps the way Payo does (Eskinita, IlalimNgTulay, LagoonCove, Kanto, Arena). Their shaders and builders differ, so check each. Same-eye live vs baked shots, including statue-like detail and open floor; the owner rejected the first Payo bake for darker shadows and lost face creases, so compare closely and show crops.
2. Find the uneven frames (p99 10 ms) and the 275 ms Eskinita hitch with a development build and the profiler (GameBuilder takes `-developmentBuild` only on the command line; the request path cannot pass it yet).
3. Cascades: the owner wants to COMPARE High at 4 vs 2 cascades before deciding. Same-eye shots plus fps; do not change it without the verdict.

**Also open:** 43 MB of LandLod glbs embed textures they do not need (strip before the repo grows); Payo's night view and Low-tier look with the bake are unchecked; far-mesh switching not watched in play; Payo Blades assets churn on every rebuild and stay uncommitted on purpose; the AO flash through platforms when on Paete was not re-tested since the bake (`Tumbang Preso/Debug` has Character AO Off and First Person Hand Shade Off, the recorder is `Temp/rec.request`); 226 plaza LODGroup plants are the next plaza cost (needs the owner's call: they sway on the CPU); break/halftime camera director still off (`PayoBreakDirector.Enabled`, last clip check not clean); Mayari goddess model pending with the VFX session.

