using System;

namespace TumbangPreso.Core
{
    public static class RafiRules
    {
        public const float BahaWarning=.8f, BahaHalfWidth=3f, BahaSpeed=5f;
        public const float BahaCarryDistance=3f, BahaCarryTail=BahaCarryDistance/BahaSpeed;
        public const float BahaMaximumRange=64f, BahaCost=15f;
        public static float BahaDuration(float distance) => BahaWarning+distance/BahaSpeed+BahaCarryTail;

        // Distance until this lane leaves a finite, potentially asymmetric court.
        // Out-of-court side lanes are closed, not extended through scenery.
        public static float BahaLaneExit(float x,float z,float dx,float dz,
            float minX,float maxX,float minZ,float maxZ)
        {
            if(!float.IsFinite(x)||!float.IsFinite(z)||!float.IsFinite(dx)||!float.IsFinite(dz)
                ||!float.IsFinite(minX)||!float.IsFinite(maxX)||!float.IsFinite(minZ)||!float.IsFinite(maxZ)
                ||minX>=maxX||minZ>=maxZ||x<minX||x>maxX||z<minZ||z>maxZ
                ||Math.Abs(dx*dx+dz*dz-1)>.001f)return 0;
            float tx=Math.Abs(dx)<.00001f?float.PositiveInfinity:(dx>0?(maxX-x)/dx:(minX-x)/dx);
            float tz=Math.Abs(dz)<.00001f?float.PositiveInfinity:(dz>0?(maxZ-z)/dz:(minZ-z)/dz);
            return Math.Clamp(Math.Min(tx,tz),0,BahaMaximumRange);
        }

        public static bool BahaCrosses(float side,float height,float forward,
            float previous,float current,float laneLimit,bool grounded)
        {
            return grounded&&float.IsFinite(side)&&float.IsFinite(height)&&float.IsFinite(forward)
                &&float.IsFinite(previous)&&float.IsFinite(current)&&float.IsFinite(laneLimit)
                &&previous>=0&&current>=previous&&laneLimit>=0&&laneLimit<=BahaMaximumRange
                &&Math.Abs(side)<=BahaHalfWidth&&height>=-.4f&&height<=.7f
                &&forward>=0&&forward<=laneLimit&&forward>=previous-.45f&&forward<=current+.45f;
        }

        // Published Hydro design, October 1: replace two charges with 35 seconds.
        public const float SkimCooldown = 35f;
        public const float SkimLoadSeconds = 8f;
        public const float SkimDistance = 2f;
        public const float SkimSpeed = 5f;
        public const float SkimStepHeight = .15f;
        public const float WallCooldown = 35f;
        public const float WallSeconds = 4f;
        public const float WallRange = 4f;
        public const float WallHalfWidth = 2f;
        public const float WallHeight = 1.8f;
        public const float WallGather = .25f;

        public static bool WallCrossing(float ax, float ay, float az,
            float bx, float by, float bz, out float time)
        {
            time = 0;
            if (!float.IsFinite(ax) || !float.IsFinite(ay) || !float.IsFinite(az)
                || !float.IsFinite(bx) || !float.IsFinite(by) || !float.IsFinite(bz)) return false;
            float dz = az - bz;
            if (Math.Abs(dz) < .00001f || (az > 0 && bz > 0) || (az < 0 && bz < 0)) return false;
            time = az / dz;
            if (time < 0 || time > 1) return false;
            float x = ax + (bx - ax) * time, y = ay + (by - ay) * time;
            return Math.Abs(x) <= WallHalfWidth && y >= -.05f && y <= WallHeight;
        }
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
