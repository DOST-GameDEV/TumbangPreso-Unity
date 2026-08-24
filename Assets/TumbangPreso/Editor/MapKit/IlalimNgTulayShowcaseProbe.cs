using System;
using System.IO;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    public static class IlalimNgTulayShowcaseProbe
    {
        private const string ScenePath = "Assets/TumbangPreso/Scenes/Maps/IlalimNgTulay.unity";
        private const string BrainDir = @"C:\Users\matth\.gemini\antigravity\brain\61b67b47-0ca5-4f60-8bf9-89affab2ee54";
        private const int ShotWidth = 1280;
        private const int ShotHeight = 720;

        [MenuItem("Tumbang Preso/Capture Ilalim Ng Tulay Showcase")]
        public static void RunFromMenu() => Execute();

        public static void Run()
        {
            bool ok = Execute();
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool Execute()
        {
            Debug.Log("[IlalimNgTulayShowcaseProbe] Loading scene...");
            Directory.CreateDirectory(BrainDir);
            Directory.CreateDirectory("Logs");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // 1. High-Angle Cinematic Overview (Avenue, Bridge, Train, Pillars, Buildings)
            CaptureCameraShot(
                new Vector3(12.0f, 13.5f, -10.0f),
                Quaternion.Euler(30.0f, -48.0f, 0.0f),
                58.0f,
                Path.Combine(BrainDir, "ilalim_overview.png")
            );

            // 2. PC Express Storefront Showcase (Showroom, GPU Boxes, RGB Fans, Signboard, Boost Pad)
            CaptureCameraShot(
                new Vector3(-2.2f, 1.9f, 5.5f),
                Quaternion.Euler(4.0f, -90.0f, 0.0f),
                65.0f,
                Path.Combine(BrainDir, "ilalim_pcexpress.png")
            );

            // 3. Thrower's Gameplay Perspective (View from Throwing Line Z=3m looking at Lata Z=13.5m)
            CaptureCameraShot(
                new Vector3(0.0f, 1.8f, 1.5f),
                Quaternion.Euler(6.0f, 0.0f, 0.0f),
                68.0f,
                Path.Combine(BrainDir, "ilalim_thrower_view.png")
            );

            // 4. Street Life (Pisonet Arcade, Pares Food Cart, Street Clutter)
            CaptureCameraShot(
                new Vector3(2.5f, 1.8f, 0.0f),
                Quaternion.Euler(6.0f, 75.0f, 0.0f),
                62.0f,
                Path.Combine(BrainDir, "ilalim_street_life.png")
            );

            // Copy to Logs folder
            File.Copy(Path.Combine(BrainDir, "ilalim_overview.png"), "Logs/ilalim_overview.png", true);
            File.Copy(Path.Combine(BrainDir, "ilalim_pcexpress.png"), "Logs/ilalim_pcexpress.png", true);
            File.Copy(Path.Combine(BrainDir, "ilalim_thrower_view.png"), "Logs/ilalim_thrower_view.png", true);
            File.Copy(Path.Combine(BrainDir, "ilalim_street_life.png"), "Logs/ilalim_street_life.png", true);

            Debug.Log("[IlalimNgTulayShowcaseProbe] All showcase screenshots captured successfully!");
            return true;
        }

        private static void CaptureCameraShot(Vector3 pos, Quaternion rot, float fov, string outPath)
        {
            var camGo = new GameObject("~ShowcaseCam");
            camGo.transform.position = pos;
            camGo.transform.rotation = rot;

            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 120.0f;
            cam.clearFlags = CameraClearFlags.Skybox;

            camGo.AddComponent<ColourGrade>().Set(1.05f, 1.10f, 1.15f, 0.92f, 1.85f);

            var rt = new RenderTexture(ShotWidth, ShotHeight, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4
            };
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(ShotWidth, ShotHeight, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, ShotWidth, ShotHeight), 0, 0);
            tex.Apply();

            RenderTexture.active = null;
            cam.targetTexture = null;

            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Debug.Log($"[IlalimNgTulayShowcaseProbe] Saved shot to {outPath} ({new FileInfo(outPath).Length / 1024} KB)");

            UnityEngine.Object.DestroyImmediate(tex);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(camGo);
        }
    }
}
