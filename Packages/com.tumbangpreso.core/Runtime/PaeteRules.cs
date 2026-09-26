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

        /// <summary>
        /// ⚠️⚠️ BAKYA BLOOM IS PLANTED OUTSIDE THE TAYA'S BOX ONLY (owner, 2026-09-26: *"also make paete's E only placeable
        /// outside box"*). A spot aimed inside the box is moved straight out across its NEAREST edge, to this far past the
        /// line, so the pot never overlaps the chalk; a spot already outside is untouched. Moved rather than refused: until the
        /// cast preview can show a red spot (CAST-1) a refused plant is a press that silently did nothing.
        /// </summary>
        public const float PlantBoxMargin = 0.6f;

        /// <summary>Where a plant aimed at (x, z) is allowed to land: outside the box (see <see cref="PlantBoxMargin"/>).</summary>
        public static void PlantSpotOutsideBox(ref float x, ref float z, float radius = Balance.ConfinementRadius)
        {
            if (!Confinement.IsInsideBox(x, z, radius)) return;
            float edge = radius + PlantBoxMargin;
            // The nearest edge: whichever axis is already closest to the line.
            if (System.Math.Abs(x) >= System.Math.Abs(z)) x = x >= 0f ? edge : -edge;
            else z = z >= 0f ? edge : -edge;
        }

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

        /// <summary>
        /// ⚠️⚠️ PLACED WHERE HE LOOKS, NOT ON HIS BODY (owner, 2026-09-26: *"I WANT IT to be castable and not cast on body make
        /// it possible for him to place it somewhere else like his ult and other skill"*). Held, the spot follows his sight line
        /// between these two; released, the rattan bursts there, and the 7 m reach and the 1 m landing are measured from THAT
        /// spot. PROPOSED, NOT OBJECTED TO: the far end is BAKYA BLOOM's 6 m (`PlantThrowRange`), so his two placed plants reach
        /// the same distance; the near end is his own feet, so the old stamp-on-the-spot cast is still one aim away.
        /// </summary>
        public const float ThornAimMinRange = 0.0f;
        public const float ThornAimRange = PlantThrowRange;

        /// <summary>
        /// ⚠️ NOTHING APPEARS FROM EMPTY AIR (`HERO_KIT_METHOD.md` section 0, rule 4): a line of thorn shoots races through the
        /// court from his stamp to the spot, and the rattan bursts when it arrives. 24 m/s puts the far end (6 m) 0.25 s out,
        /// one hold beat, so a thrown slipper is not given a quarter of a second more than it had. Closer than
        /// `ThornTrailMinDistance` there is no trail: the rattan comes up under his stamp as it always did.
        /// </summary>
        public const float ThornTrailSpeed = 24.0f;
        public const float ThornTrailMinDistance = 0.6f;

        /// <summary>How long the thorns take to reach a spot this far from his stamp. Every peer computes it from the accepted cast.</summary>
        public static float ThornTrailSeconds(float distance)
            => distance <= ThornTrailMinDistance ? 0.0f : distance / ThornTrailSpeed;

        // ------------------------------------------------------------------ MAKILING'S EMBRACE (ultimate)

        /// <summary>Owner: *"yes"* to 16 objective points.</summary>
        public const float SentryCost = 16.0f;

        /// <summary>The seed's throw range: the furthest he can place the guardian (the hold-to-aim ring's far end).</summary>
        public const float SentryThrowRange = 8.0f;

        /// <summary>
        /// The nearest he can place it (owner, 2026-09-27: *"make it so that paete can choose as well where his ult will be cast"*;
        /// PROPOSED, NOT OBJECTED TO). 3 m: the guardian's roots reach 2.14 m at its scale (`SentryCanClearance`'s measurement), so
        /// nearer than 3 m it would come up with its roots under his own knees.
        /// </summary>
        public const float SentryAimMinRange = 3.0f;

        /// <summary>Owner: *"WITHIN 9 meters"*.</summary>
        public const float SentryRadius = 9.0f;

        /// <summary>
        /// A pulled body stops this far from the sentry's centre, standing against its trunk.
        /// ⚠️ 1.1 until 2026-09-26; the sentry grew to an imposing 4 m tree with a 1.3 m trunk base
        /// (owner: *"it doesnt make sense too that the characters get pulled to smth that small"*),
        /// so 1.1 would have stood the caught bodies inside the bark.
        /// ⚠️⚠️ 1.9 until 2026-09-27, when the tree was made smaller and sleek (owner, on the v5 film:
        /// *"Make the tre a bit smaller and a lot more sleek so that it isnt too distracting"*). The v9
        /// tree's root knuckles are 1.17 m out at shin height (was 1.7 m), measured off the built glb at
        /// its 1.3 scale, so at 1.9 the prisoners stood clear of the wood and read as standing NEAR the
        /// tree, which is the owner's older complaint (*"make it seem more apparent that the people tied
        /// to the tree are actually TIED"*). At 1.4 a body's back (0.3 m of it) is against the roots.
        /// </summary>
        public const float SentryHoldDistance = 1.4f;

        /// <summary>
        /// ⚠️⚠️ THE GUARDIAN NEVER STANDS ON THE CAN (owner, 2026-09-27: *"make it so that it cant block the
        /// can too (dont let it be placed in a place it STANDS on can)"*). The tree's centre is kept this far
        /// from the lata. The sum, measured off the built v9 `sentry.glb` at its 1.3 scale: the claw roots and
        /// the heave of soil at each toe reach 2.14 m out at most, the can is 0.12 m across its base, and the
        /// last 0.2 m keeps the court breaking round the toes off it; a prisoner held on the can's side (1.4 m
        /// hold plus 0.35 m of them) also ends up clear of it.
        /// </summary>
        public const float SentryCanClearance = 2.4f;

        /// <summary>
        /// ⚠️⚠️ WHERE THE GUARDIAN STANDS, KEPT OFF THE CAN. A spot inside <see cref="SentryCanClearance"/> of the
        /// can is pushed straight out from it to that distance, so it lands as near where it was aimed as it can;
        /// aimed exactly at the can, it is pushed back toward the caster (<paramref name="fromX"/>, <paramref name="fromZ"/>).
        /// If the box (half sizes <paramref name="halfX"/>, <paramref name="halfZ"/>) cuts that spot off, as when the
        /// can lies against a wall, the other three quarter turns round the can are tried in a fixed order and the
        /// first that is both clear and inside wins, so every peer computing it from the same inputs agrees.
        ///
        /// ⚠️ IN CORE, NOT IN THE HAZARD: the host, every peer and every bot run it on the same numbers, and
        /// `dotnet test` proves it in a second. The push is continuous in the can's position everywhere except
        /// exactly on the can, so a peer whose knocked-down can sits a few centimetres from the host's puts the
        /// tree a few centimetres from the host's, never on the other side of it.
        /// </summary>
        public static void SentrySpotClearOfCan(float spotX, float spotZ, float canX, float canZ, float fromX, float fromZ,
                                                float halfX, float halfZ, out float x, out float z)
        {
            x = spotX; z = spotZ;
            float dx = spotX - canX, dz = spotZ - canZ;
            float d = (float)System.Math.Sqrt(dx * dx + dz * dz);
            if (d >= SentryCanClearance) return;
            float ux, uz;
            if (d > 1e-3f) { ux = dx / d; uz = dz / d; }
            else
            {
                float bx = fromX - canX, bz = fromZ - canZ;
                float b = (float)System.Math.Sqrt(bx * bx + bz * bz);
                if (b > 1e-3f) { ux = bx / b; uz = bz / b; } else { ux = 0f; uz = 1f; }
            }
            // Straight out, then a quarter turn each way, then straight back: the first that is clear and inside wins.
            float[] tryX = { ux, -uz, uz, -ux }, tryZ = { uz, ux, -ux, -uz };
            float bestX = spotX, bestZ = spotZ, bestGap = -1f;
            for (int i = 0; i < 4; i++)
            {
                float cx = Clamp(canX + tryX[i] * SentryCanClearance, -halfX, halfX);
                float cz = Clamp(canZ + tryZ[i] * SentryCanClearance, -halfZ, halfZ);
                float gx = cx - canX, gz = cz - canZ;
                float gap = (float)System.Math.Sqrt(gx * gx + gz * gz);
                if (gap >= SentryCanClearance - 1e-3f) { x = cx; z = cz; return; }
                if (gap > bestGap) { bestGap = gap; bestX = cx; bestZ = cz; }
            }
            x = bestX; z = bestZ;
        }

        private static float Clamp(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v;

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
