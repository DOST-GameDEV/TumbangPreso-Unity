# Loading And Performance: Where To Work

Target: expensive loading and reusable initialization complete behind responsive,
work-driven loading,not on the next click/cast. Respect memory and authored assets.
Read the current queue,[loading evidence](reports/stability-2026-09-27/loading-audit.md),
[working rules](WORKING_RULES.md) and current source before changing a stage.

## Studio intro before boot work

Unity branding and the existing BH Studios clip precede login. The retired
illustrated loading screen is absent from this startup route. After login, the
existing main-menu loading view stays for at least five seconds and until its
required preparation completes, then enters Home. Fresh input skips only the
studio clip. Linux uses an explicitly bound VP8 derivative because H264 VideoClip
import is unsupported there; the original MP4 remains. Missing/failed media has
a bounded fallback. [Startup evidence](reports/startup-order-2026-10-07/README.md).

## Current Entry Points

The current login/main-menu route now prepares the shared asynchronous roster
catalogue and existing yielded gameplay icon, prop and effect-data caches before
map and Home media. This restores preparation skipped when the retired splash
route was removed. No additional models or preview cameras are created. The six existing mode-selection
posters also preload through their shared asynchronous cache before Home media. Native
and actual release timing evidence stays distinct in the
[active-route preparation report](reports/active-menu-preload-2026-10-07/README.md).

All seven maps have recorded map-selection backgrounds and matching posters.
Voting previews the highlighted court in the full background; browsing is local,
LOCK VOTE submits explicitly. [Native flow and media evidence](reports/map-vote-background-2026-10-07/README.md).
Native checks cover the normal12-second ballot deadline, interrupted decoder
recovery and original captured-source receipts against the current integration.
The [recording method](MAP_PREVIEW_RECORDING.md) regenerates true1080p footage
without retaining the previous capture's live scene. [Exact evidence and limits](reports/map-preview-lifetime-2026-10-07/README.md).
Packaged performance, loop seams, portable codecs and actual peers remain open.
Fresh all-map recordings now wait for the actual world-look ground update before
frame0, correcting Eskinita's dark poster/loop flash while retaining capture
quality. [Native source/decode/loop checks and remaining traffic seam](reports/settled-map-previews-2026-10-07/README.md).

Arena map selection uses recorded output of the existing preview camera; the live
lobby remains a real scene. Login warms map poster/clip metadata and only the
shown view decodes. [Native playback and map-vote evidence](reports/recorded-map-preview-2026-10-07/README.md).
All seven maps now have checked recordings and a reusable regeneration path;
player/device acceptance remains separate.

Login-time gameplay assets: [native responsiveness and interruption checks](reports/laptop-validation-2026-10-06/login-gameplay-preload/README.md).

Bot rival-spacing queries reuse per-brain scratch storage. A calibrated native
100-query measurement falls from200allocation events to0; four cases preserve
claim expiry/refresh and reader independence. No loading or whole-frame claim.
[Evidence](reports/feedback-2026-09-30/bot-spacing-allocation.md).

Bot retrieval landing prediction shares world-flight math with the existing
ground circle and refreshes a per-brain cache at existing Think cadence. Eight
native cases pass;100cached reads allocate0events. Full forecasting and whole-frame
cost are separate; this does not alter loading.
[Evidence](reports/feedback-2026-09-30/bot-landing-terrain.md).

Runtime files are under `Assets/TumbangPreso/Runtime/`.

