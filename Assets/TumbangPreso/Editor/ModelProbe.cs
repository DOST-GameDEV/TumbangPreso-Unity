using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Reports what is actually inside the imported models: clips, rigs, and mesh bounds.
    ///
    /// ⚠️ WRITTEN BECAUSE GUESSING CLIP NAMES IS HOW AN ANIMATION LAYER SILENTLY DOES NOTHING.
    /// A wrong clip name does not error; the character simply stands still, which reads as "the
    /// animation is not wired yet" forever. Ask the asset instead.
    /// </summary>
    public static class ModelProbe
    {
        private const string ResultPath = "Logs/model-probe.txt";

        [MenuItem("Tumbang Preso/Probe Models")]
        public static void RunFromMenu() => Execute();

        public static void Run()
        {
            Execute();
            EditorApplication.Exit(0);
        }

        private static void Execute()
        {
            var sb = new StringBuilder();
            sb.AppendLine("MODEL PROBE");
            sb.AppendLine();

            Probe(sb, "Assets/TumbangPreso/Art/characters/persons/character-male-f.glb");
            Probe(sb, "Assets/TumbangPreso/Art/models/lata_pasip.obj");
            Probe(sb, "Assets/TumbangPreso/Art/models/tsinelas_classic.obj");

            try
            {
                Directory.CreateDirectory("Logs");
                File.WriteAllText(ResultPath, sb.ToString());
            }
            catch { }

            Debug.Log(sb.ToString());
        }

        private static void Probe(StringBuilder sb, string path)
        {
            sb.AppendLine($"-- {path} --");

            var all = AssetDatabase.LoadAllAssetsAtPath(path);
            if (all == null || all.Length == 0)
            {
                sb.AppendLine("   NOT FOUND or not imported");
                sb.AppendLine();
                return;
            }

            var clips = new List<string>();
            var meshes = new List<string>();
            bool hasAvatar = false;

            foreach (var o in all)
            {
                switch (o)
                {
                    case AnimationClip c when !c.name.StartsWith("__preview"):
                        clips.Add($"{c.name}  ({c.length:F2}s, legacy={c.legacy})");
                        break;
                    case Mesh m:
                        meshes.Add($"{m.name} verts={m.vertexCount} bounds={m.bounds.size}");
                        break;
                    case Avatar _:
                        hasAvatar = true;
                        break;
                }
            }

            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null)
            {
                sb.AppendLine($"   animationType : {importer.animationType}");
                sb.AppendLine($"   importAnim    : {importer.importAnimation}");
                sb.AppendLine($"   scaleFactor   : {importer.globalScale}");
            }

            sb.AppendLine($"   avatar        : {(hasAvatar ? "yes" : "no")}");
            sb.AppendLine($"   clips ({clips.Count}):");
            foreach (var c in clips) sb.AppendLine($"      {c}");

            sb.AppendLine($"   meshes ({meshes.Count}):");
            foreach (var m in meshes) sb.AppendLine($"      {m}");

            sb.AppendLine();
        }
    }
}
