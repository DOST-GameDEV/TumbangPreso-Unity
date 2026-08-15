using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The screen graph, and the one place a scene name is written down.
    ///
    /// ⚠️ NAMES IN ONE PLACE, NOT SCATTERED THROUGH THE SCREENS. In the Godot build each screen
    /// carried its own `res://scenes/ui/Whatever.tscn` constant, which is fine until a scene is
    /// renamed and the reference that breaks is in a screen nobody opened during testing. A
    /// missing scene here fails loudly with the name it wanted, rather than silently doing
    /// nothing when a button is pressed.
    /// </summary>
    public static class SceneFlow
    {
        public const string Splash = "Splash";
        public const string MainMenu = "MainMenu";
        public const string ModeSelect = "ModeSelect";
        public const string MatchSetup = "MatchSetup";
        public const string MultiplayerSetup = "MultiplayerSetup";
        public const string CharacterSelect = "CharacterSelect";
        public const string MatchResult = "MatchResult";

        /// <summary>The playable arenas, by the names the Godot builders gave them.</summary>
        public const string Eskinita = "Eskinita";
        public const string BayanPlaza = "BayanPlaza";

        public static readonly string[] Maps = { Eskinita, BayanPlaza };

        /// <summary>Which map the next match loads. Set by the setup screen.</summary>
        public static string SelectedMap = Eskinita;

        /// <summary>True when the next match is networked rather than against bots.</summary>
        public static bool Networked;

        public static void Go(string scene)
        {
            if (string.IsNullOrEmpty(scene))
            {
                Debug.LogError("[Flow] asked for an empty scene name.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                // ⚠️ LOUD, NOT SILENT. A scene missing from the build settings is the single
                // most common way a menu button does nothing at all, and it is invisible in the
                // editor where scenes load by path regardless.
                Debug.LogError($"[Flow] scene '{scene}' is not in the build settings. " +
                               "Add it, or the button that asked for it will do nothing in a build.");
                return;
            }

            SceneManager.LoadScene(scene);
        }

        public static void StartMatch()
        {
            Go(SelectedMap);
        }

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
