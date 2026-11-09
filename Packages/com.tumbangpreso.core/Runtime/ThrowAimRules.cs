using System;

namespace TumbangPreso.Core
{
    /// <summary>Continuous, visible aim drift; no extra random error at release.</summary>
    public static class ThrowAimRules
    {
        public const float ResidualDegrees = .07f;
        public const float FreshAimDegrees = 1.8f;
        public const float SettleSeconds = .42f;
        public const float MovingDegrees = 2.8f;
        private const float MaxMovementLoad = 1.9f;

        /// <summary>Measured speed relative to a normal walk. An airborne release cannot plant its feet.</summary>
        public static float MovementLoad(float horizontalSpeed, bool grounded, bool sliding = false)
        {
            float pace = Clamp(horizontalSpeed / Balance.AttackerWalkSpeed, 0, MaxMovementLoad);
            if (!grounded) return Math.Min(MaxMovementLoad, 1.25f + pace * .4f);
            if (sliding) return Math.Min(MaxMovementLoad, Math.Max(1.35f, pace) + .2f);
            return pace;
        }

        // Only a settled, stationary hold gets a long directional guide. This is
        // presentation confidence, not extra launch power or a different shot roll.
        public static float GuideConfidence(float heldSeconds, float movement, int slipperIndex = -1) =>
            Clamp((.65f - Amplitude(heldSeconds, movement, slipperIndex)) / .55f, 0, 1);

        public static float GuideHorizon(float heldSeconds, float movement, int slipperIndex = -1) =>
            .24f + .50f * GuideConfidence(heldSeconds, movement, slipperIndex);

        public static float Amplitude(float heldSeconds, float movement, int slipperIndex = -1)
        {
            float hold = Clamp(heldSeconds, 0, 30);
            float moving = Clamp(movement, 0, MaxMovementLoad);
            // The held equipment changes how quickly preparation settles, not movement
            // drift or the final residual. This same visible solution is released.
            return ResidualDegrees + FreshAimDegrees * (float)Math.Exp(-hold * Roster.SlipperRecoveryScale(slipperIndex) / SettleSeconds)
                + MovingDegrees * moving;
        }

        public static (float Yaw, float Pitch) Sample(float heldSeconds, float movement, float clockSeconds, float seed, int slipperIndex = -1)
        {
            double t = Finite(clockSeconds) * Math.PI * 2;
            double phase = Finite(seed) * Math.PI * 2;
            float amplitude = Amplitude(heldSeconds, movement, slipperIndex);
            float yaw = (float)(.55 * Math.Sin(t * 1.35 + phase)
                + .30 * Math.Sin(t * 2.17 + phase * .7 + .5)
                + .15 * Math.Sin(t * .71 + phase * 1.3 + 1.1));
            float pitch = (float)(.43 * Math.Sin(t * 1.11 + phase + 1.7)
                + .17 * Math.Sin(t * 2.41 + phase * .8)
                + .10 * Math.Sin(t * .67 + phase * 1.2));
            return (yaw * amplitude, pitch * amplitude);
        }

        private static float Finite(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0 : value;
        private static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, Finite(value)));
    }
}
