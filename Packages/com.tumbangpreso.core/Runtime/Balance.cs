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

        /// <summary>Classic keeps the one-defence-turn street format.</summary>
        public const int Rounds = 4;

        /// <summary>
        /// Hero Strike runs two complete role rotations. Eight rounds gives every seat the
        /// attacker and defender matchup twice, which is the requested competitive format,
        /// while Classic remains the shorter four-round street game.
        /// </summary>
        public const int HeroStrikeRounds = 8;
        public const int PlayerCount = 4;
        public const float RoundTime = 90.0f;
        public const float IntermissionDuration = 3.0f;
        public const float WarmupBufferDuration = 15.0f;

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

        // Tournament Anti-Camping & Anti-Stall Penalties
        public const float TayaCampRadius = 2.2f;
        public const float TayaCampClearRadius = 2.8f;
        public const float TayaCampWarningTime = 3.0f;
        public const float TayaCampGracePeriod = 5.0f;
        public const int ScoreTayaCampPenalty = -5;

        public const float SlipperUnretrievedWarningTime = 7.0f;
        public const float SlipperUnretrievedGracePeriod = 10.0f;
        public const int ScoreUnretrievedPenalty = -5;
        public const float TournamentPenaltyInterval = 1.0f;

        // ------------------------------------------------------------------
        // Hero Strike ultimate economy. EVERY POINT IS EARNED BY AN ACT.
        //
        // ⚠️⚠️ `UltimatePassiveChargePerSecond` WAS DELETED ON 2026-08-25 AND MUST NOT COME
        // BACK. It was 1.0/s against a max of 100, so a player who did nothing at all reached
        // **90 of the 100 in a 90 s round**: the meter was a 100 second clock wearing an
        // economy's clothes, and objective play was a bonus on top of a gift.
        //
        // `docs/VISION.md` § 4 lists **"Nothing may reward waiting"** as a competitive
        // requirement and names the ultimate charge in the same breath, so the trickle was
        // against the mode's own stated rules the whole time. 🧑 2026-08-25: *"make it so that
        // ult has to be charged and isnt cooldown gated"*.
        //
        // ⚠️ THE COST IS PER HERO NOW, at `HeroKit.UltimateCost`, and ranges 90 to 150 against
        // these earnings. `HeroKit.UltimateMax` 100 is only the meter's full scale.
        // `docs/Hero_Strike_Balance.md` § 3.1 has both tables and the reasoning for each price.
        // ------------------------------------------------------------------

        // ⚠️⚠️ THE METER COUNTS EVENTS NOW, NOT POINTS. 🧑 2026-08-27: *"wtf how many points or
        // charges to ult does downing can give? i want downing can and tayaing to only give one
        // point for the charges"*, and then *"i wanted like 10-20 charges required on ult
        // depending on impact"*. A knockdown used to be 25 and a tag 20 against costs of 90 to
        // 150, so the only way to answer "how close am I" was to divide two numbers nothing on
        // screen ever showed. **One knockdown is one charge. One tag is one charge.** An ultimate
        // costs 10 to 20 of them, ranked by how much it swings a round.
        //
        // ⚠️⚠️ THIS IS A REAL PACING CHANGE AND NOT ONLY A RESCALE, SO HERE IS THE ARITHMETIC IT
        // HAS TO BE JUDGED AGAINST. The old economy bought the most expensive ultimate for six
        // knockdowns; the new one asks twenty. A live attacker earns roughly
        //
        //     1 to 2 knockdowns        1.0 to 2.0
        //     3 to 4 retrievals        1.5 to 2.0
        //     5 to 6 throws            0.8 to 0.9
        //     ------------------------------------
        //     about 4.3 charges per 90 s round
        //
        // so Nemu's 10 lands after about two and a half rounds and Zack's 20 after about four and
        // a half. Over Hero Strike's eight rounds that is between one and three ultimates per
        // seat per match, against roughly three to five before. **If a match measures fewer than
        // one ultimate per seat, this is the number to move, and moving the COST is the honest
        // lever rather than inflating what an act pays.** `BotBehaviourProbe` prints ultimates
        // per match and `docs/TODO.md` § 38 carries the measurement.
        //
        // ⚠️ THE RATIOS BETWEEN THE FOUR EARNINGS ARE UNCHANGED, except the tag. Retrieval was
        // 12/25 of a knockdown and is 0.5; a throw was 4/25 and is 0.15. The tag goes from 0.8 of
        // a knockdown to a full 1.0 because he asked for both objectives to be worth one: a 25
        // per cent raise to the taya's only source of charge, for one round in four.

        /// <summary>The objective. ONE CHARGE, and everything else is priced against it.</summary>
        public const float UltimateChargeLataKnock = 1.0f;

        /// <summary>
        /// The taya's only way to earn, and now worth exactly what the objective is.
        ///
        /// ⚠️ 1.0, UP FROM THE 0.8 A STRAIGHT RESCALE WOULD HAVE GIVEN. Asked for by name, and
        /// defensible on its own: an attacker has three ways to earn charge and the defender has
        /// this one, for one round in four.
        /// </summary>
        public const float UltimateChargeTag = 1.0f;

        /// <summary>
        /// ⚠️⚠️ IT PAYS THE ACT THE WHOLE GAME IS BUILT AROUND. `docs/VISION.md` § 0: *"The
        /// tension is the retrieval, not the throw. Throwing is safe and free; going back in for
        /// your tsinelas is the only moment you can be caught."* Until 2026-08-25 the retrieval
        /// earned NOTHING toward an ultimate and the safe act earned 8, which paid the two halves
        /// of the game in exactly the wrong order.
        ///
        /// ⚠️ HALF A KNOCKDOWN, which is what 12 against 25 was.
        /// </summary>
        public const float UltimateChargeOwnSlipperRetrieved = 0.5f;

        /// <summary>
        /// ⚠️ THE SMALLEST EARNING IN THE GAME, for the reason above. A throw costs nothing and
        /// risks nothing, so it is the one act that should pay least. It still pays something,
        /// because a round where nobody throws is not a round.
        ///
        /// ⚠️ 0.15 OF A KNOCKDOWN, which is what 4 against 25 was.
        /// </summary>
        public const float UltimateChargeLegalThrow = 0.15f;

        // Pektus (Curve Spin) Throwing
        public const float PektusCurveStrength = 14.0f;
        public const float MaxPektusSpin = 1.0f;
        /// <summary>
        /// How far above a body's feet a resting tsinelas may sit and still be picked up.
        ///
        /// ⚠️ IT IS UNDER PickupRadius ON PURPOSE. `Slipper.CanBeGrabbedBy` measures a 3D
        /// distance from the grabber's transform, which is at their feet, so a slipper this
        /// high has already spent almost the whole 1.4 m budget on height alone and can only
        /// be reached by standing exactly underneath it. Anything higher is out of play, and
        /// a piece of ammunition that is out of play has to be given back rather than lost.
        /// </summary>
        public const float SlipperMaxRestReach = 1.2f;

        public const float PektusBankSpinThreshold = 0.55f;
        public const float PektusBankRestitution = 0.62f;
        public const int MaxScoringBanks = 1;

        // -------------------------------------------------------------------
        // MOVEMENT AND THE ARENA — character_base.gd
        // -------------------------------------------------------------------

        public const float Speed = 4.6f;

        /// <summary>
        /// ⚠️⚠️ 0.45, DOWN FROM 0.75, AND IT IS A 40% CUT HE ASKED FOR BY NAME. 🧑 2026-08-29,
        /// after playing the 8-round Hero Strike build: *"defender kinda hard now so can we slow
        /// down all attackers as well as bot, even when they sprint, by 40%"*, and separately
        /// *"feels like shit get past defender very easily"*.
        ///
        /// 0.75 x 0.60 = 0.45. **Walk 2.07 m/s, sprint 3.11 m/s**, against 3.45 and 5.18 before.
        ///
        /// ⚠️ THE SPRINT IS CUT BY THE SAME 40% WITHOUT TOUCHING `SprintScale`, WHICH IS WHY IT
        /// IS THIS CONSTANT THAT MOVED. `CharacterMotor` composes speed as
        /// `Speed * RoleSpeedScale * PersonSpeedScale * sprint * SpeedZones`, all multiplicative,
        /// so scaling the role term scales every state an attacker can be in — walking, sprinting,
        /// fatigued and inside a hazard zone — by exactly 0.60. Cutting `SprintScale` instead
        /// would have slowed the sprint and left the walk untouched, and would have hit the TAYA's
        /// sprint too, which is the opposite of the ask.
        ///
        /// ⚠️ IT REACHES THE BOTS FOR FREE. `AIController` drives the same `InputIntent` through
        /// the same motor; there is no second speed path to change. The second half of the quote
        /// is therefore satisfied by this line and not by anything in `AIController`.
        ///
        /// ⚠️⚠️ AND IT MOVES THE INTERLOCKED SET. `Stamina`'s header names StaminaMax,
        /// StaminaDrainRate, SprintScale and ConfinementRadius as one set dimensioned so the bar
        /// buys roughly one crossing of the danger zone. A 40% slower attacker covers 40% less
        /// ground on the same bar, so **a sprint no longer buys a full crossing** — that is a
        /// deliberate consequence of the ask, not an oversight, and it is what makes the taya
        /// stronger. `TripGraceAfterGetUp` was re-solved against the new speed; nothing else in
        /// the set was, and re-measuring it is `docs/TODO.md` § 83.1.
        /// </summary>
        public const float AttackerSpeedScale = 0.45f;

        /// <summary>
        /// The taya's own multiplier, which used to be a literal 1.0 inside `RoleSpeedScale`.
        ///
        /// ⚠️ 🧑 2026-08-29, in the same breath as widening the block: *"make them a bit faster
        /// too"*. 4.6 x 1.10 = **5.06 m/s**. "A bit" is taken at its word: the taya was already
        /// the faster role and the attacker cut above is doing most of the work.
        ///
        /// ⚠️ IT IS A NAMED CONSTANT RATHER THAN A NUMBER IN THE EXPRESSION, because the two role
        /// scales are read against each other constantly — the ratio is the whole balance of
        /// chase versus escape — and one of them being invisible is how it stayed at 1.0 through
        /// every retune of the other.
        /// </summary>
        public const float DefenderSpeedScale = 1.10f;

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
        /// How far apart the three attackers stand on their shared line. Two body widths:
        /// close enough to read as one group, far enough that nobody spawns inside anybody
        /// and the spawn settle has nothing to untangle.
        /// </summary>
        public const float AttackerSpawnSpacing = 1.8f;

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
        /// <summary>How close a body's FEET have to be to a resting tsinelas to pick it up.
        ///
        /// ⚠️⚠️ 1.75, UP FROM 1.40 ON 2026-08-26. 🧑, off the 4.69 player: *"cant pick up any
        /// slipper"*, in every mode. The mechanism was not broken — `SoloPracticeTests` puts a
        /// loose tsinelas at a seat's own feet and the grab connects — so what he was reporting
        /// is REACH, and the geometry says he is right. This is a 3D distance measured from the
        /// motor's transform, which sits at the SOLE OF THE FOOT, while the camera is at about
        /// 1.6 m and pitched down. At 1.40 m a tsinelas that is legally grabbable sits near the
        /// BOTTOM EDGE of the frame; anything the player can see at the crosshair is three or
        /// four metres out and refuses silently.
        ///
        /// ⚠️ 1.75 IS BOUNDED BY THE HAZARD ARITHMETIC AND NOT BY FEEL. `docs/TODO.md` § 12.2
        /// turns on this radius being SMALLER than the widest trip-hazard footprint of 2.60 m,
        /// which is what makes a swallowed tsinelas unreachable and the ejector necessary. 1.75
        /// keeps that true with 0.85 m to spare, so nothing in that entry changes.
        ///
        /// ⚠️ AND THE REAL FIX IS THE PROMPT, NOT THE NUMBER. `Hud.UpdatePickupPrompt` now says
        /// when a tsinelas is in reach, because a silent refusal is indistinguishable from a
        /// broken key and that is exactly how this was reported.</summary>
        public const float PickupRadius = 1.75f;
        public const float MuzzleForward = 0.15f;

        public const float LaunchSpeed = 18.5f;
        public const float SlipperHitRadius = 0.23f;

        /// <summary>
        /// Extra HORIZONTAL reach on the taya's body when a tsinelas is tested against it, and
        /// on nobody else's.
        ///
        /// ⚠️⚠️ 🧑 2026-08-29, twice: *"make hit box for defender bigger but make sure it reverts
        /// to nroaml when theyre attacker ... this is bcz defender kinda feels weak right now"*,
        /// and then the shape of it: *"make hitbox bigger horizontally but not fatter so taht
        /// tehy can defend perktus bettert"*.
        ///
        /// **A block is the taya's only way to stop a throw that is already in the air**, and
        /// `Slipper.HitsBody` tested it against `SlipperHitRadius + CharacterController.radius`
        /// — about 0.60 m of flat reach for a projectile crossing that window at up to 18.5 m/s,
        /// which at a 50 Hz physics step is 0.37 m of travel per test. A Pektus throw curves
        /// ACROSS that window rather than into it, so the taya was being asked to stand within a
        /// third of a metre of a path they cannot predict.
        ///
        /// ⚠️⚠️ IT IS ADDED IN `HitsBody` AND NOT TO THE `CharacterController`, WHICH IS THE
        /// WHOLE OF "NOT FATTER". Growing the capsule would widen the physical body: the taya
        /// would push attackers around, snag on the map, spawn-settle differently, and read as a
        /// wider character on screen. This number is only ever consulted when deciding whether a
        /// slipper CONNECTS, so the body stays exactly the size it looks.
        ///
        /// ⚠️ AND HORIZONTAL ONLY, ON PURPOSE. `HitsBody`'s vertical test is unchanged, so a
        /// throw sailing over the taya's head still sails over it. Widening both would have made
        /// the taya a column rather than a person.
        ///
        /// ⚠️ THE REVERT IS FREE BECAUSE THERE IS NO STATE. `HitsBody` reads `who.IsDefender` on
        /// the frame of the test, so the bonus arrives and leaves with the role and there is
        /// nothing to unwind when the taya rotates — which is precisely what he asked to be sure
        /// of. `docs/TODO.md` § 83.2.
        /// </summary>
        public const float DefenderBlockRadiusBonus = 0.35f;

        public const float MaxFlightTime = 6.0f;

        /// <summary>
        /// The total time a tsinelas may spend off the ground, deflections included.
        ///
        /// ⚠️⚠️ `MaxFlightTime` CANNOT DO THIS JOB, BECAUSE A DEFLECT RESETS IT. Bouncing off a
        /// body or off the can restarts the per-arc clock on purpose: the new arc genuinely is
        /// a new flight and deserves its own budget. The consequence nobody wrote down is that a
        /// slipper which deflects again before that budget runs out never runs it out at all. A
        /// tsinelas falling onto somebody standing still gets lifted by `DeflectLift`, comes
        /// straight back down onto them, and hovers there for the rest of the round. Its owner
        /// is then fined -5 a second under the anti-stall rule for failing to fetch a slipper
        /// that is not on the ground to be fetched.
        ///
        /// ⚠️ IT IS THE SAME 6 SECONDS AS THE PER-ARC CAP RATHER THAN A MULTIPLE. Real throws
        /// resolve in about a second and a half, so six seconds of accumulated air is already
        /// far beyond anything the game asks for; a larger number would only mean a longer
        /// stall before the same recovery.
        /// </summary>
        public const float MaxAirborneTime = 6.0f;
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

        // -------------------------------------------------------------------
        // SLIPPER FLIGHT — transcribed from slipper.gd. The systems that read these
        // are still PARTIAL; the numbers land first so the port cannot re-derive them.
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ ENERGY IS LOST ON THE BOUNCE. A perfectly elastic wall would return a slipper
        /// at throw speed, which is a projectile nobody threw and which can still knock the
        /// lata down — a point scored by the wall. 0.45 is enough to carry it clear of the
        /// boundary and not enough to be a shot.
        /// </summary>
        public const float BounceRestitution = 0.45f;

        /// <summary>A tap still throws, at this fraction of full speed, so an accidental
        /// click is a weak throw and not a dropped input. Mirrors Carrier.ChargeMinPower.
        /// Read as `LaunchSpeed * lerp(MinPowerScale, 1, power)` — the aim line and the
        /// flight line share the one expression on purpose.</summary>
        public const float MinPowerScale = 0.35f;

        /// <summary>Where a slipper lies when it is at rest. The three-flat-slipper average,
        /// so it cannot look wrong on the default skin.</summary>
        public const float SlipperRestHeight = 0.045f;

        /// <summary>Visual spin about the long axis while airborne, degrees/second.</summary>
        public const float SlipperSpinSpeedDeg = 900.0f;

        /// <summary>End-over-end tumble, degrees/second. A real thrown slipper does both at
        /// once; doing only the spin is what made the predecessor read as "flying perfectly
        /// flat".</summary>
        public const float SlipperTumbleSpeedDeg = 520.0f;

        /// <summary>
        /// ⚠️ A GAMEPLAY CONSTANT WEARING A COSMETIC HAT. 0.691 is what the generated meshes
        /// already measured (0.432 mesh × the 1.6 visual scale in `TsinelasVisual.tscn`),
        /// kept exactly so the constants that depend on it did not have to move again when
        /// downloaded models arrived.
        /// </summary>
        public const float SlipperModelLength = 0.691f;

        /// <summary>How far below the arena a slipper has to fall before it is considered
        /// lost and returned to its spawn rather than falling forever.</summary>
        public const float VoidY = -12.0f;

        /// <summary>
        /// The rim glow strength on the slipper that belongs to the local player.
        ///
        /// ⚠️ PER-PEER, DELIBERATELY NOT REPLICATED. "Yours" is a different slipper on every
        /// machine, so this is computed locally each time it changes and never sent — a
        /// networked glow would light one slipper for everybody.
        /// </summary>
        public const float OwnerRimStrength = 0.85f;

        /// <summary>
        /// The rim strength on a tsinelas that has just been thrown and come to rest.
        ///
        /// ⚠️⚠️ § THE LANDED HIGHLIGHT ANSWERS A DIFFERENT QUESTION FROM THE OWNER GLOW, which
        /// is why it is tracked separately despite sharing a lever and a strength. The owner
        /// glow is "which slipper is MINE": per-peer, a different slipper on every machine,
        /// never sent. This is "where did the one that was just thrown END UP": the same answer
        /// on all four machines, because it falls out of the slipper's state plus the one bit
        /// that says the arrival at rest ended a flight rather than a teleport.
        ///
        /// ⚠️ IT STAYS ON FOR THE WHOLE LOOSE REST, NOT FOR A TIMED FLASH. A throw that missed
        /// is exactly the moment its owner has lost track of the thing, and a cue that expired
        /// after two seconds would be gone by the time they finished looking for it. It goes
        /// out when the slipper leaves Loose, which is to say when somebody has it: when the
        /// question is answered, rather than when a timer says it should be.
        ///
        /// ⚠️ AND IT LIGHTS EVERY LANDED SLIPPER, NOT ONLY YOUR OWN. Anybody may pick up
        /// anybody's tsinelas, so "one is lying loose over there" is information for all three
        /// attackers and for the taya. The owner glow is still what says which one is yours.
        /// </summary>
        public const float LandedRimStrength = 0.85f;

        /// <summary>Metres per second below which a downed can stops rolling. A can lying
        /// still must lie still rather than creep.</summary>
        public const float DownedRollSettle = 0.35f;

        // -------------------------------------------------------------------
        // IMPACT FEEL
        // -------------------------------------------------------------------

        /// <summary>Global time scale and duration for the freeze on a landed hit.
        ///
        /// ⚠️ HITSTOP WRITES A GLOBAL TIME SCALE, which is why CameraRig's shake is
        /// deliberately NOT built on it — see that file's note. Whatever drives this must
        /// restore the previous scale even if the round ends mid-freeze.</summary>
        public const float HitstopDuration = 0.06f;
        public const float HitstopTimeScale = 0.05f;

        /// <summary>Below this fall speed a landing is silent. Stops a unit that stepped off
        /// a kerb from thumping like one that fell off a roof.</summary>
        public const float LandSfxMinSpeed = 2.0f;

        /// <summary>How often a networked slipper's transform is sent.</summary>
        public const float SlipperSyncInterval = 0.25f;

        // -------------------------------------------------------------------
        // GETTING BACK UP
        //
        // 🧑, 2026-08-25, on the street trip hazards: *"like maybe places u can trip on?
        // then fall down animation plays and u have to spam a button to get back up"*.
        //
        // ⚠⚠ A TRIP WAS THE ONLY DEAD TIME IN THE GAME A PLAYER COULD NOT ANSWER. The knockdown
        // and the get-up already shipped: `StreetTripHazard` calls `CharacterMotor.ApplyTrip`,
        // `CharacterAnimator` plays the knockdown while `TripLeft > 0.7` and the get-up under it,
        // and the stagger runs for the same span. Then the timer ran down on its own and no
        // input touched it, so 2.5 s on the floor was 2.5 s of watching. Every other status in
        // this game is either short, self-inflicted or answered by a decision.
        //
        // ⚠⚠ IT IS A FLOOR, NOT A RACE, AND THAT IS THE WHOLE BALANCE OF IT. Rewarding raw
        // press rate would hand the round to whoever owns a mouse with a turbo switch, and
        // `docs/VISION.md` § 4 aims Hero Strike at a bracket. So: presses are RATE-CAPPED, each
        // one buys a fixed slice, and no amount of mashing takes the fall below `MinTripDown`.
        // The gap a mash can close is therefore bounded and knowable: from 2.50 s down to 0.90 s.
        // -------------------------------------------------------------------

        /// <summary>Seconds a single accepted press removes from a trip.
        ///
        /// ⚠ SOLVED, NOT PICKED. `StreetTripHazard` sets `TripDuration` to 2.50 s and
        /// `MinTripDown` is 0.35 s, so a mash has **2.15 s** to remove, which is 6.1 presses at
        /// this value: 0.61 s of mashing at the 10 Hz cap, for a fall of 0.96 s answered
        /// perfectly. Raised from 0.20 with `MinTripDown` on 2026-08-26; at 0.20 the same slack
        /// wanted 10.75 presses, which is 1.08 s of mashing and a 1.43 s fall, and it left the
        /// per-press value doing the work the floor should have been doing.
        ///
        /// ⚠⚠ RAISED FROM 0.13 TO 0.20 ON 2026-08-25, AND THE OLD VALUE WAS SOLVED AGAINST THE
        /// WRONG QUANTITY. 0.13 was chosen so the saving "lands comfortably inside the fall",
        /// which it did: 1.60 / 0.13 = 12.3 presses at 0.10 s is 1.23 s of mashing. But what the
        /// player experiences is not the mashing window, it is the WHOLE TIME ON THE FLOOR, and
        /// that is the mash plus the floor: 1.23 + 0.90 = **2.13 s**. 🧑 asked for a fall you can
        /// answer "in 1-2 seconds", and 2.13 is outside that.
        ///
        /// 0.20 s takes the same 1.60 s off in 8 presses, which is 0.80 s of mashing, for a total
        /// of 0.80 + 0.90 = **1.70 s** on the floor. Still eight real presses, so it remains a
        /// burst rather than two taps, and the anti-turbo bound in `MashCooldown` is untouched.
        ///
        /// ⚠ THE FLOOR IS NOT THE LEVER AND MUST NOT BECOME ONE. `MinTripDown` is pinned at
        /// 0.90 by the knockdown clip, as its own note explains, so the only honest way to
        /// shorten a fall is to buy the slack faster.</summary>
        /// ⚠️⚠️ 0.22, DOWN FROM 0.35 ON 2026-08-26, BECAUSE 0.35 MADE THE MASH A TAP. 🧑, off
        /// the 4.69 player: *"mash is weird now, I js have to click it twice to get up im not fr
        /// mashing"*. He was measuring correctly and the arithmetic agrees: at 0.35 the 2.15 s
        /// slack was 6.1 presses, and two of them plus a second and a half of passive bleed had
        /// him up. A six-press burst is short enough that the passive rate is doing most of the
        /// work in any real fall, which is the same complaint as *"it automatically resolves"*
        /// wearing different clothes.
        ///
        /// At 0.22 the slack is **9.8 presses**, which is 0.98 s of hammering at the 10 Hz cap
        /// and 1.33 s on the floor in total. That is a burst you have to commit to, it is inside
        /// the 1 to 2 s he asked a fall to last, and two taps now buy 20 per cent of the meter
        /// rather than a third of it.
        public const float MashRecoverPerPress = 0.22f;

        /// <summary>Shortest gap between two presses that both count.
        ///
        /// ⚠ THIS IS THE ANTI-TURBO BOUND AND IT IS THE REASON THE MASH IS FAIR. 10 Hz is
        /// comfortably above what a human sustains on a burst and comfortably below what a macro
        /// or a turbo-fire mouse does, so the ceiling is reachable by hand and cannot be beaten
        /// by hardware.</summary>
        public const float MashCooldown = 0.10f;

        /// <summary>How long a trip lasts however hard it is answered.
        ///
        /// ⚠ IT IS THE GET-UP ANIMATION'S LENGTH AND NOTHING ELSE, AND IT WAS 0.90 FOR A
        /// REASON THAT STOPPED BEING TRUE. `CharacterAnimator` switches from the knockdown to
        /// the get-up at this value and TIME-SCALES the get-up clip to fill it exactly, so the
        /// get-up lands as control returns. The old note said 0.90 was "pinned by the knockdown
        /// clip"; the knockdown is now a separate held phase that ENDS here, so the only thing
        /// this has to be long enough for is the get-up itself. Every clip on every rig measures
        /// **0.333 s**, so 0.35 plays it at 0.95x, which is its own speed.
        ///
        /// ⚠⚠ AND 0.90 WAS THE "PROGRESS PAUSES" BUG. 🧑, 2026-08-26, off the built player:
        /// *"if i mash it, the progress pauses"*. It was not a rendering fault. `MashRecover`
        /// clamps at this floor, so with 0.90 a fall of 2.50 s had only 1.60 s a press could buy
        /// and **0.90 s, over a third of the whole event, in which every further press was
        /// refused**. A fast masher spent that 0.90 s hammering a button that did nothing, with
        /// the bar crawling at the passive rate. At 0.35 the un-mashable tail is the get-up
        /// animation and nothing else: presses count for **86 per cent** of the fall.
        ///
        /// ⚠ THE SWITCH USED TO BE A SEPARATE 0.70 TYPED INTO `CharacterAnimator.Choose`, which
        /// meant part of every fall was a state with no name: the mash refused, the HUD saying
        /// GETTING UP, and the body still face down. One number, one meaning.</summary>
        public const float MinTripDown = 0.35f;

        /// <summary>How long a fall may last with NOBODY pressing anything, after which the
        /// body is released whatever the mash meter says.
        ///
        /// ⚠️⚠️ IT REPLACES `TripPassiveDecayRate`, WHICH WAS DELETED ON 2026-08-26, AND THE
        /// DELETION IS THE FIX. That constant bled the fall away on its own at 0.60x, so a
        /// player who pressed nothing still stood up in 3.93 s and the get-up meter was not the
        /// thing that ended the fall. 🧑, off the 4.70 player: *"u randomly get up after set amt
        /// of time, i dont have to actually mash it"* and *"i want it so that i can only get up
        /// when ive reached the end of the mashing shit bcz sometimes i get up with it still at
        /// middle or when i only clicked once"*. Both sentences describe a clock, and no value
        /// of a decay RATE can answer them: while a rate above zero exists, time alone ends the
        /// fall and the meter is decoration on a countdown. The three previous passes at this
        /// (§ 12.1, § 13.1, § 14.1 in `docs/TODO.md`) each retuned the rate and each left that
        /// property standing.
        ///
        /// ⚠️⚠️ SO THE RULE IS NOW ONE SENTENCE: **above `MinTripDown` only an accepted press
        /// moves the fall.** The meter is the gate rather than a readout, and
        /// `CharacterMotor.MashRemoved` reaching the mashable slack is the ONLY thing that puts
        /// a player on their feet inside this window.
        ///
        /// ⚠️ THIS IS A STRANDING GUARD, NOT A SECOND WAY UP, and it is set far enough out that
        /// it can never be the better option. A perfectly answered fall is 1.33 s; this is 5.0,
        /// which is **3.8x** that and **5.1x** the 0.98 s mash window. Nobody waits five seconds
        /// on the road to save ten presses. What it does buy is the property the old note on
        /// `TripPassiveDecayRate` was right about: a trip that ONLY a press can clear strands a
        /// player whose hands left the keyboard, and hands a griefing tool to anything that can
        /// re-apply one.
        ///
        /// ⚠️⚠️ AND WHEN IT FIRES IT FILLS THE METER. `CharacterMotor` credits the whole slack
        /// to `MashRemoved` on the way out, so the invariant a player can see holds without
        /// exception: **you never stand up with the bar part-full.** Without that line this
        /// constant would reintroduce, once every five seconds, the exact frame he photographed.
        /// </summary>
        public const float TripAutoRecoverSeconds = 5.0f;

        /// <summary>Seconds after a trip ends during which no hazard may start another one.
        ///
        /// ⚠⚠ WITHOUT IT THE MASH IS A TRAP AND THE HAZARD FIELD IS A LOOP. The mash is bound
        /// to Jump (`CharacterMotor`), so the instant `_tripLeft` reaches 0 the same hammering
        /// becomes real jumps. A jump gives the body well over `StreetTripHazard`'s
        /// `MinSpeedToTrip` of 1.0 m/s while it is still standing on or beside the hazard, and
        /// the hazard trips it again. `StreetTripHazard`'s own `Cooldown` cannot answer this
        /// because it is PER HAZARD: a neighbouring one re-trips with no wait at all, which on
        /// Ilalim ng Tulay meant two hazards 2.6 m apart could pass a player back and forth.
        ///
        /// ⚠ SOLVED AGAINST THE FOOTPRINT, NOT PICKED. An attacker moves at
        /// `Speed` * `AttackerSpeedScale` = 2.07 m/s, so 1.60 s carries them 3.31 m. The largest
        /// hazard footprint on the map is 2.60 m, so the grace covers walking clear of the thing
        /// that felled you from its centre, with margin.
        ///
        /// ⚠️⚠️ 1.60, RE-SOLVED WHEN `AttackerSpeedScale` WENT TO 0.45 ON 2026-08-29, AND THE OLD
        /// 1.20 WOULD HAVE REINTRODUCED THE LOOP RATHER THAN MERELY BEEN STALE. It was solved
        /// against 3.45 m/s and covered 4.14 m; at 2.07 m/s the same 1.20 s covers only **2.48 m**,
        /// which is UNDER the 2.60 m footprint it exists to clear. A slower attacker would have
        /// stood up still inside the hazard that felled them, with the grace already expired —
        /// the exact ping-pong this constant was added to stop. **This is what "re-measure the
        /// interlocked set" means in practice**, and it is the one member of it that could be
        /// re-solved by arithmetic rather than by playing.
        ///
        /// ⚠ IT LIVES ON THE MOTOR, NOT ON THE HAZARD, so every hazard present and future
        /// respects one window rather than each keeping its own.</summary>
        public const float TripGraceAfterGetUp = 1.60f;

        // -------------------------------------------------------------------
        // § MASHING OUT OF AN ABILITY STUN
        //
        // ⚠️⚠️ THIS APPLIES TO ABILITY STUNS ONLY AND DELIBERATELY NOT TO THE TAYA'S TAG.
        // 🧑 2026-08-26: *"for abilities that freeze or stun enmies ... i want them to go to TPP
        // and to have a button mashing thing to get unstunned or unfrozen (same as when u trip)
        // but maybe diff UI and effect"*. The sentence names abilities, and the distinction is
        // load-bearing rather than a narrow reading: `TagStunTime` is 5.0 s and the tag is the
        // ONE scoring verb a defender has (`docs/VISION.md` § 4). Letting an attacker hammer out
        // of it would take the single thing the taya can do and halve it, in the mode aimed at a
        // bracket. A tag is answered by not being caught.
        //
        // ⚠️ SO THE TWO STATUSES NOW READ DIFFERENTLY ON PURPOSE, WHICH IS THE WHOLE POINT OF
        // SPLITTING THEM. A tag drains the body's colour and cannot be fought (§ THE CAUGHT MARK
        // in `Toon.shader`). An ability stun coats the body in the caster's ELEMENT and can be.
        // One of them is a rule, the other is a fight.
        //
        // ⚠️ IT REUSES `MashCooldown`, NOT A SECOND RATE CAP. The 10 Hz anti-turbo bound is the
        // reason the trip mash is fair against a macro, and there is no argument for a different
        // ceiling here: it is the same hand on the same key.
        // -------------------------------------------------------------------

        /// <summary>
        /// How many accepted presses break a stun, when the ability does not say otherwise.
        ///
        /// ⚠️⚠️ THE COST IS PER ABILITY, NOT ONE GLOBAL SLICE, AND THAT WAS ASKED FOR DIRECTLY.
        /// 🧑 2026-08-26: *"maybe chaneg the amt needed to be button mash for each skill? make it
        /// dependent on how hard the skill is supposed to hit"*. So the tuning knob is a PRESS
        /// COUNT that each ability declares, and the seconds a press buys are DERIVED from it:
        ///
        ///     perPress = (stunTotal - MinStunDown) / breakPresses
        ///
        /// ⚠️ IT IS EXPRESSED AS PRESSES RATHER THAN AS SECONDS-PER-PRESS BECAUSE PRESSES ARE
        /// THE THING BEING BALANCED. "Nemu's phase grab takes four presses, Cheska's nova takes
        /// nine" is a sentence somebody can hold in their head and tune against. A table of
        /// per-press durations says the same thing in units nobody experiences, and it silently
        /// changes meaning whenever a stun's DURATION is retuned: the same 0.30 buys half as
        /// much of a 6 s stun as of a 3 s one. Deriving from the total keeps a press count
        /// meaning the same thing whatever the duration is.
        ///
        /// ⚠️ 6 IS THE MIDDLE OF THE RANGE, NOT A CEILING. `Hero_Strike_Balance.md` carries the
        /// per-ability numbers; light staggers should sit at 3 or 4 and an ultimate's hold at 9
        /// or more. This constant is only what an ability that never said gets.
        /// </summary>
        public const int StunBreakPressesDefault = 6;

        /// <summary>
        /// The bounds a per-ability press count is clamped into.
        ///
        /// ⚠️⚠️ THE FLOOR EXISTS SO A STUN CANNOT BE TAPPED OFF AND THE CEILING SO IT CANNOT
        /// BECOME UNANSWERABLE. At the 10 Hz `MashCooldown`, 2 presses is 0.2 s of input, which
        /// is inside the reaction time of noticing you were stunned at all: that is not a fight,
        /// it is a formality. 14 presses is 1.4 s of sustained hammering, which is already longer
        /// than the perfectly answered trip and is as far as this should ever go before the
        /// honest answer is a longer `MinStunDown` instead.
        ///
        /// ⚠️ CLAMPED RATHER THAN ASSERTED, deliberately. A kit author typing 0 should get a
        /// playable stun and a balance review, not a division by zero in the middle of a match.
        /// </summary>
        public const int StunBreakPressesMin = 3;

        public const int StunBreakPressesMax = 14;

        /// <summary>The shortest an ability stun can be made by mashing perfectly.
        ///
        /// ⚠⚠ IT IS A FLOOR AND IT IS WHAT KEEPS A COOLDOWN WORTH SPENDING. Without one, a
        /// player with fast hands would clear a stun in three presses and every control ability
        /// in Hero Strike would stop being a control ability. 1.20 s is long enough that the
        /// caster still gets the opening they paid for and short enough that the victim is not
        /// spectating. It is the same argument `MinTripDown` makes, at a higher number because
        /// the thing being escaped cost somebody a cooldown.
        ///
        /// ⚠ AND IT IS ABOVE `Balance.MashCooldown` BY MORE THAN AN ORDER OF MAGNITUDE, so the
        /// floor can never be reached by a single lucky press inside one frame's window.</summary>
        public const float MinStunDown = 1.20f;
    }
}
