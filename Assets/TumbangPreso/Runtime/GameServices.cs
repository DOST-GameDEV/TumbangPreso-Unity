using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// THE REPLACEMENT FOR GODOT'S NINE AUTOLOADS.
    ///
    /// Godot's project.godot declares nine autoload singletons (AudioManager, RoundManager,
    /// MatchManager, NetworkManager, LanBeacon, ServerQuery, GameLaunch, SettingsManager,
    /// DebugPlayerSwitcher). They exist before any scene loads, outlive every scene change,
    /// and every script reaches them by bare name. Unity has no such concept.
    ///
    /// ⚠️ THE OBVIOUS PORT IS A BOOTSTRAP SCENE, AND IT IS THE WRONG ONE. That is the
    /// common Unity answer: a scene at build index 0 holding the managers, DontDestroyOnLoad
    /// on each, every other scene loaded after it. It has a failure mode that costs hours
    /// every time and teaches nothing: press Play in any scene that is not the bootstrap and
    /// every manager is null. That is precisely how this team works, opening the scene they
    /// are editing and pressing Play, and it would also break every headless probe, each of
    /// which loads exactly the one scene it measures.
    ///
    /// ⚠️ SO THE SERVICES CREATE THEMSELVES INSTEAD. `RuntimeInitializeOnLoadMethod` with
    /// `BeforeSceneLoad` runs before the first scene of the session regardless of WHICH
    /// scene that is: the editor's current scene, a probe's single scene, or the dedicated
    /// server's. That reproduces the property that actually mattered about autoloads (they
    /// are simply always there) without reproducing the build-order dependency Unity would
    /// have made us adopt along with it.
    ///
    /// ⚠️ AND IT WORKS ON THE DEDICATED SERVER, which is not incidental. The Linux server
    /// build has no scene the operator picks; anything that depends on a human opening the
    /// right scene has already failed there.
    /// </summary>
    public static class GameServices
    {
        private static GameObject _root;

        public static AudioDirector Audio { get; private set; }
        public static MatchDirector Match { get; private set; }
        public static RoundDirector Round { get; private set; }

        /// <summary>
        /// PHASE 12's LAST TSINELAS STANDING match half. Present in every match and inert in
        /// every format but its own, which is why it is a service rather than something the
        /// arena installs: `SliceRunner` and `MatchBootstrap` are two runners and a rule that
        /// only one of them installed would be a rule the other silently lacks.
        /// </summary>
        public static LastTsinelasDirector Tsinelas { get; private set; }
        public static Audio.MusicDirector Music { get; private set; }
        public static Net.PlayerAccount Account { get; private set; }

        /// <summary>Counts what happens in a match, host-side. See its own header for why it
        /// is not simply a listener on `MatchDirector.Scored`.</summary>
        public static MatchStatsCollector Stats { get; private set; }

        /// <summary>The career: profile, match history and the queue of records that have not
        /// reached the server yet.</summary>
        public static Net.CareerStore Career { get; private set; }

        /// <summary>
        /// Session telemetry: the first-launch funnel and the match-level counters, batched and
        /// sent once. `docs/TODO.md` § 90.3.
        /// </summary>
        public static Net.TelemetrySink Telemetry { get; private set; }

        /// <summary>Friends, blocks and presence. `docs/TODO.md` § 102.</summary>
        public static Net.SocialStore Social { get; private set; }

        /// <summary>The announcer. Godot had it inside AudioManager; it is its own director
        /// here because its take pooling, per-line cooldowns and music ducking are a system,
        /// not three fields on the SFX player.</summary>
        public static Audio.VoiceDirector Voice { get; private set; }

        public static bool Ready => _root != null;

        /// <summary>
        /// The two OST tracks, loaded from Resources on first use.
        ///
        /// ⚠️ LOADED LAZILY AND ALLOWED TO BE NULL. A missing music file must not stop the game
        /// booting: the bed is the one thing in an audio stack whose absence a player can play
        /// straight through without noticing anything is broken.
        /// </summary>
        public static AudioClip MenuTrack => LoadMusic("ost_menu", ref _menuTrack);
        public static AudioClip MatchTrack => LoadMusic("ost_match", ref _matchTrack);

        private static AudioClip _menuTrack;
        private static AudioClip _matchTrack;
        private static bool _menuTried, _matchTried;

        private static AudioClip LoadMusic(string name, ref AudioClip cache)
        {
            if (cache != null) return cache;

            bool tried = name == "ost_menu" ? _menuTried : _matchTried;
            if (tried) return null;

            if (name == "ost_menu") _menuTried = true; else _matchTried = true;

            cache = Resources.Load<AudioClip>($"Music/{name}");
            if (cache == null)
                Debug.Log($"[Audio] no music at Resources/Music/{name}. The game runs without it.");

            return cache;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap() => Ensure();

        /// <summary>
        /// Builds the services root if it does not exist yet. Idempotent, and public for
        /// exactly one reason: see the ⚠️ below.
        /// </summary>
        /// ⚠️ CALLABLE EARLY, BECAUSE BootSting NEEDS THIS TO EXIST BEFORE BeforeSceneLoad DOES.
        /// The boot sting plays at BeforeSplashScreen, which runs before this class's own
        /// BeforeSceneLoad hook, so BootSting used to build its own throwaway AudioListener to
        /// have something to be heard by. That listener and AudioDirector's later one were BOTH
        /// on HideAndDontSave objects, and FindObjectsByType silently excludes those (see the
        /// ⚠️ on AudioDirector.Awake for the measurement), so the two listeners could never see
        /// each other and neither was ever disabled: two enabled AudioListeners persisted for the
        /// entire process, and Unity's own duplicate-listener warning printed every frame. Rather
        /// than race two independent RuntimeInitializeOnLoadMethod hooks (undefined relative
        /// order between two hooks of the SAME load type; see NetIdentity.SignInAtBoot for the
        /// same hazard), BootSting now calls this directly, forcing the one true owner to exist
        /// before it needs one, deterministically rather than by hoping for a lucky order.
        public static void Ensure()
        {
            if (_root != null) return;

            _root = new GameObject("~GameServices");
            Object.DontDestroyOnLoad(_root);

            // ⚠️ HIDDEN FROM THE HIERARCHY ON PURPOSE. It is not part of any scene and must
            // never be saved into one: a copy accidentally committed into a scene file is a
            // SECOND set of managers, which is the failure mode where half the game talks
            // to one and half to the other.
            _root.hideFlags = HideFlags.HideAndDontSave;

            Audio = _root.AddComponent<AudioDirector>();
            Match = _root.AddComponent<MatchDirector>();
            Round = _root.AddComponent<RoundDirector>();

            // ⚠️ AFTER BOTH DIRECTORS, because `LastTsinelasDirector.OnEnable` subscribes to
            // `Match.RoundStarted` and `Round.Tagged`, and `AddComponent` runs `OnEnable`
            // immediately. Reversing these lines gives it two nulls to subscribe to and the
            // format silently never counts a tag. Same hazard and same answer as `Career` below.
            Tsinelas = _root.AddComponent<LastTsinelasDirector>();

            Music = _root.AddComponent<Audio.MusicDirector>();
            Voice = _root.AddComponent<Audio.VoiceDirector>();
            Account = _root.AddComponent<Net.PlayerAccount>();

            // ⚠️ THE CAREER IS ADDED AFTER THE ACCOUNT, DELIBERATELY. `CareerStore.Awake`
            // subscribes to `PlayerAccount.Changed` so it can sync the moment a sign-in lands,
            // and `AddComponent` runs `Awake` immediately: reversing these two lines gives the
            // career a null account to subscribe to and nothing ever uploads.
            Career = _root.AddComponent<Net.CareerStore>();

            // ⚠️ AND THE COLLECTOR LAST, because its `OnEnable` subscribes to `Match` and its
            // `Adopt` reaches `Career`. Same hazard, same answer: order the construction rather
            // than hoping for one.
            Stats = _root.AddComponent<MatchStatsCollector>();

            // ⚠️ TELEMETRY IS BUILT LAST AND IT DEPENDS ON NOTHING ABOVE IT, which is the point.
            // Its `Awake` opens the first-launch funnel, and a funnel whose first step could be
            // skipped by an earlier service failing would measure the boot it wanted rather than
            // the boot that happened. Nothing else in this list reads it, so it can never be the
            // reason another service is missing.
            Telemetry = _root.AddComponent<Net.TelemetrySink>();

            // ⚠️ SOCIAL AFTER THE CAREER, because `SocialStore.Load` keys its cache on
            // `CareerStore.LocalPlayerId` and a cache with no owner is one player's friends list
            // drawn under another player's name. Same hazard and same answer as the two lines
            // above: order the construction rather than hoping for one.
            Social = _root.AddComponent<Net.SocialStore>();
        }

        /// <summary>
        /// ⚠️ EDIT-MODE AND PLAY-MODE TESTS NEED THIS. Domain reload can be disabled in the
        /// editor, in which case statics survive between Play sessions and the second run
        /// starts with managers pointing at destroyed GameObjects.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _root = null;
            Audio = null;
            Match = null;
            Round = null;
            Tsinelas = null;
            Music = null;
            Account = null;
            Career = null;
            Stats = null;
            Telemetry = null;
            Social = null;

            _menuTrack = null;
            _matchTrack = null;
            _menuTried = false;
            _matchTried = false;
        }
    }
}
