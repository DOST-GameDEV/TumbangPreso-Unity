using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// The owner's animated HOME scene: Zack at Sa Bubong, a seamless 42 second loop, played in the
    /// hub's reserved full-bleed <see cref="TumpHub.Scene"/> layer over the live court.
    ///
    /// ⚠️⚠️ THE ANIMATION IS NOT MADE HERE. Its source is `ArtSource/home-scene/` (a Remotion project,
    /// hand-drawn in code, blocky and lineless, copied off team-zack.glb and team-sean.glb) and its
    /// design, research and timetable are `docs/reports/home-scene/README.md`. This component only
    /// plays the rendered file, so changing the animation is: edit the source, re-render, replace
    /// `Resources/UI/home/zack-home-loop.mp4`. Nothing in C# changes.
    ///
    /// ⚠️ WHY A VIDEO AND NOT THE LIVE COURT: the brief reserved this layer for "a simple animated scene
    /// like Valorant's home screens", and Valorant's are pre-rendered loops for the same reason, so
    /// the scene is art-directed frame by frame and costs one decoder rather than a second 3D camera.
    ///
    /// THE RULES IT KEEPS:
    ///   - ENVELOPED AT 16:9, NEVER STRETCHED, for `TumpHub.AdoptCourt`'s reason (`CLAUDE.md` § 6.2c
    ///     row 2). The loop was composed so his face, the lata and TUMP! sit inside x 240-1680 and
    ///     y 150-948 of the 1920x1080 frame, which is what survives the cover crop at 4:3 and on the
    ///     owner's 1600x680 window.
    ///   - THE POSTER FIRST. Frame 0 of the loop is shown from the first frame the hub draws, so the
    ///     screen is never empty while the decoder prepares, and it is what stays if the clip fails.
    ///   - REDUCED MOTION SHOWS THE POSTER AND NEVER STARTS THE DECODER
    ///     (`SettingsStore.Current.ReducedUiMotion`, as `HomeCourtScene` honours it).
    ///   - ⚠️ IT IS HOME'S AND ONLY HOME'S. Under every other hub screen it is HIDDEN and paused, so
    ///     the live court (the room's map, which the lobby is meant to show) is behind them.
    ///     Paused-but-visible froze whatever frame was up (a whip pan's blur, an impact frame)
    ///     behind the lobby. It resumes where it left off on returning HOME.
    ///   - NO AUDIO TRACK. The loop is silent by design; `docs/reports/home-scene/README.md` § 4 lists
    ///     the cue times if the hub ever wants to play its own sounds against it.
    /// </summary>
    public sealed class HubSceneVideo : MonoBehaviour
    {
        public const string ClipPath = "UI/home/zack-home-loop";
        public const string PosterPath = "UI/home/zack-home-poster";

        public VideoPlayer Player { get; private set; }
        public bool Prepared { get; private set; }
        public bool ShowingVideo => _image != null && _target != null && _image.texture == _target;

        private RawImage _image;
        private RenderTexture _target;
        private Texture2D _poster;

        /// <summary>Put the scene into the hub's background slot, above the court. Idempotent.</summary>
        public static HubSceneVideo Install(RectTransform scene)
        {
            if (scene == null) return null;
            var existing = scene.GetComponentInChildren<HubSceneVideo>(true);
            if (existing != null) return existing;
            var go = new GameObject("HomeSceneVideo", typeof(RectTransform), typeof(RawImage));
            go.layer = scene.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(scene, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            var fitter = go.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;
            return go.AddComponent<HubSceneVideo>();
        }

        private void Awake()
        {
            _image = GetComponent<RawImage>();
            _image.raycastTarget = false;
            _poster = Resources.Load<Texture2D>(PosterPath);
            _image.texture = _poster;
            _image.enabled = _poster != null;

            if (Settings.SettingsStore.Current.ReducedUiMotion) return;
            var clip = Resources.Load<VideoClip>(ClipPath);
            if (clip == null) return;

            _target = new RenderTexture(1920, 1080, 0) { name = "HomeSceneVideo" };
            Player = gameObject.AddComponent<VideoPlayer>();
            Player.playOnAwake = false;
            Player.isLooping = true;
            Player.skipOnDrop = true;
            Player.waitForFirstFrame = true;
            Player.renderMode = VideoRenderMode.RenderTexture;
            Player.targetTexture = _target;
            Player.audioOutputMode = VideoAudioOutputMode.None;
            Player.aspectRatio = VideoAspectRatio.FitOutside;
            Player.clip = clip;
            Player.prepareCompleted += OnPrepared;
            Player.errorReceived += OnError;
            Player.Prepare();
        }

        private void OnPrepared(VideoPlayer player)
        {
            Prepared = true;
            _image.texture = _target;
            _image.enabled = true;
            if (AtHome) player.Play();
            else _image.enabled = false;
        }

        private void OnError(VideoPlayer player, string message)
        {
            // ⚠️ A decoder that cannot play the file leaves the poster up rather than a black hole.
            Debug.LogWarning("[HubSceneVideo] " + message + " - showing the poster instead.");
            Prepared = false;
            _image.texture = _poster;
            _image.enabled = _poster != null;
        }

        private static bool AtHome => TumpHub.Current == null || TumpHub.Current.AtHome;

        private void Update()
        {
            // HOME only, in every state: the poster before the clip prepares, with reduced motion, and
            // after a decode failure too, or a hub opened straight into the lobby would cover its map.
            bool home = AtHome;
            _image.enabled = home && _image.texture != null;
            if (Player == null || !Prepared) return;
            if (home && !Player.isPlaying) Player.Play();
            else if (!home && Player.isPlaying) Player.Pause();
        }

        private void OnDestroy()
        {
            if (Player != null)
            {
                Player.prepareCompleted -= OnPrepared;
                Player.errorReceived -= OnError;
            }
            if (_target != null)
            {
                _target.Release();
                Destroy(_target);
            }
        }
    }
}
