using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// MOUNTAIN FORM'S REVIEW SHEET: Dante in his armour, one row per state, one column per angle, on a court floor so contact and
    /// floating read against a line. A state is "clip@seconds@armour": the clip (his own or a `mountain-` one) sampled at that
    /// second, and the armour's own clock (`DanteMountainArmour.StepTo`; "f" is formed, a number is the clock, "shed:x" is x
    /// seconds into the form, the shedding from 20). One launch draws every pose asked for, so a refinement pass is one render.
    ///
    ///   -executeMethod TumbangPreso.EditorTools.MountainFormSheet.Run -out Logs/dante-armour/sheet_v17.png
    ///   -rows "mountain-idle@0@f;mountain-walk@.3@f;stun@.2@f" -views front,quarter,side,back,top -cell 420 [-focus head]
    /// </summary>
    internal static class MountainFormSheet
    {
        private static readonly Dictionary<string, (float yaw, float pitch)> Views = new Dictionary<string, (float, float)>
        {
            { "front", (0f, 6f) }, { "quarter", (40f, 10f) }, { "side", (-90f, 4f) }, { "back", (180f, 8f) },
            { "backquarter", (215f, 12f) }, { "top", (25f, 52f) }, { "low", (20f, -8f) },
        };

        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs();
            string Arg(string name) { int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
            // -batch <file>: one sheet a line, "out | rows | views | cell | focus", all in this one launch.
            string batch = Arg("-batch");
            if (!string.IsNullOrEmpty(batch))
            {
                bool all = true;
                foreach (var line in File.ReadAllLines(batch))
                {
                    if (line.Trim().Length == 0 || line.TrimStart().StartsWith("#")) continue;
                    var f = line.Split('|').Select(s => s.Trim()).ToArray();
                    string path = Path.GetFullPath(f[0]);
                    if (File.Exists(path)) { Debug.LogError("[MountainSheet] exists, skipped: " + path); all = false; continue; }
                    all &= Shoot(f[1].Split(';').Where(r => r.Trim().Length > 0).ToArray(),
                                 (f.Length > 2 && f[2].Length > 0 ? f[2] : "front,quarter,side,back,top").Split(','),
                                 f.Length > 3 && int.TryParse(f[3], out int size) ? size : 420, f.Length > 4 ? f[4] : null, path);
                }
                EditorApplication.Exit(all ? 0 : 1);
                return;
            }
            string output = Arg("-out");
            if (string.IsNullOrEmpty(output)) throw new ArgumentException("-out is required (a new, versioned filename).");
            output = Path.GetFullPath(output);
            if (File.Exists(output)) throw new IOException("Use a new review filename: " + output);
            var rows = (Arg("-rows") ?? "mountain-idle@0@f").Split(';').Where(r => r.Trim().Length > 0).ToArray();
            var views = (Arg("-views") ?? "front,quarter,side,back,top").Split(',');
            int cell = int.TryParse(Arg("-cell"), out int c) ? c : 420;
            string focus = Arg("-focus");
            bool ok = Shoot(rows, views, cell, focus, output);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        private static bool Shoot(string[] rows, string[] views, int cell, string focus, string output)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var entry = RosterBook.Load()?.FindPersonArt("dante");
            if (entry == null || entry.Model == null) { Debug.LogError("[MountainSheet] no dante art"); return false; }
            var clips = (entry.Clips ?? Array.Empty<AnimationClip>()).Where(k => k != null).ToList();

            var key = new GameObject("Key").AddComponent<Light>();
            key.type = LightType.Directional; key.intensity = .95f; key.color = new Color(1f, .96f, .88f);
            key.shadows = LightShadows.Soft;
            key.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.6f, .56f, .5f) * .8f;
            RenderSettings.fog = false;

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.transform.localScale = Vector3.one * 3f;
            MaterialKit.Dress(floor.GetComponent<Renderer>(), new Color(.55f, .47f, .38f));
            ToonSkin.Apply(floor.GetComponent<Renderer>(), 0f);
            // A chalk line across the court under him, so a stone below the floor shows as cut by it.
            var line = GameObject.CreatePrimitive(PrimitiveType.Quad);
            line.transform.rotation = Quaternion.Euler(90f, 0f, 0f); line.transform.position = new Vector3(0f, .002f, 0f);
            line.transform.localScale = new Vector3(6f, .03f, 1f);
            MaterialKit.Dress(line.GetComponent<Renderer>(), new Color(.92f, .88f, .8f));
            UnityEngine.Object.DestroyImmediate(line.GetComponent<Collider>());

            var camera = new GameObject("Sheet Camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.2f, .19f, .2f);
            camera.fieldOfView = focus == "head" ? 18f : 30f; camera.nearClipPlane = .05f; camera.farClipPlane = 60f;
            camera.gameObject.AddComponent<ColourGrade>().Set(1.0f, 1.03f, 1.18f, 0.92f, 1.9f);

            // ⚠️ ONE TEXTURE A CELL, read into the sheet: `ColourGrade` blits the whole target, so a camera rect per cell on one
            // texture leaves only the last cell, drawn over the whole sheet.
            int width = views.Length * cell, height = rows.Length * cell;
            var rt = new RenderTexture(cell, cell, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            camera.targetTexture = rt;
            var shot = new Texture2D(width, height, TextureFormat.RGB24, false);
            var report = new System.Text.StringBuilder();

            for (int r = 0; r < rows.Length; r++)
            {
                var parts = rows[r].Trim().Split('@');
                string clipName = parts[0];
                float at = parts.Length > 1 ? float.Parse(parts[1], CultureInfo.InvariantCulture) : 0f;
                string state = parts.Length > 2 ? parts[2] : "f";

                var root = new GameObject("Dante root");
                var model = UnityEngine.Object.Instantiate(entry.Model, root.transform);
                model.transform.localRotation = Quaternion.Euler(0f, CharacterVisual.PersonModelYaw, 0f);
                ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, entry.Palette);
                var clip = clips.FirstOrDefault(k => k.name == clipName);
                if (clip == null) report.AppendLine($"row {r}: no clip '{clipName}'");
                else clip.SampleAnimation(model, Mathf.Min(at, clip.length));

                float clock = DanteMountainArmour.FormedAt + 4f;
                if (state.StartsWith("shed:")) clock = DanteMountainArmour.FormedAt + float.Parse(state.Substring(5), CultureInfo.InvariantCulture);
                else if (state != "f" && state != "none") clock = float.Parse(state, CultureInfo.InvariantCulture);
                DanteMountainArmour armour = null;
                if (state != "none")
                {
                    armour = DanteMountainArmour.Build(model, root.transform);
                    if (armour != null)
                    {
                        // A shed row walks the clock up to it, so every stone lets go from where it was.
                        if (clock > armour.EndAt - DanteMountainArmour.ShedSeconds)
                            for (float s = armour.EndAt - DanteMountainArmour.ShedSeconds - .1f; s < clock; s += 1f / 30f) armour.StepTo(s);
                        armour.StepTo(clock);
                    }
                }

                var renderers = model.GetComponentsInChildren<Renderer>().Where(k => k.enabled).ToArray();
                var bounds = renderers[0].bounds; foreach (var k in renderers) bounds.Encapsulate(k.bounds);
                float lowest = bounds.min.y;
                Vector3 target = bounds.center; float extent = Mathf.Max(bounds.extents.y, Mathf.Max(bounds.extents.x, bounds.extents.z)) * 1.12f;
                if (focus == "head")
                {
                    var head = model.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "head");
                    if (head != null) { target = head.position + Vector3.down * .05f; extent = .32f; }
                }
                report.AppendLine($"row {r} {rows[r]}: pieces {(armour != null ? armour.PieceCount : 0)}, lowest {lowest:F3} m, top {bounds.max.y:F3} m");

                for (int v = 0; v < views.Length; v++)
                {
                    var (yaw, pitch) = Views.TryGetValue(views[v].Trim(), out var w) ? w : (0f, 6f);
                    var dir = Quaternion.Euler(-pitch, yaw, 0f) * Vector3.forward;
                    float distance = extent / Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad);
                    camera.transform.position = target + dir * distance;
                    camera.transform.LookAt(target);
                    camera.Render();
                    RenderTexture.active = rt;
                    shot.ReadPixels(new Rect(0, 0, cell, cell), v * cell, height - (r + 1) * cell);
                    RenderTexture.active = null;
                }
                UnityEngine.Object.DestroyImmediate(root);
                foreach (var debris in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                    if (debris != null && debris.name == "DanteMountainDebris") UnityEngine.Object.DestroyImmediate(debris.gameObject);
            }

            shot.Apply();
            camera.targetTexture = null;
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllBytes(output, shot.EncodeToPNG());
            File.WriteAllText(Path.ChangeExtension(output, ".txt"), report.ToString());
            UnityEngine.Object.DestroyImmediate(shot); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
            EditorSceneManager.CloseScene(scene, true);
            Debug.Log("[MountainSheet] wrote " + output + "\n" + report);
            return true;
        }
    }
}
