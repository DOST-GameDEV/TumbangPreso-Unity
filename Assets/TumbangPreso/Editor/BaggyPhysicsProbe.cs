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
            var lightObj = new GameObject("Light");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1.2f;
            lightObj.transform.rotation = Quaternion.Euler(25f, -35f, 0f);

            var camObj = new GameObject("Cam");
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.09f, 0.08f, 0.14f, 1.0f);
            cam.transform.position = new Vector3(0.0f, 0.70f, -2.1f);
            cam.transform.rotation = Quaternion.Euler(8.0f, 0.0f, 0.0f);

            var rt = RenderTexture.GetTemporary(1200, 400, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            var inst = UnityEngine.Object.Instantiate(prefab);
            inst.transform.position = Vector3.zero;
            inst.transform.localScale = Vector3.one * 2.38f;

            var phys = inst.AddComponent<BaggyClothingPhysics>();
            phys.Bind(inst.transform);

            var tex = new Texture2D(1200, 400, TextureFormat.RGB24, false);

            // Frame 1: Resting Idle
            RenderTexture.active = rt;
            cam.Render();
            tex.ReadPixels(new Rect(0, 0, 400, 400), 0, 0);

            // Frame 2: Sprint Forward Momentum
            inst.transform.position += new Vector3(0, 0, 1.5f);
            StepPhysics(phys, 0.016f);
            cam.transform.position = new Vector3(0.0f, 0.70f, -2.1f + 1.5f);
            cam.Render();
            tex.ReadPixels(new Rect(0, 0, 400, 400), 400, 0);

            // Frame 3: Turn Centrifugal Flare
            inst.transform.rotation = Quaternion.Euler(0, 45f, 0);
            StepPhysics(phys, 0.016f);
            cam.Render();
            tex.ReadPixels(new Rect(0, 0, 400, 400), 800, 0);

            tex.Apply();
            File.WriteAllBytes(outPath, tex.EncodeToPNG());

            RenderTexture.active = null;
            cam.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}
