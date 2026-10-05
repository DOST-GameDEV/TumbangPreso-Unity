using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// The Arena's break picture: the camera that shows the stage rebuilding itself between
    /// rounds (owner: "ther'll be camera cinematics showing the transforming play arrea").
    ///
    /// It is its own camera drawn OVER the game camera (a higher depth), enabled only while the
    /// show is playing on this map, so `CameraRig` and `HalftimePresentation` are not
    /// touched and the player's view is exactly where it was when the break ends. It moves on
    /// the break's shared clock, never `deltaTime` (`Time.timeScale` is 0 in a break).
    ///
    /// FIVE SHOTS, CUT ON THE STAGE'S OWN BEATS (`ArenaStage.BreakBeats`; owner, 2026-10-05: "map
    /// transformation is so dull, theres no emphasis on it"). One slow orbit showed everything
    /// and pointed at nothing; each of these has one thing to show:
    ///   1. THE LIGHTS GO DOWN  a low push in from the turf as the stadium dims;
    ///   2. THE BLUEPRINT       from above, the whole stage, as the hologram sweeps out over it;
    ///   3. THE SPLIT           a low push across the stage as the pieces undock and go;
    ///   4. THE LAST LOCK       a long lens on the piece that locks last, from outside the stage;
    ///   5. THE REVEAL          a rising wide shot as the lights come back and the crowd goes up.
    /// Every eye position is outside or above the stage, higher than any deck, so no piece
    /// travels through the lens.
    ///
    /// ⚠️ IT SHAKES ITSELF. The break is not drawn by `CameraRig` (which does not even run in a
    /// hold), so `CameraRig.Shake` would move nothing here. `Punch` is this camera's own: a few
    /// tenths of a degree, because the eye is tens of metres from the stage and a shake in
    /// centimetres would not move a pixel. It obeys the player's camera shake setting
    /// (`EffectiveCameraShake`, which reduced effects quarters) and is off under reduced motion.
    ///
    /// THE DIM IS ON THIS CAMERA'S OWN GRADE (`ColourGrade.SetEventGrade`), so the scene's
    /// lights, the look system and the game camera are untouched and nothing has to be put back.
    ///
    /// AT HALFTIME IT WAITS. The middle break's own 10 s (replay, standings) play first with
    /// this camera off; it takes the picture when the show starts
    /// (`HalftimePresentation.StageShowPlaying`) and cuts the same five shots.
    ///
    /// It carries no listener and none of the game camera's outline or anti-alias
    /// passes; it takes the map's colour grade so the picture does not change tone at the cut.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class ArenaBreakCamera : MonoBehaviour
    {
        /// <summary>Above the game camera (0) and below any overlay canvas.</summary>
        public const float Depth = 50.0f;

        /// <summary>The wide view's bearing, radius and height (the reveal's shot starts from
        /// them, and reduced motion holds them for the whole break), written for a stage
        /// `AuthoredRadius` across and scaled to the stage's own `Radius`.</summary>
        public float StartYaw = 35.0f;
        public float OrbitRadiusFrom = 26.0f;
        public float OrbitHeightFrom = 11.0f;
        public float AuthoredRadius = 14.0f;
        /// <summary>The blueprint's shot: no higher than this over the stage (the scoreboard hangs from y 41.6).</summary>
        public float TopHeight = 37.0f;
        /// <summary>How dark the alarm takes the picture, and how grey (1 is untouched).</summary>
        // 1 and 1: no dim. It was 0.60 and 0.86 for the alarm; the owner, 2026-10-05, on playing it:
        // "the screen goes dim when the platform switches". The show must not darken the picture.
        public float AlarmBrightness = 1.0f, AlarmSaturation = 1.0f;
        /// <summary>The largest turn of one punch at strength 1, degrees.</summary>
        public float ShakeDegrees = 0.55f;

        /// <summary>The break's camera while it is the one drawing the picture, else null.</summary>
        public static Camera Showing { get; private set; }

        private static ArenaBreakCamera _instance;
        private Camera _camera;
        private Visual.ColourGrade _grade;
        private float _baseFov = 50.0f;
        private float _punch, _punchAt = -99.0f;
        private readonly Vector3[] _hero = new Vector3[1];

        /// <summary>
        /// Shake the break's picture: `strength` 1 is a platform locking, 2 the reveal. A weaker
        /// punch never lowers a stronger one still ringing. Nothing happens outside a break.
        /// </summary>
        public static void Punch(float strength)
        {
            var self = _instance;
            if (self == null || Showing == null) return;

            float left = self._punch * Mathf.Exp(-(Time.unscaledTime - self._punchAt) * PunchDecay);
            if (strength < left) return;
            self._punch = strength;
            self._punchAt = Time.unscaledTime;
        }

        private const float PunchDecay = 5.5f;

        private void Awake()
        {
            _instance = this;
            _camera = GetComponent<Camera>();
            _camera.depth = Depth;
            _camera.enabled = false;
            _baseFov = _camera.fieldOfView;
        }

        private void Start()
        {
            _grade = GetComponent<Visual.ColourGrade>();
            if (_grade == null) _grade = gameObject.AddComponent<Visual.ColourGrade>();
            _grade.AdoptFromScene();
        }

        private void OnEnable() => _instance = this;

        private void OnDisable()
        {
            if (_camera != null) _camera.enabled = false;
            if (Showing == _camera) Showing = null;
            if (_instance == this) _instance = null;
        }

        private void LateUpdate()
        {
            var stage = ArenaStage.Instance;
            var hp = HalftimePresentation.Instance;
            bool show = stage != null && hp != null && hp.StageShowPlaying && hp.Duration > 0.0f;

            if (_camera.enabled != show) _camera.enabled = show;
            Showing = show ? _camera : (Showing == _camera ? null : Showing);
            if (!show)
            {
                _punch = 0.0f;
                if (_grade != null) _grade.SetEventGrade(1.0f, 1.0f);
                return;
            }

            var settings = Settings.SettingsStore.Current;
            Vector3 centre = stage.transform.position;
            float scale = AuthoredRadius > 0.0f ? Mathf.Max(1.0f, stage.Radius / AuthoredRadius) : 1.0f;

            // Reduced motion, or a break this stage cannot read: one still wide view of the whole stage.
            if (settings.ReducedUiMotion || !stage.TryBreak(out var beats))
            {
                Vector3 still = Polar(centre, StartYaw, OrbitRadiusFrom * scale, OrbitHeightFrom * scale);
                Pose(still, centre + Vector3.up * 0.5f, Vector3.up, _baseFov);
                if (_grade != null) _grade.SetEventGrade(1.0f, 1.0f);
                return;
            }

            float age = beats.Age, radius = stage.Radius, can = stage.CanHeight;
            float blueprint = beats.ScanStart + 0.5f;
            float hero = Mathf.Max(beats.Undock + 1.2f, beats.MoveEnd - 1.25f);

            if (age < blueprint)
            {
                // 1. The lights go down: low over the turf, pushing in.
                float t = Smooth(age / blueprint);
                Vector3 eye = Polar(centre, StartYaw - 25.0f + 7.0f * t, radius * Mathf.Lerp(2.15f, 1.95f, t), Mathf.Lerp(3.2f, 2.7f, t));
                Pose(eye, centre + Vector3.up * 1.6f, Vector3.up, 42.0f);
            }
            else if (age < beats.Undock)
            {
                // 2. The blueprint: straight down on the whole stage, turning slowly, the top
                //    of the frame along the shot's own bearing.
                float t = Mathf.Clamp01((age - blueprint) / (beats.Undock - blueprint));
                float height = Mathf.Min(TopHeight, radius * 1.7f) * Mathf.Lerp(1.0f, 0.93f, Smooth(t));
                float yaw = StartYaw + 40.0f + 14.0f * t;
                Vector3 ahead = ArenaStageMesh.Direction(yaw);
                Vector3 eye = centre - ahead * 3.0f + Vector3.up * height;
                float fov = Mathf.Clamp(2.0f * Mathf.Atan(radius * 1.12f / height) * Mathf.Rad2Deg, 40.0f, 75.0f);
                Pose(eye, centre, ahead, fov);
            }
            else if (age < hero)
            {
                // 3. The split: a low push across the stage as it comes apart, over every deck.
                float t = Smooth((age - beats.Undock) / (hero - beats.Undock));
                float yaw = StartYaw + 150.0f + 22.0f * t;
                Vector3 eye = Polar(centre, yaw, radius * Mathf.Lerp(1.45f, 0.62f, t), Mathf.Lerp(5.8f, 4.6f, t) + Mathf.Max(0.0f, can));
                Pose(eye, centre + Vector3.up * (can + 0.3f), Vector3.up, 55.0f);
            }
            else if (age < beats.Reveal)
            {
                // 4. The last lock: a long lens from outside the stage on the piece that locks last.
                float t = Smooth((age - hero) / (beats.Reveal - hero));
                Vector3 target = centre + Vector3.up * can;
                int last = stage.LastLocking(beats.From, beats.To);
                if (last >= 0 && stage.PiecePoints(last, beats.To, _hero) > 0) target = _hero[0];

                Vector3 flat = target - centre; flat.y = 0.0f;
                float out_ = flat.magnitude;
                float bearing = out_ > 1.0f ? Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg : StartYaw + 90.0f;
                float stand = Mathf.Max(out_ + 8.0f, radius + 4.0f) - 1.5f * t;
                Vector3 eye = Polar(centre, bearing + Mathf.Lerp(26.0f, 20.0f, t), stand, target.y - centre.y + Mathf.Lerp(3.4f, 2.9f, t));
                float fov = Mathf.Clamp(2.0f * Mathf.Atan(6.5f / Mathf.Max(1.0f, Vector3.Distance(eye, target))) * Mathf.Rad2Deg, 24.0f, 55.0f);
                Pose(eye, target + Vector3.up * 0.3f, Vector3.up, fov);
            }
            else
            {
                // 5. The reveal: rising and widening, the stands in frame as the crowd goes up.
                float t = Smooth((age - beats.Reveal) / Mathf.Max(0.1f, beats.Duration - beats.Reveal));
                Vector3 eye = Polar(centre, StartYaw + 200.0f + 32.0f * t, OrbitRadiusFrom * scale * Mathf.Lerp(1.0f, 1.14f, t),
                                    OrbitHeightFrom * scale * Mathf.Lerp(0.72f, 1.1f, t));
                Pose(eye, centre + Vector3.up * 2.0f, Vector3.up, Mathf.Lerp(52.0f, 56.0f, t));
            }

            // The alarm's dim: down over the first half second, back up in a fifth at the reveal.
            if (_grade != null)
            {
                float down = Mathf.Clamp01(age / 0.5f), up = Mathf.Clamp01((age - beats.Reveal) / 0.2f);
                float dim = Smooth(down) * (1.0f - up);
                float depth = settings.ReducedEffects ? 0.5f : 1.0f;
                _grade.SetEventGrade(Mathf.Lerp(1.0f, AlarmBrightness, dim * depth), Mathf.Lerp(1.0f, AlarmSaturation, dim * depth));
            }

            // Its own shake, last, so it is added to the shot and never fed back into it.
            float level = settings.EffectiveCameraShake;
            float ring = _punch * Mathf.Exp(-(Time.unscaledTime - _punchAt) * PunchDecay);
            if (level > 0.0f && ring > 0.01f)
            {
                float clock = Time.unscaledTime, turn = ShakeDegrees * ring * level;
                transform.rotation *= Quaternion.Euler(Mathf.Sin(clock * 61.3f) * turn, Mathf.Sin(clock * 47.9f + 1.3f) * turn, Mathf.Sin(clock * 53.1f + 0.5f) * turn * 0.5f);
            }
        }

        private void Pose(Vector3 eye, Vector3 look, Vector3 up, float fov)
        {
            Vector3 forward = look - eye;
            if (forward.sqrMagnitude < 1e-6f) forward = Vector3.down;
            // A look along the up vector has no roll to give: fall back to any other up.
            if (Vector3.Cross(forward, up).sqrMagnitude < 1e-6f) up = Vector3.forward;
            transform.SetPositionAndRotation(eye, Quaternion.LookRotation(forward, up));
            if (!Mathf.Approximately(_camera.fieldOfView, fov)) _camera.fieldOfView = fov;
        }

        private static Vector3 Polar(Vector3 centre, float bearing, float radius, float height) =>
            centre + ArenaStageMesh.Direction(bearing) * radius + Vector3.up * height;

        private static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3.0f - 2.0f * t);
        }
    }
}
