using System;

namespace TumbangPreso.Core
{
    /// <summary>
    /// Zack, the speedster (owner's V3 skill sheet on the Wiki, approved 2026-10-25; `docs/reports/zack-kit-2026-10-25/plan.md`).
    /// MOMENTUM, BACKTRACK, AFTERIMAGE, LIVE WIRE and OVERCLOCK. Every number is the plan's starting value and is asserted by
    /// `Core.Tests/ZackRulesTests.cs`. Anything every peer must agree on is a pure function here: the wire's crossing test,
    /// its sag, the copies' fan and the Backtrack history window.
    /// </summary>
    public static class ZackRules
    {
        // ------------------------------------------------------------------ MOMENTUM (passive)

        /// <summary>Owner's sheet: *"up to 20% faster after 3 seconds"*.</summary>
        public const float MomentumMaxBonus = 0.20f;
        public const float MomentumRampSeconds = 3.0f;

        /// <summary>Running means moving at least this fast on the court (a walk is 2.5, a jog more).</summary>
        public const float MomentumRunSpeed = 1.5f;

        /// <summary>A stop is slower than <see cref="MomentumRunSpeed"/> for longer than this: one slow footstep is not a stop.</summary>
        public const float MomentumStopGrace = 0.15f;

        /// <summary>The movement multiplier after this many seconds of uninterrupted running: 1.0 to 1.2, linear over 3 s.</summary>
        public static float MomentumScale(float seconds)
            => 1.0f + MomentumMaxBonus * Clamp01(Finite(seconds) ? seconds / MomentumRampSeconds : 0.0f);

        /// <summary>The visible step (0 none, 1 at one second, 2 at two, 3 full), drawn on his heels and hands.</summary>
        public static int MomentumStep(float seconds)
            => !(seconds >= 1.0f) ? 0 : seconds < 2.0f ? 1 : seconds < MomentumRampSeconds ? 2 : 3;

        /// <summary>
        /// One step of the passive: running builds it (capped at the ramp), a stop longer than the grace resets it. Returns the new
        /// running seconds; <paramref name="stoppedFor"/> carries how long he has been under the run speed.
        /// </summary>
        public static float StepMomentum(float seconds, ref float stoppedFor, bool running, float dt)
        {
            if (!Finite(dt) || dt <= 0.0f) return Finite(seconds) ? Clamp(seconds, 0.0f, MomentumRampSeconds) : 0.0f;
            if (!Finite(seconds)) seconds = 0.0f;
            if (running)
            {
                stoppedFor = 0.0f;
                return Math.Min(MomentumRampSeconds, Math.Max(0.0f, seconds) + dt);
            }
            stoppedFor = (Finite(stoppedFor) ? Math.Max(0.0f, stoppedFor) : 0.0f) + dt;
            return stoppedFor > MomentumStopGrace ? 0.0f : Clamp(seconds, 0.0f, MomentumRampSeconds);
        }

        // ------------------------------------------------------------------ BACKTRACK (signature)

        /// <summary>Owner's sheet: *"Snap back to where you were 3 seconds ago"*, 30 s cooldown.</summary>
        public const float BacktrackSeconds = 3.0f;
        public const float BacktrackCooldown = 30.0f;

        /// <summary>Overclocked: *"Backtrack recharges in 3 seconds"*.</summary>
        public const float BacktrackOverclockedCooldown = 3.0f;

        /// <summary>Early in a round with less history than 3 s he goes to the oldest point, if he has at least this much.</summary>
        public const float BacktrackMinHistory = 0.5f;

        /// <summary>A snap shorter than this goes nowhere: the cast is refused rather than spending 30 s on nothing.</summary>
        public const float BacktrackMinDistance = 0.5f;

        /// <summary>Every peer samples each Zack's flat position this often, and keeps this much of it.</summary>
        public const float BacktrackSampleInterval = 0.05f;
        public const float BacktrackHistoryKeep = 4.0f;

