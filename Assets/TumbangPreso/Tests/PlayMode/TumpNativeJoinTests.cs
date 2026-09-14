using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class TumpNativeJoinTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [UnityTest]
        public IEnumerator NativeJoinShowsSourceListsErrorsAndAnActualBackPath()
        {
            var owner = new GameObject("JoinReviewOwner");
            try
            {
                var panel = LobbyJoinPanel.Build(owner.transform, null); panel.Open(); yield return null;
                var canvas = GameObject.Find("OwnerJoinCanvas").GetComponent<Canvas>();
                Assert.IsEmpty(canvas.GetComponentsInChildren<PaperSkin>(true));
                Press("ConnectToRoom"); yield return null;
                Assert.That(canvas.GetComponentsInChildren<Text>().First(t => t.name == "JoinStatus").text, Does.Contain("Enter"));
                yield return TumpUiCapture.Capture("OwnerJoin-nearby-v1", canvas, 1920, 1080, false);
                Press("OnlineChip"); yield return null;
                Assert.IsNotNull(GameObject.Find("OnlineRooms"));
                yield return TumpUiCapture.Capture("OwnerJoin-online-v1", canvas, 1280, 960, false);
                Press("CloseJoinButton"); yield return null;
                Assert.IsFalse(panel.IsOpen); Assert.IsFalse(canvas.gameObject.activeSelf);
            }
            finally { Object.DestroyImmediate(owner); }
        }
        [UnityTest]
        public IEnumerator CancelledDelayedJoinCannotCloseOrCompleteTheNewerView()
        {
            var owner = new GameObject("JoinCancellationOwner");
            try
            {
                var panel = LobbyJoinPanel.Build(owner.transform, null);
                var older = new TaskCompletionSource<bool>(); var newer = new TaskCompletionSource<bool>();
                int joined = 0; panel.Joined += () => joined++;
                panel.Connection = (_, token) => older.Task;
                panel.Open(); var first = panel.AutomationJoin("ABCD"); yield return null;
                Press("CloseJoinButton"); yield return null;
                panel.Connection = (_, token) => newer.Task;
                panel.Open(); var second = panel.AutomationJoin("EFGH"); yield return null;
                older.SetResult(true); yield return null;
                Assert.IsTrue(first.IsCompleted); Assert.IsFalse(first.Result);
                Assert.IsTrue(panel.IsOpen); Assert.AreEqual(0, joined);
                Assert.IsFalse(Find("ConnectToRoom").interactable, "Old cleanup must not release a newer busy state.");
                newer.SetResult(true); yield return null;
                Assert.IsTrue(second.IsCompleted); Assert.IsTrue(second.Result);
                Assert.AreEqual(1, joined); Assert.IsFalse(panel.IsOpen);
            }
            finally { Object.DestroyImmediate(owner); }
        }
        [UnityTest]
        public IEnumerator QueueRemainsNonmodalAndItsCancelReturnsTheStartAction()
        {
            var owner = new GameObject("QueueReviewOwner");
            try
            {
                var canvas = OwnerUiLayout.Canvas(owner.transform, "QueueReviewCanvas", 100);
                OwnerUiBackdrop.Build(canvas.transform);
                bool underlyingUsed = false;
                OwnerTextAction.Create(canvas.transform, "UnderlyingLoadout", "LOADOUT", () => underlyingUsed = true,70,100,340,94,30);
                var card = QueueCard.Build(canvas.transform);
                var queue = Matchmaker.Current; queue.enabled = false;
                typeof(Matchmaker).GetProperty("State").GetSetMethod(true).Invoke(queue, new object[] { QueueState.Searching });
                typeof(Matchmaker).GetProperty("Mode").GetSetMethod(true).Invoke(queue, new object[] { GameMode.HeroStrike });
                typeof(Matchmaker).GetProperty("Elapsed").GetSetMethod(true).Invoke(queue, new object[] { 37f });
                typeof(QueueCard).GetMethod("Refresh", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(card, null);
                yield return null;
                Press("UnderlyingLoadout"); Assert.IsTrue(underlyingUsed, "Searching must not block the rest of preparation.");
                yield return TumpUiCapture.Capture("OwnerQueue-v1", canvas, 1920, 1080,false);
                Press("CancelQueueButton"); yield return null;
                Assert.IsFalse(card.IsQueueing); Assert.IsTrue(Find("QuickMatchButton").interactable);
            }
            finally { Object.DestroyImmediate(owner); }
        }
        private static Button Find(string name) => Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name == name && b.isActiveAndEnabled);
        private static void Press(string name)
        {
            var button = Find(name); Canvas.ForceUpdateCanvases(); var rect = (RectTransform)button.transform;
            var canvas = button.GetComponentInParent<Canvas>(); var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var p = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)) };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(p, hits);
            Assert.IsNotEmpty(hits, name); Assert.AreEqual(button, hits[0].gameObject.GetComponentInParent<Button>(), name + " covered");
            ExecuteEvents.Execute(button.gameObject, p, ExecuteEvents.pointerClickHandler);
        }
    }
}
