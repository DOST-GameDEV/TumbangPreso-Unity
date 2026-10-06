using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>DIN for match text; the central match clock keeps Darumadrop.</summary>
    public static class InGameTypography
    {
        private static Font _reading, _bold, _clock;
        public static Font Reading => _reading != null ? _reading :
            _reading = Resources.Load<Font>("UI/fonts/DINNextLTArabic-Light");
        public static Font Bold => _bold != null ? _bold :
            _bold = Resources.Load<Font>("UI/fonts/DINNextLTArabic-Bold");
        public static Font Clock => _clock != null ? _clock :
            _clock = Resources.Load<Font>("UI/fonts/DarumadropOne-Regular");

        private static bool IsMatchScene(Scene scene)
            => scene.IsValid() && System.Array.IndexOf(SceneFlow.Maps, scene.name) >= 0;

        public static Font Resolve(Font original, bool emphasized, Text label)
        {
            if (label == null) return original;
            var scene = label.gameObject.scene;
            // Prefetched map UI uses its own scene, rather than the still-active Home scene.
            bool match = IsMatchScene(scene) ||
                (scene.name == "DontDestroyOnLoad" && IsMatchScene(SceneManager.GetActiveScene()));
            if (!match) return original;
            if (label.name == "TimeLeft" || label.name == "TimerLabel")
                return Clock != null ? Clock : original;
            // Keep font-drawn reticle symbols in their existing symbol-capable face.
            if (label.name == "Reticle" || label.name == "HitConfirmation") return original;
            var requested = emphasized ? Bold : Reading;
            return requested != null ? requested : original;
        }
    }
}
