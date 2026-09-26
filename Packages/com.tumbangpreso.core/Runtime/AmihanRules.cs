namespace TumbangPreso.Core
{
    /// <summary>
    /// ⚠️⚠️ AMIHAN'S KIT, AS NUMBERS. The first kit built for the ability overhaul (owner,
    /// 2026-09-25): a SIGNATURE that is the same in either role, a ROLE ability that is one thing
    /// attacking and another defending, and an ultimate. The owner's table gave the names, what
    /// each does and three of the costs; everything else is chosen here, written as a distance or
    /// a time, and derived from `Balance.Friction` where it moves a body
    /// (`docs/reports/amihan-kit-2026-09-25/plan.md` § 4).
    ///
    /// ⚠️ IN CORE SO `dotnet test` ASSERTS THEM IN A SECOND, beside the carry arithmetic they are
    /// solved with. `AmihanHeroKit` reads every value from here.
    /// </summary>
    public static class AmihanRules
    {
        // ------------------------------------------------------------------ QUICK DASH (signature)

        /// <summary>Owner's table: *"40 s cooldown"*.</summary>
        public const float QuickDashCooldown = 40.0f;

        /// <summary>How far the dash throws her, metres, along her facing.</summary>
        public const float QuickDashDistance = 5.0f;

        /// <summary>The held speed. Under `Balance.MaxKnockbackSpeed` so no clamp ever bites it.</summary>
        public const float QuickDashSpeed = 14.0f;

        /// <summary>A body this close to her swept path is hit. The lunge's own reach is 1.3 m;
        /// the dash is a thinner line because it is a pass, not a grab.</summary>
        public const float QuickDashHitRadius = 0.9f;

        /// <summary>*"slightly pushes back other players"*: 1.2 m, a quarter of the dash.</summary>
        public const float QuickDashPushDistance = 1.2f;

        public static float QuickDashHoldSeconds => CarryRules.SecondsFor(QuickDashDistance, QuickDashSpeed);
        public static float QuickDashPushSpeed => CarryRules.ImpulseFor(QuickDashPushDistance);

        // ------------------------------------------------------------------ UPDRAFT (attacking role)

        /// <summary>
        /// ⚠️ NOT GIVEN BY THE OWNER. 45 s sits beside her other two (35 and 40), and a flight is a
        /// move-your-own-body power, which `HeroAbility`'s charge rule gives a long cooldown.
        /// </summary>
        public const float UpdraftCooldown = 45.0f;

        /// <summary>Owner's table: *"Fly for 10 seconds."*</summary>
        public const float UpdraftSeconds = 10.0f;

        /// <summary>
        /// Owner, 2026-09-25: *"flies high"*. 2.8 m of air under her feet is well over a standing
        /// taya's head, so the flat tag reach cannot be read as reaching her, and under every
        /// covered part of the five maps (the LRT deck is the lowest roof over the court).
        /// </summary>
        public const float UpdraftHeight = 2.8f;
        public const float UpdraftRiseSeconds = 0.45f;

        /// <summary>*"cant pick up unless they choose to go down"*: the glide down, m/s.</summary>
        public const float UpdraftDescentSpeed = 3.5f;

        // ------------------------------------------------------------------ WHIRLWIND (defending role)

        /// <summary>Owner's table: *"35 s cooldown"*.</summary>
        public const float WhirlwindCooldown = 35.0f;

        /// <summary>Owner's table: *"The gale lasts 2.5 s."*</summary>
        public const float WhirlwindSeconds = 2.5f;

        /// <summary>*"as it swiftly moves forward"*: 5.5 m/s, faster than anyone runs (4.6 x 1.10),
        /// so it can catch a runner, and 13.75 m over its life, which crosses the whole court.</summary>
        public const float WhirlwindSpeed = 5.5f;

        /// <summary>The arc's chord. 3.2 m keeps it a lane, not a wall (VISION § 2).</summary>
        public const float WhirlwindWidth = 3.2f;

        /// <summary>How thick the front is. A body inside this band of the arc is hit.</summary>
        public const float WhirlwindDepth = 0.9f;

        /// <summary>Where the arc is born, in front of her.</summary>
        public const float WhirlwindStart = 1.0f;

        /// <summary>How far the arc bows forward at its middle.</summary>
        public const float WhirlwindBow = 0.9f;

        public static float WhirlwindTravel => WhirlwindSpeed * WhirlwindSeconds;

        // ------------------------------------------------------------------ STORM SURGE (ultimate)

        /// <summary>Owner's table: *"15 objective points"*.</summary>
        public const float StormSurgeCost = 15.0f;

        /// <summary>Owner's table: *"After a 2.5 s delay"*.</summary>
        public const float StormSurgeGatherSeconds = 2.5f;

        /// <summary>Half the fan's angle. 70 degrees across: wide enough to be a storm, narrow
        /// enough that stepping out of it sideways in 2.5 s is always possible.</summary>
        public const float StormSurgeHalfAngle = 35.0f;

        /// <summary>*"map-wide"*: longer than the longest playable diagonal of any map.</summary>
        public const float StormSurgeRange = 40.0f;

        /// <summary>
        /// Owner, 2026-09-25: *"very far, the rsn for this is we want them to fall off the map or
        /// pushed to the edge"*. 16 m is more than the whole playable width of every street map
        /// (2 x 8.6), so anyone caught ends at the edge, and on Sa Bubong goes over it.
        /// </summary>
        public const float StormSurgeDistance = 16.0f;
        public const float StormSurgeSpeed = 15.0f;

        /// <summary>A small lift so the carry reads as the wind picking them up, not a slide.</summary>
        public const float StormSurgeLift = 3.5f;

        /// <summary>Her own walk while the storm gathers: half speed, the storm is not hers to ride.</summary>
        public const float StormSurgeGatherSpeedScale = 0.5f;

        public static float StormSurgeHoldSeconds => CarryRules.SecondsFor(StormSurgeDistance, StormSurgeSpeed);

        /// <summary>A loose slipper caught in the storm: the same distance as a body, as one throw.</summary>
        public const float StormSurgeSlipperSpeed = 15.0f;
    }
}
