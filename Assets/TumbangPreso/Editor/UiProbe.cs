using System.IO;
using System.Text;
using TumbangPreso.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Dumps what every themed control in a converted screen actually resolved to.
    ///
    /// ⚠️ A SCREENSHOT SAYS "THIS IS WRONG" AND NEVER SAYS WHY. A white rectangle where a wood
    /// panel belongs is a null sprite, a wrong variation, an opaque hit area or a layer in the
    /// wrong draw order, and those four look identical in a PNG. This prints the state instead
    /// of inviting another guess.
    /// </summary>
    public static class UiProbe
    {
        [MenuItem("Tumbang Preso/Probe UI State")]
        public static void ProbeFromMenu() => Execute();

        public static void Probe()
        {
            Execute();
            EditorApplication.Exit(0);
        }

        private static void Execute()
        {
            var report = new StringBuilder();

            foreach (var scene in new[]
            {
                "Assets/TumbangPreso/Scenes/Ui/MatchSetup.unity",
                "Assets/TumbangPreso/Scenes/Ui/MainMenu.unity",
            })
            {
                if (!File.Exists(scene)) continue;

                EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
                report.AppendLine($"== {Path.GetFileNameWithoutExtension(scene)} ==");

                foreach (var panel in Object.FindObjectsByType<GodotPanel>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    panel.Apply();
                    report.AppendLine($"PANEL {PathOf(panel.transform)} variation={panel.Variation}");
                    Dump(report, panel.transform);
                }

                foreach (var button in Object.FindObjectsByType<GodotButton>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    button.Apply();
                    button.Refresh();
                    report.AppendLine($"BUTTON {PathOf(button.transform)} variation={button.Variation}");
                    Dump(report, button.transform);
                }

                // ⚠️ THE PREFERRED SIZES, NOT ONLY THE FINAL RECTS. A panel that refuses to
                // shrink to its contents is a child reporting a preferred height nobody
                // authored, and the usual culprit is a wrapping Label measured before it has a
                // width. The rect alone cannot tell those apart.
                foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene()
                                                .GetRootGameObjects())
                {
                    foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
                    {
                        if (!PathOf(rt).Contains("RightColumn")) continue;

                        report.AppendLine(
                            $"   RECT {rt.name} size={rt.rect.width:F0}x{rt.rect.height:F0} " +
                            $"min={LayoutUtility.GetMinHeight(rt):F0} " +
                            $"pref={LayoutUtility.GetPreferredHeight(rt):F0} " +
                            $"flex={LayoutUtility.GetFlexibleHeight(rt):F0}");
                    }
                }

                report.AppendLine();
            }

            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/ui-probe.txt", report.ToString());
            Debug.Log(report.ToString());
        }

        private static void Dump(StringBuilder report, Transform root)
        {
            var own = root.GetComponent<Image>();
            report.AppendLine($"   own: sprite={Name(own)} colour={Colour(own)}");

            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                var img = child.GetComponent<Image>();
                if (img == null) continue;

                report.AppendLine($"   [{i}] {child.name}: sprite={Name(img)} " +
                                  $"colour={Colour(img)} enabled={img.enabled}");
            }
        }

        private static string Name(Image img) =>
            img == null ? "<no image>" : (img.sprite == null ? "NULL" : img.sprite.name);

        private static string Colour(Image img) =>
            img == null ? "-" : ColorUtility.ToHtmlStringRGBA(img.color);

        private static string PathOf(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }

            return path;
        }
    }
}
