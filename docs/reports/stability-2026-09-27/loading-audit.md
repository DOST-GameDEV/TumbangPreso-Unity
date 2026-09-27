# General loading and first-use audit, 2026-09-27

Latest evidence: the [input integration pass](input-integration.md) completed7/7
native state/loading cases, including destination readiness and supplementary-motion
retention. Its exact coverage supersedes earlier NOT RUN notes below for those
cases only. No player hitch table or blanket native qualification is implied.

## Deferred Audio Samples

Base `de426a0e`. The old splash audio stage loaded AudioClip references only.
139 of198 SFX importers disable preloadAudioData, all with DecompressOnLoad; the11
voice clips also arrived without loaded samples in the native check. The existing
reference preload therefore still left sample loading on first playback. Unity's
[sample-loading API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AudioClip.LoadAudioData.html)
and [preload flag](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AudioClip-preloadAudioData.html)
distinguish those two operations.

SplashScreen now loads non-streaming SFX/voice samples in yielded turns and retains
the actual clip references in its existing asset cache. Progress advances after
each completed cue; already-loaded clips are reused. An asynchronous loading state
gets a10-second per-clip bound and failures report a warning rather than hanging or
silently claiming sample readiness. No sound plays during preparation. Recordings,
import settings,quality,mix,music policy and streaming clips are unchanged.

One NEW guarded native test passed1/1 on206 frozen inputs with no drift. It reproduced
the reference-only gap with ui_toggle, then checked loaded samples,retained identity,
monotonic progress,repeated reuse,no new/playing sources and unchanged music state.
209 clips:150 initially cold,153 yielded turns,reported clip memory2,971,985 to
21,735,549bytes (17.9MiB moved earlier). Longest Editor iterator slice57.680ms;
the existing synchronous folder lookup and individual decodes are not frame-budget
guarantees. This is a sample-readiness fix,not a measured player hitch reduction.
Test duration1.4623945s; minimum sampled free storage6,720,446,464bytes; guard restored
two named-profile files and three shared Editor preferences. No old suite/films rerun.
[Receipt](checks/audio-preload-native.json),[native XML](checks/audio-preload-native.xml).

## UI Flow Native Qualification

The first focused pass for the previously implemented practice controls,settings
value-change path and preview resizing passed3/3 on the unchanged `de426a0e` layer:
206 frozen inputs,no drift,8.0297146s test duration,minimum free6,864,703,488bytes.
The guard restored two named-profile files and three shared Editor preferences.

- Practice: local control gates,prepared menu/target-body reuse and role/resource controls.
- Settings: retained rows,ordinary value changes and larger-text reflow.
- Preview: shared target during resize,exact settled dimensions and immediate capture sizing.

These results supersede only the corresponding NOT RUN notes below. They do not
establish visual approval,physical-device input,actual peers or player frame timings.
The earlier7 state/loading cases and2 Voodoo cases were not rerun.
[Receipt](checks/ui-flow-native.json),[native XML](checks/ui-flow-native.xml).

## First HOME decoder preparation

Base `4a9f9cf2`. HubSceneVideo previously loaded its clip/poster and prepared a
decoder in Awake on hub entry. Boot now asynchronously fetches the declared loop
metadata and selected poster,then retains one hidden paused player until a real
first decoded frame is ready. The first hub adopts that same player and render
target instead of reopening the decoder. Random selection,paired fallback,16:9
fit,silent playback and the authored movies are unchanged.

The image keeps its poster until frameReady,not just prepareCompleted. First-frame
callbacks are disabled after use. Reduced motion starts no decoder; live toggling
also pauses/shows the poster. Error or a30s failure budget ends preparation with
poster fallback and releases the target; this is not a minimum loading duration.
Concurrent asset waiters recheck the cache before creating a player. At most one
preloaded player is retained; later newly built hubs still use the normal path.
Memory tradeoff: one prepared decoder/1920x1080target lives earlier,until adoption.
No claim that every later hub rebuild or every click is now hitch-free.

API basis: Unity documents [paused preparation](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Video.VideoPlayer.Pause.html),
[retaining preparation by pausing rather than stopping](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Video.VideoPlayer.Prepare.html),
and [first-frame notification plus disabling its ongoing overhead](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Video.VideoPlayer-frameReady.html).

Runtime,Editor,Tests and PlayTests compile on126frozen inputs (5changed),reusing
the unchanged Core assembly. A final duplicate-creation guard was compiled in
Runtime only; unaffected assemblies/tests were not rerun.
[Receipt](checks/home-video-compile.json). Two new native tests cover paused first
frame/player-target reuse/cleanup and reduced-motion poster-only startup. They are
NOT RUN under the existing native disk boundary. No old film/suite repeat or asset
edit. Actual decoder/GPU/first-entry timings and native visuals remain OPEN.

