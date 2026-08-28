using System.IO;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.EditorTools
{
    public static class PhaisterEndToEndProbe
    {
        private const string OutDir = "Logs/shots-hero";

        public static void Run()
        {
            Debug.Log("[PhaisterEndToEndProbe] Starting End-to-End Verification...");
            Directory.CreateDirectory(OutDir);

            // 1. Rebuild RosterBook
            RosterBookBuilder.BuildFromMenu();
            var book = RosterBook.Load();
            if (book == null)
            {
                Debug.LogError("[PhaisterEndToEndProbe] FAIL: RosterBook.Load() returned null!");
                EditorApplication.Exit(1);
                return;
            }

            // 2. Verify Roster entry
            var heroPeople = Roster.GetPeople(GameMode.HeroStrike);
            bool foundHero = false;
            int heroIndex = -1;
            for (int i = 0; i < heroPeople.Count; i++)
            {
                if (heroPeople[i].Id == "phaister")
                {
                    foundHero = true;
                    heroIndex = i;
                    break;
                }
            }

            if (!foundHero)
            {
                Debug.LogError("[PhaisterEndToEndProbe] FAIL: Phaister not found in Roster.GetPeople(GameMode.HeroStrike)!");
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log($"[PhaisterEndToEndProbe] PASS: Phaister found at Hero Index {heroIndex} ({heroPeople[heroIndex].Name})");

            // 3. Verify Hero Kit & Glyphs
            var kit = HeroAbilitySystem.CreateKitFor("phaister");
            if (kit == null || !(kit is PhaisterHeroKit))
            {
                Debug.LogError("[PhaisterEndToEndProbe] FAIL: HeroAbilitySystem.CreateKitFor(\"phaister\") failed!");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"[PhaisterEndToEndProbe] PASS: Kit Skill1 = {kit.Skill1.Name} ({kit.Skill1.Glyph}), Skill2 = {kit.Skill2.Name} ({kit.Skill2.Glyph}), Ult = {kit.Ultimate.Name} ({kit.Ultimate.Glyph})");

            // 4. Verify UI Theme Accent
            var accent = UiTheme.ColorForHero("phaister");
            Debug.Log($"[PhaisterEndToEndProbe] PASS: Phaister UI Theme Color = {accent}");

            // 5. Capture Hero UI Sheets & Inspect Tray
            HeroUiProbe.CaptureFromMenu();

            // 6. Capture Character Select Screen with Phaister
            CaptureCharacterSelectWithPhaister(heroIndex);

            Debug.Log("[PhaisterEndToEndProbe] END-TO-END VERIFICATION: ALL PASS!");
            EditorApplication.Exit(0);
        }

        private static void CaptureCharacterSelectWithPhaister(int heroIndex)
        {
            string scenePath = "Assets/TumbangPreso/Scenes/Ui/CharacterSelect.unity";
            if (!File.Exists(scenePath))
            {
                Debug.LogWarning("[PhaisterEndToEndProbe] CharacterSelect.unity scene not found, skipping scene capture.");
                return;
            }

            SceneFlow.SelectedMode = GameMode.HeroStrike;
            Settings.SettingsStore.Current.CharacterPick = heroIndex;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var cam = Camera.main;
            if (cam == null) cam = Object.FindFirstObjectByType<Camera>();
            if (cam == null)
            {
                var camGo = new GameObject("ProbeCam");
                cam = camGo.AddComponent<Camera>();
            }

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.10f, 0.18f, 1.0f);

            var charSelect = Object.FindFirstObjectByType<ConvertedCharacterSelect>();
            if (charSelect != null)
            {
                foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = cam;
                    canvas.planeDistance = 1.0f;
                    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvas.transform);
                }
                Canvas.ForceUpdateCanvases();
            }

            int width = 1280;
            int height = 720;
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            RenderTexture.active = null;
            cam.targetTexture = null;

            string shotPath = Path.Combine(OutDir, "character_select_phaister.png");
            File.WriteAllBytes(shotPath, tex.EncodeToPNG());
            Debug.Log($"[PhaisterEndToEndProbe] wrote {shotPath} ({width}x{height})");

            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
        }
    }
}
