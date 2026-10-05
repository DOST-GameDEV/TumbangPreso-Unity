using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// The Arena's break picture: a high camera that shows the stage rebuilding itself between
    /// rounds (owner: "ther'll be camera cinematics showing the transforming play arrea").
    ///
    /// It is its own camera drawn OVER the game camera (a higher depth), enabled only while an
    /// ordinary break is playing on this map, so `CameraRig` and `HalftimePresentation` are not
    /// touched and the player's view is exactly where it was when the break ends. It moves on
    /// the break's shared clock, never `deltaTime` (`Time.timeScale` is 0 in a break): a rising
    /// orbit round the stage, then a push in on the can.
    ///
    /// HALFTIME IS LEFT ALONE. The 10 s middle break keeps its own replay and standings.
    ///
    /// It carries no listener and none of the game camera's outline or anti-alias
    /// passes; it takes the map's colour grade so the picture does not change tone at the cut.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class ArenaBreakCamera : MonoBehaviour
    {
        /// <summary>Above the game camera (0) and below any overlay canvas.</summary>
        public const float Depth = 50.0f;

        /// <summary>The share of the break spent on the orbit; the rest is the push in.</summary>
        public float OrbitShare = 0.7f;
        public float StartYaw = 35.0f, OrbitSweep = 80.0f, PushSweep = 12.0f;
        public float OrbitRadiusFrom = 26.0f, OrbitRadiusTo = 21.0f;
        public float OrbitHeightFrom = 11.0f, OrbitHeightTo = 19.0f;
        public float PushRadius = 9.0f, PushHeight = 5.5f;
        /// <summary>The orbit above is written for a stage this far across from the can to its
        /// furthest edge; it is scaled to the stage's own `Radius`. The push in is on the can and is not.</summary>
        public float AuthoredRadius = 14.0f;

        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.depth = Depth;
            _camera.enabled = false;
        }

        private void Start()
        {
            var grade = GetComponent<Visual.ColourGrade>();
            if (grade == null) grade = gameObject.AddComponent<Visual.ColourGrade>();
            grade.AdoptFromScene();
        }

        private void OnDisable()
        {
            if (_camera != null) _camera.enabled = false;
        }

        private void LateUpdate()
        {
            var stage = ArenaStage.Instance;
            var hp = HalftimePresentation.Instance;
            bool show = stage != null && hp != null && hp.Active && !hp.IsHalftime && hp.Duration > 0.0f;

            if (_camera.enabled != show) _camera.enabled = show;
            if (!show) return;

            float t = Mathf.Clamp01((float)(SharedUltimatePhase.Now - hp.Began) / hp.Duration);
            // Reduced motion: one still high view of the whole stage.
            if (Settings.SettingsStore.Current.ReducedUiMotion) t = 0.0f;

            float orbit = Smooth(Mathf.Clamp01(t / OrbitShare));
            float push = Smooth(Mathf.Clamp01((t - OrbitShare) / (1.0f - OrbitShare)));

            float yaw = (StartYaw + OrbitSweep * orbit + PushSweep * push) * Mathf.Deg2Rad;
            float can = stage.CanHeight;
            float scale = AuthoredRadius > 0.0f ? Mathf.Max(1.0f, stage.Radius / AuthoredRadius) : 1.0f;
            float radius = Mathf.Lerp(Mathf.Lerp(OrbitRadiusFrom, OrbitRadiusTo, orbit) * scale, PushRadius, push);
            float height = Mathf.Lerp(Mathf.Lerp(OrbitHeightFrom, OrbitHeightTo, orbit) * scale, PushHeight + can, push);

            Vector3 centre = stage.transform.position;
            Vector3 eye = centre + new Vector3(Mathf.Sin(yaw) * radius, height, Mathf.Cos(yaw) * radius);
            Vector3 look = centre + Vector3.up * Mathf.Lerp(0.5f, can + 0.6f, push);
            transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye, Vector3.up));
        }

        private static float Smooth(float t) => t * t * (3.0f - 2.0f * t);
    }
}
