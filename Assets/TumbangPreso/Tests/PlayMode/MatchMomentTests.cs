using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class MatchMomentTests
    {
        private INetProvider _provider;
        private sealed class Client : INetProvider
        {
            public bool IsHost => false; public bool IsNetworked => true;
            public int LocalSlot => 1; public int LocalPeerId => 1; public bool IsSeatlessReferee => false;
        }
        [UnitySetUp] public IEnumerator Before() { _provider = NetAuthority.Provider; yield return PlayModeWorld.Reset(); }
        [UnityTearDown] public IEnumerator After() { NetAuthority.Provider = _provider; yield return PlayModeWorld.Reset(); }
        [UnityTest] public IEnumerator DuplicateStaleAndLowerPriorityMomentsCannotChangeScoresOrReplayRecognition()
        {
            yield return MapRetrievalProbe.Load("Eskinita");
            var match = GameServices.Match; var banner = Object.FindAnyObjectByType<MatchMomentBanner>();
            Assert.IsNotNull(banner); int points = match.ScoreFor(1), events = 0;
            match.MomentPresented += _ => events++;
            long id = match.PresentationMatchId;
            NetAuthority.Provider = new Client();
            var triple = new MatchMoment(id, 10, match.RoundNumber, 1, MatchMomentKind.TripleCatch, 3, 25);
            Assert.IsTrue(match.ApplyNetworkMoment(triple));
            Assert.IsFalse(match.ApplyNetworkMoment(triple));
            Assert.IsFalse(match.ApplyNetworkMoment(new MatchMoment(id, 9, match.RoundNumber, 1, MatchMomentKind.FirstKnockdown)));
            Assert.IsFalse(match.ApplyNetworkMoment(new MatchMoment(id - 1, 11, match.RoundNumber, 1, MatchMomentKind.FirstKnockdown)));
            Assert.IsFalse(match.ApplyNetworkMoment(new MatchMoment(id, 11, match.RoundNumber + 1, 1, MatchMomentKind.FirstKnockdown)));
            Assert.IsTrue(match.ApplyNetworkMoment(new MatchMoment(id, 11, match.RoundNumber, 2, MatchMomentKind.LeadChange)));
            yield return new WaitForSecondsRealtime(.1f);
            Assert.AreEqual("TRIPLE CATCH", banner.Phrase, "A lower-priority same-frame phrase cannot replace the major catch.");
            Assert.AreEqual(2, events); Assert.AreEqual(points, match.ScoreFor(1), "Recognition cannot award points locally.");
            Hud.Instance.SetCleanFeed(true); yield return null;
            Hud.Instance.SetCleanFeed(false); yield return null; yield return null;
            Assert.IsFalse(banner.Showing, "Returning from clean feed cannot replay stale recognition.");
            match.AdoptPresentationMatch(id + 1);
            Assert.IsFalse(match.ApplyNetworkMoment(triple), "An old match event cannot replay after rematch.");
            Assert.IsTrue(match.ApplyNetworkMoment(new MatchMoment(id + 1, 1, match.RoundNumber, 1, MatchMomentKind.AccurateThree, 3, 20)));
            yield return new WaitForSecondsRealtime(.1f);
            Assert.AreEqual("THREE ON TARGET", banner.Phrase);
            match.ApplySnapshot(new int[4], match.RoundNumber + 1, true); yield return null;
            Assert.IsFalse(banner.Showing, "A round boundary clears the previous phrase.");
        }
    }
}
