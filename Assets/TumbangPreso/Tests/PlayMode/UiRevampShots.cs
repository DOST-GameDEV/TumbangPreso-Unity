using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.InputLayer;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// UI revamp review frames (2026-10-06). One live Hero Strike round per map with the worst
    /// ordinary pile-up staged at once: an announcement, an earned moment, a refusal, an action
    /// with progress, three feed rows, several statuses and a long real player name. Frames go
    /// through `GameplayShots.Render` (graded HDR world, ungraded UI on top), the path the game
    /// itself composites, at the shapes the owner plays and judges.
    ///
    /// The assertions are geometric facts a picture cannot be trusted to show: the central
    /// lanes do not overlap, statuses stay on screen and clear of the lanes, and the names fit.
    /// Whether it looks right is decided by looking at the frames.
    /// </summary>
    public sealed class UiRevampShots
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        private const string Out = "Logs/ui-revamp";
        private static readonly Vector2Int[] Shapes =
        {
            new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(1440, 1080),
            new Vector2Int(2560, 1080), new Vector2Int(1600, 680),
        };

        [UnityTest, Timeout(300000)] public IEnumerator StreetCrowdedHud() => Crowded("Eskinita", "street");
        [UnityTest, Timeout(300000)] public IEnumerator ArenaCrowdedHud() => Crowded("Arena", "arena");

        private static IEnumerator Crowded(string scene, string tag)
        {
            var settings = SettingsStore.Current;
            float scale = settings.HudScale; bool larger = settings.LargerText, contrast = settings.HighContrastHud, reduced = settings.ReducedUiMotion;
            try
            {
                SceneFlow.Networked = false; SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
                yield return SceneManager.LoadSceneAsync(scene); yield return new WaitForSecondsRealtime(.4f);
                foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
                Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
                yield return new WaitForSecondsRealtime(3.7f);
                float until = Time.realtimeSinceStartup + 40;
                while ((Map.ArenaIntro.HidesUi || RoleSwapCard.Showing) && Time.realtimeSinceStartup < until) yield return null;
                yield return new WaitForSecondsRealtime(.5f);

                var round = GameServices.Round; var local = round.PlayerAt(GameLaunch.SoloSeat);
                TumpUiCapture.StageHudReview(local);
                local.GetComponent<PlayerInputReader>().enabled = false; local.Intent.Clear(); local.enabled = false;
                round.enabled = false;
                local.PlayerName = "Juan_dela_Cruz_99";
                var scores = (Scoreboard)typeof(MatchDirector).GetField("_scores", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(GameServices.Match);
                scores.SetAll(new[] { 2450, 1980, 640, 1120 });
                var view = Object.FindFirstObjectByType<TumpMatchReadout>();
                var root = (RectTransform)view.Canvas.transform;
                settings.ReducedUiMotion = true;

                // Calm first: what the player looks at for most of a round.
                local.ClearStatuses();
                view.Tick(local, false, false, false, false); yield return null;
                yield return GameplayShots.Render(Camera.main, tag + "-quiet-1920x1080", true, Out, null, 1920, 1080);

                foreach (var shape in Shapes)
                {
                    yield return Pile(view, local);
                    // Render first so a layout failure still leaves the frame that shows it.
                    yield return GameplayShots.Render(Camera.main, tag + "-crowded-" + shape.x + "x" + shape.y, true, Out, null, shape.x, shape.y);
                    // Render returns the canvases to the window's own shape; let the HUD lay out again.
                    yield return null; yield return null; Canvas.ForceUpdateCanvases();
                    AssertLanes(root);
                }

                // Larger text and high contrast at the smallest common window.
                settings.HudScale = 1.2f; settings.LargerText = true; settings.HighContrastHud = true;
                yield return Pile(view, local);
                yield return GameplayShots.Render(Camera.main, tag + "-accessible-1280x720", true, Out, null, 1280, 720);
                yield return null; yield return null; Canvas.ForceUpdateCanvases();
                AssertLanes(root);

                // The match menu over the same live scene, normal size, with pad focus on a choice.
                settings.HudScale = 1; settings.LargerText = false; settings.HighContrastHud = false;
                local.ClearStatuses(); view.Toast("", 0);
                var watcher = Object.FindFirstObjectByType<PauseWatcher>();
                Assert.IsNotNull(watcher, "The match needs its pause watcher.");
                var pause = Panel.Open<PausePanel>(watcher); pause.Local = local;
                yield return null; yield return new WaitForSecondsRealtime(.3f);
                var menu = GameObject.Find("OwnerPauseCanvas");
                Assert.IsNotNull(menu);
                foreach (var name in new[] { "ResumeMatch", "PauseSettings", "LeaveMatch" })
                    Assert.IsNotNull(menu.GetComponentsInChildren<Button>(true).SingleOrDefault(b => b.name == name), name);
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(
                    menu.GetComponentsInChildren<Button>(true).Single(b => b.name == "PauseSettings").gameObject);
                yield return new WaitForSecondsRealtime(.25f);
                yield return GameplayShots.Render(Camera.main, tag + "-pause-1920x1080", true, Out, null, 1920, 1080);
                yield return GameplayShots.Render(Camera.main, tag + "-pause-1280x720", true, Out, null, 1280, 720);
                pause.Close(); yield return null;
            }
            finally
            {
                settings.HudScale = scale; settings.LargerText = larger; settings.HighContrastHud = contrast; settings.ReducedUiMotion = reduced;
            }
        }

        /// <summary>Raises every central message and a realistic status/feed load on the real HUD.</summary>
        private static IEnumerator Pile(TumpMatchReadout view, CharacterMotor local)
        {
            local.ClearStatuses();
            local.ApplyWhirled(30); local.ApplyChilled(30); local.ApplyRooted(30);
            var idle = new float[4]; idle[local.PlayerSlot] = 8; GameServices.Round.ApplyNetworkTournamentState(0, idle);
            view.Toast("OUT OF BOUNDS", 30);
            typeof(MatchDirector).GetMethod("PresentHostMoment", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(GameServices.Match, new object[] { local.PlayerSlot, MatchMomentKind.FirstKnockdown, 1, 0 });
            var at = GameServices.Round.Lata.transform.position;
            Visual.MatchFlair.Play(Visual.MatchFlair.Kind.Block, 2, 1, at);
            Visual.MatchFlair.Play(Visual.MatchFlair.Kind.Tag, 0, 3, at);
            Visual.MatchFlair.Play(Visual.MatchFlair.Kind.LataDown, local.PlayerSlot, -1, at);
            for (int i = 0; i < 3; i++) { view.Tick(local, false, false, false, false); yield return null; }
            Canvas.ForceUpdateCanvases();
        }

        private static void AssertLanes(RectTransform root)
        {
            var toast = Bounds(root, (RectTransform)root.Find("MatchToastPlate"));
            var moment = Bounds(root, (RectTransform)root.Find("EarnedMoment"));
            var warning = Bounds(root, (RectTransform)root.Find("WarningMessage"));
            var action = Bounds(root, (RectTransform)root.Find("ContextualAction/PromptPlate"));
            Assert.IsTrue(root.Find("WarningMessage").gameObject.activeSelf, "The staged refusal must be live.");
            Assert.IsFalse(toast.Overlaps(moment), "Announcement overlaps the earned moment.");
            Assert.IsFalse(moment.Overlaps(warning), "Earned moment overlaps the refusal.");
            Assert.IsFalse(toast.Overlaps(warning), "Announcement overlaps the refusal.");
            Assert.IsFalse(warning.Overlaps(action), "Refusal overlaps the action.");
            foreach (var chip in root.GetComponentsInChildren<RectTransform>().Where(t => t.name.StartsWith("StatusChip")))
            {
                var b = Bounds(root, chip);
                Assert.IsTrue(root.rect.Contains(b.min) && root.rect.Contains(b.max), chip.name + " left the screen.");
                Assert.IsFalse(b.Overlaps(action) || b.Overlaps(warning), chip.name + " overlaps a central lane.");
            }
            foreach (var name in root.Find("MatchScores").GetComponentsInChildren<Text>().Where(t => t.name == "PlayerName" && t.enabled))
                Assert.LessOrEqual(name.preferredWidth, name.rectTransform.rect.width + 1, "Card name overflows: " + name.text);
        }

        private static Rect Bounds(RectTransform root, RectTransform target)
        {
            var points = new Vector3[4]; target.GetWorldCorners(points);
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity), max = -min;
            foreach (var point in points) { Vector2 p = root.InverseTransformPoint(point); min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
