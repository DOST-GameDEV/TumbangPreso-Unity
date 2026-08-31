using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Everything about you, in one place, laid out the way a settings screen is.
    ///
    /// ⚠️⚠️ IT REPLACES `AccountOverlay` AND THE TOP HALF OF `ProfileOverlay`, AND THE REASON IS
    /// ON THE RECORD AS FOUR SCREENSHOTS. 🧑, 2026-08-30: *"ui for player account is so ugly"*,
    /// *"theres liek 20 shits at once"*, *"everything is js shit on one block and is
    /// overwhelming"*, *"THINK ABT conncepts like visual hierarchy annd user experiennce"*. The
    /// panel he photographed asked six questions and offered six equally-weighted buttons on one
    /// brown rectangle, with DELETE ACCOUNT the same size and shape as SAVE PROFILE.
    ///
    /// **The redesign is three ideas and they are all borrowed from what he pointed at:**
    ///
    /// 1. **One job per tab.** Six actions became four destinations, and each has exactly one
    ///    primary thing to do. Editing who you are is not the same job as signing in, and neither
    ///    is looking at your stats.
    /// 2. **Rows, not a form.** `UiRows` builds a full-width row with the label hard left and the
    ///    control hard right, under a section header with one grey line of explanation. That is
    ///    Valorant's settings screen and PUBG's, which he sent as the reference.
    /// 3. **Hierarchy by size, not by colour.** The career tab opens with four big numbers and
    ///    then the detail; the old page opened with fifteen identical rows reading `0/0 (needs 10
    ///    throws)`, which is a screen telling a new player that the game is broken.
    ///
    /// ⚠️⚠️ AND THE DESTRUCTIVE ACTION IS NOT A PEER OF THE SAFE ONES. DELETE ACCOUNT sat between
    /// PLAY AS GUEST and CLOSE at the same size. It is now the last row of the account tab, under
    /// its own header, and it still takes two presses.
    /// </summary>
    public sealed class PlayerHub : MonoBehaviour
    {
        private enum Tab { Profile, Friends, Career, Matches, Account }

        private const int HistoryPageSize = 20;

        private Canvas _canvas;
        private GameObject _root;
        private RectTransform _list;
        private ScrollRect _scroll;

        private Text _handle, _levelChip, _state, _xpCount;
        private Image _xpFill;
        private Text _footerNote;
        private Button _footerAction;
        private Text _footerLabel;

        private readonly Dictionary<Tab, Button> _tabs = new Dictionary<Tab, Button>();
        private Tab _tab = Tab.Profile;

        /// <summary>
        /// Which groups the player has opened or closed, keyed by tab and heading.
        ///
        /// ⚠️⚠️ COLLAPSING IS WHAT KEEPS A LONG TAB READABLE, AND \U0001f9d1 ASKED FOR IT BY NAME:
        /// *"usually to make shit easier to navigate games use dropdownns and shit annd separate
        /// shit"*, *"u figure out which parts need this annd apply this logic"*. The career tab
        /// has six groups and thirty rows in it. **Open, that is the same wall of numbers the old
        /// page was**, just better aligned; the grouping only helps if the groups can be shut.
        ///
        /// ⚠️ THE DEFAULTS ARE NOT ALL THE SAME AND THAT IS THE DESIGN. What opens by default is
        /// what somebody arriving at that tab came for: the four headline career numbers, the
        /// display name, the account state. What starts closed is detail and danger: the six stat
        /// groups, the optional profile fields, the guest handover and the delete row.
        ///
        /// ⚠️ AND IT IS REMEMBERED PER SESSION RATHER THAN SAVED. A player who opens ATTACK to
        /// read it expects it open when they come back a minute later, and does not expect the
        /// game to remember a panel state across a reinstall. `SettingsStore` is for settings.
        /// </summary>
        private readonly Dictionary<string, bool> _groups = new Dictionary<string, bool>();

        private InputField _displayName, _country, _pronouns, _bio;
        private GameMode _mode = GameMode.Classic;
        private int _page;
        private List<MatchRecord> _shown = new List<MatchRecord>();
        private bool _deleteArmed;
        private string _notice = "";

        private SignInScreen _signIn;

        /// <summary>
        /// The four-player breakdown of one finished match.
        ///
        /// ⚠️⚠️ IT EXISTS BECAUSE DELETING `ProfileOverlay` WOULD OTHERWISE HAVE DELETED A
        /// SHIPPED FEATURE. That class carried a popup showing every seat's line for one record,
        /// and `docs/TODO.md` § 92.4 listed it as the one thing the rebuild dropped. A redesign
        /// that quietly loses a screen is a regression wearing a better layout.
        /// </summary>
        private GameObject _detail;
        private Text _detailTitle, _detailBody;

        /// <summary>
        /// Raised with true when the hub opens and false when it closes.
        ///
        /// ⚠️⚠️ THE NAMEPLATE HAS TO GO AWAY WHILE THIS IS UP, AND THE FIRST SCREENSHOT SHOWS
        /// WHY. The plate sits at the top left of the menu and the hub header puts the same handle
        /// in the same place at twice the size, so the two drew on top of each other: the name
        /// appeared twice, once small and once large, overlapping. **Two canvases with different
        /// sorting orders is not a defence**, because the plate is the thing the player just
        /// pressed and leaving it lit under a full-screen panel is what makes an overlay feel
        /// bolted on.
        /// </summary>
        public event Action<bool> VisibleChanged;

        /// <summary>
        /// Escape backs out of the hub, innermost thing first.
        ///
        /// ⚠️⚠️ IT WAS DEAD ON THIS SCREEN AND ON THIS SCREEN ONLY, WHICH IS THE WORST PLACE FOR
        /// AN INCONSISTENCY. `ConvertedScreen.Update` gives every converted screen an Escape that
        /// goes somewhere, and its own header records what it cost when three screens were left
        /// with `CancelTarget = null`: *"Escape was therefore dead on exactly the screens..."*.
        /// The hub and the sign-in screen are built in code rather than converted from a `.tscn`,
        /// so they inherited none of it. **A player who learns that Escape backs out everywhere
        /// and then meets one screen where it does not has learned that it is unreliable**, which
        /// is worse than it never working.
        ///
        /// ⚠️⚠️ INNERMOST FIRST, AND THE ORDER IS THE WHOLE BEHAVIOUR. The match-detail popup
        /// sits over the MATCHES tab, so Escape closes the popup and leaves the hub open. One
        /// press, one layer. Closing both would throw away the list the player was reading; doing
        /// nothing would trap them in a popup whose only exit is a BACK button they have to find.
        ///
        /// ⚠️ THE SIGN-IN SCREEN OWNS ITS OWN ESCAPE, because at boot it must NOT be dismissable:
        /// there is nothing behind it. See `SignInScreen.Update`.
        ///
        /// ⚠️ AND THE SOUND FOLLOWS THE DECISION, copying `ConvertedScreen.Update` exactly: a
        /// click on a press that then does nothing reads as a press that was swallowed.
        /// </summary>
        private void Update()
        {
            if (_root == null || !_root.activeSelf) return;
            if (_signIn != null && _signIn.IsOpen) return;
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            if (_detail != null && _detail.activeSelf)
            {
                _detail.SetActive(false);
                MenuSfx.Back();
                return;
            }

            Close();
            MenuSfx.Back();
        }

        // -------------------------------------------------------------------
        // § CHROME
        // -------------------------------------------------------------------

        public void Install()
        {
            if (_canvas != null) return;

            _canvas = MenuKit.BuildCanvas(transform, "PlayerHubCanvas");

            // ⚠️⚠️ 500, NOT 85, AND A RENDER IS WHY. At 85 the first screenshot of this
            // screen had the MULTIPLAYER setup screen drawn straight through it: JOIN A GAME, the
            // join-code field and START MATCH all legible over the career rows. **85 was picked
            // against the two overlays this replaced and says nothing about every converted
            // screen in the game**, several of which are authored in `.tscn` files with their own
            // orders that nothing here can see.
            //
            // ⚠️ A FULL-SCREEN TAKEOVER SHOULD SORT ABOVE EVERYTHING BY A MARGIN RATHER THAN BY
            // ONE, so a screen added later cannot land between this and the game. The scrim is
            // 93 per cent and that is a look, not a defence: a canvas above it draws over it
            // whatever its alpha.
            _canvas.sortingOrder = 500;

            _root = new GameObject("HubRoot", typeof(RectTransform));
            _root.transform.SetParent(_canvas.transform, false);
            MenuKit.Stretch((RectTransform)_root.transform);

            // ⚠️ THE SCRIM IS OPAQUE ENOUGH TO KILL THE MENU BEHIND IT. The old panels floated a
            // brown rectangle over a lit street with the PLAY button still readable beside it, so
            // the eye never settled. Everything a player can act on is inside this screen while
            // it is up.
            MenuKit.Backdrop(_root.transform, new Color(0.03f, 0.02f, 0.01f, 0.93f));

            BuildHeader();
            BuildTabBar();

            var listGo = new GameObject("ListArea", typeof(RectTransform));
            listGo.transform.SetParent(_root.transform, false);
            var listRt = (RectTransform)listGo.transform;
            listRt.anchorMin = new Vector2(0.06f, 0.0f);
            listRt.anchorMax = new Vector2(0.94f, 1.0f);
            listRt.offsetMin = new Vector2(0.0f, 96.0f);
            listRt.offsetMax = new Vector2(0.0f, -232.0f);

            _list = UiRows.ScrollList(listGo.transform, "Rows", out _scroll);
            MenuKit.Stretch((RectTransform)_scroll.transform);

            BuildFooter();
            BuildDetail();

            _signIn = gameObject.GetComponent<SignInScreen>();
            if (_signIn == null) _signIn = gameObject.AddComponent<SignInScreen>();
            _signIn.Install();
            _signIn.Closed += OnSignInClosed;

            // ⚠️ THE HUB GETS OUT OF THE WAY WHILE SIGNING IN. See `SignInScreen.Opened`: the
            // right-hand side of that screen is meant to be the game, not a sliced-up copy of
            // this one. `_root` rather than the canvas, so the sign-in screen's own canvas is
            // untouched.
            _signIn.Opened += visible =>
            {
                if (_root != null) _root.SetActive(!visible);
            };

            _root.SetActive(false);

            if (GameServices.Account != null) GameServices.Account.Changed += OnDataChanged;
            if (GameServices.Career != null) GameServices.Career.Changed += OnDataChanged;

            // ⚠️ THE ONE SCREEN IN THE GAME WHOSE CONTENT CHANGES WITHOUT THE PLAYER TOUCHING
            // ANYTHING. A friend comes online, a request arrives, a lobby opens; `SocialStore`
            // raises `Changed` and this redraws. Polling the list from `Update` would be the same
            // per-frame rebuild `Hud` cost a probe an eighth of its frames with.
            if (GameServices.Social != null) GameServices.Social.Changed += OnDataChanged;
        }

        private void OnDestroy()
        {
            if (GameServices.Account != null) GameServices.Account.Changed -= OnDataChanged;
            if (GameServices.Career != null) GameServices.Career.Changed -= OnDataChanged;
            if (GameServices.Social != null) GameServices.Social.Changed -= OnDataChanged;
            if (_signIn != null) _signIn.Closed -= OnSignInClosed;
        }

        /// <summary>
        /// The identity band: who you are, once, at the top, in the biggest type on the screen.
        ///
        /// ⚠️⚠️ IT IS THE ONLY PLACE THE HANDLE APPEARS NOW. The old arrangement printed it on the
        /// account panel AND on the career panel, so two screens each claimed to be where your
        /// name lived and neither was the header of anything. A header that persists across the
        /// tabs is what makes four tabs feel like one screen.
        /// </summary>
        private void BuildHeader()
        {
            _handle = MenuKit.Label(_root.transform, "", 44, UiTheme.Cream,
                new Vector2(0.0f, 1.0f), new Vector2(400.0f, -78.0f), new Vector2(680.0f, 58.0f),
                TextAnchor.MiddleLeft);

            _state = MenuKit.Label(_root.transform, "", MenuKit.MinReadableUnits,
                UiTheme.CreamMuted, new Vector2(0.0f, 1.0f), new Vector2(420.0f, -122.0f),
                new Vector2(720.0f, 28.0f), TextAnchor.MiddleLeft);

            // ⚠️⚠️ THE WHOLE XP BLOCK MOVED LEFT ON 2026-08-30 BECAUSE THE BAR WAS DRAWN UNDER
            // THE CLOSE BUTTON. The track was centred at -300 with a half-width of 220, so it ran
            // to x = -80; CLOSE is centred at -118 with a half-width of 90, so it starts at -208.
            // **128 px of the bar, which is the end a player reads to see how close they are to
            // the next level, was behind a wood button.** It is in 🧑's own screenshot and it is
            // the same class of fault as everything in § 92: two absolute offsets authored
            // separately, each correct on its own, never checked against each other.
            //
            // The block now ends at -238, which clears CLOSE's left edge by 30 px, and the three
            // parts are laid out from that one number: level at the bar's left end, the XP count
            // at its right end, the bar under both.
            const float XpRight = -238.0f;
            const float XpWidth = 440.0f;
            const float XpCentre = XpRight - XpWidth * 0.5f;

            _levelChip = MenuKit.Label(_root.transform, "", 22, UiTheme.Amber,
                new Vector2(1.0f, 1.0f), new Vector2(XpRight - XpWidth + 100.0f, -80.0f),
                new Vector2(200.0f, 32.0f), TextAnchor.MiddleLeft);

            // ⚠️ THE NUMBER IS NEW AND THE BAR NEEDED IT. A bar with no scale says "somewhere
            // between two levels", which is the one thing the player can already see. `FUTURE.md`
            // PHASE 4 asks for the level and the progress to be legible at a glance and a bare
            // 8 px sliver is not; `ProgressionRules.XpPerLevel` is flat, so the denominator is a
            // constant and the fraction is honest without any further arithmetic.
            _xpCount = MenuKit.Label(_root.transform, "", MenuKit.MinReadableUnits,
                UiTheme.CreamMuted, new Vector2(1.0f, 1.0f), new Vector2(XpRight - 120.0f, -80.0f),
                new Vector2(240.0f, 28.0f), TextAnchor.MiddleRight);

            // ⚠️ THE XP BAR IS HERE RATHER THAN ON THE CAREER TAB because it is identity, not a
            // statistic: `FUTURE.md` PHASE 4 puts level and border on the header card, and a bar
            // that only exists on one tab stops being the thing you glance at.
            var track = new GameObject("XpTrack", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(_root.transform, false);
            MenuKit.Place((RectTransform)track.transform, new Vector2(1.0f, 1.0f),
                new Vector2(XpCentre, -114.0f), new Vector2(XpWidth, 10.0f));
            track.GetComponent<Image>().color = UiTheme.WoodDark;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(track.transform, false);
            _xpFill = fillGo.GetComponent<Image>();
            _xpFill.color = UiTheme.Amber;

            var fill = _xpFill.rectTransform;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0.0f, 1.0f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;

            MenuKit.WoodButton(_root.transform, "CLOSE", new Vector2(1.0f, 1.0f),
                new Vector2(-118.0f, -74.0f), new Vector2(180.0f, 52.0f), Close);
        }

        /// <summary>
        /// ⚠️ TABS ACROSS THE TOP, WHICH IS THE REFERENCE'S ARRANGEMENT. Valorant runs
        /// GENERAL / GRAPHICS QUALITY / STATS across the top of its settings and PUBG runs its
        /// sections down a rail; either works, and across the top is the one that survives a
        /// 16:10 window without a second scroll region.
        ///
        /// ⚠️⚠️ FRIENDS IS THE FIFTH AND IT IS A TAB RATHER THAN A GROUP ON `PROFILE`, WHICH IS
        /// A DECISION AND NOT AN OVERSIGHT. `FUTURE.md` § 0.5b's row for Phase 6 asks for *"a
        /// friends rail on the hub"* with **who is online now** as the one thing on it, and a
        /// group buried inside a tab about YOU fails that twice over: it is collapsed by default
        /// like every other group, and PROFILE is the screen about the local player while this is
        /// the only screen in the game about anybody else.
        ///
        /// ⚠️ AND A TAB IS NOT A SECOND DOOR. `CLAUDE.md` § 6.3's rule is about the MENU growing
        /// a button per feature (§ 92's six-button panel); the hub still has exactly one entrance
        /// and everything inside it is one press from the others. `docs/TODO.md` § 102.
        ///
        /// ⚠️ THE SPACING IS DERIVED RATHER THAN TYPED. Five tabs at the four-tab offsets would
        /// have run the last one off the panel, and the next tab added would do it again.
        /// </summary>
        private void BuildTabBar()
        {
            var order = new[]
            {
                (Tab.Profile, "PROFILE"),
                (Tab.Friends, "FRIENDS"),
                (Tab.Career, "CAREER"),
                (Tab.Matches, "MATCHES"),
                (Tab.Account, "ACCOUNT"),
            };

            const float Pitch = 200.0f;
            float first = -Pitch * (order.Length - 1) * 0.5f;

            for (int i = 0; i < order.Length; i++)
                AddTab(order[i].Item1, order[i].Item2, first + Pitch * i);
        }

        private void AddTab(Tab tab, string label, float x)
        {
            _tabs[tab] = MenuKit.WoodButton(_root.transform, label, new Vector2(0.5f, 1.0f),
                new Vector2(x, -182.0f), new Vector2(192.0f, 54.0f), () => Show(tab));
        }

        private void BuildFooter()
        {
            // ⚠️ ALIGNED WITH THE LIST, NOT WITH THE SCREEN. `ListArea` starts at 6 per cent of
            // the width, which is x = 115 at 1920, and this note started at x = 60, so the one
            // sentence explaining the whole tab hung 55 px outside the column every row above it
            // shares. It reads as something that fell off rather than as the footer of anything.
            _footerNote = MenuKit.Label(_root.transform, "", MenuKit.MinReadableUnits,
                UiTheme.CreamMuted, new Vector2(0.06f, 0.0f), new Vector2(420.0f, 50.0f),
                new Vector2(840.0f, 28.0f), TextAnchor.MiddleLeft);

            // ⚠️⚠️ ONE ACTION, BOTTOM RIGHT, AND IT CHANGES WITH THE TAB. PUBG parks a single
            // persistent action there (UPLOAD TO CLOUD) and that is the whole idea: a screen with
            // one button has an obvious thing to do, and a screen with six has none. Everything
            // else on this screen is a row.
            _footerAction = MenuKit.WoodButton(_root.transform, "SAVE", new Vector2(1.0f, 0.0f),
                new Vector2(-140.0f, 50.0f), new Vector2(230.0f, 56.0f), FooterPressed,
                "WoodPrimaryButton");

            _footerLabel = _footerAction.GetComponentInChildren<Text>();
        }

        /// <summary>
        /// ⚠️ ONE MONOSPACED BLOCK RATHER THAN ROWS, AND IT IS THE RIGHT CHOICE HERE. Everywhere
        /// else on this screen a row is a label and one value, which is what `UiRows` is for; a
        /// match scoreboard is a TABLE, four rows by nine columns, and every column has to line
        /// up with the one above it or the numbers cannot be compared. Aligned columns in a
        /// padded string is how the old panel did it and it was the one part of it that worked.
        /// </summary>
        private void BuildDetail()
        {
            _detail = new GameObject("MatchDetail", typeof(RectTransform), typeof(Image));
            _detail.transform.SetParent(_root.transform, false);
            MenuKit.Place((RectTransform)_detail.transform, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1180.0f, 620.0f));

            _detail.GetComponent<Image>().color = UiTheme.WoodDeep;

            var skin = _detail.AddComponent<GodotPanel>();
            skin.Variation = "WoodPanel";
            skin.ApplyContentMargins = false;
            skin.Apply();

            _detailTitle = MenuKit.Label(_detail.transform, "", 26, UiTheme.Amber,
                new Vector2(0.5f, 1.0f), new Vector2(0.0f, -50.0f), new Vector2(1060.0f, 40.0f));

            _detailBody = MenuKit.Label(_detail.transform, "", MenuKit.MinReadableUnits,
                UiTheme.Cream,
                new Vector2(0.5f, 1.0f), new Vector2(0.0f, -300.0f), new Vector2(1060.0f, 440.0f),
                TextAnchor.UpperLeft);

            MenuKit.WoodButton(_detail.transform, "BACK", new Vector2(0.5f, 0.0f),
                new Vector2(0.0f, 52.0f), new Vector2(220.0f, 52.0f),
                () => _detail.SetActive(false));

            _detail.SetActive(false);
        }

        private void OpenDetail(MatchRecord record)
        {
            if (record?.Players == null) return;

            _detailTitle.text = $"{MenuKit.ModeLabel(record.Mode)}   ·   " +
                                $"{record.MapId.ToUpperInvariant()}   ·   {Short(record.PlayedUtc)}";

            var lines = new List<string>
            {
                $"{"PLACE",-7}{"PLAYER",-20}{"PTS",6}{"KD",6}{"THR",6}{"RET",6}{"TAG",6}{"SAB",6}{"DEF s",8}",
                "",
            };

            var order = new List<PlayerMatchStats>(record.Players);
            order.Sort((a, b) => (a?.Placement ?? 9).CompareTo(b?.Placement ?? 9));

            foreach (var p in order)
            {
                if (p == null) continue;
                string who = string.IsNullOrEmpty(p.Handle) ? $"P{p.Slot + 1}" : p.Handle;
                if (who.Length > 18) who = who.Substring(0, 18);

                lines.Add($"{Ordinal(p.Placement),-7}{who,-20}{p.Score,6}{p.Knockdowns,6}" +
                          $"{p.Throws,6}{p.Retrievals,6}{p.Tags,6}{p.Sabotages,6}" +
                          $"{MatchRecordRules.PassiveDefenceSeconds(p),8:0}");
            }

            // ⚠️ WHO DEFENDED EACH ROUND IS STORED ON THE RECORD RATHER THAN RE-DERIVED, per
            // `MatchRecord.DefenderByRound`, so a schedule change cannot rewrite history.
            if (record.DefenderByRound != null && record.DefenderByRound.Length > 0)
            {
                lines.Add("");
                var taya = new List<string>();
                for (int i = 0; i < record.DefenderByRound.Length; i++)
                    taya.Add($"R{i + 1} P{record.DefenderByRound[i] + 1}");
                lines.Add("TAYA EACH ROUND   " + string.Join("   ", taya));
            }

            _detailBody.text = string.Join("\n", lines);
            _detail.SetActive(true);
        }

        // -------------------------------------------------------------------
        // § OPENING AND SWITCHING
        // -------------------------------------------------------------------

        /// <summary>
        /// Opens the hub. ⚠️ `onAccount` is the upgrade offer landing on the tab that answers
        /// it, per `PlayerNameplate.Press`; every other route opens on PROFILE, which is the tab
        /// somebody who pressed their own name was most likely looking for.
        /// </summary>
        public void Open(bool onAccount = false)
        {
            _root.SetActive(true);
            VisibleChanged?.Invoke(true);
            Show(onAccount ? Tab.Account : Tab.Profile);
        }

        private void Close()
        {
            _deleteArmed = false;
            _notice = "";
            if (_detail != null) _detail.SetActive(false);
            _root.SetActive(false);
            VisibleChanged?.Invoke(false);
        }

        private void OnSignInClosed()
        {
            _notice = "";
            Show(Tab.Account);
        }

        private void OnDataChanged()
        {
            if (_root != null && _root.activeSelf) Show(_tab);
        }

        /// <summary>
        /// ⚠️ THE LIST IS REBUILT RATHER THAN SHOWN AND HIDDEN. Four tabs of rows kept alive at
        /// once is four layouts recomputing on every resize, and `Hud`'s per-frame string rebuild
        /// already cost this project an eighth of a probe's frames. A tab switch is a rare event;
        /// paying for it there is free and paying for it every frame is not.
        /// </summary>
        private void Show(Tab tab)
        {
            bool arriving = _tab != tab;

            _tab = tab;
            _deleteArmed = _deleteArmed && tab == Tab.Account;

            // ⚠️⚠️ FRIENDS ASKS THE SERVICE THE MOMENT IT IS OPENED, AND WITHOUT THIS THE TAB
            // ONLY EVER SHOWS THE CACHE. Every other tab in this hub draws state this machine
            // already owns — a profile, a career, a match history — so opening one needs no call.
            // **This is the only screen in the game whose whole content is a fact about other
            // people**, and a friends list that says OFFLINE because nobody asked is worse than
            // one that says nothing.
            //
            // ⚠️ ON ARRIVAL, NOT ON EVERY `Show`. `SocialStore.Changed` calls `Show(_tab)` to
            // redraw, so refreshing unconditionally would be a call per answer for ever.
            // `SocialStore.Refresh` also refuses a second request while one is in flight, which
            // is the belt to this braces.
            if (arriving && tab == Tab.Friends) GameServices.Social?.Refresh();

            foreach (var pair in _tabs) Highlight(pair.Value, pair.Key == tab);

            // ⚠️⚠️ DETACHED BEFORE IT IS DESTROYED, AND THAT ORDER IS LOAD BEARING.
            // `Destroy` is deferred to the end of the frame, so the outgoing tab's rows are still
            // CHILDREN of the list while the incoming tab is built. Two things go wrong if they
            // are only destroyed: the layout group stacks both tabs for a frame, and, worse,
            // `UiRows.Row` counts existing `Row_` children to decide which rows get the zebra
            // band. **The new tab's striping would start on whichever foot the previous tab's row
            // count left it on**, so the same tab would band differently depending on where you
            // came from. `SetParent(null)` takes them out of the count immediately.
            for (int i = _list.childCount - 1; i >= 0; i--)
            {
                var child = _list.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }

            ForgetFields();

            RefreshHeader();

            switch (tab)
            {
                case Tab.Profile: BuildProfileTab(); break;
                case Tab.Friends: BuildFriendsTab(); break;
                case Tab.Career: BuildCareerTab(); break;
                case Tab.Matches: BuildMatchesTab(); break;
                case Tab.Account: BuildAccountTab(); break;
            }

            if (_scroll != null) _scroll.verticalNormalizedPosition = 1.0f;
        }

        private static void Highlight(Button button, bool on)
        {
            var skin = button?.GetComponent<GodotButton>();
            if (skin == null) return;

            skin.Variation = on ? "WoodAmberButton" : "WoodButton";
            skin.Apply();
            skin.Refresh();
        }

        /// <summary>
        /// Draws a group heading and answers whether its rows should be built at all.
        ///
        /// ⚠️ A CLOSED GROUP IS NOT BUILT. `Show` rebuilds the whole tab on every change, so
        /// "closed" costs nothing to honour and the scroll height stays honest about what is
        /// actually on screen. See `UiRows.Section`.
        /// </summary>
        private bool Group(string title, string subtitle, bool openByDefault = true)
        {
            string key = _tab + "/" + title;
            if (!_groups.TryGetValue(key, out bool open)) open = openByDefault;

            bool now = open;
            UiRows.Section(_list, title, subtitle, now, () =>
            {
                _groups[key] = !now;
                Show(_tab);
            });

            return now;
        }

        private void RefreshHeader()
        {
            var account = GameServices.Account;
            var profile = GameServices.Career?.Profile;

            _handle.text = account != null ? account.LobbyName : "PLAYER";

            _state.text = account == null ? ""
                : account.IsGuest ? "GUEST  ·  nothing is being saved to an account"
                : account.HasPassword ? $"SIGNED IN AS {account.Username.ToUpperInvariant()}"
                : "PLAYING ON THIS MACHINE ONLY  ·  no username yet";

            // ⚠️ THE LEVEL IS HIDDEN UNTIL IT HAS BEEN EARNED, exactly as `ProfileOverlay` argues
            // about the rank badge: every account is level 1 the moment it exists, and printing
            // it before a single match has paid teaches the player the number is decoration.
            int xp = profile?.Xp ?? 0;
            if (xp > 0)
            {
                int level = ProgressionRules.LevelForXp(xp);
                _levelChip.text = $"LEVEL {level}";
                int into = ProgressionRules.XpIntoLevel(xp);
                _xpCount.text = $"{into} / {ProgressionRules.XpPerLevel} XP";
                _xpFill.rectTransform.anchorMax = new Vector2(
                    Mathf.Clamp01(into / (float)ProgressionRules.XpPerLevel), 1.0f);
            }
            else
            {
                _levelChip.text = "";
                _xpCount.text = "";
                _xpFill.rectTransform.anchorMax = new Vector2(0.0f, 1.0f);
            }
        }

        // -------------------------------------------------------------------
        // § PROFILE
        // -------------------------------------------------------------------

        private void BuildProfileTab()
        {
            var account = GameServices.Account;

            if (Group("Identity",
                      "The name and tag every other player sees in a lobby and on the scoreboard."))
            {
                _displayName = UiRows.FieldRow(_list, "Display name", "Maria Clara",
                    AccountRules.DisplayNameMax,
                    $"Up to {AccountRules.DisplayNameMax} characters. It does not have to be unique.");
                _displayName.text = account?.DisplayName ?? "";

                // ⚠️ THE TAG IS SHOWN AND CANNOT BE EDITED, which is the fact the old panel
                // never stated anywhere. `AccountRules` derives it from the player id (FNV-1a,
                // and again server-side), so a player who types over it would be typing over the
                // one thing stopping somebody impersonating them. A hint costs one line.
                UiRows.ValueRow(_list, "Your tag", account != null ? account.LobbyName : "",
                    "Given by the game and fixed. It tells two players with one name apart.",
                    UiTheme.Amber);
            }

            // ⚠️⚠️ CLOSED BY DEFAULT, AND THIS IS THE CLEAREST CASE FOR IT ON ANY TAB. Three
            // optional fields nobody fills in on their first visit were three of the six things
            // the old panel asked at once. Closed, this tab is one field and one fact.
            if (Group("Optional details",
                      "None of this is required and all of it is public on your career page.",
                      false))
            {
                _country = UiRows.FieldRow(_list, "Country", "PH", AccountRules.CountryCodeLength,
                    "Two letters. Shown next to your name at a tournament.");
                _country.text = account?.Country ?? "";

                _pronouns = UiRows.FieldRow(_list, "Pronouns", "they/them",
                                            AccountRules.PronounsMax);
                _pronouns.text = account?.Pronouns ?? "";

                _bio = UiRows.FieldRow(_list, "Bio", "One line about you", AccountRules.BioMax,
                    $"Up to {AccountRules.BioMax} characters.");
                _bio.text = account?.Bio ?? "";
            }

            BuildBannerGroup();

            UiRows.Gap(_list, 40.0f);

            SetFooter("SAVE", _notice);
        }

        /// <summary>
        /// The banner: what a player wears beside their name, and the first surface Phase 4's
        /// rewards have ever had.
        ///
        /// ⚠️⚠️ PHASE 4 EARNS TITLES, BADGES, PALETTES AND BORDERS AND NOTHING WORE ANY OF THEM,
        /// which `docs/TODO.md` § 91.8 records and § 98 is the entry for. **A progression track
        /// whose rewards are invisible is a number going up.** `FUTURE.md` § 0.5b's row for
        /// Phase 5 says to wire what already exists before authoring anything new, or the first
        /// thing that phase ships is a second unworn set.
        ///
        /// ⚠️⚠️ IT OPENS ONLY ONCE THERE IS SOMETHING TO WEAR, WHICH IS THE ANSWER TO
        /// § 0.5b QUESTION 3. A fresh account has earned nothing, and four dropdowns each
        /// reading "None" is the fifteen-rows-of-zeroes fault (§ 92.1 fault 4) in a new costume:
        /// it teaches a new player that the feature is empty rather than that it is coming.
        /// Closed with one sentence, it says where the things come from; open with choices in it,
        /// it is worth looking at. **The group appears when it becomes relevant.**
        ///
        /// ⚠️ EVERY DROPDOWN OFFERS "None" FIRST AND IT IS NEVER A MISSING VALUE. Wearing
        /// nothing is a legal banner and the state every account starts in;
        /// `BannerRules.Normalise` answers an empty selection rather than null for the same
        /// reason.
        /// </summary>
        private void BuildBannerGroup()
        {
            var profile = GameServices.Career?.Profile;
            var earned = BannerRules.Earned(profile);

            if (earned.Count == 0)
            {
                // ⚠️ THE EMPTY STATE IS A SENTENCE, NOT A DISABLED CONTROL. It says where the
                // rewards come from, which is the one thing a player who has none needs to know.
                if (Group("Banner", "What people see beside your name.", false))
                {
                    UiRows.ValueRow(_list, "Nothing earned yet", "",
                        "Titles, badges, borders and palettes come from your account level and " +
                        "from hero mastery. Play a match and they start arriving.");
                }

                return;
            }

            if (!Group("Banner", "What people see beside your name.", false)) return;

            var settings = Settings.SettingsStore.Current;

            BannerSlot("Title", RewardKind.Title, earned, settings.BannerTitleId,
                id => settings.BannerTitleId = id);
            BannerSlot("Badge", RewardKind.Badge, earned, settings.BannerBadgeId,
                id => settings.BannerBadgeId = id);
            BannerSlot("Border", RewardKind.Border, earned, settings.BannerBorderId,
                id => settings.BannerBorderId = id);
            BannerSlot("Palette", RewardKind.Palette, earned, settings.BannerPaletteId,
                id => settings.BannerPaletteId = id);
        }

        /// <summary>
        /// One dropdown of everything of this kind the player has earned, plus "None".
        ///
        /// ⚠️⚠️ THE LIST IS BUILT FROM `BannerRules.Earned` AND NEVER FROM THE WHOLE TABLE, so a
        /// player cannot select something they have not earned in the first place. That is
        /// belt-and-braces rather than the actual guard: `BannerRules.Normalise` is the guard,
        /// it runs on the receiving side, and it is the only thing that can be trusted about a
        /// banner that arrived from somebody else's machine.
        ///
        /// ⚠️ THE DROPDOWN SHOWS `Label` AND STORES `Id`. A reward's label is prose that may be
        /// reworded; its id crosses the wire and never changes. `FUTURE.md` PHASE 5's string-id
        /// rule is about exactly this pair being kept apart.
        /// </summary>
        private void BannerSlot(string label, RewardKind kind, List<Reward> earned,
                                string current, Action<string> choose)
        {
            var options = new List<string> { "NONE" };
            var ids = new List<string> { "" };

            foreach (var reward in earned)
            {
                if (reward == null || reward.Kind != kind) continue;
                if (ids.Contains(reward.Id)) continue;

                ids.Add(reward.Id);
                options.Add((reward.Label ?? reward.Id).ToUpperInvariant());
            }

            // ⚠️ A KIND WITH NOTHING EARNED IN IT IS NOT DRAWN. A dropdown whose only entry is
            // NONE is a control that cannot do anything, and four of them is the empty state this
            // group already refuses to show.
            if (ids.Count <= 1) return;

            int index = Mathf.Max(0, ids.IndexOf(current ?? ""));

            UiRows.DropdownRow(_list, label, options.ToArray(), index, picked =>
            {
                choose(picked >= 0 && picked < ids.Count ? ids[picked] : "");
                Settings.SettingsStore.Save();
                Show(Tab.Profile);
            });
        }

        /// <summary>
        /// ⚠️⚠️ THE FIELD HANDLES ARE CLEARED WHENEVER A TAB IS REBUILT, because the objects
        /// behind them are destroyed and a stale `InputField` reference reads as "the group is
        /// open and empty". `SaveProfile` branches on null, so this is what makes a closed group
        /// safe rather than destructive.
        /// </summary>
        private void ForgetFields()
        {
            _displayName = null;
            _country = null;
            _pronouns = null;
            _bio = null;
        }

        private async void SaveProfile()
        {
            try
            {
                SetFooter("SAVE", "Saving...");

                // ⚠️⚠️ A CLOSED GROUP HAS NO FIELDS, SO THE CURRENT ACCOUNT VALUE IS SENT FOR
                // ANYTHING NOT ON SCREEN. `SetProfileAsync` takes all four at once, so reading a
                // destroyed `InputField` would throw, and defaulting one to "" would silently
                // WIPE a bio the player had written just because they had the group shut.
                var a = GameServices.Account;
                string name = _displayName != null ? _displayName.text : a.DisplayName;
                string bio = _bio != null ? _bio.text : a.Bio;
                string country = _country != null ? _country.text : a.Country;
                string pronouns = _pronouns != null ? _pronouns.text : a.Pronouns;

                await a.SetProfileAsync(name, bio, country, pronouns);
                _notice = "Saved.";
                Show(Tab.Profile);
            }
            catch (Exception e)
            {
                _notice = e.Message;
                SetFooter("SAVE", _notice);
            }
        }

        // -------------------------------------------------------------------
        // § CAREER
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE HEADLINE FOUR COME FIRST AND EVERYTHING ELSE IS DETAIL UNDER A HEADING. The
        /// page this replaces opened with fifteen rows of the same size, in this order: matches,
        /// placements, knockdowns, knockdowns per throw, retrievals, under pressure, tags, tags
        /// per round, passive defence, sabotages, shove rate, lunge rate, last stand, clutch,
        /// first throw, distance. Nothing was bigger than anything else, so the eye had nowhere
        /// to land and 🧑 read the whole thing as one block.
        ///
        /// ⚠️⚠️ AND A RATE THAT CANNOT BE REPORTED IS NOW ABSENT RATHER THAN PRINTED AS `0/0`.
        /// `FUTURE.md` § 2.2 says not to show a stat you will not defend, and the old page obeyed
        /// half of it: it withheld the NUMBER and still drew the row, so a new account saw eight
        /// rows of `0/0 (needs 10 throws)`. Withholding a row is what that rule meant.
        /// </summary>
        private void BuildCareerTab()
        {
            var profile = GameServices.Career?.Profile;
            if (profile == null) { EmptyCareer(); return; }

            var totals = ProfileRules.ModeFor(profile, _mode.ToString()).Totals;
            if (totals.Matches == 0) { EmptyCareer(); return; }

            var rank = profile.Rank;
            if (rank != null && rank.MatchesThisSeason > 0 && Group("Competitive Rank", "Your standing on the seasonal ranked ladder."))
            {
                var tier = RatingRules.TierFor(rank.Rating);
                string tierName = RatingRules.TierName(tier);
                bool placing = rank.Deviation > RatingRules.SettledDeviation;

                UiRows.ValueRow(_list, "Current Tier", placing ? $"{tierName} (Placing)" : tierName,
                    placing ? "Play more ranked matches to settle your rank rating." : "Your official competitive ladder tier.",
                    UiTheme.Amber);

                UiRows.ValueRow(_list, "Season Matches", rank.MatchesThisSeason.ToString());
            }

            if (Group("Overview",
                      "Classic and Hero Strike are separate games and their numbers never merge."))
            {
                // ⚠️⚠️ A DROPDOWN, NOT TWO BUTTONS. The pair this replaces is what 🧑
                // photographed overlapping itself. See `UiRows.DropdownRow`.
                UiRows.DropdownRow(_list, "Mode", new[] { "CLASSIC", "HERO STRIKE" },
                    _mode == GameMode.HeroStrike ? 1 : 0,
                    v =>
                    {
                        _mode = v == 1 ? GameMode.HeroStrike : GameMode.Classic;
                        Show(Tab.Career);
                    });

                UiRows.ValueRow(_list, "Matches played", totals.Matches.ToString());
                UiRows.ValueRow(_list, "Matches won", totals.Wins.ToString());

                if (MatchRecordRules.IsReportable(totals.Matches))
                    UiRows.ValueRow(_list, "Win rate",
                        $"{ProfileRules.WinRate(totals) * 100.0f:0}%", "", UiTheme.Amber);

                UiRows.ValueRow(_list, "Hours played", $"{ProfileRules.HoursPlayed(totals):0.0}");

                // ⚠️ FOUR CELLS, NOT ONE STRING. This row used to be
                // `1st 3   2nd 3   3rd 3   4th 3` as a single right-aligned value, so four
                // separate facts arrived as one long word whose internal spacing was whatever
                // the font did with three spaces. See `UiRows.DistributionRow`.
                UiRows.DistributionRow(_list, "Finishes",
                    new[] { "1ST", "2ND", "3RD", "4TH" },
                    new[]
                    {
                        totals.Placements[0].ToString(),
                        totals.Placements[1].ToString(),
                        totals.Placements[2].ToString(),
                        totals.Placements[3].ToString(),
                    },
                    "A four-player game has four outcomes, so all four are shown.");
            }

            // ⚠️⚠️ THE SIX DETAIL GROUPS ALL START CLOSED. Open, they are thirty rows, which is
            // twice what the old page showed at once and is the exact complaint this rebuild
            // answers. Closed, the career tab is six headings and one sentence each, and the
            // player opens the one they came for.
            if (Group("Attack", "What you did with the tsinelas in your hand.", false))
            {
                UiRows.ValueRow(_list, "Throws", totals.Throws.ToString());
                UiRows.ValueRow(_list, "Knockdowns", totals.Knockdowns.ToString());
                Rate(_list, "Knockdowns per throw", ProfileRules.KnockdownsPerThrow(totals),
                     totals.Throws);
            }

            if (Group("Retrieval",
                      "The run back in for your slipper, which is the game this is built around.",
                      false))
            {
                UiRows.ValueRow(_list, "Retrievals", totals.Retrievals.ToString());
                UiRows.ValueRow(_list, "Under pressure", totals.RetrievalsUnderPressure.ToString(),
                    $"Made within {MatchRecordRules.PressureRadius:0.0} m of the taya, their reach.");
                if (totals.MatchesWithAThrow > 0)
                    UiRows.ValueRow(_list, "Average first throw",
                        $"{ProfileRules.AverageTimeToFirstThrow(totals):0.0} s");
            }

            if (Group("Defence", "The rounds you spent as the taya.", false))
            {
                UiRows.ValueRow(_list, "Rounds defended", totals.RoundsDefended.ToString());
                UiRows.ValueRow(_list, "Tags", totals.Tags.ToString());
                Rate(_list, "Tags per round defended", ProfileRules.TagsPerRoundDefended(totals),
                     totals.RoundsDefended, false);
                UiRows.ValueRow(_list, "Passive defence",
                    $"{ProfileRules.PassiveDefenceSeconds(totals):0} s");
                UiRows.ValueRow(_list, "Sabotages", totals.Sabotages.ToString());
            }

            if (Group("Melee", "Shove and lunge, the close game.", false))
            {
                Rate(_list, "Shove hit rate", ProfileRules.ShoveHitRate(totals),
                     totals.ShoveAttempts);
                Rate(_list, "Lunge hit rate", ProfileRules.LungeHitRate(totals),
                     totals.LungeAttempts);
            }

            if (Group("Standout", "The rare ones.", false))
            {
                UiRows.ValueRow(_list, "Longest last stand", $"{totals.LongestLastAttacker:0.0} s");
                UiRows.ValueRow(_list, "Longest win streak", totals.LongestWinStreak.ToString());
                Rate(_list, "Clutch rate", ProfileRules.ClutchRate(totals), totals.ComebackChances);
            }

            BuildMasteryRows(profile);
            BuildAchievementsRows(profile);

            UiRows.Gap(_list, 40.0f);
            SetFooter("", "Rates appear once there are enough attempts to mean anything.");
        }

        /// <summary>A rate row, or nothing at all when the sample cannot carry it.</summary>
        private static void Rate(RectTransform list, string label, float value, float sample,
                                 bool percent = true)
        {
            if (!MatchRecordRules.IsReportable(sample)) return;

            UiRows.ValueRow(list, label,
                percent ? $"{value * 100.0f:0}%" : $"{value:0.00}");
        }

        /// <summary>
        /// ⚠️ THE SIX HEROES ONLY, and each row carries its level and how far to the next one.
        /// `FUTURE.md` PHASE 4 narrowed the mastery paths from eighteen characters to six; the
        /// other twelve keep a played count and appear in the same list without a level, which is
        /// what makes the difference between the two groups visible rather than confusing.
        /// </summary>
        private void BuildMasteryRows(PlayerProfile profile)
        {
            if (!Group("Hero mastery",
                       "Playing a hero levels that hero. Only the six heroes have a path.",
                       false))
                return;

            foreach (var hero in Roster.HeroPeople)
            {
                int xp = 0;
                if (profile.Mastery != null)
                    foreach (var m in profile.Mastery)
                        if (m != null && m.Id == hero.Id) xp = m.Xp;

                int games = 0;
                foreach (var pick in profile.Characters)
                    if (pick != null && pick.Id == hero.Id) games = pick.Games;

                int level = ProgressionRules.MasteryLevelForXp(xp);
                int remaining = ProgressionRules.MasteryXpPerLevel
                                - (xp % ProgressionRules.MasteryXpPerLevel);

                UiRows.ValueRow(_list, hero.Name,
                    xp > 0 ? $"MASTERY {level}" : "not played",
                    xp > 0 ? $"{games} games  ·  {remaining} XP to mastery {level + 1}" : "",
                    xp > 0 ? UiTheme.Amber : UiTheme.CreamMuted);
            }
        }

        private void BuildAchievementsRows(PlayerProfile profile)
        {
            if (!Group("Achievements",
                       "Milestones unlocked across street matches, modes, and ranks.",
                       false))
                return;

            foreach (var tier in new[] { AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Gold })
            {
                var list = AchievementRules.Tier(tier);
                foreach (var ach in list)
                {
                    int progress = AchievementRules.ProgressFor(ach, profile);
                    bool unlocked = progress >= ach.TargetCount;

                    string status = unlocked ? $"COMPLETED  ·  {ach.RewardLabel}" : $"{progress} / {ach.TargetCount}  ·  {ach.RewardLabel}";
                    Color color = unlocked ? (tier == AchievementTier.Gold ? UiTheme.Amber : UiTheme.Highlight) : UiTheme.CreamMuted;

                    UiRows.ValueRow(_list, $"[{tier.ToString().ToUpperInvariant()}] {ach.Title}",
                        unlocked ? "UNLOCKED" : $"{progress}/{ach.TargetCount}",
                        $"{ach.Description}  ({status})",
                        color);
                }
            }
        }

        /// <summary>
        /// ⚠️⚠️ ONE CARD, ONE SENTENCE, ONE ROUTE OUT. The old page answered an empty career with
        /// three different sentences in three places ("No matches recorded yet", "No matches on
        /// this account yet", "No matches played yet. Finish one and it lands here.") and then
        /// drew fifteen rows of zeroes underneath them. An empty state is an invitation.
        /// </summary>
        private void EmptyCareer()
        {
            UiRows.Section(_list, "No matches yet",
                "Finish a match and everything you did in it lands here.");
            UiRows.Gap(_list, 24.0f);
            UiRows.ButtonRow(_list, "Ready when you are", "PLAY", () =>
            {
                Close();
                SceneFlow.Networked = true;
                SceneFlow.Go(SceneFlow.MatchSetup);
            }, "", "WoodPrimaryButton");

            SetFooter("", "");
        }

        // -------------------------------------------------------------------
        // § FRIENDS. `docs/TODO.md` § 102.
        // -------------------------------------------------------------------

        /// <summary>
        /// Who is online now, who wants to be your friend, and who you have shut out.
        ///
        /// ⚠️⚠️ THE ORDER OF THE THREE GROUPS IS THE WHOLE DESIGN. `FUTURE.md` § 0.5b: the one
        /// thing on a friends surface is **who is online now**, and the four ordering tools are
        /// position first. So requests come first and only when there ARE any — they are the one
        /// thing on this screen waiting for an answer from you — then the list, sorted by
        /// `SocialRules.Sorted` so joinable and online rise on their own, then blocked at the
        /// bottom, closed.
        ///
        /// ⚠️⚠️ AND ALL THREE EMPTY STATES ARE DESIGNED, WHICH IS § 0.5b QUESTION 3 AND WHICH
        /// THAT SECTION SINGLES THIS PHASE OUT FOR: *"a friends list is a live list, so it has
        /// three empty states (no friends, none online, service down) and § 0.5b question 3 says
        /// all three get designed."* They say different things: **no friends** points at the
        /// end-of-match screen, **none online** is not an error and does not read as one, and
        /// **not signed in** is the guest's state and the only one with an action attached.
        /// </summary>
        private void BuildFriendsTab()
        {
            var social = GameServices.Social;
            var list = social?.List;
            var now = DateTime.UtcNow;

            // ⚠️⚠️ THE GUEST'S STATE COMES FIRST AND IT IS NOT AN ERROR. `docs/TODO.md` § 97:
            // CONTINUE AS GUEST is one press and must keep working with the cable out, so a guest
            // reaching this tab is an expected player rather than a broken one. **It says what to
            // do rather than what is wrong.**
            if (!SocialRules.IsAddressable(GameServices.Account?.PlayerId))
            {
                UiRows.Section(_list, "Friends", "Who is online, and who you can join.");
                UiRows.ValueRow(_list, "Not signed in", "",
                    "Friends are attached to an account. Make one on the ACCOUNT tab and this " +
                    "list follows you to any machine you sign in on.");

                UiRows.Gap(_list, 40.0f);
                SetFooter("", "");
                return;
            }

            BuildRequestRows(social, list);
            BuildFriendRows(social, list, now);
            BuildBlockedRows(social, list);

            UiRows.Gap(_list, 40.0f);

            // ⚠️ THE FOOTER ACTION IS A REFRESH RATHER THAN A SAVE, BECAUSE NOTHING ON THIS TAB IS
            // EDITED LOCALLY. Every press here is a call the endpoint answers with the whole list,
            // so there is nothing to commit; what a player wants from this screen is "is that
            // still true", which is one call.
            SetFooter("REFRESH", "Presence updates about once a minute while the game is open.");
        }

        private void BuildRequestRows(Net.SocialStore social, SocialList list)
        {
            int pending = list?.Incoming?.Count ?? 0;
            if (pending == 0) return;

            // ⚠️ OPEN AND FIRST, WHICH NO OTHER GROUP IN THIS HUB IS. Every other group is a
            // thing you went looking for; this is the only one waiting on an answer from you, and
            // a collapsed group is a group nobody opens.
            UiRows.Section(_list, "Friend requests",
                           pending == 1 ? "One person wants to play with you."
                                        : $"{pending} people want to play with you.");

            foreach (var row in list.Incoming)
            {
                string who = string.IsNullOrEmpty(row.Handle) ? Shorten(row.PlayerId) : row.Handle;
                string id = row.PlayerId;

                // ⚠️⚠️ ACCEPT IS THE BUTTON AND DECLINE IS ITS OWN ROW, WHICH IS § 0.5b QUESTION
                // 4 ANSWERED. `UiRows.ButtonRow`'s own note is blunt: never more than one button
                // in a row, because a row with three is the six-button panel again. The safe
                // action leads and the quiet one sits under it.
                UiRows.ButtonRow(_list, who, "ACCEPT", () =>
                {
                    social?.Accept(id);
                    MenuSfx.Click();
                }, "Adds them to your list. They see you as online.", "WoodPrimaryButton");

                UiRows.ButtonRow(_list, "", "DECLINE", () =>
                {
                    social?.Decline(id);
                    MenuSfx.Back();
                }, "Removes the request. They are not told.");
            }
        }

        private void BuildFriendRows(Net.SocialStore social, SocialList list, DateTime now)
        {
            var friends = SocialRules.Sorted(list?.Friends, now);

            if (friends.Count == 0)
            {
                UiRows.Section(_list, "Friends", "Who is online, and who you can join.");

                // ⚠️ THE EMPTY STATE POINTS AT WHERE FRIENDS ACTUALLY COME FROM. `FUTURE.md` § 6
                // calls the end-of-match screen *"the highest-converting social prompt any game
                // of this shape has"*. A player with an empty list needs to be told where the
                // button is, not that the list is empty — they can see that.
                UiRows.ValueRow(_list, "Nobody yet", "",
                    "After a match, the scoreboard offers to add everybody you just played " +
                    "with. That is the quickest way to fill this in.");
                return;
            }

            int online = 0;
            foreach (var friend in friends)
                if (SocialRules.EffectivePresence(friend, now) != PresenceState.Offline) online++;

            // ⚠️ THE SUBTITLE IS THE ONE THING ON THE SCREEN, AS A SENTENCE, readable before any
            // row is. **None online is not an error and does not say it is**: it is Tuesday
            // morning.
            UiRows.Section(_list, "Friends",
                online == 0 ? $"None of your {friends.Count} friends are online right now."
                            : $"{online} of {friends.Count} online.");

            foreach (var friend in friends)
            {
                var state = SocialRules.EffectivePresence(friend, now);
                string who = string.IsNullOrEmpty(friend.Handle) ? Shorten(friend.PlayerId)
                                                                 : friend.Handle;
                string id = friend.PlayerId;
                string code = friend.JoinCode;

                if (SocialRules.IsJoinable(friend, now))
                {
                    // ⚠️⚠️ JOIN IS OFFERED ONLY WHEN IT CAN DO SOMETHING, and
                    // `SocialRules.IsJoinable` decides. A JOIN beside somebody whose lobby closed
                    // an hour ago sends a player to a room that is not there, which reads as the
                    // game being broken rather than as the friend having left. `CLAUDE.md` § 6.3.
                    UiRows.ButtonRow(_list, who, "JOIN", () => JoinFriend(code),
                        SocialRules.PresenceLabel(state) + "  \u00b7  in a game you can join",
                        "WoodPrimaryButton");
                }
                else
                {
                    UiRows.ValueRow(_list, who, SocialRules.PresenceLabel(state), "",
                        state == PresenceState.Offline ? UiTheme.CreamMuted : UiTheme.Amber);
                }

                UiRows.ButtonRow(_list, "", "REMOVE", () =>
                {
                    social?.Remove(id);
                    MenuSfx.Back();
                }, "Takes them off both lists. Neither of you is told.");
            }
        }

        private void BuildBlockedRows(Net.SocialStore social, SocialList list)
        {
            int blocked = list?.Blocked?.Count ?? 0;
            if (blocked == 0) return;

            // ⚠️⚠️ CLOSED BY DEFAULT AND LAST, WHICH IS THE POINT OF HAVING IT AT ALL. A block
            // list you cannot see is a list you cannot undo; a block list you see every time you
            // open the tab is a screen that keeps showing you the people you asked not to see.
            if (!Group("Blocked", $"{blocked} blocked. They cannot join a game you host.", false))
                return;

            foreach (string id in list.Blocked)
            {
                string subject = id;

                // ⚠️ THE ID AND NOT A NAME, DELIBERATELY. `SocialList.Blocked` stores ids on
                // purpose: a list holding handles keeps drawing the name of somebody you blocked,
                // and it has to survive them renaming themselves.
                UiRows.ButtonRow(_list, Shorten(subject), "UNBLOCK", () =>
                {
                    social?.Unblock(subject);
                    MenuSfx.Click();
                }, "They can join your games again. They are not added back as a friend.");
            }
        }

        /// <summary>⚠️ A PLAYER ID IS ABOUT THIRTY CHARACTERS AND A ROW LABEL IS NOT. It is shown
        /// at all only because a blocked row has nothing else to identify it by.</summary>
        private static string Shorten(string id)
            => string.IsNullOrEmpty(id) || id.Length <= 12 ? id ?? "" : id.Substring(0, 12) + "\u2026";

        /// <summary>
        /// ⚠️⚠️ JOINING GOES THROUGH THE PATH THE JOIN PANEL ALREADY USES, AND `ServerQuery`
        /// RESOLVES A CODE LAN-FIRST THEN ONLINE. A second join path would be a second copy of
        /// the reconnection, seat-reclamation and relay-versus-LAN decisions `LobbySession`
        /// already owns, which `docs/TODO.md` § 38.5 records the cost of: three dead protocols,
        /// and the maintained one being the one nothing called.
        /// </summary>
        private void JoinFriend(string joinCode)
        {
            if (string.IsNullOrEmpty(joinCode)) return;

            MenuSfx.Click();
            Close();
            SceneFlow.PendingJoinCode = joinCode;
            SceneFlow.Go(SceneFlow.MatchSetup);
        }

        // -------------------------------------------------------------------
        // § MATCHES
        // -------------------------------------------------------------------

        private void BuildMatchesTab()
        {
            UiRows.Section(_list, "Recent matches",
                "The last twenty, newest first. The colour on the left is where you placed.");

            var career = GameServices.Career;
            if (career == null || _shown.Count == 0)
            {
                var history = career?.History;
                _shown = new List<MatchRecord>();
                if (history != null)
                    for (int i = 0; i < history.Count && i < HistoryPageSize; i++)
                        _shown.Add(history[i]);
            }

            if (_shown.Count == 0)
            {
                UiRows.Gap(_list, 16.0f);
                UiRows.ValueRow(_list, "Nothing played yet", "", "Your first match lands here.");
                SetFooter("", "");
                return;
            }

            string me = Net.CareerStore.LocalPlayerId;

            foreach (var record in _shown)
            {
                var line = MatchRecordRules.LineFor(record, me);
                string place = line != null ? Ordinal(line.Placement) : "-";
                string when = Short(record.PlayedUtc);

                var value = UiRows.ValueRow(_list,
                    $"{place}   {MenuKit.ModeLabel(record.Mode)}",
                    line != null ? $"{line.Score} PTS" : "",
                    $"{record.MapId}  ·  {record.Rounds} rounds  ·  " +
                    $"{record.DurationSeconds / 60.0f:0} min  ·  {when}",
                    line != null && line.Placement == 1 ? UiTheme.Amber : UiTheme.Cream);

                var captured = record;
                UiRows.RowButton((RectTransform)value.transform.parent, () => OpenDetail(captured));
            }

            UiRows.Gap(_list, 40.0f);
            SetFooter("REFRESH", career.Status);
        }

        private static string Ordinal(int placement)
        {
            switch (placement)
            {
                case 1: return "1ST";
                case 2: return "2ND";
                case 3: return "3RD";
                case 4: return "4TH";
                default: return "-";
            }
        }

        private static string Short(string utc)
        {
            return DateTime.TryParse(utc, null,
                       System.Globalization.DateTimeStyles.RoundtripKind, out DateTime when)
                ? when.ToLocalTime().ToString("d MMM HH:mm")
                : "";
        }

        private async void RefreshMatches()
        {
            var career = GameServices.Career;
            if (career == null) return;

            try
            {
                SetFooter("REFRESH", "Loading...");
                _shown = await career.HistoryPageAsync(_page * HistoryPageSize, HistoryPageSize);
                Show(Tab.Matches);
            }
            catch (Exception e)
            {
                SetFooter("REFRESH", e.Message);
            }
        }

        // -------------------------------------------------------------------
        // § ACCOUNT
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE SMALLEST TAB, AND IT IS SMALL ON PURPOSE. Six of the old panel's controls were
        /// account controls, and five of them are one-per-lifetime actions: you link a username
        /// once, you sign in when you move machine, and you delete an account never. The row that
        /// matters on any given day is the first one, which just says what state you are in.
        /// </summary>
        private void BuildAccountTab()
        {
            var account = GameServices.Account;

            if (!Group("This account",
                       "Where your progress is kept and what would happen to it."))
            {
                SetFooter("", _notice);
                return;
            }

            if (account == null)
            {
                UiRows.ValueRow(_list, "Accounts", "unavailable");
                SetFooter("", "");
                return;
            }

            UiRows.ValueRow(_list, "Status",
                account.IsGuest ? "GUEST"
                : account.HasPassword ? "SIGNED IN"
                : "LOCAL ONLY",
                account.Status,
                account.HasPassword ? UiTheme.MenuGreen : UiTheme.Amber);

            UiRows.ValueRow(_list, "Player id", account.PlayerId,
                "Never shown to anybody else. It is what your career is filed under.");

            if (!account.HasPassword)
            {
                UiRows.Section(_list, "Keep your progress",
                    "Right now everything you have earned only exists on this machine. Clearing the game data would lose it.");
                UiRows.ButtonRow(_list, "Username and password", "SET ONE UP", OpenSignIn,
                    "Keeps everything you have already played.", "WoodPrimaryButton");
            }
            else
            {
                UiRows.Section(_list, "Another machine",
                    "Signing in somewhere else brings this career with you.");
                UiRows.ButtonRow(_list, "Signed in as " + account.Username, "SWITCH ACCOUNT",
                    OpenSignIn, "Leaves whatever is on this machine behind.");
            }

            // ⚠️ CLOSED UNLESS A GUEST IS ALREADY PLAYING. Handing the machine over is a
            // once-a-tournament action, and a row offering it on every visit is the same noise
            // PLAY AS GUEST made sitting beside DELETE ACCOUNT. When a guest IS on, the state is
            // urgent and the group opens itself.
            if (Group("Tournament guest",
                      "Lets somebody else play on this machine without touching your account.",
                      account.IsGuest))
            {
                UiRows.ButtonRow(_list,
                    account.IsGuest ? "A guest is playing" : "Hand over the machine",
                    account.IsGuest ? "TAKE IT BACK" : "PLAY AS GUEST", ToggleGuest);
            }

            // ⚠️⚠️ THE DANGER ZONE IS LAST, ALONE, UNDER ITS OWN HEADING, AND NOT NEXT TO
            // ANYTHING SAFE. In the panel this replaces, DELETE ACCOUNT sat in a row between PLAY
            // AS GUEST and CLOSE at exactly the same size, which is one misclick from losing a
            // career. It still takes two presses and the second one says so.
            UiRows.Gap(_list, 48.0f);

            if (Group("Danger",
                      "Deleting removes the account, the profile and every match on the server.",
                      _deleteArmed))
            {
                UiRows.ButtonRow(_list, "Delete this account",
                    _deleteArmed ? "PRESS AGAIN TO DELETE" : "DELETE",
                    DeleteAccount, _deleteArmed ? "This is permanent." : "", "WoodDangerButton");
            }

            UiRows.Gap(_list, 40.0f);
            SetFooter("", _notice);
        }

        private void OpenSignIn()
        {
            _notice = "";
            _signIn.Open();
        }

        private void ToggleGuest()
        {
            try
            {
                var account = GameServices.Account;
                if (account.IsGuest) account.LeaveGuest();
                else account.SignInAsGuest(account.DisplayName);
                Show(Tab.Account);
            }
            catch (Exception e) { _notice = e.Message; SetFooter("", _notice); }
        }

        private async void DeleteAccount()
        {
            if (!_deleteArmed)
            {
                _deleteArmed = true;
                Show(Tab.Account);
                return;
            }

            try
            {
                _notice = "Deleting...";
                SetFooter("", _notice);
                await GameServices.Account.DeleteAsync();
                _deleteArmed = false;
                _notice = "Account deleted.";
                Show(Tab.Account);
            }
            catch (Exception e)
            {
                _notice = e.Message;
                SetFooter("", _notice);
            }
        }

        // -------------------------------------------------------------------
        // § THE FOOTER
        // -------------------------------------------------------------------

        private void SetFooter(string action, string note)
        {
            _footerNote.text = note ?? "";

            bool visible = !string.IsNullOrEmpty(action);
            _footerAction.gameObject.SetActive(visible);
            if (visible && _footerLabel != null) _footerLabel.text = action;
        }

        /// <summary>
        /// ⚠️ THE REFRESH IS THE WHOLE OF WHAT THIS TAB'S ONE ACTION DOES, and the redraw is on
        /// `SocialStore.Changed` rather than here. A screen that redraws itself after asking
        /// would be drawing the old list, because the call is asynchronous and the press is not.
        /// </summary>
        private void RefreshFriends()
        {
            GameServices.Social?.Refresh();
            MenuSfx.Click();
        }

        private void FooterPressed()
        {
            switch (_tab)
            {
                case Tab.Profile: SaveProfile(); break;
                case Tab.Friends: RefreshFriends(); break;
                case Tab.Matches: RefreshMatches(); break;
            }
        }
    }
}
