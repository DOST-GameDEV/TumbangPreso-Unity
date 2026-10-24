using System;

namespace TumbangPreso.Core
{
    /// <summary>
    /// ⚠️⚠️ SEAN'S LAVA BRAWLER KIT, AS NUMBERS (owner, 2026-10-24: *"I want sean to be a brawler strength based character
    /// that relies on his muscles"*, mixed with lava, born near Mayon). The plan, the six beats and every owner answer are in
    /// `docs/reports/sean-kit-2026-10-24/plan.md`. Numbers the owner gave carry his answer; the rest are PROPOSED, NOT YET
    /// PLAYED, written as a distance or a time and solved against `Balance.Friction` where they move a body.
    ///
    /// `SeanHeroKit`, `SeanMagmaFist`, `SeanVolcano`, `SeanVolcanicBomb`, `Lata` (the hot can) and `CombatVerbs` (PRESSURE on a
    /// shove) read these. Since MAYON AWAKENS v2 the volcano throws the bombs; E is his role skill again while Molten.
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

        /// <summary>
        /// How far the charge carries him if he hits nothing. Owner, 2026-10-24: "give it a pretty long distance". A held
        /// charge (`CarryRules`): `RamSpeed` the whole way, then the slide-out, so it is not bound by the knockback cap.
        /// </summary>
        public const float RamTravel = 8.0f;

        /// <summary>The charge's speed, held until the slide-out (3.3 m of the 8 m).</summary>
        public const float RamSpeed = 14.0f;

        /// <summary>How far the first body he hits is launched (13.4 m/s); erupted, 4.2 m.</summary>
        public const float RamLaunch = 3.0f;

        /// <summary>Owner, 2026-10-24: "make them stumble a bit longer (1 second)".</summary>
        public const float RamStumbleSeconds = 1.0f;

        /// <summary>
        /// SEARED, after the stumble (owner, 2026-10-24: "slowed for like 2 seconds na nagdedecay"): the body starts at
        /// `RamSlowScale` of its speed and is back to full in `RamSlowSeconds`, evenly.
        /// </summary>
        public const float RamSlowSeconds = 2.0f;
        public const float RamSlowScale = 0.5f;

        /// <summary>The whole SEARED clock a ram hit sets: the stumble, then the fade.</summary>
        public static float SearedTotalSeconds => RamStumbleSeconds + RamSlowSeconds;

        /// <summary>The speed multiplier with <paramref name="searedLeft"/> seconds of SEARED left: full slow through the
        /// stumble, then back to 1 in a straight line over the last `RamSlowSeconds`.</summary>
        public static float SearedSpeedScale(float searedLeft)
        {
            if (float.IsNaN(searedLeft) || searedLeft <= 0) return 1.0f;
            float depth = Math.Min(1.0f, searedLeft / RamSlowSeconds);
            return 1.0f - (1.0f - RamSlowScale) * depth;
        }

        /// <summary>Running into a wall or prop stuns HIM: the price of missing.</summary>
        public const float RamWallStunSeconds = 0.6f;

        /// <summary>How close in front of him a body must be for the charge to stop on it.</summary>
        public const float RamContactReach = 0.9f;

        public static float RamHoldSeconds => CarryRules.SecondsFor(RamTravel, RamSpeed);
        public static float RamLaunchSpeed => Combat.SpeedForDistance(RamLaunch);
        public static float RamTravelSeconds => RamHoldSeconds + RamSpeed / Balance.Friction;
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

        // CONTACT-1024 (owner: the hot can's slide "parang teleported", wants it "visibly flying away"). Under `Balance.Friction`
        // the whole 3 m went by in 0.45 s at 13 m/s. It now LEAVES THE ROAD: an arc of `HotCanFlightSeconds` peaking at
        // `HotCanApex`, then a skid under the gentler `HotCanSkidFriction`, the same `HotCanSlide` in all, over about 0.9 s.

