using System;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Which arrangement the setup screen draws.
    ///
    /// ⚠️⚠️ `Classic` IS KEPT AND IT IS NOT DEAD CODE. 🧑 2026-08-28: *"dont delete old huds and ui
    /// tho keep them incase ur shit turns ugly"*. It is the authored converted layout exactly as
    /// it shipped, and switching back is this one enum rather than a revert, because everything
    /// `Street` does is a repositioning applied at runtime to the SAME nodes. No node is created
    /// that `Classic` needs, none is destroyed, and none is renamed.
    /// </summary>
    public enum LobbyStyle
    {
        /// <summary>Two centred wooden columns, as converted from the .tscn.</summary>
        Classic,

        /// <summary>The room is the picture: two paper rails, and nothing in between.</summary>
        Street,
    }

    /// <summary>
    /// The three things a player can be doing on this screen.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE 🧑 FOUND A REDUNDANCY NOBODY ELSE HAD: **"dont quick match and start
    /// match do the same thing? kinda confusing no?"**, and then the fix in his own words: *"maybe
    /// for lobby separate it into ranked and custom or other shit"*, *"maybe if ull join other
    /// server or use lan thats custom"*, *"you know use other games as reference"*.
    ///
    /// **He is right and the old screen had two primaries.** START MATCH loaded an arena with
    /// whoever was in the four seats; QUICK MATCH joined a matchmaking queue that would find a room
    /// and load an arena. Both said "a match starts now" and they sat 400 units apart in the same
    /// rail, so the player had to work out which one they meant every single time.
    /// `game-ui-design` puts POSITION first among the ordering tools and there is no position that
    /// fixes two controls with the same verb.
    ///
    /// ⚠️ THE MECHANISM COMES FROM THE GAMES `docs/TODO.md` § 118.3 ALREADY NAMES, AND IT IS THE
    /// SAME ONE IN ALL OF THEM: **one primary verb, and the MODE chosen beside it.** Rocket
    /// League's home screen is PLAY over a list of Casual / Competitive / Private; Overwatch 2 has
    /// one enormous button whose LABEL changes with the mode selector above it; Valorant puts a
    /// mode dropdown next to one START. None of them ships two buttons that both start a game.
    ///
    /// ⚠️⚠️ AND THE THREE ARE NOT ONE SCREEN THREE TIMES. 🧑, before any of this was built: *"make
    /// custom and ranked ladder shit diff dont jsut copy paste, bcz ranked laddder dont need join
    /// code"*, and *"make it as well na u cant queue with a friend in ranked ladder or smth"*. Each
    /// mode owns a different right-hand column, a different settings control and a different
    /// primary label, because each one genuinely has different content:
    ///
    /// | Mode | The one thing | The primary | The right column | Settings |
    /// |---|---|---|---|---|
    /// | `Practice` | play now, alone | START PRACTICE | nothing at all | open: map, mode, bots, rules |
    /// | `Ranked` | climb | FIND A RANKED MATCH | your TIER and the party rule | locked: the ladder fixes them |
    /// | `Custom` | get friends in | START MATCH | the room code, JOIN, CHAT | open |
    ///
    /// ⚠️ THE RANKED RULES ARE THE CORE'S, NOT THIS FILE'S. `PartyRules.CanQueue` already refuses a
    /// full stack in ranked (`MaxRankedSize` is three) and already refuses a party member who is
    /// not signed in, and `RatingRules` already owns the five tiers. **Nothing about ranked is
    /// invented here**; what changes is that it finally has a door, because until this pass
    /// `QueueCard` hard-coded `QueueStake.Casual` and **no player could ever reach the ladder at
    /// all.** `docs/TODO.md` § 119.8.
    /// </summary>
    public enum LobbyMode
    {
        /// <summary>Alone against bots. No transport, no account, no network.</summary>
        Practice,

        /// <summary>The seasonal ladder. Matchmade, solo or a party of three, signed in.</summary>
        Ranked,

        /// <summary>Your own room: a code, a LAN address, a browser, a chat.</summary>
        Custom,
    }

    /// <summary>
    /// The lobby, composed as ONE ROOM BETWEEN TWO RAILS.
    ///
    /// 🧑 2026-09-01: *"redesign teh whole ass UI (dont touch the camera and shit tho)"*, *"u can
    /// change placemenet of everything too"*, **"ur goal is to make it inntuitive and easy for user
    /// to traverse and calming. I DONT WANT it to be overwhelming for htem"**.
    ///
    /// -----------------------------------------------------------------------------------------
    /// ⚠️⚠️ THE COMPOSITION, AND WHY IT IS STRUCTURAL RATHER THAN A REPAINT
    /// -----------------------------------------------------------------------------------------
    ///
    /// `docs/TODO.md` § 118.1 row 2 measured the old arrangement exactly: **four corners and a
    /// hole.** The tab row ended at y≈100 and MATCH SETTINGS began at y≈780, so the left side of
    /// the screen was 680 units of nothing and the right side 475. Six plates floated in four
    /// corners, each with its own width and its own edge, and the middle band, the only part of the
    /// screen with anything worth looking at in it, was framed by all six.
    ///
    /// **The answer is two full-width rails**, which is the mechanism § 118.3 credits to Rocket
    /// League and Overwatch and which Fall Guys uses for this exact screen:
    ///
    /// | Band | Height | What is in it |
    /// |---|---|---|
    /// | TOP RAIL | <see cref="TopRailHeight"/> | BACK, the three mode tabs, your name, the door to your profile |
    /// | THE ROOM | everything left | the cast and four seat plates. **No chrome, ever.** |
    /// | BOTTOM RAIL | <see cref="BottomRailHeight"/> | who you are playing, the one action, and whatever the mode needs |
    ///
    /// ⚠️⚠️ EVERY DRAWER OPENS UPWARD OUT OF THE RAIL AND IS PARENTED TO THE COLUMN THAT OPENED
    /// IT. The queue card, the chat and the settings body used to be plates anchored to canvas
    /// corners, so each had to be positioned against every other one by arithmetic (`StackRight`,
    /// which is deleted) and each read as an unrelated box.
    ///
    /// -----------------------------------------------------------------------------------------
    /// ⚠️⚠️ THE COLOUR HIERARCHY, WHICH IS 🧑'S OWN CORRECTION AND IS NOT "CREAM EVERYWHERE"
    /// -----------------------------------------------------------------------------------------
    ///
    /// After the first paper build: *"u can also still use the brown color, i think it will look
    /// good with this, as long as u balance which one is brown and which one is this color bcz
    /// start match lowk looks good"*, then *"figure out visual hierarchy and shit onn where brown
    /// can be used"*, and, of the one amber thing left: **"this yellow dont look good withh creme
    /// too btw"**.
    ///
    /// **So brown is not removed, it is PROMOTED, and amber leaves this screen entirely.** Brown
    /// used to be the field, which is why the screen and the street were the same colour; it is the
    /// FIGURE now:
    ///
    /// | Layer | Colour | Why |
    /// |---|---|---|
    /// | the field: rails, sheets, drawers | cream `f4ecdd` | the one pair of axes Eskinita does not occupy |
    /// | recesses: rows, fields, values | sand `efdabe` | the same paper under less light |
    /// | **the verb**: the one action | **his authored brown slab** | the heaviest object in the frame, so the eye lands on it first |
    /// | **where you are**: the live tab | **wood-dark pill, cream lettering** | a 10:1 inversion, with no accent spent |
    /// | **the one fact**: the room code | **wood plaque, cream lettering** | `ffba00` on `f4ecdd` is 1.7:1, and he rejected it by eye |
    /// | type | warm ink `3b2415`, soft `7a5c40` | 12.1:1 and 5.2:1 on cream |
    ///
    /// ⚠️ IT STILL MOVES WHAT IS ALREADY THERE RATHER THAN REBUILDING IT. `ConvertedScreen` finds
    /// every control by the name Godot gave it and logs an error on a miss, so a redesign that
    /// rebuilt this screen would have to reproduce twenty exact names or break the wiring silently.
    /// `BackButton`, `StartButton`, `PrimaryButton`, `SeatButton0..3`, `CharacterButton`,
    /// `MapValueLabel` and the rest are REPARENTED, keeping their names, their handlers and their
    /// `Button` components. `docs/TODO.md` § 119.3 is the full inventory and `PaperPurityProbe` is
    /// the gate on it.
    /// </summary>
    public static class LobbyChrome
    {
        /// <summary>
        /// The default, and the only place it is decided.
        ///
        /// ⚠️ A FIELD RATHER THAN A CONST so a probe can photograph both without a rebuild, and so
        /// reverting is one assignment. `LobbyStyleProbe` asserts that every name the screen
        /// reaches for still resolves under both.
        /// </summary>
        public static LobbyStyle Style = LobbyStyle.Street;

        // -------------------------------------------------------------------------------------
        // ⚠️⚠️ THE HARMONY SET, AND EVERY NUMBER IN IT IS SIZED AGAINST ITS OWN CONTENT.
        //
        // 🧑 2026-09-01, with a crop of the fighter column: **"be aware of tightness and empty
        // space as well this looks ugly bcz of big ass empty sopace"**. That column was 400 units
        // wide around a 154-unit name, so `DANTE` sat at the far left and its chevron at the far
        // right with 200 units of bare cream between them. **A control is as wide as what is in
        // it**, and a row whose two ends are pinned to opposite edges of a box that is wider than
        // its content is a row with a hole in it by construction.
        //
        // This is `CLAUDE.md` § 6.2c question 1 (*what is this size measured against?*) and it is
        // the same fault the harmony block of the old file recorded from the other direction: back
        // then the rail was too wide because a caption column was, and 100 units came out of the
        // caption rather than out of the type.
        // -------------------------------------------------------------------------------------

        /// <summary>The margin every edge-anchored thing uses, on all four sides.</summary>
        private const float EdgeMargin = 40.0f;

        /// <summary>
        /// The top rail, added up: BACK, the three tabs, the name field, the profile door, and the
        /// gutters between them.
        ///
        /// ⚠️ IT IS ARITHMETIC AND NOT A FRACTION. `PaperKit.Pad` 18 + `BackWidth` 132 +
        /// `PaperKit.Gap` + three `TabWidth` with two gaps + one gap + `ProfileWidth`, plus the
        /// padding and the sheet\'s own corner radius at each end.
        /// `CLAUDE.md` § 6.2c question 1: a percentage of the window is
        /// not a size, and `AspectSafeCanvas` scales on the SHORT axis, so one fraction is two very
        /// different widths at two aspect ratios.
        /// </summary>
        private const float TopRailWidth =
            (PaperKit.Pad * 2.0f) + 16.0f + BackWidth + PaperKit.Gap
            + (TabWidth * 3.0f) + (PaperKit.Gap * 2.0f)
            + PaperKit.Gap + ProfileWidth;

        /// <summary>The bottom rail, added up: the three columns and their gutters.</summary>
        private const float BottomRailWidth =
            (PaperKit.Pad * 2.0f) + FighterColumnWidth + PaperKit.Pad + ActionWidth
            + PaperKit.Pad + RoomColumnWidth;

        /// <summary>
        /// The top rail: identity and navigation.
        ///
        /// ⚠️ 68 IS ITS CONTENT ADDED UP: a <see cref="PaperKit.ChipHeight"/> 40 control plus
        /// `PaperKit.Pad` 14 above and below, rounded to a multiple of 4 so `PaperSkin` does not
        /// requantise it. 🧑, of the 84-unit version: *"its still so big too, i wanted it to be
        /// tighter"*.
        /// </summary>
        private const float TopRailHeight = 68.0f;

        /// <summary>
        /// The bottom rail: the match.
        ///
        /// ⚠️ 184 IS THE TALLEST OF THE THREE COLUMNS PLUS THE PADDING, AND THE TALLEST IS THE
        /// CENTRE: the settings chip at <see cref="SettingsChipHeight"/> 44, one `PaperKit.Gap` 10
        /// and the primary at <see cref="ActionHeight"/> 96, which is 150, plus `PaperKit.Pad` 14
        /// either side and `PaperCraft.Drop` 6 for the shadow the rail draws inside its own bottom
        /// edge.
        ///
        /// ⚠️⚠️ IT WENT 192, 168, 184, AND THE TWO MOVES ARE TWO DIFFERENT NOTES FROM 🧑. First
        /// *"its still so big too, i wanted it to be tighter and overhauled"*, which took the
        /// PADDING and the GAPS down and shrank every column with them; then, of the result,
        /// **"make taht start match bigger, i was lowkey okay wiht it earlier"**. Those are not
        /// contradictory: what he wants tighter is the CHROME and what he wants bigger is the ONE
        /// ACTION, and 96 against a 44-unit chip is a stronger ratio than 88 against 56 was. The
        /// rail is 8 units shorter than it started and its primary is 8 units taller.
        ///
        /// ⚠️⚠️ AND IT IS 17.8 PER CENT OF A 1080 SCREEN, WHICH IS THE BUDGET THIS COSTS THE ROOM.
        /// With the top rail that is 28 per cent of the frame spent on chrome, against about 34 in
        /// the old arrangement, and the difference is that none of it is in the middle any more.
        /// The cast's feet sit at about y=760 in `Logs/shots-runtime/Lobby-v51.png` and this rail's
        /// top edge is at y=860, so nothing on it crosses a body.
        /// </summary>
        private const float BottomRailHeight = 184.0f;

        /// <summary>
        /// The left column of the bottom rail: who you are playing.
        ///
        /// ⚠️ 340 IS THE WIDEST THING IN IT PLUS ITS GUTTERS, NOT A THIRD OF THE RAIL. The longest
        /// roster name is `LOLA PACING`, about 154 units at <see cref="PaperKit.Title"/>; the
        /// longest loadout line is `Decades Tuna · Tsinelas`, about 176 at
        /// <see cref="PaperKit.Caption"/>. Plus `PaperKit.Pad` in from the left and a 34-unit
        /// chevron gutter on the right, that is 228, and 340 leaves it room to breathe without the
        /// hole 🧑 photographed at 400.
        /// </summary>
        private const float FighterColumnWidth = 320.0f;

        /// <summary>
        /// The right column of the bottom rail, whatever the mode puts in it.
        ///
        /// ⚠️ 420 IS THE WIDEST CONTENT ANY MODE PUTS THERE, WHICH IS CUSTOM'S CHIP ROW: `JOIN A
        /// GAME` is about 118 units at <see cref="PaperKit.Body"/> and gets 186. The ladder's tier
        /// plate and Practice's nothing both fit inside it.
        /// </summary>
        private const float RoomColumnWidth = 380.0f;

        /// <summary>
        /// How wide the primary action is allowed to get.
        ///
        /// ⚠️⚠️ A CAP, BECAUSE THE CENTRE COLUMN IS FLEXIBLE AND THE BUTTON IS 🧑'S OWN TEXTURE. At
        /// 1920 the centre column is about 1000 units wide, and `BUTTON LONG.png` is 818x135:
        /// stretched to 1000 at 88 tall it is drawn at half its authored aspect and the chamfered
        /// ends smear. 500 is a little over the authored width, which is the widest it can be drawn
        /// and still look like the object it is a photograph of.
        /// </summary>
        private const float ActionWidth = 520.0f;

        /// <summary>START MATCH / FIND A RANKED MATCH / START PRACTICE. The one control on this
        /// screen that ends the screen.</summary>
        private const float ActionHeight = 96.0f;

        /// <summary>The two-line MATCH SETTINGS chip: its name over the settings it summarises.
        /// </summary>
        private const float SettingsChipHeight = 44.0f;

        /// <summary>The FIGHTER row: a name over a loadout, so two lines.</summary>
        private const float FighterRowHeight = 54.0f;

        /// <summary>The SKILLS row: one line, and its caption BESIDE the value rather than at the
        /// far end of the row. ⚠️ It is a DIFFERENT SHAPE from the row above it on purpose
        /// (`docs/TODO.md` § 118.1 row 4) and its two strings are ADJACENT rather than pinned to
        /// opposite edges, which is 🧑's *"big ass empty sopace"*.</summary>
        private const float SkillsRowHeight = 40.0f;

        /// <summary>The room plaque, and the height its 40-unit code needs with a caption over it.
        /// </summary>
        private const float RoomSignHeight = 62.0f;

        /// <summary>
        /// How tall the opened settings drawer is.
        ///
        /// ⚠️ THE CONTENT ADDED UP: four <see cref="SettingsRowHeight"/> rows, three 6-unit gaps,
        /// the map's detail box, `PaperKit.Pad` either side and `PaperCraft.Drop` for the shadow
        /// the sheet draws inside its own bottom edge. A drawer sized by eye is a drawer whose last
        /// row is outside it the first time anybody adds one, which this file has recorded twice.
        /// </summary>
        private const float SettingsBodyHeight =
            (SettingsRowHeight * 4.0f) + 18.0f + SettingsDetailHeight + (PaperKit.Pad * 2.0f)
            + PaperCraft.Drop;

        private const float SettingsRowHeight = 56.0f;
        private const float SettingsCaptionWidth = 96.0f;
        private const float SettingsArrowSize = 42.0f;
        private const float SettingsDetailHeight = 56.0f;

        /// <summary>
        /// How wide the value in a selector well actually is, for a caller that has to fit a string
        /// into one. ⚠️ Arithmetic off the drawer and not a guess; `LAST TSINELAS STANDING` needs
        /// it.
        /// </summary>
        public const float FormatValueWidth =
            SettingsDrawerWidth - (PaperKit.Pad * 2.0f) - SettingsCaptionWidth - 14.0f
            - (SettingsArrowSize * 2.0f) - 32.0f;

        /// <summary>The settings drawer is as wide as the primary it opens above, so the centre of
        /// the screen is one column rather than two things that happen to be near each other.
        /// </summary>
        private const float SettingsDrawerWidth = ActionWidth;

        internal const int ValueSize = PaperKit.Title;

        /// <summary>BACK, at the far left of the top rail. ⚠️ SMALL, and that is `docs/TODO.md`
        /// § 118.1 row 5: it used to share a band, a height family and a material with the tabs, and
        /// it is the one control on this screen that leaves.</summary>
        private const float BackWidth = 120.0f;

        /// <summary>One tab, sized against `PRACTICE` at <see cref="PaperKit.Body"/> with room for
        /// the widest of the three.</summary>
        private const float TabWidth = 172.0f;

        /// <summary>The name field and the profile door, at the far right of the top rail.
        /// ⚠️ 220 IS SIZED AGAINST THE LONGEST LABEL THE DOOR EVER CARRIES, `SECURE PROGRESS`,
        /// which `ConvertedMatchSetup.RefreshProfileDoor` swaps in for a guest. Sizing against
        /// `PROFILE` would have been sizing against the state nobody starts in.</summary>
        private const float ProfileWidth = 200.0f;

        /// <summary>How tall the gradient bands over the street are, as a fraction of the screen.
        /// </summary>
        private const float TopBandFraction = 0.20f;
        private const float BottomBandFraction = 0.26f;

        /// <summary>
        /// How dark each band gets at the screen edge.
        ///
        /// ⚠️⚠️ BOTH CAME DOWN BY A THIRD WHEN THE RAILS WENT CREAM, AND THAT IS `CLAUDE.md`
        /// § 6.2c QUESTION 3 ANSWERED HONESTLY. The old numbers (0.52 and 0.30) bought cream type
        /// its legibility over a bright road, because the type was drawn straight onto the wood and
        /// the wood straight onto the street. **Every word on this screen now sits on an opaque
        /// cream sheet**, so the only job left is separation. Anything stronger is dimming the
        /// cast, which is the one thing this arrangement exists to show.
        /// </summary>
        private const float TopBandAlpha = 0.34f;
        private const float BottomBandAlpha = 0.20f;

        /// <summary>
        /// Applies the arrangement. Safe to call once per screen load and nowhere else.
        /// </summary>
        /// <param name="root">The screen's own transform, already indexed by the caller.</param>
        /// <param name="find">How to reach a node by its Godot name.</param>
        /// <param name="onMode">Raised with the chosen mode.</param>
        public static Parts Apply(Transform root, Func<string, Transform> find,
                                  bool isLobby, Action<LobbyMode> onMode)
        {
            if (Style != LobbyStyle.Street) return null;
            if (root == null || find == null) return null;

            SoftenScrim(root, find);
            HideBanner(find);

            var parts = new Parts();

            // ⚠️ THE BANNER'S PARENT IS THE CANVAS ROOT, and every piece below hangs off it. It is
            // resolved once here rather than by each builder, because the version of this file
            // before 2026-09-01 asked for it four separate times and one of those asked before
            // `HideBanner` had run.
            var banner = find("Banner");
            Transform canvasRoot = banner != null ? banner.parent : root;

            var left = find("LeftColumn");
            var right = find("RightColumn");
            LooseTheColumns(find);

            // ⚠️ A SCREEN ENTERED FROM PLAY IS `Custom` AND A SCREEN ENTERED FROM PRACTICE IS
            // `Practice`. `SceneFlow.Networked` is the one bit the rest of the game carries, so
            // ranked has to be chosen ON this screen rather than arrived at; that is correct, and
            // it is what makes the ladder a destination rather than a state somebody can be
            // dropped into without noticing.
            var mode = isLobby ? LobbyMode.Custom : LobbyMode.Practice;

            BuildTopRail(canvasRoot, find, left, onMode, parts);
            BuildBottomRail(canvasRoot, find, left, right, parts);

            // ⚠️⚠️ LAST, AND NOT INSIDE `BuildTabs`, BECAUSE THE RIGHT COLUMN DOES NOT EXIST YET
            // WHEN THE TABS ARE BUILT. `SetMode` is what swaps the whole right-hand side, and
            // `ConvertedMatchSetup.SelectMode` only runs when the player CHANGES tab: a screen
            // entered as practice would otherwise ship with a room code and a chat describing a
            // room that does not exist.
            parts.SetMode(mode);

            return parts;
        }

        /// <summary>
        /// Stops the authored two-column layout driving anything.
        ///
        /// ⚠️⚠️ THE GROUP IS DISABLED RATHER THAN THE COLUMNS BEING DELETED, AND THAT IS
        /// `LobbyStyle.Classic` STILL WORKING. Everything this file does is a repositioning of
        /// nodes that exist in `MatchSetup.unity`; deleting the containers would make the enum a lie
        /// and would make `SceneScriptCheck` refuse a scene whose script expects them.
        /// </summary>
        private static void LooseTheColumns(Func<string, Transform> find)
        {
            var columns = find("Columns");
            if (columns == null) return;

            var group = columns.GetComponent<LayoutGroup>();
            if (group != null) group.enabled = false;

            if (columns is RectTransform rect) MenuKit.Stretch(rect, 0.0f);
        }

        // -------------------------------------------------------------------------------------
        // THE TOP RAIL: who you are, where you are, and how you get out.
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// One cream bar across the top, holding four things and nothing else.
        ///
        /// ⚠️⚠️ THE PROFILE DOOR IS AT THE FAR RIGHT OF THIS BAR ON 🧑'S INSTRUCTION: *"i tink te
        /// profile screen should be more up instead of being below character select"*. It used to
        /// be the last row of a five-row card in the top-right corner, under the room code, the
        /// name field, the character row and the build row, which is the bottom of a stack whose
        /// top is the thing you look at. `CLAUDE.md` § 6.3 and `docs/TODO.md` § 96: the hub had
        /// exactly one door and the person who commissioned it never found it.
        /// </summary>
        private static void BuildTopRail(Transform canvasRoot, Func<string, Transform> find,
                                         Transform leftColumn, Action<LobbyMode> onMode,
                                         Parts parts)
        {
            var rail = PaperKit.Sheet(canvasRoot, "LobbyTopRail");
            rail.raycastTarget = true;

            // ⚠️⚠️ AN ISLAND, NOT A FULL-BLEED BAR, AND 🧑 ASKED FOR THIS BY NAME: **"be aware
            // of tightness and empty space as well this looks ugly bcz of big ass empty sopace"**.
            // Stretched edge to edge the rail was about 1800 units around 1140 of content, so it
            // carried 660 units of bare cream in two gaps that no control could ever fill. A bar
            // sized to what is IN it reads as a designed object; a bar sized to the window reads as
            // a browser toolbar, and it costs the street two corners it does not need to lose.
            var rect = rail.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1.0f);
            rect.anchorMax = new Vector2(0.5f, 1.0f);
            rect.pivot = new Vector2(0.5f, 1.0f);
            rect.anchoredPosition = new Vector2(0.0f, -EdgeMargin);
            rect.sizeDelta = new Vector2(TopRailWidth, TopRailHeight);

            LiftBack(rail.transform, leftColumn);
            BuildTabs(rail.transform, onMode, parts);
            BuildProfileButton(rail.transform, parts);
            LiftVersionStamp(canvasRoot, rail.transform);
        }

        /// <summary>
        /// BACK, as a small pill pinned to the left end of the top rail.
        ///
        /// ⚠️ THE AUTHORED NODE IS MOVED, NOT REPLACED, so `BackButton` still resolves and still
        /// carries whatever `ConvertedMatchSetup` wired to it. Its wooden skin is stripped and a
        /// paper one put on: `PaperSkin.Apply` destroys the `WoodSkin` and `PaperKit.Paperise`
        /// disables `GodotButton`, because both write the Image from `Update` and two of them on
        /// one node flicker between materials every frame.
        /// </summary>
        private static void LiftBack(Transform rail, Transform leftColumn)
        {
            var back = Descend(leftColumn, "BackButton") as RectTransform;
            if (back == null) return;

            back.SetParent(rail, false);

            back.anchorMin = new Vector2(0.0f, 0.5f);
            back.anchorMax = new Vector2(0.0f, 0.5f);
            back.pivot = new Vector2(0.0f, 0.5f);
            // ⚠️⚠️ THE INSET IS THE PADDING PLUS THE RAIL OWN CORNER, NOT THE PADDING. 🧑, with a
            // crop of the top rail: **"back is brokenn"**. `PaperCraft` cuts every sheet with an
            // 18-unit radius, so a chip placed `PaperKit.Pad` 14 in from the edge has its left end
            // and its halo sitting ON the curve, which reads as a control falling off the bar.
            // **The first control in a rounded container clears the RADIUS, not the padding.**
            back.anchoredPosition = new Vector2(PaperKit.Pad + 8.0f, 0.0f);
            back.sizeDelta = new Vector2(BackWidth, PaperKit.ChipHeight);

            var element = back.GetComponent<LayoutElement>();
            if (element != null) element.ignoreLayout = true;

            PaperKit.Paperise(back.gameObject, PaperCraft.Surface.Token);

            var label = back.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = "‹  BACK";
                label.name = "Label";
                label.fontSize = PaperKit.Body;
                label.color = UiTheme.PaperInk;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleCenter;
                MenuKit.Stretch(label.rectTransform, 0.0f);
                label.rectTransform.offsetMax = new Vector2(0.0f, -PaperCraft.Drop);
                MenuKit.Fit(label, BackWidth - 20.0f);
            }

            back.gameObject.AddComponent<PaperButton>();
            FocusRing.Attach(back.gameObject, 4.0f);

            var spacer = Descend(leftColumn, "Spacer");
            if (spacer != null) spacer.gameObject.SetActive(false);
        }

        /// <summary>
        /// PRACTICE, RANKED and CUSTOM, centred on the top rail.
        ///
        /// ⚠️⚠️ THE THREE ARE DISTINGUISHED BY SURFACE AND WEIGHT, NEVER BY HUE. `docs/TODO.md`
        /// § 118.4: a tab is a statement about where you already are and is not an action. The live
        /// tab is a wood-dark `PaperCraft.Surface.Live` pill with cream bold lettering and the other
        /// two are `Ghost` outlines with soft ink. **That is a value inversion of about 10:1**,
        /// which is what `Logs/shots-runtime/Lobby-v52.png` proved was needed: at `Token` against
        /// `Ghost` the pair were 4 per cent apart and unreadable at a glance.
        /// </summary>
        private static void BuildTabs(Transform rail, Action<LobbyMode> onMode, Parts parts)
        {
            var bar = new GameObject("LobbyTabBar", typeof(RectTransform));
            bar.transform.SetParent(rail, false);

            var barRect = (RectTransform)bar.transform;
            barRect.anchorMin = new Vector2(0.5f, 0.5f);
            barRect.anchorMax = new Vector2(0.5f, 0.5f);
            barRect.pivot = new Vector2(0.5f, 0.5f);

            // ⚠️ THE BAR SITS WHERE THE ARITHMETIC PUTS IT, NOT WHERE A NUDGE DOES. The rail is
            // sized to its content now (`TopRailWidth`), so the tabs occupy the exact middle of a
            // row whose left end is BACK and whose right end is the identity pair; the half of the
            // difference between those two blocks is what the bar has to lean by, and it is
            // computed rather than eyeballed.
            const float leftBlock = PaperKit.Pad + BackWidth + PaperKit.Gap;
            const float rightBlock = PaperKit.Gap + ProfileWidth + PaperKit.Pad + 8.0f;
            barRect.anchoredPosition = new Vector2((leftBlock - rightBlock) * 0.5f, 0.0f);
            barRect.sizeDelta = new Vector2((TabWidth * 3.0f) + (PaperKit.Gap * 2.0f),
                                            PaperKit.ChipHeight);

            var layout = bar.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = PaperKit.Gap;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleCenter;

            parts.Tabs[(int)LobbyMode.Practice] =
                PaperKit.Chip(bar.transform, "PracticeTab", "PRACTICE");
            parts.Tabs[(int)LobbyMode.Ranked] =
                PaperKit.Chip(bar.transform, "RankedTab", "RANKED");
            parts.Tabs[(int)LobbyMode.Custom] =
                PaperKit.Chip(bar.transform, "CustomTab", "CUSTOM");

            for (int i = 0; i < parts.Tabs.Length; i++)
            {
                var chosen = (LobbyMode)i;
                parts.Tabs[i].onClick.AddListener(() => onMode?.Invoke(chosen));
            }

            // ⚠️ KEPT FOR THE PROBES AND FOR EVERY CALLER THAT STILL THINKS IN TWO TABS.
            // `PaperPurityProbe` and `UiRuntimeShots` both press these by name.
            parts.Practice = parts.Tabs[(int)LobbyMode.Practice];
            parts.Multiplayer = parts.Tabs[(int)LobbyMode.Custom];
        }


        /// <summary>
        /// The door to `PlayerHub`, and the whole of the account system behind it.
        ///
        /// ⚠️ THE LABEL IS WRITTEN BY `ConvertedMatchSetup.RefreshProfileDoor` and changes with
        /// the account: a signed-in player is told their level and tier, and everybody else sees
        /// `ACCOUNT`. 🧑: **"can u replace secure progress to Account and allow to put thhe name
        /// there if not logged in, bcz offlinne mode is for torunnaments and shit"**. `SECURE
        /// PROGRESS` was a sales pitch on a navigation control, and it was the wrong door for the
        /// thing a player most often wants behind it, which is their own name.
        ///
        /// ⚠️⚠️ AND ITS LABEL IS INK, NEVER AMBER. `Logs/shots-runtime/Lobby-v52.png` shipped
        /// `SECURE YOUR PROGRESS` in amber at the top-right corner, where it was the loudest thing
        /// on the rail and directly competing with the primary action for the eye. `docs/TODO.md`
        /// § 117.3 is exactly this fault one control over.
        /// </summary>
        private static void BuildProfileButton(Transform rail, Parts parts)
        {
            var button = PaperKit.Chip(rail, "ProfileButton", "ACCOUNT");

            var rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(1.0f, 0.5f);
            rect.anchorMax = new Vector2(1.0f, 0.5f);
            rect.pivot = new Vector2(1.0f, 0.5f);
            rect.anchoredPosition = new Vector2(-(PaperKit.Pad + 8.0f), 0.0f);
            rect.sizeDelta = new Vector2(ProfileWidth, PaperKit.ChipHeight);

            var label = button.transform.Find("Label")?.GetComponent<Text>();
            if (label != null)
            {
                label.name = "ProfileValue";
                label.alignment = TextAnchor.MiddleCenter;
                parts.ProfileValue = label;
            }

            parts.ProfileButton = button;
        }

        /// <summary>
        /// Puts the build number on the rail instead of on the road.
        ///
        /// ⚠️ § 118.1 ROW 8: it was the one word on the screen sitting on nothing. It is found by
        /// COMPONENT rather than by name, because `VersionStamp` is attached in the authored scene
        /// and the node it is on has been renamed once already.
        ///
        /// ⚠️ UNDER `BACK`, NOT BESIDE IT. On `Logs/shots-runtime/Lobby-v52.png` it sat in the gap
        /// between BACK and the tab row, where it read as a second, broken label attached to the
        /// button. A build number belongs in a corner and the rail has one.
        /// </summary>
        private static void LiftVersionStamp(Transform canvasRoot, Transform rail)
        {
            var stamp = canvasRoot.GetComponentInChildren<VersionStamp>(true);
            if (stamp == null) return;

            var rect = stamp.transform as RectTransform;
            if (rect == null) return;

            // ⚠️⚠️ IT SITS UNDER THE RAIL, NOT ON IT. `Logs/shots-runtime/Lobby-v53.png` has
            // `v1.0.0` drawn straight through the word BACK, because the rail's bottom-left corner
            // and the BACK chip's vertical centre are the same 18 units of padding apart and the
            // chip is 44 tall. Hanging it below the rail costs nothing, cannot collide with a
            // control, and is where a build number belongs: the quietest corner of the screen.
            rect.SetParent(rail, false);
            rect.anchorMin = new Vector2(1.0f, 0.0f);
            rect.anchorMax = new Vector2(1.0f, 0.0f);
            rect.pivot = new Vector2(1.0f, 1.0f);
            rect.anchoredPosition = new Vector2(0.0f, -6.0f);
            rect.sizeDelta = new Vector2(160.0f, 18.0f);

            var text = stamp.GetComponent<Text>();
            if (text == null) return;

            text.fontSize = 13;
            text.color = new Color(UiTheme.Cream.r, UiTheme.Cream.g, UiTheme.Cream.b, 0.75f);
            text.alignment = TextAnchor.UpperRight;
            text.raycastTarget = false;

            // ⚠️ THE ONE OUTLINED STRING ON THE SCREEN, AND IT IS OUTLINED BECAUSE IT IS THE ONE
            // STRING NOT ON A SURFACE. `PaperKit.Ink`'s note is that an outline over an opaque
            // sheet is a smear; over a live street it is the only thing that makes cream type
            // readable, which is what every `GodotTheme` menu style was written for. A build
            // number cannot have a plate of its own without being more important than it is.
            var outline = text.GetComponent<GodotOutline>();
            if (outline == null) outline = text.gameObject.AddComponent<GodotOutline>();
            outline.enabled = true;
            outline.Radius = 2.0f;
            outline.OutlineColour = new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 0.7f);
        }

        // -------------------------------------------------------------------------------------
        // THE BOTTOM RAIL: the match.
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// One cream bar across the bottom, in three columns.
        ///
        /// ⚠️⚠️ THE THREE COLUMNS ARE THREE QUESTIONS AND NOT THREE GROUPS OF CONTROLS, WHICH IS
        /// THE DIFFERENCE BETWEEN A RAIL AND A TOOLBAR. Left: *who am I playing?* Centre: *what is
        /// this match, and how do I start it?* Right: *whatever this MODE needs and no more.*
        ///
        /// ⚠️⚠️ AND BOTH SIDE COLUMNS ALWAYS OCCUPY THEIR WIDTH, EVEN WHEN EMPTY. On
        /// `Logs/shots-runtime/LobbyPractice-v52.png` the right column was switched off and the
        /// flexible centre swallowed its 460 units, so the primary slid 230 units right of the
        /// screen's centre while the left column stayed put. **A rail whose contents move when a
        /// mode changes is a rail the player has to re-find**, so the column is emptied rather than
        /// removed.
        /// </summary>
        private static void BuildBottomRail(Transform canvasRoot, Func<string, Transform> find,
                                            Transform leftColumn, Transform rightColumn,
                                            Parts parts)
        {
            var rail = PaperKit.Sheet(canvasRoot, "LobbyBottomRail");
            rail.raycastTarget = true;

            // ⚠️ AN ISLAND, FOR THE REASON THE TOP RAIL IS ONE: its width is the columns that are
            // actually in it, so the gaps 🧑 photographed either side of START MATCH cannot exist.
            //
            // ⚠️⚠️ AND IT MEASURES ITSELF RATHER THAN TAKING A CONSTANT, WHICH IS WHAT MAKES
            // PRACTICE WORK. 🧑, of the practice tab: **"why is entire right side empty"**. That
            // mode has no third column at all, and a rail whose width was arithmetic kept the 380
            // units the mode column would have used. A `ContentSizeFitter` on the horizontal axis
            // means the rail is exactly as wide as whatever the mode put in it, and the whole
            // island re-centres when a column comes or goes. **One fewer number that can be wrong.**
            var rect = rail.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.0f);
            rect.anchorMax = new Vector2(0.5f, 0.0f);
            rect.pivot = new Vector2(0.5f, 0.0f);
            rect.anchoredPosition = new Vector2(0.0f, EdgeMargin);
            rect.sizeDelta = new Vector2(BottomRailWidth, BottomRailHeight);

            var fitter = rail.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var layout = rail.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset((int)PaperKit.Pad, (int)PaperKit.Pad,
                                            (int)PaperKit.Pad,
                                            (int)PaperKit.Pad + PaperCraft.Drop);
            layout.spacing = PaperKit.Pad;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleCenter;

            BuildFighterColumn(rail.transform, find, parts);
            BuildActionColumn(rail.transform, find, leftColumn, parts);
            BuildModeColumn(rail.transform, rightColumn, parts);
        }

        /// <summary>
        /// Left column: the two things about you that the match will actually use.
        ///
        /// ⚠️ THEY ARE TWO DIFFERENT SHAPES (`docs/TODO.md` § 118.1 row 4). The fighter is a
        /// two-line row with a chevron, because a character has a name and a kit; the build is a
        /// caption-and-value row whose two strings sit NEXT TO EACH OTHER on the left, because it
        /// is one fact and because pinning them to opposite edges is what 🧑 photographed as *"big
        /// ass empty sopace"*.
        /// </summary>
        private static void BuildFighterColumn(Transform rail, Func<string, Transform> find,
                                               Parts parts)
        {
            var column = new GameObject("FighterColumn", typeof(RectTransform));
            column.transform.SetParent(rail, false);

            var element = column.AddComponent<LayoutElement>();
            element.minWidth = FighterColumnWidth;
            element.preferredWidth = FighterColumnWidth;
            element.flexibleWidth = 0.0f;

            var layout = column.AddComponent<VerticalLayoutGroup>();
            layout.spacing = PaperKit.Gap;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            BuildCharacterRow(column.transform, find, parts);
            BuildSkillsRow(column.transform, parts);
        }

        /// <summary>
        /// The authored `CharacterButton`, moved and re-dressed as a two-line paper row.
        ///
        /// ⚠️ THE NODE IS THE AUTHORED ONE. 🧑 2026-08-28: *"I want it to lead to the same screen as
        /// before"*. `ConvertedMatchSetup.OpenCharacterSelect` is untouched and still reveals
        /// `CharacterSelectPanel` in place; this keeps the name, the `Button` and the handler and
        /// changes only the surface and the position.
        /// </summary>
        private static void BuildCharacterRow(Transform column, Func<string, Transform> find,
                                              Parts parts)
        {
            var node = find("CharacterButton") as RectTransform;
            if (node == null) return;

            var fighterRow = node.parent;
            node.SetParent(column, false);
            if (fighterRow != null && fighterRow.name == "FighterRow")
                fighterRow.gameObject.SetActive(false);

            var element = node.GetComponent<LayoutElement>();
            if (element == null) element = node.gameObject.AddComponent<LayoutElement>();
            element.minHeight = FighterRowHeight;
            element.preferredHeight = FighterRowHeight;
            element.flexibleHeight = 0.0f;
            element.minWidth = 0.0f;
            element.preferredWidth = -1.0f;
            element.flexibleWidth = 1.0f;

            PaperKit.Paperise(node.gameObject, PaperCraft.Surface.Tray);

            var authored = node.GetComponentInChildren<Text>(true);
            if (authored != null && authored.name == "Label") authored.gameObject.SetActive(false);

            // ⚠️⚠️ THE TWO LINES ARE CENTRED IN THE ROW, WHICH IS 🧑'S OWN FIX: **"make dante box
            // centered maybe that will fix it"**. He is right, and the reason it works is the same
            // reason it looked wrong: `DANTE` is about 154 units at <see cref="PaperKit.Title"/> in
            // a 320-unit column, so left-aligning it pins 130 units of bare cream to one side of
            // the row and the chevron to the other. Centred, the space is split and reads as
            // margin instead of as a hole. **Left alignment is for a list you scan down; a row with
            // one thing in it is a label.**
            var name = PaperKit.Ink(node, "", PaperKit.Title, TextAnchor.LowerCenter);
            name.name = "CharacterName";
            name.raycastTarget = false;
            name.fontStyle = FontStyle.Bold;
            name.rectTransform.anchorMin = new Vector2(0.0f, 0.44f);
            name.rectTransform.anchorMax = Vector2.one;
            name.rectTransform.offsetMin = new Vector2(34.0f, 0.0f);
            name.rectTransform.offsetMax = new Vector2(-34.0f, -4.0f);

            var loadout = PaperKit.Ink(node, "", PaperKit.Caption, TextAnchor.UpperCenter,
                                       soft: true);
            loadout.name = "CharacterLoadout";
            loadout.raycastTarget = false;
            loadout.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            loadout.rectTransform.anchorMax = new Vector2(1.0f, 0.44f);
            loadout.rectTransform.offsetMin = new Vector2(34.0f, PaperCraft.Drop);
            loadout.rectTransform.offsetMax = new Vector2(-34.0f, 0.0f);

            PaperKit.Chevron(node);

            node.gameObject.AddComponent<PaperButton>();
            FocusRing.Attach(node.gameObject, 3.0f);

            parts.CharacterName = name;
            parts.CharacterLoadout = loadout;
        }

        /// <summary>
        /// The door to the LOADOUT tab of the hub, and what is equipped on it.
        ///
        /// ⚠️⚠️ THE CAPTION AND THE VALUE ARE ADJACENT, NOT AT OPPOSITE ENDS. 🧑, with a crop of
        /// this row: *"be aware of tightness and empty space as well this looks ugly bcz of big ass
        /// empty sopace"*. `SKILLS` at the left edge and `Standard build` at the right edge of a
        /// 400-unit row leaves 150 units of bare cream between two strings that belong together. A
        /// caption is a label ON its value, so it sits next to it and the pair floats left as one
        /// object.
        ///
        /// ⚠️ HIDDEN IN CLASSIC, WITH ITS CAPTION. Classic has no skills, so `Parts.SetSkills(false,
        /// ...)` takes the whole row off the column rather than leaving a caption naming a control
        /// that is not there.
        /// </summary>
        private static void BuildSkillsRow(Transform column, Parts parts)
        {
            var go = new GameObject("LoadoutButton", typeof(RectTransform), typeof(Image),
                                    typeof(Button));
            go.transform.SetParent(column, false);

            PaperSkin.Apply(go, PaperCraft.Surface.Tray);

            var element = go.AddComponent<LayoutElement>();
            element.minHeight = SkillsRowHeight;
            element.preferredHeight = SkillsRowHeight;
            element.flexibleHeight = 0.0f;

            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = go.GetComponent<Image>();

            // ⚠️ THE CAPTION AND THE VALUE ARE ONE CENTRED PAIR, for the reason the row above
            // is centred: two strings pinned to opposite edges of a row wider than both is the
            // hole 🧑 photographed. The caption sits immediately left of the value and the pair
            // floats in the middle as one object.
            var caption = PaperKit.Ink(go.transform, "SKILLS", PaperKit.Caption,
                                       TextAnchor.MiddleRight, soft: true);
            caption.name = "LoadoutCaption";
            caption.raycastTarget = false;
            MenuKit.Stretch(caption.rectTransform, 0.0f);
            // ⚠️⚠️ THE CAPTION STOPS 10 UNITS SHORT OF THE MIDDLE AND THE VALUE STARTS AT IT.
            // `Logs/shots-runtime/Lobby-v55.png` reads `St̶a̶ndard Build` with `SKILLS` drawn
            // through it, because the caption's box ran to the middle and the value's began 46
            // units before it: a 46-unit overlap, and **two labels overlapping is silent in every
            // direction** (§ 102.4, rotated). Two boxes that share an edge cannot overlap by
            // construction, which is the only version of this that stays fixed.
            caption.rectTransform.offsetMin = new Vector2(24.0f, PaperCraft.Drop);
            caption.rectTransform.offsetMax =
                new Vector2(-((FighterColumnWidth * 0.5f) + 10.0f), 0.0f);

            var label = PaperKit.Ink(go.transform, "", PaperKit.Body, TextAnchor.MiddleLeft);
            label.name = "LoadoutValue";
            label.raycastTarget = false;
            MenuKit.Stretch(label.rectTransform, 0.0f);
            label.rectTransform.offsetMin =
                new Vector2((FighterColumnWidth * 0.5f) - 4.0f, PaperCraft.Drop);
            label.rectTransform.offsetMax = new Vector2(-34.0f, 0.0f);

            PaperKit.Chevron(go.transform);

            go.AddComponent<PaperButton>();
            FocusRing.Attach(go, 3.0f);

            parts.LoadoutButton = button;
            parts.LoadoutValue = label;
            parts.LoadoutCaption = caption.gameObject;
        }

        /// <summary>
        /// Centre column: what this match is, and the one button that starts it.
        ///
        /// ⚠️⚠️ THE SETTINGS SUMMARY IS THE SECOND LINE OF THE CHIP RATHER THAN A LABEL UNDER IT.
        /// The old arrangement had a 52-unit toggle, a 2-unit gap and a 22-unit caption as three
        /// separate children of a vertical group: three rectangles to say one thing, and 76 units of
        /// the rail's height. One two-line control is 56, it cannot drift away from the toggle it
        /// describes, and pressing the words you are reading is what opens the drawer that changes
        /// them.
        ///
        /// ⚠️⚠️ AND IN RANKED THAT CHIP IS REPLACED BY A LOCKED PLATE, NOT GREYED OUT. 🧑: *"make
        /// custom and ranked ladder shit diff dont jsut copy paste"*. A ladder fixes its own map,
        /// mode and rules, so an editable settings drawer there would be a control that cannot do
        /// anything; `CLAUDE.md` § 6.3 calls a control that looks pressable and is not the fault,
        /// and § 6.2 calls a greyed one indistinguishable from a broken one. A `Tray` with no
        /// chevron and no `PaperButton` is visibly a STATEMENT rather than a door.
        /// </summary>
        private static void BuildActionColumn(Transform rail, Func<string, Transform> find,
                                              Transform leftColumn, Parts parts)
        {
            var column = new GameObject("ActionColumn", typeof(RectTransform));
            column.transform.SetParent(rail, false);

            var element = column.AddComponent<LayoutElement>();
            // ⚠️ NO FLEX. The rail is sized to its content now (`BottomRailWidth`), so a
            // flexible centre would have nothing to flex into; leaving it flexible is what let the
            // column swallow the mode column's 420 units in practice and slide the primary 210
            // units off the screen's centre.
            element.minWidth = ActionWidth;
            element.preferredWidth = ActionWidth;
            element.flexibleWidth = 0.0f;

            var layout = column.AddComponent<VerticalLayoutGroup>();
            layout.spacing = PaperKit.Gap;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            var chip = BuildSettingsChip(column.transform, find, parts);
            BuildRankedRuleLine(column.transform, parts);
            BuildActionSlot(column.transform, find, leftColumn, parts);

            BuildSettingsDrawer(column.transform, find, chip, parts);
        }

        /// <summary>The two-line chip: MATCH SETTINGS over the three values it opens.</summary>
        private static Button BuildSettingsChip(Transform column, Func<string, Transform> find,
                                                Parts parts)
        {
            var go = new GameObject("SettingsDrawerToggle", typeof(RectTransform), typeof(Image),
                                    typeof(Button));
            go.transform.SetParent(column, false);

            PaperSkin.Apply(go, PaperCraft.Surface.Token);

            var element = go.AddComponent<LayoutElement>();
            element.minHeight = SettingsChipHeight;
            element.preferredHeight = SettingsChipHeight;
            element.flexibleHeight = 0.0f;
            element.minWidth = ActionWidth;
            element.preferredWidth = ActionWidth;
            element.flexibleWidth = 0.0f;

            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = go.GetComponent<Image>();

            var title = PaperKit.Ink(go.transform, "MATCH SETTINGS", PaperKit.Body,
                                     TextAnchor.LowerCenter);
            title.name = "Label";
            title.raycastTarget = false;
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.anchorMin = new Vector2(0.0f, 0.48f);
            title.rectTransform.anchorMax = Vector2.one;
            title.rectTransform.offsetMin = new Vector2(44.0f, 0.0f);
            title.rectTransform.offsetMax = new Vector2(-44.0f, -6.0f);

            var summary = PaperKit.Ink(go.transform, "", PaperKit.Caption, TextAnchor.UpperCenter,
                                       soft: true);
            summary.name = "SettingsSummary";
            summary.raycastTarget = false;
            summary.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            summary.rectTransform.anchorMax = new Vector2(1.0f, 0.48f);
            summary.rectTransform.offsetMin = new Vector2(44.0f, PaperCraft.Drop + 2.0f);
            summary.rectTransform.offsetMax = new Vector2(-44.0f, 0.0f);

            var caret = PaperKit.Ink(go.transform, "▾", PaperKit.Body, TextAnchor.MiddleRight,
                                     soft: true);
            caret.name = "DrawerChevron";
            caret.raycastTarget = false;
            MenuKit.Stretch(caret.rectTransform, 0.0f);
            caret.rectTransform.offsetMin = new Vector2(0.0f, PaperCraft.Drop);
            caret.rectTransform.offsetMax = new Vector2(-PaperKit.Pad, 0.0f);

            go.AddComponent<PaperButton>();
            FocusRing.Attach(go, 3.0f);

            parts.SettingsChip = go;
            parts.SettingsSummary = summary;
            parts.SettingsCaret = caret;

            // ⚠️ THE SUMMARY IS A VIEW OF THE THREE VALUE LABELS AND IS REWRITTEN FROM `Refresh`,
            // never composed once at build time. `Logs/shots-runtime/Lobby-v35.png` shipped
            // `ESKINITA · CAPTURE · NORMAL` on a Hero Strike lobby because the old version was
            // composed inside `Apply`, which runs before the screen's first `Refresh`, and
            // `CAPTURE` is a placeholder from a mode this game does not have.
            //
            // ⚠️ SENTENCE CASE, NOT CAPS. See `Sentence`: caps for verbs and names, sentence case
            // for anything the player merely reads.
            parts.RefreshSummary = () =>
            {
                if (summary == null) return;

                summary.text = $"{Sentence(Value(find, "MapValueLabel"))}   ·   " +
                               $"{Sentence(Value(find, "ModeValueLabel"))}   ·   " +
                               $"{Sentence(Value(find, "DifficultyValueLabel"))}";
                summary.fontSize = PaperKit.Caption;
                MenuKit.Fit(summary, ActionWidth - 88.0f, 12);
            };

            return button;
        }

        /// <summary>
        /// What stands where the settings chip does while the ladder is selected.
        ///
        /// ⚠️ IT IS A `Tray` WITH NO CHEVRON AND NO `PaperButton`, so it cannot be hovered, cannot
        /// be pressed and does not look as if it could be. That is the difference between "this is
        /// locked" and "this is broken", and it is why the ranked rail is not the custom rail with a
        /// control greyed out.
        /// </summary>
        private static void BuildRankedRuleLine(Transform column, Parts parts)
        {
            var go = new GameObject("RankedRuleLine", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(column, false);

            PaperSkin.Apply(go, PaperCraft.Surface.Tray);

            var element = go.AddComponent<LayoutElement>();
            element.minHeight = SettingsChipHeight;
            element.preferredHeight = SettingsChipHeight;
            element.flexibleHeight = 0.0f;
            element.minWidth = ActionWidth;
            element.preferredWidth = ActionWidth;
            element.flexibleWidth = 0.0f;

            var title = PaperKit.Ink(go.transform, "HERO STRIKE  ·  LADDER RULES", PaperKit.Body,
                                     TextAnchor.LowerCenter);
            title.raycastTarget = false;
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.anchorMin = new Vector2(0.0f, 0.48f);
            title.rectTransform.anchorMax = Vector2.one;
            title.rectTransform.offsetMin = new Vector2(PaperKit.Pad, 0.0f);
            title.rectTransform.offsetMax = new Vector2(-PaperKit.Pad, -4.0f);

            var note = PaperKit.Ink(go.transform, "The ladder picks the map and the rules.",
                                    PaperKit.Caption, TextAnchor.UpperCenter, soft: true);
            note.raycastTarget = false;
            note.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            note.rectTransform.anchorMax = new Vector2(1.0f, 0.48f);
            note.rectTransform.offsetMin = new Vector2(PaperKit.Pad, 4.0f);
            note.rectTransform.offsetMax = new Vector2(-PaperKit.Pad, 0.0f);

            go.SetActive(false);
            parts.RankedRuleLine = go;
        }

        /// <summary>
        /// Where `StartButton`, `PrimaryButton` and the ladder's own button all live.
        ///
        /// ⚠️⚠️ ONE SLOT, THREE OCCUPANTS, EXACTLY ONE VISIBLE, AND THAT IS THE WHOLE ANSWER TO
        /// 🧑'S REDUNDANCY. **"dont quick match and start match do the same thing? kinda confusing
        /// no?"** They did. There is one primary on this screen now, always in the same place, and
        /// its LABEL is what changes with the mode:
        ///
        /// | Mode | Who is visible | What it says |
        /// |---|---|---|
        /// | Practice | `PrimaryButton` | START MATCH |
        /// | Ranked | `StartButton` | FIND A RANKED MATCH |
        /// | Custom, hosting | `StartButton` | START MATCH |
        /// | Custom, joined | `PrimaryButton` | READY |
        ///
        /// ⚠️⚠️ THE LADDER REUSES `StartButton` RATHER THAN GETTING ITS OWN, AND THE RENDER IS WHY.
        /// A third button was built here first, through `MenuKit.WoodButton` with the green primary
        /// variation, and `Logs/shots-runtime/LobbyRanked-v53.png` shows the cost: a rounded green
        /// rectangle where every other mode has 🧑's authored chamfered slab. **The one primary on
        /// this screen has to be one OBJECT, or "always in the same place" is true of the position
        /// and false of everything else about it.** `ConvertedMatchSetup.OnStartPressed` dispatches
        /// on the mode, which is one branch against a second control that has to be kept in sync
        /// with the first forever.
        ///
        /// A player who hosts once and joins once must not have to look for the button twice, which
        /// is the reason the slot exists rather than three anchored buttons.
        ///
        /// ⚠️⚠️ THE BUTTON IS PLACED AT A FIXED SIZE RATHER THAN STRETCHED TO THE SLOT. On
        /// `Logs/shots-runtime/LobbyPractice-v52.png` it drew 155 units tall across the settings
        /// chip, because a stretched child inherits whatever the slot's rect resolved to on that
        /// frame and the slot is inside two nested layout groups. `MenuKit.Place` at
        /// <see cref="ActionWidth"/> by <see cref="ActionHeight"/> cannot be anything else.
        ///
        /// ⚠️ THE ACTION KEEPS ITS WOOD. 🧑, on the first paper build: *"u can also still use the
        /// brown color ... start match lowk looks good"*. On a cream rail his authored brown slab is
        /// the darkest, heaviest object in the frame, which is what makes the one action findable
        /// without spending any accent on it.
        ///
        /// ⚠️ `StatusLabel` COMES WITH THEM. It is the screen's one network voice (`SetStatus` /
        /// `SetAlert`), it is hidden until it has something to say, and leaving it in the
        /// deactivated authored column would have made every connection failure silent.
        /// </summary>
        private static void BuildActionSlot(Transform column, Func<string, Transform> find,
                                            Transform leftColumn, Parts parts)
        {
            var slot = new GameObject("ActionSlot", typeof(RectTransform));
            slot.transform.SetParent(column, false);

            var element = slot.AddComponent<LayoutElement>();
            element.minHeight = ActionHeight;
            element.preferredHeight = ActionHeight;
            element.flexibleHeight = 0.0f;
            element.minWidth = ActionWidth;
            element.preferredWidth = ActionWidth;
            element.flexibleWidth = 0.0f;

            foreach (string name in new[] { "StartButton", "PrimaryButton" })
            {
                var node = Descend(leftColumn, name) as RectTransform;
                if (node == null) node = find(name) as RectTransform;
                if (node == null) continue;

                node.SetParent(slot.transform, false);

                // ⚠️⚠️ THE AUTHORED FITTER HAS TO GO OR THE BUTTON SIZES ITSELF TO ITS OWN LABEL.
                // On `Logs/shots-runtime/LobbyPractice-v53.png` `PrimaryButton` drew about 110 by
                // 105 units with `START MATCH` clipped across it, because a Godot Button converts
                // with a `ContentSizeFitter` and a fitter beats anything written to `sizeDelta` on
                // the next layout pass. **A size set on a rect a fitter owns is not a size**, and
                // that is `CLAUDE.md` § 6.2c question 1 arriving from a direction this repository
                // had not seen before: not a wrong number, a number nothing reads.
                var fitter = node.GetComponent<ContentSizeFitter>();
                if (fitter != null) fitter.enabled = false;

                // ⚠️⚠️ THE PENNANT ENTRANCE HAS TO COME OFF, AND IT IS NOT BECAUSE THE ANIMATION
                // IS UNWANTED. `ArrowButtonView` is 🧑's own unfurl and `docs/TODO.md` § 118.1 row
                // 6 asks for MORE motion, not less. The problem is what it does to the RECT:
                // `SetPivot` re-applies `_offMin` and `_offMax`, captured when the component last
                // ran, on every frame until the pivot lands. On the main menu those captured
                // offsets are the pennant's authored ones and that is correct; on a control this
                // file has just reparented and resized, **they are the size the button used to
                // be**, so the rail's 520 by 96 is overwritten by the authored rect one frame
                // later. `Logs/shots-runtime/LobbyPractice-v55.png` is the receipt: a 110-unit
                // START MATCH with its label clipped across it, still there after a 1.5-second
                // wait, which is three times the 0.45-second unfurl.
                //
                // ⚠️ THE SCALE AND THE ALPHA ARE PUT BACK BY HAND, because the component is
                // disabled mid-tween and leaves both wherever the tween had got to. `GodotButton`
                // is still on these nodes and still gives them hover, press and the two sounds.
                var pennant = node.GetComponent<ArrowButtonView>();
                if (pennant != null)
                {
                    pennant.enabled = false;
                    node.localScale = Vector3.one;

                    var group = node.GetComponent<CanvasGroup>();
                    if (group != null) group.alpha = 1.0f;
                }

                MenuKit.Place(node, new Vector2(0.5f, 0.5f), Vector2.zero,
                              new Vector2(ActionWidth, ActionHeight));

                var nodeElement = node.GetComponent<LayoutElement>();
                if (nodeElement != null) nodeElement.ignoreLayout = true;

                if (node.GetComponent<Button>() != null) FocusRing.Attach(node.gameObject, 5.0f);
            }

            var status = Descend(leftColumn, "StatusLabel");
            if (status == null) status = find("StatusLabel");

            if (status != null)
            {
                status.SetParent(column, false);

                var statusElement = status.GetComponent<LayoutElement>();
                if (statusElement == null)
                    statusElement = status.gameObject.AddComponent<LayoutElement>();
                statusElement.minHeight = 30.0f;
                statusElement.preferredHeight = 30.0f;
                statusElement.flexibleHeight = 0.0f;
                statusElement.minWidth = ActionWidth;
                statusElement.preferredWidth = ActionWidth;
                statusElement.flexibleWidth = 0.0f;

                var statusText = status.GetComponent<Text>();
                if (statusText != null)
                {
                    statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
                    statusText.alignment = TextAnchor.UpperCenter;
                    statusText.fontSize = PaperKit.Caption;
                    statusText.color = UiTheme.PaperInkSoft;
                    statusText.raycastTarget = false;
                }

                // ⚠️ OFF UNTIL IT HAS SOMETHING TO SAY. An empty 30-unit gap under the primary
                // reads as a layout that failed, and `WriteStatus` turns it on.
                status.gameObject.SetActive(false);
            }

            if (leftColumn != null) leftColumn.gameObject.SetActive(false);
        }

        // -------------------------------------------------------------------------------------
        // THE MODE COLUMN: three different right-hand sides, not one with things hidden.
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// The right-hand column, whose CONTENT is the mode.
        ///
        /// ⚠️⚠️ THE THREE CONTENTS ARE THREE OBJECTS AND ONLY ONE IS EVER IN THE FRAME. 🧑: *"pls
        /// dont copy paste code u use to generate diff uis, think abt which ones u can copy paste
        /// for and which ones genuinely needs its own buttons"*. Custom gets a room plaque and two
        /// chips; Ranked gets a tier plate and NO chips at all, because a ladder has no code to
        /// share and nobody to invite; Practice gets nothing, because a solo match against bots has
        /// no third question.
        /// </summary>
        private static void BuildModeColumn(Transform rail, Transform rightColumn, Parts parts)
        {
            var column = new GameObject("ModeColumn", typeof(RectTransform));
            column.transform.SetParent(rail, false);

            var element = column.AddComponent<LayoutElement>();
            element.minWidth = RoomColumnWidth;
            element.preferredWidth = RoomColumnWidth;
            element.flexibleWidth = 0.0f;

            var layout = column.AddComponent<VerticalLayoutGroup>();
            layout.spacing = PaperKit.Gap;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            BuildRoomSign(column.transform, parts);
            BuildChipRow(column.transform, parts);
            BuildTierPlate(column.transform, parts);
            BuildRoomDrawer(column.transform, rightColumn, parts);

            parts.ModeColumn = column;
            parts.QueueDock = column.transform;
        }

        /// <summary>
        /// The room code, on a wood plaque.
        ///
        /// ⚠️⚠️ IT WAS A CREAM PLATE WITH AN AMBER UNDERLINE AND 🧑 REJECTED THAT BY EYE: **"this
        /// yellow dont look good withh creme too btw"**. `PaperCraft.Surface.Sign` carries the
        /// reasoning and the number (`ffba00` on `f4ecdd` is 1.7:1). The marker role moves from HUE
        /// to VALUE: on a cream front end the one important thing is the one DARK thing, and this
        /// plaque is the same brown as the primary action twelve units to its left.
        ///
        /// ⚠️ THE CODE IS 44 UNITS AND IT IS THE LARGEST TYPE ON THE SCREEN. `docs/TODO.md`
        /// § 118.3's Among Us row: *"The room code IS the lobby's headline, drawn enormous."* On
        /// `Logs/shots-runtime/Lobby-v52.png` it was 34 and read as a value in a row rather than as
        /// the fact the screen exists to produce.
        ///
        /// ⚠️ THE CAPTION AND THE HINT SIT ON ONE LINE ABOVE IT rather than at opposite ends of the
        /// plate, which is 🧑's *"big ass empty sopace"* applied to the plate he did not crop.
        /// </summary>
        private static void BuildRoomSign(Transform column, Parts parts)
        {
            var go = new GameObject("RoomCodeButton", typeof(RectTransform), typeof(Image),
                                    typeof(Button));
            go.transform.SetParent(column, false);

            PaperSkin.Apply(go, PaperCraft.Surface.Sign);

            var element = go.AddComponent<LayoutElement>();
            element.minHeight = RoomSignHeight;
            element.preferredHeight = RoomSignHeight;
            element.flexibleHeight = 0.0f;

            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = go.GetComponent<Image>();

            var caption = MenuKit.Label(go.transform, "ROOM CODE", PaperKit.Caption,
                                        UiTheme.CreamMuted, Vector2.zero, Vector2.zero,
                                        Vector2.zero, TextAnchor.UpperLeft);
            caption.name = "RoomCodeCaption";
            caption.raycastTarget = false;
            // ⚠️⚠️ THE CAPTION AND THE CODE OWN TWO BANDS OF THE PLAQUE RATHER THAN TWO STRETCHED
            // RECTS. `Logs/shots-runtime/Lobby-v54.png` has `ROOM CODE` drawn through the top of
            // `TS5U`, because a 16-unit caption inset 24 from the bottom and a 44-unit value inset
            // 26 from the top overlap by 12 units on a 62-unit plate. **Two labels overlapping is
            // silent in every direction**, which is § 102.4's fault rotated 90 degrees.
            caption.rectTransform.anchorMin = new Vector2(0.0f, 0.58f);
            caption.rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
            caption.rectTransform.offsetMin = new Vector2(PaperKit.Pad, 0.0f);
            caption.rectTransform.offsetMax = new Vector2(-PaperKit.Pad, -8.0f);

            var hint = MenuKit.Label(go.transform, "tap to copy", PaperKit.Caption,
                                     UiTheme.CreamMuted, Vector2.zero, Vector2.zero,
                                     Vector2.zero, TextAnchor.UpperRight);
            hint.name = "RoomCodeHint";
            hint.raycastTarget = false;
            hint.rectTransform.anchorMin = new Vector2(0.0f, 0.58f);
            hint.rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
            hint.rectTransform.offsetMin = new Vector2(PaperKit.Pad, 0.0f);
            hint.rectTransform.offsetMax = new Vector2(-PaperKit.Pad, -8.0f);

            var label = MenuKit.Label(go.transform, "", PaperKit.Display, UiTheme.Cream,
                                      Vector2.zero, Vector2.zero, Vector2.zero,
                                      TextAnchor.LowerLeft);
            label.name = "RoomCodeValue";
            label.fontStyle = FontStyle.Bold;
            label.raycastTarget = false;
            label.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            label.rectTransform.anchorMax = new Vector2(1.0f, 0.58f);
            label.rectTransform.offsetMin = new Vector2(PaperKit.Pad, PaperCraft.Drop + 2.0f);
            label.rectTransform.offsetMax = new Vector2(-PaperKit.Pad, 0.0f);

            go.AddComponent<PaperButton>();
            FocusRing.Attach(go, 3.0f);

            parts.CodeButton = button;
            parts.CodeValue = label;
            parts.CodeHint = hint;
            parts.CodeCaption = caption.gameObject;

            button.onClick.AddListener(() => parts.CopyCode());
        }

        /// <summary>
        /// JOIN and CHAT: the two ways into and around a custom room.
        ///
        /// ⚠️ THERE IS NO `QUICK MATCH` CHIP ANY MORE AND THAT IS THE POINT. It was a third way to
        /// start a game sitting beside two others; matchmaking is the RANKED tab now and it has the
        /// primary button, not a chip. `docs/TODO.md` § 119.8.
        /// </summary>
        private static void BuildChipRow(Transform column, Parts parts)
        {
            var row = new GameObject("RoomChips", typeof(RectTransform));
            row.transform.SetParent(column, false);

            var element = row.AddComponent<LayoutElement>();
            element.minHeight = PaperKit.ChipHeight;
            element.preferredHeight = PaperKit.ChipHeight;
            element.flexibleHeight = 0.0f;

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = PaperKit.Gap;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            parts.JoinChip = PaperKit.Chip(row.transform, "JoinChip", "JOIN A GAME");
            parts.ChatChip = PaperKit.Chip(row.transform, "ChatChip", "CHAT");

            parts.ChipRow = row;
        }

        /// <summary>
        /// The ladder's own right-hand column: where you stand.
        ///
        /// ⚠️⚠️ IT IS BUILT OUT OF DIFFERENT PARTS FROM THE ROOM COLUMN, NOT THE SAME PARTS WITH
        /// DIFFERENT WORDS. 🧑: *"make custom and ranked ladder shit diff dont jsut copy paste, bcz
        /// ranked laddder dont need join code"*. A room code is a fact you SHARE and has a copy
        /// affordance and a dark plaque; a tier is a fact you EARNED, so it is a plain cream
        /// `Sheet`, it is not pressable at all, and it carries a sentence instead of a chip row.
        ///
        /// ⚠️ THE RULE LINE IS THE ANSWER TO *"make it as well na u cant queue with a friend in
        /// ranked ladder or smth"*. `PartyRules.MaxRankedSize` is `Balance.PlayerCount - 1`, so the
        /// core already refuses a full stack and already refuses a member who is not signed in.
        /// **What was missing was the screen saying so before you press.** A refusal that arrives
        /// only after the press is `CLAUDE.md` § 6.2's INTUITIVE failure.
        /// </summary>
        private static void BuildTierPlate(Transform column, Parts parts)
        {
            var go = PaperKit.Sheet(column, "TierPlate");
            go.raycastTarget = false;

            var element = go.gameObject.AddComponent<LayoutElement>();
            element.minHeight = RoomSignHeight + PaperKit.ChipHeight + PaperKit.Gap;
            element.preferredHeight = element.minHeight;
            element.flexibleHeight = 0.0f;

            var caption = PaperKit.Ink(go.transform, "YOUR TIER", PaperKit.Caption,
                                       TextAnchor.UpperLeft, soft: true);
            caption.raycastTarget = false;
            MenuKit.Stretch(caption.rectTransform, 0.0f);
            caption.rectTransform.offsetMin = new Vector2(PaperKit.Pad, 0.0f);
            caption.rectTransform.offsetMax = new Vector2(-PaperKit.Pad, -PaperKit.Pad);

            // ⚠️⚠️ THE THREE LINES OWN THREE BANDS OF THE PLATE, NOT THREE STRETCHED RECTS.
            // `Logs/shots-runtime/LobbyRanked-v53.png` shows `UNRANKED` and no note under it: the
            // value's rect stretched to the plate's bottom edge and drew straight over the
            // sentence, which is invisible because a `Text` draws nothing where it has no glyphs.
            // **An overlap between two labels is silent in every direction**, which is the fault
            // § 102.4 records for `UiRows` measured horizontally and this one is vertical.
            var tier = PaperKit.Marker(go.transform, "UNRANKED", PaperKit.Display,
                                       TextAnchor.MiddleLeft);
            tier.name = "TierValue";
            tier.raycastTarget = false;
            tier.rectTransform.anchorMin = new Vector2(0.0f, 0.34f);
            tier.rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
            tier.rectTransform.offsetMin = new Vector2(PaperKit.Pad, 0.0f);
            tier.rectTransform.offsetMax = new Vector2(-PaperKit.Pad, -(PaperKit.Pad + 16.0f));

            var note = PaperKit.Ink(go.transform, "", PaperKit.Caption, TextAnchor.LowerLeft,
                                    soft: true);
            note.name = "TierNote";
            note.raycastTarget = false;
            note.horizontalOverflow = HorizontalWrapMode.Wrap;
            note.verticalOverflow = VerticalWrapMode.Truncate;
            note.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            note.rectTransform.anchorMax = new Vector2(1.0f, 0.34f);
            note.rectTransform.offsetMin = new Vector2(PaperKit.Pad, PaperCraft.Drop + 8.0f);
            note.rectTransform.offsetMax = new Vector2(-PaperKit.Pad, 0.0f);

            go.gameObject.SetActive(false);

            parts.TierPlate = go.gameObject;
            parts.TierValue = tier;
            parts.TierNote = note;
        }

        // -------------------------------------------------------------------------------------
        // THE DRAWERS: everything that is not needed RIGHT NOW.
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// A drawer: a cream sheet that grows upward out of the column that opened it.
        ///
        /// ⚠️⚠️ PARENTED TO THE COLUMN, NOT TO THE CANVAS, AND THAT IS WHAT DELETED `StackRight`.
        /// The old chat, queue card and settings body were each anchored to a canvas corner, so
        /// every one had to be positioned against the measured height of the others and
        /// `Logs/shots-runtime/Lobby-v36.png` still shipped a pill floating over the fourth
        /// character with 160 px of bare road under it. A drawer whose parent is its own toggle's
        /// column cannot drift, cannot need to know how tall the chat is, and cannot be left behind
        /// when the rail moves at another aspect ratio.
        ///
        /// ⚠️ IT IS BUILT SHUT. `CLAUDE.md` § 6.2 question 3: a group closed by default with a
        /// one-line summary on its header beats the same rows always open.
        /// </summary>
        private static GameObject Drawer(Transform column, string name, float width, float height)
        {
            var sheet = PaperKit.Sheet(column, name);
            sheet.raycastTarget = true;

            var rect = sheet.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1.0f);
            rect.anchorMax = new Vector2(0.5f, 1.0f);
            rect.pivot = new Vector2(0.5f, 0.0f);
            rect.anchoredPosition = new Vector2(0.0f, PaperKit.Gap);
            rect.sizeDelta = new Vector2(width, height);

            // ⚠️ IGNORED BY THE COLUMN'S OWN LAYOUT GROUP. It is a child for the sake of the
            // ANCHOR, not for the sake of the stack: without this the vertical group would make the
            // drawer the column's third row and push the rail's contents off the rail.
            var element = sheet.gameObject.AddComponent<LayoutElement>();
            element.ignoreLayout = true;

            sheet.gameObject.SetActive(false);
            return sheet.gameObject;
        }

        /// <summary>
        /// The match settings, in a drawer above the primary action.
        ///
        /// ⚠️ THE AUTHORED SELECTOR ROWS COME WITH IT EVEN THOUGH THE DROPDOWNS REPLACED THEM.
        /// `MapPrevButton`, `MapNextButton` and their six siblings are wired by
        /// `ConvertedMatchSetup` and named by `RefreshLeaderControls`, so dropping them would break
        /// the greying that tells three players in four that only the host may change the map.
        /// </summary>
        private static void BuildSettingsDrawer(Transform column, Func<string, Transform> find,
                                                Button chip, Parts parts)
        {
            var body = Drawer(column, "SettingsBody", SettingsDrawerWidth, SettingsBodyHeight);

            var layout = body.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset((int)PaperKit.Pad, (int)PaperKit.Pad,
                                            (int)PaperKit.Pad,
                                            (int)PaperKit.Pad + PaperCraft.Drop);
            layout.spacing = 6.0f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var config = find("ConfigPanel");

            var authored = Descend(config, "Rows");
            if (authored != null)
            {
                authored.SetParent(body.transform, false);
                authored.gameObject.SetActive(false);
            }

            BuildDropdownRows(body.transform, parts);

            var detail = find("DetailBox");
            if (detail != null)
            {
                detail.SetParent(body.transform, false);

                var detailElement = detail.GetComponent<LayoutElement>();
                if (detailElement == null)
                    detailElement = detail.gameObject.AddComponent<LayoutElement>();
                detailElement.minHeight = SettingsDetailHeight;
                detailElement.preferredHeight = SettingsDetailHeight;
                detailElement.flexibleHeight = 0.0f;

                PaperKit.Paperise(detail.gameObject, PaperCraft.Surface.Tray);

                var detailLabel = Descend(detail, "DetailLabel")?.GetComponent<Text>();
                if (detailLabel != null)
                {
                    // ⚠️⚠️ THE INK OUTLINE HAS TO GO OR THE SENTENCE IS A SMEAR. Every `MenuBody`
                    // and `MenuCaption` in `GodotTheme` carries a 3 to 5 unit `GodotOutline`,
                    // because those styles were written to be read over a live 3D street. On an
                    // opaque cream tray a 16-unit caption with a 3-unit outline has a third of its
                    // stroke width added back as a dark halo, which is what
                    // `Logs/shots-runtime/LobbySettings-v53.png` shows on the map's detail line.
                    var outline = detailLabel.GetComponent<GodotOutline>();
                    if (outline != null) outline.enabled = false;

                    detailLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                    detailLabel.alignment = TextAnchor.UpperLeft;
                    detailLabel.color = UiTheme.PaperInkSoft;
                    detailLabel.fontSize = PaperKit.Caption;
                    detailLabel.raycastTarget = false;

                    var lrect = detailLabel.rectTransform;
                    lrect.anchorMin = Vector2.zero;
                    lrect.anchorMax = Vector2.one;
                    lrect.offsetMin = new Vector2(14.0f, 8.0f);
                    lrect.offsetMax = new Vector2(-14.0f, -8.0f);
                }
            }

            if (config != null) config.gameObject.SetActive(false);

            parts.RefreshSummary?.Invoke();

            bool open = false;
            chip.onClick.AddListener(() =>
            {
                open = !open;
                body.SetActive(open);
                if (parts.SettingsCaret != null) parts.SettingsCaret.text = open ? "▴" : "▾";
                if (!open) parts.RefreshSummary?.Invoke();

                Canvas.ForceUpdateCanvases();
            });

            parts.SettingsBody = body;
        }

        /// <summary>Where `ConvertedMatchSetup.BuildSettingsDropdowns` puts the four
        /// <see cref="WoodDropdown"/> rows. ⚠️ The screen owns the option tables, so it builds the
        /// controls; this owns the layout, so it builds the container.</summary>
        private static void BuildDropdownRows(Transform body, Parts parts)
        {
            var holder = new GameObject("SettingsRows", typeof(RectTransform));
            holder.transform.SetParent(body, false);

            var layout = holder.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6.0f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = holder.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            parts.SettingsRows = holder.transform;
        }

        /// <summary>
        /// The authored right column (the seat list, the address row and the two entry buttons), in
        /// a drawer above the JOIN chip.
        ///
        /// ⚠️⚠️ IT IS DRESSED AS A WHOLE SUBTREE, NOT AS A ROOT. On
        /// `Logs/shots-runtime/LobbyServers-v52.png` this drawer was the one entirely wooden thing
        /// left on the screen, because the previous pass applied a paper skin to its own Image and
        /// every `GodotPanel` and `GodotButton` INSIDE it kept drawing wood. That is precisely the
        /// leftover `PaperPurityProbe` exists to catch and precisely what 🧑 asked twice to be sure
        /// of.
        /// </summary>
        private static void BuildRoomDrawer(Transform column, Transform rightColumn, Parts parts)
        {
            if (rightColumn is not RectTransform details) return;

            details.SetParent(column, false);

            details.anchorMin = new Vector2(0.5f, 1.0f);
            details.anchorMax = new Vector2(0.5f, 1.0f);
            details.pivot = new Vector2(0.5f, 0.0f);
            details.anchoredPosition = new Vector2(0.0f, PaperKit.Gap);
            details.sizeDelta = new Vector2(RoomColumnWidth, details.sizeDelta.y);
            details.localScale = Vector3.one;

            var element = details.GetComponent<LayoutElement>();
            if (element == null) element = details.gameObject.AddComponent<LayoutElement>();
            element.ignoreLayout = true;

            var fitter = details.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = details.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Narrow(details, RoomColumnWidth);
            PaperDress.Screen(details);
            PaperKit.Paperise(details.gameObject, PaperCraft.Surface.Sheet);

            details.gameObject.SetActive(false);
            parts.LobbyDetailsRect = details;
            parts.LobbyDrawer = details.gameObject;
        }

        // -------------------------------------------------------------------------------------
        // Shared helpers
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// `ESKINITA` to `Eskinita`, for the quiet restatement lines.
        ///
        /// ⚠️⚠️ 🧑 ASKED FOR THE TYPOGRAPHY BY NAME: *"can u think abt typography and font color and
        /// font size and shit too in ur design"*. Everything on the old rail was capitals, which in
        /// a rounded display face is a wall of same-height rectangles: it is slower to scan and it
        /// shouts, and a screen where every string shouts is the definition of overwhelming.
        ///
        /// **The rule this front end follows: CAPS for verbs and for names, sentence case for
        /// everything the player merely reads.** START MATCH, JOIN A GAME and PRACTICE are caps
        /// because they are things you do or places you are; `Eskinita · Hero Strike · Normal` is
        /// sentence case because it is a restatement of a setting.
        /// </summary>
        internal static string Sentence(string caps)
        {
            if (string.IsNullOrEmpty(caps)) return caps;

            var sb = new System.Text.StringBuilder(caps.Length);
            bool boundary = true;

            foreach (char c in caps)
            {
                if (c == ' ' || c == '·' || c == '-')
                {
                    sb.Append(c);
                    boundary = true;
                    continue;
                }

                sb.Append(boundary ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
                boundary = false;
            }

            return sb.ToString();
        }

        private static string Value(Func<string, Transform> find, string name)
        {
            var node = find(name);
            var label = node != null ? node.GetComponent<Text>() : null;
            if (label == null && node != null) label = node.GetComponentInChildren<Text>();
            return label != null ? label.text.Trim() : "?";
        }

        private static void HideBanner(Func<string, Transform> find)
        {
            var banner = find("Banner");
            if (banner == null) return;

            banner.gameObject.SetActive(false);
        }

        /// <summary>
        /// ⚠️⚠️ THE SCRIM CHANGES SHAPE, NOT STRENGTH, AND THAT IS THE DIFFERENCE BETWEEN "the
        /// arena is the background" AND "the arena is the picture". It is authored as one
        /// full-screen dim over the live map, which is correct when two opaque panels sit in the
        /// middle of the frame and there is nothing else to look at. With four characters standing
        /// in the middle of that frame it is a grey sheet over the only thing worth seeing.
        ///
        /// ⚠️ THE GRADIENT IS A GENERATED TEXTURE RATHER THAN A STACK OF PLATES. Two coplanar
        /// translucent plates sort arbitrarily, which `VISION.md` § 2 rule 3 records shipping a
        /// trail that drew a different colour per drop; a vertical ramp in one texture cannot.
        /// </summary>
        private static void SoftenScrim(Transform root, Func<string, Transform> find)
        {
            var scrim = find("Scrim");
            if (scrim == null) return;

            var image = scrim.GetComponent<Image>();
            if (image == null) return;

            Color authored = image.color;
            image.color = new Color(authored.r, authored.g, authored.b, authored.a * 0.12f);
            image.raycastTarget = false;

            // ⚠️ WARM, NOT NEUTRAL. `CLAUDE.md` § 6.4 bans cold grey in any layer, and a dim over a
            // warm street drawn in neutral black is a cold grey by the time it is composited.
            var band = new Color(0.11f, 0.06f, 0.03f, 1.0f);

            Band(scrim, "ScrimTop", band, TopBandFraction, TopBandAlpha, fromTop: true);
            Band(scrim, "ScrimBottom", band, BottomBandFraction, BottomBandAlpha, fromTop: false);
        }

        private static void Band(Transform sibling, string name, Color tint, float fraction,
                                 float alpha, bool fromTop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(sibling.parent, false);
            go.transform.SetSiblingIndex(sibling.GetSiblingIndex() + 1);

            var image = go.AddComponent<Image>();
            image.sprite = Ramp(fromTop);
            image.type = Image.Type.Simple;
            image.color = new Color(tint.r, tint.g, tint.b, Mathf.Clamp01(alpha));
            image.raycastTarget = false;

            var rt = image.rectTransform;

            if (fromTop)
            {
                rt.anchorMin = new Vector2(0.0f, 1.0f - fraction);
                rt.anchorMax = new Vector2(1.0f, 1.0f);
            }
            else
            {
                rt.anchorMin = new Vector2(0.0f, 0.0f);
                rt.anchorMax = new Vector2(1.0f, fraction);
            }

            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Sprite _rampDown;
        private static Sprite _rampUp;

        private static Sprite Ramp(bool fromTop)
        {
            ref Sprite cached = ref fromTop ? ref _rampDown : ref _rampUp;
            if (cached != null) return cached;

            const int steps = 64;

            var tex = new Texture2D(1, steps, TextureFormat.RGBA32, false)
            {
                name = fromTop ? "ScrimRampDown" : "ScrimRampUp",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            for (int y = 0; y < steps; y++)
            {
                float v = y / (float)(steps - 1);
                float toEdge = fromTop ? v : 1.0f - v;

                // Squared, so the ramp fades out slowly at the edge and quickly in the middle.
                float alpha = toEdge * toEdge;
                tex.SetPixel(0, y, new Color(1.0f, 1.0f, 1.0f, alpha));
            }

            tex.Apply();

            cached = Sprite.Create(tex, new Rect(0, 0, 1, steps), new Vector2(0.5f, 0.5f), 100.0f);
            cached.name = tex.name;

            return cached;
        }

        private static Transform Descend(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;

            for (int i = 0; i < root.childCount; i++)
            {
                var hit = Descend(root.GetChild(i), name);
                if (hit != null) return hit;
            }

            return null;
        }

        /// <summary>
        /// Makes an authored column and everything in it fit a width it was not authored for.
        ///
        /// ⚠️ EVERY CHILD IS ASKED, NOT JUST THE COLUMN. A `LayoutElement` with a preferred width
        /// authored against a 500 px panel keeps that width inside a 420 px parent and overflows
        /// silently, because `MenuKit.Label` overflows rather than wrapping.
        /// </summary>
        private static void Narrow(RectTransform column, float width)
        {
            if (column == null) return;

            var group = column.GetComponent<HorizontalOrVerticalLayoutGroup>();

            if (group != null)
            {
                group.childControlWidth = true;
                group.childForceExpandWidth = true;
            }

            for (int i = 0; i < column.childCount; i++)
            {
                var child = column.GetChild(i) as RectTransform;
                if (child == null) continue;

                var element = child.GetComponent<LayoutElement>();

                if (element != null)
                {
                    if (element.minWidth > 0.0f) element.minWidth = width;
                    if (element.preferredWidth > 0.0f) element.preferredWidth = width;
                }

                var fitter = child.GetComponent<ContentSizeFitter>();
                if (fitter != null) fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                child.sizeDelta = new Vector2(width, child.sizeDelta.y);
            }
        }

        /// <summary>Prints where the rails and the columns actually landed. ⚠️ For a render review,
        /// not for a test: a rectangle in the log is how a layout fault gets a number instead of an
        /// adjective.</summary>
        public static void ReportColumns(Func<string, Transform> find)
        {
            if (find == null) return;

            var root = find("Scrim")?.parent;
            if (root == null) return;

            foreach (string name in new[] { "LobbyTopRail", "LobbyBottomRail", "ActionSlot",
                                            "ModeColumn", "FighterColumn" })
            {
                var node = Descend(root, name) as RectTransform;
                if (node == null) continue;

                var corners = new Vector3[4];
                node.GetWorldCorners(corners);

                Debug.Log($"[LobbyChrome] {name} rect {node.rect.width:F0}x{node.rect.height:F0} " +
                          $"screen x {corners[0].x:F0}..{corners[2].x:F0} " +
                          $"y {corners[0].y:F0}..{corners[2].y:F0}");
            }
        }

        // -------------------------------------------------------------------------------------
        // The handle the screen keeps on everything above.
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// What `ConvertedMatchSetup` holds onto after `Apply`.
        ///
        /// ⚠️ THE FIELDS ARE THE CONTRACT BETWEEN THE TWO FILES. `docs/TODO.md` § 119.3 is the
        /// inventory this satisfies; a field removed here is a control that silently stops being
        /// refreshed, which is worse than one that stops compiling.
        /// </summary>
        public sealed class Parts
        {
            /// <summary>The three mode tabs, indexed by <see cref="LobbyMode"/>.</summary>
            public readonly Button[] Tabs = new Button[3];

            /// <summary>⚠️ ALIASES FOR THE TWO TABS THE PROBES AND THE SHOT PASS PRESS BY NAME.
            /// `PracticeTab` and `CustomTab` are the two that existed before the ladder did.
            /// </summary>
            public Button Practice;
            public Button Multiplayer;

            public LobbyMode Mode { get; private set; } = LobbyMode.Custom;

            /// <summary>The authored right column, used as the JOIN drawer.</summary>
            public GameObject LobbyDrawer;
            public RectTransform LobbyDetailsRect;

            /// <summary>The right-hand column of the bottom rail, and the three things that can be
            /// in it.</summary>
            public GameObject ModeColumn;
            public GameObject ChipRow;
            public GameObject TierPlate;
            public Text TierValue;
            public Text TierNote;

            public Button JoinChip;
            public Button ChatChip;

            /// <summary>The settings chip and the locked plate that replaces it in ranked.
            /// </summary>
            public GameObject SettingsChip;
            public GameObject RankedRuleLine;
            public GameObject SettingsBody;

            /// <summary>The big line on the fighter row: who you are playing.</summary>
            public Text CharacterName;

            /// <summary>The small line under it: the can and the slipper.</summary>
            public Text CharacterLoadout;

            /// <summary>The SKILLS row: the door to the hub's LOADOUT tab, the summary on it, and
            /// the caption beside it, so all three can be hidden together in Classic.</summary>
            public Button LoadoutButton;
            public Text LoadoutValue;
            public GameObject LoadoutCaption;

            /// <summary>The room code plaque, the code, its hint and the caption, so all four go
            /// when there is no code.</summary>
            public Button CodeButton;
            public Text CodeValue;
            public Text CodeHint;
            public GameObject CodeCaption;

            /// <summary>The closed settings chip's second line, and its caret.</summary>
            public Text SettingsSummary;
            public Text SettingsCaret;

            /// <summary>Where the match settings dropdowns are built, by the screen that owns the
            /// option tables.</summary>
            public Transform SettingsRows;

            /// <summary>Where the queue card docks: above the mode column, so it grows out of the
            /// button that opened it.</summary>
            public Transform QueueDock;

            /// <summary>⚠️ PHASE 12'S RULES STEPPER, WHICH ONLY EXISTS UNDER `LobbyStyle.Classic`.
            /// `Street` builds four `WoodDropdown` rows instead and these stay null, which is what
            /// they have always done; they are kept so the two styles share one handle.</summary>
            public Button FormatPrev;
            public Button FormatNext;
            public Text FormatValue;

            /// <summary>The door to `PlayerHub`, and the line on it.</summary>
            public Button ProfileButton;
            public Text ProfileValue;

            /// <summary>Raised when the player finishes editing their name, so the screen can push
            /// it to the lobby rather than waiting for the next redraw.</summary>
            public Action NameCommitted;

            /// <summary>Rewrites the closed chip's second line from the three value labels.
            /// ⚠️ It hangs off `Refresh`, which is what actually changes those values.</summary>
            public Action RefreshSummary;

            private string _code = "";
            private float _copiedUntil;

            /// <summary>
            /// Writes the code, or takes the whole plaque off the column.
            ///
            /// ⚠️ THE CAPTION AND THE HINT ARE CHILDREN OF THE PLAQUE, so one `SetActive` takes the
            /// whole thing. The previous version had them as siblings, which is why that method had
            /// to hide three objects and why forgetting one left a `ROOM` heading sitting over the
            /// next control down.
            /// </summary>
            public void SetCode(string code)
            {
                _code = code ?? "";
                bool has = HasCode && Mode == LobbyMode.Custom;

                if (CodeButton != null) CodeButton.gameObject.SetActive(has);
                if (!has || CodeValue == null) return;

                if (Time.unscaledTime < _copiedUntil) return;

                CodeValue.text = _code;

                // The value's box runs from `PaperKit.Pad` in to `PaperKit.Pad` off the right edge,
                // so inside `RoomColumnWidth` 420 it has 384. A Relay join code is four characters
                // at 44 units, about 112; a LAN code may be longer, and `Fit` is what stops the
                // longer one running off the plaque.
                MenuKit.Fit(CodeValue, RoomColumnWidth - (PaperKit.Pad * 2.0f));

                if (CodeHint != null) CodeHint.text = "tap to copy";
            }

            /// <summary>
            /// ⚠️ THE RECEIPT IS ON THE CONTROL ITSELF AND LASTS A MOMENT. A copy that reports
            /// nothing is indistinguishable from a copy that failed, and the status line under the
            /// primary is for network faults, not for confirmations.
            /// </summary>
            public void CopyCode()
            {
                if (string.IsNullOrWhiteSpace(_code)) return;

                GUIUtility.systemCopyBuffer = _code;
                _copiedUntil = Time.unscaledTime + 1.6f;

                if (CodeHint != null) CodeHint.text = "copied";
                MenuSfx.Click();
            }

            /// <summary>
            /// Writes the SKILLS row, or takes it off the column.
            ///
            /// ⚠️ THE CAPTION GOES WITH IT. Hiding the value alone leaves a `SKILLS` caption over
            /// nothing, which is a label naming a control that is not there.
            /// </summary>
            public void SetSkills(bool shown, string summary)
            {
                if (LoadoutButton != null) LoadoutButton.gameObject.SetActive(shown);

                if (!shown || LoadoutValue == null) return;

                // ⚠️ THE HERO'S NAME IS STRIPPED, BECAUSE THE ROW DIRECTLY ABOVE THIS ONE IS THE
                // HERO'S NAME. `Logs/shots-runtime/Lobby-v52.png` reads `DANTE` at 26 units and
                // then `DANTE · standard build` at 20 units underneath it: the same word twice, in
                // two sizes, in adjacent rows. § 94.7's *"the same number twice"* one control over.
                string trimmed = summary ?? "";
                int split = trimmed.IndexOf('·');
                if (split >= 0 && split + 1 < trimmed.Length)
                    trimmed = trimmed.Substring(split + 1).Trim();

                LoadoutValue.text = Sentence(trimmed);
                LoadoutValue.fontSize = PaperKit.Body;
                MenuKit.Fit(LoadoutValue, FighterColumnWidth - 130.0f);
            }

            public void SetLoadout(string character, string loadout)
            {
                if (CharacterName != null)
                {
                    CharacterName.text = character;
                    CharacterName.fontSize = PaperKit.Title;
                    MenuKit.Fit(CharacterName, FighterColumnWidth - 60.0f);
                }

                if (CharacterLoadout != null)
                {
                    // ⚠️ SENTENCE CASE. See `Sentence`: caps for verbs and names, sentence case for
                    // anything the player merely reads. `PASIP · TSINELAS` under a 26-unit `DANTE`
                    // was two shouted lines in a 62-unit row.
                    CharacterLoadout.text = Sentence(loadout);
                    CharacterLoadout.fontSize = PaperKit.Caption;
                    MenuKit.Fit(CharacterLoadout, FighterColumnWidth - 60.0f, 12);
                }
            }

            /// <summary>
            /// Switches the whole screen between the three modes.
            ///
            /// ⚠️⚠️ IT SWAPS THREE THINGS AT ONCE AND THAT IS WHY IT IS ONE METHOD: the live tab,
            /// what the centre column's top control IS, and what the right column CONTAINS. Doing
            /// it in three places is how a screen ends up in a state no mode describes, which is
            /// what `docs/TODO.md` § 114.13 records for the queue card.
            /// </summary>
            public void SetMode(LobbyMode mode)
            {
                Mode = mode;

                for (int i = 0; i < Tabs.Length; i++) Paint(Tabs[i], i == (int)mode);

                bool custom = mode == LobbyMode.Custom;
                bool ranked = mode == LobbyMode.Ranked;

                if (SettingsChip != null) SettingsChip.SetActive(!ranked);
                if (RankedRuleLine != null) RankedRuleLine.SetActive(ranked);
                if (SettingsBody != null && ranked) SettingsBody.SetActive(false);

                if (ChipRow != null) ChipRow.SetActive(custom);
                if (TierPlate != null) TierPlate.SetActive(ranked);

                // ⚠️ THE WHOLE COLUMN GOES IN PRACTICE AND THE RAIL SHRINKS WITH IT. See the
                // `ContentSizeFitter` in `BuildBottomRail`: 🧑 asked *"why is entire right side
                // empty"* of a rail that was still reserving the column's width for a mode that
                // has nothing to put in it.
                if (ModeColumn != null) ModeColumn.SetActive(mode != LobbyMode.Practice);
                if (CodeButton != null) CodeButton.gameObject.SetActive(custom && HasCode);
                if (LobbyDrawer != null && !custom) LobbyDrawer.SetActive(false);

                if (SettingsCaret != null) SettingsCaret.text = "▾";
            }

            private bool HasCode => !string.IsNullOrWhiteSpace(_code);

            /// <summary>⚠️ KEPT FOR THE TWO CALLERS THAT STILL THINK IN "IS THIS NETWORKED". A
            /// networked screen is `Custom` unless the player has already chosen the ladder.
            /// </summary>
            public void SetActive(bool lobby)
            {
                if (!lobby) SetMode(LobbyMode.Practice);
                else if (Mode == LobbyMode.Practice) SetMode(LobbyMode.Custom);
                else SetMode(Mode);
            }

            /// <summary>
            /// Writes the ladder plate.
            ///
            /// ⚠️ AN UNPLACED PLAYER IS TOLD SO RATHER THAN SHOWN A ZERO. `RatingRules.TierFor`
            /// answers `BATA` for a fresh rating, which would advertise a tier nobody has earned;
            /// the honest string before any ranked match is played is that there is no tier yet.
            /// </summary>
            public void SetTier(string tier, string note)
            {
                if (TierValue != null)
                {
                    TierValue.text = tier ?? "UNRANKED";
                    MenuKit.Fit(TierValue, RoomColumnWidth - (PaperKit.Pad * 2.0f));
                }

                if (TierNote != null) TierNote.text = note ?? "";
            }

            /// <summary>
            /// ⚠️⚠️ THE LIVE TAB IS A WOOD-DARK PILL WITH CREAM LETTERING AND THE OTHERS ARE
            /// OUTLINES. `docs/TODO.md` § 118.4 forbids putting the amber accent on a tab and
            /// `game-ui-design`'s `Color-Only Information` anti-pattern forbids saying "this one" in
            /// hue alone. This is a VALUE inversion of about 10:1 plus a fill-against-outline
            /// silhouette difference, which is two signals and neither of them is a hue.
            ///
            /// ⚠️ IT WAS `Token` AGAINST `Ghost` AND THE RENDER SAID THAT WAS NOT ENOUGH:
            /// `Logs/shots-runtime/Lobby-v52.png`, where the two chips are 4 per cent apart in value
            /// and read as the same control twice.
            /// </summary>
            private static void Paint(Button button, bool active)
            {
                if (button == null) return;

                var skin = button.GetComponent<PaperSkin>();
                if (skin != null)
                {
                    skin.Surface = active ? PaperCraft.Surface.Live : PaperCraft.Surface.Ghost;
                    skin.Rebuild();
                }

                var label = button.transform.Find("Label")?.GetComponent<Text>();
                if (label == null) return;

                label.fontStyle = FontStyle.Bold;
                label.color = active ? UiTheme.Cream : UiTheme.PaperInkSoft;
            }
        }
    }
}
