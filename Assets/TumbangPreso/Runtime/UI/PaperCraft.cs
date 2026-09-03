using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The front end's surfaces after the figure and the ground were swapped: six constructions
    /// in CUT PAPER, taken from the game's own sticker logo.
    ///
    /// 🧑 2026-09-01, with `Art/ui/TUMP.png` and a two-swatch card attached: *"game reads as too
    /// brown bcz the game itself is brown already (the map and shit)"*, *"can we remodel the color
    /// of all UI for lobby and login to look like this?"*, and then, on the whole screen rather
    /// than the palette: *"redesign teh whole ass UI"*, *"ur goal is to make it inntuitive and easy
    /// for user to traverse and calming"*, **"I DONT WANT it to be overwhelming for htem"**.
    ///
    /// -----------------------------------------------------------------------------------------
    /// ⚠️⚠️ WHY THIS FILE EXISTS WHEN `WoodCraft` ALREADY DID THIS JOB
    /// -----------------------------------------------------------------------------------------
    ///
    /// `WoodCraft` is correct and it is not deleted. It transcribes 🧑's authored art
    /// (`BUTTON LONG`, `JOIN BUTTON`, `TEXT FIELD`, the pennants) and it is still what draws every
    /// wooden control in the game, including every one on the main menu and in the match, neither
    /// of which this pass may touch.
    ///
    /// **What it could not fix is that the front end and the world are the same colour.** Sampling
    /// `Logs/shots-runtime/Lobby-v51.png`: Eskinita's road, its houses, its poles and its fences
    /// sit at hue 18 to 40 and 30 to 60 per cent saturation. `UiTheme.WoodFace` `793e1f` is hue 22
    /// at 74 per cent. Every panel on that screen is therefore a slightly darker version of the
    /// picture behind it, found by its keyline rather than seen as a shape, and no amount of
    /// bevel, grain or varnish changes that: **the problem is one step up from the material.**
    ///
    /// ⚠️ SO THIS IS NOT A FIFTH HUE AND `CLAUDE.md` § 6.4 IS INTACT. `f4ecdd` and `efdabe` are
    /// hue 34 to 38 at 6 to 20 per cent saturation, one step off `UiTheme.Cream` `f5e6c8`, which
    /// has been in the palette since `ui_theme.gd`. The wood, the amber, the green and the ink are
    /// all unchanged. What changed is **which of them is the field**.
    ///
    /// -----------------------------------------------------------------------------------------
    /// ⚠️⚠️ EVERY SURFACE HAS A CAST SHADOW, AND THAT IS THE 2026-09-01 CORRECTION
    /// -----------------------------------------------------------------------------------------
    ///
    /// 🧑, on the first paper build: **"thoroughly plan as well how to make the buttons look better
    /// bcz right now they dont maybe bcz u just recolored them all"**. He is right and the
    /// diagnosis is exact. The first version drew every control as a flat pill with a halo and a
    /// two-unit lip, so a screen of them read as **printing on the panel** rather than as objects
    /// lying on it: nothing had a below, so nothing had a height.
    ///
    /// **A paper cut-out casts a shadow. That is the whole of what was missing.** Every raised
    /// surface here now paints the same silhouette twice, once offset down by
    /// <see cref="Drop"/> in a warm near-black at low alpha, and the face on top of it. The
    /// pressed state removes the offset entirely and puts the shadow INSIDE the top edge, so a
    /// press is the object going down onto the surface rather than a fill going darker.
    ///
    /// ⚠️⚠️ THE SHADOW IS DRAWN INSIDE THE SPRITE'S OWN BOUNDS AND COSTS THE FACE ITS HEIGHT, NOT
    /// THE LAYOUT ITS SIZE. The other way to do this is to bleed the sprite outside the rect,
    /// which is what `SkinLayers.Shadow` does with a second Image; it means every width and height
    /// in every caller is a lie by the size of the bleed, and `CLAUDE.md` § 6.2c question 1 is
    /// three separate faults in this repository caused by exactly that. **A 44-unit chip is 44
    /// units of layout with a 38-unit face and 6 units of shadow under it**, and the constant is
    /// named so a caller sizing against the FACE can subtract it.
    ///
    /// -----------------------------------------------------------------------------------------
    /// THE SIX CONSTRUCTIONS, AND THEY ARE NOT VARIATIONS OF EACH OTHER
    /// -----------------------------------------------------------------------------------------
    ///
    /// ⚠️⚠️ 🧑 HAS REJECTED "ONE CALL WITH A DIFFERENT FILL" THREE TIMES NOW: *"the issue with old
    /// UI is everything feels repetitive bcz i think u use the same code to generate them all"*,
    /// *"DONT USE THE SAME METHODS IN MAKING DIFF PAGES AND PANELS"*, and *"maybe bcz u just
    /// recolored them all"*. So each role below differs in SILHOUETTE and in RELIEF, and the
    /// difference survives a photograph and a colourblind player.
    ///
    /// | Role | Silhouette | Relief | What it means |
    /// |---|---|---|---|
    /// | `Sheet` | soft 18-unit round | halo, flat cream face, a soft cast shadow | furniture: a rail, a card, a panel |
    /// | `Token` | full pill | halo, warm face, a heavy lip AND a cast shadow | you can press it |
    /// | `Live` | full pill | wood-dark face, cream lettering, the same lip | the one of a set you are ON |
    /// | `Tray` | tight 8-unit round | NO halo, NO shadow, an inner shadow along the TOP | you read it or type in it |
    /// | `Ghost` | soft 18-unit round | two hairlines, no fill, no shadow | nothing is here YET |
    /// | `Sign` | soft 18-unit round | halo, cream face, a solid amber band, a cast shadow | the one fact on the screen |
    /// | `Action` | **chamfer**, 0.34 of the height | bright keyline, dark rim, full-height ramp, a varnish band, a 22 per cent wall, a contact shadow | the ONE thing the screen is for |
    ///
    /// ⚠️⚠️ `Action` IS THE SEVENTH AND IT IS THE ONLY CHAMFER, WHICH IS `CLAUDE.md` § 6.5 RATHER
    /// THAN AN EXCEPTION TO IT: *a chamfer means pressable and a round means furniture*, in his
    /// art with no exception. Six rounds and one cut corner is that rule with one action per
    /// screen. 🧑 chose the shape after seeing both: **"i kinda preferred the sharper edges on
    /// this, i js wanted u to make it mroe 3d"**. `docs/TODO.md` § 121.1.
    ///
    /// ⚠️ THE HALO IS OUTSIDE THE ARTWORK, WHICH IS THE LOGO'S OWN MOVE AND NOT A BORDER. Every
    /// letter of `TUMP` and the blob behind it keep a band of sand outside their own edge, which is
    /// what makes the mark read as a cut-out lying on a surface rather than as a shape drawn on
    /// one. A wooden control is the opposite construction: a BRIGHT thin keyline over a DARK rim
    /// over a gradient face. The two never read as the same object, which is the point of having
    /// both on one screen.
    ///
    /// ⚠️⚠️ AND A PAPER FACE HAS NO GRADIENT AT ALL. `WoodCraft`'s whole face is a five-stop value
    /// ramp with a varnish band, because painted wood under a light does that. Paper does not: it
    /// is one flat tone with fibre in it, and the only shading on it is where an edge is
    /// physically above or below its neighbour. That is what makes a screen of these read as calm
    /// where a screen of planks reads as busy, and it is the whole of 🧑's *"calming"*.
    ///
    /// ⚠️ NOTHING HERE REPAINTS HIS ART, WHICH IS `VISION.md` § 6 AND `CLAUDE.md` § 6.4. The
    /// pennants, `JOIN BUTTON`, the arrows, `TUMP.png` and the key art are all still drawn from
    /// the PNGs, untinted.
    ///
    /// ⚠️⚠️ ONE EXCEPTION AS OF 2026-09-02 AND IT IS HIS OWN CALL: THE LOBBY'S PRIMARY NO LONGER
    /// DRAWS `BUTTON LONG.png`. 🧑: **"can u js remake the entire start match button? keep the
    /// color and font and shit but remake the whole button, bcz i think trying to imrpove it
    /// manually will lead nowhere"**. The FILE is untouched and the main menu still draws it with
    /// its unfurl intact; `PaperKit.MakeAction` switches off the `Artwork` child on that one node,
    /// which is the same decision `docs/TODO.md` § 120.4 recorded for two other authored PNGs.
    ///
    /// ⚠️ THE GEOMETRY IS SHARED WITH `WoodCraft` ON PURPOSE. `WoodCraft.Depth` and
    /// `WoodCraft.Finish` are `internal` and used here rather than copied: that file's own note
    /// says the distance field is "the part that should be shared", and three copies of a rounded
    /// rect is how two materials pick up different corner anti-aliasing and the screen grows a
    /// seam nobody can name.
    /// </summary>
    public static class PaperCraft
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>
        /// The six things a paper surface is allowed to be.
        ///
        /// ⚠️⚠️ A CLOSED LIST, FOR THE REASON `WoodCraft.Surface` IS ONE. The caller picks a
        /// MEANING and the silhouette, the relief and the colour all follow. There is deliberately
        /// no `fill` parameter anywhere in this file: the failure both of the previous two passes
        /// shipped is a screen of plates that were one call with a different colour, and the way
        /// that happens every time is that the colour is a parameter.
        /// </summary>
        public enum Surface
        {
            /// <summary>
            /// A button drawn the way the LOGO is drawn: a flat fill inside a thick uneven
            /// deep-red stroke, with a darker bar sitting inside the bottom edge.
            ///
            /// ⚠️⚠️ 🧑 ASKED FOR THIS IN AS MANY WORDS, 2026-09-03, AFTER SEEING THE FONT PASS ON
            /// ITS OWN: **"the darumadrop buttons AS TEXT stay, i wanted u to remake all buttons
            /// in a diff style that feels like my logo bruh"**, and before that *"can u overhaul
            /// it like all of it gang, i dont wanna use the old colors anymore"*. The lettering
            /// was never the complaint. **The SURFACE was.**
            ///
            /// ⚠️⚠️ IT IS A DIFFERENT CONSTRUCTION FROM EVERY OTHER SURFACE IN THIS FILE, WHICH IS
            /// THE WHOLE POINT RATHER THAN AN INCONSISTENCY. `Token`, `Live`, `Action` and `Tray`
            /// are all LIT SOLIDS: a value ramp down the face, a keyline, a rim, a cast shadow.
            /// That vocabulary came from sampling his `BUTTON LONG.png`, and it is faithful to
            /// that art. **His logo is drawn by completely different rules**: there is no ramp
            /// anywhere in it, no bevel and no lit edge. Every shape is a FLAT colour held inside
            /// a heavy irregular line, and the only depth in the whole mark is a darker red bar
            /// tucked inside the bottom of each letter. Painting a button in that language means
            /// throwing the ramp away, not softening it.
            ///
            /// ⚠️ THE STROKE WOBBLES ON Y AND NOT ON X, AND THAT IS A SLICING CONSTRAINT RATHER
            /// than a design one. These sprites are nine-sliced horizontally, so the middle column
            /// is STRETCHED: any variation along x would smear into streaks at whatever width the
            /// caller happens to be. Varying the silhouette with y makes the left and right edges
            /// hand-drawn and leaves the top and bottom straight, which is also how the mark
            /// itself reads, its horizontals being much steadier than its verticals.
            ///
            /// ⚠️ AND `CLAUDE.md` § 6.5'S RULE SURVIVES: pick a ROLE, not a fill. This is a role,
            /// the closed `Accent` list still decides the one authored colour, and no caller may
            /// pass an arbitrary hue.
            /// </summary>
            Brand,

            /// <summary>Furniture: a rail, a card, a panel.</summary>
            Sheet,

            /// <summary>A pressable paper control.</summary>
            Token,

            /// <summary>A value you read or a field you type in.</summary>
            Tray,

            /// <summary>An empty slot.</summary>
            Ghost,

            /// <summary>
            /// The one fact on the screen: a wood plaque with cream lettering.
            ///
            /// ⚠️⚠️ IT WAS A CREAM PLATE WITH AN AMBER BAND AND 🧑 REJECTED THE AMBER BY EYE, WITH
            /// A CROP OF THIS EXACT CONTROL: **"this yellow dont look good withh creme too btw,
            /// inncase u reuse taht sit"**. He is right and the number agrees: `ffba00` on
            /// `f4ecdd` is a **1.7:1** contrast ratio, so on a cream front end amber can only ever
            /// be a shape and never a word, and even as a shape it is a high-chroma stripe against
            /// a 6-per-cent-saturated field. It was the loudest and least useful thing on the rail.
            ///
            /// ⚠️ SO THE MARKER ROLE MOVES FROM HUE TO VALUE, which is the same move
            /// <see cref="Live"/> makes and for the same reason. `docs/TODO.md` § 118.4 says *amber
            /// is the marker*; that rule was written for a WOODEN front end, where amber was the
            /// one light thing on a dark screen. Invert the field and the rule inverts with it:
            /// **on cream, the marker is the one DARK thing.** A wood plaque with cream lettering
            /// is 10:1 against the sheet it sits on, it is the same brown as the primary action, and
            /// it introduces no colour at all.
            /// </summary>
            Sign,

            /// <summary>
            /// A carved wooden signboard with a raised frame round it. The game's own name, and
            /// nothing else.
            ///
            /// ⚠️⚠️ 🧑 ASKED FOR IT IN THREE MESSAGES, WITH A CROP OF THE LOGIN CARD'S WORDMARK:
            /// **"can u add a pretty outline for this too"**, *"one that reads as wooden frame or
            /// some shit?"*, **"can u give it a wooden texture too the tump box"**.
            ///
            /// ⚠️⚠️ IT IS A NEW SURFACE RATHER THAN AN EDIT TO <see cref="Sign"/>, AND THE REASON
            /// IS A SECOND CONTROL. `Sign` is also the lobby's ROOM CODE plaque and the ranked
            /// rail's plate, which are VALUES you read at a glance; growing them a carved frame
            /// and a grain would be spending detail on two controls he did not ask about and
            /// making the room code compete with the primary beside it. **A role is what varies
            /// here, never a fill** (`CLAUDE.md` § 6.5), and "the one plaque the game's own name is
            /// carved into" is a different role from "a value on a dark plate".
            ///
            /// ⚠️⚠️ THE FRAME IS A REAL BAND, NOT AN OUTLINE, WHICH IS THE WHOLE OF *"reads as
            /// wooden frame"*. An outline is a line drawn at the silhouette and reads as a border
            /// on a rectangle; a frame is a RAISED ledge with its own lit top edge and its own
            /// shadow falling inward onto the field it surrounds, so the field reads as sitting
            /// BEHIND it. That inward shadow is the entire difference and it costs one gradient.
            ///
            /// ⚠️ AND THE GRAIN IS ONE-DIMENSIONAL. `WoodCraft`'s own note says it in as many
            /// words: *"two-dimensional noise reads as dirt"*. Wood grain runs along the plank, so
            /// the noise is sampled on x alone and the same value is written down the whole
            /// column; `PaperCraft.Fibre` samples both axes because paper fibre genuinely is
            /// two-dimensional. Using the wrong one of the two is how a wooden sign ends up
            /// looking mouldy.
            ///
            /// ⚠️ IT IS DRAWN IN 🧑'S OWN CONSTRUCTION ORDER, which is the sampling `WoodCraft`'s
            /// header records off `Art/ui/host-game/*.png`: a **bright keyline** outside a **dark
            /// rim** over a **full-height ramp** with a **varnish band** a quarter of the way down.
            /// The same five layers `Surface.Action` uses, at a sign's proportions instead of a
            /// button's, so the plaque belongs to the same family as the primary under it without
            /// being mistakable for something you can press. **A chamfer means pressable and a
            /// round means furniture** (§ 6.5): this is rounded, on purpose, because it is a sign.
            /// </summary>
            Plaque,

            /// <summary>
            /// The one of a set you are ON: the live tab, the selected half of a switch.
            ///
            /// ⚠️⚠️ IT EXISTS BECAUSE `Token` AGAINST `Ghost` WAS NOT ENOUGH AND THE RENDER SAID
            /// SO. Measured off `Logs/shots-runtime/Lobby-v52.png`: the live tab was `PaperWarm`
            /// `efdabe` on a `Sheet` of `Paper` `f4ecdd`, **4 per cent apart in value**, and at 44
            /// units on a cream rail PRACTICE and MULTIPLAYER were indistinguishable at a glance.
            /// The silhouette difference is real and is not enough on its own, because both shapes
            /// are pills and the halo dominates both.
            ///
            /// ⚠️ SO IT SPENDS **VALUE**, WHICH IS THE ONE AXIS LEFT. `docs/TODO.md` § 118.4
            /// forbids putting the amber accent on a tab and `game-ui-design` forbids saying "this
            /// one" in hue alone. A wood-dark pill with cream lettering is a 10:1 inversion against
            /// its neighbour, it costs no new colour (`UiTheme.WoodMid` has been in the palette
            /// since `ui_theme.gd`), and it puts a little of 🧑's own wood back on a screen that is
            /// otherwise entirely paper.
            /// </summary>
            Live,

            /// <summary>
            /// The ONE action on a screen: START MATCH, CREATE ACCOUNT, KEEP AND USE, CHOOSE.
            ///
            /// ⚠️⚠️ IT EXISTS BECAUSE THE PRIMARY WAS THE LAST CONTROL IN THIS FRONT END STILL
            /// DRAWN IN WOOD, AND HE FOUND IT ON FOUR SCREENS WITHOUT CONNECTING THEM. 2026-09-02:
            /// **"u really have to redesign start match button, it doesnt FEEL like a start match
            /// button"**, then the correction that names the fault exactly, **"i like the size adn
            /// color but it feells so flat, it doesn thave start match energy"**, and on the
            /// maker's footer, **"i dont get why theres rounded sshit next to square shit"**.
            ///
            /// ⚠️⚠️ THE "GREY" HE CAN SEE AND CANNOT NAME IS MEASURED, AND IT IS THE SHADOW.
            /// `tools/sample_png.js row` on `Logs/shots-runtime/SignInCreate-v56.png` at y=767:
            /// the cream field is `f4ecdd` (hue 39, **sat 9 per cent**), a paper control's own
            /// edge is `dcc19a` (hue 35, **sat 30**), and the wooden primary's halo is `ada69b`
            /// (hue 37, **sat 10, value 68**). **Same hue, a third of the chroma, eighteen value
            /// steps darker than the sheet.** `CLAUDE.md` section 6.4 bans cold grey in any layer,
            /// and a 10-per-cent-saturation neutral beside a 30-per-cent warm edge is that rule
            /// caught on the warm axis rather than on the blue one. `docs/TODO.md` 121.1.
            ///
            /// ⚠️ SO IT IS A PILL, LIKE EVERY OTHER PRESSABLE THING HERE. `WoodCraft.Surface.Action`
            /// is chamfered, which was correct while the front end was wood: 6.5's rule is *a
            /// chamfer means pressable and a round means furniture*, and when everything around it
            /// became a rounded paper token the chamfer stopped saying "press me" and started
            /// saying "different object". **The rule did not change; the surroundings did.**
            ///
            /// ⚠️ AND IT IS BUILT IN HIS ART'S OWN ORDER, which is what makes it read as one of
            /// his rather than as a coloured rectangle: `WoodCraft`'s header samples every surface
            /// he authored as a **bright keyline outside a dark rim over a full-height ramp**, and
            /// this is that construction at paper's corner radius with a warm cast shadow.
            /// </summary>
            Action,
        }

        /// <summary>
        /// The two fills a <see cref="Surface.Action"/> may take.
        ///
        /// ⚠️⚠️ A CLOSED LIST OF TWO AUTHORED COLOURS, WHICH IS WHY IT IS NOT THE `fill` PARAMETER
        /// THE ENUM ABOVE FORBIDS. `docs/VISION.md` and `CLAUDE.md` 6.5 say a fill must not be the
        /// only difference between two ROLES, because that is how a screen becomes twelve plates
        /// from one call. There is exactly one role here, it appears **once per screen** by
        /// construction, and both members are colours he authored rather than colours anybody
        /// picked: `Green` is the measured peak of his `JOIN BUTTON.png` and `Wood` is the brown
        /// he asked to keep by name on the lobby (*"i like the size adn color"*, and 119.10's
        /// *"u can also still use the brown color ... start match lowk looks good"*).
        /// </summary>
        public enum Accent
        {
            /// <summary>His own green. `JOIN BUTTON.png` and the PLAY pennant are both authored in
            /// it, which `docs/VISION.md` section 6.5 calls evidence rather than taste.</summary>
            Green,

            /// <summary>The lobby's brown, kept because he asked for it on that screen.
            ///
            /// ⚠️ ON <see cref="Surface.Brand"/> THIS IS HONEY QUARTZ AND NOT BROWN. The enum is
            /// a closed list of two, and what the two MEAN is decided by the surface reading them:
            /// on `Action` they are his authored green and his authored brown, and on `Brand` they
            /// are the logo's action colour and its quiet one. The name is kept rather than
            /// widened because renaming it would touch every call site in the front end for no
            /// behaviour, and `docs/TODO.md` § 133 already has a font change and a palette change
            /// in it.
            /// </summary>
            Wood,

            /// <summary>
            /// The dark side of the inversion, for a LIVE tab.
            ///
            /// ⚠️⚠️ A TAB ROW NEEDS A PAIR AND THE PAIR MAY NOT BE TWO HUES. `docs/TODO.md`
            /// § 118.4 and `ConvertedCharacterSelect`'s own note both settle it: the difference
            /// between the tab you are on and the tab you are not is a **value inversion of about
            /// 10:1**, which is the one difference that survives a photograph in greyscale and a
            /// colourblind player. Marking a live tab with Chartreuse instead would make it the
            /// same colour as the screen's one primary, and there is only ever one of those.
            ///
            /// ⚠️ IT IS THE PALETTE'S OWN DARK, not a brown: Army darkened, which is the only
            /// dark tone in the logo that is not the outline red.
            /// </summary>
            Dark,
        }

        /// <summary>
        /// What the pointer is doing to a paper control.
        ///
        /// ⚠️⚠️ A PRESS PUTS THE OBJECT DOWN ONTO THE SURFACE. The cast shadow collapses, the lip
        /// goes, and the shading moves inside the TOP edge, which is what a physical token being
        /// pushed flat actually looks like. `WoodCraft.Pose` records the same decision one material
        /// over and for the same reason: a control that merely darkens when pressed is confusable
        /// with a control that is disabled, and that is the one state it must never be mistaken
        /// for. `PaperButton` also sinks the LABEL two units, which is Godot's own behaviour and
        /// what `GodotButton` does for the wooden set.
        /// </summary>
        public enum Pose
        {
            Rest,
            Hover,
            Press,
            Off,
        }

        /// <summary>
        /// The halo, in units, and it is thick on purpose.
        ///
        /// ⚠️ MEASURED OFF `TUMP.png` RATHER THAN CHOSEN: the sand band around the wordmark is
        /// about 26 px on a 1240 px mark whose letters are about 300 px tall, which is 8.5 per
        /// cent of the letter height. On a 56-unit control that is 4.8. Four is the value that
        /// survives being drawn: at three it aliases into the fill at the corners, and at six a
        /// small chip is more halo than face.
        /// </summary>
        private const int Halo = 4;

        /// <summary>
        /// How far a raised surface stands off the thing behind it.
        ///
        /// ⚠️⚠️ IT COMES OUT OF THE FACE, NOT OUT OF THE LAYOUT. See the class note: a 44-unit
        /// chip is 44 units to every `LayoutElement` in the project and has a 38-unit face with 6
        /// units of shadow under it. Bleeding outside the rect instead is what makes every size in
        /// every caller a lie, which `CLAUDE.md` § 6.2c question 1 records three times.
        ///
        /// ⚠️ SIX, MEASURED AGAINST THE SMALLEST CONTROL RATHER THAN THE LARGEST. On a 44-unit
        /// chip six units is 14 per cent of the height, which is enough to see at a glance and
        /// little enough that the face still clears `game-ui-design`'s 32-unit pointer floor with
        /// 38. On a 192-unit rail the same six units is a hairline, which is correct: a rail lies
        /// almost flat and a button stands up.
        /// </summary>
        public const int Drop = 6;

        /// <summary>The soft corner every non-pill paper surface takes.
        /// ⚠️ A CONSTANT AND NOT A FRACTION OF HEIGHT, which is the opposite of `WoodCraft`'s
        /// chamfer. A cut-out has ONE pair of scissors: a tall card and a short row cut from the
        /// same sheet have the same corner, and expressing it as a fraction is what makes a stack
        /// of rows look like a stack of different objects.</summary>
        private const int SoftCorner = 18;

        /// <summary>A tray's corner: tighter, because a slot cut INTO a sheet has a smaller radius
        /// than the sheet's own outside edge. It is what says "inside".</summary>
        private const int TrayCorner = 8;

        /// <summary>
        /// The cast shadow under every paper control.
        ///
        /// ⚠️ WARM, because `CLAUDE.md` § 6.4 bans cold grey in any layer and a neutral black at
        /// 30 per cent over cream composites as exactly that.
        ///
        /// ⚠️⚠️⚠️ AND IT IS `ActionShade`'S OWN VALUE NOW, BECAUSE THE MEASUREMENT § 121.1 TOOK ON
        /// THE PRIMARY WAS TRUE OF EVERY OTHER CONTROL AND ONLY THE PRIMARY WAS FIXED. 🧑
        /// 2026-09-02, with a crop of the login card: **"the shadows suck too"**, of SIGN IN,
        /// CREATE, PLAY AS GUEST and the wordmark plaque.
        ///
        /// **He is describing the same grey and the arithmetic is the same arithmetic.** This was
        /// `(0.30, 0.19, 0.10)` at **0.34** alpha; composited over `Paper` `f4ecdd` that lands at
        /// **`bbac9b`, 17 per cent saturation**, against every paper EDGE on the same screen at
        /// **30** (`PaperEdge` `dcc19a`). A 17-per-cent neutral beside a 30-per-cent warm edge is
        /// § 6.4's cold-grey ban caught on the warm axis, which is exactly what
        /// `Surface.Action`'s own note says about the wooden halo it replaced.
        ///
        /// ⚠️⚠️ `ActionShade`'S NOTE ALREADY DID THIS SUM AND SCOPED IT TOO NARROWLY: *"fine under
        /// a 40-unit chip, and under a 520-unit slab it is the wide grey band"*. **The size
        /// argument was wrong.** A shadow's saturation does not depend on the width of the object
        /// casting it; what the extra width changed was how much of it there was to see. A rich
        /// brown at half alpha composites to about `a78d79`, **hue 26 at 28 per cent**, which sits
        /// with the 30 every paper edge already carries at any size.
        ///
        /// ⚠️ SO THERE IS ONE SHADOW COLOUR IN THIS FILE AGAIN, which is the point. Two constants
        /// for one material is how a front end acquires two shadows, and `ActionShade` is kept as
        /// an alias rather than deleted so the primary's own note stays findable.
        /// </summary>
        /// ⚠️⚠️⚠️ `WoodEdge` AT 0.48, NOT `WoodMid` AT 0.50, AND IT IS LIGHTER AND MORE SATURATED
        /// AT ONCE RATHER THAN A TRADE BETWEEN THE TWO. 🧑 2026-09-02, with crops of the login's
        /// CREATE ACCOUNT and the maker's KEEP AND USE: **"can we lessen or fix shadows here"**,
        /// **"shadow here js looks so bad"**, and then the one that says it is not a tuning note,
        /// **"rlly dont get that shadow"**. Composited over `Paper` `f4ecdd`:
        ///
        /// | | Composite | Hue | Saturation | Value |
        /// |---|---|---|---|---|
        /// | `WoodMid` at 0.50 (was) | `a78e79` | 27 | **27.8 %** | **65.5 %** |
        /// | `WoodEdge` at 0.48 (is) | `c2a286` | 28 | **31.0 %** | **75.9 %** |
        /// | `PaperEdge` `dcc19a`, the target | | 35 | 30.0 % | 86.3 % |
        ///
        /// ⚠️⚠️ THE OLD VALUE NEVER MET THE BOUND ITS OWN NOTE SET, AND THE NOTE ROUNDED IN ITS
        /// OWN FAVOUR. § 121.1 asked for a shadow *"at or above the 30 per cent saturation every
        /// other paper edge on the screen carries"*; `WoodMid` at half alpha lands at **27.8** and
        /// was written up as 28. So the control 🧑 kept photographing was carrying the exact fault
        /// the previous pass believed it had fixed.
        ///
        /// ⚠️⚠️ AND A DEEPER BROWN CANNOT REACH 30 BY GETTING DARKER. Past about half alpha the
        /// composite runs toward the ink, so saturation climbs a point at a time while VALUE falls
        /// away, which is the heavy band he photographed twice. **The chroma has to come from the
        /// colour and not from the alpha.** `WoodEdge` `8b5227` is the palette's own edge brown
        /// (`CLAUDE.md` § 6.4 names it), and being lighter to begin with it clears 30 per cent
        /// while sitting ten value steps higher than what it replaces.
        ///
        /// ⚠️ SO THE SHADOW GOT WARMER, MORE COLOURFUL AND WEAKER TO THE EYE AT THE SAME TIME,
        /// which is the only combination that answers both *"the shadows suck too"* and *"rlly
        /// dont get that shadow"*. Washing it out with alpha instead would put back the
        /// 17-per-cent neutral that earned the first complaint: **a weak shadow and a grey shadow
        /// are the same pixel.** How much of it there is to see is `ActionDrop`'s half.
        private static readonly Color Shade = new Color(
            UiTheme.WoodEdge.r, UiTheme.WoodEdge.g, UiTheme.WoodEdge.b, 0.48f);

        /// <summary>
        /// The warm scrim any paper screen puts over the live street behind it.
        ///
        /// ⚠️⚠️ IT IS WARM AND IT IS WEAK, AND BOTH ARE `CLAUDE.md` § 6.2c QUESTION 3. A scrim
        /// buys legibility or separation and is not decoration; every word on these screens sits
        /// on an opaque sheet already, so the only job left is separation. A cold or heavy scrim
        /// here would be dimming the subject in exchange for nothing, which is § 100's fault.
        /// </summary>
        public static readonly Color Scrim = new Color(0.14f, 0.09f, 0.05f, 0.38f);

        /// <summary>
        /// A paper surface at a given height.
        ///
        /// ⚠️ HEIGHT MATTERS BECAUSE A PILL'S RADIUS IS HALF OF IT and because the sprite is
        /// sliced horizontally only below `WoodCraft.TallSurface`. <see cref="PaperSkin"/> is what
        /// keeps a laid-out control honest; nothing should call this directly except a caller that
        /// genuinely knows its own final height.
        /// </summary>
        public static Sprite Slab(Surface surface, float height, Pose pose = Pose.Rest,
                                  Accent accent = Accent.Green)
        {
            // ⚠️⚠️ AN ACTION IS NEVER "TALL", AND WITHOUT THIS THE PRIMARY LOSES ITS SHADOW.
            // `WoodCraft.TallSurface` is the height above which a surface is treated as a board
            // and sliced on both axes, and `PaintRaised` drops the cast shadow for a tall one
            // because a rail lies almost flat. The lobby's primary is 96 units, which is exactly
            // at that boundary, so the one control on the screen that most needs to stand up was
            // the one being drawn as furniture. It is pinned to the raised construction here.
            bool tall = surface != Surface.Action && surface != Surface.Brand
                       && height > WoodCraft.TallSurface;

            int h = surface == Surface.Action || surface == Surface.Brand
                ? Mathf.Clamp(Mathf.RoundToInt(height / 4.0f) * 4, 24, 160)
                : tall ? 96
                : Mathf.Clamp(Mathf.RoundToInt(height / 4.0f) * 4, 20, WoodCraft.TallSurface);

            // ⚠️ THE ACCENT IS IN THE KEY ONLY FOR AN ACTION. Every other surface ignores it, and
            // putting it in their keys would double a cache that is already keyed on four things.
            string key = surface == Surface.Action || surface == Surface.Brand
                ? $"pc_{surface}_{accent}_{pose}_{h}"
                : $"pc_{surface}_{pose}_{h}_{tall}";

            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            Sprite made;
            switch (surface)
            {
                case Surface.Brand: made = PaintBrand(h, pose, key, accent); break;
                // ⚠️⚠️ EVERY CHIP AND EVERY TAB IS PAINTED IN THE BRAND LANGUAGE TOO, AND
                // AGAIN IT IS THE PAINTER THAT MOVED RATHER THAN THE SURFACE. `Token` and `Live`
                // are read by name all over the front end to decide LABEL COLOUR and the tab
                // inversion (`PaperButton.Restyle`, `PaperKit.MarkLive`, `PlayerHub.Highlight`),
                // so repointing the enum would have changed a look and broken a contract in one
                // line. The accent each one takes is fixed by the surface: a chip is the quiet
                // fill and a live tab is the dark one, which keeps the 10:1 value inversion
                // § 118.4 settled.
                case Surface.Token: made = PaintBrand(h, pose, key, Accent.Wood); break;
                case Surface.Live: made = PaintBrand(h, pose, key, Accent.Dark); break;
                // ⚠️⚠️ THE PRIMARY IS PAINTED IN THE BRAND LANGUAGE NOW, AND IT IS THE
                // PAINTER THAT MOVED RATHER THAN THE SURFACE. 🧑 2026-09-03: **"i wanted u to
                // remake all buttons in a diff style that feels like my logo bruh"**.
                // `Surface.Action` is read by name in five places (`PaperKit.MakeAction`,
                // `PaperButton`'s label handling and its hover rule, and two guards in
                // `PaperDress`), all of which are about BEHAVIOUR rather than about paint;
                // repointing the enum would have moved a look and broken a contract in the same
                // line. Swapping the painter moves exactly the thing he asked about.
                case Surface.Action: made = PaintBrand(h, pose, key, accent); break;
                case Surface.Tray: made = PaintTray(h, pose, tall, key); break;
                case Surface.Ghost: made = PaintGhost(h, tall, key); break;
                case Surface.Sign: made = PaintPlate(h, tall, key, true); break;
                case Surface.Plaque: made = PaintPlaque(h, key); break;
                default: made = PaintPlate(h, tall, key, false); break;
            }

            Cache[key] = made;
            return made;
        }

        // -----------------------------------------------------------------------------------
        // PLATE: the furniture, and the one marked sign.
        // -----------------------------------------------------------------------------------

        /// <summary>
        /// A piece of cut card lying on the street, with its own shadow under it.
        ///
        /// ⚠️ THE `Sign` IS THE SAME CONSTRUCTION IN WOOD: same silhouette, same halo, same cast
        /// shadow, inverted values. See <see cref="Surface.Sign"/> for why the marker is a dark
        /// plaque rather than an amber band, and 🧑's crop for who decided it.
        ///
        /// ⚠️ A DARK PLATE STILL GETS A LIT LIP ALONG ITS TOP EDGE, which a cream one does not
        /// need. On cream the halo alone separates the plate from the sheet; on wood the halo is
        /// the darkest thing in the frame, so without a lit top edge the plaque reads as a hole cut
        /// in the rail rather than as an object nailed to it. That is `WoodCraft.PaintSlate`'s own
        /// finding one material over.
        /// </summary>
        private static Sprite PaintPlate(int h, bool tall, string key, bool sign)
        {
            int cap = SoftCorner + Halo + Drop + 2;
            int width = (cap * 2) + 4;
            var pixels = new Color[width * h];

            int face = h - Drop;
            int lip = Mathf.Max(2, Mathf.RoundToInt(face * 0.04f));

            // ⚠️⚠️ THE SIGN'S DARK IS THE BRAND'S DARK NOW, NOT THE SHARED WOOD. It read
            // `WoodMid` over `WoodDeep`, which are two of the seven constants this pass
            // deliberately did NOT move because the in-match layer reads them and § 133.4 scopes
            // the HUD out. The result was that the ROOM CODE plate and the seat tags were the
            // last brown objects on an otherwise repainted lobby
            // (`Logs/shots-runtime/Lobby-v82.png`).
            //
            // ⚠️ SO THE POINTER MOVED RATHER THAN THE CONSTANT, which is the same move the
            // painters made one level up. `WoodFace` is Army darkened and is already the front
            // end's dark tone; cream on it measures about 9:1, so the plaque keeps the value
            // inversion `Surface.Sign` exists for and loses the brown.
            Color fill = sign ? UiTheme.WoodFace : UiTheme.Paper;
            Color ring = sign ? UiTheme.WoodSlot : UiTheme.PaperEdge;

            for (int y = 0; y < h; y++)
                for (int x = 0; x < width; x++)
                {
                    float faceDepth = Depth(x, y - Drop, width, face, SoftCorner);
                    float shadowDepth = Depth(x, y, width, face, SoftCorner);

                    Color c;

                    if (faceDepth > 0.0f)
                    {
                        int upFromFace = y - Drop;
                        int downFromTop = (face - 1) - upFromFace;

                        c = faceDepth <= Halo
                            ? Fade(ring, faceDepth)
                            : Fibre(fill, x, y);

                        // ⚠️⚠️ EVERY PLATE CATCHES THE LIGHT ALONG ITS TOP EDGE, AND THIS IS THE
                        // ONE DETAIL THAT MAKES CREAM READ AS CARD RATHER THAN AS FILL. 🧑: *"make
                        // it look prettier"*, and, earlier, *"i js dont wwant it too flat"*. It is
                        // `WoodCraft`'s varnish band arriving on the opposite material: a flat face
                        // with a cast shadow under it is an object seen from directly in front,
                        // and a lit top edge is the same object seen under a light that is
                        // somewhere. Two units, because on cream a bright edge is visible long
                        // before it is loud.
                        if (!tall && faceDepth > Halo && downFromTop < lip)
                            c = Color.Lerp(c, sign ? UiTheme.BrandArmy : Color.white,
                                           sign ? 0.85f : 0.55f);
                    }
                    else if (shadowDepth > 0.0f && !tall)
                    {
                        // ⚠️⚠️ THE SAME SQUARED FALLOFF `PaintRaised` HAS, AND THIS METHOD NEVER
                        // HAD IT. It painted a flat band of `Shade` across the whole `Drop`, which
                        // was survivable at the old 0.34 alpha and is a hard dark bar at the 0.50
                        // that constant now carries. **A cast shadow that does not fade is a
                        // second, darker copy of the object**, which is the sticker read
                        // `PaintRaised`'s own note describes.
                        //
                        // ⚠️ IT IS THE SAME CORRECTION `Surface.Action` TOOK (cubed there, squared
                        // here) FOR THE SAME REASON: darkest where the two surfaces nearly touch,
                        // gone by the time it has travelled the object's own thickness. 🧑
                        // 2026-09-02, of the login card and its plaque: **"the shadows suck too"**.
                        int below = Drop - y;
                        float reach = below <= 0 ? 1.0f : 1.0f - (below / (float)(Drop + 1));
                        reach = Mathf.Clamp01(reach);

                        c = Fade(Shade, shadowDepth);
                        c.a *= reach * reach;
                    }
                    else
                    {
                        c = Color.clear;
                    }

                    pixels[(y * width) + x] = c;
                }

            return WoodCraft.Finish(pixels, width, h, cap, tall, key);
        }

        // -----------------------------------------------------------------------------------
        // PLAQUE: the game's own name, carved into a framed board.
        // -----------------------------------------------------------------------------------

        /// <summary>
        /// How much of the plaque's height the frame band takes.
        ///
        /// ⚠️ 0.11, AND IT IS A FRACTION BECAUSE A FRAME IS A PROPORTION OF ITS BOARD. This is the
        /// opposite call from <see cref="SoftCorner"/>, which is a constant precisely because a
        /// cut-out has one pair of scissors; a frame is a piece of moulding, and a wide board with
        /// a hairline round it reads as a printed border rather than as a made object. On the
        /// login card's 146-unit plaque that is 16 units, which is about the width of one letter's
        /// stroke in `TUMP.png` and therefore the width the mark itself makes look right.
        /// </summary>
        private const float FrameBand = 0.11f;

        /// <summary>
        /// A framed wooden board, with the field recessed inside the frame.
        ///
        /// ⚠️⚠️ SIX LAYERS, AND THE ONE THAT MAKES IT A FRAME RATHER THAN AN OUTLINE IS THE
        /// FOURTH. 🧑 2026-09-02, with a crop of the login wordmark: **"can u add a pretty outline
        /// for this too"**, *"one that reads as wooden frame or some shit?"*, **"can u give it a
        /// wooden texture too the tump box"**.
        ///
        /// Outward to inward:
        ///
        ///   1. a **cast shadow** in the bottom `Drop` units, so the board sits ON the card,
        ///   2. a **bright keyline** at the silhouette, which is the lit edge of every surface he
        ///      authored (`WoodCraft`'s header samples it on all of them),
        ///   3. a **dark rim** just inside it,
        ///   4. the **frame band**: a lit ledge running all the way round, brightest along its own
        ///      top edge and darkest along its bottom, so the moulding has a direction,
        ///   5. the **inner shadow the frame casts onto the field**, three units, strongest at the
        ///      top where the light is coming from, and
        ///   6. the **field**: darker wood with a one-dimensional grain, which is where the
        ///      wordmark is drawn.
        ///
        /// ⚠️⚠️ LAYER 5 IS THE WHOLE ARGUMENT AND IT IS ONE GRADIENT. Without it, layers 4 and 6
        /// are two flat browns meeting at a hard line, which is exactly what an "outline" looks
        /// like and exactly what he was pointing at. **A shadow falling from the frame onto the
        /// field is what says the field is BEHIND the frame**, and that single relationship is the
        /// difference between a border and a piece of joinery.
        ///
        /// ⚠️ AND THE FIELD IS DARKER THAN THE FRAME RATHER THAN LIGHTER. `TUMP.png` is about 60
        /// per cent warm off-white letters carrying their own baked dark outline (`SignInScreen
        /// .BuildLogo` has the sampling), so the mark needs a dark ground or its outline and its
        /// faces collapse into one value. That is the finding that decided `Surface.Sign` and it
        /// is unchanged; this only adds the frame around it.
        ///
        /// ⚠️ NO POSE. A plaque is furniture: it is not pressable, it has no hover and it has no
        /// disabled state, so it takes no `Pose` parameter and cannot acquire one by accident.
        /// `CLAUDE.md` § 6.3: a control that does nothing must not look pressable.
        /// </summary>
        private static Sprite PaintPlaque(int h, string key)
        {
            int cap = SoftCorner + Halo + Drop + 2;
            int width = (cap * 2) + 4;
            var pixels = new Color[width * h];

            int face = h - Drop;

            // ⚠️ THE KEYLINE AND RIM ARE `PaintAction`'S OWN RATIOS (5 and 4 per cent), which are
            // `BUTTON LONG.png`'s measured 7 px and 6 px at 135. One sampling of his art, two
            // surfaces; re-deriving them here is how the two drift apart.
            int keyline = Mathf.Max(2, Mathf.RoundToInt(face * 0.05f));
            int rim = Mathf.Max(2, Mathf.RoundToInt(face * 0.04f));
            int frame = Mathf.Max(6, Mathf.RoundToInt(face * FrameBand));
            int well = Mathf.Max(3, Mathf.RoundToInt(face * 0.03f));

            int outer = keyline + rim;
            int inner = outer + frame;

            var baseColour = UiTheme.WoodFace;

            var keyColour = WoodCraft.Shift(baseColour, 1.34f, 0.90f);
            var rimColour = WoodCraft.Shift(baseColour, 0.34f, 1.10f);
            var frameTop = WoodCraft.Shift(baseColour, 1.14f, 0.94f);
            var frameFloor = WoodCraft.Shift(baseColour, 0.66f, 1.08f);

            // ⚠️⚠️⚠️ THE FIELD IS THE ROAD, NOT WOOD, AND 🧑 REJECTED THE WOODEN ONE ON SIGHT:
            // **"is this update login bcz its ugly (the tump logo bg wtf is that)"**, 2026-09-02,
            // with a crop of this exact object. It was `UiTheme.WoodSlot`, so the plaque was a
            // brown frame around a brown field, and what he photographed reads as **a picture
            // frame with a brown photograph in it** rather than as a sign.
            //
            // ⚠️⚠️ A FRAME NEEDS SOMETHING THAT IS NOT ITSELF INSIDE IT. `WoodSlot` `36180c`
            // against the frame's `WoodFace` `793e1f` is one hue at two values, so layer 5's
            // inner shadow was the ONLY thing separating them and it is three units wide. Two
            // browns a shadow apart is the same fault § 122.11 records one screen out (a wooden
            // card on a wooden backdrop, *"background pretty ugly can we not use brown at all"*),
            // arriving inside a single control.
            //
            // ⚠️ AND IT IS THE ASPHALT `CLAUDE.md` § 6.5 ALREADY NAMES AS A SURFACE OF THIS DESIGN
            // SYSTEM, at the same values `ConvertedCharacterSelect.VerticalBackdrop` uses: hue
            // about 35 at under 15 per cent saturation. **`TUMP.png` is off-white letters carrying
            // their own baked dark outline**, and on near-black those letters are the brightest
            // thing on the login card by a wide margin, which is what a painted sign at night
            // actually looks like. Every channel still has more red in it than blue, which is
            // § 6.4's one-line test.
            var field = new Color(0.088f, 0.081f, 0.070f);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < width; x++)
                {
                    float depth = Depth(x, y - Drop, width, face, SoftCorner);
                    float shadowDepth = Depth(x, y, width, face, SoftCorner);

                    Color c;

                    if (depth > 0.0f)
                    {
                        int upFromFace = y - Drop;
                        int downFromTop = (face - 1) - upFromFace;
                        float alongFace = face <= 1 ? 0.0f : upFromFace / (float)(face - 1);

                        if (depth <= keyline)
                        {
                            c = Fade(keyColour, depth);
                        }
                        else if (depth <= outer)
                        {
                            c = rimColour;
                        }
                        else if (depth <= inner)
                        {
                            // The moulding: a full-height ramp, like every board he authored.
                            c = Color.Lerp(frameFloor, frameTop, alongFace);

                            // ⚠️ THE VARNISH BAND SITS A QUARTER DOWN FROM THE TOP, which is where
                            // it sits in every one of his slabs. A gloss line, not a shine.
                            float band = 1.0f - Mathf.Abs(alongFace - 0.76f) / 0.13f;
                            if (band > 0.0f) c = WoodCraft.Lift(c, band * 0.10f);

                            // The lit crest along the very top of the moulding, and the dark lip
                            // along the very bottom: the two edges that say which way is up.
                            if (downFromTop < keyline)
                                c = Color.Lerp(c, keyColour,
                                               0.6f * (1.0f - (downFromTop / (float)keyline)));
                            else if (upFromFace < keyline)
                                c = Color.Lerp(c, rimColour,
                                               0.7f * (1.0f - (upFromFace / (float)keyline)));
                        }
                        else
                        {
                            c = field;

                            // ⚠️⚠️ ONE-DIMENSIONAL. `WoodCraft`'s own note: *"Two-dimensional
                            // noise reads as dirt."* Grain runs along the plank, so the sample is
                            // on x alone and the same value is written down the whole column.
                            // `PaperCraft.Fibre` samples both axes because paper fibre is genuinely
                            // two-dimensional; using the wrong one here would make the sign mouldy.
                            float grain = Mathf.PerlinNoise(x * 0.35f, 0.0f) - 0.5f;
                            c = WoodCraft.Lift(c, grain * 0.030f);

                            // ⚠️⚠️ THE FRAME'S SHADOW FALLING INTO THE FIELD. This is the layer
                            // that makes the thing a frame rather than an outline: see the method
                            // header. It is strongest at the TOP, because that is where the light
                            // is, and it wraps the sides at half strength so the recess has walls.
                            float intoWell = Mathf.Clamp01((depth - inner) / well);
                            float fromTop = downFromTop < inner + well
                                ? Mathf.Clamp01(1.0f - ((downFromTop - inner) / (float)well))
                                : 0.0f;

                            float dark = Mathf.Max(fromTop, (1.0f - intoWell) * 0.5f);
                            if (dark > 0.0f)
                                c = Color.Lerp(c, UiTheme.WoodDark, dark * 0.7f);
                        }
                    }
                    else if (shadowDepth > 0.0f)
                    {
                        // ⚠️ THE SAME CONTACT SHADOW `PaintAction` ARRIVED AT: cubed, so two thirds
                        // of the alpha is spent in the first third of the drop. A squared falloff
                        // over ten units is a blur, and on cream a blur is a smudge
                        // (🧑: *"this still looks ugly, especially the shadow"*).
                        int below = Drop - y;
                        float reach = below <= 0 ? 1.0f : 1.0f - (below / (float)(Drop + 1));
                        reach = Mathf.Clamp01(reach);

                        var shade = WoodCraft.Shift(baseColour, 0.30f, 0.55f);
                        shade.a = 0.50f;

                        c = Fade(shade, shadowDepth);
                        c.a *= reach * reach * reach;
                    }
                    else
                    {
                        c = Color.clear;
                    }

                    pixels[(y * width) + x] = c;
                }

            // ⚠️ NOT `tall`, EVER. A plaque is sliced horizontally only, so its full-height ramp,
            // its crest and its lip arrive exactly as painted and only the flat middle stretches.
            // Slicing it on four sides would repeat the frame's own gradient down the sides.
            return WoodCraft.Finish(pixels, width, h, cap, false, key);
        }

        // -----------------------------------------------------------------------------------
        // RAISED: a thing you press, and the one you are on.
        // -----------------------------------------------------------------------------------

        /// <summary>
        /// A pill with a physical thickness and a shadow under it.
        ///
        /// ⚠️⚠️ THE PILL IS THE WHOLE AFFORDANCE AND IT IS DELIBERATELY NOT A CHAMFER. In 🧑's
        /// wooden art a chamfer means pressable and `WoodCraft` transcribes that faithfully. A
        /// paper control cannot borrow the chamfer without reading as a badly drawn wooden button,
        /// so it takes the other end of the same axis: the roundest shape there is. On one screen
        /// the two then say "press me" in two different languages, which is what stops a rail of
        /// paper chips and a rail of wooden buttons being the same rail twice.
        ///
        /// ⚠️ `Live` IS THIS SAME OBJECT INVERTED, not a second silhouette. Same pill, same halo,
        /// same lip, same shadow; only the values swap. Giving the selected tab its own shape
        /// would say "these two controls are different KINDS of thing", which is the opposite of
        /// what a tab pair means.
        /// </summary>
        private static Sprite PaintRaised(int h, Pose pose, bool tall, string key, bool live)
        {
            bool pressed = pose == Pose.Press;
            bool off = pose == Pose.Off;

            // ⚠️ A PRESSED CONTROL LOSES ITS SHADOW ENTIRELY: it is lying on the surface, so there
            // is no gap for a shadow to fall into. That single change is most of what makes the
            // press read without the footprint moving.
            //
            // ⚠️⚠️ AND A HOVERED ONE STANDS TWO UNITS HIGHER, WHICH IS THE ONE STATE CHANGE THAT
            // HAPPENS TO THE OBJECT RATHER THAN TO ITS COLOUR. 🧑 2026-09-01, on the pass before
            // this one: *"the butons look 2d too and blank"*, *"REWORK THE BUTTONS so that it
            // feels great to click and isnt flat"*. Hover used to be a lighter fill and nothing
            // else, so a pointer crossing the rail changed the picture's colour and not its
            // geometry. **The top edge of the face does not move** (it is pinned at `h` in every
            // pose), so the lift is bought entirely out of the gap underneath, which is where a
            // real object's lift is visible.
            int drop = pressed ? 0 : pose == Pose.Hover ? Drop + 2 : Drop;
            int face = h - drop;

            float corner = face * 0.5f;
            int cap = Mathf.CeilToInt(corner) + Drop + 4;
            int width = (cap * 2) + 4;
            var pixels = new Color[width * h];

            Color fill = live
                ? (off ? UiTheme.WoodMid : pose == Pose.Hover ? UiTheme.WoodEdge : UiTheme.WoodMid)
                : (off ? Color.Lerp(UiTheme.PaperWarm, UiTheme.PaperEdge, 0.6f)
                   : pose == Pose.Hover ? UiTheme.Paper
                   : pressed ? UiTheme.PaperSunk
                   : UiTheme.PaperWarm);

            Color ring = live
                ? UiTheme.WoodDeep
                : off ? Color.Lerp(UiTheme.PaperEdge, UiTheme.PaperSunk, 0.5f) : UiTheme.PaperEdge;

            Color lipColour = live ? UiTheme.WoodDeep : UiTheme.PaperSunk;

            // ⚠️⚠️ THE WALL IS 14 PER CENT OF THE FACE AND IT WAS 7, AND THIS IS THE HALF OF
            // *"the butons look 2d"* THAT IS IN THE SPRITE. At seven per cent a 40-unit chip's
            // wall was two units under a four-unit halo, so the only thing below the face was
            // ring: the control had a cast shadow but no THICKNESS, which is a sticker rather
            // than a token. Fourteen puts five units of card edge on the same chip, which is
            // still under a tenth of the height and is the first thing the eye reads as depth.
            //
            // ⚠️ AND IT IS A RAMP, NOT A BAND. A flat dark stripe along the bottom is the same
            // gradient-instead-of-a-hole fault `PaintTray` records one construction over: a cut
            // paper edge catches a little light at the top of the cut and none at the bottom.
            // ⚠️⚠️ 18 PER CENT SINCE 2026-09-02, UP FROM 14, AND IT IS THE SAME ASK THE PRIMARY
            // GOT: **"I want all buttons in that menu to feel like 3d and shit"**, *"its okay if
            // theyre circular and stuff"*. The primary went to a 22-per-cent wall and stopped
            // looking flat (`PaperCraft.Surface.Action`); every chip beside it kept 14 and now
            // reads as the printed version of the same object. **The wall is the cut edge and the
            // cut edge is what the eye reads as thickness**, which is the finding this file
            // already recorded when it went 7 to 14 and did not follow through.
            int wall = Mathf.Max(4, Mathf.RoundToInt(face * 0.18f));

            // ⚠️⚠️ A LIT TOP EDGE, WHICH `PaintPlate` HAS ALWAYS HAD AND THIS CONSTRUCTION NEVER
            // DID. That asymmetry is most of why the rails read as card and the chips standing on
            // them read as fill: a raised object under a light that is above the screen is bright
            // along its top and dark along its bottom, and this one only ever had the bottom half
            // of that. Two units on a 40-unit chip, at 0.55, which is `PaintPlate`'s own number.
            int crest = Mathf.Max(2, Mathf.RoundToInt(face * 0.05f));

            for (int y = 0; y < h; y++)
                for (int x = 0; x < width; x++)
                {
                    float faceDepth = Depth(x, y - drop, width, face, corner);
                    float shadowDepth = drop > 0 ? Depth(x, y, width, face, corner) : -1.0f;

                    Color c;

                    if (faceDepth > 0.0f)
                    {
                        int upFromFace = y - drop;
                        int downFromTop = (face - 1) - upFromFace;

                        if (faceDepth <= Halo - 1)
                        {
                            c = Fade(ring, faceDepth);
                        }
                        else
                        {
                            // ⚠️⚠️ THE FACE IS A GRADIENT AND IT WAS A FLAT FILL. 🧑: *"the butons
                            // look 2d too and blank"*. `WoodCraft` has expressed every one of 🧑's
                            // authored surfaces as a full-height ramp since it was written, and
                            // this file drew the same object as one colour with a rim round it.
                            // Four per cent from top to bottom: enough that the surface has a
                            // direction, little enough that a row of eight chips still reads as
                            // one material.
                            // ⚠️⚠️ THE RAMP IS 9 PER CENT AND IT WAS 4. Same note as the wall above
                            // and the same request: *"I want all buttons in that menu to feel like
                            // 3d and shit"*. Four per cent top to bottom is a direction you can
                            // measure and cannot see; nine is the first value at which a row of
                            // chips reads as eight lit objects rather than as eight fills, and it
                            // is still under a tenth, so the row is plainly one material.
                            float alongFace = face <= 1 ? 0.0f : upFromFace / (float)(face - 1);
                            c = Fibre(WoodCraft.Lift(fill, (alongFace - 0.5f) * -0.09f), x, y);

                            if (pressed)
                            {
                                // The near wall of a recess is the top one: the light in this
                                // front end is above the screen.
                                if (downFromTop < Halo + wall)
                                    c = Color.Lerp(c, UiTheme.PaperInk, 0.16f);
                            }
                            else
                            {
                                if (upFromFace < Halo + wall)
                                {
                                    float into = 1.0f - ((upFromFace - Halo)
                                                         / (float)Mathf.Max(1, wall));
                                    c = Color.Lerp(c, lipColour, Mathf.Clamp01(into) * 0.92f);
                                }

                                if (!off && downFromTop < crest)
                                    c = Color.Lerp(c, live ? UiTheme.WoodEdge : Color.white,
                                                   live ? 0.5f : 0.55f);
                            }
                        }
                    }
                    else if (shadowDepth > 0.0f && !tall && !off)
                    {
                        // ⚠️⚠️ THE CAST SHADOW FADES DOWNWARD NOW AND IT USED TO BE A FLAT COPY OF
                        // THE SILHOUETTE AT ONE ALPHA. A hard-edged block of 34 per cent brown
                        // under every chip is what a sticker printed with its own shadow looks
                        // like; a real contact shadow is darkest where the two surfaces nearly
                        // touch and gone by the time it has travelled the object's own height.
                        // The falloff is squared, which is the cheapest approximation of that
                        // and the one `PaintTray`'s inner shadow already uses.
                        int below = drop - y;
                        float reach = below <= 0 ? 1.0f
                            : 1.0f - (below / (float)(drop + 1));
                        c = Fade(Shade, shadowDepth);
                        c.a *= Mathf.Clamp01(reach) * Mathf.Clamp01(reach);
                    }
                    else
                    {
                        c = Color.clear;
                    }

                    pixels[(y * width) + x] = c;
                }

            return WoodCraft.Finish(pixels, width, h, cap, tall, key);
        }

        // -----------------------------------------------------------------------------------
        // ACTION: the one thing the screen is for.
        // -----------------------------------------------------------------------------------

        /// <summary>
        /// How far the primary stands off the sheet. ⚠️ TEN AGAINST A CHIP'S SIX, AND THE RATIO IS
        /// THE POINT. A chip is 40 units tall and lifts 6, which is 15 per cent; the primary is 96
        /// and lifting it 6 would be 6 per cent, so the biggest control on the screen would be the
        /// FLATTEST one. Ten holds it at about a tenth, which is the same object at a bigger size.
        /// </summary>
        /// ⚠️⚠️ SIX, NOT TEN, AND THE ARGUMENT FOR IT IS ONE § 122.13 WROTE DOWN AND THEN DID NOT
        /// APPLY. That note reads: *"A shadow's saturation does not depend on the width of the
        /// object casting it; what the extra width changed was how much of it there was to see."*
        /// **It then fixed the saturation and left how much there is to see at ten units**, which
        /// is the half 🧑 was actually looking at when he said **"rlly dont get that shadow"**.
        ///
        /// ⚠️ TEN UNITS UNDER A 96-UNIT CONTROL IS A TENTH OF THE OBJECT SPENT ON ITS OWN SHADOW.
        /// At that size it stops reading as lift and starts reading as a second object lying
        /// behind the first, which is why the answer to *"lessen the shadows"* is this constant
        /// rather than the alpha. `Shade` gets MORE chroma in the same pass, not less: the fault
        /// is the quantity, and fading it out is how the grey came back last time.
        private const int ActionDrop = 6;

        /// <summary>
        /// The darkest an <see cref="Surface.Action"/> rim may get, as an HSV value.
        ///
        /// ⚠️ 0.26, WHICH IS A SHADE UNDER WHERE THE GREEN PRIMARY ALREADY LANDS ON ITS OWN
        /// (`MenuGreenFace` at 0.34 of value 86.7 gives 29.5). Picking it there rather than at a
        /// round number is what makes the floor invisible on the fill that never needed it: the
        /// green is untouched by construction and only the brown, whose face is 39 value points
        /// darker, is lifted off black. See `rimColour` for the measurement and the reasoning.
        /// </summary>
        private const float RimFloor = 0.26f;

        /// <summary>
        /// The primary's cast shadow.
        ///
        /// ⚠️⚠️ IT IS A SEPARATE CONSTANT FROM <see cref="Shade"/> BECAUSE THE MEASUREMENT SAID
        /// SO. Composited over `Paper` `f4ecdd`, `Shade` lands near `bbac9b`, which is **17 per
        /// cent saturation**: fine under a 40-unit chip, and under a 520-unit slab it is the wide
        /// grey band `docs/TODO.md` 121.1 measures at `ada69b` and 10 per cent on the wooden one.
        /// A rich brown at half alpha composites to about `a78d78`, **hue 26 at 28 per cent**,
        /// which sits with the 30 per cent every paper edge on the same screen already carries.
        /// **A shadow big enough to see is a colour, and it has to be in the palette.**
        /// </summary>
        /// ⚠️ IT TRACKS <see cref="Shade"/> AND HAS DONE SINCE § 122.13 UNIFIED THEM, so the
        /// 2026-09-02 retune above reaches the primary too. Two constants for one material is how
        /// a front end acquires two shadows; this stays as an alias rather than being deleted only
        /// so the primary's own history in this note remains findable.
        private static readonly Color ActionShade = new Color(
            UiTheme.WoodEdge.r, UiTheme.WoodEdge.g, UiTheme.WoodEdge.b, 0.48f);

        /// <summary>
        /// The one action on the screen, drawn in his art's own order at paper's corner radius.
        ///
        /// ⚠️⚠️ FIVE LAYERS, AND EVERY ONE OF THEM IS A ROW IN `WoodCraft`'s SAMPLING TABLE OF THE
        /// SURFACES HE AUTHORED: a **bright keyline** outside a **dark rim** over a **full-height
        /// ramp** with a **varnish band** a quarter of the way down, plus the **lit top edge**
        /// `PaintPlate` has always had. The wooden `Action` had four of the five and a chamfer;
        /// this has all five and a pill, so it belongs to the row of paper controls it stands in
        /// rather than to the menu it came from. `docs/TODO.md` 121.1.
        ///
        /// ⚠️⚠️ THE HOVER AND THE PRESS ARE BIGGER HERE THAN ON A CHIP, ON PURPOSE. He said the
        /// SIZE and the COLOUR were already right and that it *"feells so flat"* and *"doesn
        /// thave start match energy"*, which is a statement about MOTION and DEPTH rather than
        /// about either. A hover adds four units of stand-off against a chip's two, and a press
        /// takes the whole ten away and moves the shading to the inside of the top edge, so the
        /// slab travels a fifth of its own thickness under the pointer.
        ///
        /// ⚠️ IT IS NEVER DRAWN "OFF" AS A GREY PLATE. A disabled primary desaturates towards the
        /// sheet and keeps its shape, because a primary that goes grey is the one control a player
        /// reads as broken rather than as unavailable.
        /// </summary>
        /// ⚠️⚠️ SUPERSEDED 2026-09-03 AND KEPT RATHER THAN DELETED. Nothing dispatches to this
        /// any more: `Surface.Action` is painted by <see cref="PaintBrand"/>, because 🧑 asked for
        /// every button to be redrawn in the logo's language and the logo has no lit solids in it.
        ///
        /// **It is kept because the decision it encodes is his and he has reversed one like it
        /// before.** Every number in the body below was measured off `BUTTON LONG.png` and tuned
        /// against three of his own rejections (*"i js wanted u to make it mroe 3d"*, *"i prefer
        /// the old sharper edges on it"*, *"ugly shadows and edges"*), and the character select
        /// screen is already a case where he asked for an older look to come back by name. If the
        /// lit slab is ever wanted again, this is it, intact, with its receipts; rebuilding it
        /// from the comments would take an afternoon and lose the measurements.
        ///
        /// ⚠️ `GameVersion`'s branch-stamping machinery is kept for exactly this reason and says
        /// so in `CLAUDE.md` § 7. Do not delete either without asking.
        private static Sprite PaintAction(int h, Pose pose, string key, Accent accent)
        {
            bool pressed = pose == Pose.Press;
            bool off = pose == Pose.Off;

            int drop = pressed ? 0 : pose == Pose.Hover ? ActionDrop + 4 : ActionDrop;
            int face = h - drop;

            // ⚠️⚠️ A CHAMFER, AND THE PILL IT REPLACES LASTED ONE RENDER. 🧑 2026-09-02, having
            // seen both: **"i kinda preferred the sharper edges on this, i js wanted u to make it
            // mroe 3d"**, then *"i prefer the old sharper edges on it"*. **The silhouette was
            // never the fault and the first version of this method got that wrong**: what he
            // photographed as *"a circle and a sharp shape at the same time"* was TWO objects
            // stacked, his authored `Artwork` child drawing over a new pill on the node's own
            // Image, and `PaperKit.MakeAction` is where that is fixed. Rounding the primary was
            // solving a problem that had already been solved somewhere else.
            //
            // ⚠️ SO `CLAUDE.md` § 6.5 IS BACK THE RIGHT WAY UP: *a chamfer means pressable and a
            // round means furniture*, in his art with no exception. The one action on the screen
            // is the one chamfered thing on it, which is the rule doing exactly what it says.
            //
            // ⚠️ 0.34 OF THE FACE IS MEASURED OFF `BUTTON LONG.png`'S OWN CUT rather than picked:
            // its end taper runs about a third of the slab's height. At 96 units that is a 29-unit
            // diagonal, which is what makes the shape read as a slab with its corners taken off
            // rather than as an octagon.
            float corner = face * 0.34f;
            int cap = Mathf.CeilToInt(corner) + ActionDrop + 8;
            int width = (cap * 2) + 4;
            var pixels = new Color[width * h];

            // ⚠️ BOTH BASES ARE AUTHORED. `MenuGreenFace` is the measured peak of `JOIN
            // BUTTON.png` (`UiTheme` carries that note and why `MenuGreen` is a third too dark),
            // and `WoodFace` is the brown of `BUTTON LONG.png`.
            Color baseColour = accent == Accent.Green ? UiTheme.MenuGreenFace : UiTheme.WoodFace;

            if (off) baseColour = WoodCraft.Shift(baseColour, 0.86f, 0.34f);

            // ⚠️ HOVER LIGHTENS THE WHOLE OBJECT BY ONE STEP RATHER THAN SWAPPING A COLOUR IN, so
            // the ramp, the keyline and the rim all move together and the control stays one
            // material. A hover that changes only the face is what made the old set read as a
            // colour swatch that happened to be under the pointer.
            if (pose == Pose.Hover) baseColour = WoodCraft.Shift(baseColour, 1.07f, 0.99f);

            // ⚠️⚠️ THE RAMP IS THE 3D AND IT WAS TOO SHY. 🧑, of the first build of this surface:
            // *"i js wanted u to make it mroe 3d"*. It ran 1.04 of the base at the top to 0.70 at
            // the bottom, which is a 34-point spread on a 96-unit object: enough to see and not
            // enough to read as a lit solid. `WoodCraft`'s sampling of his own art is a
            // FULL-HEIGHT ramp, and these numbers are that: **1.12 down to 0.58**, a 54-point
            // spread, with the rim below it darker still. A slab lit from above is bright along
            // its top, falls all the way down its face, and ends in a wall that is darker than
            // any part of the face.
            Color keyColour = WoodCraft.Shift(baseColour, 1.34f, 0.90f);

            // ⚠️⚠️⚠️ THE RIM IS A RATIO WITH A FLOOR, BECAUSE 0.34 OF AN ALREADY-DARK FILL IS
            // BLACK. 🧑 2026-09-02, of FIND A RANKED MATCH: **"this button ugly"**, and asked what
            // about it, **"ugly shadows and edges"**. The shadow half is `Shade`; this is the edge.
            //
            // **Measured off `Logs/shots-runtime/LobbyRanked-v71.png` at the button's top edge:
            // `291307`, value 16 per cent, half of that band.** The arithmetic: `WoodFace`
            // `793e1f` is value **47.5** and 0.34 of it is 16.1, which on the cream rail this
            // control actually sits on is a hard black outline round a brown slab.
            //
            // ⚠️⚠️ AND THE RATIO IS NOT WRONG, WHICH IS WHY THIS IS A FLOOR RATHER THAN A NEW
            // NUMBER. `MenuGreenFace` `51dd38` is value **86.7**, so the same 0.34 lands at 29.5
            // and reads as a dark green edge exactly as it does in `BUTTON LONG.png`. **The two
            // fills are 39 value points apart, so one multiplier cannot serve both**: the ratio is
            // faithful to his art and the ABSOLUTE result is what goes black. A rim is the wall
            // under a lit face, and a wall is dark; it is not an ink line.
            //
            // ⚠️ SO GREEN IS UNTOUCHED BY CONSTRUCTION. The floor only bites when `value * 0.34`
            // would fall under it, which is true of the brown at 47.5 and false of the green at
            // 86.7. `RimFloor` is 0.26, a shade under where the green already lands, so the two
            // primaries end up on the same side of black instead of on opposite ones.
            Color.RGBToHSV(baseColour, out _, out _, out float baseValue);

            float rimDrop = Mathf.Max(0.34f, RimFloor / Mathf.Max(0.01f, baseValue));
            Color rimColour = WoodCraft.Shift(baseColour, rimDrop, 1.10f);
            Color faceTop = WoodCraft.Shift(baseColour, 1.12f, 0.94f);
            Color faceFloor = WoodCraft.Shift(baseColour, 0.58f, 1.10f);

            // ⚠️ THE KEYLINE IS 5 PER CENT AND THE RIM 4, WHICH IS `BUTTON LONG.png`'S OWN RATIO
            // (7 px and about 6 px at 135) rather than a pair of round numbers.
            int keyline = Mathf.Max(2, Mathf.RoundToInt(face * 0.05f));
            int rim = Mathf.Max(2, Mathf.RoundToInt(face * 0.04f));

            // ⚠️⚠️ 22 PER CENT AGAINST A CHIP'S 14, AND THE WALL IS WHAT "3D" ACTUALLY MEANS HERE.
            // The eye reads thickness off the CUT EDGE, not off the shading on the top surface: a
            // 96-unit slab with a 21-unit wall is an object you could pick up, and the same slab
            // with a 13-unit wall is a printed shape with a gradient on it. This is the one number
            // that changed the read.
            int wall = Mathf.Max(6, Mathf.RoundToInt(face * 0.22f));
            int crest = Mathf.Max(3, Mathf.RoundToInt(face * 0.07f));

            for (int y = 0; y < h; y++)
                for (int x = 0; x < width; x++)
                {
                    float faceDepth = Depth(x, y - drop, width, face, corner, true);
                    float shadowDepth = drop > 0 ? Depth(x, y, width, face, corner, true) : -1.0f;

                    Color c;

                    if (faceDepth > 0.0f)
                    {
                        int upFromFace = y - drop;
                        int downFromTop = (face - 1) - upFromFace;
                        float alongFace = face <= 1 ? 0.0f : upFromFace / (float)(face - 1);

                        if (faceDepth <= keyline)
                        {
                            c = Fade(keyColour, faceDepth);
                        }
                        else if (faceDepth <= keyline + rim)
                        {
                            c = rimColour;
                        }
                        else
                        {
                            c = Color.Lerp(faceFloor, faceTop, alongFace);

                            // ⚠️ THE VARNISH BAND SITS A QUARTER DOWN FROM THE TOP, which is where
                            // it sits in every one of his authored slabs. It is a narrow lift
                            // rather than a highlight shape: a gloss line, not a shine.
                            float band = 1.0f - Mathf.Abs(alongFace - 0.76f) / 0.13f;
                            if (band > 0.0f) c = WoodCraft.Lift(c, band * 0.10f);

                            if (pressed)
                            {
                                if (downFromTop < rim + wall)
                                    c = Color.Lerp(c, rimColour,
                                                   1.0f - (downFromTop / (float)(rim + wall)));
                            }
                            else
                            {
                                if (upFromFace < keyline + rim + wall)
                                {
                                    float into = 1.0f - ((upFromFace - keyline - rim)
                                                         / (float)Mathf.Max(1, wall));
                                    c = Color.Lerp(c, rimColour, Mathf.Clamp01(into) * 0.98f);
                                }

                                if (downFromTop < crest)
                                    c = Color.Lerp(c, keyColour,
                                                   0.72f * (1.0f - (downFromTop / (float)crest)));
                            }
                        }
                    }
                    else if (shadowDepth > 0.0f && !off)
                    {
                        // ⚠️⚠️ A CONTACT SHADOW, NOT A HALO, AND HE NAMED THE OLD ONE AS THE WORST
                        // PART: *"this still looks ugly, especially the shadow"*. It was
                        // `ActionShade` with a SQUARED falloff over ten units, which is a soft
                        // gradient reaching a long way from the object in every direction: a blur
                        // rather than a shadow, and on cream a blur reads as a smudge.
                        //
                        // **A slab lying on paper is darkest where the two surfaces nearly touch
                        // and gone within a few units.** The falloff is cubed now, so two thirds
                        // of the alpha is spent in the first third of the drop, and the alpha it
                        // starts from is higher: a tighter, darker, warmer mark that ends where
                        // the object's own thickness ends.
                        int below = drop - y;
                        float reach = below <= 0 ? 1.0f : 1.0f - (below / (float)(drop + 1));
                        reach = Mathf.Clamp01(reach);

                        // ⚠️⚠️ THE SHADOW TAKES THE FILL'S OWN HUE, AND A FIXED BROWN ONE WAS
                        // WRONG UNDER THE GREEN. `Logs/crops/join-v64.png` at 4x: a warm brown
                        // contact shadow under a saturated green slab reads as a **grey** smear,
                        // which is the exact word 🧑 used about the old halo (*"especially the
                        // shadow"*) arriving from the opposite direction. An object's shadow is
                        // its own colour with the light taken out of it, not a second colour
                        // chosen once for every object.
                        //
                        // ⚠️ IT IS STILL WARM AND STILL IN THE PALETTE, because it is derived from
                        // a fill that is: `CLAUDE.md` § 6.4 bans a cold grey and this cannot
                        // produce one from either accent. 0.30 of the value at 0.55 of the
                        // saturation is dark enough to read on cream and desaturated enough not to
                        // look like a second, smaller button underneath the first.
                        var shade = WoodCraft.Shift(baseColour, 0.30f, 0.55f);
                        shade.a = ActionShade.a;

                        c = Fade(shade, shadowDepth);
                        c.a *= reach * reach * reach;
                    }
                    else
                    {
                        c = Color.clear;
                    }

                    pixels[(y * width) + x] = c;
                }

            return WoodCraft.Finish(pixels, width, h, cap, false, key);
        }

        // -----------------------------------------------------------------------------------
        // BRAND: a button drawn the way the logo is drawn.
        // -----------------------------------------------------------------------------------

        /// <summary>
        /// A flat fill inside a thick uneven deep-red stroke, with a darker bar inside its bottom
        /// edge. See <see cref="Surface.Brand"/> for who asked and why it throws the ramp away.
        ///
        /// **The three measurements, taken off `tump_logo_colour.jpg` rather than chosen:**
        ///
        /// - **The stroke is about 8.5 per cent of the letter height.** In the mark it is the
        ///   single largest area, 34.3 per cent of the whole drawing, which is why the logo reads
        ///   as drawn rather than as filled: the LINE is the object and the colour inside it is
        ///   the hole.
        /// - **The under-bar is about 5.5 per cent**, sits a pixel or two clear of the stroke
        ///   rather than touching it, and is the rim red rather than a shade of the fill. It is
        ///   the only depth cue anywhere in the mark.
        /// - **Nothing is lit.** No ramp, no keyline, no bevel, no varnish band. Every fill in
        ///   the logo is one flat value.
        /// </summary>
        private static Sprite PaintBrand(int h, Pose pose, string key, Accent accent)
        {
            bool pressed = pose == Pose.Press;
            bool off = pose == Pose.Off;

            // ⚠️ THE PRESS IS THE WHOLE OBJECT SITTING DOWN ONTO ITS OWN UNDER-BAR, which is the
            // only animation this construction can afford: there is no highlight to move and no
            // ramp to shift, so the bar going away IS the press. It is also the honest reading of
            // the mark, where the bar is what holds each letter up off the page.
            int drop = pressed ? 0 : BrandLift;
            int face = h - drop;
            if (face < 8) { drop = 0; face = h; }

            float corner = face * 0.22f;
            int cap = Mathf.CeilToInt(corner) + BrandLift + BrandWobble + 6;
            int width = (cap * 2) + 4;
            var pixels = new Color[width * h];

            int stroke = Mathf.Max(3, Mathf.RoundToInt(face * 0.085f));
            int bar = Mathf.Max(2, Mathf.RoundToInt(face * 0.055f));

            // ⚠️ THE CLOSED LIST IS STILL CLOSED. `CLAUDE.md` § 6.5: pick a role, not a fill.
            // Chartreuse is the ACTION and Honey Quartz is everything else, which is
            // `docs/Front_End_Design.md` § 4's role table and not a choice made here.
            Color fill = accent == Accent.Green ? UiTheme.BrandChartreuse
                       : accent == Accent.Dark ? UiTheme.WoodFace
                       : UiTheme.BrandHoney;
            Color line = UiTheme.BrandRed;
            Color under = UiTheme.BrandRimRed;

            // ⚠️ DISABLED DESATURATES THE FILL AND LEAVES THE LINE ALONE, so the control keeps its
            // silhouette and loses its voice. `game-ui-design`'s Color-Only Information
            // anti-pattern is why the shape may not change: a control distinguishable only by
            // colour is not distinguishable. `PaperButton` takes the bar off as well.
            if (off)
            {
                fill = WoodCraft.Shift(fill, 0.88f, 0.30f);
                line = WoodCraft.Shift(line, 1.25f, 0.35f);
                under = line;
            }

            // ⚠️ HOVER LIFTS THE FILL ONLY. The line is the identity of the object and a stroke
            // that brightens under the pointer reads as a different control rather than as the
            // same one being pointed at.
            if (pose == Pose.Hover) fill = WoodCraft.Shift(fill, 1.10f, 0.96f);

            for (int y = 0; y < h; y++)
            {
                // ⚠️⚠️ THE WOBBLE IS A FUNCTION OF Y ALONE AND THE SURFACE NOTE SAYS WHY: these
                // sprites are nine-sliced horizontally, so anything that varies along x is
                // stretched into streaks at whatever width the caller asks for. Sampled at two
                // frequencies because one produces a regular ripple, which reads as a wave rather
                // than as a hand.
                float n1 = Mathf.PerlinNoise(0.37f, y * 0.085f) - 0.5f;
                float n2 = Mathf.PerlinNoise(7.13f, y * 0.031f) - 0.5f;
                float wobble = ((n1 * 0.65f) + (n2 * 0.35f)) * BrandWobble * 2.0f;

                for (int x = 0; x < width; x++)
                {
                    int yy = y - drop;
                    float depth = Depth(x, yy, width, face, corner) + wobble;

                    Color c;

                    if (depth > 0.0f)
                    {
                        if (depth <= stroke)
                        {
                            c = Fade(line, depth);
                        }
                        else if (!pressed && !off
                                 && yy >= stroke + 1 && yy < stroke + 1 + bar
                                 && depth > stroke + 2)
                        {
                            // The darker bar tucked inside the bottom edge. It stops short of the
                            // stroke so the two read as two marks rather than as one thick one,
                            // which is exactly how it is drawn under the letters.
                            c = under;
                        }
                        else
                        {
                            c = fill;
                        }
                    }
                    else if (drop > 0)
                    {
                        // ⚠️ NO CAST SHADOW, AND THAT IS DELIBERATE RATHER THAN UNFINISHED. There
                        // is not one anywhere in the logo: the mark sits ON the page. The band
                        // under the face is the page showing through, and the under-bar is what
                        // gives the object its thickness instead.
                        c = Color.clear;
                    }
                    else
                    {
                        c = Color.clear;
                    }

                    pixels[(y * width) + x] = c;
                }
            }

            return WoodCraft.Finish(pixels, width, h, cap, false, key);
        }

        /// <summary>How far a brand button stands off its own under-bar.</summary>
        private const int BrandLift = 5;

        /// <summary>
        /// How far the stroke may wander, in units.
        /// ⚠️ SMALL ON PURPOSE. The mark's line varies by a few per cent of its own thickness, not
        /// by a visible amount: what makes it read as hand-drawn is that the variation is
        /// IRREGULAR, not that it is large. At 3 units on a 96-unit button it is under the eye's
        /// threshold for "wrong" and over its threshold for "drawn".
        /// </summary>
        private const int BrandWobble = 3;

        // -----------------------------------------------------------------------------------
        // TRAY: a thing you read out of, or type into.
        // -----------------------------------------------------------------------------------

        /// <summary>
        /// A slot cut into the sheet.
        ///
        /// ⚠️ NO HALO AND NO CAST SHADOW, AND THAT IS THE ENTIRE DISTINCTION FROM A TOKEN. A halo
        /// says an object is lying ON the surface and a shadow says it is above it; a recess has
        /// neither, because there is nothing outside it to catch the light. What says it is a
        /// recess is the shadow along its own TOP edge.
        ///
        /// ⚠️ AND `Pose.Hover` LIGHTENS IT RATHER THAN OUTLINING IT, because a tray is used for
        /// list rows as well as fields and a row that grows an outline on hover makes a list of
        /// eight flicker as the pointer crosses it.
        /// </summary>
        private static Sprite PaintTray(int h, Pose pose, bool tall, string key)
        {
            int cap = TrayCorner + 4;
            int width = (cap * 2) + 4;
            var pixels = new Color[width * h];

            Color fill = pose == Pose.Hover || pose == Pose.Press
                ? Color.Lerp(UiTheme.PaperWarm, UiTheme.Paper, 0.7f)
                : pose == Pose.Off ? Color.Lerp(UiTheme.PaperWarm, UiTheme.PaperEdge, 0.75f)
                : UiTheme.PaperWarm;

            // ⚠️⚠️ THE RECESS IS FOUR THINGS NOW AND IT WAS ONE, BECAUSE 🧑 SAID SO WITH A CROP
            // OF THE FIGHTER ROWS: **"this 2nd pic ugly too its still 2d"**. A single dark band
            // along the top edge is not a hole, it is a gradient: what makes a real recess read is
            // that light falls INTO it from one direction and out of it on the other side. So a
            // tray now carries
            //
            //   * a hard inner shadow along the top, three quarters strength at the very edge,
            //   * a WRAP of that shadow one unit down the left and right walls, which is what says
            //     the hole has sides rather than a lid,
            //   * a lit bottom lip in `Paper`, the light that got in bouncing off the floor, and
            //   * a one-unit dark hairline right at the silhouette, so the cut edge is crisp
            //     against whatever sheet it is cut into.
            //
            // Four cheap gradients beat one expensive one: this is `WoodCraft`'s own finding about
            // the varnish band, arriving on the opposite material.
            int shadow = Mathf.Max(4, Mathf.RoundToInt(h * 0.14f));

            for (int y = 0; y < h; y++)
            {
                int downFromTop = (h - 1) - y;

                for (int x = 0; x < width; x++)
                {
                    float depth = Depth(x, y, width, h, TrayCorner);

                    Color c;

                    if (depth <= 0.0f) c = Color.clear;
                    else
                    {
                        c = Fibre(fill, x, y);

                        if (!tall && downFromTop < shadow)
                        {
                            float t = 1.0f - (downFromTop / (float)shadow);
                            c = Color.Lerp(c, UiTheme.PaperSunk, t * t * 0.9f);
                        }

                        // The side walls: a narrower version of the same shadow, so the recess has
                        // depth in both axes rather than only downward.
                        if (!tall && depth < 3.0f)
                            c = Color.Lerp(c, UiTheme.PaperSunk, (3.0f - depth) / 3.0f * 0.55f);

                        // The floor catching the light that got in.
                        if (!tall && y < 3)
                            c = Color.Lerp(c, UiTheme.Paper, (3 - y) / 3.0f * 0.6f);

                        // The cut edge itself.
                        if (depth <= 1.4f)
                            c = Color.Lerp(c, UiTheme.PaperEdge, 0.7f);
                    }

                    pixels[(y * width) + x] = c;
                }
            }

            return WoodCraft.Finish(pixels, width, h, cap, tall, key);
        }

        // -----------------------------------------------------------------------------------
        // GHOST: a slot with nobody in it.
        // -----------------------------------------------------------------------------------

        /// <summary>
        /// The shape of an absence.
        ///
        /// ⚠️⚠️ IT EXISTS BECAUSE `docs/TODO.md` § 118.1 ROW 3 IS A CONTENT PROBLEM WITH NO
        /// SURFACE TO SOLVE IT ON. The lobby's three other seats were three identical filled plates
        /// reading `BOT`, and a player who has never played this game cannot tell whether that
        /// means "a bot is sitting here" or "this seat is free". Among Us solves it by making an
        /// empty seat visibly empty (§ 118.3), and an empty seat cannot be drawn with a filled
        /// surface however it is coloured. **It needs a silhouette that is mostly nothing.**
        ///
        /// ⚠️ TWO HAIRLINES WITH A GAP RATHER THAN A DASHED OUTLINE. A dash pattern is destroyed by
        /// a nine-slice: the middle of the sprite is what stretches, so the dashes on a 300-unit
        /// plate would be four times the length of the ones on a 90-unit plate and the corners
        /// would not match either. A double rule survives any width and reads as "outline only" at
        /// a glance in exactly the same way.
        /// </summary>
        private static Sprite PaintGhost(int h, bool tall, string key)
        {
            int cap = SoftCorner + 8;
            int width = (cap * 2) + 4;
            var pixels = new Color[width * h];

            var fill = new Color(UiTheme.Paper.r, UiTheme.Paper.g, UiTheme.Paper.b, 0.16f);
            var rule = new Color(UiTheme.PaperSunk.r, UiTheme.PaperSunk.g, UiTheme.PaperSunk.b,
                                 0.95f);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < width; x++)
                {
                    float depth = Depth(x, y, width, h, SoftCorner);

                    Color c;

                    if (depth <= 0.0f) c = Color.clear;
                    else if (depth <= 2.6f) c = Fade(rule, depth);
                    else if (depth <= 5.0f) c = fill;
                    else if (depth <= 6.6f) c = Fade(rule, depth - 5.0f);
                    else c = fill;

                    pixels[(y * width) + x] = c;
                }

            return WoodCraft.Finish(pixels, width, h, cap, tall, key);
        }

        // -----------------------------------------------------------------------------------
        // Shared
        // -----------------------------------------------------------------------------------

        /// <summary>
        /// How far inside a rounded rect a pixel is, where the rect is `width` by `height` and its
        /// bottom edge is at y = 0 of the caller's own frame.
        ///
        /// ⚠️ IT TAKES THE RECT RATHER THAN READING THE SPRITE, WHICH IS WHAT MAKES THE CAST
        /// SHADOW ONE FUNCTION INSTEAD OF TWO. The face and its shadow are the same shape at two
        /// different origins, so both are this call with a different `y`.
        /// </summary>
        private static float Depth(int x, int y, int width, int height, float corner)
            => Depth(x, y, width, height, corner, false);

        /// <summary>
        /// ⚠️ THE `chamfer` OVERLOAD EXISTS FOR <see cref="Surface.Action"/> AND FOR NOTHING ELSE.
        /// Every other paper surface is a round, because a cut-out has one pair of scissors; the
        /// primary is the one control in this front end drawn in 🧑's own chamfer, and he asked
        /// for that twice after seeing it as a pill: **"i kinda preferred the sharper edges on
        /// this"**, *"i prefer the old sharper edges on it"*.
        /// </summary>
        private static float Depth(int x, int y, int width, int height, float corner, bool chamfer)
        {
            if (y < 0 || y >= height) return -1.0f;

            float midX = (width - 1) * 0.5f;
            float midY = (height - 1) * 0.5f;

            float dx = (midX + 0.5f) - Mathf.Abs(x - midX);
            float dy = (midY + 0.5f) - Mathf.Abs(y - midY);

            return WoodCraft.Depth(dx, dy, corner, chamfer, false);
        }

        /// <summary>
        /// ⚠️ 1.2 PER CENT, AND IT IS LOWER THAN WOOD'S GRAIN FOR A REASON THAT IS ABOUT THE EYE
        /// RATHER THAN ABOUT PAPER. Noise is far more visible on a light surface than on a dark
        /// one; `WoodCraft`'s own paper path settled at 1.5 per cent and these sheets are larger
        /// than any plate that file draws, so the same amplitude spread over a 900-unit rail reads
        /// as dirt. It is here at all because a completely flat 900-unit cream rectangle reads as a
        /// UI panel rather than as a piece of card.
        /// </summary>
        private static Color Fibre(Color c, int x, int y)
        {
            float n = Mathf.PerlinNoise(x * 1.7f, y * 1.7f) - 0.5f;
            return WoodCraft.Lift(c, n * 0.012f);
        }

        /// <summary>Anti-aliases a colour against the edge of the silhouette.</summary>
        private static Color Fade(Color c, float depth)
        {
            var faded = c;
            faded.a *= Mathf.Clamp01(depth);
            return faded;
        }
    }

    /// <summary>
    /// Puts a <see cref="PaperCraft"/> surface on an Image and keeps it at the right height.
    ///
    /// ⚠️⚠️ IT IS `WoodSkin` FOR PAPER AND IT EXISTS FOR THE IDENTICAL REASON: the sprite is only
    /// correct at the height it was built for. A `Token` is a pill, so its corner radius IS half
    /// its face height; every raised surface carries a lip and a cast shadow measured from its own
    /// bottom edge. Every rect in this front end is driven by a layout group or an aspect-scaled
    /// canvas, so no caller can know its own height at the moment it builds itself. Watching the
    /// rect is the only version of this that cannot go stale.
    ///
    /// ⚠️ IT REBUILDS ON A 2-UNIT CHANGE, NOT EVERY FRAME. `PaperCraft.Slab` quantises to 4 units
    /// and caches, so a settled layout costs one float compare per frame.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Image))]
    public sealed class PaperSkin : MonoBehaviour
    {
        public PaperCraft.Surface Surface = PaperCraft.Surface.Sheet;

        /// <summary>
        /// Which of the two authored fills a <see cref="PaperCraft.Surface.Action"/> takes.
        ///
        /// ⚠️ IGNORED BY EVERY OTHER SURFACE, and deliberately not a `Color`. See
        /// `PaperCraft.Accent`: a closed list of two colours he authored is not the open `fill`
        /// parameter `CLAUDE.md` 6.5 forbids, and making it a colour field here is precisely how
        /// it would become one.
        /// </summary>
        public PaperCraft.Accent Accent = PaperCraft.Accent.Green;

        private Image _image;
        private float _built = -1.0f;
        private PaperCraft.Surface _builtSurface;
        private PaperCraft.Pose _pose = PaperCraft.Pose.Rest;

        /// <summary>
        /// The pose the sprite currently on the Image was baked for.
        ///
        /// ⚠️⚠️ IT IS PART OF THE CACHE KEY NOW AND LEAVING IT OUT IS HALF OF THE STUCK-HOVER
        /// BUG. 🧑 2026-09-02, with a crop of the lobby's mode tabs: **"theres brown ink left over
        /// if i dont hover back to the buttons on top"**, *"make it so that i dont have to hover
        /// back to buttons on top to get rid of it"*.
        ///
        /// `SetPose` clears `_built` to force a repaint, and `Rebuild` **returns without painting
        /// and without recording anything when the rect reports zero height** — which is every
        /// frame the control is inactive, and a drawer closing is exactly that. The pose write was
        /// therefore dropped on the floor, and the next `OnEnable` repainted with `_built = -1`
        /// against a `_pose` field still holding `Hover`. **The plate came back lit, on a control
        /// nothing was pointing at, and only a fresh enter-and-exit could clear it.**
        ///
        /// ⚠️ THE SURFACE WAS ALREADY IN THE KEY AND THE POSE WAS NOT, which is why this looked
        /// like a colour bug rather than a state bug: `MarkLive` swapping `Live` for `Ghost` DID
        /// repaint, so the tab row's selection was always right and only its lighting was stale.
        /// </summary>
        private PaperCraft.Pose _builtPose = PaperCraft.Pose.Rest;

        /// <summary>The accent the sprite on the Image was baked for. ⚠️ In the key for the same
        /// reason the pose is: a caller that swaps it must not need to know to invalidate.
        /// </summary>
        private PaperCraft.Accent _builtAccent = PaperCraft.Accent.Green;

        /// <summary>The sprite this skin last wrote, so it can tell when somebody else has
        /// overwritten it. See the note in <see cref="Rebuild"/>.</summary>
        private Sprite _wrote;

        public static PaperSkin Apply(GameObject target, PaperCraft.Surface surface)
        {
            if (target == null) return null;

            var image = target.GetComponent<Image>();
            if (image == null) image = target.AddComponent<Image>();

            var skin = target.GetComponent<PaperSkin>();
            if (skin == null) skin = target.AddComponent<PaperSkin>();

            // ⚠️ A TARGET THAT ALREADY CARRIES A WOOD SKIN IS THE SINGLE MOST LIKELY WAY A PIECE
            // OF THE OLD FRONT END SURVIVES THIS PASS. 🧑, on the overhaul: *"MAKE SURE U
            // COMPLETELY REPLACE UI BCZ I DOTN WANT LEFTOVER SHIT FROM OLD UI TO STILL BE FRIGGING
            // WITH US"*. `WoodSkin` writes the sprite from `Update`, so a node with both would
            // flicker between two materials every frame and look like a rendering bug rather than
            // like a missed call site.
            var wood = target.GetComponent<WoodSkin>();
            if (wood != null) Object.DestroyImmediate(wood);

            skin.Surface = surface;
            skin._image = image;
            skin._built = -1.0f;
            skin.Rebuild();

            return skin;
        }

        /// <summary>Sets the pointer state. ⚠️ Called by `PaperButton`, not by layout code.
        /// </summary>
        public void SetPose(PaperCraft.Pose pose)
        {
            if (_pose == pose) return;
            _pose = pose;
            _built = -1.0f;
            Rebuild();
        }

        private void OnEnable()
        {
            _built = -1.0f;
            Rebuild();
        }

        private void OnRectTransformDimensionsChange() => Rebuild();

        private void Update() => Rebuild();

        public void Rebuild()
        {
            if (_image == null) _image = GetComponent<Image>();
            if (_image == null) return;

            float height = _image.rectTransform.rect.height;

            // ⚠️ A RECT THAT HAS NOT BEEN LAID OUT YET REPORTS 0, and baking against that pins the
            // control to the 20-unit floor forever. See `WoodSkin.Rebuild`.
            if (height <= 1.0f) return;

            // ⚠️⚠️ THE SPRITE ITSELF IS PART OF THE TEST NOW, BECAUSE SOMETHING ELSE WRITING IT IS
            // A FAULT THIS REPOSITORY HAS SHIPPED TWICE. `docs/TODO.md` § 120.5 row 1:
            // `ConvertedCharacterSelect.RefreshTabs` ran AFTER `PaperDress.Screen` and wrote a
            // `GodotTheme.Box` straight onto the Image, and this method's cache said the height
            // and the surface had not changed, **so the paper sprite was never put back and the
            // picker's tab bar was an amber nine-patch for a whole pass.** Any caller that reaches
            // for `Image.sprite` on a dressed node does the same thing, and there is no way to
            // stop them from here.
            //
            // ⚠️ IT IS A REFERENCE COMPARE, NOT A REBUILD. `PaperCraft.Slab` caches by key, so the
            // sprite this skin wants is the same object every frame; noticing that the Image is
            // holding a different one costs one pointer compare and repairs the frame after the
            // overwrite instead of the pass after the render.
            if (_built > 0.0f && Mathf.Abs(height - _built) < 2.0f
                && _builtSurface == Surface && _builtPose == _pose && _builtAccent == Accent
                && ReferenceEquals(_image.sprite, _wrote))
                return;

            _built = height;
            _builtSurface = Surface;
            _builtPose = _pose;
            _builtAccent = Accent;

            _image.sprite = PaperCraft.Slab(Surface, height, _pose, Accent);
            _wrote = _image.sprite;
            _image.type = Image.Type.Sliced;
            _image.color = Color.white;

            // ⚠️ WITHOUT THIS THE SLICE IS SCALED BY THE SPRITE'S PIXELS-PER-UNIT AND THE CAPS
            // ARRIVE AT THE WRONG SIZE. Every sliced sprite in this project sets it.
            _image.pixelsPerUnitMultiplier = 1.0f;
        }
    }
}
