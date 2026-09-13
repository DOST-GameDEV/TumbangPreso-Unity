using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class TumpNativeResultTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [UnityTest]
        public IEnumerator ResultsKeepStandingsPlayersMapChoiceAndRealRematch()
        {
            SceneFlow.Networked = false; SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            yield return SceneManager.LoadSceneAsync("Eskinita"); yield return new WaitForSecondsRealtime(.4f);
            var result = Object.FindFirstObjectByType<MatchResult>(); Assert.IsNotNull(result);
            result.OnMatchWon(-1); yield return null;
            var canvas = GameObject.Find("TumpResultCanvas").GetComponent<Canvas>();
            Assert.IsEmpty(canvas.GetComponentsInChildren<PaperSkin>(true));
            Assert.AreEqual(4, canvas.GetComponentsInChildren<Image>().Count(i => i.name.StartsWith("FinisherPortrait") && i.enabled && i.sprite != null));
            Assert.That(canvas.GetComponentsInChildren<Text>().First(t => t.name == "ResultHeadline").text, Does.Contain("draw"));
            yield return TumpUiCapture.Capture("NativeResults-standings-v1", canvas, 1920, 1080);
            Press("ResultTab2"); yield return null;
            Assert.AreEqual(4, canvas.GetComponentsInChildren<Text>().Count(t => t.name == "RecentPlayerName"));
            yield return TumpUiCapture.Capture("NativeResults-players-v1", canvas, 1280, 960);
            Press("ResultTab1"); yield return null;
            yield return TumpUiCapture.Capture("NativeResults-details-v1", canvas, 1280, 720);
            int next = (System.Array.IndexOf(SceneFlow.Maps, SceneFlow.SelectedMap) + 1) % SceneFlow.Maps.Length;
            result.HostReceiveMapVote(GameLaunch.SoloSeat, next); yield return null;
            Assert.That(Find("ResultNextMap").GetComponentInChildren<Text>().text, Does.Contain(SceneFlow.PreviewFor(SceneFlow.Maps[next]).Name));
            result.RequestRematch(); yield return null;
            Assert.IsFalse(result.IsVisible);
            Assert.That(Time.timeScale, Is.EqualTo(1).Within(.001));
        }
        [UnityTest]
        public IEnumerator AllExistingRankTiersHaveEditableVisibleEmblems()
        {
            var owner = new GameObject("RankDesignReview");
            try
            {
                var canvas = TumpUiFactory.Canvas(owner.transform, "RankReviewCanvas", 100);
                TumpUiFactory.Ground(canvas.transform, TumpUiTheme.Current.Cream);
                var title = TumpUiFactory.Text(canvas.transform, "Heading", "Rank emblem review", 62, true);
                TumpUiFactory.Place(title.rectTransform, 104, 92, 1680, 110);
                for (int i = 0; i < RatingRules.TierNames.Length; i++)
                {
                    var badge = TumpUiFactory.Rect(canvas.transform, "Rank" + i).gameObject.AddComponent<TumpRankBadge>();
                    badge.Tier = i; badge.raycastTarget = false;
                    TumpUiFactory.Place(badge.rectTransform, 104 + i * 348, 348, 270, 270);
                    var label = TumpUiFactory.Text(canvas.transform, "TierName" + i, RatingRules.TierName((RankTier)i), 36, true);
                    label.alignment = TextAnchor.MiddleCenter; TumpUiFactory.Place(label.rectTransform, 72 + i * 348, 678, 338, 86);
                }
                yield return TumpUiCapture.Capture("NativeRanks-v1", canvas, 1920, 1080);
                var counts = canvas.GetComponentsInChildren<TumpRankBadge>().Select(b => b.canvasRenderer.GetMesh().vertexCount).ToArray();
                Assert.IsTrue(counts.All(c => c > 0), "Each rank must produce actual rendered geometry; visual distinction is reviewed in the capture.");
            }
            finally { Object.DestroyImmediate(owner); }
        }
        private static Button Find(string name) => Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name == name && b.isActiveAndEnabled);
        private static void Press(string name)
        {
            var button = Find(name); Canvas.ForceUpdateCanvases(); var rect = (RectTransform)button.transform;
            var canvas = button.GetComponentInParent<Canvas>(); var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)) };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.IsNotEmpty(hits); Assert.AreEqual(button, hits[0].gameObject.GetComponentInParent<Button>(), name);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
    }
}
