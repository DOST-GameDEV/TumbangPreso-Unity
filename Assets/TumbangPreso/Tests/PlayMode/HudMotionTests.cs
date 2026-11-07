using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The match bar moves when the match does (owner, 2026-10-22): a new total rolls into its chip while the label
    /// already holds the true score, the points fly home from the knockdown, and spectators see each seat's ultimate
    /// meter and a TAYA tab. Captures in Logs/shots-native-ui are the review.
    /// </summary>
    public sealed class HudMotionTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        private static Text Named(TumpMatchReadout readout, string name, int slot)
            => readout.Canvas.transform.Find("Root/MatchScores/ScoreRow" + slot + "/" + name)?.GetComponent<Text>()
               ?? readout.Canvas.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.name == name && t.transform.parent.name == "ScoreRow" + slot);

        [UnityTest]
        public IEnumerator KnockdownRollsTheTotalAndFliesThePointsHome()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            Settings.SettingsStore.Current.ReducedUiMotion = false;
            try
            {
                var can = GameServices.Round.Lata; var match = GameServices.Match;
                var readout = Object.FindAnyObjectByType<TumpMatchReadout>();
                Assert.IsNotNull(readout);
                float limit = Time.time + 4;
                while (can.IsProtected && Time.time < limit) yield return null;
                int thrower = (match.DefenderSlot + 1) % 4;
                can.HostKnockDown(thrower);
                yield return null; yield return null;
                var score = Named(readout, "Score", thrower);
                Assert.IsNotNull(score, "The thrower's chip has a score label.");
                Assert.AreEqual(match.ScoreFor(thrower).ToString(), score.text, "The label holds the true total at once.");
                var ghost = Named(readout, "ScoreRollOut", thrower);
                Assert.IsNotNull(ghost); Assert.IsTrue(ghost.enabled, "The old total rolls out above the new one.");
                // In play the chip row is off, so nothing flies and the corner and the plates tell the score.
                Assert.IsFalse(readout.Canvas.GetComponentsInChildren<Text>().Any(t => t.name == "PointsInFlight" && t.enabled),
                    "With the chip row off there is no chip for the points to fly to.");
                yield return TumpUiCapture.Capture("HudMotion-roll-and-flight", readout.Canvas, 1600, 900, false, true);
                yield return new WaitForSecondsRealtime(.8f);
                Assert.IsFalse(ghost.enabled, "The roll settles.");
                Assert.AreEqual(match.ScoreFor(thrower).ToString(), score.text);
            }
            finally { Settings.SettingsStore.Current.ReducedUiMotion = reduced; }
        }

        [UnityTest]
        public IEnumerator SpectatorChipsCarryUltMetersAndTheTayaTab()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita, GameMode.HeroStrike);
            var match = GameServices.Match;
            Hud.Instance.EnterSpectatorMode();
            var readout = Object.FindAnyObjectByType<TumpMatchReadout>();
            // A spectator reads the HUD's own score plates, standing, ranked (owner, 2026-11-08), not the old chip row.
            float until = Time.unscaledTime + 3;
            while (readout.BoardPlatesShown < 4 && Time.unscaledTime < until) yield return null;
            Assert.AreEqual(4, readout.BoardPlatesShown, "Every seat's plate shows to a spectator.");
            Assert.IsFalse(readout.Canvas.GetComponentsInChildren<RectTransform>().Any(r => r.name.StartsWith("ScoreRow")), "The old chip row is gone for spectators.");
            yield return new WaitForSecondsRealtime(.4f);
            yield return TumpUiCapture.Capture("HudMotion-spectator-cards", readout.Canvas, 1600, 900, false, true);

            // The broadcast feed is always on for a spectator: a moment on the court lands a row they can see.
            var feed = Object.FindAnyObjectByType<MatchEventFeed>();
            Assert.IsNotNull(feed); Assert.IsTrue(feed.isActiveAndEnabled, "Spectators keep the event feed.");
            int rows = feed.Count;
            Visual.MatchFlair.Play(Visual.MatchFlair.Kind.LataDown, (match.DefenderSlot + 1) % 4, -1, GameServices.Round.Lata.transform.position);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(rows + 1, feed.Count, "The moment reaches the spectator's feed.");
            Assert.IsTrue(feed.transform.Find("Event0").GetComponent<CanvasGroup>().alpha > .5f, "and is drawn.");
        }
    }
}
