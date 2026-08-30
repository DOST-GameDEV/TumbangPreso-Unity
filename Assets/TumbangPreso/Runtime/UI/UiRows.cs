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
        /// Where the value and control column begins, as a fraction of the row's width.
        ///
        /// ⚠️⚠️ THIS IS THE FIX FOR THE ONE THING THAT MADE THE CAREER TAB UNREADABLE, AND IT IS
        /// NOT A TASTE CALL. 🧑, on the shipped screen: *"its so messy and ugly"*, *"easier to
        /// process"*. Rows are full width, and the value was pinned HARD RIGHT, so on a 1920 px
        /// screen `Matches played` sat at x = 145 and its `12` sat at x = 1770. **A label and its
        /// value 1600 px apart are not a row, they are two separate things on the same line**, and
        /// the eye has to traverse the whole screen and come back for every one of thirty of them.
        /// The zebra band was carrying that entire journey on its own.
        ///
        /// ⚠️ THE REFERENCES DO NOT ACTUALLY PUT THE VALUE HARD RIGHT AND THAT IS WHAT WAS
        /// MISREAD. Valorant's and PUBG's settings rows put a WIDE CONTROL, a dropdown or a
        /// slider, in a fixed right-hand column, so it fills the space and reads as one object
        /// with its label. A bare two-character number does not fill anything. **What survives
        /// the copy is the COLUMN, not the alignment**: everything on the right starts at the
        /// same x, whatever it is, and the values form a readable second column instead of a
        /// ragged right margin.
        ///
        /// ⚠️ IT IS A FRACTION RATHER THAN A PIXEL OFFSET so it holds at all nine resolutions
        /// `PlayerHubLayoutProbe` drives. A hand-written offset here would be the same fault this
        /// whole file was written to make impossible, one level up.
        /// </summary>
        public const float ValueColumn = 0.56f;

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
            //
            // ⚠️⚠️ BUT A CLOSED GROUP IS ONE LINE NOW RATHER THAN THREE, AND THAT IS THE WHOLE
            // POINT OF CLOSING IT. The subtitle sat UNDER the heading whether the group was open
            // or shut, so a shut group cost 96 px plus 18 of gap plus a rule to say nothing: the
            // career tab's six shut groups spent about 680 px, most of a screen, on headings. The
            // sentence moves onto the heading's own line, muted, in the value column, so a shut
            // group is a row like every other row and six of them fit where two did.
            // 🧑: *"theres liek 20 shits at once"*, and a wall of headings is the same complaint
            // with the numbers taken out.
            bool shut = open == false;
            bool stacked = !string.IsNullOrEmpty(subtitle) && !shut;

            float height = stacked ? 96.0f : 68.0f;
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
                if (stacked)
                {
                    var note = MenuKit.Label(go.transform, subtitle, HintUnits, UiTheme.CreamMuted,
                        new Vector2(0.0f, 1.0f), new Vector2(SidePadding + 420.0f, top - 54.0f),
                        new Vector2(840.0f, 28.0f), TextAnchor.MiddleLeft);
                    note.raycastTarget = false;
                }
                else
                {
                    // ⚠️ IT LANDS IN `ValueColumn`, THE SAME COLUMN EVERY ROW'S VALUE USES, so a
                    // shut group's sentence lines up with the numbers above and below it rather
                    // than starting at some third x nothing else shares.
                    var noteGo = new GameObject("Summary", typeof(RectTransform));
                    noteGo.transform.SetParent(go.transform, false);

                    var rt = (RectTransform)noteGo.transform;
                    rt.anchorMin = new Vector2(ValueColumn, 1.0f);
                    rt.anchorMax = new Vector2(1.0f, 1.0f);
                    rt.pivot = new Vector2(0.5f, 1.0f);
                    rt.offsetMin = new Vector2(0.0f, top - 44.0f);
                    rt.offsetMax = new Vector2(-SidePadding, top - 8.0f);

                    var note = MenuKit.Label(noteGo.transform, subtitle, HintUnits,
                        UiTheme.CreamMuted, new Vector2(0.5f, 0.5f), Vector2.zero,
                        new Vector2(600.0f, 28.0f), TextAnchor.MiddleLeft);
                    MenuKit.Stretch(note.rectTransform);
                    note.alignment = TextAnchor.MiddleLeft;
                    note.raycastTarget = false;
                }
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
            // ⚠️⚠️ THE STRIPE RESTARTS AT EVERY SECTION HEADER, AND IT USED TO RUN THROUGH THEM.
            // It counted every `Row_` in the whole list, so whether a group's first row was
            // banded depended on how many rows the groups ABOVE it happened to have, and on the
            // career tab that changes with the player's own history: a group opens shaded on one
            // account and clear on another. **Banding is meant to say "these rows belong
            // together"**, and a stripe that ignores the boundary says the opposite. Counting
            // back to the last section makes every group start the same way.
            int index = 0;
            for (int i = list.childCount - 1; i >= 0; i--)
            {
                string name = list.GetChild(i).name;
                if (name.StartsWith("Section_")) break;
                if (name.StartsWith("Row_")) index++;
            }

            var go = new GameObject($"Row_{label}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(list, false);

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

            // ⚠️⚠️ THE SLOT IS A COLUMN NOW, NOT A BOX ON THE RIGHT MARGIN. It used to be
            // `controlWidth` wide, pinned to the row's right edge, which put every value at a
            // different distance from its label depending on how wide the caller asked for. See
            // `ValueColumn` for the measurement that produced this. `controlWidth` is kept in the
            // signature because `ButtonRow` still wants a button that is button-sized rather than
            // one that fills 44 per cent of the screen; everything else stretches into the column.
            var slot = (RectTransform)slotGo.transform;
            slot.anchorMin = new Vector2(ValueColumn, 0.5f);
            slot.anchorMax = new Vector2(1.0f, 0.5f);
            slot.pivot = new Vector2(0.5f, 0.5f);
            slot.offsetMin = new Vector2(0.0f, -(RowHeight - 14.0f) * 0.5f);
            slot.offsetMax = new Vector2(-SidePadding, (RowHeight - 14.0f) * 0.5f);
            return slot;
        }

        /// <summary>
        /// Pins a control to the LEFT of its value column and caps how wide it may grow.
        ///
        /// ⚠️⚠️ WITHOUT THIS, EVERY CONTROL FILLED THE WHOLE COLUMN, AND THE FIRST RENDER OF THE
        /// COLUMN SHOWED WHY THAT IS WRONG. `Row`'s slot spans `ValueColumn` to the right margin,
        /// which is about 715 px at 1920, so `MenuKit.Stretch` gave the display-name field, a box
        /// for **fourteen characters**, a 715 px white rectangle, and gave the CLASSIC / HERO
        /// STRIKE picker the same. **The loudest thing on the screen was the widest control
        /// rather than the most important one**, which is the hierarchy fault 🧑 named in the
        /// first place, arriving by a different route.
        ///
        /// ⚠️ THE CAP IS A CAP AND NOT A SIZE. A control is anchored at the column's left edge
        /// and given its authored width, so at 4:3 the column is about 368 px and every width
        /// this file hands out still fits inside it. Widen one past that and it overhangs the
        /// row, which is why they are all under 368.
        /// </summary>
        private static void Cap(RectTransform rt, float width)
        {
            rt.anchorMin = new Vector2(0.0f, 0.0f);
            rt.anchorMax = new Vector2(0.0f, 1.0f);
            rt.pivot = new Vector2(0.0f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(width, 0.0f);
        }

        /// <summary>A row whose right-hand side is a read-only value. The career page is made of
        /// these.</summary>
        /// <remarks>
        /// ⚠️⚠️ THE VALUE IS LEFT-ALIGNED IN ITS COLUMN, WHICH IS THE OPPOSITE OF WHAT SHIPPED.
        /// Right-aligning it against the screen edge is what put `12` 1600 px from
        /// `Matches played`. Aligning every value to the LEFT of one column means they line up
        /// with each other AND sit a fixed, short distance from their labels, which is both of
        /// the things the reader needs. ⚠️ It costs the one thing right-alignment buys, digits
        /// lining up by place value, and that trade is deliberate: these are counts of matches
        /// and percentages, two to four characters, never a column of currency.
        /// </remarks>
        public static Text ValueRow(RectTransform list, string label, string value,
                                    string hint = "", Color? colour = null)
        {
            var slot = Row(list, label, hint, 520.0f);
            var text = MenuKit.Label(slot, value, LabelUnits, colour ?? UiTheme.Cream,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520.0f, 30.0f),
                TextAnchor.MiddleLeft);
            MenuKit.Stretch(text.rectTransform);
            text.alignment = TextAnchor.MiddleLeft;
            return text;
        }

        /// <summary>
        /// A row whose value is several small labelled cells across the column, evenly spaced.
        ///
        /// ⚠️⚠️ IT EXISTS BECAUSE ONE ROW ON THE CAREER TAB WAS CARRYING FOUR NUMBERS IN A
        /// SINGLE STRING. `Finishes` read `1st 3   2nd 3   3rd 3   4th 3` as one right-aligned
        /// value, so four separate facts arrived as one long word with no structure, and the
        /// spacing between them was whatever the font did with the spaces. A distribution is
        /// four values, so it gets four cells; each cell is its own caption over its own number,
        /// and they line up whatever the numbers are.
        ///
        /// ⚠️ IT IS NOT A CHART AND MUST NOT BECOME ONE. Four bars would need a scale, and a
        /// scale needs a maximum, and a maximum on a four-outcome distribution of a dozen matches
        /// is noise drawn at full height. `VISION.md` § 2's rule about spending the budget on
        /// detail rather than area is the same argument one screen over.
        /// </summary>
        public static void DistributionRow(RectTransform list, string label, string[] captions,
                                           string[] values, string hint = "")
        {
            var slot = Row(list, label, hint);
            int cells = Mathf.Min(captions.Length, values.Length);
            if (cells <= 0) return;

            // ⚠️ THE CELLS SHARE ONE CAPPED STRIP RATHER THAN THE WHOLE COLUMN. Spread across
            // 715 px at 1920 the four outcomes read as four unrelated numbers scattered along a
            // line; in 340 they read as one distribution, which is what they are.
            var stripGo = new GameObject("Cells", typeof(RectTransform));
            stripGo.transform.SetParent(slot, false);

            var strip = (RectTransform)stripGo.transform;
            Cap(strip, 340.0f);

            for (int i = 0; i < cells; i++)
            {
                var cellGo = new GameObject($"Cell{i}", typeof(RectTransform));
                cellGo.transform.SetParent(strip, false);

                var cell = (RectTransform)cellGo.transform;
                cell.anchorMin = new Vector2(i / (float)cells, 0.0f);
                cell.anchorMax = new Vector2((i + 1) / (float)cells, 1.0f);
                cell.offsetMin = Vector2.zero;
                cell.offsetMax = new Vector2(-12.0f, 0.0f);

                var caption = MenuKit.Label(cell, captions[i], HintUnits, UiTheme.CreamMuted,
                    new Vector2(0.0f, 0.5f), new Vector2(0.0f, 13.0f), new Vector2(120.0f, 22.0f),
                    TextAnchor.MiddleLeft);
                MenuKit.Stretch(caption.rectTransform);
                caption.rectTransform.anchorMin = new Vector2(0.0f, 0.5f);
                caption.rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
                caption.rectTransform.offsetMin = Vector2.zero;
                caption.rectTransform.offsetMax = Vector2.zero;
                caption.alignment = TextAnchor.LowerLeft;

                var number = MenuKit.Label(cell, values[i], LabelUnits, UiTheme.Cream,
                    new Vector2(0.0f, 0.5f), Vector2.zero, new Vector2(120.0f, 26.0f),
                    TextAnchor.MiddleLeft);
                number.rectTransform.anchorMin = Vector2.zero;
                number.rectTransform.anchorMax = new Vector2(1.0f, 0.5f);
                number.rectTransform.offsetMin = Vector2.zero;
                number.rectTransform.offsetMax = Vector2.zero;
                number.alignment = TextAnchor.UpperLeft;
            }
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
            var slot = Row(list, label, hint, 360.0f);

            var go = new GameObject("Field", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(slot, false);
            Cap((RectTransform)go.transform, 360.0f);

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

            // ⚠️ THE BUTTON STARTS AT THE COLUMN LIKE EVERY OTHER VALUE, NOT CENTRED IN IT. It
            // was anchored at the slot's midpoint, so on the ACCOUNT tab SET ONE UP sat 225 px
            // right of the column every number and every dropdown on the same screen starts at.
            // One column or none.
            var btn = MenuKit.WoodButton(slot, button, new Vector2(0.0f, 0.5f),
                                         new Vector2(130.0f, 0.0f),
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
            Cap((RectTransform)go.transform, 340.0f);

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
