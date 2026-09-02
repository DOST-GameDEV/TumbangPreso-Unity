using System.Collections.Generic;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// The telemetry contract. `docs/TODO.md` § 90.3.
    ///
    /// ⚠️ THE POINT OF TESTING A NAMING RULE IS THAT NOTHING ELSE EVER WILL. A renamed event
    /// compiles, runs, and produces a counter that starts at zero beside a year of data it can
    /// no longer be joined to. There is no runtime symptom at all.
    /// </summary>
    public sealed class TelemetryTests
    {
        [Fact]
        public void EveryFunnelStepIsAlsoAKnownEvent()
        {
            foreach (string step in TelemetryEvents.Funnel)
                Assert.True(TelemetryRules.IsKnownEvent(step), step + " is not in TelemetryEvents.All");
        }

        [Fact]
        public void AnUnknownEventNameIsRefusedRatherThanSentAsANewSeries()
        {
            Assert.False(TelemetryRules.IsKnownEvent("match_start"));
            Assert.False(TelemetryRules.IsKnownEvent(""));
            Assert.False(TelemetryRules.Accept(new TelemetryEvent { Name = "match_start" }));
        }

        /// <summary>
        /// ⚠️ THE ORDER IS THE MEANING, so this pins it. Inserting a step in the middle silently
        /// rewrites what every already-stored funnel position claims: a player recorded at index
        /// 3 becomes a player who reached a step that did not exist when they played.
        /// </summary>
        [Fact]
        public void TheFunnelIsTheSixStepsInThisOrder()
        {
            Assert.Equal(
                new[]
                {
                    "first_launch", "first_sign_in", "first_menu",
                    "first_queue", "first_match_started", "first_match_finished",
                },
                TelemetryEvents.Funnel);
        }

        [Fact]
        public void TheFunnelOnlyEverGoesForward()
        {
            int reached = TelemetryRules.FurthestFunnelStep(-1, TelemetryEvents.FirstLaunch);
            Assert.Equal(0, reached);

            reached = TelemetryRules.FurthestFunnelStep(reached, TelemetryEvents.FirstMatchFinished);
            Assert.Equal(5, reached);

            // Reaching the menu again after finishing a match must not walk the funnel back.
            reached = TelemetryRules.FurthestFunnelStep(reached, TelemetryEvents.FirstMenu);
            Assert.Equal(5, reached);
        }

        [Fact]
        public void AFunnelStepIsNewOnlyOnce()
        {
            Assert.True(TelemetryRules.IsNewFunnelStep(-1, TelemetryEvents.FirstLaunch));
            Assert.False(TelemetryRules.IsNewFunnelStep(0, TelemetryEvents.FirstLaunch));
            Assert.False(TelemetryRules.IsNewFunnelStep(3, TelemetryEvents.FirstMenu));
            Assert.False(TelemetryRules.IsNewFunnelStep(0, TelemetryEvents.SessionStart));
        }

        /// <summary>
        /// ⚠️⚠️ `FUTURE.md` § 19.3: *"No personally identifying field in any event, ever."* This
        /// is the client half of that rule and `telemetry.js` is the other, because the client is
        /// the half somebody can edit. The check is on the field NAME rather than the value,
        /// because a value cannot be inspected for whether it identifies somebody and a name can.
        /// </summary>
        [Theory]
        [InlineData("player_name")]
        [InlineData("email")]
        [InlineData("account")]
        [InlineData("handle")]
        [InlineData("session_id")]
        [InlineData("auth_token")]
        [InlineData("ip")]
        [InlineData("device")]
        [InlineData("save_path")]
        public void AParameterNamedAfterAPersonIsRefused(string key)
            => Assert.False(TelemetryRules.IsSafeParameterName(key));

        /// <summary>
        /// ⚠️⚠️ `slipper` AND `character` ARE IN HERE BECAUSE THE FIRST VERSION OF THE RULE
        /// REFUSED ONE OF THEM. `ip` was matched as a SUBSTRING, and `slipper` contains it, so
        /// the tsinelas pick rate that `FUTURE.md` § 3 asks for by name would have been stripped
        /// from every event with nothing anywhere saying so. Short words are matched whole or as
        /// a suffix now; a two-letter substring rule refuses words it has never heard of.
        /// </summary>
        [Theory]
        [InlineData("mode")]
        [InlineData("map")]
        [InlineData("rounds")]
        [InlineData("seconds")]
        [InlineData("placement")]
        [InlineData("gpu")]
        [InlineData("slipper")]
        [InlineData("character")]
        [InlineData("online")]
        [InlineData("seats")]
        [InlineData("bots")]
        [InlineData("round")]
        [InlineData("reason")]
        [InlineData("cores")]
        [InlineData("ram_gb")]
        [InlineData("screen_w")]
        [InlineData("screen_h")]
        public void AnOrdinaryBucketNameIsAllowed(string key)
            => Assert.True(TelemetryRules.IsSafeParameterName(key));

        /// <summary>
        /// ⚠️ EVERY COLUMN THE GAME ACTUALLY SENDS, CHECKED IN ONE PLACE. A refused parameter
        /// costs a column and logs nothing, so the only way this is ever noticed is a test that
        /// names the real call sites. If `TelemetrySink` gains a column, it goes here too.
        /// </summary>
        [Fact]
        public void EveryColumnTheGameSendsSurvivesTheRules()
        {
            string[] columns =
            {
                "mode", "map", "seats", "bots", "rounds", "seconds", "placement",
                "character", "slipper", "round", "reason", "online",
                "gpu", "cores", "ram_gb", "screen_w", "screen_h",

                // ⚠️ `frames` PASSES BY ONE LETTER: the refused-fragment list holds `name`, which
                // it does not contain. `frame_name` or `named_frames` would be stripped in
                // silence, costing the column that says how big the frame-rate sample was.
                "band", "fps_avg", "fps_p50", "fps_p5", "fps_p1", "frames",
            };

            foreach (string column in columns)
                Assert.True(TelemetryRules.IsSafeParameterName(column), column + " would be stripped");
        }

        /// <summary>
        /// ⚠️ A LABEL IS REFUSED RATHER THAN TRUNCATED, unlike a display name. A clipped display
        /// name is still that person's name; a clipped label is a new value that will never join
        /// to the one it came from, which is the same broken history a renamed event produces.
        /// </summary>
        [Fact]
        public void AFreeTextValueIsRefusedRatherThanTrimmedIntoABucket()
        {
            Assert.Equal("", TelemetryRules.Label("something a person typed"));
            Assert.Equal("", TelemetryRules.Label(new string('a', TelemetryRules.MaxParameterLength + 1)));
            Assert.Equal("hero_strike", TelemetryRules.Label("hero_strike"));
            Assert.Equal("Radeon.RX-6600", TelemetryRules.Label("Radeon.RX-6600"));
        }

        [Fact]
        public void AnEventKeepsItsSafeColumnsAndLosesTheRest()
        {
            var candidate = new TelemetryEvent { Name = TelemetryEvents.MatchFinished };
            candidate.Labels["mode"] = "Classic";
            candidate.Labels["player_name"] = "Maria Clara";
            candidate.Labels["note"] = "something typed here";
            candidate.Numbers["rounds"] = 4;

            Assert.True(TelemetryRules.Accept(candidate));
            Assert.True(candidate.Labels.ContainsKey("mode"));
            Assert.False(candidate.Labels.ContainsKey("player_name"));
            Assert.False(candidate.Labels.ContainsKey("note"));
            Assert.True(candidate.Numbers.ContainsKey("rounds"));
        }

        /// <summary>
        /// ⚠️⚠️ THIS IS WHY TELEMETRY DOES NOT COST ONE CALL PER EVENT. `FUTURE.md` § 0.3:
        /// *"Call it once per match, never per event."* A Hero Strike match carries nine hundred
        /// passive-defence ticks, and folding is what turns them into one row with a count.
        /// </summary>
        [Fact]
        public void IdenticalEventsFoldIntoOneCountedRow()
        {
            var buffer = new Dictionary<string, TelemetryEvent>();

            for (int i = 0; i < 900; i++)
            {
                var candidate = new TelemetryEvent { Name = TelemetryEvents.Pick };
                candidate.Labels["mode"] = "Classic";
                Assert.True(TelemetryRules.Fold(buffer, candidate));
            }

            Assert.Single(buffer);
            foreach (var entry in buffer.Values) Assert.Equal(900, entry.Count);
        }

        /// <summary>
        /// ⚠️ A DIFFERENT NUMBER IS A DIFFERENT ROW. Folding a 340 second match into a 512 second
        /// one would produce a count of two carrying one of the two durations, and the server
        /// averages what it is sent.
        /// </summary>
        [Fact]
        public void EventsThatDifferInAnyColumnStaySeparateRows()
        {
            var buffer = new Dictionary<string, TelemetryEvent>();

            var first = new TelemetryEvent { Name = TelemetryEvents.MatchFinished };
            first.Numbers["seconds"] = 340;
            var second = new TelemetryEvent { Name = TelemetryEvents.MatchFinished };
            second.Numbers["seconds"] = 512;

            TelemetryRules.Fold(buffer, first);
            TelemetryRules.Fold(buffer, second);

            Assert.Equal(2, buffer.Count);
        }

        /// <summary>
        /// ⚠️ A SESSION LEFT RUNNING OVERNIGHT MUST NOT GROW A LIST UNTIL THE PROCESS DIES. Past
        /// the cap, counts on rows already in the buffer keep rising and only new SHAPES are
        /// dropped, so everything that was already interesting stays correct.
        /// </summary>
        [Fact]
        public void TheSessionBufferIsBoundedAndKeepsCountingWhatItAlreadyHolds()
        {
            var buffer = new Dictionary<string, TelemetryEvent>();

            for (int i = 0; i < TelemetryRules.MaxEventsPerBatch + 20; i++)
            {
                var candidate = new TelemetryEvent { Name = TelemetryEvents.MatchFinished };
                candidate.Numbers["seconds"] = i;
                TelemetryRules.Fold(buffer, candidate);
            }

            Assert.Equal(TelemetryRules.MaxEventsPerBatch, buffer.Count);

            var repeat = new TelemetryEvent { Name = TelemetryEvents.MatchFinished };
            repeat.Numbers["seconds"] = 0;
            Assert.True(TelemetryRules.Fold(buffer, repeat));
            Assert.Equal(TelemetryRules.MaxEventsPerBatch, buffer.Count);
        }
    }
}
