using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TumbangPreso.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// Converts the Godot UI scenes into Unity scenes, node for node, reusing the real art.
    ///
    /// ⚠️⚠️ THIS REPLACES A HAND-REBUILT UI AND THAT REBUILD WAS THE WRONG CALL. The menus were
    /// first re-created from the palette, which produced screens that were tidy, consistent,
    /// and nothing like the game. The `.tscn` files carry every anchor, offset, texture, font
    /// size and colour explicitly, exactly as the map scenes do. Convert, do not redraw.
    ///
    /// ⚠️⚠️ AND THE FIRST CONVERSION READ ONLY HALF OF WHAT A `.tscn` SAYS, WHICH IS WHY IT
    /// LOOKED BROKEN. Godot's theme is project-wide (`gui/theme/custom`), so a scene records
    /// styling as `theme_type_variation = &"MenuHeading"` and the theme supplies the face. The
    /// importer read `theme_override_*` and ignored variations completely, so:
    ///   - every label came out at one fallback size in one fallback colour, which is why a
    ///     300px "MAP:" caption printed at the same weight as its value and landed on top of it,
    ///   - every WoodPanel came out as a flat brown rectangle with no border, radius or shadow,
    ///   - every WoodButton lost its five states, its lettering colours and its press.
    /// <see cref="GodotTheme"/> is the missing half. It is not optional decoration: the layout
    /// itself depends on it, because a StyleBox carries the content margins a PanelContainer
    /// insets its child by.
    ///
    /// ⚠️ SIZE FLAGS ARE LAYOUT, NOT A HINT. Godot's 0 / 1 / 2 / 4 / 8 are SHRINK_BEGIN, FILL,
    /// EXPAND, SHRINK_CENTER and SHRINK_END, and a container reads them per axis. Treating every
    /// child as "fill" collapses a row of mixed children onto one another.
    ///
    /// ⚠️ INSTANCED SUB-SCENES ARE EXPANDED FOR REAL, RECURSIVELY. `ArrowButton.tscn` is
    /// instanced ten times with per-instance overrides, and MainMenu instances the whole
    /// SettingsPanel, Tutorial and Credits scenes as hidden children. An earlier version turned
    /// every instance into a single Image, which is why the settings overlay had to be
    /// hand-drawn in code and looked nothing like the game's.
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

        /// <summary>Parsed `.tscn` files, so an instanced sub-scene is read once per run.</summary>
        private static readonly Dictionary<string, TscnUi.Scene> SceneCache =
            new Dictionary<string, TscnUi.Scene>();

        private static bool Execute()
        {
            Report.Clear();
            SceneCache.Clear();
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
                // ⚠️ AND THESE THREE ARE OVERLAYS. They are instanced into MainMenu as hidden
                // children and opened in place, exactly as `main_menu.gd` does it. Converting
                // them as standalone scenes as well is how a build ends up with two settings
                // panels, one of which nothing can reach.
                // ⚠️ `Tutorial` LEFT THIS LIST BECAUSE THE PANEL LEFT THE GAME. Its .tscn is still
                // in the frozen Godot repo, so naming it here would re-import a screen with no
                // behaviour left to bind to it and `SceneScriptCheck` would refuse the build.
                "SettingsPanel", "CreditsPanel", "CharacterSelect",
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
                    Report.AppendLine($"FAIL {name}: {e.Message}\n{e.StackTrace}");
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

        private static TscnUi.Scene Load(string name)
        {
            if (SceneCache.TryGetValue(name, out var cached)) return cached;

            string path = $"{SourceDir}/{name}.tscn";
            if (!File.Exists(path)) return null;

            var scene = TscnUi.Parse(File.ReadAllLines(path));
            SceneCache[name] = scene;
            return scene;
        }

        // -------------------------------------------------------------------

        private static bool ImportScreen(string path, string screenName)
        {
            var scene = TscnUi.Parse(File.ReadAllLines(path));
            SceneCache[screenName] = scene;

            Report.AppendLine($"-- {screenName} --");
            Report.AppendLine($"   {scene.Ext.Count} ext, {scene.Sub.Count} sub, {scene.Nodes.Count} nodes");

            var unityScene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;

            var canvasGo = new GameObject($"{screenName}Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;

            // ⚠️ MATCH ON HEIGHT. The Godot layout is authored against a fixed 1920x1080 and its
            // menus are anchored from the left edge; matching on width instead crops the arrow
            // buttons off the side on anything wider than 16:9.
            scaler.matchWidthOrHeight = 1.0f;

            // ⚠️ AND EXPAND ON TOP OF IT, which is match-on-height at 16:9 and wider and stops
            // being a crop at 16:10 and 4:3. See TumbangPreso.UI.AspectSafeCanvas for the
            // arithmetic; ConvertedScreen applies the same rule at runtime for the screens that
            // were imported before this line existed.
            UI.AspectSafeCanvas.Apply(scaler);

            canvasGo.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            var state = new BuildState { Canvas = canvasGo.transform };
            state.ByPath["."] = canvasGo.transform;

            BuildNodes(scene, state, ".", canvasGo.transform, 0);

            Report.AppendLine($"   built {state.Built} nodes, {state.Missing} missing textures");

            FinishScrollRects(canvasGo.transform);
            ClearStrayRaycastTargets(canvasGo.transform);
            AttachBehaviour(screenName, canvasGo);
            AttachNestedPanels(state);
            StampVersion(screenName, canvasGo.transform);

            // ⚠️ CHECKED BEFORE IT IS WRITTEN. Both of these catch a scene that opens perfectly
            // in the editor and refuses to load in a player, which is the most expensive kind of
            // bug this importer can produce: nothing is visibly wrong until the .exe is handed
            // over. See each method for the failure it exists to stop.
            bool clean = NothingRuntimeOnly(canvasGo.transform);
            clean &= ScriptsResolve(canvasGo.transform);

            string outPath = $"{OutDir}/{screenName}.unity";
            bool saved = EditorSceneManager.SaveScene(unityScene, outPath) && clean;
            Report.AppendLine(saved ? $"   wrote {outPath}" : $"   FAILED to write {outPath}");
            Report.AppendLine();

            return saved;
        }

        private sealed class BuildState
        {
            public Transform Canvas;
            public readonly Dictionary<string, Transform> ByPath = new Dictionary<string, Transform>();
            public readonly Dictionary<string, TscnUi.NodeDef> DefByPath =
                new Dictionary<string, TscnUi.NodeDef>();
            public int Built;
            public int Missing;
        }

        /// <summary>
        /// Walks one scene's node list under `rootPath`, which is "." for a screen and the
        /// instance's own path for an inlined sub-scene.
        /// </summary>
        private static void BuildNodes(TscnUi.Scene scene, BuildState state, string prefix,
                                       Transform root, int depth)
        {
            foreach (var n in scene.Nodes)
            {
                if (n.Parent == null)
                {
                    // The scene's own root. For a screen that is the canvas; for an inlined
                    // sub-scene it is the instance node, which already exists.
                    continue;
                }

                string parentPath = Join(prefix, n.Parent);
                string selfPath = Join(prefix, n.PathKey);

                Transform parent = state.ByPath.TryGetValue(parentPath, out var p) ? p : root;

                // ⚠️ A NODE WITH NO TYPE AND NO INSTANCE IS AN OVERRIDE OF SOMETHING THAT ALREADY
                // EXISTS INSIDE AN INLINED SUB-SCENE, not a new node. Building it again produces
                // a second copy of a control sitting exactly on top of the first, which is
                // invisible in the import log because both convert perfectly.
                if (string.IsNullOrEmpty(n.Type) && n.InstanceExtId == null &&
                    state.ByPath.TryGetValue(selfPath, out var existing))
                {
                    ApplyOverrides(existing.gameObject, n, scene, state);
                    continue;
                }

                var go = BuildNode(n, scene, state, depth);
                state.Built++;

                go.transform.SetParent(parent, worldPositionStays: false);

                // ⚠️⚠️ A CHILD OF A LAYOUT GROUP MUST NOT GET GODOT'S ANCHORS. Godot containers
                // compute their children's positions at runtime and write nothing useful into
                // the .tscn for them, so applying those empty offsets pins every child to the
                // same corner AND fights the Unity layout group trying to place it.
                bool parentLays = parent != null && parent.GetComponent<LayoutGroup>() != null;

                if (parentLays)
                {
                    var parentDef = state.DefByPath.TryGetValue(parentPath, out var pd) ? pd : null;
                    ApplyLayoutElement(go, n, parentDef);
                }
                else
                {
                    TscnUi.ApplyControlRect(go.GetComponent<RectTransform>(), n);
                }

                // ⚠️⚠️ RESPECT `visible = false`. Several screens carry a hidden duplicate of a
                // control that the script swaps in for a different state, and converting them
                // all as visible stacks two identical buttons on top of each other.
                if (n.Props.TryGetValue("visible", out var vis) && vis.Trim() == "false")
                    go.SetActive(false);

                state.ByPath[selfPath] = go.transform;
                state.DefByPath[selfPath] = n;

                // An instanced sub-scene: inline its whole tree under this node.
                if (n.InstanceExtId != null)
                {
                    var parentDef = state.DefByPath.TryGetValue(parentPath, out var pdef) ? pdef : null;
                    Inline(go, n, scene, state, selfPath, depth, parentLays, parentDef);
                }
            }
        }

        private static string Join(string prefix, string path)
        {
            if (prefix == ".") return path;
            return path == "." ? prefix : prefix + "/" + path;
        }

        /// <summary>
        /// Inlines an instanced sub-scene, then applies the instance's own overrides on top.
        ///
        /// ⚠️ THE OVERRIDES ARE THE WHOLE POINT. Every ArrowButton instance shares one scene and
        /// differs entirely by its per-instance properties: which texture it wears, its caption,
        /// its text colour, its slant and its indent. Inlining without them produces ten
        /// identical blank buttons.
        /// </summary>
        private static void Inline(GameObject go, TscnUi.NodeDef n, TscnUi.Scene scene,
                                   BuildState state, string selfPath, int depth, bool parentLays,
                                   TscnUi.NodeDef parentDef)
        {
            if (depth > 6) return;   // a guard, not a limit any real screen approaches

            if (!scene.Ext.TryGetValue(n.InstanceExtId, out var res)) return;

            string subName = Path.GetFileNameWithoutExtension(res.Path);
            var sub = Load(subName);

            if (sub == null)
            {
                Report.AppendLine($"      MISSING sub-scene: {res.Path}");
                return;
            }

            // The sub-scene's root properties apply to the instance node itself.
            var subRoot = sub.Nodes.Count > 0 && sub.Nodes[0].Parent == null ? sub.Nodes[0] : null;

            if (subRoot != null)
            {
                ConfigureFromDef(go, subRoot, sub, state);

                // ⚠️⚠️ AN INSTANCE INHERITS THE SUB-SCENE ROOT'S OWN LAYOUT, and forgetting that
                // collapses it to a zero-size box in the top-left corner. `MainMenu.tscn`
                // instances SettingsPanel with nothing but `visible = false`; every anchor it
                // has lives in `SettingsPanel.tscn`'s root, which says full-rect. Reading only
                // the instance line gave the settings overlay a rect of nothing, so its card
                // hung off the left edge of the screen with the title menu showing through.
                //
                // The instance's own properties still win where it states them: every
                // ArrowButton places itself with explicit offsets over the sub-scene's.
                var merged = new TscnUi.NodeDef { Name = n.Name, Type = subRoot.Type };

                foreach (var pair in subRoot.Props) merged.Props[pair.Key] = pair.Value;
                foreach (var pair in n.Props) merged.Props[pair.Key] = pair.Value;

                if (!parentLays)
                    TscnUi.ApplyControlRect(go.GetComponent<RectTransform>(), merged);
                else
                    ApplyLayoutElement(go, merged, parentDef);
            }

            BuildNodes(sub, state, selfPath, go.transform, depth + 1);

            // ArrowButton is the one sub-scene with a script whose exports are the instance's
            // whole appearance, so it gets its own pass.
            if (subName == "ArrowButton") ConfigureArrowButton(go, n, scene, state);
        }

        private static void ApplyOverrides(GameObject go, TscnUi.NodeDef n, TscnUi.Scene scene,
                                           BuildState state)
        {
            var text = go.GetComponent<Text>();
            if (text != null)
            {
                string content = TscnUi.Str(n, "text");
                if (content != null) text.text = content;
            }

            if (n.Props.TryGetValue("visible", out var vis) && vis.Trim() == "false")
                go.SetActive(false);
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// Sizes a laid-out child from Godot's minimum size and its size flags.
        ///
        /// ⚠️⚠️ SIZE FLAGS ARE READ PER AXIS AND THE MEANING DEPENDS ON THE PARENT. Along the
        /// container's own axis, only EXPAND (bit 2) matters: it is what claims a share of the
        /// leftover space. Across it, FILL (bit 1) stretches the child to the container's width
        /// and the SHRINK values (0, 4, 8) leave it at its minimum. Treating everything as fill
        /// is what put the left and right columns of the setup screen on top of each other.
        ///
        /// ⚠️ `custom_minimum_size` IS A MINIMUM, NOT A SIZE. Setting it as the preferred size
        /// and stopping there caps a control that was meant to grow with its text.
        /// </summary>
        private static void ApplyLayoutElement(GameObject go, TscnUi.NodeDef n,
                                               TscnUi.NodeDef parent)
        {
            var el = go.GetComponent<LayoutElement>();
            if (el == null) el = go.AddComponent<LayoutElement>();

            bool parentIsRow = parent != null && parent.Type == "HBoxContainer";

            Vector2 min = MinimumSize(n);

            // Fixed-size leaves inside a container still carry their authored offsets.
            float authoredW = Mathf.Abs(TscnUi.Prop(n, "offset_right") - TscnUi.Prop(n, "offset_left"));
            float authoredH = Mathf.Abs(TscnUi.Prop(n, "offset_bottom") - TscnUi.Prop(n, "offset_top"));

            float w = min.x > 0.0f ? min.x : authoredW;
            float h = min.y > 0.0f ? min.y : authoredH;

            int hFlags = (int)TscnUi.Prop(n, "size_flags_horizontal", 1.0f);
            int vFlags = (int)TscnUi.Prop(n, "size_flags_vertical", 1.0f);

            float ratio = Mathf.Max(1.0f, TscnUi.Prop(n, "size_flags_stretch_ratio", 1.0f));

            const int Fill = 1;
            const int Expand = 2;

            // ⚠️ A "FIT" PARENT FILLS ITS CHILD REGARDLESS OF FLAGS. A PanelContainer or a
            // MarginContainer in Godot sizes its single child to itself minus its margins; the
            // size flags are a BoxContainer's language and do not apply. Reading the flags here
            // instead left the settings card's contents at their own preferred size, floating in
            // the middle of a panel they were supposed to line.
            bool fitParent = parent != null &&
                             (parent.Type == "PanelContainer" || parent.Type == "MarginContainer" ||
                              parent.Type == "AspectRatioContainer");

            bool hMain = parentIsRow;
            bool hGrows = fitParent || (hMain ? (hFlags & Expand) != 0 : (hFlags & Fill) != 0);

            bool vMain = !parentIsRow;
            bool vGrows = fitParent || (vMain ? (vFlags & Expand) != 0 : (vFlags & Fill) != 0);

            if (w > 0.0f) el.minWidth = w;
            if (h > 0.0f) el.minHeight = h;

            // ⚠️⚠️ FLEXIBLE IS SET EXPLICITLY IN BOTH DIRECTIONS, INCLUDING TO ZERO, AND THAT IS
            // THE WHOLE REASON THE SETTINGS BUTTONS WERE 350 PIXELS TALL. Left unset it is -1,
            // which means "no opinion", and Unity then asks the node's own layout group — which
            // reports a flexible size because ITS children asked to expand across it. A row of
            // three buttons that each fill the row's height therefore demanded every spare pixel
            // in the card. In Godot, FILL across a row means "match the row", never "make the
            // row taller", and only EXPAND claims space from a parent. A LayoutElement outranks a
            // layout group, so writing the zero is what says that.
            el.flexibleWidth = hGrows ? ratio : 0.0f;
            el.flexibleHeight = vGrows ? ratio : 0.0f;

            if (!hGrows && w > 0.0f) el.preferredWidth = w;
            if (!vGrows && h > 0.0f) el.preferredHeight = h;

            // ⚠️ A CONTROL WHOSE ART IS DRAWN BY A CHILD STILL NEEDS A HEIGHT. A CheckBox holds
            // its box and its lettering as children, so the node itself measures as nothing and
            // the rows above and below it close over the top of it. Godot derives these from the
            // theme's own minimum sizes; there is no equivalent to read here, so the floors are
            // the sizes the widgets are actually drawn at.
            if (h <= 0.0f)
            {
                // ⚠️⚠️ A BUTTON COLLAPSES TO NOTHING WITHOUT A FLOOR, AND IT LOOKS LIKE A
                // MISSING CONTROL RATHER THAN A SIZING BUG. Its art is an Image, which reports
                // no preferred height at all, and its caption is a CHILD, so a Button with no
                // authored `custom_minimum_size` measures as zero inside a VBox. The match
                // result screen's REMATCH and MAIN MENU came out as a single 6-pixel bar across
                // the bottom of the card — visible in the capture, and easy to read as "the
                // buttons did not convert".
                //
                // Godot derives this from the theme's own content margins; there is nothing to
                // read here, so the floor is the caption's line height plus the wood face's
                // vertical padding, which is what the control is actually drawn at.
                float buttonFloor = n.Type == "Button" || n.Type == "TextureButton"
                    ? (int)TscnUi.Prop(n, "theme_override_font_sizes/font_size", 30.0f) * 1.35f + 24.0f
                    : 0.0f;

                float floor = n.Type == "CheckBox" ? 34.0f
                            : n.Type == "LineEdit" ? 46.0f
                            : n.Type == "HSlider" ? 34.0f
                            : buttonFloor;

                if (floor > 0.0f)
                {
                    el.minHeight = Mathf.Max(el.minHeight, floor);
                    if (!vGrows) el.preferredHeight = floor;
                }
            }

            // ⚠️ A LABEL SIZES ITSELF FROM ITS TEXT IN BOTH ENGINES, and Unity's Text already
            // reports that through ILayoutElement. Overriding it with a number would cap a
            // caption at whatever the .tscn happened to record, so the override is left off and
            // only a floor is set: a row whose only child is an unwrapped label still needs a
            // height on the very first layout pass, before the font has metrics.
            var text = go.GetComponent<Text>();
            if (text != null && h <= 0.0f)
                el.minHeight = Mathf.Max(el.minHeight, text.fontSize * 1.35f);

            // ⚠️ A CONTAINER'S HEIGHT COMES FROM ITS CONTENTS and Godot never writes it down,
            // but a Unity layout group reports it through ILayoutElement, so nothing needs
            // inventing here. Only a bare Control (a spacer, a clipping box) has neither.
            bool bare = go.GetComponent<LayoutGroup>() == null && text == null &&
                        go.GetComponent<Image>() == null && go.GetComponent<RawImage>() == null;

            if (bare && h <= 0.0f && !vGrows) el.minHeight = Mathf.Max(el.minHeight, 1.0f);
        }

        private static Vector2 MinimumSize(TscnUi.NodeDef n)
        {
            if (!n.Props.TryGetValue("custom_minimum_size", out var raw)) return Vector2.zero;

            var m = Regex.Match(raw, @"Vector2\(([^)]*)\)");
            if (!m.Success) return Vector2.zero;

            var parts = m.Groups[1].Value.Split(',');
            return parts.Length >= 2
                ? new Vector2(TscnUi.F(parts[0]), TscnUi.F(parts[1]))
                : Vector2.zero;
        }

        /// <summary>
        /// ⚠️⚠️ THIS IS THE FIX FOR "THE BUTTONS DON'T WORK, BACK ETC", AND THE CAUSE IS ONE
        /// PROPERTY UNITY DEFAULTS THE WRONG WAY FOR A CONVERTED SCENE.
        ///
        /// Every Godot decorative node in these .tscn files carries `mouse_filter = 2`
        /// (MOUSE_FILTER_IGNORE) — the scrims, the banners, the backdrops, the panel bodies, the
        /// margin containers, all of it. Unity has no equivalent property on the node; it has
        /// `Graphic.raycastTarget`, and it defaults to **true**. So every converted decoration
        /// with an Image on it is a click-eater by default, and a full-screen scrim laid over a
        /// screen swallows every button underneath it while drawing perfectly.
        ///
        /// The individual branches in `ConfigureFromDef` turn it off one node TYPE at a time,
        /// which means the property is only correct for the types somebody remembered. This
        /// sweep inverts the default instead: NOTHING is a raycast target unless it is the
        /// graphic a Selectable actually uses to receive its clicks.
        ///
        /// ⚠️ IT REPORTS THE COUNT. "The importer ran" and "the importer fixed anything" are
        /// different claims, and this whole class of bug is invisible in a screenshot.
        /// </summary>
        private static void ClearStrayRaycastTargets(Transform root)
        {
            int cleared = 0, kept = 0;

            // ⚠⚠ THE LIVE SET IS COLLECTED FROM THE SELECTABLES FIRST, AND IT USED TO BE
            // DECIDED PER GRAPHIC BY ASKING `graphic.GetComponent<Selectable>()`. That question
            // only has the right answer when a control's hit area sits on the control's OWN
            // node, which is true of a Button and false of a Slider: Unity puts a Slider's
            // Background, Fill and Handle on CHILD nodes, so every one of them answered "no
            // Selectable here", every one was muted, and the four settings sliders shipped with
            // no raycast target anywhere beneath them. They drew, they seeded, they wired their
            // listener, and a press at their centre went straight through to the card behind.
            // Walk the Selectables and ask what each one receives through instead.
            var live = new HashSet<Graphic>();

            foreach (var selectable in root.GetComponentsInChildren<Selectable>(includeInactive: true))
            {
                var own = selectable.GetComponent<Graphic>();

                // ⚠️ A Selectable WITH NO targetGraphic STILL NEEDS ONE HIT AREA, or the control
                // converts, skins, wires its listener and is simply not clickable. Give it the
                // graphic on its own node rather than leaving it dead.
                if (selectable.targetGraphic == null) selectable.targetGraphic = own;

                if (selectable.targetGraphic != null) live.Add(selectable.targetGraphic);

                // ⚠️ AND THE GRAPHIC ON THE CONTROL'S OWN NODE IS KEPT WHETHER OR NOT IT IS THE
                // targetGraphic. For a Slider that graphic is the transparent pad `BuildSlider`
                // lays over the whole row, which is the hit area; the targetGraphic stays the
                // Handle, because that is the part that has a pressed colour.
                if (own != null) live.Add(own);
            }

            foreach (var graphic in root.GetComponentsInChildren<Graphic>(includeInactive: true))
            {
                // Anything else on a control — its label, its icon, its shadow — must not receive.
                if (live.Contains(graphic))
                {
                    graphic.raycastTarget = true;
                    kept++;
                    continue;
                }

                if (!graphic.raycastTarget) continue;

                graphic.raycastTarget = false;
                cleared++;
            }

            Report.AppendLine($"   raycast: {kept} live hit areas, {cleared} decorations muted");
        }

        /// <summary>
        /// Is this the viewport that renders an ARENA, as opposed to the one that renders a
        /// character model?
        ///
        /// ⚠️ IT ASKS THE NODE'S OWN NAME, because the .tscn's script reference is an
        /// `ExtResource` id that means nothing outside its own file. `MapPreview` is the setup
        /// screen's node and `CharacterPreview` is the character screen's, and both names are
        /// stable: a ported script reaches them by exactly these strings.
        /// </summary>
        private static bool IsMapViewport(TscnUi.NodeDef n) => n.Name == "MapPreview";

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
                return;

            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // -------------------------------------------------------------------

        private static GameObject BuildNode(TscnUi.NodeDef n, TscnUi.Scene scene, BuildState state,
                                            int depth)
        {
            var go = new GameObject(n.Name);
            go.AddComponent<RectTransform>();

            ConfigureFromDef(go, n, scene, state);
            return go;
        }

        /// <summary>Builds the components a Godot node type needs, and styles them from the theme.</summary>
        private static void ConfigureFromDef(GameObject go, TscnUi.NodeDef n, TscnUi.Scene scene,
                                             BuildState state)
        {
            string variation = Variation(n, scene);

            switch (n.Type)
            {
                case "ColorRect":
                    {
                        var img = Ensure<Image>(go);
                        img.color = TscnUi.ParseColor(
                            n.Props.TryGetValue("color", out var c) ? c : null, Color.white);
                        img.raycastTarget = false;
                        break;
                    }

                case "TextureRect":
                    BuildTextureRect(go, n, scene, state);
                    break;

                // ⚠️ A SubViewport IS A LIVE 3D RENDER, NOT A SPRITE. The setup screen shows the
                // chosen map and the character select spins the actual model. The surface is
                // built here and the camera is wired at runtime by the screen's behaviour.
                // ⚠️ A SubViewport IS A LIVE 3D RENDER, NOT A SPRITE. The setup screen shows the
                // chosen map and the character screen spins the actual model.
                //
                // ⚠️⚠️ AND THE TWO ARE NOT THE SAME VIEWPORT. Godot distinguishes them by the
                // SCRIPT the .tscn attaches — `map_preview.gd` on one, `character_preview.gd` on
                // the other — and this branch attached the MAP surface to every one it found.
                // The character screen therefore rendered an ARENA where the fighter should be:
                // a full-screen close-up of somebody's roof behind the stat card, which is
                // exactly what the capture showed. The model preview is created by
                // `ConvertedCharacterSelect` at runtime, so this node only has to stay out of
                // its way.
                case "SubViewportContainer":
                    {
                        var raw = Ensure<RawImage>(go);
                        raw.color = Color.white;
                        raw.raycastTarget = false;

                        if (IsMapViewport(n)) go.AddComponent<MapPreviewSurface>();
                        else raw.color = new Color(1, 1, 1, 0);

                        break;
                    }

                case "SubViewport":
                case "Camera3D":
                case "ViewportTexture":
                    break;

                case "Label":
                    BuildLabel(go, n, variation);
                    break;

                case "Button":
                    BuildButton(go, n, scene, state, variation, null);
                    break;

                case "TextureButton":
                    BuildButton(go, n, scene, state, variation, "texture_normal");
                    break;

                case "CheckBox":
                    BuildCheckBox(go, n, variation);
                    break;

                case "LineEdit":
                    BuildLineEdit(go, n);
                    break;

                case "HSlider":
                    BuildSlider(go, n);
                    break;

                case "Panel":
                    {
                        var img = Ensure<Image>(go);
                        img.raycastTarget = false;
                        ApplyPanelStyle(go, n, variation, layout: false);
                        break;
                    }

                case "NinePatchRect":
                    {
                        var img = Ensure<Image>(go);
                        img.sprite = LoadSprite(n, scene, state, "texture");
                        img.type = Image.Type.Sliced;
                        img.color = img.sprite != null ? Color.white : UiTheme.WoodDeep;
                        break;
                    }

                // ⚠️⚠️ A PanelContainer IS A CONTAINER, NOT A BACKGROUND. It was treated as a
                // plain image, so it never sized its child and everything below it collapsed to
                // zero. The Godot name says "Panel" first and that is the trap; the word that
                // matters is Container.
                case "PanelContainer":
                    {
                        Ensure<Image>(go);
                        FitContainer(go);
                        ApplyPanelStyle(go, n, variation, layout: true);
                        break;
                    }

                case "ScrollContainer":
                    {
                        var scroll = go.AddComponent<ScrollRect>();
                        scroll.horizontal = false;
                        scroll.vertical = true;
                        scroll.movementType = ScrollRect.MovementType.Clamped;
                        scroll.scrollSensitivity = 32.0f;

                        // ⚠️ THE MASK GOES ON THE SCROLL NODE ITSELF rather than on a viewport
                        // child, because Godot's ScrollContainer has exactly one child and
                        // inserting a wrapper here would break every path a ported script uses
                        // to reach the content.
                        go.AddComponent<RectMask2D>();
                        break;
                    }

                case "CenterContainer":
                    {
                        var g = go.AddComponent<VerticalLayoutGroup>();
                        g.childControlHeight = true;
                        g.childControlWidth = true;
                        g.childForceExpandHeight = false;
                        g.childForceExpandWidth = false;
                        g.childAlignment = TextAnchor.MiddleCenter;
                        break;
                    }

                case "AspectRatioContainer":
                    FitContainer(go);
                    break;

                case "ProgressBar":
                case "TextureProgressBar":
                    {
                        var img = Ensure<Image>(go);
                        img.sprite = LoadSprite(n, scene, state, "texture_progress")
                                     ?? GodotTheme.CardBox(UiTheme.Card, UiTheme.Ink);
                        img.type = Image.Type.Filled;
                        img.fillMethod = Image.FillMethod.Horizontal;
                        img.raycastTarget = false;
                        break;
                    }

                // ⚠️⚠️ GODOT CONTAINERS POSITION THEIR CHILDREN AT RUNTIME. A VBoxContainer's
                // children carry no useful offsets in the .tscn because the container computes
                // them, so converting the container as a plain rect stacks every child at the
                // same corner.
                case "VBoxContainer":
                    {
                        var v = go.AddComponent<VerticalLayoutGroup>();

                        // ⚠️ childControlHeight MUST BE ON. With it off the group does not size
                        // its children vertically, so every row kept a height of ZERO, which
                        // collapsed its own horizontal group and printed "MAP:" over "ESKINITA".
                        v.childControlHeight = true;
                        v.childControlWidth = true;

                        // ⚠️ AND FORCE-EXPAND MUST BE OFF, because per-child size flags decide
                        // who grows. Forcing it overrides SHRINK_BEGIN on the panels and makes
                        // every child the full width of the column.
                        v.childForceExpandHeight = false;
                        v.childForceExpandWidth = false;
                        v.childAlignment = AlignmentOf(n, vertical: true);
                        v.spacing = TscnUi.Prop(n, "theme_override_constants/separation", 4.0f);
                        break;
                    }

                case "HBoxContainer":
                    {
                        var h = go.AddComponent<HorizontalLayoutGroup>();
                        h.childControlHeight = true;
                        h.childControlWidth = true;
                        h.childForceExpandHeight = false;
                        h.childForceExpandWidth = false;
                        h.childAlignment = AlignmentOf(n, vertical: false);
                        h.spacing = TscnUi.Prop(n, "theme_override_constants/separation", 4.0f);
                        break;
                    }

                case "MarginContainer":
                    {
                        var group = FitContainer(go);
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
        }

        private static T Ensure<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        /// <summary>
        /// A Godot "fit" container: PanelContainer, MarginContainer and friends, which size
        /// themselves to their single child rather than laying a run of them out.
        ///
        /// ⚠️⚠️ `childForceExpand` MUST BE OFF AND THAT IS NOT A DETAIL. A Unity layout group
        /// with it ON reports a flexible size of at least 1 to ITS OWN parent, so the flag does
        /// not merely stretch the children — it makes the container itself demand every spare
        /// pixel above it. The setup screen's seat panel was a wood slab running the full height
        /// of the column with four rows at the top of it and 350 px of empty grain underneath,
        /// where the Godot original wraps tightly around the rows. Nothing in the panel asked to
        /// expand; the container asked on its behalf.
        ///
        /// ⚠️ AND TURNING IT OFF DOES NOT BREAK THE PANELS THAT DO NEED TO FILL. A child that
        /// genuinely reports flexible height — the settings list's ScrollContainer carries
        /// `size_flags_vertical = 3` — still receives the surplus, because forceExpand only
        /// imposes a FLOOR of 1 on every child rather than being the mechanism by which surplus
        /// is handed out.
        /// </summary>
        private static VerticalLayoutGroup FitContainer(GameObject go)
        {
            var group = go.AddComponent<VerticalLayoutGroup>();
            group.childControlHeight = true;
            group.childControlWidth = true;
            group.childForceExpandHeight = false;
            group.childForceExpandWidth = false;
            return group;
        }

        private static string Variation(TscnUi.NodeDef n, TscnUi.Scene scene)
        {
            if (n.Props.TryGetValue("theme_type_variation", out var raw))
            {
                // Recorded as `&"WoodPanel"`.
                var m = Regex.Match(raw, "\"([^\"]+)\"");
                if (m.Success) return m.Groups[1].Value;
            }

            return VariationFromStyleBox(n, scene);
        }

        /// <summary>
        /// The variation a hand-written StyleBox is really asking for.
        ///
        /// ⚠️⚠️ A BUTTON CAN NAME ITS LOOK TWO WAYS AND ONLY ONE OF THEM WAS READ. Most controls
        /// carry `theme_type_variation`, but `CharacterSelect.tscn`'s BACK button spells the
        /// same thing out as three `theme_override_styles` StyleBoxFlats. With no variation to
        /// find, it fell through to the theme's plain Button — CARD face, INK border — and came
        /// out WHITE, sitting under a dark brown CHOOSE button on the same panel. Reported as
        /// *"wrong back button colour"*, and it is the only white control on the screen.
        ///
        /// ⚠️ MATCHED ON THE COLOURS RATHER THAN SPECIAL-CASED BY NODE NAME. The .tscn's
        /// `bg_color` is 0.192/0.098/0.043 and its `border_color` is 0.545/0.322/0.153, which
        /// are WoodDeep and WoodEdge to five decimal places: the author was writing out the wood
        /// button by hand. Matching the palette means any other control that does the same gets
        /// the right skin without a second special case here.
        /// </summary>
        private static string VariationFromStyleBox(TscnUi.NodeDef n, TscnUi.Scene scene)
        {
            if (scene == null) return null;
            if (!n.Props.TryGetValue("theme_override_styles/normal", out var raw)) return null;

            string id = TscnUi.SubId(raw);
            if (id == null || !scene.Sub.TryGetValue(id, out var box)) return null;
            if (box.Type != "StyleBoxFlat") return null;

            var fill = TscnUi.ParseColor(box.Props.TryGetValue("bg_color", out var bg) ? bg : null,
                                         Color.clear);

            foreach (string candidate in new[] { "WoodButton", "WoodPrimaryButton",
                                                 "WoodDangerButton", "PrimaryButton" })
            {
                var style = GodotTheme.ForButton(candidate);
                if (Near(style.Fill, fill)) return candidate;
            }

            return null;
        }

        /// <summary>Godot writes these to three decimals, so an exact compare never matches.</summary>
        private static bool Near(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < 0.004f && Mathf.Abs(a.g - b.g) < 0.004f &&
            Mathf.Abs(a.b - b.b) < 0.004f;

        /// <summary>
        /// Godot's BoxContainer `alignment`: 0 begin, 1 centre, 2 end. It positions the whole
        /// run of children along the container's own axis.
        /// </summary>
        private static TextAnchor AlignmentOf(TscnUi.NodeDef n, bool vertical)
        {
            int a = (int)TscnUi.Prop(n, "alignment", 0.0f);

            if (vertical)
                return a == 1 ? TextAnchor.MiddleLeft : (a == 2 ? TextAnchor.LowerLeft : TextAnchor.UpperLeft);

            // A row centres its children vertically by default, which is what Godot's FILL
            // plus SHRINK_CENTER combination produces on every selector row in the game.
            return a == 1 ? TextAnchor.MiddleCenter : (a == 2 ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft);
        }

        private static void ApplyPanelStyle(GameObject go, TscnUi.NodeDef n, string variation,
                                            bool layout)
        {
            // A scene may still override the whole StyleBox by hand, in which case the flat
            // colour it names wins over the variation.
            if (n.Props.TryGetValue("theme_override_styles/panel", out var raw))
            {
                string id = TscnUi.SubId(raw);
                var img = go.GetComponent<Image>();

                if (id != null && img != null)
                {
                    img.sprite = null;
                    img.color = new Color(0, 0, 0, 1);
                    return;
                }
            }

            var skin = go.AddComponent<GodotPanel>();
            skin.Variation = variation ?? "Card";
            skin.ApplyContentMargins = layout;
            skin.Apply();
        }

        // -------------------------------------------------------------------

        private static void BuildTextureRect(GameObject go, TscnUi.NodeDef n, TscnUi.Scene scene,
                                             BuildState state)
        {
            // ⚠️⚠️ NO TEXTURE MEANS NO IMAGE, NOT A MAGENTA ONE. A TextureRect whose texture is
            // assigned in code has no `texture` line in the .tscn at all, and marking those
            // magenta filled the setup screen with a full-bleed pink rect that hid every control.
            if (!n.Props.ContainsKey("texture")) return;

            // ⚠️ A GENERATED TEXTURE IS NOT A MISSING ONE. The scrim behind every panel is a
            // GradientTexture2D built in the scene, so it is a SubResource with no file behind
            // it. It is rebuilt here for real rather than flattened to its first stop: the
            // MatchSetup scrim runs dark-clear-dark across the screen, and a flat tint at 0.88
            // alpha buries the map preview it exists to sit over.
            if (TscnUi.SubId(n.Props["texture"]) != null)
            {
                var scrim = Ensure<Image>(go);
                scrim.sprite = GradientSprite(n, scene);
                scrim.color = Color.white;
                scrim.raycastTarget = false;
                scrim.type = Image.Type.Simple;
                return;
            }

            var img = Ensure<Image>(go);
            img.sprite = LoadSprite(n, scene, state, "texture");
            img.color = img.sprite != null ? Color.white : Color.magenta;
            img.raycastTarget = false;
            img.type = Image.Type.Simple;

            // Godot stretch_mode: 0 scale (distort to fit), 5 keep-aspect-centred, 6 covered.
            int stretch = (int)TscnUi.Prop(n, "stretch_mode", 0.0f);

            img.preserveAspect = stretch == 5;

            if (stretch == 6 && img.sprite != null)
            {
                // ⚠️ "COVERED" CROPS, IT DOES NOT LETTERBOX. The menu backdrop is a photograph
                // that must fill the screen at any aspect; preserving it inside the rect leaves
                // navy bars down the sides of a 21:9 monitor.
                var fitter = go.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = img.sprite.rect.width / Mathf.Max(1.0f, img.sprite.rect.height);
            }
        }

        private static void BuildLabel(GameObject go, TscnUi.NodeDef n, string variation)
        {
            string content = TscnUi.Str(n, "text") ?? "";

            GodotTheme.TryText(variation, out var style);

            // A per-node override still wins over the variation, exactly as Godot resolves it.
            int size = (int)TscnUi.Prop(n, "theme_override_font_sizes/font_size", style.Size);

            var colour = n.Props.TryGetValue("theme_override_colors/font_color", out var fc)
                ? TscnUi.ParseColor(fc, style.Colour)
                : style.Colour;

            var text = MakeText(go, content, size, colour, TscnUi.Align(n));
            text.raycastTarget = false;

            // ⚠️ AUTOWRAP IS A LAYOUT PROPERTY, NOT A COSMETIC ONE. The map blurbs and the
            // tutorial body are authored to wrap inside a fixed box; left overflowing they run
            // off the side of the screen and over whatever is next to them.
            bool wrap = (int)TscnUi.Prop(n, "autowrap_mode", 0.0f) > 0;
            text.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            int outline = (int)TscnUi.Prop(n, "theme_override_constants/outline_size", style.Outline);
            if (outline <= 0) return;

            var oc = n.Props.TryGetValue("theme_override_colors/font_outline_color", out var o)
                ? TscnUi.ParseColor(o, style.OutlineColour)
                : style.OutlineColour;

            if (oc.a <= 0.0f) return;

            var ring = go.AddComponent<GodotOutline>();
            ring.OutlineColour = oc;

            // Godot's outline_size is a radius in pixels and so is this, but a Text mesh is
            // grown by whole copies rather than by dilating the glyph, so half the radius reads
            // as the same weight without closing up the counters at 16px.
            ring.Radius = Mathf.Max(1.0f, outline * 0.5f);
        }

        private static Text MakeText(GameObject go, string content, int size, Color colour,
                                     TextAnchor align)
        {
            var t = Ensure<Text>(go);

            t.font = _font;
            t.text = content;
            t.fontSize = Mathf.Max(1, size);
            t.color = colour;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;

            // ⚠️⚠️ THIS ONE FLAG IS GODOT'S `BASELINE_OFFSET`. Darumadrop is a Japanese face and
            // reserves a deep descent for kana, so its ascent:descent split is about 4:1. Both
            // engines centre the LINE BOX rather than the ink, which leaves an all-caps Latin
            // string sitting ~13% of the font size below the optical centre of its box. Godot
            // corrects it globally with a FontVariation; Unity has no such knob, but
            // `alignByGeometry` measures the glyphs instead of the metrics, which is the same
            // correction. Without it every string in the game reads as sitting low and it gets
            // blamed on the font.
            t.alignByGeometry = true;

            return t;
        }

        private static void BuildButton(GameObject go, TscnUi.NodeDef n, TscnUi.Scene scene,
                                        BuildState state, string variation, string textureKey)
        {
            var img = Ensure<Image>(go);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var sprite = textureKey != null ? LoadSprite(n, scene, state, textureKey) : null;

            bool flat = n.Props.TryGetValue("flat", out var f) && f.Trim() == "true";

            if (sprite != null)
            {
                // A TextureButton draws its own art and takes no StyleBox at all.
                img.sprite = sprite;
                img.color = Color.white;
                img.type = Image.Type.Simple;
                img.preserveAspect = (int)TscnUi.Prop(n, "stretch_mode", 0.0f) == 5;

                go.AddComponent<TextureButtonFeedback>();
            }
            else if (flat)
            {
                // `flat = true` is the ArrowButton root: the artwork child draws everything and
                // the Button itself is only a hit area. It still has to be raycastable.
                img.color = new Color(0, 0, 0, 0);
            }
            else
            {
                // ⚠️ APPLIED AGAIN AFTER THE VARIATION IS SET: AddComponent already ran OnEnable
                // with the field's default, so the assignment alone changes nothing.
                var skin = go.AddComponent<GodotButton>();
                skin.Variation = variation ?? "Button";
                skin.Apply();
                skin.Refresh();
            }

            if (n.Props.TryGetValue("disabled", out var d) && d.Trim() == "true")
                btn.interactable = false;

            string caption = TscnUi.Str(n, "text");
            if (string.IsNullOrEmpty(caption)) return;

            var style = GodotTheme.ForButton(variation);
            int size = (int)TscnUi.Prop(n, "theme_override_font_sizes/font_size", style.FontSize);

            var labelGo = new GameObject("Label");
            labelGo.AddComponent<RectTransform>();
            labelGo.transform.SetParent(go.transform, false);

            // ⚠️ A BUTTON'S `alignment` IS ITS TEXT ALIGNMENT: 0 left, 1 centre, 2 right. The
            // seat rows on the setup screen are left-aligned on purpose so "P1 · TAYA FIRST"
            // starts at the same x as the three below it; centring them makes four rows of
            // different-length text zigzag down the panel.
            int align = (int)TscnUi.Prop(n, "alignment", 1.0f);
            var anchor = align == 0 ? TextAnchor.MiddleLeft
                       : (align == 2 ? TextAnchor.MiddleRight : TextAnchor.MiddleCenter);

            var t = MakeText(labelGo, caption, size, style.Ink, anchor);
            t.raycastTarget = false;

            var rt = t.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;

            // The StyleBox content margins, so left-aligned lettering clears the border.
            var pad = GodotTheme.ContentMargins(style.Wood, false);
            rt.offsetMin = new Vector2(pad.left, pad.bottom);
            rt.offsetMax = new Vector2(-pad.right, -pad.top);
        }

        private static void BuildCheckBox(GameObject go, TscnUi.NodeDef n, string variation)
        {
            // Godot's CheckBox theme is a transparent box plus the tick, so only the lettering
            // and the tick need drawing here.
            GodotTheme.TryText(variation ?? "MenuCheckBox", out var style);

            var toggle = go.AddComponent<Toggle>();

            var boxGo = new GameObject("Box");
            boxGo.AddComponent<RectTransform>();
            boxGo.transform.SetParent(go.transform, false);

            var box = boxGo.AddComponent<Image>();
            box.sprite = GodotTheme.CardBox(UiTheme.Card, UiTheme.Ink);
            box.type = Image.Type.Sliced;

            var boxRt = box.rectTransform;
            boxRt.anchorMin = new Vector2(0.0f, 0.5f);
            boxRt.anchorMax = new Vector2(0.0f, 0.5f);
            boxRt.pivot = new Vector2(0.0f, 0.5f);
            boxRt.anchoredPosition = new Vector2(0.0f, 0.0f);
            boxRt.sizeDelta = new Vector2(30.0f, 30.0f);

            var tickGo = new GameObject("Tick");
            tickGo.AddComponent<RectTransform>();
            tickGo.transform.SetParent(boxGo.transform, false);

            var tick = tickGo.AddComponent<Image>();
            tick.sprite = GodotTheme.Plain(3);
            tick.type = Image.Type.Sliced;
            tick.color = UiTheme.Impact;

            var tickRt = tick.rectTransform;
            tickRt.anchorMin = Vector2.zero;
            tickRt.anchorMax = Vector2.one;
            tickRt.offsetMin = new Vector2(7.0f, 7.0f);
            tickRt.offsetMax = new Vector2(-7.0f, -7.0f);

            toggle.targetGraphic = box;
            toggle.graphic = tick;
            toggle.isOn = n.Props.TryGetValue("button_pressed", out var p) && p.Trim() == "true";

            string caption = TscnUi.Str(n, "text");
            if (string.IsNullOrEmpty(caption)) return;

            var labelGo = new GameObject("Label");
            labelGo.AddComponent<RectTransform>();
            labelGo.transform.SetParent(go.transform, false);

            var t = MakeText(labelGo, caption, style.Size, style.Colour, TextAnchor.MiddleLeft);
            t.raycastTarget = false;

            var rt = t.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(42.0f, 0.0f);
            rt.offsetMax = Vector2.zero;
        }

        private static void BuildLineEdit(GameObject go, TscnUi.NodeDef n)
        {
            var bg = Ensure<Image>(go);
            bg.sprite = GodotTheme.CardBox(UiTheme.WoodDark, UiTheme.WoodEdge);
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;

            var textGo = new GameObject("Text");
            textGo.AddComponent<RectTransform>();
            textGo.transform.SetParent(go.transform, false);

            // ⚠️ CENTRED, LIKE EVERY OTHER VALUE IN THE SETTINGS LIST. A LineEdit defaults to
            // left-aligned while every Button under it centres its label, which put one box in a
            // column of fourteen hard against the left edge. It was reported.
            int align = (int)TscnUi.Prop(n, "alignment", 0.0f);
            var anchor = align == 1 ? TextAnchor.MiddleCenter
                       : (align == 2 ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft);

            var text = MakeText(textGo, "", 21, UiTheme.Cream, anchor);
            var textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(12.0f, 4.0f);
            textRt.offsetMax = new Vector2(-12.0f, -4.0f);

            var placeholderGo = new GameObject("Placeholder");
            placeholderGo.AddComponent<RectTransform>();
            placeholderGo.transform.SetParent(go.transform, false);

            var placeholder = MakeText(placeholderGo, TscnUi.Str(n, "placeholder_text") ?? "",
                                       21, UiTheme.CreamMuted, anchor);
            var phRt = placeholder.rectTransform;
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.offsetMin = new Vector2(12.0f, 4.0f);
            phRt.offsetMax = new Vector2(-12.0f, -4.0f);

            var field = go.AddComponent<InputField>();
            field.textComponent = text;
            field.placeholder = placeholder;
            field.targetGraphic = bg;
            field.characterLimit = (int)TscnUi.Prop(n, "max_length", 0.0f);
            field.text = TscnUi.Str(n, "text") ?? "";
        }

        private static void BuildSlider(GameObject go, TscnUi.NodeDef n)
        {
            var slider = go.AddComponent<Slider>();

            // -------------------------------------------------------------------
            // ⚠️⚠️ THE WHOLE CONTROL IS A RAYCAST TARGET, AND WITHOUT THIS THE SLIDERS WERE
            // BARELY DRAGGABLE. 🧑 2026-08-27, two reports that are one bug: *"sound and volume
            // dont decrease theyre hard locked"* and *"awkward scrolling motion for settings
            // (repeated problem)"*.
            //
            // ⚠️⚠️ THE SLIDER'S ROOT HAD NO GRAPHIC ON IT AT ALL. Unity's EventSystem raycasts to
            // find a GRAPHIC and then walks UP the hierarchy for something that handles the drag,
            // so the only parts of a slider that could start one were the 14 px `Background`
            // strip and the 22 by 34 px `Handle`. Every other pixel of the row hit nothing, the
            // event carried on up to the `ScrollRect` that these rows live in, and the list
            // scrolled instead. That is both symptoms exactly: a slider that mostly refuses to
            // move, and a panel that scrolls when you did not ask it to.
            //
            // ⚠️ IT IS INVISIBLE AND IT IS NOT DECORATION. Alpha 0 with `raycastTarget` true is
            // the standard way to say "this rectangle belongs to this control"; anything visible
            // here would draw a box behind every slider in the game.
            //
            // ⚠️ AND IT IS ADDED BEFORE THE CHILDREN so it sits at the BACK of the draw order.
            // In front, it would swallow the handle's own hit test and the grab would stop
            // working for the opposite reason.
            var hit = go.AddComponent<Image>();
            hit.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            hit.raycastTarget = true;
            // ⚠⚠ AND IT ONLY SURVIVES BECAUSE `ClearStrayRaycastTargets` WAS FIXED IN THE SAME
            // BATCH. That sweep asked `graphic.GetComponent<Selectable>()` and kept a graphic
            // only when it WAS the Selectable's `targetGraphic`. This pad is on the slider's own
            // node, so the question found the Slider, but the answer was the Handle: the pad was
            // muted a few milliseconds after it was made, on the same run. `SettingsPanel.prefab`
            // regenerated with the pad in place still shipped **4 live raycast targets and 54
            // muted**, the same four Buttons as before. The sweep now collects the live set from
            // the Selectables instead.

            var bgGo = new GameObject("Background");
            bgGo.AddComponent<RectTransform>();
            bgGo.transform.SetParent(go.transform, false);

            var bg = bgGo.AddComponent<Image>();
            bg.sprite = GodotTheme.CardBox(UiTheme.WoodDark, UiTheme.WoodEdge);
            bg.type = Image.Type.Sliced;
            Band(bg.rectTransform, 14.0f);

            var fillArea = new GameObject("Fill Area");
            fillArea.AddComponent<RectTransform>();
            fillArea.transform.SetParent(go.transform, false);
            Band(fillArea.GetComponent<RectTransform>(), 14.0f);

            var fillGo = new GameObject("Fill");
            fillGo.AddComponent<RectTransform>();
            fillGo.transform.SetParent(fillArea.transform, false);

            var fill = fillGo.AddComponent<Image>();
            // The grabber area takes DEFENSE in the Godot theme, which is the one blue in the
            // palette that means something; it is the fill of a slider there and stays that here.
            fill.sprite = GodotTheme.CardBox(UiTheme.Defense, UiTheme.Ink);
            fill.type = Image.Type.Sliced;

            var fillRt = fill.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            var handleArea = new GameObject("Handle Slide Area");
            handleArea.AddComponent<RectTransform>();
            handleArea.transform.SetParent(go.transform, false);

            var handleAreaRt = handleArea.GetComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(11.0f, 0.0f);
            handleAreaRt.offsetMax = new Vector2(-11.0f, 0.0f);

            var handleGo = new GameObject("Handle");
            handleGo.AddComponent<RectTransform>();
            handleGo.transform.SetParent(handleArea.transform, false);

            var handle = handleGo.AddComponent<Image>();
            handle.sprite = GodotTheme.CardBox(UiTheme.Cream, UiTheme.Ink);
            handle.type = Image.Type.Sliced;

            var handleRt = handle.rectTransform;
            handleRt.anchorMin = new Vector2(0.0f, 0.5f);
            handleRt.anchorMax = new Vector2(0.0f, 0.5f);
            handleRt.pivot = new Vector2(0.5f, 0.5f);
            handleRt.sizeDelta = new Vector2(22.0f, 34.0f);

            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;

            slider.minValue = TscnUi.Prop(n, "min_value", 0.0f);
            slider.maxValue = TscnUi.Prop(n, "max_value", 1.0f);
            slider.value = TscnUi.Prop(n, "value", slider.minValue);

            float step = TscnUi.Prop(n, "step", 0.0f);
            slider.wholeNumbers = step >= 1.0f;
        }

        /// <summary>A horizontal band centred in the control, for the slider's groove.</summary>
        private static void Band(RectTransform rt, float height)
        {
            rt.anchorMin = new Vector2(0.0f, 0.5f);
            rt.anchorMax = new Vector2(1.0f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0.0f, height);
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// Applies an ArrowButton instance's exported properties to the inlined sub-scene.
        ///
        /// ⚠️ THE CAPTION IS A REAL LABEL, NOT THE BUTTON'S OWN TEXT, and that is authored: it
        /// has to be rotatable so it follows the pennant's slant, which a Button's built-in text
        /// cannot do. It is also indented into the arrow body rather than centred, because the
        /// artwork's tip has no room for lettering.
        /// </summary>
        private static void ConfigureArrowButton(GameObject go, TscnUi.NodeDef n,
                                                 TscnUi.Scene scene, BuildState state)
        {
            var artwork = go.transform.Find("Artwork");
            var caption = go.transform.Find("Caption");

            if (artwork != null)
            {
                var img = Ensure<Image>(artwork.gameObject);
                img.sprite = LoadSprite(n, scene, state, "texture");
                img.color = img.sprite != null ? Color.white : Color.magenta;
                img.raycastTarget = false;
                img.type = Image.Type.Simple;
            }

            var view = go.AddComponent<ArrowButtonView>();
            view.PoleDistance = TscnUi.Prop(n, "pole_distance", 420.0f);
            view.LeftOffset = TscnUi.Prop(n, "offset_left", 0.0f);

            if (caption == null) return;

            string words = TscnUi.Str(n, "caption") ?? "";
            int size = (int)TscnUi.Prop(n, "label_size", 72.0f);

            var colour = n.Props.TryGetValue("text_color", out var tc)
                ? TscnUi.ParseColor(tc, UiTheme.Ink)
                : new Color(0.133f, 0.102f, 0.063f, 1.0f);   // arrow_button.gd's #221a10 default

            var text = MakeText(caption.gameObject, words, size, colour, TextAnchor.MiddleCenter);
            text.raycastTarget = false;

            float indent = TscnUi.Prop(n, "text_indent", 70.0f);
            float tip = TscnUi.Prop(n, "tip_padding", 96.0f);
            float offsetY = TscnUi.Prop(n, "text_offset_y", 0.0f);

            var rt = text.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;

            // Godot: offset_top = max(0, y*2), offset_bottom = min(0, y*2), and Y is flipped.
            rt.offsetMin = new Vector2(indent, -Mathf.Min(0.0f, offsetY * 2.0f));
            rt.offsetMax = new Vector2(-tip, -Mathf.Max(0.0f, offsetY * 2.0f));

            // Degrees clockwise in Godot; Unity's Z rotation is anticlockwise.
            float slant = TscnUi.Prop(n, "label_rotation", 0.0f);
            if (Mathf.Abs(slant) > 0.001f)
                rt.localRotation = Quaternion.Euler(0.0f, 0.0f, -slant);

            float stretchX = TscnUi.Prop(n, "label_stretch_x", 1.0f);
            float stretchY = TscnUi.Prop(n, "label_stretch_y", 1.0f);

            if (Mathf.Abs(stretchX - 1.0f) > 0.001f || Mathf.Abs(stretchY - 1.0f) > 0.001f)
                rt.localScale = new Vector3(stretchX, stretchY, 1.0f);
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// Rebuilds a Godot GradientTexture2D as a real sprite.
        ///
        /// ⚠️ THE SCRIM IS LOAD-BEARING, NOT DECORATION, AND ITS SHAPE MATTERS. Every panel sits
        /// over a photographic backdrop or a live map render. The setup screen's scrim runs
        /// dark, clear, dark from left to right so the panels on either side stay readable while
        /// the map in the middle stays visible. Flattening it to its first stop puts an 88%
        /// navy sheet over the whole screen and hides the thing it exists to sit over.
        /// </summary>
        private static Sprite GradientSprite(TscnUi.NodeDef n, TscnUi.Scene scene)
        {
            var stops = new List<KeyValuePair<float, Color>>();
            bool vertical = false;

            string texId = TscnUi.SubId(n.Props["texture"]);

            if (texId != null && scene.Sub.TryGetValue(texId, out var tex))
            {
                // `fill_from` / `fill_to` decide the direction; the default runs left to right.
                if (tex.Props.TryGetValue("fill_to", out var to))
                {
                    var m = Regex.Match(to, @"Vector2\(([^)]*)\)");
                    if (m.Success)
                    {
                        var parts = m.Groups[1].Value.Split(',');
                        if (parts.Length >= 2 && Mathf.Abs(TscnUi.F(parts[1])) >
                                                 Mathf.Abs(TscnUi.F(parts[0])))
                            vertical = true;
                    }
                }

                if (tex.Props.TryGetValue("gradient", out var gradRef))
                {
                    string gid = TscnUi.SubId(gradRef);

                    if (gid != null && scene.Sub.TryGetValue(gid, out var grad))
                    {
                        var offsets = Floats(grad, "offsets");
                        var colours = Floats(grad, "colors");

                        for (int i = 0; i < offsets.Count && (i + 1) * 4 <= colours.Count; i++)
                        {
                            stops.Add(new KeyValuePair<float, Color>(offsets[i], new Color(
                                colours[i * 4 + 0], colours[i * 4 + 1],
                                colours[i * 4 + 2], colours[i * 4 + 3])));
                        }
                    }
                }
            }

            if (stops.Count == 0)
                stops.Add(new KeyValuePair<float, Color>(0.0f, new Color(0, 0, 0, 0.45f)));

            const int steps = 256;
            int w = vertical ? 1 : steps;
            int h = vertical ? steps : 1;

            var pixels = new Color[steps];

            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)(steps - 1);
                // Godot's texture Y runs top-down and Unity's runs bottom-up.
                pixels[i] = Sample(stops, vertical ? 1.0f - t : t);
            }

            return BakeGradient(pixels, w, h, vertical);
        }

        /// <summary>
        /// Writes a gradient out as a real sprite asset and returns it.
        ///
        /// ⚠️⚠️ IT CANNOT BE A RUNTIME TEXTURE, AND THAT CRASHED THE BUILT GAME. A `Sprite`
        /// created with `Sprite.Create` is not an asset, so assigning one to an Image and saving
        /// the scene writes a reference to an object the scene does not own. The editor is
        /// perfectly happy: the scene opens, the screens photograph correctly, every test
        /// passes. The PLAYER then reports `The file 'level1' is corrupted!` and dies on the
        /// first scene load, before it has drawn one frame of the menu.
        ///
        /// ⚠️ AND THE SAME RULE COVERS THE STYLE BOXES, which is why `StyleBoxBaker` exists.
        /// Anything a scene points at has to live on disk.
        /// </summary>
        private static Sprite BakeGradient(Color[] pixels, int w, int h, bool vertical)
        {
            const string dir = "Assets/TumbangPreso/Art/ui/generated";
            Directory.CreateDirectory(dir);

            // Named for its contents, so two screens with the same scrim share one asset and a
            // changed gradient writes a new file rather than silently reusing the old one.
            int hash = 17;
            foreach (var c in pixels) hash = hash * 31 + c.GetHashCode();

            string path = $"{dir}/scrim_{(vertical ? "v" : "h")}_{(uint)hash:x8}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply(false, false);

            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static List<float> Floats(TscnUi.SubRes res, string key)
        {
            var values = new List<float>();
            if (!res.Props.TryGetValue(key, out var raw)) return values;

            var m = Regex.Match(raw, @"\(([^)]*)\)");
            if (!m.Success) return values;

            foreach (var part in m.Groups[1].Value.Split(','))
                values.Add(TscnUi.F(part));

            return values;
        }

        private static Color Sample(List<KeyValuePair<float, Color>> stops, float t)
        {
            if (stops.Count == 1) return stops[0].Value;

            for (int i = 0; i < stops.Count - 1; i++)
            {
                if (t > stops[i + 1].Key) continue;

                float span = Mathf.Max(0.0001f, stops[i + 1].Key - stops[i].Key);
                float k = Mathf.Clamp01((t - stops[i].Key) / span);
                return Color.Lerp(stops[i].Value, stops[i + 1].Value, k);
            }

            return stops[stops.Count - 1].Value;
        }

        private static Sprite LoadSprite(TscnUi.NodeDef n, TscnUi.Scene scene, BuildState state,
                                         string key)
        {
            if (!n.Props.TryGetValue(key, out var raw)) return null;

            string id = TscnUi.ExtId(raw);
            if (id == null || !scene.Ext.TryGetValue(id, out var res)) return null;

            string assetPath = TscnUi.ToAssetPath(res.Path, ArtRoot);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

            if (sprite == null)
            {
                // ⚠️ A PNG IMPORTS AS A TEXTURE BY DEFAULT, NOT A SPRITE, so the first load
                // returns null even though the file is right there.
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
                state.Missing++;
            }

            return sprite;
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// A Unity ScrollRect needs its content wired after the children exist.
        ///
        /// ⚠️ AND THE CONTENT MUST BE TOP-ANCHORED WITH A FITTER, or it sits centred in the
        /// viewport and the first row is already scrolled off the top before the player touches
        /// anything.
        /// </summary>
        private static void FinishScrollRects(Transform root)
        {
            foreach (var scroll in root.GetComponentsInChildren<ScrollRect>(true))
            {
                scroll.viewport = scroll.GetComponent<RectTransform>();

                Transform content = null;

                for (int i = 0; i < scroll.transform.childCount; i++)
                {
                    var child = scroll.transform.GetChild(i);
                    if (child.GetComponent<LayoutGroup>() == null) continue;

                    content = child;
                    break;
                }

                if (content == null) continue;

                var rt = content.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.0f, 1.0f);
                rt.anchorMax = new Vector2(1.0f, 1.0f);
                rt.pivot = new Vector2(0.5f, 1.0f);
                rt.offsetMin = new Vector2(0.0f, rt.offsetMin.y);
                rt.offsetMax = new Vector2(0.0f, 0.0f);

                var fitter = content.GetComponent<ContentSizeFitter>();
                if (fitter == null) fitter = content.gameObject.AddComponent<ContentSizeFitter>();

                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var element = content.GetComponent<LayoutElement>();
                if (element != null) element.ignoreLayout = true;

                scroll.content = rt;
            }
        }

        /// <summary>
        /// Repoints every component at the real `.cs` asset, and says so if one cannot be found.
        ///
        /// ⚠️⚠️ THIS IS WHY THE BUILT GAME DIED ON ITS FIRST SCENE. Unity can only produce a
        /// MonoScript asset for the class whose name matches the FILE name. A second
        /// MonoBehaviour in the same file has no asset to point at, so `AddComponent` serialises
        /// an EMBEDDED MonoScript stub instead: the saved scene grows a `--- !u!115` block with
        /// an empty name and the behaviour references it by local id. The editor resolves that
        /// fine, so every scene opened, every screen photographed and every test passed, while
        /// the player reported `The file 'level1' is corrupted!` and crashed before drawing a
        /// frame. SplashScreen was the one converted scene with no components on its canvas,
        /// which is exactly why level0 loaded and level1 did not.
        ///
        /// The fix is one class per file; this is the check that keeps it that way.
        /// </summary>
        private static bool ScriptsResolve(Transform root)
        {
            bool ok = true;

            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null) continue;

                var so = new SerializedObject(behaviour);
                var slot = so.FindProperty("m_Script");

                if (slot == null) continue;
                if (slot.objectReferenceValue is MonoScript current &&
                    AssetDatabase.Contains(current))
                {
                    continue;
                }

                var script = FindScript(behaviour.GetType());

                if (script == null)
                {
                    Report.AppendLine($"      FAIL: no MonoScript asset for " +
                                      $"{behaviour.GetType().FullName}. Move that class into a " +
                                      "file of its own name, or the scene ships with an embedded " +
                                      "stub and refuses to load.");
                    ok = false;
                    continue;
                }

                slot.objectReferenceValue = script;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            return ok;
        }

        private static readonly Dictionary<Type, MonoScript> ScriptCache =
            new Dictionary<Type, MonoScript>();

        private static MonoScript FindScript(Type type)
        {
            if (ScriptCache.TryGetValue(type, out var cached)) return cached;

            MonoScript found = null;

            foreach (var guid in AssetDatabase.FindAssets($"{type.Name} t:MonoScript"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);

                if (script == null || script.GetClass() != type) continue;

                found = script;
                break;
            }

            ScriptCache[type] = found;
            return found;
        }

        /// <summary>
        /// Refuses to save a scene that points at anything not on disk.
        ///
        /// ⚠️ A sprite made with `Sprite.Create` is not an asset, so a scene holding one saves a
        /// reference to an object it does not own, and the built player calls that scene
        /// corrupted. A failure here means a style box is missing from the bake or a gradient
        /// was not written out, both of which are one-line fixes once you know which sprite.
        /// </summary>
        private static bool NothingRuntimeOnly(Transform root)
        {
            bool ok = true;

            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite == null) continue;
                if (AssetDatabase.Contains(image.sprite)) continue;

                Report.AppendLine($"      FAIL: '{image.name}' uses the runtime-only sprite " +
                                  $"'{image.sprite.name}'. Saving that corrupts the built scene. " +
                                  "Run Tumbang Preso > Bake UI Style Boxes and re-import.");
                ok = false;
            }

            foreach (var raw in root.GetComponentsInChildren<RawImage>(true))
            {
                if (raw.texture == null || AssetDatabase.Contains(raw.texture)) continue;

                Report.AppendLine($"      FAIL: '{raw.name}' uses a runtime-only texture.");
                ok = false;
            }

            return ok;
        }

        /// <summary>
        /// Binds the behaviour for a sub-scene instanced INTO a screen.
        ///
        /// ⚠️⚠️ THESE ARE WHERE HALF THE FRONT END LIVES. MainMenu instances SettingsPanel
        /// and CreditsPanel as hidden children and MatchSetup instances the whole
        /// character screen; the Godot scripts show them in place rather than switching scene.
        /// Without a behaviour on each, the panels convert perfectly and do nothing at all,
        /// which is what pushed the last pass into hand-drawing replacements in C#.
        /// </summary>
        private static void AttachNestedPanels(BuildState state)
        {
            foreach (var pair in state.ByPath)
            {
                var go = pair.Value.gameObject;

                switch (go.name)
                {
                    case "SettingsPanel":
                        Bind<ConvertedSettingsPanel>(go);
                        ExportPanelPrefab(go, "SettingsPanel");
                        break;

                    // ⚠️ `TutorialPanel` HAD A CASE HERE AND THE PANEL IS DELETED. The six-page
                    // reference card was replaced by the playable route on 2026-08-28; see
                    // `ConvertedMainMenu.Wire`. Re-adding a bind for it would need the class back.
                    case "CreditsPanel":
                        Bind<ConvertedCreditsPanel>(go);
                        break;

                    case "CharacterSelectPanel":
                        Bind<ConvertedCharacterSelect>(go);
                        break;
                }
            }
        }

        private static void Bind<T>(GameObject go) where T : MonoBehaviour
        {
            if (go.GetComponent<T>() != null) return;

            go.AddComponent<T>();
            Report.AppendLine($"      bound {typeof(T).Name} to {go.name}");
        }

        /// <summary>
        /// ⚠️ THE PAUSE MENU OPENS THE SAME SETTINGS PANEL THE TITLE SCREEN DOES, and it is in
        /// a different scene, so it needs a prefab to instantiate. Two separately built panels
        /// drift the moment one of them gains a row.
        /// </summary>
        private static void ExportPanelPrefab(GameObject go, string name)
        {
            const string dir = "Assets/TumbangPreso/Resources/UI";
            Directory.CreateDirectory(dir);

            bool wasActive = go.activeSelf;
            go.SetActive(true);

            PrefabUtility.SaveAsPrefabAsset(go, $"{dir}/{name}.prefab", out bool ok);

            go.SetActive(wasActive);

            Report.AppendLine(ok
                ? $"      wrote Resources/UI/{name}.prefab"
                : $"      FAILED to write Resources/UI/{name}.prefab");
        }

        /// <summary>
        /// ⚠️ THE VERSION STAMP IS IN THE CORNER OF EVERY SCREEN IN THE GODOT BUILD, added by
        /// `GameVersion.attach_to()` rather than authored per scene. Screenshots go to sponsors
        /// with that number on them, and the converted screens had none at all.
        /// </summary>
        private static void StampVersion(string screenName, Transform canvas)
        {
            if (screenName == "SplashScreen" || screenName == "HUD") return;

            var go = new GameObject("VersionLabel");
            go.AddComponent<RectTransform>();
            go.transform.SetParent(canvas, false);

            var text = MakeText(go, "v" + PlayerSettings.bundleVersion, 18,
                                new Color(1, 1, 1, 0.5f), TextAnchor.LowerRight);
            text.raycastTarget = false;

            // ⚠️ BOTTOM-RIGHT, WHICH IS WHERE `GameVersion.attach_to` PUTS IT: preset
            // BOTTOM_RIGHT, inset 24 from the right edge and 20 from the bottom. Unity's (1,1)
            // anchor is the TOP right, so the obvious transcription puts it in the wrong corner.
            var rt = text.rectTransform;
            rt.anchorMin = new Vector2(1.0f, 0.0f);
            rt.anchorMax = new Vector2(1.0f, 0.0f);
            rt.pivot = new Vector2(1.0f, 0.0f);
            rt.anchoredPosition = new Vector2(-24.0f, 20.0f);
            rt.sizeDelta = new Vector2(132.0f, 22.0f);

            go.AddComponent<VersionStamp>();
        }

        /// <summary>
        /// Attaches the behaviour script that drives a converted screen.
        ///
        /// ⚠️ THE LAYOUT IS CONVERTED; THE BEHAVIOUR IS PORTED CODE. A `.tscn` carries no logic,
        /// only the tree, so each screen still needs its script.
        /// </summary>
        private static void AttachBehaviour(string screenName, GameObject canvasGo)
        {
            switch (screenName)
            {
                case "MainMenu":
                    canvasGo.AddComponent<ConvertedMainMenu>();
                    canvasGo.AddComponent<PennantEntrance>();
                    break;

                case "ModeSelect":
                    canvasGo.AddComponent<ConvertedModeSelect>();
                    canvasGo.AddComponent<PennantEntrance>();
                    break;

                case "MatchSetup":
                    canvasGo.AddComponent<ConvertedMatchSetup>();
                    canvasGo.AddComponent<PennantEntrance>();
                    break;

                case "MultiplayerSetup":
                    canvasGo.AddComponent<ConvertedMultiplayerSetup>();
                    canvasGo.AddComponent<PennantEntrance>();
                    break;

                case "MatchResult":
                    canvasGo.AddComponent<ConvertedMatchResult>();
                    break;

                case "SplashScreen":
                    BindSplash(canvasGo);
                    break;

                case "HUD":
                    Report.AppendLine("      (HUD is bound at match start, not here)");
                    break;

                default:
                    Report.AppendLine($"      (no behaviour bound yet for {screenName})");
                    break;
            }
        }

        /// <summary>
        /// ⚠️⚠️ THE SPLASH'S CLIP AND STING WERE SERIALISED FIELDS THAT NOTHING EVER ASSIGNED,
        /// so the BH Studios sting played on no launch at all: the component was attached, the
        /// coroutine ran, both references were null, and the screen was three seconds of black
        /// followed by the menu. It was reported as "it's also supposed to play the BH studios
        /// animation at the start". Wiring it here is the whole fix, and it belongs here because
        /// this is the only place that knows the scene is being built.
        /// </summary>
        private static void BindSplash(GameObject canvasGo)
        {
            // ⚠️ FULLY QUALIFIED: UnityEngine has a SplashScreen too, and with both namespaces
            // in scope the short name is ambiguous rather than merely surprising.
            var splash = canvasGo.AddComponent<TumbangPreso.UI.SplashScreen>();

            var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(
                $"{ArtRoot}/video/opening_animation.mp4");

            var sting = AssetDatabase.LoadAssetAtPath<AudioClip>(
                $"{ArtRoot}/audio/sfx/boot_sting.wav");

            var so = new SerializedObject(splash);
            so.FindProperty("_clip").objectReferenceValue = clip;
            so.FindProperty("_sting").objectReferenceValue = sting;
            so.ApplyModifiedPropertiesWithoutUndo();

            Report.AppendLine(clip != null
                ? "      splash: opening_animation.mp4 bound"
                : "      FAIL: opening_animation.mp4 missing, the boot sting will be black");

            Report.AppendLine(sting != null
                ? "      splash: boot_sting.wav bound"
                : "      FAIL: boot_sting.wav missing");
        }
    }
}
