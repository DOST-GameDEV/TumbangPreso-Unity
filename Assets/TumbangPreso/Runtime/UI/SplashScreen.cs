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
    /// never fires on some driver, the game still reaches the menu. A boot screen is exactly
    /// the place where a silent failure costs the whole session.
    ///
    /// ⚠️ THE AUDIO IS A SEPARATE STREAM FROM THE VIDEO, and that is inherited rather than
    /// accidental. Godot's only core codec is Theora, so the clip was exported with no audio
    /// track and the sting played as a normal sound cue. Unity could carry audio in the video,
    /// but keeping them separate means the sting still respects the volume sliders like every
    /// other sound, which is what a player expects when they turn the game down.
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
        private float _elapsed;
        private bool _leaving;

        private void Start() => StartCoroutine(Run());

        private IEnumerator Run()
        {
            BuildSurface();

            if (_sting != null)
            {
                var s = SettingsStoreVolume();
                AudioSource.PlayClipAtPoint(_sting, Vector3.zero, s);
            }

            if (_clip != null)
            {
                _video.clip = _clip;
                _video.Play();
            }

            while (!_leaving)
            {
                _elapsed += Time.unscaledDeltaTime;

                if (_elapsed >= SkipArmedAfter && AnyInput()) break;

                // Exit 1: the clip finished honestly.
                if (_clip != null && _video.isPrepared && !_video.isPlaying && _elapsed > 0.5f) break;

                // Exit 2: the watchdog. See the class note.
                if (_elapsed >= MaxWait) break;

                yield return null;
            }

            Leave();
        }

        private static float SettingsStoreVolume()
        {
            var s = Settings.SettingsStore.Current;
            return Mathf.Clamp01(s.MasterVolume * s.SfxVolume);
        }

        private static bool AnyInput() =>
            Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);

        private void BuildSurface()
        {
            var canvasGo = new GameObject("SplashCanvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

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

            if (_target != null)
            {
                _video.targetTexture = null;
                _target.Release();
            }

            SceneFlow.Go(SceneFlow.MainMenu);
        }
    }
}
