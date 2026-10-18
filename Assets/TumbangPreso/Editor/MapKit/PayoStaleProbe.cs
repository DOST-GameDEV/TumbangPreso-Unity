using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.Editor.MapKit
{
    /// <summary>
    /// Counts hidden leftover objects in the open editor (owner, 2026-10-18: "there are paete vine swing crosshairs
    /// everywhere"). `Visual.PaeteCatchMarker` installs a HideAndDontSave object each Play, and DontSave objects outlive
    /// Play in the editor, so earlier copies may stack. Request `Temp/stale.request` (any text), answer `Temp/stale.done`:
    /// every PaeteCatchMarker / its "Leaves" child, alive or orphaned, with its flags, active state and position.
    /// Read only, except a request reading `clean` (edit mode only), which destroys the leftover markers first.
    /// </summary>
    [InitializeOnLoad]
    internal static class PayoStaleProbe
    {
        private const string Request = "Temp/stale.request", Done = "Temp/stale.done";

        static PayoStaleProbe() => EditorApplication.update += Poll;

        private static void Poll()
        {
            if (!File.Exists(Request)) return;
            string ask = File.ReadAllText(Request).Trim();
            bool clean = ask == "clean";
            File.Delete(Request);
            var text = new StringBuilder();
            if (ask == "leaks")
            {
                // Owner, 2026-10-19, "random ability vfx are stuck floating in the map": every live renderer, grouped by the
                // name of its top object, with whether that object is outside any scene (DontSave / DontDestroyOnLoad) and
                // whether it is in the loaded Payo scene. Read only.
                var groups = new System.Collections.Generic.Dictionary<string, (int count, int shown, string flags, string scene, Vector3 at)>();
                foreach (var r in Resources.FindObjectsOfTypeAll<Renderer>())
                {
                    if (r == null || EditorUtility.IsPersistent(r)) continue;
                    var top = r.transform.root;
                    if (top.gameObject.scene.name == "Payo" && (top.gameObject.hideFlags & HideFlags.DontSave) == 0) continue;
                    string key = top.name + " | " + r.gameObject.name;
                    groups.TryGetValue(key, out var g);
                    g.count++; if (r.enabled && r.gameObject.activeInHierarchy) g.shown++;
                    g.flags = top.gameObject.hideFlags.ToString(); g.scene = top.gameObject.scene.name; g.at = r.bounds.center;
                    groups[key] = g;
                }
                foreach (var kv in System.Linq.Enumerable.OrderByDescending(groups, k => k.Value.shown))
                    text.AppendLine($"  {kv.Value.shown,4} shown / {kv.Value.count,4}  {kv.Key}  [{kv.Value.flags}] scene '{kv.Value.scene}' near {kv.Value.at.ToString("F0")}");
                File.WriteAllText(Done, "OK leaks (" + (EditorApplication.isPlaying ? "in Play" : "edit mode") + ")\n" + text);
                return;
            }
            if (ask == "buried")
            {
                // Owner, 2026-10-19, "ambient occlusion flashing and bleeding through walls": plaza pieces with something
                // drawn right over them (a floor or a wall they sit inside). Renderer bounds tops checked against every
                // other renderer's bounds is too coarse, so rays: up from just above each piece's base.
                var plaza = GameObject.Find("Plaza");
                int all = 0, under = 0;
                if (plaza != null)
                    foreach (Transform group in plaza.transform)
                    foreach (Transform piece in group)
                    {
                        all++;
                        Vector3 p = piece.position + Vector3.up * 0.05f;
                        foreach (var hit in Physics.RaycastAll(p, Vector3.up, 2.5f, ~0, QueryTriggerInteraction.Ignore))
                        {
                            if (hit.transform.IsChildOf(piece)) continue;
                            under++;
                            if (under <= 40) text.AppendLine($"  {group.name}/{piece.name} at {piece.position.ToString("F2")}: {hit.collider.name} {hit.distance:F2} m above");
                            break;
                        }
                    }
                File.WriteAllText(Done, $"OK buried: {(plaza == null ? "no Plaza object" : under + " of " + all + " plaza pieces have something solid within 2.5 m above")}\n" + text);
                return;
            }
            if (ask == "figures")
            {
                // Owner, 2026-10-19, "floating models out here": every life figure's feet, its size, and the land under it.
                foreach (var actors in Object.FindObjectsByType<Map.PayoActors>(FindObjectsInactive.Include))
                foreach (var a in actors.Actors)
                {
                    if (a.Root == null) continue;
                    var b = new Bounds(a.Root.position, Vector3.zero);
                    foreach (var r in a.Root.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
                    Vector3 p = a.Root.position;
                    string under = Physics.Raycast(p + Vector3.up * 200f, Vector3.down, out var hit, 600f) ? hit.point.y.ToString("F1") + " (" + hit.collider.name + ")" : "none";
                    text.AppendLine($"{a.Name,-16} kind {a.Kind,-13} at {p.ToString("F1")} scale {a.Root.lossyScale.x:F2} height {b.size.y:F1} bottom {b.min.y:F1} | land under {under}");
                }
                File.WriteAllText(Done, "OK figures\n" + text);
                return;
            }
            if (clean && !EditorApplication.isPlaying)
            {
                int gone = 0;
                foreach (var m in Resources.FindObjectsOfTypeAll<Visual.PaeteCatchMarker>())
                    if (m != null) { Object.DestroyImmediate(m.gameObject); gone++; }
                text.AppendLine("cleaned " + gone + " leftover PaeteCatchMarker objects");
            }
            text.AppendLine("OK stale " + System.DateTime.Now.ToString("HH:mm:ss") + (EditorApplication.isPlaying ? " (in Play)" : " (edit mode)"));
            var markers = Resources.FindObjectsOfTypeAll<Visual.PaeteCatchMarker>();
            text.AppendLine("PaeteCatchMarker components: " + markers.Length);
            foreach (var m in markers)
                text.AppendLine("  " + m.gameObject.name + " flags " + m.gameObject.hideFlags + " enabled " + m.enabled + " scene '" + m.gameObject.scene.name + "'");
            int leaves = 0, shown = 0;
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.name != "Leaves" && go.name != "PaeteCatchMarker") continue;
                if (go.name == "Leaves") { leaves++; if (go.activeInHierarchy) shown++; }
                text.AppendLine("  object " + go.name + " flags " + go.hideFlags + " active " + go.activeInHierarchy
                    + " parent " + (go.transform.parent != null ? go.transform.parent.name : "-") + " at " + go.transform.position.ToString("F1")
                    + " scale " + go.transform.localScale.x.ToString("F2"));
            }
            text.AppendLine("Leaves objects: " + leaves + ", active: " + shown);
            File.WriteAllText(Done, text.ToString());
        }
    }
}
