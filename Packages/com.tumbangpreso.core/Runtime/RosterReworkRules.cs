namespace TumbangPreso.Core
{
    /// <summary>
    /// ⚠️⚠️ THE ROSTER REWORK'S KITS, AS NUMBERS (ABILITY-2, owner 2026-09-26: the power tables for Cryo,
    /// Geo, Necro and Voodoo; *"will create completley new VFX SFX AND SKILLS FOR ALL CHARACTERS"*).
    /// `docs/reports/ability-rework-2026-09-26/plan.md` section 3 is the design; every value the owner
    /// did not give is listed in its section 7 for his review. Written as distances and times; a move
    /// of a body is solved against `Balance.Friction` (`CarryRules`).
    ///
    /// ⚠️ IN CORE SO `dotnet test` ASSERTS THE OWNER'S NUMBERS IN A SECOND. The kits read from here.
    /// </summary>
    public static class CryoRules
    {
        /// <summary>COLD FEET, owner: *"The chilling field lasts for 5 seconds"*, *"35 Seconds Cooldown"*.</summary>
        public const float ColdFeetCooldown = 35.0f;
        public const float ColdFeetSeconds = 5.0f;
        /// <summary>The old Permafrost Sheet's radius and aim band, which already sat in the footprint budget.</summary>
        public const float ColdFeetRadius = 2.3f;
        public const float ColdFeetMinRange = 1.8f;
        public const float ColdFeetMaxRange = 5.0f;

        /// <summary>FROSTBITE, owner: *"35 Seconds Cooldown"*. The load lasts 10 s (set here).</summary>
        public const float FrostbiteCooldown = 35.0f;
        public const float FrostbiteLoadSeconds = 10.0f;

        /// <summary>GLACIAL WALL, owner: *"takes 3 slipper hits to shatter"*, *"35 Seconds Cooldown"*.
        /// The arc's size and life are set here.</summary>
        public const float GlacialWallCooldown = 35.0f;
        public const int GlacialWallHits = 3;
        public const float GlacialWallArcLength = 4.2f;
        public const float GlacialWallArcRadius = 3.0f;
        public const float GlacialWallSeconds = 8.0f;
        public const float GlacialWallMinRange = 1.8f;
        public const float GlacialWallMaxRange = 4.0f;

        /// <summary>ABSOLUTE ZERO, owner: *"12 Objective Points"*; Frozen on everyone, then Chilled.</summary>
        public const float AbsoluteZeroCost = 12.0f;
    }

    public static class GeoRules
    {
        /// <summary>SHIELD, owner: *"Status immunity for 20 seconds"*. Cooldown 45 s (set here).</summary>
        public const float ShieldSeconds = 20.0f;
        public const float ShieldCooldown = 45.0f;

        /// <summary>BOULDER, owner: *"Throw rock -> Concussed"*. Cooldown, speed and roll set here.</summary>
        public const float BoulderCooldown = 30.0f;
        public const float BoulderSpeed = 14.0f;
        public const float BoulderRollDistance = 2.0f;
        public const float BoulderHitRadius = 0.7f;
        public const float BoulderShoveDistance = 1.5f;
        public static float BoulderShoveSpeed => CarryRules.ImpulseFor(BoulderShoveDistance);

        /// <summary>BARRIER, owner: *"lasts for 7.5 seconds and follows you around"*, *"25 Seconds
        /// Cooldown"*. Width and offset set here.</summary>
        public const float BarrierCooldown = 25.0f;
        public const float BarrierSeconds = 7.5f;
        public const float BarrierWidth = 3.4f;
        public const float BarrierForward = 1.2f;

        /// <summary>EARTHQUAKE, owner: *"Everyone concussed"*. Cost set here.</summary>
        public const float EarthquakeCost = 14.0f;
    }

    public static class NecroRules
    {
        /// <summary>TERRIFY, owner: *"Leave Kuro somewhere and everyone there gets feared"*. Set here:
        /// 40 s cooldown, aimed up to 7 m, a 4 s haunt, feared within 2.2 m, once per body per haunt.</summary>
        public const float TerrifyCooldown = 40.0f;
        public const float TerrifyMaxRange = 7.0f;
        public const float TerrifyHauntSeconds = 4.0f;
        public const float TerrifyRadius = 2.2f;

        /// <summary>KURO FETCH, owner: *"Slipper retrieve"*, and the taya can intercept. Set here.</summary>
        public const float FetchCooldown = 30.0f;
        public const float FetchSpeed = 9.0f;
        /// <summary>A tag this close to Kuro while he carries makes him drop it.</summary>
        public const float FetchInterceptRadius = 1.3f;

        /// <summary>KURO GUARD, owner: *"kuro aids withh blocking and becomes a bit bigger"*, *"give her
        /// like an AI to think abt where to stand but dont make it infallible"*. Set here.</summary>
        public const float GuardCooldown = 30.0f;
        public const float GuardSeconds = 6.0f;
        public const float GuardScale = 1.6f;
        public const float GuardMoveSpeed = 6.0f;
        public const float GuardBlockRadius = 0.9f;
        /// <summary>How often he re-decides, and how long he waits before acting on it: the fallibility.</summary>
        public const float GuardThinkSeconds = 0.35f;
        public const float GuardReactSeconds = 0.25f;

        /// <summary>KURO PLAYS, owner: *"HARD BOT and fulfills whatever role u have and ghets separate copy
        /// of ur skills"*. Cost set here; lasts to the round's end.</summary>
        public const float KuroPlaysCost = 16.0f;
    }

    public static class VoodooRules
    {
        /// <summary>SHADOW BLINK, owner: *"refine everything about shadow blink"*. Its numbers are kept.</summary>
        public const float BlinkCooldown = 52.0f;

        /// <summary>CURSE: DISORIENTED, a thrown doll. Set here.</summary>
        public const float DisorientCooldown = 32.0f;
        public const float DollSpeed = 12.0f;
        public const float DollMaxRange = 10.0f;
        public const float DollHitRadius = 1.2f;

        /// <summary>CURSE: VULNERABLE, a cone. Set here.</summary>
        public const float VulnerableCooldown = 32.0f;
        public const float VulnerableConeDegrees = 60.0f;
        public const float VulnerableConeRange = 7.0f;

        /// <summary>HIGOP, owner: *"really slowly cast"*, *"pulls players ands slipeprs except for her
        /// shit and no escape for entire duration but they can try to"*. Set here.</summary>
        public const float HigopCost = 15.0f;
        public const float HigopCastSeconds = 2.2f;
        public const float HigopSeconds = 5.0f;
        public const float HigopMaxRange = 8.0f;
        public const float HigopRadius = 7.5f;
        /// <summary>The pull is faster than anyone can run, so pushing away gains ground only for a
        /// moment (the owner's "they can try to").</summary>
        public const float HigopPullOverRun = 1.5f;
        public const float HigopHoldRadius = 0.8f;
    }
}
