using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class HomeModeCardReadabilityTests
    {
        private int _choice;
        private Core.GameMode _mode;
        private Core.CustomRules _rules;
        private bool _pinned;
        [UnitySetUp] public IEnumerator Before()
        {
            _choice = HubHome.Choice;
            _mode = SceneFlow.SelectedMode;
            _rules = SceneFlow.SelectedRules.Clone(); _pinned = SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            HubHome.Choice = _choice;
            yield return PlayModeWorld.Reset();
            SceneFlow.SelectedMode = _mode;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }

        private static Rect InCard(Transform card, RectTransform child)
        {
            var corners = new Vector3[4]; child.GetWorldCorners(corners);
            var points = corners.Select(card.InverseTransformPoint).ToArray();
            return Rect.MinMaxRect(points.Min(p => p.x), points.Min(p => p.y),
                points.Max(p => p.x), points.Max(p => p.y));
        }

        [UnityTest] public IEnumerator ActualModePostersStayClearOfTheHeadlineAt720And1080()
        {
            yield return HubFlowTests.OpenHome();
            var hub = TumpHub.Current; var conflicts = new List<string>();
            foreach (int choice in new[] { 0, 1, 2 })
            {
                HubHome.Choice = choice; hub.Top.Resumed();
                yield return null; Canvas.ForceUpdateCanvases();
                var card = hub.Canvas.GetComponentsInChildren<Button>().Single(b => b.name == "ModeCard");
                var window = card.GetComponentsInChildren<RectTransform>().Single(r => r.name == "PosterWindow");
                var title = card.GetComponentsInChildren<Text>().Single(t => t.name == "ModeTitle");
                var poster = window.GetComponentsInChildren<Image>().Single(i => i.name == "Poster");
                Assert.IsNotNull(poster.sprite, "The selected mode poster is missing.");
                Assert.AreEqual(choice == 0 ? "RANKED" : "CASUAL", title.text);
                foreach (int width in new[] { 1280, 1920 })
                    yield return TumpUiCapture.Capture("Home-mode-card-" + choice + "-" + width,
                        hub.Canvas, width, width == 1280 ? 720 : 1080, false,
                        checkActionBounds: true, inspectViewport: () =>
                        {
                            if (InCard(card.transform, window).Overlaps(InCard(card.transform, title.rectTransform)))
                                conflicts.Add("mode " + choice + " at " + width + ": poster crosses headline area");
                        });
            }
            Assert.IsEmpty(conflicts, string.Join("; ", conflicts));
        }

        [UnityTest] public IEnumerator ModeCardStillOpensItsChoiceDoorAndBackKeepsTheSelection()
        {
            HubHome.Choice = 2;
            yield return HubFlowTests.OpenHome();
            yield return HubFlowTests.Press("ModeCard");
            Assert.IsInstanceOf<HubModeSelect>(TumpHub.Current.Top);
            TumpHub.Current.Back(); yield return new WaitForSecondsRealtime(.3f);
            Assert.IsInstanceOf<HubHome>(TumpHub.Current.Top);
            Assert.AreEqual(2, HubHome.Choice);
        }
    }
}