        /// <summary>Seconds the struck can is in the air before it lands and skids.</summary>
        public const float HotCanFlightSeconds = 0.45f;
        /// <summary>How high the struck can's arc peaks, metres.</summary>
        public const float HotCanApex = 0.5f;
        /// <summary>The landed can's skid deceleration, m/s^2 (the road's `Balance.Friction` is 30).</summary>
        public const float HotCanSkidFriction = 9.0f;
        /// <summary>The arc's gravity, so it peaks at <see cref="HotCanApex"/> halfway through <see cref="HotCanFlightSeconds"/>.</summary>
        public static float HotCanGravity => 8.0f * HotCanApex / (HotCanFlightSeconds * HotCanFlightSeconds);
        /// <summary>The arc's upward launch speed.</summary>
        public static float HotCanLaunchUp => HotCanGravity * HotCanFlightSeconds * 0.5f;
        /// <summary>
        /// The flat speed it leaves at and keeps through the air, solved so flight plus skid is exactly <see cref="HotCanSlide"/>:
        /// v T + v^2 / (2 a) = d.
        /// </summary>
        public static float HotCanFlightSpeed
        {
            get
            {
                float t = HotCanFlightSeconds, a = HotCanSkidFriction, d = HotCanSlide;
                // v^2 / (2a) + v t - d = 0
                float qa = 1.0f / (2.0f * a);
                return (-t + (float)System.Math.Sqrt(t * t + 4.0f * qa * d)) / (2.0f * qa);
            }
        }

        /// <summary>The coated throw flies this much faster, and it never curves: no pektus spin is applied to it.</summary>
        public const float MoltenThrowSpeedScale = 1.15f;

        /// <summary>The can lands hot: the taya cannot START the reset until it has cooled this long.</summary>
        public const float HotCanSeconds = 1.5f;

        public static bool HotCanResettable(float secondsSinceLanded)
            => !float.IsNaN(secondsSinceLanded) && secondsSinceLanded >= HotCanSeconds;

        // ------------------------------------------------------------------ MAGMA FIST (defending)

        public const float FistCooldown = 22.0f;

        /// <summary>
        /// Where he may place it (owner, 2026-10-24: "he js does a prayer pose tapos he long presses or smth to decide where
        /// it gets cast"): held, the ring sits where he looks on the court, this near to this far (`HeroAbility.AimByHolding`).
        /// </summary>
        public const float FistAimMinRange = 2.0f;
        public const float FistAimRange = 9.0f;

        /// <summary>The puddle opens and bubbles this long before the fist comes: the warning anyone on it can step off.</summary>
        public const float FistWarnSeconds = 0.45f;

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

        /// <summary>
        /// The direction (unit, on the ground) a MAGMA FIST sends its target: straight away from the lata. A target standing on
        /// the can goes the way he cast it (from him to the puddle), so every peer computes the same push.
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
        // ⚠️⚠️ v2, THE MOUNTAIN RISES (owner 2026-10-24: *"the ult of sean doesnt earn its place"*, beside Paete's and Phaister's;
        // he chose "The mountain rises"). v1 was 4 m of push and then 8 s of a buff nobody could see, with its payoff (the bomb on
        // E) deferred and optional. Now it puts a THING in the court that everyone plays around, as MAKILING'S EMBRACE does: he
        // holds to place it where he looks, a small Mayon bursts up there and throws everyone within 9 m out, then for 8 s it
        // lobs a volcanic bomb every 1.2 s at the nearest opponent, while he is Molten beside it.

        /// <summary>15 charges, as Supernova was (`docs/Hero_Strike_Balance.md` § 3.1): it can knock the lata over itself.</summary>
        public const float UltimateCost = 15.0f;

        /// <summary>The cutscene. The method's cap is 6.5 s; Dante's lesson (*"dont force it to 7 seconds"*) is to end it as the
        /// power launches, so it ends as the mountain bursts up and its wave heads for the players.</summary>
        public const float CutsceneSeconds = 5.0f;

        /// <summary>Where he may put it: held where he looks, between these (as MAKILING'S EMBRACE).</summary>
        public const float VolcanoAimMinRange = 3.0f;
        public const float VolcanoAimMaxRange = 10.0f;

        /// <summary>Its foot never stands this near the lata (the owner's rule for Paete's tree: never on the can).</summary>
        public const float VolcanoClearOfCan = 3.0f;

        /// <summary>The mountain's body: its height to the summit and the solid round its foot nobody walks through.</summary>
        public const float VolcanoHeight = 3.0f;
        public const float VolcanoSolidRadius = 1.1f;

