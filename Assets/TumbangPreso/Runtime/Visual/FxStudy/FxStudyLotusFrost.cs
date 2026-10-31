using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ABSOLUTE ZERO'S FLOOR: THE WALL'S ICE ON EVERY SURFACE, WITH THE LOTUS'S ICE SPIKES AT HER FEET. The owner: "for cheska,
    /// her ult can use the floor vfx used in lotus"; of the transparent sheet that followed, "the ice on the floor should have
    /// the same material as the ice wall"; of the flat star at her feet, "ice spikes not snowflake". So the floor is the approved
    /// Glacial Wall DepthB material (`CheskaFrostKit.Look`, shader `CheskaWallBetween`), called as it is, and the centre is the
    /// bench lotus's own spikes (`FxStudyLotus`, flower alone), standing 3D crystals, in that same DepthB ice (`WearWallIce`).
    ///
    /// ⚠️ THE FLOOR IS PLATES, NOT A SHEET. That shader is solid and raises each piece from a per-vertex foot and life.
    ///   * The leading edge is the outline of floes rising as the front reaches them.
    ///   * ⚠️ EVERY VERTEX IS ITS OWN FOOT. The shader narrows a piece toward its foot as it grows; with one foot a floe, the
    ///     road showed in the cracks behind the front (c6c).
    ///   * ⚠️ WHERE THEY LIE IS `CheskaIceFloorMap`: every walkable surface at its own height, and a floe never crosses a change
    ///     of height. At an edge it hangs a skirt down to the surface below, risen with it like a curtain.
    ///
    /// ⚠️ THE WALL'S MATERIAL IS NEVER WRITTEN. Its inputs are overridden on THIS renderer alone by a property block, because
    /// a floor is seen at grazing angles a wall never is. See `FloorInputs`.
    ///
    /// ⚠️ THE FRONT IS THE RULE'S: far * (1 - (1 - t / run)^2), the sum `CheskaAbsoluteZeroLook.Arrival` solves.
    ///
    /// Cost: 1 draw each for the floes, their frost and their sheen (`Shaders/CheskaIceFloorSheen`), built once a cast; the strike's light and burst; the spikes are the
    /// lotus's own, about 25 draws standing and more for an instant as they break, and none on her own client.
    /// </summary>
    public sealed class FxStudyLotusFrost : FxStudyEffect
    {
        private const float PlateMin = 0.075f, PlateMax = 0.125f, Bevel = 0.04f, Skirt = 0.03f;
        private const float PlateGrow = 0.14f, PlateSink = 0.34f, FloorContrast = 0.08f;

        // Where the crystal's bake reads a steady thickness, measured off `ice_stub_mask.png` (alpha 0.45 to 0.85 with no
        // detail over 25 texels). Any spot at all gave floes at 0 thickness, blank beside the veined ones (c6c). A floe
        // reads one spot, so floes differ as big shapes and never carry the map's own detail.
        private static readonly Vector2[] ThickSpots =
        {
            new Vector2(0.152f, 0.597f), new Vector2(0.252f, 0.523f), new Vector2(0.494f, 0.488f), new Vector2(0.842f, 0.428f), new Vector2(0.298f, 0.372f),
        };

        private readonly FxPiece _glow, _burst;
        private readonly Renderer[] _plates;
        private readonly float _far, _run, _thawStart, _thawSeconds, _lead;
        private FxStudyLotus _spikes;

        public float Far => _far;
        public string Report { get; private set; } = "";
        /// <summary>The spikes' renderers: none on the caster's own client, whose camera stands among them (`HideSpikes`).</summary>
        public Renderer[] Lotus => _spikes != null ? _spikes.Root.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
        /// <summary>The caster's own client: no spikes, because they stand 2 m tall round her camera.</summary>
        public void HideSpikes() { _spikes?.Dispose(); _spikes = null; }

        public override float Seconds => _lead + _thawStart + _thawSeconds + 0.15f;
        public override float FloorCell => 1f;
        public override Color FloorColour => new Color(0.050f, 0.058f, 0.095f);
        public override Color FloorLine => new Color(0.085f, 0.10f, 0.16f);
        public override float FloorDim => 0.35f;
        public override float FloorDimAt => 40f;
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        { eye = new Vector3(0f, 22f, -19f); focus = new Vector3(0f, 0f, 2.5f); fov = 50f; }

        /// <summary>
        /// The ice over the box from <paramref name="min"/> to <paramref name="max"/> (x, z, metres from her), the front
        /// reaching its far corner in <paramref name="run"/> seconds, held until <paramref name="thawStart"/> and gone
        /// <paramref name="thawSeconds"/> after it, lying where <paramref name="map"/> says (world space, her feet at
        /// <paramref name="origin"/>). No map: a flat floor (the bench). <paramref name="lead"/> is empty floor before 0 s.
        /// </summary>
        public FxStudyLotusFrost(Transform parent, Vector2 min, Vector2 max, float run, float thawStart, float thawSeconds, CheskaIceFloorMap map, float lead = 0f, Vector3 origin = default)
            : base("lotusfrost", parent)
        {
            _run = Mathf.Max(0.05f, run); _thawStart = thawStart; _thawSeconds = thawSeconds; _lead = lead;
            float far = 1f;
            for (int k = 0; k < 4; k++) far = Mathf.Max(far, new Vector2((k & 1) == 0 ? min.x : max.x, (k & 2) == 0 ? min.y : max.y).magnitude);
            _far = far;

            // ⚠️ THE CAST ONLY REVEALS. The floes' shape is built once with the map (`PrepareGeometry`, behind the loading screen and
            // when a stage changes layout); all a cast writes is when each floe rises and sinks. It was 46 to 54 ms on Eskinita.
            bool ownMap = map == null;
            if (ownMap)
            {
                CheskaIceFloorMap.ForgetBake();
                map = CheskaIceFloorMap.For(min, max, (Vector2 xz, List<VfxShapes.Surface> into) => into.Add(new VfxShapes.Surface(0f, true)), null, Vector3.zero, "bench");
                CheskaIceFloorMap.ForgetBake();
            }
            PrepareGeometry(map);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Reveal(map, origin);
            watch.Stop();
            Mesh plates = map.Plates, edges = map.Edges;
            string counts = map.MeshReport;
            // Only a map made for the bench goes with the effect. The holder pieces have no material and draw nothing.
            if (ownMap) { Add("plates-owner", plates, null, 0); Add("frost-owner", edges, null, 0); }
            var floes = CheskaFrostKit.Draw(Root, new[] { null, null, plates, null });
            var block = new MaterialPropertyBlock();
            foreach (var r in floes)
            {
                r.GetPropertyBlock(block);
                FloorInputs(block);
                r.SetPropertyBlock(block);
            }
            // ⚠️ WHAT MAKES IT ICE AND NOT A SWIMMING POOL ("reads as a pool" of c7d): a white frosted band round every floe, of
            // uneven width, on the wall's own pale top band. It is what draws a floe's outline at any distance, and it is big
            // shapes, one per floe, never texture.
            var frost = CheskaFrostKit.Draw(Root, new[] { null, null, edges, null });
            foreach (var r in frost)
            {
                r.GetPropertyBlock(block);
                block.SetFloat("_Rim", 0f); block.SetFloat("_Core", 0f); block.SetFloat("_Inner", 0f); block.SetFloat("_Cloud", 0f); block.SetFloat("_Depth", 0f);
                block.SetFloat("_Contrast", 0.72f);
                r.SetPropertyBlock(block);
            }
            var all = new List<Renderer>(floes); all.AddRange(frost);
            // The depth and shine: a second draw of the floes in their own shader. The wall's material and shader are untouched.
            var sheen = Sheen();
            if (sheen != null)
            {
                var over = CheskaFrostKit.Draw(Root, new[] { null, null, plates, null });
                foreach (var r in over) r.sharedMaterial = sheen;
                all.AddRange(over);
            }
            _plates = all.ToArray();
            // The meshes are in world space, shared by every cast on this map.
            foreach (var r in _plates) r.transform.localPosition = -origin;
            Report = map.Report + " Mesh: " + counts + "; this cast wrote its reveal in " + watch.Elapsed.TotalMilliseconds.ToString("0.0") + " ms";
            _spikes = new FxStudyLotus(Root, true);
            Report += "; spike renderers " + _spikes.Root.GetComponentsInChildren<Renderer>(true).Length;

            _glow = AddGlow("frost-glow", 2, FxStudyLotus.Ice, FxStudyLotus.IceDeep, lying: true);
            _glow.At.localPosition = new Vector3(0f, PlateMax + 0.03f, 0f);
            _burst = FxStudyLotus.DressBurst(AddFlat("burst", 4, "erode_radial", lying: true, erode: "erode_cloud"));
            _burst.At.localPosition = new Vector3(0f, PlateMax + 0.04f, 0f);

            // ⚠️ THE LOTUS'S ARRANGEMENT AND CLOCK, FROZEN'S ICE. The owner, showing the frozen victim's crystals: "can u make the
            // lotus spike look more like this". So these renderers alone wear the wall's DepthB material, thicker. The bench
            // lotus is not touched, and half as big again is still so they stand over a body at her feet.
            _spikes.Root.localScale = Vector3.one * 1.4f;
            WearWallIce(_spikes.Root);
        }

        private const float SpikeGirth = 1.7f;
        private const int SpikeSeeds = 5;
        private static readonly Dictionary<(Mesh, int), Mesh> WallIceMeshes = new Dictionary<(Mesh, int), Mesh>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ForgetWallIce() => WallIceMeshes.Clear();

        // The wall's shader reads a foot and a life per vertex and its "up" is the mesh's own y (`CheskaFrostKit.Bake`). So each
        // crystal part draws a copy of its mesh in its holder's space, foot at y 0, past any rise or sink: the pose stays
        // `PoseFlower`'s transforms, and `IceCrystalKit.Dress` writes the same _Age and _Lit this shader reads. One copy a
        // mesh and seed, built once a session, so the spikes do not all wear one pattern of cracks.
        private static void WearWallIce(Transform flower)
        {
            int seed = 0;
            foreach (Transform holder in flower)
            {
                int kind = holder.name.EndsWith(IceCrystalKit.Shard) ? CheskaFrostKit.Shard : holder.name.EndsWith(IceCrystalKit.Cluster) ? CheskaFrostKit.Cluster
                         : holder.name.EndsWith(IceCrystalKit.Stub) ? CheskaFrostKit.Stub : CheskaFrostKit.Spike;
                var look = CheskaFrostKit.Look(kind);
                if (look == null) continue;
                seed++;
                foreach (var filter in holder.GetComponentsInChildren<MeshFilter>(true))
                {
                    var r = filter.GetComponent<MeshRenderer>();
                    if (r == null || filter.sharedMesh == null || !filter.sharedMesh.isReadable) continue;
                    Matrix4x4 toHolder = holder.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                    filter.sharedMesh = WallIceMesh(filter.sharedMesh, toHolder, seed % SpikeSeeds);
                    filter.transform.SetParent(holder, false);
                    filter.transform.localPosition = Vector3.zero; filter.transform.localRotation = Quaternion.identity; filter.transform.localScale = Vector3.one;
                    r.sharedMaterial = look;
                }
            }
        }

        private static Mesh WallIceMesh(Mesh from, Matrix4x4 toHolder, int seed)
        {
            if (WallIceMeshes.TryGetValue((from, seed), out var made) && made != null) return made;
            var verts = from.vertices; var norms = from.normals; Matrix4x4 bend = toHolder.inverse.transpose;
            float tall = 0.05f;
            for (int i = 0; i < verts.Length; i++)
            {
                var p = toHolder.MultiplyPoint3x4(verts[i]);
                verts[i] = new Vector3(p.x * SpikeGirth, p.y, p.z * SpikeGirth);
                tall = Mathf.Max(tall, p.y);
                if (i < norms.Length) { var n = bend.MultiplyVector(norms[i]); norms[i] = new Vector3(n.x / SpikeGirth, n.y, n.z / SpikeGirth).normalized; }
            }
            var feet = new Vector4[verts.Length]; var lives = new Vector4[verts.Length];
            for (int i = 0; i < verts.Length; i++) lives[i] = new Vector4(-1000f, tall, 0f, seed * 7 + 1);
            made = new Mesh { name = from.name + " (wall ice)", hideFlags = HideFlags.HideAndDontSave, indexFormat = from.indexFormat };
            made.vertices = verts; made.normals = norms; made.uv = from.uv;
            made.SetUVs(1, feet); made.SetUVs(2, lives);
            made.triangles = from.triangles;
            made.RecalculateBounds();
            WallIceMeshes[(from, seed)] = made;
            return made;
        }

        private static Material _sheen;
        private static Material Sheen()
        {
            if (_sheen != null) return _sheen;
            var shader = Resources.Load<Shader>("Shaders/CheskaIceFloorSheen");
            if (shader == null || !shader.isSupported) return null;
            _sheen = new Material(shader) { name = "CheskaIceFloorSheen", hideFlags = HideFlags.HideAndDontSave };
            _sheen.SetVector("_KeyDir", IceCrystalKit.TowardKey);
            return _sheen;
        }

        // The wall's inputs a floor needs otherwise (the wall's own: rim 0.90, contrast 0.88, shell 0.28). Rim: at the wall's
        // strength a floor is rim from 3 m out. Contrast: an upward face is always the wall's palest band, so this pulls it
        // back toward the same material's cyan body. Shell: its thin-ice wash is everywhere at a grazing angle. Mid: see
        // ROUNDS; the wall's own, deepened, because the veins and sparkle over it lift a floor more than a crystal's bands.
        // Inner and Cracks: curved veins on a wide flat floor read as water caustics (c7b on Lagoon Cove); the shader's straight
        // cut cracks read as ice. Shimmer: its band runs by each piece's seed, and the seeds are laid along the court, so
        // one cool sweep of light crosses floe by floe during the hold.
        private static void FloorInputs(MaterialPropertyBlock block)
        {
            // Measured with a wall standing on the floor (c7w, same frame and light), wall body hue 191 sat 0.57; floor hue 192
            // sat 0.37 at contrast 0.32. Same hue, too pale, so contrast is nearly the body itself.
            block.SetFloat("_Rim", 0f); block.SetFloat("_Contrast", FloorContrast); block.SetFloat("_Shell", 0f);
            block.SetFloat("_Inner", 0.20f); block.SetFloat("_Cracks", 0.80f); block.SetFloat("_Shimmer", 0.35f); block.SetColor("_Mid", new Color(0.20f, 0.70f, 0.82f));
            block.SetFloat("_Core", 0.30f);
        }

        /// <summary>Radius of the front <paramref name="t"/> seconds after the hand-back.</summary>
        public float Front(float t)
        {
            float x = Mathf.Clamp01(t / _run);
            return _far * (1f - (1f - x) * (1f - x));
        }

        /// <summary>When the front reaches <paramref name="distance"/> metres from her (the inverse of `Front`).</summary>
        public float Reaches(float distance) => _run * (1f - Mathf.Sqrt(1f - Mathf.Clamp01(distance / _far)));

        private sealed class Streams
        {
            public readonly List<Vector3> Verts = new List<Vector3>(), Norms = new List<Vector3>();
            public readonly List<Vector2> Uvs = new List<Vector2>(), Extra = new List<Vector2>();
            public readonly List<Vector4> Feet = new List<Vector4>(), Lives = new List<Vector4>();
            public readonly List<int> Tris = new List<int>();

            // One flat triangle, front faces clockwise from outside. `ha`, `hb`, `hc` are heights over the ground under each.
            public void Tri(Vector3 ga, float ha, Vector3 gb, float hb, Vector3 gc, float hc, Vector2 spot, Vector4 life, float ea = 0f, float eb = 0f, float ec = 0f, float seed = 0f)
            {
                Vector3 a = ga + Vector3.up * ha, b = gb + Vector3.up * hb, c = gc + Vector3.up * hc;
                Vector3 n = Vector3.Cross(b - a, c - a).normalized;
                int s = Verts.Count;
                Vert(a, ga, n, spot, life, ea, seed); Vert(b, gb, n, spot, life, eb, seed); Vert(c, gc, n, spot, life, ec, seed);
                Tris.Add(s); Tris.Add(s + 1); Tris.Add(s + 2);
            }

            private void Vert(Vector3 p, Vector3 foot, Vector3 n, Vector2 spot, Vector4 life, float inside, float seed)
            { Verts.Add(p); Norms.Add(n); Uvs.Add(spot); Feet.Add(new Vector4(foot.x, foot.y, foot.z, 0f)); Lives.Add(life); Extra.Add(new Vector2(inside, seed)); }
        }

        // A polygon moved in along its corners' bisectors (`by` under 0 moves it out). Clockwise from above, so inward is right.
        private static Vector2[] Inset(Vector2[] poly, System.Func<int, float> by)
        {
            int n = poly.Length; var result = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 before = (poly[i] - poly[(i + n - 1) % n]).normalized, after = (poly[(i + 1) % n] - poly[i]).normalized;
                Vector2 r1 = new Vector2(before.y, -before.x), r2 = new Vector2(after.y, -after.x), m = (r1 + r2).normalized;
                float cos = Mathf.Max(0.5f, Vector2.Dot(m, r1));
                result[i] = poly[i] + m * (by(i) / cos);
            }
            return result;
        }

        private static List<int> EarClip(Vector2[] poly)
        {
            var tris = new List<int>(); var left = new List<int>();
            for (int i = 0; i < poly.Length; i++) left.Add(i);
            int guard = poly.Length * poly.Length + 8;
            while (left.Count > 3 && guard-- > 0)
            {
                bool cut = false;
                for (int i = 0; i < left.Count; i++)
                {
                    int a = left[(i + left.Count - 1) % left.Count], b = left[i], c = left[(i + 1) % left.Count];
                    Vector2 pa = poly[a], pb = poly[b], pc = poly[c];
                    // Clockwise: an ear turns right.
                    if ((pb.x - pa.x) * (pc.y - pa.y) - (pb.y - pa.y) * (pc.x - pa.x) >= 0f) continue;
                    bool inside = false;
                    foreach (int q in left)
                    {
                        if (q == a || q == b || q == c) continue;
                        Vector2 p = poly[q];
                        float d1 = (pb.x - pa.x) * (p.y - pa.y) - (pb.y - pa.y) * (p.x - pa.x);
                        float d2 = (pc.x - pb.x) * (p.y - pb.y) - (pc.y - pb.y) * (p.x - pb.x);
                        float d3 = (pa.x - pc.x) * (p.y - pc.y) - (pa.y - pc.y) * (p.x - pc.x);
                        if (d1 <= 0f && d2 <= 0f && d3 <= 0f) { inside = true; break; }
                    }
                    if (inside) continue;
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    left.RemoveAt(i); cut = true;
                    break;
                }
                if (!cut) break;
            }
            for (int i = 1; i < left.Count - 1; i++) { tris.Add(left[0]); tris.Add(left[i]); tris.Add(left[i + 1]); }
            return tris;
        }

        // When each floe rises and sinks, from where she stands: all a cast has to write.
        private void Reveal(CheskaIceFloorMap map, Vector3 origin)
        {
            var o = new Vector2(origin.x, origin.z);
            void Stamp(List<Vector4> lives, int from, int to, float birth, float death)
            {
                for (int v = from; v < to; v++) { var l = lives[v]; l.x = birth; l.z = death; lives[v] = l; }
            }
            foreach (var floe in map.Floes)
            {
                int f = floe.Id;
                float r = (floe.Centre - o).magnitude, share = Mathf.Clamp01(r / _far);
                float birth = Reaches(Mathf.Max(0f, r - 0.8f)) - 0.03f;
                float death = Mathf.Max(0f, (1f - share) * (_thawSeconds - PlateSink - 0.14f) + 0.05f * (FxStudyKit.Hash(f, 56) - 0.5f));
                Stamp(map.PlateLives, map.PlateFrom[f], map.PlateFrom[f + 1], birth, death);
                Stamp(map.EdgeLives, map.EdgeFrom[f], map.EdgeFrom[f + 1], birth, death);
            }
            map.Plates.SetUVs(2, map.PlateLives);
            map.Edges.SetUVs(2, map.EdgeLives);
        }

        /// <summary>The floes' meshes for this map, in world space, once. `CheskaAbsoluteZeroLook.PrepareFloor` calls it where a
        /// hitch does not show.</summary>
        public static void PrepareGeometry(CheskaIceFloorMap map)
        {
            if (map.Plates != null) return;
            var geometryWatch = System.Diagnostics.Stopwatch.StartNew();
            var s = new Streams(); var edge = new Streams();
            int drops = 0, open = 0;
            var o = Vector2.zero;
            var origin = Vector3.zero;
            Vector2 mid = (map.Min + map.Max) * 0.5f;
            float half = Mathf.Max(1f, (map.Max - map.Min).magnitude * 0.5f);
            var plateFrom = new int[map.Floes.Count + 1]; var edgeFrom = new int[map.Floes.Count + 1];
            foreach (var floe in map.Floes)
            {
                int f = floe.Id;
                plateFrom[f] = s.Verts.Count; edgeFrom[f] = edge.Verts.Count;
                Vector2 centre = floe.Centre;
                // Over uneven ground it stands a little prouder and its top is cut finer, or the road pokes through in tan wedges (c9c).
                float tall = Mathf.Lerp(PlateMin, PlateMax, FxStudyKit.Hash(f, 53)) + Mathf.Min(0.10f, floe.Range * 0.25f);
                float cut = floe.Range > 0.08f ? 1.5f : 2.2f;
                var tilt = new Vector2(FxStudyKit.Hash(f, 54) - 0.5f, FxStudyKit.Hash(f, 55) - 0.5f) * 0.018f;
                const float birth = 0f, death = 0f;
                var spot = ThickSpots[Mathf.FloorToInt(FxStudyKit.Hash(f, 57) * ThickSpots.Length) % ThickSpots.Length];
                float H(Vector2 p) => tall + Vector2.Dot(p - floe.Centre, tilt);
                // Buried by its highest corner, or the high corner of a tilted floe pokes through ahead of the front (c6).
                // And by the ground's own unevenness: on a sloping beach the waiting floe stuck up through the sand ahead of the front (c11).
                float highest = tall + Mathf.Min(0.6f, floe.Range);
                foreach (var p in floe.Poly) highest = Mathf.Max(highest, H(p));
                // The shader's shimmer peaks when frac(seed * 0.137 - age * 0.32) is 0, so this is when the sweep reaches this
                // floe: 0.8 s to 2.3 s, in order along the court. The seed moves nothing else but which cracks it wears.
                float along = Mathf.Clamp01(Vector2.Dot(centre - mid, new Vector2(0.6f, 0.8f)) / half * 0.5f + 0.5f);
                var life = new Vector4(birth, highest, death, (0.25f + 0.5f * along) / 0.137f);

                Vector3 G(Vector2 p) => new Vector3(p.x - o.x, map.Height(floe, p) - origin.y, p.y - o.y);
                var outer = Inset(floe.Poly, i => Bevel);
                var skirtOut = Inset(floe.Poly, i => -Skirt);
                var apron = Inset(floe.Poly, i => -0.40f);
                var inner = Inset(outer, i => FrostWidth(f, i));

                float floeSeed = FxStudyKit.Hash(f, 60);
                // How far inside the floe's outline: the sheen's depth grows with it, deep in the middle and pale at the bevel.
                float Inside(Vector2 p)
                {
                    float best = float.MaxValue;
                    for (int e = 0; e < outer.Length; e++)
                    {
                        Vector2 u = outer[e], w = outer[(e + 1) % outer.Length] - u;
                        float along2 = Mathf.Clamp01(Vector2.Dot(p - u, w) / Mathf.Max(1e-5f, w.sqrMagnitude));
                        best = Mathf.Min(best, (u + w * along2 - p).sqrMagnitude);
                    }
                    return Mathf.Sqrt(best);
                }
                // The top: split until no edge is long, each point on its own surface, so it follows a slope or a ramp.
                void Top(Vector2 a, Vector2 b, Vector2 c, int depth)
                {
                    float ab = (a - b).sqrMagnitude, bc = (b - c).sqrMagnitude, ca = (c - a).sqrMagnitude, big = Mathf.Max(ab, Mathf.Max(bc, ca));
                    if (big < cut * cut || depth > 6) { s.Tri(G(a), H(a), G(b), H(b), G(c), H(c), spot, life, Inside(a), Inside(b), Inside(c), floeSeed); return; }
                    if (big == ab) { var m = (a + b) * 0.5f; Top(a, m, c, depth + 1); Top(m, b, c, depth + 1); }
                    else if (big == bc) { var m = (b + c) * 0.5f; Top(a, b, m, depth + 1); Top(a, m, c, depth + 1); }
                    else { var m = (c + a) * 0.5f; Top(a, b, m, depth + 1); Top(m, b, c, depth + 1); }
                }
                var ears = EarClip(outer);
                for (int t = 0; t + 2 < ears.Count; t += 3) Top(outer[ears[t]], outer[ears[t + 1]], outer[ears[t + 2]], 0);

                // The skirt, per corner: into the road beside a neighbour on its own surface, or all the way down to what is below
                // it at a drop, and its life buries that whole length, so it rises like a curtain and hangs nothing before its time.
                int m2 = floe.Poly.Length;
                var depths = new float[m2];
                var bare = new bool[m2];
                float deepest = highest;
                for (int i = 0; i < m2; i++)
                {
                    Vector2 g2 = floe.Poly[i], past = g2 + (skirtOut[i] - g2).normalized * 0.45f;
                    float here = map.Height(floe, g2);
                    float? top = map.TopAt(past.x, past.y);
                    if (top.HasValue && Mathf.Abs(top.Value - here) <= 0.34f) { depths[i] = -0.03f; bare[i] = !map.Iced(past, here); continue; }
                    float? below = map.Below(past, here - 0.3f);
                    // ⚠️ A CURTAIN ONLY OVER A SMALL DROP. Hung down a 2.5 m terrace it was a wall of cyan across the street (c9 on
                    // Eskinita). Past a metre it is a frost lip on the edge, against whatever face is there.
                    if (below.HasValue && here - below.Value <= 1.0f) { depths[i] = below.Value - here - 0.03f; drops++; }
                    else { depths[i] = -0.35f; open++; }
                    deepest = Mathf.Max(deepest, highest - depths[i]);
                }
                var skirtLife = new Vector4(birth, deepest, death, life.w);
                for (int i = 0; i < m2; i++)
                {
                    int j = (i + 1) % m2;
                    Vector3 gi = G(outer[i]), gj = G(outer[j]);
                    Vector3 bi = new Vector3(skirtOut[i].x - o.x, gi.y, skirtOut[i].y - o.y), bj = new Vector3(skirtOut[j].x - o.x, gj.y, skirtOut[j].y - o.y);
                    // Clockwise seen from outside.
                    s.Tri(gi, H(outer[i]), bi, depths[i], gj, H(outer[j]), spot, skirtLife);
                    s.Tri(gj, H(outer[j]), bi, depths[i], bj, depths[j], spot, skirtLife);
                    // An apron under the neighbour, beside a neighbour on its own surface: a thin sheet just over the road, so the
                    // outlines two floes do not quite share (`CheskaIceFloorMap` straightens them to grid cells) never show road (c9g).
                    if (bare[i] && bare[j]) Feather(edge, map, floe, skirtOut[i], skirtOut[j], f * 64 + i, spot, life);
                    else if (depths[i] > -0.05f && depths[j] > -0.05f)
                    {
                        Vector3 ai = new Vector3(apron[i].x, gi.y, apron[i].y), aj = new Vector3(apron[j].x, gj.y, apron[j].y);
                        s.Tri(bi, 0.02f, ai, 0.02f, bj, 0.02f, spot, skirtLife);
                        s.Tri(bj, 0.02f, ai, 0.02f, aj, 0.02f, spot, skirtLife);
                    }
                    // The frost on its edge: a band of uneven width lying just on the top.
                    Vector3 gInI = new Vector3(inner[i].x - o.x, gi.y, inner[i].y - o.y), gInJ = new Vector3(inner[j].x - o.x, gj.y, inner[j].y - o.y);
                    edge.Tri(gi, H(outer[i]) + 0.008f, gj, H(outer[j]) + 0.008f, gInJ, H(inner[j]) + 0.008f, spot, life);
                    edge.Tri(gi, H(outer[i]) + 0.008f, gInJ, H(inner[j]) + 0.008f, gInI, H(inner[i]) + 0.008f, spot, life);
                }
            }

            plateFrom[map.Floes.Count] = s.Verts.Count; edgeFrom[map.Floes.Count] = edge.Verts.Count;
            var plates = Finish(s, "AbsoluteZeroPlates");
            var frostEdges = Finish(edge, "AbsoluteZeroFrostEdges");
            var box = plates.bounds;
            string counts = s.Tris.Count / 3 + " floe triangles, " + drops + " skirt corners down a drop, " + open + " open corners (nothing below); x " + box.min.x.ToString("0.0") + " to " + box.max.x.ToString("0.0")
                     + ", z " + box.min.z.ToString("0.0") + " to " + box.max.z.ToString("0.0") + " (world)";
            map.SetGeometry(plates, frostEdges, s.Lives, edge.Lives, plateFrom, edgeFrom, counts + ", built in " + geometryWatch.Elapsed.TotalMilliseconds.ToString("0.0") + " ms with the map");
        }

        // Where the ice ends on open ground: fingers of frost out onto it, each its own length, only where there is ground under
        // the tip. Clockwise from above, so outward is the left of the edge.
        private static void Feather(Streams edge, CheskaIceFloorMap map, CheskaIceFloorMap.Floe floe, Vector2 from, Vector2 to, int seed, Vector2 spot, Vector4 life)
        {
            Vector2 along = to - from;
            float length = along.magnitude;
            if (length < 0.05f) return;
            along /= length;
            var outward = new Vector2(-along.y, along.x);
            int n = 0;
            for (float t = 0.6f * FxStudyKit.Hash(seed, 70); t < length; n++)
            {
                float a = FxStudyKit.Hash(seed + n * 7, 71), b = FxStudyKit.Hash(seed + n * 7, 72), c = FxStudyKit.Hash(seed + n * 7, 73);
                float wide = Mathf.Min(0.10f + 0.24f * a, length - t);
                Vector2 p = from + along * t, q = from + along * (t + wide);
                Vector2 tip = (p + q) * 0.5f + outward * (0.12f + 0.46f * b * b) + along * (c - 0.5f) * 0.24f;
                t += wide + 0.05f + 0.55f * c;
                float foot = map.Height(floe, p);
                float? ground = map.TopAt(tip.x, tip.y);
                if (!ground.HasValue || Mathf.Abs(ground.Value - foot) > 0.2f) continue;
                edge.Tri(new Vector3(p.x, foot, p.y), 0.012f, new Vector3(tip.x, ground.Value, tip.y), 0.012f, new Vector3(q.x, map.Height(floe, q), q.y), 0.012f, spot, life);
            }
        }

        // Broken and tapered: an even band all round every floe read as white grout, a tiled floor (c8). About a third of the
        // corners pinch it to nothing, so frost gathers along some edges and leaves others bare.
        private static float FrostWidth(int floe, int corner)
        {
            float h = FxStudyKit.Hash(floe * 16 + corner, 59);
            // Thin: 21 cm was a pale strip like grout from her own eyes (c9g). The outline has to read, not to be seen as a band.
            return h < 0.40f ? 0.004f : 0.015f + 0.075f * (h - 0.40f) / 0.60f;
        }

        private static Mesh Finish(Streams s, string name)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave, indexFormat = s.Verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            mesh.SetVertices(s.Verts); mesh.SetNormals(s.Norms); mesh.SetUVs(0, s.Uvs); mesh.SetUVs(1, s.Feet); mesh.SetUVs(2, s.Lives); mesh.SetUVs(3, s.Extra);
            mesh.SetTriangles(s.Tris, 0);
            mesh.RecalculateBounds();
            // Sunk pieces sit under the road: the bounds hold them there too, as `CheskaFrostKit.Bake` does.
            var box = mesh.bounds; box.Encapsulate(box.min - Vector3.up * 7f); mesh.bounds = box;
            return mesh;
        }

        // The bench lotus's clock for this ultimate's: its flower up at the strike, standing, and snapping as the thaw comes,
        // its pieces down by the end. The bench has 3.25 s between bloom and shatter; this has under 2.
        private float SpikeClock(float t)
        {
            const float Up = 0.7f;
            float shatter = _thawStart - 0.3f, end = _thawStart + _thawSeconds;
            if (t < Up) return FxStudyLotus.BloomAge + t;
            float standing = FxStudyLotus.BloomAge + Up;
            if (t < shatter) return Mathf.Lerp(standing, FxStudyLotus.ShatterAge, (t - Up) / (shatter - Up));
            return Mathf.Lerp(FxStudyLotus.ShatterAge, FxStudyLotus.PiecesDownAge, (t - shatter) / (end - shatter));
        }

        public override void Sample(float age, Vector3 eye)
        {
            float t = age - _lead;
            CheskaFrostKit.Pose(_plates, t, _thawStart, 1f, PlateGrow, PlateSink);
            FxStudyLotus.PoseBurst(_burst, t, 0f, 3f, 11f);
            // The light is the strike's: the spikes carry the hold.
            FxStudyLotus.PoseFernGlow(_glow, t, 12f, 0f, 0.35f, 0.4f, 1f);
            if (_spikes != null) _spikes.PoseFlower(t < 0f ? 0f : SpikeClock(t));
        }
    }
}
