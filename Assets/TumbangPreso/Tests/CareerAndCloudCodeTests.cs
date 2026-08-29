using System.IO;
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
        }

        /// <summary>
        /// ⚠️ `ugs deploy` PUBLISHES A FOLDER, so a script written anywhere else is a script that
        /// is never deployed, and `CareerStore` is built to keep a local career quietly when the
        /// endpoint is unreachable. That combination looks exactly like a career nobody has played
        /// yet. `UgsServicesProbe.TheCareerEndpointAnswersALoad` is what proves the deploy landed;
        /// this is what proves there is something for it to deploy.
        /// </summary>
        [Test]
        public void BothCloudCodeScriptsSitInTheFolderTheCliDeploys()
        {
            Assert.IsTrue(File.Exists(Path.Combine(CloudCodeRoot, "player-account.js")));
            Assert.IsTrue(File.Exists(Path.Combine(CloudCodeRoot, "match-record.js")));
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

        private static int ConstantIn(string js, string name)
        {
            var match = Regex.Match(js, @"const\s+" + Regex.Escape(name) + @"\s*=\s*(\d+)\s*;");
            Assert.IsTrue(match.Success, $"no `const {name} = <number>;` in the script");
            return int.Parse(match.Groups[1].Value);
        }
    }
}
