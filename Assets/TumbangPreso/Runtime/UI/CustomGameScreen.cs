using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// CUSTOM GAME: the rules this match is played by, as a screen a player can actually open.
    ///
    /// ⚠️⚠️ PHASE 12 PUTS THIS FIRST AND EVERYTHING ELSE IN THE PHASE GETS CHEAPER AFTER IT.
    /// `docs/FUTURE.md` § 19.12 orders it explicitly: *"Custom games first, then modes, then map
    /// rotation. Every mode is cheaper once custom games exist"*, and *"it is also the tournament
    /// tool for Phase 17"*. `CustomGameRules` has carried the bounds, the refusal, the wire form
    /// and the defaults, with `Core.Tests` coverage, since the day the phase was written.
    /// **Nothing in `Assets/` ever constructed a `CustomRules`.** That is `CLAUDE.md` § 6.2a in
    /// one sentence: *"A FEATURE WITHOUT A SCREEN IS NOT SHIPPED"*, and a data model with tests
    /// is one row further from shipped than a row on a panel.
    ///
    /// ⚠️⚠️ AND IT IS WHAT FINALLY WRITES `SceneFlow.SelectedTsinelas`. `docs/TODO.md` § 130.13
    /// built LAST TSINELAS STANDING's whole match half and left that field *"clamped on the host
    /// and written by nothing"*: the format ships, the stock is fixed at three, and there has
    /// never been a control for it. This screen is that control.
    ///
    /// ---
    ///
    /// § `CLAUDE.md` § 6.2's FOUR QUESTIONS, ANSWERED BEFORE THE SCREEN WAS WRITTEN
    ///
    /// 1. **What is the ONE thing on this screen?** *The match you are about to play, said in one
    ///    line.* The headline is a live summary (`HERO STRIKE · STANDARD · 8 ROUNDS · 90s`) and it
    ///    rewrites itself on every press. Every row underneath exists to change that sentence, and
    ///    the sentence is what the player checks before pressing the one action.
    /// 2. **What is the first press, and can the player guess it?** FORMAT, the top row, because
    ///    it is the choice that changes the most and it renames the game. It is the first thing
    ///    under the headline and it is labelled with the word the lobby already uses (RULES ->
    ///    FORMAT is deliberately the same list, drawn from `CustomGameRules.FormatName`).
    /// 3. **What is on screen that the player does not need RIGHT NOW?** Three things, and all
    ///    three are hidden rather than greyed: TSINELAS EACH exists only under LAST TSINELAS,
    ///    BOT SKILL only when there are bots, and PASSWORD only when the room is private. **THE
    ///    ROOM is a closed group with a one-line summary on its header** (`No bots · open to
    ///    anybody`), which is § 6.2's *"a group closed by default with a one-line summary on its
    ///    header beats the same rows always open"*.
    /// 4. **How do they get out, and is it one press?** Escape, once, like every other screen
    ///    (`CLAUDE.md` § 6.3). Changes apply live, the way the settings panel's do, so there is
    ///    nothing to lose by leaving and no confirmation to invent.
    ///
    /// ⚠️⚠️ THE PRIMARY IS NEVER DEAD, WHICH IS § 108'S EQUIP BUTTON. `CustomGameRules.Refusal`
    /// answers a SENTENCE rather than a bool precisely so a screen can say why, and that sentence
    /// is drawn under the action while the action is uninteractable. A button that does nothing
    /// when pressed is the fault this project has shipped twice.
    ///
    /// ⚠️ RANKED IS SAID OUT LOUD RATHER THAN DISCOVERED. `CustomGameRules.CanBeRanked` is the
    /// one rule in that file its header calls *"not negotiable"*, and a player who has just set a
    /// 12-round match with three bots needs to know it does not touch the ladder BEFORE they play
    /// it, not on the results board.
    ///
    /// ⚠️⚠️ A CLIENT SEES THIS SCREEN READ-ONLY RATHER THAN NOT AT ALL. `docs/VISION.md` § 4:
    /// the host decides everything. Hiding the door from a client would be § 96's fault (a
    /// destination with no visible door) applied to the four people who most need to know what
    /// they are about to play. Every control is uninteractable and the headline says who owns it.
    /// </summary>
    public sealed class CustomGameScreen : MonoBehaviour
    {
        /// <summary>
        /// ⚠️ 530, ABOVE `CustomCharacterScreen`'s 520, THE SIGN-IN SCREEN'S 510 AND THE HUB'S
        /// 500. This opens FROM the lobby, which is a converted screen, and `docs/TODO.md` § 108.2
        /// is what a wrong number costs: two screens built themselves correctly at 95 and were
        /// drawn underneath the screen that opened them, so the press appeared to do nothing.
        /// `MenuKit.BuildCanvas` sets `overrideSorting`, without which the number is inert (§ 99).
        /// </summary>
        private const int SortingOrder = 530;

        private Canvas _canvas;
        private GameObject _root;
        private RectTransform _list;
        private ScrollRect _scroll;
        private Text _headline;
        private Text _ranked;
        private Text _refusal;
        private Button _use;
        private InputField _password;
        private GameObject _passwordRow;

        /// <summary>
        /// The rule set being edited, held apart from `SceneFlow` until it is applied.
        ///
        /// ⚠️⚠️ A WORKING COPY, AND IT IS NOT `CustomCharacterScreen`'S REASON. There, BACK
        /// discards because a save slot you cannot leave without overwriting is not a save slot.
        /// Here **every change applies the moment it is made** (see `Apply`), and the copy exists
        /// for a different reason: `CustomGameRules.Refusal` has to be asked about a candidate
        /// rule set, and a set that has already been written to `SceneFlow` is one the match would
        /// try to play. **The clone is what makes an invalid intermediate state safe**, which is
        /// what lets the rounds stepper pass through a number the round length then makes illegal.
        /// </summary>
        private CustomRules _editing;

        /// <summary>Whether this machine may change anything. See the class header.</summary>
        private static bool MayEdit => !SceneFlow.Networked || NetAuthority.IsHost;

        public bool IsOpen => _root != null && _root.activeSelf;

        public static CustomGameScreen Ensure()
        {
            var found = UnityEngine.Object.FindAnyObjectByType<CustomGameScreen>();
            if (found != null) return found;

            var go = new GameObject("CustomGameScreen");
            return go.AddComponent<CustomGameScreen>();
        }

        private void Awake() => ScreenTakeover.Register(this, () => IsOpen);

        private void OnDestroy() => ScreenTakeover.Unregister(this);

        /// <summary>
        /// ⚠️ ESCAPE BACKS OUT, LIKE EVERY OTHER SCREEN. `CLAUDE.md` § 6.3: *"a player who learns
        /// Escape is reliable and then meets one screen where it is not has learned that it is
        /// unreliable."* The press is SPENT here (`ScreenTakeover.ConsumeEscape`) so the lobby
        /// underneath does not also back out on it, which is the fault that once landed 🧑 on the
        /// boot login screen from the character maker.
        /// </summary>
        private void Update()
        {
            if (!IsOpen) return;
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            ScreenTakeover.ConsumeEscape();
            MenuSfx.Back();
            Close();
        }

        public void Open()
        {
            if (_root == null) Build();

            // ⚠️ THE LIVE RULES ARE THE STARTING POINT, NOT `Defaults`. A player who opens this
            // to change ONE thing must not have to reconstruct the other seven, which is
            // `CustomGameRules.Defaults`'s own note about a screen full of empty fields.
            _editing = SceneFlow.SelectedRules.Clone();

            if (_password != null) _password.text = _editing.Password ?? "";

            _root.SetActive(true);

            // ⚠️ THE POINTER IS RELEASED, LIKE EVERY OTHER MENU. `CursorMode`'s header: *"the
            // buttons don't work" has a cursor-shaped cause, and it is invisible in a screenshot.*
            CursorMode.Release();

            Refresh();
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
            _canvas = MenuKit.BuildCanvas(transform, "CustomGameCanvas");
            _canvas.sortingOrder = SortingOrder;

            _root = new GameObject("CustomGameRoot", typeof(RectTransform));
            _root.transform.SetParent(_canvas.transform, false);
            MenuKit.Stretch((RectTransform)_root.transform);

            // ⚠️⚠️ A SURFACE, NOT A SCRIM, AND `CLAUDE.md` § 6.2c QUESTION 3 IS WHY. A scrim buys
            // legibility over a live scene or separation from one; **the one thing on this screen
            // is a form**, every word on it already sits on paper, and the lobby behind it is a
            // second thing competing for the same eye. `CustomCharacterScreen` reached the same
            // answer for the same reason and 🧑 asked for it there by name: *"why can i see the
            // main menu"*, *"give it a solid brown background too or creme"*.
            //
            // ⚠️ IT IS ALSO THE BLOCKER (§ 6.2c question 4). An opaque `Image` with
            // `raycastTarget` on is what stops a press reaching the lobby underneath, and naming
            // that here is the rule that section exists to enforce: **when a full-screen graphic
            // goes, name its replacement blocker in the same commit.**
            MenuKit.Backdrop(_root.transform, UiTheme.Paper);

            BuildHeader();
            BuildList();
            BuildFooter();

            _root.SetActive(false);

            // ⚠️⚠️ ONE CALL DRESSES THE WHOLE SCREEN IN PAPER, SCOPED TO THIS SUBTREE.
            // `GodotPanel` and `GodotButton` are the choke points every converted screen is
            // skinned through, so editing either would repaint the main menu and the in-match HUD,
            // which 🧑 scoped out twice. `docs/TODO.md` § 119.2 and § 119.5.
            PaperDress.Screen(_root.transform);
        }

        /// <summary>
        /// The title, the live summary, and the password field.
        ///
        /// ⚠️⚠️ THE PASSWORD LIVES IN THE HEADER AND NOT IN THE LIST, AND THAT IS NOT A LAYOUT
        /// PREFERENCE. `UiRows.Field`'s own note records the reason: *"a field inside a list that
        /// rebuilds on every stepper press is a field that loses its caret"*, which is
        /// `docs/TODO.md` § 113 and which `CustomCharacterScreen` moved its NAME field out of the
        /// list to avoid. This list is rebuilt on every single change (that is how a closed group
        /// stays cheap), so a password typed into it would lose the caret on the first keystroke
        /// that changed anything.
        /// </summary>
        private void BuildHeader()
        {
            var head = MenuKit.Label(_root.transform, "CUSTOM GAME", UiRows.HeadingUnits + 8,
                UiTheme.PaperInk, new Vector2(0.0f, 1.0f), new Vector2(80.0f, -70.0f),
                new Vector2(700.0f, 56.0f), TextAnchor.MiddleLeft);
            MenuKit.Apply(head, PaperKit.FaceFor(head.fontSize), bold: true);

            // ⚠️⚠️ THE SUMMARY IS THE ONE THING ON THIS SCREEN AND IT IS A SENTENCE RATHER THAN A
            // BADGE. `docs/FUTURE.md` § 0.5b's per-phase table answers question 1 for Phase 12 as
            // *"what is different about this mode, in one line"*, and this is that line for the
            // whole rule set rather than for the format alone: a player reads it, recognises the
            // match they meant to set up, and presses the one action.
            _headline = MenuKit.Label(_root.transform, "", UiRows.LabelUnits,
                UiTheme.PaperInkSoft, new Vector2(0.0f, 1.0f), new Vector2(80.0f, -118.0f),
                new Vector2(900.0f, 30.0f), TextAnchor.MiddleLeft);

            _passwordRow = new GameObject("PasswordRow", typeof(RectTransform));
            _passwordRow.transform.SetParent(_root.transform, false);
            MenuKit.Place((RectTransform)_passwordRow.transform, new Vector2(1.0f, 1.0f),
                          new Vector2(-320.0f, -92.0f), new Vector2(420.0f, 52.0f));

            var cap = MenuKit.Label(_passwordRow.transform, "PASSWORD", UiRows.HintUnits,
                UiTheme.PaperInkSoft, new Vector2(0.0f, 1.0f), new Vector2(0.0f, 8.0f),
                new Vector2(200.0f, 20.0f), TextAnchor.LowerLeft);
            cap.raycastTarget = false;

            _password = UiRows.Field(_passwordRow.transform, "four to sixteen characters",
                                     CustomGameRules.MaxPasswordLength);
            MenuKit.Place(_password.GetComponent<RectTransform>(), new Vector2(0.0f, 0.0f),
                          new Vector2(210.0f, 18.0f), new Vector2(420.0f, 40.0f));

            // ⚠️ WRITTEN ON EVERY KEYSTROKE RATHER THAN ON SUBMIT. A field that only commits on
            // Enter is a field a player fills in, walks away from and finds empty, and there is
            // nothing to validate per character: `IsPasswordUsable` answers about the finished
            // string and the refusal line says so live.
            _password.onValueChanged.AddListener(text =>
            {
                if (_editing == null) return;
                _editing.Password = text ?? "";
                Apply();
            });
        }

        private void BuildList()
        {
            var host = new GameObject("ListHost", typeof(RectTransform));
            host.transform.SetParent(_root.transform, false);

            var rect = (RectTransform)host.transform;
            rect.anchorMin = new Vector2(0.0f, 0.0f);
            rect.anchorMax = new Vector2(1.0f, 1.0f);
            rect.offsetMin = new Vector2(80.0f, 150.0f);
            rect.offsetMax = new Vector2(-80.0f, -160.0f);

            _list = UiRows.ScrollList(host.transform, "Rules", out _scroll);
        }

        private void BuildFooter()
        {
            _ranked = MenuKit.Label(_root.transform, "", UiRows.HintUnits, UiTheme.PaperInkSoft,
                new Vector2(0.0f, 0.0f), new Vector2(80.0f, 118.0f),
                new Vector2(900.0f, 24.0f), TextAnchor.MiddleLeft);

            // ⚠️ THE REFUSAL IS `MenuRed` AND IT IS THE ONLY RED ON THE SCREEN. One accent for
            // one job (`FUTURE.md` § 0.5b's four ordering tools), and a colour that means "this
            // cannot be played" must not also mean anything decorative.
            _refusal = MenuKit.Label(_root.transform, "", UiRows.HintUnits, UiTheme.MenuRed,
                new Vector2(0.0f, 0.0f), new Vector2(80.0f, 92.0f),
                new Vector2(900.0f, 24.0f), TextAnchor.MiddleLeft);

            // ⚠️⚠️ THE ONE ACTION IS A `PaperKit` ACTION, NOT A `WoodButton`, AND `CLAUDE.md`
            // § 6.5 IS AN ENTIRE SECTION ABOUT WHY. Until `PaperCraft.Surface.Action` existed the
            // one action on every paper screen was still a wooden object standing in a row of
            // paper ones, and 🧑 found it on four screens without connecting them: *"i dont get
            // why theres rounded sshit next to square shit"*, *"it feells so flat"*.
            _use = MenuKit.WoodButton(_root.transform, "USE THESE RULES", new Vector2(1.0f, 0.0f),
                new Vector2(-260.0f, 78.0f), new Vector2(440.0f, 62.0f), OnUse);
            PaperKit.MakeAction(_use.gameObject, PaperCraft.Accent.Green);

            MenuKit.WoodButton(_root.transform, "RESET TO DEFAULTS", new Vector2(0.0f, 0.0f),
                new Vector2(210.0f, 46.0f), new Vector2(300.0f, 44.0f), OnReset);
        }

        // -------------------------------------------------------------------
        // § THE ROWS
        // -------------------------------------------------------------------

        private bool _roomOpen;

        /// <summary>
        /// Rebuild every row from `_editing`.
        ///
        /// ⚠️⚠️ THE LIST IS REBUILT RATHER THAN UPDATED, WHICH IS WHAT MAKES A CLOSED GROUP FREE.
        /// `UiRows.Section`'s own note: *"A CLOSED GROUP IS NOT BUILT, IT IS NOT HIDDEN"*, so a
        /// caller simply does not add the rows of a group the player has shut. There is no hidden
        /// subtree recomputing layout, nothing to keep in step, and the scroll height is honest
        /// about what is on screen. `PlayerHub.Show` is the same pattern on the same rows.
        ///
        /// ⚠️ IT IS ALSO WHY THE THREE CONDITIONAL ROWS ARE CHEAP. TSINELAS EACH, BOT SKILL and
        /// PASSWORD are not disabled controls, they are rows that were never added, so there is
        /// no greyed-out furniture teaching the player about a setting that does not apply.
        /// </summary>
        private void Refresh()
        {
            if (_list == null || _editing == null) return;

            for (int i = _list.childCount - 1; i >= 0; i--)
                Destroy(_list.GetChild(i).gameObject);

            bool editable = MayEdit;

            UiRows.Section(_list, "THE MATCH",
                "What is played, how long it lasts, and how it is won.");

            UiRows.StepperRow(_list, "FORMAT",
                CustomGameRules.FormatName(_editing.Format),
                (int)_editing.Format, FormatCount,
                v => { _editing.Format = (MatchFormat)Mathf.Clamp(v, 0, FormatCount - 1); Apply(); },
                CustomGameRules.FormatBlurb(_editing.Format));

            UiRows.StepperRow(_list, "MODE",
                _editing.Mode == GameMode.HeroStrike ? "HERO STRIKE" : "CLASSIC",
                _editing.Mode == GameMode.HeroStrike ? 1 : 0, 2,
                v => { SetMode(v == 1 ? GameMode.HeroStrike : GameMode.Classic); },
                _editing.Mode == GameMode.HeroStrike
                    ? "Six heroes with two skills and an ultimate each. Eight rounds as it ships."
                    : "The street game, twelve characters, no powers. Four rounds as it ships.");

            UiRows.StepperRow(_list, "ROUNDS", _editing.Rounds.ToString(),
                _editing.Rounds - CustomGameRules.MinRounds, RoundOptionCount,
                v => { _editing.Rounds = CustomGameRules.MinRounds + v; Apply(); },
                $"{CustomGameRules.MinRounds} to {CustomGameRules.MaxRounds}. " +
                $"The shipped {(_editing.Mode == GameMode.HeroStrike ? "Hero Strike" : "Classic")} " +
                $"match is {MatchRules.RoundCountFor(_editing.Mode)}.");

            UiRows.StepperRow(_list, "ROUND LENGTH", _editing.RoundSeconds + "s",
                SecondsIndex(_editing.RoundSeconds), SecondsOptions.Length,
                v => { _editing.RoundSeconds = SecondsOptions[Mathf.Clamp(v, 0, SecondsOptions.Length - 1)]; Apply(); },
                $"{CustomGameRules.MinRoundSeconds} to {CustomGameRules.MaxRoundSeconds} seconds. " +
                $"The game ships at {(int)Balance.RoundTime}.");

            UiRows.StepperRow(_list, "SCORE TARGET", ScoreTargetLabel(_editing.ScoreTarget),
                TargetIndex(_editing.ScoreTarget), TargetOptions.Length,
                v => { _editing.ScoreTarget = TargetOptions[Mathf.Clamp(v, 0, TargetOptions.Length - 1)]; Apply(); },
                "Ends the match early when somebody reaches it. OFF plays every round.");

            // ⚠️ ONLY UNDER THE FORMAT IT BELONGS TO. `SceneFlow.SelectedTsinelas`'s own note:
            // *"it is read ONLY when SelectedFormat is LastTsinelas and is meaningless
            // otherwise"*, and a control for a meaningless number is § 6.2's third failure.
            if (_editing.Format == MatchFormat.LastTsinelas)
            {
                UiRows.StepperRow(_list, "TSINELAS EACH", _editing.Tsinelas.ToString(),
                    _editing.Tsinelas - CustomGameRules.MinTsinelas, TsinelasOptionCount,
                    v => { _editing.Tsinelas = CustomGameRules.MinTsinelas + v; Apply(); },
                    "How many an attacker starts a round with. Lose them all and you are out " +
                    $"for that round. The format ships at {CustomGameRules.StartingTsinelas}.");
            }

            UiRows.Gap(_list, 8.0f);

            UiRows.Section(_list, "THE ROOM", RoomSummary(), _roomOpen,
                () => { _roomOpen = !_roomOpen; MenuSfx.Click(); Refresh(); });

            if (_roomOpen)
            {
                // ⚠️⚠️ ONE CONTROL FOR THE BOTS, NOT A COUNT AND A TIER, AND THAT IS A
                // DELIBERATE NARROWING RATHER THAN AN OMISSION. `CustomRules.Bots` is a COUNT
                // with a bound of `MaxBots` 3, and nothing in this game can currently fill a
                // PARTIAL number of empty seats: `GameLaunch.AllBots` and the lobby's own BOTS
                // row are a difficulty list whose last entry is **NONE**, meaning "no filler bots
                // at all". A stepper offering 0, 1, 2, 3 would be four options of which two do
                // exactly the same thing.
                //
                // ⚠️ `docs/TODO.md` § 108 IS WHY THAT MATTERS: an EQUIP button with no listener
                // and a CUSTOMIZE LOADOUT button opening a screen drawn underneath the screen
                // that opened it. **Both looked fine and both did nothing.** A control this
                // screen cannot honour is that fault with a nicer excuse, so the row offered here
                // is the one the game can obey, and `Bots` is written from it as all-or-nothing.
                UiRows.StepperRow(_list, "BOTS", BotLabel(_editing),
                    BotIndex(_editing), DifficultyCount + 1,
                    SetBots,
                    "Filler seats when there are not four people. NORMAL is the tier every " +
                    "balance number in this project was measured at, and a match with a bot in " +
                    "it never counts for your rank.");

                UiRows.ButtonRow(_list, "PRIVATE ROOM", _editing.Private ? "ON" : "OFF",
                    () => { _editing.Private = !_editing.Private; MenuSfx.Click(); Apply(); Refresh(); },
                    "A private room is not published to the online pool. The join code still works.");

                if (_editing.Private)
                {
                    // ⚠️ THE ROW IS A POINTER TO THE FIELD IN THE HEADER, NOT A SECOND FIELD.
                    // See `BuildHeader`: a field in this list loses its caret on every rebuild,
                    // and two fields for one value is `docs/TODO.md` § 94.1's four copies of one
                    // lookup all agreeing on the wrong answer.
                    UiRows.ValueRow(_list, "PASSWORD",
                        string.IsNullOrEmpty(_editing.Password) ? "not set" : "set",
                        "Typed in the box at the top right. It is compared on the host and never " +
                        "travels on the wire.");
                }
            }

            UiRows.Gap(_list, 10.0f);

            RefreshHeadline();
            RefreshInteractable(editable);

            // ⚠️ THE LIST GOES BACK TO THE TOP ONLY WHEN IT WAS ALREADY THERE. A rebuild that
            // always scrolled home would throw a player who had opened THE ROOM back to FORMAT
            // every time they changed a bot count.
            if (_scroll != null && _scroll.verticalNormalizedPosition > 0.99f)
                _scroll.verticalNormalizedPosition = 1.0f;
        }

        /// <summary>
        /// ⚠️ THE HEADER SUMMARY IS THE REASON THE GROUP IS WORTH CLOSING. `CLAUDE.md` § 6.2
        /// question 3: *"a group closed by default with a one-line summary on its header beats the
        /// same rows always open, and the summary is what makes it worth opening."* A closed group
        /// with no summary is a mystery the player has to open to resolve, which is worse than
        /// four visible rows.
        /// </summary>
        private string RoomSummary()
        {
            string bots = _editing.Bots == 0
                ? "No bots"
                : "Bots on " + DifficultyName(_editing.BotDifficulty).ToLowerInvariant();

            string door = _editing.Private
                ? (string.IsNullOrEmpty(_editing.Password) ? "private, no password" : "private, password set")
                : "open to anybody with the code";

            return bots + " · " + door;
        }

        private void RefreshHeadline()
        {
            if (_headline != null)
            {
                string target = _editing.ScoreTarget > 0
                    ? " · first to " + _editing.ScoreTarget
                    : "";

                string stock = _editing.Format == MatchFormat.LastTsinelas
                    ? " · " + _editing.Tsinelas + " tsinelas"
                    : "";

                _headline.text =
                    (_editing.Mode == GameMode.HeroStrike ? "HERO STRIKE" : "CLASSIC") +
                    " · " + CustomGameRules.FormatName(_editing.Format) +
                    " · " + _editing.Rounds + (_editing.Rounds == 1 ? " round" : " rounds") +
                    " · " + _editing.RoundSeconds + "s" + target + stock;

                if (!MayEdit)
                    _headline.text += "   (the host sets these)";
            }

            if (_ranked != null)
            {
                _ranked.text = CustomGameRules.CanBeRanked(_editing)
                    ? "These are the shipped rules, so this match counts for your rank."
                    : "Custom rules. This match does not count for your rank.";
            }

            string refusal = CustomGameRules.Refusal(_editing);
            if (_refusal != null) _refusal.text = refusal;
            if (_use != null) _use.interactable = MayEdit && string.IsNullOrEmpty(refusal);

            if (_passwordRow != null)
                _passwordRow.SetActive(_editing.Private && MayEdit);
        }

        private void RefreshInteractable(bool editable)
        {
            if (_list == null) return;

            foreach (var selectable in _list.GetComponentsInChildren<Selectable>(true))
                selectable.interactable = editable;
        }

        // -------------------------------------------------------------------
        // § APPLYING
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ A CHANGE APPLIES THE MOMENT IT IS MADE, AND `USE THESE RULES` IS A DOOR RATHER
        /// THAN A COMMIT. Every settings-shaped screen in this game works this way and a player
        /// who has learned that from `ConvertedSettingsPanel` must not meet one screen where a
        /// change is silently discarded. **The action closes the screen**; it does not save
        /// anything the rows have not already saved, and its label says what it does rather than
        /// promising a write.
        ///
        /// ⚠️ AN INVALID SET IS STILL WRITTEN LOCALLY AND STILL REFUSED AT THE START BUTTON.
        /// Refusing to store an intermediate state would make the rounds stepper unable to pass
        /// through a number the round length has temporarily made illegal, and `Refusal` exists
        /// precisely so the screen can hold a bad set and say why.
        /// </summary>
        private void Apply()
        {
            if (_editing == null) return;

            SceneFlow.SetSelectedRules(_editing);

            // ⚠️⚠️ THE ROOM IS TOLD ON EVERY CHANGE, AND ONLY THE HOST MAY TELL IT.
            // `MatchRpc.SelectRulesServerRpc` is the single path (`docs/TODO.md` § 38.5: three
            // dead protocols came from adding a second one), it clamps on the host before it
            // broadcasts, and it is what moved `NetSession.ProtocolVersion` to 23. **A rule set
            // the host has and the other three have not is two different games sharing one
            // scoreboard**, which is the sentence that constant exists for.
            //
            // ⚠️ ON EVERY CHANGE RATHER THAN ON CLOSE, because a player watching the lobby fill
            // up while the host is still setting rules should see the rules move. It is one short
            // string per press on a control nobody holds down.
            if (SceneFlow.Networked && NetAuthority.IsHost)
                Net.MatchRpc.Instance?.SelectRulesServerRpc(CustomGameRules.ToWire(_editing));

            RefreshHeadline();
        }

        private void SetMode(GameMode mode)
        {
            if (_editing.Mode == mode) return;

            // ⚠️⚠️ THE ROUND COUNT FOLLOWS THE MODE **ONLY IF IT WAS STILL THE SHIPPED ONE**, and
            // that condition is the whole of this method. `docs/VISION.md` § 1: Classic plays four
            // rounds and Hero Strike eight, *"the role schedule and scoring stay shared"*. A
            // player who switched mode and silently kept 8 rounds of Classic would have doubled
            // the taya rotation without asking. A player who had deliberately typed 3 and then
            // switched mode must not have their 3 overwritten. **Following a default is not the
            // same as overwriting a choice.**
            bool wasDefault = _editing.Rounds == MatchRules.RoundCountFor(_editing.Mode);

            _editing.Mode = mode;
            if (wasDefault) _editing.Rounds = MatchRules.RoundCountFor(mode);

            Apply();
            Refresh();
        }

        private void OnUse()
        {
            MenuSfx.Click();
            Apply();
            Close();
        }

        private void OnReset()
        {
            MenuSfx.Click();

            // ⚠️ THE MODE SURVIVES A RESET AND NOTHING ELSE DOES. `Defaults(mode)` takes the mode
            // as its argument precisely because the mode is not a custom rule: it is which of the
            // two games this is (`docs/VISION.md` § 1), and resetting the RULES to defaults should
            // not silently move a player from Classic to Hero Strike.
            _editing = CustomGameRules.Defaults(_editing.Mode);

            if (_password != null) _password.text = "";

            Apply();
            Refresh();
        }

        // -------------------------------------------------------------------
        // § THE OPTION TABLES
        //
        // ⚠️ EVERY BOUND COMES OUT OF `CustomGameRules` AND NONE IS TYPED HERE. That file's own
        // header: *"EVERY BOUND IN HERE IS A BOUND ON THE HOST, NOT A SUGGESTION TO IT."* A second
        // copy of `MaxRounds` on a screen is § 5's drift rule waiting to happen, where the prose
        // and the code disagree and nobody can say which is the bug.
        // -------------------------------------------------------------------

        private static int FormatCount => (int)MatchFormat.Mirror + 1;

        private static int RoundOptionCount
            => CustomGameRules.MaxRounds - CustomGameRules.MinRounds + 1;

        private static int TsinelasOptionCount
            => CustomGameRules.MaxTsinelas - CustomGameRules.MinTsinelas + 1;

        /// <summary>
        /// ⚠️⚠️ THREE TIERS, NOT FOUR, AND THE LOBBY'S FOUR-ENTRY LIST IS WHY THAT IS EASY TO GET
        /// WRONG. `Difficulty` is `Bata`, `Normal`, `Astig` and nothing else;
        /// `ConvertedMatchSetup.Difficulties` has FOUR rows because its last one is **NONE**,
        /// which is not a tier at all but "no filler bots". This screen splits those two facts
        /// into two controls (BOTS is a count, BOT SKILL is a tier), so there is no fourth row to
        /// invent and no index that means something different from the enum's.
        ///
        /// ⚠️ THE NAMES ARE THE LOBBY'S, so a player who set HARD there and opens this screen
        /// reads HARD. `Difficulty.Astig` is the id and `HARD` is what it has always been called
        /// on screen; renaming it here would be teaching two words for one tier.
        /// </summary>
        private static int DifficultyCount => (int)Difficulty.Astig + 1;

        private static string DifficultyName(int tier) => tier switch
        {
            (int)Difficulty.Bata => "EASY",
            (int)Difficulty.Normal => "NORMAL",
            _ => "HARD",
        };

        /// <summary>
        /// The BOTS control is one list: three tiers, then NONE.
        ///
        /// ⚠️⚠️ THE ORDER IS THE LOBBY'S OWN AND NOT A NEW ONE. `ConvertedMatchSetup.Difficulties`
        /// reads `EASY, NORMAL, HARD, NONE` and `AIController.NoBotsIndex` is what that last row
        /// means; a player who set HARD there and opens this screen must read HARD, and a second
        /// ordering would teach two different meanings for one index.
        /// </summary>
        private static int BotIndex(CustomRules rules)
            => rules.Bots <= 0 ? DifficultyCount
             : Mathf.Clamp(rules.BotDifficulty, 0, DifficultyCount - 1);

        private static string BotLabel(CustomRules rules)
            => rules.Bots <= 0 ? "NONE" : DifficultyName(rules.BotDifficulty);

        /// <summary>
        /// ⚠️⚠️ IT WRITES THE SETTING AND CALLS `AIController.ApplyDifficulty` AS WELL AS THE
        /// RULE SET, because the bots are the one custom rule this game was already obeying
        /// through a different door. `ConvertedMatchSetup.OnDifficultyCycle` does exactly these
        /// two things, and a screen that wrote only `CustomRules.Bots` would be a control that
        /// changed a record nothing reads, which is `docs/TODO.md` § 108's dead button.
        ///
        /// ⚠️ THE COUNT IS ALL OR NOTHING, WHICH THE ROW'S OWN NOTE ARGUES FOR: `MaxBots` when
        /// bots are wanted and 0 when they are not. It is what the game can currently honour, and
        /// `CanBeRanked` only ever asks whether it is zero.
        /// </summary>
        private void SetBots(int index)
        {
            int clamped = Mathf.Clamp(index, 0, DifficultyCount);
            bool none = clamped >= DifficultyCount;

            _editing.Bots = none ? 0 : CustomGameRules.MaxBots;
            if (!none) _editing.BotDifficulty = clamped;

            var settings = Settings.SettingsStore.Current;
            if (settings != null)
            {
                settings.AiDifficulty = none ? AIController.NoBotsIndex : clamped;
                Settings.SettingsStore.Save();
            }

            AIController.ApplyDifficulty(none ? AIController.NoBotsIndex : clamped);

            Apply();
            Refresh();
        }

        /// <summary>
        /// ⚠️⚠️ ROUND LENGTH STEPS IN FIFTEENS RATHER THAN IN SECONDS, AND THAT IS A CONTROL
        /// DECISION RATHER THAN A RANGE ONE. `CustomGameRules` allows 30 to 180, which is **151
        /// distinct values**: a stepper over 151 options is a control a player holds down and
        /// watches, and nobody has ever wanted a 97 second round. Eleven steps of fifteen cover
        /// the same range, land on every number anybody would name, and include the shipped 90.
        ///
        /// ⚠️ THE WIRE STILL CARRIES ANY VALUE IN THE RANGE. `CustomGameRules.ClampRoundSeconds`
        /// is what bounds a rule set arriving from a peer, and it is untouched: this table is
        /// what this SCREEN offers, not what the format permits.
        /// </summary>
        private static readonly int[] SecondsOptions =
            { 30, 45, 60, 75, 90, 105, 120, 135, 150, 165, 180 };

        private static int SecondsIndex(int seconds)
        {
            int best = 0;
            int gap = int.MaxValue;

            for (int i = 0; i < SecondsOptions.Length; i++)
            {
                int d = Mathf.Abs(SecondsOptions[i] - seconds);
                if (d >= gap) continue;
                gap = d;
                best = i;
            }

            return best;
        }

        /// <summary>
        /// ⚠️ ZERO IS FIRST AND IT MEANS OFF. `CustomRules.ScoreTarget`'s own note: *"0 means
        /// play every round, which is how the game ships"*, so the OFF option is the shipped
        /// behaviour rather than a disabled state, and it is where the stepper starts.
        /// </summary>
        private static readonly int[] TargetOptions =
            { 0, 500, 1000, 1500, 2000, 2500, 3000, 4000, 5000 };

        private static int TargetIndex(int target)
        {
            for (int i = 0; i < TargetOptions.Length; i++)
                if (TargetOptions[i] == target) return i;

            return 0;
        }

        private static string ScoreTargetLabel(int target)
            => target <= 0 ? "OFF" : target.ToString();
    }
}
