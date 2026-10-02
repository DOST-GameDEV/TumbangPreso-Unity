using NUnit.Framework;
using System.Linq;
using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// The Unity half of Phase 7 and Phase 5's colour dial: what actually crosses the wire.
    ///
    /// ⚠️ `Core.Tests` PROVES WHAT THE RULES DO WITH VALUES THEY ARE HANDED. This file proves the
    /// game hands them the right ones, which is the gap `docs/TODO.md` § 94.1 lived in for two
    /// phases: every copy of "which line is mine" agreed on the same wrong value, and no test of
    /// the rules could see it.
    /// </summary>
    public class MatchmakingWireTests
    {
        [TestCase("refused")]
        [TestCase("fault")]
        [TestCase("accepted")]
        [TestCase("cancelled")]
        [TestCase("replaced")]
        public async System.Threading.Tasks.Task QueueHostFailureReconsidersTheCachedListWithoutRevivingAnOldTicket(string outcome)
        {
            Assert.IsTrue(System.Environment.GetCommandLineArgs().Contains("-tp-profile"));
            Assert.IsFalse(NetIdentity.IsOnline, "This queue recovery check never initializes a service session.");
            const System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var type = typeof(Matchmaker);
            var originalAccount = GameServices.Account;
            var accountRoot = new GameObject("Dormant queue recovery account"); accountRoot.SetActive(false);
            var account = accountRoot.AddComponent<PlayerAccount>();
            typeof(PlayerAccount).GetField("_profile", hidden).SetValue(account, new AccountProfile { PlayerId = "queue-retry-local" });
            typeof(GameServices).GetProperty("Account").SetValue(null, account);
            var owner = new GameObject("Dormant queue recovery session"); owner.SetActive(false);
            var net = owner.AddComponent<NetSession>(); var queue = owner.AddComponent<Matchmaker>();
            var pending = new System.Threading.Tasks.TaskCompletionSource<bool>();
            System.Threading.Tasks.Task attempt = null;
            try
            {
                type.GetField("_net", hidden).SetValue(queue, net);
                type.GetProperty("Elapsed").SetValue(queue, MatchmakingRules.SecondsToWidest + 1);
                var deadlineField = type.GetField("_reevaluateAt", hidden);
                var busyField = type.GetField("_busy", hidden);
                System.Func<System.Threading.Tasks.Task<bool>> start = () => outcome == "fault"
                    ? System.Threading.Tasks.Task.FromException<bool>(new System.InvalidOperationException("simulated host startup failure"))
                    : outcome == "accepted" || outcome == "refused"
                        ? System.Threading.Tasks.Task.FromResult(outcome == "accepted") : pending.Task;
                float before = Time.unscaledTime;
                attempt = (System.Threading.Tasks.Task)type.GetMethod("HostAsync", hidden).Invoke(queue, new object[] { start });
                float replacementDeadline = before + 123;
                if (outcome == "cancelled")
                {
                    Assert.AreEqual(QueueState.Hosting, queue.State); queue.Cancel(); pending.SetResult(false);
                }
                else if (outcome == "replaced")
                {
                    Assert.AreEqual(QueueState.Hosting, queue.State);
                    ((JoinAttemptGate)type.GetField("_queueAttempts", hidden).GetValue(queue)).Begin();
                    type.GetProperty("State").SetValue(queue, QueueState.Searching);
                    deadlineField.SetValue(queue, replacementDeadline); busyField.SetValue(queue, true);
                    pending.SetResult(false);
                }
                await attempt;
                float deadline = (float)deadlineField.GetValue(queue);
                if (outcome == "refused" || outcome == "fault")
                {
                    Assert.AreEqual(QueueState.Searching, queue.State);
                    Assert.IsFalse(float.IsInfinity(deadline), "A stable maximum-width queue has no event left to recover from this hosting failure.");
                    Assert.GreaterOrEqual(deadline, before + (float)MatchmakingCandidateCache.RetrySeconds);
                    Assert.LessOrEqual(deadline, Time.unscaledTime + (float)MatchmakingCandidateCache.RetrySeconds);
                }
                else if (outcome == "replaced")
                    Assert.AreEqual(replacementDeadline, deadline, "An obsolete host completion changed the replacement ticket's deadline.");
                else Assert.IsTrue(float.IsPositiveInfinity(deadline), "A successful/cancelled ticket acquired an unsolicited retry.");
                Assert.AreEqual(outcome == "cancelled" ? QueueState.Cancelled : QueueState.Searching, queue.State);
                Assert.AreEqual(outcome == "replaced", (bool)busyField.GetValue(queue), "An old request changed the current ticket's busy ownership.");
            }
            finally
            {
                if (!pending.Task.IsCompleted) pending.SetResult(false);
                if (attempt != null) await attempt;
                Object.DestroyImmediate(owner); Object.DestroyImmediate(accountRoot);
                typeof(GameServices).GetProperty("Account").SetValue(null, originalAccount);
            }
        }

        [TestCase(QueueStake.Casual)]
        [TestCase(QueueStake.Ranked)]
        public void QueueCandidatesNeedMatchingSkillDataWithoutChangingTheirPoolRules(QueueStake stake)
        {
            string contract = new string('A', 64);
            string pool = MatchmakingRules.PoolKey(GameMode.HeroStrike, stake, InputDevice.KeyboardMouse,
                PlatformFamily.Desktop, NetSession.ProtocolVersion);
            var entry = new ServerQuery.Entry
            {
                Id = "room", RelayCode = "relay", SkillContract = contract, PoolKey = pool,
                Seated = 1, Occupied = 1, Capacity = 4, BandLow = 1000, BandHigh = 2000,
                SeatLow = 1500, SeatHigh = 1500, HostPlayerId = "other"
            };
            var candidates = new MatchmakingCandidateCache();
            Assert.IsTrue(candidates.CanTry(entry, contract, 0));
            Assert.AreEqual(JoinRefusal.None, MatchmakingRules.Evaluate(entry.AsAdvert(), "me", 1500, 0, pool, null));
            entry.Occupied = 3;
            Assert.IsFalse(candidates.CanTry(entry, contract, 0, seatsNeeded: 2), "The party was offered reserved chairs.");
            Assert.IsTrue(candidates.CanTry(entry, contract, 0, seatsNeeded: 1));
            entry.Occupied = 1; entry.InProgress = true; entry.Backfill = true;
            Assert.IsTrue(candidates.CanTry(entry, contract, 0), "Compatibility filtering must not disable allowed backfill.");
            Assert.AreEqual(JoinRefusal.None, MatchmakingRules.Evaluate(entry.AsAdvert(), "me", 1500, 0, pool, null));
            entry.InProgress = false; entry.Backfill = false;
            entry.SkillContract = new string('B', 64);
            Assert.IsFalse(candidates.CanTry(entry, contract, 0));
            entry.SkillContract = "";
            Assert.IsFalse(candidates.CanTry(entry, contract, 0), "Unknown compatibility must not enter automatic pairing.");
            entry.SkillContract = contract; entry.RelayCode = "";
            Assert.IsFalse(candidates.CanTry(entry, contract, 0));
            string padPool = MatchmakingRules.PoolKey(GameMode.HeroStrike, stake, InputDevice.Gamepad,
                PlatformFamily.Desktop, NetSession.ProtocolVersion);
            if (stake == QueueStake.Ranked) Assert.AreNotEqual(pool, padPool);
            else Assert.AreEqual(pool, padPool);
        }

        [Test]
        public void QueueFailureCooldownTracksTheAttemptedEndpointAndResetsPerTicket()
        {
            string contract = new string('A', 64);
            var entry = new ServerQuery.Entry { Id = "room", RelayCode = "old", SkillContract = contract };
            var candidates = new MatchmakingCandidateCache();
            candidates.Failed(entry.Id, entry.RelayCode, 10);
            Assert.IsFalse(candidates.CanTry(entry, contract, 11));
            entry.RelayCode = "replacement";
            Assert.IsTrue(candidates.CanTry(entry, contract, 11));
            entry.RelayCode = "old";
            Assert.IsFalse(candidates.CanTry(entry, contract, 11), "A stale advertisement resurrected its failed allocation.");
            Assert.IsTrue(candidates.CanTry(entry, contract, 10 + MatchmakingCandidateCache.RetrySeconds));
            candidates.Failed(entry.Id, entry.RelayCode, 50);
            candidates.Clear();
            Assert.IsTrue(candidates.CanTry(entry, contract, 51));
        }

        // ------------------------------------------------------------------------------
        // The look frame
        // ------------------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE ROUND TRIP IS THE WHOLE OF WHY `LobbySeatInfo.PaletteId` BECAME `Look`. Three
        /// values now decide what a character looks like and they travel as one versioned string;
        /// a frame that does not survive its own codec dresses every remote player wrong.
        /// </summary>
        [Test]
        public void TheLookFrameSurvivesTheWire()
        {
            var look = new CharacterLook("mastery.zack.palette.alt1", 210, 130);
            var back = LookCodec.Decode(LookCodec.Encode(look));

            Assert.AreEqual("mastery.zack.palette.alt1", back.PaletteId);
            Assert.AreEqual(210, back.HueDegrees);
            Assert.AreEqual(130, back.SaturationPercent);
        }

        /// <summary>
        /// ⚠️⚠️ A BUILD THAT HAS NEVER HEARD OF THE FRAME DRAWS THE AUTHORED COLOURS RATHER THAN
        /// A BROKEN CHARACTER. That is the same degradation `PaletteRules.IsKnownVariant` already
        /// guarantees for an unknown palette id, and `Roster.Slippers`' header is the rule it
        /// comes from.
        /// </summary>
        [Test]
        public void AnUnknownOrEmptyFrameIsTheAuthoredCharacter()
        {
            Assert.IsTrue(LookCodec.Decode("").IsAuthored);
            Assert.IsTrue(LookCodec.Decode(null).IsAuthored);
            Assert.IsTrue(LookCodec.Decode("L9:something:1:2").IsAuthored);
            Assert.IsTrue(LookCodec.Decode("garbage").IsAuthored);
        }

        /// <summary>
        /// ⚠️⚠️ A MODIFIED CLIENT CANNOT PLAY AS A SHADOW. The receiver clamps what it draws
        /// rather than trusting what it was sent, which is the difference between an earned
        /// cosmetic and an expressive one stated as an assertion.
        /// </summary>
        [Test]
        public void AnOutOfRangeDialIsClampedOnArrival()
        {
            var silhouette = LookCodec.Decode("L1::400:0");

            Assert.AreEqual(40, silhouette.HueDegrees, "a hue past 359 wraps rather than clamping");
            Assert.AreEqual(PaletteRules.SaturationMin, silhouette.SaturationPercent,
                "a saturation of zero would draw a character as a grey silhouette on the " +
                "Eskinita road. VISION.md § 2 rule 5.");

            Assert.AreEqual(PaletteRules.SaturationMax,
                            LookCodec.Decode("L1::0:9000").SaturationPercent);
        }

        /// <summary>
        /// ⚠️ THE EARNED HALF IS REFUSED WHEN IT IS NOT OWNED AND THE FREE HALF IS NOT, which is
        /// the whole ownership model in one assertion. `settings.json` is a plain text file on the
        /// player's disk.
        /// </summary>
        [Test]
        public void AnUnownedPaletteIsRefusedAndTheDialIsNot()
        {
            var claim = new BannerClaim
            {
                PaletteId = "mastery.zack.palette.alt1",
                HueDegrees = 200,
                SaturationPercent = 120,
                Xp = 0,
            };

            var authorised = BannerRules.AuthoriseLook(claim, "dante");

            Assert.AreEqual(PaletteRules.DefaultId, authorised.PaletteId,
                "an account that has earned nothing was allowed to wear a mastery palette");
            Assert.AreEqual(200, authorised.HueDegrees, "the free dial was refused, and it is not earned");
            Assert.AreEqual(120, authorised.SaturationPercent);
        }

        // ------------------------------------------------------------------------------
        // The lobby advert
        // ------------------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ A LOBBY THAT NEVER QUEUED MUST NOT BE QUICK-MATCHABLE, AND THE DEFAULT IS WHAT
        /// GUARANTEES IT. Every lobby in this game auto-hosts on arrival
        /// (`ConvertedMatchSetup.AutoHost`), so a default of "in the pool" would silently offer
        /// the room of somebody waiting for one friend to three strangers.
        /// </summary>
        [Test]
        public void ARoomThatNeverQueuedIsNotInAnyPool()
        {
            var none = ServerQuery.HostedAdvert.None;

            string pool = MatchmakingRules.PoolKey(GameMode.Classic, QueueStake.Casual,
                                                   InputDevice.KeyboardMouse, PlatformFamily.Desktop,
                                                   NetSession.ProtocolVersion);

            var entry = new ServerQuery.Entry
            {
                PoolKey = none.PoolKey,
                BandLow = none.BandLow,
                BandHigh = none.BandHigh,
                SeatLow = none.SeatLow,
                SeatHigh = none.SeatHigh,
                Seated = 1,
                Capacity = LobbySession.MaxPlayers,
                HostPlayerId = none.HostPlayerId,
            };

            Assert.AreEqual(JoinRefusal.WrongPool,
                MatchmakingRules.Evaluate(entry.AsAdvert(), "me", 1500, 0.0f, pool, null),
                "a private room advertised itself into the quick match pool");
        }

        /// <summary>
        /// ⚠️ AN ENTRY FROM AN OLDER BUILD HAS NO POOL KEY AND STAYS VISIBLE IN THE BROWSER WHILE
        /// BEING INVISIBLE TO THE QUEUE, which is exactly right for a room that never opted in.
        /// </summary>
        [Test]
        public void ALobbyFromAnOlderBuildIsBrowsableAndNotQueueable()
        {
            var old = new ServerQuery.Entry
            {
                Seated = 1,
                Capacity = LobbySession.MaxPlayers,
                JoinCode = "AB12",
            };

            Assert.IsTrue(old.IsJoinable, "an older lobby stopped being joinable by hand");

            string pool = MatchmakingRules.PoolKey(GameMode.Classic, QueueStake.Casual,
                                                   InputDevice.KeyboardMouse, PlatformFamily.Desktop,
                                                   NetSession.ProtocolVersion);

            Assert.AreEqual(JoinRefusal.WrongPool,
                MatchmakingRules.Evaluate(old.AsAdvert(), "me", 1500, 0.0f, pool, null));
        }

        /// <summary>
        /// ⚠️⚠️ THE PROTOCOL VERSION IS IN THE POOL KEY SO THE QUEUE NEVER OFFERS A MATCH THE
        /// APPROVAL WILL REFUSE. Without it the player watches a queue find a match and then
        /// bounce off it with a version message, which reads as the queue being broken.
        /// </summary>
        [Test]
        public void TheQueueNeverOffersAMatchConnectionApprovalWouldRefuse()
        {
            string mine = MatchmakingRules.PoolKey(GameMode.Classic, QueueStake.Casual,
                                                   InputDevice.KeyboardMouse, PlatformFamily.Desktop,
                                                   NetSession.ProtocolVersion);

            string older = MatchmakingRules.PoolKey(GameMode.Classic, QueueStake.Casual,
                                                    InputDevice.KeyboardMouse, PlatformFamily.Desktop,
                                                    NetSession.ProtocolVersion - 1);

            Assert.AreNotEqual(mine, older);
            StringAssert.Contains($"v{NetSession.ProtocolVersion}.", mine);
        }

        /// <summary>
        /// ⚠️ THE MATCHMAKER IS A LOOKUP AND NOT AN `Ensure` AT ITS TWO READ SITES. Practice, LAN
        /// and the whole of the nationals venue have no queue at all, and the right answer to
        /// "was this ranked" and "should I offer a backfill seat" there is no.
        /// </summary>
        [Test]
        public void AskingForTheMatchmakerNeverCreatesOne()
        {
            foreach (var stray in Object.FindObjectsByType<Matchmaker>(FindObjectsInactive.Include,
                                                                       FindObjectsSortMode.None))
                Object.DestroyImmediate(stray);

            Assert.IsNull(Matchmaker.Current,
                "Matchmaker.Current built a matchmaker. A practice match would then be able to " +
                "advertise a backfill seat into the online pool.");
        }
    }
}
