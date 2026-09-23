using System;
using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>What a catalogue row sells.</summary>
    public enum ShopKind
    {
        Hero = 0,
        Slipper = 1,
        Can = 2,
    }

    /// <summary>One thing the shop sells, at the one price the server charges.</summary>
    public sealed class CatalogItem
    {
        /// <summary>The wallet id, `kind:ref`, e.g. `hero:zack`, `slipper:heels`, `can:karne`.</summary>
        public string Id { get; }
        public ShopKind Kind { get; }

        /// <summary>The roster id the item is (`Roster.HeroPeople` / `Slippers` / `Cans`).</summary>
        public string RefId { get; }
        public int Price { get; }

        public CatalogItem(ShopKind kind, string refId, int price)
        {
            Kind = kind;
            RefId = refId;
            Price = price;
            Id = EconomyRules.ItemId(kind, refId);
        }
    }

    /// <summary>The wallet document, exactly as Cloud Save's protected `wallet` key holds it.</summary>
    [Serializable]
    public sealed class Wallet
    {
        public int Balance;
        public List<string> Owned = new List<string>();

        /// <summary>Match ids this wallet has already been paid for. Newest last, bounded.</summary>
        public List<string> PaidMatchIds = new List<string>();

        /// <summary>The UTC day number <see cref="EarnedToday"/> belongs to.</summary>
        public int EarnedDay;
        public int EarnedToday;

        /// <summary>Claimed tasks as `period:index:taskId`, so a task can be claimed once per period.</summary>
        public List<string> Claimed = new List<string>();

        public int Version = 1;
        public string CreatedUtc = "";
    }

    /// <summary>What one task counts.</summary>
    public enum TaskStat
    {
        Matches = 0,
        Wins = 1,
        Knockdowns = 2,
        Retrievals = 3,
        PressureRetrievals = 4,
        Tags = 5,
        Throws = 6,
        HeroStrikeMatches = 7,
        ClassicMatches = 8,
        Podiums = 9,
    }

    public enum TaskPeriod
    {
        Daily = 0,
        Weekly = 1,
    }

    public sealed class TaskDef
    {
        public string Id { get; }
        public TaskPeriod Period { get; }
        public TaskStat Stat { get; }
        public int Target { get; }
        public int Reward { get; }

        /// <summary>The sentence the TASKS screen shows, with {0} for the target.</summary>
        public string Text { get; }

        public TaskDef(string id, TaskPeriod period, TaskStat stat, int target, int reward, string text)
        {
            Id = id;
            Period = period;
            Stat = stat;
            Target = target;
            Reward = reward;
            Text = text;
        }

        public string Sentence => string.Format(Text, Target);
    }

    /// <summary>
    /// The facts one finished match contributes to tasks and payouts, read off the player's own
    /// line of a SERVER-HELD match record. ⚠️ Never built from anything a client asserts.
    /// </summary>
    public struct EarnedMatch
    {
        public string MatchId;
        public int Day;
        public string Mode;
        public int Placement;
        public bool Afk;
        public int Throws, Knockdowns, Retrievals, PressureRetrievals, Tags;
    }

    /// <summary>
    /// TANSAN: the soft currency, the shop, the starter set, migration and tasks.
    ///
    /// ⚠️⚠️ THIS FILE IS THE SPECIFICATION AND `ugs/cloud-code/wallet.js` IS THE COPY THAT DECIDES.
    /// The same trade `match-record.js` records about `ProfileRules`: Cloud Code cannot import C#,
    /// so every rule is written twice, the C# carries the tests (`Core.Tests/EconomyTests.cs`), and
    /// when the two disagree the JavaScript is the bug. Every constant below is named in the
    /// script beside the line that mirrors it.
    ///
    /// ⚠️⚠️ THE SERVER IS THE ONLY WRITER OF A BALANCE. A client computes nothing here that it then
    /// sends: payouts are derived from `matchHistory`, which only `match-record.js` writes; tasks
    /// are derived from the same records; a purchase names an id and the server looks the price
    /// up. The C# copy exists so the TASKS screen can draw progress offline and so the rules can be
    /// asserted in a second, never so a client can grant itself anything.
    ///
    /// ⚠️ NO REAL MONEY, EVER. There is no purchase of TANSAN anywhere; the + beside the balance
    /// opens TASKS. The owner's rule for UX-1, and `FUTURE.md`'s free-tier constraint.
    ///
    /// ⚠️ CLASSIC IS NOT SOLD. The twelve street characters are cosmetic with neutral stats
    /// (`VISION.md` § 1) and stay free; the shop sells heroes and the tsinelas and lata sidegrades,
    /// every one of which is budget-neutral (`Roster`: "no shoe is a direct upgrade").
    /// </summary>
    public static class EconomyRules
    {
        public const string CurrencyName = "TANSAN";

        /// <summary>What a brand new or migrated wallet opens with. Enough for one item on day one.</summary>
        public const int StartingBalance = 300;

        public const int HeroPrice = 1200;
        public const int SlipperPrice = 300;
        public const int CanPrice = 250;

        /// <summary>A finished, non-AFK match pays this plus <see cref="PlacementPay"/>.</summary>
        public const int CompletionPay = 25;
        public static readonly int[] PlacementPay = { 40, 25, 15, 10 };

        /// <summary>
        /// ⚠️ THE MOST MATCHES CAN PAY IN ONE UTC DAY. A bot match is a real match (every challenge
        /// is Practice-safe by design, `AbilityVariant.PracticeSafe`), so without a ceiling the
        /// fastest way to buy the roster would be leaving a practice lobby running. Tasks are
        /// outside the cap: they are the part of the economy that asks for variety.
        /// </summary>
        public const int DailyMatchCap = 450;

        /// <summary>How many paid match ids a wallet remembers. Mirrors `APPLIED_ID_MEMORY`.</summary>
        public const int PaidIdMemory = 200;

        public const int DailyTaskCount = 3;
        public const int WeeklyTaskCount = 3;

        /// <summary>
        /// ⚠️⚠️ THE STARTER SET, ONE CONSTANT EACH SO THE OWNER CAN WIDEN THEM IN ONE LINE. Three
        /// heroes with three different jobs (DANTE holds ground, CHESKA controls lanes, SEAN hits
        /// hard), and the first four tsinelas and lata, which are the four each list shipped with
        /// before any were appended. Entry 0 of each prop list is always in, because it is what an
        /// unpicked prop wears (`CLAUDE.md` § 4).
        /// </summary>
        public static readonly string[] StarterHeroes = { "dante", "cheska", "sean" };
        public static readonly string[] StarterSlippers = { "tsinelas", "crocs", "pantulog", "sike" };
        public static readonly string[] StarterCans = { "pasip", "boyben", "decades", "metal" };

        public static string ItemId(ShopKind kind, string refId) => kind switch
        {
            ShopKind.Hero => "hero:" + refId,
            ShopKind.Slipper => "slipper:" + refId,
            ShopKind.Can => "can:" + refId,
            _ => refId,
        };

        private static List<CatalogItem> _catalog;

        /// <summary>Everything priced, in roster order: heroes, then tsinelas, then lata.</summary>
        public static IReadOnlyList<CatalogItem> Catalog
        {
            get
            {
                if (_catalog != null) return _catalog;
                var list = new List<CatalogItem>();
                foreach (var hero in Roster.HeroPeople) list.Add(new CatalogItem(ShopKind.Hero, hero.Id, HeroPrice));
                foreach (var shoe in Roster.Slippers) list.Add(new CatalogItem(ShopKind.Slipper, shoe.Id, SlipperPrice));
                foreach (var can in Roster.Cans) list.Add(new CatalogItem(ShopKind.Can, can.Id, CanPrice));
                return _catalog = list;
            }
        }

        public static CatalogItem Find(string id)
        {
            foreach (var item in Catalog) if (item.Id == id) return item;
            return null;
        }

        public static IEnumerable<string> StarterIds()
        {
            foreach (var id in StarterHeroes) yield return ItemId(ShopKind.Hero, id);
            foreach (var id in StarterSlippers) yield return ItemId(ShopKind.Slipper, id);
            foreach (var id in StarterCans) yield return ItemId(ShopKind.Can, id);
        }

        public static bool IsStarter(string id)
        {
            foreach (var s in StarterIds()) if (s == id) return true;
            return false;
        }

        /// <summary>
        /// The wallet a player gets the first time the server sees them.
        ///
        /// ⚠️⚠️ MIGRATION, NEVER RESET. Every hero and every tsinelas the SERVER'S OWN career record
        /// (`careerProfile.Characters`, `.Slippers`, `.Mastery`) shows the player has already
        /// played becomes theirs, so nobody who played before UX-1 finds a thing they used locked.
        /// The career itself is not touched.
        /// </summary>
        public static Wallet Create(PlayerProfile career, string createdUtc)
        {
            var wallet = new Wallet { Balance = StartingBalance, CreatedUtc = createdUtc ?? "" };
            foreach (var id in StarterIds()) Grant(wallet, id);

            if (career != null)
            {
                foreach (var pick in career.Characters ?? new List<PickRecord>())
                    if (pick != null && IsHero(pick.Id)) Grant(wallet, ItemId(ShopKind.Hero, pick.Id));
                foreach (var mastery in career.Mastery ?? new List<MasteryRecord>())
                    if (mastery != null && mastery.Xp > 0 && IsHero(mastery.Id))
                        Grant(wallet, ItemId(ShopKind.Hero, mastery.Id));
                foreach (var pick in career.Slippers ?? new List<PickRecord>())
                    if (pick != null && Roster.IndexIn(Roster.Slippers, pick.Id) >= 0)
                        Grant(wallet, ItemId(ShopKind.Slipper, pick.Id));
            }

            return wallet;
        }

        public static bool IsHero(string id)
        {
            foreach (var hero in Roster.HeroPeople) if (hero.Id == id) return true;
            return false;
        }

        private static void Grant(Wallet wallet, string id)
        {
            if (!wallet.Owned.Contains(id)) wallet.Owned.Add(id);
        }

        public static bool Owns(Wallet wallet, string id) =>
            IsStarter(id) || (wallet != null && wallet.Owned != null && wallet.Owned.Contains(id));

        public enum BuyResult { Bought = 0, Unknown = 1, AlreadyOwned = 2, TooPoor = 3 }

        /// <summary>The server's purchase. The price is looked up here, never supplied by a caller.</summary>
        public static BuyResult Buy(Wallet wallet, string id)
        {
            var item = Find(id);
            if (item == null) return BuyResult.Unknown;
            if (Owns(wallet, id)) return BuyResult.AlreadyOwned;
            if (wallet.Balance < item.Price) return BuyResult.TooPoor;
            wallet.Balance -= item.Price;
            wallet.Owned.Add(id);
            return BuyResult.Bought;
        }

        // ------------------------------------------------------------------ payouts

        public static int MatchPay(EarnedMatch match)
        {
            if (match.Afk) return 0;
            int pay = CompletionPay;
            if (match.Placement >= 1 && match.Placement <= PlacementPay.Length) pay += PlacementPay[match.Placement - 1];
            return pay;
        }

        /// <summary>
        /// Pay every match the wallet has not been paid for, respecting the daily cap.
        ///
        /// ⚠️ A MATCH OVER THE CAP IS STILL MARKED PAID. Otherwise it would pay tomorrow, and the cap
        /// would only ever delay money rather than limit it.
        /// </summary>
        public static int Settle(Wallet wallet, IEnumerable<EarnedMatch> matches, int today)
        {
            int paid = 0;
            if (wallet.EarnedDay != today) { wallet.EarnedDay = today; wallet.EarnedToday = 0; }

            foreach (var match in matches)
            {
                if (string.IsNullOrEmpty(match.MatchId) || wallet.PaidMatchIds.Contains(match.MatchId)) continue;
                wallet.PaidMatchIds.Add(match.MatchId);

                int pay = MatchPay(match);
                if (match.Day != today) pay = 0;
                pay = Math.Min(pay, Math.Max(0, DailyMatchCap - wallet.EarnedToday));
                wallet.EarnedToday += pay;
                wallet.Balance += pay;
                paid += pay;
            }

            while (wallet.PaidMatchIds.Count > PaidIdMemory) wallet.PaidMatchIds.RemoveAt(0);
            return paid;
        }

        // ------------------------------------------------------------------ tasks

        /// <summary>
        /// ⚠️ THE POOL IS ABOUT DOING THE GAME'S VERBS, NOT ABOUT WINNING. Most players do not win
        /// most matches, and a task list that only paid winners would pay the players who least need
        /// a reason to come back. Each row names one verb the tutorial teaches.
        /// </summary>
        public static readonly TaskDef[] DailyPool =
        {
            new TaskDef("d_play2", TaskPeriod.Daily, TaskStat.Matches, 2, 60, "Play {0} matches"),
            new TaskDef("d_knock3", TaskPeriod.Daily, TaskStat.Knockdowns, 3, 70, "Knock the lata down {0} times"),
            new TaskDef("d_fetch4", TaskPeriod.Daily, TaskStat.Retrievals, 4, 60, "Get your tsinelas back {0} times"),
            new TaskDef("d_tag2", TaskPeriod.Daily, TaskStat.Tags, 2, 70, "Tag {0} attackers as the taya"),
            new TaskDef("d_throw12", TaskPeriod.Daily, TaskStat.Throws, 12, 50, "Throw {0} times"),
            new TaskDef("d_hero1", TaskPeriod.Daily, TaskStat.HeroStrikeMatches, 1, 50, "Play {0} Hero Strike match"),
            new TaskDef("d_classic1", TaskPeriod.Daily, TaskStat.ClassicMatches, 1, 50, "Play {0} Classic match"),
            new TaskDef("d_podium1", TaskPeriod.Daily, TaskStat.Podiums, 1, 80, "Finish top two {0} time"),
        };

        public static readonly TaskDef[] WeeklyPool =
        {
            new TaskDef("w_play10", TaskPeriod.Weekly, TaskStat.Matches, 10, 300, "Play {0} matches"),
            new TaskDef("w_win3", TaskPeriod.Weekly, TaskStat.Wins, 3, 400, "Win {0} matches"),
            new TaskDef("w_pressure5", TaskPeriod.Weekly, TaskStat.PressureRetrievals, 5, 350, "Retrieve under pressure {0} times"),
            new TaskDef("w_knock15", TaskPeriod.Weekly, TaskStat.Knockdowns, 15, 350, "Knock the lata down {0} times"),
            new TaskDef("w_tag10", TaskPeriod.Weekly, TaskStat.Tags, 10, 350, "Tag {0} attackers as the taya"),
        };

        /// <summary>Days since 1970-01-01 UTC.</summary>
        public static int DayOf(DateTime utc) => (int)Math.Floor((utc - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalDays);

        /// <summary>Weeks since the Monday before 1970-01-01. Day 0 was a Thursday, so +3 lands Monday.</summary>
        public static int WeekOf(int day) => (int)Math.Floor((day + 3) / 7.0);

        public static int PeriodIndex(TaskPeriod period, int day) => period == TaskPeriod.Daily ? day : WeekOf(day);

        /// <summary>
        /// Which tasks this player has in this period.
        ///
        /// ⚠️ DETERMINISTIC FROM THE PLAYER ID AND THE PERIOD, SO THERE IS NOTHING TO STORE. The
        /// server and every client derive the same three from the same two facts, and a player
        /// cannot reroll by reinstalling. FNV-1a over UTF-16 units, the same hash `player-account.js`
        /// uses for the tag, so the JavaScript mirror walks the string the same way.
        /// </summary>
        public static List<TaskDef> TasksFor(string playerId, TaskPeriod period, int day)
        {
            var pool = period == TaskPeriod.Daily ? DailyPool : WeeklyPool;
            int count = period == TaskPeriod.Daily ? DailyTaskCount : WeeklyTaskCount;
            uint seed = Fnv(playerId + "|" + (int)period + "|" + PeriodIndex(period, day));

            var order = new List<int>();
            for (int i = 0; i < pool.Length; i++) order.Add(i);
            // Fisher-Yates driven by an xorshift of the seed.
            uint state = seed == 0 ? 2463534242u : seed;
            for (int i = order.Count - 1; i > 0; i--)
            {
                state ^= state << 13; state ^= state >> 17; state ^= state << 5;
                int j = (int)(state % (uint)(i + 1));
                (order[i], order[j]) = (order[j], order[i]);
            }

            var chosen = new List<TaskDef>();
            for (int i = 0; i < Math.Min(count, order.Count); i++) chosen.Add(pool[order[i]]);
            return chosen;
        }

        public static uint Fnv(string text)
        {
            uint hash = 2166136261u;
            foreach (char c in text ?? "")
            {
                hash ^= c;
                hash = unchecked(hash * 16777619u);
            }
            return hash;
        }

        public static int Count(TaskStat stat, EarnedMatch m)
        {
            if (m.Afk) return 0;
            return stat switch
            {
                TaskStat.Matches => 1,
                TaskStat.Wins => m.Placement == 1 ? 1 : 0,
                TaskStat.Podiums => m.Placement >= 1 && m.Placement <= 2 ? 1 : 0,
                TaskStat.Knockdowns => m.Knockdowns,
                TaskStat.Retrievals => m.Retrievals,
                TaskStat.PressureRetrievals => m.PressureRetrievals,
                TaskStat.Tags => m.Tags,
                TaskStat.Throws => m.Throws,
                TaskStat.HeroStrikeMatches => m.Mode == "HeroStrike" ? 1 : 0,
                TaskStat.ClassicMatches => m.Mode == "Classic" ? 1 : 0,
                _ => 0,
            };
        }

        /// <summary>Progress on a task over the matches in its period, capped at the target.</summary>
        public static int Progress(TaskDef task, IEnumerable<EarnedMatch> matches, int today)
        {
            int period = PeriodIndex(task.Period, today);
            int total = 0;
            foreach (var m in matches)
                if (PeriodIndex(task.Period, m.Day) == period) total += Count(task.Stat, m);
            return Math.Min(total, task.Target);
        }

        public static string ClaimKey(TaskDef task, int today) =>
            (task.Period == TaskPeriod.Daily ? "d" : "w") + ":" + PeriodIndex(task.Period, today) + ":" + task.Id;

        public enum ClaimResult { Claimed = 0, NotAssigned = 1, Unfinished = 2, AlreadyClaimed = 3 }

        /// <summary>
        /// The server's claim. The task must be one of THIS player's tasks for THIS period, finished
        /// by server-held matches, and unclaimed.
        /// </summary>
        public static ClaimResult Claim(Wallet wallet, string playerId, string taskId,
                                        IEnumerable<EarnedMatch> matches, int today)
        {
            TaskDef task = null;
            foreach (var period in new[] { TaskPeriod.Daily, TaskPeriod.Weekly })
                foreach (var t in TasksFor(playerId, period, today))
                    if (t.Id == taskId) task = t;
            if (task == null) return ClaimResult.NotAssigned;

            string key = ClaimKey(task, today);
            if (wallet.Claimed.Contains(key)) return ClaimResult.AlreadyClaimed;
            if (Progress(task, matches, today) < task.Target) return ClaimResult.Unfinished;

            wallet.Claimed.Add(key);
            while (wallet.Claimed.Count > 64) wallet.Claimed.RemoveAt(0);
            wallet.Balance += task.Reward;
            return ClaimResult.Claimed;
        }

        /// <summary>
        /// The facts a history record contributes, for the player's own line.
        /// ⚠️ Returns false for a record this player has no human line in.
        /// </summary>
        public static bool TryRead(MatchRecord record, string playerId, out EarnedMatch earned)
        {
            earned = default;
            if (record == null || string.IsNullOrEmpty(record.MatchId)) return false;
            var line = MatchRecordRules.LineFor(record, playerId);
            if (line == null || line.IsBot) return false;

            DateTime played;
            if (!DateTime.TryParse(record.PlayedUtc, System.Globalization.CultureInfo.InvariantCulture,
                                   System.Globalization.DateTimeStyles.AdjustToUniversal |
                                   System.Globalization.DateTimeStyles.AssumeUniversal, out played))
                return false;

            earned = new EarnedMatch
            {
                MatchId = record.MatchId,
                Day = DayOf(played),
                Mode = record.Mode ?? "",
                Placement = line.Placement,
                Afk = ProgressionRules.WasAfk(record, line),
                Throws = line.Throws,
                Knockdowns = line.Knockdowns,
                Retrievals = line.Retrievals,
                PressureRetrievals = line.RetrievalsUnderPressure,
                Tags = line.Tags,
            };
            return true;
        }
    }
}
