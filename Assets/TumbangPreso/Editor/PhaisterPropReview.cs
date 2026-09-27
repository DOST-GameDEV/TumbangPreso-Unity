using System;
using System.IO;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// ⚠️ PHAISTER'S PROPS, RENDERED IN THE GAME'S OWN LOOK (HERO-10, CLAUDE.md 6.1: show, do not describe; never an external
    /// renderer). One row per prop, three angles each (top, three-quarter, side), each scaled to fill its cell, plus the manika
    /// in two victims' colours. Wings are posed half open so the hinge reads.
    ///
    ///     python tools/run_unity_guarded.py -batchmode -projectPath . -executeMethod TumbangPreso.EditorTools.PhaisterPropReview.Run -out Logs/phaister-props-vN/props.png -logFile Logs/phaister-props-vN.log
    /// </summary>
    public static class PhaisterPropReview
    {
        private static readonly (string Name, float Fill, float Wings)[] Props =
        {
            ("butterfly", 0.9f, 25f), ("moth", 0.9f, 15f), ("beetle", 0.9f, 0f), ("manika", 0.8f, 0f), ("hatpin", 0.8f, 0f),
        };

        private static readonly (string Label, float Pitch, float Yaw)[] Angles =
        {
            ("top", 80f, 180f), ("three-quarter", 30f, 220f), ("side", 5f, 270f),
        };

        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-out");
            if (at < 0 || at + 1 >= args.Length) throw new ArgumentException("PhaisterPropReview needs -out <versioned png>.");
            string output = Path.GetFullPath(args[at + 1]);
            if (File.Exists(output)) throw new IOException("Use a new review filename: " + output);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var key = new GameObject("Key").AddComponent<Light>();
            key.type = LightType.Directional; key.intensity = 0.85f; key.color = new Color(1f, 0.97f, 0.9f);
            key.transform.rotation = Quaternion.Euler(38f, -40f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.58f, 0.52f) * 0.78f;
            RenderSettings.fog = false;

            int rows = Props.Length + 1, cols = Angles.Length;
            for (int r = 0; r < Props.Length; r++)
                for (int c = 0; c < cols; c++)
                    Place(Props[r].Name, Props[r].Fill, Props[r].Wings, Angles[c].Pitch, Angles[c].Yaw, c, r, null);
            // The manika wearing two victims' colours (Sean's red, Cheska's ice), front on.
            Place("manika", 0.8f, 0f, 5f, 180f, 0, Props.Length, PhaisterProp.ClothedIn(new Color(0.78f, 0.18f, 0.14f)));
            Place("manika", 0.8f, 0f, 5f, 180f, 1, Props.Length, PhaisterProp.ClothedIn(new Color(0.80f, 0.90f, 0.95f)));
            Place("manika", 0.8f, 0f, 5f, 180f, 2, Props.Length, null);

            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.orthographic = true; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.16f, 0.17f, 0.22f);
            camera.transform.position = new Vector3((cols - 1) * 0.5f, -(rows - 1) * 0.5f, -20f);
            camera.orthographicSize = rows * 0.5f; camera.nearClipPlane = 0.1f; camera.farClipPlane = 60f;
            camera.aspect = (float)cols / rows;

            int width = 360 * cols, height = 360 * rows;
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            var shot = new Texture2D(width, height, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, width, height), 0, 0); shot.Apply();
            RenderTexture.active = null; camera.targetTexture = null;
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllBytes(output, shot.EncodeToPNG());
            Object.DestroyImmediate(shot); rt.Release(); Object.DestroyImmediate(rt);
            EditorSceneManager.CloseScene(scene, true);
            EditorApplication.Exit(File.Exists(output) ? 0 : 1);
        }

        private static void Place(string name, float fill, float wings, float pitch, float yaw, int col, int row, Color[] palette)
        {
            var holder = new GameObject(name + "-" + col + "-" + row);
            var go = PhaisterProp.Spawn(name, holder.transform, palette, PhaisterProp.InsectOutlineWidth);
            if (go == null) throw new InvalidOperationException("missing prop " + name);
            foreach (var side in new[] { "l", "r" })
            {
                var w = PhaisterProp.Find(go, "wing-" + side);
                if (w != null) w.localRotation = Quaternion.AngleAxis(side == "l" ? wings : -wings, Vector3.forward) * w.localRotation;
            }
            var bounds = new Bounds(holder.transform.position, Vector3.zero);
            foreach (var rr in go.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(rr.bounds);
            float size = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            float scale = fill / Mathf.Max(0.01f, size);
            go.transform.localPosition = -bounds.center;
            holder.transform.localScale = Vector3.one * scale;
            holder.transform.rotation = Quaternion.Euler(pitch, 0f, 0f) * Quaternion.Euler(0f, yaw, 0f);
            holder.transform.position = new Vector3(col, -row, 0f);
        }
    }
}
