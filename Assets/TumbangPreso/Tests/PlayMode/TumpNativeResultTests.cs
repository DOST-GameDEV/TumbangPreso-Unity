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
            => ReviewResults(GameMode.Classic);
        [UnityTest]
        public IEnumerator HeroResultsKeepStandingsPlayersMapChoiceAndRealRematch()
            => ReviewResults(GameMode.HeroStrike);
        private static IEnumerator ReviewResults(GameMode mode)
        {
            SceneFlow.Networked = false; SceneFlow.SetSelectedRules(CustomGameRules.Defaults(mode));
            yield return SceneManager.LoadSceneAsync("Eskinita"); yield return new WaitForSecondsRealtime(.4f);
            var result = Object.FindFirstObjectByType<MatchResult>(); Assert.IsNotNull(result);
            result.OnMatchWon(-1); yield return null;
            var canvas = GameObject.Find("OwnerResultCanvas").GetComponent<Canvas>();
            Assert.IsEmpty(canvas.GetComponentsInChildren<PaperSkin>(true));
            Assert.AreEqual(4, canvas.GetComponentsInChildren<Image>().Count(i => i.name.StartsWith("FinisherPortrait") && i.enabled && i.sprite != null));
            Assert.That(canvas.GetComponentsInChildren<Text>().First(t => t.name == "ResultHeadline").text, Does.Contain("draw"));
            foreach(var size in TumpUiCapture.PcViewports)
                yield return TumpUiCapture.Capture("FinishSheet-"+mode+"-standings-"+size.x+"x"+size.y,canvas,size.x,size.y,false,checkActionBounds:true);
            Press("ResultTab2"); yield return null;
            Assert.AreEqual(4, canvas.GetComponentsInChildren<Text>().Count(t => t.name == "RecentPlayerName"));
            foreach(var size in TumpUiCapture.PcViewports)
                yield return TumpUiCapture.Capture("FinishSheet-"+mode+"-players-"+size.x+"x"+size.y,canvas,size.x,size.y,false,checkActionBounds:true);
            Press("ResultTab1"); yield return null;
            Assert.IsTrue(canvas.GetComponentsInChildren<Text>().Any(t=>
                (t.name=="EmptyMatchDetails" || t.name=="YourMatchSummary" || t.name=="EarnedXp" || t.name=="MatchHighlight")
                && !string.IsNullOrWhiteSpace(t.text)),"Details needs a report or a clear empty state.");
            yield return TumpUiCapture.Capture("FinishSheet-"+mode+"-empty-details",canvas,960,540,false,checkActionBounds:true);
            // Local presentation fixture only. This does not award XP or write a career.
            typeof(MatchResult).GetMethod("ShowProgression",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                .Invoke(result,new object[]{new XpAward{MatchXp=175,LevelBefore=2,LevelAfter=3},new PlayerProfile{Xp=2750}});
            Assert.That(canvas.GetComponentsInChildren<Text>().First(t=>t.name=="EarnedXp").text,Does.Contain("175 XP"));
            foreach(var size in TumpUiCapture.PcViewports)
                yield return TumpUiCapture.Capture("FinishSheet-"+mode+"-reward-fixture-"+size.x+"x"+size.y,canvas,size.x,size.y,false,checkActionBounds:true);
            int next = (System.Array.IndexOf(SceneFlow.Maps, SceneFlow.SelectedMap) + 1) % SceneFlow.Maps.Length;
            result.HostReceiveMapVote(GameLaunch.SoloSeat, next); yield return null;
            Assert.That(Find("ResultNextMap").GetComponentInChildren<Text>().text, Does.Contain(SceneFlow.PreviewFor(SceneFlow.Maps[next]).Name));
            result.RequestRematch(); yield return null;
            Assert.IsFalse(result.IsVisible);
            Assert.That(Time.timeScale, Is.EqualTo(1).Within(.001));
        }
        [UnityTest]
        public IEnumerator PopulatedResultSummaryAndRewardBreakdownRemainReadable()
        {
            SceneFlow.Networked=false;SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            yield return SceneManager.LoadSceneAsync("Eskinita");yield return new WaitForSecondsRealtime(.4f);
            var result=Object.FindFirstObjectByType<MatchResult>();result.OnMatchWon(1);yield return null;
            // Presentation-only record. It is never submitted or applied to the saved career.
            var line=new PlayerMatchStats{Slot=1,PlayerId=Net.CareerStore.LocalPlayerId,CharacterId="zack",Placement=1,
                Knockdowns=4,Retrievals=6,Tags=2,DefenceTicks=74,Score=850};
            var record=new MatchRecord{MatchId="results-presentation-fixture",Mode=GameMode.HeroStrike.ToString(),Rounds=8,
                WinningSlot=1,Players=new[]{line}};
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(MatchResult).GetMethod("OnRecordReady",flags).Invoke(result,new object[]{record});
            var profile=new PlayerProfile{Xp=1930};ProgressionRules.MasteryFor(profile,"zack").Xp=1930;
            var award=ProgressionRules.Award(profile,record,line);
            Assert.AreEqual(ProgressionRules.Breakdown(record,line).Sum(part=>part.Xp),award.MatchXp);
            typeof(MatchResult).GetMethod("ShowProgression",flags).Invoke(result,new object[]{award,profile});
            Press("ResultTab1");yield return null;
            var canvas=GameObject.Find("OwnerResultCanvas").GetComponent<Canvas>();
            Assert.That(canvas.GetComponentsInChildren<Text>().First(t=>t.name=="YourMatchSummary").text,Does.Contain("6 RETRIEVALS"));
            Assert.That(canvas.GetComponentsInChildren<Text>().First(t=>t.name=="RewardDetails").text,Does.Contain("MASTERY"));
            foreach(var size in new[]{new Vector2Int(960,540),new Vector2Int(1280,960),new Vector2Int(1920,1080),new Vector2Int(3840,2160)})
                yield return TumpUiCapture.Capture("FinishSheet-populated-fixture-"+size.x+"x"+size.y,canvas,size.x,size.y,false,checkActionBounds:true);
        }
        [UnityTest]
        public IEnumerator AllExistingRankTiersHaveEditableVisibleEmblems()
        {
            var owner = new GameObject("RankDesignReview");
            try
            {
                var canvas = OwnerUiLayout.Canvas(owner.transform, "RankReviewCanvas", 100);
                OwnerUiBackdrop.Build(canvas.transform);
                var title = OwnerUiLayout.Text(canvas.transform, "Heading", "RANK EMBLEMS", 62,OwnerUiLayout.TypeRole.Display);
                OwnerUiLayout.Place(title.rectTransform, 104, 92, 1680, 110);
                for (int i = 0; i < RatingRules.TierNames.Length; i++)
                {
                    var badge = OwnerUiLayout.Rect(canvas.transform, "Rank" + i).gameObject.AddComponent<TumpRankBadge>();
                    badge.Tier = i; badge.raycastTarget = false;
                    OwnerUiLayout.Place(badge.rectTransform, 104 + i * 348, 348, 270, 270);
                    var label = OwnerUiLayout.Text(canvas.transform, "TierName" + i, RatingRules.TierName((RankTier)i), 36,OwnerUiLayout.TypeRole.Display);
                    label.alignment = TextAnchor.MiddleCenter; OwnerUiLayout.Place(label.rectTransform, 72 + i * 348, 678, 338, 86);
                }
                yield return TumpUiCapture.Capture("OwnerRanks-v1", canvas, 1920, 1080,false);
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
