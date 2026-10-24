using System;

namespace TumbangPreso.Core
{
    /// <summary>
    /// ⚠️⚠️ SEAN'S LAVA BRAWLER KIT, AS NUMBERS (owner, 2026-10-24: *"I want sean to be a brawler strength based character
    /// that relies on his muscles"*, mixed with lava, born near Mayon). The plan, the six beats and every owner answer are in
    /// `docs/reports/sean-kit-2026-10-24/plan.md`. Numbers the owner gave carry his answer; the rest are PROPOSED, NOT YET
    /// PLAYED, written as a distance or a time and solved against `Balance.Friction` where they move a body.
    ///
    /// `SeanHeroKit`, `SeanMagmaFist`, `SeanVolcanicBomb`, `Lata` (the hot can) and `CombatVerbs` (PRESSURE on a shove) read
    /// these. VOLCANIC BOMB is thrown with the E button while Molten: the ultimate's own button belongs to the shared
    /// cutscene path (`HeroAbilitySystem.SubmitSharedUltimate`), which has no second press.
    ///
    /// ⚠️ EVERY GROUND PUSH HERE STAYS UNDER `Balance.MaxKnockbackSpeed` (16 m/s, 4.27 m of slide). `CharacterMotor.ApplyImpulse`
    /// clamps anything faster, so a bigger number would be a lie the motor quietly corrects. That cap is why LAVA RAM launches
    /// 3.0 m (the plan first said 4.5) and an eruption adds 40% (the plan first said 50%).
    /// </summary>
    public static class SeanRules
    {
        /// <summary>The furthest any ground push in this kit may carry a body: the slide at `Balance.MaxKnockbackSpeed`.</summary>
        public static float MaxPushDistance => Combat.KnockbackDistance(Balance.MaxKnockbackSpeed);

        // ------------------------------------------------------------------ PRESSURE (passive)

        /// <summary>Body hits to fill the heat. Owner, 2026-10-24: keep PRESSURE as written ("three bars").</summary>
        public const int PressureBars = 3;

        /// <summary>Seconds without landing a hit before the heat cools back to nothing.</summary>
        public const float PressureCoolSeconds = 6.0f;

        /// <summary>How much farther an erupting shove or LAVA RAM sends its target. Capped by `MaxPushDistance`.</summary>
        public const float EruptDistanceScale = 1.4f;

        /// <summary>The splash round an erupting hit: anyone else this close to the target is pushed too.</summary>
        public const float EruptSplashRadius = 1.6f;
        public const float EruptSplashPush = 1.5f;

        /// <summary>Heat after one more body hit. Never above full, never below empty, whatever is passed in.</summary>
        public static int PressureAfterHit(int bars) => Math.Min(PressureBars, Math.Max(0, bars) + 1);

        /// <summary>Heat left after <paramref name="secondsSinceHit"/> with no contact: all of it, or none once it has cooled.</summary>
        public static int PressureAfterIdle(int bars, float secondsSinceHit)
            => float.IsNaN(secondsSinceHit) || secondsSinceHit >= PressureCoolSeconds ? 0 : Math.Max(0, Math.Min(PressureBars, bars));

        public static bool PressureFull(int bars) => bars >= PressureBars;

        /// <summary>How far an erupting hit sends a body that would otherwise go <paramref name="baseDistance"/>.</summary>
        /// <summary>
        /// ⚠️ THE HEAT TRAVELS AS ONE NUMBER, because the timed-kit message carries one passive float and every peer must show
        /// the same fist glow (`docs/reports/sean-kit-2026-10-24/feel-and-readability.md` § 7 item 9): `(bars - 1)` whole cool
        /// windows plus the cooling left, and 0 for no heat. Its ceiling is `HeatWireCapacity`, which the receiver's own
        /// capture reports, so the message's bounds check still holds.
        /// </summary>
        public static float HeatWire(int bars, float coolLeft)
        {
            if (bars <= 0 || float.IsNaN(coolLeft) || coolLeft <= 0) return 0;
            bars = Math.Min(PressureBars, bars);
            return (bars - 1) * PressureCoolSeconds + Math.Min(coolLeft, PressureCoolSeconds - 0.001f);
        }

        public static float HeatWireCapacity => PressureBars * PressureCoolSeconds;

        public static void HeatFromWire(float wire, out int bars, out float coolLeft)
        {
            bars = 0; coolLeft = 0;
            if (float.IsNaN(wire) || wire <= 0) return;
            wire = Math.Min(wire, HeatWireCapacity - 0.001f);
            bars = (int)Math.Floor(wire / PressureCoolSeconds) + 1;
            coolLeft = wire - (bars - 1) * PressureCoolSeconds;
        }

        public static float EruptedDistance(float baseDistance)
            => float.IsNaN(baseDistance) || baseDistance <= 0 ? 0 : Math.Min(baseDistance * EruptDistanceScale, MaxPushDistance);

        // ------------------------------------------------------------------ LAVA RAM (signature, both roles)

        public const float RamCooldown = 14.0f;

