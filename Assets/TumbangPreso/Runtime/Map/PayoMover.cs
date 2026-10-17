using System;
using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// ONE SMALL STONE SLAB THAT MOVES DURING A ROUND on the Payo map (owner, 2026-10-12: "you can try having smaller
    /// moving platforms"). The middle of its TOP travels the straight line `From` to `To` and back once every
    /// `Period` seconds, resting `Dwell` seconds at each end, offset by `Phase` (a share of the period).
    ///
    /// ⚠️ ITS PLACE IS A CLOSED FORM OF ONE CLOCK (`PayoMovers.Clock`): nothing about it is sent, every peer works
    /// out the same place from the time every peer already shares, as `ArenaTraffic`, `AlleyStreet` and the run's
    /// `RunMover` do. It has no state: a peer that joins late, or comes out of a break, finds it where it is.
    /// </summary>
    public sealed class PayoMover : MonoBehaviour
    {
        public Vector3 From, To;
        public float Period = 16.0f, Phase, Dwell = 3.0f;
        public Collider Solid;

        [NonSerialized] public Vector3 LastPosition;

        /// <summary>0 at `From`, 1 at `To`, at clock `t`: rest, an eased crossing, rest, the crossing back.</summary>
        public float Share(double t)
        {
            float period = Mathf.Max(0.5f, Period);
            float dwell = Mathf.Clamp(Dwell, 0.0f, period * 0.5f - 0.05f), travel = period * 0.5f - dwell;
            // The turn's share is taken before it becomes a float: the clock may be hours.
            float at = (float)(((t / period + Phase) % 1.0 + 1.0) % 1.0) * period;
            if (at < dwell) return 0.0f;
            if (at < dwell + travel) return Ease((at - dwell) / travel);
            if (at < dwell * 2.0f + travel) return 1.0f;
            return 1.0f - Ease((at - dwell * 2.0f - travel) / travel);
        }

        public Vector3 Pose(double t) => Vector3.LerpUnclamped(From, To, Share(t));

        /// <summary>A stone slab does not start or stop dead: a short ease at each end, a steady pace between.</summary>
        private static float Ease(float u)
        {
            u = Mathf.Clamp01(u);
            const float k = 0.18f;                      // the share of the crossing spent speeding up, and again slowing
            float top = 1.0f / (1.0f - k);              // the steady pace, so the whole is still 0 to 1
            if (u < k) return 0.5f * top * u * u / k;
            if (u > 1.0f - k) { float v = 1.0f - u; return 1.0f - 0.5f * top * v * v / k; }
            return 0.5f * top * k + top * (u - k);
        }
    }
}
