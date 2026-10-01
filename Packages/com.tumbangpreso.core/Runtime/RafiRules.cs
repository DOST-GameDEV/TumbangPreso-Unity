using System;

namespace TumbangPreso.Core
{
    public static class RafiRules
    {
        // Published Hydro design, October 1: replace two charges with 35 seconds.
        public const float SkimCooldown = 35f;
        public const float SkimLoadSeconds = 8f;
        public const float SkimDistance = 2f;
        public const float SkimSpeed = 5f;
        public const float SkimStepHeight = .15f;
        public const float CurrentCooldown = 35f;
        public const float CurrentRange = 6f;
        public const float CurrentRadius = .65f;
        public const float CurrentHalfHeight = .70f;
        public const float CurrentGather = .18f;
        public const float CurrentTurnDegrees = 40f;

        // Intersect relative shoe motion with the current's finite cylinder.
        // Earliest overlap, not closest approach or scene enumeration order, wins.
        public static bool FirstCurrentContact(float ax, float ay, float az,
            float bx, float by, float bz, float radius, out float time)
        {
            time = 0;
            if (!float.IsFinite(ax) || !float.IsFinite(ay) || !float.IsFinite(az)
                || !float.IsFinite(bx) || !float.IsFinite(by) || !float.IsFinite(bz)
                || !float.IsFinite(radius) || radius <= 0 || radius > CurrentRadius) return false;
            double dx = (double)bx - ax, dy = (double)by - ay, dz = (double)bz - az;
            double enter = 0, leave = 1;
            if (Math.Abs(dy) < 1e-9)
            {
                if (Math.Abs(ay) > CurrentHalfHeight) return false;
            }
            else
            {
                double low = (-CurrentHalfHeight - ay) / dy;
                double high = (CurrentHalfHeight - ay) / dy;
                enter = Math.Max(enter, Math.Min(low, high));
                leave = Math.Min(leave, Math.Max(low, high));
            }
            double a = dx * dx + dz * dz;
            double c = (double)ax * ax + (double)az * az - (double)radius * radius;
            if (a < 1e-12)
            {
                if (c > 0) return false;
            }
            else
            {
                double b = 2 * (ax * dx + az * dz);
                double discriminant = b * b - 4 * a * c;
                if (discriminant < 0) return false;
                double root = Math.Sqrt(discriminant);
                enter = Math.Max(enter, (-b - root) / (2 * a));
                leave = Math.Min(leave, (-b + root) / (2 * a));
            }
            if (enter > leave) return false;
            time = (float)enter;
            return true;
        }
    }
}
