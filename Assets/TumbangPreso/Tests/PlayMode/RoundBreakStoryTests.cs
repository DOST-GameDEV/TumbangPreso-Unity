using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The break card tells the round's own story (owner, 2026-10-22): each standings chip counts up what its seat earned
    /// this round, and the chips drop in and slide from the last break's ranks. Captures in Logs/shots-native-ui.
    /// </summary>
    public sealed class RoundBreakStoryTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator BreakChipsCountUpWhatEachSeatEarnedThisRound()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            var match = GameServices.Match; var round = GameServices.Round;
            int scorer = (match.DefenderSlot + 1) % 4;
            int before = match.ScoreFor(scorer);
            match.AddScore(scorer, ScoreEvent.LataKnocked);
            match.AddScore(scorer, ScoreEvent.LataKnocked);
            int earned = match.ScoreFor(scorer) - before;
            Assert.Greater(earned, 0);
            round.EndRound(); match.BeginIntermission();
            Assert.IsTrue(RoleSwapCard.Showing);
            var view = Object.FindAnyObjectByType<TumpRoundSwapView>();
            Assert.IsNotNull(view);
            yield return new WaitForSecondsRealtime(.3f);
            yield return TumpUiCapture.Capture("BreakStory-landing", view.Canvas, 1600, 900, false, true);
            yield return new WaitForSecondsRealtime(1.0f);
            int rank = System.Array.IndexOf(match.Ranking(), scorer);
            Assert.AreEqual(earned, view.GainAt(rank), "The scorer's chip carries what it earned this round.");
            var gain = view.Canvas.transform.Find("CourtBreakPopup/ScoreTicket" + rank + "/StandingGain" + rank)?.GetComponent<UnityEngine.UI.Text>();
            Assert.IsNotNull(gain);
            Assert.AreEqual("+" + earned, gain.text, "The count lands on the full amount.");
            yield return TumpUiCapture.Capture("BreakStory-settled", view.Canvas, 1600, 900, false, true);
        }
    }
}
