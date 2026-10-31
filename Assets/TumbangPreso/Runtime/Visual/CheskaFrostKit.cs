using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// YASMIN'S ICE, IN THE APPROVED VOCABULARY, FOR THE REST OF HER KIT. The owner approved one ice for her: GLACIAL WALL
    /// DepthB ("depth b looks good", 2026-10-18), the approved crystals (`IceCrystalKit`) drawn with
    /// `Shaders/CheskaWallBetween`, standing in the painted frost band `Shaders/CheskaWallFrost`. COLD FEET, the FROZEN
    /// casing (FROSTBITE's hit and ABSOLUTE ZERO's freeze) and ABSOLUTE ZERO's live look are drawn from those same pieces
    /// here, so all four of her abilities read as one ice.
    ///
    /// ⚠️ IT CALLS THE APPROVED LOOK, IT DOES NOT COPY IT. The crystal materials are the wall's own DepthB materials,
    /// fetched by building one wall through its public <see cref="CheskaGlacialWallLook.Build"/> and keeping what it
    /// painted with; the frost is the wall's shader and maps with its painted switch, as the wall draws it. Nothing in
    /// the wall's file is changed, and a wall in a match shares these materials (every value that differs per cast is
    /// in a property block).
    ///
    /// ⚠️ WHAT IS NEW HERE IS ONLY LAYOUT. <see cref="Bake"/> stands typed crystals anywhere (a ring, a casing) the way
    /// the wall's private bake stands them on its arc: the same vertex contract (TEXCOORD1 the crystal's foot, TEXCOORD2
    /// its birth, height, death and seed), so the shader raises each crystal out of the road and sinks it again on its
    /// own. Ice grows from the ground and ends by sinking, never by a pop or an alpha fade on a solid.
    ///
    /// Cost: a baked set is one mesh a kind, built once a session; a cast is one renderer a kind used plus its frost,
    /// shared materials, one property block write a frame. No mesh is rebuilt per frame.
    /// </summary>
    public static class CheskaFrostKit
    {
        public const int Shard = 0, Spike = 1, Stub = 2, Cluster = 3;
        private static readonly string[] KindNames = { IceCrystalKit.Shard, IceCrystalKit.Spike, IceCrystalKit.Stub, IceCrystalKit.Cluster };

        private static readonly int AgeId = Shader.PropertyToID("_Age"), LitId = Shader.PropertyToID("_Lit"),
            DieAtId = Shader.PropertyToID("_DieAt"), GrowId = Shader.PropertyToID("_GrowTime"), DieId = Shader.PropertyToID("_DieTime"),
            FrontId = Shader.PropertyToID("_Front"), ThawId = Shader.PropertyToID("_Thaw"), OpacityId = Shader.PropertyToID("_Opacity");

        /// <summary>One crystal, typed: kind, foot (metres, in the owner's space), size, lean, turn, birth and death.</summary>
        public readonly struct Crystal
        {
            public readonly int Kind;
            public readonly float Heading;          // degrees about up; the crystal's own +z (its "out") points this way
            public readonly float Radius;           // metres from the owner's origin, along Heading
            public readonly float Wide, Tall, Deep; // metres
            public readonly float LeanOut, LeanSide, Turn; // degrees; + LeanOut tips the top away from the origin
            public readonly float Birth, Death;     // seconds: when it starts to rise; how long after the sink starts it goes
            /// <summary>The height of its foot over the owner's origin (m).</summary>
            public readonly float Y;
            public Crystal(int kind, float heading, float radius, float wide, float tall, float deep,
                           float leanOut, float leanSide, float turn, float birth, float death, float y = 0f)
            {
                Kind = kind; Heading = heading; Radius = radius; Wide = wide; Tall = tall; Deep = deep;
                LeanOut = leanOut; LeanSide = leanSide; Turn = turn; Birth = birth; Death = death; Y = y;
            }
        }

        private static readonly Material[] _looks = new Material[4];
        private static Material _frost;
        private static MaterialPropertyBlock _block;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Forget()
        {
            for (int i = 0; i < 4; i++) _looks[i] = null;
            for (int i = 0; i < SlabVariants; i++) _slabs[i] = null;
            _frost = null; _rime = null; _block = null;
        }

        /// <summary>
        /// The wall's own DepthB material for a kind. Built once, by asking the approved wall to build itself with the
        /// match's numbers (what `CheskaIceVisuals.Warmup` already does behind the loading screen) and keeping what each
        /// of its crystal renderers was painted with.
        /// </summary>
        public static Material Look(int kind)
        {
            if (kind < 0 || kind > 3) return null;
            if (_looks[kind] != null) return _looks[kind];
            var temp = new GameObject("~CheskaFrostKit-look") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var wall = CheskaGlacialWallLook.Build(temp.transform, Core.CryoRules.GlacialWallArcLength,
                    Core.CryoRules.GlacialWallArcRadius, CheskaGlacialWallLook.Style.DepthB);
                if (wall != null)
                    foreach (var part in wall.Parts)
                    {
                        if (part == null) continue;
                        for (int k = 0; k < 4; k++)
                            if (part.name == "Ice-" + KindNames[k]) _looks[k] = part.sharedMaterial;
                    }
            }
            finally { Kill(temp); }
            return _looks[kind];
        }

        /// <summary>
        /// The painted frost the wall stands in (`Shaders/CheskaWallFrost`, its maps, painted on), as one material shared
        /// by every Cold Feet field, casing and Absolute Zero sheet. ⚠️ Its body is pushed a step toward cyan from the
        /// shader's near-white: under the wall the band sits beneath crystals that carry the colour, but these floors
        /// stand alone on a pale sunlit road, and a white floor there reads as glare, not as ice.
        /// </summary>
        public static Material Frost()
        {
            if (_frost != null) return _frost;
            var shader = Resources.Load<Shader>("Shaders/CheskaWallFrost");
            if (shader == null || !shader.isSupported) return null;
            _frost = new Material(shader) { name = "CheskaFrostFloor" };
            _frost.SetTexture("_MainTex", Resources.Load<Texture2D>("Textures/UltimateFx/crystal_grunge"));
            // ⚠️ NOT THE WALL'S `erode_cloud`. That map is a gradient down v (rows near v 0 average 55 of 255, its middle
            // 189), made for shells whose v is their life. The frost reads it at metres x 0.38, so every 2.63 m of road
            // along z crosses its dark rows, and c1 drew Cold Feet as two half discs with a bare 0.4 m band through the
            // middle at z = 0 (the wall's 1.5 m band never spans one). `erode_blobs` is the bench's tileable cloud: flat
            // across both axes (row spread 54, column 38), seams under 6, the same 0 to 1 spread, so the shader's
            // patches, thaw and sparkle read as designed with no band anywhere.
            _frost.SetTexture("_ErodeTex", Resources.Load<Texture2D>("Textures/UltimateFx/erode_blobs"));
            _frost.SetFloat("_Painted", 1f);
            _frost.SetColor("_Color", new Color(0.80f, 0.94f, 1.00f, 0.80f));
            _frost.SetColor("_PaintRim", new Color(0.42f, 0.74f, 0.90f, 0.95f));
            return _frost;
        }

        /// <summary>
        /// Bakes typed crystals into one mesh a kind (null where no crystal is of that kind), in the owner's space with
        /// its foot at y 0. Each vertex carries its crystal's foot and life, so the shader grows and sinks it.
        /// </summary>
        public static Mesh[] Bake(string name, Crystal[] crystals, out string report)
        {
            var meshes = new Mesh[4];
            var text = new System.Text.StringBuilder();
            for (int k = 0; k < 4; k++)
            {
                bool any = false;
                foreach (var c in crystals) any |= c.Kind == k;
                if (!any) continue;
                var probe = new GameObject("~bake") { hideFlags = HideFlags.HideAndDontSave };
                try
                {
                    var holder = IceCrystalKit.Spawn(KindNames[k], probe.transform, out _, out _);
                    if (holder == null) { text.Append(KindNames[k]).Append(" missing; "); continue; }
                    var filters = holder.GetComponentsInChildren<MeshFilter>(true);
                    // Its own bounds in the holder's space: across x, up y, through z (as the wall's bake measures it).
                    var bounds = new Bounds(); bool have = false;
                    foreach (var f in filters)
                    {
                        Matrix4x4 m = holder.transform.worldToLocalMatrix * f.transform.localToWorldMatrix;
                        foreach (var v in f.sharedMesh.vertices)
                        { var p = m.MultiplyPoint3x4(v); if (!have) { bounds = new Bounds(p, Vector3.zero); have = true; } else bounds.Encapsulate(p); }
                    }
                    var verts = new List<Vector3>(); var norms = new List<Vector3>(); var tans = new List<Vector4>();
                    var uvs = new List<Vector2>(); var feet = new List<Vector4>(); var lives = new List<Vector4>(); var tris = new List<int>();
                    int count = 0;
                    for (int i = 0; i < crystals.Length; i++)
                    {
                        var c = crystals[i];
                        if (c.Kind != k) continue;
                        count++;
                        float h = c.Heading * Mathf.Deg2Rad;
                        var foot = new Vector3(Mathf.Sin(h) * c.Radius, c.Y, Mathf.Cos(h) * c.Radius);
                        var size = new Vector3(c.Wide / Mathf.Max(0.01f, bounds.size.x), c.Tall / Mathf.Max(0.01f, bounds.size.y), c.Deep / Mathf.Max(0.01f, bounds.size.z));
                        Matrix4x4 place = Matrix4x4.TRS(foot, Quaternion.Euler(0f, c.Heading, 0f) * Quaternion.Euler(c.LeanOut, 0f, -c.LeanSide), Vector3.one)
                                        * Matrix4x4.Scale(size) * Matrix4x4.Rotate(Quaternion.Euler(0f, c.Turn, 0f))
                                        * Matrix4x4.Translate(new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z));
                        Matrix4x4 bend = place.inverse.transpose;
                        foreach (var f in filters)
                        {
                            var mesh = f.sharedMesh;
                            Matrix4x4 local = holder.transform.worldToLocalMatrix * f.transform.localToWorldMatrix;
                            Matrix4x4 m = place * local, mn = local.inverse.transpose;
                            int start = verts.Count;
                            var mv = mesh.vertices; var mnrm = mesh.normals; var mt = mesh.tangents; var mu = mesh.uv;
                            for (int q = 0; q < mv.Length; q++)
                            {
                                verts.Add(m.MultiplyPoint3x4(mv[q]));
                                Vector3 n = q < mnrm.Length ? mnrm[q] : Vector3.up;
                                norms.Add(bend.MultiplyVector(mn.MultiplyVector(n)).normalized);
                                if (q < mt.Length) { Vector3 t = m.MultiplyVector(new Vector3(mt[q].x, mt[q].y, mt[q].z)).normalized; tans.Add(new Vector4(t.x, t.y, t.z, mt[q].w)); }
                                uvs.Add(q < mu.Length ? mu[q] : Vector2.zero);
                                feet.Add(new Vector4(foot.x, foot.y, foot.z, 0f));
                                lives.Add(new Vector4(c.Birth, c.Tall, c.Death, i + 1));
                            }
                            foreach (int t in mesh.triangles) tris.Add(start + t);
                        }
                    }
                    var baked = new Mesh { name = name + "_" + KindNames[k], indexFormat = verts.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                    baked.SetVertices(verts); baked.SetNormals(norms); baked.SetUVs(0, uvs);
                    baked.SetUVs(1, feet); baked.SetUVs(2, lives);
                    baked.SetTriangles(tris, 0);
                    if (tans.Count == verts.Count) baked.SetTangents(tans); else baked.RecalculateTangents();
                    baked.RecalculateBounds();
                    // Sunk crystals sit under the road: the bounds hold them there too, or they cull while rising.
                    var b = baked.bounds; b.Encapsulate(b.min - Vector3.up * 1.5f); baked.bounds = b;
                    baked.UploadMeshData(true);
                    meshes[k] = baked;
                    text.Append(KindNames[k]).Append(": ").Append(count).Append(" crystals, ").Append(tris.Count / 3).Append(" triangles; ");
                }
                finally { Kill(probe); }
            }
            report = text.ToString();
            return meshes;
        }

        /// <summary>One renderer a baked kind under <paramref name="parent"/>, each with the wall's DepthB material.</summary>
        public static Renderer[] Draw(Transform parent, Mesh[] meshes)
        {
            var parts = new List<Renderer>();
            for (int k = 0; k < 4; k++)
            {
                if (meshes == null || meshes[k] == null) continue;
                var look = Look(k);
                if (look == null) continue;
                var go = new GameObject("Ice-" + KindNames[k]);
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = meshes[k];
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = look;
                r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
                VfxRenderTag.Attach(go);
                parts.Add(r);
            }
            return parts.ToArray();
        }

        /// <summary>A frost floor renderer for <paramref name="mesh"/> (uv the run, uv2 metres, as the wall's band).</summary>
        public static Renderer DrawFrost(Transform parent, Mesh mesh, string name, float lift = 0.012f)
        {
            var look = Frost();
            if (mesh == null || look == null) return null;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, lift, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = look;
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            VfxRenderTag.Attach(go);
            return r;
        }

        /// <summary>
        /// A round frost floor, radius 1 in its own space scaled to <paramref name="radius"/> metres. Its run goes out
        /// from the middle: uv.x is 0.5 + 0.4 r, so the frost shader's "out from the middle" (0 at the aimed point, 1 at
        /// the ends) reaches 0.8 at the rim and the painted patches stay nearly whole to the edge. uv2 is metres, so the
        /// patches keep the wall's size whatever the radius.
        /// </summary>
        public static Mesh Disc(float radius, int rings = 14, int around = 64)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var metres = new List<Vector2>(); var t = new List<int>();
            v.Add(Vector3.zero); uv.Add(new Vector2(0.5f, 0.5f)); metres.Add(Vector2.zero);
            for (int i = 1; i <= rings; i++)
            {
                float r = i / (float)rings;
                for (int j = 0; j < around; j++)
                {
                    float a = j / (float)around * Mathf.PI * 2f;
                    var p = new Vector3(Mathf.Sin(a) * r * radius, 0f, Mathf.Cos(a) * r * radius);
                    v.Add(p); uv.Add(new Vector2(0.5f + 0.4f * r, 0.5f)); metres.Add(new Vector2(p.x, p.z));
                }
            }
            for (int j = 0; j < around; j++) { t.Add(0); t.Add(1 + j); t.Add(1 + (j + 1) % around); }
            for (int i = 1; i < rings; i++)
                for (int j = 0; j < around; j++)
                {
                    int a = 1 + (i - 1) * around + j, b = 1 + (i - 1) * around + (j + 1) % around, c = a + around, d = b + around;
                    t.Add(a); t.Add(c); t.Add(b); t.Add(b); t.Add(c); t.Add(d);
                }
            var mesh = new Mesh { name = "CheskaFrostDisc" };
            mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetUVs(1, metres); mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Poses crystals: the owner's clock, when the sink starts, and how lit from inside (1 normal).</summary>
        public static void Pose(Renderer[] parts, float age, float dieAt, float lit = 1f, float grow = -1f, float die = -1f)
        {
            if (parts == null) return;
            _block ??= new MaterialPropertyBlock();
            foreach (var part in parts)
            {
                if (part == null) continue;
                part.GetPropertyBlock(_block);
                _block.SetFloat(AgeId, age);
                _block.SetFloat(DieAtId, dieAt);
                _block.SetFloat(LitId, lit);
                if (grow > 0f) _block.SetFloat(GrowId, grow);
                if (die > 0f) _block.SetFloat(DieId, die);
                part.SetPropertyBlock(_block);
            }
        }

        /// <summary>Poses a frost floor: its clock, how far it has run (0 to 1.06) and how far it has thawed (0 to 1.08).</summary>
        public static void PoseFrost(Renderer frost, float age, float front, float thaw, float opacity = 1f)
        {
            if (frost == null) return;
            _block ??= new MaterialPropertyBlock();
            frost.GetPropertyBlock(_block);
            _block.SetFloat(AgeId, age);
            _block.SetFloat(FrontId, front);
            _block.SetFloat(ThawId, thaw);
            _block.SetFloat(OpacityId, opacity);
            frost.SetPropertyBlock(_block);
        }

        // ------------------------------------------------------------------------------------------------ the slab (c3)
        //
        // The owner rejected the c1 frost floors ("they're flat and dont read as an icy floor at all") and named Greg
        // Tapper's ice sketch #27 as the reference for all of her effects. The floor is now a SLAB of ice
        // (`Shaders/CheskaIceSlab`, whose header carries the reference and the reasoning): a top, a bright bevelled lip
        // and a dark side face on the road, and a short frost feather round it (`Shaders/CheskaIceRime`).

        /// <summary>The floor's look for the material board: 0 A (deep, swirled), 1 B (wider swirl, lighter), 2 C (darkest,
        /// tight cells). The match draws <see cref="SlabVariant"/>.</summary>
        public static int SlabVariant = 0;
        public const int SlabVariants = 3;
        //                                      bodyBias depth  cell  swirl seam   gloss
        private static readonly float[,] SlabTable =
        {
            { -0.04f, 0.20f, 0.70f, 0.75f, 0.022f, 1.0f },   // A
            {  0.02f, 0.16f, 0.85f, 1.05f, 0.026f, 1.0f },   // B
            { -0.10f, 0.26f, 0.60f, 0.60f, 0.020f, 0.8f },   // C
        };
        private static readonly Material[] _slabs = new Material[SlabVariants];
        private static Material _rime;
        private static readonly int EdgeScaleId = Shader.PropertyToID("_EdgeScale"), ClipId = Shader.PropertyToID("_Clip"),
            CellId = Shader.PropertyToID("_CellSize"), LagId = Shader.PropertyToID("_SpiralLag");

        public static Material Slab(int variant = -1)
        {
            if (variant < 0) variant = SlabVariant;
            variant = Mathf.Clamp(variant, 0, SlabVariants - 1);
            if (_slabs[variant] != null) return _slabs[variant];
            var shader = Resources.Load<Shader>("Shaders/CheskaIceSlab");
            if (shader == null || !shader.isSupported) return null;
            var m = new Material(shader) { name = "CheskaIceSlab" + (char)('A' + variant) };
            m.SetVector("_KeyDir", IceCrystalKit.TowardKey);
            m.SetFloat("_BodyBias", SlabTable[variant, 0]); m.SetFloat("_DepthAmt", SlabTable[variant, 1]);
            m.SetFloat("_CellSize", SlabTable[variant, 2]); m.SetFloat("_Swirl", SlabTable[variant, 3]);
            m.SetFloat("_SeamWidth", SlabTable[variant, 4]); m.SetFloat("_Gloss", SlabTable[variant, 5]);
            return _slabs[variant] = m;
        }

        public static Material Rime()
        {
            if (_rime != null) return _rime;
            var shader = Resources.Load<Shader>("Shaders/CheskaIceRime");
            if (shader == null || !shader.isSupported) return null;
            return _rime = new Material(shader) { name = "CheskaIceRime" };
        }

        /// <summary>A slab renderer for <paramref name="mesh"/> (sub-mesh 0 the slab, 1 the frost feather if it has one).</summary>
        public static Renderer DrawSlab(Transform parent, Mesh mesh, string name, int variant = -1)
        {
            var slab = Slab(variant);
            if (mesh == null || slab == null) return null;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            var rime = Rime();
            r.sharedMaterials = mesh.subMeshCount > 1 && rime != null ? new[] { slab, rime } : new[] { slab };
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            VfxRenderTag.Attach(go);
            VfxShapes.GeneratedMeshOwner owner = go.AddComponent<VfxShapes.GeneratedMeshOwner>();
            owner.Owned = mesh;
            return r;
        }

        /// <summary>Poses a slab: how far it has formed (run units), thawed, and the sizes that differ per cast.</summary>
        public static void PoseSlab(Renderer slab, float front, float thaw, float edgeScale, Vector4? clip = null, float cell = -1f, float lag = -1f)
        {
            if (slab == null) return;
            _block ??= new MaterialPropertyBlock();
            slab.GetPropertyBlock(_block);
            _block.SetFloat(FrontId, front);
            _block.SetFloat(ThawId, thaw);
            _block.SetFloat(EdgeScaleId, edgeScale);
            if (clip.HasValue) _block.SetVector(ClipId, clip.Value);
            if (cell > 0f) _block.SetFloat(CellId, cell);
            if (lag >= 0f) _block.SetFloat(LagId, lag);
            slab.SetPropertyBlock(_block);
        }

        /// <summary>A round slab of radius <paramref name="radius"/> (its lip's outer edge is the radius exactly).</summary>
        public static Mesh SlabDisc(float radius, float thick = 0.06f, float bevel = 0.07f, float feather = 0.22f, int rings = 16, int around = 96)
        {
            var b = new SlabBuilder(thick, bevel, feather);
            float inner = radius - bevel;
            b.Top(Vector3.zero, 0f, radius);
            for (int i = 1; i <= rings; i++)
                for (int j = 0; j < around; j++)
                {
                    float r = inner * i / rings, a = j / (float)around * Mathf.PI * 2f;
                    b.Top(new Vector3(Mathf.Sin(a) * r, 0f, Mathf.Cos(a) * r), r / radius, radius - r);
                }
            for (int j = 0; j < around; j++) b.TopTri(0, 1 + j, 1 + (j + 1) % around);
            for (int i = 1; i < rings; i++)
                for (int j = 0; j < around; j++)
                {
                    int p = 1 + (i - 1) * around + j, q = 1 + (i - 1) * around + (j + 1) % around;
                    b.TopTri(p, p + around, q); b.TopTri(q, p + around, q + around);
                }
            for (int j = 0; j <= around; j++)
            {
                float a = j / (float)around * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                b.Edge(dir * radius, dir * inner, dir, 1f);
            }
            return b.Build("CheskaIceDisc");
        }

        /// <summary>
        /// A slab over the box (<paramref name="min"/>, <paramref name="max"/>, metres in the slab's own space), its run
        /// measured out from the slab's origin (the impact) to <paramref name="far"/>.
        /// </summary>
        public static Mesh SlabBox(Vector2 min, Vector2 max, float far, float thick = 0.04f, float bevel = 0.06f, float feather = 0f, float step = 0.8f)
        {
            var b = new SlabBuilder(thick, bevel, feather);
            Vector2 lo = min + Vector2.one * bevel, hi = max - Vector2.one * bevel;
            int nx = Mathf.Clamp(Mathf.CeilToInt((hi.x - lo.x) / step), 2, 64), nz = Mathf.Clamp(Mathf.CeilToInt((hi.y - lo.y) / step), 2, 64);
            float Edge(float x, float z) => Mathf.Min(Mathf.Min(x - min.x, max.x - x), Mathf.Min(z - min.y, max.y - z));
            for (int i = 0; i <= nz; i++)
                for (int j = 0; j <= nx; j++)
                {
                    float x = Mathf.Lerp(lo.x, hi.x, j / (float)nx), z = Mathf.Lerp(lo.y, hi.y, i / (float)nz);
                    b.Top(new Vector3(x, 0f, z), new Vector2(x, z).magnitude / far, Edge(x, z));
                }
            for (int i = 0; i < nz; i++)
                for (int j = 0; j < nx; j++)
                {
                    int p = i * (nx + 1) + j, q = p + 1, s = p + nx + 1, t = s + 1;
                    b.TopTri(p, s, q); b.TopTri(q, s, t);
                }
            // The lip and side, walked round the box. Each sample's inner point is on the inset rectangle, so the strips
            // meet the top's edge exactly and mitre at the corners.
            void Side(Vector2 from, Vector2 to, int count, Vector3 normal, bool first)
            {
                for (int k = first ? 0 : 1; k <= count; k++)
                {
                    var p = Vector2.Lerp(from, to, k / (float)count);
                    var q = new Vector2(Mathf.Clamp(p.x, lo.x, hi.x), Mathf.Clamp(p.y, lo.y, hi.y));
                    b.Edge(new Vector3(p.x, 0f, p.y), new Vector3(q.x, 0f, q.y), normal, p.magnitude / far);
                }
            }
            Side(new Vector2(min.x, min.y), new Vector2(max.x, min.y), nx, Vector3.back, true);
            Side(new Vector2(max.x, min.y), new Vector2(max.x, max.y), nz, Vector3.right, false);
            Side(new Vector2(max.x, max.y), new Vector2(min.x, max.y), nx, Vector3.forward, false);
            Side(new Vector2(min.x, max.y), new Vector2(min.x, min.y), nz, Vector3.left, false);
            return b.Build("CheskaIceSheet");
        }

        /// <summary>Builds a slab: top vertices and triangles, then the edge walked once round (lip, side, feather).</summary>
        private sealed class SlabBuilder
        {
            private readonly float _thick, _bevel, _feather;
            private readonly List<Vector3> _v = new List<Vector3>(), _n = new List<Vector3>();
            private readonly List<Vector2> _run = new List<Vector2>(), _m = new List<Vector2>(), _part = new List<Vector2>();
            private readonly List<int> _slab = new List<int>(), _rime = new List<int>();
            private readonly List<(Vector3 outer, Vector3 inner, Vector3 normal, float run)> _edge = new List<(Vector3, Vector3, Vector3, float)>();
            public SlabBuilder(float thick, float bevel, float feather) { _thick = thick; _bevel = bevel; _feather = feather; }

            private int Add(Vector3 p, Vector3 n, float run, float edge, float part)
            {
                _v.Add(p); _n.Add(n); _run.Add(new Vector2(run, edge)); _m.Add(new Vector2(p.x, p.z)); _part.Add(new Vector2(part, 0f));
                return _v.Count - 1;
            }
            public void Top(Vector3 p, float run, float edge) => Add(new Vector3(p.x, _thick, p.z), Vector3.up, run, edge, 0f);
            public void TopTri(int a, int b, int c) { _slab.Add(a); _slab.Add(b); _slab.Add(c); }
            public void Edge(Vector3 outer, Vector3 inner, Vector3 normal, float run) => _edge.Add((outer, inner, normal, run));

            public Mesh Build(string name)
            {
                float mid = _thick * 0.45f;
                for (int k = 0; k + 1 < _edge.Count; k++)
                {
                    var e0 = _edge[k]; var e1 = _edge[k + 1];
                    // The lip: from the top's edge down the bevel to the outer edge.
                    var lipN = (e0.normal * (_thick - mid) + Vector3.up * _bevel).normalized;
                    int a = Add(new Vector3(e0.inner.x, _thick, e0.inner.z), lipN, e0.run, _bevel, 1f);
                    int b = Add(new Vector3(e1.inner.x, _thick, e1.inner.z), lipN, e1.run, _bevel, 1f);
                    int c = Add(new Vector3(e0.outer.x, mid, e0.outer.z), lipN, e0.run, 0f, 1f);
                    int d = Add(new Vector3(e1.outer.x, mid, e1.outer.z), lipN, e1.run, 0f, 1f);
                    _slab.Add(a); _slab.Add(b); _slab.Add(c); _slab.Add(b); _slab.Add(d); _slab.Add(c);
                    // The side: straight down into the road a few centimetres, so it stands on a kerb's crown too.
                    int s0 = Add(new Vector3(e0.outer.x, mid, e0.outer.z), e0.normal, e0.run, 0f, 2f);
                    int s1 = Add(new Vector3(e1.outer.x, mid, e1.outer.z), e1.normal, e1.run, 0f, 2f);
                    int s2 = Add(new Vector3(e0.outer.x, -0.03f, e0.outer.z), e0.normal, e0.run, 0f, 2f);
                    int s3 = Add(new Vector3(e1.outer.x, -0.03f, e1.outer.z), e1.normal, e1.run, 0f, 2f);
                    _slab.Add(s0); _slab.Add(s1); _slab.Add(s2); _slab.Add(s1); _slab.Add(s3); _slab.Add(s2);
                    if (_feather <= 0f) continue;
                    // The frost feather on the road outside it: uv.y is metres out, negative.
                    int r0 = Add(new Vector3(e0.outer.x, 0.006f, e0.outer.z), Vector3.up, e0.run, 0f, 3f);
                    int r1 = Add(new Vector3(e1.outer.x, 0.006f, e1.outer.z), Vector3.up, e1.run, 0f, 3f);
                    int r2 = Add(e0.outer + e0.normal * _feather + Vector3.up * 0.006f, Vector3.up, e0.run, -_feather, 3f);
                    int r3 = Add(e1.outer + e1.normal * _feather + Vector3.up * 0.006f, Vector3.up, e1.run, -_feather, 3f);
                    _rime.Add(r0); _rime.Add(r2); _rime.Add(r1); _rime.Add(r1); _rime.Add(r2); _rime.Add(r3);
                }
                var mesh = new Mesh { name = name, indexFormat = _v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(_v); mesh.SetNormals(_n);
                mesh.SetUVs(0, _run); mesh.SetUVs(1, _m); mesh.SetUVs(2, _part);
                mesh.subMeshCount = _rime.Count > 0 ? 2 : 1;
                mesh.SetTriangles(_slab, 0);
                if (_rime.Count > 0) mesh.SetTriangles(_rime, 1);
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        internal static void Kill(Object thing)
        {
            if (thing == null) return;
            if (Application.isPlaying) Object.Destroy(thing); else Object.DestroyImmediate(thing);
        }
    }
}
