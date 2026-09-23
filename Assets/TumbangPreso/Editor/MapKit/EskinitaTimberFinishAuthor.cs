using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>Fit timber finish to four individually reviewed horizontal-course homes.</summary>
    public static class EskinitaTimberFinishAuthor
    {
        private const string Folder = "Assets/TumbangPreso/Art/EskinitaHouseFinishes";
        private const string OriginalTag = "TumpRefineOriginalMaterial";
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/Eskinita.unity");
            var report = new StringBuilder();
            FinishLoadedScene(report);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Logs/eskinita-house-finish");
            File.WriteAllText("Logs/eskinita-house-finish/timber5w.txt", report.ToString());
            Debug.Log(report.ToString()); EditorApplication.Exit(0);
        }

        public static void FinishLoadedScene(StringBuilder report)
        {
            FinishHouse("5_W", report);
            FinishHouse("2_E", report);
            FinishHouse("5_E", report);
            FinishHouse("6_E", report);
            AssetDatabase.SaveAssets();
        }

        private static void FinishHouse(string lot, StringBuilder report)
        {
            var house = GameObject.Find("Eskinita/Dressing/NeighborhoodRework/HouseFinish_Bahay_" + lot);
            if (house == null) throw new InvalidOperationException("The retained timber finish is missing: " + lot);
            string folder = Folder + "/Timber" + lot.Replace("_", "");
            Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            int changed = 0;
            foreach (var renderer in house.GetComponentsInChildren<MeshRenderer>())
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var original = materials[i];
                    string prior = original.GetTag(OriginalTag, false);
                    if (!string.IsNullOrEmpty(prior)) original = AssetDatabase.LoadAssetAtPath<Material>(prior);
                    if (original == null) throw new InvalidOperationException("Lost original timber material.");
                    bool plank = original.name.StartsWith("Timber upper ", StringComparison.Ordinal);
                    bool joint = original.name.StartsWith("Timber course joints", StringComparison.Ordinal);
                    if (!plank && !joint) continue;
                    string source = AssetDatabase.GetAssetPath(original);
                    if (string.IsNullOrEmpty(source)) throw new InvalidOperationException("Timber source must be persistent.");
                    string label = joint ? "CourseJoints" : original.name.Contains("warm") ? "WarmBoards" : "QuietBoards";
                    string path = folder + "/" + label + ".mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null) { material = new Material(original); AssetDatabase.CreateAsset(material, path); }
                    else EditorUtility.CopySerialized(original, material);
                    material.name = "Eskinita" + lot.Replace("_", "") + "_" + label;
                    material.SetOverrideTag(OriginalTag, source);
                    // Real horizontal boards already provide the joints. The existing
                    // single-plank mode removes the second procedural vertical board grid,
                    // retaining filtered lengthwise grain in the measured facade UV basis.
                    material.SetFloat("_DeckSurface", 1);
                    material.SetFloat("_SurfaceStrength", joint ? 0 : .8f);
                    EditorUtility.SetDirty(material); materials[i] = material; changed++;
                }
                renderer.sharedMaterials = materials;
            }
            if (changed != 3) throw new InvalidOperationException("Expected the two board finishes and their course-joint backing; found " + changed);
            report.AppendLine(lot + ":3local material derivatives; original geometry, UVs, board joints, color, windows and collision unchanged.");
            report.AppendLine("Filtered lengthwise grain replaces the second procedural vertical board grid on real horizontal cladding.");
        }
    }
}
