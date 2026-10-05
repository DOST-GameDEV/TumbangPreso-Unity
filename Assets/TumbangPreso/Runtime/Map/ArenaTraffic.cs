using System;
using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// The Arena's sky traffic: craft that slide round closed paths outside the stadium
    /// (tools/arena_traffic.json, placed by `ArenaSceneBuilder`).
    ///
    /// PRESENTATION ONLY, AND NOTHING IS SENT. A craft's place is a closed-form function of one
    /// clock: in a session the server clock every peer already shares (as `KantoTraffic`'s
    /// locked mode reads it), so every peer draws the same sky; offline this peer's own game
    /// clock, so a pause stops it. Nothing here has a collider and nothing reads it.
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
            /// <summary>The craft on this loop, spaced evenly round it.</summary>
            public Transform[] Craft = Array.Empty<Transform>();
        }

        public Path[] Paths = Array.Empty<Path>();

        /// <summary>How far ahead a craft looks for its heading, so it turns through a corner.</summary>
        private const float LookAhead = 6.0f;

        private float[][] _along;
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

                    double at = (_clock * path.Speed + (double)loop * c / path.Craft.Length) % loop;
                    if (at < 0.0) at += loop;
                    float sign = path.Speed < 0.0f ? -1.0f : 1.0f;
                    Vector3 here = At(path.Points, along, (float)at);
                    Vector3 ahead = At(path.Points, along, Mathf.Repeat((float)at + LookAhead * sign, loop));
                    Vector3 heading = transform.TransformDirection(ahead - here);
                    craft.SetPositionAndRotation(transform.TransformPoint(here),
                        heading.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(heading, Vector3.up) : craft.rotation);
                }
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
