using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>
    /// A picture of the control, for every place the game currently draws its NAME.
    ///
    /// ⚠️⚠️ THIS CLOSES THE AUTHORED-GLYPH GAP `docs/TODO.md` § 126.9 AND § 125.13 BOTH LEAVE
    /// OPEN, AND THE GAP IS WORST EXACTLY WHERE IT MATTERS MOST. Every key prompt in the game
    /// goes through `Hud.KeyLabelFor`, which returns a STRING: `W`, `LEFT SHIFT`, `LMB`. That is
    /// correct and readable on a keyboard. **On a pad it returns `BUTTON WEST`**, which is not
    /// what is written on any controller ever made, and § 126.9 records the second half of the
    /// same fault: the hero picker's key chip is **26 units wide**, sized for `Q`, and
    /// `BUTTON WEST` in it is *"trading one overflow for a worse one"*.
    ///
    /// ⚠️ THE TEXT IS THE FALLBACK AND IT IS NEVER REMOVED. A control with no glyph draws
    /// exactly what it draws today, so this can only ever improve a prompt and can never blank
    /// one. `docs/VISION.md` § 3: *"a screen that teaches the wrong key is worse than one that
    /// teaches none"*, and a glyph that is missing is not a glyph that is wrong.
    ///
    /// ⚠️⚠️ IT IS KEYED ON THE LABEL `Hud` ALREADY RESOLVED, NOT ON A SECOND READ OF THE BINDING,
    /// AND THAT IS THE WHOLE REASON IT STAYS TRUE AFTER A REBIND. `Hud.KeyLabel` resolves the
    /// live binding, per device, cached on `Rebinding.Revision` and `LastInputDevice.Revision`,
    /// and it is already the single source of every key the game teaches (its own note says so:
    /// *"the deck, the inspect tray, the lata card, the get-up prompt and the training route all
    /// read it"*). A second resolver here would be a second thing to keep in step, and
    /// `docs/TODO.md` § 38.5's three dead protocols are what a second path costs. **Rebind a key
    /// and the glyph follows, because the string it is looked up by follows.**
    ///
    /// ⚠️⚠️ THE SHEETS ARE RECOLOURED ON THE WAY IN AND MUST NEVER BE USED AS BOUGHT.
    /// The pack is vryell's "Controllers and Keyboard", drawn in the author's `rosyandblue`
    /// ramp: `14182e`, `2b2b45`, `3a3f5e`, `404973`, `4c6885`, `686f99`, `a3a7c2`, `dfe0e8`.
    /// **Every one of those has more blue in it than red**, which is `CLAUDE.md` § 6.4's own
    /// test stated wide, and that section was written because navy keycaps were reported five
    /// separate times. `tools/build_input_glyphs.py` maps the ramp onto wood and cream and
    /// collapses the pad's face-button hues to amber; the sheets under
    /// `Resources/UI/input/` are the OUTPUT of that script and are not hand-edited.
    ///
    /// ⚠️ AND THE FACE BUTTONS LOSE THEIR HUE ON PURPOSE. Xbox A is green, B red, X blue, Y
    /// yellow, and two of those four are illegal in this front end. They are all amber now and
    /// **the LETTER is what names the button**, which is `docs/FUTURE.md` § 16.1's rule: a
    /// distinction carried by hue alone is a distinction some players do not have.
    /// </summary>
    public static class InputGlyphs
    {
        /// <summary>
        /// One cell of one sheet, 16 px square.
        ///
        /// ⚠️ THE SHEETS KEEP THE PACK'S OWN GRID AND ARE SLICED AT RUNTIME. A build step that
        /// re-packed the cells tighter would be a second place to keep in step with this file,
        /// and the sheets are 8 KB each: there is nothing to save.
        /// </summary>
        private readonly struct Cell
        {
            public readonly Sheet Sheet;
            public readonly int Column;
            public readonly int Row;

            public Cell(Sheet sheet, int column, int row)
            {
                Sheet = sheet;
                Column = column;
                Row = row;
            }
        }

        private enum Sheet { Key, Pad, Mouse, Stick }

        public const int CellPixels = 16;

        /// <summary>
        /// ⚠️⚠️ TWO VARIANTS PER CONTROL, CHOSEN BY WHAT IS BEHIND IT, AND ONE VARIANT WOULD BE
        /// INVISIBLE ON HALF THE SCREENS IN THIS GAME. The pack draws every cap twice: once bare
        /// and once inside a cream KEYLINE. The keyline is what separates a wooden cap from the
        /// asphalt behind the in-match HUD and the training card; on a CREAM PAPER screen the
        /// same keyline is cream on cream and the cap loses its edge, so there the bare cap's own
        /// dark rim is what does the separating. **This is `CLAUDE.md` § 6.2b's *"over the real
        /// background, never an empty scene"* as a parameter**: the sprite is not correct on its
        /// own, only against the thing it is drawn on.
        ///
        /// The keyboard sheet lays its four variants out as: 0 bare with a cream lip, 1 the same
        /// with a keyline, 2 bare with a warm lip, 3 that with a keyline. The pad sheet's are
        /// 0 bare, 3 keyline. Both offsets are per sheet and neither is guessable, which is why
        /// they are named here rather than written at the call sites.
        /// </summary>
        private const int KeyVariantOnDark = 1;
        private const int KeyVariantOnLight = 2;
        private const int PadVariantOnDark = 3;
        private const int PadVariantOnLight = 0;

        // -------------------------------------------------------------------
        // § THE TABLE
        //
        // ⚠️ THE KEYS ARE EXACTLY WHAT `Hud.KeyLabel` RETURNS, UPPERCASE. `SingleKeyLabel` runs
        // `InputControlPath.ToHumanReadableString(..., OmitDevice)` and then `ToUpperInvariant`,
        // with three hand-written abbreviations (LMB, RMB, MMB) on top. So `<Keyboard>/leftShift`
        // arrives here as `LEFT SHIFT` and `<Gamepad>/buttonWest` as `BUTTON WEST`.
        // A label with no row falls through to text, which is what ships today.
        // -------------------------------------------------------------------

        /// <summary>
        /// The keyboard sheet is four groups of four variant columns, 26 rows deep.
        ///
        /// ⚠️ THE GROUP IS THE FIRST COLUMN OF ITS FOUR AND THE VARIANT IS ADDED TO IT. Reading
        /// the sheet as 16 independent columns is the mistake that puts an `A` where an `ESC`
        /// belongs, because the groups are not the same length: letters fill all 26 rows,
        /// function keys and digits fill 22, and the two symbol groups fill 26 and 16.
        /// </summary>
        private const int KeyGroupLetters = 0;
        private const int KeyGroupFunctionAndDigits = 4;
        private const int KeyGroupSymbols = 8;
        private const int KeyGroupNavigation = 12;

        private static readonly Dictionary<string, Cell> Table = BuildTable();

        private static Dictionary<string, Cell> BuildTable()
        {
            var table = new Dictionary<string, Cell>(160);

            // A to Z, one row each, in order.
            for (int i = 0; i < 26; i++)
                table[((char)('A' + i)).ToString()] = new Cell(Sheet.Key, KeyGroupLetters, i);

            // F1 to F12, then the digit row 1 2 3 4 5 6 7 8 9 0.
            for (int i = 0; i < 12; i++)
                table["F" + (i + 1)] = new Cell(Sheet.Key, KeyGroupFunctionAndDigits, i);

            for (int i = 1; i <= 9; i++)
                table[i.ToString()] = new Cell(Sheet.Key, KeyGroupFunctionAndDigits, 11 + i);

            table["0"] = new Cell(Sheet.Key, KeyGroupFunctionAndDigits, 21);

            // ⚠️ THE MODIFIERS ARE LISTED WITH AND WITHOUT A SIDE. `ToHumanReadableString` returns
            // `Left Shift` for `<Keyboard>/leftShift` and plain `Shift` for the any-shift control,
            // and a table holding only one of the two silently drops the prompt for the other.
            table["ESCAPE"] = new Cell(Sheet.Key, KeyGroupSymbols, 0);
            table["ESC"] = new Cell(Sheet.Key, KeyGroupSymbols, 0);
            table["TAB"] = new Cell(Sheet.Key, KeyGroupSymbols, 2);
            table["CAPS LOCK"] = new Cell(Sheet.Key, KeyGroupSymbols, 4);
            table["SHIFT"] = new Cell(Sheet.Key, KeyGroupSymbols, 5);
            table["LEFT SHIFT"] = new Cell(Sheet.Key, KeyGroupSymbols, 5);
            table["RIGHT SHIFT"] = new Cell(Sheet.Key, KeyGroupSymbols, 5);
            table["CTRL"] = new Cell(Sheet.Key, KeyGroupSymbols, 6);
            table["LEFT CTRL"] = new Cell(Sheet.Key, KeyGroupSymbols, 6);
            table["RIGHT CTRL"] = new Cell(Sheet.Key, KeyGroupSymbols, 6);
            table["LEFT CONTROL"] = new Cell(Sheet.Key, KeyGroupSymbols, 6);
            table["ALT"] = new Cell(Sheet.Key, KeyGroupSymbols, 7);
            table["LEFT ALT"] = new Cell(Sheet.Key, KeyGroupSymbols, 7);
            table["RIGHT ALT"] = new Cell(Sheet.Key, KeyGroupSymbols, 7);
            table["SPACE"] = new Cell(Sheet.Key, KeyGroupSymbols, 8);
            table["SPACEBAR"] = new Cell(Sheet.Key, KeyGroupSymbols, 8);
            table["]"] = new Cell(Sheet.Key, KeyGroupSymbols, 9);
            table["RIGHT BRACKET"] = new Cell(Sheet.Key, KeyGroupSymbols, 9);
            table["["] = new Cell(Sheet.Key, KeyGroupSymbols, 10);
            table["LEFT BRACKET"] = new Cell(Sheet.Key, KeyGroupSymbols, 10);
            table["BACKSPACE"] = new Cell(Sheet.Key, KeyGroupSymbols, 23);
            table["ENTER"] = new Cell(Sheet.Key, KeyGroupSymbols, 25);
            table["RETURN"] = new Cell(Sheet.Key, KeyGroupSymbols, 25);

            table["-"] = new Cell(Sheet.Key, KeyGroupNavigation, 0);
            table["MINUS"] = new Cell(Sheet.Key, KeyGroupNavigation, 0);
            table["="] = new Cell(Sheet.Key, KeyGroupNavigation, 1);
            table["EQUALS"] = new Cell(Sheet.Key, KeyGroupNavigation, 1);
            table["INSERT"] = new Cell(Sheet.Key, KeyGroupNavigation, 6);
            table["DELETE"] = new Cell(Sheet.Key, KeyGroupNavigation, 7);
            table["HOME"] = new Cell(Sheet.Key, KeyGroupNavigation, 8);
            table["END"] = new Cell(Sheet.Key, KeyGroupNavigation, 9);
            table["PAGE UP"] = new Cell(Sheet.Key, KeyGroupNavigation, 10);
            table["PAGE DOWN"] = new Cell(Sheet.Key, KeyGroupNavigation, 11);
            table["UP ARROW"] = new Cell(Sheet.Key, KeyGroupNavigation, 12);
            table["LEFT ARROW"] = new Cell(Sheet.Key, KeyGroupNavigation, 13);
            table["DOWN ARROW"] = new Cell(Sheet.Key, KeyGroupNavigation, 14);
            table["RIGHT ARROW"] = new Cell(Sheet.Key, KeyGroupNavigation, 15);

            // ⚠️ THE THREE MOUSE ABBREVIATIONS ARE `Hud.SingleKeyLabel`'S OWN, hand-written there
            // because `Left Button` on a card reads as a keyboard key. The sheet's cap rows are
            // laid out the same way the keycaps are, so LMB and RMB sit beside W and A without
            // looking like a different set of art.
            table["LMB"] = new Cell(Sheet.Mouse, 1, 4);
            table["RMB"] = new Cell(Sheet.Mouse, 1, 6);
            table["MMB"] = new Cell(Sheet.Mouse, 0, 8);

            // ⚠️⚠️ THE PAD ROWS ARE THE **DARK** ROW OF EACH PAIR, WHICH IS THE ONE WITH THE
            // AMBER LETTER ON IT. The sheet alternates dark cap / light cap down the whole face
            // block (Y, Y, X, X, A, A, B, B), and the light rows carry a DIM letter meant for a
            // pressed state. Picking the wrong one of a pair is invisible in the source and
            // reads on screen as a greyed-out prompt.
            table["BUTTON NORTH"] = new Cell(Sheet.Pad, 0, 0);   // Y
            table["BUTTON WEST"] = new Cell(Sheet.Pad, 0, 2);    // X
            table["BUTTON SOUTH"] = new Cell(Sheet.Pad, 0, 4);   // A
            table["BUTTON EAST"] = new Cell(Sheet.Pad, 0, 6);    // B

            // ⚠️ THE D-PAD'S FOUR DIRECTIONS ARE FOUR DIFFERENT CELLS AND SHARING ONE CROSS WOULD
            // BE WORSE THAN THE TEXT IT REPLACES. Four actions bind to the four directions
            // (`EmoteWheel` up, `CleanFeed` down, `CurveLeft` left, `CurveRight` right), so one
            // generic cross would teach a player that all four are the same button.
            table["D-PAD"] = new Cell(Sheet.Pad, 0, 8);
            table["D-PAD/UP"] = new Cell(Sheet.Pad, 0, 8);
            table["D-PAD/LEFT"] = new Cell(Sheet.Pad, 0, 8);
            table["D-PAD/DOWN"] = new Cell(Sheet.Pad, 0, 8);
            table["D-PAD/RIGHT"] = new Cell(Sheet.Pad, 0, 8);

            table["LEFT SHOULDER"] = new Cell(Sheet.Pad, 0, 16);
            table["RIGHT SHOULDER"] = new Cell(Sheet.Pad, 0, 22);
            table["LEFT TRIGGER"] = new Cell(Sheet.Pad, 0, 19);
            table["RIGHT TRIGGER"] = new Cell(Sheet.Pad, 0, 25);
            table["START"] = new Cell(Sheet.Pad, 0, 11);
            table["SELECT"] = new Cell(Sheet.Pad, 0, 13);

            // ⚠️ THE STICKS ARE THEIR OWN SHEET AND HAVE NO KEYLINE VARIANT, so both grounds get
            // the same cell. A stick is a silhouette rather than a cap: it already reads against
            // asphalt and against paper, which is why the pack draws it once.
            table["LEFT STICK"] = new Cell(Sheet.Stick, 0, 3);
            table["RIGHT STICK"] = new Cell(Sheet.Stick, 0, 7);
            table["LEFT STICK PRESS"] = new Cell(Sheet.Stick, 3, 10);
            table["RIGHT STICK PRESS"] = new Cell(Sheet.Stick, 2, 10);

            return table;
        }

        /// <summary>
        /// ⚠️⚠️ THE FOUR D-PAD DIRECTIONS ARE RESOLVED BY A COLUMN OFFSET RATHER THAN BY A ROW,
        /// AND THE OFFSET IS DIFFERENT ON THE TWO GROUNDS. The sheet's row 8 is
        /// `[bare, all-lit, up, left, down, right]` and row 9 is
        /// `[all-outlined, up, left, down, right]`: **the bare row carries an extra "all lit"
        /// column that the outlined row does not**, so the same direction is at column 2 on one
        /// and column 1 on the other. Sharing one number here would put LEFT where UP belongs on
        /// exactly one of the two backgrounds, which is the kind of fault that ships.
        /// </summary>
        private static int DPadColumn(string label, bool onDark)
        {
            int first = onDark ? 1 : 2;

            switch (label)
            {
                case "D-PAD/UP": return first;
                case "D-PAD/LEFT": return first + 1;
                case "D-PAD/DOWN": return first + 2;
                case "D-PAD/RIGHT": return first + 3;
                default: return onDark ? 0 : 0;
            }
        }

        private static int DPadRow(bool onDark) => onDark ? 9 : 8;

        // -------------------------------------------------------------------
        // § THE SHEETS
        // -------------------------------------------------------------------

        private static readonly Dictionary<Sheet, Texture2D> Textures = new Dictionary<Sheet, Texture2D>();
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        private static string PathFor(Sheet sheet)
        {
            switch (sheet)
            {
                case Sheet.Pad: return "UI/input/glyphs_pad_v1";
                case Sheet.Mouse: return "UI/input/glyphs_mouse_v1";
                case Sheet.Stick: return "UI/input/glyphs_stick_v1";
                default: return "UI/input/glyphs_key_v1";
            }
        }

        /// <summary>
        /// ⚠️ A MISSING SHEET IS A NULL AND NEVER AN EXCEPTION. These four textures are the only
        /// thing in this system that can be absent, and they are absent in exactly one situation
        /// worth designing for: somebody has cloned the repository without them, because
        /// `Attention.md` § 7.1 leaves whether they may be committed as 🧑's call. **The prompts
        /// then draw their text, which is what they draw today**, and nothing logs per frame.
        /// </summary>
        private static Texture2D TextureFor(Sheet sheet)
        {
            if (Textures.TryGetValue(sheet, out var cached)) return cached;

            var loaded = Resources.Load<Texture2D>(PathFor(sheet));
            Textures[sheet] = loaded;
            return loaded;
        }

        /// <summary>
        /// The picture of a control, or null when there is not one.
        ///
        /// ⚠️ `onDark` IS THE GROUND, NOT A STYLE. Pass true when the glyph is drawn over the
        /// street, the in-match HUD or the training card; false on a cream paper screen. See
        /// <see cref="KeyVariantOnDark"/> for why one sprite cannot serve both.
        /// </summary>
        public static Sprite For(string label, bool onDark)
        {
            if (string.IsNullOrEmpty(label)) return null;

            string key = label.Trim().ToUpperInvariant();
            if (!Table.TryGetValue(key, out var cell)) return null;

            int column = cell.Column;
            int row = cell.Row;

            switch (cell.Sheet)
            {
                case Sheet.Key:
                    column += onDark ? KeyVariantOnDark : KeyVariantOnLight;
                    break;

                case Sheet.Pad:
                    if (row == 8)
                    {
                        column = DPadColumn(key, onDark);
                        row = DPadRow(onDark);
                    }
                    else
                    {
                        column += onDark ? PadVariantOnDark : PadVariantOnLight;
                    }
                    break;

                case Sheet.Mouse:
                    // The cap rows come in bare/keyline pairs one row apart, like the keyboard's.
                    if (onDark) row += 1;
                    break;
            }

            string id = cell.Sheet + ":" + column + ":" + row;
            if (Sprites.TryGetValue(id, out var made)) return made;

            var texture = TextureFor(cell.Sheet);
            if (texture == null)
            {
                Sprites[id] = null;
                return null;
            }

            int x = column * CellPixels;

            // ⚠️⚠️ THE Y FLIP IS NOT OPTIONAL AND IT IS THE ONE THING THAT LOOKS RIGHT WHEN IT IS
            // WRONG. A sprite sheet is READ top-down (row 0 is the top row of the picture) and a
            // `Texture2D` is ADDRESSED bottom-up. Without the flip every lookup returns a cell
            // the same distance from the WRONG end of the sheet, so `A` comes back as `Z` and
            // every glyph in the game is a plausible-looking wrong one.
            int y = texture.height - ((row + 1) * CellPixels);

            if (x < 0 || y < 0 || x + CellPixels > texture.width || y + CellPixels > texture.height)
            {
                Sprites[id] = null;
                return null;
            }

            var sprite = Sprite.Create(texture,
                                       new Rect(x, y, CellPixels, CellPixels),
                                       new Vector2(0.5f, 0.5f),
                                       CellPixels);
            sprite.name = "glyph_" + id;

            Sprites[id] = sprite;
            return sprite;
        }

        /// <summary>
        /// Whether a label has a picture at all, without building one.
        /// ⚠️ FOR TESTS AND FOR CALLERS THAT SIZE A BOX BEFORE THEY FILL IT. A caller that asked
        /// `For(...)` twice would build the sprite, throw it away and build it again.
        /// </summary>
        public static bool Has(string label)
            => !string.IsNullOrEmpty(label) && Table.ContainsKey(label.Trim().ToUpperInvariant());

        /// <summary>
        /// Every label this table can draw, for the test that asserts the tutorial's prompts are
        /// covered.
        /// </summary>
        public static IEnumerable<string> KnownLabels() => Table.Keys;
    }
}
