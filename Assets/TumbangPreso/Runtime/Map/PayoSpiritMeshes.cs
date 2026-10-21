using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// THE BUBBLE SPIRIT'S MESHES, built once in code and shared by every catch (`PayoBubbleSpirit`). Nothing here is
    /// touched again after the build: every moving part is moved in its shader from the clock and the numbers baked
    /// into each vertex (see the headers of `PayoSpirit.shader`, `PayoBubble.shader` and `PayoSplash.shader` for what
    /// each channel carries).
    ///
    /// THE SPIRIT (head top at the origin, front +z, about 1.7 m wide and 2 m from head to tail tip): a chubby
    /// turquoise water body that tapers into a curled tail, a woven red-black-white headband, a warm grey stone mask
    /// with turquoise inlay eyes (a white glint in each), a brow ridge, a nose, rosy cheeks and a small mouth, two
    /// droopy taro-leaf ears hung from the band, and two stubby arms. Calm painted colours: one base colour per part
    /// with one soft gradient (`Logs/payo/pieces/CALM_STYLE.md`).
    /// </summary>
    public static class PayoSpiritMeshes
    {
        public const int PartBody = 0, PartStone = 1, PartInlay = 2, PartLeaf = 3, PartArm = 4, PartBand = 5, PartMouth = 6, PartGlint = 7;
        /// <summary>The spirit's body radius at its widest, and the height (under the head top) where that is.</summary>
        public const float BodyRadius = 0.85f, Widest = 0.75f;

        private static Mesh _spirit, _bubble, _splash;

        public static Mesh Spirit => _spirit != null ? _spirit : (_spirit = BuildSpirit());
        public static Mesh Bubble => _bubble != null ? _bubble : (_bubble = BuildBubble());
        public static Mesh Splash => _splash != null ? _splash : (_splash = BuildSplash());

        // ------------------------------------------------------------------ a small builder

        private sealed class B
        {
            public readonly List<Vector3> P = new List<Vector3>(), N = new List<Vector3>();
            public readonly List<Color> C = new List<Color>();
            public readonly List<Vector4> U0 = new List<Vector4>(), U1 = new List<Vector4>();
            public readonly List<int> T = new List<int>();
            public int V => P.Count;
            public int Add(Vector3 p, Vector3 n, Color c, Vector4 u0, Vector4 u1) { P.Add(p); N.Add(n); C.Add(c); U0.Add(u0); U1.Add(u1); return P.Count - 1; }
            public void Tri(int a, int b, int c) { T.Add(a); T.Add(b); T.Add(c); }

            /// <summary>Smooth normals for the vertices from `v0` on, from the triangles from `t0` on.</summary>
            public void Normals(int v0, int t0)
            {
                for (int i = v0; i < P.Count; i++) N[i] = Vector3.zero;
                for (int i = t0; i + 2 < T.Count; i += 3)
                {
                    int a = T[i], b = T[i + 1], c = T[i + 2];
                    var f = Vector3.Cross(P[b] - P[a], P[c] - P[a]);
                    N[a] += f; N[b] += f; N[c] += f;
                }
                for (int i = v0; i < P.Count; i++) N[i] = N[i].sqrMagnitude > 1e-12f ? N[i].normalized : Vector3.up;
            }

            public Mesh Make(string name, Bounds bounds)
            {
                var m = new Mesh { name = name, hideFlags = HideFlags.DontSave | HideFlags.DontUnloadUnusedAsset };
                if (V > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                m.SetVertices(P); m.SetNormals(N); m.SetColors(C); m.SetUVs(0, U0); m.SetUVs(1, U1);
                m.SetTriangles(T, 0, false);
                // The shaders move things well outside the rest shape: the bounds say so, or the spirit would be culled.
                m.bounds = bounds;
                m.UploadMeshData(true);
                return m;
            }
        }

        /// <summary>
        /// A closed surface of revolution: rings of (centre, radius x, radius z) in order, capped by a pole at each end.
        /// The colour is asked per ring (`colour(ringIndex, angle)`). Triangles wind outward.
        /// </summary>
        private static void Lathe(B b, IList<Vector4> rings, Vector3 topPole, Vector3 bottomPole, int seg, System.Func<int, float, Color> colour,
                                  Vector4 u0, Vector4 u1, Matrix4x4 m)
        {
            int v0 = b.V, t0 = b.T.Count;
            int top = b.Add(m.MultiplyPoint3x4(topPole), Vector3.up, colour(0, 0f), u0, u1);
            for (int r = 0; r < rings.Count; r++)
                for (int s = 0; s < seg; s++)
                {
                    float a = s * Mathf.PI * 2f / seg;
                    var ring = rings[r];
                    var p = new Vector3(ring.x + Mathf.Cos(a) * ring.w, ring.y, ring.z + Mathf.Sin(a) * WidthZ(ring));
                    b.Add(m.MultiplyPoint3x4(p), Vector3.up, colour(r, a), u0, u1);
                }
            int bottom = b.Add(m.MultiplyPoint3x4(bottomPole), Vector3.up, colour(rings.Count - 1, 0f), u0, u1);
            int first = top + 1;
            for (int s = 0; s < seg; s++) b.Tri(top, first + (s + 1) % seg, first + s);
            for (int r = 0; r + 1 < rings.Count; r++)
                for (int s = 0; s < seg; s++)
                {
                    int a0 = first + r * seg + s, a1 = first + r * seg + (s + 1) % seg;
                    int b0 = a0 + seg, b1 = a1 + seg;
                    b.Tri(a0, a1, b1); b.Tri(a0, b1, b0);
                }
            int last = first + (rings.Count - 1) * seg;
            for (int s = 0; s < seg; s++) b.Tri(bottom, last + s, last + (s + 1) % seg);
            b.Normals(v0, t0);
        }

        // A ring's z radius rides in its x radius unless a lathe is an ellipsoid: `Ellipsoid` scales afterwards instead.
        private static float WidthZ(Vector4 ring) => ring.w;

        /// <summary>An ellipsoid (radii in its own frame, then `m`), one flat colour, `bend` applied to every point after.</summary>
        private static void Ellipsoid(B b, Vector3 centre, Vector3 radii, Quaternion turn, int seg, int stacks, Color colour, Vector4 u0, Vector4 u1,
                                      System.Func<Vector3, Vector3> bend = null, Color? bottomColour = null)
        {
            var rings = new List<Vector4>();
            for (int i = 1; i < stacks; i++)
            {
                float a = i * Mathf.PI / stacks;
                rings.Add(new Vector4(0f, Mathf.Cos(a), 0f, Mathf.Sin(a)));
            }
            var m = Matrix4x4.TRS(centre, turn, radii);
            Color bottom = bottomColour ?? colour;
            int v0 = b.V;
            Lathe(b, rings, Vector3.up, Vector3.down, seg, (r, a) => Color.Lerp(colour, bottom, (float)r / Mathf.Max(1, rings.Count - 1)), u0, u1, m);
            if (bend == null) return;
            int t0 = b.T.Count - (seg * 2 + (rings.Count - 1) * seg * 2) * 3;
            for (int i = v0; i < b.V; i++) b.P[i] = bend(b.P[i]);
            b.Normals(v0, t0);
        }

        // ------------------------------------------------------------------ the spirit

        // The body's surface radius at a height under the head top (the dome above the widest ring, the taper below).
        private static float BodyR(float y)
        {
            if (y >= -Widest) { float c = 1f + y / Widest; return BodyRadius * Mathf.Sqrt(Mathf.Max(0f, 1f - c * c)); }
            float s = Mathf.Clamp01((-y - Widest) / 0.75f);
            return BodyRadius * Mathf.Pow(Mathf.Max(0f, Mathf.Cos(s * Mathf.PI * 0.5f * 0.92f)), 0.85f);
        }

        private static Color Lerp3(Color a, Color b, Color c, float t) => t < 0.5f ? Color.Lerp(a, b, t * 2f) : Color.Lerp(b, c, t * 2f - 1f);

        private static Mesh BuildSpirit()
        {
            var b = new B();
            Color waterTop = new Color(0.52f, 0.86f, 0.88f), waterMid = new Color(0.27f, 0.68f, 0.80f), waterLow = new Color(0.20f, 0.52f, 0.72f), mist = new Color(0.72f, 0.89f, 0.93f);
            Color stone = new Color(0.68f, 0.64f, 0.58f), stoneLow = new Color(0.60f, 0.56f, 0.51f), stoneLight = new Color(0.76f, 0.72f, 0.66f);
            Color inlay = new Color(0.22f, 0.78f, 0.76f), red = new Color(0.72f, 0.20f, 0.18f), black = new Color(0.16f, 0.14f, 0.15f), white = new Color(0.93f, 0.90f, 0.84f);
            Color leaf = new Color(0.32f, 0.54f, 0.30f), leafTip = new Color(0.54f, 0.73f, 0.40f), blush = new Color(0.93f, 0.62f, 0.58f), mouth = new Color(0.30f, 0.20f, 0.20f);

            // ---- the body: a dome to the widest ring, a taper, then a tail that curls to one side
            var rings = new List<Vector4>(); var ys = new List<float>();
            for (int k = 1; k <= 8; k++)
            {
                float a = k / 8f * Mathf.PI * 0.5f;
                float y = -Widest * (1f - Mathf.Cos(a));
                rings.Add(new Vector4(0f, y, 0f, BodyRadius * Mathf.Sin(a))); ys.Add(y);
            }
            for (int k = 1; k <= 10; k++)
            {
                float y = -Widest - k / 10f * 0.75f;
                rings.Add(new Vector4(0f, y, 0f, BodyR(y))); ys.Add(y);
            }
            float tailR = BodyR(-1.5f);
            for (int k = 1; k <= 6; k++)
            {
                float s = k / 6f, y = -1.5f - s * 0.45f;
                rings.Add(new Vector4(0.22f * s * s, y, -0.08f * s * s, tailR * (1f - s) + 0.015f)); ys.Add(y);
            }
            Color BodyColour(int r, float a)
            {
                float y = ys[Mathf.Clamp(r, 0, ys.Count - 1)];
                if (y > -Widest) return Color.Lerp(waterTop, waterMid, Mathf.SmoothStep(0f, 1f, -y / Widest));
                if (y > -1.45f) return Color.Lerp(waterMid, waterLow, (-y - Widest) / 0.7f);
                return Color.Lerp(waterLow, mist, Mathf.Clamp01((-y - 1.45f) / 0.45f));
            }
            Lathe(b, rings, Vector3.zero, new Vector3(0.24f, -1.96f, -0.09f), 28, BodyColour, new Vector4(0, 0, PartBody, 0), Vector4.zero, Matrix4x4.identity);

            // ---- the woven band round the brow: bold blocks, red red black white, each block its own vertices so the colours stay crisp
            {
                const int Blocks = 16, Steps = 2;
                float y0 = -0.42f, y1 = -0.29f;
                float[] bandY = { y1 + 0.012f, y1, y0, y0 - 0.012f };
                float[] push = { -0.03f, 0.028f, 0.028f, -0.03f };
                Color[] pattern = { red, red, black, white };
                for (int blk = 0; blk < Blocks; blk++)
                {
                    Color c = pattern[blk % pattern.Length];
                    int v0 = b.V;
                    for (int s = 0; s <= Steps; s++)
                    {
                        float a = (blk + s / (float)Steps) * Mathf.PI * 2f / Blocks;
                        for (int r = 0; r < bandY.Length; r++)
                        {
                            float y = bandY[r], rad = BodyR(y) + push[r];
                            var p = new Vector3(Mathf.Cos(a) * rad, y, Mathf.Sin(a) * rad);
                            // The body's own normal there: the band reads as painted on, not as a hoop.
                            var n = new Vector3(p.x / (BodyRadius * BodyRadius), (y + Widest) / (Widest * Widest), p.z / (BodyRadius * BodyRadius)).normalized;
                            b.Add(p, n, c, new Vector4(0, 0, PartBand, 0), Vector4.zero);
                        }
                    }
                    for (int s = 0; s < Steps; s++)
                        for (int r = 0; r + 1 < bandY.Length; r++)
                        {
                            int a0 = v0 + s * bandY.Length + r, a1 = a0 + bandY.Length;
                            b.Tri(a0, a1, a1 + 1); b.Tri(a0, a1 + 1, a0 + 1);
                        }
                }
            }

            // ---- the stone mask, bent round the body, and its face
            var maskC = new Vector3(0f, -0.80f, 0.86f);
            var maskR = new Vector3(0.42f, 0.34f, 0.11f);
            Vector3 Bend(Vector3 p) { float dx = p.x - maskC.x, dy = p.y - maskC.y; p.z -= 0.62f * dx * dx + 0.85f * dy * dy; return p; }
            // Where the mask's front is, at a point of its face.
            Vector3 OnMask(float x, float y, float lift)
            {
                float u = x / maskR.x, v = (y - maskC.y) / maskR.y;
                float z = maskC.z + maskR.z * Mathf.Sqrt(Mathf.Max(0f, 1f - u * u - v * v)) + lift;
                return Bend(new Vector3(x, y, z));
            }
            Ellipsoid(b, maskC, maskR, Quaternion.identity, 22, 12, stone, new Vector4(0, 0, PartStone, 0), Vector4.zero, Bend, stoneLow);
            // The brow ridge, the nose, the cheeks.
            Ellipsoid(b, OnMask(0f, -0.60f, -0.015f), new Vector3(0.30f, 0.045f, 0.045f), Quaternion.identity, 14, 6, stoneLight, new Vector4(0, 0, PartStone, 0), Vector4.zero);
            Ellipsoid(b, OnMask(0f, -0.86f, -0.012f), new Vector3(0.045f, 0.075f, 0.045f), Quaternion.identity, 10, 6, stoneLight, new Vector4(0, 0, PartStone, 0), Vector4.zero);
            for (int side = -1; side <= 1; side += 2)
            {
                Ellipsoid(b, OnMask(side * 0.27f, -0.93f, -0.006f), new Vector3(0.065f, 0.04f, 0.018f), Quaternion.identity, 10, 5, blush, new Vector4(0, side, PartStone, 0), Vector4.zero);
                // The inlay eyes, tilted back to sit on the bent mask, and a glint in each, up and in.
                var eye = OnMask(side * 0.165f, -0.75f, -0.01f);
                Ellipsoid(b, eye, new Vector3(0.095f, 0.115f, 0.035f), Quaternion.Euler(0f, side * 14f, 0f), 14, 8, inlay, new Vector4(0, side, PartInlay, 0), Vector4.zero);
                Ellipsoid(b, eye + new Vector3(-side * 0.03f + -0.012f, 0.045f, 0.03f), Vector3.one * 0.028f, Quaternion.identity, 8, 5, Color.white, new Vector4(0, side, PartGlint, 0), Vector4.zero);
            }
            var mouthAt = OnMask(0f, -1.0f, -0.008f);
            Ellipsoid(b, mouthAt, new Vector3(0.07f, 0.045f, 0.02f), Quaternion.identity, 12, 6, mouth, new Vector4(0, 0, PartMouth, 0), new Vector4(mouthAt.x, mouthAt.y, mouthAt.z, 0));

            // ---- the leaf ears: taro leaves hung from the band's sides, drooping outward, both faces
            for (int side = -1; side <= 1; side += 2)
            {
                var root = new Vector3(side * 0.66f, -0.34f, 0.02f);
                var along = new Vector3(side * 0.95f, -0.08f, 0.12f).normalized;
                var across = Vector3.Cross(along, Vector3.up).normalized;      // flat, front to back
                var up = Vector3.Cross(across, along).normalized * side;
                if (up.y < 0f) up = -up;
                const int L = 9, W = 6;
                const float Length = 0.78f, Width = 0.46f, Droop = 0.30f;
                for (int face = 0; face < 2; face++)
                {
                    int v0 = b.V, t0 = b.T.Count;
                    for (int i = 0; i <= L; i++)
                    {
                        float u = i / (float)L;
                        float w = Width * Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Pow(u, 0.75f)), 0.75f) * (1f - 0.1f * u) + 0.012f;
                        for (int j = 0; j <= W; j++)
                        {
                            float v = j / (float)W * 2f - 1f;
                            var p = root + along * (Length * u) + across * (v * w * 0.5f)
                                    + up * (Mathf.Abs(v) * 0.05f - Droop * u * u + (face == 0 ? 0.004f : -0.004f));
                            b.Add(p, up, Color.Lerp(leaf, leafTip, u), new Vector4(u, side, PartLeaf, 0), new Vector4(root.x, root.y, root.z, 0));
                        }
                    }
                    for (int i = 0; i < L; i++)
                        for (int j = 0; j < W; j++)
                        {
                            int a0 = v0 + i * (W + 1) + j, a1 = a0 + W + 1;
                            if (face == 0) { b.Tri(a0, a1, a1 + 1); b.Tri(a0, a1 + 1, a0 + 1); }
                            else { b.Tri(a0, a1 + 1, a1); b.Tri(a0, a0 + 1, a1 + 1); }
                        }
                    // Each face's normals come out on the side its winding shows, so each side lights as itself.
                    b.Normals(v0, t0);
                }
            }

            // ---- the arms: stubby, at the widest, pointing out and a little down; raised about the shoulder in the shader
            for (int side = -1; side <= 1; side += 2)
            {
                var shoulder = new Vector3(side * 0.78f, -0.98f, 0.10f);
                var turn = Quaternion.Euler(0f, 0f, side * -22f);
                Ellipsoid(b, shoulder + turn * new Vector3(side * 0.16f, 0f, 0f), new Vector3(0.22f, 0.15f, 0.15f), turn, 12, 7, Color.Lerp(waterMid, waterTop, 0.35f),
                          new Vector4(0, side, PartArm, 0), new Vector4(shoulder.x, shoulder.y, shoulder.z, 0));
            }

            return b.Make("Payo bubble spirit (built in code)", new Bounds(new Vector3(0f, -1f, 0f), new Vector3(3.2f, 3.0f, 3.0f)));
        }

        // ------------------------------------------------------------------ the bubble

        private static Mesh BuildBubble()
        {
            var b = new B();
            // An icosphere, three times divided: round enough to wobble smoothly, 1,280 triangles.
            var verts = new List<Vector3>();
            var tris = new List<int>();
            float g = (1f + Mathf.Sqrt(5f)) * 0.5f;
            Vector3[] seed = { new Vector3(-1, g, 0), new Vector3(1, g, 0), new Vector3(-1, -g, 0), new Vector3(1, -g, 0), new Vector3(0, -1, g), new Vector3(0, 1, g),
                               new Vector3(0, -1, -g), new Vector3(0, 1, -g), new Vector3(g, 0, -1), new Vector3(g, 0, 1), new Vector3(-g, 0, -1), new Vector3(-g, 0, 1) };
            foreach (var s in seed) verts.Add(s.normalized);
            int[] faces = { 0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8, 3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 };
            tris.AddRange(faces);
            for (int level = 0; level < 3; level++)
            {
                var mid = new Dictionary<long, int>();
                var next = new List<int>();
                int Mid(int i, int j)
                {
                    long key = i < j ? ((long)i << 32) | (uint)j : ((long)j << 32) | (uint)i;
                    if (mid.TryGetValue(key, out int at)) return at;
                    verts.Add((verts[i] + verts[j]).normalized);
                    mid[key] = verts.Count - 1;
                    return verts.Count - 1;
                }
                for (int t = 0; t < tris.Count; t += 3)
                {
                    int a = tris[t], c1 = tris[t + 1], c2 = tris[t + 2];
                    int ab = Mid(a, c1), bc = Mid(c1, c2), ca = Mid(c2, a);
                    next.AddRange(new[] { a, ab, ca, c1, bc, ab, c2, ca, bc, ab, bc, ca });
                }
                tris = next;
            }
            foreach (var v in verts) b.Add(v, v, Color.white, new Vector4(0, 0, 0, 0), Vector4.zero);
            // Icosphere faces as seeded wind clockwise seen from outside in Unity's left-handed frame? Measured, not assumed.
            bool outward = Vector3.Dot(Vector3.Cross(verts[tris[1]] - verts[tris[0]], verts[tris[2]] - verts[tris[0]]), verts[tris[0]]) > 0f;
            for (int t = 0; t < tris.Count; t += 3)
                if (outward) b.Tri(tris[t], tris[t + 1], tris[t + 2]); else b.Tri(tris[t], tris[t + 2], tris[t + 1]);

            // Six drips: small balls (drawn out into teardrops in the shader), each with a bearing, a phase and a speed.
            var rnd = new System.Random(4127);
            for (int d = 0; d < 6; d++)
            {
                float az = d * Mathf.PI * 2f / 6f + (float)rnd.NextDouble() * 0.7f;
                float phase = (float)rnd.NextDouble(), speed = 0.30f + 0.22f * (float)rnd.NextDouble(), size = 0.050f + 0.025f * (float)rnd.NextDouble();
                AddBall(b, 8, 6, new Vector4(az, phase, 1, size), new Vector4(0, 0, 0, speed));
            }
            // Two glints, each a quad drawn both ways round (whichever faces the eye after the turn is kept).
            Vector3[] spots = { new Vector3(-0.55f, 0.62f, 0.56f).normalized, new Vector3(0.70f, -0.20f, 0.68f).normalized };
            float[] sizes = { 0.20f, 0.13f };
            for (int s = 0; s < spots.Length; s++)
            {
                int v0 = b.V;
                Vector2[] corners = { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) };
                foreach (var c in corners) b.Add(Vector3.zero, Vector3.up, Color.white, new Vector4(c.x, c.y, 2, sizes[s]), new Vector4(spots[s].x, spots[s].y, spots[s].z, s * 0.47f));
                b.Tri(v0, v0 + 1, v0 + 2); b.Tri(v0, v0 + 2, v0 + 3);
                b.Tri(v0, v0 + 2, v0 + 1); b.Tri(v0, v0 + 3, v0 + 2);
            }
            return b.Make("Payo spirit bubble (built in code)", new Bounds(new Vector3(0f, -1f, 0f), new Vector3(2.6f, 4.8f, 2.6f)));
        }

        /// <summary>A unit ball (`seg` round, `stacks` from pole to pole) with every vertex carrying the same numbers.</summary>
        private static void AddBall(B b, int seg, int stacks, Vector4 u0, Vector4 u1)
        {
            var rings = new List<Vector4>();
            for (int i = 1; i < stacks; i++) { float a = i * Mathf.PI / stacks; rings.Add(new Vector4(0f, Mathf.Cos(a), 0f, Mathf.Sin(a))); }
            int v0 = b.V;
            Lathe(b, rings, Vector3.up, Vector3.down, seg, (r, a) => Color.white, u0, u1, Matrix4x4.identity);
            // A unit ball's normal is its position.
            for (int i = v0; i < b.V; i++) b.N[i] = b.P[i].normalized;
        }

        // ------------------------------------------------------------------ the splash

        public const int SplashDrops = 26;

        private static Mesh BuildSplash()
        {
            var b = new B();
            var rnd = new System.Random(911);
            float R() => (float)rnd.NextDouble();
            // Droplets thrown out of the skin: mostly outward and up, a few straight up, a couple low.
            for (int d = 0; d < SplashDrops; d++)
            {
                float az = d * 2.39996f + R() * 0.4f;                 // golden-angle round, so any share is spread
                float el = Mathf.Lerp(-0.15f, 1.15f, R());
                var dir = new Vector3(Mathf.Cos(az) * Mathf.Cos(el), Mathf.Sin(el), Mathf.Sin(az) * Mathf.Cos(el));
                float speed = Mathf.Lerp(2.6f, 5.4f, R());
                var vel = dir * speed + Vector3.up * 1.2f;
                float size = Mathf.Lerp(0.055f, 0.12f, R());
                AddBall(b, 7, 5, new Vector4(0, R() * 0.06f, size, (d + 0.5f) / SplashDrops), new Vector4(vel.x, vel.y, vel.z, 0));
            }
            // The rainbow ring and the floor ring: bands of 48 steps, two vertices across.
            for (int part = 1; part <= 2; part++)
            {
                const int Steps = 48;
                int v0 = b.V;
                for (int s = 0; s <= Steps; s++)
                {
                    float a = s * Mathf.PI * 2f / Steps;
                    b.Add(Vector3.zero, Vector3.up, Color.white, new Vector4(part, 0, a, 0), Vector4.zero);
                    b.Add(Vector3.zero, Vector3.up, Color.white, new Vector4(part, 1, a, 0), Vector4.zero);
                }
                for (int s = 0; s < Steps; s++)
                {
                    int a0 = v0 + s * 2;
                    b.Tri(a0, a0 + 2, a0 + 3); b.Tri(a0, a0 + 3, a0 + 1);
                }
            }
            return b.Make("Payo spirit splash (built in code)", new Bounds(Vector3.zero, new Vector3(8f, 8f, 8f)));
        }
    }
}
