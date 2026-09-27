# Loading And Performance: Where To Work

Target: expensive loading and reusable initialization complete behind responsive,
work-driven loading,not on the next click/cast. Respect memory and authored assets.
Read the current queue,[loading evidence](reports/stability-2026-09-27/loading-audit.md),
[working rules](WORKING_RULES.md) and current source before changing a stage.

## Current Entry Points

Runtime files are under `Assets/TumbangPreso/Runtime/`.

| Responsibility | Source |
|---|---|
| Boot stages and progress | UI/SplashScreen.cs; shader slices,rosters,audio,menu art,input,glyphs and retained dependencies |
| Deferred SFX/voice samples | UI/SplashScreen.WarmAudioAssets; yielded sample loading and retention,not just clip references; music/streaming policy unchanged |
| Real menu activation barrier | UI/SplashScreen.MenuActivation.cs and ConvertedMainMenu.IsPrepared; retain existing canvas through Wire/layout,then reveal login/input |
| Title/login art and avatars | UI/OwnerMenuArt.cs,Avatars.cs; yielded preparation and retained resources |
| Hub/HUD portraits and mode cards | UI/OwnerPortraitArt.cs; async roster-driven warmup and shared cache used by HubKit/TumpUiFactory |
| First HOME loop | UI/Hub/HubSceneVideo.Warmup.cs; async metadata/selected poster,one paused decoded frame behind boot,then adoption of the same player/target |
| Ability prop source prefabs | Visual/HeroPropAssets.cs; current Paete/Rework/Phaister folders,no gameplay spawn during asset preload |
| Supplementary baked motion | Visual/GeneratedMotionAssets.cs; yielded per-rig data preload shared by CharacterAnimator and rooted introduction lookups,no clip/graph generation |
| Roster outline geometry | Visual/OutlineNormals.Warmup and boot roster loop; per-mesh welds survive scene notifications while the exact mesh lives,without retaining dead runtime meshes |
| Effect sheets and authored intro data | Visual/VfxFlipbook.cs,UltimatePerformance and existing per-kit warmups |
| Shared particles and status icons | Visual/AbilityVfx.WarmupAssets prepares existing cached geometry; UI/StatusIcons.Warmup follows StatusRules.All with async sprite loading |
| Match loading surface | UI/Hub/HubLoading.cs and MatchInstaller.IsPrepared; destination-scoped installation, failure/return and cancellation instead of timed success |
| Menu scene entry | SceneFlow.Go -> HubLoading.BeginMenu; asynchronous known converted scenes,ConvertedScreen initialization/error,canvas layout and same-curtain hub preparation; root pointer blocker |
| Custom map switching | HubLoading.PreparePreview and MapPreviewSurface.PrepareAll; real hub curtain covers all scene/setup/first-draw work,then cached scene/look instances serve selection without new loads |
| Training controls/targets | PracticeRange.cs,UI/PausePanel.TrainingRange.cs; prebuilt inactive menu and target bodies reused without changing saved preferences |
| First-use/runtime costs | Existing profiler markers,FrameRateHistogram,tools/cold_start.py and current internal player |
| Settings value changes | UI/TumpSettingsView partials; whole-row reflow only for new sections/text size,cached chips and one unsaved-state calculation per notification |
| Character preview targets | UI/ModelPreview.cs; coalesce continuous pixel-size reallocations,keep current panel projection,settle exact sizing and capture immediately |
| Preview cache ownership | Visual/ToonSkin keys base/overlay variants separately; MapPreviewSurface destroys resized-out targets and rebinds camera/UI together |

## Rules For Changes

- Owner permits loading screens wherever substantial initialization needs one.
  Reuse the existing surface,cover the actual work,keep feedback responsive and
  provide failure/exit behavior. Do not add timed waits to cheap interactions.
- Progress follows completed work. A timeout or decorative percentage is not proof
  of readiness. Scene load90% is not menu Awake/Start/layout completion.
- Yield between slices; avoid one global Shader.WarmupAllShaders or unbounded
  synchronous catalog sweep. Retain the objects consumers actually reuse.
- Loading a prefab is not instance/material/GPU preparation. Parsing a clip table
  is not rendering the first animation frame. Name the precise work moved.
- Warmup must not cast abilities,create hazards,spend resources,award progress,
  enter live services or mutate the player's profile. Preserve local fallback.
- Cache against real lifetime and invalidation. Do not make art smaller or replace
  supplied materials merely to report a faster launch.
- Measure representative player first-use/frame timings when the environment permits.
  Record source/build,hardware,path and memory tradeoffs; no hitch-free claims from
  compilation or a warm cache. Test changed stages,not the same whole boot repeatedly.

## Current Qualification Boundary

The latest [focused native integration](reports/stability-2026-09-27/input-integration.md)
passes loading-readiness and supplementary-data retention cases alongside five
state/presentation cases. The subsequent [UI/audio pass](reports/stability-2026-09-27/loading-audit.md#ui-flow-native-qualification)
passes three first-run range/settings/preview reuse cases; a separate new audio case
confirms deferred sample preparation and no playback. Menu activation,portrait/prop
retention,HOME decoder,physical input and visual acceptance still have separate gaps.
Earlier shader/art evidence remains separate. There is no current complete player
before/after hitch table. Consult the ledger for current headroom/processes; the
latest native pass succeeded, but player packaging headroom is not established.
Do not repeat unchanged successful cases or relaunch a blocked workload in a loop.
