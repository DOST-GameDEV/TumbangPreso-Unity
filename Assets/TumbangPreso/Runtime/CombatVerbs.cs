using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// The three contact verbs: the attacker's shove, and the taya's lunge and punch.
    ///
    /// ⚠️⚠️ ALL THREE RESOLVE BY DISTANCE AND ANGLE, HOST-SIDE, NEVER BY A TRIGGER. That is
    /// carried over from measurement, not preference: an overlap fires on whichever peer owns
    /// the body, and 16 of 36 were measured failing to land, split by target.
    ///
    /// ⚠️ THE DEFENDER CANNOT SHOVE AND CANNOT BE SHOVED. They have the tag; giving them both
    /// would make the box unenterable, which deletes the retrieval the whole game is about.
    /// </summary>
    /// <remarks>
    /// ⚠️ +50 SO THE SHOVE READS `Carrier.IsBusy` AFTER THE PICKUP HAS HAD ITS SAY. See the
    /// execution-order note on <see cref="Carrier"/> for the whole three-component ordering and
    /// what each half of it prevents.
    /// </remarks>
    [DefaultExecutionOrder(50)]
    [RequireComponent(typeof(CharacterMotor))]
    public sealed class CombatVerbs : MonoBehaviour
    {
        private CharacterMotor _motor;
        private Carrier _carrier;

        private float _shoveCooldown;
        private float _punchCooldown;
        private float _lungeCooldown;

        private float _lungeCharge;
        private bool _lungeCharging;
        private float _lungeActiveLeft;
        private Vector3 _lungeFrom;

        public float ShoveCooldownLeft => _shoveCooldown;
        public float PunchCooldownLeft => _punchCooldown;
        public float LungeCooldownLeft => _lungeCooldown;
        public float LungeChargeRatio => Mathf.Clamp01(_lungeCharge / Balance.LungeChargeTime);

        /// <summary>
        /// The wind-up as a ratio, or <b>-1 when nobody is winding up at all</b>. Mirrors
        /// `character_base.gd::observed_lunge_charge()`, whose `_observed_lunge_charge` rests at
        /// -1 for exactly this reason.
        ///
        /// ⚠️⚠️ THE -1 IS THE WHOLE POINT AND ITS ABSENCE WAS TWO LIVE BUGS. <see
        /// cref="LungeChargeRatio"/> is a `Clamp01`, so it is never negative and `>= 0.0f` is a
        /// tautology against it. Both call sites that wanted "is a lunge being wound up right
        /// now" asked it that way and both were therefore always-true:
        ///
        ///  * `AIController` reacted to a taya "winding up" on every frame of every round, so
        ///    the dodge that is supposed to be a read on a tell fired against no tell.
        ///  * `YouCard` drew the taya's LUNGE meter, and the attacker's throw meter, for the
        ///    whole match instead of only while the key is held — a permanently empty second
        ///    bar in the corner the player looks at most. `you_card.gd::_update_row_visibility`
        ///    gates both rows on activity and this is the value it gates on.
        ///
        /// A ratio and a state deliberately travel in one number here rather than two, because
        /// that is what the .gd replicates and two fields can disagree across a peer boundary.
        /// </summary>
        public float ObservedLungeCharge =>
            _lungeCharging ? Mathf.Clamp01(_lungeCharge / Balance.LungeChargeTime) : -1.0f;

        /// <summary>The unit's animator, if it has a model bound yet.</summary>
        private Visual.CharacterAnimator Animator => _animator != null
            ? _animator
            : _animator = GetComponentInChildren<Visual.CharacterAnimator>();

        private Visual.CharacterAnimator _animator;

        /// <summary>The rig, but ONLY when it is looking through this unit — a kick applied to
        /// somebody else's camera is a hit landing on the wrong screen.</summary>
        private CameraSystem.CameraRig Rig
        {
            get
            {
                var rig = UnityEngine.Camera.main != null
                    ? UnityEngine.Camera.main.GetComponent<CameraSystem.CameraRig>()
                    : null;

                return rig != null && rig.IsFollowing(_motor) ? rig : null;
            }
        }

        private void Awake()
        {
            _motor = GetComponent<CharacterMotor>();
            _carrier = GetComponent<Carrier>();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            Tick(ref _shoveCooldown, dt);
            Tick(ref _punchCooldown, dt);
            Tick(ref _lungeCooldown, dt);

            if (!_motor.CanAct())
            {
                _lungeCharging = false;
                _lungeCharge = 0.0f;
                return;
            }

            if (_motor.IsDefender)
            {
                StepPunch();
                StepLunge(dt);
            }
            else
            {
                StepShove();
            }

            if (_lungeActiveLeft > 0.0f)
            {
                _lungeActiveLeft -= dt;
                SweepLungeTag();
            }
        }

        private static void Tick(ref float t, float dt)
        {
            if (t > 0.0f) t = Mathf.Max(0.0f, t - dt);
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ A SINGLE TAP, NOT A HOLD. The charge time is zero. And it runs AFTER the carrier
        /// has had first refusal, so a press that picked a slipper up never also shoves.
        /// </summary>
        private void StepShove()
        {
            if (_shoveCooldown > 0.0f) return;

            // ⚠️ `IsBusy` NOW ALSO MEANS "A GRAB ALREADY SPENT THIS PRESS". See its own note on
            // `Carrier`: a pickup and a shove are the same key, and without that word every
            // successful pickup also fired a shove.
            if (_carrier != null && _carrier.IsBusy) return;
            if (!_motor.Intent.JustPressed(Verb.Grab)) return;

            // ⚠️⚠️ FATIGUE REFUSES THE SHOVE OUTRIGHT, AND THAT CHECK WAS MISSING.
            // `character_base.gd::_step_shove` is `if _stamina < SHOVE_STAMINA_COST or
            // _fatigue_left > 0.0: return` — two conditions, and only the first was ported.
            // Fatigue is the lockout you earn by emptying the bar, so a shove that still fired
            // during it let a player spend the one resource the lockout exists to withhold, and
            // did it at the moment they were meant to be recovering.
            if (_motor.Stamina.IsFatigued) return;

            // ⚠️ THE REAL PRICE IS THE SPRINT, NOT THE POINTS. That half-bar is the same bar
            // that gets you back out of the box, so a shove is paid for in escape distance.
            if (!_motor.Stamina.Spend(Balance.ShoveStaminaCost)) return;

            // ⚠️ THE READ PLAYS ON THE SWING, NOT ON THE HIT. A shove that only animates when
            // it connects gives the other three players no warning it happened, and a miss
            // looks identical to not having pressed anything.
            // § THE VIEWMODEL RIDES ALONG. `PlayAction` drives the first-person arm too, from
            // its one call site — see its note. The arms carry no `shove` clip, so this resolves
            // to the procedural kick, which is exactly what the .gd does for it.
            Animator?.PlayAction("shove");
            GameServices.Audio?.PlayAt("bump_swing", transform.position);

            var victim = FindInCone(Balance.ShoveRange, Balance.ShoveArcDeg, requireTaggable: false);
            if (victim == null)
            {
                _shoveCooldown = Balance.ShoveMissCooldown;
                return;
            }

            ApplyShoveTo(victim);
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ LEFT-CLICK IS FREE ON A DEFENDER AND ONLY ON A DEFENDER. It is the throw charge
        /// for everyone else, and the throw refuses a defender outright, so nothing was taken
        /// from anybody to give the taya a punch.
        /// </summary>
        private void StepPunch()
        {
            if (_punchCooldown > 0.0f) return;
            if (!_motor.Intent.JustPressed(Verb.SpecialAbility)) return;

            _punchCooldown = Balance.PunchCooldown;

            // Same rule as the shove: the jab reads on the swing, and `PlayAction` now carries
            // the first-person kick with it.
            Animator?.PlayAction("punch");

            var victim = FindInCone(Balance.PunchRange, Balance.PunchArcDeg, requireTaggable: true);
            if (victim != null) GameServices.Round?.ResolveTag(_motor, victim);
        }

        /// <summary>
        /// ⚠️ A CHARGE, A DASH AND A COOLDOWN, and it answers a different problem from the
        /// punch: it is the right answer to somebody running PAST you and the wrong one to
        /// somebody standing next to you, because the charge is exactly long enough for them
        /// to leave.
        /// </summary>
        private void StepLunge(float dt)
        {
            // While the reset channel runs, the lunge charge is cancelled. They are separate
            // keys now (E channels, right click lunges), so this is no longer a shared-key
            // problem, but it stays: a taya who starts a lunge with one hand while righting the
            // can with the other should still finish the can, and this is what makes the
            // channel uninterruptible from their own inputs.
            if (_carrier != null && _carrier.ChannelRatio > 0.0f)
            {
                _lungeCharging = false;
                _lungeCharge = 0.0f;
                return;
            }

            if (_lungeCooldown > 0.0f) return;

            if (_motor.Intent.Pressed(Verb.Lunge))
            {
                _lungeCharging = true;
                _lungeCharge += dt;
                return;
            }

            if (!_lungeCharging) return;

            _lungeCharging = false;
            float power = Mathf.Clamp(_lungeCharge / Balance.LungeChargeTime, Balance.LungeMinPower, 1.0f);
            _lungeCharge = 0.0f;
            ReleaseLunge(power);
        }

        private void ReleaseLunge(float power)
        {
            _lungeCooldown = Balance.LungeCooldown;
            _lungeActiveLeft = Balance.LungeActiveTime;
            _lungeFrom = transform.position;

            // ⚠️ ITS OWN CLIP, NOT THE SHOVE'S. attack-kick-right leads with the body, which is
            // what a dash INTO somebody looks like; the punch leads with the arm. These were
            // one animation for three verbs until 2026-08-01.
            // § THE TAG, IN THE PLAYER'S OWN HANDS TOO. `PlayAction` reaches the viewmodel; the
            // 1.4 kick here is the lunge's own harder shove of the view, kept because a dash is
            // meant to hit the camera harder than a jab.
            Animator?.PlayAction("lunge");
            Rig?.ViewmodelKick(Vector3.forward, 1.4f);

            // ⚠️ A VELOCITY IMPULSE, NOT A TELEPORT. The friction model integrates it down and
            // the intervening frames are what the sweep reads. It is also why the taya can be
            // body-blocked mid-lunge instead of passing through geometry.
            Vector3 forward = transform.forward;
            forward.y = 0.0f;
            _motor.ApplyImpulse(forward.normalized * Balance.LungeSpeed * power);
        }

        /// <summary>
        /// ⚠️⚠️ THE SWEEP RUNS EVERY FRAME THE LUNGE IS LIVE, NOT ONCE AT THE END. A dash at
        /// 60 Hz tunnels straight past a body standing halfway along it otherwise. Measured in
        /// the original: the furthest start that still tags is IDENTICAL against a stationary
        /// target and one crossing at full attacker walk speed, which is what proves there is
        /// no tunnelling. The tag is a lead problem, not a reach problem.
        /// </summary>
        private void SweepLungeTag()
        {
            var round = GameServices.Round;
            if (round == null || round.Lata == null || !round.Lata.IsUpright) return;

            foreach (var p in round.Players)
            {
                if (p == null || p == _motor || p.IsDefender) continue;
                if (!p.IsTaggable()) continue;

                // Distance to the dash SEGMENT, not to the endpoint: the same region the
                // per-frame sweep covers, and a segment has no sampling rate.
                Vector3 a = Flat(_lungeFrom);
                Vector3 b = Flat(transform.position);
                Vector3 t = Flat(p.transform.position);

                if (DistanceToSegment(t, a, b) > Balance.LungeTagRadius) continue;

                round.ResolveTag(_motor, p);
                _lungeActiveLeft = 0.0f; // one tag per lunge
                return;
            }
        }

        // -------------------------------------------------------------------
        // HOST-SIDE RESOLUTION.
        //
        // ⚠️⚠️ THESE ARE THE ONLY PLACES A VERB LANDS, AND BOTH PATHS COME THROUGH THEM. The
        // solo game calls them directly; a client asks over the wire and the host calls the
        // same function. That is what stops networked play quietly obeying different rules
        // from single player — the failure this whole indirection exists to prevent.
        // -------------------------------------------------------------------

        /// <summary>The taya's jab. Instant, no charge, more reach than the lunge.</summary>
        public void HostResolvePunch(Vector3 from, Vector3 facing)
        {
            if (!NetAuthority.ShouldResolve()) return;

            var victim = FindInCone(from, facing, Balance.PunchRange, Balance.PunchArcDeg,
                                    requireTaggable: true);

            if (victim != null) GameServices.Round?.ResolveTag(_motor, victim);
        }

        /// <summary>
        /// The taya's dash. ⚠️ THE IMPULSE IS APPLIED HOST-SIDE AND THE SWEEP FOLLOWS IT, so a
        /// lunge cannot tag from a position the dash never actually reached.
        /// </summary>
        public void HostResolveLunge(Vector3 from, Vector3 facing, float power)
        {
            if (!NetAuthority.ShouldResolve()) return;

            _lungeCooldown = Balance.LungeCooldown;
            _lungeActiveLeft = Balance.LungeActiveTime;
            _lungeFrom = from;

            Vector3 flat = facing;
            flat.y = 0.0f;
            _motor.ApplyImpulse(flat.normalized * Balance.LungeSpeed * power);
        }

        /// <summary>An attacker shoving a rival, resolved from the sender's own frame.</summary>
        public void HostResolveShove(Vector3 from, Vector3 facing)
        {
            if (!NetAuthority.ShouldResolve()) return;

            var victim = FindInCone(from, facing, Balance.ShoveRange, Balance.ShoveArcDeg,
                                    requireTaggable: false);

            if (victim == null)
            {
                _shoveCooldown = Balance.ShoveMissCooldown;
                return;
            }

            ApplyShoveTo(victim);
        }

        /// <summary>The push itself, shared by the local and networked paths.</summary>
        private void ApplyShoveTo(CharacterMotor victim)
        {
            Vector3 push = victim.transform.position - transform.position;
            push.y = 0.0f;
            push = push.normalized * Balance.ShoveSpeed
                   * Roster.PersonPowerScale(_motor.CharacterIndex, _motor.Mode)
                   / Roster.PersonGritScale(victim.CharacterIndex, victim.Mode);
            push.y = Balance.ShoveLift;

            victim.ApplyImpulse(push);
            victim.ApplyStagger(Balance.ShoveStun);
            Visual.DizzyStars.Attach(victim.transform, Balance.ShoveStun);
            Visual.ComicPopup.Bonk(victim.transform.position);

            GameServices.Round?.NoteShove(victim.PlayerSlot, _motor.PlayerSlot);
            _shoveCooldown = Balance.ShoveCooldown;
        }

        // -------------------------------------------------------------------

        private CharacterMotor FindInCone(float range, float halfAngleDeg, bool requireTaggable)
            => FindInCone(transform.position, transform.forward, range, halfAngleDeg, requireTaggable);

        /// <summary>
        /// ⚠️ THE ORIGIN AND FACING ARE PARAMETERS SO THE HOST CAN JUDGE FROM THE CLIENT'S OWN
        /// FRAME. A networked verb must be resolved against where the client BELIEVED it was
        /// standing when it pressed, not where the host thinks it is now — otherwise every
        /// verb is judged a frame or two late and misses on a lagged connection while looking
        /// like a clean hit on the sender's screen.
        /// </summary>
        private CharacterMotor FindInCone(Vector3 origin, Vector3 facingRaw,
            float range, float halfAngleDeg, bool requireTaggable)
        {
            var round = GameServices.Round;
            if (round == null) return null;

            CharacterMotor best = null;
            float bestDist = float.MaxValue;

            Vector3 facing = facingRaw;
            facing.y = 0.0f;
            facing.Normalize();

            foreach (var p in round.Players)
            {
                if (p == null || p == _motor) continue;
                if (requireTaggable && !p.IsTaggable()) continue;

                // Attackers shove attackers. The defender is neither a shover nor a target.
                if (!requireTaggable && (p.IsDefender || _motor.IsDefender)) continue;

                Vector3 to = p.transform.position - origin;
                to.y = 0.0f;

                float d = to.magnitude;
                if (d > range || d >= bestDist) continue;

                float angle = Vector3.Angle(facing, to.normalized);
                if (!Combat.InCone(d, angle, range, halfAngleDeg)) continue;

                bestDist = d;
                best = p;
            }

            return best;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0.0f, v.z);

        private static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 0.0001f) return Vector3.Distance(p, a);

            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2);
            return Vector3.Distance(p, a + ab * t);
        }
    }
}
