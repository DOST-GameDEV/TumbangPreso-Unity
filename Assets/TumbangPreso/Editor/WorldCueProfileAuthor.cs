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
            bool changed=false;
            if(!System.IO.File.Exists(Path))
            {AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<WorldCueProfile>(),Path);changed=true;}
            const string look="Assets/TumbangPreso/Resources/WorldLookProfile.asset";
            if(!System.IO.File.Exists(look))
            {AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<WorldLookProfile>(),look);changed=true;}
            if(changed)AssetDatabase.SaveAssets();
        }
        // Explicit authoring command for the measured batchB choice. Never
        // invoked automatically over an owner's later authored profile values.
        public static void ApplyMeasuredWorldShadow()
        {
            Ensure();
            var profile=AssetDatabase.LoadAssetAtPath<WorldLookProfile>("Assets/TumbangPreso/Resources/WorldLookProfile.asset");
            profile.ShadowLevel=.44f;EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
        }
    }
}
