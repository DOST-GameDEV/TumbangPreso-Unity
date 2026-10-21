using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.InputLayer;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
        [UnityTest, Timeout(90000)]
        public IEnumerator RealMatchEndParksTouchAndLeavesOverlayResultActionsHittable()
        {
            Assert.IsTrue(System.Environment.GetCommandLineArgs().Any(arg =>
                string.Equals(arg, "-tp-profile", System.StringComparison.OrdinalIgnoreCase)),
                "The result-touch check needs a named isolated profile.");

            bool networked = SceneFlow.Networked;
            string map = SceneFlow.SelectedMap;
            var rules = SceneFlow.SelectedRules.Clone();
            bool pinned = SceneFlow.RulesPinned;
            bool allBots = GameLaunch.AllBots;
            bool spectator = GameLaunch.Spectator;
            int soloSeat = GameLaunch.SoloSeat;
            bool forceVisible = TouchHud.ForceVisible;
            bool touchActive = TouchInput.Active;
            var inputSettings = InputSystem.settings;
            var background = inputSettings.backgroundBehavior;
            var editorInput = inputSettings.editorInputBehaviorInPlayMode;
            Touchscreen createdTouchscreen = null;

            try
            {
                SceneFlow.Networked = false;
                SceneFlow.SelectedMap = SceneFlow.Eskinita;
                var oneRound = CustomGameRules.Defaults(GameMode.Classic);
                oneRound.Rounds = 1;
                SceneFlow.PinSelectedRules(oneRound);
                GameLaunch.AllBots = false;
                GameLaunch.Spectator = false;
                GameLaunch.SoloSeat = 1;
                TouchHud.ForceVisible = false;

                if (Touchscreen.current == null)
                {
                    inputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                    inputSettings.editorInputBehaviorInPlayMode =
                        InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                    createdTouchscreen = InputSystem.AddDevice<Touchscreen>();
                    InputSystem.EnableDevice(createdTouchscreen);
                }
                Assert.IsTrue(TouchHud.ShouldShow, "The touchscreen did not select the ordinary touch path.");

                yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
                yield return new WaitForSecondsRealtime(.4f);
                var runner = Object.FindFirstObjectByType<SliceRunner>();
                var gate = Object.FindFirstObjectByType<ReadyGate>();
                Assert.IsNotNull(runner);
                Assert.IsNotNull(gate, "The arena did not offer its normal READY gate.");
                if (!GameServices.Round.RoundActive) gate.StartLocalCountdown();
                float until = Time.realtimeSinceStartup + 6f;
                while (Time.realtimeSinceStartup < until &&
                    (!GameServices.Round.RoundActive || gate.AwaitingReady || gate.CountingDown))
                    yield return null;
                Assert.IsTrue(GameServices.Round.RoundActive, "The READY countdown never began the round.");
                Assert.IsFalse(gate.AwaitingReady, "The READY prompt remained open over the live round.");
                Assert.IsFalse(gate.CountingDown, "The READY countdown remained open over the live round.");
                yield return null;

                var match = GameServices.Match;
                var local = GameServices.Round.PlayerAt(1);
                var result = Object.FindFirstObjectByType<MatchResult>();
                var touch = TouchHud.Instance;
                Assert.IsNotNull(local);
                Assert.IsNotNull(result);
                Assert.IsNotNull(touch, "The arena did not install touch controls for its touchscreen.");
                Assert.IsTrue(match.MatchInProgress);
                Assert.AreEqual(1, match.RoundNumber);
                Assert.AreEqual(1, match.TotalRounds);
                Assert.IsTrue(runner.Running);
                Assert.IsFalse(local.Intent.Parked, "The ordinary round never reached an unparked local seat.");
                Assert.IsTrue(touch.ShouldBeOnScreen);
                Assert.IsTrue(touch.Canvas.enabled, "Touch controls were not visible during the live round.");
                Assert.IsFalse(result.IsVisible);

                GameServices.Round.EndRound();
                match.BeginIntermission();
                yield return null;
                yield return null;

                Assert.IsFalse(match.MatchInProgress);
                Assert.IsFalse(runner.Running, "SliceRunner did not receive the match-end event.");
                Assert.IsTrue(local.Intent.Parked, "The match-end event did not park local input.");
                Assert.IsTrue(result.IsVisible, "The director event did not present the result board.");
                Assert.IsFalse(touch.ShouldBeOnScreen);
                Assert.IsFalse(touch.Canvas.enabled, "Parked touch controls still intercept the result board.");

                var canvasField = typeof(MatchResult).GetField("_canvas",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.IsNotNull(canvasField);
                var resultCanvas = (Canvas)canvasField.GetValue(result);
                Assert.IsNotNull(resultCanvas, "The visible MatchResult lost its own canvas.");
                var activeResults = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                    FindObjectsSortMode.None).Where(c => c.name == "OwnerResultCanvas" && c.isActiveAndEnabled).ToArray();
                Assert.AreEqual(1, activeResults.Length, "More than one active result canvas could alter the top hit.");
                Assert.AreSame(resultCanvas, activeResults[0], "The active result canvas is not the board's own canvas.");
                Assert.AreEqual(RenderMode.ScreenSpaceOverlay, touch.Canvas.renderMode);
                Assert.AreEqual(RenderMode.ScreenSpaceOverlay, resultCanvas.renderMode);
                Debug.Log("[ResultTouch] canvas=" + resultCanvas.name +
                    " override=" + resultCanvas.overrideSorting +
                    " order=" + resultCanvas.sortingOrder +
                    " root=" + (resultCanvas.rootCanvas != null ? resultCanvas.rootCanvas.name : "none") +
                    " parent=" + (resultCanvas.transform.parent != null ? resultCanvas.transform.parent.name : "none") +
                    " touchOrder=" + touch.Canvas.sortingOrder);
                var system = EventSystem.current;
                Assert.IsNotNull(system);
                Canvas.ForceUpdateCanvases();
                foreach (string name in new[] { "ResultTab0", "ResultTab1", "ResultTab2",
                    "ResultRematch", "ResultNextMap", "ResultMainMenu" })
                {
                    var button = Find(name);
                    var rect = ScreenFocus.HitRectOf(button);
                    Vector2 point = RectTransformUtility.WorldToScreenPoint(null,
                        rect.TransformPoint(rect.rect.center));
                    var hits = new List<RaycastResult>();
                    system.RaycastAll(new PointerEventData(system) { position = point }, hits);
                    Assert.IsNotEmpty(hits, name + " had no Overlay raycast hit.");
                    Assert.AreSame(button, hits[0].gameObject.GetComponentInParent<Button>(),
                        name + " was intercepted by " + hits[0].gameObject.name);
                    Assert.IsFalse(hits.Any(hit => hit.gameObject.transform.IsChildOf(touch.Canvas.transform)),
                        name + " still raycast through the disabled touch canvas.");
                }
            }
            finally
            {
                if (createdTouchscreen != null && createdTouchscreen.added)
                    InputSystem.RemoveDevice(createdTouchscreen);
                inputSettings.backgroundBehavior = background;
                inputSettings.editorInputBehaviorInPlayMode = editorInput;
                TouchInput.ReleaseAll();
                TouchInput.Active = touchActive;
                TouchHud.ForceVisible = forceVisible;
                GameLaunch.AllBots = allBots;
                GameLaunch.Spectator = spectator;
                GameLaunch.SoloSeat = soloSeat;
                SceneFlow.Networked = networked;
                SceneFlow.SelectedMap = map;
                SceneFlow.AdoptRemoteRules(rules);
                SceneFlow.SelectedRules.Password = rules.Password;
                if (pinned) SceneFlow.PinSelectedRules(rules);
                else SceneFlow.UnpinSelectedRules();
            }
        }
        [Test]
        public void PracticeSummaryDoesNotClaimAnOlderQueuedMatchWillUpload()
        {
            var priorOnlineQueue = new List<MatchRecord>
            {
                new MatchRecord { MatchId = "older-online-pending", Online = true }
            };
            var current = new MatchRecord { MatchId = "practice-result-copy", Online = false };
            Assert.AreEqual("", MatchResult.UploadCopyFor(current, priorOnlineQueue.Count),
                "Practice must not claim that its own result will upload because another match is queued.");

            current.Online = true;
            StringAssert.Contains("WILL UPLOAD", MatchResult.UploadCopyFor(current, priorOnlineQueue.Count));
            Assert.AreEqual("", MatchResult.UploadCopyFor(current, 0));
            Assert.AreEqual("", MatchResult.UploadCopyFor(null, priorOnlineQueue.Count));
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator LateCareerAcknowledgementUpdatesTheOpenResultDetails()
        {
            Assert.IsTrue(System.Environment.GetCommandLineArgs().Contains("-tp-profile"));
            SceneFlow.Networked = false;
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
            yield return new WaitForSecondsRealtime(.4f);
            var result = Object.FindFirstObjectByType<MatchResult>(); Assert.IsNotNull(result);
            var career = GameServices.Career; Assert.IsNotNull(career);
            Assert.IsFalse(GameServices.Account?.IsSignedIn ?? false, "This result check stays offline.");
            result.OnMatchWon(1); yield return null;
            var record = new MatchRecord {
                MatchId = System.Guid.NewGuid().ToString("N"), Online = true,
                Mode = "Classic", MapId = SceneFlow.Eskinita, Rounds = 8,
                DurationSeconds = 720, WinningSlot = 1, PlayedUtc = "2026-10-02T00:00:00Z",
                Players = new PlayerMatchStats[4] };
            for (int seat = 0; seat < 4; seat++) record.Players[seat] = new PlayerMatchStats {
                Slot = seat, PlayerId = seat == 1 ? TumbangPreso.Net.CareerStore.LocalPlayerId : "result-only-" + seat,
                IsBot = seat != 1, Placement = seat == 1 ? 1 : seat + 1,
                ActiveRounds = 8, Throws = 3, Tags = 2, Score = seat == 1 ? 500 : 100 };
            GameServices.Stats.Adopt(record); yield return null;
            Assert.IsNotNull(career.LastAward, "The local record must have a payable line before testing its UI acknowledgement.");
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var detail = (Text)typeof(MatchResult).GetField("_xpDetail", flags).GetValue(result);
            Assert.IsNotNull(detail); StringAssert.DoesNotContain("THIS RESULT DID NOT MATCH", detail.text);
            var cache = typeof(TumbangPreso.Net.CareerStore).GetField("_cache", flags).GetValue(career);
            typeof(TumbangPreso.Net.CareerStore).GetMethod("CompleteSubmission", flags)
                .Invoke(career, new object[] { cache, record, "{\"verdict\":\"disputed\"}" });
            yield return null;
            StringAssert.Contains("THIS RESULT DID NOT MATCH", detail.text,
                "The current match acknowledgement never reached the already-open result details.");
            var changed = (System.Delegate)typeof(TumbangPreso.Net.CareerStore).GetField("Changed", flags).GetValue(career);
            Assert.AreEqual(1, changed.GetInvocationList().Count(handler => ReferenceEquals(handler.Target, result)));
            result.enabled = false; yield return null;
            changed = (System.Delegate)typeof(TumbangPreso.Net.CareerStore).GetField("Changed", flags).GetValue(career);
            Assert.AreEqual(0, changed?.GetInvocationList().Count(handler => ReferenceEquals(handler.Target, result)) ?? 0,
                "The retired result board retained its career callback.");
        }

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
        public IEnumerator RematchActuallyLoadsTheChosenArena()
        {
            SceneFlow.Networked=false;SceneFlow.SelectedMap=SceneFlow.Eskinita;
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);yield return new WaitForSecondsRealtime(.4f);
            var result=Object.FindFirstObjectByType<MatchResult>();result.OnMatchWon(0);yield return null;
            int next=System.Array.IndexOf(SceneFlow.Maps,SceneFlow.BayanPlaza);
            result.HostReceiveMapVote(GameLaunch.SoloSeat,next);result.RequestRematch();yield return null;
            Assert.AreEqual(SceneFlow.BayanPlaza,SceneFlow.SelectedMap);
            // UX-1 now enters offline arenas through HubLoading.LoadSceneAsync.
            // One frame is not load completion; keep the real arena assertion.
            float deadline=Time.realtimeSinceStartup+30;int frames=0;
            while((SceneManager.GetActiveScene().name!=SceneFlow.BayanPlaza||Object.FindFirstObjectByType<ReadyGate>()==null)
                &&Time.realtimeSinceStartup<deadline&&frames++<ProbeWait.MaxFrames)yield return null;
            Assert.AreEqual(SceneFlow.BayanPlaza,SceneManager.GetActiveScene().name,"Rematch selected the next map but kept playing on the old court.");
            Assert.IsNotNull(Object.FindFirstObjectByType<ReadyGate>());
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
            // VISUAL-1.16: the other three seats get lines too, so every chip has accolades to draw.
            var others=new[]{
                new PlayerMatchStats{Slot=0,PlayerId="fixture-0",Tags=3,Retrievals=2,RetrievalsUnderPressure=1,Score=420},
                new PlayerMatchStats{Slot=2,PlayerId="fixture-2",Retrievals=5,Score=300},
                new PlayerMatchStats{Slot=3,PlayerId="fixture-3",Sabotages=2,Retrievals=3,RetrievalsUnderPressure=2,Score=250}};
            var record=new MatchRecord{MatchId="results-presentation-fixture",Mode=GameMode.HeroStrike.ToString(),Rounds=8,
                WinningSlot=1,Players=new[]{line,others[0],others[1],others[2]}};
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
            // VISUAL-1.16: the standings show two record-backed accolades per player, glyph and number.
            Press("ResultTab0");yield return null;
            var counts=canvas.GetComponentsInChildren<Text>().Where(t=>t.name.StartsWith("AccoladeCount")&&t.enabled).Select(t=>t.text).ToList();
            CollectionAssert.Contains(counts,"4","Zack's four knockdowns are his first accolade.");
            Assert.AreEqual(7,counts.Count,"Two accolades each, except seat 2 whose only non-zero stat is retrievals.");
            foreach(var size in new[]{new Vector2Int(1920,1080),TumpUiCapture.OwnerWindow,new Vector2Int(960,540)})
                yield return TumpUiCapture.Capture("FinishSheet-podium-fixture-"+size.x+"x"+size.y,canvas,size.x,size.y,false,true,checkActionBounds:true);
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
