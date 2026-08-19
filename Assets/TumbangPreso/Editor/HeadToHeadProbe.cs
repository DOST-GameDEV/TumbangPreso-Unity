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
    public static class HeadToHeadProbe
    {
        private const int CellPixels = 300;

        public static void Run()
        {
            var basePalette = PaletteFor("inday");

            var tanPalette = (Color[])basePalette.Clone();
            tanPalette[13] = HexColor("d88c48");
            tanPalette[14] = HexColor("a45c26");
            tanPalette[15] = HexColor("f4a868");

            var fairPalette = (Color[])basePalette.Clone();
            fairPalette[13] = HexColor("f5caa6");
            fairPalette[14] = HexColor("cf9a7e");
            fairPalette[15] = HexColor("ffe8d6");

            var angles = new[] { ("front", 180.0f), ("three-quarter", 220.0f),
                                 ("side", 270.0f), ("back", 0.0f) };

            var variants = new[]
            {
                new { Path = "Assets/TumbangPreso/Art/characters/persons/team-inday.glb", Label = "Option 1: Full Bangs - Brown / Warm Tan", Palette = tanPalette },
                new { Path = "Assets/TumbangPreso/Art/characters/persons/team-inday.glb", Label = "Option 2: Full Bangs - Fair Human", Palette = fairPalette },
            };

            // Render 2x4 Head to Head comparison
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
            CaptureTo(compCam, angles.Length * CellPixels, variants.Length * CellPixels, "Logs/full-bangs-comparison.png");

            EditorSceneManager.CloseScene(compScene, true);
            Debug.Log("HeadToHeadProbe complete.");
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

        private static void Caption(Transform parent, string text)
        {
            var go = new GameObject("caption");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0.0f, -0.44f, -0.5f);
            go.transform.localScale = Vector3.one * 0.010f;

            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = 44;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = new Color(0.85f, 0.87f, 0.92f);
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

        private static Camera BuildCamera(int cols, int rows)
        {
            var go = new GameObject("Probe Camera");
            var camera = go.AddComponent<Camera>();

            camera.orthographic = true;
            camera.orthographicSize = rows * 0.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.16f, 0.17f, 0.22f, 1.0f);

            go.transform.position = new Vector3((cols - 1) * 0.5f, -(rows - 1) * 0.5f, -20.0f);
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
