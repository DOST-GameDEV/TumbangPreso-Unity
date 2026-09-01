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
            CentreOnFace(label);

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

        /// <summary>
        /// Says "this is the one you are on", on any paper control, in one call.
        ///
        /// ⚠️⚠️ IT EXISTS BECAUSE FIVE SCREENS HAD WRITTEN THIS BY HAND AND THREE OF THEM WERE
        /// WRONG IN THREE DIFFERENT WAYS. `PlayerHub.Highlight` and
        /// `ConvertedCharacterSelect.RefreshTabs` were writing a `GodotButton` variation that
        /// `PaperDress` had already disabled, so their live tab was invisible;
        /// `LobbyJoinPanel.PaintChip` and `SignInScreen.SetTab` used `Token` against `Ghost`, which
        /// `Logs/shots-runtime/Lobby-v52.png` measured at **4 per cent apart in value** and which
        /// the lobby abandoned for that reason. The one place they all agreed was that the caller
        /// has to remember to re-tint the lettering too, and every one of them did it differently.
        ///
        /// ⚠️ IT RETURNS FALSE WHEN THERE IS NO PAPER SKIN, so a caller that still has a wooden
        /// fallback can take it. That is not hypothetical: `RefreshTabs` runs on this screen
        /// before the dress on the very first pass.
        /// </summary>
        /// <param name="idle">
        /// What the control is when it is NOT the one you are on.
        /// ⚠️ `Ghost` FOR A TAB AND `Tray` FOR A LIST ROW, and the difference is not decoration.
        /// A tab you are not on is an alternative you could move to, which is an outline; an
        /// option in an open dropdown is a value you are reading, which is a slot. Drawing the
        /// unselected options of a list as ghosts turns four readable rows into four empty ones.
        /// </param>
        public static bool MarkLive(Component control, bool live,
                                    PaperCraft.Surface idle = PaperCraft.Surface.Ghost)
        {
            if (control == null) return false;

            var skin = control.GetComponent<PaperSkin>();
            if (skin == null) return false;

            skin.Surface = live ? PaperCraft.Surface.Live : idle;
            skin.Rebuild();

            var chip = control.GetComponent<PaperButton>();
            if (chip != null) chip.Restyle();

            return true;
        }

        /// <summary>
        /// Raises a label's box off the bottom of its control by <see cref="PaperCraft.Drop"/>, so
        /// the lettering is centred on the FACE rather than on the rect.
        ///
        /// ⚠️⚠️ IT IS ONE FUNCTION BECAUSE THE CORRECTION HAD ALREADY BEEN WRITTEN TWICE AND ONE
        /// OF THE TWO HAD THE SIGN BACKWARDS. 🧑 2026-09-01, with a crop of the top rail: **"back
        /// still isnt centered as well"**. `PaperKit.Chip` raises `offsetMin.y`, which lifts the
        /// box's BOTTOM edge off the shadow; `LobbyChrome.LiftBack` lowered `offsetMax.y`, which
        /// pulls the box's TOP edge down instead. Both move the box by six units and they move it
        /// in opposite directions, so BACK sat **twelve units below** every other chip in the game
        /// and the two lines of code looked equally reasonable in review.
        ///
        /// ⚠️ IT ADDS TO WHATEVER INSET THE CALLER ALREADY SET rather than assigning, so a chip
        /// that has been inset by `Pad` keeps its padding and a rail control that has not stays
        /// full width. Every raised paper surface draws its cast shadow inside its own bottom
        /// `Drop` units, so this is the whole of the difference between the rect and the face.
        /// </summary>
        public static void CentreOnFace(Text label)
        {
            if (label == null) return;

            var rt = label.rectTransform;
            rt.offsetMin = new Vector2(rt.offsetMin.x, rt.offsetMin.y + PaperCraft.Drop);
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

            // ⚠️⚠️ LAST, AND THE ORDER IS THE POINT. `Type` remaps `UiTheme.Cream` to ink because
            // on a paper sheet cream lettering is invisible, and the lettering on a `Live` pill is
            // the one place in this front end where cream is CORRECT: the pill is wood-dark and
            // the word on it has to invert with it. Running the tint before `Type` means the type
            // pass immediately undoes it, which is a live tab with ink words on a dark plate, and
            // is 🧑's *"hard to read"* on the hub's tab bar exactly.
            foreach (var chip in root.GetComponentsInChildren<PaperButton>(true))
                chip.Restyle();
        }

        private static void Panel(GodotPanel panel)
        {
            if (panel == null) return;

            // ⚠️⚠️ `Card` IS A FIELD AND IT WAS ARRIVING AS FURNITURE. The two nodes in the game
            // carrying that variation are `UiRows.FieldRow`'s text box and `UiRows.DropdownRow`'s
            // face: both are things you read a value out of or type into, which is `Tray` by the
            // enum's own definition. Falling through to `Sheet` drew them as RAISED cut paper with
            // a halo and a cast shadow, so on the hub and in the settings drawer **the input
            // fields stood proud of the sheet they were cut into** and looked identical to the
            // rows around them. It is half of *"match settings ui look ugly"* and all of why the
            // account screen's own name field was hard to find.
            var surface = panel.Variation == "WoodSlot" || panel.Variation == "Card"
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

            // ⚠️⚠️ THE LIVE TAB IS `Live` AND NOT `Token`, AND THAT ONE ROW IS WHY THE HUB'S TAB
            // BAR WAS UNREADABLE. 🧑 2026-09-01, with a crop of the account screen: **"PLAYER CARD
            // IS STILL BROWN AND HARD TO READ COULD BE IMPROVED"**. `PlayerHub.Highlight` and
            // `ConvertedCharacterSelect` both say which tab you are on by writing
            // `WoodTabLiveButton` on a `GodotButton`, and this switch mapped everything that was
            // not the IDLE variation onto one paper pill: six tabs, all `Token`, one of them
            // claiming to be selected in a language the paper front end does not speak.
            //
            // ⚠️ IT IS THE SAME FINDING `PaperCraft.Surface.Live`'S OWN NOTE RECORDS, arrived at
            // from the other end. `Token` against `Ghost` measured 4 per cent apart on
            // `Lobby-v52.png` and the lobby moved to a value inversion; every converted screen
            // kept the pair that had already been rejected, because the mapping lived here.
            var surface = skin.Variation == "WoodTabIdleButton"
                ? PaperCraft.Surface.Ghost
                : skin.Variation == "WoodTabLiveButton"
                ? PaperCraft.Surface.Live
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
        private PaperCraft.Surface _wasSurface;

        /// <summary>
        /// How far through the hover and the press this control currently is, 0 to 1, eased every
        /// frame rather than snapped.
        ///
        /// ⚠️⚠️ THE EASE IS THE WHOLE OF *"feels great to click"* AND IT IS NOT IN THE SPRITE.
        /// 🧑 2026-09-01: **"REWORK THE BUTTONS so that it feels great to click and isnt flat"**,
        /// after a pass that had already given every control a shadow. A sprite swap is a single
        /// frame: the surface is one thing and then it is another, which is exactly as much motion
        /// as a checkbox has. What a physical button gives you is the twentieth of a second in
        /// BETWEEN, and that has to be interpolated by something that runs every frame.
        ///
        /// ⚠️ THE PRESS IS FASTER THAN THE RELEASE, 26 AGAINST 15. A control that goes down slowly
        /// feels unresponsive however quickly it acts, and one that comes back instantly feels
        /// like it bounced. This is the same asymmetry every good physical key has.
        /// </summary>
        private float _lift, _sink;

        /// <summary>
        /// Whether this control may write its own `localScale`.
        ///
        /// ⚠️⚠️ FALSE WHENEVER `ArrowButtonView` IS STILL LIVE ON THE SAME NODE, BECAUSE THAT
        /// COMPONENT ALSO WRITES `localScale` AND `docs/TODO.md` § 119.9 ROW 1 IS WHAT HAPPENS
        /// WHEN TWO THINGS OWN ONE TRANSFORM PROPERTY. `PaperKit.Paperise` disables it for every
        /// node the lobby reparents, but `PaperDress.Screen` converts whole authored screens that
        /// keep their own unfurl, and on those the pennant animation is 🧑's and wins. Reading it
        /// once here is cheaper than a rule somebody has to remember.
        /// </summary>
        private bool _mayScale;

        /// <summary>Where the label sits at rest, so the press can put it back. ⚠️ Captured on the
        /// first refresh rather than in `Awake`, because a label inside a layout group has no
        /// position until the first layout pass has run.</summary>
        private Vector2? _home;

        /// <summary>Whether this control is currently the selected one of a set, and therefore
        /// wants cream lettering rather than ink. ⚠️ Read from the skin rather than told, so a
        /// caller that swaps the surface cannot forget to swap the type with it.</summary>
        /// <summary>
        /// Whether this control's face is wood-dark, and therefore wants CREAM lettering.
        ///
        /// ⚠️⚠️ `Sign` COUNTS AND LEAVING IT OUT PUT INK ON A DARK PLAQUE. The lobby's ROOM CODE
        /// plate is a `Sign`, it carries a `PaperButton` because it is pressable (tap to copy),
        /// and this predicate used to name `Live` alone: the caption came back `PaperInk` on
        /// `WoodMid`, which measured **1.3:1** on `Logs/shots-runtime/Lobby-v57.png` and is 🧑's
        /// *"pic 1 can be improve"* on that exact plate. Both surfaces are the same idea (a dark
        /// object on a cream field) and the type has to invert on both.
        /// </summary>
        private bool _live => _skin != null
                              && (_skin.Surface == PaperCraft.Surface.Live
                                  || _skin.Surface == PaperCraft.Surface.Sign);

        /// <summary>
        /// Whether this control should draw as available.
        ///
        /// ⚠️⚠️ A `Live` CONTROL IS NEVER "OFF", EVEN WHEN `Button.interactable` IS FALSE, AND
        /// THAT EXCEPTION IS LOAD-BEARING RATHER THAN TIDY. `ConvertedCharacterSelect.RefreshTabs`
        /// sets `interactable = !active` on purpose, so the tab you are already on cannot be
        /// pressed again; every other tab row in the game leaves `interactable` alone. Without
        /// this, the one tab that IS selected is also the one tab drawn greyed out, with soft ink
        /// on a wood-dark pill: **the selected state and the unavailable state would be the same
        /// picture, which is the one pair `PaperCraft.Pose`'s own note says must never collide.**
        /// </summary>
        private bool Available => _live || _button == null || _button.interactable;

        private void Awake()
        {
            _skin = GetComponent<PaperSkin>();
            _button = GetComponent<Button>();
            _label = transform.Find("Label") != null
                ? transform.Find("Label").GetComponent<Text>()
                : GetComponentInChildren<Text>();

            var pennant = GetComponent<ArrowButtonView>();
            _mayScale = pennant == null || !pennant.enabled;

            if (_skin != null) _wasSurface = _skin.Surface;

            // ⚠️⚠️ THE TINT RUNS AT `Awake` AND THE POSITION DOES NOT, AND THE SPLIT IS THE WHOLE
            // REASON THIS IS TWO METHODS. `PaperDress.ButtonSkin` sets the surface to `Live` and
            // then adds this component, so a live tab whose lettering waits for the first pointer
            // event is a wood-dark pill with ink words on it for as long as nobody touches it,
            // which is every screenshot ever taken of it. `_home`, on the other hand, cannot be
            // read yet: a label inside a layout group has no position until the first layout pass
            // and capturing zero here would pin every press animation to the wrong origin.
            TintLabel();
        }

        private void Update()
        {
            bool on = Available;

            if (on != _wasInteractable)
            {
                _wasInteractable = on;
                Refresh();
            }

            // ⚠️⚠️ THE SURFACE IS WATCHED RATHER THAN PUSHED, AND THAT IS WHAT MAKES A TAB ROW
            // LEGIBLE WITHOUT EVERY CALLER REMEMBERING TWO LINES. `PlayerHub.Highlight` and
            // `SignInScreen.SetTab` both swap a `PaperSkin.Surface` between `Live` and `Ghost`;
            // the lettering has to invert with it (cream on the wood-dark pill, ink on the
            // outline) or the live tab is a dark plate with dark words on it. Every screen that
            // forgot the second line shipped an unreadable tab, which is 🧑's *"tab row is barely
            // legible"* on the hub.
            if (_skin != null && _skin.Surface != _wasSurface)
            {
                _wasSurface = _skin.Surface;
                Refresh();
            }

            Animate();
        }

        /// <summary>
        /// Eases the control towards its pose, every frame, in UNSCALED time.
        ///
        /// ⚠️⚠️ UNSCALED, BECAUSE THIS FRONT END IS DRAWN OVER A PAUSED GAME. `PausePanel` and
        /// every `ScreenTakeover` in the project set `Time.timeScale` to zero, and a button eased
        /// on `Time.deltaTime` would then never move at all: the press would land, the sprite
        /// would swap and the lettering would stay exactly where it was. Every other timed thing
        /// in the front end (`SignInScreen.WelcomeHold`, the drawer unfurls) is unscaled for the
        /// same reason.
        ///
        /// ⚠️ IT EARLY-OUTS WHEN THERE IS NOTHING TO MOVE. A settled control costs two float
        /// compares a frame, which matters because a lobby has about thirty of these on it and
        /// `Hud`'s per-frame rebuild once cost the 6x probe an eighth of its frames.
        /// </summary>
        private void Animate()
        {
            float wantLift = _hovered && !_held && _wasInteractable ? 1.0f : 0.0f;
            float wantSink = _held ? 1.0f : 0.0f;

            if (Mathf.Abs(_lift - wantLift) < 0.002f && Mathf.Abs(_sink - wantSink) < 0.002f)
            {
                _lift = wantLift;
                _sink = wantSink;
                return;
            }

            float dt = Time.unscaledDeltaTime;
            _lift = Mathf.MoveTowards(_lift, wantLift, dt * (wantLift > _lift ? 15.0f : 11.0f));
            _sink = Mathf.MoveTowards(_sink, wantSink, dt * (wantSink > _sink ? 26.0f : 15.0f));

            Pose();
        }

        /// <summary>
        /// Writes the eased pose onto the transform and the label.
        ///
        /// ⚠️⚠️ THE SCALE IS 2.5 PER CENT AND THE PRESS TAKES 3 OFF IT, WHICH ARE SMALL ON PURPOSE
        /// AND MEASURED AGAINST THE ROW RATHER THAN AGAINST THE BUTTON. A lobby chip is 40 units
        /// in a rail with a 10-unit gap; at five per cent a hovered chip grows two units and
        /// closes a fifth of the gap to its neighbour, which reads as the row shuffling. At 2.5 it
        /// is one unit, which the eye reads as the object coming forward and not as the layout
        /// moving.
        ///
        /// ⚠️ `localScale` DOES NOT REFLOW A LAYOUT GROUP. Unity's layout works off `rect`, so
        /// scaling is purely visual and cannot make a rail twitch. Shrinking the RECT instead is
        /// the fault `GodotButton`'s header opens with.
        /// </summary>
        private void Pose()
        {
            if (_mayScale)
                transform.localScale =
                    Vector3.one * (1.0f + (0.025f * _lift) - (0.03f * _sink));

            if (_label == null || !_home.HasValue) return;

            // ⚠️ THE LABEL RIDES THE SAME TWO NUMBERS THE SURFACE DOES: two units up on a hover
            // (`PaperCraft.PaintRaised` raises the face by the same two) and the full `Drop` down
            // on a press (it takes the whole cast shadow away). Anything else and the lettering
            // and the object it is printed on are moving independently.
            _label.rectTransform.anchoredPosition = _home.Value
                + new Vector2(0.0f, (2.0f * _lift) - (PaperCraft.Drop * _sink));
        }

        /// <summary>
        /// ⚠️⚠️ A CONTROL THAT IS SWITCHED OFF MID-HOVER NEVER GETS ITS `OnPointerExit`, so
        /// without this a drawer closed by the button inside it comes back next time still
        /// scaled up and still lit. Every chip on the bottom rail opens a drawer that hides the
        /// rail, which is the exact shape of that bug and the reason this is here rather than
        /// left to the pointer.
        /// </summary>
        private void OnDisable()
        {
            _hovered = false;
            _held = false;
            _lift = 0.0f;
            _sink = 0.0f;

            if (_mayScale) transform.localScale = Vector3.one;
            if (_label != null && _home.HasValue)
                _label.rectTransform.anchoredPosition = _home.Value;
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

        /// <summary>
        /// Ink on a paper chip, cream on a `Live` one, soft ink when it is off.
        ///
        /// ⚠️ IT READS THE SURFACE RATHER THAN BEING TOLD, so a caller that swaps `Live` for
        /// `Ghost` cannot forget to swap the type with it. That is the failure `PaperDress`'s tab
        /// mapping note records from the other side.
        /// </summary>
        /// <summary>Re-reads the surface and re-tints the label. ⚠️ Called by `PaperDress.Screen`
        /// after its type pass; see the note there for why the order matters.</summary>
        public void Restyle()
        {
            if (_skin == null) _skin = GetComponent<PaperSkin>();
            if (_label == null)
                _label = transform.Find("Label") != null
                    ? transform.Find("Label").GetComponent<Text>()
                    : GetComponentInChildren<Text>();

            if (_skin != null) _wasSurface = _skin.Surface;
            TintLabel();
        }

        private void TintLabel()
        {
            if (_label == null) return;

            bool on = Available;

            _label.color = !on ? UiTheme.PaperInkSoft
                : _live ? UiTheme.Cream
                : UiTheme.PaperInk;
        }

        private void Refresh()
        {
            if (_skin == null) _skin = GetComponent<PaperSkin>();
            if (_skin == null) return;

            bool on = Available;

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

            TintLabel();

            // ⚠️ THE POSITION IS THE ANIMATOR'S NOW, NOT THIS METHOD'S. It used to snap the label
            // to one of two places on every pointer event, which is what made the press read as a
            // colour change with a jump in it; `Animate` owns both offsets so there is exactly one
            // writer of this property. See `Pose`.
            Pose();
        }
    }
}