## Shared menu portrait preload

Base `210cd801`. The avatar/title preload did not cover the separate hub portraits
and mode-card resource paths. HubKit loaded imported portraits on each lookup and
kept a second fallback-sprite cache; other native screens used OwnerPortraitArt.
Boot now loads each live people/can/slipper portrait and the six current mode cards
with Resources.LoadAsync, yielding per asset and advancing progress on completion.
Roster additions join without maintaining a second character list. Existing art,
fallbacks and layout remain; the shared cache retains asset references through
scene activation. HubKit,TumpUiFactory and the two mode-card callers use that cache.

All four assemblies compile on98 frozen inputs (7changed). The first compiler
preflight stopped at5,067,190,272freebytes, below5GiB, before starting; one bounded
retry passed after space recovered. No Unity/native launch or repeated film/suite.
[Compiler receipt](checks/menu-portraits-compile.json). The new native shared-cache,
progress and no-spawn check is pending. There is no measured first-click/hitch-free
claim, no decoder preparation and no additional UI or lobby instantiation here.

## Menu activation behind loading

Base `01346a80`. Splash previously destroyed its loading canvas before releasing
MainMenu's held async load. Unity's90% load had not run the menu's Awake/Start,
native hierarchy construction or canvas layout, leaving that work outside loading.

The existing loading owner and root canvas now survive activation and are retired
only after ConvertedMainMenu signals successful wiring and the initial layout/draw
frame passes. The normal account/preload/reading barriers are preserved. Login is
installed under loading but offered on reveal, so its welcome interval is not
consumed invisibly. Home still draws under the curtain but CanvasGroup gates its
controls; the any-key shortcut now respects that group. Menu music is deferred
until reveal. Missing/failed menu initialization reports failure instead of ready.
The curtain blocks pointer input and Escape; its retained video target/canvas and
takeover registration clean up on destruction. Supplied artwork is unchanged.

All four assemblies compile on the93-file frozen source/dependency candidate,
including7changed inputs. [Compiler receipt](checks/menu-activation-compile.json).
BootActivationRetainsCurtainAndDefersInputAndLoginUntilMenuIsPrepared was added to
the existing PlayMode fixture but NOT RUN under the existing disk limitation.
No previous shader/art check,character film or broad suite was repeated. Native
first-boot/return/error interaction and device timings remain OPEN. This removes
an uncovered initialization boundary; it does not prove all clicks are hitch-free.

## Hero prop prefab preparation

Follow-up after `750da6ad`: the existing Phaister setup warmup still called the
retired Grand Coven builder after the OMEN rework. It now calls only the authored
PhaisterOmen presentation with the current ultimate's windup/duration. It uses no
owner transform and never creates VoodooBlackHole,SeanceVoidComponent or HazardVolume.
The visual is immediately deactivated and destroyed; Random.state preservation and
the existing Ready/reset lifecycle remain. Source/API-call reviewed only; no new
compile/runtime/film or first-cast timing claim for this small loader correction.

Base `4b1f39d8`. The roster preload touches body models, but ability props have
separate resource paths. The general retained-asset cache explicitly excludes
GameObjects, so it does not retain those prefab roots across menu activation.
`HeroPropAssets` now loads the existing Paete, Rework and Phaister prop folders in
yielded boot stages, retaining their source prefabs and caching named lookups for
the four existing spawn paths. Progress advances after each completed folder load.
New assets in those folders are discovered without a second list of filenames.

The current source folders contain14 GLBs totaling5,553,428bytes on disk. This is
NOT their decoded resident-memory cost. No instance, effect, profile, material,
palette, geometry or animation is created/changed by the warmup. The loading screen
completes this stage before its existing asset-ready barrier can lift. Instantiation,
material generation and GPU first-draw cost are separate remaining work, not claimed
solved by loading prefab data.

Runtime and PlayTests compile with the installed Unity Roslyn toolchain and existing
Bee inputs.7/7 changed candidate hashes match. [Compiler receipt](checks/hero-props-compile.json).
Added one focused prefab retention/yield/no-effect-spawn case for the next safe
native integration run; it has NOT run. The preceding editor startup hit the disk
reserve, so no repeated editor launch was attempted for this batch. No measured
player frame-time, peak-memory or hitch-free claim.

## Character Preview Allocation

After `b93d2a4a`, ModelPreview coalesces repeated target-size changes instead of
releasing/allocating a new4x MSAA RenderTexture for every intermediate pixel size.
The first target is immediate. Later target allocation waits until dimensions have
been unchanged for120ms; explicit StepForCapture settles immediately through the
same path. The current panel projection updates during that wait so the previous
target's dimensions do not set the new display aspect.

