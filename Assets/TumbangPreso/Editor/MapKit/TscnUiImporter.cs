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

                // ⚠️⚠️ A CHILD OF A LAYOUT GROUP MUST NOT GET GODOT'S ANCHORS. Godot containers
                // compute their children's positions at runtime and write nothing useful into
                // the .tscn for them, so applying those empty offsets pins every child to the
                // same corner AND fights the Unity layout group that is trying to place it.
                // The result is a readable-looking pile of overlapping labels, which is exactly
                // what the first conversion produced.
                bool parentLays = parent != null && parent.GetComponent<LayoutGroup>() != null;

                if (parentLays) ApplyLayoutElement(go, n);
                else TscnUi.ApplyControlRect(go.GetComponent<RectTransform>(), n);

                // ⚠️⚠️ RESPECT `visible = false`. Several screens carry a hidden duplicate of a
                // control that the script swaps in for a different state, and converting them
                // all as visible stacks two identical buttons on top of each other. That is
                // what put two START MATCH labels on the setup screen, and it is invisible in
                // the import log because both converted perfectly.
                if (n.Props.TryGetValue("visible", out var vis) && vis.Trim() == "false")
                    go.SetActive(false);

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

        /// <summary>
        /// Sizes a laid-out child from whatever Godot recorded about its size.
        ///
        /// ⚠️ `custom_minimum_size` IS THE ONLY SIZE HINT A CONTAINER CHILD CARRIES. Godot's
        /// containers derive everything else, so this is the one authored number available, and
        /// without it every child collapses to zero and the panel renders as a thin line.
        /// </summary>
        private static void ApplyLayoutElement(GameObject go, TscnUi.NodeDef n)
        {
            var el = go.GetComponent<LayoutElement>();
            if (el == null) el = go.AddComponent<LayoutElement>();

            Vector2 min = Vector2.zero;
            if (n.Props.TryGetValue("custom_minimum_size", out var raw))
            {
                var m = System.Text.RegularExpressions.Regex.Match(raw, @"Vector2\(([^)]*)\)");
                if (m.Success)
                {
                    var parts = m.Groups[1].Value.Split(',');
                    if (parts.Length >= 2) min = new Vector2(TscnUi.F(parts[0]), TscnUi.F(parts[1]));
                }
            }

            // Fall back to the authored offsets, which containers still carry for fixed-size
            // leaves such as the selector boxes and the arrow buttons.
            float w = min.x > 0.0f ? min.x
                : Mathf.Abs(TscnUi.Prop(n, "offset_right") - TscnUi.Prop(n, "offset_left"));
            float h = min.y > 0.0f ? min.y
                : Mathf.Abs(TscnUi.Prop(n, "offset_bottom") - TscnUi.Prop(n, "offset_top"));

            if (w > 0.0f) el.preferredWidth = w;
            if (h > 0.0f) el.preferredHeight = h;

            // ⚠️ A LABEL WITH NO AUTHORED HEIGHT STILL NEEDS ONE, or the row it sits in has zero
            // height and the whole column above it silently disappears.
            if (h <= 0.0f && go.GetComponent<Text>() != null)
                el.preferredHeight = go.GetComponent<Text>().fontSize * 1.6f;

            // ⚠️⚠️ size_flags IS HOW GODOT SPLITS A ROW, AND IGNORING IT COLLAPSES THE LAYOUT.
            // Bit 1 (value 1) is FILL and bit 2 (value 2) is EXPAND, so the common `3` means
            // "take a share of the leftover space". The setup screen's two columns are both 3;
            // without it they fall back to a preferred width of zero and stack on top of each
            // other, which is exactly how the left and right panels ended up overlapping.
            int hFlags = (int)TscnUi.Prop(n, "size_flags_horizontal", 1.0f);
            int vFlags = (int)TscnUi.Prop(n, "size_flags_vertical", 1.0f);

            const int Expand = 2;

            if ((hFlags & Expand) != 0)
            {
                el.flexibleWidth = Mathf.Max(1.0f, TscnUi.Prop(n, "size_flags_stretch_ratio", 1.0f));
                if (w <= 0.0f) el.preferredWidth = -1.0f;
            }

            if ((vFlags & Expand) != 0)
            {
                el.flexibleHeight = Mathf.Max(1.0f, TscnUi.Prop(n, "size_flags_stretch_ratio", 1.0f));
                if (h <= 0.0f) el.preferredHeight = -1.0f;
            }

            // ⚠️⚠️ A CHILD WITH NO SIZE HINT AT ALL COLLAPSES TO ZERO AND STACKS AT THE EDGE.
            // Godot sizes a Label from its text and a container from its contents, neither of
            // which is written into the scene. Under a Unity layout group that means width
            // zero, so a row's caption and its selector land on exactly the same pixel and
            // overlap perfectly, which is what "MAP:" over "ESKINITA" was.
            if (el.preferredWidth <= 0.0f && el.flexibleWidth <= 0.0f)
            {
                var text = go.GetComponent<Text>();

                if (text != null)
                {
                    // ⚠️ A ContentSizeFitter, NOT Text.preferredWidth. That property is zero at
                    // import time because the Text has never been through a layout pass, so
                    // reading it here silently assigns every label a width of nothing and they
                    // all pile up on the left edge. A fitter measures at runtime, when the font
                    // actually has metrics.
                    var fitter = go.GetComponent<ContentSizeFitter>();
                    if (fitter == null) fitter = go.AddComponent<ContentSizeFitter>();

                    fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }
                else
                {
                    el.flexibleWidth = 1.0f;
                }
            }

            // ⚠️ A CONTAINER'S HEIGHT COMES FROM ITS TALLEST CHILD, and Godot never writes that
            // down because it computes it. Without a floor here a row is zero-high and
            // everything inside it lands on one line. 84 is the authored height of the selector
            // boxes, which is what actually sets the row height on this screen.
            if (el.preferredHeight <= 0.0f && el.flexibleHeight <= 0.0f)
            {
                bool isRow = n.Type == "HBoxContainer" || n.Type == "VBoxContainer";
                if (isRow) el.minHeight = 84.0f;
                else if (go.GetComponent<Text>() == null) el.flexibleHeight = 1.0f;
            }
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
                        // ⚠️⚠️ NO TEXTURE MEANS NO IMAGE, NOT A MAGENTA ONE. A TextureRect whose
                        // texture is assigned in code (a map preview, a character render, a
                        // gradient scrim built at runtime) has no `texture` line in the .tscn at
                        // all. Marking those magenta filled the entire setup screen with a
                        // full-bleed pink rect that hid every control behind it. Only a texture
                        // that was REFERENCED and failed to load deserves the marker.
                        if (!n.Props.ContainsKey("texture")) break;

                        // ⚠️⚠️ A GENERATED TEXTURE IS NOT A MISSING ONE. The scrim that darkens
                        // the backdrop behind every panel is a GradientTexture2D built in the
                        // scene, so it is a SubResource with no file behind it. Treating it as
                        // a failed load painted a full-bleed magenta rect over the entire setup
                        // screen and hid every control on it.
                        if (TscnUi.SubId(n.Props["texture"]) != null)
                        {
                            var scrim = go.AddComponent<Image>();
                            scrim.color = GradientTint(n, scene);
                            scrim.raycastTarget = false;
                            break;
                        }

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

                // ⚠️ A SubViewport IS A LIVE 3D RENDER, NOT A SPRITE. The setup screen shows the
                // chosen map and the character select spins the actual model. Those need a
                // camera rendering to a texture, which is wired at runtime by the screen's
                // behaviour; here the node is a placeholder that must stay INVISIBLE rather
                // than painting over the panel beside it.
                case "SubViewportContainer":
                case "SubViewport":
                case "ViewportTexture":
                    break;

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
                case "NinePatchRect":
                    {
                        var img = go.AddComponent<Image>();
                        img.sprite = LoadSprite(n, scene, ref missing);
                        img.color = img.sprite != null ? Color.white : UiTheme.WoodDeep;
                        break;
                    }

                // ⚠️⚠️ A PanelContainer IS A CONTAINER, NOT A BACKGROUND. It was treated as a
                // plain image, so it never sized its child, and everything below it in the tree
                // collapsed to zero: the margin, the rows, and every caption and selector
                // inside them, which is why they all landed on the same pixel. The Godot name
                // says "Panel" first and that is the trap; the word that matters is Container.
                case "PanelContainer":
                    {
                        var img = go.AddComponent<Image>();
                        img.sprite = LoadSprite(n, scene, ref missing);
                        img.color = img.sprite != null ? Color.white : UiTheme.WoodDeep;

                        var g = go.AddComponent<VerticalLayoutGroup>();
                        g.childControlHeight = true;
                        g.childControlWidth = true;
                        g.childForceExpandHeight = true;
                        g.childForceExpandWidth = true;
                        break;
                    }

                // Same trap, same fix: these all lay their children out in Godot.
                case "ScrollContainer":
                case "CenterContainer":
                case "AspectRatioContainer":
                    {
                        var g = go.AddComponent<VerticalLayoutGroup>();
                        g.childControlHeight = true;
                        g.childControlWidth = true;
                        g.childForceExpandHeight = true;
                        g.childForceExpandWidth = true;
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

                // ⚠️⚠️ GODOT CONTAINERS POSITION THEIR CHILDREN AT RUNTIME, AND THAT IS WHY THE
                // CHILDREN COLLAPSED. A VBoxContainer's children carry no useful offsets in the
                // .tscn because the container computes them, so converting the container as a
                // plain rect stacks every child at the same corner. Unity's layout groups do
                // the same job and must be added, or the panel arrives as a pile of overlapping
                // labels in the top-left.
                case "VBoxContainer":
                    {
                        var v = go.AddComponent<VerticalLayoutGroup>();

                        // ⚠️⚠️ childControlHeight MUST BE ON, and this cost several passes to
                        // find. With it off the group does not size its children vertically, so
                        // every row kept a height of ZERO. A row with no height collapses its
                        // own horizontal group, which put each caption exactly on top of its
                        // value: "MAP:" printed over "ESKINITA" and read as a font bug rather
                        // than a layout one.
                        v.childControlHeight = true;
                        v.childControlWidth = true;
                        v.childForceExpandHeight = false;
                        v.childForceExpandWidth = true;
                        v.spacing = TscnUi.Prop(n, "theme_override_constants/separation", 8.0f);
                        break;
                    }

                case "HBoxContainer":
                    {
                        var h = go.AddComponent<HorizontalLayoutGroup>();

                        // ⚠️ childControlWidth MUST BE ON so the group honours flexibleWidth.
                        // With it off, a child that asked to expand is drawn at its own size
                        // and the row does not divide at all.
                        h.childControlHeight = true;
                        h.childControlWidth = true;
                        h.childForceExpandHeight = true;
                        h.childForceExpandWidth = false;
                        h.childAlignment = TextAnchor.MiddleLeft;
                        h.spacing = TscnUi.Prop(n, "theme_override_constants/separation", 8.0f);
                        break;
                    }

                case "MarginContainer":
                    {
                        var pad = go.AddComponent<LayoutElement>();
                        pad.ignoreLayout = false;

                        var group = go.AddComponent<VerticalLayoutGroup>();
                        group.childControlHeight = true;
                        group.childControlWidth = true;
                        group.childForceExpandHeight = true;
                        group.childForceExpandWidth = true;
                        group.padding = new RectOffset(
                            (int)TscnUi.Prop(n, "theme_override_constants/margin_left", 0.0f),
                            (int)TscnUi.Prop(n, "theme_override_constants/margin_right", 0.0f),
                            (int)TscnUi.Prop(n, "theme_override_constants/margin_top", 0.0f),
                            (int)TscnUi.Prop(n, "theme_override_constants/margin_bottom", 0.0f));
                        break;
                    }

                // Plain Controls carry layout only.
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

            // ⚠️ ONLY THE NODE THAT AUTHORS THE CAPTION DRAWS IT. A primary action is often a
            // wrapper node holding the real button, and both carrying the text is how the setup
            // screen ended up with two overlapping START MATCH labels.
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

        /// <summary>
        /// A flat stand-in for a Godot gradient scrim.
        ///
        /// ⚠️ THE SCRIM IS LOAD-BEARING, NOT DECORATION. Every panel in this game sits over a
        /// photographic backdrop, and without it the cream lettering is unreadable against the
        /// bright half of the street. A flat tint at the gradient's own alpha reads close
        /// enough at these sizes; a real gradient is a texture to generate later if it matters.
        /// </summary>
        private static Color GradientTint(TscnUi.NodeDef n, TscnUi.Scene scene)
        {
            string id = TscnUi.SubId(n.Props["texture"]);

            if (id != null && scene.Sub.TryGetValue(id, out var sub) &&
                sub.Props.TryGetValue("gradient", out var gradRef))
            {
                string gid = TscnUi.SubId(gradRef);
                if (gid != null && scene.Sub.TryGetValue(gid, out var grad) &&
                    grad.Props.TryGetValue("colors", out var colors))
                {
                    // PackedColorArray(r, g, b, a, r, g, b, a, ...) — take the first stop.
                    var m = System.Text.RegularExpressions.Regex.Match(colors, @"\(([^)]*)\)");
                    if (m.Success)
                    {
                        var parts = m.Groups[1].Value.Split(',');
                        if (parts.Length >= 4)
                            return new Color(TscnUi.F(parts[0]), TscnUi.F(parts[1]),
                                             TscnUi.F(parts[2]), TscnUi.F(parts[3]));
                    }
                }
            }

            return new Color(0.0f, 0.0f, 0.0f, 0.45f);
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
                case "MatchSetup": canvasGo.AddComponent<ConvertedMatchSetup>(); break;
                case "CharacterSelect": canvasGo.AddComponent<ConvertedCharacterSelect>(); break;
                case "MultiplayerSetup": canvasGo.AddComponent<ConvertedMultiplayerSetup>(); break;
                case "MatchResult": canvasGo.AddComponent<ConvertedMatchResult>(); break;
                case "SplashScreen": canvasGo.AddComponent<SplashScreen>(); break;

                // ⚠️ THESE ARE OVERLAYS, NOT SCREENS. Settings, Tutorial and Credits are opened
                // on top of whatever is running and must not become scenes of their own, or
                // opening settings mid-match unloads the match.
                case "SettingsPanel":
                case "Tutorial":
                case "CreditsPanel":
                    Report.AppendLine($"      ({screenName} is an overlay; converted for reuse as a panel)");
                    break;

                case "HUD":
                    Report.AppendLine("      (HUD is bound at match start, not here)");
                    break;

                default:
                    Report.AppendLine($"      (no behaviour bound yet for {screenName})");
                    break;
            }
        }
    }
}
