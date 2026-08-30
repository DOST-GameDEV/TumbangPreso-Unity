using System;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The row, section and field kit every settings-shaped screen is built from.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE ABSOLUTE OFFSETS ARE WHAT MADE THE ACCOUNT AND CAREER SCREENS
    /// OVERLAP THEMSELVES. `AccountOverlay` placed six captions and six fields at hand-written Y
    /// offsets (-196, -286, -376, -466, -566, -656) inside an 870 px panel, and `ProfileOverlay`
    /// did the same with a 940 px one. 🧑 photographed both: the career page ran its buttons off
    /// the bottom of the screen and drew a stray CLASSIC label straight through the HERO STRIKE
    /// tab. **A hand-written Y offset is a layout that is correct at exactly one panel height and
    /// one aspect ratio**, and `AspectRatioProbes` drives nine of them.
    ///
    /// ⚠️⚠️ AND THE SHAPE IS COPIED FROM WHAT 🧑 ASKED FOR BY NAME. Shown Valorant's and PUBG's
    /// settings screens: *"look at how valorant settings look like"*. Both are the same thing and
    /// neither is a form. **A full-width row, the label hard left, the control hard right, a
    /// section header with one grey line of explanation above each group, and alternating row
    /// bands so the eye can track across.** No cards, no centred columns, no grid of equal
    /// buttons. That is what this file makes, and it is why nothing here takes an offset.
    ///
    /// ⚠️ ZEBRA BANDING IS NOT DECORATION. At 1480 px wide the label and its control are half a
    /// screen apart, and the band is the only thing joining them. Valorant's settings list is
    /// unreadable without it and so is this one.
    /// </summary>
    public static class UiRows
    {
        /// <summary>
        /// ⚠️⚠️ THE WHOLE TYPE SCALE IS SET BY `MenuKit.MinReadableUnits`, WHICH IS 18, AND
        /// THE FIRST RUN OF `PlayerHubLayoutProbe` FAILED THIS FILE ON IT. The rows were written
        /// with a 17-unit label and a 13-unit hint, which is the scale the reference screenshots
        /// use and is below this project's measured floor. `AspectRatioProbes` fails anything
        /// under 18 and `ui_theme.gd` records three separate attempts at small text, each
        /// answered with *"text still small"*.
        ///
        /// ⚠️⚠️ AND RAISING THE HINT ALONE WOULD HAVE FLATTENED THE HIERARCHY. A 17-unit
        /// label above an 18-unit hint says the hint is the more important of the two, which is
        /// the opposite of the design. **The floor moves the whole scale, not one end of it:**
        /// heading 26, label and value 22, subtitle and hint 18. Three steps, all legible, in the
        /// same order as before.
        /// </summary>
        public const int HeadingUnits = 26;
        public const int LabelUnits = 22;
        public const int HintUnits = MenuKit.MinReadableUnits;

        /// <summary>⚠️ 64 RATHER THAN 56, BECAUSE THE SCALE GREW. A 22-unit label stacked over
        /// an 18-unit hint needs 24 units of stacked half-height plus air, and a row that does not
        /// grow with its type is a row whose two lines touch.</summary>
        public const float RowHeight = 64.0f;
        public const float SidePadding = 28.0f;

        /// <summary>
        /// ⚠️⚠️ 3.5 PER CENT, MEASURED OFF THE FIRST RENDER RATHER THAN PICKED. It was written
        /// at 6 per cent against the wood panel this kit was designed for, and the hub does not
        /// have one: it is a 93 per cent scrim over the live street, which is much darker, so the
        /// same alpha came out as **solid grey blocks** rather than as a band. The first
        /// screenshot of the career tab reads as a striped table, which is the spreadsheet look
        /// the banding exists to avoid.
        ///
        /// ⚠️ A NUMBER TUNED AGAINST ONE BACKGROUND IS NOT A NUMBER. If this kit is ever put on
        /// a light surface it needs measuring again, and the render is how you measure it.
        /// </summary>
        private static readonly Color Band = new Color(1.0f, 1.0f, 1.0f, 0.035f);

        /// <summary>
        /// A vertical list that grows with its content and scrolls when it outgrows the panel.
        ///
        /// ⚠️ IT SCROLLS RATHER THAN SHRINKING, which is the other half of the overflow fix. The
        /// career page has fifteen stat rows in Classic and will have more; a panel that tries to
        /// fit everything is a panel whose rows get shorter until nothing is legible.
        /// </summary>
        public static RectTransform ScrollList(Transform parent, string name, out ScrollRect scroll)
        {
            var viewGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Mask));
            viewGo.transform.SetParent(parent, false);

            // ⚠️⚠️ THE MASK GRAPHIC MUST NOT BE FULLY TRANSPARENT, AND THIS SHIPPED AT ALPHA
            // ZERO FIRST. A `Mask` writes its stencil from the graphic it sits on, so an Image at
            // alpha 0 masks EVERYTHING OUT: the viewport draws, the rows exist, every layout
            // number is correct, and the list is simply invisible. **The first screenshot of this
            // screen showed the header, the tabs, the level bar and the SAVE button over an empty
            // brown field**, which reads as a layout fault and is not one.
            //
            // ⚠️⚠️ AND THE LAYOUT PROBE PASSED IT, which is worse than the bug. It asserted
            // that SOME labels were measured, and the header and the tab bar are labels, so an
            // entirely empty list cleared the bar. `PlayerHubLayoutProbe` now counts rows in the
            // LIST specifically. A test that cannot fail on an empty screen is not a test of that
            // screen.
            //
            // ⚠️ 0.01 IS THE SMALLEST ALPHA UNITY STILL STENCILS FROM. `DropdownRow`'s viewport
            // uses the same value for the same reason.
            viewGo.GetComponent<Image>().color = new Color(0.0f, 0.0f, 0.0f, 0.01f);
            viewGo.GetComponent<Mask>().showMaskGraphic = false;

            scroll = viewGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32.0f;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewGo.transform, false);

            var content = (RectTransform)contentGo.transform;
            content.anchorMin = new Vector2(0.0f, 1.0f);
            content.anchorMax = new Vector2(1.0f, 1.0f);
            content.pivot = new Vector2(0.5f, 1.0f);
            content.offsetMin = new Vector2(0.0f, 0.0f);
            content.offsetMax = new Vector2(0.0f, 0.0f);

            var group = contentGo.AddComponent<VerticalLayoutGroup>();
            group.childControlHeight = true;
            group.childControlWidth = true;
            group.childForceExpandHeight = false;
            group.childForceExpandWidth = true;
            group.spacing = 0.0f;

            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = (RectTransform)viewGo.transform;
            scroll.content = content;
            return content;
        }

        /// <summary>
        /// A section header and the one grey line under it that says what the group is for.
        ///
        /// ⚠️ THE SUBTITLE IS THE PART WORTH COPYING. PUBG writes *"Camera Sensitivity (Affects
        /// the sensitivity of the camera when the screen is swiped without firing.)"* next to the
        /// heading, so a player never has to guess what a group of rows has in common. A heading
        /// on its own is a label; a heading plus one sentence is an explanation.
        /// </summary>
        public static void Section(RectTransform list, string title, string subtitle = "",
                                   bool? open = null, Action onToggle = null)
        {
            bool first = list.childCount == 0;

            var go = new GameObject($"Section_{title}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(list, false);

            // ⚠️⚠️ A CLOSED GROUP IS NOT BUILT, IT IS NOT HIDDEN, and that is what makes this
            // cheap enough to use everywhere. \U0001f9d1: *"usually to make shit easier to navigate games
            // use dropdownns and shit annd separate shit"*. The tabs are already rebuilt on every
            // switch (`PlayerHub.Show`), so a caller simply does not add the rows of a group the
            // player has closed. There is no hidden subtree recomputing layout, nothing to keep
            // in sync, and the scroll height is honest about what is on screen.
            var face = go.GetComponent<Image>();
            face.color = new Color(0, 0, 0, 0);
            face.raycastTarget = onToggle != null;

            if (onToggle != null)
            {
                var button = go.AddComponent<Button>();
                button.targetGraphic = face;
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => onToggle());
            }

            // ⚠️ A CLOSED GROUP KEEPS ITS SUBTITLE. It is the one line telling the player
            // what is inside, so hiding it with the rows would leave a list of bare headings and
            // make the closed state harder to read than the open one.
            float height = string.IsNullOrEmpty(subtitle) ? 68.0f : 96.0f;
            var element = go.AddComponent<LayoutElement>();
            element.minHeight = first ? height : height + 18.0f;
            element.preferredHeight = element.minHeight;

            float top = first ? -8.0f : -26.0f;

            // ⚠️ ASCII, NOT A CHEVRON GLYPH. The font is Darumadrop One and `docs/TODO.md`
            // records that it has no multiplication sign; assuming it has arrows is the same bet
            // one step further on, and a missing glyph draws as an empty box on the one row whose
            // whole job is to say whether the group is open.
            string mark = open == null ? "" : (open.Value ? "-  " : "+  ");

            var heading = MenuKit.Label(go.transform, mark + title.ToUpperInvariant(), HeadingUnits,
                UiTheme.Amber, new Vector2(0.0f, 1.0f),
                new Vector2(SidePadding + 190.0f, top - 20.0f),
                new Vector2(380.0f, 34.0f), TextAnchor.MiddleLeft);
            heading.raycastTarget = false;
            heading.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            if (!string.IsNullOrEmpty(subtitle))
            {
                var note = MenuKit.Label(go.transform, subtitle, HintUnits, UiTheme.CreamMuted,
                    new Vector2(0.0f, 1.0f), new Vector2(SidePadding + 420.0f, top - 54.0f),
                    new Vector2(840.0f, 28.0f), TextAnchor.MiddleLeft);
                note.raycastTarget = false;
            }

            Rule(go.transform);
        }

        /// <summary>A hairline under a section heading, the width of the list.</summary>
        private static void Rule(Transform parent)
        {
            var go = new GameObject("Rule", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.0f, 0.0f);
            rt.anchorMax = new Vector2(1.0f, 0.0f);
            rt.pivot = new Vector2(0.5f, 0.0f);
            rt.offsetMin = new Vector2(SidePadding, 0.0f);
            rt.offsetMax = new Vector2(-SidePadding, 2.0f);

            go.GetComponent<Image>().color = new Color(UiTheme.WoodEdge.r, UiTheme.WoodEdge.g,
                                                       UiTheme.WoodEdge.b, 0.5f);
        }

        /// <summary>
        /// One full-width row: the label hard left, whatever the caller puts in it hard right.
        ///
        /// ⚠️ THE CONTROL AREA IS RETURNED RATHER THAN BUILT HERE, so a row can hold a field, a
        /// value, a toggle or a button without this file knowing about any of them. It is a
        /// stretched rect on the right, so the control sizes itself against the row rather than
        /// against a number somebody typed.
        /// </summary>
        public static RectTransform Row(RectTransform list, string label, string hint = "",
                                        float controlWidth = 420.0f)
        {
            int index = 0;
            for (int i = 0; i < list.childCount; i++)
                if (list.GetChild(i).name.StartsWith("Row_")) index++;

            var go = new GameObject($"Row_{label}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(list, false);

            // ⚠️ THE BAND ALTERNATES ON THE ROW INDEX, NOT THE CHILD INDEX, so a section header
            // in the middle of a list does not flip the stripe and restart the pattern.
            go.GetComponent<Image>().color = index % 2 == 0 ? Band : new Color(0, 0, 0, 0);

            var element = go.AddComponent<LayoutElement>();
            element.minHeight = RowHeight;
            element.preferredHeight = RowHeight;

            var caption = MenuKit.Label(go.transform, label, LabelUnits, UiTheme.Cream,
                new Vector2(0.0f, 0.5f), new Vector2(SidePadding + 210.0f, 0.0f),
                new Vector2(420.0f, 32.0f), TextAnchor.MiddleLeft);
            MenuKit.Fit(caption, 410.0f);

            if (!string.IsNullOrEmpty(hint))
            {
                caption.rectTransform.anchoredPosition = new Vector2(SidePadding + 210.0f, 14.0f);

                // ⚠️ THE HINT WRAPS AND THE LABEL DOES NOT. A hint is a sentence and a label is
                // a noun; a sentence given one line either overflows its neighbour or gets cut,
                // and `MenuKit.Fit` shrinking it would take it under the readable floor.
                var note = MenuKit.Label(go.transform, hint, HintUnits, UiTheme.CreamMuted,
                    new Vector2(0.0f, 0.5f), new Vector2(SidePadding + 400.0f, -15.0f),
                    new Vector2(800.0f, 24.0f), TextAnchor.MiddleLeft);
                note.horizontalOverflow = HorizontalWrapMode.Wrap;
            }

            var slotGo = new GameObject("Control", typeof(RectTransform));
            slotGo.transform.SetParent(go.transform, false);

            var slot = (RectTransform)slotGo.transform;
            slot.anchorMin = new Vector2(1.0f, 0.5f);
            slot.anchorMax = new Vector2(1.0f, 0.5f);
            slot.pivot = new Vector2(1.0f, 0.5f);
            slot.anchoredPosition = new Vector2(-SidePadding, 0.0f);
            slot.sizeDelta = new Vector2(controlWidth, RowHeight - 14.0f);
            return slot;
        }

        /// <summary>A row whose right-hand side is a read-only value. The career page is made of
        /// these.</summary>
        public static Text ValueRow(RectTransform list, string label, string value,
                                    string hint = "", Color? colour = null)
        {
            var slot = Row(list, label, hint, 520.0f);
            var text = MenuKit.Label(slot, value, LabelUnits, colour ?? UiTheme.Cream,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520.0f, 30.0f),
                TextAnchor.MiddleRight);
            MenuKit.Stretch(text.rectTransform);
            text.alignment = TextAnchor.MiddleRight;
            return text;
        }

        /// <summary>
        /// A text field sized to its row.
        ///
        /// ⚠️ THE MICRO-LABEL IS THE ROW'S LABEL AND THERE IS NO SECOND CAPTION. The old panel
        /// drew a caption on the left AND a placeholder repeating it inside the box
        /// ("COUNTRY CODE (OPTIONAL)" beside a field reading "country code (optional)"), which is
        /// the same words twice and is most of why that screen felt like twenty things at once.
        /// The placeholder here is an EXAMPLE of valid input, which is what a placeholder is for.
        /// </summary>
        public static InputField FieldRow(RectTransform list, string label, string placeholder,
                                          int limit, string hint = "", bool password = false)
        {
            var slot = Row(list, label, hint, 460.0f);

            var go = new GameObject("Field", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(slot, false);
            MenuKit.Stretch((RectTransform)go.transform);

            var image = go.GetComponent<Image>();
            image.color = UiTheme.Card;

            var skin = go.AddComponent<GodotPanel>();
            skin.Variation = "Card";
            skin.ApplyContentMargins = false;
            skin.Apply();

            var input = go.AddComponent<InputField>();
            input.targetGraphic = image;
            input.characterLimit = limit;
            input.lineType = InputField.LineType.SingleLine;
            if (password) input.contentType = InputField.ContentType.Password;

            var text = MenuKit.Label(go.transform, "", LabelUnits, UiTheme.Ink,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
            MenuKit.Stretch(text.rectTransform, -14.0f);
            text.alignment = TextAnchor.MiddleLeft;
            input.textComponent = text;

            var ghost = MenuKit.Label(go.transform, placeholder, HintUnits, UiTheme.InkMuted,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
            MenuKit.Stretch(ghost.rectTransform, -14.0f);
            ghost.alignment = TextAnchor.MiddleLeft;
            input.placeholder = ghost;

            return input;
        }

        /// <summary>A row whose control is a single button, for one-per-row actions like SIGN
        /// OUT. ⚠️ Never more than one: a row with three buttons in it is the old panel again.</summary>
        public static Button ButtonRow(RectTransform list, string label, string button,
                                       Action onClick, string hint = "",
                                       string variation = "WoodButton")
        {
            var slot = Row(list, label, hint, 260.0f);
            var btn = MenuKit.WoodButton(slot, button, new Vector2(0.5f, 0.5f), Vector2.zero,
                                         new Vector2(260.0f, RowHeight - 16.0f), onClick, variation);
            return btn;
        }

        /// <summary>
        /// Makes a whole row clickable, for a list whose rows open something.
        ///
        /// ⚠️ THE ROW IS THE TARGET, NOT A BUTTON INSIDE IT. A 1400 px row with a small OPEN
        /// button on the right is a small target at the far end of a long line, and every list
        /// like this in every game opens on the row. The row already has an `Image` for the zebra
        /// band, so this costs one component and no extra geometry.
        ///
        /// ⚠️ AND IT KEEPS THE BAND. `Button` tints its `targetGraphic` on hover, so handing it
        /// the band image would make alternate rows flash a different colour from each other. The
        /// transition is set to None and the hover is left to the row staying legible.
        /// </summary>
        public static void RowButton(RectTransform slot, Action onClick)
        {
            var row = slot != null ? slot.parent : null;
            if (row == null || onClick == null) return;

            var image = row.GetComponent<Image>();
            if (image == null) return;

            image.raycastTarget = true;

            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => onClick());
        }

        /// <summary>
        /// A row whose control is a dropdown.
        ///
        /// ⚠️⚠️ IT REPLACES A PAIR OF BUTTONS THAT USED TO OVERLAP EACH OTHER. The career
        /// page picked its mode with two wood buttons side by side, and \U0001f9d1 photographed the result:
        /// a stray CLASSIC label drawn straight through the HERO STRIKE button. **Two buttons is
        /// the wrong control for one choice out of a set** regardless of the overlap: it does not
        /// say which is selected without a second colour, it does not scale past two, and it takes
        /// as much width as there are options.
        ///
        /// ⚠️ THE OPTIONS ARE STRINGS AND THE CALLBACK GETS AN INDEX, so the caller owns what
        /// the choice means. Nothing about a mode, a season or a filter belongs in this file.
        /// </summary>
        public static Dropdown DropdownRow(RectTransform list, string label, string[] options,
                                           int index, Action<int> onChange, string hint = "")
        {
            var slot = Row(list, label, hint, 340.0f);

            var go = new GameObject("Dropdown", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(slot, false);
            MenuKit.Stretch((RectTransform)go.transform);

            var face = go.GetComponent<Image>();
            face.color = UiTheme.Card;

            var skin = go.AddComponent<GodotPanel>();
            skin.Variation = "Card";
            skin.ApplyContentMargins = false;
            skin.Apply();

            var drop = go.AddComponent<Dropdown>();
            drop.targetGraphic = face;

            var caption = MenuKit.Label(go.transform, "", LabelUnits, UiTheme.Ink,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
            MenuKit.Stretch(caption.rectTransform, -16.0f);
            caption.alignment = TextAnchor.MiddleLeft;
            drop.captionText = caption;

            // ⚠️ THE TEMPLATE IS BUILT BY HAND BECAUSE A CODE-BUILT `Dropdown` HAS NONE.
            // `AddComponent<Dropdown>` gives a control with a null template, and the failure mode
            // is silent: it draws its caption correctly and simply does nothing when pressed,
            // which is the same class of bug `MenuKit.EnsureHitArea` records about four sliders
            // that shipped dead.
            var templateGo = new GameObject("Template", typeof(RectTransform), typeof(Image));
            templateGo.transform.SetParent(go.transform, false);

            var template = (RectTransform)templateGo.transform;
            template.anchorMin = new Vector2(0.0f, 0.0f);
            template.anchorMax = new Vector2(1.0f, 0.0f);
            template.pivot = new Vector2(0.5f, 1.0f);
            template.anchoredPosition = Vector2.zero;
            template.sizeDelta = new Vector2(0.0f, 180.0f);
            templateGo.GetComponent<Image>().color = UiTheme.WoodDeep;

            var scroll = templateGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image),
                                            typeof(Mask));
            viewportGo.transform.SetParent(templateGo.transform, false);
            MenuKit.Stretch((RectTransform)viewportGo.transform);
            viewportGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var content = (RectTransform)contentGo.transform;
            content.anchorMin = new Vector2(0.0f, 1.0f);
            content.anchorMax = new Vector2(1.0f, 1.0f);
            content.pivot = new Vector2(0.5f, 1.0f);
            content.sizeDelta = new Vector2(0.0f, RowHeight);

            var itemGo = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            itemGo.transform.SetParent(contentGo.transform, false);
            var item = (RectTransform)itemGo.transform;
            item.anchorMin = new Vector2(0.0f, 0.5f);
            item.anchorMax = new Vector2(1.0f, 0.5f);
            item.sizeDelta = new Vector2(0.0f, RowHeight);

            var itemBg = new GameObject("ItemBackground", typeof(RectTransform), typeof(Image));
            itemBg.transform.SetParent(itemGo.transform, false);
            MenuKit.Stretch((RectTransform)itemBg.transform);
            itemBg.GetComponent<Image>().color = UiTheme.WoodMid;

            var itemLabel = MenuKit.Label(itemGo.transform, "", LabelUnits, UiTheme.Cream,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
            MenuKit.Stretch(itemLabel.rectTransform, -16.0f);
            itemLabel.alignment = TextAnchor.MiddleLeft;

            var toggle = itemGo.GetComponent<Toggle>();
            toggle.targetGraphic = itemBg.GetComponent<Image>();

            scroll.viewport = (RectTransform)viewportGo.transform;
            scroll.content = content;

            drop.template = template;
            drop.itemText = itemLabel;
            templateGo.SetActive(false);

            var list_ = new System.Collections.Generic.List<string>(options);
            drop.ClearOptions();
            drop.AddOptions(list_);
            drop.SetValueWithoutNotify(Mathf.Clamp(index, 0, options.Length - 1));
            drop.RefreshShownValue();

            if (onChange != null) drop.onValueChanged.AddListener(v => onChange(v));
            return drop;
        }

        /// <summary>Vertical air between groups, when a rule would be too loud.</summary>
        public static void Gap(RectTransform list, float height)
        {
            var go = new GameObject("Gap", typeof(RectTransform));
            go.transform.SetParent(list, false);

            var element = go.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
        }
    }
}