| Responsibility | Source |
|---|---|
| Boot stages and progress | UI/SplashScreen.cs keeps shader slices,rosters,audio,menu art,input and retained map dependencies. ConvertedMainMenu overlaps gameplay icon/VFX/prop/hero-data and Home-video preparation with usable login before automatic Home arrival |
| Roster catalogue handoff | RosterBook.Warmup shares one async catalogue request on the current ConvertedMainMenu preparation route; the retained legacy SplashScreen consumer uses the same cache. Load adopts a completed cancelled request and retains direct synchronous fallback plus the once-only missing warning; no roster IDs/order or serialized art changes |
| Shader preparation turns | SplashScreen calls WarmUpProgressively(1),checks a2ms elapsed target between calls and caps10variants/turn; one indivisible native compile may overrun,so this is not a hard frame guarantee |
| Deferred SFX/voice samples | UI/SplashScreen.WarmAudioAssets; yielded sample loading and retention,not just clip references; music/streaming policy unchanged |
| Real menu activation barrier | UI/SplashScreen.MenuActivation.cs and ConvertedMainMenu.IsPrepared; retain existing canvas through Wire/layout,then reveal login/input |
| Boot failure exit | SplashScreen.MenuActivation exposes one focusable/pointer-accessible EXIT GAME control above the failed curtain; button and Cancel use the same quit path without claiming readiness |
| Arrival cancellation | MatchArrivalPresentation.Run is generation-owned and finally-cleaned even when another component drives its iterator; cancelled/disabled/replaced runs cannot reacquire the camera or hold after loading |
| Title/login art and avatars | UI/OwnerMenuArt.cs,Avatars.cs; async cold reads awaited per item,then existing retained texture/sprite caches; supplied pixels and fallback policy unchanged |
| Hub/HUD portraits and mode cards | UI/OwnerPortraitArt.cs; async roster-driven warmup and shared cache used by HubKit/TumpUiFactory |
| First HOME loop | ConvertedMainMenu starts UI/Hub/HubSceneVideo.Warmup.cs during usable login; async clips/selected poster and hidden decoder until frameReady, adopting the same player/target. Scene activation begins after login ends; low background priority is restored on menu destruction |
| Hidden HOME background | HubSceneVideo binds MapPreviewSurface rendering visibility; opaque media suspends the covered court camera/surface without discarding prepared scenes or the fallback |
| Ability prop source prefabs | Visual/HeroPropAssets.cs; current Paete/Rework/Phaister folders,no gameplay spawn during asset preload |
| Supplementary baked motion | Visual/GeneratedMotionAssets.cs; yielded per-rig data preload shared by CharacterAnimator and rooted introduction lookups,no clip/graph generation |
| First-person source meshes | Camera/ViewmodelMeshAssets.cs; async boot reads for stock arm/slipper, Inday details and roster-ID-derived left/right arms, retained by the same cache ViewmodelArms uses. Missing fallback and authored geometry/materials/poses are unchanged |
| Existing introduction preparation | UltimateIntroductionCache.PrepareRound runs through HubLoading.PrepareMatchVisuals before unchanged arena draws; yields per existing grounded-clip attempt,retains held variants and releases its preparation owner on cancellation |
| Roster outline geometry | Visual/OutlineNormals.Warmup and boot roster loop; per-mesh welds survive scene notifications while the exact mesh lives,without retaining dead runtime meshes |
| Effect sheets and authored intro data | Visual/VfxFlipbook.cs,UltimatePerformance and existing per-kit warmups |
| Shared particles and status icons | Visual/AbilityVfx.WarmupAssets prepares existing cached geometry; UI/StatusIcons.Warmup follows StatusRules.All with async sprite loading |
| Ability icons and cooldown | UI/AbilityIcons.Warmup async-loads illustrations,yields per existing fallback bake/upload and prepares the radial cooldown graphic; repeated warmup reuses completed cache entries |
| Match loading surface | UI/Hub/HubLoading.cs and MatchInstaller.IsPrepared; ARENAS ONLY. Every SceneFlow peer uses owned async loading after a curtain frame, with in-flight target deduplication, destination setup, failure/return and cancellation instead of timed success. externallyLoaded is explicit observation only, not the network default |
| Loading illustration deck | LoadingArtwork.Warmup asynchronously retains all three existing textures during boot; subsequent curtain installs and rotations reuse them. Direct no-boot fallback and artwork remain unchanged |
| Menu scene entry | SceneFlow.Go -> SceneManager.LoadScene. No curtain since 2026-09-30 (TODO LOAD-1.4): the second "GETTING READY" screen was removed as redundant; the boot splash is the one loading screen before a match |
| Custom map switching | SplashScreen retains all map dependencies at boot. MapPreviewSurface keeps the completed image during cold instantiation, rejects unavailable destinations before parking the valid map and reuses cached scenes/looks. [Native pixels and recovery](reports/laptop-validation-2026-10-06/preview-selection-recovery/README.md) |
| Training controls/targets | PracticeRange.cs,UI/PausePanel.TrainingRange.cs; prebuilt inactive menu and target bodies reused without changing saved preferences |
| First-use/runtime costs | Existing profiler markers,FrameRateHistogram,tools/cold_start.py and current internal player |
| Settings value changes | UI/TumpSettingsView partials; whole-row reflow only for new sections/text size,cached chips and one unsaved-state calculation per notification |
| Character preview targets | UI/ModelPreview.cs; coalesce continuous pixel-size reallocations,keep current panel projection,settle exact sizing and capture immediately |
| Repeated preview selection | ModelPreview.Show compares model/pet,clip/palette snapshots and shading mode; unchanged selections retain their instance/materials/pose,changed subjects deactivate before deferred destruction. Mutating authoring callers use forceRebuild before relative scaling/dressing; the retained creator remains unavailable to players |
| Runtime avatar lifetime | Visual/CharacterAnimator owns only avatars generated for its binding; release the graph before detaching/destroying them on rebind/clear/teardown,and preserve borrowed/imported avatars |
| Preview cache ownership | Visual/ToonSkin keys base/overlay variants separately; MapPreviewSurface destroys resized-out targets and rebinds camera/UI together |

