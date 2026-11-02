using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// The owner's animated HOME scenes: one per featured hero (Zack at BGC, a 45 s film into a held idle;
    /// Phaister and Nemu under the tulay, a 54 s loop: a loop is as long as its story needs, and the player
    /// loops whatever length it is given), ONE of them picked at random each time the hub is built, and
    /// played in the hub's reserved full-bleed <see cref="TumpHub.Scene"/> layer over the live court.
    ///
    /// ⚠️⚠️ RANDOM, NOT THE SELECTED OR FAVOURITE HERO. 🧑 2026-09-24: *"i decided to make diff character
    /// lobby screens and the current one will js be random"*, *"make it random which one shows up"*. The
    /// pick is <see cref="Pick"/> over <see cref="Heroes"/>, limited to the heroes whose clip is actually
    /// in Resources, so adding a hero's loop is: render it, ship `UI/home/HERO-home-loop.mp4` and
    /// `HERO-home-poster.png`, add the id to <see cref="Heroes"/>. A hero listed without a clip is
    /// skipped, and if nothing resolves at all it falls back to Zack's, the first loop that shipped.
    /// Each loop's design is in `docs/reports/home-scene/` (README.md for Zack, phaister.md for Phaister).
    ///
    /// ⚠️⚠️ THE ANIMATION IS NOT MADE HERE. Its source is `ArtSource/home-scene/` (a Remotion project with
    /// the game's own models on a drawn set) and its design, research and timetable are
    /// `docs/reports/home-scene/zack-bgc.md`. This component only plays the rendered files, so changing
    /// the animation is: edit the source, re-render, run `scripts/ship-zack.mjs`. Nothing in C# changes.
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
    ///     behind the lobby. It resumes where it left off on returning HOME. ⚠️ A POPUP OVER HOME
    ///     (the hamburger MENU) is still HOME: it keeps playing under the popup's scrim.
    ///   - ⚠️ A FILM PLAYS ONCE, THEN THE IDLE HOLDS (🧑 2026-11-01). A hero may ship
    ///     `UI/home/HERO-home-intro` beside the loop. It plays through one time, its last frame stays up
    ///     while the loop starts on its own decoder, and the picture swaps on the loop's first decoded
    ///     frame, so it never restarts and never shows a seam. The frame after the film's last is the
    ///     loop's frame 0 (`ArtSource/home-scene/src/bgc/time.ts`). It plays once per run of the game for
    ///     each hero. A hero with no intro loops as before. The poster is the loop's own frame 0 (reduced
    ///     motion holds that); `HERO-home-intro-poster` covers the film's preparation.
    ///   - NO AUDIO TRACK. The loop is silent by design; `docs/reports/home-scene/README.md` § 4 lists
    ///     the cue times if the hub ever wants to play its own sounds against it.
    /// </summary>
    public sealed partial class HubSceneVideo : MonoBehaviour
    {
        /// <summary>Zack's loop: the fallback when a picked hero's files are missing.</summary>
        public const string ClipPath = "UI/home/zack-home-loop";
        public const string PosterPath = "UI/home/zack-home-poster";

        /// <summary>Every hero with a HOME loop, in the order they shipped. The random pick draws from these.</summary>
        public static readonly string[] Heroes = { "zack", "phaister" };

        public static string ClipPathFor(string hero) => "UI/home/" + hero + "-home-loop";
        public static string PosterPathFor(string hero) => "UI/home/" + hero + "-home-poster";
        public static string IntroPathFor(string hero) => "UI/home/" + hero + "-home-intro";
        public static string IntroPosterPathFor(string hero) => "UI/home/" + hero + "-home-intro-poster";

        /// <summary>
        /// Test hook: when set, the next installed scene plays this hero instead of rolling. Never set by
        /// the game; tests clear it in their set up and tear down.
        /// </summary>
        public static string ForcedHero;

        /// <summary>The hero whose loop this scene is playing.</summary>
        public string Hero { get; private set; }

        /// <summary>
        /// The random pick, with the roll injected so a test can ask for every outcome. Only heroes whose
        /// clip loads are candidates; with none, Zack.
        /// </summary>
        public static string Pick(System.Func<int, int> roll, System.Func<string, bool> hasClip = null)
        {
            hasClip = hasClip ?? (h => LoadClip(h) != null);
            var candidates = new System.Collections.Generic.List<string>();
            foreach (var h in Heroes)
                if (hasClip(h)) candidates.Add(h);
            if (candidates.Count == 0) return "zack";
            return candidates[Mathf.Clamp(roll(candidates.Count), 0, candidates.Count - 1)];
        }

        /// <summary>The decoder on screen: the film while it plays, the held idle after it.</summary>
        public VideoPlayer Player { get; private set; }

        /// <summary>True on the looping clip: from the first frame of the held idle on, and for a hero with no film.</summary>
        public bool HoldingIdle => Player != null && Player.isLooping;
        public bool Prepared { get; private set; }
        public bool FirstFrameReady { get; private set; }
        public bool ShowingVideo => _image != null && _target != null && _image.texture == _target;

        private RawImage _image;
        private RenderTexture _target;
        private Texture2D _poster;
        private VideoClip _clip, _film;
        private Texture2D _filmPoster;
        private VideoPlayer _next;
        private RenderTexture _nextTarget;
        private bool _nextPreparing, _nextPrepared, _filmEnded;
        private bool _preloading, _failed;
        private MapPreviewSurface _court;

        /// <summary>Put the scene into the hub's background slot, above the court. Idempotent.</summary>
        public static HubSceneVideo Install(RectTransform scene, MapPreviewSurface court = null)
        {
            if (scene == null) return null;
            var existing = scene.GetComponentInChildren<HubSceneVideo>(true);
            if (existing != null) { existing.BindCourt(court); return existing; }
            var warm = TakePreloaded(scene);
            if (warm != null) { warm.BindCourt(court); return warm; }
            var video = Create(scene, false);
            video.BindCourt(court);
            return video;
        }

        private void BindCourt(MapPreviewSurface court)
        {
            if (court == null) return;
            if (_court != null && _court != court) _court.SetRenderingEnabled(true);
            _court = court;
            UpdateCourtVisibility();
        }

        private void UpdateCourtVisibility()
        {
            if (_court == null) return;
            bool covered = !_preloading && _image != null && _image.isActiveAndEnabled &&
                _image.texture != null && _image.color.a >= .999f && _image.canvasRenderer.GetInheritedAlpha() >= .999f;
            _court.SetRenderingEnabled(!covered);
        }

        private void OnDisable()
        {
            if (_court != null) _court.SetRenderingEnabled(true);
        }

        private static HubSceneVideo Create(RectTransform scene, bool preloading, string hero = null)
        {
            var go = new GameObject("HomeSceneVideo", typeof(RectTransform), typeof(RawImage));
            go.SetActive(false);
            if (scene != null) go.layer = scene.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(scene, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            var fitter = go.AddComponent<AspectRatioFitter>();
            fitter.enabled = !preloading;
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;
            var video = go.AddComponent<HubSceneVideo>();
            video._preloading = preloading; video.Hero = hero;
            if (preloading) DontDestroyOnLoad(go);
            go.SetActive(true);
            return video;
        }

        private void Awake()
        {
            _image = GetComponent<RawImage>();
            _image.raycastTarget = false;
            if (string.IsNullOrEmpty(Hero)) Hero = HeroForThisRun();
            // ⚠️ POSTER AND CLIP ARE ONE HERO'S PAIR. The poster is that loop's own frame 0, so a fallback
            // takes both of Zack's rather than showing one hero's poster before another hero's clip.
            _clip = LoadClip(Hero);
            _poster = LoadPoster(Hero);
            if (_clip == null || _poster == null)
            {
                Hero = "zack";
                _clip = LoadClip(Hero);
                _poster = LoadPoster(Hero);
            }
            _film = LoadFilm(Hero);
            _filmPoster = _film != null ? LoadFilmPoster(Hero) : null;
            _image.texture = FirstPicture;
            _image.enabled = _image.texture != null;

            if (!Settings.SettingsStore.Current.ReducedUiMotion) PreparePlayback();
        }

        private void PreparePlayback()
        {
            if (Player != null || _clip == null || _failed) return;

            _target = new RenderTexture(1920, 1080, 0) { name = "HomeSceneVideo" };
            Player = NewPlayer(_film != null ? _film : _clip, _film == null, _target);
            Player.prepareCompleted += OnPrepared;
            Player.frameReady += OnFirstFrame;
            Player.sendFrameReadyEvents = true;
            Player.errorReceived += OnError;
            if (_film != null) Player.loopPointReached += OnFilmEnded;
            ResetWatchdog();
            Player.Prepare();
        }

        private VideoPlayer NewPlayer(VideoClip clip, bool loops, RenderTexture target)
        {
            var player = gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.isLooping = loops;
            player.skipOnDrop = true;
            player.waitForFirstFrame = true;
            // ⚠️ NOT THE AUDIO CLOCK, the default: these clips are silent, and a stalled or reset audio device froze the picture.
            player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = target;
            player.audioOutputMode = VideoAudioOutputMode.None;
            player.aspectRatio = VideoAspectRatio.FitOutside;
            player.clip = clip;
            return player;
        }

        private Texture FirstPicture => _filmPoster != null ? _filmPoster : _poster;

        /// <summary>The idle's decoder, opened once the film is really running so the two never fight over boot.</summary>
        private void PrepareIdle()
        {
            if (_nextPreparing || _film == null || _failed) return;
            _nextPreparing = true;
            _nextTarget = new RenderTexture(1920, 1080, 0) { name = "HomeSceneIdle" };
            _next = NewPlayer(_clip, true, _nextTarget);
            _next.prepareCompleted += OnIdlePrepared;
            _next.errorReceived += OnError;
            _next.frameReady += OnIdleFrame;
            _next.sendFrameReadyEvents = true;
            _next.Prepare();
        }

        private void OnIdlePrepared(VideoPlayer player) { _nextPrepared = true; }

        private void OnFilmEnded(VideoPlayer player)
        {
            if (player != Player || _film == null) return;
            // The film's last frame stays in its target until the idle has a frame of its own.
            _filmEnded = true;
            FilmsSeen.Add(Hero);
            ResetWatchdog();
        }

        private void OnIdleFrame(VideoPlayer player, long frame)
        {
            // The prepared idle decodes a frame of its own accord; only one played after the film may take over.
            if (_failed || player != _next || !_filmEnded) return;
            player.sendFrameReadyEvents = false;
            player.frameReady -= OnIdleFrame;
            player.prepareCompleted -= OnIdlePrepared;
            var film = Player;
            var filmTarget = _target;
            Player = player; _target = _nextTarget; _next = null; _nextTarget = null; _filmEnded = false; _film = null; _filmPoster = null;
            _image.texture = _target;
            film.prepareCompleted -= OnPrepared; film.frameReady -= OnFirstFrame; film.errorReceived -= OnError;
            film.loopPointReached -= OnFilmEnded;
            film.Stop(); film.targetTexture = null;
            Destroy(film);
            filmTarget.Release(); Destroy(filmTarget);
            ResetWatchdog();
        }

        private void OnPrepared(VideoPlayer player)
        {
            if (_failed || player != Player) return;
            Prepared = true;
            // A paused preparation did not deliver the frameReady callback on
            // the Windows decoder. Decode one hidden frame,then pause in that callback.
            if (_preloading || (AtHome && !Settings.SettingsStore.Current.ReducedUiMotion)) player.Play();
        }

        private void OnFirstFrame(VideoPlayer player, long frame)
        {
            if (_failed || player != Player) return;
            FirstFrameReady = true;
            Prepared = player.isPrepared;
            player.sendFrameReadyEvents = false;
            if (_preloading) player.Pause();
        }

        private void OnError(VideoPlayer player, string message)
        {
            if (_failed) return;
            Debug.LogWarning("[HubSceneVideo] " + message);
            if (Recover()) return;
            // ⚠️ A decoder that cannot play the file leaves the poster up rather than a black hole.
            Prepared = false; FirstFrameReady = false; _failed = true;
            _image.texture = _poster;
            _image.enabled = !_preloading && AtHome && _poster != null;
            if (Player != null) Player.Stop();
            if (_next != null) _next.Stop();
            ReleaseTarget();
        }

        private static bool AtHome => TumpHub.Current == null || TumpHub.Current.ShowingHomeScene;

        private void Update()
        {
            if (_preloading) { _image.enabled = false; return; }
            // HOME and the pages laid over it (TumpHub.ShowingHomeScene), in every state: the poster before
            // the clip prepares, with reduced motion, and after a decode failure too. Never the lobby or
            // the map vote, or it would cover their map.
            bool home = AtHome;
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            // Reduced motion and a failed decode hold the idle's poster; only the film's preparation shows its own.
            _image.texture = !reduced && FirstFrameReady && _target != null ? _target
                : !reduced && !_failed ? FirstPicture : _poster;
            _image.enabled = home && _image.texture != null;
            UpdateCourtVisibility();
            if (home && !reduced && Player == null) PreparePlayback();
            if (home && !reduced) Watch();
            if (Player == null || !Prepared) return;
            if (_film != null && _next == null && !_nextPreparing && FirstFrameReady && Player.isPlaying) PrepareIdle();
            bool run = home && !reduced;
            if (_filmEnded)
            {
                if (_next == null) { if (run) PrepareIdle(); return; }
                if (_nextPrepared && run && !_next.isPlaying) _next.Play();
                else if (!run && _next.isPlaying) _next.Pause();
                return;
            }
            if (run && !Player.isPlaying) Player.Play();
            else if (!run && Player.isPlaying) Player.Pause();
        }

        private void OnDestroy()
        {
            if (_court != null) _court.SetRenderingEnabled(true);
            if (_preloaded == this) _preloaded = null;
            DropPlayers();
        }

        private void ReleaseTarget()
        {
            if (_target != null)
            {
                if (Player != null) Player.targetTexture = null;
                _target.Release();
                Destroy(_target);
                _target = null;
            }
            if (_nextTarget != null)
            {
                if (_next != null) _next.targetTexture = null;
                _nextTarget.Release();
                Destroy(_nextTarget);
                _nextTarget = null;
            }
        }
    }
}
