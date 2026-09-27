# General loading and first-use audit, 2026-09-27

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
