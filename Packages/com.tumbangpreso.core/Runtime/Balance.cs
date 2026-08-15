namespace TumbangPreso.Core
{
    /// <summary>
    /// Every tuned number in the game, in one place.
    ///
    /// ⚠️ TRANSCRIBED FROM THE GDScript, NOT FROM Design.md. The two have drifted in
    /// at least two places and the code is the newer half (see docs/Port_Plan.md §7.1).
    /// Each block below names the .gd file it came from so the pairing can be re-checked
    /// rather than re-derived.
    ///
    /// ⚠️ A NUMBER HERE THAT DISAGREES WITH THE GODOT BUILD IS A PORT BUG, and it is the
    /// kind no compiler catches and no casual playtest surfaces. Core.Tests asserts the
    /// derived quantities that Design.md recorded as MEASURED, which is what makes a
    /// drift fail a build instead of quietly changing how the game plays.
    /// </summary>
    public static class Balance
    {
        // -------------------------------------------------------------------
        // MATCH STRUCTURE — match_manager.gd, round_manager.gd
        // -------------------------------------------------------------------

        public const int Rounds = 4;
        public const int PlayerCount = 4;
        public const float RoundTime = 90.0f;
        public const float IntermissionDuration = 3.0f;

        // -------------------------------------------------------------------
        // SCORING — round_manager.gd
        //
        // ⚠️ Every point in the game is awarded in ONE function, host-side. The
        // predecessor spread its win conditions across four files and the recurring
        // bug class was a rule firing on the wrong peer.
        // -------------------------------------------------------------------

        public const int ScoreLataKnocked = 100;
        public const int ScoreTag = 100;
        public const int ScoreSabotage = 50;
        public const int ScoreDefensePerTick = 10;
        public const float DefenseTickInterval = 1.0f;
        public const float TagStunTime = 5.0f;
        public const float SabotageWindow = 2.5f;
        public const float ThrowRestoreCooldown = 1.25f;

        // -------------------------------------------------------------------
        // MOVEMENT AND THE ARENA — character_base.gd
        // -------------------------------------------------------------------

        public const float Speed = 4.6f;
        public const float AttackerSpeedScale = 0.75f;
        public const float SprintScale = 1.50f;
        public const float Friction = 30.0f;
        public const float Gravity = 20.0f;
        public const float MaxFallSpeed = 26.0f;
        public const float JumpVelocity = 5.8f;

        /// <summary>
        /// ⚠️ A SQUARE, NOT A CIRCLE. Both the chalk the map builders draw and the clamp
        /// in _move_and_confine() are max(|x|,|z|) against this number. A square and a
        /// circle of the same "radius" agree only at the four edge midpoints and differ
        /// by 2.07 units on the diagonals, which is exactly where a taya moves to cover
        /// a corner. Making either one radial cost a session on 2026-07-29.
        ///
        /// ⚠️ 7.0 IS A CEILING THIS MAP IMPOSES, not a preference:
        /// CONFINEMENT_RADIUS + THROW_STANDOFF (1.2) + a capsule must stay inside
        /// Eskinita's wall faces at x = ±8.6, or the attackers' standoff ring lands
        /// inside a wall and the bots jam against it trying to reach it.
        /// </summary>
        public const float ConfinementRadius = 7.0f;

        public const float SafeZoneMargin = 2.0f;
        public const float DefenderStartOffset = 2.5f;

        /// <summary>
        /// ⚠️ DO NOT REMOVE ON THE ASSUMPTION UNITY DOES NOT NEED IT. This is the real
        /// fix for B-100 in Godot: writing position on a physics body updates the scene
        /// tree at once and the broadphase only at the next step, so on a role rotation
        /// two players trade marks and each stands on the other's stale collider for one
        /// frame. Measured there: three contacts with normal (0,1,0), a 1.60 shove up,
        /// then 9.89 units into a wall on the next frame. Unity resolves transforms
        /// against its own broadphase with the same lag; port it deliberately and let a
        /// probe say whether it can go.
        /// </summary>
        public const int SpawnSettleFrames = 3;

        // -------------------------------------------------------------------
        // STAMINA — character_base.gd
        //
        // ⚠️⚠️ STAMINA_MAX IS THE KNOWN DOC DRIFT. The code says 60.0, which is 1.5 s of
        // sprint at 40/s. Design.md's §3 note, its §2.5 "MEASURED" block and its §5.3
        // shove arithmetic all still describe a 50-point pool and 1.25 s. This file
        // follows the CODE, because the code is what every recent match was played
        // against. Port_Plan.md §7.1 carries the reconciliation as a Phase 1 blocker.
        // -------------------------------------------------------------------

        public const float StaminaMax = 60.0f;
        public const float StaminaDrainRate = 40.0f;
        public const float StaminaRegenRate = 20.0f;
        public const float StaminaRegenDelay = 1.0f;

        /// <summary>You cannot START a sprint below this, so the bar cannot be feathered.</summary>
        public const float StaminaSprintFloor = 7.5f;

        public const float FatigueTime = 2.0f;
        public const float FatigueSpeedScale = 0.75f;

        // -------------------------------------------------------------------
        // THE SHOVE — character_base.gd. Attackers shove Attackers.
        //
        // ⚠️ THE REAL PRICE IS THE SPRINT, NOT THE 25 POINTS. That half-bar is the same
        // bar that gets you back out of the box, so a shove is paid for in escape
        // distance. That is why it stays rare without needing a bigger number on it.
        // -------------------------------------------------------------------

        public const float ShoveChargeTime = 0.0f;
        public const float ShoveSpeed = 12.247f;
        public const float ShoveLift = 2.2f;
        public const float ShoveStun = 1.25f;
        public const float ShoveStaminaCost = 25.0f;
        public const float ShoveCooldown = 7.5f;
        public const float ShoveMissCooldown = 2.0f;
        public const float ShoveRange = 1.6f;
        public const float ShoveArcDeg = 70.0f;

        // -------------------------------------------------------------------
        // THE TAYA'S TWO TAG VERBS — character_base.gd
        //
        // They answer different problems. The lunge is a charge, a dash and a cooldown:
        // the right answer to somebody running PAST you and the wrong one to somebody
        // standing next to you, because the charge is exactly long enough for them to
        // leave. The punch has no charge, more reach, and does not move the taya at all.
        // -------------------------------------------------------------------

        public const float LungeChargeTime = 0.5f;
        public const float LungeSpeed = 7.746f;
        public const float LungeTagRadius = 1.3f;
        public const float LungeActiveTime = 0.45f;
        public const float LungeCooldown = 1.5f;
        public const float LungeMinPower = 0.35f;

        public const float PunchRange = 1.7f;
        public const float PunchArcDeg = 75.0f;
        public const float PunchCooldown = 0.9f;

        public const float MaxKnockbackSpeed = 16.0f;
        public const float MaxKnockbackLift = 7.0f;
        public const float BaseStaggerTime = 0.25f;

        // -------------------------------------------------------------------
        // THE THROW — carrier.gd and slipper.gd
        // -------------------------------------------------------------------

        public const float ChargeFullTime = 2.5f;
        public const float ChargeMinPower = 0.35f;
        public const float ThrowLockTime = 1.25f;
        public const float PickupRadius = 1.4f;
        public const float MuzzleForward = 0.15f;

        public const float LaunchSpeed = 18.5f;
        public const float SlipperHitRadius = 0.23f;
        public const float MaxFlightTime = 6.0f;
        public const float ThrowerIgnoreTime = 0.25f;

        /// <summary>
        /// ⚠️ A BLOCKED SLIPPER STAYS IN THE BOX, AND THAT IS THE TAYA'S WHOLE SCORING
        /// VERB. At the old 0.62 a block threw the slipper 5.7 m, to the chalk or past
        /// it. An attacker is taggable exactly while holding a slipper INSIDE the box,
        /// so a block that put the slipper OUTSIDE meant the retrieval never entered the
        /// box and the tag could never happen: measured at 22.5% of all points before,
        /// 1.8% after. At 0.27 it travels 2.5 m and they have to come in for it.
        /// </summary>
        public const float DeflectSpeedScale = 0.27f;

        public const float DeflectLift = 5.0f;

        /// <summary>
        /// The slipper coming off the CAN, about 1.3 m. ⚠️ This and DeflectSpeedScale
        /// were ONE constant until 2026-08-01 and had to move in opposite directions:
        /// cutting the block scale for the tag fix silently collapsed the can knock to
        /// 0.3 m for a reason that had nothing to do with it.
        /// </summary>
        public const float LataRecoilScale = 0.25f;

        public const float LataRecoilLiftScale = 0.55f;
        public const float BlockKnockbackSpeed = 4.583f;

        // -------------------------------------------------------------------
        // THE LATA — lata.gd
        // -------------------------------------------------------------------

        public const float InteractionRadius = 1.6f;
        public const float ResetChannelTime = 1.5f;
        public const float DownedTiltDeg = 88.0f;
        public const float ToppleTime = 0.22f;

        /// <summary>
        /// ⚠️ THE NUMBER THAT DECIDES EVERY KNOCKDOWN IN THE GAME. A thrown slipper
        /// connects when its flat distance to the can is inside
        /// SlipperHitRadius + HitMargin = 0.53 m at neutral, tested per physics frame,
        /// host-side. Until 2026-08-01 this was an unnamed literal in slipper.gd while
        /// Design.md documented a hurtbox shape and Lata.tscn carried an Area3D authored
        /// to it, and grep found no reader for either. Three numbers that were meant to
        /// be one.
        ///
        /// ⚠️ THE WINDOW IS SKIN-INDEPENDENT EXCEPT THROUGH A DECLARED STAT. The four
        /// cans span 0.108 to 0.143 in radius, a 32% spread; deriving the window from
        /// that geometry would make the prettiest can quietly the hardest to hit with
        /// nothing on screen saying so. STANCE is how the difference is declared.
        /// </summary>
        public const float LataHitMargin = 0.30f;

        // -------------------------------------------------------------------
        // TRAITS — character_base.gd
        //
        // ⚠️ THE SPREAD IS DELIBERATELY NARROW. This is a party game about hitting a can
        // with a slipper; a pick 40% faster than another is not a personality, it is the
        // correct answer.
        // -------------------------------------------------------------------

        public const float TraitSpeedPerPoint = 0.05f;
        public const float TraitPowerPerPoint = 0.07f;
        public const float TraitGritPerPoint = 0.07f;

        // -------------------------------------------------------------------
        // MISC
        // -------------------------------------------------------------------

        /// <summary>Also SettingsManager.PLAYER_NAME_MAX. One cap, not two on one row.</summary>
        public const int PlayerNameMax = 14;
    }
}
