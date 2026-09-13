using System;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    public static class CourtPaintRepair
    {
        public static void RepairAndBuild()
        {
            const string path="Assets/TumbangPreso/Scenes/Maps/BayanPlaza.unity";
            EditorSceneManager.OpenScene(path);
            var root=GameObject.Find("BayanPlaza");
            int count=CivicTownAuthor.SeatCourtPaint(root.transform);
            if(count!=8)throw new InvalidOperationException("Expected eight retained plaza court marks, got "+count);
            // A second pass must not progressively shrink/lift the authored marks.
            var first=Snapshot(root.transform);CivicTownAuthor.SeatCourtPaint(root.transform);
            if(first!=Snapshot(root.transform))throw new InvalidOperationException("Court seating is not stable on repeat");
            EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.SaveScene(root.scene);
            Directory.CreateDirectory("Logs");File.WriteAllText("Logs/bayan-court-paint-repair.txt",first);
            GameBuilder.BuildWindows();
        }

        private static string Snapshot(Transform root)
        {
            var lines=new System.Collections.Generic.List<string>();
            foreach(var mark in root.GetComponentsInChildren<Transform>(true))
                if(mark.name.StartsWith("Confinement",StringComparison.Ordinal)||mark.name.StartsWith("Boundary",StringComparison.Ordinal))
                    lines.Add(mark.name+" position="+mark.position.ToString("F5")+" scale="+mark.localScale.ToString("F5"));
            lines.Sort(StringComparer.Ordinal);return string.Join("\n",lines);
        }
    }
}
