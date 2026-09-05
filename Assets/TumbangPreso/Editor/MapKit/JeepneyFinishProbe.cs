using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Prints what the jeepney's materials actually hold, on the placed prop, after the finish
    /// pass has run.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE `docs/TODO.md` § 144.8 SAYS THE FIRST STEP IS ONE MEASUREMENT AND IT
    /// HAD NOT BEEN TAKEN. 🧑, on the fourth render: **"this js white shit gang idk if u did
    /// shaders properly o rnot"**. The obvious move is to raise `_Metallic` from 0.40 to 0.9, and
    /// it is worthless if the write is not landing at all: `Material.HasProperty` returns false
    /// for a property the shader does not declare and `SetFloat` is then simply skipped, **which
    /// logs nothing**. Four causes need four different fixes and this table tells you which.
    ///
    /// ⚠️ IT MEASURES THE SCENE AND NOT THE BUILDER. `GiveWhitePanelsAMetalFinish` runs during a
    /// map rebuild and writes `renderer.sharedMaterials` on a prefab instance; whether that
    /// survives the scene save is cause 3 on that list, and only opening the saved scene can say.
    ///
    /// ⚠️ IT WRITES NOTHING. Opening a scene in the editor and reading materials cannot change
    /// either, and `SceneDependencyCheck`'s note is the precedent: it *"never SAVES a scene,
    /// because saving would rewrite the stubs the text check exists to find"*.
    ///
    ///   Unity.exe -batchmode -quit -projectPath . \
    ///             -executeMethod TumbangPreso.EditorTools.JeepneyFinishProbe.Report
    /// </summary>
    public static class JeepneyFinishProbe
    {
        private const string ScenePath = "Assets/TumbangPreso/Scenes/Maps/IlalimNgTulay.unity";
        private const string Out = "Logs/jeepney-finish.txt";

        [MenuItem("Tumbang Preso/Probes/Jeepney finish")]
        public static void Report()
        {
            var sb = new StringBuilder();
            sb.AppendLine("JEEPNEY FINISH, AS PLACED");
            sb.AppendLine();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Finish(sb.AppendLine($"could not open {ScenePath}"));
                return;
            }

            var vehicles = new List<GameObject>();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(includeInactive: true))
                {
                    if (t.name.ToLowerInvariant().Contains("jeep")) vehicles.Add(t.gameObject);
                }
            }

            sb.AppendLine($"  scene            {ScenePath}");
            sb.AppendLine($"  jeepney objects  {vehicles.Count}");

            // ⚠️ CAUSE 2 ON § 144.8'S LIST, ASKED BEFORE ANY NUMBER IS READ. Metal is reflection:
            // a metallic surface with nothing to reflect renders as a flat patch rather than as
            // shine, so the absence of a probe is an answer on its own.
            int probes = 0;
            foreach (var root in scene.GetRootGameObjects())
                probes += root.GetComponentsInChildren<ReflectionProbe>(true).Length;

            sb.AppendLine($"  reflection probes in the scene: {probes}" +
                          (probes == 0
                              ? "   ⚠️ NONE. § 144.8 cause 2: metal with nothing to reflect is a "
                                + "flat patch, whatever _Metallic says."
                              : ""));
            sb.AppendLine();

            sb.AppendLine($"  {"material",-34} {"shader",-40} {"has_M",6} {"metal",6} " +
                          $"{"has_S",6} {"smooth",7} {"map",4}");
            sb.AppendLine("  " + new string('-', 108));

            int rows = 0;
            var seen = new HashSet<Material>();

            foreach (var go in vehicles)
            {
                foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (var m in renderer.sharedMaterials)
                    {
                        if (m == null || !seen.Add(m)) continue;

                        bool hasM = m.HasProperty("_Metallic");
                        bool hasS = m.HasProperty("_Smoothness");

                        sb.AppendLine(
                            $"  {Trim(m.name, 34),-34} {Trim(m.shader != null ? m.shader.name : "(none)", 40),-40} " +
                            $"{hasM,6} {(hasM ? m.GetFloat("_Metallic").ToString("0.00") : "-"),6} " +
                            $"{hasS,6} {(hasS ? m.GetFloat("_Smoothness").ToString("0.00") : "-"),7} " +
                            $"{(m.IsKeywordEnabled("_METALLICSPECGLOSSMAP") ? "yes" : "no"),4}");
                        rows++;
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine($"  {rows} distinct material(s) on the placed prop.");

            // ⚠️⚠️ THE SECOND HALF OF THE MEASUREMENT, ADDED AFTER THE FIRST RUN ANSWERED
            // § 144.8 CAUSE 1. Every material came back `has_M False, has_S False` on
            // `glTF/PbrMetallicRoughness`, which says the two writes are being skipped and says
            // NOTHING about what to write instead. Naming the replacement properties from memory
            // is exactly the guess this probe exists to replace, and glTFast's property names
            // have changed across versions of that package. So the shader is asked what it
            // actually declares, and the answer goes in the report next to the failure.
            //
            // ⚠️ ROUGHNESS IS NOT SMOOTHNESS AND THE NEXT READER MUST NOT TRANSCRIBE THE NUMBER.
            // glTF authors ROUGHNESS, URP authors SMOOTHNESS, and they are inverses: the chrome's
            // 0.80 smoothness is 0.20 roughness. A straight copy of the existing table would make
            // the shiniest surface the dullest one.
            DumpShaderProperties(sb, seen);
            sb.AppendLine();
            sb.AppendLine("⚠️ READ `has_M` FIRST. False on a material the finish pass targets is");
            sb.AppendLine("   § 144.8 cause 1, the cheapest and most likely: the property names do");
            sb.AppendLine("   not exist on the shader glTFast built, both writes were skipped, and");
            sb.AppendLine("   nothing logged. `_finish` in a name says the pass ran and made a copy.");
            sb.AppendLine();
            sb.AppendLine("⚠️ A NAME WITHOUT `_finish` IS EITHER A SURFACE THE SELECTION REFUSED ON");
            sb.AppendLine("   PURPOSE (the livery, the bench seats, glass, rubber, plastic) OR");
            sb.AppendLine("   § 144.8 cause 3, the scene not keeping the prefab override.");

            Finish(sb);
        }

        private static string Trim(string s, int n) =>
            string.IsNullOrEmpty(s) ? "" : (s.Length <= n ? s : s.Substring(0, n - 1) + "…");

        /// <summary>
        /// Every float and colour property each distinct SHADER on the prop actually declares.
        ///
        /// ⚠️ ONE BLOCK PER SHADER, NOT PER MATERIAL. Seventeen materials shared one shader in the
        /// first run, so a per-material dump would print the same list seventeen times and bury
        /// the answer it exists to give.
        /// </summary>
        private static void DumpShaderProperties(StringBuilder sb, HashSet<Material> materials)
        {
            var shaders = new HashSet<Shader>();
            foreach (var m in materials)
                if (m != null && m.shader != null) shaders.Add(m.shader);

            foreach (var shader in shaders)
            {
                sb.AppendLine();
                sb.AppendLine($"  SHADER PROPERTIES: {shader.name}");

                int count = shader.GetPropertyCount();
                for (int i = 0; i < count; i++)
                {
                    var kind = shader.GetPropertyType(i);
                    if (kind != UnityEngine.Rendering.ShaderPropertyType.Float &&
                        kind != UnityEngine.Rendering.ShaderPropertyType.Range &&
                        kind != UnityEngine.Rendering.ShaderPropertyType.Color)
                        continue;

                    sb.AppendLine($"    {shader.GetPropertyName(i),-34} {kind}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("  ⚠️ THE METAL AND THE ROUGHNESS NAMES IN THAT LIST ARE WHAT THE FINISH");
            sb.AppendLine("     PASS HAS TO WRITE. ⚠️ ROUGHNESS IS THE INVERSE OF SMOOTHNESS:");
            sb.AppendLine("     0.80 smoothness is 0.20 roughness. Do not transcribe the numbers.");
        }

        private static void Finish(StringBuilder sb)
        {
            string text = sb.ToString();
            Debug.Log(text);

            Directory.CreateDirectory("Logs");
            File.WriteAllText(Out, text);
            Debug.Log($"[Jeepney] wrote {Out}");
        }
    }
}
