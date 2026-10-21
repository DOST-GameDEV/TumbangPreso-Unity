using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// Places the Arena's ART (the bowl, the roof, the hull, the city, the shaft's rim) from
    /// Assets/TumbangPreso/Art/Arena/arena_layout.json, written by tools/export_arena_unity.py in
    /// the format the Ilalim map uses: one .glb per prototype under Models/, a list of placements
    /// (a row-major matrix in Unity axes each), and the kits' materials BY NAME, built here.
    ///
    /// THE FRAME. Every .glb of this map is in the game's own space once glTFast has imported it
    /// (a Blender point (x, y, z) is Unity (x, z, y); the export turns each mesh half a turn
    /// before it goes out and proves the result). So a placement's matrix is the identity for
    /// everything the kits modelled in place, and a stage piece or a prop can be instantiated
    /// with no matrix at all.
    ///
    /// ⚠️ EVERY MATERIAL SETTING IS IN ONE TABLE, <see cref="Rules"/>, by material name: which
    /// shader, the anti-tiling resample, the emission strength, how its textures wrap and whether
    /// they have mipmaps, fog, two sides. The layout says only which files a material reads and
    /// what its strength was in Blender. Change a look there and nowhere else.
    ///
    /// WHAT IS DRAWN HOW (the budget is the layout's `budget`: about 301,000 triangles for the four
    /// kits, 10,000 to 16,000 for one stage layout, about 38,000 for the crowd):
    ///   * NOTHING HAS A LODGroup. Players only ever stand on the stage, so every distance is
    ///     fixed: a LODGroup here would show one level for the whole match, which makes the lower
    ///     level the model and the upper one dead weight. What a camera in the MIDDLE of a
    ///     stadium needs is frustum culling, and a mesh that runs all the way round the can is
    ///     never culled: the export cuts the three rings (the lower bowl, the hull's body, the
    ///     rails) into sectors, the same faces in eight or twelve objects. The `lods` list is
    ///     still read, so a kit that later delivers a real second model only has to name it.
    ///   * STATIC BATCHING merges everything placed here by material (53 materials); the train
    ///     alone is left out, because it moves.
    ///   * NO SHADOWS from or onto any of it. The one shadow light is the key standing in for
    ///     two dozen floodlight banks; a canopy's or the scoreboard's single hard shadow across
    ///     the stage would be wrong, and the stands are past every tier's shadow distance. The
    ///     stage's own pieces cast and receive (`ArenaSceneBuilder`), for the players' shadows.
    ///   * NO OCCLUSION BAKE. It is one open bowl seen from its middle: the only things hidden
    ///     are the hull's plaza and underside behind the stands, about 50,000 triangles in a
    ///     dozen batches, not worth a bake that every rebuild would have to repeat.
    ///   * NO COLLIDERS. The play walls stand 22 m from the can (a square, its corners 31.1 m
    ///     out), from the kill plane to 12 m over the stage; nothing of the hull is inside radius
    ///     36.6 (the shaft's ledges) and those start 10 m below the stage. A body cannot reach
    ///     any of it.
    /// </summary>
    internal static class ArenaArtPlacer
    {
        public const string Root = "Assets/TumbangPreso/Art/Arena";
        public const string LayoutPath = Root + "/arena_layout.json";
        public const string PaintedShader = "TumbangPreso/ArenaPainted", GlowShader = "TumbangPreso/ArenaGlow";
        private const string Tag = "[Arena] ";

        [Serializable] private class MatSpec
        {
            public string name, shader, albedo, emissionMap, blend;
            public float emissionStrength;
            public bool twoSided, alphaFromAlbedo;
            public int[] size;
        }
        [Serializable] private class Placement { public string model, group, @object; public float[] matrix; }
        [Serializable] private class LodSpec { public string model, lod1, lod2; }
        [Serializable] private class Layout { public MatSpec[] materials; public Placement[] placements; public LodSpec[] lods; }

        // ------------------------------------------------------------------ THE TABLE

        /// <summary>What a material is drawn as.</summary>
        private enum Surface
        {
            /// <summary>TumbangPreso/ArenaPainted: lit, opaque.</summary>
            Painted,
            /// <summary>ArenaPainted with its alpha clip at 0.5.</summary>
            Cutout,
            /// <summary>TumbangPreso/ArenaGlow, added: light with no surface (holograms, a beam).</summary>
            Light,
            /// <summary>TumbangPreso/ArenaGlow, blended: a see-through sheet (the haze).</summary>
            Sheet,
        }

        private enum Wrap { Repeat, Clamp, RepeatUClampV }

        /// <summary>One row of the table. `Name` is a material's whole name, or a prefix ending in `*`.</summary>
        private sealed class Rule
        {
            public readonly string Name;
            public Surface Surface = Surface.Painted;
            /// <summary>The turned, masked resample that hides a tile's repeat. ONLY for a surface with no drawing in it.</summary>
            public bool AntiTile;
            /// <summary>The emission map's strength. <see cref="Kit"/>: the strength the kit gave it in Blender.</summary>
            public float Emission = Kit;
            public Wrap Wrap = Wrap.Repeat;
            public bool Mips = true;
            public bool Fog = true;
            public bool TwoSided;
            /// <summary>Never two-sided, even though the export finds open edges on it: a sheet of
            /// light that is READ (a logo, lettering). The kit builds it as two sheets back to
            /// back, each drawn from its own side only, so it is never seen mirrored.</summary>
            public bool OneSided;
            /// <summary>For the two unlit kinds: how much of the albedo is drawn (a lit surface in
            /// Blender, unlit here, so it is dimmed to what the night would leave of it).</summary>
            public float Albedo = 1.0f;
            public Rule(string name) { Name = name; }
        }

        private const float Kit = -1.0f;

        /// <summary>
        /// ⚠️ THE FIRST ROW THAT MATCHES WINS, so a kit's exceptions stand above its `*` row.
        /// The numbers are the kit authors' (their REPORT.md files and tools/author_arena_*.py),
        /// each author's words beside the row they asked for.
        /// </summary>
        private static readonly Rule[] Rules =
        {
            // ---- THE BOWL. "Turn the anti-tiling resample OFF for seat, led, door, marking and
            // panel: they are drawings and atlases. Leave it on for turf_a, turf_b, track,
            // concrete, step, steel."
            new Rule("arena_bowl_turf_a") { AntiTile = true },
            new Rule("arena_bowl_turf_b") { AntiTile = true },
            new Rule("arena_bowl_track") { AntiTile = true },
            new Rule("arena_bowl_concrete") { AntiTile = true },
            new Rule("arena_bowl_step") { AntiTile = true },
            new Rule("arena_bowl_steel") { AntiTile = true },
            // "LED emission near 1.0 keeps the blue deep (#0a1a9a). Higher washes it toward the
            // sky blue the owner rejected. led wraps in u; arena_bowl_led.png is 1024 x 512."
            // ⚠️ THE GLOW PASS (2026-10-05, the owner: "look into emmissives for the arena so things are
            // glowy"). The map blooms above 1.8 (`WorldLookProfile`, the Arena row's `Glow`), so what
            // is meant to glow is given 2.5 and more below, over the authors' Blender numbers: LED
            // rows, light strips, rims, signs, pads, holograms. Lit windows and screens stay under it.
            new Rule("arena_bowl_led") { Emission = 2.6f, Wrap = Wrap.RepeatUClampV },
            // "arena_bowl_marking is RGBA and needs alpha clip at 0.5 (the logo decal). The decal
            // casts no shadow." (Nothing placed here casts one.)
            new Rule("arena_bowl_marking") { Surface = Surface.Cutout, TwoSided = false },
            new Rule("arena_bowl_*"),                                   // seat; door 1.6 and panel 1.0 from the kit

            // ---- THE ROOF. Its atlases take no resample, and neither does its steel: the
            // trusses are tubes a few pixels wide from the stage, where a tile cannot be seen to
            // repeat and the resample's noise would be paid for nothing. "screen and logo must match."
            new Rule("arena_roof_lamp") { Emission = 8.0f },
            new Rule("arena_roof_led") { Emission = 3.0f },
            new Rule("arena_roof_glazing") { Emission = 1.0f },
            new Rule("arena_roof_screen") { Emission = 1.5f },
            new Rule("arena_roof_logo") { Emission = 1.5f },
            new Rule("arena_roof_booth_glass") { Emission = 2.2f },
            new Rule("arena_roof_*"),

            // ---- THE HULL. No resample on any of it but the steel. "arena_hull_glass is opaque."
            new Rule("arena_hull_steel") { AntiTile = true },
            new Rule("arena_hull_glass") { Emission = 0.8f },
            new Rule("arena_hull_light") { Emission = 3.6f },
            new Rule("arena_hull_glow") { Emission = 7.0f },
            new Rule("arena_hull_shaft") { Emission = 2.4f },
            new Rule("arena_hull_pad") { Emission = 3.0f },
            new Rule("arena_hull_trim") { Emission = 3.0f },
            new Rule("arena_hull_*"),

            // ---- THE CITY. "tune down, not up."
            // ⚠️ NO RESAMPLE ON ANY OF IT. The city author named trim, signs, floor, fx and base as
            // off. The facades (glass, resi, bands, far), the metal and the roofs are left off
            // too: every one is a drawing of windows, floors or panels, and the resample TURNS a
            // patch of it 37 degrees. If a facade's 64 m tile is seen to repeat, that is the
            // thing to judge in the review pictures before this is switched on.
            new Rule("arena_city_glass_a") { Emission = 0.8f },
            new Rule("arena_city_glass_b") { Emission = 0.8f },
            new Rule("arena_city_resi") { Emission = 0.8f },
            new Rule("arena_city_bands") { Emission = 0.8f },
            new Rule("arena_city_far") { Emission = 0.7f },
            // "arena_city_base repeats in u, clamps in v": v runs from the city floor to the sky lobby.
            new Rule("arena_city_base") { Emission = 0.9f, Wrap = Wrap.RepeatUClampV },
            new Rule("arena_city_roofs") { Emission = 0.9f },
            new Rule("arena_city_metal") { Emission = 1.0f },
            new Rule("arena_city_floor") { Emission = 1.0f },
            // "arena_city_trim is eight 32 px rows: mipmaps off, clamp." A mip would mix two LED colours.
            new Rule("arena_city_trim") { Emission = 3.2f, Wrap = Wrap.Clamp, Mips = false },
            new Rule("arena_city_signs") { Emission = 2.0f },
            // "arena_city_fx is transparent or additive, cull off, no shadows."
            new Rule("arena_city_fx") { Surface = Surface.Light, Emission = 2.4f, TwoSided = true, Albedo = 0.25f },
            // "arena_city_haze is alpha blend, cull off, no fog, drawn after the opaque city."
            new Rule("arena_city_haze") { Surface = Surface.Sheet, Emission = 1.0f, TwoSided = true, Fog = false, Albedo = 0.3f },
            new Rule("arena_city_*"),

            // ---- THE STAGE. Atlases and world-scale drawings (the deck's hexagons run on across
            // pieces): no resample. The kit's strengths: deck 1.0, line 1.0, rim 3.2, under 2.2,
            // mark 1.4, props 1.15.
            // The drone's tractor beam, two cones: WHITE drawings in the alpha that tile and are
            // slid along the cones and coloured by `ArenaDrone` through a property block (so
            // they repeat, and take no emission of their own: the block's colour is the light).
            new Rule("arena_stage_beamcore") { Surface = Surface.Light, Emission = 0.0f, TwoSided = true, Fog = false },
            new Rule("arena_stage_beam") { Surface = Surface.Light, Emission = 0.0f, TwoSided = true, Fog = false },
            // The drone's landing mark: PAINT on the deck, blended and not added (light added to a
            // near-white deck is only more white), with enough of its own light to read at night.
            new Rule("arena_stage_spot") { Surface = Surface.Sheet, Emission = 0.35f, TwoSided = true, Fog = false, Wrap = Wrap.Clamp },
            // The first Unity review (2026-10-05) drew the deck pure white, its hexagons gone: the
            // kit's 1.0 was set under Blender's one sun, and here the key and the stage lights
            // light it as well. The deck glows only enough to stay the brightest floor.
            new Rule("arena_stage_deck") { Emission = 0.12f },
            new Rule("arena_stage_line") { Emission = 0.5f },
            new Rule("arena_stage_rim") { Emission = 2.4f },
            new Rule("arena_stage_under") { Emission = 3.0f },
            new Rule("arena_stage_mark") { Emission = 2.2f },
            new Rule("arena_stage_props") { Emission = 4.4f },   // 2.6 read dark once HDR stayed on under MSAA: a saturated purple no longer clips up to bright
            new Rule("arena_stage_*"),

            // ---- THE HOLO KIT (tools/author_arena_holo.py): the hologram ads and the slipper.
            // "Translucent, added to the sky, no fog (they are 250 to 760 m out and would be
            // fogged to nothing), never brighter than the stage." The stage's rim is 2.4 and the
            // lamps 8; these sit at 1.5 to 2.0, about the map's bloom threshold (1.8): the logos'
            // whites just bloom, the washes do not. Tune DOWN first if they fight the play.
            // ads and logo are ONE-SIDED (lettering and marks, built as two sheets back to back).
            // The ads' strip repeats in v: `ArenaHoloMotion` scrolls it by a property block.
            new Rule("arena_holo_ads") { Surface = Surface.Light, Emission = 1.5f, OneSided = true, Fog = false, Albedo = 0.30f },
            new Rule("arena_holo_logo") { Surface = Surface.Light, Emission = 2.0f, OneSided = true, Fog = false, Albedo = 0.35f },
            new Rule("arena_holo_fx") { Surface = Surface.Light, Emission = 1.7f, TwoSided = true, Fog = false, Albedo = 0.25f },
            // "arena_holo_led is eight 32 px rows: mipmaps off, clamp." A mip would mix two colours.
            new Rule("arena_holo_led") { Emission = 3.0f, Wrap = Wrap.Clamp, Mips = false },
            new Rule("arena_holo_metal") { Emission = 1.0f },
            // The slipper balloon: lit paint that also gives off 0.7 of its own colour (its
            // emission image), so it is toy-bright at night without being a lamp.
            new Rule("arena_holo_balloon") { Emission = 1.0f },
            new Rule("arena_holo_*"),

            new Rule("*"),
        };

        private static Rule RuleFor(string name)
        {
            foreach (var rule in Rules)
            {
                bool prefix = rule.Name.EndsWith("*", StringComparison.Ordinal);
                if (prefix ? name.StartsWith(rule.Name.Substring(0, rule.Name.Length - 1), StringComparison.Ordinal) : name == rule.Name) return rule;
            }

            return Rules[Rules.Length - 1];
        }

        // ------------------------------------------------------------------ what the builder calls

        /// <summary>Placements whose things move at run time: never static. (The Holo kit's
        /// things turn, sway, ride and scroll: `ArenaHoloMotion`.)</summary>
        private static readonly HashSet<string> Moving = new HashSet<string> { "Train", "Holo" };

        private const float LodBias = 2f, LodLens = 2.1826f;
        private static readonly Vector2 LodMetres = new Vector2(70f, 160f);

        private static Layout _layout;
        private static Dictionary<string, Material> _materials = new Dictionary<string, Material>();
        private static readonly HashSet<string> Unmatched = new HashSet<string>();

        public static bool Exists => File.Exists(LayoutPath);

        /// <summary>Reads the layout and builds every material. Before anything that wears one
        /// (the stage's pieces are instantiated before the art is placed). Returns the materials made.</summary>
        public static int Prepare()
        {
            _layout = JsonUtility.FromJson<Layout>(File.ReadAllText(LayoutPath));
            if (_layout == null || _layout.placements == null) throw new InvalidOperationException(LayoutPath + " has no placements");
            Unmatched.Clear();
            _materials = BuildMaterials(_layout.materials ?? Array.Empty<MatSpec>());
            return _materials.Count;
        }

        /// <summary>For a build with no layout: nothing of an earlier build in this editor session is worn.</summary>
        public static void Forget()
        {
            _layout = null;
            _materials = new Dictionary<string, Material>();
            Unmatched.Clear();
        }

        /// <summary>A kit material by name, or null (no layout, or no such material).</summary>
        public static Material Material(string name) => _materials.TryGetValue(name, out var m) ? m : null;

        /// <summary>
        /// Swaps every imported placeholder under `go` for the kit material of the same name,
        /// and says who casts and takes shadows. A name with no material is kept and listed by
        /// <see cref="ReportUnmatched"/>.
        /// </summary>
        public static void Dress(GameObject go, bool shadows)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                bool solid = true;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string key = mats[i].name.Replace(" (Instance)", "").Trim();
                    if (_materials.TryGetValue(key, out var m)) mats[i] = m;
                    else Unmatched.Add(key);
                    if (mats[i].shader != null && mats[i].shader.name == GlowShader) solid = false;
                }

                r.sharedMaterials = mats;
                r.shadowCastingMode = shadows && solid ? ShadowCastingMode.On : ShadowCastingMode.Off;
                r.receiveShadows = shadows && solid;
            }
        }

        public static void ReportUnmatched()
        {
            if (Unmatched.Count > 0) Debug.LogWarning(Tag + "Material names with no kit material (they keep the importer's grey): " + string.Join(", ", Unmatched.OrderBy(s => s)));
        }

        /// <summary>Returns the placements made. The caller checks <see cref="Exists"/> and calls <see cref="Prepare"/> first.</summary>
        public static int Place(Transform root)
        {
            if (_layout == null) Prepare();
            var lods = new Dictionary<string, LodSpec>();
            foreach (var l in _layout.lods ?? Array.Empty<LodSpec>())
                if (l != null && !string.IsNullOrEmpty(l.model) && !string.IsNullOrEmpty(l.lod1)) lods[l.model] = l;
            if (lods.Count > 0 && !Mathf.Approximately(QualitySettings.lodBias, LodBias))
                Debug.LogWarning($"{Tag}QualitySettings.lodBias is {QualitySettings.lodBias}, the LOD distances assume {LodBias}");

            var dressing = new GameObject("Dressing").transform;
            dressing.SetParent(root, false);
            var groups = new Dictionary<string, Transform>();
            var triangles = new SortedDictionary<string, long>();
            int placed = 0, missing = 0, withLods = 0;

            foreach (var p in _layout.placements)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Models/{p.model}.glb");
                if (prefab == null) { Debug.LogWarning(Tag + "Missing model " + p.model); missing++; continue; }

                string groupName = string.IsNullOrEmpty(p.group) ? "Other" : p.group;
                if (!groups.TryGetValue(groupName, out var group))
                {
                    groups[groupName] = group = new GameObject(groupName).transform;
                    group.SetParent(dressing, false);
                }

                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
                go.name = string.IsNullOrEmpty(p.@object) ? p.model : p.@object;
                ApplyMatrix(go.transform, p.matrix);

                var lower = lods.TryGetValue(p.model, out var lod) ? AddLowerLods(go, lod) : null;
                Dress(go, false);
                if (!Moving.Contains(groupName))
                    foreach (var t in go.GetComponentsInChildren<Transform>())
                        GameObjectUtility.SetStaticEditorFlags(t.gameObject, (StaticEditorFlags)~0 & ~StaticEditorFlags.OccluderStatic & ~StaticEditorFlags.OccludeeStatic);
                if (lower != null) { RealLods(go, lower); withLods++; }

                triangles.TryGetValue(groupName, out long count);
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                    if (mf.sharedMesh != null && (lower == null || !lower.Any(l => mf.transform.IsChildOf(l.transform))))
                        for (int k = 0; k < mf.sharedMesh.subMeshCount; k++) count += (long)mf.sharedMesh.GetIndexCount(k) / 3;
                triangles[groupName] = count;
                placed++;
            }

            Debug.Log($"{Tag}Art placed: {placed} placements ({missing} missing models), {_materials.Count} materials, {withLods} with real LODs, " +
                      $"{triangles.Values.Sum()} triangles: {string.Join(", ", triangles.Select(kv => kv.Key + " " + kv.Value))}.");
            return placed;
        }

        /// <summary>A model of that name under the art folder, in any of the forms a kit may export.</summary>
        public static GameObject FindModel(string folder, string name)
        {
            foreach (string extension in new[] { ".glb", ".fbx", ".prefab" })
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/{folder}/{name}{extension}");
                if (model != null) return model;
            }

            return null;
        }

        // ------------------------------------------------------------------ transforms

        /// <summary>Decomposes a TRS matrix (row-major, Unity axes) onto a transform in world space.
        /// A mirrored placement (negative determinant) is folded into a negative X scale.</summary>
        private static void ApplyMatrix(Transform t, float[] f)
        {
            if (f == null || f.Length != 16) throw new InvalidOperationException("Placement matrix must have 16 floats");
            var m = new Matrix4x4();
            for (int r = 0; r < 4; r++) for (int c = 0; c < 4; c++) m[r, c] = f[r * 4 + c];
            Vector3 cx = m.GetColumn(0), cy = m.GetColumn(1), cz = m.GetColumn(2);
            var scale = new Vector3(cx.magnitude, cy.magnitude, cz.magnitude);
            if (Vector3.Dot(Vector3.Cross(cx, cy), cz) < 0) { scale.x = -scale.x; cx = -cx; }
            t.SetPositionAndRotation(m.GetColumn(3), Quaternion.LookRotation(cz / Mathf.Max(scale.z, 1e-6f), cy / Mathf.Max(scale.y, 1e-6f)));
            t.localScale = scale;
        }

        // ------------------------------------------------------------------ materials

        private static Dictionary<string, Material> BuildMaterials(MatSpec[] specs)
        {
            string folder = Root + "/Materials";
            if (!AssetDatabase.IsValidFolder(folder)) { Directory.CreateDirectory(folder); AssetDatabase.Refresh(); }

            var painted = Shader.Find(PaintedShader);
            var glow = Shader.Find(GlowShader);
            if (painted == null || glow == null)
                throw new InvalidOperationException($"Missing {(painted == null ? PaintedShader : GlowShader)}: it did not compile (see the shader's errors in this log) or is not in the project");

            // Two materials may read one file: it is imported once, the stricter way (no mipmaps
            // if either asks for none; clamped only where both clamp).
            var imports = new Dictionary<string, (bool clampU, bool clampV, bool mips, bool alpha, bool cutout)>();
            void Use(string file, Rule rule, bool alpha, bool cutout)
            {
                if (string.IsNullOrEmpty(file)) return;
                bool u = rule.Wrap == Wrap.Clamp, v = rule.Wrap != Wrap.Repeat;
                imports[file] = imports.TryGetValue(file, out var had)
                    ? (had.clampU && u, had.clampV && v, had.mips && rule.Mips, had.alpha || alpha, had.cutout || cutout)
                    : (u, v, rule.Mips, alpha, cutout);
            }
            foreach (var s in specs)
            {
                if (s == null || string.IsNullOrEmpty(s.name)) continue;
                var rule = RuleFor(s.name);
                // Whatever is drawn through its alpha (a cut-out, a hologram, the haze) has its alpha read as a shape.
                Use(s.albedo, rule, rule.Surface != Surface.Painted, rule.Surface == Surface.Cutout);
                Use(s.emissionMap, rule, false, false);
            }
            var textures = new Dictionary<string, Texture2D>();
            foreach (var kv in imports) textures[kv.Key] = ImportTexture(kv.Key, kv.Value.clampU, kv.Value.clampV, kv.Value.mips, kv.Value.alpha, kv.Value.cutout);
            Texture2D Tex(string file) => !string.IsNullOrEmpty(file) && textures.TryGetValue(file, out var t) ? t : null;

            var result = new Dictionary<string, Material>();
            var rows = new List<string>();
            foreach (var s in specs)
            {
                if (s == null || string.IsNullOrEmpty(s.name)) continue;

                var rule = RuleFor(s.name);
                bool lit = rule.Surface == Surface.Painted || rule.Surface == Surface.Cutout;
                // The kit says a material is see-through; the table must agree, or it is drawn solid.
                if (lit && s.blend == "blend") Debug.LogWarning($"{Tag}{s.name} blends in Blender and has no see-through row in ArenaArtPlacer.Rules: drawn opaque");
                if (rule.Surface != Surface.Cutout && s.blend == "cutout") Debug.LogWarning($"{Tag}{s.name} is cut out in Blender and has no cut-out row in ArenaArtPlacer.Rules");

                var shader = lit ? painted : glow;
                string path = $"{folder}/{s.name}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
                m.shader = shader;
                // A saved material keeps properties an earlier build set; start from the shader's defaults.
                m.CopyPropertiesFromMaterial(new Material(shader));
                m.shaderKeywords = Array.Empty<string>();
                m.SetOverrideTag("RenderType", "");
                m.renderQueue = -1;

                float strength = string.IsNullOrEmpty(s.emissionMap) ? 0.0f : rule.Emission >= 0.0f ? rule.Emission : s.emissionStrength;
                bool twoSided = !rule.OneSided && (rule.TwoSided || (s.twoSided && !lit));
                m.SetTexture("_MainTex", Tex(s.albedo));
                m.SetTexture("_EmissionMap", Tex(s.emissionMap));
                m.SetFloat("_EmissionStrength", strength);
                m.SetFloat("_Fog", rule.Fog ? 1.0f : 0.0f);
                m.SetFloat("_Cull", (float)(twoSided ? CullMode.Off : CullMode.Back));
                if (lit)
                {
                    m.SetColor("_Color", Color.white);
                    m.SetFloat("_AntiTile", rule.AntiTile ? 1.0f : 0.0f);
                    if (rule.AntiTile) m.EnableKeyword("_ANTITILE_ON");
                    if (rule.Surface == Surface.Cutout)
                    {
                        m.SetFloat("_AlphaClip", 1.0f);
                        m.SetFloat("_Cutoff", 0.5f);
                        m.EnableKeyword("_ALPHATEST_ON");
                        // So the depth and normals pass the outline reads cuts the same holes.
                        m.SetOverrideTag("RenderType", "TransparentCutout");
                        m.renderQueue = (int)RenderQueue.AlphaTest;
                    }
                }
                else
                {
                    bool added = rule.Surface == Surface.Light;
                    m.SetColor("_Color", new Color(rule.Albedo, rule.Albedo, rule.Albedo, 1.0f));
                    m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                    m.SetFloat("_DstBlend", (float)(added ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
                    m.SetFloat("_FogToColour", added ? 0.0f : 1.0f);
                    m.renderQueue = (int)RenderQueue.Transparent;
                }

                m.enableInstancing = true;
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                EditorUtility.SetDirty(m);
                result[s.name] = m;
                rows.Add($"{s.name}: {rule.Surface}{(rule.AntiTile ? ", resample" : "")}, emission {strength:0.##}{(rule.Emission < 0 && strength > 0 ? " (kit)" : "")}" +
                         $"{(rule.Wrap != Wrap.Repeat ? ", " + rule.Wrap : "")}{(rule.Mips ? "" : ", no mips")}{(rule.Fog ? "" : ", no fog")}{(twoSided ? ", two-sided" : "")}");
            }

            Debug.Log($"{Tag}Materials ({result.Count}), from the one table:\n  " + string.Join("\n  ", rows));
            return result;
        }

        /// <summary>A kit texture with the import its materials ask for. Colour (sRGB) always:
        /// the emission maps are colour too. `alpha`: its alpha is a shape (colour is bled under
        /// the clear texels). `cutout`: and it is tested at 0.5, so it keeps its coverage down the
        /// mip chain.</summary>
        public static Texture2D ImportTexture(string file, bool clampU, bool clampV, bool mips, bool alpha, bool cutout, int maxSize = 2048)
        {
            string path = file.StartsWith("Assets/") ? file : $"{Root}/Textures/{file}";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { Debug.LogWarning(Tag + "Missing texture " + path); return null; }

            bool dirty = false;
            if (importer.textureType != TextureImporterType.Default) { importer.textureType = TextureImporterType.Default; dirty = true; }
            if (!importer.sRGBTexture) { importer.sRGBTexture = true; dirty = true; }
            var u = clampU ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            var v = clampV ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            if (importer.wrapModeU != u) { importer.wrapModeU = u; dirty = true; }
            if (importer.wrapModeV != v) { importer.wrapModeV = v; dirty = true; }
            if (importer.mipmapEnabled != mips) { importer.mipmapEnabled = mips; dirty = true; }
            if (importer.alphaIsTransparency != alpha) { importer.alphaIsTransparency = alpha; dirty = true; }
            if (importer.mipMapsPreserveCoverage != (cutout && mips)) { importer.mipMapsPreserveCoverage = cutout && mips; importer.alphaTestReferenceValue = 0.5f; dirty = true; }
            int aniso = mips ? 4 : 1;
            if (importer.anisoLevel != aniso) { importer.anisoLevel = aniso; dirty = true; }
            if (importer.maxTextureSize != maxSize) { importer.maxTextureSize = maxSize; dirty = true; }
            if (dirty) importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ------------------------------------------------------------------ levels of detail (for a kit that delivers one)

        /// <summary>The prototype's LOD1 (and LOD2) as children "LOD1" and "LOD2" at the
        /// placement's own origin. Null when the LOD1 file is not there.</summary>
        private static List<GameObject> AddLowerLods(GameObject go, LodSpec lod)
        {
            var lower = new List<GameObject>();
            var names = new[] { lod.lod1, lod.lod2 };
            for (int level = 0; level < names.Length; level++)
            {
                if (string.IsNullOrEmpty(names[level])) break;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Models/{names[level]}.glb");
                if (prefab == null) { Debug.LogWarning($"{Tag}LOD model {names[level]} is listed but missing; {lod.model} keeps what it has"); break; }
                var child = (GameObject)PrefabUtility.InstantiatePrefab(prefab, go.transform);
                child.name = "LOD" + (level + 1);
                child.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                child.transform.localScale = Vector3.one;
                lower.Add(child);
            }
            return lower.Count > 0 ? lower : null;
        }

        private static float LodHeight(float size, float metres) => size * LodBias / (LodLens * (size * 0.5f + metres));

        /// <summary>LOD0 (every renderer the placement had), LOD1, an optional LOD2. Nothing is culled.</summary>
        private static void RealLods(GameObject go, List<GameObject> lower)
        {
            var sets = lower.Select(l => l.GetComponentsInChildren<Renderer>()).ToArray();
            var lowerAll = new HashSet<Renderer>(sets.SelectMany(s => s));
            var lod0 = go.GetComponentsInChildren<Renderer>().Where(r => !lowerAll.Contains(r)).ToArray();
            if (lod0.Length == 0 || sets.Any(s => s.Length == 0)) { foreach (var l in lower) Object.DestroyImmediate(l); return; }

            var group = go.AddComponent<LODGroup>();
            group.SetLODs(new[] { new LOD(0.5f, lod0) });
            group.RecalculateBounds();
            var scale = go.transform.lossyScale;
            float size = group.size * Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            float h1 = Mathf.Min(LodHeight(size, LodMetres.x), 0.99f);
            float h2 = Mathf.Min(LodHeight(size, LodMetres.y), h1 * 0.9f);
            var levels = new List<LOD> { new LOD(h1, lod0) };
            if (sets.Length > 1) { levels.Add(new LOD(h2, sets[0])); levels.Add(new LOD(0f, sets[1])); }
            else levels.Add(new LOD(0f, sets[0]));
            group.fadeMode = LODFadeMode.None;
            group.SetLODs(levels.ToArray());
        }
    }
}
