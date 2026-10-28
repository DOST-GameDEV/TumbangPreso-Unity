using System;

namespace TumbangPreso.Core
{
    /// <summary>
    /// Zack, the speedster (owner's V3 skill sheet on the Wiki, approved 2026-10-25; `docs/reports/zack-kit-2026-10-25/plan.md`).
    /// MOMENTUM, BACKTRACK, FULL THROTTLE (Afterimage until 2026-10-26), LIVE WIRE and OVERCLOCK, refined from the owner's 2026-10-26 feedback (the V4
    /// note on the Wiki's Zack Kit Rework tab). Every number is a starting value and is asserted by
    /// `Core.Tests/ZackRulesTests.cs`. Anything every peer must agree on is a pure function here: the wire's crossing test,
    /// its sag, the copies' fan and the Backtrack history window.
    /// </summary>
    public static class ZackRules
    {
        // ------------------------------------------------------------------ MOMENTUM (passive) and his base speed

        /// <summary>
        /// Owner, 2026-10-26: *"Zack feels too slow. Increase his actual speed modestly and make Momentum build faster."* He is
        /// the one hero whose ordinary locomotion is quicker than the shared role speeds, by this much, always.
        /// </summary>
        public const float BaseSpeedScale = 1.08f;

        /// <summary>Up to 25% faster after 2 seconds (was 20% after 3; owner: its benefit barely registered).</summary>
        public const float MomentumMaxBonus = 0.25f;
        public const float MomentumRampSeconds = 2.0f;

        /// <summary>Running means moving at least this fast on the court (a walk is 2.5, a jog more).</summary>
        public const float MomentumRunSpeed = 1.5f;

        /// <summary>
        /// A stop is slower than <see cref="MomentumRunSpeed"/> for longer than this. A hard cut or a reversal dips the speed
        /// for a moment; that dip is not a stop, so a weaving run keeps its Momentum.
        /// </summary>
        public const float MomentumStopGrace = 0.25f;

        /// <summary>The three visible stages, in seconds of running: the thirds of the ramp.</summary>
        public const float MomentumStageOne = 0.7f, MomentumStageTwo = 1.4f;

        /// <summary>The Momentum multiplier after this many seconds of uninterrupted running: 1.0 to 1.25, linear over 2 s.</summary>
        public static float MomentumScale(float seconds)
            => 1.0f + MomentumMaxBonus * Clamp01(Finite(seconds) ? seconds / MomentumRampSeconds : 0.0f);

        /// <summary>The visible stage (0 none, 1 at 0.7 s, 2 at 1.4 s, 3 full at 2 s), drawn on his run, heels and hands.</summary>
        public static int MomentumStep(float seconds)
            => !(seconds >= MomentumStageOne) ? 0 : seconds < MomentumStageTwo ? 1 : seconds < MomentumRampSeconds ? 2 : 3;

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

        // ------------------------------------------------------------------ FULL THROTTLE (attacking)

        /// <summary>
        /// Owner, 2026-10-26: *"i approve full throttle"* (option 1 of the Afterimage replacements in
        /// `docs/reports/zack-kit-2026-10-25/refinement-2026-10-26.md`). One tap: Momentum is full at once, Charge is full
        /// at once, and for 3 s Momentum cannot run out. A stop, a throw or a snap does not cost it inside the lock; a hit,
        /// a stun, a trip, a root or a tag still does, and ends the lock with it.
        /// </summary>
        public const float FullThrottleCooldown = 24.0f;
        public const float FullThrottleSeconds = 3.0f;

        /// <summary>Overclocked: *"every skill overclocked"*. Full Throttle recharges in this long instead.</summary>
        public const float FullThrottleOverclockedCooldown = 8.0f;

        public static float FullThrottleCooldownFor(bool overclocked)
            => overclocked ? FullThrottleOverclockedCooldown : FullThrottleCooldown;

        /// <summary>
        /// One step of the passive under Full Throttle's lock: full, whatever he does with his feet. Outside the lock it is
        /// <see cref="StepMomentum"/> exactly.
        /// </summary>
        public static float StepMomentum(float seconds, ref float stoppedFor, bool running, float dt, bool throttled)
        {
            if (!throttled) return StepMomentum(seconds, ref stoppedFor, running, dt);
            stoppedFor = 0.0f;
            return MomentumRampSeconds;
        }

        // ------------------------------------------------------------------ AFTERIMAGE (retired attacking skill)

        // ⚠️ RETIRED 2026-10-26 for FULL THROTTLE. Nothing casts it; the numbers stay because replay clips of schema 16 and 17
        // still restore and draw its copies (`RecordedFieldView`, `ZackAfterimage.Valid`), and world-effect kind 20 stays reserved.

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

