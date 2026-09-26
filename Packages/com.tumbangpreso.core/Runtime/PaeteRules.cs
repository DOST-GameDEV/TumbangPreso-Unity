namespace TumbangPreso.Core
{
    /// <summary>
    /// ⚠️⚠️ PAETE'S KIT, AS NUMBERS. The ninth hero, a plant (Dendro) from Mount Makiling, on the
    /// signature plus role ability shape (owner, 2026-09-25). The owner's table gave what each
    /// power does; his answers the same day settled the rest
    /// (`docs/reports/paete-kit-2026-09-25/plan.md` § 7). Everything else is chosen here, written
    /// as a distance or a time, and derived from `Balance.Friction` where it moves a body.
    ///
    /// ⚠️ IN CORE SO `dotnet test` ASSERTS THEM IN A SECOND. `PaeteHeroKit` and `PaeteHazards`
    /// read every value from here.
    /// </summary>
    public static class PaeteRules
    {
        // ------------------------------------------------------------------ LIANA LEAP (signature)

        /// <summary>Proposed 30 s, accepted with the other defaults (plan § 7).</summary>
        public const float VineCooldown = 30.0f;

        /// <summary>How far the vines reach for an anchor. 8 m is a little over half the 14 m box.</summary>
        public const float VineRange = 8.0f;

        /// <summary>
        /// The reel speed. Under `Balance.MaxKnockbackSpeed` (16) so the clamp never bites, and
        /// faster than anyone runs, so the pull reads as being pulled rather than as running.
        /// </summary>
        public const float VineReelSpeed = 14.0f;

        /// <summary>He stops this short of the anchor, so he lands beside a wall rather than in it.</summary>
        public const float VineStopShort = 0.8f;

        /// <summary>The swing's lift, m/s: enough to leave the ground in an arc, well under the cap.</summary>
        public const float VineLift = 3.0f;

        /// <summary>The tell: both arms draw back and the palms glow before the vines leave.</summary>
        public const float VineTellSeconds = 0.12f;

        /// <summary>How long the vines take to reach the anchor, as the beat before he is reeled.</summary>
        public const float VineReachSeconds = 0.14f;

        /// <summary>Hold time for a reel to an anchor <paramref name="distance"/> away (the stop-short taken off).</summary>
        public static float VineHoldSeconds(float distance)
            => CarryRules.SecondsFor(System.Math.Max(0.0f, distance - VineStopShort), VineReelSpeed);

        // ------------------------------------------------------------------ BAKYA BLOOM (attacking)

        /// <summary>The seed's throw range, from his feet.</summary>
        public const float PlantThrowRange = 6.0f;

        /// <summary>Cooldown on planting, from the cast. One plant at a time: a new one replaces the old.</summary>
        public const float PlantCooldown = 30.0f;

        /// <summary>From landing to its first wooden slipper: the pop-up and the first one growing.</summary>
        public const float PlantFirstShotSeconds = 3.0f;

        /// <summary>
        /// Owner's table: *"a cooldown of 10-20 seconds"*, read as the time between shots and
        /// accepted at the middle of his range (plan § 7, question 2).
        /// </summary>
        public const float PlantReloadSeconds = 15.0f;

        /// <summary>
        /// Owner, 2026-09-25: *"invincible for the first 15 seconds but after that make a visual
        /// indicator showing that it can be pulled out"*.
        /// </summary>
        public const float PlantRootedSeconds = 15.0f;

        /// <summary>
        /// ⚠️ NOT GIVEN BY THE OWNER. How long it lives in all. After its 15 rooted seconds it
        /// loosens visibly for 25 more and then withers on its own, so a plant nobody pulls cannot
        /// fire every 15 s for a whole 90 s round (that would be six wooden throws from one cast).
        /// </summary>
        public const float PlantLifeSeconds = 40.0f;

        /// <summary>How close an opponent must stand to pull it out, and how long they hold Interact.</summary>
        public const float PlantPullReach = 1.5f;
        public const float PlantPullSeconds = 1.2f;

        /// <summary>A wooden slipper's launch speed: a firm throw, not a pektus.</summary>
        public const float WoodenSlipperSpeed = 13.0f;

        /// <summary>
        /// Owner, on what a wooden slipper that knocks the lata is worth: *"maybe lessened plus"*.
        /// Half a knockdown, the sabotage tier, awarded through `MatchDirector.AddScore` as
        /// `ScoreEvent.SproutKnock`.
        /// </summary>
        public const int SproutKnockPoints = 50;

        /// <summary>How long a wooden slipper lies where it lands before it withers.</summary>
        public const float WoodenSlipperWitherSeconds = 2.0f;

        // ------------------------------------------------------------------ THORN HARVEST (defending)

        public const float ThornCooldown = 30.0f;

        /// <summary>Every slipper within this of his feet is caught (owner: *"ALL, even the ones on the hands"*).</summary>
        public const float ThornRange = 7.0f;

        /// <summary>The Scorpion beat: caught, held taut, then yanked.</summary>
        public const float ThornHoldSeconds = 0.25f;

        /// <summary>The yank itself, from wherever it was caught to beside him.</summary>
        public const float ThornYankSeconds = 0.5f;

        /// <summary>Where a yanked slipper lands: this far from the construct, so it never lands on him.</summary>
        public const float ThornLandDistance = 1.0f;

        /// <summary>The construct's whole life, from the stamp to the last thorn dropping.</summary>
        public const float ThornConstructSeconds = 3.0f;

        // ------------------------------------------------------------------ MAKILING'S EMBRACE (ultimate)

        /// <summary>Owner: *"yes"* to 16 objective points.</summary>
        public const float SentryCost = 16.0f;

        /// <summary>The seed's throw range.</summary>
        public const float SentryThrowRange = 8.0f;

        /// <summary>Owner: *"WITHIN 9 meters"*.</summary>
        public const float SentryRadius = 9.0f;

        /// <summary>
        /// A pulled body stops this far from the sentry's centre, standing against its trunk.
        /// ⚠️ 1.1 until 2026-09-26; the sentry grew to an imposing 4 m tree with a 1.3 m trunk base
        /// (owner: *"it doesnt make sense too that the characters get pulled to smth that small"*),
        /// so 1.1 would have stood the caught bodies inside the bark.
        /// </summary>
        public const float SentryHoldDistance = 1.9f;

        /// <summary>The pull's speed; the carry time is solved per body from its own distance.</summary>
        public const float SentryPullSpeed = 15.0f;

        /// <summary>The pull's beat before the drag, the same hold as the thorns.</summary>
        public const float SentryCatchSeconds = 0.3f;

        /// <summary>
        /// Owner: *"they have to hold for like 7 seconds while stuck here"*, with the general
        /// interact button; progress is kept when they let go (plan § 7).
        /// </summary>
        public const float BreakFreeHoldSeconds = 7.0f;

        /// <summary>
        /// ⚠️ PROPOSED, NOT OBJECTED TO. The sentry withers after this and frees anyone still
        /// held, so a player who never presses is not stuck for the rest of the round.
        /// </summary>
        public const float SentryLifeSeconds = 10.0f;

        /// <summary>
        /// ⚠️⚠️ THE PULL'S SPEED FOR A BODY <paramref name="distance"/> AWAY, SOLVED SO IT STOPS AT THE
        /// TRUNK. A fixed 15 m/s slides 15^2/(2 x 30) = 3.75 m after the hold alone, so anybody caught
        /// closer than that was flung past the sentry; the first played match (`PaeteKitPlayProbe`,
        /// 2026-09-26) also found the hold solving to a negative time for them. Near bodies get the
        /// speed whose slide is exactly their distance; far ones the full 15 m/s and a hold.
        /// </summary>
        public static float SentryPullSpeedFor(float distance)
        {
            float d = System.Math.Max(0.0f, distance - SentryHoldDistance);
            return System.Math.Min(SentryPullSpeed, (float)System.Math.Sqrt(2.0 * Balance.Friction * d));
        }

        /// <summary>Hold time for a pull from <paramref name="distance"/> away, at <see cref="SentryPullSpeedFor"/>.</summary>
        public static float SentryPullHoldSeconds(float distance)
        {
            float v = SentryPullSpeedFor(distance);
            return v <= 0.0f ? 0.0f : CarryRules.SecondsFor(System.Math.Max(0.0f, distance - SentryHoldDistance), v);
        }

        /// <summary>When the pulled body arrives: the hold, then the slide out against `Friction`.</summary>
        public static float SentryPullArriveSeconds(float distance)
            => SentryPullHoldSeconds(distance) + SentryPullSpeedFor(distance) / Balance.Friction;
    }
}
