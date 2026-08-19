using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    public static class IterationTurnaroundProbe
    {
        private const int CellPixels = 300;

        public static void Run()
        {
            var basePalette = PaletteFor("inday");

            // Option 1: Sun-Kissed Warm Tan (#ecaa6c) - Original Baseline
            var pal1 = (Color[])basePalette.Clone();
            pal1[10] = HexColor("e4a032");
            pal1[13] = HexColor("ecaa6c");
            pal1[14] = HexColor("d8985e");
            pal1[15] = HexColor("ecaa6c"); // 100% matched

            // Option 2: Natural Fair Peach (#f5b894)
            var pal2 = (Color[])basePalette.Clone();
            pal2[10] = HexColor("e4a032");
            pal2[13] = HexColor("f5b894");
            pal2[14] = HexColor("db9874");
            pal2[15] = HexColor("f5b894"); // 100% matched

            // Option 3: Pale Cream Porcelain (#fae2ce) - Noticeably Whiter
            var pal3 = (Color[])basePalette.Clone();
            pal3[10] = HexColor("e4a032");
            pal3[13] = HexColor("fae2ce");
            pal3[14] = HexColor("e0ba9e");
            pal3[15] = HexColor("fae2ce"); // 100% matched

            // Option 4: Snow White Alabaster (#fff0e2) - Maximum Fair
            var pal4 = (Color[])basePalette.Clone();
            pal4[10] = HexColor("e4a032");
            pal4[13] = HexColor("fff0e2");
            pal4[14] = HexColor("e8c8b0");
            pal4[15] = HexColor("fff0e2"); // 100% matched

            var angles = new[] { ("front", 180.0f), ("three-quarter", 220.0f),
                                 ("side", 270.0f), ("back", 0.0f) };

            var variants = new[]
            {
                new { Path = "Assets/TumbangPreso/Art/characters/persons/iteration-1.glb", Label = "Option 1: Warm Tan (#ecaa6c)", Palette = pal1, OutPath = "Logs/skin-opt1-turnaround.png" },
                new { Path = "Assets/TumbangPreso/Art/characters/persons/iteration-2.glb", Label = "Option 2: Fair Peach (#f5b894)", Palette = pal2, OutPath = "Logs/skin-opt2-turnaround.png" },
                new { Path = "Assets/TumbangPreso/Art/characters/persons/iteration-3.glb", Label = "Option 3: Pale Porcelain (#fae2ce)", Palette = pal3, OutPath = "Logs/skin-opt3-turnaround.png" },
                new { Path = "Assets/TumbangPreso/Art/characters/persons/iteration-4.glb", Label = "Option 4: Snow White (#fff0e2)", Palette = pal4, OutPath = "Logs/skin-opt4-turnaround.png" },
            };

            // Individual turnarounds
            for (int i = 0; i < variants.Length; i++)
            {
                var iterScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                BuildLight();

                for (int col = 0; col < angles.Length; col++)
                {
                    PlaceTurn(variants[i].Path, angles[col].Item1, angles[col].Item2, variants[i].Palette, col, 0);
                }

                var loopCam = BuildCamera(angles.Length, 1);
                CaptureTo(loopCam, angles.Length * CellPixels, CellPixels, variants[i].OutPath);
                EditorSceneManager.CloseScene(iterScene, true);
            }

            // Combined comparison sheet (4 rows x 4 angles)
            var compScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildLight();

            for (int row = 0; row < variants.Length; row++)
            {
                for (int col = 0; col < angles.Length; col++)
                {
                    PlaceTurn(variants[row].Path, $"{variants[row].Label} - {angles[col].Item1}", angles[col].Item2, variants[row].Palette, col, row);
                }
            }

            var compCam = BuildCamera(angles.Length, variants.Length);
            CaptureTo(compCam, angles.Length * CellPixels, variants.Length * CellPixels, "Logs/big-headpiece-comparison.png");

            EditorSceneManager.CloseScene(compScene, true);

            // Head close-up zoom comparison sheet (4 rows x 2 angles: front & 3/4)
            var zoomScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildLight();

            var zoomAngles = new[] { ("front", 180.0f), ("three-quarter", 220.0f), ("side", 270.0f) };
            float zoomSpacingX = 1.05f;
            float zoomSpacingY = 1.25f;

            for (int row = 0; row < variants.Length; row++)
            {
                for (int col = 0; col < zoomAngles.Length; col++)
                {
                    PlaceHeadZoom(variants[row].Path, $"{variants[row].Label} - {zoomAngles[col].Item1}", zoomAngles[col].Item2, variants[row].Palette, col, row, zoomSpacingX, zoomSpacingY);
                }
            }

            var zoomCam = BuildCamera(zoomAngles.Length, variants.Length, zoomSpacingX, zoomSpacingY);
            CaptureTo(zoomCam, zoomAngles.Length * CellPixels, variants.Length * CellPixels, "Logs/big-headpiece-zoom-comparison.png");

            EditorSceneManager.CloseScene(zoomScene, true);

            float lineupSpacing = 0.92f;

            // 1. Front Lineup (all 4 standing together side-by-side)
            var frontScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildLight();
            for (int c = 0; c < variants.Length; c++)
                PlaceTurn(variants[c].Path, variants[c].Label, 180.0f, variants[c].Palette, c, 0);
            var frontCam = BuildCamera(variants.Length, 1, lineupSpacing, 1.15f);
            frontCam.orthographicSize = 0.64f;
            CaptureTo(frontCam, 1920, 520, "Logs/settled_ushanka_skin_comparison_front_v100.png");
            EditorSceneManager.CloseScene(frontScene, true);

            // 2. Three-Quarter Lineup (all 4 standing together side-by-side)
            var threeQScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildLight();
            for (int c = 0; c < variants.Length; c++)
                PlaceTurn(variants[c].Path, variants[c].Label, 220.0f, variants[c].Palette, c, 0);
            var threeQCam = BuildCamera(variants.Length, 1, lineupSpacing, 1.15f);
            threeQCam.orthographicSize = 0.64f;
            CaptureTo(threeQCam, 1920, 520, "Logs/settled_ushanka_skin_comparison_three_quarter_v100.png");
            EditorSceneManager.CloseScene(threeQScene, true);

            // 3. Single Ushanka Turnaround
            var singleScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildLight();
            for (int col = 0; col < angles.Length; col++)
                PlaceTurn(variants[1].Path, angles[col].Item1, angles[col].Item2, variants[1].Palette, col, 0);
            var singleCam = BuildCamera(angles.Length, 1);
            CaptureTo(singleCam, angles.Length * CellPixels, CellPixels, "Logs/settled_ushanka_single_turnaround_v100.png");
            EditorSceneManager.CloseScene(singleScene, true);

            Debug.Log("IterationTurnaroundProbe complete.");
        }

        private static void PlaceHeadZoom(string modelPath, string label, float yaw, Color[] palette, int col, int row, float spacingX = 1.0f, float spacingY = 1.0f)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (prefab == null) return;

            var pivot = new GameObject($"zoom-{row}-{col}");
            pivot.transform.position = new Vector3(col * spacingX, -row * spacingY, 0.0f);

            var model = Object.Instantiate(prefab, pivot.transform);
            model.transform.localRotation = Quaternion.Euler(0.0f, CharacterVisual.PersonModelYaw + yaw, 0.0f);

            ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, palette);

            // Zoom into head region (y ~ 0.55 to 0.79)
            model.transform.localScale = Vector3.one * 1.35f;
            model.transform.position = pivot.transform.position - new Vector3(0.0f, 0.88f, 0.0f);

            Caption(pivot.transform, label, 0.44f);
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color c);
            return c;
        }

        private static void PlaceTurn(string modelPath, string label, float yaw, Color[] palette, int col, int row)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (prefab == null)
            {
                Debug.LogError($"Could not load model at {modelPath}");
                return;
            }

            var pivot = new GameObject($"turn-{row}-{col}");
            pivot.transform.position = new Vector3(col, -row, 0.0f);

            var model = Object.Instantiate(prefab, pivot.transform);
            model.transform.localRotation =
                Quaternion.Euler(0.0f, CharacterVisual.PersonModelYaw + yaw, 0.0f);

            ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, palette);

            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);

            float extent = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z));
            if (extent < 0.0001f) return;

            model.transform.localScale = Vector3.one * (0.76f / (extent * 2.0f));

            bounds = model.GetComponentsInChildren<Renderer>()[0].bounds;
            foreach (var r in model.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);

            model.transform.position += pivot.transform.position - bounds.center;

            Caption(pivot.transform, label);
        }

        private static Color[] PaletteFor(string id)
        {
            var book = AssetDatabase.LoadAssetAtPath<RosterBook>(
                "Assets/TumbangPreso/Resources/RosterBook.asset");

            var entry = book == null ? null : book.People.FirstOrDefault(p => p != null && p.Id == id);

            return entry != null && entry.Palette != null && entry.Palette.Length == 16
                ? entry.Palette : null;
        }

        private static void Caption(Transform parent, string text, float yOffset = -0.46f)
        {
            var go = new GameObject("caption");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0.0f, yOffset, -0.5f);
            go.transform.localScale = Vector3.one * 0.0065f;

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
            camera.orthographicSize = rows * spacingY * 0.5f;
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
                antiAliasing = 4,
            };

            camera.targetTexture = rt;
            camera.Render();

            RenderTexture.active = rt;

            var shot = new Texture2D(width, height, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            shot.Apply();

            RenderTexture.active = null;
            camera.targetTexture = null;

            Directory.CreateDirectory("Logs");
            File.WriteAllBytes(path, shot.EncodeToPNG());

            Object.DestroyImmediate(shot);
            rt.Release();
            Object.DestroyImmediate(rt);

            return File.Exists(path) && new FileInfo(path).Length > 0;
        }
    }
}
