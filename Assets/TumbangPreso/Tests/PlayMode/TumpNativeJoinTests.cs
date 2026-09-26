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
        private sealed class ClientProvider : INetProvider
        {
            public bool IsNetworked { get; set; } = true;
            public bool IsHost { get; set; }
            public int LocalSlot => 1;
            public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }

        private static JoinAttemptGate.Attempt BeginTitleOperation(NetSession net, bool internalShutdown = false)
        {
            var type = typeof(NetSession);
            var gate = (JoinAttemptGate)type.GetField("_joinAttempts", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(net);
            var attempt = gate.Begin();
            type.GetField("_clientTitleOperation", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(net, (JoinAttemptGate.Attempt?)attempt);
            type.GetMethod("ClearJoinedClientRoomTitle", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(net, null);
            if (internalShutdown)
                type.GetMethod("StopCurrentTransport", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(net, null);
            return attempt;
        }

        private static IEnumerator AwaitClientStart(Task<bool> task)
        {
            int frames = 0;
            float deadline = Time.realtimeSinceStartup + 20f;
            while (!task.IsCompleted)
            {
                if (frames++ > 600 || Time.realtimeSinceStartup > deadline)
                    throw new System.TimeoutException("client start exceeded 600 frames or 20 seconds");
                yield return null;
            }
            if (task.IsFaulted) throw task.Exception;
            Assert.IsTrue(task.Result, NetSession.Instance?.Status);
        }

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
                foreach (var size in TumpUiCapture.PcViewports)
                {
                    yield return TumpUiCapture.Capture("CourtJoin-nearby-" + size.x + "x" + size.y,
                        canvas, size.x, size.y, false, checkActionBounds: true);
                    var heading = canvas.GetComponentsInChildren<Text>().First(t => t.name == "JoinTitle");
                    Assert.That(heading.cachedTextGenerator.lines.Count, Is.EqualTo(2),
                        "The two-line JOIN A / ROOM heading must draw both lines.");
                    Assert.GreaterOrEqual(heading.cachedTextGenerator.characterCountVisible, heading.text.Length,
                        "The room heading lost characters despite reporting a fitted preferred height.");
                }
                Press("OnlineChip"); yield return null;
                Assert.IsNotNull(GameObject.Find("OnlineRooms"));
                foreach (var size in TumpUiCapture.PcViewports)
                    yield return TumpUiCapture.Capture("CourtJoin-online-" + size.x + "x" + size.y,
                        canvas, size.x, size.y, false, checkActionBounds: true);
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
        public IEnumerator LeavingTheHubCancelsItsJoinBeforeANewerRequestStarts()
        {
            var owner = new GameObject("HubJoinCancellationOwner");
            try
            {
                var host = owner.AddComponent<ConvertedMatchSetup>(); host.enabled = false;
                var panel = LobbyJoinPanel.Build(owner.transform, null);
                typeof(ConvertedMatchSetup).GetField("_joinPanel", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(host, panel);
                var older = new TaskCompletionSource<bool>(); var newer = new TaskCompletionSource<bool>();
                int joined = 0; panel.Joined += () => joined++;
                panel.Connection = (_, token) => older.Task;
                var first = host.Join("ABCD");
                yield return null;
                host.LeaveRoom();
                Assert.IsFalse(SceneFlow.Networked);
                panel.Connection = (_, token) => newer.Task;
                var second = host.Join("EFGH");
                Assert.IsFalse(second.IsCompleted, "The old panel busy state blocked the new request.");
                older.SetResult(true); yield return null;
                Assert.IsTrue(first.IsCompleted);
                Assert.AreEqual("Room request cancelled.", first.Result);
                Assert.AreEqual(0, joined, "The cancelled join fired the lobby arrival callback.");
                Assert.IsTrue(SceneFlow.Networked, "The old completion reset the newer request's network flag.");
                typeof(LobbyJoinPanel).GetMethod("Report", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(panel, new object[] { "The new room is full." });
                newer.SetResult(false); yield return null;
                Assert.IsTrue(second.IsCompleted);
                Assert.AreEqual("The new room is full.", second.Result, "Old cleanup unsubscribed the new request's status handler.");
                Assert.IsFalse(SceneFlow.Networked);
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator JoinedTitleUsesOnlyTheCurrentListedRoomAndItsSeatedCode()
        {
            string previousTitle = NetSession.RoomTitle;
            bool previousNetworked = SceneFlow.Networked;
            var owner = new GameObject("JoinTitleOwner");
            var net = NetSession.Ensure();
            var previousProvider = NetAuthority.Provider;
            var client = new ClientProvider();
            var seen = (Dictionary<string, LanEntry>)typeof(LanBeacon)
                .GetField("_seen", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(net.Beacon);
            var host = owner.AddComponent<ConvertedMatchSetup>(); host.enabled = false;
            var panel = LobbyJoinPanel.Build(owner.transform, null);
            typeof(ConvertedMatchSetup).GetField("_joinPanel", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(host, panel);
            void List(string address, string code, string title)
                => seen[address + ":8910"] = new LanEntry
                { Address = address, Port = 8910, JoinCode = code, HostName = title };
            try
            {
                NetSession.RoomTitle = "";
                net.Beacon.StopAll();
                net.Query?.StopBrowsing();
                NetAuthority.Provider = client;
                List("192.0.2.10", "A1BC", "OLD ROOM");
                var older = new TaskCompletionSource<bool>();
                JoinAttemptGate.Attempt olderAttempt = default;
                panel.Connection = (_, token) => { olderAttempt = BeginTitleOperation(net); return older.Task; };
                var first = host.Join("192.0.2.10:8910"); yield return null;
                Assert.IsEmpty(net.Beacon.SortedEntries,
                    "The join no longer reproduces the listing loss after LeaveRoom.");

                List("192.0.2.11", "B2CD", "CURRENT ROOM");
                var newer = new TaskCompletionSource<bool>();
                JoinAttemptGate.Attempt newerAttempt = default;
                panel.Connection = (_, token) => { newerAttempt = BeginTitleOperation(net, internalShutdown: true); return newer.Task; };
                var second = host.Join("192.0.2.11"); yield return null;
                Assert.IsFalse(olderAttempt.OwnsSession,"The old operation still owned the replaced transport.");
                Assert.IsTrue(newerAttempt.OwnsSession,"Internal shutdown rekeyed the new operation.");
                older.SetResult(true); yield return null;
                Assert.IsTrue(first.IsCompleted);
                Assert.AreEqual("Room request cancelled.", first.Result);
                net.Lobby.SetJoinCode("A1BC");
                Assert.AreEqual("", host.RoomTitle, "A cancelled request leaked its listed title.");
                net.Lobby.SetJoinCode("");

                newer.SetResult(true); yield return null;
                Assert.IsTrue(second.IsCompleted);
                Assert.AreEqual("", second.Result);
                Assert.AreEqual("", host.RoomTitle,"A pending title appeared before seating.");
                net.Lobby.SetJoinCode("B2CD");
                Assert.AreEqual("CURRENT ROOM", host.RoomTitle,
                    "The title did not promote when the matching seat arrived after join completion.");
                client.IsNetworked = false;
                Assert.AreEqual("", host.RoomTitle, "A disconnected client kept its joined title.");
                client.IsNetworked = true;
                client.IsHost = true;
                Assert.AreEqual("", host.RoomTitle, "A host read a previous client's listed title.");
                client.IsHost = false;
                Assert.AreEqual("CURRENT ROOM", host.RoomTitle);

                Object.DestroyImmediate(owner);
                owner = new GameObject("ReloadedJoinTitleOwner");
                host = owner.AddComponent<ConvertedMatchSetup>(); host.enabled = false;
                panel = LobbyJoinPanel.Build(owner.transform, null);
                typeof(ConvertedMatchSetup).GetField("_joinPanel", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(host, panel);
                Assert.AreEqual("CURRENT ROOM", host.RoomTitle,
                    "Reloading the preparation screen lost a title from the same seated session.");

                var cancelledOperation = net.ClientTitleOperation;
                net.CancelPendingOperation();
                net.RememberJoinedClientRoomTitle("STALE ROOM", "B2CD", cancelledOperation);
                Assert.AreEqual("", host.RoomTitle, "Cancelling a session operation retained its client title.");
                host.LeaveRoom();
                Assert.AreEqual("", host.RoomTitle, "Leaving retained the joined room's title.");

                List("192.0.2.12", "C3DE", "WRONG SEAT ROOM");
                panel.Connection = (_, token) => { BeginTitleOperation(net); return Task.FromResult(true); };
                var wrong = host.Join("192.0.2.12:8910"); yield return null;
                Assert.IsTrue(wrong.IsCompleted);
                Assert.AreEqual("",wrong.Result);
                net.Lobby.SetJoinCode("WRNG");
                Assert.AreEqual("", host.RoomTitle,"A mismatched seat accepted the pending title.");
                net.Lobby.SetJoinCode("C3DE");
                Assert.AreEqual("", host.RoomTitle,"A rejected wrong-code title revived later.");

                host.LeaveRoom();
                List("192.0.2.13", "D4EF", "EARLY SEAT ROOM");
                var early = new TaskCompletionSource<bool>();
                panel.Connection = (_, token) => { BeginTitleOperation(net); return early.Task; };
                var beforeSeat = host.Join("192.0.2.13:8910"); yield return null;
                net.Lobby.SetJoinCode("D4EF");
                early.SetResult(true); yield return null;
                Assert.IsTrue(beforeSeat.IsCompleted);
                Assert.AreEqual("",beforeSeat.Result);
                Assert.AreEqual("EARLY SEAT ROOM",host.RoomTitle,
                    "A seat arriving before the panel completion did not promote immediately.");

                host.LeaveRoom();
                List("192.0.2.14", "E5FG", "REPLACED ROOM");
                var stale = new TaskCompletionSource<bool>();
                panel.Connection = (_, token) => { BeginTitleOperation(net); return stale.Task; };
                var staleJoin = host.Join("192.0.2.14:8910"); yield return null;
                BeginTitleOperation(net);
                stale.SetResult(true); yield return null;
                Assert.IsTrue(staleJoin.IsCompleted);
                Assert.AreEqual("",staleJoin.Result,
                    "The older panel did not actually return a successful callback for the stale-operation check.");
                net.Lobby.SetJoinCode("E5FG");
                Assert.AreEqual("",host.RoomTitle,
                    "An old successful panel callback labeled a newer direct session operation.");

                host.LeaveRoom();
                panel.Connection = (_, token) => { BeginTitleOperation(net); return Task.FromResult(true); };
                var bare = host.Join("203.0.113.20:8910"); yield return null;
                Assert.IsTrue(bare.IsCompleted);
                net.Lobby.SetJoinCode("C3DE");
                Assert.AreEqual("", host.RoomTitle,
                    "A bare address without a listing invented a room title.");
            }
            finally
            {
                host.LeaveRoom();
                NetSession.RoomTitle = previousTitle;
                SceneFlow.Networked = previousNetworked;
                NetAuthority.Provider = previousProvider;
                Object.DestroyImmediate(owner);
            }
        }

        [UnityTest, Timeout(10000)]
        public IEnumerator AnUnsolicitedDisconnectClearsTheSessionOwnedTitle()
        {
            var net = NetSession.Ensure();
            var previousProvider = NetAuthority.Provider;
            string previousTitle = NetSession.RoomTitle;
            var client = new ClientProvider();
            try
            {
                net.Stop();
                NetSession.RoomTitle = "";
                NetAuthority.Provider = client;
                var operation = BeginTitleOperation(net);
                net.Lobby.SetJoinCode("B2CD");
                net.RememberJoinedClientRoomTitle("CURRENT ROOM", "B2CD", operation);
                Assert.AreEqual("CURRENT ROOM", net.JoinedClientRoomTitle);
                typeof(NetSession).GetField("_localShutdown", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(net, false);
                typeof(NetSession).GetMethod("OnClientDisconnected", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(net, new object[] { 0UL });
                net.Lobby.SetJoinCode("B2CD");
                Assert.AreEqual("", net.JoinedClientRoomTitle,
                    "A disconnected client reused its old title after the same code was restored.");
                net.RememberJoinedClientRoomTitle("LATE ROOM", "B2CD", operation);
                Assert.AreEqual("", net.JoinedClientRoomTitle,
                    "A delayed completion restored title metadata after an unsolicited disconnect.");
                operation = BeginTitleOperation(net);
                net.RememberJoinedClientRoomTitle("NEW ROOM", "B2CD", operation);
                Assert.AreEqual("NEW ROOM", net.JoinedClientRoomTitle);
                net.Stop();
                net.Lobby.SetJoinCode("B2CD");
                Assert.AreEqual("", net.JoinedClientRoomTitle, "Stop retained a client room title.");
            }
            finally
            {
                net.Stop();
                NetAuthority.Provider = previousProvider;
                NetSession.RoomTitle = previousTitle;
            }
            yield return null;
        }

        [UnityTest, Timeout(30000)]
        public IEnumerator RestartingAListeningClientKeepsOnlyTheNewTitleOperation()
        {
            var net = NetSession.Ensure();
            var previousProvider = NetAuthority.Provider;
            try
            {
                net.Stop();
                NetAuthority.Provider = net;
                yield return AwaitClientStart(net.StartClientAsync("127.0.0.1", 18679));
                Assert.IsTrue(net.IsNetworked && !net.IsHost,
                    "The first client was not listening before replacement.");
                var oldOperation = net.ClientTitleOperation;

                var restart = net.StartClientAsync("127.0.0.1", 18680);
                var newOperation = net.ClientTitleOperation;
                Assert.IsTrue(newOperation.HasValue && newOperation.Value.OwnsSession);
                Assert.IsFalse(oldOperation?.OwnsSession == true,
                    "The prior client still owns the replacement operation.");
                yield return AwaitClientStart(restart);
                Assert.IsTrue(newOperation.Value.CanContinue,
                    "Stopping the prior listening transport invalidated the replacement token.");
                net.Lobby.SetJoinCode("B2CD");
                net.RememberJoinedClientRoomTitle("RESTARTED ROOM", "B2CD", newOperation);
                Assert.AreEqual("RESTARTED ROOM", net.JoinedClientRoomTitle);
                net.RememberJoinedClientRoomTitle("STALE ROOM", "B2CD", oldOperation);
                Assert.AreEqual("RESTARTED ROOM", net.JoinedClientRoomTitle,
                    "The replaced operation overwrote the new client's title.");
            }
            finally
            {
                net.Stop();
                NetAuthority.Provider = previousProvider;
            }
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
                foreach(var elapsed in new[]{10f,180f})
                {
                    typeof(Matchmaker).GetProperty("Elapsed").GetSetMethod(true).Invoke(queue,new object[]{elapsed});
                    typeof(QueueCard).GetMethod("Refresh",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(card,null);yield return null;
                    foreach(var size in TumpUiCapture.PcViewports)
                        yield return TumpUiCapture.Capture("QueueStatus-"+elapsed+"-"+size.x+"x"+size.y,canvas,size.x,size.y,false,checkActionBounds:true);
                }
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
