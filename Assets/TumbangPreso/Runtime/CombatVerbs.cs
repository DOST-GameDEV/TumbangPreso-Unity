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
        private float _shoveCooldownTotal = Balance.ShoveCooldown;
        private float _punchCooldown;
        private float _punchCooldownTotal = Balance.PunchCooldown;
        private float _lungeCooldown;
        private bool _punchPressSpent;

        private float _lungeCharge;
        private bool _lungeCharging;
        private float _observedLunge=-1,_observedLungeAt=-100,_lungeSyncAt;
        private bool _observedLungeFromInput;
        private bool _sentLunge;
        private float _lungeActiveLeft;
        private Vector3 _lungeFrom;

        /// <summary>An attacker's Shove / Lunge press already became a shove.</summary>
        private bool _shoveLungePressSpent;

        public float ShoveCooldownLeft => _shoveCooldown;
        public float ShoveCooldownDuration => _shoveCooldownTotal;
        public void ConfirmShoveResult(bool hit)
        {
            float elapsed = Mathf.Max(0, _shoveCooldownTotal - _shoveCooldown);
            _shoveCooldownTotal = hit ? Balance.ShoveCooldown : Balance.ShoveMissCooldown;
            _shoveCooldown = Mathf.Max(0, _shoveCooldownTotal - elapsed);
        }
        public float PunchCooldownLeft => _punchCooldown;
        public float PunchCooldownDuration => _punchCooldownTotal;

        public void ConfirmPunchResult(bool hit)
        {
            float elapsed = Mathf.Max(0, _punchCooldownTotal - _punchCooldown);
            _punchCooldownTotal = hit ? Balance.PunchHitCooldown : Balance.PunchCooldown;
            _punchCooldown = Mathf.Max(0, _punchCooldownTotal - elapsed);
        }
        public float LungeCooldownLeft => _lungeCooldown;

        /// <summary>Legacy diagnostic surface for the removed retrieval slide.</summary>
        public float SlideCooldownLeft => 0;

        /// <summary>The removed retrieval slide never owns an active window.</summary>
        public bool SlideActive => false;
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
            _lungeCharging ? Mathf.Clamp01(_lungeCharge / Balance.LungeChargeTime) :
            _motor!=null && _motor.IsDefender && _motor.CanAct() && _observedLunge>=0 && Time.time-_observedLungeAt<.85f
                ? Mathf.Clamp01((_observedLunge+Time.time-_observedLungeAt)/Balance.LungeChargeTime) : -1.0f;

        public void ApplyObservedLungeCharge(bool active,float seconds=0)
        {
            if(float.IsNaN(seconds) || float.IsInfinity(seconds))return;
            _observedLungeFromInput = active && _observedLungeFromInput && NetAuthority.IsHost
                                      && (_motor.PlayerSlot == NetAuthority.LocalSlot || _motor.IsBot);
            _observedLunge=active?Mathf.Clamp(seconds,0,Balance.LungeChargeTime):-1;
            _observedLungeAt=Time.time;
        }

        private void LateUpdate()
        {
            // Only the actual driver publishes input. Remote presentation never
            // writes _lungeCharging, input, impulse, recovery, score or tag state.
            bool driver=!NetAuthority.IsNetworked || _motor.PlayerSlot==NetAuthority.LocalSlot || (NetAuthority.IsHost && _motor.IsBot);
            if(!driver)return;
            bool active=_motor.IsDefender && _motor.CanAct() && _lungeCharging;
            if(active!=_sentLunge || (active && Time.time>=_lungeSyncAt))
            {
                _sentLunge=active;_lungeSyncAt=Time.time+.12f;
                if(NetAuthority.IsNetworked)Net.MatchRpc.Instance?.SetThrowCharge(_motor.PlayerSlot,active,_lungeCharge,0,true);
                // The host may synchronously apply its own published sample.
                _observedLungeFromInput = active;
            }
        }

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

        private void OnDisable() => RetireActions();

        internal void RetireActions()
        {
            // A retired body must not resume a contact sweep from its old position.
            // Keep the spent cooldown; an ordinary clock hold leaves this component enabled.
            _lungeActiveLeft = 0.0f;
            CancelPendingInput();
        }

        internal void CancelPendingInput() => RetireProducerInput(true);

        internal void RetireProducerInput(bool clearReceivedPresentation)
        {
            // Input retirement cancels the windup, not an already committed contact window.
            _lungeCharging = false;
            _lungeCharge = 0.0f;
            if (clearReceivedPresentation || _observedLungeFromInput)
            {
                _observedLunge = -1.0f;
                _observedLungeFromInput = false;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            Tick(ref _shoveCooldown, dt);
            Tick(ref _punchCooldown, dt);
            Tick(ref _lungeCooldown, dt);

            // A refused prediction refunds cooldown, not ownership of its input edge.
            // Observe release even when interruption or role change skips the punch path.
            if (!_motor.Intent.Pressed(Verb.SpecialAbility) && !_motor.Intent.JustPressed(Verb.SpecialAbility))
                _punchPressSpent = false;

            // Shared-button release also rearms while stun or a role change defers its verb.
            if (!_motor.Intent.Pressed(Verb.Lunge)) _shoveLungePressSpent = false;

            if (!_motor.CanAct())
            {
                // A real interruption retires the dash contact windows. A presentation
                // hold or offline pause merely freezes it and must survive Resume.
                if (!_motor.RoundActive || _motor.IsStunned || _motor.IsFeared)
                {
                    _lungeActiveLeft = 0.0f;
                        }
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
                // A defender windup cannot follow this body into its attacker role.
                CancelPendingInput();
                // Retrieval slide was removed by the owner. This press only shoves;
                // ordinary pickup keeps its separate input and press ownership.
                if (_motor.Intent.Pressed(Verb.Lunge) && !_shoveLungePressSpent)
                {
                    float before = _shoveCooldown;
                    StepShove();
                    if (_shoveCooldown > before) _shoveLungePressSpent = true;
                }
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

            // ⚠️ `IsBusy` still refuses a shove mid-charge or mid-channel. The shove left the pickup
            // key on 2026-09-27 (it is Shove / Lunge now, `Verb.Lunge`), so a pickup can no longer
            // fire one; before that, `IsBusy` also carried "a grab already spent this press".
            if (_carrier != null && _carrier.IsBusy) return;
            if (!_motor.Intent.JustPressed(Verb.Lunge)) return;

            // Owner playtest revision: shove is free even with an empty/fatigued bar.
            // Cooldown, role, action and carrier/channel gates still apply.

            // ⚠️ THE READ PLAYS ON THE SWING, NOT ON THE HIT. A shove that only animates when
            // it connects gives the other three players no warning it happened, and a miss
            // looks identical to not having pressed anything.
            // § THE VIEWMODEL RIDES ALONG. `PlayAction` drives the first-person arm too, from
            // its one call site. See its note.
            //
            // ⚠️⚠️ THE KICK IS ASKED FOR EXPLICITLY NOW, AND IT HAS TO BE. The arms used to carry
            // no `shove` clip, so `PlayViewmodelAction` fell through to its procedural kick and
            // this line got the view shake for free. They carry one as of 2026-08-28, and that
            // fallback is documented to retire the moment a clip with the name exists, so the
            // free kick went with it: the shove would have gained an arm and quietly lost its
            // weight on the same commit. `ReleaseLunge` has always asked outright, at 1.4,
            // because a dash is meant to hit the camera harder than a push.
            Animator?.PlayAction("shove");
            Rig?.ViewmodelKick(Vector3.forward);
            // ⚠️ VARIED, LIKE EVERY OTHER CUE THAT FIRES THIS OFTEN.
            // `AudioDirector.PlayAtVaried`'s own header states the rule, and the throw release,
            // the pickup, the landing, the jump, the bump and the tag all obey it; the shove did
            // not. ⚠️ `PlayVaried` and not `PlayAtVaried`: this was already relayed and must stay
            // relayed. `audit_cue_relay.py`'s `CALL` pattern covers both names, so its
            // `OWNER_DRIVEN` row for this cue still binds.
            NetCue.PlayVaried("bump_swing", transform.position);

            if (NetAuthority.ShouldRequest())
            {
                _shoveCooldown = _shoveCooldownTotal = Balance.ShoveCooldown;
                Net.MatchRpc.Instance?.RequestShoveServerRpc(
                    _motor.PlayerSlot, transform.position, transform.forward);
                return;
            }

            if (NetAuthority.IsNetworked)
                Net.MatchRpc.Instance?.BroadcastAction(_motor.PlayerSlot, "shove");

            var victim = FindInCone(Balance.ShoveRange, Balance.ShoveArcDeg, requireTaggable: false);

            // ⚠️⚠️ COUNTED HERE AND IN `HostResolveShove`, WHICH IS TWO SITES FOR ONE STAT AND
            // IS NOT A DUPLICATE. These are two different bodies: this line is reached for the
            // host's own seat and in solo play, and a CLIENT has already returned above at
            // `ShouldRequest()` so its shove is counted once, on the host, when the request
            // lands. Counting at the press instead would be one site and would count a shove
            // twice for every client in the room.
            //
            // ⚠️ EVERYTHING THAT CAN REFUSE THE VERB IS ABOVE THIS LINE: the cooldown, being
            // the taya and the action/channel gates. A press that never became a
            // shove is not a miss, and counting it as one makes the hit rate a measure of how
            // often somebody mashed.
            GameServices.Stats?.NoteShoveAttempt(_motor.PlayerSlot, victim != null);

            if (victim == null)
            {
                _shoveCooldown = _shoveCooldownTotal = Balance.ShoveMissCooldown;
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
            if (_punchCooldown > 0.0f || _punchPressSpent) return;
            if (!_motor.Intent.JustPressed(Verb.SpecialAbility)) return;

            _punchCooldown = _punchCooldownTotal = Balance.PunchCooldown;
            _punchPressSpent = true;

            // Same rule as the shove: the jab reads on the swing, in both views, and it asks for
            // its own view kick rather than relying on the fallback the new `punch` clip has now
            // retired. See the note on the shove.
            //
            // ⚠️ AT THE DEFAULT STRENGTH, NOT THE LUNGE'S 1.4. The taya carries two tag verbs and
            // the whole reason to have both is that they feel different: the jab is cheap,
            // instant and close, the dash is a commitment. One shake for both flattens that.
            Animator?.PlayAction("punch");
            Rig?.ViewmodelKick(Vector3.forward);

            if (NetAuthority.ShouldRequest())
            {
                Net.MatchRpc.Instance?.RequestPunchServerRpc(
                    _motor.PlayerSlot, transform.position, transform.forward);
                return;
            }

            if (NetAuthority.IsNetworked)
                Net.MatchRpc.Instance?.BroadcastAction(_motor.PlayerSlot, "punch");

            var victim = FindInCone(Balance.PunchRange, Balance.PunchArcDeg, requireTaggable: true);
            if (victim != null && GameServices.Round?.TryResolveTag(_motor, victim) == true) ConfirmPunchResult(true);
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
            _lungeCooldown = Combat.LungeCooldownFor(power);

            // ⚠️ THE SAME TWO-SITE PAIRING THE SHOVE ABOVE EXPLAINS. This is the host's own
            // body and the solo game; `HostResolveLunge` is a client's, resolved on the host.
            GameServices.Stats?.NoteLungeAttempt(_motor.PlayerSlot);
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

            if (NetAuthority.ShouldRequest())
            {
                Net.MatchRpc.Instance?.RequestLungeServerRpc(
                    _motor.PlayerSlot, _lungeFrom, forward, power);
            }
            else if (NetAuthority.IsNetworked)
            {
                Net.MatchRpc.Instance?.BroadcastAction(_motor.PlayerSlot, "lunge");
            }
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
            // ⚠️⚠️ A TAG IS A DECISION AND ONLY THE HOST MAKES DECISIONS. The sweep ran on
            // whichever peer was lunging, so a client's dash called `RoundDirector.ResolveTag`
            // locally: it staggered a body it does not own, respawned somebody on its own screen
            // alone, and asked for a score the host had not awarded. The host runs this same
            // sweep off `HostResolveLunge`, from the position the client reported, and its result
            // is the one everybody sees. `CLAUDE.md` § 4: contact resolves by distance ON THE
            // HOST.
            if (!NetAuthority.ShouldResolve()) return;

            var round = GameServices.Round;
            if (round == null || round.Lata == null || !round.Lata.IsUpright) return;

            // `Bodies`, not `Players`: a companion attacker is taggable like any attacker (plan 9.12).
            foreach (var p in round.Bodies)
            {
                if (p == null || p == _motor || p.IsDefender) continue;
                if (!p.IsTaggable()) continue;

                // Distance to the dash SEGMENT, not to the endpoint: the same region the
                // per-frame sweep covers, and a segment has no sampling rate.
                Vector3 a = Flat(_lungeFrom);
                Vector3 b = Flat(transform.position);
                Vector3 t = Flat(p.transform.position);

                if (DistanceToSegment(t, a, b) > Balance.LungeTagRadius * p.TagReachScale) continue;

                // ⚠️ COUNTED BEFORE THE TAG RATHER THAN AFTER IT. `ResolveTag` re-checks the
                // whole world and can still refuse, but a refusal there means the sweep found
                // somebody the rules protect, not that the lunge missed. A hit rate counting
                // only the tags that scored would be measuring the victim's state instead of
                // the taya's aim.
                GameServices.Stats?.NoteLungeHit(_motor.PlayerSlot);
                round.ResolveTag(_motor, p);
                _lungeActiveLeft = 0.0f; // one tag per lunge
                return;
            }
        }

        // -------------------------------------------------------------------
        // Retained query for tooling compatibility; no player or bot can start a slide.
        public bool SlideMayStartFrom(Vector3 from, Vector3 facing, out Slipper target)
        { target = null; return false; }

        // HOST-SIDE RESOLUTION.
        //
        // ⚠️⚠️ THESE ARE THE ONLY PLACES A VERB LANDS, AND BOTH PATHS COME THROUGH THEM. The
        // solo game calls them directly; a client asks over the wire and the host calls the
        // same function. That is what stops networked play quietly obeying different rules
        // from single player — the failure this whole indirection exists to prevent.
        // -------------------------------------------------------------------

        /// <summary>The taya's jab. Instant, no charge, more reach than the lunge.</summary>
        public bool HostResolvePunch(Vector3 from, Vector3 facing)
        {
            if (!NetAuthority.ShouldResolve() || _punchCooldown > 0.0f ||
                !_motor.IsDefender || !_motor.CanAct()) return false;

            _punchCooldown = _punchCooldownTotal = Balance.PunchCooldown;
            Animator?.PlayAction("punch");

            var victim = FindInCone(from, facing, Balance.PunchRange, Balance.PunchArcDeg,
                                    requireTaggable: true);

            if (victim != null && GameServices.Round?.TryResolveTag(_motor, victim) == true) ConfirmPunchResult(true);
            return true;
        }

        /// <summary>
        /// The taya's dash. ⚠️ THE IMPULSE IS APPLIED HOST-SIDE AND THE SWEEP FOLLOWS IT, so a
        /// lunge cannot tag from a position the dash never actually reached.
        /// </summary>
        public bool HostResolveLunge(Vector3 from, Vector3 facing, float power)
        {
            if (!NetAuthority.ShouldResolve() || _lungeCooldown > 0.0f ||
                !_motor.IsDefender || !_motor.CanAct()) return false;

            _lungeCooldown = Combat.LungeCooldownFor(power);
            _lungeActiveLeft = Balance.LungeActiveTime;
            _lungeFrom = from;

            // The other half of the pair; see `ReleaseLunge`.
            GameServices.Stats?.NoteLungeAttempt(_motor.PlayerSlot);

            Vector3 flat = facing;
            flat.y = 0.0f;
            _motor.ApplyImpulse(flat.normalized * Balance.LungeSpeed * power);
            Animator?.PlayAction("lunge");
            return true;
        }

        /// <summary>Refuse the retired wire verb without spending or moving anything.</summary>
        public bool HostResolveSlide(Vector3 from, Vector3 facing) => false;

        /// <summary>An attacker shoving a rival, resolved from the sender's own frame.</summary>
        public bool HostResolveShove(Vector3 from, Vector3 facing)
        {
            if (!NetAuthority.ShouldResolve() || _shoveCooldown > 0.0f ||
                _motor.IsDefender || !_motor.CanAct()) return false;

            Animator?.PlayAction("shove");

            var victim = FindInCone(from, facing, Balance.ShoveRange, Balance.ShoveArcDeg,
                                    requireTaggable: false);

            // The other half of the pair; see the note on the local path above.
            GameServices.Stats?.NoteShoveAttempt(_motor.PlayerSlot, victim != null);

            if (victim == null)
            {
                _shoveCooldown = _shoveCooldownTotal = Balance.ShoveMissCooldown;
                return true;
            }

            ApplyShoveTo(victim);
            return true;
        }

        // -------------------------------------------------------------------
        // § THE REFUSAL, WHICH IS THE OTHER HALF OF A PREDICTED VERB
        //
        // ⚠️⚠️ A CLIENT PAYS FOR ALL THREE OF THESE BEFORE IT ASKS, AND THE HOST USED TO REFUSE
        // IN SILENCE. `StepPunch` stamps `_punchCooldown`, `ReleaseLunge` stamps `_lungeCooldown`
        // and `_lungeActiveLeft` and applies its own impulse, and `StepShove` spends
        // `Balance.ShoveStaminaCost` AND stamps `_shoveCooldown`, every one of them before the
        // `ShouldRequest()` branch sends anything. Each of the three `HostResolve` methods above
        // returns false on a refusal and `MatchRpc` threw that answer away.
        //
        // This is the same defect `HostDenyAbilityCast` was built for one file away, and it was
        // found by walking every request handler in `MatchRpc` against that shape rather than by
        // playing. `docs/TODO.md` § 135.2 has the table of all eight handlers and which three
        // have it.
        //
        // ⚠️⚠️ AND IT IS WORSE HERE THAN IT IS FOR AN ABILITY, FOR TWO REASONS THAT BOTH HAD TO
        // BE CHECKED RATHER THAN ASSUMED.
        //   1. An ability had a 5 Hz `SyncAbility` writing the host's cooldown over the client's,
        //      so much so that § 71 had to build `mayLower` to STOP a refused cast healing
        //      itself. **These three cooldowns are on no wire at all.**
        //   2. The stamina does not heal either, which is the part that looks wrong until you
        //      read `CharacterMotor.StepNetworkTransform`: `SyncUnit` DOES carry
        //      `Stamina.Current`, but it is only broadcast for a body the host actually drives
        //      (`HostDrivesThisBody`), and a remote human's seat is deliberately not
        //      re-broadcast because echoing it back fights the 50 Hz stream its owner is
        //      sending. **The one seat that can be refused is the one seat no snapshot corrects.**
        //
        // ⚠️ THE LUNGE IMPULSE IS NOT TAKEN BACK, AND THAT IS DELIBERATE. A refused lunge has
        // already moved the body a few centimetres, and `SubmitMove` reconciles a position every
        // physics step anyway. Yanking the velocity to zero here would make a refusal look like
        // running into a wall, which is a worse lie than a short slide.
        // -------------------------------------------------------------------

        /// <summary>Gives back what a verb the host refused had already charged this peer.</summary>
        public void RollBackRefusedVerb(Net.MatchRpc.DeniedVerb verb, bool refundResources = true)
        {
            switch (verb)
            {
                case Net.MatchRpc.DeniedVerb.Punch:
                    _punchCooldown = 0.0f;
                    break;

                case Net.MatchRpc.DeniedVerb.Lunge:
                    _lungeCooldown = 0.0f;

                    // ⚠️⚠️ THE ACTIVE WINDOW GOES TOO, AND IT IS THE HALF THAT IS NOT ABOUT
                    // FAIRNESS TO THE REFUSED PLAYER. `_lungeActiveLeft` is the only gate on
                    // `SweepLungeTag`, which hands out tags. Returning the cooldown and leaving
                    // the window open would let a dash the host never ran keep hunting for a
                    // victim on this screen for `Balance.LungeActiveTime`, and a tag is scored
                    // host-side, so the two peers would disagree about a POINT.
                    _lungeActiveLeft = 0.0f;
                    break;

                case Net.MatchRpc.DeniedVerb.Shove:
                    _shoveCooldown = 0.0f;

                    // ⚠️ THE BAR IS THE HALF THAT MATTERS. `CLAUDE.md` § 4: the real price of a
                    // shove is the sprint it costs, so a refusal that returned only the cooldown
                    // would still have taken the escape distance and the player would never know
                    // why they could not get out of the box.
                    // Shoves spend no stamina, so a refusal must not credit any.
                    break;

                case Net.MatchRpc.DeniedVerb.Slide:
                    // Retired verb: it spends nothing and cannot refund a current action.
                    break;
            }
        }

        /// <summary>
        /// `Time.time` of the last shove that actually moved somebody, or a large negative
        /// number if this seat has never landed one.
        ///
        /// ⚠️⚠️ IT EXISTS BECAUSE A COOLDOWN IS NOT A HIT, AND THE TUTORIAL WAS READING ONE AS
        /// THE OTHER. `GuidedTraining` completed SHOVE, PUNCH and LUNGE the moment the matching
        /// `*CooldownLeft` rose above its baseline, which is the verb having FIRED. 🧑
        /// 2026-09-02: *"sometimes some tasks get marked even if u dont rlly do them like
        /// pushing ppl (as long as u click push it gets marked as done)"*. Every one of those
        /// three cooldowns is set before the cone is searched, so a press into empty air
        /// completed the lesson and the student was taught that the verb needs no aim.
        ///
        /// ⚠️ IT IS A TIMESTAMP RATHER THAN A COUNTER BECAUSE THE READER IS A LESSON WITH A
        /// START. A counter would have to be zeroed by whoever reads it, which is a second
        /// writer on a field the combat code owns; a lesson can simply remember the time it
        /// began and ask whether anything has landed since.
        ///
        /// ⚠️ SET IN `ApplyShoveTo`, WHICH IS THE ONE PLACE A SHOVE LANDS. `StepShove` reaches
        /// it in solo play and on the host's own seat, `HostResolveShove` reaches it for a
        /// client's request, and neither can push anybody without coming through here.
        /// </summary>
        public float LastShoveLandedAt { get; private set; } = -999.0f;

        /// <summary>The push itself, shared by the local and networked paths.</summary>
        private void ApplyShoveTo(CharacterMotor victim)
        {
            LastShoveLandedAt = Time.time;

            Vector3 push = victim.transform.position - transform.position;
            push.y = 0.0f;
            push = push.normalized * Balance.ShoveSpeed
                   * Roster.PersonPowerScale(_motor.CharacterIndex, _motor.Mode)
                   / Roster.PersonGritScale(victim.CharacterIndex, victim.Mode);
            push.y = Balance.ShoveLift;

            victim.ApplyResolvedImpact(push);
            victim.ApplyStagger(Balance.ShoveStun);
            if (_motor.Mode == GameMode.HeroStrike && _motor.AbilitySystem?.Kit is Abilities.CheskaHeroKit)
                victim.ApplyChilled();
            Visual.DizzyStars.Attach(victim.transform, Balance.ShoveStun);
            Visual.ComicPopup.Bonk(victim.transform.position);

            GameServices.Round?.NoteShove(victim.PlayerSlot, _motor.PlayerSlot);
            _shoveCooldown = _shoveCooldownTotal = Balance.ShoveCooldown;
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

            foreach (var p in round.Bodies)
            {
                if (p == null || p == _motor) continue;
                if (requireTaggable && !p.IsTaggable()) continue;
                // A companion and its owner never shove each other: it is on her side.
                if (CompanionSeats.OwnerOf(p.PlayerSlot) == CompanionSeats.OwnerOf(_motor.PlayerSlot)) continue;

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