        /// <summary>
        /// The host's check of an owner's chosen spot: within this flat distance of where the HOST saw that body anywhere from
        /// <see cref="BacktrackWindowEarly"/> to <see cref="BacktrackWindowLate"/> seconds ago. It is wide enough for a peer's
        /// pose stream to lag the owner by a few hundred milliseconds and narrow enough that nobody can name an arbitrary spot.
        /// </summary>
        public const float BacktrackTolerance = 2.5f;
        public const float BacktrackWindowEarly = 2.5f;
        public const float BacktrackWindowLate = 3.5f;

        /// <summary>How far back the owner looks: 3 s, or all of the round's history if that is shorter (but at least 0.5 s).</summary>
        public static float BacktrackLookBack(float historySeconds)
            => !Finite(historySeconds) || historySeconds < BacktrackMinHistory ? -1.0f : Math.Min(BacktrackSeconds, historySeconds);

        public static float BacktrackCooldownFor(bool overclocked) => overclocked ? BacktrackOverclockedCooldown : BacktrackCooldown;

        // ------------------------------------------------------------------ AFTERIMAGE (attacking)

        /// <summary>Owner's sheet: *"running straight ahead for 3 seconds"*, 30 s cooldown.</summary>
        public const float AfterimageCooldown = 30.0f;
        public const float AfterimageSeconds = 3.0f;

        /// <summary>It runs an attacker's run (`Balance.AttackerRunSpeed`), so 15 m at most: the box's diagonal.</summary>
        public const float AfterimageSpeed = Balance.AttackerRunSpeed;
        public static float AfterimageReach => AfterimageSpeed * AfterimageSeconds;

        /// <summary>*"It vanishes if the taya touches it"*: the taya's feet within this flat distance of the copy's.</summary>
        public const float AfterimagePopRadius = 0.8f;

        /// <summary>When its path ends early (a wall, the court's edge) it runs on the spot this long, then fizzles.</summary>
        public const float AfterimageStallSeconds = 0.25f;

        /// <summary>Overclocked: *"Afterimage sends two copies"*, fanned this many degrees either side of his aim.</summary>
        public const float AfterimageSpreadDegrees = 14.0f;

        public static int AfterimageCopies(bool overclocked) => overclocked ? 2 : 1;

        /// <summary>The yaw, in degrees from his aim, of copy <paramref name="index"/> of <paramref name="count"/>.</summary>
        public static float AfterimageYaw(int index, int count)
            => count <= 1 ? 0.0f : (index <= 0 ? -AfterimageSpreadDegrees : AfterimageSpreadDegrees);

        /// <summary>How far along its path a copy is at <paramref name="age"/>, for a path <paramref name="length"/> long.</summary>
        public static float AfterimageTravel(float age, float length)
            => !Finite(age) || !Finite(length) ? 0.0f : Clamp(age * AfterimageSpeed, 0.0f, Math.Max(0.0f, length));

        /// <summary>A copy's whole life: its run to the end of its path (or the full 3 s), and the stall if the path ended early.</summary>
        public static float AfterimageLife(float length)
        {
            if (!Finite(length) || length <= 0.0f) return AfterimageStallSeconds;
            float run = Math.Min(AfterimageSeconds, length / AfterimageSpeed);
            return length < AfterimageReach - 0.01f ? run + AfterimageStallSeconds : AfterimageSeconds;
        }

        // ------------------------------------------------------------------ LIVE WIRE (defending)

        /// <summary>Owner's sheet: *"between two points up to 6 metres apart"*, *"Lasts 10 seconds"*, 30 s cooldown.</summary>
        public const float LiveWireCooldown = 30.0f;
        public const float LiveWireSeconds = 10.0f;
        public const float LiveWireMaxLength = 6.0f;
        public const float LiveWireMinLength = 1.5f;

