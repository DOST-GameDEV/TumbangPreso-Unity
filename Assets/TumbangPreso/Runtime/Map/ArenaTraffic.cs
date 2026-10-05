using System;
using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// The Arena's sky traffic: craft that slide round closed lanes outside the stadium, and the
    /// train on its loop far below (tools/arena_traffic.json, placed by `ArenaSceneBuilder`).
    ///
    /// PRESENTATION ONLY, AND NOTHING IS SENT. A craft's place is a closed-form function of one
    /// clock: in a session the server clock every peer already shares (as `KantoTraffic`'s
    /// locked mode reads it), so every peer draws the same sky; offline this peer's own game
    /// clock, so a pause stops it. Nothing here has a collider and nothing reads it.
    ///
    /// A craft sits at the fraction (phase + time x speed / length) of its lane, as the city kit
    /// wrote it; a lane with no phases spaces its craft evenly. A SPINNER is a thing modelled
    /// already bent to a circle about the can (the train on its rail loop): it is turned about
    /// the vertical through the map's origin, not moved along points.
    /// </summary>
    public sealed class ArenaTraffic : MonoBehaviour
    {
        [Serializable]
        public sealed class Path
        {
            public string Name;
            /// <summary>The corners of a closed loop, in the map's frame. The last joins the first.</summary>
            public Vector3[] Points = Array.Empty<Vector3>();
            /// <summary>Metres a second along the loop. Negative runs it the other way.</summary>
            public float Speed = 18.0f;
            /// <summary>The craft on this loop.</summary>
            public Transform[] Craft = Array.Empty<Transform>();
            /// <summary>Each craft's place at time zero, as a share of the loop (0 to 1). Shorter
            /// than `Craft`, or empty: the rest are spaced evenly.</summary>
            public float[] Phases = Array.Empty<float>();
        }

        [Serializable]
        public sealed class Spinner
        {
            public Transform Body;
            /// <summary>Degrees a second about the vertical, clockwise seen from above.</summary>
            public float DegreesPerSecond;
        }

        public Path[] Paths = Array.Empty<Path>();
        public Spinner[] Spinners = Array.Empty<Spinner>();

        /// <summary>How far ahead a craft looks for its heading, so it turns through a corner.</summary>
        private const float LookAhead = 6.0f;

        private float[][] _along;
        private Quaternion[] _spinFrom;
        private Vector3[] _spinAt;
        private double _clock;

        private void Awake()
        {
            _along = new float[Paths.Length][];
            for (int p = 0; p < Paths.Length; p++)
            {
                var points = Paths[p] != null ? Paths[p].Points : null;
                int n = points != null ? points.Length : 0;
                // Distance along the loop at each corner, and the whole loop last.
                var along = new float[n + 1];
                for (int i = 0; i < n; i++) along[i + 1] = along[i] + Vector3.Distance(points[i], points[(i + 1) % n]);
                _along[p] = along;
            }

            // A spinner's own pose as the scene has it, in this object's frame: it is turned from there.
            _spinFrom = new Quaternion[Spinners.Length];
            _spinAt = new Vector3[Spinners.Length];
            for (int s = 0; s < Spinners.Length; s++)
            {
                var body = Spinners[s] != null ? Spinners[s].Body : null;
                if (body == null) continue;
                _spinFrom[s] = Quaternion.Inverse(transform.rotation) * body.rotation;
                _spinAt[s] = transform.InverseTransformPoint(body.position);
            }
        }

        private void Update()
        {
            var nm = Unity.Netcode.NetworkManager.Singleton;
            if (NetAuthority.IsNetworked && nm != null && nm.IsListening) _clock = nm.ServerTime.Time;
            else _clock += Time.deltaTime;

            for (int p = 0; p < Paths.Length; p++)
            {
                var path = Paths[p];
                var along = _along[p];
                if (path == null || along.Length < 3 || path.Craft == null) continue;

                float loop = along[along.Length - 1];
                if (loop < 1.0f) continue;

                for (int c = 0; c < path.Craft.Length; c++)
                {
                    var craft = path.Craft[c];
                    if (craft == null) continue;

                    double phase = path.Phases != null && c < path.Phases.Length ? path.Phases[c] : (double)c / path.Craft.Length;
                    double at = (_clock * path.Speed + loop * phase) % loop;
                    if (at < 0.0) at += loop;
                    float sign = path.Speed < 0.0f ? -1.0f : 1.0f;
                    Vector3 here = At(path.Points, along, (float)at);
                    Vector3 ahead = At(path.Points, along, Mathf.Repeat((float)at + LookAhead * sign, loop));
                    Vector3 heading = transform.TransformDirection(ahead - here);
                    craft.SetPositionAndRotation(transform.TransformPoint(here),
                        heading.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(heading, Vector3.up) : craft.rotation);
                }
            }

            for (int s = 0; s < Spinners.Length; s++)
            {
                var spinner = Spinners[s];
                if (spinner == null || spinner.Body == null) continue;

                var turn = Quaternion.Euler(0.0f, (float)(_clock * spinner.DegreesPerSecond % 360.0), 0.0f);
                spinner.Body.SetPositionAndRotation(transform.TransformPoint(turn * _spinAt[s]), transform.rotation * turn * _spinFrom[s]);
            }
        }

        private static Vector3 At(Vector3[] points, float[] along, float distance)
        {
            int n = points.Length;
            for (int i = 0; i < n; i++)
            {
                if (distance > along[i + 1] && i < n - 1) continue;
                float span = along[i + 1] - along[i];
                return Vector3.Lerp(points[i], points[(i + 1) % n], span > 1e-5f ? (distance - along[i]) / span : 0.0f);
            }

            return points[0];
        }
    }
}
