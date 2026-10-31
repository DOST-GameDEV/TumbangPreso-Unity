using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// A caught body reacting, for the replays' render-only copies (owner, October 31: "it looks so clunky or unnatural"). The
    /// whole body used to tip like a plank in whatever stride the tag found it. Each part now answers the hit at its own
    /// time: chest first, head whipping after it, arms thrown out, a small hop, all settling on one spring.
    /// Laid over the recorded pose each frame. Presentation only.
    /// </summary>
    internal sealed class CaughtRecoil
    {
        /// <summary>How far the whole body tips, degrees: the chest and head add their own, so 26 folded them in half.</summary>
        public const float Tilt = 14;
        private readonly Transform _torso, _head, _armLeft, _armRight;
        public CaughtRecoil(Transform[] bones)
        {
            foreach (var bone in bones)
            {
                if (bone == null) continue;
                switch (bone.name)
                {
                    case "torso": _torso = bone; break;
                    case "head": _head = bone; break;
                    case "arm-left": _armLeft = bone; break;
                    case "arm-right": _armRight = bone; break;
                }
            }
        }
        // Past one and back: 0 at the hit, overshoots, then comes to rest at 1.
        private static float Spring(float t) => t <= 0 ? 0 : 1 - Mathf.Exp(-6.5f * t) * Mathf.Cos(11 * t);
        /// <summary>How far the feet leave the ground, metres, at <paramref name="seconds"/> after the hit.</summary>
        public static float Hop(float seconds) => seconds <= 0 ? 0 : .07f * Mathf.Sin(Mathf.Clamp01(seconds / .3f) * Mathf.PI);
        public void Pose(Vector3 away, float seconds)
        {
            if (seconds <= 0) return;
            Vector3 back = Vector3.Cross(Vector3.up, away);
            if (_torso != null) _torso.rotation = Quaternion.AngleAxis(6 * Spring(seconds), back) * _torso.rotation;
            // The head lags the chest and goes further.
            if (_head != null) _head.rotation = Quaternion.AngleAxis(-20 * Spring(seconds - .03f), back) * _head.rotation;
            float fling = 38 * Spring(seconds - .015f);
            if (_armLeft != null) _armLeft.rotation = Quaternion.AngleAxis(fling, away) * _armLeft.rotation;
            if (_armRight != null) _armRight.rotation = Quaternion.AngleAxis(-fling, away) * _armRight.rotation;
        }
    }
}
