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

        private float _slideCooldown;
        private float _slideActiveLeft;
        private Vector3 _slideFrom;

        public float ShoveCooldownLeft => _shoveCooldown;
        public float PunchCooldownLeft => _punchCooldown;
        public float LungeCooldownLeft => _lungeCooldown;

        /// <summary>How long before this attacker may commit to another retrieval slide.</summary>
        public float SlideCooldownLeft => _slideCooldown;

        /// <summary>True while the slide is live and its sweep is looking for a tsinelas.</summary>
        public bool SlideActive => _slideActiveLeft > 0.0f;
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
            Tick(ref _slideCooldown, dt);

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
                StepSlide(dt);
            }

            if (_lungeActiveLeft > 0.0f)
            {
                _lungeActiveLeft -= dt;
                SweepLungeTag();
            }

            if (_slideActiveLeft > 0.0f)
            {
                _slideActiveLeft -= dt;
                SweepSlideRetrieval();
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
            NetCue.Play("bump_swing", transform.position);

            if (NetAuthority.ShouldRequest())
            {
                _shoveCooldown = Balance.ShoveCooldown;
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
            // the taya, a fatigued bar and the stamina spend. A press that never became a
            // shove is not a miss, and counting it as one makes the hit rate a measure of how
            // often somebody mashed.
            GameServices.Stats?.NoteShoveAttempt(_motor.PlayerSlot, victim != null);

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
        // § THE ATTACKER'S RETRIEVAL SLIDE
        //
        // ⚠️⚠️ IT IS THE SAME PRESS AS THE TAYA'S LUNGE AND IT REPLACES NOTHING. `Verb.Lunge` is
        // read here only behind `if (_motor.IsDefender)`, so on the three attackers in every
        // round the key, the pad's left trigger and the touch layer's LUNGE button all did
        // literally nothing. This is that dead control given the one job an attacker actually
        // has: `docs/VISION.md` § 0, *"the tension is the retrieval, not the throw"*.
        //
        // ⚠️⚠️ IT ADDS NO METER, NO WINDOW AND NO TIMING. `docs/VISION.md` § 1.1 forbids Classic
        // a power and `CLAUDE.md` § 6.2 forbids anything else to hold in the head. The decision
        // it creates is one sentence long: *I can walk up and pick this up safely, or I can commit
        // and get there a third of a second sooner.* Everything that makes it a decision rather
        // than a free buff is COMMITMENT, reduced steering, a recovery, a cooldown and a stamina
        // price, rather than a status effect, because a commitment is something the taya can see
        // and read and a status effect is something that happens to somebody.
        //
        // ⚠️ THE PICKUP RULE IS NOT RESTATED HERE. `Slipper.CanBeGrabbedBy` decides eligibility
        // and `Slipper.HostGrab` performs it, exactly as a walking pickup does, so a slide cannot
        // collect anything a walk-up could not: not the taya's parked shoe, not one in flight,
        // not one somebody else is holding. `docs/VISION.md` § 4's *"the host decides everything
        // that scores"* is unchanged, and so is § 4's *"contact resolves by DISTANCE on the
        // host"*.
        // -------------------------------------------------------------------

        private void StepSlide(float dt)
        {
            if (_slideCooldown > 0.0f) return;

            // ⚠️⚠️ IT IS AN EDGE, NOT A HOLD, AND THAT IS THE DIFFERENCE FROM THE LUNGE. The taya
            // charges because the charge is *"exactly long enough for them to leave"*, it is
            // aimed at a moving person. A retrieval is aimed at an object lying on the ground that
            // is not going anywhere, so a wind-up would only tell the taya what is coming without
            // asking the attacker for anything in return. The commitment is spent AFTER the press
            // instead of before it.
            if (!_motor.Intent.JustPressed(Verb.Lunge)) return;

            // ⚠️ NOTHING TO FETCH MEANS NOTHING HAPPENS, AND THE PRESS IS NOT SPENT. A slide with
            // no tsinelas in front of it is a free 1.75 m dash, which is the mobility buff this
            // verb must not be. `CLAUDE.md` § 6.3: a control that does nothing must not look
            // pressable, and the HUD prompt is what says when it will work.
            if (_carrier != null && _carrier.Held != null) return;
            if (!AnySlideTargetAhead()) return;

            // ⚠️ FATIGUE REFUSES IT, LIKE THE SHOVE. `HostResolveShove` opens with
            // `_motor.Stamina.IsFatigued` for the same reason: a bar that has bottomed out is the
            // one moment the game already says you have overcommitted, and letting a commitment
            // verb through it would make the bar advisory.
            if (_motor.Stamina.IsFatigued) return;
            if (!_motor.Stamina.Spend(Balance.SlideStaminaCost)) return;

            ReleaseSlide();
        }

        /// <summary>
        /// Whether there is a tsinelas this attacker could legally take within a slide of here.
        ///
        /// ⚠️⚠️ IT ASKS THE SAME QUESTION `Carrier.TryPickup` ASKS, ONE RADIUS FURTHER OUT, AND
        /// IT MUST KEEP DOING SO. `Slipper.CanBeGrabbedBy` already tests the state, the role and
        /// the reach; this widens only the reach, by exactly the ground the slide covers. A second
        /// eligibility rule here would be a second answer to "whose shoe is this", which is the
        /// fault `docs/TODO.md` § 94.1 is about.
        ///
        /// ⚠️ IT IS A LOCAL PREDICATE AND NOT AN AUTHORITY. A client uses it to decide whether to
        /// spend the press; the host re-asks everything when the sweep runs.
        /// </summary>
        private bool AnySlideTargetAhead()
        {
            var round = GameServices.Round;
            if (round == null) return false;

            Vector3 from = Flat(transform.position);
            Vector3 forward = transform.forward;
            forward.y = 0.0f;
            if (forward.sqrMagnitude < 0.0001f) return false;

            Vector3 to = from + forward.normalized * Balance.SlideDistance;

            foreach (var s in FindObjectsByType<Slipper>(FindObjectsSortMode.None))
            {
                if (s == null || s.State != SlipperState.Loose) continue;
                if (DistanceToSegment(Flat(s.transform.position), from, to) > Balance.PickupRadius)
                    continue;

                return true;
            }

            return false;
        }

        private void ReleaseSlide()
        {
            _slideCooldown = Balance.SlideCooldown;
            _slideActiveLeft = Balance.SlideActiveTime;
            _slideFrom = transform.position;

            // ⚠️⚠️ THE COMMITMENT COVERS THE SLIDE **AND** THE RECOVERY, and the recovery is the
            // half that makes it punishable. During the slide the attacker is moving fast and is
            // hard to catch; the 0.61 s afterwards is when a taya who read it arrives. Committing
            // only for the dash would be committing for the part that is already an advantage.
            _motor.Commit(Balance.SlideActiveTime + Balance.SlideRecoveryTime);

            // ⚠️ ITS OWN CLIP IS THE LUNGE'S, DELIBERATELY. Both are a body-led dash and the rig
            // has one; `docs/TODO.md` § 146 carries the note that a slide of its own is art work
            // rather than code work, and a shared clip that reads correctly beats a wrong one.
            Animator?.PlayAction("lunge");
            Rig?.ViewmodelKick(Vector3.forward, 1.1f);

            Vector3 forward = transform.forward;
            forward.y = 0.0f;
            _motor.ApplyImpulse(forward.normalized * Balance.SlideSpeed);

            GameServices.Audio?.PlayAt("dash", transform.position);

            if (NetAuthority.ShouldRequest())
            {
                Net.MatchRpc.Instance?.RequestSlideServerRpc(
                    _motor.PlayerSlot, _slideFrom, forward);
            }
            else if (NetAuthority.IsNetworked)
            {
                Net.MatchRpc.Instance?.BroadcastAction(_motor.PlayerSlot, "lunge");
            }
        }

        /// <summary>
        /// Collects an eligible tsinelas anywhere along the slide, host-side.
        ///
        /// ⚠️⚠️ ALONG THE SEGMENT AND EVERY FRAME, FOR `SweepLungeTag`'S REASON. A dash sampled
        /// only where it stops passes straight over anything in the middle of it at 60 Hz, and
        /// the whole point of this verb is to arrive AT the shoe rather than past it.
        ///
        /// ⚠️⚠️ AND IT CHECKS LINE OF SIGHT, WHICH THE TAG SWEEP DOES NOT NEED TO. A tag is
        /// resolved between two bodies that are both being pushed out of geometry by the physics
        /// engine, so a segment between them is a segment through open street. A tsinelas is not:
        /// `Slipper` comes to rest wherever it lands, including hard against the far side of a
        /// wall or a jeepney, and a radius around a segment does not know a wall is there. Without
        /// this an attacker could stand against a wall and slide a shoe through it.
        ///
        /// ⚠️ ONE PER SLIDE. `_slideActiveLeft` is cleared on the first success, so a slide that
        /// passes two loose tsinelas takes one, exactly as a walk-up would.
        /// </summary>
        private void SweepSlideRetrieval()
        {
            if (!NetAuthority.ShouldResolve()) return;
            if (_carrier == null || _carrier.Held != null) return;

            var round = GameServices.Round;
            if (round == null) return;

            Vector3 a = Flat(_slideFrom);
            Vector3 b = Flat(transform.position);

            Slipper best = null;
            float bestDistance = float.MaxValue;

            foreach (var s in FindObjectsByType<Slipper>(FindObjectsSortMode.None))
            {
                if (s == null) continue;

                // ⚠️ THE ELIGIBILITY GATE IS THE PICKUP'S OWN, ASKED UNCHANGED. The only thing
                // the slide relaxes is WHERE the attacker has to be standing, so the radius test
                // is re-done against the segment below and everything else in that method still
                // applies exactly as it does to a walk-up.
                if (s.State != SlipperState.Loose || !_motor.CanAct() || _motor.IsDefender) continue;

                Vector3 at = Flat(s.transform.position);
                float d = DistanceToSegment(at, a, b);
                if (d > Balance.PickupRadius || d >= bestDistance) continue;
                if (!ReachableThroughTheStreet(s.transform.position)) continue;

                bestDistance = d;
                best = s;
            }

            if (best == null) return;
            if (!best.HostGrab(_motor)) return;

            _slideActiveLeft = 0.0f;

            // ⚠️⚠️ THERE IS NO STYLE AWARD HERE AND THE FIRST VERSION HAD ONE, WHICH
            // `tools/audit_presentation_reach.py` CAUGHT ON THE RUN IT WAS WRITTEN. It reported
            // the only HOST-ONLY presentation call site in the whole game (98 sites, 97
            // reachable), and it was right twice over:
            //
            //  1. **The pickup already reports style.** `Slipper.HostGrab` calls
            //     `Carrier.NotifyHolding`, which fires `ReportStyle` on EVERY peer for every
            //     pickup however it happened. A second award here is the same retrieval paid
            //     twice, which is `docs/TODO.md` § 57.3's fault in the cosmetic bar.
            //  2. **A call inside a `ShouldResolve()` gate is one player's.** `Hud.ReportStyle`
            //     does relay by default, so it would in fact have reached the seat's owner, but
            //     an audit that has to know that about every call site is an audit nobody can
            //     read, and the correct call site is the one that already runs everywhere.
            //
            // ⚠️ THE CALLOUT STILL NAMES THE SLIDE, in `Carrier.NotifyHolding`, off
            // `CharacterMotor.IsCommitted`. One award, one funnel, and the word says which
            // retrieval it was.
        }

        /// <summary>
        /// Whether a straight line from this body to that point crosses the world.
        ///
        /// ⚠️ IT IS A RAYCAST AND IT IS THE ONE IN THIS FILE, which is worth saying out loud
        /// because `CLAUDE.md` § 4 says contact resolves by DISTANCE and never by a trigger. That
        /// rule is about who a verb REACHES; this is about whether a wall is in the way, which a
        /// distance cannot answer and a trigger volume never could either.
        ///
        /// ⚠️ IT IGNORES TRIGGERS AND THE BODIES. A player standing between an attacker and their
        /// tsinelas is not a wall, and a hazard zone is not either; the shoe under a jeepney is.
        /// </summary>
        private bool ReachableThroughTheStreet(Vector3 target)
        {
            Vector3 eye = transform.position + Vector3.up * 0.5f;
            Vector3 toward = (target + Vector3.up * 0.1f) - eye;

            float distance = toward.magnitude;
            if (distance < 0.05f) return true;

            return !Physics.Raycast(eye, toward / distance, out var hit, distance,
                                    ~0, QueryTriggerInteraction.Ignore)
                   || hit.collider == null
                   || hit.collider.GetComponentInParent<CharacterMotor>() != null
                   || hit.collider.GetComponentInParent<Slipper>() != null;
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
        public bool HostResolvePunch(Vector3 from, Vector3 facing)
        {
            if (!NetAuthority.ShouldResolve() || _punchCooldown > 0.0f ||
                !_motor.IsDefender || !_motor.CanAct()) return false;

            _punchCooldown = Balance.PunchCooldown;
            Animator?.PlayAction("punch");

            var victim = FindInCone(from, facing, Balance.PunchRange, Balance.PunchArcDeg,
                                    requireTaggable: true);

            if (victim != null) GameServices.Round?.ResolveTag(_motor, victim);
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

            _lungeCooldown = Balance.LungeCooldown;
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

        /// <summary>
        /// An attacker's retrieval slide, resolved from the sender's own frame.
        ///
        /// ⚠️⚠️ IT RE-CHECKS EVERYTHING AND TAKES THE STAMINA HERE, which is `HostResolveShove`'s
        /// shape and not an accident. A client that has predicted the dash has also predicted the
        /// cost; if the host refuses, `RefuseVerb` hands the local prediction back. A host that
        /// applied the impulse without spending would let a client slide for free by lying about
        /// its own bar.
        ///
        /// ⚠️ THE SWEEP FOLLOWS THE HOST'S OWN IMPULSE, so a slide cannot collect from a position
        /// the dash never actually reached. Same guarantee `HostResolveLunge` states for the tag.
        /// </summary>
        public bool HostResolveSlide(Vector3 from, Vector3 facing)
        {
            if (!NetAuthority.ShouldResolve() || _slideCooldown > 0.0f ||
                _motor.IsDefender || !_motor.CanAct() || _motor.Stamina.IsFatigued ||
                (_carrier != null && _carrier.Held != null) ||
                !_motor.Stamina.Spend(Balance.SlideStaminaCost)) return false;

            _slideCooldown = Balance.SlideCooldown;
            _slideActiveLeft = Balance.SlideActiveTime;
            _slideFrom = from;

            _motor.Commit(Balance.SlideActiveTime + Balance.SlideRecoveryTime);

            Vector3 flat = facing;
            flat.y = 0.0f;
            if (flat.sqrMagnitude < 0.0001f) flat = transform.forward;

            _motor.ApplyImpulse(flat.normalized * Balance.SlideSpeed);
            Animator?.PlayAction("lunge");
            return true;
        }

        /// <summary>An attacker shoving a rival, resolved from the sender's own frame.</summary>
        public bool HostResolveShove(Vector3 from, Vector3 facing)
        {
            if (!NetAuthority.ShouldResolve() || _shoveCooldown > 0.0f ||
                _motor.IsDefender || !_motor.CanAct() || _motor.Stamina.IsFatigued ||
                !_motor.Stamina.Spend(Balance.ShoveStaminaCost)) return false;

            Animator?.PlayAction("shove");

            var victim = FindInCone(from, facing, Balance.ShoveRange, Balance.ShoveArcDeg,
                                    requireTaggable: false);

            // The other half of the pair; see the note on the local path above.
            GameServices.Stats?.NoteShoveAttempt(_motor.PlayerSlot, victim != null);

            if (victim == null)
            {
                _shoveCooldown = Balance.ShoveMissCooldown;
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
        public void RollBackRefusedVerb(Net.MatchRpc.DeniedVerb verb)
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
                    _motor.Stamina.Refund(Balance.ShoveStaminaCost);
                    break;

                case Net.MatchRpc.DeniedVerb.Slide:
                    _slideCooldown = 0.0f;
                    _slideActiveLeft = 0.0f;
                    _motor.Stamina.Refund(Balance.SlideStaminaCost);

                    // ⚠️⚠️ THE COMMITMENT IS RETURNED TOO, AND IT IS THE ONE A PLAYER WOULD
                    // ACTUALLY NOTICE. The other two refusals hand back a cooldown and a bar;
                    // this one has also narrowed the player's steering to 0.35 for most of a
                    // second, so a refusal that left it running would leave somebody wading
                    // through a commitment they were told they never made.
                    _motor.ReleaseCommitment();
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
