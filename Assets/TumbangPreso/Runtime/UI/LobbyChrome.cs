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
        ///
        /// ⚠️⚠️ AND IT GREW BY `SettingsWidth` + `PaperKit.Gap` ON 2026-09-02, WHEN 🧑 ASKED FOR
        /// A SETTINGS DOOR ON THIS SCREEN: **"cann u also add a settings button in lobby?"**. The
        /// rail is sized to its content, so a new control has to appear in this sum or the rail
        /// will not be wide enough for what is on it and the tab bar's centring (see `BuildTabs`)
        /// will lean the wrong way. **Both of those are one edit or neither**; adding the chip and
        /// forgetting the arithmetic is `docs/TODO.md` § 114.13's fault in slow motion.
        /// </summary>
        private const float TopRailWidth =
            (PaperKit.Pad * 2.0f) + 16.0f + BackWidth + PaperKit.Gap
            + (TabWidth * 3.0f) + (PaperKit.Gap * 2.0f)
            + PaperKit.Gap + SettingsWidth + PaperKit.Gap + ProfileWidth;

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
        /// How tall the tarpaulin is, and it is nearly three times the rail it replaces.
        ///
        /// ⚠️⚠️ THE RAIL WAS 68 UNITS OF CREAM HOLDING SIX PILLS OF ONE SIZE IN ONE ROW, AND IT
        /// IS THE OBJECT `docs/TODO.md` § 133.13 IS ABOUT. *"The failed pass drew the red line
        /// and kept the grid."* A bar that is exactly as tall as the controls on it cannot hold a
        /// hierarchy: everything on it is the same size by construction, so the screen's name,
        /// the fact the screen exists to produce, and three navigation tabs were all 40-unit
        /// chips in a row.
        ///
        /// ⚠️ 196 IS THE ROOM CODE PLUS ITS TWO LINES PLUS THE SAG, MEASURED RATHER THAN CHOSEN:
        /// a `Caption` eyebrow at 16, the code at `Display` 44 drawn at 1.6x for the one fact the
        /// screen produces (70), a hint at 16, three 10-unit gaps, and the 11-per-cent dip the
        /// bottom edge takes in the middle (22). Nothing was rounded to a grid.
        /// </summary>
        private const float TarpHeight = 196.0f;

        /// <summary>
        /// How far the tarpaulin runs off each edge of the screen.
        ///
        /// ⚠️⚠️ IT ESCAPES ITS OWN BOUNDARY ON PURPOSE, WHICH IS THE LOGO'S ONE STRUCTURAL IDEA.
        /// § 133.13: *"the character is in things OVERLAPPING, things sitting at angles to each
        /// other, one element escaping its own boundary"*, and the drip running off the
        /// wordmark's corner is where it comes from. It is also just true of a tarp: one strung
        /// over a street is tied to something outside the picture.
        ///
        /// ⚠️ AND IT IS WHY THE OLD RAIL'S OWN NOTE ABOUT BEING AN ISLAND NO LONGER APPLIES. That
        /// note answered **"be aware of tightness and empty space as well this looks ugly bcz of
        /// big ass empty sopace"**: a full-bleed CREAM bar carried 660 units of bare paper in two
        /// gaps no control could fill. The answer then was to shrink the bar to its content. The
        /// answer now is that the bar is not a container of controls any more, it is a piece of
        /// the room, and a tarp with clear vinyl either side of its printing is what a tarp
        /// looks like. **The empty space is the object rather than a gap in it.**
        /// </summary>
        private const float TarpOverhang = 90.0f;

        /// <summary>
        /// How far the three mode tags hang below the tarpaulin, one length each.
        ///
        /// ⚠️⚠️ THIS IS WHERE THE QUIRK COMES FROM AND IT ADDS NOTHING TO THE SCREEN. § 133.7:
        /// *"the personality is in the SHAPE and the LINE, not in the count"*, and § 133.13:
        /// *"quirk comes from how the existing elements are arranged and shaped, not from new
        /// elements."* These are the same three tabs that were there before. They hang on cords
        /// of three different lengths instead of sitting in a row of equal pills, and that is the
        /// whole change.
        ///
        /// ⚠️ THEY ARE NOT ROTATED, AND `Front_End_Design.md` § 1.2 IS EXPLICIT ABOUT WHY: the
        /// lean is for chrome and **never for type**. Rotated type is unreadable type and
        /// `AspectRatioProbes` measures what a label needs rather than what it looks like. The
        /// irregularity is spent on the cord lengths and on `PaperCraft.Hand`, which already
        /// gives each of the three its own silhouette and its own edge.
        /// </summary>
        private static readonly float[] TagDrop = { 30.0f, 14.0f, 38.0f };

        /// <summary>
        /// How far the whole hung group is raised into the banner.
        ///
        /// ⚠️ 46, WHICH IS ABOUT HALF A TAG. `PaperKit.ChipHeight` is 40 and the tarp's painted
        /// edge runs between 19 and 60 units above this rail's bottom depending on the sag, so at
        /// 46 every tag crosses that edge at every point along it: none of the three is fully on
        /// the vinyl (which would make it a printed word rather than a hung object) and none is
        /// fully on the street (which is what made them disappear).
        /// </summary>
        private const float TagLift = 46.0f;

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
        ///
        /// ⚠️⚠️ 202 SINCE 2026-09-02, AND THE 18 IS THE SETTINGS CHIP GROWING RATHER THAN A
        /// RETUNE OF THE RAIL. `SettingsChipHeight` went 44 to 62 because both of its lines were
        /// drawing outside boxes shorter than their own type (🧑: *"too tight vertically, the text
        /// feel like the text is abt to overflow"*), and this number is that chip plus the gap
        /// plus the primary plus the padding. **It is derived, so it is written as the sum rather
        /// than as a literal**: the previous three values here were all typed by hand and the note
        /// above had to explain each of them after the fact.
        /// </summary>
        private const float BottomRailHeight =
            SettingsChipHeight + PaperKit.Gap + ActionHeight + (PaperKit.Pad * 2.0f)
            + PaperCraft.Drop;

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
        /// <summary>
        /// How wide the one action is.
        ///
        /// ⚠️⚠️ 460 SINCE 2026-09-02 AND IT WAS 520. 🧑, of the remade button: **"maybe tighten
        /// start match button theres big empty space"**. Measured off
        /// `Logs/crops/start-match-v61c.png`: `START MATCH` draws about **330 units** of lettering
        /// in a 520-unit slab, so 190 units, more than a third of the control, was bare brown. The
        /// longest label this slot ever holds is `FIND A RANKED MATCH` at about 400, and
        /// `ConvertedMatchSetup.SetFittedButtonLabel` fits it, so 460 is that plus one 30-unit
        /// margin either side.
        ///
        /// ⚠️ IT IS THE SAME NOTE HE HAS MADE ABOUT FOUR OTHER CONTROLS ON THIS SCREEN
        /// (*"big ass empty sopace"*, § 119.10) and the answer is always the same one: **size the
        /// control against its content and state the arithmetic**, `CLAUDE.md` § 6.2c question 1.
        /// The rail is 60 units narrower with it, which is the tightening he asked for twice.
        /// </summary>
        private const float ActionWidth = 560.0f;

        /// <summary>START MATCH / FIND A RANKED MATCH / START PRACTICE. The one control on this
        /// screen that ends the screen.</summary>
        private const float ActionHeight = 132.0f;

        /// <summary>
        /// How far the burst reaches past the primary, as a multiple of it.
        ///
        /// ⚠️⚠️ 🧑 ASKED FOR THIS DIRECTLY: **"i want start match button to have genuine
        /// emphases and look adn feel good to press"**. The sprite alone cannot answer that.
        /// `FUTURE.md` § 0.5b's four ordering tools are position, size, weight and colour IN THAT
        /// ORDER, and this control already had three of them: the corner every console flow uses,
        /// the largest size on the screen, and the only chartreuse in the palette. **What it did
        /// not have was anything saying the press MATTERS.**
        ///
        /// ⚠️ THE ANSWER IS THE MARK'S OWN BURST, NOT A GLOW. `tsinelas_hit.png` is a slipper
        /// with an impact drawn behind it, so his own art already answers "what does a hit look
        /// like in this hand": irregular spokes, flat colour, no blur. A soft radial glow would
        /// be the one thing in this front end drawn by nobody's hand, and it is also the single
        /// most common way a button is made to look important by somebody who has run out of
        /// ideas.
        ///
        /// ⚠️ 1.5, WHICH IS 280 UNITS OF REACH ON EACH SIDE OF A 560-UNIT CONTROL. It has to
        /// clear the button far enough to read as behind it rather than as a halo ON it, and stop
        /// short of the chips to its left: the action column is 560 wide with a 10-unit gap to
        /// the chip row, so anything past 1.6 would touch a control that is not this one.
        /// </summary>
        private const float BurstReach = 1.9f;

        /// <summary>
        /// The two-line MATCH SETTINGS chip: its name over the settings it summarises.
        ///
        /// ⚠️⚠️ 62 AND IT WAS 44, WHICH WAS SHORTER THAN THE TYPE IT HELD. 🧑 2026-09-02, with a
        /// crop of it: **"match setings look weird, especially with it being too tight vertically,
        /// the text feel like the text is abt to overflow"**. He is describing an arithmetic
        /// error and the numbers are these:
        ///
        /// The chip draws its cast shadow inside its own bottom `PaperCraft.Drop` 6, so 44 units
        /// of layout is **38 units of face**. The title owns the top 52 per cent less a 6-unit
        /// inset, which is about **17 units for a 20-unit line**; the summary owns the bottom 48
        /// per cent less 8, which is about **13 units for a 16-unit line**. **Both boxes were
        /// shorter than the glyphs in them**, and a legacy `Text` with `verticalOverflow =
        /// Overflow` draws outside its box rather than clipping, which is precisely the "about to
        /// overflow" read: the letters were already outside, and only the absence of anything
        /// immediately above or below them hid it.
        ///
        /// ⚠️ 62 IS THE CONTENT ADDED UP RATHER THAN A ROUNDER NUMBER: a 20-unit line at 1.25
        /// leading is 25, a 16-unit line is 20, the shadow is 6 and the two insets are 11. That is
        /// 62, and it is the first height at which neither line is drawing outside its own box.
        /// </summary>
        private const float SettingsChipHeight = 62.0f;

        /// <summary>The FIGHTER row: a name over a loadout, so two lines.</summary>
        private const float FighterRowHeight = 54.0f;

        /// <summary>The SKILLS row: one line, and its caption BESIDE the value rather than at the
        /// far end of the row. ⚠️ It is a DIFFERENT SHAPE from the row above it on purpose
        /// (`docs/TODO.md` § 118.1 row 4) and its two strings are ADJACENT rather than pinned to
        /// opposite edges, which is 🧑's *"big ass empty sopace"*.</summary>
        /// <summary>
        /// The BUILD row. ⚠️ THE SAME HEIGHT AS THE FIGHTER ROW SINCE 2026-09-02, because it is
        /// the same object now: a value over what it is. It was 40 against 54, which is two rows
        /// in one column at two heights on two centre lines, and 🧑 read that as
        /// **"it isnt centered like both of them"**.
        /// </summary>
        private const float SkillsRowHeight = FighterRowHeight;

        /// <summary>The room plaque, and the height its 40-unit code needs with a caption over it.
        /// </summary>
        /// <summary>
        /// ⚠️⚠️⚠️ 82, AND 62 WAS SMALLER THAN THE CODE IT HAD TO HOLD. 🧑 2026-09-02, of the plate
        /// after `docs/TODO.md` § 122.16 had already re-centred the value: **"this ugly maybe
        /// center the code up"**, *"room code and tap to copy are okay"*.
        ///
        /// **Centring could not fix it and the arithmetic says why.** The plate draws its cast
        /// shadow inside its own bottom `PaperCraft.Drop` 6, so a 62-unit plate is a 56-unit face;
        /// the caption row owns the top 40 per cent of it, which left the code a band of about
        /// **31 units for a `PaperKit.Display` glyph that is 44**. `MenuKit.Label` OVERFLOWS
        /// rather than shrinking, so the code spilled about six units past its box in both
        /// directions and the bottom one landed on the plate's inner edge. **A value centred in a
        /// box too small for it is still touching both ends of the box.**
        ///
        /// ⚠️ THE ROOM COLUMN HAD THE ROOM AND THE RAIL DOES NOT GROW. `BottomRailHeight` is
        /// driven by the ACTION column (`SettingsChipHeight` 62 + `Gap` 10 + `ActionHeight` 96 =
        /// 168 of inner height); the room column was `RoomSignHeight` 62 + `Gap` + `ChipHeight` 40
        /// = 112, so **56 units were spare**. This spends 20 of them and leaves 36.
        ///
        /// ⚠️ AND `BuildTierPlate` FOLLOWS BY CONSTRUCTION, because its own height is written as
        /// `RoomSignHeight + ChipHeight + Gap + 40`. The ranked rail cannot drift out of step with
        /// the custom one, which is the whole reason that line is arithmetic.
        /// </summary>
        private const float RoomSignHeight = 82.0f;

        /// <summary>
        /// ⚠️ THE SLOT IS THE PLAQUE PLUS ROOM TO BREATHE INSIDE A 196-UNIT BAND. The tarp's
        /// usable interior at its centre is the band less the sag, which is about 170; the plaque
        /// is 96 here rather than `RoomSignHeight` 82 because it is the one fact the screen
        /// exists to produce and it is now the only thing in the middle of the banner.
        /// </summary>
        private const float RoomSlotHeight = 96.0f;

        /// <summary>
        /// ⚠️ WIDER THAN THE COLUMN IT CAME FROM, BECAUSE THE SPACE IS. In the mode column it was
        /// `RoomColumnWidth` 380 with chips under it; on the tarp it has the whole middle of the
        /// screen and only the screen's name and the identity chip either side of it. 420 leaves
        /// more than 300 units of clear band on each side at 4:3, which is the narrowest shape
        /// `AspectRatioProbes` drives.
        /// </summary>
        private const float RoomSlotWidth = 420.0f;

        /// <summary>
        /// ⚠️ 46 FROM THE TOP OF THE BAND, WHICH CLEARS THE SCREEN NAME'S BASELINE. `LOBBY` sits
        /// at -26 with a 50-unit box, so its ink ends at 76; the plaque starts at 46 and is 420
        /// wide centred, which at every aspect ratio this game runs at leaves the two objects
        /// hundreds of units apart horizontally. The number is about the VERTICAL band the sag
        /// gives back, not about the name.
        /// </summary>
        private const float RoomSlotTop = 46.0f;

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

        /// <summary>
        /// ⚠️⚠️ 64 AGAIN, BECAUSE THE ROWS ARE STEPPERS AGAIN. It was 64 until `18f6d81` turned
        /// them into dropdowns and 56 while they were, and both numbers are the content added up
        /// rather than a size that looked right: a stepper is a 42-unit arrow plus its well and
        /// its border, a closed dropdown is a bare face. `SettingsBodyHeight` reads this, so the
        /// drawer resizes itself and nothing else has to be told.
        /// </summary>
        private const float SettingsRowHeight = 64.0f;
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

        /// <summary>
        /// The identity chip: wide enough for a face, a name and a state line.
        ///
        /// ⚠️ SIZED AGAINST ITS CONTENT AND THE ARITHMETIC IS STATED, which is `CLAUDE.md`
        /// § 6.2c question 1 and the fault § 100 records (a column sized as a PERCENTAGE of the
        /// window, which is two very different widths at two aspect ratios). It is the pad 14,
        /// the face 72, a 12-unit gap, `Player#8226` set at `Title` 26 in Darumadrop (about 190
        /// units), a chevron at 34, and the pad again: **334**.
        /// </summary>
        private const float IdentityWidth = 334.0f;

        /// <summary>
        /// ⚠️ THE FACE PLUS ITS TWO INSETS PLUS THE DROP. 72 + 14 + 14 + 6 = 106, and the two
        /// lines of type inside it come to 26 + 16 + a 6-unit lead = 48, which fits inside the
        /// face's own height so the chip is sized by the picture rather than by the words.
        /// </summary>
        private const float IdentityHeight = 106.0f;

        /// <summary>
        /// ⚠️ 72, WHICH IS HALF THE 144-UNIT THUMB FLOOR AND THAT IS DELIBERATE. The face is not
        /// the touch target; the CHIP is, and at 334 by 106 it clears the floor on its short axis
        /// by construction once `ScreenFocus.MakeRoomForThumbs` has padded it. Sizing the picture
        /// to the floor instead would have given a 144-unit face on a 68-unit rail.
        /// </summary>
        private const float IdentityFace = 72.0f;

        /// <summary>
        /// The door to the game's own settings, between the tabs and the account door.
        ///
        /// ⚠️⚠️ 🧑 ASKED FOR IT BY NAME: **"cann u also add a settings button in lobby?"**,
        /// 2026-09-02. Until then the ONLY way to reach the audio, video and key bindings from a
        /// lobby was to leave it: BACK to the main menu, SETTINGS, change the thing, PLAY, and set
        /// the room up again. **A player who wants to turn the music down mid-lobby had to
        /// dissolve the room to do it**, which is `CLAUDE.md` § 6.3's journey test failing at four
        /// presses and a destroyed match.
        ///
        /// ⚠️⚠️ AND IT IS **NOT** THE SAME THING AS THE `MATCH SETTINGS` CHIP ON THE BOTTOM RAIL,
        /// WHICH IS WHY THE QUALIFIER IS ON THAT ONE AND NOT ON THIS ONE. `BuildSettingsDrawer`
        /// opens the map, the mode and the format: facts about THIS match, on the rail that is
        /// about this match, and only the host may change them. This opens the audio sliders and
        /// the bindings list: facts about this MACHINE, on the rail that is about you, and every
        /// player may change them. **The specific one carries the adjective and the general one is
        /// the bare word**, which is the way round a player can guess; naming this one `GAME
        /// SETTINGS` would have put an adjective on both and made the pair a puzzle.
        ///
        /// ⚠️ 148 IS `SETTINGS` AT `PaperKit.Body` PLUS ONE `Pad` EITHER SIDE, sized like every
        /// other chip on this rail rather than rounded to the nearest fifty. It is narrower than
        /// `ProfileWidth` on purpose: that door has to hold `SECURE PROGRESS`.
        /// </summary>
        private const float SettingsWidth = 148.0f;

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

            BuildGroundMarks(canvasRoot);
            BuildTopRail(canvasRoot, find, left, onMode, parts);
            BuildBottomRail(canvasRoot, find, left, right, parts);
            HangTheModeSlot(parts);

            // ⚠️⚠️ LAST, AND NOT INSIDE `BuildTabs`, BECAUSE THE RIGHT COLUMN DOES NOT EXIST YET
            // WHEN THE TABS ARE BUILT. `SetMode` is what swaps the whole right-hand side, and
            // `ConvertedMatchSetup.SelectMode` only runs when the player CHANGES tab: a screen
            // entered as practice would otherwise ship with a room code and a chat describing a
            // room that does not exist.
            parts.SetMode(mode);

            return parts;
        }

        /// <summary>
        /// Chalk in the two bottom corners, and it means nothing at all.
        ///
        /// ⚠️⚠️ 🧑 ASKED FOR THIS TWICE AND IT IS THE ONE INSTRUCTION THE FAILED PASS DID NOT
        /// EVEN ATTEMPT: **"u can add random shit and designs to the ui too btw to give our
        /// screens character, not everything has to be functional"**, and again while this pass
        /// was running, *"put ranodm designs and drawings too"*. `docs/TODO.md` § 133.14 lists it
        /// as NOT BUILT.
        ///
        /// ⚠️⚠️ IT PULLS AGAINST § 92 AND THE RESOLUTION IS A PLACE RATHER THAN A QUANTITY.
        /// *"Theres liek 20 shits at once"* was six BUTTONS in six visual languages: every one of
        /// them was a thing the player had to look at, decide about and dismiss. **A drawing that
        /// means nothing costs none of that.** `Front_End_Design.md` § 1.3: *decoration is free
        /// where nothing has to be read, and expensive where something does.*
        ///
        /// ⚠️ SO IT GOES IN THE DEAD GROUND, AND § 118.1 ROW 2 MEASURED HOW MUCH THERE IS:
        /// **680 units of nothing down the lobby's left side and 475 down its right.** That is
        /// not space that needs protecting, it is space that is already doing nothing. These two
        /// marks sit in the bottom corners, outside every content rect on the screen.
        ///
        /// ⚠️ AND THEY ARE THE FIRST SIBLINGS, so every rail and every control draws over them.
        /// A decoration that can cover a control is not a decoration; `raycastTarget` is off for
        /// the same reason one level down, because anything covering the screen is also eating
        /// clicks and `CLAUDE.md` § 6.2c question 5 records that being nobody's stated job three
        /// separate times.
        ///
        /// ⚠️ 0.16 ALPHA IS UNDER § 1.3'S 1.5:1 CEILING against the asphalt, so it cannot compete
        /// with anything at `Caption` or larger, all of which measure 5:1 or better. **A drawing
        /// that fails that ratio is not decoration, it is a seventh sign.**
        /// </summary>
        private static void BuildGroundMarks(Transform canvasRoot)
        {
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject(i == 0 ? "ChalkLeft" : "ChalkRight",
                                        typeof(RectTransform), typeof(Image));
                go.transform.SetParent(canvasRoot, false);

                // ⚠️⚠️ LAST, NOT FIRST, AND `Logs/shots-runtime/Lobby-v86.png` IS WHY. As the
                // first sibling it drew under the scrim and the two tint bands `SoftenScrim` and
                // `Band` install, which is three multiplications of a mark that was already at a
                // third of its own alpha. It was not faint, it was absent.
                //
                // ⚠️ AND DRAWING LAST IS SAFE HERE FOR TWO REASONS THAT BOTH HAVE TO HOLD. It
                // sits in a screen CORNER, outside every content rect on the lobby, so there is
                // nothing for it to cover; and `raycastTarget` is off, so it can never take a
                // press from anything it happens to overlap. `Front_End_Design.md` § 1.3's rule
                // is about CONTRAST rather than about z-order, and the alpha below is what keeps
                // it under the ratio.
                go.transform.SetAsLastSibling();

                var image = go.GetComponent<Image>();
                image.sprite = BrandMarks.Chalk(i);
                image.type = Image.Type.Simple;
                image.raycastTarget = false;
                // ⚠️ 0.30, AND IT WAS 0.16 IN `Logs/shots-runtime/Lobby-v85.png`, WHERE IT IS
                // NOT VISIBLE AT ALL. The number was reasoned from § 1.3's 1.5:1 ceiling and the
                // reasoning was right; what it missed is that the sprite's own strokes are
                // ALREADY feathered to an alpha under 1, so the Image tint multiplies a soft mark
                // rather than a solid one. **A ratio computed against the colour rather than
                // against the drawn pixel is a ratio nobody measured**, which is the same class
                // of miss as the chalk rule at 0.55 alpha in `docs/TODO.md` § 117.7: *"a chalk
                // rule at 0.55 alpha is a quarter-strength mark, because the tint multiplies the
                // sprite's own"*. Cream at 0.30 on asphalt still measures well under 1.5:1.
                image.color = new Color(1.0f, 1.0f, 1.0f, 0.34f);

                var rect = (RectTransform)go.transform;
                rect.anchorMin = new Vector2(i == 0 ? 0.0f : 1.0f, 0.0f);
                rect.anchorMax = rect.anchorMin;
                rect.pivot = new Vector2(i == 0 ? 0.0f : 1.0f, 0.0f);
                rect.anchoredPosition = new Vector2(i == 0 ? 24.0f : -24.0f, 24.0f);
                rect.sizeDelta = new Vector2(300.0f, 150.0f);
            }
        }

        /// <summary>
        /// Moves the room code onto the tarpaulin, which is the slot each mode fills.
        ///
        /// ⚠️⚠️ THE TARP SHIPPED WITH 196 UNITS OF EMPTY BAND AND ONE WORD IN THE CORNER, WHICH
        /// IS THE FAULT IT WAS BUILT TO FIX ARRIVING FROM THE OTHER SIDE. 🧑 on the old rail:
        /// **"be aware of tightness and empty space as well this looks ugly bcz of big ass empty
        /// sopace"**, and, watching this pass, *"be aware of empty space and shit, as well as
        /// negative space"*. A taller band with nothing in the middle of it is worse than the
        /// short one, not better.
        ///
        /// ⚠️⚠️ AND IT IS THE ANSWER TO *"lets say u click ranked wtf would show?"*
        /// `Front_End_Design.md` § 2.2b: **the composition does not change between modes, one
        /// slot does.** The tarp's middle carries the one fact the current mode exists to
        /// produce, at the same size in the same place, so a player who has learned where to look
        /// has learned it once. `Parts.SetMode` still decides WHICH fact.
        ///
        /// ⚠️ THE AUTHORED NODE IS REPARENTED, NEVER REBUILT. `RoomCodeButton` keeps its name,
        /// its `Button`, its copy handler and its `PaperSkin`, so
        /// `PaperPurityProbe.NothingOnTheInventoryDisappeared` still resolves it and the 338
        /// captured controls are all still there. **That probe is what makes tearing a screen
        /// apart safe** (`docs/TODO.md` § 133.14) and it only works if things are moved rather
        /// than replaced.
        ///
        /// ⚠️ IT RUNS AFTER BOTH RAILS, because the node does not exist until `BuildBottomRail`
        /// has made the mode column. Calling it earlier finds nothing and fails silently, which
        /// is `LobbyChrome.Apply`'s own recorded trap one call up.
        /// </summary>
        private static void HangTheModeSlot(Parts parts)
        {
            var code = parts.CodeButton;
            var rail = parts.TopRail;
            if (code == null || rail == null) return;

            var rect = (RectTransform)code.transform;
            rect.SetParent(rail, false);

            var element = rect.GetComponent<LayoutElement>();
            if (element == null) element = rect.gameObject.AddComponent<LayoutElement>();
            element.ignoreLayout = true;

            // ⚠️ CENTRED ON THE SCREEN, NOT ON THE SPRITE. The tarp runs one `TarpOverhang` off
            // each edge, so its own middle IS the screen's middle; anchoring to 0.5 of the rail
            // is therefore correct here and would not be if the overhang were ever asymmetric.
            // ⚠️⚠️ THE PIVOT IS THE TOP EDGE, AND IT WAS THE BOTTOM ONE IN
            // `Logs/shots-runtime/Lobby-v85.png`, WHERE THE ROOM CODE IS CUT IN HALF BY THE TOP
            // OF THE SCREEN. With `pivot.y = 0` the rect grows UPWARD from the anchored point, so
            // "46 units down from the top of the banner" placed the plaque's BOTTOM there and put
            // its 96 units of height above the window. **A pivot is not an alignment**, and this
            // is the one-character version of the fault `CLAUDE.md` § 6.2c question 1 keeps
            // recording: a number that is correct against a rectangle nobody is measuring.
            rect.anchorMin = new Vector2(0.5f, 1.0f);
            rect.anchorMax = new Vector2(0.5f, 1.0f);
            rect.pivot = new Vector2(0.5f, 1.0f);
            rect.anchoredPosition = new Vector2(0.0f, -RoomSlotTop);
            rect.sizeDelta = new Vector2(RoomSlotWidth, RoomSlotHeight);

            var skin = code.GetComponent<PaperSkin>();
            if (skin != null) skin.Rebuild();
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
            // ⚠️⚠️ BUILT BY HAND RATHER THAN THROUGH `PaperKit.Sheet`, AND THE FIRST VERSION OF
            // THIS METHOD USED THE KIT AND SHIPPED A BLANK CREAM BAND. `PaperKit.Sheet` attaches
            // a `PaperSkin`, and `PaperSkin.Update` calls `Rebuild` EVERY FRAME, which writes
            // `_image.sprite` from `PaperCraft.Slab`. Setting the sprite after the kit built the
            // node therefore lasted exactly one frame.
            //
            // ⚠️⚠️ AND `Object.Destroy(skin)` DOES NOT FIX IT, WHICH IS THE PART WORTH RECORDING.
            // `Destroy` is deferred to the end of the frame, so the component's `Update` runs at
            // least once more and repaints a `Sheet` over the tarp.
            // `Logs/shots-runtime/Lobby-v84.png` is the receipt: a flat `UiTheme.Paper` band with
            // no sag, no stroke and no ties, which is the OLD rail with a new height. **A
            // component that paints from `Update` has to be disabled, not destroyed**, and the
            // honest version is not to create it at all.
            var railGo = new GameObject("LobbyTopRail", typeof(RectTransform), typeof(Image));
            railGo.transform.SetParent(canvasRoot, false);

            var rail = railGo.GetComponent<Image>();
            rail.raycastTarget = true;
            rail.sprite = BrandMarks.Tarpaulin();
            rail.type = Image.Type.Simple;
            rail.color = Color.white;

            // ⚠️⚠️ AN ISLAND, NOT A FULL-BLEED BAR, AND 🧑 ASKED FOR THIS BY NAME: **"be aware
            // of tightness and empty space as well this looks ugly bcz of big ass empty sopace"**.
            // Stretched edge to edge the rail was about 1800 units around 1140 of content, so it
            // carried 660 units of bare cream in two gaps that no control could ever fill. A bar
            // sized to what is IN it reads as a designed object; a bar sized to the window reads as
            // a browser toolbar, and it costs the street two corners it does not need to lose.
            // ⚠️ STRETCHED ON X AND PINNED TO THE TOP, WITH THE OVERHANG IN `sizeDelta`. With
            // `anchorMin.x = 0` and `anchorMax.x = 1`, `sizeDelta.x` is the amount ADDED to the
            // parent's width, so the tarp is the canvas plus one overhang each side at every
            // aspect ratio without a single hard-coded width. `AspectRatioProbes` drives nine.
            var rect = rail.rectTransform;
            rect.anchorMin = new Vector2(0.0f, 1.0f);
            rect.anchorMax = new Vector2(1.0f, 1.0f);
            rect.pivot = new Vector2(0.5f, 1.0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(TarpOverhang * 2.0f, TarpHeight);

            parts.TopRail = rail.transform;

            BuildTies(rail.transform);
            BuildScreenName(rail.transform);

            LiftBack(rail.transform, leftColumn);
            BuildTabs(rail.transform, onMode, parts);
            BuildProfileButton(rail.transform, parts);
            BuildSettingsButton(rail.transform, parts);
            LiftVersionStamp(canvasRoot, rail.transform);
        }

        /// <summary>
        /// The two eyelets the tarpaulin hangs from.
        ///
        /// ⚠️ A SAG WITHOUT A TIE IS A CURVE, NOT A HUNG THING. The bottom edge dipping in the
        /// middle only reads as weight if something is visibly holding the two ends up; without
        /// the eyelets the band reads as a shape somebody drew with a wobbly bottom, which is the
        /// same object the old rail was with a different silhouette. **Two marks, twenty units
        /// each, and they are the difference between a motif and a decoration.**
        ///
        /// ⚠️ THEY ARE SEPARATE SPRITES BECAUSE THE TARP IS STRETCHED RATHER THAN NINE-SLICED
        /// (see <see cref="BrandMarks.Tarpaulin"/>): an eyelet drawn into that texture would be
        /// scaled to twice its width on his window and stop being a circle.
        /// </summary>
        private static void BuildTies(Transform rail)
        {
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject(i == 0 ? "TarpTieLeft" : "TarpTieRight",
                                        typeof(RectTransform), typeof(Image));
                go.transform.SetParent(rail, false);

                var image = go.GetComponent<Image>();
                image.sprite = BrandMarks.Tie();
                image.type = Image.Type.Simple;
                image.raycastTarget = false;

                var r = (RectTransform)go.transform;
                r.anchorMin = new Vector2(i == 0 ? 0.10f : 0.90f, 1.0f);
                r.anchorMax = r.anchorMin;
                r.pivot = new Vector2(0.5f, 1.0f);
                r.anchoredPosition = new Vector2(0.0f, 6.0f);
                r.sizeDelta = new Vector2(30.0f, 48.0f);
            }
        }

        /// <summary>
        /// WHERE AM I, top left, in the display face.
        ///
        /// ⚠️⚠️ THE LOBBY HAD NO NAME AT ALL AND THAT IS THE FIRST ROW OF
        /// `Front_End_Design.md` § 1'S SPINE: *"it is the answer to 'where am I', and a player
        /// who has to hunt for that has already lost the screen."* Every other screen in this
        /// front end has a heading; this one had three tabs, and a tab says which mode you picked
        /// rather than which screen you are on.
        ///
        /// ⚠️ THE SUB LINE IS THE SEAT COUNT, WHICH IS THE ONE FACT THAT BELONGS UNDER A ROOM'S
        /// NAME AND WAS PREVIOUSLY ONLY DERIVABLE BY COUNTING NAMEPLATES. It is set by
        /// `Parts.SetSeats`; the literal here is what a screen with no session shows.
        /// </summary>
        private static void BuildScreenName(Transform rail)
        {
            var name = PaperKit.Ink(rail, "LOBBY", PaperKit.Display, TextAnchor.UpperLeft);
            name.name = "ScreenName";
            name.raycastTarget = false;

            var r = name.rectTransform;
            r.anchorMin = new Vector2(0.0f, 1.0f);
            r.anchorMax = new Vector2(0.0f, 1.0f);
            r.pivot = new Vector2(0.0f, 1.0f);
            // ⚠️ THE OVERHANG IS IN THE INSET. The tarp starts one `TarpOverhang` LEFT of the
            // screen, so a control placed at x = 0 on this rail is off screen entirely. Every
            // inset on this rail is measured from the screen edge, not from the sprite's.
            r.anchoredPosition = new Vector2(TarpOverhang + EdgeMargin, -26.0f);
            r.sizeDelta = new Vector2(420.0f, 50.0f);

            var seats = PaperKit.Ink(rail, "4 in the room", PaperKit.Caption,
                                     TextAnchor.UpperLeft, soft: true);
            seats.name = "SeatCount";
            seats.raycastTarget = false;

            var sr = seats.rectTransform;
            sr.anchorMin = new Vector2(0.0f, 1.0f);
            sr.anchorMax = new Vector2(0.0f, 1.0f);
            sr.pivot = new Vector2(0.0f, 1.0f);
            sr.anchoredPosition = new Vector2(TarpOverhang + EdgeMargin + 2.0f, -78.0f);
            sr.sizeDelta = new Vector2(420.0f, 24.0f);
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

            // ⚠️⚠️ IT HANGS BELOW THE TARPAULIN NOW RATHER THAN SITTING INSIDE A BAR, AND THAT
            // ANSWERS § 118.1 ROW 5 BY POSITION RATHER THAN BY SIZE. That row is *"BACK competes
            // with the tab row"*, and it competed because the two were the same object at the
            // same height on the same rail: a 40-unit pill among four other 40-unit pills, where
            // the only thing saying "this one is the way out" was the word on it.
            // `Front_End_Design.md` § 1 pins BACK top left, immediately under the screen's name,
            // and that is what this is: the name is on the tarp and the way out is directly below
            // it, which is the arrangement every console flow has used for fifteen years.
            back.anchorMin = new Vector2(0.0f, 1.0f);
            back.anchorMax = new Vector2(0.0f, 1.0f);
            back.pivot = new Vector2(0.0f, 1.0f);
            // ⚠️⚠️ THE INSET IS THE PADDING PLUS THE RAIL OWN CORNER, NOT THE PADDING. 🧑, with a
            // crop of the top rail: **"back is brokenn"**. `PaperCraft` cuts every sheet with an
            // 18-unit radius, so a chip placed `PaperKit.Pad` 14 in from the edge has its left end
            // and its halo sitting ON the curve, which reads as a control falling off the bar.
            // **The first control in a rounded container clears the RADIUS, not the padding.**
            // ⚠️ THE OVERHANG IS IN THE INSET, because the tarp starts one `TarpOverhang` LEFT
            // of the screen: a control at x = 0 on this rail is off screen. The old note about
            // clearing the container's own corner radius no longer applies, since the control is
            // outside the container rather than inside it.
            // ⚠️⚠️ ON THE TARPAULIN, DIRECTLY UNDER THE SCREEN'S NAME, WHICH IS
            // `Front_End_Design.md` § 1'S SECOND SPINE ROW WORD FOR WORD: *"BACK — top left,
            // immediately under the name"*. The first version hung it below the banner with the
            // mode tags, which was better than the old rail (§ 118.1 row 5, BACK competing with
            // the tab row) and still not the spine: it put the way OUT in the same band as the
            // three ways SIDEWAYS.
            //
            // ⚠️ AND IT IS ALSO WHAT THE LEFT OF THE BANNER IS FOR. `Logs/shots-runtime/
            // Lobby-v87.png` carries about 600 units of bare vinyl between the screen's name and
            // the room code, which is 🧑's *"big ass empty sopace"* arriving on the object built
            // to fix it. **A tarp has clear vinyl on it and a banner with a hole in it does
            // not**, and the difference is whether the printing is grouped.
            //
            // ⚠️ THE OVERHANG IS IN THE INSET, because the tarp starts one `TarpOverhang` LEFT of
            // the screen: a control at x = 0 on this rail is off screen entirely.
            back.anchoredPosition = new Vector2(TarpOverhang + EdgeMargin, -104.0f);
            back.sizeDelta = new Vector2(BackWidth, PaperKit.ChipHeight);

            var element = back.GetComponent<LayoutElement>();
            if (element != null) element.ignoreLayout = true;

            PaperKit.Paperise(back.gameObject, PaperCraft.Surface.Token);

            var label = back.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                // ⚠️⚠️ ONE SPACE, NOT TWO, AND THE CHEVRON IS WHY IT LOOKED LEFT-HEAVY. 🧑, with
                // a crop of the top rail: **"back still isnt centered as well"**. `Text` centres
                // on the string's own ink, so `"‹  BACK"` centres the chevron AND the word
                // together and the word itself therefore sits right of the pill's middle by half
                // the chevron plus half the gap. Two spaces at 20 units is about 11 units of that
                // error on a 120-unit chip, which is where "not centred" comes from on a control
                // whose box genuinely is centred.
                label.text = "‹ BACK";
                label.name = "Label";
                label.fontSize = PaperKit.Body;
                label.color = UiTheme.PaperInk;
                MenuKit.Apply(label, PaperKit.FaceFor(label.fontSize), bold: true);
                label.alignment = TextAnchor.MiddleCenter;

                // ⚠️⚠️ AND IT WAS SIX UNITS LOW ON TOP OF THAT, IN THE OTHER DIRECTION FROM EVERY
                // OTHER CHIP IN THE GAME. This line read `offsetMax = (0, -Drop)`, which pulls the
                // box's TOP edge DOWN; `PaperKit.Chip` raises the BOTTOM edge instead. Both are
                // six units and they move the lettering opposite ways, so BACK sat twelve units
                // below its neighbours. `PaperKit.CentreOnFace` is now the only place either
                // correction is written.
                MenuKit.Stretch(label.rectTransform, 0.0f);
                PaperKit.CentreOnFace(label);
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
            barRect.anchorMin = new Vector2(0.0f, 0.0f);
            barRect.anchorMax = new Vector2(0.0f, 0.0f);
            barRect.pivot = new Vector2(0.0f, 1.0f);

            // ⚠️ THE BAR SITS WHERE THE ARITHMETIC PUTS IT, NOT WHERE A NUDGE DOES. The rail is
            // sized to its content now (`TopRailWidth`), so the tabs occupy the exact middle of a
            // row whose left end is BACK and whose right end is the identity pair; the half of the
            // difference between those two blocks is what the bar has to lean by, and it is
            // computed rather than eyeballed.
            // ⚠️⚠️ THE RIGHT BLOCK IS TWO CONTROLS NOW AND IT WAS ONE. `BuildSettingsButton`
            // added a chip between the tabs and the account door on 2026-09-02
            // (🧑: **"cann u also add a settings button in lobby?"**), and this arithmetic is what
            // keeps the tab bar in the rail's optical middle rather than its geometric one. Miss
            // this line and the three tabs sit 79 units left of where they look like they should.
            // ⚠️⚠️ NO LAYOUT GROUP, AND THAT IS THE WHOLE CHANGE. A `HorizontalLayoutGroup`
            // with `childForceExpand` on both axes is a machine for producing identical objects
            // in a row, which is exactly the fault 🧑 named: *"the issue with old UI is
            // everything feels repetitive bcz i think u use the same code to generate them all"*.
            // The three tabs are placed by hand at three different DROPS, so they hang the way
            // things hang off a tarp rather than sitting in a rail.
            //
            // ⚠️ THE PLACEMENT IS STILL ARITHMETIC RATHER THAN NUDGES. Each tag's x is the sum of
            // the ones before it plus one gap, and its y is `TagDrop[i]`; nothing here is a magic
            // offset tuned against one window, which is `CLAUDE.md` § 6.3's rule about `UiRows`
            // applied one screen over.
            // ⚠️⚠️ THE TAGS STRADDLE THE TARPAULIN'S BOTTOM EDGE RATHER THAN HANGING CLEAR OF
            // IT, AND 🧑 LOST THEM ENTIRELY WHEN THEY DID NOT: **"also where did the gamemodes
            // go? practice, custom, ranked that shit? i kinda liked those"**, looking at
            // `Logs/shots-runtime/Lobby-v89.png` where all three are present, wired and drawn.
            //
            // ⚠️⚠️ THAT IS `docs/TODO.md` § 96 IN MINIATURE AND IT IS THE MOST IMPORTANT NOTE ON
            // THIS METHOD. The hub had exactly one door and the person who commissioned it could
            // not find it, while `PlayerHubLayoutProbe` was green at all nine resolutions. Here
            // `UiClickProbe` can prove all three tags are reachable and `PaperPurityProbe` can
            // prove none of them was lost, and **both of those are true of a control nobody
            // sees.** A pale Honey Quartz chip standing on a sunlit street is a control with no
            // ground: `PaperKit.Ink`'s own note says the front end is legible because the SHEET
            // makes it so, and these three were the only paper controls in the game with no sheet
            // under them.
            //
            // ⚠️ SO THEY KEEP THE HUNG ARRANGEMENT AND GET THEIR GROUND BACK. Raised 46 units
            // each tag is half on the vinyl and half on the street, which is what a tag hung off
            // a tarp actually looks like, and the half on honey is what makes the lettering
            // read. **The three different cord lengths, which is where the quirk lives, are
            // untouched.**
            barRect.anchoredPosition = new Vector2(
                TarpOverhang + EdgeMargin + BackWidth + 44.0f, TagLift);
            barRect.sizeDelta = new Vector2((TabWidth * 3.0f) + (PaperKit.Gap * 2.0f),
                                            PaperKit.ChipHeight + 44.0f);

            string[] names = { "PracticeTab", "RankedTab", "CustomTab" };
            string[] words = { "PRACTICE", "RANKED", "CUSTOM" };
            LobbyMode[] order = { LobbyMode.Practice, LobbyMode.Ranked, LobbyMode.Custom };

            for (int i = 0; i < 3; i++)
            {
                float x = i * (TabWidth + PaperKit.Gap);
                float drop = TagDrop[i];

                // the cord. Two units wide, deep red, from the tarp's edge to the tag's top: it
                // is what makes three chips at three heights read as three things HUNG rather
                // than as three things somebody failed to align.
                var cord = new GameObject($"{names[i]}Cord", typeof(RectTransform), typeof(Image));
                cord.transform.SetParent(bar.transform, false);
                var cordImage = cord.GetComponent<Image>();
                cordImage.color = UiTheme.BrandRed;
                cordImage.raycastTarget = false;
                var cr = (RectTransform)cord.transform;
                cr.anchorMin = new Vector2(0.0f, 1.0f);
                cr.anchorMax = new Vector2(0.0f, 1.0f);
                cr.pivot = new Vector2(0.5f, 1.0f);
                cr.anchoredPosition = new Vector2(x + (TabWidth * 0.5f), 4.0f);
                cr.sizeDelta = new Vector2(4.0f, drop + 6.0f);

                var chip = PaperKit.Chip(bar.transform, names[i], words[i]);
                var rect = (RectTransform)chip.transform;
                rect.anchorMin = new Vector2(0.0f, 1.0f);
                rect.anchorMax = new Vector2(0.0f, 1.0f);
                rect.pivot = new Vector2(0.0f, 1.0f);
                rect.anchoredPosition = new Vector2(x, -drop);
                rect.sizeDelta = new Vector2(TabWidth, PaperKit.ChipHeight);

                var element = chip.GetComponent<LayoutElement>();
                if (element != null) element.ignoreLayout = true;

                parts.Tabs[(int)order[i]] = chip;

                var chosen = order[i];
                chip.onClick.AddListener(() => onMode?.Invoke(chosen));
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

            // ⚠️⚠️ IT CARRIES A FACE NOW, AND THAT IS `docs/TODO.md` § 96'S FIX RATHER THAN A
            // DECORATION. He commissioned the player hub and then could not find the way into it,
            // because its one door was a corner chip stating a name and a level: **a status
            // readout, which is a thing people read and not a thing people press.** A face is a
            // thing people press, and top-right with a face is where Overwatch, Valorant and
            // Fortnite all put the way into a profile, so it costs no teaching at all
            // (`Front_End_Design.md` § 1, and § 133.8's *"controls are familliar to them
            // already"*).
            //
            // ⚠️ AND IT IS STILL EXACTLY ONE DOOR. § 6.3 forbids adding a second door to fix a
            // findability problem, which is how § 92's six-button panel happened. This is the
            // same door, moved and given a picture.
            //
            // ⚠️ IT HANGS OFF THE TARPAULIN'S BOTTOM EDGE, overlapping it, rather than sitting
            // inside it. The logo's own structural idea is things overlapping and escaping their
            // boundaries (§ 133.13), and it does a second job here: an object clipped to the
            // banner reads as attached to the room, where a word printed on the banner would read
            // as part of the banner's message.
            var rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(1.0f, 0.0f);
            rect.anchorMax = new Vector2(1.0f, 0.0f);
            rect.pivot = new Vector2(1.0f, 0.5f);
            rect.anchoredPosition = new Vector2(-(TarpOverhang + EdgeMargin), 6.0f);
            rect.sizeDelta = new Vector2(IdentityWidth, IdentityHeight);

            var face = Avatars.Frame(button.transform, "ProfileFace", null);
            var fr = face.rectTransform;
            fr.anchorMin = new Vector2(0.0f, 0.5f);
            fr.anchorMax = new Vector2(0.0f, 0.5f);
            fr.pivot = new Vector2(0.0f, 0.5f);
            fr.anchoredPosition = new Vector2(PaperKit.Pad, PaperCraft.Drop * 0.5f);
            fr.sizeDelta = new Vector2(IdentityFace, IdentityFace);
            parts.ProfileFace = face;

            var label = button.transform.Find("Label")?.GetComponent<Text>();
            if (label != null)
            {
                // ⚠️ LEFT-ALIGNED BESIDE THE FACE, NOT CENTRED ON THE CHIP. A name centred on a
                // chip that also holds a picture centres on the chip and therefore sits off
                // centre against the picture, which is the *"back still isnt centered"* fault one
                // control over: a box that is centred and lettering that does not look it.
                label.name = "ProfileValue";
                label.alignment = TextAnchor.LowerLeft;
                label.fontSize = PaperKit.Title;
                MenuKit.Apply(label, PaperKit.FaceFor(label.fontSize));
                MenuKit.Stretch(label.rectTransform, 0.0f);
                label.rectTransform.offsetMin =
                    new Vector2(PaperKit.Pad + IdentityFace + 12.0f, IdentityHeight * 0.44f);
                label.rectTransform.offsetMax = new Vector2(-34.0f, -PaperKit.Pad);
                parts.ProfileValue = label;
            }

            // ⚠️ THE SECOND LINE IS THE ACCOUNT STATE AND IT REPLACES A WHOLE TAB. `SECURE
            // PROGRESS` used to be a fifth pill on the top rail, sitting beside three MODE tabs
            // as though it were a mode. It is not a place, it is a fact about you, so it belongs
            // on the thing that says who you are. **That is one fewer object on the screen and
            // one fewer thing to scan**, which is `CLAUDE.md` § 6.2's third claim.
            var state = PaperKit.Ink(button.transform, "GUEST · SAVE PROGRESS", PaperKit.Caption,
                                     TextAnchor.UpperLeft, soft: true);
            state.name = "ProfileState";
            state.raycastTarget = false;
            MenuKit.Read(state, bold: true);
            MenuKit.Stretch(state.rectTransform, 0.0f);
            state.rectTransform.offsetMin =
                new Vector2(PaperKit.Pad + IdentityFace + 12.0f, PaperCraft.Drop + 8.0f);
            state.rectTransform.offsetMax =
                new Vector2(-34.0f, -(IdentityHeight * 0.56f));
            parts.ProfileState = state;

            var chevron = PaperKit.Chevron(button.transform);
            chevron.raycastTarget = false;

            parts.ProfileButton = button;
        }

        /// <summary>
        /// SETTINGS, between the mode tabs and the account door.
        ///
        /// ⚠️⚠️ 🧑 ASKED FOR IT: **"cann u also add a settings button in lobby?"**, 2026-09-02.
        /// <see cref="SettingsWidth"/> carries the journey it fixes and the argument for the name;
        /// this is where it is placed and why there.
        ///
        /// ⚠️⚠️ IT SITS ON THE RIGHT END WITH `ACCOUNT` BECAUSE BOTH ARE ABOUT **YOU AND THIS
        /// MACHINE**, and the top rail's own header says what the rail is for: *who you are, where
        /// you are, and how you get out*. BACK is the way out, the three tabs are where you are,
        /// and the two chips on the right are who you are. **Putting it on the bottom rail would
        /// have been the wrong rail entirely**: that one is three questions about the MATCH, and a
        /// control that changes the master volume is not one of them.
        ///
        /// ⚠️ AND IT IS INSIDE THE ACCOUNT DOOR, NOT OUTSIDE IT, which is `game-ui-design`'s
        /// ordering read backwards: the rightmost thing on a rail is the last thing scanned and
        /// `ACCOUNT` is the more important of the two (it is the only way to set a name on a
        /// machine with no network, `docs/TODO.md` § 97). A settings door is a utility and belongs
        /// one step in from the end.
        ///
        /// ⚠️⚠️ IT IS A PLAIN `PaperKit.Chip` AND CARRIES NO ACCENT, and that is § 118.4 rather
        /// than a shortage of ideas. There is one accent per screen and it is START MATCH; a
        /// second coloured control on the top rail would be a second "press me" competing with the
        /// button this whole screen exists to reach. A door is furniture that opens.
        /// </summary>
        private static void BuildSettingsButton(Transform rail, Parts parts)
        {
            var button = PaperKit.Chip(rail, "GameSettingsButton", "SETTINGS");

            var rect = (RectTransform)button.transform;

            // ⚠️ IT HANGS TO THE LEFT OF THE IDENTITY CHIP, WHICH IS THE SPINE'S RULE FOR A
            // SECONDARY: chips in a row to the LEFT of the more important thing, never above or
            // below it (`Front_End_Design.md` § 1). The argument in this method's own note is
            // unchanged and still holds: both controls are about YOU AND THIS MACHINE, and the
            // account door is the more important of the two, so the utility sits one step in from
            // the end.
            //
            // ⚠️ MEASURED FROM THE RIGHT EDGE THROUGH THE DOOR BESIDE IT, not from a number of
            // its own, so the pair cannot overlap however either width changes.
            rect.anchorMin = new Vector2(1.0f, 0.0f);
            rect.anchorMax = new Vector2(1.0f, 0.0f);
            rect.pivot = new Vector2(1.0f, 0.5f);
            rect.anchoredPosition = new Vector2(
                -(TarpOverhang + EdgeMargin + IdentityWidth + PaperKit.Gap), 6.0f);
            rect.sizeDelta = new Vector2(SettingsWidth, PaperKit.ChipHeight + 12.0f);

            parts.GameSettingsButton = button;
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
            // ⚠️⚠️ THE SCREEN'S CORNER, NOT THE RAIL'S, AND THE RAIL MOVING IS WHY. This method
            // hung the stamp under the top rail's bottom-right, which was the quietest corner of
            // the screen while that rail was a 68-unit bar in the middle of the top edge. The
            // rail is a full-bleed tarpaulin now and its bottom-right corner is **exactly where
            // the identity chip hangs**, so the line that avoided a collision with BACK made one
            // with the account door instead, and the stamp vanished behind it.
            //
            // ⚠️ THE INTENT IN THE ORIGINAL NOTE IS WHAT IS KEPT: *"where a build number belongs:
            // the quietest corner of the screen"*. That corner is measured against the SCREEN now
            // rather than against a control that has moved twice. § 118.1 row 8 (*"the version
            // stamp sits on nothing"*) stays answered by the outline this method already gives it.
            rect.SetParent(canvasRoot, false);
            rect.anchorMin = new Vector2(1.0f, 0.0f);
            rect.anchorMax = new Vector2(1.0f, 0.0f);
            rect.pivot = new Vector2(1.0f, 0.0f);
            rect.anchoredPosition = new Vector2(-EdgeMargin, EdgeMargin * 0.5f);
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
            // ⚠️⚠️ BUILT BY HAND, BECAUSE `PaperSkin.Rebuild` WRITES `color` AS WELL AS
            // `sprite` AND DISABLING IT WAS NOT ENOUGH. `Logs/shots-runtime/Lobby-v86.png` still
            // shows the cream slab after this method set the Image's alpha to zero and switched
            // the skin off in the same breath: `OnRectTransformDimensionsChange` is not an
            // Update-family callback and still reaches the component, and its line 38 puts
            // `Color.white` back. **This is the same lesson the top rail learned one method up**
            // (there it was `Object.Destroy` being deferred), and the conclusion is the same
            // both times: a node that must not be repainted should never be given the painter.
            var railGo = new GameObject("LobbyBottomRail", typeof(RectTransform), typeof(Image));
            railGo.transform.SetParent(canvasRoot, false);

            var rail = railGo.GetComponent<Image>();

            // ⚠️⚠️ TRANSPARENT AND STILL A RAYCAST TARGET, WHICH IS DELIBERATE AND IS NOT A
            // CONTRADICTION. uGUI hit-tests a Graphic against its alpha THRESHOLD, which defaults
            // to zero, so a fully transparent Image still catches presses. The rail has always
            // eaten clicks that would otherwise fall through to the street behind it, and
            // `CLAUDE.md` § 6.2c question 5 is explicit that when a full-screen graphic goes you
            // name its replacement blocker in the same commit. **Here the blocker does not
            // change; only the paint does.**
            rail.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
            rail.raycastTarget = true;

            // ⚠️⚠️ THE RAIL IS INVISIBLE NOW, AND IT IS STILL A RAIL. `Logs/shots-runtime/
            // Lobby-v85.png`: with the top of the screen rebuilt as a hung tarp, the one object
            // left reading as a generated container was this one, a flat cream slab across the
            // bottom third holding three cells in a row. **That is the exact object
            // `docs/TODO.md` § 133.13 rejects** — *"every object is the same pill in the same
            // grid with a red line around it"* — and it is the last of it on this screen.
            //
            // ⚠️ THE CONTROLS DID NOT NEED IT AND THAT IS WHY THIS IS FREE. Every child of this
            // rail carries its own opaque paper surface (the fighter card, the build tray, the
            // primary, the two chips, the room plaque), so nothing on it was relying on the slab
            // for legibility over the street. The slab was buying separation from a background
            // that the controls already separate themselves from, which is `CLAUDE.md` § 6.2c
            // question 3 asked and answered: *"ask what a dimming layer protects before retuning
            // it"*.
            //
            // ⚠️⚠️ AND THE LAYOUT, THE FITTER AND THE CENTRING ALL STAY, which is the whole point
            // of doing it this way. The rail still measures itself, still re-centres when
            // practice drops a column, and still owns every offset in this file. **Only the paint
            // is gone.** Deleting the object instead would have moved three columns' worth of
            // arithmetic into their callers for a visual change.
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

            // ⚠️⚠️ THE THREE GROUPS SHARE A BASELINE NOW, AND THEY DID NOT WHILE THERE WAS A
            // SLAB UNDER THEM TO HIDE IT. `childForceExpandHeight` made every column as tall as
            // the rail and `MiddleCenter` then centred each one inside that height, so the
            // fighter card, the primary and the two chips each floated at a different offset.
            // On a cream slab that reads as padding; on the street, with the slab gone, it reads
            // as three objects somebody dropped. `Logs/shots-runtime/Lobby-v87.png` is the
            // receipt: JOIN and CHAT sit level with the TOP of a primary they are secondary to.
            //
            // ⚠️ AND A SHARED BASELINE IS THE RULE RATHER THAN A TIDY-UP. `FUTURE.md` § 0.5b's
            // four ordering tools put POSITION first, and things standing on one line read as
            // one row of objects on a road, which is what this screen is now: the secondary
            // chips are visibly smaller than the primary and visibly on the same ground.
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.LowerCenter;

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
            MenuKit.Apply(name, PaperKit.FaceFor(name.fontSize), bold: true);
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

            // ⚠️⚠️ THE PAIR IS ONE OBJECT THAT CENTRES ITSELF, AND THE ARITHMETIC IT REPLACES WAS
            // OFF-CENTRE BY CONSTRUCTION. 🧑 2026-09-02, with a crop of this row and the DANTE row
            // above it: *"these look ugly"*, then the diagnosis in his own words, **"it looks ugly
            // bcz it isnt centered like both of them and theres big empty space"**.
            //
            // **He is right and it is measurable.** The previous version gave the caption a box
            // ending 10 units LEFT of the row's middle and the value a box STARTING at the middle,
            // left-aligned. So the visible pair ran from `centre - captionWidth - 10` to
            // `centre + valueWidth`: `SKILLS` is about 62 units at `Caption` and `Standard Build`
            // about 130 at `Body`, which puts the pair's own centre **about 34 units right of the
            // row's**. `BuildCharacterRow` above centres `DANTE` properly, so the two rows in one
            // column had two different centre lines and the eye reads the mismatch long before it
            // can name it. **The old note claimed the pair "floats in the middle as one object";
            // it was two objects that met in the middle, which is not the same thing.**
            //
            // ⚠️ A LAYOUT GROUP KEEPS THE GUARANTEE THE OLD ARITHMETIC WAS BOUGHT FOR. That code
            // had the two boxes share an edge on purpose, because `Lobby-v55.png` showed `SKILLS`
            // drawn straight through `Standard Build` when they overlapped by 46 units, and **two
            // labels overlapping is silent in every direction** (§ 102.4 rotated). A horizontal
            // group sized to its own content cannot overlap either, and it also cannot be wrong
            // when the value string changes length, which the hand-written version always could.
            // ⚠️⚠️ THE TWO DOORS IN THIS COLUMN ARE ONE SHAPE NOW: A VALUE OVER WHAT IT IS.
            // 🧑 2026-09-02, with a crop of both rows: *"these look ugly"*, **"it looks ugly bcz
            // it isnt centered like both of them and theres big empty space"**, and then the ask
            // that decided the copy: **"make this look better its confusing what they do, u have
            // permission to overhaul the text on them to make it easier to uunderstand"**.
            //
            // **Two faults, and the second one is the interesting one.** The rows were different
            // shapes (a two-line block over a one-line pair, at two heights, on two centre lines),
            // AND neither said what pressing it did: `DANTE` is a name and `SKILLS Standard Build`
            // is a setting, and a player who has never opened either cannot tell that both are
            // doors to other screens. **A row that states a value states a fact; a row that states
            // a value UNDER ITS NOUN states a control.**
            //
            // So both are now the same object: the VALUE on top at `PaperKit.Title`, and what it
            // is underneath at `PaperKit.Caption` in soft ink, with the chevron that says it
            // opens. `DANTE / Fighter · Pasip · Tsinelas` and `Standard Build / Skill loadout`.
            // The ranking is by SIZE and VALUE rather than by two type sizes on one line, which is
            // the arrangement he rejected as *"these diff fonts look ugly"*.
            var label = PaperKit.Ink(go.transform, "", PaperKit.Title, TextAnchor.LowerCenter);
            label.name = "LoadoutValue";
            label.raycastTarget = false;
            MenuKit.Apply(label, PaperKit.FaceFor(label.fontSize), bold: true);
            label.rectTransform.anchorMin = new Vector2(0.0f, 0.44f);
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(34.0f, 0.0f);
            label.rectTransform.offsetMax = new Vector2(-34.0f, -4.0f);

            var caption = PaperKit.Ink(go.transform, "Skill loadout", PaperKit.Caption,
                                       TextAnchor.UpperCenter, soft: true);
            caption.name = "LoadoutCaption";
            caption.raycastTarget = false;
            caption.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            caption.rectTransform.anchorMax = new Vector2(1.0f, 0.44f);
            caption.rectTransform.offsetMin = new Vector2(34.0f, PaperCraft.Drop);
            caption.rectTransform.offsetMax = new Vector2(-34.0f, 0.0f);

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
            MenuKit.Apply(title, PaperKit.FaceFor(title.fontSize), bold: true);
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
            MenuKit.Apply(title, PaperKit.FaceFor(title.fontSize), bold: true);
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

                // ⚠️⚠️ THE PRIMARY IS PAPER'S OWN `Action` NOW, IN THE BROWN HE ASKED TO KEEP.
                // 🧑 2026-09-02, in order: *"orange outline when i hover over start match is
                // ugly"*, **"u really have to redesign start match button, it doesnt FEEL like a
                // start match button"**, then the correction that says what the fault actually is,
                // **"i like the size adn color but it feells so flat, it doesn thave start match
                // energy"**, and finally **"i want start amtchg to still be brown okay"**.
                //
                // **Size and colour were never the problem and he said so.** This node kept
                // `GodotButton` and had `ArrowButtonView` switched off above, so while § 120.1 was
                // giving every paper chip in the front end an eased hover, a 2.5 per cent lift and
                // a collapsing shadow, **the one control the screen exists for had a sprite swap
                // and nothing else.** `PaperCraft.Surface.Action` is the depth and
                // `PaperKit.MakeAction` is the motion; `docs/TODO.md` § 121.1 carries the
                // measurement of the grey halo that came off with the wooden construction.
                //
                // ⚠️ NO `FocusRing` ON IT ANY MORE. It lit on POINTER HOVER as well as on focus
                // and drew a rounded-rect amber outline around a chamfered slab: a silhouette that
                // did not follow the control, in a colour that measures **1.46:1** on `Paper`. The
                // hover is said by the pose now, which is what a hover is for; the ring stays on
                // the controls that have no other focus state (see `FocusRing`).
                // ⚠️⚠️ CHARTREUSE, NOT THE BROWN, AND THE INSTRUCTION THAT PUT BROWN HERE HAS
                // BEEN SUPERSEDED BY A LATER ONE. 🧑 chose brown for this control by name once
                // (*"u can also still use the brown color ... start match lowk looks good"*),
                // and on 2026-09-03 said **"i dont wanna use the old colors anymore"** and
                // *"i want colors corresponding to or using the same colors as my logo"*.
                //
                // ⚠️ AND `Accent.Wood` NO LONGER MEANS WHAT IT MEANT WHEN THAT LINE WAS WRITTEN.
                // Under `PaperCraft.Surface.Brand` it is Honey Quartz, which is the QUIET fill:
                // on the honey rail this control sits on, the screen's one primary was drawing
                // honey on honey and had no presence at all (`Logs/shots-runtime/Lobby-v79.png`,
                // where START MATCH is the palest thing in the frame). **A primary must be the
                // heaviest object on its screen**, which is `docs/Front_End_Design.md` § 4's role
                // table: Chartreuse is the action, one per screen.
                PaperKit.MakeAction(node.gameObject, PaperCraft.Accent.Green);

                // ⚠️⚠️ THE LETTERING GROWS WITH THE CONTROL, AND WITHOUT THIS LINE IT DOES NOT.
                // `MakeAction` does not touch `fontSize`, so the label stayed at whatever the
                // authored Godot button carried while the plate around it went from 460x96 to
                // 560x132: **a bigger button with the same words on it reads as a button with
                // more empty space, not as a louder one.** `Display` is the one step in the type
                // scale reserved for one thing per screen, and this is that thing.
                var primaryLabel = node.GetComponentInChildren<Text>(true);
                if (primaryLabel != null)
                {
                    primaryLabel.fontSize = PaperKit.Display;
                    MenuKit.Apply(primaryLabel, PaperKit.FaceFor(primaryLabel.fontSize));
                    MenuKit.Fit(primaryLabel, ActionWidth - 48.0f, PaperKit.Title);
                }

                BuildBurst(slot.transform, node);
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

        /// <summary>
        /// The impact behind the one action on the screen.
        ///
        /// ⚠️⚠️ IT IS INSERTED AS THE FIRST SIBLING, WHICH IS THE ENTIRE MECHANISM. uGUI draws
        /// in hierarchy order, so a decoration added after the button draws OVER its lettering
        /// and the control this exists to emphasise becomes the one thing on the screen you
        /// cannot read. `Front_End_Design.md` § 1.3 names that as a place decoration may never
        /// go: *"behind or on a control's own lettering"*, which is § 6.4's amber-on-cream
        /// problem with a picture instead of a colour.
        ///
        /// ⚠️ IT IS NOT A RAYCAST TARGET. Anything covering a control is also eating clicks, and
        /// `CLAUDE.md` § 6.2c question 5 records that block being nobody's stated job three
        /// times. This one blocks nothing and says so.
        ///
        /// ⚠️ AND IT SITS UNDER § 1.3'S RATIO BY CONSTRUCTION. `BrandMarks.Burst` peaks at 0.34
        /// alpha in Golden and falls to nothing at its rim; on the lit street that is well under
        /// 1.5:1 against its own ground, so it cannot compete with anything at `Caption` or
        /// larger. **A drawing that fails that ratio is not decoration, it is a seventh sign.**
        /// </summary>
        private static void BuildBurst(Transform slot, RectTransform button)
        {
            var go = new GameObject("PrimaryBurst", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(slot, false);
            go.transform.SetAsFirstSibling();

            var image = go.GetComponent<Image>();
            image.sprite = BrandMarks.Burst();
            image.type = Image.Type.Simple;
            image.raycastTarget = false;

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = button != null ? button.anchoredPosition : Vector2.zero;
            rect.sizeDelta = new Vector2(ActionWidth * BurstReach, ActionHeight * BurstReach);
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
            caption.rectTransform.anchorMin = new Vector2(0.0f, 0.72f);
            caption.rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
            caption.rectTransform.offsetMin = new Vector2(PaperKit.Pad, 0.0f);
            caption.rectTransform.offsetMax = new Vector2(-PaperKit.Pad, -7.0f);

            var hint = MenuKit.Label(go.transform, "tap to copy", PaperKit.Caption,
                                     UiTheme.CreamMuted, Vector2.zero, Vector2.zero,
                                     Vector2.zero, TextAnchor.UpperRight);
            hint.name = "RoomCodeHint";
            hint.raycastTarget = false;
            hint.rectTransform.anchorMin = new Vector2(0.0f, 0.72f);
            hint.rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
            hint.rectTransform.offsetMin = new Vector2(PaperKit.Pad, 0.0f);
            hint.rectTransform.offsetMax = new Vector2(-PaperKit.Pad, -7.0f);

            // ⚠️⚠️ CENTRED, BECAUSE THE CODE IS THE ONE FACT ON THIS PLATE AND IT WAS SITTING IN
            // THE CORNER OF IT. 🧑, with a crop of exactly this plaque: **"pic 1 can be improve"**.
            // A four-character code drawn `LowerLeft` on a 380-unit plaque leaves about 240 units
            // of empty wood to its right, with `tap to copy` marooned in the far corner above it:
            // three strings, three corners, and the middle of the object empty. The caption and
            // the hint keep the top band's two ends, which is what makes them read as labels ON
            // the sign rather than as competitors with it.
            // ⚠️⚠️⚠️ `MiddleCenter`, NOT `LowerCenter`, AND THAT ONE WORD IS WHAT
            // 🧑 PHOTOGRAPHED. 2026-09-02, with a crop of this plaque: **"make this botton look
            // prettier MN26 is so close to the bottom of box even tho theres a lot of space at
            // top"**. He is describing the anchor exactly: the code was drawn against the BOTTOM
            // of its own band, so it sat on the plate's inner edge with every spare unit of the
            // band stacked above it as a gap. **A value pinned to one edge of its box puts all of
            // its slack on the other edge**, and the slack here is the difference between a
            // 44-unit `Display` glyph and a 29-unit band.
            //
            // ⚠️ AND THE BAND MOVED WITH IT. 0.60 rather than 0.58 lifts the code's box two
            // per cent, which is what puts its optical centre in the middle of the FACE rather
            // than in the middle of the RECT: the plate draws its cast shadow inside its own
            // bottom `PaperCraft.Drop` units, so the two are three units apart. That is the same
            // correction `PaperKit.CentreOnFace` makes for every chip in the game.
            var label = MenuKit.Label(go.transform, "", PaperKit.Display, UiTheme.Cream,
                                      Vector2.zero, Vector2.zero, Vector2.zero,
                                      TextAnchor.MiddleCenter);
            // ⚠️⚠️ IT IS CALLED `Label`, AND THE NAME IS WIRING RATHER THAN A DESCRIPTION.
            // `PaperButton` looks for a child called `Label` and falls back to the FIRST `Text`
            // under the control; on this plate that fallback is `RoomCodeCaption`, so the
            // component tinted the word ROOM CODE instead of the code, and it sank the caption on
            // a press instead of the value. Naming the code makes the plaque's one important
            // string the one the component acts on. Nothing looks any of these three up by name,
            // so this is safe: `parts` holds the references.
            label.name = "Label";
            MenuKit.Apply(label, PaperKit.FaceFor(label.fontSize), bold: true);
            label.raycastTarget = false;
            label.alignment = TextAnchor.MiddleCenter;
            label.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            // ⚠️ 0.72, WHICH IS 23 UNITS OF CAPTION ROW ON AN 82-UNIT PLATE AND 53 FOR THE CODE.
            // A 44-unit glyph centred in 53 clears both ends by four units, which is the first
            // time this plate has had a box its own value fits inside. See `RoomSignHeight`.
            label.rectTransform.anchorMax = new Vector2(1.0f, 0.72f);
            label.rectTransform.offsetMin = new Vector2(PaperKit.Pad, PaperCraft.Drop);
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

            // ⚠️⚠️ THE PLATE IS 40 UNITS TALLER THAN THE COLUMN IT MIRRORS, AND THE ARITHMETIC IS
            // THE SENTENCE. `LobbyRanked-v58.png` shows YOUR TIER, UNRANKED, and **nothing where
            // the note should be** — which is § 119.9 row 4 back in a different form. That row was
            // an overlap; this is a height. The plate was `RoomSignHeight` 62 + `ChipHeight` 40 +
            // `Gap` 10 = 112, matching the room column's plaque-plus-chips; the note owns the
            // bottom 34 per cent of it, which is 38 units, less its 14-unit inset, which is
            // **24 units of room for a caption that wraps to two lines of 16** (about 40). With
            // `verticalOverflow = Truncate` the whole thing disappears rather than spilling.
            //
            // ⚠️ AND THE FIX IS HEIGHT RATHER THAN A SHORTER STRING, because the string is the
            // party rule and § 119.8 exists to state it BEFORE the press: *"make it as well na u
            // cant queue with a friend in ranked ladder or smth"*. A rule the player only meets
            // after being refused is `CLAUDE.md` § 6.2's INTUITIVE failure, which is what this
            // plate was built to answer.
            var element = go.gameObject.AddComponent<LayoutElement>();
            element.minHeight = RoomSignHeight + PaperKit.ChipHeight + PaperKit.Gap + 40.0f;
            element.preferredHeight = element.minHeight;
            element.flexibleHeight = 0.0f;

            // ⚠️⚠️ ALL THREE LINES CENTRE, AND THIS PLATE WAS THE ONE THING ON THE RAIL THAT DID
            // NOT. 🧑 2026-09-02: **"also make everything centered (your tier unranked looks ugly
            // bcz it isnt centered"**. The fighter rows to its left centre, the mode plate above
            // the primary centres, the room code centres since § 120.6, and these three were
            // `UpperLeft` / `MiddleLeft` / `LowerLeft`. **One plate aligned differently from every
            // other plate in the same rail reads as a mistake even to somebody who cannot say
            // which plate is wrong**, which is what the four ordering tools mean by consistency.
            //
            // ⚠️ THE HEIGHTS DO NOT MOVE. This method's own note measures the note's box at 64
            // units for a caption that wraps to two lines of 16; centring changes alignment only,
            // and re-deriving the bands here would put § 119.9 row 4 back (a value's rect drawn
            // over a sentence, invisible because a `Text` draws nothing where it has no glyphs).
            var caption = PaperKit.Ink(go.transform, "YOUR TIER", PaperKit.Caption,
                                       TextAnchor.UpperCenter, soft: true);
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
                                       TextAnchor.MiddleCenter);
            tier.name = "TierValue";
            tier.raycastTarget = false;
            // ⚠️ 0.42, WHICH IS 64 UNITS OF NOTE ON A 152-UNIT PLATE: two 20-unit lines plus the
            // 14-unit inset plus four spare. The value keeps 88, which is twice its own 44.
            tier.rectTransform.anchorMin = new Vector2(0.0f, 0.42f);
            tier.rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
            tier.rectTransform.offsetMin = new Vector2(PaperKit.Pad, 0.0f);
            tier.rectTransform.offsetMax = new Vector2(-PaperKit.Pad, -(PaperKit.Pad + 16.0f));

            var note = PaperKit.Ink(go.transform, "", PaperKit.Caption, TextAnchor.LowerCenter,
                                    soft: true);
            note.name = "TierNote";
            note.raycastTarget = false;
            note.horizontalOverflow = HorizontalWrapMode.Wrap;
            note.verticalOverflow = VerticalWrapMode.Truncate;
            note.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            note.rectTransform.anchorMax = new Vector2(1.0f, 0.42f);
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

            // ⚠️⚠️⚠️ THE FOUR ROWS ARE STEPPERS AGAIN, AND THIS REVERSES `18f6d81` AND § 116.8.
            // 🧑 2026-09-02, off the `ui-redesign` player: **"whyd we even change the old design of
            // it wherein it was clickable from left to right"**, and then the flow he wants in his
            // own words, **"to change between maps i click match settings and i js clcik left and
            // rigth to swtich between diff maps and bots and stuff"**. Asked which shape to bring
            // back, he chose the authored rows exactly rather than arrows bolted onto the paper
            // ones.
            //
            // ⚠️⚠️ THE ENTRY THAT REPLACED THEM RECORDS ITS OWN MANDATE AS *"His suggestion, taken
            // literally"*, AND THE SUGGESTION WAS **"u can use dropdowns and shit to make some
            // shit work or look good"**. That is permission to use a dropdown somewhere, and it
            // was read as an instruction to replace a working control everywhere. § 116.8's
            // argument (twelve controls for four choices, and no row says what the other options
            // are) is a real argument and it is not the one he was making. **A rationale nobody
            // asked for is how a screen he liked became a screen he could not use.**
            //
            // ⚠️ SO THE DROPDOWNS ARE NOT BUILT AND `WoodDropdown` IS NOT DELETED. The type still
            // compiles, still carries its own sorting fix, and `parts.SettingsRows` stays null, on
            // which `ConvertedMatchSetup.BuildSettingsDropdowns` already returns early and
            // `RefreshSettingsDropdowns` is null-guarded on all four. Deleting a control he might
            // ask for again is the mistake this comment exists to stop repeating in reverse.
            var rows = Descend(config, "Rows");
            if (rows != null)
            {
                rows.SetParent(body.transform, false);
                rows.gameObject.SetActive(true);

                // ⚠️ FOUR ROWS, NOT THREE. `BuildFormatRow` adds RULES under BOTS, and this height
                // is what the rows container claims: left at three, the fourth row drew outside
                // the sheet and over the cast's legs.
                float rowsHeight = (SettingsRowHeight * 4.0f) + 16.0f;

                var rowsElement = rows.GetComponent<LayoutElement>();
                if (rowsElement == null) rowsElement = rows.gameObject.AddComponent<LayoutElement>();
                rowsElement.minHeight = rowsHeight;
                rowsElement.preferredHeight = rowsHeight;
                rowsElement.flexibleHeight = 0.0f;

                var rowsLayout = rows.GetComponent<HorizontalOrVerticalLayoutGroup>();
                if (rowsLayout != null) rowsLayout.spacing = 8.0f;

                Narrow(rows as RectTransform, SettingsDrawerWidth - (PaperKit.Pad * 2.0f));

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

        /// <summary>
        /// The RULES row: STANDARD, LAST TSINELAS STANDING or MIRROR.
        ///
        /// ⚠️⚠️ IT IS A CLONE OF THE AUTHORED BOTS ROW RATHER THAN A ROW BUILT FROM SCRATCH, AND
        /// THAT IS THE POINT. The three selector rows are authored nodes carrying his own arrow
        /// TEXTURES, an authored well and an authored inner layout. A fourth row written in code
        /// would be a fourth visual language on a rail whose whole redesign was about the first
        /// three not lining up, and `docs/VISION.md` § 6 is the standing rule: **his UI art IS the
        /// design system**. `Instantiate` gets all of it free and cannot drift from the other
        /// three.
        ///
        /// ⚠️⚠️ AND THE BUTTONS COME BACK ON `Parts` RATHER THAN BEING FOUND BY NAME. Every other
        /// control on this screen is wired with `OnClick("SomeButton", ...)`, which reads
        /// `ConvertedScreen`'s name index, and **that index is built in `Start` before this method
        /// runs**. A clone made afterwards is not in it, so a name lookup would answer null and the
        /// row would be a stepper whose arrows do nothing: `docs/TODO.md` § 108's EQUIP button
        /// exactly, in a place nobody would think to look for it.
        /// </summary>
        private static void BuildFormatRow(Transform rows, Parts parts)
        {
            var source = Descend(rows, "DifficultyRow");
            if (source == null) return;

            // ⚠️ NOT TWICE. This drawer is rebuilt whenever the lobby restyles, and a second clone
            // would stack a duplicate RULES row under the first and push START MATCH off the rail.
            var existing = Descend(rows, "FormatRow");
            if (existing != null) UnityEngine.Object.Destroy(existing.gameObject);

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

            // ⚠️ THE CLONED LISTENERS GO. `Instantiate` copies a `Button`'s persistent `onClick`
            // entries with it, so without this both arrows would still be cycling the BOT
            // DIFFICULTY they were cloned from, on a row labelled RULES. Nothing would log.
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

        /// <summary>
        /// One selector row: caption on the left, stepper on the right.
        ///
        /// ⚠️⚠️ IT IS PAPER NOW AND IT WAS WOOD BEFORE `18f6d81`, WHICH IS THE ONE THING THIS
        /// REVERT MAY NOT RESTORE. § 119 repainted the whole front end, so bringing the pre-dropdown
        /// version back verbatim would put a `GodotTheme.WoodBox` well and amber type inside a paper
        /// drawer: `docs/TODO.md` § 117's *"two design systems stacked"* rebuilt on purpose, and the
        /// code-drawn half would be the half that looks wrong. **The interaction comes back; the
        /// material does not.** The well is `PaperCraft.Surface.Slot`, the caption is the same
        /// `PaperInkSoft` at `PaperKit.Caption` the dropdown rows used, and the value is `PaperInk`
        /// at `PaperKit.Body`, so a restored row and a paper row are the same object.
        ///
        /// ⚠️ EVERY STEP IS GUARDED SEPARATELY, so a row whose caption was renamed loses its caption
        /// and keeps its stepper rather than throwing halfway and leaving the drawer in neither
        /// layout.
        /// </summary>
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

            // ⚠️⚠️ THE CAPTION COLUMN IS FIXED AND THAT IS THE HALF NOBODY SEES. MAP, MODE and BOTS
            // are three different widths, and the authored group sized each caption to its own
            // string, so the three steppers started at three different x positions. Nothing in the
            // drawer lined up with anything, which is most of what "ugly" was the first time.
            var captionNode = Descend(rowNode, caption);
            var captionText = captionNode == null ? null : captionNode.GetComponent<Text>();

            if (captionText != null)
            {
                // ⚠️ THE COLON GOES, AND IT IS WORTH 54 UNITS OF THE RAIL. The scene authors these
                // as `MAP:`, `MODE:` and `BOTS:`, and the caption column has to be as wide as the
                // longest; dropping the colon let `SettingsCaptionWidth` come down from 150 to 96.
                // The value sits in its own well with an arrow either side, which says "what
                // follows is the value" louder than a colon does.
                captionText.text = word;
                captionText.fontSize = PaperKit.Caption;
                captionText.color = UiTheme.PaperInkSoft;
                captionText.alignment = TextAnchor.MiddleLeft;
                captionText.horizontalOverflow = HorizontalWrapMode.Overflow;
                captionText.verticalOverflow = VerticalWrapMode.Overflow;
                captionText.raycastTarget = false;

                // ⚠️ THE INK OUTLINE HAS TO GO ON PAPER. Every `GodotTheme` menu style carries a 3
                // to 5 unit `GodotOutline` because those styles were written to be read over a live
                // 3D street; on an opaque cream sheet it is a dark halo round every letter, which is
                // the fault the detail label in this same drawer already records.
                var captionOutline = captionNode.GetComponent<GodotOutline>();
                if (captionOutline != null) captionOutline.enabled = false;

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
                // ⚠️⚠️ THE WELL IS `Tray`, WHICH IS PAPER'S OWN RECESS: *"you read it or type in
                // it"*, no halo, no cast shadow, an inner shadow along the top. The authored plate
                // was repainted `WoodBox` dark before `18f6d81` so the value read as set INTO the
                // panel, and this is the same sentence in the paper language. ⚠️ `PaperSkin.Apply`
                // rather than a sprite assignment, because it destroys the `WoodSkin` that would
                // otherwise keep redrawing the plank underneath on the next rect change.
                PaperSkin.Apply(selectorNode.gameObject, PaperCraft.Surface.Tray);

                var plate = selectorNode.GetComponent<Image>();
                if (plate != null)
                {
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
                // `Inner` is a stretched child of the plate, so 27 off a 62-unit stepper leaves the
                // arrows 35 units tall inside a 62-unit well. 10 keeps a visible border and gives
                // the arrows the height they were drawn at.
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
                valueText.fontSize = PaperKit.Body;
                valueText.color = UiTheme.PaperInk;
                valueText.alignment = TextAnchor.MiddleCenter;
                valueText.horizontalOverflow = HorizontalWrapMode.Overflow;
                valueText.verticalOverflow = VerticalWrapMode.Overflow;
                valueText.raycastTarget = false;

                var valueOutline = valueNode.GetComponent<GodotOutline>();
                if (valueOutline != null) valueOutline.enabled = false;

                var element = valueNode.GetComponent<LayoutElement>();
                if (element == null) element = valueNode.gameObject.AddComponent<LayoutElement>();

                // ⚠️ FLEXIBLE, WITH THE TWO ARROWS FIXED EITHER SIDE. The authored row gave the
                // value a preferred width, so a short word like NONE left the two arrows floating
                // in the middle of the well and a long one like ILALIM NG TULAY pushed them out of
                // it. Pinning the arrows and letting the value take what is left is what makes the
                // four rows read as one control repeated.
                element.minWidth = 0.0f;
                element.preferredWidth = -1.0f;
                element.flexibleWidth = 1.0f;
            }
        }

        /// <summary>One stepper arrow: square, fixed, and never resized by the value beside it.</summary>
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
            /// <summary>
            /// The tarpaulin, so anything that has to hang off it can find it.
            ///
            /// ⚠️ IT IS HELD RATHER THAN LOOKED UP BY NAME, because `find("LobbyTopRail")` walks
            /// the authored scene and this node is built in code: the two namespaces have been
            /// confused here before (`docs/TODO.md` § 124.11 is a probe knocking on a door that
            /// had moved).
            /// </summary>
            public Transform TopRail;

            /// <summary>The avatar on the identity chip. Set by `RefreshProfileDoor`.</summary>
            public Image ProfileFace;

            /// <summary>
            /// The account state under the player's name on the identity chip.
            ///
            /// ⚠️ IT IS WHERE `SECURE PROGRESS` WENT. That was a fifth pill on the top rail
            /// standing beside three MODE tabs as though signing in were a place you could be.
            /// </summary>
            public Text ProfileState;

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

            /// <summary>
            /// The door to the game's own settings panel: audio, video, key bindings.
            ///
            /// ⚠️ NOT `SettingsDrawerToggle`, WHICH IS A DIFFERENT CONTROL WITH A DIFFERENT JOB.
            /// That one opens the MATCH settings drawer on the bottom rail (the map, the mode, the
            /// format) and only the host may use it. See `BuildSettingsButton` for why they are
            /// named the way they are and why they live on opposite rails.
            /// </summary>
            public Button GameSettingsButton;

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
                LoadoutValue.fontSize = PaperKit.Title;
                MenuKit.Fit(LoadoutValue, FighterColumnWidth - 68.0f);
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
                    // ⚠️⚠️ IT SAYS WHAT THE ROW IS, NOT JUST WHAT IS IN IT. 🧑 2026-09-02:
                    // **"make this look better its confusing what they do, u have permission to
                    // overhaul the text on them to make it easier to uunderstand"**. The line read
                    // `Pasip · Tsinelas`, which are the lata and the tsinelas this fighter is
                    // carrying: true, and it never told a player who had not already opened the
                    // picker that the row above it is a FIGHTER or that pressing it changes one.
                    // The noun goes first for the same reason the BUILD row's does.
                    CharacterLoadout.text = string.IsNullOrWhiteSpace(loadout)
                        ? "Fighter"
                        : "Fighter  ·  " + Sentence(loadout);
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

                // ⚠️⚠️ FULL `PaperInk` ON THE IDLE TAB, WHICH IS `SignInScreen.SetTab`'S FIX
                // APPLIED TO THE PAIR THAT SHARES ITS DESIGN. 🧑 2026-09-02 photographed the login
                // screen's tabs (**"kinda hard to read this text"**) and this row is built to the
                // same rule, so it carried the same fault on a lighter ground. As WCAG contrast:
                //
                // | Tab | Type on ground | Ratio |
                // |---|---|---|
                // | Live | `Cream` `f5e6c8` on `WoodMid` `5a2f14` | **9.20:1** |
                // | Idle, was | `PaperInkSoft` `7a5c40` on `Paper` `f4ecdd` | **5.21:1** |
                // | Idle, is | `PaperInk` `3b2415` on `Paper` `f4ecdd` | **12.34:1** |
                //
                // ⚠️ THE COMPLAINT IS THE RATIO BETWEEN THE TWO, NOT EITHER NUMBER. 5.21 passes on
                // its own; it fails sitting beside 9.20 in one rail, because the eye reads the
                // pair rather than the label. `Ghost` carries no fill, so the idle ground here is
                // the panel's own `Paper` rather than the login's `PaperWarm`, which is why the
                // two screens land on different numbers from the same swatch.
                //
                // ⚠️ THE NOTE ABOVE STILL HOLDS AND IS NOT WEAKENED BY THIS. The 10:1 value
                // inversion and the fill-against-outline silhouette are still the two signals
                // saying which tab is live; this changes only how legible the one you are NOT on
                // is, and recede was never meant to mean unreadable.
                MenuKit.Apply(label, PaperKit.FaceFor(label.fontSize), bold: true);
                label.color = active ? UiTheme.Cream : UiTheme.PaperInk;
            }
        }
    }
}
