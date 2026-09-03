using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    public static class HeroTurnaroundProbe
    {
        private const int CellPixels = 600;

        private static readonly (string Id, string Name, string Path)[] Heroes =
        {
            ("cheska",   "Cheska",   "Assets/TumbangPreso/Art/characters/persons/team-cheska.glb"),
            ("dante",    "Dante",    "Assets/TumbangPreso/Art/characters/persons/team-dante.glb"),
            ("nemu",     "Nemu",     "Assets/TumbangPreso/Art/characters/persons/team-nemu.glb"),
            ("phaister", "Phaister", "Assets/TumbangPreso/Art/characters/persons/team-phaister.glb"),
            ("sean",     "Sean",     "Assets/TumbangPreso/Art/characters/persons/team-sean.glb"),
            ("zack",     "Zack",     "Assets/TumbangPreso/Art/characters/persons/team-zack.glb"),
        };

        private static readonly (string Label, float Yaw)[] Angles =
        {
            ("Front", 180.0f),
            ("Three-Quarter", 220.0f),
            ("Side", 270.0f),
            ("Back", 0.0f),
        };

        [MenuItem("Tumbang Preso/Probe All Heroes Turnaround")]
        public static void RunFromMenu() => Execute();

        public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

        public static bool Execute()
        {
            var report = new StringBuilder();
            report.AppendLine("==================================================");
            report.AppendLine("HERO TURNAROUND PROBE - ALL 6 HEROES (4 ANGLES)");
            report.AppendLine("==================================================");

            Directory.CreateDirectory("Logs");

            // 1. Force synchronous import of models & pet
            foreach (var hero in Heroes)
            {
                AssetDatabase.ImportAsset(hero.Path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            }
            AssetDatabase.ImportAsset("Assets/TumbangPreso/Art/characters/pets/pet-nemu-ghost.glb", ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            bool ok = true;

            // 2. Render individual 4-angle turnarounds for each hero
            foreach (var hero in Heroes)
            {
                string outPath = $"Logs/turnaround_{hero.Id}.png";
                bool shotOk = ShootHeroTurnaround(hero.Id, hero.Name, hero.Path, outPath, report);
                ok &= shotOk;
                report.AppendLine($"Hero [{hero.Name}]: {(shotOk ? "SUCCESS -> " + outPath : "FAIL")}");
            }

            // 3. Render combined 6x4 comparison sheet (6 rows x 4 columns = 24 panels)
            string combinedPath = "Logs/all_heroes_4angles.png";
            bool compOk = ShootAllHeroesGrid(combinedPath, report);
            ok &= compOk;
            report.AppendLine($"All Heroes 6x4 Grid: {(compOk ? "SUCCESS -> " + combinedPath : "FAIL")}");

            // 4. Render 6-hero lineup (Front view side-by-side)
            string lineupPath = "Logs/all_heroes_lineup.png";
            bool lineupOk = ShootHeroLineup(lineupPath, report);
            ok &= lineupOk;
            report.AppendLine($"All Heroes Lineup: {(lineupOk ? "SUCCESS -> " + lineupPath : "FAIL")}");

            File.WriteAllText("Logs/hero-turnaround-probe.txt", report.ToString());
            Debug.Log(report.ToString());

            return ok;
        }

        private static bool ShootHeroTurnaround(string id, string name, string modelPath, string outPath, StringBuilder report)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildLight();

            var palette = PaletteFor(id);
            if (palette == null)
            {
                report.AppendLine($"WARNING: Palette not found for {id}, using default toon palette.");
            }

            for (int col = 0; col < Angles.Length; col++)
            {
                PlaceTurn(modelPath, id, $"{name.ToUpper()} - {Angles[col].Label.ToUpper()}", Angles[col].Yaw, palette, col, 0);
            }

            var camera = BuildCamera(Angles.Length, 1);
            camera.orthographicSize = 0.58f;
            bool success = CaptureTo(camera, Angles.Length * CellPixels, CellPixels, outPath);

            EditorSceneManager.CloseScene(scene, true);
            return success;
        }

        private static bool ShootAllHeroesGrid(string outPath, StringBuilder report)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildLight();

            float spacingX = 1.0f;
            float spacingY = 1.15f;

            for (int row = 0; row < Heroes.Length; row++)
            {
                var hero = Heroes[row];
                var palette = PaletteFor(hero.Id);

                for (int col = 0; col < Angles.Length; col++)
                {
                    string label = $"{hero.Name.ToUpper()}  [{Angles[col].Label.ToUpper()}]";
                    PlaceTurn(hero.Path, hero.Id, label, Angles[col].Yaw, palette, col, row, spacingX, spacingY);
                }
            }

            var camera = BuildCamera(Angles.Length, Heroes.Length, spacingX, spacingY);
            camera.orthographicSize = 3.65f;
            bool success = CaptureTo(camera, 2400, 3600, outPath);

            EditorSceneManager.CloseScene(scene, true);
            return success;
        }

        private static bool ShootHeroLineup(string outPath, StringBuilder report)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildLight();

            float spacingX = 0.92f;

            for (int col = 0; col < Heroes.Length; col++)
            {
                var hero = Heroes[col];
                var palette = PaletteFor(hero.Id);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(hero.Path);
                if (prefab == null) continue;

                var pivot = new GameObject($"lineup-{col}");
                pivot.transform.position = new Vector3(col * spacingX, 0.0f, 0.0f);

                var model = UnityEngine.Object.Instantiate(prefab, pivot.transform);
                model.transform.localRotation = Quaternion.Euler(0.0f, CharacterVisual.PersonModelYaw + 180.0f, 0.0f);

                ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, palette);

                foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(hero.Path))
                {
                    if (sub is AnimationClip clip && clip.name == "idle")
                    {
                        clip.SampleAnimation(model, 0.0f);
                        break;
                    }
                }

                if (hero.Id == "nemu")
                {
                    var petPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/characters/pets/pet-nemu-ghost.glb");
                    if (petPrefab != null)
                    {
                        var pet = UnityEngine.Object.Instantiate(petPrefab, model.transform);
                        pet.transform.localPosition = new Vector3(-0.30f, 0.60f, 0.04f);
                        pet.transform.localRotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);
                        ToonSkin.Apply(pet, ToonSkin.PersonOutlineWidth, palette);
                    }
                }

                var renderers = model.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) continue;

                model.transform.localScale = Vector3.one;

                var bounds = renderers[0].bounds;
                foreach (var r in model.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);

                model.transform.position += pivot.transform.position - new Vector3(bounds.center.x, bounds.min.y + 0.38f, bounds.center.z);

                Caption(pivot.transform, hero.Name.ToUpper(), -0.48f);
            }

            var camera = BuildCamera(Heroes.Length, 1, spacingX, 1.0f);
            camera.orthographicSize = 0.70f;
            bool success = CaptureTo(camera, 2400, 600, outPath);

            EditorSceneManager.CloseScene(scene, true);
            return success;
        }

        private static void PlaceTurn(string modelPath, string rosterId, string label, float yaw, Color[] palette, int col, int row = 0, float spacingX = 1.0f, float spacingY = 1.0f)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (prefab == null) return;

            var pivot = new GameObject($"turn-{row}-{col}");
            pivot.transform.position = new Vector3(col * spacingX, -row * spacingY, 0.0f);

            var model = UnityEngine.Object.Instantiate(prefab, pivot.transform);
            model.transform.localRotation = Quaternion.Euler(0.0f, CharacterVisual.PersonModelYaw + yaw, 0.0f);

            ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, palette);

            foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                if (sub is AnimationClip clip && clip.name == "idle")
                {
                    clip.SampleAnimation(model, 0.0f);
                    break;
                }
            }

            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);

            float baseRefHeight = 0.85f;
            float extent = Mathf.Max(bounds.extents.x, Mathf.Max(baseRefHeight * 0.5f, bounds.extents.z));
            if (extent < 0.0001f) return;

            model.transform.localScale = Vector3.one * (0.76f / (extent * 2.0f));

            bounds = model.GetComponentsInChildren<Renderer>()[0].bounds;
            foreach (var r in model.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);

            model.transform.position += pivot.transform.position - bounds.center;

            if (rosterId == "nemu")
            {
                var petPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/characters/pets/pet-nemu-ghost.glb");
                if (petPrefab != null)
                {
                    var pet = UnityEngine.Object.Instantiate(petPrefab, model.transform);
                    pet.transform.localPosition = new Vector3(-0.30f, 0.60f, 0.04f);
                    pet.transform.localRotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);
                    ToonSkin.Apply(pet, ToonSkin.PersonOutlineWidth, palette);
                }
            }

            Caption(pivot.transform, label, -0.45f);
        }

        private static Color[] PaletteFor(string id)
        {
            var book = AssetDatabase.LoadAssetAtPath<RosterBook>(
                "Assets/TumbangPreso/Resources/RosterBook.asset");

            var entry = book == null ? null : book.People.FirstOrDefault(p => p != null && p.Id == id);
            if (entry == null)
                entry = AssetDatabase.LoadAssetAtPath<RosterEntryAsset>($"Assets/TumbangPreso/Resources/Roster/person_{id}.asset");

            if (entry != null && entry.Palette != null && entry.Palette.Length == 16)
                return entry.Palette;

            string tresPath = $"MapSource/materials_persons/person_team-{id}.tres";
            if (File.Exists(tresPath))
            {
                var match = Regex.Match(File.ReadAllText(tresPath), @"shader_parameter/palette\s*=\s*PackedColorArray\(([^)]*)\)");
                if (match.Success)
                {
                    var parts = match.Groups[1].Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    var colors = new List<Color>();
                    for (int i = 0; i + 3 < parts.Length; i += 4)
                    {
                        if (float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out float r) &&
                            float.TryParse(parts[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out float g) &&
                            float.TryParse(parts[i + 2], NumberStyles.Float, CultureInfo.InvariantCulture, out float b) &&
                            float.TryParse(parts[i + 3], NumberStyles.Float, CultureInfo.InvariantCulture, out float a))
                        {
                            colors.Add(new Color(r, g, b, a));
                        }
                    }
                    if (colors.Count == 16) return colors.ToArray();
                }
            }

            return null;
        }

        private static void Caption(Transform parent, string text, float yOffset = -0.45f)
        {
            var go = new GameObject("caption");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0.0f, yOffset, -0.5f);
            go.transform.localScale = Vector3.one * 0.007f;

            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = 38;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = new Color(0.88f, 0.90f, 0.95f);
        }

        private static void BuildLight()
        {
            var go = new GameObject("Key");
            var light = go.AddComponent<Light>();

            light.type = LightType.Directional;
            light.intensity = 0.85f;
            light.color = new Color(1.0f, 0.97f, 0.9f);
            go.transform.rotation = Quaternion.Euler(38.0f, -40.0f, 0.0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.58f, 0.52f) * 0.78f;
            RenderSettings.fog = false;
        }

        private static Camera BuildCamera(int cols, int rows, float spacingX = 1.0f, float spacingY = 1.0f)
        {
            var go = new GameObject("Probe Camera");
            var camera = go.AddComponent<Camera>();

            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.16f, 0.17f, 0.22f, 1.0f);

            go.transform.position = new Vector3((cols - 1) * spacingX * 0.5f, -(rows - 1) * spacingY * 0.5f, -20.0f);
            go.transform.rotation = Quaternion.identity;

            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 60.0f;

            camera.gameObject.AddComponent<ColourGrade>().Set(1.0f, 1.03f, 1.18f, 0.92f, 1.9f);

            return camera;
        }

        private static bool CaptureTo(Camera camera, int width, int height, string path)
        {
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 8,
            };

            camera.targetTexture = rt;
            camera.Render();

            RenderTexture.active = rt;

            var shot = new Texture2D(width, height, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            shot.Apply();

            RenderTexture.active = null;
            camera.targetTexture = null;

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(path, shot.EncodeToPNG());

            UnityEngine.Object.DestroyImmediate(shot);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);

            return File.Exists(path) && new FileInfo(path).Length > 0;
        }
    }
}
