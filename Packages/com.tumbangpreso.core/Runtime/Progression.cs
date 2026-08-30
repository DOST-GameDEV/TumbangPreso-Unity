using System;
using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>
    /// What a track can hand out.
    ///
    /// ⚠️⚠️ FOUR KINDS, AND EVERY ONE OF THEM IS TEXT OR A COLOUR. `FUTURE.md` § 4.1 sorted
    /// every reward this game could offer by what it costs to author and drew the line under
    /// the fourth row: a title is a line in a data file, a badge is one flat shape, a palette is
    /// sixteen numbers `ToonSkin` already remaps, and a border is one 2D frame. An emote is an
    /// animation somebody has to make, and a tsinelas skin is a model, a UV and an import pass
    /// for one of the ten props the whole game looks at. **Adding a fifth kind here is a promise
    /// to author content on a schedule, which is the thing § 4 cut the season track to avoid.**
    /// </summary>
    public enum RewardKind
    {
        Title,
        Badge,
        Palette,
        Border,
    }

    /// <summary>
    /// One thing a player earns.
    ///
    /// ⚠️⚠️ IT CARRIES NO NUMBER AND THAT IS THE WHOLE DESIGN. `FUTURE.md` § 0.5 rule 4:
    /// *"Nothing on any progression track may change a gameplay number."* A reward that cannot
    /// hold a number cannot change one, so the rule is enforced by the shape of this type rather
    /// than by everybody remembering it. `ProgressionTests.ARewardCannotCarryAGameplayNumber`
    /// walks this class by reflection and fails if a numeric field is ever added, which is the
    /// only test that keeps being true after somebody adds a field in a hurry.
    ///
    /// ⚠️ AND IT IS NOT STORED. See <see cref="ProgressionRules.AccountRewards"/>: what a player
    /// has earned is a pure function of their level, so there is no inventory to write, no
    /// migration when the table grows and no way for the document and the table to disagree.
    /// </summary>
    [Serializable]
    public sealed class Reward
    {
        public RewardKind Kind;
        public string Id = "";
        public string Label = "";

        public Reward() { }

        public Reward(RewardKind kind, string id, string label)
        {
            Kind = kind;
            Id = id ?? "";
            Label = label ?? "";
        }
    }

    /// <summary>One hero's mastery. ⚠️ Only the six heroes get one; `FUTURE.md` § 4 narrowed the
    /// paths from eighteen characters to six on 2026-08-31 and § 10 has the reasoning.</summary>
    [Serializable]
    public sealed class MasteryRecord
    {
        public string Id = "";
        public int Xp;
        public int Level = 1;
    }

    /// <summary>
    /// One line of the end-of-match XP breakdown: what it was for, and what it paid.
    ///
    /// ⚠️ THE BREAKDOWN IS COMPUTED IN THE CORE RATHER THAN ASSEMBLED BY THE SCREEN, because a
    /// bar that adds up to a different number from the one the server awarded is the single most
    /// reported bug class in every game that has one. <see cref="ProgressionRules.Breakdown"/>
    /// and <see cref="ProgressionRules.MatchXp"/> are asserted to agree.
    /// </summary>
    public readonly struct XpLine
    {
        public readonly string Label;
        public readonly int Xp;

        public XpLine(string label, int xp)
        {
            Label = label ?? "";
            Xp = xp;
        }
    }

    /// <summary>What one finished match paid, for the screen to draw.</summary>
    public sealed class XpAward
    {
        public int MatchXp;
        public int MasteryXp;
        public string MasteryId = "";

        public int LevelBefore = 1;
        public int LevelAfter = 1;
        public int MasteryLevelBefore = 1;
        public int MasteryLevelAfter = 1;

        /// <summary>The seat did nothing for a whole round and was paid nothing for the match.</summary>
        public bool Afk;

        /// <summary>The account was inside an earned XP suspension, so the match paid nothing
        /// even though it was played properly. See <see cref="ProgressionRules.AfkPenaltyMatches"/>.</summary>
        public bool Suspended;

        public List<Reward> Unlocked = new List<Reward>();
    }

    /// <summary>
    /// XP, levels and per-hero mastery. `FUTURE.md` § 4 and its prompt § 19.4.
    ///
    /// ⚠️⚠️ THE RATE IS FLAT AND THAT IS A DECISION, NOT AN OMISSION. No diminishing returns, no
    /// rested bonus, no daily cap: a match pays what a match pays, for everybody, forever. Two
    /// rate curves were proposed on 2026-08-31 and 🧑 cut both, the second one
    /// (*"3 diminishing xp is doing too much"*, *"dont do diminishing xp"*) being the instructive
    /// cut, because rested XP is the better mechanism and is still a whole extra system with a
    /// pool, a rate, a UI and a rule nobody asked for. **The problem those curves solve does not
    /// exist here**: nothing on any track touches a match (rule 4 above), so a player who grinds
    /// out-levels a player who does not and buys a border for it.
    ///
    /// ⚠️⚠️ AND THE LEVEL COST IS FLAT FOR THE SAME REASON. Every level costs
    /// <see cref="XpPerLevel"/>, forever. A rising curve is diminishing returns wearing a
    /// different hat: the player feels the same slowdown and cannot see where it came from.
    /// "Every match pays about the same and every level costs the same" is one sentence, which is
    /// the bar § 4 sets.
    ///
    /// ⚠️ EVERY NUMBER IN HERE IS A STARTING POINT AND SAYS WHAT IT WAS DERIVED FROM.
    /// `FUTURE.md` § 0.6: the numbers in the plan are illustrations, not balance. There is
    /// telemetry now (`docs/TODO.md` § 90.3), so the first real match-length and completion
    /// distribution is what should move these, not an opinion.
    /// </summary>
    public static class ProgressionRules
    {
        // -------------------------------------------------------------------
        // § WHAT A MATCH PAYS
        //
        // ⚠️⚠️ COMPLETION IS THE BIGGEST SINGLE TERM AND PLACEMENT IS THE SMALLEST, WHICH IS
        // `FUTURE.md` § 4 AS ARITHMETIC: *"Weight completion heavily and placement lightly, so
        // leaving is the only thing that costs."* Finishing pays 100. Winning pays 40 more than
        // nothing and 30 more than coming last. A player who finishes every match in last place
        // earns 110 a match against the 140 a winner earns, which is 79 per cent: enough that
        // winning is worth something and nowhere near enough that losing is worth quitting.
        //
        // ⚠️ LEAVING PAYS ZERO WITHOUT A RULE FOR IT. A `MatchRecord` is authored at the whistle
        // by `MatchStatsCollector.OnMatchEnded` and there is no record for a match somebody left,
        // so a leaver is not penalised, they are simply never paid. That is the cheapest possible
        // implementation of the asymmetry § 4 asks for, and it is why no "leave penalty" exists.
        // -------------------------------------------------------------------

        public const int CompletionXp = 100;

        /// <summary>Index 0 is 1st place. ⚠️ Length is <see cref="Balance.PlayerCount"/> because a
        /// four-player game has four outcomes; `VISION.md` § 2.1 makes the same point about the
        /// placement distribution on the profile.</summary>
        public static readonly int[] PlacementXp = { 40, 25, 15, 10 };

        // The objectives. ⚠️ EACH ONE IS A THING THE GAME IS ABOUT, PAID ONCE, FLAT. They are
        // deliberately "did it at all" rather than "did it n times": a per-event rate is a second
        // scoring system running beside `MatchDirector.AddScore`, and a player would have to hold
        // two of them in their head at once (`FUTURE.md` § 0.5 rule 11b).
        public const int ObjectiveKnockdownXp = 15;

        /// <summary>⚠️ THE BIGGEST OBJECTIVE, ON PURPOSE. `VISION.md` § 0: *"The tension is the
        /// retrieval, not the throw."* A retrieval made inside the taya's reach is the one moment
        /// the whole game is built around, so it is the one the track pays most for.</summary>
        public const int ObjectivePressureRetrievalXp = 20;

        public const int ObjectiveTagXp = 15;
        public const int ObjectiveSabotageXp = 10;

        /// <summary>Finished with no taya-camp and no unretrieved-slipper penalty. ⚠️ It pays for
        /// NOT doing something, which is the only shape of objective that can reward the taya and
        /// the attackers with one rule.</summary>
        public const int ObjectiveCleanXp = 15;

        // -------------------------------------------------------------------
        // § THE LEVEL CURVE
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ 1000, WHICH IS BETWEEN FIVE AND NINE MATCHES. A match pays 110 at worst and 215 at
        /// best (completion, first, and every objective), so the fastest level is 5 matches and
        /// the slowest is 9. That range is the number to move once telemetry has a real
        /// match-length distribution to point at; it is not a measurement yet and says so.
        /// </summary>
        public const int XpPerLevel = 1000;

        /// <summary>⚠️ A NEW BORDER EVERY 50 LEVELS, `FUTURE.md` § 4. Border 0 is what every
        /// account starts with, so the first EARNED border is at level 50.</summary>
        public const int LevelsPerBorder = 50;

        /// <summary>
        /// ⚠️ TWICE <see cref="XpPerLevel"/>, BECAUSE MASTERY IS PER HERO AND THERE ARE SIX OF
        /// THEM. A player spreading their matches across the roster levels their account at the
        /// same rate as anybody else and levels each hero a sixth as fast, which is the point: a
        /// mastery number says you played THAT hero, and it would say nothing if it moved as fast
        /// as the account level.
        /// </summary>
        public const int MasteryXpPerLevel = 2000;

        // -------------------------------------------------------------------
        // § AFK
        //
        // ⚠️⚠️ THIS EXISTS BEFORE XP DOES AND § 19.4 ORDERS IT FIRST FOR A REASON: *"The moment
        // completion pays, standing still pays."* Completion is the largest term above, and a
        // seat that loads in and walks away collects it four times an hour.
        // -------------------------------------------------------------------

        /// <summary>
        /// How far a seat has to travel in a round to count as having played it: 5.06 m.
        ///
        /// ⚠️⚠️ IT IS DERIVED FROM MOVEMENT AND ROUND LENGTH RATHER THAN PICKED, and it is
        /// deliberately generous. It is two seconds of walking at the ATTACKER's speed, which is
        /// the slowest anyone moves (`Balance.Speed` scaled by `Balance.AttackerSpeedScale`), out
        /// of a 90-second round. `BotBehaviourProbe` measures live seats at roughly 130 m a round,
        /// so this bar is about 4 per cent of what playing looks like: it separates "did nothing
        /// whatsoever" from "played badly", and only the first of those is what § 4 means by AFK.
        ///
        /// ⚠️ AND MOVEMENT IS THE SIGNAL RATHER THAN INPUT, WHICH IS NOT A SHORTCUT. The host
        /// does not receive remote players' `InputIntent` at all: `MatchRpc.SubmitMoveServerRpc`
        /// carries a transform, not a key. Reading `Intent` would have detected AFK on the local
        /// seat and on bots and on nobody else, which is precisely backwards. Position arrives for
        /// every seat, the host already samples it for `DistanceTravelled`, and a player who is
        /// there is a player who moves.
        /// </summary>
        public const float AfkRoundMetres =
            Balance.Speed * Balance.AttackerSpeedScale * AfkActiveSeconds;

        /// <summary>Two seconds of the 90-second round. See <see cref="AfkRoundMetres"/>.</summary>
        public const float AfkActiveSeconds = 2.0f;

        /// <summary>
        /// ⚠️ THE ESCALATION IS ONE SENTENCE LONG AND THAT IS ITS BUDGET. `FUTURE.md` § 0.5 rule
        /// 11b measures a feature by what the PLAYER has to hold in their head, so this is: "go
        /// AFK and the match pays nothing; do it three times and you earn nothing for three
        /// matches." No tiers, no timers, no ban, nothing that stops anybody playing.
        /// </summary>
        public const int AfkStrikesBeforePenalty = 3;

        public const int AfkPenaltyMatches = 3;

        // -------------------------------------------------------------------
        // § THE QUESTIONS
        // -------------------------------------------------------------------

        /// <summary>
        /// Whether this seat sat out a whole round.
        ///
        /// ⚠️⚠️ AN UNMEASURED RECORD IS NOT AN AFK RECORD. <see cref="PlayerMatchStats.ActiveRounds"/>
        /// is -1 for every record written before this phase existed and for every record written by
        /// a peer on an older build, and the offline queue resubmits records that can be weeks old.
        /// Reading -1 as "zero active rounds" would mark every historical match AFK and strike out
        /// accounts for matches they played properly. This is the same sentinel, for the same
        /// reason, as <see cref="PlayerMatchStats.TimeToFirstThrow"/>'s -1.
        ///
        /// ⚠️ A BOT IS NEVER AFK. It has no account to pay and no strikes to carry, and a bot
        /// standing still is a bug in the AI rather than a player being idle.
        /// </summary>
        public static bool WasAfk(MatchRecord record, PlayerMatchStats line)
        {
            if (record == null || line == null || line.IsBot) return false;
            if (record.Rounds <= 0) return false;
            if (line.ActiveRounds < 0) return false;
            return line.ActiveRounds < record.Rounds;
        }

        /// <summary>
        /// What one match pays, before any suspension. Zero for an AFK seat.
        ///
        /// ⚠️ IT IS THE SUM OF <see cref="Breakdown"/> AND A TEST ASSERTS THAT. Two ways of
        /// computing the same number is how a results screen ends up disagreeing with a career.
        /// </summary>
        public static int MatchXp(MatchRecord record, PlayerMatchStats line)
        {
            int total = 0;
            foreach (var part in Breakdown(record, line)) total += part.Xp;
            return total;
        }

        /// <summary>
        /// The end-of-match bar, line by line, in the order it is drawn.
        ///
        /// ⚠️ AN AFK MATCH RETURNS ONE LINE READING ZERO RATHER THAN AN EMPTY LIST. A screen given
        /// nothing draws nothing, and a player who was paid nothing needs to be told why more than
        /// anybody else does.
        /// </summary>
        public static List<XpLine> Breakdown(MatchRecord record, PlayerMatchStats line)
        {
            var lines = new List<XpLine>();
            if (record == null || line == null || line.IsBot) return lines;

            if (WasAfk(record, line))
            {
                lines.Add(new XpLine("AWAY FOR A ROUND", 0));
                return lines;
            }

            lines.Add(new XpLine("MATCH FINISHED", CompletionXp));

            int place = line.Placement;
            if (place >= 1 && place <= PlacementXp.Length)
                lines.Add(new XpLine(PlacementLabel(place), PlacementXp[place - 1]));

            if (line.Knockdowns > 0) lines.Add(new XpLine("KNOCKED THE LATA", ObjectiveKnockdownXp));
            if (line.RetrievalsUnderPressure > 0)
                lines.Add(new XpLine("RETRIEVED UNDER PRESSURE", ObjectivePressureRetrievalXp));
            if (line.Tags > 0) lines.Add(new XpLine("TAGGED AS TAYA", ObjectiveTagXp));
            if (line.Sabotages > 0) lines.Add(new XpLine("SABOTAGE", ObjectiveSabotageXp));
            if (line.TayaCampPenalties == 0 && line.UnretrievedSlipperPenalties == 0)
                lines.Add(new XpLine("NO PENALTIES", ObjectiveCleanXp));

            return lines;
        }

        private static string PlacementLabel(int place)
        {
            switch (place)
            {
                case 1: return "1ST PLACE";
                case 2: return "2ND PLACE";
                case 3: return "3RD PLACE";
                default: return place + "TH PLACE";
            }
        }

        /// <summary>Level from total XP. Uncapped, and never below 1.</summary>
        public static int LevelForXp(int xp) => xp <= 0 ? 1 : 1 + xp / XpPerLevel;

        /// <summary>How far into the current level, for the bar. Always 0 to
        /// <see cref="XpPerLevel"/> - 1.</summary>
        public static int XpIntoLevel(int xp) => xp <= 0 ? 0 : xp % XpPerLevel;

        /// <summary>⚠️ Border 0 is the starting frame nobody earned. The first earned one is at
        /// level <see cref="LevelsPerBorder"/>.</summary>
        public static int BorderForLevel(int level) => level <= 0 ? 0 : level / LevelsPerBorder;

        /// <summary>Mastery level from that hero's XP. Same shape as the account level and a
        /// different divisor; see <see cref="MasteryXpPerLevel"/>.</summary>
        public static int MasteryLevelForXp(int xp) => xp <= 0 ? 1 : 1 + xp / MasteryXpPerLevel;

        /// <summary>
        /// Whether this roster id has a mastery path.
        ///
        /// ⚠️ THE SIX HEROES ONLY, AND IT IS ASKED OF `Roster.HeroPeople` RATHER THAN OF A SECOND
        /// LIST WRITTEN HERE. `FUTURE.md` § 4 narrowed the paths from eighteen to six on
        /// 2026-08-31; the other twelve keep a played count, which `PlayerProfile.Characters`
        /// already carries for every character in the game. A copy of the hero ids in this file
        /// is a copy that goes stale the first time a hero is added.
        /// </summary>
        public static bool HasMasteryPath(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return false;
            foreach (var entry in Roster.HeroPeople)
                if (entry != null && entry.Id == characterId) return true;
            return false;
        }

        public static MasteryRecord MasteryFor(PlayerProfile profile, string characterId)
        {
            if (profile == null || string.IsNullOrEmpty(characterId)) return null;
            profile.Mastery ??= new List<MasteryRecord>();

            foreach (var m in profile.Mastery)
                if (m != null && m.Id == characterId) return m;

            var added = new MasteryRecord { Id = characterId, Level = 1 };
            profile.Mastery.Add(added);
            return added;
        }

        // -------------------------------------------------------------------
        // § AWARDING IT
        // -------------------------------------------------------------------

        /// <summary>
        /// Pays one match into a career.
        ///
        /// ⚠️⚠️ IT IS CALLED FROM INSIDE <see cref="ProfileRules.Apply"/> AND FROM NOWHERE ELSE,
        /// WHICH IS THE ONLY THING MAKING IT IDEMPOTENT. `Apply` refuses a `MatchId` it has
        /// already counted, and the offline queue exists precisely to resubmit records, so a
        /// second entry point would be a second chance to pay the same match. Calling this beside
        /// `Apply` rather than inside it would work on the day it was written and double a career
        /// the first time somebody's Wi-Fi dropped.
        ///
        /// ⚠️ THE SUSPENSION IS SPENT BY A MATCH THAT WOULD OTHERWISE HAVE PAID. An AFK match
        /// does not count against it: otherwise the fastest way out of a suspension is to keep
        /// standing still, which is the opposite of what the rule is for.
        /// </summary>
        public static XpAward Award(PlayerProfile profile, MatchRecord record, PlayerMatchStats line)
        {
            var award = new XpAward();
            if (profile == null || record == null || line == null || line.IsBot) return award;

            profile.Mastery ??= new List<MasteryRecord>();
            if (profile.Level < 1) profile.Level = 1;

            award.LevelBefore = LevelForXp(profile.Xp);
            award.MasteryId = HasMasteryPath(line.CharacterId) ? line.CharacterId : "";

            var mastery = string.IsNullOrEmpty(award.MasteryId) ? null : MasteryFor(profile, award.MasteryId);
            award.MasteryLevelBefore = mastery != null ? MasteryLevelForXp(mastery.Xp) : 1;
            award.MasteryLevelAfter = award.MasteryLevelBefore;

            if (WasAfk(record, line))
            {
                award.Afk = true;
                profile.AfkStrikes++;
                if (profile.AfkStrikes >= AfkStrikesBeforePenalty)
                {
                    profile.AfkStrikes = 0;
                    profile.XpPenaltyMatches = AfkPenaltyMatches;
                }
                award.LevelAfter = award.LevelBefore;
                return award;
            }

            // ⚠️ A CLEAN MATCH CLEARS THE STRIKES RATHER THAN DECREMENTING THEM. Three AFK
            // matches in a row is a player who walked away; three across a month is a player
            // whose connection dropped, and the second one must not accumulate into a penalty.
            profile.AfkStrikes = 0;

            if (profile.XpPenaltyMatches > 0)
            {
                profile.XpPenaltyMatches--;
                award.Suspended = true;
                award.LevelAfter = award.LevelBefore;
                return award;
            }

            award.MatchXp = MatchXp(record, line);
            profile.Xp += award.MatchXp;
            profile.Level = LevelForXp(profile.Xp);
            award.LevelAfter = profile.Level;

            if (mastery != null)
            {
                // ⚠️ MASTERY IS PAID THE SAME XP THE ACCOUNT GOT, not a separate calculation.
                // One number the player earned, spent on two tracks that divide it differently.
                award.MasteryXp = award.MatchXp;
                mastery.Xp += award.MasteryXp;
                mastery.Level = MasteryLevelForXp(mastery.Xp);
                award.MasteryLevelAfter = mastery.Level;
            }

            CollectUnlocks(award);
            return award;
        }

        private static void CollectUnlocks(XpAward award)
        {
            for (int level = award.LevelBefore + 1; level <= award.LevelAfter; level++)
                foreach (var reward in AccountRewardsAt(level))
                    award.Unlocked.Add(reward);

            if (string.IsNullOrEmpty(award.MasteryId)) return;

            for (int level = award.MasteryLevelBefore + 1; level <= award.MasteryLevelAfter; level++)
                foreach (var reward in MasteryRewardsAt(award.MasteryId, level))
                    award.Unlocked.Add(reward);
        }

        // -------------------------------------------------------------------
        // § THE REWARD TABLES
        //
        // ⚠️⚠️ NOTHING IS STORED. What a player owns is `AccountRewards(level)`, a pure function,
        // so there is no inventory document to write, nothing to migrate when a row is added, and
        // no way for a career and a table to disagree about what somebody earned. Adding a title
        // at level 30 grants it retroactively to everybody who is already past 30, which is the
        // behaviour anybody would expect and is free here.
        //
        // ⚠️ `PlayerProfile.Inventory` IS PHASE 5'S AND IS NOT THIS. It is for cosmetics that are
        // not a function of a level. Do not start writing track rewards into it.
        // -------------------------------------------------------------------

        /// <summary>Account titles, by the level that grants them.</summary>
        private static readonly (int Level, RewardKind Kind, string Id, string Label)[] AccountTable =
        {
            (5,   RewardKind.Title,   "title.taga_kanto",     "TAGA-KANTO"),
            (10,  RewardKind.Badge,   "badge.first_lata",     "FIRST LATA"),
            (15,  RewardKind.Title,   "title.palaboy",        "PALABOY"),
            (25,  RewardKind.Title,   "title.hari_ng_tapat",  "HARI NG TAPAT"),
            (35,  RewardKind.Badge,   "badge.tsinelas_tatlo", "TATLONG TSINELAS"),
            (50,  RewardKind.Border,  "border.tanso",         "TANSO"),
            (60,  RewardKind.Title,   "title.tagapagtanggol", "TAGAPAGTANGGOL"),
            (75,  RewardKind.Title,   "title.walang_takas",   "WALANG TAKAS"),
            (100, RewardKind.Border,  "border.pilak",         "PILAK"),
            (150, RewardKind.Border,  "border.ginto",         "GINTO"),
            (200, RewardKind.Title,   "title.alamat",         "ALAMAT"),
        };

        /// <summary>
        /// The mastery ladder, the same five steps for every hero.
        ///
        /// ⚠️ ONE TABLE FOR ALL SIX RATHER THAN SIX TABLES, so a new hero ships with a full path
        /// instead of five blank tiles. The reward IDS are per hero (`mastery.zack.title.3`);
        /// only the SHAPE is shared. `docs/TODO.md` § 91 records why the ids carry the hero.
        /// </summary>
        private static readonly (int Level, RewardKind Kind, string Suffix, string Label)[] MasteryTable =
        {
            (3,  RewardKind.Title,   "title.katuwang",  "KATUWANG"),
            (5,  RewardKind.Palette, "palette.alt1",    "ALTERNATE COLOURS"),
            (10, RewardKind.Badge,   "badge.mastery",   "MASTERY"),
            (15, RewardKind.Palette, "palette.alt2",    "SECOND COLOURS"),
            (25, RewardKind.Title,   "title.dalubhasa", "DALUBHASA"),
        };

        public static List<Reward> AccountRewardsAt(int level)
        {
            var found = new List<Reward>();
            foreach (var row in AccountTable)
                if (row.Level == level) found.Add(new Reward(row.Kind, row.Id, row.Label));
            return found;
        }

        /// <summary>Everything an account of this level has earned.</summary>
        public static List<Reward> AccountRewards(int level)
        {
            var found = new List<Reward>();
            foreach (var row in AccountTable)
                if (row.Level <= level) found.Add(new Reward(row.Kind, row.Id, row.Label));
            return found;
        }

        public static List<Reward> MasteryRewardsAt(string heroId, int level)
        {
            var found = new List<Reward>();
            if (!HasMasteryPath(heroId)) return found;

            foreach (var row in MasteryTable)
                if (row.Level == level)
                    found.Add(new Reward(row.Kind, "mastery." + heroId + "." + row.Suffix, row.Label));
            return found;
        }

        public static List<Reward> MasteryRewards(string heroId, int level)
        {
            var found = new List<Reward>();
            if (!HasMasteryPath(heroId)) return found;

            foreach (var row in MasteryTable)
                if (row.Level <= level)
                    found.Add(new Reward(row.Kind, "mastery." + heroId + "." + row.Suffix, row.Label));
            return found;
        }

        /// <summary>Every level at which the account track pays something, ascending. For the
        /// screen that draws the ladder ahead of the player.</summary>
        public static List<int> AccountRewardLevels()
        {
            var levels = new List<int>();
            foreach (var row in AccountTable)
                if (!levels.Contains(row.Level)) levels.Add(row.Level);
            levels.Sort();
            return levels;
        }

        /// <summary>Every level at which a hero's mastery pays something, ascending.</summary>
        public static List<int> MasteryRewardLevels()
        {
            var levels = new List<int>();
            foreach (var row in MasteryTable)
                if (!levels.Contains(row.Level)) levels.Add(row.Level);
            levels.Sort();
            return levels;
        }
    }
}
