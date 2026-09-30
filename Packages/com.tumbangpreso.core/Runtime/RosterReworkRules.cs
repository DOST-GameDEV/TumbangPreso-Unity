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
        /// <summary>COLD FEET: current Wiki field lifetime7.5seconds, cooldown35seconds.</summary>
        public const float ColdFeetCooldown = 35.0f;
        public const float ColdFeetSeconds = 7.5f;
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
        public const float AbsoluteZeroDelay = 1.5f;
    }

    public static class GeoRules
    {
        public const float EarthboundDistanceScale = .5f;
        /// <summary>Current Wiki UNSTOPPABLE:15seconds of status immunity,40second cooldown.</summary>
        public const float ShieldSeconds = 15.0f;
        public const float ShieldCooldown = 40.0f;

        /// <summary>BOULDER, owner: *"Throw rock -> Concussed"*. Cooldown, speed and roll set here.</summary>
        public const float BoulderCooldown = 30.0f;
        public const float BoulderSpeed = 14.0f;
        public const float BoulderRollDistance = 2.0f;
        public const float BoulderHitRadius = 0.7f;
        public const float BoulderShoveDistance = 1.5f;
        public static float BoulderShoveSpeed => CarryRules.ImpulseFor(BoulderShoveDistance);

        /// <summary>BARRIER, owner: *"lasts for 7.5 seconds and follows you around"*, *"25 Seconds
        /// Cooldown"*. Width and offset set here.</summary>
        public const float BarrierCooldown = 35.0f;
        public const float BarrierSeconds = 7.5f;
        public const float BarrierWidth = 3.4f;
        public const float BarrierForward = 1.2f;

        /// <summary>EARTHQUAKE, owner: *"Everyone concussed"*. Cost set here.</summary>
        public const float EarthquakeCost = 14.0f;
    }

    public static class NecroRules
    {
        /// <summary>Current Wiki: every Kuro basic ability shares one 25-second cooldown.</summary>
        public const float BasicCooldown = 25.0f;
        public const float SitSeconds = 10.0f;
        // The Wiki changes the action, not its existing hold-to-aim reach.
        public const float SitMaxRange = TerrifyMaxRange;

        /// <summary>TERRIFY, owner: *"Leave Kuro somewhere and everyone there gets feared"*. Set here:
        /// aimed up to 7 m, a 4 s haunt, feared within 2.2 m, once per body per haunt.
        /// Legacy fear-spot tuning is retained for references; the live signature now uses Sit.</summary>
        public const float TerrifyCooldown = BasicCooldown;
        public const float TerrifyMaxRange = 7.0f;
        public const float TerrifyHauntSeconds = 4.0f;
        public const float TerrifyRadius = 2.2f;

        /// <summary>KURO FETCH, owner: *"Slipper retrieve"*, and the taya can intercept. Set here.</summary>
        public const float FetchCooldown = BasicCooldown;
        public const float FetchSpeed = 9.0f;
        /// <summary>A tag this close to Kuro while he carries makes him drop it.</summary>
        public const float FetchInterceptRadius = 1.3f;

        /// <summary>KURO: CATCH! Current Wiki grants five seconds of can protection.
        /// Original companion presentation tuning is retained below.</summary>
        public const float GuardCooldown = BasicCooldown;
        public const float GuardSeconds = 5.0f;
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
        /// <summary>
        /// Where caught bodies are held: on a RING this far from the eye's axis, not at its centre. ⚠️ 1.65, was 0.8 (unused): the
        /// HERO-10 film v4 dragged every body into the middle of the 2.4 m cosmos window and they covered it. On the ring they
        /// orbit its rim, bobbing, and the portal stays in view. Eye radius 1.2 plus about half a body. Set here.
        /// </summary>
        public const float HigopHoldRadius = 1.65f;

        /// <summary>
        /// OMEN's eye is placed IN THE AIR as well as on the court (owner, 2026-09-27: *"make it so that she can choose as well
        /// where blackhole goes and how high bcz i want it to be able to put ppl on the air as well haha"*). She aims the height
        /// by looking up: the eye's centre goes where her sight line is at the aimed distance, between chest height and this cap.
        /// The pull's lift then holds caught bodies with their chests at the eye (`HigopLiftBelowEye` under it). Set here.
        /// </summary>
        public const float HigopMinHeight = 1.1f;
        public const float HigopMaxHeight = 4.5f;
        public const float HigopLiftBelowEye = 1.0f;

        // ==============================================================================================================
        // ⚠️⚠️ v3, THE OWNER'S OWN TABLE (2026-09-27): the numbers below replace the kit above as its abilities are rebuilt
        // (`docs/reports/phaister-kit-2026-09-27/plan.md` section 9). His numbers are quoted; the rest are marked proposed
        // and listed in plan section 9.11 for him to retune.
        // ==============================================================================================================

        /// <summary>TELEPORT, *"35 Seconds Cooldown"*.</summary>
        public const float TeleportCooldown = 35.0f;
        /// <summary>Today's VANISHING ACT reach, kept (proposed).</summary>
        public const float TeleportMinRange = 2.0f;
        public const float TeleportMaxRange = 5.5f;

        /// <summary>CURSE: DRAIN, *"35 Seconds Cooldown"*.</summary>
        public const float DrainCooldown = 35.0f;
        /// <summary>*"After a 1.5 seconds delay, inflict Drained"*.</summary>
        public const float DrainDelaySeconds = 1.5f;

        /// <summary>CURSE: HEX, *"35 Seconds Cooldown."*</summary>
        public const float HexCooldown = 35.0f;
        /// <summary>*"After 10 seconds it can be recast to inflict Hex"*.</summary>
        public const float HexArmSeconds = 10.0f;
        /// <summary>An unused hex mark frays away this long after it lands: ten to arm and fifteen to use it (proposed).</summary>
        public const float HexMarkLifeSeconds = 25.0f;

        /// <summary>THE REACH, how both curses mark: *"hold her hand out towards someone for like 2 seconds or smth"*.</summary>
        public const float ReachSeconds = 2.0f;
        /// <summary>How far away she may start reaching, and where the thread breaks (proposed).</summary>
        public const float ReachStartRange = 9.0f;
        public const float ReachBreakRange = 11.0f;
        /// <summary>How far off her aim the target may be, degrees either side (proposed).</summary>
        public const float ReachConeDegrees = 35.0f;
        /// <summary>The share of the cooldown a broken reach hands back (proposed).</summary>
        public const float ReachBrokenRefund = 0.5f;

        /// <summary>VOODOO, the passive: *"Whenever Phaister marks someone she takes 10% of their speed and they slow down by
        /// 10%"*. For as long as the mark lives (proposed; his text ties it to the mark).</summary>
        public const float PassiveSpeedShare = 0.10f;

        /// <summary>VOODOO DOLL, *"12 Objective Points"*.</summary>
        public const float DollCost = 12.0f;

        /// <summary>
        /// The doll body's share of a player's walk and run. The rule is the owner's (*"big fat voodoo doll that's kinda sllow(to
        /// balance it)"*, *"make him look sluggish and its okay if he's slower than others"*); the number is proposed. Slow is its
        /// balance (a Hard AI that never tires) and its look: at a player's speed its short legs could only skate. 0.65 until
        /// 2026-09-29, then 0.5 on the owner's *"refine animation of doll i want it to look more sluggish, make it slow too"*.
        /// </summary>
        public const float DollSpeedScale = 0.5f;

        /// <summary>May she start reaching for someone this far away and this far off her aim, in sight?</summary>
        public static bool ReachCanStart(float distance, float offAimDegrees, bool inSight)
            => inSight && distance <= ReachStartRange && offAimDegrees <= ReachConeDegrees;

        /// <summary>Does a reach already running still hold? It may stretch to the break range before it snaps.</summary>
        public static bool ReachHolds(float distance, float offAimDegrees, bool inSight)
            => inSight && distance <= ReachBreakRange && offAimDegrees <= ReachConeDegrees;

        /// <summary>A hex mark this old can be recast.</summary>
        public static bool HexArmed(float markAge) => markAge >= HexArmSeconds && markAge < HexMarkLifeSeconds;

        /// <summary>A hex mark this old has frayed away unused.</summary>
        public static bool HexExpired(float markAge) => markAge >= HexMarkLifeSeconds;

        /// <summary>The passive's speed multiplier: the marked one runs slower, she runs faster, while her mark lives.</summary>
        public static float PassiveSpeedScale(bool isTheCaster) => isTheCaster ? 1.0f + PassiveSpeedShare : 1.0f - PassiveSpeedShare;
    }
}