## Rules For Changes

- Latest owner startup flow: usable login overlaps asynchronous Home preparation,
  then supplied title art becomes a noninteractive loading surface with a small
  actual progress bar and automatic Home arrival. Arena entry keeps its curtain.
  [Native login preparation](reports/laptop-validation-2026-10-06/login-video-preload/README.md).
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
- Introduction cache HasResult includes a known unsupported-rig result; Find still
  returns null for it. That result warns once per exact source/hero/held key and
  resets with a different source or new play session. It is not proof a clip exists.
- Measure representative player first-use/frame timings when the environment permits.
  Record source/build,hardware,path and memory tradeoffs; no hitch-free claims from
  compilation or a warm cache. Test changed stages,not the same whole boot repeatedly.

## Current Qualification Boundary

The current automatic startup route passes three native painting/real-pointer/
full-boot checks on270f98d75. Map warmup took41.53s of45.62s total in that editor
run; packaged/device timing remains open. [Exact evidence](reports/laptop-validation-2026-10-06/startup-auto-home-flow/README.md).

The latest [focused native integration](reports/stability-2026-09-27/input-integration.md)
passes loading-readiness and supplementary-data retention cases alongside five
state/presentation cases. The subsequent [UI/audio pass](reports/stability-2026-09-27/loading-audit.md#ui-flow-native-qualification)
passes three first-run range/settings/preview reuse cases; a separate new audio case
confirms deferred sample preparation and no playback. HOME decoded-frame handoff
now passes after fixing a reproduced30-second readiness timeout; reduced-motion
poster behavior also passes. Original boot menu activation and its failure exit
now have focused native state/input-routing evidence. Portrait/prop retention,whole-player
entry timings,other-device codecs,physical input and visual acceptance remain separate.
Earlier shader/art evidence remains separate. There is no current complete player
before/after hitch table. Consult the ledger for current headroom/processes; the
latest native pass succeeded, but player packaging headroom is not established.
Do not repeat unchanged successful cases or relaunch a blocked workload in a loop.
