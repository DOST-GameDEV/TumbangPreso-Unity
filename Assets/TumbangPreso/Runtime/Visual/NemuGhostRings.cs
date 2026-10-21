using System.Collections.Generic;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// The rings Nemu's ghosts draw on the court, in her own look: black and one violet (her ultimate's rule). A crisp thin
    /// violet line marks the edge, the court inside it is shaded a little darker, and her black smoke (`NemuSmokeBatch`, the
    /// same smoke as her ghosts) crawls slowly round the line with a few strands curling up and inward.
    ///  * PEEKER's trigger ring (owner 2026-10-21: *"show the like circle aura that activates it"*), at exactly
    ///    `NecroRules.DontLookTriggerRadius`, for everyone, for as long as the ghost waits: the line a rival sees is the line
    ///    that springs it.
    ///  * SOULBOUND's reclaim ring round each of her dormant ghosts, at `NemuRules.ReclaimRadius`, drawn on her screen only.
    ///  * Each SPREADS from its ghost when it appears (0.4 s) and draws back into it when it goes (0.3 s).
    /// Purely presentational and local: every peer draws it from the ghosts it already runs. Posed from its own clock,
    /// which stands still while the world is paused.
    /// </summary>
    public sealed class NemuGhostRings : MonoBehaviour
    {
        private const float GrowSeconds = .4f, ShrinkSeconds = .3f;
        private static readonly Color Edge = new Color(.62f, .26f, 1f, .55f);
        private static readonly Color Shade = new Color(.06f, .02f, .10f, .16f);

        // The smoke crawling round the line, typed: (angle in degrees, size in metres, density, height in metres).
        // Uneven on purpose, with gaps, so it reads as something circling and not a stamped wreath.
        private static readonly Vector4[] Crawl =
        {
            new Vector4(8, .62f, .55f, .14f), new Vector4(27, .44f, .40f, .10f), new Vector4(49, .70f, .60f, .18f),
            new Vector4(71, .38f, .35f, .08f), new Vector4(96, .58f, .50f, .15f), new Vector4(122, .50f, .45f, .12f),
            new Vector4(139, .74f, .62f, .20f), new Vector4(167, .41f, .38f, .09f), new Vector4(190, .64f, .55f, .16f),
            new Vector4(214, .46f, .42f, .11f), new Vector4(241, .68f, .58f, .17f), new Vector4(262, .36f, .33f, .08f),
            new Vector4(288, .60f, .52f, .15f), new Vector4(309, .48f, .44f, .12f), new Vector4(333, .72f, .60f, .19f),
        };

        // Strands curling up and inward off the line, typed: (angle in degrees, length in metres, phase).
        private static readonly Vector3[] Strands =
        {
            new Vector3(40, .55f, .10f), new Vector3(118, .42f, .55f), new Vector3(201, .60f, .30f),
            new Vector3(276, .48f, .80f), new Vector3(338, .38f, .65f),
        };

        private const float CrawlDegreesPerSecond = 9f;

        private sealed class Ring
        {
            public GameObject Go; public NemuSmokeBatch Smoke; public float Grow, Clock, Radius; public bool Seen; public Vector3 At;
        }

        private static NemuGhostRings _instance;
        private readonly Dictionary<Object, Ring> _rings = new Dictionary<Object, Ring>();
        private readonly List<Object> _gone = new List<Object>();
        private readonly Dictionary<float, Mesh> _meshes = new Dictionary<float, Mesh>();
        private Material _material;

        /// <summary>Called by each ghost as it appears, so the drawer exists whenever there is a ring to draw.</summary>
        public static void Ensure()
        {
            if (_instance != null || !Application.isPlaying) return;
            var go = new GameObject("NemuGhostRings") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<NemuGhostRings>();
        }

        /// <summary>How many ghosts had a ring last frame, not counting rings drawing back into a ghost that went (tests).</summary>
        public static int Count
        {
            get
            {
                if (_instance == null) return 0;
                int n = 0;
                foreach (var r in _instance._rings.Values) if (r.Seen) n++;
                return n;
            }
        }

        private void LateUpdate()
        {
            foreach (var r in _rings.Values) r.Seen = false;
            foreach (var g in NemuBlindfold.Live) if (g != null && !g.Sprung && !g.Retiring) Mark(g, g.Spot, NecroRules.DontLookTriggerRadius);
            bool spectating = GameLaunch.Spectator;
            foreach (var g in NemuDormant.Live)
                if (g != null && !g.Reclaimed && !g.Expired && !spectating && g.OwnerSlot == NetAuthority.LocalSlot) Mark(g, g.Position, NemuRules.ReclaimRadius);

            float dt = Time.deltaTime;
            _gone.Clear();
            foreach (var pair in _rings)
            {
                var r = pair.Value;
                r.Grow = r.Seen ? Mathf.Min(1f, r.Grow + dt / GrowSeconds) : r.Grow - dt / ShrinkSeconds;
                if (r.Grow <= 0f) { _gone.Add(pair.Key); continue; }
                r.Clock += dt;
                float k = 1f - (1f - r.Grow) * (1f - r.Grow);
                Vector3 centre = new Vector3(r.At.x, Slipper.GroundY(r.At) + .025f, r.At.z);
                var t = r.Go.transform;
                t.position = centre;
                t.localScale = new Vector3(k, 1f, k);
                DrawSmoke(r, centre, k);
            }
            foreach (var key in _gone) { _rings[key].Smoke?.Dispose(); Destroy(_rings[key].Go); _rings.Remove(key); }
        }

        private static void DrawSmoke(Ring r, Vector3 centre, float k)
        {
            var s = r.Smoke;
            float radius = r.Radius * k;
            float turn = r.Clock * CrawlDegreesPerSecond;
            s.Begin();
            for (int i = 0; i < Crawl.Length; i++)
            {
                var c = Crawl[i];
                float breathe = .85f + .15f * Mathf.Sin(r.Clock * 1.3f + i * 1.7f);
                s.Puff(centre + Dir(c.x + turn) * radius + Vector3.up * c.w, c.y * breathe, c.z * k, i / (float)Crawl.Length,
                    0f, .7f, c.x * 2f);
            }
            for (int i = 0; i < Strands.Length; i++)
            {
                var c = Strands[i];
                float u = Mathf.Repeat(r.Clock * .35f + c.z, 1f);           // each strand rises, thins and comes again
                Vector3 foot = Dir(c.x + turn) * radius;
                Vector3 along = (Vector3.up * 1.4f - foot.normalized * .6f).normalized;
                s.Wisp(centre + foot + along * (c.y * (.4f + u)), along, .10f, c.y, .45f * (1f - u) * k, .3f + i * .13f,
                    u * .8f, .9f);
            }
            for (int i = 0; i < 3; i++)                                       // a dim violet glow riding the line
            {
                float a = turn * 1.6f + i * 120f;
                s.Light(centre + Dir(a) * radius + Vector3.up * .05f, .55f, .35f * k, NemuSmokeBatch.Violet, default, 1.2f);
            }
            s.End(r.Clock);
        }

        private void Mark(Object ghost, Vector3 at, float radius)
        {
            if (!_rings.TryGetValue(ghost, out var r))
            {
                r = new Ring { Go = Build(radius), Radius = radius };
                r.Smoke = new NemuSmokeBatch(r.Go.transform, "NemuRingSmoke", Crawl.Length + Strands.Length, 3, 1);
                _rings[ghost] = r;
            }
            r.Seen = true;
            r.At = at;
        }

        private GameObject Build(float radius)
        {
            if (!_meshes.TryGetValue(radius, out var mesh)) _meshes[radius] = mesh = BuildMesh(radius);
            if (_material == null) _material = new Material(Shader.Find("Sprites/Default")) { name = "NemuRingGround" };
            var go = new GameObject("NemuGhostRing");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }

        /// <summary>The court inside shaded a little darker, then the thin violet line: one flat mesh, coloured per vertex.</summary>
        private static Mesh BuildMesh(float radius)
        {
            var v = new List<Vector3>(); var c = new List<Color>(); var tris = new List<int>();
            const int sides = 72;
            const float line = .05f;

            int centre = v.Count; v.Add(Vector3.zero); c.Add(new Color(Shade.r, Shade.g, Shade.b, 0f));
            for (int i = 0; i <= sides; i++) { v.Add(Dir(i * 360f / sides) * (radius - line)); c.Add(Shade); }
            for (int i = 0; i < sides; i++) { tris.Add(centre); tris.Add(centre + 1 + i); tris.Add(centre + 2 + i); }

            int edge = v.Count;
            for (int i = 0; i <= sides; i++)
            {
                var d = Dir(i * 360f / sides);
                v.Add(d * (radius - line)); c.Add(Edge);
                v.Add(d * radius); c.Add(Edge);
            }
            for (int i = 0; i < sides; i++)
            {
                int k = edge + i * 2;
                tris.Add(k); tris.Add(k + 1); tris.Add(k + 2); tris.Add(k + 2); tris.Add(k + 1); tris.Add(k + 3);
            }

            var mesh = new Mesh { name = "NemuGhostRing", hideFlags = HideFlags.DontSave };
            mesh.SetVertices(v); mesh.SetColors(c); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(radius * 2f, .2f, radius * 2f));
            return mesh;
        }

        private static Vector3 Dir(float degrees)
        {
            float a = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
        }
    }
}