        /// <summary>The rise: everyone within this of its foot is thrown straight out from it and SEARED.</summary>
        public const float VolcanoBlastRadius = 9.0f;

        /// <summary>How far the rise throws: the motor's cap, every body the same (`MaxPushDistance`).</summary>
        public static float VolcanoBlastPush => MaxPushDistance;

        public const float VolcanoBlastStaggerSeconds = 0.5f;
        public const float VolcanoSearedSeconds = 2.0f;

        /// <summary>How long it erupts after the rise; he is Molten for exactly as long.</summary>
        public const float MoltenSeconds = 8.0f;

        /// <summary>A bomb leaves the crater this often, the first this long after the rise.</summary>
        public const float VolcanoBombEvery = 1.2f;
        public const float VolcanoFirstBomb = 0.6f;

        /// <summary>It lobs only at opponents this near its foot.</summary>
        public const float VolcanoBombRange = 12.0f;

        /// <summary>A bomb's flight: its ring is on the court the whole way down, so this is the warning a player gets.</summary>
        public const float VolcanoBombFlightSeconds = 1.0f;

        /// <summary>One bomb's splash, throw and hold. Smaller than v1's single bomb: there are six of them.</summary>
        public const float VolcanoBombRadius = 2.5f;
        public const float VolcanoBombPush = 3.0f;
        public const float BombStaggerSeconds = 0.4f;

        /// <summary>The pool a bomb leaves: how long it lasts, its share of the splash and how much it slows anyone in it.</summary>
        public const float PoolSeconds = 3.0f;
        public const float PoolRadiusShare = 0.6f;
        public const float PoolSpeedScale = 0.7f;

        /// <summary>After the eruption it cools to black glass and sinks back into the court.</summary>
        public const float VolcanoSinkSeconds = 1.0f;

        /// <summary>How many bombs one eruption throws at most.</summary>
        public static int VolcanoBombCount => (int)Math.Floor((MoltenSeconds - VolcanoFirstBomb) / VolcanoBombEvery) + 1;

        /// <summary>Whether a bomb leaves the crater between <paramref name="before"/> and <paramref name="now"/> (seconds since the
        /// rise), and which one. Every peer could ask this, but only the host does: it also picks the target.</summary>
        public static bool BombDue(float before, float now, out int index)
        {
            index = -1;
            if (!(now > before) || now < VolcanoFirstBomb || before >= MoltenSeconds) return false;
            int last = (int)Math.Floor((Math.Min(now, MoltenSeconds) - VolcanoFirstBomb) / VolcanoBombEvery);
            int prior = before < VolcanoFirstBomb ? -1 : (int)Math.Floor((before - VolcanoFirstBomb) / VolcanoBombEvery);
            if (last <= prior || last >= VolcanoBombCount) return false;
            index = last;
            return true;
        }

        /// <summary>Owner, 2026-10-24: "No knockback" while Molten. A multiplier on every push he takes.</summary>
        public const float MoltenKnockbackScale = 0.0f;

        public static float IncomingKnockbackScale(bool molten) => molten ? MoltenKnockbackScale : 1.0f;

        /// <summary>A bomb knocks the lata over unless the caster is the taya guarding it.</summary>
        public static bool BombKnocksCan(bool casterIsTaya) => !casterIsTaya;

        /// <summary>
        /// The mountain's foot, kept off the can: a spot nearer the can than `VolcanoClearOfCan` is pushed straight out from it
        /// (along the line from where he stands if it sits right on top of it). Plain numbers so the cutscene, every peer and
        /// the host all put it in the same place.
        /// </summary>
        public static void VolcanoSpotClearOfCan(float x, float z, float canX, float canZ, float fromX, float fromZ,
                                                 out float outX, out float outZ)
        {
            double dx = x - canX, dz = z - canZ, d = Math.Sqrt(dx * dx + dz * dz);
            if (d >= VolcanoClearOfCan) { outX = x; outZ = z; return; }
            if (d < 1e-3) { dx = fromX - canX; dz = fromZ - canZ; d = Math.Sqrt(dx * dx + dz * dz); }
            if (d < 1e-3) { dx = 0; dz = 1; d = 1; }
            outX = (float)(canX + dx / d * VolcanoClearOfCan);
            outZ = (float)(canZ + dz / d * VolcanoClearOfCan);
        }

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
    }
}
