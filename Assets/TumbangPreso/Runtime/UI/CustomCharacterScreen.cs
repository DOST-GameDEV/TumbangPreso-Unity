using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// MAKE YOUR OWN: the three-slot character creator, as a screen a player can actually open.
    ///
    /// ⚠️⚠️ THE THING THIS REPLACES HAD NO SCREEN AT ALL, AND THAT IS THE WHOLE ENTRY IN
    /// `docs/TODO.md` § 108. `CustomCharacterCreator` was a `MonoBehaviour` with 22 setter methods,
    /// four C# events and a 388-line file, and it drew nothing: no canvas, no row, no label, no
    /// button. Nothing in the project ever created one, so the only door to it,
    /// `FindFirstObjectByType&lt;CustomCharacterCreator&gt;()` on character select, returned null on
    /// every press. `CLAUDE.md` § 6.2: **"A FEATURE WITHOUT A SCREEN IS NOT SHIPPED, AND 'I ADDED
    /// A ROW FOR IT' IS NOT A DESIGN."** A data model is one row further from shipped than that.
    ///
    /// ⚠️⚠️ AND ITS `ApplyLiveCharacterToPreview` NEVER REACHED THE PREVIEW. It computed a
    /// 16-colour array into a private field and returned; `_preview` was assigned once in
    /// `BindPreview` and never read again. The array it computed also wrote the bottom-half
    /// clothing colour into slots 7, 8 and 9, **and slot 8 is the face** (`PaletteRules.FaceSlot`,
    /// and `docs/Voxel_Person_Guide.md`: *"A light slot 8 does not give a light-haired character,
    /// it gives one with no face"*). The only reason that never shipped as denim eyes is that the
    /// method was dead.
    ///
    /// § 0.5b QUESTION 1, WHAT IS THE ONE THING ON THIS SCREEN: **the character, life size, wearing
    /// the change you just made.** Everything else is sized against that. The model owns the left
    /// 44 per cent and the controls the right 56; the model never moves, never resizes and never
    /// waits, because the entire activity is "change one thing, look at it".
    ///
    /// ⚠️⚠️ THE CONTROL IS A STEPPER AND NOT A DROPDOWN, AND THE LIST LENGTHS ARE WHY.
    /// `CustomCharacterRules` carries 48 hairstyles, 48 tops, 36 bottoms and 32 skin tones. A
    /// 48-row dropdown is taller than his window (`CLAUDE.md` § 6.2b: `Fullscreen` is **false** in
    /// his `settings.json`), costs two presses per change, and asks the player to read forty-eight
    /// names to find one. `UiRows.StepperRow` is one press per step with the count on it, which is
    /// what every console creator does and what makes this browsable with a stick.
    ///
    /// ⚠️ THE SECTIONS MOVE THE CAMERA, THROUGH `ModelPreview.LookAt`, WHICH IS A REAL CAMERA AND
    /// NOT AN EVENT. The previous version raised `CameraFocusChanged` with a four-value enum that
    /// nothing subscribed to. Aiming without moving the aim height would have pulled in on
    /// `AimHeightRatio` 0.54, the waist, and pushed the head out of frame while the player was
    /// choosing a hat.
    ///
    /// ⚠️ IT REGISTERS WITH `ScreenTakeover`, so `PlayerNameplate` hides for it. That is
    /// `CLAUDE.md` § 6.2b row 4, which has now cost this project three separate builds.
    /// </summary>
    public sealed class CustomCharacterScreen : MonoBehaviour
    {
        /// <summary>
        /// ⚠️ 520, ABOVE THE HUB'S 500 AND THE SIGN-IN SCREEN'S 510. `MenuKit.BuildCanvas` records
        /// what a wrong number here costs and, worse, what an INERT one costs: a nested canvas
        /// ignores `sortingOrder` entirely unless `overrideSorting` is set, which is why 480, 500
        /// and 510 were once three numbers that did nothing. `BuildCanvas` sets it; this screen
        /// gets it for free by using `BuildCanvas` rather than `AddComponent<Canvas>()`.
        ///
        /// ⚠️⚠️ THE TWO SCREENS THIS ONE REPLACES USED 95, WHICH PUT THEM UNDER THE HUB THAT
        /// OPENS THEM. `HeroLoadoutScreen` and `AchievementsScreen` were both opened from a button
        /// on the hub's CAREER tab, and the hub is a 93 per cent scrim at 500. **Both screens
        /// built themselves correctly and were then drawn underneath the screen that opened
        /// them**, so the press appeared to do nothing at all. `docs/TODO.md` § 108.2.
        /// </summary>
        private const int SortingOrder = 520;

        /// <summary>
        /// What each section is looking at: how far up the body, and how close.
        ///
        /// ⚠️ THE HEIGHTS ARE FRACTIONS OF THE SUBJECT'S OWN MEASURED BOUNDS. See
        /// `ModelPreview.LookAt`: the cast spans 132 mm between the shortest and tallest rig, so a
        /// world-space aim would frame one character's forehead and another's chin.
        ///
        /// ⚠️ THE ZOOMS ARE INSIDE `ModelPreview.ZoomMin` 0.55 AND `ZoomMax` 2.2 by construction,
        /// and none of them is the floor: at the floor a head fills the frame edge to edge and a
        /// hat's silhouette, which is the thing being chosen, runs off the top.
        ///
        /// ⚠️⚠️ AND THE AIMS CAME IN TOWARD THE SUBJECT'S OWN CENTRE ON 2026-09-01, BECAUSE AN AIM
        /// IS ALSO A COMPOSITION AND NOBODY HAD LOOKED AT IT AS ONE. 🧑, of this screen: *"this
        /// shhit is off center"*. The camera puts the AIM POINT in the middle of the frame, so an
        /// aim of 0.76 puts three quarters of the body BELOW the centre line and leaves the top
        /// of the card empty: the figure reads as having sunk rather than as having been framed.
        /// The range is 0.44 to 0.68 now, which still moves the camera visibly between sections
        /// (the head is a quarter of a frame higher on FACE than on KIT) while the body's own
        /// centre stays within about a tenth of a frame of the card's. **The horizontal half of
        /// the same fault was one call, `ModelPreview.CentreSubject`, in `BuildStage`.**
        ///
        /// ⚠️⚠️ THEY CAME DOWN 15 PER CENT ON 2026-09-01 BECAUSE THE CARD GREW, AND THE MEASUREMENT
        /// IS OFF A PICTURE. `Logs/ui/21-creator-clothes-laptop_v2.png`: the figure was 290 px tall
        /// inside a 527 px card, **55 per cent**, and at 4:3 it was 175 px across a 358 px card.
        /// A character with half a card of nothing under him is the same fault as the band of brown
        /// `ActionRowTop` removed, one rect further in. ⚠️ **They are not lower than that**, because
        /// the section aims are fractions of the subject's own height and a zoom tight enough to
        /// fill a tall card at the highest aim crops the feet off at the lowest one. ⚠️ The
        /// numbers in that sentence were 0.34 to 0.80 and are 0.44 to 0.68 now; see the note
        /// above for why they moved.
        /// </summary>
        /// <remarks>
        /// ⚠️⚠️ THE BLURB IS ONE LINE OF CHROME UNDER THE TAB BAR NOW, NOT A `UiRows.Section`
        /// INSIDE THE LIST, AND THE DUPLICATION IS WHY. A `Section` draws its title as an amber
        /// heading; the tab that opened it is already lit amber two rows above, so the screen said
        /// **FACE** twice, 40 units apart, and spent 96 units of list on the second one. 🧑, of the
        /// build he opened: *"look this ugly ass ui its overwhelming"*. `CLAUDE.md` § 6.2
        /// question 3 is the test: what is on screen that the player does not need right now.
        ///
        /// ⚠️ IT ALSO REMOVES A WIDTH TRAP. `UiRows.Section` lays its stacked subtitle at a fixed
        /// `SidePadding + 420` in an 840-unit box, which needs a list about 1300 units wide; this
        /// screen's list is 1000, so the sentence ran off the end of every section header on it.
        /// As chrome the line owns the full column and cannot.
        /// </remarks>
        private static readonly (string Title, string Blurb, float Aim, float Zoom)[] Sections =
        {
            ("Face",    "Skin, expression and marks. Yours is the one skin the game lets you pick.",
                                                                                          0.66f, 0.80f),
            ("Hair",    "Cut and colour. Both free from level one, neither earned.",       0.68f, 0.76f),
            ("Body",    "Height and build, 85 to 115 per cent. It changes nothing but the look.",
                                                                                          0.54f, 0.87f),
            // ⚠️ CLOTHES FRAMES THE WHOLE FIGURE NOW, AND `docs/TODO.md` § 113 IS WHY. A pair of
            // track pants used to be a band at the hip, so a waist-high aim showed all of it;
            // the legs are real garments now and a hem is the thing being chosen.
            ("Clothes", "Top and bottom, and the colour of each. Two choices per garment.",
                                                                                          0.50f, 0.92f),
            ("Gear",    "Headwear, eyewear, wrists and neck.",                             0.62f, 0.78f),
            ("Kit",     "Your tsinelas, your lata, and whose skills you borrow.",          0.44f, 0.85f),
        };

        private Canvas _canvas;
        private GameObject _root;
        private RectTransform _list;
        private ScrollRect _scroll;
        private ModelPreview _preview;
        private RectTransform _stage;
        private Text _footer;
        private Text _blurb;
        private InputField _nameField;
        private readonly List<Button> _slotTabs = new List<Button>();
        private readonly List<Button> _sectionTabs = new List<Button>();

        private GameObject _tabBar;

        private int _slot;
        private int _section;

        /// <summary>⚠️ THE MARGIN IS THE SAME 96 THE HUB USES, so two full-screen takeovers
        /// do not start their text at two different distances from the same edge.</summary>
        private const float Margin = 96.0f;

        /// <summary>
        /// How wide the control column is, in authored units, measured against its CONTENT.
        ///
        /// ⚠️⚠️ A FIXED WIDTH AND NOT A FRACTION, AND `AspectSafeCanvas` IS WHAT MAKES THAT
        /// SAFE. `screenMatchMode` is `Expand`, so the canvas is **never narrower than 1920
        /// units**: at 4:3 it is 1920 by 1440 and it gets taller rather than thinner. A fixed
        /// width therefore cannot be squeezed off a narrow screen, and 🧑 plays in a short WIDE
        /// window (`CLAUDE.md` § 6.2b row 3), where a fraction would have made this column 1300
        /// units of nothing.
        ///
        /// ⚠️⚠️ AND 1000 IS ARITHMETIC. `UiRows.Row` puts its label in a 420-unit box starting at
        /// `SidePadding` 24, so the label ends at 444, and the control column starts at
        /// `UiRows.ValueColumn` 0.56 of the row. At 1000 units that is 560: **116 units after the
        /// label ends.** The screen he photographed ran this column at about 1100 units inside a
        /// full-width list and the value sat visibly adrift of its label, which is `docs/TODO.md`
        /// § 94.7 fault 1 in a smaller font. It is also six 157-unit section tabs across the same
        /// column, and the longest of them, CLOTHES, measures about 104.
        /// </summary>
        private const float ColumnWidth = 1000.0f;

        /// <summary>Where the content starts under the header rule, and where it stops above the
        /// action row. Both are measured from the canvas edges, so they hold at 1080 and at the
        /// 1440-unit height a 4:3 panel gets.</summary>
        private const float ContentTop = -186.0f;

        private const float ContentBottom = 176.0f;

        /// <summary>
        /// The top of the bottom action row, which is where the two columns stop.
        ///
        /// ⚠️⚠️ THE CARD USED TO STOP 84 UNITS ABOVE THIS AND THE GAP WAS VISIBLE AS A BAND OF
        /// BROWN ACROSS THE WHOLE SCREEN. `Logs/ui/20-creator-face-laptop_v2.png`: the model card
        /// ends at 583 px of 768 and the buttons start at 675, so 92 px of the shortest window this
        /// game supports is empty. The buttons are 62 tall centred on 88, so they occupy 57 to 119,
        /// and 150 clears the top of that by 31.
        /// </summary>
        private const float ActionRowTop = 150.0f;

        /// <summary>
        /// The `anchoredPosition.x` that puts a box of `width` with its LEFT edge at `left`.
        ///
        /// ⚠️⚠️ `MenuKit.Place` PIVOTS AT (0.5, 0.5), SO EVERY OFFSET IT TAKES IS A CENTRE.
        /// Passing a left margin straight in is the mistake, and it is silent: a 760-unit heading at
        /// `x = 96` spans -284 to 476, draws its first 284 units off the canvas, and every layout
        /// probe passes because the LABEL fits the BOX it was given. The box is in the wrong place.
        /// </summary>
        private static float LeftAt(float left, float width) => left + (width * 0.5f);

        /// <summary>The same, from the right edge, for a control anchored at anchor x = 1.</summary>
        private static float RightAt(float right, float width) => -(right + (width * 0.5f));

        /// <summary>
        /// The character being edited, held apart from the store until the player keeps it.
        ///
        /// ⚠️⚠️ A WORKING COPY, AND CANCEL IS WHY. Writing every stepper press straight to disk
        /// would make BACK a lie: a player who tries eight hats and then backs out has silently
        /// replaced the character they had. `docs/TODO.md` § 107 asks for *"3 characters u can save
        /// at once"*, and a save slot you cannot leave without overwriting is not a save slot.
        /// KEEP writes; BACK does not.
        /// </summary>
        private CustomCharacter _editing;

        public bool IsOpen => _root != null && _root.activeSelf;

        /// <summary>
        /// Finds the one instance, or makes it.
        ///
        /// ⚠️ ONE INSTANCE, BECAUSE IT OWNS A `ModelPreview` AND A `ModelPreview` OWNS A CAMERA,
        /// A RENDER TARGET, TWO LIGHTS AND A SLICE OF `ModelPreview.Stage`. A second one is a
        /// second set of all of that, standing on a neighbouring stage, rendering every frame.
        /// </summary>
        public static CustomCharacterScreen Ensure()
        {
            var found = UnityEngine.Object.FindAnyObjectByType<CustomCharacterScreen>();
            if (found != null) return found;

            var go = new GameObject("CustomCharacterScreen");
            return go.AddComponent<CustomCharacterScreen>();
        }

        private void Awake()
        {
            ScreenTakeover.Register(this, () => IsOpen);
        }

        private void OnDestroy()
        {
            ScreenTakeover.Unregister(this);
        }

        /// <summary>
        /// ⚠️ ESCAPE BACKS OUT, LIKE EVERY OTHER SCREEN IN THE GAME. `CLAUDE.md` § 6.3:
        /// *"A player who learns Escape is reliable and then meets one screen where it is not has
        /// learned that it is unreliable."* Both screens this replaces had no `Update` at all and
        /// their only exit was a BACK button in the corner.
        ///
        /// ⚠️ IT DISCARDS, MATCHING THE BACK BUTTON, and the footer says so before the player
        /// needs to find out. An Escape that silently saved would be a different verb wearing the
        /// same key.
        /// </summary>
        private void Update()
        {
            if (!IsOpen) return;
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            // ⚠️ THE PRESS IS SPENT HERE, so the converted screen underneath cannot also back out
            // on it. See `ScreenTakeover.ConsumeEscape`: that is exactly what happened, and it
            // landed 🧑 on the boot login screen from the character maker.
            ScreenTakeover.ConsumeEscape();
            MenuSfx.Back();
            Close();
        }

        public void Open()
        {
            if (_root == null) Build();

            _slot = CustomCharacterStore.Profile.ActiveSlot;
            _editing = CustomCharacterStore.Profile.Slots[_slot].Clone();
            _section = 0;

            // ⚠️ THE NAME FIELD IS BUILT ONCE AND RELOADED HERE, because it lives in the header
            // rather than in the list `Refresh` rebuilds. A screen reopened on a different slot
            // would otherwise show the previous character's name over the new character.
            if (_nameField != null) _nameField.text = _editing.Name ?? "";

            _root.SetActive(true);

            // ⚠️ THE POINTER IS RELEASED, LIKE EVERY OTHER MENU. `CursorMode`'s own header:
            // *"the buttons don't work" has a cursor-shaped cause, and it is invisible in a
            // screenshot.* A locked pointer sends every raycast to the same pixel for ever.
            CursorMode.Release();

            Refresh();
            ShowModel();
        }

        public void Close()
        {
            if (_root == null) return;
            _root.SetActive(false);
        }

        // -------------------------------------------------------------------
        // § CHROME
        // -------------------------------------------------------------------

        private void Build()
        {
            _canvas = MenuKit.BuildCanvas(transform, "CustomCharacterCanvas");
            _canvas.sortingOrder = SortingOrder;

            _root = new GameObject("CreatorRoot", typeof(RectTransform));
            _root.transform.SetParent(_canvas.transform, false);
            MenuKit.Stretch((RectTransform)_root.transform);

            // ⚠️⚠️ OPAQUE. ALPHA 1.0. 🧑, LOOKING AT THIS SCREEN IN THE BUILD: *"why can i see
            // the main menu"*, and *"give it a solid brown background too or creme coz this looks
            // ugly"*.
            //
            // **It was 0.94 and 0.94 is not opaque enough on this background**, which is the whole
            // finding. Six per cent of a lit street is still a street: the menu's PLAY, SETTINGS,
            // TUTORIAL and QUIT signs are saturated green and amber plates, the wall behind them
            // carries the TUMP wordmark at a metre high, and a scrim that leaves 6 per cent of
            // that through leaves every one of those shapes legible under the form. Measured off
            // `Logs/ui/20-creator-face-laptop_v1.png`, the brick behind the heading reads
            // (58, 52, 48) rather than the (16, 13, 12) the arithmetic promises, because the
            // wall under it is not mid grey, it is a lit facade.
            //
            // ⚠️⚠️ AND THE LESSON IS NOT "USE 0.99". A scrim buys legibility over a live scene
            // (`CLAUDE.md` § 6.2c question 3: *ask what it protects before retuning it*), and this
            // screen does not want the scene at all: the ONE thing on it is a character you are
            // dressing, and a street behind him is a second character competing for the same eye.
            // **There is nothing for a scrim to protect here, so this is a surface rather than a
            // scrim.** `PlayerHub` is the same argument and 🧑 has never complained about it,
            // because the hub happens to open over nothing.
            //
            // ⚠️ IT IS ALSO THE BLOCKER (§ 6.2c question 4). Everything a player can act on is
            // inside this screen while it is up, and an opaque `Image` with `raycastTarget` on is
            // what stops a press reaching the character select underneath.
            MenuKit.Backdrop(_root.transform, UiTheme.WoodDeep);

            BuildHeader();
            BuildStage();
            BuildList();
            BuildFooter();

            _root.SetActive(false);
        }

        /// <summary>
        /// The title, the three save slots, and the name.
        ///
        /// ⚠️⚠️ THE SLOT TABS ARE TABS RATHER THAN A DROPDOWN OR THREE CARDS, because there are
        /// exactly three of them for ever (`CustomCharacterRules.MaxSlots`) and a tab bar is the
        /// control that says "these are the same kind of thing and you are looking at one of
        /// them". `PlayerHub.BuildTabBar` is the same control on the same wood for the same
        /// reason, which is `docs/VISION.md` § 6: one visual language.
        ///
        /// ⚠️ SWITCHING SLOTS KEEPS NOTHING. It re-reads the store, so the working copy of the
        /// slot you were on is dropped, exactly as BACK drops it. Two different discard rules on
        /// one screen is how a player loses work without being told.
        /// </summary>
        private void BuildHeader()
        {
            // ⚠️⚠️ EVERY X BELOW IS A CENTRE, NOT A LEFT EDGE, AND THE FIRST VERSION OF THIS
            // SCREEN GOT IT WRONG FOUR TIMES. `MenuKit.Place` sets `pivot` to (0.5, 0.5), so a
            // 760-unit box at `offset.x = 96` spans **-284 to 476**: the heading, the footer
            // sentence and two of the four buttons were off the left edge of the canvas entirely,
            // and the layout probe was green because every label fitted its own box. `LeftAt` does
            // the arithmetic once so it cannot be got wrong per call.
            var head = MenuKit.Label(_root.transform, "MAKE YOUR OWN", UiRows.HeadingUnits + 8,
                UiTheme.Amber, new Vector2(0.0f, 1.0f), new Vector2(LeftAt(Margin, 760.0f), -68.0f),
                new Vector2(760.0f, 52.0f), TextAnchor.MiddleLeft);
            head.raycastTarget = false;

            var sub = MenuKit.Label(_root.transform,
                "Three you can keep. One walks into the match.", UiRows.HintUnits,
                UiTheme.CreamMuted, new Vector2(0.0f, 1.0f),
                new Vector2(LeftAt(Margin, 760.0f), -110.0f),
                new Vector2(760.0f, 28.0f), TextAnchor.MiddleLeft);
            sub.raycastTarget = false;

            // ⚠️⚠️ THE NAME LIVES IN THE HEADER NOW AND IT USED TO BE THE FIRST ROW OF THE `FACE`
            // SECTION, WHICH IS THE WRONG SCREEN FOR IT TWICE OVER. A name is not a facial
            // feature, so a player looking for it under FACE found it by accident and a player
            // looking for it under any other tab could not find it at all: switching to HAIR
            // rebuilt the list and the field vanished. It also carried the only `hint` on the
            // screen, which is what forced the whole list to stay wide enough for
            // `UiRows.Row`'s 800-unit hint box.
            //
            // ⚠️ IT IS BUILT ONCE AND NEVER REBUILT, which `Refresh` could not promise: a
            // `Refresh` on `onValueChanged` destroys the `InputField` the player is typing into
            // and takes the caret with it, and that reads as the screen eating every second
            // character. Out here nothing rebuilds it but a slot switch, which is a deliberate
            // change of subject.
            var nameCap = MenuKit.Label(_root.transform, "NAME", UiRows.HintUnits, UiTheme.Amber,
                new Vector2(1.0f, 1.0f), new Vector2(RightAt(Margin + 460.0f, 90.0f), -140.0f),
                new Vector2(90.0f, 26.0f), TextAnchor.MiddleRight);
            nameCap.raycastTarget = false;

            _nameField = UiRows.Field(_root.transform, "Batang Kalye", NameLimit);

            MenuKit.Place(_nameField.GetComponent<RectTransform>(), new Vector2(1.0f, 1.0f),
                new Vector2(RightAt(Margin, 440.0f), -140.0f), new Vector2(440.0f, 46.0f));

            _nameField.onValueChanged.AddListener(text =>
            {
                if (_editing != null) _editing.Name = text;
            });

            _slotTabs.Clear();
            for (int i = 0; i < CustomCharacterRules.MaxSlots; i++)
            {
                int index = i;

                // ⚠️⚠️ `WoodAmberButton`, NOT `WoodPrimaryButton`, AND `GodotTheme` HAD ALREADY
                // WRITTEN DOWN WHY: *"AMBER IS THE SELECTED-TAB COLOUR AND IT IS NOT A SECOND 'GO'
                // BUTTON... painting it green put two 'press me' buttons on one screen with the
                // more important one further from the hand."* This screen had THREE greens at
                // once, and 🧑 photographed it: the live slot tab, the live section tab and KEEP
                // AND USE, all in `MenuGreen`, so nothing on the screen led. Green is the ACT
                // colour and exactly one control here acts.
                var tab = MenuKit.WoodButton(_root.transform, $"SLOT {i + 1}",
                    new Vector2(1.0f, 1.0f),
                    new Vector2(RightAt(Margin + ((2 - i) * 212.0f), 200.0f), -80.0f),
                    new Vector2(200.0f, 56.0f),
                    () =>
                    {
                        if (_slot == index) return;
                        MenuSfx.Click();
                        _slot = index;
                        _editing = CustomCharacterStore.Profile.Slots[_slot].Clone();
                        if (_nameField != null) _nameField.text = _editing.Name ?? "";
                        Refresh();
                        ShowModel();
                    });

                _slotTabs.Add(tab);
            }

            // ⚠️ A HAIRLINE UNDER THE HEADER, because a gap alone was not separating the title
            // from the controls: `FUTURE.md` § 0.5b's fourth ordering tool is space, and it is
            // used up here by the name field sitting in the same band.
            var rule = new GameObject("HeaderRule", typeof(RectTransform), typeof(Image));
            rule.transform.SetParent(_root.transform, false);

            var ruleRt = (RectTransform)rule.transform;
            ruleRt.anchorMin = new Vector2(0.0f, 1.0f);
            ruleRt.anchorMax = new Vector2(1.0f, 1.0f);
            ruleRt.pivot = new Vector2(0.5f, 1.0f);
            ruleRt.offsetMin = new Vector2(Margin, -174.0f);
            ruleRt.offsetMax = new Vector2(-Margin, -172.0f);

            var ruleImage = rule.GetComponent<Image>();
            ruleImage.color = new Color(UiTheme.WoodEdge.r, UiTheme.WoodEdge.g,
                                        UiTheme.WoodEdge.b, 0.55f);
            ruleImage.raycastTarget = false;
        }

        /// <summary>
        /// The left column: the character, and nothing else.
        ///
        /// ⚠️⚠️ SIZED AGAINST THE FIGURE RATHER THAN AS A FRACTION OF THE WINDOW, WHICH IS
        /// `CLAUDE.md` § 6.2c QUESTION 1. `AspectSafeCanvas` scales on the SHORT axis, so a
        /// percentage of the canvas is 1920 units wide at 4:3 and about 2250 on the short wide
        /// window he actually plays in: **one fraction is two very different widths.** The stage
        /// is anchored to the LEFT edge with a fixed 860-unit width, which is a 2.38-scale figure
        /// plus a margin either side, so the model is the same size on every screen and the row
        /// column takes the slack.
        ///
        /// ⚠️ IT IS DRAGGABLE, because `ModelPreview.Attach` installs `ModelPreviewInput` and the
        /// footer says so. A creator where you cannot turn the model round to see the back of the
        /// hat you just chose is a creator that hides half of every choice.
        /// </summary>
        private void BuildStage()
        {
            // ⚠️⚠️ THE MODEL SITS ON A CARD NOW AND IT USED TO SIT ON THE STREET. That is the
            // difference between a preview and a character standing in front of the main menu:
            // 🧑, of the screen he opened, *"why can i see the main menu"*. The backdrop above is
            // what removed the street; this is what gives the figure a frame, so the ONE thing on
            // the screen reads as the subject of the screen rather than as a cut-out.
            //
            // ⚠️ IT IS A SHADE DARKER THAN THE FIELD, NOT LIGHTER. The character is a bright
            // voxel figure wearing an 8 mm ink outline (`ToonSkin.PersonOutlineWidth`); a cream
            // card would put the loudest surface on the screen behind the thing it is meant to
            // show off, and the outline that separates him from it would be drawn against the one
            // background it cannot contrast with.
            var card = new GameObject("StageCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(_root.transform, false);

            var cardRt = (RectTransform)card.transform;
            cardRt.anchorMin = new Vector2(0.0f, 0.0f);
            cardRt.anchorMax = new Vector2(1.0f, 1.0f);

            // ⚠️⚠️ THE CARD TAKES THE SLACK AND THE COLUMN DOES NOT, WHICH IS THE WHOLE REASON
            // THIS RECT IS ANCHORED TO BOTH EDGES. `AspectSafeCanvas` is `Expand`, so the canvas
            // is 1920 units wide at 16:9 and about 2560 on an ultrawide. Two fixed columns would
            // put 640 units of empty brown down the middle of the screen at that shape.
            // `CLAUDE.md` § 6.2c question 1: size a panel against its CONTENT. The column's
            // content is a 1000-unit row; the card's content is a character, and a character is
            // happy to be bigger.
            cardRt.offsetMin = new Vector2(Margin, ActionRowTop);
            cardRt.offsetMax = new Vector2(-(ColumnWidth + Margin + 56.0f), ContentTop);

            var face = card.GetComponent<Image>();
            face.color = UiTheme.WoodDark;
            face.raycastTarget = false;

            var skin = card.AddComponent<GodotPanel>();
            skin.Variation = "WoodSlot";
            skin.ApplyContentMargins = false;
            skin.Apply();

            var go = new GameObject("Stage", typeof(RectTransform));
            go.transform.SetParent(_root.transform, false);

            // ⚠️⚠️ A FRACTION, AND THE ARITHMETIC IS THE REASON RATHER THAN THE HABIT.
            // The first version pinned this to a FIXED 860 units, which is 45 per cent of the
            // canvas at 16:9 and **60 per cent of it at 4:3**, because `AspectSafeCanvas` scales on
            // the SHORT axis: the canvas is about 1920 units wide at 16:9 and 1440 at 4:3.
            // `CLAUDE.md` § 6.2c question 1 is normally the argument AGAINST a fraction; here it is
            // the argument FOR one, because **the thing this has to stay in proportion with is the
            // list beside it, not a fixed piece of content.**
            //
            // ⚠️⚠️ AND THE LIST IS THE CONSTRAINT, NOT THE MODEL. `UiRows.Row` puts its
            // control column at `ValueColumn` 0.56 and its hint box ends 828 units in, so a list
            // narrower than about 1480 units draws the hint straight through the control. At 0.40
            // and 0.97 the list is 1094 units at 16:9 and 820 at 4:3, **both under that**, which is
            // why the creator's rows carry no hints and the explanation lives on the section header
            // instead. That is a design consequence of the split and it is written here so the next
            // person does not re-add hints and re-create the overlap.
            // ⚠️ INSET INSIDE THE CARD BY 14 SO THE CARD'S OWN BEVEL IS NOT DRAWN OVER. The
            // preview is a live camera rendering a subject parked five hundred metres under the
            // world (`ModelPreview.Stage`), so it has no idea the card is there.
            _stage = (RectTransform)go.transform;
            _stage.anchorMin = new Vector2(0.0f, 0.0f);
            _stage.anchorMax = new Vector2(1.0f, 1.0f);
            _stage.offsetMin = new Vector2(Margin + 14.0f, ActionRowTop + 14.0f);
            _stage.offsetMax = new Vector2(-(ColumnWidth + Margin + 70.0f), ContentTop - 14.0f);

            _preview = go.AddComponent<ModelPreview>();
            _preview.Attach(_stage);

            // ⚠️⚠️ CENTRED, AND WITHOUT THIS THE MODEL STANDS 17 PER CENT OF THE CARD RIGHT OF
            // THE MIDDLE OF IT. 🧑 2026-09-01: *"this shhit is off center"*. `ModelPreview` carries
            // the character select screen's `FrameHorizontalOffsetRatio` by default, which shoves
            // the subject sideways to clear a control column **on the left**; this screen's column
            // is on the RIGHT, so the same offset pushed the figure toward the panel rather than
            // away from it. See `ModelPreview.CentreSubject`.
            _preview.CentreSubject();
        }

        /// <summary>
        /// The control column: six tabs, one sentence, and the rows of the section you are on.
        ///
        /// ⚠️⚠️ 1000 UNITS WIDE AND ANCHORED TO THE RIGHT EDGE, NOT A FRACTION OF THE WINDOW.
        /// See `ColumnWidth` for the arithmetic; it is `CLAUDE.md` § 6.2c question 1 answered with
        /// a number rather than with a percentage.
        /// </summary>
        private void BuildList()
        {
            var listGo = new GameObject("ListArea", typeof(RectTransform));
            listGo.transform.SetParent(_root.transform, false);

            var listRt = (RectTransform)listGo.transform;
            listRt.anchorMin = new Vector2(1.0f, 0.0f);
            listRt.anchorMax = new Vector2(1.0f, 1.0f);
            listRt.offsetMin = new Vector2(-(ColumnWidth + Margin), ActionRowTop);
            listRt.offsetMax = new Vector2(-Margin, ContentTop - 122.0f);

            _list = UiRows.ScrollList(listGo.transform, "Rows", out _scroll);
            MenuKit.Stretch((RectTransform)_scroll.transform);
        }

        /// <summary>
        /// The four things a player can do with what is on screen, in the order they do them.
        ///
        /// ⚠️⚠️ ONE PRIMARY BUTTON AND IT IS THE ONE THAT COMMITS. `docs/TODO.md` § 92 is the
        /// six-button panel: *"look wtf why are these buttons here"*, six controls in six visual
        /// languages at six hand-written offsets. Here KEEP is `WoodPrimaryButton` (the green ACT
        /// variation `GodotTheme` reserves for START MATCH and READY) and the other three are
        /// plain wood, so the row has one obvious answer and three alternatives rather than four
        /// equal choices.
        ///
        /// ⚠️ BACK SAYS WHAT IT DISCARDS IN THE FOOTER LINE ABOVE IT, before the player presses
        /// it. A confirm dialog would be the fifth control on a screen whose whole job is trying
        /// things quickly; a sentence costs nothing and answers the same question.
        /// </summary>
        private void BuildFooter()
        {
            // ⚠️⚠️ SURPRISE ME AND PRESETS MOVED UNDER THE MODEL AND OUT OF THE ACTION ROW, AND
            // THAT IS `FUTURE.md` § 0.5b QUESTION 4 RATHER THAN TIDINESS. Four buttons of the same
            // size along one edge is four equal choices, and two of them were BACK, which throws
            // the edit away, and KEEP AND USE, which is the only reason the screen exists. **These
            // two act on the character**, so they belong under the character; the two that leave
            // the screen stay together on the right, where every screen in this game puts them.
            MenuKit.WoodButton(_root.transform, "SURPRISE ME", new Vector2(0.0f, 0.0f),
                new Vector2(LeftAt(Margin, 300.0f), 88.0f), new Vector2(300.0f, 62.0f),
                () =>
                {
                    MenuSfx.Click();
                    CustomCharacterRules.Randomize(_editing);
                    if (_nameField != null) _nameField.text = _editing.Name ?? "";
                    Refresh();
                    ShowModel();
                });

            MenuKit.WoodButton(_root.transform, "PRESETS", new Vector2(0.0f, 0.0f),
                new Vector2(LeftAt(Margin + 316.0f, 260.0f), 88.0f), new Vector2(260.0f, 62.0f),
                () =>
                {
                    MenuSfx.Click();
                    _presetIndex = (_presetIndex + 1) % CustomCharacterRules.PresetNames.Length;
                    CustomCharacterRules.ApplyPreset(_editing, _presetIndex);
                    if (_nameField != null) _nameField.text = _editing.Name ?? "";
                    Refresh();
                    ShowModel();
                });

            // ⚠️⚠️ ONE SHORT LINE, NOT THREE SENTENCES. It read *"Drag the model to turn it,
            // wheel to zoom. KEEP AND USE saves this slot and plays as it. BACK discards."* at
            // `HintUnits` across 1400 units, under a screen 🧑 had just called overwhelming. Two
            // of those three sentences describe buttons that are eighty units to the right saying
            // the same thing in bigger type, which is `CLAUDE.md` § 6.2 question 3.
            _footer = MenuKit.Label(_root.transform, "", UiRows.HintUnits, UiTheme.CreamMuted,
                new Vector2(0.0f, 0.0f), new Vector2(LeftAt(Margin + 14.0f, 620.0f), ActionRowTop + 28.0f),
                new Vector2(620.0f, 26.0f), TextAnchor.MiddleLeft);
            _footer.raycastTarget = false;

            MenuKit.WoodButton(_root.transform, "BACK", new Vector2(1.0f, 0.0f),
                new Vector2(RightAt(Margin + 320.0f, 240.0f), 88.0f), new Vector2(240.0f, 62.0f),
                () => { MenuSfx.Back(); Close(); });

            MenuKit.WoodButton(_root.transform, "KEEP AND USE", new Vector2(1.0f, 0.0f),
                new Vector2(RightAt(Margin, 304.0f), 88.0f), new Vector2(304.0f, 62.0f),
                () =>
                {
                    MenuSfx.Click();
                    CustomCharacterStore.SetSlot(_slot, _editing);
                    CustomCharacterStore.SetActiveSlot(_slot);
                    CustomCharacterStore.InUse = true;
                    Close();
                },
                "WoodPrimaryButton");
        }

        private int _presetIndex = -1;

        /// <summary>⚠️ THE SAME CEILING `AccountRules.DisplayNameMax` USES, because both strings
        /// are drawn in a nameplate over a head at arena distance and a limit that differs between
        /// the two would let one of them overflow a box the other fits.</summary>
        private const int NameLimit = 16;

        // -------------------------------------------------------------------
        // § CONTENT
        // -------------------------------------------------------------------

        private void Refresh()
        {
            if (_list == null) return;

            for (int i = _list.childCount - 1; i >= 0; i--)
                Destroy(_list.GetChild(i).gameObject);

            // ⚠️⚠️ THE CHILDREN ARE DETACHED IN THE SAME BREATH AS BEING DESTROYED, AND THE
            // ORDER MATTERS. `Destroy` is deferred to the end of the frame, so a rebuild that only
            // called it would lay the new rows out ALONGSIDE the old ones for one frame: the
            // zebra banding in `UiRows.Row` counts siblings, so every stripe would be wrong, and
            // `ContentSizeFitter` would size the list to both sets. `SetParent(null)` takes them
            // out of the layout immediately and `Destroy` then reclaims them.
            for (int i = _list.childCount - 1; i >= 0; i--)
                _list.GetChild(i).SetParent(null, false);

            for (int i = 0; i < _slotTabs.Count; i++)
            {
                var skin = _slotTabs[i].GetComponent<GodotButton>();
                if (skin == null) continue;
                skin.Variation = i == _slot ? "WoodAmberButton" : "WoodButton";
                skin.Apply();
                skin.Refresh();
            }

            BuildSectionTabs();

            switch (_section)
            {
                case 0: BuildFace(); break;
                case 1: BuildHair(); break;
                case 2: BuildBody(); break;
                case 3: BuildClothes(); break;
                case 4: BuildGear(); break;
                default: BuildKit(); break;
            }

            UiRows.Gap(_list, 40.0f);

            if (_blurb != null) _blurb.text = Blurb();

            // ⚠️ ONE SHORT LINE, AND IT IS ABOUT THE MODEL because it lives under the model. What
            // KEEP AND USE and BACK do is written on KEEP AND USE and BACK.
            bool active = CustomCharacterStore.InUse
                          && CustomCharacterStore.Profile.ActiveSlot == _slot;

            _footer.text = active
                ? "Drag to turn, wheel to zoom.  ·  This is the slot you play as."
                : "Drag to turn, wheel to zoom.";
        }

        /// <summary>
        /// The one sentence under the tab bar, which is the SECTION's, except on KIT where it is
        /// the borrowed hero's three abilities.
        ///
        /// ⚠️⚠️ THE KIT LINE WAS A SECOND `UiRows.Section` INSIDE THE LIST AND ITS OWN COMMENT
        /// RECORDS WHY IT COULD NOT BE A ROW: `SEISMIC STOMP · DEMONIC CARAPACE · TITAN FISSURE`
        /// measures 527 units and the value column is 458. It is chrome now, on the full width of
        /// the column, which is 1000 units, so the longest of the six fits with room over.
        /// </summary>
        private string Blurb()
        {
            if (_section != 5) return Sections[_section].Blurb;

            return HeroKitBlurb(CustomCharacterRules.KitFor(_editing.HeroKitId));
        }

        /// <summary>
        /// The six part tabs, as a bar across the top of the right-hand column.
        ///
        /// ⚠️⚠️ THEY WERE INSIDE A `UiRows.Row` AND SIX OF THEM DID NOT FIT IN IT. A row's
        /// control slot is `ValueColumn` 0.56 to the right margin, which is about 460 units on this
        /// screen; six 144-unit tabs at 150 spacing is 900. **CLOTHES, GEAR and KIT were off the
        /// right edge**, so half the screen was unreachable and the probe was green because the
        /// three tabs that DID fit fitted. `CLAUDE.md` § 6.2c, the width question.
        ///
        /// ⚠️ IT IS A TAB BAR BECAUSE IT IS THE SAME CONTROL `PlayerHub.BuildTabBar` IS, doing
        /// the same job, on the same wood. `docs/VISION.md` § 6: one visual language.
        ///
        /// ⚠️ THE TABS ARE SIZED FROM THE COLUMN THEY SIT IN rather than given a width, so six
        /// of them fill it at every resolution and the arithmetic cannot go stale.
        /// </summary>
        private void BuildSectionTabs()
        {
            // ⚠️⚠️ DETACHED IN THE SAME BREATH AS BEING DESTROYED, for the reason `Refresh` gives
            // about the list: `Destroy` is deferred to the end of the frame, so the old bar and
            // the new one are both children of `_root` for one frame and the old blurb draws
            // straight through the new one. The blurb is destroyed WITH the bar because it belongs
            // to it; leaving it out is how a label accumulates one copy per section press.
            if (_tabBar != null)
            {
                _tabBar.transform.SetParent(null, false);
                Destroy(_tabBar);
            }

            if (_blurb != null)
            {
                _blurb.transform.SetParent(null, false);
                Destroy(_blurb.gameObject);
                _blurb = null;
            }

            _tabBar = new GameObject("SectionTabs", typeof(RectTransform));
            _tabBar.transform.SetParent(_root.transform, false);

            // ⚠️ THE SAME 1000-UNIT COLUMN THE ROWS UNDER IT USE, anchored to the same edge. It
            // was a pair of fractions, 0.400 to 0.975, which is 1104 units at 16:9 and 1472 on the
            // window he plays in: **the tab bar and the rows it switches were two different
            // widths on the same screen** and neither of them was the one the arithmetic in
            // `ColumnWidth` is about.
            var bar = (RectTransform)_tabBar.transform;
            bar.anchorMin = new Vector2(1.0f, 1.0f);
            bar.anchorMax = new Vector2(1.0f, 1.0f);
            bar.pivot = new Vector2(0.5f, 1.0f);
            bar.offsetMin = new Vector2(-(ColumnWidth + Margin), ContentTop - 60.0f);
            bar.offsetMax = new Vector2(-Margin, ContentTop);

            var group = _tabBar.AddComponent<HorizontalLayoutGroup>();
            group.spacing = 8.0f;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = true;
            group.childControlWidth = true;
            group.childControlHeight = true;

            _sectionTabs.Clear();
            for (int i = 0; i < Sections.Length; i++)
            {
                int index = i;
                var tab = MenuKit.WoodButton(_tabBar.transform,
                    Sections[i].Title.ToUpperInvariant(),
                    new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0.0f, 0.0f),
                    () =>
                    {
                        if (_section == index) return;
                        MenuSfx.Click();
                        _section = index;
                        Refresh();
                        AimCamera();
                    },
                    i == _section ? "WoodAmberButton" : "WoodButton");

                // ⚠️ THE LAYOUT GROUP OWNS THE RECT, so the button's own `Place` call is
                // overridden a frame later. `MenuKit.WoodButton` sizes its LABEL from the size it
                // was handed, which is zero here, so the label is re-fitted against the real cell.
                var element = tab.gameObject.AddComponent<LayoutElement>();
                element.flexibleWidth = 1.0f;

                var label = tab.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    label.fontSize = MenuKit.MinReadableUnits;
                    MenuKit.Stretch(label.rectTransform, -6.0f);
                    label.alignment = TextAnchor.MiddleCenter;
                }

                _sectionTabs.Add(tab);
            }

            // ⚠️ THE BLURB IS REBUILT WITH THE BAR because it belongs to the bar rather than to
            // the list: it is the one line saying what the lit tab is FOR. It is built here rather
            // than in `Build` so it cannot outlive the bar it explains.
            var line = MenuKit.Label(_root.transform, "", UiRows.HintUnits, UiTheme.CreamMuted,
                new Vector2(1.0f, 1.0f),
                new Vector2(RightAt(Margin, ColumnWidth), ContentTop - 84.0f),
                new Vector2(ColumnWidth, 26.0f), TextAnchor.MiddleLeft);

            line.raycastTarget = false;
            line.text = Blurb();
            _blurb = line;
        }

        private void BuildFace()
        {
            Stepper("Skin tone", CustomCharacterRules.SkinToneNames, _editing.SkinToneIndex,
                    v => _editing.SkinToneIndex = v);

            Stepper("Expression", CustomCharacterRules.FaceExpressionNames,
                    _editing.FaceExpressionIndex, v => _editing.FaceExpressionIndex = v);

            Stepper("Marks", CustomCharacterRules.FaceMarkingNames,
                    _editing.FaceMarkingIndex, v => _editing.FaceMarkingIndex = v);
        }

        private void BuildHair()
        {
            Stepper("Cut", CustomCharacterRules.HairstyleNames, _editing.HairstyleIndex,
                    v => _editing.HairstyleIndex = v);

            Stepper("Colour", CustomCharacterRules.HairColorNames, _editing.HairColorIndex,
                    v => _editing.HairColorIndex = v);
        }

        /// <summary>
        /// ⚠️⚠️ THE HEIGHT ROW STATES ITS BOUNDS AND THE BOUNDS ARE A COMPETITIVE NUMBER, NOT A
        /// TASTE ONE. `CustomCharacter.MinHeightPercent` is 85 and `MaxHeightPercent` is 115.
        /// `Roster.HeroPeople`'s header records what a size difference actually does in this game:
        /// *"bcz Sean is larger than all, he should be slower than all (he has a defender
        /// advantage)"*, because `CLAUDE.md` § 4 resolves contact by DISTANCE and reach is the
        /// taya's whole job. A cosmetic that changed reach would be `FUTURE.md` § 0.5 rule 4
        /// broken, so the window is narrow and the hint says why in one line.
        ///
        /// ⚠️ IN FIVE-POINT STEPS RATHER THAN A SLIDER. Seven values a player can name beats a
        /// continuous dial nobody can return to, and it is the same argument `TintStrengths` makes
        /// on character select: names rather than numbers, three steps, never more.
        /// </summary>
        private void BuildBody()
        {
            const int Step = 5;
            int span = (CustomCharacter.MaxHeightPercent - CustomCharacter.MinHeightPercent) / Step;
            int current = Mathf.Clamp((_editing.HeightPercent - CustomCharacter.MinHeightPercent) / Step,
                                      0, span);

            var labels = new string[span + 1];
            for (int i = 0; i <= span; i++)
                labels[i] = $"{CustomCharacter.MinHeightPercent + (i * Step)} %";

            UiRows.StepperRow(_list, "Height", labels[current], current, labels.Length,
                v =>
                {
                    _editing.HeightPercent = CustomCharacter.MinHeightPercent + (v * Step);
                    Refresh();
                    ShowModel();
                });

            Stepper("Build", CustomCharacterRules.BuildSizeNames, _editing.BuildSizeIndex,
                    v => _editing.BuildSizeIndex = v);
        }

        private void BuildClothes()
        {
            Stepper("Top", CustomCharacterRules.TopClothingNames, _editing.TopClothingIndex,
                    v => _editing.TopClothingIndex = v);

            // ⚠️⚠️ THE COLOUR IS ITS OWN ROW BECAUSE HE ASKED FOR IT TO BE.
            // \U0001f9d1: *"can i change the color of thhose clothes too??"*. The version this replaces
            // derived a garment's colour from its INDEX, so picking a jersey picked its colour and
            // there was no way to have a red one and a blue one of the same shirt.
            Stepper("Top colour", CustomCharacterRules.ClothingColourNames, _editing.TopColorIndex,
                    v => _editing.TopColorIndex = v);

            Stepper("Bottom", CustomCharacterRules.BottomClothingNames,
                    _editing.BottomClothingIndex, v => _editing.BottomClothingIndex = v);

            Stepper("Bottom colour", CustomCharacterRules.ClothingColourNames,
                    _editing.BottomColorIndex, v => _editing.BottomColorIndex = v);
        }

        private void BuildGear()
        {
            Stepper("Headwear", CustomCharacterRules.HeadwearNames, _editing.HeadAccessoryIndex,
                    v => _editing.HeadAccessoryIndex = v);

            Stepper("Face", CustomCharacterRules.FaceAccessoryNames, _editing.FaceAccessoryIndex,
                    v => _editing.FaceAccessoryIndex = v);

            Stepper("Wrists", CustomCharacterRules.WristAccessoryNames,
                    _editing.WristAccessoryIndex, v => _editing.WristAccessoryIndex = v);

            Stepper("Neck", CustomCharacterRules.NeckAccessoryNames, _editing.NeckAccessoryIndex,
                    v => _editing.NeckAccessoryIndex = v);
        }

        /// <summary>
        /// The tsinelas, the lata, and the borrowed kit.
        ///
        /// ⚠️⚠️ THE KIT ROW IS ONE CHOICE AND IT IS THE WHOLE HERO. \U0001f9d1, 2026-08-31: *"it can js
        /// borrow the skills of any of the characters for its skills and ult"*, then immediately
        /// *"it can only follow onne skill tree tho and cant mix diff shits"*. **The second
        /// sentence is why this is ONE stepper rather than three.** A custom character that could
        /// take Zack's sprint with Cheska's barricade would be a seventh hero built out of the best
        /// third of six, and `docs/VISION.md` § 4's competitive argument is that reading which
        /// ultimate an opponent has banked is a skill. Borrowing a whole kit keeps every tell true.
        ///
        /// ⚠️ THE ROW NAMES THE HERO'S ACTUAL ABILITIES UNDERNEATH IT, so the choice is made on
        /// what it does rather than on a name. `docs/VISION.md` § 3: a player must be able to
        /// understand a power by looking at it, and character select is the LEARN layer.
        ///
        /// ⚠️ AND THE TSINELAS AND LATA ARE THE REAL ROSTER LISTS, not invented names.
        /// `Roster.Slippers` and `Roster.Cans` are wire-replicated picks the lobby already sends,
        /// so choosing one here changes no protocol at all.
        /// </summary>
        private void BuildKit()
        {
            Stepper("Footwear", CustomCharacterRules.FootwearNames, _editing.FootwearIndex,
                    v => _editing.FootwearIndex = v);

            var slippers = Roster.Slippers;
            var slipperNames = new string[slippers.Count];
            for (int i = 0; i < slippers.Count; i++) slipperNames[i] = slippers[i].Name;

            Stepper("Tsinelas", slipperNames, _editing.SlipperIndex,
                    v => _editing.SlipperIndex = v);

            var cans = Roster.Cans;
            var canNames = new string[cans.Count];
            for (int i = 0; i < cans.Count; i++) canNames[i] = cans[i].Name;

            Stepper("Lata", canNames, _editing.CanIndex, v => _editing.CanIndex = v);

            // ⚠️ THE ABILITY NAMES ARE THE SECTION'S BLURB LINE ON THIS TAB. See `Blurb()`: they
            // were a `ValueRow` first, and `SEISMIC STOMP · DEMONIC CARAPACE · TITAN FISSURE`
            // measures 527 units against a 458-unit value column, so it drew over the row beside
            // it; then a second `UiRows.Section` inside the list, which put a second amber heading
            // on a screen that already had one. `CLAUDE.md` § 6.2c's width question is the rule:
            // size a control against the NARROWEST box it will ever live in, and the widest box on
            // this screen is the blurb line.
            var heroes = Roster.HeroPeople;
            string kit = CustomCharacterRules.KitFor(_editing.HeroKitId);

            int kitIndex = 0;
            var heroNames = new string[heroes.Count];
            for (int i = 0; i < heroes.Count; i++)
            {
                heroNames[i] = heroes[i].Name;
                if (heroes[i].Id == kit) kitIndex = i;
            }

            UiRows.StepperRow(_list, "Borrowed kit", heroNames[kitIndex], kitIndex, heroNames.Length,
                v =>
                {
                    _editing.HeroKitId = heroes[v].Id;
                    MenuSfx.Click();
                    Refresh();
                });

        }

        /// <summary>
        /// ⚠️ THE THREE ABILITY NAMES, FROM THE HERO KITS THEMSELVES. `docs/TODO.md` § 108.3 is
        /// the entry about a table that invented twelve abilities none of which are in this
        /// repository; these are transcribed from the `base(...)` call that registers each one in
        /// `Assets/TumbangPreso/Runtime/Abilities/*HeroKit.cs`.
        /// </summary>
        private static string HeroKitBlurb(string heroId)
        {
            switch (heroId)
            {
                case "dante": return "SEISMIC STOMP  ·  DEMONIC CARAPACE  ·  TITAN FISSURE";
                case "cheska": return "PERMAFROST SHEET  ·  ICE BARRICADE  ·  GLACIAL NOVA";
                case "sean": return "FLAME RUSH  ·  IGNITION CANNON  ·  SUPERNOVA";
                case "zack": return "BOLT SPRINT  ·  STATIC CHARGE  ·  THUNDERSTRIKE";
                case "nemu": return "PHANTOM VEIL  ·  ASTRAL HIJACK  ·  DEVOURING SEANCE";
                default: return "HEX  ·  SHADOW BLINK  ·  GRAND COVEN";
            }
        }

        private void Stepper(string label, string[] names, int index, Action<int> apply,
                             string hint = "")
        {
            int safe = Mathf.Clamp(index, 0, names.Length - 1);

            UiRows.StepperRow(_list, label, Pretty(names[safe]), safe, names.Length,
                v =>
                {
                    apply(v);
                    MenuSfx.Click();
                    Refresh();
                    ShowModel();
                },
                hint);
        }

        /// <summary>
        /// ⚠️ THE HEX CODE COMES OFF THE LABEL BEFORE A PLAYER SEES IT. The skin tone names in
        /// `CustomCharacterRules` carry their own value, `"Classic Kayumanggi (#C88A52)"`, because
        /// that is where the colour is actually defined and one list beats a list and a table that
        /// can disagree. **A player does not need the hex and it is longer than the name.**
        /// </summary>
        private static string Pretty(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            int bracket = name.IndexOf(" (#", StringComparison.Ordinal);
            return bracket < 0 ? name : name.Substring(0, bracket);
        }

        // -------------------------------------------------------------------
        // § THE MODEL
        // -------------------------------------------------------------------

        /// <summary>
        /// Puts the edited character on the stage, dressed, coloured and scaled.
        ///
        /// ⚠️⚠️ THIS IS THE METHOD 🧑 WAS ASKING ABOUT. *"like if i change size or eyes or
        /// mouth or add an accessory i can actually see it"*. Before `VoxelDresser` existed,
        /// fifteen of this screen's controls wrote a number and moved nothing on the model: the
        /// expression, the markings, every hairstyle, every hat, every pair of shades, the wrist
        /// and neck rows and the footwear were **names with no geometry behind them**
        /// (`docs/TODO.md` § 108.4). All of them are boxes now, and `VoxelWardrobe` is where they
        /// are authored.
        ///
        /// ⚠️⚠️ THE ORDER IS SHOW, THEN SCALE, THEN DRESS, AND IT IS NOT INTERCHANGEABLE.
        /// `ModelPreview.Show` rebuilds the subject from the prefab, so anything dressed onto the
        /// previous one is gone; `VoxelDresser` MEASURES the head and torso, so it has to run after
        /// the scale that changes what those measure. Dressing first and scaling after would leave
        /// a hat sized for a body that no longer exists.
        ///
        /// ⚠️⚠️ THE RIG IS `BaseRigId`, NOT `CustomCharacterId`, AND THE TWO ARE DIFFERENT
        /// THINGS. `custom` is who this player IS, on the wire and in `settings.json`;
        /// `custom_base` is the `.glb` the wardrobe hangs off, and until 2026-08-31 they were the
        /// same row and it pointed at a copy of a fully dressed hero. `docs/TODO.md` § 112:
        /// against a rig with hair, a sando and shorts baked in, every wearable had to COVER what
        /// was under it, so a hairstyle was a lid and an expression needed a plate over the rig's
        /// own painted eyes. `team-custom-base.glb` is bald, bare and faceless.
        ///
        /// ⚠️ IT DEGRADES THROUGH `custom` AND THEN TO THE ROSTER RATHER THAN TO NOTHING. A
        /// fresh clone before `RosterBookBuilder.Build` has neither asset, and a screen that
        /// answers a blank stage is indistinguishable from one that is broken.
        /// </summary>
        private void ShowModel()
        {
            if (!Application.isPlaying || _preview == null) return;

            var book = RosterBook.Load();
            if (book == null) return;

            var art = book.FindPersonArt(CustomCharacterRules.BaseRigId)
                      ?? book.FindPersonArt(CustomCharacterRules.CustomCharacterId)
                      ?? book.PersonArt(0, SceneFlow.SelectedMode);
            if (art == null) return;

            var palette = CustomCharacterOutfit.PaletteFor(art.Palette, _editing);

            _preview.Show(art.Model, art.Clips, palette, art.PetModel);

            var subject = _preview.Subject;
            if (subject != null)
            {
                CustomCharacterOutfit.ApplyBodyScale(subject, _editing);
                CustomCharacterOutfit.Dress(subject, _editing, palette);
            }

            AimCamera();
        }
        /// <summary>
        /// ⚠️⚠️ THE SCALE, THE PALETTE AND THE TEN DRESS CALLS MOVED TO
        /// `CustomCharacterOutfit` AND THIS SCREEN IS NOT THE OWNER ANY MORE.
        /// `docs/TODO.md` § 112. They were private methods here, so a match seat could only
        /// ever have had a SECOND implementation of what a custom character looks like, which
        /// is exactly the shape of § 94.1's four hand-written copies of "which line in this
        /// record is mine", all agreeing on the wrong value. One owner, and the preview, the
        /// wardrobe contact sheet and a live match seat all call it.
        ///
        /// ⚠️ `SkinColour`, `HairColour` AND `ClothColour` MOVED WITH THEM and are public on
        /// `CustomCharacterOutfit` now, because `WardrobeSheetProbe` reads them.
        /// </summary>
        private void AimCamera()
        {
            if (_preview == null) return;
            _preview.LookAt(Sections[_section].Aim, Sections[_section].Zoom);
        }
    }
}
