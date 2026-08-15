using System;

namespace TumbangPreso.Core
{
    /// <summary>Everything the throw needs to know about the thrower and the world.</summary>
    public struct ThrowContext
    {
        public bool RoundActive;
        public bool IsDefender;
        public bool HoldingSlipper;
        public bool LataUpright;
        public float ThrowCooldownLeft;
        public float X;
        public float Z;
        public float ConfinementRadius;

        public static ThrowContext Default()
        {
            return new ThrowContext
            {
                RoundActive = true,
                IsDefender = false,
                HoldingSlipper = true,
                LataUpright = true,
                ThrowCooldownLeft = 0.0f,
                X = 0.0f,
                Z = 0.0f,
                ConfinementRadius = Balance.ConfinementRadius,
            };
        }
    }

    /// <summary>
    /// Throw legality, the scoring window, and the arc.
    /// </summary>
    public static class ThrowRules
    {
        /// <summary>
        /// ⚠️ ALL OF THESE MUST HOLD OR THE THROW IS REFUSED, and this one function is the
        /// entire rule. The Defender is refused outright, which is what frees left-click
        /// to be their punch without taking anything from anybody.
        ///
        /// ⚠️⚠️ THE CROSSHAIR ASKS THIS SAME FUNCTION. It is shown only when a throw would
        /// actually be accepted, so it greys out for exactly the reasons the throw
        /// refuses. A second opinion about legality is a crosshair that promises a throw
        /// the rules then refuse, which is the most confusing possible failure: the
        /// player sees no reason for nothing to happen. Do not give the UI its own copy
        /// of this logic during the port.
        /// </summary>
        public static bool CanThrow(in ThrowContext c)
        {
            if (!c.RoundActive) return false;
            if (c.IsDefender) return false;
            if (!c.HoldingSlipper) return false;
            if (!c.LataUpright) return false;
            if (c.ThrowCooldownLeft > 0.0f) return false;

            // Outside the box. The negation of IsInsideBox, so the boundary case cannot
            // be decided differently in the two places.
            return !Confinement.IsInsideBox(c.X, c.Z, c.ConfinementRadius);
        }

        /// <summary>
        /// The scoring window: how close the slipper's flat distance has to get to the
        /// can. 0.53 m at neutral, divided by the can's STANCE.
        ///
        /// ⚠️ THIS IS NOT THE COLLIDER AND THAT IS THE FAIRNESS RULING. The four cans span
        /// 0.108 to 0.143 m in body radius, a 32% spread. Deriving the scoring window from
        /// that geometry would make the prettiest can quietly the hardest to hit with
        /// nothing on screen saying so. A competitive difference between COSMETIC picks
        /// has to be DECLARED, and the STANCE meter is where it is declared: BOYBEN
        /// (stance 5) shrinks the window to 0.493 m, PASIP (stance 1) opens it to 0.579.
        /// </summary>
        /// ⚠️ ONLY THE MARGIN IS DIVIDED BY STANCE, NOT THE WHOLE WINDOW. The slipper's own
        /// contact radius is a property of the slipper and cannot be changed by which can
        /// it is flying at. Dividing the sum instead reproduces neither documented figure
        /// (it gives BOYBEN 0.465 against the recorded 0.493); dividing only the margin
        /// reproduces both to three decimals, which is what fixes the formula here.
        public static float HitWindow(int canIndex)
        {
            return Balance.SlipperHitRadius + Balance.LataHitMargin / Roster.CanStanceScale(canIndex);
        }

        /// <summary>Did this flight connect? Flat distance, tested per physics frame,
        /// host-side. ⚠️ Deliberately a DISTANCE CHECK and not an overlap volume: an
        /// overlap fires on whichever peer owns the body, and hit_probe measured 16 of 36
        /// overlaps failing to land, split by target.</summary>
        public static bool Connects(float flatDistance, int canIndex) =>
            flatDistance <= HitWindow(canIndex);

        /// <summary>
        /// How fast this slipper actually leaves the hand, at a given charge.
        ///
        /// ⚠️ THE PREVIEW MUST ASK THIS SAME FUNCTION. The dotted aim arc and the real
        /// flight are one line by construction only while both integrate the velocity
        /// this returns. They were measured agreeing to 0.000 m on three of the four
        /// skins; the fourth misses by 0.263 m for a reason that is not the arc (a crocs
        /// rests 0.161 m off the ground, so the preview's fixed floor epsilon stops the
        /// line higher than the slipper stops).
        /// </summary>
        public static float LaunchSpeedFor(int slipperIndex, float charge01)
        {
            float power = Balance.ChargeMinPower + (1.0f - Balance.ChargeMinPower) * Clamp01(charge01);
            return Balance.LaunchSpeed * Roster.SlipperFlightScale(slipperIndex) * power;
        }

        /// <summary>Charge ratio from how long the button has been held.</summary>
        public static float ChargeRatio(float heldSeconds) =>
            Clamp01(heldSeconds / Balance.ChargeFullTime);

        /// <summary>
        /// How long after a pickup before this slipper can be thrown, divided by the
        /// skin's RECOVERY.
        ///
        /// ⚠️ RECOVERY BUYS EXPOSURE BACK, NOT DAMAGE, and that is the stat playing the
        /// game's actual thesis: the tension is the retrieval, not the throw. A shorter
        /// lock is less time stood inside the box vulnerable.
        /// </summary>
        public static float ThrowLockFor(int slipperIndex) =>
            Balance.ThrowLockTime / Roster.SlipperRecoveryScale(slipperIndex);

        /// <summary>
        /// The 45° ballistic range of a launch speed, which is the bound every arena size
        /// has to clear: speed² / gravity.
        ///
        /// The shortest legal throw is the box radius, against a neutral range of
        /// 18.5² / 20.0 = 17.11 m. Both map builders re-verify this and abort rather than
        /// shipping an arena where a legal position cannot reach the can.
        /// </summary>
        public static float MaxRange(float launchSpeed) =>
            launchSpeed * launchSpeed / Balance.Gravity;

        /// <summary>
        /// How long a projectile launched at <paramref name="verticalSpeed"/> takes to
        /// return to its launch height. Shared by the flight and the preview.
        /// </summary>
        public static float FlightTime(float verticalSpeed) =>
            2.0f * verticalSpeed / Balance.Gravity;

        private static float Clamp01(float v) => v < 0.0f ? 0.0f : (v > 1.0f ? 1.0f : v);
    }
}
