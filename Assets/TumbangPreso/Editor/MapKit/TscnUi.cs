using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// Shared parsing for Godot `.tscn` files, and the coordinate maths the UI conversion
    /// depends on.
    ///
    /// ⚠️⚠️ GODOT'S UI Y AXIS POINTS DOWN AND UNITY'S POINTS UP. That single fact is what makes
    /// a naive conversion produce a layout that is subtly, entirely wrong: every panel appears
    /// mirrored vertically, which looks like somebody rearranged the screen rather than like a
    /// maths error. The anchor flip below is the whole conversion and it is worth reading
    /// twice.
    ///
    /// Godot anchors: `anchor_top` 0 is the TOP of the parent, `anchor_bottom` 1 is the BOTTOM.
    /// Unity anchors: `anchorMin.y` 0 is the BOTTOM, `anchorMax.y` 1 is the TOP.
    /// So the Y anchors swap AND invert, and the offsets negate.
    /// </summary>
    public static class TscnUi
    {
        public static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public sealed class ExtRes
        {
            public string Id;
            public string Path;
            public string Type;
        }

        public sealed class SubRes
        {
            public string Id;
            public string Type;
            public readonly Dictionary<string, string> Props = new Dictionary<string, string>();
        }

        public sealed class NodeDef
        {
            public string Name;
            public string Type;
            public string Parent;
            public string InstanceExtId;
            public readonly Dictionary<string, string> Props = new Dictionary<string, string>();

            public string PathKey =>
                Parent == null ? "." : (Parent == "." ? Name : Parent + "/" + Name);
        }

        public sealed class Scene
        {
            public readonly Dictionary<string, ExtRes> Ext = new Dictionary<string, ExtRes>();
            public readonly Dictionary<string, SubRes> Sub = new Dictionary<string, SubRes>();
            public readonly List<NodeDef> Nodes = new List<NodeDef>();
        }

        private static readonly Regex ExtRe = new Regex(
            @"^\[ext_resource\s+type=""(?<type>[^""]+)""\s+(?:uid=""[^""]*""\s+)?path=""(?<path>[^""]+)""\s+id=""(?<id>[^""]+)""\]");

        private static readonly Regex SubRe = new Regex(
            @"^\[sub_resource\s+type=""(?<type>[^""]+)""\s+id=""(?<id>[^""]+)""\]");

        private static readonly Regex NodeRe = new Regex(
            @"^\[node\s+name=""(?<name>[^""]*)""(?:\s+type=""(?<type>[^""]*)"")?(?:\s+parent=""(?<parent>[^""]*)"")?(?:\s+instance=ExtResource\(""(?<inst>[^""]+)""\))?");

        private static readonly Regex PropRe =
            new Regex(@"^(?<key>[A-Za-z_][A-Za-z0-9_/]*)\s*=\s*(?<val>.+)$");

        public static Scene Parse(string[] lines)
        {
            var scene = new Scene();
            NodeDef node = null;
            SubRes sub = null;

            foreach (var raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;

                var m = ExtRe.Match(line);
                if (m.Success)
                {
                    node = null; sub = null;
                    scene.Ext[m.Groups["id"].Value] = new ExtRes
                    {
                        Id = m.Groups["id"].Value,
                        Path = m.Groups["path"].Value,
                        Type = m.Groups["type"].Value,
                    };
                    continue;
                }

                m = SubRe.Match(line);
                if (m.Success)
                {
                    node = null;
                    sub = new SubRes { Id = m.Groups["id"].Value, Type = m.Groups["type"].Value };
                    scene.Sub[sub.Id] = sub;
                    continue;
                }

                m = NodeRe.Match(line);
                if (m.Success)
                {
                    sub = null;
                    node = new NodeDef
                    {
                        Name = m.Groups["name"].Value,
                        Type = m.Groups["type"].Success ? m.Groups["type"].Value : "",
                        Parent = m.Groups["parent"].Success ? m.Groups["parent"].Value : null,
                        InstanceExtId = m.Groups["inst"].Success ? m.Groups["inst"].Value : null,
                    };
                    scene.Nodes.Add(node);
                    continue;
                }

                if (line.StartsWith("[")) { node = null; sub = null; continue; }

                var p = PropRe.Match(line);
                if (!p.Success) continue;

                string key = p.Groups["key"].Value;
                string val = p.Groups["val"].Value.Trim();

                if (node != null) node.Props[key] = val;
                else if (sub != null) sub.Props[key] = val;
            }

            return scene;
        }

        // -------------------------------------------------------------------

        public static float F(string s, float fallback = 0.0f) =>
            float.TryParse((s ?? "").Trim(), NumberStyles.Float, Inv, out var v) ? v : fallback;

        public static float Prop(NodeDef n, string key, float fallback = 0.0f) =>
            n.Props.TryGetValue(key, out var s) ? F(s, fallback) : fallback;

        public static string Str(NodeDef n, string key)
        {
            if (!n.Props.TryGetValue(key, out var s)) return null;

            s = s.Trim();
            if (s.StartsWith("\"") && s.EndsWith("\"") && s.Length >= 2)
                s = s.Substring(1, s.Length - 2);

            // Godot escapes newlines in scene text.
            return s.Replace("\\n", "\n").Replace("\\\"", "\"");
        }

        public static Color ParseColor(string value, Color fallback)
        {
            if (string.IsNullOrEmpty(value)) return fallback;

            var m = Regex.Match(value, @"Color\(([^)]*)\)");
            if (!m.Success) return fallback;

            var parts = m.Groups[1].Value.Split(',');
            if (parts.Length < 3) return fallback;

            return new Color(F(parts[0]), F(parts[1]), F(parts[2]),
                             parts.Length > 3 ? F(parts[3], 1.0f) : 1.0f);
        }

        public static string ExtId(string value)
        {
            var m = Regex.Match(value ?? "", @"ExtResource\(""([^""]+)""\)");
            return m.Success ? m.Groups[1].Value : null;
        }

        public static string SubId(string value)
        {
            var m = Regex.Match(value ?? "", @"SubResource\(""([^""]+)""\)");
            return m.Success ? m.Groups[1].Value : null;
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// Applies a Godot Control's anchors and offsets to a Unity RectTransform.
        ///
        /// ⚠️⚠️ THE Y AXIS FLIP IS THE ENTIRE CONVERSION AND GETTING IT WRONG IS INVISIBLE AT
        /// FIRST. Godot measures from the TOP down; Unity measures from the BOTTOM up. So the
        /// Y anchors both swap and invert, and both Y offsets negate. A version that skips this
        /// produces a screen where everything is vertically mirrored, which reads as a layout
        /// somebody rearranged rather than as a maths bug.
        ///
        /// ⚠️ AND GODOT'S DEFAULTS ARE NOT UNITY'S. An unspecified anchor is 0 in Godot, which
        /// means "pinned to the top-left", and a node with only offsets is a fixed-size box at
        /// a fixed position. Defaulting the anchors to anything else silently stretches nodes
        /// that were never meant to stretch.
        /// </summary>
        public static void ApplyControlRect(RectTransform rt, NodeDef n)
        {
            float aL = Prop(n, "anchor_left", 0.0f);
            float aT = Prop(n, "anchor_top", 0.0f);
            float aR = Prop(n, "anchor_right", 0.0f);
            float aB = Prop(n, "anchor_bottom", 0.0f);

            float oL = Prop(n, "offset_left", 0.0f);
            float oT = Prop(n, "offset_top", 0.0f);
            float oR = Prop(n, "offset_right", 0.0f);
            float oB = Prop(n, "offset_bottom", 0.0f);

            rt.anchorMin = new Vector2(aL, 1.0f - aB);
            rt.anchorMax = new Vector2(aR, 1.0f - aT);

            rt.offsetMin = new Vector2(oL, -oB);
            rt.offsetMax = new Vector2(oR, -oT);

            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        /// <summary>Godot's horizontal_alignment / vertical_alignment to a Unity TextAnchor.</summary>
        public static TextAnchor Align(NodeDef n)
        {
            int h = (int)Prop(n, "horizontal_alignment", 0.0f); // 0 left, 1 centre, 2 right
            int v = (int)Prop(n, "vertical_alignment", 0.0f);   // 0 top, 1 centre, 2 bottom

            if (v == 1)
                return h == 1 ? TextAnchor.MiddleCenter : (h == 2 ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft);
            if (v == 2)
                return h == 1 ? TextAnchor.LowerCenter : (h == 2 ? TextAnchor.LowerRight : TextAnchor.LowerLeft);

            return h == 1 ? TextAnchor.UpperCenter : (h == 2 ? TextAnchor.UpperRight : TextAnchor.UpperLeft);
        }

        /// <summary>res:// to the Unity asset path the art was copied to.</summary>
        public static string ToAssetPath(string godotPath, string artRoot)
        {
            string rel = (godotPath ?? "").Replace("res://", "");
            if (rel.StartsWith("assets/")) rel = rel.Substring("assets/".Length);
            return $"{artRoot}/{rel}";
        }
    }
}
