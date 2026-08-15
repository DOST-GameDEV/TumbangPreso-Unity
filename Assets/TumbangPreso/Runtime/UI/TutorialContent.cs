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
        public readonly struct Row
        {
            public readonly string Chip;
            public readonly string Body;
            public Row(string chip, string body) { Chip = chip; Body = body; }
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
                new Row("90s × 4 ROUNDS", "The taya moves one seat clockwise each round, so everybody is taya exactly once."),
                new Row("POINTS, NOT WINS", "You carry your own score across all four rounds. Highest total takes the match."),
                new Row("TAYA", "Guard the lata and never leave the box. Block throws, stand it back up, tag attackers."),
                new Row("ATTACKERS", "Throw from outside the box, then walk in and get your slipper back."),
            }),

            new Page("HOW A ROUND GOES", "", new[]
            {
                new Row("1.  THROW", "You start holding your slipper. From outside the box, hold LEFT CLICK to charge and release."),
                new Row("2.  IT LANDS", "Hit the lata and it goes over. If the taya blocks it, it drops right beside them."),
                new Row("3.  RETRIEVE", "Walk in and pick it up. This is the risk, and the entire point of the game."),
                new Row("4.  RESET", "The taya holds E by the lata for 1.5 seconds to stand it up. No throws for a moment after."),
            }),

            new Page("THE RISK", "", new[]
            {
                new Row("SAFE UNTIL YOU GRAB", "Empty-handed you cannot be tagged at all, even inside the box. The danger is self-inflicted."),
                new Row("TAGGED", "Thrown back to the safe zone and stunned for 5 seconds. Nobody is ever out of the round."),
                new Row("NO THROWING FROM INSIDE", "A throw needs you outside the box, the lata standing, and your pickup cooldown expired."),
                new Row("CROSSHAIR", "It only appears when a throw would be allowed. No crosshair means the throw is refused."),
            }),

            new Page("CONTROLS  ·  MOVING", "", new[]
            {
                new Row("W A S D", "Move."),
                new Row("MOUSE", "Look, and aim your throw at the point your crosshair is actually on."),
                new Row("SHIFT", "Sprint, about 1.5 seconds' worth. Empty it completely and you are winded for 2 seconds."),
                new Row("SPACE", "Jump."),
                new Row("ESC", "Pause."),
            }),

            new Page("CONTROLS  ·  ATTACKER", "", new[]
            {
                new Row("LEFT CLICK", "Hold to charge, release to throw. 2.5 seconds to full power; a tap still throws, weakly."),
                new Row("E  ·  tap", "Pick up any loose slipper you are standing near, yours or not. You can carry one."),
                new Row("E  ·  tap (nothing to grab)", "SHOVE a rival 2.5 metres back. If they are tagged after it, you are paid +50."),
            }),

            new Page("CONTROLS  ·  TAYA", "", new[]
            {
                new Row("LEFT CLICK", "PUNCH. An instant jab ahead, tagging any attacker in front of you holding a slipper."),
                new Row("E  ·  hold", "LUNGE. Hold half a second, release to dash a metre and tag anyone in the path."),
                new Row("E  ·  in the ring", "With the lata down, hold E to set it back up. Letting go loses all of it."),
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
