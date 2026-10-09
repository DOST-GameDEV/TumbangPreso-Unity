using System;

namespace TumbangPreso.Abilities
{
    /// <summary>What LIANA LEAP does with a catch. `Reel` is the flat pull it always was; the other three are swings.</summary>
    public enum VineSwing : byte
    {
        /// <summary>The flat pull to a low catch or the ground: `CharacterMotor.ApplyResolvedCarry`, as before the swing existed.</summary>
        Reel,
        /// <summary>The catch is the lip of something he can stand on: up outside it, over the lip and onto it.</summary>
        Over,
        /// <summary>The catch is high on a face with nothing to stand on and no way past: up to it, and he drops from there.</summary>
        Wall,
        /// <summary>The catch hangs over open space (a beam, a branch, an overhang): under it and flung out the far side.</summary>
        Under
    }

    /// <summary>
    /// ⚠️⚠️ THE SWING (owner, 2026-10-07: *"can you make the leap swing better and also fix the aiming so i can swing from
    /// higher? look at where im aiming vs where i actually leap from"*; his answers the same day: catch SURFACES, a sky aim
    /// SNAPS UP to the nearest edge, ANYTHING IN RANGE, and SWING THROUGH AND FLING).
    ///
    /// Until then every leap was a flat carry with a 3 m/s hop, and an aim at the sky was dropped to the ground under it,
    /// so it could cross a floor and never leave it. A swing is a PATH: one curve in the upright plane through his feet
    /// and the catch (x along the ground toward the catch, y up, both from where his feet were), walked at a speed that
    /// builds, and at its end the vines let go and he is thrown (<paramref name="flingX"/>, <paramref name="flingY"/>,
    /// held for <paramref name="hold"/> seconds, after which he steers in the air as on any jump).
    ///
    /// ⚠️ PURE NUMBERS, NO ENGINE, ON PURPOSE: the body's owner walks it (`CharacterMotor.BeginSwing`), the host times
    /// the vines by it (<see cref="Seconds"/>), and `tools/plot_paete_swing.py` is the same lines in Python, which is
    /// how the shapes were looked at before they were played (`Logs/paete-swing/`). Change one, change the other.
    ///
    /// ⚠️ EVERY SPEED HERE IS UNDER `Balance.MaxKnockbackSpeed` (16) and so far under the host's move budget (30 m/s).
    /// </summary>
    public static class PaeteSwing
    {
        /// <summary>The path is stored a point every this many seconds; at most <see cref="MaxSteps"/> of them (2.2 s).</summary>
        public const float Step = 0.02f;
        public const int MaxSteps = 110;

        /// <summary>Where the vines leave him: his chest, this far over his feet.</summary>
        public const float Chest = 1.3f;
        /// <summary>A catch lower than this over his feet is not worth a swing: the old flat pull.</summary>
        public const float MinRise = 2.6f;
        /// <summary>A lip lower than this over his feet he can all but step onto: the old flat pull.</summary>
        public const float MinLipRise = 1.2f;

        /// <summary>He rises this far out from a face, so his body clears it.</summary>
        public const float StandOff = 0.55f;
        /// <summary>OVER ends with his feet this far above the lip.</summary>
        public const float OverClear = 0.35f;
        /// <summary>The speed along the path: from <see cref="StartSpeed"/>, gaining <see cref="SpeedGain"/> a second, to its top.</summary>
        public const float StartSpeed = 5.0f, TopSpeed = 13.0f, UnderSpeed = 12.5f, SpeedGain = 55.0f;
        /// <summary>OVER's throw: onto the roof, about 1.5 m in from its edge.</summary>
        public const float OverFlingForward = 5.5f, OverFlingUp = 4.0f, OverHold = 0.28f;
        /// <summary>WALL's throw: a small hop off the face, and he falls or steers from there.</summary>
        public const float WallFlingUp = 2.0f;
        /// <summary>
        /// ⚠️ UNDER IS A REAL SWING (owner, 2026-10-07, of the first one: "it also doesnt feel spiderman swing-y.. you're
        /// unable to go under and further from the vine cluster because when you try to go under it, it just pulls you
        /// closer to the cluster"). The first shortened the vines all the way and let go a step past the catch. Now he
        /// is brought in to the BOTTOM of the swing (the vines <see cref="UnderRope"/> of the catch's height over his
        /// chest long, never under <see cref="UnderMinRope"/>), and from there the vines keep their length: a true arc
        /// up the far side to <see cref="UnderLetGo"/> radians past straight down, and he is thrown along it.
        /// </summary>
        public const float UnderRope = 0.65f, UnderMinRope = 1.5f, UnderLetGo = 0.75f;
        /// <summary>UNDER's throw is the arc's own speed and direction where it ends; its lift is capped, and the forward
        /// speed is held for this many times the time he is still rising (so past the top of the throw).</summary>
        public const float MaxFlingUp = 9.0f, UnderHold = 1.2f;
        private const float Gravity = 20.0f;

