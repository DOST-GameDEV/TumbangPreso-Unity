using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// Animates the overhead LRT-2 train zooming across the viaduct tracks above Gilmore Avenue.
    /// Triggers audio whoosh, dynamic shadows, and electric third-rail sparks.
    /// </summary>
    public sealed class LrtTrainFlyby : MonoBehaviour
    {
        [Header("Train Movement Settings")]
        [Tooltip("Seconds between train passes.")]
        public float Interval = 26.0f;

        [Tooltip("Initial delay before the first train pass.")]
        public float InitialDelay = 7.0f;

        [Tooltip("Speed of the train crossing the viaduct (m/s).")]
        public float Speed = 22.0f;

        [Tooltip("Start Z position of the train.")]
        public float StartZ = -48.0f;

        [Tooltip("End Z position where the train disappears.")]
        public float EndZ = 48.0f;

        [Tooltip("Track X offset (Track 1 = -1.5m, Track 2 = +1.5m).")]
        public float TrackX = -1.5f;

        [Tooltip("Track Y elevation.")]
        public float TrackY = 9.2f;

        private float _timer;
        private bool _isRunning;
        private float _currentZ;
        private bool _whooshPlayed;

        private void Start()
        {
            _timer = Interval - InitialDelay;
            _isRunning = false;
            transform.position = new Vector3(TrackX, TrackY, StartZ);
        }

        private void Update()
        {
            if (!_isRunning)
            {
                _timer += Time.deltaTime;
                if (_timer >= Interval)
                {
                    _timer = 0.0f;
                    _isRunning = true;
                    _currentZ = StartZ;
                    _whooshPlayed = false;
                }
                return;
            }

            // Move train along +Z
            _currentZ += Speed * Time.deltaTime;
            transform.position = new Vector3(TrackX, TrackY, _currentZ);

            // Play train audio when approaching the arena center
            if (!_whooshPlayed && _currentZ >= -18.0f)
            {
                _whooshPlayed = true;
                GameServices.Audio?.PlayAtVaried("sfx_fire_whoosh", transform.position, 0.85f, 1.05f, 0.85f);
                ImpactBurst.SpawnAt(new Vector3(TrackX, TrackY - 0.5f, _currentZ));
            }

            // Train passed the end
            if (_currentZ >= EndZ)
            {
                _isRunning = false;
                transform.position = new Vector3(TrackX, TrackY, StartZ);
            }
        }
    }
}
