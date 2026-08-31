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
        /// and 0.62 is deliberately not the floor: at the floor a head fills the frame edge to
        /// edge and a hat's silhouette, which is the thing being chosen, runs off the top.
        /// </summary>
        private static readonly (string Title, string Blurb, float Aim, float Zoom)[] Sections =
        {
            // ⚠️ EVERY BLURB IS UNDER 90 CHARACTERS, AND THE FIRST RUN OF THE PROBE IS WHY.
            // `UiRows.Section` gives its subtitle an 840-unit box and `MenuKit.Label` OVERFLOWS
            // rather than wrapping, so a 116-character sentence measured 870 units and drew over
            // the row under it. The probe caught this one, which is the difference between a
            // measurable fault and the six in § 108.6b that it could not see.
            ("Face",    "Skin, expression and marks. A roster character's skin is locked; yours is not.",
                                                                                          0.80f, 0.84f),
            ("Hair",    "Cut and colour. Both free from level one, neither earned.",       0.82f, 0.84f),
            ("Body",    "Height and build, 85 to 115 per cent. Reach decides a tag.",      0.54f, 1.00f),
            ("Clothes", "Top and bottom.",                                                0.60f, 0.92f),
            ("Gear",    "Headwear, eyewear, wrists and neck.",                            0.74f, 0.88f),
            ("Kit",     "The tsinelas you throw and the lata you guard.",                 0.30f, 0.96f),
        };

        private Canvas _canvas;
        private GameObject _root;
        private RectTransform _list;
        private ScrollRect _scroll;
        private ModelPreview _preview;
        private RectTransform _stage;
        private Text _footer;
        private readonly List<Button> _slotTabs = new List<Button>();
        private readonly List<Button> _sectionTabs = new List<Button>();

        private GameObject _tabBar;

        private int _slot;
        private int _section;

        /// <summary>⚠️ THE MARGIN IS THE SAME 96 THE HUB USES, so two full-screen takeovers
        /// do not start their text at two different distances from the same edge.</summary>
        private const float Margin = 96.0f;

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

            MenuSfx.Back();
            Close();
        }

        public void Open()
        {
            if (_root == null) Build();

            _slot = CustomCharacterStore.Profile.ActiveSlot;
            _editing = CustomCharacterStore.Profile.Slots[_slot].Clone();
            _section = 0;

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

            // ⚠️ THE SCRIM IS OPAQUE ENOUGH TO KILL THE SCREEN BEHIND IT, copying `PlayerHub`.
            // Character select is a lit street with a cast standing on it; a translucent panel
            // over that is two characters competing for the same eye. **Everything a player can
            // act on is inside this screen while it is up**, and the block is this graphic's job
            // as much as the look is (`CLAUDE.md` § 6.2c, question 4).
            MenuKit.Backdrop(_root.transform, new Color(0.03f, 0.02f, 0.01f, 0.94f));

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
                UiTheme.Amber, new Vector2(0.0f, 1.0f), new Vector2(LeftAt(Margin, 760.0f), -74.0f),
                new Vector2(760.0f, 52.0f), TextAnchor.MiddleLeft);
            head.raycastTarget = false;

            var sub = MenuKit.Label(_root.transform,
                "Three you can keep. One walks into the match.", UiRows.HintUnits,
                UiTheme.CreamMuted, new Vector2(0.0f, 1.0f),
                new Vector2(LeftAt(Margin, 760.0f), -118.0f),
                new Vector2(760.0f, 28.0f), TextAnchor.MiddleLeft);
            sub.raycastTarget = false;

            _slotTabs.Clear();
            for (int i = 0; i < CustomCharacterRules.MaxSlots; i++)
            {
                int index = i;
                var tab = MenuKit.WoodButton(_root.transform, $"SLOT {i + 1}",
                    new Vector2(1.0f, 1.0f),
                    new Vector2(RightAt(Margin + ((2 - i) * 236.0f), 224.0f), -96.0f),
                    new Vector2(224.0f, 60.0f),
                    () =>
                    {
                        if (_slot == index) return;
                        MenuSfx.Click();
                        _slot = index;
                        _editing = CustomCharacterStore.Profile.Slots[_slot].Clone();
                        Refresh();
                        ShowModel();
                    });

                _slotTabs.Add(tab);
            }
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
            _stage = (RectTransform)go.transform;
            _stage.anchorMin = new Vector2(0.025f, 0.0f);
            _stage.anchorMax = new Vector2(0.375f, 1.0f);
            _stage.offsetMin = new Vector2(0.0f, 128.0f);
            _stage.offsetMax = new Vector2(0.0f, -196.0f);

            _preview = go.AddComponent<ModelPreview>();
            _preview.Attach(_stage);
        }

        private void BuildList()
        {
            var listGo = new GameObject("ListArea", typeof(RectTransform));
            listGo.transform.SetParent(_root.transform, false);

            var listRt = (RectTransform)listGo.transform;
            listRt.anchorMin = new Vector2(0.400f, 0.0f);
            listRt.anchorMax = new Vector2(0.975f, 1.0f);
            listRt.offsetMin = new Vector2(0.0f, 128.0f);
            listRt.offsetMax = new Vector2(0.0f, -260.0f);

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
            _footer = MenuKit.Label(_root.transform, "", UiRows.HintUnits, UiTheme.CreamMuted,
                new Vector2(0.0f, 0.0f), new Vector2(LeftAt(Margin, 1400.0f), 104.0f),
                new Vector2(1400.0f, 26.0f), TextAnchor.MiddleLeft);
            _footer.raycastTarget = false;

            MenuKit.WoodButton(_root.transform, "SURPRISE ME", new Vector2(0.0f, 0.0f),
                new Vector2(LeftAt(Margin, 280.0f), 48.0f), new Vector2(280.0f, 62.0f),
                () =>
                {
                    MenuSfx.Click();
                    CustomCharacterRules.Randomize(_editing);
                    Refresh();
                    ShowModel();
                });

            MenuKit.WoodButton(_root.transform, "PRESETS", new Vector2(0.0f, 0.0f),
                new Vector2(LeftAt(Margin + 296.0f, 240.0f), 48.0f), new Vector2(240.0f, 62.0f),
                () =>
                {
                    MenuSfx.Click();
                    _presetIndex = (_presetIndex + 1) % CustomCharacterRules.PresetNames.Length;
                    CustomCharacterRules.ApplyPreset(_editing, _presetIndex);
                    Refresh();
                    ShowModel();
                });

            MenuKit.WoodButton(_root.transform, "BACK", new Vector2(1.0f, 0.0f),
                new Vector2(RightAt(Margin + 320.0f, 240.0f), 48.0f), new Vector2(240.0f, 62.0f),
                () => { MenuSfx.Back(); Close(); });

            MenuKit.WoodButton(_root.transform, "KEEP AND USE", new Vector2(1.0f, 0.0f),
                new Vector2(RightAt(Margin, 304.0f), 48.0f), new Vector2(304.0f, 62.0f),
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
                skin.Variation = i == _slot ? "WoodPrimaryButton" : "WoodButton";
                skin.Apply();
                skin.Refresh();
            }

            // ⚠️ THE FIELD WRITES INTO THE WORKING COPY ON EVERY KEYSTROKE AND THE ROW IS NOT
            // REBUILT WHILE IT IS FOCUSED. A `Refresh` on `onValueChanged` would destroy the
            // `InputField` the player is typing into and take the caret with it, which reads as
            // the screen eating every second character.
            //
            // ⚠️ AND IT IS THE ONLY ROW ON THIS SCREEN THAT CARRIES A HINT, because it is
            // the only one whose control is narrow enough to leave room for one. See the note on
            // the stage rect: `UiRows.Row`'s hint box ends 828 units in and the control column
            // starts at 0.56 of a list this screen keeps under 1100 units wide.
            var nameField = UiRows.FieldRow(_list, "Name", "Batang Kalye", NameLimit,
                "Up to " + NameLimit + " characters.");
            nameField.text = _editing.Name ?? "";
            nameField.onValueChanged.AddListener(text => _editing.Name = text);

            BuildSectionHeader();
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

            bool active = CustomCharacterStore.InUse
                          && CustomCharacterStore.Profile.ActiveSlot == _slot;

            _footer.text = active
                ? "Drag the model to turn it, wheel to zoom. This slot is the one you play as. BACK discards this edit."
                : "Drag the model to turn it, wheel to zoom. KEEP AND USE saves this slot and plays as it. BACK discards.";
        }

        /// <summary>
        /// ⚠️ SIX SECTIONS RATHER THAN ONE LONG LIST, AND THE COUNT IS THE REASON. Fifteen
        /// steppers in one scroll is `docs/TODO.md` § 92's *"theres liek 20 shits at once"* with
        /// different nouns. Each section is three or four rows, which is one screen with no
        /// scrolling on the window he plays in, and the camera moves to what the section is about.
        /// </summary>
        private void BuildSectionHeader()
        {
            UiRows.Section(_list, Sections[_section].Title, Sections[_section].Blurb);
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
            if (_tabBar != null) Destroy(_tabBar);

            _tabBar = new GameObject("SectionTabs", typeof(RectTransform));
            _tabBar.transform.SetParent(_root.transform, false);

            var bar = (RectTransform)_tabBar.transform;
            bar.anchorMin = new Vector2(0.400f, 1.0f);
            bar.anchorMax = new Vector2(0.975f, 1.0f);
            bar.pivot = new Vector2(0.5f, 1.0f);
            bar.offsetMin = new Vector2(0.0f, -256.0f);
            bar.offsetMax = new Vector2(0.0f, -188.0f);

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
                    i == _section ? "WoodPrimaryButton" : "WoodButton");

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

            Stepper("Bottom", CustomCharacterRules.BottomClothingNames,
                    _editing.BottomClothingIndex, v => _editing.BottomClothingIndex = v);
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

        private void BuildKit()
        {
            Stepper("Tsinelas", CustomCharacterRules.FootwearNames, _editing.FootwearIndex,
                    v => _editing.FootwearIndex = v);

            Stepper("Lata", CustomCharacterRules.LataSkinNames, _editing.LataSkinIndex,
                    v => _editing.LataSkinIndex = v);
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
        /// Puts the edited character on the stage, wearing the palette the choices produce.
        ///
        /// ⚠️⚠️ THROUGH `ModelPreview.Show` WITH A REAL PALETTE, WHICH IS THE HALF THE PREVIOUS
        /// VERSION SKIPPED. `FUTURE.md` PHASE 5: *"Preview through `ModelPreview` with the real
        /// shader, never a flat icon"*, and `CLAUDE.md` § 6.1 one level up: a model change with no
        /// picture attached cannot be judged. The toon ramp, the ink outline and Unity's linear
        /// conversion ARE the look; a swatch beside a name is a different picture of a different
        /// thing.
        ///
        /// ⚠️ THE `custom` ROSTER ENTRY IS LOOKED UP BY ID, NOT BY INDEX.
        /// `RosterBook.FindPersonArt("custom")` resolves `Resources/Roster/person_custom.asset`,
        /// and `custom` is deliberately NOT a row in `Roster.AllPeople`: that list's header is
        /// explicit that its order is a network contract and that entries are appended, never
        /// inserted. A nineteenth row meaning "custom" would change what index 18 resolves to on
        /// every build that has not shipped yet.
        ///
        /// ⚠️ IT DEGRADES TO THE ROSTER ART RATHER THAN TO NOTHING when the custom `.glb` is
        /// missing, which is what a fresh clone before `RosterBookBuilder.Build` looks like. An
        /// empty stage reads as a broken screen; a stand-in reads as a stand-in.
        /// </summary>
        private void ShowModel()
        {
            if (!Application.isPlaying || _preview == null) return;

            var book = RosterBook.Load();
            if (book == null) return;

            var art = book.FindPersonArt(CustomCharacterRules.CustomCharacterId);
            if (art == null) art = book.PersonArt(0, SceneFlow.SelectedMode);
            if (art == null) return;

            _preview.Show(art.Model, art.Clips, PaletteFor(art.Palette), art.PetModel);
            AimCamera();
        }

        private void AimCamera()
        {
            if (_preview == null) return;
            _preview.LookAt(Sections[_section].Aim, Sections[_section].Zoom);
        }

        /// <summary>
        /// The sixteen colours the edited character is painted with.
        ///
        /// ⚠️⚠️ THE FACE SLOT IS COPIED THROUGH AND THE SKIN SLOTS ARE WRITTEN, WHICH IS THE
        /// OPPOSITE WAY ROUND FROM EVERY ROSTER CHARACTER AND IS THE WHOLE POINT OF THIS FEATURE.
        /// `PaletteRules.IsProtectedSlot` stops a hue dial reaching a canonical character's skin
        /// (`docs/TODO.md` § 107); this character's skin is not rotated out of the authored
        /// colours, it is CHOSEN, so it is written straight in and never travels that path.
        ///
        /// ⚠️⚠️ SLOT 8 IS THE FACE AND IS LEFT ALONE. The version this replaces wrote the
        /// bottom-half clothing colour into slots 7, 8 and 9. `docs/Voxel_Person_Guide.md`: *"A
        /// light slot 8 does not give a light-haired character, it gives one with no face."*
        ///
        /// ⚠️ THE THREE-STEP RAMPS ARE SHADE, BASE, LIT, MEASURED OFF THE SHIPPED `.tres` FILES
        /// RATHER THAN INVENTED. `person_team-zack.tres` carries slot 13 and slot 15 at the same
        /// lit tone with slot 14 a clear step darker, which is a two-band toon ramp with its lit
        /// value repeated. 0.78 and 1.14 reproduce that spacing from a single chosen colour.
        ///
        /// ⚠️ THE CLOTHING COLOURS ARE DERIVED FROM THE GARMENT INDEX RATHER THAN HARD-CODED.
        /// The version this replaces painted every top the same red and every bottom the same
        /// denim whatever the player chose, so 48 tops and 36 bottoms were 84 names attached to
        /// two colours. Golden-ratio hue stepping keeps adjacent entries visibly different, which
        /// is the one property that matters while stepping through a list one press at a time.
        /// </summary>
        private Color[] PaletteFor(Color[] authored)
        {
            if (authored == null || authored.Length < PaletteRules.SlotCount) return authored;

            var palette = new Color[authored.Length];
            Array.Copy(authored, palette, authored.Length);

            Ramp(palette, PaletteRules.SkinSlots, SkinColour(_editing.SkinToneIndex));
            Ramp(palette, HairSlots, HairColour(_editing.HairColorIndex));
            Ramp(palette, TopSlots, GarmentColour(_editing.TopClothingIndex, 0.62f, 0.74f));
            Ramp(palette, BottomSlots, GarmentColour(_editing.BottomClothingIndex, 0.48f, 0.58f));

            return palette;
        }

        /// <summary>
        /// ⚠️ 10, 11 AND 12, AND `docs/Voxel_Person_Guide.md` § 5.7 IS WHY THIS IS WRITTEN DOWN
        /// RATHER THAN ASSUMED. That section records *"slot 13 is his hair" was one session's
        /// guess*, written as a fact, and it cost a build. 13 to 15 are skin, measured off the
        /// `.tres` files; the guide's § 6 says 9, 10 and 11 keep stock Kenney values, so the hair
        /// band sits at 10 to 12 where it overlaps only the two spare slots and never the face.
        /// </summary>
        private static readonly int[] HairSlots = { 10, 11, 12 };

        /// <summary>⚠️ 4, 5 AND 6. NOT 7, 8, 9: SLOT 8 IS THE FACE.</summary>
        private static readonly int[] TopSlots = { 4, 5, 6 };

        /// <summary>⚠️ 0, 1 AND 2, STEPPING AROUND THE FACE AT 8 rather than through it.</summary>
        private static readonly int[] BottomSlots = { 0, 1, 2 };

        private static void Ramp(Color[] palette, int[] slots, Color basis)
        {
            if (slots.Length < 3) return;

            palette[slots[0]] = Scale(basis, 1.14f);
            palette[slots[1]] = Scale(basis, 0.78f);
            palette[slots[2]] = Scale(basis, 1.14f);
        }

        /// <summary>⚠️ CLAMPED, BECAUSE A COLOUR ABOVE 1.0 IS NOT A BRIGHTER COLOUR IN A TOON
        /// SHADER THAT BANDS ON VALUE, it is a slot that has quietly left the ramp. `Mathf.Clamp01`
        /// per channel rather than a `Color` multiply, which has no bound at all.</summary>
        private static Color Scale(Color c, float factor)
            => new Color(Mathf.Clamp01(c.r * factor), Mathf.Clamp01(c.g * factor),
                         Mathf.Clamp01(c.b * factor), c.a);

        /// <summary>
        /// ⚠️ THE HEX IS PARSED OUT OF THE NAME, WHICH IS WHERE IT IS DEFINED. One list rather
        /// than a list plus a colour table that can disagree with it: the same argument
        /// `PaletteVariants` makes about a swatch painted from an authored constant.
        /// </summary>
        public static Color SkinColour(int index)
        {
            var names = CustomCharacterRules.SkinToneNames;
            if (index < 0 || index >= names.Length) index = 0;

            string name = names[index];
            int hash = name.IndexOf('#');

            if (hash >= 0 && name.Length >= hash + 7
                && ColorUtility.TryParseHtmlString(name.Substring(hash, 7), out var parsed))
                return parsed;

            return new Color(0.78f, 0.54f, 0.32f);
        }

        /// <summary>
        /// ⚠️ THE HAIR COLOURS ARE DERIVED FROM THEIR NAMES' POSITION RATHER THAN FROM A SECOND
        /// HAND-WRITTEN ARRAY. The version this replaces kept a 32-entry `Color[]` beside the
        /// 32-entry `string[]` in `CustomCharacterRules`, in a different file, with nothing
        /// asserting they stayed the same length: adding a hairstyle colour in one place and not
        /// the other is an index that silently reads the wrong colour, which is exactly the class
        /// of bug `Roster.Slippers`' header is about.
        ///
        /// ⚠️ THE FIRST FOURTEEN ARE NATURAL AND THE REST ARE DYE, which is what the names in
        /// `CustomCharacterRules.HairColorNames` already say: `Jet Black` through `Salt &amp;
        /// Pepper`, then `Jeepney Crimson` onward. The naturals interpolate along one warm
        /// brown ramp; the dyes step the hue wheel. Same list, one source.
        /// </summary>
        public static Color HairColour(int index)
        {
            var names = CustomCharacterRules.HairColorNames;
            if (index < 0 || index >= names.Length) index = 0;

            const int Naturals = 14;

            if (index < Naturals)
            {
                float t = index / (float)(Naturals - 1);
                return Color.Lerp(new Color(0.075f, 0.070f, 0.080f),
                                  new Color(0.86f, 0.78f, 0.62f), t);
            }

            float hue = ((index - Naturals) * 0.137f) % 1.0f;
            return Color.HSVToRGB(hue, 0.72f, 0.82f);
        }

        /// <summary>
        /// A garment's colour, from its index.
        ///
        /// ⚠️ 0.381966 IS THE GOLDEN-RATIO CONJUGATE AND IT IS HERE FOR ONE PROPERTY: consecutive
        /// indices land far apart on the wheel and the sequence never repeats inside 48 entries.
        /// A player stepping through the tops one press at a time sees a different colour every
        /// press, which is the only thing this has to get right until the garment meshes exist.
        ///
        /// ⚠️⚠️ AND THIS IS A STAND-IN THAT SAYS SO. `docs/TODO.md` § 108.4: the 48 tops and 36
        /// bottoms are NAMES today and the modular garment geometry is not built, so what changes
        /// on screen is the colour and not the cut. Recording that is the difference between a
        /// known gap and a bug somebody rediscovers in three weeks (`CLAUDE.md` § 2.3).
        /// </summary>
        private static Color GarmentColour(int index, float saturation, float value)
        {
            float hue = (index * 0.381966f) % 1.0f;
            return Color.HSVToRGB(hue, saturation, value);
        }
    }
}
