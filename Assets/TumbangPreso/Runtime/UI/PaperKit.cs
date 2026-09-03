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
        /// Which face a step of the scale is set in, and it is decided by the STEP rather than by
        /// the caller.
        ///
        /// ⚠️⚠️ THE SPLIT IS MECHANICAL ON PURPOSE, AND THAT IS `CLAUDE.md` § 4a'S ARGUMENT
        /// APPLIED TO TYPE: *"the answer is construction, not discipline."* `docs/TODO.md` § 133
        /// asks for two faces doing two jobs, and the obvious way to build that is a `Face`
        /// parameter on every text call. **That is a second place to forget**, and forgetting it
        /// compiles, renders and looks approximately right, which is how the front end ended up
        /// with one display face setting four-line ability descriptions in the first place.
        ///
        /// **So the boundary is a number and the number is <see cref="Body"/>.** Everything at
        /// `Body` and above is Darumadrop; only `Caption` and anything smaller is Nunito. A
        /// caller never chooses, so a caller can never choose wrong.
        ///
        /// ⚠️⚠️ THE BOUNDARY WAS `Title` FOR ONE AFTERNOON AND 🧑 MOVED IT, AND HIS REASON IS THE
        /// DEFINITION OF THE SECOND FACE RATHER THAN A PREFERENCE ABOUT IT. At `Title` the split
        /// put `Body` 20 into Nunito, which is most of the lettering in the game: every settings
        /// row, every button, every list entry. He looked at it and said **"ur over replacing
        /// fonts, i lowk js wanted u to replace sub fonts with the new font, not everything
        /// gang"**, and of the login screen specifically, *"i think everything here in darumadrop
        /// looked good, just change your username to the sub font"*.
        ///
        /// **So Nunito is the SUB font and not the body font**, which is a narrower job than
        /// § 133 first described: the quiet second line under a row, a hint, a field's caption and
        /// its placeholder, an ability description. Darumadrop keeps everything a player looks AT,
        /// and that is now most of the front end rather than half of it.
        ///
        /// ⚠️ AND THE FAULT § 133 EXISTS FOR IS STILL FIXED, WHICH IS WHY THIS COSTS NOTHING.
        /// § 132.8's complaint was the SMEAR: `FontStyle.Bold` on a face with one weight. That is
        /// unreachable now wherever this lands, because <see cref="MenuKit.Apply"/> clears
        /// `fontStyle` on both sides and bold on Darumadrop is a documented no-op. The prose that
        /// actually needed a reading face, the four-line ability descriptions, is authored at
        /// `Caption`, so it is still Nunito.
        ///
        /// ⚠️ A CALLER THAT GENUINELY NEEDS THE OTHER SIDE CALLS <see cref="MenuKit.Apply"/>
        /// AFTERWARDS AND SAYS WHY IN A COMMENT.
        /// </summary>
        public static MenuKit.Face FaceFor(int size)
            => size >= Body ? MenuKit.Face.Display : MenuKit.Face.Body;

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

            // ⚠️ EVERY WORD ON A PAPER SCREEN GOES THROUGH HERE, so this one line is what moves
            // the front end's reading matter onto the body face. See `FaceFor` for why the step
            // decides rather than the caller.
            MenuKit.Apply(t, FaceFor(size));

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

            // ⚠️ THE BOLD FILE, NOT `FontStyle.Bold`. At `Body` and `Caption` this is a drawn
            // weight; at `Title` and above it resolves to Darumadrop, which has no bold and
            // needs none. `MenuKit.Apply` is where that decision is written down.
            MenuKit.Apply(t, FaceFor(size), bold: true);

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

            // ⚠️⚠️ EVERY CHIP IN THE FRONT END IS DRAWN IN THE LOGO'S LANGUAGE NOW. 🧑
            // 2026-09-03: **"i wanted u to remake all buttons in a diff style that feels like my
            // logo bruh"**, and *"the darumadrop buttons AS TEXT stay"*. The lettering was never
            // the complaint; the lit-solid surface under it was. `PaperCraft.Surface.Brand` is a
            // flat fill inside a thick uneven deep-red stroke with a darker bar inside its bottom
            // edge, which is how every shape in the mark is built.
            //
            // ⚠️ `Accent.Wood` IS HONEY QUARTZ HERE, not brown. A chip is a SECONDARY control, so
            // it takes the quiet fill; the one primary per screen takes Chartreuse through
            // `Accent.Green`. `docs/Front_End_Design.md` § 4 is the role table and this is the one
            // place the two are told apart.
            PaperSkin.Apply(go, PaperCraft.Surface.Token);

            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = go.GetComponent<Image>();

            var label = Ink(go.transform, text, size, TextAnchor.MiddleCenter);
            label.name = "Label";
            MenuKit.Apply(label, FaceFor(size), bold: true);
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

            // ⚠️⚠️ A CONVERTED TEXT FIELD LOSES UNITY'S BLUE SELECTION HIGHLIGHT HERE, AND THIS
            // IS THE PER-NODE HALF OF THE FIX IN `PaperDress.Screen`. Both are needed because
            // both paths exist: `ConvertedSettingsPanel.WireNameField` paperises the ONE node
            // rather than the screen, so a walk that only ran over a whole root would miss the
            // exact field `PaperPurityProbe.NoFieldHighlightsInBlue` caught.
            //
            // ⚠️ `a8ceff` IS 87 LEVELS MORE BLUE THAN RED and `CLAUDE.md` § 6.4 bans it in any
            // layer. Nothing in the project had ever assigned `selectionColor`, so every field in
            // the game shipped with it.
            foreach (var field in target.GetComponentsInChildren<InputField>(true))
                MenuKit.Dress(field);

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
        /// Makes a control THE action on its screen: one call, one appearance, everywhere.
        ///
        /// ⚠️⚠️ IT EXISTS BECAUSE THE PRIMARY WAS THE ONE CONTROL EACH SCREEN STILL DREW ITS OWN
        /// WAY, AND HE FOUND IT ON FOUR OF THEM IN ONE SITTING. 2026-09-02: **"u really have to
        /// redesign start match button, it doesnt FEEL like a start match button"**, *"i like the
        /// size adn color but it feells so flat"*, and, of BACK sitting beside KEEP AND USE,
        /// **"i dont get why theres rounded sshit next to square shit"**. The lobby's came from
        /// `GodotTheme.WoodPrimaryButton`, the login's from the same, the maker's and the picker's
        /// from `MenuKit.WoodButton`: four screens, one intention, four constructions.
        /// `docs/TODO.md` 121.1 has the measurement that settled it.
        ///
        /// ⚠️ IT DISABLES `GodotButton` RATHER THAN LIVING BESIDE IT, which `Paperise` already
        /// does for every other converted control and which matters more here than anywhere: that
        /// component sinks the LABEL five units on a press and so does `PaperButton`. **Two owners
        /// of one transform property is 119.9 row 1** and it has shipped once already.
        ///
        /// ⚠️ THE LETTERING IS CENTRED ON THE FACE, NOT ON THE RECT. Every raised paper surface
        /// draws its cast shadow inside its own bottom units, so a label centred on the rect sits
        /// low by half the drop. `CentreOnFace` is the one place that correction is written, for
        /// the reason 120.2 records: it had been written twice before and one copy had the sign
        /// backwards.
        /// </summary>
        public static PaperButton MakeAction(GameObject target, PaperCraft.Accent accent)
        {
            if (target == null) return null;

            Paperise(target, PaperCraft.Surface.Action);

            // ⚠️⚠️⚠️ EVERY CHILD GRAPHIC GOES, AND WITHOUT THIS THE BUTTON DRAWS TWO SILHOUETTES
            // AT ONCE. 🧑 2026-09-02, with a crop of the first build of this surface:
            // **"ew wtf os i[ with that start match shit its a circle and a sharp shape at the
            // same time wtfffffffffffffffffffffffff"**, and then **"can u js remake the entire
            // start match button? keep the color and font and shit but remake the whole button,
            // bcz i think trying to imrpove it manually will lead nowhere"**.
            //
            // **He was describing exactly what was on screen and it took a 6x crop to see why.**
            // `Logs/crops/start-cap-v61.png`: a rounded `Action` pill on the node's own Image, and
            // 🧑's chamfered `BUTTON LONG.png` drawn on top of it by a CHILD. `Paperise` disables
            // `GodotButton` and the two `SkinLayers` children it knows about, `Face` and `Shadow`
            // — and `ArrowButtonView` builds three more (`Artwork`, `Lit`, `Rim`) that nothing in
            // that method has ever heard of. **Disabling a component does not remove the objects
            // it made**, which is the same finding `PaperDress`'s own header records about
            // `SkinLayers` and is now the second time it has cost a render.
            //
            // ⚠️ SO IT WALKS RATHER THAN NAMING. A list of layer names is a list somebody has to
            // extend, and the layer added next year is the one that draws through the primary. An
            // `Action` owns exactly one surface by definition, so "every graphic below me that is
            // not the lettering" is the rule rather than five strings.
            //
            // ⚠️⚠️ AND THIS IS THE ONE PLACE THIS PASS STOPS DRAWING AN AUTHORED CONTROL, WHICH
            // NEEDS SAYING OUT LOUD BECAUSE `CLAUDE.md` § 6.4 AND `docs/VISION.md` § 6 BOTH
            // FORBID REPAINTING HIS ART. Three things make it the right call and he made the
            // first of them: **he asked for the button to be remade, by name, twice.** The FILE
            // is untouched, the main menu still draws it through `ArrowButtonView` with its
            // unfurl intact, and `docs/TODO.md` § 120.4 already recorded the same decision for
            // `SETTINGS CONFIG PANEL.png` and `MAP MODE DISPLAY.png` when they were the field
            // rather than the control. **His art that is still a control on this screen stays:**
            // the pennants, `JOIN BUTTON.png`, the arrows, `TUMP.png` and the key art.
            PaperButton.SilenceChildGraphics(target);

            var skin = target.GetComponent<PaperSkin>();
            if (skin != null)
            {
                skin.Accent = accent;
                skin.Rebuild();
            }

            var label = target.transform.Find("Label") != null
                ? target.transform.Find("Label").GetComponent<Text>()
                : target.GetComponentInChildren<Text>();

            if (label != null)
            {
                // ⚠️⚠️ INK ON CHARTREUSE AND CREAM ON THE DARK ONE, BECAUSE ONE COLOUR CANNOT
                // SERVE BOTH FILLS AND THE MEASUREMENT SAYS SO. `Cream` was right for the whole
                // life of this method: every `Surface.Action` fill was a dark slab, his authored
                // brown or his authored green, and cream on either is 8:1 or better. **Chartreuse
                // `d6ce01` is a LIGHT fill**, and cream on it measures **1.2:1**, which is
                // invisible. Ink on it measures **9.1:1**.
                //
                // ⚠️ THIS IS `CLAUDE.md` § 6.4'S OWN LESSON ON THE OTHER AXIS: a colour that was
                // correct against the surface it was chosen for is not a colour, it is a pairing,
                // and moving the surface without moving its partner is how a label goes quietly
                // unreadable. `scratchpad/fontsrc/ramp.py` computes both numbers.
                label.color = accent == PaperCraft.Accent.Green ? UiTheme.PaperInk : UiTheme.Cream;
                label.alignment = TextAnchor.MiddleCenter;

                // ⚠️ THE AUTHORED SHADOW COMES OFF. `GodotButton` adds a `Shadow` to every wooden
                // label so cream survives over a lit street; on an opaque slab it only thickens
                // the strokes, which is the same argument `PaperKit.Ink`'s header makes about
                // outlines and is why no paper type in this file carries one.
                var shadow = label.GetComponent<Shadow>();
                if (shadow != null) shadow.enabled = false;

                CentreOnFace(label);
            }

            var chip = target.GetComponent<PaperButton>();
            if (chip == null) chip = target.AddComponent<PaperButton>();
            chip.Restyle();

            return chip;
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
        ///
        /// ⚠️⚠️ IT WAS `Ghost` AND THE RENDER SAID NO: A TAB ROW WAS TWO DIFFERENT SILHOUETTES.
        /// `Logs/crops/picker-tabs-v61.png` is four controls in one rail: `HERO` a full pill,
        /// `LATA` and `TSINELAS` 18-unit rounded RECTANGLES, `MAKE YOUR OWN` a pill again. 🧑, of
        /// that exact row: **"these buttons look ugly"**, and of the same fault on the lobby's
        /// primary, **"i dont get why theres rounded sshit next to square shit"**.
        ///
        /// **`PaperCraft.Surface.Live`'s own note had already forbidden this in writing:** *"`Live`
        /// IS THIS SAME OBJECT INVERTED, not a second silhouette. Same pill, same halo, same lip,
        /// same shadow; only the values swap. Giving the selected tab its own shape would say
        /// 'these two controls are different KINDS of thing', which is the opposite of what a tab
        /// pair means."* It was paired with `Ghost`, which is a different shape, so the rule was
        /// broken from the side nobody was looking at.
        ///
        /// ⚠️ AND THE OLD ARGUMENT FOR `Ghost` NO LONGER HOLDS, WHICH IS WHY THIS IS A CORRECTION
        /// RATHER THAN A REVERSAL. It read: *"A tab you are not on is an alternative you could
        /// move to, which is an outline."* That was written when the live half was `Token`, and
        /// § 120.3 measured `Token` against `Ghost` at **4 per cent apart in value** and moved the
        /// live half to `Live`. With a wood-dark `Live` the pair is a **10:1** inversion whatever
        /// the idle is, so the shape no longer has to carry the difference and can go back to
        /// meaning what it says: `Token` is *you can press it*, which is exactly what an
        /// unselected tab is, and `Ghost` is *nothing is here yet*, which it never was.
        ///
        /// ⚠️ `Tray` FOR A LIST ROW STILL STANDS. An option in an open dropdown is a value you are
        /// reading, which is a slot; drawing those as tokens would make four readable rows into
        /// four buttons.
        /// </param>
        public static bool MarkLive(Component control, bool live,
                                    PaperCraft.Surface idle = PaperCraft.Surface.Token)
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
    /// ⚠️⚠️ CORRECTED 2026-09-02: THE GREEN PRIMARY IS NOT LEFT ALONE ANY MORE, AND THE OLD NOTE
    /// IS KEPT HERE BECAUSE ITS ARGUMENT WAS RIGHT AND ITS CONCLUSION WAS NOT. It read:
    /// *"`WoodPrimaryButton` is his own `JOIN BUTTON.png` colour and `CLAUDE.md` § 6.5 calls green
    /// his primary; on a cream screen it is the only saturated object in the frame, which is what
    /// makes the one action findable without spending the accent. Repainting it would leave a
    /// screen with no figure at all."*
    ///
    /// **Every clause of that is still true and none of it required the WOODEN construction.**
    /// `PaperCraft.Surface.Action` keeps the colour, keeps the saturation and keeps the figure,
    /// and drops the chamfer and the 10-per-cent-saturation halo that made the one control on the
    /// screen look like it came from a different program. `docs/TODO.md` § 121.1 is the
    /// measurement and 🧑's *"i dont get why theres rounded sshit next to square shit"* is the
    /// report. **A rule written to protect a colour ended up protecting a silhouette nobody meant
    /// to keep**, which is the same shape of mistake `CLAUDE.md` § 6.4 records about "outlines".
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

            // ⚠️⚠️ AND EVERY CONVERTED TEXT FIELD LOSES UNITY'S BLUE SELECTION HIGHLIGHT HERE,
            // BECAUSE A CONVERTED FIELD HAS NO `AddComponent<InputField>` SITE TO FIX.
            // `MenuKit.Dress` was called at all four places the game BUILDS a field in code, and
            // `PaperPurityProbe.NoFieldHighlightsInBlue` immediately found a FIFTH: the settings
            // panel's `PlayerNameField`, which comes out of a `.tscn` and so was reached by none
            // of them. That is `docs/TODO.md` § 120.4's lesson exactly, one component across:
            // **a thing set outside the components the conversion knows about is a thing the
            // conversion is blind to.**
            //
            // ⚠️ IT IS IN THE WALK RATHER THAN AT THE ONE SITE, so a converted field added later
            // cannot miss it. `CLAUDE.md` § 4a: the answer is construction, not discipline.
            foreach (var field in root.GetComponentsInChildren<InputField>(true))
                MenuKit.Dress(field);

            // ⚠️⚠️ LAST, AND THE ORDER IS THE POINT. `Type` remaps `UiTheme.Cream` to ink because
            // on a paper sheet cream lettering is invisible, and the lettering on a `Live` pill is
            // the one place in this front end where cream is CORRECT: the pill is wood-dark and
            // the word on it has to invert with it. Running the tint before `Type` means the type
            // pass immediately undoes it, which is a live tab with ink words on a dark plate, and
            // is 🧑's *"hard to read"* on the hub's tab bar exactly.
            foreach (var chip in root.GetComponentsInChildren<PaperButton>(true))
            {
                chip.Restyle();

                // ⚠️⚠️ AND THE LETTERING IS LIFTED ONTO THE FACE, WHICH NOTHING THAT ARRIVES
                // THROUGH THIS PASS HAD EVER HAD. 🧑 2026-09-02, of the picker's tab rail:
                // **"problem wiht this is they arent centered"**, *"look it droops a bit down
                // more"*. `PaperKit.Chip` and `MakeAction` centre on the face at build time; a
                // converted `GodotButton` keeps whatever box the `.tscn` or `MenuKit.WoodButton`
                // gave it, and every raised surface hides six units of cast shadow inside its own
                // bottom edge. **Every tab in the game was three units low.** See
                // `PaperButton.CentreLabelOnFace`; it latches, because this method runs after
                // every tab press.
                chip.CentreLabelOnFace();
            }
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

            // ⚠️⚠️ ALREADY AN ACTION: LEAVE IT, AND THIS GUARD IS LOAD-BEARING TWICE OVER.
            // `PaperDress.Screen` runs after EVERY tab build and every drawer open, and
            // `MakeAction` calls `PaperKit.CentreOnFace`, which ADDS `PaperCraft.Drop` to the
            // label's inset rather than assigning it (see that method: adding is what lets a chip
            // keep the padding its caller gave it). Running it twice would walk the lettering six
            // units up the button on every redraw. It also stops this pass from taking the lobby's
            // BROWN primary, which `LobbyChrome.BuildActionSlot` has already converted with the
            // other accent, and flattening it back to a `Token`.
            var already = skin.GetComponent<PaperSkin>();
            if (already != null && already.Surface == PaperCraft.Surface.Action) return;

            // ⚠️⚠️ THE PRIMARY IS PAPER'S OWN `Action` NOW, AND THIS ONE LINE IS WHY IT WAS WOOD
            // ON FIVE SCREENS. This method used to `return` here, under a note saying the green
            // primary keeps its wood because it is the only saturated object in the frame and
            // repainting it would leave a screen with no figure. **The figure argument was right
            // and the conclusion was wrong**: an `Action` is still his authored green, still the
            // only saturated object on the screen, and now it is also the same KIND of object as
            // everything standing beside it.
            //
            // 🧑 2026-09-02, with a crop of the maker's footer showing BACK beside KEEP AND USE:
            // **"i dont get why theres rounded sshit next to square shit or wtbv the design of the
            // shit nexxt to it is"**. That was this early return, seen from the outside: a rounded
            // paper pill next to a chamfered wooden slab with a halo that measures **10 per cent
            // saturation** beside paper edges at 30 (`docs/TODO.md` § 121.1). Every green primary
            // in the paper front end goes through one call now: CREATE ACCOUNT, KEEP AND USE,
            // CHOOSE, JOIN, the hub's footer action and the queue's.
            if (skin.Variation == "WoodPrimaryButton" || skin.Variation == "PrimaryButton")
            {
                PaperKit.MakeAction(skin.gameObject, PaperCraft.Accent.Green);
                return;
            }

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
            // ⚠️⚠️ THE IDLE TAB IS A `Token` NOW AND THIS IS THE SECOND OF TWO PLACES THAT DECIDE
            // IT, WHICH IS WHY THE HUB AND THE PICKER DISAGREED FOR A WHOLE RENDER.
            // `PaperKit.MarkLive` picks the surface when a tab is pressed and THIS picks it when
            // the screen is dressed, and `PlayerHub.Show` runs `Highlight` **before**
            // `PaperDress.Screen`: so changing only `MarkLive` fixed the fighter picker's row and
            // left the hub's column exactly as it was, because the dress ran afterwards and put
            // `Ghost` back. **Two writers of one property, found in a picture rather than in a
            // review** (`Logs/crops/hub-tabs-v61b.png` against `picker-tabs-v61b.png` from the
            // same run), which is the same shape as § 119.9 row 1 and § 120.5 row 1.
            //
            // ⚠️ THE REASON IT IS `Token` IS IN `MarkLive`'S OWN `idle` PARAMETER: a tab row was
            // shipping as two different SILHOUETTES, a `Live` pill against `Ghost` rounded
            // rectangles, which `PaperCraft.Surface.Live`'s note forbids in writing. 🧑, of that
            // row: **"these buttons look ugly"**. `docs/TODO.md` § 121.10 row 3.
            var surface = skin.Variation == "WoodTabIdleButton"
                ? PaperCraft.Surface.Token
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

            // ⚠️⚠️ THIS IS THE ONE LINE THAT MOVES THE CONVERTED SCREENS ONTO THE BODY FACE, and
            // it is worth as much as every hand edit in `docs/TODO.md` § 133 put together. Settings,
            // character select and match setup are `.tscn` conversions: their labels are built by
            // `TscnUiImporter` from an authored scene, so no C# call site exists to change. Every
            // one of them passes through here, once, when the screen is papered.
            //
            // ⚠️ IT READS THE AUTHORED SIZE, which is the same input `FaceFor` gets everywhere
            // else, so a converted row and a code-built row of the same size land in the same
            // face. A screen where half the rows were Darumadrop and half Nunito would be worse
            // than either face alone.
            var face = PaperKit.FaceFor(text.fontSize);
            bool wasBold = text.fontStyle == FontStyle.Bold
                           || text.fontStyle == FontStyle.BoldAndItalic;

            MenuKit.Apply(text, face, wasBold);

            if (Near(text.color, UiTheme.Amber) || Near(text.color, UiTheme.Highlight))
            {
                text.color = UiTheme.PaperInk;

                // ⚠️ WEIGHT REPLACES THE ACCENT, and now there is a weight to replace it WITH.
                // Amber on cream paper is 1.7:1 and invisible, so this row has always traded the
                // colour for emphasis; until § 133 that emphasis was Unity's synthetic bold on a
                // face with no bold, so it bought a smear rather than a weight and the accent was
                // spent for nothing.
                MenuKit.Apply(text, face, bold: true);
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

        /// <summary>
        /// Whether the keyboard or the pad is on this control.
        ///
        /// ⚠️⚠️ IT IS A SECOND WAY INTO THE SAME POSE AND IT REPLACED A RING. 🧑 2026-09-02:
        /// **"can u remvoe thhat black line taht shows up in everywhere? i really dont want to
        /// have that"**. `FocusRing` used to draw a hard-edged rectangle outside the control;
        /// it now sets this instead, so a focused control lifts, scales and grows its shadow
        /// exactly as a hovered one does. `game-ui-design`'s `missing-focus-visible` asks for a
        /// visible indicator and does not ask for a NEW one.
        ///
        /// ⚠️ IT IS SEPARATE FROM `_hovered` RATHER THAN FOLDED INTO IT, because the two are
        /// cleared by different events: the pointer clears the first on exit and the EventSystem
        /// clears the second on deselect, and a control can genuinely be both. Merging them would
        /// make moving the mouse off a keyboard-focused button drop the focus mark.
        /// </summary>
        private bool _focused;

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

        /// <summary>
        /// Whether the lettering has already been lifted onto the face.
        ///
        /// ⚠️⚠️ IT IS A LATCH BECAUSE THE CORRECTION IS AN ADDITION AND `PaperDress.Screen` RUNS
        /// AFTER EVERY TAB PRESS AND EVERY DRAWER OPEN. Applying it twice walks the words three
        /// units up the control on every redraw, which is the failure `PaperKit.CentreOnFace`'s
        /// own note warns about from the other side.
        /// </summary>
        private bool _facedLabel;

        /// <summary>
        /// Lifts the lettering off the cast shadow so it is centred on the FACE, not on the rect.
        ///
        /// ⚠️⚠️ HE SAW THIS BY EYE ON A TAB RAIL AND HE IS RIGHT TO THE PIXEL. 2026-09-02, with a
        /// crop of the fighter picker's four tabs: **"problem wiht this is they arent centered"**,
        /// then, in case it was not clear, *"look it droops a bit down more"*.
        ///
        /// **Every raised paper surface draws its cast shadow inside its own bottom
        /// `PaperCraft.Drop` units**, so a 56-unit control is a 50-unit face with 6 units of
        /// shadow under it. A label centred on the RECT is therefore centred three units below the
        /// middle of the thing it is printed on, on every control, forever. Small enough to
        /// survive review and big enough to see, which is exactly the class `docs/TODO.md` § 120.2
        /// records for BACK: *"BACK sat twelve units below every other chip in the game and both
        /// lines looked equally reasonable in review."*
        ///
        /// ⚠️ `PaperKit.Chip` AND `MakeAction` DO THIS AT BUILD TIME through `CentreOnFace`, which
        /// raises the box's BOTTOM edge. This is for everything that arrives through `PaperDress`
        /// instead: a `GodotButton` converted from a `.tscn` or from `MenuKit.WoodButton`, which
        /// is every tab in the game, CLOSE, the hub's column, the picker's rail and the maker's
        /// two rows. **None of them had ever had it.**
        /// </summary>
        public void CentreLabelOnFace()
        {
            if (_facedLabel) return;
            _facedLabel = true;

            if (_label == null)
                _label = transform.Find("Label") != null
                    ? transform.Find("Label").GetComponent<Text>()
                    : GetComponentInChildren<Text>();

            if (_label == null) return;

            _label.rectTransform.anchoredPosition +=
                new Vector2(0.0f, PaperCraft.Drop * 0.5f);

            // ⚠️ THE REST POSITION IS FORGOTTEN SO THE ANIMATOR RE-READS IT. `_home` is what the
            // hover and the press are measured from; leaving the old value here would make every
            // press put the lettering back where it used to droop.
            _home = null;
        }

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
        /// Whether the face under the lettering is dark, and therefore wants CREAM words.
        ///
        /// ⚠️⚠️ IT IS SEPARATE FROM <see cref="_live"/> AND MERGING THE TWO WOULD BREAK THE
        /// PRIMARY. `_live` does two jobs: it picks the lettering AND it is the exception that
        /// stops a selected tab ever being drawn "off". An `Action` wants the first and must not
        /// have the second: START MATCH is genuinely unavailable while a room is connecting, and a
        /// primary that cannot draw its own disabled state is a button a player presses into
        /// silence. `docs/TODO.md` 121.1.
        /// </summary>
        /// ⚠️⚠️ AND IT ASKS THE ACCENT NOW, BECAUSE `Surface.Action` STOPPED MEANING "DARK".
        /// For the whole life of this property an action's fill was a dark slab, his authored
        /// brown or his authored green, so the surface alone answered the question. Under
        /// `PaperCraft.Surface.Brand` **both of its fills are light**: Chartreuse `d6ce01` and
        /// Honey Quartz `fcd39f`. Cream lettering on chartreuse measures **1.2:1** and ink on it
        /// measures **9.1:1**, so the old shortcut put the screen's one primary in a colour
        /// nobody could read, which is what `Logs/shots-runtime/Lobby-v81.png` shows.
        ///
        /// ⚠️ A LIVE TAB IS STILL DARK AND STILL TAKES CREAM. `Accent.Dark` is the only fill in
        /// the brand construction that a light letter belongs on, and that is what `_live`
        /// resolves to.
        private bool _darkFace => _live
                                  || (_skin != null
                                      && _skin.Surface == PaperCraft.Surface.Action
                                      && _skin.Accent == PaperCraft.Accent.Dark);

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
        /// <summary>
        /// Told by <see cref="FocusRing"/> when the EventSystem's selection lands here or leaves.
        ///
        /// ⚠️ IT GOES THROUGH `Refresh` RATHER THAN WRITING THE SKIN, so this control keeps
        /// exactly one writer of its own pose. See <see cref="FocusRing.Hold"/> for the other half
        /// of that argument and `docs/TODO.md` § 119.9 row 1 for what two writers cost last time.
        /// </summary>
        public void SetFocused(bool on)
        {
            if (_focused == on) return;
            _focused = on;
            Refresh();
        }

        private void Animate()
        {
            // ⚠️ FOCUS LIFTS THE SAME TWO UNITS A HOVER DOES, deliberately, because it IS the
            // focus indicator now. See `_focused`.
            float wantLift = (_hovered || _focused) && !_held && _wasInteractable ? 1.0f : 0.0f;
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
            // ⚠️ THE FOCUS FLAG CLEARS WITH THE POINTER ONE, for the reason this method's header
            // already gives about the pointer: a control switched off while it holds the
            // EventSystem's selection never gets a deselect, so a drawer closing over a focused
            // chip would bring it back lit. `FocusRing.OnDisable` releases from the other side.
            _focused = false;
            _lift = 0.0f;
            _sink = 0.0f;

            if (_mayScale) transform.localScale = Vector3.one;
            if (_label != null && _home.HasValue)
                _label.rectTransform.anchoredPosition = _home.Value;

            // ⚠️⚠️ THE SURFACE HAS TO GO BACK TOO, AND THIS METHOD WAS FIXING ONLY THE TRANSFORM
            // HALF OF ITS OWN HEADER. 🧑 2026-09-02, with a crop of the lobby's mode tabs:
            // **"theres brown ink left over if i dont hover back to the buttons on top"**. The
            // three lines above put the scale and the lettering back and left the PLATE lit, so a
            // chip that was under the pointer when its drawer closed came back drawn as hovered
            // by something nobody was pointing at. `docs/TODO.md` § 121.2.
            //
            // ⚠️ IT IS SAFE TO WRITE WHILE INACTIVE ONLY BECAUSE `PaperSkin` NOW KEEPS THE POSE
            // IN ITS CACHE KEY. `Rebuild` bails on a zero-height rect, which is exactly what this
            // rect reports on the frame it is switched off, so before that change this line would
            // have been dropped on the floor and the bug would have looked fixed in the source.
            if (_skin != null) _skin.SetPose(PaperCraft.Pose.Rest);
        }

        /// <summary>
        /// ⚠️ THE OTHER END OF `OnDisable`. A control can be switched off by something that is not
        /// a pointer at all (a tab rebuild, `PlayerHub.Show` destroying a list, a drawer), and the
        /// pointer may well be somewhere else entirely by the time it comes back. Re-asserting the
        /// resting pose here means the first frame a control is visible is never mid-animation.
        /// </summary>
        private void OnEnable()
        {
            _hovered = false;
            _held = false;
            _lift = 0.0f;
            _sink = 0.0f;

            if (_skin == null) _skin = GetComponent<PaperSkin>();
            if (_skin != null)
                _skin.SetPose(Available ? PaperCraft.Pose.Rest : PaperCraft.Pose.Off);

            // ⚠️⚠️ AN `Action` RE-SILENCES ITS CHILD GRAPHICS EVERY TIME IT IS SWITCHED ON, AND
            // `Logs/crops/join-v63.png` IS WHY THAT IS NOT PARANOIA. `PaperKit.MakeAction` walks
            // the children once, at build time, and the lobby's JOIN A GAME still came back as
            // 🧑's wooden green slab with its grey halo: that button is built INTO A DRAWER THAT
            // IS SWITCHED OFF, so its `GodotButton` and its `SkinLayers` had not run yet when the
            // walk happened, and the layers appeared the first time the drawer opened.
            //
            // **A one-shot cleanup cannot cover an object whose layers are created lazily**, and
            // the whole class of fault this pass keeps meeting is a second writer arriving after
            // the conversion (§ 119.9 row 1, § 120.5 row 1, and this). Re-running it on enable
            // costs a walk of three or four children on the frame a drawer opens.
            if (_skin != null && _skin.Surface == PaperCraft.Surface.Action)
                SilenceChildGraphics(gameObject);
        }

        /// <summary>
        /// Switches off every graphic below a node except its own surface.
        ///
        /// ⚠️ AN `Action` OWNS EXACTLY ONE SURFACE BY DEFINITION, so this is a rule rather than a
        /// list of layer names. `PaperKit.Paperise` names `Face` and `Shadow`; `ArrowButtonView`
        /// builds `Artwork`, `Lit` and `Rim`; the next component to draw a layer will name it
        /// something else again, and a list is a list somebody has to remember to extend.
        /// </summary>
        internal static void SilenceChildGraphics(GameObject target)
        {
            if (target == null) return;

            foreach (var graphic in target.GetComponentsInChildren<Image>(true))
                if (graphic != null && graphic.gameObject != target) graphic.enabled = false;

            foreach (var raw in target.GetComponentsInChildren<RawImage>(true))
                if (raw != null && raw.gameObject != target) raw.enabled = false;
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

        /// <summary>
        /// Leaves the lettering the colour the caller set.
        ///
        /// ⚠️⚠️ ONE CONTROL IN THE GAME USES IT AND IT IS `LEAVE GAME`. Every other paper chip
        /// wants its type decided by its surface, which is the whole reason `TintLabel` reads the
        /// surface rather than being told; a DESTRUCTIVE action is the one case where the word
        /// itself carries a meaning the plate does not. `GodotTheme.WoodDangerButton` said it with
        /// a red slab, and on cream a red slab beside a green primary is two saturated rectangles
        /// arguing. One red WORD on a plain token is the same statement at a tenth of the area.
        ///
        /// ⚠️ IT IS A FLAG RATHER THAN A COLOUR FIELD ON PURPOSE. A colour here would be a second
        /// place that decides what a paper control's type looks like, which is exactly the
        /// five-copies problem `PaperKit.MarkLive` was written to end.
        /// </summary>
        public bool KeepLabelColour;

        private void TintLabel()
        {
            if (_label == null || KeepLabelColour) return;

            bool on = Available;

            // ⚠️ A DISABLED ACTION KEEPS CREAM LETTERING RATHER THAN DROPPING TO SOFT INK. The
            // slab desaturates towards the sheet but stays dark (see `PaintAction`), so soft ink
            // on it measures worse than the 4.5:1 floor, and "unavailable" is already said by the
            // colour of the plate.
            _label.color = _darkFace ? UiTheme.Cream
                : !on ? UiTheme.PaperInkSoft
                : UiTheme.PaperInk;
        }

        private void Refresh()
        {
            if (_skin == null) _skin = GetComponent<PaperSkin>();
            if (_skin == null) return;

            bool on = Available;

            _skin.SetPose(!on ? PaperCraft.Pose.Off
                          : _held ? PaperCraft.Pose.Press
                          : _hovered || _focused ? PaperCraft.Pose.Hover
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
