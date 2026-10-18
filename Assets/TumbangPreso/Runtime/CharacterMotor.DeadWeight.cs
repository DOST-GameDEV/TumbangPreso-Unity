using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// DEAD WEIGHT, Nemu's passive (owner, 2026-10-16): a rival within `NemuRules.DeadWeightRadius` of any of her
    /// visible ghosts moves 10 % slower. It is read off the ghosts every peer already runs from the host's frames
    /// (protocol 163 and 164), like Voodoo's marks, so it carries no state and no wire of its own.
    ///
    /// ⚠️ DON'T LOOK's ghost counts only while it is still visible, arming. Once armed it is hidden from rivals, and a
    /// slow round an invisible ghost would give its spot away: the passive must never betray her own trap.
    /// </summary>
    public sealed partial class CharacterMotor
    {
        public float DeadWeightSpeedScale => NemuRules.DeadWeightScale(NearestRivalGhost());

        public bool InDeadWeight => DeadWeightSpeedScale < 1.0f;

        /// <summary>Flat distance to the nearest of another player's chilling ghosts, or -1 when there is none.</summary>
        private float NearestRivalGhost()
        {
            if (Mode != GameMode.HeroStrike) return -1.0f;
            float best = -1.0f;
            Vector3 at = transform.position;
            foreach (var g in NemuSkitter.Live)
                if (g != null && !g.Leaving && g.OwnerSlot != PlayerSlot) best = Closer(best, at, g.Position);
            foreach (var g in NemuLookout.Live)
                if (g != null && !g.Leaving && g.OwnerSlot != PlayerSlot) best = Closer(best, at, g.Position);
            foreach (var g in NemuBlindfold.Live)
                if (g != null && !g.Armed && !g.Sprung && g.OwnerSlot != PlayerSlot) best = Closer(best, at, g.Spot);
            return best;
        }

        private static float Closer(float best, Vector3 a, Vector3 b)
        {
            float d = new Vector2(a.x - b.x, a.z - b.z).magnitude;
            return best < 0.0f || d < best ? d : best;
        }
    }
}
