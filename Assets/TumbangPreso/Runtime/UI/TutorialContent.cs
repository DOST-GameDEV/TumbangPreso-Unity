namespace TumbangPreso.UI
{
    /// <summary>
    /// The HOW TO PLAY pages, converted verbatim from `scripts/ui/tutorial.gd`'s `PAGES`.
    ///
    /// ⚠️⚠️ IT SAYS "TAYA", NOT "DEFENDER", AND THE HUD IS WHY. Every readout in the match
    /// says taya — the round label, the YOU card, the scoreboard's marker. This is where a
    /// player learns the word, so teaching them a different one and then showing this one on
    /// screen for six minutes is the single thing these pages must not do. The gloss carries
    /// the English, the same trick LATA and TSINELAS already use.
    ///
    /// ⚠️ THE COLOUR RULE IS ON PAGE TWO ON PURPOSE. It is the one thing a player must carry
    /// into the match: the colours track the ROLE, so their own colour changes when their
    /// turn as taya comes.
    ///
    /// ⚠️ THE NUMBERS HERE ARE PROSE COPIES OF BALANCE VALUES. If a constant moves, these
    /// sentences are wrong and nothing will fail to compile. Check them against
    /// `Balance.cs` whenever tuning changes.
    /// </summary>
    public static class TutorialContent
    {
        /// <summary>
        /// One line of a page: a chip on the left, a sentence on the right.
        ///
        /// ⚠️⚠️ A CHIP THAT NAMES A KEY IS RESOLVED FROM THE LIVE BINDING, NOT TYPED. 🧑
        /// 2026-08-27: *"make it so that tutorial shows the actual keys u rebinded to and arent
        /// just hardcoded"*. Every chip on the three controls pages was a literal, so a player
        /// who rebound anything was taught the defaults for the rest of the match.
        ///
        /// ⚠️⚠️ AND THE LITERALS WERE ALREADY WRONG BEFORE ANYBODY REBOUND ANYTHING. This file
        /// said pickup was `E` and the taya's lunge was `E · hold`; the shipped map has `Grab` on
        /// **X** and `Lunge` on the **right mouse button**, and has since the one-control-one-action
        /// pass (`Rebinding`'s class note: E used to carry Grab, Lunge AND Skill1 at once). The
        /// HOW TO PLAY screen, which exists to teach the controls, was naming a key that does
        /// something else. That is the second, quieter half of what a hard-coded chip costs: a
        /// literal cannot go stale loudly.
        ///
        /// ⚠️ RESOLVED AT DRAW TIME, NEVER AT STATIC INIT. These arrays are `static readonly`, so
        /// baking a key into them would capture whatever the bindings were the first time
        /// anything touched this class, which is before `Rebinding.Load` has necessarily run.
        /// `ConvertedTutorialPanel` calls <see cref="ChipText"/> on every page turn.
        /// </summary>
        public readonly struct Row
        {
            private readonly string _chip;
            private readonly string _body;
            private readonly string[] _actions;

            /// <summary>A row that names no control: a heading, a number, a word.</summary>
            public Row(string chip, string body)
            {
                _chip = chip;
                _body = body;
                _actions = null;
            }

            private Row(string chip, string body, string[] actions)
            {
                _chip = chip;
                _body = body;
                _actions = actions;
            }

            /// <summary>
            /// A row whose chip, body or both name live controls. Both strings are
            /// `string.Format` patterns over the SAME key list, so `{0}` means the same control
            /// in either column and a row can name a key twice without repeating itself.
            /// </summary>
            public static Row Keyed(string chip, string body, params string[] actions)
                => new Row(chip, body, actions);

            /// <summary>What to draw on the left. Call this rather than reading a field.</summary>
            public string ChipText() => Resolve(_chip);

            /// <summary>What to draw on the right.</summary>
            public string BodyText() => Resolve(_body);

            private string Resolve(string pattern)
            {
                if (_actions == null || _actions.Length == 0) return pattern;

                var keys = new object[_actions.Length];
                for (int i = 0; i < _actions.Length; i++) keys[i] = Hud.KeyLabelFor(_actions[i]);

                return string.Format(pattern, keys);
            }
        }

        public readonly struct Page
        {
            public readonly string Title;
            public readonly string Lede;
            public readonly Row[] Rows;
            public Page(string title, string lede, Row[] rows)
            {
                Title = title; Lede = lede; Rows = rows;
            }
        }

        public const float ChipWidth = 330.0f;

        /// <summary>
        /// One column of the premise card: the real game object in 3D, the Filipino word under
        /// it in the ROLE colour, and the English gloss under that.
        ///
        /// ⚠️ THE PICTURE IS THE ACTUAL ASSET, not an icon drawn for this screen. There is no
        /// icon art in the project and inventing four pieces of it is the art lane's call —
        /// but the model preview already loads the real can, slipper and person rigs and frames
        /// them from their MEASURED bounds, so the card can show the player exactly the object
        /// they will meet in the match. It also means the page cannot go stale: reskin the lata
        /// and this page reskins with it.
        ///
        /// ⚠️ ONLY THE WORDS TAKE THE ROLE COLOUR, NEVER THE MODEL. Flat-tinting a person orange
        /// would fight the art palette and stop the person reading as a person. The colour rule
        /// is about what the UI says, and the words are the UI.
        /// </summary>
        public readonly struct Tile
        {
            public readonly string Kind;      // "can", "slipper", or a person
            public readonly int Index;        // roster index, for a person
            public readonly string Fil;
            public readonly string Eng;
            public readonly bool Offense;

            public Tile(string kind, int index, string fil, string eng, bool offense)
            {
                Kind = kind; Index = index; Fil = fil; Eng = eng; Offense = offense;
            }
        }

        public const float TileWidth = 250.0f;

        /// <summary>A FLOOR, not a height. The icon expands into whatever the panel has left
        /// after the two words. 210 stranded the strip at the top of a mostly empty panel and
        /// 330 clipped the English gloss and raised a scrollbar.</summary>
        public const float TileIconMinHeight = 190.0f;

        /// <summary>
        /// Pulls the camera in on the preview's measured shot, and centres the subject with it.
        ///
        /// ⚠️ `ModelPreview.FrameMargin` LEAVES 62% AIR, sized for a T-pose sharing the frame
        /// with a wood panel; in a tile that is the big empty box that was reported. 0.62 was
        /// tried and was too tight even centred, because it cancels the margin exactly and
        /// cropped the lata top and bottom. 0.80 fills the tile with a little air left, which is
        /// what a picture wants.
        /// </summary>
        public const float TileZoom = 0.80f;

        public const int TileFilSize = 46;
        public const int TileEngSize = 24;

        /// <summary>
        /// ⚠️ CROCS, NOT SLIPPER 0, AND IKE WAS TRIED FIRST AND RENDERED AS A BLACK BLOB. 🧑
        /// 2026-08-01, looking at this card: *"use ike tsinelas here the tsinelas model here
        /// looks ugly"*, then *"js do crocs"*. Index 0 is the procedural mesh and at icon size
        /// it reads as a brown smear; IKE's texture is nearly black and this tile's lighting is
        /// flat and head-on, so the model that looks best on the character screen loses all its
        /// shape at 120 px. **A prop that previews well in one frame is not a prop that reads as
        /// an icon.** Looked up by ID rather than by index, because the table has been reordered
        /// before and an index here would silently become a different shoe.
        /// </summary>
        public const string TileSlipperId = "crocs";

        /// <summary>The premise strip, page 1 only. `tutorial.gd` builds this instead of the
        /// chip rows, because the page has no hook to hang a chip on: it is naming things.</summary>
        public static readonly Tile[] PremiseTiles =
        {
            new Tile("can", 0, "LATA", "the can", false),
            new Tile("person", 0, "TAYA", "guards it, alone", false),
            new Tile("slipper", 0, "TSINELAS", "the slipper", true),
            new Tile("person", 1, "ATTACKER", "throws, then runs", true),
        };

        public static readonly Page[] Pages =
        {
            new Page("TUMBANG PRESO", "1v1v1v1. One taya.", new[]
            {
                new Row("LATA", "the can"),
                new Row("TAYA", "guards it, alone"),
                new Row("TSINELAS", "the slipper"),
                new Row("ATTACKER", "throws, then runs"),
            }),

            new Page("THE GAME",
                "Blue is the taya, orange is the attack. The colours follow the ROLE, so yours changes.",
                new[]
            {
                new Row("1v1v1v1", "Four players, four separate scores. No teams, no allies — empty seats are bots."),
                new Row("90s ROUNDS", "Classic plays 4 rounds; Hero Strike plays 8. The taya moves one seat clockwise each round."),
                new Row("POINTS, NOT WINS", "You carry your score across the whole match. Highest total takes it."),
                new Row("TAYA", "Guard the lata and never leave the box. Block throws, stand it back up, tag attackers."),
                new Row("ATTACKERS", "Throw from outside the box, then walk in and get your slipper back."),
            }),

            new Page("HOW A ROUND GOES", "", new[]
            {
                // ⚠️ THE KEYS IN THESE SENTENCES ARE LIVE TOO. A body that says "hold LEFT CLICK"
                // beside a chip that says something else is the same defect one column across.
                Row.Keyed("1.  THROW", "You start holding your slipper. From outside the box, hold {0} to charge and release.", "SpecialAbility"),
                new Row("2.  IT LANDS", "Hit the lata and it goes over. If the taya blocks it, it drops right beside them."),
                new Row("3.  RETRIEVE", "Walk in and pick it up. This is the risk, and the entire point of the game."),
                Row.Keyed("4.  RESET", "The taya holds {0} by the lata for 1.5 seconds to stand it up. No throws for a moment after.", "Grab"),
            }),

            new Page("THE RISK", "", new[]
            {
                new Row("SAFE UNTIL YOU GRAB", "Empty-handed you cannot be tagged at all, even inside the box. The danger is self-inflicted."),
                new Row("TAGGED", "Thrown back to the safe zone and stunned for 5 seconds. Nobody is ever out of the round."),
                new Row("NO THROWING FROM INSIDE", "A throw needs you outside the box, the lata standing, and your pickup cooldown expired."),
                new Row("CROSSHAIR", "It only appears when a throw would be allowed. No crosshair means the throw is refused."),
            }),

            // ⚠️⚠️ EVERY CHIP ON THE NEXT THREE PAGES IS A LIVE BINDING. See `Row`: they were
            // literals, three of them named a key the shipped map does not use for that job, and
            // a rebind was invisible to the one screen whose whole purpose is teaching controls.
            //
            // ⚠️ `Move` PRINTS THE WHOLE COMPOSITE. `Hud.KeyLabel` returns "WASD" in reading
            // order for the four-part composite and falls back to a slash-joined list for a
            // player who rebound them, which is why this row can be one chip rather than four.
            //
            // ⚠️ MOUSE LOOK AND ESC STAY LITERAL BECAUSE THEY ARE NOT IN THE MAP. Look is a raw
            // device axis and pause is handled by `PauseWatcher`, so there is no action to ask
            // and nothing a player can rebind. A chip resolved from a binding that does not exist
            // would print "-", which is worse than the truth.
            new Page("CONTROLS  ·  MOVING", "", new[]
            {
                Row.Keyed("{0}", "Move.", "Move"),
                new Row("MOUSE", "Look, and aim your throw at the point your crosshair is actually on."),
                Row.Keyed("{0}", "Sprint, about 1.5 seconds' worth. Empty it completely and you are winded for 2 seconds.", "Sprint"),
                Row.Keyed("{0}", "Jump.", "Jump"),
                new Row("ESC", "Pause."),
            }),

            new Page("CONTROLS  ·  ATTACKER", "", new[]
            {
                Row.Keyed("{0}", "Hold to charge, release to throw. 2.5 seconds to full power; a tap still throws, weakly.", "SpecialAbility"),

                // ⚠️ `Grab`, WHICH IS X. This row said E, which is `Skill2`. See the `Row` note.
                Row.Keyed("{0}  ·  tap", "Pick up any loose slipper you are standing near, yours or not. You can carry one.", "Grab"),
                Row.Keyed("{0}  ·  tap (nothing to grab)", "SHOVE a rival 2.5 metres back. If they are tagged after it, you are paid +50.", "Grab"),

                // ⚠️⚠️ THE CURVE WAS NOT ON THIS PAGE AT ALL, WHICH IS WHY IT HAD TO BE DISCOVERED
                // IN THE SETTINGS PANEL. It is the skill ceiling of the throw
                // (`GAME_OVERVIEW.md` § 4.3) and the one control the HOW TO PLAY screen never
                // mentioned. It moved to Z and C on 2026-08-27 (`Rebinding`), so a page that
                // named the arrow keys would have been wrong from the day it was added.
                Row.Keyed("{0} / {1}  ·  while charging",
                          "PEKTUS. Bend the throw left or right in the air. The mouse wheel does it too.",
                          "CurveLeft", "CurveRight"),
            }),

            new Page("CONTROLS  ·  TAYA", "", new[]
            {
                Row.Keyed("{0}", "PUNCH. An instant jab ahead, tagging any attacker in front of you holding a slipper.", "SpecialAbility"),

                // ⚠️ `Lunge`, WHICH IS THE RIGHT MOUSE BUTTON. This row said "E · hold", a key
                // that has not been the lunge since the one-control-one-action pass.
                Row.Keyed("{0}  ·  hold", "LUNGE. Hold half a second, release to dash a metre and tag anyone in the path.", "Lunge"),
                Row.Keyed("{0}  ·  in the ring", "With the lata down, hold it to set the can back up. Letting go loses all of it.", "Grab"),
                new Row("⚠  NO TAGS WHILE IT IS DOWN", "Neither verb can tag until the lata is standing. Reset first, then hunt."),
                new Row("YOU ARE FASTER", "Both verbs aim where you FACE, and you only turn while walking — keep moving into them."),
            }),

            new Page("SCORING", "", new[]
            {
                new Row("+100  KNOCKDOWN", "To the attacker whose slipper hit the lata. Only the thrower is paid."),
                new Row("+100  TAG", "To the taya, for catching an attacker who is holding a slipper inside the box."),
                new Row("+10 / s  DEFENCE", "To the taya, for every second the lata is left standing."),
                new Row("+50  SABOTAGE", "To an attacker who shoves a rival who is then tagged."),
            }),
        };
    }
}
