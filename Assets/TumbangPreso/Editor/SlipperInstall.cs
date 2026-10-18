using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// THE REMODELLED SLIPPERS GO IN (owner, 2026-10-18: "can you put in the new slipper designs too"; the set is
    /// `tools/build_slippers.py`'s, sheet `Logs/slippers/slippers_v6.png`). The new glbs are copied over
    /// `Art/models/tsinelas_ID.glb` (their .meta kept), and Sike's is a new `.glb` where an `.obj` was. A glTF's main
    /// object is named after its scene, so the roster's link to the model is not trusted to survive the swap: this points
    /// every `Resources/Roster/slipper_ID.asset` at its model again, by path, the same table `RosterBookBuilder` reads.
    ///
    /// Run from the open editor without a menu: write `Temp/slippers.request`; the answer is `Temp/slippers.done`.
    /// </summary>
    [InitializeOnLoad]
    internal static class SlipperInstall
    {
        private const string Request = "Temp/slippers.request", Done = "Temp/slippers.done";
        private static readonly string[] Ids = { "tsinelas", "crocs", "pantulog", "sike", "spartan", "alpombra", "pambahay", "heels", "sandals", "loafers" };

        static SlipperInstall() { EditorApplication.update += Poll; }

        private static void Poll()
        {
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            File.Delete(Request);
            var said = new StringBuilder();
            bool ok = true;
            try
            {
                AssetDatabase.Refresh();
                foreach (string id in Ids)
                {
                    string modelPath = $"Assets/TumbangPreso/Art/models/tsinelas_{id}.glb", entryPath = $"Assets/TumbangPreso/Resources/Roster/slipper_{id}.asset";
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                    var entry = AssetDatabase.LoadAssetAtPath<RosterEntryAsset>(entryPath);
                    if (model == null || entry == null) { ok = false; said.AppendLine($"FAIL {id}: model {(model != null)}, roster entry {(entry != null)}"); continue; }
                    bool changed = entry.Model != model;
                    entry.Model = model;
                    EditorUtility.SetDirty(entry);
                    var bounds = new Bounds();
                    bool any = false;
                    foreach (var f in model.GetComponentsInChildren<MeshFilter>(true))
                        if (f.sharedMesh != null) { var b = f.sharedMesh.bounds; if (!any) { bounds = b; any = true; } else bounds.Encapsulate(b); }
                    said.AppendLine($"{id}: {(changed ? "relinked" : "already linked")} to {modelPath}, mesh size {bounds.size.x:0.000} x {bounds.size.y:0.000} x {bounds.size.z:0.000} m");
                }
                AssetDatabase.SaveAssets();
            }
            catch (Exception e) { ok = false; said.AppendLine("FAIL " + e); }
            File.WriteAllText(Done, (ok ? "OK" : "FAIL") + " slippers\n" + said);
            Debug.Log("[Slippers] " + (ok ? "installed" : "FAILED") + "\n" + said);
        }
    }
}
