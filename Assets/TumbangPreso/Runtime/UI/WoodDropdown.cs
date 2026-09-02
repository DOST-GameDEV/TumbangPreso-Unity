using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// A dropdown sized for the lobby's rail: one row closed, every option visible open.
    ///
    /// 🧑 2026-09-01: *"u can use dropdowns and shit to make some shit work or look good"*, in the
    /// same breath as *"buttons were the biggest problem"*. Both halves are the same complaint.
    ///
    /// ⚠️⚠️ A STEPPER HIDES EVERY OPTION BUT ONE, AND THE LOBBY HAD FOUR OF THEM IN A COLUMN.
    /// MAP, MODE, BOTS and RULES were four identical `&lt; VALUE &gt;` rows: **twelve controls to
    /// express four choices**, none of which tells the player what the other options are or how
    /// many there are. Pressing the arrow four times to see the fourth map is not a control, it is
    /// a guessing game with a button on it. `game-ui-design`'s Progressive Information Disclosure
    /// is the pattern: layer one is the current value, layer two is the whole list on demand.
    ///
    /// ⚠️⚠️ AND `UiRows.DropdownRow` COULD NOT BE REUSED, WHICH IS A MEASUREMENT AND NOT A
    /// PREFERENCE. That control is built for the hub's list: `UiRows.Row` puts its label in a
    /// 420-unit box and starts the value column at 0.56 of the row, so it needs a list about 1480
    /// units wide before the hint stops drawing through the control. **The lobby rail is 460.**
    /// `CustomCharacterScreen`'s `ColumnWidth` note records the same arithmetic from the other
    /// side, and § 108's `StepperRow` shipped 476 units wide into a 1366-pixel window with its
    /// right-hand arrow off the screen. A rail control has to be built against the rail.
    ///
    /// ⚠️⚠️ THE OPEN LIST OVERLAYS AND DOES NOT PUSH. It is a `LayoutElement.ignoreLayout` child,
    /// for the reason `LobbyChat.BuildHistoryPanel` records in full: a `VerticalLayoutGroup` owns
    /// the position and size of every ACTIVE child, so an open list that took part in the layout
    /// would shove every row under it down the rail and walk START MATCH off the bottom of the
    /// screen. **That exact fault shipped in the chat this same day.**
    ///
    /// ⚠️ AND ONLY ONE IS OPEN AT A TIME. A static, because four dropdowns in one column cannot
    /// see each other and two open lists overlap into an unreadable stack.
    /// </summary>
    public sealed class WoodDropdown : MonoBehaviour
    {
        private static WoodDropdown _open;

        private readonly List<Button> _optionButtons = new List<Button>();

        private Text _value;
        private Text _caret;
        private GameObject _list;
        private RectTransform _listRect;

        /// <summary>
        /// The open list's own <see cref="Canvas"/>, kept so <see cref="Toggle"/> can re-derive its
        /// sorting order against the hierarchy the control is actually in when it opens.
        /// </summary>
        private Canvas _listCanvas;

        private string[] _options = Array.Empty<string>();
        private int _index;
        private Action<int> _picked;

        /// <summary>How tall the closed row is. ⚠️ 56, which clears the 44-unit touch floor
        /// `game-ui-design` sets and matches the rail's other rows.</summary>
        public const float RowHeight = 56.0f;

        /// <summary>How tall one option in the open list is.</summary>
        private const float OptionHeight = 46.0f;

        /// <summary>
        /// How far above its host screen an open list sorts.
        ///
        /// ⚠️ IT IS A GAP RATHER THAN AN ABSOLUTE, so the popup cannot be stranded under a screen
        /// whose order changes later. The screens in this project step in tens and hundreds (90
        /// for the chat, 100 for a panel, 480 to 510 for the nameplate, hub and sign-in), so one
        /// hundred clears the host and its siblings without reaching the next screen up.
        /// </summary>
        private const int PopupLift = 100;

        /// <summary>
        /// The `sortingOrder` an open list has to beat: the nearest ancestor that actually decides
        /// sorting for this subtree.
        ///
        /// ⚠️⚠️ A NESTED `Canvas` WITHOUT `overrideSorting` CARRIES A NUMBER THAT DOES NOTHING,
        /// AND READING ONE IS THE WHOLE OF `docs/TODO.md` § 99. That entry found 480, 500 and 510
        /// set on three nested canvases that had never overridden anything, so the values were
        /// inert and a fix built on them *"worked for a reason nobody checked"*. This walk skips
        /// exactly those: only a root canvas, or a nested one that has taken sorting over, sets
        /// the level this popup must clear.
        ///
        /// ⚠️ ZERO WHEN THERE IS NO ANCESTOR CANVAS AT ALL, which is a control being built into a
        /// detached hierarchy in a test. `PopupLift` alone is then still above the default 0.
        /// </summary>
        private static int HostSortingOrder(GameObject go)
        {
            if (go == null) return 0;

            for (var t = go.transform.parent; t != null; t = t.parent)
            {
                var canvas = t.GetComponent<Canvas>();
                if (canvas == null) continue;

                if (canvas.isRootCanvas || canvas.overrideSorting) return canvas.sortingOrder;
            }

            return 0;
        }

        /// <summary>
        /// Build a dropdown as the next row of a vertical layout.
        ///
        /// ⚠️ THE CAPTION IS A FIXED COLUMN AND THE VALUE TAKES THE REST, which is
        /// `LobbyChrome.DressSelectorRow`'s hard-won note applied here: MAP, MODE, BOTS and RULES
        /// are four different widths, and sizing each caption to its own string put the four
        /// values at four different x positions. Nothing in the rail lined up with anything, which
        /// is most of what "ugly" was.
        /// </summary>
        public static WoodDropdown Build(Transform parent, string caption, float captionWidth,
                                         string[] options, int index, Action<int> picked)
        {
            var go = new GameObject($"Dropdown_{caption}", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var element = go.AddComponent<LayoutElement>();
            element.minHeight = RowHeight;
            element.preferredHeight = RowHeight;
            element.flexibleHeight = 0.0f;

            var drop = go.AddComponent<WoodDropdown>();
            drop.Construct(caption, captionWidth, options, index, picked);
            return drop;
        }

        private void Construct(string caption, float captionWidth, string[] options, int index,
                               Action<int> picked)
        {
            _options = options ?? Array.Empty<string>();
            _index = Mathf.Clamp(index, 0, Mathf.Max(0, _options.Length - 1));
            _picked = picked;

            // ⚠️⚠️ THE WHOLE CONTROL IS PAPER NOW AND THE CLASS NAME IS A LIE THAT IS KEPT ON
            // PURPOSE. `WoodDropdown` is reached by name from `ConvertedMatchSetup`,
            // `PhaseSurfaceLayoutProbe` and two documents; renaming it is a rename commit and this
            // is a material commit, and mixing the two is how a diff stops being reviewable. What
            // it draws is what changed: on `Logs/shots-runtime/LobbySettings-v52.png` these four
            // rows were the only dark-brown objects on a cream drawer, which is exactly the
            // leftover 🧑 asked twice to be sure of.
            var label = MenuKit.Label(transform, caption, PaperKit.Caption, UiTheme.PaperInkSoft,
                                      Vector2.zero, Vector2.zero, Vector2.zero,
                                      TextAnchor.MiddleLeft);
            label.raycastTarget = false;
            var labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0.0f, 0.0f);
            labelRect.anchorMax = new Vector2(0.0f, 1.0f);
            labelRect.pivot = new Vector2(0.0f, 0.5f);
            labelRect.offsetMin = new Vector2(2.0f, 0.0f);
            labelRect.offsetMax = new Vector2(captionWidth, 0.0f);
            MenuKit.Fit(label, captionWidth - 8.0f);

            // ---- the closed row ---------------------------------------------------------
            var faceGo = new GameObject("Face", typeof(RectTransform), typeof(Image));
            faceGo.transform.SetParent(transform, false);

            var face = faceGo.GetComponent<Image>();
            // ⚠️ A `Tray` AND NOT A `Token`: a dropdown's closed face is a VALUE you read, and
            // `PaperCraft`'s distinction is that a tray is cut into the sheet and a token stands on
            // it. The caret is what says it opens, which is the same rule the settings chip
            // follows one control up.
            PaperSkin.Apply(faceGo, PaperCraft.Surface.Tray);
            face.type = Image.Type.Sliced;
            face.color = Color.white;

            var faceRect = face.rectTransform;
            faceRect.anchorMin = new Vector2(0.0f, 0.0f);
            faceRect.anchorMax = new Vector2(1.0f, 1.0f);
            faceRect.offsetMin = new Vector2(captionWidth + 8.0f, 2.0f);
            faceRect.offsetMax = new Vector2(0.0f, -2.0f);

            var button = faceGo.AddComponent<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(Toggle);
            FocusRing.Attach(faceGo, 3.0f);

            // ⚠️⚠️ IT HOVERS AND IT PRESSES, WHICH IT DID NOT. 🧑 2026-09-01, of the four rows in
            // this drawer: **"match settings ui look ugly"**, and separately
            // **"REWORK THE BUTTONS so that it feels great to click"**. This control had a caret
            // saying it opened and nothing at all under the pointer: no hover, no press, no sound.
            // `CLAUDE.md` § 6.3 states the rule as *a control that does something must react to
            // the pointer*, and a row that does not is indistinguishable from a readout however
            // clearly it is drawn.
            //
            // ⚠️ ADDED AFTER THE VALUE AND THE CARET EXIST, at the end of this method.
            // `PaperButton.Awake` captures the child it is going to move and tint, and it runs the
            // instant `AddComponent` is called: adding it here would give it a control with no
            // `Text` under it yet, and the press would then move nothing.

            _value = MenuKit.Label(faceGo.transform, Current, PaperKit.Body, UiTheme.PaperInk,
                                   Vector2.zero, Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
            _value.raycastTarget = false;
            MenuKit.Stretch(_value.rectTransform, 0.0f);
            _value.rectTransform.offsetMin = new Vector2(16.0f, 0.0f);
            _value.rectTransform.offsetMax = new Vector2(-40.0f, 0.0f);

            // ⚠️ THE CARET IS THE AFFORDANCE. `CLAUDE.md` § 6.3: a door is a thing that looks
            // pressable, and a value in a box with no mark on it is a readout.
            _caret = MenuKit.Label(faceGo.transform, "▾", PaperKit.Body, UiTheme.PaperInkSoft,
                                   Vector2.zero, Vector2.zero, Vector2.zero, TextAnchor.MiddleRight);
            _caret.raycastTarget = false;
            MenuKit.Stretch(_caret.rectTransform, 0.0f);
            _caret.rectTransform.offsetMax = new Vector2(-14.0f, 0.0f);

            // ⚠️ THE VALUE IS THE THING THAT MOVES ON A PRESS, so it is named `Label`.
            // `PaperButton` looks for that child first and otherwise takes the first `Text` under
            // the control, which here would be whichever of the value and the caret was built
            // first. See `LobbyChrome.BuildRoomSign`, where the same fallback tinted the caption
            // instead of the code.
            _value.name = "Label";
            faceGo.AddComponent<PaperButton>();

            BuildList(captionWidth);
            Refresh();
        }

        private string Current =>
            _options.Length == 0 ? "" : _options[Mathf.Clamp(_index, 0, _options.Length - 1)];

        private void BuildList(float captionWidth)
        {
            _list = new GameObject("Options", typeof(RectTransform), typeof(Image));
            _list.transform.SetParent(transform, false);

            // ⚠️ IGNORED BY THE LAYOUT, so opening it overlays the rows below rather than pushing
            // them. See the class note: this is the fault the lobby chat shipped this same day.
            _list.AddComponent<LayoutElement>().ignoreLayout = true;

            var plate = _list.GetComponent<Image>();
            // ⚠️ THE OPEN LIST IS A `Sheet`, so it reads as a piece of card laid OVER the drawer
            // rather than as a hole in it. It is the same surface the drawer itself is, one layer
            // up, and its own cast shadow is what separates the two.
            PaperSkin.Apply(_list, PaperCraft.Surface.Sheet);
            plate.type = Image.Type.Sliced;
            plate.color = Color.white;

            // ⚠️ IT EATS CLICKS. An open list over a rail full of controls that let a press fall
            // through would change the map and the mode from one click.
            plate.raycastTarget = true;

            _listRect = plate.rectTransform;
            _listRect.anchorMin = new Vector2(0.0f, 0.0f);
            _listRect.anchorMax = new Vector2(1.0f, 0.0f);
            _listRect.pivot = new Vector2(0.5f, 1.0f);
            _listRect.offsetMin = new Vector2(captionWidth + 8.0f, 0.0f);
            _listRect.offsetMax = new Vector2(0.0f, 0.0f);

            // ⚠️⚠️ ITS OWN CANVAS, OR IT DRAWS UNDER THE ROW BELOW IT. Sibling order decides
            // draw order in Unity UI, and this object is the LAST child of its own row rather
            // than of the rail, so every row underneath is painted after it. `docs/TODO.md` § 99
            // is the same trap one property over: a nested canvas ignores `sortingOrder` unless
            // `overrideSorting` is set, which is why both lines are here.
            //
            // ⚠️⚠️⚠️ AND THE ORDER IS DERIVED FROM THE HOST RATHER THAN WRITTEN DOWN, BECAUSE THE
            // WRITTEN-DOWN ONE WAS 60 AND EVERY SCREEN THAT HOSTS THIS CONTROL SITS AT 100 OR
            // ABOVE. 🧑 2026-09-02, off the `ui-redesign` player: **"drop down ui broken af"** and
            // then **"i cant even change map"**, with the open MAP list drawing BEHIND the MODE,
            // BOTS and RULES rows and ILALIM NG TULAY showing as a sliver between two of them.
            //
            // **`Panel` and `ConvertedScreen` build their canvas through `MenuKit.BuildCanvas` at
            // `sortingOrder` 100**, so an overriding child at 60 sorts UNDER the whole panel: not
            // just under the rows, under everything the panel draws. The rows are opaque and they
            // are raycast targets, so they took the presses too, which is the half that turned a
            // drawing fault into "i cant even change map".
            //
            // ⚠️⚠️ THIS IS § 99'S OWN PREDICTED REGRESSION, ARRIVING SIX SECTIONS LATER. That entry
            // set `overrideSorting = true` in `MenuKit.BuildCanvas` and closed with *"the
            // regression to look for is something covering something else"*. Before it, every
            // nested `sortingOrder` was inert and this list drew in hierarchy order, which
            // happened to be correct; the day those numbers started meaning something, **60 began
            // losing to a number this file had never heard of.** A constant that only worked while
            // the system ignored it is not a constant, and the comment above it was already right
            // about the mechanism while being wrong about the value.
            //
            // ⚠️ SO IT IS RELATIVE, AND A SCREEN MAY CHANGE ITS OWN ORDER WITHOUT BREAKING THIS.
            // A popup belongs above whatever opened it and nothing else in this file can know what
            // that is. `HostSortingOrder` walks to the nearest ancestor that actually decides
            // sorting (a root canvas, or a nested one with `overrideSorting`), because a nested
            // canvas WITHOUT the flag carries a number that does nothing, and reading it would put
            // the same class of bug back one level up.
            _listCanvas = _list.AddComponent<Canvas>();
            _listCanvas.overrideSorting = true;
            _listCanvas.sortingOrder = HostSortingOrder(_list) + PopupLift;
            _list.AddComponent<GraphicRaycaster>();

            var column = new GameObject("Column", typeof(RectTransform));
            column.transform.SetParent(_list.transform, false);
            MenuKit.Stretch((RectTransform)column.transform, -6.0f);

            var layout = column.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 2.0f;
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            for (int i = 0; i < _options.Length; i++)
            {
                int choice = i;

                var optionGo = new GameObject($"Option_{i}", typeof(RectTransform), typeof(Image));
                optionGo.transform.SetParent(column.transform, false);

                var optionElement = optionGo.AddComponent<LayoutElement>();
                optionElement.minHeight = OptionHeight;
                optionElement.preferredHeight = OptionHeight;
                optionElement.flexibleHeight = 0.0f;

                var optionFace = optionGo.GetComponent<Image>();
                PaperSkin.Apply(optionGo, PaperCraft.Surface.Tray);
                optionFace.type = Image.Type.Sliced;

                var optionButton = optionGo.AddComponent<Button>();
                optionButton.targetGraphic = optionFace;
                optionButton.transition = Selectable.Transition.None;
                optionButton.onClick.AddListener(() => Choose(choice));
                FocusRing.Attach(optionGo, 2.0f);

                var text = MenuKit.Label(optionGo.transform, _options[i], PaperKit.Body,
                                         UiTheme.PaperInk,
                                         Vector2.zero, Vector2.zero, Vector2.zero,
                                         TextAnchor.MiddleLeft);
                text.name = "Label";
                text.raycastTarget = false;
                MenuKit.Stretch(text.rectTransform, 0.0f);
                text.rectTransform.offsetMin = new Vector2(14.0f, 0.0f);
                text.rectTransform.offsetMax = new Vector2(-12.0f, 0.0f);

                // ⚠️ EVERY OPTION REACTS TO THE POINTER. A list of four identical trays where
                // nothing lights up under the cursor is a list a player has to aim at twice, and
                // `MenuSfx` rides on this component, so without it choosing a map was the one
                // silent press in the front end.
                optionGo.AddComponent<PaperButton>();

                _optionButtons.Add(optionButton);
            }

            float height = (OptionHeight * _options.Length)
                           + (2.0f * Mathf.Max(0, _options.Length - 1)) + 12.0f;
            _listRect.sizeDelta = new Vector2(_listRect.sizeDelta.x, height);

            _list.SetActive(false);
        }

        private void Toggle()
        {
            MenuSfx.Click();

            if (_list.activeSelf) { Close(); return; }

            // ⚠️ THE OTHER ONE CLOSES FIRST. See the class note.
            if (_open != null && _open != this) _open.Close();

            _open = this;

            // ⚠️⚠️ THE ORDER IS RE-DERIVED ON EVERY OPEN, NOT ONLY WHERE IT IS FIRST SET IN
            // `BuildList`. That runs inside `Construct`, and a row is routinely built BEFORE it is
            // parented into the screen that hosts it: the walk would then find no ancestor canvas,
            // answer 0, and leave the list sorting at `PopupLift` alone under a hub at 500. The
            // hierarchy is always complete by the time somebody presses the control, so this is
            // the one moment the answer is guaranteed correct.
            //
            // ⚠️ IT IS ALSO WHAT MAKES THE CONTROL SURVIVE BEING REPARENTED. `LobbyChrome` moves
            // authored nodes between rails while it builds, and a sorting order captured at
            // construction is a fact about a parent the control may no longer have.
            if (_listCanvas != null)
                _listCanvas.sortingOrder = HostSortingOrder(_list) + PopupLift;

            _list.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (_list != null) _list.SetActive(false);
            if (_open == this) _open = null;

            if (_caret != null) _caret.text = "▾";
        }

        private void Choose(int index)
        {
            _index = Mathf.Clamp(index, 0, Mathf.Max(0, _options.Length - 1));
            Close();
            Refresh();
            _picked?.Invoke(_index);
        }

        /// <summary>Called by the screen when the value changed somewhere else, which in a
        /// networked lobby is every time the host picks.</summary>
        public void SetIndex(int index)
        {
            _index = Mathf.Clamp(index, 0, Mathf.Max(0, _options.Length - 1));
            Refresh();
        }

        public void SetInteractable(bool on)
        {
            var button = GetComponentInChildren<Button>();
            if (button != null) button.interactable = on;

            // ⚠️ A DROPDOWN THAT CANNOT BE USED MUST NOT BE LEFT OPEN. Only the host may change
            // the map in a networked lobby, and a peer with a list hanging open over the rail is
            // a control that looks live and refuses every press.
            if (!on) Close();
        }

        private void Refresh()
        {
            if (_value != null)
            {
                _value.text = Current;

                // ⚠️ FITTED. `LAST TSINELAS STANDING` and `ILALIM NG TULAY` are both longer than
                // the well they sit in, and `MenuKit.Label` overflows rather than wrapping, so the
                // failure is a value drawn through the caret beside it.
                MenuKit.Fit(_value, _listRect.rect.width - 56.0f);
            }

            if (_caret != null) _caret.text = _list != null && _list.activeSelf ? "▴" : "▾";

            // ⚠️ THE CHOSEN OPTION IS AMBER IN THE LIST, so opening it answers "which one am I on"
            // as well as "what else is there".
            for (int i = 0; i < _optionButtons.Count; i++)
            {
                bool chosen = i == _index;

                // ⚠️⚠️ THE CHOSEN OPTION IS A `Live` PILL AND IT WAS A BOLD WORD. Weight alone is
                // the weakest of `game-ui-design`'s four ordering tools and it is the only one
                // this list was spending: four identical recessed trays, one of whose labels was
                // a little heavier than the others. 🧑, of this drawer: **"match settings ui look
                // ugly"**. `PaperKit.MarkLive` gives it the wood-dark plate with cream lettering
                // that every other "this is the one you are on" in the game now uses, which is a
                // value inversion of about 10:1 and costs no colour.
                //
                // ⚠️ THE FALLBACK IS THE OLD WEIGHT RULE rather than nothing, because `MarkLive`
                // answers false on a control with no paper skin and this list is also built by
                // callers that predate it.
                if (!PaperKit.MarkLive(_optionButtons[i], chosen, PaperCraft.Surface.Tray))
                {
                    var plain = _optionButtons[i].GetComponentInChildren<Text>();
                    if (plain != null)
                        plain.color = chosen ? UiTheme.PaperInk : UiTheme.PaperInkSoft;
                }

                var text = _optionButtons[i].GetComponentInChildren<Text>();
                if (text != null) text.fontStyle = chosen ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        /// <summary>⚠️ ESCAPE CLOSES THE LIST BEFORE ANYTHING ELSE READS IT, the same contract
        /// `LobbyChat.Update` has with the lobby's own Escape: one press means the innermost open
        /// thing.</summary>
        private void Update()
        {
            if (_list == null || !_list.activeSelf) return;
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            ScreenTakeover.ConsumeEscape();
            Close();
        }

        private void OnDisable() => Close();
    }
}
