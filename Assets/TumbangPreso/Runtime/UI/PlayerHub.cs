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
        /// <summary>
        /// ⚠️ `Loadout` IS GONE. It moved to the fighter picker on 2026-09-02
        /// (🧑: **"put loadout here, it makes no sense to be in profile"**); `BuildTabColumn`
        /// carries both sides of that decision and `ConvertedCharacterSelect.SlotView` is where
        /// the feature lives now.
        /// </summary>
        private enum Tab { Profile, Friends, Career, Matches, Account }

        private const int HistoryPageSize = 20;

        // -------------------------------------------------------------------
        // § THE TWO REGIONS, AND EVERY NUMBER IN THEM IS DERIVED FROM ONE OTHER NUMBER.
        //
        // ⚠️⚠️ NOTHING HERE IS A FRACTION OF THE CANVAS, AND THAT IS `CLAUDE.md` § 6.2c QUESTION 1
        // APPLIED. `AspectSafeCanvas` uses `Expand`, so the canvas is about 1920 units wide at 4:3
        // and about 2400 on the short wide window he actually plays in: **one fraction is two
        // different widths.** The rail is a fixed 420 because that is what its content needs, and
        // the page is "whatever is left", which is the only pair of definitions that holds at all
        // nine resolutions `PlayerHubLayoutProbe` drives.
        // -------------------------------------------------------------------

        /// <summary>The air between the screen's edge and either region.</summary>
        private const float ScreenMargin = 56.0f;

        /// <summary>
        /// ⚠️ 420 IS THE CONTENT PLUS ONE INSET EITHER SIDE, NOT A PROPORTION. The widest thing in
        /// the card is a 368-unit tab, and 368 + 26 + 26 is 420. Sizing it against the window is
        /// the fault § 100 records on the sign-in column, where 38 per cent of his window was 860
        /// units of board around a 420-unit form.
        /// </summary>
        private const float RailWidth = 420.0f;

        private const float RailPad = 26.0f;

        /// <summary>The gap between the rail and the page. ⚠️ Wider than `RailPad` on purpose: the
        /// space BETWEEN two objects has to beat the space inside either of them or they read as
        /// one object with a line down it.</summary>
        private const float RailGap = 36.0f;

        /// <summary>
        /// How much of the rail the identity block owns WHEN THERE IS XP TO SHOW.
        ///
        /// ⚠️ THE ARITHMETIC, SO THE NEXT PERSON DOES NOT RE-DERIVE IT: the handle's box is
        /// centred 28 below the pad and is 56 tall, so it ends 56 below it; the state line hangs
        /// from 76 and is given three lines, 66, so it ends at 142; the rule is at 158; the level
        /// row is centred at 188 and is 28 tall, so it ends at 202; the XP track is centred at 216
        /// and is 10 tall, so it ends at 221. Eight units of air under that is 229.
        /// </summary>
        private const float IdentityHeight = 229.0f;

        /// <summary>
        /// The same block on an account that has never earned XP, which is every fresh account.
        ///
        /// ⚠️⚠️ IT COLLAPSES RATHER THAN LEAVING THE ROOM EMPTY, AND HE CAUGHT THAT IN THE FIRST
        /// RENDER OF THE COLUMN. 🧑 2026-09-02, with a crop of this rail: **"thhis looks really
        /// good just tighten it, i dont want huge empty space"**. `RefreshHeader` has always
        /// hidden the level, the XP count and (since § 120.5 row 6) the track itself when
        /// `Xp == 0`, so on a fresh account the block was drawing a name, a sentence, a rule and
        /// then **79 units of nothing**, which is the exact fault § 120.5 row 6 records one
        /// control smaller: a mark that means nothing, left drawn because the branch that empties
        /// it only emptied the words.
        ///
        /// ⚠️ THE HEIGHT IS A LAYOUT FACT AND THEREFORE LIVES WHERE THE LAYOUT IS, which is why
        /// `RefreshHeader` writes it rather than `BuildIdentity`. The block is built once and the
        /// account changes underneath it: signing in mid-session turns the XP on, and a height
        /// captured at build time would be a rail that is correct until the player does the one
        /// thing this screen exists for.
        /// </summary>
        private const float IdentityHeightNoXp = 166.0f;

        private const float TabHeight = 58.0f;

        /// <summary>Six tabs and the five gaps between them.</summary>
        /// <summary>
        /// The tab column, added up.
        ///
        /// ⚠️⚠️ FIVE, NOT SIX, SINCE 2026-09-02, AND THIS NUMBER IS WHY THE COUNT IS ARITHMETIC
        /// RATHER THAN A CONSTANT. LOADOUT left the hub for the fighter picker on 🧑's instruction
        /// (**"put loadout here, it makes no sense to be in profile"**); `BuildRail` sizes the card
        /// from `RailPad + IdentityHeight + RailBlockGap + TabsHeight + RailBlockGap +
        /// CloseHeight`, so a column that lost a tab without this line losing one would have left
        /// **68 units of bare cream** under CLOSE. That is *"i dont want huge empty space"*
        /// (§ 121.10 row 5) coming straight back on the same card, one tab later.
        /// </summary>
        private const float TabsHeight = (TabHeight * 5.0f) + (TabGap * 4.0f);

        private const float TabGap = 10.0f;

        private const float CloseHeight = 52.0f;

        /// <summary>
        /// The air between the rail's three blocks: identity, destinations, the way out.
        ///
        /// ⚠️ BIGGER THAN `TabGap` ON PURPOSE. The space BETWEEN two groups has to beat the space
        /// inside either of them or the reader sees one list of eight things instead of three
        /// blocks, which is the ordering tool `game-ui-design` calls the last and cheapest one and
        /// the one this rail spends instead of drawing more lines.
        /// </summary>
        private const float RailBlockGap = 24.0f;

        /// <summary>The page's one status line, above the rows.</summary>
        private const float PageHeaderHeight = 44.0f;

        /// <summary>The page's one action, below the rows.</summary>
        private const float PageFooterHeight = 76.0f;

        /// <summary>
        /// ⚠️ THE STATUS LINE IS CAPPED RATHER THAN STRETCHED, AND § 94.7 FAULT 6 IS WHY. When the
        /// value column moved, every control that was allowed to fill its slot became the widest
        /// object on the page and therefore the loudest: a field for fourteen characters drew as a
        /// 715 px white rectangle. A `Text` has no fill, so the cap here is not about loudness but
        /// about the same discipline: the page is up to 1840 units wide on his window, and a
        /// sentence given all of it wraps nowhere and runs under the action button in the corner.
        /// </summary>
        private const float PageNoteWidth = 980.0f;

        private const float PageActionWidth = 230.0f;

        private Canvas _canvas;
        private GameObject _root;
        private RectTransform _rail;
        private RectTransform _tabHost;
        private RectTransform _pageArea;
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
        /// <summary>
        /// Which mode's career this tab is showing.
        ///
        /// ⚠️⚠️ IT FOLLOWS `SceneFlow.SelectedMode` NOW AND IT USED TO BE HARD-CODED TO CLASSIC.
        /// `SceneFlow.SelectedMode` defaults to **Hero Strike**, which is the mode the ranked
        /// ladder is on (`docs/TODO.md` § 105) and the mode the lobby lands in, so the career tab
        /// opened on the numbers for the OTHER game and the Ability builds group, which is Hero
        /// Strike only, was invisible until the player found a dropdown and changed it.
        /// **A tab that opens on the wrong mode is a tab whose contents look missing.**
        /// § 114.12.
        /// </summary>
        private GameMode _mode = SceneFlow.SelectedMode;
        private int _page;
        private List<MatchRecord> _shown = new List<MatchRecord>();
        // ⚠️ `_loadoutHeroIndex` AND `_loadoutViews` LIVED HERE AND ARE DELETED WITH THE TAB
        // THEY BACKED. See the long note above `BuildAchievementsRows` for what moved where and
        // for the one behaviour (browsing a locked variant) that deliberately did not come with it.
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
            if (!InputLayer.MenuNav.CancelPressed) return;

            // ⚠️ THE PRESS IS SPENT BEFORE ANYTHING UNDER THIS SCREEN CAN READ IT. See
            // `ScreenTakeover.ConsumeEscape`: `Input.GetKeyDown` is a fact about the frame, so
            // without this the lobby underneath backs out on the same press that closed the hub.
            ScreenTakeover.ConsumeEscape();

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

            // ⚠️⚠️ REGISTERED, AND IT NEVER WAS. `ScreenTakeover`'s own header is about chrome
            // asking *"is anything on top of me"* rather than keeping a list, and the register
            // had exactly one member: the character maker. **The hub is a full-screen takeover at
            // sorting order 500 and nothing in the game knew it existed**, so the lobby
            // underneath read Escape on the same press that closed it. Same fault as the maker's,
            // one screen over. `ScreenTakeover.EscapeIsSpoken`.
            ScreenTakeover.Register(this, () => _root != null && _root.activeSelf);

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

            // ⚠️⚠️ OPAQUE. ALPHA 1.0. IT WAS 0.93 AND 0.93 IS NOT OPAQUE, WHICH IS THE SAME
            // FINDING `CustomCharacterScreen.Build` RECORDS ONE SCREEN OVER AND ONE DAY EARLIER.
            // 🧑 2026-09-01, with a shot of the ACCOUNT tab drawn over a lit lobby: *"profile ui
            // ugly"*, *"cannt read"*, *"maybe but smth on Background dont make it transparent"*.
            //
            // **Seven per cent of a lit street is still a street.** What is under this screen is
            // not mid grey: it is four voxel characters at full saturation, the amber MATCH
            // SETTINGS and LOBBY & SERVERS plates, a green START MATCH and a row of houses, and
            // every one of those shapes stayed legible through the scrim and competed with the
            // rows drawn on top of them. The arithmetic promises (16, 13, 12) behind a heading
            // and the render measures nothing like it, for the reason that entry sets out: the
            // surface underneath is lit and saturated rather than neutral.
            //
            // ⚠️ AND THE ANSWER IS NOT 0.99. There is nothing here for a scrim to protect. The
            // one thing on this screen is the player's own account, the lobby behind it is a
            // second screen competing for the same eye, and a takeover that shows the screen it
            // took over has not taken it over. Solid wood, which is also what makes this read as
            // furniture in 🧑's own visual language (`docs/VISION.md` § 6) rather than as a
            // window someone left open.
            // ⚠️⚠️ CREAM, NOT `WoodDeep`, AND THIS ONE LINE IS 🧑'S WHOLE REPORT ON THIS SCREEN.
            // 2026-09-01, with a crop of the account screen off the paper build: **"PLAYER CARD IS
            // STILL BROWN AND HARD TO READ COULD BE IMPROVED"**. He is right, and the reason it
            // survived a pass that repainted every other surface in the front end is instructive:
            // `PaperDress.Screen` walks `GodotPanel`, `GodotButton` and `WoodSkin`, and **the
            // backdrop is none of the three.** It is a bare `Image` with a colour on it, so the
            // one node on this screen that decides what colour the screen IS was invisible to the
            // machinery that repaints screens.
            //
            // ⚠️ AND EVERY LABEL ON TOP OF IT WAS ALREADY BEING CONVERTED, which is what made it
            // unreadable rather than merely old. `PaperDress.Type` remaps `Cream` and `CreamMuted`
            // to `PaperInk` and `PaperInkSoft`, because those are correct on paper; against a
            // `WoodDeep` backdrop they are dark brown words on a dark brown board. **The dress
            // half-ran, and a half-converted screen is worse than an unconverted one.**
            //
            // ⚠️ IT IS `Paper` AND NOT A `Sheet` SURFACE, because this is the field the sheets sit
            // on rather than a sheet itself, and it covers the whole screen: a cut-out silhouette
            // with a halo and a cast shadow at 1920 units is a rounded corner nobody sees and a
            // shadow falling on nothing.
            MenuKit.Backdrop(_root.transform, UiTheme.Paper);

            // ⚠️⚠️ TWO REGIONS, AND THE LEFT ONE IS AN ID CARD WITH THE NAVIGATION IN IT. This is
            // `docs/TODO.md` § 119.5's own plan for this screen finally built: *"an ID card with a
            // tab COLUMN rather than a tab row ... six tabs across a header is the row that made
            // § 92 unreadable"*, and § 120.7 named it as the largest thing left undone, with the
            // cost measured: **on the PROFILE tab of a fresh account the bottom 45 per cent of the
            // screen was bare cream**, which is § 6.2's *"big ass empty sopace"* on the one screen
            // in the game that is entirely about the player.
            //
            // **A column fixes both halves of that at once and § 120.7 says so in one line:** *"A
            // column would fill the left edge and narrow the rows at the same time."* The rows get
            // narrower, so `UiRows.ValueColumn` comes in with them and the label-to-value journey
            // § 94.7 fault 1 measured at 1600 px shortens again; and the tall empty left edge is
            // now the object that says who you are and where you can go.
            BuildRail();
            BuildPage();

            BuildIdentity();
            BuildTabColumn();
            BuildRailFooter();

            var listGo = new GameObject("ListArea", typeof(RectTransform));
            listGo.transform.SetParent(_pageArea, false);
            var listRt = (RectTransform)listGo.transform;
            listRt.anchorMin = Vector2.zero;
            listRt.anchorMax = Vector2.one;
            // ⚠️ THE INSETS ARE AGAINST THE PAGE, NOT AGAINST THE SCREEN, which is the whole point
            // of the page being a rect of its own. The old version wrote `0.06` and `0.94` of the
            // WHOLE canvas here, and `AspectSafeCanvas` scales on the short axis, so one fraction
            // was two different widths at two aspect ratios (`CLAUDE.md` § 6.2c question 1).
            listRt.offsetMin = new Vector2(0.0f, PageFooterHeight);
            listRt.offsetMax = new Vector2(0.0f, -PageHeaderHeight);

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

            // ⚠️⚠️ THE SHELL IS DRESSED AT INSTALL AND NOT ONLY IN `Show`, AND THE WIDENED
            // `PaperPurityProbe` IS WHAT SAID SO. It reported all six tabs, CLOSE, `MatchDetail`
            // and its BACK as `still carries GodotButton (WoodButton)`: `Show` dresses `_root`,
            // and `Show` does not run until the player opens this screen and picks a tab. So the
            // whole shell sat wooden from scene load until the first press, which is invisible in
            // a render (nobody photographs a screen that is switched off) and is exactly the case
            // `docs/TODO.md` § 119.6 wanted a probe for.
            //
            // ⚠️ IT IS NOT A REPLACEMENT FOR THE CALL IN `Show`. That one exists because the ROWS
            // are destroyed and rebuilt on every tab press; this one is for the furniture around
            // them, which is built exactly once, here.
            PaperDress.Screen(_root.transform);

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
            ScreenTakeover.Unregister(this);
            if (GameServices.Account != null) GameServices.Account.Changed -= OnDataChanged;
            if (GameServices.Career != null) GameServices.Career.Changed -= OnDataChanged;
            if (GameServices.Social != null) GameServices.Social.Changed -= OnDataChanged;
            if (_signIn != null) _signIn.Closed -= OnSignInClosed;
        }

        /// <summary>
        /// The left rail: an ID card with the whole of the navigation inside it.
        ///
        /// ⚠️⚠️ IT REPLACES A SIX-TAB ROW ACROSS THE TOP AND `docs/TODO.md` § 119.5 PLANNED THIS
        /// FROM THE START: *"an ID card with a tab COLUMN rather than a tab row ... six tabs
        /// across a header is the row that made § 92 unreadable"*. § 120.7 then measured what the
        /// row cost: on the PROFILE tab of a fresh account **the bottom 45 per cent of the screen
        /// was bare cream**, because a row across the top spends 232 units of height on chrome and
        /// leaves the content one wide short block with nothing under it.
        ///
        /// **The rail says three things top to bottom, which is the whole reason it is one object
        /// rather than three:** who you are, where you can go, and how you leave. `CLAUDE.md`
        /// § 6.3's four questions about a journey are answered in one column, in reading order.
        ///
        /// ⚠️ FULL HEIGHT, AND SIZED IN UNITS RATHER THAN AS A FRACTION. `AspectSafeCanvas` scales
        /// on the SHORT axis, so the canvas is about 1920 wide at 4:3 and about 2400 on his
        /// window: a fraction of the width is two different rails. 420 units is the widest control
        /// this card carries (a 368-unit label plus one 26-unit inset either side) and it is
        /// therefore the same rail at every shape the game ships at.
        /// </summary>
        private void BuildRail()
        {
            var card = PaperKit.Sheet(_root.transform, "IdentityRail");
            card.raycastTarget = true;

            // ⚠️⚠️ THE CARD IS THE HEIGHT OF ITS CONTENT AND IT WAS FULL HEIGHT, AND HE REJECTED
            // THAT ON THE FIRST RENDER: **"thhis looks really good just tighten it, i dont want
            // huge empty space"**. Full height put three blocks in a 1000-unit column and left
            // about 300 units of bare card between them, which is the same *"big ass empty
            // sopace"* § 119.10 records him rejecting on the fighter column, one screen over.
            //
            // ⚠️ EMPTY CARD AND EMPTY FIELD ARE NOT THE SAME THING, and that distinction is the
            // whole reason this works. A card that ends is an object with a size; the paper under
            // it is the screen's own margin. The hole he photographed was INSIDE a drawn surface,
            // which reads as a layout that failed to fill itself.
            //
            // ⚠️ THE HEIGHT IS WRITTEN BY `RefreshHeader`, not here. It depends on whether the
            // account has XP, and the account can change while this screen is open.
            _rail = card.rectTransform;
            _rail.anchorMin = new Vector2(0.0f, 1.0f);
            _rail.anchorMax = new Vector2(0.0f, 1.0f);
            _rail.pivot = new Vector2(0.0f, 1.0f);
            _rail.anchoredPosition = new Vector2(ScreenMargin, -ScreenMargin);
            _rail.sizeDelta = new Vector2(RailWidth, RailPad + IdentityHeight + RailBlockGap
                                                     + TabsHeight + RailBlockGap + CloseHeight
                                                     + RailPad);
        }

        /// <summary>
        /// Everything the rail is not: the rows, their one status line and the tab's one action.
        ///
        /// ⚠️ IT IS A RECT WITH NO GRAPHIC ON IT, WHICH IS DELIBERATE. A second big card beside
        /// the first would make this screen two boxes on a board, which is the fault § 92 records
        /// (*"everything is js shit on one block"*) solved by adding a block. The page is a REGION:
        /// it exists so that every inset inside it is measured against the space the rows actually
        /// have, instead of against the whole canvas.
        /// </summary>
        private void BuildPage()
        {
            var go = new GameObject("Page", typeof(RectTransform));
            go.transform.SetParent(_root.transform, false);

            _pageArea = (RectTransform)go.transform;
            _pageArea.anchorMin = Vector2.zero;
            _pageArea.anchorMax = Vector2.one;
            _pageArea.offsetMin = new Vector2(ScreenMargin + RailWidth + RailGap, ScreenMargin);
            _pageArea.offsetMax = new Vector2(-ScreenMargin, -ScreenMargin);
        }

        /// <summary>
        /// Who you are, at the top of the rail, in the biggest type on the screen.
        ///
        /// ⚠️⚠️ IT IS THE ONLY PLACE THE HANDLE APPEARS. The old arrangement printed it on the
        /// account panel AND on the career panel, so two screens each claimed to be where your
        /// name lived and neither was the header of anything. A block that persists across the
        /// tabs is what makes six tabs feel like one screen.
        ///
        /// ⚠️⚠️ THE XP BLOCK IS NO LONGER ANYWHERE NEAR A BUTTON, WHICH IS § 94.7 FAULT 5 MADE
        /// IMPOSSIBLE RATHER THAN FIXED. That fault was a 440-unit track centred at -300 running
        /// under a CLOSE button that started at -208: **128 px of the bar, the end a player reads
        /// to see how close they are, drawn behind a wood button.** It was corrected by moving the
        /// block to a number that cleared CLOSE by 30 px, which is two absolute offsets checked
        /// against each other once. Here the track is a child of the card, inset from the card's
        /// own edges, and the only other thing in the card is below it in the same column: there
        /// is no second offset for it to disagree with.
        /// </summary>
        private void BuildIdentity()
        {
            float inner = RailWidth - (RailPad * 2.0f);

            // ⚠️⚠️ EVERY OFFSET HERE IS THE BOX'S CENTRE, BECAUSE `MenuKit.Place` PINS THE PIVOT
            // TO (0.5, 0.5) AND WRITING A PIVOT AFTERWARDS MOVES THE RECT BY HALF ITS OWN SIZE.
            // That is a silent shift of 184 units on a 368-unit box, and it is the same class of
            // fault as § 120.2's BACK: two lines that both look like a placement, one of which
            // quietly undoes the other. `Mid` is the one place the arithmetic is written.
            float mid = RailPad + (inner * 0.5f);

            _handle = MenuKit.Label(_rail, "", 44, UiTheme.PaperInk,
                new Vector2(0.0f, 1.0f), new Vector2(mid, -(RailPad + 28.0f)),
                new Vector2(inner, 56.0f), TextAnchor.MiddleLeft);

            // ⚠️⚠️ IT WRAPS, AND ON A 420-UNIT RAIL IT HAS TO. The longest string this label ever
            // holds is `GUEST  ·  nothing is being saved to an account`, which is about 470 units
            // at 18 and was given a 720-unit box when this block ran across the top of the screen.
            // In a column it does not fit on one line at any readable size, and `MenuKit.Label`
            // OVERFLOWS rather than wrapping (`CLAUDE.md` § 6.2c), so without this it would draw
            // straight off the side of the card. Two lines of 18 is 40 units and the block below
            // is placed from that number.
            // ⚠️⚠️ THREE LINES, AND TWO WAS A TRUNCATION THE FIRST RENDER CAUGHT. On
            // `LobbyAccount-v61.png` this label reads `PLAYING ON THIS MACHINE ONLY  ·  no` and
            // stops: the string ends `no username yet`, the box was 44 units, and
            // `verticalOverflow = Truncate` **drops whole lines silently**. A 46-character
            // sentence at 18 units in a 368-unit box is three lines, so it is given three.
            //
            // ⚠️ TRUNCATE RATHER THAN OVERFLOW IS STILL RIGHT. Overflow would draw the fourth line
            // over the rule under it, which is the silent overlap § 102.4 records; the fix for a
            // truncation is a box that fits the sentence, not a box that lets it escape.
            _state = MenuKit.Label(_rail, "", MenuKit.MinReadableUnits,
                UiTheme.PaperInkSoft, new Vector2(0.0f, 1.0f),
                new Vector2(mid, -(RailPad + 109.0f)), new Vector2(inner, 66.0f),
                TextAnchor.UpperLeft);
            _state.horizontalOverflow = HorizontalWrapMode.Wrap;
            _state.verticalOverflow = VerticalWrapMode.Truncate;

            var rule = PaperKit.Rule(_rail);
            MenuKit.Place(rule.rectTransform, new Vector2(0.0f, 1.0f),
                new Vector2(mid, -(RailPad + 158.0f)), new Vector2(inner, 2.0f));

            _levelChip = MenuKit.Label(_rail, "", 22, UiTheme.PaperInk,
                new Vector2(0.0f, 1.0f),
                new Vector2(RailPad + (inner * 0.25f), -(RailPad + 188.0f)),
                new Vector2(inner * 0.5f, 28.0f), TextAnchor.MiddleLeft);

            _xpCount = MenuKit.Label(_rail, "", MenuKit.MinReadableUnits,
                UiTheme.PaperInkSoft, new Vector2(0.0f, 1.0f),
                new Vector2(RailPad + (inner * 0.75f), -(RailPad + 188.0f)),
                new Vector2(inner * 0.5f, 28.0f), TextAnchor.MiddleRight);

            var track = new GameObject("XpTrack", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(_rail, false);

            MenuKit.Place((RectTransform)track.transform, new Vector2(0.0f, 1.0f),
                new Vector2(mid, -(RailPad + 216.0f)), new Vector2(inner, 10.0f));

            // ⚠️⚠️ THE BAR INVERTS WITH THE FIELD, WHICH IS THE SAME MOVE `PaperCraft.Surface.Sign`
            // RECORDS. It was an amber fill on a near-black track, which is correct on a wooden
            // screen where amber is the one light thing; on cream `UiTheme.Amber` `ffba00` against
            // `Paper` `f4ecdd` measures **1.46:1** (`tools/sample_png.js contrast`) and the FILLED
            // part of the bar is the part that disappears. He rejected that ratio by eye twice on
            // other controls (`docs/TODO.md` § 119.10), so this is applying his answer rather than
            // re-testing it. On paper the marker is the one DARK thing: wood in a `PaperSunk`
            // groove is about 8:1 and introduces no colour at all.
            track.GetComponent<Image>().color = UiTheme.PaperSunk;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(track.transform, false);
            _xpFill = fillGo.GetComponent<Image>();
            _xpFill.color = UiTheme.WoodMid;

            var fill = _xpFill.rectTransform;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0.0f, 1.0f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// The six destinations, down the rail, in the space between the identity block and the
        /// way out.
        ///
        /// ⚠️⚠️ A COLUMN RATHER THAN A ROW, AND THE REASON IS A MEASUREMENT RATHER THAN A STYLE.
        /// Six tabs across the top were 168 units wide at a 176 pitch, so every label had to fit
        /// **148 units of usable width**: `ACCOUNT` and `MATCHES` were at the edge of it and any
        /// seventh destination would have had to shrink all six. Down a 420-unit rail each tab has
        /// **368**, which is two and a half times the room, and a seventh costs one row of height
        /// on a column that has spare height by construction.
        ///
        /// ⚠️ THEY ARE CENTRED IN THE SPACE THEY HAVE RATHER THAN PACKED AT THE TOP OF IT. Six
        /// 58-unit tabs with a 10-unit gap are 398 units; the rail's middle band is about 690 at
        /// 1080 and about 1050 at 4:3, so packing them would leave a growing hole under the last
        /// one at exactly the aspect ratios `AspectRatioProbes` drives. Centred, the slack is
        /// split above and below and reads as air.
        ///
        /// ⚠️⚠️ LOADOUT IS A TAB BECAUSE NOBODY COULD FIND IT AS A GROUP, AND HE IS THE "NOBODY".
        /// 2026-09-01: *"i also dont know hhow to navigate to loadouts section"*, then, before
        /// anybody could answer: *"that isnt my fault thats ur fault"*, *"that means if i cnat ifnd
        /// it, no one owuld"*. It was four presses deep and two of them were invisible: the rows
        /// lived in a group called *Ability builds*, CLOSED BY DEFAULT, at the bottom of the CAREER
        /// tab, and on a fresh account the career tab returned before building it at all. **A
        /// collapsed group is a good answer to "never overwhelming" and a bad one to "where is the
        /// thing"**, and `CLAUDE.md` § 6.2 is three separate claims: this feature passed the third
        /// by failing the second.
        ///
        /// ⚠️ THE DOOR MOVED, IT DID NOT MULTIPLY, WHICH IS § 6.3's RULE BY NAME. There is exactly
        /// one place in the game that equips an ability build and it is named on the tab column you
        /// land on. And it is spelled the way he asks for it: the code calls these hero BUILDS
        /// (`HeroBuildRules`) and the word he reaches for is LOADOUT.
        ///
        /// ⚠️⚠️ FRIENDS IS A TAB RATHER THAN A GROUP ON `PROFILE`, WHICH IS A DECISION AND NOT AN
        /// OVERSIGHT. `FUTURE.md` § 0.5b's row for Phase 6 asks for *"a friends rail on the hub"*
        /// with **who is online now** as the one thing on it, and a group buried inside a tab about
        /// YOU fails that twice over: it is collapsed by default like every other group, and
        /// PROFILE is the screen about the local player while this is the only screen in the game
        /// about anybody else.
        /// </summary>
        private void BuildTabColumn()
        {
            var host = new GameObject("TabColumn", typeof(RectTransform));
            host.transform.SetParent(_rail, false);

            // ⚠️⚠️ TOP-ANCHORED AND FIXED HEIGHT, NOT STRETCHED AND CENTRED. The first version
            // filled the space between the identity block and CLOSE and centred the six tabs in
            // it, which on a full-height card meant **two holes rather than one**: about 200 units
            // above the tabs and 150 below. 🧑, with a crop of it: **"just tighten it, i dont want
            // huge empty space"**. The card is content-height now (see `BuildRail`), so the tabs
            // hang from the block above them and there is nothing left to centre in.
            _tabHost = (RectTransform)host.transform;
            _tabHost.anchorMin = new Vector2(0.0f, 1.0f);
            _tabHost.anchorMax = new Vector2(1.0f, 1.0f);
            _tabHost.pivot = new Vector2(0.5f, 1.0f);
            _tabHost.offsetMin = new Vector2(RailPad, 0.0f);
            _tabHost.offsetMax = new Vector2(-RailPad, 0.0f);
            _tabHost.anchoredPosition =
                new Vector2(0.0f, -(RailPad + IdentityHeight + RailBlockGap));
            _tabHost.sizeDelta = new Vector2(_tabHost.sizeDelta.x, TabsHeight);

            var layout = host.AddComponent<VerticalLayoutGroup>();
            layout.spacing = TabGap;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperCenter;

            // ⚠️⚠️⚠️ LOADOUT IS NOT IN THIS LIST ANY MORE AND THE PARAGRAPH ABOVE ARGUING FOR IT
            // IS KEPT, BECAUSE BOTH DECISIONS WERE HIS AND THE SECOND ONE ONLY MAKES SENSE NEXT
            // TO THE FIRST. On 2026-09-01 he could not find the ability builds (*"i also dont know
            // hhow to navigate to loadouts section"*, *"that means if i cnat ifnd it, no one
            // owuld"*) and they became a tab here. On 2026-09-02, having found them:
            // **"put loadout here, it makes no sense to be in profile"**, with a crop of CHOOSE
            // YOUR HERO, and *"there were button updates can u put loadouts in the choose ur hero
            // screen too and shit"*.
            //
            // ⚠️⚠️ AND HE IS RIGHT FOR THE REASON THE NOTE ABOVE ALREADY GIVES. § 6.3's rule is
            // *fix the door or MOVE it*, never add a second one, and this tab was a door moved to
            // the wrong room: a build belongs to a HERO, and the screen where a hero is chosen and
            // its two skills are described is the fighter picker. Making it a tab on the account
            // screen fixed findability and left the player equipping a build for a character they
            // had to re-select on a stepper to reach.
            //
            // ⚠️ NOTHING IS DELETED, WHICH IS THE OTHER HALF OF THE RULE. Every option, every
            // lock, every challenge counter and `HeroBuildRules` itself are untouched and are now
            // reached from `ConvertedCharacterSelect.SlotView` and `CycleSlot`. **A redesign that
            // quietly loses a feature is a regression wearing a better layout** (§ 121.6 item 4).
            var order = new[]
            {
                (Tab.Profile, "PROFILE"),
                (Tab.Friends, "FRIENDS"),
                (Tab.Career, "CAREER"),
                (Tab.Matches, "MATCHES"),
                (Tab.Account, "ACCOUNT"),
            };

            foreach (var (tab, label) in order) AddTab(host.transform, tab, label);
        }

        private void AddTab(Transform host, Tab tab, string label)
        {
            var button = MenuKit.WoodButton(host, label, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(368.0f, TabHeight), () => Show(tab));

            // ⚠️ THE LAYOUT GROUP OWNS THE RECT, SO THE HEIGHT HAS TO BE STATED AS AN ELEMENT.
            // `MenuKit.WoodButton` writes `sizeDelta`, and `childControlHeight` overwrites it on
            // the next pass; § 117.7 records the same thing happening to a tab that three comments
            // in this repository called *"four units taller"* and that was never taller in any
            // build, because `childForceExpandHeight` silently beat every `LayoutElement` under it.
            var element = button.gameObject.GetComponent<LayoutElement>();
            if (element == null) element = button.gameObject.AddComponent<LayoutElement>();
            element.minHeight = TabHeight;
            element.preferredHeight = TabHeight;
            element.flexibleHeight = 0.0f;

            _tabs[tab] = button;
        }

        /// <summary>
        /// The way out, at the bottom of the rail.
        ///
        /// ⚠️⚠️ IT MOVED OUT OF THE TOP-RIGHT CORNER AND INTO THE COLUMN, AND THAT IS WHAT MAKES
        /// THE RAIL A WHOLE OBJECT. `CLAUDE.md` § 6.3's last question about any screen is *"how do
        /// they get out, and is it one press"*, and the answer now sits in the same column as
        /// every other place this screen can send you, at the end of it. A floating CLOSE in a
        /// corner is a control with no neighbours, which is what § 92's six-button panel was made
        /// of.
        ///
        /// ⚠️ IT IS NAMED, AND THE NAME IS LOAD-BEARING FOR THE SHOT PASS. `UiRuntimeShots` used
        /// to close this screen with `Find("HubRoot").GetComponentInChildren&lt;Button&gt;()`,
        /// which is "the first button in the hierarchy" and was CLOSE only by accident of build
        /// order. In a column the first button is PROFILE, so the probe would have pressed a tab
        /// and photographed the hub it was trying to leave.
        ///
        /// ⚠️ ESCAPE STILL DOES THIS AND ALWAYS DID. See `Update`: innermost layer first, the
        /// match-detail popup before the screen. The button is the visible door; Escape is the one
        /// a player who has learned this game already knows.
        /// </summary>
        private void BuildRailFooter()
        {
            var close = MenuKit.WoodButton(_rail, "CLOSE", new Vector2(0.5f, 0.0f),
                new Vector2(0.0f, RailPad + (CloseHeight * 0.5f)),
                new Vector2(RailWidth - (RailPad * 2.0f), CloseHeight), Close);

            close.gameObject.name = "HubClose";
        }

        private void BuildFooter()
        {
            // ⚠️⚠️ THE ONE LINE EXPLAINING A TAB SITS ABOVE THE ROWS IT EXPLAINS, NOT UNDER THEM.
            // It was a footer, at the bottom-left of the whole screen, about 900 units below the
            // first row it described and under a list the player has to scroll to the end of to
            // reach. **A caption that arrives after the thing it captions is not a caption.** It
            // also carries the live status on two tabs (`career.Status`, `Loading...`), and a
            // status line the player only meets by scrolling is a status nobody reads.
            //
            // ⚠️ IT IS ALIGNED TO THE PAGE, so it starts at the same x as every row under it. The
            // version this replaces was pinned at 6 per cent of the canvas while `ListArea` began
            // somewhere else, which § 94.7 fault 7 records as *"one column or none"*.
            _footerNote = MenuKit.Label(_pageArea, "", MenuKit.MinReadableUnits,
                UiTheme.PaperInkSoft, new Vector2(0.0f, 1.0f),
                new Vector2(PageNoteWidth * 0.5f, -18.0f),
                new Vector2(PageNoteWidth, 30.0f), TextAnchor.MiddleLeft);

            // ⚠️⚠️ ONE ACTION, BOTTOM RIGHT OF THE PAGE, AND IT CHANGES WITH THE TAB. PUBG parks a
            // single persistent action there (UPLOAD TO CLOUD) and that is the whole idea: a
            // screen with one button has an obvious thing to do and a screen with six has none.
            // Everything else on this page is a row.
            //
            // ⚠️ BOTTOM RIGHT OF THE PAGE RATHER THAN OF THE SCREEN, which is now a different
            // place. It applies to the rows, so it belongs under the rows; the rail's own footer
            // is CLOSE, which applies to the screen.
            _footerAction = MenuKit.WoodButton(_pageArea, "SAVE", new Vector2(1.0f, 0.0f),
                new Vector2(-(PageActionWidth * 0.5f), 34.0f),
                new Vector2(PageActionWidth, 56.0f), FooterPressed,
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
        /// <summary>
        /// Whether the hub is showing.
        ///
        /// ⚠️ IT EXISTS SO THE SCREEN UNDERNEATH CAN DECLINE ONE ESCAPE. `ConvertedScreen.Update`
        /// and this class's `Update` both read `GetKeyDown(KeyCode.Escape)` on the same frame, so
        /// without this the hub closes AND the lobby backs out to the title in one press. The hub
        /// owns the press, and `ConvertedMatchSetup.Cancel` asks this before doing anything.
        /// </summary>
        public bool IsOpen => _root != null && _root.activeSelf;

        public void Open(bool onAccount = false)
        {
            _root.SetActive(true);
            VisibleChanged?.Invoke(true);
            Show(onAccount ? Tab.Account : Tab.Profile);
        }

        /// <summary>
        /// ⚠️⚠️ `OpenLoadout` LIVED HERE AND IS DELETED. It was the lobby YOUR SKILLS row's deep
        /// link into the LOADOUT tab; that row opens `ConvertedCharacterSelect` now
        /// (`ConvertedMatchSetup.OpenLoadout`), because 🧑 asked for the feature to move:
        /// **"put loadout here, it makes no sense to be in profile"**, 2026-09-02.
        ///
        /// ⚠️ ITS OWN ARGUMENT SURVIVED THE MOVE AND IS WORTH KEEPING. It read: *"the loadout has
        /// exactly one destination and the lobby row lands ON it rather than beside it"*, and
        /// *"it belongs under the character it changes"*. **Both are still true; the destination
        /// changed.** The picker is more literally "under the character it changes" than a tab on
        /// the account screen ever was.
        /// </summary>
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

            // ⚠️⚠️ THE DRESS RUNS AFTER EVERY TAB BUILD AND NOT ONCE AT INSTALL, BECAUSE THE
            // ROWS ARE DESTROYED AND REBUILT ON EVERY TAB PRESS (see the loop at the top of this
            // method). A single pass at install time would paint the PROFILE tab and leave the
            // other five wooden, which is exactly the kind of leftover 🧑 asked twice to be sure
            // of: *"MAKE SURE U COMPLETELY REPLACE UI BCZ I DOTN WANT LEFTOVER SHIT FROM OLD UI TO
            // STILL BE FRIGGING WITH US"*.
            //
            // ⚠️ IT IS IDEMPOTENT. `PaperSkin.Apply` destroys any `WoodSkin` it finds and
            // `PaperDress` disables rather than destroys, so re-running it on already-dressed
            // chrome costs a walk and changes nothing.
            PaperDress.Screen(_root.transform);
        }

        private static void Highlight(Button button, bool on)
        {
            var skin = button?.GetComponent<GodotButton>();
            if (skin == null) return;

            // ⚠️⚠️ RELIEF, NOT HUE, AND IT IS THE SAME PAIR THE LOBBY AND THE SIGN-IN
            // SCREEN NOW USE. A live tab painted solid amber against plain wood distinguishes
            // itself by colour alone, which `game-ui-design` lists as `colorblind-failure`, and it
            // spends the screen's accent on a statement about where you already are.
            // `GodotTheme.WoodTabIdleButton` carries the argument: the idle tab is drawn through
            // `WoodCraft.Surface.Field`, lit from BELOW, so the two tabs sit at two depths.
            skin.Variation = on ? "WoodTabLiveButton" : "WoodTabIdleButton";

            // ⚠️⚠️ AND THE PAPER SKIN IS SET DIRECTLY, BECAUSE BY THE SECOND TAB PRESS THE
            // `GodotButton` ABOVE IS DISABLED AND `Apply` IS SHOUTING INTO A COMPONENT NOBODY
            // READS. `Show` dresses the whole screen at the end of every rebuild, and
            // `PaperDress.ButtonSkin` turns `GodotButton` off on the way past (it writes its own
            // sprite on hover, so leaving it on flips the control back to wood under the pointer).
            // The variation above is still written because that is what the dress reads on the
            // NEXT pass; this line is what makes the current frame correct.
            //
            // ⚠️ `Live` AGAINST `Ghost`, WHICH IS A VALUE INVERSION OF ABOUT 10:1. The pair used
            // to arrive as `Token` against `Ghost`, measured 4 per cent apart on
            // `Logs/shots-runtime/Lobby-v52.png` and rejected there; the lobby moved and this
            // screen did not, which is 🧑's *"hard to read"* on six tabs he has to choose between.
            // `PaperButton` inverts the lettering to cream on its own, by watching the surface.
            if (PaperKit.MarkLive(button, on)) return;

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

            // ⚠️⚠️ THE HANDLE IS FITTED NOW, AND IN A COLUMN IT HAS TO BE. Across the top of the
            // screen it had a 680-unit box, which no display name could overflow; in a 420-unit
            // rail it has 368, and `Balance.PlayerNameMax` is 14 characters. Measured in
            // Darumadrop One at 44 units, fourteen wide characters run about **430**, so the
            // longest legal name overflows by about 60 and `MenuKit.Label` OVERFLOWS rather than
            // wrapping (`CLAUDE.md` § 6.2c): it would draw straight off the side of the card,
            // silently. `Fit` shrinks to the readable floor, which at 44 is four steps of room.
            //
            // ⚠️ THE TAG IS NOT PART OF THIS STRING. `LobbyName` is the display name; the `#1296`
            // half lives on the PROFILE tab's own row, which is where § 92.3 put it.
            _handle.fontSize = 44;
            MenuKit.Fit(_handle, RailWidth - (RailPad * 2.0f));

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

            // ⚠️⚠️ THE TRACK HIDES WITH THE NUMBERS, AND ONLY A CREAM SCREEN MADE THAT VISIBLE.
            // The branch above has always emptied the two labels when there is no XP, and it has
            // always left the 440-unit track drawn: on a `WoodDeep` backdrop an empty `WoodDark`
            // groove was invisible, so nobody saw it. On paper the same track is a `PaperSunk` bar
            // four value steps darker than the sheet, and
            // `Logs/shots-runtime/LobbyAccount-v57.png` shows it as **a stray tan rule floating
            // under CLOSE with no label anywhere near it** — a mark that means nothing, on the
            // screen 🧑 said he could not read. A bar with nothing in it and no caption is not a
            // quiet zero, it is a line.
            //
            // ⚠️ IT IS THE SAME ARGUMENT THE `xp > 0` GUARD ABOVE ALREADY MAKES about the level
            // number, applied to the one part of that block that was not covered by it.
            if (_xpFill != null && _xpFill.transform.parent != null)
                _xpFill.transform.parent.gameObject.SetActive(xp > 0);

            // ⚠️⚠️ AND THE RAIL COLLAPSES BY THE HEIGHT THOSE THREE CONTROLS WERE OCCUPYING,
            // WHICH IS THE OTHER HALF OF THE SAME BRANCH. The guards above have always emptied
            // the level and the count when there is no XP, and § 120.5 row 6 added the track to
            // them; **the SPACE was still reserved**, so on every fresh account the card drew a
            // name, a sentence, a rule and then 79 units of nothing. 🧑, with a crop of exactly
            // that: **"thhis looks really good just tighten it, i dont want huge empty space"**.
            //
            // ⚠️ IT IS WRITTEN HERE RATHER THAN AT BUILD TIME BECAUSE THE ACCOUNT CHANGES UNDER
            // THIS SCREEN. Signing in is something a player does WITH this screen open, and a
            // height captured in `BuildIdentity` would be a rail that is correct until the moment
            // the thing it describes happens.
            float identity = xp > 0 ? IdentityHeight : IdentityHeightNoXp;

            if (_rail != null)
                _rail.sizeDelta = new Vector2(RailWidth,
                    RailPad + identity + RailBlockGap + TabsHeight + RailBlockGap
                    + CloseHeight + RailPad);

            if (_tabHost != null)
                _tabHost.anchoredPosition =
                    new Vector2(0.0f, -(RailPad + identity + RailBlockGap));
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

                // ⚠️⚠️ THIS IS THE LOBBY'S NAME FIELD NOW, AND THE NODE NAME IS THE CONTRACT.
                // 🧑 2026-09-01: **"why does insert player name still live here shouldnt tat be in
                // the account button?"**, of a second field on the lobby's top rail. He is right:
                // this row has existed since Phase 1 and the rail's was a duplicate writing the
                // same string through a different path. `UiClickProbe`, `UiRuntimeShots` and
                // `PaperPurityProbe` all reach for `PlayerNameEdit` by name, so renaming the
                // GameObject here is what keeps three tests pointed at the control that survived
                // rather than at the one that was deleted.
                _displayName.gameObject.name = "PlayerNameEdit";

                // ⚠️⚠️ AND IT WORKS WITH NO ACCOUNT AT ALL, WHICH IS THE HALF THAT WAS MISSING.
                // 🧑: *"bcz offlinne mode is for torunnaments and shit"*. `FUTURE.md` § 0.5 rule 7
                // and `docs/TODO.md` § 97: practice, LAN and joining by code must never sit behind
                // a login, and the nationals in General Santos City are why. A machine with no
                // network still has to be able to set the name the other three laptops will see,
                // and `Settings.SettingsStore.PlayerName` is what `NetSession.ConfigureClientHello`
                // actually puts on the wire.
                _displayName.text = account?.DisplayName
                    ?? Settings.SettingsStore.Current.PlayerName ?? "";

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
                string name = _displayName != null ? _displayName.text : a?.DisplayName ?? "";

                // ⚠️⚠️ SIGNED OUT, THE NAME STILL SAVES. Without this the whole method threw a
                // null reference on `a.Bio` and the one control a tournament machine needs was
                // dead. See the note on the field itself; `GameSettings.SanitiseName` is the same
                // function `LobbySession.Admit` runs on arrival, so the name this writes is the
                // name every other machine sees.
                if (a == null)
                {
                    Settings.SettingsStore.Current.PlayerName =
                        Settings.GameSettings.SanitiseName(name);
                    Settings.SettingsStore.Save();

                    _notice = "Saved on this machine.";
                    Show(Tab.Profile);
                    return;
                }

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
            if (totals.Matches == 0) { EmptyCareer(profile); return; }

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

            // ⚠️ THE ABILITY BUILD ROWS WERE CALLED HERE TOO AND THE CALL IS GONE WITH THEM. This
            // was the SECOND place they were drawn, which is § 114.12's finding from the other
            // side: they were on the career tab AND on their own tab at once, so the same twelve
            // rows appeared in two destinations and neither one was the answer to "where is the
            // loadout". They are on the fighter picker now and nowhere else, which is § 6.3's
            // *one door* stated as a fact about the code rather than about the navigation.
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

        /// <summary>
        /// ⚠️⚠️⚠️ `BuildLoadoutTab` AND `BuildAbilityBuildRows` LIVED HERE AND ARE DELETED. The
        /// ability builds moved to the fighter picker on 2026-09-02, on 🧑's instruction:
        /// **"put loadout here, it makes no sense to be in profile"**, with a crop of CHOOSE YOUR
        /// HERO, and *"there were button updates can u put loadouts in the choose ur hero screen
        /// too and shit"*. `ConvertedCharacterSelect.SlotView` and `CycleSlot` are the feature
        /// now; `BuildTabColumn` carries the argument and `docs/TODO.md` § 122.5 is the entry.
        ///
        /// ⚠️⚠️ NOTHING IN THE CORE MOVED AND NOTHING WAS LOST, WHICH IS THE CLAIM WORTH BEING
        /// ABLE TO CHECK. `HeroLoadoutRules`, `HeroBuildRules`, every variant, every unlock
        /// challenge and every counter are untouched in
        /// `Packages/com.tumbangpreso.core/Runtime/HeroLoadout.cs`, and the new caller reads and
        /// writes through the same four methods this one did: `VariantsFor`, `RowFor`, `Equipped`
        /// and `IsUnlocked`. **Only the screen changed.**
        ///
        /// ⚠️ THE TWO THINGS THAT DID NOT SURVIVE ARE NAMED RATHER THAN LEFT TO BE NOTICED:
        ///
        ///   * **The hero stepper.** It existed so this tab could ask "which hero?" — a question
        ///     the picker has already answered by construction, because the hero on screen IS the
        ///     selection. That is the whole reason the move shortens the journey from five presses
        ///     to two (`ConvertedMatchSetup.OpenLoadout`).
        ///   * **`_loadoutViews`, the browse cursor.** A stepper needs somewhere to hold "the
        ///     variant I am looking at but have not equipped"; a two-state toggle on the ability
        ///     row itself has no cursor, so what is drawn is always what is equipped.
        ///     `ConvertedCharacterSelect.SlotView`'s header carries that argument, and the honest
        ///     consequence is that a locked variant can no longer be browsed — it is NAMED on the
        ///     row with its challenge and its running count instead, which is strictly more
        ///     information in one fewer press.
        ///
        /// ⚠️ AND THE CLASSIC BRANCH WENT WITH IT, correctly. This tab needed a sentence
        /// explaining that Classic has no kit (`docs/VISION.md` § 1.1) because a tab that vanishes
        /// in one mode reads as a lost feature. The picker has no such problem: in Classic it
        /// draws trait meters instead of ability rows and always has, so there is nothing to
        /// explain away.
        /// </summary>
        /// <summary>
        /// The achievement shelf.
        ///
        /// ⚠️⚠️ ONE FACT PER COLUMN. The version this replaces printed the progress in
        /// the VALUE column and then again inside the hint with the reward appended in brackets:
        /// `12/30` on the right and `Win a match  (12 / 30 - Kalye title)` underneath. `UiRows`'
        /// whole design is a label, a value and one grey line of explanation; a row that says the
        /// same number twice is the *"its so messy and ugly"* screen (`docs/TODO.md` 94.7)
        /// reassembling itself one row at a time.
        ///
        /// ⚠️ THE SUMMARY IS ON THE SECTION HEADER, so a player who never opens the group
        /// still learns how many they have. A collapsed group that says nothing is a group nobody
        /// opens.
        ///
        /// ⚠️ THE TIER IS A COLOUR, NOT A PREFIX. `[BRONZE] ` in front of every title pushes
        /// the actual name nine characters right and makes thirty rows start with the same word.
        /// Gold is amber, silver is the highlight, bronze is plain cream, which is the escalation
        /// `docs/Art_Direction.md` 9.1 already describes for the badge art.
        /// </summary>
        private void BuildAchievementsRows(PlayerProfile profile)
        {
            int earned = 0;
            foreach (var entry in AchievementRules.Catalog)
                if (AchievementRules.IsUnlocked(entry, profile)) earned++;

            if (!Group("Achievements",
                       // ⚠️ SHORT FOR THE SAME REASON THE BUILDS SUBTITLE IS. This group was
                       // unreachable on a fresh account too, so its header had never been
                       // measured at 720p either. A `Section` subtitle overflows silently.
                       earned + " of " + AchievementRules.Catalog.Count
                       + " earned. None of them change a number.", false))
                return;

            foreach (var tier in new[] { AchievementTier.Bronze, AchievementTier.Silver,
                                         AchievementTier.Gold })
            {
                foreach (var entry in AchievementRules.Tier(tier))
                {
                    int progress = AchievementRules.ProgressFor(entry, profile);
                    bool unlocked = AchievementRules.IsUnlocked(entry, profile);

                    Color colour = !unlocked ? UiTheme.CreamMuted
                                 : tier == AchievementTier.Gold ? UiTheme.Amber
                                 : tier == AchievementTier.Silver ? UiTheme.Highlight
                                 : UiTheme.Cream;

                    UiRows.ValueRow(_list, entry.Title,
                        unlocked ? "EARNED" : progress + " / " + entry.TargetCount,
                        entry.Description, colour);
                }
            }
        }

        /// <summary>
        /// ⚠️⚠️ ONE CARD, ONE SENTENCE, ONE ROUTE OUT. The old page answered an empty career with
        /// three different sentences in three places ("No matches recorded yet", "No matches on
        /// this account yet", "No matches played yet. Finish one and it lands here.") and then
        /// drew fifteen rows of zeroes underneath them. An empty state is an invitation.
        /// </summary>
        /// <summary>
        /// The career tab with nothing in it yet.
        ///
        /// ⚠️⚠️ IT STILL DRAWS THE ACHIEVEMENT SHELF, AND IT USED TO `return` BEFORE IT.
        /// 🧑 2026-09-01: *"make sure phase 1-10 already exist in the game and can be accessed
        /// thru ui and hud"*. It did exist, and on a fresh account it **was not reachable by any
        /// route at all**: `BuildCareerTab` bailed to this method the moment `totals.Matches == 0`,
        /// and the group is built after that line.
        ///
        /// ⚠️ THE ABILITY BUILDS LEFT THIS METHOD AND THIS TAB ENTIRELY on the same day, for a
        /// harder version of the same complaint: reachable is not findable. They are the LOADOUT
        /// tab now. See `BuildTabBar`.
        ///
        /// **Both are things you do BEFORE your first match, not after it.** An ability build is a
        /// choice a player makes while learning a hero, and the achievement shelf is the one
        /// surface that says what there is to do. Hiding them until a match has been finished
        /// hides them from exactly the player they are for. § 114.12.
        ///
        /// ⚠️ THE MODE DROPDOWN COMES WITH THEM, because Ability builds is Hero Strike only
        /// (`VISION.md` § 1.1: Classic has no kit) and the dropdown otherwise only exists inside
        /// the Overview group, which is not built here. Without it a Classic-mode career tab would
        /// show an achievement shelf and silently no builds, which reads as a missing feature
        /// rather than as a mode that does not have one.
        /// </summary>
        private void EmptyCareer(PlayerProfile profile = null)
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

            if (profile != null)
            {
                UiRows.Gap(_list, 32.0f);

                UiRows.DropdownRow(_list, "Mode", new[] { "CLASSIC", "HERO STRIKE" },
                    _mode == GameMode.HeroStrike ? 1 : 0,
                    v =>
                    {
                        _mode = v == 1 ? GameMode.HeroStrike : GameMode.Classic;
                        Show(Tab.Career);
                    });

                BuildAchievementsRows(profile);
            }

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
            BuildFindFriendRows(social);
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

        private void BuildFindFriendRows(Net.SocialStore social)
        {
            if (!Group("Find a friend", "Add somebody by their exact NAME#TAG.", false)) return;

            var handle = UiRows.FieldRow(_list, "Name and tag", "Maria#4417",
                                         AccountRules.HandleMax,
                                         "The four digits are on their profile and account card.");
            UiRows.ButtonRow(_list, "", "SEND REQUEST", () =>
            {
                social?.RequestHandle(handle != null ? handle.text : "");
                MenuSfx.Click();
            }, "They receive a request. Nothing is added until they accept.",
               "WoodPrimaryButton");

            if (!string.IsNullOrEmpty(social?.SearchStatus))
                UiRows.ValueRow(_list, "Search", social.SearchStatus, "");
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

            // ⚠⚠ THE GOOGLE ROW IS DRAWN ONLY IN A BUILD THAT HAS A CLIENT ID, AND IT IS ONE
            // ROW RATHER THAN A GROUP. 🧑 2026-09-01: *"can we add some sort of authentication too?
            // like an option to sign inn with google acct or connect google acct"*. It sits with
            // the username row because they answer the same question, *"how do I get this career
            // onto another machine"*, and a player only has to do ONE of them. A separate
            // collapsed group would have implied a second, unrelated chore.
            //
            // ⚠️ THE CONNECTED STATE IS NOT A BUTTON. There is nothing useful to press: the game
            // has no reason to unlink and offering it would be a destructive control with no
            // recovery, on a screen whose own DANGER group is closed and two presses deep.
            if (account.GoogleAvailable)
            {
                if (account.HasGoogle)
                    UiRows.ValueRow(_list, "Google", "CONNECTED",
                        "Sign in with it on another machine to bring this career with you.",
                        UiTheme.MenuGreen);
                else
                    UiRows.ButtonRow(_list, "Google account", "CONNECT", OpenSignInForGoogle,
                        "One press, in your browser. Keeps everything you have already played.");
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

            // ⚠️⚠️ THE SENTENCE IS SHORTER BECAUSE THE PAGE IS NARROWER, AND THIS IS THE ONE
            // REGRESSION THE TAB COLUMN CAUSED. `PlayerHubLayoutProbe`, at 16:9 720p:
            // *"'Label' needs 575 units for "Deleting removes the account, the profile and every
            // match on the server." and was given 567"*. A shut group's summary lands in
            // `UiRows.ValueColumn`, which is a FRACTION of the row, so moving the navigation into
            // a 420-unit rail took about a fifth off every row and this was the one sentence with
            // less than a fifth of slack in it.
            //
            // ⚠️ IT IS THE COPY THAT GIVES RATHER THAN THE LAYOUT, and that is the cheaper trade
            // here: the heading already says **Danger** and the row under it already says
            // *Delete this account*, so the word "Deleting" was the third statement of the same
            // fact in three lines. `MenuKit.Fit` cannot rescue it (it stops at the 18-unit floor
            // and this is already 18) and wrapping would draw the second line over the rule.
            if (Group("Danger",
                      "Removes the account, the profile and every match on the server.",
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

        /// <summary>
        /// ⚠️ IT OPENS THE SIGN-IN SCREEN ON ITS CREATE TAB RATHER THAN STARTING THE BROWSER
        /// FLOW FROM HERE. The two verbs (connect this account, or move to another) live on that
        /// screen with a heading that says which one is about to happen, and a second entry point
        /// that silently picked one of them would be the same word meaning two things, which is
        /// the confusion `SignInScreen.SetMode` was written to remove. `CLAUDE.md` § 6.3: fix the
        /// door or move it, never grow a second one.
        /// </summary>
        private void OpenSignInForGoogle()
        {
            _notice = "";
            _signIn.OpenForUpgrade();
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
