using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// The two things about the career layer that no runtime test can see: that the Cloud Code
    /// request exists in exactly one place, and that the server script still agrees with the core
    /// about the numbers it had to be told twice.
    ///
    /// ⚠️⚠️ THESE READ THE SOURCE AS TEXT ON PURPOSE, like `SceneScriptCheck` and the three
    /// `tools/` audits. Both faults below are invisible to every other test in the repository:
    /// the first because a second copy of a working request still works until it drifts, and the
    /// second because the JavaScript is never compiled by anything on this machine.
    /// </summary>
    public class CareerAndCloudCodeTests
    {
        private const string AssetsRoot = "Assets/TumbangPreso";
        private const string CloudCodeRoot = "ugs/cloud-code";
        /// <summary>
        /// ⚠️⚠️ SPLIT ACROSS THE `+` ON PURPOSE, AND DO NOT "TIDY" IT BACK INTO ONE STRING.
        /// The test below searches every `.cs` file under `Assets` for this host name, and
        /// this file is one of them: written as a single literal, the audit reports itself as
        /// the offender on every run. The alternative is a skip list keyed on this file name,
        /// which would also hide a real second request if one were ever added HERE. The
        /// compiler folds these two halves into the same constant.
        /// </summary>
        private const string Endpoint = "cloud-code" + ".services.api.unity.com";

        /// <summary>
        /// ⚠️⚠️ THIS IS `docs/TODO.md` § 89.5 AS A GATE, AND § 88.4 IS THE SESSION THAT PAID FOR
        /// IT. That entry kept a hand-written duplicate of `PlayerAccount.CallCloudAsync` inside
        /// `UgsServicesProbe` and wrote down what it would cost: *"if the call shape drifts, the
        /// probe passes while the game fails, which is the worst outcome available."* Phase 2
        /// needed a third copy, so the request moved into `Net.CloudCode` and everything calls
        /// that. A comment asking the next person not to write a fourth is what § 88.4 already
        /// tried; this fails instead.
        /// </summary>
        [Test]
        public void EveryCloudCodeRequestGoesThroughTheOneHelper()
        {
            var offenders = new System.Collections.Generic.List<string>();

            foreach (string path in Directory.GetFiles(AssetsRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(path) == "CloudCode.cs") continue;
                if (!File.ReadAllText(path).Contains(Endpoint)) continue;
                offenders.Add(path.Replace('\\', '/'));
            }

            Assert.IsEmpty(offenders,
                "a second Cloud Code request has been written by hand. Call `Net.CloudCode.CallAsync` " +
                "instead: docs/TODO.md § 89.5 records what the last duplicate cost. Offenders: " +
                string.Join(", ", offenders));
        }

        /// <summary>
        /// ⚠️⚠️ `ugs/cloud-code/match-record.js` IS `ProfileRules` WRITTEN AGAIN AND THE SERVER IS
        /// THE AUTHORITY, so a constant that drifts there is a career that silently changes the
        /// moment a player comes back online. `docs/TODO.md` § 89.6. This cannot check the whole
        /// file, but the numbers are the part most likely to be edited on one side only.
        /// </summary>
        [Test]
        public void TheCareerScriptStillAgreesWithTheCoreAboutItsNumbers()
        {
            string js = File.ReadAllText(Path.Combine(CloudCodeRoot, "match-record.js"));

            Assert.AreEqual(Balance.PlayerCount, ConstantIn(js, "PLAYER_COUNT"),
                "PLAYER_COUNT in match-record.js no longer matches Balance.PlayerCount");
            Assert.AreEqual(ProfileRules.HistoryLimit, ConstantIn(js, "HISTORY_LIMIT"),
                "HISTORY_LIMIT in match-record.js no longer matches ProfileRules.HistoryLimit");
            Assert.AreEqual(ProfileRules.AppliedIdMemory, ConstantIn(js, "APPLIED_ID_MEMORY"),
                "APPLIED_ID_MEMORY in match-record.js no longer matches ProfileRules.AppliedIdMemory. " +
                "It has to stay larger than HISTORY_LIMIT or a record just rolled into the totals " +
                "becomes resubmittable.");
        }

        /// <summary>
        /// ⚠️⚠️ THIS IS § 88.1a, WHICH WAS ONLY A COMMENT UNTIL NOW. The name limit read 16 in the
        /// validator while `Balance.PlayerNameMax` read 14, and the server is the authority: it
        /// would have stored a 15-character name that every client then silently clipped, so the
        /// profile and the scoreboard stopped being the same string. The C# half of that has had
        /// `TheAccountNameLimitIsTheOneTheWireAndTheHudUse` since; the JavaScript half had a
        /// paragraph asking somebody to remember.
        /// </summary>
        [Test]
        public void TheAccountScriptStillAgreesWithTheCoreAboutTheNameLimit()
        {
            string js = File.ReadAllText(Path.Combine(CloudCodeRoot, "player-account.js"));

            Assert.AreEqual(AccountRules.DisplayNameMax, ConstantIn(js, "DISPLAY_NAME_MAX"),
                "DISPLAY_NAME_MAX in player-account.js no longer matches AccountRules.DisplayNameMax, " +
                "which is Balance.PlayerNameMax. docs/TODO.md § 88.1a is what that cost last time.");
            Assert.AreEqual(AccountRules.DisplayNameMin, ConstantIn(js, "DISPLAY_NAME_MIN"),
                "DISPLAY_NAME_MIN in player-account.js no longer matches AccountRules.DisplayNameMin");

            Assert.AreEqual(AccountRules.HandleProofMinutes, ConstantIn(js, "PROOF_MINUTES"),
                "PROOF_MINUTES in player-account.js no longer matches AccountRules.HandleProofMinutes. " +
                "The client re-mints with a minute to spare against this number; if the server's " +
                "is shorter, every honest peer arrives with an expired proof and is demoted.");
        }

        /// <summary>
        /// ⚠️⚠️ THE IMPERSONATION GUARD RESTS ENTIRELY ON THE TWO COPIES OF FNV-1a AGREEING, AND
        /// NOTHING ELSE HERE CAN SEE THAT THEY DO. `docs/TODO.md` § 88.1c and § 90.1: the tag is
        /// derived from the player id in `AccountRules.DerivedTag` and again in the script's
        /// `derivedTag`, and the server is the authority. If they split, the endpoint vouches for
        /// a handle no client will ever compute and every online lobby demotes everybody.
        ///
        /// ⚠️ THE NUMERIC AGREEMENT IS ASSERTED IN `Core.Tests`
        /// (`TheDerivedTagMatchesTheServerScriptsCopyOfTheSameHash`), against vectors produced by
        /// running the JavaScript. This checks the cheaper half: that the script still contains
        /// the hash at all rather than having quietly gone back to trusting the caller.
        /// </summary>
        [Test]
        public void TheAccountScriptStillDerivesTheTagRatherThanTrustingTheClaim()
        {
            string js = File.ReadAllText(Path.Combine(CloudCodeRoot, "player-account.js"));

            StringAssert.Contains("2166136261", js, "the FNV-1a offset basis is gone from player-account.js");
            StringAssert.Contains("16777619", js, "the FNV-1a prime is gone from player-account.js");
            StringAssert.Contains("Discriminator: derivedTag(playerId)", js,
                "player-account.js is storing a discriminator that did not come from the player " +
                "id. A client that can write its own tag can write somebody else's, attest to " +
                "it, and the whole guard proves a lie for them. docs/TODO.md § 88.1c.");
        }

        /// <summary>
        /// ⚠️⚠️ A RENAMED EVENT IS A BROKEN HISTORY, `FUTURE.md` § 19.3, AND THIS IS THE ONLY
        /// THING THAT CAN CATCH ONE. The server refuses a name it does not know, so a name added
        /// on one side and not the other is not an error anywhere: it is an event the client
        /// counts, sends, and has silently discarded, forever, with a `refused` number nobody is
        /// reading. `docs/TODO.md` § 90.3 is the contract in prose.
        /// </summary>
        [Test]
        public void TheTelemetryScriptKnowsExactlyTheEventsTheCoreCanSend()
        {
            string js = File.ReadAllText(Path.Combine(CloudCodeRoot, "telemetry.js"));

            var funnel = StringArrayIn(js, "FUNNEL");
            CollectionAssert.AreEqual(TelemetryEvents.Funnel, funnel,
                "FUNNEL in telemetry.js no longer matches TelemetryEvents.Funnel, in content or " +
                "in ORDER. The order is the meaning: a funnel position is an index, so reordering " +
                "rewrites what every stored profile is claiming.");

            var known = new System.Collections.Generic.List<string>(funnel);
            known.AddRange(StringArrayIn(js, "EVENTS", concatenated: true));

            CollectionAssert.AreEquivalent(TelemetryEvents.All, known,
                "telemetry.js and TelemetryEvents.All disagree about which events exist. The " +
                "server refuses a name it does not know, so the client would count and send an " +
                "event that is thrown away with no error on either side.");
        }

        [Test]
        public void TheTelemetryScriptStillAgreesWithTheCoreAboutItsLimits()
        {
            string js = File.ReadAllText(Path.Combine(CloudCodeRoot, "telemetry.js"));

            Assert.AreEqual(TelemetryRules.MaxEventsPerBatch, ConstantIn(js, "MAX_EVENTS_PER_BATCH"));
            Assert.AreEqual(TelemetryRules.MaxParametersPerEvent, ConstantIn(js, "MAX_PARAMETERS_PER_EVENT"));
            Assert.AreEqual(TelemetryRules.MaxParameterLength, ConstantIn(js, "MAX_PARAMETER_LENGTH"));
        }

        /// <summary>
        /// ⚠️ `ugs deploy` PUBLISHES A FOLDER, so a script written anywhere else is a script that
        /// is never deployed, and `CareerStore` is built to keep a local career quietly when the
        /// endpoint is unreachable. That combination looks exactly like a career nobody has played
        /// yet. `UgsServicesProbe.TheCareerEndpointAnswersALoad` is what proves the deploy landed;
        /// this is what proves there is something for it to deploy.
        /// </summary>
        [Test]
        public void EveryCloudCodeScriptSitsInTheFolderTheCliDeploys()
        {
            Assert.IsTrue(File.Exists(Path.Combine(CloudCodeRoot, "player-account.js")));
            Assert.IsTrue(File.Exists(Path.Combine(CloudCodeRoot, "match-record.js")));
            Assert.IsTrue(File.Exists(Path.Combine(CloudCodeRoot, "telemetry.js")));
        }

        /// <summary>
        /// ⚠️⚠️ THE RECORD IS BIGGER THAN A PACKET, AND THIS IS THE MEASUREMENT RATHER THAN THE
        /// ASSUMPTION. Every other named message in `MatchRpc` is tens of bytes and takes the
        /// default `ReliableSequenced`, which cannot split a message: an oversized one is
        /// refused by the transport, the host logs a line nobody reads, and every client
        /// silently gets no end-of-match summary and no career entry. That is the exact failure
        /// the protocol bump for this message exists to make impossible, so the delivery is
        /// asserted here as well as chosen there.
        ///
        /// ⚠️ THE FLOOR IS 1300 BYTES, WHICH IS UNDER A PLAIN ETHERNET MTU ON PURPOSE.
        /// `MatchRpc.PoseDelivery`'s note records that they play over Hamachi, a VPN with a
        /// smaller MTU and real loss, and that the relay path *"was not better designed, it was
        /// luckier"*. Sizing anything against 1500 is that mistake one layer up.
        /// </summary>
        [Test]
        public void AFullMatchRecordNeedsMoreThanOnePacketAndIsSentFragmented()
        {
            var players = new PlayerMatchStats[Balance.PlayerCount];
            for (int i = 0; i < players.Length; i++)
                players[i] = new PlayerMatchStats
                {
                    Slot = i,
                    PlayerId = new string('p', 32),
                    Handle = new string('W', AccountRules.HandleMax),
                    CharacterId = "phaister",
                    SlipperId = "alpombra",
                };

            var record = new MatchRecord
            {
                MatchId = System.Guid.NewGuid().ToString("N"),
                Mode = GameMode.HeroStrike.ToString(),
                MapId = "ilalim_ng_tulay",
                Rounds = Balance.HeroStrikeRounds,
                PlayedUtc = System.DateTime.UtcNow.ToString("O"),
                DefenderByRound = new int[Balance.HeroStrikeRounds],
                Players = players,
            };

            int bytes = System.Text.Encoding.UTF8.GetByteCount(JsonUtility.ToJson(record));
            Debug.Log($"[CareerAudit] a full four-player MatchRecord serialises to {bytes} bytes");

            Assert.Greater(bytes, 1300,
                "a record now fits in one packet, so the fragmented delivery may no longer be " +
                "needed. Re-measure before simplifying it: this test is the reason it is there.");

            string rpc = File.ReadAllText(Path.Combine(AssetsRoot, "Runtime", "Net", "MatchRpc.cs"));
            StringAssert.Contains("NetworkDelivery.ReliableFragmentedSequenced", rpc,
                "the match record is larger than one packet and must not go out on a delivery " +
                "that cannot split it. docs/TODO.md § 89.5.");
        }

        /// <summary>
        /// ⚠️⚠️ CLOUD CODE STRIPS EVERY PARAMETER A SCRIPT DOES NOT DECLARE, AND THE FAILURE IS
        /// SILENT AND LOOKS EXACTLY LIKE A WORKING ENDPOINT. Measured live on 2026-08-30: the
        /// telemetry endpoint was called with `{"action":"report"}` and answered with the SUBMIT
        /// branch's payload, which is what an ABSENT action falls through to. Every script here
        /// dispatches on `params.action || "<default>"`, so a stripped action does not throw, does
        /// not log, and returns a well-formed answer from the wrong branch.
        ///
        /// **`docs/TODO.md` § 90.5 is what that had already cost**: `player-account`'s save and
        /// delete and `match-record`'s submit had never once run against the live project, and the
        /// probes passed the whole time because they only ask whether the endpoint answered.
        ///
        /// ⚠️ THIS READS THE SOURCE AS TEXT for the same reason the tests above it do: nothing on
        /// this machine compiles the JavaScript, and the live symptom is an answer rather than an
        /// error.
        /// </summary>
        [Test]
        public void EveryParameterACloudCodeScriptReadsIsDeclaredSoItIsNotStripped()
        {
            foreach (string script in new[] { "player-account.js", "match-record.js", "telemetry.js" })
            {
                string js = File.ReadAllText(Path.Combine(CloudCodeRoot, script));

                var block = Regex.Match(js, @"module\.exports\.params\s*=\s*\{(.*?)\n\};",
                                        RegexOptions.Singleline);
                Assert.IsTrue(block.Success,
                    $"{script} declares no `module.exports.params`, so Cloud Code strips every " +
                    "parameter it is sent and every call lands on the default branch.");

                var declared = new System.Collections.Generic.HashSet<string>();
                foreach (Match entry in Regex.Matches(block.Groups[1].Value, @"^\s*(\w+)\s*:", RegexOptions.Multiline))
                    declared.Add(entry.Groups[1].Value);

                var used = new System.Collections.Generic.HashSet<string>();
                foreach (Match entry in Regex.Matches(js, @"params\.(\w+)"))
                    used.Add(entry.Groups[1].Value);

                used.ExceptWith(declared);
                Assert.IsEmpty(used,
                    $"{script} reads parameters it does not declare, so Cloud Code delivers them " +
                    $"as undefined: {string.Join(", ", used)}");

                // ⚠️⚠️ AND THE DECLARATION IS DROPPED BY A TOP-LEVEL FUNCTION CALLED
                // `parameters`, WHICH COST AN AFTERNOON TO FIND. `docs/TODO.md` § 90.5: with one
                // in the file, `ugs deploy` uploaded the code and then reported `params: []` for
                // the WHOLE script, so `action` went missing and every request landed on the
                // default branch. Nothing failed and nothing warned. It was bisected one function
                // at a time against the live service; renaming it was the entire fix.
                Assert.IsFalse(Regex.IsMatch(js, @"^\s*(async\s+)?function\s+parameters\s*\(",
                                             RegexOptions.Multiline),
                    $"{script} declares a top-level `function parameters`, which makes the deploy " +
                    "silently drop every declared parameter. docs/TODO.md § 90.5.");
            }
        }

        /// <summary>
        /// ⚠️ THE TELEMETRY BATCH TRAVELS AS A `String`, NOT AS `JSON`. Declaring it `JSON` made
        /// the service drop the whole parameter block, and `TelemetrySink` serialises it to match.
        /// `docs/TODO.md` § 90.5. Two halves of one wire shape, in two languages, so this pins them.
        /// </summary>
        [Test]
        public void TheTelemetryBatchIsSentAndDeclaredAsAString()
        {
            string js = File.ReadAllText(Path.Combine(CloudCodeRoot, "telemetry.js"));
            StringAssert.Contains("events: \"String\"", js,
                "telemetry.js no longer declares `events` as a String. docs/TODO.md § 90.5.");

            string sink = File.ReadAllText(
                Path.Combine(AssetsRoot, "Runtime", "Net", "TelemetrySink.cs"));
            StringAssert.Contains("JsonConvert.SerializeObject(events)", sink,
                "TelemetrySink is no longer serialising the batch before sending it, so the " +
                "endpoint receives something its `parseBatch` cannot read. docs/TODO.md § 90.5.");
        }

        /// <summary>
        /// Reads a `const NAME = [ "a", "b" ];` list out of a script, and optionally the
        /// `const NAME = OTHER.concat([ ... ]);` form the event list uses.
        ///
        /// ⚠️ IT PARSES RATHER THAN SEARCHING FOR SUBSTRINGS, because a `Contains` check passes
        /// on a script that has the right names AND an extra one, which is exactly the drift that
        /// makes a client event silently disappear.
        /// </summary>
        private static string[] StringArrayIn(string js, string name, bool concatenated = false)
        {
            string pattern = concatenated
                ? @"const\s+" + Regex.Escape(name) + @"\s*=\s*\w+\.concat\(\[(.*?)\]\)"
                : @"const\s+" + Regex.Escape(name) + @"\s*=\s*\[(.*?)\]";

            var match = Regex.Match(js, pattern, RegexOptions.Singleline);
            Assert.IsTrue(match.Success, $"no `const {name} = [...]` in the script");

            var values = new System.Collections.Generic.List<string>();
            foreach (Match entry in Regex.Matches(match.Groups[1].Value, "\"([^\"]+)\""))
                values.Add(entry.Groups[1].Value);

            return values.ToArray();
        }

        /// <summary>
        /// Every number `ProgressionRules` and `match-record.js` both hold, compared as text.
        ///
        /// ⚠️⚠️ THE LIVE PROBE AND THIS TEST ASK DIFFERENT QUESTIONS AND NEITHER REPLACES THE
        /// OTHER. `CloudEndpointActionProbe.ARealSubmissionPaysExactlyWhatProgressionRulesSays`
        /// submits a real record and asserts the XP the DEPLOYED script paid, which is the only
        /// thing that can catch a service running an older file. It is `[Category("Ugs")]`, so it
        /// needs a network, spends free-tier quota and does not run in a default sweep. **This
        /// asks whether the two copies in the REPOSITORY agree, in about a millisecond, on every
        /// run**, and it is the one that fails in the pull request rather than in production.
        /// `docs/TODO.md` § 90.9 makes the identical argument for telemetry.
        ///
        /// ⚠️ THE PLACEMENT TABLE IS COMPARED ELEMENT BY ELEMENT, NOT AS A LENGTH. A four-entry
        /// array that agrees on its first and last value and not its middle two is exactly the
        /// edit somebody makes tuning second place, and a count would wave it through.
        ///
        /// ⚠️ AND THE AFK CONSTANTS ARE IN HERE FOR THE REASON § 91.1 GIVES. They decide whether
        /// a match pays at all, so a drift there is not a small XP difference: it is the whole
        /// award, on one side only, with the server winning.
        /// </summary>
        [Test]
        public void TheCareerScriptStillAgreesWithTheCoreAboutEveryXpNumber()
        {
            string js = File.ReadAllText(Path.Combine(CloudCodeRoot, "match-record.js"));

            Assert.AreEqual(ProgressionRules.CompletionXp, ConstantIn(js, "COMPLETION_XP"),
                "COMPLETION_XP drifted. It is the largest single term, so this is the one that " +
                "makes a player's level disagree with the bar they just watched fill.");
            Assert.AreEqual(ProgressionRules.ObjectiveKnockdownXp,
                ConstantIn(js, "OBJECTIVE_KNOCKDOWN_XP"), "OBJECTIVE_KNOCKDOWN_XP drifted");
            Assert.AreEqual(ProgressionRules.ObjectivePressureRetrievalXp,
                ConstantIn(js, "OBJECTIVE_PRESSURE_RETRIEVAL_XP"),
                "OBJECTIVE_PRESSURE_RETRIEVAL_XP drifted");
            Assert.AreEqual(ProgressionRules.ObjectiveTagXp,
                ConstantIn(js, "OBJECTIVE_TAG_XP"), "OBJECTIVE_TAG_XP drifted");
            Assert.AreEqual(ProgressionRules.ObjectiveSabotageXp,
                ConstantIn(js, "OBJECTIVE_SABOTAGE_XP"), "OBJECTIVE_SABOTAGE_XP drifted");
            Assert.AreEqual(ProgressionRules.ObjectiveCleanXp,
                ConstantIn(js, "OBJECTIVE_CLEAN_XP"), "OBJECTIVE_CLEAN_XP drifted");

            Assert.AreEqual(ProgressionRules.XpPerLevel, ConstantIn(js, "XP_PER_LEVEL"),
                "XP_PER_LEVEL drifted, so the server's level and the client's are different " +
                "views of the same XP");
            Assert.AreEqual(ProgressionRules.MasteryXpPerLevel,
                ConstantIn(js, "MASTERY_XP_PER_LEVEL"), "MASTERY_XP_PER_LEVEL drifted");

            Assert.AreEqual(ProgressionRules.AfkStrikesBeforePenalty,
                ConstantIn(js, "AFK_STRIKES_BEFORE_PENALTY"),
                "AFK_STRIKES_BEFORE_PENALTY drifted. docs/TODO.md § 91.1.");
            Assert.AreEqual(ProgressionRules.AfkPenaltyMatches,
                ConstantIn(js, "AFK_PENALTY_MATCHES"), "AFK_PENALTY_MATCHES drifted");

            var placement = Regex.Match(js, @"const\s+PLACEMENT_XP\s*=\s*\[([^\]]*)\]\s*;");
            Assert.IsTrue(placement.Success, "no `const PLACEMENT_XP = [...];` in match-record.js");

            var written = placement.Groups[1].Value
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .Select(int.Parse)
                .ToArray();

            Assert.AreEqual(ProgressionRules.PlacementXp.Length, written.Length,
                "PLACEMENT_XP has a different number of places than ProgressionRules.PlacementXp");

            for (int i = 0; i < written.Length; i++)
                Assert.AreEqual(ProgressionRules.PlacementXp[i], written[i],
                    $"PLACEMENT_XP[{i}] is {written[i]} in match-record.js and " +
                    $"{ProgressionRules.PlacementXp[i]} in ProgressionRules. The C# is the " +
                    "specification, so the script is the bug.");
        }

        private static int ConstantIn(string js, string name)
        {
            var match = Regex.Match(js, @"const\s+" + Regex.Escape(name) + @"\s*=\s*(\d+)\s*;");
            Assert.IsTrue(match.Success, $"no `const {name} = <number>;` in the script");
            return int.Parse(match.Groups[1].Value);
        }
    }
}
