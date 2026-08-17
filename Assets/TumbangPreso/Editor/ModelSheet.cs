using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// One picture of every 3D model in the project, laid out in a grid.
    ///
    /// ⚠️ IT RENDERS THEM WEARING THE GAME'S OWN MATERIAL, not the importer's. `ToonSkin.Apply`
    /// is the same call `CharacterVisual` and `MatchInstaller` make, so a model that looks wrong
    /// HERE looks wrong in play — which is the only reason a contact sheet is worth having. A
    /// sheet rendered on the stock lit shader would be a picture of the import, and every
    /// question anybody actually asks of it ("is the outline too fat on the can", "did that skin
    /// come in white") would be unanswerable.
    ///
    /// ⚠️ EACH CELL IS NORMALISED TO ITS OWN BOUNDS. The set spans a soda can and a municipal
    /// hall; at a shared scale all but about six cells are a dot. Normalising means the sheet
    /// says what each thing IS, and says nothing about how big it is, so do not read relative
    /// size off it.
    ///
    /// ⚠️⚠️ IT MUST RUN WITHOUT `-nographics`. Same rule `ScreenshotTool` and `ToonProbe` carry:
    /// with no rendering device the capture comes back blank and the run still reports success.
    ///
    /// Run:
    ///   Unity.exe -batchmode -quit -projectPath . \
    ///             -executeMethod TumbangPreso.EditorTools.ModelSheet.Run
    /// </summary>
    public static class ModelSheet
    {
        private static string OutPath => _charactersOnly ? "Logs/cast-sheet.png" : "Logs/model-sheet.png";
        private static string IndexPath => _charactersOnly ? "Logs/cast-sheet.txt" : "Logs/model-sheet.txt";

        /// <summary>Pixels per cell. 12 columns at 220 is a 2640-wide sheet, which is readable
        /// at 100% and still opens in anything.</summary>
        private const int CellPixels = 220;

        /// <summary>
        /// What the current run is shooting. Set by the entry point, read by the layout.
        ///
        /// ⚠️ THE CAST SHEET IS A DIFFERENT PICTURE, NOT A CROP OF THE FULL ONE. A character is
        /// read front-on and large — the face, the shirt, the silhouette — where a prop is read
        /// three-quarter so its depth shows. Shooting both the same way makes one of them useless,
        /// which is why these are two layouts rather than one with a filter.
        /// </summary>
        private static bool _charactersOnly;

        private const string PersonFolder = "Assets/TumbangPreso/Art/characters/persons/";

        private static int Columns => _charactersOnly ? 6 : 12;

        private static int CellSize => _charactersOnly ? 420 : CellPixels;

        /// <summary>
        /// Front-on for the cast, three-quarter for everything else.
        ///
        /// ⚠️⚠️ THE CAST YAW IS 180 AND THAT IS NOT A PREFERENCE. These rigs wear their face on
        /// -Z while Unity's forward is +Z, so an identity rotation photographs the backs of all
        /// twelve heads. `CharacterVisual` carries the same constant as `PersonModelYawDeg` for
        /// the same reason, and its own header records that the fault was reported across more
        /// than ten sessions on the Godot side before it was pinned down. It cost a first
        /// attempt at this sheet too: 🧑 *"make sure they face front lmao"*.
        /// </summary>
        private static Quaternion CellRotation => _charactersOnly
            ? Quaternion.Euler(0.0f, 180.0f, 0.0f)
            : Quaternion.Euler(-18.0f, 35.0f, 0.0f);

        private static float CellFill => _charactersOnly ? 0.74f : Fill;

        /// <summary>How much of a cell the model fills, leaving room for the caption.</summary>
        private const float Fill = 0.62f;

        /// <summary>Extra height, in cells, so the bottom row's captions are not cut off.</summary>
        private const float BottomMargin = 0.25f;

        [MenuItem("Tumbang Preso/Shoot Model Sheet")]
        public static void RunFromMenu() => Execute();

        [MenuItem("Tumbang Preso/Shoot Cast Sheet")]
        public static void RunCastFromMenu() { _charactersOnly = true; Execute(); }

        public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

        public static void RunCast()
        {
            _charactersOnly = true;
            EditorApplication.Exit(Execute() ? 0 : 1);
        }

        private static bool Execute()
        {
            var paths = ModelPaths();

            if (paths.Count == 0)
            {
                Debug.LogError("[ModelSheet] found no models under Art.");
                return false;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                                                    NewSceneMode.Single);

            int rows = Mathf.CeilToInt(paths.Count / (float)Columns);

            BuildLight();

            var camera = BuildCamera(rows);
            var index = new System.Text.StringBuilder();

            index.AppendLine($"MODEL SHEET — {paths.Count} models, {Columns} columns");
            index.AppendLine();

            for (int i = 0; i < paths.Count; i++)
            {
                int col = i % Columns;
                int row = i / Columns;

                PlaceCell(paths[i], col, row);
                index.AppendLine($"r{row + 1}c{col + 1}  {paths[i]}");
            }

            // ⚠️ A QUARTER-CELL OF EXTRA HEIGHT, BECAUSE THE CAPTION HANGS BELOW ITS CELL. The
            // last row's labels sit at -0.40 in a cell that is 1.0 tall, so a sheet sized to
            // exactly `rows` clips them along the bottom edge. The pixel height carries the same
            // margin so the aspect still matches the camera and nothing is stretched.
            bool ok = Capture(camera,
                              Columns * CellSize,
                              Mathf.RoundToInt((rows + BottomMargin) * CellSize));

            Directory.CreateDirectory("Logs");
            File.WriteAllText(IndexPath, index.ToString());

            Debug.Log($"[ModelSheet] {paths.Count} models, {Columns}x{rows}, wrote {OutPath}");

            EditorSceneManager.CloseScene(scene, true);
            return ok;
        }

        /// <summary>
        /// Every model the project ships, in a stable order.
        ///
        /// ⚠️ SORTED BY PATH SO THE SHEET IS COMPARABLE BETWEEN RUNS. A sheet whose cells move
        /// when a model is added cannot be diffed against the last one, which is most of what a
        /// contact sheet is for.
        /// </summary>
        private static List<string> ModelPaths()
        {
            string[] wanted = { ".glb", ".obj", ".fbx" };

            string root = _charactersOnly ? PersonFolder : "Assets/TumbangPreso/Art/";

            return AssetDatabase.GetAllAssetPaths()
                .Where(p => p.StartsWith(root))
                .Where(p => wanted.Contains(Path.GetExtension(p).ToLowerInvariant()))
                .OrderBy(p => p, System.StringComparer.Ordinal)
                .ToList();
        }

        private static void BuildLight()
        {
            var go = new GameObject("Key");
            var light = go.AddComponent<Light>();

            light.type = LightType.Directional;
            light.intensity = 0.85f;
            light.color = new Color(1.0f, 0.97f, 0.9f);

            go.transform.rotation = Quaternion.Euler(38.0f, -40.0f, 0.0f);

            // The toon face is two flat bands, so ambient decides how dark the shadow band reads.
            //
            // ⚠️⚠️ SCALED DOWN FROM THE ARENA'S OWN 1.65, AND THAT IS NOT A DISAGREEMENT WITH IT.
            // Eskinita's ambient is (0.62, 0.58, 0.52) x 1.65 = (1.02, 0.96, 0.86), which is over
            // full scale before the key light is added at all — and that is CORRECT in play,
            // because `ColourGrade` runs an ACES curve over the composited frame and rolls it
            // off. This sheet has no such pass: `ColourGrade` is an `OnRenderImage` effect and a
            // manual `camera.Render()` in an editor scene never runs its lifecycle, so the raw
            // value simply clips and every character comes out bleached.
            //
            // ⚠️ COMPENSATED HERE RATHER THAN BY FORCING THE POST PASS. Making the effect run in
            // edit mode means `ExecuteAlways` on a runtime rendering component, which would then
            // also run in every other editor scene that happens to hold a camera. A darker key
            // and ambient are local to this tool and cannot leak.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.58f, 0.52f) * 0.78f;
            RenderSettings.fog = false;
        }

        private static Camera BuildCamera(int rows)
        {
            var go = new GameObject("Sheet Camera");
            var camera = go.AddComponent<Camera>();

            camera.orthographic = true;
            camera.orthographicSize = (rows + BottomMargin) * 0.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;

            // A mid slate rather than white or black: every model here is either pale kit
            // plastic or near-black ink outline, and both vanish against one of those.
            camera.backgroundColor = new Color(0.16f, 0.17f, 0.22f, 1.0f);

            // Centre of the grid. Cells run +X and -Y from the origin.
            float cx = (Columns - 1) * 0.5f;
            float cy = -(rows - 1) * 0.5f - BottomMargin * 0.5f;

            go.transform.position = new Vector3(cx, cy, -20.0f);
            go.transform.rotation = Quaternion.identity;

            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 60.0f;

            // ⚠️⚠️ THE SHEET GRADES ITSELF, FOR THE REASON `ToonProbe` GIVES. `Toon.shader`
            // stopped tonemapping when the ACES curve moved to a full-screen camera pass, so a
            // camera without `ColourGrade` renders a whole stop hotter than the game and the
            // sheet stops being a reference — which is its only job. Eskinita's own numbers,
            // the same ones the key light and ambient above are borrowed from.
            camera.gameObject.AddComponent<TumbangPreso.Visual.ColourGrade>()
                  .Set(1.0f, 1.03f, 1.18f, 0.92f, 1.9f);

            return camera;
        }

        /// <summary>
        /// One model, normalised into its cell and captioned.
        ///
        /// ⚠️ THE BOUNDS COME OFF THE RENDERERS, NOT THE TRANSFORM. An imported prefab's
        /// transform is whatever the exporter wrote and is routinely the identity on a mesh that
        /// is nowhere near the origin, so scaling by it puts half the set off its own cell.
        /// </summary>
        private static void PlaceCell(string path, int col, int row)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;

            var pivot = new GameObject($"cell-{row}-{col}");
            pivot.transform.position = new Vector3(col, -row, 0.0f);

            var model = Object.Instantiate(prefab, pivot.transform);

            // Three-quarter view, so a box reads as a box rather than as a rectangle.
            model.transform.localRotation = CellRotation;

            ToonSkin.Apply(model, _charactersOnly ? ToonSkin.PersonOutlineWidth
                                                  : ToonSkin.PropOutlineWidth);

            if (_charactersOnly) PoseIdle(path, model);

            var renderers = model.GetComponentsInChildren<Renderer>();

            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

                float extent = Mathf.Max(bounds.extents.x,
                                Mathf.Max(bounds.extents.y, bounds.extents.z));

                if (extent > 0.0001f)
                {
                    float scale = CellFill / (extent * 2.0f);
                    model.transform.localScale = Vector3.one * scale;

                    // Re-measure after scaling: recentring on the pre-scale centre leaves tall
                    // models hanging out of the top of their cell.
                    bounds = model.GetComponentsInChildren<Renderer>()[0].bounds;
                    foreach (var r in model.GetComponentsInChildren<Renderer>())
                        bounds.Encapsulate(r.bounds);

                    model.transform.position += pivot.transform.position - bounds.center;

                    // Lift slightly so the caption below is clear of the mesh.
                    model.transform.position += Vector3.up * 0.06f;
                }
            }

            Caption(pivot.transform, Path.GetFileNameWithoutExtension(path));
        }

        /// <summary>
        /// Stand the rig in its idle pose instead of its bind pose.
        ///
        /// ⚠️⚠️ THE BIND POSE IS A T-POSE AND IT IS NOT WHAT THESE CHARACTERS LOOK LIKE. An
        /// instantiated `.glb` sits in whatever pose the skin was authored against — arms
        /// straight out, feet together — which `CharacterAnimator` even names as the joke
        /// behind the `tpose` emote. A cast sheet of twelve T-poses says nothing about the
        /// silhouette any of them actually has in play.
        ///
        /// ⚠️ SAMPLED, NOT PLAYED. There is no Animator running in an editor scene that is
        /// never entered, so `SampleAnimation` at t=0 is what puts the bones where frame one of
        /// `idle` puts them. It is also why this needs no playmode and no `AnimationMode`
        /// bracket: nothing is being recorded, only read once before the capture.
        ///
        /// ⚠️ AND A MISS IS FINE. A rig that ships no clip called `idle` keeps its bind pose
        /// rather than failing the sheet; the clips are placeholder (§4a) and the set they ship
        /// with is expected to change.
        /// </summary>
        private static void PoseIdle(string path, GameObject model)
        {
            AnimationClip idle = null;

            foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (!(sub is AnimationClip clip)) continue;
                if (clip.name.ToLowerInvariant() != "idle") continue;

                idle = clip;
                break;
            }

            if (idle == null) return;

            idle.SampleAnimation(model, 0.0f);
        }

        /// <summary>
        /// ⚠️ A `TextMesh`, NOT A CANVAS. A world-space UI canvas needs an EventSystem and a
        /// scaler to render predictably off a bare orthographic camera in batch mode; a TextMesh
        /// is geometry and simply draws. The sheet is a picture, so nothing here needs to be
        /// interactive.
        /// </summary>
        private static void Caption(Transform parent, string words)
        {
            var go = new GameObject("Caption");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0.0f, -0.40f, -0.5f);

            var text = go.AddComponent<TextMesh>();

            text.text = words.Length > 22 ? words.Substring(0, 21) + "…" : words;
            text.anchor = TextAnchor.UpperCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 64;

            // ⚠️ THE CAPTION IS SIZED IN WORLD UNITS, SO IT DOES NOT SHRINK WHEN THE CELL GROWS.
            // The cast sheet renders the same 1.0 cell at 420 px instead of 220, so a caption
            // that reads correctly on the full sheet comes out nearly twice the height of the
            // character it labels. Scaled per mode rather than per pixel size.
            text.characterSize = _charactersOnly ? 0.010f : 0.018f;
            text.color = new Color(0.93f, 0.9f, 0.82f, 1.0f);
        }

        private static bool Capture(Camera camera, int width, int height)
        {
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4,
            };

            camera.targetTexture = rt;
            camera.Render();

            RenderTexture.active = rt;

            var shot = new Texture2D(width, height, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            shot.Apply();

            RenderTexture.active = null;
            camera.targetTexture = null;

            Directory.CreateDirectory("Logs");
            File.WriteAllBytes(OutPath, shot.EncodeToPNG());

            Object.DestroyImmediate(shot);
            rt.Release();
            Object.DestroyImmediate(rt);

            return File.Exists(OutPath) && new FileInfo(OutPath).Length > 0;
        }
    }
}
