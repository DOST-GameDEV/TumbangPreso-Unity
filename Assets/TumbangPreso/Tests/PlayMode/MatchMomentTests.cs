using System.Collections;
using System.Linq;
using UnityEngine.UI;
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
        [UnityTest]
        public IEnumerator HigherCatchImmediatelyReplacesItsActorWithoutDroppingUnrelatedMoments()
        {
            yield return MapRetrievalProbe.Load("Eskinita");
            Hud.Instance.ShowReadyPrompt(false);
            var match = GameServices.Match;
            var banner = Object.FindAnyObjectByType<MatchMomentBanner>();
            Assert.IsNotNull(banner);
            int score = match.ScoreFor(1);
            long id = match.PresentationMatchId;
            int round = match.RoundNumber;
            NetAuthority.Provider = new Client();
            Assert.IsTrue(match.ApplyNetworkMoment(new MatchMoment(id, 1, round, 1, MatchMomentKind.SingleCatch, 1)));
            Assert.AreEqual("SINGLE CATCH", banner.Phrase);
            Assert.IsTrue(match.ApplyNetworkMoment(new MatchMoment(id, 2, round, 2, MatchMomentKind.FirstKnockdown, 1, 50)));
            Assert.IsTrue(match.ApplyNetworkMoment(new MatchMoment(id, 3, round, 2, MatchMomentKind.SingleCatch, 1)));
            Assert.IsTrue(match.ApplyNetworkMoment(new MatchMoment(id, 4, round, 1, MatchMomentKind.DoubleCatch, 2, 25)));
            Assert.AreEqual("DOUBLE CATCH", banner.Phrase, "A higher catch must replace the current catch immediately.");
            Assert.IsTrue(match.ApplyNetworkMoment(new MatchMoment(id, 5, round, 1, MatchMomentKind.TripleCatch, 3, 25)));
            Assert.AreEqual("TRIPLE CATCH", banner.Phrase);
            Assert.IsTrue(match.ApplyNetworkMoment(new MatchMoment(id, 6, round, 3, MatchMomentKind.SingleCatch, 1)));
            Assert.IsTrue(match.ApplyNetworkMoment(new MatchMoment(id, 7, round, 3, MatchMomentKind.DoubleCatch, 2, 25)));
            Assert.AreEqual("TRIPLE CATCH", banner.Phrase, "Another actor cannot interrupt this catch.");
            yield return null;
            Canvas.ForceUpdateCanvases();
            var title = banner.GetComponentsInChildren<Text>().First(t => t.name == "MomentTitle");
            Assert.Greater(title.cachedTextGenerator.vertexCount, 0);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var began = typeof(MatchMomentBanner).GetField("_began", flags);
            var paint = typeof(MatchMomentBanner).GetMethod("Paint", flags);
            began.SetValue(banner, Time.unscaledTime - 2.49f); paint.Invoke(banner, null);
            Assert.AreEqual("TRIPLE CATCH", banner.Phrase, "Upgrade retains its complete display interval.");
            began.SetValue(banner, Time.unscaledTime - 2.51f); paint.Invoke(banner, null);
            Assert.AreEqual("FIRST KNOCKDOWN", banner.Phrase, "Unrelated recognition stays in order.");
            began.SetValue(banner, Time.unscaledTime - 2.51f); paint.Invoke(banner, null);
            Assert.AreEqual("SINGLE CATCH", banner.Phrase, "Another actor's catch must survive.");
            began.SetValue(banner, Time.unscaledTime - 2.51f); paint.Invoke(banner, null);
            Assert.AreEqual("DOUBLE CATCH", banner.Phrase, "A queued upgrade replaces its lower catch in place.");
            Assert.AreEqual(score, match.ScoreFor(1), "Presentation upgrades cannot award score.");
        }

        [UnityTest] public IEnumerator DuplicateStaleAndQueuedMomentsCannotChangeScoresOrReplayRecognition()
        {
            yield return MapRetrievalProbe.Load("Eskinita");
            Hud.Instance.ShowReadyPrompt(false);
            var match = GameServices.Match; var banner = Object.FindAnyObjectByType<MatchMomentBanner>();
            Assert.IsNotNull(banner); int points = match.ScoreFor(1), events = 0;
            match.MomentPresented += _ => events++;
            long id = match.PresentationMatchId;
            NetAuthority.Provider = new Client();
            var triple = new MatchMoment(id, 10, match.RoundNumber, 1, MatchMomentKind.MultiKnockdown, 3, 50);
            Assert.IsTrue(match.ApplyNetworkMoment(triple));
            Assert.IsFalse(match.ApplyNetworkMoment(triple));
            Assert.IsFalse(match.ApplyNetworkMoment(new MatchMoment(id, 9, match.RoundNumber, 1, MatchMomentKind.FirstKnockdown)));
            Assert.IsFalse(match.ApplyNetworkMoment(new MatchMoment(id - 1, 11, match.RoundNumber, 1, MatchMomentKind.FirstKnockdown)));
            Assert.IsFalse(match.ApplyNetworkMoment(new MatchMoment(id, 11, match.RoundNumber + 1, 1, MatchMomentKind.FirstKnockdown)));
            Assert.IsTrue(match.ApplyNetworkMoment(new MatchMoment(id, 11, match.RoundNumber, 2, MatchMomentKind.SingleCatch)));
            yield return new WaitForSecondsRealtime(.1f);
            Assert.AreEqual("MULTI KNOCKDOWN", banner.Phrase, "Another same-frame award must queue behind the current phrase.");
            Canvas.ForceUpdateCanvases();
            var title = banner.GetComponentsInChildren<Text>().First(t => t.name == "MomentTitle");
            Assert.Greater(title.cachedTextGenerator.vertexCount, 0, "An accepted string is not enough: the display title must generate visible geometry.");
            Assert.LessOrEqual(title.preferredWidth, title.rectTransform.rect.width);
            Assert.IsNotNull(banner.GetComponent<CanvasRenderer>(), "The plate needs its own renderer.");
            yield return GameplayShots.Render(Camera.main, "triple-catch-visible-banner", true, outDir: "Logs/chain-awards-v2");
            Assert.AreEqual(2, events); Assert.AreEqual(points, match.ScoreFor(1), "Recognition cannot award points locally.");
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var began=typeof(MatchMomentBanner).GetField("_began",flags);
            var paint=typeof(MatchMomentBanner).GetMethod("Paint",flags);
            Assert.AreEqual(2.5f,MatchMomentBanner.Lifetime);
            began.SetValue(banner,Time.unscaledTime-2.49f);paint.Invoke(banner,null);
            Assert.AreEqual("MULTI KNOCKDOWN",banner.Phrase);
            began.SetValue(banner,Time.unscaledTime-2.51f);paint.Invoke(banner,null);
            Assert.AreEqual("SINGLE CATCH",banner.Phrase,"The queued recognition gets its own full interval.");
            Hud.Instance.SetCleanFeed(true); yield return null;
            Hud.Instance.SetCleanFeed(false); yield return null; yield return null;
            Assert.IsFalse(banner.Showing, "Returning from clean feed cannot replay stale recognition.");
            match.AdoptPresentationMatch(id + 1);
            Assert.IsFalse(match.ApplyNetworkMoment(triple), "An old match event cannot replay after rematch.");
            Assert.IsTrue(match.ApplyNetworkMoment(new MatchMoment(id + 1, 1, match.RoundNumber, 1, MatchMomentKind.FirstKnockdown, 1, 50)));
            yield return new WaitForSecondsRealtime(.1f);
            Assert.AreEqual("FIRST KNOCKDOWN", banner.Phrase);
            match.ApplySnapshot(new int[4], match.RoundNumber + 1, true); yield return null;
            Assert.IsFalse(banner.Showing, "A round boundary clears the previous phrase.");
        }
    }
}
