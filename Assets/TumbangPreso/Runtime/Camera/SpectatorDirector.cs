using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// THE AUTOPILOT CAMERA OPERATOR. It flies <see cref="SpectatorCamera"/> on its own,
    /// decides what the match is ABOUT right now, and cuts between genuinely different shots the
    /// way a broadcast director does.
    ///
    /// 🧑 2026-08-27: *"add autopilot option in spectator that moves on its own naturally and
    /// looks good, assume A LOT OF PPL WILL be watching how it moves so make sure it moves
    /// smooth and decides where to move camera properly"*. Then 2026-09-03, for the nationals:
    /// make it look **intentionally directed** rather than merely automatic.
    ///
    /// ⚠️⚠️ THIS IS A SEPARATE COMPONENT BECAUSE `SpectatorCamera`'S OWN HEADER SAYS IT MUST BE:
    /// *"If a cinematic auto-cam is ever wanted it is a new component with a new name."* That
    /// line was written beside 🧑's 2026-07-31 instruction *"dont give spectator AI... spectator
    /// should only be controllable by a person"*, and the 2026-08-27 request is the same person
    /// changing their mind twenty-seven days later. **The old instruction is superseded, not
    /// forgotten**, and the shape it asked for is honoured exactly: the spectator camera class
    /// still reads hardware and only hardware, and this class writes a POSE onto a transform.
    /// Nothing here writes gameplay state, sends an RPC, resolves a hit, or touches an
    /// <see cref="InputIntent"/>. A bot is still one component away from flying the camera and
    /// zero components away from anything else.
    ///
    /// ⚠️⚠️ AND IT NEVER PAUSES AND NEVER REPLAYS. 🧑: *"dont let autopilot spectator pause or
    /// replay thats for human only"*. Nothing in this file calls `ToggleBroadcastPause`,
    /// `StartReplay` or `SetBroadcastScale`, and there is no code path from here to any of them.
    /// `SpectatorCamera` no longer self-replays either, so there is nothing left to suppress.
    ///
    /// § WHAT CHANGED ON 2026-09-04, AND WHY THE OLD VERSION LOOKED AUTOMATIC
    ///
    /// ⚠️⚠️ THE OLD DIRECTOR HAD EXACTLY ONE SHOT AND SCORED FOUR BODIES CONTINUOUSLY.
    /// `ScoreSubject` summed six live terms every frame and `ComputeShot` solved one orbit
    /// bearing around a focus point at a distance driven by the spread between two things. A
    /// retrieval, an ultimate, a knockdown and a quiet beat were **that same solve at a different
    /// radius**. `docs/TODO.md` § 134.3 is the full baseline; the three faults that mattered:
    ///
    ///   1. **It committed to a PERSON, never to a PLAY.** `MinShotSeconds` held the subject for
    ///      2.4 s. A retrieval runs about four, so the camera could legitimately leave one
    ///      two-thirds of the way through because somebody else briefly scored higher.
    ///   2. **It never asked whether it could SEE anything.** The only spatial guard was a clamp
    ///      to the playable box. Both maps are enclosed, Eskinita by house facades and Ilalim
    ///      ng Tulay by viaduct pillars, so a bearing that landed on a pillar was taken, and
    ///      on a broadcast that is indistinguishable from the stream having broken.
    ///   3. **The lata left the frame in the one shot the whole game is about.** `FocusPoint`
    ///      framed a retriever with the taya INSTEAD of the can, so the objective was absent from
    ///      the retrieval.
    ///
    /// **All three are answered by structure rather than by tuning**: <see cref="SpectatorInterest"/>
    /// is an EVENT with a start, a duration and a commitment window; <see cref="Solve"/> is nine
    /// different camera solves rather than one; and <see cref="ValidatePose"/> refuses a pose that
    /// cannot see what it was sent for.
    ///
    /// § THE THREE THINGS THE OLD VERSION GOT RIGHT, KEPT UNCHANGED
    ///
    /// ⚠️⚠️ 1. IT CUTS. IT DOES NOT WHIP-PAN ACROSS THE MAP. When the interesting thing moves
    /// somewhere this camera cannot smoothly reach, the shot CUTS: the pose is written outright
    /// and the smoothing state is cleared. Flying the whole way instead is the single most
    /// amateur thing a camera can do, it arrives after the moment it was sent for, and on a
    /// 14 by 14 m arena it sweeps the viewer past everything else on the way.
    ///
    /// ⚠️⚠️ 2. IT COMMITS. The mechanism moved from a subject to a beat; the reasoning is the
    /// same one `AiTuning.TagSwitchMargin` and `HeadingCommitSeconds` record for the bots: a
    /// decision that genuinely changes 60 times a second gets drawn faithfully as something that
    /// cannot make its mind up.
    ///
    /// ⚠️⚠️ 3. IT IS NEVER COMPLETELY STILL. A locked-off camera on a quiet moment reads as a
    /// frozen game. <see cref="DriftDegPerSecond"/> keeps a slow orbit running under every held
    /// shot, small enough that nobody can name it and large enough that the picture is alive.
    ///
    /// ⚠️ IT AIMS WHERE THE SUBJECT IS GOING, NOT WHERE IT IS. <see cref="LeadSeconds"/> is why
    /// the camera looks like it knew: an operator who tracks a runner is always slightly ahead of
    /// them, and one who is exactly on them looks like they are being dragged.
    /// </summary>
    [RequireComponent(typeof(SpectatorCamera))]
    public sealed class SpectatorDirector : MonoBehaviour
    {
        // -------------------------------------------------------------------
        // § THE SHOT
        // -------------------------------------------------------------------

        /// <summary>
        /// How long a shot must hold before another beat may take it, in seconds.
        ///
        /// ⚠️ 2.4 s IS A BROADCAST MINIMUM, NOT A GUESS. Under about two seconds a viewer has not
        /// finished reading the frame before it changes, and a director who cuts faster than that
        /// is editing rather than covering.
        ///
        /// ⚠️⚠️ IT IS NOW A DEFAULT RATHER THAN THE RULE. Each interest carries its own
        /// `CommitSeconds` (`SpectatorInterestModel`), because a can falling over is done in about
        /// a second and holding it for 2.4 is a shot of an empty street.
        /// </summary>
        public const float MinShotSeconds = SpectatorInterestModel.MinCommit;

        /// <summary>
        /// The longest a single shot holds before the director looks for a new ANGLE on the same
        /// beat, in seconds.
        ///
        /// ⚠️ IT IS AN ANGLE CHANGE, NOT A SUBJECT CHANGE. A nine-second unbroken take on one
        /// player is a security camera. This re-frames the same play from a different bearing,
        /// which is what a second camera would be doing in a real gallery.
        /// </summary>
        public const float MaxShotSeconds = 9.0f;

        /// <summary>
        /// Past this much movement, the shot CUTS instead of flying, in metres.
        ///
        /// ⚠️⚠️ 6.0 m IS UNDER HALF THE ARENA ON PURPOSE. `Confinement` is 7.0, so the box is
        /// 14 m across: anything beyond this is most of the way across the court, and flying it
        /// takes long enough that the camera lands after the thing it was sent to see. Below it,
        /// a fly is smoother than a cut and keeps the viewer oriented.
        /// </summary>
        public const float CutDistance = 6.0f;

        /// <summary>Seconds for the position spring to close most of its error.</summary>
        public const float PositionSmoothTime = 0.55f;

        /// <summary>
        /// Seconds for the aim to close most of its error.
        ///
        /// ⚠️ THE AIM IS SLOWER THAN THE BODY, WHICH IS BACKWARDS ON PAPER AND CORRECT ON SCREEN.
        /// A camera whose rotation snaps to its target while its position eases reads as two
        /// different cameras; letting the aim trail slightly is the lag a real head and a real
        /// tripod have.
        /// </summary>
        public const float AimSmoothTime = 0.32f;

        /// <summary>How far ahead of a moving subject the camera aims, in seconds.</summary>
        public const float LeadSeconds = 0.42f;

        /// <summary>The slow orbit that keeps a held shot from reading as a freeze frame.</summary>
        public const float DriftDegPerSecond = 3.4f;

        // -------------------------------------------------------------------
        // § THE FRAME
        // -------------------------------------------------------------------

        /// <summary>How high above the floor the camera sits for a normal shot, in metres.</summary>
        public const float ShotHeight = 3.1f;

        public const float ShotDistanceMin = 5.5f;
        public const float ShotDistanceMax = 13.5f;

        /// <summary>
        /// Where the camera looks on a body, in metres above its feet.
        ///
        /// ⚠️ CHEST, NOT HEAD AND NOT FEET. Aiming at the transform origin puts the horizon
        /// through everyone's shins; aiming at the head leaves no room above it and reads as
        /// crowding. This is the standard framing height and it is why the arena floor and the
        /// chalk stay in the bottom of the picture where they belong.
        /// </summary>
        public const float SubjectEyeLine = 1.15f;

        /// <summary>
        /// How far off centre the main subject is placed, as a fraction of the frame.
        ///
        /// ⚠️⚠️ THE BRIEF ASKS FOR THIS BY NAME: *"avoid centring everything."* A subject dead
        /// centre with its objective out of frame is the composition every automatic camera
        /// produces. Aiming a little PAST the subject toward whatever the shot is also about puts
        /// the subject off centre and the objective in frame in one operation, which is what a
        /// two-shot is.
        ///
        /// ⚠️ 0.34 KEEPS THE NAMED SUBJECT DOMINANT. Carried over from the old `SecondaryWeight`
        /// with its reasoning: *"a two-shot is the whole game and a one-shot is half of it."*
        /// </summary>
        public const float SecondaryWeight = 0.34f;

        /// <summary>
        /// Headroom: how far above the aim point the camera looks, in metres.
        ///
        /// ⚠️ A CAMERA AIMED EXACTLY AT A CHEST PUTS THE HEAD AGAINST THE TOP OF THE FRAME when
        /// it is close, which is the "crowding" every framing guide names. Lifting the aim a
        /// little keeps a person's head inside the picture at every distance this director uses.
        /// </summary>
        public const float Headroom = 0.35f;

        // -------------------------------------------------------------------
        // § VALIDATION
        // -------------------------------------------------------------------

        /// <summary>
        /// How far from the playable box the camera may stand, in metres.
        ///
        /// ⚠️ `AIController.PlayableHalfX/Z` ARE MEASURED OFF THE MAP'S OWN BOUNDS AT LOAD and
        /// are the same numbers the bots are held to. The margin is what lets a wide shot stand
        /// outside the chalk; past it, both maps are solid.
        /// </summary>
        public const float BoundsMargin = 1.5f;

        /// <summary>How much clear space a camera needs around it, in metres.</summary>
        public const float ClearanceRadius = 0.45f;

        /// <summary>
        /// How many bearings are tried before the shot falls back to the safe pose.
        ///
        /// ⚠️⚠️ IT RE-SOLVES THE SAME INTEREST FROM A DIFFERENT ANGLE RATHER THAN ABANDONING THE
        /// PLAY, and that distinction is the whole of why validation lives here and not in the
        /// interest model. Occlusion is a property of a POSE. A retrieval does not stop being the
        /// most interesting thing in the match because one candidate bearing happens to look at
        /// the back of a house.
        ///
        /// ⚠️ SIX AT 60 DEGREES IS A FULL CIRCLE. Anything finer is more raycasts for bearings
        /// that differ by less than the shot's own drift.
        /// </summary>
        public const int BearingAttempts = 6;

        /// <summary>
        /// How often the pose is re-checked for occlusion, in seconds.
        ///
        /// ⚠️⚠️ 0.4 s, AND IT IS A FIX FOR CUTTING RATHER THAN A PERFORMANCE TUNE. See the note
        /// in `ComputeShot`: a per-frame validator and `DriftDegPerSecond` fought each other and
        /// produced a cut every 1.45 s against a 2.4 s minimum.
        /// </summary>
        public const float RevalidateSeconds = 0.4f;

        // -------------------------------------------------------------------

        private SpectatorCamera _camera;
        private readonly SpectatorInterestModel _interest = new SpectatorInterestModel();

        private bool _engaged;
        private SpectatorInterest _shot;
        private float _shotClock;
        private float _bearingDeg;
        private float _driftSign = 1.0f;

        private float _validatedAt = -99.0f;

        private Vector3 _posVelocity;
        private float _yawVelocity;
        private float _pitchVelocity;
        private float _yawDeg;
        private float _pitchDeg;

        /// <summary>How many times the pose validator has had to move a shot. For the capture log.</summary>
        public int OccludedPoseRejections { get; private set; }

        /// <summary>How many times it ran out of bearings and used the safe pose.</summary>
        public int SafePoseFallbacks { get; private set; }

        /// <summary>How many cuts this session. For the capture log.</summary>
        public int Cuts { get; private set; }

        /// <summary>The beat currently being covered.</summary>
        public SpectatorBeat Beat => _shot.Valid ? _shot.Beat : SpectatorBeat.Quiet;

        /// <summary>The shot currently being composed.</summary>
        public ShotType Shot => _shot.Valid ? _shot.Shot : ShotType.QuietEstablish;

        /// <summary>The director's own account of the last decision, for a capture log.</summary>
        public string Diagnostic => _interest.LastDecision;

        /// <summary>
        /// True while the autopilot owns the camera.
        ///
        /// ⚠️ SETTING IT IS WHAT DISENGAGES IT TOO, and both directions clear the smoothing
        /// state. Handing a half-finished spring back to a human operator makes their first
        /// frame of input feel sticky, which is the one thing a camera must never do.
        /// </summary>
        public bool Engaged
        {
            get => _engaged;
            set
            {
                if (_engaged == value) return;

                _engaged = value;
                _posVelocity = Vector3.zero;
                _yawVelocity = 0.0f;
                _pitchVelocity = 0.0f;

                if (_engaged)
                {
                    _shot = default;
                    _shotClock = MaxShotSeconds;   // pick a shot on the first frame
                    _yawDeg = transform.eulerAngles.y;
                    _pitchDeg = NormalisePitch(transform.eulerAngles.x);
                }
                else
                {
                    // Hand the human the angle that is actually on screen, or the view snaps
                    // back to whatever they were last holding.
                    _camera?.AdoptCurrentAngles();
                }
            }
        }

        public void Toggle() => Engaged = !Engaged;

        /// <summary>
        /// What the shot is, in three or four words, for the spectator status line.
        ///
        /// ⚠️ IT NAMES THE PLAY AND THE SUBJECT, NOT THE MODE. "AUTOPILOT · following P2" tells
        /// an operator nothing they cannot see; "retrieval · P2" tells them why the camera is
        /// there and therefore whether it has read the play the same way they have.
        /// </summary>
        public string ShotName()
        {
            if (!_shot.Valid || _shot.Main == null) return "finding a shot";

            string who = _shot.Main.DisplayName();

            switch (_shot.Beat)
            {
                case SpectatorBeat.Retrieval: return "retrieval · " + who;
                case SpectatorBeat.Ultimate: return "ultimate · " + who;
                case SpectatorBeat.LataHit: return "the can · " + who;
                case SpectatorBeat.Tag: return "tag · " + who;
                case SpectatorBeat.SlipperLanded: return "the can";
                case SpectatorBeat.Downed: return "down · " + who;
                case SpectatorBeat.Reset: return "reset · " + who;
                case SpectatorBeat.ThrowPrep: return "throw · " + who;
                default: return "the street";
            }
        }

        private void Awake() => _camera = GetComponent<SpectatorCamera>();

        private void OnDisable() => _interest.Unhook();

        /// <summary>
        /// ⚠️ `LateUpdate`, NOT `Update`, AND THE ORDER IS THE POINT. `SpectatorCamera.Update`
        /// runs its own pose work first and returns early on the branches this owns; writing the
        /// transform after it means there is never a frame in which both have written and the
        /// loser's value is the one that was rendered.
        /// </summary>
        private void LateUpdate()
        {
            if (!_engaged || _camera == null) return;
            if (UI.Panel.AnyOpen) return;

            float dt = Time.unscaledDeltaTime;
            if (dt <= 0.0f) return;

            _shotClock += dt;

            PickShot();
            FlyToShot(dt);
        }

        // -------------------------------------------------------------------
        // § WHAT TO COVER
        // -------------------------------------------------------------------

        private void PickShot()
        {
            var next = _interest.Decide();
            if (!next.Valid) return;

            bool sameBeat = _shot.Valid
                            && next.Beat == _shot.Beat
                            && ReferenceEquals(next.Main, _shot.Main);

            if (!sameBeat)
            {
                _shot = next;
                NewShot(cut: true);
                return;
            }

            _shot = next;

            // Same play, but the take has run long. Re-frame from somewhere else rather than
            // holding one angle until it reads as a fixed camera.
            if (_shotClock >= MaxShotSeconds) NewShot(cut: false);
        }

        /// <summary>
        /// Chooses a bearing to shoot the current beat from and restarts the shot clock.
        ///
        /// ⚠️⚠️ THE BEARING IS NOT RANDOM, IT IS CHOSEN AGAINST THE ARENA AND AGAINST THE SHOT.
        /// A camera dropped on an arbitrary compass angle spends a third of its shots outside the
        /// map looking at the back of a house facade, because the playable box is 8.6 by 13.0 and
        /// the walls are closer on one axis than the other. Each shot type has its own idea of
        /// where the lens belongs relative to the action, and <see cref="BaseBearingFor"/> is
        /// that idea.
        ///
        /// ⚠️⚠️ AND IT ALTERNATES SHOULDERS RATHER THAN REPEATING, WITH ONE EXCEPTION THAT IS THE
        /// AXIS RULE. Two consecutive shots from the same shoulder read as one long shot with a
        /// jump cut in the middle. But alternating on EVERY cut crosses the action line every
        /// other time, which is the thing the brief asks to avoid: *"avoid crossing the action
        /// axis without re-establishing orientation."* So the shoulder flips on a re-frame of the
        /// SAME beat, and holds across a cut to a NEW one, where the wide establishing geometry
        /// re-establishes the axis for free.
        /// </summary>
        private void NewShot(bool cut)
        {
            _shotClock = 0.0f;

            if (!cut) _driftSign = -_driftSign;

            _bearingDeg = BaseBearingFor(_shot);

            // ⚠️⚠️ A NEW BEAT DOES NOT AUTOMATICALLY MEAN A CUT, AND ASSUMING IT DID IS MOST OF
            // WHY THE FIRST CAPTURE RAN AT **53 CUTS IN 77 SECONDS**. `PickShot` calls this with
            // `cut: true` on every beat change, and a beat change is often a small reframe: a
            // retrieval resolving into a tag is the same two people two metres apart. Cutting
            // there is an edit nobody asked for, and `docs/TODO.md` § 134.3 lists *"excessive
            // cutting"* as a baseline failure in its own right.
            //
            // ⚠️⚠️ SO THE TEST IS THE ONE `FlyToShot` ALREADY USES: how far would the CAMERA
            // have to travel. Beyond `CutDistance` a fly arrives after the moment it was sent
            // for and sweeps the viewer past everything on the way, so it cuts; inside it, a fly
            // is smoother and keeps the viewer oriented. **One rule, asked in two places, rather
            // than a rule and an assumption.**
            if (!cut) return;

            ComputeShot(out Vector3 wantPos, out _, out _);

            if ((wantPos - transform.position).sqrMagnitude > CutDistance * CutDistance)
            {
                CutNow();
                Cuts++;
            }
        }

        /// <summary>
        /// Where this kind of shot wants the lens, as a compass bearing from the focus point.
        ///
        /// ⚠️⚠️ THIS FUNCTION IS THE DIFFERENCE BETWEEN NINE SHOTS AND ONE. The old director had
        /// a single answer here: the bearing from the arena centre out to the focus, swung 28 to
        /// 62 degrees to one shoulder. Every composition below is a different geometric
        /// relationship to what is actually happening.
        /// </summary>
        private float BaseBearingFor(SpectatorInterest shot)
        {
            Vector3 main = shot.Main != null ? shot.Main.transform.position : Vector3.zero;

            switch (shot.Shot)
            {
                // ⚠️ PERPENDICULAR TO THE CHASE LINE, AND OUTSIDE IT. A camera behind the chaser
                // shows two backs and no gap; a camera on the line shows one person hiding the
                // other. Standing off the axis is what makes the distance between them readable,
                // which IS the tension in a chase.
                case ShotType.Chase:
                case ShotType.RetrievalTwoShot:
                {
                    Vector3 other = shot.Secondary != null
                        ? shot.Secondary.transform.position
                        : (shot.HasObjective ? shot.Objective : main + Vector3.forward);

                    Vector3 axis = other - main;
                    axis.y = 0.0f;

                    if (axis.sqrMagnitude < 0.01f) return _bearingDeg;

                    float along = Mathf.Atan2(axis.x, axis.z) * Mathf.Rad2Deg;
                    return along + 90.0f * _driftSign;
                }

                // ⚠️ FROM BEHIND THE OBJECTIVE, LOOKING BACK AT THE PERSON. The can is large in
                // the foreground and the thrower is small in depth: that is what "objective shot"
                // means, and it is the only composition in this game that shows the ANGLE a
                // throw has to solve.
                case ShotType.Objective:
                {
                    if (!shot.HasObjective) return _bearingDeg;

                    Vector3 fromObjective = shot.Objective - main;
                    fromObjective.y = 0.0f;

                    if (fromObjective.sqrMagnitude < 0.01f) return _bearingDeg;

                    return Mathf.Atan2(fromObjective.x, fromObjective.z) * Mathf.Rad2Deg;
                }

                // ⚠️⚠️ THE ULTIMATE SHOTS AVOID THE CAST DIRECTION, WHICH IS THE HERO-SPECIFIC
                // RULE STATED ONCE INSTEAD OF SIX TIMES. Dante's brief asks not to put the camera
                // in the fissure path; Zack's asks for vertical room; Sean's asks not to stare at
                // empty ground. All three are the same instruction: stand OFF the axis the power
                // travels along, which is the caster's own facing. A per-hero table would be six
                // places to forget, which is `HeroAbility.Glyph`'s argument.
                case ShotType.UltimateWide:
                case ShotType.UltimateHero:
                {
                    Vector3 facing = shot.Main != null ? shot.Main.transform.forward : Vector3.forward;
                    facing.y = 0.0f;

                    if (facing.sqrMagnitude < 0.01f) return _bearingDeg;

                    float castDeg = Mathf.Atan2(facing.x, facing.z) * Mathf.Rad2Deg;
                    return castDeg + 118.0f * _driftSign;
                }

                // The taya and the can, from outside the box looking in: the bearing from the
                // can out through the taya, so both are in frame with the chalk between them.
                case ShotType.Defender:
                {
                    if (!shot.HasObjective) return _bearingDeg;

                    Vector3 out2 = main - shot.Objective;
                    out2.y = 0.0f;

                    if (out2.sqrMagnitude < 0.01f) return _bearingDeg;

                    return Mathf.Atan2(out2.x, out2.z) * Mathf.Rad2Deg;
                }

                // A body on the floor and whatever is coming for it: stand square to the pair so
                // the gap closing is readable, which is the same argument as the chase.
                case ShotType.Recovery:
                {
                    Vector3 threat = shot.Secondary != null
                        ? shot.Secondary.transform.position : main + Vector3.forward;

                    Vector3 axis = threat - main;
                    axis.y = 0.0f;

                    if (axis.sqrMagnitude < 0.01f) return _bearingDeg;

                    return Mathf.Atan2(axis.x, axis.z) * Mathf.Rad2Deg + 70.0f * _driftSign;
                }

                // ⚠️ THE QUIET SHOT LOOKS ALONG THE LONG AXIS OF THE MAP. The box is 8.6 by 13.0,
                // so a shot down the 13 is a street and a shot across the 8.6 is a wall. This is
                // the one composition whose job is to say where we are.
                default:
                    return _driftSign > 0.0f ? 0.0f : 180.0f;
            }
        }

        // -------------------------------------------------------------------
        // § WHERE TO PUT THE CAMERA
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ A CUT CLEARS THE SPRINGS AS WELL AS THE POSE. Leaving a velocity in
        /// `Vector3.SmoothDamp` across a teleport makes the camera drift away from its mark for
        /// half a second after the cut, which reads as the shot being wrong rather than as a cut,
        /// and it is the classic way a "snap" ends up looking like a glitch.
        /// </summary>
        private void CutNow()
        {
            ComputeShot(out Vector3 wantPos, out float wantYaw, out float wantPitch);

            transform.position = wantPos;
            _yawDeg = wantYaw;
            _pitchDeg = wantPitch;
            _posVelocity = Vector3.zero;
            _yawVelocity = 0.0f;
            _pitchVelocity = 0.0f;

            transform.rotation = Quaternion.Euler(_pitchDeg, _yawDeg, 0.0f);
            _camera.AdoptPose(transform.position, _yawDeg, _pitchDeg);
        }

        private void FlyToShot(float dt)
        {
            // The slow orbit under every held shot.
            _bearingDeg += DriftDegPerSecond * _driftSign * dt;

            ComputeShot(out Vector3 wantPos, out float wantYaw, out float wantPitch);

            // ⚠️ THE CUT TEST IS ON THE WANTED POSE, NOT ON THE SUBJECT'S SPEED. What matters is
            // how far the CAMERA would have to travel: a subject sprinting straight at the lens
            // barely moves the shot, and one stepping sideways past a wall can move it right
            // across the court.
            if ((wantPos - transform.position).sqrMagnitude > CutDistance * CutDistance
                && _shotClock >= MinShotSeconds)
            {
                CutNow();
                Cuts++;
                return;
            }

            transform.position = Vector3.SmoothDamp(transform.position, wantPos,
                                                    ref _posVelocity, PositionSmoothTime,
                                                    Mathf.Infinity, dt);

            _yawDeg = Mathf.SmoothDampAngle(_yawDeg, wantYaw, ref _yawVelocity, AimSmoothTime,
                                            Mathf.Infinity, dt);
            _pitchDeg = Mathf.SmoothDampAngle(_pitchDeg, wantPitch, ref _pitchVelocity,
                                              AimSmoothTime, Mathf.Infinity, dt);

            transform.rotation = Quaternion.Euler(_pitchDeg, _yawDeg, 0.0f);
            _camera.AdoptPose(transform.position, _yawDeg, _pitchDeg);
        }

        /// <summary>
        /// What the shot is aimed at: the subject, pulled toward whatever it is also about.
        ///
        /// ⚠️⚠️ THE LATA IS IN THE RETRIEVAL FRAME NOW, WHICH IT WAS NOT. The old `FocusPoint`
        /// framed a retriever with the taya INSTEAD of the can, which meant the objective left
        /// the frame in the one shot the whole game is about. A retrieval two-shot pulls toward
        /// the midpoint of the taya and the can, so all three are in the picture.
        ///
        /// ⚠️ THE SUBJECT'S OWN VELOCITY IS THE LEAD, and it is read off the motor rather than
        /// differenced here: `CharacterMotor.Velocity` is the resolved value the body actually
        /// moved by, so a shove or a lunge is in it and a camera that anticipated a walk does not
        /// get left behind by a dash.
        /// </summary>
        private Vector3 FocusPoint(out float spread)
        {
            spread = 0.0f;

            if (!_shot.Valid || _shot.Main == null) return Vector3.up * SubjectEyeLine;

            Vector3 primary = _shot.Main.transform.position + Vector3.up * SubjectEyeLine;

            Vector3 v = _shot.Main.Velocity;
            v.y = 0.0f;
            primary += v * LeadSeconds;

            Vector3 secondary = primary;
            bool hasSecondary = false;

            switch (_shot.Shot)
            {
                case ShotType.RetrievalTwoShot:
                case ShotType.Chase:
                {
                    // Both the chaser and the objective, so the run has a destination in frame.
                    Vector3 sum = Vector3.zero;
                    int count = 0;

                    if (_shot.Secondary != null)
                    {
                        sum += _shot.Secondary.transform.position + Vector3.up * SubjectEyeLine;
                        count++;
                    }

                    if (_shot.HasObjective)
                    {
                        sum += _shot.Objective + Vector3.up * 0.4f;
                        count++;
                    }

                    if (count > 0)
                    {
                        secondary = sum / count;
                        hasSecondary = true;
                    }

                    break;
                }

                case ShotType.Objective:
                case ShotType.Defender:
                    if (_shot.HasObjective)
                    {
                        secondary = _shot.Objective + Vector3.up * 0.4f;
                        hasSecondary = true;
                    }
                    break;

                case ShotType.Recovery:
                case ShotType.UltimateWide:
                    if (_shot.Secondary != null)
                    {
                        secondary = _shot.Secondary.transform.position + Vector3.up * SubjectEyeLine;
                        hasSecondary = true;
                    }
                    break;

                // ⚠️ THE HERO SHOT AND THE QUIET SHOT ARE ONE-SHOTS ON PURPOSE. A hero shot whose
                // framing is pulled toward somebody else is not a hero shot, and a quiet
                // establishing shot pulled toward a second body is a two-shot of nothing
                // happening.
                default:
                    break;
            }

            if (!hasSecondary) return primary + Vector3.up * Headroom;

            spread = Flat(primary, secondary);
            return Vector3.Lerp(primary, secondary, SecondaryWeight) + Vector3.up * Headroom;
        }

        private void ComputeShot(out Vector3 position, out float yawDeg, out float pitchDeg)
        {
            Vector3 focus = FocusPoint(out float spread);

            Solve(focus, spread, _bearingDeg, out position);

            // ⚠️⚠️ VALIDATION RUNS ON A CADENCE, NOT EVERY FRAME, AND THE FIRST CAPTURE RUN IS
            // WHY. It reported **53 cuts across 77 seconds**, a cut every 1.45 s against a 2.4 s
            // minimum, with **28 occluded poses re-solved** and zero safe-pose fallbacks. Those
            // two numbers are the same number: a per-frame validator finds the held bearing
            // occluded, swings 60 degrees to a clear one, and `FlyToShot` then sees a pose that
            // moved further than `CutDistance` and cuts. `DriftDegPerSecond` walks the bearing
            // back toward the obstruction and it happens again. **The camera was cutting because
            // it was arguing with itself**, which is `docs/TODO.md` § 134.3's *"excessive
            // cutting"* arriving out of the fix for *"camera enters or faces geometry"*.
            //
            // ⚠️ A CAMERA DOES NOT NEED 60 Hz OCCLUSION. Bodies move at 2.5 to 5 m/s and walls do
            // not move at all, so asking two and a half times a second is well inside the rate at
            // which the answer can change, and it costs a fifth of the raycasts.
            //
            // ⚠️⚠️ A REFUSAL STILL MOVES THE CAMERA RATHER THAN THE SHOT. See `BearingAttempts`:
            // occlusion is a property of a POSE, and a retrieval does not stop being the most
            // interesting thing in the match because one bearing looks at a house.
            bool due = Time.unscaledTime - _validatedAt >= RevalidateSeconds;

            if (due && !ValidatePose(position, focus))
            {
                bool found = false;

                for (int i = 1; i < BearingAttempts && !found; i++)
                {
                    float tryBearing = _bearingDeg + i * (360.0f / BearingAttempts) * _driftSign;
                    Solve(focus, spread, tryBearing, out Vector3 candidate);

                    if (!ValidatePose(candidate, focus)) continue;

                    position = candidate;
                    _bearingDeg = tryBearing;
                    found = true;
                }

                // ⚠️⚠️ A SECOND SWEEP THAT ONLY ASKS FOR THE MAIN SUBJECT, AND THE CAPTURE IS
                // WHY IT EXISTS. The first run with a validation cadence reported **10 safe-pose
                // fallbacks in 77 seconds**: ten times the camera gave up and jumped to the
                // overhead establishing pose, which on a broadcast is a visible lurch. Both maps
                // are full of poles, wires, awnings and parked cars, so a bearing that can see
                // the runner AND the can AND the taya is often simply not there.
                //
                // ⚠️ SEEING THE SUBJECT IS THE SHOT; SEEING THE SECONDARY IS THE COMPOSITION.
                // Giving up the second is a worse frame. Giving up the first is a different
                // frame, and the overhead fallback is a different frame of a different match.
                if (!found)
                {
                    for (int i = 0; i < BearingAttempts && !found; i++)
                    {
                        float tryBearing = _bearingDeg + i * (360.0f / BearingAttempts) * _driftSign;
                        Solve(focus, spread, tryBearing, out Vector3 candidate);

                        if (!ValidatePose(candidate, focus, requireSecondary: false)) continue;

                        position = candidate;
                        _bearingDeg = tryBearing;
                        found = true;
                    }
                }

                if (found) OccludedPoseRejections++;
                else
                {
                    SafePoseFallbacks++;
                    position = SafePose(focus);
                }
            }

            if (due) _validatedAt = Time.unscaledTime;

            Vector3 toFocus = focus - position;
            if (toFocus.sqrMagnitude < 0.0001f) toFocus = Vector3.forward;

            yawDeg = Mathf.Atan2(toFocus.x, toFocus.z) * Mathf.Rad2Deg;

            float flat = new Vector2(toFocus.x, toFocus.z).magnitude;
            pitchDeg = -Mathf.Atan2(toFocus.y, Mathf.Max(flat, 0.01f)) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// The camera position for the current shot type, at a given bearing.
        ///
        /// ⚠️⚠️ THE DISTANCE AND THE HEIGHT ARE PER SHOT TYPE. That is the other half of "nine
        /// shots rather than one": a hero shot is LOW and CLOSE, a wide is HIGH and BACK, an
        /// objective shot is almost at can height so the can reads as large. The old solve had
        /// one height (3.1 m) and one distance rule (spread-driven) for everything.
        ///
        /// ⚠️ THE PULL-BACK IS STILL THE SPREAD, NOT A CONSTANT, wherever two things have to be
        /// in frame. Two subjects nine metres apart need a wider shot than one; a fixed distance
        /// frames one of them and crops the other, which on this game means showing the chase
        /// without the chaser.
        /// </summary>
        private void Solve(Vector3 focus, float spread, float bearingDeg, out Vector3 position)
        {
            float distance;
            float height;

            switch (_shot.Shot)
            {
                case ShotType.UltimateHero:
                    // Low, close, and looking slightly UP at the caster: the one composition in
                    // this game that makes a person look big.
                    distance = ShotDistanceMin * 0.82f;
                    height = 1.55f;
                    break;

                case ShotType.UltimateWide:
                    // High and back far enough that the whole footprint plus anybody standing in
                    // it fits. `docs/VISION.md` § 2 rule 5 as a camera rule.
                    distance = Mathf.Clamp(9.5f + spread * 0.8f, 9.5f, ShotDistanceMax);
                    height = 6.4f;
                    break;

                case ShotType.Objective:
                    // Near can height so the can is large in the foreground.
                    distance = Mathf.Clamp(4.2f + spread * 0.55f, 4.2f, 9.0f);
                    height = 1.25f;
                    break;

                case ShotType.Chase:
                    distance = Mathf.Clamp(ShotDistanceMin + spread * 0.70f,
                                           ShotDistanceMin, ShotDistanceMax);
                    height = 2.35f;
                    break;

                case ShotType.Recovery:
                    distance = Mathf.Clamp(4.6f + spread * 0.55f, 4.6f, 10.0f);
                    height = 1.85f;
                    break;

                case ShotType.QuietEstablish:
                    distance = ShotDistanceMax;
                    height = 7.2f;
                    break;

                default:
                    distance = Mathf.Clamp(ShotDistanceMin + spread * 0.75f,
                                           ShotDistanceMin, ShotDistanceMax);
                    height = ShotHeight + spread * 0.16f;
                    break;
            }

            float rad = bearingDeg * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Sin(rad), 0.0f, Mathf.Cos(rad)) * distance;

            position = focus + offset;
            position.y = height;

            // ⚠️⚠️ CLAMPED TO THE PLAYABLE BOX PLUS A MARGIN, AND THIS IS NOT COSMETIC. Both maps
            // are enclosed: Eskinita by house facades and Ilalim ng Tulay by viaduct pillars. A
            // camera allowed to solve for a bearing that puts it outside them spends the shot
            // looking at the inside face of a wall, and on a broadcast that is indistinguishable
            // from the stream having broken.
            float halfX = AIController.PlayableHalfX + BoundsMargin;
            float halfZ = AIController.PlayableHalfZ + BoundsMargin;
            position.x = Mathf.Clamp(position.x, -halfX, halfX);
            position.z = Mathf.Clamp(position.z, -halfZ, halfZ);
        }

        // -------------------------------------------------------------------
        // § CAN THIS CAMERA SEE WHAT IT WAS SENT FOR
        //
        // ⚠️⚠️ THE OLD DIRECTOR NEVER ASKED. Its only spatial guard was the box clamp above, so a
        // bearing that landed on a viaduct pillar was taken and the shot was of a pillar. That is
        // `docs/TODO.md` § 134.3's "camera enters or faces geometry", and it is the failure a
        // viewer reads as the stream having broken rather than as a bad angle.
        //
        // ⚠️ IT FILTERS BY COMPONENT RATHER THAN BY LAYER, FOR THE PROJECT'S OWN REASON.
        // `ProjectSettings/TagManager.asset` has no custom layers: everything is on `Default`.
        // `Slipper.ResolveFlight` and `AIController.ShoveRouteIsClear` both solve this the same
        // way. **A player briefly crossing the lens is not an occlusion.** Bodies move, and a
        // camera that cut every time somebody walked past would cut constantly.
        // -------------------------------------------------------------------

        private static readonly Collider[] Overlap = new Collider[8];

        private bool ValidatePose(Vector3 position, Vector3 focus,
                                  bool requireSecondary = true)
        {
            // Inside a wall, a building, a pillar, a vehicle or a prop?
            int hits = Physics.OverlapSphereNonAlloc(position, ClearanceRadius, Overlap, ~0,
                                                     QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hits; i++)
            {
                if (IsSceneryFree(Overlap[i])) continue;
                return false;
            }

            // A clear line to what the shot is about.
            if (!CanSee(position, focus)) return false;

            if (_shot.Main != null
                && !CanSee(position, _shot.Main.transform.position + Vector3.up * SubjectEyeLine))
                return false;

            if (!requireSecondary) return true;

            // ⚠️ THE SECOND SIGHTLINE IS THE SECONDARY OR THE OBJECTIVE, WHICHEVER THE SHOT IS
            // ACTUALLY ABOUT. Asking for both would refuse a legitimate retrieval frame whenever
            // the taya happened to stand behind the can.
            if (_shot.Secondary != null)
                return CanSee(position,
                              _shot.Secondary.transform.position + Vector3.up * SubjectEyeLine);

            if (_shot.HasObjective) return CanSee(position, _shot.Objective + Vector3.up * 0.4f);

            return true;
        }

        private static bool CanSee(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.05f) return true;

            var hits = Physics.RaycastAll(from, delta / length, length, ~0,
                                          QueryTriggerInteraction.Ignore);

            foreach (var hit in hits)
                if (!IsSceneryFree(hit.collider)) return false;

            return true;
        }

        private static bool IsSceneryFree(Collider collider)
        {
            if (collider == null) return true;
            if (collider.GetComponentInParent<CharacterMotor>() != null) return true;
            if (collider.GetComponentInParent<Slipper>() != null) return true;
            if (collider.GetComponentInParent<Lata>() != null) return true;
            return false;
        }

        /// <summary>
        /// The pose used when no bearing works: high over the arena, looking down at the action.
        ///
        /// ⚠️⚠️ A KNOWN SAFE ESTABLISHING POSE IS PART OF THE BRIEF AND IT HAS TO BE SAFE BY
        /// CONSTRUCTION RATHER THAN BY VALIDATION. Nothing in either map is 11 m in the air over
        /// the middle of the court, so this cannot be inside anything, and looking down means
        /// nothing can be between it and the floor. A fallback that could itself fail is not a
        /// fallback.
        /// </summary>
        private static Vector3 SafePose(Vector3 focus)
        {
            Vector3 pose = focus;
            pose.y = 11.0f;

            // Pulled back a little along Z so the shot is a raked overhead rather than a plan
            // view, which reads as a security camera.
            pose.z -= 5.0f;
            pose.z = Mathf.Clamp(pose.z, -AIController.PlayableHalfZ, AIController.PlayableHalfZ);

            return pose;
        }

        // -------------------------------------------------------------------

        private static float Flat(Vector3 a, Vector3 b)
        {
            a.y = 0.0f;
            b.y = 0.0f;
            return Vector3.Distance(a, b);
        }

        private static float NormalisePitch(float eulerX)
            => eulerX > 180.0f ? eulerX - 360.0f : eulerX;
    }
}