        /// <summary>While Molten the RAM comes back this fast, so the 8 s state is a brawl.</summary>
        public const float RamMoltenCooldown = 4.0f;

        /// <summary>The tell: the brace must be held at least this long before a release charges.</summary>
        public const float RamMinBraceSeconds = 0.25f;

        /// <summary>How far the charge carries him if he hits nothing.</summary>
        public const float RamTravel = 4.0f;

        /// <summary>How far the first body he hits is launched (13.4 m/s); erupted, 4.2 m.</summary>
        public const float RamLaunch = 3.0f;

        public const float RamStumbleSeconds = 0.8f;

        /// <summary>Running into a wall or prop stuns HIM: the price of missing.</summary>
        public const float RamWallStunSeconds = 0.6f;

        /// <summary>How close in front of him a body must be for the charge to stop on it.</summary>
        public const float RamContactReach = 0.9f;

        public static float RamSpeed => Combat.SpeedForDistance(RamTravel);
        public static float RamLaunchSpeed => Combat.SpeedForDistance(RamLaunch);
        public static float RamTravelSeconds => RamSpeed / Balance.Friction;
        public static float RamCooldownFor(bool molten) => molten ? RamMoltenCooldown : RamCooldown;

        // ------------------------------------------------------------------ MOLTEN THROW (attacking)

        public const float MoltenThrowCooldown = 30.0f;

        /// <summary>The coat lasts for one throw within this window.</summary>
        public const float MoltenThrowLoadSeconds = 8.0f;

        /// <summary>
        /// A lata it knocks over is blasted this far along the throw, sliding to a stop under `Balance.Friction`. An ordinary
        /// knockdown topples the can where it stands, so this is the whole of "the can flies": the taya walks to it before the
        /// reset can start, and `HostRestore` stands it back on its mark either way.
        /// </summary>
        public const float HotCanSlide = 3.0f;
        public static float HotCanSlideSpeed => Combat.SpeedForDistance(HotCanSlide);

        /// <summary>The coated throw flies this much faster, and it never curves: no pektus spin is applied to it.</summary>
        public const float MoltenThrowSpeedScale = 1.15f;

        /// <summary>The can lands hot: the taya cannot START the reset until it has cooled this long.</summary>
        public const float HotCanSeconds = 1.5f;

        public static bool HotCanResettable(float secondsSinceLanded)
            => !float.IsNaN(secondsSinceLanded) && secondsSinceLanded >= HotCanSeconds;

        // ------------------------------------------------------------------ MAGMA FIST (defending)

        public const float FistCooldown = 22.0f;

        /// <summary>The lock reaches the rival nearest his aim within this range and cone (half-angle, degrees).</summary>
        public const float FistLockRange = 9.0f;
        public const float FistLockHalfAngle = 15.0f;

        /// <summary>The flying fist: speed, and how fast it may turn toward its target.</summary>
        public const float FistSpeed = 12.0f;
        public const float FistTurnDegreesPerSecond = 90.0f;

        /// <summary>Its longest life in the air: the lock range at its speed, plus room for the curve. Then it falls short.</summary>
        public static float FistMaxFlightSeconds => FistLockRange / FistSpeed + 0.35f;

        /// <summary>How close the flying fist must come to its target to sink into the puddle under them.</summary>
        public const float FistArriveRadius = 0.6f;

        /// <summary>The punch up out of the puddle catches everyone this close to the puddle's middle.</summary>
        public const float FistPunchRadius = 1.0f;

        /// <summary>Puddle to full height, then the hold at the top before it cools.</summary>
        public const float FistRiseSeconds = 0.25f;
        public const float FistHoldSeconds = 0.2f;

        /// <summary>The knock-up height and the push away from the lata.</summary>
        public const float FistKnockUpHeight = 1.0f;
        public const float FistPush = 3.5f;

        public static float FistPushSpeed => Combat.SpeedForDistance(FistPush);
        public static float FistLiftSpeed => (float)Math.Sqrt(2.0 * Balance.CharacterGravity * FistKnockUpHeight);

        /// <summary>Whether a rival this far away and this many degrees off his aim can be locked.</summary>
        public static bool FistCanLock(float distance, float degreesOffAim)
            => !float.IsNaN(distance) && !float.IsNaN(degreesOffAim) && distance > 0 && distance <= FistLockRange
               && Math.Abs(degreesOffAim) <= FistLockHalfAngle;

        /// <summary>
        /// The fist's heading after <paramref name="dt"/> seconds of turning from <paramref name="heading"/> toward
        /// <paramref name="toTarget"/> (both degrees). It turns the short way round, at most `FistTurnDegreesPerSecond`.
        /// </summary>
        public static float FistSteer(float heading, float toTarget, float dt)
        {
            if (float.IsNaN(heading) || float.IsNaN(toTarget) || float.IsNaN(dt) || dt <= 0) return Wrap(heading);
            float delta = Wrap(toTarget - heading);
            float step = FistTurnDegreesPerSecond * dt;
            return Wrap(heading + Math.Max(-step, Math.Min(step, delta)));
        }

