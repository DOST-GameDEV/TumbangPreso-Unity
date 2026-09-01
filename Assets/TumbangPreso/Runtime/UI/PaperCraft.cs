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
    /// pennants, `BUTTON LONG`, `JOIN BUTTON`, the arrows, `TUMP.png` and the key art are all
    /// still drawn from the PNGs, untinted.
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

        /// <summary>The cast shadow. ⚠️ WARM, because `CLAUDE.md` § 6.4 bans cold grey in any
        /// layer and a neutral black at 30 per cent over cream composites as exactly that.
        /// </summary>
        private static readonly Color Shade = new Color(0.30f, 0.19f, 0.10f, 0.34f);

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
        public static Sprite Slab(Surface surface, float height, Pose pose = Pose.Rest)
        {
            bool tall = height > WoodCraft.TallSurface;
            int h = tall ? 96 : Mathf.Clamp(Mathf.RoundToInt(height / 4.0f) * 4, 20,
                                            WoodCraft.TallSurface);

            string key = $"pc_{surface}_{pose}_{h}_{tall}";
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            Sprite made;
            switch (surface)
            {
                case Surface.Token: made = PaintRaised(h, pose, tall, key, false); break;
                case Surface.Live: made = PaintRaised(h, pose, tall, key, true); break;
                case Surface.Tray: made = PaintTray(h, pose, tall, key); break;
                case Surface.Ghost: made = PaintGhost(h, tall, key); break;
                case Surface.Sign: made = PaintPlate(h, tall, key, true); break;
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

            Color fill = sign ? UiTheme.WoodMid : UiTheme.Paper;
            Color ring = sign ? UiTheme.WoodDeep : UiTheme.PaperEdge;

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
                            c = Color.Lerp(c, sign ? UiTheme.WoodEdge : Color.white,
                                           sign ? 0.85f : 0.55f);
                    }
                    else if (shadowDepth > 0.0f && !tall)
                    {
                        c = Fade(Shade, shadowDepth);
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
            int wall = Mathf.Max(3, Mathf.RoundToInt(face * 0.14f));

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
                            float alongFace = face <= 1 ? 0.0f : upFromFace / (float)(face - 1);
                            c = Fibre(WoodCraft.Lift(fill, (alongFace - 0.5f) * -0.04f), x, y);

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
        {
            if (y < 0 || y >= height) return -1.0f;

            float midX = (width - 1) * 0.5f;
            float midY = (height - 1) * 0.5f;

            float dx = (midX + 0.5f) - Mathf.Abs(x - midX);
            float dy = (midY + 0.5f) - Mathf.Abs(y - midY);

            return WoodCraft.Depth(dx, dy, corner, false, false);
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

        private Image _image;
        private float _built = -1.0f;
        private PaperCraft.Surface _builtSurface;
        private PaperCraft.Pose _pose = PaperCraft.Pose.Rest;

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
            if (_built > 0.0f && Mathf.Abs(height - _built) < 2.0f
                && _builtSurface == Surface) return;

            _built = height;
            _builtSurface = Surface;

            _image.sprite = PaperCraft.Slab(Surface, height, _pose);
            _image.type = Image.Type.Sliced;
            _image.color = Color.white;

            // ⚠️ WITHOUT THIS THE SLICE IS SCALED BY THE SPRITE'S PIXELS-PER-UNIT AND THE CAPS
            // ARRIVE AT THE WRONG SIZE. Every sliced sprite in this project sets it.
            _image.pixelsPerUnitMultiplier = 1.0f;
        }
    }
}
