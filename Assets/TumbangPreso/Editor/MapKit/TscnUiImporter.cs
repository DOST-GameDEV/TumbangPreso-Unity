using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TumbangPreso.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// Converts the Godot UI scenes into Unity scenes, node for node, reusing the real art.
    ///
    /// ⚠️⚠️ THIS REPLACES A HAND-REBUILT UI AND THAT REBUILD WAS THE WRONG CALL. The menus were
    /// first re-created from the palette, which produced screens that were tidy, consistent,
    /// and nothing like the game. The `.tscn` files carry every anchor, offset, texture, font
    /// size and colour explicitly, exactly as the map scenes do, so there is no reason to
    /// approximate any of it. Convert, do not redraw.
    ///
    /// ⚠️ INSTANCED SUB-SCENES ARE EXPANDED IN PLACE. `ArrowButton.tscn` is instanced ten times
    /// across the menus with per-instance overrides (its texture, caption, colours, and the
    /// geometry of the arrow). Treating an instance as an opaque node would produce menus with
    /// no buttons at all, so the sub-scene is parsed and inlined, then the instance's own
    /// properties are applied on top.
    ///
    /// ⚠️ AND A MISSING TEXTURE IS LOUD. A silently absent button is a screen that looks
    /// deliberately empty; a magenta box is obviously a missing asset.
    /// </summary>
    public static class TscnUiImporter
    {
        private const string SourceDir = "MapSource/scenes_ui";
        private const string OutDir = "Assets/TumbangPreso/Scenes/Ui";
        private const string ArtRoot = "Assets/TumbangPreso/Art";
        private const string ResultPath = "Logs/ui-import.txt";

        /// <summary>Godot's design resolution, from project.godot.</summary>
        private static readonly Vector2 Reference = new Vector2(1920, 1080);

        [MenuItem("Tumbang Preso/Import Godot UI")]
        public static void ImportAllFromMenu() => Execute();

        public static void ImportAll() => EditorApplication.Exit(Execute() ? 0 : 1);

        private static readonly StringBuilder Report = new StringBuilder();
        private static Font _font;

        private static bool Execute()
        {
            Report.Clear();
            Report.AppendLine("UI IMPORT (Godot .tscn -> Unity scenes)");
            Report.AppendLine();

            _font = AssetDatabase.LoadAssetAtPath<Font>(
                $"{ArtRoot}/ui/fonts/DarumadropOne-Regular.ttf");

            if (_font == null)
            {
                Report.AppendLine("WARNING: Darumadrop font not found; falling back to built-in.");
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            else
            {
                Report.AppendLine("font: DarumadropOne-Regular");
            }

            Report.AppendLine();

            if (!Directory.Exists(SourceDir))
            {
                Report.AppendLine($"FAIL: {SourceDir} does not exist.");
                Flush();
                return false;
            }

            Directory.CreateDirectory(OutDir);

            bool ok = true;

            // ⚠️ THESE ARE COMPONENT SCENES, NOT SCREENS. They are instanced BY the screens and
            // must never become scenes of their own, or the build settings fill with fragments
            // and a menu button loads a lone widget.
            var componentsOnly = new HashSet<string>
            {
                "ArrowButton", "PremiseIcon", "YouCard", "RoleSwapCard", "OffscreenIndicators",
                "DebugBar",
            };

            foreach (var path in Directory.GetFiles(SourceDir, "*.tscn"))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                if (componentsOnly.Contains(name)) continue;

                try
                {
                    ok &= ImportScreen(path, name);
                }
                catch (Exception e)
                {
                    Report.AppendLine($"FAIL {name}: {e.Message}");
                    ok = false;
                }
            }

            Report.AppendLine();
            Report.AppendLine(ok ? "RESULT: OK." : "RESULT: FAILED.");
            Flush();

            AssetDatabase.Refresh();
            return ok;
        }

        private static void Flush()
        {
            try
            {
                Directory.CreateDirectory("Logs");
                File.WriteAllText(ResultPath, Report.ToString());
            }
            catch { }

            Debug.Log(Report.ToString());
        }

        // -------------------------------------------------------------------

        private static bool ImportScreen(string path, string screenName)
        {
            var scene = TscnUi.Parse(File.ReadAllLines(path));
            Report.AppendLine($"-- {screenName} --");
            Report.AppendLine($"   {scene.Ext.Count} ext, {scene.Sub.Count} sub, {scene.Nodes.Count} nodes");

            var unityScene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;

            // ⚠️ THE LISTENER LIVES ON THE SERVICES OBJECT, not here, so it survives scene
            // changes. A second listener in a scene produces a warning and unpredictable
            // volume, which is worse than the silence it looks like it is fixing.

            var canvasGo = new GameObject($"{screenName}Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;

            // ⚠️ MATCH ON HEIGHT. The Godot layout is authored against a fixed 1920x1080 and
            // its menus are anchored from the left edge; matching on width instead crops the
            // arrow buttons off the side on anything wider than 16:9.
            scaler.matchWidthOrHeight = 1.0f;

            canvasGo.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            var byPath = new Dictionary<string, Transform> { { ".", canvasGo.transform } };
            int built = 0, missing = 0;

            foreach (var n in scene.Nodes)
            {
                if (n.Parent == null)
                {
                    // The root Control becomes the canvas itself.
                    byPath["."] = canvasGo.transform;
                    continue;
                }

                var go = BuildNode(n, scene, ref missing);
                built++;

                Transform parent = byPath.TryGetValue(n.Parent, out var p) ? p : canvasGo.transform;
                go.transform.SetParent(parent, worldPositionStays: false);

                TscnUi.ApplyControlRect(go.GetComponent<RectTransform>(), n);
                byPath[n.PathKey] = go.transform;
            }

            Report.AppendLine($"   built {built} nodes, {missing} missing textures");

            AttachBehaviour(screenName, canvasGo);

            string outPath = $"{OutDir}/{screenName}.unity";
            bool saved = EditorSceneManager.SaveScene(unityScene, outPath);
            Report.AppendLine(saved ? $"   wrote {outPath}" : $"   FAILED to write {outPath}");
            Report.AppendLine();

            return saved;
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
                return;

            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // -------------------------------------------------------------------

        private static GameObject BuildNode(TscnUi.NodeDef n, TscnUi.Scene scene, ref int missing)
        {
            var go = new GameObject(n.Name);
            go.AddComponent<RectTransform>();

            // An instanced sub-scene: ArrowButton and friends.
            if (n.InstanceExtId != null && scene.Ext.TryGetValue(n.InstanceExtId, out var inst))
            {
                BuildInstance(go, n, inst, scene, ref missing);
                return go;
            }

            switch (n.Type)
            {
                case "ColorRect":
                    {
                        var img = go.AddComponent<Image>();
                        img.color = TscnUi.ParseColor(
                            n.Props.TryGetValue("color", out var c) ? c : null, Color.white);
                        img.raycastTarget = false;
                        break;
                    }

                case "TextureRect":
                    {
                        var img = go.AddComponent<Image>();
                        img.sprite = LoadSprite(n, scene, ref missing);
                        img.color = img.sprite != null ? Color.white : Color.magenta;
                        img.raycastTarget = false;

                        // Godot stretch_mode 6 is "keep aspect covered"; 5 is "keep aspect fit".
                        int stretch = (int)TscnUi.Prop(n, "stretch_mode", 0.0f);
                        img.preserveAspect = stretch == 5;
                        img.type = Image.Type.Simple;
                        break;
                    }

                case "Label":
                    BuildLabel(go, n);
                    break;

                case "Button":
                case "TextureButton":
                    {
                        var img = go.AddComponent<Image>();
                        var sprite = LoadSprite(n, scene, ref missing);

                        img.sprite = sprite;
                        img.color = sprite != null ? Color.white : UiTheme.WoodDeep;
                        img.type = Image.Type.Simple;

                        var btn = go.AddComponent<Button>();
                        btn.targetGraphic = img;

                        string caption = TscnUi.Str(n, "text");
                        if (!string.IsNullOrEmpty(caption))
                        {
                            var labelGo = new GameObject("Label");
                            labelGo.AddComponent<RectTransform>();
                            labelGo.transform.SetParent(go.transform, false);

                            var t = MakeText(labelGo, caption, 28, UiTheme.Cream,
                                             TextAnchor.MiddleCenter);
                            t.raycastTarget = false;
                            TscnUi.ApplyControlRect(t.rectTransform, new TscnUi.NodeDef
                            {
                                Props = { { "anchor_right", "1.0" }, { "anchor_bottom", "1.0" } }
                            });
                        }
                        break;
                    }

                case "Panel":
                case "PanelContainer":
                case "NinePatchRect":
                    {
                        var img = go.AddComponent<Image>();
                        img.sprite = LoadSprite(n, scene, ref missing);
                        img.color = img.sprite != null ? Color.white : UiTheme.WoodDeep;
                        break;
                    }

                case "ProgressBar":
                case "TextureProgressBar":
                    {
                        var img = go.AddComponent<Image>();
                        img.sprite = LoadSprite(n, scene, ref missing);
                        img.color = img.sprite != null ? Color.white : UiTheme.Cream;
                        img.type = Image.Type.Filled;
                        img.fillMethod = Image.FillMethod.Horizontal;
                        break;
                    }

                // Containers, viewports and plain Controls carry layout only.
                default:
                    break;
            }

            return go;
        }

        /// <summary>
        /// Inlines an instanced sub-scene, then applies the instance's overrides.
        ///
        /// ⚠️ THE OVERRIDES ARE THE WHOLE POINT. Every ArrowButton instance shares one scene and
        /// differs entirely by its per-instance properties: which texture it wears, its caption,
        /// its text colour, and the arrow geometry. Inlining without them produces ten identical
        /// blank buttons.
        /// </summary>
        private static void BuildInstance(GameObject go, TscnUi.NodeDef n, TscnUi.ExtRes inst,
                                          TscnUi.Scene scene, ref int missing)
        {
            var img = go.AddComponent<Image>();
            var sprite = LoadSprite(n, scene, ref missing);

            img.sprite = sprite;
            img.color = sprite != null ? Color.white : Color.magenta;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var colors = btn.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1.0f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1.0f);
            colors.fadeDuration = 0.06f;
            btn.colors = colors;

            string caption = TscnUi.Str(n, "caption") ?? TscnUi.Str(n, "text");
            if (string.IsNullOrEmpty(caption)) return;

            var labelGo = new GameObject("Caption");
            labelGo.AddComponent<RectTransform>();
            labelGo.transform.SetParent(go.transform, false);

            int size = (int)TscnUi.Prop(n, "label_size", 64.0f);
            var color = TscnUi.ParseColor(
                n.Props.TryGetValue("text_color", out var tc) ? tc : null, UiTheme.Ink);

            var text = MakeText(labelGo, caption, size, color, TextAnchor.MiddleLeft);
            text.raycastTarget = false;

            // ⚠️ THE CAPTION IS INDENTED INTO THE ARROW BODY, NOT CENTRED. The button art is an
            // arrow with a long tip, so a centred label sits in the point where there is no
            // room for it. `text_indent` is the authored left inset and it is per-instance.
            float indent = TscnUi.Prop(n, "text_indent", 0.0f);
            float offsetY = TscnUi.Prop(n, "text_offset_y", 0.0f);

            var rt = text.rectTransform;
            rt.anchorMin = new Vector2(0.0f, 0.0f);
            rt.anchorMax = new Vector2(1.0f, 1.0f);
            rt.offsetMin = new Vector2(indent, -offsetY);
            rt.offsetMax = new Vector2(0.0f, -offsetY);
        }

        private static void BuildLabel(GameObject go, TscnUi.NodeDef n)
        {
            string content = TscnUi.Str(n, "text") ?? "";
            int size = (int)TscnUi.Prop(n, "theme_override_font_sizes/font_size", 28.0f);

            var color = TscnUi.ParseColor(
                n.Props.TryGetValue("theme_override_colors/font_color", out var fc) ? fc : null,
                UiTheme.Cream);

            var text = MakeText(go, content, size, color, TscnUi.Align(n));
            text.raycastTarget = false;

            // ⚠️ THE OUTLINE IS NOT DECORATION. Every label in this game sits over a photographic
            // backdrop, and without the dark outline the cream lettering is unreadable against
            // the bright parts of the street. Godot authors it per label; Unity needs a
            // component.
            int outline = (int)TscnUi.Prop(n, "theme_override_constants/outline_size", 0.0f);
            if (outline <= 0) return;

            var oc = TscnUi.ParseColor(
                n.Props.TryGetValue("theme_override_colors/font_outline_color", out var o) ? o : null,
                Color.black);

            var shadow = go.AddComponent<Outline>();
            shadow.effectColor = oc;

            // Godot's outline_size is a radius in pixels; UGUI's Outline is a corner offset, so
            // roughly a quarter reads the same weight.
            float d = Mathf.Max(1.0f, outline * 0.25f);
            shadow.effectDistance = new Vector2(d, -d);
        }

        private static Text MakeText(GameObject go, string content, int size, Color color,
                                     TextAnchor align)
        {
            var t = go.GetComponent<Text>();
            if (t == null) t = go.AddComponent<Text>();

            t.font = _font;
            t.text = content;
            t.fontSize = Mathf.Max(1, size);
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static Sprite LoadSprite(TscnUi.NodeDef n, TscnUi.Scene scene, ref int missing)
        {
            if (!n.Props.TryGetValue("texture", out var raw)) return null;

            string id = TscnUi.ExtId(raw);
            if (id == null || !scene.Ext.TryGetValue(id, out var res)) return null;

            string assetPath = TscnUi.ToAssetPath(res.Path, ArtRoot);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

            if (sprite == null)
            {
                // ⚠️ A PNG IMPORTS AS A TEXTURE BY DEFAULT, NOT A SPRITE, so the first load
                // returns null even though the file is right there. Flip the importer and try
                // again rather than reporting a missing asset that exists.
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();

                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                }
            }

            if (sprite == null)
            {
                Report.AppendLine($"      MISSING texture: {res.Path}");
                missing++;
            }

            return sprite;
        }

        /// <summary>
        /// Attaches the behaviour script that drives a converted screen.
        ///
        /// ⚠️ THE LAYOUT IS CONVERTED; THE BEHAVIOUR IS PORTED CODE. A `.tscn` carries no logic,
        /// only the tree, so each screen still needs its script. Wiring them by name here keeps
        /// that pairing in one place rather than scattered through the converter.
        /// </summary>
        private static void AttachBehaviour(string screenName, GameObject canvasGo)
        {
            switch (screenName)
            {
                case "MainMenu": canvasGo.AddComponent<ConvertedMainMenu>(); break;
                case "ModeSelect": canvasGo.AddComponent<ConvertedModeSelect>(); break;
                default:
                    Report.AppendLine($"      (no behaviour bound yet for {screenName})");
                    break;
            }
        }
    }
}
