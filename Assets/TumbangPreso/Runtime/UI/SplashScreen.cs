using System.Collections;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The BH Studios boot sting. Plays on **every** launch, then hands off to the title.
    ///
    /// ⚠️ EVERY TIME IS LITERAL. There is no "seen it already" flag and no skip-on-second-launch.
    /// That was an explicit call and it is not an oversight to fix.
    ///
    /// ⚠️⚠️ THIS IS ALSO THE BOOT LOADING SCREEN, SO INPUT NEVER SKIPS IT. Earlier builds let a
    /// buffered click jump straight to the menu while shaders, audio and both rosters were still
    /// cold. That merely moved the wait to the first PLAY or CHARACTER press, where it looked
    /// like the UI had frozen. The sting now stays up until both its presentation and the preload
    /// barrier are complete.
    ///
    /// ⚠️⚠️ THE CLIP AND THE STING ARE ASSIGNED BY THE IMPORTER, NOT BY HAND. They are
    /// serialised fields and for the whole first conversion nothing ever set them: the component
    /// was attached, the coroutine ran, both references were null, and every launch showed three
    /// seconds of black. `TscnUiImporter.BindSplash` wires them now, so the failure cannot come
    /// back through somebody rebuilding the scene.
    ///
    /// ⚠️ THE MENU LOADS UNDERNEATH. `LoadSceneAsync` with activation held off runs while the
    /// animation plays, so the handoff is instant instead of a second black frame at the end of
    /// a three second clip.
    /// </summary>
    public sealed class SplashScreen : MonoBehaviour
    {
        /// <summary>When crossed, log that loading is slow but keep the barrier intact.</summary>
        public const float MaxWait = 6.0f;

        [SerializeField] private VideoClip _clip;
        [SerializeField] private AudioClip _sting;

        private VideoPlayer _video;
        private RawImage _surface;

        /// <summary>The key art behind the sting. See <see cref="BuildSplashArt"/>.</summary>
        private RawImage _art;
        private RenderTexture _target;
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
        private float _loadingProgress;

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

            public static void CaptureLoadedAssets()
            {
                foreach (Object asset in Resources.FindObjectsOfTypeAll<Object>())
                {
                    if (!ShouldRetain(asset) || !EntityIds.Add(asset.GetEntityId())) continue;
                    Assets.Add(asset);
                }
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
            BuildSurface();
            BeginPreload();
            // ⚠️ THE MENU IS ACTIVATED ONLY AFTER THE ACCOUNT BARRIER SETTLES. There is no
            // prompt and no account form here: a fresh player signs in anonymously while the
            // existing studio/loading screen is already doing its work, and an unreachable
            // service settles to the local profile inside PlayerAccount's bounded budget.
            var accountBarrier = GameServices.Account?.InitializeAsync();

            // ⚠️ ONLY IF THE EARLY HOOK DID NOT ALREADY START IT. On a normal launch the sting
            // is already playing across the Unity logo; this is the fallback for entering the
            // scene directly in the editor.
            if (_sting != null && !BootSting.Started)
            {
                var s = Settings.SettingsStore.Current;
                AudioSource.PlayClipAtPoint(_sting, Vector3.zero,
                                            Mathf.Clamp01(s.SfxGain));
            }

            if (_clip != null)
            {
                _video.clip = _clip;
                _video.Play();
            }
            else
            {
                Debug.LogWarning("[Splash] no video clip bound; run Tumbang Preso > Import Godot UI.");
            }

            // Start opaque and fade the black out, so a slow first decode reads as a deliberate
            // fade-in rather than as a frozen black screen.
            float fade = 0.0f;

            while (!_leaving)
            {
                _elapsed += Time.unscaledDeltaTime;
                UpdateLoadingAnimation();

                fade = Mathf.Clamp01(_elapsed / 0.35f);
                SetFade(1.0f - fade);

                bool presentationComplete = _clip == null
                    ? _elapsed >= 0.5f
                    : (_video.isPrepared && !_video.isPlaying && _elapsed > 0.5f);

                // ⚠️⚠️ THE VIDEO SURFACE IS SWITCHED OFF THE FRAME THE STING ENDS, AND WITHOUT
                // THIS THE ART IS NEVER SEEN. A `VideoPlayer` that has finished leaves its LAST
                // FRAME in the render texture, so the `RawImage` above the art keeps drawing a
                // frozen frame of the studio logo for the whole remainder of the preload. That
                // is the part that runs past six seconds, and it is the entire window this art
                // exists to fill. `_surface` rather than the texture, so nothing is reallocated.
                if (presentationComplete && _art != null && _surface != null && _surface.enabled)
                    _surface.enabled = false;

                bool accountReady = accountBarrier == null || accountBarrier.IsCompleted;
                if (presentationComplete && PreloadComplete && accountReady) break;

                if (!_slowLoadReported && _elapsed >= MaxWait)
                {
                    _slowLoadReported = true;
                    Debug.LogWarning("[Splash] preload exceeded six seconds; keeping the BH loading screen visible until it is genuinely ready.");
                }

                yield return null;
            }

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

            // Out on black, then hand over.
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
        /// preload operation; only then is its activation held until the sting finishes.
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
        private IEnumerator PreloadGameAssets()
        {
            // 1. Warm up shaders across all materials
            SetLoadingStage("preparing shaders", 0.04f);
            yield return null;
            Shader.WarmupAllShaders();
            yield return null;

            // 2. Pre-load RosterBook (models, rigs, materials, clips, pets)
            SetLoadingStage("loading characters", 0.14f);
            var book = RosterBook.Load();
            if (book != null)
            {
                if (book.People != null)
                {
                    foreach (var p in book.People)
                    {
                        if (p != null)
                        {
                            _ = p.Model;
                            _ = p.Clips;
                            _ = p.Palette;
                            _ = p.PetModel;
                        }
                    }
                }

                if (book.Cans != null)
                {
                    foreach (var c in book.Cans)
                    {
                        if (c != null) _ = c.Model;
                    }
                }

                if (book.Slippers != null)
                {
                    foreach (var s in book.Slippers)
                    {
                        if (s != null) _ = s.Model;
                    }
                }
            }
            yield return null;

            // 3. Pre-load Audio clips and sound resources
            SetLoadingStage("loading audio", 0.27f);
            try
            {
                var allAudio = Resources.LoadAll<AudioClip>("");
                _ = allAudio;
            }
            catch (System.Exception) { }
            yield return null;

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

            // 6. Every procedurally baked UI sprite.
            //
            // ⚠️⚠️ THESE ARE PAINTED PIXEL BY PIXEL ON FIRST USE AND THAT IS NOT FREE. Each
            // `GodotTheme.Box` rasterises a rounded, bordered texture and uploads it, and the
            // HUD asks for a fresh one for every distinct fill, border, width and radius the
            // frame it is built. Baking them here moves the whole cost behind the logo.
            SetLoadingStage("building interface", 0.52f);
            WarmSprites();
            yield return null;

            // 7. Every ability glyph.
            SetLoadingStage("loading abilities", 0.61f);
            foreach (AbilityGlyph glyph in System.Enum.GetValues(typeof(AbilityGlyph)))
                AbilityIcons.For(glyph);
            yield return null;

            // 8. Both arenas, as a dependency load rather than a scene load.
            //
            // ⚠️⚠️ THIS IS THE ONE THAT ACTUALLY FIXES THE STUTTER ON PLAY. Only the menu was
            // being pre-loaded, so the map, its materials and its meshes were read off disk on
            // the frame the player pressed the button. `LoadSceneAsync` cannot hold two scenes
            // at 90% at once without activating one of them, so the arena is warmed through its
            // ASSETS instead: everything the scene references is what costs the time, not the
            // scene graph.
            //
            // ⚠️ BOTH MAPS, NOT THE SELECTED ONE. Nothing has been selected yet at boot, and the
            // player can change the map on the setup screen without ever returning here.
            yield return WarmMapAssets();

            // 9. The hero ability layer.
            //
            // ⚠️ CONSTRUCTING EVERY KIT TOUCHES EVERY ABILITY OBJECT, its strings and its glyph,
            // which is what the character select and the HUD read the instant Hero Strike opens.
            SetLoadingStage("preparing hero abilities", 0.84f);
            Visual.AbilityVfx.Warmup();
            foreach (string heroId in Roster.HeroPeople != null
                         ? HeroIdsFrom(Roster.HeroPeople)
                         : new string[0])
            {
                var kit = Abilities.HeroAbilitySystem.CreateKitFor(heroId);
                _ = kit?.Skill1?.Name;
                _ = kit?.Skill2?.Name;
                _ = kit?.Ultimate?.Name;
            }
            yield return null;

            WarmAssetCache.CaptureLoadedAssets();
            Debug.Log($"[Splash] preload retained {WarmAssetCache.Count} assets in memory.");

            // This must remain the final scene operation in the preload chain. Once activation is
            // held, Unity will not complete an additive load or unload queued behind this one.
            SetLoadingStage("opening main menu", 0.92f);
            BeginMenuPreload();
            _assetsPreloaded = true;
        }

        private static string[] HeroIdsFrom(System.Collections.Generic.IReadOnlyList<RosterEntry> people)
        {
            var ids = new string[people.Count];
            for (int i = 0; i < people.Count; i++) ids[i] = people[i] != null ? people[i].Id : null;
            return ids;
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
            string[] maps = { SceneFlow.Eskinita, SceneFlow.BayanPlaza };

            // MatchInstaller's setup happens in Start(). Mark these loads as previews before they
            // are requested so no seats, HUD, services or match state are created behind the sting.
            bool previousPreviewOnly = MatchInstaller.PreviewOnly;
            MatchInstaller.PreviewOnly = true;

            try
            {
                foreach (string map in maps)
                {
                    if (!Application.CanStreamedLevelBeLoaded(map)) continue;

                    SetLoadingStage(map == SceneFlow.Eskinita
                        ? "loading eskinita"
                        : "loading bayan plaza",
                        map == SceneFlow.Eskinita ? 0.68f : 0.76f);

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

        private void SetLoadingStage(string label, float progress)
        {
            _loadingProgress = Mathf.Clamp01(progress);
            if (_loadingLabel != null) _loadingLabel.text = label;
            if (_loadingFill != null) _loadingFill.fillAmount = _loadingProgress;
        }

        private void UpdateLoadingAnimation()
        {
            if (_assetsPreloaded && _menu != null)
            {
                _loadingProgress = Mathf.Max(_loadingProgress,
                    0.92f + Mathf.Clamp01(_menu.progress / 0.9f) * 0.08f);
                if (_loadingFill != null) _loadingFill.fillAmount = _loadingProgress;
            }

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

        private void BuildSurface()
        {
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

            // ⚠️ A BLACK BACKDROP UNDER THE VIDEO. The clip is 16:9 and the window may not be,
            // so without this the player sees whatever the camera happened to be rendering
            // through the letterbox bars on the very first frame of the game.
            var bg = new GameObject("Backdrop");
            bg.transform.SetParent(canvasGo.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = Color.black;
            Stretch(bgImg.rectTransform);

            BuildSplashArt(canvasGo.transform);

            var surfaceGo = new GameObject("Video");
            surfaceGo.transform.SetParent(canvasGo.transform, false);
            _surface = surfaceGo.AddComponent<RawImage>();
            Stretch(_surface.rectTransform);

            var fadeGo = new GameObject("Fade");
            fadeGo.transform.SetParent(canvasGo.transform, false);
            _fade = fadeGo.AddComponent<Image>();
            _fade.color = Color.black;
            _fade.raycastTarget = false;
            Stretch(_fade.rectTransform);

            BuildLoadingIndicator(canvasGo.transform);

            _target = new RenderTexture(1280, 720, 0);
            _surface.texture = _target;

            _video = canvasGo.AddComponent<VideoPlayer>();
            _video.playOnAwake = false;
            _video.isLooping = false;
            _video.renderMode = VideoRenderMode.RenderTexture;
            _video.targetTexture = _target;
            _video.audioOutputMode = VideoAudioOutputMode.None; // the sting is its own cue

            // ⚠️ STRETCH, MATCHING `expand = true` ON THE .tscn's VideoStreamPlayer. The clip and
            // the reference resolution are both 16:9, so on any ordinary window this is identical
            // to fitting inside; the black Backdrop above is what covers the difference on a
            // window that is not.
            _video.aspectRatio = VideoAspectRatio.Stretch;
        }

        /// <summary>
        /// The key art the player looks at while the game finishes loading.
        ///
        /// ⚠️⚠️ IT SITS UNDER THE VIDEO AND IS REVEALED WHEN THE VIDEO ENDS, which is the whole
        /// point of putting it here rather than on the menu. The studio sting is about half a
        /// second; the preload behind it routinely runs past six seconds and logs when it does.
        /// **Everything after the sting used to be a black screen with three dots on it**, which
        /// is the longest single stretch of the game a first-time player sees and the least of it.
        ///
        /// ⚠️ `RawImage` AND `Resources.Load<Texture2D>`, NOT A SPRITE. A sprite needs the
        /// importer's texture type set on the `.meta`, which is a file nobody edits by hand and
        /// which a re-import can reset; a `RawImage` draws a plain `Texture2D` with whatever
        /// import settings the file arrived with. One less thing that can silently come back as a
        /// magenta rectangle.
        ///
        /// ⚠️⚠️ AND IT COVERS RATHER THAN STRETCHES. The art is 1267x697, about 1.82:1, and the
        /// game ships at nine shapes from 4:3 to 21:9. `Stretch` would distort the cast on every
        /// one of them that is not 1.82; `AspectRatioFitter.EnvelopeParent` fills the window and
        /// crops the overflow instead, which is what the black `Backdrop` above already assumes.
        ///
        /// ⚠️ A MISSING FILE IS SILENT AND LEAVES THE BLACK BACKDROP. `Resources.Load` answers
        /// null rather than throwing, and a boot screen is the worst possible place to take an
        /// exception, so the art is treated as decoration the boot can proceed without.
        /// </summary>
        private void BuildSplashArt(Transform parent)
        {
            var art = Resources.Load<Texture2D>("UI/splash_art");
            if (art == null)
            {
                Debug.LogWarning("[Splash] no splash art at Resources/UI/splash_art; " +
                                 "the loading screen falls back to the black backdrop.");
                return;
            }

            var go = new GameObject("SplashArt");
            go.transform.SetParent(parent, false);

            _art = go.AddComponent<RawImage>();
            _art.texture = art;
            _art.raycastTarget = false;
            Stretch(_art.rectTransform);

            var fitter = go.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            fitter.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = art.width / (float)art.height;
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
            dotsRt.anchoredPosition = new Vector2(0.0f, 152.0f);
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
            _loadingLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _loadingLabel.text = "preparing game";
            _loadingLabel.fontSize = 18;
            _loadingLabel.color = new Color(0.03f, 0.03f, 0.03f, 0.72f);
            _loadingLabel.alignment = TextAnchor.MiddleCenter;
            _loadingLabel.raycastTarget = false;

            var labelRt = _loadingLabel.rectTransform;
            labelRt.anchorMin = new Vector2(0.5f, 0.0f);
            labelRt.anchorMax = new Vector2(0.5f, 0.0f);
            labelRt.pivot = new Vector2(0.5f, 0.0f);
            labelRt.anchoredPosition = new Vector2(0.0f, 108.0f);
            labelRt.sizeDelta = new Vector2(420.0f, 30.0f);

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
            trackRt.anchoredPosition = new Vector2(0.0f, 90.0f);
            trackRt.sizeDelta = new Vector2(360.0f, 4.0f);

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

            if (_target != null)
            {
                _video.targetTexture = null;
                _target.Release();
            }

            // ⚠️ THE CANVAS IS A ROOT OBJECT NOW, so the scene change does NOT take it with it
            // on the frame it is torn down; a stale black plate over the title menu is exactly
            // the failure this file exists to avoid.
            if (_canvas != null) Destroy(_canvas);

            if (_menu != null)
            {
                _menu.allowSceneActivation = true;
                return;
            }

            SceneFlow.Go(SceneFlow.MainMenu);
        }
    }
}
