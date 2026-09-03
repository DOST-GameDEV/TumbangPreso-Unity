using System;
using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>
    /// PHASE 12. The format a match is played in, on TOP of its mode.
    ///
    /// ⚠️⚠️ A FORMAT IS NOT A `GameMode` AND MAKING IT ONE WOULD HAVE BROKEN FOUR THINGS.
    /// `FUTURE.md` § 12 calls LAST TSINELAS STANDING and MIRROR *"modes"* and caps the game at
    /// *"TWO EXTRA MODES, EVER"*, and the obvious reading is two more values on `GameMode`. That
    /// enum is not a label, it is a ruleset identity: `docs/VISION.md` § 1 says Classic and Hero
    /// Strike are *"two modes, neither a variant of the other"*, `MatchRules.RoundCountFor`
    /// branches on it, `ProfileRules` keeps a whole separate career per value and says so on
    /// screen (*"Classic and Hero Strike are separate games and their numbers never merge"*), and
    /// `MatchRecord.Mode` is a stored string that older builds read back.
    ///
    /// **Both of these are playable in EITHER mode**, which is the tell that they are not modes.
    /// LAST TSINELAS STANDING is a win condition; MIRROR is a restriction on what everybody
    /// brings. So they ride BESIDE the mode, and a Classic Last Tsinelas match is still a Classic
    /// match in the career, which is also what makes them cheap, and cheap is the entire argument
    /// § 12 makes for them: *"every mode below reuses the existing arena, rules and art"*.
    ///
    /// ⚠️ RANKED IS <see cref="Standard"/> ONLY, and that is `docs/TODO.md` § 105's decision
    /// rather than a new one: **one ladder, on HERO STRIKE**. A second win condition on the same
    /// ladder is a second game being rated by one number.
    /// </summary>
    public enum MatchFormat
    {
        /// <summary>Four rounds (Classic) or eight (Hero Strike), cumulative score.</summary>
        Standard = 0,

        /// <summary>
        /// Three tsinelas each; lose them all and you are out; the last attacker takes the round.
        ///
        /// ⚠️ `FUTURE.md` § 12: *"the most different game available from parts that already
        /// exist, which is why it earns the slot"*. Nothing here is new geometry, a new verb or a
        /// new piece of art: it is the retrieval rule with a stock counter on it, which is
        /// `docs/VISION.md`'s *"the tension is the retrieval, not the throw"* turned up.
        /// </summary>
        LastTsinelas = 1,

        /// <summary>
        /// Everybody plays the same character and the same tsinelas, rotated weekly.
        ///
        /// ⚠️ `FUTURE.md` § 12: *"the cheapest possible new mode, one line of lobby logic, and a
        /// genuinely good competitive format"*. It is one line because the pick already crosses
        /// the wire in `LobbySeatInfo`; this only decides what the pick is.
        /// </summary>
        Mirror = 2,
    }

    /// <summary>
    /// PHASE 12's rules: the two formats, and the settings a private lobby may change.
    ///
    /// ⚠️⚠️ CUSTOM GAMES COME FIRST IN § 12 AND EVERYTHING ELSE IN THE PHASE GETS CHEAPER AFTER
    /// THEM. `FUTURE.md` § 19.12 orders the phase explicitly: *"custom games, because everything
    /// else in this phase gets cheaper afterwards, and it is also the tournament tool for Phase
    /// 17"*. A format is a value in a custom rule set; a map vote is a custom rule set with more
    /// than one map in it.
    ///
    /// ⚠️ EVERY BOUND IN HERE IS A BOUND ON THE HOST, NOT A SUGGESTION TO IT. A custom lobby is
    /// the one place a player can write a number that every other machine then plays by, so each
    /// one is clamped on the way in and again on the way out of the wire. `docs/VISION.md` § 4:
    /// the host decides everything that scores, which is only safe while the host cannot be
    /// handed a number that breaks the match.
    /// </summary>
    public static class CustomGameRules
    {
        // ---- LAST TSINELAS STANDING -------------------------------------------------------

        /// <summary>
        /// How many tsinelas an attacker starts a Last Tsinelas round with.
        ///
        /// ⚠️ THREE, FROM `FUTURE.md` § 12 (*"three tsinelas per attacker"*), and it is also the
        /// number that makes the format last about as long as a standard round: a round is 90 s
        /// and a throw-plus-retrieval cycle measures around 12 s in `BotBehaviourProbe`, so three
        /// lives is between 36 and 60 seconds of play before anybody is out.
        /// </summary>
        public const int StartingTsinelas = 3;

        /// <summary>The bounds a custom lobby may set that stock to.</summary>
        public const int MinTsinelas = 1;
        public const int MaxTsinelas = 5;

        /// <summary>
        /// A tsinelas is spent when it is thrown and LOST, never when it is thrown.
        ///
        /// ⚠️⚠️ THIS IS THE WHOLE FORMAT IN ONE FUNCTION AND GETTING IT THE OTHER WAY ROUND WOULD
        /// HAVE DELETED THE GAME. If a throw costs a life, the optimal play is to never throw, and
        /// `docs/VISION.md`'s one paragraph is *"throwing is safe and free; going back in for your
        /// tsinelas is the only moment you can be caught"*. Spending on a FAILED RETRIEVAL keeps
        /// every incentive pointing the same way it does in the base game and makes the risk the
        /// thing that is scored. A tsinelas is lost when the round ends with it still on the floor,
        /// or when the taya tags you while you are carrying it back.
        /// </summary>
        public static int TsinelasLeft(int stock, int lostThisRound)
        {
            int left = stock - (lostThisRound < 0 ? 0 : lostThisRound);
            return left < 0 ? 0 : left;
        }

        /// <summary>An attacker with no tsinelas left is out for the rest of the round.</summary>
        public static bool IsOut(int stock, int lostThisRound) => TsinelasLeft(stock, lostThisRound) <= 0;

        /// <summary>
        /// Who takes a Last Tsinelas round: the last attacker still holding a tsinelas.
        ///
        /// ⚠️ IT ANSWERS -1 WHILE MORE THAN ONE IS ALIVE, AND ALSO WHEN NOBODY IS. Two survivors
        /// is a round still being played; zero survivors is everybody out on the same tick, which
        /// is rare and real (a round-end sweep can take the last two at once), and in that case the
        /// round belongs to the TAYA. The caller decides what to do with -1 rather than this
        /// picking a winner out of a tie, because a rule that invents a result is a rule nobody
        /// can check.
        /// </summary>
        public static int LastAttackerStanding(IReadOnlyList<int> stocks, int defenderSlot)
        {
            if (stocks == null) return -1;

            int alive = 0;
            int last = -1;

            for (int i = 0; i < stocks.Count; i++)
            {
                if (i == defenderSlot) continue;
                if (stocks[i] <= 0) continue;

                alive++;
                last = i;
            }

            return alive == 1 ? last : -1;
        }

        /// <summary>
        /// How many attackers still hold a tsinelas.
        ///
        /// ⚠️⚠️ THIS EXISTS BECAUSE <see cref="LastAttackerStanding"/> ANSWERS -1 FOR TWO
        /// OPPOSITE SITUATIONS AND THE MATCH HALF HAS TO TELL THEM APART. Its own note says so:
        /// -1 is "more than one alive" (the round is still being played) and also "nobody is
        /// alive" (everybody went out on the same tick, and the round belongs to the taya). A
        /// caller that only has the slot cannot distinguish "carry on" from "end the round and
        /// pay nobody", and guessing wrong either hangs a decided round on the clock or ends a
        /// live one with two people still playing.
        ///
        /// ⚠️ IT IS NOT FOLDED INTO A SINGLE FUNCTION RETURNING A TUPLE, because the winner and
        /// the count are asked at different moments: the count decides whether the round is over
        /// and the slot decides who is paid, and only the first is checked every tag.
        /// </summary>
        public static int AliveAttackers(IReadOnlyList<int> stocks, int defenderSlot)
        {
            if (stocks == null) return 0;

            int alive = 0;
            for (int i = 0; i < stocks.Count; i++)
            {
                if (i == defenderSlot) continue;
                if (stocks[i] > 0) alive++;
            }

            return alive;
        }

        /// <summary>
        /// Whether a Last Tsinelas round has been settled and should end before the clock does.
        ///
        /// ⚠️⚠️ ONE SURVIVOR **OR ZERO**, AND THE ZERO CASE IS THE ONE THAT WOULD HAVE BEEN
        /// MISSED. Writing this as `alive == 1` reads correctly and leaves a round with nobody
        /// left in it running to the full 90 seconds with four bodies that cannot act, which is
        /// the format's worst possible failure: a minute of nothing, on a screen that gives no
        /// reason. A round-end sweep taking the last two attackers on the same tick is rare and
        /// real, so this asks `alive &lt;= 1`.
        /// </summary>
        public static bool RoundIsDecided(IReadOnlyList<int> stocks, int defenderSlot)
            => AliveAttackers(stocks, defenderSlot) <= 1;

        /// <summary>
        /// What the last attacker standing is paid.
        ///
        /// ⚠️ IT IS A KNOCKDOWN'S WORTH, NOT A NEW ECONOMY. `MatchRules.PointsFor` pays 100 for a
        /// knockdown and this is the same 100, so a Last Tsinelas round is scored in the units
        /// every other round in the game is scored in and the career totals stay comparable.
        /// A format that pays in its own currency is a format whose scores cannot be read beside
        /// anybody else's.
        /// </summary>
        public const int LastStandingPoints = 100;

        // ---- MIRROR -----------------------------------------------------------------------

        /// <summary>
        /// Which character everybody plays this week, given the roster and the date.
        ///
        /// ⚠️⚠️ IT IS DERIVED FROM THE WEEK NUMBER AND NEVER STORED, WHICH IS WHAT MAKES IT ONE
        /// LINE OF LOBBY LOGIC. Every machine computes the same answer from the same UTC week with
        /// no service, no document and no wire field, so a LAN lobby in a hall with no internet
        /// mirrors the same character as an online one. `RatingRules.SeasonAt` uses the same shape
        /// for the same reason.
        ///
        /// ⚠️ THE WEEK IS COUNTED FROM `RatingRules.SeasonOneStartUtc`, so the rotation and the
        /// season boundary line up rather than drifting past each other by a few days a year.
        /// </summary>
        public static int MirrorIndex(int rosterCount, DateTime utc)
        {
            if (rosterCount <= 0) return 0;

            var span = utc.ToUniversalTime() - RatingRules.SeasonOneStartUtc;
            int weeks = (int)Math.Floor(span.TotalDays / 7.0);

            // ⚠️ THE MODULO IS MADE POSITIVE BY HAND. C#'s % keeps the sign of the left operand,
            // so a date before the epoch (a machine with a wrong clock, which happens at venues)
            // would index backwards off the front of the roster and throw.
            int index = weeks % rosterCount;
            return index < 0 ? index + rosterCount : index;
        }

        /// <summary>
        /// How long until the mirror character changes, in whole days.
        /// ⚠️ It is on the lobby caption so a player who dislikes this week's pick can see it is
        /// not permanent, which is the entire reason a rotation reads differently from a lock.
        /// </summary>
        public static int DaysUntilMirrorRotates(DateTime utc)
        {
            var span = utc.ToUniversalTime() - RatingRules.SeasonOneStartUtc;
            double intoWeek = span.TotalDays - (Math.Floor(span.TotalDays / 7.0) * 7.0);
            int left = (int)Math.Ceiling(7.0 - intoWeek);

            return left < 1 ? 1 : (left > 7 ? 7 : left);
        }

        // ---- THE CUSTOM RULE SET ----------------------------------------------------------

        public const int MinRoundSeconds = 30;
        public const int MaxRoundSeconds = 180;
        public const int MinRounds = 1;
        public const int MaxRounds = 12;
        public const int MinScoreTarget = 0;
        public const int MaxScoreTarget = 5000;
        public const int MaxBots = 3;

        /// <summary>
        /// A password is four to sixteen characters and is never a secret worth protecting.
        ///
        /// ⚠️⚠️ IT GATES A LOBBY, IT DOES NOT PROTECT AN ACCOUNT, AND THE DIFFERENCE IS WORTH
        /// WRITING DOWN BEFORE SOMEBODY "IMPROVES" IT. The lobby code is already a four-character
        /// public string and the host approves every connection; a password is one more thing a
        /// stranger has to be told before they can walk into somebody's private game. It is
        /// compared on the HOST (`docs/VISION.md` § 4: the host decides), it is never stored, and
        /// it must never be reused as anything else.
        /// </summary>
        public const int MinPasswordLength = 4;
        public const int MaxPasswordLength = 16;

        public static bool IsPasswordUsable(string password)
        {
            if (string.IsNullOrEmpty(password)) return true;

            string t = password.Trim();
            return t.Length >= MinPasswordLength && t.Length <= MaxPasswordLength;
        }

        public static int ClampRoundSeconds(int seconds)
            => seconds < MinRoundSeconds ? MinRoundSeconds
             : seconds > MaxRoundSeconds ? MaxRoundSeconds : seconds;

        public static int ClampRounds(int rounds)
            => rounds < MinRounds ? MinRounds : rounds > MaxRounds ? MaxRounds : rounds;

        public static int ClampScoreTarget(int target)
            => target < MinScoreTarget ? MinScoreTarget
             : target > MaxScoreTarget ? MaxScoreTarget : target;

        public static int ClampBots(int bots)
            => bots < 0 ? 0 : bots > MaxBots ? MaxBots : bots;

        /// <summary>
        /// The default rule set for a mode: exactly what the game plays today.
        ///
        /// ⚠️⚠️ A CUSTOM LOBBY OPENS ON THE SHIPPED RULES AND NOT ON A BLANK FORM. A player who
        /// opens custom games to change ONE thing must not have to reconstruct the other seven,
        /// and a screen full of empty fields is `CLAUDE.md` § 6.2's *"never overwhelming"* failed
        /// before anybody has touched it. `MatchRules.RoundCountFor` and `Balance.RoundTime`
        /// are the source; this file copies neither and asks both.
        /// </summary>
        public static CustomRules Defaults(GameMode mode) => new CustomRules
        {
            Mode = mode,
            Format = MatchFormat.Standard,
            Rounds = MatchRules.RoundCountFor(mode),
            RoundSeconds = (int)Balance.RoundTime,
            ScoreTarget = 0,
            Tsinelas = StartingTsinelas,
            Bots = 0,
            BotDifficulty = (int)Difficulty.Normal,
            Private = false,
            Password = "",
        };

        /// <summary>
        /// Whether a rule set is playable. ⚠️ It answers a REASON rather than a bool, because
        /// "cannot start" with no sentence is `docs/TODO.md` § 53.5's dead button again.
        /// </summary>
        public static string Refusal(CustomRules rules)
        {
            if (rules == null) return "There are no rules to play by.";
            if (!IsPasswordUsable(rules.Password))
                return $"A password is {MinPasswordLength} to {MaxPasswordLength} characters, or empty.";

            if (rules.Format == MatchFormat.LastTsinelas &&
                (rules.Tsinelas < MinTsinelas || rules.Tsinelas > MaxTsinelas))
                return $"Last Tsinelas Standing needs {MinTsinelas} to {MaxTsinelas} tsinelas each.";

            if (rules.Rounds < MinRounds || rules.Rounds > MaxRounds)
                return $"A match is {MinRounds} to {MaxRounds} rounds.";

            if (rules.RoundSeconds < MinRoundSeconds || rules.RoundSeconds > MaxRoundSeconds)
                return $"A round is {MinRoundSeconds} to {MaxRoundSeconds} seconds.";

            return "";
        }

        /// <summary>
        /// ⚠️⚠️ A CUSTOM MATCH IS NEVER RANKED, AND THIS IS THE ONE RULE IN THE FILE THAT IS NOT
        /// NEGOTIABLE. Every other field here is a number a player may choose, which is exactly
        /// why the ladder cannot see any of them: a 12-round match with a 30-second clock and
        /// three bots is not the game the rating was measured on. `docs/TODO.md` § 105: one
        /// ladder, on Hero Strike, on the shipped rules.
        /// </summary>
        public static bool CanBeRanked(CustomRules rules)
            => rules != null
               && rules.Format == MatchFormat.Standard
               && !rules.Private
               && rules.Bots == 0
               && rules.Rounds == MatchRules.RoundCountFor(rules.Mode)
               && rules.RoundSeconds == (int)Balance.RoundTime
               && rules.ScoreTarget == 0;

        /// <summary>How a format is written for a player to read.</summary>
        public static string FormatName(MatchFormat format) => format switch
        {
            MatchFormat.LastTsinelas => "LAST TSINELAS STANDING",
            MatchFormat.Mirror => "MIRROR",
            _ => "STANDARD",
        };

        /// <summary>One line saying what the format changes, for the lobby caption.</summary>
        public static string FormatBlurb(MatchFormat format) => format switch
        {
            MatchFormat.LastTsinelas =>
                "Three tsinelas each. Lose them all and you are out. The last attacker takes the round.",
            MatchFormat.Mirror =>
                "Everybody plays the same character and the same tsinelas. It changes every week.",
            _ => "The game as it ships.",
        };

        /// <summary>
        /// The compact wire form, so a lobby advert and the approval hello can carry a rule set
        /// without a second protocol.
        ///
        /// ⚠️ FIELDS ARE APPENDED, NEVER INSERTED, AND A SHORT STRING IS READ AS DEFAULTS. That
        /// is `docs/TODO.md` § 70.7's rule about a roster that only grows, applied to a record:
        /// an older build reading a newer string must get a playable answer rather than an
        /// exception, and `Parse` therefore fills anything missing from <see cref="Defaults"/>.
        /// </summary>
        public static string ToWire(CustomRules r)
        {
            if (r == null) return "";

            return string.Join("|",
                ((int)r.Mode).ToString(),
                ((int)r.Format).ToString(),
                r.Rounds.ToString(),
                r.RoundSeconds.ToString(),
                r.ScoreTarget.ToString(),
                r.Tsinelas.ToString(),
                r.Bots.ToString(),
                r.BotDifficulty.ToString(),
                r.Private ? "1" : "0");
        }

        public static CustomRules Parse(string wire, GameMode fallback)
        {
            var rules = Defaults(fallback);
            if (string.IsNullOrWhiteSpace(wire)) return rules;

            string[] parts = wire.Split('|');

            if (parts.Length > 0 && int.TryParse(parts[0], out int mode))
                rules.Mode = mode == (int)GameMode.HeroStrike ? GameMode.HeroStrike : GameMode.Classic;

            if (parts.Length > 1 && int.TryParse(parts[1], out int format))
                rules.Format = format >= 0 && format <= (int)MatchFormat.Mirror
                    ? (MatchFormat)format : MatchFormat.Standard;

            if (parts.Length > 2 && int.TryParse(parts[2], out int rounds)) rules.Rounds = ClampRounds(rounds);
            if (parts.Length > 3 && int.TryParse(parts[3], out int secs)) rules.RoundSeconds = ClampRoundSeconds(secs);
            if (parts.Length > 4 && int.TryParse(parts[4], out int target)) rules.ScoreTarget = ClampScoreTarget(target);

            if (parts.Length > 5 && int.TryParse(parts[5], out int stock))
                rules.Tsinelas = stock < MinTsinelas ? MinTsinelas : stock > MaxTsinelas ? MaxTsinelas : stock;

            if (parts.Length > 6 && int.TryParse(parts[6], out int bots)) rules.Bots = ClampBots(bots);

            if (parts.Length > 7 && int.TryParse(parts[7], out int tier))
                rules.BotDifficulty = tier < 0 ? 0 : tier > (int)Difficulty.Astig ? (int)Difficulty.Astig : tier;

            if (parts.Length > 8) rules.Private = parts[8] == "1";

            // ⚠️ THE PASSWORD IS NOT ON THE WIRE AND MUST NEVER BE. A lobby advert is readable by
            // everybody in the pool; a password in it is a lock with the key taped to the door.
            // The host holds it and compares what a joiner sends against it.
            rules.Password = "";

            return rules;
        }
    }

    /// <summary>
    /// One custom lobby's rule set. A plain data class so it can be serialised, sent and
    /// compared without dragging anything engine-shaped into the core.
    /// </summary>
    [Serializable]
    public sealed class CustomRules
    {
        public GameMode Mode = GameMode.HeroStrike;
        public MatchFormat Format = MatchFormat.Standard;
        public int Rounds = 8;
        public int RoundSeconds = 90;

        /// <summary>0 means "play every round", which is how the game ships.</summary>
        public int ScoreTarget;

        public int Tsinelas = CustomGameRules.StartingTsinelas;
        public int Bots;
        public int BotDifficulty = (int)Difficulty.Normal;
        public bool Private;

        /// <summary>⚠️ HOST-ONLY. Never serialised onto the wire. See `CustomGameRules.Parse`.</summary>
        public string Password = "";

        public CustomRules Clone() => new CustomRules
        {
            Mode = Mode,
            Format = Format,
            Rounds = Rounds,
            RoundSeconds = RoundSeconds,
            ScoreTarget = ScoreTarget,
            Tsinelas = Tsinelas,
            Bots = Bots,
            BotDifficulty = BotDifficulty,
            Private = Private,
            Password = Password,
        };
    }
}
