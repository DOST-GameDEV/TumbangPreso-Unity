using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Reports which importer actually owns each model, and what an instantiated prefab
    /// contains.
    ///
    /// ⚠️ WRITTEN BECAUSE AN IMPORT SETTING SCRIPT SILENTLY FIXED NOTHING. `ModelImporter` is
    /// Unity's own FBX/OBJ importer; `.glb` is not natively supported at all and is claimed by
    /// glTFast's ScriptedImporter instead. Casting to the wrong importer type yields null, the
    /// loop skips every file, and the log cheerfully reports zero changes as though everything
    /// was already correct.
    /// </summary>
    public static class ImporterProbe
    {
        private const string ResultPath = "Logs/importer-probe.txt";

        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("IMPORTER PROBE");
            sb.AppendLine();

            foreach (var path in new[]
            {
                "Assets/TumbangPreso/Art/characters/persons/character-male-f.glb",
                "Assets/TumbangPreso/Art/models/lata_pasip.obj",
            })
            {
                sb.AppendLine($"-- {path} --");

                var imp = AssetImporter.GetAtPath(path);
                sb.AppendLine($"   importer type : {(imp == null ? "NULL" : imp.GetType().FullName)}");

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                sb.AppendLine($"   prefab        : {(prefab == null ? "NULL" : prefab.name)}");

                if (prefab != null)
                {
                    var anim = prefab.GetComponentInChildren<Animator>(true);
                    var legacy = prefab.GetComponentInChildren<Animation>(true);
                    var skinned = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    var renderers = prefab.GetComponentsInChildren<Renderer>(true);

                    sb.AppendLine($"   Animator      : {(anim == null ? "none" : "yes, controller=" + (anim.runtimeAnimatorController == null ? "null" : anim.runtimeAnimatorController.name) + ", avatar=" + (anim.avatar == null ? "null" : anim.avatar.name))}");
                    sb.AppendLine($"   Animation     : {(legacy == null ? "none" : "yes")}");
                    sb.AppendLine($"   SkinnedMesh   : {skinned.Length}");
                    sb.AppendLine($"   Renderers     : {renderers.Length}");
                    sb.AppendLine($"   bounds        : {(renderers.Length > 0 ? renderers[0].bounds.size.ToString() : "n/a")}");

                    var t = prefab.transform;
                    sb.AppendLine($"   children      : {t.childCount}");
                    for (int i = 0; i < t.childCount && i < 6; i++)
                        sb.AppendLine($"      {t.GetChild(i).name}");
                }

                sb.AppendLine();
            }

            try
            {
                Directory.CreateDirectory("Logs");
                File.WriteAllText(ResultPath, sb.ToString());
            }
            catch { }

            Debug.Log(sb.ToString());
            EditorApplication.Exit(0);
        }
    }
}
