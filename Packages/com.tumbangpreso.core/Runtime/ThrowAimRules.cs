using System;

namespace TumbangPreso.Core
{
    /// <summary>Continuous, visible aim drift; no extra random error at release.</summary>
    public static class ThrowAimRules
    {
        public const float ResidualDegrees = .07f;
        public const float FreshAimDegrees = .95f;
        public const float SettleSeconds = .42f;
        public const float MovingDegrees = .90f;

        public static float Amplitude(float heldSeconds, float movement)
        {
            float hold = Clamp(heldSeconds, 0, 30);
            float moving = Clamp(movement, 0, 1.5f);
            return ResidualDegrees + FreshAimDegrees * (float)Math.Exp(-hold / SettleSeconds)
                + MovingDegrees * moving;
        }

        public static (float Yaw, float Pitch) Sample(float heldSeconds, float movement, float clockSeconds, float seed)
        {
            double t = Finite(clockSeconds) * Math.PI * 2;
            double phase = Finite(seed) * Math.PI * 2;
            float amplitude = Amplitude(heldSeconds, movement);
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
