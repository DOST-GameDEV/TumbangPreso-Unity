using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// MOMENTUM on his body, in three stages that each announce themselves (owner, 2026-10-26: *"Its benefit and VFX currently
    /// barely register"*, *"add electro VFX and everything else that communicates what is happening on screen better"*).
    ///
    /// * Every stage-up: a crack of sparks at his feet and a gold shock ring that snaps out across the court, so the moment he
    ///   gets faster is readable from anywhere, plus a slight widening of his own lens.
    /// * Stage 1 (0.7 s): sparks strike off both heels on every stride.
    /// * Stage 2 (1.4 s): two gold ribbons trail from his heels, with jagged current running along them.
    /// * Stage 2 also starts the SPEED LANGUAGE (owner, 2026-10-26: *"visually communicate to everyone that zack is getting
    ///   faster ... vfx, animation, dash/blur effect, like the Flash or other fast characters"*): a motion-blur SMEAR of his
    ///   real body, four fading contours stepped back along the route he actually ran (the Backtrack memory shader's trail
    ///   layers), and a crackling LIGHTNING TRAIL at hip height along that route, re-struck every frame.
    /// * Stage 3 (2 s, full): the ribbons thicken, the smear stretches and brightens, three STROBE afterimages hang further
    ///   back on alternate beats (the Flash's stutter), the lightning trail forks, short arcs crawl over his body, embers kick
    ///   off his feet, and speed streaks peel off his shoulders and hips. Reaching full is a SONIC BOOM: a double ring, a
    ///   spark burst and a stretch of his body along the run. In his own first person (where his body is not drawn) the same
    ///   stages show as thin streaks in the edges of the frame, never the centre, a lens that widens per stage and a gold
    ///   edge flash on the boom.
    /// The gait (`GaitStyles.Zack.Dash`) leans further into the dash as his real speed climbs, so the body says it too.
    /// A stop breaks it into sparks; a hit snaps it away.
    ///
    /// ⚠️ NOTHING OF IT IS ON THE WIRE. The peer that moves him reads the kit's own count; every other peer counts the same rule
    /// (`ZackRules.StepMomentum`) from the speed it sees him travel at, so all screens show the same stage within a frame or
    /// two. The gait's dash gear (`GaitStyles.Zack.Dash`) follows his real speed on every peer too. It decides nothing.
    /// </summary>
    public sealed class ZackMomentumLook : MonoBehaviour
    {
        private const int RibbonPoints = 10, Ghosts = 3, Streaks = 4, EyeStreaks = 6, RingPoints = 20;
        private const int RouteKeep = 48, TrailPoints = 16, BodyArcs = 2;
        private static readonly System.Collections.Generic.Dictionary<int, ZackMomentumLook> Live =
            new System.Collections.Generic.Dictionary<int, ZackMomentumLook>();
        private CharacterMotor _motor;
        private ZackHeroKit _kit;
        private LineRenderer _heelA, _heelB, _ribbonL, _ribbonR, _ribbonInkL, _ribbonInkR, _ribbonCurrent, _ring;
        private readonly LineRenderer[] _streaks = new LineRenderer[Streaks];
        private readonly LineRenderer[] _eyeStreaks = new LineRenderer[EyeStreaks];
        private readonly Vector3[] _heelsL = new Vector3[RibbonPoints], _heelsR = new Vector3[RibbonPoints];
        private readonly ZackFx.Echo[] _ghosts = new ZackFx.Echo[Ghosts];
        private float _observed, _stoppedFor, _nextHeel, _age, _nextGhostTry, _ringAge = 10.0f, _lens, _boomAge = 10.0f;
        private float _nextEmber, _nextSmearTry;
        // His hip route on this screen (every frame, newest at _routeHead), for the smear's contours and the lightning trail.
        private readonly Vector3[] _route = new Vector3[RouteKeep];
        private readonly float[] _routeTime = new float[RouteKeep];
        private int _routeHead, _routeCount;
        private LineRenderer _trail, _trailInk, _fork, _boomRing;
        private readonly LineRenderer[] _bodyArcs = new LineRenderer[BodyArcs];
        private ZackFx.Echo _smear;
        private readonly Vector4[] _smearTrail = new Vector4[5];
        private Vector3 _last, _ringAt;
        private int _step;

        /// <summary>The stage this screen draws for him (0 to 3), for traces and the meter.</summary>
        public int Stage => _step;

        /// <summary>Degrees his own first-person lens widens for the current stage (0 elsewhere, and with reduced effects).</summary>
        public static float LensWiden(CharacterMotor motor)
            => motor != null && Live.TryGetValue(motor.PlayerSlot, out var look) && look != null && look._motor == motor
               && !Settings.SettingsStore.Current.ReducedEffects ? look._lens : 0.0f;

        public static ZackMomentumLook Attach(CharacterMotor motor, ZackHeroKit kit)
        {
            if (motor == null) return null;
            var go = new GameObject("ZackMomentum-P" + (motor.PlayerSlot + 1));
            var look = go.AddComponent<ZackMomentumLook>();
            look._motor = motor; look._kit = kit; look._last = motor.transform.position;
            look._heelA = ZackFx.Line(go.transform, "HeelSparkA", 4, 0.024f, ZackFx.Core, 1.6f);
            look._heelB = ZackFx.Line(go.transform, "HeelSparkB", 4, 0.024f, ZackFx.Core, 1.6f);
            look._ribbonInkL = ZackFx.Line(go.transform, "RibbonInkL", RibbonPoints, 0.08f, ZackFx.Ink, 0.0f);
            look._ribbonInkR = ZackFx.Line(go.transform, "RibbonInkR", RibbonPoints, 0.08f, ZackFx.Ink, 0.0f);
            look._ribbonL = ZackFx.Line(go.transform, "RibbonL", RibbonPoints, 0.04f, ZackFx.Body, 1.4f);
            look._ribbonR = ZackFx.Line(go.transform, "RibbonR", RibbonPoints, 0.04f, ZackFx.Body, 1.4f);
            look._ribbonCurrent = ZackFx.Line(go.transform, "RibbonCurrent", 7, 0.018f, ZackFx.Core, 1.8f);
            foreach (var r in new[] { look._ribbonL, look._ribbonR, look._ribbonInkL, look._ribbonInkR })
                r.widthCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, .15f));
            look._ring = ZackFx.Line(go.transform, "StageRing", RingPoints, 0.04f, ZackFx.Core, 1.6f);
            look._ring.loop = true; look._ring.enabled = false;
            for (int i = 0; i < Streaks; i++)
            {
                look._streaks[i] = ZackFx.Line(go.transform, "SpeedStreak" + i, 2, 0.02f, i % 2 == 0 ? ZackFx.Core : ZackFx.Body, 1.6f);
                look._streaks[i].widthCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, .05f));
            }
            for (int i = 0; i < EyeStreaks; i++)
            {
                look._eyeStreaks[i] = ZackFx.Line(go.transform, "EyeStreak" + i, 2, 0.006f, ZackFx.Core, 1.5f);
                look._eyeStreaks[i].widthCurve = new AnimationCurve(new Keyframe(0, .1f), new Keyframe(.5f, 1), new Keyframe(1, .1f));
            }
            for (int i = 0; i < RibbonPoints; i++) look._heelsL[i] = look._heelsR[i] = motor.transform.position;
            look._trailInk = ZackFx.Line(go.transform, "LightningTrailInk", TrailPoints, 0.075f, ZackFx.Ink, 0.0f);
            look._trail = ZackFx.Line(go.transform, "LightningTrail", TrailPoints, 0.035f, ZackFx.Core, 2.0f);
            look._trail.widthCurve = look._trailInk.widthCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, .1f));
            look._fork = ZackFx.Line(go.transform, "LightningFork", 5, 0.02f, ZackFx.Core, 1.9f);
            look._boomRing = ZackFx.Line(go.transform, "SonicBoomRing", RingPoints, 0.06f, ZackFx.Body, 1.8f);
            look._boomRing.loop = true;
            for (int i = 0; i < BodyArcs; i++) look._bodyArcs[i] = ZackFx.Line(go.transform, "BodyArc" + i, 6, 0.016f, ZackFx.Core, 2.0f);
            look._trail.enabled = look._trailInk.enabled = look._fork.enabled = look._boomRing.enabled = false;
            foreach (var arc in look._bodyArcs) arc.enabled = false;
            Live[motor.PlayerSlot] = look;
            return look;
        }

        public bool BelongsTo(CharacterMotor motor) => this != null && _motor == motor;

        /// <summary>Seconds of running this screen counts for him.</summary>
        private float Seconds(float dt)
        {
            var at = _motor.transform.position;
            if (_motor.IsLocallySimulated()) { _last = at; return _kit.MomentumSeconds; }
            var moved = at - _last; moved.y = 0.0f;
            _last = at;
            float speed = dt > 1e-4f ? moved.magnitude / dt : 0.0f;
            // A jump of metres in a frame is a teleport (a Backtrack, a respawn), not a run.
            bool running = speed >= ZackRules.MomentumRunSpeed && speed < 30.0f && !_motor.IsStunned;
            if (speed >= 30.0f && !_kit.Throttled) _observed = 0.0f;
            // Full Throttle's lock runs from the accepted cast on every peer, so every screen jumps to full with it.
            _observed = ZackRules.StepMomentum(_observed, ref _stoppedFor, running, dt, _kit.Throttled);
            return _observed;
        }

        private void LateUpdate()
        {
            if (_motor == null || _kit == null || _motor.AbilitySystem?.Kit != _kit) { Destroy(gameObject); return; }
            float dt = Time.deltaTime;
            _age += dt;
            int step = ZackRules.MomentumStep(Seconds(dt));
            var at = _motor.transform.position;
            bool reduced = Settings.SettingsStore.Current.ReducedEffects;
            bool eyes = ZackFx.InOwnFirstPerson(_motor);
            if (step == 0 && _step >= 2) ZackFx.Sparks(at + Vector3.up * 0.15f, 0.3f, reduced ? 2 : 4, _motor.PlayerSlot);
            if (step > _step)
            {
                // A stage-up: a crack of sparks and a shock ring across the court.
                _ringAge = 0.0f; _ringAt = at + Vector3.up * 0.04f;
                if (!eyes) ZackFx.Sparks(at + Vector3.up * 0.2f, 0.25f + 0.1f * step, reduced ? 1 : 2 + step, _motor.PlayerSlot + step);
            }
            if (step == 3 && _step < 3)
            {
                GameServices.Audio?.PlayAt("sfx_zack_momentum_full", at);
                // The sonic boom: a second, wider ring, a spark burst at his chest and a stretch along the run.
                _boomAge = 0.0f;
                if (!eyes)
                {
                    ZackFx.Sparks(at + Vector3.up * 1.0f, 0.45f, reduced ? 2 : 7, _motor.PlayerSlot + 23);
                    var flat = _motor.transform.forward; flat.y = 0.0f;
                    _motor.GetComponent<CharacterSquashStretch>()?.DashStretch(flat, 0.22f);
                }
                else if (!reduced)
                {
                    var rig = UnityEngine.Camera.main != null ? UnityEngine.Camera.main.GetComponent<CameraRig>() : null;
                    if (rig != null) DamageVignette.Flash(rig, ZackFx.Body, 0.25f);
                }
            }
            _step = step;
            _lens = Mathf.MoveTowards(_lens, step * 1.6f, dt * 12.0f);
            StepRing(dt, reduced);
            RecordRoute(at);
            StepBoom(dt, reduced);
            StepSmear(step, eyes, reduced);
            StepLightningTrail(step, eyes, reduced);
            StepBodyArcs(step, eyes, reduced, at);
            if (step >= 3 && !eyes && !reduced && Time.time >= _nextEmber)
            {
                // Embers kicked off his feet on the stride.
                _nextEmber = Time.time + 0.14f;
                ZackFx.Sparks(at + Vector3.up * 0.06f - _motor.transform.forward * 0.2f, 0.18f, 3, _age * 13.0f);
            }
            var back = -_motor.transform.forward; back.y = 0.0f;
            var side = _motor.transform.right;
            // Stage 1: sparks at the heels, re-striking each stride.
            bool heel = step >= 1 && !eyes && (reduced || Mathf.Repeat(_age, 0.1f) < 0.055f);
            _heelA.enabled = heel; _heelB.enabled = heel;
            if (heel)
            {
                int strike = Mathf.FloorToInt(_age / 0.1f);
                ZackFx.Jag(_heelA, at + side * 0.12f + Vector3.up * 0.03f, at + side * 0.18f + back * (0.3f + 0.1f * step) + Vector3.up * 0.1f, 0.04f, strike);
                ZackFx.Jag(_heelB, at - side * 0.12f + Vector3.up * 0.03f, at - side * 0.18f + back * (0.3f + 0.1f * step) + Vector3.up * 0.1f, 0.04f, strike + 3);
            }
            // Stage 2: two gold ribbons from his heels, current running along them.
            if (Time.time >= _nextHeel)
            {
                _nextHeel = Time.time + 0.028f;
                for (int i = RibbonPoints - 1; i > 0; i--) { _heelsL[i] = _heelsL[i - 1]; _heelsR[i] = _heelsR[i - 1]; }
                _heelsL[0] = at - side * 0.11f; _heelsR[0] = at + side * 0.11f;
            }
            if (step < 2) for (int i = 0; i < RibbonPoints; i++) { _heelsL[i] = at - side * 0.11f; _heelsR[i] = at + side * 0.11f; }
            bool ribbons = step >= 2 && !eyes;
            float width = step >= 3 ? 1.5f : 1.0f;
            for (int i = 0; i < RibbonPoints; i++)
            {
                float lift = 0.08f + 0.02f * i;
                _ribbonL.SetPosition(i, _heelsL[i] + Vector3.up * lift);
                _ribbonR.SetPosition(i, _heelsR[i] + Vector3.up * lift);
                _ribbonInkL.SetPosition(i, _heelsL[i] + Vector3.up * (lift - 0.01f));
                _ribbonInkR.SetPosition(i, _heelsR[i] + Vector3.up * (lift - 0.01f));
            }
            _ribbonL.widthMultiplier = _ribbonR.widthMultiplier = 0.04f * width;
            _ribbonInkL.widthMultiplier = _ribbonInkR.widthMultiplier = 0.08f * width;
            _ribbonL.enabled = _ribbonR.enabled = _ribbonInkL.enabled = _ribbonInkR.enabled = ribbons;
            _ribbonCurrent.enabled = ribbons && !reduced && Mathf.Repeat(_age, 0.08f) < 0.05f;
            if (_ribbonCurrent.enabled)
            {
                bool left = Mathf.FloorToInt(_age / 0.08f) % 2 == 0;
                var trail = left ? _heelsL : _heelsR;
                ZackFx.Jag(_ribbonCurrent, trail[0] + Vector3.up * .09f, trail[RibbonPoints / 2] + Vector3.up * .16f, .05f, Mathf.FloorToInt(_age * 40));
            }
            // Stage 3: ghost frames (unless Overclocked already draws its own) and speed streaks off his shoulders and hips.
            bool full = step >= 3 && !reduced && !eyes;
            bool ghosts = full && !_kit.IsOverclocked;
            for (int i = 0; i < Ghosts; i++)
            {
                if (_ghosts[i] == null && ghosts && Time.time >= _nextGhostTry)
                {
                    _ghosts[i] = ZackFx.Echo.Of(_motor.PlayerSlot, transform);
                    if (_ghosts[i] == null) _nextGhostTry = Time.time + 1.0f;
                }
                var ghost = _ghosts[i];
                if (ghost == null) continue;
                if (ghosts)
                {
                    // Strobes hang further back than the smear (0.1, 0.2, 0.3 s), so the stutter reads apart from the blur.
                    ghost.PoseAt(ghost.Track.Newest - 0.1f * (i + 1));
                    ghost.Past(.55f + .1f * i, .3f - .06f * i);
                }
                ghost.Visible(ghosts && Mathf.Repeat(_age * 12.0f + i, 2.0f) < 1.0f);
            }
            for (int i = 0; i < Streaks; i++)
            {
                var streak = _streaks[i];
                streak.enabled = full && Mathf.Repeat(_age * 9.0f + i * 0.37f, 1.0f) < 0.6f;
                if (!streak.enabled) continue;
                float height = i < 2 ? 0.95f : 0.5f, offset = (i % 2 == 0 ? 1 : -1) * (i < 2 ? 0.26f : 0.2f);
                var from = at + Vector3.up * height + side * offset + back * 0.15f;
                streak.SetPosition(0, from);
                streak.SetPosition(1, from + back * (0.7f + 0.3f * Mathf.Abs(Mathf.Sin(_age * 7 + i))));
            }
            StepEyeStreaks(eyes && step >= 2 && !reduced, step);
        }

        private void RecordRoute(Vector3 at)
        {
            // A teleport (a Backtrack, a respawn) starts the route again so nothing smears across the court.
            if (_routeCount > 0 && (at - _route[_routeHead]).sqrMagnitude > 4.0f) _routeCount = 0;
            _routeHead = (_routeHead + 1) % RouteKeep;
            _route[_routeHead] = at; _routeTime[_routeHead] = Time.time;
            _routeCount = Mathf.Min(_routeCount + 1, RouteKeep);
        }

        /// <summary>Where his feet were <paramref name="ago"/> seconds ago on this screen (the oldest point it holds if less).</summary>
        private Vector3 RouteAgo(float ago)
        {
            if (_routeCount == 0) return _motor.transform.position;
            float target = Time.time - ago;
            for (int n = 0; n < _routeCount; n++)
            {
                int i = (_routeHead - n + RouteKeep) % RouteKeep;
                if (_routeTime[i] > target) continue;
                if (n == 0) return _route[i];
                int newer = (i + 1) % RouteKeep;
                float span = _routeTime[newer] - _routeTime[i];
                return Vector3.Lerp(_route[i], _route[newer], span > 1e-5f ? (target - _routeTime[i]) / span : 0.0f);
            }
            return _route[(_routeHead - _routeCount + 1 + RouteKeep) % RouteKeep];
        }

        /// <summary>
        /// The motion blur: one render-only copy of his real body at his real pose, whose four trail layers are stepped back
        /// along his route and fade with distance. The base layer is hidden (the live body is already there). Stage 2 is a
        /// short soft smear; full is longer and brighter.
        /// </summary>
        private void StepSmear(int step, bool eyes, bool reduced)
        {
            bool on = step >= 2 && !eyes && !reduced;
            if (_smear == null && on && Time.time >= _nextSmearTry)
            {
                _smear = ZackFx.Echo.Of(_motor.PlayerSlot, transform);
                if (_smear == null) _nextSmearTry = Time.time + 1.0f;
                else _smear.Past(.15f, 0.0f);
            }
            if (_smear == null) return;
            _smear.Visible(on);
            if (!on) return;
            _smear.PoseAt(_smear.Track.Newest);
            var now = _motor.transform.position;
            float spacing = step >= 3 ? 0.035f : 0.022f, strength = step >= 3 ? 0.62f : 0.42f;
            _smearTrail[0] = new Vector4(0, 0, 0, 0);
            for (int layer = 1; layer < _smearTrail.Length; layer++)
            {
                var q = RouteAgo(spacing * layer) - now;
                _smearTrail[layer] = new Vector4(q.x, q.y, q.z, strength * Mathf.Pow(.68f, layer - 1));
            }
            _smear.Past(.15f, 1.0f);
            _smear.Trail(_smearTrail);
        }

        /// <summary>The lightning trail along his hip route: re-struck every frame, longer and forking at full.</summary>
        private void StepLightningTrail(int step, bool eyes, bool reduced)
        {
            bool on = step >= 2 && !eyes && _routeCount > 3;
            _trail.enabled = _trailInk.enabled = on;
            _fork.enabled = false;
            if (!on) return;
            float span = step >= 3 ? 0.42f : 0.24f, amplitude = step >= 3 ? 0.09f : 0.06f;
            int seed = reduced ? 0 : Mathf.FloorToInt(_age * 30.0f);
            var side = _motor.transform.right;
            for (int i = 0; i < TrailPoints; i++)
            {
                float t = i / (float)(TrailPoints - 1);
                var p = RouteAgo(span * t) + Vector3.up * (0.75f + 0.12f * Mathf.Sin(t * 9.0f + _age * 4.0f));
                float knee = i == 0 ? 0.0f : ((i % 2 == 0) ? 1 : -1) * amplitude * (0.4f + 0.6f * Mathf.Abs(Mathf.Sin(i * 2.3f + seed)));
                p += side * knee + Vector3.up * knee * 0.6f;
                _trail.SetPosition(i, p); _trailInk.SetPosition(i, p - Vector3.up * 0.01f);
            }
            _trail.widthMultiplier = step >= 3 ? 0.05f : 0.035f;
            _trailInk.widthMultiplier = step >= 3 ? 0.1f : 0.075f;
            // Full: a fork snaps off a point along the trail on alternate beats.
            if (step >= 3 && !reduced && Mathf.Repeat(_age * 10.0f, 1.0f) < 0.5f)
            {
                int at = 4 + Mathf.FloorToInt(Mathf.Repeat(_age * 23.0f, 1.0f) * (TrailPoints - 6));
                var from = _trail.GetPosition(at);
                var to = from + (Mathf.Repeat(_age * 7.0f, 2.0f) < 1.0f ? side : -side) * 0.35f + Vector3.down * 0.45f;
                ZackFx.Jag(_fork, from, to, 0.06f, seed + at);
                _fork.enabled = true;
            }
        }

        /// <summary>Full: short arcs crawl over his body, shoulder to opposite hip, flickering.</summary>
        private void StepBodyArcs(int step, bool eyes, bool reduced, Vector3 at)
        {
            var right = _motor.transform.right;
            for (int i = 0; i < BodyArcs; i++)
            {
                var arc = _bodyArcs[i];
                bool lit = step >= 3 && !eyes && !reduced && Mathf.Repeat(_age * 11.0f + i * 0.45f, 1.0f) < 0.45f;
                arc.enabled = lit;
                if (!lit) continue;
                float s = i == 0 ? 1.0f : -1.0f;
                var from = at + Vector3.up * 1.15f + right * 0.24f * s;
                var to = at + Vector3.up * 0.55f - right * 0.2f * s - _motor.transform.forward * 0.08f;
                ZackFx.Jag(arc, from, to, 0.07f, Mathf.FloorToInt(_age * 40.0f) + i * 3);
            }
        }

        /// <summary>The sonic boom on reaching full: a wider, slower second ring behind the stage ring.</summary>
        private void StepBoom(float dt, bool reduced)
        {
            _boomAge += dt;
            const float life = 0.42f;
            _boomRing.enabled = _boomAge < life && !reduced;
            if (!_boomRing.enabled) return;
            float u = _boomAge / life, radius = Mathf.Lerp(0.5f, 3.2f, 1 - (1 - u) * (1 - u));
            for (int i = 0; i < RingPoints; i++)
            {
                float a = i / (float)RingPoints * Mathf.PI * 2.0f;
                float knee = ((i % 2 == 0) ? 1 : -1) * 0.05f;
                _boomRing.SetPosition(i, _ringAt + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (radius + knee));
            }
            ZackFx.Tint(_boomRing, new Color(ZackFx.Body.r, ZackFx.Body.g, ZackFx.Body.b, 1 - u));
        }

        private void StepRing(float dt, bool reduced)
        {
            _ringAge += dt;
            const float life = 0.28f;
            _ring.enabled = _ringAge < life && !reduced;
            if (!_ring.enabled) return;
            float u = _ringAge / life, radius = Mathf.Lerp(0.3f, 1.1f + 0.2f * _step, 1 - (1 - u) * (1 - u));
            for (int i = 0; i < RingPoints; i++)
            {
                float a = i / (float)RingPoints * Mathf.PI * 2.0f;
                float knee = ((i % 2 == 0) ? 1 : -1) * 0.03f;
                _ring.SetPosition(i, _ringAt + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (radius + knee));
            }
            ZackFx.Tint(_ring, new Color(ZackFx.Core.r, ZackFx.Core.g, ZackFx.Core.b, 1 - u));
        }

        /// <summary>His own first person: thin streaks rushing past in the outer ring of the frame, more and longer at full.</summary>
        private void StepEyeStreaks(bool on, int step)
        {
            var cam = UnityEngine.Camera.main;
            for (int i = 0; i < EyeStreaks; i++)
            {
                var streak = _eyeStreaks[i];
                bool lit = on && cam != null && (step >= 3 || i % 2 == 0) && Mathf.Repeat(_age * 6.0f + i * 0.29f, 1.0f) < 0.45f;
                streak.enabled = lit;
                if (!lit) continue;
                var t = cam.transform;
                int beat = Mathf.FloorToInt(_age * 6.0f + i * 0.29f);
                float angle = (i * 1.047f + Mathf.Sin(beat * 2.3f + i) * 0.35f);
                // Keep to the outer ring of the frame (rule 3): never within the middle 60% of the view.
                var offset = t.right * Mathf.Cos(angle) * 1.15f + t.up * Mathf.Sin(angle) * 0.7f;
                float phase = Mathf.Repeat(_age * 6.0f + i * 0.29f, 1.0f) / 0.45f;
                var from = t.position + t.forward * Mathf.Lerp(2.4f, 0.9f, phase) + offset;
                streak.SetPosition(0, from);
                streak.SetPosition(1, from - t.forward * (step >= 3 ? 0.9f : 0.55f));
                ZackFx.Tint(streak, new Color(ZackFx.Core.r, ZackFx.Core.g, ZackFx.Core.b, 0.55f * Mathf.Sin(Mathf.PI * phase)));
            }
        }

        private void OnDestroy()
        {
            foreach (var ghost in _ghosts) ghost?.Destroy();
            _smear?.Destroy();
            if (_motor != null && Live.TryGetValue(_motor.PlayerSlot, out var look) && look == this) Live.Remove(_motor.PlayerSlot);
        }
    }
}
