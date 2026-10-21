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
    /// THE FAR HOUSES ON ONE CHEAP MATERIAL (2026-10-19, owner: "optimize the city side of the map, i still get fps drops
    /// ... the houses on the farther side can be using simpler textures too. maybe do this, or dynamically change textures
    /// by applying an LOD style effect").
    ///
    /// Before: every house type (granary, tiered, tower, gong, long) wore its own material with its own 2048 albedo and
    /// 2048 normal map, so the town's 33 houses up the south slope (and the 11 hamlet houses down the valley) drew as five
    /// materials that never batch with each other, each pixel sampling a normal map that at 130 m and beyond is under a
    /// pixel of detail.
    ///
    /// Now each house is a `LODGroup`:
    ///  * LOD0 the house exactly as approved, to `FarHouse` (130 m at 60 degrees vertical, lodBias 1: about 120 m at the
    ///    game's 95 degrees on Ultra). From the court that is the whole front row of the town (78 to 120 m) and the near end
    ///    of the back row, so what a player sees up close is untouched.
    ///  * LOD1 the same mesh (no decimation: the silhouette cannot pop) with its UVs moved into one shared atlas
    ///    (`Generated/HouseFar/house_far_atlas.png`, each type's albedo at 512, its tint baked in) and ONE material on the
    ///    same painted shader with no normal map (`payo_house_far`), plus one shared glow material for the lit windows
    ///    (`payo_house_far_glow`, named as a glow so the night turns it up like the others). Every far house therefore
    ///    shares two materials, is static and batches, casts no shadow (it stands past the shadow distance from any place
    ///    a player can be) and its tangents are dropped.
    /// A model whose UVs leave 0..1 (an atlas cannot wrap) keeps its own picture in a far material of its own, still
    /// without the normal map; the build line says which.
    /// </summary>
    public static partial class PayoSceneBuilder
    {
        /// <summary>A house draws its full material nearer than this (60 degrees vertical, lodBias 1), metres.</summary>
        private const float FarHouse = 130.0f;
        private const int HouseTile = 512, HouseAtlasColumns = 4, HouseAtlasRows = 2;
        private const string HouseFarFolder = Generated + "/HouseFar";
        /// <summary>The build line's far part: the instanced plants (FarFlora) and the far houses.</summary>
        private static string _farSaid = "";

        private static string FarTown(Transform root)
        {
            var dressing = root.Find("Dressing");
            if (dressing == null) return "far houses: no Dressing";
            var pieces = new List<Transform>();
            foreach (Transform group in dressing)
                if (group.name.StartsWith("house_", StringComparison.Ordinal))
                    foreach (Transform piece in group) pieces.Add(piece);
            if (pieces.Count == 0) return "far houses: no house pieces";
            var painted = Shader.Find(PaintedShader);
            if (painted == null) return "far houses: NO painted shader, houses left as they were";
            EnsureFolder(HouseFarFolder);

            // The types, each with its main material (the one that is not a glow) and the meshes it is drawn with.
            var types = new List<string>();
            var mainOf = new Dictionary<string, Material>();
            var glowOf = new Dictionary<string, Material>();
            var fits = new Dictionary<string, bool>();
            foreach (var piece in pieces)
            {
                string type = piece.parent.name;
                if (types.Contains(type)) continue;
                bool fit = true;
                foreach (var filter in piece.GetComponentsInChildren<MeshFilter>(true))
                {
                    var r = filter.GetComponent<MeshRenderer>();
                    if (r == null || filter.sharedMesh == null) continue;
                    if (!filter.sharedMesh.isReadable) { fit = false; continue; }
                    var uv = filter.sharedMesh.uv;
                    if (uv.Length == 0 || uv.Any(u => u.x < -0.002f || u.x > 1.002f || u.y < -0.002f || u.y > 1.002f)) fit = false;
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m == null) continue;
                        if (Glows.Contains(m.name)) { if (!glowOf.ContainsKey(type)) glowOf[type] = m; }
                        else if (!mainOf.ContainsKey(type)) mainOf[type] = m;
                    }
                }
                if (!mainOf.ContainsKey(type) || mainOf[type].GetTexture("_MainTex") == null) continue;
                types.Add(type);
                fits[type] = fit;
            }
            if (types.Count == 0) return "far houses: no house type with a picture";

            // The atlas: each fitting type's albedo drawn into a 512 tile, its tint multiplied in (sRGB bytes times the
            // sRGB tint is the shader's linear product), saved as a png the importer gives mips and clamps.
            var atlased = types.Where(t => fits[t]).Take(HouseAtlasColumns * HouseAtlasRows).ToList();
            var tileOf = new Dictionary<string, Vector4>();   // uv scale x, y, offset x, y
            Texture2D atlasTexture = null;
            string atlasPath = HouseFarFolder + "/house_far_atlas.png";
            if (atlased.Count > 0)
            {
                int width = HouseTile * HouseAtlasColumns, height = HouseTile * HouseAtlasRows;
                var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
                var clear = new Color32[width * height];
                for (int i = 0; i < clear.Length; i++) clear[i] = new Color32(128, 128, 128, 255);
                image.SetPixels32(clear);
                var tileRt = RenderTexture.GetTemporary(HouseTile, HouseTile, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                var previous = RenderTexture.active;
                try
                {
                    for (int k = 0; k < atlased.Count; k++)
                    {
                        string type = atlased[k];
                        int col = k % HouseAtlasColumns, row = k / HouseAtlasColumns;
                        Graphics.Blit(mainOf[type].GetTexture("_MainTex"), tileRt);
                        RenderTexture.active = tileRt;
                        var tile = new Texture2D(HouseTile, HouseTile, TextureFormat.RGBA32, false);
                        tile.ReadPixels(new Rect(0, 0, HouseTile, HouseTile), 0, 0);
                        tile.Apply();
                        Color tint = mainOf[type].HasProperty("_Color") ? mainOf[type].GetColor("_Color") : Color.white;
                        var px = tile.GetPixels32();
                        for (int i = 0; i < px.Length; i++)
                            px[i] = new Color32((byte)Mathf.Clamp(px[i].r * tint.r, 0, 255), (byte)Mathf.Clamp(px[i].g * tint.g, 0, 255), (byte)Mathf.Clamp(px[i].b * tint.b, 0, 255), 255);
                        image.SetPixels32(col * HouseTile, row * HouseTile, HouseTile, HouseTile, px);
                        Object.DestroyImmediate(tile);
                        tileOf[type] = new Vector4(1.0f / HouseAtlasColumns, 1.0f / HouseAtlasRows, (float)col / HouseAtlasColumns, (float)row / HouseAtlasRows);
                    }
                }
                finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(tileRt); }
                image.Apply();
                File.WriteAllBytes(atlasPath, image.EncodeToPNG());
                Object.DestroyImmediate(image);
                AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceSynchronousImport);
                if (AssetImporter.GetAtPath(atlasPath) is TextureImporter importer)
                {
                    bool dirty = false;
                    if (importer.textureType != TextureImporterType.Default) { importer.textureType = TextureImporterType.Default; dirty = true; }
                    if (!importer.sRGBTexture) { importer.sRGBTexture = true; dirty = true; }
                    if (!importer.mipmapEnabled) { importer.mipmapEnabled = true; dirty = true; }
                    if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; dirty = true; }
                    if (importer.maxTextureSize != 2048) { importer.maxTextureSize = 2048; dirty = true; }
                    if (importer.alphaSource != TextureImporterAlphaSource.None) { importer.alphaSource = TextureImporterAlphaSource.None; dirty = true; }
                    if (importer.anisoLevel != 4) { importer.anisoLevel = 4; dirty = true; }
                    if (dirty) importer.SaveAndReimport();
                }
                atlasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
                if (atlasTexture == null) { atlased.Clear(); tileOf.Clear(); }
            }

            // The far materials: the painted shader as the houses wear it, with no normal map and a white tint.
            Material FarMaterial(string name, Material like, Texture picture, bool glow)
            {
                string path = $"{HouseFarFolder}/{name}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = new Material(painted); AssetDatabase.CreateAsset(m, path); }
                m.shader = painted;
                m.CopyPropertiesFromMaterial(like);
                m.shaderKeywords = like.shaderKeywords;
                if (picture != null) { m.SetTexture("_MainTex", picture); m.SetTextureScale("_MainTex", Vector2.one); m.SetTextureOffset("_MainTex", Vector2.zero); m.SetColor("_Color", Color.white); }
                m.SetTexture("_BumpMap", null);
                m.SetFloat("_BumpScale", 0.0f);
                m.enableInstancing = true;
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                m.renderQueue = -1;
                if (glow) Glows.Add(name);
                EditorUtility.SetDirty(m);
                return m;
            }
            Material sharedMain = null, sharedGlow = null;
            if (atlased.Count > 0)
            {
                sharedMain = FarMaterial("payo_house_far", mainOf[atlased[0]], atlasTexture, false);
                string withGlow = atlased.FirstOrDefault(t => glowOf.ContainsKey(t));
                if (withGlow != null) sharedGlow = FarMaterial("payo_house_far_glow", glowOf[withGlow], atlasTexture, true);
            }
            var ownMain = new Dictionary<string, Material>();
            var ownGlow = new Dictionary<string, Material>();
            foreach (string type in types.Where(t => !tileOf.ContainsKey(t)))
            {
                ownMain[type] = FarMaterial(mainOf[type].name + "_lod", mainOf[type], null, false);
                if (glowOf.ContainsKey(type)) ownGlow[type] = FarMaterial(glowOf[type].name + "_lod", glowOf[type], null, true);
            }

            // The far meshes: one per source mesh and type, UVs into the type's tile, no tangents.
            var farMesh = new Dictionary<(Mesh, string), Mesh>();
            Mesh FarMesh(Mesh source, string type)
            {
                if (farMesh.TryGetValue((source, type), out var made)) return made;
                var mesh = Object.Instantiate(source);
                mesh.name = source.name + "_far";
                if (tileOf.TryGetValue(type, out var t))
                {
                    var uv = mesh.uv;
                    for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(Mathf.Clamp01(uv[i].x) * t.x + t.z, Mathf.Clamp01(uv[i].y) * t.y + t.w);
                    mesh.uv = uv;
                }
                mesh.tangents = null;
                mesh.UploadMeshData(false);
                string path = $"{HouseFarFolder}/{type}_{source.name}_far.asset";
                if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(mesh, path);
                farMesh[(source, type)] = mesh;
                return mesh;
            }

            float tan = Mathf.Tan(LodReferenceFov * 0.5f * Mathf.Deg2Rad);
            int grouped = 0, renderers = 0;
            foreach (var piece in pieces)
            {
                string type = piece.parent.name;
                if (!types.Contains(type) || piece.GetComponent<LODGroup>() != null) continue;
                var near = piece.GetComponentsInChildren<MeshRenderer>(true);
                var far = new List<Renderer>();
                foreach (var filter in piece.GetComponentsInChildren<MeshFilter>(true))
                {
                    var r = filter.GetComponent<MeshRenderer>();
                    if (r == null || filter.sharedMesh == null) continue;
                    var low = new GameObject(filter.name + " far").transform;
                    low.SetParent(filter.transform.parent, false);
                    low.localPosition = filter.transform.localPosition; low.localRotation = filter.transform.localRotation; low.localScale = filter.transform.localScale;
                    low.gameObject.AddComponent<MeshFilter>().sharedMesh = FarMesh(filter.sharedMesh, type);
                    var lr = low.gameObject.AddComponent<MeshRenderer>();
                    lr.sharedMaterials = r.sharedMaterials.Select(m => m == null ? null
                        : Glows.Contains(m.name) ? (tileOf.ContainsKey(type) ? sharedGlow : ownGlow.TryGetValue(type, out var g) ? g : m) ?? m
                        : (tileOf.ContainsKey(type) ? sharedMain : ownMain[type])).ToArray();
                    lr.shadowCastingMode = ShadowCastingMode.Off;
                    lr.receiveShadows = r.receiveShadows;
                    lr.lightProbeUsage = r.lightProbeUsage; lr.reflectionProbeUsage = r.reflectionProbeUsage;
                    GameObjectUtility.SetStaticEditorFlags(low.gameObject, GameObjectUtility.GetStaticEditorFlags(filter.gameObject));
                    far.Add(lr);
                    renderers++;
                }
                if (far.Count == 0) continue;
                var group = piece.gameObject.AddComponent<LODGroup>();
                group.fadeMode = LODFadeMode.None;
                group.SetLODs(new[] { new LOD(0.5f, near), new LOD(0.001f, far.ToArray()) });
                group.RecalculateBounds();
                Vector3 s = piece.lossyScale;
                float size = Mathf.Max(group.size * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z)), 0.1f);
                float h0 = Mathf.Clamp(size / (2.0f * FarHouse * tan), 0.002f, 0.99f);
                group.SetLODs(new[] { new LOD(h0, near), new LOD(Mathf.Min(0.0005f, h0 * 0.5f), far.ToArray()) });
                grouped++;
            }
            string own = ownMain.Count > 0 ? $"; own far pictures (UVs outside 0..1 or unreadable): {string.Join(", ", ownMain.Keys)}" : "";
            return $"far houses: {grouped} houses given a LOD1 past {FarHouse:0} m ({renderers} renderers, {farMesh.Count} far meshes), " +
                   $"{atlased.Count} types in one {HouseTile * HouseAtlasColumns}x{HouseTile * HouseAtlasRows} atlas ({string.Join(", ", atlased)}) on " +
                   $"{(sharedMain != null ? 1 : 0) + (sharedGlow != null ? 1 : 0)} shared materials, no normal map, no shadow{own}";
        }
    }
}
