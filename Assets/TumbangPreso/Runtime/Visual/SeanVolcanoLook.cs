using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ RAGO'S VOLCANO AS PLAY STANDS IT: concept B, colour set 2 (owner 2026-11-23: "go with b", "i like warm basalt colour
    /// set"). A clean Mayon cone (`Models/SeanProps/Volcano/mayon_b`, built by tools/build_sean_volcano.py, 3.0 m to its rim),
    /// with the character round its foot: Dante's rocks thrust up out of the court, slabs of the court itself heaved up
    /// and cracked, lava seams across the court. Reviewed on the bench as `fxbench seanvolcanob2...` (`FxStudySeanVolcano`).
    ///
    /// What it wears, and why:
    ///   * THE BODY AND THE BROKEN FACES: `TumbangPreso/LagoonPainted` (the painted prop shader the maps use: albedo, a
    ///     tangent-space normal map read through the model's own TANGENTs, Standard light, shadows), through the material
    ///     asset `volcano_body` so the shader always ships. Not the bench's own shader: the toon shader is retiring and the
    ///     game's props are lit by the map's sun.
    ///   * THE HEAVED TOPS: THE COURT'S OWN MATERIAL, found under the foot (`FindCourt`), with uv0 rewritten from where each
    ///     piece was torn (`PaveTops`), so they are the court's paving at the court's scale, lit as the court is.
    ///   * THE LAVA: `SeanVolcanoLava`, on the volcano's own age (`_Clock`), growing down from the crater over its first
    ///     second (`_Grow`, TEXCOORD_2).
    ///   * DANTE'S ROCKS: HIS meshes (`dante_boulder`, `dante_bastion`) in HIS material (`FxStudyDanteBoulder.Stone`,
    ///     `FxStudyDanteBastion.Slab`), recoloured through its colour parameters only. They are MERGED at spawn into one mesh
    ///     per material and size band (`MergeRocks`), and burst up and are eaten through his shader's own `_Rise`/`_Sink`.
    /// </summary>
    public static class SeanVolcanoLook
    {
        public const string Folder = "Models/SeanProps/Volcano/";
        public const string ModelName = "mayon_b";

        /// <summary>Set 2, warm basalt: Dante's rock tones through `_Top`, `_Side`, `_Foot`, and the lava's scorched bank.</summary>
        public static readonly Color RockTop = Hex(0x88765F), RockSide = Hex(0x564739), RockFoot = Hex(0x261E18), Scorch = Hex(0x54453A);
        private static Color Hex(int h) => new Color(((h >> 16) & 255) / 255f, ((h >> 8) & 255) / 255f, (h & 255) / 255f);

        /// <summary>
        /// Where to look for the court's renderers. Play leaves it null (every loaded scene); a film in a preview scene hands
        /// in that scene's own renderers, which `FindObjectsByType` cannot see.
        /// </summary>
        public static Func<IEnumerable<MeshRenderer>> CourtSearch;

        /// <summary>
        /// Where the can stands. Play leaves it null (the round's own lata); a film stands its can and hands it in here.
        /// </summary>
        public static Func<Vector3?> CanSearch;

        private static Vector3? CanNow()
        {
            if (CanSearch != null) return CanSearch();
            var lata = GameServices.Round != null ? GameServices.Round.Lata : null;
            return lata != null ? lata.transform.position : (Vector3?)null;
        }

        // ⚠️ THE CAN IS NEVER STOOD AMONG THE ROCKS (live film 2, 2026-11-23). The rule keeps the volcano's foot 3.0 m from the
        // can (`SeanRules.VolcanoClearOfCan`), and B's character round its foot reaches 4.4 m: measured, at 3.0 m there is no
        // bearing clear of a rock (tools/clearance.py), so a can there stood in a heaved slab or a lava pool. So, look only:
        //   * the whole look is TURNED at spawn so its most open side (clear of slabs, court lava and pools, which cannot be
        //     taken out) faces the can, scored over 72 bearings, and
        //   * any of Dante's merged rocks still within `CanClear` of the can is left sunk for that cast (rank 9: his shader
        //     never raises a rank that far, it folds it away, see `FxStudyRock`'s held-stone branch with `_Rise.w` 1).
        // The solid, the blast, the bombs and the spot itself are untouched: nothing here is a rule.
        public const float CanClear = 0.55f;
        private static List<Vector2> _footprint;

        /// <summary>
        /// ⚠️ THE HEAVED PIECES' TOPS ARE THE COURT THEY WERE TORN FROM (owner's brief for v6: "the court's real paving texture
        /// on top, at the court's scale with its joints, and lit like the court"). The builder writes, in each top vertex's
        /// TEXCOORD_1, the court x, z (model metres, glTF axes) the piece lay at before it was heaved. Here every top's uv0 is
        /// rewritten from that through the COURT'S OWN mapping (<paramref name="courtUv"/>: a world point to the uv the
        /// ground under it draws at), so the paving's joints on a piece carry on from the ground round it, at its scale.
        /// A copy of the mesh is returned (made once, at spawn); the source is never edited.
        /// ⚠️ glTF's x arrives mirrored and every uv set's v flipped (the importer), so the stored (x, z) arrives as (u, 1 - v)
        /// and is the model's (-u, 1 - v) in Unity's axes. Measured on the bench: tops stand 0.06 m (median) from where the
        /// builder says they lay, exactly the builder's own figure; read the other way round they would be metres off.
        /// </summary>
        public static Mesh PaveTops(Mesh source, Transform node, Func<Vector3, Vector2> courtUv, out float drift)
        {
            drift = -1f;
            if (source == null || node == null || courtUv == null) return source;
            var mesh = Object.Instantiate(source);
            mesh.name = source.name + " (paved)";
            mesh.hideFlags = HideFlags.DontSave;
            var pre = new List<Vector2>(); mesh.GetUVs(1, pre);
            var pos = new List<Vector3>(); mesh.GetVertices(pos);
            if (pre.Count != pos.Count) return mesh;
            var uv = new Vector2[pos.Count];
            double off = 0;
            for (int i = 0; i < pos.Count; i++)
            {
                var court = new Vector3(-pre[i].x, 0f, 1f - pre[i].y);
                uv[i] = courtUv(node.TransformPoint(court));
                off += new Vector2(pos[i].x - court.x, pos[i].z - court.z).magnitude;
            }
            drift = pos.Count > 0 ? (float)(off / pos.Count) : -1f;
            mesh.uv = uv;
            mesh.RecalculateTangents();
            return mesh;
        }

        /// <summary>
        /// The court under <paramref name="foot"/>: the opaque renderer whose triangle lies under it, nearest its height, and
        /// that triangle's own uv mapping as a function of a world point (exact for the maps' metre-planar floors; Eskinita's
        /// `ek_floor` is u = x, v = z + 1 in glTF metres). Null when nothing readable is found: the caller falls back.
        /// </summary>
        public static Material FindCourt(Vector3 foot, Transform skip, out Func<Vector3, Vector2> uvOf, out string what)
        {
            uvOf = null; what = "no court found";
            Material flat = null; string flatWhat = null; float flatThin = float.MaxValue;
            IEnumerable<MeshRenderer> all = CourtSearch != null ? CourtSearch() : Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Material best = null; float bestGap = 0.25f;
            foreach (var r in all)
            {
                if (r == null || !r.enabled || (skip != null && r.transform.IsChildOf(skip))) continue;
                var b = r.bounds;
                if (b.size.x < 3f || b.size.z < 3f) continue;
                if (foot.x < b.min.x || foot.x > b.max.x || foot.z < b.min.z || foot.z > b.max.z) continue;
                if (foot.y < b.min.y - 0.3f || foot.y > b.max.y + 0.3f) continue;
                var filter = r.GetComponent<MeshFilter>();
                var mesh = filter != null ? filter.sharedMesh : null;
                // ⚠️ A STATIC-BATCHED COURT (every map floor marked static: Eskinita's `alley_ground`, Ilalim, Bayan) is drawn
                // from ONE combined mesh in world space, in every player build and in Play in the editor: its MeshFilter
                // then holds that combined mesh, whose submeshes are not this renderer's and which a player build will not
                // let us read. Its own collider still holds the original mesh; failing that, the court is read flat (its
                // material at a metre a repeat, the maps' floors are metre-planar), which keeps the paving and its scale
                // and may only slide its joints. A film stands outside Play and sets `PretendBatched` to see that path.
                bool batched = r.isPartOfStaticBatch || PretendBatched;
                if (batched)
                {
                    var collider = PretendBatched ? null : r.GetComponent<MeshCollider>();
                    mesh = collider != null ? collider.sharedMesh : null;
                }
                if (mesh == null || !mesh.isReadable)
                {
                    // The FLATTEST opaque thing at least 3 m across round the foot at its height is the floor: a map's
                    // ground carries kerbs and steps so its top stands over the court, and the biggest is the whole district
                    // (filmed: `district (ek_tin_grey)` won on area).
                    float thin = b.size.y;
                    if (thin < flatThin)
                    {
                        // Which of its materials is the court cannot be read either: the one named for a floor, else its first
                        // opaque one (filmed: Eskinita's ground's first is `ek_chb_warm`, its walls; its floor is `ek_floor`).
                        Material pick = null;
                        foreach (var m0 in r.sharedMaterials)
                        {
                            if (m0 == null || m0.renderQueue >= 2450) continue;
                            string n0 = m0.name.ToLowerInvariant();
                            bool floor = n0.Contains("floor") || n0.Contains("court") || n0.Contains("ground") || n0.Contains("pav");
                            if (pick == null || floor) pick = m0;
                            if (floor) break;
                        }
                        if (pick != null) { flat = pick; flatThin = thin; flatWhat = r.name + " (" + pick.name + ") read flat, its mesh " + (batched ? "batched" : "unreadable"); }
                    }
                    continue;
                }
                var mats = r.sharedMaterials;
                var verts = mesh.vertices; var uvs = mesh.uv;
                if (uvs == null || uvs.Length != verts.Length) continue;
                var toWorld = r.transform.localToWorldMatrix;
                for (int s = 0; s < mesh.subMeshCount && s < mats.Length; s++)
                {
                    var m = mats[s];
                    if (m == null || m.renderQueue >= 2450) continue;
                    var tris = mesh.GetTriangles(s);
                    for (int t = 0; t + 2 < tris.Length; t += 3)
                    {
                        Vector3 a = toWorld.MultiplyPoint3x4(verts[tris[t]]), c1 = toWorld.MultiplyPoint3x4(verts[tris[t + 1]]), c2 = toWorld.MultiplyPoint3x4(verts[tris[t + 2]]);
                        if (!Under(foot, a, c1, c2, out float wa, out float w1, out float w2)) continue;
                        float y = a.y * wa + c1.y * w1 + c2.y * w2;
                        float gap = Mathf.Abs(y - foot.y);
                        if (gap >= bestGap) continue;
                        // The triangle's affine map from world x, z to uv.
                        Vector2 ua = uvs[tris[t]], u1 = uvs[tris[t + 1]], u2 = uvs[tris[t + 2]];
                        float x0 = a.x, z0 = a.z, dx1 = c1.x - x0, dz1 = c1.z - z0, dx2 = c2.x - x0, dz2 = c2.z - z0;
                        float det = dx1 * dz2 - dx2 * dz1;
                        if (Mathf.Abs(det) < 1e-6f) continue;
                        Vector2 du1 = u1 - ua, du2 = u2 - ua;
                        bestGap = gap; best = m;
                        uvOf = w =>
                        {
                            float px = w.x - x0, pz = w.z - z0;
                            float l1 = (px * dz2 - dx2 * pz) / det, l2 = (dx1 * pz - px * dz1) / det;
                            return ua + du1 * l1 + du2 * l2;
                        };
                        what = r.name + " (" + m.name + ")";
                    }
                }
            }
            if (best == null && flat != null)
            {
                uvOf = w => new Vector2(w.x, w.z);
                what = flatWhat;
                return flat;
            }
            return best;
        }

        /// <summary>A film outside Play: treat every court as static batching leaves it in a player build.</summary>
        public static bool PretendBatched;

        // A film poses a volcano outside Play, where `Destroy` is refused.
        private static void Kill(Object o) { if (o == null) return; if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o); }

        private static bool Under(Vector3 p, Vector3 a, Vector3 b, Vector3 c, out float wa, out float wb, out float wc)
        {
            float d = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
            wa = wb = wc = 0f;
            if (Mathf.Abs(d) < 1e-8f) return false;
            wa = ((b.z - c.z) * (p.x - c.x) + (c.x - b.x) * (p.z - c.z)) / d;
            wb = ((c.z - a.z) * (p.x - c.x) + (a.x - c.x) * (p.z - c.z)) / d;
            wc = 1f - wa - wb;
            return wa >= -1e-4f && wb >= -1e-4f && wc >= -1e-4f;
        }

        private static Vector2 Turn(Vector2 p, float degrees)
        {
            // As `Quaternion.Euler(0, degrees, 0)` turns (x, z): clockwise seen from above.
            float r = degrees * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(p.x * c + p.y * s, -p.x * s + p.y * c);
        }

        // What cannot be taken out round the foot, across the court (model metres): the heaved slabs, the court lava and the
        // pools, every third vertex. Made once; the model never changes.
        private static List<Vector2> Footprint(GameObject model)
        {
            var list = new List<Vector2>();
            var toModel = model.transform.worldToLocalMatrix;
            foreach (var f in model.GetComponentsInChildren<MeshFilter>(true))
            {
                string n = f.name;
                if (n != "surround_tops" && n != "surround_slabs" && n != "surround_lava" && n != "puddles") continue;
                var mesh = f.sharedMesh;
                if (mesh == null || !mesh.isReadable) continue;
                var m = toModel * f.transform.localToWorldMatrix;
                var v = mesh.vertices;
                for (int i = 0; i < v.Length; i += 3) { var w = m.MultiplyPoint3x4(v[i]); list.Add(new Vector2(w.x, w.z)); }
            }
            return list;
        }

        // ------------------------------------------------------------------ the shared materials (made once)

        private static Material _flow, _pool, _shade, _heat;
        private static readonly Dictionary<int, Material> RockMats = new Dictionary<int, Material>();

        private static Material Lava(bool pool)
        {
            ref Material m = ref (pool ? ref _pool : ref _flow);
            if (m != null) return m;
            var shader = Shader.Find("TumbangPreso/SeanVolcanoLava");
            if (shader == null) return null;
            m = new Material(shader) { name = pool ? "SeanVolcano-pool" : "SeanVolcano-flow", hideFlags = HideFlags.HideAndDontSave };
            m.SetTexture("_CrustTex", Resources.Load<Texture2D>(Folder + "mayon_b_crust"));
            m.SetTexture("_ErodeTex", FxStudyKit.Mask("erode_cloud"));
            m.SetFloat("_Pool", pool ? 1f : 0f);
            m.SetColor("_Scorch", Scorch);
            m.SetVector("_Key", FxStudyDanteBoulder.KeyLight);
            return m;
        }

        private static Material Shade()
        {
            if (_shade != null) return _shade;
            var shader = Shader.Find("TumbangPreso/SeanVolcanoShade");
            if (shader == null) return null;
            // In play the sun throws the real shadows; this is only where a piece meets the ground, so it is lighter than
            // the bench's (0.85 there, under no sun at all).
            _shade = new Material(shader) { name = "SeanVolcano-contact", hideFlags = HideFlags.HideAndDontSave };
            _shade.SetFloat("_Strength", 0.75f);
            return _shade;
        }

        private static Material Heat()
        {
            if (_heat != null) return _heat;
            var shader = Shader.Find("TumbangPreso/SeanVolcanoRockHeat");
            if (shader == null) return null;
            _heat = new Material(shader) { name = "SeanVolcano-rockheat", hideFlags = HideFlags.HideAndDontSave };
            _heat.SetFloat("_Baked", 1f);
            return _heat;
        }

        // ⚠️ DANTE'S STONE AT ONE SIZE BAND. His wear is set PER INSTANCE on the bench (`_Reach` and the noise frequencies
        // times the rock's scale k, the widths capped inside the bake's reach), so a merged mesh cannot hold rocks of every
        // size under one material. Rocks are banded by k in steps of `Band` (each within about 7 per cent of its band's
        // middle), each band merged in its own mesh at the band's scale `kq` and drawn with the bench's own numbers for kq.
        // So his shader, his mask and his noise see exactly what they saw on the bench, to within that 7 per cent.
        private const float Band = 1.15f;

        private static Material RockMat(bool slab, int band)
        {
            int key = (slab ? 1000 : 0) + band;
            if (RockMats.TryGetValue(key, out var m) && m != null)
            {
                // A material forgets an array whenever its shader is loaded again (Dante's note in `Stone`): set every time.
                SetPalette(m);
                return m;
            }
            var source = slab ? FxStudyDanteBastion.Slab() : FxStudyDanteBoulder.Stone(true);
            if (source == null) return null;
            m = new Material(source) { name = (slab ? "SeanVolcano-bastion-" : "SeanVolcano-boulder-") + band, hideFlags = HideFlags.HideAndDontSave };
            SetPalette(m);
            m.SetColor("_Top", RockTop); m.SetColor("_Side", RockSide); m.SetColor("_Foot", RockFoot);
            m.SetColor("_FlashColor", new Color(1.0f, 0.45f, 0.14f));
            float k = Mathf.Pow(Band, band);
            float reach = (slab ? 0.070f : 0.040f) * k;
            float cap = Mathf.Min(1f, 0.8f * reach / (0.044f * 1.35f));
            m.SetFloat("_Reach", reach);
            m.SetVector("_Band", new Vector4(0.012f * cap, 0.044f * cap, 0.38f, 0.06f));
            m.SetVector("_Line", new Vector4(0.003f * cap, 0.010f * cap, 0.42f, 0.22f));
            m.SetVector("_Noise", new Vector4(0.55f * k, 0.50f * k, 0.60f, 0f));
            m.SetFloat("_Flash", 0f);
            m.SetFloat("_Heat", 0f);
            RockMats[key] = m;
            return m;
        }

        private static void SetPalette(Material m)
        {
            var linear = new List<Vector4>(16);
            foreach (var colour in FxStudyDanteBoulder.Palette) linear.Add(colour.linear);
            m.SetVectorArray("_Palette", linear);
        }

        // ------------------------------------------------------------------ the merged rocks (made once per session)

        [Serializable] private sealed class Inst { public string group, source, node; public float[] scale; }
        [Serializable] private sealed class Doc { public string concept; public Inst[] instances; }

        private sealed class RockSet
        {
            public readonly List<(Mesh mesh, Material mat, float scale)> Bands = new List<(Mesh, Material, float)>();
            public Mesh Heat;
            public int Instances, Triangles;
            public float WorstBandError;
            // Per rock: its band, its vertices there, its middle and reach across the court (model metres), and its
            // vertices in the warmth mesh (-1: none). Kept so a cast can leave single rocks sunk (`CanClear`).
            public readonly List<(int band, int start, int count, Vector2 mid, float reach, int heatStart, int heatCount)> Rocks
                = new List<(int, int, int, Vector2, float, int, int)>();
            public readonly List<List<Vector2>> Rise = new List<List<Vector2>>();
        }
        private static RockSet _rocks;

        /// <summary>
        /// Dante's rocks round the foot, MERGED: one mesh per (his material, size band), each vertex carrying in TEXCOORD_1
        /// what his shader's `_Rise`/`_Sink` read (its rock's rank out from the middle, 0 to 3, and how high it stands, in the
        /// band's own units), so they burst up centre first, past their seat and home, and are eaten going, all on the GPU.
        /// The lava's light on the faces that look at it (`SeanVolcanoRockHeat`) is worked out per vertex once and merged too.
        /// The rocks never move against the mountain, so this is built once and kept for every cast after.
        /// </summary>
        private static RockSet MergeRocks(GameObject model, List<Vector3> lavaPoints)
        {
            if (_rocks != null) return _rocks;
            var doc = JsonUtility.FromJson<Doc>(Resources.Load<TextAsset>(Folder + ModelName + "_rocks")?.text ?? "{}");
            var boulder = Resources.Load<GameObject>(FxStudyDanteBoulder.Model);
            var bastion = Resources.Load<GameObject>(FxStudyDanteBastion.Model);
            if (doc?.instances == null || boulder == null || bastion == null) return null;
            var named = new Dictionary<string, Transform>();
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) named[t.name] = t;
            var toModel = model.transform.worldToLocalMatrix;
            var groups = new Dictionary<int, (List<Vector3> p, List<Vector3> n, List<Vector2> uv, List<Vector2> rise, List<int> tri, float kq, bool slab, int band)>();
            var keys = new List<int>();
            var pending = new List<(int key, int start, int count, Vector2 mid, float reach, int heatStart, int heatCount)>();
            var heatP = new List<Vector3>(); var heatN = new List<Vector3>(); var heatC = new List<Color>(); var heatT = new List<int>();
            var set = new RockSet();
            const float HeatReach = 0.9f;
            for (int i = 0; i < doc.instances.Length; i++)
            {
                var inst = doc.instances[i];
                if (!named.TryGetValue(inst.group + "-" + i.ToString("00"), out var at)) continue;
                bool slab = inst.source.Contains("bastion");
                var mesh = MeshOf(slab ? bastion : boulder, inst.node);
                if (mesh == null || !mesh.isReadable) continue;
                float k = Mathf.Abs(inst.scale[1]);
                int band = Mathf.RoundToInt(Mathf.Log(k) / Mathf.Log(Band));
                float kq = Mathf.Pow(Band, band);
                set.WorstBandError = Mathf.Max(set.WorstBandError, Mathf.Abs(k / kq - 1f));
                int key = (slab ? 1000 : 0) + band;
                if (!groups.TryGetValue(key, out var g))
                {
                    groups[key] = g = (new List<Vector3>(), new List<Vector3>(), new List<Vector2>(), new List<Vector2>(), new List<int>(), kq, slab, band);
                    keys.Add(key);
                }
                // Model space: the node's place, the mesh centred on it as the bench does (`-mesh.bounds.center`).
                var m = toModel * at.localToWorldMatrix * Matrix4x4.Translate(-mesh.bounds.center);
                var nm = m.inverse.transpose;
                bool mirrored = m.determinant < 0f;
                var verts = mesh.vertices; var norms = mesh.normals; var uvs = mesh.uv; var tris = mesh.triangles;
                var wp = new Vector3[verts.Length];
                float top = float.MinValue; Vector3 mid = Vector3.zero;
                for (int v = 0; v < verts.Length; v++) { wp[v] = m.MultiplyPoint3x4(verts[v]); top = Mathf.Max(top, wp[v].y); mid += wp[v]; }
                mid /= Mathf.Max(1, verts.Length);
                float rank = Mathf.Clamp((new Vector2(mid.x, mid.z).magnitude - 2.1f) / 0.75f, 0f, 3f);
                int b0 = g.p.Count;
                for (int v = 0; v < verts.Length; v++)
                {
                    g.p.Add(wp[v] / kq);
                    g.n.Add(nm.MultiplyVector(norms.Length == verts.Length ? norms[v] : Vector3.up).normalized);
                    g.uv.Add(uvs.Length == verts.Length ? uvs[v] : Vector2.zero);
                    g.rise.Add(new Vector2(rank, Mathf.Max(0.05f, top) / kq));
                }
                for (int t = 0; t + 2 < tris.Length; t += 3)
                {
                    g.tri.Add(b0 + tris[t]);
                    if (mirrored) { g.tri.Add(b0 + tris[t + 2]); g.tri.Add(b0 + tris[t + 1]); }
                    else { g.tri.Add(b0 + tris[t + 1]); g.tri.Add(b0 + tris[t + 2]); }
                }
                set.Instances++; set.Triangles += tris.Length / 3;
                float reach = 0f;
                foreach (var w in wp) reach = Mathf.Max(reach, new Vector2(w.x - mid.x, w.z - mid.z).magnitude);
                pending.Add((key, b0, verts.Length, new Vector2(mid.x, mid.z), reach, -1, 0));

                // The lava's light, as the bench draws it (its four nearest lava points, a quarter metre apart at least),
                // worked out per vertex into the colour's red.
                float near = float.MaxValue;
                foreach (var p in lavaPoints) near = Mathf.Min(near, (p - mid).sqrMagnitude);
                float size = mesh.bounds.extents.magnitude * k;
                if (lavaPoints.Count == 0 || Mathf.Sqrt(near) >= 0.45f + size) continue;
                var order = new List<Vector3>(lavaPoints);
                order.Sort((a, b) => (a - mid).sqrMagnitude.CompareTo((b - mid).sqrMagnitude));
                var chosen = new List<Vector3>(4);
                foreach (var p in order)
                {
                    if (chosen.Count == 4 || (p - mid).magnitude > HeatReach * 0.5f + size) break;
                    bool apart = true;
                    foreach (var c in chosen) if ((c - p).sqrMagnitude < 0.0625f) { apart = false; break; }
                    if (apart) chosen.Add(p);
                }
                int h0 = heatP.Count;
                var last = pending[pending.Count - 1]; last.heatStart = h0; last.heatCount = verts.Length; pending[pending.Count - 1] = last;
                for (int v = 0; v < verts.Length; v++)
                {
                    var n = nm.MultiplyVector(norms.Length == verts.Length ? norms[v] : Vector3.up).normalized;
                    float warm = 0f;
                    foreach (var l in chosen)
                    {
                        var d = l - wp[v]; float r = d.magnitude;
                        float fall = Mathf.Clamp01(1f - r / HeatReach);
                        warm = Mathf.Max(warm, Mathf.Clamp01(Vector3.Dot(n, d / Mathf.Max(r, 1e-3f)) * 1.15f - 0.05f) * fall * fall);
                    }
                    heatP.Add(wp[v]); heatN.Add(n); heatC.Add(new Color(warm, 0f, 0f, 1f));
                }
                for (int t = 0; t + 2 < tris.Length; t += 3)
                {
                    heatT.Add(h0 + tris[t]);
                    if (mirrored) { heatT.Add(h0 + tris[t + 2]); heatT.Add(h0 + tris[t + 1]); }
                    else { heatT.Add(h0 + tris[t + 1]); heatT.Add(h0 + tris[t + 2]); }
                }
            }
            foreach (int gk in keys)
            {
                var g = groups[gk];
                var mesh = new Mesh { name = (g.slab ? "SeanVolcano-bastion-" : "SeanVolcano-boulder-") + g.band, hideFlags = HideFlags.HideAndDontSave };
                mesh.indexFormat = g.p.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
                mesh.SetVertices(g.p); mesh.SetNormals(g.n); mesh.SetUVs(0, g.uv); mesh.SetUVs(1, g.rise);
                mesh.SetTriangles(g.tri, 0);
                mesh.RecalculateBounds();
                // The rise lowers vertices in the shader: the bounds keep the risen shape (they are never lower than the court).
                // Kept readable: a cast that leaves a rock sunk copies its band (`CanClear`).
                mesh.UploadMeshData(false);
                set.Bands.Add((mesh, RockMat(g.slab, g.band), g.kq));
                set.Rise.Add(g.rise);
            }
            foreach (var r in pending) set.Rocks.Add((keys.IndexOf(r.key), r.start, r.count, r.mid, r.reach, r.heatStart, r.heatCount));
            if (heatP.Count > 0)
            {
                var heat = new Mesh { name = "SeanVolcano-rockheat", hideFlags = HideFlags.HideAndDontSave };
                heat.indexFormat = heatP.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
                heat.SetVertices(heatP); heat.SetNormals(heatN); heat.SetColors(heatC); heat.SetTriangles(heatT, 0);
                heat.RecalculateBounds();
                heat.UploadMeshData(false);
                set.Heat = heat;
            }
            _rocks = set;
            return set;
        }

        private static Mesh MeshOf(GameObject model, string node)
        {
            foreach (var f in model.GetComponentsInChildren<MeshFilter>(true)) if (f.name == node) return f.sharedMesh;
            foreach (var f in model.GetComponentsInChildren<MeshFilter>(true)) if (f.transform.parent != null && f.transform.parent.name == node) return f.sharedMesh;
            return null;
        }

        // ------------------------------------------------------------------ one volcano

        /// <summary>One standing volcano's look: what it built, and how it is posed each frame.</summary>
        public sealed class Live
        {
            public Transform Mountain, Surround, Rocks;
            /// <summary>The cone's drawn height above the foot at its fitted scale (the fit is measured, not assumed).</summary>
            public float DrawnHeight;
            public float Fit = 1f;
            public string Report = "";
            private readonly List<Renderer> _lava = new List<Renderer>();
            private readonly List<Renderer> _rockRenderers = new List<Renderer>();
            private Renderer _contact, _heat;
            private Mesh _paved;
            private readonly List<Mesh> _copies = new List<Mesh>();
            /// <summary>How far the look was turned about its foot to face its open side to the can (degrees), and how many of
            /// Dante's rocks were left sunk for it. The cutscene's mountain, when it is rebuilt, turns the same way.</summary>
            public float Yaw;
            public int SunkForCan;
            private float _growMax = 1f;
            private MaterialPropertyBlock _block;

            public static readonly int Grow = Shader.PropertyToID("_Grow"), Clock = Shader.PropertyToID("_Clock"), Rise = Shader.PropertyToID("_Rise"),
                SinkId = Shader.PropertyToID("_Sink"), Fade = Shader.PropertyToID("_Fade"), Amount = Shader.PropertyToID("_Amount");

            /// <summary>How long the court takes to burst up round it, past its seat and home.</summary>
            public const float BurstSeconds = 0.40f;
            /// <summary>How long the lava takes to run from the crater to the ends of its flows.</summary>
            public const float GrowSeconds = 1.0f;

            /// <summary>
            /// Every frame: <paramref name="burst"/> seconds since the court began to burst (negative before), and
            /// <paramref name="sink"/> 0 to 1 as it goes back into the court, <paramref name="sinkSeconds"/> since it began.
            /// </summary>
            public void Pose(float burst, float age, float sink, float sinkSeconds)
            {
                _block ??= new MaterialPropertyBlock();
                float u = Mathf.Clamp01(burst / BurstSeconds);
                // Up out of the court fast, a little past its seat, and pressed home (no straight tween).
                float up = burst <= 0f ? 0f : 1f - Mathf.Pow(1f - u, 3f) + 0.12f * Mathf.Sin(u * Mathf.PI) * (1f - u * 0.4f);
                if (Surround != null) Surround.localPosition = new Vector3(Surround.localPosition.x, -(1f - up) * 0.5f - 0.55f * sink * sink, Surround.localPosition.z);
                if (Rocks != null) Rocks.localPosition = new Vector3(Rocks.localPosition.x, -0.30f * sink * sink, Rocks.localPosition.z);

                // The lava runs down from the crater: quick out of it, slowing as it reaches the ends.
                float g = Mathf.Clamp01(burst / GrowSeconds);
                float grow = burst <= 0f ? -1f : g >= 1f ? 100f : (1f - Mathf.Pow(1f - g, 2.2f)) * _growMax;
                foreach (var r in _lava)
                {
                    if (r == null) continue;
                    r.GetPropertyBlock(_block);
                    _block.SetFloat(Grow, grow);
                    _block.SetFloat(Clock, 5f + Mathf.Max(0f, age));
                    r.SetPropertyBlock(_block);
                }
                foreach (var r in _rockRenderers)
                {
                    if (r == null) continue;
                    r.GetPropertyBlock(_block);
                    // His `_Rise`: (seconds, stagger per rank, how long one takes, on); `_Sink`: (seconds, stagger, how long,
                    // the outermost rank), outside first, each eaten as it sinks.
                    _block.SetVector(Rise, new Vector4(Mathf.Max(0f, burst), 0.045f, 0.34f, 1f));
                    _block.SetVector(SinkId, new Vector4(sink > 0f ? sinkSeconds : 0f, 0.07f, 0.55f, 3f));
                    r.SetPropertyBlock(_block);
                }
                if (_contact != null)
                {
                    _contact.GetPropertyBlock(_block);
                    _block.SetFloat(Fade, Mathf.Clamp01(burst / 0.30f) * (1f - sink));
                    _contact.SetPropertyBlock(_block);
                }
                if (_heat != null)
                {
                    // The lava's warmth on the rocks: on once they are home and the lava has reached them, off as they go.
                    bool on = burst > BurstSeconds + 0.1f && sink <= 0f;
                    _heat.enabled = on;
                    if (on)
                    {
                        _heat.GetPropertyBlock(_block);
                        _block.SetFloat(Amount, 0.34f * Mathf.Clamp01((burst - BurstSeconds - 0.1f) / 0.5f));
                        _heat.SetPropertyBlock(_block);
                    }
                }
            }

            public void Dispose()
            {
                if (_paved != null) Kill(_paved);
                _paved = null;
                foreach (var m in _copies) Kill(m);
                _copies.Clear();
            }

            internal void Build(Transform root, Transform body, Vector3 foot)
            {
                var source = HeroPropAssets.Load(Folder.TrimEnd('/'), ModelName);
                if (source == null) throw new InvalidOperationException("missing " + Folder + ModelName);
                var go = Object.Instantiate(source, body, false);
                go.name = "SeanVolcano-B";
                foreach (var c in go.GetComponentsInChildren<Collider>(true)) { c.enabled = false; Kill(c); }
                var named = new Dictionary<string, Transform>();
                foreach (var t in go.GetComponentsInChildren<Transform>(true)) named[t.name] = t;
                Mountain = go.transform;

                // ⚠️ THE FIT IS 1 (brief: "the model stands 3.0 m"). The builder authors it at play's size, origin at the foot's
                // middle on the ground; this measures the cone's drawn rim against `SeanRules.VolcanoHeight` and scales only
                // if the import ever disagrees.
                if (named.TryGetValue("cone", out var cone) && cone.TryGetComponent<Renderer>(out var coneR))
                {
                    float tall = coneR.bounds.max.y - foot.y;
                    Fit = tall > 0.5f ? Core.SeanRules.VolcanoHeight / tall : 1f;
                    if (Mathf.Abs(Fit - 1f) < 0.02f) Fit = 1f;
                    DrawnHeight = tall * Fit;
                }

                var body_ = Resources.Load<Material>(Folder + "volcano_body");
                var lavaPoints = new List<Vector3>();
                Surround = new GameObject("Surround").transform;
                Surround.SetParent(root, false);
                Surround.localScale = Vector3.one * Fit;
                Rocks = new GameObject("Rocks").transform;
                Rocks.SetParent(root, false);
                Rocks.localScale = Vector3.one * Fit;
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
                {
                    VfxRenderTag.Attach(r.gameObject);
                    var mats = r.sharedMaterials;
                    bool lava = false;
                    for (int k = 0; k < mats.Length; k++)
                    {
                        string n = mats[k] != null ? mats[k].name : "";
                        if (n.StartsWith("VolcanoLava")) { mats[k] = Lava(false); lava = true; }
                        else if (n.StartsWith("VolcanoPool")) { mats[k] = Lava(true); lava = true; }
                        else if (n.StartsWith("VolcanoShade")) mats[k] = Shade();
                        else if (n.StartsWith("VolcanoCourt")) mats[k] = body_;
                        else mats[k] = body_;
                    }
                    r.sharedMaterials = mats;
                    r.shadowCastingMode = lava || r.name == "slab_contact" ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
                    r.receiveShadows = !lava;
                    if (lava) _lava.Add(r);
                    var mesh = r.GetComponent<MeshFilter>()?.sharedMesh;
                    if (lava && mesh != null && mesh.isReadable)
                    {
                        var v = mesh.vertices;
                        var toModel = go.transform.worldToLocalMatrix * r.transform.localToWorldMatrix;
                        for (int k = 0; k < v.Length; k += 6) lavaPoints.Add(toModel.MultiplyPoint3x4(v[k]));
                        var g2 = new List<Vector2>(); mesh.GetUVs(2, g2);
                        foreach (var q in g2) _growMax = Mathf.Max(_growMax, q.x + 0.5f);
                    }
                }
                if (named.TryGetValue("slab_shadow", out var benchShadow)) Kill(benchShadow.gameObject);
                // What stands round the foot, sampled while it is still under the model (see `CanClear`).
                _footprint ??= Footprint(go);
                // The parts round the foot stand in the court, not on the mountain: they rise and sink on their own.
                foreach (var part in new[] { "surround_tops", "surround_slabs", "surround_lava", "slab_contact" })
                    if (named.TryGetValue(part, out var t)) t.SetParent(Surround, false);
                if (named.TryGetValue("slab_contact", out var contact)) _contact = contact.GetComponent<Renderer>();

                // Turned so its open side faces the can (see `CanClear`): before the tops are paved, so they take the court
                // where they now stand.
                var can = CanNow();
                Vector2 canAt = Vector2.zero;
                bool facing = false;
                if (can.HasValue)
                {
                    var c = root.InverseTransformPoint(can.Value);
                    canAt = new Vector2(c.x, c.z) / Mathf.Max(0.01f, Fit);
                    facing = canAt.magnitude < 6.5f;
                }
                if (facing)
                {
                    var rocksNow = MergeRocks(go, lavaPoints);
                    float bestScore = float.MinValue;
                    for (int s = 0; s < 72; s++)
                    {
                        float yaw = s * 5f;
                        var local = Turn(canAt, -yaw);
                        float clear = float.MaxValue;
                        foreach (var q in _footprint) clear = Mathf.Min(clear, (q - local).sqrMagnitude);
                        clear = Mathf.Sqrt(clear);
                        int sunk = 0;
                        if (rocksNow != null) foreach (var r in rocksNow.Rocks) if ((r.mid - local).magnitude - r.reach < CanClear) sunk++;
                        // Clear of the slabs and the lava first (to about a can and a step), then as few rocks sunk as can be.
                        float score = Mathf.Min(clear, CanClear + 0.35f) - 0.03f * sunk;
                        if (score > bestScore) { bestScore = score; Yaw = yaw; }
                    }
                    var turn = Quaternion.Euler(0f, Yaw, 0f);
                    go.transform.localRotation = turn * go.transform.localRotation;
                    Surround.localRotation = turn;
                    Rocks.localRotation = turn;
                    Report += "turned " + Yaw.ToString("0") + " deg to face the can, " + canAt.magnitude.ToString("0.00") + " m out; ";
                }

                // The tops: the court's own material and mapping, from where each piece was torn.
                if (named.TryGetValue("surround_tops", out var tops) && tops.TryGetComponent<MeshFilter>(out var topFilter) && tops.TryGetComponent<MeshRenderer>(out var topR))
                {
                    var court = FindCourt(foot, root, out var uvOf, out string what);
                    if (court == null)
                    {
                        court = Resources.Load<Material>(Folder + "volcano_paving");
                        uvOf = w => new Vector2(w.x, w.z) / 2f;
                        what = "fallback paving (no readable court under the foot)";
                    }
                    // Placed where it will stand (identity under the root) for the mapping, then left there.
                    _paved = PaveTops(topFilter.sharedMesh, tops, uvOf, out float drift);
                    topFilter.sharedMesh = _paved;
                    topR.sharedMaterial = court;
                    Report += "tops: " + what + ", " + drift.ToString("0.00") + " m mean from where they lay; ";
                }

                // Dante's rocks: merged, his material, at their size bands.
                var set = MergeRocks(go, lavaPoints);
                if (set != null)
                {
                    // The rocks still on the can this cast: their bands (and the warmth) copied with those rocks sunk.
                    var bandMesh = new Mesh[set.Bands.Count];
                    var heatMesh = set.Heat;
                    if (facing)
                    {
                        var local = Turn(canAt, -Yaw);
                        List<Vector2>[] rise = null; List<Vector3> heatP = null;
                        foreach (var r in set.Rocks)
                        {
                            if ((r.mid - local).magnitude - r.reach >= CanClear) continue;
                            SunkForCan++;
                            rise ??= new List<Vector2>[set.Bands.Count];
                            rise[r.band] ??= new List<Vector2>(set.Rise[r.band]);
                            for (int v = r.start; v < r.start + r.count; v++) rise[r.band][v] = new Vector2(9f, rise[r.band][v].y);
                            if (r.heatStart >= 0 && set.Heat != null)
                            {
                                if (heatP == null) { heatP = new List<Vector3>(); set.Heat.GetVertices(heatP); }
                                for (int v = r.heatStart; v < r.heatStart + r.heatCount; v++) heatP[v] = Vector3.zero;
                            }
                        }
                        if (rise != null)
                            for (int b = 0; b < rise.Length; b++)
                            {
                                if (rise[b] == null) continue;
                                var copy = Object.Instantiate(set.Bands[b].mesh);
                                copy.name = set.Bands[b].mesh.name + " (can clear)"; copy.hideFlags = HideFlags.HideAndDontSave;
                                copy.SetUVs(1, rise[b]);
                                _copies.Add(copy); bandMesh[b] = copy;
                            }
                        if (heatP != null)
                        {
                            heatMesh = Object.Instantiate(set.Heat);
                            heatMesh.name = set.Heat.name + " (can clear)"; heatMesh.hideFlags = HideFlags.HideAndDontSave;
                            heatMesh.SetVertices(heatP);
                            _copies.Add(heatMesh);
                        }
                        Report += SunkForCan + " of Dante's rocks left sunk for the can; ";
                    }
                    for (int b = 0; b < set.Bands.Count; b++)
                    {
                        var (shared, mat, scale) = set.Bands[b];
                        var mesh = bandMesh[b] != null ? bandMesh[b] : shared;
                        var piece = new GameObject(mesh.name);
                        piece.transform.SetParent(Rocks, false);
                        piece.transform.localScale = Vector3.one * scale;
                        piece.AddComponent<MeshFilter>().sharedMesh = mesh;
                        var mr = piece.AddComponent<MeshRenderer>();
                        mr.sharedMaterial = mat;
                        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        VfxRenderTag.Attach(piece);
                        _rockRenderers.Add(mr);
                    }
                    if (set.Heat != null && Heat() != null)
                    {
                        var glow = new GameObject("SeanVolcano-rockheat");
                        glow.transform.SetParent(Rocks, false);
                        glow.AddComponent<MeshFilter>().sharedMesh = heatMesh;
                        _heat = glow.AddComponent<MeshRenderer>();
                        _heat.sharedMaterial = Heat();
                        _heat.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        _heat.receiveShadows = false;
                        _heat.enabled = false;
                        VfxRenderTag.Attach(glow);
                    }
                    Report += "Dante's rocks: " + set.Instances + " instances merged into " + set.Bands.Count + " meshes (worst band error "
                              + (set.WorstBandError * 100f).ToString("0") + " per cent), " + set.Triangles + " triangles, warmth "
                              + (set.Heat != null ? "1 merged draw" : "none") + "; ";
                }
                // The empty rock nodes of the model have nothing left to do.
                if (named.TryGetValue("surround_rocks", out var empties)) Kill(empties.gameObject);
                Report += "lava grows to " + _growMax.ToString("0.0") + " m; fit " + Fit.ToString("0.000") + ", drawn " + DrawnHeight.ToString("0.00") + " m";
            }
        }

        /// <summary>Builds the B volcano under <paramref name="body"/> (the mountain) and <paramref name="root"/> (what stands
        /// round it). Null when its model is missing, so the caller can fall back.</summary>
        public static Live Build(Transform root, Transform body, Vector3 foot)
        {
            var live = new Live();
            try { live.Build(root, body, foot); }
            catch (Exception e)
            {
                Debug.LogWarning("[SeanVolcanoLook] " + e.Message);
                return null;
            }
            return live;
        }
    }
}
