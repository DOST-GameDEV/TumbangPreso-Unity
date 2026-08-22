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
        private RenderTexture _target;
        private Image _fade;
        private GameObject _canvas;
        private AsyncOperation _menu;
        private float _elapsed;
        private bool _leaving;
        private bool _assetsPreloaded;
        private bool _slowLoadReported;

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

            // ⚠️ ONLY IF THE EARLY HOOK DID NOT ALREADY START IT. On a normal launch the sting
            // is already playing across the Unity logo; this is the fallback for entering the
            // scene directly in the editor.
            if (_sting != null && !BootSting.Started)
            {
                var s = Settings.SettingsStore.Current;
                AudioSource.PlayClipAtPoint(_sting, Vector3.zero,
                                            Mathf.Clamp01(s.MasterVolume * s.SfxVolume));
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

                fade = Mathf.Clamp01(_elapsed / 0.35f);
                SetFade(1.0f - fade);

                bool presentationComplete = _clip == null
                    ? _elapsed >= 0.5f
                    : (_video.isPrepared && !_video.isPlaying && _elapsed > 0.5f);

                if (presentationComplete && PreloadComplete) break;

                if (!_slowLoadReported && _elapsed >= MaxWait)
                {
                    _slowLoadReported = true;
                    Debug.LogWarning("[Splash] preload exceeded six seconds; keeping the BH loading screen visible until it is genuinely ready.");
                }

                yield return null;
            }

            // Out on black, then hand over.
            for (float t = 0.0f; t < 0.22f; t += Time.unscaledDeltaTime)
            {
                SetFade(t / 0.22f);
                yield return null;
            }

            SetFade(1.0f);
            Leave();
        }

        /// <summary>
        /// ⚠️ THE MENU AND ASSETS ARE PRELOADED. `allowSceneActivation = false` holds the menu at
        /// 90% until the animation is over, and shaders/roster assets are pre-warmed so clicking
        /// Single Player or Character Select is instant with zero stutter.
        /// </summary>
        private void BeginPreload()
        {
            if (Application.CanStreamedLevelBeLoaded(SceneFlow.MainMenu))
            {
                _menu = SceneManager.LoadSceneAsync(SceneFlow.MainMenu);
                if (_menu != null) _menu.allowSceneActivation = false;
            }

            StartCoroutine(PreloadGameAssets());
        }

        private IEnumerator PreloadGameAssets()
        {
            // 1. Warm up shaders across all materials
            Shader.WarmupAllShaders();
            yield return null;

            // 2. Pre-load RosterBook (models, rigs, materials, clips, pets)
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
            try
            {
                var allAudio = Resources.LoadAll<AudioClip>("");
                _ = allAudio;
            }
            catch (System.Exception) { }
            yield return null;

            // 4. Pre-load Settings & Roster tables
            _ = Settings.SettingsStore.Current;
            _ = Roster.People;
            _ = Roster.ClassicPeople;
            _ = Roster.HeroPeople;
            _ = Roster.Cans;
            _ = Roster.Slippers;
            yield return null;

            _assetsPreloaded = true;
        }

        private bool PreloadComplete =>
            _assetsPreloaded && (_menu == null || _menu.progress >= 0.9f);

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

            var hintGo = new GameObject("SkipHint");
            hintGo.transform.SetParent(canvasGo.transform, false);

            // ⚠️ THE GAME'S OWN FACE, NOT LegacyRuntime. Godot sets Darumadrop project-wide, so
            // every string in the game is that face; the built-in fallback is the single most
            // obvious tell that a screen was rebuilt rather than converted.
            var hint = hintGo.AddComponent<Text>();
            hint.font = MenuKit.Font;
            hint.text = "loading game assets...";
            hint.fontSize = 20;
            hint.color = new Color(1, 1, 1, 0.45f);
            hint.alignment = TextAnchor.LowerRight;
            hint.raycastTarget = false;

            // The .tscn's own numbers: -320/-64 to -32/-28 off the bottom-right corner.
            var hintRt = hint.rectTransform;
            hintRt.anchorMin = Vector2.one;
            hintRt.anchorMax = Vector2.one;
            hintRt.pivot = Vector2.one;
            hintRt.anchoredPosition = new Vector2(-32.0f, -28.0f);
            hintRt.sizeDelta = new Vector2(288.0f, 36.0f);

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
