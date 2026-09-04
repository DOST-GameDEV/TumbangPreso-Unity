using System;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// The can. It stands on its mark, it goes over, and the taya stands it back up.
    ///
    /// ⚠️⚠️ IsUpright GATES FOUR SEPARATE RULES: the throw, the tag, passive scoring and the
    /// reset channel. It is host-authoritative, and in the Godot build it is replicated by an
    /// EXPLICIT RPC rather than a synchronised property. That was not a style choice: a
    /// `MultiplayerSynchronizer` writes the property directly, so the setter's signal never
    /// fires on the peer that RECEIVED it. One setter, three symptoms, and it cost a whole
    /// session.
    ///
    /// ⚠️ THE SAME TRAP EXISTS IN UNITY. A `NetworkVariable` hook fires on change, but a
    /// value written server-side and read client-side in the same frame can be observed
    /// before the hook runs. When Phase 5 arrives, replicate this with an explicit Rpc and
    /// raise <see cref="UprightChanged"/> from one place, exactly as the original does.
    /// </summary>
    public sealed class Lata : MonoBehaviour
    {
        public event Action<bool> UprightChanged;

        [SerializeField] private int _skinIndex = -1;

        private bool _isUpright = true;
        private float _toppleTimer;
        private Vector3 _mark;
        private float _restoreProtectionLeft;
        private GameObject _downBeacon;
        private GameObject _protectionShell;

        public int SkinIndex { get => _skinIndex; set => _skinIndex = value; }
        public bool IsUpright => _isUpright;
        public bool IsProtected => _restoreProtectionLeft > 0.0f;
        public float ProtectionLeft => Mathf.Max(0.0f, _restoreProtectionLeft);

        /// <summary>The scoring window for the CURRENT can skin, divided by its STANCE.</summary>
        public float HitWindow => ThrowRules.HitWindow(_skinIndex);

        /// <summary>How long the taya's reset channel takes on this can.</summary>
        public float ResetChannelTime => Combat.ResetChannelFor(_skinIndex);

        private void Awake() => _mark = transform.position;

        /// <summary>
        /// ⚠️⚠️ THE MARK IS SNAPPED TO THE GROUND BY RAYCAST, AND WITHOUT THIS THE CAN SINKS.
        /// Reported against this build as *"can clips thru the floor"*. The lata's mark comes
        /// from a marker the map author placed, and a marker's Y is whatever it was dragged to;
        /// `lata.gd::_snap_home_to_ground` exists because that was already wrong in the Godot
        /// build. Ported rather than fixed by adding a constant, and for the reason that file
        /// records: the two maps can disagree, and the mark is the one spot in the arena whose
        /// height a map is most likely to change, because Bayan Plaza has a step there.
        ///
        /// ⚠️ IT IS THE MARK THAT MOVES, NOT ONLY THE TRANSFORM. `_mark` is what `HostRestore`
        /// puts the can back on, so snapping only the live position would seat the can correctly
        /// on the first round and drop it through the floor on the first reset.
        ///
        /// ⚠️ AND THE CAN'S OWN COLLIDERS ARE EXCLUDED. A ray dropped from two metres up hits
        /// the top of the can before it reaches the road, so the "ground" comes back as the
        /// can's own lid and the snap lifts it by its full height. The Godot note records that
        /// measured as +0.385, which is the can's height exactly.
        ///
        /// ⚠️ IN Start, NOT Awake. The arena's own colliders have to be in the scene before the
        /// ray is cast, and in Awake they are not.
        /// </summary>
        private void Start() => SnapHomeToGround();

        private void SnapHomeToGround()
        {
            var hits = Physics.RaycastAll(_mark + Vector3.up * 2.0f, Vector3.down, 8.0f,
                                          ~0, QueryTriggerInteraction.Ignore);

            if (hits == null || hits.Length == 0) return;

            bool found = false;
            float bestY = 0.0f;

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                if (hit.collider.transform == transform ||
                    hit.collider.transform.IsChildOf(transform)) continue;

                if (found && hit.point.y <= bestY) continue;

                bestY = hit.point.y;
                found = true;
            }

            if (!found) return;

            _mark = new Vector3(_mark.x, bestY, _mark.z);
            transform.position = _mark;
        }

        /// <summary>
        /// Host-side. Did a slipper at this position connect?
        ///
        /// ⚠️ FLAT DISTANCE, TESTED PER PHYSICS FRAME, AND NOT AN OVERLAP VOLUME. The Godot
        /// `Lata.tscn` still carries an `Area3D` authored to a hurtbox shape that nothing
        /// ever read: the rule ran off a bare literal in another file while the balance doc
        /// documented a third shape. Three numbers that were meant to be one. Here there is
        /// exactly one, and it is <see cref="Balance.LataHitMargin"/>.
        /// </summary>
        public bool Connects(Vector3 slipperPosition)
        {
            Vector3 a = new Vector3(slipperPosition.x, 0.0f, slipperPosition.z);
            Vector3 b = new Vector3(transform.position.x, 0.0f, transform.position.z);
            return ThrowRules.Connects(Vector3.Distance(a, b), _skinIndex);
        }

        /// <summary>
        /// Applies a host snapshot. Rejoin is observation of an existing state, not a new can
        /// event, so nothing here replays a round the arriving peer did not watch.
        ///
        /// ⚠️ IT DOES ANNOUNCE A CHANGE NOW, AND ONLY A CHANGE. This used to play no feedback at
        /// all, which was right for a rejoin and wrong for the 5 Hz stream a client spends the
        /// whole match receiving: the can going over is an EDGE in this data, and refusing to
        /// read it is why three players out of four saw the objective fall in silence. See
        /// `AnnounceUprightChange`, which is gated on the edge and on not being the host.
        /// </summary>
        public void ApplySnapshotState(Vector3 position, Quaternion rotation,
                                       bool isUpright, int skinIndex)
        {
            bool restoredOnThisPeer = !_isUpright && isUpright;
            bool knockedOnThisPeer = _isUpright && !isUpright;

            transform.SetPositionAndRotation(position, rotation);
            _skinIndex = skinIndex;
            _isUpright = isUpright;
            if (restoredOnThisPeer) _restoreProtectionLeft = Balance.ThrowRestoreCooldown;
            else if (!isUpright) _restoreProtectionLeft = 0.0f;
            _toppleTimer = 0.0f;
            _toppleAngle = isUpright ? 0.0f : rotation.eulerAngles.x;
            _rollAngleDeg = isUpright ? 0.0f : rotation.eulerAngles.y;
            _lastRollPosition = position;
            RefreshStatePresentation();

            // ⚠️⚠️ THE ANNOUNCER SPEAKS ON A CLIENT TOO, AND IT NEVER DID. 🧑 2026-08-29:
            // *"non hosts dont have sfx in some plarts, example, lata down/ lata hit has no sound
            // for non host but has sound for host"*. `SetUpright` is the one place both lines are
            // spoken and it is host-only by construction, so `tumbang!` and `lata restored` were
            // heard on one machine out of four — the loudest moment in a round, announced to
            // whoever happened to press HOST.
            //
            // ⚠️ THE VOICE IS NOT PUT THROUGH `NetCue` AND MUST NOT BE. That class's header is
            // explicit that it is for WORLD events; the announcer is a per-listener commentary
            // track, so the right shape is each peer speaking its own line off the state it has
            // just been told about, which is exactly this line.
            //
            // ⚠️ ON THE EDGE, AND ON THE EDGE ONLY, which is what keeps this method's promise
            // that a rejoin is *"observation of an existing state, not a new can event"*: a
            // snapshot that does not change the flag says nothing. A rejoin that lands exactly on
            // the frame the can goes over will speak once, which is the correct answer anyway.
            //
            // ⚠️ AND ONLY OFF THE HOST, so the listen host does not say it twice — `SetUpright`
            // has already spoken there, and `HostSyncPeer` feeds the host its own snapshot back.
            if (!NetAuthority.ShouldResolve() && (knockedOnThisPeer || restoredOnThisPeer))
                AnnounceUprightChange(isUpright);

            UprightChanged?.Invoke(isUpright);
        }

        /// <summary>
        /// Moves a replica can along the host's path WITHOUT touching whether it is standing.
        ///
        /// ⚠️⚠️ THE ROLL IS A STREAM AND GOING OVER IS AN EVENT, AND THEY TRAVEL SEPARATELY NOW.
        /// A struck can rolls for a second or so, which is a position that fully replaces itself
        /// every step and can afford to lose a packet; `_isUpright` is what scores and may never
        /// lose one. `Slipper.ApplySnapshotPose` carries the full reasoning, including why the
        /// unreliable half must carry no state at all rather than state the receiver ignores.
        ///
        /// ⚠️ IT DELIBERATELY DOES NOT TOUCH `_toppleAngle`, `_rollAngleDeg` OR THE PROTECTION
        /// WINDOW. Those are consequences of the state change that `ApplySnapshotState` sets when
        /// the event arrives, and re-deriving them from a rotation fifty times a second would let
        /// a pose packet restart a restore-protection window the host has already spent.
        /// `_lastRollPosition` moves with the transform because it is the roll's own trail.
        /// </summary>
        public void ApplySnapshotPose(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            _lastRollPosition = position;
        }

        /// <summary>Host-side. Knock it over and pay the thrower.</summary>
        public void HostKnockDown(int throwerSlot)
        {
            if (!NetAuthority.ShouldResolve()) return;
            if (!_isUpright) return;

            // A reset must create a real safe beat, not only refuse newly launched throws.
            // A slipper that was already airborne when the channel completed can otherwise
            // knock the lata down on the very next physics frame. It still bounces off the can,
            // but no state change or score is created during the visible shield window.
            if (IsProtected)
            {
                PulseProtection();
                // ⚠️⚠️ `NetCue`: THIS IS INSIDE `HostKnockDown` AND WAS HOST-ONLY. The sound of
                // the OBJECTIVE going over is the single most important event in a round, and
                // three of the four players could not hear it. See `Carrier.HostThrowAt` for the
                // audit that found both and why the authority gate itself is not the problem.
                NetCue.PlayVaried("lata_seal", transform.position,
                                                 1.05f, 1.12f, 0.55f);
                return;
            }

            Hitstop.Trigger(0.045f, 0.10f);
            SetUpright(false);
            _toppleTimer = Balance.ToppleTime;

            // ⚠️ THE KNOCKDOWN CUE IS NOT PLAYED HERE, AND ADDING ONE DOUBLED IT. `SetUpright`
            // below picks `can_knockdown` or `reset_channel_start` off the same boolean, for the
            // reason its own note gives: one state change read two ways, rather than two call
            // sites free to drift apart. A second `PlayAt` here fires the same sample twice on
            // the same frame, which is a flam rather than a hit.

            // ⚠️⚠️ THE TAYA IS NOT PAID FOR KNOCKING THEIR OWN CAN OVER, AND THIS PORT WAS
            // PAYING THEM 100 FOR IT. `round_manager.gd::host_note_lata_knocked` is
            //
            //     if by_slot >= 0 and by_slot != MatchManager.defender_slot:
            //
            // and only the `>= 0` half made it across. The defender's own slipper, their body
            // clipping the can, or anything else that credits their slot would have scored the
            // attackers' event for them — and since the can spends most of a round on its side,
            // the taya standing it up and knocking it down is a loop worth 100 a go.
            //
            // ⚠️ AND THE ROUND HAS TO BE LIVE. The .gd returns before scoring when it is not.
            // The topple above still happens either way: that is physics, and a can knocked
            // over between rounds is still knocked over.
            if (throwerSlot < 0) return;
            if (GameServices.Round == null || !GameServices.Round.RoundActive) return;
            if (GameServices.Match != null && throwerSlot == GameServices.Match.DefenderSlot) return;

            GameServices.Match.AddScore(throwerSlot, ScoreEvent.LataKnocked);
            // ⚠️⚠️ RELAYED, BECAUSE THE ENCLOSING VERB IS HOST-RESOLVED AND WHAT IT DRAWS IS
            // FOR EVERYBODY. 🧑 2026-08-29: *"make sure that all host sided shit is seen by
            // everyone and not js host"*. See `Visual.MatchFlair` and
            // `tools/audit_presentation_reach.py`, which is what found this one.
            Visual.MatchFlair.Announce(Visual.MatchFlair.Kind.LataDown,
                                       throwerSlot, -1, transform.position);

            // ⚠️⚠️ THE CAN USED TO REACH INTO `AIController` AND START AN EMOTE FROM HERE, AND
            // THAT CALL IS GONE ON PURPOSE. It was a second path into the celebration: it skipped
            // the safety gate entirely, so a bot could be told to stand still and dance while it
            // was inside the chalk with a taya on it. `AIController` § THE FACE listens to
            // `MatchDirector.Scored` instead, which is the event the line above has just raised,
            // so the same celebration still happens off the same knockdown and now goes through
            // the one gate that knows whether standing still is affordable.
            //
            // ⚠️ AND THE CAN HAS NO BUSINESS KNOWING WHAT A BOT IS. Everything else on this path
            // is scoring and physics; a `GetComponent<AIController>` here is the kind of reach
            // that makes a rules object depend on the AI layer.
            var throwerMotor = GameServices.Round.PlayerAt(throwerSlot);
            throwerMotor?.AbilitySystem?.OnLataKnocked();
        }

        /// <summary>
        /// Host-side. The end of a completed reset channel.
        ///
        /// ⚠️ IT GOES BACK ON ITS MARK AND *THEN* STANDS UP, IN THAT ORDER. A lata that
        /// stands up where it was knocked to is a lata the next throw cannot miss, and the
        /// taya would have spent the channel making the attackers' next shot easier.
        /// </summary>
        public void HostRestore()
        {
            // ⚠️⚠️ THIS GATE WAS MISSING AND THREE COMMENTS IN TWO FILES SAID IT WAS HERE.
            // `SetUpright`'s note and `AnnounceUprightChange`'s summary both claimed this method
            // and `HostKnockDown` "both open with `NetAuthority.ShouldResolve()`", and
            // `NetCue`'s header repeated it. Only `HostKnockDown` did. `tools/audit_cue_relay.py`
            // found it by propagating gatedness from the callers and reporting the two cue lines
            // in `SetUpright` as UNGATED; `docs/TODO.md` § 135.6 recorded that it could not be
            // proven either way from reading.
            //
            // ⚠️ IT IS A NO-OP TODAY AND IS STILL WORTH WRITING, which is the whole argument for
            // it. Every path a client can currently take is closed somewhere else: `Carrier`
            // returns at `ShouldRequest()` before reaching the restore (its own note explains
            // that a client righting the can locally is a bug they already fixed once),
            // `MatchRpc.HostApplyResetPhase` checks `IsHost`, `GuidedTraining` is offline where
            // `ShouldResolve()` is true, and `MatchBootstrap.ResetWorld` hangs off
            // `MatchDirector.RoundStarted`, which `ApplySnapshot` DELIBERATELY DOES NOT RAISE on
            // a client (see its own ⚠️⚠️ note: raising it would give every client a second
            // authority over the round number). So the protection is against the NEXT caller,
            // and the cost of not having it is that the one method in this file that stands the
            // objective back up trusts four other files to remember.
            //
            // ⚠️ AND IT CHANGES NOTHING OFFLINE. `ShouldResolve()` is `IsHost`, and the solo
            // provider answers true, which is why every host-side path runs unchanged in single
            // player and in the tutorial.
            if (!NetAuthority.ShouldResolve()) return;

            transform.position = _mark;
            transform.rotation = Quaternion.identity;

            // ⚠️ THE TILT STATE IS CLEARED, NOT JUST THE TRANSFORM. `_toppleAngle` and the roll
            // survive a restore otherwise, and the next knockdown starts its lift from wherever
            // the last one ended — which lands the can in the air on its second topple.
            _toppleAngle = 0.0f;
            _rollAngleDeg = 0.0f;
            _toppleTimer = 0.0f;
            _lastRollPosition = _mark;

            _restoreProtectionLeft = Balance.ThrowRestoreCooldown;
            SetUpright(true);

            // ⚠️ NO CUE HERE EITHER. `SetUpright(true)` above already sounds the restore; see
            // the note in HostKnockDown.
            GameServices.Round.NotifyLataRestored();
        }

        private void SetUpright(bool value)
        {
            if (_isUpright == value) return;
            _isUpright = value;

            // ⚠️ ONE PLACE, BOTH DIRECTIONS. The can going over and the can going back up are
            // the same state change read two ways, and the .gd picks the cue off the same
            // boolean rather than from two call sites that can drift apart.
            //
            // ⚠️⚠️ THE UPRIGHT BRANCH IS `reset_complete`, AND IT WAS PLAYING `reset_channel_start`.
            // `lata.gd:276` is
            //
            //     AudioManager.play_at("can_knockdown" if not now_upright else "reset_complete", ...)
            //
            // and this is the moment the channel FINISHES, not the moment it begins: the taya has
            // already held E for the full duration and the can is standing again. The port
            // announced the end of the channel with its opening sound, so the payoff for the
            // taya's longest commitment in the game was the cue that means "starting". It is also
            // why `reset_complete` and `reset_channel_complete` both shipped as live cues with
            // zero call sites anywhere in the port.
            //
            // ⚠️⚠️ THROUGH `NetCue`, BECAUSE THIS METHOD ONLY EVER RUNS ON THE HOST AND THE SOUND
            // IS FOR EVERYBODY. 🧑 2026-08-29: *"lata down/ lata hit has no sound for non host but
            // has sound for host"*. `SetUpright` is reached from `HostKnockDown` and `HostRestore`
            // and nowhere else, and both open with `NetAuthority.ShouldResolve()`; a client is
            // told the can moved by `ApplySnapshotState`, which bypasses this method on purpose so
            // that a rejoin does not replay the round it walked into. So the two loudest events in
            // a round were audible on one machine out of four.
            //
            // ⚠️ THE GATE ABOVE IT IS STILL RIGHT AND IS NOT WHAT MOVED. `NetCue`'s header states
            // the rule: the host is the only peer that may DECIDE the can went over, and deciding
            // and announcing were the same line. This separates them and changes nothing offline,
            // where `NetCue` is exactly `GameServices.Audio` with no transport running.
            if (value)
                NetCue.PlayImpact("reset_complete", "lata_seal", transform.position, 0.72f);
            else
                NetCue.PlayImpact("can_knockdown", "lata_impact", transform.position, 1.0f);

            AnnounceUprightChange(value);

            RefreshStatePresentation();

            UprightChanged?.Invoke(value);
        }

        /// <summary>
        /// Everything a player SEES and HEARS when the can changes state, apart from the impact
        /// itself.
        ///
        /// ⚠️⚠️ IT IS A SEPARATE METHOD SO A CLIENT CAN RUN IT, AND EVERY LINE OF IT USED TO BE
        /// HOST-ONLY. 🧑 2026-08-29: *"lata down/ lata hit has no sound for non host but has sound
        /// for host"*, and *"some clients dont see the correct ability effects but host do"*.
        /// `SetUpright` is reached only from `HostKnockDown` and `HostRestore`, both gated on
        /// `NetAuthority.ShouldResolve()`; a client learns the can moved through
        /// `ApplySnapshotState`, which bypasses `SetUpright` on purpose. So the announcer, the
        /// `TUMBA!` / `LATA DOWN!` popup, the hitmarker, the burst, the confetti and the camera
        /// punch — **the entire presentation of the loudest event in the game** — happened on one
        /// machine out of four. Three players watched the can silently lie down.
        ///
        /// ⚠️ THE IMPACT CUE IS NOT IN HERE, and that is not an oversight: it goes out through
        /// `NetCue` from `SetUpright`, which plays it locally and relays it, so putting it here as
        /// well would play it twice on a client. Sound that travels and presentation that is
        /// derived are two different mechanisms and only one of them belongs on the wire.
        ///
        /// ⚠️ THE CAMERA PUNCH IS PER-MACHINE BY NATURE. It reads `Camera.main`, so each peer
        /// punches its own rig from its own position, which is the correct behaviour and is only
        /// available because this runs locally rather than being relayed.
        /// </summary>
        private void AnnounceUprightChange(bool nowUpright)
        {
            if (nowUpright)
            {
                GameServices.Voice?.OnLataRestored();
                Visual.ComicPopup.Spawn(transform.position + Vector3.up * 0.8f, "RESTORED!", UI.UiTheme.Defense, 1.2f);
                return;
            }

            GameServices.Voice?.OnLataKnocked();
            string callout = UI.SceneFlow.SelectedMode == GameMode.Classic
                ? "TUMBA!" : "LATA DOWN!";
            Visual.ComicPopup.Spawn(transform.position + Vector3.up * 0.8f, callout, UI.UiTheme.Offense, 1.4f);
            UI.Hud.TriggerHitmarker(UI.UiTheme.Offense, "💥");
            Visual.ImpactBurst.SpawnAt(transform.position);
            Abilities.HeroHazards.SpawnConfettiShower(transform.position, 24);

            if (UnityEngine.Camera.main == null) return;

            var rig = UnityEngine.Camera.main.GetComponent<CameraSystem.CameraRig>();
            if (rig == null) return;

            Vector3 away = UnityEngine.Camera.main.transform.position - transform.position;
            rig.ImpactPunch(away.sqrMagnitude > 0.01f ? away.normalized : Vector3.back, 0.8f);
        }

        /// <summary>
        /// ⚠️ A TOPPLED CAN IS LIFTED BY ITS OWN RADIUS, AND THAT IS LOAD-BEARING. The tilt
        /// rotates the visual about its BASE, so a lying-down cylinder's axis would sit at
        /// floor level and half the can would be underground. That was reported from play as
        /// "the cans are phasing thru the floor". Measure the lift off the mesh bounds so it
        /// follows the skin rather than assuming a radius, because the four cans span 0.108
        /// to 0.143 and the default is the slimmest of them.
        /// </summary>
        private void Update()
        {
            StepStatePresentation();

            if (_isUpright || _toppleTimer <= 0.0f) return;

            _toppleTimer = Mathf.Max(0.0f, _toppleTimer - Time.deltaTime);
            float t = 1.0f - (_toppleTimer / Balance.ToppleTime);

            _toppleAngle = Mathf.Lerp(0.0f, Balance.DownedTiltDeg, t);
            ApplyTilt(_toppleAngle);
        }

        /// <summary>
        /// The lata is the objective, so its state must still read when the HUD is hidden or the
        /// player is watching the fight instead of the bottom-right card. A narrow vertical
        /// beacon marks DOWN without spending more floor area, while the short restoration
        /// protection gets a compact shell whose lifetime is the actual gameplay timer.
        /// </summary>
        private void RefreshStatePresentation()
        {
            if (_isUpright)
            {
                ClearDownBeacon();
                if (IsProtected) BuildProtectionShell();
                else ClearProtectionShell();
                return;
            }

            _restoreProtectionLeft = 0.0f;
            ClearProtectionShell();
            BuildDownBeacon();
        }

        /// <summary>
        /// § THE DOWNED READ. What marks a toppled can, now that the beacon is gone.
        ///
        /// ⚠️⚠️ THERE WAS A RED BEACON HERE AND IT WAS DELETED ON REPORT. 🧑 2026-08-26, with the
        /// frame: *"that red line, thats red beacon when lata is down, that looks bad ... the
        /// purpose of it is to put emphasis on lata being down but its shit"*, and the shape of
        /// the replacement in the same message: *"without putting a fkn beacon on it or covering
        /// the lata completley with some effect"*.
        ///
        /// ⚠️ WHAT IT WAS MADE OF IS WHY IT LOOKED LIKE THAT. A 4 m translucent `Cylinder`, a
        /// second translucent `Cylinder` lying flat under it, and a point light over the pair:
        /// the exact stack `docs/VISION.md` § 2 rule 3 names as the thing every effect in this
        /// game used to be, and that § 19 spent a pass removing from the ability kits. Nothing
        /// had come back to the objective itself. A 0.18 m wide vertical tube seen from standing
        /// eye height across a 14 m arena is foreshortened into a RED LINE lying on the road,
        /// which is what he photographed: it did not read as a column of light from any angle a
        /// player actually has.
        ///
        /// ⚠️⚠️ AND THE GAME ALREADY SAID "LATA DOWN" SIX OTHER TIMES. The world popup at the can
        /// (`OnKnocked` below), the centre alert, the bottom-right card title, the objective
        /// line under it, the score toast and the crosshair all fire off the same state. The
        /// problem was never that the message was too quiet to hear; it was that the seventh
        /// copy was a light in the middle of the arena. Emphasis is not repetition.
        ///
        /// ⚠️ SO THE CAN IS THE SIGNAL, NOT SOMETHING PARKED NEXT TO IT. Two parts, deliberately
        /// built two different ways, per § 19's rule that construction is the channel:
        ///
        ///   * a RIM PULSE on the can's own renderers, which costs no floor area at all and
        ///     cannot cover the object because it IS the object's silhouette. It reuses the
        ///     `_RimStrength` / `_RimColor` path `Slipper` already drives for the landed
        ///     highlight, so it is a property block on the existing mesh rather than new
        ///     geometry;
        ///   * a COLLAR at the foot, which is an annulus with an open middle, so the can stays
        ///     visible through it. It is what finds the can when a body is standing in front of
        ///     it. `VfxShapes.Collar`, the § 19 builder, not a flat cylinder.
        ///
        /// ⚠️ 0.95 m OF RADIUS IS 2.8 m², WHICH IS 1.4 PER CENT OF THE 196 m² BOX. The old flare
        /// was 1.35 and solid. `docs/VISION.md` § 2 rule 1 puts a SKILL at 1.8 to 2.5 m; the
        /// objective marker has no business being larger than a skill, and an annulus spends a
        /// fraction of even this on actual pixels.
        /// </summary>
        private void BuildDownBeacon()
        {
            if (_downBeacon != null) return;

            _downBeacon = new GameObject("LataDownMark");
            _downBeacon.transform.SetParent(transform, false);

            // ⚠️ `Lay`, NOT `Stand`. A collar is a flat annulus and `Lay` scales X and Z while
            // leaving Y at 1.0, which is exactly right for a ring on the floor and exactly wrong
            // for anything with height. The old shaft is the reason that distinction is worth
            // restating here.
            var collar = Visual.VfxShapes.Lay(_downBeacon.transform, "DownCollar",
                                              Visual.VfxShapes.Collar(24, 0.10f, 0.88f),
                                              DownCollarRadius, 0.02f);

            Visual.VfxMaterial.Ghost(collar.GetComponent<Renderer>(),
                new Color(UI.UiTheme.Danger.r, UI.UiTheme.Danger.g, UI.UiTheme.Danger.b, 0.55f),
                1.6f);
            Visual.VfxMaterial.StripCollider(collar);

            _downCollar = collar.transform;
        }

        /// <summary>The foot marker's radius. See the note on <see cref="BuildDownBeacon"/>.</summary>
        private const float DownCollarRadius = 0.95f;

        private Transform _downCollar;

        /// <summary>
        /// The pulse, driven every frame the can is down.
        ///
        /// ⚠️ ONE SINE, TWO CONSUMERS. The collar breathes on it and the rim brightens on it, so
        /// the two halves are visibly the same heartbeat rather than two effects that happen to
        /// both be red. Deriving them from one number is what makes them read as one object
        /// rather than as a marker plus a glow.
        /// </summary>
        private void DriveDownPulse()
        {
            if (_downBeacon == null) return;

            float beat = Mathf.Sin(Time.unscaledTime * 5.0f) * 0.5f + 0.5f;

            if (_downCollar != null)
            {
                float r = DownCollarRadius * (1.0f + beat * 0.07f);
                _downCollar.localScale = new Vector3(r, 1.0f, r);
            }

            SetRim(Mathf.Lerp(0.35f, 1.0f, beat));
        }

        /// <summary>
        /// ⚠️ THROUGH A PROPERTY BLOCK, WHICH IS WHAT KEEPS IT PER-CAN. The lata's material is a
        /// shared skin asset; writing the rim into the material would light every can in the
        /// project including the one posing on a menu. `Slipper` drives the landed highlight the
        /// same way and for the same reason.
        /// </summary>
        private void SetRim(float strength)
        {
            if (_renderers == null) CacheRenderers();
            if (_renderers.Length == 0) return;

            _rimBlock ??= new MaterialPropertyBlock();

            foreach (var r in _renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_rimBlock);
                _rimBlock.SetFloat(RimStrengthId, strength);
                _rimBlock.SetColor(RimColorId, UI.UiTheme.Danger);
                r.SetPropertyBlock(_rimBlock);
            }
        }

        private void CacheRenderers()
            => _renderers = GetComponentsInChildren<Renderer>(true);

        private Renderer[] _renderers;
        private MaterialPropertyBlock _rimBlock;

        private static readonly int RimStrengthId = Shader.PropertyToID("_RimStrength");
        private static readonly int RimColorId = Shader.PropertyToID("_RimColor");

        private void ClearDownBeacon()
        {
            if (_downBeacon != null) Destroy(_downBeacon);
            _downBeacon = null;
            _downCollar = null;

            // ⚠️ THE RIM IS PUT BACK, AND FORGETTING THIS LEAVES THE CAN GLOWING RED ALL ROUND.
            // The property block persists on the renderer; nothing resets it when the object
            // that set it goes away.
            SetRim(0.0f);
        }

        private void BuildProtectionShell()
        {
            if (_protectionShell != null) return;

            _protectionShell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _protectionShell.name = "LataRestoreShield";
            _protectionShell.transform.SetParent(transform, false);
            _protectionShell.transform.localPosition = new Vector3(0.0f, 0.22f, 0.0f);
            _protectionShell.transform.localScale = Vector3.one * 0.72f;
            Visual.VfxMaterial.Ghost(_protectionShell.GetComponent<Renderer>(),
                new Color(UI.UiTheme.Defense.r, UI.UiTheme.Defense.g, UI.UiTheme.Defense.b, 0.28f),
                2.0f);
            Visual.VfxMaterial.StripCollider(_protectionShell);
        }

        private void ClearProtectionShell()
        {
            if (_protectionShell != null) Destroy(_protectionShell);
            _protectionShell = null;
        }

        private void PulseProtection()
        {
            BuildProtectionShell();
            if (_protectionShell != null) _protectionShell.transform.localScale = Vector3.one * 0.92f;
            // ⚠️ NO WORD. `PulseProtection` fires on every refresh of the throw-restore window,
            // and the lata card carries `PROTECTED 1.2s` as a live countdown for the whole of it.
            // The shell is the signal; the countdown is the number. A callout on top of both was
            // the third copy of one fact, on a pulse.

        }

        private void StepStatePresentation()
        {
            if (_restoreProtectionLeft > 0.0f)
            {
                _restoreProtectionLeft = Mathf.Max(0.0f, _restoreProtectionLeft - Time.deltaTime);
                BuildProtectionShell();

                if (_protectionShell != null)
                {
                    float pulse = 0.72f + (Mathf.Sin(Time.time * 18.0f) * 0.5f + 0.5f) * 0.10f;
                    _protectionShell.transform.localScale = Vector3.one * pulse;
                    _protectionShell.transform.Rotate(0.0f, 120.0f * Time.deltaTime, 0.0f,
                                                       Space.Self);
                }

                if (_restoreProtectionLeft <= 0.0f) ClearProtectionShell();
            }

            // ⚠️ THE WHOLE MARKER NO LONGER SCALES, ONLY THE COLLAR DOES. Scaling the parent
            // scaled the shaft's HEIGHT along with everything else, which is part of why the old
            // beacon swept about so much. `DriveDownPulse` breathes the ring and the rim off one
            // sine and leaves the marker's own transform alone.
            DriveDownPulse();
        }

        /// <summary>
        /// The tilt and the lift that exactly matches it, written together.
        ///
        /// ⚠️⚠️ THE LIFT IS `radius · |sin(angle)|`, NOT A STRAIGHT LINE, AND THE PORT SHIPPED
        /// THE STRAIGHT LINE. At angle t the can's lowest point is -radius·sin(t), so that is
        /// the lift that keeps it exactly on the road; a linear ramp agrees with it only at the
        /// two ends. `lata.gd::_set_tilt` carries the same note and the measurement that forced
        /// it — at the halfway point the rotation needed 0.076 of lift and the linear one
        /// supplied 0.054, dipping the can 22 mm through the floor mid-animation.
        ///
        /// ⚠️⚠️ AND THE RADIUS IS MEASURED ONCE, FROM THE UPRIGHT CAN. It was being re-measured
        /// every frame off `Renderer.bounds`, which is a WORLD-space AABB of a mesh that is in
        /// the middle of rotating: as the can lies down, its X/Z extents stop being its radius
        /// and become half its LENGTH. Measured on the shipped skins, that walks the lift from
        /// 0.14 up to about 0.19, so a settled can ends the topple hanging five centimetres off
        /// the road for the rest of the round. Reported as *"the can randomly floats"*.
        ///
        /// ⚠️ AND X AND Z ARE LEFT ALONE. Rewriting them from the mark every frame drags a can
        /// that has been shoved back to the middle of its own topple.
        /// </summary>
        private void ApplyTilt(float angleDeg)
        {
            transform.rotation = Quaternion.Euler(angleDeg, _rollAngleDeg, 0.0f);

            float lift = DownedLift * Mathf.Abs(Mathf.Sin(angleDeg * Mathf.Deg2Rad));

            Vector3 at = transform.position;
            transform.position = new Vector3(at.x, _mark.y + lift, at.z);
        }

        /// <summary>
        /// Half the can's width, measured off the UPRIGHT mesh the first time it is asked for.
        ///
        /// ⚠️ THE SKINS SPAN 0.108 TO 0.143 AND THE DEFAULT IS THE SLIMMEST OF THEM, so this is
        /// measured rather than assumed: a constant would sink the fattest can by 3.5 cm.
        /// </summary>
        private float DownedLift
        {
            get
            {
                if (_downedLift > 0.0f) return _downedLift;

                _downedLift = 0.14f;

                var mesh = GetComponentInChildren<Renderer>();

                // ⚠️ `localBounds` ON A MESH FILTER, NOT `Renderer.bounds`. The renderer's are
                // in world space and already carry whatever rotation the can is wearing at the
                // moment of the call, which is the fault this property exists to end.
                var filter = GetComponentInChildren<MeshFilter>();

                if (filter != null && filter.sharedMesh != null)
                {
                    Vector3 e = filter.sharedMesh.bounds.extents;
                    Vector3 s = filter.transform.lossyScale;
                    _downedLift = Mathf.Max(e.x * Mathf.Abs(s.x), e.z * Mathf.Abs(s.z));
                }
                else if (mesh != null)
                {
                    _downedLift = Mathf.Max(mesh.bounds.extents.x, mesh.bounds.extents.z);
                }

                return _downedLift;
            }
        }

        private float _downedLift;

        private float _toppleAngle;
        private float _rollAngleDeg;
        private Vector3 _lastRollPosition;

        /// <summary>
        /// A knocked can ROLLS when it is pushed, rather than sliding.
        ///
        /// ⚠️ THE ROLL IS DERIVED FROM DISTANCE TRAVELLED, NOT FROM A TIMER. A can rolling on
        /// a clock keeps spinning while it sits still, and one driven by speed alone spins the
        /// same way whichever direction it is shoved. Angle = distance / radius, signed by
        /// which way it is actually going, so reversing direction reverses the roll.
        ///
        /// ⚠️ AND IT COMES TO REST. Below the settle speed it stops accumulating, because a can
        /// lying still must lie still rather than creep.
        /// </summary>
        private void LateUpdate()
        {
            if (_isUpright) { _rollAngleDeg = 0.0f; _lastRollPosition = transform.position; return; }

            Vector3 here = transform.position;
            Vector3 moved = new Vector3(here.x - _lastRollPosition.x, 0.0f,
                                        here.z - _lastRollPosition.z);
            _lastRollPosition = here;

            float speed = moved.magnitude / Mathf.Max(0.0001f, Time.deltaTime);
            if (speed < Balance.DownedRollSettle) return;

            // ⚠️ THE SAME MEASURED RADIUS THE LIFT USES. It was read off the rotating world
            // bounds here too, so a can lying down rolled against a radius half again too big
            // and turned more slowly the flatter it got.
            float radius = Mathf.Max(0.05f, DownedLift);

            // Along the can's own lying axis, so it rolls the way it is pointing.
            Vector3 forward = Vector3.Cross(Vector3.up, transform.right).normalized;

            _rollAngleDeg += Vector3.Dot(moved, forward) / radius * Mathf.Rad2Deg;

            // ⚠️ THROUGH `ApplyTilt`, so the roll cannot leave the can at a height that
            // disagrees with its tilt. The two used to be written from two places.
            ApplyTilt(_toppleAngle);
        }
    }
}
