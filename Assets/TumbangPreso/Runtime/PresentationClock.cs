using UnityEngine;

namespace TumbangPreso
{
    // The shared introduction owns a hold, not the user's requested match speed.
    // A pause/slow-speed change during the hold becomes the rate on release.
    public static class PresentationClock
    {
        public static bool Held { get; private set; }
        private static float _resumeScale = 1;
        private static float _normalFixedStep;
        public static bool BlocksInput => (Held && !SharedUltimatePhase.Collecting) || SharedUltimatePhase.BlocksActions || (!SharedUltimatePhase.Collecting && RequestedScale <= 0);
        public static float RequestedScale => Held ? _resumeScale : Hitstop.RequestedScale;
        public static void RequestScale(float scale)
        {
            if (float.IsNaN(scale) || float.IsInfinity(scale)) return;
            scale = Mathf.Max(0, scale);
            if (Held) _resumeScale = scale;
            else ApplyScale(scale);
        }

        private static void ApplyScale(float scale)
        {
            // Motors and the pose stream both run on the physics step. Keep
            // their real-time cadence while reducing the simulated time per step.
            if (scale > 0 && scale < 1)
            {
                if (_normalFixedStep <= 0) _normalFixedStep = Time.fixedDeltaTime;
                Time.fixedDeltaTime = _normalFixedStep * Mathf.Max(.25f, scale);
            }
            else if (_normalFixedStep > 0)
            {
                Time.fixedDeltaTime = _normalFixedStep;
                _normalFixedStep = 0;
            }
            Time.timeScale = scale;
        }
        internal static void Hold()
        {
            if (Held) return;
            Hitstop.End(); _resumeScale = Time.timeScale; Held = true; Time.timeScale = 0;
        }
        internal static void Release()
        {
            if (!Held) return;
            Held = false; ApplyScale(_resumeScale);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { Held = false; _resumeScale = 1; _normalFixedStep = 0; }
    }
}
