using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ WHICH LOOK PHAISTER'S KIT IS DRAWN IN, WHILE THE OWNER CHOOSES (2026-10-29). Owner: *"we're reworking phaister's
    /// kit's vfx"*. The reference board offered three directions, and he asked to see two of them built before he picks:
    ///
    /// | Direction | The idea | TELEPORT | HEX mark |
    /// |---|---|---|---|
    /// | `Current` | what ships today | `PhaisterSwarm` and `PhaisterArrivalSeal` | `VoodooCursePresenter`'s line button |
    /// | `A`, moonlit moths | the moth is her unit for everything | her body drawn out into one ribbon of large moths, a ghost left behind that drips away (`PhaisterMothRibbon`) | a moth asleep on a button over a dark pool, waking when it arms (`PhaisterHexLook`) |
    /// | `B`, sleepy cosmos | violet star-flecked swirls over dark pools | a pool opens under her and its swirl swallows her, then blooms open where she lands (`PhaisterCosmosPortal`) | a cosmic bubble, a dark disc under a light swirl (`PhaisterHexLook`) |
    ///
    /// ⚠️ COSMETIC ONLY. No rule, timing, name or wire format moves with it, and every peer draws its own copy from the same
    /// replicated state, so two peers on different looks still play the same match. The default is `Current`, so the game is
    /// unchanged until he picks; the film tool (`PhaisterKitFilm`) draws all three side by side.
    ///
    /// Both new directions are painted in her own palette (`PhaisterProp`: violet 6A3AA8, deep 3E1F6E, lilac C9A2F0, moon pale
    /// F2E6DA) and are GPU-driven: static meshes built once, moved and painted in `PhaisterMoths`, `PhaisterGhost` and
    /// `PhaisterCosmos` from a clock each effect owns.
    /// </summary>
    public static class PhaisterLook
    {
        public enum Style { Current, A, B }

        /// <summary>The look the game draws. `Current` until the owner picks.</summary>
        public static Style Direction = Style.Current;

        /// <summary>A fresh material for one effect on <paramref name="go"/>, owned by it and freed with it; null if the shader is missing.</summary>
        internal static Material Paint(GameObject go, string shader)
        {
            var s = Resources.Load<Shader>("Shaders/" + shader);
            if (s == null) { Debug.LogWarning("[PhaisterLook] Shaders/" + shader + " is missing."); return null; }
            var m = new Material(s) { name = shader };
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.sharedMaterial = m;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            // The bench's own authored masks: the look lives in these pictures (docs/VFX_BENCH_METHOD.md 1a).
            foreach (var (property, mask) in new[] { ("_NoiseTex", "noise_simplex_cells"), ("_SmokeTex", "trail_smoke"), ("_CloudTex", "erode_cloud"), ("_StarTex", "flare_star") })
                if (m.HasProperty(property)) m.SetTexture(property, FxStudyKit.Mask(mask));
            VfxRenderTag.Own(go, m);
            return m;
        }

        /// <summary>An object drawing <paramref name="mesh"/> (static, shared) at the world origin; the shader places it.</summary>
        internal static GameObject Draw(string name, Transform parent, Mesh mesh)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            go.transform.localScale = Vector3.one;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>();
            VfxRenderTag.Attach(go);
            return go;
        }

        // ⚠️ Every mesh here is placed in the world by its vertex program, so its own bounds mean nothing: they are made
        // huge, or the camera would cull a pool five metres from where the mesh "is".
        private static void Unbounded(Mesh m) => m.bounds = new Bounds(Vector3.zero, Vector3.one * 2000f);

        private static Mesh _quad, _shell;

        /// <summary>One quad, corners at -1.2 to 1.2 in x and z: room for a disc of radius 1 and its soft edge.</summary>
        internal static Mesh Quad
        {
            get
            {
                if (_quad != null) return _quad;
                _quad = new Mesh { name = "PhaisterQuad" };
                const float k = 1.2f;
                _quad.vertices = new[] { new Vector3(-k, 0, -k), new Vector3(k, 0, -k), new Vector3(k, 0, k), new Vector3(-k, 0, k) };
                _quad.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
                _quad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                Unbounded(_quad);
                return _quad;
            }
        }

        /// <summary>An open tube, 40 columns round by 10 rings up, (round, up) in its first texture channel.</summary>
        internal static Mesh Shell
        {
            get
            {
                if (_shell != null) return _shell;
                const int Round = 40, Up = 10;
                var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
                for (int j = 0; j <= Up; j++)
                    for (int i = 0; i <= Round; i++) { v.Add(Vector3.zero); uv.Add(new Vector2(i / (float)Round, j / (float)Up)); }
                for (int j = 0; j < Up; j++)
                    for (int i = 0; i < Round; i++)
                    {
                        int a = j * (Round + 1) + i, b = a + 1, c = a + Round + 1, d = c + 1;
                        t.Add(a); t.Add(c); t.Add(b); t.Add(b); t.Add(c); t.Add(d);
                    }
                _shell = new Mesh { name = "PhaisterShell" };
                _shell.SetVertices(v); _shell.SetUVs(0, uv); _shell.SetTriangles(t, 0);
                Unbounded(_shell);
                return _shell;
            }
        }

        /// <summary>
        /// One moth row, typed: when it leaves (s), its lane sideways and up (m), its phase (degrees), the height it leaves her
        /// body at and the side, the height it packs back in at, its size, its wing beats a second, and whether it is a
        /// straggler that flies off instead of packing in.
        /// </summary>
        internal struct MothRow
        {
            public float Delay, LaneSide, LaneUp, Phase, LeaveHeight, LeaveSide, KnitHeight, Size, WingHz;
            public bool Straggler;
        }

        /// <summary>
        /// The mesh for <paramref name="rows"/>: three cards a moth (two wings, a body), each card carrying its row.
        /// `PhaisterMoths.shader` documents the channels.
        /// </summary>
        internal static Mesh Moths(string name, MothRow[] rows)
        {
            var v = new List<Vector3>(); var uv0 = new List<Vector4>(); var uv1 = new List<Vector4>();
            var uv2 = new List<Vector4>(); var uv3 = new List<Vector4>(); var t = new List<int>();
            for (int r = 0; r < rows.Length; r++)
            {
                var row = rows[r];
                foreach (float part in new[] { -1f, 1f, 0f })
                {
                    int at = v.Count;
                    for (int c = 0; c < 4; c++)
                    {
                        var corner = new Vector2(c == 1 || c == 2 ? 1f : 0f, c >= 2 ? 1f : 0f);
                        v.Add(Vector3.zero);
                        uv0.Add(new Vector4(corner.x, corner.y, 0f, 0f));
                        uv1.Add(new Vector4(part, r, row.Size, row.WingHz));
                        uv2.Add(new Vector4(row.Delay, row.LaneSide, row.LaneUp, row.Phase * Mathf.Deg2Rad));
                        uv3.Add(new Vector4(row.LeaveHeight, row.KnitHeight, row.LeaveSide, row.Straggler ? 1f : 0f));
                    }
                    t.Add(at); t.Add(at + 2); t.Add(at + 1); t.Add(at); t.Add(at + 3); t.Add(at + 2);
                }
            }
            var m = new Mesh { name = name };
            m.SetVertices(v); m.SetUVs(0, uv0); m.SetUVs(1, uv1); m.SetUVs(2, uv2); m.SetUVs(3, uv3); m.SetTriangles(t, 0);
            Unbounded(m);
            return m;
        }

        /// <summary>Her drawn model under a body, or null (a preview has none).</summary>
        public static Transform ModelOf(Transform her)
        {
            if (her == null) return null;
            var visual = her.GetComponentInChildren<CharacterVisual>();
            return visual != null && visual.Model != null ? visual.Model.transform : null;
        }

        /// <summary>
        /// Hides every renderer of <paramref name="model"/> that is showing, into <paramref name="hidden"/>. ⚠️ With
        /// `forceRenderingOff`, never `enabled`: first person and the capture paths own `enabled` (`PhaisterSwarm`'s rule).
        /// </summary>
        internal static void Hide(Transform model, List<Renderer> hidden)
        {
            if (model == null) return;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
                if (!r.forceRenderingOff) { r.forceRenderingOff = true; hidden.Add(r); }
        }

        /// <summary>
        /// A soft light turned to the eye (the bench's `glow_soft`, or `flare_star`): the impact frame nothing blooms for.
        /// Mesh and material go into <paramref name="owned"/> to be freed with the effect.
        /// </summary>
        internal static FxPiece Glow(Transform parent, string name, string mask, Color core, Color edge, int order, bool lying, List<Object> owned)
        {
            var mesh = FxStudyKit.Square(lying);
            var piece = new FxPiece(name, parent, mesh, FxStudyKit.Ribbon(mask), order);
            piece.Colours(edge * 0.6f, edge, core, edge).Set("_Ends", 0f, 0f, 0f).Set("_Scroll", 0f, 0f).Set("_Halo", 0f).Set("_Add", 1f)
                 .Set("_Sprite", 1f).Set("_Emit", 0f).Set("_Cut", 0f, 0.55f).Set("_Stops", 0.30f, 0.50f, 0.95f);
            owned.Add(mesh); owned.Add(piece.Material);
            return piece;
        }

        /// <summary>Where the eye is: the bench hands it in; in a match it is the main camera.</summary>
        internal static Vector3 EyeOr(Vector3? eye)
        {
            if (eye.HasValue) return eye.Value;
            var cam = Camera.main;
            return cam != null ? cam.transform.position : new Vector3(0f, 2f, -10f);
        }

        /// <summary>Frees an object in Play or out of it (the film steps these looks in the editor, where `Destroy` refuses).</summary>
        internal static void Free(Object thing)
        {
            if (thing == null) return;
            if (Application.isPlaying) Object.Destroy(thing); else Object.DestroyImmediate(thing);
        }

        internal static void Show(List<Renderer> hidden)
        {
            foreach (var r in hidden) if (r != null) r.forceRenderingOff = false;
            hidden.Clear();
        }
    }
}
