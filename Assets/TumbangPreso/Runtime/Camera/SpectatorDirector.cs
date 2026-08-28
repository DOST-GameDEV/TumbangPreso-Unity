using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// THE AUTOPILOT CAMERA OPERATOR. It flies <see cref="SpectatorCamera"/> on its own,
    /// picks who to watch, and cuts between shots the way a broadcast director does.
    ///
    /// 🧑 2026-08-27: *"add autopilot option in spectator that moves on its own naturally and
    /// looks good, assume A LOT OF PPL WILL be watching how it moves so make sure it moves
    /// smooth and decides where to move camera properly"*.
    ///
    /// ⚠️⚠️ THIS IS A SEPARATE COMPONENT BECAUSE `SpectatorCamera`'S OWN HEADER SAYS IT MUST BE:
    /// *"If a cinematic auto-cam is ever wanted it is a new component with a new name."* That
    /// line was written beside 🧑's 2026-07-31 instruction *"dont give spectator AI... spectator
    /// should only be controllable by a person"*, and the 2026-08-27 request above is the same
    /// person changing their mind twenty-seven days later. **The old instruction is superseded,
    /// not forgotten**, and the shape it asked for is honoured exactly: the spectator camera
    /// class still reads hardware and only hardware, and this class writes a POSE onto a
    /// transform. Nothing here writes gameplay state, sends an RPC, resolves a hit, or touches
    /// an <see cref="InputIntent"/>. A bot is still one component away from flying the camera
    /// and zero components away from nothing else.
    ///
    /// ⚠️⚠️ AND IT NEVER PAUSES AND NEVER REPLAYS. 🧑, in the next message: *"dont let autopilot
    /// spectator pause or replay thats for human only"*. Two things follow, and the second one is
    /// the one that is easy to miss:
    ///
    ///   1. Nothing in this file calls `ToggleBroadcastPause`, `StartReplay` or
    ///      `SetBroadcastScale`. There is no code path from here to any of them.
    ///   2. **`SpectatorCamera` ALREADY REPLAYS BY ITSELF** and always has:
    ///      `StepPendingHighlight` starts one on a knockdown, a tag or a score play with no key
    ///      pressed at all. Left alone, engaging the autopilot would produce a camera that flies
    ///      itself AND replays itself, which is exactly what the instruction forbids however
    ///      little of it this file wrote. `SpectatorCamera.AutopilotSuppressesAutoReplay` is
    ///      where that is turned off, and it is turned off only while this is engaged, so a human
    ///      operator keeps the behaviour they have always had.
    ///
    /// ⚠️ THE HUMAN TAKES THE WHEEL BY MOVING IT. Any look, any fly key, any target key
    /// disengages the autopilot on the spot. A broadcast operator does not want to hunt for a
    /// toggle while a play is happening, and a camera that fights its own operator for half a
    /// second is worse than one that never helped.
    ///
    /// § THE THREE THINGS THAT MAKE IT LOOK LIKE AN OPERATOR RATHER THAN A SCRIPT
    ///
    /// ⚠️⚠️ 1. IT CUTS. IT DOES NOT WHIP-PAN ACROSS THE MAP. When the interesting thing moves
    /// somewhere this camera cannot smoothly reach, the shot CUTS: the pose is written outright
    /// and the smoothing state is cleared. Flying the whole way instead is the single most
    /// amateur thing a camera can do, it arrives after the moment it was sent for, and on a
    /// 14 by 14 m arena it sweeps the viewer past everything else on the way.
    ///
    /// ⚠️⚠️ 2. IT COMMITS TO A SUBJECT. Every term in <see cref="ScoreSubject"/> moves
    /// continuously while four bodies run, so the leader changes several times a second and a
    /// camera that followed it would be unwatchable. `SubjectSwitchMargin` and `MinShotSeconds`
    /// are the same fix `AiTuning.TagSwitchMargin` and `HeadingCommitSeconds` are for the bots,
    /// for the same reason, found the same way: a decision that genuinely changes 60 times a
    /// second gets drawn faithfully as something that cannot make its mind up.
    ///
    /// ⚠️⚠️ 3. IT IS NEVER COMPLETELY STILL. A locked-off camera on a quiet moment reads as a
    /// frozen game. `DriftDegPerSecond` keeps a slow orbit running under every held shot, small
    /// enough that nobody can name it and large enough that the picture is alive.
    ///
    /// ⚠️ IT AIMS WHERE THE SUBJECT IS GOING, NOT WHERE IT IS. `LeadSeconds` is why the camera
    /// looks like it knew: an operator who tracks a runner is always slightly ahead of them, and
    /// one who is exactly on them looks like they are being dragged.
    /// </summary>
    [RequireComponent(typeof(SpectatorCamera))]
    public sealed class SpectatorDirector : MonoBehaviour
    {
        // -------------------------------------------------------------------
        // § THE SHOT
        // -------------------------------------------------------------------

        /// <summary>
        /// How long a shot must hold before another subject may take it, in seconds.
        ///
        /// ⚠️ 2.4 s IS A BROADCAST MINIMUM, NOT A GUESS. Under about two seconds a viewer has not
        /// finished reading the frame before it changes, and a director who cuts faster than that
        /// is editing rather than covering. It is also long enough to ride out the whole of a
        /// retrieval run, which at `Balance.Speed` 4.6 m/s crosses the 14 m box in about three.
        /// </summary>
        public const float MinShotSeconds = 2.4f;

        /// <summary>
        /// The longest a single shot holds before the director looks for a new angle even
        /// though nothing has changed, in seconds.
        ///
        /// ⚠️ IT IS AN ANGLE CHANGE, NOT NECESSARILY A SUBJECT CHANGE. A nine-second unbroken
        /// take on one player is a security camera. This lets the same subject be re-framed from
        /// a different bearing, which is what a second camera would be doing in a real gallery.
        /// </summary>
        public const float MaxShotSeconds = 9.0f;

        /// <summary>
        /// How much better a rival subject must score to take the shot early.
        ///
        /// ⚠️ THE SAME REASONING AS `AiTuning.TagSwitchMargin`, and it earned its place the same
        /// way. Without it the camera follows whichever of four running bodies is momentarily
        /// ahead on a continuous score, which is a camera that points at the middle of the arena
        /// and shakes.
        /// </summary>
        public const float SubjectSwitchMargin = 1.25f;

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
        /// ⚠️ THE AIM IS SLOWER THAN THE BODY, WHICH IS BACKWARDS FROM WHAT LOOKS RIGHT ON PAPER
        /// AND CORRECT ON SCREEN. A camera whose rotation snaps to its target while its position
        /// eases reads as two different cameras; letting the aim trail slightly is the lag a real
        /// head and a real tripod have.
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

        /// <summary>
        /// The closest and furthest the camera stands from what it is framing, in metres.
        ///
        /// ⚠️ THE RANGE IS DRIVEN BY THE SPREAD OF WHAT IS IN THE SHOT, not by taste. A single
        /// runner is worth a tight 5.5 m; a taya closing on a retriever with the lata between
        /// them is three things that all have to be in frame, and the pull-back is what keeps
        /// `docs/VISION.md` § 2 rule 5 true of a broadcast frame as well as an ability one.
        /// </summary>
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
        /// How much the secondary point of interest pulls the framing off the subject.
        ///
        /// ⚠️ A TWO-SHOT IS THE WHOLE GAME AND A ONE-SHOT IS HALF OF IT. `docs/VISION.md`: *"the
        /// tension is the retrieval, not the throw"*, and a retrieval only reads if the chaser
        /// is in the frame with the chased. 0.34 keeps the named subject dominant while
        /// guaranteeing the other thing is on screen.
        /// </summary>
        public const float SecondaryWeight = 0.34f;

        // -------------------------------------------------------------------

        private SpectatorCamera _camera;

        private bool _engaged;
        private CharacterMotor _subject;
        private float _shotClock;
        private float _bearingDeg;
        private float _driftSign = 1.0f;

        private Vector3 _posVelocity;
        private float _yawVelocity;
        private float _pitchVelocity;
        private float _yawDeg;
        private float _pitchDeg;

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
                    _subject = null;
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
        /// ⚠️ IT NAMES THE SUBJECT AND THE REASON, not the mode. "AUTOPILOT · following P2" tells
        /// an operator nothing they cannot see; "RETRIEVAL · P2" tells them why the camera is
        /// there and therefore whether it has read the play the same way they have.
        /// </summary>
        public string ShotName()
        {
            if (_subject == null) return "finding a shot";

            string who = _subject.DisplayName();

            if (_subject.IsStunned || _subject.IsTripped) return "down · " + who;
            if (_subject.IsTaggable()) return "retrieval · " + who;

            var kit = _subject.AbilitySystem != null ? _subject.AbilitySystem.Kit : null;
            if (kit != null && kit.Ultimate != null
                && (kit.Ultimate.IsWindingUp || kit.Ultimate.IsActive))
                return "ultimate · " + who;

            if (_subject.HoldingSlipper) return "armed · " + who;
            if (_subject.IsDefender) return "the taya · " + who;

            return who;
        }

        private void Awake() => _camera = GetComponent<SpectatorCamera>();

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

            PickSubject();
            FlyToShot(dt);
        }

        // -------------------------------------------------------------------
        // § WHO TO WATCH
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE SCORE IS THE GAME'S OWN STATEMENT OF WHAT MATTERS, NOT A LIST OF THINGS THAT
        /// MOVE. `docs/VISION.md` § 0: *"The tension is the retrieval, not the throw. Throwing is
        /// safe and free; going back in for your tsinelas is the only moment you can be caught."*
        /// So an armed attacker inside the chalk outranks everything, and it outranks it by a
        /// lot: that is the shot the whole game is about, and a director who cuts away from it to
        /// follow a throw has missed the point of the sport.
        /// </summary>
        private void PickSubject()
        {
            var round = GameServices.Round;
            if (round == null) return;

            CharacterMotor best = null;
            float bestScore = float.NegativeInfinity;

            foreach (var unit in round.Players)
            {
                if (unit == null || !unit.RoundActive) continue;

                float score = ScoreSubject(unit, round);

                // The shot in hand keeps its subject unless somebody clearly outranks it.
                if (unit == _subject) score += SubjectSwitchMargin;

                if (score <= bestScore) continue;

                bestScore = score;
                best = unit;
            }

            if (best == null) return;

            bool held = _shotClock < MinShotSeconds;
            bool stale = _shotClock >= MaxShotSeconds;

            if (best != _subject && !held)
            {
                _subject = best;
                NewShot(cut: true);
                return;
            }

            // Same subject, but the take has run long. Re-frame from somewhere else rather than
            // holding one angle until it reads as a fixed camera.
            if (stale)
            {
                if (_subject == null) _subject = best;
                NewShot(cut: false);
            }
        }

        private float ScoreSubject(CharacterMotor unit, RoundDirector round)
        {
            float score = 0.0f;

            // ⚠️ THE RETRIEVAL, AND IT IS THE LARGEST TERM BY A WIDE MARGIN ON PURPOSE.
            if (unit.IsTaggable()) score += 6.0f;

            // Armed but still outside: the run is about to start, which is the shot BEFORE the
            // shot and is worth being on early.
            else if (unit.HoldingSlipper) score += 2.2f;

            // ⚠️ THE TAYA IS NEVER THE SUBJECT ON ITS OWN, and that is deliberate rather than an
            // oversight. It is almost always the SECONDARY in somebody else's frame, because a
            // defender alone in shot is a person standing near a tin can. It scores enough to be
            // picked when literally nothing else is happening.
            if (unit.IsDefender) score += 0.6f;

            // Somebody on the floor is a moment that just resolved, and holding on them for a
            // beat is how a viewer learns what happened.
            if (unit.IsStunned || unit.IsTripped) score += 2.6f;

            // ⚠️ PROXIMITY BETWEEN THE HUNTER AND THE HUNTED IS THE TENSION ITSELF. A taya six
            // metres behind a retriever is a chase; one on the far side of the arena is two
            // unrelated people.
            var taya = DefenderOf(round);
            if (taya != null && unit != taya && unit.IsTaggable())
            {
                float gap = Flat(taya.transform.position, unit.transform.position);
                score += Mathf.Clamp01(1.0f - gap / 9.0f) * 3.0f;
            }

            // A hero mid-cast is the most expensive thing on screen and it is over in a second.
            var kit = unit.AbilitySystem != null ? unit.AbilitySystem.Kit : null;
            if (kit != null)
            {
                if (kit.Ultimate != null && (kit.Ultimate.IsWindingUp || kit.Ultimate.IsActive))
                    score += 5.0f;
                else if ((kit.Skill1 != null && kit.Skill1.IsActive)
                         || (kit.Skill2 != null && kit.Skill2.IsActive))
                    score += 1.4f;
            }

            return score;
        }

        // -------------------------------------------------------------------
        // § WHERE TO PUT THE CAMERA
        // -------------------------------------------------------------------

        /// <summary>
        /// Chooses a new bearing to shoot from and restarts the shot clock.
        ///
        /// ⚠️⚠️ THE BEARING IS NOT RANDOM, IT IS CHOSEN AGAINST THE ARENA. A camera dropped on an
        /// arbitrary compass angle spends a third of its shots outside the map looking at the
        /// back of a house facade, because the playable box is 8.6 by 13.0 and the walls are
        /// closer on one axis than the other. Working from the side the ACTION is not on keeps
        /// the shot inside the court and puts the open street behind the subject.
        ///
        /// ⚠️ AND IT ALTERNATES RATHER THAN REPEATS. Two consecutive shots from the same shoulder
        /// read as one long shot with a jump cut in the middle, which is the one edit that always
        /// looks like a mistake.
        /// </summary>
        private void NewShot(bool cut)
        {
            _shotClock = 0.0f;
            _driftSign = -_driftSign;

            Vector3 focus = FocusPoint(out _);

            // Shoot from the outside of the arena looking in: the bearing from the centre of the
            // court out to the focus, swung to one shoulder.
            Vector3 outward = focus;
            outward.y = 0.0f;

            float baseDeg = outward.sqrMagnitude > 0.01f
                ? Mathf.Atan2(outward.x, outward.z) * Mathf.Rad2Deg
                : _bearingDeg;

            _bearingDeg = baseDeg + _driftSign * Random.Range(28.0f, 62.0f);

            if (cut) CutNow();
        }

        /// <summary>
        /// ⚠️ A CUT CLEARS THE SPRINGS AS WELL AS THE POSE. Leaving a velocity in
        /// `Vector3.SmoothDamp` across a teleport makes the camera drift away from its mark for
        /// half a second after the cut, which reads as the shot being wrong rather than as a
        /// cut, and it is the classic way a "snap" ends up looking like a glitch.
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
        /// What the shot is about: the subject, pulled toward whatever it is interacting with.
        /// </summary>
        private Vector3 FocusPoint(out float spread)
        {
            spread = 0.0f;

            var round = GameServices.Round;

            Vector3 primary = _subject != null
                ? _subject.transform.position + Vector3.up * SubjectEyeLine
                : Vector3.up * SubjectEyeLine;

            // ⚠️ THE SUBJECT'S OWN VELOCITY IS THE LEAD, and it is read off the motor rather than
            // differenced here: `CharacterMotor.Velocity` is the resolved value the body actually
            // moved by, so a shove or a lunge is in it and a camera that anticipated a walk does
            // not get left behind by a dash.
            if (_subject != null)
            {
                Vector3 v = _subject.Velocity;
                v.y = 0.0f;
                primary += v * LeadSeconds;
            }

            Vector3 secondary = primary;
            bool hasSecondary = false;

            if (round != null && _subject != null)
            {
                // A retriever is framed with whoever is coming for them; anybody else is framed
                // with the lata, which is the thing every plan in this game is about.
                var taya = DefenderOf(round);

                if (_subject.IsTaggable() && taya != null && taya != _subject)
                {
                    secondary = taya.transform.position + Vector3.up * SubjectEyeLine;
                    hasSecondary = true;
                }
                else if (round.Lata != null)
                {
                    secondary = round.Lata.transform.position + Vector3.up * 0.4f;
                    hasSecondary = true;
                }
            }

            if (!hasSecondary) return primary;

            spread = Flat(primary, secondary);
            return Vector3.Lerp(primary, secondary, SecondaryWeight);
        }

        private void ComputeShot(out Vector3 position, out float yawDeg, out float pitchDeg)
        {
            Vector3 focus = FocusPoint(out float spread);

            // ⚠️ THE PULL-BACK IS THE SPREAD, NOT A CONSTANT. Two subjects nine metres apart
            // need a wider shot than one; a fixed distance frames one of them and crops the
            // other, which on this game means showing the chase without the chaser.
            float distance = Mathf.Clamp(ShotDistanceMin + spread * 0.75f,
                                         ShotDistanceMin, ShotDistanceMax);

            float rad = _bearingDeg * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Sin(rad), 0.0f, Mathf.Cos(rad)) * distance;

            position = focus + offset;
            position.y = ShotHeight + spread * 0.16f;

            // ⚠️⚠️ CLAMPED TO THE PLAYABLE BOX PLUS A MARGIN, AND THIS IS NOT COSMETIC. Both maps
            // are enclosed: Eskinita by house facades and Ilalim ng Tulay by viaduct pillars. A
            // camera allowed to solve for a bearing that puts it outside them spends the shot
            // looking at the inside face of a wall, and on a broadcast that is indistinguishable
            // from the stream having broken. `AIController.PlayableHalfX/Z` are the same numbers
            // the bots are held to, measured off the map's own bounds at load.
            float halfX = AIController.PlayableHalfX + 1.5f;
            float halfZ = AIController.PlayableHalfZ + 1.5f;
            position.x = Mathf.Clamp(position.x, -halfX, halfX);
            position.z = Mathf.Clamp(position.z, -halfZ, halfZ);

            Vector3 toFocus = focus - position;
            if (toFocus.sqrMagnitude < 0.0001f) toFocus = Vector3.forward;

            yawDeg = Mathf.Atan2(toFocus.x, toFocus.z) * Mathf.Rad2Deg;

            float flat = new Vector2(toFocus.x, toFocus.z).magnitude;
            pitchDeg = -Mathf.Atan2(toFocus.y, Mathf.Max(flat, 0.01f)) * Mathf.Rad2Deg;
        }

        // -------------------------------------------------------------------

        private static CharacterMotor DefenderOf(RoundDirector round)
        {
            foreach (var p in round.Players)
                if (p != null && p.IsDefender) return p;

            return null;
        }

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