Final physical-pixel sizing, the2048 uniform cap, filtering,MSAA,model framing and
source art are unchanged. During continuous resize the previous target is briefly
resampled, rather than allocating at every intermediate dimension. This is a bounded
allocation policy, not a measured FPS or first-click improvement claim.

EnsureAvatar also destroys a nonnull invalid avatar before returning: that newly
created object has not been assigned or shared. Valid-avatar lifetime management
remains OPEN. Unity documents [BuildGenericAvatar](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AvatarBuilder.BuildGenericAvatar.html)
as creating a new asset, while [OnDestroy](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MonoBehaviour.OnDestroy.html)
only runs for previously active objects. Inactive binding and copied avatar references
need native ownership evidence before adding a blanket destroy hook or cache. No
valid/imported avatar,rig,clip or animation was changed in this unit.

Frozen171 inputs,2 changed sources. Runtime,Editor,Tests and PlayTests compile
with unchanged Core reused. The new native case covers target identity during a
resize burst, final pixel resolution, stable reuse and capture flush; the existing
DPI/aspect case is unchanged. **NOT RUN** under the existing editor disk boundary.
No old Core,broad suite or films rerun. [Receipt](checks/preview-resize-compile.json).

## Settings Value-Change Work

After `2b588964`, ordinary settings slider/name changes no longer rerun the full
text/row layout pass or discover and resize every chip in the section. Section
construction caches its chip references and invalidates layout; a larger-text change
still reflows all rows and refits the cached chips. Binding labels are still read
on change, but only changed labels are assigned/refitted, avoiding a stale-label
assumption around device/binding changes.

Each notification evaluates unsaved state once rather than twice. Save-face styling
is updated only when that boolean changes. The transaction, live preview, profile
serialization, rebind and save/discard implementations are unchanged. No new settings
prebuild framework or UI redesign was introduced. This removes source-proven repeated
hierarchy/geometry work; player frame-time improvement is not measured.

Frozen169 inputs,4 changed sources. Runtime,Editor,Tests and PlayTests compile with
unchanged Core reused. Added a native case covering volume value changes, retained
rows, large/normal text reflow, binding rows and discard state. **NOT RUN** under the
existing editor disk boundary. No previous settings films/suites or Core cases rerun.
[Receipt](checks/settings-hotpath-compile.json).

## Supplementary Motion Data

After `fe3a93eb`, boot's roster stage asynchronously loads and retains the five
supplementary baked-motion sets resolved by each actual model's rig hierarchy:
dance,carry,swimming,recovery and rooted interactions. It yields between requests
and does not instantiate actors,create PlayableGraphs,generate clips or alter any
authored model,clip,timing,palette or rig path.

GeneratedMotionAssets is shared by CharacterAnimator binding and the existing
Paete/Phaister introduction rooted-clip lookups. Successful assets stay referenced
and repeated roster rigs reuse them. Missing/non-rigged assets retain the old null
fallback and are not permanently negative-cached, including editor authoring changes.
The cache resets at subsystem registration. Existing imported roster clips remain
separate; no blanket resource-folder load was added.

This moves supplementary asset lookup/deserialization off the first model bind or
introduction. It does not remove graph construction,instantiation/material/GPU cost
or establish a measured no-hitch result. Decoded resident-memory and first-use player
timings remain unmeasured. HeroVoice's lazy HeroVo lookup was inspected but left
unchanged because no hero-recording resources are present in the current repository.

Frozen165 inputs,8 owned source/test/metadata paths. Runtime,Editor,Tests and
PlayTests compile with unchanged Core reused. The new native case checks exact
retained asset identity,repeated warmup and no actor/animator component creation;
**NOT RUN** under the existing editor disk boundary. No old Core/broad suite/films
repeated. [Receipt](checks/motion-loading-compile.json).

## Destination Setup Readiness

After `a6111f72`, the generic loading handoff no longer treats eight elapsed seconds,
an unrelated ReadyGate or a persistent active RoundDirector as completed setup.
MatchInstaller publishes success only after its body, HUD and control installation
returns; range launches additionally await the range's live startup. Installation
exceptions remain failures and retain their diagnostic exception.

HubLoading requires an installer in the actual destination scene. A networked
same-scene rematch must first replace the previous Scene instance. Progress stays
at the completed scene-load stage while installation is pending instead of advancing
by elapsed time. A60-second watchdog produces an error and a focused return-to-menu
button, never100percent or a ready log. Prewarm exceptions likewise retain the
curtain with an error. Existing warmup views, camera settings and map assets are
unchanged. The loading owner disposes its existing preparation iterator so its
camera/target cleanup runs on cancellation or destruction too.

A non-match transition or replacement cancels the obsolete curtain, releases its
root canvas and input ownership, and cannot leave it covering HOME. PlayerInputReader
clears held gameplay input while loading and discards held menu actions until release
when loading finishes. No new input backend, match rule or network protocol was added.

