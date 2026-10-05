using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using TumbangPreso.Map;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// Puts the Arena's crowd in the scene (docs/ARENA_ART_BRIEF.md, the `crowd` kit).
    /// `ArenaSceneBuilder` finds `Build(Transform parent)` by name and calls it with its `Crowd`
    /// group, which sits at the can (the origin) with no rotation.
    ///
    /// WHAT IT READS
    ///   * THE ROWS: tools/arena_rows.json, the bowl kit's own, if it is there. Until then
    ///     tools/arena_crowd_rows_fallback.json, which tools/author_arena_crowd.py DERIVES from
    ///     the art brief's table and the blockout. Both are in the BLENDER frame (metres, z up,
    ///     bearings clockwise from north) and are turned here: a Blender bearing is a Unity
    ///     bearing from +z, and Blender's z is Unity's y. The format read:
    ///         { "banks": [ { "name", "tread", "rows": [ { "r", "z" } ], "sections": [ [from, to] ] } ],
    ///           "voids": [ { "lo", "hi", "z_max" } ] }
    ///     `r` and `z` are a row's FRONT edge and its floor; `tread` may sit on a row instead of
    ///     its bank; a section may be `{ "lo", "hi" }`. ⚠️ If the bowl kit writes another shape,
    ///     this is the one function to change (`ReadRows`), and it throws rather than guess.
    ///   * THE ATLAS: tools/arena_crowd_atlas.json, for the grid, the cell's size in metres and
    ///     the loops. The loops are CHECKED against `ArenaCrowd.Loops` (which the shader's table
    ///     mirrors): a re-rendered atlas with other rows must not be drawn with the old table.
    ///
    /// WHAT IT MAKES: the material Art/Arena/Materials/arena_crowd.mat, the two textures' import
    /// settings, and an `ArenaCrowd` on `parent` holding the rows. The meshes are NOT assets:
    /// `ArenaCrowd.Rebuild` lays them out when the scene loads (and once here, to count them and
    /// to show the crowd in the editor).
    /// </summary>
    public static class ArenaCrowdBuilder
    {
        public const string RowsPath = "tools/arena_rows.json";
        public const string RowsFallbackPath = "tools/arena_crowd_rows_fallback.json";
        public const string AtlasLayoutPath = "tools/arena_crowd_atlas.json";
        public const string AtlasPath = ArenaArtPlacer.Root + "/Textures/arena_crowd_atlas.png";
        public const string EmissionPath = ArenaArtPlacer.Root + "/Textures/arena_crowd_atlas_emit.png";
        public const string MaterialPath = ArenaArtPlacer.Root + "/Materials/arena_crowd.mat";
        public const string ShaderName = "TumbangPreso/ArenaCrowd";
        private const string Tag = "[Arena crowd] ";

        public static ArenaCrowd Build(Transform parent)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (!File.Exists(AtlasLayoutPath)) throw new FileNotFoundException("No crowd atlas layout (run tools/author_arena_crowd.py): " + AtlasLayoutPath);
            var atlas = JObject.Parse(File.ReadAllText(AtlasLayoutPath));
            CheckLoops(atlas);

            string rowsPath = File.Exists(RowsPath) ? RowsPath : RowsFallbackPath;
            if (!File.Exists(rowsPath)) throw new FileNotFoundException("No rows for the crowd: neither " + RowsPath + " nor " + RowsFallbackPath);
            ReadRows(JObject.Parse(File.ReadAllText(rowsPath)), out var banks, out var voids);

            var crowd = parent.GetComponent<ArenaCrowd>();
            if (crowd == null) crowd = parent.gameObject.AddComponent<ArenaCrowd>();
            crowd.Banks = banks;
            crowd.Voids = voids;
            crowd.People = (int)atlas["columns"];
            crowd.CellMetres = new Vector2((float)atlas["cell_m"][0], (float)atlas["cell_m"][1]);
            crowd.Material = Material(atlas, crowd.CellMetres);
            crowd.Rebuild();
            EditorUtility.SetDirty(crowd);

            var built = crowd.Built;
            Debug.Log($"{Tag}{built.x} spectators in {built.z} chunks from {rowsPath}" +
                      (rowsPath == RowsFallbackPath ? " (DERIVED rows: the bowl kit has not written " + RowsPath + ")" : "") +
                      $". Every seat: {built.x} quads, {built.x * 4} vertices, {built.x * 2} triangles. Far meshes: {built.y} quads. " +
                      $"At most {built.z} draw calls, one material, no shadows.");
            return crowd;
        }

        /// <summary>The atlas's loops, in its own order, must be the ones the shader's table was written for.</summary>
        private static void CheckLoops(JObject atlas)
        {
            var loops = atlas["loops"] as JArray ?? throw new InvalidDataException(AtlasLayoutPath + " has no loops");
            var want = ArenaCrowd.Loops;
            bool same = loops.Count == want.Length;
            int start = 0;
            for (int i = 0; same && i < want.Length; i++)
            {
                same = (string)loops[i]["name"] == want[i].name && (int)loops[i]["frames"] == want[i].frames && (int)loops[i]["start"] == start;
                start += want[i].frames;
            }
            if (!same)
                throw new InvalidDataException(AtlasLayoutPath + "'s loops are not the ones ArenaCrowd.Loops and ArenaCrowd.shader's LoopStart/LoopCount " +
                                               "were written for. Change all three together.");
        }

        private static void ReadRows(JObject doc, out ArenaCrowd.Bank[] banks, out Vector3[] voids)
        {
            var list = new List<ArenaCrowd.Bank>();
            var source = doc["banks"] as JArray ?? throw new InvalidDataException("The rows file has no `banks` list");
            foreach (var b in source)
            {
                var rows = b["rows"] as JArray ?? throw new InvalidDataException("A bank has no `rows`: " + b["name"]);
                var sections = b["sections"] as JArray ?? throw new InvalidDataException("A bank has no `sections`: " + b["name"]);
                if (rows.Count == 0 || sections.Count == 0) continue;
                float tread = b["tread"] != null ? (float)b["tread"] : rows[0]["tread"] != null ? (float)rows[0]["tread"] :
                    rows.Count > 1 ? (float)rows[1]["r"] - (float)rows[0]["r"] : 1.6f;
                list.Add(new ArenaCrowd.Bank
                {
                    Name = (string)b["name"] ?? "bank " + list.Count,
                    Tread = tread,
                    // Blender (r, z up) to Unity (r, y up).
                    Rows = rows.Select(r => new Vector2((float)r["r"], (float)r["z"])).ToArray(),
                    Sections = sections.Select(s => s is JArray a ? new Vector2((float)a[0], (float)a[1]) : new Vector2((float)s["lo"], (float)s["hi"]))
                        .OrderBy(s => s.x).ToArray(),
                });
            }
            banks = list.ToArray();
            voids = (doc["voids"] as JArray ?? new JArray())
                .Select(v => new Vector3((float)v["lo"], (float)v["hi"], (float)v["z_max"])).ToArray();
        }

        private static Material Material(JObject atlas, Vector2 cellMetres)
        {
            Import(AtlasPath, true);
            Import(EmissionPath, false);
            var shader = Shader.Find(ShaderName) ?? throw new InvalidOperationException("Shader not found: " + ShaderName);
            Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "arena_crowd" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.shader = shader;
            material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath));
            material.SetTexture("_EmitTex", AssetDatabase.LoadAssetAtPath<Texture2D>(EmissionPath));
            float size = (float)atlas["atlas_px"];
            material.SetVector("_Grid", new Vector4((float)atlas["cell_px"][0] / size, (float)atlas["cell_px"][1] / size, (int)atlas["columns"], 0f));
            // The hop (w) is the shader's own default unless somebody has tuned it on the material.
            float hop = material.GetVector("_CellMetres").w;
            material.SetVector("_CellMetres", new Vector4(cellMetres.x, cellMetres.y, (float)atlas["ground_m"], hop));
            material.enableInstancing = false;
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>
        /// ⚠️ THE CUTOUT MUST KEEP ITS COVERAGE DOWN THE MIP CHAIN. In the top rows a spectator
        /// is 8 px tall, three mips down; an averaged alpha tested at 0.5 thins a figure until
        /// its arms and what it holds vanish. `mipMapsPreserveCoverage` rescales each mip's alpha
        /// so the tested area stays the same. Clamped: a cell at the atlas's edge must not wrap.
        /// </summary>
        private static void Import(string path, bool colour)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("No crowd texture (run tools/author_arena_crowd.py): " + path);
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Not a texture: " + path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = colour;
            importer.alphaSource = colour ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = colour;
            importer.mipmapEnabled = true;
            importer.mipMapsPreserveCoverage = colour;
            importer.alphaTestReferenceValue = 0.5f;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 1;
            importer.maxTextureSize = 2048;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
    }
}
