using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    // The Home screen reuses the existing picker rather than maintaining another inventory.
    public static class HomeAssetsAuthor
    {
        [MenuItem("Tumbang Preso/Authoring/Refresh home character picker")]
        public static void SavePickerFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Ui/MatchSetup.unity");
            var source = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).First(t => t.name == "CharacterSelectPanel");
            var clone = UnityEngine.Object.Instantiate(source.gameObject);
            clone.name = "CharacterPicker";
            clone.SetActive(false);
            Directory.CreateDirectory("Assets/TumbangPreso/Resources/UI/home");
            PrefabUtility.SaveAsPrefabAsset(clone, "Assets/TumbangPreso/Resources/UI/home/CharacterPicker.prefab");
            UnityEngine.Object.DestroyImmediate(clone);
        }

    }
}
