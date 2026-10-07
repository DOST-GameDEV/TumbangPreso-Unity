using System.Collections;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Skippable studio intro, then work-driven loading and the existing menu barrier.
    /// Skipping only ends the logo presentation; it never bypasses game readiness.
    /// The importer preserves the authored clip and sting bindings.
    /// </summary>
    public sealed partial class SplashScreen : MonoBehaviour
    {
        /// <summary>When crossed, log that loading is slow but keep the barrier intact.</summary>
        public const float MaxWait = 6.0f;
        private IllustratedBackdrop _illustration;
        private GameObject _storyRoot;
        private Text _storyText;
        private Button _artButton;
        private RectTransform _loadingMark;
        private int _storyIndex;

        [SerializeField] private VideoClip _clip;
        [SerializeField] private AudioClip _sting;

        private RawImage _surface;

        private Image _fade;
        private Text _loadingLabel;
        private Image _loadingFill;
        private RectTransform[] _loadingDots;
        private GameObject _canvas;
        private AsyncOperation _menu;
        private float _elapsed;
        private bool _leaving;
        private bool _assetsPreloaded;
        private bool _slowLoadReported;

        /// <summary>
        /// Where the load actually is, and where the bar has got to drawing it.
        ///
        /// ⚠️⚠️ TWO VALUES, BECAUSE ONE SNAPPED. `SetLoadingStage` used to write `fillAmount`
        /// directly, so the bar jumped 0.14 to 0.27 in a single frame and then held still for
        /// three seconds while the stage it had already announced actually ran. A bar that is
        /// stationary for three quarters of a boot is not reporting progress, it is reporting
        /// that four numbers were written. `_shown` eases toward `_target` every frame, so the
        /// bar is always moving toward the truth and never ahead of it.
        ///
        /// ⚠️ AND IT NEVER REACHES 1.0 BEFORE THE SCREEN LEAVES. See <see cref="SignInSpan"/>.
        /// </summary>
        private float _targetProgress;
        private float _shownProgress;

        /// <summary>
        /// ⚠️⚠️ THE ACCOUNT BARRIER OWNS THE LAST EIGHT PER CENT, BECAUSE IT WAS NOT IN THE BAR AT
        /// ALL AND IT IS THE PART THAT TAKES THE LONGEST ON A BAD CONNECTION. The loop waits on
        /// `accountBarrier.IsCompleted` as well as on the preload, and `PreloadComplete` knows
        /// nothing about it, so the bar reached its last stage, wrote `opening main menu`, and the
        /// screen then sat there with a full bar and a label that had already claimed the menu was
        /// open. That is the frame 🧑 photographed on 2026-09-01 and called broken, and he was
        /// reading it correctly: the screen was telling him it had finished.
        /// </summary>
        private const float SignInSpan = 0.92f;

        /// <summary>
        /// Unity performs an unused-asset sweep when the held MainMenu load activates. Merely
        /// loading and unloading an arena therefore warms disk caches but does not guarantee its
        /// meshes and textures remain in memory. These explicit static references survive the
        /// SplashScreen scene and make the preload barrier mean what it says.
        /// </summary>
        private static class WarmAssetCache
        {
            private static readonly System.Collections.Generic.List<Object> Assets =
                new System.Collections.Generic.List<Object>();
            private static readonly System.Collections.Generic.HashSet<EntityId> EntityIds =
                new System.Collections.Generic.HashSet<EntityId>();

            public static int Count => Assets.Count;

            public static void Retain(Object asset)
            {
                if (ShouldRetain(asset) && EntityIds.Add(asset.GetEntityId())) Assets.Add(asset);
            }

            public static void CaptureLoadedAssets()
            {
                foreach (Object asset in Resources.FindObjectsOfTypeAll<Object>())
                    Retain(asset);
            }

            private static bool ShouldRetain(Object asset)
            {
                if (asset == null || asset is GameObject || asset is Component ||
                    asset is RenderTexture)
                    return false;

                return asset is Mesh ||
                       asset is Material ||
                       asset is Texture ||
                       asset is Shader ||
                       asset is Sprite ||
                       asset is AudioClip ||
                       asset is AnimationClip ||
                       asset is RuntimeAnimatorController ||
                       asset is Avatar ||
                       asset is Font ||
                       asset is TextAsset ||
                       asset is VideoClip ||
                       asset is ScriptableObject ||
                       asset is PhysicsMaterial ||
                       asset is TerrainData;
            }
        }

        private void Start()
        {
            // `splash_screen.gd::_ready` opens with `Input.mouse_mode = MOUSE_MODE_VISIBLE`.
            // A game whose very first frame hides the cursor cannot be quit with the mouse.
            CursorMode.Release();

            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            // Startup is logos only. Preparation belongs to the visible title
            // after account admission, never to an illustrated screen before login.
            GameServices.Music?.StopNow();
            yield return PlayStudioIntro();
            ReleaseStudioIntro();
            BootSting.Stop();
            SceneFlow.BootedThroughSplash = true;
            SceneFlow.LoginStepOffered = false;
            _menu = SceneManager.LoadSceneAsync(SceneFlow.MainMenu);
            while (_menu != null && !_menu.isDone) yield return null;
        }

        private IEnumerator RunLegacyPreparation()
        {
            GameServices.Music?.StopNow();
            yield return PlayStudioIntro();
            BuildSurface();
            SetFadeColour(Color.white);
            SetFade(1);
            ReleaseStudioIntro();
            BootSting.Stop();
            BeginPreload();
            // Presentation skips never bypass asset, account or menu readiness.
            var accountBarrier = GameServices.Account?.InitializeAsync();

            // ⚠️ The original white fade avoided a black flash after the studio mark
            // (114.2). The illustrated route uses warm paper for the same quiet join.
            float fade = 0.0f;

            while (!_leaving)
            {
                _elapsed += Time.unscaledDeltaTime;

                bool accountReady = accountBarrier == null || accountBarrier.IsCompleted;

                // ⚠️⚠️ SIGN-IN IS A STAGE WITH A LABEL, NOT AN INVISIBLE EXTRA WAIT. See
                // `SignInSpan`: the bar used to reach its last stage and then stall at full while
                // this barrier ran, which told the player the boot had finished when it had not.
                if (_assetsPreloaded && !accountReady)
                    SetLoadingStage("signing in", SignInSpan + 0.04f);

                UpdateLoadingAnimation();
                if (_storyRoot != null && _storyRoot.activeSelf && InputLayer.MenuNav.CancelPressed)
                {
                    ShowStory(false);
                    ScreenTakeover.ConsumeEscape();
                }

                fade = Mathf.Clamp01(_elapsed / 0.35f);
                SetFade(1.0f - fade);

                if (LoadingPresentation.CanLeave(PreloadComplete, accountReady,
                        _storyRoot != null && _storyRoot.activeSelf)) break;

                if (!_slowLoadReported && !PreloadComplete && _elapsed >= MaxWait)
                {
                    _slowLoadReported = true;
                    Debug.LogWarning("[Splash] preload exceeded six seconds; keeping the loading screen visible until it is ready.");
                }

                yield return null;
            }

            yield return ActivatePreparedMenu();
            if (_menuActivationFailed)
            {
                while (!_quitAfterMenuFailure && !InputLayer.MenuNav.CancelPressed) yield return null;
                SceneFlow.Quit();
                yield break;
            }

            // ⚠️ FULL ONLY ON THE WAY OUT. Everything above is bounded under 1.0 so that a full
            // bar is never a thing the player can sit and look at.
            Debug.Log($"[Splash] boot loading finished after {_elapsed:F2} s (work-driven, no reading window).");
            SetLoadingStage("ready", 1.0f);
            _shownProgress = 1.0f;
            if (_loadingFill != null) _loadingFill.fillAmount = 1.0f;

            // ⚠️ THE TWO MIDDLE FUNNEL STEPS ARE RECORDED HERE BECAUSE THIS IS THE ONE PLACE THAT
            // KNOWS BOTH ANSWERS. `FUTURE.md` § 3 wants launch, sign-in and menu as separate
            // steps, and the difference between "the account settled" and "the menu appeared" is
            // exactly the wait this loop just finished. Recording them from the menu instead
            // would collapse the two and hide a slow boot, which is the thing worth finding.
            // `docs/TODO.md` § 90.3.
            var telemetry = GameServices.Telemetry;
            if (telemetry != null)
            {
                telemetry.NoteSignInSettled(GameServices.Account != null && GameServices.Account.IsSignedIn);
                telemetry.NoteMenuReached();
            }

            // ⚠️ Commit to leaving before the fade so a late press cannot open a
            // story after its readiness/reading barrier has already been evaluated.
            if (_artButton != null) _artButton.interactable = false;
            if (_illustration != null) _illustration.Animating = false;
            // The illustrated home continues on paper; the legacy video exits on black.
            SetFadeColour(_ownerLoading ? (Color)new Color32(238,108,74,255) : _illustration != null ? UiTheme.Paper : Color.black);

            for (float t = 0.0f; t < 0.22f; t += Time.unscaledDeltaTime)
            {
                SetFade(t / 0.22f);
                yield return null;
            }

            // ⚠️⚠️ THIS IS THE ONE PLACE THAT CAN SAY "THE GAME JUST BOOTED", AND
            // `PlayerNameplate.OfferTheAccountChoiceOnce` NEEDS IT TO BE A NARROW CLAIM. The
            // account question was first gated on nothing but a nameplate being installed, and a
            // nameplate is installed by every scene that shows the menu: `UiClickProbe` reported
            // **every settings control on the title screen blocked by `SignInCanvas`**, because
            // the boot screen opened over a menu the probe had loaded directly and nothing was
            // ever going to answer it. That is the same class § 92.7 records, and the same probe
            // caught it again.
            //
            // ⚠️ A SCENE LOAD IS NOT A BOOT. The menu is reached from here, from
            // `LeaveMatchToMainMenu`, and from any test that loads it by name; only the first is
            // a launch, and only a launch has a first-time player behind it.
            SceneFlow.BootedThroughSplash = true;

            SetFade(1.0f);
            Leave();
        }

        /// <summary>
        /// ⚠️ THE ASSETS ARE WARMED BEFORE THE HELD MENU LOAD STARTS. Unity serialises scene
        /// operations behind a load whose activation is held at 90%, so starting the menu first
        /// deadlocks every additive arena load queued after it. The menu is deliberately the final
        /// preload operation; only then is its activation held until readiness finishes.
        /// </summary>
        private void BeginPreload()
        {
            StartCoroutine(PreloadGameAssets());
        }

        private void BeginMenuPreload()
        {
            if (!Application.CanStreamedLevelBeLoaded(SceneFlow.MainMenu)) return;

            _menu = SceneManager.LoadSceneAsync(SceneFlow.MainMenu);
            if (_menu != null) _menu.allowSceneActivation = false;
        }

        /// <summary>
        /// Everything the game will need, brought into memory and onto the GPU while the sting
        /// plays, so no first-use of anything happens during a round.
        ///
        /// ⚠⚠ THE POINT IS THE FIRST FRAME OF THE MATCH, NOT THE MENU. 🧑 2026-08-23: *"load
        /// every resource in the BH studios loading screen, when i click play it lags and loads
        /// everything i think"*. Exactly right, and the old routine is why: it warmed the
        /// roster, the audio and the MAIN MENU scene, and then the arena, its materials, the
        /// hero effect meshes and every procedurally baked UI sprite were all still cold when
        /// the player pressed Play. The work did not disappear, it just happened at the worst
        /// possible moment.
        ///
        /// ⚠️ IT YIELDS BETWEEN EVERY STAGE, DELIBERATELY. This runs while a video is playing;
        /// a stage that blocks for 400 ms stutters the sting itself, which is the studio logo.
        /// The barrier at the end is what guarantees completeness, not doing it all in one go.
        ///
        /// ⚠️ ADD NEW STAGES HERE, AND ADD THEM WITH A `yield`. Anything that is instantiated,
        /// baked or compiled the first time it is used belongs in this list. The rule for
        /// deciding: if it can hitch, it warms here.
        /// </summary>
        /// <summary>The generated collection in `Resources`, written by
        /// `TumbangPreso.EditorTools.ShaderWarmupCollection` on every build.</summary>
        private const string ShaderWarmupResource = "ShaderWarmup";

        /// <summary>
        /// Maximum shader variants warmed between two frames.
        ///
        /// ⚠️ SMALL ON PURPOSE. The whole point is that the main thread comes back between
        /// slices, and the target device is a cheap handset rather than this desktop. Ten
        /// variants is a ceiling,not an unconditional batch. Check elapsed time after each
        /// variant so a slow compilation yields before starting another. One native compile
        /// can exceed the budget; actual handset slice time still needs measurement.
        /// </summary>
        private const int ShaderWarmupSlice = 10;
        private const double ShaderWarmupBudgetMs = 2;

        private IEnumerator PreloadGameAssets()
        {
            // 1. Warm up shaders across all materials, A SLICE PER FRAME.
            //
            // ⚠️⚠️ THIS WAS `Shader.WarmupAllShaders()` AND IT ANR'D A PHONE AT BOOT, TWICE.
            // `docs/TODO.md` § 126.10: the .apk never got past this exact bar in two separate
            // launches, several minutes each, and Android raised its "isn't responding" dialog
            // over the loading screen both times. That call compiles every variant in the build
            // in ONE blocking call, and it was **the only stage in this routine that could not
            // yield**, in a routine whose own header three paragraphs up says *"IT YIELDS BETWEEN
            // EVERY STAGE, DELIBERATELY"* and whose every other stage was broken up per character
            // for exactly this reason (§ 114.4). It was left whole because on a desktop it costs
            // a few seconds.
            //
            // ⚠️⚠️ AND IT IS NOT ONLY AN EMULATOR PROBLEM. A cheap Metro Manila handset is the
            // target (`GameBuilder.ConfigureAndroid`'s own note) and it pays a version of this
            // cost on every cold boot, on the one screen where the player has nothing to look at
            // but a bar that is not moving. **An ANR at boot is the worst place to have one,
            // because Android offers the player a button that closes the game.**
            //
            // ⚠️ `WarmUpProgressively` IS THE ONLY INCREMENTAL WARM-UP UNITY EXPOSES.
            // `Shader.WarmupAllShaders` and `ShaderVariantCollection.WarmUp` are both
            // all-or-nothing. `ShaderWarmupCollection` (editor) generates the asset from every
            // material in the project on every build, so the coverage is a fact about the build
            // rather than about whichever screens somebody walked past while recording.
            SetLoadingStage("preparing shaders", 0.04f);
            yield return null;

            var warmup = Resources.Load<ShaderVariantCollection>(ShaderWarmupResource);
            if (warmup == null)
            {
                // ⚠️ NAMED RATHER THAN SILENTLY SKIPPED, AND IT DOES NOT FALL BACK TO
                // `WarmupAllShaders`. Falling back would reinstate the ANR on the one platform
                // that cannot survive it, and would do so invisibly on the build where the asset
                // failed to generate. `ShaderWarmupCollection.Execute` is a build gate for this
                // reason: a missing collection is a build problem, not a runtime decision.
                Debug.LogWarning($"[Splash] no '{ShaderWarmupResource}' variant collection, so " +
                                 "shaders will compile at first use. Run " +
                                 "TumbangPreso.EditorTools.ShaderWarmupCollection.RebuildForBuild.");
            }
            else
            {
                // The API takes variants per call and returns true when the collection is done.
                // Keep a variant-count bound as protection against a stalled warmup.
                int callLimit = Mathf.Max(1, warmup.variantCount);
                var warmupWatch = System.Diagnostics.Stopwatch.StartNew();
                double maxSliceMs = 0;
                int warmupCalls = 0, warmupFrames = 0;
                bool done = false;

                while (!done && warmupCalls < callLimit)
                {
                    long sliceTick = System.Diagnostics.Stopwatch.GetTimestamp();
                    double sliceMs;
                    int frameCalls = 0;
                    UnityEngine.Profiling.Profiler.BeginSample("TUMP.Boot.ShaderWarmupSlice");
                    do
                    {
                        done = warmup.WarmUpProgressively(1);
                        warmupCalls++; frameCalls++;
                        sliceMs = (System.Diagnostics.Stopwatch.GetTimestamp() - sliceTick) * 1000.0 /
                            System.Diagnostics.Stopwatch.Frequency;
                    }
                    while (!done && warmupCalls < callLimit && frameCalls < ShaderWarmupSlice && sliceMs < ShaderWarmupBudgetMs);
                    UnityEngine.Profiling.Profiler.EndSample();
                    maxSliceMs = System.Math.Max(maxSliceMs, sliceMs);
                    warmupFrames++;

                    // The bar moves inside the stage rather than only at its boundaries, so the
                    // stage that used to look frozen is now the one that visibly counts down.
                    SetLoadingStage("preparing shaders",
                                    Mathf.Lerp(0.04f, 0.09f, warmup.warmedUpVariantCount / (float)callLimit));
                    yield return null;
                }
                Debug.Log(System.FormattableString.Invariant($"[SplashShaders] shaders={warmup.shaderCount} variants={warmup.variantCount} warmed={warmup.warmedUpVariantCount} complete={warmup.isWarmedUp} calls={warmupCalls} frames={warmupFrames} budget_ms={ShaderWarmupBudgetMs:F1} elapsed_ms={warmupWatch.Elapsed.TotalMilliseconds:F3} max_slice_ms={maxSliceMs:F3}"));
                if (!warmup.isWarmedUp)
                    Debug.LogWarning($"[SplashShaders] bounded warmup incomplete: {warmup.warmedUpVariantCount}/{warmup.variantCount} variants; remaining variants may compile on first use.");
            }

            yield return null;

            // 2. Pre-load RosterBook (models, rigs, materials, clips, pets)
            //
            // ⚠️⚠️ IT YIELDS PER CHARACTER, AND IT USED TO WALK THE WHOLE BOOK IN ONE FRAME.
            // `docs/TODO.md` § 114.4: every one of these property reads pulls a mesh, a clip set,
            // a palette and a pet off disk, and doing all of them between two `yield`s hands the
            // main thread away for as long as that takes. The dots and the bar are driven from
            // `Update`-time code on that same thread, so the loading animation froze for the whole
            // stage. **An animation that stops during loading is reporting the opposite of what it
            // exists to report.**
            //
            // ⚠️ THE PROGRESS MOVES INSIDE THE STAGE TOO, rather than only at its boundaries, so
            // the longest stage is not also the flattest part of the bar.
            SetLoadingStage("loading characters", 0.10f);
            yield return RosterBook.Warmup();
            var book = RosterBook.Load();
            if (book != null)
            {
                if (book.People != null)
                {
                    for (int i = 0; i < book.People.Count; i++)
                    {
                        var p = book.People[i];
                        if (p != null)
                        {
                            _ = p.Model;
                            _ = p.Clips;
                            _ = p.Palette;
                            _ = p.PetModel;
                            yield return Visual.GeneratedMotionAssets.Warmup(p.Model);
                            yield return Visual.OutlineNormals.Warmup(p.Model);
                            yield return Visual.OutlineNormals.Warmup(p.PetModel);
                        }

                        SetLoadingStage("loading characters",
                            Mathf.Lerp(0.10f, 0.22f, (i + 1) / (float)Mathf.Max(1, book.People.Count)));
                        yield return null;
                    }
                }

                if (book.Cans != null)
                {
                    foreach (var c in book.Cans)
                    {
                        if (c != null) yield return Visual.OutlineNormals.Warmup(c.Model);
                    }
                }
                yield return null;

                if (book.Slippers != null)
                {
                    foreach (var s in book.Slippers)
                    {
                        if (s != null) yield return Visual.OutlineNormals.Warmup(s.Model);
                    }
                }
            }
            yield return CameraSystem.ViewmodelMeshAssets.Warmup(book, done =>
                SetLoadingStage("preparing first-person meshes", Mathf.Lerp(.22f, .24f, done)));
            SetLoadingStage("loading characters", 0.24f);
            yield return null;

            SetLoadingStage("preparing nameplate text", 0.24f);
            yield return Visual.CharacterNameplate.WarmupFont();

            // 3. Load clip references AND short-cue sample data before first playback.
            yield return WarmAudioAssets();

            // 4. Pre-load Settings & Roster tables
            SetLoadingStage("applying settings", 0.36f);
            _ = Settings.SettingsStore.Current;
            _ = Roster.People;
            _ = Roster.ClassicPeople;
            _ = Roster.HeroPeople;
            _ = Roster.Cans;
            _ = Roster.Slippers;
            yield return null;

            // 5. The input asset, with the player's rebinds already applied.
            //
            // ⚠️ THE HUD READS BINDINGS TO DRAW ITS KEY CAPS, so a cold asset means the first
            // frame of the deck draws "?" on all three tiles and then corrects itself.
            SetLoadingStage("preparing controls", 0.44f);
            var actions = Resources.Load<UnityEngine.InputSystem.InputActionAsset>("TumbangPreso");
            if (actions != null) Settings.Rebinding.Load(actions);
            yield return null;

            SetLoadingStage("loading menu artwork", 0.48f);
            yield return OwnerMenuArt.Warmup();
            yield return LoadingArtwork.Warmup();
            yield return Avatars.Warmup();
            yield return OwnerPortraitArt.Warmup(done =>
                SetLoadingStage("loading menu artwork", Mathf.Lerp(.48f, .52f, done)));

            // 6. Every procedurally baked UI sprite.
            //
            // ⚠️⚠️ THESE ARE PAINTED PIXEL BY PIXEL ON FIRST USE AND THAT IS NOT FREE. Each
            // `GodotTheme.Box` rasterises a rounded, bordered texture and uploads it, and the
            // HUD asks for a fresh one for every distinct fill, border, width and radius the
            // frame it is built. Baking them here moves the whole cost behind the logo.
            SetLoadingStage("building interface", 0.52f);
            WarmSprites();
            yield return null;
            // Home's clips, poster and decoder prepare while the login form is
            // already usable. ConvertedMainMenu owns that asynchronous stage.

            // Gameplay-only caches prepare while login is usable. Shader and
            // map scene activation stay here because their Awake work is indivisible.

            // 8. Both arenas, as a dependency load rather than a scene load.
            //
            // ⚠️⚠️ THIS IS THE ONE THAT ACTUALLY FIXES THE STUTTER ON PLAY. Only the menu was
            // being pre-loaded, so the map, its materials and its meshes were read off disk on
            // the frame the player pressed the button. `LoadSceneAsync` cannot hold two scenes
            // at 90% at once without activating one of them, so the arena is warmed through its
            // ASSETS instead: everything the scene references is what costs the time, not the
            // scene graph.
            //
            // ⚠️ EVERY MAP, NOT THE SELECTED ONE. Nothing has been selected yet at boot, and the
            // player can change the map on the setup screen without ever returning here. This read
            // "both maps" and warmed Eskinita and Bayan Plaza only, which was every map when it was
            // written; Ilalim ng Tulay, Sa Bubong and the Lagoon then loaded cold on PLAY. Owner,
            // 2026-09-27: every shader and every asset loads behind the loading screen.
            //
            // ⚠️⚠️ UNCONDITIONAL AGAIN. From 2026-09-27 this was skipped whenever the hub was on,
            // because the hub loaded every arena itself behind a second "GETTING READY" screen
            // (`HubLoading.PreparePreview`). Request, 2026-09-30: that second screen is
            // redundant and every asset and shader loads HERE. The hub curtain is gone, so this is
            // once again the only place the arenas' meshes, textures and materials are read.
            double mapWarmBegan = Time.realtimeSinceStartupAsDouble;
            yield return WarmMapAssets();
            Debug.Log($"[Splash] map asset warmup completed in {Time.realtimeSinceStartupAsDouble - mapWarmBegan:0.00} s.");

            WarmAssetCache.CaptureLoadedAssets();
            Debug.Log($"[Splash] preload retained {WarmAssetCache.Count} assets in memory.");
            _ = Net.SkillContractFingerprint.Current;

            // This must remain the final scene operation in the preload chain. Once activation is
            // held, Unity will not complete an additive load or unload queued behind this one.
            SetLoadingStage("opening main menu", 0.88f);
            BeginMenuPreload();
            _assetsPreloaded = true;
        }

        internal static IEnumerator WarmGameplayAssets(System.Action<float> completed)
        {
            yield return AbilityIcons.Warmup(done=>completed?.Invoke(.2f*done));
            yield return StatusIcons.Warmup();
            completed?.Invoke(.25f);
            yield return Visual.VfxFlipbook.Warmup();
            completed?.Invoke(.4f);
            yield return Visual.CheskaIceVisuals.Warmup();
            yield return Visual.HeroPropAssets.Warmup(done=>completed?.Invoke(.4f+.25f*done));
            yield return Visual.AbilityVfx.WarmupAssets();
            completed?.Invoke(.7f);
            string[] heroes = Roster.HeroPeople != null
                         ? HeroIdsFrom(Roster.HeroPeople)
                         : new string[0];
            for (int i=0;i<heroes.Length;i++)
            {
                string heroId=heroes[i];
                var kit = Abilities.HeroAbilitySystem.CreateKitFor(heroId);
                _ = kit?.Skill1?.Name;
                _ = kit?.Skill2?.Name;
                _ = kit?.Ultimate?.Name;
                _ = Visual.UltimatePerformance.For(heroId);
                _ = Visual.UltimatePerformance.For(heroId, holdingSlipper: true);
                completed?.Invoke(.7f+.3f*(i+1)/heroes.Length);
                yield return null;
            }
            completed?.Invoke(1f);
        }

        private static string[] HeroIdsFrom(System.Collections.Generic.IReadOnlyList<RosterEntry> people)
        {
            var ids = new string[people.Count];
            for (int i = 0; i < people.Count; i++) ids[i] = people[i] != null ? people[i].Id : null;
            return ids;
        }

        private IEnumerator WarmAudioAssets()
        {
            string[] folders = { "Sfx", "Vo", "Music" };
            for (int folder = 0; folder < folders.Length; folder++)
            {
                SetLoadingStage("loading audio", Mathf.Lerp(.24f, .34f, folder / (float)folders.Length));
                yield return null;
                AudioClip[] clips;
                try { clips = Resources.LoadAll<AudioClip>(folders[folder]); }
                catch (System.Exception error)
                {
                    Debug.LogWarning($"[SplashAudio] Cannot load {folders[folder]}: {error.Message}");
                    continue;
                }

                for (int i = 0; i < clips.Length; i++)
                {
                    var clip = clips[i];
                    WarmAssetCache.Retain(clip);
                    // Preserve the music/streaming policy. Loading an AudioClip alone does
                    // not load samples when its importer disables preloadAudioData.
                    if (clip != null && folders[folder] != "Music" &&
                        clip.loadType != AudioClipLoadType.Streaming && clip.loadState != AudioDataLoadState.Loaded)
                    {
                        yield return null;
                        if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
                        float deadline = Time.realtimeSinceStartup + 10f;
                        while (clip.loadState == AudioDataLoadState.Loading && Time.realtimeSinceStartup < deadline)
                            yield return null;
                        if (clip.loadState != AudioDataLoadState.Loaded)
                            Debug.LogWarning($"[SplashAudio] {clip.name} sample preparation ended in {clip.loadState}; playback may load late or remain silent.");
                    }
                    SetLoadingStage("loading audio", Mathf.Lerp(.24f, .34f,
                        (folder + (i + 1f) / clips.Length) / folders.Length));
                }
                SetLoadingStage("loading audio", Mathf.Lerp(.24f, .34f, (folder + 1f) / folders.Length));
            }
        }

        /// <summary>
        /// ⚠️ THE ARENA'S ASSETS, NOT THE ARENA. `Application.CanStreamedLevelBeLoaded` only
        /// tells us the scene is in the build; there is no supported way to hold two scenes
        /// pre-loaded at once. Loading additively and immediately unloading DOES warm the
        /// dependency graph, and it happens behind a full-screen video where a frame spike
        /// costs nothing.
        /// </summary>
        private IEnumerator WarmMapAssets()
        {
            string[] maps = SceneFlow.Maps;

            // MatchInstaller's setup happens in Start(). Mark these loads as previews before they
            // are requested so no seats, HUD, services or match state are created behind the sting.
            bool previousPreviewOnly = MatchInstaller.PreviewOnly;
            MatchInstaller.PreviewOnly = true;

            try
            {
                for (int m = 0; m < maps.Length; m++)
                {
                    string map = maps[m];
                    if (!Application.CanStreamedLevelBeLoaded(map)) continue;

                    // The map stage owns 0.66 to 0.82 of the bar, split evenly across the maps.
                    SetLoadingStage("loading " + SceneFlow.PreviewFor(map).Name.ToLowerInvariant(),
                                    Mathf.Lerp(0.66f, 0.82f, m / (float)maps.Length));

                    AsyncOperation load = null;
                    try
                    {
                        load = SceneManager.LoadSceneAsync(map, LoadSceneMode.Additive);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[Splash] could not warm {map}: {e.Message}");
                    }

                    if (load == null) continue;

                    while (!load.isDone) yield return null;

                    var scene = SceneManager.GetSceneByName(map);
                    if (scene.IsValid() && scene.isLoaded)
                    {
                        // Capture while the scene still owns every dependency. The static cache
                        // keeps them reachable through the later MainMenu unused-asset sweep.
                        WarmAssetCache.CaptureLoadedAssets();

                        // Awake/OnEnable may already have run by the time an additive load reports
                        // done, so PreviewOnly is the real match-start guard. Deactivating the roots
                        // also prevents Start/Update work before the unload completes.
                        foreach (var root in scene.GetRootGameObjects())
                            if (root != null) root.SetActive(false);

                        var unload = SceneManager.UnloadSceneAsync(scene);
                        while (unload != null && !unload.isDone) yield return null;
                    }

                    yield return null;
                }
            }
            finally
            {
                MatchInstaller.PreviewOnly = previousPreviewOnly;
            }

            // The meshes and textures stay resident; only the scene graph went away. Do NOT
            // call Resources.UnloadUnusedAssets here, it would undo the entire stage.
        }

        /// <summary>
        /// Bake the boxes the menus and the HUD ask for, in the combinations they ask for them.
        /// </summary>
        private static void WarmSprites()
        {
            GodotTheme.WoodBox(UiTheme.WoodDark, UiTheme.WoodEdge);
            GodotTheme.WoodBox(UiTheme.WoodDeep, UiTheme.WoodEdge);
            GodotTheme.CardBox(UiTheme.WoodDark, UiTheme.WoodEdge);
            GodotTheme.CardBox(UiTheme.WoodDeep, UiTheme.WoodEdge);
            GodotTheme.ShadowBox();

            for (int radius = 2; radius <= 6; radius++) GodotTheme.Plain(radius);

            GodotTheme.Box(UiTheme.WoodDark, UiTheme.WoodEdge,
                           GodotTheme.WoodBorderWidth, GodotTheme.WoodCornerRadius);
            GodotTheme.Box(UiTheme.WoodDeep, UiTheme.WoodEdge, 3, 6);
            GodotTheme.Box(UiTheme.Amber, UiTheme.Ink, 3, 5);
            GodotTheme.Box(UiTheme.Amber, UiTheme.Ink, 3, 6);
            GodotTheme.Box(UiTheme.Ink, new Color(0, 0, 0, 0), 0, 4);
        }

        private bool PreloadComplete =>
            _assetsPreloaded && (_menu == null || _menu.progress >= 0.9f);

        /// <summary>
        /// ⚠️ THE CEILING WHILE THE SCREEN IS STILL UP. A bar drawn full over a screen that has
        /// not gone anywhere is the specific thing 🧑 photographed, so nothing inside the loop is
        /// allowed to write 1.0; only the line after the loop breaks does.
        /// </summary>
        private const float ProgressCeiling = 0.985f;

        /// <summary>
        /// Names the stage and moves the TARGET. ⚠️ It never writes `fillAmount`: the drawn value
        /// is eased toward this by <see cref="UpdateLoadingAnimation"/>, which is what stops the
        /// bar teleporting between stages and then standing still inside one.
        ///
        /// ⚠️ AND IT ONLY EVER GOES FORWARD. Two stages that settle out of order would otherwise
        /// drag the bar backwards, which reads as the load having failed and restarted.
        /// </summary>
        private void SetLoadingStage(string label, float progress)
        {
            _targetProgress = Mathf.Max(_targetProgress, Mathf.Clamp01(progress));
            if (_loadingLabel != null) _loadingLabel.text = _ownerLoading ? FriendlyLoadingStage(label) : label == "ready" ? "Ready" : "Getting ready";
        }

        private void UpdateLoadingAnimation()
        {
            if (_loadingMark != null)
                _loadingMark.localRotation = Settings.SettingsStore.Current.ReducedUiMotion ? Quaternion.identity
                    : Quaternion.Euler(0, 0, Mathf.Sin(_elapsed * 2.1f) * 8f);
            // ⚠️ THE MENU LOAD IS A CONTINUOUS SOURCE, so it is read every frame rather than being
            // announced once. `LoadSceneAsync` held at 0.9 is "done"; the divide normalises that.
            if (_assetsPreloaded && _menu != null)
            {
                _targetProgress = Mathf.Max(_targetProgress,
                    0.88f + Mathf.Clamp01(_menu.progress / 0.9f) * 0.04f);
            }

            // ⚠️⚠️ EASED, AND AT A RATE RATHER THAN OVER A DURATION. `Lerp` toward the target at
            // 4 per second covers the whole bar in well under the six seconds a cold boot takes,
            // and it cannot overshoot a target that stops moving.
            float ceiling = _leaving ? 1.0f : ProgressCeiling;
            _shownProgress = Mathf.Min(ceiling,
                Mathf.Lerp(_shownProgress, Mathf.Min(_targetProgress, ceiling),
                           1.0f - Mathf.Exp(-4.0f * Time.unscaledDeltaTime)));

            if (_loadingFill != null) _loadingFill.fillAmount = _shownProgress;

            if (_loadingDots == null) return;

            for (int i = 0; i < _loadingDots.Length; i++)
            {
                var dot = _loadingDots[i];
                if (dot == null) continue;

                float bounce = Mathf.Max(0.0f,
                    Mathf.Sin(_elapsed * 5.5f - i * 0.85f));
                dot.anchoredPosition = new Vector2((i - 1) * 24.0f, bounce * 8.0f);
                dot.localScale = Vector3.one * Mathf.Lerp(0.82f, 1.12f, bounce);
            }
        }

        private void SetFade(float alpha)
        {
            if (_fade == null) return;

            var c = _fade.color;
            c.a = Mathf.Clamp01(alpha);
            _fade.color = c;
        }

        /// <summary>Repaints the fade plate without touching how opaque it currently is.</summary>
        private void SetFadeColour(Color colour)
        {
            if (_fade == null) return;

            colour.a = _fade.color.a;
            _fade.color = colour;
        }

        private void BuildSurface()
        {
            if(OwnerUiTheme.Current.Background!=null && OwnerUiTheme.Current.Art(OwnerUiTheme.Piece.Logo)!=null)
            {BuildOwnerLoadingSurface();return;}
            if (Resources.Load<Texture2D>("UI/illustrations/street_key_art") != null)
            {
                BuildIllustratedSurface();
                return;
            }
            // ⚠️⚠️ A ROOT OBJECT, NOT A CHILD OF THE CONVERTED NODE, AND THAT IS THE WHOLE FIX
            // FOR "THE LOGO IS A POSTAGE STAMP". A nested Canvas inherits its PARENT's rect, and
            // the node the importer attaches this component to is a converted Control whose rect
            // is whatever the .tscn happened to leave it at before Godot's own layout pass ran.
            // Every child here then stretched to that little box: the video came out about a
            // hundred pixels square in the middle of a black screen, and the skip hint, anchored
            // to the box's bottom-right corner, landed beside it in the middle of the frame
            // instead of in the corner. Both were visible in the report's screenshot and both are
            // the same bug.
            //
            // A root Canvas has the SCREEN as its rect by definition, so the sting fills the
            // window whatever the converted scene does, and cannot regress if that scene is
            // re-imported.
            var canvasGo = new GameObject("SplashCanvas");
            _canvas = canvasGo;

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1.0f;
            AspectSafeCanvas.Apply(scaler);

            // ⚠️ THE CONVERTED SCENE'S OWN CONTENT IS HIDDEN, not drawn underneath. `SplashScreen.tscn`
            // authors a Letterbox panel, a Video rect, a Fade and a SkipHint, and every one of
            // those is reproduced here at the right size. Leaving the converted copies visible
            // put a second "press any key to skip" on screen in a different font, which is what
            // the report's two overlapping hints actually were.
            HideConvertedContent();

            // ⚠️⚠️ A WHITE BACKDROP UNDER THE VIDEO, AND IT WAS BLACK. The clip is 16:9, the
            // window may not be, and the sting itself is a black mark on white, so black bars
            // put a hard frame round a white picture on every shape that is not 16:9 (including
            // the short wide window 🧑 actually plays in, `CLAUDE.md` § 6.2b row 3). White makes
            // the letterbox invisible and makes the Unity splash, this screen and the hold after
            // it read as ONE frame rather than three. `docs/TODO.md` § 114.2.
            var bg = new GameObject("Backdrop");
            bg.transform.SetParent(canvasGo.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = Color.white;
            Stretch(bgImg.rectTransform);

            var fadeGo = new GameObject("Fade");
            fadeGo.transform.SetParent(canvasGo.transform, false);
            _fade = fadeGo.AddComponent<Image>();

            // ⚠️ WHITE, BECAUSE THE SCREEN BEFORE THIS ONE IS WHITE. `SetFadeColour` repaints it
            // black for the exit. See the fade note in `Run`.
            _fade.color = Color.white;
            _fade.raycastTarget = false;
            Stretch(_fade.rectTransform);

            BuildLoadingIndicator(canvasGo.transform);


        }

        private void BuildIllustratedSurface()
        {
            var canvas = MenuKit.BuildCanvas(null, "SplashCanvas");
            canvas.sortingOrder = 500;
            _canvas = canvas.gameObject;
            HideConvertedContent();
            var root = canvas.transform;
            var artGo = new GameObject("LoadingArtButton", typeof(RectTransform), typeof(RawImage));
            artGo.transform.SetParent(root, false);
            _surface = artGo.GetComponent<RawImage>();
            MenuKit.Stretch(_surface.rectTransform);
            _illustration = artGo.AddComponent<IllustratedBackdrop>();
            _surface.raycastTarget = true;
            _artButton = artGo.AddComponent<Button>();
            _artButton.targetGraphic = _surface;
            _artButton.transition = Selectable.Transition.None;
            _artButton.onClick.AddListener(() => ShowStory(true));

            var plate = StreetUi.Detail(root, "LoadingPaper", StreetGraphic.Surface.Card);
            MenuKit.Place(plate.rectTransform, new Vector2(0,0), new Vector2(330,130), new Vector2(560,174));
            var title = MenuKit.Label(root, "LOADING", 40, UiTheme.PaperInk,
                Vector2.zero, new Vector2(180,170), new Vector2(250,56), TextAnchor.MiddleLeft);
            MenuKit.Apply(title, MenuKit.Face.Accent);
            title.raycastTarget = false;
            _loadingLabel = MenuKit.Label(root, "Getting ready", 24, UiTheme.PaperInkSoft,
                Vector2.zero, new Vector2(278,119), new Vector2(440,34), TextAnchor.MiddleLeft);
            MenuKit.Read(_loadingLabel); _loadingLabel.raycastTarget = false;
            var hint = MenuKit.Label(root, "Click the artwork for stories and tips.", 22, UiTheme.PaperInk,
                Vector2.zero, new Vector2(308, 75), new Vector2(500,32), TextAnchor.MiddleLeft);
            MenuKit.Read(hint); hint.raycastTarget = false;

            var markGo = new GameObject("LoadingSlipper", typeof(RectTransform), typeof(RawImage));
            markGo.transform.SetParent(root, false);
            var mark = markGo.GetComponent<RawImage>();
            mark.texture = Resources.Load<Texture2D>("UI/brand/tsinelas_hit");
            mark.raycastTarget = false;
            _loadingMark = mark.rectTransform;
            float ratio = mark.texture != null ? mark.texture.width/(float)mark.texture.height : 1f;
            MenuKit.Place(_loadingMark, new Vector2(1,0), new Vector2(-116,126), new Vector2(116*ratio,116));

            _storyRoot = new GameObject("LoadingStoryRoot", typeof(RectTransform));
            _storyRoot.transform.SetParent(root, false);
            MenuKit.Stretch((RectTransform)_storyRoot.transform);
            var paper = StreetUi.Detail(_storyRoot.transform, "StoryPaper", StreetGraphic.Surface.Card);
            MenuKit.Place(paper.rectTransform, new Vector2(.5f,.5f), Vector2.zero, new Vector2(820,460));
            paper.raycastTarget = true;
            _storyText = MenuKit.Label(_storyRoot.transform, "", 28, UiTheme.PaperInk,
                new Vector2(.5f,.5f), new Vector2(0,20), new Vector2(700,300), TextAnchor.UpperLeft);
            MenuKit.Read(_storyText); _storyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _storyText.raycastTarget = false;
            var close = StreetUi.Button(_storyRoot.transform, "LoadingStoryClose", "", 24, StreetGraphic.Surface.Navigation);
            MenuKit.Place((RectTransform)close.transform, new Vector2(.5f,.5f), new Vector2(354,183), new Vector2(64,64));
            StreetUi.Icon(close.transform, StreetIcon.Glyph.Close, new Vector2(.5f,.5f), Vector2.zero, new Vector2(28,28));
            close.onClick.AddListener(() => ShowStory(false));
            var next = StreetUi.Button(_storyRoot.transform, "LoadingStoryNext", "", 24, StreetGraphic.Surface.Navigation);
            MenuKit.Place((RectTransform)next.transform, new Vector2(.5f,.5f), new Vector2(330,-170), new Vector2(72,58));
            StreetUi.Icon(next.transform, StreetIcon.Glyph.Next, new Vector2(.5f,.5f), Vector2.zero, new Vector2(32,32));
            next.onClick.AddListener(() => { _storyIndex++; RefreshStory(); });
            InputLayer.ScreenFocus.Install(_storyRoot);
            _storyRoot.SetActive(false);

            var fadeGo = new GameObject("LoadingFade", typeof(RectTransform), typeof(Image));
            fadeGo.transform.SetParent(root, false);
            _fade = fadeGo.GetComponent<Image>();
            _fade.color = UiTheme.Paper; _fade.raycastTarget = false;
            MenuKit.Stretch(_fade.rectTransform);
        }

        private void ShowStory(bool open)
        {
            if (_storyRoot == null) return;
            _storyRoot.SetActive(open);
            if (_artButton != null) _artButton.interactable = !open;
            if (_illustration != null) _illustration.Animating = !open;
            if (open) { RefreshStory(); _storyRoot.GetComponent<InputLayer.ScreenFocus>()?.Rebuild(); }
        }

        private void RefreshStory()
        {
            if (_storyText != null)
                _storyText.text = LoadingPresentation.Stories[_storyIndex % LoadingPresentation.Stories.Length];
        }

        private void BuildLoadingIndicator(Transform parent)
        {
            var panelGo = new GameObject("LoadingIndicator");
            panelGo.transform.SetParent(parent, false);
            var panelRt = panelGo.AddComponent<RectTransform>();
            Stretch(panelRt);

            var dotsGo = new GameObject("TansanDots");
            dotsGo.transform.SetParent(panelGo.transform, false);
            var dotsRt = dotsGo.AddComponent<RectTransform>();
            dotsRt.anchorMin = new Vector2(0.5f, 0.0f);
            dotsRt.anchorMax = new Vector2(0.5f, 0.0f);
            // ⚠️ THE STACK IS TRACK 110, LABEL 132, DOTS 190, MEASURED OFF EACH OTHER RATHER THAN
            // TYPED SEPARATELY. The track moved up and the two above it have to move with it or
            // the label draws through the bar.
            dotsRt.anchoredPosition = new Vector2(0.0f, 190.0f);
            dotsRt.sizeDelta = new Vector2(80.0f, 24.0f);

            _loadingDots = new RectTransform[3];
            for (int i = 0; i < _loadingDots.Length; i++)
            {
                var dotGo = new GameObject($"Tansan{i + 1}");
                dotGo.transform.SetParent(dotsGo.transform, false);
                var dot = dotGo.AddComponent<Image>();
                dot.sprite = GodotTheme.Plain(12);
                dot.color = new Color(0.03f, 0.03f, 0.03f, 0.88f - i * 0.16f);
                dot.raycastTarget = false;

                var rt = dot.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(16.0f, 16.0f);
                _loadingDots[i] = rt;
            }

            var labelGo = new GameObject("Status");
            labelGo.transform.SetParent(panelGo.transform, false);
            _loadingLabel = labelGo.AddComponent<Text>();

            // ⚠️⚠️ THE GAME'S OWN FONT, AND IT WAS `LegacyRuntime.ttf`. Every other word in the
            // build is Darumadrop One, so the one line a player reads before anything else was
            // the one line in a different typeface. `MenuKit.Font` falls back to the legacy font
            // itself if the asset is missing, so this is strictly the better of the two paths.
            _loadingLabel.font = MenuKit.Font;
            _loadingLabel.text = "preparing game";
            _loadingLabel.fontSize = MenuKit.MinReadableUnits;
            _loadingLabel.color = new Color(0.03f, 0.03f, 0.03f, 0.72f);
            _loadingLabel.alignment = TextAnchor.MiddleCenter;
            _loadingLabel.raycastTarget = false;

            var labelRt = _loadingLabel.rectTransform;
            labelRt.anchorMin = new Vector2(0.5f, 0.0f);
            labelRt.anchorMax = new Vector2(0.5f, 0.0f);
            labelRt.pivot = new Vector2(0.5f, 0.0f);
            labelRt.anchoredPosition = new Vector2(0.0f, 132.0f);
            labelRt.sizeDelta = new Vector2(560.0f, 34.0f);

            var trackGo = new GameObject("ProgressTrack");
            trackGo.transform.SetParent(panelGo.transform, false);
            var track = trackGo.AddComponent<Image>();
            track.sprite = GodotTheme.Plain(4);
            track.color = new Color(0.03f, 0.03f, 0.03f, 0.14f);
            track.raycastTarget = false;
            var trackRt = track.rectTransform;
            trackRt.anchorMin = new Vector2(0.5f, 0.0f);
            trackRt.anchorMax = new Vector2(0.5f, 0.0f);
            trackRt.pivot = new Vector2(0.5f, 0.0f);
            // ⚠️ 420 x 6 AT 110 UNITS UP, AND IT WAS 360 x 4 AT 90. A 4-unit rule on white reads
            // as a hairline rather than as a bar, and 90 units off the bottom of a 1080 canvas
            // puts it inside the bottom eight per cent, which on the short wide window 🧑 plays in
            // is the part of the frame closest to the taskbar. The track is the widest thing in
            // this group and everything else is centred on it.
            trackRt.anchoredPosition = new Vector2(0.0f, 110.0f);
            trackRt.sizeDelta = new Vector2(420.0f, 6.0f);

            var fillGo = new GameObject("ProgressFill");
            fillGo.transform.SetParent(trackGo.transform, false);
            _loadingFill = fillGo.AddComponent<Image>();
            _loadingFill.sprite = GodotTheme.Plain(4);
            _loadingFill.color = new Color(0.03f, 0.03f, 0.03f, 0.90f);
            _loadingFill.type = Image.Type.Filled;
            _loadingFill.fillMethod = Image.FillMethod.Horizontal;
            _loadingFill.fillOrigin = 0;
            _loadingFill.fillAmount = 0.0f;
            _loadingFill.raycastTarget = false;
            Stretch(_loadingFill.rectTransform);
        }

        /// <summary>
        /// ⚠️ THE CONVERTED SPLASH SCENE HAS A SECOND COPY OF EVERY ELEMENT ON THIS SCREEN, and
        /// it is not usable: `Letterbox`, `Video`, `Fade` and `SkipHint` come across as flat
        /// rects at whatever size the .tscn stored, because Godot's layout solves them at
        /// runtime and the converter cannot. Drawing them under the real ones put a second skip
        /// hint on screen in a second font.
        ///
        /// Hidden rather than destroyed, so re-importing the scene is still the way to change
        /// what it authors, and so nothing here depends on a node existing.
        /// </summary>
        private void HideConvertedContent()
        {
            foreach (string node in new[] { "Letterbox", "Video", "Fade", "SkipHint" })
            {
                var found = FindChild(transform, node);
                if (found != null) found.gameObject.SetActive(false);
            }
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root.name == name) return root;

            foreach (Transform child in root)
            {
                var hit = FindChild(child, name);
                if (hit != null) return hit;
            }

            return null;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void Leave()
        {
            if (_leaving) return;
            _leaving = true;

            // The sting goes with the picture. A skip that blacks the screen and leaves a chord
            // playing over the title menu is worse than no sting at all.
            BootSting.Stop();

            _preparedMenu?.CompleteBootLoading();
            ScreenTakeover.ConsumeEscape();
            Destroy(gameObject);
        }
    }
}
