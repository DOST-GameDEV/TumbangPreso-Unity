using System.Collections.Generic;
using System.IO;
using TumbangPreso.UI;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Sets the build order from the CONVERTED scenes.
    ///
    /// ⚠️⚠️ THIS USED TO GENERATE MENUS AND THAT WAS THE WRONG CALL. It built tidy screens out
    /// of the palette which looked nothing like the game. The menus now come from
    /// `TscnUiImporter`, converted node-for-node out of the Godot `.tscn` with the real
    /// artwork, so this file only decides what ships and in what order.
    ///
    /// ⚠️ THE SPLASH MUST BE INDEX 0. A built player opens on scene 0 and the boot sting plays
    /// on every launch. In the editor the flow works regardless because scenes load by path, so
    /// a wrong index 0 is a bug you only find after handing somebody the exe.
    /// </summary>
    public static class MenuSceneBuilder
    {
        private const string UiDir = "Assets/TumbangPreso/Scenes/Ui";
        private const string MapDir = "Assets/TumbangPreso/Scenes/Maps";
        private const string OldDir = "Assets/TumbangPreso/Scenes";

        [MenuItem("Tumbang Preso/Set Build Order")]
        public static void BuildAllFromMenu() => Execute();

        public static void BuildAll() => EditorApplication.Exit(Execute() ? 0 : 1);

        private static bool Execute()
        {
            RemoveRebuiltScenes();

            var wanted = new List<string>
            {
                $"{UiDir}/{SceneFlow.Splash}.unity",
                $"{UiDir}/{SceneFlow.MainMenu}.unity",
                $"{UiDir}/{SceneFlow.ModeSelect}.unity",
                $"{UiDir}/{SceneFlow.MatchSetup}.unity",
                $"{UiDir}/{SceneFlow.MultiplayerSetup}.unity",
                $"{UiDir}/{SceneFlow.CharacterSelect}.unity",
                $"{UiDir}/{SceneFlow.MatchResult}.unity",
                $"{MapDir}/{SceneFlow.Eskinita}.unity",
                $"{MapDir}/{SceneFlow.BayanPlaza}.unity",
            };

            var list = new List<EditorBuildSettingsScene>();
            bool ok = true;

            foreach (var path in wanted)
            {
                if (!File.Exists(path))
                {
                    Debug.LogError($"[BuildOrder] MISSING {path}. Run " +
                                   "Tumbang Preso > Import Godot UI and Import Godot Maps first.");
                    ok = false;
                    continue;
                }

                list.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = list.ToArray();

            Debug.Log($"[BuildOrder] {list.Count} scenes, splash first:\n  " +
                      string.Join("\n  ", list.ConvertAll(s => s.path)));

            AssetDatabase.SaveAssets();
            return ok;
        }

        /// <summary>
        /// ⚠️ THE OLD REBUILT SCENES ARE DELETED, NOT LEFT ALONGSIDE. Two scenes named MainMenu
        /// in one project is how a build ships the wrong one, and the wrong one here is the one
        /// that does not look like the game.
        /// </summary>
        private static void RemoveRebuiltScenes()
        {
            foreach (var name in new[]
            {
                "Splash", "MainMenu", "ModeSelect", "MatchSetup",
                "MultiplayerSetup", "CharacterSelect", "MatchResult",
            })
            {
                string path = $"{OldDir}/{name}.unity";
                if (!File.Exists(path)) continue;

                AssetDatabase.DeleteAsset(path);
                Debug.Log($"[BuildOrder] removed the rebuilt {name}.unity");
            }
        }
    }
}
