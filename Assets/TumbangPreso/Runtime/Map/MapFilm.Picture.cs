using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Map
{
    public abstract partial class MapFilm
    {
        // ------------------------------------------------------------------ the camera

        private Vector3 _eye, _aim;
        private float _fov = 50.0f, _roll;
        private bool _shot;
        private float _punch, _punchAt = -99.0f;
        private const float PunchDecay = 6.0f, ShakeDegrees = 0.6f;

        /// <summary>This frame's shot: the camera at `eye` looking at `aim` with `fov`, and a `roll` (Dutch) in degrees.</summary>
        protected void Shoot(Vector3 eye, Vector3 aim, float fov, float roll = 0.0f)
        {
            _eye = eye; _fov = fov; _roll = roll; _shot = true;
            _aim = (aim - eye).sqrMagnitude > 1e-4f ? aim : eye + Vector3.forward;
        }

        /// <summary>A jolt of the picture (an impact, a slam): a few tenths of a degree, by the player's setting. Never
        /// under reduced motion.</summary>
        protected void Punch(float strength)
        {
            if (Still) return;
            float left = _punch * Mathf.Exp(-(Time.unscaledTime - _punchAt) * PunchDecay);
            if (strength < left) return;
            _punch = strength; _punchAt = Time.unscaledTime;
        }

        /// <summary>The farthest the eye can be along the line from `focus` toward `desired` without a wall between.</summary>
        protected Vector3 ClearEye(Vector3 focus, Vector3 desired)
        {
            Vector3 ray = desired - focus;
            float distance = ray.magnitude;
            if (distance < 0.01f) return desired;
            int count = Physics.SphereCastNonAlloc(focus, 0.2f, ray / distance, _hits, distance, ~0, QueryTriggerInteraction.Ignore);
            float clear = distance;
            for (int i = 0; i < count; i++)
                if (World(_hits[i].collider)) clear = Mathf.Min(clear, Mathf.Max(0.01f, _hits[i].distance - 0.3f));
            return focus + ray / distance * clear;
        }

        /// <summary>
        /// A seat framed from its drawn bounds: `fill` of the frame's height, from the front three-quarter that leaves
        /// the subject clearest of everyone else, pushed in a little over `local` (0 to 1). Each candidate view is
        /// scored by how much of the frame the other bodies' drawn boxes cover and how much of the subject they hide;
        /// a body nearer the lens than the subject counts several times over (film r2: a neighbour's face filled the
        /// foreground of the YOU shot).
        /// </summary>
        protected void FrameSeat(int seat, float fill, float local, out Vector3 eye, out Vector3 aim, out float fov)
        {
            fov = 38.0f;
            var drawn = Drawn(seat);
            aim = drawn.center;
            float tangent = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            float aspect = Camera != null ? Mathf.Max(0.5f, Camera.aspect) : 1.78f;
            float across = Mathf.Max(drawn.extents.x, drawn.extents.z);
            float distance = Mathf.Max(drawn.extents.y / (fill * tangent), across / (0.82f * tangent * aspect)) + across;
            Vector3 front = Body(seat) != null ? Body(seat).transform.forward : Vector3.forward; front.y = 0.0f;
            if (front.sqrMagnitude < 0.0001f) front = Vector3.forward;
            front.Normalize();
            var others = new System.Collections.Generic.List<Bounds>();
            for (int other = 0; other < Seats; other++) if (other != seat && Body(other) != null) others.Add(Drawn(other));
            float best = float.NegativeInfinity;
            eye = aim + front * distance;
            for (float pitch = 6.0f; pitch <= 22.0f; pitch += 8.0f)
                for (float yaw = -70.0f; yaw <= 70.0f; yaw += 10.0f)
                {
                    Vector3 away = Quaternion.Euler(0.0f, yaw, 0.0f) * front;
                    away = away * Mathf.Cos(pitch * Mathf.Deg2Rad) + Vector3.up * Mathf.Sin(pitch * Mathf.Deg2Rad);
                    Vector3 candidate = ClearEye(aim, aim + away * distance);
                    var turn = Quaternion.LookRotation(aim - candidate);
                    Rect subject = ScreenBox(drawn, candidate, turn, tangent, aspect, out float subjectNear);
                    float cover = 0.0f;
                    foreach (var o in others)
                    {
                        Rect r = ScreenBox(o, candidate, turn, tangent, aspect, out float near);
                        if (r.width <= 0.0f) continue;
                        float inFrame = Overlap(r, new Rect(-1, -1, 2, 2));
                        float onSubject = Overlap(r, subject);
                        bool nearer = near < subjectNear;
                        cover += inFrame * (nearer ? 3.0f : 0.6f) + onSubject * (nearer ? 6.0f : 0.4f);
                    }
                    // A view pulled in by a wall loses a little; a body in front of the subject loses a lot (film r3:
                    // the clearance term won over a neighbour's face filling the YOU shot).
                    float clear = Vector3.Distance(candidate, aim) / distance;
                    if (clear < 0.6f) continue;
                    float score = clear * 3.0f - cover * 8.0f - Mathf.Abs(yaw - 10.0f) * 0.004f - (pitch - 6.0f) * 0.015f;
                    if (score > best) { best = score; eye = candidate; }
                }
            eye = Vector3.Lerp(eye, aim, 0.04f * Smooth(local));
        }

        private readonly System.Collections.Generic.Dictionary<string, Vector3[]> _framed = new System.Collections.Generic.Dictionary<string, Vector3[]>();

        /// <summary>`FrameSeat`, chosen once per `key` (a shot) and then carried with the subject as it moves, so the
        /// camera never hops between candidate views mid-shot.</summary>
        protected void Framed(string key, int seat, float fill, float local, out Vector3 eye, out Vector3 aim, out float fov)
        {
            Vector3 now = Drawn(seat).center;
            if (!_framed.TryGetValue(key, out var f))
            {
                FrameSeat(seat, fill, 0.0f, out var e, out var a, out var v);
                f = new[] { e, a, new Vector3(v, 0, 0) };
                _framed[key] = f;
            }
            aim = now; eye = f[0] + (now - f[1]); fov = f[2].x;
            eye = Vector3.Lerp(eye, aim, 0.05f * Smooth(local));
        }

        /// <summary>A world box's extent on a view's screen, -1 to 1 each way (zero size when behind), and its nearest depth.</summary>
        private static Rect ScreenBox(Bounds box, Vector3 eye, Quaternion turn, float tangent, float aspect, out float nearest)
        {
            var inverse = Quaternion.Inverse(turn);
            float x0 = float.PositiveInfinity, x1 = float.NegativeInfinity, y0 = float.PositiveInfinity, y1 = float.NegativeInfinity;
            nearest = float.PositiveInfinity;
            for (int c = 0; c < 8; c++)
            {
                Vector3 p = box.center + Vector3.Scale(box.extents, new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1));
                Vector3 v = inverse * (p - eye);
                if (v.z < 0.1f) { nearest = Mathf.Min(nearest, 0.1f); continue; }
                nearest = Mathf.Min(nearest, v.z);
                float x = v.x / (v.z * tangent * aspect), y = v.y / (v.z * tangent);
                x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x); y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y);
            }
            return float.IsInfinity(x0) ? new Rect(0, 0, 0, 0) : new Rect(x0, y0, x1 - x0, y1 - y0);
        }

        private static float Overlap(Rect a, Rect b)
        {
            float w = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin), h = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            return w > 0.0f && h > 0.0f ? w * h / 4.0f : 0.0f;
        }

        // ------------------------------------------------------------------ the close

        private bool _ownFramed;
        private Vector3 _ownEye, _ownAim;
        private float _ownFov;

        private void Close(float age)
        {
            var t = Timeline;
            if (age < t.Close) return;
            float local = Mathf.Clamp01((age - t.Close) / OwnSeconds);
            for (int s = 0; s < Seats; s++) { Home(s); ShowSeat(s, true); }
            if (OwnClose)
            {
                // This player's own shot, framed once from where they stand for the round.
                if (!_ownFramed) { FrameSeat(ViewerSeat, 0.78f, 0.0f, out _ownEye, out _ownAim, out _ownFov); _ownFramed = true; }
                Shoot(Vector3.Lerp(_ownEye, _ownAim, 0.05f * Smooth(local)), _ownAim, _ownFov);
                for (int s = 0; s < Seats; s++) { Greet(s, s == ViewerSeat && !Still ? 0.75f * Span(local, 0.0f, 0.3f) * (1.0f - Span(age, t.Handoff, t.End)) : 0.001f); Animate(s, false); }
            }
            else
            {
                TayaShot(local, out var eye, out var aim, out var fov);
                Shoot(eye, aim, fov);
                for (int s = 0; s < Seats; s++) { Greet(s, s == Taya && !Still ? 0.75f * (1.0f - Span(age, t.Handoff, t.End)) : 0.001f); Animate(s, false); }
            }
        }

        private void Picture(float age)
        {
            if (!_shot) Shoot(_homePosition + _homeRotation * Vector3.back * 0.01f, _homePosition + _homeRotation * Vector3.forward, _homeFov);
            var t = Timeline;
            Quaternion turn = Quaternion.LookRotation(_aim - _eye, Vector3.up) * Quaternion.Euler(0.0f, 0.0f, _roll);
            Vector3 eye = _eye;
            float fov = _fov;

            float level = Settings.SettingsStore.Current.EffectiveCameraShake;
            float ring = _punch * Mathf.Exp(-(Time.unscaledTime - _punchAt) * PunchDecay);
            if (!Still && level > 0.0f && ring > 0.01f && age < t.Handoff)
            {
                float clock = Time.unscaledTime, by = ShakeDegrees * ring * level;
                turn *= Quaternion.Euler(Mathf.Sin(clock * 61.3f) * by, Mathf.Sin(clock * 47.9f + 1.3f) * by, Mathf.Sin(clock * 53.1f + 0.5f) * by * 0.5f);
            }

            // One deliberate return to this player's own view; under reduced motion a cut at its middle.
            if (age >= t.Handoff)
            {
                float home = Smooth((age - t.Handoff) / (t.End - t.Handoff));
                if (Still) home = home >= 0.5f ? 1.0f : 0.0f;
                eye = Vector3.Lerp(eye, _homePosition, home);
                turn = Quaternion.Slerp(turn, _homeRotation, home);
                fov = Mathf.Lerp(fov, _homeFov, home);
            }
            Camera.transform.SetPositionAndRotation(eye, turn);
            Camera.fieldOfView = fov;
        }

        // ------------------------------------------------------------------ the light
        //
        // A film may take the map to dusk, night, golden hour or dawn for its length. Everything it may touch is read
        // once under the curtain and written back by `End`: the ambient trilight, the fog, the sky's own colours,
        // the key light, every other light it dims, and the camera's event grade.

        /// <summary>A time of day for the film: what the map's look is blended toward.</summary>
        protected struct Mood
        {
            public Color Sky, Equator, Ground, Fog, Zenith, Horizon, Sun;
            public float FogStart, FogEnd, SunIntensity, SunElevation, SunYaw, Brightness, Saturation, Lamps;
            public bool KeepYaw;
        }

        private Light _sun;
        private Color _sunColour; private float _sunIntensity, _sunShadow; private Quaternion _sunTurn;
        private UnityEngine.Rendering.AmbientMode _ambientMode;
        private Color _ambientSky, _ambientEquator, _ambientGround, _fogColour;
        private bool _fogOn; private FogMode _fogMode; private float _fogStart, _fogEnd;
        private Material _sky;
        private readonly string[] _skyIds = { "_Zenith", "_Horizon", "_SunColor", "_CloudLight", "_CloudShade" };
        private readonly Color[] _skyWas = new Color[5];
        private readonly bool[] _skyHas = new bool[5];
        private ColourGrade _grade;
        private float _gradeBrightness = 1.0f, _gradeSaturation = 1.0f;
        private Light[] _sceneLamps = new Light[0];
        private float[] _sceneLampWas = new float[0];
        private bool _lightTaken;

        /// <summary>The map's look as it was when the film began.</summary>
        protected Mood Authored { get; private set; }

        private void SnapshotLight()
        {
            var look = WorldLookPresentation.Current;
            _sun = look != null && look.KeyLight != null ? look.KeyLight : RenderSettings.sun;
            if (_sun == null)
                foreach (var l in FindObjectsByType<Light>())
                    if (l.type == LightType.Directional && l.isActiveAndEnabled && l.transform.position.y > -100.0f && (_sun == null || l.intensity > _sun.intensity)) _sun = l;
            if (_sun != null) { _sunColour = _sun.color; _sunIntensity = _sun.intensity; _sunShadow = _sun.shadowStrength; _sunTurn = _sun.transform.rotation; }
            _ambientMode = RenderSettings.ambientMode;
            _ambientSky = RenderSettings.ambientSkyColor; _ambientEquator = RenderSettings.ambientEquatorColor; _ambientGround = RenderSettings.ambientGroundColor;
            _fogOn = RenderSettings.fog; _fogMode = RenderSettings.fogMode; _fogStart = RenderSettings.fogStartDistance; _fogEnd = RenderSettings.fogEndDistance; _fogColour = RenderSettings.fogColor;
            _sky = RenderSettings.skybox;
            for (int i = 0; i < _skyIds.Length; i++)
            {
                _skyHas[i] = _sky != null && _sky.HasProperty(_skyIds[i]);
                if (_skyHas[i]) _skyWas[i] = _sky.GetColor(_skyIds[i]);
            }
            _grade = Camera != null ? Camera.GetComponent<ColourGrade>() : null;
            if (_grade != null) { _gradeBrightness = _grade.RecordedBrightness; _gradeSaturation = _grade.RecordedSaturation; }
            // The map's own lamps (street lights, window glows), dimmed for a brighter hour and brought up for night.
            var lamps = new System.Collections.Generic.List<Light>();
            foreach (var l in FindObjectsByType<Light>())
                if (l != null && l.type != LightType.Directional && l.transform.root != transform && l.isActiveAndEnabled) lamps.Add(l);
            _sceneLamps = lamps.ToArray();
            _sceneLampWas = new float[_sceneLamps.Length];
            for (int i = 0; i < _sceneLamps.Length; i++) _sceneLampWas[i] = _sceneLamps[i].intensity;

            Vector3 forward = _sun != null ? _sunTurn * Vector3.forward : Vector3.down;
            Authored = new Mood
            {
                Sky = _ambientSky, Equator = _ambientEquator, Ground = _ambientGround, Fog = _fogColour,
                Zenith = _skyHas[0] ? _skyWas[0] : Color.gray, Horizon = _skyHas[1] ? _skyWas[1] : Color.gray,
                Sun = _sun != null ? _sunColour : Color.white, SunIntensity = _sun != null ? _sunIntensity : 1.0f,
                SunElevation = Mathf.Asin(Mathf.Clamp(-forward.y, -1.0f, 1.0f)) * Mathf.Rad2Deg,
                SunYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg,
                FogStart = _fogStart, FogEnd = _fogEnd, Brightness = _gradeBrightness, Saturation = _gradeSaturation, Lamps = 1.0f, KeepYaw = true,
            };
            _lightTaken = true;
        }

        /// <summary>Blends the map's look toward `mood` by `weight` (0 the map as authored, 1 the mood).</summary>
        protected void Tint(in Mood mood, float weight)
        {
            if (!_lightTaken) return;
            var a = Authored;
            float w = Mathf.Clamp01(weight);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(a.Sky, mood.Sky, w);
            RenderSettings.ambientEquatorColor = Color.Lerp(a.Equator, mood.Equator, w);
            RenderSettings.ambientGroundColor = Color.Lerp(a.Ground, mood.Ground, w);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Color.Lerp(a.Fog, mood.Fog, w);
            RenderSettings.fogStartDistance = Mathf.Lerp(a.FogStart, mood.FogStart, w);
            RenderSettings.fogEndDistance = Mathf.Lerp(a.FogEnd, mood.FogEnd, w);
            if (_sky != null)
            {
                if (_skyHas[0]) _sky.SetColor(_skyIds[0], Color.Lerp(a.Zenith, mood.Zenith, w));
                if (_skyHas[1]) _sky.SetColor(_skyIds[1], Color.Lerp(a.Horizon, mood.Horizon, w));
                if (_skyHas[2]) _sky.SetColor(_skyIds[2], Color.Lerp(_skyWas[2], mood.Sun, w));
                if (_skyHas[3]) _sky.SetColor(_skyIds[3], Color.Lerp(_skyWas[3], Color.Lerp(mood.Horizon, mood.Sun, 0.5f), w * 0.8f));
                if (_skyHas[4]) _sky.SetColor(_skyIds[4], Color.Lerp(_skyWas[4], Color.Lerp(mood.Zenith, mood.Fog, 0.5f), w * 0.8f));
            }
            if (_sun != null)
            {
                _sun.color = Color.Lerp(a.Sun, mood.Sun, w);
                _sun.intensity = Mathf.Lerp(a.SunIntensity, mood.SunIntensity, w);
                float elevation = Mathf.Lerp(a.SunElevation, mood.SunElevation, w) * Mathf.Deg2Rad;
                float yaw = (mood.KeepYaw ? a.SunYaw : Mathf.LerpAngle(a.SunYaw, mood.SunYaw, w)) * Mathf.Deg2Rad;
                Vector3 flat = new Vector3(Mathf.Sin(yaw), 0.0f, Mathf.Cos(yaw));
                _sun.transform.rotation = Quaternion.LookRotation(flat * Mathf.Cos(elevation) + Vector3.down * Mathf.Sin(elevation), Vector3.up);
            }
            if (_grade != null) _grade.SetEventGrade(Mathf.Lerp(a.Brightness, mood.Brightness, w), Mathf.Lerp(a.Saturation, mood.Saturation, w));
            float lamps = Mathf.Lerp(1.0f, mood.Lamps, w);
            for (int i = 0; i < _sceneLamps.Length; i++) if (_sceneLamps[i] != null) _sceneLamps[i].intensity = _sceneLampWas[i] * lamps;
        }

        private void RestoreLight()
        {
            if (!_lightTaken) return;
            _lightTaken = false;
            RenderSettings.ambientMode = _ambientMode;
            RenderSettings.ambientSkyColor = _ambientSky; RenderSettings.ambientEquatorColor = _ambientEquator; RenderSettings.ambientGroundColor = _ambientGround;
            RenderSettings.fog = _fogOn; RenderSettings.fogMode = _fogMode; RenderSettings.fogStartDistance = _fogStart; RenderSettings.fogEndDistance = _fogEnd; RenderSettings.fogColor = _fogColour;
            if (_sky != null) for (int i = 0; i < _skyIds.Length; i++) if (_skyHas[i]) _sky.SetColor(_skyIds[i], _skyWas[i]);
            if (_sun != null) { _sun.color = _sunColour; _sun.intensity = _sunIntensity; _sun.shadowStrength = _sunShadow; _sun.transform.rotation = _sunTurn; }
            if (_grade != null) _grade.SetEventGrade(_gradeBrightness, _gradeSaturation);
            for (int i = 0; i < _sceneLamps.Length; i++) if (_sceneLamps[i] != null) _sceneLamps[i].intensity = _sceneLampWas[i];
            _sceneLamps = new Light[0]; _sceneLampWas = new float[0];
            _sun = null; _sky = null; _grade = null;
        }

        /// <summary>How hard a flash or strobe may hit on this screen (the Flash intensity setting).</summary>
        protected static float FlashLevel => Settings.SettingsStore.Current.EffectiveFlashIntensity;
    }
}
