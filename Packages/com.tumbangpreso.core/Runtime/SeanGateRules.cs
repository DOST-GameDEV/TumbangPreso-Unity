using System;

namespace TumbangPreso.Core
{
    public static class SeanGateRules
    {
        public const float Cooldown = 35f;
        public const float PlacementRange = 4f;
        public const float HalfWidth = 1.5f;
        public const float WarningSeconds = .35f;
        public const float ArmedSeconds = 3f;
        public const float TotalSeconds = WarningSeconds + ArmedSeconds;
        public const float PushDistance = .8f;
        public const float GroundTolerance = .25f;
        private const float SideEpsilon = .0001f;

        public static bool IsArmed(float age)
            => float.IsFinite(age) && age >= WarningSeconds && age < TotalSeconds;

        public static int Side(float z)
            => !float.IsFinite(z) || Math.Abs(z) <= SideEpsilon ? 0 : z > 0 ? 1 : -1;

        // Coordinates are relative to the finite line: X along it, Z across it.
        // A centre-line sample retains its last nonzero approach side. Touching
        // and returning is not crossing, and spawning on the line invents no side.
        public static bool TryCrossing(float previousX, float previousZ,
            float currentX, float currentZ, float bodyRadius, int lastSide,
            out float time, out int approachSide)
        {
            time = 0; approachSide = 0;
            if (!float.IsFinite(previousX) || !float.IsFinite(previousZ)
                || !float.IsFinite(currentX) || !float.IsFinite(currentZ)
                || !float.IsFinite(bodyRadius) || bodyRadius < 0 || bodyRadius > 2
                || lastSide < -1 || lastSide > 1) return false;
            int before = Side(previousZ), after = Side(currentZ);
            if (before == 0) before = lastSide;
            if (before == 0 || after == 0 || before == after) return false;
            double delta = (double)previousZ - currentZ;
            double crossing = Math.Abs(previousZ) <= SideEpsilon ? 0 : previousZ / delta;
            if (!double.IsFinite(crossing) || crossing < 0 || crossing > 1) return false;
            double x = previousX + ((double)currentX - previousX) * crossing;
            if (Math.Abs(x) > HalfWidth + bodyRadius) return false;
            time = (float)crossing; approachSide = before;
            return true;
        }
    }
}
