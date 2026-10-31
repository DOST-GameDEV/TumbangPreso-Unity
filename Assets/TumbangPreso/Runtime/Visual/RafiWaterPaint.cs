using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ THE WATER OF ILYAS'S APPROVED CROSSCURRENT, AS MATERIALS ANY OF HIS OTHER EFFECTS CAN WEAR. The owner passed the
    /// current at rw8 ("cross current looks good"), and it is itself made of the water wall's pieces: the streak sheet
    /// (`water_flow`), the fast white lines (`water_lines`), the foam lobes (`water_foam`), the splash lobes (`water_crest`),
    /// the ground arc (`water_ring`) and the fat drops (`water_drop`), on `FxStudyRibbon.shader` in the wall's colours
    /// (`FxStudyRafiWall.Look`). SKIM and BAHA are drawn from these, so his kit reads as one element.
    ///
    /// ⚠️ EVERY NUMBER HERE IS THE CURRENT'S OR THE WALL'S, named where it is set. What is new is only which picture goes on
    /// which shape, and the size of the shapes, which belongs to each ability (the owner found reused pieces "a bit
    /// cartoony" when they were stretched to a job they were not made for).
    ///
    /// ⚠️ DAYLIGHT. Built with the match's share of light laid on (`RafiWallCurtain.DayLight`, 0.27 against the bench's
    /// 0.45), the share the wall and the current were filmed on the sunlit court with: the rest of every piece COVERS what is
    /// behind it, because additive water goes pale on a bright road with no bloom.
    ///
    /// ⚠️ ONE MATERIAL PER PICTURE, SHARED BY EVERY CAST, and everything that changes from frame to frame (the clock, how
    /// much shows, how far it is eaten, the open window, the tiling) goes in each renderer's property block. Nothing is
    /// made when a piece is posed. The materials are never freed: there are eight, for the life of the game.
    /// </summary>
    public static class RafiWaterPaint
    {
        public const int QRing = 3000, QTrack = 3001, QFlood = 3002, QSheet = 3004, QLines = 3005, QLip = 3006, QCrest = 3008, QDrop = 3009;
        private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        /// <summary>The look the match draws with: the wall's colours with the match's share of light.</summary>
        public static FxStudyRafiWall.Look Day
        {
            get
            {
                var look = FxStudyRafiWall.Look.Bench;
                look.Light = RafiWallCurtain.StagedLight >= 0f ? RafiWallCurtain.StagedLight : RafiWallCurtain.DayLight;
                return look;
            }
        }

        private static Material Paint(string key, string mask, string erode, int queue, System.Action<Material, FxStudyRafiWall.Look> dress)
        {
            var look = Day;
            key += "@" + Mathf.RoundToInt(look.Light * 100f);
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = FxStudyKit.Ribbon(mask, erode);
            if (m == null) return null;
            m.name = "Ilyas water " + key; m.renderQueue = queue;
            dress(m, look);
            Cache[key] = m;
            return m;
        }

        private static void Colours(Material m, Color deep, Color body, Color hot, Color halo)
        { m.SetColor("_Deep", deep); m.SetColor("_Body", body); m.SetColor("_Hot", hot); m.SetColor("_HaloColor", halo); }
        private static void V(Material m, string n, float x, float y = 0f, float z = 0f, float w = 0f) => m.SetVector(n, new Vector4(x, y, z, w));
        private static void F(Material m, string n, float x) => m.SetFloat(n, x);

        /// <summary>The current's track on the ground (`kit.Track`): a wet streaked sheet, darker than the body.</summary>
        public static Material Track => Paint("track", "water_flow", "erode_cloud", QTrack, (m, l) =>
        {
            Colours(m, l.Deep, Color.Lerp(l.Deep, l.Body, 0.5f), l.Body, l.Deep);
            V(m, "_Cut", 0.10f, 0.85f); V(m, "_Stops", 0.40f, 0.95f, 1.0f); V(m, "_Ends", 0.04f, 0.10f, 0.30f); V(m, "_Scroll", 0.9f, 0f);
            F(m, "_Halo", 0f); F(m, "_Emit", 0f); F(m, "_Add", l.Light * 0.6f); F(m, "_Soft", 0.2f); F(m, "_Sweep", 1.2f);
            V(m, "_Hue", l.HueDrift, 0f, 0.8f, 1f);
        });

        /// <summary>
        /// A flood's body on the ground: the current's track, with more of its body colour and a solid floor, since it is
        /// six metres of water on a pale road and not a thin wet line. (The wall's back sheet's cut and stops.)
        /// </summary>
        public static Material Flood => Paint("flood", "water_flow", "erode_cloud", QFlood, (m, l) =>
        {
            Colours(m, l.Deep, Color.Lerp(l.Deep, l.Body, 0.6f), l.Body, l.Deep);
            V(m, "_Cut", 0.0f, 0.55f); V(m, "_Stops", 0.30f, 0.85f, 1.0f); V(m, "_Ends", 0f, 0f, 0.06f); V(m, "_Scroll", 1.1f, 0.05f);
            V(m, "_ErodeScroll", 0.6f, 0f); F(m, "_Sweep", 1.4f); F(m, "_Soft", 0.22f);
            F(m, "_Halo", 0f); F(m, "_Emit", 0f); F(m, "_Add", l.Light * 0.5f); V(m, "_Hue", l.HueDrift, 4f, 0.9f, 1f);
        });

        /// <summary>
        /// ⚠️ THE BODY UNDER THE PICTURES (k2, 2026-10-29). The wall's and the current's veil (`kit.Veil`): no picture, so
        /// an even mask reads the body colour everywhere, a little eaten by the cloud map so its edge is never ruled. The
        /// k1 film showed why it is needed: the streak picture alone, laid on a pale road, read as "a carpet of thin blue
        /// strands, like scribbles, grass or hair" and not as water. Every broad surface of Baha and SKIM's track stands on
        /// this. `Wet` is the one lying on the ground (deeper, fading at its two long sides); `Veil` stands up.
        /// </summary>
        public static Material Veil => Paint("veil", null, "erode_cloud", QFlood, (m, l) =>
        {
            Colours(m, l.Deep, l.Body, l.Body, l.Body);
            V(m, "_Cut", 0f, 1f); V(m, "_Stops", 0.5f, 2f, 3f); V(m, "_Ends", 0.10f, 0.02f, 0f); V(m, "_Scroll", 0f, 0f);
            F(m, "_Halo", 0f); F(m, "_Emit", 0f); F(m, "_Add", l.Light * 0.7f); F(m, "_Sweep", 1.4f); F(m, "_Soft", 0.2f);
            V(m, "_Hue", l.HueDrift, 0f, 0.6f, 1f);
        });

        public static Material Wet => Paint("wet", null, "erode_cloud", QTrack, (m, l) =>
        {
            Colours(m, l.Deep, Color.Lerp(l.Deep, l.Body, 0.75f), l.Body, l.Body);
            V(m, "_Cut", 0f, 1f); V(m, "_Stops", 0.5f, 2f, 3f); V(m, "_Ends", 0.02f, 0.02f, 0.18f); V(m, "_Scroll", 0f, 0f);
            V(m, "_ErodeScroll", 0.15f, 0f);
            F(m, "_Halo", 0f); F(m, "_Emit", 0f); F(m, "_Add", l.Light * 0.5f); F(m, "_Sweep", 1.2f); F(m, "_Soft", 0.25f);
            V(m, "_Hue", l.HueDrift, 0f, 0.8f, 1f);
        });

        /// <summary>Broad foam patches drifting on a body of water: the wall's foam picture lying flat, tiled large.</summary>
        public static Material FoamPatches => Paint("foampatch", "water_foam", "erode_cloud", QSheet, (m, l) =>
        {
            Colours(m, l.Body, l.Pale, l.Foam, l.Pale);
            V(m, "_Cut", 0.30f, 0.62f); V(m, "_Stops", 0.30f, 0.62f, 0.90f); V(m, "_Ends", 0f, 0f, 0.15f); V(m, "_Scroll", 0.35f, 0.04f);
            V(m, "_ErodeScroll", 0.2f, 0f); F(m, "_Sweep", 1.2f); F(m, "_Soft", 0.18f);
            F(m, "_Halo", 0f); F(m, "_Emit", 0.2f); F(m, "_Add", Mathf.Lerp(0.2f, 0.6f, l.Light)); V(m, "_Warp", 0.03f, 2f, 0.4f, 1.6f);
        });

        // ---- the wall's lit accents (`RafiWaterVisual.WallGlow`). The owner: "can the waterwall have some emmissives on it".
        // ⚠️ NOTHING BLOOMS IN A MATCH, so "lit" is VALUE, not light added: a near-white core inside a saturated cyan edge,
        // drawn mostly as COVER (`_Add` low), on the thin cores of pictures the wall already wears. The cut is raised well over
        // the wall's own, so only the brightest threads of each picture survive: white stays in thin lines, never areas.
        public static readonly Color GlowEdge = new Color(0.00f, 0.66f, 0.90f), GlowBody = new Color(0.22f, 0.90f, 1.00f), GlowCore = new Color(0.90f, 1.00f, 1.00f);

        /// <summary>The lines down the sheet, lit from within: the wall's `water_lines`, cut to their cores.</summary>
        public static Material GlowLines => Paint("glowlines", "water_lines", "water_flow", 3012, (m, l) =>
        {
            Colours(m, GlowEdge, GlowBody, GlowCore, GlowBody);
            V(m, "_Cut", 0.46f, 0.74f); V(m, "_Stops", 0.30f, 0.55f, 0.92f); V(m, "_Ends", 0.05f, 0.14f, 0.06f);
            F(m, "_Soft", 0.20f); F(m, "_Halo", 0f); F(m, "_Emit", 1.0f); F(m, "_Add", 0.30f);
        });

        /// <summary>The crest's lobes lit along their brightest rims: the wall's lip picture, cut to its cores.</summary>
        public static Material GlowCrest => Paint("glowcrest", "water_foam", "erode_cloud", 3013, (m, l) =>
        {
            Colours(m, GlowEdge, GlowBody, GlowCore, GlowBody);
            V(m, "_Cut", 0.50f, 0.74f); V(m, "_Stops", 0.30f, 0.58f, 0.92f); V(m, "_Ends", 0.07f, 0.07f, 0f);
            F(m, "_Soft", 0.15f); F(m, "_Halo", 0f); F(m, "_Emit", 1.0f); F(m, "_Add", 0.30f);
        });

        /// <summary>The seam where the wall meets the ground: an even soft cyan band, broken by the cloud map, no white.</summary>
        public static Material GlowSeam => Paint("glowseam", null, "erode_cloud", 3011, (m, l) =>
        {
            Colours(m, GlowEdge, GlowBody, GlowBody, GlowBody);
            V(m, "_Cut", 0f, 1f); V(m, "_Stops", 0.5f, 2f, 3f); V(m, "_Ends", 0.04f, 0.04f, 0.45f); V(m, "_Scroll", 0f, 0f);
            V(m, "_ErodeScroll", 0.25f, 0f); F(m, "_Soft", 0.25f); F(m, "_Halo", 0f); F(m, "_Emit", 0.6f); F(m, "_Add", 0.35f);
        });

        /// <summary>The streaked sheet (`kit.Sheet`, the wall's, number for number).</summary>
        public static Material Sheet => Paint("sheet", "water_flow", "water_flow", QSheet, (m, l) =>
        {
            Colours(m, l.Deep, l.Body, l.Pale, l.Body);
            V(m, "_Cut", 0.05f, 0.70f); V(m, "_Stops", 0.30f, 0.80f, 1.0f); V(m, "_Ends", 0.05f, 0.10f, 0.08f); V(m, "_Scroll", 2.3f, -0.10f);
            V(m, "_ErodeScroll", 2.3f, -0.10f); F(m, "_Sweep", -1.2f); F(m, "_Soft", 0.22f);
            F(m, "_Halo", 0f); F(m, "_Emit", 0.2f); F(m, "_Add", l.Light * 0.8f); V(m, "_Hue", l.HueDrift, 5f, 0.2f, 1f);
            V(m, "_Warp", 0.03f, 2.5f, 0.4f, 3.2f);
        });

        /// <summary>The thin fast white lines (`kit.Lines`).</summary>
        public static Material Lines => Paint("lines", "water_lines", "water_flow", QLines, (m, l) =>
        {
            Colours(m, l.Body, l.Pale, l.Foam, l.Pale);
            V(m, "_Cut", 0.22f, 0.70f); V(m, "_Stops", 0.30f, 0.50f, 0.90f); V(m, "_Ends", 0.05f, 0.14f, 0.08f); V(m, "_Scroll", 3.6f, -0.15f);
            V(m, "_ErodeScroll", 0.9f, 0f); F(m, "_Sweep", -1.0f); F(m, "_Soft", 0.20f);
            F(m, "_Halo", 0f); F(m, "_Emit", 0.5f); F(m, "_Add", Mathf.Lerp(0.5f, 0.9f, l.Light)); V(m, "_Warp", 0.03f, 2.5f, 0.4f, 3.2f);
        });

        /// <summary>The foam lip (`kit.Lip`): lobes rolling along the top of a front.</summary>
        public static Material Lip => Paint("lip", "water_foam", "erode_cloud", QLip, (m, l) =>
        {
            Colours(m, l.Body, l.Pale, l.Foam, l.Pale);
            V(m, "_Cut", 0.10f, 0.40f); V(m, "_Stops", 0.30f, 0.66f, 0.92f); V(m, "_Ends", 0.06f, 0.06f, 0f); V(m, "_Scroll", 0.55f, 0f);
            V(m, "_ErodeScroll", 0.3f, 0f); F(m, "_Sweep", 1.2f); F(m, "_Soft", 0.15f);
            F(m, "_Halo", 0f); F(m, "_Emit", 0.3f); F(m, "_Add", Mathf.Lerp(0.2f, 0.6f, l.Light)); V(m, "_Warp", 0.04f, 2.5f, 0.4f, 3.2f);
        });

        /// <summary>The splash lobes laid crosswise so each lobe runs root to tip (`kit.Collar`).</summary>
        public static Material Crest => Paint("crest", "water_crest", "erode_cloud", QCrest, (m, l) =>
        {
            Colours(m, l.Deep, l.Body, l.Pale, l.Body);
            V(m, "_Cut", 0.10f, 0.50f); V(m, "_Stops", 0.30f, 0.74f, 0.98f); V(m, "_Ends", 0f, 0.08f, 0f); V(m, "_Scroll", 0.15f, 0f);
            F(m, "_Swap", 1f); F(m, "_Sweep", -1.2f); F(m, "_Soft", 0.18f); F(m, "_Halo", 0f); F(m, "_Emit", 0.05f);
            F(m, "_Add", Mathf.Lerp(0.15f, 0.45f, l.Light)); V(m, "_Hue", l.HueDrift, 0f, 0.3f, 1f);
        });

        /// <summary>
        /// The wall's splash crown as it stands on its line (`FxStudyRafiWall`'s crown): the same lobes NOT laid crosswise,
        /// so a sweep's v 0 is its root on the ground and its lobes stand up toward v 1.
        /// </summary>
        public static Material Splash => Paint("splash", "water_crest", "erode_cloud", QCrest, (m, l) =>
        {
            Colours(m, l.Deep, l.Body, l.Pale, l.Body);
            V(m, "_Cut", 0.10f, 0.50f); V(m, "_Stops", 0.30f, 0.74f, 0.98f); V(m, "_Ends", 0.08f, 0.08f, 0f); V(m, "_Scroll", -0.6f, 0f);
            F(m, "_Soft", 0.18f); F(m, "_Halo", 0f); F(m, "_Emit", 0.05f);
            F(m, "_Add", Mathf.Lerp(0.15f, 0.45f, l.Light)); V(m, "_Hue", l.HueDrift, 0f, 0.3f, 1f);
        });

        /// <summary>The ground arc (`kit.Ring`).</summary>
        public static Material Ring => Paint("ring", "water_ring", "erode_cloud", QRing, (m, l) =>
        {
            Colours(m, l.Deep, l.Body, l.Pale, l.Body);
            V(m, "_Cut", 0.06f, 0.70f); V(m, "_Stops", 0.30f, 0.70f, 1.0f); V(m, "_Ends", 0f, 0f, 0f); V(m, "_Scroll", 0f, 0f);
            F(m, "_Soft", 0.25f); F(m, "_Halo", 0f); F(m, "_Add", Mathf.Lerp(0.3f, 0.8f, l.Light)); V(m, "_Hue", l.HueDrift, 0f, 0.4f, 1f);
        });

        /// <summary>The fat drops (`kit.Drop`). The picture is four drops: a piece picks one by its tiling (`DropCell`).</summary>
        public static Material Drop => Paint("drop", "water_drop", "erode_cloud", QDrop, (m, l) =>
        {
            Colours(m, l.Deep, l.Body, l.Foam, l.Body);
            V(m, "_Cut", 0.10f, 0.45f); V(m, "_Stops", 0.30f, 0.70f, 0.95f); V(m, "_Ends", 0f, 0f, 0f); V(m, "_Scroll", 0f, 0f);
            F(m, "_Halo", 0f); F(m, "_Add", Mathf.Lerp(0.2f, 0.6f, l.Light));
        });

        /// <summary>The tiling that shows drop number `cell` of the four on the drop picture.</summary>
        public static Vector4 DropCell(int cell) => new Vector4(0.5f, 0.5f, (cell % 2) * 0.5f, (cell / 2 % 2) * 0.5f);

        private static readonly int AgeId = Shader.PropertyToID("_Age"), OpacityId = Shader.PropertyToID("_Opacity"),
            ErodeId = Shader.PropertyToID("_Erode"), WindowId = Shader.PropertyToID("_Window"),
            TileId = Shader.PropertyToID("_Tile"), ErodeTileId = Shader.PropertyToID("_ErodeTile");

        /// <summary>
        /// One drawn piece: a renderer on a shared mesh and a shared material, with its own property block. `Frame` hides a
        /// piece that shows nothing (it costs no draw then).
        /// </summary>
        public sealed class Part
        {
            public readonly Transform At;
            public readonly MeshRenderer Renderer;
            private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
            private Vector4 _tile = new Vector4(1f, 1f, 0f, 0f), _erodeTile = new Vector4(1f, 1f, 0f, 0f), _window = new Vector4(-1f, 2f, 0.05f, 0f);

            public Part(string name, Transform parent, Mesh mesh, Material material)
            {
                var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
                At = go.transform; At.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                Renderer = go.AddComponent<MeshRenderer>();
                Renderer.sharedMaterial = material;
                Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Renderer.receiveShadows = false;
                Renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                Renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                Renderer.enabled = false;
                // An effect, not a prop: the ground queries and the prop checks pass over it.
                VfxRenderTag.Attach(go);
            }

            public Part Tile(Vector4 tile) { _tile = tile; return this; }
            public Part ErodeTile(Vector4 tile) { _erodeTile = tile; return this; }
            /// <summary>Only u from `tail` to `head` is drawn, with this softness at both ends.</summary>
            public Part Window(float tail, float head, float soft) { _window = new Vector4(tail, head, soft, 0f); return this; }

            public void Frame(float age, float opacity, float erode = 0f)
            {
                bool shown = opacity > 0.002f && erode < 0.999f && Renderer.sharedMaterial != null;
                if (Renderer.enabled != shown) Renderer.enabled = shown;
                if (!shown) return;
                _block.Clear();
                _block.SetFloat(AgeId, age); _block.SetFloat(OpacityId, Mathf.Clamp01(opacity)); _block.SetFloat(ErodeId, Mathf.Clamp01(erode));
                _block.SetVector(WindowId, _window); _block.SetVector(TileId, _tile); _block.SetVector(ErodeTileId, _erodeTile);
                Renderer.SetPropertyBlock(_block);
            }

            public void Hide() { if (Renderer.enabled) Renderer.enabled = false; }

            /// <summary>Turns an upright square to the eye.</summary>
            public void Face(Vector3 eye)
            {
                Vector3 to = At.position - eye;
                if (to.sqrMagnitude > 1e-6f) At.rotation = Quaternion.LookRotation(to, Vector3.up);
            }
        }

        /// <summary>Where the lens is: a film's staged eye, else the main camera, else a point behind and above.</summary>
        public static Vector3 Eye(Vector3? staged, Vector3 near)
        {
            if (staged.HasValue) return staged.Value;
            var lens = Camera.main;
            return lens != null ? lens.transform.position : near + new Vector3(0f, 1.6f, -6f);
        }

        /// <summary>Uploads a mesh made once to be shared, and widens its box by what the shader's warp can reach.</summary>
        public static Mesh Keep(Mesh mesh, string name, float warp = 0.15f)
        {
            mesh.name = name; mesh.hideFlags = HideFlags.HideAndDontSave;
            var b = mesh.bounds; b.Expand(warp); mesh.bounds = b;
            mesh.UploadMeshData(true);
            return mesh;
        }
    }
}
