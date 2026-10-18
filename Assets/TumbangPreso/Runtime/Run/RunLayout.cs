using System;
using UnityEngine;

namespace TumbangPreso.Run
{
    /// <summary>
    /// PAETE'S RUN, THE COURSE AS DATA: `Art/PaeteRun/paete_run_layout.json`, written by
    /// `tools/author_paete_run_layout.py` from the course's way points. Unity axes, metres. The scene builder
    /// (`Editor/MapKit/PaeteRunSceneBuilder.cs`) makes the solids, movers and geysers from it; the mode
    /// (<see cref="RunMode"/>) reads the rest in play: swim volumes, chutes, checkpoints, the start grid, the heights
    /// a fall is counted from.
    ///
    /// The field names are the file's own (JsonUtility reads by name), which is why they are not the code's usual case.
    /// </summary>
    [Serializable]
    public sealed class RunLayout
    {
        [Serializable] public sealed class Solid { public string name, kind, leg, shape; public Vector3 c, s; public float yaw, pitch, r, h; }
        [Serializable] public sealed class Mover
        {
            public string name, kind, leg, shape, motion; public Vector3 c, s, axis, pivot, offset;
            public float yaw, r, h, amp, period, phase, arm;
        }
        [Serializable] public sealed class Water { public string name, leg; public Vector3 c, s; public float yaw, surface, floor; }
        [Serializable] public sealed class Pad { public string name, leg; public Vector3 position; public float radius, speed, rise, apex; }
        [Serializable] public sealed class Anchor { public string name, leg, kind, mover; public Vector3 aim, c, from; public float under; }
        [Serializable] public sealed class Chute { public string name, leg; public Vector3[] points; public float width, speed, start_speed, gain, above, below; }
        [Serializable] public sealed class Checkpoint
        {
            public int id, leg, main, shortcut; public string name, stage; public Vector3 p; public float yaw, radius, half_height, fall_y, progress;
            public int[] next; public bool water;
        }
        [Serializable] public sealed class Pose { public Vector3 p; public float yaw; }
        [Serializable] public sealed class Shortcut { public string name, words; public int from, to; public int[] checkpoints; public string[] legs; }
        [Serializable] public sealed class Limits { public float min_x, max_x, min_z, max_z, floor, ceiling; }
        [Serializable] public sealed class Shot { public string name; public Vector3 at, look; public float fov; }
        [Serializable] public sealed class LegInfo { public int n; public string kind, stage, words; public Vector3 a, c; public float length, rise; }
        [Serializable] public sealed class Step { public string act, mover; public Vector3 p, aim, to; public float r; }
        [Serializable] public sealed class Route { public string leg; public Step[] steps; }

        public Solid[] solids = Array.Empty<Solid>();
        public Mover[] movers = Array.Empty<Mover>();
        public Water[] water = Array.Empty<Water>();
        public Pad[] pads = Array.Empty<Pad>();
        public Anchor[] vine_anchors = Array.Empty<Anchor>();
        public Chute[] chutes = Array.Empty<Chute>();
        public Checkpoint[] checkpoints = Array.Empty<Checkpoint>();
        public int finish;
        public Pose[] start_grid = Array.Empty<Pose>();
        public Shortcut[] shortcuts = Array.Empty<Shortcut>();
        public float kill_y = -40f;
        public Limits bounds = new Limits();
        public Shot[] review = Array.Empty<Shot>();
        public LegInfo[] legs = Array.Empty<LegInfo>();
        public Route[] routes = Array.Empty<Route>();

        /// <summary>Null when the text is not a layout (no checkpoints): the mode then does nothing rather than half a race.</summary>
        public static RunLayout Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            RunLayout layout;
            try { layout = JsonUtility.FromJson<RunLayout>(json); }
            catch (Exception e) { Debug.LogError("[PaeteRun] unreadable layout: " + e.Message); return null; }
            if (layout == null || layout.checkpoints == null || layout.checkpoints.Length < 2) return null;
            for (int i = 0; i < layout.checkpoints.Length; i++)
            {
                if (layout.checkpoints[i].id != i) { Debug.LogError("[PaeteRun] checkpoint " + i + " carries id " + layout.checkpoints[i].id); return null; }
                if (layout.checkpoints[i].next == null) layout.checkpoints[i].next = Array.Empty<int>();
            }
            return layout;
        }

        public Route RouteOf(string leg)
        {
            if (routes != null) foreach (var r in routes) if (r != null && r.leg == leg) return r;
            return null;
        }
    }

    /// <summary>
    /// THE ONE CLOCK every moving thing on the course is a function of. In a session it is Netcode's server time, which
    /// every peer already has (no message of ours), so every peer computes the same boulder in the same place: the
    /// Arena's traffic and the alley's road do the same. Offline it is the game's own time, so a pause stops the course.
    /// </summary>
    public static class RunClock
    {
        public static double Now
        {
            get
            {
                var nm = Unity.Netcode.NetworkManager.Singleton;
                if (NetAuthority.IsNetworked && nm != null && nm.IsListening) return nm.ServerTime.Time;
                return Time.inFixedTimeStep ? Time.fixedTimeAsDouble : Time.timeAsDouble;
            }
        }
    }
}
