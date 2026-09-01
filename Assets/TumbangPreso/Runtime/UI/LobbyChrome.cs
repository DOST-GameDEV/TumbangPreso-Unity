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

        /// <summary>The room is the picture: furniture pushed to the edges, cast in the middle.</summary>
        Street,
    }

    /// <summary>
    /// Rearranges the setup screen's authored furniture into the `Street` layout.
    ///
    /// ⚠️⚠️ IT MOVES WHAT IS ALREADY THERE AND BUILDS ALMOST NOTHING. `ConvertedScreen` finds
    /// every control by the name Godot gave it and `Node()` logs an error on a miss, so a redesign
    /// that rebuilt this screen would have to reproduce fourteen exact names or break the wiring
    /// silently. Repositioning keeps `SeatButton0..3`, `PrimaryButton`, `StartButton`,
    /// `MapValueLabel` and the rest exactly where the script expects to find them, keeps their
    /// handlers, keeps their `GodotButton` skins, and keeps `MatchSetup.unity` almost unchanged on
    /// disk so `SceneScriptCheck` has nothing new to refuse.
    ///
    /// ⚠️⚠️ AND THE PANELS STAY OPAQUE WOOD. `UiTheme.HeroPlate`'s note is explicit that a
    /// translucent near-black plate is COMBAT chrome, where the court behind it is the subject,
    /// and that menu chrome is FURNITURE and may be opaque. 🧑 has already rejected the other
    /// answer once: *"the brown shit looks ugly"*. The way the room becomes the picture here is by
    /// making the furniture SMALLER and pushing it to the edges, which is what the reference
    /// screenshots actually do, rather than by making it see-through.
    ///
    /// ⚠️ EVERY STEP IS INDIVIDUALLY GUARDED. A missing node leaves that piece in its authored
    /// place and logs, rather than throwing halfway through and leaving the screen in a state
    /// that is neither layout.
    /// </summary>
    public static class LobbyChrome
    {
        /// <summary>
        /// The default, and the only place it is decided.
        ///
        /// ⚠️ A FIELD RATHER THAN A CONST so a probe can photograph both without a rebuild, and
        /// so reverting is one assignment. `LobbyStyleTests` asserts that every name the screen
        /// reaches for still resolves under both.
        /// </summary>
        public static LobbyStyle Style = LobbyStyle.Street;

        /// <summary>
        /// Width of the furniture columns in the authored 1920x1080 space.
        ///
        /// ⚠️⚠️ MEASURED OFF `Logs/shots-runtime/Lobby-v2.png` RATHER THAN CHOSEN. At 660 and 560
        /// the two columns cover 1220 of 1920 px, so the clear band the cast has to stand in was
        /// 700 px wide and the leftmost of the four was entirely behind the config panel.
        /// Narrowing them to 580 and 500 gives the middle 840 px, which is what four bodies at
        /// `LobbyCast.Spacing` 1.75 occupy at the lobby framing.
        /// </summary>
        private const float RightWidth = 590.0f;

        // -------------------------------------------------------------------------------------
        // ⚠️⚠️ THE HARMONY SET. EVERY EDGE, GAP AND HEIGHT ON THIS SCREEN COMES FROM HERE.
        //
        // 🧑 2026-08-28, with the bottom band cropped out of `Lobby-v35.png`: *"make these huds or
        // ui look good bruh its so weird to look at as none of them have visual harmony or shit"*.
        // He was right and the crop proves it. Measured off that image, the bottom-left rail alone
        // had THREE different left edges and THREE different widths: the MATCH SETTINGS pill
        // started at x=75 and ran 300 px, its summary line started at x=60, and START MATCH
        // started at x=55 and ran 380. On the right the LOBBY & SERVERS pill and the chat box
        // below it shared neither a width nor a right edge. Nothing lined up with anything.
        //
        // The cause was structural rather than a set of wrong numbers: the left side was TWO
        // separate hosts (the authored `LeftColumn` at one anchor and a `SettingsDrawer` built
        // beside it at another), each scaled by a different factor, each sizing its own children.
        // Two containers cannot share an edge by arithmetic; they share one by being one
        // container. So there is one rail per side now, everything is a child of it, and the
        // group gives every child the same width.
        //
        // ⚠️ AND THE SCALES ARE GONE FROM THE LEFT. `LeftScale` 0.66 existed to squeeze an
        // authored 820 px panel into the corner, and it made every number on that side a lie:
        // a 56 px header drew at 37, an 18 unit caption rendered at 12, and no value here could be
        // compared against any value anywhere else in the UI. The rail is authored at its real
        // size in the canvas's own 1920x1080 space. `RightScale` stays, for the reason at its own
        // note.
        // -------------------------------------------------------------------------------------

        /// <summary>The margin every edge-anchored thing uses, left and right.</summary>
        private const float EdgeMargin = 48.0f;

        /// <summary>The line both bottom rails sit on.</summary>
        private const float BottomMargin = 40.0f;

        /// <summary>The line BACK, both tabs and the player card all hang from.</summary>
        private const float TopMargin = 34.0f;

        /// <summary>The one gap. Between any two stacked pieces of furniture, on either side.
        /// </summary>
        private const float RailSpacing = 12.0f;

        /// <summary>
        /// The bottom-left rail: settings, the primary action, and the status line.
        ///
        /// ⚠️⚠️ IT CAME DOWN FROM 560 AND THE 100 px CAME OUT OF THE CAPTION COLUMN, NOT OUT OF
        /// THE TYPE. 🧑 2026-08-28, of the rail: *"do u not feel weird that theres b ig ass empty
        /// space left and right"*. At 560 the words MATCH SETTINGS sat in the middle of a pill with
        /// about 150 px of bare wood either side of them, and START MATCH the same. The rail was
        /// that wide because a selector row was, and a selector row was that wide because
        /// `SettingsCaptionWidth` was 150: it had to hold `BOTS:` at <see cref="CaptionSize"/>,
        /// which needs about 66. Dropping the colon (see `DressSelectorRow`) and sizing the column
        /// to the word took it to 96 and the rail with it.
        ///
        /// The arithmetic at 460: 96 caption + 14 gap + a stepper of 20 padding, two
        /// <see cref="SettingsArrowSize"/> 42 arrows, two 6 px gaps and the value. That leaves
        /// **214 px** for the value, against `ILALIM NG TULAY` measuring about 195 at
        /// <see cref="ValueSize"/> 26, plus 32 of panel padding.
        ///
        /// ⚠️ IT IS ALSO THE WIDTH OF START MATCH, THE SUMMARY AND THE STATUS LINE, because that
        /// is what "one rail" means. The old layout sized each of those to itself.
        /// </summary>
        private const float LeftWidth = 460.0f;

        /// <summary>
        /// The bottom-right rail: the lobby drawer and the chat.
        ///
        /// ⚠️⚠️ IT IS <see cref="LeftWidth"/> NOW, NOT THE CHAT'S OWN 392. 🧑 2026-08-28: *"align
        /// the yellow thing with match settings"*. The two drawer toggles are the same kind of
        /// control and were 460 and 392, so the bottom of the screen had two widths in it for no
        /// reason a player could see. One number means the two rails mirror rather than merely
        /// share a margin, and it costs the chat nothing: a wider log fits more of a line.
        ///
        /// ⚠️ THE CHAT IS TOLD, NOT ASKED. `ConvertedMatchSetup.BuildChat` passes this to
        /// `LobbyChat.PlaceBottomRight`; `LobbyChat`'s own `PanelWidth` is the fallback for the
        /// in-match instance, which has no rail to belong to.
        /// </summary>
        private const float RightRailWidth = LeftWidth;

        /// <summary>One height for BACK and for each tab, so the top of the screen is one band.
        /// </summary>
        private const float HeaderHeight = 56.0f;

        /// <summary>One height for both drawer toggles: MATCH SETTINGS and LOBBY & SERVERS.
        /// </summary>
        private const float ToggleHeight = 52.0f;

        /// <summary>START MATCH and READY. Taller than everything else on purpose: it is the one
        /// control on this screen that ends the screen.</summary>
        private const float ActionHeight = 104.0f;

        /// <summary>
        /// The caption under MATCH SETTINGS: `ESKINITA · HERO STRIKE · HARD`.
        ///
        /// ⚠️ SMALLER THAN `MenuKit.MinReadableUnits`, AND IT IS THE ONE PLACE THAT IS ALLOWED.
        /// 🧑 2026-08-28: *"make font size here smaller"*. The floor exists so a sentence does not
        /// become texture; this is not a sentence, it is three words the drawer directly under it
        /// restates at <see cref="ValueSize"/> 26 the moment it is opened. Nothing on this screen
        /// is only knowable from this line.
        ///
        /// ⚠️ AND THE GAP TO THE BUTTON IS <see cref="HeaderGap"/> 2, NOT `RailSpacing` 12,
        /// because it is a caption ON that button rather than a neighbour of it.
        /// </summary>
        private const int SummarySize = 16;
        private const float SummaryHeight = 22.0f;
        private const float HeaderGap = 2.0f;

        /// <summary>
        /// Where the bottom edge of BOTH drawer toggles sits.
        ///
        /// ⚠️⚠️ THE TWO YELLOW PILLS ARE ONE ROW, ON REQUEST. 🧑 2026-08-28: *"align the yellow
        /// thing with match settings use same font size too"*. They are the same KIND of control
        /// (open a drawer), so they get one width, one height, one type size and one baseline.
        /// Before this the left one was 460 wide at 26 units and the right one 392 at the wood
        /// variation's authored size, sitting 63 px lower.
        ///
        /// Counted up the left rail from the floor: <see cref="BottomMargin"/> 40, START MATCH at
        /// <see cref="ActionHeight"/> 104, one <see cref="RailSpacing"/> 12, then the summary and
        /// its 2 px gap inside the header block.
        ///
        /// ⚠️ THE RIGHT ONE TAKES THE HIGHER OF THIS AND THE CHAT'S TOP. See
        /// <see cref="Parts.StackRight"/>: the chat grows upward as lines arrive, so a fixed
        /// baseline would be overlapped by the sixth message.
        /// </summary>
        private static float ToggleBaseline => BottomMargin + ActionHeight + RailSpacing
                                               + SummaryHeight + HeaderGap;

        /// <summary>
        /// How much the two authored columns shrink in the `Street` arrangement.
        ///
        /// ⚠️⚠️ MEASURED OFF THE RENDERS, AND TIGHTENED TWICE. The config panel draws 820 px wide
        /// and the seat panel 560, so unscaled the clear band between them is 320 px and four
        /// characters need about 700. 0.72 and 0.86 opened it to 625, which fit the cast at
        /// `LobbyCast.Spacing` 1.20 and stopped fitting the moment the spacing was widened on
        /// request: `Lobby-v10.png` has the outer two behind the furniture again. 0.66 and 0.78
        /// give 846 px, which holds the wider line with about 70 px of margin at each end.
        ///
        /// ⚠️ THIS IS NEAR THE FLOOR AND THE FLOOR IS REAL. The smallest type in the left column
        /// is the map detail line at 20 units, which at 0.66 renders as 13: below that it stops
        /// being a sentence and becomes texture. Widening the band any further has to come from
        /// moving the camera back, not from shrinking the furniture again.
        ///
        /// ⚠️ THE TWO DIFFER BECAUSE THEIR CONTENTS DO. The seat panel is four rows of a name and
        /// a tick, and it is the thing a player reads to find out who is here, so it keeps more of
        /// its size. The config panel is four labelled cyclers whose values are short words, and
        /// it survives being small.
        ///
        /// ⚠️ AND NEITHER MAY GO BELOW ABOUT 0.65, because `MenuKit.MinReadableUnits` is a floor on
        /// the AUTHORED font size and a scale multiplies whatever survives it. `AspectRatioProbes`
        /// checks the authored number and cannot see a scaled parent, so this is the one place
        /// that bound has to be respected by hand.
        /// </summary>
        /// <summary>
        /// ⚠️⚠️ ONLY THE RIGHT COLUMN IS SCALED NOW, AND ONLY BECAUSE ITS CONTENT IS AUTHORED.
        /// The lobby drawer is the seat panel with the address row, the code row and the two entry
        /// buttons built into it by `ConvertedMatchSetup.BuildRightPanelNetwork`, all of it sized
        /// against a 500 px column; rebuilding that at a real size is a different job from this
        /// one. 502 x 0.78 is 392, which is <see cref="RightRailWidth"/> exactly, so it shares the
        /// chat's width and right edge while keeping the tuned look inside it.
        ///
        /// ⚠️ THE LEFT SCALE IS DELETED. See the harmony block above: it made every number on that
        /// side a lie, and the rail is authored at its real size instead.
        /// </summary>
        private const float RightScale = 0.78f;

        /// <summary>How tall the gradient bands are, as a fraction of the screen.</summary>
        private const float TopBandFraction = 0.24f;
        private const float BottomBandFraction = 0.30f;

        /// <summary>
        /// How dark each band gets at the screen edge.
        ///
        /// ⚠️ THE BOTTOM IS LIGHTER THAN THE TOP, WHICH IS THE OPPOSITE OF THE OBVIOUS CHOICE. The
        /// top band sits behind the banner and the tabs and has nothing but sky under it; the
        /// bottom band has the CAST'S LEGS under it, and the whole point of the arrangement is that
        /// the room is the picture. 0.30 is enough for cream type over a bright road and little
        /// enough that a character standing in it still reads as lit.
        /// </summary>
        private const float TopBandAlpha = 0.52f;
        private const float BottomBandAlpha = 0.30f;

        /// <summary>
        /// How tall the opened settings card is.
        ///
        /// ⚠️⚠️ IT CAME DOWN FROM 430 WHEN THE FOURTH ROW LEFT. The card held MAP, MODE, BOTS and
        /// CHARACTER; CHARACTER is now part of the player card (see <see cref="BuildIdentity"/>),
        /// so what is left is three cyclers and the map's detail line. Leaving the old height
        /// would have opened a drawer with 90 px of bare wood under the last row, which reads as
        /// a row that failed to draw rather than as spacing.
        ///
        /// 3 rows x <see cref="SettingsRowHeight"/> + 2 gaps x 8 + the detail box + 36 of padding
        /// is 3 x 64 + 16 + 56 + 36 = 300.
        /// </summary>
        /// ⚠️ 364 SINCE PHASE 12: three selector rows became four (RULES, see `BuildFormatRow`)
        /// and 64 is `SettingsRowHeight`. It is the rows plus the detail box plus the padding,
        /// added up, rather than a number that looked right: a drawer sized by eye is a drawer
        /// whose last row is outside it the first time anybody adds one.
        private const float SettingsBodyHeight = 364.0f;

        /// <summary>
        /// One selector row: caption on the left, stepper on the right.
        ///
        /// ⚠️⚠️ THE AUTHORED ROWS WERE THE UGLY PART AND THE CAUSE IS ONE NUMBER. 🧑 2026-08-28:
        /// *"match settings look ugly"*, *"revamp UI for match settings bcz its really ugly and
        /// doesnt look good like that"*. In `MatchSetup.unity` every caption (`MapCaption`,
        /// `ModeCaption`, `DifficultyCaption`, `FighterCaption`) is authored at **52 units** and
        /// every value (`MapValueLabel` and its two siblings) at **34**, so the word MAP: is drawn
        /// half again as large as the map's actual name. The label shouted and the thing it
        /// labelled whispered, which is the whole reason the panel read as a form.
        ///
        /// The rebuilt row inverts that: <see cref="CaptionSize"/> 22 amber for the caption,
        /// <see cref="ValueSize"/> 26 cream for the value. It also fixes the caption COLUMN, which
        /// the authored `HorizontalLayoutGroup` never did: MAP:, MODE: and BOTS: are three
        /// different widths, so the three steppers each started at a different x and nothing in
        /// the panel lined up vertically.
        /// </summary>
        private const float SettingsRowHeight = 64.0f;
        private const float SettingsCaptionWidth = 96.0f;

        /// <summary>
        /// How wide the value in a selector well actually is, for a caller that has to fit a
        /// string into one.
        ///
        /// ⚠️ IT IS ARITHMETIC OFF THE RAIL AND NOT A GUESS: the rail is `LeftWidth`, the body
        /// takes 16 either side, the row takes the caption column and a 14-unit gap, and the well
        /// holds two `SettingsArrowSize` arrows with 6 units of spacing each side of the value.
        /// `LAST TSINELAS STANDING` is what needs it (`ConvertedMatchSetup`'s RULES row).
        /// </summary>
        public const float FormatValueWidth =
            LeftWidth - 32.0f - SettingsCaptionWidth - 14.0f - (SettingsArrowSize * 2.0f) - 32.0f;
        private const float SettingsArrowSize = 42.0f;
        private const int CaptionSize = 22;
        internal const int ValueSize = 26;
        private const float SettingsDetailHeight = 56.0f;

        /// <summary>
        /// BACK, in the top-left corner the banner used to fill.
        ///
        /// ⚠️⚠️ IT WAS DIRECTLY UNDER START MATCH AND THAT IS A HIERARCHY FAULT, NOT A SPACING
        /// ONE. 🧑 2026-08-28: *"put BACK somewhere else, it looks ugly that its right below start
        /// match, it fucks up the visual hierarchy"*. The authored column stacks CONFIG, START,
        /// STATUS, a spacer and BACK, so the screen's single most important control and its single
        /// least important one shared an edge, the same width and the same corner. The eye reads a
        /// stack as a list of equals.
        ///
        /// ⚠️ TOP-LEFT IS FREE BECAUSE THE BANNER LEFT. See <see cref="HideBanner"/>. Putting BACK
        /// there is also where every other screen in this game keeps it in the reader's memory:
        /// `CharacterSelect` and both overlays anchor theirs to the top-left of their card.
        ///
        /// ⚠️ AND IT IS SMALL AND UNSCALED. The bottom-left rail is drawn at
        /// <see cref="LeftScale"/> 0.66; BACK sits outside that rail now, so it keeps its authored
        /// type at a size chosen against the string rather than inheriting a shrink meant for a
        /// panel of cyclers.
        /// </summary>
        private const float BackWidth = 208.0f;

        /// <summary>
        /// The player card: who you are, and who you are playing as.
        ///
        /// ⚠️⚠️ CHARACTER LEFT THE MATCH SETTINGS ON PURPOSE AND THE REASON IS AUTHORITY, NOT
        /// TIDINESS. 🧑 2026-08-28: *"also maybe plan out where to put ui for char select, remove
        /// it in match settings"*, and *"make a better button for character select and u figrue
        /// out where to place it"*. MAP, MODE and BOTS are the LEADER'S controls: on a client all
        /// three are greyed by `RefreshLeaderControls` because only the host may change them. Your
        /// CHARACTER is the one choice on this screen that is always yours, whoever is hosting, so
        /// keeping it as the fourth row of a panel that greys out told three players in every
        /// four-player lobby that they could not pick a fighter.
        ///
        /// ⚠️ IT IS THE SAME PANEL IT ALWAYS OPENED. 🧑: *"I want it to lead to the same screen as
        /// before"*. `OpenCharacterSelect` is untouched and still reveals `CharacterSelectPanel`
        /// in place; the authored `CharacterButton` node is REPARENTED here, keeping its name, its
        /// `Button`, its `GodotButton` skin and its handler. `docs/TODO.md` § 68.13 forbids
        /// touching `ConvertedCharacterSelect.cs` or `CharacterSelect.unity` and neither is.
        ///
        /// ⚠️ NAME AND CHARACTER SIT TOGETHER BECAUSE THEY ARE ONE FACT. They are the two things
        /// the other three people in the room see about you, and they are what the nameplate over
        /// your body in the arena behind this card is drawn from. Splitting them put half your
        /// identity in a corner and half in a drawer.
        /// </summary>
        /// <summary>
        /// The player card matches the right-hand rail rather than choosing its own width.
        ///
        /// ⚠️ 392 IS <see cref="RightRailWidth"/>, WHICH IS THE CHAT'S. The card is the top of the
        /// right-hand side and the chat is the bottom of it; giving them one width and one right
        /// edge is what makes that side read as a column instead of as three unrelated boxes.
        /// </summary>
        /// <summary>
        /// The player card's width.
        ///
        /// ⚠️⚠️ IT IS 330 AND NOT <see cref="RightRailWidth"/> 392, AND THAT IS A CORRECTION.
        /// Matching the chat's width looked like the harmonious answer and produced a card with a
        /// visible hole in it: 🧑 2026-08-28, twice, *"theres big empty space from cheska and my
        /// name to > and edit, tighten it"* and, of the first attempt, *"i asked u to tighten this
        /// and make stuff smaller, hhave you?"*. The first pass took the space out of the PADDING,
        /// which was not where it was. The gap is between the END OF A SHORT LEFT-ALIGNED STRING
        /// and an affordance pinned to the right edge, so the only thing that closes it is the
        /// distance between those two, which is the width.
        ///
        /// ⚠️ THE SHARED AXIS IS THE RIGHT EDGE, NOT THE WIDTH, AND THAT IS THE HONEST RULE. A
        /// chat log is sized by how much text fits on a line; a player card is sized by the two
        /// names in it. Forcing one number on both is how the card ended up 60 px wider than
        /// anything it contains. Everything on this side still starts at <see cref="EdgeMargin"/>
        /// from the right, which is the alignment a reader actually sees.
        ///
        /// ⚠️ AND 330 IS MEASURED AGAINST THE WORST CASE IN THE ROSTER, NOT AGAINST `CHESKA`.
        /// Inside the padding there are 302 px. The longest character name is `LOLA PACING`, 11
        /// characters, about 154 px at <see cref="CardCharacterSize"/> against the 244 the row
        /// gives it. The longest loadout line is `DECADES TUNA  ·  TSINELAS`, 25 characters and
        /// about 225 px at <see cref="MenuKit.MinReadableUnits"/>, which is the tightest of the
        /// three and still clears. A player name is capped at `Balance.PlayerNameMax` 14, about
        /// 154 px at <see cref="CardNameSize"/> against 238, and the field is best-fit down to 14
        /// units on top of that because a name is the one string here typed by a human.
        /// </summary>
        private const float CardWidth = 330.0f;

        /// <summary>
        /// The card's own paddings and heights, tightened once against a render.
        ///
        /// ⚠️⚠️ THE FIRST VERSION WAS FULL OF AIR AND 🧑 CALLED IT: *"theres big empty space from
        /// cheska and my name to > and edit, tighten it"*, *"make font smaaller for Cheska it looks
        /// ugly lowkey"*. Measured off `Logs/shots-runtime/Lobby-v36.png`: the name row reserved
        /// **72 px** on the right for the word EDIT, which needs 40 at
        /// <see cref="MenuKit.MinReadableUnits"/>, and the character row reserved 68 for a chevron
        /// needing 22. Between the two of them the card threw away 78 px of its 392 to gutters
        /// nothing was in, and CHESKA at 32 units in a row 82 px tall left a band of bare wood
        /// under it.
        ///
        /// ⚠️ THE CARD KEEPS ITS WIDTH THOUGH, AND THAT IS DELIBERATE. It is
        /// <see cref="RightRailWidth"/>, the chat's, so the top and bottom of the right-hand side
        /// share one edge and one width. The dead space is taken out of the PADDING, not out of
        /// the alignment that the rest of this pass exists to establish.
        ///
        /// ⚠️ AND THE GUTTERS ARE STILL BIGGER THAN THE GLYPHS THEY HOLD. `EDIT` is 40 px and gets
        /// 56; `›` is 22 and gets 52. A gutter cut to the exact measurement is a gutter that
        /// overlaps the first time somebody picks a longer character name, which is the failure
        /// this file already records four times.
        /// </summary>
        private const float CardPadding = 14.0f;
        private const float CardCaptionHeight = 20.0f;
        private const float CardFieldHeight = 44.0f;
        private const float CardCharacterHeight = 66.0f;
        private const float CardEditGutter = 52.0f;
        private const float CardChevronGutter = 46.0f;

        /// <summary>
        /// Type in the player card.
        ///
        /// ⚠️ CHESKA CAME DOWN FROM 32 TO 24 ACROSS TWO PASSES, ON REQUEST BOTH TIMES: *"make font
        /// smaaller for Cheska it looks ugly lowkey"*, then *"i asked u to tighten this and make
        /// stuff smaller, hhave you?"*. It sits just under <see cref="ValueSize"/> 26, which is
        /// what the match-settings values are drawn at, because the character is a smaller claim
        /// than the map: the row under it has to carry a can and a slipper as well.
        ///
        /// ⚠️ AND IT IS ABOVE `MenuKit.MinReadableUnits` 18 WITH ROOM TO SHRINK. `Parts.SetLoadout`
        /// fits it against the real string, so a longer roster name than `LOLA PACING` costs type
        /// size rather than running out of the card.
        /// </summary>
        private const int CardNameSize = 20;
        private const int CardCharacterSize = 24;

        /// <summary>
        /// Where each piece of the right-hand rail sits, all measured up from
        /// <see cref="BottomMargin"/> with one <see cref="RailSpacing"/> between them.
        ///
        /// ⚠️ COMPUTED, NOT TABULATED, AND THE PREVIOUS THREE LITERALS ARE WHY. They were 60 and
        /// 142 against a chat at 40, and none of the three knew how tall the chat actually was:
        /// the numbers happened to clear it and would have stopped the moment `LobbyChat.MaxLines`
        /// changed. Stacking them off the real height is one expression that cannot drift.
        /// </summary>
        private static float SocialToggleBottom => ToggleBaseline;
        private static float SocialDetailsBottom => SocialToggleBottom + ToggleHeight + RailSpacing;

        private const float TabWidth = 260.0f;

        /// <summary>
        /// Applies the arrangement. Safe to call once per screen load and nowhere else.
        /// </summary>
        /// <param name="root">The screen's own transform, already indexed by the caller.</param>
        /// <param name="find">How to reach a node by its Godot name.</param>
        /// <param name="onTab">Raised with the chosen tab: false for practice, true for lobby.</param>
        public static Parts Apply(Transform root, Func<string, Transform> find,
                                  bool isLobby, Action<bool> onTab)
        {
            if (Style != LobbyStyle.Street) return null;
            if (root == null || find == null) return null;

            SoftenScrim(root, find);
            HideBanner(find);

            var parts = new Parts();

            parts.LobbyDrawer = MoveColumns(root, find, parts);
            EnlargePrimaryActions(find);

            // ⚠️ AFTER `MoveColumns`, WHICH IS WHAT PUTS THE AUTHORED ROWS WHERE THIS CAN REACH
            // THEM. The character button is lifted out of the settings drawer's row list, and
            // `LiftSettings` has to have built that list first or this pulls a node out of a
            // parent that is about to be reparented underneath it.
            BuildIdentity(root, find, parts);

            BuildTabs(root, find, isLobby, onTab, parts);
            parts.SetActive(isLobby);
            return parts;
        }

        /// <summary>Makes START MATCH and READY read as the lobby's primary action instead of as
        /// another ordinary row. Both exist because the same screen serves multiplayer and
        /// practice; only the active one is visible.</summary>
        private static void EnlargePrimaryActions(Func<string, Transform> find)
        {
            foreach (string name in new[] { "StartButton", "PrimaryButton" })
            {
                var node = find(name);
                if (node == null) continue;

                var element = node.GetComponent<LayoutElement>();
                if (element == null) element = node.gameObject.AddComponent<LayoutElement>();
                element.minHeight = ActionHeight;
                element.preferredHeight = ActionHeight;
                element.flexibleHeight = 0.0f;
                element.minWidth = LeftWidth;
                element.preferredWidth = LeftWidth;
                element.flexibleWidth = 0.0f;

                var label = node.GetComponentInChildren<Text>();
                if (label != null)
                {
                    // The converted label's rect is driven by the old column layout and can
                    // collapse to zero when either new drawer rebuilds Columns. Keep the authored
                    // button and skin, but replace that fragile text child with one whose anchors
                    // belong to the button itself.
                    string text = label.text;
                    label.gameObject.SetActive(false);

                    var stable = MenuKit.Label(node, text, 42, UiTheme.Cream,
                                               Vector2.zero, Vector2.zero, Vector2.zero,
                                               TextAnchor.MiddleCenter);
                    stable.name = "ActionRailLabel";
                    stable.raycastTarget = false;
                    stable.horizontalOverflow = HorizontalWrapMode.Overflow;
                    MenuKit.Stretch(stable.rectTransform, 12.0f);
                    MenuKit.Fit(stable, LeftWidth - 48.0f, 24);
                }
            }
        }

        /// <summary>
        /// The player card: your name, and the fighter you are taking in.
        ///
        /// See <see cref="CardWidth"/> for why CHARACTER lives here rather than in the match
        /// settings, and why the two halves belong in one card.
        ///
        /// ⚠️⚠️ THE OLD CHIP PUT THE CAPTION AND THE FIELD SIDE BY SIDE IN A 52 px PILL AND IT
        /// READ AS ONE BROKEN LABEL. 🧑 2026-08-28, pointing at it: *"Pic 4 fix player name"*. The
        /// caption took the left 31 per cent of a 420 px box, which is 130 px, and the words
        /// PLAYER NAME need about 100 at the 14 units it was set to: below `MenuKit.
        /// MinReadableUnits` 18, which is the floor `AspectRatioProbes` asserts for exactly this
        /// reason. What was left for the actual field was 280 px carrying a placeholder, a caret
        /// and up to `Balance.PlayerNameMax` characters, and the two ran into each other.
        ///
        /// Stacking them is the fix: the caption gets a whole line at a readable size, and the
        /// field gets the whole width. It is also what makes room for the character block, which
        /// could not have gone anywhere near a control that was already overfull.
        ///
        /// ⚠️ THE PENCIL IS GONE. `Darumadrop One` has no glyph at U+270E (checked against the
        /// font's own cmap: 525 glyphs, no `✎`, no `✓`, no `◀`), so it was drawn by whatever
        /// system font Unity's dynamic-font fallback picked, at a different weight and a different
        /// baseline from every other character beside it. `EDIT` is four letters the game's own
        /// font actually has.
        /// </summary>
        private static void BuildIdentity(Transform root, Func<string, Transform> find, Parts parts)
        {
            var banner = find("Banner");
            Transform parent = banner != null ? banner.parent : root;

            string playerName = GameServices.Account?.LobbyName ?? Settings.SettingsStore.Current.PlayerName;
            if (string.IsNullOrWhiteSpace(playerName)) playerName = "Player";
            else playerName = playerName.Trim();

            var card = new GameObject("LobbyIdentity");
            card.transform.SetParent(parent, false);

            var image = card.AddComponent<Image>();

            // ⚠️ THE CARD IS A RAISED PLANK AND THE FIELD INSIDE IT IS A RECESSED ONE, so the
            // thing you type in reads as cut INTO the thing it is on. See `UiMaterials`.
            image.sprite = UiMaterials.Plank(UiTheme.WoodDeep);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = false;

            var rect = image.rectTransform;
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-EdgeMargin, -TopMargin);
            rect.sizeDelta = new Vector2(CardWidth, 100.0f);

            var column = card.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset((int)CardPadding, (int)CardPadding, 12, 14);
            column.spacing = 3;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            column.childAlignment = TextAnchor.UpperLeft;

            // ⚠️ THE CARD SIZES ITSELF TO WHAT IS IN IT. The character block's height depends on
            // whether the loadout line has anything to say, and a fixed height would either clip
            // it or leave a strip of bare wood under it.
            var fitter = card.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CardCaption(card.transform, "PLAYER NAME");
            BuildNameField(card.transform, playerName, parts);

            CardCaption(card.transform, "PLAYING AS");
            BuildCharacterButton(card.transform, find, parts);

            // ⚠⚠ YOUR SKILLS SITS DIRECTLY UNDER PLAYING AS, BECAUSE A LOADOUT IS A FACT ABOUT
            // THE CHARACTER ON THE ROW ABOVE IT. 🧑 2026-09-01, twice, after the feature had
            // already shipped: *"i also dont know hhow to navigae to loadouts section"*, and then
            // *"btw fix ui for loadouts I oculdnt find it, place button for it whereveer it
            // should belonng"*. **He commissioned the feature and could not reach it**, which is
            // the strongest findability signal this project can get.
            //
            // ⚠️ IT IS A DEEP LINK INTO THE HUB'S LOADOUT TAB, NOT A SECOND SCREEN. § 6.3 bans a
            // second DOOR to a hard-to-find destination and the answer it prescribes is to move
            // the door; the destination moved (out of a collapsed group on the career tab, into a
            // tab of its own) and this row is the lobby's way in. `PlayerHub.OpenLoadout`.
            //
            // ⚠⚠ AND IT IS HIDDEN IN CLASSIC RATHER THAN DISABLED. `docs/VISION.md` § 1.1:
            // Classic has no kit and never gets one, so a greyed SKILLS row on a Classic lobby
            // would be advertising a feature that mode does not have. `ConvertedMatchSetup`
            // switches it with the mode.
            // ⚠⚠⚠ THE ROOM CODE IS THE FIRST THING A HUMAN NEEDS IN A MULTIPLAYER LOBBY AND IT
            // WAS INSIDE A CLOSED DRAWER. 🧑 2026-09-01: *"make the ui genuinely good and easy to go
            // thru as a human"*. Walk the journey out loud, which is `CLAUDE.md` § 6.3's method:
            // *"I want my friend to join me"* was **open LOBBY & SERVERS, find the code row, read
            // it out** — three presses and a hunt, for the single fact the screen exists to
            // produce. It is on the card now, in the corner the player is already reading, and it
            // is only there when there IS one.
            //
            // ⚠️ AND IT COPIES ITSELF. A four-character code read off a screen and typed into
            // Discord is four chances to get it wrong; `GUIUtility.systemCopyBuffer` is one line
            // and it turns the answer into one press. The label says COPIED for a moment, because
            // a press that silently succeeds is `docs/TODO.md` § 53.5's dead button from the other
            // side.
            CardCaption(card.transform, "ROOM CODE");
            BuildCodeButton(card.transform, parts);

            CardCaption(card.transform, "YOUR SKILLS");
            BuildLoadoutButton(card.transform, parts);

            CardCaption(card.transform, "YOUR PROFILE");
            BuildProfileButton(card.transform, parts);
        }

        /// <summary>
        /// The room code, on the card, one press from the clipboard.
        ///
        /// ⚠️ IT IS THE ONLY CONTROL ON THIS CARD WHOSE VALUE IS THE POINT, so the code is drawn
        /// at 30 units in amber rather than at the card's 18-unit body size: it is read across a
        /// room and into a phone. `MenuKit.Fit` still bounds it, because a Relay join code is four
        /// characters and a LAN one may not be.
        /// </summary>
        private static void BuildCodeButton(Transform parent, Parts parts)
        {
            var go = new GameObject("RoomCodeButton", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.sprite = UiMaterials.Plank(UiTheme.WoodDeep, raised: false);
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            var element = go.AddComponent<LayoutElement>();
            element.minHeight = CardFieldHeight;
            element.preferredHeight = CardFieldHeight;
            element.flexibleHeight = 0.0f;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            go.AddComponent<TextureButtonFeedback>();
            FocusRing.Attach(go, 3.0f);

            var label = MenuKit.Label(go.transform, "", 30, UiTheme.Amber,
                                      Vector2.zero, Vector2.zero, Vector2.zero,
                                      TextAnchor.MiddleCenter);
            label.name = "RoomCodeValue";
            label.raycastTarget = false;

            // ⚠️ THE VALUE STOPS WHERE THE HINT STARTS. Both were stretched across the whole
            // row, so a centred code and a right-aligned "tap to copy" were sharing one box: at
            // four characters they clear each other by luck, and a LAN code is not four
            // characters. `CLAUDE.md` § 6.2c question 4, in miniature.
            MenuKit.Stretch(label.rectTransform, 0.0f);
            label.rectTransform.offsetMin = new Vector2(14.0f, 0.0f);
            label.rectTransform.offsetMax = new Vector2(-118.0f, 0.0f);

            var hint = MenuKit.Label(go.transform, "tap to copy", MenuKit.MinReadableUnits,
                                     UiTheme.CreamMuted, Vector2.zero, Vector2.zero,
                                     Vector2.zero, TextAnchor.MiddleRight);
            hint.name = "RoomCodeHint";
            hint.raycastTarget = false;
            MenuKit.Stretch(hint.rectTransform, 0.0f);
            hint.rectTransform.offsetMax = new Vector2(-14.0f, 0.0f);

            parts.CodeButton = button;
            parts.CodeValue = label;
            parts.CodeHint = hint;
            parts.CodeCaption = parent.GetChild(parent.childCount - 2).gameObject;

            button.onClick.AddListener(() => parts.CopyCode());
        }

        /// <summary>
        /// The lobby's way into the loadout, and the summary of what is equipped.
        ///
        /// ⚠️ IT IS BUILT LIKE THE CHARACTER ROW AND NOT LIKE THE PROFILE ROW, because it is the
        /// same kind of thing: a current value with a chevron saying it can be changed. The
        /// profile row is a destination with no value of its own, so it reads as a list of places.
        /// </summary>
        private static void BuildLoadoutButton(Transform parent, Parts parts)
        {
            var go = new GameObject("LoadoutButton", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.sprite = GodotTheme.WoodBox(UiTheme.WoodMid, UiTheme.WoodEdge);
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            var element = go.AddComponent<LayoutElement>();
            element.minHeight = CardFieldHeight;
            element.preferredHeight = CardFieldHeight;
            element.flexibleHeight = 0.0f;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            go.AddComponent<TextureButtonFeedback>();

            var label = MenuKit.Label(go.transform, "", MenuKit.MinReadableUnits, UiTheme.Cream,
                                      Vector2.zero, Vector2.zero, Vector2.zero,
                                      TextAnchor.MiddleLeft);
            label.name = "LoadoutValue";
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            MenuKit.Stretch(label.rectTransform, 0.0f);
            label.rectTransform.offsetMin = new Vector2(18.0f, 0.0f);
            label.rectTransform.offsetMax = new Vector2(-CardChevronGutter, 0.0f);

            var chevron = MenuKit.Label(go.transform, "›", 30, UiTheme.Amber,
                                        Vector2.zero, Vector2.zero, Vector2.zero,
                                        TextAnchor.MiddleRight);
            chevron.name = "LoadoutChevron";
            chevron.raycastTarget = false;
            MenuKit.Stretch(chevron.rectTransform, 0.0f);
            chevron.rectTransform.offsetMax = new Vector2(-18.0f, 0.0f);

            // ⚠️ EVERY PRESSABLE THING IN THE LOBBY WEARS A RING WHEN IT HAS THE POINTER OR THE
            // KEYBOARD, and until this pass none of them said anything at all: `TextureButtonFeedback`
            // tints a very dark brown by a few per cent, which is a change nobody can see.
            // `game-ui-design` calls a focus state that is only a colour a `colorblind-failure`.
            FocusRing.Attach(go, 3.0f);

            parts.LoadoutButton = button;
            parts.LoadoutValue = label;
            parts.LoadoutCaption = parent.GetChild(parent.childCount - 2).gameObject;
        }

        /// <summary>
        /// The door to `PlayerHub`, and it is the ONLY one in the game as of 2026-09-01.
        ///
        /// ⚠️⚠️ IT IS A THIRD BLOCK ON A CARD THE PLAYER ALREADY READS, NOT A NEW PLATE BESIDE
        /// IT. 🧑: *"I think the player shit should live in lobby screen, not play"*, and *"AND
        /// LOBBY IS WHERE ALL UI SHOULD LIVE"*. The obvious build would have been to install
        /// `PlayerNameplate` on this screen too, and that is exactly `docs/TODO.md` § 92's fault
        /// arriving one control at a time: **two identity plates on one screen, in two visual
        /// languages, each sized against its own corner.** This card already answers "who am I
        /// and what am I playing"; "and how am I doing" is the same question.
        ///
        /// ⚠️⚠️ THE LABEL IS TWO LINES BECAUSE LEVEL AND TIER MUST NEVER BE CONFUSABLE.
        /// `PlayerNameplate.Refresh` carries the argument in full and it is not repeated here:
        /// **LEVEL is how long you have played and only goes up; a TIER is how good you are and
        /// moves both ways.** The value line keeps `LV` on the number and spells the tier as a
        /// word, so `LV 14 · KAMPEON` cannot be read as one quantity.
        ///
        /// ⚠️ AND A FRESH ACCOUNT READS `PROFILE · CAREER · MATCHES` RATHER THAN `LV 1`. § 96:
        /// the plate that read as a status readout was never pressed by the person who
        /// commissioned it. A door says what is behind it until there is something of the
        /// player's own to say instead.
        /// </summary>
        private static void BuildProfileButton(Transform parent, Parts parts)
        {
            var go = new GameObject("ProfileButton", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.sprite = GodotTheme.WoodBox(UiTheme.WoodMid, UiTheme.WoodEdge);
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            var element = go.AddComponent<LayoutElement>();
            element.minHeight = CardFieldHeight;
            element.preferredHeight = CardFieldHeight;
            element.flexibleHeight = 0.0f;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            // ⚠️ IT REACTS TO THE POINTER, BECAUSE A CONTROL THAT DOES NOT MOVE IS NOT A CONTROL.
            // § 6.3, and § 96's receipt one control down: the plate this replaces had a bare
            // `Button` tint on very dark brown, which is a change nobody can see.
            go.AddComponent<TextureButtonFeedback>();

            // ⚠️⚠️ THE HINT IS MEASURED AGAINST THE CARD, WHICH IS `CLAUDE.md` § 6.2c QUESTION 4.
            // `CardWidth` is 330 and `CardPadding` is 14 each side, so the label's box is 302
            // less this row's own 14 of inset either side: **274 units**. `PROFILE · CAREER ·
            // MATCHES` at 18 units is about 250, and the version with double spaces around each
            // dot was about 300 and would have drawn straight off the wood. Nothing on this card
            // is sized against 1920, for the reason `UiRows.Cap` records.
            var label = MenuKit.Label(go.transform, "PROFILE · CAREER · MATCHES",
                MenuKit.MinReadableUnits, UiTheme.Cream, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
            MenuKit.Stretch(label.rectTransform, -14.0f);
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;

            // ⚠️ AND IT SHRINKS RATHER THAN OVERFLOWING, because `MenuKit.Label` OVERFLOWS by
            // default and the failure is silent: the control does not shrink, it draws over its
            // neighbour or off the edge. `Fit` stops at `MinReadableUnits`, so a string that
            // cannot fit at 18 is still a defect and still visible.
            MenuKit.Fit(label, CardWidth - (CardPadding * 2.0f) - 28.0f);

            FocusRing.Attach(go, 3.0f);

            parts.ProfileButton = button;
            parts.ProfileValue = label;
        }

        /// <summary>One amber caption line inside the player card. ⚠️ AT
        /// <see cref="CaptionSize"/> AND NOT BELOW: `MenuKit.MinReadableUnits` is 18 and the chip
        /// this replaced ran its caption at 14.</summary>
        private static void CardCaption(Transform parent, string words)
        {
            var label = MenuKit.Label(parent, words, MenuKit.MinReadableUnits, UiTheme.Amber,
                                      Vector2.zero, Vector2.zero, Vector2.zero,
                                      TextAnchor.LowerLeft);
            label.name = $"Caption_{words}";
            label.raycastTarget = false;

            var element = label.gameObject.AddComponent<LayoutElement>();
            element.minHeight = CardCaptionHeight;
            element.preferredHeight = CardCaptionHeight;
            element.flexibleHeight = 0.0f;
        }

        private static void BuildNameField(Transform parent, string playerName, Parts parts)
        {
            var fieldGo = new GameObject("PlayerNameEdit");
            fieldGo.transform.SetParent(parent, false);

            var fieldImage = fieldGo.AddComponent<Image>();
            // ⚠️ `WoodDeep` RECESSED AND NOT `WoodDark`, measured off the lobby render: `WoodDark`
            // is `1d0e06`, near black, and a recessed plank of it drew as a black slot in the
            // middle of the card. Same finding as the sign-in screen's mode well, same day.
            fieldImage.sprite = UiMaterials.Plank(UiTheme.WoodDeep, raised: false);
            fieldImage.type = Image.Type.Sliced;
            fieldImage.color = Color.white;

            var element = fieldGo.AddComponent<LayoutElement>();
            element.minHeight = CardFieldHeight;
            element.preferredHeight = CardFieldHeight;
            element.flexibleHeight = 0.0f;

            // ⚠️⚠️ THE "EDIT" AFFORDANCE WAS DELETED ON 2026-08-29. 🧑: *"remove edit here bcz it
            // lowk does nothing"*, *"tap already works"*.
            //
            // It was never a button. Its own note said so — *"it is decorative: the field itself
            // takes the click, over its whole width"* — which is exactly the problem: a word
            // styled in Amber at the end of a field reads as a control, and the one thing it
            // cannot do is be pressed. The placeholder below already says TAP TO SET YOUR NAME,
            // so the affordance was stated twice and the second statement was a lie about where
            // to click.
            //
            // ⚠️ THE FIELD'S CLICK TARGET IS UNCHANGED and is what makes this safe to remove:
            // the whole field takes the press, which is what `docs/TODO.md` § 72 spent a session
            // proving (`UiClickProbe` reports the topmost raycast hit at the field's centre as
            // the field itself). Removing a `raycastTarget = false` label cannot alter that.

            var placeholder = MenuKit.Label(fieldGo.transform, "TAP TO SET YOUR NAME", MenuKit.MinReadableUnits,
                                            UiTheme.CreamMuted, Vector2.zero, Vector2.zero,
                                            Vector2.zero, TextAnchor.MiddleLeft);
            placeholder.raycastTarget = false;
            MenuKit.Stretch(placeholder.rectTransform, 0.0f);
            placeholder.rectTransform.offsetMin = new Vector2(14.0f, 0.0f);
            placeholder.rectTransform.offsetMax = new Vector2(-CardEditGutter, 0.0f);
            MenuKit.Fit(placeholder, CardWidth - (CardPadding * 2.0f) - CardEditGutter);

            var typed = MenuKit.Label(fieldGo.transform, playerName, CardNameSize, UiTheme.Cream,
                                      Vector2.zero, Vector2.zero, Vector2.zero,
                                      TextAnchor.MiddleLeft);
            typed.raycastTarget = false;
            typed.supportRichText = false;

            // ⚠️ BEST-FIT RATHER THAN A ONE-SHOT `Fit`, BECAUSE THE STRING CHANGES UNDER IT. A
            // name is typed a character at a time and `FitEverything` does not run per keystroke,
            // so a fitted size measured against "Ma" is still in force at "Matthew Labrador".
            typed.resizeTextForBestFit = true;
            typed.resizeTextMinSize = 13;
            typed.resizeTextMaxSize = CardNameSize;
            MenuKit.Stretch(typed.rectTransform, 0.0f);
            typed.rectTransform.offsetMin = new Vector2(14.0f, 0.0f);
            typed.rectTransform.offsetMax = new Vector2(-CardEditGutter, 0.0f);

            var field = fieldGo.AddComponent<InputField>();
            field.targetGraphic = fieldImage;
            field.placeholder = placeholder;
            field.textComponent = typed;
            field.text = playerName == "Player" &&
                         string.IsNullOrWhiteSpace(Settings.SettingsStore.Current.PlayerName)
                ? ""
                : playerName;
            field.characterLimit = Core.AccountRules.DisplayNameMax;
            field.lineType = InputField.LineType.SingleLine;
            field.onEndEdit.AddListener(raw =>
            {
                // ⚠⚠ A FIELD BEING SWITCHED OFF RAISES THIS TOO, AND THAT IS A TEARDOWN ARTEFACT
                // RATHER THAN SOMEBODY FINISHING TYPING. `InputField.OnDisable` calls
                // `DeactivateInputField`, which fires `onEndEdit` with the text it already had,
                // so closing the lobby scene ran a full `ConvertedMatchSetup.Refresh` from inside
                // Unity's own deactivation.
                //
                // That was harmless until § 84.3 made `Refresh` fit synchronously: the fit calls
                // `Canvas.ForceUpdateCanvases`, which tried to start a coroutine on an object
                // Unity was in the middle of disabling — *"Coroutine couldn't be started because
                // the the game object 'LobbyIdentity' is inactive"* — and an unhandled error log
                // fails every PlayMode test in the file. `MatchRunTests` and `PreviewDragProbe`
                // both went red on it and neither has anything to do with a name field.
                //
                // ⚠ THE GUARD IS AT THE SOURCE RATHER THAN INSIDE `Refresh`. An `isActiveAndEnabled`
                // check on the SCREEN does not catch this, because the screen is still active
                // while one of its children is being disabled; the object that knows is the field
                // itself.
                if (field == null || !field.isActiveAndEnabled) return;

                string clean = Settings.GameSettings.SanitiseName(raw);
                var account = GameServices.Account;
                if (account != null)
                {
                    _ = account.SetProfileAsync(clean, account.Bio, account.Country, account.Pronouns);
                }
                else
                {
                    Settings.SettingsStore.Current.PlayerName = clean;
                    Settings.SettingsStore.Save();
                }
                field.SetTextWithoutNotify(clean);
                parts.NameCommitted?.Invoke();
            });
        }

        /// <summary>
        /// Lifts the authored `CharacterButton` into the player card and gives it two lines.
        ///
        /// ⚠️⚠️ THE NODE IS MOVED, NOT REPLACED, AND THAT IS WHAT KEEPS IT WIRED.
        /// `ConvertedMatchSetup.Wire` calls `OnClick("CharacterButton", OpenCharacterSelect)`
        /// BEFORE the chrome runs, and `ConvertedScreen` holds `Transform` references from its own
        /// index: reparenting one does not change what `Node("CharacterButton")` returns, and
        /// rebuilding it would leave a handler attached to a button nobody can see. That is
        /// `docs/TODO.md` § 68.4's rule, and it is why this reads as a repositioning.
        ///
        /// ⚠️⚠️ THE AUTHORED `Label` IS DEACTIVATED AND TWO STABLE ONES REPLACE IT, which is
        /// `EnlargePrimaryActions`' finding applied a second time: that child's rect is driven by
        /// the layout chain the button just left, and it collapses to zero width on the frame
        /// either drawer rebuilds. The pair is also the point of the redesign. One line of
        /// `CHESKA · KALAWANG · CROCS ▸` at 24 units in a 370 px box is 27 characters that
        /// `MenuKit.Fit` has to grind down toward its floor, and what it produces is a caption
        /// where the character's NAME, which is the thing you chose, is the same size as the
        /// slipper you did not think about.
        ///
        /// ⚠️ `›` RATHER THAN `▸`. Neither `▸` (U+25B8) nor `▶` (U+25B6) is in Darumadrop One;
        /// `›` (U+203A) is. See <see cref="BuildIdentity"/>'s note on the font's cmap.
        /// </summary>
        private static void BuildCharacterButton(Transform parent, Func<string, Transform> find,
                                                 Parts parts)
        {
            var node = find("CharacterButton") as RectTransform;
            if (node == null) return;

            // ⚠️ THE ROW IT CAME OUT OF GOES WITH IT. `FighterRow` is a caption plus this button;
            // left behind it would draw the word CHARACTER: over an empty stretch of the settings
            // drawer. Deactivated rather than destroyed, per § 68.4: `Classic` still uses it.
            var fighterRow = node.parent;
            node.SetParent(parent, false);
            if (fighterRow != null && fighterRow.name == "FighterRow")
                fighterRow.gameObject.SetActive(false);

            var element = node.GetComponent<LayoutElement>();
            if (element == null) element = node.gameObject.AddComponent<LayoutElement>();
            element.minHeight = CardCharacterHeight;
            element.preferredHeight = CardCharacterHeight;
            element.flexibleHeight = 0.0f;
            element.minWidth = 0.0f;
            element.preferredWidth = -1.0f;
            element.flexibleWidth = 1.0f;

            var authored = node.GetComponentInChildren<Text>(true);
            if (authored != null && authored.name == "Label") authored.gameObject.SetActive(false);

            var name = MenuKit.Label(node, "", CardCharacterSize, UiTheme.Cream,
                                     Vector2.zero, Vector2.zero, Vector2.zero,
                                     TextAnchor.MiddleLeft);
            name.name = "CharacterName";
            name.raycastTarget = false;
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            name.rectTransform.anchorMin = new Vector2(0.0f, 0.46f);
            name.rectTransform.anchorMax = Vector2.one;
            name.rectTransform.offsetMin = new Vector2(18.0f, 0.0f);
            name.rectTransform.offsetMax = new Vector2(-CardChevronGutter, -4.0f);

            var loadout = MenuKit.Label(node, "", MenuKit.MinReadableUnits, UiTheme.CreamMuted,
                                        Vector2.zero, Vector2.zero, Vector2.zero,
                                        TextAnchor.MiddleLeft);
            loadout.name = "CharacterLoadout";
            loadout.raycastTarget = false;
            loadout.horizontalOverflow = HorizontalWrapMode.Overflow;
            loadout.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            loadout.rectTransform.anchorMax = new Vector2(1.0f, 0.46f);
            loadout.rectTransform.offsetMin = new Vector2(18.0f, 6.0f);
            loadout.rectTransform.offsetMax = new Vector2(-CardChevronGutter, 0.0f);

            var chevron = MenuKit.Label(node, "›", 30, UiTheme.Amber,
                                        Vector2.zero, Vector2.zero, Vector2.zero,
                                        TextAnchor.MiddleRight);
            chevron.name = "CharacterChevron";
            chevron.raycastTarget = false;
            MenuKit.Stretch(chevron.rectTransform, 0.0f);
            chevron.rectTransform.offsetMax = new Vector2(-18.0f, 0.0f);

            parts.CharacterName = name;
            parts.CharacterLoadout = loadout;
        }

        /// <summary>
        /// ⚠️⚠️ THE BANNER IS HIDDEN IN `Street`, NOT SHRUNK. 🧑 2026-08-28, pointing at the yellow
        /// LOBBY pennant: *"also remove this lobby thing bcz we all know this is lobby already"*.
        /// It is a 648x144 piece of art whose only content is a word the `MULTIPLAYER` tab
        /// directly under it already says, and it sat in the one corner nothing else could use.
        /// BACK moves into the space it leaves (see <see cref="BackWidth"/>).
        ///
        /// ⚠️ `SetActive(false)`, NOT DESTROYED, AND `BannerLabel` IS STILL WRITTEN.
        /// `ConvertedScreen` indexes every node in `Start` before `Wire` runs, so the transform
        /// stays resolvable and `Refresh`'s `SetHeadline("BannerLabel", ...)` keeps working rather
        /// than logging a missing node on every redraw. `Classic` never calls this and keeps the
        /// pennant, which is the whole point of that style existing.
        /// </summary>
        private static void HideBanner(Func<string, Transform> find)
        {
            var banner = find("Banner");
            if (banner == null) return;

            banner.gameObject.SetActive(false);
        }

        /// <summary>
        /// Moves BACK out of the bottom-left action stack and into the top-left corner.
        ///
        /// See <see cref="BackWidth"/> for why. ⚠️ IT RUNS BEFORE `Corner`, because `Narrow`
        /// rewrites the `sizeDelta` of every child of the column it is given, and a BACK button
        /// still in that column would be stretched to <see cref="LeftWidth"/> on its way out.
        /// </summary>
        private static void LiftBack(Transform root, Func<string, Transform> find,
                                     Transform leftColumn)
        {
            var back = Descend(leftColumn, "BackButton") as RectTransform;
            if (back == null) return;

            var banner = find("Banner");
            Transform parent = banner != null ? banner.parent : root;

            back.SetParent(parent, false);

            back.anchorMin = new Vector2(0.0f, 1.0f);
            back.anchorMax = new Vector2(0.0f, 1.0f);
            back.pivot = new Vector2(0.0f, 1.0f);
            back.anchoredPosition = new Vector2(EdgeMargin, -TopMargin);
            back.sizeDelta = new Vector2(BackWidth, HeaderHeight);

            // ⚠️ THE LAYOUT ELEMENT GOES DEAD. It carried the minimums the old column sized it
            // against, and a `LayoutElement` on a node with no layout group parent is inert but
            // misleading; `ignoreLayout` says out loud that nothing above drives this rect now.
            var element = back.GetComponent<LayoutElement>();
            if (element != null) element.ignoreLayout = true;

            var label = back.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.fontSize = 24;
                MenuKit.Fit(label, BackWidth - 48.0f);
            }

            // The spacer existed only to push BACK to the bottom of the stack it has just left.
            var spacer = Descend(leftColumn, "Spacer");
            if (spacer != null) spacer.gameObject.SetActive(false);
        }

        /// <summary>
        /// Every piece of `Street` chrome the screen has to talk to after it is built, handed back
        /// to the screen that owns it.
        ///
        /// ⚠️⚠️ NONE OF THIS IS KEPT IN A STATIC. `LobbyChrome` is a static helper and a static
        /// field holding a scene object survives the scene that made it: a second load of
        /// `MatchSetup` would find a reference to a destroyed button that still answers a C#
        /// `!= null` check only until Unity's overload runs, and the tab would look wired and do
        /// nothing. The screen holds these for exactly as long as it exists.
        ///
        /// ⚠️ IT WAS CALLED `Tabs` AND IT STOPPED BEING ONLY THAT when the player card gained the
        /// character button, whose two labels the screen has to write on every refresh. A class
        /// named for one of the four things it carries is how the next reader concludes the other
        /// three are incidental.
        /// </summary>
        public sealed class Parts
        {
            public Button Practice;
            public Button Multiplayer;
            public GameObject LobbyDrawer;

            /// <summary>The big line on the character button: who you are playing.</summary>
            public Text CharacterName;

            /// <summary>The small line under it: the can and the slipper.</summary>
            public Text CharacterLoadout;

            /// <summary>YOUR SKILLS: the row that opens `PlayerHub` on its LOADOUT tab, its
            /// summary label, and the amber caption above it so both can be hidden together in
            /// Classic. See <see cref="BuildLoadoutButton"/>.</summary>
            public Button LoadoutButton;
            public Text LoadoutValue;
            public GameObject LoadoutCaption;

            /// <summary>PHASE 12's RULES stepper. ⚠️ Handed back by reference rather than found by
            /// name: the row is a clone made after `ConvertedScreen` built its name index. See
            /// <see cref="BuildFormatRow"/>.</summary>
            /// <summary>The room code row: the plate, the code, its hint and the caption above it,
            /// so all four can be hidden together when there is no code. See
            /// <see cref="BuildCodeButton"/>.</summary>
            public Button CodeButton;
            public Text CodeValue;
            public Text CodeHint;
            public GameObject CodeCaption;

            private string _code = "";
            private float _copiedUntil;

            /// <summary>
            /// Writes the code, or takes the whole row off the card.
            ///
            /// ⚠️ THE CAPTION GOES WITH IT, for the same reason `SetSkills` says: hiding the
            /// value alone leaves an amber ROOM CODE heading over whatever is underneath.
            /// </summary>
            public void SetCode(string code)
            {
                _code = code ?? "";
                bool has = !string.IsNullOrWhiteSpace(_code);

                if (CodeButton != null) CodeButton.gameObject.SetActive(has);
                if (CodeCaption != null) CodeCaption.SetActive(has);
                if (!has || CodeValue == null) return;

                if (Time.unscaledTime < _copiedUntil) return;

                CodeValue.text = _code;
                CodeValue.color = UiTheme.Amber;
                MenuKit.Fit(CodeValue, CardWidth - (CardPadding * 2.0f) - 132.0f);

                if (CodeHint != null) CodeHint.text = "tap to copy";
            }

            /// <summary>
            /// ⚠️ THE RECEIPT IS ON THE CONTROL ITSELF AND LASTS A MOMENT. A copy that reports
            /// nothing is indistinguishable from a copy that failed, and the status line at the
            /// bottom of this screen is for network faults (`SetAlert`), not for confirmations.
            /// </summary>
            public void CopyCode()
            {
                if (string.IsNullOrWhiteSpace(_code)) return;

                GUIUtility.systemCopyBuffer = _code;
                _copiedUntil = Time.unscaledTime + 1.6f;

                if (CodeHint != null) CodeHint.text = "copied";
                MenuSfx.Click();
            }

            /// <summary>The chalk bar under whichever top tab is live, and half the distance
            /// between the two tabs' centres. See <see cref="BuildTabs"/>.</summary>
            public Image TabMarker;
            public float TabMarkerPitch;
            private float _tabMarkerY;

            /// <summary>Moves the bar under the live tab. ⚠️ The Y is passed once and remembered,
            /// so a caller switching tabs does not have to know the header's geometry.</summary>
            public void SetTabMarker(bool multiplayer, float y = 0.0f)
            {
                if (TabMarker == null) return;

                if (y > 0.0f) _tabMarkerY = y;

                TabMarker.rectTransform.anchoredPosition =
                    new Vector2(multiplayer ? TabMarkerPitch : -TabMarkerPitch, -_tabMarkerY);
            }

            /// <summary>The bottom-left action rail: the settings drawer, the primary action and,
            /// since the UI pass, the queue. See <see cref="QueueCard.Dock"/>.</summary>
            public Transform LeftRail;

            public Button FormatPrev;
            public Button FormatNext;
            public Text FormatValue;

            /// <summary>The door to `PlayerHub`, and the line on it. See
            /// <see cref="BuildProfileButton"/>.</summary>
            public Button ProfileButton;
            public Text ProfileValue;

            /// <summary>Raised when the player finishes editing their name in the card, so the
            /// screen can push it to the lobby rather than waiting for the next redraw.</summary>
            public Action NameCommitted;

            /// <summary>
            /// Rewrites the closed drawer's one-line summary from the three value labels.
            ///
            /// ⚠️⚠️ IT WAS BUILT ONCE AND THEN ONLY UPDATED ON CLOSE, SO IT SHIPPED THE AUTHORED
            /// PLACEHOLDERS. `Logs/shots-runtime/Lobby-v35.png` reads `ESKINITA · CAPTURE ·
            /// NORMAL` on a lobby whose settings are Hero Strike and HARD, because `CAPTURE` is
            /// the string `MatchSetup.unity` ships in `ModeValueLabel` and this summary was
            /// composed inside `LobbyChrome.Apply`, which `ConvertedMatchSetup.Wire` runs BEFORE
            /// its first `Refresh`. A player who never opened the drawer was told the wrong mode
            /// for the whole session, and `CAPTURE` is not even a mode this game has: it is a
            /// leftover from the deleted 2v2 design, so it reads as a third mode nobody can find.
            ///
            /// ⚠️ IT HANGS OFF `Refresh` NOW, WHICH IS WHAT ACTUALLY CHANGES THE VALUES. Map,
            /// mode and difficulty all move through `Refresh`, whether from an arrow on this
            /// machine or a `SyncMap` from the host, and the summary is a view of them.
            /// </summary>
            public Action RefreshSummary;

            /// <summary>The two pieces of right-hand furniture that have to sit above the chat.
            /// See <see cref="StackRight"/>.</summary>
            public RectTransform LobbyToggleRect;
            public RectTransform LobbyDetailsRect;

            private float _stackedFor = -1.0f;

            /// <summary>
            /// Stacks LOBBY & SERVERS, and the card above it, on top of the chat.
            ///
            /// ⚠️⚠️ AGAINST THE CHAT'S MEASURED HEIGHT, NOT ITS CAPACITY, AND THAT DISTINCTION IS
            /// THE WHOLE BUG. `LobbyChat` reserves six line slots and then collapses onto whatever
            /// is in them, so an empty log is about 65 px and the arithmetic said 224.
            /// `Logs/shots-runtime/Lobby-v36.png` has the pill floating over the fourth character
            /// with 160 px of bare road under it. `LobbyChat.PanelHeight` is the real number.
            ///
            /// ⚠️ AND IT IS RE-ASKED, BECAUSE THE CHAT GROWS AS LINES ARRIVE. A single placement at
            /// build time is correct for exactly as long as nobody says anything. The guard makes
            /// re-asking every frame free.
            /// </summary>
            public void StackRight(float chatHeight)
            {
                if (Mathf.Approximately(_stackedFor, chatHeight)) return;
                _stackedFor = chatHeight;

                // ⚠️ THE HIGHER OF THE TWO. `ToggleBaseline` puts this pill on the same line as
                // MATCH SETTINGS, which is what was asked for and what is true of an empty chat;
                // the second term is what stops the sixth chat line growing up underneath it.
                float toggleBottom = Mathf.Max(ToggleBaseline,
                                               BottomMargin + chatHeight + RailSpacing);

                if (LobbyToggleRect != null)
                    LobbyToggleRect.anchoredPosition = new Vector2(-EdgeMargin, toggleBottom);

                if (LobbyDetailsRect != null)
                    LobbyDetailsRect.anchoredPosition =
                        new Vector2(-EdgeMargin, toggleBottom + ToggleHeight + RailSpacing);
            }

            /// <summary>
            /// Writes the character block.
            ///
            /// ⚠️ THE NAME IS FITTED AND THE LOADOUT IS NOT ALLOWED TO GROW INTO IT. Both are
            /// `Overflow` labels in a fixed 430 px card, and a hero name plus a can plus a slipper
            /// is three arbitrary roster strings: `ConvertedScreen.SetHeadline` records this
            /// project shipping that overflow four separate times.
            /// </summary>
            /// <summary>
            /// Writes the YOUR SKILLS row, or takes it off the card.
            ///
            /// ⚠️ THE CAPTION GOES WITH IT. Hiding the button alone leaves an amber YOUR SKILLS
            /// heading over the profile row, which is a label naming the wrong control: the exact
            /// shape of § 94.7's *"a value drawn 1600 px from its label"* in miniature.
            /// </summary>
            public void SetSkills(bool shown, string summary)
            {
                if (LoadoutButton != null) LoadoutButton.gameObject.SetActive(shown);
                if (LoadoutCaption != null) LoadoutCaption.SetActive(shown);

                if (!shown || LoadoutValue == null) return;

                LoadoutValue.text = summary ?? "";
                LoadoutValue.fontSize = MenuKit.MinReadableUnits;
                MenuKit.Fit(LoadoutValue, CardWidth - (CardPadding * 2.0f) - CardChevronGutter);
            }

            public void SetLoadout(string character, string loadout)
            {
                if (CharacterName != null)
                {
                    CharacterName.text = character;
                    CharacterName.fontSize = CardCharacterSize;
                    MenuKit.Fit(CharacterName, CardWidth - (CardPadding * 2.0f) - CardChevronGutter);
                }

                if (CharacterLoadout != null)
                {
                    CharacterLoadout.text = loadout;
                    CharacterLoadout.fontSize = MenuKit.MinReadableUnits;
                    MenuKit.Fit(CharacterLoadout, CardWidth - (CardPadding * 2.0f) - CardChevronGutter);
                }
            }

            /// <summary>
            /// ⚠️ THE VARIATION IS SWAPPED AND RE-APPLIED, NOT THE IMAGE COLOUR. `GodotButton`
            /// carries five authored states per variation and writes the Image itself on hover,
            /// press and disable; tinting the graphic directly is overwritten by whichever state
            /// the skin resolves next, which reads as a tab that forgets it is selected the first
            /// time the mouse crosses it.
            /// </summary>
            public void SetActive(bool lobby)
            {
                Paint(Practice, !lobby);
                Paint(Multiplayer, lobby);

                // ⚠️ THE BAR MOVES WITH THE PAINT. `SelectTab` switches these two IN PLACE rather
                // than rebuilding the screen, so a marker positioned only at build time would sit
                // under PRACTICE for the rest of the session.
                SetTabMarker(lobby);

                if (LobbyDrawer != null) LobbyDrawer.SetActive(lobby);
            }

            private static void Paint(Button button, bool active)
            {
                if (button == null) return;

                var skin = button.GetComponent<GodotButton>();
                if (skin == null) return;

                skin.Variation = active ? "WoodAmberButton" : "WoodButton";
                skin.Apply();
                skin.Refresh();
            }
        }

        /// <summary>
        /// ⚠️⚠️ THE SCRIM CHANGES SHAPE, NOT STRENGTH, AND THAT IS THE WHOLE DIFFERENCE BETWEEN
        /// "the arena is the background" AND "the arena is the picture". It is authored as one
        /// full-screen dim over the live map, which is correct when two opaque panels sit in the
        /// middle of the frame and there is nothing else to look at. With four characters standing
        /// in the middle of that frame it is a grey sheet over the only thing worth seeing.
        ///
        /// Two vertical gradients do the same job for the text: dark at the top where the banner
        /// and the tabs sit, dark at the bottom where the furniture sits, and clean through the
        /// middle band where the cast is. The alpha at the edges is the authored value, so nothing
        /// gets HARDER to read than it already was.
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

            // The flat sheet goes; the two bands carry its weight at the edges.
            image.color = new Color(authored.r, authored.g, authored.b, authored.a * 0.18f);
            image.raycastTarget = false;

            // ⚠️⚠️ THE BANDS ARE INK, NOT THE AUTHORED SCRIM COLOUR, AND REUSING THAT COLOUR
            // WASHED THE BOTTOM THIRD OF THE SCREEN OUT. `Logs/shots-runtime/Lobby-v8.png` has a
            // pale grey haze over the road and the cast's legs with a visible horizontal edge
            // where it starts, because the authored scrim is a LIGHT wash: correct as a flat dim
            // over a whole screen with two opaque panels on it, and exactly backwards as a
            // gradient whose job is to make cream text read over a bright street.
            //
            // ⚠️ `UiTheme.Ink` IS THE RIGHT COLOUR BY THE PALETTE'S OWN RULE. Its entry is "text,
            // borders, pressed fills": it is already what every dark edge in this UI is made of,
            // and darkening under light type is the one thing a scrim is for.
            var band = new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 1.0f);

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

        /// <summary>
        /// A one-pixel-wide vertical alpha ramp, cached.
        ///
        /// ⚠️ THE CURVE IS SQUARED RATHER THAN LINEAR. A linear ramp has a visible edge where it
        /// reaches zero, because the eye finds the discontinuity in the FIRST DERIVATIVE, not in
        /// the value. Squaring puts the fade's own falloff to zero at the same point and the band
        /// ends without a line across the screen.
        /// </summary>
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
                // v is 0 at the screen edge and 1 where the band meets the clean middle.
                float v = y / (float)(steps - 1);
                float toEdge = fromTop ? v : 1.0f - v;

                float alpha = toEdge * toEdge;
                tex.SetPixel(0, y, new Color(1.0f, 1.0f, 1.0f, alpha));
            }

            tex.Apply();

            cached = Sprite.Create(tex, new Rect(0, 0, 1, steps), new Vector2(0.5f, 0.5f), 100.0f);
            cached.name = tex.name;

            return cached;
        }

        /// <summary>
        /// Pushes the two authored columns into the bottom corners and lets the middle of the
        /// frame belong to the arena.
        ///
        /// ⚠️⚠️ THE PARENT'S LAYOUT GROUP IS DISABLED, NOT DELETED. `Columns` is a
        /// `HorizontalLayoutGroup` that centres its two children and drives their rects every
        /// frame; leaving it on would fight every anchor set below and win, because a layout group
        /// writes its children's rects during the layout pass and an anchor set from a script runs
        /// before it. Disabling rather than destroying is what makes `Classic` a one-line revert:
        /// the component and its authored spacing, padding and alignment are all still there.
        ///
        /// ⚠️ AND EACH COLUMN GETS A `ContentSizeFitter`. Once the parent stops driving them their
        /// height is whatever the rect says, which for a layout-driven node is the 100x100
        /// placeholder every converted container carries. Fitting to preferred height is what
        /// makes a column as tall as the rows inside it, and anchoring the pivot to the BOTTOM is
        /// what makes it grow upward from the corner instead of down off the screen.
        /// </summary>
        private static GameObject MoveColumns(Transform root, Func<string, Transform> find,
                                              Parts parts)
        {
            var columns = find("Columns");
            var left = find("LeftColumn");
            var right = find("RightColumn");

            if (columns == null || left == null || right == null)
            {
                Debug.LogWarning("[LobbyChrome] the authored columns are missing; " +
                                 "keeping the Classic arrangement.");
                return null;
            }

            var group = columns.GetComponent<LayoutGroup>();
            if (group != null) group.enabled = false;

            var columnsRect = columns as RectTransform;
            if (columnsRect != null) MenuKit.Stretch(columnsRect, 0.0f);

            LiftBack(root, find, left);

            var banner = find("Banner");
            Transform canvasRoot = banner != null ? banner.parent : root;

            GameObject lobbyDrawer = null;

            if (right is RectTransform rightRect)
            {
                // ⚠️⚠️ THE RIGHT-HAND FURNITURE LEAVES `Columns` ENTIRELY, AND THAT IS WHY ITS
                // RIGHT MARGIN USED TO NEED A -47 FUDGE. `Columns` is a child of `Body`, which is a
                // full-screen `VerticalLayoutGroup`: disabling the group ON `Columns` stops it
                // driving its own children, and does nothing about `Body` driving `Columns` itself.
                // So an anchor of "48 px in from my parent's right edge" was 48 px in from a rect
                // somebody else was still moving, and `Logs/shots-runtime/Lobby-v36.png` has the
                // LOBBY & SERVERS pill about 145 px from the screen edge against the chat's 48.
                // A constant that compensates for a layout group is a constant that is wrong the
                // next time anything above it changes; reparenting to the canvas removes the
                // argument instead of settling it.
                rightRect.SetParent(canvasRoot, false);

                // The authored column keeps its scale so its contents stay the size they were
                // tuned at. See `RightScale`.
                Corner(rightRect, RightWidth, toLeft: false, toTop: false);
                rightRect.anchoredPosition = new Vector2(-EdgeMargin, SocialDetailsBottom);

                lobbyDrawer = BuildLobbyDrawer(canvasRoot, rightRect, parts);
            }

            BuildLeftRail(root, find, left, parts);
            return lobbyDrawer;
        }

        /// <summary>Keeps network mechanics available without permanently covering the fourth
        /// character. The card opens on demand; chat follows the bottom edge of whichever state
        /// is visible.</summary>
        private static GameObject BuildLobbyDrawer(Transform canvasRoot, RectTransform details,
                                                   Parts parts)
        {
            if (details == null || canvasRoot == null) return null;

            // The authored social card reserves room for rows removed by the lobby redesign.
            // Fit the live share/action content so opening it does not needlessly cover P4.
            var seatPanel = Descend(details, "SeatPanel");
            if (seatPanel != null)
            {
                var panelElement = seatPanel.GetComponent<LayoutElement>();
                if (panelElement == null)
                    panelElement = seatPanel.gameObject.AddComponent<LayoutElement>();
                panelElement.minHeight = 410.0f;
                panelElement.preferredHeight = 410.0f;
                panelElement.flexibleHeight = 0.0f;
            }

            // ⚠️⚠️ UNSCALED, AND THE SAME SIZE AND TYPE AS MATCH SETTINGS. 🧑 2026-08-28: *"align
            // the yellow thing with match settings use same font size too"*, *"tighten lobby and
            // servers so muc yellow empty space"*. It used to be authored at `RightWidth` and drawn
            // at `RightScale` so it matched the CARD it opens; that made it 392 wide against the
            // left toggle's 460, at the wood variation's authored type against the left one's 26,
            // sitting 63 px lower. Two controls that do the same thing looked like two different
            // controls. The card behind it keeps its scale, because its contents are authored; the
            // pill has nothing in it but a word.
            var toggle = MenuKit.WoodButton(canvasRoot, "LOBBY & SERVERS  ▼", Vector2.zero,
                                            Vector2.zero,
                                            new Vector2(RightRailWidth, ToggleHeight),
                                            null, "WoodAmberButton");
            toggle.name = "LobbyDrawerToggle";
            FocusRing.Attach(toggle.gameObject, 3.0f);

            var toggleCaption = toggle.GetComponentInChildren<Text>();
            if (toggleCaption != null)
            {
                toggleCaption.fontSize = 26;
                MenuKit.Fit(toggleCaption, RightRailWidth - 48.0f);
            }

            var toggleRect = toggle.transform as RectTransform;
            if (toggleRect != null)
            {
                toggleRect.anchorMin = new Vector2(1.0f, 0.0f);
                toggleRect.anchorMax = new Vector2(1.0f, 0.0f);
                toggleRect.pivot = new Vector2(1.0f, 0.0f);
                toggleRect.anchoredPosition = new Vector2(-EdgeMargin, SocialToggleBottom);
            }

            parts.LobbyToggleRect = toggleRect;
            parts.LobbyDetailsRect = details;

            var label = toggle.GetComponentInChildren<Text>();
            details.gameObject.SetActive(false);
            bool open = false;

            toggle.onClick.AddListener(() =>
            {
                open = !open;
                details.gameObject.SetActive(open);
                if (label != null)
                    label.text = open ? "CLOSE LOBBY DETAILS  ▲" : "LOBBY & SERVERS  ▼";
            });

            return toggle.gameObject;
        }

        /// <summary>
        /// Makes MAP / MODE / BOTS / CHARACTER an on-demand drawer immediately above the
        /// bottom-left START/READY rail.
        ///
        /// ⚠️⚠️ THE LEFT COLUMN WAS TWO DIFFERENT THINGS IN ONE STACK. 🧑 2026-08-28, pointing at
        /// the settings block sitting above START: *"maybe put this right below lobby? looks ugly
        /// there"*. They are two different KINDS of control and the reference separates them for a
        /// reason: the four cyclers are SETTINGS, which you read and adjust before you are ready,
        /// and START is the ACTION, which wants to be alone in the corner your hand rests in.
        /// Stacked together, the action reads as the fifth row of the settings.
        ///
        /// ⚠️ THE NODES ARE REPARENTED, NOT REBUILT. `ConvertedScreen` indexes every node by name
        /// in `Start`, BEFORE this runs, and it holds `Transform` references: moving one to a new
        /// parent does not change what `Node("MapValueLabel")` returns. Rebuilding them would.
        ///
        /// ⚠️ AND THE DETAIL LINE GOES WITH THEM. "ESKINITA  Urban side street" is a caption on
        /// the MAP row, not a status line; leaving it at the bottom would strand it under a START
        /// button describing a map picker that is no longer next to it.
        /// </summary>
        private static void BuildLeftRail(Transform root, Func<string, Transform> find,
                                          Transform leftColumn, Parts parts)
        {
            var config = find("ConfigPanel");
            if (config == null || leftColumn == null) return;

            var banner = find("Banner");
            Transform canvasRoot = banner != null ? banner.parent : root;

            // ⚠️⚠️ ONE CONTAINER, NOT TWO, AND THAT IS THE WHOLE FIX FOR THE RAGGED LEFT EDGE.
            // See the harmony block at the top of this file: the settings drawer and the action
            // stack used to be two hosts at two anchors with two scales, so their left edges
            // differed by 20 px and their widths by 80. A single `VerticalLayoutGroup` with
            // `childForceExpandWidth` on gives every child the rail's width by construction, and
            // there is no arithmetic left to get wrong.
            //
            // ⚠️ IT IS A DIRECT CHILD OF THE CANVAS, NOT OF `Body`. `Body` is a full-screen
            // `VerticalLayoutGroup` and anything parented into it is positioned by that group,
            // which would fight the corner anchor below and win.
            var host = new GameObject("LobbyLeftRail");
            host.transform.SetParent(canvasRoot, false);

            var rect = host.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;

            // ⚠️ THE PIVOT IS THE BOTTOM-LEFT CORNER, so the rail GROWS UPWARD when the drawer
            // opens. With a centred pivot, opening the settings would have slid START MATCH down
            // off the bottom of the screen by half the drawer's height.
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(EdgeMargin, BottomMargin);
            rect.sizeDelta = new Vector2(LeftWidth, 100.0f);

            var layout = host.AddComponent<VerticalLayoutGroup>();
            layout.spacing = RailSpacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.LowerLeft;

            // ⚠⚠ THE TOGGLE AND ITS SUMMARY ARE ONE BLOCK, NOT TWO RAIL CHILDREN. 🧑 2026-08-28:
            // *"space BETWEEN MATCH settinsg and start mathch too biug"*. As siblings they took a
            // full RailSpacing above AND below the summary, so the caption ON the button was as
            // far from it as START MATCH was, and the eye read three unrelated rows instead of a
            // labelled control and an action. Nesting them costs one GameObject and makes the gap
            // that matters (HeaderGap 2) independent of the gap between pieces of furniture.
            var header = new GameObject("SettingsHeader");
            header.transform.SetParent(host.transform, false);
            header.AddComponent<RectTransform>();

            var headerLayout = header.AddComponent<VerticalLayoutGroup>();
            headerLayout.spacing = HeaderGap;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = true;
            headerLayout.childForceExpandHeight = false;

            var headerElement = header.AddComponent<LayoutElement>();
            headerElement.minHeight = ToggleHeight + HeaderGap + SummaryHeight;
            headerElement.preferredHeight = ToggleHeight + HeaderGap + SummaryHeight;
            headerElement.flexibleHeight = 0.0f;

            var fitter = host.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // The drawer header is the only furniture visible until the player asks to edit the
            // match. This follows the lobby hierarchy in the reference: cast first, settings on
            // demand, primary action always available.
            var toggle = MenuKit.WoodButton(header.transform, "MATCH SETTINGS  ▼", Vector2.zero,
                                            Vector2.zero,
                                            new Vector2(LeftWidth, ToggleHeight), null,
                                            "WoodAmberButton");
            toggle.name = "SettingsDrawerToggle";
            FocusRing.Attach(toggle.gameObject, 3.0f);

            // ⚠️ THE CAPTION IS SIZED AGAINST THE RAIL RATHER THAN LEFT AT THE VARIATION'S
            // AUTHORED SIZE. `WoodAmberButton` is drawn for short words like BACK, so
            // `MATCH SETTINGS ▼` sat small in the middle of a 460 px pill with air on both sides,
            // which is half of what 🧑 meant by *"big ass empty space left and right"*.
            var toggleCaption = toggle.GetComponentInChildren<Text>();
            if (toggleCaption != null)
            {
                toggleCaption.fontSize = 26;
                MenuKit.Fit(toggleCaption, LeftWidth - 48.0f);
            }

            var toggleElement = toggle.gameObject.AddComponent<LayoutElement>();
            toggleElement.minHeight = ToggleHeight;
            toggleElement.preferredHeight = ToggleHeight;
            toggleElement.flexibleHeight = 0.0f;

            // ⚠️ CENTRED, ON REQUEST: *"make this middle aligned"*. It is a caption ON the button
            // above it rather than a line of prose, and the button's own label is centred, so a
            // left-aligned caption under a centred label reads as two unrelated things that happen
            // to share a left edge.
            var summary = MenuKit.Label(header.transform, "", SummarySize, UiTheme.CreamMuted,
                                        Vector2.zero, Vector2.zero,
                                        new Vector2(LeftWidth, SummaryHeight),
                                        TextAnchor.MiddleCenter);
            summary.name = "SettingsSummary";
            summary.raycastTarget = false;
            var summaryElement = summary.gameObject.AddComponent<LayoutElement>();
            summaryElement.minHeight = SummaryHeight;
            summaryElement.preferredHeight = SummaryHeight;
            summaryElement.flexibleHeight = 0.0f;

            var body = new GameObject("SettingsBody");
            body.transform.SetParent(host.transform, false);
            var bodyImage = body.AddComponent<Image>();
            bodyImage.rectTransform.sizeDelta = new Vector2(LeftWidth, SettingsBodyHeight);

            // ⚠⚠ THE DRAWER'S BODY IS RECESSED AND ITS TOGGLE IS RAISED, WHICH IS THE WHOLE
            // POINT OF `UiMaterials`. Both were `GodotTheme.WoodBox`: the same nine-slice with the
            // same bevel on all four sides, so a drawer and the button that opens it were the same
            // object at two sizes and the screen read as a stack of identical planks. 🧑
            // 2026-09-01: *"our UI is ugly and repetitive and unimaginative"*. A groove is dark
            // along its top edge because the light is above it; a plank is bright along its top
            // edge for the same reason. Nothing here is a new colour.
            bodyImage.sprite = UiMaterials.Plank(UiTheme.WoodDeep, raised: false);
            bodyImage.type = Image.Type.Sliced;
            bodyImage.color = Color.white;

            var bodyElement = body.AddComponent<LayoutElement>();
            bodyElement.minHeight = SettingsBodyHeight;
            bodyElement.preferredHeight = SettingsBodyHeight;
            bodyElement.flexibleHeight = 0.0f;

            var bodyLayout = body.AddComponent<VerticalLayoutGroup>();
            bodyLayout.padding = new RectOffset(16, 16, 16, 16);
            bodyLayout.spacing = 8;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = false;

            // Use the authored rows and their wired buttons, but not ConfigPanel's oversized
            // 634x540 face. The face is what made a few controls read as a giant opaque card.
            var rows = Descend(config, "Rows");
            if (rows != null)
            {
                rows.SetParent(body.transform, false);

                // ⚠️ FOUR ROWS SINCE PHASE 12, NOT THREE. `BuildFormatRow` adds RULES under BOTS,
                // and this height is what the rows container claims: left at three, the fourth row
                // drew outside the wood and over the cast's legs. `SettingsBodyHeight` carries the
                // same arithmetic one rect out.
                float rowsHeight = (SettingsRowHeight * 4.0f) + 16.0f;

                var rowsElement = rows.GetComponent<LayoutElement>();
                if (rowsElement == null) rowsElement = rows.gameObject.AddComponent<LayoutElement>();
                rowsElement.minHeight = rowsHeight;
                rowsElement.preferredHeight = rowsHeight;
                rowsElement.flexibleHeight = 0.0f;

                var rowsLayout = rows.GetComponent<HorizontalOrVerticalLayoutGroup>();
                if (rowsLayout != null) rowsLayout.spacing = 8.0f;

                Narrow(rows as RectTransform, LeftWidth - 32.0f);

                DressSelectorRow(rows, "MapRow", "MapCaption", "MAP", "MapSelector",
                                 "MapPrevButton", "MapValueLabel", "MapNextButton");
                DressSelectorRow(rows, "ModeRow", "ModeCaption", "MODE", "ModeSelector",
                                 "ModePrevButton", "ModeValueLabel", "ModeNextButton");
                DressSelectorRow(rows, "DifficultyRow", "DifficultyCaption", "BOTS",
                                 "DifficultySelector", "DifficultyPrevButton",
                                 "DifficultyValueLabel", "DifficultyNextButton");

                BuildFormatRow(rows, parts);
            }

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

                // ⚠️ THE MAP'S DETAIL LINE IS PROSE AND HAS TO WRAP. It is authored `Overflow`
                // like every other converted label, and "LRT Gilmore strip. Viaduct pillars, PC
                // Express, pisonet." is 58 characters against a 714 px box: at the authored 21
                // units it drew straight past the panel's right border and over the cast.
                // `ConvertedMatchSetup.FitAsBlock` already lists `DetailLabel`, and `FitBlock`
                // only takes the height once the label is allowed to wrap in the first place.
                var detailLabel = Descend(detail, "DetailLabel")?.GetComponent<Text>();
                if (detailLabel != null)
                {
                    detailLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                    detailLabel.alignment = TextAnchor.UpperLeft;
                    detailLabel.color = UiTheme.CreamMuted;
                    detailLabel.fontSize = MenuKit.MinReadableUnits;
                    detailLabel.raycastTarget = false;
                }
            }

            config.gameObject.SetActive(false);

            Func<string, string> value = name =>
            {
                var node = find(name);
                var label = node != null ? node.GetComponent<Text>() : null;
                if (label == null && node != null) label = node.GetComponentInChildren<Text>();
                return label != null ? label.text.Trim() : "?";
            };

            Action refreshSummary = () =>
            {
                if (summary == null) return;

                summary.text = $"{value("MapValueLabel")}  •  {value("ModeValueLabel")}  •  " +
                               $"{value("DifficultyValueLabel")}";
                summary.fontSize = SummarySize;
                MenuKit.Fit(summary, LeftWidth - 8.0f, 14);
            };

            // See `Parts.RefreshSummary`: the screen calls this from `Refresh`, because that is
            // what actually changes the three values this line is a view of.
            parts.RefreshSummary = refreshSummary;

            var toggleLabel = toggle.GetComponentInChildren<Text>();
            bool open = false;
            body.SetActive(false);
            refreshSummary();

            toggle.onClick.AddListener(() =>
            {
                open = !open;
                body.SetActive(open);
                summary.gameObject.SetActive(!open);
                if (toggleLabel != null)
                    toggleLabel.text = open ? "CLOSE MATCH SETTINGS  ▲" : "MATCH SETTINGS  ▼";
                if (!open) refreshSummary();

                // Activating the wider drawer rebuilds the converted Columns layout. On some
                // frames Unity keeps the START plate at full width but collapses its child Text
                // to zero, leaving one letter visible. Flush that rebuild, then restore the
                // label's stretch anchors in the final layout.
                Canvas.ForceUpdateCanvases();
                RepairActionLabels(find);
            });

            // ⚠️⚠️ THE ACTION AND THE STATUS LINE JOIN THE SAME RAIL, WHICH IS THE OTHER HALF OF
            // THE HARMONY FIX. They stayed in the authored `LeftColumn` before this, at a different
            // anchor and a different scale, which is why `Lobby-v35.png` has START MATCH starting
            // 20 px to the left of the MATCH SETTINGS pill above it and running 80 px wider.
            //
            // ⚠️ BOTH ACTION BUTTONS ARE MOVED, AND `RefreshActionButtons` STILL DECIDES WHICH IS
            // VISIBLE. Only one is ever active, so the group leaves no gap for the hidden one.
            foreach (string name in new[] { "StartButton", "PrimaryButton", "StatusLabel" })
            {
                var node = Descend(leftColumn, name);
                if (node == null) continue;

                node.SetParent(host.transform, false);

                // ⚠️ THE PRIMARY WEARS A RING LIKE EVERYTHING ELSE. It is the one control on this
                // screen a player looks for without reading, so it is also the one where a
                // missing focus state is most obviously a keyboard dead end.
                if (node.GetComponent<Button>() != null) FocusRing.Attach(node.gameObject, 4.0f);
            }

            // ⚠️ THE AUTHORED COLUMN IS EMPTIED AND SWITCHED OFF, NOT DESTROYED. `Classic` is a
            // working screen at every commit (`docs/TODO.md` § 68.3) and `LobbyChrome.Style` is the
            // one line that chooses; destroying the column would make the revert a repair.
            leftColumn.gameObject.SetActive(false);

            // ⚠⚠ THE RAIL IS HANDED BACK SO THE QUEUE CAN LIVE IN IT. 🧑 2026-09-01: *"our UI is
            // ugly and repetitive and unimaginative"*, and the loudest single thing on the lobby
            // was QUICK MATCH, a 560-unit amber bar floating in the middle of the screen over the
            // cast, while the actual primary action sat in the corner. **Two primaries is not a
            // hierarchy**, and `game-ui-design`'s ordering is position first: this rail is the
            // PLAY column, so both ways of starting a game belong in it, one under the other,
            // with the accent spent on one of them. See `QueueCard.Dock`.
            parts.LeftRail = host.transform;

            var status = Descend(host.transform, "StatusLabel");
            if (status != null)
            {
                var statusElement = status.GetComponent<LayoutElement>();
                if (statusElement == null)
                    statusElement = status.gameObject.AddComponent<LayoutElement>();
                statusElement.minHeight = 56.0f;
                statusElement.preferredHeight = 56.0f;
                statusElement.flexibleHeight = 0.0f;

                var statusText = status.GetComponent<Text>();
                if (statusText != null)
                {
                    // ⚠️ IT WRAPS. `AutoHost`'s refusal message names the port, the likely cause
                    // and the way out, which is well over one line of a 560 px rail; authored
                    // `Overflow` drew it straight across the cast's feet.
                    statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
                    statusText.alignment = TextAnchor.UpperCenter;
                    statusText.fontSize = 20;
                    statusText.raycastTarget = false;
                }

                // ⚠️⚠️ IT STARTS HIDDEN AND ONLY AN ALERT OPENS IT. 🧑 2026-08-28, pointing under
                // START MATCH: *"remove undertext for start match"*. The line was carrying `Lobby
                // open. Share the code, or press JOIN.` permanently, which is a sentence describing
                // a state the screen already shows: the join code is in the drawer, the JOIN button
                // is on it, and the tab says MULTIPLAYER.
                //
                // ⚠️ IT IS NOT DELETED THOUGH, AND THE REASON IS THE FOUR FAILURE MESSAGES.
                // `AutoHost` writes the refused-port reason here, `HandleClientDisconnected` writes
                // why a connection ended, `ToggleOnline` writes why a relay room could not open,
                // and `OnPrimaryPressed` writes "still connecting". Those are the only things on
                // this screen a player has to ACT on, and deleting the label would leave all four
                // with nowhere to land. `ConvertedMatchSetup.WriteStatus` shows it for an alert and
                // hides it for news, so the chatter goes and the errors stay.
                status.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Turns one authored selector row into the shape the whole panel is built from: a fixed
        /// caption column on the left, and a stepper of a fixed height on the right.
        ///
        /// See <see cref="SettingsRowHeight"/> for the two measured faults this fixes (the 52-unit
        /// caption over the 34-unit value, and the three ragged caption widths). What follows is
        /// what each line is for.
        ///
        /// ⚠️⚠️ THE AUTHORED NODES ARE RESTYLED, NEVER REBUILT. `MapPrevButton`,
        /// `MapValueLabel` and `MapNextButton` carry the wiring `ConvertedMatchSetup.Wire` put on
        /// them, the `TextureButtonFeedback` that makes an arrow press, and the `GodotOutline`
        /// that gives the type its ink edge. Building fresh ones would be four names to reproduce
        /// per row and a silent break of `docs/TODO.md` § 68.4 if any were spelled differently.
        ///
        /// ⚠️ EVERY STEP IS GUARDED SEPARATELY, so a row whose caption was renamed loses its
        /// caption and keeps its stepper rather than throwing halfway and leaving the panel in
        /// neither layout.
        /// </summary>
        /// <summary>
        /// PHASE 12's RULES row: STANDARD, LAST TSINELAS STANDING or MIRROR.
        ///
        /// ⚠⚠ IT IS A CLONE OF THE AUTHORED BOTS ROW RATHER THAN A ROW BUILT FROM SCRATCH, AND
        /// THAT IS THE POINT. The three selector rows are `.tscn` nodes with authored arrow
        /// TEXTURES on them (`Arrow`: *"the arrows are textures, not type, so the font's missing
        /// glyph cannot reach them"*), an authored recessed plate and an authored inner layout.
        /// A fourth row written in code would be a fourth visual language on a rail whose whole
        /// redesign was about the first three not lining up, and `docs/VISION.md` § 6 is the
        /// standing rule: **his UI art IS the design system**. `Instantiate` gets all of it free
        /// and cannot drift from the other three.
        ///
        /// ⚠⚠ AND THE BUTTONS COME BACK ON `Parts` RATHER THAN BEING FOUND BY NAME. Every other
        /// control on this screen is wired with `OnClick("SomeButton", ...)`, which reads
        /// `ConvertedScreen`'s name index, and **that index is built in `Start` before this method
        /// runs**. A clone made afterwards is not in it, so a name lookup would answer null and
        /// the row would be a stepper whose arrows do nothing: `docs/TODO.md` § 108's EQUIP button
        /// exactly, in a place nobody would think to look for it.
        ///
        /// ⚠️ THE WORD IS "RULES" AND NOT "FORMAT". `docs/Formats.md` calls the concept a format
        /// because it needed a name that was not "mode"; the player reading a rail that already
        /// says MAP, MODE and BOTS is better served by the plainest word for it. The value says
        /// which one.
        /// </summary>
        private static void BuildFormatRow(Transform rows, Parts parts)
        {
            var source = Descend(rows, "DifficultyRow");
            if (source == null) return;

            var clone = UnityEngine.Object.Instantiate(source.gameObject, rows);
            clone.name = "FormatRow";
            clone.transform.SetAsLastSibling();

            Rename(clone.transform, "DifficultyCaption", "FormatCaption");
            Rename(clone.transform, "DifficultySelector", "FormatSelector");
            Rename(clone.transform, "DifficultyPrevButton", "FormatPrevButton");
            Rename(clone.transform, "DifficultyValueLabel", "FormatValueLabel");
            Rename(clone.transform, "DifficultyNextButton", "FormatNextButton");

            DressSelectorRow(rows, "FormatRow", "FormatCaption", "RULES",
                             "FormatSelector", "FormatPrevButton",
                             "FormatValueLabel", "FormatNextButton");

            // ⚠️ THE CLONED LISTENERS GO. `Instantiate` copies a `Button`'s persistent
            // `onClick` entries with it, so without this both arrows would still be cycling the
            // BOT DIFFICULTY they were cloned from, on a row labelled RULES. Nothing would log.
            var prev = Descend(clone.transform, "FormatPrevButton")?.GetComponent<Button>();
            var next = Descend(clone.transform, "FormatNextButton")?.GetComponent<Button>();

            if (prev != null) prev.onClick.RemoveAllListeners();
            if (next != null) next.onClick.RemoveAllListeners();

            parts.FormatPrev = prev;
            parts.FormatNext = next;
            parts.FormatValue = Descend(clone.transform, "FormatValueLabel")?.GetComponent<Text>();
        }

        private static void Rename(Transform root, string from, string to)
        {
            var node = Descend(root, from);
            if (node != null) node.name = to;
        }

        private static void DressSelectorRow(Transform rows, string row, string caption,
                                             string word, string selector, string prev,
                                             string value, string next)
        {
            var rowNode = Descend(rows, row) as RectTransform;
            if (rowNode == null) return;

            var rowElement = rowNode.GetComponent<LayoutElement>();
            if (rowElement == null) rowElement = rowNode.gameObject.AddComponent<LayoutElement>();
            rowElement.minHeight = SettingsRowHeight;
            rowElement.preferredHeight = SettingsRowHeight;
            rowElement.flexibleHeight = 0.0f;

            var rowLayout = rowNode.GetComponent<HorizontalLayoutGroup>();
            if (rowLayout != null)
            {
                rowLayout.padding = new RectOffset(0, 0, 0, 0);
                rowLayout.spacing = 14.0f;
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = false;
                rowLayout.childForceExpandHeight = true;
                rowLayout.childAlignment = TextAnchor.MiddleLeft;
            }

            // ⚠️⚠️ THE CAPTION COLUMN IS FIXED AND THAT IS THE HALF NOBODY SEES. MAP:, MODE: and
            // BOTS: are three different widths, and the authored group sized each caption to its
            // own string, so the three steppers started at three different x positions. Nothing in
            // the panel lined up with anything, which is most of what "ugly" was.
            var captionNode = Descend(rowNode, caption);
            var captionText = captionNode == null ? null : captionNode.GetComponent<Text>();

            if (captionText != null)
            {
                // ⚠️⚠️ THE COLON GOES, AND IT IS WORTH 54 px OF THE RAIL. The scene authors these
                // as `'MAP:'`, `'MODE:'` and `'BOTS:'`, and the caption column has to be as wide as
                // the longest of them; dropping the colon takes the longest from `BOTS:` to `BOTS`
                // and let `SettingsCaptionWidth` come down from 150 to 96. 🧑 2026-08-28, of the
                // rail: *"do u not feel weird that theres b ig ass empty space left and right"*.
                // That space came out of the caption column, which was mostly air, rather than out
                // of the value's type size, which is what a reader is actually looking at.
                //
                // ⚠️ THE COLON IS ALSO REDUNDANT HERE IN A WAY IT WAS NOT IN THE AUTHORED PANEL.
                // A colon says "what follows is the value"; the value now sits in its own recessed
                // well with an arrow either side, which says it louder.
                captionText.text = word;
                captionText.fontSize = CaptionSize;
                captionText.color = UiTheme.Amber;
                captionText.alignment = TextAnchor.MiddleLeft;
                captionText.horizontalOverflow = HorizontalWrapMode.Overflow;
                captionText.verticalOverflow = VerticalWrapMode.Overflow;
                captionText.raycastTarget = false;

                var element = captionNode.GetComponent<LayoutElement>();
                if (element == null) element = captionNode.gameObject.AddComponent<LayoutElement>();
                element.minWidth = SettingsCaptionWidth;
                element.preferredWidth = SettingsCaptionWidth;
                element.flexibleWidth = 0.0f;

                MenuKit.Fit(captionText, SettingsCaptionWidth - 8.0f);
            }

            var selectorNode = Descend(rowNode, selector) as RectTransform;
            if (selectorNode != null)
            {
                // The authored plate is the recessed well the value sits in. Repainted dark so the
                // value reads as SET INTO the panel rather than as another plank on top of it.
                var plate = selectorNode.GetComponent<Image>();
                if (plate != null)
                {
                    plate.sprite = GodotTheme.WoodBox(UiTheme.WoodDark, UiTheme.WoodEdge);
                    plate.type = Image.Type.Sliced;
                    plate.color = Color.white;
                    plate.raycastTarget = false;
                }

                var element = selectorNode.GetComponent<LayoutElement>();
                if (element == null)
                    element = selectorNode.gameObject.AddComponent<LayoutElement>();
                element.minWidth = 0.0f;
                element.preferredWidth = -1.0f;
                element.flexibleWidth = 1.0f;
                element.minHeight = SettingsRowHeight - 8.0f;
                element.preferredHeight = SettingsRowHeight - 8.0f;

                // ⚠️ THE AUTHORED INSET IS 45x27 AND IT WAS CUTTING THE ROW IN HALF VERTICALLY.
                // `Inner` is a stretched child of the plate, so 27 off a 62 px stepper leaves the
                // arrows 35 px tall inside a 62 px well. 10 keeps a visible border and gives the
                // arrows the height they were drawn at.
                var inner = Descend(selectorNode, "Inner") as RectTransform;
                if (inner != null)
                {
                    inner.anchorMin = Vector2.zero;
                    inner.anchorMax = Vector2.one;
                    inner.offsetMin = new Vector2(10.0f, 8.0f);
                    inner.offsetMax = new Vector2(-10.0f, -8.0f);

                    var innerLayout = inner.GetComponent<HorizontalLayoutGroup>();
                    if (innerLayout != null)
                    {
                        innerLayout.padding = new RectOffset(0, 0, 0, 0);
                        innerLayout.spacing = 6.0f;
                        innerLayout.childControlWidth = true;
                        innerLayout.childControlHeight = true;
                        innerLayout.childForceExpandWidth = false;
                        innerLayout.childForceExpandHeight = true;
                        innerLayout.childAlignment = TextAnchor.MiddleCenter;
                    }
                }
            }

            Arrow(rowNode, prev);
            Arrow(rowNode, next);

            var valueNode = Descend(rowNode, value);
            var valueText = valueNode == null ? null : valueNode.GetComponent<Text>();

            if (valueText != null)
            {
                valueText.fontSize = ValueSize;
                valueText.color = UiTheme.Cream;
                valueText.alignment = TextAnchor.MiddleCenter;
                valueText.horizontalOverflow = HorizontalWrapMode.Overflow;
                valueText.verticalOverflow = VerticalWrapMode.Overflow;
                valueText.raycastTarget = false;

                var element = valueNode.GetComponent<LayoutElement>();
                if (element == null) element = valueNode.gameObject.AddComponent<LayoutElement>();

                // ⚠️ FLEXIBLE, WITH THE TWO ARROWS FIXED EITHER SIDE. The authored row gave the
                // value a preferred width, so a short word like NONE left the two arrows floating
                // in the middle of the well and a long one like ILALIM NG TULAY pushed them out of
                // it. Pinning the arrows and letting the value take what is left is what makes the
                // three rows read as one control repeated.
                element.minWidth = 0.0f;
                element.preferredWidth = -1.0f;
                element.flexibleWidth = 1.0f;
            }
        }

        /// <summary>One stepper arrow: square, fixed, and never resized by the value beside it.
        /// ⚠️ THE ARROWS ARE TEXTURES, NOT TYPE (`TextureButtonFeedback` over an `Image`), so
        /// their size is a rect and not a font size, and the font's missing `◀` glyph cannot
        /// reach them.</summary>
        private static void Arrow(Transform row, string name)
        {
            var node = Descend(row, name);
            if (node == null) return;

            var element = node.GetComponent<LayoutElement>();
            if (element == null) element = node.gameObject.AddComponent<LayoutElement>();

            element.minWidth = SettingsArrowSize;
            element.preferredWidth = SettingsArrowSize;
            element.flexibleWidth = 0.0f;
            element.minHeight = SettingsArrowSize;
            element.preferredHeight = SettingsArrowSize;
            element.flexibleHeight = 0.0f;

            var image = node.GetComponent<Image>();
            if (image != null) image.preserveAspect = true;
        }

        private static void RepairActionLabels(Func<string, Transform> find)
        {
            foreach (string name in new[] { "StartButton", "PrimaryButton" })
            {
                var node = find(name);
                var stable = Descend(node, "ActionRailLabel");
                var label = stable != null ? stable.GetComponent<Text>() : null;
                if (label == null) continue;

                MenuKit.Stretch(label.rectTransform, 12.0f);
                MenuKit.Fit(label, LeftWidth - 48.0f, 24);
            }
        }

        /// <summary>
        /// Swaps a converted row between stacked and side by side.
        ///
        /// ⚠️⚠️ THE COMPONENT IS REPLACED, BECAUSE A LAYOUT GROUP'S ORIENTATION IS ITS TYPE.
        /// `HorizontalLayoutGroup` and `VerticalLayoutGroup` are two classes with no shared switch,
        /// so turning a row is destroy-and-add, and the authored spacing, padding and alignment are
        /// carried across by hand rather than re-picked: they are what makes the row look like the
        /// rest of this UI.
        ///
        /// ⚠️ `DestroyImmediate`, NOT `Destroy`. `Destroy` defers to the end of the frame, so
        /// the new group would spend this frame fighting the old one over the same children and
        /// the layout that lands is whichever ran last. This is a controlled one-shot at screen
        /// build, which is the case `DestroyImmediate` is for.
        /// </summary>
        /// <summary>
        /// The first descendant of <paramref name="root"/> with this name.
        ///
        /// ⚠️ SCOPED TO A SUBTREE ON PURPOSE. `ConvertedScreen.Node` searches the whole screen
        /// and `MatchSetup` carries two `Rows`, two `Face`s, two `Shadow`s and four `Label`s;
        /// which one you get is tree order, which is not a thing a reader can see. Everything this
        /// class reaches for inside a panel is found from that panel.
        /// </summary>
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
        /// Puts an AUTHORED column into a bottom corner at <see cref="RightScale"/>.
        ///
        /// ⚠️⚠️ ONLY THE RIGHT-HAND COLUMN GOES THROUGH THIS NOW. The left side is built as a real
        /// rail by `BuildLeftRail` and is not scaled at all; see the harmony block at the top of
        /// this file for why the two-scaled-hosts arrangement had to go. This is kept because the
        /// lobby drawer's contents are authored against a 500 px column and rebuilding them is a
        /// different job.
        ///
        /// ⚠️ THE `toTop` ARM WENT WITH THE TOP RAIL IT SERVED. Nothing is corner-anchored to the
        /// top any more: BACK, the tabs and the player card each place themselves against
        /// <see cref="TopMargin"/>.
        /// </summary>
        private static void Corner(RectTransform column, float width, bool toLeft, bool toTop)
        {
            if (column == null) return;

            float y = toTop ? 1.0f : 0.0f;

            column.anchorMin = new Vector2(toLeft ? 0.0f : 1.0f, y);
            column.anchorMax = new Vector2(toLeft ? 0.0f : 1.0f, y);
            column.pivot = new Vector2(toLeft ? 0.0f : 1.0f, y);
            column.anchoredPosition = new Vector2(toLeft ? EdgeMargin : -EdgeMargin,
                                                  toTop ? -TopMargin : BottomMargin);
            column.sizeDelta = new Vector2(width, column.sizeDelta.y);

            // ⚠️⚠️ THE SIZE IS SET **AND** THE COLUMN IS SCALED, AND THE SCALE IS THE HALF THAT
            // ACTUALLY WORKS. Three renders in a row (`Logs/shots-runtime/Lobby-v2..v5.png`) came
            // back with an 820 px panel inside a rect that `LobbyChrome.ReportColumns` measured at
            // 580, because a rect handed to a layout system is a REQUEST: the authored
            // `VerticalLayoutGroup`, its children's `LayoutElement` minimums and their own
            // `ContentSizeFitter`s each get to overrule it, and `Narrow` below only reaches the
            // first two. `localScale` is outside that argument entirely. Nothing in Unity's layout
            // reads it, so it cannot be overruled, and it shrinks the panel WITH its type, its
            // borders and its spacing, which is what "compact furniture" means and what setting a
            // width alone would not have done even if it had held.
            //
            // ⚠️ THE PIVOT IS THE CORNER THE COLUMN IS ANCHORED TO, so it shrinks TOWARD that
            // corner and the margin stays the margin. With a centred pivot the same scale would
            // have pulled the panel away from the edge by half the difference.
            column.localScale = new Vector3(RightScale, RightScale, 1.0f);

            var fitter = column.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = column.gameObject.AddComponent<ContentSizeFitter>();

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Narrow(column, width);
        }

        /// <summary>
        /// Makes the column's contents actually the column's width.
        ///
        /// ⚠️⚠️ SETTING THE COLUMN'S OWN `sizeDelta` IS NOT ENOUGH AND `Logs/shots-runtime/
        /// Lobby-v3.png` IS THE PROOF: the column was set to 580 and the config panel inside it
        /// still measured 820 on screen, so the clear band the cast stands in was 240 px narrower
        /// than the arithmetic said and the two left-hand characters were behind the furniture
        /// from the knee up.
        ///
        /// The cause is that the authored `VerticalLayoutGroup` ships with `childControlWidth`
        /// OFF, which means it POSITIONS its children and does not SIZE them: a child keeps
        /// whatever width the .tscn gave it and simply overhangs a parent that got smaller.
        /// Turning control on, and forcing expansion so a narrower child grows back to the new
        /// width rather than sitting in the middle of it, is what makes the number mean something.
        ///
        /// ⚠️ THE CHILDREN'S OWN `LayoutElement` IS OVERRIDDEN TOO, because a `preferredWidth`
        /// authored on the panel outranks the group's expansion and would win.
        /// </summary>
        private static void Narrow(RectTransform column, float width)
        {
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
                    // ⚠️⚠️ `minWidth` AS WELL AS `preferredWidth`, AND ONLY DOING THE SECOND IS
                    // WHY `Logs/shots-runtime/Lobby-v4.png` STILL HAS AN 820 px PANEL IN A 580 px
                    // COLUMN. Unity's layout resolves a child's width as at least its `minWidth`
                    // whatever the group wants, so an authored minimum outranks both the group's
                    // control and its expansion. The BACK button, which has no authored minimum,
                    // stretched to the new width in that same frame: two children of one group
                    // disagreeing is what named the cause.
                    if (element.minWidth > 0.0f) element.minWidth = width;
                    if (element.preferredWidth > 0.0f) element.preferredWidth = width;
                }

                // ⚠️ AND A `ContentSizeFitter` ON THE CHILD OUTRANKS EVERYTHING ABOVE, because it
                // writes the rect itself after the group has finished. The horizontal half has to
                // stand down; the vertical half is usually the only reason the fitter is there,
                // so it is left alone.
                var fitter = child.GetComponent<ContentSizeFitter>();
                if (fitter != null) fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                child.sizeDelta = new Vector2(width, child.sizeDelta.y);
            }
        }

        /// <summary>
        /// Reports what the columns actually ended up being, once the layout has run.
        ///
        /// ⚠️⚠️ THIS IS HERE BECAUSE THREE RENDERS IN A ROW DISAGREED WITH THE ARITHMETIC AND
        /// NOTHING COULD SAY WHY. A screenshot shows that a panel is too wide; it cannot show
        /// whether the column was set correctly and a child overhung it, whether the anchor was
        /// wrong, or whether a `ContentSizeFitter` rewrote the rect afterwards, and those three
        /// have three different fixes. `UiProbe`'s header makes the same argument about a white
        /// rectangle having four indistinguishable causes.
        /// </summary>
        public static void ReportColumns(Func<string, Transform> find)
        {
            if (find == null) return;

            // `find` is ConvertedScreen.Node, whose index contains only nodes imported from the
            // authored scene. SettingsStrip is created by Apply at runtime, so asking that index
            // for it emits a missing-node error and aborts PlayMode screenshot tests. Its child
            // ConfigPanel is indexed and reports the same final bounds after reparenting.
            foreach (string name in new[] { "LeftColumn", "RightColumn", "ConfigPanel", "SeatPanel",
                                            "StartButton" })
            {
                var node = find(name) as RectTransform;
                if (node == null) continue;

                var corners = new Vector3[4];
                node.GetWorldCorners(corners);

                Debug.Log($"[LobbyChrome] {name} rect {node.rect.width:F0}x{node.rect.height:F0} " +
                          $"screen x {corners[0].x:F0}..{corners[2].x:F0} " +
                          $"y {corners[0].y:F0}..{corners[2].y:F0}");
            }
        }

        /// <summary>
        /// `PRACTICE` and `MULTIPLAYER` across the top, which is the one piece of the reference's
        /// navigation this game actually has two of.
        ///
        /// ⚠️⚠️ THE REFERENCE'S OTHER TABS ARE NOT INVENTED. PUBG's row is PLAY / CUSTOMIZATION /
        /// REWARDS / CAREER and the mobile shot's is RANK / SEASON / WORKSHOP / MISSIONS /
        /// INVENTORY. This game has none of those, and a nav bar of five tabs where three do
        /// nothing is worse than a nav bar of two that both work: a dead tab is a promise the
        /// build does not keep, and it is the first thing anybody clicks.
        ///
        /// ⚠️ SWITCHING IS IN PLACE, WITH NO SCENE LOAD. A reload here would tear down the map
        /// preview's cached arenas, both render textures and the whole cast, and `SceneFlow.Go`'s
        /// one-load-per-frame latch would not even deduplicate it, because that latch is scoped to
        /// a single frame on purpose.
        /// </summary>
        private static void BuildTabs(Transform root, Func<string, Transform> find,
                                      bool isLobby, Action<bool> onTab, Parts parts)
        {
            var banner = find("Banner");
            Transform parent = banner != null ? banner.parent : root;

            var bar = new GameObject("LobbyTabBar");
            bar.transform.SetParent(parent, false);

            var barRect = bar.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.5f, 1.0f);
            barRect.anchorMax = new Vector2(0.5f, 1.0f);
            barRect.pivot = new Vector2(0.5f, 1.0f);
            barRect.anchoredPosition = new Vector2(0.0f, -TopMargin);
            barRect.sizeDelta = new Vector2((TabWidth * 2.0f) + RailSpacing, HeaderHeight);

            var layout = bar.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = RailSpacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            parts.Practice = Tab(bar.transform, "PracticeTab", "PRACTICE", !isLobby,
                                 () => onTab?.Invoke(false));
            parts.Multiplayer = Tab(bar.transform, "MultiplayerTab", "MULTIPLAYER", isLobby,
                                    () => onTab?.Invoke(true));

            // ⚠⚠ A CHALK BAR UNDER THE LIVE TAB, FOR THE SAME REASON THE SIGN-IN SCREEN'S PAIR
            // HAS ONE: turning amber is a COLOUR, and `game-ui-design` lists a state told only by
            // colour as `colorblind-failure`. Amber is also this front end's accent, so the live
            // tab and START MATCH were saying different things in the same paint. The bar is a
            // shape and it survives a photograph, a bad monitor and a player who cannot separate
            // amber from wood.
            //
            // ⚠️ IT IS A SIBLING OF THE BAR, not a child of either tab, so switching tabs moves
            // one object. Two markers is two things to keep in step and one is always the one
            // somebody forgets.
            parts.TabMarker = UiMaterials.Underline(parent, TabWidth - 40.0f, 0.0f, UiTheme.Amber);

            var markerRect = parts.TabMarker.rectTransform;
            markerRect.anchorMin = new Vector2(0.5f, 1.0f);
            markerRect.anchorMax = new Vector2(0.5f, 1.0f);
            markerRect.pivot = new Vector2(0.5f, 1.0f);
            parts.TabMarkerPitch = (TabWidth + RailSpacing) * 0.5f;
            parts.SetTabMarker(isLobby, TopMargin + HeaderHeight + 6.0f);
        }

        private static Button Tab(Transform parent, string name, string text, bool active,
                                  Action onClick)
        {
            // ⚠️ THE ACTIVE TAB USES THE PRIMARY VARIATION RATHER THAN A TINT. `GodotButton`
            // carries five authored states per variation, and colouring the Image directly fights
            // whichever state the skin writes next, which is how a hovered button ends up the
            // wrong colour a frame later.
            var button = MenuKit.WoodButton(parent, text, Vector2.zero, Vector2.zero,
                                            new Vector2(TabWidth, HeaderHeight), onClick,
                                            active ? "WoodAmberButton" : "WoodButton");
            button.name = name;

            var element = button.gameObject.AddComponent<LayoutElement>();
            element.minHeight = HeaderHeight;
            element.preferredHeight = HeaderHeight;

            var label = button.GetComponentInChildren<Text>();

            // ⚠️ FITTED, BECAUSE "MULTIPLAYER" IS ELEVEN CHARACTERS IN A 260 px BOX AND THE
            // AUTHORED WOOD BUTTON FONT IS SIZED FOR "BACK". See `MenuKit.Fit`, and the four
            // recorded times a label has run out of its box in this project.
            if (label != null) MenuKit.Fit(label, TabWidth - 44.0f);

            return button;
        }
    }
}
