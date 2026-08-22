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

        public int SkinIndex { get => _skinIndex; set => _skinIndex = value; }
        public bool IsUpright => _isUpright;

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

        /// <summary>Host-side. Knock it over and pay the thrower.</summary>
        public void HostKnockDown(int throwerSlot)
        {
            if (!NetAuthority.ShouldResolve()) return;
            if (!_isUpright) return;

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

            var throwerMotor = GameServices.Round.PlayerAt(throwerSlot);
            if (throwerMotor != null)
            {
                var ai = throwerMotor.GetComponent<AIController>();
                if (ai != null)
                {
                    string[] celebEmotes = { "dance", "yes", "tpose", "crouch" };
                    ai.TryTriggerEmote(celebEmotes[UnityEngine.Random.Range(0, celebEmotes.Length)], 0.9f);
                }
            }
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
            transform.position = _mark;
            transform.rotation = Quaternion.identity;

            // ⚠️ THE TILT STATE IS CLEARED, NOT JUST THE TRANSFORM. `_toppleAngle` and the roll
            // survive a restore otherwise, and the next knockdown starts its lift from wherever
            // the last one ended — which lands the can in the air on its second topple.
            _toppleAngle = 0.0f;
            _rollAngleDeg = 0.0f;
            _toppleTimer = 0.0f;
            _lastRollPosition = _mark;

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
            GameServices.Audio?.PlayAt(value ? "reset_complete" : "can_knockdown",
                                       transform.position);

            if (value)
            {
                GameServices.Voice?.OnLataRestored();
                Visual.ComicPopup.Spawn(transform.position + Vector3.up * 0.8f, "RESTORED!", UI.UiTheme.Defense, 1.2f);
            }
            else
            {
                GameServices.Voice?.OnLataKnocked();
                Visual.ComicPopup.Spawn(transform.position + Vector3.up * 0.8f, "DOWNED!", UI.UiTheme.Offense, 1.4f);
                UI.Hud.TriggerHitmarker(UI.UiTheme.Offense, "💥");
                Visual.ImpactBurst.SpawnAt(transform.position);
                Abilities.HeroHazards.SpawnConfettiShower(transform.position, 24);
                if (UnityEngine.Camera.main != null)
                {
                    var rig = UnityEngine.Camera.main.GetComponent<CameraSystem.CameraRig>();
                    if (rig != null) rig.Shake(0.35f, 0.2f);
                }
            }

            UprightChanged?.Invoke(value);
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
            if (_isUpright || _toppleTimer <= 0.0f) return;

            _toppleTimer = Mathf.Max(0.0f, _toppleTimer - Time.deltaTime);
            float t = 1.0f - (_toppleTimer / Balance.ToppleTime);

            _toppleAngle = Mathf.Lerp(0.0f, Balance.DownedTiltDeg, t);
            ApplyTilt(_toppleAngle);
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
