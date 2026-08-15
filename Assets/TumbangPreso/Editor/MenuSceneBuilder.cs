using System.Collections.Generic;
using System.IO;
using TumbangPreso.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Video;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Generates every menu scene and sets the build order.
    ///
    /// ⚠️ THE SPLASH MUST BE FIRST IN THE BUILD SETTINGS. Scene 0 is what a built game opens
    /// with, and the boot sting was an explicit requirement: it plays on every launch. Getting
    /// the order wrong means the shipped build starts on whatever scene happens to be at index
    /// 0, which in a fresh project is usually the last one somebody saved.
    ///
    /// ⚠️ EVERY MENU IS ONE COMPONENT ON AN EMPTY OBJECT. The screens build their own UI in
    /// code, so these scenes carry no authored widgets at all. That is what makes them
    /// regenerable: a palette change is one edit and a rebuild, not seventeen scenes to open.
    ///
    /// Run:
    ///   Unity.exe -batchmode -quit -nographics -projectPath . \
    ///             -executeMethod TumbangPreso.EditorTools.MenuSceneBuilder.BuildAll
    /// </summary>
    public static class MenuSceneBuilder
    {
        private const string OutDir = "Assets/TumbangPreso/Scenes";
        private const string MapDir = "Assets/TumbangPreso/Scenes/Maps";

        [MenuItem("Tumbang Preso/Build All Menu Scenes")]
        public static void BuildAllFromMenu() => Execute();

        public static void BuildAll() => EditorApplication.Exit(Execute() ? 0 : 1);

        private static bool Execute()
        {
            Directory.CreateDirectory(OutDir);

            BuildScreen<SplashScreen>(SceneFlow.Splash, WireSplash);
            BuildScreen<MainMenuScreen>(SceneFlow.MainMenu, null);
            BuildScreen<ModeSelectScreen>(SceneFlow.ModeSelect, null);
            BuildScreen<MatchSetupScreen>(SceneFlow.MatchSetup, null);
            BuildScreen<MultiplayerSetupScreen>(SceneFlow.MultiplayerSetup, null);
            BuildScreen<CharacterSelectScreen>(SceneFlow.CharacterSelect, null);
            BuildScreen<MatchResultScreen>(SceneFlow.MatchResult, null);

            SetBuildOrder();

            Debug.Log("[MenuSceneBuilder] built the menu flow and set the build order.");
            AssetDatabase.SaveAssets();
            return true;
        }

        private static void BuildScreen<T>(string sceneName, System.Action<T> wire)
            where T : MonoBehaviour
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ⚠️ A CAMERA EVEN THOUGH THE UI IS SCREEN-SPACE OVERLAY. Without one, Unity logs a
            // "no cameras rendering" warning over a screen that looks perfectly fine, and it
            // becomes noise everybody learns to scroll past, including on the day it matters.
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UiTheme.WoodDark;

            var go = new GameObject(sceneName);
            var comp = go.AddComponent<T>();
            wire?.Invoke(comp);

            EditorSceneManager.SaveScene(scene, $"{OutDir}/{sceneName}.unity");
        }

        /// <summary>
        /// ⚠️ THE VIDEO AND THE STING ARE WIRED SEPARATELY, matching the Godot build. The clip
        /// was exported with no audio track because Godot's only core codec is Theora, and the
        /// sting played as a normal sound cue. Keeping them separate means the sting still
        /// obeys the volume sliders, which is what a player expects when they turn the game
        /// down before it has finished booting.
        /// </summary>
        private static void WireSplash(SplashScreen splash)
        {
            var so = new SerializedObject(splash);

            var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(
                "Assets/TumbangPreso/Art/video/opening_animation.mp4");
            var sting = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/TumbangPreso/Art/audio/sfx/boot_sting.wav");

            if (clip == null) Debug.LogWarning("[MenuSceneBuilder] opening_animation.mp4 not found.");
            if (sting == null) Debug.LogWarning("[MenuSceneBuilder] boot_sting.wav not found.");

            so.FindProperty("_clip").objectReferenceValue = clip;
            so.FindProperty("_sting").objectReferenceValue = sting;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// ⚠️ ORDER MATTERS AND SPLASH IS INDEX 0. Everything else can be in any order, but a
        /// scene missing from this list loads fine in the editor and fails silently in a build,
        /// which is the classic "the button works on my machine" bug.
        /// </summary>
        private static void SetBuildOrder()
        {
            var wanted = new List<string>
            {
                $"{OutDir}/{SceneFlow.Splash}.unity",
                $"{OutDir}/{SceneFlow.MainMenu}.unity",
                $"{OutDir}/{SceneFlow.ModeSelect}.unity",
                $"{OutDir}/{SceneFlow.MatchSetup}.unity",
                $"{OutDir}/{SceneFlow.MultiplayerSetup}.unity",
                $"{OutDir}/{SceneFlow.CharacterSelect}.unity",
                $"{OutDir}/{SceneFlow.MatchResult}.unity",
                $"{MapDir}/{SceneFlow.Eskinita}.unity",
                $"{MapDir}/{SceneFlow.BayanPlaza}.unity",
                $"{OutDir}/VerticalSlice.unity",
            };

            var list = new List<EditorBuildSettingsScene>();
            foreach (var path in wanted)
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[MenuSceneBuilder] {path} does not exist yet, skipping.");
                    continue;
                }
                list.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log($"[MenuSceneBuilder] build order set, {list.Count} scenes, splash first.");
        }
    }
}
