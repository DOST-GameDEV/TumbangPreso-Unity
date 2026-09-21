using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class ScoreFeedbackTests
    {
        private bool _reduced;
        [UnitySetUp] public IEnumerator Before()
        { _reduced = SettingsStore.Current.ReducedUiMotion; yield return PlayModeWorld.Reset(); }
        [UnityTearDown] public IEnumerator After()
        { SettingsStore.Current.ReducedUiMotion = _reduced; yield return PlayModeWorld.Reset(); }
        [UnityTest] public IEnumerator AcceptedScoreEmphasisFollowsTheSeatThroughRankingAndClearsWhenHidden()
        {
            yield return MapRetrievalProbe.Load("Eskinita");
            SettingsStore.Current.ReducedUiMotion = false;
            var can = GameServices.Round.Lata; var match = GameServices.Match;
            var readout = Object.FindAnyObjectByType<TumpMatchReadout>(); Assert.IsNotNull(readout);
            Text Score(int slot)
            {
                var name = readout.Canvas.GetComponentsInChildren<Text>(true).First(t => t.name == "PlayerName" && t.text.StartsWith(PlayerIdentity.Label(slot) + " "));
                return name.transform.parent.GetComponentsInChildren<Text>(true).First(t => t.name == "Score");
            }
            float limit = Time.time + 4;
            while (can.IsProtected && Time.time < limit) yield return null;
            Assert.IsFalse(can.IsProtected);
            int before = match.ScoreFor(3); can.HostKnockDown(3);
            yield return new WaitForSecondsRealtime(.10f);
            Assert.AreEqual((before + 100).ToString(), Score(3).text);
            Assert.AreEqual("ScoreRow0", Score(3).transform.parent.name, "The credited scorer should rank above passive defence.");
            Assert.Greater(Score(3).rectTransform.localScale.x, 1.01f);
            Assert.AreEqual(Vector3.one, Score(1).rectTransform.localScale, "Sorting cannot transfer emphasis to a different seat.");
            SettingsStore.Current.ReducedUiMotion = true;
            yield return null; yield return null;
            Assert.AreEqual(Vector3.one, Score(3).rectTransform.localScale);
            Hud.Instance.SetCleanFeed(true); yield return null;
            SettingsStore.Current.ReducedUiMotion = false;
            Hud.Instance.SetCleanFeed(false); yield return null; yield return null;
            Assert.AreEqual(Vector3.one, Score(3).rectTransform.localScale, "Reopening must not replay an expired/hidden receipt.");
            Assert.AreEqual((before + 100).ToString(), Score(3).text);
            can.HostRestore();
            limit = Time.time + 4;
            while (can.IsProtected && Time.time < limit) yield return null;
            can.HostKnockDown(2); yield return new WaitForSecondsRealtime(.1f);
            Assert.Greater(Score(2).rectTransform.localScale.x, 1.01f);
            GameServices.Round.EndRound(); yield return null; yield return null;
            Assert.AreEqual(Vector3.one, Score(2).rectTransform.localScale);
        }
    }
}