        /// <summary>
        /// The path for a catch <paramref name="ax"/> metres along the ground and <paramref name="ay"/> metres above
        /// his feet. Fills <paramref name="xs"/> and <paramref name="ys"/> (each at least <see cref="MaxSteps"/> long)
        /// and returns how many points; under 2 means no swing.
        /// </summary>
        public static int Plan(VineSwing kind, float ax, float ay, float[] xs, float[] ys,
                               out float flingX, out float flingY, out float hold)
        {
            flingX = flingY = hold = 0.0f;
            if (kind == VineSwing.Reel || xs == null || ys == null || xs.Length < MaxSteps || ys.Length < MaxSteps) return 0;
            if (float.IsNaN(ax) || float.IsNaN(ay) || float.IsInfinity(ax) || float.IsInfinity(ay)) return 0;
            ax = Math.Max(0.0f, Math.Min(ax, 40.0f));
            ay = Math.Max(0.0f, Math.Min(ay, 40.0f));

            // A curve from his feet, round a corner (cx, cy), to (ex, ey). OVER and WALL end there. UNDER's curve ends
            // level at the bottom of the swing, straight under the catch, and an arc on the vines carries on from it.
            float ex, ey, cx, cy, top, rope = 0.0f;
            if (kind == VineSwing.Under)
            {
                float over = ay - Chest;
                rope = Math.Max(UnderMinRope, over * UnderRope);
                ex = ax; ey = over - rope;
                cx = ax * 0.5f; cy = ey;
                top = UnderSpeed;
            }
            else
            {
                ex = Math.Max(0.0f, ax - StandOff);
                ey = kind == VineSwing.Over ? ay + OverClear : Math.Max(0.3f, ay - 1.1f);
                cx = ex * 0.85f;
                cy = ey * 0.10f;
                top = TopSpeed;
            }

            xs[0] = 0.0f; ys[0] = 0.0f;
            int count = 1;
            float t = 0.0f, u = 0.0f;
            while (u < 1.0f && count < MaxSteps)
            {
                t += Step;
                float speed = Math.Min(top, StartSpeed + SpeedGain * t);
                // The curve's own speed at u, so `speed x Step` metres is this much of u.
                float dx = 2 * (1 - u) * cx + 2 * u * (ex - cx);
                float dy = 2 * (1 - u) * cy + 2 * u * (ey - cy);
                u = Math.Min(1.0f, u + speed * Step / Math.Max(0.5f, (float)Math.Sqrt(dx * dx + dy * dy)));
                xs[count] = 2 * (1 - u) * u * cx + u * u * ex;
                ys[count] = 2 * (1 - u) * u * cy + u * u * ey;
                count++;
            }
            float swung = 0.0f;
            while (kind == VineSwing.Under && swung < UnderLetGo && count < MaxSteps)
            {
                t += Step;
                swung = Math.Min(UnderLetGo, swung + Math.Min(top, StartSpeed + SpeedGain * t) * Step / rope);
                xs[count] = ex + rope * (float)Math.Sin(swung);
                ys[count] = ey + rope * (1.0f - (float)Math.Cos(swung));
                count++;
            }

            if (kind == VineSwing.Over) { flingX = OverFlingForward; flingY = OverFlingUp; hold = OverHold; }
            else if (kind == VineSwing.Wall) { flingY = WallFlingUp; }
            else
            {
                float speed = Math.Min(top, StartSpeed + SpeedGain * t);
                flingY = Math.Min(MaxFlingUp, speed * (float)Math.Sin(swung));
                flingX = speed * (float)Math.Cos(swung);
                hold = UnderHold * flingY / Gravity;
            }
            return count;
        }

        [ThreadStatic] private static float[] _xs, _ys;

        /// <summary>How long he is on the vines for this catch: what the host tells every peer's vines to last.</summary>
        public static float Seconds(VineSwing kind, float ax, float ay)
        {
            _xs ??= new float[MaxSteps]; _ys ??= new float[MaxSteps];
            int count = Plan(kind, ax, ay, _xs, _ys, out _, out _, out _);
            return count < 2 ? 0.0f : (count - 1) * Step;
        }
    }
}
