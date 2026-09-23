using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// TANSAN, the shop, the starter set, migration, payouts and tasks. `docs/TODO.md` UX-1.8 and
    /// UX-1.9, `docs/reports/front-end-flow-2026-09-23/ux1-plan.md` § 6.
    /// </summary>
    public sealed class EconomyTests
    {
        private static EarnedMatch Played(string id, int day, int placement = 2, bool afk = false,
                                          string mode = "HeroStrike", int knocks = 1, int tags = 0)
            => new EarnedMatch
            {
                MatchId = id, Day = day, Placement = placement, Afk = afk, Mode = mode,
                Knockdowns = knocks, Tags = tags, Throws = 5, Retrievals = 2, PressureRetrievals = 1,
            };

        [Fact]
        public void EveryHeroAndPropIsPricedAndNoClassicPersonIs()
        {
            foreach (var hero in Roster.HeroPeople)
                Assert.Equal(EconomyRules.HeroPrice, EconomyRules.Find("hero:" + hero.Id).Price);
            foreach (var shoe in Roster.Slippers) Assert.NotNull(EconomyRules.Find("slipper:" + shoe.Id));
            foreach (var can in Roster.Cans) Assert.NotNull(EconomyRules.Find("can:" + can.Id));

            // VISION § 1: Classic people are cosmetic and neutral, and they are not sold.
            foreach (var person in Roster.ClassicPeople)
                Assert.Null(EconomyRules.Find("hero:" + person.Id));
        }

        [Fact]
        public void TheStarterSetIsRealRosterEntriesAndIncludesEntryZero()
        {
            foreach (var id in EconomyRules.StarterHeroes) Assert.True(EconomyRules.IsHero(id), id);
            foreach (var id in EconomyRules.StarterSlippers) Assert.True(Roster.IndexIn(Roster.Slippers, id) >= 0, id);
            foreach (var id in EconomyRules.StarterCans) Assert.True(Roster.IndexIn(Roster.Cans, id) >= 0, id);

            // CLAUDE.md § 4: entry 0 of each prop list is what an unpicked prop wears.
            Assert.Contains(Roster.Slippers[0].Id, EconomyRules.StarterSlippers);
            Assert.Contains(Roster.Cans[0].Id, EconomyRules.StarterCans);

            // And the shop still has something to sell in every aisle.
            Assert.True(Roster.HeroPeople.Count > EconomyRules.StarterHeroes.Length);
            Assert.True(Roster.Slippers.Count > EconomyRules.StarterSlippers.Length);
            Assert.True(Roster.Cans.Count > EconomyRules.StarterCans.Length);
        }

        [Fact]
        public void MigrationKeepsEveryHeroAndShoeTheCareerAlreadyPlayed()
        {
            var career = new PlayerProfile();
            career.Characters.Add(new PickRecord { Id = "zack", Games = 3 });
            career.Characters.Add(new PickRecord { Id = "maring", Games = 9 });   // Classic: never sold
            career.Mastery.Add(new MasteryRecord { Id = "nemu", Xp = 40 });
            career.Mastery.Add(new MasteryRecord { Id = "phaister", Xp = 0 });    // never played
            career.Slippers.Add(new PickRecord { Id = "heels", Games = 1 });

            var wallet = EconomyRules.Create(career, "2026-09-23T00:00:00Z");

            Assert.Equal(EconomyRules.StartingBalance, wallet.Balance);
            Assert.True(EconomyRules.Owns(wallet, "hero:zack"));
            Assert.True(EconomyRules.Owns(wallet, "hero:nemu"));
            Assert.False(EconomyRules.Owns(wallet, "hero:phaister"));
            Assert.True(EconomyRules.Owns(wallet, "slipper:heels"));
            Assert.DoesNotContain("hero:maring", wallet.Owned);
            foreach (var id in EconomyRules.StarterIds()) Assert.True(EconomyRules.Owns(wallet, id), id);
        }

        [Fact]
        public void BuyingChargesTheServersPriceOnceAndRefusesTheRest()
        {
            var wallet = EconomyRules.Create(null, "");
            wallet.Balance = EconomyRules.HeroPrice + 10;

            Assert.Equal(EconomyRules.BuyResult.Unknown, EconomyRules.Buy(wallet, "hero:maring"));
            Assert.Equal(EconomyRules.BuyResult.AlreadyOwned, EconomyRules.Buy(wallet, "hero:dante"));
            Assert.Equal(EconomyRules.BuyResult.Bought, EconomyRules.Buy(wallet, "hero:zack"));
            Assert.Equal(10, wallet.Balance);
            Assert.Equal(EconomyRules.BuyResult.AlreadyOwned, EconomyRules.Buy(wallet, "hero:zack"));
            Assert.Equal(EconomyRules.BuyResult.TooPoor, EconomyRules.Buy(wallet, "slipper:heels"));
            Assert.Equal(10, wallet.Balance);
        }

        [Fact]
        public void AMatchPaysOnceAndAnAfkMatchPaysNothing()
        {
            var wallet = EconomyRules.Create(null, "");
            int start = wallet.Balance;
            var matches = new[] { Played("a", 100, placement: 1), Played("b", 100, afk: true) };

            int paid = EconomyRules.Settle(wallet, matches, 100);
            Assert.Equal(EconomyRules.CompletionPay + EconomyRules.PlacementPay[0], paid);
            Assert.Equal(start + paid, wallet.Balance);

            // Settling the same history again pays nothing: the ids are remembered.
            Assert.Equal(0, EconomyRules.Settle(wallet, matches, 100));
        }

        [Fact]
        public void TheDailyCapLimitsMoneyRatherThanDelayingIt()
        {
            var wallet = EconomyRules.Create(null, "");
            var today = Enumerable.Range(0, 40).Select(i => Played("m" + i, 100, placement: 1)).ToList();
            int paid = EconomyRules.Settle(wallet, today, 100);
            Assert.Equal(EconomyRules.DailyMatchCap, paid);

            // Tomorrow the over-cap matches do not pay late: they were marked paid.
            Assert.Equal(0, EconomyRules.Settle(wallet, today, 101));

            // A match from an earlier day that arrives late is recorded but not paid.
            Assert.Equal(0, EconomyRules.Settle(wallet, new[] { Played("late", 99) }, 101));
            Assert.Contains("late", wallet.PaidMatchIds);
        }

        [Fact]
        public void TasksAreDeterministicDistinctAndChangeWithThePeriod()
        {
            var a = EconomyRules.TasksFor("player-1", TaskPeriod.Daily, 20000).Select(t => t.Id).ToList();
            var again = EconomyRules.TasksFor("player-1", TaskPeriod.Daily, 20000).Select(t => t.Id).ToList();
            Assert.Equal(a, again);
            Assert.Equal(EconomyRules.DailyTaskCount, a.Distinct().Count());

            // Over a fortnight a player sees more than one set.
            var sets = Enumerable.Range(20000, 14)
                .Select(d => string.Join(",", EconomyRules.TasksFor("player-1", TaskPeriod.Daily, d).Select(t => t.Id)))
                .Distinct().Count();
            Assert.True(sets > 1);

            // A weekly set is the same all week.
            int monday = 20000 - ((20000 + 3) % 7);
            var week = EconomyRules.TasksFor("p", TaskPeriod.Weekly, monday).Select(t => t.Id).ToList();
            for (int d = monday; d < monday + 7; d++)
                Assert.Equal(week, EconomyRules.TasksFor("p", TaskPeriod.Weekly, d).Select(t => t.Id).ToList());
        }

        [Fact]
        public void AClaimNeedsAFinishedAssignedUnclaimedTask()
        {
            const int day = 20000;
            var task = EconomyRules.TasksFor("p", TaskPeriod.Daily, day)[0];
            var wallet = EconomyRules.Create(null, "");

            Assert.Equal(EconomyRules.ClaimResult.NotAssigned,
                         EconomyRules.Claim(wallet, "p", "not-a-task", new EarnedMatch[0], day));
            Assert.Equal(EconomyRules.ClaimResult.Unfinished,
                         EconomyRules.Claim(wallet, "p", task.Id, new EarnedMatch[0], day));

            // Enough matches of every kind to finish any daily task.
            var matches = Enumerable.Range(0, 20)
                .Select(i => Played("x" + i, day, placement: 1, mode: i % 2 == 0 ? "Classic" : "HeroStrike", knocks: 3, tags: 3))
                .ToList();
            int before = wallet.Balance;
            Assert.Equal(EconomyRules.ClaimResult.Claimed, EconomyRules.Claim(wallet, "p", task.Id, matches, day));
            Assert.Equal(before + task.Reward, wallet.Balance);
            Assert.Equal(EconomyRules.ClaimResult.AlreadyClaimed, EconomyRules.Claim(wallet, "p", task.Id, matches, day));

            // Yesterday's matches do not finish today's task.
            var stale = matches.Select(m => { m.Day = day - 1; return m; }).ToList();
            Assert.Equal(0, EconomyRules.Progress(task, stale, day));
        }

        /// <summary>
        /// ⚠️ GOLDEN VALUES SHARED WITH `tools/test_wallet_script.js`, which asserts the same six ids
        /// from `wallet.js`. Both copies agreeing on these is what proves they pick the same tasks.
        /// </summary>
        [Fact]
        public void TheTaskPickMatchesTheServerScriptsGoldenValues()
        {
            Assert.Equal("d_tag2,d_classic1,d_fetch4",
                         string.Join(",", EconomyRules.TasksFor("player-1", TaskPeriod.Daily, 20000).Select(t => t.Id)));
            Assert.Equal("w_play10,w_pressure5,w_win3",
                         string.Join(",", EconomyRules.TasksFor("p", TaskPeriod.Weekly, 20000).Select(t => t.Id)));
        }

        [Fact]
        public void TheWeekStartsOnMonday()
        {
            // 1970-01-05 was a Monday: day 4.
            Assert.Equal(EconomyRules.WeekOf(4), EconomyRules.WeekOf(10));
            Assert.NotEqual(EconomyRules.WeekOf(3), EconomyRules.WeekOf(4));
        }

        /// <summary>
        /// ⚠️⚠️ THE SERVER COPY MUST STATE THE SAME NUMBERS. `wallet.js` cannot import this file, so
        /// the test reads the script as text and checks every mirrored constant, which is how
        /// `CareerAndCloudCodeTests` keeps `player-account.js` honest about the name length.
        /// </summary>
        [Fact]
        public void TheCloudScriptMirrorsEveryConstant()
        {
            string script = File.ReadAllText(Path.Combine(RepoRoot(), "ugs", "cloud-code", "wallet.js"));

            void Const(string name, int value) =>
                Assert.Matches(new Regex(@"const " + name + @"\s*=\s*" + value + ";"), script);

            Const("STARTING_BALANCE", EconomyRules.StartingBalance);
            Const("HERO_PRICE", EconomyRules.HeroPrice);
            Const("SLIPPER_PRICE", EconomyRules.SlipperPrice);
            Const("CAN_PRICE", EconomyRules.CanPrice);
            Const("COMPLETION_PAY", EconomyRules.CompletionPay);
            Const("DAILY_MATCH_CAP", EconomyRules.DailyMatchCap);
            Const("PAID_ID_MEMORY", EconomyRules.PaidIdMemory);
            Const("DAILY_TASK_COUNT", EconomyRules.DailyTaskCount);
            Const("WEEKLY_TASK_COUNT", EconomyRules.WeeklyTaskCount);

            Assert.Contains("[" + string.Join(", ", EconomyRules.PlacementPay) + "]", script);
            foreach (var id in EconomyRules.StarterHeroes.Concat(EconomyRules.StarterSlippers).Concat(EconomyRules.StarterCans))
                Assert.Contains("\"" + id + "\"", script);
            foreach (var hero in Roster.HeroPeople) Assert.Contains("\"" + hero.Id + "\"", script);
            foreach (var shoe in Roster.Slippers) Assert.Contains("\"" + shoe.Id + "\"", script);
            foreach (var can in Roster.Cans) Assert.Contains("\"" + can.Id + "\"", script);
            foreach (var task in EconomyRules.DailyPool.Concat(EconomyRules.WeeklyPool))
                Assert.Contains("\"" + task.Id + "\"", script);

            // Cloud Code strips undeclared parameters silently (docs/TODO.md § 90.5).
            Assert.Matches(new Regex(@"module\.exports\.params\s*=\s*\{[^}]*action:\s*""String""[^}]*item:\s*""String""[^}]*task:\s*""String""", RegexOptions.Singleline), script);
        }

        private static string RepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "AGENTS.md"))) dir = dir.Parent;
            return dir?.FullName ?? throw new InvalidOperationException("repo root not found");
        }
    }
}
