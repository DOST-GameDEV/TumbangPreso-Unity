using NUnit.Framework;
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
