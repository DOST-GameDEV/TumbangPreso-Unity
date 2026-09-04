using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

        private enum Sheet { Key, Mouse }

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

            // ⚠️⚠️ THE PAD ROWS USED TO BE HERE AND ARE NOW A SHEET OF THEIR OWN. Every gamepad
            // control resolves through `PadColumns` below, off Kenney's PS4 prompts, and this
            // table is the KEYBOARD and MOUSE only. 🧑 2026-09-04: *"change the control icons to
            // these"*, with a sheet of PlayStation glyphs, then *"it should be the ps4 icons"*.
            //
            // ⚠️ WHAT WENT WITH THEM, AND IT IS ALL COMPLEXITY THE BOUGHT SHEET'S LAYOUT FORCED:
            // the dark/light row pairing, the `PadVariantOnDark` / `PadVariantOnLight` column
            // offsets, and `DPadColumn` / `DPadRow`, whose own note had to explain that *"the same
            // direction is at column 2 on one and column 1 on the other"* because the bare row
            // carried an extra all-lit cell the outlined row did not. The new sheet is generated
            // by `tools/build_pad_prompt_icons.py`, so its grid is a plain
            // `column = control, row = ground` and needs none of it.

            return table;
        }

        // -------------------------------------------------------------------
        // § THE PAD SHEET
        //
        // ⚠️⚠️ EVERY GAMEPAD CONTROL COMES FROM HERE AND NOT FROM `Table`, AND IT IS A SEPARATE
        // PATH BECAUSE IT IS A SEPARATE PACK WITH A DIFFERENT GRID. `tools/build_pad_prompt_icons.py`
        // recolours Kenney's PS4 prompts (CC0) into a sheet whose layout this game chose rather
        // than inherited: one COLUMN per control, one ROW per ground, 64 px cells. That is why
        // there is no variant arithmetic on this side, where the keyboard sheet still needs it.
        //
        // ⚠️⚠️ THE COLUMN ORDER IS READ FROM A MANIFEST THE GENERATOR EMITS, NOT TYPED HERE. The
        // sheet and the index come out of one pass, so a control added or moved cannot leave this
        // file pointing at the wrong cell — the same arrangement `InputLayer.PadDiagram` uses for
        // the controller map's anchors, and for the same reason.
        // -------------------------------------------------------------------

        private const string PadSheetPath = "UI/input/glyphs_pad_v2";
        private const string PadIndexPath = "UI/input/glyphs_pad_v2_index";
        private const int PadCellPixels = 64;

        private static Dictionary<string, int> _padColumns;

        private static Dictionary<string, int> PadColumns
        {
            get
            {
                if (_padColumns != null) return _padColumns;

                _padColumns = new Dictionary<string, int>(24);

                var manifest = Resources.Load<TextAsset>(PadIndexPath);
                if (manifest == null) return _padColumns;

                foreach (string raw in manifest.text.Split('\n'))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line[0] == '#') continue;

                    // ⚠️ SPLIT ON `|`, NOT ON A SPACE. Half these labels contain one: `BUTTON
                    // NORTH`, `LEFT STICK PRESS`. A space-separated manifest would key the table
                    // on `BUTTON` and lose every multi-word control, which is most of them.
                    int bar = line.IndexOf('|');
                    if (bar <= 0) continue;

                    if (int.TryParse(line.Substring(bar + 1), out int column))
                        _padColumns[line.Substring(0, bar)] = column;
                }

                return _padColumns;
            }
        }

        /// <summary>Which family of button names the pad in the player's hands uses.</summary>
        public enum PadFamily
        {
            PlayStation,
            Xbox,
        }

        /// <summary>
        /// The family to draw, from the pad that is actually attached.
        ///
        /// ⚠️⚠️ A PLAYER ON AN XBOX PAD WAS BEING SHOWN A CROSS AND A TRIANGLE, WHICH IS THE SAME
        /// FAULT THE PLAYSTATION SHEET WAS ADDED TO FIX, POINTING THE OTHER WAY. `docs/VISION.md`
        /// § 3: a screen that teaches the wrong control is worse than one that teaches none. The
        /// Input System already knows the answer, so nothing here has to guess.
        ///
        /// ⚠️ XBOX IS THE DEFAULT AND PLAYSTATION IS THE SPECIAL CASE, WHICH IS THE RIGHT WAY
        /// ROUND FOR THIS GAME. `DualShockGamepad` covers the DualShock 3, 4 and the DualSense;
        /// everything else Unity matches, plus **every pad `InputLayer.GenericPadBridge` stands in
        /// for**, presents as an XInput-shaped device. Guessing PlayStation would put a cross on
        /// the no-name pads that bridge, which are the ones least likely to be a DualShock.
        /// </summary>
        public static PadFamily FamilyOf(Gamepad pad)
            => pad is UnityEngine.InputSystem.DualShock.DualShockGamepad
                ? PadFamily.PlayStation
                : PadFamily.Xbox;

        /// <summary>
        /// ⚠️ NO PAD MEANS PLAYSTATION, AND THAT IS A DECISION ABOUT ONE SCREEN. The only surface
        /// that draws pad glyphs with nothing plugged in is `InputLayer.ControllerMapScreen`, whose
        /// drawing IS a DualShock 4: a triangle beside a picture of a triangle is coherent, and an
        /// Xbox `Y` beside it is the two-vocabularies fault this whole pass is about. Everywhere
        /// else, no pad means no pad prompt is being drawn at all.
        /// </summary>
        public static PadFamily CurrentFamily
            => Gamepad.current != null ? FamilyOf(Gamepad.current) : PadFamily.PlayStation;

        /// <summary>
        /// ⚠️⚠️ THE ROW ARITHMETIC IS A CONTRACT WITH `tools/build_pad_prompt_icons.py`, WHICH
        /// STATES IT IN THE MANIFEST IT WRITES: rows are ps-light, ps-dark, xbox-light, xbox-dark,
        /// so `row = (xbox ? 2 : 0) + (onDark ? 1 : 0)`. Reordering the generator's `ROWS` puts
        /// Xbox glyphs on a DualShock, or cream ones on a cream screen, and nothing fails.
        ///
        /// ⚠️ BOTH TINTS ARE BAKED because `For`'s callers set `Image.sprite` and never
        /// `Image.color`.
        /// </summary>
        private static Sprite PadSprite(int column, bool onDark, PadFamily family)
        {
            int row = (family == PadFamily.Xbox ? 2 : 0) + (onDark ? 1 : 0);
            string id = "pad:" + column + ":" + row;
            if (Sprites.TryGetValue(id, out var made)) return made;

            var texture = Resources.Load<Texture2D>(PadSheetPath);

            if (texture == null)
            {
                Sprites[id] = null;
                return null;
            }

            int x = column * PadCellPixels;
            int y = texture.height - (row + 1) * PadCellPixels;

            if (x < 0 || y < 0
                || x + PadCellPixels > texture.width || y + PadCellPixels > texture.height)
            {
                Sprites[id] = null;
                return null;
            }

            var sprite = Sprite.Create(texture,
                                       new Rect(x, y, PadCellPixels, PadCellPixels),
                                       new Vector2(0.5f, 0.5f),
                                       PadCellPixels);
            sprite.name = "glyph_" + id;

            Sprites[id] = sprite;
            return sprite;
        }

        // -------------------------------------------------------------------
        // § THE SHEETS
        // -------------------------------------------------------------------

        private static readonly Dictionary<Sheet, Texture2D> Textures = new Dictionary<Sheet, Texture2D>();
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        private static string PathFor(Sheet sheet)
        {
            switch (sheet)
            {
                case Sheet.Mouse: return "UI/input/glyphs_mouse_v1";
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
        /// <summary>
        /// ⚠️ `family` OVERRIDES THE ATTACHED PAD AND ONLY ONE CALLER PASSES IT.
        /// `ControllerMapScreen` draws a DualShock and labels its own picture, so its glyphs
        /// follow the DRAWING rather than the hardware; everywhere else the player's own pad
        /// decides. Leaving it null is the normal answer.
        /// </summary>
        public static Sprite For(string label, bool onDark, PadFamily? family = null)
        {
            if (string.IsNullOrEmpty(label)) return null;

            string key = label.Trim().ToUpperInvariant();

            // ⚠️ THE PAD IS ASKED FIRST. Nothing is in both tables today, and if a keyboard row
            // is ever added whose name collides with a controller one, the controller answer is
            // the one a player holding a controller wants.
            if (PadColumns.TryGetValue(key, out int padColumn))
                return PadSprite(padColumn, onDark, family ?? CurrentFamily);

            if (!Table.TryGetValue(key, out var cell)) return null;

            int column = cell.Column;
            int row = cell.Row;

            switch (cell.Sheet)
            {
                case Sheet.Key:
                    column += onDark ? KeyVariantOnDark : KeyVariantOnLight;
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
        {
            if (string.IsNullOrEmpty(label)) return false;

            string key = label.Trim().ToUpperInvariant();
            return PadColumns.ContainsKey(key) || Table.ContainsKey(key);
        }

        /// <summary>
        /// Every label this table can draw, for the test that asserts the tutorial's prompts are
        /// covered.
        /// </summary>
        public static IEnumerable<string> KnownLabels()
        {
            foreach (string key in Table.Keys) yield return key;
            foreach (string key in PadColumns.Keys) yield return key;
        }
    }
}
