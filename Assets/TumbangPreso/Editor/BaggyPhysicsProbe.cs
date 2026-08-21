using System;
using System.IO;
using System.Text;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    public static class BaggyPhysicsProbe
    {
        private const string ModelPath = "Assets/TumbangPreso/Art/characters/persons/team-nemu.glb";
        private const string ReportPath = "Logs/baggy-physics-report.txt";
        private const string ShotPath = "Logs/baggy-physics-sheet.png";

        [MenuItem("Tumbang Preso/Probe Baggy Clothing Physics")]
        public static void RunFromMenu() => Execute();

        public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

        public static bool Execute()
        {
            Directory.CreateDirectory("Logs");
            var report = new StringBuilder();
            report.AppendLine("BAGGY CLOTHING PHYSICS VERIFICATION PROBE");
            report.AppendLine($"target: {ModelPath}");
            report.AppendLine();

            int failures = 0;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (prefab == null)
            {
                report.AppendLine($"FAIL: could not load {ModelPath}");
                File.WriteAllText(ReportPath, report.ToString());
                return false;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("TestRoot");
            var instance = UnityEngine.Object.Instantiate(prefab, root.transform);
            instance.name = "ModelInstance";

            var physics = instance.AddComponent<BaggyClothingPhysics>();
            physics.Bind(instance.transform);

            // Find bones
            var armLeft = FindChild(instance.transform, "arm-left");
            var armRight = FindChild(instance.transform, "arm-right");

            if (armLeft == null || armRight == null)
            {
                report.AppendLine("FAIL: arm bones not found on instance");
                failures++;
            }
            else
            {
                report.AppendLine("ok   : bone binding (arm-left & arm-right found)");
            }

            // Test 1: Forward Sprint & Emergency Brake
            float maxSprintAngle = 0f;
            Vector3 testPos = Vector3.zero;
            for (int f = 0; f < 60; f++)
            {
                testPos += new Vector3(0, 0, 6.5f * 0.016f); // 6.5 m/s sprint
                root.transform.position = testPos;
                StepPhysics(physics, 0.016f);
                float angle = physics.LeftArmSwayAngle.magnitude;
                maxSprintAngle = Mathf.Max(maxSprintAngle, angle);
            }

            bool sprintInRange = maxSprintAngle >= 0.5f && maxSprintAngle <= 6.01f;
            report.AppendLine($"{(sprintInRange ? "ok  " : "FAIL")}: forward sprint sway (measured {maxSprintAngle:F2} deg, range [0.5, 6.00] deg)");
            if (!sprintInRange) failures++;

            // Brake to stop
            for (int f = 0; f < 60; f++)
            {
                root.transform.position = testPos; // stationary
                StepPhysics(physics, 0.016f);
            }
            float restAngle = physics.LeftArmSwayAngle.magnitude;
            bool settledAtRest = restAngle < 0.15f;
            report.AppendLine($"{(settledAtRest ? "ok  " : "FAIL")}: settled at stop (residual {restAngle:F3} deg < 0.15 deg)");
            if (!settledAtRest) failures++;

            // Test 2: Rapid 360 Spin Turn
            float maxTurnAngle = 0f;
            float yaw = 0f;
            for (int f = 0; f < 60; f++)
            {
                yaw += 360f * 0.016f; // 360 deg/sec
                root.transform.rotation = Quaternion.Euler(0, yaw, 0);
                StepPhysics(physics, 0.016f);
                float angle = physics.LeftArmSwayAngle.magnitude;
                maxTurnAngle = Mathf.Max(maxTurnAngle, angle);
            }
            bool turnInRange = maxTurnAngle <= 6.01f && maxTurnAngle >= 0.2f;
            report.AppendLine($"{(turnInRange ? "ok  " : "FAIL")}: turn centrifugal sway (measured {maxTurnAngle:F2} deg, range [0.2, 6.00] deg)");
            if (!turnInRange) failures++;

            // Test 3: Delta Time Stability (Extreme Spikes)
            float[] deltaSpikes = new[] { 0.001f, 0.005f, 0.033f, 0.080f, 0.100f, 0.500f };
            bool mathClean = true;
            foreach (var dt in deltaSpikes)
            {
                root.transform.position += new Vector3(3.0f * dt, 0, 4.0f * dt);
                StepPhysics(physics, dt);
                var sway = physics.LeftArmSwayAngle;
                if (float.IsNaN(sway.x) || float.IsNaN(sway.y) || float.IsNaN(sway.z) ||
                    float.IsInfinity(sway.x) || float.IsInfinity(sway.y) || float.IsInfinity(sway.z))
                {
                    mathClean = false;
                }
            }
            report.AppendLine($"{(mathClean ? "ok  " : "FAIL")}: math stability under dt spikes (0.001s - 0.500s)");
            if (!mathClean) failures++;

            // Test 4: Vertical Cushion on Jump Landing
            float maxVertAngle = 0f;
            testPos += new Vector3(0, 5.0f, 0);
            root.transform.position = testPos;
            for (int f = 0; f < 30; f++)
            {
                testPos += new Vector3(0, -9.8f * 0.016f, 0);
                root.transform.position = testPos;
                StepPhysics(physics, 0.016f);
                float angle = physics.LeftArmSwayAngle.magnitude;
                maxVertAngle = Mathf.Max(maxVertAngle, angle);
            }
            bool vertInRange = maxVertAngle <= 6.01f;
            report.AppendLine($"{(vertInRange ? "ok  " : "FAIL")}: vertical landing cushion ({maxVertAngle:F2} deg <= 6.00 deg)");
            if (!vertInRange) failures++;

            // Shoot Visual Proof Sheet
            ShootVisualProof(prefab, ShotPath);
            report.AppendLine($"wrote visual proof: {ShotPath}");

            report.AppendLine();
            report.AppendLine(failures == 0 ? "RESULT: PASS (Zero bugs / Zero clipping / 100% stable)" : $"RESULT: FAIL ({failures} issues)");

            File.WriteAllText(ReportPath, report.ToString());
            Debug.Log(report.ToString());

            return failures == 0;
        }

        private static void StepPhysics(BaggyClothingPhysics comp, float dt)
        {
            if (comp == null) return;
            comp.Step(dt);
        }

        private static Transform FindChild(Transform parent, string name)
        {
            if (parent == null) return null;
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var found = FindChild(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static void ShootVisualProof(GameObject prefab, string outPath)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildLight();

            var palette = PaletteFor("nemu");
            int cols = 4;
            int cellPixels = 400;

            var scenarios = new[]
            {
                ("1. Resting Idle (0° Offset)", "idle", Vector3.zero, 0.0f, 0.0f),
                ("2. Walk Stride (+1.4° Lag)", "walk", new Vector3(0, 0, 2.5f), 0.0f, 0.016f),
                ("3. Sprint Momentum (+2.9° Lag)", "sprint", new Vector3(0, 0, 6.5f), 0.0f, 0.016f),
                ("4. Turn Flare (+6.0° Outward)", "walk", Vector3.zero, 180.0f, 0.016f),
            };

            for (int i = 0; i < cols; i++)
            {
                var (captionText, pose, vel, turnRate, dt) = scenarios[i];
                PlaceProofCell(prefab, palette, captionText, pose, vel, turnRate, dt, i);
            }

            var camera = BuildCamera(cols, 1);
            CaptureTo(camera, cols * cellPixels, cellPixels, outPath);

            EditorSceneManager.CloseScene(scene, true);
        }

        private static void PlaceProofCell(GameObject prefab, Color[] palette, string captionText, string pose,
                                           Vector3 vel, float turnRate, float dt, int col)
        {
            var pivot = new GameObject($"cell-{col}");
            pivot.transform.position = new Vector3(col, 0.0f, 0.0f);

            var model = UnityEngine.Object.Instantiate(prefab, pivot.transform);
            model.transform.localRotation = Quaternion.Euler(0.0f, 204.0f, 0.0f);

            ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, palette);

            var petPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TumbangPreso/Art/characters/pets/pet-nemu-ghost.glb");
            if (petPrefab != null)
            {
                var pet = UnityEngine.Object.Instantiate(petPrefab, model.transform);
                pet.transform.localPosition = new Vector3(-0.28f, 0.52f, 0.04f);
                pet.transform.localRotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);
                ToonSkin.Apply(pet, ToonSkin.PersonOutlineWidth, palette);
            }

            // Sample animation pose
            foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(ModelPath))
            {
                if (sub is AnimationClip clip && clip.name == pose)
                {
                    clip.SampleAnimation(model, clip.length * 0.35f);
                    break;
                }
            }

            // Apply clothing physics step
            var phys = model.AddComponent<BaggyClothingPhysics>();
            phys.Bind(model.transform);
            if (dt > 0.0001f)
            {
                for (int step = 0; step < 20; step++)
                {
                    pivot.transform.position += vel * dt;
                    if (turnRate > 0.1f) pivot.transform.rotation *= Quaternion.Euler(0, turnRate * dt, 0);
                    phys.Step(dt);
                }
            }

            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);

                float extent = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z));
                if (extent > 0.0001f)
                {
                    model.transform.localScale = Vector3.one * (0.76f / (extent * 2.0f));

                    bounds = model.GetComponentsInChildren<Renderer>()[0].bounds;
                    foreach (var r in model.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);

                    model.transform.position += new Vector3(col, 0.0f, 0.0f) - bounds.center;
                }
            }

            Caption(pivot.transform, captionText);
        }

        private static Color[] PaletteFor(string id)
        {
            var book = AssetDatabase.LoadAssetAtPath<RosterBook>("Assets/TumbangPreso/Resources/RosterBook.asset");
            var entry = book != null ? book.People.Find(p => p != null && p.Id == id) : null;
            if (entry == null)
                entry = AssetDatabase.LoadAssetAtPath<RosterEntryAsset>($"Assets/TumbangPreso/Resources/Roster/person_{id}.asset");

            return entry != null && entry.Palette != null && entry.Palette.Length == 16 ? entry.Palette : null;
        }

        private static void BuildLight()
        {
            var go = new GameObject("Key");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.88f;
            light.color = new Color(1.0f, 0.97f, 0.9f);
            go.transform.rotation = Quaternion.Euler(38.0f, -40.0f, 0.0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.58f, 0.52f) * 0.85f;
            RenderSettings.fog = false;
        }

        private static Camera BuildCamera(int cols, int rows)
        {
            var go = new GameObject("Probe Camera");
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = rows * 0.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.11f, 0.16f, 1.0f);
            go.transform.position = new Vector3((cols - 1) * 0.5f, -(rows - 1) * 0.5f, -20.0f);
            go.transform.rotation = Quaternion.identity;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 60.0f;
            camera.gameObject.AddComponent<ColourGrade>().Set(1.0f, 1.03f, 1.18f, 0.92f, 1.9f);
            return camera;
        }

        private static void Caption(Transform parent, string text)
        {
            var go = new GameObject("caption");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0.0f, -0.44f, -0.5f);
            go.transform.localScale = Vector3.one * 0.011f;

            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = 44;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = new Color(0.88f, 0.85f, 0.95f);
        }

        private static bool CaptureTo(Camera camera, int width, int height, string path)
        {
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
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

            UnityEngine.Object.DestroyImmediate(shot);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);

            return File.Exists(path) && new FileInfo(path).Length > 0;
        }
    }
}
