using System.Collections;
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
    /// ⚠️⚠️ IT MUST BE SKIPPABLE AND IT MUST NEVER STRAND THE PLAYER. Two independent exits,
    /// because a splash that hangs is worse than no splash at all:
    ///   1. any key, click or button skips immediately, and
    ///   2. <see cref="MaxWait"/> is a hard watchdog.
    /// If the video fails to decode, the file is missing from a build, or the finished callback
    /// never fires on some driver, the game still reaches the menu.
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
        /// <summary>The clip is 3.0 s; this is that plus slack for a slow first-frame decode.</summary>
        public const float MaxWait = 6.0f;

        /// <summary>
        /// ⚠️ INPUT IS IGNORED FOR A MOMENT so a keypress still in the buffer from launching
        /// the game does not skip the sting before it is even visible.
        /// </summary>
        public const float SkipArmedAfter = 0.35f;

        [SerializeField] private VideoClip _clip;
        [SerializeField] private AudioClip _sting;

        private VideoPlayer _video;
        private RawImage _surface;
        private RenderTexture _target;
        private Image _fade;
        private AsyncOperation _menu;
        private float _elapsed;
        private bool _leaving;

        private void Start() => StartCoroutine(Run());

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

                if (_elapsed >= SkipArmedAfter && AnyInput()) break;

                // Exit 1: the clip finished honestly.
                if (_clip != null && _video.isPrepared && !_video.isPlaying && _elapsed > 0.5f) break;

                // Exit 2: the watchdog. See the class note.
                if (_elapsed >= MaxWait) break;

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
        /// ⚠️ THE MENU IS LOADED BUT NOT ACTIVATED. `allowSceneActivation = false` holds it at
        /// 90% until the animation is over, so the whole load happens behind the sting instead
        /// of after it.
        /// </summary>
        private void BeginPreload()
        {
            if (!Application.CanStreamedLevelBeLoaded(SceneFlow.MainMenu)) return;

            _menu = SceneManager.LoadSceneAsync(SceneFlow.MainMenu);
            if (_menu != null) _menu.allowSceneActivation = false;
        }

        private static bool AnyInput() =>
            Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);

        private void SetFade(float alpha)
        {
            if (_fade == null) return;

            var c = _fade.color;
            c.a = Mathf.Clamp01(alpha);
            _fade.color = c;
        }

        private void BuildSurface()
        {
            var canvasGo = new GameObject("SplashCanvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

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

            var hint = hintGo.AddComponent<Text>();
            hint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hint.text = "press any key to skip";
            hint.fontSize = 20;
            hint.color = new Color(1, 1, 1, 0.45f);
            hint.alignment = TextAnchor.LowerRight;
            hint.raycastTarget = false;

            var hintRt = hint.rectTransform;
            hintRt.anchorMin = Vector2.one;
            hintRt.anchorMax = Vector2.one;
            hintRt.pivot = Vector2.one;
            hintRt.anchoredPosition = new Vector2(-32.0f, -28.0f);
            hintRt.sizeDelta = new Vector2(288.0f, 36.0f);

            _target = new RenderTexture(1280, 720, 0);
            _surface.texture = _target;

            _video = gameObject.AddComponent<VideoPlayer>();
            _video.playOnAwake = false;
            _video.isLooping = false;
            _video.renderMode = VideoRenderMode.RenderTexture;
            _video.targetTexture = _target;
            _video.audioOutputMode = VideoAudioOutputMode.None; // the sting is its own cue
            _video.aspectRatio = VideoAspectRatio.FitInside;
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

            if (_menu != null)
            {
                _menu.allowSceneActivation = true;
                return;
            }

            SceneFlow.Go(SceneFlow.MainMenu);
        }
    }
}