        /// <summary>
        /// The direction (unit, on the ground) a MAGMA FIST sends its target: straight away from the lata. A target standing on
        /// the can goes the way the fist was travelling, so every peer computes the same push.
        /// </summary>
        public static void FistPushDirection(float targetX, float targetZ, float canX, float canZ,
            float fistDirX, float fistDirZ, out float x, out float z)
        {
            double dx = targetX - canX, dz = targetZ - canZ, len = Math.Sqrt(dx * dx + dz * dz);
            if (!(len >= 0.05)) { dx = fistDirX; dz = fistDirZ; len = Math.Sqrt(dx * dx + dz * dz); }
            if (!(len > 1e-6)) { x = 0; z = 1; return; }
            x = (float)(dx / len); z = (float)(dz / len);
        }

        // ------------------------------------------------------------------ MAYON AWAKENS (ultimate)

        /// <summary>15 charges, as Supernova was (`docs/Hero_Strike_Balance.md` § 3.1): it can knock the lata over itself.</summary>
        public const float UltimateCost = 15.0f;

        /// <summary>Owner, 2026-10-24: "7.0 s" for the cutscene.</summary>
        public const float CutsceneSeconds = 7.0f;

        /// <summary>The awakening blast as the cutscene hands back.</summary>
        public const float AwakeningRadius = 4.0f;
        public const float AwakeningPush = 3.5f;

        public const float MoltenSeconds = 8.0f;

        /// <summary>Owner, 2026-10-24: "No knockback" while Molten. A multiplier on every push he takes.</summary>
        public const float MoltenKnockbackScale = 0.0f;

        public static float IncomingKnockbackScale(bool molten) => molten ? MoltenKnockbackScale : 1.0f;

        /// <summary>VOLCANIC BOMB: the gather (the tell), the flight and the reach of the throw.</summary>
        public const float BombTellSeconds = 0.5f;
        public const float BombFlightSeconds = 0.9f;
        public const float BombMaxRange = 14.0f;

        /// <summary>The thrown splash.</summary>
        public const float BombRadius = 4.8f;
        public const float BombPush = 4.0f;

        /// <summary>Owner, 2026-10-24: "Drops at his feet" if never thrown. A smaller splash where he stands.</summary>
        public const float DroppedBombRadius = 3.0f;
        public const float DroppedBombPush = 3.0f;

        /// <summary>The pool it leaves: how long it lasts and how much it slows anyone in it.</summary>
        public const float PoolSeconds = 4.0f;
        public const float PoolSpeedScale = 0.7f;

        /// <summary>Where a thrown bomb lands along his aim: as far as he aimed, never past `BombMaxRange`.</summary>
        public static float BombLandingDistance(float aimedDistance)
            => float.IsNaN(aimedDistance) || aimedDistance <= 0 ? 0 : Math.Min(aimedDistance, BombMaxRange);

        public static float BombRadiusFor(bool thrown) => thrown ? BombRadius : DroppedBombRadius;
        public static float BombPushFor(bool thrown) => thrown ? BombPush : DroppedBombPush;

        /// <summary>The bomb knocks the lata over unless the caster is the taya guarding it.</summary>
        public static bool BombKnocksCan(bool casterIsTaya) => !casterIsTaya;

        /// <summary>How long the players a blast throws are held: stars only from half a second.</summary>
        public const float AwakeningStaggerSeconds = 0.4f;
        public const float BombStaggerSeconds = 0.5f;

        // ------------------------------------------------------------------ how it meets the court
        // `docs/reports/sean-kit-2026-10-24/feel-and-readability.md` § 2: the ring is the hitbox, and nothing reaches a level
        // it cannot touch.

        /// <summary>A blast, a splash or the pool reaches only bodies within this height of it, so a roof above a street blast
        /// and a street below a roof blast are out of reach. A standing jump (about 1.1 m) stays inside it.</summary>
        public const float BlastReachHeight = 1.8f;

        /// <summary>MAGMA FIST arrives, and its giant fist throws, only on its own level.</summary>
        public const float FistReachHeight = 1.2f;

        /// <summary>The fist climbs a kerb or a step up to this, the same limit a ground mark drapes over; taller is a wall.</summary>
        public const float FistStepUp = 0.6f;

        /// <summary>The hot can's slide stops at a step up taller than this; a step down it simply falls.</summary>
        public const float HotCanStepUp = 0.3f;

        /// <summary>The giant fist hits as its knuckles reach the target's waist, so what they see touch them is what hits them.</summary>
        public static float FistHitDelay => FistRiseSeconds * 0.4f;

        /// <summary>Whether something this far above or below (in metres, either sign) is on a level a reach can touch.</summary>
        public static bool WithinReach(float heightDifference, float reach)
            => !float.IsNaN(heightDifference) && Math.Abs(heightDifference) <= reach;

        private static float Wrap(float degrees)
        {
            if (float.IsNaN(degrees) || float.IsInfinity(degrees)) return 0;
            float d = degrees % 360.0f;
            if (d > 180.0f) d -= 360.0f;
            if (d <= -180.0f) d += 360.0f;
            return d;
        }
    }
}
