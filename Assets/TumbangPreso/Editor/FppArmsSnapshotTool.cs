using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    public static class FppArmsSnapshotTool
    {
        private const string OutDir = "Logs/shots-fpp";

        /// <summary>
        /// ⚠️⚠️ BUMP THIS EVERY CAPTURE. `CLAUDE.md` § 6.1: chat clients cache images by
        /// filename, so overwriting a render leaves the previous one on screen and the whole
        /// review is conducted against an image that is not on disk any more. This tool wrote
        /// `fpp_nemu_showcase.png` unversioned for its whole life, which is why nobody could
        /// review two iterations of an arm in one sitting.
        /// </summary>
        private const string Version = "v16";
        private const int Width = 1280;
        private const int Height = 720;

        private static readonly string[] Characters =
        {
            "sean", "zack", "dante", "cheska", "nemu", "phaister",
            "bayan", "maring", "totoy", "inday", "kuya_boy", "ate_girlie",
            "tikboy", "bebang", "jun_jun", "lola_pacing", "mang_kanor", "aling_nena",
        };

        [MenuItem("Tumbang Preso/Capture FPP Arms Screenshots")]
        public static void CaptureAllFromMenu() => Execute();

        public static void CaptureAll()
        {
            InspectHeroModels();
            Execute();
            EditorApplication.Exit(0);
        }

        private static void InspectHeroModels()
        {
            var book = Resources.Load<RosterBook>("RosterBook");
            if (book == null) return;

            foreach (var heroId in Characters)
            {
                var person = book.People.Find(p => p.Id == heroId);
                if (person == null || person.Model == null) continue;

                Debug.Log($"=== HERO MODEL: {heroId} ===");
                var smrs = person.Model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var smr in smrs)
                {
                    Debug.Log($"  SMR: {smr.name}, mesh={smr.sharedMesh?.name}, verts={smr.sharedMesh?.vertexCount}, bones={smr.bones?.Length}");
                    if (smr.bones != null)
                    {
                        for (int i = 0; i < smr.bones.Length; i++)
                        {
                            var b = smr.bones[i];
                            Debug.Log($"    Bone [{i}]: {b.name}, pos={b.localPosition}, rot={b.localRotation.eulerAngles}");
                        }
                    }
                }
            }
        }

        private static void Execute()
        {
            Directory.CreateDirectory(OutDir);

            // Open Eskinita or create isolated scene
            if (File.Exists("Assets/TumbangPreso/Scenes/Arena/Eskinita.unity"))
            {
                EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Arena/Eskinita.unity", OpenSceneMode.Single);
            }
            else
            {
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            }

            var camGo = new GameObject("~FppCaptureCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 58.0f;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 100.0f;

            // Create lighting matching in-game toon shading balance
            var lightGo = new GameObject("~FppCaptureLight");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.05f;
            light.color = new Color(1.0f, 0.98f, 0.95f);
            light.transform.rotation = Quaternion.Euler(45.0f, -30.0f, 0);

            var fillLightGo = new GameObject("~FppFillLight");
            var fillLight = fillLightGo.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.intensity = 0.35f;
            fillLight.color = new Color(0.75f, 0.78f, 0.85f);
            fillLight.transform.rotation = Quaternion.Euler(30.0f, 150.0f, 0);

            var armsMount = new GameObject("~ViewmodelMount");
            armsMount.transform.position = Vector3.zero;
            armsMount.transform.localScale = Vector3.one * 0.72f;
            armsMount.transform.localRotation = Quaternion.identity;

            var arms = armsMount.AddComponent<CameraSystem.ViewmodelArms>();
            arms.EnsureBuilt();

            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            foreach (string charId in Characters)
            {
                arms.SetCharacter(charId);

                // --- 1. In-Game First Person Perspective ---
                cam.transform.position = new Vector3(0.0f, 0.12f, -0.16f);
                cam.transform.rotation = Quaternion.Euler(22.0f, 0, 0);

                arms.SetHolding(true);
                arms.StepVisuals(0.016f, snap: true);
                cam.Render();
                SaveTexture(rt, Path.Combine(OutDir, $"fpp_{charId}_holding_{Version}.png"));

                arms.SetHolding(false);
                arms.StepVisuals(0.016f, snap: true);
                cam.Render();
                SaveTexture(rt, Path.Combine(OutDir, $"fpp_{charId}_empty_{Version}.png"));

                // --- 2. Full Arm Showcase Inspect View (Showing sleeves, pauldron, markings, bracelets) ---
                cam.transform.position = new Vector3(0.0f, 0.42f, -0.82f);
                cam.transform.rotation = Quaternion.Euler(28.0f, 0, 0);

                arms.SetHolding(true);
                arms.StepVisuals(0.016f, snap: true);
                cam.Render();
                SaveTexture(rt, Path.Combine(OutDir, $"fpp_{charId}_showcase_{Version}.png"));
            }

            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(camGo);
            UnityEngine.Object.DestroyImmediate(lightGo);
            UnityEngine.Object.DestroyImmediate(fillLightGo);

            Debug.Log("[FppArmsSnapshotTool] Captured FPP arms screenshots for all characters successfully!");
        }

        private static void SaveTexture(RenderTexture rt, string filePath)
        {
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);
            UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}
