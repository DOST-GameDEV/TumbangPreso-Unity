using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Stills of the live match HUD's other-score plates, Tab table, kill feed and status landings at 1920 x 1080 over the
    /// real court, for review against the boards. Never touches `Logs/ui-artboards`: the pictures are copied to
    /// `Logs/live-hud-captures`. Motion is held still, so a still is the resting look; how it moves cannot be read here.
    /// </summary>
    public sealed class LiveHudCaptureShots
    {
        private const BindingFlags Priv = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string Folder = "Logs/live-hud-captures";

        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        private static IEnumerator Shoot(TumpMatchReadout view, string name)
        {
            yield return TumpUiCapture.Capture(name, view.Canvas, 1920, 1080, false, true);
            Directory.CreateDirectory(Folder);
            File.Copy("Logs/shots-native-ui/" + name + ".png", Folder + "/" + name + ".png", true);
        }

        private static Canvas HoldStamp(System.Type stamp)
        {
            var live = (Component)stamp.GetField("_live", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.IsNotNull(live, stamp.Name + " did not play.");
            stamp.GetField("_life", Priv).SetValue(live, 60f);
            return live.GetComponent<Canvas>();
        }

        private static (float Top, float Bottom) Span(Canvas canvas, params string[] names)
        {
            var scale = canvas.transform.lossyScale.y;
            float top = float.MinValue, bottom = float.MaxValue; var corners = new Vector3[4];
            foreach (var rect in canvas.GetComponentsInChildren<RectTransform>())
            {
                if (System.Array.IndexOf(names, rect.name) < 0) continue;
                rect.GetWorldCorners(corners);
                top = Mathf.Max(top, corners[1].y / scale); bottom = Mathf.Min(bottom, corners[0].y / scale);
            }
            Assert.Greater(top, bottom, "None of " + string.Join(",", names) + " is showing.");
            return (top, bottom);
        }

        /// <summary>Every centre message up at once: each keeps its own lane under the timer, none drawn over another.</summary>
        [UnityTest, Timeout(300000)]
        public IEnumerator CentreMessagesDoNotOverlap()
        {
            GameLaunch.SoloSeat = (MatchRules.DefenderSlotFor(1) + 1) % 4;
            yield return UiArtboardShots.Match("Eskinita", GameMode.HeroStrike);
            var round = GameServices.Round; var local = round.PlayerAt(GameLaunch.SoloSeat);
            var view = Object.FindFirstObjectByType<TumpMatchReadout>();
            TayaReveal.Play(0); CanDownStamp.Play(0);
            var taya = HoldStamp(typeof(TayaReveal)); var can = HoldStamp(typeof(CanDownStamp));
            view.Toast("OUT OF BOUNDS", 30);
            for (int i = 0; i < 4; i++)
            {
                view.Refuse("CANNOT RUN - STAMINA MUST BE REPLENISHED FIRST"); view.Tick(local, false, false, false, false);
                yield return new WaitForSecondsRealtime(.25f);
            }
            var lanes = new[]
            {
                Span(taya, "TayaRevealWord", "TayaRevealWho"), Span(can, "CanDownPlate"),
                Span(view.Canvas, "WarningMessage", "WarningReasonPlate"), Span(view.Canvas, "MatchToast")
            };
            for (int i = 1; i < lanes.Length; i++)
                Debug.Log("[Lanes] " + i + " " + lanes[i].Top + " to " + lanes[i].Bottom);
            yield return TumpUiCapture.Capture("live-hud-lanes", view.Canvas, 1920, 1080, false, true, new[] { taya, can });
            Directory.CreateDirectory(Folder);
            File.Copy("Logs/shots-native-ui/live-hud-lanes.png", Folder + "/live-hud-lanes.png", true);
            for (int i = 1; i < lanes.Length; i++)
                Assert.LessOrEqual(lanes[i].Top, lanes[i - 1].Bottom + 12, "Message lane " + i + " runs into the one above it.");
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator LiveHudStills()
        {
            GameLaunch.SoloSeat = (MatchRules.DefenderSlotFor(1) + 1) % 4;
            SettingsStore.Current.CinematicCameraMotion = true; // as the artboard pass has it, which is when the other seats spawn
            yield return UiArtboardShots.Match("Eskinita", GameMode.HeroStrike);
            float until = Time.realtimeSinceStartup + 20;
            while (Time.realtimeSinceStartup < until && (GameServices.Round.PlayerAt(0) == null || GameServices.Round.PlayerAt(2) == null || GameServices.Round.PlayerAt(3) == null))
                yield return null;
            var round = GameServices.Round; var local = round.PlayerAt(GameLaunch.SoloSeat);
            var view = Object.FindFirstObjectByType<TumpMatchReadout>();
            var scores = (Scoreboard)typeof(MatchDirector).GetField("_scores", Priv).GetValue(GameServices.Match);
            var scoreAt = typeof(TumpMatchReadout).GetField("_scoreAt", Priv);
            local.ClearStatuses();
            // The scores settle first, so the next change is the news.
            view.Tick(local, false, false, false, false); yield return new WaitForSecondsRealtime(.5f);
            view.Tick(local, false, false, false, false); yield return new WaitForSecondsRealtime(3.5f);
            view.Tick(local, false, false, false, false);
            yield return Shoot(view, "live-hud-quiet");

            for (int seat = 0; seat < 4; seat++)
            {
                var who = round.PlayerAt(seat);
                Debug.Log("[LiveHudSeats] seat " + seat + " actor=" + (who != null) + " index=" + (who != null ? who.CharacterIndex : -9)
                          + " mode=" + (who != null ? who.Mode.ToString() : "-") + " score=" + GameServices.Match.ScoreFor(seat) + " local=" + GameLaunch.SoloSeat);
            }
            // Another seat scores: the plates slide in with the green points.
            scores.SetAll(new[] { 2450, 1980, 640, 1220 }); scoreAt.SetValue(view, 0f);
            view.Tick(local, false, false, false, false); yield return new WaitForSecondsRealtime(.7f);
            view.Tick(local, false, false, false, false);
            var at = round.Lata.transform.position;
            Visual.MatchFlair.Play(Visual.MatchFlair.Kind.Block, 2, 1, at);
            Visual.MatchFlair.Play(Visual.MatchFlair.Kind.Tag, 0, 3, at);
            yield return new WaitForSecondsRealtime(.3f);
            // The capture itself takes seconds, so the plates are held up for it.
            typeof(TumpMatchReadout).GetField("_boardPopUntil", Priv).SetValue(view, Time.unscaledTime + 30f);
            yield return Shoot(view, "live-hud-scorepop");

            // Hold Tab: the whole table, with your own plate underlined.
            local.ApplyWhirled(30); local.ApplyChilled(30); local.ApplyRooted(30);
            view.HoldBoardForCapture(true);
            view.Tick(local, false, false, false, false); yield return new WaitForSecondsRealtime(.7f);
            view.Tick(local, false, false, false, false);
            Debug.Log("[LiveHudSeats] plates shown " + view.BoardPlatesShown + " held " + view.BoardHeld);
            Assert.IsTrue(view.BoardHeld, "Holding Tab is the board.");
            yield return Shoot(view, "live-hud-tab");
            view.HoldBoardForCapture(false);
            view.Tick(local, false, false, false, false);

            // The statuses have just landed: the soonest to expire speaks first, the rest wait.
            yield return new WaitForSecondsRealtime(.9f);
            view.Tick(local, false, false, false, false);
            yield return Shoot(view, "live-hud-status-land");
            var landedAt = typeof(TumpMatchReadout).GetField("_statusLandingAt", Priv);
            landedAt.SetValue(view, (float)landedAt.GetValue(view) - 2.1f);
            view.Tick(local, false, false, false, false); yield return new WaitForSecondsRealtime(.3f);
            view.Tick(local, false, false, false, false);
            yield return Shoot(view, "live-hud-status-land-2");

            // A busy moment: a warning, a notice, statuses, the feed and the plates together.
            local.ClearStatuses(); local.ApplyWhirled(30); local.ApplyChilled(30); local.ApplyRooted(30);
            var idle = new float[4]; idle[local.PlayerSlot] = 8; round.ApplyNetworkTournamentState(0, idle);
            view.Toast("OUT OF BOUNDS", 30);
            scores.SetAll(new[] { 2450, 1980, 640, 1320 }); scoreAt.SetValue(view, 0f);
            for (int i = 0; i < 3; i++) view.Tick(local, false, false, false, false);
            typeof(TumpMatchReadout).GetField("_boardPopUntil", Priv).SetValue(view, Time.unscaledTime + 30f);
            yield return new WaitForSecondsRealtime(.8f);
            view.Tick(local, false, false, false, false);
            yield return Shoot(view, "live-hud-crowded");
        }
    }
}
