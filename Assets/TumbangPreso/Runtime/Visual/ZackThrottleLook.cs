using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// FULL THROTTLE on his body (owner, 2026-10-26: *"i approve full throttle"*, then *"animation and vfx when that skill is
    /// casted? make it look like he's recharged"*). The read is a battery snapping to full.
    ///
    /// * The surge (on the cast, 0.5 s): three bolts strike down out of the air into his head and shoulders, a gold charge band
    ///   sweeps up his body from his feet to the crown like a meter filling, two shock rings snap out across the court one
    ///   after the other, and a spark burst cracks off his chest when the band reaches the top. Momentum's own stage ring and
    ///   heel ribbons arrive with it, because the passive jumps straight to full.
    /// * The lock (3 s): current orbits his waist and chest in two short jagged arcs, and a gold ring on the court under his
    ///   feet runs down like a clock face. Everyone can read how long he cannot be slowed down.
    /// * The end: a fizzle of sparks; broken early by a hit, stun, trip or root, a bigger short-out.
    ///
    /// In his own first person the body effects are held back (look rule 3); he sees the bolts land in front of him, a gold
    /// wash at the edges of the frame (`DamageVignette`, his own accent), current crackling between his clenched fists
    /// (`ViewmodelArms` "throttle-rev"), the countdown ring at his feet and the HUD's FULL THROTTLE caption.
    ///
    /// PAINTED (2026-10-29, the owner: "its pretty poorly made so eventually we'll need to do a vfx revamp again"). With the
    /// switch on, the same beats are drawn by `ZackThrottlePaint` in the approved Zack pieces (z4's bolt and strike, b13d's
    /// spark bursts, z4's amber streak for the clock) on prebuilt meshes, instead of the line renderers below rewritten every
    /// frame. The vignette and the fist crackle in his own first person are the same either way.
    ///
    /// ⚠️ PRESENTATION ONLY. It reads the kit's lock (`ZackHeroKit.Throttled`), which every peer runs from the accepted cast and
    /// a joiner restores from the timed-kit snapshot. It decides nothing.
    /// </summary>
    public sealed class ZackThrottleLook : MonoBehaviour
    {
        /// <summary>
        /// ⚠️ THE SWITCH. On, Full Throttle is painted (`ZackThrottlePaint`). Off, it is the rework's line renderers. Read when
        /// the look is attached to his body. TO GO BACK, MAKE THE LINE BELOW `false`.
        /// </summary>
        public static bool Painted = true;

        private const int Bolts = 3, Orbits = 2, ClockPoints = 32, RingPoints = 24;
        private const float SurgeLife = 0.5f, BandLife = 0.32f, BoltLife = 0.22f;
        private CharacterMotor _motor;
        private ZackHeroKit _kit;
        private readonly LineRenderer[] _bolts = new LineRenderer[Bolts], _boltInk = new LineRenderer[Bolts];
        private readonly LineRenderer[] _orbits = new LineRenderer[Orbits];
        private LineRenderer _band, _bandInk, _clock, _clockInk, _ringA, _ringB, _fists;
        private ViewmodelArms _arms;
        private float _surgeAge = 10.0f, _age, _endedAt = -100.0f;
        private bool _wasLocked, _sparked, _endBroken;
        private Vector3 _surgeAt;
        // The body this frame: his motor's in a match, a film's stand-in when staged.
        private Transform _body;
        private Vector3 _at, _fwd, _right;
        private bool _locked;
        private float _left;
        private int _seed;
        private bool _painted, _staged;
        private ZackThrottlePaint _paint;

        /// <summary>Whether this look draws the painted pieces (the switch as it stood when it was attached).</summary>
        public bool DrawnPainted => _painted;

        public static ZackThrottleLook Attach(CharacterMotor motor, ZackHeroKit kit)
        {
            if (motor == null) return null;
            var look = Make("ZackThrottle-P" + (motor.PlayerSlot + 1), null, Painted);
            look._motor = motor; look._kit = kit; look._body = motor.transform; look._seed = motor.PlayerSlot;
            return look;
        }

        /// <summary>
        /// FOR A FILM MADE OUTSIDE PLAY: the look on a stand-in `body` with no motor and no kit, the lock told to it by
        /// `StagedStep`. Nothing frees itself; destroy its object when done.
        /// </summary>
        public static ZackThrottleLook Stage(Transform parent, Transform body, bool painted)
        {
            var look = Make("ZackThrottle-staged", parent, painted);
            look._staged = true; look._body = body;
            return look;
        }

        private static ZackThrottleLook Make(string name, Transform parent, bool painted)
        {
            var go = new GameObject(name);
            if (parent != null) { go.hideFlags = HideFlags.DontSave; go.transform.SetParent(parent, false); }
            var look = go.AddComponent<ZackThrottleLook>();
            look._painted = painted;
            var t = go.transform;
            if (painted) look._paint = new ZackThrottlePaint(t);
            else
            {
                for (int i = 0; i < Bolts; i++)
                {
                    look._boltInk[i] = ZackFx.Line(t, "SurgeBoltInk" + i, 9, 0.075f, ZackFx.Ink, 0.0f);
                    look._bolts[i] = ZackFx.Line(t, "SurgeBolt" + i, 9, 0.05f, ZackFx.Core, 2.2f);
                    look._bolts[i].widthCurve = look._boltInk[i].widthCurve =
                        new AnimationCurve(new Keyframe(0, .35f), new Keyframe(.85f, 1), new Keyframe(1, .6f));
                }
                look._bandInk = ZackFx.Line(t, "ChargeBandInk", RingPoints, 0.09f, ZackFx.Ink, 0.0f);
                look._band = ZackFx.Line(t, "ChargeBand", RingPoints, 0.05f, ZackFx.Body, 2.0f);
                look._bandInk.loop = look._band.loop = true;
                look._ringA = ZackFx.Line(t, "SurgeRingA", RingPoints, 0.07f, ZackFx.Core, 1.8f);
                look._ringB = ZackFx.Line(t, "SurgeRingB", RingPoints, 0.045f, ZackFx.Body, 1.6f);
                look._ringA.loop = look._ringB.loop = true;
                for (int i = 0; i < Orbits; i++)
                    look._orbits[i] = ZackFx.Line(t, "LockOrbit" + i, 7, 0.022f, ZackFx.Core, 1.8f);
                look._clockInk = ZackFx.Line(t, "LockClockInk", ClockPoints, 0.075f, ZackFx.Ink, 0.0f);
                look._clock = ZackFx.Line(t, "LockClock", ClockPoints, 0.04f, ZackFx.Body, 1.6f);
            }
            look._fists = ZackFx.Line(t, "FistCrackle", 6, 0.006f, ZackFx.Core, 2.0f);
            look.HideAll();
            return look;
        }

        public bool BelongsTo(CharacterMotor motor) => this != null && _motor == motor;

        /// <summary>The cast landed on this screen: play the surge from here.</summary>
        public void Surge()
        {
            if (_body == null) return;
            _surgeAge = 0.0f; _sparked = false;
            _surgeAt = _body.position;
            if (_motor != null && ZackFx.InOwnFirstPerson(_motor) && !Settings.SettingsStore.Current.ReducedEffects)
            {
                var cam = UnityEngine.Camera.main;
                var rig = cam != null ? cam.GetComponent<CameraRig>() : null;
                if (rig != null) DamageVignette.Flash(rig, ZackFx.Body, 0.35f);
            }
        }

        private void HideAll()
        {
            if (!_painted)
            {
                foreach (var l in _bolts) l.enabled = false;
                foreach (var l in _boltInk) l.enabled = false;
                foreach (var l in _orbits) l.enabled = false;
                _band.enabled = _bandInk.enabled = _clock.enabled = _clockInk.enabled = _ringA.enabled = _ringB.enabled = false;
            }
            else _paint?.Hide();
            _fists.enabled = false;
        }

        private void LateUpdate()
        {
            if (_staged) return;
            if (_motor == null || _kit == null || _motor.AbilitySystem?.Kit != _kit) { Destroy(gameObject); return; }
            float dt = Time.deltaTime;
            _age += dt; _surgeAge += dt;
            bool broken = _motor.IsStunned || _motor.IsTripped || _motor.IsRooted || _motor.IsTagged;
            Advance(_kit.Throttled, _kit.ThrottleLeft, broken, ZackFx.InOwnFirstPerson(_motor), Settings.SettingsStore.Current.ReducedEffects);
        }

        /// <summary>
        /// A staged look at `seconds` on the film's clock, cast at `castAt` (below 0 for never), the lock running its rule's
        /// 3 s unbroken. Posed from the clock alone, so the same moment can be posed again for another view.
        /// </summary>
        public void StagedStep(float seconds, float castAt, bool eyes)
        {
            if (!_staged || _body == null) return;
            _age = seconds;
            bool cast = castAt >= 0.0f && seconds >= castAt;
            _surgeAge = cast ? seconds - castAt : 10.0f;
            if (cast) _surgeAt = PositionAt(castAt);
            _sparked = _surgeAge >= BandLife;
            float endsAt = castAt + ZackRules.FullThrottleSeconds;
            bool locked = cast && seconds < endsAt;
            _wasLocked = locked;
            _endedAt = cast && !locked ? endsAt : -100.0f; _endBroken = false;
            Advance(locked, locked ? endsAt - seconds : 0.0f, false, eyes, false);
        }

        /// <summary>A film's: where its stand-in stood at `seconds` (the film moves it; this is told the moment of the cast).</summary>
        public System.Func<float, Vector3> StagedPath;
        private Vector3 PositionAt(float seconds) => StagedPath != null ? StagedPath(seconds) : _body.position;

        private void Advance(bool locked, float left, bool broken, bool eyes, bool reduced)
        {
            _at = _body.position; _fwd = _body.forward; _right = _body.right;
            _locked = locked; _left = left;
            if (_wasLocked && !locked)
            {
                _endedAt = _age; _endBroken = broken;
                if (!_painted && !_staged)
                    ZackFx.Sparks(_at + Vector3.up * (broken ? 0.9f : 0.3f), broken ? 0.45f : 0.25f, reduced ? 2 : broken ? 7 : 4,
                                  _seed * 3 + _age);
            }
            _wasLocked = locked;
            if (_painted)
            {
                float share = locked ? Mathf.Clamp01(left / ZackRules.FullThrottleSeconds) : 0.0f;
                _paint.Pose(_surgeAge, _age, _at, _fwd, _right, _surgeAt, eyes, reduced, share, _endedAt > -50.0f ? _age - _endedAt : -1.0f, _endBroken);
                StepFists(eyes);
                return;
            }
            StepSurge(eyes, reduced);
            StepLock(eyes, reduced);
        }

        // His own eyes: while the fists are clenched at his hips, current jumps between them (both looks).
        private void StepFists(bool eyes)
        {
            bool live = _surgeAge < SurgeLife;
            bool fists = live && eyes && !_staged && _surgeAge < 0.24f && Mathf.Repeat(_surgeAge * 40.0f, 2.0f) < 1.3f;
            if (fists)
            {
                if (_arms == null) _arms = Object.FindFirstObjectByType<ViewmodelArms>();
                fists = _arms != null && _arms.TryZackHandTips(out var lead, out var rear) && Draw(lead, rear);
                bool Draw(Vector3 a, Vector3 c) { ZackFx.Jag(_fists, a, c, 0.025f, Mathf.FloorToInt(_surgeAge * 50.0f)); return true; }
            }
            _fists.enabled = fists;
        }

        private void StepSurge(bool eyes, bool reduced)
        {
            float u = _surgeAge / SurgeLife;
            bool live = u < 1.0f;
            // The bolts: strike down from 3.5 m into his crown and both shoulders, flickering on alternate frames.
            for (int i = 0; i < Bolts; i++)
            {
                float start = i * 0.05f, local = (_surgeAge - start) / BoltLife;
                bool lit = live && local >= 0.0f && local < 1.0f && (reduced || Mathf.Repeat(_surgeAge * 30.0f + i, 2.0f) < 1.4f);
                _bolts[i].enabled = _boltInk[i].enabled = lit;
                if (!lit) continue;
                var side = _right * (i == 0 ? 0.0f : i == 1 ? -0.28f : 0.28f);
                var hit = _at + side + Vector3.up * (i == 0 ? 1.55f : 1.25f);
                var from = hit + Vector3.up * 3.5f + side * 1.6f + _fwd * (0.4f - 0.3f * i);
                // In his own first person the bolts land a little ahead of him so he sees them strike.
                if (eyes) { hit += _fwd * 1.1f; from += _fwd * 1.1f; }
                int seed = Mathf.FloorToInt(_surgeAge * 40.0f) + i * 7;
                ZackFx.Jag(_boltInk[i], from, hit, 0.22f, seed);
                ZackFx.Jag(_bolts[i], from, hit, 0.22f, seed);
                float fade = 1.0f - local;
                ZackFx.Tint(_bolts[i], new Color(ZackFx.Core.r, ZackFx.Core.g, ZackFx.Core.b, 0.4f + 0.6f * fade));
                ZackFx.Tint(_boltInk[i], new Color(ZackFx.Ink.r, ZackFx.Ink.g, ZackFx.Ink.b, 0.22f + 0.20f * fade));
            }
            // The charge band: a ring around him that climbs from his feet to the crown, the meter filling.
            float b = _surgeAge / BandLife;
            bool band = live && b < 1.0f && !eyes;
            _band.enabled = _bandInk.enabled = band;
            if (band)
            {
                float eased = 1.0f - (1.0f - b) * (1.0f - b);
                float height = Mathf.Lerp(0.05f, 1.75f, eased), radius = Mathf.Lerp(0.55f, 0.38f, eased);
                var centre = _at + Vector3.up * height;
                for (int i = 0; i < RingPoints; i++)
                {
                    float a = i / (float)RingPoints * Mathf.PI * 2.0f;
                    float knee = (i % 2 == 0 ? 1 : -1) * 0.025f;
                    var p = centre + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (radius + knee);
                    _band.SetPosition(i, p); _bandInk.SetPosition(i, p - Vector3.up * 0.01f);
                }
            }
            if (live && !_sparked && b >= 1.0f)
            {
                _sparked = true;
                if (!eyes && !_staged) ZackFx.Sparks(_at + Vector3.up * 1.3f, 0.4f, reduced ? 2 : 6, _seed + 11);
            }
            StepFists(eyes);
            // Two shock rings across the court, the second chasing the first.
            Ring(_ringA, _surgeAge / 0.34f, 2.6f, reduced);
            Ring(_ringB, (_surgeAge - 0.1f) / 0.34f, 1.9f, reduced);
        }

        private void Ring(LineRenderer ring, float u, float reach, bool reduced)
        {
            ring.enabled = u >= 0.0f && u < 1.0f && !reduced;
            if (!ring.enabled) return;
            float radius = Mathf.Lerp(0.35f, reach, 1.0f - (1.0f - u) * (1.0f - u));
            for (int i = 0; i < RingPoints; i++)
            {
                float a = i / (float)RingPoints * Mathf.PI * 2.0f;
                float knee = (i % 2 == 0 ? 1 : -1) * 0.04f;
                ring.SetPosition(i, _surgeAt + Vector3.up * 0.05f + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (radius + knee));
            }
            ZackFx.Tint(ring, new Color(ZackFx.Core.r, ZackFx.Core.g, ZackFx.Core.b, 1.0f - u));
        }

        private void StepLock(bool eyes, bool reduced)
        {
            // The countdown ring: the share of the lock left, drawn as an arc round his feet from twelve o'clock.
            float left = _locked ? Mathf.Clamp01(_left / ZackRules.FullThrottleSeconds) : 0.0f;
            bool clock = _locked && left > 0.0f;
            _clock.enabled = _clockInk.enabled = clock;
            if (clock)
            {
                var fwd = _fwd; fwd.y = 0.0f;
                float yaw0 = Mathf.Atan2(fwd.x, fwd.z);
                // The last second blinks so the end never surprises him.
                bool blink = left < 1.0f / ZackRules.FullThrottleSeconds && Mathf.Repeat(_age * 8.0f, 1.0f) < 0.35f;
                _clock.enabled = _clockInk.enabled = !blink;
                for (int i = 0; i < ClockPoints; i++)
                {
                    float a = yaw0 + (i / (float)(ClockPoints - 1)) * left * Mathf.PI * 2.0f;
                    var p = _at + Vector3.up * 0.04f + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * 0.72f;
                    _clock.SetPosition(i, p); _clockInk.SetPosition(i, p - Vector3.up * 0.005f);
                }
            }
            // Current orbiting his waist and chest.
            for (int i = 0; i < Orbits; i++)
            {
                var orbit = _orbits[i];
                bool lit = _locked && !eyes && (reduced ? i == 0 : Mathf.Repeat(_age * 14.0f + i * 0.5f, 1.0f) < 0.7f);
                orbit.enabled = lit;
                if (!lit) continue;
                float height = i == 0 ? 0.75f : 1.15f, spin = _age * (i == 0 ? 9.0f : -11.0f);
                int seed = Mathf.FloorToInt(_age * 30.0f) + i * 5, n = orbit.positionCount;
                for (int k = 0; k < n; k++)
                {
                    // A jagged arc a quarter of the way round him: the knees jump in and out and up and down.
                    float a = spin + 1.6f * k / (n - 1);
                    float knee = k == 0 || k == n - 1 ? 0.0f : (k % 2 == 0 ? 1 : -1) * 0.07f * (0.5f + 0.5f * Mathf.Abs(Mathf.Sin(k * 2.7f + seed)));
                    orbit.SetPosition(k, _at + Vector3.up * (height + knee * 0.6f)
                                         + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (0.42f + knee));
                }
            }
        }
    }
}
