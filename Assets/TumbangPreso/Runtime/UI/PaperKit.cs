using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The atoms every paper screen is assembled from, and the type scale they share.
    ///
    /// ⚠️⚠️ THIS IS DELIBERATELY A KIT OF PARTS AND NOT A SCREEN BUILDER, WHICH IS THE WHOLE
    /// ANSWER TO 🧑'S OLDEST COMPLAINT ABOUT THIS FRONT END: *"the issue with old UI is everything
    /// feels repetitive bcz i think u use the same code to generate them all"*, and this pass
    /// *"DONT USE THE SAME METHODS IN MAKING DIFF PAGES AND PANELS unless u have to"*. A shared
    /// `BuildPanel(title, rows)` is exactly how five screens end up being one screen five times.
    /// **What is shared here is the smallest unit that CAN be shared** (a chip, a tray, an ink
    /// label, the type scale); what each screen looks like is its own composition and lives in its
    /// own file.
    ///
    /// The type scale, and it is four steps rather than nine:
    ///
    /// | Step | Units | What it is for |
    /// |---|---|---|
    /// | `Display` | 44 | one per screen: the room code, the wordmark's neighbour |
    /// | `Title` | 26 | the name of a thing: a heading, a value, a player |
    /// | `Body` | 20 | a sentence, a button's lettering, a row |
    /// | `Caption` | 16 | the quiet second line under something, never a sentence on its own |
    ///
    /// ⚠️ `Body` IS 20 AND `MenuKit.MinReadableUnits` IS 18, so every sentence in a paper screen
    /// clears the floor with room to shrink. `Caption` is under it, and it is allowed for the same
    /// reason `LobbyChrome.SummarySize` is: it is only ever a restatement of something already on
    /// the screen at `Title` or larger, never the only place a fact appears.
    /// </summary>
    public static class PaperKit
    {
        public const int Display = 44;
        public const int Title = 26;
        public const int Body = 20;
        public const int Caption = 16;

        /// <summary>
        /// The one gap. Every space between two things on a paper screen is this or a multiple.
        ///
        /// ⚠️⚠️ IT IS NOT NEGOTIABLE PER SCREEN. `docs/TODO.md` § 118.1 and the harmony block in
        /// `LobbyChrome` both record the same fault: the bottom-left rail once had three different
        /// left edges and three different widths because each piece sized itself. One spacing
        /// constant used everywhere is what makes a screen feel calm without anybody being able to
        /// point at why, which is 🧑's *"calming"* as a number.
        ///
        /// ⚠️ IT CAME DOWN FROM 12 TO 10 AND `Pad` FROM 18 TO 14 ON 2026-09-01. 🧑, of the first
        /// island build: **"its still so big too, i wanted it to be tighter and overhauled"**. Two
        /// units of gap and four of padding sound like nothing and they are not: the bottom rail
        /// carries a padding either side and two gaps down its centre column, so this alone takes
        /// 16 units off its height, and every chip, tray and drawer on every paper screen tightens
        /// with it. **Spacing is the loudest thing in a layout that has no other decoration.**
        /// </summary>
        public const float Gap = 10.0f;

        /// <summary>Inside any sheet, between its edge and its content.</summary>
        public const float Pad = 14.0f;

        /// <summary>A pressable paper chip. ⚠️ 40 clears the 32-unit pointer-target floor
        /// `game-ui-design`'s `validations.md` sets even after `PaperCraft.Drop` comes off the
        /// face, with the whole width pressable on top.</summary>
        public const float ChipHeight = 40.0f;

        /// <summary>A row you read: a list entry, a value, a field.</summary>
        public const float RowHeight = 46.0f;

        // -------------------------------------------------------------------------------------
        // Surfaces
        // -------------------------------------------------------------------------------------

        /// <summary>A sheet of card. The furniture everything else sits on.</summary>
        public static Image Sheet(Transform parent, string name)
            => Surface(parent, name, PaperCraft.Surface.Sheet);

        /// <summary>A slot cut into a sheet: a value, a list row, a field's plate.</summary>
        public static Image Tray(Transform parent, string name)
            => Surface(parent, name, PaperCraft.Surface.Tray);

        /// <summary>An empty slot: something that could be filled and is not.</summary>
        public static Image Ghost(Transform parent, string name)
            => Surface(parent, name, PaperCraft.Surface.Ghost);

        /// <summary>The one marked plate on a screen.</summary>
        public static Image Sign(Transform parent, string name)
            => Surface(parent, name, PaperCraft.Surface.Sign);

        private static Image Surface(Transform parent, string name, PaperCraft.Surface surface)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            PaperSkin.Apply(go, surface);
            return go.GetComponent<Image>();
        }

        // -------------------------------------------------------------------------------------
        // Type
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// Ink on paper.
        ///
        /// ⚠️⚠️ NO OUTLINE, AND THAT IS THE SINGLE BIGGEST READABILITY CHANGE IN THIS PASS. Every
        /// `MenuBody`, `MenuValue` and `MenuCaption` in `GodotTheme` carries a 3 to 5 unit ink
        /// outline, because those styles are drawn over a live 3D street and an outline is what
        /// buys a cream word its legibility there. **On an opaque cream sheet the outline has
        /// nothing to do**: it thickens every stroke of a 16-unit caption by a third and turns a
        /// row of small type into a grey smear. Words on paper are drawn flat, and the sheet is
        /// what makes them readable.
        /// </summary>
        public static Text Ink(Transform parent, string text, int size,
                               TextAnchor align = TextAnchor.MiddleLeft, bool soft = false)
        {
            var t = MenuKit.Label(parent, text, size,
                                  soft ? UiTheme.PaperInkSoft : UiTheme.PaperInk,
                                  Vector2.zero, Vector2.zero, Vector2.zero, align);
            t.name = soft ? "InkSoft" : "Ink";
            return t;
        }

        /// <summary>The amber marker, for the one fact on a screen that is a value rather than a
        /// name. ⚠️ On paper amber needs ink under it: `ffba00` on `f4ecdd` is 1.7:1, which is
        /// invisible. The `Sign` surface draws the amber as a BAND and the word on it stays ink,
        /// which is the only way this palette can spend the accent legibly.</summary>
        public static Text Marker(Transform parent, string text, int size,
                                  TextAnchor align = TextAnchor.MiddleCenter)
        {
            var t = MenuKit.Label(parent, text, size, UiTheme.PaperInk,
                                  Vector2.zero, Vector2.zero, Vector2.zero, align);
            t.name = "Marker";
            t.fontStyle = FontStyle.Bold;
            return t;
        }

        // -------------------------------------------------------------------------------------
        // Controls
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// A paper chip: a pill you can press, with its lettering centred on it.
        ///
        /// ⚠️ THE LABEL IS A CHILD RATHER THAN THE CONTROL'S OWN TEXT so the pose can tint it
        /// without touching the surface, and so a caller can put an icon or a second line beside
        /// it without rebuilding the control.
        /// </summary>
        public static Button Chip(Transform parent, string name, string text, int size = Body)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            PaperSkin.Apply(go, PaperCraft.Surface.Token);

            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = go.GetComponent<Image>();

            var label = Ink(go.transform, text, size, TextAnchor.MiddleCenter);
            label.name = "Label";
            label.fontStyle = FontStyle.Bold;
            MenuKit.Stretch(label.rectTransform, -Pad);

            // ⚠️⚠️ THE LETTERING IS CENTRED ON THE FACE, NOT ON THE RECT, AND THE DIFFERENCE IS
            // `PaperCraft.Drop`. 🧑: *"make sure typography looks good with the boxes ok"*. Every
            // raised paper surface draws its cast shadow inside its own bottom edge, so a label
            // stretched to the whole rect is optically three units low on every chip in the game.
            // Three units is not a lot and it is exactly the amount that makes a button look
            // slightly wrong without anybody being able to say why; `GodotTheme.BaselineNudge`
            // records the same class of correction for the font's own line box.
            label.rectTransform.offsetMin =
                new Vector2(label.rectTransform.offsetMin.x, Pad + PaperCraft.Drop);

            go.AddComponent<PaperButton>();
            FocusRing.Attach(go, 4.0f);

            return button;
        }

        /// <summary>
        /// A row that presses: a tray with a chevron on its right, for a list entry that opens
        /// something.
        ///
        /// ⚠️ THE CHEVRON IS THE DOOR AND IT IS NOT DECORATION. `CLAUDE.md` § 6.3: every
        /// destination has a visible door and a door is a thing that looks pressable. A tray with
        /// no chevron is a value; a tray with one is a way through.
        /// </summary>
        public static Button Row(Transform parent, string name, out Text label,
                                 out Text detail, bool chevron = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            PaperSkin.Apply(go, PaperCraft.Surface.Tray);

            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = go.GetComponent<Image>();

            float right = chevron ? 40.0f : Pad;

            label = Ink(go.transform, string.Empty, Body);
            label.name = "RowLabel";
            var lrt = label.rectTransform;
            lrt.anchorMin = new Vector2(0.0f, 0.5f);
            lrt.anchorMax = new Vector2(1.0f, 1.0f);
            lrt.offsetMin = new Vector2(Pad, 0.0f);
            lrt.offsetMax = new Vector2(-right, -6.0f);

            detail = Ink(go.transform, string.Empty, Caption, TextAnchor.LowerLeft, soft: true);
            detail.name = "RowDetail";
            var drt = detail.rectTransform;
            drt.anchorMin = new Vector2(0.0f, 0.0f);
            drt.anchorMax = new Vector2(1.0f, 0.5f);
            drt.offsetMin = new Vector2(Pad, 6.0f);
            drt.offsetMax = new Vector2(-right, 0.0f);

            if (chevron)
            {
                var caret = Ink(go.transform, "›", Title, TextAnchor.MiddleCenter,
                                soft: true);
                caret.name = "Chevron";
                var crt = caret.rectTransform;
                crt.anchorMin = new Vector2(1.0f, 0.0f);
                crt.anchorMax = new Vector2(1.0f, 1.0f);
                crt.pivot = new Vector2(1.0f, 0.5f);
                crt.sizeDelta = new Vector2(36.0f, 0.0f);
                crt.anchoredPosition = new Vector2(-10.0f, 0.0f);
            }

            go.AddComponent<PaperButton>();
            FocusRing.Attach(go, 3.0f);

            return button;
        }

        /// <summary>
        /// The chevron that says a row is a door.
        ///
        /// ⚠️ ONE FUNCTION, BECAUSE THREE ROWS HAVE ONE AND THEY MUST NOT BE THREE SIZES. It is
        /// `CLAUDE.md` § 6.3's rule made cheap: *every destination has a visible door and a door is
        /// a thing that looks pressable*. A `Tray` with no chevron is a value; a `Tray` with one is
        /// a way through, and that distinction only works if the mark is identical everywhere.
        /// </summary>
        public static Text Chevron(Transform parent)
        {
            var caret = Ink(parent, "›", Title, TextAnchor.MiddleRight, soft: true);
            caret.name = "Chevron";
            caret.raycastTarget = false;

            MenuKit.Stretch(caret.rectTransform, 0.0f);
            caret.rectTransform.offsetMax = new Vector2(-Pad, 0.0f);

            return caret;
        }

        /// <summary>
        /// Puts a paper surface on a node that used to carry a wooden one.
        ///
        /// ⚠️⚠️ IT DISABLES `GodotButton` AS WELL AS DESTROYING `WoodSkin`, AND BOTH HALVES ARE
        /// LOAD-BEARING. 🧑: *"MAKE SURE U COMPLETELY REPLACE UI BCZ I DOTN WANT LEFTOVER SHIT FROM
        /// OLD UI TO STILL BE FRIGGING WITH US"*. `GodotButton` writes the Image's sprite on every
        /// hover, press and disable, so a node carrying both would look correct until the pointer
        /// touched it and then flip to wood for as long as the pointer stayed. **That is worse than
        /// not converting it at all, because it is invisible in a screenshot.**
        ///
        /// ⚠️ IT IS DISABLED RATHER THAN DESTROYED, because `SkinLayers` has already given the node
        /// a `Face` child and a `Shadow` child, and destroying the component leaves those behind
        /// drawing wood underneath the new surface. Turning the layers off is what actually removes
        /// them from the screen.
        /// </summary>
        public static void Paperise(GameObject target, PaperCraft.Surface surface)
        {
            if (target == null) return;

            var skin = target.GetComponent<GodotButton>();
            if (skin != null) skin.enabled = false;

            // ⚠️⚠️ THE PENNANT ANIMATOR OWNS THE RECT AND HAS TO COME OFF ANY NODE THIS PASS
            // MOVES. 🧑, twice, with a crop of the top rail: *"back is brokenn"*, then **"te back
            // button still broken"** after the inset was widened, which is what said the inset was
            // never the cause. `ArrowButtonView.SetPivot` re-applies `_offMin` and `_offMax` every
            // frame until its pivot lands, and those are the offsets the node had when the
            // component last captured them: **the authored rect, not the one this file just gave
            // it.** So a reparented control snaps back to where and what it used to be, one frame
            // after being placed, and no amount of correcting the placement can win.
            //
            // ⚠️ THE ANIMATION IS NOT UNWANTED AND IT IS NOT DELETED. It is 🧑's own unfurl and
            // `docs/TODO.md` § 118.1 row 6 asks for more motion, not less; it is correct on the
            // main menu, where the pennants keep the rect it captured. It is wrong on a rail whose
            // layout is decided somewhere else.
            var pennant = target.GetComponent<ArrowButtonView>();
            if (pennant != null)
            {
                pennant.enabled = false;
                target.transform.localScale = Vector3.one;

                var group = target.GetComponent<CanvasGroup>();
                if (group != null) group.alpha = 1.0f;
            }

            foreach (string layer in new[] { "Face", "Shadow" })
            {
                var child = target.transform.Find(layer);
                if (child != null) child.gameObject.SetActive(false);
            }

            var panel = target.GetComponent<GodotPanel>();
            if (panel != null) panel.enabled = false;

            PaperSkin.Apply(target, surface);
        }

        /// <summary>
        /// A chalk hairline, for separating two groups of rows inside one sheet.
        ///
        /// ⚠️ A RULE RATHER THAN A SECOND SHEET. The fault § 92 records is a screen made of boxes
        /// inside boxes; a group that only needs to be told apart from the group above it needs a
        /// line, not a container. This is the cheapest separator there is and it adds no edges.
        /// </summary>
        public static Image Rule(Transform parent)
        {
            var go = new GameObject("Rule", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.color = new Color(UiTheme.PaperEdge.r, UiTheme.PaperEdge.g,
                                    UiTheme.PaperEdge.b, 0.85f);
            image.raycastTarget = false;

            var element = go.AddComponent<LayoutElement>();
            element.minHeight = 2.0f;
            element.preferredHeight = 2.0f;

            return image;
        }

        /// <summary>Vertical stack with the kit's own spacing and padding.</summary>
        public static VerticalLayoutGroup Stack(Transform host, float spacing = Gap,
                                                float pad = Pad)
        {
            var group = host.gameObject.GetComponent<VerticalLayoutGroup>();
            if (group == null) group = host.gameObject.AddComponent<VerticalLayoutGroup>();

            group.spacing = spacing;
            group.padding = new RectOffset((int)pad, (int)pad, (int)pad, (int)pad);
            group.childForceExpandWidth = true;

            // ⚠️⚠️ FALSE, ALWAYS. `childForceExpandHeight` silently overrides every
            // `LayoutElement` under the group, which is the fault `docs/TODO.md` § 117.7 records:
            // three comments in this repository claimed the live tab was four units taller and it
            // never was in any build. A stack whose children state their own heights is the only
            // kind whose heights can be trusted.
            group.childForceExpandHeight = false;
            group.childControlWidth = true;
            group.childControlHeight = true;

            return group;
        }

        /// <summary>Fixes a child's height inside a <see cref="Stack"/>.</summary>
        public static LayoutElement Height(Component child, float height)
        {
            var element = child.gameObject.GetComponent<LayoutElement>();
            if (element == null) element = child.gameObject.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleHeight = 0.0f;
            return element;
        }
    }


    /// <summary>
    /// Converts a whole screen that was built in the wooden language into paper, in one call.
    ///
    /// ⚠️⚠️ IT IS SCOPED BY ROOT AND THAT IS THE ONLY THING KEEPING THE MAIN MENU AND THE MATCH
    /// OUT OF IT. `GodotPanel` and `GodotButton` are the choke points every converted screen in
    /// this game is skinned through, which is what let one edit reach phases 1 to 12 on
    /// 2026-09-01; the same property means editing either of them again would repaint the two
    /// surfaces 🧑 has scoped out twice (*"dont touch main menu and inngame ui"*). So this walks a
    /// GIVEN subtree instead, and the four screens that want it call it by name.
    ///
    /// ⚠️⚠️ IT DISABLES RATHER THAN DESTROYS, AND BOTH HALVES MATTER. `GodotButton` writes its
    /// Image's sprite on every hover, press and disable, so a node carrying both skins would look
    /// right until the pointer touched it and then flip to wood for as long as the pointer stayed:
    /// **invisible in a screenshot, obvious in the hand.** And `SkinLayers` has already given each
    /// of them a `Face` and a `Shadow` child, so turning the component off is not enough on its
    /// own; the layers have to go too or they keep drawing wood underneath the new surface. 🧑:
    /// *"MAKE SURE U COMPLETELY REPLACE UI BCZ I DOTN WANT LEFTOVER SHIT FROM OLD UI TO STILL BE
    /// FRIGGING WITH US"*. `PaperPurityProbe` is the gate that says this actually happened.
    ///
    /// ⚠️ THE GREEN PRIMARY IS LEFT ALONE. `WoodPrimaryButton` is 🧑's own `JOIN BUTTON.png`
    /// colour and `CLAUDE.md` § 6.5 calls green his primary; on a cream screen it is the only
    /// saturated object in the frame, which is what makes the one action findable without
    /// spending the accent. Repainting it would leave a screen with no figure at all.
    /// </summary>
    public static class PaperDress
    {
        /// <summary>Dresses every surface under `root`. Safe to call more than once.</summary>
        public static void Screen(Transform root)
        {
            if (root == null) return;

            foreach (var panel in root.GetComponentsInChildren<GodotPanel>(true))
                Panel(panel);

            foreach (var button in root.GetComponentsInChildren<GodotButton>(true))
                ButtonSkin(button);

            foreach (var wood in root.GetComponentsInChildren<WoodSkin>(true))
                FromWood(wood);

            foreach (var text in root.GetComponentsInChildren<Text>(true))
                Type(text);
        }

        private static void Panel(GodotPanel panel)
        {
            if (panel == null) return;

            var surface = panel.Variation == "WoodSlot"
                ? PaperCraft.Surface.Tray
                : PaperCraft.Surface.Sheet;

            Strip(panel.gameObject);
            panel.enabled = false;
            PaperSkin.Apply(panel.gameObject, surface);
        }

        private static void ButtonSkin(GodotButton skin)
        {
            if (skin == null) return;

            // ⚠️ THE PRIMARY KEEPS ITS WOOD. See the class note.
            if (skin.Variation == "WoodPrimaryButton" || skin.Variation == "PrimaryButton") return;

            var surface = skin.Variation == "WoodTabIdleButton"
                ? PaperCraft.Surface.Ghost
                : PaperCraft.Surface.Token;

            Strip(skin.gameObject);
            skin.enabled = false;
            PaperSkin.Apply(skin.gameObject, surface);

            if (skin.GetComponent<PaperButton>() == null)
                skin.gameObject.AddComponent<PaperButton>();
        }

        private static void FromWood(WoodSkin wood)
        {
            if (wood == null) return;

            var surface = wood.Surface switch
            {
                WoodCraft.Surface.Button => PaperCraft.Surface.Token,
                WoodCraft.Surface.Action => PaperCraft.Surface.Token,
                WoodCraft.Surface.Tab => PaperCraft.Surface.Token,
                WoodCraft.Surface.Field => PaperCraft.Surface.Tray,
                WoodCraft.Surface.Paper => PaperCraft.Surface.Tray,
                WoodCraft.Surface.PaperField => PaperCraft.Surface.Tray,
                WoodCraft.Surface.Slate => PaperCraft.Surface.Tray,
                _ => PaperCraft.Surface.Sheet,
            };

            // ⚠️ `PaperSkin.Apply` DESTROYS THE `WoodSkin` ITSELF, which is why this loop can
            // iterate an array it is mutating: `GetComponentsInChildren` has already snapshotted.
            var target = wood.gameObject;
            Strip(target);
            PaperSkin.Apply(target, surface);
        }

        /// <summary>
        /// Ink on paper, for a label that was written for cream on wood.
        ///
        /// ⚠️⚠️ THE OUTLINE IS THE BIGGEST SINGLE CHANGE AND IT IS NOT COSMETIC. Every
        /// `MenuBody`, `MenuValue` and `MenuCaption` in `GodotTheme` carries a 3 to 5 unit ink
        /// outline, because those styles were written to be read over a live 3D street. On an
        /// opaque cream sheet the outline thickens every stroke of a 16-unit caption by a third
        /// and turns a column of small type into a grey smear.
        ///
        /// ⚠️ ONLY THE KNOWN MENU COLOURS ARE REMAPPED. A hero accent, a rank colour, the taya
        /// blue and `Offense` orange all MEAN something (`UiTheme`'s own header), and a blanket
        /// recolour is how a screen quietly stops telling the player which side is which.
        /// </summary>
        private static void Type(Text text)
        {
            if (text == null) return;

            var outline = text.GetComponent<GodotOutline>();
            if (outline != null) outline.enabled = false;

            if (Near(text.color, UiTheme.Amber) || Near(text.color, UiTheme.Highlight))
            {
                text.color = UiTheme.PaperInk;
                text.fontStyle = text.fontStyle == FontStyle.Italic
                    ? FontStyle.BoldAndItalic : FontStyle.Bold;
                return;
            }

            if (Near(text.color, UiTheme.Cream) || Near(text.color, UiTheme.Card)
                || Near(text.color, UiTheme.Ink) || Near(text.color, UiTheme.Panel))
            {
                text.color = UiTheme.PaperInk;
                return;
            }

            if (Near(text.color, UiTheme.CreamMuted) || Near(text.color, UiTheme.InkMuted))
                text.color = UiTheme.PaperInkSoft;
        }

        /// <summary>⚠️ ALPHA IS COMPARED TOO, because `CreamMuted` is `Cream` at 0.68 and nothing
        /// else distinguishes them.</summary>
        private static bool Near(Color a, Color b)
            => Mathf.Abs(a.r - b.r) < 0.02f && Mathf.Abs(a.g - b.g) < 0.02f
               && Mathf.Abs(a.b - b.b) < 0.02f && Mathf.Abs(a.a - b.a) < 0.05f;

        /// <summary>Removes the wooden layers `SkinLayers` left behind. ⚠️ See the class note:
        /// turning the component off leaves its `Face` and `Shadow` children drawing wood under
        /// the new surface, which is the leftover 🧑 asked twice to be sure of.</summary>
        private static void Strip(GameObject target)
        {
            foreach (string layer in new[] { "Face", "Shadow" })
            {
                var child = target.transform.Find(layer);
                if (child != null) child.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Hover, press and disabled for a paper control, plus the two sounds.
    ///
    /// ⚠️⚠️ IT IS A SEPARATE COMPONENT FROM `GodotButton` RATHER THAN A FLAG ON IT. That class
    /// resolves a `GodotTheme.ButtonStyle` by variation name, builds a shadow layer and a face
    /// layer through `SkinLayers`, and sinks its label by Godot's own five units. None of that is
    /// true of a paper chip, and threading a second material through it would put two unrelated
    /// state machines in one `Update`.
    ///
    /// ⚠️ THE DISABLED STATE IS A POSE, NOT A TINT. `game-ui-design`'s `Color-Only Information`
    /// anti-pattern is explicit: a control that is only distinguishable by colour is not
    /// distinguishable. A disabled paper chip loses its bottom lip as well as its contrast, so it
    /// stops being a raised object.
    /// </summary>
    [RequireComponent(typeof(PaperSkin))]
    public sealed class PaperButton : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private PaperSkin _skin;
        private Button _button;
        private Text _label;
        private bool _hovered, _held;
        private bool _wasInteractable = true;

        /// <summary>Where the label sits at rest, so the press can put it back. ⚠️ Captured on the
        /// first refresh rather than in `Awake`, because a label inside a layout group has no
        /// position until the first layout pass has run.</summary>
        private Vector2? _home;

        /// <summary>Whether this control is currently the selected one of a set, and therefore
        /// wants cream lettering rather than ink. ⚠️ Read from the skin rather than told, so a
        /// caller that swaps the surface cannot forget to swap the type with it.</summary>
        private bool _live => _skin != null && _skin.Surface == PaperCraft.Surface.Live;

        private void Awake()
        {
            _skin = GetComponent<PaperSkin>();
            _button = GetComponent<Button>();
            _label = transform.Find("Label") != null
                ? transform.Find("Label").GetComponent<Text>()
                : GetComponentInChildren<Text>();
        }

        private void Update()
        {
            bool on = _button == null || _button.interactable;
            if (on == _wasInteractable) return;

            _wasInteractable = on;
            Refresh();
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (_button != null && !_button.interactable) return;
            _hovered = true;
            Refresh();
            MenuSfx.Hover();
        }

        public void OnPointerExit(PointerEventData e)
        {
            _hovered = false;
            _held = false;
            Refresh();
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (_button != null && !_button.interactable) return;
            _held = true;
            Refresh();
            MenuSfx.Click();
        }

        public void OnPointerUp(PointerEventData e)
        {
            _held = false;
            Refresh();
        }

        private void Refresh()
        {
            if (_skin == null) _skin = GetComponent<PaperSkin>();
            if (_skin == null) return;

            bool on = _button == null || _button.interactable;

            _skin.SetPose(!on ? PaperCraft.Pose.Off
                          : _held ? PaperCraft.Pose.Press
                          : _hovered ? PaperCraft.Pose.Hover
                          : PaperCraft.Pose.Rest);

            if (_label == null) return;

            // ⚠️⚠️ THE LABEL SINKS ON A PRESS AND THAT IS THE HALF OF THE FEEL THAT IS NOT IN THE
            // SPRITE. 🧑: *"refine the buttons make them feel good and shit idk, i js dont wwant it
            // too flat"*. `PaperCraft` takes the cast shadow away when a control is pressed, so the
            // OBJECT goes down; without moving the lettering with it the words float where they
            // were and the press reads as a colour change. `GodotButton` does exactly this for the
            // wooden set (Godot's own five-unit sink, ported), and it is why his authored buttons
            // have always felt better than anything drawn in code.
            //
            // ⚠️ IT MOVES THE LABEL, NOT THE BUTTON. Shrinking or offsetting the control itself
            // reflows every sibling in its layout group and makes the whole rail twitch, which is
            // the note `GodotButton`'s header opens with.
            if (!_home.HasValue) _home = _label.rectTransform.anchoredPosition;

            _label.color = !on ? UiTheme.PaperInkSoft
                : _live ? UiTheme.Cream
                : UiTheme.PaperInk;

            _label.rectTransform.anchoredPosition = _held
                ? _home.Value + new Vector2(0.0f, -PaperCraft.Drop)
                : _home.Value;
        }
    }
}