Frozen154 inputs,5 owned source/test/metadata paths. Runtime, Editor, Tests and
PlayTests compile; unchanged Core is reused. The first compiler run found obsolete
integer SceneHandle conversions in the new code; the bounded correction retains
and compares Scene values directly. The new native case covers stale global round,
unprepared destination, error/return availability, cancellation and same-scene reload.
It is **NOT RUN** under the existing editor disk-reserve limitation. No old Core tests,
films or broad suites were repeated, and no player hitch-free claim follows.
[Compiler/source receipt](checks/match-loading-compile.json).

## Evidence boundary

The intake source audit began at ASTRAReworks `04886cc4`; current instrumentation is based on `026fed74` plus reviewed diagnostic amendments. The historical Desktop player identifies itself as a dirty `85832b6b` build, not a current-source performance baseline. The guarded Development build stopped during packaging when storage fell below the protected reserve. No current player boot, first-click, cast or profiler before/after timing exists, so no hitch reduction or FPS improvement is claimed.

## Concrete boot defect

`SplashScreen.PreloadGameAssets` used `shaderCount` to bound calls to `ShaderVariantCollection.WarmUpProgressively(10)` and stopped on a `false` return. Unity 6 documents the parameter as **variant count** and the return as **true when all variants are warmed** ([API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ShaderVariantCollection.WarmUpProgressively.html)). The tracked collection contains more variants than shaders, so the original loop could stop after its first incomplete slice. This is a source-level correctness defect, not a measured frame hitch.

The current main source bounds calls by `variantCount`, stops on `true`, and still yields after every 10-variant slice. It records shader/variant totals, warmed count, completion, call count, elapsed time and longest slice. A bounded pass that remains incomplete emits an explicit warning and continues rather than holding the splash indefinitely. Actual slice time and Android responsiveness require device measurement; ten variants alone cannot guarantee an ANR-free frame.

The same existing splash hero loop now fills `UltimatePerformance`'s parsed-table cache for each hero's ordinary and held-slipper introduction, yielding after each hero. Previously the first accepted ultimate could call `SharedUltimatePhase.SecondsFor` into `UltimatePerformance.For`, loading and parsing the authored text on the action path. The existing loader unloads each `TextAsset` after parsing and retains only its bounded cached table; no effect, scene object, profile or rule is created by this preload. This is a source-level first-use move only, with no new player timing claim.

## Other first-use paths

Hero-shop wallet updates previously called the full `HubHero.Show` path on both
busy and completion notifications. That path destroys/reinstantiates the preview
and rebuilds every ability tile. Wallet notifications and purchase completion now
call the existing purchase-control refresh only; changing hero still rebuilds the
selected presentation. This removes redundant work on the request path and keeps
the inspected model intact. It is source-reviewed, not a measured frame-time gain.

Boot stages load roster resources, audio folders, UI resources and ability data in separate steps (`SplashScreen.cs`). `VfxFlipbook.Build` formerly loaded its sheet texture on the first effect; it now uses a successful-only texture cache, and the existing ability-resource stage warms the 12 `VfxSheets.All` entries with a yield after each. Their current source PNGs total about 7.15 MiB as uncompressed RGBA with no mipmaps; no effect mesh, material or GameObject is built at boot, and the missing-sheet warning remains on the live path. First ability use can still create effects or read other assets, including `PaeteTrees.Spawn`, `FrostSurfacePresentation.Part` and `WindVfx`; these are unmeasured candidates, not proven frame hitches. The built-in `MatchStatsCollector` histogram samples active rounds rather than boot or menu loading. Preserve original model, texture, mesh, audio and effect quality during any future optimization.

## Validation status

The frozen broad pre-build gate completed red: 555 cases, 468 passed, 72 failed and 15 skipped. Four focused EditMode contract classes on the earlier performance candidate passed 74/74, confirming that candidate compiled. A later one-case PlayMode check compiled the current authored source snapshot and passed the corrected shader stage plus staged menu-art/avatar cache references ([loading-warmup-final.xml](checks/loading-warmup-final.xml), 1/1). Its Editor receipt was `shaders=50 variants=97 warmed=97 complete=True calls=10`; the longest recorded slice was 497.021 ms in Editor, so no smooth-loading-frame or handset claim follows. That count belongs to the isolated collection (SHA-256 `FD1C564C55242752C2721AEDCFD2A6512C57F3EE5DA86CB55F4A05DE9BD3B709`); the main collection has different bytes (SHA-256 `99F204ED8EEB12C2C2C353CB35389F3CE9EFE9B092660B7B2299DEC8FC985641`) and was not replaced. The Development player build did not complete, and no player profiler `.raw` or first-use performance table exists. Windows player profiling remains unmeasured until a guarded build and storage headroom are available.
