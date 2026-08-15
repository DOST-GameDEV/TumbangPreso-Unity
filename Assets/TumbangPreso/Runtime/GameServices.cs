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
        public static Audio.MusicDirector Music { get; private set; }

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
        private static void Bootstrap()
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
            Music = _root.AddComponent<Audio.MusicDirector>();
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
            Music = null;

            _menuTrack = null;
            _matchTrack = null;
            _menuTried = false;
            _matchTried = false;
        }
    }
}
