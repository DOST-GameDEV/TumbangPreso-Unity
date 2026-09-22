using UnityEditor;
using UnityEngine;
using TumbangPreso.Visual;

namespace TumbangPreso.EditorTools
{
    // Creates the first authorable defaults through Unity serialization. Existing
    // authored values are never regenerated or overwritten on an import/build.
    [InitializeOnLoad]
    public static class WorldCueProfileAuthor
    {
        private const string Path = "Assets/TumbangPreso/Resources/WorldCueProfile.asset";
        static WorldCueProfileAuthor() => EditorApplication.delayCall += Ensure;
        [MenuItem("Tumbang Preso/Presentation/Ensure world cue profile")]
        public static void Ensure()
        {
            if (System.IO.File.Exists(Path)) return;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<WorldCueProfile>(), Path);
            AssetDatabase.SaveAssets();
        }
    }
}
