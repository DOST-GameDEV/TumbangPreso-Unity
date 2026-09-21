using UnityEngine;

namespace TumbangPreso
{
    // The shared introduction owns a hold, not the user's requested match speed.
    // A pause/slow-speed change during the hold becomes the rate on release.
    public static class PresentationClock
    {
        public static bool Held { get; private set; }
        private static float _resumeScale = 1;
        public static float RequestedScale => Held ? _resumeScale : Time.timeScale;
        public static void RequestScale(float scale)
        {
            if (float.IsNaN(scale) || float.IsInfinity(scale)) return;
            scale = Mathf.Max(0, scale);
            if (Held) _resumeScale = scale;
            else Time.timeScale = scale;
        }
        internal static void Hold()
        {
            if (Held) return;
            Hitstop.End(); _resumeScale = Time.timeScale; Held = true; Time.timeScale = 0;
        }
        internal static void Release()
        {
            if (!Held) return;
            Held = false; Time.timeScale = _resumeScale;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { Held = false; _resumeScale = 1; }
    }
}
