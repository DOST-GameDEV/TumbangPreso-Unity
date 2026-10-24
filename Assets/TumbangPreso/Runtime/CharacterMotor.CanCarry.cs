using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// ⚠️⚠️ THE TAYA CARRYING THE CAN WALKS THEMSELF BACK TO THE MARK (CAN-CARRY-1024, owner 2026-10-24: *"kasama na ung
    /// movement sa holding"*, *"siguro normal speed"*). While the can is in this body's hands (`Lata.Carry.cs`) the stick
    /// is ignored: through the pickup and the set-down the body stands, and through the carry it walks straight to the
    /// mark at its ordinary walking speed, no sprint, stopping where the can it holds is over the mark.
    ///
    /// ⚠️ ON THE PEER THAT SIMULATES THE BODY, like `FleeWish`. The carry state is the host's and reaches every peer on
    /// `SyncLata`; the owning client's own prediction starts it a round trip early. The host then sees the walk arrive in
    /// the body's synced position and sets the can down.
    /// </summary>
    public sealed partial class CharacterMotor
    {
        /// <summary>True for the whole carry, pickup to set-down: the stick is ignored and there is no sprint.</summary>
        public bool IsCarryingCan
        {
            get
            {
                var lata = GameServices.Round?.Lata;
                return lata != null && lata.CarryHolds(_playerSlot);
            }
        }

        /// <summary>The walk to the mark this frame (flat, length 0 to 1), or zero while standing for the pickup or set-down.</summary>
        private Vector3 CarryWish(float speed, float dt)
        {
            var lata = GameServices.Round?.Lata;
            if (lata == null || !lata.CarryWalks(_playerSlot)) return Vector3.zero;
            Vector3 to = lata.Mark - transform.position; to.y = 0.0f;
            float left = to.magnitude - (Lata.HoldReach + Lata.WalkStopSlack);
            if (left <= 0.0f || to.sqrMagnitude < 1e-6f) return Vector3.zero;
            // No overshoot on the last step: the wish shrinks to exactly what is left.
            float step = Mathf.Max(1e-4f, speed * Mathf.Max(1e-4f, dt));
            return to.normalized * Mathf.Min(1.0f, left / step);
        }
    }
}