        /// <summary>
        /// The wire hangs from the posts' tops and sags to its middle. 0.85 m is above any jump (`Balance.JumpVelocity` 5.75 under
        /// `CharacterGravity` 20 peaks at 0.83 m) so it cannot be hopped, and far below a Featherfall flyer, who passes over it.
        /// </summary>
        public const float LiveWirePostHeight = 1.15f;
        public const float LiveWireSagHeight = 0.85f;

        /// <summary>A body whose feet are higher than this above the court where it crosses goes over the wire (flight, a pad).</summary>
        public const float LiveWireReachAbove = 1.6f;

        /// <summary>Both posts stand within this height of his floor (no wire strung up a staircase).</summary>
        public const float LiveWireFloorTolerance = 0.3f;

        /// <summary>The wire's height above the court at <paramref name="t"/> (0 one post, 1 the other): a parabola.</summary>
        public static float LiveWireHeightAt(float t)
        {
            t = Clamp01(Finite(t) ? t : 0.0f);
            return LiveWirePostHeight - (LiveWirePostHeight - LiveWireSagHeight) * 4.0f * t * (1.0f - t);
        }

        public static bool LiveWireLengthValid(float length)
            => Finite(length) && length >= LiveWireMinLength - 0.001f && length <= LiveWireMaxLength + 0.001f;

        /// <summary>
        /// ⚠️ THE ONE CROSSING TEST, on the court's plane: the step from (p0) to (p1) crosses the wire from (a) to (b). A body must
        /// go from strictly one side to strictly the other, through the segment between the posts (never round its ends), so a
        /// body standing on the line does not cross on every frame. <paramref name="along"/> is where on the wire (0 to 1).
        /// </summary>
        public static bool Crossed(float ax, float az, float bx, float bz,
                                   float p0x, float p0z, float p1x, float p1z, out float along)
        {
            along = 0.0f;
            if (!Finite(ax) || !Finite(az) || !Finite(bx) || !Finite(bz)
                || !Finite(p0x) || !Finite(p0z) || !Finite(p1x) || !Finite(p1z)) return false;
            float wx = bx - ax, wz = bz - az;
            float side0 = wx * (p0z - az) - wz * (p0x - ax);
            float side1 = wx * (p1z - az) - wz * (p1x - ax);
            if (side0 == 0.0f || side1 == 0.0f || (side0 < 0.0f) == (side1 < 0.0f)) return false;
            float mx = p1x - p0x, mz = p1z - p0z;
            float denominator = wx * mz - wz * mx;
            if (Math.Abs(denominator) < 1e-9f) return false;
            // Where the step meets the wire's line, as a share of the wire.
            float t = ((p0x - ax) * mz - (p0z - az) * mx) / denominator;
            if (t < 0.0f || t > 1.0f) return false;
            along = t;
            return true;
        }

        /// <summary>Which side of the wire a point is on: +1, -1, or 0 on the line.</summary>
        public static int Side(float ax, float az, float bx, float bz, float px, float pz)
        {
            float s = (bx - ax) * (pz - az) - (bz - az) * (px - ax);
            return s > 0.0f ? 1 : s < 0.0f ? -1 : 0;
        }

        // ------------------------------------------------------------------ OVERCLOCK (ultimate) and OVERCLOCKED (status)

        /// <summary>Owner's sheet: 15 objective points; *"Zack becomes Overclocked for 25 seconds"*.</summary>
        public const float OverclockCost = 15.0f;
        public const float OverclockSeconds = 25.0f;

        /// <summary>Overclocked: *"Moves 40% faster"*. It multiplies with Momentum (owner, 2026-10-25: stack, 1.68 at most).</summary>
        public const float OverclockSpeedScale = 1.4f;

        /// <summary>His whole kit speed scale: Momentum times Overclocked.</summary>
        public static float SpeedScale(float momentumSeconds, bool overclocked)
            => MomentumScale(momentumSeconds) * (overclocked ? OverclockSpeedScale : 1.0f);

        // ------------------------------------------------------------------ helpers

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static float Clamp01(float value) => value < 0.0f ? 0.0f : value > 1.0f ? 1.0f : value;
        private static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;
    }
}
