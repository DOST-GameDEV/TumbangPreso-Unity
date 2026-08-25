using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    public static class InGameAngleProbe
    {
        private const string ModelPath = "Assets/TumbangPreso/Art/characters/persons/team-iggy.glb";
        private const string RosterId = "kuya_boy";
        private const int CellPixels = 340;

        [MenuItem("Tumbang Preso/Probe Iggy In-Game Angles")]
        public static void RunFromMenu() => Execute();

        public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

        public static bool Execute()
        {
            var palette = PaletteFor(RosterId);
            Directory.CreateDirectory("Logs");

            // 1. Eight-angle 360° orbital showcase at gameplay isometric elevation (pitch 18°)
            ShootOrbit(palette, "Logs/iggy_8angle_orbit.png");

            // 2. Gameplay Action Sheet (Third-Person Player View, Aiming Throw, Sprinting, Smirk)
            ShootActionSheet(palette, "Logs/iggy_ingame_action_sheet.png");

            // 3. Head Zoom 4-Angle Turnaround (Focused on Mohawk details)
            ShootHeadZoom(palette, "Logs/iggy_head_closeup_orbit.png");

            // 4. True In-Game Scale Cast Lineup (Shared Scale Grounded Comparison)
            ShootCastLineup("Logs/cast_lineup.png");

            return true;
        }

        private static void ShootOrbit(Color[] palette, string outPath)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildLight();

            var angles = new[]
            {
                ("0° Back (Player View)", 0.0f),
                ("45° Rear-Right", 45.0f),
                ("90° Right Profile", 90.0f),
                ("135° Front-Right", 135.0f),
                ("180° Front View", 180.0f),
                ("225° Front-Left (Flame Focus)", 220.0f),
                ("270° Left Profile (Flame Crest)", 270.0f),
                ("315° Rear-Left (Mohawk Spine)", 315.0f)
            };

            for (int i = 0; i < angles.Length; i++)
            {
                PlacePose(ModelPath, palette, "idle", angles[i].Item2, i, 0, angles[i].Item1);
            }

            var camera = BuildCamera(angles.Length, 1, pitch: 18.0f);
            CaptureTo(camera, angles.Length * CellPixels, CellPixels + 60, outPath);

            EditorSceneManager.CloseScene(scene, true);
        }

        private static void ShootActionSheet(Color[] palette, string outPath)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildLight();

            var shots = new[]
            {
                ("1. Third-Person Idle (Player View)", "idle", 20.0f, true),
                ("2. Sprinting Forward (Rear Cam)", "sprint", 15.0f, false),
                ("3. Slipper Aim & Wind-up (Action Cam)", "holding-right-shoot", 40.0f, true),
                ("4. Cheerful Smile / Emote (Front View)", "emote-yes", 180.0f, false),
            };

            for (int i = 0; i < shots.Length; i++)
            {
                PlacePose(ModelPath, palette, shots[i].Item2, shots[i].Item3, i, 0, shots[i].Item1, shots[i].Item4);
            }

            var camera = BuildCamera(shots.Length, 1, pitch: 20.0f);
            CaptureTo(camera, shots.Length * (CellPixels + 30), CellPixels + 60, outPath);

            EditorSceneManager.CloseScene(scene, true);
        }

        private static void ShootHeadZoom(Color[] palette, string outPath)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildLight();

            var angles = new[]
            {
                ("Front Head View", 180.0f),
                ("3/4 Front (Flame Focus)", 220.0f),
                ("Left Side (Mohawk Profile)", 270.0f),
                ("Back View (Mohawk Spine)", 0.0f)
            };

            for (int i = 0; i < angles.Length; i++)
            {
                PlaceHeadZoom(ModelPath, palette, angles[i].Item2, i, 0, angles[i].Item1);
            }

            var camera = BuildCamera(angles.Length, 1, pitch: 8.0f);
            camera.orthographicSize = 0.40f;
            CaptureTo(camera, angles.Length * CellPixels, CellPixels + 60, outPath);

            EditorSceneManager.CloseScene(scene, true);
        }

        private static void PlacePose(string path, Color[] palette, string pose, float yaw, int col, int row, string label, bool slipper = false)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;

            var pivot = new GameObject($"cell-{row}-{col}");
            pivot.transform.position = new Vector3(col * 1.0f, -row * 1.0f, 0.0f);

            var model = Object.Instantiate(prefab, pivot.transform);
            model.transform.localRotation = Quaternion.Euler(0.0f, CharacterVisual.PersonModelYaw + yaw, 0.0f);

            ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, palette);

            foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (!(sub is AnimationClip clip) || clip.name != pose) continue;
                clip.SampleAnimation(model, clip.length * 0.35f);
                break;
            }

            if (slipper) HangSlipper(model);

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

        private static void PlaceHeadZoom(string path, Color[] palette, float yaw, int col, int row, string label)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;

            var pivot = new GameObject($"head-{row}-{col}");
            pivot.transform.position = new Vector3(col * 1.0f, -row * 1.0f, 0.0f);

            var model = Object.Instantiate(prefab, pivot.transform);
            model.transform.localRotation = Quaternion.Euler(0.0f, CharacterVisual.PersonModelYaw + yaw, 0.0f);

            ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, palette);

            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);

            model.transform.localScale = Vector3.one * 1.6f;
            // Center specifically on head (y ≈ 0.58 in world bounds)
            model.transform.position += pivot.transform.position - new Vector3(bounds.center.x, 0.58f, bounds.center.z);

            Caption(pivot.transform, label, yOffset: -0.34f);
        }

        private static void HangSlipper(GameObject model)
        {
            var skinned = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (string arm in new[] { "arm-right", "arm-left" })
            {
                var target = skinned.FirstOrDefault(s => s != null && s.bones != null && s.bones.Any(b => b != null && b.name == arm));
                if (target == null) continue;

                int index = System.Array.FindIndex(target.bones, b => b != null && b.name == arm);
                if (!CharacterVisual.PalmCentre(target, index, out Vector3 palm)) continue;

                var book = AssetDatabase.LoadAssetAtPath<RosterBook>("Assets/TumbangPreso/Resources/RosterBook.asset");
                var entry = book == null || book.Slippers.Count == 0 ? null : book.Slippers[0];
                if (entry == null || entry.Model == null) return;

                var shoe = Object.Instantiate(entry.Model, target.bones[index]);
                shoe.transform.localPosition = palm + Vector3.up * CharacterVisual.HandTopLift;
                shoe.transform.localRotation = Quaternion.identity;

                ToonSkin.Apply(shoe, ToonSkin.PropOutlineWidth);
                return;
            }
        }

        private static void Caption(Transform parent, string text, float yOffset = -0.46f)
        {
            var go = new GameObject("caption");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0.0f, yOffset, -0.5f);
            go.transform.localScale = Vector3.one * 0.011f;

            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = 44;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = new Color(0.88f, 0.90f, 0.94f);
        }

        private static void BuildLight()
        {
            var go = new GameObject("Key");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.88f;
            light.color = new Color(1.0f, 0.97f, 0.90f);
            go.transform.rotation = Quaternion.Euler(38.0f, -40.0f, 0.0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.58f, 0.52f) * 0.82f;
            RenderSettings.fog = false;
        }

        private static Camera BuildCamera(int cols, int rows, float pitch = 0.0f)
        {
            var go = new GameObject("Probe Camera");
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = rows * 0.55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.14f, 0.15f, 0.19f, 1.0f);

            Vector3 center = new Vector3((cols - 1) * 0.50f, -(rows - 1) * 0.50f, 0.0f);
            Quaternion rot = Quaternion.Euler(pitch, 0.0f, 0.0f);
            go.transform.rotation = rot;
            go.transform.position = center - rot * Vector3.forward * 15.0f;

            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 40.0f;

            camera.gameObject.AddComponent<ColourGrade>().Set(1.0f, 1.03f, 1.18f, 0.92f, 1.9f);
            return camera;
        }

        private static bool CaptureTo(Camera camera, int width, int height, string path)
        {
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;

            var shot = new Texture2D(width, height, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            shot.Apply();

            RenderTexture.active = null;
            camera.targetTexture = null;

            File.WriteAllBytes(path, shot.EncodeToPNG());
            Object.DestroyImmediate(shot);
            rt.Release();
            Object.DestroyImmediate(rt);

            return File.Exists(path) && new FileInfo(path).Length > 0;
        }

        private static Color[] PaletteFor(string id)
        {
            var book = AssetDatabase.LoadAssetAtPath<RosterBook>("Assets/TumbangPreso/Resources/RosterBook.asset");
            var entry = book == null ? null : book.People.FirstOrDefault(p => p != null && p.Id == id);
            return entry != null && entry.Palette != null && entry.Palette.Length == 16 ? entry.Palette : null;
        }

        private static void ShootCastLineup(string outPath)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildLight();

            var cast = new[]
            {
                new { Path = "Assets/TumbangPreso/Art/characters/persons/character-male-b.glb", RosterId = "kuya_boy", Label = "Base (male-b)" },
                new { Path = "Assets/TumbangPreso/Art/characters/persons/team-inday.glb", RosterId = "inday", Label = "Inday" },
                new { Path = "Assets/TumbangPreso/Art/characters/persons/team-zack.glb", RosterId = "zack", Label = "Zack" },
                new { Path = "Assets/TumbangPreso/Art/characters/persons/team-bayan.glb", RosterId = "bayan", Label = "Bayan" },
                new { Path = "Assets/TumbangPreso/Art/characters/persons/team-iggy.glb", RosterId = "kuya_boy", Label = "Iggy (Heavyweight)" },
            };

            for (int i = 0; i < cast.Length; i++)
            {
                var pal = PaletteFor(cast[i].RosterId);
                PlaceTrueScale(cast[i].Path, pal, 180.0f, i, 0, cast[i].Label);
            }

            var camera = BuildCamera(cast.Length, 1, pitch: 12.0f);
            camera.orthographicSize = 0.52f;
            Vector3 center = new Vector3((cast.Length - 1) * 0.90f * 0.5f, 0.40f, 0.0f);
            Quaternion rot = Quaternion.Euler(12.0f, 0.0f, 0.0f);
            camera.transform.rotation = rot;
            camera.transform.position = center - rot * Vector3.forward * 15.0f;

            CaptureTo(camera, cast.Length * CellPixels, CellPixels + 80, outPath);

            EditorSceneManager.CloseScene(scene, true);
        }

        private static void PlaceTrueScale(string path, Color[] palette, float yaw, int col, int row, string label)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;

            var pivot = new GameObject($"lineup-{row}-{col}");
            pivot.transform.position = new Vector3(col * 0.90f, 0.0f, 0.0f);

            var model = Object.Instantiate(prefab, pivot.transform);
            model.transform.localRotation = Quaternion.Euler(0.0f, CharacterVisual.PersonModelYaw + yaw, 0.0f);

            ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, palette);

            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);

            // Ground the feet at y = 0
            model.transform.position += pivot.transform.position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

            Caption(pivot.transform, label, yOffset: -0.06f);
        }
    }
}
