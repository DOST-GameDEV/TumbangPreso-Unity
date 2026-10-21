using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>Individually fitted finishes for Eskinita; imported masters remain intact.</summary>
    public static class EskinitaHouseFinishAuthor
    {
        public const string RootName = "EskinitaHouseRefinement";
        private const string Folder = "Assets/TumbangPreso/Art/EskinitaHouseFinishes";
        private const string Target = "Bahay_Rework_Bahay_4_W";
        private const int Tile = 256, Gutter = 4, Pitch = Tile + Gutter * 2;

        public static void ClearPrevious(string map)
        {
            if (map != "Eskinita") return;
            var previous = GameObject.Find("Eskinita/Dressing/" + RootName);
            if (previous != null) Object.DestroyImmediate(previous);
        }

        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/Eskinita.unity");
            var report = new StringBuilder();
            FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Logs/eskinita-house-finish");
            File.WriteAllText("Logs/eskinita-house-finish/author.txt", report.ToString());
            Debug.Log(report.ToString());
            EditorApplication.Exit(0);
        }

        public static void FinishLoadedScene(StringBuilder report)
        {
            var map = GameObject.Find("Eskinita");
            if (map == null) throw new InvalidOperationException("Only Eskinita may be loaded for these finishes.");
            var dressing = map.transform.Find("Dressing");
            var house = dressing.Find("Bahay/" + Target);
            if (house == null) throw new InvalidOperationException("The measured family-b house is missing.");
            var old = dressing.Find(RootName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var colliders = map.GetComponentsInChildren<Collider>(true).ToDictionary(c => c, c => c.bounds);
            var sourceMeshes = house.GetComponentsInChildren<MeshFilter>().ToDictionary(f => f, f => f.sharedMesh);
            var root = new GameObject(RootName).transform;
            root.SetParent(dressing, false);
            var finish = new GameObject("Bahay_4_W_FittedFinish").transform;
            finish.SetParent(root, false);
            finish.SetPositionAndRotation(house.position, house.rotation);
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();

            var material = CreateMaterial(Target);
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            var doorHeads = new List<Vector3>();
            int wallFaces = 0, roofFaces = 0;
            foreach (var filter in sourceMeshes.Keys)
            {
                var mesh = filter.sharedMesh;
                var points = mesh.vertices;
                var sourceUv = mesh.uv;
                var sourceNormals = mesh.normals;
                var indices = mesh.triangles;
                var matrix = finish.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                var normalMatrix = matrix.inverse.transpose;
                for (int i = 0; i < points.Length; i++)
                {
                    Vector3 p = matrix.MultiplyPoint3x4(points[i]);
                    if (Mathf.Abs(sourceUv[i].x - .21875f) < .018f && Mathf.Abs(p.z - 2) < .02f && Mathf.Abs(p.y - 1.35f) < .02f)
                        doorHeads.Add(p);
                }
                for (int t = 0; t < indices.Length; t += 3)
                {
                    int a = indices[t];
                    Vector3 normal = normalMatrix.MultiplyVector(sourceNormals[a]).normalized;
                    // UV columns are the already measured city atlas contract. Copy only
                    // real solid wall/roof triangles, preserving the original opening holes.
                    bool wall = Mathf.Abs(sourceUv[a].x - .46875f) < .018f && Mathf.Abs(normal.y) < .04f;
                    bool roof = Mathf.Abs(sourceUv[a].x - .09375f) < .018f && sourceUv[a].y >= .5f && normal.y > .2f;
                    // This body's embedded shrubs share the source green palette with roofs.
                    // The roof atlas consequently made them rust red. Restore only the
                    // measured foliage swatch here, not the house's timber door or trim.
                    bool foliage = Mathf.Abs(sourceUv[a].x - .21875f) < .018f && sourceUv[a].y < .25f;
                    if (!wall && !roof && !foliage) continue;
                    int tile = foliage ? 8 : roof ? 4 + (normal.x < 0 ? 1 : 0) : Mathf.Abs(normal.z) > .9f ? (normal.z > 0 ? 0 : 1) : (normal.x > 0 ? 2 : 3);
                    int first = vertices.Count;
                    for (int c = 0; c < 3; c++)
                    {
                        int i = indices[t + c];
                        Vector3 n = normalMatrix.MultiplyVector(sourceNormals[i]).normalized;
                        Vector3 p = matrix.MultiplyPoint3x4(points[i]);
                        vertices.Add(p + n * (wall ? .012f : .009f));
                        normals.Add(n);
                        // Roof seams run down the actual slope, so their repetition axis is
                        // the ridge. The stepped low roof and tall roof need different axes.
                        Vector2 coordinate = foliage ? new Vector2(.5f, sourceUv[i].y / .25f) : roof ? (Mathf.Abs(n.x) > Mathf.Abs(n.z) ?
                            new Vector2((p.z + 5) / 10, (p.x + 5) / 10) : new Vector2((p.x + 5) / 10, (p.z + 5) / 10)) :
                            new Vector2(((Mathf.Abs(n.z) > .9f ? p.x : p.z) + 5) / 10, p.y / 6);
                        uv.Add(AtlasUv(tile, coordinate));
                        triangles.Add(first + c);
                    }
                    if (wall) wallFaces++; else if (roof) roofFaces++;
                }
            }
            if (wallFaces < 12 || roofFaces < 4) throw new InvalidOperationException("Measured wall/roof classification failed.");
            if (doorHeads.Count < 2) throw new InvalidOperationException("The original front door must be measured before fitting its shade.");
            float doorX = (doorHeads.Min(p => p.x) + doorHeads.Max(p => p.x)) * .5f;
            // The original front door is 1.35m tall in this deliberately chibi house.
            // Shade sits below the existing eave, behind the existing private lot boundary.
            Box(new Vector3(doorX, 1.67f, 2.26f), new Vector3(1.32f, .065f, .68f), Quaternion.Euler(8, 0, 0), 6);
            foreach (float x in new[] { doorX - .49f, doorX + .49f })
            {
                Box(new Vector3(x, 1.46f, 2.055f), new Vector3(.09f, .39f, .09f), Quaternion.identity, 7);
                Vector3 from = new Vector3(x, 1.29f, 2.08f), to = new Vector3(x, 1.61f, 2.47f);
                Box((from + to) * .5f, new Vector3(.075f, Vector3.Distance(from, to), .075f), Quaternion.FromToRotation(Vector3.up, to - from), 7);
            }
            for (int rib = 0; rib < 6; rib++)
                Box(new Vector3(doorX - .55f + rib * .22f, 1.709f, 2.26f), new Vector3(.022f, .018f, .65f), Quaternion.Euler(8, 0, 0), 6);

            void Box(Vector3 center, Vector3 size, Quaternion rotation, int tile)
            {
                Vector3[] corners = { new Vector3(-1,-1,-1), new Vector3(1,-1,-1), new Vector3(1,1,-1), new Vector3(-1,1,-1),
                    new Vector3(-1,-1,1), new Vector3(1,-1,1), new Vector3(1,1,1), new Vector3(-1,1,1) };
                int[][] faces = { new[]{0,3,2,1}, new[]{5,6,7,4}, new[]{4,7,3,0}, new[]{1,2,6,5}, new[]{3,7,6,2}, new[]{4,0,1,5} };
                foreach (var face in faces)
                {
                    var a = center + rotation * Vector3.Scale(corners[face[0]], size * .5f);
                    var b = center + rotation * Vector3.Scale(corners[face[1]], size * .5f);
                    var c = center + rotation * Vector3.Scale(corners[face[2]], size * .5f);
                    var normal = Vector3.Cross(b - a, c - a).normalized;
                    int first = vertices.Count;
                    for (int i = 0; i < 4; i++)
                    {
                        vertices.Add(center + rotation * Vector3.Scale(corners[face[i]], size * .5f));
                        normals.Add(normal); uv.Add(AtlasUv(tile, new Vector2(.5f, .5f)));
                    }
                    triangles.AddRange(new[] { first, first+1, first+2, first, first+2, first+3 });
                }
            }
            var fitted = new Mesh { name = "Bahay_4_W_wall_and_roof_finish" };
            fitted.SetVertices(vertices); fitted.SetNormals(normals); fitted.SetUVs(0, uv); fitted.SetTriangles(triangles, 0); fitted.RecalculateBounds();
            var meshPath = Folder + "/Bahay_4_W_finish.asset";
            var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (savedMesh == null) { AssetDatabase.CreateAsset(fitted, meshPath); savedMesh = fitted; }
            else { EditorUtility.CopySerialized(fitted, savedMesh); Object.DestroyImmediate(fitted); }
            finish.gameObject.AddComponent<MeshFilter>().sharedMesh = savedMesh;
            var renderer = finish.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // Retained solid still casts.
            finish.gameObject.isStatic = true;
            AirborneByDesign.Attach(finish.gameObject, "Thin finishes fitted to measured retained house walls and roofs; solid original geometry and collision are preserved.");

            if (sourceMeshes.Any(p => p.Key.sharedMesh != p.Value)) throw new InvalidOperationException("A source mesh changed.");
            if (root.GetComponentsInChildren<Collider>().Length != 0 || colliders.Count != map.GetComponentsInChildren<Collider>(true).Length ||
                colliders.Any(p => p.Key == null || p.Key.bounds != p.Value)) throw new InvalidOperationException("House finish changed gameplay collision.");
            AssetDatabase.SaveAssets();
            report.AppendLine($"{Target}: {wallFaces} fitted wall triangles, {roofFaces} roof triangles, {vertices.Count} vertices; one renderer/material.");
            report.AppendLine("Original opening holes, solid house mesh, household details, roof hue and gameplay colliders preserved. Only one Eskinita home changed.");
        }

        private static Vector2 AtlasUv(int tile, Vector2 coordinate)
        {
            coordinate.x = Mathf.Clamp01(coordinate.x); coordinate.y = Mathf.Clamp01(coordinate.y);
            return new Vector2((tile % 4 * Pitch + Gutter + .5f + coordinate.x * (Tile - 1)) / (Pitch * 4),
                (tile / 4 * Pitch + Gutter + .5f + coordinate.y * (Tile - 1)) / (Pitch * 3));
        }

        private static Material CreateMaterial(string instance)
        {
            var roof = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            string roofName = EnvColourPass.RoofAtlases[EnvColourPass.RoofIndexFor(instance)];
            roof.LoadImage(File.ReadAllBytes("Assets/TumbangPreso/Resources/Models/roofs/" + roofName + ".png"));
            Color tint = EnvColourPass.FacadeTints[EnvColourPass.FacadeIndexFor(instance)];
            Color wallColor = roof.GetPixelBilinear(.46875f, .40f) * tint;
            Color roofColor = roof.GetPixelBilinear(.09375f, .7f) * tint;
            wallColor.a = roofColor.a = 1;
            var atlas = new Texture2D(Pitch * 4, Pitch * 3, TextureFormat.RGB24, false);
            var pixels = new Color32[atlas.width * atlas.height];
            for (int tile = 0; tile < 12; tile++)
                for (int y = 0; y < Pitch; y++)
                    for (int x = 0; x < Pitch; x++)
                    {
                        float u = Mathf.Clamp01((x - Gutter) / (float)(Tile - 1));
                        float v = Mathf.Clamp01((y - Gutter) / (float)(Tile - 1));
                        Color color = tile < 4 ? Plaster(u * 10 - 5, v * 6, tile, wallColor) : tile < 6 ? Metal(u * 10 - 5, v * 10 - 5, tile, roofColor) :
                            tile == 6 ? new Color(.55f, .57f, .54f) : tile == 7 ? new Color(.32f, .29f, .23f) :
                            Color.Lerp(new Color(.19f, .29f, .14f), new Color(.40f, .53f, .28f), v);
                        pixels[(tile / 4 * Pitch + y) * atlas.width + tile % 4 * Pitch + x] = color;
                    }
            atlas.SetPixels32(pixels); atlas.Apply();
            string path = Folder + "/Bahay_4_W_materials.png";
            File.WriteAllBytes(path, atlas.EncodeToPNG());
            Object.DestroyImmediate(atlas); Object.DestroyImmediate(roof);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default; importer.sRGBTexture = true;
            importer.npotScale = TextureImporterNPOTScale.None; importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            string matPath = Folder + "/Bahay_4_W_materials.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (material == null) { material = new Material(Shader.Find("TumbangPreso/NearFade")); AssetDatabase.CreateAsset(material, matPath); }
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            material.color = Color.white;
            material.SetFloat("_SurfaceKind", 0); material.SetFloat("_SurfaceVertexRoles", 0);
            material.SetFloat("_Glossiness", .16f); material.SetFloat("_Metallic", 0);
            EditorUtility.SetDirty(material);
            if (ShaderUtil.ShaderHasError(material.shader)) throw new InvalidOperationException("Existing finish shader has a compile error.");
            return material;
        }

        private static Color Plaster(float x, float y, int side, Color paint)
        {
            // Authored broad trowel/paint variation. No random speckles or universal grime.
            if (y < 2.17f) paint = Color.Lerp(new Color(.76f, .71f, .61f), paint, .18f);
            float variation = .055f * Mathf.Sin(x * 1.2f + side * .8f) * Mathf.Sin(y * 1.7f + .9f);
            variation += .035f * Mathf.Sin(x * 2.7f + y * .55f + side);
            variation += .012f * Mathf.Sin(x * 41 + Mathf.Sin(y * 13)) * Mathf.Sin(y * 33 + side);
            float repair = side == 0 ? Patch(x, y, -2.85f, .35f, .55f, .21f) :
                side == 2 ? Patch(x, y, .8f, .5f, .70f, .28f) : 0;
            Color color = paint * (1 + variation);
            color = Color.Lerp(color, Color.Lerp(paint, new Color(.59f, .56f, .48f), .25f), repair * .7f);
            // A deliberately maintained lower paint course; not a black dirt gradient.
            if (y < .62f) color = Color.Lerp(color, new Color(.49f, .45f, .37f), .7f);
            if (Mathf.Abs(y - .62f) < .022f) color *= .9f;
            color.a = 1; return color;
        }

        private static float Patch(float x, float y, float cx, float cy, float rx, float ry)
        {
            float d = Mathf.Max(Mathf.Abs((x - cx) / rx), Mathf.Abs((y - cy) / ry));
            return 1 - Mathf.SmoothStep(.84f, 1, d);
        }

        private static Color Metal(float x, float y, int side, Color paint)
        {
            float sheet = Mathf.Floor((x + 5) / .82f);
            float tone = (sheet % 3 - 1) * .024f;
            float rib = Mathf.Pow(.5f + .5f * Mathf.Cos(x / .205f * Mathf.PI * 2), 4);
            float seam = Mathf.Abs(Mathf.Repeat(x + 5, .82f) - .025f) < .028f ? -.12f : 0;
            float overlap = Mathf.Abs(y - (side == 4 ? .9f : -.7f)) < .035f ? -.09f : 0;
            Color color = paint * (1 + tone + rib * .055f + seam + overlap);
            color.a = 1; return color;
        }
    }
}
