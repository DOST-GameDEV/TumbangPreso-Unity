namespace TumbangPreso.Core
{
    /// <summary>
    /// Impulses, reach, and the stun rule.
    ///
    /// ⚠️⚠️ EVERY IMPULSE IN THIS GAME IS DERIVED FROM Friction, NEVER TYPED IN AS A
    /// DISTANCE. A body given speed v and decelerated at Friction travels v²/(2·Friction),
    /// which is v²/60 at the shipping value. Write the DISTANCE you want and solve for the
    /// speed; never hard-code a distance beside a speed, or moving Friction leaves one of
    /// the two silently wrong. This is why BlockKnockbackSpeed is 4.583 rather than "0.35":
    /// sqrt(0.35 × 60) is where that number comes from.
    /// </summary>
    public static class Combat
    {
        /// <summary>How far an impulse of this speed carries. v²/(2·Friction).</summary>
        public static float KnockbackDistance(float speed) =>
            speed * speed / (2.0f * Balance.Friction);

        /// <summary>The inverse: what speed lands a body exactly this far away.</summary>
        public static float SpeedForDistance(float distance) =>
            (float)System.Math.Sqrt(distance * 2.0f * Balance.Friction);

        // -------------------------------------------------------------------
        // THE SHOVE — attacker against attacker.
        //
        // ⚠️ THE DEFENDER CAN NEITHER SHOVE NOR BE SHOVED. They have the tag, and giving
        // them both would make the box unenterable.
        // -------------------------------------------------------------------

        /// <summary>2.50 m at the shipping constants.</summary>
        public static float ShoveDistance() => KnockbackDistance(Balance.ShoveSpeed);

        // -------------------------------------------------------------------
        // THE LUNGE — the taya's committed tag.
        // -------------------------------------------------------------------

        /// <summary>
        /// How far the dash carries at a given commitment. Power is clamped to
        /// [LungeMinPower, 1].
        ///
        /// ⚠️ A VELOCITY IMPULSE, NOT A TELEPORT, and the intervening frames are what the
        /// local sweep reads. It is also why the taya can be body-blocked mid-lunge
        /// instead of passing through geometry.
        /// </summary>
        public static float LungeDash(float power = 1.0f)
        {
            float p = power < Balance.LungeMinPower ? Balance.LungeMinPower : (power > 1.0f ? 1.0f : power);
            return KnockbackDistance(Balance.LungeSpeed * p);
        }

        /// <summary>
        /// The furthest a lunge started from here can still tag: the dash plus the sweep
        /// radius.
        ///
        /// ⚠️⚠️ THIS IS 2.30 m AT THE SHIPPING CONSTANTS, AND Design.md REPORTS 3.20 m AS
        /// MEASURED. Both cannot be true, and the code is the newer half. Design.md's own
        /// §6 constants TABLE agrees with the code (7.746, "a 1.0 m dash by v²/60"); it is
        /// the §6 prose and the §2.6 measurement that still describe LUNGE_SPEED 12.247
        /// and a 2.5 m dash, which is where 2.5 + 1.3 - the charge = 3.20 came from.
        ///
        /// So the lunge was cut by more than half at some point, the table was updated,
        /// and the probe was never re-run. This matters well beyond a doc tidy: the lunge
        /// is the taya's primary scoring verb, §2.6's "the tag is a lead problem, not a
        /// reach problem" conclusion was drawn at the old reach, and the tag's share of
        /// all points is one of the numbers the fairness gate watches.
        ///
        /// ⚠️ DO NOT "FIX" EITHER SIDE FROM THIS FILE. Port_Plan.md §7.1 carries it as a
        /// Phase 1 blocker: decide which is intended in the Godot repo, re-run mech_probe,
        /// and let the answer arrive here as a constant change.
        /// </summary>
        public static float LungeReach(float power = 1.0f) =>
            LungeDash(power) + Balance.LungeTagRadius;

        // -------------------------------------------------------------------
        // GEOMETRY — the shove and the punch are both a range plus an arc.
        // -------------------------------------------------------------------

        /// <summary>
        /// Is a target at this distance and this angle off the actor's facing inside a
        /// cone? <paramref name="halfAngleDeg"/> is the HALF-angle, matching
        /// ShoveArcDeg 70 and PunchArcDeg 75.
        /// </summary>
        public static bool InCone(float distance, float angleOffFacingDeg, float range, float halfAngleDeg)
        {
            if (distance > range) return false;
            float a = angleOffFacingDeg < 0 ? -angleOffFacingDeg : angleOffFacingDeg;
            return a <= halfAngleDeg;
        }

        public static bool InShoveCone(float distance, float angleOffFacingDeg) =>
            InCone(distance, angleOffFacingDeg, Balance.ShoveRange, Balance.ShoveArcDeg);

        public static bool InPunchCone(float distance, float angleOffFacingDeg) =>
            InCone(distance, angleOffFacingDeg, Balance.PunchRange, Balance.PunchArcDeg);

        // -------------------------------------------------------------------
        // THE BODY BLOCK — what a blocked slipper does to the blocker.
        // -------------------------------------------------------------------

        /// <summary>
        /// The push a body block deals, 0.35 m at neutral.
        ///
        /// ⚠️⚠️ THE TWO STAT TABLES COMPOSE HERE AND NEITHER KNOWS ABOUT THE OTHER: scaled
        /// by the THROWER's slipper IMPACT and divided by the BLOCKER's own person GRIT.
        /// A crocs thrown at Bebang (grit 5) barely moves her and the same throw rocks
        /// Jun-Jun (grit 2).
        ///
        /// ⚠️ A PUSH AND NOT A STUN, AND THAT WAS A DELIBERATE REVERSAL. A stagger was the
        /// obvious way to make blocking cost something and it is wrong here: three
        /// attackers throwing at one box would chain stuns onto the defender, and Max()
        /// bounds the DURATION of one stun without bounding how often the next one starts.
        /// Knockback costs the taya POSITION, which is the resource a body block is
        /// actually about, and it cannot lock anybody out of the game.
        /// </summary>
        public static float BlockKnockbackSpeed(int throwerSlipperIndex, int blockerPersonIndex)
        {
            return Balance.BlockKnockbackSpeed
                   * Roster.SlipperImpactScale(throwerSlipperIndex)
                   / Roster.PersonGritScale(blockerPersonIndex);
        }

        /// <summary>How far a blocked slipper deflects. ⚠️ Directed AWAY FROM THE BLOCKER
        /// rather than mirrored: a true reflection sends it wherever the incoming angle
        /// points, which is as often as not deeper into the box.</summary>
        public static float DeflectDistance(float launchSpeed) =>
            KnockbackDistance(launchSpeed * Balance.DeflectSpeedScale);

        /// <summary>How far a slipper comes off the can it just knocked over.</summary>
        public static float LataRecoilDistance(float launchSpeed, int canIndex) =>
            KnockbackDistance(launchSpeed * Balance.LataRecoilScale * Roster.CanReboundScale(canIndex));

        // -------------------------------------------------------------------
        // STUNS
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ STUNS OVERLAP RATHER THAN STACK, AND Max() IS THE ENTIRE BOUND. There is no
        /// additive path anywhere in the game. The only unbounded thing in a 1-vs-3 game
        /// is a stun chain: an additive path would let three attackers hold one taya, or
        /// one taya hold one attacker, indefinitely.
        ///
        /// ⚠️ ITS KNOWN COST IS ACCEPTED, NOT OVERLOOKED. A short stun landing inside a
        /// longer one is invisible on the HUD, so a 1.25 s shove stun inside the 5 s tag
        /// penalty reads as nothing happening. Both events already announce themselves
        /// through channels that are not the status stack (the shove has its own
        /// knockback, hit flash and swing sound; the tag has its own toast), so nothing is
        /// silent. Only the HUD ROW is merged.
        /// </summary>
        public static float ApplyStagger(float currentStunLeft, float incoming) =>
            currentStunLeft > incoming ? currentStunLeft : incoming;

        /// <summary>
        /// How long the taya's reset channel takes on a given can, divided by its RESET.
        /// 1.30 s on PASIP, 1.79 s on BOYBEN.
        ///
        /// ⚠️ THE CAN GOES BACK ON ITS MARK AND THEN STANDS UP, IN THAT ORDER. A lata that
        /// stands up where it was knocked to is a lata the next throw cannot miss.
        /// </summary>
        public static float ResetChannelFor(int canIndex) =>
            Balance.ResetChannelTime / Roster.CanResetScale(canIndex);
    }
}