        /// <summary>
        /// Owner, 2026-10-26: the wire STAYS on the court after it trips someone, until the round ends or he reels it in. The
        /// cooldown runs from each cast; with one wire that outlives it, it only gates moving the wire, so it is 20 s, not 30.
        /// </summary>
        public const float LiveWireCooldown = 20.0f;
        public const float LiveWireMaxLength = 6.0f;
        public const float LiveWireMinLength = 1.5f;

        /// <summary>
        /// NOT AN EXPIRY. A round is `Balance.RoundTime` (90 s) and round end retires every wire, so this bound is never
        /// reached in play; it exists so a snapshot's lifetime fields stay finite and validated like every other field's.
        /// </summary>
        public const float LiveWireLifeBound = 600.0f;

        /// <summary>After a normal wire Zaps someone it goes dark (harmless, visibly unlit) this long, then rearms.</summary>
        public const float LiveWireDarkSeconds = 5.0f;

        /// <summary>An overcharged wire never goes dark, but the attacker it just Zapped cannot be Zapped by it again for this long.</summary>
        public const float LiveWireOverchargeGrace = 3.0f;

        /// <summary>Reeling it in: look at your own wire within this reach and hold the skill this long.</summary>
        public const float LiveWireReelRange = 4.0f;
        public const float LiveWireReelHoldSeconds = 0.35f;

        /// <summary>How close the look ray must pass to the wire (flat metres) to count as looking at it.</summary>
        public const float LiveWireReelAimTolerance = 0.6f;

        /// <summary>The host's allowance on the owner's look and reach for a reel it times itself (poses cross the wire late).</summary>
        public const float LiveWireReelLeeway = 0.75f;

        /// <summary>A wire that tripped someone at <paramref name="dischargedAt"/> (0 never) is dark at <paramref name="age"/>.</summary>
        public static bool LiveWireDark(float dischargedAt, float age)
            => Finite(dischargedAt) && Finite(age) && dischargedAt > 0.0f && age >= dischargedAt
               && age - dischargedAt < LiveWireDarkSeconds;

        /// <summary>
        /// The flat distance from the look ray (origin <paramref name="ox"/>,<paramref name="oz"/>, flat direction
        /// <paramref name="dx"/>,<paramref name="dz"/>) to the wire segment, counting only the part of the ray within
        /// <paramref name="reach"/>. Infinity when no part of the wire is ahead within reach. Owner and host compute it alike.
        /// </summary>
        public static float LookToWire(float ox, float oz, float dx, float dz, float reach,
                                       float ax, float az, float bx, float bz)
        {
            if (!Finite(ox) || !Finite(oz) || !Finite(dx) || !Finite(dz) || !Finite(reach)
                || !Finite(ax) || !Finite(az) || !Finite(bx) || !Finite(bz)) return float.PositiveInfinity;
            float len = (float)Math.Sqrt(dx * dx + dz * dz);
            if (len < 1e-5f || reach <= 0.0f) return float.PositiveInfinity;
            dx /= len; dz /= len;
            float best = float.PositiveInfinity;
            const int samples = 32;
            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                float px = ax + (bx - ax) * t - ox, pz = az + (bz - az) * t - oz;
                float along = px * dx + pz * dz;
                if (along < 0.0f || along > reach) continue;
                float side = Math.Abs(px * dz - pz * dx);
                if (side < best) best = side;
            }
            return best;
        }

        /// <summary>An ankle-height tension wire on floor cleats. A timed jump clears the visible line.</summary>
        public const float LiveWirePostHeight = 0.28f;
        public const float LiveWireSagHeight = 0.24f;

        /// <summary>Feet above the wire plus a small body-contact allowance pass over it.</summary>
        public const float LiveWireReachAbove = 0.40f;

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

        // ------------------------------------------------------------------ CHARGE (his own energy, in the stamina bar's place)

        /// <summary>
        /// Owner, 2026-10-26: *"his own energy system and a distinct meter in the existing stamina location"*. The pool keeps the
        /// shared capacity (`Balance.StaminaMax`) so every resource snapshot and paid verb reads the same units; what is his is
        /// how it spends and refills. A full bar is about 4.2 s of sprint (the shared one is 2.5), it starts refilling 0.6 s
        /// after the sprint ends (1.0) and refills in 2 s (2.5). While Overclocked his sprint costs nothing and the same bar
        /// shows Overclock fuel instead, so there is never a second meter.
        /// </summary>
        public const float ChargeDrainRate = 60.0f;
        public const float ChargeRegenDelay = 0.6f;
        public const float ChargeRegenRate = 125.0f;

