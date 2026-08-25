using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Refuse to ship a scene holding a component the PLAYER cannot bind to a script.
    ///
    /// ⚠️⚠️ THIS EXISTS BECAUSE IT SHIPPED A BUILD THAT HARD CRASHED ON A MAP SELECT. On
    /// 2026-08-25 the released Windows player died the instant Ilalim ng Tulay was picked, with
    /// "The file 'level8' is corrupted! Remove it and launch unity again!" and
    /// "[Position out of bounds!]" in the log. Nothing was corrupt. Every serialized file
    /// parsed clean: headers self-consistent, all 12,045 objects inside the data section, every
    /// streaming record inside its .resS.
    ///
    /// The scene held eight `HazardVolume` components whose `m_Script` pointed at an INLINE
    /// `!u!115 MonoScript` stub written into the scene file rather than at a script asset.
    /// Unity writes that stub when it cannot resolve a `MonoScript` for a type, which happens
    /// whenever the class name does not match its file name: `HazardVolume` was declared inside
    /// `HazardMap.cs`. The player then has a component with no layout to deserialize against,
    /// runs off the end of the object, and takes the process down.
    ///
    /// ⚠️⚠️ AND THE WHOLE EXISTING GATE PASSED. Core, EditMode, PlayMode, HeadlessCheck,
    /// ArenaCheck, AudioCueCheck and MapGeometryCheck were all green on the commit that shipped
    /// it, because THE EDITOR RESOLVES THE STUB BY CLASS NAME AND THE PLAYER CANNOT. Every
    /// in-editor check is blind to this by construction. That is also why this check reads the
    /// scene AS TEXT and never opens it: opening the scene is what hides the defect, since
    /// `EditorSceneManager` binds the type by name and a subsequent save would quietly rewrite
    /// the stub away without anyone learning it had been there.
    ///
    /// ⚠️ IT ALSO CATCHES A DANGLING GUID, which is the same failure by the other route: a
    /// component pointing at a script asset that no longer exists. Both end as a missing script
    /// in the player.
    ///
    /// ⚠️ BUILD SCENES GATE, EVERYTHING ELSE REPORTS. A scene not in `EditorBuildSettings`
    /// cannot be loaded by the player, so a stub in one is a latent bug rather than a crash.
    /// It is still printed, because `CharacterSelect.unity` and `VerticalSlice.unity` both held
    /// stale ones and neither had ever been mentioned anywhere.
    ///
    /// Run:
    ///   Unity.exe -batchmode -quit -nographics -projectPath . \
    ///             -executeMethod TumbangPreso.EditorTools.SceneScriptCheck.Run -logFile -
    /// </summary>
    public static class SceneScriptCheck
    {
        private const string ResultPath = "Logs/scene-script-check.txt";

        /// <summary>An inline MonoScript document: a script asset faked inside the scene file.</summary>
        private static readonly Regex StubDoc = new Regex(
            @"^--- !u!115 &(\d+)", RegexOptions.Multiline);

        /// <summary>`m_Script: {fileID: N}` with no guid: a reference to something local.</summary>
        private static readonly Regex LocalScriptRef = new Regex(
            @"^\s*m_Script: \{fileID: (-?\d+)\}\s*$", RegexOptions.Multiline);

        /// <summary>`m_Script: {fileID: 11500000, guid: ..., type: 3}`: the correct form.</summary>
        private static readonly Regex GuidScriptRef = new Regex(
            @"^\s*m_Script: \{fileID: -?\d+, guid: ([0-9a-f]{32}), type: \d+\}",
            RegexOptions.Multiline);

        private static readonly Regex ClassName = new Regex(@"^\s*m_ClassName: (\S+)", RegexOptions.Multiline);

        [MenuItem("Tumbang Preso/Check Scene Scripts")]
        public static void RunFromMenu() => Execute(true);

        public static void Run() => EditorApplication.Exit(Execute(true) ? 0 : 1);

        public static void RunReportOnly() => Execute(false);

        public static bool Execute(bool gate)
        {
            var sb = new StringBuilder();
            sb.AppendLine("SCENE SCRIPT CHECK");
            sb.AppendLine("  a component the player cannot bind to a script is a crash, not a warning");
            sb.AppendLine();

            var build = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in EditorBuildSettings.scenes)
                if (s.enabled)
                    build.Add(s.path.Replace('\\', '/'));

            int gatedFindings = 0;
            int otherFindings = 0;

            sb.AppendLine($"SHIPS ({build.Count} scenes in the build, these gate)");
            foreach (string path in Sorted(build))
                gatedFindings += Inspect(path, sb);

            sb.AppendLine();
            sb.AppendLine("DOES NOT SHIP (reported only)");
            var rest = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Scene t:Prefab"))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
                if (!p.StartsWith("Assets/", StringComparison.Ordinal)) continue;
                if (build.Contains(p)) continue;
                rest.Add(p);
            }
            rest.Sort(StringComparer.Ordinal);
            foreach (string path in rest)
                otherFindings += Inspect(path, sb, quietWhenClean: true);
            if (otherFindings == 0)
                sb.AppendLine($"  {rest.Count} scenes and prefabs, all clean");

            sb.AppendLine();
            sb.AppendLine(gatedFindings == 0
                ? $"OK. No unbindable script reference in any build scene."
                : $"FAILED. {gatedFindings} unbindable script reference(s) in build scenes.");
            if (otherFindings > 0)
                sb.AppendLine($"  plus {otherFindings} in scenes that do not ship, listed above.");

            string report = sb.ToString();
            Debug.Log(report);
            try
            {
                Directory.CreateDirectory("Logs");
                File.WriteAllText(ResultPath, report);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SceneScriptCheck] could not write {ResultPath}: {e.Message}");
            }

            return !gate || gatedFindings == 0;
        }

        private static IEnumerable<string> Sorted(HashSet<string> set)
        {
            var l = new List<string>(set);
            l.Sort(StringComparer.Ordinal);
            return l;
        }

        /// <summary>
        /// ⚠️ TEXT, NOT `EditorSceneManager.OpenScene`. See the class note: opening the scene is
        /// exactly what makes this bug invisible.
        /// </summary>
        private static int Inspect(string path, StringBuilder sb, bool quietWhenClean = false)
        {
            if (!File.Exists(path))
            {
                sb.AppendLine($"  {path}: MISSING FROM DISK");
                return 1;
            }

            string text;
            try
            {
                text = File.ReadAllText(path);
            }
            catch (Exception e)
            {
                sb.AppendLine($"  {path}: could not be read: {e.Message}");
                return 1;
            }

            // A YAML scene starts "%YAML"; a binary or force-text-off asset cannot be scanned.
            if (!text.StartsWith("%YAML", StringComparison.Ordinal))
            {
                if (!quietWhenClean)
                    sb.AppendLine($"  {path}: not text-serialized, cannot be scanned");
                return 0;
            }

            var findings = new List<string>();

            var stubIds = new List<string>();
            foreach (Match m in StubDoc.Matches(text))
                stubIds.Add(m.Groups[1].Value);

            if (stubIds.Count > 0)
            {
                foreach (Match m in ClassName.Matches(text))
                    findings.Add($"inline MonoScript stub for class '{m.Groups[1].Value}' " +
                                 "(class name does not match its file name, or the script asset was missing when this was saved)");
                if (findings.Count == 0)
                    findings.Add($"{stubIds.Count} inline MonoScript stub(s) with no class name");
            }

            foreach (Match m in LocalScriptRef.Matches(text))
                findings.Add($"m_Script points at local fileID {m.Groups[1].Value} with no guid; " +
                             "the player has no script asset to bind");

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match m in GuidScriptRef.Matches(text))
            {
                string guid = m.Groups[1].Value;
                if (!seen.Add(guid)) continue;
                string asset = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(asset) || !File.Exists(asset))
                    findings.Add($"m_Script guid {guid} resolves to nothing");
            }

            if (findings.Count == 0)
            {
                if (!quietWhenClean) sb.AppendLine($"  {path}: clean");
                return 0;
            }

            sb.AppendLine($"  {path}: {findings.Count} FINDING(S)");
            foreach (string f in findings)
                sb.AppendLine($"      {f}");
            return findings.Count;
        }
    }
}