        public static StaminaProfile ChargeProfile(bool overclocked)
            => new StaminaProfile(ChargeDrainRate, ChargeRegenDelay, ChargeRegenRate, overclocked);

        // ------------------------------------------------------------------ OVERCLOCK (ultimate) and OVERCLOCKED (status)

        /// <summary>15 objective points. Overclock runs on fuel: 25 seconds of it, burning 1 per second on its own.</summary>
        public const float OverclockCost = 15.0f;
        public const float OverclockSeconds = 25.0f;

        /// <summary>Overclocked moves 30% faster (was 40%: his base speed and the faster Momentum carry the rest).</summary>
        public const float OverclockSpeedScale = 1.3f;

        /// <summary>His whole kit speed scale: his base, Momentum and Overclocked, multiplied.</summary>
        public static float SpeedScale(float momentumSeconds, bool overclocked)
            => BaseSpeedScale * MomentumScale(momentumSeconds) * (overclocked ? OverclockSpeedScale : 1.0f);

        // ------------------------------------------------------------------ THE OVERCLOCK ZAP (hold the ultimate while Overclocked)

        /// <summary>
        /// Owner, 2026-10-26: *"While Overclocked, HOLD X and aim at an enemy to channel the zap, spending fuel. It should stop on
        /// release or fuel exhaustion."* A rival within this range whose chest he can see.
        /// </summary>
        public const float ZapRange = 9.0f;

        /// <summary>A rival this close to the crosshair (degrees) is taken; once taken, he stays held out to the wider angle.</summary>
        public const float ZapAcquireDegrees = 8.0f;
        public const float ZapHoldDegrees = 16.0f;

        /// <summary>The host's allowance on the owner's aim (its pose stream lags): a held target must stay inside this cone.</summary>
        public const float ZapHostDegrees = 26.0f;

        /// <summary>The host's allowance on range for the same reason.</summary>
        public const float ZapHostRangeLeeway = 0.75f;

        /// <summary>Held in the zap this long without a real break, a rival is Zapped (`StatusRules.ZappedSeconds`).</summary>
        public const float ZapLockSeconds = 1.0f;

        /// <summary>A break in the beam shorter than this keeps the lock's progress; longer, it starts again.</summary>
        public const float ZapLockGrace = 0.3f;

        /// <summary>After the zap Zaps a rival, that rival is grounded against it this long (the beam still shows, harmlessly).</summary>
        public const float ZapImmunitySeconds = 3.0f;

        /// <summary>Extra fuel per second on top of Overclock's own 1 per second: while connected to a rival, and while searching.</summary>
        public const float ZapFuelConnected = 2.0f;
        public const float ZapFuelSearching = 0.5f;

        /// <summary>He walks while he channels: no sprint and this share of his speed.</summary>
        public const float ZapMoveScale = 0.6f;

        /// <summary>Fuel spent per second right now.</summary>
        public static float FuelBurn(bool zapping, bool connected)
            => 1.0f + (!zapping ? 0.0f : connected ? ZapFuelConnected : ZapFuelSearching);

        /// <summary>The lock's progress after a step: it builds while connected, holds through a short break, then resets.</summary>
        public static float StepZapLock(float progress, ref float brokenFor, bool connected, float dt)
        {
            if (!Finite(progress)) progress = 0.0f;
            if (!Finite(dt) || dt <= 0.0f) return Clamp(progress, 0.0f, ZapLockSeconds);
            if (connected) { brokenFor = 0.0f; return Math.Min(ZapLockSeconds, Math.Max(0.0f, progress) + dt); }
            brokenFor = (Finite(brokenFor) ? Math.Max(0.0f, brokenFor) : 0.0f) + dt;
            return brokenFor > ZapLockGrace ? 0.0f : Clamp(progress, 0.0f, ZapLockSeconds);
        }

        /// <summary>The angle in degrees between a flat aim and the flat direction to a target; 180 when either is degenerate.</summary>
        public static float FlatAngle(float aimX, float aimZ, float toX, float toZ)
        {
            float a = (float)Math.Sqrt(aimX * aimX + aimZ * aimZ), b = (float)Math.Sqrt(toX * toX + toZ * toZ);
            if (!Finite(a) || !Finite(b) || a < 1e-5f || b < 1e-5f) return 180.0f;
            float c = Clamp((aimX * toX + aimZ * toZ) / (a * b), -1.0f, 1.0f);
            return (float)(Math.Acos(c) * 180.0 / Math.PI);
        }

        // ------------------------------------------------------------------ helpers

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static float Clamp01(float value) => value < 0.0f ? 0.0f : value > 1.0f ? 1.0f : value;
        private static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;
    }
}
